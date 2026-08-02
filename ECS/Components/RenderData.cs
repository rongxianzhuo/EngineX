namespace EngineX.ECS.Components
{
    public struct RenderData : IComponentData
    {
        public string MeshPath;

        public string MaterialPath;

        public RenderData(string meshPath, string materialPath)
        {
            MeshPath = meshPath;
            MaterialPath = materialPath;
        }
    }
}
