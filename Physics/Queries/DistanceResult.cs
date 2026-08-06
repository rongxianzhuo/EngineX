using System;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics
{
    public readonly struct DistanceResult : IEquatable<DistanceResult>
    {
        public readonly FP Distance;
        public readonly Vector3 PointA;
        public readonly Vector3 PointB;
        public readonly Vector3 Normal;
        public readonly int Iterations;

        public DistanceResult(FP distance, Vector3 pointA, Vector3 pointB, Vector3 normal, int iterations)
        {
            Distance = distance;
            PointA = pointA;
            PointB = pointB;
            Normal = normal;
            Iterations = iterations;
        }

        public bool Overlapping => Distance < FP.Zero;

        public bool Touching => Distance == FP.Zero;

        public DistanceResult Swapped => new DistanceResult(Distance, PointB, PointA, -Normal, Iterations);

        public static bool operator ==(DistanceResult a, DistanceResult b) =>
            a.Distance == b.Distance
            && a.PointA == b.PointA
            && a.PointB == b.PointB
            && a.Normal == b.Normal
            && a.Iterations == b.Iterations;

        public static bool operator !=(DistanceResult a, DistanceResult b) => !(a == b);

        public bool Equals(DistanceResult other) => this == other;

        public override bool Equals(object obj) => obj is DistanceResult other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Distance, PointA, PointB, Normal, Iterations);

        public override string ToString() =>
            $"DistanceResult[Distance={Distance}, Normal={Normal}, Iterations={Iterations}]";
    }
}
