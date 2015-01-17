using System;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace TagJam18.Entities
{
    [StaticTileEntity]
    class Wall : Entity, INeedsAdjacencyInformation
    {
        public Vector3 Position { get; private set; }
        private int TileX;
        private int TileY;

        private GeometricPrimitive mesh;
        private const string meshId = "Wall/Mesh";

        private bool connectedLeft = false;
        private bool connectedRight = false;
        private bool connectedTop = false;
        private bool connectedBottom = false;

        public const float Thickness = 0.2f;
        public const float Height = 3f;

        private bool HorizontalDrawEnabled;
        private Vector3 HorizontalDrawOffset;
        private Vector3 HorizontalDrawScale;
        private bool VerticalDrawEnabled;
        private Vector3 VerticalDrawOffset;
        private Vector3 VerticalDrawScale;

        private Vector3 baseScaling = new Vector3(Thickness, Thickness, Height);
        private Vector3 baseOffset = new Vector3(0f, 0f, -Height / 2f);

        private Texture2D texture;
        private string textureId = "Wall/Bricks";

        [TilesetConstructor(1)]
        public Wall(TagGame parentGame, int x, int y)
            : base(parentGame)
        {
            this.Position = new Vector3((float)x, (float)y, 0);
            mesh = ParentGame.Resources.Get<GeometricPrimitive>(meshId, () => GeometricPrimitive.Cube.New(ParentGame.GraphicsDevice));

            TileX = x;
            TileY = y;

            texture = ParentGame.Resources.Get<Texture2D>(textureId, () => ParentGame.Content.Load<Texture2D>("bricks"));
        }

        public void ComputeAdjacency(Level level)
        {
            connectedLeft = ConnectsWith(level.GetStaticEntityAt(TileX - 1, TileY));
            connectedRight = ConnectsWith(level.GetStaticEntityAt(TileX + 1, TileY));
            connectedTop = ConnectsWith(level.GetStaticEntityAt(TileX, TileY - 1));
            connectedBottom = ConnectsWith(level.GetStaticEntityAt(TileX, TileY + 1));

            HorizontalDrawEnabled = false;
            HorizontalDrawOffset = baseOffset;
            HorizontalDrawScale = baseScaling;

            if (connectedLeft)
            {
                HorizontalDrawEnabled = true;
                HorizontalDrawScale += new Vector3(0.5f - Thickness / 2f, 0f, 0f);
                HorizontalDrawOffset -= new Vector3(0.5f - Thickness / 2f, 0f, 0f);
            }

            if (connectedRight)
            {
                HorizontalDrawEnabled = true;
                HorizontalDrawScale += new Vector3(0.5f - Thickness / 2f, 0f, 0f);
                HorizontalDrawOffset += new Vector3(0.5f - Thickness / 2f, 0f, 0f);

                if (!connectedLeft)
                {
                    HorizontalDrawScale += new Vector3(Thickness, 0f, 0f);
                    HorizontalDrawOffset -= new Vector3(Thickness / 2f, 0f, 0f);
                }
            }
            else if (connectedLeft)
            {
                HorizontalDrawScale += new Vector3(Thickness, 0f, 0f);
                HorizontalDrawOffset += new Vector3(Thickness / 2f, 0f, 0f);
            }

            VerticalDrawEnabled = false;
            VerticalDrawOffset = baseOffset;
            VerticalDrawScale = baseScaling;

            if (connectedTop)
            {
                VerticalDrawEnabled = true;
                VerticalDrawScale += new Vector3(0.5f - Thickness / 2f, 0f, 0f);
                VerticalDrawOffset -= new Vector3(0f, 0.5f - Thickness / 2f, 0f);
            }

            if (connectedBottom)
            {
                VerticalDrawEnabled = true;
                VerticalDrawScale += new Vector3(0.5f - Thickness / 2f, 0f, 0f);
                VerticalDrawOffset += new Vector3(0f, 0.5f - Thickness / 2f, 0f);
            }
        }

        private bool ConnectsWith(Entity entity)
        {
            return entity is Wall || entity is Door;
        }

        public override void Render(GameTime gameTime)
        {
            ParentGame.BasicEffect.Texture = texture;
            ParentGame.BasicEffect.TextureEnabled = true;

            if (HorizontalDrawEnabled)
            {
                ParentGame.BasicEffect.World = Matrix.Scaling(HorizontalDrawScale) * Matrix.Translation(Position + HorizontalDrawOffset);
                mesh.Draw(ParentGame.BasicEffect);
            }

            if (VerticalDrawEnabled)
            {
                ParentGame.BasicEffect.World = Matrix.Scaling(VerticalDrawScale) * Matrix.RotationZ(MathF.Pi / 2f) * Matrix.Translation(Position + VerticalDrawOffset);
                mesh.Draw(ParentGame.BasicEffect);
            }

            //if (!HorizontalDrawEnabled && !VerticalDrawEnabled)
            {
                ParentGame.BasicEffect.World = Matrix.Scaling(baseScaling) * Matrix.Translation(Position + baseOffset - Vector3.UnitZ);
                ParentGame.BasicEffect.TextureEnabled = false;
                mesh.Draw(ParentGame.BasicEffect);
            }

            ParentGame.BasicEffect.TextureEnabled = false;
            ParentGame.BasicEffect.Texture = null;
        }

        protected override void Dispose(bool disposing)
        {
            ParentGame.Resources.Drop(meshId, mesh);
            ParentGame.Resources.Drop(textureId, texture);
        }
    }
}
