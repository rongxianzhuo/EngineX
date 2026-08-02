using System;

namespace EngineX.ECS
{
    public readonly struct Entity : IEquatable<Entity>
    {
        public readonly int Index;

        public readonly int Version;

        public Entity(int index, int version)
        {
            Index = index;
            Version = version;
        }

        public static readonly Entity Null = new Entity(0, 0);

        public bool Equals(Entity other)
        {
            return Index == other.Index && Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            return obj is Entity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Index, Version);
        }

        public static bool operator ==(Entity a, Entity b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Entity a, Entity b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            return $"Entity({Index}:{Version})";
        }
    }
}
