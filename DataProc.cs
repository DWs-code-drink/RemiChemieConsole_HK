using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RemiChemieConsole
{
    static internal class DataProc
    {
        static private Encoding[] ecn = new Encoding[6] { Encoding.UTF8, Encoding.Unicode, Encoding.ASCII, Encoding.BigEndianUnicode, Encoding.UTF7, Encoding.UTF32 };
        static private Regex rcsv = new Regex(@"^(?:\s*?(?>([^,]+))\s*?,?)+$");
        static internal void CSVRdouble(in string fp, out string[] dn, out double[][] dt, out int[] di, out int Dtn)//CSVファイル読み(double)、fpはファイルパス、dnはデータ名、dtはデータ、diはデータインデックス
        {
            if (!File.Exists(fp)) throw new FileNotFoundException("fp", "CSVRdouble : File not exists.");
            dn = null;
            dt = null;
            di = null;
            Dtn = -1;
            using (FileStream fs = new FileStream(fp, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                bool flg = false;
                bool flg2;
                string s;
                Match m;
                int itemp, cnt, ivf, cnt2;
                List<string> nl = new List<string>();
                List<int> il = new List<int>();
                List<double>[] dl;
                ParallelOptions po = new ParallelOptions();
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                po.CancellationToken = ct;
                po.MaxDegreeOfParallelism = CommonParam.thdn;
                po.TaskScheduler = TaskScheduler.Default;
                for (int enci = 0; enci < ecn.Length; enci++)
                {
                    try
                    {
                        using (StreamReader sr = new StreamReader(fs, ecn[enci], false, 128, false))
                        {
                            s = sr.ReadLine();
                            m = rcsv.Match(s);
                            if (!m.Success) continue;
                            itemp = m.Groups[1].Captures.Count;
                            string stemp;
                            for (cnt = 0; cnt < itemp; cnt++)
                            {
                                stemp = m.Groups[1].Captures[cnt].Value.Trim();
                                if (stemp == "")
                                {
                                    if (itemp != cnt + 1) throw new ArgumentOutOfRangeException("itemp/cnt", "CSVRDouble : Invalid name.");
                                    itemp = cnt;
                                    break;
                                }
                                nl.Add(stemp);
                                il.Add(cnt);
                            }
                            dl = new List<double>[itemp];
                            for (cnt = 0; cnt < itemp; cnt++)
                            {
                                dl[cnt] = new List<double>();
                            }
                            s = sr.ReadLine();
                            if (s == null) continue;
                            m = rcsv.Match(s);
                            if (!m.Success) continue;
                            ivf = 0;
                            flg2 = false;
                            while (s != null)
                            {
                                if (itemp != m.Groups[1].Captures.Count && itemp + 1 != m.Groups[1].Captures.Count)
                                {
                                    flg2 = true;
                                    break;
                                }
                                try
                                {
                                    for (cnt2 = 0; cnt2 < itemp; cnt2++)
                                    {
                                        if (cts.IsCancellationRequested) return;
                                        try
                                        {
                                            double dtemp;
                                            if (!double.TryParse(m.Groups[1].Captures[cnt2].Value.Trim(), out dtemp))
                                            {
                                                cts.Cancel();
                                                return;
                                            }
                                            dl[cnt2].Add(dtemp);
                                        }
                                        catch (Exception)
                                        {
                                            cts.Cancel();
                                            return;
                                        }
                                    }
                                }
                                catch (TaskCanceledException)
                                {
                                    flg2 = true;
                                    break;
                                }
                                ivf++;
                                s = sr.ReadLine();
                                if (s == null) break;
                                m = rcsv.Match(s);
                                if (!m.Success)
                                {
                                    flg2 = true;
                                    break;
                                }
                            }
                            if (flg2) continue;
                            dn = nl.ToArray();
                            di = il.ToArray();
                            double[][] dtt = new double[itemp][];
                            try
                            {
                                Parallel.For(0, itemp, po, (ind) =>
                                {
                                    if (dl[ind].Count != ivf)
                                    {
                                        cts.Cancel();
                                        return;
                                    }
                                    dtt[ind] = dl[ind].ToArray();
                                });
                            }
                            catch (TaskCanceledException)
                            {
                                throw new ArgumentOutOfRangeException("dl/ivf", "CSVRdouble : Can not verify data length.");
                            }
                            dt = dtt;
                            Dtn = ivf;
                            flg = true;
                        }
                    }
                    catch (Exception) { continue; }
                    if (flg) break;
                }
                if (!flg) throw new FileLoadException("fs", "CSVRdouble : Unknown encoding.");
            }
        }
        static internal void CSVRdoubleJ(in string fp, out string[] dn, out double[][] dt, out int[] di, out string[][] mdt, out int[] mdi, out int Dtn)//CSVファイル読み(double)、fpはファイルパス、dnはデータ名、dtはデータ、diはデータインデックス、mdtは非doubleデータ、mdiは非doubleインデックス
        {
            if (!File.Exists(fp)) throw new FileNotFoundException("fp", "CSVRdoubleJ : File not exists.");
            dn = null;
            dt = null;
            di = null;
            mdt = null;
            mdi = null;
            Dtn = -1;
            using (FileStream fs = new FileStream(fp, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                bool flg = false;
                bool flg2;
                bool flg3;
                string s;
                Match m;
                double dtemp;
                int itemp, cnt, ivf, ind, mind, mvf;
                object lo = new object();
                List<string> nl = new List<string>();
                List<int> il = new List<int>();
                List<double>[] dl;
                List<List<string>> mdl = new List<List<string>>();
                List<int> mil = new List<int>();
                ParallelOptions po = new ParallelOptions();
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                po.CancellationToken = ct;
                po.MaxDegreeOfParallelism = CommonParam.thdn;
                po.TaskScheduler = TaskScheduler.Default;
                for (int enci = 0; enci < ecn.Length; enci++)
                {
                    try
                    {
                        using (StreamReader sr = new StreamReader(fs, ecn[enci], false, 128, false))
                        {
                            s = sr.ReadLine();
                            m = rcsv.Match(s);
                            if (!m.Success) continue;
                            itemp = m.Groups[1].Captures.Count;
                            string stemp;
                            for (cnt = 0; cnt < itemp; cnt++)
                            {
                                stemp = m.Groups[1].Captures[cnt].Value.Trim();
                                if (stemp == "")
                                {
                                    if (itemp != cnt + 1) throw new ArgumentOutOfRangeException("itemp/cnt", "CSVRDoubleJ : Invalid name.");
                                    itemp = cnt;
                                    break;
                                }
                                nl.Add(stemp);
                                il.Add(cnt);
                            }
                            dl = new List<double>[itemp];
                            for (cnt = 0; cnt < itemp; cnt++)
                            {
                                dl[cnt] = new List<double>();
                            }
                            s = sr.ReadLine();
                            if (s == null) continue;
                            ivf = 0;
                            m = rcsv.Match(s);
                            if (!m.Success) continue;
                            if (itemp != m.Groups[1].Captures.Count && itemp + 1 != m.Groups[1].Captures.Count) { Console.WriteLine("{0}\t{1}", itemp, m.Groups[1].Captures.Count); continue; }
                            mind = 0;
                            for (ind = 0; ind < itemp; ind++)
                            {
                                if (!double.TryParse(m.Groups[1].Captures[ind].Value.Trim(), out dtemp))
                                {
                                    List<string> mdlt = new List<string>();
                                    mil.Add(ind);
                                    mdl.Add(mdlt);
                                    mdl[mind].Add(m.Groups[1].Captures[ind].Value.Trim());
                                    mind++;
                                    dl[ind].Add(double.NaN);
                                }
                                dl[ind].Add(dtemp);
                            }
                            mvf = mind;
                            ivf++;
                            s = sr.ReadLine();
                            flg2 = false;
                            while (s != null)
                            {
                                m = rcsv.Match(s);
                                if (!m.Success)
                                {
                                    flg2 = true;
                                    break;
                                }
                                if (itemp != m.Groups[1].Captures.Count && itemp + 1 != m.Groups[1].Captures.Count)
                                {
                                    flg2 = true;
                                    break;
                                }
                                flg3 = false;
                                mind = 0;
                                for (ind = 0; ind < itemp; ind++)
                                {
                                    if (!double.TryParse(m.Groups[1].Captures[ind].Value, out dtemp))
                                    {
                                        if (mind >= mil.Count || mil[mind] != ind)
                                        {
                                            flg3 = true;
                                            break;
                                        }
                                        mdl[mind].Add(m.Groups[1].Captures[ind].Value);
                                        mind++;
                                        dl[ind].Add(double.NaN);
                                    }
                                    dl[ind].Add(dtemp);
                                }
                                if (flg3)
                                {
                                    flg2 = true;
                                    break;
                                }
                                if (mind != mvf) throw new ArgumentOutOfRangeException("mind/mvf", "CSVRdoubleJ ; Can not verify string length.");
                                ivf++;
                                s = sr.ReadLine();
                            }
                            if (flg2) continue;
                            dn = nl.ToArray();
                            di = il.ToArray();
                            mdi = mil.ToArray();
                            double[][] dtt = new double[itemp][];
                            string[][] mdtt = new string[mvf][];
                            try
                            {
                                Parallel.For(0, itemp, po, (ind) =>
                                {
                                    if (dl[ind].Count != ivf)
                                    {
                                        cts.Cancel();
                                        return;
                                    }
                                    dtt[ind] = dl[ind].ToArray();
                                });
                                Parallel.For(0, mvf, po, (ind) =>
                                {
                                    if (mdl[ind].Count != ivf)
                                    {
                                        cts.Cancel();
                                        return;
                                    }
                                    mdtt[ind] = mdl[ind].ToArray();
                                });
                            }
                            catch (TaskCanceledException)
                            {
                                throw new ArgumentOutOfRangeException("dl/mdl/ivf", "CSVRdoubleJ : Can not verify data length.");
                            }
                            dt = dtt;
                            mdt = mdtt;
                            Dtn = ivf;
                            flg = true;
                        }
                    }
                    catch (Exception) { continue; }
                    if (flg) break;
                }
                if (!flg) throw new FileLoadException("fs", "CSVRdoubleJ : Unknown encoding.");
            }
        }
        static internal double[] StdSc(in double[] din, in Tuple<double, double> nc)//標準スコア、nc(平均、標準偏差)
        {
            if (nc.Item2 == 0.0 || double.IsNaN(nc.Item1) || double.IsInfinity(nc.Item1) || double.IsNaN(nc.Item2) || double.IsInfinity(nc.Item2)) throw new ArgumentOutOfRangeException("nc", "StdSc : Incorrect parameters.");
            double[] dou = new double[din.Length];
            for (int cnt = 0; cnt < din.Length; cnt++)
            {
                dou[cnt] = (din[cnt] - nc.Item1) / nc.Item2;
            }
            return dou;
        }
        static internal bool SkkSC(in double[] din, out double[] dou, out Tuple<double, double> nc)//標準スコア正規化
        {
            dou = null;
            nc = null;
            if (din == null || din.Length == 0) return false;
            double A = 0.0;
            int cnt;
            for (cnt = 0; cnt < din.Length; cnt++)
            {
                A += din[cnt];
            }
            A /= din.Length;
            if (double.IsNaN(A) || double.IsInfinity(A)) return false;
            double D = 0.0;
            double dtemp;
            for (cnt = 0; cnt < din.Length; cnt++)
            {
                dtemp = din[cnt] - A;
                D += dtemp * dtemp;
            }
            D /= din.Length;
            if (D == 0.0 || double.IsNaN(D) || double.IsInfinity(D)) return false;
            nc = new Tuple<double, double>(A, Math.Sqrt(D));
            try
            {
                dou = StdSc(in din, in nc);
            }
            catch (ArgumentOutOfRangeException)
            {
                dou = null;
                return false;
            }
            return true;
        }
        static internal bool THNSkk(in double[] din, out double[] dou, out Tuple<double, double> nc)//東方ネットワーク用正規化
        {
            nc = null;
            if (din == null || din.Length == 0)
            {
                dou = null;
                nc = null;
                return false;
            }
            double A = 0.0;
            int cnt;
            for (cnt = 0; cnt < din.Length; cnt++)
            {
                A += din[cnt];
            }
            A /= din.Length;
            if (double.IsNaN(A) || double.IsInfinity(A))
            {
                dou = null;
                nc = null;
                return false;
            }
            double D = 0.0;
            double dtemp;
            for (cnt = 0; cnt < din.Length; cnt++)
            {
                dtemp = din[cnt] - A;
                D += dtemp * dtemp;
            }
            D /= din.Length;
            if (D == 0.0 || double.IsNaN(D) || double.IsInfinity(D))
            {
                dou = null;
                nc = null;
                return false;
            }
            D = Math.Sqrt(D);
            double r = Math.Abs(A) / D;
            bool c = false, s = false;
            if (double.IsNaN(r) || double.IsInfinity(r))
            {
                dou = null;
                nc = null;
                return false;
            }
            else if (r >= 10.0)
            {
                r = Math.Exp(r / 5.0 - 2.0);
                if (double.IsNaN(r) || double.IsInfinity(r)) c = true;
                else
                {
                    Random rnd = new Random();
                    r = 1 / r;
                    double drnd = rnd.NextDouble();
                    if (drnd > r) c = true;
                }
            }
            if (D < 0.1)
            {
                double l = -Math.Log10(D);
                if (l < 0 || double.IsNaN(l) || double.IsInfinity(l))
                {
                    dou = null;
                    nc = null;
                    return false;
                }
                else
                {
                    l = 1 / (l + 1);
                    Random rnd = new Random();
                    double drnd = rnd.NextDouble();
                    if (drnd > l) s = true;
                }
            }
            else if (D > 10.0)
            {
                double l = Math.Log10(D);
                if (l < 0 || double.IsNaN(l) || double.IsInfinity(l))
                {
                    dou = null;
                    nc = null;
                    return false;
                }
                else
                {
                    l = 1 / (l + 1);
                    Random rnd = new Random();
                    double drnd = rnd.NextDouble();
                    if (drnd > l) s = true;
                }
            }
            if (c && s)
            {
                nc = new Tuple<double, double>(A, D);
                try
                {
                    dou = StdSc(in din, in nc);
                }
                catch (ArgumentOutOfRangeException)
                {
                    dou = null;
                    return false;
                }
            }
            else if (!c && s)
            {
                nc = new Tuple<double, double>(0.0, D);
                try
                {
                    dou = StdSc(in din, in nc);
                }
                catch (ArgumentOutOfRangeException)
                {
                    dou = null;
                    return false;
                }
            }
            else if (c && !s)
            {
                nc = new Tuple<double, double>(A, 1.0);
                try
                {
                    dou = StdSc(in din, in nc);
                }
                catch (ArgumentOutOfRangeException)
                {
                    dou = null;
                    return false;
                }
            }
            else
            {
                dou = null;
                nc = new Tuple<double, double>(0.0, 1.0);
                return false;
            }
            return true;
        }
        static internal bool MbzFg(out string msg, in double[] y, out double[] gdo, out double[] gdt, out int[] Dti, in int[][] ind, in int[] T)//目標データ分割、`yは全真データ、gdoは学習真データ、gdtはテスト真データ、Dtiはテストデータインデックス、indは選択可能なインデックスセット、Tはセット毎に取り出すテストインデックス
        {
            msg = null;
            gdo = null;
            gdt = null;
            Dti = null;
            if (ind.Length == 0 || y.Length == 0 || T.Length == 0)
            {
                msg = "MbzFg (ind/y/T) : No choice to make.";
                return false;
            }
            if (ind.Length != T.Length)
            {
                msg = "MbzFg (ind/T) : Not equal length.";
                return false;
            }
            int l = 0;
            int cnt, cnt2, cnt3;
            for (cnt = 0; cnt < T.Length; cnt++)
            {
                if (ind[cnt].Length < T[cnt])
                {
                    msg = "MbzFg (ind/T) : Not enough choice.";
                    return false;
                }
                l += T[cnt];
            }
            gdt = new double[l];
            l = y.Length - l;
            gdo = new double[l];
            Random rnd = new Random();
            List<int> indl;
            List<int> Dtil = new List<int>();
            for (cnt = 0; cnt < T.Length; cnt++)
            {
                indl = new List<int>(ind[cnt]);
                for (cnt2 = 0; cnt2 < T[cnt]; cnt2++)
                {
                    cnt3 = rnd.Next(indl.Count);
                    Dtil.Add(indl[cnt3]);
                    indl.RemoveAt(cnt3);
                }
            }
            if (Dtil.Count != gdt.Length)
            {
                msg = "MbzFg (Dtil) : Can not verify index list length.";
                return false;
            }
            Dtil.Sort();
            Dti = Dtil.ToArray();
            cnt2 = 0;
            cnt3 = 0;
            for (cnt = 0; cnt < y.Length; cnt++)
            {
                if (cnt2 >= Dti.Length || cnt != Dti[cnt2])
                {
                    gdo[cnt3] = y[cnt];
                    cnt3++;
                }
                else
                {
                    gdt[cnt2] = y[cnt];
                    cnt2++;
                }
            }
            if (cnt2 != gdt.Length || cnt3 != gdo.Length || (cnt2 + cnt3) != y.Length)
            {
                msg = "MbzFg (cnt2/cnt3) : Can not verify array length.";
                return false;
            }
            msg = "Successfully separated data.";
            return true;
        }
        static internal bool DesStR(out string msg, in int ln0, in double[][] desL, in int[] desI, in int[] Dti, out double[][] desin, out double[][] dest, out int[] Di, out double[] Dc, out double[] Dn, in byte thnn)//記述子選択(ランダム)、ln0は入力記述子数、desLは全記述子列、desIは全記述子インデックス、Dtiはテストデータインデックス、destは入力記述子列、destはテスト記述子、Diは入力記述子インデックス、Diは記述子インデックス、Dcは記述子中心化係数、thnn 0は正規化しない,1は標準スコア正規化,2は東方ネットワーク正規化
        {
            msg = null;
            desin = null;
            dest = null;
            Di = null;
            Dc = null;
            Dn = null;
            if (ln0 > desL.Length)
            {
                msg = "DesStR (ln0) : Not enough descriptors.";
                return false;
            }
            if (desL.Length != desI.Length)
            {
                msg = "DesStR (desL/desI) : Can not verify descriptor length.";
                return false;
            }
            Random rnd = new Random();
            desin = new double[ln0][];
            Di = new int[ln0];
            Dc = new double[ln0];
            Dn = new double[ln0];
            dest = new double[ln0][];
            List<int> dil = new List<int>(desI);
            double[] dint, dtt;
            int cnt = 0;
            int dic = desL[0].Length - Dti.Length;
            int cnt2, cnt3, cnt4, cnt5;
            double[] destp = null;
            Tuple<double, double> ttemp = null;
            bool flg = false;
            while (cnt < ln0)
            {
                if (dil.Count == 0)
                {
                    msg = "DesStR (dil) : Not enough suitable descriptors.";
                    return false;
                }
                cnt2 = rnd.Next(dil.Count);
                switch (thnn)
                {
                    case 0:
                        {
                            bool flg2 = false;
                            destp = new double[desL[0].Length];
                            ttemp = new Tuple<double, double>(0.0, 1.0);
                            flg = false;
                            for (cnt3 = 0; cnt3 < desL[0].Length; cnt3++)
                            {
                                if (desL[dil[cnt2]][cnt3] != desL[dil[cnt2]][0]) flg2 = true;
                                destp[cnt3] = desL[dil[cnt2]][cnt3];
                            }
                            if (!flg2) ttemp = null;
                            break;
                        }
                    case 1:
                        {
                            flg = SkkSC(in desL[dil[cnt2]], out destp, out ttemp);
                            if (!flg) ttemp = null;
                            break;
                        }
                    case 2:
                        {
                            flg = THNSkk(in desL[dil[cnt2]], out destp, out ttemp);
                            break;
                        }
                    default:
                        {
                            msg = "DesStR (thnn) : Unexpected Touhou Network normalization type.";
                            return false;
                        }
                }
                if (!flg && ttemp == null)
                {
                    dil.RemoveAt(cnt2);
                    continue;
                }
                if (!flg)
                {
                    dint = new double[dic];
                    dtt = new double[Dti.Length];
                    cnt3 = 0;
                    cnt4 = 0;
                    for (cnt5 = 0; cnt5 < desL[dil[cnt2]].Length; cnt5++)
                    {
                        if (cnt3 >= Dti.Length || cnt5 != Dti[cnt3])
                        {
                            dint[cnt4] = desL[dil[cnt2]][cnt5];
                            cnt4++;
                        }
                        else
                        {
                            dtt[cnt3] = desL[dil[cnt2]][cnt5];
                            cnt3++;
                        }
                    }
                    if (desL[dil[cnt2]].Length != desL[0].Length || cnt4 != dic || cnt3 != Dti.Length || (cnt3 + cnt4) != desL[dil[cnt2]].Length) throw new ArgumentOutOfRangeException("desL/cnt4/cnt3", "DesStRTn : Can not verify array length.");
                    desin[cnt] = dint;
                    dest[cnt] = dtt;
                    if (ttemp == null || ttemp.Item1 != 0.0 || ttemp.Item2 != 1.0)
                    {
                        msg = "DesStR (ttemp) : Unexpected value.";
                    }
                    Dc[cnt] = ttemp.Item1;
                    Dn[cnt] = ttemp.Item2;
                }
                else
                {
                    dint = new double[dic];
                    dtt = new double[Dti.Length];
                    cnt3 = 0;
                    cnt4 = 0;
                    for (cnt5 = 0; cnt5 < destp.Length; cnt5++)
                    {
                        if (cnt3 >= Dti.Length || cnt5 != Dti[cnt3])
                        {
                            dint[cnt4] = destp[cnt5];
                            cnt4++;
                        }
                        else
                        {
                            dtt[cnt3] = destp[cnt5];
                            cnt3++;
                        }
                    }
                    if (destp.Length != desL[0].Length || cnt4 != dic || cnt3 != Dti.Length || (cnt3 + cnt4) != destp.Length) throw new ArgumentOutOfRangeException("desL/cnt4/cnt3", "DesStRTn : Can not verify array length.");
                    desin[cnt] = dint;
                    dest[cnt] = dtt;
                    if (ttemp == null || ttemp.Item2 <= 0.0 || double.IsNaN(ttemp.Item1) || double.IsNaN(ttemp.Item2) || double.IsInfinity(ttemp.Item1) || double.IsInfinity(ttemp.Item2)) throw new ArgumentOutOfRangeException("ttemp", "DesStRTn : Unexpected value.");
                    Dc[cnt] = ttemp.Item1;
                    Dn[cnt] = ttemp.Item2;
                }
                Di[cnt] = dil[cnt2];
                dil.RemoveAt(cnt2);
                cnt++;
            }
            msg = "Descriptor chose successfully.";
            return true;
        }
        static internal bool BfiI(out string msg, in string sdin, out int Fn, out FileInfo fi)//出力ファイル作成
        {
            msg = null;
            Fn = -1;
            fi = null;
            DirectoryInfo di;
            string sdi = sdin;
            if (!Directory.Exists(sdi))
            {
                msg = "BfiI (sdin) : Can not find directory.";
                return false;
            }
            else di = new DirectoryInfo(sdi);
            if ((di.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly || (di.Attributes & FileAttributes.System) == FileAttributes.System)
            {
                msg = "BfiI : Can not access directory.";
                return false;
            }
            int find, fond;
            if (!sdi.EndsWith('\\')) sdi += "\\";
            StringBuilder sb;
            string fdi;
            for (find = 0; ; find++)
            {
                if (find >= 1000)
                {
                    msg = "BfiI (find) : Over 1000 folders.";
                    return false;
                }
                sb = new StringBuilder(sdi);
                sb.Append(string.Format("{0}k\\", find));
                if (Directory.Exists(sb.ToString())) continue;
                else
                {
                    if (find != 0) find--;
                    else
                    {
                        sb = new StringBuilder(sdi);
                        sb.Append(string.Format("{0}k\\", find));
                        Directory.CreateDirectory(sb.ToString());
                    }
                    sb = new StringBuilder(sdi);
                    sb.Append(string.Format("{0}k\\", find));
                    fdi = sb.ToString();
                    for (fond = 0; ; fond++)
                    {
                        if (fond == 1000)
                        {
                            sb = new StringBuilder(sdi);
                            find++;
                            fond = 0;
                            sb.Append(string.Format("{0}k\\", find));
                            Directory.CreateDirectory(sb.ToString());
                            sb.Append(string.Format("{0}.RClog", fond));
                            using (FileStream fs = File.Create(sb.ToString())) { }
                            fi = new FileInfo(sb.ToString());
                            Fn = find * 1000 + fond;
                            msg = "Successfully created logfile.";
                            return true;
                        }
                        sb = new StringBuilder(fdi);
                        sb.Append(string.Format("{0}.RClog", fond));
                        if (File.Exists(sb.ToString())) continue;
                        else
                        {
                            using (FileStream fs = File.Create(sb.ToString())) { }
                            fi = new FileInfo(sb.ToString());
                            Fn = find * 1000 + fond;
                            msg = "Successfully created logfile.";
                            return true;
                        }
                    }
                }
            }
        }
        static internal void RegexCheck(in string s, in Regex r)
        {
            Match m = r.Match(s);
            Console.WriteLine("Success : {0}", m.Success);
            Console.WriteLine("Groups : {0}\r\n\r\n\r\n", m.Groups.Count);
            int cnt, cnt2, itemp;
            double dtemp;
            bool b;
            for (cnt = 0; cnt < m.Groups.Count; cnt++)
            {
                Console.Write("Group : {0}\t\t", cnt);
                Console.WriteLine("Captures : {0}", m.Groups[cnt].Captures.Count);
                for (cnt2 = 0; cnt2 < m.Groups[cnt].Captures.Count; cnt2++)
                {
                    Console.Write("{0}\t", m.Groups[cnt].Captures[cnt2].Value);
                }
                Console.WriteLine();
                Console.WriteLine("Parse:");
                for (cnt2 = 0; cnt2 < m.Groups[cnt].Captures.Count; cnt2++)
                {
                    b = int.TryParse(m.Groups[cnt].Captures[cnt2].Value.Trim(), out itemp);
                    if (b) Console.Write("(i){0}\t", itemp);
                    else
                    {
                        b = double.TryParse(m.Groups[cnt].Captures[cnt2].Value.Trim(), out dtemp);
                        if (b) Console.Write("(d){0}\t", dtemp);
                    }
                }
                Console.WriteLine("\r\n\r\n\r\n\r\n");
            }
        }
        static internal bool DesfZH(out string msg, in DirectoryInfo di)//記述子ファイルを統合する
        {
            if (!di.Exists)
            {
                msg = "Can not find folder.";
                return false;
            }
            msg = string.Empty;
            string fpth, stemp, desntemp, destemp, desn0 = null;
            string dpth = di.FullName;
            double dtemp;
            int cnt, cnt2, cnt3, foind, desl = -1;
            StringBuilder sb, sb2, sbdes = new StringBuilder(), sbdl = new StringBuilder();
            Match m;
            bool flg, flg2;
            sb = new StringBuilder(dpth);
            if (!dpth.EndsWith("\\"))
            {
                sb.Append("\\");
                dpth = sb.ToString();
            }
            for (cnt = 0; cnt < 1000; cnt++)
            {
                sb = new StringBuilder(dpth);
                sb.Append(string.Format("{0}k\\", cnt));
                if (!Directory.Exists(sb.ToString()))
                {
                    if (cnt == 0)
                    {
                        msg = "Can not find descriptor file";
                        return false;
                    }
                    break;
                }
                foind = cnt * 1000;
                for (cnt2 = 0; cnt2 < 1000; cnt2++)
                {
                    sb2 = new StringBuilder(sb.ToString());
                    sb2.Append(string.Format("{0}.csv", cnt2));
                    fpth = sb2.ToString();
                    if (!File.Exists(fpth)) continue;
                    using (FileStream fs = new FileStream(fpth, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        try
                        {
                            using (StreamReader sr = new StreamReader(fs, Encoding.UTF8, true, 256, false))
                            {
                                desntemp = sr.ReadLine();
                                if (desntemp == null || desntemp.Trim() == string.Empty)
                                {
                                    msg = string.Format("{0} : Can not find descriptor name.", fpth);
                                    return false;
                                }
                                if (desn0 == null)
                                {
                                    desn0 = desntemp;
                                }
                                else
                                {
                                    if (desn0 != desntemp)
                                    {
                                        msg = string.Format("{0} : Can not verify descriptor name, not the same as previous file.", fpth);
                                        return false;
                                    }
                                }
                                destemp = sr.ReadLine();
                                while (true)
                                {
                                    stemp = sr.ReadLine();
                                    if (stemp != null && stemp.Trim() != string.Empty)
                                    {
                                        msg = string.Format("{0} : Can not verify file content, unexpected line : {1}.", fpth, stemp);
                                        return false;
                                    }
                                    else
                                    {
                                        if (stemp == null) break;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            msg = string.Format("Can not read descriptor file\r\n{0}", e);
                            return false;
                        }
                    }
                    m = rcsv.Match(destemp);
                    if (!m.Success)
                    {
                        msg = string.Format("{0} : Invalid descriptors.", fpth);
                        return false;
                    }
                    if (desl == -1)
                    {
                        desl = m.Groups[1].Captures.Count;
                    }
                    else
                    {
                        if (m.Groups[1].Captures.Count != desl)
                        {
                            msg = string.Format("{0} : Can not verify descriptor amount.", fpth);
                            return false;
                        }
                    }
                    flg2 = false;
                    for (cnt3 = 0; cnt3 < desl; cnt3++)
                    {
                        stemp = m.Groups[1].Captures[cnt3].Value.Trim();
                        if (stemp != string.Empty)
                        {
                            if (flg2)
                            {
                                msg = string.Format("{0} : Invalid descriptors, unexpected : {1}.", fpth, m.Groups[1].Captures[cnt3].Value);
                                return false;
                            }
                        }
                        if (stemp == string.Empty)
                        {
                            if (!flg2) flg2 = true;
                            continue;
                        }
                        flg = double.TryParse(stemp, out dtemp);
                        if (!flg || double.IsNaN(dtemp))
                        {
                            msg = string.Format("{0} : Invalid descriptors : {1}.", fpth, m.Groups[1].Captures[cnt3].Value);
                            return false;
                        }
                    }
                    sbdes.AppendLine(destemp);
                    sbdl.AppendLine((foind + cnt2).ToString("G"));
                }
            }
            stemp = sbdl.ToString();
            sbdl = new StringBuilder("Index\r\n");
            sbdl.Append(stemp);
            stemp = sbdes.ToString();
            sbdes = new StringBuilder(desn0);
            sbdes.AppendLine();
            sbdes.Append(stemp);
            sb = new StringBuilder(dpth);
            sb.Append("DesIndex.csv");
            using (FileStream fs = new FileStream(sb.ToString(), FileMode.Create, FileAccess.Write, FileShare.None, 256))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8, 256, false))
                {
                    sw.Write(sbdl.ToString());
                }
            }
            sb = new StringBuilder(dpth);
            sb.Append("Descriptors.csv");
            using (FileStream fs = new FileStream(sb.ToString(), FileMode.Create, FileAccess.Write, FileShare.None, 256))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8, 256, false))
                {
                    sw.Write(sbdes.ToString());
                }
            }
            msg = "Successfully read.";
            return true;
        }
    }
    static internal class SKBINOL
    {
        static internal readonly string destOHa = "D:\\ML\\a\\OHdes.csv";
        static internal readonly string destAca = "D:\\ML\\a\\Acdes.csv";
        static internal readonly string destPha = "D:\\ML\\a\\Phdes.csv";
        static internal readonly string destOHb = "D:\\ML\\b\\OHdes.csv";
        static internal readonly string destAcb = "D:\\ML\\b\\Acdes.csv";
        static internal readonly string destPhb = "D:\\ML\\b\\Phdes.csv";
        static internal readonly string resa = "D:\\ML\\a\\alphares.csv";
        static internal readonly string resb = "D:\\ML\\b\\betares.csv";
        ///*
        static internal readonly string SaeOH = "D:\\ML\\Run1\\aeh\\OH";
        static internal readonly string SaeAc = "D:\\ML\\Run1\\aeh\\Ac";
        static internal readonly string SaePh = "D:\\ML\\Run1\\aeh\\Ph";
        //*/
        /* 
        static internal readonly string SaeOH = "D:\\ML\\Run1\\aer\\OH";
        static internal readonly string SaeAc = "D:\\ML\\Run1\\aer\\Ac";
        static internal readonly string SaePh = "D:\\ML\\Run1\\aer\\Ph";
        //*/
        /* 
        static internal readonly string SaeOH = "D:\\ML\\Run1\\aes\\OH";
        static internal readonly string SaeAc = "D:\\ML\\Run1\\aes\\Ac";
        static internal readonly string SaePh = "D:\\ML\\Run1\\aes\\Ph";
        //*/
        static internal readonly string SayOH = "D:\\ML\\Run1\\ay\\OH";
        static internal readonly string SayAc = "D:\\ML\\Run1\\ay\\Ac";
        static internal readonly string SayPh = "D:\\ML\\Run1\\ay\\Ph";
        static internal readonly string SbyOH = "D:\\ML\\Run1\\by\\OH";
        static internal readonly string SbyAc = "D:\\ML\\Run1\\by\\Ac";
        static internal readonly string SbyPh = "D:\\ML\\Run1\\by\\Ph";
        static internal readonly int[] rayh = new int[3] { 2, 9, 11 };
        static internal readonly int[] raym = new int[3] { 15, 17, 27 };
        static internal readonly int[] rayl = new int[3] { 21, 24, 26 };
        static internal readonly int[] raeh = new int[3] { 9, 11, 22 };
        static internal readonly int[] raem = new int[4] { 0, 21, 28, 29 };
        static internal readonly int[] rael = new int[5] { 3, 7, 16, 20, 27 };
        static internal readonly int[] rbyh = new int[5] { 2, 11, 18, 22, 32 };
        static internal readonly int[] rbym = new int[4] { 4, 16, 28, 29 };
        static internal readonly int[] rbyl = new int[3] { 3, 20, 25 };
        static internal readonly int ayi = 2;
        static internal readonly int aei = 8;
        //static internal readonly int aei = 6;
        //static internal readonly int aei = 9;
        static internal readonly int byi = 3;
        static internal void BINOL(bool? j, bool? t)//jnullαee%falseα収率trueβ収率、tnullOHfalseActruePh
        {
            string[] desN;
            double[][] desL;
            int[] desI;
            int Dtn;
            try//記述子を読み込む
            {
                if (t == null)
                {
                    if (j == true) DataProc.CSVRdouble(in destOHb, out desN, out desL, out desI, out Dtn);
                    else DataProc.CSVRdouble(in destOHa, out desN, out desL, out desI, out Dtn);
                }
                else if (t == false)
                {
                    if (j == true) DataProc.CSVRdouble(in destAcb, out desN, out desL, out desI, out Dtn);
                    else DataProc.CSVRdouble(in destAca, out desN, out desL, out desI, out Dtn);
                }
                else
                {
                    if (j == true) DataProc.CSVRdouble(in destPhb, out desN, out desL, out desI, out Dtn);
                    else DataProc.CSVRdouble(in destPha, out desN, out desL, out desI, out Dtn);
                }
            }
            catch (FileNotFoundException fnfe)
            {
                Console.WriteLine("File Not Found => {0}", fnfe.FileName);
                Console.WriteLine(fnfe.Message);
                return;
            }
            catch (FileLoadException fle)
            {
                Console.WriteLine("Fail reading => {0}", fle.FileName);
                Console.WriteLine(fle.Message);
                return;
            }
            catch (ArgumentOutOfRangeException aore)
            {
                Console.WriteLine("Fail reading => {0}", aore.ParamName);
                Console.WriteLine(aore.Message);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (e.InnerException != null) Console.WriteLine(e.InnerException);
                return;
            }
            double[] y;
            try//実験データを読み込む
            {
                if (j == null)
                {
                    string[] yN;
                    double[][] yL;
                    int[] yi, iaatemp;
                    string[][] saatemp;
                    int itemp;
                    DataProc.CSVRdoubleJ(in resa, out yN, out yL, out yi, out saatemp, out iaatemp, out itemp);
                    y = yL[aei];
                    if (itemp != Dtn)
                    {
                        Console.WriteLine("Can not verify data length.");
                    }
                }
                else if (j == false)
                {
                    string[] yN;
                    double[][] yL;
                    int[] yi, iaatemp;
                    string[][] saatemp;
                    int itemp;
                    DataProc.CSVRdoubleJ(in resa, out yN, out yL, out yi, out saatemp, out iaatemp, out itemp);
                    y = yL[ayi];
                    if (itemp != Dtn)
                    {
                        Console.WriteLine("Can not verify data length.");
                    }
                }
                else
                {
                    string[] yN;
                    double[][] yL;
                    int[] yi, iaatemp;
                    string[][] saatemp;
                    int itemp;
                    DataProc.CSVRdoubleJ(in resb, out yN, out yL, out yi, out saatemp, out iaatemp, out itemp);
                    y = yL[byi];
                    if (itemp != Dtn)
                    {
                        Console.WriteLine("Can not verify data length.");
                    }
                }
            }
            catch (FileNotFoundException fnfe)
            {
                Console.WriteLine("File Not Found => {0}", fnfe.FileName);
                Console.WriteLine(fnfe.Message);
                return;
            }
            catch (FileLoadException fle)
            {
                Console.WriteLine("Fail reading => {0}", fle.FileName);
                Console.WriteLine(fle.Message);
                return;
            }
            catch (ArgumentOutOfRangeException aore)
            {
                Console.WriteLine("Fail reading => {0}", aore.ParamName);
                Console.WriteLine(aore.Message);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (e.InnerException != null) Console.WriteLine(e.InnerException);
                return;
            }
            double[][] gdo = new double[1][];//真データ
            double[][] gdt = new double[1][];//テスト真データ
            int[] Dti;//テストデータインデックス
            int[][] ind = new int[3][];
            int D = Dtn - ind.Length;
            if (j == null)
            {
                ind[0] = raeh;
                ind[1] = raem;
                ind[2] = rael;
            }
            else if (j == false)
            {
                ind[0] = rayh;
                ind[1] = raym;
                ind[2] = rayl;
            }
            else
            {
                ind[0] = rbyh;
                ind[1] = rbym;
                ind[2] = rbyl;
            }
            int[] T = new int[3] { 1, 1, 1 };
            DataProc.MbzFg(out string msg, in y, out gdo[0], out gdt[0], out Dti, in ind, in T);//真データを分割する
            double[][] desin;//入力ノードのデータ
            int[] Di;//記述子インデックス
            double[] Dc;//記述子中心化係数
            double[] Dn;//記述子正規化スケール
            double[][] dest;//テスト記述子
            DataProc.DesStR(out msg, 50, in desL, in desI, in Dti, out desin, out dest, out Di, out Dc, out Dn, 2);//記述子をTHN正規化してから分割する
            Random rnd = new Random();
            int lyn = rnd.Next(3) + 3;//層数
            int[] ln;//ノード数
            Tuple<int, int>[] Tcn;//層の連結数範囲
            double[][] Fwp;//超層連結率
            int[][] Nfp;//関数確率
            if (lyn == 3)
            {
                ln = new int[3] { 50, 20, 1 };
                Tcn = new Tuple<int, int>[2];
                Tcn[0] = new Tuple<int, int>(10, 15);
                Tcn[1] = new Tuple<int, int>(20, 20);
                Fwp = new double[2][];
                Fwp[0] = new double[1] { 1.0 };
                Fwp[1] = new double[2] { 0.05, 1.0 };
                Nfp = new int[3][];
                Nfp[0] = new int[1] { 9 };
                //Nfp[1] = new int[1] { 3 };
                Nfp[1] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                Nfp[2] = new int[1] { 9 };
            }
            else if (lyn == 4)
            {
                ln = new int[4] { 50, 35, 20, 1 };
                Tcn = new Tuple<int, int>[3];
                Tcn[0] = new Tuple<int, int>(10, 15);
                Tcn[1] = new Tuple<int, int>(13, 17);
                Tcn[2] = new Tuple<int, int>(20, 20);
                Fwp = new double[3][];
                Fwp[0] = new double[1] { 1.0 };
                Fwp[1] = new double[2] { 0.05, 1.0 };
                Fwp[2] = new double[3] { 0.02, 0.05, 1.0 };
                Nfp = new int[4][];
                Nfp[0] = new int[1] { 9 };
                //Nfp[1] = new int[1] { 3 };
                //Nfp[2] = new int[1] { 3 };
                Nfp[1] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                Nfp[2] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                Nfp[3] = new int[1] { 9 };
            }
            else if (lyn == 5)
            {
                ln = new int[5] { 50, 40, 30, 20, 1 };
                Tcn = new Tuple<int, int>[4];
                Tcn[0] = new Tuple<int, int>(10, 15);
                Tcn[1] = new Tuple<int, int>(12, 17);
                Tcn[2] = new Tuple<int, int>(15, 20);
                Tcn[3] = new Tuple<int, int>(20, 20);
                Fwp = new double[4][];
                Fwp[0] = new double[1] { 1.0 };
                Fwp[1] = new double[2] { 0.05, 1.0 };
                Fwp[2] = new double[3] { 0.02, 0.05, 1.0 };
                Fwp[3] = new double[4] { 0.02, 0.025, 0.05, 1.0 };
                Nfp = new int[5][];
                Nfp[0] = new int[1] { 9 };
                //Nfp[1] = new int[1] { 3 };
                //Nfp[2] = new int[1] { 3 };
                //Nfp[3] = new int[1] { 3 };
                Nfp[1] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                Nfp[2] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                Nfp[3] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                Nfp[4] = new int[1] { 9 };
            }
            else
            {
                Console.WriteLine("Unexpected layer number.");
                return;
            }
            double[][] cdr = new double[1][];
            Tuple<double, double>[][] err = new Tuple<double, double>[1][];
            ///*
            if (j == null)
            {
                cdr[0] = new double[5] { -3.0, 3.0, 10.0, 50.0, 200.0 };
                err[0] = new Tuple<double, double>[5];
                err[0][0] = new Tuple<double, double>(3.0, -3.0);
                err[0][1] = new Tuple<double, double>(-6.0, 6.0);
                err[0][2] = new Tuple<double, double>(4.5, -4.5);
                err[0][3] = new Tuple<double, double>(3.5, -3.5);
                err[0][4] = new Tuple<double, double>(2.5, -2.5);
            }
            //*/
            /*
            if (j == null)
            {
                cdr[0] = new double[5] { -3.0, 3.0, 10.0, 30.0, 100.0 };
                err[0] = new Tuple<double, double>[5];
                err[0][0] = new Tuple<double, double>(4.5, -4.5);
                err[0][1] = new Tuple<double, double>(-6.0, 6.0);
                err[0][2] = new Tuple<double, double>(4.5, -4.5);
                err[0][3] = new Tuple<double, double>(3.5, -3.5);
                err[0][4] = new Tuple<double, double>(2.5, -2.5);
            }
            //*/
            else if (j == false)
            {
                cdr[0] = new double[3] { 4.0, 20.0, 100.0 };
                err[0] = new Tuple<double, double>[3];
                err[0][0] = new Tuple<double, double>(-5.0, 8.0);
                err[0][1] = new Tuple<double, double>(3.0, -3.0);
                err[0][2] = new Tuple<double, double>(2.5, -2.5);
            }
            else
            {
                cdr[0] = new double[3] { 3.0, 15.0, 100.0 };
                err[0] = new Tuple<double, double>[3];
                err[0][0] = new Tuple<double, double>(-3.0, 6.0);
                err[0][1] = new Tuple<double, double>(3.0, -3.0);
                err[0][2] = new Tuple<double, double>(2.5, -2.5);
            }
            int NK = 34343;//学習回数
            double G = 0.00514;//学習率
            FileInfo fi;//記録ファイ
            int Fn;
            string sdi;
            string ResFP;//真データファイルパス
            int[] Resi;//真データインデックス
            string DesFP;//記述子ファイルパス

            if (j == null)
            {
                ResFP = resa;
                Resi = new int[1] { aei };
                if (t == null)
                {
                    sdi = SaeOH;
                    DesFP = destOHa;
                }
                else if (t == false)
                {
                    sdi = SaeAc;
                    DesFP = destAca;
                }
                else
                {
                    sdi = SaePh;
                    DesFP = destPha;
                }
            }
            else if (j == false)
            {
                ResFP = resa;
                Resi = new int[1] { ayi };
                if (t == null)
                {
                    sdi = SayOH;
                    DesFP = destOHa;
                }
                else if (t == false)
                {
                    sdi = SayAc;
                    DesFP = destAca;
                }
                else
                {
                    sdi = SayPh;
                    DesFP = destPha;
                }
            }
            else
            {
                ResFP = resb;
                Resi = new int[1] { byi };
                if (t == null)
                {
                    sdi = SbyOH;
                    DesFP = destOHb;
                }
                else if (t == false)
                {
                    sdi = SbyAc;
                    DesFP = destAcb;
                }
                else
                {
                    sdi = SbyPh;
                    DesFP = destPhb;
                }
            }
            /*
            StringBuilder sb = new StringBuilder();
            sb.Append("SU : ");
            for (int cnt = 0; cnt < Dti.Length; cnt++)
            {
                sb.Append(string.Format("{0}  ", Dti[cnt]));
            }
            sb.AppendLine();
            for (int cnt = 0; cnt < gdo[0].Length; cnt++)
            {
                sb.Append(string.Format("{0}  ", gdo[0][cnt]));
            }
            sb.AppendLine();
            for (int cnt = 0; cnt < gdt[0].Length; cnt++)
            {
                sb.Append(string.Format("{0}  ", gdt[0][cnt]));
            }
            sb.AppendLine();
            Console.WriteLine(sb.ToString());
            for (int cnt = 0; cnt < desin.Length; cnt++)
            {
                sb = new StringBuilder();
                sb.Append("KI : ");
                sb.AppendLine(string.Format("{0} : {1}", Di[cnt], SGKC.Exl2610.DtA(Di[cnt] + 1)));
                sb.AppendLine(Dc[cnt].ToString());
                sb.AppendLine(Dn[cnt].ToString());
                for (int cnt2 = 0; cnt2 < desin[cnt].Length; cnt2++)
                {
                    sb.Append(desin[cnt][cnt2]);
                    sb.Append(" ");
                }
                sb.AppendLine();
                for (int cnt2 = 0; cnt2 < dest[cnt].Length; cnt2++)
                {
                    sb.Append(dest[cnt][cnt2]);
                    sb.Append(" ");
                }
                sb.AppendLine();
                Console.WriteLine(sb.ToString());
            }
            //*///debug用
            DataProc.BfiI(out msg, in sdi, out Fn, out fi);
            int ccc = 0;
            THNetwork.THNM(D, in ln, in Tcn, in Fwp, in Nfp, in desin, in gdo, in cdr, in err, NK, G, in Di, in Dc, in Dn, in fi, 3, in dest, in gdt, in Dti, "Test", in ResFP, in Resi, in DesFP, true, ref ccc);
        }
        static internal void BINOLB(bool? j, bool? t, int b)//jnullαee%falseα収率trueβ収率、tnullOHfalseActruePh、bはバッチジョブ数
        {
            string[] desN;
            double[][] desL;
            int[] desI;
            int Dtn;
            int errcnt = 0;
            DESREAD:
            try//記述子を読み込む
            {
                if (t == null)
                {
                    if (j == true) DataProc.CSVRdouble(in destOHb, out desN, out desL, out desI, out Dtn);
                    else DataProc.CSVRdouble(in destOHa, out desN, out desL, out desI, out Dtn);
                }
                else if (t == false)
                {
                    if (j == true) DataProc.CSVRdouble(in destAcb, out desN, out desL, out desI, out Dtn);
                    else DataProc.CSVRdouble(in destAca, out desN, out desL, out desI, out Dtn);
                }
                else
                {
                    if (j == true) DataProc.CSVRdouble(in destPhb, out desN, out desL, out desI, out Dtn);
                    else DataProc.CSVRdouble(in destPha, out desN, out desL, out desI, out Dtn);
                }
            }
            catch (IndexOutOfRangeException iore)
            {
                Console.WriteLine(iore);
                errcnt++;
                if (errcnt == 10)
                {
                    Console.WriteLine("Can not read file.\r\nPlease restart the program.");
                    return;
                }
                goto DESREAD;
            }
            catch (FileNotFoundException fnfe)
            {
                Console.WriteLine("File Not Found => {0}", fnfe.FileName);
                Console.WriteLine(fnfe.Message);
                return;
            }
            catch (FileLoadException fle)
            {
                Console.WriteLine("Fail reading => {0}", fle.FileName);
                Console.WriteLine(fle.Message);
                return;
            }
            catch (ArgumentOutOfRangeException aore)
            {
                Console.WriteLine("Fail reading => {0}", aore.ParamName);
                Console.WriteLine(aore.Message);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (e.InnerException != null) Console.WriteLine(e.InnerException);
                return;
            }
            double[] y;
            errcnt = 0;
            ERREAD:
            try//実験データを読み込む
            {
                if (j == null)
                {
                    string[] yN;
                    double[][] yL;
                    int[] yi, iaatemp;
                    string[][] saatemp;
                    int itemp;
                    DataProc.CSVRdoubleJ(in resa, out yN, out yL, out yi, out saatemp, out iaatemp, out itemp);
                    y = yL[aei];
                    if (itemp != Dtn)
                    {
                        Console.WriteLine("Can not verify data length.");
                    }
                }
                else if (j == false)
                {
                    string[] yN;
                    double[][] yL;
                    int[] yi, iaatemp;
                    string[][] saatemp;
                    int itemp;
                    DataProc.CSVRdoubleJ(in resa, out yN, out yL, out yi, out saatemp, out iaatemp, out itemp);
                    y = yL[ayi];
                    if (itemp != Dtn)
                    {
                        Console.WriteLine("Can not verify data length.");
                    }
                }
                else
                {
                    string[] yN;
                    double[][] yL;
                    int[] yi, iaatemp;
                    string[][] saatemp;
                    int itemp;
                    DataProc.CSVRdoubleJ(in resb, out yN, out yL, out yi, out saatemp, out iaatemp, out itemp);
                    y = yL[byi];
                    if (itemp != Dtn)
                    {
                        Console.WriteLine("Can not verify data length.");
                    }
                }
            }
            catch (IndexOutOfRangeException iore)
            {
                Console.WriteLine(iore);
                errcnt++;
                if (errcnt == 10)
                {
                    Console.WriteLine("Can not read file.\r\nPlease restart the program.");
                    return;
                }
                goto ERREAD;
            }
            catch (FileNotFoundException fnfe)
            {
                Console.WriteLine("File Not Found => {0}", fnfe.FileName);
                Console.WriteLine(fnfe.Message);
                return;
            }
            catch (FileLoadException fle)
            {
                Console.WriteLine("Fail reading => {0}", fle.FileName);
                Console.WriteLine(fle.Message);
                return;
            }
            catch (ArgumentOutOfRangeException aore)
            {
                Console.WriteLine("Fail reading => {0}", aore.ParamName);
                Console.WriteLine(aore.Message);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (e.InnerException != null) Console.WriteLine(e.InnerException);
                return;
            }
            double[][] gdo = new double[1][];//真データ
            double[][] gdt = new double[1][];//テスト真データ
            int[] Dti;//テストデータインデックス
            int[][] ind = new int[3][];
            int D = Dtn - ind.Length;
            if (j == null)
            {
                ind[0] = raeh;
                ind[1] = raem;
                ind[2] = rael;
            }
            else if (j == false)
            {
                ind[0] = rayh;
                ind[1] = raym;
                ind[2] = rayl;
            }
            else
            {
                ind[0] = rbyh;
                ind[1] = rbym;
                ind[2] = rbyl;
            }
            int[] T = new int[3] { 1, 1, 1 };
            //DataProc.MbzFg(in y, out gdo[0], out gdt[0], out Dti, in ind, in T);//真データを分割する
            double[][] desin;//入力ノードのデータ
            int[] Di;//記述子インデックス
            double[] Dc;//記述子中心化係数
            double[] Dn;//記述子正規化スケール
            double[][] dest;//テスト記述子
                            //DataProc.DesStRTn(50, in desL, in desI, in Dti, out desin, out dest, out Di, out Dc, out Dn);//記述子をTHN正規化してから分割する
            Random rnd = new Random();
            //int lyn = rnd.Next(3) + 3;//層数
            int[] ln;//ノード数
            Tuple<int, int>[] Tcn;//層の連結数範囲
            double[][] Fwp;//超層連結率
            int[][] Nfp;//関数確率
            double[][] cdr = new double[1][];
            Tuple<double, double>[][] err = new Tuple<double, double>[1][];
            /*
            if (j == null)
            {
                cdr[0] = new double[5] { -3.0, 3.0, 10.0, 50.0, 200.0 };
                err[0] = new Tuple<double, double>[5];
                err[0][0] = new Tuple<double, double>(3.0, -3.0);
                err[0][1] = new Tuple<double, double>(-6.0, 6.0);
                err[0][2] = new Tuple<double, double>(3.5, -3.5);
                err[0][3] = new Tuple<double, double>(3.0, -3.0);
                err[0][4] = new Tuple<double, double>(2.5, -2.5);
            }
            //*/
            ///*
            if (j == null)
            {
                cdr[0] = new double[5] { -3.0, 3.0, 10.0, 30.0, 100.0 };
                err[0] = new Tuple<double, double>[5];
                err[0][0] = new Tuple<double, double>(4.5, -4.5);
                err[0][1] = new Tuple<double, double>(-6.0, 6.0);
                err[0][2] = new Tuple<double, double>(4.5, -4.5);
                err[0][3] = new Tuple<double, double>(3.5, -3.5);
                err[0][4] = new Tuple<double, double>(2.5, -2.5);
            }
            //*/
            else if (j == false)
            {
                cdr[0] = new double[3] { 4.0, 20.0, 100.0 };
                err[0] = new Tuple<double, double>[3];
                err[0][0] = new Tuple<double, double>(-5.0, 8.0);
                err[0][1] = new Tuple<double, double>(3.0, -3.0);
                err[0][2] = new Tuple<double, double>(2.5, -2.5);
            }
            else
            {
                cdr[0] = new double[3] { 3.0, 15.0, 100.0 };
                err[0] = new Tuple<double, double>[3];
                err[0][0] = new Tuple<double, double>(-3.0, 6.0);
                err[0][1] = new Tuple<double, double>(3.0, -3.0);
                err[0][2] = new Tuple<double, double>(2.5, -2.5);
            }
            int NK = 34340;//学習回数
            double G = 0.034;//学習率
            FileInfo fi;//記録ファイ
            int Fn;
            string sdi;
            string ResFP;//真データファイルパス
            int[] Resi;//真データインデックス
            string DesFP;//記述子ファイルパス
            if (j == null)
            {
                ResFP = resa;
                Resi = new int[1] { aei };
                if (t == null)
                {
                    sdi = SaeOH;
                    DesFP = destOHa;
                }
                else if (t == false)
                {
                    sdi = SaeAc;
                    DesFP = destAca;
                }
                else
                {
                    sdi = SaePh;
                    DesFP = destPha;
                }
            }
            else if (j == false)
            {
                ResFP = resa;
                Resi = new int[1] { ayi };
                if (t == null)
                {
                    sdi = SayOH;
                    DesFP = destOHa;
                }
                else if (t == false)
                {
                    sdi = SayAc;
                    DesFP = destAca;
                }
                else
                {
                    sdi = SayPh;
                    DesFP = destPha;
                }
            }
            else
            {
                ResFP = resb;
                Resi = new int[1] { byi };
                if (t == null)
                {
                    sdi = SbyOH;
                    DesFP = destOHb;
                }
                else if (t == false)
                {
                    sdi = SbyAc;
                    DesFP = destAcb;
                }
                else
                {
                    sdi = SbyPh;
                    DesFP = destPhb;
                }
            }
            /*
            StringBuilder sb = new StringBuilder();
            sb.Append("SU : ");
            for (int cnt = 0; cnt < Dti.Length; cnt++)
            {
                sb.Append(string.Format("{0}  ", Dti[cnt]));
            }
            sb.AppendLine();
            for (int cnt = 0; cnt < gdo[0].Length; cnt++)
            {
                sb.Append(string.Format("{0}  ", gdo[0][cnt]));
            }
            sb.AppendLine();
            for (int cnt = 0; cnt < gdt[0].Length; cnt++)
            {
                sb.Append(string.Format("{0}  ", gdt[0][cnt]));
            }
            sb.AppendLine();
            Console.WriteLine(sb.ToString());
            for (int cnt = 0; cnt < desin.Length; cnt++)
            {
                sb = new StringBuilder();
                sb.Append("KI : ");
                sb.AppendLine(string.Format("{0} : {1}", Di[cnt], SGKC.Exl2610.DtA(Di[cnt] + 1)));
                sb.AppendLine(Dc[cnt].ToString());
                sb.AppendLine(Dn[cnt].ToString());
                for (int cnt2 = 0; cnt2 < desin[cnt].Length; cnt2++)
                {
                    sb.Append(desin[cnt][cnt2]);
                    sb.Append(" ");
                }
                sb.AppendLine();
                for (int cnt2 = 0; cnt2 < dest[cnt].Length; cnt2++)
                {
                    sb.Append(dest[cnt][cnt2]);
                    sb.Append(" ");
                }
                sb.AppendLine();
                Console.WriteLine(sb.ToString());
            }
            //*///debug用
            for (int bcnt = 0; bcnt < b; bcnt++)
            {
                DataProc.MbzFg(out string msg, in y, out gdo[0], out gdt[0], out Dti, in ind, in T);//真データを分割する
                DataProc.DesStR(out msg, 50, in desL, in desI, in Dti, out desin, out dest, out Di, out Dc, out Dn, 2);//記述子をTHN正規化してから分割する
                int lyn = rnd.Next(3) + 3;//層数
                if (lyn == 3)
                {
                    ln = new int[3] { 50, 20, 1 };
                    Tcn = new Tuple<int, int>[2];
                    Tcn[0] = new Tuple<int, int>(10, 15);
                    Tcn[1] = new Tuple<int, int>(20, 20);
                    Fwp = new double[2][];
                    Fwp[0] = new double[1] { 1.0 };
                    Fwp[1] = new double[2] { 0.05, 1.0 };
                    Nfp = new int[3][];
                    Nfp[0] = new int[1] { 9 };
                    //Nfp[1] = new int[1] { 3 };
                    Nfp[1] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                    Nfp[2] = new int[1] { 9 };
                }
                else if (lyn == 4)
                {
                    ln = new int[4] { 50, 35, 20, 1 };
                    Tcn = new Tuple<int, int>[3];
                    Tcn[0] = new Tuple<int, int>(10, 15);
                    Tcn[1] = new Tuple<int, int>(13, 17);
                    Tcn[2] = new Tuple<int, int>(20, 20);
                    Fwp = new double[3][];
                    Fwp[0] = new double[1] { 1.0 };
                    Fwp[1] = new double[2] { 0.05, 1.0 };
                    Fwp[2] = new double[3] { 0.02, 0.05, 1.0 };
                    Nfp = new int[4][];
                    Nfp[0] = new int[1] { 9 };
                    //Nfp[1] = new int[1] { 3 };
                    //Nfp[2] = new int[1] { 3 };
                    Nfp[1] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                    Nfp[2] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                    Nfp[3] = new int[1] { 9 };
                }
                else if (lyn == 5)
                {
                    ln = new int[5] { 50, 40, 30, 20, 1 };
                    Tcn = new Tuple<int, int>[4];
                    Tcn[0] = new Tuple<int, int>(10, 15);
                    Tcn[1] = new Tuple<int, int>(12, 17);
                    Tcn[2] = new Tuple<int, int>(15, 20);
                    Tcn[3] = new Tuple<int, int>(20, 20);
                    Fwp = new double[4][];
                    Fwp[0] = new double[1] { 1.0 };
                    Fwp[1] = new double[2] { 0.05, 1.0 };
                    Fwp[2] = new double[3] { 0.02, 0.05, 1.0 };
                    Fwp[3] = new double[4] { 0.02, 0.025, 0.05, 1.0 };
                    Nfp = new int[5][];
                    Nfp[0] = new int[1] { 9 };
                    //Nfp[1] = new int[1] { 3 };
                    //Nfp[2] = new int[1] { 3 };
                    //Nfp[3] = new int[1] { 3 };
                    Nfp[1] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                    Nfp[2] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                    Nfp[3] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                    Nfp[4] = new int[1] { 9 };
                }
                else
                {
                    Console.WriteLine("Unexpected layer number.");
                    return;
                }
                DataProc.BfiI(out msg, in sdi, out Fn, out fi);
                int[] Lyn = new int[3] { 3, 4, 5 };
                int[][] LNn = new int[3][];
                LNn[0] = new int[1] { 20 };
                LNn[1] = new int[2] { 35, 20 };
                LNn[2] = new int[3] { 40, 30, 20 };
                Tuple<int, int>[][] ttcn = new Tuple<int, int>[3][];
                ttcn[0] = new Tuple<int, int>[2];
                ttcn[0][0] = new Tuple<int, int>(10, 15);
                ttcn[0][1] = new Tuple<int, int>(20, 20);
                ttcn[1] = new Tuple<int, int>[3];
                ttcn[1][0] = new Tuple<int, int>(10, 15);
                ttcn[1][1] = new Tuple<int, int>(13, 17);
                ttcn[1][2] = new Tuple<int, int>(20, 20);
                ttcn[2] = new Tuple<int, int>[4];
                ttcn[2][0] = new Tuple<int, int>(10, 15);
                ttcn[2][1] = new Tuple<int, int>(12, 17);
                ttcn[2][2] = new Tuple<int, int>(15, 20);
                ttcn[2][3] = new Tuple<int, int>(20, 20);
                double[][][] tfwp = new double[3][][];
                tfwp[0] = new double[2][];
                tfwp[0][0] = new double[1] { 1.0 };
                tfwp[0][1] = new double[2] { 0.05, 1.0 };
                tfwp[1] = new double[3][];
                tfwp[1][0] = new double[1] { 1.0 };
                tfwp[1][1] = new double[2] { 0.05, 1.0 };
                tfwp[1][2] = new double[3] { 0.02, 0.05, 1.0 };
                tfwp[2] = new double[4][];
                tfwp[2][0] = new double[1] { 1.0 };
                tfwp[2][1] = new double[2] { 0.05, 1.0 };
                tfwp[2][2] = new double[3] { 0.02, 0.05, 1.0 };
                tfwp[2][3] = new double[4] { 0.02, 0.025, 0.05, 1.0 };
                int[][][] tnfp = new int[3][][];
                tnfp[0] = new int[3][];
                tnfp[0][0] = new int[1] { 9 };
                tnfp[0][1] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                tnfp[0][2] = new int[1] { 9 };
                tnfp[1] = new int[4][];
                tnfp[1][0] = new int[1] { 9 };
                tnfp[1][1] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                tnfp[1][2] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                tnfp[1][3] = new int[1] { 9 };
                tnfp[2] = new int[5][];
                tnfp[2][0] = new int[1] { 9 };
                tnfp[2][1] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                tnfp[2][2] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                tnfp[2][3] = new int[10] { 500, 2034, 0, 2495, 1013, 178, 310, 515, 630, 1111 };
                tnfp[2][4] = new int[1] { 9 };
                int ccc = 0;
                try
                {
                    //THNetwork.THNJskk("Test", new string[1] { DesFP }, in ResFP, in sdi, in Resi, in T, in ind, new int[1] { 50 }, in cdr, in err, in NK, in G, (byte)2);
                    //THNetwork.THNHset(in sdi, in Lyn, in LNn, in ttcn, in tfwp, in tnfp);
                    THNetwork.THNM(D, in ln, in Tcn, in Fwp, in Nfp, in desin, in gdo, in cdr, in err, NK, G, in Di, in Dc, in Dn, in fi, 3, in dest, in gdt, in Dti, "Test", in ResFP, in Resi, in DesFP, true, ref ccc);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    if (e.InnerException != null) Console.WriteLine(e.InnerException);
                }
            }
        }
    }
}
