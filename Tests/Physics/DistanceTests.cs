using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;

namespace EngineX.Physics.Tests
{
    public static class DistanceTests
    {
        private static readonly FP FpEps = FP.FromFloat(0.0001f);
        private static readonly FP FpEpsCoarse = FP.FromFloat(0.01f);

        private static void AssertApprox(FP expected, FP actual, string what)
        {
            FP diff = (expected - actual).Abs();
            if (diff > FpEps)
            {
                throw new AssertionException($"{what}: expected {expected}, actual {actual}, diff {diff}");
            }
        }

        private static void AssertApprox(FP expected, FP actual, FP tolerance, string what)
        {
            FP diff = (expected - actual).Abs();
            if (diff > tolerance)
            {
                throw new AssertionException($"{what}: expected {expected}, actual {actual}, diff {diff}");
            }
        }

        private static void AssertApproxVec(Vector3 expected, Vector3 actual, string what)
        {
            AssertApprox(expected.X, actual.X, what + ".X");
            AssertApprox(expected.Y, actual.Y, what + ".Y");
            AssertApprox(expected.Z, actual.Z, what + ".Z");
        }

        private static void AssertOnSphere(Sphere s, Vector3 p, string what)
        {
            FP dist = (p - s.Center).Magnitude;
            AssertApprox(s.Radius, dist, FpEpsCoarse, what);
        }

        private static void AssertOnBoxSurface(Box b, Vector3 p, string what)
        {
            AssertApprox(FP.One, ((p - b.Center).Magnitude / b.HalfExtents.Magnitude), FpEpsCoarse, what);
        }

        private static void AssertUnit(Vector3 v, string what)
        {
            FP mag = v.Magnitude;
            AssertApprox(FP.One, mag, FpEpsCoarse, what);
        }

        [Test]
        public static void SphereSphereSeparatedAlongX()
        {
            var a = new Sphere(new Vector3(FP.Zero, FP.Zero, FP.Zero), FP.FromInt(1));
            var b = new Sphere(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), FP.FromInt(1));
            var r = ShapeDistance.Compute(a, b);
            AssertApprox(FP.FromInt(3), r.Distance, "distance");
            AssertUnit(r.Normal, "normal");
            AssertOnSphere(a, r.PointA, "pointA on a");
            AssertOnSphere(b, r.PointB, "pointB on b");
        }

        [Test]
        public static void SphereSphereSeparatedAtAngle()
        {
            var a = new Sphere(Vector3.Zero, FP.FromInt(1));
            var b = new Sphere(new Vector3(FP.FromInt(3), FP.FromInt(4), FP.Zero), FP.FromInt(1));
            var r = ShapeDistance.Compute(a, b);
            AssertApprox(FP.FromInt(3), r.Distance, "distance (5 - 2 = 3)");
            AssertUnit(r.Normal, "normal");
        }

        [Test]
        public static void SphereSphereTouching()
        {
            var a = new Sphere(Vector3.Zero, FP.FromInt(1));
            var b = new Sphere(new Vector3(FP.FromInt(2), FP.Zero, FP.Zero), FP.FromInt(1));
            var r = ShapeDistance.Compute(a, b);
            TestRunner.AssertEqual(FP.Zero, r.Distance);
            TestRunner.Assert(r.Touching);
        }

        [Test]
        public static void SphereSphereOverlapping()
        {
            var a = new Sphere(Vector3.Zero, FP.FromInt(1));
            var b = new Sphere(new Vector3(FP.FromInt(1), FP.Zero, FP.Zero), FP.FromInt(1));
            var r = ShapeDistance.Compute(a, b);
            TestRunner.AssertEqual(-FP.FromInt(1), r.Distance);
            TestRunner.Assert(r.Overlapping);
        }

        [Test]
        public static void SphereSphereConcentricHasArbitraryNormal()
        {
            var a = new Sphere(Vector3.Zero, FP.FromInt(1));
            var b = new Sphere(Vector3.Zero, FP.FromInt(1));
            var r = ShapeDistance.Compute(a, b);
            TestRunner.AssertEqual(-FP.FromInt(2), r.Distance);
            FP mag = r.Normal.Magnitude;
            TestRunner.Assert(mag > FP.Zero, "concentric should have non-zero normal");
        }

        [Test]
        public static void SpherePlaneAbove()
        {
            var s = new Sphere(new Vector3(FP.Zero, FP.FromInt(3), FP.Zero), FP.FromInt(1));
            var p = new Plane(Vector3.Up, FP.FromInt(0));
            var r = ShapeDistance.Compute(s, p);
            AssertApprox(FP.FromInt(2), r.Distance, "distance");
            AssertUnit(r.Normal, "normal");
        }

        [Test]
        public static void SpherePlaneTouching()
        {
            var s = new Sphere(new Vector3(FP.Zero, FP.FromInt(1), FP.Zero), FP.FromInt(1));
            var p = new Plane(Vector3.Up, FP.FromInt(0));
            var r = ShapeDistance.Compute(s, p);
            TestRunner.AssertEqual(FP.Zero, r.Distance);
        }

        [Test]
        public static void SpherePlaneEmbedded()
        {
            var s = new Sphere(new Vector3(FP.Zero, -FP.FromInt(1), FP.Zero), FP.FromInt(1));
            var p = new Plane(Vector3.Up, FP.FromInt(0));
            var r = ShapeDistance.Compute(s, p);
            AssertApprox(-FP.FromInt(2), r.Distance, "distance");
            TestRunner.Assert(r.Overlapping);
        }

        [Test]
        public static void SpherePlaneAngled()
        {
            var s = new Sphere(new Vector3(FP.FromInt(3), FP.FromInt(4), FP.Zero), FP.FromInt(1));
            var p = new Plane(Vector3.Up, FP.FromInt(0));
            var r = ShapeDistance.Compute(s, p);
            AssertApprox(FP.FromInt(3), r.Distance, "distance (4 - radius 1)");
        }

        [Test]
        public static void SphereBoxAboveAligned()
        {
            var sphere = new Sphere(new Vector3(FP.Zero, FP.FromInt(5), FP.Zero), FP.FromInt(1));
            var box = new Box(Vector3.Zero, new Vector3(FP.FromInt(2), FP.FromInt(1), FP.FromInt(2)), Quaternion.Identity);
            var r = ShapeDistance.Compute(sphere, box);
            AssertApprox(FP.FromInt(3), r.Distance, "distance");
            AssertUnit(r.Normal, "normal");
        }

        [Test]
        public static void SphereBoxTouchingFromSide()
        {
            var sphere = new Sphere(new Vector3(FP.FromInt(2), FP.Zero, FP.Zero), FP.FromInt(1));
            var box = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var r = ShapeDistance.Compute(sphere, box);
            TestRunner.AssertEqual(FP.Zero, r.Distance);
            TestRunner.Assert(r.Touching);
        }

        [Test]
        public static void SphereBoxOverlapping()
        {
            var sphere = new Sphere(new Vector3(FP.FromFloat(1.5f), FP.Zero, FP.Zero), FP.FromInt(1));
            var box = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var r = ShapeDistance.Compute(sphere, box);
            TestRunner.Assert(r.Overlapping);
            TestRunner.Assert(r.Distance < FP.Zero);
        }

        [Test]
        public static void SphereBoxAtCorner()
        {
            var sphere = new Sphere(new Vector3(FP.FromInt(2), FP.FromInt(2), FP.FromInt(2)), FP.FromInt(1));
            var box = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var r = ShapeDistance.Compute(sphere, box);
            FP expected = ((new Vector3(FP.FromInt(2), FP.FromInt(2), FP.FromInt(2))).Magnitude) - FP.FromInt(1) - (FP.FromInt(3)).Sqrt;
            AssertApprox(expected, r.Distance, FpEpsCoarse, "distance");
        }

        [Test]
        public static void SphereBoxCenterInside()
        {
            var sphere = new Sphere(Vector3.Zero, FP.FromFloat(0.5f));
            var box = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var r = ShapeDistance.Compute(sphere, box);
            TestRunner.Assert(r.Overlapping);
            TestRunner.Assert(r.Distance < FP.Zero);
        }

        [Test]
        public static void SphereCapsuleEndOnEnd()
        {
            var s = new Sphere(new Vector3(FP.Zero, FP.FromInt(5), FP.Zero), FP.FromInt(1));
            var cap = new Capsule(new Vector3(FP.Zero, FP.Zero, FP.Zero), new Vector3(FP.Zero, FP.FromInt(2), FP.Zero), FP.FromInt(1));
            var r = ShapeDistance.Compute(s, cap);
            AssertApprox(FP.FromInt(1), r.Distance, "distance");
            AssertUnit(r.Normal, "normal");
        }

        [Test]
        public static void SphereCapsuleParallel()
        {
            var s = new Sphere(new Vector3(FP.FromInt(5), FP.FromInt(1), FP.Zero), FP.FromInt(1));
            var cap = new Capsule(new Vector3(FP.Zero, FP.Zero, FP.Zero), new Vector3(FP.Zero, FP.FromInt(5), FP.Zero), FP.FromInt(1));
            var r = ShapeDistance.Compute(s, cap);
            AssertApprox(FP.FromInt(3), r.Distance, "distance");
        }

        [Test]
        public static void SphereCapsuleOverlapping()
        {
            var s = new Sphere(new Vector3(FP.Zero, FP.FromInt(1), FP.Zero), FP.FromInt(1));
            var cap = new Capsule(new Vector3(FP.Zero, FP.Zero, FP.Zero), new Vector3(FP.Zero, FP.FromInt(2), FP.Zero), FP.FromInt(1));
            var r = ShapeDistance.Compute(s, cap);
            TestRunner.Assert(r.Distance < FP.Zero);
        }

        [Test]
        public static void SphereCapsuleDegenerateAxis()
        {
            var s = new Sphere(new Vector3(FP.FromInt(2), FP.Zero, FP.Zero), FP.FromInt(1));
            var cap = new Capsule(Vector3.Zero, Vector3.Zero, FP.FromInt(1));
            var r = ShapeDistance.Compute(s, cap);
            AssertApprox(FP.Zero, r.Distance, "distance");
        }

        [Test]
        public static void BoxPlaneAbove()
        {
            var box = new Box(new Vector3(FP.Zero, FP.FromInt(2), FP.Zero), Vector3.One, Quaternion.Identity);
            var plane = new Plane(Vector3.Up, FP.FromInt(0));
            var r = ShapeDistance.Compute(box, plane);
            AssertApprox(FP.FromInt(1), r.Distance, "distance (box bottom face at y=1)");
            TestRunner.AssertEqual(Vector3.Up, r.Normal);
        }

        [Test]
        public static void BoxPlaneIntersecting()
        {
            var box = new Box(new Vector3(FP.Zero, FP.Half, FP.Zero), Vector3.One, Quaternion.Identity);
            var plane = new Plane(Vector3.Up, FP.Zero);
            var r = ShapeDistance.Compute(box, plane);
            AssertApprox(-FP.Half, r.Distance, "distance (box bottom face at y=-0.5)");
        }

        [Test]
        public static void BoxPlaneRotated()
        {
            var box = new Box(new Vector3(FP.Zero, FP.FromInt(3), FP.Zero),
                new Vector3(FP.FromInt(2), FP.FromInt(1), FP.FromInt(1)),
                Quaternion.AngleAxis(FP.FromInt(30), Vector3.Up));
            var plane = new Plane(Vector3.Up, FP.Zero);
            var r = ShapeDistance.Compute(box, plane);
            AssertApprox(FP.FromInt(2), r.Distance, FpEpsCoarse, "distance (box bottom face at y=2; Y rotation keeps Y extent)");
        }

        [Test]
        public static void BoxPlaneBelow()
        {
            var box = new Box(new Vector3(FP.Zero, -FP.FromInt(2), FP.Zero), Vector3.One, Quaternion.Identity);
            var plane = new Plane(Vector3.Up, FP.Zero);
            var r = ShapeDistance.Compute(box, plane);
            AssertApprox(-FP.FromInt(1), r.Distance, "distance (box top face at y=-1)");
        }

        [Test]
        public static void CapsulePlaneAbove()
        {
            var cap = new Capsule(new Vector3(FP.Zero, FP.FromInt(3), FP.Zero), new Vector3(FP.Zero, FP.FromInt(5), FP.Zero), FP.FromInt(1));
            var plane = new Plane(Vector3.Up, FP.Zero);
            var r = ShapeDistance.Compute(cap, plane);
            AssertApprox(FP.FromInt(2), r.Distance, "distance");
        }

        [Test]
        public static void CapsulePlaneStraddling()
        {
            var cap = new Capsule(new Vector3(FP.Zero, -FP.FromInt(2), FP.Zero), new Vector3(FP.Zero, FP.FromInt(2), FP.Zero), FP.FromInt(1));
            var plane = new Plane(Vector3.Up, FP.Zero);
            var r = ShapeDistance.Compute(cap, plane);
            AssertApprox(-FP.FromInt(1), r.Distance, "distance");
            TestRunner.Assert(r.Overlapping);
        }

        [Test]
        public static void CapsulePlaneFar()
        {
            var cap = new Capsule(new Vector3(FP.FromInt(0), FP.FromInt(10), FP.Zero), new Vector3(FP.FromInt(0), FP.FromInt(12), FP.Zero), FP.FromInt(1));
            var plane = new Plane(Vector3.Up, FP.Zero);
            var r = ShapeDistance.Compute(cap, plane);
            AssertApprox(FP.FromInt(9), r.Distance, "distance");
        }

        [Test]
        public static void BoxBoxAlignedSeparated()
        {
            var a = new Box(new Vector3(FP.FromInt(0), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            var r = ShapeDistance.Compute(a, b);
            AssertApprox(FP.FromInt(3), r.Distance, "distance");
        }

        [Test]
        public static void BoxBoxAlignedTouching()
        {
            var a = new Box(new Vector3(FP.FromInt(0), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromInt(2), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            var r = ShapeDistance.Compute(a, b);
            TestRunner.AssertEqual(FP.Zero, r.Distance);
        }

        [Test]
        public static void BoxBoxAlignedOverlapping()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromInt(1), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            var r = ShapeDistance.Compute(a, b);
            TestRunner.Assert(r.Overlapping);
            TestRunner.Assert(r.Distance < FP.Zero);
        }

        [Test]
        public static void BoxBoxRotatedSeparated()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromInt(3), FP.FromInt(3), FP.Zero),
                Vector3.One, Quaternion.AngleAxis(FP.FromInt(45), Vector3.Up));
            var r = ShapeDistance.Compute(a, b);
            TestRunner.Assert(r.Distance > FP.Zero, "should be separated");
            TestRunner.Assert(r.Distance < FP.FromInt(3), "should not exceed 3 (loose bound)");
        }

        [Test]
        public static void BoxBoxRotatedTouching()
        {
            var a = new Box(Vector3.Zero, new Vector3(FP.FromInt(1), FP.FromInt(1), FP.FromInt(1)), Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromFloat(2.5f), FP.FromInt(2), FP.Zero),
                new Vector3(FP.FromInt(1), FP.FromInt(1), FP.FromInt(1)),
                Quaternion.AngleAxis(FP.FromInt(45), Vector3.Up));
            var r = ShapeDistance.Compute(a, b);
            TestRunner.Assert(r.Distance > FP.Zero, "should be barely separated");
        }

        [Test]
        public static void BoxBoxDeepOverlap()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.Half, FP.Half, FP.Half), Vector3.One, Quaternion.Identity);
            var r = ShapeDistance.Compute(a, b);
            TestRunner.Assert(r.Overlapping);
        }

        [Test]
        public static void CapsuleCapsuleParallel()
        {
            var a = new Capsule(new Vector3(FP.Zero, FP.Zero, FP.Zero), new Vector3(FP.Zero, FP.FromInt(5), FP.Zero), FP.FromInt(1));
            var b = new Capsule(new Vector3(FP.FromInt(4), FP.FromInt(2), FP.Zero), new Vector3(FP.FromInt(4), FP.FromInt(8), FP.Zero), FP.FromInt(1));
            var r = ShapeDistance.Compute(a, b);
            AssertApprox(FP.FromInt(2), r.Distance, FpEpsCoarse, "distance");
        }

        [Test]
        public static void CapsuleCapsuleCrossing()
        {
            var a = new Capsule(new Vector3(FP.FromInt(-3), FP.Zero, FP.Zero), new Vector3(FP.FromInt(3), FP.Zero, FP.Zero), FP.FromInt(1));
            var b = new Capsule(new Vector3(FP.Zero, FP.FromInt(-3), FP.Zero), new Vector3(FP.Zero, FP.FromInt(3), FP.Zero), FP.FromInt(1));
            var r = ShapeDistance.Compute(a, b);
            AssertApprox(-FP.FromInt(2), r.Distance, FpEpsCoarse, "distance (overlap)");
        }

        [Test]
        public static void CapsuleCapsulePointLikeA()
        {
            var a = new Capsule(new Vector3(FP.FromInt(0), FP.Zero, FP.Zero), new Vector3(FP.FromInt(0), FP.Zero, FP.Zero), FP.FromInt(1));
            var b = new Capsule(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), new Vector3(FP.FromInt(10), FP.Zero, FP.Zero), FP.FromInt(1));
            var r = ShapeDistance.Compute(a, b);
            AssertApprox(FP.FromInt(3), r.Distance, "distance");
        }

        [Test]
        public static void CapsuleCapsulePointLikeBoth()
        {
            var a = new Capsule(new Vector3(FP.FromInt(0), FP.Zero, FP.Zero), new Vector3(FP.FromInt(0), FP.Zero, FP.Zero), FP.FromInt(1));
            var b = new Capsule(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), FP.FromInt(1));
            var r = ShapeDistance.Compute(a, b);
            AssertApprox(FP.FromInt(3), r.Distance, "distance");
        }

        [Test]
        public static void CapsuleCapsuleOverlapping()
        {
            var a = new Capsule(new Vector3(FP.Zero, FP.Zero, FP.Zero), new Vector3(FP.Zero, FP.FromInt(5), FP.Zero), FP.FromInt(1));
            var b = new Capsule(new Vector3(FP.Zero, FP.FromInt(3), FP.Zero), new Vector3(FP.Zero, FP.FromInt(7), FP.Zero), FP.FromInt(1));
            var r = ShapeDistance.Compute(a, b);
            TestRunner.Assert(r.Overlapping);
        }

        [Test]
        public static void BoxCapsuleEndToEnd()
        {
            var box = Box.Identity;
            var cap = new Capsule(new Vector3(FP.Zero, FP.FromInt(5), FP.Zero), new Vector3(FP.Zero, FP.FromInt(10), FP.Zero), FP.FromInt(1));
            var r = ShapeDistance.Compute(box, cap);
            AssertApprox(FP.FromInt(3), r.Distance, FpEpsCoarse, "distance (box top at 1, capsule bottom at 5, so 4 - capsule radius 1 = 3)");
        }

        [Test]
        public static void BoxCapsuleTouching()
        {
            var box = Box.Identity;
            var cap = new Capsule(new Vector3(FP.Zero, FP.FromInt(2), FP.Zero), new Vector3(FP.Zero, FP.FromInt(4), FP.Zero), FP.FromInt(1));
            var r = ShapeDistance.Compute(box, cap);
            TestRunner.Assert(r.Distance.Abs() < FpEpsCoarse, $"touching should be ~0, got {r.Distance}");
        }

        [Test]
        public static void BoxCapsuleOverlapping()
        {
            var box = Box.Identity;
            var cap = new Capsule(new Vector3(FP.Zero, FP.FromInt(1), FP.Zero), new Vector3(FP.Zero, FP.FromInt(3), FP.Zero), FP.FromInt(1));
            var r = ShapeDistance.Compute(box, cap);
            TestRunner.Assert(r.Overlapping);
        }

        [Test]
        public static void BoxCapsuleDegenerateAxis()
        {
            var box = Box.Identity;
            var cap = new Capsule(new Vector3(FP.FromInt(3), FP.Zero, FP.Zero), new Vector3(FP.FromInt(3), FP.Zero, FP.Zero), FP.FromInt(1));
            var r = ShapeDistance.Compute(box, cap);
            AssertApprox(FP.FromInt(1), r.Distance, "distance");
        }

        [Test]
        public static void DispatcherAllOverloads()
        {
            var sphere = new Sphere(Vector3.Zero, FP.FromInt(1));
            var box = Box.Identity;
            var cap = new Capsule(Vector3.Zero, Vector3.Up, FP.FromInt(1));
            var plane = new Plane(Vector3.Up, FP.Zero);

            ShapeDistance.Compute(sphere, sphere);
            ShapeDistance.Compute(sphere, box);
            ShapeDistance.Compute(box, sphere);
            ShapeDistance.Compute(sphere, cap);
            ShapeDistance.Compute(cap, sphere);
            ShapeDistance.Compute(sphere, plane);
            ShapeDistance.Compute(plane, sphere);
            ShapeDistance.Compute(box, box);
            ShapeDistance.Compute(box, cap);
            ShapeDistance.Compute(cap, box);
            ShapeDistance.Compute(box, plane);
            ShapeDistance.Compute(plane, box);
            ShapeDistance.Compute(cap, cap);
            ShapeDistance.Compute(cap, plane);
            ShapeDistance.Compute(plane, cap);
        }

        [Test]
        public static void DispatcherSwappedResultHasFlippedNormal()
        {
            var a = new Sphere(Vector3.Zero, FP.FromInt(1));
            var b = new Sphere(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), FP.FromInt(1));
            var r1 = ShapeDistance.Compute(a, b);
            var r2 = ShapeDistance.Compute(b, a);
            TestRunner.AssertEqual(r1.Distance, r2.Distance);
            TestRunner.AssertEqual(r1.PointA, r2.PointB);
            TestRunner.AssertEqual(r1.PointB, r2.PointA);
            TestRunner.AssertEqual(-r1.Normal.X, r2.Normal.X);
            TestRunner.AssertEqual(-r1.Normal.Y, r2.Normal.Y);
            TestRunner.AssertEqual(-r1.Normal.Z, r2.Normal.Z);
        }

        [Test]
        public static void DistanceResultEquality()
        {
            var a = new DistanceResult(FP.One, Vector3.Zero, Vector3.Up, Vector3.Forward, 0);
            var b = new DistanceResult(FP.One, Vector3.Zero, Vector3.Up, Vector3.Forward, 0);
            var c = new DistanceResult(FP.FromInt(2), Vector3.Zero, Vector3.Up, Vector3.Forward, 0);
            TestRunner.Assert(a == b);
            TestRunner.Assert(a != c);
        }
    }
}
