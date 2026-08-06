using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics.Internal.Distance;

namespace EngineX.Physics
{
    public static class ShapeDistance
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Sphere a, Sphere b)
            => SphereSphere.Compute(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Sphere a, Box b)
            => SphereBox.Compute(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Box a, Sphere b)
            => SphereBox.Compute(b, a).Swapped;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Sphere a, Capsule b)
            => SphereCapsule.Compute(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Capsule a, Sphere b)
            => SphereCapsule.Compute(b, a).Swapped;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Sphere a, Plane b)
            => SpherePlane.Compute(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Plane a, Sphere b)
            => SpherePlane.Compute(b, a).Swapped;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Box a, Box b)
            => BoxBox.Compute(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Box a, Capsule b)
            => BoxCapsule.Compute(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Capsule a, Box b)
            => BoxCapsule.Compute(b, a).Swapped;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Box a, Plane b)
            => BoxPlane.Compute(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Plane a, Box b)
            => BoxPlane.Compute(b, a).Swapped;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Capsule a, Capsule b)
            => CapsuleCapsule.Compute(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Capsule a, Plane b)
            => CapsulePlane.Compute(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DistanceResult Compute(Plane a, Capsule b)
            => CapsulePlane.Compute(b, a).Swapped;
    }
}
