using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace RemiChemieConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");
            Stopwatch sw = new Stopwatch();
            sw.Start();



            //GausInter gi = new GausInter();
            //gi.CnvMoltCom(1);
            //gi.CnvLogtDes();
            //DirectoryInfo di = new DirectoryInfo("D:\\OJ\\KKS\\beta\\BINOL Lib\\Desc\\Lib3\\OH\\");
            //DataProc.DesfZH(out string msg, in di);

            /*
            //SKBINOL.BINOLB(null, null, 40000);
            string msg;
            bool flg;
            int ccc = 0;
            DirectoryInfo di = new DirectoryInfo("D:\\ML\\Run2\\aer\\OH\\");
            flg = THNetwork.TH_N_T_R_(out msg, in di, 343434, true,ref ccc);
            Console.WriteLine(flg);
            Console.WriteLine(msg);
            //*/


            /*
            DirectoryInfo di = new DirectoryInfo("D:\\ML\\Run2\\aer\\Ac\\");
            bool[] ht = new bool[6] { true, true, true, true, true, true };
            byte ut = 5;
            double[] rr = new double[6] { 0.0, 0.44, 0.55, 0.66, 0.77, 0.88 };
            double[] uw = new double[7] { 0.0, 0.0, 1.0, 1.0, Math.Pow(Math.E * Math.PI, 2), Math.Pow(Math.E * Math.PI, 2.5), Math.Pow(Math.E * Math.PI, 5) };
            double[] LtP = new double[6] { 10.0, 10.0, 10.0, 10.0, 10.0, 10.0 };
            bool flg;
            flg = THNetwork.THNMtjgx(out string msg, in di, in ht, ut, rr, uw, in LtP);
            Console.WriteLine(flg);
            Console.WriteLine(msg);
            //*/



            /*
            DirectoryInfo di;
            FileInfo desfi = new FileInfo("D:\\ML\\Run2\\by\\Ac\\Pred3\\Descriptors.csv");
            double[] wt = new double[7] { 0.0, 1.0, Math.E, Math.Exp(2.0), Math.Exp(3.0), Math.Exp(4.0), Math.Exp(5.0) };
            bool flg = false;
            string msg = string.Empty;
            //di = new DirectoryInfo("D:\\ML\\Run2\\by\\Ac\\");
            //flg = THNetwork.THNYSKR(out msg, in di, in desfi, in wt);
            //di = new DirectoryInfo("D:\\ML\\Run2\\by\\Ac\\Pred3\\");
            //flg = THNetwork.THNYSKSK(out msg, in di, 200.0, -200.0);
            Console.WriteLine(flg);
            Console.WriteLine(msg);
            //*/



            //Console.WriteLine( SGKC.Exl2610.AtD("ATL"));
            //Console.WriteLine(SGKC.Exl2610.DtA(1208));


            /*
            string s = "3層(51, 20, 1)";
            Regex r = new Regex(@"^(\d+)層\((?>(?>(\d+))(?:,\s)?)+\)$", RegexOptions.CultureInvariant, CommonParam.ts);//層/ノード数
            DataProc.RegexCheck(s, r);
            //*/








            sw.Stop();
            Console.WriteLine("\r\n\r\n\r\n{0} ms\r\n{1} ticks", sw.ElapsedMilliseconds, sw.ElapsedTicks);
            sw.Reset();
            Console.WriteLine("AllByteCT={0}\tAllByte={1}\r\nTotMem={2}", GC.GetAllocatedBytesForCurrentThread(), GC.GetTotalAllocatedBytes(), GC.GetTotalMemory(true));
        }
    }
}
