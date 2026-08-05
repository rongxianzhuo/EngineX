using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.ECS.Tests
{
    public static class BaselineVector3Tests
    {
        [Test]
        public static void ComponentWiseMultiply()
        {
            var a = new Vector3(FP.FromInt(2), FP.FromInt(3), FP.FromInt(4));
            var b = new Vector3(FP.FromInt(5), FP.FromInt(6), FP.FromInt(7));
            var r = a * b;
            TestRunner.AssertEqual(new Vector3(FP.FromInt(10), FP.FromInt(18), FP.FromInt(28)), r);
        }

        [Test]
        public static void ComponentWiseMultiplyIsCommutative()
        {
            var a = new Vector3(FP.FromInt(2), FP.FromInt(-3), FP.FromInt(4));
            var b = new Vector3(FP.FromInt(5), FP.FromInt(6), FP.FromInt(-7));
            TestRunner.AssertEqual(a * b, b * a);
        }

        [Test]
        public static void ComponentWiseMultiplyIdentityAndZero()
        {
            var v = new Vector3(FP.FromInt(3), FP.FromInt(4), FP.FromInt(5));
            TestRunner.AssertEqual(v, v * Vector3.One);
            TestRunner.AssertEqual(Vector3.Zero, v * Vector3.Zero);
        }

        [Test]
        public static void ComponentWiseMultiplyMatchesScalarMultiply()
        {
            var a = new Vector3(FP.FromInt(2), FP.FromInt(3), FP.FromInt(4));
            var scalar = FP.FromInt(5);
            var b = new Vector3(scalar, scalar, scalar);
            TestRunner.AssertEqual(a * scalar, a * b);
        }
    }
}
