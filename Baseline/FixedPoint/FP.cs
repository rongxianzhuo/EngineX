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

        public static readonly FP PI = new FP(13493038080);
        public static readonly FP Zero = default;
        public static readonly FP Epsilon = new FP(1L);
        public static readonly FP One = new FP(1L << FractionBit);
        public static readonly FP MaxValue = new FP(0x7FFFFFFFFFFFFFFFL);

        public readonly long RawData;

        private FP(long rawData)
        {
            RawData = rawData;
        }

        public FP Sqrt()
        {
            if (this <= 0) return Zero;
            var l = Epsilon;
            var r = this;
            while (r - l > Epsilon)
            {
                var m = (l + r) >> 1;
                var m2 = m * m;
                if (m2 == this) return m;
                if (m2 > this) r = m;
                else l = m;
            }
            return l;
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

        public override string ToString()
        {
            return Single().ToString(CultureInfo.InvariantCulture);
        }
    }
}