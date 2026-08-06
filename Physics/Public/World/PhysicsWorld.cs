using System.Collections.Generic;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics.Internal.Broadphase;
using EngineX.Physics.Internal.Dynamics;

namespace EngineX.Physics
{
    public sealed class PhysicsWorld
    {
        private readonly BodySet _bodies;
        private readonly DynamicAabbTree _tree;
        private readonly Dictionary<BodyHandle, int> _proxyByBody;

        public int BodyCount => _bodies.Count;

        public Vector3 Gravity;
        public FP LinearSleepThreshold = SleepingSystem.DefaultLinearThreshold;
        public FP AngularSleepThreshold = SleepingSystem.DefaultAngularThreshold;
        public int FramesToSleep = SleepingSystem.DefaultFramesToSleep;

        public PhysicsWorld(int bodyCapacity)
            : this(bodyCapacity, new Vector3(FP.Zero, -FP.FromInt(10), FP.Zero))
        {
        }

        public PhysicsWorld(int bodyCapacity, Vector3 gravity)
        {
            _bodies = new BodySet(bodyCapacity);
            _tree = new DynamicAabbTree(System.Math.Max(bodyCapacity * 2, 64));
            _proxyByBody = new Dictionary<BodyHandle, int>(bodyCapacity);
            Gravity = gravity;
        }

        public BodyHandle AddBody(in Body body)
        {
            BodyHandle h = _bodies.Add(body);
            if (!h.IsValid) return BodyHandle.Invalid;
            int proxy = _tree.Insert(h.Id, ComputeBodyAabb(body));
            _proxyByBody[h] = proxy;
            return h;
        }

        public bool RemoveBody(BodyHandle h)
        {
            if (!_bodies.IsValid(h)) return false;
            if (_proxyByBody.TryGetValue(h, out int proxy))
            {
                _tree.Remove(proxy);
                _proxyByBody.Remove(h);
            }
            _bodies.Remove(h);
            return true;
        }

        public bool TryGetBody(BodyHandle h, out Body body)
        {
            return _bodies.TryGet(h, out body);
        }

        public int BroadphaseLeafCount => _tree.CountLeaves();

        public void Step(FP dt)
        {
            for (int i = 0; i < _bodies.Capacity; i++)
            {
                BodyHandle h = _bodies.GetHandle(i);
                if (!_bodies.IsValid(h)) continue;
                Body body = _bodies.Get(h);

                if ((body.Flags & BodyFlags.Static) != 0) continue;
                if ((body.Flags & BodyFlags.Sleeping) != 0) continue;

                Integrator.ApplyGravity(ref body, Gravity, dt);
                Integrator.Integrate(ref body, dt);
                SleepingSystem.Update(ref body, LinearSleepThreshold, AngularSleepThreshold, FramesToSleep);

                _bodies.Set(h, body);

                if (_proxyByBody.TryGetValue(h, out int proxy))
                {
                    _tree.Update(proxy, ComputeBodyAabb(body), true);
                }
            }
        }

        private static Aabb ComputeBodyAabb(in Body body)
        {
            Vector3 halfExtents = new Vector3(FP.One, FP.One, FP.One);
            return Aabb.FromCenterExtents(body.Position, halfExtents);
        }
    }
}