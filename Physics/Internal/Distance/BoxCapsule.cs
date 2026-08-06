using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.Distance
{
    internal static class BoxCapsule
    {
        private const int MaxIterations = 10;
        private static readonly FP ConvergenceEpsilon = FP.FromFloat(1e-5f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Box box, Capsule capsule)
        {
            Vector3 axis = capsule.PointB - capsule.PointA;
            FP segLenSqr = axis.SqrMagnitude;

            if (segLenSqr == FP.Zero)
            {
                Vector3 degenerateClosest = BoxMath.ClosestPointOnBox(box, capsule.PointA);
                Vector3 delta = capsule.PointA - degenerateClosest;
                FP dist = delta.Magnitude;
                Vector3 normal = dist == FP.Zero ? Vector3.Up : delta / dist;
                Vector3 pointA = degenerateClosest;
                Vector3 pointB = capsule.PointA - normal * capsule.Radius;
                return new DistanceResult(dist - capsule.Radius, pointA, pointB, normal, 0);
            }

            FP t = FP.Half;
            Vector3 closestOnBox = BoxMath.ClosestPointOnBox(box, capsule.PointA + axis * t);
            int iter;
            for (iter = 0; iter < MaxIterations; iter++)
            {
                Vector3 projectOnSeg = BoxMath.ClosestPointOnSegment(capsule.PointA, capsule.PointB, closestOnBox);
                FP newT = Vector3.Dot(projectOnSeg - capsule.PointA, axis) / segLenSqr;
                if ((newT - t).Abs() < ConvergenceEpsilon)
                {
                    t = newT;
                    break;
                }
                t = newT;
                closestOnBox = BoxMath.ClosestPointOnBox(box, capsule.PointA + axis * t);
            }

            Vector3 finalSegPoint = capsule.PointA + axis * t;
            Vector3 finalDelta = finalSegPoint - closestOnBox;
            FP finalDist = finalDelta.Magnitude;
            Vector3 finalNormal = finalDist == FP.Zero ? Vector3.Up : finalDelta / finalDist;
            Vector3 pointABox = closestOnBox;
            Vector3 pointBCap = finalSegPoint - finalNormal * capsule.Radius;
            FP separation = finalDist - capsule.Radius;
            return new DistanceResult(separation, pointABox, pointBCap, finalNormal, iter + 1);
        }
    }
}
