using System;
using EngineX.Baseline.FixedPoint;

namespace EngineX.Foundation.Math
{
    public readonly struct Vector2 : IEquatable<Vector2>
    {
        public readonly FP X;
        
        public readonly FP Y;

        /// <summary>Zero vector (0, 0).</summary>
        public static readonly Vector2 Zero = new(FP.Zero, FP.Zero);

        /// <summary>One vector (1, 1).</summary>
        public static readonly Vector2 One = new(FP.One, FP.One);

        /// <summary>Up direction (0, 1).</summary>
        public static readonly Vector2 Up = new(FP.Zero, FP.One);

        /// <summary>Down direction (0, -1).</summary>
        public static readonly Vector2 Down = new(FP.Zero, -1);

        /// <summary>Left direction (-1, 0).</summary>
        public static readonly Vector2 Left = new(-1, 0);

        /// <summary>Right direction (1, 0).</summary>
        public static readonly Vector2 Right = new(FP.One, FP.Zero);
        
        /// <summary>
        /// Creates a new <see cref="Vector2"/> with the given components.
        /// </summary>
        public Vector2(FP x, FP y)
        {
            X = x;
            Y = y;
        }
        
        /// <summary>
        /// Returns the length (magnitude) of this vector.
        /// Computed as Sqrt(X*X + Y*Y).
        /// </summary>
        public FP Magnitude => SqrMagnitude.Sqrt;

        /// <summary>
        /// Returns the squared length (X*X + Y*Y). Faster than <see cref="Magnitude"/>
        /// because it avoids a square root.
        /// </summary>
        public FP SqrMagnitude => X * X + Y * Y;

        /// <summary>
        /// Returns a normalized copy of this vector (length = 1).
        /// Returns <see cref="Zero"/> if the vector is zero.
        /// </summary>
        public Vector2 Normalized
        {
            get
            {
                FP sqrMag = SqrMagnitude;
                if (sqrMag == FP.Zero)
                    return Zero;
                FP invMag = FP.One / SqrMagnitude.Sqrt;
                return new Vector2(X * invMag, Y * invMag);
            }
        }

        /// <summary>Component-wise addition.</summary>
        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);

        /// <summary>Component-wise subtraction.</summary>
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);

        /// <summary>Unary negation (reverses direction).</summary>
        public static Vector2 operator -(Vector2 v) => new(-v.X, -v.Y);

        /// <summary>Scalar multiplication (vector * scalar).</summary>
        public static Vector2 operator *(Vector2 v, FP scalar) => new(v.X * scalar, v.Y * scalar);

        /// <summary>Scalar multiplication (scalar * vector).</summary>
        public static Vector2 operator *(FP scalar, Vector2 v) => new(v.X * scalar, v.Y * scalar);

        /// <summary>Scalar division (vector / scalar).</summary>
        public static Vector2 operator /(Vector2 v, FP scalar) => new(v.X / scalar, v.Y / scalar);

        /// <summary>Component-wise equality.</summary>
        public static bool operator ==(Vector2 a, Vector2 b) => a.X == b.X && a.Y == b.Y;

        /// <summary>Component-wise inequality.</summary>
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);

        /// <summary>
        /// Returns the dot product of this vector and <paramref name="other"/>.
        /// </summary>
        public FP Dot(Vector2 other) => X * other.X + Y * other.Y;

        /// <summary>
        /// Returns the dot product of <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        public static FP Dot(Vector2 a, Vector2 b) => a.Dot(b);

        /// <summary>
        /// Returns the Euclidean distance between two points.
        /// </summary>
        public static FP Distance(Vector2 a, Vector2 b) => (a - b).Magnitude;

        /// <summary>
        /// Returns the squared Euclidean distance between two points.
        /// Faster than <see cref="Distance"/> because it avoids a square root.
        /// </summary>
        public static FP SqrDistance(Vector2 a, Vector2 b) => (a - b).SqrMagnitude;

        /// <summary>
        /// Returns the angle in degrees between <paramref name="from"/> and <paramref name="to"/>.
        /// Result is always in [0, 180] degrees.
        /// </summary>
        public static FP Angle(Vector2 from, Vector2 to)
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
        public static Vector2 Lerp(Vector2 a, Vector2 b, FP t)
        {
            t = FP.Clamp01(t);
            return LerpUnclamped(a, b, t);
        }

        /// <summary>
        /// Linearly interpolates between two vectors without clamping <paramref name="t"/>.
        /// </summary>
        public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, FP t)
        {
            return new Vector2(
                FP.LerpUnclamped(a.X, b.X, t),
                FP.LerpUnclamped(a.Y, b.Y, t));
        }

        /// <summary>
        /// Moves <paramref name="current"/> towards <paramref name="target"/> by at most
        /// <paramref name="maxDistanceDelta"/>. Won't overshoot.
        /// </summary>
        public static Vector2 MoveTowards(Vector2 current, Vector2 target, FP maxDistanceDelta)
        {
            Vector2 diff = target - current;
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
        public static Vector2 Reflect(Vector2 inDirection, Vector2 inNormal)
        {
            FP dot = Dot(inDirection, inNormal);
            return inDirection - 2 * dot * inNormal;
        }

        /// <summary>
        /// Returns a vector where each component is the maximum of the corresponding
        /// components of <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        public static Vector2 Max(Vector2 a, Vector2 b)
        {
            return new Vector2(FP.Max(a.X, b.X), FP.Max(a.Y, b.Y));
        }

        /// <summary>
        /// Returns a vector where each component is the minimum of the corresponding
        /// components of <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        public static Vector2 Min(Vector2 a, Vector2 b)
        {
            return new Vector2(FP.Min(a.X, b.X), FP.Min(a.Y, b.Y));
        }

        /// <inheritdoc />
        public bool Equals(Vector2 other) => this == other;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Vector2 other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(X, Y);

        /// <summary>
        /// Returns a string representation of this vector in the format "(X, Y)".
        /// Each component uses 6 decimal places, allowing lossless round-trip via <see cref="Parse"/>.
        /// </summary>
        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}