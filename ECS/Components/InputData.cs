using EngineX.Baseline.FixedPoint;

namespace EngineX.ECS.Components
{
    public struct InputData : IComponentData
    {
        public FP Horizontal;

        public FP Vertical;
    }
}
