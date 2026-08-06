using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.Distance
{
    internal static class SpherePlane
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Sphere sphere, Plane plane)
        {
            FP signedDist = plane.SignedDistance(sphere.Center);
            FP separation = signedDist - sphere.Radius;
            Vector3 normal = plane.Normal;
            Vector3 pointA = sphere.Center + normal * sphere.Radius;
            Vector3 pointB = sphere.Center - normal * signedDist;
            return new DistanceResult(separation, pointA, pointB, normal, 0);
        }
    }
}
