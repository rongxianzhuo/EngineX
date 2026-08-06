using System;
using EngineX.Baseline.FixedPoint;

namespace EngineX.Physics
{
    public readonly struct BodyHandle : IEquatable<BodyHandle>
    {
        public readonly int Id;
        public readonly int Version;

        public BodyHandle(int id, int version)
        {
            Id = id;
            Version = version;
        }

        public static BodyHandle Invalid => new BodyHandle(-1, 0);

        public bool IsValid => Id >= 0;

        public bool Equals(BodyHandle other) => Id == other.Id && Version == other.Version;

        public override bool Equals(object obj) => obj is BodyHandle other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Id, Version);

        public static bool operator ==(BodyHandle a, BodyHandle b) => a.Equals(b);
        public static bool operator !=(BodyHandle a, BodyHandle b) => !a.Equals(b);

        public override string ToString() => $"BodyHandle[Id={Id}, Ver={Version}]";
    }

    [Flags]
    public enum BodyFlags : byte
    {
        None = 0,
        Awake = 1 << 0,
        Sleeping = 1 << 1,
        Static = 1 << 2,
        Disabled = 1 << 3,
    }
}