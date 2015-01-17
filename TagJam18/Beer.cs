using System;
using SharpDX;
using SharpDX.Toolkit;

namespace TagJam18
{
    class Beer : Entity
    {
        public Vector3 Position { get; private set; }

        [TilesetConstructor(2)]
        public Beer(TagGame parentGame, float x, float y)
            : base(parentGame)
        {
            this.Position = new Vector3(x, y, 0f);
        }

        public override void Render(GameTime gameTime)
        {
            ParentGame.effect.World = Matrix.Scaling(0.7f) * Matrix.RotationX(-MathF.Pi / 2f) * Matrix.Translation(Position);
            ParentGame.teapot.Draw(ParentGame.effect);
        }
    }
}
