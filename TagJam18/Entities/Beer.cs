using System;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace TagJam18.Entities
{
    class Beer : Entity
    {
        public Vector3 Position { get; private set; }
        private GeometricPrimitive mesh;
        private const string meshId = "Beer/Mesh";

        [TilesetConstructor(2)]
        public Beer(TagGame parentGame, int x, int y)
            : base(parentGame)
        {
            this.Position = new Vector3((float)x, (float)y, -0.5f);
            mesh = ParentGame.Resources.Get<GeometricPrimitive>(meshId, () => GeometricPrimitive.Teapot.New(ParentGame.GraphicsDevice));
        }

        public override void Render(GameTime gameTime)
        {
            ParentGame.BasicEffect.World = Matrix.Scaling(0.7f) * Matrix.RotationX(-MathF.Pi / 2f) * Matrix.Translation(Position);
            mesh.Draw(ParentGame.BasicEffect);
        }

        protected override void Dispose(bool disposing)
        {
            ParentGame.Resources.Drop(meshId, mesh);
        }
    }
}
