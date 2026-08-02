using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace EngineX.Baseline.FixedPoint
{
    public readonly struct FP : IEquatable<FP>
    {

        public const int RawDataBit = 64;
        public const int FractionBit = 32;
        public const long FractionBitPow2 = 1L << FractionBit;

        // ── Mathematical constants ─────────────────────────────────────────
        // Q32.32 layout: a value v is stored as round(v * 2^32). One raw
        // unit equals 1/2^32 ≈ 2.32830644×10^-10 ( == FP.Epsilon ).
        //
        // PI = 13493037705 raw
        //   = round(π × 2^32)
        //   = round(3.141592653589793 × 4294967296)
        //   ≈ 3.141592653701082 (error ≈ 1.11×10^-10, ≈ 0.48 raw units)
        // Sources verified with Python:
        //   >>> round(math.pi * 2**32) → 13493037705
        public static readonly FP PI = new FP(13493037705L);

        // Deg2Rad = π/180  →  raw = 74961321
        //   ≈ 0.017453292617574 (true: 0.017453292519943, error < 1 raw unit)
        // Pre-computed as raw to avoid an FP division in static init
        // (FP/FP is O(64) bit-shift long division; precomputing is exact).
        public static readonly FP Deg2Rad = new FP(74961321L);

        // Rad2Deg = 180/π  →  raw = 246083499208
        //   ≈ 57.295779513195 (true: 57.295779513082, error < 1 raw unit)
        public static readonly FP Rad2Deg = new FP(246083499208L);

        public static readonly FP Zero = default;
        public static readonly FP Epsilon = new FP(1L);
        public static readonly FP One = new FP(1L << FractionBit);
        public static readonly FP MaxValue = new FP(0x7FFFFFFFFFFFFFFFL);

        public readonly long RawData;

        private FP(long rawData)
        {
            RawData = rawData;
        }

        public FP Sqrt
        {
            get
            {
                // Newton's method: x_{n+1} = (x_n + n/x_n) / 2
                // Quadratic convergence kicks in only AFTER |x/sqrt(n)| is
                // small. Starting from n/2, the linear regime dominates and
                // 30+ iterations are needed for full precision (each iteration
                // halves the relative gap until k<4, then quadratically
                // converges).
                //
                // Cheap bit-trick for a good initial guess:
                //   For Q32.32 raw with leading 1 at bit h, value's magnitude
                //   is 2^(h-32), so sqrt's raw leading-bit is roughly at
                //   (h + 32) / 2. Using that single bit keeps the factor
                //   within 2 of the true sqrt, so 5 Newton iterations land
                //   at full Q32.32 precision (~3× the bit-budget needed).
                //
                // Determinism: every step is in fixed-point, no float paths.
                if (RawData < 0)
                    throw new ArgumentOutOfRangeException(nameof(RawData),
                        "Cannot take sqrt of a negative FP value.");
                if (RawData == 0) return Zero;

                // 1. Locate leading 1 bit in RawData.
                var raw = RawData;
                var h = 63;
                while (h >= 0 && (raw & (1L << h)) == 0) h--;
                // raw > 0 ⇒ h ∈ [0, 62].

                // 2. Initial guess = 1L << ((h+32) / 2). Clamp the bit index
                //    so we never shift by a non-positive amount.
                var guessBit = (h + 32) >> 1;
                if (guessBit < 1) guessBit = 1;
                if (guessBit > 62) guessBit = 62;
                var x = 1L << (int)guessBit;

                // 3. Five Newton iterations, all in fixed-point.
                for (var i = 0; i < 5; i++)
                {
                    // Q32.32 division via the FP / FP operator (which
                    // normalizes by leading bits to avoid long overflow).
                    x = (x + (this / FP.FromRawData(x)).RawData) >> 1;
                }
                return FP.FromRawData(x);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FP Abs()
        {
            return RawData < 0 ? -this : this;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Sign()
        {
            return RawData < 0 ? -1 : 1;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FP other)
        {
            return RawData == other.RawData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is FP other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return RawData.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP FromRawData(long l) => new FP(l);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP FromInt(int l) => new FP((long)l << FractionBit);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP FromFloat(float f) => new FP((long)((double)f * FractionBitPow2));

        public static FP FromString(string text)
        {
            if (text.StartsWith('-')) return -FromString(text.Substring(1));
            if (text.Contains("."))
            {
                var dotIndex = text.IndexOf(".", StringComparison.Ordinal) + 1;
                var scale = 1;
                while (dotIndex++ < text.Length) scale *= 10;
                return FromInt(int.Parse(text.Replace(".", ""))) / scale;
            }
            else
            {
                return FromInt(int.Parse(text));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Int() => RawData >= 0 ? (int)(RawData >> FractionBit) : -(int)(~(RawData - 1) >> FractionBit);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Single() => (float)((double)RawData / FractionBitPow2);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP Clamp(FP value, FP min, FP max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP LerpUnclamped(FP a, FP b, FP t)
        {
            return a + (b - a) * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP Clamp01(FP value)
        {
            return Clamp(value, Zero, One);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP operator %(FP a, FP b) => new FP(a.RawData % b.RawData);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP operator +(FP a, int b) => a + FromInt(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP operator +(int a, FP b) => FromInt(a) + b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP operator +(FP a, FP b) => new FP(a.RawData + b.RawData);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP operator -(FP a) => new FP(-a.RawData);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP operator -(FP a, int b) => a - FromInt(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP operator -(int a, FP b) => FromInt(a) - b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP operator -(FP a, FP b) => new FP(a.RawData - b.RawData);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP operator *(FP a, int b) => new FP(a.RawData * b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP operator *(int a, FP b) => new FP(a * b.RawData);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(FP a, int b) => a >= FromInt(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(FP a, FP b) => a.RawData >= b.RawData;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(FP a, int b) => a <= FromInt(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(FP a, FP b) => a.RawData <= b.RawData;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(FP a, int b) => a > FromInt(b);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(FP a, FP b) => a.RawData > b.RawData;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(FP a, int b) => a < FromInt(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(FP a, FP b) => a.RawData < b.RawData;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FP a, FP b) => a.RawData == b.RawData;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FP a, FP b) => a.RawData != b.RawData;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP operator >>(FP a, int b) => new FP(a.RawData >> b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP operator <<(FP a, int b) => new FP(a.RawData << b);

        public static FP operator *(FP a, FP b)
        {
            var aRawData = a.RawData >= 0 ? a.RawData : ~(a.RawData - 1);
            var bRawData = b.RawData >= 0 ? b.RawData : ~(b.RawData - 1);
            var rawData = 0L;
            for (var i = 0; i < RawDataBit - 1; i++)
            {
                if (((1L << i) & bRawData) == 0) continue;
                if (FractionBit > i ) rawData += aRawData >> (FractionBit - i);
                else rawData += aRawData << (i - FractionBit);
            }
            if (a.RawData >= 0 != b.RawData >= 0) rawData = -rawData;
            return new FP(rawData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP operator /(FP a, int b) => new FP(a.RawData / b);

        public static FP operator /(FP a, FP b)
        {
            if (b.RawData == 0) throw new DivideByZeroException();
            if (a.RawData == 0) return Zero;
            var aRawData = a.RawData > 0 ? a.RawData : ~(a.RawData - 1);
            var bRawData = b.RawData > 0 ? b.RawData : ~(b.RawData - 1);
            var af = RawDataBit - 1;
            for (var i = 0; i < RawDataBit - 1; i++)
            {
                af--;
                if ((aRawData & (1L << af)) != 0) break;
            }
            var bf = RawDataBit - 1;
            for (var i = 0; i < RawDataBit - 1; i++)
            {
                bf--;
                if ((bRawData & (1L << bf)) != 0) break;
            }

            if (af >= bf)
            {
                bRawData <<= af - bf;
            }
            else
            {
                aRawData <<= bf - af;
            }
            var result = 0L;
            var tag = 1L << (FractionBit + af - bf);
            while (aRawData > 0 && tag != 0)
            {
                if (aRawData >= bRawData)
                {
                    result += tag;
                    aRawData -= bRawData;
                }
                aRawData <<= 1;
                tag >>= 1;
            }
            if (a.RawData >= 0 != b.RawData >= 0) result = -result;
            return new FP(result);
        }

        public static FP Max(FP a, FP b)
        {
            return a > b ? a : b;
        }

        public static FP Min(FP a, FP b)
        {
            return a < b ? a : b;
        }

        public static implicit operator FP(int i) => FromInt(i);

        public override string ToString()
        {
            return Single().ToString(CultureInfo.InvariantCulture);
        }
    }
}