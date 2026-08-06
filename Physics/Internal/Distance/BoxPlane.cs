using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.Distance
{
    internal static class BoxPlane
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Box box, Plane plane)
        {
            Quaternion inv = Quaternion.Inverse(box.Orientation);
            Vector3 localNormal = inv * plane.Normal;

            Vector3 localSupportPos = new Vector3(
                localNormal.X >= FP.Zero ? box.HalfExtents.X : -box.HalfExtents.X,
                localNormal.Y >= FP.Zero ? box.HalfExtents.Y : -box.HalfExtents.Y,
                localNormal.Z >= FP.Zero ? box.HalfExtents.Z : -box.HalfExtents.Z);
            Vector3 supportPos = box.Center + box.Orientation * localSupportPos;
            FP signedPos = plane.SignedDistance(supportPos);

            Vector3 localSupportNeg = new Vector3(
                localNormal.X >= FP.Zero ? -box.HalfExtents.X : box.HalfExtents.X,
                localNormal.Y >= FP.Zero ? -box.HalfExtents.Y : box.HalfExtents.Y,
                localNormal.Z >= FP.Zero ? -box.HalfExtents.Z : box.HalfExtents.Z);
            Vector3 supportNeg = box.Center + box.Orientation * localSupportNeg;
            FP signedNeg = plane.SignedDistance(supportNeg);

            FP separation;
            Vector3 supportVertex;
            if (signedPos.Abs() <= signedNeg.Abs())
            {
                separation = signedPos;
                supportVertex = supportPos;
            }
            else
            {
                separation = signedNeg;
                supportVertex = supportNeg;
            }

            Vector3 normal = plane.Normal;
            Vector3 pointA = supportVertex;
            Vector3 pointB = supportVertex - normal * separation;
            return new DistanceResult(separation, pointA, pointB, normal, 0);
        }
    }
}
