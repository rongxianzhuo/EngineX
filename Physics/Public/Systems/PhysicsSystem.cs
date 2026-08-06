using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.ECS;
using EngineX.ECS.Components;
using EngineX.Physics.Components;

namespace EngineX.Physics
{
    public sealed class PhysicsSystem : ISystem
    {
        public PhysicsWorld World;
        public FP FixedDt = FP.FromFloat(0.01f);
        public FP Gravity = new Vector3(FP.Zero, -FP.FromInt(10), FP.Zero).Y;

        public PhysicsSystem()
        {
        }

        public PhysicsSystem(PhysicsWorld world)
        {
            World = world;
        }

        public void OnCreate(ref SystemState state)
        {
            if (World == null)
            {
                World = new PhysicsWorld(256, new Vector3(FP.Zero, Gravity, FP.Zero));
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            if (World == null) return;
            SyncPreState(state.World);
            SyncPreVelocity(state.World);
            World.Step(FixedDt);
            SyncPost(state.World);
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        private struct PreSyncVisitor : IForEach<RigidBodyComponent, TransformData, ColliderComponent, PhysicsStateComponent>
        {
            public PhysicsWorld World;

            public void Execute(
                ref RigidBodyComponent rb,
                ref TransformData xform,
                ref ColliderComponent col,
                ref PhysicsStateComponent phys)
            {
                BodyHandle handle = rb.Handle;
                Body body;
                if (!handle.IsValid || !World.TryGetBody(handle, out body))
                {
                    body = new Body
                    {
                        Position = xform.Position,
                        Orientation = xform.Rotation,
                        InverseMass = phys.InverseMass,
                        InverseInertia = phys.InverseInertia,
                        Shape = col.Shape,
                        Flags = phys.Flags | BodyFlags.Awake,
                    };
                    handle = World.AddBody(body);
                    rb.Handle = handle;
                }
                else
                {
                    body.Position = xform.Position;
                    body.Orientation = xform.Rotation;
                    body.InverseMass = phys.InverseMass;
                    body.InverseInertia = phys.InverseInertia;
                    body.Shape = col.Shape;
                    body.Flags = phys.Flags | BodyFlags.Awake;
                    World.SetBody(handle, body);
                }
            }
        }

        private struct PreVelocityVisitor : IForEach<RigidBodyComponent, PhysicsVelocityComponent>
        {
            public PhysicsWorld World;

            public void Execute(ref RigidBodyComponent rb, ref PhysicsVelocityComponent vel)
            {
                if (!rb.Handle.IsValid) return;
                if (!World.TryGetBody(rb.Handle, out var body)) return;
                body.LinearVelocity = vel.LinearVelocity;
                body.AngularVelocity = vel.AngularVelocity;
                World.SetBody(rb.Handle, body);
            }
        }

        private struct PostSyncVisitor : IForEach<RigidBodyComponent, TransformData, PhysicsVelocityComponent>
        {
            public PhysicsWorld World;

            public void Execute(
                ref RigidBodyComponent rb,
                ref TransformData xform,
                ref PhysicsVelocityComponent vel)
            {
                if (!rb.Handle.IsValid) return;
                if (!World.TryGetBody(rb.Handle, out var body)) return;
                xform.Position = body.Position;
                xform.Rotation = body.Orientation;
                vel.LinearVelocity = body.LinearVelocity;
                vel.AngularVelocity = body.AngularVelocity;
            }
        }

        private void SyncPreState(World ecsWorld)
        {
            var query = ecsWorld.Query()
                .WithAll<RigidBodyComponent>()
                .WithAll<TransformData>()
                .WithAll<ColliderComponent>()
                .WithAll<PhysicsStateComponent>()
                .Build();
            var visitor = new PreSyncVisitor { World = World };
            query.ForEach<PreSyncVisitor, RigidBodyComponent, TransformData, ColliderComponent, PhysicsStateComponent>(ref visitor);
        }

        private void SyncPreVelocity(World ecsWorld)
        {
            var query = ecsWorld.Query()
                .WithAll<RigidBodyComponent>()
                .WithAll<PhysicsVelocityComponent>()
                .Build();
            var visitor = new PreVelocityVisitor { World = World };
            query.ForEach<PreVelocityVisitor, RigidBodyComponent, PhysicsVelocityComponent>(ref visitor);
        }

        private void SyncPost(World ecsWorld)
        {
            var query = ecsWorld.Query()
                .WithAll<RigidBodyComponent>()
                .WithAll<TransformData>()
                .WithAll<PhysicsVelocityComponent>()
                .Build();
            var visitor = new PostSyncVisitor { World = World };
            query.ForEach<PostSyncVisitor, RigidBodyComponent, TransformData, PhysicsVelocityComponent>(ref visitor);
        }
    }
}