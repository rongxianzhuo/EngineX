using System;
using EngineX.Baseline.FixedPoint;

namespace EngineX.Baseline.Math
{
    public readonly struct Vector2 : IEquatable<Vector2>
    {
        public readonly FP X;

        public readonly FP Y;

        // Small-angle threshold (in radians) below which spherical interpolation
        // falls back to linear interpolation to avoid divide-by-near-zero.
        private static readonly FP AngleEpsilon = FP.Epsilon * FP.FromInt(100);

        public static readonly Vector2 Zero = new(FP.Zero, FP.Zero);

        public static readonly Vector2 One = new(FP.One, FP.One);

        public static readonly Vector2 Up = new(FP.Zero, FP.One);

        public static readonly Vector2 Down = new(FP.Zero, -FP.One);

        public static readonly Vector2 Left = new(-FP.One, FP.Zero);

        public static readonly Vector2 Right = new(FP.One, FP.Zero);

        public Vector2(FP x, FP y)
        {
            X = x;
            Y = y;
        }

        public FP Magnitude => SqrMagnitude.Sqrt;

        public FP SqrMagnitude => X * X + Y * Y;

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

        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);

        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);

        public static Vector2 operator -(Vector2 v) => new(-v.X, -v.Y);

        public static Vector2 operator *(Vector2 v, FP scalar) => new(v.X * scalar, v.Y * scalar);

        public static Vector2 operator *(FP scalar, Vector2 v) => new(v.X * scalar, v.Y * scalar);

        public static Vector2 operator /(Vector2 v, FP scalar) => new(v.X / scalar, v.Y / scalar);

        public static bool operator ==(Vector2 a, Vector2 b) => a.X == b.X && a.Y == b.Y;

        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);

        public FP Dot(Vector2 other) => X * other.X + Y * other.Y;

        public static FP Dot(Vector2 a, Vector2 b) => a.Dot(b);

        public static FP Distance(Vector2 a, Vector2 b) => (a - b).Magnitude;

        public static FP SqrDistance(Vector2 a, Vector2 b) => (a - b).SqrMagnitude;

        public static FP Angle(Vector2 from, Vector2 to)
        {
            FP denominator = (from.SqrMagnitude * to.SqrMagnitude).Sqrt;
            if (denominator == FP.Zero)
                return FP.Zero;

            FP cosAngle = FP.Clamp(Dot(from, to) / denominator, -1, FP.One);
            return FpMath.Acos(cosAngle) * FP.Rad2Deg;
        }

        public static Vector2 Lerp(Vector2 a, Vector2 b, FP t)
        {
            t = FP.Clamp01(t);
            return LerpUnclamped(a, b, t);
        }

        public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, FP t)
        {
            return new Vector2(
                FP.LerpUnclamped(a.X, b.X, t),
                FP.LerpUnclamped(a.Y, b.Y, t));
        }

        public static Vector2 MoveTowards(Vector2 current, Vector2 target, FP maxDistanceDelta)
        {
            Vector2 diff = target - current;
            FP sqrDist = diff.SqrMagnitude;

            if (sqrDist == FP.Zero || (maxDistanceDelta >= FP.Zero && sqrDist <= maxDistanceDelta * maxDistanceDelta))
                return target;

            FP dist = sqrDist.Sqrt;
            return current + diff / dist * maxDistanceDelta;
        }

        public static Vector2 Reflect(Vector2 inDirection, Vector2 inNormal)
        {
            FP dot = Dot(inDirection, inNormal);
            return inDirection - 2 * dot * inNormal;
        }

        public static Vector2 Project(Vector2 vector, Vector2 onNormal)
        {
            FP sqrMag = onNormal.SqrMagnitude;
            if (sqrMag == FP.Zero)
                return Zero;
            FP dot = Dot(vector, onNormal);
            return onNormal * (dot / sqrMag);
        }

        public static Vector2 ProjectOnPlane(Vector2 vector, Vector2 planeNormal)
        {
            return vector - Project(vector, planeNormal);
        }

        public static Vector2 Slerp(Vector2 a, Vector2 b, FP t)
        {
            t = FP.Clamp01(t);
            return SlerpUnclamped(a, b, t);
        }

        public static Vector2 SlerpUnclamped(Vector2 a, Vector2 b, FP t)
        {
            Vector2 aNorm = a.Normalized;
            Vector2 bNorm = b.Normalized;

            if (aNorm == Zero || bNorm == Zero)
                return LerpUnclamped(aNorm, bNorm, t);

            FP dot = FP.Clamp(Dot(aNorm, bNorm), -1, FP.One);
            FP angleRad = FpMath.Acos(dot);

            if (angleRad.Abs() < AngleEpsilon)
                return LerpUnclamped(aNorm, bNorm, t);

            FP sinAngle = FpMath.Sin(angleRad);
            FP invSinAngle = FP.One / sinAngle;

            FP weightA = FpMath.Sin((FP.One - t) * angleRad) * invSinAngle;
            FP weightB = FpMath.Sin(t * angleRad) * invSinAngle;

            return (aNorm * weightA + bNorm * weightB).Normalized;
        }

        public static Vector2 RotateTowards(Vector2 current, Vector2 target, FP maxRadiansDelta, FP maxMagnitudeDelta)
        {
            FP curMag = current.Magnitude;
            FP targetMag = target.Magnitude;

            if (curMag == FP.Zero || targetMag == FP.Zero)
                return MoveTowards(current, target, maxMagnitudeDelta);

            Vector2 curNorm = current / curMag;
            Vector2 targetNorm = target / targetMag;

            FP dot = FP.Clamp(Dot(curNorm, targetNorm), -1, FP.One);
            FP angleRad = FpMath.Acos(dot);

            Vector2 newDir;
            if (angleRad <= maxRadiansDelta)
            {
                newDir = targetNorm;
            }
            else
            {
                FP t = maxRadiansDelta / angleRad;
                FP sinAngle = FpMath.Sin(angleRad);
                FP invSinAngle = FP.One / sinAngle;
                FP weightA = FpMath.Sin((FP.One - t) * angleRad) * invSinAngle;
                FP weightB = FpMath.Sin(t * angleRad) * invSinAngle;
                newDir = (curNorm * weightA + targetNorm * weightB).Normalized;
            }

            FP newMag = MoveTowardsScalar(curMag, targetMag, maxMagnitudeDelta);
            return newDir * newMag;
        }

        private static FP MoveTowardsScalar(FP current, FP target, FP maxDelta)
        {
            FP diff = target - current;
            FP absDiff = diff.Abs();
            if (absDiff <= maxDelta || absDiff == FP.Zero)
                return target;
            return current + diff.Sign() * maxDelta;
        }

        public static Vector2 Max(Vector2 a, Vector2 b)
        {
            return new Vector2(FP.Max(a.X, b.X), FP.Max(a.Y, b.Y));
        }

        public static Vector2 Min(Vector2 a, Vector2 b)
        {
            return new Vector2(FP.Min(a.X, b.X), FP.Min(a.Y, b.Y));
        }

        public bool Equals(Vector2 other) => this == other;

        public override bool Equals(object? obj) => obj is Vector2 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
