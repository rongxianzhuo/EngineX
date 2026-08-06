using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.Dynamics
{
    internal struct Body
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
            Flags = BodyFlags.Awake,
            Version = 1,
            SleepTimer = 0,
        };
    }
}