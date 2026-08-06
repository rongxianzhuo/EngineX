using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;
using EngineX.Physics.Internal.Dynamics;

namespace EngineX.Physics.Tests
{
    public static class PhysicsWorldTests
    {
        [Test]
        public static void AddBodyIncreasesCount()
        {
            var world = new PhysicsWorld(16);
            TestRunner.AssertEqual(0, world.BodyCount);
            var body = Body.Defaults;
            world.AddBody(body);
            TestRunner.AssertEqual(1, world.BodyCount);
        }

        [Test]
        public static void RemoveBodyDecreasesCount()
        {
            var world = new PhysicsWorld(16);
            var body = Body.Defaults;
            var h = world.AddBody(body);
            world.RemoveBody(h);
            TestRunner.AssertEqual(0, world.BodyCount);
        }

        [Test]
        public static void FreeFallBodyMovesUnderGravity()
        {
            var world = new PhysicsWorld(16);
            var body = Body.Defaults;
            body.Position = new Vector3(FP.Zero, FP.FromInt(10), FP.Zero);
            body.InverseMass = FP.One;
            var h = world.AddBody(body);

            FP dt = FP.FromFloat(0.01f);
            for (int i = 0; i < 100; i++)
            {
                world.Step(dt);
            }

            world.TryGetBody(h, out var updated);
            TestRunner.Assert(updated.Position.Y < FP.FromInt(5),
                $"body should have fallen, got y={updated.Position.Y}");
        }

        [Test]
        public static void StaticBodyDoesNotMoveUnderGravity()
        {
            var world = new PhysicsWorld(16);
            var body = Body.Defaults;
            body.InverseMass = FP.Zero;
            body.Flags = BodyFlags.Static;
            var h = world.AddBody(body);

            for (int i = 0; i < 100; i++)
            {
                world.Step(FP.FromFloat(0.01f));
            }

            world.TryGetBody(h, out var updated);
            TestRunner.AssertEqual(FP.Zero, updated.Position.Y,
                "static body should not move");
        }

        [Test]
        public static void SleepingBodySkippedInStep()
        {
            var world = new PhysicsWorld(16, Vector3.Zero);
            world.FramesToSleep = 5;
            var body = Body.Defaults;
            body.InverseMass = FP.One;
            var h = world.AddBody(body);

            for (int i = 0; i < 5; i++)
            {
                world.Step(FP.FromFloat(0.01f));
            }

            world.TryGetBody(h, out var sleeping);
            TestRunner.Assert((sleeping.Flags & BodyFlags.Sleeping) != 0, "should be sleeping");

            Vector3 oldPos = sleeping.Position;
            for (int i = 0; i < 10; i++)
            {
                world.Step(FP.FromFloat(0.01f));
            }
            world.TryGetBody(h, out var afterSleep);
            TestRunner.AssertEqual(oldPos.X, afterSleep.Position.X,
                "sleeping body position should not change");
        }

        [Test]
        public static void BroadphaseTracksMovedBody()
        {
            var world = new PhysicsWorld(16);
            var body = Body.Defaults;
            body.InverseMass = FP.One;
            var h = world.AddBody(body);

            for (int i = 0; i < 10; i++)
            {
                world.Step(FP.FromFloat(0.01f));
            }

            int leaves = world.BroadphaseLeafCount;
            TestRunner.AssertEqual(1, leaves, "broadphase should have 1 leaf");
        }

        [Test]
        public static void MultipleBodiesBroadphaseHasManyLeaves()
        {
            var world = new PhysicsWorld(64);
            for (int i = 0; i < 10; i++)
            {
                var body = Body.Defaults;
                body.InverseMass = FP.One;
                body.Position = new Vector3(FP.FromInt(i), FP.Zero, FP.Zero);
                world.AddBody(body);
            }
            TestRunner.AssertEqual(10, world.BroadphaseLeafCount);
        }

        [Test]
        public static void RemovedBodyBroadphaseLeafCountDecreases()
        {
            var world = new PhysicsWorld(64);
            var handles = new BodyHandle[5];
            for (int i = 0; i < 5; i++)
            {
                var body = Body.Defaults;
                body.InverseMass = FP.One;
                handles[i] = world.AddBody(body);
            }
            TestRunner.AssertEqual(5, world.BroadphaseLeafCount);
            world.RemoveBody(handles[2]);
            TestRunner.AssertEqual(4, world.BroadphaseLeafCount);
        }
    }
}