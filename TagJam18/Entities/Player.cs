using System;
using System.Diagnostics;
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
        /// <summary>
        /// The amount of beers the player has to drink before the screen starts distorting.
        /// </summary>
        public int ToleranceBuzzed { get; private set; }
        /// <summary>
        /// The amount of beers the player has to drink before he gets brave enough to tag.
        /// </summary>
        public int Tolerance { get; private set; }
        private const double PercentDrunkExponent = 1.5;
        public float PercentDrunkRaw
        {
            get
            {
                if (!IsDrunk)
                { return 0f; }

                // Cap at 100%
                if (BeersDranken >= Tolerance)
                { return 1f; }

                return (float)Math.Pow((double)(BeersDranken - ToleranceBuzzed) / (double)(Tolerance - ToleranceBuzzed), PercentDrunkExponent);
            }
        }
        public bool IsDrunk
        {
            get { return BeersDranken >= ToleranceBuzzed; }
        }
        public bool IsBrave
        {
            get { return BeersDranken >= Tolerance; }
        }

        /// <summary>
        /// An interpolated version of PercentDrunkRaw
        /// </summary>
        public float PercentDrunk { get; private set; }
        private const float percentDrunkChangeSpeed = 0.1f;

        private Level level;
        
        [TilesetConstructor(5)]
        public Player(Level level, int x, int y)
            : base(level.ParentGame)
        {
            this.level = level;
            RenderOrder = -1; // Also affects update order. Will ensure player position is fresh for all entities.
            this.Position = new Vector3((float)x, (float)y, -0.5f);
            this.CollisionSize = 1f;
            mesh = ParentGame.Resources.Get<GeometricPrimitive>(meshId, () => GeometricPrimitive.Cylinder.New(ParentGame.GraphicsDevice));

            BeersDranken = 0;
            ToleranceBuzzed = 2;//TODO: Determine this based on the number of beers on the level
            Tolerance = 6;//TODO: Determine this based on the number of beers on the level
        }

        public void DrinkBeer()
        {
            BeersDranken++;
            Debug.Print("GLUG GLUG GLUG. %Drunk = {0}", PercentDrunkRaw);
        }

        public override void Render(GameTime gameTime)
        {
            ParentGame.BasicEffect.World = Matrix.RotationX(MathF.Pi / 2f) * Matrix.Translation(Position);
            mesh.Draw(ParentGame.BasicEffect);
        }

        public override void Update(GameTime gameTime)
        {
            if (ParentGame.Keyboard.IsKeyReleased(Keys.Z))
            {
                ParentGame.AddSpeechBubble(new SpeechBubble(ParentGame, "This is a test!*.?", Position));
            }

            // Keep PercentDrunk up-to-date
            if (Math.Abs(PercentDrunk - PercentDrunkRaw) < 0.001f)
            { PercentDrunk = PercentDrunkRaw; }
            else if (PercentDrunk < PercentDrunkRaw)
            { PercentDrunk += percentDrunkChangeSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds; }
            else if (PercentDrunk > PercentDrunkRaw)
            { PercentDrunk -= percentDrunkChangeSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds; }
            
            // Handle movement
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

            Vector3 velocity = new Vector3(xSpeed * speed * deltaTime, ySpeed * speed * deltaTime, 0f);
            const float extraSpace = 0.1f;
            Vector3 newPosition = Position + velocity + velocity.Normalized() * extraSpace;

            // Handle colliding with walls
            int newTileX = (int)Math.Round(newPosition.X);
            int newTileY = (int)Math.Round(newPosition.Y);
            int tileX = (int)Math.Round(Position.X);
            int tileY = (int)Math.Round(Position.Y);

            bool stoppedX = false;
            bool stoppedY = false;

            //TODO: We should calculate the change needed to let the plauyer touch the wall exactly (so the distance they stop from the wall is exact.)
            if (level.GetStaticEntityAt(newTileX, tileY) is Wall)
            {
                velocity.X = 0f;
                stoppedX = true;
            }

            if (level.GetStaticEntityAt(tileX, newTileY) is Wall)
            {
                velocity.Y = 0f;
                stoppedY = true;
            }

            // Handle edge case where player approaches an entity perfectly diagonally.
            if (!stoppedX && !stoppedY && level.GetStaticEntityAt(newTileX, newTileY) is Wall)
            {
                velocity.X = velocity.Y = 0f;
                stoppedX = stoppedY = true;
            }

            if (!stoppedX || !stoppedY)
            {
                Position += velocity;
            }
        }

        protected override void Dispose(bool disposing)
        {
            ParentGame.Resources.Drop(meshId, mesh);
        }
    }
}
