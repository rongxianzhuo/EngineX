using System;
using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.Dynamics
{
    internal struct Island
    {
        public int FirstBodyIndex;
        public int BodyCount;
        public int FirstContactIndex;
        public int ContactCount;

        public static Island Default => new Island();
    }
}