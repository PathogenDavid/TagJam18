using System;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace TagJam18
{
    class Wall : Entity
    {
        public Vector3 Position { get; private set; }
        private GeometricPrimitive mesh;
        private const string meshId = "Wall/Mesh";

        [TilesetConstructor(1)]
        public Wall(TagGame parentGame, float x, float y)
            : base(parentGame)
        {
            this.Position = new Vector3(x, y, 0f);
            mesh = ParentGame.Resources.Get<GeometricPrimitive>(meshId, () => GeometricPrimitive.Cube.New(ParentGame.GraphicsDevice));
        }

        public override void Render(GameTime gameTime)
        {
            ParentGame.effect.World = Matrix.Translation(Position);
            mesh.Draw(ParentGame.effect);
        }

        protected override void Dispose(bool disposing)
        {
            ParentGame.Resources.Drop(meshId, mesh);
        }
    }
}
