using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using EngineX.Jobs;

namespace EngineX.Jobs.Tests
{
    public static class JobSystemTests
    {
        [Test]
        public static void WorkerThreadAndMaxThreadCount()
        {
            TestRunner.AssertEqual(Environment.ProcessorCount, JobSystem.MaxJobThreadCount, "MaxJobThreadCount == ProcessorCount");
            TestRunner.AssertEqual(Math.Max(1, Environment.ProcessorCount - 1), JobSystem.WorkerThreadCount, "WorkerThreadCount == CPU-1");
        }

        [Test]
        public static void BasicSchedule()
        {
            var ran = 0;
            var h = JobSystem.Schedule(new ActionJob(() => Interlocked.Increment(ref ran)));
            TestRunner.Assert(!h.IsCompleted || ran > 0, "handle created");
            h.Complete();
            TestRunner.AssertEqual(1, ran, "job executed exactly once");
            TestRunner.Assert(h.IsCompleted, "IsCompleted == true after Complete");
            h.Complete();
            TestRunner.Assert(true, "double Complete is safe");
        }

        [Test]
        public static void DependencyChain()
        {
            var order = new ConcurrentQueue<int>();
            var a = JobSystem.Schedule(new ActionJob(() => { order.Enqueue(1); }));
            var b = JobSystem.Schedule(new ActionJob(() => { order.Enqueue(2); }), a);
            var c = JobSystem.Schedule(new ActionJob(() => { order.Enqueue(3); }), b);
            c.Complete();
            TestRunner.Assert(order.SequenceEqual(new[] { 1, 2, 3 }), "execution order 1,2,3");
        }

        [Test]
        public static void ParallelForCorrectness()
        {
            const int n = 100_000;
            var seen = new int[n];
            var h = JobSystem.ScheduleParallel(new IndexJob(seen), n, 64);
            h.Complete();
            bool allOnce = true;
            long sum = 0;
            for (int i = 0; i < n; i++)
            {
                if (seen[i] != 1) allOnce = false;
                sum += seen[i];
            }
            TestRunner.Assert(allOnce && sum == n, "every index executed exactly once");
        }

        [Test]
        public static void ParallelForBatchCorrectness()
        {
            const int n = 100_000;
            var seen = new int[n];
            using (var arr = new NativeArray<int>(n, Allocator.TempJob))
            {
                var h = JobSystem.ScheduleParallel(new BatchJob(seen), arr, 64);
                h.Complete();
                bool allOnce = true;
                long sum = 0;
                for (int i = 0; i < n; i++)
                {
                    if (seen[i] != 1) allOnce = false;
                    sum += seen[i];
                }
                TestRunner.Assert(allOnce && sum == n, "every element covered exactly once");
            }
        }

        [Test]
        public static void BatchSizeVariants()
        {
            const int n = 10_000;

            var seen1 = new int[n];
            JobSystem.ScheduleParallel(new IndexJob(seen1), n, 1).Complete();
            TestRunner.Assert(seen1.All(v => v == 1), "innerLoopBatchCount=1 covers every index exactly once");

            var seen2 = new int[n];
            JobSystem.ScheduleParallel(new IndexJob(seen2), n, n).Complete();
            TestRunner.Assert(seen2.All(v => v == 1), "huge batch covers every index exactly once");

            var seen3 = new int[n];
            using (var arr = new NativeArray<int>(n, Allocator.TempJob))
            {
                JobSystem.ScheduleParallel(new BatchJob(seen3), arr, 1).Complete();
            }
            TestRunner.Assert(seen3.All(v => v == 1), "batch job with batch=1 covers every element exactly once");

            var empty = JobSystem.ScheduleParallel(new IndexJob(new int[0]), 0, 1);
            TestRunner.Assert(empty.IsCompleted, "zero-length parallel-for returns completed handle");
            empty.Complete();
            TestRunner.Assert(true, "zero-length Complete is safe");
        }

        [Test]
        public static void ParallelForDependency()
        {
            var order = new ConcurrentQueue<int>();
            var dep = JobSystem.Schedule(new ActionJob(() => { Thread.Sleep(50); order.Enqueue(1); }));
            var seen = new int[1000];
            var pf = JobSystem.ScheduleParallel(new IndexJob(seen), 1000, 16, dep);
            pf.Complete();
            var seq = order.ToArray();
            TestRunner.Assert(seq.Length > 0 && seq[0] == 1 && seen.All(v => v == 1), "parallel-for ran after its dependency");
        }

        [Test]
        public static void CombineDependencies()
        {
            var order = new ConcurrentQueue<int>();
            var a = JobSystem.Schedule(new ActionJob(() => { Thread.Sleep(30); order.Enqueue(1); }));
            var b = JobSystem.Schedule(new ActionJob(() => { Thread.Sleep(30); order.Enqueue(2); }));
            var dep = JobHandle.CombineDependencies(a, b);
            var c = JobSystem.Schedule(new ActionJob(() => order.Enqueue(3)), dep);
            c.Complete();
            var seq = order.ToArray();
            TestRunner.Assert(seq.Length == 3 && seq[2] == 3, "C ran after both A and B");

            var d = JobSystem.Schedule(new ActionJob(() => order.Enqueue(4)));
            var dep2 = JobHandle.CombineDependencies(new JobHandle[] { default, d, default });
            var e = JobSystem.Schedule(new ActionJob(() => order.Enqueue(5)), dep2);
            e.Complete();
            TestRunner.Assert(order.ToArray().Last() == 5, "CombineDependencies(array) with default handles works");
            TestRunner.Assert(JobHandle.CombineDependencies(default, default).Info == null, "CombineDependencies(default, default) is default");

            int multiRan = 0;
            var m1 = JobSystem.Schedule(new ActionJob(() => { Thread.Sleep(30); Interlocked.Increment(ref multiRan); }));
            var m2 = JobSystem.Schedule(new ActionJob(() => { Thread.Sleep(30); Interlocked.Increment(ref multiRan); }));
            var m3 = JobSystem.Schedule(new ActionJob(() => { Thread.Sleep(30); Interlocked.Increment(ref multiRan); }));
            var m4 = JobSystem.Schedule(new ActionJob(() => { Thread.Sleep(30); Interlocked.Increment(ref multiRan); }));
            var m5 = JobSystem.Schedule(new ActionJob(() => { Thread.Sleep(30); Interlocked.Increment(ref multiRan); }));
            var multi = JobHandle.CombineDependencies(new[] { m1, m2, m3, m4, m5 });
            var after = JobSystem.Schedule(new ActionJob(() => order.Enqueue(99)), multi);
            after.Complete();
            TestRunner.AssertEqual(5, multiRan, "CombineDependencies(5 handles) waits for ALL of them");
            TestRunner.Assert(after.IsCompleted && order.ToArray().Last() == 99, "dependent of 5-handle combine ran");
        }

        [Test]
        public static void ExceptionPropagation()
        {
            var h = JobSystem.Schedule(new ActionJob(() => throw new InvalidOperationException("boom")));
            bool caught = false;
            try { h.Complete(); }
            catch (InvalidOperationException ex) { caught = ex.Message == "boom"; }
            TestRunner.Assert(caught, "Complete() rethrows job exception");

            int bRan = 0;
            var a = JobSystem.Schedule(new ActionJob(() => throw new InvalidOperationException("dep-boom")));
            var b = JobSystem.Schedule(new ActionJob(() => Interlocked.Increment(ref bRan)), a);
            bool caught2 = false;
            try { b.Complete(); }
            catch (InvalidOperationException ex) { caught2 = ex.Message == "dep-boom"; }
            TestRunner.Assert(caught2, "exception from dependency propagates through chain");
            TestRunner.AssertEqual(0, bRan, "dependent job body was skipped after dependency failure");

            int tRan = 0;
            var a3 = JobSystem.Schedule(new ActionJob(() => throw new InvalidOperationException("dep-boom")));
            var b3 = JobSystem.Schedule(new ActionJob(() => Interlocked.Increment(ref tRan)), a3);
            var c3 = JobSystem.Schedule(new ActionJob(() => Interlocked.Increment(ref tRan)), b3);
            bool caught3 = false;
            try { c3.Complete(); }
            catch (InvalidOperationException ex) { caught3 = ex.Message == "dep-boom"; }
            TestRunner.Assert(caught3 && tRan == 0, "error propagates transitively through skipped chain");

            var a2 = JobSystem.Schedule(new ActionJob(() => throw new InvalidOperationException("late-boom")));
            int dRan = 0;
            var d = JobSystem.Schedule(new ActionJob(() => Interlocked.Increment(ref dRan)), a2);
            bool caught4 = false;
            try { d.Complete(); }
            catch (InvalidOperationException ex) { caught4 = ex.Message == "late-boom"; }
            TestRunner.Assert(caught4 && dRan == 0, "schedule on failed-but-alive handle -> skipped + throws");

            try { a2.Complete(); } catch (InvalidOperationException) { }
            int eRan = 0;
            var e = JobSystem.Schedule(new ActionJob(() => Interlocked.Increment(ref eRan)), a2);
            e.Complete();
            TestRunner.AssertEqual(1, eRan, "schedule on completed (recycled) handle runs normally");

            int pfRan = 0;
            var pf = JobSystem.ScheduleParallel(new ThrowAtJob(50, () => Interlocked.Increment(ref pfRan)), 1000, 16);

            int pfDepRan = 0;
            var pfDep = JobSystem.Schedule(new ActionJob(() => Interlocked.Increment(ref pfDepRan)), pf);
            bool caught6 = false;
            try { pfDep.Complete(); }
            catch (InvalidOperationException) { caught6 = true; }
            TestRunner.Assert(caught6 && pfDepRan == 0, "dependent of failed parallel-for is skipped");

            bool caught5 = false;
            try { pf.Complete(); }
            catch (InvalidOperationException) { caught5 = true; }
            TestRunner.Assert(caught5, "ScheduleParallel combined handle throws when a batch fails");
        }

        [Test]
        public static void LegacyInterfacePath()
        {
            int ran = 0;
            var h = JobSystem.Schedule((IJob)new ActionJob(() => Interlocked.Increment(ref ran)));
            h.Complete();
            TestRunner.AssertEqual(1, ran, "Schedule(IJob) works");

            var seen = new int[1000];
            var pf = JobSystem.ScheduleParallel((IJobParallelFor)new IndexJob(seen), 1000, 32);
            pf.Complete();
            TestRunner.Assert(seen.All(v => v == 1), "ScheduleParallel(IJobParallelFor) works");

            using (var arr = new NativeArray<int>(100, Allocator.TempJob))
            {
                var batched = JobSystem.ScheduleParallel((IJobParallelForBatch)new BatchJob(new int[100]), arr, 8);
                batched.Complete();
            }
            TestRunner.Assert(true, "ScheduleParallel(IJobParallelForBatch, NativeArray) works");
        }

        [Test]
        public static void ZeroGcSchedule()
        {
            for (int i = 0; i < 1000; i++) JobSystem.Schedule(new ActionJob(() => { })).Complete();

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 20000; i++) JobSystem.Schedule(new ActionJob(() => { })).Complete();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < 4096, $"Schedule steady-state: {allocated} bytes / 20000 jobs (< 4KB)");
        }

        [Test]
        public static void ZeroGcParallelFor()
        {
            const int n = 1024;
            var scratch = new int[n];
            for (int i = 0; i < 1000; i++) JobSystem.ScheduleParallel(new IndexJob(scratch), n, 16).Complete();

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 5000; i++) JobSystem.ScheduleParallel(new IndexJob(scratch), n, 16).Complete();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < 4096, $"ScheduleParallel steady-state: {allocated} bytes / 5000 jobs (< 4KB)");
        }

        [Test]
        public static void ZeroGcCombineDependencies()
        {
            for (int i = 0; i < 1000; i++)
            {
                var a = JobSystem.Schedule(new ActionJob(() => { }));
                var b = JobSystem.Schedule(new ActionJob(() => { }));
                JobHandle.CombineDependencies(a, b).Complete();
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 5000; i++)
            {
                var a = JobSystem.Schedule(new ActionJob(() => { }));
                var b = JobSystem.Schedule(new ActionJob(() => { }));
                JobHandle.CombineDependencies(a, b).Complete();
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestRunner.Assert(allocated < 4096, $"CombineDependencies steady-state: {allocated} bytes / 5000 combines (< 4KB)");
        }

        [Test]
        public static void IsCompletedWhileBlocked()
        {
            var gate = new ManualResetEventSlim(false);
            var h = JobSystem.Schedule(new ActionJob(() => gate.Wait()));
            TestRunner.Assert(!h.IsCompleted, "not completed while blocked");
            gate.Set();
            h.Complete();
        }

        [Test]
        public static void IsCompletedAfterRelease()
        {
            var gate = new ManualResetEventSlim(false);
            var h = JobSystem.Schedule(new ActionJob(() => gate.Wait()));
            gate.Set();
            h.Complete();
            TestRunner.Assert(h.IsCompleted, "completed after release");
        }

        [Test]
        public static void IsCompletedAfterComplete()
        {
            var done = JobSystem.Schedule(new ActionJob(() => { }));
            done.Complete();
            TestRunner.Assert(done.IsCompleted, "IsCompleted after complete");
        }

        [Test]
        public static void DefaultHandleIsCompleted()
        {
            TestRunner.Assert(default(JobHandle).IsCompleted, "default handle is completed");
        }

        [Test]
        public static void Stress()
        {
            const int count = 3000;
            var handles = new JobHandle[count];
            var rng = new Random(12345);
            var completed = new int[count];
            for (int i = 0; i < count; i++)
            {
                int local = i;
                if (i == 0)
                {
                    handles[i] = JobSystem.Schedule(new ActionJob(() => Interlocked.Increment(ref completed[local])));
                }
                else
                {
                    JobHandle dep = handles[rng.Next(i)];
                    if (rng.Next(2) == 0) dep = default;
                    handles[i] = JobSystem.Schedule(new ActionJob(() => Interlocked.Increment(ref completed[local])), dep);
                }
            }
            foreach (var h in handles) h.Complete();
            TestRunner.Assert(completed.All(c => c == 1), "all jobs ran exactly once, no deadlock");
        }

        [Test]
        public static void WorkerParallelismEvidence()
        {
            if (Environment.ProcessorCount < 2)
            {
                Console.WriteLine("  [skip] needs >1 CPU");
                return;
            }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var a = JobSystem.Schedule(new ActionJob(() => Thread.Sleep(300)));
            var b = JobSystem.Schedule(new ActionJob(() => Thread.Sleep(300)));
            var c = JobSystem.Schedule(new ActionJob(() => Thread.Sleep(300)));
            var d = JobSystem.Schedule(new ActionJob(() => Thread.Sleep(300)));
            JobHandle.CombineDependencies(new[] { a, b, c, d }).Complete();
            sw.Stop();
            TestRunner.Assert(sw.ElapsedMilliseconds < 1000, $"4x300ms jobs took {sw.ElapsedMilliseconds}ms (parallel if < ~1000ms)");
        }

        private struct ActionJob : IJob
        {
            private readonly Action _action;
            public ActionJob(Action action) { _action = action; }
            public void Execute() => _action();
        }

        private struct IndexJob : IJobParallelFor
        {
            private readonly int[] _seen;
            public IndexJob(int[] seen) { _seen = seen; }
            public void Execute(int index) => Interlocked.Increment(ref _seen[index]);
        }

        private struct BatchJob : IJobParallelForBatch
        {
            private readonly int[] _seen;
            public BatchJob(int[] seen) { _seen = seen; }
            public void Execute(int startIndex, int count)
            {
                for (int i = startIndex; i < startIndex + count; i++) Interlocked.Increment(ref _seen[i]);
            }
        }

        private struct ThrowAtJob : IJobParallelFor
        {
            private readonly int _throwAt;
            private readonly Action _onOther;
            public ThrowAtJob(int throwAt, Action onOther) { _throwAt = throwAt; _onOther = onOther; }
            public void Execute(int index)
            {
                if (index == _throwAt) throw new InvalidOperationException("batch-boom");
                _onOther();
            }
        }
    }
}
