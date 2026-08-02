using System;
using EngineX.Jobs.Internal;

namespace EngineX.Jobs
{
    public readonly struct JobHandle : IEquatable<JobHandle>
    {
        internal readonly JobInfoBase Info;
        internal readonly int Version;

        internal JobHandle(JobInfoBase info)
        {
            Info = info;
            Version = info == null ? 0 : info.Version;
        }

        internal bool IsAlive => Info != null && Info.Version == Version;

        public bool IsCompleted
        {
            get
            {
                var info = Info;
                if (info == null) return true;
                if (info.Version != Version) return true;
                return info.IsCompleted;
            }
        }

        public void Complete()
        {
            var info = Info;
            if (info != null && info.Version == Version)
            {
                info.Complete(Version);
            }
        }

        public bool Equals(JobHandle other)
        {
            return ReferenceEquals(Info, other.Info) && Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            return obj is JobHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Info == null ? Version : HashCode.Combine(Info.GetHashCode(), Version);
        }

        public static bool operator ==(JobHandle a, JobHandle b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(JobHandle a, JobHandle b)
        {
            return !a.Equals(b);
        }

        public static JobHandle CombineDependencies(JobHandle a, JobHandle b)
        {
            if (!a.IsAlive) return b;
            if (!b.IsAlive) return a;
            return new JobHandle(CompositeJobInfo.Acquire2(a, b));
        }

        public static JobHandle CombineDependencies(JobHandle a, JobHandle b, JobHandle c)
        {
            if (!a.IsAlive) return CombineDependencies(b, c);
            if (!b.IsAlive) return CombineDependencies(a, c);
            if (!c.IsAlive) return CombineDependencies(a, b);
            return new JobHandle(CompositeJobInfo.Acquire3(a, b, c));
        }

        public static JobHandle CombineDependencies(JobHandle[] handles)
        {
            if (handles == null || handles.Length == 0) return default;

            int n = 0;
            for (int i = 0; i < handles.Length; i++)
            {
                if (handles[i].IsAlive) n++;
            }
            if (n == 0) return default;
            if (n == 1)
            {
                for (int i = 0; i < handles.Length; i++)
                {
                    if (handles[i].IsAlive) return handles[i];
                }
            }
            if (n == 2 || n == 3)
            {
                JobHandle x = default, y = default, z = default;
                int filled = 0;
                for (int i = 0; i < handles.Length && filled < n; i++)
                {
                    if (!handles[i].IsAlive) continue;
                    if (filled == 0) x = handles[i];
                    else if (filled == 1) y = handles[i];
                    else z = handles[i];
                    filled++;
                }
                return n == 2 ? CombineDependencies(x, y) : CombineDependencies(x, y, z);
            }

            var alive = new JobHandle[n];
            int k = 0;
            for (int i = 0; i < handles.Length; i++)
            {
                if (handles[i].IsAlive) alive[k++] = handles[i];
            }
            return new JobHandle(CompositeJobInfo.AcquireMany(alive, n));
        }
    }
}
