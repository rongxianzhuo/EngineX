using System;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.ECS.Components;
using EngineX.Jobs;

namespace EngineX.ECS.Tests
{
    public static class EcsComponentsTests
    {
        private const float Tolerance = 0.01f;

        [Test]
        public static void TransformDataDefaultsToIdentity()
        {
            var t = new TransformData();
            TestRunner.AssertEqual(Vector3.Zero, t.Position);
            TestRunner.AssertEqual(default(Quaternion), t.Rotation);
            TestRunner.AssertEqual(Vector3.Zero, t.Scale);
            TestRunner.AssertEqual(Vector3.Zero, TransformData.Identity.Position);
            TestRunner.AssertEqual(Quaternion.Identity, TransformData.Identity.Rotation);
            TestRunner.AssertEqual(Vector3.One, TransformData.Identity.Scale);
        }

        [Test]
        public static void TransformDataRoundTripThroughWorld()
        {
            using var world = new World();
            var e = world.CreateEntity();
            var transform = new TransformData(
                new Vector3(FP.FromInt(10), FP.FromInt(20), FP.FromInt(30)),
                Quaternion.Euler(FP.FromInt(0), FP.FromInt(90), FP.FromInt(0)),
                new Vector3(FP.FromInt(2), FP.FromInt(2), FP.FromInt(2)));
            world.AddComponent(e, transform);
            var read = world.GetComponent<TransformData>(e);
            TestRunner.AssertEqual(transform.Position, read.Position);
            TestRunner.AssertEqual(transform.Rotation, read.Rotation);
            TestRunner.AssertEqual(transform.Scale, read.Scale);
            TestRunner.Assert(world.HasComponent<TransformData>(e));
        }

        [Test]
        public static void TransformDataFromEuler()
        {
            var t = TransformData.FromEuler(
                new Vector3(FP.FromInt(1), FP.FromInt(2), FP.FromInt(3)),
                new Vector3(FP.FromInt(0), FP.FromInt(90), FP.FromInt(0)));
            var euler = t.EulerAngles;
            TestRunner.Assert(Math.Abs(euler.X.Single()) < Tolerance);
            TestRunner.Assert(Math.Abs(euler.Y.Single() - 90f) < Tolerance);
            TestRunner.Assert(Math.Abs(euler.Z.Single()) < Tolerance);
        }

        [Test]
        public static void TransformDataWithMethods()
        {
            var t = TransformData.Identity
                .WithPosition(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero))
                .WithScale(new Vector3(FP.FromInt(2), FP.FromInt(2), FP.FromInt(2)));
            TestRunner.AssertEqual(new Vector3(FP.FromInt(5), FP.Zero, FP.Zero), t.Position);
            TestRunner.AssertEqual(new Vector3(FP.FromInt(2), FP.FromInt(2), FP.FromInt(2)), t.Scale);
            TestRunner.AssertEqual(Quaternion.Identity, t.Rotation);
        }

        [Test]
        public static void TransformPointAppliesScaleRotationTranslation()
        {
            var t = TransformData.FromEuler(
                new Vector3(FP.FromInt(100), FP.Zero, FP.Zero),
                new Vector3(FP.Zero, FP.FromInt(90), FP.Zero),
                new Vector3(FP.FromInt(2), FP.FromInt(2), FP.FromInt(2)));
            var result = t.TransformPoint(new Vector3(FP.One, FP.Zero, FP.Zero));
            TestRunner.Assert(Math.Abs(result.X.Single() - 100f) < Tolerance);
            TestRunner.Assert(Math.Abs(result.Y.Single()) < Tolerance);
            TestRunner.Assert(Math.Abs(result.Z.Single() - (-2f)) < Tolerance);
        }

        [Test]
        public static void TransformDataInQueryForEach()
        {
            using var world = new World();
            for (int i = 0; i < 100; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, TransformData.FromEuler(
                    new Vector3(FP.FromInt(i), FP.Zero, FP.Zero),
                    new Vector3(FP.Zero, FP.FromInt(i), FP.Zero)));
            }
            var query = world.Query<TransformData>();
            var addOne = new AddOneVisitor();
            query.ForEach<AddOneVisitor, TransformData>(ref addOne);

            var sum = new SumXVisitor();
            query.ForEach<SumXVisitor, TransformData>(ref sum);
            TestRunner.AssertEqual(5050, sum.Sum);
        }

        private struct AddOneVisitor : IForEach<TransformData>
        {
            public void Execute(ref TransformData t)
            {
                t.Position = new Vector3(t.Position.X + FP.One, t.Position.Y, t.Position.Z);
            }
        }

        private struct SumXVisitor : IForEach<TransformData>
        {
            public int Sum;

            public void Execute(ref TransformData t)
            {
                Sum += t.Position.X.Int();
            }
        }

        [Test]
        public static void TransformDataInChunkJob()
        {
            using var world = new World();
            for (int i = 0; i < 300; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new TransformData(new Vector3(FP.FromInt(i), FP.Zero, FP.Zero)));
            }
            var query = world.Query<TransformData>();
            var chunks = new NativeArray<ChunkHandle>(query.CalculateChunkCount(), Allocator.TempJob);
            query.ToChunkArray(chunks);
            var handle = JobSystem.ScheduleParallel(new MoveTransformJob { Chunks = chunks }, chunks, 1);
            handle.Complete();
            chunks.Dispose();

            var sum = new SumXVisitor();
            query.ForEach<SumXVisitor, TransformData>(ref sum);
            TestRunner.AssertEqual(45150, sum.Sum);
        }

        private struct MoveTransformJob : IJobParallelForBatch
        {
            public NativeArray<ChunkHandle> Chunks;

            public void Execute(int startIndex, int count)
            {
                for (int i = startIndex; i < startIndex + count; i++)
                {
                    var chunk = Chunks[i].Chunk;
                    for (int e = 0; e < chunk.Count; e++)
                    {
                        ref var t = ref chunk.GetComponentRef<TransformData>(e);
                        t.Position = new Vector3(t.Position.X + FP.One, t.Position.Y, t.Position.Z);
                    }
                }
            }
        }
    }
}
