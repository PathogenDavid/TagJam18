using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace TagJam18.Entities
{
    //TODO: Right now if a tagging location ends at a corner / end of wall, it will stick past the wall.
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
        private Texture2D texture;
        private const string textureId = "TaggingLocation/Texture";

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

            mesh = ParentGame.Resources.Get<GeometricPrimitive>(meshId, () => GeometricPrimitive.Plane.New(ParentGame.GraphicsDevice));
            texture = ParentGame.Resources.Get<Texture2D>(textureId, () => ParentGame.Content.Load<Texture2D>("Orange"));
        }

        public void ComputeAdjacency(Level level)
        {
            if (wasAbsorbed)
            { return; }

            // First, we merge all tagging locations together
            while (MergeWithOtherTaggingLocations()) ;

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
        }

        /// <returns>True if another TaggingLocation was absorbed in this computation</returns>
        private bool MergeWithOtherTaggingLocations()
        {
            if (IsHorizontal && entityLeft is TaggingLocation)
            {
                CombineWith((TaggingLocation)entityLeft);
                return true;
            }

            if (IsHorizontal && entityRight is TaggingLocation)
            {
                CombineWith((TaggingLocation)entityRight);
                return true;
            }

            if (IsVertical && entityUp is TaggingLocation)
            {
                CombineWith((TaggingLocation)entityUp);
                return true;
            }

            if (IsVertical && entityDown is TaggingLocation)
            {
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
            float distToWall = 0.5f + 0.5f - Wall.Thickness / 2f;
            distToWall -= 0.01f; // Subtract a little to prevent Z-fighting.

            // Make the poster the correct size
            Matrix transform = Matrix.Scaling((float)Math.Max(tileWidth, tileHeight), Wall.Height - 1f, 1f);
            transform *= Matrix.RotationX(MathF.Pi); // Flip the plane over

            // Move the poster to the wall it is on
            switch (wallDirection)
            {
                case WallDirection.Up:
                    offset = new Vector3(0f, -distToWall, 0f);
                    transform *= Matrix.RotationX(MathF.Pi / 2f);
                    break;
                case WallDirection.Down:
                    offset = new Vector3(0f, distToWall, 0f);
                    transform *= Matrix.RotationX(-MathF.Pi / 2f);
                    break;
                case WallDirection.Left:
                    offset = new Vector3(-distToWall, 0f, 0f);
                    transform *= Matrix.RotationX(MathF.Pi / 2f);
                    transform *= Matrix.RotationZ(-MathF.Pi / 2f);
                    break;
                case WallDirection.Right:
                    offset = new Vector3(distToWall, 0f, 0f);
                    transform *= Matrix.RotationX(MathF.Pi / 2f);
                    transform *= Matrix.RotationZ(MathF.Pi / 2f);
                    break;
            }

            // Raise the poster to the correct height
            offset += new Vector3(0f, 0f, -Wall.Height / 2f);

            // Move to proper location
            transform *= Matrix.Translation(Position + offset);

            ParentGame.BasicEffect.World = transform;
            ParentGame.BasicEffect.Texture = texture;
            ParentGame.BasicEffect.TextureEnabled = true;
            
            mesh.Draw(ParentGame.BasicEffect);

            ParentGame.BasicEffect.TextureEnabled = false;
            ParentGame.BasicEffect.Texture = null;
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
