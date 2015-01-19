using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using SharpDX.Toolkit.Input;

namespace TagJam18.Entities
{
    //TODO: Right now if a tagging location ends at a corner / end of wall, it will stick past the wall.
    [StaticTileEntity]
    public class TaggingLocation : Entity, INeedsAdjacencyInformation
    {
        // Note: tile X and Y are at left-top corner of tagging location, Position is at center.
        private int tileX;
        private int tileY;
        private int tileWidth;
        private int tileHeight;

        public const float TypicalTaggingLocationWidth = 3f;
        public const float TagHeight = Wall.Height - 1f;

        /// <summary>
        /// True when this tagging location has been absorbed by another tagging location
        /// </summary>
        private bool wasAbsorbed;

        private GeometricPrimitive mesh;
        private const string meshId = "TaggingLocation/Cube";
        private RenderTarget2D rtt;
        private Texture2D templateTexture;
        private const string templateTextureIdFormat = "TaggingLocation/Template{0}";
        private readonly string templateTextureId;
        private const int maxTemplateTextureNum = 1;
        private int templateNum = 1;

        private Texture2D paintSprayTexture;
        private const string paintSprayTextureId = "TaggingLocation/PaintSpray";

        private Texture2D wallTexture;
        private string wallTextureId = "Wall/Bricks";

        private BasicEffect taggingEffect;

        public bool IsTagged { get; set; }

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
            RenderOrder = 1000; // Need to be rendered after most things because we can be transparent.
            this.level = level;
            tileX = x;
            tileY = y;
            tileWidth = 1;
            tileHeight = 1;
            Position = new Vector3((float)x, (float)y, 0f);

            mesh = ParentGame.Resources.Get<GeometricPrimitive>(meshId, () => GeometricPrimitive.Plane.New(ParentGame.GraphicsDevice));

            // Load template texture
            templateNum = 1;//TODO: Determine with random once we have another template
            templateTextureId = String.Format(templateTextureIdFormat, templateNum);
            string templateTextureAssert = String.Format("Template{0}", templateNum);
            templateTexture = ParentGame.Resources.Get<Texture2D>(templateTextureId, () => ParentGame.Content.Load<Texture2D>(templateTextureAssert));

            // Create the render target texture for 
            const int rttWidth = 512;
            const int rttHeight = 512;

            rtt = RenderTarget2D.New(ParentGame.GraphicsDevice, rttWidth, rttHeight, PixelFormat.R8G8B8A8.UNorm);
            RenderTarget2D oldRtt = ParentGame.GraphicsDevice.BackBuffer;
            ParentGame.GraphicsDevice.SetRenderTargets(rtt);
            ParentGame.GraphicsDevice.Clear(new Color4(0f, 0f, 0f, 0f));
            ParentGame.GraphicsDevice.SetRenderTargets(oldRtt);

            // Load stuff for tagging mode
            paintSprayTexture = ParentGame.Resources.Get<Texture2D>(paintSprayTextureId, () => ParentGame.Content.Load<Texture2D>("PaintSpray"));

            taggingEffect = new BasicEffect(ParentGame.GraphicsDevice)
            {
                World = Matrix.Identity,
                Projection = Matrix.Identity,
                View = Matrix.Identity,
                TextureEnabled = true
            };

            wallTexture = ParentGame.Resources.Get<Texture2D>(wallTextureId, () => ParentGame.Content.Load<Texture2D>("bricks"));
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
            Matrix transform = Matrix.Scaling((float)Math.Max(tileWidth, tileHeight), TagHeight, 1f);
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
                    transform *= Matrix.RotationX(MathF.Pi / 2f);
                    transform *= Matrix.RotationZ(MathF.Pi);
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

            if (IsTagged)
            {
                ParentGame.GraphicsDevice.SetBlendState(ParentGame.GraphicsDevice.BlendStates.NonPremultiplied);
                ParentGame.BasicEffect.Texture = rtt;
                ParentGame.BasicEffect.TextureEnabled = true;
                ParentGame.BasicEffect.LightingEnabled = false;

                mesh.Draw(ParentGame.BasicEffect);

                ParentGame.BasicEffect.LightingEnabled = true;
                ParentGame.BasicEffect.TextureEnabled = false;
                ParentGame.BasicEffect.Texture = null;
                ParentGame.GraphicsDevice.SetBlendState(ParentGame.GraphicsDevice.BlendStates.Default);
            }
            else
            {
                ParentGame.GraphicsDevice.SetBlendState(ParentGame.GraphicsDevice.BlendStates.AlphaBlend);
                ParentGame.GraphicsDevice.SetDepthStencilState(ParentGame.GraphicsDevice.DepthStencilStates.DepthRead);
                Vector4 oldColor = ParentGame.BasicEffect.DiffuseColor;

                // Tagging locations glow more brightly depending on how drunk the player is
                float percentDrunk = ParentGame.Player == null ? 0f : ParentGame.Player.PercentDrunk;
                const float minBlink = 0.5f;
                const float maxBlink = 0.8f;
                const float minAlpha = 0.1f;
                float alpha = minBlink + MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds) * (maxBlink - minBlink);
                alpha *= Math.Min(percentDrunk + minAlpha, 1f);

                ParentGame.BasicEffect.Alpha = alpha;
                ParentGame.BasicEffect.DiffuseColor = new Vector4(1f, 0.85f, 0.5f, 1f); // Glow color
                mesh.Draw(ParentGame.BasicEffect);

                ParentGame.BasicEffect.Alpha = 1f;
                ParentGame.BasicEffect.DiffuseColor = oldColor;
                ParentGame.GraphicsDevice.SetBlendState(ParentGame.GraphicsDevice.BlendStates.Default);
                ParentGame.GraphicsDevice.SetDepthStencilState(ParentGame.GraphicsDevice.DepthStencilStates.Default);
            }
        }

        private Color4[] paintColors =
        {
            Color.White,
            new Color(181, 124, 95), // Brown
            Color.Black,
        };
        private int currentPaintColor;

        private Vector2 lastMousePosition;
        private Vector2 mousePosition;
        private bool isPainting;

        public void RenderTaggingHud(GameTime gameTime)
        {
            ParentGame.GraphicsDevice.SetBlendState(ParentGame.GraphicsDevice.BlendStates.NonPremultiplied);

            //------------------------------------------------------------------
            // Render brick background
            //------------------------------------------------------------------
            taggingEffect.DiffuseColor = new Vector4(0.5f, 0.5f, 0.5f, 1f);
            taggingEffect.Alpha = 0.955f;
            taggingEffect.World = Matrix.Scaling(1f, 2f, 1f);
            taggingEffect.Texture = wallTexture;

            taggingEffect.World *= Matrix.Translation(-0.5f, 0f, 0f);
            mesh.Draw(taggingEffect);
            taggingEffect.World *= Matrix.Translation(1f, 0f, 0f);
            mesh.Draw(taggingEffect);

            //------------------------------------------------------------------
            // Setup effect for rendering the poster
            //------------------------------------------------------------------
            taggingEffect.DiffuseColor = Vector4.One;

            Matrix correctForScreenAspect = Matrix.Scaling(1f / ParentGame.GraphicsDevice.Viewport.AspectRatio, 1f, 1f);

            const float aspectRatio = TypicalTaggingLocationWidth / TagHeight;
            const float prefferredUseScreenHeight = 0.95f;
            float useScreenHeight = prefferredUseScreenHeight;

            // Hack to fix standard aspect ratio monitors
            if (ParentGame.GraphicsDevice.Viewport.AspectRatio < 1.4)
            { useScreenHeight = 0.85f; }

            taggingEffect.World = Matrix.Scaling(useScreenHeight * aspectRatio * 2f, useScreenHeight * 2f, 1f) * correctForScreenAspect;

            //------------------------------------------------------------------
            // Render a light background for tagging area
            //------------------------------------------------------------------
            taggingEffect.Alpha = 0.2f;
            taggingEffect.TextureEnabled = false;
            mesh.Draw(taggingEffect);
            taggingEffect.TextureEnabled = true;

            //------------------------------------------------------------------
            // Render template
            //------------------------------------------------------------------
            taggingEffect.Alpha = 0.5f;
            taggingEffect.Texture = templateTexture;
            mesh.Draw(taggingEffect);

            //------------------------------------------------------------------
            // Render current tagging
            //------------------------------------------------------------------
            taggingEffect.Alpha = 1f;
            taggingEffect.Texture = rtt;
            mesh.Draw(taggingEffect);

            //------------------------------------------------------------------
            // Render the cursor
            //------------------------------------------------------------------
            // Used to make sure cursor scales right with 4:3 monitor hack
            float cursorCorrection = ((useScreenHeight + (prefferredUseScreenHeight - useScreenHeight) / 2f) / prefferredUseScreenHeight);

            float cursorSize = 0.1f;//0.07f;
            cursorSize *= cursorCorrection;
            taggingEffect.DiffuseColor = paintColors[currentPaintColor];
            taggingEffect.Texture = paintSprayTexture;
            taggingEffect.World = Matrix.Scaling(cursorSize, cursorSize, 1f) * correctForScreenAspect;
            taggingEffect.Alpha = 0.75f;

            Vector2 movedMousePosition = mousePosition * 2f - Vector2.One;
            taggingEffect.World *= Matrix.Translation(movedMousePosition.X, -movedMousePosition.Y, 0f);

            mesh.Draw(taggingEffect);

            //------------------------------------------------------------------
            // Spray paint on the poster
            //------------------------------------------------------------------
            if (isPainting)
            {
                RenderTarget2D oldRtt = ParentGame.GraphicsDevice.BackBuffer;
                ParentGame.GraphicsDevice.SetRenderTargets(rtt);
                taggingEffect.Alpha = 1f;
                mesh.Draw(taggingEffect);
                ParentGame.GraphicsDevice.SetRenderTargets(oldRtt);
            }
        }

        public override void Update(GameTime gameTime)
        {
            // All update stuff is relevant to when we are currently being drawn on
            if (ParentGame.TaggingTarget != this)
            { return; }

            // If right was pressed, we cycle to the next color
            if (ParentGame.Mouse.RightButton.Pressed)
            {
                currentPaintColor += 1;
                if (currentPaintColor >= paintColors.Length)
                { currentPaintColor = 0; }
            }

            lastMousePosition = mousePosition;
            mousePosition = new Vector2(ParentGame.Mouse.X, ParentGame.Mouse.Y);
            isPainting = ParentGame.Mouse.LeftButton.Down;

            if (ParentGame.Keyboard.IsKeyReleased(Keys.Enter))
            {
                if (ParentGame.Player != null)
                { ParentGame.AddSpeechBubble(new SpeechBubble(ParentGame, Player.DoneTaggingMessage, ParentGame.Player.Position)); }

                ParentGame.FinishTagging();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed)
            { return; }

            if (disposing)
            {
                taggingEffect.Dispose();
                ParentGame.Resources.Drop(templateTextureId, templateTexture);
                ParentGame.Resources.Drop(meshId, mesh);
            }
        }
    }
}
