using System;
using EngineX.Baseline.FixedPoint;

namespace EngineX.Baseline.Math
{
    /// <summary>
    /// Represents a three-dimensional vector using <see cref="FP"/> fixed-point numbers.
    /// All operations are deterministic across platforms.
    /// </summary>
    public readonly partial struct Vector3 : IEquatable<Vector3>
    {

        /// <summary>X component.</summary>
        public readonly FP X;

        /// <summary>Y component.</summary>
        public readonly FP Y;

        /// <summary>Z component.</summary>
        public readonly FP Z;

        /// <summary>Zero vector (0, 0, 0).</summary>
        public static readonly Vector3 Zero = new(FP.Zero, FP.Zero, FP.Zero);

        /// <summary>One vector (1, 1, 1).</summary>
        public static readonly Vector3 One = new(FP.One, FP.One, FP.One);

        /// <summary>Forward direction (0, 0, 1) — right-handed, Z-forward.</summary>
        public static readonly Vector3 Forward = new(FP.Zero, FP.Zero, FP.One);

        /// <summary>Back direction (0, 0, -1).</summary>
        public static readonly Vector3 Back = new(FP.Zero, FP.Zero, -1);

        /// <summary>Up direction (0, 1, 0).</summary>
        public static readonly Vector3 Up = new(FP.Zero, FP.One, FP.Zero);

        /// <summary>Down direction (0, -1, 0).</summary>
        public static readonly Vector3 Down = new(FP.Zero, -1, FP.Zero);

        /// <summary>Right direction (1, 0, 0).</summary>
        public static readonly Vector3 Right = new(FP.One, FP.Zero, FP.Zero);

        /// <summary>Left direction (-1, 0, 0).</summary>
        public static readonly Vector3 Left = new(-1, FP.Zero, FP.Zero);

        /// <summary>
        /// Creates a new <see cref="Vector3"/> with the given components.
        /// </summary>
        public Vector3(FP x, FP y, FP z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Returns the length (magnitude) of this vector.
        /// Computed as Sqrt(X*X + Y*Y + Z*Z).
        /// </summary>
        public FP Magnitude => SqrMagnitude.Sqrt;

        /// <summary>
        /// Returns the squared length (X*X + Y*Y + Z*Z). Faster than <see cref="Magnitude"/>
        /// because it avoids a square root.
        /// </summary>
        public FP SqrMagnitude => X * X + Y * Y + Z * Z;

        /// <summary>
        /// Returns a normalized copy of this vector (length = 1).
        /// Returns <see cref="Zero"/> if the vector is zero.
        /// </summary>
        public Vector3 Normalized
        {
            get
            {
                FP sqrMag = SqrMagnitude;
                if (sqrMag == FP.Zero)
                    return Zero;
                FP invMag = FP.One / sqrMag.Sqrt;
                return new Vector3(X * invMag, Y * invMag, Z * invMag);
            }
        }

        /// <summary>Component-wise addition.</summary>
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        /// <summary>Component-wise subtraction.</summary>
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        /// <summary>Unary negation (reverses direction).</summary>
        public static Vector3 operator -(Vector3 v) => new(-v.X, -v.Y, -v.Z);

        /// <summary>Scalar multiplication (vector * scalar).</summary>
        public static Vector3 operator *(Vector3 v, FP scalar) => new(v.X * scalar, v.Y * scalar, v.Z * scalar);

        /// <summary>Scalar multiplication (scalar * vector).</summary>
        public static Vector3 operator *(FP scalar, Vector3 v) => new(v.X * scalar, v.Y * scalar, v.Z * scalar);

        /// <summary>Scalar division (vector / scalar).</summary>
        public static Vector3 operator /(Vector3 v, FP scalar) => new(v.X / scalar, v.Y / scalar, v.Z / scalar);

        /// <summary>Component-wise equality.</summary>
        public static bool operator ==(Vector3 a, Vector3 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;

        /// <summary>Component-wise inequality.</summary>
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);

        /// <summary>
        /// Returns the dot product of this vector and <paramref name="other"/>.
        /// </summary>
        public FP Dot(Vector3 other) => X * other.X + Y * other.Y + Z * other.Z;

        /// <summary>
        /// Returns the dot product of <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        public static FP Dot(Vector3 a, Vector3 b) => a.Dot(b);

        /// <summary>
        /// Returns the cross product of <paramref name="a"/> and <paramref name="b"/>.
        /// The result is perpendicular to both input vectors.
        /// </summary>
        public static Vector3 Cross(Vector3 a, Vector3 b)
        {
            return new Vector3(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X);
        }

        /// <summary>
        /// Returns the Euclidean distance between two points.
        /// </summary>
        public static FP Distance(Vector3 a, Vector3 b) => (a - b).Magnitude;

        /// <summary>
        /// Returns the squared Euclidean distance between two points.
        /// Faster than <see cref="Distance"/> because it avoids a square root.
        /// </summary>
        public static FP SqrDistance(Vector3 a, Vector3 b) => (a - b).SqrMagnitude;

        /// <summary>
        /// Returns the angle in degrees between <paramref name="from"/> and <paramref name="to"/>.
        /// Result is always in [0, 180] degrees.
        /// </summary>
        public static FP Angle(Vector3 from, Vector3 to)
        {
            FP denominator = (from.SqrMagnitude * to.SqrMagnitude).Sqrt;
            if (denominator == FP.Zero)
                return FP.Zero;

            FP cosAngle = FP.Clamp(Dot(from, to) / denominator, -1, FP.One);
            return FpMath.Acos(cosAngle) * FP.Rad2Deg;
        }

        /// <summary>
        /// Linearly interpolates between two vectors. <paramref name="t"/> is clamped to [0, 1].
        /// </summary>
        public static Vector3 Lerp(Vector3 a, Vector3 b, FP t)
        {
            t = FP.Clamp01(t);
            return LerpUnclamped(a, b, t);
        }

        /// <summary>
        /// Linearly interpolates between two vectors without clamping <paramref name="t"/>.
        /// </summary>
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, FP t)
        {
            return new Vector3(
                FP.LerpUnclamped(a.X, b.X, t),
                FP.LerpUnclamped(a.Y, b.Y, t),
                FP.LerpUnclamped(a.Z, b.Z, t));
        }

        /// <summary>
        /// Moves <paramref name="current"/> towards <paramref name="target"/> by at most
        /// <paramref name="maxDistanceDelta"/>. Won't overshoot.
        /// </summary>
        public static Vector3 MoveTowards(Vector3 current, Vector3 target, FP maxDistanceDelta)
        {
            Vector3 diff = target - current;
            FP sqrDist = diff.SqrMagnitude;

            if (sqrDist == FP.Zero || (maxDistanceDelta >= FP.Zero && sqrDist <= maxDistanceDelta * maxDistanceDelta))
                return target;

            FP dist = sqrDist.Sqrt;
            return current + diff / dist * maxDistanceDelta;
        }

        /// <summary>
        /// Reflects <paramref name="inDirection"/> off a surface with the given <paramref name="inNormal"/>.
        /// The normal should be normalized.
        /// </summary>
        public static Vector3 Reflect(Vector3 inDirection, Vector3 inNormal)
        {
            FP dot = Dot(inDirection, inNormal);
            return inDirection - 2 * dot * inNormal;
        }

        /// <summary>
        /// Projects <paramref name="vector"/> onto <paramref name="onNormal"/>.
        /// The normal should be normalized.
        /// </summary>
        public static Vector3 Project(Vector3 vector, Vector3 onNormal)
        {
            FP sqrMag = onNormal.SqrMagnitude;
            if (sqrMag == FP.Zero)
                return Zero;
            FP dot = Dot(vector, onNormal);
            return onNormal * (dot / sqrMag);
        }

        /// <summary>
        /// Projects <paramref name="vector"/> onto the plane defined by <paramref name="planeNormal"/>.
        /// The normal should be normalized.
        /// </summary>
        public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal)
        {
            return vector - Project(vector, planeNormal);
        }

        /// <summary>
        /// Spherically interpolates between two normalized direction vectors.
        /// <paramref name="t"/> is clamped to [0, 1].
        /// </summary>
        public static Vector3 Slerp(Vector3 a, Vector3 b, FP t)
        {
            t = FP.Clamp01(t);
            return SlerpUnclamped(a, b, t);
        }

        /// <summary>
        /// Spherically interpolates between two normalized direction vectors
        /// without clamping <paramref name="t"/>.
        /// </summary>
        public static Vector3 SlerpUnclamped(Vector3 a, Vector3 b, FP t)
        {
            // Normalize inputs for robust spherical interpolation
            Vector3 aNorm = a.Normalized;
            Vector3 bNorm = b.Normalized;

            if (aNorm == Zero || bNorm == Zero)
                return LerpUnclamped(aNorm, bNorm, t);

            FP dot = FP.Clamp(Dot(aNorm, bNorm), -1, FP.One);
            FP angleRad = FpMath.Acos(dot); // radians

            if (angleRad.Abs() < FP.Epsilon * FP.FromInt(100))
                return LerpUnclamped(aNorm, bNorm, t);

            FP sinAngle = FpMath.Sin(angleRad);
            FP invSinAngle = FP.One / sinAngle;

            FP weightA = FpMath.Sin((FP.One - t) * angleRad) * invSinAngle;
            FP weightB = FpMath.Sin(t * angleRad) * invSinAngle;

            return aNorm * weightA + bNorm * weightB;
        }

        /// <summary>
        /// Rotates <paramref name="current"/> towards <paramref name="target"/> by at most
        /// <paramref name="maxRadiansDelta"/> radians. Also moves the magnitude towards
        /// the target by at most <paramref name="maxMagnitudeDelta"/>.
        /// </summary>
        public static Vector3 RotateTowards(Vector3 current, Vector3 target, FP maxRadiansDelta, FP maxMagnitudeDelta)
        {
            FP curMag = current.Magnitude;
            FP targetMag = target.Magnitude;

            // Handle zero-length vectors
            if (curMag == FP.Zero || targetMag == FP.Zero)
                return MoveTowards(current, target, maxMagnitudeDelta);

            Vector3 curNorm = current / curMag;
            Vector3 targetNorm = target / targetMag;

            FP dot = FP.Clamp(Dot(curNorm, targetNorm), -1, FP.One);
            FP angleRad = FpMath.Acos(dot);

            // If within the allowed angle delta, snap to the target direction
            Vector3 newDir;
            if (angleRad <= maxRadiansDelta)
            {
                newDir = targetNorm;
            }
            else
            {
                // Slerp towards target by maxRadiansDelta / angleRad fraction
                FP t = maxRadiansDelta / angleRad;
                FP sinAngle = FpMath.Sin(angleRad);
                FP invSinAngle = FP.One / sinAngle;
                FP weightA = FpMath.Sin((FP.One - t) * angleRad) * invSinAngle;
                FP weightB = FpMath.Sin(t * angleRad) * invSinAngle;
                newDir = (curNorm * weightA + targetNorm * weightB).Normalized;
            }

            // Move magnitude towards target
            FP newMag = MoveTowardsScalar(curMag, targetMag, maxMagnitudeDelta);
            return newDir * newMag;
        }

        /// <summary>
        /// Returns a vector where each component is the maximum of the corresponding
        /// components of <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        public static Vector3 Max(Vector3 a, Vector3 b)
        {
            return new Vector3(FP.Max(a.X, b.X), FP.Max(a.Y, b.Y), FP.Max(a.Z, b.Z));
        }

        /// <summary>
        /// Returns a vector where each component is the minimum of the corresponding
        /// components of <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        public static Vector3 Min(Vector3 a, Vector3 b)
        {
            return new Vector3(FP.Min(a.X, b.X), FP.Min(a.Y, b.Y), FP.Min(a.Z, b.Z));
        }

        private static FP MoveTowardsScalar(FP current, FP target, FP maxDelta)
        {
            FP diff = target - current;
            FP absDiff = diff.Abs();
            if (absDiff <= maxDelta || absDiff == FP.Zero)
                return target;
            return current + diff.Sign() * maxDelta;
        }

        /// <inheritdoc />
        public bool Equals(Vector3 other) => this == other;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Vector3 other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);

        /// <summary>
        /// Returns a string representation of this vector in the format "(X, Y, Z)".
        /// Each component uses 6 decimal places, allowing lossless round-trip via <see cref="Parse"/>.
        /// </summary>
        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}