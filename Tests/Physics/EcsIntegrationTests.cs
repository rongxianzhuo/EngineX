using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.ECS;
using EngineX.ECS.Components;
using EngineX.Physics;
using EngineX.Physics.Components;

namespace EngineX.Physics.Tests
{
    public static class EcsIntegrationTests
    {
        private static Entity CreatePhysicsEntity(
            ECS.World ecsWorld,
            Vector3 position,
            Vector3 halfExtents,
            FP mass = default)
        {
            if (mass == default) mass = FP.One;
            Entity entity = ecsWorld.CreateEntity();
            ecsWorld.AddComponent(entity, TransformData.Identity.WithPosition(position));
            ecsWorld.AddComponent(entity, ColliderComponent.Box(halfExtents));
            ecsWorld.AddComponent(entity, PhysicsStateComponent.Dynamic(mass, FP.One));
            ecsWorld.AddComponent(entity, new PhysicsVelocityComponent());
            ecsWorld.AddComponent(entity, new RigidBodyComponent());
            return entity;
        }

        [Test]
        public static void EntityCreatesBodyOnFirstUpdate()
        {
            var ecsWorld = new ECS.World();
            var systemsGroup = new SystemsGroup();
            var physicsSystem = new PhysicsSystem();
            physicsSystem.FixedDt = FP.FromFloat(0.01f);
            systemsGroup.Add(physicsSystem);

            Entity entity = CreatePhysicsEntity(
                ecsWorld,
                new Vector3(FP.Zero, FP.FromInt(5), FP.Zero),
                new Vector3(FP.Half, FP.Half, FP.Half));

            var state = new SystemState { World = ecsWorld };
            physicsSystem.OnCreate(ref state);
            physicsSystem.OnUpdate(ref state);

            RigidBodyComponent rb = ecsWorld.GetComponent<RigidBodyComponent>(entity);
            TestRunner.Assert(rb.Handle.IsValid, "body handle should be valid after first update");
            TestRunner.Assert(physicsSystem.World.TryGetBody(rb.Handle, out _),
                "body should be in physics world");
        }

        [Test]
        public static void EntityFallsUnderGravity()
        {
            var ecsWorld = new ECS.World();
            var systemsGroup = new SystemsGroup();
            var physicsSystem = new PhysicsSystem();
            physicsSystem.FixedDt = FP.FromFloat(0.005f);
            systemsGroup.Add(physicsSystem);

            Entity entity = CreatePhysicsEntity(
                ecsWorld,
                new Vector3(FP.Zero, FP.FromInt(5), FP.Zero),
                new Vector3(FP.Half, FP.Half, FP.Half));

            var state = new SystemState { World = ecsWorld };
            physicsSystem.OnCreate(ref state);
            for (int i = 0; i < 100; i++)
            {
                physicsSystem.OnUpdate(ref state);
            }

            TransformData xform = ecsWorld.GetComponent<TransformData>(entity);
            TestRunner.Assert(xform.Position.Y < FP.FromInt(5),
                $"entity should fall, got y={xform.Position.Y}");
        }

        [Test]
        public static void EntityLandsOnGround()
        {
            var ecsWorld = new ECS.World();
            var systemsGroup = new SystemsGroup();
            var physicsSystem = new PhysicsSystem();
            physicsSystem.FixedDt = FP.FromFloat(0.005f);
            systemsGroup.Add(physicsSystem);

            var ground = ecsWorld.CreateEntity();
            ecsWorld.AddComponent(ground, TransformData.Identity.WithPosition(Vector3.Zero));
            ecsWorld.AddComponent(ground, ColliderComponent.Box(new Vector3(FP.FromInt(10), FP.FromFloat(0.5f), FP.FromInt(10))));
            ecsWorld.AddComponent(ground, PhysicsStateComponent.Static());
            ecsWorld.AddComponent(ground, new RigidBodyComponent());

            Entity box = CreatePhysicsEntity(
                ecsWorld,
                new Vector3(FP.Zero, FP.FromInt(2), FP.Zero),
                new Vector3(FP.Half, FP.Half, FP.Half));

            var state = new SystemState { World = ecsWorld };
            physicsSystem.OnCreate(ref state);
            for (int i = 0; i < 200; i++)
            {
                physicsSystem.OnUpdate(ref state);
            }

            TransformData xform = ecsWorld.GetComponent<TransformData>(box);
            TestRunner.Assert(xform.Position.Y > FP.FromFloat(0.5f),
                $"box should not fall through ground, got y={xform.Position.Y}");
            TestRunner.Assert(xform.Position.Y < FP.FromInt(2),
                $"box should fall, got y={xform.Position.Y}");
        }

        [Test]
        public static void VelocityComponentControlsMotion()
        {
            var ecsWorld = new ECS.World();
            var physicsSystem = new PhysicsSystem();
            physicsSystem.FixedDt = FP.FromFloat(0.01f);
            physicsSystem.Gravity = FP.Zero;

            Entity entity = CreatePhysicsEntity(
                ecsWorld,
                Vector3.Zero,
                new Vector3(FP.Half, FP.Half, FP.Half));

            ecsWorld.SetComponent(entity, new PhysicsVelocityComponent
            {
                LinearVelocity = new Vector3(FP.One, FP.Zero, FP.Zero),
                AngularVelocity = Vector3.Zero,
            });

            var state = new SystemState { World = ecsWorld };
            physicsSystem.OnCreate(ref state);
            for (int i = 0; i < 50; i++)
            {
                physicsSystem.OnUpdate(ref state);
            }

            TransformData xform = ecsWorld.GetComponent<TransformData>(entity);
            TestRunner.Assert(xform.Position.X > FP.FromFloat(0.1f),
                $"entity should move right via velocity, got x={xform.Position.X}");
        }

        [Test]
        public static void VelocityComponentReflectsAfterUpdate()
        {
            var ecsWorld = new ECS.World();
            var physicsSystem = new PhysicsSystem();
            physicsSystem.FixedDt = FP.FromFloat(0.01f);
            physicsSystem.Gravity = FP.Zero;

            Entity entity = CreatePhysicsEntity(
                ecsWorld,
                Vector3.Zero,
                new Vector3(FP.Half, FP.Half, FP.Half));

            var state = new SystemState { World = ecsWorld };
            physicsSystem.OnCreate(ref state);
            physicsSystem.OnUpdate(ref state);

            PhysicsVelocityComponent vel = ecsWorld.GetComponent<PhysicsVelocityComponent>(entity);
            TestRunner.AssertEqual(FP.Zero, vel.LinearVelocity.Y,
                "stationary body should have zero velocity after update");
        }

        [Test]
        public static void MultipleEntitiesAllUpdated()
        {
            var ecsWorld = new ECS.World();
            var physicsSystem = new PhysicsSystem();
            physicsSystem.FixedDt = FP.FromFloat(0.005f);

            Entity[] entities = new Entity[10];
            for (int i = 0; i < 10; i++)
            {
                entities[i] = CreatePhysicsEntity(
                    ecsWorld,
                    new Vector3(FP.FromInt(i), FP.FromInt(5), FP.Zero),
                    new Vector3(FP.Half, FP.Half, FP.Half));
            }

            var state = new SystemState { World = ecsWorld };
            physicsSystem.OnCreate(ref state);
            for (int i = 0; i < 100; i++)
            {
                physicsSystem.OnUpdate(ref state);
            }

            for (int i = 0; i < 10; i++)
            {
                TransformData xform = ecsWorld.GetComponent<TransformData>(entities[i]);
                TestRunner.Assert(xform.Position.Y < FP.FromInt(5),
                    $"entity {i} should fall, got y={xform.Position.Y}");
            }
        }

        [Test]
        public static void TwoEntitiesStackVertically()
        {
            var ecsWorld = new ECS.World();
            var physicsSystem = new PhysicsSystem();
            physicsSystem.FixedDt = FP.FromFloat(0.005f);

            var ground = ecsWorld.CreateEntity();
            ecsWorld.AddComponent(ground, TransformData.Identity.WithPosition(Vector3.Zero));
            ecsWorld.AddComponent(ground, ColliderComponent.Box(new Vector3(FP.FromInt(10), FP.FromFloat(0.5f), FP.FromInt(10))));
            ecsWorld.AddComponent(ground, PhysicsStateComponent.Static());
            ecsWorld.AddComponent(ground, new RigidBodyComponent());

            Entity bottom = CreatePhysicsEntity(
                ecsWorld,
                new Vector3(FP.Zero, FP.FromInt(2), FP.Zero),
                new Vector3(FP.Half, FP.Half, FP.Half));
            Entity top = CreatePhysicsEntity(
                ecsWorld,
                new Vector3(FP.Zero, FP.FromInt(4), FP.Zero),
                new Vector3(FP.Half, FP.Half, FP.Half));

            var state = new SystemState { World = ecsWorld };
            physicsSystem.OnCreate(ref state);
            for (int i = 0; i < 300; i++)
            {
                physicsSystem.OnUpdate(ref state);
            }

            TransformData bottomXform = ecsWorld.GetComponent<TransformData>(bottom);
            TransformData topXform = ecsWorld.GetComponent<TransformData>(top);
            TestRunner.Assert(topXform.Position.Y > bottomXform.Position.Y,
                $"top should be above bottom, got top={topXform.Position.Y}, bottom={bottomXform.Position.Y}");
            TestRunner.Assert(bottomXform.Position.Y > FP.FromFloat(0.5f),
                $"bottom should not fall through ground, got y={bottomXform.Position.Y}");
        }

        [Test]
        public static void StaticEntityDoesNotMove()
        {
            var ecsWorld = new ECS.World();
            var physicsSystem = new PhysicsSystem();
            physicsSystem.FixedDt = FP.FromFloat(0.005f);

            Entity staticEntity = ecsWorld.CreateEntity();
            ecsWorld.AddComponent(staticEntity, TransformData.Identity.WithPosition(new Vector3(FP.Zero, FP.FromInt(5), FP.Zero)));
            ecsWorld.AddComponent(staticEntity, ColliderComponent.Box(new Vector3(FP.Half, FP.Half, FP.Half)));
            ecsWorld.AddComponent(staticEntity, PhysicsStateComponent.Static());
            ecsWorld.AddComponent(staticEntity, new RigidBodyComponent());

            var state = new SystemState { World = ecsWorld };
            physicsSystem.OnCreate(ref state);
            for (int i = 0; i < 50; i++)
            {
                physicsSystem.OnUpdate(ref state);
            }

            TransformData xform = ecsWorld.GetComponent<TransformData>(staticEntity);
            TestRunner.AssertEqual(FP.FromInt(5), xform.Position.Y,
                $"static entity should not move, got y={xform.Position.Y}");
        }
    }

    internal static class PhysicsVelocityDefaults
    {
        public static PhysicsVelocityComponent Default => new PhysicsVelocityComponent
        {
            LinearVelocity = Vector3.Zero,
            AngularVelocity = Vector3.Zero,
        };
    }
}