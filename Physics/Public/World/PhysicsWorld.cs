using System.Collections.Generic;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics.Internal.Broadphase;
using EngineX.Physics.Internal.Dynamics;
using EngineX.Physics.Internal.NarrowPhase;
using EngineX.Physics.Internal.Solver;

namespace EngineX.Physics
{
    public sealed class PhysicsWorld
    {
        private readonly BodySet _bodies;
        private readonly DynamicAabbTree _tree;
        private readonly Dictionary<BodyHandle, int> _proxyByBody;

        private readonly ContactConstraint[] _contacts;
        private readonly ContactPair[] _contactPairs;
        private readonly int[] _islandParent;
        private readonly Island[] _islands;
        private readonly int[] _islandBodyList;
        private readonly int[] _islandContactList;
        private readonly int[] _islandOrder;
        private List<int> _pairQuery;

        public int BodyCount => _bodies.Count;
        public int BroadphaseLeafCount => _tree.CountLeaves();

        public Vector3 Gravity;
        public FP LinearSleepThreshold = SleepingSystem.DefaultLinearThreshold;
        public FP AngularSleepThreshold = SleepingSystem.DefaultAngularThreshold;
        public int FramesToSleep = SleepingSystem.DefaultFramesToSleep;
        public int VelocityIterations = SequentialImpulses.DefaultVelocityIterations;
        public int PositionIterations = SequentialImpulses.DefaultPositionIterations;
        public FP DefaultRestitution = FP.Zero;
        public FP DefaultFriction = FP.FromFloat(0.3f);

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

            int maxContacts = System.Math.Max(bodyCapacity * 4, 256);
            _contacts = new ContactConstraint[maxContacts];
            _contactPairs = new ContactPair[maxContacts];
            _islandParent = new int[bodyCapacity];
            _islands = new Island[IslandBuilder.MaxIslands];
            _islandBodyList = new int[bodyCapacity];
            _islandContactList = new int[maxContacts];
            _islandOrder = new int[IslandBuilder.MaxIslands];
            _pairQuery = new List<int>(32);
            _pairQuery.Capacity = 32;
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

        public void Step(FP dt)
        {
            ApplyAndIntegrate(dt);
            UpdateBroadphase();
            int contactCount = FindContacts();
            if (contactCount > 0)
            {
                int islandCount = IslandBuilder.Build(
                    _bodies, _contactPairs, contactCount,
                    _islandParent, _islands, _islandBodyList, _islandContactList);
                if (islandCount > 0)
                {
                    SequentialImpulses.PreStep(_bodies, _contacts, contactCount, dt);
                    int orderCount = IslandScheduler.ScheduleBySizeDesc(_islands, islandCount, _islandOrder);
                    for (int v = 0; v < VelocityIterations; v++)
                    {
                        for (int oi = 0; oi < orderCount; oi++)
                        {
                            Island island = _islands[_islandOrder[oi]];
                            SolveIslandVelocity(island);
                        }
                    }
                    for (int p = 0; p < PositionIterations; p++)
                    {
                        for (int oi = 0; oi < orderCount; oi++)
                        {
                            Island island = _islands[_islandOrder[oi]];
                            SolveIslandPosition(island);
                        }
                    }
                }
            }
        }

        private void ApplyAndIntegrate(FP dt)
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
            }
        }

        private void UpdateBroadphase()
        {
            for (int i = 0; i < _bodies.Capacity; i++)
            {
                BodyHandle h = _bodies.GetHandle(i);
                if (!_bodies.IsValid(h)) continue;
                Body body = _bodies.Get(h);
                Aabb aabb = ComputeBodyAabb(body);
                body.Bounds = aabb;
                _bodies.Set(h, body);
                if (_proxyByBody.TryGetValue(h, out int proxy))
                {
                    _tree.Update(proxy, aabb, true);
                }
            }
        }

        private int FindContacts()
        {
            int contactCount = 0;
            int pairCap = _contactPairs.Length;
            for (int i = 0; i < _bodies.Capacity && contactCount < pairCap; i++)
            {
                BodyHandle hA = _bodies.GetHandle(i);
                if (!_bodies.IsValid(hA)) continue;
                Body bodyA = _bodies.Get(hA);

                Aabb aabbA = bodyA.Bounds;
                if (aabbA.Min.X > aabbA.Max.X) continue;

                _pairQuery.Clear();
                _tree.Query(aabbA, ref _pairQuery, PairQueryCallback);

                int idA = hA.Id;
                for (int q = 0; q < _pairQuery.Count && contactCount < pairCap; q++)
                {
                    int idB = _pairQuery[q];
                    if (idB <= idA) continue;
                    BodyHandle hB = _bodies.GetHandle(idB);
                    if (!_bodies.IsValid(hB)) continue;
                    Body bodyB = _bodies.Get(hB);
                    if ((bodyB.Flags & BodyFlags.Static) != 0
                        && (bodyA.Flags & BodyFlags.Static) != 0) continue;

                    OverlapResult overlap = RunNarrowPhase(bodyA, bodyB);
                    if (!overlap.IsOverlapping) continue;

                    Vector3 point = bodyA.Position;
                    if (overlap.Penetration > FP.Zero)
                    {
                        point = bodyB.Position - overlap.Normal * bodyB.Shape.HalfExtents.Y;
                    }
                    _contacts[contactCount] = new ContactConstraint
                    {
                        BodyA = hA,
                        BodyB = hB,
                        Normal = overlap.Normal,
                        Penetration = overlap.Penetration,
                        Point = point,
                        Restitution = DefaultRestitution,
                        Friction = DefaultFriction,
                    };
                    _contactPairs[contactCount] = new ContactPair(hA, hB);
                    contactCount++;
                }
            }
            return contactCount;
        }

        private static OverlapResult RunNarrowPhase(in Body a, in Body b)
        {
            if (a.Shape.Type == ShapeType.Box && b.Shape.Type == ShapeType.Box)
            {
                return BoxBoxContact(a, b);
            }
            if (a.Shape.Type == ShapeType.Sphere && b.Shape.Type == ShapeType.Sphere)
            {
                var sphereA = new Sphere(a.Position, a.Shape.Radius);
                var sphereB = new Sphere(b.Position, b.Shape.Radius);
                return ShapeOverlap.Compute(sphereA, sphereB);
            }
            if (a.Shape.Type == ShapeType.Sphere && b.Shape.Type == ShapeType.Box)
            {
                var sphereA = new Sphere(a.Position, a.Shape.Radius);
                var boxB = new Box(b.Position, b.Shape.HalfExtents, b.Orientation);
                return ShapeOverlap.Compute(sphereA, boxB);
            }
            if (a.Shape.Type == ShapeType.Box && b.Shape.Type == ShapeType.Sphere)
            {
                var boxA = new Box(a.Position, a.Shape.HalfExtents, a.Orientation);
                var sphereB = new Sphere(b.Position, b.Shape.Radius);
                return ShapeOverlap.Compute(boxA, sphereB);
            }
            return OverlapResult.NotOverlapping();
        }

        private static OverlapResult BoxBoxContact(in Body a, in Body b)
        {
            if (a.Shape.HalfExtents.X == FP.Zero && a.Shape.HalfExtents.Y == FP.Zero && a.Shape.HalfExtents.Z == FP.Zero)
                return OverlapResult.NotOverlapping();
            if (b.Shape.HalfExtents.X == FP.Zero && b.Shape.HalfExtents.Y == FP.Zero && b.Shape.HalfExtents.Z == FP.Zero)
                return OverlapResult.NotOverlapping();

            Vector3 aMin = a.Position - a.Shape.HalfExtents;
            Vector3 aMax = a.Position + a.Shape.HalfExtents;
            Vector3 bMin = b.Position - b.Shape.HalfExtents;
            Vector3 bMax = b.Position + b.Shape.HalfExtents;

            FP overlapX = FP.Min(aMax.X, bMax.X) - FP.Max(aMin.X, bMin.X);
            FP overlapY = FP.Min(aMax.Y, bMax.Y) - FP.Max(aMin.Y, bMin.Y);
            FP overlapZ = FP.Min(aMax.Z, bMax.Z) - FP.Max(aMin.Z, bMin.Z);

            if (overlapX <= FP.Zero || overlapY <= FP.Zero || overlapZ <= FP.Zero)
                return OverlapResult.NotOverlapping();

            if (overlapX <= overlapY && overlapX <= overlapZ)
            {
                FP xsign = b.Position.X >= a.Position.X ? FP.One : -FP.One;
                return new OverlapResult(true, new Vector3(xsign, FP.Zero, FP.Zero), overlapX, 0);
            }
            if (overlapY <= overlapZ)
            {
                FP ysign = b.Position.Y >= a.Position.Y ? FP.One : -FP.One;
                return new OverlapResult(true, new Vector3(FP.Zero, ysign, FP.Zero), overlapY, 0);
            }
            FP zsign = b.Position.Z >= a.Position.Z ? FP.One : -FP.One;
            return new OverlapResult(true, new Vector3(FP.Zero, FP.Zero, zsign), overlapZ, 0);
        }

        private void SolveIslandVelocity(Island island)
        {
            int start = island.FirstContactIndex;
            int end = start + island.ContactCount;
            for (int c = start; c < end; c++)
            {
                int idx = _islandContactList[c];
                SequentialImpulses.SolveContactVelocity(_bodies, ref _contacts[idx]);
            }
        }

        private void SolveIslandPosition(Island island)
        {
            int start = island.FirstContactIndex;
            int end = start + island.ContactCount;
            for (int c = start; c < end; c++)
            {
                int idx = _islandContactList[c];
                SequentialImpulses.SolveContactPosition(_bodies, ref _contacts[idx]);
            }
        }

        private static Aabb ComputeBodyAabb(in Body body)
        {
            if (body.Shape.Type == ShapeType.Box)
            {
                return Aabb.FromCenterExtents(body.Position, body.Shape.HalfExtents);
            }
            if (body.Shape.Type == ShapeType.Sphere)
            {
                Vector3 r = new Vector3(body.Shape.Radius, body.Shape.Radius, body.Shape.Radius);
                return Aabb.FromCenterExtents(body.Position, r);
            }
            return Aabb.FromCenterExtents(body.Position, new Vector3(FP.One, FP.One, FP.One));
        }

        private static readonly TreeQueryCallback<List<int>> PairQueryCallback = BroadphasePairCallback;
        private static bool BroadphasePairCallback(ref List<int> ctx, int userData)
        {
            ctx.Add(userData);
            return true;
        }
    }
}