using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.Distance
{
    internal static class BoxMath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ClosestPointOnBox(Box box, Vector3 point)
        {
            Quaternion inv = Quaternion.Inverse(box.Orientation);
            Vector3 local = inv * (point - box.Center);
            Vector3 clamped = new Vector3(
                FP.Clamp(local.X, -box.HalfExtents.X, box.HalfExtents.X),
                FP.Clamp(local.Y, -box.HalfExtents.Y, box.HalfExtents.Y),
                FP.Clamp(local.Z, -box.HalfExtents.Z, box.HalfExtents.Z));
            return box.Center + box.Orientation * clamped;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 point)
        {
            Vector3 ab = b - a;
            FP lenSqr = ab.SqrMagnitude;
            if (lenSqr == FP.Zero) return a;
            FP t = FP.Clamp(Vector3.Dot(point - a, ab) / lenSqr, FP.Zero, FP.One);
            return a + ab * t;
        }
    }
}
