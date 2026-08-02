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
                if (!job.IsSkipped)
                {
                    job.Body();
                }
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
            if (dependsOn.Info != null)
            {
                if (!dependsOn.Info.IsCompleted)
                {
                    refCount = dependsOn.Info.RegisterSuccessor(info);
                }
                else if (dependsOn.Info.HasError)
                {
                    JobInfo.MarkSkipped(dependsOn.Info.FirstError, info);
                }
            }
            Volatile.Write(ref info.RefCount, refCount);
            if (refCount == 0)
            {
                EnqueueReady(info);
            }
            return new JobHandle(info);
        }

        public static JobHandle ScheduleParallelFor(Action<int> body, int arrayLength, int innerLoopBatchCount, JobHandle dependsOn)
        {
            EnsureInitialized();
            if (arrayLength <= 0) return default;

            int batchSize = Math.Max(1, innerLoopBatchCount);
            int batchCount = (arrayLength + batchSize - 1) / batchSize;
            int jobCount = Math.Min(_workerCount, batchCount);

            int cursor = 0;
            var pull = new Action(() =>
            {
                while (true)
                {
                    int start = Interlocked.Add(ref cursor, batchSize) - batchSize;
                    if (start >= arrayLength) break;
                    int count = Math.Min(batchSize, arrayLength - start);
                    for (int i = 0; i < count; i++) body(start + i);
                }
            });

            var handles = new JobHandle[jobCount];
            for (int w = 0; w < jobCount; w++)
            {
                handles[w] = Schedule(pull, dependsOn);
            }
            return JobHandle.CombineDependencies(handles);
        }

        public static JobHandle ScheduleParallelForBatch(Action<int, int> body, int arrayLength, int innerLoopBatchCount, JobHandle dependsOn)
        {
            EnsureInitialized();
            if (arrayLength <= 0) return default;

            int batchSize = Math.Max(1, innerLoopBatchCount);
            int batchCount = (arrayLength + batchSize - 1) / batchSize;
            int jobCount = Math.Min(_workerCount, batchCount);

            int cursor = 0;
            var pull = new Action(() =>
            {
                while (true)
                {
                    int start = Interlocked.Add(ref cursor, batchSize) - batchSize;
                    if (start >= arrayLength) break;
                    int count = Math.Min(batchSize, arrayLength - start);
                    body(start, count);
                }
            });

            var handles = new JobHandle[jobCount];
            for (int w = 0; w < jobCount; w++)
            {
                handles[w] = Schedule(pull, dependsOn);
            }
            return JobHandle.CombineDependencies(handles);
        }
    }
}
