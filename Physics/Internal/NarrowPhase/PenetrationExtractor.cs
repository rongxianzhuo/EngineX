using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.NarrowPhase
{
    internal static class PenetrationExtractor
    {
        public static void Extract(Simplex s, out Vector3 normal, out FP penetration)
        {
            switch (s.Count)
            {
                case 2:
                    ExtractFromSegment(s.P1, s.P2, out normal, out penetration);
                    return;
                case 3:
                    ExtractFromTriangle(s.P1, s.P2, s.P3, out normal, out penetration);
                    return;
                case 4:
                    ExtractFromTetrahedron(s.P1, s.P2, s.P3, s.P4, out normal, out penetration);
                    return;
                default:
                    normal = Vector3.Up;
                    penetration = FP.FromFloat(0.01f);
                    return;
            }
        }

        private static void ExtractFromSegment(Vector3 p1, Vector3 p2, out Vector3 normal, out FP penetration)
        {
            FP d1 = p1.Magnitude;
            FP d2 = p2.Magnitude;
            if (d1 <= d2)
            {
                if (d1 == FP.Zero)
                {
                    normal = Vector3.Up;
                    penetration = d2;
                    return;
                }
                normal = p1 / d1;
                penetration = d1;
            }
            else
            {
                normal = p2 / d2;
                penetration = d2;
            }
        }

        private static void ExtractFromTriangle(Vector3 a, Vector3 b, Vector3 c, out Vector3 normal, out FP penetration)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 n = Vector3.Cross(ab, ac);
            FP len = n.Magnitude;
            if (len == FP.Zero)
            {
                normal = Vector3.Up;
                penetration = FP.FromFloat(0.01f);
                return;
            }
            normal = n / len;
            penetration = -Vector3.Dot(normal, a);
            if (penetration < FP.Zero)
            {
                normal = -normal;
                penetration = -penetration;
            }
        }

        private static void ExtractFromTetrahedron(
            Vector3 a, Vector3 b, Vector3 c, Vector3 d,
            out Vector3 normal, out FP penetration)
        {
            FP bestDist = FP.MaxValue;
            normal = Vector3.Up;

            TryFace(a, b, c, d, out var n1, out var d1);
            if (d1 < bestDist) { bestDist = d1; normal = n1; }

            TryFace(a, b, d, c, out var n2, out var d2);
            if (d2 < bestDist) { bestDist = d2; normal = n2; }

            TryFace(a, c, d, b, out var n3, out var d3);
            if (d3 < bestDist) { bestDist = d3; normal = n3; }

            TryFace(b, c, d, a, out var n4, out var d4);
            if (d4 < bestDist) { bestDist = d4; normal = n4; }

            if (bestDist < FP.Zero)
            {
                normal = -normal;
                bestDist = -bestDist;
            }
            penetration = bestDist;
        }

        private static void TryFace(Vector3 x, Vector3 y, Vector3 z, Vector3 opposite,
            out Vector3 normal, out FP signedDist)
        {
            Vector3 xy = y - x;
            Vector3 xz = z - x;
            Vector3 n = Vector3.Cross(xy, xz);
            FP len = n.Magnitude;
            if (len == FP.Zero)
            {
                normal = Vector3.Up;
                signedDist = FP.MaxValue;
                return;
            }
            n = n / len;
            FP oppSide = Vector3.Dot(n, opposite - x);
            if (oppSide > FP.Zero)
            {
                n = -n;
                signedDist = -Vector3.Dot(n, -x);
            }
            else
            {
                signedDist = -Vector3.Dot(n, -x);
            }
            normal = n;
        }
    }
}