using System;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.ECS.Components;
using EngineX.Jobs;

namespace EngineX.ECS.Tests
{
    public static class CameraDataTests
    {
        private const float Tolerance = 0.01f;

        [Test]
        public static void CameraDataDefaultValues()
        {
            var c = CameraData.Default;
            TestRunner.Assert(Math.Abs(c.Fov.Single() - 60f) < Tolerance);
            TestRunner.Assert(Math.Abs(c.NearClip.Single() - 0.3f) < Tolerance);
            TestRunner.Assert(Math.Abs(c.FarClip.Single() - 1000f) < Tolerance);
            TestRunner.AssertEqual(CameraProjection.Perspective, c.Projection);
        }

        [Test]
        public static void CameraDataDefaultStructIsInvalidCamera()
        {
            var c = default(CameraData);
            TestRunner.AssertEqual(FP.Zero, c.Fov);
            TestRunner.AssertEqual(FP.Zero, c.NearClip);
            TestRunner.AssertEqual(FP.Zero, c.FarClip);
            TestRunner.AssertEqual(default(CameraProjection), c.Projection);
        }

        [Test]
        public static void CameraDataConstructorDefaults()
        {
            var c = new CameraData(FP.FromInt(75));
            TestRunner.Assert(Math.Abs(c.Fov.Single() - 75f) < Tolerance);
            TestRunner.Assert(Math.Abs(c.NearClip.Single() - 0.3f) < Tolerance);
            TestRunner.Assert(Math.Abs(c.FarClip.Single() - 1000f) < Tolerance);
            TestRunner.AssertEqual(CameraProjection.Perspective, c.Projection);
        }

        [Test]
        public static void CameraDataFullConstructor()
        {
            var c = new CameraData(FP.FromInt(90), FP.FromFloat(0.1f), FP.FromInt(500), CameraProjection.Orthographic);
            TestRunner.AssertEqual(FP.FromInt(90), c.Fov);
            TestRunner.AssertEqual(FP.FromFloat(0.1f), c.NearClip);
            TestRunner.AssertEqual(FP.FromInt(500), c.FarClip);
            TestRunner.AssertEqual(CameraProjection.Orthographic, c.Projection);
        }

        [Test]
        public static void CameraEntityUsesTransformDataForPlacement()
        {
            using var world = new World();
            var e = world.CreateEntity();
            world.AddComponent(e, TransformData.FromEuler(
                new Vector3(FP.FromInt(1), FP.FromInt(2), FP.FromInt(3)),
                new Vector3(FP.Zero, FP.FromInt(90), FP.Zero)));
            world.AddComponent(e, CameraData.Default);
            var transform = world.GetComponent<TransformData>(e);
            var camera = world.GetComponent<CameraData>(e);
            TestRunner.AssertEqual(FP.FromInt(1), transform.Position.X);
            TestRunner.AssertEqual(FP.FromInt(60), camera.Fov);
            TestRunner.Assert(world.HasComponent<CameraData>(e));
        }

        [Test]
        public static void CameraDataInQueryForEach()
        {
            using var world = new World();
            for (int i = 0; i < 100; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new CameraData(FP.FromInt(60 + i)));
            }
            var query = world.Query<CameraData>();
            var visitor = new SumFovVisitor();
            query.ForEach<SumFovVisitor, CameraData>(ref visitor);
            int expected = 0;
            for (int i = 0; i < 100; i++)
            {
                expected += 60 + i;
            }
            TestRunner.AssertEqual(expected, visitor.SumFov);
        }

        private struct SumFovVisitor : IForEach<CameraData>
        {
            public int SumFov;

            public void Execute(ref CameraData c)
            {
                SumFov += c.Fov.Int();
            }
        }

        [Test]
        public static void CameraDataInChunkJob()
        {
            using var world = new World();
            for (int i = 0; i < 200; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new CameraData(FP.FromInt(60)));
            }
            var query = world.Query<CameraData>();
            var chunks = new NativeArray<ChunkHandle>(query.CalculateChunkCount(), Allocator.TempJob);
            query.ToChunkArray(chunks);
            var handle = JobSystem.ScheduleParallel(new UpdateFovJob { Chunks = chunks }, chunks, 1);
            handle.Complete();
            chunks.Dispose();
            var visitor = new CountFovVisitor();
            query.ForEach<CountFovVisitor, CameraData>(ref visitor);
            TestRunner.AssertEqual(200, visitor.Updated);
        }

        private struct UpdateFovJob : IJobParallelForBatch
        {
            public NativeArray<ChunkHandle> Chunks;

            public void Execute(int startIndex, int count)
            {
                for (int i = startIndex; i < startIndex + count; i++)
                {
                    var chunk = Chunks[i].Chunk;
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        ref var c = ref chunk.GetComponentRef<CameraData>(e);
                        c.Fov = FP.FromInt(90);
                    }
                }
            }
        }

        private struct CountFovVisitor : IForEach<CameraData>
        {
            public int Updated;

            public void Execute(ref CameraData c)
            {
                if (c.Fov == FP.FromInt(90))
                {
                    Updated++;
                }
            }
        }
    }
}
