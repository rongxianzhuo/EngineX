using System;
using EngineX.Jobs;

namespace EngineX.ECS
{
    internal interface IComponentArray
    {
        void SetDefault(int index);

        void CopyTo(int source, IComponentArray target, int targetIndex);

        void Dispose();
    }

    internal sealed class ComponentArray<T> : IComponentArray where T : unmanaged, IComponentData
    {
        private NativeArray<T> _data;

        public ComponentArray(int capacity, Allocator allocator)
        {
            _data = new NativeArray<T>(capacity, allocator);
        }

        public NativeArray<T> Data => _data;

        public NativeArray<T> GetView() => _data.GetView();

        public ref T GetRef(int index)
        {
            return ref _data.GetRef(index);
        }

        public T Get(int index)
        {
            return _data[index];
        }

        public void Set(int index, T value)
        {
            _data[index] = value;
        }

        public void SetDefault(int index)
        {
            _data[index] = default;
        }

        public void CopyTo(int source, IComponentArray target, int targetIndex)
        {
            var typed = (ComponentArray<T>)target;
            typed._data[targetIndex] = _data[source];
        }

        public void Dispose()
        {
            _data.Dispose();
        }
    }

    internal static class ComponentType<T> where T : struct, IComponentData
    {
        public static readonly int Index = ComponentTypeRegistry.Register(typeof(T));
    }

    internal static class ComponentTypeRegistry
    {
        private static readonly System.Collections.Generic.Dictionary<Type, int> IndexByType = new System.Collections.Generic.Dictionary<Type, int>();
        private static readonly System.Collections.Generic.List<Type> Types = new System.Collections.Generic.List<Type>();

        public static int Register(Type type)
        {
            if (IndexByType.TryGetValue(type, out int index))
            {
                return index;
            }
            index = Types.Count;
            IndexByType[type] = index;
            Types.Add(type);
            return index;
        }

        public static int GetIndex(Type type)
        {
            return IndexByType.TryGetValue(type, out int index) ? index : -1;
        }

        public static Type GetType(int index)
        {
            return Types[index];
        }
    }
}
