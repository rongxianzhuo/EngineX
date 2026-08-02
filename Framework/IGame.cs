using EngineX.ECS;

namespace EngineX.Framework
{

    public interface IGame
    {

        World Create();
        void Update();
        void Destroy();

    }

}
