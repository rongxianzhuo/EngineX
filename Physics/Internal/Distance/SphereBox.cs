using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.Distance
{
    internal static class SphereBox
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Sphere sphere, Box box)
        {
            Vector3 closestOnBox = BoxMath.ClosestPointOnBox(box, sphere.Center);
            Vector3 delta = sphere.Center - closestOnBox;
            FP distSqr = delta.SqrMagnitude;

            if (distSqr == FP.Zero)
            {
                return ComputeCenterInside(sphere, box);
            }

            FP dist = distSqr.Sqrt;
            Vector3 normal = delta / dist;
            Vector3 pointA = sphere.Center + normal * sphere.Radius;
            Vector3 pointB = closestOnBox;
            FP separation = dist - sphere.Radius;
            return new DistanceResult(separation, pointA, pointB, normal, 0);
        }

        private static DistanceResult ComputeCenterInside(Sphere sphere, Box box)
        {
            Quaternion inv = Quaternion.Inverse(box.Orientation);
            Vector3 localCenter = inv * (sphere.Center - box.Center);
            FP dx = box.HalfExtents.X - localCenter.X.Abs();
            FP dy = box.HalfExtents.Y - localCenter.Y.Abs();
            FP dz = box.HalfExtents.Z - localCenter.Z.Abs();
            FP minPenetration = FP.Min(FP.Min(dx, dy), dz);
            Vector3 localNormal;
            Vector3 localVertex;
            if (minPenetration == dx)
            {
                localNormal = new Vector3(localCenter.X >= FP.Zero ? FP.One : -FP.One, FP.Zero, FP.Zero);
                localVertex = new Vector3(localNormal.X * box.HalfExtents.X, FP.Zero, FP.Zero);
            }
            else if (minPenetration == dy)
            {
                localNormal = new Vector3(FP.Zero, localCenter.Y >= FP.Zero ? FP.One : -FP.One, FP.Zero);
                localVertex = new Vector3(FP.Zero, localNormal.Y * box.HalfExtents.Y, FP.Zero);
            }
            else
            {
                localNormal = new Vector3(FP.Zero, FP.Zero, localCenter.Z >= FP.Zero ? FP.One : -FP.One);
                localVertex = new Vector3(FP.Zero, FP.Zero, localNormal.Z * box.HalfExtents.Z);
            }
            Vector3 worldNormal = box.Orientation * localNormal;
            Vector3 pointB = box.Center + box.Orientation * localVertex;
            Vector3 pointA = sphere.Center + worldNormal * sphere.Radius;
            return new DistanceResult(-minPenetration - sphere.Radius, pointA, pointB, worldNormal, 0);
        }
    }
}
