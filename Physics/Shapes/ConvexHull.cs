using System;
using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics
{
    public readonly struct ConvexHull : IEquatable<ConvexHull>, ISupport
    {
        public readonly Vector3[] Vertices;

        public ConvexHull(Vector3[] vertices)
        {
            Vertices = vertices;
        }

        public int VertexCount => Vertices == null ? 0 : Vertices.Length;

        public Vector3 GetVertex(int index) => Vertices[index];

        public Vector3 Centroid
        {
            get
            {
                if (Vertices == null || Vertices.Length == 0) return Vector3.Zero;
                Vector3 sum = Vector3.Zero;
                for (int i = 0; i < Vertices.Length; i++) sum += Vertices[i];
                return sum / FP.FromInt(Vertices.Length);
            }
        }

        public Aabb BoundingBox
        {
            get
            {
                if (Vertices == null || Vertices.Length == 0) return Aabb.Empty;
                Aabb box = new Aabb(Vertices[0], Vertices[0]);
                for (int i = 1; i < Vertices.Length; i++) box = box.Merge(Vertices[i]);
                return box;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Support(Vector3 direction)
        {
            if (Vertices == null || Vertices.Length == 0) return Vector3.Zero;
            int bestIndex = 0;
            FP bestDot = Vector3.Dot(Vertices[0], direction);
            for (int i = 1; i < Vertices.Length; i++)
            {
                FP d = Vector3.Dot(Vertices[i], direction);
                if (d > bestDot)
                {
                    bestDot = d;
                    bestIndex = i;
                }
            }
            return Vertices[bestIndex];
        }

        public bool Equals(ConvexHull other) =>
            ReferenceEquals(Vertices, other.Vertices)
            || (Vertices != null && other.Vertices != null && Vertices.Length == other.Vertices.Length);

        public override bool Equals(object obj) => obj is ConvexHull other && Equals(other);

        public override int GetHashCode() => Vertices == null ? 0 : Vertices.Length.GetHashCode();

        public override string ToString() => $"ConvexHull[VertexCount={VertexCount}]";
    }
}
