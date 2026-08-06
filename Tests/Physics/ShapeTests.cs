using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;

namespace EngineX.Physics.Tests
{
    public static class ShapeTests
    {
        private static readonly FP FpEps = FP.FromFloat(0.0001f);

        private static void AssertApprox(FP expected, FP actual, string what)
        {
            FP diff = (expected - actual).Abs();
            if (diff > FpEps)
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

        [Test]
        public static void SphereVolumeMatchesFormula()
        {
            var s = new Sphere(Vector3.Zero, FP.FromInt(2));
            FP expected = FP.FromInt(4) / FP.FromInt(3) * FP.PI * FP.FromInt(8);
            AssertApprox(expected, s.Volume, "sphere(r=2).Volume");
        }

        [Test]
        public static void SphereCentroidIsCenter()
        {
            var c = new Vector3(FP.FromInt(3), FP.FromInt(4), FP.FromInt(5));
            var s = new Sphere(c, FP.FromInt(1));
            TestRunner.AssertEqual(c, s.Centroid);
        }

        [Test]
        public static void SphereBoundingBoxExtentsMatchRadius()
        {
            var c = new Vector3(FP.FromInt(1), FP.FromInt(2), FP.FromInt(3));
            var s = new Sphere(c, FP.FromInt(5));
            var box = s.BoundingBox;
            AssertApproxVec(c - new Vector3(FP.FromInt(5), FP.FromInt(5), FP.FromInt(5)), box.Min, "bbox.Min");
            AssertApproxVec(c + new Vector3(FP.FromInt(5), FP.FromInt(5), FP.FromInt(5)), box.Max, "bbox.Max");
        }

        [Test]
        public static void SphereSupportAlongAxis()
        {
            var s = new Sphere(new Vector3(FP.FromInt(1), FP.FromInt(2), FP.FromInt(3)), FP.FromInt(4));
            var sup = s.Support(new Vector3(FP.One, FP.Zero, FP.Zero));
            TestRunner.AssertEqual(FP.FromInt(5), sup.X);
            TestRunner.AssertEqual(FP.FromInt(2), sup.Y);
            TestRunner.AssertEqual(FP.FromInt(3), sup.Z);
        }

        [Test]
        public static void SphereEquality()
        {
            var a = new Sphere(Vector3.Zero, FP.FromInt(1));
            var b = new Sphere(Vector3.Zero, FP.FromInt(1));
            var c = new Sphere(Vector3.Zero, FP.FromInt(2));
            TestRunner.Assert(a == b);
            TestRunner.Assert(a != c);
            TestRunner.Assert(a.Equals((object)b));
        }

        [Test]
        public static void BoxVolumeMatchesFormula()
        {
            var b = new Box(
                Vector3.Zero,
                new Vector3(FP.FromInt(2), FP.FromInt(3), FP.FromInt(4)),
                Quaternion.Identity);
            TestRunner.AssertEqual(FP.FromInt(2 * 8 * 3 * 4), b.Volume);
        }

        [Test]
        public static void BoxIdentityBoundingBoxEqualsAabbOfHalfExtents()
        {
            var b = Box.Identity;
            var box = b.BoundingBox;
            AssertApproxVec(-Vector3.One, box.Min, "identity bbox.Min");
            AssertApproxVec(Vector3.One, box.Max, "identity bbox.Max");
        }

        [Test]
        public static void BoxRotated45DegreesAroundYExpandsAabb()
        {
            var b = new Box(
                Vector3.Zero,
                new Vector3(FP.FromInt(1), FP.FromInt(1), FP.FromInt(1)),
                Quaternion.AngleAxis(FP.FromInt(45), Vector3.Up));
            var box = b.BoundingBox;
            FP halfDiag = FP.FromFloat(1.4142135f);
            AssertApprox(-halfDiag, box.Min.X, "rot45 bbox.Min.X");
            AssertApprox(halfDiag, box.Max.X, "rot45 bbox.Max.X");
            AssertApprox(-FP.FromInt(1), box.Min.Y, "rot45 bbox.Min.Y");
            AssertApprox(FP.FromInt(1), box.Max.Y, "rot45 bbox.Max.Y");
        }

        [Test]
        public static void BoxSupportAlongAxis()
        {
            var b = new Box(
                Vector3.Zero,
                new Vector3(FP.FromInt(2), FP.FromInt(3), FP.FromInt(4)),
                Quaternion.Identity);
            var supX = b.Support(new Vector3(FP.One, FP.Zero, FP.Zero));
            TestRunner.AssertEqual(FP.FromInt(2), supX.X);
            TestRunner.AssertEqual(FP.FromInt(3), supY(supX));
            TestRunner.AssertEqual(FP.FromInt(4), supZ(supX));

            var supNegY = b.Support(new Vector3(FP.Zero, -FP.One, FP.Zero));
            TestRunner.AssertEqual(FP.FromInt(3), supNegY.Y.Abs());
        }

        private static FP supY(Vector3 v) => v.Y;
        private static FP supZ(Vector3 v) => v.Z;

        [Test]
        public static void BoxEquality()
        {
            var a = Box.Identity;
            var b = Box.Identity;
            var c = new Box(Vector3.One, Vector3.One, Quaternion.Identity);
            TestRunner.Assert(a == b);
            TestRunner.Assert(a != c);
        }

        [Test]
        public static void CapsuleVolumeMatchesFormula()
        {
            var cap = new Capsule(
                new Vector3(FP.Zero, FP.Zero, FP.Zero),
                new Vector3(FP.Zero, FP.FromInt(2), FP.Zero),
                FP.FromInt(1));
            FP cylinder = FP.PI * FP.FromInt(1) * FP.FromInt(2);
            FP sphere = FP.FromInt(4) * FP.PI / FP.FromInt(3);
            AssertApprox(cylinder + sphere, cap.Volume, "capsule volume");
        }

        [Test]
        public static void CapsuleAxisAndHeight()
        {
            var cap = new Capsule(
                new Vector3(FP.Zero, FP.Zero, FP.Zero),
                new Vector3(FP.Zero, FP.FromInt(5), FP.Zero),
                FP.FromInt(1));
            TestRunner.AssertEqual(FP.FromInt(5), cap.Axis.Y);
            TestRunner.AssertEqual(FP.FromInt(5), cap.Height);
        }

        [Test]
        public static void CapsuleCentroidIsMidpoint()
        {
            var cap = new Capsule(
                new Vector3(FP.FromInt(0), FP.FromInt(0), FP.FromInt(0)),
                new Vector3(FP.FromInt(4), FP.FromInt(6), FP.FromInt(8)),
                FP.FromInt(1));
            TestRunner.AssertEqual(new Vector3(FP.FromInt(2), FP.FromInt(3), FP.FromInt(4)), cap.Centroid);
        }

        [Test]
        public static void CapsuleSupportAtAxisEnds()
        {
            var cap = new Capsule(
                new Vector3(FP.Zero, FP.Zero, FP.Zero),
                new Vector3(FP.Zero, FP.FromInt(2), FP.Zero),
                FP.FromInt(1));
            var supUp = cap.Support(Vector3.Up);
            AssertApprox(FP.FromInt(3), supUp.Y, "capsule support up");
            var supDown = cap.Support(Vector3.Down);
            AssertApprox(-FP.FromInt(1), supDown.Y, "capsule support down");
        }

        [Test]
        public static void CapsuleEquality()
        {
            var a = new Capsule(Vector3.Zero, Vector3.Up, FP.FromInt(1));
            var b = new Capsule(Vector3.Zero, Vector3.Up, FP.FromInt(1));
            var c = new Capsule(Vector3.Zero, Vector3.Up, FP.FromInt(2));
            TestRunner.Assert(a == b);
            TestRunner.Assert(a != c);
        }

        [Test]
        public static void PlaneSignedDistanceIsCorrect()
        {
            var p = new Plane(Vector3.Up, FP.FromInt(5));
            TestRunner.AssertEqual(FP.FromInt(-5), p.SignedDistance(Vector3.Zero));
            TestRunner.AssertEqual(FP.Zero, p.SignedDistance(new Vector3(FP.Zero, FP.FromInt(5), FP.Zero)));
            TestRunner.AssertEqual(FP.FromInt(3), p.SignedDistance(new Vector3(FP.Zero, FP.FromInt(8), FP.Zero)));
        }

        [Test]
        public static void PlaneFromNormalAndPointMatches()
        {
            var p = Plane.FromNormalAndPoint(Vector3.Up, new Vector3(FP.FromInt(3), FP.FromInt(7), FP.FromInt(0)));
            TestRunner.AssertEqual(FP.FromInt(7), p.D);
            TestRunner.AssertEqual(FP.One, p.Normal.Y);
        }

        [Test]
        public static void PlaneClosestPointOnPlane()
        {
            var p = new Plane(Vector3.Up, FP.FromInt(5));
            var pt = new Vector3(FP.FromInt(1), FP.FromInt(9), FP.FromInt(2));
            var closest = p.ClosestPointOnPlane(pt);
            AssertApproxVec(new Vector3(FP.FromInt(1), FP.FromInt(5), FP.FromInt(2)), closest, "closest");
        }

        [Test]
        public static void PlaneEquality()
        {
            var a = new Plane(Vector3.Up, FP.FromInt(1));
            var b = new Plane(Vector3.Up, FP.FromInt(1));
            var c = new Plane(Vector3.Up, FP.FromInt(2));
            TestRunner.Assert(a == b);
            TestRunner.Assert(a != c);
        }

        [Test]
        public static void ConvexHullCentroidIsAverage()
        {
            var verts = new Vector3[]
            {
                new Vector3(FP.Zero, FP.Zero, FP.Zero),
                new Vector3(FP.FromInt(3), FP.Zero, FP.Zero),
                new Vector3(FP.Zero, FP.FromInt(3), FP.Zero),
                new Vector3(FP.Zero, FP.Zero, FP.FromInt(3))
            };
            var h = new ConvexHull(verts);
            TestRunner.AssertEqual(FP.FromInt(3) / FP.FromInt(4), h.Centroid.X);
            TestRunner.AssertEqual(FP.FromInt(3) / FP.FromInt(4), h.Centroid.Y);
            TestRunner.AssertEqual(FP.FromInt(3) / FP.FromInt(4), h.Centroid.Z);
        }

        [Test]
        public static void ConvexHullBoundingBoxEnclosesAllVertices()
        {
            var verts = new Vector3[]
            {
                new Vector3(FP.FromInt(-1), FP.FromInt(2), FP.FromInt(-3)),
                new Vector3(FP.FromInt(4), FP.FromInt(-5), FP.FromInt(6)),
                new Vector3(FP.FromInt(-7), FP.FromInt(8), FP.FromInt(-9))
            };
            var h = new ConvexHull(verts);
            var box = h.BoundingBox;
            TestRunner.AssertEqual(FP.FromInt(-7), box.Min.X);
            TestRunner.AssertEqual(FP.FromInt(-5), box.Min.Y);
            TestRunner.AssertEqual(FP.FromInt(-9), box.Min.Z);
            TestRunner.AssertEqual(FP.FromInt(4), box.Max.X);
            TestRunner.AssertEqual(FP.FromInt(8), box.Max.Y);
            TestRunner.AssertEqual(FP.FromInt(6), box.Max.Z);
        }

        [Test]
        public static void ConvexHullSupportPicksMaxDot()
        {
            var verts = new Vector3[]
            {
                new Vector3(FP.FromInt(-1), FP.Zero, FP.Zero),
                new Vector3(FP.FromInt(2), FP.Zero, FP.Zero),
                new Vector3(FP.Zero, FP.FromInt(3), FP.Zero)
            };
            var h = new ConvexHull(verts);
            var sup = h.Support(new Vector3(FP.One, FP.Zero, FP.Zero));
            TestRunner.AssertEqual(FP.FromInt(2), sup.X);
            var supY = h.Support(new Vector3(FP.Zero, FP.One, FP.Zero));
            TestRunner.AssertEqual(FP.FromInt(3), supY.Y);
        }

        [Test]
        public static void ConvexHullVertexCount()
        {
            var verts = new Vector3[]
            {
                Vector3.Zero, Vector3.One, Vector3.Up, Vector3.Right
            };
            var h = new ConvexHull(verts);
            TestRunner.AssertEqual(4, h.VertexCount);
            TestRunner.AssertEqual(Vector3.Zero, h.GetVertex(0));
        }

        [Test]
        public static void AabbFromCenterExtents()
        {
            var a = Aabb.FromCenterExtents(Vector3.Zero, new Vector3(FP.FromInt(1), FP.FromInt(2), FP.FromInt(3)));
            TestRunner.AssertEqual(-FP.FromInt(1), a.Min.X);
            TestRunner.AssertEqual(FP.FromInt(1), a.Max.X);
            TestRunner.AssertEqual(-FP.FromInt(2), a.Min.Y);
            TestRunner.AssertEqual(FP.FromInt(2), a.Max.Y);
        }

        [Test]
        public static void AabbCenterAndExtents()
        {
            var a = new Aabb(new Vector3(FP.FromInt(-2), FP.FromInt(-4), FP.FromInt(-6)), new Vector3(FP.FromInt(2), FP.FromInt(4), FP.FromInt(6)));
            TestRunner.AssertEqual(Vector3.Zero, a.Center);
            TestRunner.AssertEqual(new Vector3(FP.FromInt(2), FP.FromInt(4), FP.FromInt(6)), a.Extents);
        }

        [Test]
        public static void AabbSurfaceArea()
        {
            var a = new Aabb(Vector3.Zero, new Vector3(FP.FromInt(2), FP.FromInt(3), FP.FromInt(4)));
            FP expected = FP.FromInt(2) * (FP.FromInt(2 * 3) + FP.FromInt(3 * 4) + FP.FromInt(2 * 4));
            TestRunner.AssertEqual(expected, a.SurfaceArea);
        }

        [Test]
        public static void AabbVolume()
        {
            var a = new Aabb(Vector3.Zero, new Vector3(FP.FromInt(2), FP.FromInt(3), FP.FromInt(4)));
            TestRunner.AssertEqual(FP.FromInt(24), a.Volume);
        }

        [Test]
        public static void AabbContainsAndOverlaps()
        {
            var a = Aabb.FromCenterExtents(Vector3.Zero, new Vector3(FP.FromInt(2), FP.FromInt(2), FP.FromInt(2)));
            var b = Aabb.FromCenterExtents(new Vector3(FP.FromInt(1), FP.Zero, FP.Zero), new Vector3(FP.FromInt(1), FP.FromInt(1), FP.FromInt(1)));
            var c = Aabb.FromCenterExtents(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), new Vector3(FP.FromInt(1), FP.FromInt(1), FP.FromInt(1)));
            TestRunner.Assert(a.Contains(Vector3.Zero));
            TestRunner.Assert(a.Contains(b));
            TestRunner.Assert(!a.Contains(c));
            TestRunner.Assert(a.Overlaps(b));
            TestRunner.Assert(!a.Overlaps(c));
        }

        [Test]
        public static void AabbMergeAndExpand()
        {
            var a = new Aabb(Vector3.Zero, Vector3.One);
            var b = new Aabb(new Vector3(FP.FromInt(2), FP.FromInt(2), FP.FromInt(2)), new Vector3(FP.FromInt(3), FP.FromInt(3), FP.FromInt(3)));
            var merged = a.Merge(b);
            TestRunner.AssertEqual(Vector3.Zero, merged.Min);
            TestRunner.AssertEqual(new Vector3(FP.FromInt(3), FP.FromInt(3), FP.FromInt(3)), merged.Max);

            var expanded = a.Expand(FP.FromInt(1));
            TestRunner.AssertEqual(-Vector3.One, expanded.Min);
            TestRunner.AssertEqual(new Vector3(FP.FromInt(2), FP.FromInt(2), FP.FromInt(2)), expanded.Max);
        }

        [Test]
        public static void AabbEmptyIsIdentityForMerge()
        {
            var a = new Aabb(new Vector3(FP.FromInt(1), FP.FromInt(2), FP.FromInt(3)), new Vector3(FP.FromInt(4), FP.FromInt(5), FP.FromInt(6)));
            var m = Aabb.Empty.Merge(a);
            TestRunner.Assert(a == m);
        }

        [Test]
        public static void RayGetPointAlongDirection()
        {
            var r = new Ray(Vector3.Zero, new Vector3(FP.One, FP.Zero, FP.Zero));
            var p = r.GetPoint(FP.FromInt(3));
            TestRunner.AssertEqual(FP.FromInt(3), p.X);
            TestRunner.AssertEqual(FP.Zero, p.Y);
        }

        [Test]
        public static void RayNormalizesDirection()
        {
            var r = new Ray(Vector3.Zero, new Vector3(FP.FromInt(2), FP.Zero, FP.Zero));
            AssertApprox(FP.One, r.Direction.X, "ray normalized direction.X");
        }
    }
}
