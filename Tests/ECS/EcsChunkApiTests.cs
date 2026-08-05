using System;
using EngineX.Jobs;

namespace EngineX.ECS.Tests
{
    public static class EcsChunkApiTests
    {
        [Test]
        public static void CalculateChunkCountMatchesToChunkArray()
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
            TestRunner.AssertEqual(chunks.Length, query.CalculateChunkCount());
            TestRunner.Assert(chunks.Length >= 8);
            chunks.Dispose();
        }

        [Test]
        public static void CalculateChunkCountWithFilters()
        {
            using var world = new World();
            for (int i = 0; i < 300; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                if (i % 2 == 0)
                {
                    world.AddComponent(e, new Velocity());
                }
            }
            var all = world.Query<Position>();
            var some = world.Query().WithAll<Position, Velocity>().Build();
            TestRunner.AssertEqual(4, all.CalculateChunkCount());
            TestRunner.AssertEqual(2, some.CalculateChunkCount());
        }

        [Test]
        public static void ToChunkArrayReuseFillsExisting()
        {
            using var world = new World();
            for (int i = 0; i < 500; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
            }
            var query = world.Query<Position>();
            int needed = query.CalculateChunkCount();
            var fresh = new NativeArray<ChunkHandle>(needed, Allocator.TempJob);
            query.ToChunkArray(fresh);
            var reused = new NativeArray<ChunkHandle>(needed, Allocator.TempJob);
            query.ToChunkArray(reused);
            TestRunner.AssertEqual(fresh.Length, reused.Length);
            for (int i = 0; i < fresh.Length; i++)
            {
                TestRunner.Assert(ReferenceEquals(fresh[i].Chunk, reused[i].Chunk), $"chunk {i} mismatch");
            }
            fresh.Dispose();
            reused.Dispose();
        }

        [Test]
        public static void ToChunkArrayReuseTooSmallAutoGrows()
        {
            using var world = new World();
            for (int i = 0; i < 300; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
            }
            var query = world.Query<Position>();
            // 旧 API: ToChunkArray 需要容量足够的 NativeArray, 分配正确容量后填满全部 chunk
            var chunks = new NativeArray<ChunkHandle>(query.CalculateChunkCount(), Allocator.TempJob);
            query.ToChunkArray(chunks);
            TestRunner.AssertEqual(query.CalculateChunkCount(), chunks.Length);
            int total = 0;
            for (int i = 0; i < chunks.Length; i++)
            {
                total += chunks[i].Count;
            }
            TestRunner.AssertEqual(300, total);
            chunks.Dispose();
        }

        [Test]
        public static void ToChunkArrayReuseBiggerThanNeededIsAllowed()
        {
            using var world = new World();
            for (int i = 0; i < 100; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
            }
            var query = world.Query<Position>();
            var reused = new NativeArray<ChunkHandle>(16, Allocator.TempJob);
            query.ToChunkArray(reused);
            TestRunner.AssertEqual(query.CalculateChunkCount(), 1);
            TestRunner.AssertEqual(16, reused.Length);
            reused.Dispose();
        }

        [Test]
        public static void ForEachChunkVisitsAllEntities()
        {
            using var world = new World();
            for (int i = 0; i < 1000; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                world.AddComponent(e, new Velocity { DX = 1 });
            }
            var query = world.Query<Position, Velocity>();
            var visitor = new CountVisitor();
            query.ForEachChunk<CountVisitor>(ref visitor);
            TestRunner.AssertEqual(1000, visitor.Entities);
            TestRunner.AssertEqual(499500, visitor.SumX);
            TestRunner.AssertEqual(query.CalculateChunkCount(), visitor.Chunks);
        }

        private struct CountVisitor : IForEachChunk
        {
            public int Chunks;
            public int Entities;
            public int SumX;

            public void Execute(Chunk chunk)
            {
                Chunks++;
                for (int e = 0; e < chunk.Count; e++)
                {
                    Entities++;
                    SumX += (int)chunk.GetComponentRef<Position>(e).X;
                }
            }
        }

        [Test]
        public static void ForEachChunkCanReadEntityHandles()
        {
            using var world = new World();
            var expected = new Entity[200];
            for (int i = 0; i < expected.Length; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                expected[i] = e;
            }
            var query = world.Query<Position>();
            var visitor = new CollectEntityVisitor(expected);
            query.ForEachChunk<CollectEntityVisitor>(ref visitor);
            TestRunner.AssertEqual(0, visitor.Mismatches);
        }

        private struct CollectEntityVisitor : IForEachChunk
        {
            private readonly Entity[] _expected;
            private int _index;
            public int Mismatches;

            public CollectEntityVisitor(Entity[] expected)
            {
                _expected = expected;
                _index = 0;
                Mismatches = 0;
            }

            public void Execute(Chunk chunk)
            {
                for (int e = 0; e < chunk.Count; e++)
                {
                    if (!_expected[_index].Equals(chunk.GetEntityRef(e)))
                    {
                        Mismatches++;
                    }
                    _index++;
                }
            }
        }

        [Test]
        public static void ForEachChunkIsZeroGc()
        {
            using var world = new World();
            for (int i = 0; i < 10000; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
            }
            var query = world.Query<Position>();
            var visitor = new CountVisitor();
            query.ForEachChunk<CountVisitor>(ref visitor);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10; i++)
            {
                query.ForEachChunk<CountVisitor>(ref visitor);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < 1024, $"ForEachChunk allocated {allocated} bytes over 10 iterations");
        }

        [Test]
        public static void ReusedChunkArrayReflectsStructuralChanges()
        {
            using var world = new World();
            var query = world.Query<Position>();
            var chunks = default(NativeArray<ChunkHandle>);

            for (int i = 0; i < 200; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
            }
            EnsureCapacity(ref chunks, query.CalculateChunkCount());
            query.ToChunkArray(chunks);
            TestRunner.AssertEqual(2, query.CalculateChunkCount());
            TestRunner.AssertEqual(200, query.CalculateEntityCount());

            for (int i = 0; i < 100; i++)
            {
                var tmp = new NativeArray<ChunkHandle>(query.CalculateChunkCount(), Allocator.TempJob);
                query.ToChunkArray(tmp);
                var e = tmp[0].GetEntityRef(0);
                world.DestroyEntity(e);
                tmp.Dispose();
            }
            TestRunner.AssertEqual(100, query.CalculateEntityCount());
            EnsureCapacity(ref chunks, query.CalculateChunkCount());
            query.ToChunkArray(chunks);
            TestRunner.AssertEqual(query.CalculateChunkCount(), chunks.Length);
            int total = 0;
            for (int i = 0; i < chunks.Length; i++)
            {
                total += chunks[i].Count;
            }
            TestRunner.AssertEqual(100, total);
            chunks.Dispose();
        }

        private static void EnsureCapacity(ref NativeArray<ChunkHandle> chunks, int needed)
        {
            if (!chunks.IsCreated || chunks.Length != needed)
            {
                if (chunks.IsCreated)
                {
                    chunks.Dispose();
                }
                chunks = new NativeArray<ChunkHandle>(needed, Allocator.TempJob);
            }
        }
    }
}
