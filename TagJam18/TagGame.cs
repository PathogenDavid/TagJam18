using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using SharpDX.Toolkit.Input;
using SharpDX.Windows;
using System.Collections.Generic;
using System.IO;

namespace TagJam18
{
    public class TagGame : Game
    {
        private readonly GraphicsDeviceManager graphics;
        private readonly KeyboardManager keyboard;
        private readonly MouseManager mouse;
        private List<Entity> entities = new List<Entity>();
        private Level level;

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

        internal BasicEffect effect;
        internal GeometricPrimitive cube;
        internal GeometricPrimitive cylinder;
        internal GeometricPrimitive teapot;
        internal GeometricPrimitive torus;

        protected override void LoadContent()
        {
            level = new Level(this, Path.Combine(Content.RootDirectory, "Level1.tmx"));

            effect = new BasicEffect(GraphicsDevice)
            {
                View = Matrix.LookAtRH(new Vector3(level.Width / 2, level.Height / 2, -30f), new Vector3(level.Width / 2, level.Height / 2, 0f), -Vector3.UnitY),
                Projection = Matrix.PerspectiveFovRH(MathF.Pi / 4f, (float)GraphicsDevice.BackBuffer.Width / (float)GraphicsDevice.BackBuffer.Height, 0.1f, 100f),
                World = Matrix.Identity,
                PreferPerPixelLighting = true,
            };
            effect.EnableDefaultLighting();

            cube = GeometricPrimitive.Cube.New(GraphicsDevice);
            cylinder = GeometricPrimitive.Cylinder.New(GraphicsDevice);
            teapot = GeometricPrimitive.Teapot.New(GraphicsDevice);
            torus = GeometricPrimitive.Torus.New(GraphicsDevice);

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            torus.Dispose();
            teapot.Dispose();
            cylinder.Dispose();
            cube.Dispose();
            effect.Dispose();

            base.UnloadContent();
        }

        public void AddEntity(Entity entity)
        {
            entities.Add(entity);
        }

        public void RemoveEntity(Entity entity)
        {
            entities.Remove(entity);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            //effect.World = Matrix.Translation(0f, -1f, 0f);
            //cube.Draw(effect);
            foreach (Entity entity in entities)
            {
                entity.Render(gameTime);
            }
            
            base.Draw(gameTime);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = keyboard.GetState();

            if (keyboardState.IsKeyReleased(Keys.Escape))
            {
                Exit();
            }

            foreach (Entity entity in entities)
            {
                entity.Update(gameTime);
            }
            
            base.Update(gameTime);
        }
    }
}
