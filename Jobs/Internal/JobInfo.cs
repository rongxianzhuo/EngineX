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
        public List<JobInfo> Successors;
        public Exception Error;

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

        private void ThrowPendingError()
        {
            var err = Error;
            if (err != null)
            {
                Error = null;
                throw err;
            }
        }

        internal void DispatchSuccessors()
        {
            var succs = Successors;
            Successors = null;
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
    }
}