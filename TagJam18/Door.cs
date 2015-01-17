using System;
using SharpDX;
using SharpDX.Toolkit;

namespace TagJam18
{
    class Door : Entity
    {
        public Vector3 Position { get; private set; }

        [TilesetConstructor(4)]
        public Door(TagGame parentGame, float x, float y)
            : base(parentGame)
        {
            this.Position = new Vector3(x, y, 0f);
        }

        public override void Render(GameTime gameTime)
        {
            ParentGame.effect.World = Matrix.Translation(Position);
            ParentGame.torus.Draw(ParentGame.effect);
        }
    }
}
