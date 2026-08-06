using EngineX.Baseline.FixedPoint;

namespace EngineX.Physics.Internal.Dynamics
{
    internal sealed class BodySet
    {
        private readonly Body[] _bodies;
        private readonly bool[] _alive;
        private readonly int[] _freeStack;
        private int _freeTop;
        private int _count;

        public int Capacity => _bodies.Length;
        public int Count => _count;

        public BodySet(int capacity)
        {
            _bodies = new Body[capacity];
            _alive = new bool[capacity];
            _freeStack = new int[capacity];
            _freeTop = 0;
            for (int i = capacity - 1; i >= 0; i--)
            {
                _freeStack[_freeTop++] = i;
            }
            _count = 0;
        }

        public BodyHandle Add(in Body body)
        {
            if (_freeTop == 0)
            {
                return BodyHandle.Invalid;
            }
            int id = _freeStack[--_freeTop];
            _alive[id] = true;
            Body b = body;
            b.Version = 1;
            _bodies[id] = b;
            _count++;
            return new BodyHandle(id, b.Version);
        }

        public bool Remove(BodyHandle handle)
        {
            if (!IsValid(handle)) return false;
            int id = handle.Id;
            _alive[id] = false;
            _bodies[id].Version++;
            _freeStack[_freeTop++] = id;
            _count--;
            return true;
        }

        public bool TryGet(BodyHandle handle, out Body body)
        {
            if (!IsValid(handle))
            {
                body = default;
                return false;
            }
            body = _bodies[handle.Id];
            return true;
        }

        public Body Get(BodyHandle handle)
        {
            return _bodies[handle.Id];
        }

        public void Set(BodyHandle handle, in Body body)
        {
            _bodies[handle.Id] = body;
        }

        public bool IsValid(BodyHandle handle)
        {
            return handle.Id >= 0
                && handle.Id < _bodies.Length
                && _alive[handle.Id]
                && _bodies[handle.Id].Version == handle.Version;
        }

        public BodyHandle GetHandle(int id)
        {
            if (id < 0 || id >= _bodies.Length || !_alive[id])
                return BodyHandle.Invalid;
            return new BodyHandle(id, _bodies[id].Version);
        }
    }
}