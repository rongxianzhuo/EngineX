using System;
using EngineX.ECS.Components;
using EngineX.Jobs;

namespace EngineX.ECS.Tests
{
    public static class EntityCommandBufferTests
    {
        [Test]
        public static void CreateEntityPlaybackCreatesEntity()
        {
            using var world = new World();
            var ecb = new EntityCommandBuffer();
            ecb.CreateEntity();
            TestRunner.AssertEqual(1, ecb.CommandCount);
            TestRunner.AssertEqual(0, world.EntityCount);
            ecb.Playback(world);
            TestRunner.AssertEqual(1, world.EntityCount);
        }

        [Test]
        public static void CreateEntityMultipleCreates()
        {
            using var world = new World();
            var ecb = new EntityCommandBuffer();
            for (int i = 0; i < 5; i++)
            {
                ecb.CreateEntity();
            }
            ecb.Playback(world);
            TestRunner.AssertEqual(5, world.EntityCount);
        }

        [Test]
        public static void DestroyEntityRemovesExisting()
        {
            using var world = new World();
            var e = world.CreateEntity();
            world.AddComponent(e, new Position { X = 1 });
            var ecb = new EntityCommandBuffer();
            ecb.DestroyEntity(e);
            ecb.Playback(world);
            TestRunner.Assert(!world.Exists(e));
            TestRunner.AssertEqual(0, world.EntityCount);
        }

        [Test]
        public static void AddComponentValueRoundTrip()
        {
            using var world = new World();
            var e = world.CreateEntity();
            world.AddComponent(e, new Position { X = 1, Y = 2 });
            var ecb = new EntityCommandBuffer();
            ecb.AddComponent(e, new Velocity { DX = 3, DY = 4 });
            ecb.Playback(world);
            var v = world.GetComponent<Velocity>(e);
            TestRunner.AssertEqual(3f, v.DX);
            TestRunner.AssertEqual(4f, v.DY);
            var p = world.GetComponent<Position>(e);
            TestRunner.AssertEqual(1f, p.X);
            TestRunner.AssertEqual(2f, p.Y);
        }

        [Test]
        public static void AddComponentMultipleTypesOnSameEntity()
        {
            using var world = new World();
            var e = world.CreateEntity();
            var ecb = new EntityCommandBuffer();
            ecb.AddComponent(e, new Position { X = 1, Y = 2 });
            ecb.AddComponent(e, new Velocity { DX = 3, DY = 4 });
            ecb.AddComponent(e, new Health { Value = 100 });
            ecb.Playback(world);
            TestRunner.AssertEqual(1f, world.GetComponent<Position>(e).X);
            TestRunner.AssertEqual(3f, world.GetComponent<Velocity>(e).DX);
            TestRunner.AssertEqual(100, world.GetComponent<Health>(e).Value);
        }

        [Test]
        public static void AddComponentRenderDataManagedComponent()
        {
            using var world = new World();
            var e = world.CreateEntity();
            var ecb = new EntityCommandBuffer();
            ecb.AddComponent(e, new RenderData("meshes/cube.obj", "materials/red.mat"));
            ecb.Playback(world);
            var r = world.GetComponent<RenderData>(e);
            TestRunner.AssertEqual("meshes/cube.obj", r.MeshPath);
            TestRunner.AssertEqual("materials/red.mat", r.MaterialPath);
        }

        [Test]
        public static void RemoveComponentKeepsOthers()
        {
            using var world = new World();
            var e = world.CreateEntity();
            world.AddComponent(e, new Position { X = 1 });
            world.AddComponent(e, new Velocity { DX = 2 });
            var ecb = new EntityCommandBuffer();
            ecb.RemoveComponent<Velocity>(e);
            ecb.Playback(world);
            TestRunner.Assert(world.HasComponent<Position>(e));
            TestRunner.Assert(!world.HasComponent<Velocity>(e));
        }

        [Test]
        public static void CreateThenAddComponentsSpawnPattern()
        {
            using var world = new World();
            var ecb = new EntityCommandBuffer();
            for (int i = 0; i < 3; i++)
            {
                var e = ecb.CreateEntity();
                ecb.AddComponent(e, new Position { X = i, Y = i * 10 });
                ecb.AddComponent(e, new Health { Value = i + 1 });
            }
            TestRunner.AssertEqual(0, world.EntityCount);
            ecb.Playback(world);
            TestRunner.AssertEqual(3, world.EntityCount);

            var query = world.Query<Position, Health>();
            TestRunner.AssertEqual(3, query.CalculateEntityCount());
            var verify = new VerifySpawnedVisitor();
            query.ForEach<VerifySpawnedVisitor, Position, Health>(ref verify);
            TestRunner.AssertEqual(3f, verify.SumX);
            TestRunner.AssertEqual(30f, verify.SumY);
            TestRunner.AssertEqual(6, verify.SumHealth);
        }

        private struct VerifySpawnedVisitor : IForEach<Position, Health>
        {
            public float SumX;
            public float SumY;
            public int SumHealth;

            public void Execute(ref Position p, ref Health h)
            {
                SumX += p.X;
                SumY += p.Y;
                SumHealth += h.Value;
            }
        }

        [Test]
        public static void CreateThenDestroyNetZero()
        {
            using var world = new World();
            var before = world.EntityCount;
            var ecb = new EntityCommandBuffer();
            var e = ecb.CreateEntity();
            ecb.AddComponent(e, new Position { X = 1 });
            ecb.DestroyEntity(e);
            ecb.Playback(world);
            TestRunner.AssertEqual(before, world.EntityCount);
        }

        [Test]
        public static void PlaybackOrderAddRemoveAdd()
        {
            using var world = new World();
            var e = world.CreateEntity();
            var ecb = new EntityCommandBuffer();
            ecb.AddComponent(e, new Health { Value = 1 });
            ecb.RemoveComponent<Health>(e);
            ecb.AddComponent(e, new Health { Value = 2 });
            ecb.Playback(world);
            TestRunner.Assert(world.HasComponent<Health>(e));
            TestRunner.AssertEqual(2, world.GetComponent<Health>(e).Value);
        }

        [Test]
        public static void MixedExistingAndDeferredEntities()
        {
            using var world = new World();
            var existing = world.CreateEntity();
            world.AddComponent(existing, new Position { X = 7 });
            var ecb = new EntityCommandBuffer();
            ecb.AddComponent(existing, new Velocity { DX = 8 });
            var created = ecb.CreateEntity();
            ecb.AddComponent(created, new Position { X = 9 });
            ecb.Playback(world);
            TestRunner.AssertEqual(2, world.EntityCount);
            TestRunner.AssertEqual(8f, world.GetComponent<Velocity>(existing).DX);
            TestRunner.AssertEqual(7f, world.GetComponent<Position>(existing).X);

            var query = world.Query<Position>();
            TestRunner.AssertEqual(2, query.CalculateEntityCount());
            var verify = new SumPositionVisitor();
            query.ForEach<SumPositionVisitor, Position>(ref verify);
            TestRunner.AssertEqual(16f, verify.SumX);
        }

        private struct SumPositionVisitor : IForEach<Position>
        {
            public float SumX;

            public void Execute(ref Position p)
            {
                SumX += p.X;
            }
        }

        [Test]
        public static void EmptyPlaybackIsNoOp()
        {
            using var world = new World();
            var e = world.CreateEntity();
            world.AddComponent(e, new Position { X = 1 });
            var ecb = new EntityCommandBuffer();
            ecb.Playback(world);
            TestRunner.AssertEqual(1, world.EntityCount);
            TestRunner.AssertEqual(1f, world.GetComponent<Position>(e).X);
            TestRunner.AssertEqual(0, ecb.CommandCount);
        }

        [Test]
        public static void ClearReusesBufferAndResetsValues()
        {
            using var world = new World();
            var e = world.CreateEntity();
            var ecb = new EntityCommandBuffer();
            ecb.AddComponent(e, new Health { Value = 1 });
            TestRunner.AssertEqual(1, ecb.CommandCount);
            ecb.Clear();
            TestRunner.AssertEqual(0, ecb.CommandCount);
            ecb.Playback(world);
            TestRunner.Assert(!world.HasComponent<Health>(e));
            ecb.AddComponent(e, new Health { Value = 2 });
            ecb.Playback(world);
            TestRunner.AssertEqual(2, world.GetComponent<Health>(e).Value);
        }

        [Test]
        public static void IterationCollectThenDestroyPattern()
        {
            using var world = new World();
            for (int i = 0; i < 200; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Health { Value = i < 50 ? 0 : 100 });
            }
            var query = world.Query<Health>();
            var ecb = new EntityCommandBuffer();
            var visitor = new CollectDeadVisitor { Ecb = ecb };
            query.ForEachChunk<CollectDeadVisitor>(ref visitor);
            TestRunner.AssertEqual(50, visitor.Count);
            TestRunner.AssertEqual(50, ecb.CommandCount);
            ecb.Playback(world);
            TestRunner.AssertEqual(150, world.EntityCount);
            TestRunner.AssertEqual(150, query.CalculateEntityCount());
        }

        private struct CollectDeadVisitor : IForEachChunk
        {
            public EntityCommandBuffer Ecb;

            public int Count;

            public void Execute(Chunk chunk)
            {
                for (int e = 0; e < chunk.Count; e++)
                {
                    if (chunk.GetComponentRef<Health>(e).Value == 0)
                    {
                        Ecb.DestroyEntity(chunk.GetEntityRef(e));
                        Count++;
                    }
                }
            }
        }

        [Test]
        public static void JobCompletesThenEcbDestroysPattern()
        {
            using var world = new World();
            for (int i = 0; i < 200; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Health { Value = 100 });
            }
            var query = world.Query<Health>();
            var chunks = new NativeArray<ChunkHandle>(query.CalculateChunkCount(), Allocator.TempJob);
            query.ToChunkArray(chunks);
            var handle = JobSystem.ScheduleParallel(new KillJob { Chunks = chunks }, chunks, 1);
            handle.Complete();
            chunks.Dispose();

            var ecb = new EntityCommandBuffer();
            var collect = new CollectDeadVisitor { Ecb = ecb };
            query.ForEachChunk<CollectDeadVisitor>(ref collect);
            TestRunner.AssertEqual(200, collect.Count);
            TestRunner.AssertEqual(200, ecb.CommandCount);
            ecb.Playback(world);
            TestRunner.AssertEqual(0, world.EntityCount);
            TestRunner.AssertEqual(0, query.CalculateEntityCount());
        }

        private struct KillJob : IJobParallelForBatch
        {
            public NativeArray<ChunkHandle> Chunks;

            public void Execute(int startIndex, int count)
            {
                for (int i = startIndex; i < startIndex + count; i++)
                {
                    var chunk = Chunks[i].Chunk;
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        ref var h = ref chunk.GetComponentRef<Health>(e);
                        h.Value = 0;
                    }
                }
            }
        }

        [Test]
        public static void ZeroGcBuffering()
        {
            using var world = new World();
            var e = world.CreateEntity();
            world.AddComponent(e, new Position { X = 1 });
            var ecb = new EntityCommandBuffer();

            for (int warmup = 0; warmup < 2; warmup++)
            {
                for (int i = 0; i < 100; i++)
                {
                    ecb.AddComponent(e, new Velocity { DX = i });
                    ecb.RemoveComponent<Health>(e);
                }
                ecb.Clear();
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int round = 0; round < 3; round++)
            {
                for (int i = 0; i < 100; i++)
                {
                    ecb.AddComponent(e, new Velocity { DX = i });
                    ecb.RemoveComponent<Health>(e);
                }
                ecb.Clear();
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < 1024, $"ECB buffering allocated {allocated} bytes over 3 rounds");
        }

        [Test]
        public static void CreateInOnePlaybackConsumesStoreValues()
        {
            using var world = new World();
            var ecb = new EntityCommandBuffer();
            var e1 = ecb.CreateEntity();
            ecb.AddComponent(e1, new Health { Value = 10 });
            var e2 = ecb.CreateEntity();
            ecb.AddComponent(e2, new Health { Value = 20 });
            ecb.Playback(world);

            var query = world.Query<Health>();
            TestRunner.AssertEqual(2, query.CalculateEntityCount());
            var verify = new SumHealthVisitor();
            query.ForEach<SumHealthVisitor, Health>(ref verify);
            TestRunner.AssertEqual(30, verify.SumHealth);
        }

        private struct SumHealthVisitor : IForEach<Health>
        {
            public int SumHealth;

            public void Execute(ref Health h)
            {
                SumHealth += h.Value;
            }
        }
    }
}
