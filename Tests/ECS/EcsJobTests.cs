using EngineX.Jobs;

namespace EngineX.ECS.Tests
{
    public static class EcsJobTests
    {
        private struct MoveChunkJob : IJobParallelForBatch
        {
            public NativeArray<ChunkHandle> Chunks;
            public float Delta;

            public void Execute(int startIndex, int count)
            {
                for (int i = startIndex; i < startIndex + count; i++)
                {
                    var chunk = Chunks[i].Chunk;
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        ref var pos = ref chunk.GetComponentRef<Position>(e);
                        ref var vel = ref chunk.GetComponentRef<Velocity>(e);
                        pos.X += vel.DX * Delta;
                        pos.Y += vel.DY * Delta;
                    }
                }
            }
        }

        private struct CollectJob : IJobParallelForBatch
        {
            public NativeArray<ChunkHandle> Chunks;
            public NativeArray<float> Results;

            public void Execute(int startIndex, int count)
            {
                int k = startIndex * Chunk.Capacity;
                for (int i = startIndex; i < startIndex + count; i++)
                {
                    var chunk = Chunks[i].Chunk;
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        Results[k++] = chunk.GetComponentRef<Position>(e).X;
                    }
                }
            }
        }

        [Test]
        public static void ParallelChunkJobUpdatesComponents()
        {
            using var world = new World();
            int entityCount = 3000;
            for (int i = 0; i < entityCount; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i, Y = i * 2 });
                world.AddComponent(e, new Velocity { DX = 1, DY = 1 });
            }

            var query = world.Query<Position, Velocity>();
            var chunks = new NativeArray<ChunkHandle>(query.CalculateChunkCount(), Allocator.TempJob);
            query.ToChunkArray(chunks);
            var handle = JobSystem.ScheduleParallel(new MoveChunkJob { Chunks = chunks, Delta = 0.5f }, chunks, 1);
            handle.Complete();
            chunks.Dispose();

            var sumVisitor = new SumXVisitor();
            query.ForEach<SumXVisitor, Position>(ref sumVisitor);
            int expected = 0;
            for (int i = 0; i < entityCount; i++)
            {
                expected += (int)(i + 0.5f);
            }
            TestRunner.AssertEqual(expected, sumVisitor.Sum);
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
        public static void ParallelChunkJobWithDependencyChain()
        {
            using var world = new World();
            for (int i = 0; i < 500; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = 1 });
                world.AddComponent(e, new Velocity { DX = 1 });
            }

            var query = world.Query<Position, Velocity>();
            var chunks = new NativeArray<ChunkHandle>(query.CalculateChunkCount(), Allocator.TempJob);
            query.ToChunkArray(chunks);
            var handle1 = JobSystem.ScheduleParallel(new MoveChunkJob { Chunks = chunks, Delta = 1f }, chunks, 1);
            var handle2 = JobSystem.ScheduleParallel(new MoveChunkJob { Chunks = chunks, Delta = 1f }, chunks, 1, handle1);
            handle2.Complete();
            chunks.Dispose();

            var sumVisitor = new SumXVisitor();
            query.ForEach<SumXVisitor, Position>(ref sumVisitor);
            TestRunner.AssertEqual(1500, sumVisitor.Sum);
        }

        [Test]
        public static void JobResultsMatchMainThread()
        {
            using var world = new World();
            for (int i = 0; i < 1000; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                world.AddComponent(e, new Velocity { DX = 1 });
            }

            var query = world.Query<Position, Velocity>();
            var chunks = new NativeArray<ChunkHandle>(query.CalculateChunkCount(), Allocator.TempJob);
            query.ToChunkArray(chunks);
            var results = new NativeArray<float>(1000, Allocator.TempJob);
            var handle = JobSystem.ScheduleParallel(new CollectJob { Chunks = chunks, Results = results }, chunks, 1);
            handle.Complete();

            var verify = new VerifyResultsVisitor { Results = results };
            query.ForEach<VerifyResultsVisitor, Position>(ref verify);
            TestRunner.AssertEqual(0, verify.Mismatches);
            TestRunner.AssertEqual(1000, verify.Count);

            chunks.Dispose();
            results.Dispose();
        }

        private struct VerifyResultsVisitor : IForEach<Position>
        {
            public NativeArray<float> Results;
            public int Count;
            public int Mismatches;

            public void Execute(ref Position p)
            {
                if (p.X != Results[Count])
                {
                    Mismatches++;
                }
                Count++;
            }
        }

        [Test]
        public static void SystemStateDependencyAcrossFrames()
        {
            using var world = new World();
            for (int i = 0; i < 2000; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = 0 });
                world.AddComponent(e, new Velocity { DX = 1 });
            }

            var system = new JobSystemWithDependency();
            var group = new SystemsGroup();
            group.Add(system);
            group.Create(world);
            for (int frame = 0; frame < 5; frame++)
            {
                group.Update(world);
                system.Handle.Complete();
            }
            group.Destroy();

            var sumVisitor = new SumXVisitor();
            world.Query<Position>().ForEach<SumXVisitor, Position>(ref sumVisitor);
            TestRunner.AssertEqual(2000 * 5, sumVisitor.Sum);
        }

        private sealed class JobSystemWithDependency : ISystem
        {
            public EntityQuery Query;
            public JobHandle Handle;
            private NativeArray<ChunkHandle> _chunks;

            public void OnCreate(ref SystemState state)
            {
                Query = state.World.Query<Position, Velocity>();
            }

            public void OnUpdate(ref SystemState state)
            {
                if (_chunks.IsCreated)
                {
                    _chunks.Dispose();
                }
                _chunks = new NativeArray<ChunkHandle>(Query.CalculateChunkCount(), Allocator.TempJob);
                Query.ToChunkArray(_chunks);
                Handle = JobSystem.ScheduleParallel(new MoveChunkJob { Chunks = _chunks, Delta = 1f }, _chunks, 1, state.Dependency);
                state.Dependency = Handle;
            }

            public void OnDestroy(ref SystemState state)
            {
                Handle.Complete();
                if (_chunks.IsCreated)
                {
                    _chunks.Dispose();
                }
            }
        }
    }
}
