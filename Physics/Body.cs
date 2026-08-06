using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics
{
    public struct Body
    {
        public Vector3 Position;
        public Quaternion Orientation;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;

        public FP InverseMass;
        public FP InverseInertia;

        public Vector3 Force;
        public Vector3 Torque;

        public Aabb Bounds;

        public BodyShape Shape;

        public BodyFlags Flags;
        public int Version;
        public int SleepTimer;

        public static Body Defaults => new Body
        {
            Position = Vector3.Zero,
            Orientation = Quaternion.Identity,
            LinearVelocity = Vector3.Zero,
            AngularVelocity = Vector3.Zero,
            InverseMass = FP.One,
            InverseInertia = FP.One,
            Force = Vector3.Zero,
            Torque = Vector3.Zero,
            Bounds = Aabb.Empty,
            Shape = BodyShape.Default,
            Flags = BodyFlags.Awake,
            Version = 1,
            SleepTimer = 0,
        };
    }

    public enum ShapeType : byte
    {
        Sphere = 0,
        Box = 1,
        Capsule = 2,
        ConvexHull = 3,
    }

    public struct BodyShape
    {
        public ShapeType Type;
        public FP Radius;
        public Vector3 HalfExtents;

        public static BodyShape Default => new BodyShape
        {
            Type = ShapeType.Box,
            HalfExtents = new Vector3(FP.Half, FP.Half, FP.Half),
        };

        public static BodyShape Sphere(FP radius) => new BodyShape
        {
            Type = ShapeType.Sphere,
            Radius = radius,
        };

        public static BodyShape Box(Vector3 halfExtents) => new BodyShape
        {
            Type = ShapeType.Box,
            HalfExtents = halfExtents,
        };
    }
}