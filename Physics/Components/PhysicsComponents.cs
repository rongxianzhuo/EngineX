using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.ECS;

namespace EngineX.Physics.Components
{
    public struct RigidBodyComponent : IComponentData
    {
        public BodyHandle Handle;
    }

    public struct ColliderComponent : IComponentData
    {
        public BodyShape Shape;

        public static ColliderComponent Box(Vector3 halfExtents) => new ColliderComponent
        {
            Shape = BodyShape.Box(halfExtents),
        };

        public static ColliderComponent Sphere(FP radius) => new ColliderComponent
        {
            Shape = BodyShape.Sphere(radius),
        };
    }

    public struct PhysicsStateComponent : IComponentData
    {
        public FP Mass;
        public FP InverseMass;
        public FP InverseInertia;
        public BodyFlags Flags;

        public static PhysicsStateComponent Dynamic(FP mass, FP inverseInertia) => new PhysicsStateComponent
        {
            Mass = mass,
            InverseMass = mass > FP.Zero ? FP.One / mass : FP.Zero,
            InverseInertia = inverseInertia,
            Flags = BodyFlags.Awake,
        };

        public static PhysicsStateComponent Static() => new PhysicsStateComponent
        {
            Mass = FP.Zero,
            InverseMass = FP.Zero,
            InverseInertia = FP.Zero,
            Flags = BodyFlags.Static,
        };
    }

    public struct PhysicsVelocityComponent : IComponentData
    {
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
    }
}