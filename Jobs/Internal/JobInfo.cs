using System;
using System.Collections.Generic;
using System.Threading;

namespace EngineX.Jobs.Internal
{
    internal sealed class JobInfo : JobInfoBase
    {
        public Action Body;

        private static readonly ThreadLocal<Stack<JobInfo>> Pool =
            new ThreadLocal<Stack<JobInfo>>(() => new Stack<JobInfo>());

        internal static JobInfo Acquire()
        {
            var pool = Pool.Value;
            var info = pool.Count > 0 ? pool.Pop() : new JobInfo();
            info.Reset();
            return info;
        }

        public override void ExecuteBody()
        {
            Body?.Invoke();
        }

        protected override void PushToPool()
        {
            Body = null;
            Pool.Value.Push(this);
        }
    }

    // Parallel-for workers never expose their handle to users, so they live in a
    // global pool (recycled on worker threads, acquired on scheduling threads)
    // and recycle themselves right after dispatch.
    internal sealed class ParallelForActionJobInfo : JobInfoBase
    {
        public Action<int> Body;
        public int ArrayLength;
        public int BatchSize;
        public JobInfoBase Owner;

        private static readonly Stack<ParallelForActionJobInfo> Pool = new Stack<ParallelForActionJobInfo>();
        private static readonly object PoolLock = new object();


        internal static ParallelForActionJobInfo Acquire()
        {
            lock (PoolLock)
            {
                var info = Pool.Count > 0 ? Pool.Pop() : new ParallelForActionJobInfo();
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
                for (int i = 0; i < count; i++) Body(start + i);
            }
        }

        protected override void PushToPool()
        {
            Body = null;
            Owner = null;
            ArrayLength = 0;
            BatchSize = 0;
            lock (PoolLock)
            {
                Pool.Push(this);
            }
        }
    }

    internal sealed class ParallelForBatchActionJobInfo : JobInfoBase
    {
        public Action<int, int> Body;
        public int ArrayLength;
        public int BatchSize;
        public JobInfoBase Owner;

        private static readonly Stack<ParallelForBatchActionJobInfo> Pool = new Stack<ParallelForBatchActionJobInfo>();
        private static readonly object PoolLock = new object();


        internal static ParallelForBatchActionJobInfo Acquire()
        {
            lock (PoolLock)
            {
                var info = Pool.Count > 0 ? Pool.Pop() : new ParallelForBatchActionJobInfo();
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
                Body(start, count);
            }
        }

        protected override void PushToPool()
        {
            Body = null;
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
