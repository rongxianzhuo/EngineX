using System;
using System.Collections.Generic;

namespace EngineX.ECS
{
    public sealed class EntityCommandBuffer
    {
        private enum Op : byte
        {
            CreateEntity,
            DestroyEntity,
            AddComponent,
            RemoveComponent,
        }

        private readonly List<Op> _ops = new List<Op>();
        private readonly List<Entity> _entities = new List<Entity>();
        private readonly List<int> _typeIndexes = new List<int>();
        private readonly List<int> _valueIndexes = new List<int>();
        private readonly Dictionary<int, IComponentValueStore> _stores = new Dictionary<int, IComponentValueStore>();
        private readonly List<Entity> _created = new List<Entity>();

        private static readonly Dictionary<int, Action<EntityCommandBuffer, World, Entity, int>> AddAppliers = new Dictionary<int, Action<EntityCommandBuffer, World, Entity, int>>();
        private static readonly Dictionary<int, Action<World, Entity>> RemoveAppliers = new Dictionary<int, Action<World, Entity>>();

        public int CommandCount => _ops.Count;

        public Entity CreateEntity()
        {
            _ops.Add(Op.CreateEntity);
            _entities.Add(new Entity(-_created.Count - 1, 0));
            _typeIndexes.Add(0);
            _valueIndexes.Add(0);
            _created.Add(default);
            return new Entity(-_created.Count, 0);
        }

        public void DestroyEntity(Entity entity)
        {
            _ops.Add(Op.DestroyEntity);
            _entities.Add(entity);
            _typeIndexes.Add(0);
            _valueIndexes.Add(0);
        }

        public void AddComponent<T>(Entity entity, T component) where T : struct, IComponentData
        {
            var store = GetStore<T>();
            _ops.Add(Op.AddComponent);
            _entities.Add(entity);
            _typeIndexes.Add(ComponentType<T>.Index);
            _valueIndexes.Add(store.Add(component));
        }

        public void RemoveComponent<T>(Entity entity) where T : struct, IComponentData
        {
            _ops.Add(Op.RemoveComponent);
            _entities.Add(entity);
            _typeIndexes.Add(ComponentType<T>.Index);
            _valueIndexes.Add(0);
        }

        public void Playback(World world)
        {
            for (int i = 0; i < _ops.Count; i++)
            {
                var op = _ops[i];
                switch (op)
                {
                    case Op.CreateEntity:
                    {
                        var entity = world.CreateEntity();
                        _created[-_entities[i].Index - 1] = entity;
                        break;
                    }
                    case Op.DestroyEntity:
                        world.DestroyEntity(Resolve(_entities[i]));
                        break;
                    case Op.AddComponent:
                        GetAddApplier(_typeIndexes[i])(this, world, Resolve(_entities[i]), _valueIndexes[i]);
                        break;
                    case Op.RemoveComponent:
                        GetRemoveApplier(_typeIndexes[i])(world, Resolve(_entities[i]));
                        break;
                }
            }
        }

        public void Clear()
        {
            _ops.Clear();
            _entities.Clear();
            _typeIndexes.Clear();
            _valueIndexes.Clear();
            _created.Clear();
            foreach (var store in _stores.Values)
            {
                store.Clear();
            }
        }

        private Entity Resolve(Entity entity)
        {
            return entity.Index < 0 ? _created[-entity.Index - 1] : entity;
        }

        private ComponentValueStore<T> GetStore<T>() where T : struct, IComponentData
        {
            int typeIndex = ComponentType<T>.Index;
            if (_stores.TryGetValue(typeIndex, out var store))
            {
                return (ComponentValueStore<T>)store;
            }
            store = new ComponentValueStore<T>();
            _stores[typeIndex] = store;
            return (ComponentValueStore<T>)store;
        }

        private static Action<EntityCommandBuffer, World, Entity, int> GetAddApplier(int typeIndex)
        {
            if (!AddAppliers.TryGetValue(typeIndex, out var applier))
            {
                var type = ComponentTypeRegistry.GetType(typeIndex);
                var method = typeof(EntityCommandBuffer)
                    .GetMethod(nameof(ApplyAdd), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                    .MakeGenericMethod(type);
                applier = (Action<EntityCommandBuffer, World, Entity, int>)method.CreateDelegate(typeof(Action<EntityCommandBuffer, World, Entity, int>));
                AddAppliers[typeIndex] = applier;
            }
            return applier;
        }

        private static Action<World, Entity> GetRemoveApplier(int typeIndex)
        {
            if (!RemoveAppliers.TryGetValue(typeIndex, out var applier))
            {
                var type = ComponentTypeRegistry.GetType(typeIndex);
                var method = typeof(EntityCommandBuffer)
                    .GetMethod(nameof(ApplyRemove), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                    .MakeGenericMethod(type);
                applier = (Action<World, Entity>)method.CreateDelegate(typeof(Action<World, Entity>));
                RemoveAppliers[typeIndex] = applier;
            }
            return applier;
        }

        private static void ApplyAdd<T>(EntityCommandBuffer ecb, World world, Entity entity, int valueIndex) where T : unmanaged, IComponentData
        {
            var store = (ComponentValueStore<T>)ecb._stores[ComponentType<T>.Index];
            world.AddComponent(entity, store.Get(valueIndex));
        }

        private static void ApplyRemove<T>(World world, Entity entity) where T : struct, IComponentData
        {
            world.RemoveComponent<T>(entity);
        }
    }

    internal interface IComponentValueStore
    {
        void Clear();
    }

    internal sealed class ComponentValueStore<T> : IComponentValueStore where T : struct, IComponentData
    {
        private readonly List<T> _values = new List<T>();

        public int Add(T value)
        {
            _values.Add(value);
            return _values.Count - 1;
        }

        public T Get(int index)
        {
            return _values[index];
        }

        public void Clear()
        {
            _values.Clear();
        }
    }
}
