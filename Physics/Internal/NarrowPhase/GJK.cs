using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.NarrowPhase
{
    internal static class GJK
    {
        private const int MaxIterations = 32;
        private static readonly FP ToleranceSqr = FP.FromRawData(4294);

        public static bool Intersects<T0, T1>(in T0 a, in T1 b, out GjkResult result)
            where T0 : struct, ISupport
            where T1 : struct, ISupport
        {
            Vector3 initialDir = a.Support(Vector3.Right) - b.Support(Vector3.Left);
            if (initialDir.SqrMagnitude < FP.FromFloat(1e-20f))
            {
                initialDir = Vector3.Up;
            }

            Simplex simplex = default;
            Vector3 sa, sb;
            Vector3 support = SupportMinkowski.Compute(a, b, initialDir, out sa, out sb);
            simplex.Set(0, support, sa, sb);
            simplex.Count = 1;

            Vector3 direction = SimplexSolver.DoSimplex(ref simplex, out bool containsOrigin);

            for (int iter = 0; iter < MaxIterations; iter++)
            {
                if (containsOrigin)
                {
                    result = new GjkResult(true, simplex, iter + 1);
                    return true;
                }

                if (direction.SqrMagnitude < ToleranceSqr)
                {
                    result = new GjkResult(true, simplex, iter + 1);
                    return true;
                }

                Vector3 newSa, newSb;
                Vector3 newSupport = SupportMinkowski.Compute(a, b, direction, out newSa, out newSb);

                if (Vector3.Dot(newSupport, direction) < FP.Zero)
                {
                    result = new GjkResult(false, simplex, iter + 1);
                    return false;
                }

                simplex.Set(simplex.Count, newSupport, newSa, newSb);
                simplex.Count++;

                direction = SimplexSolver.DoSimplex(ref simplex, out containsOrigin);
            }

            result = new GjkResult(false, simplex, MaxIterations);
            return false;
        }
    }

    internal struct GjkResult
    {
        public bool Intersecting;
        public Simplex Simplex;
        public int Iterations;

        public GjkResult(bool intersecting, Simplex simplex, int iterations)
        {
            Intersecting = intersecting;
            Simplex = simplex;
            Iterations = iterations;
        }
    }
}