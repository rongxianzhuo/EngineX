using System;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics
{
    public readonly struct Manifold : IEquatable<Manifold>
    {
        public readonly bool IsValid;
        public readonly Vector3 PointOnA;
        public readonly Vector3 PointOnB;
        public readonly Vector3 Normal;
        public readonly FP Penetration;

        public Manifold(Vector3 pointOnA, Vector3 pointOnB, Vector3 normal, FP penetration)
        {
            IsValid = true;
            PointOnA = pointOnA;
            PointOnB = pointOnB;
            Normal = normal;
            Penetration = penetration;
        }

        public static Manifold Empty => default;

        public bool Equals(Manifold other) =>
            IsValid == other.IsValid
            && PointOnA == other.PointOnA
            && PointOnB == other.PointOnB
            && Normal == other.Normal
            && Penetration == other.Penetration;

        public override bool Equals(object obj) => obj is Manifold other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(IsValid, PointOnA, PointOnB, Normal, Penetration);

        public static bool operator ==(Manifold a, Manifold b) => a.Equals(b);
        public static bool operator !=(Manifold a, Manifold b) => !a.Equals(b);

        public override string ToString()
            => $"Manifold[Valid={IsValid}, A={PointOnA}, B={PointOnB}, N={Normal}, D={Penetration}]";
    }
}