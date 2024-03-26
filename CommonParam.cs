using System;

namespace RemiChemieConsole
{
    class CommonParam
    {
        internal static readonly int thdn = Environment.ProcessorCount;//CPUスレッド数
        internal static readonly TimeSpan ts = TimeSpan.FromSeconds(9.25 / thdn);
        internal static readonly TimeSpan tthnj = TimeSpan.FromSeconds((343.4 / thdn) > 34.34 ? 34.34 : (343.4 / thdn));//最大東方ネットワークジョブ時間
        internal static readonly TimeSpan tthnl = TimeSpan.FromSeconds(Math.PI * Math.E / thdn);//最大東方ネットワークジョブ時間
    }
}
