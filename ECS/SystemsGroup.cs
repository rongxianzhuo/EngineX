using System.Collections.Generic;

namespace EngineX.ECS
{
    public sealed class SystemsGroup
    {
        private sealed class Entry
        {
            public ISystem System;

            public SystemState State;
        }

        private readonly List<Entry> _entries = new List<Entry>();

        public int Count => _entries.Count;

        public void Add(ISystem system)
        {
            _entries.Add(new Entry { System = system, State = new SystemState() });
        }

        public void Create(World world)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                entry.State.World = world;
                entry.System.OnCreate(ref entry.State);
            }
        }

        public void Update(World world)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                entry.State.World = world;
                entry.System.OnUpdate(ref entry.State);
            }
        }

        public void Destroy()
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                entry.System.OnDestroy(ref entry.State);
            }
        }
    }
}
