using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.Distance
{
    internal static class SphereSphere
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Sphere a, Sphere b)
        {
            Vector3 delta = b.Center - a.Center;
            FP dist = delta.Magnitude;
            Vector3 normal = dist == FP.Zero ? Vector3.Up : delta / dist;
            Vector3 pointA = a.Center + normal * a.Radius;
            Vector3 pointB = b.Center - normal * b.Radius;
            FP separation = dist - a.Radius - b.Radius;
            return new DistanceResult(separation, pointA, pointB, normal, 0);
        }
    }
}
