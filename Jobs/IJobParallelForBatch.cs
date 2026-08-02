namespace EngineX.Jobs
{
    public interface IJobParallelForBatch
    {
        void Execute(int startIndex, int count);
    }
}