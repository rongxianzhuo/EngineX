using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.ECS.Components
{
    public enum CameraProjection
    {
        Perspective,

        Orthographic,
    }

    public struct CameraData : IComponentData
    {
        public Vector3 Position;

        public Quaternion Rotation;

        public FP Fov;

        public FP NearClip;

        public FP FarClip;

        public CameraProjection Projection;

        public static readonly CameraData Default = new CameraData(
            Vector3.Zero,
            Quaternion.Identity,
            FP.FromInt(60),
            FP.FromFloat(0.3f),
            FP.FromInt(1000),
            CameraProjection.Perspective);

        public CameraData(Vector3 position, Quaternion rotation)
            : this(position, rotation, FP.FromInt(60), FP.FromFloat(0.3f), FP.FromInt(1000), CameraProjection.Perspective)
        {
        }

        public CameraData(Vector3 position, Quaternion rotation, FP fov)
            : this(position, rotation, fov, FP.FromFloat(0.3f), FP.FromInt(1000), CameraProjection.Perspective)
        {
        }

        public CameraData(Vector3 position, Quaternion rotation, FP fov, FP nearClip, FP farClip, CameraProjection projection)
        {
            Position = position;
            Rotation = rotation;
            Fov = fov;
            NearClip = nearClip;
            FarClip = farClip;
            Projection = projection;
        }

        public static CameraData FromEuler(Vector3 position, Vector3 eulerAngles, FP fov)
        {
            return new CameraData(position, Quaternion.Euler(eulerAngles.X, eulerAngles.Y, eulerAngles.Z), fov);
        }

        public Vector3 EulerAngles => Rotation.EulerAngles;
    }
}
