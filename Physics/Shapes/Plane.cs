using System;
using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics
{
    public readonly struct Plane : IEquatable<Plane>
    {
        public readonly Vector3 Normal;
        public readonly FP D;

        public Plane(Vector3 normal, FP d)
        {
            Normal = normal.Normalized;
            D = d;
        }

        public static Plane FromNormalAndPoint(Vector3 normal, Vector3 point)
        {
            Vector3 n = normal.Normalized;
            return new Plane(n, Vector3.Dot(n, point));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FP SignedDistance(Vector3 point)
        {
            return Vector3.Dot(Normal, point) - D;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 ClosestPointOnPlane(Vector3 point)
        {
            return point - Normal * SignedDistance(point);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Project(Vector3 point)
        {
            return point - Normal * Vector3.Dot(Normal, point);
        }

        public static bool operator ==(Plane a, Plane b) => a.Normal == b.Normal && a.D == b.D;

        public static bool operator !=(Plane a, Plane b) => !(a == b);

        public bool Equals(Plane other) => Normal == other.Normal && D == other.D;

        public override bool Equals(object obj) => obj is Plane other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Normal, D);

        public override string ToString() => $"Plane[Normal={Normal}, D={D}]";
    }
}
