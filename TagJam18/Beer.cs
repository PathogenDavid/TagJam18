using System;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace TagJam18
{
    class Beer : Entity
    {
        public Vector3 Position { get; private set; }
        private GeometricPrimitive mesh;
        private const string meshId = "Beer/Mesh";

        [TilesetConstructor(2)]
        public Beer(TagGame parentGame, float x, float y)
            : base(parentGame)
        {
            this.Position = new Vector3(x, y, 0f);
            mesh = ParentGame.Resources.Get<GeometricPrimitive>(meshId, () => GeometricPrimitive.Teapot.New(ParentGame.GraphicsDevice));
        }

        public override void Render(GameTime gameTime)
        {
            ParentGame.effect.World = Matrix.Scaling(0.7f) * Matrix.RotationX(-MathF.Pi / 2f) * Matrix.Translation(Position);
            mesh.Draw(ParentGame.effect);
        }

        protected override void Dispose(bool disposing)
        {
            ParentGame.Resources.Drop(meshId, mesh);
        }
    }
}
