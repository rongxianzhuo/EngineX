using System;
using System.Collections.Generic;
using System.Threading;

namespace EngineX.Jobs.Internal
{
    internal sealed class JobInfo : JobInfoBase
    {
        public Action Body;
        public int RefCount;
        public readonly ManualResetEventSlim CompletedEvent = new ManualResetEventSlim(false);
        public Exception Error;

        private List<JobInfo> _successors;

        public override bool IsCompleted => CompletedEvent.IsSet;

        public override void Complete()
        {
            if (CompletedEvent.IsSet)
            {
                ThrowPendingError();
                return;
            }
            CompletedEvent.Wait();
            ThrowPendingError();
        }

        internal override int RegisterSuccessor(JobInfo successor)
        {
            lock (this)
            {
                if (CompletedEvent.IsSet)
                {
                    return 0;
                }
                _successors ??= new List<JobInfo>();
                _successors.Add(successor);
                return 1;
            }
        }

        internal void MarkCompletedAndDispatch()
        {
            List<JobInfo> succs;
            lock (this)
            {
                if (CompletedEvent.IsSet)
                {
                    return;
                }
                CompletedEvent.Set();
                succs = _successors;
                _successors = null;
            }
            if (succs == null) return;
            for (int i = 0; i < succs.Count; i++)
            {
                var s = succs[i];
                if (Interlocked.Decrement(ref s.RefCount) == 0)
                {
                    JobScheduler.EnqueueReady(s);
                }
            }
        }

        private void ThrowPendingError()
        {
            var err = Error;
            if (err != null)
            {
                Error = null;
                throw err;
            }
        }
    }
}