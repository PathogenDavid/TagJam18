using System;
using SharpDX;
using SharpDX.Toolkit;

namespace TagJam18
{
    class Player : Entity
    {
        public Vector3 Position { get; private set; }
        
        [TilesetConstructor(5)]
        public Player(TagGame parentGame, float x, float y) : base(parentGame)
        {
            this.Position = new Vector3(x, y, 0f);
        }

        public override void Render(GameTime gameTime)
        {
            ParentGame.effect.World = Matrix.RotationX(MathF.Pi / 2f) * Matrix.Translation(Position);
            ParentGame.cylinder.Draw(ParentGame.effect);
        }
    }
}
