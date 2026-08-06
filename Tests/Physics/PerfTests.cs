using System.Diagnostics;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;

namespace EngineX.Physics.Tests
{
    public static class PerfTests
    {
        private const long TicksPerMillisecond = 10000L * 1000L;

        private static PhysicsWorld BuildGrid(
            int count, out BodyHandle[] handles)
        {
            var world = new PhysicsWorld(System.Math.Max(count * 2, 256), new Vector3(FP.Zero, -FP.FromInt(10), FP.Zero));

            var ground = Body.Defaults;
            ground.InverseMass = FP.Zero;
            ground.Flags = BodyFlags.Static;
            ground.Position = Vector3.Zero;
            ground.Shape = BodyShape.Box(new Vector3(FP.FromInt(50), FP.FromFloat(0.5f), FP.FromInt(50)));
            world.AddBody(ground);

            handles = new BodyHandle[count];
            int perSide = (int)System.Math.Ceiling(System.Math.Sqrt(count));
            FP step = FP.FromInt(2);
            for (int i = 0; i < count; i++)
            {
                int r = i / perSide;
                int c = i % perSide;
                var box = Body.Defaults;
                box.InverseMass = FP.One;
                box.Position = new Vector3(
                    FP.FromInt(c) * step - FP.FromInt(perSide),
                    FP.FromFloat(0.6f) + FP.FromInt(r / perSide) * FP.FromInt(2),
                    FP.FromInt(r % perSide) * step - FP.FromInt(perSide));
                box.Shape = BodyShape.Box(new Vector3(FP.Half, FP.Half, FP.Half));
                handles[i] = world.AddBody(box);
            }
            return world;
        }

        [Test]
        public static void HundredBoxStepCompletes()
        {
            var world = BuildGrid(100, out _);
            world.VelocityIterations = 8;
            world.PositionIterations = 3;

            FP dt = FP.FromFloat(0.01f);
            for (int i = 0; i < 5; i++) world.Step(dt);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++) world.Step(dt);
            sw.Stop();

            long avgUs = sw.ElapsedTicks * 1000L / TicksPerMillisecond / 100L;
            TestRunner.Assert(avgUs < 2000,
                $"100 boxes / step should be < 2ms, got avg {avgUs}us");
        }

        [Test]
        public static void StaticSceneStepIsFast()
        {
            var world = new PhysicsWorld(256);
            for (int i = 0; i < 100; i++)
            {
                var box = Body.Defaults;
                box.InverseMass = FP.Zero;
                box.Flags = BodyFlags.Static;
                box.Shape = BodyShape.Box(new Vector3(FP.Half, FP.Half, FP.Half));
                box.Position = new Vector3(
                    FP.FromInt(i % 10) * FP.FromInt(2),
                    FP.FromFloat(0.5f),
                    FP.FromInt(i / 10) * FP.FromInt(2));
                world.AddBody(box);
            }

            FP dt = FP.FromFloat(0.01f);
            for (int i = 0; i < 3; i++) world.Step(dt);
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++) world.Step(dt);
            sw.Stop();

            long avgMs = sw.ElapsedTicks / TicksPerMillisecond / 100L;
            TestRunner.Assert(avgMs < 50,
                $"100 static boxes / step should be fast, got {avgMs}ms");
        }

        [Test]
        public static void SleepingBoxesStepIsFast()
        {
            var world = new PhysicsWorld(512, new Vector3(FP.Zero, -FP.FromInt(10), FP.Zero));
            world.FramesToSleep = 10;

            var ground = Body.Defaults;
            ground.InverseMass = FP.Zero;
            ground.Flags = BodyFlags.Static;
            ground.Shape = BodyShape.Box(new Vector3(FP.FromInt(50), FP.FromFloat(0.5f), FP.FromInt(50)));
            world.AddBody(ground);

            var boxHandles = new BodyHandle[50];
            for (int i = 0; i < 50; i++)
            {
                var box = Body.Defaults;
                box.InverseMass = FP.One;
                int r = i / 10;
                int c = i % 10;
                box.Position = new Vector3(
                    FP.FromInt(c) * FP.FromInt(2) - FP.FromInt(9),
                    FP.FromFloat(0.6f),
                    FP.FromInt(r) * FP.FromInt(2) - FP.FromInt(9));
                box.Shape = BodyShape.Box(new Vector3(FP.Half, FP.Half, FP.Half));
                boxHandles[i] = world.AddBody(box);
            }

            FP dt = FP.FromFloat(0.01f);
            for (int i = 0; i < 100; i++) world.Step(dt);

            int awake = 0;
            for (int i = 0; i < 50; i++)
            {
                if (world.TryGetBody(boxHandles[i], out var body))
                {
                    if ((body.Flags & BodyFlags.Sleeping) == 0) awake++;
                }
            }

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++) world.Step(dt);
            sw.Stop();

            long avgMs = sw.ElapsedTicks / TicksPerMillisecond / 100L;
            TestRunner.Assert(avgMs < 50,
                $"sleeping scene / step should be fast (awake={awake}), got {avgMs}ms");
        }

        [Test]
        public static void SingleBodyStepIsFast()
        {
            var world = new PhysicsWorld(8, new Vector3(FP.Zero, -FP.FromInt(10), FP.Zero));
            var box = Body.Defaults;
            box.InverseMass = FP.One;
            box.Position = new Vector3(FP.Zero, FP.FromInt(5), FP.Zero);
            box.Shape = BodyShape.Box(new Vector3(FP.Half, FP.Half, FP.Half));
            world.AddBody(box);

            FP dt = FP.FromFloat(0.01f);
            for (int i = 0; i < 5; i++) world.Step(dt);
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++) world.Step(dt);
            sw.Stop();

            long avgUs = sw.ElapsedTicks * 1000L / TicksPerMillisecond / 1000L;
            TestRunner.Assert(avgUs < 5000,
                $"single body / step should be < 5ms, got avg {avgUs}us");
        }
    }
}