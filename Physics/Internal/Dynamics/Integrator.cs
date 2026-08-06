using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.Dynamics
{
    internal static class Integrator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IntegrateLinear(ref Body body, FP dt)
        {
            if (body.InverseMass == FP.Zero) return;
            Vector3 linearAccel = body.Force * body.InverseMass;
            body.LinearVelocity = body.LinearVelocity + linearAccel * dt;
            body.Position = body.Position + body.LinearVelocity * dt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IntegrateAngular(ref Body body, FP dt)
        {
            if (body.InverseInertia == FP.Zero) return;
            Vector3 angularAccel = body.Torque * body.InverseInertia;
            body.AngularVelocity = body.AngularVelocity + angularAccel * dt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IntegrateOrientation(ref Body body, FP dt)
        {
            if (body.InverseInertia == FP.Zero) return;
            FP wx = body.AngularVelocity.X * FP.Half * dt;
            FP wy = body.AngularVelocity.Y * FP.Half * dt;
            FP wz = body.AngularVelocity.Z * FP.Half * dt;

            FP qx = body.Orientation.X;
            FP qy = body.Orientation.Y;
            FP qz = body.Orientation.Z;
            FP qw = body.Orientation.W;

            FP dqx =  wx * qw + wy * qz - wz * qy;
            FP dqy = -wx * qz + wy * qw + wz * qx;
            FP dqz =  wx * qy - wy * qx + wz * qw;
            FP dqw = -wx * qx - wy * qy - wz * qz;

            body.Orientation = new Quaternion(qx + dqx, qy + dqy, qz + dqz, qw + dqw).Normalized;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Integrate(ref Body body, FP dt)
        {
            IntegrateLinear(ref body, dt);
            IntegrateAngular(ref body, dt);
            IntegrateOrientation(ref body, dt);
            body.Force = Vector3.Zero;
            body.Torque = Vector3.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ApplyGravity(ref Body body, Vector3 gravity, FP dt)
        {
            if (body.InverseMass == FP.Zero) return;
            Vector3 impulse = gravity * dt;
            body.LinearVelocity = body.LinearVelocity + impulse;
        }
    }
}