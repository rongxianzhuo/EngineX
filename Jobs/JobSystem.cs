using System;

namespace EngineX.Jobs
{
    public static class JobSystem
    {
        public static int WorkerThreadCount => EngineX.Jobs.Internal.JobScheduler.WorkerCount;

        public static int MaxJobThreadCount => EngineX.Jobs.Internal.JobScheduler.MaxWorkerCount;

        public static JobHandle Schedule(IJob job, JobHandle dependsOn = default)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            return EngineX.Jobs.Internal.JobScheduler.Schedule(job.Execute, dependsOn);
        }

        public static JobHandle ScheduleParallel(IJobParallelFor job, int arrayLength, int innerLoopBatchCount = 1, JobHandle dependsOn = default)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (arrayLength < 0) throw new ArgumentOutOfRangeException(nameof(arrayLength));
            if (innerLoopBatchCount < 1) innerLoopBatchCount = 1;
            return EngineX.Jobs.Internal.JobScheduler.ScheduleParallelFor(job.Execute, arrayLength, dependsOn);
        }

        public static JobHandle ScheduleParallel<T>(IJobParallelForBatch job, NativeArray<T> array, int innerLoopBatchCount = 1, JobHandle dependsOn = default) where T : struct
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (innerLoopBatchCount < 1) innerLoopBatchCount = 1;
            if (!array.IsCreated) throw new ArgumentException("NativeArray is not allocated", nameof(array));
            return EngineX.Jobs.Internal.JobScheduler.ScheduleParallelForBatch(job.Execute, array.Length, dependsOn);
        }
    }
}