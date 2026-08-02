using System;
using EngineX.Baseline.FixedPoint;

namespace EngineX.Baseline.Math
{
    public readonly struct Vector3 : IEquatable<Vector3>
    {

        public readonly FP X;

        public readonly FP Y;

        public readonly FP Z;

        public static readonly Vector3 Zero = new(FP.Zero, FP.Zero, FP.Zero);

        public static readonly Vector3 One = new(FP.One, FP.One, FP.One);

        public static readonly Vector3 Forward = new(FP.Zero, FP.Zero, FP.One);

        public static readonly Vector3 Back = new(FP.Zero, FP.Zero, -1);

        public static readonly Vector3 Up = new(FP.Zero, FP.One, FP.Zero);

        public static readonly Vector3 Down = new(FP.Zero, -1, FP.Zero);

        public static readonly Vector3 Right = new(FP.One, FP.Zero, FP.Zero);

        public static readonly Vector3 Left = new(-1, FP.Zero, FP.Zero);

        public Vector3(FP x, FP y, FP z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public FP Magnitude => SqrMagnitude.Sqrt;

        public FP SqrMagnitude => X * X + Y * Y + Z * Z;

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

        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Vector3 operator -(Vector3 v) => new(-v.X, -v.Y, -v.Z);

        public static Vector3 operator *(Vector3 v, FP scalar) => new(v.X * scalar, v.Y * scalar, v.Z * scalar);

        public static Vector3 operator *(FP scalar, Vector3 v) => new(v.X * scalar, v.Y * scalar, v.Z * scalar);

        public static Vector3 operator /(Vector3 v, FP scalar) => new(v.X / scalar, v.Y / scalar, v.Z / scalar);

        public static bool operator ==(Vector3 a, Vector3 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;

        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);

        public FP Dot(Vector3 other) => X * other.X + Y * other.Y + Z * other.Z;

        public static FP Dot(Vector3 a, Vector3 b) => a.Dot(b);

        public static Vector3 Cross(Vector3 a, Vector3 b)
        {
            return new Vector3(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X);
        }

        public static FP Distance(Vector3 a, Vector3 b) => (a - b).Magnitude;

        public static FP SqrDistance(Vector3 a, Vector3 b) => (a - b).SqrMagnitude;

        public static FP Angle(Vector3 from, Vector3 to)
        {
            FP denominator = (from.SqrMagnitude * to.SqrMagnitude).Sqrt;
            if (denominator == FP.Zero)
                return FP.Zero;

            FP cosAngle = FP.Clamp(Dot(from, to) / denominator, -1, FP.One);
            return FpMath.Acos(cosAngle) * FP.Rad2Deg;
        }

        public static Vector3 Lerp(Vector3 a, Vector3 b, FP t)
        {
            t = FP.Clamp01(t);
            return LerpUnclamped(a, b, t);
        }

        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, FP t)
        {
            return new Vector3(
                FP.LerpUnclamped(a.X, b.X, t),
                FP.LerpUnclamped(a.Y, b.Y, t),
                FP.LerpUnclamped(a.Z, b.Z, t));
        }

        public static Vector3 MoveTowards(Vector3 current, Vector3 target, FP maxDistanceDelta)
        {
            Vector3 diff = target - current;
            FP sqrDist = diff.SqrMagnitude;

            if (sqrDist == FP.Zero || (maxDistanceDelta >= FP.Zero && sqrDist <= maxDistanceDelta * maxDistanceDelta))
                return target;

            FP dist = sqrDist.Sqrt;
            return current + diff / dist * maxDistanceDelta;
        }

        public static Vector3 Reflect(Vector3 inDirection, Vector3 inNormal)
        {
            FP dot = Dot(inDirection, inNormal);
            return inDirection - 2 * dot * inNormal;
        }

        public static Vector3 Project(Vector3 vector, Vector3 onNormal)
        {
            FP sqrMag = onNormal.SqrMagnitude;
            if (sqrMag == FP.Zero)
                return Zero;
            FP dot = Dot(vector, onNormal);
            return onNormal * (dot / sqrMag);
        }

        public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal)
        {
            return vector - Project(vector, planeNormal);
        }

        public static Vector3 Slerp(Vector3 a, Vector3 b, FP t)
        {
            t = FP.Clamp01(t);
            return SlerpUnclamped(a, b, t);
        }

        public static Vector3 SlerpUnclamped(Vector3 a, Vector3 b, FP t)
        {
            Vector3 aNorm = a.Normalized;
            Vector3 bNorm = b.Normalized;

            if (aNorm == Zero || bNorm == Zero)
                return LerpUnclamped(aNorm, bNorm, t);

            FP dot = FP.Clamp(Dot(aNorm, bNorm), -1, FP.One);
            FP angleRad = FpMath.Acos(dot);

            if (angleRad.Abs() < FP.Epsilon * FP.FromInt(100))
                return LerpUnclamped(aNorm, bNorm, t);

            FP sinAngle = FpMath.Sin(angleRad);
            FP invSinAngle = FP.One / sinAngle;

            FP weightA = FpMath.Sin((FP.One - t) * angleRad) * invSinAngle;
            FP weightB = FpMath.Sin(t * angleRad) * invSinAngle;

            return aNorm * weightA + bNorm * weightB;
        }

        public static Vector3 RotateTowards(Vector3 current, Vector3 target, FP maxRadiansDelta, FP maxMagnitudeDelta)
        {
            FP curMag = current.Magnitude;
            FP targetMag = target.Magnitude;

            if (curMag == FP.Zero || targetMag == FP.Zero)
                return MoveTowards(current, target, maxMagnitudeDelta);

            Vector3 curNorm = current / curMag;
            Vector3 targetNorm = target / targetMag;

            FP dot = FP.Clamp(Dot(curNorm, targetNorm), -1, FP.One);
            FP angleRad = FpMath.Acos(dot);

            Vector3 newDir;
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

        public static Vector3 Max(Vector3 a, Vector3 b)
        {
            return new Vector3(FP.Max(a.X, b.X), FP.Max(a.Y, b.Y), FP.Max(a.Z, b.Z));
        }

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

        public bool Equals(Vector3 other) => this == other;

        public override bool Equals(object? obj) => obj is Vector3 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Z);

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}
