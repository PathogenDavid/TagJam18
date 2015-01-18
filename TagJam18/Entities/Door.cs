using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace TagJam18.Entities
{
    [StaticTileEntity]
    class Door : Entity, INeedsAdjacencyInformation
    {
        private GeometricPrimitive mesh;
        private const string meshId = "Door/Mesh";
        private Texture2D texture;
        private const string textureId = "Door/Texture";

        int TileX;
        int TileY;

        public const float Thickness = Wall.Thickness / 4f;
        public const float Width = 1f + 0.05f; // A little extra to fill cap between double doors
        public const float Height = Wall.Height;

        private Matrix modelTransform;
        private Matrix worldTransform;
        bool attachLeft = true;
        bool attachRight;
        bool attachUp;
        bool attachDown;

        [TilesetConstructor(4)]
        public Door(TagGame parentGame, int x, int y)
            : base(parentGame)
        {
            TileX = x;
            TileY = y;
            mesh = ParentGame.Resources.Get<GeometricPrimitive>(meshId, () => GeometricPrimitive.Cube.New(ParentGame.GraphicsDevice));
            texture = ParentGame.Resources.Get<Texture2D>(textureId, () => ParentGame.Content.Load<Texture2D>("Door"));
            ComputeTransforms();
        }

        public void ComputeAdjacency(Level level)
        {
            attachLeft = attachRight = attachUp = attachDown = false;

            if (level.GetStaticEntityAt(TileX - 1, TileY) is Wall)
            { attachLeft = true; }
            else if (level.GetStaticEntityAt(TileX + 1, TileY) is Wall)
            { attachRight = true; }
            else if (level.GetStaticEntityAt(TileX, TileY - 1) is Wall)
            { attachUp = true; }
            else if (level.GetStaticEntityAt(TileX, TileY + 1) is Wall)
            { attachDown = true; }
            else
            { attachLeft = true; } // If for some reason the door is floating in space with no walls, attach it on the left.

            ComputeTransforms();
        }

        private void ComputeTransforms()
        {
            Debug.Assert(attachLeft || attachRight || attachUp || attachDown);

            // Make the door the right shape and move it out of the "ground"
            modelTransform = Matrix.Scaling(Width, Thickness, Height) * Matrix.Translation(0f, 0f, -Height / 2f);

            // Rotate door to proper orientation
            if (attachRight)
            { modelTransform *= Matrix.RotationZ(MathF.Pi); }
            else if (attachUp)
            { modelTransform *= Matrix.RotationZ(-MathF.Pi / 2f); }
            else if (attachDown)
            { modelTransform *= Matrix.RotationZ(MathF.Pi / 2f); }

            // Move the door so Z axis goes through the edge of the door
            float doorRotationOffset = Width / 2f - Thickness / 2f;
            if (attachLeft)
            { modelTransform *= Matrix.Translation(doorRotationOffset, 0f, 0f); }
            else if (attachRight)
            { modelTransform *= Matrix.Translation(-doorRotationOffset, 0f, 0f); }
            else if (attachUp)
            { modelTransform *= Matrix.Translation(0f, doorRotationOffset, 0f); }
            else if (attachDown)
            { modelTransform *= Matrix.Translation(0f, -doorRotationOffset, 0f); }

            // Compute the door's world transform
            float offsetAlong = Width / 2f;
            //offsetAlong = 0f;
            float x = (float)TileX;
            float y = (float)TileY;

            if (attachLeft)
            { worldTransform = Matrix.Translation(x - offsetAlong, y, 0f); }
            else if (attachRight)
            { worldTransform = Matrix.Translation(x + offsetAlong, y, 0f); }
            else if (attachUp)
            { worldTransform = Matrix.Translation(x, y - offsetAlong, 0f); }
            else if (attachDown)
            { worldTransform = Matrix.Translation(x, y + offsetAlong, 0f); }
        }

        public override void Render(GameTime gameTime)
        {
            ParentGame.BasicEffect.World = modelTransform * worldTransform;
            ParentGame.BasicEffect.Texture = texture;
            ParentGame.BasicEffect.TextureEnabled = true;

            mesh.Draw(ParentGame.BasicEffect);

            ParentGame.BasicEffect.TextureEnabled = false;
            ParentGame.BasicEffect.Texture = null;
        }

        protected override void Dispose(bool disposing)
        {
            ParentGame.Resources.Drop(meshId, mesh);
        }
    }
}
