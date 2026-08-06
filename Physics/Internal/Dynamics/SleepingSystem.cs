using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.Dynamics
{
    internal static class SleepingSystem
    {
        public const int DefaultFramesToSleep = 30;
        public static readonly FP DefaultLinearThreshold = FP.FromFloat(0.01f);
        public static readonly FP DefaultAngularThreshold = FP.FromFloat(0.01f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(
            ref Body body,
            FP linearThreshold,
            FP angularThreshold,
            int framesToSleep)
        {
            if ((body.Flags & BodyFlags.Static) != 0) return;

            if ((body.Flags & BodyFlags.Sleeping) != 0)
            {
                return;
            }

            if ((body.Flags & BodyFlags.Awake) == 0)
            {
                body.Flags |= BodyFlags.Awake;
            }

            FP linSpeedSqr = body.LinearVelocity.SqrMagnitude;
            FP angSpeedSqr = body.AngularVelocity.SqrMagnitude;
            FP linearLimit = linearThreshold * linearThreshold;
            FP angularLimit = angularThreshold * angularThreshold;

            if (linSpeedSqr < linearLimit && angSpeedSqr < angularLimit)
            {
                body.SleepTimer++;
                if (body.SleepTimer >= framesToSleep)
                {
                    body.Flags |= BodyFlags.Sleeping;
                    body.Flags &= ~BodyFlags.Awake;
                    body.LinearVelocity = Vector3.Zero;
                    body.AngularVelocity = Vector3.Zero;
                }
            }
            else
            {
                body.SleepTimer = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WakeUp(ref Body body)
        {
            if ((body.Flags & BodyFlags.Sleeping) != 0)
            {
                body.Flags &= ~BodyFlags.Sleeping;
                body.Flags |= BodyFlags.Awake;
                body.SleepTimer = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateAll(
            BodySet bodies,
            FP linearThreshold,
            FP angularThreshold,
            int framesToSleep)
        {
            for (int i = 0; i < bodies.Capacity; i++)
            {
                if (!bodies.IsValid(bodies.GetHandle(i))) continue;
                Body b = bodies.Get(bodies.GetHandle(i));
                Update(ref b, linearThreshold, angularThreshold, framesToSleep);
                bodies.Set(bodies.GetHandle(i), b);
            }
        }
    }
}