using EngineX.Baseline.FixedPoint;
using EngineX.ECS.Components;

namespace EngineX.ECS.Tests
{
    public static class InputDataTests
    {
        [Test]
        public static void InputDataDefaultIsNoInput()
        {
            var input = default(InputData);
            TestRunner.AssertEqual(FP.Zero, input.Horizontal);
            TestRunner.AssertEqual(FP.Zero, input.Vertical);
        }

        [Test]
        public static void InputDataRoundTripThroughWorld()
        {
            using var world = new World();
            var e = world.CreateEntity();
            world.AddComponent(e, new InputData { Horizontal = FP.One, Vertical = FP.FromFloat(-0.5f) });
            var read = world.GetComponent<InputData>(e);
            TestRunner.AssertEqual(FP.One, read.Horizontal);
            TestRunner.AssertEqual(FP.FromFloat(-0.5f), read.Vertical);
            TestRunner.Assert(world.HasComponent<InputData>(e));
        }

        [Test]
        public static void InputDataInQueryForEach()
        {
            using var world = new World();
            for (int i = 0; i < 100; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new InputData { Horizontal = FP.FromInt(i), Vertical = FP.One });
            }
            var query = world.Query<InputData>();
            var visitor = new SumInputVisitor();
            query.ForEach<SumInputVisitor, InputData>(ref visitor);
            TestRunner.AssertEqual(4950, visitor.SumHorizontal);
            TestRunner.AssertEqual(100, visitor.CountVerticalOne);
        }

        private struct SumInputVisitor : IForEach<InputData>
        {
            public int SumHorizontal;
            public int CountVerticalOne;

            public void Execute(ref InputData input)
            {
                SumHorizontal += input.Horizontal.Int();
                if (input.Vertical == FP.One)
                {
                    CountVerticalOne++;
                }
            }
        }
    }
}
