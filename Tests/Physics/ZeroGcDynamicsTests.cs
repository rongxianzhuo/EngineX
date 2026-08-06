using System;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;
using EngineX.Physics.Internal.Dynamics;

namespace EngineX.Physics.Tests
{
    public static class ZeroGcDynamicsTests
    {
        private const int Iterations = 100;
        private const long AllocationBudget = 64;

        [Test]
        public static void IntegratorLinearIsZeroGc()
        {
            var body = Body.Defaults;
            body.InverseMass = FP.One;
            body.LinearVelocity = new Vector3(FP.FromInt(2), FP.Zero, FP.Zero);

            for (int i = 0; i < 3; i++)
                Integrator.IntegrateLinear(ref body, FP.FromFloat(0.1f));

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++)
                Integrator.IntegrateLinear(ref body, FP.FromFloat(0.1f));
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget,
                $"IntegrateLinear allocated {allocated} bytes");
        }

        [Test]
        public static void IntegratorOrientationIsZeroGc()
        {
            var body = Body.Defaults;
            body.InverseInertia = FP.One;
            body.AngularVelocity = new Vector3(FP.One, FP.Zero, FP.Zero);

            for (int i = 0; i < 3; i++)
                Integrator.IntegrateOrientation(ref body, FP.FromFloat(0.01f));

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++)
                Integrator.IntegrateOrientation(ref body, FP.FromFloat(0.01f));
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget,
                $"IntegrateOrientation allocated {allocated} bytes");
        }

        [Test]
        public static void SleepingUpdateIsZeroGc()
        {
            var body = Body.Defaults;
            body.InverseMass = FP.One;

            for (int i = 0; i < 3; i++)
                SleepingSystem.Update(ref body, FP.FromFloat(0.01f), FP.FromFloat(0.01f), 30);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++)
                SleepingSystem.Update(ref body, FP.FromFloat(0.01f), FP.FromFloat(0.01f), 30);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget,
                $"SleepingSystem.Update allocated {allocated} bytes");
        }

        [Test]
        public static void BodySetAddRemoveIsZeroGc()
        {
            var bodies = new BodySet(1024);
            for (int i = 0; i < 100; i++) bodies.Add(Body.Defaults);
            for (int i = 0; i < 50; i++) bodies.Remove(bodies.GetHandle(i));

            var probes = new int[10];
            for (int i = 0; i < 10; i++) probes[i] = bodies.Add(Body.Defaults).Id;

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++)
            {
                bodies.Add(Body.Defaults);
            }
            for (int i = 0; i < 50; i++)
            {
                bodies.Remove(bodies.GetHandle(i));
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget,
                $"BodySet Add/Remove allocated {allocated} bytes");
        }

        [Test]
        public static void MixedDynamicsIsZeroGc()
        {
            var body = Body.Defaults;
            body.InverseMass = FP.One;
            body.InverseInertia = FP.One;

            for (int i = 0; i < 3; i++)
            {
                Integrator.Integrate(ref body, FP.FromFloat(0.01f));
                SleepingSystem.Update(ref body, FP.FromFloat(0.01f), FP.FromFloat(0.01f), 30);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++)
            {
                Integrator.Integrate(ref body, FP.FromFloat(0.01f));
                SleepingSystem.Update(ref body, FP.FromFloat(0.01f), FP.FromFloat(0.01f), 30);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < AllocationBudget,
                $"Mixed dynamics allocated {allocated} bytes");
        }
    }
}