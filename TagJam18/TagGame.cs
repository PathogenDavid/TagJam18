#if DEBUG
#define DEBUG_CULLED_OBJECTS
#endif

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Input;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TagJam18.Entities;
using DxSamplerState = SharpDX.Direct3D11.SamplerState;

namespace TagJam18
{
    using SharpDX.Toolkit.Graphics;

    public partial class TagGame : Game
    {
        private readonly GraphicsDeviceManager graphics;
        private readonly KeyboardManager keyboard;
        private readonly MouseManager mouse;
        private Level level;

        public Random Random { get; private set; }

        public ResourcePool Resources { get; private set; }

        internal BasicEffect BasicEffect;

        public PlayerCamera Camera { get; private set; }
        public Player Player { get; private set; }
        public TaggingLocation TaggingTarget { get; private set; }

        public bool IsPlayerTagging
        {
            get { return TaggingTarget != null; }
        }

        private DepthStencilBuffer defaultDepthBuffer;
        private RenderTarget2D defaultRenderTarget;
        private RenderTarget2D fullScreenRtt;
        private RenderTarget2D hudRtt;
        private GeometricPrimitive fullScreenQuad;
        private BasicEffect fullScreenRenderEffect;
        private Effect blurEffect;

        private SpriteFont spriteFont;
        private SpriteBatch spriteBatch;

        private List<SpeechBubble> speechBubbles = new List<SpeechBubble>();

        public TagGame()
            : base()
        {
            graphics = new GraphicsDeviceManager(this);
            keyboard = new KeyboardManager(this);
            mouse = new MouseManager(this);
            GameSystems.Add(new EffectCompilerSystem(this));

            Content.RootDirectory = "Content";

            this.Random = new Random();
        }

        protected override void Initialize()
        {
            Window.Title = "Pathogen-David's TAG Jam 18 Game";

            // Apply screen settings, we will be fullscreen no-border on the primary display
            int outputNum = Debugger.IsAttached ? 1 : 0;
            if (outputNum >= GraphicsDevice.Adapter.OutputsCount) { outputNum = 0; }
            GraphicsOutput output = GraphicsDevice.Adapter.GetOutputAt(outputNum);
            graphics.PreferredBackBufferWidth = output.DesktopBounds.Width;
            graphics.PreferredBackBufferHeight = output.DesktopBounds.Height;

            RenderForm form = Window.NativeWindow as RenderForm;
            if (form == null) // If we couldn't get the form, use traditional fullscreen.
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break(); // If you allow the program to continue, debugging may be difficult.
                }

                graphics.IsFullScreen = true;
            }
            else
            {
                Window.AllowUserResizing = false;
                form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                form.DesktopBounds = new System.Drawing.Rectangle(output.DesktopBounds.Left, output.DesktopBounds.Top, output.DesktopBounds.Width, output.DesktopBounds.Height);
            }

            graphics.ApplyChanges();

            // Other graphics settings
            GraphicsDevice.SetRasterizerState(GraphicsDevice.RasterizerStates.CullBack);

            // The EffectCompilerSystem allows automatic reloading of shaders in debug builds.
            GameSystems.Add(new EffectCompilerSystem(this));
            
            base.Initialize();
        }

        protected override void LoadContent()
        {
            Resources = new ResourcePool();

            Camera = new PlayerCamera(this);

            BasicEffect = new BasicEffect(GraphicsDevice)
            {
                Projection = Matrix.PerspectiveFovRH(MathF.Pi / 4f, (float)GraphicsDevice.BackBuffer.Width / (float)GraphicsDevice.BackBuffer.Height, 0.1f, 100f),
                World = Matrix.Identity,
                PreferPerPixelLighting = true,
                SpecularPower = 32f
            };
            BasicEffect.EnableDefaultLighting();

            SamplerStateDescription samplerStateDescription = ((DxSamplerState)BasicEffect.Sampler).Description;
            samplerStateDescription.Filter = Filter.Anisotropic;
            samplerStateDescription.MaximumAnisotropy = 16;
            BasicEffect.Sampler = SamplerState.New(GraphicsDevice, new DxSamplerState(GraphicsDevice, samplerStateDescription));

            level = new Level(this, Path.Combine(Content.RootDirectory, "Level1.tmx"));
            Camera.DefaultLookAt = new Vector3(level.Width / 2, level.Height / 2, 0f);

            // Create render targets and related resources
            defaultDepthBuffer = GraphicsDevice.DepthStencilBuffer;
            defaultRenderTarget = GraphicsDevice.BackBuffer;
            fullScreenRtt = RenderTarget2D.New(GraphicsDevice, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight, graphics.PreferredBackBufferFormat);
            hudRtt = RenderTarget2D.New(GraphicsDevice, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight, graphics.PreferredBackBufferFormat);
            fullScreenQuad = GeometricPrimitive.Plane.New(GraphicsDevice, 2f, 2f);

            fullScreenRenderEffect = new BasicEffect(GraphicsDevice)
            {
                World = Matrix.Identity,
                Projection = Matrix.Identity,
                View = Matrix.Identity,
                TextureEnabled = true
            };

            blurEffect = Content.Load<Effect>("BlurShader");

            //Load fonts
            spriteFont = Content.Load<SpriteFont>("LeagueGothic");
            spriteBatch = new SpriteBatch(GraphicsDevice);

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            spriteBatch.Dispose();
            spriteFont.Dispose();

            blurEffect.Dispose();
            fullScreenRenderEffect.Dispose();
            fullScreenQuad.Dispose();
            hudRtt.Dispose();
            fullScreenRtt.Dispose();

            BasicEffect.Dispose();

            foreach (Entity entity in entities)
            { entity.Dispose(); }

            Resources.Dispose();

            base.UnloadContent();
        }

        protected override void Draw(GameTime gameTime)
        {
            BasicEffect.View = Camera.ViewTransform;

            // Render the game
#if DEBUG_CULLED_OBJECTS
            GraphicsDevice.SetRasterizerState(gameTime.FrameCount % 2 == 0 ? GraphicsDevice.RasterizerStates.CullBack : GraphicsDevice.RasterizerStates.CullNone);
#endif
            GraphicsDevice.SetRenderTargets(defaultDepthBuffer, fullScreenRtt);
            GraphicsDevice.Clear(Color.Black);

            ProtectEntitiesList();
            foreach (Entity entity in entities)
            { entity.Render(gameTime); }
            EndProtectEntitiesList();

            //Render the HUD
            GraphicsDevice.SetRenderTargets(hudRtt);
            GraphicsDevice.Clear(new Color4(0f, 0f, 0f, 0f));
            spriteBatch.Begin(SpriteSortMode.Deferred, GraphicsDevice.BlendStates.NonPremultiplied);

            spriteBatch.DrawString(spriteFont, String.Format("Your status: {0}", GetPlayerStatusMessage()), new Vector2(50f, 50f), Color.White);

            if (numTagsFinished > 0)
            { spriteBatch.DrawString(spriteFont, String.Format("Walls defaced: {0}", numTagsFinished), new Vector2(50f, 70f), Color.White); }

            if (Score > 0)
            { spriteBatch.DrawString(spriteFont, String.Format("Total score: {0:#,0}", Score), new Vector2(50f, 90f), Color.White); }

            foreach (SpeechBubble bubble in speechBubbles)
            { bubble.Render(gameTime, spriteBatch); }

            spriteBatch.End();

            if (IsPlayerTagging)
            {
                TaggingTarget.RenderTaggingHud(gameTime, hudRtt);
            }

            // Present the final screen
            GraphicsDevice.SetRenderTargets(defaultRenderTarget);
            GraphicsDevice.Clear(Color.Red);
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Default);
            ApplyScreenEffects(gameTime, fullScreenRtt, 1f);
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.AlphaBlend);
            ApplyScreenEffects(gameTime, hudRtt, 0.05f);
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Default);
            
            base.Draw(gameTime);
        }

        bool drunkEffectOverride = false;
        private void ApplyScreenEffects(GameTime gameTime, RenderTarget2D rtt, float drunkBlurPower = 1f)
        {
            if (Player == null || !Player.IsDrunk || drunkEffectOverride)
            {
                fullScreenRenderEffect.Texture = rtt;
                fullScreenQuad.Draw(fullScreenRenderEffect);
            }
            else
            {
                // Set up the blur effect
                blurEffect.Parameters["RenderTargetTexture"].SetResource(rtt);
                blurEffect.Parameters["TextureSampler"].SetResource(GraphicsDevice.SamplerStates.Default);

                float time = (float)gameTime.TotalGameTime.TotalSeconds;

                float wobbleScale = Player.PercentDrunk * 0.05f * drunkBlurPower;
                Vector2 center = new Vector2(0.5f, 0.5f);
                center += new Vector2(MathF.Sin(time * 2f) * wobbleScale, MathF.Cos(time / -2f) * wobbleScale);
                blurEffect.Parameters["Center"].SetValue(center); // Note: Center is in UV coordinates across the render target

                blurEffect.Parameters["BlurWidth"].SetValue(-0.1f * Player.PercentDrunk * drunkBlurPower);

                Matrix transform = Matrix.Identity;

                const float minForWobble = 0.5f;
                if (Player.PercentDrunk > minForWobble)
                {
                    const float maxRotateAmount = 0.025f;
                    const float maxScaleAmount = 0.025f;
                    float percentWobble = (Player.PercentDrunk - minForWobble) / minForWobble;

                    float wobbleFactor = MathF.Sin(gameTime.TotalGameTime.TotalSeconds);
                    float rotateAmount = maxRotateAmount * percentWobble * wobbleFactor;
                    float scaleAmount = maxScaleAmount * percentWobble * wobbleFactor;

                    transform *= Matrix.Scaling(1f + Math.Abs(scaleAmount));
                    transform *= Matrix.RotationZ(rotateAmount);
                }

                blurEffect.Parameters["Transform"].SetValue(transform);

                // Render the screen
                fullScreenQuad.Draw(blurEffect);
            }
        }

        public KeyboardState Keyboard { get; private set; }
        public MouseState Mouse { get; private set; }

        protected override void Update(GameTime gameTime)
        {
            Keyboard = keyboard.GetState();
            Mouse = mouse.GetState();

            if (!IsPlayerTagging && Keyboard.IsKeyReleased(Keys.Escape))
            { Exit(); }

            if (Keyboard.IsKeyReleased(Keys.N))
            { drunkEffectOverride = ! drunkEffectOverride; }

            ProtectEntitiesList();
            foreach (Entity entity in entities)
            { entity.Update(gameTime); }
            EndProtectEntitiesList();

            foreach (SpeechBubble bubble in speechBubbles)
            { bubble.Update(gameTime); }
            ActuallyRemoveSpeechBubbles();
            
            base.Update(gameTime);
        }

        public string GetPlayerStatusMessage()
        {
            if (Player == null)
            { return "Lost"; }

            if (Player.IsBrave)
            { return "Slewed"; }

            if (Player.BeersDranken >= 2 && !Player.IsDrunk)
            { return "Starting to feel it..."; }

            if (!Player.IsDrunk)
            { return "Sober, afraid of getting caught"; }

            if (Player.PercentDrunk > 0.7f)
            { return "Practically invicible"; }
            else if (Player.PercentDrunk > 0.5f)
            { return "'I'm only a little drunk!'"; }
            else if (Player.PercentDrunk > 0.2f)
            { return "Getting there..."; }
            else
            { return "Tipsy"; }
        }

        public void AddSpeechBubble(SpeechBubble newBubble)
        {
            speechBubbles.Add(newBubble);
        }

        private List<SpeechBubble> deadSpeechBubbles = new List<SpeechBubble>();
        public void RemoveSpeechBubble(SpeechBubble bubble)
        {
            deadSpeechBubbles.Add(bubble);
        }

        private void ActuallyRemoveSpeechBubbles()
        {
            foreach (SpeechBubble bubble in deadSpeechBubbles)
            {
                speechBubbles.Remove(bubble);
            }
            deadSpeechBubbles.Clear();
        }

        public void StartTagging(TaggingLocation taggingLocation)
        {
            if (taggingLocation.IsTagged)
            { throw new InvalidOperationException("Can't start tagging a location that is already tagged!"); }

            TaggingTarget = taggingLocation;
            TaggingTarget.IsTagged = true;
        }

        private int numTagsFinished = 0;
        public void FinishTagging(long score)
        {
            const long scoreForFinishing = 1000000;
            TaggingTarget = null;
            numTagsFinished++;
            Score += score + scoreForFinishing;
        }

        public long Score { get; private set; }
    }
}
