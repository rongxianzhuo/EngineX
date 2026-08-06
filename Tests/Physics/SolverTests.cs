using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;
using EngineX.Physics.Internal.Dynamics;
using EngineX.Physics.Internal.Solver;

namespace EngineX.Physics.Tests
{
    public static class SolverTests
    {
        private static ContactConstraint MakeContact(
            BodyHandle a, BodyHandle b,
            Vector3 normal, Vector3 point, FP penetration)
        {
            return new ContactConstraint
            {
                BodyA = a,
                BodyB = b,
                Normal = normal,
                Point = point,
                Penetration = penetration,
                Restitution = FP.Zero,
                Friction = FP.FromFloat(0.3f),
            };
        }

        [Test]
        public static void BallStopsOnStaticGround()
        {
            var bodies = new BodySet(8);
            var ground = Body.Defaults;
            ground.InverseMass = FP.Zero;
            ground.Flags = BodyFlags.Static;
            ground.Position = Vector3.Zero;
            var hGround = bodies.Add(ground);

            var ball = Body.Defaults;
            ball.InverseMass = FP.One;
            ball.Position = new Vector3(FP.Zero, FP.FromFloat(0.5f), FP.Zero);
            ball.LinearVelocity = new Vector3(FP.Zero, -FP.FromInt(5), FP.Zero);
            var hBall = bodies.Add(ball);

            var contacts = new ContactConstraint[1];
            contacts[0] = MakeContact(hBall, hGround,
                new Vector3(FP.Zero, -FP.One, FP.Zero),
                new Vector3(FP.Zero, FP.Zero, FP.Zero),
                FP.FromFloat(0.5f));

            FP dt = FP.FromFloat(0.01f);
            SequentialImpulses.PreStep(bodies, contacts, 1, dt);
            for (int i = 0; i < 8; i++)
            {
                SequentialImpulses.SolveVelocity(bodies, contacts, 1);
            }

            bodies.TryGet(hBall, out var updated);
            TestRunner.Assert(updated.LinearVelocity.Y >= -FP.FromFloat(0.5f),
                $"ball y-velocity should be stopped, got {updated.LinearVelocity.Y}");
        }

        [Test]
        public static void StaticContactDoesNotMoveStaticBody()
        {
            var bodies = new BodySet(8);
            var a = Body.Defaults;
            a.InverseMass = FP.Zero;
            a.Flags = BodyFlags.Static;
            var hA = bodies.Add(a);

            var b = Body.Defaults;
            b.InverseMass = FP.One;
            b.Position = new Vector3(FP.One, FP.Zero, FP.Zero);
            b.LinearVelocity = new Vector3(-FP.FromInt(2), FP.Zero, FP.Zero);
            var hB = bodies.Add(b);

            var contacts = new ContactConstraint[1];
            contacts[0] = MakeContact(hA, hB,
                new Vector3(FP.One, FP.Zero, FP.Zero),
                new Vector3(FP.One, FP.Zero, FP.Zero),
                FP.FromFloat(0.5f));

            SequentialImpulses.PreStep(bodies, contacts, 1, FP.FromFloat(0.01f));
            for (int i = 0; i < 8; i++)
            {
                SequentialImpulses.SolveVelocity(bodies, contacts, 1);
            }

            bodies.TryGet(hA, out var stillStatic);
            TestRunner.AssertEqual(FP.Zero, stillStatic.LinearVelocity.X,
                "static body should not gain velocity");
        }

        [Test]
        public static void TwoMovingBodiesSeparateAfterCollision()
        {
            var bodies = new BodySet(8);
            var a = Body.Defaults;
            a.InverseMass = FP.One;
            a.Position = Vector3.Zero;
            a.LinearVelocity = new Vector3(FP.One, FP.Zero, FP.Zero);
            var hA = bodies.Add(a);

            var b = Body.Defaults;
            b.InverseMass = FP.One;
            b.Position = new Vector3(FP.FromFloat(0.5f), FP.Zero, FP.Zero);
            b.LinearVelocity = new Vector3(-FP.One, FP.Zero, FP.Zero);
            var hB = bodies.Add(b);

            var contacts = new ContactConstraint[1];
            contacts[0] = MakeContact(hA, hB,
                new Vector3(FP.One, FP.Zero, FP.Zero),
                new Vector3(FP.FromFloat(0.25f), FP.Zero, FP.Zero),
                FP.FromFloat(0.5f));

            SequentialImpulses.PreStep(bodies, contacts, 1, FP.FromFloat(0.01f));
            for (int i = 0; i < 8; i++)
            {
                SequentialImpulses.SolveVelocity(bodies, contacts, 1);
            }

            bodies.TryGet(hA, out var bodyA);
            bodies.TryGet(hB, out var bodyB);
            TestRunner.Assert(bodyA.LinearVelocity.X < FP.One,
                $"a should slow down, got vx={bodyA.LinearVelocity.X}");
            TestRunner.Assert(bodyB.LinearVelocity.X > -FP.One,
                $"b should slow down (or reverse), got vx={bodyB.LinearVelocity.X}");
        }

        [Test]
        public static void NormalImpulseIsNonNegative()
        {
            var bodies = new BodySet(8);
            var a = Body.Defaults;
            a.InverseMass = FP.One;
            a.Position = Vector3.Zero;
            a.LinearVelocity = new Vector3(FP.Zero, FP.FromInt(5), FP.Zero);
            var hA = bodies.Add(a);

            var b = Body.Defaults;
            b.InverseMass = FP.Zero;
            b.Flags = BodyFlags.Static;
            b.Position = new Vector3(FP.Zero, FP.FromFloat(0.1f), FP.Zero);
            var hB = bodies.Add(b);

            var contacts = new ContactConstraint[1];
            contacts[0] = MakeContact(hA, hB,
                new Vector3(FP.Zero, FP.One, FP.Zero),
                new Vector3(FP.Zero, FP.FromFloat(0.1f), FP.Zero),
                FP.FromFloat(0.5f));

            SequentialImpulses.PreStep(bodies, contacts, 1, FP.FromFloat(0.01f));
            SequentialImpulses.SolveVelocity(bodies, contacts, 1);
            TestRunner.Assert(contacts[0].NormalImpulse >= FP.Zero,
                $"normal impulse should be >= 0, got {contacts[0].NormalImpulse}");
        }

        [Test]
        public static void FrictionImpulseIsBounded()
        {
            var bodies = new BodySet(8);
            var a = Body.Defaults;
            a.InverseMass = FP.Zero;
            a.Flags = BodyFlags.Static;
            a.Position = Vector3.Zero;
            var hA = bodies.Add(a);

            var b = Body.Defaults;
            b.InverseMass = FP.One;
            b.Position = new Vector3(FP.Zero, FP.FromFloat(0.5f), FP.Zero);
            b.LinearVelocity = new Vector3(FP.FromInt(10), FP.Zero, FP.Zero);
            var hB = bodies.Add(b);

            var contacts = new ContactConstraint[1];
            contacts[0] = MakeContact(hB, hA,
                new Vector3(FP.Zero, FP.One, FP.Zero),
                new Vector3(FP.Zero, FP.Zero, FP.Zero),
                FP.FromFloat(0.5f));

            SequentialImpulses.PreStep(bodies, contacts, 1, FP.FromFloat(0.01f));
            for (int i = 0; i < 16; i++)
            {
                SequentialImpulses.SolveVelocity(bodies, contacts, 1);
            }

            FP t1 = contacts[0].TangentImpulse1;
            FP t2 = contacts[0].TangentImpulse2;
            FP n = contacts[0].NormalImpulse;
            FP friction = contacts[0].Friction;
            FP t1Sqr = t1 * t1;
            FP t2Sqr = t2 * t2;
            FP maxSqr = (friction * n) * (friction * n);
            TestRunner.Assert(t1Sqr <= maxSqr * FP.FromFloat(1.5f),
                $"t1 impulse {t1} should be bounded by friction*N={friction * n}");
        }

        [Test]
        public static void RestitutionAddsBias()
        {
            var bodies = new BodySet(8);
            var a = Body.Defaults;
            a.InverseMass = FP.One;
            a.Position = new Vector3(FP.Zero, FP.FromFloat(0.5f), FP.Zero);
            a.LinearVelocity = new Vector3(FP.Zero, -FP.FromInt(5), FP.Zero);
            var hA = bodies.Add(a);

            var b = Body.Defaults;
            b.InverseMass = FP.Zero;
            b.Flags = BodyFlags.Static;
            b.Position = Vector3.Zero;
            var hB = bodies.Add(b);

            var contacts = new ContactConstraint[1];
            contacts[0] = MakeContact(hA, hB,
                new Vector3(FP.Zero, FP.One, FP.Zero),
                new Vector3(FP.Zero, FP.Zero, FP.Zero),
                FP.FromFloat(0.5f));
            contacts[0] = new ContactConstraint
            {
                BodyA = hA,
                BodyB = hB,
                Normal = new Vector3(FP.Zero, -FP.One, FP.Zero),
                Point = new Vector3(FP.Zero, FP.Zero, FP.Zero),
                Penetration = FP.FromFloat(0.5f),
                Restitution = FP.FromFloat(0.5f),
                Friction = FP.Zero,
            };

            SequentialImpulses.PreStep(bodies, contacts, 1, FP.FromFloat(0.01f));
            TestRunner.Assert(contacts[0].Bias > FP.Zero,
                $"restitution bias should be positive, got {contacts[0].Bias}");
        }

        [Test]
        public static void PositionCorrectionSeparatesOverlapping()
        {
            var bodies = new BodySet(8);
            var a = Body.Defaults;
            a.InverseMass = FP.One;
            a.Position = Vector3.Zero;
            var hA = bodies.Add(a);

            var b = Body.Defaults;
            b.InverseMass = FP.One;
            b.Position = new Vector3(FP.FromFloat(0.5f), FP.Zero, FP.Zero);
            var hB = bodies.Add(b);

            var contacts = new ContactConstraint[1];
            contacts[0] = MakeContact(hA, hB,
                new Vector3(FP.One, FP.Zero, FP.Zero),
                new Vector3(FP.FromFloat(0.25f), FP.Zero, FP.Zero),
                FP.FromFloat(0.5f));

            SequentialImpulses.PreStep(bodies, contacts, 1, FP.FromFloat(0.01f));
            SequentialImpulses.SolvePosition(bodies, contacts, 1);

            bodies.TryGet(hA, out var bodyA);
            bodies.TryGet(hB, out var bodyB);
            TestRunner.Assert(bodyA.Position.X < FP.Zero,
                $"a should move left, got {bodyA.Position.X}");
            TestRunner.Assert(bodyB.Position.X > FP.FromFloat(0.5f),
                $"b should move right, got {bodyB.Position.X}");
        }

        [Test]
        public static void EffectiveMassIsReciprocalOfK()
        {
            var bodies = new BodySet(8);
            var a = Body.Defaults;
            a.InverseMass = FP.One;
            var hA = bodies.Add(a);

            var b = Body.Defaults;
            b.InverseMass = FP.One;
            var hB = bodies.Add(b);

            var contacts = new ContactConstraint[1];
            contacts[0] = new ContactConstraint
            {
                BodyA = hA,
                BodyB = hB,
                Normal = new Vector3(FP.Zero, FP.One, FP.Zero),
                Point = Vector3.Zero,
                Penetration = FP.One,
                Restitution = FP.Zero,
                Friction = FP.Zero,
            };

            SequentialImpulses.PreStep(bodies, contacts, 1, FP.FromFloat(0.01f));
            TestRunner.Assert((contacts[0].EffectiveMassNormal - FP.Half).Abs() < FP.FromFloat(0.01f),
                $"effective mass should be ~0.5 for invMass 1+1, got {contacts[0].EffectiveMassNormal}");
        }
    }
}