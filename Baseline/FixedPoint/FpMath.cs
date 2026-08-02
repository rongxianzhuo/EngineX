
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
        
    }
}