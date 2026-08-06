using System;
using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;
using EngineX.Physics.Internal.NarrowPhase;

namespace EngineX.Physics.Tests
{
    public static class GjkDebug
    {
        public static void Run()
        {
            Console.WriteLine("=== GJK Debug: box-box overlap ===");
            var a = new Box(Vector3.Zero, Vector3.One, Quaternion.Identity);
            var b = new Box(new Vector3(FP.FromFloat(1.5f), FP.Zero, FP.Zero), Vector3.One, Quaternion.Identity);

            bool overlap = GJK.Intersects(a, b, out var gjk);
            Console.WriteLine($"overlap = {overlap}, iters = {gjk.Iterations}");
            var s = gjk.Simplex;
            Console.WriteLine($"simplex.Count = {s.Count}");
            Console.WriteLine($"  P1={s.P1} A1={s.A1} B1={s.B1}");
            Console.WriteLine($"  P2={s.P2} A2={s.A2} B2={s.B2}");
            Console.WriteLine($"  P3={s.P3} A3={s.A3} B3={s.B3}");
            Console.WriteLine($"  P4={s.P4} A4={s.A4} B4={s.B4}");

            PenetrationExtractor.Extract(s, out var normal, out var depth);
            Console.WriteLine($"normal = {normal}, depth = {depth}, normal.Mag = {normal.Magnitude}");

            var r = ShapeOverlap.Compute(a, b);
            Console.WriteLine($"ShapeOverlap result: overlap={r.IsOverlapping}, normal={r.Normal}, depth={r.Penetration}");
        }
    }
}