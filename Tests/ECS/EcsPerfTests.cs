using System;
using System.Diagnostics;

namespace EngineX.ECS.Tests
{
    public static class EcsPerfTests
    {
        private struct SumVisitor : IForEach<Position>
        {
            public int Count;
            public int Sum;

            public void Execute(ref Position position)
            {
                Count++;
                Sum += (int)position.X;
            }
        }

        [Test]
        public static void ForEachVisitorIsZeroGc()
        {
            using var world = new World();
            for (int i = 0; i < 10000; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                world.AddComponent(e, new Velocity { DX = 1 });
            }
            var query = world.Query<Position, Velocity>();
            var visitor = new SumVisitor();
            query.ForEach<SumVisitor, Position>(ref visitor);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10; i++)
            {
                query.ForEach<SumVisitor, Position>(ref visitor);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < 1024, $"visitor ForEach allocated {allocated} bytes over 10 iterations");
        }

        [Test]
        public static void PerfSmokeReport()
        {
            using var world = new World();
            int entityCount = 100000;
            for (int i = 0; i < entityCount; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                world.AddComponent(e, new Velocity { DX = 1 });
            }
            var query = world.Query<Position, Velocity>();

            var visitor = new MoveVisitor { Delta = 1 };
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                query.ForEach<MoveVisitor, Position, Velocity>(ref visitor);
            }
            stopwatch.Stop();
            long visitorMs = stopwatch.ElapsedMilliseconds;

            Console.WriteLine($"[perf] visitor ForEach: {visitorMs} ms for 100 x {entityCount} entities ({entityCount * 100 / Math.Max(1, visitorMs) / 1000.0:F1} M entities/s)");

            var sum = new SumVisitor();
            query.ForEach<SumVisitor, Position>(ref sum);
            TestRunner.Assert(sum.Sum > 0);
        }

        private struct MoveVisitor : IForEach<Position, Velocity>
        {
            public float Delta;

            public void Execute(ref Position position, ref Velocity velocity)
            {
                position.X += velocity.DX * Delta;
                position.Y += velocity.DY * Delta;
            }
        }
    }
}
