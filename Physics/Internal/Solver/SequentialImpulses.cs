using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics.Internal.Dynamics;

namespace EngineX.Physics.Internal.Solver
{
    internal static class SequentialImpulses
    {
        public const int DefaultVelocityIterations = 8;
        public const int DefaultPositionIterations = 1;
        public static readonly FP Slop = FP.FromFloat(0.005f);
        public static readonly FP BaumgarteBeta = FP.FromFloat(0.2f);
        public static readonly FP RestitutionThreshold = FP.FromInt(1);

        public static void PreStep(
            BodySet bodies,
            ContactConstraint[] contacts,
            int count,
            FP dt)
        {
            FP invDt = FP.One / dt;
            for (int c = 0; c < count; c++)
            {
                if (!bodies.TryGet(contacts[c].BodyA, out var bodyA)) continue;
                if (!bodies.TryGet(contacts[c].BodyB, out var bodyB)) continue;

                ContactConstraint con = contacts[c];

                con.RA = con.Point - bodyA.Position;
                con.RB = con.Point - bodyB.Position;

                BuildTangents(ref con);

                Vector3 n = con.Normal;
                Vector3 t1 = con.Tangent1;
                Vector3 t2 = con.Tangent2;

                Vector3 rAxN = Vector3.Cross(con.RA, n);
                Vector3 rBxN = Vector3.Cross(con.RB, n);
                Vector3 rAxT1 = Vector3.Cross(con.RA, t1);
                Vector3 rBxT1 = Vector3.Cross(con.RB, t1);
                Vector3 rAxT2 = Vector3.Cross(con.RA, t2);
                Vector3 rBxT2 = Vector3.Cross(con.RB, t2);

                FP invMassSum = bodyA.InverseMass + bodyB.InverseMass;
                FP kNormal = invMassSum
                    + bodyA.InverseInertia * rAxN.SqrMagnitude
                    + bodyB.InverseInertia * rBxN.SqrMagnitude;
                FP kTangent1 = invMassSum
                    + bodyA.InverseInertia * rAxT1.SqrMagnitude
                    + bodyB.InverseInertia * rBxT1.SqrMagnitude;
                FP kTangent2 = invMassSum
                    + bodyA.InverseInertia * rAxT2.SqrMagnitude
                    + bodyB.InverseInertia * rBxT2.SqrMagnitude;

                con.EffectiveMassNormal = kNormal > FP.Zero ? FP.One / kNormal : FP.Zero;
                con.EffectiveMassTangent1 = kTangent1 > FP.Zero ? FP.One / kTangent1 : FP.Zero;
                con.EffectiveMassTangent2 = kTangent2 > FP.Zero ? FP.One / kTangent2 : FP.Zero;

                Vector3 vRel = RelativeVelocity(bodyA, bodyB, con.RA, con.RB);
                FP vRelNormal = Vector3.Dot(vRel, n);

                if (vRelNormal < -RestitutionThreshold)
                {
                    con.Bias = -con.Restitution * vRelNormal;
                }
                else
                {
                    con.Bias = FP.Zero;
                }

                con.NormalImpulse = FP.Zero;
                con.TangentImpulse1 = FP.Zero;
                con.TangentImpulse2 = FP.Zero;

                contacts[c] = con;
            }
        }

        public static void SolveVelocity(
            BodySet bodies,
            ContactConstraint[] contacts,
            int count)
        {
            for (int c = 0; c < count; c++)
            {
                SolveContactVelocity(bodies, ref contacts[c]);
            }
        }

        internal static void SolveContactVelocity(BodySet bodies, ref ContactConstraint con)
        {
            if (!bodies.TryGet(con.BodyA, out var bodyA)) return;
            if (!bodies.TryGet(con.BodyB, out var bodyB)) return;

            Vector3 vRel = RelativeVelocity(bodyA, bodyB, con.RA, con.RB);
            Vector3 n = con.Normal;
            Vector3 t1 = con.Tangent1;
            Vector3 t2 = con.Tangent2;

            FP vRelN = Vector3.Dot(vRel, n);

            FP deltaLambda = -(vRelN + con.Bias) * con.EffectiveMassNormal;
            FP newNormalImpulse = FP.Max(FP.Zero, con.NormalImpulse + deltaLambda);
            deltaLambda = newNormalImpulse - con.NormalImpulse;
            con.NormalImpulse = newNormalImpulse;

            ApplyImpulse(bodies, con.BodyA, con.BodyB,
                n * deltaLambda,
                Vector3.Cross(con.RA, n) * deltaLambda * bodyA.InverseInertia,
                Vector3.Cross(con.RB, n) * deltaLambda * bodyB.InverseInertia);

            if (!bodies.TryGet(con.BodyA, out bodyA)) return;
            if (!bodies.TryGet(con.BodyB, out bodyB)) return;

            vRel = RelativeVelocity(bodyA, bodyB, con.RA, con.RB);

            FP vRelT1 = Vector3.Dot(vRel, t1);
            FP deltaLambdaT1 = -vRelT1 * con.EffectiveMassTangent1;
            FP maxTangent = con.Friction * con.NormalImpulse;
            FP newTangent1 = Clamp(con.TangentImpulse1 + deltaLambdaT1, -maxTangent, maxTangent);
            deltaLambdaT1 = newTangent1 - con.TangentImpulse1;
            con.TangentImpulse1 = newTangent1;

            ApplyImpulse(bodies, con.BodyA, con.BodyB,
                t1 * deltaLambdaT1,
                Vector3.Cross(con.RA, t1) * deltaLambdaT1 * bodyA.InverseInertia,
                Vector3.Cross(con.RB, t1) * deltaLambdaT1 * bodyB.InverseInertia);

            if (!bodies.TryGet(con.BodyA, out bodyA)) return;
            if (!bodies.TryGet(con.BodyB, out bodyB)) return;

            vRel = RelativeVelocity(bodyA, bodyB, con.RA, con.RB);

            FP vRelT2 = Vector3.Dot(vRel, t2);
            FP deltaLambdaT2 = -vRelT2 * con.EffectiveMassTangent2;
            FP newTangent2 = Clamp(con.TangentImpulse2 + deltaLambdaT2, -maxTangent, maxTangent);
            deltaLambdaT2 = newTangent2 - con.TangentImpulse2;
            con.TangentImpulse2 = newTangent2;

            ApplyImpulse(bodies, con.BodyA, con.BodyB,
                t2 * deltaLambdaT2,
                Vector3.Cross(con.RA, t2) * deltaLambdaT2 * bodyA.InverseInertia,
                Vector3.Cross(con.RB, t2) * deltaLambdaT2 * bodyB.InverseInertia);
        }

        public static void SolvePosition(
            BodySet bodies,
            ContactConstraint[] contacts,
            int count)
        {
            for (int c = 0; c < count; c++)
            {
                SolveContactPosition(bodies, ref contacts[c]);
            }
        }

        internal static void SolveContactPosition(BodySet bodies, ref ContactConstraint con)
        {
            if (!bodies.TryGet(con.BodyA, out var bodyA)) return;
            if (!bodies.TryGet(con.BodyB, out var bodyB)) return;

            FP penetration = con.Penetration - Slop;
            if (penetration <= FP.Zero) return;

            FP correction = -penetration * con.EffectiveMassNormal;
            Vector3 impulse = con.Normal * correction;

            bodyA.Position = bodyA.Position + impulse * bodyA.InverseMass;
            bodyB.Position = bodyB.Position - impulse * bodyB.InverseMass;

            bodies.Set(con.BodyA, bodyA);
            bodies.Set(con.BodyB, bodyB);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 RelativeVelocity(in Body bodyA, in Body bodyB, Vector3 rA, Vector3 rB)
        {
            Vector3 vA = bodyA.LinearVelocity + Vector3.Cross(bodyA.AngularVelocity, rA);
            Vector3 vB = bodyB.LinearVelocity + Vector3.Cross(bodyB.AngularVelocity, rB);
            return vB - vA;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ApplyImpulse(
            BodySet bodies, BodyHandle hA, BodyHandle hB,
            Vector3 linImpulse, Vector3 angImpulseA, Vector3 angImpulseB)
        {
            if (!bodies.TryGet(hA, out var bodyA)) return;
            if (!bodies.TryGet(hB, out var bodyB)) return;

            bodyA.LinearVelocity = bodyA.LinearVelocity - linImpulse * bodyA.InverseMass;
            bodyA.AngularVelocity = bodyA.AngularVelocity - angImpulseA;
            bodyB.LinearVelocity = bodyB.LinearVelocity + linImpulse * bodyB.InverseMass;
            bodyB.AngularVelocity = bodyB.AngularVelocity + angImpulseB;

            bodies.Set(hA, bodyA);
            bodies.Set(hB, bodyB);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static FP Clamp(FP v, FP min, FP max)
            => v < min ? min : (v > max ? max : v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BuildTangents(ref ContactConstraint con)
        {
            Vector3 n = con.Normal;
            FP absNx = n.X.Abs();
            Vector3 helper = absNx >= FP.FromFloat(0.57735f)
                ? new Vector3(FP.Zero, FP.One, FP.Zero)
                : new Vector3(FP.One, FP.Zero, FP.Zero);
            con.Tangent1 = Vector3.Cross(helper, n).Normalized;
            con.Tangent2 = Vector3.Cross(n, con.Tangent1);
        }
    }
}