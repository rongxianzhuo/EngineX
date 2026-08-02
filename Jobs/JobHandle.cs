using System;
using EngineX.Jobs.Internal;

namespace EngineX.Jobs
{
    public readonly struct JobHandle : IEquatable<JobHandle>
    {
        internal readonly JobInfoBase Info;

        internal JobHandle(JobInfoBase info)
        {
            Info = info;
        }

        public bool IsCompleted => Info == null || Info.IsCompleted;

        public void Complete()
        {
            Info?.Complete();
        }

        public bool Equals(JobHandle other)
        {
            return ReferenceEquals(Info, other.Info);
        }

        public override bool Equals(object obj)
        {
            return obj is JobHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Info == null ? 0 : Info.GetHashCode();
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
            if (a.Info == null) return b;
            if (b.Info == null) return a;
            return new JobHandle(new CompositeJobInfo(new[] { a, b }));
        }

        public static JobHandle CombineDependencies(JobHandle a, JobHandle b, JobHandle c)
        {
            if (a.Info == null) return CombineDependencies(b, c);
            if (b.Info == null) return CombineDependencies(a, c);
            if (c.Info == null) return CombineDependencies(a, b);
            return new JobHandle(new CompositeJobInfo(new[] { a, b, c }));
        }

        public static JobHandle CombineDependencies(JobHandle[] handles)
        {
            if (handles == null || handles.Length == 0) return default;
            JobHandle result = default;
            for (int i = 0; i < handles.Length; i++)
            {
                result = CombineDependencies(result, handles[i]);
            }
            return result;
        }
    }
}