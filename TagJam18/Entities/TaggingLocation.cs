using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace TagJam18.Entities
{
    [StaticTileEntity]
    class TaggingLocation : Entity, INeedsAdjacencyInformation
    {
        // Note: tile X and Y are at left-top corner of tagging location, Position is at center.
        private int tileX;
        private int tileY;
        private int tileWidth;
        private int tileHeight;

        /// <summary>
        /// True when this tagging location has been absorbed by another tagging location
        /// </summary>
        private bool wasAbsorbed;

        private GeometricPrimitive mesh;
        private const string meshId = "TaggingLocation/Cube";

        private bool IsHorizontal
        {
            get { return tileWidth >= tileHeight; }
        }

        private bool IsVertical
        {
            get { return tileHeight >= tileWidth; }
        }

        private enum WallDirection
        {
            Unknown,
            Up,
            Down,
            Left,
            Right
        }
        private WallDirection wallDirection;

        private Level level;

        private Entity entityLeft
        {
            get { return level.GetStaticEntityAt(tileX - 1, tileY); }
        }

        private Entity entityRight
        {
            get { return level.GetStaticEntityAt(tileX + tileWidth, tileY); }
        }

        private Entity entityUp
        {
            get { return level.GetStaticEntityAt(tileX, tileY - 1); }
        }

        private Entity entityDown
        {
            get { return level.GetStaticEntityAt(tileX, tileY + tileHeight); }
        }
        
        [TilesetConstructor(3)]
        public TaggingLocation(Level level, int x, int y)
            : base(level.ParentGame)
        {
            this.level = level;
            tileX = x;
            tileY = y;
            tileWidth = 1;
            tileHeight = 1;
            Position = new Vector3((float)x, (float)y, 0f);

            mesh = ParentGame.Resources.Get<GeometricPrimitive>(meshId, () => GeometricPrimitive.Cube.New(ParentGame.GraphicsDevice));
        }

        public void ComputeAdjacency(Level level)
        {
            if (wasAbsorbed)
            { return; }

            // First, we merge all tagging locations together
            Debug.Print("Computing merge for {0},{1}...", tileX, tileY);
            while (MergeWithOtherTaggingLocations()) ;
            Debug.Print("Done computing merge. Am now {0},{1} {2}x{3}", tileX, tileY, tileWidth, tileHeight);

            // Finally, we figure out the wall direction
            if (IsHorizontal)
            {
                if (entityUp is Wall)
                { wallDirection = WallDirection.Up; }
                else if (entityDown is Wall)
                { wallDirection = WallDirection.Down; }
            }
            
            if (IsVertical)
            {
                if (entityLeft is Wall)
                { wallDirection = WallDirection.Left; }
                else if (entityRight is Wall)
                { wallDirection = WallDirection.Right; }
            }

            Debug.Print("I have a wall on my {0}", wallDirection);
        }

        /// <returns>True if another TaggingLocation was absorbed in this computation</returns>
        private bool MergeWithOtherTaggingLocations()
        {
            if (IsHorizontal && entityLeft is TaggingLocation)
            {
                Debug.Print("Combining with entity on left.");
                CombineWith((TaggingLocation)entityLeft);
                return true;
            }

            if (IsHorizontal && entityRight is TaggingLocation)
            {
                Debug.Print("Combining with entity on right.");
                CombineWith((TaggingLocation)entityRight);
                return true;
            }

            if (IsVertical && entityUp is TaggingLocation)
            {
                Debug.Print("Combining with entity on up.");
                CombineWith((TaggingLocation)entityUp);
                return true;
            }

            if (IsVertical && entityDown is TaggingLocation)
            {
                Debug.Print("Combining with entity on down.");
                CombineWith((TaggingLocation)entityDown);
                return true;
            }

            return false;
        }

        private void CombineWith(TaggingLocation other)
        {
            // This method should handle it, but with the way we process right now, we shouldn't expect to see any locations with >1 deimensions.
            Debug.Assert(other.tileWidth == 1 && other.tileHeight == 1);

            if (tileX == other.tileX)
            { this.tileHeight += other.tileHeight; }
            else
            { this.tileWidth += other.tileWidth; }

            if (tileWidth > 1 && tileHeight > 1)
            { throw new InvalidOperationException("Tried to combine with a tagging location that we shouldn't combine with!"); }

            this.tileX = Math.Min(this.tileX, other.tileX);
            this.tileY = Math.Min(this.tileY, other.tileY);

            level.SetStaticEntityAt(this, other.tileX, other.tileY);
            other.Remove();
            other.wasAbsorbed = true;

            // Update position:
            Position = new Vector3((float)tileX + (float)(tileWidth - 1) / 2f, (float)tileY + (float)(tileHeight - 1) / 2f, 0f);
        }

        public override void Render(GameTime gameTime)
        {
            Vector3 offset = Vector3.Zero;
            float distToWall = 0.5f - Wall.Thickness;
            switch (wallDirection)
            {
                case WallDirection.Up:
                    offset = new Vector3(0f, -distToWall, 0f);
                    break;
                case WallDirection.Down:
                    offset = new Vector3(0f, distToWall, 0f);
                    break;
                case WallDirection.Left:
                    offset = new Vector3(-distToWall, 0f, 0f);
                    break;
                case WallDirection.Right:
                    offset = new Vector3(distToWall, 0f, 0f);
                    break;
            }

            ParentGame.BasicEffect.World = Matrix.Scaling((float)tileWidth, (float)tileHeight, 1f) * Matrix.Translation(Position + new Vector3(0f, 0f, -0.5f) + offset);
            mesh.Draw(ParentGame.BasicEffect);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ParentGame.Resources.Drop(meshId, mesh);
            }
        }
    }
}
