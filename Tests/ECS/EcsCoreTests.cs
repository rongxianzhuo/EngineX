using System;
using EngineX.ECS;

namespace EngineX.ECS.Tests
{
    public struct Position : IComponentData
    {
        public float X;
        public float Y;
    }

    public struct Velocity : IComponentData
    {
        public float DX;
        public float DY;
    }

    public struct Health : IComponentData
    {
        public int Value;
    }

    public struct Dead : IComponentData
    {
    }

    public struct Tag : IComponentData
    {
    }

    public static class EcsCoreTests
    {
        [Test]
        public static void CreateDestroyEntity()
        {
            using var world = new World();
            var e = world.CreateEntity();
            TestRunner.Assert(world.Exists(e));
            TestRunner.AssertEqual(1, world.EntityCount);
            world.DestroyEntity(e);
            TestRunner.Assert(!world.Exists(e));
            TestRunner.AssertEqual(0, world.EntityCount);
        }

        [Test]
        public static void EntityVersionInvalidation()
        {
            using var world = new World();
            var e1 = world.CreateEntity();
            world.DestroyEntity(e1);
            var e2 = world.CreateEntity();
            TestRunner.AssertEqual(e1.Index, e2.Index);
            TestRunner.Assert(e1 != e2);
            TestRunner.Assert(!world.Exists(e1));
            TestRunner.Assert(world.Exists(e2));
        }

        [Test]
        public static void DestroyStaleEntityIsNoOp()
        {
            using var world = new World();
            var e1 = world.CreateEntity();
            world.DestroyEntity(e1);
            var e2 = world.CreateEntity();
            world.DestroyEntity(e1);
            TestRunner.Assert(world.Exists(e2));
        }

        [Test]
        public static void AddGetSetComponent()
        {
            using var world = new World();
            var e = world.CreateEntity();
            world.AddComponent(e, new Position { X = 1, Y = 2 });
            TestRunner.Assert(world.HasComponent<Position>(e));
            var p = world.GetComponent<Position>(e);
            TestRunner.AssertEqual(1f, p.X);
            TestRunner.AssertEqual(2f, p.Y);
            world.SetComponent(e, new Position { X = 5, Y = 6 });
            p = world.GetComponent<Position>(e);
            TestRunner.AssertEqual(5f, p.X);
            TestRunner.AssertEqual(6f, p.Y);
        }

        [Test]
        public static void GetMissingComponentThrows()
        {
            using var world = new World();
            var e = world.CreateEntity();
            bool threw = false;
            try
            {
                world.GetComponent<Position>(e);
            }
            catch (ArgumentException)
            {
                threw = true;
            }
            TestRunner.Assert(threw);
        }

        [Test]
        public static void RemoveComponentMigration()
        {
            using var world = new World();
            var e = world.CreateEntity();
            world.AddComponent(e, new Position { X = 3, Y = 4 });
            world.AddComponent(e, new Velocity { DX = 1, DY = 2 });
            TestRunner.Assert(world.HasComponent<Position>(e));
            TestRunner.Assert(world.HasComponent<Velocity>(e));
            world.RemoveComponent<Velocity>(e);
            TestRunner.Assert(world.HasComponent<Position>(e));
            TestRunner.Assert(!world.HasComponent<Velocity>(e));
            var p = world.GetComponent<Position>(e);
            TestRunner.AssertEqual(3f, p.X);
            TestRunner.AssertEqual(4f, p.Y);
        }

        [Test]
        public static void ChunkBoundaryAcrossEntities()
        {
            using var world = new World();
            var entities = new Entity[Chunk.Capacity + 1];
            for (int i = 0; i < entities.Length; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                entities[i] = e;
            }
            TestRunner.AssertEqual(entities.Length, world.EntityCount);
            TestRunner.AssertEqual(entities.Length, world.Query<Position>().CalculateEntityCount());
            for (int i = 0; i < entities.Length; i++)
            {
                var p = world.GetComponent<Position>(entities[i]);
                TestRunner.AssertEqual((float)i, p.X);
            }
        }

        [Test]
        public static void SwapRemovalKeepsDataConsistent()
        {
            using var world = new World();
            var entities = new Entity[300];
            for (int i = 0; i < entities.Length; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                entities[i] = e;
            }
            for (int i = 0; i < 100; i++)
            {
                world.DestroyEntity(entities[i]);
            }
            TestRunner.AssertEqual(200, world.EntityCount);
            for (int i = 100; i < entities.Length; i++)
            {
                var p = world.GetComponent<Position>(entities[i]);
                TestRunner.AssertEqual((float)i, p.X);
            }
        }

        [Test]
        public static void AddComponentPreservesExistingData()
        {
            using var world = new World();
            var entities = new Entity[500];
            for (int i = 0; i < entities.Length; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i, Y = i * 2 });
                entities[i] = e;
            }
            for (int i = 0; i < entities.Length; i++)
            {
                world.AddComponent(entities[i], new Velocity { DX = i + 1 });
            }
            for (int i = 0; i < entities.Length; i++)
            {
                var p = world.GetComponent<Position>(entities[i]);
                var v = world.GetComponent<Velocity>(entities[i]);
                TestRunner.AssertEqual((float)i, p.X);
                TestRunner.AssertEqual((float)(i * 2), p.Y);
                TestRunner.AssertEqual((float)(i + 1), v.DX);
            }
        }

        [Test]
        public static void RandomStructuralStress()
        {
            using var world = new World();
            var random = new Random(12345);
            var expected = new System.Collections.Generic.Dictionary<Entity, Position>();
            for (int i = 0; i < 2000; i++)
            {
                var e = world.CreateEntity();
                world.AddComponent(e, new Position { X = i });
                expected[e] = new Position { X = i };
            }
            for (int i = 0; i < 3000; i++)
            {
                var keys = new Entity[expected.Count];
                expected.Keys.CopyTo(keys, 0);
                var e = keys[random.Next(keys.Length)];
                int op = random.Next(4);
                if (op == 0)
                {
                    world.AddComponent(e, new Health { Value = 42 });
                }
                else if (op == 1)
                {
                    world.RemoveComponent<Health>(e);
                }
                else if (op == 2)
                {
                    var p = expected[e];
                    world.SetComponent(e, new Position { X = p.X + 1 });
                    expected[e] = new Position { X = p.X + 1 };
                }
                else
                {
                    world.DestroyEntity(e);
                    expected.Remove(e);
                }
            }
            TestRunner.AssertEqual(expected.Count, world.EntityCount);
            foreach (var pair in expected)
            {
                var p = world.GetComponent<Position>(pair.Key);
                TestRunner.AssertEqual(pair.Value.X, p.X);
            }
        }
    }
}
