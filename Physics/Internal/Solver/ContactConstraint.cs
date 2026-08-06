using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.Solver
{
    internal struct ContactConstraint
    {
        public BodyHandle BodyA;
        public BodyHandle BodyB;

        public Vector3 Normal;
        public FP Penetration;

        public Vector3 Point;

        public FP Restitution;
        public FP Friction;

        public FP NormalImpulse;
        public FP TangentImpulse1;
        public FP TangentImpulse2;

        public FP EffectiveMassNormal;
        public FP EffectiveMassTangent1;
        public FP EffectiveMassTangent2;

        public Vector3 Tangent1;
        public Vector3 Tangent2;

        public FP Bias;

        public Vector3 RA;
        public Vector3 RB;

        public static ContactConstraint Default => new ContactConstraint
        {
            Normal = Vector3.Up,
            Tangent1 = Vector3.Right,
            Tangent2 = Vector3.Forward,
        };
    }
}