using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;
using EngineX.Physics.Internal.Dynamics;

namespace EngineX.Physics.Tests
{
    public static class SleepingTests
    {
        [Test]
        public static void StaticBodyNeverSleeps()
        {
            var body = Body.Defaults;
            body.Flags = BodyFlags.Static;
            body.LinearVelocity = Vector3.Zero;
            body.AngularVelocity = Vector3.Zero;

            for (int i = 0; i < 100; i++)
            {
                SleepingSystem.Update(ref body, FP.FromFloat(0.01f), FP.FromFloat(0.01f), 10);
            }
            TestRunner.Assert((body.Flags & BodyFlags.Sleeping) == 0,
                "static body should never sleep");
        }

        [Test]
        public static void StationaryBodyEntersSleep()
        {
            var body = Body.Defaults;
            body.InverseMass = FP.One;
            body.LinearVelocity = Vector3.Zero;
            body.AngularVelocity = Vector3.Zero;

            int framesToSleep = 10;
            for (int i = 0; i < framesToSleep; i++)
            {
                SleepingSystem.Update(ref body, FP.FromFloat(0.01f), FP.FromFloat(0.01f), framesToSleep);
            }
            TestRunner.Assert((body.Flags & BodyFlags.Sleeping) != 0,
                "stationary body should sleep after framesToSleep");
        }

        [Test]
        public static void MovingBodyDoesNotSleep()
        {
            var body = Body.Defaults;
            body.InverseMass = FP.One;
            body.LinearVelocity = new Vector3(FP.One, FP.Zero, FP.Zero);

            for (int i = 0; i < 100; i++)
            {
                SleepingSystem.Update(ref body, FP.FromFloat(0.01f), FP.FromFloat(0.01f), 10);
            }
            TestRunner.Assert((body.Flags & BodyFlags.Sleeping) == 0,
                "moving body should not sleep");
        }

        [Test]
        public static void WakeUpRestoresFlag()
        {
            var body = Body.Defaults;
            body.InverseMass = FP.One;
            for (int i = 0; i < 10; i++)
            {
                SleepingSystem.Update(ref body, FP.FromFloat(0.01f), FP.FromFloat(0.01f), 5);
            }
            TestRunner.Assert((body.Flags & BodyFlags.Sleeping) != 0, "should be sleeping");

            SleepingSystem.WakeUp(ref body);
            TestRunner.Assert((body.Flags & BodyFlags.Sleeping) == 0, "wake should clear sleeping");
            TestRunner.Assert((body.Flags & BodyFlags.Awake) != 0, "wake should set awake");
        }

        [Test]
        public static void SleepResetsVelocity()
        {
            var body = Body.Defaults;
            body.InverseMass = FP.One;
            body.LinearVelocity = new Vector3(FP.FromFloat(0.001f), FP.Zero, FP.Zero);

            for (int i = 0; i < 10; i++)
            {
                SleepingSystem.Update(ref body, FP.FromFloat(0.01f), FP.FromFloat(0.01f), 5);
            }
            TestRunner.Assert((body.Flags & BodyFlags.Sleeping) != 0, "should be sleeping");
            TestRunner.AssertEqual(FP.Zero, body.LinearVelocity.X,
                "velocity should be zeroed when sleeping");
        }

        [Test]
        public static void SleepTimerResetsOnMovement()
        {
            var body = Body.Defaults;
            body.InverseMass = FP.One;
            int framesToSleep = 10;

            for (int i = 0; i < 8; i++)
            {
                SleepingSystem.Update(ref body, FP.FromFloat(0.01f), FP.FromFloat(0.01f), framesToSleep);
            }
            int timerAfter8 = body.SleepTimer;
            TestRunner.AssertEqual(8, timerAfter8, $"sleep timer after 8 frames, got {timerAfter8}");

            body.LinearVelocity = new Vector3(FP.One, FP.Zero, FP.Zero);
            SleepingSystem.Update(ref body, FP.FromFloat(0.01f), FP.FromFloat(0.01f), framesToSleep);
            TestRunner.AssertEqual(0, body.SleepTimer,
                $"sleep timer should reset after movement, got {body.SleepTimer}");
        }

        [Test]
        public static void SleepingBodyIsNotReUpdated()
        {
            var body = Body.Defaults;
            body.InverseMass = FP.One;
            for (int i = 0; i < 5; i++)
            {
                SleepingSystem.Update(ref body, FP.FromFloat(0.01f), FP.FromFloat(0.01f), 5);
            }
            TestRunner.Assert((body.Flags & BodyFlags.Sleeping) != 0, "should be sleeping");

            body.SleepTimer = 999;
            SleepingSystem.Update(ref body, FP.FromFloat(0.01f), FP.FromFloat(0.01f), 5);
            TestRunner.AssertEqual(999, body.SleepTimer,
                "sleeping body timer should not be modified");
        }

        [Test]
        public static void BodySetUpdateAllSleeps()
        {
            var bodies = new BodySet(64);
            for (int i = 0; i < 10; i++)
            {
                var b = Body.Defaults;
                b.InverseMass = FP.One;
                bodies.Add(b);
            }
            for (int i = 0; i < 5; i++)
            {
                SleepingSystem.UpdateAll(bodies, FP.FromFloat(0.01f), FP.FromFloat(0.01f), 5);
            }
            int sleepCount = 0;
            for (int i = 0; i < bodies.Capacity; i++)
            {
                var h = bodies.GetHandle(i);
                if (!bodies.IsValid(h)) continue;
                var b = bodies.Get(h);
                if ((b.Flags & BodyFlags.Sleeping) != 0) sleepCount++;
            }
            TestRunner.AssertEqual(10, sleepCount, $"all 10 should be sleeping, got {sleepCount}");
        }
    }
}