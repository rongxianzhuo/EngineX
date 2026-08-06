using System;
using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics
{
    public readonly struct Aabb : IEquatable<Aabb>
    {
        public readonly Vector3 Min;
        public readonly Vector3 Max;

        public static readonly Aabb Empty = new Aabb(
            new Vector3(FP.MaxValue, FP.MaxValue, FP.MaxValue),
            new Vector3(-FP.MaxValue, -FP.MaxValue, -FP.MaxValue));

        public Aabb(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public static Aabb FromCenterExtents(Vector3 center, Vector3 halfExtents)
        {
            return new Aabb(center - halfExtents, center + halfExtents);
        }

        public Vector3 Center => (Min + Max) * FP.Half;

        public Vector3 Extents => (Max - Min) * FP.Half;

        public FP SurfaceArea
        {
            get
            {
                Vector3 d = Max - Min;
                return FP.FromInt(2) * (d.X * d.Y + d.Y * d.Z + d.Z * d.X);
            }
        }

        public FP Volume
        {
            get
            {
                Vector3 d = Max - Min;
                return d.X * d.Y * d.Z;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Vector3 point)
        {
            return point.X >= Min.X && point.X <= Max.X
                && point.Y >= Min.Y && point.Y <= Max.Y
                && point.Z >= Min.Z && point.Z <= Max.Z;
        }

        public bool Contains(Aabb other)
        {
            return other.Min.X >= Min.X && other.Max.X <= Max.X
                && other.Min.Y >= Min.Y && other.Max.Y <= Max.Y
                && other.Min.Z >= Min.Z && other.Max.Z <= Max.Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(Aabb other)
        {
            return Min.X <= other.Max.X && Max.X >= other.Min.X
                && Min.Y <= other.Max.Y && Max.Y >= other.Min.Y
                && Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Aabb Merge(Aabb other)
        {
            return new Aabb(Vector3.Min(Min, other.Min), Vector3.Max(Max, other.Max));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Aabb Merge(Vector3 point)
        {
            return new Aabb(Vector3.Min(Min, point), Vector3.Max(Max, point));
        }

        public Aabb Expand(FP amount)
        {
            Vector3 e = new Vector3(amount, amount, amount);
            return new Aabb(Min - e, Max + e);
        }

        public static bool operator ==(Aabb a, Aabb b) => a.Min == b.Min && a.Max == b.Max;

        public static bool operator !=(Aabb a, Aabb b) => !(a == b);

        public bool Equals(Aabb other) => Min == other.Min && Max == other.Max;

        public override bool Equals(object obj) => obj is Aabb other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Min, Max);

        public override string ToString() => $"Aabb[Min={Min}, Max={Max}]";
    }
}
