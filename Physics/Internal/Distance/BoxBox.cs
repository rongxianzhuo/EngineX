using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.Distance
{
    internal static class BoxBox
    {
        private static readonly FP AxisEpsilon = FP.FromFloat(1e-6f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Box a, Box b)
        {
            Vector3 aX = a.Orientation * Vector3.Right;
            Vector3 aY = a.Orientation * Vector3.Up;
            Vector3 aZ = a.Orientation * Vector3.Forward;
            Vector3 bX = b.Orientation * Vector3.Right;
            Vector3 bY = b.Orientation * Vector3.Up;
            Vector3 bZ = b.Orientation * Vector3.Forward;
            Vector3 T = b.Center - a.Center;
            Quaternion aInv = Quaternion.Inverse(a.Orientation);
            Quaternion bInv = Quaternion.Inverse(b.Orientation);

            FP largestPositiveGap = FP.Zero;
            Vector3 sepAxis = Vector3.Up;
            FP bestNegativeGap = -FP.MaxValue;
            Vector3 penAxis = Vector3.Up;
            bool hasPositive = false;

            TestAxis(a, b, aInv, bInv, T, aX, ref largestPositiveGap, ref sepAxis, ref bestNegativeGap, ref penAxis, ref hasPositive);
            TestAxis(a, b, aInv, bInv, T, aY, ref largestPositiveGap, ref sepAxis, ref bestNegativeGap, ref penAxis, ref hasPositive);
            TestAxis(a, b, aInv, bInv, T, aZ, ref largestPositiveGap, ref sepAxis, ref bestNegativeGap, ref penAxis, ref hasPositive);
            TestAxis(a, b, aInv, bInv, T, bX, ref largestPositiveGap, ref sepAxis, ref bestNegativeGap, ref penAxis, ref hasPositive);
            TestAxis(a, b, aInv, bInv, T, bY, ref largestPositiveGap, ref sepAxis, ref bestNegativeGap, ref penAxis, ref hasPositive);
            TestAxis(a, b, aInv, bInv, T, bZ, ref largestPositiveGap, ref sepAxis, ref bestNegativeGap, ref penAxis, ref hasPositive);

            TestAxis(a, b, aInv, bInv, T, Vector3.Cross(aX, bX), ref largestPositiveGap, ref sepAxis, ref bestNegativeGap, ref penAxis, ref hasPositive);
            TestAxis(a, b, aInv, bInv, T, Vector3.Cross(aX, bY), ref largestPositiveGap, ref sepAxis, ref bestNegativeGap, ref penAxis, ref hasPositive);
            TestAxis(a, b, aInv, bInv, T, Vector3.Cross(aX, bZ), ref largestPositiveGap, ref sepAxis, ref bestNegativeGap, ref penAxis, ref hasPositive);
            TestAxis(a, b, aInv, bInv, T, Vector3.Cross(aY, bX), ref largestPositiveGap, ref sepAxis, ref bestNegativeGap, ref penAxis, ref hasPositive);
            TestAxis(a, b, aInv, bInv, T, Vector3.Cross(aY, bY), ref largestPositiveGap, ref sepAxis, ref bestNegativeGap, ref penAxis, ref hasPositive);
            TestAxis(a, b, aInv, bInv, T, Vector3.Cross(aY, bZ), ref largestPositiveGap, ref sepAxis, ref bestNegativeGap, ref penAxis, ref hasPositive);
            TestAxis(a, b, aInv, bInv, T, Vector3.Cross(aZ, bX), ref largestPositiveGap, ref sepAxis, ref bestNegativeGap, ref penAxis, ref hasPositive);
            TestAxis(a, b, aInv, bInv, T, Vector3.Cross(aZ, bY), ref largestPositiveGap, ref sepAxis, ref bestNegativeGap, ref penAxis, ref hasPositive);
            TestAxis(a, b, aInv, bInv, T, Vector3.Cross(aZ, bZ), ref largestPositiveGap, ref sepAxis, ref bestNegativeGap, ref penAxis, ref hasPositive);

            Vector3 contactNormal;
            FP finalGap;
            if (hasPositive)
            {
                contactNormal = sepAxis;
                finalGap = largestPositiveGap;
            }
            else
            {
                contactNormal = penAxis;
                finalGap = bestNegativeGap;
            }

            Vector3 pointA = a.Support(-contactNormal);
            Vector3 pointB = b.Support(contactNormal);
            return new DistanceResult(finalGap, pointA, pointB, contactNormal, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TestAxis(
            in Box a, in Box b, in Quaternion aInv, in Quaternion bInv, in Vector3 T,
            Vector3 axis,
            ref FP largestPositiveGap, ref Vector3 sepAxis,
            ref FP bestNegativeGap, ref Vector3 penAxis,
            ref bool hasPositive)
        {
            FP sqrMag = axis.SqrMagnitude;
            if (sqrMag < AxisEpsilon) return;
            Vector3 L = axis / sqrMag.Sqrt;
            Vector3 lA = aInv * L;
            Vector3 lB = bInv * L;
            FP rA = a.HalfExtents.X * lA.X.Abs()
                  + a.HalfExtents.Y * lA.Y.Abs()
                  + a.HalfExtents.Z * lA.Z.Abs();
            FP rB = b.HalfExtents.X * lB.X.Abs()
                  + b.HalfExtents.Y * lB.Y.Abs()
                  + b.HalfExtents.Z * lB.Z.Abs();
            FP rTotal = rA + rB;
            FP signedDist = Vector3.Dot(L, T);
            FP gap = signedDist.Abs() - rTotal;
            if (gap > FP.Zero)
            {
                if (gap > largestPositiveGap)
                {
                    largestPositiveGap = gap;
                    sepAxis = signedDist >= FP.Zero ? L : -L;
                }
                hasPositive = true;
            }
            else if (gap > bestNegativeGap)
            {
                bestNegativeGap = gap;
                penAxis = signedDist >= FP.Zero ? L : -L;
            }
        }
    }
}
