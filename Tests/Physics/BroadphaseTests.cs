using System.Collections.Generic;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;
using EngineX.Physics.Internal.Broadphase;

namespace EngineX.Physics.Tests
{
    public static class BroadphaseTests
    {
        private static Aabb MakeAabb(FP x, FP y, FP z, FP hx, FP hy, FP hz)
        {
            var c = new Vector3(x, y, z);
            var e = new Vector3(hx, hy, hz);
            return Aabb.FromCenterExtents(c, e);
        }

        [Test]
        public static void EmptyTreeHasNoLeaves()
        {
            var tree = new DynamicAabbTree(16);
            TestRunner.AssertEqual(0, tree.CountLeaves());
        }

        [Test]
        public static void SingleInsertThenCountLeaves()
        {
            var tree = new DynamicAabbTree(16);
            int proxy = tree.Insert(42, MakeAabb(FP.Zero, FP.Zero, FP.Zero, FP.One, FP.One, FP.One));
            TestRunner.Assert(proxy >= 0, "insert should return non-negative proxy id");
            TestRunner.AssertEqual(1, tree.CountLeaves());
        }

        [Test]
        public static void MultipleInsertions()
        {
            var tree = new DynamicAabbTree(16);
            for (int i = 0; i < 100; i++)
            {
                tree.Insert(i, MakeAabb(FP.FromInt(i), FP.Zero, FP.Zero, FP.Half, FP.Half, FP.Half));
            }
            TestRunner.AssertEqual(100, tree.CountLeaves());
        }

        [Test]
        public static void RemoveDecreasesCount()
        {
            var tree = new DynamicAabbTree(16);
            int[] proxies = new int[10];
            for (int i = 0; i < 10; i++)
            {
                proxies[i] = tree.Insert(i, MakeAabb(FP.FromInt(i), FP.Zero, FP.Zero, FP.Half, FP.Half, FP.Half));
            }
            TestRunner.AssertEqual(10, tree.CountLeaves());
            tree.Remove(proxies[3]);
            tree.Remove(proxies[7]);
            TestRunner.AssertEqual(8, tree.CountLeaves());
        }

        [Test]
        public static void QueryOverlappingFindsExpectedLeaves()
        {
            var tree = new DynamicAabbTree(16);
            int idA = tree.Insert(1, MakeAabb(FP.Zero, FP.Zero, FP.Zero, FP.One, FP.One, FP.One));
            int idB = tree.Insert(2, MakeAabb(FP.FromInt(5), FP.Zero, FP.Zero, FP.One, FP.One, FP.One));
            int idC = tree.Insert(3, MakeAabb(FP.FromInt(10), FP.Zero, FP.Zero, FP.One, FP.One, FP.One));

            var ctx = new List<int>();
            var aabb = MakeAabb(FP.Zero, FP.Zero, FP.Zero, FP.FromFloat(0.5f), FP.FromFloat(0.5f), FP.FromFloat(0.5f));
            tree.Query(aabb, ref ctx, BroadphaseQueryCallback);
            TestRunner.AssertEqual(1, ctx.Count, "should find exactly one body overlapping origin");
            TestRunner.AssertEqual(1, ctx[0]);
        }

        [Test]
        public static void QueryLargeAabbReturnsAllOverlapping()
        {
            var tree = new DynamicAabbTree(16);
            int idA = tree.Insert(1, MakeAabb(FP.Zero, FP.Zero, FP.Zero, FP.One, FP.One, FP.One));
            int idB = tree.Insert(2, MakeAabb(FP.FromInt(5), FP.Zero, FP.Zero, FP.One, FP.One, FP.One));
            int idC = tree.Insert(3, MakeAabb(FP.FromInt(10), FP.Zero, FP.Zero, FP.One, FP.One, FP.One));

            var ctx = new List<int>();
            var aabb = MakeAabb(FP.FromInt(5), FP.Zero, FP.Zero, FP.FromFloat(0.5f), FP.FromFloat(0.5f), FP.FromFloat(0.5f));
            tree.Query(aabb, ref ctx, BroadphaseQueryCallback);
            TestRunner.Assert(ctx.Count >= 1, $"expected at least one overlap, got {ctx.Count}");
        }

        [Test]
        public static void QueryEmptyTreeReturnsNothing()
        {
            var tree = new DynamicAabbTree(16);
            var ctx = new List<int>();
            tree.Query(MakeAabb(FP.Zero, FP.Zero, FP.Zero, FP.One, FP.One, FP.One), ref ctx, BroadphaseQueryCallback);
            TestRunner.AssertEqual(0, ctx.Count);
        }

        [Test]
        public static void UpdateWithoutMovementSkipsRefit()
        {
            var tree = new DynamicAabbTree(16);
            int id = tree.Insert(1, MakeAabb(FP.Zero, FP.Zero, FP.Zero, FP.One, FP.One, FP.One));
            tree.Update(id, MakeAabb(FP.Zero, FP.Zero, FP.Zero, FP.One, FP.One, FP.One), false);
            TestRunner.AssertEqual(1, tree.CountLeaves());
        }

        [Test]
        public static void UpdateWithForceRefit()
        {
            var tree = new DynamicAabbTree(16);
            int id = tree.Insert(1, MakeAabb(FP.Zero, FP.Zero, FP.Zero, FP.One, FP.One, FP.One));
            tree.Update(id, MakeAabb(FP.FromInt(10), FP.Zero, FP.Zero, FP.One, FP.One, FP.One), true);
            Aabb stored = tree.GetFatAabb(id);
            TestRunner.AssertEqual(FP.FromInt(11), stored.Max.X, "AABB should reflect update");
        }

        [Test]
        public static void InsertRemoveInsertCycle()
        {
            var tree = new DynamicAabbTree(16);
            int id1 = tree.Insert(1, MakeAabb(FP.Zero, FP.Zero, FP.Zero, FP.One, FP.One, FP.One));
            tree.Remove(id1);
            int id2 = tree.Insert(2, MakeAabb(FP.FromInt(5), FP.Zero, FP.Zero, FP.One, FP.One, FP.One));
            TestRunner.AssertEqual(1, tree.CountLeaves());

            var ctx = new List<int>();
            tree.Query(MakeAabb(FP.FromInt(5), FP.Zero, FP.Zero, FP.Half, FP.Half, FP.Half), ref ctx, BroadphaseQueryCallback);
            TestRunner.Assert(ctx.Count >= 1, "should find newly inserted body");
        }

        [Test]
        public static void RandomInsertQueryDelete()
        {
            var tree = new DynamicAabbTree(256);
            var rng = new System.Random(42);
            var proxies = new int[100];
            for (int i = 0; i < 100; i++)
            {
                FP x = FP.FromInt(rng.Next(-10, 10));
                FP y = FP.FromInt(rng.Next(-10, 10));
                FP z = FP.FromInt(rng.Next(-10, 10));
                proxies[i] = tree.Insert(i, MakeAabb(x, y, z, FP.Half, FP.Half, FP.Half));
            }
            TestRunner.AssertEqual(100, tree.CountLeaves());

            for (int q = 0; q < 50; q++)
            {
                FP x = FP.FromInt(rng.Next(-10, 10));
                FP y = FP.FromInt(rng.Next(-10, 10));
                FP z = FP.FromInt(rng.Next(-10, 10));
                var ctx = new List<int>();
                tree.Query(MakeAabb(x, y, z, FP.FromFloat(0.6f), FP.FromFloat(0.6f), FP.FromFloat(0.6f)),
                    ref ctx, BroadphaseQueryCallback);
                foreach (int userData in ctx)
                {
                    TestRunner.Assert(userData >= 0 && userData < 100, $"unexpected userData {userData}");
                }
            }

            for (int i = 0; i < 30; i++)
            {
                tree.Remove(proxies[i]);
            }
            TestRunner.AssertEqual(70, tree.CountLeaves());
        }

        [Test]
        public static void GrowthWhenCapacityExceeded()
        {
            var tree = new DynamicAabbTree(4);
            for (int i = 0; i < 20; i++)
            {
                int proxy = tree.Insert(i, MakeAabb(FP.FromInt(i), FP.Zero, FP.Zero, FP.Half, FP.Half, FP.Half));
                TestRunner.Assert(proxy >= 0, $"insert {i} should succeed");
            }
            TestRunner.Assert(tree.Capacity >= 20, $"capacity should have grown, got {tree.Capacity}");
            TestRunner.AssertEqual(20, tree.CountLeaves());
        }

        public static bool BroadphaseQueryCallback(ref List<int> ctx, int userData)
        {
            ctx.Add(userData);
            return true;
        }
    }
}