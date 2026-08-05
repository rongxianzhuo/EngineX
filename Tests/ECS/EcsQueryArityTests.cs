using EngineX.Jobs;

namespace EngineX.ECS.Tests
{
    public static class EcsQueryArityTests
    {
        [Test]
        public static void QueryThreeComponents()
        {
            using var world = new World();
            for (int i = 0; i < 100; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                world.AddComponent(e, new Velocity { DX = 1 });
                world.AddComponent(e, new Health { Value = i });
            }
            var query = world.Query<Position, Velocity, Health>();
            TestRunner.AssertEqual(100, query.CalculateEntityCount());

            var sumVisitor = new TripleSumVisitor();
            query.ForEach<TripleSumVisitor, Position, Velocity, Health>(ref sumVisitor);
            int expected = 0;
            for (int i = 0; i < 100; i++)
            {
                expected += i + 1 + i;
            }
            TestRunner.AssertEqual(expected, sumVisitor.Sum);
        }

        private struct TripleSumVisitor : IForEach<Position, Velocity, Health>
        {
            public int Sum;

            public void Execute(ref Position p, ref Velocity v, ref Health h)
            {
                Sum += (int)p.X + (int)v.DX + h.Value;
            }
        }

        [Test]
        public static void QueryFourComponents()
        {
            using var world = new World();
            for (int i = 0; i < 50; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                world.AddComponent(e, new Velocity());
                world.AddComponent(e, new Health { Value = 1 });
                world.AddComponent(e, new Tag());
            }
            var query = world.Query<Position, Velocity, Health, Tag>();
            TestRunner.AssertEqual(50, query.CalculateEntityCount());

            var countVisitor = new FourCountVisitor();
            query.ForEach<FourCountVisitor, Position, Velocity, Health, Tag>(ref countVisitor);
            TestRunner.AssertEqual(50, countVisitor.Count);
        }

        private struct FourCountVisitor : IForEach<Position, Velocity, Health, Tag>
        {
            public int Count;

            public void Execute(ref Position p, ref Velocity v, ref Health h, ref Tag t)
            {
                Count++;
            }
        }

        [Test]
        public static void QueryFiveComponents()
        {
            using var world = new World();
            for (int i = 0; i < 300; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                world.AddComponent(e, new Velocity());
                world.AddComponent(e, new Health { Value = i });
                world.AddComponent(e, new Tag());
                if (i % 2 == 0)
                {
                    world.AddComponent(e, new Dead());
                }
            }
            var query = world.Query<Position, Velocity, Health, Tag, Dead>();
            TestRunner.AssertEqual(150, query.CalculateEntityCount());

            var visitor = new FiveComponentVisitor();
            query.ForEachChunk<FiveComponentVisitor>(ref visitor);
            TestRunner.AssertEqual(150, visitor.Entities);
            TestRunner.AssertEqual(150, visitor.DeadCount);
            TestRunner.AssertEqual(query.CalculateChunkCount(), visitor.Chunks);
        }

        private struct FiveComponentVisitor : IForEachChunk
        {
            public int Chunks;
            public int Entities;
            public int DeadCount;

            public void Execute(Chunk chunk)
            {
                Chunks++;
                for (int e = 0; e < chunk.Count; e++)
                {
                    Entities++;
                    ref var p = ref chunk.GetComponentRef<Position>(e);
                    ref var v = ref chunk.GetComponentRef<Velocity>(e);
                    ref var h = ref chunk.GetComponentRef<Health>(e);
                    ref var t = ref chunk.GetComponentRef<Tag>(e);
                    ref var d = ref chunk.GetComponentRef<Dead>(e);
                    if (h.Value % 2 == 0)
                    {
                        DeadCount++;
                    }
                }
            }
        }

        [Test]
        public static void QueryBuilderWithAllUpToFive()
        {
            using var world = new World();
            for (int i = 0; i < 80; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position());
                world.AddComponent(e, new Velocity());
                world.AddComponent(e, new Health());
                world.AddComponent(e, new Tag());
                world.AddComponent(e, new Dead());
            }
            var q3 = world.Query().WithAll<Position, Velocity, Health>().Build();
            var q4 = world.Query().WithAll<Position, Velocity, Health, Tag>().Build();
            var q5 = world.Query().WithAll<Position, Velocity, Health, Tag, Dead>().Build();
            TestRunner.AssertEqual(80, q3.CalculateEntityCount());
            TestRunner.AssertEqual(80, q4.CalculateEntityCount());
            TestRunner.AssertEqual(80, q5.CalculateEntityCount());
        }

        [Test]
        public static void FiveComponentQueryExcludesPartialEntities()
        {
            using var world = new World();
            for (int i = 0; i < 100; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position());
                world.AddComponent(e, new Velocity());
                world.AddComponent(e, new Health());
                world.AddComponent(e, new Tag());
                if (i < 50)
                {
                    world.AddComponent(e, new Dead());
                }
            }
            var q5 = world.Query<Position, Velocity, Health, Tag, Dead>();
            var q4 = world.Query<Position, Velocity, Health, Tag>();
            TestRunner.AssertEqual(50, q5.CalculateEntityCount());
            TestRunner.AssertEqual(100, q4.CalculateEntityCount());
        }
    }
}
