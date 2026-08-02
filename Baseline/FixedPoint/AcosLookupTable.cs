using System;
using System.IO;
using System.Text;

namespace EngineX.Baseline.FixedPoint
{
    public class AcosLookupTable
    {
        
        public static readonly FP Interval = FP.One / 64;

        public static FP Lookup(FP x)
        {
            if (x < -1) return FP.PI;
            if (x > 1) return FP.Zero;
            x += 1;
            var index = (x / Interval).Int();
            var l = Table[index];
            var r = Table[index < Table.Length - 1 ? index + 1 : index];
            return l + (r - l) * (x % Interval) / Interval;
        }
        
        public static void BuildLookupCode(string savePath)
        {
            var builder = new StringBuilder();
            var i = 0;
            while (i * Interval - 1 <= 1)
            {
                var x = i * Interval.Single() - 1;
                var y = MathF.Acos(x);
                var f = FP.FromFloat(y);
                builder.Append($"            FP.FromRawData({f.RawData}),\n");
                i += 1;
            }
            File.WriteAllText(savePath, builder.ToString());
        }
        
        public static StringBuilder ShowCalculateError(FP start, FP end, FP interval)
        {
            var builder = new StringBuilder();
            while (start <= end)
            {
                var y = MathF.Acos(start.Single());
                var fY = Lookup(start);
                var error = fY.Single() - y;
                builder.Append($"Acos({start}) error: {error}\n");
                start += interval;
            }
            return builder;
        }

        public static readonly FP[] Table = new[]
        {
            // Table[0] is acos(-1) = π. Keep in sync with FP.PI (13493037705).
            FP.FromRawData(13493037705L),
            FP.FromRawData(12732795904),
            FP.FromRawData(12416480256),
            FP.FromRawData(12172785664),
            FP.FromRawData(11966515200),
            FP.FromRawData(11784050688),
            FP.FromRawData(11618417664),
            FP.FromRawData(11465475072),
            FP.FromRawData(11322530816),
            FP.FromRawData(11187713024),
            FP.FromRawData(11059662848),
            FP.FromRawData(10937352192),
            FP.FromRawData(10819985408),
            FP.FromRawData(10706928640),
            FP.FromRawData(10597668864),
            FP.FromRawData(10491783168),
            FP.FromRawData(10388918272),
            FP.FromRawData(10288773120),
            FP.FromRawData(10191094784),
            FP.FromRawData(10095661056),
            FP.FromRawData(10002279424),
            FP.FromRawData(9910781952),
            FP.FromRawData(9821020160),
            FP.FromRawData(9732860928),
            FP.FromRawData(9646186496),
            FP.FromRawData(9560891392),
            FP.FromRawData(9476878336),
            FP.FromRawData(9394060288),
            FP.FromRawData(9312359424),
            FP.FromRawData(9231703040),
            FP.FromRawData(9152023552),
            FP.FromRawData(9073261568),
            FP.FromRawData(8995358720),
            FP.FromRawData(8918262784),
            FP.FromRawData(8841926656),
            FP.FromRawData(8766303232),
            FP.FromRawData(8691351552),
            FP.FromRawData(8617030656),
            FP.FromRawData(8543302656),
            FP.FromRawData(8470134272),
            FP.FromRawData(8397490176),
            FP.FromRawData(8325340672),
            FP.FromRawData(8253654528),
            FP.FromRawData(8182403584),
            FP.FromRawData(8111561216),
            FP.FromRawData(8041101312),
            FP.FromRawData(7970998784),
            FP.FromRawData(7901230080),
            FP.FromRawData(7831772160),
            FP.FromRawData(7762603520),
            FP.FromRawData(7693702144),
            FP.FromRawData(7625047552),
            FP.FromRawData(7556620288),
            FP.FromRawData(7488399872),
            FP.FromRawData(7420368384),
            FP.FromRawData(7352507392),
            FP.FromRawData(7284797952),
            FP.FromRawData(7217222656),
            FP.FromRawData(7149764096),
            FP.FromRawData(7082405376),
            FP.FromRawData(7015129600),
            FP.FromRawData(6947919360),
            FP.FromRawData(6880758272),
            FP.FromRawData(6813630464),
            FP.FromRawData(6746519040),
            FP.FromRawData(6679407104),
            FP.FromRawData(6612279296),
            FP.FromRawData(6545118208),
            FP.FromRawData(6477908480),
            FP.FromRawData(6410632192),
            FP.FromRawData(6343273472),
            FP.FromRawData(6275814912),
            FP.FromRawData(6208240128),
            FP.FromRawData(6140530688),
            FP.FromRawData(6072669184),
            FP.FromRawData(6004637696),
            FP.FromRawData(5936417792),
            FP.FromRawData(5867990016),
            FP.FromRawData(5799335936),
            FP.FromRawData(5730434560),
            FP.FromRawData(5661265408),
            FP.FromRawData(5591807488),
            FP.FromRawData(5522038784),
            FP.FromRawData(5451936256),
            FP.FromRawData(5381476352),
            FP.FromRawData(5310633984),
            FP.FromRawData(5239383040),
            FP.FromRawData(5167697408),
            FP.FromRawData(5095547392),
            FP.FromRawData(5022903808),
            FP.FromRawData(4949734912),
            FP.FromRawData(4876007424),
            FP.FromRawData(4801686528),
            FP.FromRawData(4726734336),
            FP.FromRawData(4651110912),
            FP.FromRawData(4574774784),
            FP.FromRawData(4497679360),
            FP.FromRawData(4419776512),
            FP.FromRawData(4341013504),
            FP.FromRawData(4261334528),
            FP.FromRawData(4180677888),
            FP.FromRawData(4098977024),
            FP.FromRawData(4016159744),
            FP.FromRawData(3932146688),
            FP.FromRawData(3846851072),
            FP.FromRawData(3760176896),
            FP.FromRawData(3672017920),
            FP.FromRawData(3582255872),
            FP.FromRawData(3490758656),
            FP.FromRawData(3397377280),
            FP.FromRawData(3301943040),
            FP.FromRawData(3204264448),
            FP.FromRawData(3104120064),
            FP.FromRawData(3001254400),
            FP.FromRawData(2895368448),
            FP.FromRawData(2786108928),
            FP.FromRawData(2673052416),
            FP.FromRawData(2555685632),
            FP.FromRawData(2433375232),
            FP.FromRawData(2305324544),
            FP.FromRawData(2170506752),
            FP.FromRawData(2027562112),
            FP.FromRawData(1874620160),
            FP.FromRawData(1708986752),
            FP.FromRawData(1526522496),
            FP.FromRawData(1320251648),
            FP.FromRawData(1076557824),
            FP.FromRawData(760242240),
            FP.FromRawData(0),
        };
    }
}