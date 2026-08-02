using EngineX.Baseline.FixedPoint;

namespace EngineX.ECS.Components
{
    public enum CameraProjection
    {
        Perspective,

        Orthographic,
    }

    public struct CameraData : IComponentData
    {
        public FP Fov;

        public FP NearClip;

        public FP FarClip;

        public CameraProjection Projection;

        public static readonly CameraData Default = new CameraData(
            FP.FromInt(60),
            FP.FromFloat(0.3f),
            FP.FromInt(1000),
            CameraProjection.Perspective);

        public CameraData(FP fov)
            : this(fov, FP.FromFloat(0.3f), FP.FromInt(1000), CameraProjection.Perspective)
        {
        }

        public CameraData(FP fov, FP nearClip, FP farClip, CameraProjection projection)
        {
            Fov = fov;
            NearClip = nearClip;
            FarClip = farClip;
            Projection = projection;
        }
    }
}
