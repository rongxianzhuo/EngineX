using System;
using System.Collections.Concurrent;
using System.Threading;

namespace EngineX.Jobs.Internal
{
    internal static class JobScheduler
    {
        private static readonly ConcurrentQueue<JobInfo> Pending = new ConcurrentQueue<JobInfo>();
        private static readonly SemaphoreSlim Wakeup = new SemaphoreSlim(0, int.MaxValue);
        private static Thread[] _workers;
        private static int _workerCount;
        private static int _initialized;

        public static int WorkerCount => _workerCount;

        public static int MaxJobThreadCount => Environment.ProcessorCount;

        private static int MaxBackgroundWorkers => Math.Max(1, Environment.ProcessorCount - 1);

        public static void EnsureInitialized()
        {
            if (Volatile.Read(ref _initialized) != 0) return;
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0) return;

            int count = MaxBackgroundWorkers;
            _workers = new Thread[count];
            for (int i = 0; i < count; i++)
            {
                var t = new Thread(WorkerLoop)
                {
                    IsBackground = true,
                    Name = "EngineXJobWorker-" + i,
                };
                _workers[i] = t;
                t.Start();
            }
            _workerCount = count;
        }

        private static void WorkerLoop()
        {
            while (true)
            {
                if (Pending.TryDequeue(out var job))
                {
                    RunJob(job);
                }
                else
                {
                    try
                    {
                        Wakeup.Wait();
                    }
                    catch (ThreadInterruptedException) { }
                }
            }
        }

        private static void RunJob(JobInfo job)
        {
            try
            {
                job.Body();
            }
            catch (Exception ex)
            {
                job.Error = ex;
            }
            job.MarkCompletedAndDispatch();
        }

        public static void EnqueueReady(JobInfo job)
        {
            Pending.Enqueue(job);
            Wakeup.Release();
        }

        public static JobHandle Schedule(Action body, JobHandle dependsOn)
        {
            EnsureInitialized();
            var info = new JobInfo { Body = body };
            int refCount = 0;
            if (dependsOn.Info != null && !dependsOn.Info.IsCompleted)
            {
                refCount = dependsOn.Info.RegisterSuccessor(info);
            }
            info.RefCount = refCount;
            if (refCount == 0)
            {
                EnqueueReady(info);
            }
            return new JobHandle(info);
        }

        public static JobHandle ScheduleParallelFor(Action<int> body, int arrayLength, JobHandle dependsOn)
        {
            EnsureInitialized();
            int workerCount = _workerCount;
            if (arrayLength <= 0 || workerCount <= 1)
            {
                return Schedule(() =>
                {
                    for (int i = 0; i < arrayLength; i++) body(i);
                }, dependsOn);
            }

            var handles = new JobHandle[workerCount];
            int chunkSize = (arrayLength + workerCount - 1) / workerCount;
            for (int w = 0; w < workerCount; w++)
            {
                int start = w * chunkSize;
                int end = Math.Min(start + chunkSize, arrayLength);
                if (start >= end) break;
                int localStart = start;
                int localEnd = end;
                handles[w] = Schedule(() =>
                {
                    for (int i = localStart; i < localEnd; i++) body(i);
                }, dependsOn);
            }
            return JobHandle.CombineDependencies(handles);
        }

        public static JobHandle ScheduleParallelForBatch(Action<int, int> body, int arrayLength, JobHandle dependsOn)
        {
            EnsureInitialized();
            int workerCount = _workerCount;
            if (arrayLength <= 0 || workerCount <= 1)
            {
                return Schedule(() =>
                {
                    int b = 0;
                    while (b < arrayLength)
                    {
                        int next = Math.Min(b + 64, arrayLength);
                        body(b, next - b);
                        b = next;
                    }
                }, dependsOn);
            }

            var handles = new JobHandle[workerCount];
            int chunkSize = (arrayLength + workerCount - 1) / workerCount;
            for (int w = 0; w < workerCount; w++)
            {
                int start = w * chunkSize;
                int end = Math.Min(start + chunkSize, arrayLength);
                if (start >= end) break;
                int localStart = start;
                int localEnd = end;
                handles[w] = Schedule(() =>
                {
                    int b = localStart;
                    while (b < localEnd)
                    {
                        int next = Math.Min(b + 64, localEnd);
                        body(b, next - b);
                        b = next;
                    }
                }, dependsOn);
            }
            return JobHandle.CombineDependencies(handles);
        }
    }
}