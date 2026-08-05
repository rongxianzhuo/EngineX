using System;
using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics
{
    public readonly struct Capsule : IEquatable<Capsule>, ISupport
    {
        public readonly Vector3 PointA;
        public readonly Vector3 PointB;
        public readonly FP Radius;

        public Capsule(Vector3 pointA, Vector3 pointB, FP radius)
        {
            PointA = pointA;
            PointB = pointB;
            Radius = radius;
        }

        public Vector3 Axis => PointB - PointA;

        public FP Height => Axis.Magnitude;

        public FP Volume
        {
            get
            {
                FP r2 = Radius * Radius;
                FP r3 = r2 * Radius;
                FP h = Height;
                FP cylinder = FP.PI * r2 * h;
                FP sphere = FP.FromInt(4) * FP.PI * r3 / FP.FromInt(3);
                return cylinder + sphere;
            }
        }

        public Vector3 Centroid => (PointA + PointB) * FP.Half;

        public Aabb BoundingBox
        {
            get
            {
                Vector3 axis = Axis;
                Vector3 r = new Vector3(Radius, Radius, Radius);
                Vector3 absAxis = new Vector3(axis.X.Abs(), axis.Y.Abs(), axis.Z.Abs());
                Vector3 extents = absAxis * FP.Half + r;
                Vector3 center = Centroid;
                return Aabb.FromCenterExtents(center, extents);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Support(Vector3 direction)
        {
            FP dMag = direction.Magnitude;
            if (dMag == FP.Zero) return Centroid;
            Vector3 d = direction / dMag;
            FP sign = Vector3.Dot(d, Axis) >= FP.Zero ? FP.One : -FP.One;
            Vector3 axisPoint = sign >= FP.Zero ? PointB : PointA;
            return axisPoint + d * Radius;
        }

        public static bool operator ==(Capsule a, Capsule b) =>
            a.PointA == b.PointA && a.PointB == b.PointB && a.Radius == b.Radius;

        public static bool operator !=(Capsule a, Capsule b) => !(a == b);

        public bool Equals(Capsule other) =>
            PointA == other.PointA && PointB == other.PointB && Radius == other.Radius;

        public override bool Equals(object obj) => obj is Capsule other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(PointA, PointB, Radius);

        public override string ToString() => $"Capsule[PointA={PointA}, PointB={PointB}, Radius={Radius}]";
    }
}
