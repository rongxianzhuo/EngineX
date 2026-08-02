using System;

namespace EngineX.Jobs.Internal
{
    internal abstract class JobInfoBase
    {
        public abstract bool IsCompleted { get; }

        public abstract void Complete();

        internal abstract int RegisterSuccessor(JobInfo successor);

        internal abstract bool HasError { get; }

        internal abstract Exception FirstError { get; }
    }
}
