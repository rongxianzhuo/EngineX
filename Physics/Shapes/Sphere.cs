using System;
using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics
{
    public readonly struct Sphere : IEquatable<Sphere>, ISupport
    {
        public readonly Vector3 Center;
        public readonly FP Radius;

        public Sphere(Vector3 center, FP radius)
        {
            Center = center;
            Radius = radius;
        }

        public FP Volume
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                FP fourThirds = FP.FromInt(4) / FP.FromInt(3);
                return fourThirds * FP.PI * Radius * Radius * Radius;
            }
        }

        public Vector3 Centroid => Center;

        public Aabb BoundingBox
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Aabb.FromCenterExtents(Center, new Vector3(Radius, Radius, Radius));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Support(Vector3 direction)
        {
            FP dMag = direction.Magnitude;
            if (dMag == FP.Zero) return Center;
            return Center + direction / dMag * Radius;
        }

        public static bool operator ==(Sphere a, Sphere b) => a.Center == b.Center && a.Radius == b.Radius;

        public static bool operator !=(Sphere a, Sphere b) => !(a == b);

        public bool Equals(Sphere other) => Center == other.Center && Radius == other.Radius;

        public override bool Equals(object obj) => obj is Sphere other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Center, Radius);

        public override string ToString() => $"Sphere[Center={Center}, Radius={Radius}]";
    }
}
