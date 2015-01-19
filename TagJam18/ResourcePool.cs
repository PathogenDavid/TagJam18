using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TagJam18
{
    public class ResourcePool : IDisposable
    {
        /// <remarks>
        /// This class is probably not necessary as I found out after I made it that Toolkit does its own caching internally.
        /// Whoops!
        /// On the bright side, this is good for things that don't come from TagGame.Content, like GometricPrimitive instances.
        /// </remarks>
        class PoolValue
        {
            private WeakReference weakReference;
            public int ReferenceCount { get; private set; }
            public int RessurectionCount { get; private set; }
            public Type ObjectType { get; private set; }
            bool wasDisposed = false;

            public object Reference { get { return wasDisposed ? null : weakReference.Target; } }

            public PoolValue(object obj)
            {
                weakReference = new WeakReference(obj);
                ReferenceCount = 1;
                RessurectionCount = 0;
                ObjectType = obj.GetType();
            }

            public void Ressurect(object newObj)
            {
                if (newObj.GetType() != ObjectType)
                { throw new InvalidOperationException("Attempted to ressurect object with a different type!"); }

                if (weakReference.IsAlive && !wasDisposed)
                { throw new InvalidOperationException("Attempted to ressurect an object that wasn't dead!"); }

                wasDisposed = false;
                weakReference.Target = newObj;
                ReferenceCount = 1;
                RessurectionCount++;
            }

            public void GrabReference()
            {
                Debug.Assert(!wasDisposed); // This should never happen to a disposed object.
                ReferenceCount++;
            }

            public void DropReference()
            {
                if (ReferenceCount == 0)
                {
                    throw new InvalidOperationException("Tried to drop a reference from an object with no references!");
                }

                ReferenceCount--;

                if (ReferenceCount == 0)
                {
                    Debug.Assert(!wasDisposed); // This should never happen to a disposed object.
                    IDisposable disposable = weakReference.Target as IDisposable;
                    if (disposable != null)
                    { disposable.Dispose(); }
                    wasDisposed = true;
                }
            }
        }

        private Dictionary<string, PoolValue> pool = new Dictionary<string, PoolValue>();
        private object poolMutex = new Object();

        public delegate T ObjectCreator<T>();

        public T Get<T>(string id, ObjectCreator<T> creator)
            where T : class
        {
            PoolValue poolValue;
            lock (poolMutex)
            {
                if (pool.TryGetValue(id, out poolValue))
                {
                    object retObject = poolValue.Reference;
                    T ret = retObject as T;

                    if (retObject == null) // Target was lost to garbage collection
                    {
                        if (poolValue.ObjectType != typeof(T))
                        { throw new ArgumentException(String.Format("{0} was previously a {1}, but you tried to get a {2}", id, poolValue.ObjectType, typeof(T)), "id"); }

                        Debug.Print("{1}:'{0}' was lost to garbage collection, recreating...", id, typeof(T));

                        if (poolValue.ReferenceCount > 0)
                        { Debug.Print("REFERENCE COUNT LEAK: ResourcePool.Get ressurecting of {0} of type {1}, with a non-zero reference count of {2}", id, typeof(T), poolValue.ReferenceCount); }

                        ret = creator();

                        poolValue.Ressurect(ret);
                    }
                    else if (ret == null) // ret being null means the id belongs to another type
                    {
                        throw new ArgumentException(String.Format("{0} is already being used for a {1}, you tried to get a {2}.", id, retObject.GetType(), typeof(T)), "id");
                    }
                    else
                    {
                        poolValue.GrabReference();
                    }

                    return ret;
                }
                else // No object by this name exists in the pool
                {
                    T ret = creator();
                    pool.Add(id, new PoolValue(ret));
                    return ret;
                }
            }
        }

        public void Drop<T>(string id, T obj)
        {
            PoolValue poolValue;
            lock (poolMutex)
            {
                if (pool.TryGetValue(id, out poolValue))
                {
                    if (poolValue.Reference != (object)obj)
                    {
                        throw new InvalidOperationException("The given id, object pair doesn't match what is in the pool!");
                    }

                    poolValue.DropReference();
                }
                else
                {
                    throw new InvalidOperationException("The given id doesn't exist in the pool!");
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
                foreach (KeyValuePair<string, PoolValue> item in pool)
                {
                    object obj = item.Value.Reference;
                    IDisposable disposable = obj as IDisposable;

                    if (disposable != null)
                    {
                        Debug.Print("RESOURCE LEAK: Disposing of {0} of type {1}! Reference count is {2}.", item.Key, obj.GetType(), item.Value.ReferenceCount);
                        disposable.Dispose();
                    }
                    else if (obj == null && item.Value.ReferenceCount > 0)
                    {
                        Debug.Print("REFERENCE COUNT LEAK: Found {0} of type {1} was garbage collected, but the reference count is still {2}.", item.Key, item.Value.ObjectType, item.Value.ReferenceCount);
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
