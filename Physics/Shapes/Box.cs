using System;
using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics
{
    public readonly struct Box : IEquatable<Box>, ISupport
    {
        public readonly Vector3 Center;
        public readonly Vector3 HalfExtents;
        public readonly Quaternion Orientation;

        public static readonly Box Identity = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);

        public Box(Vector3 center, Vector3 halfExtents, Quaternion orientation)
        {
            Center = center;
            HalfExtents = halfExtents;
            Orientation = orientation;
        }

        public FP Volume
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FP.FromInt(8) * HalfExtents.X * HalfExtents.Y * HalfExtents.Z;
        }

        public Vector3 Centroid => Center;

        public Aabb BoundingBox
        {
            get
            {
                Vector3 abs = new Vector3(
                    HalfExtents.X.Abs(),
                    HalfExtents.Y.Abs(),
                    HalfExtents.Z.Abs());
                Vector3 r = abs.X * AbsRow(Orientation, 0)
                          + abs.Y * AbsRow(Orientation, 1)
                          + abs.Z * AbsRow(Orientation, 2);
                return Aabb.FromCenterExtents(Center, r);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 AbsRow(Quaternion q, int row)
        {
            FP a, b, c;
            switch (row)
            {
                case 0:
                    a = FP.One - FP.FromInt(2) * (q.Y * q.Y + q.Z * q.Z);
                    b = FP.FromInt(2) * (q.X * q.Y + q.W * q.Z);
                    c = FP.FromInt(2) * (q.X * q.Z - q.W * q.Y);
                    break;
                case 1:
                    a = FP.FromInt(2) * (q.X * q.Y - q.W * q.Z);
                    b = FP.One - FP.FromInt(2) * (q.X * q.X + q.Z * q.Z);
                    c = FP.FromInt(2) * (q.Y * q.Z + q.W * q.X);
                    break;
                default:
                    a = FP.FromInt(2) * (q.X * q.Z + q.W * q.Y);
                    b = FP.FromInt(2) * (q.Y * q.Z - q.W * q.X);
                    c = FP.One - FP.FromInt(2) * (q.X * q.X + q.Y * q.Y);
                    break;
            }
            return new Vector3(a.Abs(), b.Abs(), c.Abs());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Support(Vector3 direction)
        {
            Quaternion invOrient = Quaternion.Inverse(Orientation);
            Vector3 localDir = invOrient * direction;
            Vector3 localSupport = new Vector3(
                localDir.X >= FP.Zero ? HalfExtents.X : -HalfExtents.X,
                localDir.Y >= FP.Zero ? HalfExtents.Y : -HalfExtents.Y,
                localDir.Z >= FP.Zero ? HalfExtents.Z : -HalfExtents.Z);
            return Center + Orientation * localSupport;
        }

        public static bool operator ==(Box a, Box b) =>
            a.Center == b.Center
            && a.HalfExtents == b.HalfExtents
            && a.Orientation == b.Orientation;

        public static bool operator !=(Box a, Box b) => !(a == b);

        public bool Equals(Box other) =>
            Center == other.Center
            && HalfExtents == other.HalfExtents
            && Orientation == other.Orientation;

        public override bool Equals(object obj) => obj is Box other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Center, HalfExtents, Orientation);

        public override string ToString() =>
            $"Box[Center={Center}, HalfExtents={HalfExtents}, Orientation={Orientation}]";
    }
}
