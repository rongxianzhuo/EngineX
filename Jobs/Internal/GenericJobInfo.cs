using System;
using System.Collections.Generic;
using System.Threading;

namespace EngineX.Jobs.Internal
{
    internal sealed class JobInfo<T> : JobInfoBase where T : struct, IJob
    {
        public T Job;

        private static readonly ThreadLocal<Stack<JobInfo<T>>> Pool =
            new ThreadLocal<Stack<JobInfo<T>>>(() => new Stack<JobInfo<T>>());

        internal static JobInfo<T> Acquire()
        {
            var pool = Pool.Value;
            var info = pool.Count > 0 ? pool.Pop() : new JobInfo<T>();
            info.Reset();
            return info;
        }

        public override void ExecuteBody()
        {
            Job.Execute();
        }

        protected override void PushToPool()
        {
            Job = default;
            Pool.Value.Push(this);
        }
    }

    // Parallel-for workers never expose their handle to users, so they live in a
    // global pool (recycled on worker threads, acquired on scheduling threads)
    // and recycle themselves right after dispatch.
    internal sealed class ParallelForJobInfo<T> : JobInfoBase where T : struct, IJobParallelFor
    {
        public T Job;
        public int ArrayLength;
        public int BatchSize;
        public JobInfoBase Owner;

        private static readonly Stack<ParallelForJobInfo<T>> Pool = new Stack<ParallelForJobInfo<T>>();
        private static readonly object PoolLock = new object();


        internal static ParallelForJobInfo<T> Acquire()
        {
            lock (PoolLock)
            {
                var info = Pool.Count > 0 ? Pool.Pop() : new ParallelForJobInfo<T>();
                info.Reset();
                return info;
            }
        }

        public override void ExecuteBody()
        {
            while (true)
            {
                int start = Interlocked.Add(ref Owner.Cursor, BatchSize) - BatchSize;
                if (start >= ArrayLength) return;
                int count = Math.Min(BatchSize, ArrayLength - start);
                for (int i = 0; i < count; i++) Job.Execute(start + i);
            }
        }

        protected override void PushToPool()
        {
            Job = default;
            Owner = null;
            ArrayLength = 0;
            BatchSize = 0;
            lock (PoolLock)
            {
                Pool.Push(this);
            }
        }
    }

    internal sealed class ParallelForBatchJobInfo<T> : JobInfoBase where T : struct, IJobParallelForBatch
    {
        public T Job;
        public int ArrayLength;
        public int BatchSize;
        public JobInfoBase Owner;

        private static readonly Stack<ParallelForBatchJobInfo<T>> Pool = new Stack<ParallelForBatchJobInfo<T>>();
        private static readonly object PoolLock = new object();


        internal static ParallelForBatchJobInfo<T> Acquire()
        {
            lock (PoolLock)
            {
                var info = Pool.Count > 0 ? Pool.Pop() : new ParallelForBatchJobInfo<T>();
                info.Reset();
                return info;
            }
        }

        public override void ExecuteBody()
        {
            while (true)
            {
                int start = Interlocked.Add(ref Owner.Cursor, BatchSize) - BatchSize;
                if (start >= ArrayLength) return;
                int count = Math.Min(BatchSize, ArrayLength - start);
                Job.Execute(start, count);
            }
        }

        protected override void PushToPool()
        {
            Job = default;
            Owner = null;
            ArrayLength = 0;
            BatchSize = 0;
            lock (PoolLock)
            {
                Pool.Push(this);
            }
        }
    }
}
