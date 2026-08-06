using System;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;

namespace EngineX.Physics.Tests
{
    public static class ZeroGcNarrowPhaseTests
    {
        private const int Iterations = 100;
        private const long AllocationBudget = 64;

        [Test]
        public static void SphereSphereOverlapIsZeroGc()
        {
            var a = new Sphere(Vector3.Zero, FP.FromInt(1));
            var b = new Sphere(new Vector3(FP.FromFloat(0.5f), FP.Zero, FP.Zero), FP.FromInt(1));
            for (int i = 0; i < 3; i++) ShapeOverlap.Compute(a, b);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) ShapeOverlap.Compute(a, b);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget,
                $"SphereSphere overlap allocated {allocated} bytes over {Iterations} iterations");
        }

        [Test]
        public static void SphereSphereSeparatedIsZeroGc()
        {
            var a = new Sphere(Vector3.Zero, FP.FromInt(1));
            var b = new Sphere(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), FP.FromInt(1));
            for (int i = 0; i < 3; i++) ShapeOverlap.Compute(a, b);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) ShapeOverlap.Compute(a, b);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget,
                $"SphereSphere separated allocated {allocated} bytes");
        }

        [Test]
        public static void BoxBoxOverlapIsZeroGc()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromFloat(1.5f), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            for (int i = 0; i < 3; i++) ShapeOverlap.Compute(a, b);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) ShapeOverlap.Compute(a, b);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget,
                $"BoxBox overlap allocated {allocated} bytes");
        }

        [Test]
        public static void BoxBoxSeparatedIsZeroGc()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            for (int i = 0; i < 3; i++) ShapeOverlap.Compute(a, b);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) ShapeOverlap.Compute(a, b);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget,
                $"BoxBox separated allocated {allocated} bytes");
        }

        [Test]
        public static void ConvexHullSphereOverlapIsZeroGc()
        {
            var verts = new Vector3[]
            {
                new Vector3(FP.FromInt(-2), FP.Zero, FP.Zero),
                new Vector3(FP.FromInt(2), FP.Zero, FP.Zero),
                new Vector3(FP.Zero, FP.FromInt(2), FP.Zero),
                new Vector3(FP.Zero, FP.Zero, FP.FromInt(2)),
                new Vector3(FP.Zero, FP.FromInt(-2), FP.Zero)
            };
            var hull = new ConvexHull(verts);
            var sphere = new Sphere(Vector3.Zero, FP.FromFloat(1.5f));
            for (int i = 0; i < 3; i++) ShapeOverlap.Compute(hull, sphere);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) ShapeOverlap.Compute(hull, sphere);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget,
                $"ConvexHull-Sphere overlap allocated {allocated} bytes");
        }

        [Test]
        public static void ComputeManifoldBoxBoxIsZeroGc()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromFloat(0.5f), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            for (int i = 0; i < 3; i++) ShapeOverlap.ComputeManifold(a, b);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) ShapeOverlap.ComputeManifold(a, b);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget,
                $"ComputeManifold BoxBox allocated {allocated} bytes");
        }

        [Test]
        public static void MixedPairsIsZeroGc()
        {
            var sphere = new Sphere(Vector3.Zero, FP.FromInt(1));
            var box = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var sphereFar = new Sphere(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), FP.FromInt(1));
            var boxFar = new Box(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            for (int i = 0; i < 3; i++)
            {
                ShapeOverlap.Compute(sphere, sphereFar);
                ShapeOverlap.Compute(box, boxFar);
                ShapeOverlap.Compute(sphere, box);
                ShapeOverlap.ComputeManifold(sphere, box);
            }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++)
            {
                ShapeOverlap.Compute(sphere, sphereFar);
                ShapeOverlap.Compute(box, boxFar);
                ShapeOverlap.Compute(sphere, box);
                ShapeOverlap.ComputeManifold(sphere, box);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget * 4,
                $"Mixed narrow phase allocated {allocated} bytes over {Iterations} iters");
        }
    }
}