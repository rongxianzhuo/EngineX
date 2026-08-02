
namespace EngineX.Baseline.FixedPoint
{
    public static class FpMath
    {

        public static FP Sin(FP x)
        {
            return SinLookupTable.Lookup(x);
        }

        public static FP Cos(FP x)
        {
            return CosLookupTable.Lookup(x);
        }

        public static FP Acos(FP x)
        {
            return AcosLookupTable.Lookup(x);
        }

        public static FP Asin(FP x)
        {
            return FP.PI / 2 - Acos(x);
        }

        public static FP Atan2(FP y, FP x)
        {
            var r = (x * x + y * y).Sqrt;
            if (r == FP.Zero) return FP.Zero;
            var theta = Acos(x / r);
            return y.RawData < 0 ? -theta : theta;
        }

    }
}