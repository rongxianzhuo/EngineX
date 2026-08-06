using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;

namespace EngineX.Physics.Tests
{
    public static class OverlapTests
    {
        [Test]
        public static void OverlapResultIsOverlappingSphereSphere()
        {
            var a = new Sphere(Vector3.Zero, FP.FromInt(1));
            var b = new Sphere(new Vector3(FP.FromFloat(0.5f), FP.Zero, FP.Zero), FP.FromInt(1));
            var r = ShapeOverlap.Compute(a, b);
            TestRunner.Assert(r.IsOverlapping, "should overlap");
        }

        [Test]
        public static void OverlapResultIsNotOverlappingSphereSphere()
        {
            var a = new Sphere(Vector3.Zero, FP.FromInt(1));
            var b = new Sphere(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), FP.FromInt(1));
            var r = ShapeOverlap.Compute(a, b);
            TestRunner.Assert(!r.IsOverlapping, "should not overlap");
        }

        [Test]
        public static void OverlapResultNormalIsUnitVectorForOverlappingBoxes()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromFloat(0.5f), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            var r = ShapeOverlap.Compute(a, b);
            TestRunner.Assert(r.IsOverlapping, "boxes should overlap");
            FP mag = r.Normal.Magnitude;
            TestRunner.Assert((mag - FP.One).Abs() < FP.FromFloat(0.01f),
                $"normal should be unit, got magnitude {mag}");
        }

        [Test]
        public static void OverlapResultPenetrationIsPositiveForOverlap()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromFloat(0.5f), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            var r = ShapeOverlap.Compute(a, b);
            TestRunner.Assert(r.Penetration > FP.Zero, $"penetration should be positive, got {r.Penetration}");
        }

        [Test]
        public static void OverlapResultNormalPointsFromAToB()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromFloat(1.5f), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            var r = ShapeOverlap.Compute(a, b);
            TestRunner.Assert(r.IsOverlapping, "boxes should overlap");
            TestRunner.Assert(r.Normal.X > FP.Zero,
                $"normal should point in +X direction (from A to B), got X={r.Normal.X}");
        }

        [Test]
        public static void ManifoldIsValidForOverlappingBoxes()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromFloat(0.5f), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            var m = ShapeOverlap.ComputeManifold(a, b);
            TestRunner.Assert(m.IsValid, "manifold should be valid for overlap");
            TestRunner.Assert(m.Penetration > FP.Zero, "manifold should have positive penetration");
        }

        [Test]
        public static void ManifoldIsEmptyForSeparatedBoxes()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            var m = ShapeOverlap.ComputeManifold(a, b);
            TestRunner.Assert(!m.IsValid, "manifold should be empty for separated shapes");
        }

        [Test]
        public static void OverlapSymmetryForBoxBox()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromFloat(1.5f), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            var rAB = ShapeOverlap.Compute(a, b);
            var rBA = ShapeOverlap.Compute(b, a);
            TestRunner.Assert(rAB.IsOverlapping == rBA.IsOverlapping, "overlap should be symmetric");
            TestRunner.AssertEqual(rAB.Penetration, rBA.Penetration, "penetration should be symmetric");
            TestRunner.Assert((rAB.Normal + rBA.Normal).Magnitude < FP.FromFloat(0.01f),
                "normals should be opposite");
        }

        [Test]
        public static void ConvexHullSphereOverlapping()
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
            var r = ShapeOverlap.Compute(hull, sphere);
            TestRunner.Assert(r.IsOverlapping, "convex hull and sphere at origin should overlap");
        }

        [Test]
        public static void ConvexHullSphereSeparated()
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
            var sphere = new Sphere(new Vector3(FP.FromInt(10), FP.Zero, FP.Zero), FP.FromInt(1));
            var r = ShapeOverlap.Compute(hull, sphere);
            TestRunner.Assert(!r.IsOverlapping, "convex hull and far sphere should not overlap");
        }

        [Test]
        public static void BoxBoxSymmetricPenetration()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromFloat(1.0f), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            var rAB = ShapeOverlap.Compute(a, b);
            var rBA = ShapeOverlap.Compute(b, a);
            TestRunner.AssertEqual(rAB.Penetration, rBA.Penetration, "symmetric penetration depth");
        }
    }
}