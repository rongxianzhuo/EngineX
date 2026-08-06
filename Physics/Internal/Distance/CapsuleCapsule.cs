using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.Distance
{
    internal static class CapsuleCapsule
    {
        private static readonly FP LengthEpsilon = FP.FromFloat(1e-10f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Capsule a, Capsule b)
        {
            Vector3 d1 = a.PointB - a.PointA;
            Vector3 d2 = b.PointB - b.PointA;
            Vector3 r = a.PointA - b.PointA;
            FP a1 = d1.SqrMagnitude;
            FP e = d2.SqrMagnitude;
            FP f = Vector3.Dot(d2, r);
            FP s;
            FP t;

            if (a1 <= LengthEpsilon && e <= LengthEpsilon)
            {
                s = FP.Zero;
                t = FP.Zero;
            }
            else if (a1 <= LengthEpsilon)
            {
                s = FP.Zero;
                t = FP.Clamp(f / e, FP.Zero, FP.One);
            }
            else
            {
                FP c = Vector3.Dot(d1, r);
                if (e <= LengthEpsilon)
                {
                    t = FP.Zero;
                    s = FP.Clamp(-c / a1, FP.Zero, FP.One);
                }
                else
                {
                    FP bdot = Vector3.Dot(d1, d2);
                    FP denom = a1 * e - bdot * bdot;
                    s = denom != FP.Zero
                        ? FP.Clamp((bdot * f - c * e) / denom, FP.Zero, FP.One)
                        : FP.Zero;
                    t = (bdot * s + f) / e;
                    if (t < FP.Zero)
                    {
                        t = FP.Zero;
                        s = FP.Clamp(-c / a1, FP.Zero, FP.One);
                    }
                    else if (t > FP.One)
                    {
                        t = FP.One;
                        s = FP.Clamp((bdot - c) / a1, FP.Zero, FP.One);
                    }
                }
            }

            Vector3 cA = a.PointA + d1 * s;
            Vector3 cB = b.PointA + d2 * t;
            Vector3 delta = cB - cA;
            FP dist = delta.Magnitude;
            Vector3 normal = dist == FP.Zero ? Vector3.Up : delta / dist;
            Vector3 pointA = cA + normal * a.Radius;
            Vector3 pointB = cB - normal * b.Radius;
            FP separation = dist - a.Radius - b.Radius;
            return new DistanceResult(separation, pointA, pointB, normal, 0);
        }
    }
}
