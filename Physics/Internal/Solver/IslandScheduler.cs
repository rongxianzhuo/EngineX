using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics.Internal.Dynamics;

namespace EngineX.Physics.Internal.Solver
{
    internal static class IslandScheduler
    {
        public const int MaxIslands = 64;

        public static int ScheduleBySizeDesc(Island[] islands, int count, int[] order)
        {
            for (int i = 0; i < count; i++) order[i] = i;

            for (int i = 1; i < count; i++)
            {
                int key = order[i];
                int j = i - 1;
                while (j >= 0 && islands[order[j]].BodyCount < islands[key].BodyCount)
                {
                    order[j + 1] = order[j];
                    j--;
                }
                order[j + 1] = key;
            }

            return count;
        }
    }
}