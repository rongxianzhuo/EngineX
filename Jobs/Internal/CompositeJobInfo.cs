using System;
using System.Collections.Generic;
using System.Threading;

namespace EngineX.Jobs.Internal
{
    internal sealed class CompositeJobInfo : JobInfoBase
    {
        private JobHandle _a;
        private JobHandle _b;
        private JobHandle _c;
        private JobHandle[] _extra;
        private int _count;

        private static readonly ThreadLocal<Stack<CompositeJobInfo>> Pool =
            new ThreadLocal<Stack<CompositeJobInfo>>(() => new Stack<CompositeJobInfo>());

        internal static CompositeJobInfo Acquire2(JobHandle a, JobHandle b)
        {
            var info = Acquire();
            info._a = a;
            info._b = b;
            info._count = 2;
            return info;
        }

        internal static CompositeJobInfo Acquire3(JobHandle a, JobHandle b, JobHandle c)
        {
            var info = Acquire();
            info._a = a;
            info._b = b;
            info._c = c;
            info._count = 3;
            return info;
        }

        internal static CompositeJobInfo AcquireMany(JobHandle[] handles, int count)
        {
            var info = Acquire();
            if (count <= 3)
            {
                info._a = handles[0];
                info._b = handles[1];
                info._c = handles[2];
            }
            else
            {
                info._extra = new JobHandle[count];
                Array.Copy(handles, info._extra, count);
            }
            info._count = count;
            return info;
        }

        private static CompositeJobInfo Acquire()
        {
            var pool = Pool.Value;
            var info = pool.Count > 0 ? pool.Pop() : new CompositeJobInfo();
            info.Reset();
            return info;
        }

        private JobHandle Get(int index)
        {
            if (_extra != null) return _extra[index];
            if (index == 0) return _a;
            if (index == 1) return _b;
            return _c;
        }

        public override bool IsCompleted
        {
            get
            {
                for (int i = 0; i < _count; i++)
                {
                    if (!Get(i).IsCompleted) return false;
                }
                return true;
            }
        }

        public override void ExecuteBody()
        {
            throw new InvalidOperationException("CompositeJobInfo cannot be executed");
        }

        internal override void Complete(int version)
        {
            for (int i = 0; i < _count; i++)
            {
                Get(i).Complete();
            }
            lock (this)
            {
                if (_version != version) return;
                ReleaseToPool();
            }
        }

        internal override int RegisterSuccessor(JobInfoBase successor)
        {
            int count = 0;
            for (int i = 0; i < _count; i++)
            {
                var h = Get(i);
                if (h.Info != null && h.Info.Version == h.Version)
                {
                    count += h.Info.RegisterSuccessor(successor);
                }
            }
            return count;
        }

        internal override bool HasError
        {
            get
            {
                for (int i = 0; i < _count; i++)
                {
                    var h = Get(i);
                    if (h.Info != null && h.Info.Version == h.Version && h.Info.HasError) return true;
                }
                return false;
            }
        }

        internal override Exception FirstError
        {
            get
            {
                for (int i = 0; i < _count; i++)
                {
                    var h = Get(i);
                    if (h.Info != null && h.Info.Version == h.Version)
                    {
                        var err = h.Info.FirstError;
                        if (err != null) return err;
                    }
                }
                return null;
            }
        }

        protected override void PushToPool()
        {
            _a = default;
            _b = default;
            _c = default;
            _extra = null;
            _count = 0;
            Pool.Value.Push(this);
        }
    }
}
