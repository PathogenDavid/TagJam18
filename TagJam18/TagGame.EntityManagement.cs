using SharpDX.Toolkit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TagJam18.Entities;

namespace TagJam18
{
    public partial class TagGame : Game
    {
        private List<Entity> entities = new List<Entity>();

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
    }
}
