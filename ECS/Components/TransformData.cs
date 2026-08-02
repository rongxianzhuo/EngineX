using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;

namespace EngineX.ECS.Components
{
    public struct TransformData : IComponentData
    {
        public Vector3 Position;

        public Quaternion Rotation;

        public Vector3 Scale;

        public static readonly TransformData Identity = new TransformData(Vector3.Zero, Quaternion.Identity, Vector3.One);

        public TransformData(Vector3 position)
        {
            Position = position;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
        }

        public TransformData(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
            Scale = Vector3.One;
        }

        public TransformData(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public static TransformData FromEuler(Vector3 position, Vector3 eulerAngles)
        {
            return new TransformData(position, Quaternion.Euler(eulerAngles.X, eulerAngles.Y, eulerAngles.Z));
        }

        public static TransformData FromEuler(Vector3 position, Vector3 eulerAngles, Vector3 scale)
        {
            return new TransformData(position, Quaternion.Euler(eulerAngles.X, eulerAngles.Y, eulerAngles.Z), scale);
        }

        public Vector3 EulerAngles => Rotation.EulerAngles;

        public TransformData WithPosition(Vector3 position)
        {
            return new TransformData(position, Rotation, Scale);
        }

        public TransformData WithRotation(Quaternion rotation)
        {
            return new TransformData(Position, rotation, Scale);
        }

        public TransformData WithScale(Vector3 scale)
        {
            return new TransformData(Position, Rotation, scale);
        }

        public Vector3 TransformPoint(Vector3 point)
        {
            var scaled = new Vector3(point.X * Scale.X, point.Y * Scale.Y, point.Z * Scale.Z);
            return Position + Rotation * scaled;
        }

        public Vector3 TransformDirection(Vector3 direction)
        {
            return Rotation * direction;
        }
    }
}
