using System;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;

namespace EngineX.Physics.Tests
{
    public static class ZeroGcDistanceTests
    {
        private const int Iterations = 100;
        private const long AllocationBudget = 64;

        [Test]
        public static void SphereSphereIsZeroGc()
        {
            var a = new Sphere(Vector3.Zero, FP.FromInt(1));
            var b = new Sphere(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), FP.FromInt(1));
            for (int i = 0; i < 3; i++) ShapeDistance.Compute(a, b);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) ShapeDistance.Compute(a, b);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget, $"SphereSphere allocated {allocated} bytes over {Iterations} iterations");
        }

        [Test]
        public static void SphereBoxIsZeroGc()
        {
            var s = new Sphere(new Vector3(FP.FromInt(3), FP.FromInt(4), FP.FromInt(5)), FP.FromInt(1));
            var b = new Box(Vector3.Zero, Vector3.One, Quaternion.AngleAxis(FP.FromInt(30), Vector3.Up));
            for (int i = 0; i < 3; i++) ShapeDistance.Compute(s, b);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) ShapeDistance.Compute(s, b);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget, $"SphereBox allocated {allocated} bytes");
        }

        [Test]
        public static void SphereCapsuleIsZeroGc()
        {
            var s = new Sphere(new Vector3(FP.FromInt(3), FP.FromInt(4), FP.Zero), FP.FromInt(1));
            var c = new Capsule(new Vector3(FP.Zero, FP.Zero, FP.Zero), new Vector3(FP.Zero, FP.FromInt(5), FP.Zero), FP.FromInt(1));
            for (int i = 0; i < 3; i++) ShapeDistance.Compute(s, c);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) ShapeDistance.Compute(s, c);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget, $"SphereCapsule allocated {allocated} bytes");
        }

        [Test]
        public static void SpherePlaneIsZeroGc()
        {
            var s = new Sphere(new Vector3(FP.Zero, FP.FromInt(3), FP.Zero), FP.FromInt(1));
            var p = new Plane(Vector3.Up, FP.Zero);
            for (int i = 0; i < 3; i++) ShapeDistance.Compute(s, p);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) ShapeDistance.Compute(s, p);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget, $"SpherePlane allocated {allocated} bytes");
        }

        [Test]
        public static void BoxBoxIsZeroGc()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromInt(3), FP.FromInt(2), FP.FromInt(1)), Vector3.One, Quaternion.AngleAxis(FP.FromInt(45), Vector3.Up));
            for (int i = 0; i < 3; i++) ShapeDistance.Compute(a, b);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) ShapeDistance.Compute(a, b);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget, $"BoxBox allocated {allocated} bytes");
        }

        [Test]
        public static void BoxCapsuleIsZeroGc()
        {
            var box = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var cap = new Capsule(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), new Vector3(FP.FromInt(5), FP.FromInt(5), FP.Zero), FP.FromInt(1));
            for (int i = 0; i < 3; i++) ShapeDistance.Compute(box, cap);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) ShapeDistance.Compute(box, cap);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget, $"BoxCapsule allocated {allocated} bytes");
        }

        [Test]
        public static void BoxPlaneIsZeroGc()
        {
            var b = new Box(new Vector3(FP.Zero, FP.FromInt(3), FP.Zero), Vector3.One, Quaternion.Identity);
            var p = new Plane(Vector3.Up, FP.Zero);
            for (int i = 0; i < 3; i++) ShapeDistance.Compute(b, p);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) ShapeDistance.Compute(b, p);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget, $"BoxPlane allocated {allocated} bytes");
        }

        [Test]
        public static void CapsuleCapsuleIsZeroGc()
        {
            var a = new Capsule(new Vector3(FP.Zero, FP.Zero, FP.Zero), new Vector3(FP.Zero, FP.FromInt(5), FP.Zero), FP.FromInt(1));
            var b = new Capsule(new Vector3(FP.FromInt(4), FP.FromInt(2), FP.Zero), new Vector3(FP.FromInt(4), FP.FromInt(8), FP.Zero), FP.FromInt(1));
            for (int i = 0; i < 3; i++) ShapeDistance.Compute(a, b);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) ShapeDistance.Compute(a, b);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget, $"CapsuleCapsule allocated {allocated} bytes");
        }

        [Test]
        public static void CapsulePlaneIsZeroGc()
        {
            var c = new Capsule(new Vector3(FP.Zero, FP.FromInt(3), FP.Zero), new Vector3(FP.Zero, FP.FromInt(5), FP.Zero), FP.FromInt(1));
            var p = new Plane(Vector3.Up, FP.Zero);
            for (int i = 0; i < 3; i++) ShapeDistance.Compute(c, p);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) ShapeDistance.Compute(c, p);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget, $"CapsulePlane allocated {allocated} bytes");
        }

        [Test]
        public static void MixedPairsIsZeroGc()
        {
            var sphere = new Sphere(Vector3.Zero, FP.FromInt(1));
            var box = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var cap = new Capsule(Vector3.Zero, Vector3.Up, FP.FromInt(1));
            var plane = new Plane(Vector3.Up, FP.Zero);
            for (int i = 0; i < 3; i++)
            {
                ShapeDistance.Compute(sphere, sphere);
                ShapeDistance.Compute(sphere, box);
                ShapeDistance.Compute(box, box);
                ShapeDistance.Compute(box, cap);
                ShapeDistance.Compute(cap, cap);
                ShapeDistance.Compute(sphere, plane);
                ShapeDistance.Compute(box, plane);
                ShapeDistance.Compute(cap, plane);
            }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++)
            {
                ShapeDistance.Compute(sphere, sphere);
                ShapeDistance.Compute(sphere, box);
                ShapeDistance.Compute(box, box);
                ShapeDistance.Compute(box, cap);
                ShapeDistance.Compute(cap, cap);
                ShapeDistance.Compute(sphere, plane);
                ShapeDistance.Compute(box, plane);
                ShapeDistance.Compute(cap, plane);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget * 8, $"Mixed pairs allocated {allocated} bytes over {Iterations} iters");
        }
    }
}
