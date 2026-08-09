using EngineX.Jobs;

namespace EngineX.ECS
{
    public sealed class Chunk
    {
        public const int Capacity = 128;

        internal Archetype Archetype;

        private NativeArray<Entity> _entities;

        private IComponentArray[] _components;

        public int Count;

        internal Chunk(Archetype archetype)
        {
            Archetype = archetype;
            _entities = new NativeArray<Entity>(Capacity, Allocator.Persistent);
            _components = new IComponentArray[archetype.TypeCount];
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i] = archetype.CreateComponentArray(i, Capacity);
            }
        }

        internal NativeArray<Entity> Entities => _entities;

        internal IComponentArray[] Components => _components;

        internal void SetComponentArray(int componentIndex, IComponentArray array)
        {
            _components[componentIndex] = array;
        }

        public ref Entity GetEntityRef(int index)
        {
            return ref _entities.GetRef(index);
        }

        internal ComponentArray<T> GetComponentArray<T>(int componentIndex) where T : unmanaged, IComponentData
        {
            return (ComponentArray<T>)_components[componentIndex];
        }

        public ref T GetComponentRef<T>(int indexInChunk) where T : unmanaged, IComponentData
        {
            int componentIndex = Archetype.GetComponentIndex(ComponentType<T>.Index);
            var array = (ComponentArray<T>)_components[componentIndex];
            return ref array.GetRef(indexInChunk);
        }

        public NativeArray<T> GetComponentNativeArray<T>() where T : unmanaged, IComponentData
        {
            int componentIndex = Archetype.GetComponentIndex(ComponentType<T>.Index);
            var array = (ComponentArray<T>)_components[componentIndex];
            return array.GetView();
        }

        internal void MoveEntity(int source, int target)
        {
            _entities[target] = _entities[source];
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].CopyTo(source, _components[i], target);
            }
        }

        internal void ClearSlot(int index)
        {
            _entities[index] = default;
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].SetDefault(index);
            }
        }

        internal void Dispose()
        {
            _entities.Dispose();
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].Dispose();
            }
            _components = null;
            Count = 0;
        }
    }
}
