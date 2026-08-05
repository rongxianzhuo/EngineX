using EngineX.ECS.Components;
using EngineX.Jobs;

namespace EngineX.ECS.Tests
{
    public static class RenderDataTests
    {
        [Test]
        public static void RenderDataDefaultsToNullPaths()
        {
            var r = new RenderData();
            TestRunner.Assert(r.MeshPath == null);
            TestRunner.Assert(r.MaterialPath == null);
        }

        [Test]
        public static void RenderDataConstructor()
        {
            var r = new RenderData("meshes/cube.obj", "materials/red.mat");
            TestRunner.AssertEqual("meshes/cube.obj", r.MeshPath);
            TestRunner.AssertEqual("materials/red.mat", r.MaterialPath);
        }

        [Test]
        public static void RenderDataRoundTripThroughWorld()
        {
            using var world = new World();
            var e = world.CreateEntity();
            world.AddComponent(e, new RenderData("meshes/sphere.obj", "materials/blue.mat"));
            var read = world.GetComponent<RenderData>(e);
            TestRunner.AssertEqual("meshes/sphere.obj", read.MeshPath);
            TestRunner.AssertEqual("materials/blue.mat", read.MaterialPath);
            TestRunner.Assert(world.HasComponent<RenderData>(e));
        }

        [Test]
        public static void RenderDataMigratesWithArchetypeChanges()
        {
            using var world = new World();
            var e = world.CreateEntity();
            world.AddComponent(e, new RenderData("meshes/hero.fbx", "materials/skin.mat"));
            world.AddComponent(e, new Position { X = 1 });
            world.RemoveComponent<RenderData>(e);
            TestRunner.Assert(!world.HasComponent<RenderData>(e));
            world.AddComponent(e, new RenderData("meshes/hero.fbx", "materials/skin.mat"));
            var read = world.GetComponent<RenderData>(e);
            TestRunner.AssertEqual("meshes/hero.fbx", read.MeshPath);
            TestRunner.AssertEqual("materials/skin.mat", read.MaterialPath);
        }

        [Test]
        public static void RenderDataInQueryAndForEachChunk()
        {
            using var world = new World();
            for (int i = 0; i < 300; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new RenderData($"meshes/m{i}.obj", "materials/common.mat"));
            }
            var query = world.Query<RenderData>();
            TestRunner.AssertEqual(300, query.CalculateEntityCount());

            var visitor = new RenderVisitor();
            query.ForEachChunk<RenderVisitor>(ref visitor);
            TestRunner.AssertEqual(300, visitor.Count);
            TestRunner.AssertEqual(300, visitor.CommonMaterialCount);
        }

        private struct RenderVisitor : IForEachChunk
        {
            public int Count;
            public int CommonMaterialCount;

            public void Execute(Chunk chunk)
            {
                for (int e = 0; e < chunk.Count; e++)
                {
                    ref var r = ref chunk.GetComponentRef<RenderData>(e);
                    Count++;
                    if (r.MaterialPath == "materials/common.mat")
                    {
                        CommonMaterialCount++;
                    }
                }
            }
        }

        [Test]
        public static void RenderDataInChunkJob()
        {
            using var world = new World();
            for (int i = 0; i < 200; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new RenderData($"meshes/m{i}.obj", "materials/old.mat"));
            }
            var query = world.Query<RenderData>();
            var chunks = new NativeArray<ChunkHandle>(query.CalculateChunkCount(), Allocator.TempJob);
            query.ToChunkArray(chunks);
            var handle = JobSystem.ScheduleParallel(new UpdateMaterialJob { Chunks = chunks }, chunks, 1);
            handle.Complete();
            chunks.Dispose();

            var countVisitor = new CountUpdatedVisitor();
            query.ForEach<CountUpdatedVisitor, RenderData>(ref countVisitor);
            TestRunner.AssertEqual(200, countVisitor.Updated);
        }

        private struct CountUpdatedVisitor : IForEach<RenderData>
        {
            public int Updated;

            public void Execute(ref RenderData r)
            {
                if (r.MaterialPath == "materials/new.mat")
                {
                    Updated++;
                }
            }
        }

        private struct UpdateMaterialJob : IJobParallelForBatch
        {
            public NativeArray<ChunkHandle> Chunks;

            public void Execute(int startIndex, int count)
            {
                for (int i = startIndex; i < startIndex + count; i++)
                {
                    var chunk = Chunks[i].Chunk;
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        ref var r = ref chunk.GetComponentRef<RenderData>(e);
                        r.MaterialPath = "materials/new.mat";
                    }
                }
            }
        }
    }
}
