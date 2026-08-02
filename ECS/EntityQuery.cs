using System;
using System.Collections.Generic;
using EngineX.Jobs;

namespace EngineX.ECS
{
    public delegate void EntityForEach<T0>(ref T0 c0) where T0 : struct, IComponentData;

    public delegate void EntityForEach<T0, T1>(ref T0 c0, ref T1 c1) where T0 : struct, IComponentData where T1 : struct, IComponentData;

    public delegate void EntityForEach<T0, T1, T2>(ref T0 c0, ref T1 c1, ref T2 c2) where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData;

    public delegate void EntityForEach<T0, T1, T2, T3>(ref T0 c0, ref T1 c1, ref T2 c2, ref T3 c3) where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData;

    public interface IForEach<T0> where T0 : struct, IComponentData
    {
        void Execute(ref T0 c0);
    }

    public interface IForEach<T0, T1> where T0 : struct, IComponentData where T1 : struct, IComponentData
    {
        void Execute(ref T0 c0, ref T1 c1);
    }

    public interface IForEach<T0, T1, T2> where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData
    {
        void Execute(ref T0 c0, ref T1 c1, ref T2 c2);
    }

    public interface IForEach<T0, T1, T2, T3> where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData
    {
        void Execute(ref T0 c0, ref T1 c1, ref T2 c2, ref T3 c3);
    }

    public interface IForEachChunk
    {
        void Execute(Chunk chunk);
    }

    public sealed class QueryBuilder
    {
        private readonly World _world;
        private readonly List<int> _all = new List<int>();
        private readonly List<int> _none = new List<int>();
        private readonly List<int> _any = new List<int>();

        internal QueryBuilder(World world)
        {
            _world = world;
        }

        public QueryBuilder WithAll<T0>() where T0 : struct, IComponentData
        {
            _all.Add(ComponentType<T0>.Index);
            return this;
        }

        public QueryBuilder WithAll<T0, T1>() where T0 : struct, IComponentData where T1 : struct, IComponentData
        {
            _all.Add(ComponentType<T0>.Index);
            _all.Add(ComponentType<T1>.Index);
            return this;
        }

        public QueryBuilder WithAll<T0, T1, T2>() where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData
        {
            _all.Add(ComponentType<T0>.Index);
            _all.Add(ComponentType<T1>.Index);
            _all.Add(ComponentType<T2>.Index);
            return this;
        }

        public QueryBuilder WithNone<T0>() where T0 : struct, IComponentData
        {
            _none.Add(ComponentType<T0>.Index);
            return this;
        }

        public QueryBuilder WithNone<T0, T1>() where T0 : struct, IComponentData where T1 : struct, IComponentData
        {
            _none.Add(ComponentType<T0>.Index);
            _none.Add(ComponentType<T1>.Index);
            return this;
        }

        public QueryBuilder WithAny<T0>() where T0 : struct, IComponentData
        {
            _any.Add(ComponentType<T0>.Index);
            return this;
        }

        public QueryBuilder WithAny<T0, T1>() where T0 : struct, IComponentData where T1 : struct, IComponentData
        {
            _any.Add(ComponentType<T0>.Index);
            _any.Add(ComponentType<T1>.Index);
            return this;
        }

        public EntityQuery Build()
        {
            return new EntityQuery(_world, _all.ToArray(), _none.ToArray(), _any.ToArray());
        }
    }

    public sealed class EntityQuery
    {
        private readonly World _world;
        private readonly int[] _all;
        private readonly int[] _none;
        private readonly int[] _any;

        internal EntityQuery(World world, int[] all, int[] none, int[] any)
        {
            _world = world;
            _all = all;
            _none = none;
            _any = any;
        }

        public World World => _world;

        public int CalculateEntityCount()
        {
            int count = 0;
            var archetypes = _world.Archetypes;
            for (int i = 0; i < archetypes.Count; i++)
            {
                if (Matches(archetypes[i]))
                {
                    count += archetypes[i].EntityCount;
                }
            }
            return count;
        }

        public int CalculateChunkCount()
        {
            int count = 0;
            var archetypes = _world.Archetypes;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (Matches(archetype))
                {
                    count += archetype.Chunks.Count;
                }
            }
            return count;
        }

        public NativeArray<Entity> ToEntityArray(Allocator allocator)
        {
            int count = CalculateEntityCount();
            var result = new NativeArray<Entity>(count, allocator);
            int k = 0;
            var archetypes = _world.Archetypes;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (!Matches(archetype)) continue;
                for (int c = 0; c < archetype.Chunks.Count; c++)
                {
                    var chunk = archetype.Chunks[c];
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        result[k++] = chunk.GetEntityRef(e);
                    }
                }
            }
            return result;
        }

        public NativeArray<T> ToComponentDataArray<T>(Allocator allocator) where T : struct, IComponentData
        {
            int count = CalculateEntityCount();
            int typeIndex = ComponentType<T>.Index;
            var result = new NativeArray<T>(count, allocator);
            int k = 0;
            var archetypes = _world.Archetypes;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (!Matches(archetype)) continue;
                if (!archetype.HasComponent(typeIndex))
                {
                    throw new ArgumentException($"Query does not include component {typeof(T).Name} in WithAll", nameof(T));
                }
                int componentIndex = archetype.GetComponentIndex(typeIndex);
                for (int c = 0; c < archetype.Chunks.Count; c++)
                {
                    var chunk = archetype.Chunks[c];
                    var array = chunk.GetComponentArray<T>(componentIndex);
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        result[k++] = array.Get(e);
                    }
                }
            }
            return result;
        }

        public void CopyFromComponentDataArray<T>(NativeArray<T> data) where T : struct, IComponentData
        {
            int count = CalculateEntityCount();
            if (data.Length != count)
            {
                throw new ArgumentException($"NativeArray length {data.Length} does not match query entity count {count}", nameof(data));
            }
            int typeIndex = ComponentType<T>.Index;
            int k = 0;
            var archetypes = _world.Archetypes;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (!Matches(archetype)) continue;
                if (!archetype.HasComponent(typeIndex))
                {
                    throw new ArgumentException($"Query does not include component {typeof(T).Name} in WithAll", nameof(T));
                }
                int componentIndex = archetype.GetComponentIndex(typeIndex);
                for (int c = 0; c < archetype.Chunks.Count; c++)
                {
                    var chunk = archetype.Chunks[c];
                    var array = chunk.GetComponentArray<T>(componentIndex);
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        array.Set(e, data[k++]);
                    }
                }
            }
        }

        public NativeArray<ChunkHandle> ToChunkArray(Allocator allocator)
        {
            int count = 0;
            var archetypes = _world.Archetypes;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (Matches(archetype))
                {
                    count += archetype.Chunks.Count;
                }
            }
            var result = new NativeArray<ChunkHandle>(count, allocator);
            int k = 0;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (!Matches(archetype)) continue;
                for (int c = 0; c < archetype.Chunks.Count; c++)
                {
                    result[k++] = new ChunkHandle(archetype.Chunks[c]);
                }
            }
            return result;
        }

        public void ToChunkArray(NativeArray<ChunkHandle> reuse)
        {
            int count = CalculateChunkCount();
            if (reuse.Length < count)
            {
                throw new ArgumentException($"NativeArray length {reuse.Length} is smaller than required chunk count {count}", nameof(reuse));
            }
            int k = 0;
            var archetypes = _world.Archetypes;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (!Matches(archetype)) continue;
                for (int c = 0; c < archetype.Chunks.Count; c++)
                {
                    reuse[k++] = new ChunkHandle(archetype.Chunks[c]);
                }
            }
        }

        public void ForEach<T0>(EntityForEach<T0> action) where T0 : struct, IComponentData
        {
            int typeIndex = ComponentType<T0>.Index;
            var archetypes = _world.Archetypes;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (!Matches(archetype)) continue;
                int componentIndex = archetype.GetComponentIndex(typeIndex);
                for (int c = 0; c < archetype.Chunks.Count; c++)
                {
                    var chunk = archetype.Chunks[c];
                    var array = chunk.GetComponentArray<T0>(componentIndex);
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        action(ref array.GetRef(e));
                    }
                }
            }
        }

        public void ForEach<TVisitor, T0>(ref TVisitor visitor) where TVisitor : struct, IForEach<T0> where T0 : struct, IComponentData
        {
            int typeIndex = ComponentType<T0>.Index;
            var archetypes = _world.Archetypes;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (!Matches(archetype)) continue;
                int componentIndex = archetype.GetComponentIndex(typeIndex);
                for (int c = 0; c < archetype.Chunks.Count; c++)
                {
                    var chunk = archetype.Chunks[c];
                    var array = chunk.GetComponentArray<T0>(componentIndex);
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        visitor.Execute(ref array.GetRef(e));
                    }
                }
            }
        }

        public void ForEach<T0, T1>(EntityForEach<T0, T1> action) where T0 : struct, IComponentData where T1 : struct, IComponentData
        {
            int typeIndex0 = ComponentType<T0>.Index;
            int typeIndex1 = ComponentType<T1>.Index;
            var archetypes = _world.Archetypes;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (!Matches(archetype)) continue;
                int componentIndex0 = archetype.GetComponentIndex(typeIndex0);
                int componentIndex1 = archetype.GetComponentIndex(typeIndex1);
                for (int c = 0; c < archetype.Chunks.Count; c++)
                {
                    var chunk = archetype.Chunks[c];
                    var array0 = chunk.GetComponentArray<T0>(componentIndex0);
                    var array1 = chunk.GetComponentArray<T1>(componentIndex1);
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        action(ref array0.GetRef(e), ref array1.GetRef(e));
                    }
                }
            }
        }

        public void ForEach<TVisitor, T0, T1>(ref TVisitor visitor) where TVisitor : struct, IForEach<T0, T1> where T0 : struct, IComponentData where T1 : struct, IComponentData
        {
            int typeIndex0 = ComponentType<T0>.Index;
            int typeIndex1 = ComponentType<T1>.Index;
            var archetypes = _world.Archetypes;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (!Matches(archetype)) continue;
                int componentIndex0 = archetype.GetComponentIndex(typeIndex0);
                int componentIndex1 = archetype.GetComponentIndex(typeIndex1);
                for (int c = 0; c < archetype.Chunks.Count; c++)
                {
                    var chunk = archetype.Chunks[c];
                    var array0 = chunk.GetComponentArray<T0>(componentIndex0);
                    var array1 = chunk.GetComponentArray<T1>(componentIndex1);
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        visitor.Execute(ref array0.GetRef(e), ref array1.GetRef(e));
                    }
                }
            }
        }

        public void ForEach<T0, T1, T2>(EntityForEach<T0, T1, T2> action) where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData
        {
            int typeIndex0 = ComponentType<T0>.Index;
            int typeIndex1 = ComponentType<T1>.Index;
            int typeIndex2 = ComponentType<T2>.Index;
            var archetypes = _world.Archetypes;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (!Matches(archetype)) continue;
                int componentIndex0 = archetype.GetComponentIndex(typeIndex0);
                int componentIndex1 = archetype.GetComponentIndex(typeIndex1);
                int componentIndex2 = archetype.GetComponentIndex(typeIndex2);
                for (int c = 0; c < archetype.Chunks.Count; c++)
                {
                    var chunk = archetype.Chunks[c];
                    var array0 = chunk.GetComponentArray<T0>(componentIndex0);
                    var array1 = chunk.GetComponentArray<T1>(componentIndex1);
                    var array2 = chunk.GetComponentArray<T2>(componentIndex2);
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        action(ref array0.GetRef(e), ref array1.GetRef(e), ref array2.GetRef(e));
                    }
                }
            }
        }

        public void ForEach<TVisitor, T0, T1, T2>(ref TVisitor visitor) where TVisitor : struct, IForEach<T0, T1, T2> where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData
        {
            int typeIndex0 = ComponentType<T0>.Index;
            int typeIndex1 = ComponentType<T1>.Index;
            int typeIndex2 = ComponentType<T2>.Index;
            var archetypes = _world.Archetypes;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (!Matches(archetype)) continue;
                int componentIndex0 = archetype.GetComponentIndex(typeIndex0);
                int componentIndex1 = archetype.GetComponentIndex(typeIndex1);
                int componentIndex2 = archetype.GetComponentIndex(typeIndex2);
                for (int c = 0; c < archetype.Chunks.Count; c++)
                {
                    var chunk = archetype.Chunks[c];
                    var array0 = chunk.GetComponentArray<T0>(componentIndex0);
                    var array1 = chunk.GetComponentArray<T1>(componentIndex1);
                    var array2 = chunk.GetComponentArray<T2>(componentIndex2);
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        visitor.Execute(ref array0.GetRef(e), ref array1.GetRef(e), ref array2.GetRef(e));
                    }
                }
            }
        }

        public void ForEach<T0, T1, T2, T3>(EntityForEach<T0, T1, T2, T3> action) where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData
        {
            int typeIndex0 = ComponentType<T0>.Index;
            int typeIndex1 = ComponentType<T1>.Index;
            int typeIndex2 = ComponentType<T2>.Index;
            int typeIndex3 = ComponentType<T3>.Index;
            var archetypes = _world.Archetypes;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (!Matches(archetype)) continue;
                int componentIndex0 = archetype.GetComponentIndex(typeIndex0);
                int componentIndex1 = archetype.GetComponentIndex(typeIndex1);
                int componentIndex2 = archetype.GetComponentIndex(typeIndex2);
                int componentIndex3 = archetype.GetComponentIndex(typeIndex3);
                for (int c = 0; c < archetype.Chunks.Count; c++)
                {
                    var chunk = archetype.Chunks[c];
                    var array0 = chunk.GetComponentArray<T0>(componentIndex0);
                    var array1 = chunk.GetComponentArray<T1>(componentIndex1);
                    var array2 = chunk.GetComponentArray<T2>(componentIndex2);
                    var array3 = chunk.GetComponentArray<T3>(componentIndex3);
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        action(ref array0.GetRef(e), ref array1.GetRef(e), ref array2.GetRef(e), ref array3.GetRef(e));
                    }
                }
            }
        }

        public void ForEach<TVisitor, T0, T1, T2, T3>(ref TVisitor visitor) where TVisitor : struct, IForEach<T0, T1, T2, T3> where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData
        {
            int typeIndex0 = ComponentType<T0>.Index;
            int typeIndex1 = ComponentType<T1>.Index;
            int typeIndex2 = ComponentType<T2>.Index;
            int typeIndex3 = ComponentType<T3>.Index;
            var archetypes = _world.Archetypes;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (!Matches(archetype)) continue;
                int componentIndex0 = archetype.GetComponentIndex(typeIndex0);
                int componentIndex1 = archetype.GetComponentIndex(typeIndex1);
                int componentIndex2 = archetype.GetComponentIndex(typeIndex2);
                int componentIndex3 = archetype.GetComponentIndex(typeIndex3);
                for (int c = 0; c < archetype.Chunks.Count; c++)
                {
                    var chunk = archetype.Chunks[c];
                    var array0 = chunk.GetComponentArray<T0>(componentIndex0);
                    var array1 = chunk.GetComponentArray<T1>(componentIndex1);
                    var array2 = chunk.GetComponentArray<T2>(componentIndex2);
                    var array3 = chunk.GetComponentArray<T3>(componentIndex3);
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        visitor.Execute(ref array0.GetRef(e), ref array1.GetRef(e), ref array2.GetRef(e), ref array3.GetRef(e));
                    }
                }
            }
        }

        public void ForEachChunk<TVisitor>(ref TVisitor visitor) where TVisitor : struct, IForEachChunk
        {
            var archetypes = _world.Archetypes;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (!Matches(archetype)) continue;
                for (int c = 0; c < archetype.Chunks.Count; c++)
                {
                    visitor.Execute(archetype.Chunks[c]);
                }
            }
        }

        private bool Matches(Archetype archetype)
        {
            for (int i = 0; i < _all.Length; i++)
            {
                if (!archetype.HasComponent(_all[i]))
                {
                    return false;
                }
            }
            for (int i = 0; i < _none.Length; i++)
            {
                if (archetype.HasComponent(_none[i]))
                {
                    return false;
                }
            }
            if (_any.Length > 0)
            {
                bool any = false;
                for (int i = 0; i < _any.Length; i++)
                {
                    if (archetype.HasComponent(_any[i]))
                    {
                        any = true;
                        break;
                    }
                }
                if (!any)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
