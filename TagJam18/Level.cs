#if DEBUG
//#define DEBUG_ALIGNMENTS
#endif

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.IO;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Direct3D11;
using DxSamplerState = SharpDX.Direct3D11.SamplerState;
using TagJam18.Entities;

namespace TagJam18
{
    using SharpDX.Toolkit.Graphics;

    public class Level : Entity
    {
        private delegate Entity TilesetConstructorDelegate(TagGame parentGame, float x, float y);
        private static Dictionary<int, ConstructorInfo> entityConstructors;

        public float Width { get; private set; }
        public float Height { get; private set; }
        public int TileWidth { get; private set; }
        public int TileHeight { get; private set; }

        private Texture2D concrete;
        private List<Texture2D> concreteDecals = new List<Texture2D>();
        private const string concreteIdFormat = "Level/Concrete{0}";
        private const int concreteTextureCount = 10;

        private GeometricPrimitive groundMesh;
        private BasicEffect groundEffect;
        private const string groundEffectId = "Level/GroundEffect";
        private DxSamplerState groundEffectSamplerState;
        private const string groundEffectSamplerStateId = "Level/GroundEffectSamplerState";
        private GeometricPrimitive decalMesh;
        private const string decalMeshId = "Level/DecalMesh";
#if DEBUG_ALIGNMENTS
        private const float groundTextureSize = 1f;
#else
        private const float groundTextureSize = 7f;
#endif
        private const int minNumGroundDecals = 2;
        private const float groundShrinkage = 0.5f; // Shrink to end at the center of outer walls, shrinks by this amount on all sides
        private float GroundWidth;
        private float GroundHeight;

        private Entity[,] staticTileEntities;

        private class GroundDecal
        {
            public readonly int DecalNumber;
            public readonly Vector3 Position;
            public readonly int TileX;
            public readonly int TileY;

            public GroundDecal(int decalNumber, int tileX, int tileY)
            {
                this.DecalNumber = decalNumber;
                this.Position = new Vector3(
                    (float)tileX * groundTextureSize + groundTextureSize / 2f,
                    (float)tileY * groundTextureSize + groundTextureSize / 2f,
                    0f
                );

                this.TileX = tileX;
                this.TileY = tileY;
            }
        }
        private List<GroundDecal> decals = new List<GroundDecal>();

        static Level()
        {
            entityConstructors = new Dictionary<int, ConstructorInfo>();

            foreach (Type type in typeof(Level).Assembly.GetTypes())
            foreach (ConstructorInfo constructor in type.GetConstructors())
            foreach (TilesetConstructorAttribute attrib in constructor.GetCustomAttributes<TilesetConstructorAttribute>(false))
            {
                if (!type.IsSubclassOf(typeof(Entity)))
                { throw new InvalidOperationException("TilesetConstructorAttribute can only be applied to classes which implement the Entity class."); }

                if (entityConstructors.ContainsKey(attrib.Id))
                { throw new InvalidOperationException("This assembly has more than one Entity that can construct " + attrib.Id.ToString() + "."); }

                //TODO: Ensure parameters match.
                //TODO: Ensure function is implemented.

                entityConstructors.Add(attrib.Id, constructor);
            }
        }

        public Level(TagGame parentGame, string file) : base(parentGame)
        {
            RenderOrder = Int32.MinValue;

            // Load the level
            using (StreamReader f = new StreamReader(file))
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(f);
                XmlNodeSimple xml = xmlDocument;

                TileWidth = Convert.ToInt32(xml["//map/@width"].Value);
                TileHeight = Convert.ToInt32(xml["//map/@height"].Value);
                Width = (float)TileWidth;
                Height = (float)TileHeight;
                staticTileEntities = new Entity[TileWidth, TileHeight];

                // Parse tile layers
                XmlNodeList layers = xml.SelectNodes("//map/layer");
                if (layers.Count != 1)
                { throw new ArgumentException("The given level file is invalid, it has the incorrect number of tile layers.", "file"); }

                XmlNodeSimple layer = layers[0];
                if (Convert.ToInt32(layer["@width"].Value) != TileWidth || Convert.ToInt32(layer["@height"].Value) != TileHeight)
                { throw new ArgumentException("The given level file is invalid, it contains tile layers that don't match the map dimensions.", "file"); }

                if (layer["data/@encoding"].Value != "csv")
                { throw new ArgumentException("The given level file is invalid, it uses a tile layer encoding other than CSV.", "file"); }

                ProcessCsv(layer["data/text()"].Value);

                //TODO: Parse object layers
            }

            // Go over the entire level and compute adjacency information
            foreach (INeedsAdjacencyInformation entity in ParentGame.GetEntities<INeedsAdjacencyInformation>())
            {
                entity.ComputeAdjacency(this);
            }

            // Load resources for rendering ground
            concrete = LoadConcreteTexture(0);

            for (int i = 1; i < concreteTextureCount; i++)
            {
                concreteDecals.Add(LoadConcreteTexture(i));
            }

            GroundWidth = Width - groundShrinkage * 2;
            GroundHeight = Height - groundShrinkage * 2;
            groundMesh = GeometricPrimitive.Plane.New(ParentGame.GraphicsDevice, GroundWidth, GroundHeight, 1, new Vector2(GroundWidth / groundTextureSize, GroundHeight / groundTextureSize));
            decalMesh = ParentGame.Resources.Get<GeometricPrimitive>(decalMeshId, () => GeometricPrimitive.Plane.New(ParentGame.GraphicsDevice, groundTextureSize, groundTextureSize));

            SamplerStateDescription samplerStateDescription = ((DxSamplerState)ParentGame.BasicEffect.Sampler).Description;
            samplerStateDescription.AddressU = TextureAddressMode.Wrap;
            samplerStateDescription.AddressV = TextureAddressMode.Wrap;

            groundEffectSamplerState = ParentGame.Resources.Get<DxSamplerState>(groundEffectSamplerStateId, () => new SharpDX.Direct3D11.SamplerState(ParentGame.GraphicsDevice, samplerStateDescription));

            groundEffect = ParentGame.Resources.Get<BasicEffect>(groundEffectId, () => new BasicEffect(ParentGame.GraphicsDevice)
            {
                Projection = ParentGame.BasicEffect.Projection,
                PreferPerPixelLighting = true,
                TextureEnabled = true,
                Sampler = SamplerState.New(ParentGame.GraphicsDevice, groundEffectSamplerState)
            });
            groundEffect.EnableDefaultLighting();

            // Make decals
            int maxDecalX = (int)(GroundWidth / groundTextureSize);
            int maxDecalY = (int)(GroundHeight / groundTextureSize);
            int maxNumDecals = maxDecalX * maxDecalY;
            
            int numDecals;
            if (minNumGroundDecals < maxNumDecals)
            { numDecals = ParentGame.Random.Next(minNumGroundDecals, maxNumDecals); }
            else
            { numDecals = maxNumDecals = minNumGroundDecals; }

            for (int i = 0, attempts = 0; i < numDecals && attempts < 5; i++, attempts++)
            {
                int x = ParentGame.Random.Next(maxDecalX);
                int y = ParentGame.Random.Next(maxDecalY);

                if (!IsDecalSpotFree(x, y))
                {
                    i--;
                    continue;
                }

                decals.Add(new GroundDecal(ParentGame.Random.Next(0, concreteDecals.Count), x, y));
            }
        }

        private bool IsDecalSpotFree(int x, int y)
        {
            foreach (GroundDecal decal in decals)
            {
                if (decal.TileX == x && decal.TileY == y)
                { return false; }
            }
            return true;
        }

        private Texture2D LoadConcreteTexture(int num)
        {
            string id = String.Format(concreteIdFormat, num);
            string assetName = String.Format("concrete{0}", num);
            return ParentGame.Resources.Get<Texture2D>(id, () => ParentGame.Content.Load<Texture2D>(assetName));
        }

        private void DropConcreteTexture(int num)
        {
            string id = String.Format(concreteIdFormat, num);
            ParentGame.Resources.Drop(id, num == 0 ? concrete : concreteDecals[num - 1]);
        }

        private void ProcessCsv(string csv)
        {
            int x = -1;
            int y = -1;
            string[] csvLines = csv.Replace("\r", "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in csvLines)
            {
                y++;
                x = -1;

                string[] csvTokens = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string token in csvTokens)
                {
                    x++;

                    int id = Convert.ToInt32(token);
                    if (id == 0) { continue; }

                    MakeEntity(id, x, y);
                }
            }
        }

        private void MakeEntity(int id, int x, int y)
        {
            if (!entityConstructors.ContainsKey(id))
            {
                Debug.Print("WARNING: I don't know how to make an entity of ID {0} at {1},{2}!", id, x, y);
                return;
            }

            Entity newEntity = (Entity)entityConstructors[id].Invoke(this, x, y);

            if (newEntity.GetType().GetCustomAttribute<StaticTileEntityAttribute>() != null)
            {
                Debug.Assert(staticTileEntities[x, y] == null);
                staticTileEntities[x, y] = newEntity;
            }
        }

        public Entity GetStaticEntityAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= TileWidth || y >= TileHeight)
            { return null; }

            return staticTileEntities[x, y];
        }

        public Entity SetStaticEntityAt(Entity newEntity, int x, int y)
        {
            Entity oldEntity = GetStaticEntityAt(x, y);
            staticTileEntities[x, y] = newEntity;
            return oldEntity;
        }

        public override void Render(GameTime gameTime)
        {
            ParentGame.GraphicsDevice.SetDepthStencilState(ParentGame.GraphicsDevice.DepthStencilStates.DepthRead);
            groundEffect.View = ParentGame.Camera.ViewTransform;

#if DEBUG_ALIGNMENTS
            bool dark;
            groundEffect.TextureEnabled = false;
            for (int y = 0; y < TileHeight; y++)
            {
                dark = y % 2 == 0;

                for (int x = 0; x < TileWidth; x++)
                {
                    groundEffect.World = Matrix.RotationX(MathF.Pi) * Matrix.Translation((float)x, (float)y, 0f);
                    groundEffect.DiffuseColor = (Color4)(dark ? Color.DarkGray : Color.Gray);
                    decalMesh.Draw(groundEffect);

                    dark = !dark;
                }
            }
#else
            groundEffect.World = Matrix.RotationX(MathF.Pi) * Matrix.Translation(GroundWidth / 2f, GroundWidth / 2, 0f);
            groundEffect.Texture = concrete;
            groundMesh.Draw(groundEffect);

            foreach (GroundDecal decal in decals)
            {
                groundEffect.World = Matrix.RotationX(MathF.Pi) * Matrix.Translation(decal.Position);
                groundEffect.Texture = concreteDecals[decal.DecalNumber];
                decalMesh.Draw(groundEffect);
            }
#endif

            ParentGame.GraphicsDevice.SetDepthStencilState(ParentGame.GraphicsDevice.DepthStencilStates.Default);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (int i = 0; i < concreteTextureCount; i++)
                {
                    DropConcreteTexture(i);
                }

                groundMesh.Dispose(); // Not cached in the resource pool
                ParentGame.Resources.Drop(decalMeshId, decalMesh);

                ParentGame.Resources.Drop(groundEffectId, groundEffect);
                ParentGame.Resources.Drop(groundEffectSamplerStateId, groundEffectSamplerState);
            }
        }
    }
}
