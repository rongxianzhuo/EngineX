using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;
using EngineX.Physics.Internal.Dynamics;

namespace EngineX.Physics.Tests
{
    public static class IntegratorTests
    {
        private static readonly FP Tolerance = FP.FromFloat(0.01f);

        [Test]
        public static void StaticBodyDoesNotMove()
        {
            var body = Body.Defaults;
            body.InverseMass = FP.Zero;
            body.Force = new Vector3(FP.FromInt(100), FP.FromInt(100), FP.Zero);
            Integrator.Integrate(ref body, FP.FromFloat(0.1f));
            TestRunner.AssertEqual(FP.Zero, body.Position.X, "static should not move on X");
            TestRunner.AssertEqual(FP.Zero, body.Position.Y, "static should not move on Y");
        }

        [Test]
        public static void FreeFallOneSecond()
        {
            var body = Body.Defaults;
            body.Position = new Vector3(FP.Zero, FP.FromInt(10), FP.Zero);
            body.LinearVelocity = Vector3.Zero;
            body.InverseMass = FP.One;

            Vector3 gravity = new Vector3(FP.Zero, -FP.FromInt(10), FP.Zero);
            FP dt = FP.FromFloat(0.01f);
            for (int i = 0; i < 100; i++)
            {
                Integrator.ApplyGravity(ref body, gravity, dt);
                Integrator.IntegrateLinear(ref body, dt);
            }

            FP expectedV = -FP.FromInt(10);
            TestRunner.Assert((body.LinearVelocity.Y - expectedV).Abs() < Tolerance,
                $"expected velocity -10, got {body.LinearVelocity.Y}");
            TestRunner.Assert(body.Position.Y < FP.FromInt(5),
                $"position should drop, got {body.Position.Y}");
        }

        [Test]
        public static void ConstantVelocityLinearMotion()
        {
            var body = Body.Defaults;
            body.LinearVelocity = new Vector3(FP.FromInt(2), FP.Zero, FP.Zero);
            body.InverseMass = FP.One;

            FP dt = FP.FromFloat(0.1f);
            for (int i = 0; i < 10; i++)
            {
                Integrator.IntegrateLinear(ref body, dt);
            }
            TestRunner.Assert((body.Position.X - FP.FromInt(2)).Abs() < Tolerance,
                $"after 1s at v=2, x should be 2, got {body.Position.X}");
            TestRunner.AssertEqual(FP.Zero, body.Position.Y, "no Y motion expected");
        }

        [Test]
        public static void ForceAcceleratesBody()
        {
            var body = Body.Defaults;
            body.InverseMass = FP.One;
            body.Force = new Vector3(FP.FromInt(10), FP.Zero, FP.Zero);

            FP dt = FP.FromFloat(0.1f);
            Integrator.Integrate(ref body, dt);
            TestRunner.Assert((body.LinearVelocity.X - FP.FromInt(1)).Abs() < FP.FromFloat(0.001f),
                $"after F=10*dt=0.1*invMass=1, v should be 1, got {body.LinearVelocity.X}");
        }

        [Test]
        public static void ZeroForceZeroAcceleration()
        {
            var body = Body.Defaults;
            body.InverseMass = FP.One;
            body.LinearVelocity = new Vector3(FP.FromInt(5), FP.Zero, FP.Zero);

            Integrator.Integrate(ref body, FP.FromFloat(0.1f));
            TestRunner.Assert((body.LinearVelocity.X - FP.FromInt(5)).Abs() < FP.FromFloat(0.001f),
                "zero force should not change velocity");
            TestRunner.Assert((body.Position.X - FP.FromFloat(0.5f)).Abs() < FP.FromFloat(0.001f),
                $"velocity 5 over dt=0.1 should move 0.5, got {body.Position.X}");
        }

        [Test]
        public static void ForceClearedAfterIntegration()
        {
            var body = Body.Defaults;
            body.InverseMass = FP.One;
            body.Force = new Vector3(FP.FromInt(10), FP.Zero, FP.Zero);

            Integrator.Integrate(ref body, FP.FromFloat(0.1f));
            TestRunner.AssertEqual(FP.Zero, body.Force.X,
                $"force should be cleared after integrate, got {body.Force.X}");
        }

        [Test]
        public static void OrientationStaysNormalizedAfterIntegration()
        {
            var body = Body.Defaults;
            body.InverseMass = FP.One;
            body.InverseInertia = FP.One;
            body.AngularVelocity = new Vector3(FP.One, FP.Zero, FP.Zero);

            for (int i = 0; i < 100; i++)
            {
                Integrator.Integrate(ref body, FP.FromFloat(0.01f));
            }
            FP sqrSum = body.Orientation.X * body.Orientation.X
                + body.Orientation.Y * body.Orientation.Y
                + body.Orientation.Z * body.Orientation.Z
                + body.Orientation.W * body.Orientation.W;
            FP qMag = sqrSum.Sqrt;
            TestRunner.Assert((qMag - FP.One).Abs() < FP.FromFloat(0.001f),
                $"orientation should stay normalized, magnitude={qMag}");
        }

        [Test]
        public static void FreeFallPositionMatchesAnalytic()
        {
            var body = Body.Defaults;
            body.InverseMass = FP.One;
            body.Position = Vector3.Zero;
            body.LinearVelocity = Vector3.Zero;

            Vector3 gravity = new Vector3(FP.Zero, -FP.FromInt(9), FP.Zero);
            FP dt = FP.FromFloat(0.005f);
            for (int i = 0; i < 100; i++)
            {
                Integrator.ApplyGravity(ref body, gravity, dt);
                Integrator.IntegrateLinear(ref body, dt);
            }

            FP expectedV = gravity.Y * FP.FromFloat(0.5f);
            TestRunner.Assert((body.LinearVelocity.Y - expectedV).Abs() < FP.FromFloat(0.01f),
                $"expected velocity {expectedV}, got {body.LinearVelocity.Y}");
            FP expectedY = FP.FromFloat(0.5f) * gravity.Y * FP.FromFloat(0.25f);
            TestRunner.Assert((body.Position.Y - expectedY).Abs() < FP.FromFloat(0.05f),
                $"expected position {expectedY}, got {body.Position.Y}");
        }
    }
}