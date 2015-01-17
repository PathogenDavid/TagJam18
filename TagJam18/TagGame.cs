#if DEBUG
#define DEBUG_CULLED_OBJECTS
#endif

using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Input;
using SharpDX.Windows;
using System.Collections.Generic;
using System.IO;
using SharpDX.Direct3D11;
using DxSamplerState = SharpDX.Direct3D11.SamplerState;

namespace TagJam18
{
    using SharpDX.Toolkit.Graphics;

    public class TagGame : Game
    {
        private readonly GraphicsDeviceManager graphics;
        private readonly KeyboardManager keyboard;
        private readonly MouseManager mouse;
        private List<Entity> entities = new List<Entity>();
        private Level level;

        public ResourcePool Resources { get; private set; }

        public TagGame()
            : base()
        {
            graphics = new GraphicsDeviceManager(this);
            keyboard = new KeyboardManager(this);
            mouse = new MouseManager(this);

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
            
            base.Initialize();
        }

        internal BasicEffect BasicEffect;

        protected override void LoadContent()
        {
            Resources = new ResourcePool();

            BasicEffect = new BasicEffect(GraphicsDevice)
            {
                Projection = Matrix.PerspectiveFovRH(MathF.Pi / 4f, (float)GraphicsDevice.BackBuffer.Width / (float)GraphicsDevice.BackBuffer.Height, 0.1f, 100f),
                World = Matrix.Identity,
                PreferPerPixelLighting = true,
            };
            BasicEffect.EnableDefaultLighting();

            SamplerStateDescription samplerStateDescription = ((DxSamplerState)BasicEffect.Sampler).Description;
            samplerStateDescription.Filter = Filter.Anisotropic;
            samplerStateDescription.MaximumAnisotropy = 16;
            BasicEffect.Sampler = SamplerState.New(GraphicsDevice, new DxSamplerState(GraphicsDevice, samplerStateDescription));

            level = new Level(this, Path.Combine(Content.RootDirectory, "Level1.tmx"));

            BasicEffect.View = Matrix.LookAtRH(new Vector3(level.Width / 2f, level.Height / 2f, -30f), new Vector3(level.Width / 2, level.Height / 2, 0f), -Vector3.UnitY);

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            BasicEffect.Dispose();

            foreach (Entity entity in entities)
            { entity.Dispose(); }

            Resources.Dispose();

            base.UnloadContent();
        }

        public void AddEntity(Entity entity)
        {
            entities.Add(entity);
            SortEntityRenderOrder();
        }

        public void RemoveEntity(Entity entity)
        {
            entities.Remove(entity);
        }

        public IEnumerable<Entity> GetEntities()
        {
            foreach (Entity entity in entities)
            {
                yield return entity;
            }
        }

        public IEnumerable<T> GetEntities<T>()
            where T : class
        {
            foreach (Entity entity in entities)
            {
                T ret = entity as T;
                if (ret != null)
                {
                    yield return ret;
                }
            }
        }

        public void SortEntityRenderOrder()
        {
            entities.Sort((a, b) => a.RenderOrder.CompareTo(b.RenderOrder));
        }

        protected override void Draw(GameTime gameTime)
        {
#if DEBUG_CULLED_OBJECTS
            GraphicsDevice.SetRasterizerState(gameTime.FrameCount % 2 == 0 ? GraphicsDevice.RasterizerStates.CullBack : GraphicsDevice.RasterizerStates.CullNone);
#endif

            GraphicsDevice.Clear(Color.Black);

            foreach (Entity entity in entities)
            { entity.Render(gameTime); }
            
            base.Draw(gameTime);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = keyboard.GetState();

            if (keyboardState.IsKeyReleased(Keys.Escape))
            { Exit(); }

            foreach (Entity entity in entities)
            { entity.Update(gameTime); }
            
            base.Update(gameTime);
        }
    }
}
