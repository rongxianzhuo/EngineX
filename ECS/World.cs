using System;
using System.Collections.Generic;

namespace EngineX.ECS
{
    public sealed class World : IDisposable
    {
        private const int InitialCapacity = 64;

        private readonly Dictionary<ArchetypeKey, Archetype> _archetypeByKey = new Dictionary<ArchetypeKey, Archetype>();
        private readonly List<Archetype> _archetypes = new List<Archetype>();

        private EntityLocation[] _locations = new EntityLocation[InitialCapacity];
        private int[] _freeList = new int[InitialCapacity];
        private int _freeCount;
        private int _entityCapacity;

        private Archetype _emptyArchetype;

        public World()
        {
            _emptyArchetype = GetOrCreateArchetype(Array.Empty<int>());
        }

        public int EntityCount { get; private set; }

        public int EntityCapacity => _entityCapacity;

        public int ArchetypeCount => _archetypes.Count;

        public Entity CreateEntity()
        {
            int index = AllocateIndex();
            ref var loc = ref _locations[index];
            loc.Version = loc.Version + 1;
            var entity = new Entity(index, loc.Version);

            var chunk = _emptyArchetype.AcquireChunk();
            int slot = chunk.Count;
            chunk.GetEntityRef(slot) = entity;
            chunk.Count++;
            _emptyArchetype.EntityCount++;
            EntityCount++;

            loc.Archetype = _emptyArchetype;
            loc.Chunk = chunk;
            loc.IndexInChunk = slot;
            return entity;
        }

        public void DestroyEntity(Entity entity)
        {
            if (!IsAlive(entity))
            {
                return;
            }
            ref var loc = ref _locations[entity.Index];
            RemoveFromChunk(loc);
            loc.Archetype = null;
            loc.Chunk = null;
            loc.IndexInChunk = 0;
            loc.Version = loc.Version + 1;
            _freeList[_freeCount++] = entity.Index;
            EntityCount--;
        }

        public bool Exists(Entity entity)
        {
            return IsAlive(entity);
        }

        public bool HasComponent<T>(Entity entity) where T : struct, IComponentData
        {
            if (!IsAlive(entity))
            {
                return false;
            }
            int typeIndex = ComponentTypeRegistry.GetIndex(typeof(T));
            if (typeIndex < 0)
            {
                return false;
            }
            return _locations[entity.Index].Archetype.HasComponent(typeIndex);
        }

        public T GetComponent<T>(Entity entity) where T : struct, IComponentData
        {
            ref var loc = ref _locations[entity.Index];
            if (!IsAlive(entity, loc))
            {
                throw new ArgumentException($"Entity {entity} is not alive", nameof(entity));
            }
            int typeIndex = ComponentType<T>.Index;
            if (!loc.Archetype.HasComponent(typeIndex))
            {
                throw new ArgumentException($"Entity {entity} does not have component {typeof(T).Name}", nameof(entity));
            }
            return loc.Chunk.GetComponentRef<T>(loc.IndexInChunk);
        }

        public void SetComponent<T>(Entity entity, T data) where T : struct, IComponentData
        {
            ref var loc = ref _locations[entity.Index];
            if (!IsAlive(entity, loc))
            {
                throw new ArgumentException($"Entity {entity} is not alive", nameof(entity));
            }
            int typeIndex = ComponentType<T>.Index;
            if (!loc.Archetype.HasComponent(typeIndex))
            {
                throw new ArgumentException($"Entity {entity} does not have component {typeof(T).Name}", nameof(entity));
            }
            loc.Chunk.GetComponentRef<T>(loc.IndexInChunk) = data;
        }

        public void AddComponent<T>(Entity entity, T data = default) where T : struct, IComponentData
        {
            ref var loc = ref _locations[entity.Index];
            if (!IsAlive(entity, loc))
            {
                throw new ArgumentException($"Entity {entity} is not alive", nameof(entity));
            }
            int typeIndex = ComponentType<T>.Index;
            if (loc.Archetype.HasComponent(typeIndex))
            {
                loc.Chunk.GetComponentRef<T>(loc.IndexInChunk) = data;
                return;
            }
            var target = GetOrCreateArchetype(InsertSorted(loc.Archetype.TypeIndexes, typeIndex));
            MoveEntityToArchetype(entity, target);
            ref var moved = ref _locations[entity.Index];
            moved.Chunk.GetComponentRef<T>(moved.IndexInChunk) = data;
        }

        public void RemoveComponent<T>(Entity entity) where T : struct, IComponentData
        {
            if (!IsAlive(entity))
            {
                throw new ArgumentException($"Entity {entity} is not alive", nameof(entity));
            }
            int typeIndex = ComponentType<T>.Index;
            var src = _locations[entity.Index].Archetype;
            if (!src.HasComponent(typeIndex))
            {
                return;
            }
            var target = GetOrCreateArchetype(RemoveSorted(src.TypeIndexes, typeIndex));
            MoveEntityToArchetype(entity, target);
        }

        public QueryBuilder Query()
        {
            return new QueryBuilder(this);
        }

        public EntityQuery Query<T0>() where T0 : struct, IComponentData
        {
            return new QueryBuilder(this).WithAll<T0>().Build();
        }

        public EntityQuery Query<T0, T1>() where T0 : struct, IComponentData where T1 : struct, IComponentData
        {
            return new QueryBuilder(this).WithAll<T0, T1>().Build();
        }

        public EntityQuery Query<T0, T1, T2>() where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData
        {
            return new QueryBuilder(this).WithAll<T0, T1, T2>().Build();
        }

        public EntityQuery Query<T0, T1, T2, T3>() where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData
        {
            return new QueryBuilder(this).WithAll<T0, T1, T2, T3>().Build();
        }

        public EntityQuery Query<T0, T1, T2, T3, T4>() where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData
        {
            return new QueryBuilder(this).WithAll<T0, T1, T2, T3, T4>().Build();
        }

        public void Dispose()
        {
            for (int i = 0; i < _archetypes.Count; i++)
            {
                var archetype = _archetypes[i];
                for (int c = 0; c < archetype.Chunks.Count; c++)
                {
                    archetype.Chunks[c].Dispose();
                }
                archetype.Chunks.Clear();
            }
            _archetypes.Clear();
            _archetypeByKey.Clear();
        }

        internal IReadOnlyList<Archetype> Archetypes => _archetypes;

        internal Archetype GetArchetype(Entity entity)
        {
            return _locations[entity.Index].Archetype;
        }

        private int AllocateIndex()
        {
            if (_freeCount > 0)
            {
                _freeCount--;
                return _freeList[_freeCount];
            }
            if (_entityCapacity == _locations.Length)
            {
                Grow();
            }
            return _entityCapacity++;
        }

        private void Grow()
        {
            int newCapacity = _locations.Length * 2;
            var newLocations = new EntityLocation[newCapacity];
            Array.Copy(_locations, newLocations, _locations.Length);
            _locations = newLocations;
            var newFreeList = new int[newCapacity];
            Array.Copy(_freeList, newFreeList, _freeList.Length);
            _freeList = newFreeList;
        }

        private bool IsAlive(Entity entity)
        {
            return entity.Index >= 0
                && entity.Index < _entityCapacity
                && _locations[entity.Index].Archetype != null
                && _locations[entity.Index].Version == entity.Version;
        }

        private bool IsAlive(Entity entity, in EntityLocation loc)
        {
            return entity.Index >= 0
                && entity.Index < _entityCapacity
                && loc.Archetype != null
                && loc.Version == entity.Version;
        }

        private Archetype GetOrCreateArchetype(int[] typeIndexes)
        {
            var key = new ArchetypeKey(typeIndexes);
            if (_archetypeByKey.TryGetValue(key, out var archetype))
            {
                return archetype;
            }
            archetype = new Archetype(typeIndexes);
            _archetypeByKey[key] = archetype;
            _archetypes.Add(archetype);
            return archetype;
        }

        private void MoveEntityToArchetype(Entity entity, Archetype target)
        {
            ref var loc = ref _locations[entity.Index];
            var src = loc.Archetype;
            var srcChunk = loc.Chunk;
            int srcIdx = loc.IndexInChunk;

            var dstChunk = target.AcquireChunk();
            int dstIdx = dstChunk.Count;
            dstChunk.GetEntityRef(dstIdx) = entity;
            for (int i = 0; i < target.TypeCount; i++)
            {
                int typeIndex = target.TypeIndexes[i];
                var dstArray = dstChunk.Components[i];
                if (src.TryGetComponentIndex(typeIndex, out int srcComponentIndex))
                {
                    srcChunk.Components[srcComponentIndex].CopyTo(srcIdx, dstArray, dstIdx);
                }
                else
                {
                    dstArray.SetDefault(dstIdx);
                }
            }
            dstChunk.Count++;
            target.EntityCount++;

            int last = srcChunk.Count - 1;
            if (last != srcIdx)
            {
                srcChunk.MoveEntity(last, srcIdx);
                var moved = srcChunk.GetEntityRef(srcIdx);
                ref var movedLoc = ref _locations[moved.Index];
                movedLoc.Chunk = srcChunk;
                movedLoc.IndexInChunk = srcIdx;
            }
            srcChunk.ClearSlot(last);
            srcChunk.Count--;
            src.EntityCount--;
            if (srcChunk.Count == 0)
            {
                src.ReleaseEmptyChunk(srcChunk);
            }

            loc.Archetype = target;
            loc.Chunk = dstChunk;
            loc.IndexInChunk = dstIdx;
        }

        private void RemoveFromChunk(in EntityLocation loc)
        {
            var chunk = loc.Chunk;
            var archetype = loc.Archetype;
            int last = chunk.Count - 1;
            if (last != loc.IndexInChunk)
            {
                chunk.MoveEntity(last, loc.IndexInChunk);
                var moved = chunk.GetEntityRef(loc.IndexInChunk);
                ref var movedLoc = ref _locations[moved.Index];
                movedLoc.Chunk = chunk;
                movedLoc.IndexInChunk = loc.IndexInChunk;
            }
            chunk.ClearSlot(last);
            chunk.Count--;
            archetype.EntityCount--;
            if (chunk.Count == 0)
            {
                archetype.ReleaseEmptyChunk(chunk);
            }
        }

        private static int[] InsertSorted(int[] sorted, int value)
        {
            int index = Array.BinarySearch(sorted, value);
            if (index >= 0)
            {
                return sorted;
            }
            index = ~index;
            var result = new int[sorted.Length + 1];
            Array.Copy(sorted, 0, result, 0, index);
            result[index] = value;
            Array.Copy(sorted, index, result, index + 1, sorted.Length - index);
            return result;
        }

        private static int[] RemoveSorted(int[] sorted, int value)
        {
            int index = Array.BinarySearch(sorted, value);
            if (index < 0)
            {
                return sorted;
            }
            var result = new int[sorted.Length - 1];
            Array.Copy(sorted, 0, result, 0, index);
            Array.Copy(sorted, index + 1, result, index, sorted.Length - index - 1);
            return result;
        }
    }
}
