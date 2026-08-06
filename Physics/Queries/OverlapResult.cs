using System;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics
{
    public readonly struct OverlapResult : IEquatable<OverlapResult>
    {
        public readonly bool IsOverlapping;
        public readonly Vector3 Normal;
        public readonly FP Penetration;
        public readonly int Iterations;

        public OverlapResult(bool isOverlapping, Vector3 normal, FP penetration, int iterations)
        {
            IsOverlapping = isOverlapping;
            Normal = normal;
            Penetration = penetration;
            Iterations = iterations;
        }

        public static OverlapResult NotOverlapping(int iterations = 0)
            => new OverlapResult(false, Vector3.Zero, FP.Zero, iterations);

        public bool Equals(OverlapResult other) =>
            IsOverlapping == other.IsOverlapping
            && Normal == other.Normal
            && Penetration == other.Penetration
            && Iterations == other.Iterations;

        public override bool Equals(object obj) => obj is OverlapResult other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(IsOverlapping, Normal, Penetration, Iterations);

        public static bool operator ==(OverlapResult a, OverlapResult b) => a.Equals(b);
        public static bool operator !=(OverlapResult a, OverlapResult b) => !a.Equals(b);

        public override string ToString()
            => $"OverlapResult[Overlap={IsOverlapping}, Normal={Normal}, Depth={Penetration}, Iters={Iterations}]";
    }
}