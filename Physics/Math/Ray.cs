using System;
using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics
{
    public readonly struct Ray : IEquatable<Ray>
    {
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;

        public Ray(Vector3 origin, Vector3 direction)
        {
            Origin = origin;
            Direction = direction.Normalized;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetPoint(FP t)
        {
            return Origin + Direction * t;
        }

        public static bool operator ==(Ray a, Ray b) => a.Origin == b.Origin && a.Direction == b.Direction;

        public static bool operator !=(Ray a, Ray b) => !(a == b);

        public bool Equals(Ray other) => Origin == other.Origin && Direction == other.Direction;

        public override bool Equals(object obj) => obj is Ray other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Origin, Direction);

        public override string ToString() => $"Ray[Origin={Origin}, Direction={Direction}]";
    }
}
