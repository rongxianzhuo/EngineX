using System;
using System.Collections.Generic;
using System.Threading;

namespace EngineX.Jobs.Internal
{
    internal abstract class JobInfoBase
    {
        public int RefCount;
        public readonly ManualResetEventSlim CompletedEvent = new ManualResetEventSlim(false);
        public Exception Error;
        public int Cursor;

        // Parallel-for workers owned by a barrier parent; recycled together with
        // the parent when the user completes its handle.
        internal List<JobInfoBase> Children;

        private int _skipped;
        private List<JobInfoBase> _successors;
        private bool _completed;
        protected int _version;

        public int Version => Volatile.Read(ref _version);

        public virtual bool IsCompleted => CompletedEvent.IsSet;

        internal bool IsSkipped => Volatile.Read(ref _skipped) != 0;

        internal virtual bool HasError => Volatile.Read(ref Error) != null;

        internal virtual Exception FirstError => Volatile.Read(ref Error);

        internal void Reset()
        {
            CompletedEvent.Reset();
            Error = null;
            Cursor = 0;
            _skipped = 0;
            if (_successors != null) _successors.Clear();
            if (Children != null) Children.Clear();
            _completed = false;
            RefCount = 0;
        }

        public abstract void ExecuteBody();

        internal void ReleaseToPool()
        {
            Interlocked.Increment(ref _version);
            PushToPool();
        }

        protected abstract void PushToPool();

        internal virtual void Complete(int version)
        {
            CompletedEvent.Wait();
            lock (this)
            {
                if (_version != version) return;
                // Note: Children list is intentionally kept (not nulled); the
                // next Acquire() clears it via Reset(), so it is reused.
                var children = Children;
                var err = Error;
                try
                {
                    if (err != null) throw err;
                }
                finally
                {
                    if (children != null)
                    {
                        for (int i = 0; i < children.Count; i++)
                        {
                            children[i].ReleaseToPool();
                        }
                    }
                    ReleaseToPool();
                }
            }
        }

        internal virtual int RegisterSuccessor(JobInfoBase successor)
        {
            lock (this)
            {
                if (_completed)
                {
                    var err = Error;
                    if (err != null) MarkSkipped(err, successor);
                    return 0;
                }
                _successors ??= new List<JobInfoBase>();
                _successors.Add(successor);
                return 1;
            }
        }

        internal void AddSuccessor(JobInfoBase successor)
        {
            lock (this)
            {
                if (_completed) return;
                _successors ??= new List<JobInfoBase>();
                _successors.Add(successor);
            }
        }

        internal void MarkCompletedAndDispatch()
        {
            List<JobInfoBase> succs;
            Exception myError;
            lock (this)
            {
                if (_completed) return;
                _completed = true;
                // Keep the successor list (not nulled): it is reused and cleared
                // by Reset() on the next Acquire, avoiding per-job allocations.
                succs = _successors;
                myError = Error;
            }

            bool failed = myError != null;
            if (succs != null)
            {
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

            // Set the completion event LAST: once Wait() returns, the info is
            // fully dispatched and safe to recycle in Complete().
            CompletedEvent.Set();
        }

        internal static void MarkSkipped(Exception error, JobInfoBase successor)
        {
            if (Interlocked.CompareExchange(ref successor.Error, error, null) == null)
            {
                Volatile.Write(ref successor._skipped, 1);
            }
        }
    }
}
