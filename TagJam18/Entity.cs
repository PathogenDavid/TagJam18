using System;
using SharpDX.Toolkit;
using SharpDX;

namespace TagJam18
{
    public abstract class Entity : IDisposable
    {
        protected TagGame ParentGame { get; private set; }

        public Entity(TagGame parentGame)
        {
            ParentGame = parentGame;
            ParentGame.AddEntity(this);
        }
        
        public virtual void Render(GameTime gameTime) { }
        public virtual void Update(GameTime gameTime) { }

        public virtual void Remove()
        {
            ParentGame.RemoveEntity(this);
            this.Dispose();
        }

        public bool Disposed { get; private set; }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) { return; }

            if (disposing)
            {
            }
        }

        ~Entity()
        {
            Dispose(false);
        }
    }
}
