using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;

namespace EngineX.Physics.Internal
{
    public static class FpMathExtensions
    {
        private static readonly FP OnePointFive = FP.FromInt(3) * FP.Half;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP InvSqrt(FP x)
        {
            if (x.RawData <= 0) return FP.Zero;
            FP s = x.Sqrt;
            return s == FP.Zero ? FP.Zero : FP.One / s;
        }
    }
}
