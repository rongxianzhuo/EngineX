using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.Distance
{
    internal static class CapsulePlane
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Capsule capsule, Plane plane)
        {
            FP signedA = plane.SignedDistance(capsule.PointA);
            FP signedB = plane.SignedDistance(capsule.PointB);
            bool useA = signedA < signedB;
            Vector3 closestEndpoint = useA ? capsule.PointA : capsule.PointB;
            FP signedDist = useA ? signedA : signedB;
            FP separation = signedDist >= FP.Zero ? signedDist - capsule.Radius : signedDist + capsule.Radius;
            Vector3 normal = plane.Normal;
            Vector3 pointA = closestEndpoint + normal * capsule.Radius;
            Vector3 pointB = closestEndpoint - normal * signedDist;
            return new DistanceResult(separation, pointA, pointB, normal, 0);
        }
    }
}
