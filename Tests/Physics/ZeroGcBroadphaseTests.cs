using System;
using System.Collections.Generic;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;
using EngineX.Physics.Internal.Broadphase;

namespace EngineX.Physics.Tests
{
    public static class ZeroGcBroadphaseTests
    {
        [Test]
        public static void BulkInsertIsZeroGc()
        {
            var tree = new DynamicAabbTree(4096);
            var rng = new System.Random(123);
            var proxies = new int[1000];
            for (int i = 0; i < 1000; i++)
            {
                FP x = FP.FromInt(rng.Next(-50, 50));
                FP y = FP.FromInt(rng.Next(-50, 50));
                FP z = FP.FromInt(rng.Next(-50, 50));
                proxies[i] = tree.Insert(i,
                    Aabb.FromCenterExtents(new Vector3(x, y, z),
                        new Vector3(FP.Half, FP.Half, FP.Half)));
            }
            for (int i = 0; i < 3; i++)
            {
                tree.Insert(1000 + i,
                    Aabb.FromCenterExtents(Vector3.Zero, Vector3.One));
            }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++)
            {
                tree.Insert(2000 + i,
                    Aabb.FromCenterExtents(
                        new Vector3(FP.FromInt(rng.Next(-50, 50)),
                                    FP.FromInt(rng.Next(-50, 50)),
                                    FP.FromInt(rng.Next(-50, 50))),
                        new Vector3(FP.Half, FP.Half, FP.Half)));
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget,
                $"BulkInsert allocated {allocated} bytes over 100 iterations");
        }

        [Test]
        public static void BulkUpdateIsZeroGc()
        {
            var tree = new DynamicAabbTree(1024);
            var rng = new System.Random(456);
            var proxies = new int[500];
            for (int i = 0; i < 500; i++)
            {
                FP x = FP.FromInt(rng.Next(-50, 50));
                proxies[i] = tree.Insert(i,
                    Aabb.FromCenterExtents(new Vector3(x, FP.Zero, FP.Zero),
                        new Vector3(FP.Half, FP.Half, FP.Half)));
            }
            for (int i = 0; i < 3; i++)
            {
                FP x = FP.FromInt(rng.Next(-50, 50));
                tree.Update(proxies[i],
                    Aabb.FromCenterExtents(new Vector3(x, FP.Zero, FP.Zero),
                        new Vector3(FP.Half, FP.Half, FP.Half)), true);
            }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int iter = 0; iter < 100; iter++)
            {
                for (int i = 0; i < 500; i++)
                {
                    FP x = FP.FromInt(rng.Next(-50, 50));
                    tree.Update(proxies[i],
                        Aabb.FromCenterExtents(new Vector3(x, FP.Zero, FP.Zero),
                            new Vector3(FP.Half, FP.Half, FP.Half)), true);
                }
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget,
                $"BulkUpdate allocated {allocated} bytes over 50000 ops");
        }

        [Test]
        public static void BulkQueryIsZeroGc()
        {
            var tree = new DynamicAabbTree(4096);
            var rng = new System.Random(789);
            for (int i = 0; i < 1000; i++)
            {
                FP x = FP.FromInt(rng.Next(-50, 50));
                FP y = FP.FromInt(rng.Next(-50, 50));
                FP z = FP.FromInt(rng.Next(-50, 50));
                tree.Insert(i,
                    Aabb.FromCenterExtents(new Vector3(x, y, z),
                        new Vector3(FP.Half, FP.Half, FP.Half)));
            }
            var ctx = new List<int>();
            ctx.Capacity = 200;
            for (int i = 0; i < 3; i++)
            {
                ctx.Clear();
                var aabb = Aabb.FromCenterExtents(Vector3.Zero, Vector3.One);
                tree.Query(aabb, ref ctx, CachedCallback);
            }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int iter = 0; iter < 100; iter++)
            {
                ctx.Clear();
                FP x = FP.FromInt(rng.Next(-50, 50));
                var aabb = Aabb.FromCenterExtents(
                    new Vector3(x, FP.Zero, FP.Zero),
                    new Vector3(FP.FromFloat(1.5f), FP.FromFloat(1.5f), FP.FromFloat(1.5f)));
                tree.Query(aabb, ref ctx, CachedCallback);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget,
                $"BulkQuery allocated {allocated} bytes over 100 iterations");
        }

        [Test]
        public static void BulkRemoveIsZeroGc()
        {
            var tree = new DynamicAabbTree(1024);
            var rng = new System.Random(101);
            var proxies = new int[500];
            for (int i = 0; i < 500; i++)
            {
                proxies[i] = tree.Insert(i,
                    Aabb.FromCenterExtents(
                        new Vector3(FP.FromInt(rng.Next(-50, 50)), FP.Zero, FP.Zero),
                        new Vector3(FP.Half, FP.Half, FP.Half)));
            }
            for (int i = 0; i < 3; i++) tree.Remove(proxies[i]);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int iter = 0; iter < 50; iter++)
            {
                tree.Remove(proxies[iter]);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget,
                $"BulkRemove allocated {allocated} bytes");
        }

        [Test]
        public static void MixedBroadphaseOpsIsZeroGc()
        {
            var tree = new DynamicAabbTree(4096);
            var rng = new System.Random(202);
            var proxies = new int[200];
            for (int i = 0; i < 200; i++)
            {
                proxies[i] = tree.Insert(i,
                    Aabb.FromCenterExtents(
                        new Vector3(FP.FromInt(rng.Next(-50, 50)), FP.Zero, FP.Zero),
                        new Vector3(FP.Half, FP.Half, FP.Half)));
            }
            var ctx = new List<int>();
            ctx.Capacity = 200;
            for (int i = 0; i < 3; i++)
            {
                ctx.Clear();
                tree.Query(Aabb.FromCenterExtents(Vector3.Zero, Vector3.One), ref ctx, CachedCallback);
                tree.Update(proxies[i],
                    Aabb.FromCenterExtents(
                        new Vector3(FP.FromInt(rng.Next(-50, 50)), FP.Zero, FP.Zero),
                        new Vector3(FP.Half, FP.Half, FP.Half)), true);
            }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int iter = 0; iter < 100; iter++)
            {
                ctx.Clear();
                tree.Query(Aabb.FromCenterExtents(Vector3.Zero, Vector3.One), ref ctx, CachedCallback);
                for (int i = 0; i < 200; i++)
                {
                    tree.Update(proxies[i],
                        Aabb.FromCenterExtents(
                            new Vector3(FP.FromInt(rng.Next(-50, 50)), FP.Zero, FP.Zero),
                            new Vector3(FP.Half, FP.Half, FP.Half)), true);
                }
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget * 5,
                $"MixedBroadphaseOps allocated {allocated} bytes");
        }

        public const long AllocationBudget = 64;

        public static bool ZeroGcCallback(ref List<int> ctx, int userData)
        {
            ctx.Add(userData);
            return true;
        }

        private static readonly TreeQueryCallback<List<int>> CachedCallback = ZeroGcCallback;
    }
}