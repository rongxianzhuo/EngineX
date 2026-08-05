using System;

namespace EngineX.ECS.Tests
{
    public static class ForEachZeroGcTests
    {
        private const int EntityCount = 10000;

        private static World BuildWorld()
        {
            var world = new World();
            for (int i = 0; i < EntityCount; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
            }
            return world;
        }

        [Test]
        public static void ForEachStructVisitorIsZeroGc()
        {
            using var world = BuildWorld();
            var query = world.Query<Position>();
            var visitor = new SumVisitor();
            query.ForEach<SumVisitor, Position>(ref visitor);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10; i++)
            {
                query.ForEach<SumVisitor, Position>(ref visitor);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < 1024, $"ForEach visitor allocated {allocated} bytes over 10 iterations");
        }

        private struct SumVisitor : IForEach<Position>
        {
            public int Sum;

            public void Execute(ref Position p)
            {
                Sum += (int)p.X;
            }
        }

        [Test]
        public static void QueryConstructionAllocatesButIsOneTime()
        {
            using var world = BuildWorld();
            var query = world.Query<Position>();

            long before = GC.GetAllocatedBytesForCurrentThread();
            var q2 = world.Query<Position>();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated > 0, "Query<T>() construction should allocate");

            long before2 = GC.GetAllocatedBytesForCurrentThread();
            var q3 = query;
            long allocated2 = GC.GetAllocatedBytesForCurrentThread() - before2;
            TestRunner.AssertEqual(0L, allocated2);
        }
    }
}
