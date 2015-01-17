using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TagJam18
{
    public class ResourcePool : IDisposable
    {
        class PoolValue
        {
            WeakReference Reference { get; private set; }
            int ReferenceCount { get; private set; }
            int RessurectionCount { get; private set; }

            PoolValue(object obj)
            {
                Reference = new WeakReference(obj);
                ReferenceCount = 1;
                RessurectionCount = 0;
            }

            void Ressurect(object newObj)
            {
                Debug.Assert(!Reference.IsAlive);
                Reference.Target = newObj;
                ReferenceCount = 1;
                RessurectionCount++;
            }

            void GrabReference()
            {
                ReferenceCount++;
            }

            void DropReference()
            {
                if (ReferenceCount == 0)
                {
                    throw new InvalidOperationException("Tried to drop a reference from an object with no references!");
                }

                ReferenceCount--;
            }
        }

        private Dictionary<string, WeakReference> pool = new Dictionary<string, WeakReference>();
        private object poolMutex;

        public delegate T ObjectCreator<T>();

        public T Get<T>(string id, ObjectCreator<T> creator)
            where T : class
        {
            WeakReference retReference;
            lock (poolMutex)
            {
                if (pool.TryGetValue(id, out retReference))
                {
                    object retObject = retReference.Target;
                    T ret = retObject as T;

                    if (retObject == null) // Target was lost to garbage collection
                    {
                        Debug.Print("{1}:'{0}' was lost to garbage collection, recreating...", id, typeof(T));
                        ret = creator();
                        retReference.Target = ret;
                    }
                    else if (ret == null) // ret being null means the id belongs to another type
                    {
                        throw new ArgumentException(String.Format("The given ID is already being used for a {0}, you tried to get a {1}.", retReference.GetType(), typeof(T)), "id");
                    }

                    return ret;
                }
                else // No object by this name exists in the pool
                {
                    T ret = creator();
                    pool.Add(id, new WeakReference(ret));
                    return ret;
                }
            }
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
                foreach (WeakReference weakReference in pool.Values)
                {
                    object obj = weakReference.Target;
                    IDisposable disposable = obj as IDisposable;

                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        ~ResourcePool()
        {
            Dispose(false);
        }
    }
}
