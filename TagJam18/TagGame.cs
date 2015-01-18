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
using TagJam18.Entities;

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

        public PlayerCamera Camera { get; private set; }
        public Player Player { get; private set; }

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

        private class EntityOperation
        {
            public readonly bool IsAdd;
            public readonly Entity Entity;

            public EntityOperation(Entity entity, bool isAdd)
            {
                this.IsAdd = isAdd;
                this.Entity = entity;
            }
        }

        private int entityListProtectionDepth = 0;
        private List<EntityOperation> pendingEntityOperations = new List<EntityOperation>();
        private void ProtectEntitiesList()
        {
            if (entityListProtectionDepth == 0)
            { Debug.Assert(pendingEntityOperations.Count == 0); }

            entityListProtectionDepth++;
        }

        private void EndProtectEntitiesList()
        {
            if (entityListProtectionDepth == 0)
            { throw new InvalidOperationException("Tried to end entity list protection when it wasn't being protected!"); }

            entityListProtectionDepth--;

            // Apply changes made to the eneities list
            if (entityListProtectionDepth == 0)
            {
                foreach (EntityOperation operation in pendingEntityOperations)
                {
                    if (operation.IsAdd)
                    { AddEntity(operation.Entity); }
                    else
                    { RemoveEntity(operation.Entity); }
                }
                pendingEntityOperations.Clear();
            }
        }

        /// <remarks>Note: If done during an update, entity addition may not be reflected until the end of the update.</remarks>
        public void AddEntity(Entity entity)
        {
            if (entityListProtectionDepth > 0)
            {
                pendingEntityOperations.Add(new EntityOperation(entity, true));
                return;
            }

            entities.Add(entity);
            SortEntityRenderOrder();

            if (Player == null && entity is Player)
            { Player = (Player)entity; }
        }

        /// <remarks>Note: If done during an update, entity removal may not be reflected until the end of the update.</remarks>
        public void RemoveEntity(Entity entity)
        {
            if (entityListProtectionDepth > 0)
            {
                pendingEntityOperations.Add(new EntityOperation(entity, false));
                return;
            }

            entities.Remove(entity);

            if (Player == entity)
            { Player = null; }
        }

        public IEnumerable<Entity> GetEntities()
        {
            ProtectEntitiesList();
            foreach (Entity entity in entities)
            {
                yield return entity;
            }
            EndProtectEntitiesList();
        }

        public IEnumerable<T> GetEntities<T>()
            where T : class
        {
            ProtectEntitiesList();
            foreach (Entity entity in entities)
            {
                T ret = entity as T;
                if (ret != null)
                {
                    yield return ret;
                }
            }
            EndProtectEntitiesList();
        }

        public void SortEntityRenderOrder()
        {
            entities.Sort((a, b) => a.RenderOrder.CompareTo(b.RenderOrder));
        }

        protected override void Draw(GameTime gameTime)
        {
            BasicEffect.View = Camera.ViewTransform;

#if DEBUG_CULLED_OBJECTS
            GraphicsDevice.SetRasterizerState(gameTime.FrameCount % 2 == 0 ? GraphicsDevice.RasterizerStates.CullBack : GraphicsDevice.RasterizerStates.CullNone);
#endif

            GraphicsDevice.Clear(Color.Black);

            ProtectEntitiesList();
            foreach (Entity entity in entities)
            { entity.Render(gameTime); }
            EndProtectEntitiesList();
            
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
