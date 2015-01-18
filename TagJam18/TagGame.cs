#if DEBUG
#define DEBUG_CULLED_OBJECTS
#endif

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Input;
using SharpDX.Windows;
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

        public ResourcePool Resources { get; private set; }

        internal BasicEffect BasicEffect;

        public PlayerCamera Camera { get; private set; }
        public Player Player { get; private set; }

        private DepthStencilBuffer defaultDepthBuffer;
        private RenderTarget2D defaultRenderTarget;
        private RenderTarget2D fullScreenRtt;
        private GeometricPrimitive fullScreenQuad;
        private BasicEffect fullScreenRenderEffect;
        private Effect blurEffect;

        public TagGame()
            : base()
        {
            graphics = new GraphicsDeviceManager(this);
            keyboard = new KeyboardManager(this);
            mouse = new MouseManager(this);
            GameSystems.Add(new EffectCompilerSystem(this));

            Content.RootDirectory = "Content";
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
            fullScreenQuad = GeometricPrimitive.Plane.New(GraphicsDevice, 2f, 2f);

            fullScreenRenderEffect = new BasicEffect(GraphicsDevice)
            {
                World = Matrix.Identity,
                Projection = Matrix.Identity,
                View = Matrix.Identity,
                TextureEnabled = true
            };

            blurEffect = Content.Load<Effect>("BlurShader");

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            blurEffect.Dispose();
            fullScreenRenderEffect.Dispose();
            fullScreenQuad.Dispose();
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

#if DEBUG_CULLED_OBJECTS
            GraphicsDevice.SetRasterizerState(gameTime.FrameCount % 2 == 0 ? GraphicsDevice.RasterizerStates.CullBack : GraphicsDevice.RasterizerStates.CullNone);
#endif
            GraphicsDevice.SetRenderTargets(defaultDepthBuffer, fullScreenRtt);
            GraphicsDevice.Clear(Color.Black);

            ProtectEntitiesList();
            foreach (Entity entity in entities)
            { entity.Render(gameTime); }
            EndProtectEntitiesList();

            //GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.AlphaBlend);
            GraphicsDevice.SetRenderTargets(defaultRenderTarget);
            GraphicsDevice.Clear(Color.Red);
            //fullScreenRenderEffect.Alpha = 0.5f;
            //fullScreenRenderEffect.Texture = fullScreenRtt;
            //fullScreenQuad.Draw(fullScreenRenderEffect);
            //GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Default);

            blurEffect.Parameters["RenderTargetTexture"].SetResource(fullScreenRtt);
            blurEffect.Parameters["TextureSampler"].SetResource(GraphicsDevice.SamplerStates.Default);
            blurEffect.Parameters["Center"].SetValue(new Vector2(MathF.Sin(gameTime.TotalGameTime.TotalSeconds), MathF.Cos(gameTime.TotalGameTime.TotalSeconds / -2f)));
            fullScreenQuad.Draw(blurEffect);
            //GraphicsDevice.Quad.Draw(blurEffect, true);
            
            base.Draw(gameTime);
        }

        public KeyboardState Keyboard { get; private set; }

        protected override void Update(GameTime gameTime)
        {
            Keyboard = keyboard.GetState();

            if (Keyboard.IsKeyReleased(Keys.Escape))
            { Exit(); }

            ProtectEntitiesList();
            foreach (Entity entity in entities)
            { entity.Update(gameTime); }
            EndProtectEntitiesList();
            
            base.Update(gameTime);
        }
    }
}
