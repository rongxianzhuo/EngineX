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
        private int _skipped;
        private List<JobInfo> _successors;

        public override bool IsCompleted => CompletedEvent.IsSet;

        internal override bool HasError => Volatile.Read(ref Error) != null;

        internal override Exception FirstError => Volatile.Read(ref Error);

        internal bool IsSkipped => Volatile.Read(ref _skipped) != 0;

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
                    var err = Error;
                    if (err != null) MarkSkipped(err, successor);
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
            Exception myError;
            lock (this)
            {
                if (CompletedEvent.IsSet)
                {
                    return;
                }
                CompletedEvent.Set();
                succs = _successors;
                _successors = null;
                myError = Error;
            }
            if (succs == null) return;
            bool failed = myError != null;
            for (int i = 0; i < succs.Count; i++)
            {
                var s = succs[i];
                if (failed)
                {
                    MarkSkipped(myError, s);
                }
                if (Interlocked.Decrement(ref s.RefCount) == 0)
                {
                    JobScheduler.EnqueueReady(s);
                }
            }
        }

        internal static void MarkSkipped(Exception error, JobInfo successor)
        {
            if (Interlocked.CompareExchange(ref successor.Error, error, null) == null)
            {
                Volatile.Write(ref successor._skipped, 1);
            }
        }

        private void ThrowPendingError()
        {
            var err = Error;
            if (err != null)
            {
                throw err;
            }
        }
    }
}
