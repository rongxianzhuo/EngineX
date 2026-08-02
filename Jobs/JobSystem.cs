using System;

namespace EngineX.Jobs
{
    public static class JobSystem
    {
        public static int WorkerThreadCount => 0;

        public static int MaxJobThreadCount => Math.Max(1, Environment.ProcessorCount - 1);

        public static JobHandle Schedule(IJob job, JobHandle dependsOn = default)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            dependsOn.Complete();
            job.Execute();
            return default;
        }

        public static JobHandle ScheduleParallel(IJobParallelFor job, int arrayLength, int innerLoopBatchCount = 1, JobHandle dependsOn = default)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (arrayLength < 0) throw new ArgumentOutOfRangeException(nameof(arrayLength));
            if (innerLoopBatchCount < 1) innerLoopBatchCount = 1;
            dependsOn.Complete();
            for (int i = 0; i < arrayLength; i++)
            {
                job.Execute(i);
            }
            return default;
        }
    }
}