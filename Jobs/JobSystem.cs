using System;

namespace EngineX.Jobs
{
    public static class JobSystem
    {
        public static int WorkerThreadCount
        {
            get
            {
                EngineX.Jobs.Internal.JobScheduler.EnsureInitialized();
                return EngineX.Jobs.Internal.JobScheduler.WorkerCount;
            }
        }

        public static int MaxJobThreadCount => EngineX.Jobs.Internal.JobScheduler.MaxJobThreadCount;

        // ---- IJob ----

        public static JobHandle Schedule<T>(T job, JobHandle dependsOn = default) where T : struct, IJob
        {
            return EngineX.Jobs.Internal.JobScheduler.Schedule(job, dependsOn);
        }

        public static JobHandle Schedule(IJob job, JobHandle dependsOn = default)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            return EngineX.Jobs.Internal.JobScheduler.Schedule(job.Execute, dependsOn);
        }

        // ---- IJobParallelFor ----

        public static JobHandle ScheduleParallel<T>(T job, int arrayLength, int innerLoopBatchCount = 1, JobHandle dependsOn = default) where T : struct, IJobParallelFor
        {
            if (arrayLength < 0) throw new ArgumentOutOfRangeException(nameof(arrayLength));
            if (innerLoopBatchCount < 1) innerLoopBatchCount = 1;
            return EngineX.Jobs.Internal.JobScheduler.ScheduleParallelFor(job, arrayLength, innerLoopBatchCount, dependsOn);
        }

        public static JobHandle ScheduleParallel(IJobParallelFor job, int arrayLength, int innerLoopBatchCount = 1, JobHandle dependsOn = default)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (arrayLength < 0) throw new ArgumentOutOfRangeException(nameof(arrayLength));
            if (innerLoopBatchCount < 1) innerLoopBatchCount = 1;
            return EngineX.Jobs.Internal.JobScheduler.ScheduleParallelFor(job.Execute, arrayLength, innerLoopBatchCount, dependsOn);
        }

        // ---- IJobParallelForBatch + NativeArray ----

        public static JobHandle ScheduleParallel<TJob, TArray>(TJob job, NativeArray<TArray> array, int innerLoopBatchCount = 1, JobHandle dependsOn = default)
            where TJob : struct, IJobParallelForBatch
            where TArray : unmanaged
        {
            if (innerLoopBatchCount < 1) innerLoopBatchCount = 1;
            return EngineX.Jobs.Internal.JobScheduler.ScheduleParallelForBatch(job, array.Length, innerLoopBatchCount, dependsOn);
        }

        public static JobHandle ScheduleParallel<TArray>(IJobParallelForBatch job, NativeArray<TArray> array, int innerLoopBatchCount = 1, JobHandle dependsOn = default)
            where TArray : unmanaged
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (innerLoopBatchCount < 1) innerLoopBatchCount = 1;
            return EngineX.Jobs.Internal.JobScheduler.ScheduleParallelForBatch(job.Execute, array.Length, innerLoopBatchCount, dependsOn);
        }
    }
}
