using System;
using System.Collections.Generic;
using EngineX.Jobs;

namespace EngineX.ECS
{
    internal sealed class Archetype
    {
        private static readonly Allocator ChunkAllocator = Allocator.Persistent;

        public readonly int[] TypeIndexes;

        public readonly List<Chunk> Chunks = new List<Chunk>();

        public int EntityCount;

        private readonly Dictionary<int, int> _componentIndexByType;

        public int TypeCount => TypeIndexes.Length;

        public Archetype(int[] typeIndexes)
        {
            TypeIndexes = typeIndexes;
            _componentIndexByType = new Dictionary<int, int>(typeIndexes.Length);
            for (int i = 0; i < typeIndexes.Length; i++)
            {
                _componentIndexByType[typeIndexes[i]] = i;
            }
        }

        public bool HasComponent(int typeIndex)
        {
            return _componentIndexByType.ContainsKey(typeIndex);
        }

        public int GetComponentIndex(int typeIndex)
        {
            return _componentIndexByType[typeIndex];
        }

        public IComponentArray CreateComponentArray(int componentIndex, int capacity)
        {
            return ComponentArrayFactory.Create(TypeIndexes[componentIndex], capacity, ChunkAllocator);
        }

        public Chunk AcquireChunk()
        {
            if (Chunks.Count > 0)
            {
                var last = Chunks[Chunks.Count - 1];
                if (last.Count < Chunk.Capacity)
                {
                    return last;
                }
            }
            var chunk = new Chunk(this);
            Chunks.Add(chunk);
            return chunk;
        }

        public void ReleaseEmptyChunk(Chunk chunk)
        {
            Chunks.Remove(chunk);
            chunk.Dispose();
        }

        public bool TryGetComponentIndex(int typeIndex, out int componentIndex)
        {
            return _componentIndexByType.TryGetValue(typeIndex, out componentIndex);
        }
    }

    internal static class ComponentArrayFactory
    {
        private static readonly System.Collections.Generic.Dictionary<int, Func<int, Allocator, IComponentArray>> Creators = new System.Collections.Generic.Dictionary<int, Func<int, Allocator, IComponentArray>>();

        public static IComponentArray Create(int typeIndex, int capacity, Allocator allocator)
        {
            if (!Creators.TryGetValue(typeIndex, out var creator))
            {
                var type = ComponentTypeRegistry.GetType(typeIndex);
                var method = typeof(ComponentArrayFactory)
                    .GetMethod(nameof(CreateTyped), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                    .MakeGenericMethod(type);
                creator = (Func<int, Allocator, IComponentArray>)method.CreateDelegate(typeof(Func<int, Allocator, IComponentArray>));
                Creators[typeIndex] = creator;
            }
            return creator(capacity, allocator);
        }

        private static IComponentArray CreateTyped<T>(int capacity, Allocator allocator) where T : struct, IComponentData
        {
            return new ComponentArray<T>(capacity, allocator);
        }
    }

    internal struct ArchetypeKey : IEquatable<ArchetypeKey>
    {
        public readonly int[] TypeIndexes;

        private readonly int _hash;

        public ArchetypeKey(int[] typeIndexes)
        {
            TypeIndexes = typeIndexes;
            var hash = new HashCode();
            for (int i = 0; i < typeIndexes.Length; i++)
            {
                hash.Add(typeIndexes[i]);
            }
            _hash = hash.ToHashCode();
        }

        public bool Equals(ArchetypeKey other)
        {
            var a = TypeIndexes;
            var b = other.TypeIndexes;
            if (a == b) return true;
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is ArchetypeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _hash;
        }
    }
}
