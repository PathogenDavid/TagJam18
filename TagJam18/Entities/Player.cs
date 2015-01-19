using System;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Input;
using SharpDX.Toolkit.Graphics;

namespace TagJam18.Entities
{
    public class Player : Entity, INeedsAdjacencyInformation
    {
        private GeometricPrimitive mesh;
        private const string meshId = "Player/Mesh";
        private const float walkSpeed = 1.5f;
        private const float runSpeed = 2.5f;

        private string[] NotDrunkEnoughMessage =
        {
            "I can't make art while I'm sober!",
            "But...giant beers!",
            "Not until I'm slewed!",
            "Need...TAG...beer...",
            "No beer, no spaniels."
        };

        private string[] DrunkEnoughMessage =
        {
            "All right, letsh do thish!",
            "*hic*No time like the*hic*present",
            "Deface everything! #TAGJAM18",
            "I'd say I'm slewed enough now!"
        };

        private string[] DrinkMessage =
        {
            "Mmmm, delicious",
            "I love #TAGJAM18 beer!",
            "Best beer ever",
            "*burp* yum!"
        };

        private string[] DrunkDrinkMessage =
        {
            "Mmmm,*hic*delicious",
            "I love*hic*#TAGJAM18 beer!",
            "Besht*hic*beer eber",
            "*burp* gettin' there..."
        };

        private string[] BuzzedMessage =
        {
            "Got a nice buzz going on.",
            "Now I'm feel'in it!",
            "*burp* that's the stuff!"
        };

        private string[] DoneTaggingMessage =
        {
            "Arf arf, mother**ker!",
            "Logan: 1, The Man: 0",
            "A true *hic* masterpiece!",
            "Are you proud of me now, madmarcel?"
        };

        private string NotTaggableMessage =
        {
            "Ehh, this isn't the best spot.",
            "I can do better.",
        };

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

        public void ComputeAdjacency(Level level)
        {
            // We don't actually need adjacency information, but this runs after the level is fully set up so we can use it to get a beer count.
            int numBeers = ParentGame.GetEntities<Beer>().Count();
            ToleranceBuzzed = Math.Min(numBeers / 4, 3);
            Tolerance = numBeers - numBeers / 4;

            if (Tolerance < 3)
            {
                ToleranceBuzzed = 1;
                Tolerance = numBeers;
            }

            Debug.Print("Player will need {0} beers to get buzzed and {1} beers to get slewed.", ToleranceBuzzed, Tolerance);
        }

        public void DrinkBeer()
        {
            bool wasDrunk = IsDrunk;
            BeersDranken++;
            Debug.Print("GLUG GLUG GLUG. %Drunk = {0}", PercentDrunkRaw);

            string[] messages = DrinkMessage;
            if (IsBrave)
            { messages = DrunkEnoughMessage; }
            else if (!wasDrunk && IsDrunk)
            { messages = BuzzedMessage; }
            else if (PercentDrunkRaw > 0.6f)
            { messages = DrunkDrinkMessage; }

            ParentGame.AddSpeechBubble(new SpeechBubble(ParentGame, messages, Position));
        }

        public override void Render(GameTime gameTime)
        {
            ParentGame.BasicEffect.World = Matrix.RotationX(MathF.Pi / 2f) * Matrix.Translation(Position);
            mesh.Draw(ParentGame.BasicEffect);
        }

        public override void Update(GameTime gameTime)
        {
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
            if (Disposed)
            { return; }

            if (disposing)
            {
                ParentGame.Resources.Drop(meshId, mesh);
            }
        }
    }
}
