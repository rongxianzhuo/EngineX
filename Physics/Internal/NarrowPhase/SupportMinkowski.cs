using System.Runtime.CompilerServices;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.NarrowPhase
{
    internal static class SupportMinkowski
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Compute<T0, T1>(in T0 a, in T1 b, Vector3 direction,
            out Vector3 supportA, out Vector3 supportB)
            where T0 : struct, ISupport
            where T1 : struct, ISupport
        {
            supportA = a.Support(direction);
            supportB = b.Support(-direction);
            return supportA - supportB;
        }
    }
}