using System;

namespace EngineX.ECS.Tests
{
    public static class ZeroGcStructuralTests
    {
        [Test]
        public static void AddComponentZeroGcAfterWarmup()
        {
            using var world = new World();
            var entities = new Entity[100];
            for (int i = 0; i < entities.Length; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                entities[i] = e;
            }

            for (int warmup = 0; warmup < 3; warmup++)
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    world.AddComponent(entities[i], new Velocity { DX = i });
                }
                for (int i = 0; i < entities.Length; i++)
                {
                    world.RemoveComponent<Velocity>(entities[i]);
                }
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int round = 0; round < 5; round++)
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    world.AddComponent(entities[i], new Velocity { DX = i });
                }
                for (int i = 0; i < entities.Length; i++)
                {
                    world.RemoveComponent<Velocity>(entities[i]);
                }
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < 1024, $"AddComponent/RemoveComponent allocated {allocated} bytes over 5 rounds (archetype graph should be zero-GC)");
        }

        [Test]
        public static void MixedStructuralOpsZeroGc()
        {
            using var world = new World();
            var entities = new Entity[50];
            for (int i = 0; i < entities.Length; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                entities[i] = e;
            }

            for (int warmup = 0; warmup < 3; warmup++)
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    world.AddComponent(entities[i], new Velocity { DX = i });
                    world.AddComponent(entities[i], new Health { Value = i });
                    world.RemoveComponent<Velocity>(entities[i]);
                    world.RemoveComponent<Health>(entities[i]);
                }
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int round = 0; round < 5; round++)
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    world.AddComponent(entities[i], new Velocity { DX = i });
                    world.AddComponent(entities[i], new Health { Value = i });
                    world.RemoveComponent<Velocity>(entities[i]);
                    world.RemoveComponent<Health>(entities[i]);
                }
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < 2048, $"Mixed structural ops allocated {allocated} bytes over 5 rounds");
        }

        [Test]
        public static void ArchetypeGraphReusesTargetArchetype()
        {
            using var world = new World();
            var entities = new Entity[10];
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = world.CreateEntity();
                world.AddComponent(entities[i], new Position { X = i });
            }
            int archetypeCountBefore = world.ArchetypeCount;
            for (int i = 0; i < entities.Length; i++)
            {
                world.AddComponent(entities[i], new Velocity { DX = i });
            }
            int archetypeCountAfterOne = world.ArchetypeCount;
            TestRunner.AssertEqual(archetypeCountBefore + 1, archetypeCountAfterOne);

            for (int i = 0; i < entities.Length; i++)
            {
                world.RemoveComponent<Velocity>(entities[i]);
            }
            for (int i = 0; i < entities.Length; i++)
            {
                world.AddComponent(entities[i], new Velocity { DX = i });
            }
            TestRunner.AssertEqual(archetypeCountAfterOne, world.ArchetypeCount);
        }

        [Test]
        public static void StructuralOpsPreserveData()
        {
            using var world = new World();
            var entities = new Entity[100];
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = world.CreateEntity();
                world.AddComponent(entities[i], new Position { X = i, Y = i * 2 });
            }
            for (int round = 0; round < 5; round++)
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    world.AddComponent(entities[i], new Velocity { DX = i });
                    world.RemoveComponent<Velocity>(entities[i]);
                }
                var visitor = new SumPositionVisitor();
                world.Query<Position>().ForEach<SumPositionVisitor, Position>(ref visitor);
                TestRunner.AssertEqual(4950f, visitor.SumX);
            }
        }

        private struct SumPositionVisitor : IForEach<Position>
        {
            public float SumX;

            public void Execute(ref Position p)
            {
                SumX += p.X;
            }
        }
    }
}
