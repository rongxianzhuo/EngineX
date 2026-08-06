using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics.Internal.NarrowPhase;

namespace EngineX.Physics
{
    public static class ShapeOverlap
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OverlapResult Compute<T0, T1>(in T0 a, in T1 b)
            where T0 : struct, ISupport
            where T1 : struct, ISupport
        {
            if (!GJK.Intersects(a, b, out var gjk))
            {
                return new OverlapResult(false, Vector3.Zero, FP.Zero, gjk.Iterations);
            }

            PenetrationExtractor.Extract(gjk.Simplex, out Vector3 normal, out FP depth);

            Vector3 pointOnA = gjk.Simplex.P1;
            Vector3 pointOnB = gjk.Simplex.P1 - normal * depth;
            _ = pointOnB;

            return new OverlapResult(true, normal, depth, gjk.Iterations);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Manifold ComputeManifold<T0, T1>(in T0 a, in T1 b)
            where T0 : struct, ISupport
            where T1 : struct, ISupport
        {
            if (!GJK.Intersects(a, b, out var gjk))
            {
                return Manifold.Empty;
            }

            PenetrationExtractor.Extract(gjk.Simplex, out Vector3 normal, out FP depth);

            Vector3 sa = gjk.Simplex.A1;
            Vector3 sb = gjk.Simplex.B1;

            return new Manifold(sa, sb, normal, depth);
        }
    }
}