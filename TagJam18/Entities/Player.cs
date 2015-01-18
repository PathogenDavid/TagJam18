using System;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Input;
using SharpDX.Toolkit.Graphics;

namespace TagJam18.Entities
{
    public class Player : Entity
    {
        private GeometricPrimitive mesh;
        private const string meshId = "Player/Mesh";
        private const float walkSpeed = 1.5f;
        private const float runSpeed = 2.5f;

        public int BeersDranken { get; private set; }
        
        [TilesetConstructor(5)]
        public Player(TagGame parentGame, int x, int y) : base(parentGame)
        {
            RenderOrder = -1; // Also affects update order. Will ensure player position is fresh for all entities.
            this.Position = new Vector3((float)x, (float)y, -0.5f);
            this.CollisionSize = 1f;
            mesh = ParentGame.Resources.Get<GeometricPrimitive>(meshId, () => GeometricPrimitive.Cylinder.New(ParentGame.GraphicsDevice));
            BeersDranken = 0;
        }

        public void DrinkBeer()
        {
            BeersDranken++;
        }

        public override void Render(GameTime gameTime)
        {
            ParentGame.BasicEffect.World = Matrix.RotationX(MathF.Pi / 2f) * Matrix.Translation(Position);
            mesh.Draw(ParentGame.BasicEffect);
        }

        public override void Update(GameTime gameTime)
        {
            float xSpeed = 0f;
            float ySpeed = 0f;
            bool move = false;

            if (ParentGame.Keyboard.IsKeyDown(Keys.W) || ParentGame.Keyboard.IsKeyDown(Keys.Up))
            { ySpeed -= 1f; move = true; }
            if (ParentGame.Keyboard.IsKeyDown(Keys.S) || ParentGame.Keyboard.IsKeyDown(Keys.Down))
            { ySpeed += 1f; move = true; }

            if (ParentGame.Keyboard.IsKeyDown(Keys.A) || ParentGame.Keyboard.IsKeyDown(Keys.Left))
            { xSpeed -= 1f; move = true; }
            if (ParentGame.Keyboard.IsKeyDown(Keys.D) || ParentGame.Keyboard.IsKeyDown(Keys.Right))
            { xSpeed += 1f; move = true; }

            if (!move)
            { return; }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float speed = ParentGame.Keyboard.IsKeyDown(Keys.Shift) ? runSpeed : walkSpeed;

            Position += new Vector3(xSpeed * speed * deltaTime, ySpeed * speed * deltaTime, 0f);
        }

        protected override void Dispose(bool disposing)
        {
            ParentGame.Resources.Drop(meshId, mesh);
        }
    }
}
