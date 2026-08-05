namespace EngineX.ECS.Tests
{
    public static class EcsSystemTests
    {
        private sealed class MoveSystem : ISystem
        {
            public EntityQuery Query;
            public int CreateCount;
            public int UpdateCount;
            public int DestroyCount;

            public void OnCreate(ref SystemState state)
            {
                CreateCount++;
                Query = state.World.Query<Position, Velocity>();
            }

            public void OnUpdate(ref SystemState state)
            {
                UpdateCount++;
                var visitor = new MoveVisitor();
                Query.ForEach<MoveVisitor, Position, Velocity>(ref visitor);
            }

            public void OnDestroy(ref SystemState state)
            {
                DestroyCount++;
            }
        }

        private struct MoveVisitor : IForEach<Position, Velocity>
        {
            public void Execute(ref Position p, ref Velocity v)
            {
                p.X += v.DX;
                p.Y += v.DY;
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
        public static void SystemLifecycleAndUpdate()
        {
            using var world = new World();
            var system = new MoveSystem();
            var group = new SystemsGroup();
            group.Add(system);
            group.Create(world);

            TestRunner.AssertEqual(1, system.CreateCount);
            for (int i = 0; i < 10; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = 1, Y = 2 });
                world.AddComponent(e, new Velocity { DX = 1, DY = 1 });
            }

            group.Update(world);
            group.Update(world);
            TestRunner.AssertEqual(2, system.UpdateCount);

            var sumVisitor = new SumXVisitor();
            system.Query.ForEach<SumXVisitor, Position>(ref sumVisitor);
            TestRunner.AssertEqual(30, sumVisitor.Sum);

            group.Destroy();
            TestRunner.AssertEqual(1, system.DestroyCount);
        }

        [Test]
        public static void MultipleSystemsRunInOrder()
        {
            using var world = new World();
            var e = world.CreateEntity();
            world.AddComponent(e, new Position { X = 0 });
            world.AddComponent(e, new Velocity { DX = 1 });
            world.AddComponent(e, new Tag());

            var order = new System.Collections.Generic.List<string>();
            var group = new SystemsGroup();
            group.Add(new OrderSystem("a", order));
            group.Add(new OrderSystem("b", order));
            group.Add(new OrderSystem("c", order));
            group.Create(world);
            group.Update(world);

            TestRunner.AssertEqual(3, order.Count);
            TestRunner.AssertEqual("a", order[0]);
            TestRunner.AssertEqual("b", order[1]);
            TestRunner.AssertEqual("c", order[2]);
        }

        private sealed class OrderSystem : ISystem
        {
            private readonly string _name;
            private readonly System.Collections.Generic.List<string> _order;

            public OrderSystem(string name, System.Collections.Generic.List<string> order)
            {
                _name = name;
                _order = order;
            }

            public void OnCreate(ref SystemState state)
            {
            }

            public void OnUpdate(ref SystemState state)
            {
                _order.Add(_name);
            }

            public void OnDestroy(ref SystemState state)
            {
            }
        }
    }
}
