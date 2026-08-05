using EngineX.Jobs;

namespace EngineX.ECS.Tests
{
    public static class EcsQueryTests
    {
        [Test]
        public static void ForEachLambdaUpdatesComponents()
        {
            using var world = new World();
            for (int i = 0; i < 1000; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                world.AddComponent(e, new Velocity { DX = 1 });
            }
            var query = world.Query<Position, Velocity>();
            var move = new AddVelocityVisitor();
            query.ForEach<AddVelocityVisitor, Position, Velocity>(ref move);

            var sum = new SumXVisitor();
            query.ForEach<SumXVisitor, Position>(ref sum);
            TestRunner.AssertEqual(1000, query.CalculateEntityCount());
            TestRunner.AssertEqual(500500, sum.Sum);
        }

        private struct AddVelocityVisitor : IForEach<Position, Velocity>
        {
            public void Execute(ref Position p, ref Velocity v)
            {
                p.X += v.DX;
            }
        }

        private struct SumXVisitor : IForEach<Position>
        {
            public int Sum;

            public void Execute(ref Position p)
            {
                Sum += (int)p.X;
            }
        }

        [Test]
        public static void ForEachVisitorAccumulatesState()
        {
            using var world = new World();
            for (int i = 0; i < 500; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
            }
            var visitor = new SumVisitor();
            world.Query<Position>().ForEach<SumVisitor, Position>(ref visitor);
            TestRunner.AssertEqual(500, visitor.Count);
            TestRunner.AssertEqual(124750, visitor.Sum);
        }

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
        public static void WithNoneFiltersArchetypes()
        {
            using var world = new World();
            for (int i = 0; i < 100; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                if (i % 2 == 0)
                {
                    world.AddComponent(e, new Dead());
                }
            }
            var alive = world.Query().WithAll<Position>().WithNone<Dead>().Build();
            var dead = world.Query().WithAll<Position, Dead>().Build();
            TestRunner.AssertEqual(50, alive.CalculateEntityCount());
            TestRunner.AssertEqual(50, dead.CalculateEntityCount());
        }

        [Test]
        public static void WithAnyMatchesArchetypes()
        {
            using var world = new World();
            for (int i = 0; i < 60; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                if (i % 2 == 0)
                {
                    world.AddComponent(e, new Velocity());
                }
                if (i % 3 == 0)
                {
                    world.AddComponent(e, new Health { Value = i });
                }
            }
            var query = world.Query().WithAny<Velocity, Health>().Build();
            TestRunner.AssertEqual(40, query.CalculateEntityCount());
        }

        [Test]
        public static void ToEntityArrayMatchesForEachOrder()
        {
            using var world = new World();
            for (int i = 0; i < 300; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
            }
            var query = world.Query<Position>();

            // 收集 visitor ForEach 的组件值顺序
            var forEach = new CollectXVisitor { Xs = new int[300] };
            query.ForEach<CollectXVisitor, Position>(ref forEach);
            TestRunner.AssertEqual(300, forEach.Count);

            // 收集 ForEachChunk 的实体句柄顺序
            var chunks = new CollectEntityChunkVisitor { Entities = new Entity[300] };
            query.ForEachChunk<CollectEntityChunkVisitor>(ref chunks);
            TestRunner.AssertEqual(300, chunks.Count);

            // 验证 ForEachChunk 顺序(GetEntityRef 句柄)与 visitor ForEach 顺序一致:
            // 第 k 个实体句柄读出的组件值 == 第 k 个 ForEach 值
            for (int k = 0; k < 300; k++)
            {
                var pe = world.GetComponent<Position>(chunks.Entities[k]);
                TestRunner.AssertEqual(forEach.Xs[k], (int)pe.X);
            }
        }

        private struct CollectXVisitor : IForEach<Position>
        {
            public int Count;
            public int[] Xs;

            public void Execute(ref Position p)
            {
                Xs[Count++] = (int)p.X;
            }
        }

        private struct CollectEntityChunkVisitor : IForEachChunk
        {
            public int Count;
            public Entity[] Entities;

            public void Execute(Chunk chunk)
            {
                for (int e = 0; e < chunk.Count; e++)
                {
                    Entities[Count++] = chunk.GetEntityRef(e);
                }
            }
        }

        [Test]
        public static void ComponentDataArrayRoundTrip()
        {
            using var world = new World();
            for (int i = 0; i < 256; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
            }
            var query = world.Query<Position>();
            var data = new NativeArray<Position>(query.CalculateEntityCount(), Allocator.TempJob);
            var fill = new FillDataVisitor { Data = data };
            query.ForEach<FillDataVisitor, Position>(ref fill);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new Position { X = data[i].X + 100 };
            }
            query.CopyFromComponentDataArray(data);

            var sum = new SumXVisitor();
            query.ForEach<SumXVisitor, Position>(ref sum);
            TestRunner.AssertEqual(256 * 100 + 32640, sum.Sum);
            data.Dispose();
        }

        private struct FillDataVisitor : IForEach<Position>
        {
            public NativeArray<Position> Data;
            public int Count;

            public void Execute(ref Position p)
            {
                Data[Count++] = p;
            }
        }

        [Test]
        public static void ToChunkArrayCoversAllEntities()
        {
            using var world = new World();
            for (int i = 0; i < 1000; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
            }
            var query = world.Query<Position>();
            var chunks = new NativeArray<ChunkHandle>(query.CalculateChunkCount(), Allocator.TempJob);
            query.ToChunkArray(chunks);
            int total = 0;
            for (int i = 0; i < chunks.Length; i++)
            {
                total += chunks[i].Count;
            }
            TestRunner.AssertEqual(1000, total);
            TestRunner.Assert(chunks.Length >= 8);
            chunks.Dispose();
        }

        [Test]
        public static void DeterministicIterationOrder()
        {
            using var world = new World();
            for (int i = 0; i < 400; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                if (i % 4 == 0)
                {
                    world.AddComponent(e, new Velocity());
                }
            }
            var query = world.Query<Position>();
            int[] first = CollectX(query);
            int[] second = CollectX(query);
            TestRunner.AssertEqual(first.Length, second.Length);
            for (int i = 0; i < first.Length; i++)
            {
                TestRunner.AssertEqual(first[i], second[i]);
            }
        }

        private static int[] CollectX(EntityQuery query)
        {
            var result = new int[query.CalculateEntityCount()];
            var visitor = new CollectXIntoVisitor { Result = result };
            query.ForEach<CollectXIntoVisitor, Position>(ref visitor);
            return result;
        }

        private struct CollectXIntoVisitor : IForEach<Position>
        {
            public int[] Result;
            public int Count;

            public void Execute(ref Position p)
            {
                Result[Count++] = (int)p.X;
            }
        }

        [Test]
        public static void ForEachOnEmptyQueryIsSafe()
        {
            using var world = new World();
            var query = world.Query<Position, Velocity>();
            var visitor = new CountVisitor();
            query.ForEach<CountVisitor, Position, Velocity>(ref visitor);
            TestRunner.AssertEqual(0, visitor.Count);
            TestRunner.AssertEqual(0, query.CalculateEntityCount());
        }

        private struct CountVisitor : IForEach<Position, Velocity>
        {
            public int Count;

            public void Execute(ref Position p, ref Velocity v)
            {
                Count++;
            }
        }

        [Test]
        public static void QueriesSeeNewArchetypes()
        {
            using var world = new World();
            var query = world.Query<Position>();
            TestRunner.AssertEqual(0, query.CalculateEntityCount());
            var e = world.CreateEntity();
            world.AddComponent(e, new Position { X = 7 });
            TestRunner.AssertEqual(1, query.CalculateEntityCount());
        }
    }
}
