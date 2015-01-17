using System;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace TagJam18.Entities
{
    class Player : Entity
    {
        public Vector3 Position { get; private set; }
        private GeometricPrimitive mesh;
        private const string meshId = "Player/Mesh";
        
        [TilesetConstructor(5)]
        public Player(TagGame parentGame, int x, int y) : base(parentGame)
        {
            this.Position = new Vector3((float)x, (float)y, -0.5f);
            mesh = ParentGame.Resources.Get<GeometricPrimitive>(meshId, () => GeometricPrimitive.Cylinder.New(ParentGame.GraphicsDevice));
        }

        public override void Render(GameTime gameTime)
        {
            ParentGame.BasicEffect.World = Matrix.RotationX(MathF.Pi / 2f) * Matrix.Translation(Position);
            mesh.Draw(ParentGame.BasicEffect);
        }

        protected override void Dispose(bool disposing)
        {
            ParentGame.Resources.Drop(meshId, mesh);
        }
    }
}
