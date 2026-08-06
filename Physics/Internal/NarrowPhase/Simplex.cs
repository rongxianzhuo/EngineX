using System.Runtime.CompilerServices;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.NarrowPhase
{
    internal struct Simplex
    {
        public Vector3 P1;
        public Vector3 P2;
        public Vector3 P3;
        public Vector3 P4;

        public Vector3 A1;
        public Vector3 A2;
        public Vector3 A3;
        public Vector3 A4;

        public Vector3 B1;
        public Vector3 B2;
        public Vector3 B3;
        public Vector3 B4;

        public int Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetP(int i)
        {
            switch (i)
            {
                case 0: return P1;
                case 1: return P2;
                case 2: return P3;
                default: return P4;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetP(int i, Vector3 v)
        {
            switch (i)
            {
                case 0: P1 = v; break;
                case 1: P2 = v; break;
                case 2: P3 = v; break;
                default: P4 = v; break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetA(int i)
        {
            switch (i)
            {
                case 0: return A1;
                case 1: return A2;
                case 2: return A3;
                default: return A4;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetA(int i, Vector3 v)
        {
            switch (i)
            {
                case 0: A1 = v; break;
                case 1: A2 = v; break;
                case 2: A3 = v; break;
                default: A4 = v; break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetB(int i)
        {
            switch (i)
            {
                case 0: return B1;
                case 1: return B2;
                case 2: return B3;
                default: return B4;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetB(int i, Vector3 v)
        {
            switch (i)
            {
                case 0: B1 = v; break;
                case 1: B2 = v; break;
                case 2: B3 = v; break;
                default: B4 = v; break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int i, Vector3 p, Vector3 a, Vector3 b)
        {
            SetP(i, p);
            SetA(i, a);
            SetB(i, b);
        }
    }
}