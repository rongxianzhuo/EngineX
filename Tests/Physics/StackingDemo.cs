using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;

namespace EngineX.Physics.Tests
{
    public static class StackingDemo
    {
        [Test]
        public static void BoxesFallAndSettle()
        {
            var world = new PhysicsWorld(64, new Vector3(FP.Zero, -FP.FromInt(10), FP.Zero));
            world.FramesToSleep = 60;
            world.VelocityIterations = 16;
            world.PositionIterations = 4;

            var ground = Body.Defaults;
            ground.InverseMass = FP.Zero;
            ground.Flags = BodyFlags.Static;
            ground.Position = Vector3.Zero;
            ground.Shape = BodyShape.Box(new Vector3(FP.FromInt(50), FP.FromFloat(0.5f), FP.FromInt(50)));
            world.AddBody(ground);

            int boxCount = 3;
            var boxHandles = new BodyHandle[boxCount];
            for (int i = 0; i < boxCount; i++)
            {
                var box = Body.Defaults;
                box.InverseMass = FP.One;
                box.Position = new Vector3(
                    FP.FromInt(i) * FP.FromInt(3),
                    FP.FromFloat(0.55f),
                    FP.Zero);
                box.Shape = BodyShape.Box(new Vector3(FP.Half, FP.Half, FP.Half));
                boxHandles[i] = world.AddBody(box);
            }

            FP dt = FP.FromFloat(0.002f);
            int steps = 1500;
            for (int i = 0; i < steps; i++)
            {
                world.Step(dt);
            }

            for (int i = 0; i < boxCount; i++)
            {
                if (!world.TryGetBody(boxHandles[i], out var body))
                {
                    TestRunner.Assert(false, $"body {i} missing");
                    return;
                }
                TestRunner.Assert(body.Position.Y >= FP.FromFloat(-0.4f),
                    $"box {i} should not fall below ground (y=-0.5), got y={body.Position.Y}");
            }
        }

        [Test]
        public static void HundredBoxesSettle()
        {
            var world = new PhysicsWorld(256, new Vector3(FP.Zero, -FP.FromInt(2), FP.Zero));
            world.FramesToSleep = 30;
            world.VelocityIterations = 10;
            world.PositionIterations = 1;

            var ground = Body.Defaults;
            ground.InverseMass = FP.Zero;
            ground.Flags = BodyFlags.Static;
            ground.Position = Vector3.Zero;
            ground.Shape = BodyShape.Box(new Vector3(FP.FromInt(50), FP.FromFloat(0.5f), FP.FromInt(50)));
            world.AddBody(ground);

            int boxCount = 100;
            var boxHandles = new BodyHandle[boxCount];
            int perSide = 10;
            for (int i = 0; i < boxCount; i++)
            {
                int r = i / perSide;
                int c = i % perSide;
                var box = Body.Defaults;
                box.InverseMass = FP.One;
                box.Position = new Vector3(
                    FP.FromInt(c) - FP.FromFloat(4.5f),
                    FP.FromInt(5) + FP.FromFloat(r) * FP.FromFloat(1.5f),
                    FP.Zero);
                box.Shape = BodyShape.Box(new Vector3(FP.Half, FP.Half, FP.Half));
                boxHandles[i] = world.AddBody(box);
            }

            FP dt = FP.FromFloat(0.005f);
            int steps = 1000;
            for (int i = 0; i < steps; i++)
            {
                world.Step(dt);
            }

            int fellThrough = 0;
            for (int i = 0; i < boxCount; i++)
            {
                if (!world.TryGetBody(boxHandles[i], out var body))
                {
                    fellThrough++;
                    continue;
                }
                if (body.Position.Y < FP.FromFloat(-0.4f))
                {
                    fellThrough++;
                }
            }
            TestRunner.Assert(fellThrough < 10,
                $"fewer than 10 boxes should fall through, got {fellThrough}");

            FP maxVel = FP.Zero;
            int sleeping = 0;
            for (int i = 0; i < boxCount; i++)
            {
                if (world.TryGetBody(boxHandles[i], out var body))
                {
                    FP v = body.LinearVelocity.Magnitude;
                    if (v > maxVel) maxVel = v;
                    if ((body.Flags & BodyFlags.Sleeping) != 0) sleeping++;
                }
            }
            TestRunner.Assert(maxVel < FP.FromFloat(20f),
                $"boxes should not explode, max velocity = {maxVel} (sleeping={sleeping}/{boxCount})");
        }

        [Test]
        public static void SingleBoxLandsOnGround()
        {
            var world = new PhysicsWorld(8, new Vector3(FP.Zero, -FP.FromInt(10), FP.Zero));
            world.FramesToSleep = 60;
            world.VelocityIterations = 8;
            world.PositionIterations = 3;

            var ground = Body.Defaults;
            ground.InverseMass = FP.Zero;
            ground.Flags = BodyFlags.Static;
            ground.Position = Vector3.Zero;
            ground.Shape = BodyShape.Box(new Vector3(FP.FromInt(10), FP.FromFloat(0.5f), FP.FromInt(10)));
            world.AddBody(ground);

            var box = Body.Defaults;
            box.InverseMass = FP.One;
            box.Position = new Vector3(FP.Zero, FP.FromInt(5), FP.Zero);
            box.Shape = BodyShape.Box(new Vector3(FP.Half, FP.Half, FP.Half));
            var hBox = world.AddBody(box);

            FP dt = FP.FromFloat(0.01f);
            for (int i = 0; i < 100; i++)
            {
                world.Step(dt);
            }

            world.TryGetBody(hBox, out var after);
            TestRunner.Assert(after.Position.Y < FP.FromInt(5),
                $"box should have fallen from y=5, got y={after.Position.Y}");
            TestRunner.Assert(after.Position.Y > FP.FromFloat(0.5f),
                $"box should be resting above ground, got y={after.Position.Y}");
        }

        [Test]
        public static void TwoBoxesStackVertically()
        {
            var world = new PhysicsWorld(8, new Vector3(FP.Zero, -FP.FromInt(10), FP.Zero));
            world.VelocityIterations = 12;
            world.PositionIterations = 4;

            var ground = Body.Defaults;
            ground.InverseMass = FP.Zero;
            ground.Flags = BodyFlags.Static;
            ground.Position = Vector3.Zero;
            ground.Shape = BodyShape.Box(new Vector3(FP.FromInt(10), FP.FromFloat(0.5f), FP.FromInt(10)));
            world.AddBody(ground);

            var bottom = Body.Defaults;
            bottom.InverseMass = FP.One;
            bottom.Position = new Vector3(FP.Zero, FP.FromInt(2), FP.Zero);
            bottom.Shape = BodyShape.Box(new Vector3(FP.Half, FP.Half, FP.Half));
            var hBottom = world.AddBody(bottom);

            var top = Body.Defaults;
            top.InverseMass = FP.One;
            top.Position = new Vector3(FP.Zero, FP.FromInt(4), FP.Zero);
            top.Shape = BodyShape.Box(new Vector3(FP.Half, FP.Half, FP.Half));
            var hTop = world.AddBody(top);

            FP dt = FP.FromFloat(0.01f);
            for (int i = 0; i < 200; i++)
            {
                world.Step(dt);
            }

            world.TryGetBody(hBottom, out var b);
            world.TryGetBody(hTop, out var t);
            TestRunner.Assert(t.Position.Y > b.Position.Y,
                $"top should be above bottom, got top={t.Position.Y}, bottom={b.Position.Y}");
            TestRunner.Assert(t.Position.Y < FP.FromInt(5),
                $"top should fall, got y={t.Position.Y}");
            TestRunner.Assert(b.Position.Y > FP.FromFloat(0.5f),
                $"bottom should not fall through ground, got y={b.Position.Y}");
        }
    }
}