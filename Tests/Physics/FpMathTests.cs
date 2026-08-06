using System;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics.Internal;

namespace EngineX.Physics.Tests
{
    public static class FpMathTests
    {
        private const float AbsEps = 1e-4f;
        private const float Atan2AbsEps = 0.01f;

        private static void AssertApproxFloat(FP expectedFp, double expectedDouble, FP actual, string what)
        {
            float actualFloat = actual.Single();
            float expectedFloat = (float)expectedDouble;
            float diff = Math.Abs(actualFloat - expectedFloat);
            if (diff > AbsEps)
            {
                throw new AssertionException(
                    $"{what}: expected ~{expectedFloat} (double {expectedDouble}), actual {actualFloat}, diff {diff}");
            }
            _ = expectedFp;
        }

        private static void AssertApproxFloatRaw(FP actual, double expectedDouble, float eps, string what)
        {
            float actualFloat = actual.Single();
            float expectedFloat = (float)expectedDouble;
            float diff = Math.Abs(actualFloat - expectedFloat);
            if (diff > eps)
            {
                throw new AssertionException(
                    $"{what}: expected ~{expectedFloat} (double {expectedDouble}), actual {actualFloat}, diff {diff}");
            }
        }

        [Test]
        public static void SqrtMatchesDoubleForSmallValues()
        {
            AssertApproxFloatRaw(FP.FromInt(2).Sqrt, Math.Sqrt(2.0), AbsEps, "sqrt(2)");
            AssertApproxFloatRaw(FP.FromInt(3).Sqrt, Math.Sqrt(3.0), AbsEps, "sqrt(3)");
            AssertApproxFloatRaw(FP.FromInt(5).Sqrt, Math.Sqrt(5.0), AbsEps, "sqrt(5)");
            AssertApproxFloatRaw(FP.FromInt(10).Sqrt, Math.Sqrt(10.0), AbsEps, "sqrt(10)");
        }

        [Test]
        public static void SqrtOfFractionalValues()
        {
            FP half = FP.FromFloat(0.5f);
            FP quarter = FP.FromFloat(0.25f);
            FP tenth = FP.FromFloat(0.1f);
            AssertApproxFloatRaw(half.Sqrt, Math.Sqrt(0.5), AbsEps, "sqrt(0.5)");
            AssertApproxFloatRaw(quarter.Sqrt, Math.Sqrt(0.25), AbsEps, "sqrt(0.25)");
            AssertApproxFloatRaw(tenth.Sqrt, Math.Sqrt(0.1), AbsEps, "sqrt(0.1)");
        }

        [Test]
        public static void SqrtOfZeroIsZero()
        {
            TestRunner.AssertEqual(FP.Zero, FP.Zero.Sqrt);
        }

        [Test]
        public static void SqrtOfOneIsOne()
        {
            TestRunner.AssertEqual(FP.One, FP.One.Sqrt);
        }

        [Test]
        public static void SqrtOfNegativeThrows()
        {
            try
            {
                var _ = (-FP.One).Sqrt;
                throw new AssertionException("expected ArgumentOutOfRangeException for sqrt(-1)");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        [Test]
        public static void Atan2FirstQuadrant()
        {
            FP result = FpMath.Atan2(FP.One, FP.One);
            AssertApproxFloatRaw(result, Math.Atan2(1.0, 1.0), Atan2AbsEps, "atan2(1,1)");
        }

        [Test]
        public static void Atan2OnAxes()
        {
            AssertApproxFloatRaw(FpMath.Atan2(FP.Zero, FP.One), Math.Atan2(0.0, 1.0), Atan2AbsEps, "atan2(0,1)");
            AssertApproxFloatRaw(FpMath.Atan2(FP.One, FP.Zero), Math.Atan2(1.0, 0.0), Atan2AbsEps, "atan2(1,0)");
            AssertApproxFloatRaw(FpMath.Atan2(FP.Zero, -FP.One), Math.Atan2(0.0, -1.0), Atan2AbsEps, "atan2(0,-1)");
            AssertApproxFloatRaw(FpMath.Atan2(-FP.One, FP.Zero), Math.Atan2(-1.0, 0.0), Atan2AbsEps, "atan2(-1,0)");
        }

        [Test]
        public static void Atan2ThirdQuadrant()
        {
            FP result = FpMath.Atan2(-FP.One, -FP.One);
            AssertApproxFloatRaw(result, Math.Atan2(-1.0, -1.0), Atan2AbsEps, "atan2(-1,-1)");
        }

        [Test]
        public static void Atan2Asymmetric()
        {
            FP result = FpMath.Atan2(FP.FromInt(3), FP.FromInt(5));
            AssertApproxFloatRaw(result, Math.Atan2(3.0, 5.0), Atan2AbsEps, "atan2(3,5)");
        }

        [Test]
        public static void InvSqrtOfOneIsOne()
        {
            TestRunner.AssertEqual(FP.One, FpMathExtensions.InvSqrt(FP.One));
        }

        [Test]
        public static void InvSqrtOfFourIsHalf()
        {
            AssertApproxFloatRaw(FpMathExtensions.InvSqrt(FP.FromInt(4)), 0.5, AbsEps, "invsqrt(4)");
        }

        [Test]
        public static void InvSqrtOfNineIsThird()
        {
            AssertApproxFloatRaw(FpMathExtensions.InvSqrt(FP.FromInt(9)), 1.0 / 3.0, AbsEps, "invsqrt(9)");
        }

        [Test]
        public static void InvSqrtOfFractional()
        {
            AssertApproxFloatRaw(FpMathExtensions.InvSqrt(FP.FromFloat(0.25f)), 2.0, AbsEps, "invsqrt(0.25)");
            AssertApproxFloatRaw(FpMathExtensions.InvSqrt(FP.FromFloat(2.0f)), 1.0 / Math.Sqrt(2.0), AbsEps, "invsqrt(2)");
        }

        [Test]
        public static void InvSqrtOfZeroIsZero()
        {
            TestRunner.AssertEqual(FP.Zero, FpMathExtensions.InvSqrt(FP.Zero));
        }

        [Test]
        public static void InvSqrtOfNegativeIsZero()
        {
            TestRunner.AssertEqual(FP.Zero, FpMathExtensions.InvSqrt(-FP.One));
        }

        [Test]
        public static void InvSqrtMatchesDoubleForVarious()
        {
            foreach (var v in new float[] { 0.5f, 1f, 2f, 3f, 5f, 10f, 100f })
            {
                FP fpVal = FP.FromFloat(v);
                double expected = 1.0 / Math.Sqrt(v);
                AssertApproxFloatRaw(FpMathExtensions.InvSqrt(fpVal), expected, AbsEps, $"invsqrt({v})");
            }
        }
    }
}
