using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.NarrowPhase
{
    internal static class SimplexSolver
    {
        public static Vector3 DoSimplex(ref Simplex s, out bool containsOrigin)
        {
            switch (s.Count)
            {
                case 1: containsOrigin = false; return DoPoint(ref s);
                case 2: containsOrigin = false; return DoSegment(ref s);
                case 3: return DoTriangle(ref s, out containsOrigin);
                case 4: return DoTetrahedron(ref s, out containsOrigin);
                default: containsOrigin = false; return Vector3.Up;
            }
        }

        private static Vector3 DoPoint(ref Simplex s)
        {
            return -s.P1;
        }

        private static Vector3 DoSegment(ref Simplex s)
        {
            Vector3 a = s.P1;
            Vector3 b = s.P2;
            Vector3 ab = b - a;
            FP denom = ab.SqrMagnitude;
            if (denom == FP.Zero)
            {
                s.Count = 1;
                return -a;
            }
            FP t = -Vector3.Dot(a, ab) / denom;
            if (t <= FP.Zero)
            {
                s.Count = 1;
                return -a;
            }
            if (t >= FP.One)
            {
                s.P1 = b; s.A1 = s.A2; s.B1 = s.B2;
                s.Count = 1;
                return -b;
            }
            return -(a + ab * t);
        }

        private static Vector3 DoTriangle(ref Simplex s, out bool containsOrigin)
        {
            Vector3 a = s.P1;
            Vector3 b = s.P2;
            Vector3 c = s.P3;
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ap = -a;
            Vector3 bp = -b;
            Vector3 cp = -c;

            FP d1 = Vector3.Dot(ab, ap);
            FP d2 = Vector3.Dot(ac, ap);
            if (d1 <= FP.Zero && d2 <= FP.Zero)
            {
                s.Count = 1;
                containsOrigin = false;
                return -a;
            }

            FP d3 = Vector3.Dot(ab, bp);
            FP d4 = Vector3.Dot(ac, bp);
            if (d3 >= FP.Zero && d4 <= d3)
            {
                s.P1 = b; s.A1 = s.A2; s.B1 = s.B2;
                s.Count = 1;
                containsOrigin = false;
                return -b;
            }

            FP vc = d1 * d4 - d3 * d2;
            if (vc <= FP.Zero && d1 >= FP.Zero && d3 <= FP.Zero)
            {
                FP v = d1 / (d1 - d3);
                s.P1 = a; s.P2 = b;
                s.Count = 2;
                containsOrigin = false;
                return -(a + ab * v);
            }

            FP d5 = Vector3.Dot(ab, cp);
            FP d6 = Vector3.Dot(ac, cp);
            if (d6 >= FP.Zero && d5 <= d6)
            {
                s.P1 = c; s.A1 = s.A3; s.B1 = s.B3;
                s.Count = 1;
                containsOrigin = false;
                return -c;
            }

            FP vb = d5 * d2 - d1 * d6;
            if (vb <= FP.Zero && d2 >= FP.Zero && d6 <= FP.Zero)
            {
                FP w = d2 / (d2 - d6);
                s.P1 = a; s.P2 = c;
                s.A2 = s.A3; s.B2 = s.B3;
                s.Count = 2;
                containsOrigin = false;
                return -(a + ac * w);
            }

            FP va = d3 * d6 - d5 * d4;
            if (va <= FP.Zero && (d4 - d3) >= FP.Zero && (d5 - d6) >= FP.Zero)
            {
                FP w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                s.P1 = b; s.P2 = c;
                s.A1 = s.A2; s.B1 = s.B2;
                s.A2 = s.A3; s.B2 = s.B3;
                s.Count = 2;
                containsOrigin = false;
                return -(b + (c - b) * w);
            }

            FP denom = FP.One / (va + vb + vc);
            FP vFinal = vb * denom;
            FP wFinal = vc * denom;
            containsOrigin = true;
            _ = vFinal; _ = wFinal;
            return Vector3.Zero;
        }

        private static Vector3 DoTetrahedron(ref Simplex s, out bool containsOrigin)
        {
            Vector3 a = s.P1;
            Vector3 b = s.P2;
            Vector3 c = s.P3;
            Vector3 d = s.P4;

            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ad = d - a;
            Vector3 bc = c - b;
            Vector3 bd = d - b;

            FP d1a = Vector3.Dot(Vector3.Cross(bd, bc), a - b);
            FP d1b = Vector3.Dot(Vector3.Cross(bd, bc), -b);
            bool faceBCDVisible = (d1a < FP.Zero && d1b > FP.Zero) || (d1a > FP.Zero && d1b < FP.Zero);

            FP d2a = Vector3.Dot(Vector3.Cross(ac, ad), b - a);
            FP d2b = Vector3.Dot(Vector3.Cross(ac, ad), -a);
            bool faceACDVisible = (d2a < FP.Zero && d2b > FP.Zero) || (d2a > FP.Zero && d2b < FP.Zero);

            FP d3a = Vector3.Dot(Vector3.Cross(ad, ab), c - a);
            FP d3b = Vector3.Dot(Vector3.Cross(ad, ab), -a);
            bool faceABDVisible = (d3a < FP.Zero && d3b > FP.Zero) || (d3a > FP.Zero && d3b < FP.Zero);

            FP d4a = Vector3.Dot(Vector3.Cross(ab, ac), d - a);
            FP d4b = Vector3.Dot(Vector3.Cross(ab, ac), -a);
            bool faceABCVisible = (d4a < FP.Zero && d4b > FP.Zero) || (d4a > FP.Zero && d4b < FP.Zero);

            if (!faceBCDVisible && !faceACDVisible && !faceABDVisible && !faceABCVisible)
            {
                containsOrigin = true;
                return Vector3.Zero;
            }

            if (!faceBCDVisible)
            {
                s.P1 = b; s.P2 = c; s.P3 = d;
                s.A1 = s.A2; s.B1 = s.B2;
                s.A2 = s.A3; s.B2 = s.B3;
                s.A3 = s.A4; s.B3 = s.B4;
                s.Count = 3;
                return DoTriangle(ref s, out containsOrigin);
            }
            if (!faceACDVisible)
            {
                s.P1 = a; s.P2 = c; s.P3 = d;
                s.A2 = s.A3; s.B2 = s.B3;
                s.A3 = s.A4; s.B3 = s.B4;
                s.Count = 3;
                return DoTriangle(ref s, out containsOrigin);
            }
            if (!faceABDVisible)
            {
                s.P1 = a; s.P2 = b; s.P3 = d;
                s.A3 = s.A4; s.B3 = s.B4;
                s.Count = 3;
                return DoTriangle(ref s, out containsOrigin);
            }

            s.P1 = a; s.P2 = b; s.P3 = c;
            s.Count = 3;
            return DoTriangle(ref s, out containsOrigin);
        }
    }
}