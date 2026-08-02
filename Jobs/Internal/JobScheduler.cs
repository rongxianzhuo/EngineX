using System;
using System.Collections.Concurrent;
using System.Threading;

namespace EngineX.Jobs.Internal
{
    internal static class JobScheduler
    {
        private static readonly ConcurrentQueue<JobInfoBase> Pending = new ConcurrentQueue<JobInfoBase>();
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

        private static void RunJob(JobInfoBase job)
        {
            try
            {
                if (!job.IsSkipped)
                {
                    job.ExecuteBody();
                }
            }
            catch (Exception ex)
            {
                job.Error = ex;
            }
            job.MarkCompletedAndDispatch();
        }

        public static void EnqueueReady(JobInfoBase job)
        {
            Pending.Enqueue(job);
            Wakeup.Release();
        }

        public static JobHandle Schedule<T>(T job, JobHandle dependsOn) where T : struct, IJob
        {
            EnsureInitialized();
            var info = JobInfo<T>.Acquire();
            info.Job = job;
            return RegisterAndEnqueue(info, dependsOn);
        }

        public static JobHandle Schedule(Action body, JobHandle dependsOn)
        {
            EnsureInitialized();
            var info = JobInfo.Acquire();
            info.Body = body;
            return RegisterAndEnqueue(info, dependsOn);
        }

        public static JobHandle ScheduleParallelFor<T>(T job, int arrayLength, int innerLoopBatchCount, JobHandle dependsOn) where T : struct, IJobParallelFor
        {
            EnsureInitialized();
            if (arrayLength <= 0) return default;

            int batchSize = Math.Max(1, innerLoopBatchCount);
            int batchCount = (arrayLength + batchSize - 1) / batchSize;
            int jobCount = Math.Min(_workerCount, batchCount);

            // Barrier node: one handle for the whole parallel-for. It owns the
            // shared batch cursor and completes when every worker is done.
            var parent = JobInfo.Acquire();
            parent.Cursor = 0;
            Volatile.Write(ref parent.RefCount, jobCount);

            for (int w = 0; w < jobCount; w++)
            {
                var worker = ParallelForJobInfo<T>.Acquire();
                worker.Job = job;
                worker.ArrayLength = arrayLength;
                worker.BatchSize = batchSize;
                worker.Owner = parent;
                worker.AddSuccessor(parent);
                parent.Children ??= new System.Collections.Generic.List<JobInfoBase>();
                parent.Children.Add(worker);

                int refCount = RegisterDependency(worker, dependsOn);
                Volatile.Write(ref worker.RefCount, refCount);
                if (refCount == 0) EnqueueReady(worker);
            }
            return new JobHandle(parent);
        }

        public static JobHandle ScheduleParallelFor(Action<int> body, int arrayLength, int innerLoopBatchCount, JobHandle dependsOn)
        {
            EnsureInitialized();
            if (arrayLength <= 0) return default;

            int batchSize = Math.Max(1, innerLoopBatchCount);
            int batchCount = (arrayLength + batchSize - 1) / batchSize;
            int jobCount = Math.Min(_workerCount, batchCount);

            var parent = JobInfo.Acquire();
            parent.Cursor = 0;
            Volatile.Write(ref parent.RefCount, jobCount);

            for (int w = 0; w < jobCount; w++)
            {
                var worker = ParallelForActionJobInfo.Acquire();
                worker.Body = body;
                worker.ArrayLength = arrayLength;
                worker.BatchSize = batchSize;
                worker.Owner = parent;
                worker.AddSuccessor(parent);
                parent.Children ??= new System.Collections.Generic.List<JobInfoBase>();
                parent.Children.Add(worker);

                int refCount = RegisterDependency(worker, dependsOn);
                Volatile.Write(ref worker.RefCount, refCount);
                if (refCount == 0) EnqueueReady(worker);
            }
            return new JobHandle(parent);
        }

        public static JobHandle ScheduleParallelForBatch<T>(T job, int arrayLength, int innerLoopBatchCount, JobHandle dependsOn) where T : struct, IJobParallelForBatch
        {
            EnsureInitialized();
            if (arrayLength <= 0) return default;

            int batchSize = Math.Max(1, innerLoopBatchCount);
            int batchCount = (arrayLength + batchSize - 1) / batchSize;
            int jobCount = Math.Min(_workerCount, batchCount);

            var parent = JobInfo.Acquire();
            parent.Cursor = 0;
            Volatile.Write(ref parent.RefCount, jobCount);

            for (int w = 0; w < jobCount; w++)
            {
                var worker = ParallelForBatchJobInfo<T>.Acquire();
                worker.Job = job;
                worker.ArrayLength = arrayLength;
                worker.BatchSize = batchSize;
                worker.Owner = parent;
                worker.AddSuccessor(parent);
                parent.Children ??= new System.Collections.Generic.List<JobInfoBase>();
                parent.Children.Add(worker);

                int refCount = RegisterDependency(worker, dependsOn);
                Volatile.Write(ref worker.RefCount, refCount);
                if (refCount == 0) EnqueueReady(worker);
            }
            return new JobHandle(parent);
        }

        public static JobHandle ScheduleParallelForBatch(Action<int, int> body, int arrayLength, int innerLoopBatchCount, JobHandle dependsOn)
        {
            EnsureInitialized();
            if (arrayLength <= 0) return default;

            int batchSize = Math.Max(1, innerLoopBatchCount);
            int batchCount = (arrayLength + batchSize - 1) / batchSize;
            int jobCount = Math.Min(_workerCount, batchCount);

            var parent = JobInfo.Acquire();
            parent.Cursor = 0;
            Volatile.Write(ref parent.RefCount, jobCount);

            for (int w = 0; w < jobCount; w++)
            {
                var worker = ParallelForBatchActionJobInfo.Acquire();
                worker.Body = body;
                worker.ArrayLength = arrayLength;
                worker.BatchSize = batchSize;
                worker.Owner = parent;
                worker.AddSuccessor(parent);
                parent.Children ??= new System.Collections.Generic.List<JobInfoBase>();
                parent.Children.Add(worker);

                int refCount = RegisterDependency(worker, dependsOn);
                Volatile.Write(ref worker.RefCount, refCount);
                if (refCount == 0) EnqueueReady(worker);
            }
            return new JobHandle(parent);
        }

        private static JobHandle RegisterAndEnqueue(JobInfoBase info, JobHandle dependsOn)
        {
            int refCount = RegisterDependency(info, dependsOn);
            Volatile.Write(ref info.RefCount, refCount);
            if (refCount == 0)
            {
                EnqueueReady(info);
            }
            return new JobHandle(info);
        }

        private static int RegisterDependency(JobInfoBase info, JobHandle dependsOn)
        {
            var dep = dependsOn.Info;
            if (dep == null) return 0;
            lock (dep)
            {
                if (dep.Version != dependsOn.Version) return 0;
                if (!dep.IsCompleted)
                {
                    return dep.RegisterSuccessor(info);
                }
                if (dep.HasError)
                {
                    JobInfoBase.MarkSkipped(dep.FirstError, info);
                }
                return 0;
            }
        }
    }
}
