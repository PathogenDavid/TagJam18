using System;
using SharpDX.Toolkit;
using SharpDX;

namespace TagJam18
{
    public abstract class Entity : IDisposable
    {
        public TagGame ParentGame { get; private set; }

        private int _RenderOrder;
        public int RenderOrder
        {
            get { return _RenderOrder; }
            protected set
            {
                if (_RenderOrder == value)
                { return; }
                _RenderOrder = value;
                ParentGame.SortEntityRenderOrder();
            }
        }

        public Vector3 Position { get; protected set; }
        public float CollisionSize { get; protected set; }

        public Entity(TagGame parentGame)
        {
            ParentGame = parentGame;
            ParentGame.AddEntity(this);
        }
        
        public virtual void Render(GameTime gameTime) { }
        public virtual void Update(GameTime gameTime) { }

        public bool CollidesWith(Entity other)
        {
            const float tunnelingNeeded = 0.05f;

            if (other == null || this.CollisionSize <= float.Epsilon || other.CollisionSize <= float.Epsilon)
            { return false; }

            return (this.Position - other.Position).Length() < (this.CollisionSize + other.CollisionSize - tunnelingNeeded);
        }

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
