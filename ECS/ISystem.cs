using EngineX.Jobs;

namespace EngineX.ECS
{
    public interface ISystem
    {
        void OnCreate(ref SystemState state);

        void OnUpdate(ref SystemState state);

        void OnDestroy(ref SystemState state);
    }

    public struct SystemState
    {
        public World World;

        public JobHandle Dependency;
    }
}
