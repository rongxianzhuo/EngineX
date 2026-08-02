using System;
using EngineX.Baseline.FixedPoint;

namespace EngineX.Baseline.Math
{
    public readonly struct Quaternion : IEquatable<Quaternion>
    {
        public readonly FP X;

        public readonly FP Y;

        public readonly FP Z;

        public readonly FP W;

        // Dot-product threshold used to detect (near-)parallel quaternion directions.
        private static readonly FP DotParallelEpsilon = FP.Epsilon * FP.FromInt(100);

        public static readonly Quaternion Identity = new(FP.Zero, FP.Zero, FP.Zero, FP.One);

        public Quaternion(FP x, FP y, FP z, FP w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static Quaternion Euler(FP x, FP y, FP z)
        {
            FP halfX = x * FP.Deg2Rad * FP.Half;
            FP halfY = y * FP.Deg2Rad * FP.Half;
            FP halfZ = z * FP.Deg2Rad * FP.Half;

            FP cx = FpMath.Cos(halfX);
            FP sx = FpMath.Sin(halfX);
            FP cy = FpMath.Cos(halfY);
            FP sy = FpMath.Sin(halfY);
            FP cz = FpMath.Cos(halfZ);
            FP sz = FpMath.Sin(halfZ);

            FP q1x = cy * sx;
            FP q1y = sy * cx;
            FP q1z = -sy * sx;
            FP q1w = cy * cx;

            return new Quaternion(
                q1x * cz + q1y * sz,
                -q1x * sz + q1y * cz,
                q1z * cz + q1w * sz,
                -q1z * sz + q1w * cz
            );
        }

        public static Quaternion AngleAxis(FP angleDegrees, Vector3 axis)
        {
            FP halfAngleRad = angleDegrees * FP.Deg2Rad * FP.Half;
            FP s = FpMath.Sin(halfAngleRad);
            FP c = FpMath.Cos(halfAngleRad);

            Vector3 n = axis.Normalized;
            return new Quaternion(n.X * s, n.Y * s, n.Z * s, c);
        }

        public static Quaternion LookRotation(Vector3 forward, Vector3 upwards)
        {
            forward = forward.Normalized;
            if (forward == Vector3.Zero)
                return Identity;

            Vector3 right = Vector3.Cross(upwards, forward).Normalized;
            if (right == Vector3.Zero)
            {
                Vector3 arbitrary = (forward.X).Abs() < (forward.Z).Abs()
                    ? new Vector3(FP.One, FP.Zero, FP.Zero)
                    : new Vector3(FP.Zero, FP.Zero, -FP.One);
                right = Vector3.Cross(forward, arbitrary).Normalized;
            }

            Vector3 up = Vector3.Cross(forward, right).Normalized;

            return FromRotationMatrix(right, up, forward);
        }

        public static Quaternion FromToRotation(Vector3 fromDirection, Vector3 toDirection)
        {
            fromDirection = fromDirection.Normalized;
            toDirection = toDirection.Normalized;

            if (fromDirection == Vector3.Zero || toDirection == Vector3.Zero)
                return Identity;

            FP dot = Vector3.Dot(fromDirection, toDirection);

            if (dot > FP.One - DotParallelEpsilon)
                return Identity;

            if (dot < -1 + DotParallelEpsilon)
            {
                Vector3 axis = (fromDirection.X).Abs() < (fromDirection.Z).Abs()
                    ? new Vector3(FP.One, FP.Zero, FP.Zero)
                    : new Vector3(FP.Zero, FP.Zero, -FP.One);
                axis = Vector3.Cross(fromDirection, axis).Normalized;
                return AngleAxis(FP.FromInt(180), axis);
            }

            Vector3 cross = Vector3.Cross(fromDirection, toDirection);
            FP s = ((FP.One + dot) * 2).Sqrt;
            FP invS = FP.One / s;

            return new Quaternion(
                cross.X * invS,
                cross.Y * invS,
                cross.Z * invS,
                s * FP.Half
            );
        }

        private static Quaternion FromRotationMatrix(Vector3 columnX, Vector3 columnY, Vector3 columnZ)
        {
            FP r00 = columnX.X, r10 = columnX.Y, r20 = columnX.Z;
            FP r01 = columnY.X, r11 = columnY.Y, r21 = columnY.Z;
            FP r02 = columnZ.X, r12 = columnZ.Y, r22 = columnZ.Z;

            FP trace = r00 + r11 + r22;

            FP qx, qy, qz, qw;

            if (trace > FP.Zero)
            {
                FP s = (trace + FP.One).Sqrt * 2;
                FP invS = FP.One / s;
                qw = s * FP.FromInt(1) / FP.FromInt(4);
                qx = (r21 - r12) * invS;
                qy = (r02 - r20) * invS;
                qz = (r10 - r01) * invS;
            }
            else if (r00 > r11 && r00 > r22)
            {
                FP s = (FP.One + r00 - r11 - r22).Sqrt * 2;
                FP invS = FP.One / s;
                qw = (r21 - r12) * invS;
                qx = s * FP.FromInt(1) / FP.FromInt(4);
                qy = (r01 + r10) * invS;
                qz = (r02 + r20) * invS;
            }
            else if (r11 > r22)
            {
                FP s = (FP.One + r11 - r00 - r22).Sqrt * 2;
                FP invS = FP.One / s;
                qw = (r02 - r20) * invS;
                qx = (r01 + r10) * invS;
                qy = s * FP.FromInt(1) / FP.FromInt(4);
                qz = (r12 + r21) * invS;
            }
            else
            {
                FP s = (FP.One + r22 - r00 - r11).Sqrt * 2;
                FP invS = FP.One / s;
                qw = (r10 - r01) * invS;
                qx = (r02 + r20) * invS;
                qy = (r12 + r21) * invS;
                qz = s * FP.FromInt(1) / FP.FromInt(4);
            }

            return new Quaternion(qx, qy, qz, qw).Normalized;
        }

        public Vector3 EulerAngles
        {
            get
            {
                FP sinPitch = 2 * (W * X - Y * Z);
                FP pitch = FpMath.Asin(FP.Clamp(sinPitch, -1, FP.One));

                FP sinYaw = 2 * (W * Y + X * Z);
                FP cosYaw = FP.One - 2 * (X * X + Y * Y);
                FP yaw = FpMath.Atan2(sinYaw, cosYaw);

                FP sinRoll = 2 * (W * Z + X * Y);
                FP cosRoll = FP.One - 2 * (Z * Z + X * X);
                FP roll = FpMath.Atan2(sinRoll, cosRoll);

                return new Vector3(
                    pitch * FP.Rad2Deg,
                    yaw * FP.Rad2Deg,
                    roll * FP.Rad2Deg
                );
            }
        }

        public Quaternion Normalized
        {
            get
            {
                FP sqrMag = X * X + Y * Y + Z * Z + W * W;
                if (sqrMag == FP.Zero)
                    return Identity;
                FP invMag = FP.One / sqrMag.Sqrt;
                return new Quaternion(X * invMag, Y * invMag, Z * invMag, W * invMag);
            }
        }

        public static Quaternion operator *(Quaternion a, Quaternion b)
        {
            return new Quaternion(
                a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
                a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
                a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
                a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z
            );
        }

        public static Vector3 operator *(Quaternion rotation, Vector3 point)
        {
            FP twoX = 2 * rotation.X;
            FP twoY = 2 * rotation.Y;
            FP twoZ = 2 * rotation.Z;

            FP xx = rotation.X * twoX;
            FP yy = rotation.Y * twoY;
            FP zz = rotation.Z * twoZ;
            FP xy = rotation.X * twoY;
            FP xz = rotation.X * twoZ;
            FP yz = rotation.Y * twoZ;
            FP wx = rotation.W * twoX;
            FP wy = rotation.W * twoY;
            FP wz = rotation.W * twoZ;

            return new Vector3(
                (FP.One - (yy + zz)) * point.X + (xy - wz) * point.Y + (xz + wy) * point.Z,
                (xy + wz) * point.X + (FP.One - (xx + zz)) * point.Y + (yz - wx) * point.Z,
                (xz - wy) * point.X + (yz + wx) * point.Y + (FP.One - (xx + yy)) * point.Z
            );
        }

        public static bool operator ==(Quaternion a, Quaternion b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W;
        }

        public static bool operator !=(Quaternion a, Quaternion b) => !(a == b);

        public static FP Dot(Quaternion a, Quaternion b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
        }

        public static FP Angle(Quaternion a, Quaternion b)
        {
            FP dot = FP.Clamp((Dot(a, b)).Abs(), FP.Zero, FP.One);
            return FpMath.Acos(dot) * 2 * FP.Rad2Deg;
        }

        public static Quaternion Inverse(Quaternion q)
        {
            FP sqrMag = q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W;
            if (sqrMag == FP.Zero)
                return Identity;
            FP invSqrMag = FP.One / sqrMag;
            return new Quaternion(-q.X * invSqrMag, -q.Y * invSqrMag, -q.Z * invSqrMag, q.W * invSqrMag);
        }

        public static Quaternion Normalize(Quaternion q) => q.Normalized;

        public static Quaternion Slerp(Quaternion a, Quaternion b, FP t)
        {
            t = FP.Clamp01(t);
            return SlerpUnclamped(a, b, t);
        }

        public static Quaternion SlerpUnclamped(Quaternion a, Quaternion b, FP t)
        {
            FP dot = Dot(a, b);

            Quaternion bAdjusted = b;
            if (dot < FP.Zero)
            {
                dot = -dot;
                bAdjusted = new Quaternion(-b.X, -b.Y, -b.Z, -b.W);
            }

            if (dot > FP.One - DotParallelEpsilon)
            {
                Quaternion lerped = new Quaternion(
                    FP.LerpUnclamped(a.X, bAdjusted.X, t),
                    FP.LerpUnclamped(a.Y, bAdjusted.Y, t),
                    FP.LerpUnclamped(a.Z, bAdjusted.Z, t),
                    FP.LerpUnclamped(a.W, bAdjusted.W, t)
                );
                return lerped.Normalized;
            }

            FP angleRad = FpMath.Acos(dot);
            FP sinAngle = FpMath.Sin(angleRad);
            FP invSinAngle = FP.One / sinAngle;

            FP weightA = FpMath.Sin((FP.One - t) * angleRad) * invSinAngle;
            FP weightB = FpMath.Sin(t * angleRad) * invSinAngle;

            return new Quaternion(
                weightA * a.X + weightB * bAdjusted.X,
                weightA * a.Y + weightB * bAdjusted.Y,
                weightA * a.Z + weightB * bAdjusted.Z,
                weightA * a.W + weightB * bAdjusted.W
            );
        }

        public static Quaternion Lerp(Quaternion a, Quaternion b, FP t)
        {
            t = FP.Clamp01(t);
            return LerpUnclamped(a, b, t);
        }

        public static Quaternion LerpUnclamped(Quaternion a, Quaternion b, FP t)
        {
            FP dot = Dot(a, b);
            if (dot < FP.Zero)
            {
                b = new Quaternion(-b.X, -b.Y, -b.Z, -b.W);
            }

            return new Quaternion(
                FP.LerpUnclamped(a.X, b.X, t),
                FP.LerpUnclamped(a.Y, b.Y, t),
                FP.LerpUnclamped(a.Z, b.Z, t),
                FP.LerpUnclamped(a.W, b.W, t)
            );
        }

        public static Quaternion RotateTowards(Quaternion from, Quaternion to, FP maxDegreesDelta)
        {
            FP angle = Angle(from, to);
            if (angle == FP.Zero)
                return to;

            FP t = FP.Min(FP.One, maxDegreesDelta / angle);
            return SlerpUnclamped(from, to, t);
        }

        public bool Equals(Quaternion other) => this == other;

        public override bool Equals(object? obj) => obj is Quaternion other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

        public override string ToString()
        {
            return $"({X}, {Y}, {Z}, {W})";
        }
    }
}