using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using SharpDX.Toolkit.Input;
using SharpDX.Windows;

namespace TagJam18
{
    public class TagGame : Game
    {
        private readonly GraphicsDeviceManager graphics;
        private readonly KeyboardManager keyboard;
        private readonly MouseManager mouse;

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

        private BasicEffect effect;
        private GeometricPrimitive cube;

        protected override void LoadContent()
        {
            effect = new BasicEffect(GraphicsDevice)
            {
                View = Matrix.LookAtRH(new Vector3(0f, 0f, 15f), new Vector3(0f), -Vector3.UnitY),
                Projection = Matrix.PerspectiveFovRH((float)Math.PI / 4f, (float)GraphicsDevice.BackBuffer.Width / (float)GraphicsDevice.BackBuffer.Height, 0.1f, 100f),
                World = Matrix.Identity,
                PreferPerPixelLighting = true,
            };
            effect.EnableDefaultLighting();

            cube = GeometricPrimitive.Cube.New(GraphicsDevice);

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            cube.Dispose();
            effect.Dispose();

            base.UnloadContent();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            effect.World = Matrix.Translation(0f, -1f, 0f);
            cube.Draw(effect);
            
            base.Draw(gameTime);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = keyboard.GetState();

            if (keyboardState.IsKeyReleased(Keys.Escape))
            {
                Exit();
            }
            
            base.Update(gameTime);
        }
    }
}
