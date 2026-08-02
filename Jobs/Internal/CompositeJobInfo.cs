namespace EngineX.Jobs.Internal
{
    internal sealed class CompositeJobInfo : JobInfoBase
    {
        public readonly JobHandle[] Handles;

        public CompositeJobInfo(JobHandle[] handles)
        {
            Handles = handles;
        }

        public override bool IsCompleted
        {
            get
            {
                for (int i = 0; i < Handles.Length; i++)
                {
                    if (!Handles[i].IsCompleted) return false;
                }
                return true;
            }
        }

        public override void Complete()
        {
            for (int i = 0; i < Handles.Length; i++)
            {
                Handles[i].Complete();
            }
        }

        internal override int RegisterSuccessor(JobInfo successor)
        {
            int count = 0;
            for (int i = 0; i < Handles.Length; i++)
            {
                var h = Handles[i];
                if (h.Info != null)
                {
                    count += h.Info.RegisterSuccessor(successor);
                }
            }
            return count;
        }
    }
}