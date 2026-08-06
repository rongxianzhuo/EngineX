using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;
using EngineX.Physics.Internal.NarrowPhase;

namespace EngineX.Physics.Tests
{
    public static class GjkTests
    {
        [Test]
        public static void SphereSphereOverlappingDetected()
        {
            var a = new Sphere(Vector3.Zero, FP.FromInt(1));
            var b = new Sphere(new Vector3(FP.FromInt(1), FP.Zero, FP.Zero), FP.FromInt(1));
            bool overlap = GJK.Intersects(a, b, out _);
            TestRunner.Assert(overlap, "two spheres at distance 0 should overlap");
        }

        [Test]
        public static void SphereSphereSeparatedDetected()
        {
            var a = new Sphere(Vector3.Zero, FP.FromInt(1));
            var b = new Sphere(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), FP.FromInt(1));
            bool overlap = GJK.Intersects(a, b, out _);
            TestRunner.Assert(!overlap, "two spheres at distance 3 should be separated");
        }

        [Test]
        public static void SphereSphereTouchingDetected()
        {
            var a = new Sphere(Vector3.Zero, FP.FromInt(1));
            var b = new Sphere(new Vector3(FP.FromInt(2), FP.Zero, FP.Zero), FP.FromInt(1));
            bool overlap = GJK.Intersects(a, b, out _);
            TestRunner.Assert(overlap, "two touching spheres should be detected as overlapping");
        }

        [Test]
        public static void SphereSphereConcentricOverlappingDetected()
        {
            var a = new Sphere(Vector3.Zero, FP.FromInt(1));
            var b = new Sphere(Vector3.Zero, FP.FromInt(2));
            bool overlap = GJK.Intersects(a, b, out _);
            TestRunner.Assert(overlap, "concentric spheres should overlap");
        }

        [Test]
        public static void BoxBoxOverlapping()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromInt(1), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            bool overlap = GJK.Intersects(a, b, out _);
            TestRunner.Assert(overlap, "overlapping boxes should be detected");
        }

        [Test]
        public static void BoxBoxSeparated()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);
            bool overlap = GJK.Intersects(a, b, out _);
            TestRunner.Assert(!overlap, "separated boxes should be detected");
        }

        [Test]
        public static void SphereBoxOverlapping()
        {
            var s = new Sphere(new Vector3(FP.FromInt(1), FP.Zero, FP.Zero), FP.FromInt(1));
            var b = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            bool overlap = GJK.Intersects(s, b, out _);
            TestRunner.Assert(overlap, "overlapping sphere-box should be detected");
        }

        [Test]
        public static void GjkSimplexReachesTetrahedronForDeepOverlap()
        {
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.Half, FP.Half, FP.Half), new Vector3(FP.Half, FP.Half, FP.Half), Quaternion.Identity);
            GJK.Intersects(a, b, out var result);
            TestRunner.Assert(result.Intersecting, "deep overlap should be detected");
            TestRunner.Assert(result.Iterations > 0, $"simplex should grow for deep overlap, got iter={result.Iterations}");
        }
    }
}