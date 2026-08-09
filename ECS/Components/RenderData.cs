namespace EngineX.ECS.Components
{
    public struct RenderData : IComponentData
    {
        public long MeshAssetId;

        public long MaterialAssetId;

        public RenderData(long meshAssetId, long materialAssetId)
        {
            MeshAssetId = meshAssetId;
            MaterialAssetId = materialAssetId;
        }
    }
}
