using System;
using System.Linq;
using System.Threading;
using EngineX.Jobs;

namespace EngineX.Jobs.Tests
{
    public static class NativeArrayTests
    {
        [Test]
        public static void CreateAndLength()
        {
            var arr = new NativeArray<int>(1024, Allocator.TempJob);
            TestRunner.Assert(arr.Length == 1024, "Length");
            arr.Dispose();
        }

        [Test]
        public static void IndexerWriteReadRoundtrip()
        {
            var arr = new NativeArray<int>(1024, Allocator.TempJob);
            try
            {
                for (int i = 0; i < 1024; i++) arr[i] = i * 3;
                bool ok = true;
                for (int i = 0; i < 1024; i++) if (arr[i] != i * 3) ok = false;
                TestRunner.Assert(ok, "indexer write/read roundtrip");
            }
            finally { arr.Dispose(); }
        }




        [Test]
        public static void ToArray()
        {
            var arr = new NativeArray<int>(1024, Allocator.TempJob);
            try
            {
                arr[0] = 1;
                arr[1023] = 2;
                var copy = arr.ToArray();
                TestRunner.Assert(copy.Length == 1024 && copy[0] == 1 && copy[1023] == 2, "ToArray copies correctly");
            }
            finally { arr.Dispose(); }
        }


        [Test]
        public static void ZeroLengthArray()
        {
            var empty = new NativeArray<int>(0, Allocator.Temp);
            TestRunner.Assert(empty.Length == 0, "zero-length array: Length == 0");
            empty.Dispose();
        }

        [Test]
        public static void TempAllocatesNatively()
        {
            var t = new NativeArray<int>(16, Allocator.Temp);
            try
            {
                t[0] = 42;
                TestRunner.Assert(t[0] == 42, "Allocator.Temp allocates natively");
            }
            finally { t.Dispose(); }
        }

        public static void SubArrayValues()
        {
            var arr = new NativeArray<int>(100, Allocator.TempJob);
            try
            {
                for (int i = 0; i < 100; i++) arr[i] = i;
                var view = arr.GetSubArray(10, 20);
                TestRunner.Assert(view.Length == 20 && view[0] == 10 && view[19] == 29, "subarray view values");
            }
            finally { arr.Dispose(); }
        }

        [Test]
        public static void SubArrayWritesThroughToParent()
        {
            var arr = new NativeArray<int>(100, Allocator.TempJob);
            try
            {
                for (int i = 0; i < 100; i++) arr[i] = i;
                var view = arr.GetSubArray(10, 20);
                view[0] = -1;
                TestRunner.AssertEqual(-1, arr[10], "subarray writes through to parent");
            }
            finally { arr.Dispose(); }
        }

        [Test]
        public static void SubArrayViewDisposeIsNoOp()
        {
            var arr = new NativeArray<int>(100, Allocator.TempJob);
            for (int i = 0; i < 100; i++) arr[i] = i;
            var view = arr.GetSubArray(10, 20);
            view.Dispose();
            TestRunner.Assert(arr[11] == 11, "view Dispose is a no-op (parent still valid)");
            arr.Dispose();
        }


        [Test]
        public static void ScheduleParallelWithNativeArrayOverload()
        {
            const int n = 8192;
            var arr = new NativeArray<int>(n, Allocator.TempJob);
            try
            {
                var seen = new int[n];
                var h2 = JobSystem.ScheduleParallel(new BatchJob(seen), arr, 256);
                h2.Complete();
                TestRunner.Assert(seen.All(v => v == 1), "ScheduleParallel(job, NativeArray, batch) overload still works");
            }
            finally { arr.Dispose(); }
        }
        private struct BatchJob : IJobParallelForBatch
        {
            private readonly int[] _seen;
            public BatchJob(int[] seen) { _seen = seen; }
            public void Execute(int startIndex, int count)
            {
                for (int i = startIndex; i < startIndex + count; i++) Interlocked.Increment(ref _seen[i]);
            }
        }
    }
}
