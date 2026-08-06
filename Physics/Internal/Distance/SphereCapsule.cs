using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.Distance
{
    internal static class SphereCapsule
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Sphere sphere, Capsule capsule)
        {
            Vector3 closestOnAxis = BoxMath.ClosestPointOnSegment(capsule.PointA, capsule.PointB, sphere.Center);
            Vector3 delta = sphere.Center - closestOnAxis;
            FP dist = delta.Magnitude;
            Vector3 normal = dist == FP.Zero ? Vector3.Up : delta / dist;
            Vector3 pointA = sphere.Center + normal * sphere.Radius;
            Vector3 pointB = closestOnAxis + normal * capsule.Radius;
            FP separation = dist - sphere.Radius - capsule.Radius;
            return new DistanceResult(separation, pointA, pointB, normal, 0);
        }
    }
}
