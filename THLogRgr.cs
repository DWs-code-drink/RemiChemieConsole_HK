using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OYTamn
{
    internal class THLogRgr//東方ロジスティック回帰
    {
        private readonly int Dtn;//データ数
        private readonly THLRFunc[] df;//THLR関数
        private double sf;//倍率
        private double py;//偏移
        private double[] wF;//関数係数
        private double[] pdwF;//関数係数偏微分
        private readonly int[] 記述子番号;
        private readonly int[] 関数番号;
        private double wB;//バイアス係数
        private double pdwB;//バイアス係数偏微分
        private readonly double[] ytr;//学習真データ
        private readonly double[][] 我看没啥用;//検証記述子
        private readonly double[] ﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞ;//検証真データ
        private readonly double GSR;//学習率
        private readonly double GSK;//学習回数
        private double[] z;//THLR関数線形和
        private double すげぇぇぇぇぇぇぇ;//線形和代表値(線形和絶対値の最小値)
        private double[] s;//予測値
        private double[] E;//予測誤差
        private double Es;//総二乗誤差
        private int 学習回数;//実際学習回数
        private double R2;//決定係数
        private readonly FileInfo 记录;//ログファイル
        private readonly double 允许误差;//許容誤差
        private bool 日本語使えるんかい;//エラーフラグ
        private bool 世界線は収束するんだ;//収束フラグ
        private object[] lo;
        private ParallelOptions 汉语也能用;
        private CancellationTokenSource 用なしだけど書いとこう;
        private Random 你这么牛逼咋不上天呢;
        internal int 学习次数
        {
            get
            {
                return 学習回数;
            }
        }
        internal bool 收敛
        {
            get
            {
                return 世界線は収束するんだ;
            }
        }
        internal double 決定係数
        {
            get
            {
                return R2;
            }
        }
        internal THLogRgr(in int Dtn, in double[][] desin, in double[] lrhp, in int[] hs, in int[] desind, in double xxl, in int GSK, in double[] sd, in int Dtv, in double[][] desk, in double[] kd, in double 大的一笔的数字, in FileInfo fi)//Dtnはデータ数、desinは学習記述子、lrhpはハイパーパラメータ(0は倍率,1は偏移)、hsは関数番号、desindは記述子番号、xxlは学習率、GSKは学習回数、sdは学習真データ、dtvは検証データ数、deskは検証記述子、kdは検証真データ、大的一笔的数字は許容誤差、fiはログファイル
        {
            if (lrhp == null || lrhp.Length != 2) throw new ArgumentOutOfRangeException("lrhp", "THLogRgr : Invalid hyperparameters.");
            if (大的一笔的数字 <= 0.0) throw new ArgumentOutOfRangeException("大的一笔的数字", "THLogRgr : Invalid error threshould.");
            if (desin == null || hs == null || desind == null || desin.Length < 1 || desin.Length != hs.Length || hs.Length != desind.Length) throw new ArgumentOutOfRangeException("desin/hs", "THLogRgr : Invalid descriptor length.");
            if (lrhp[0] == 0.0 || double.IsNaN(lrhp[0]) || double.IsInfinity(lrhp[0]) || lrhp[1] <= 0.0 || double.IsNaN(lrhp[1]) || double.IsInfinity(lrhp[1])) throw new ArgumentOutOfRangeException("lrhp", "THLogRgr : Invalid hyperparameters.");
            if (xxl <= 0.0 || double.IsNaN(xxl) || double.IsInfinity(xxl)) throw new ArgumentOutOfRangeException("xxl", "THLogRgr : Invalid learning rate.");
            if (GSK < 0) throw new ArgumentOutOfRangeException("GSK", "THLogRgr : Invalid learning epoch.");
            if (Dtn <= 1 || sd == null || sd.Length != Dtn) throw new ArgumentOutOfRangeException("Dtn/sd", "THLogRgr : Invalid training data number.");
            int cnt;
            for (cnt = 0; cnt < desin.Length; cnt++)
            {
                if (desin[cnt] == null || desin[cnt].Length != Dtn) throw new ArgumentOutOfRangeException("desin", "THLogRgr : Invalid validation data number.");
            }
            bool flg = true;
            if (desk == null && kd == null && Dtv == 0) flg = false;
            else
            {
                if (desk == null || desk.Length < 1 || desk.Length != desin.Length) throw new ArgumentOutOfRangeException("desk", "THLogRgr : Invalid validation descriptor number.");
                if (Dtv < 1 || kd == null || kd.Length != Dtv) throw new ArgumentOutOfRangeException("Dtv/kd", "THLogRgr : Invalid validation data number.");
                for (cnt = 0; cnt < desk.Length; cnt++)
                {
                    if (desk[cnt] == null || desk[cnt].Length != Dtv) throw new ArgumentOutOfRangeException("desk", "THLogRgr : Invalid validation data number.");
                }
            }
            this.Dtn = Dtn;
            if (!fi.Exists) using (File.Create(fi.FullName)) { }
            记录 = fi;
            df = new THLRFunc[desin.Length];
            sf = lrhp[0];
            py = lrhp[1];
            ytr = sd;
            ﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞ = kd;
            GSR = xxl;
            this.GSK = GSK;
            我看没啥用 = desk;
            記述子番号 = desind;
            関数番号 = hs;
            用なしだけど書いとこう = new CancellationTokenSource();
            汉语也能用 = new ParallelOptions();
            汉语也能用.CancellationToken = 用なしだけど書いとこう.Token;
            汉语也能用.TaskScheduler = TaskScheduler.Default;
            汉语也能用.MaxDegreeOfParallelism = Environment.ProcessorCount;
            try
            {
                for (cnt = 0; cnt < desin.Length; cnt++)
                {
                    switch (hs[cnt])
                    {
                        case 9:
                            {
                                df[cnt] = new Yuyuko(in desin[cnt], in GSR, in 汉语也能用);
                                break;
                            }
                        case 3:
                            {
                                df[cnt] = new Flandre(in desin[cnt], in GSR, in 汉语也能用);
                                break;
                            }
                        case 4:
                            {
                                df[cnt] = new Reisen(in desin[cnt], in GSR, in 汉语也能用);
                                break;
                            }
                        case 8:
                            {
                                df[cnt] = new Murasa(in desin[cnt], in GSR, in 汉语也能用);
                                break;
                            }
                        default: throw new ArgumentOutOfRangeException("lrhp", "THLogRgr : Function hasn't been implemented yet.");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception("THLogRgr : Can not create Touhou logistic regression model.");
            }
            wF = new double[desin.Length];
            你这么牛逼咋不上天呢 = new Random();
            for (cnt = 0; cnt < wF.Length; cnt++)
            {
                wF[cnt] = (1.0 + 你这么牛逼咋不上天呢.NextDouble()) / desin.Length;
            }
            wB = (1.0 + 你这么牛逼咋不上天呢.NextDouble()) / desin.Length;
            s = new double[Dtn];
            z = new double[Dtn];
            E = new double[Dtn];
            pdwF = new double[df.Length];
            Es = double.MaxValue;
            lo = new object[df.Length];
            for (cnt = 0; cnt < df.Length; cnt++)
            {
                lo[cnt] = new object();
            }
            日本語使えるんかい = true;
            すげぇぇぇぇぇぇぇ = double.MaxValue;
            学習回数 = 0;
            允许误差 = 大的一笔的数字;
            世界線は収束するんだ = false;
        }
        internal THLogRgr(in int データ数, in double[][] 描述符, in double[] 超参数, in int[] 関数, in double[] fw, in double bw, in double[][] fp)//予測用,fwは関数係数、bwはバイアス係数、fpは関数パラメータ
        {
            if (描述符 == null || 描述符.Length <= 0) throw new ArgumentOutOfRangeException("描述符", "THLogRgr : Invalid descriptors.");
            if (超参数 == null || 超参数.Length != 2) throw new ArgumentOutOfRangeException("超参数", "THLogRgr : Invalid hyperparameters.");
            if (関数 == null || 関数.Length != 描述符.Length) throw new ArgumentOutOfRangeException("関数", "THLogRgr : Invalid functions.");
            if (fw == null || fw.Length != 描述符.Length) throw new ArgumentOutOfRangeException("fw", "THLogRgr : Invalid function weights.");
            if (fp == null || fp.Length != 描述符.Length) throw new ArgumentOutOfRangeException("fp", "THLogRgr : Invalid function parameter.");
            if (超参数[0] == 0.0 || double.IsNaN(超参数[0]) || double.IsInfinity(超参数[0]) || 超参数[1] <= 0.0 || double.IsNaN(超参数[1]) || double.IsInfinity(超参数[1])) throw new ArgumentOutOfRangeException("超参数", "THLogRgr : Invalid hyperparameters.");
            if (double.IsInfinity(bw) || double.IsNaN(bw)) throw new ArgumentOutOfRangeException("bw", "THLogRgr : Invalid bias.");
            int cnt;
            for (cnt = 0; cnt < 描述符.Length; cnt++)
            {
                if (描述符[cnt].Length != データ数) throw new ArgumentOutOfRangeException("描述符", "THLogRgr : Invalid data amount of descriptors.");
                if (fp[cnt].Length != 2) throw new ArgumentOutOfRangeException("fp", "THLogRgr : Invalid function parameter amount.");
            }
            Dtn = データ数;
            df = new THLRFunc[描述符.Length];
            sf = 超参数[0];
            py = 超参数[1];
            wF = fw;
            pdwF = null;
            記述子番号 = null;
            関数番号 = 関数;
            wB = bw;
            pdwB = double.NaN;
            ytr = null;
            我看没啥用 = null;
            ﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞ = null;
            GSR = double.NaN;
            GSK = double.NaN;
            z = new double[データ数];
            すげぇぇぇぇぇぇぇ = double.NaN;
            s = new double[データ数];
            E = null;
            Es = double.NaN;
            学習回数 = -1;
            R2 = double.NaN;
            记录 = null;
            允许误差 = double.NaN;
            日本語使えるんかい = true;
            世界線は収束するんだ = false;
            lo = new object[df.Length];
            for (cnt = 0; cnt < df.Length; cnt++)
            {
                lo[cnt] = new object();
            }
            用なしだけど書いとこう = new CancellationTokenSource();
            汉语也能用 = new ParallelOptions();
            汉语也能用.CancellationToken = 用なしだけど書いとこう.Token;
            汉语也能用.TaskScheduler = TaskScheduler.Default;
            汉语也能用.MaxDegreeOfParallelism = Environment.ProcessorCount;
            try
            {
                for (cnt = 0; cnt < 関数.Length; cnt++)
                {
                    switch (関数[cnt])
                    {
                        case 9:
                            {
                                df[cnt] = new Yuyuko(in 描述符[cnt], in fp[cnt], in 汉语也能用);
                                break;
                            }
                        case 3:
                            {
                                df[cnt] = new Flandre(in 描述符[cnt], in fp[cnt], in 汉语也能用);
                                break;
                            }
                        case 4:
                            {
                                df[cnt] = new Reisen(in 描述符[cnt], in fp[cnt], in 汉语也能用);
                                break;
                            }
                        case 8:
                            {
                                df[cnt] = new Murasa(in 描述符[cnt], in fp[cnt], in 汉语也能用);
                                break;
                            }
                        default: throw new ArgumentOutOfRangeException("lrhp", "THLogRgr : Function hasn't been implemented yet.");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception("THLogRgr : Can not create Touhou logistic regression model.");
            }
            你这么牛逼咋不上天呢 = null;
        }
        internal bool FOpt()
        {
            int cnt;
            bool flg;
            string msg;
            for (cnt = 0; cnt < GSK; cnt++)
            {
                flg = stage1();
                if (!flg) return flg;
                if (世界線は収束するんだ) break;
                flg = stage2();
                if (!flg) return flg;
            }
            flg = stage3(out msg);
            if (!flg)
            {
                Console.WriteLine(msg);
                return flg;
            }
            flg = stage4(out msg);
            if (!flg)
            {
                Console.WriteLine(msg);
                return flg;
            }
            return true;
        }
        private bool stage1()
        {
            Es = 0.0;
            pdwB = 0.0;
            すげぇぇぇぇぇぇぇ = double.MaxValue;
            int cnt;
            for (cnt = 0; cnt < df.Length; cnt++)
            {
                pdwF[cnt] = 0.0;
                df[cnt].JBr();
                df[cnt].PWr();
            }
            世界線は収束するんだ = true;
            Parallel.For(0, Dtn, 汉语也能用, (dcnt) =>
            {
                int pcnt;
                bool flg;
                string msg;
                double dtemp, dtemp2, dtemp3;
                z[dcnt] = 0.0;
                if (!日本語使えるんかい) return;
                for (pcnt = 0; pcnt < df.Length; pcnt++)
                {
                    flg = df[pcnt].KS(out msg, in dcnt);
                    if (!flg)
                    {
                        日本語使えるんかい = false;
                        Console.WriteLine(msg);
                        return;
                    }
                    flg = df[pcnt].HSZ(out msg, in dcnt, out dtemp);
                    if (!flg)
                    {
                        日本語使えるんかい = false;
                        Console.WriteLine(msg);
                        return;
                    }
                    z[dcnt] += dtemp * wF[pcnt];
                }
                z[dcnt] += wB;
                dtemp2 = Math.Abs(z[dcnt]);
                if (dtemp2 < すげぇぇぇぇぇぇぇ) すげぇぇぇぇぇぇぇ = dtemp2;
                s[dcnt] = sf / (py + Math.Exp(-z[dcnt]));
                E[dcnt] = s[dcnt] - ytr[dcnt];
                dtemp2 = Math.Abs(E[dcnt]);
                lock (E.SyncRoot) Es += dtemp2 * dtemp2;
                if (dtemp2 > 允许误差) 世界線は収束するんだ = false;
                dtemp3 = E[dcnt] * s[dcnt] * (1.0 - s[dcnt]);
                lock (ytr.SyncRoot) pdwB += dtemp3;
                if (!日本語使えるんかい) return;
                for (pcnt = 0; pcnt < df.Length; pcnt++)
                {
                    flg = df[pcnt].HSZ(out msg, in dcnt, out dtemp2);
                    if (!flg)
                    {
                        日本語使えるんかい = false;
                        Console.WriteLine(msg);
                        return;
                    }
                    lock (lo[pcnt]) pdwF[pcnt] += dtemp3 * dtemp2;
                    dtemp2 = dtemp3 * wF[pcnt];
                    flg = df[pcnt].JBk(out msg, in dcnt, in dtemp2);
                    if (!flg)
                    {
                        日本語使えるんかい = false;
                        Console.WriteLine(msg);
                        return;
                    }
                    flg = df[pcnt].DS(out msg, in dcnt, out dtemp);
                    if (!flg)
                    {
                        日本語使えるんかい = false;
                        Console.WriteLine(msg);
                        return;
                    }
                    dtemp2 *= dtemp;
                    lock (lo[pcnt])
                    {
                        flg = df[pcnt].PWk(out msg, 1, in dtemp2);
                        if (!flg)
                        {
                            日本語使えるんかい = false;
                            Console.WriteLine(msg);
                            return;
                        }
                    }
                    flg = df[pcnt].DesR(out msg, in dcnt, out dtemp);
                    if (!flg)
                    {
                        日本語使えるんかい = false;
                        Console.WriteLine(msg);
                        return;
                    }
                    dtemp2 *= dtemp;
                    lock (df[pcnt].好像没什么卵用)
                    {
                        flg = df[pcnt].PWk(out msg, 0, in dtemp2);
                        if (!flg)
                        {
                            日本語使えるんかい = false;
                            Console.WriteLine(msg);
                            return;
                        }
                    }
                }
            });
            return 日本語使えるんかい;
        }
        private bool stage2()
        {
            //Console.WriteLine(pdwF[0]);//debug用
            //Console.WriteLine(pdwB);//debug用
            //Console.WriteLine(Es);//debug用
            double 动态学习率 = GSR;
            if (すげぇぇぇぇぇぇぇ > 1.0)
            {
                动态学习率 *= 1.68;
            }
            else if (すげぇぇぇぇぇぇぇ > 4.0)
            {
                动态学习率 *= 3.434;
            }
            if (pdwB != 0.0) wB -= 动态学习率 * pdwB;
            Parallel.For(0, df.Length, 汉语也能用, (find) =>
            {
                if (!日本語使えるんかい) return;
                bool flg;
                string stemp;
                if (pdwF[find] != 0.0)
                {
                    wF[find] -= 动态学习率 * pdwF[find];
                }
                flg = df[find].KK(out stemp);
                if (!flg)
                {
                    日本語使えるんかい = false;
                    Console.WriteLine(stemp);
                    return;
                }
            });
            学習回数++;
            return 日本語使えるんかい;
        }
        private bool stage3(out string msg)//決定係数R2を計算する
        {
            int cnt;
            double dtemp = 0.0, dtemp2 = 0.0;
            R2 = 0.0;
            for (cnt = 0; cnt < ytr.Length; cnt++)
            {
                dtemp += ytr[cnt];
            }
            dtemp /= ytr.Length;
            for (cnt = 0; cnt < ytr.Length; cnt++)
            {
                dtemp2 = ytr[cnt] - dtemp;
                R2 += dtemp2 * dtemp2;
            }
            if (R2 == 0.0)
            {
                msg = "Data variace is 0.";
                日本語使えるんかい = false;
                return 日本語使えるんかい;
            }
            R2 = Es / R2;
            R2 = 1.0 - R2;
            msg = "Successfully calculated.";
            return 日本語使えるんかい;
        }
        private bool stage4(out string msg)//ログファイルを作成する
        {
            int cnt;
            using (FileStream fs = new FileStream(记录.FullName, FileMode.Open, FileAccess.Write, FileShare.None, 256))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.Unicode, 256, false))
                {
                    sw.Write("描述符数量 : ");
                    sw.WriteLine(記述子番号.Length.ToString("G"));
                    sw.WriteLine();
                    for (cnt = 0; cnt < 記述子番号.Length; cnt++)
                    {
                        sw.Write("描述符序号 : ");
                        sw.WriteLine(記述子番号[cnt].ToString("G"));
                        sw.Write("関数番号 : ");
                        sw.WriteLine(関数番号[cnt].ToString("G"));
                        sw.Write("Function weight : ");
                        sw.WriteLine(wF[cnt].ToString("G15"));
                        sw.Write("記述子重み : ");
                        sw.WriteLine(df[cnt].係数.ToString("G15"));
                        sw.Write("Function bias weight : ");
                        sw.WriteLine(df[cnt].バイアス.ToString("G15"));
                        sw.WriteLine();
                    }
                    sw.Write("偏移系数 : ");
                    sw.WriteLine(wB.ToString("G"));
                    sw.WriteLine();
                    sw.Write("超参数（倍率） : ");
                    sw.WriteLine(sf.ToString("G15"));
                    sw.Write("超参数（偏移） : ");
                    sw.WriteLine(py.ToString("G15"));
                    sw.WriteLine("\r\n\r\n");
                    sw.Write("R\x00B2 = ");
                    sw.WriteLine(R2.ToString("G15"));
                    sw.Write("収束 = ");
                    sw.WriteLine(世界線は収束するんだ.ToString());
                    sw.Write("Learning epoch : ");
                    sw.WriteLine(学習回数.ToString("G"));
                    sw.WriteLine("\r\n\r\n");
                    sw.WriteLine("予測値 : ");
                    for (cnt = 0; cnt < s.Length; cnt++)
                    {
                        sw.Write(string.Format("{0,-20:G15}, ", s[cnt]));
                    }
                    sw.WriteLine();
                    sw.WriteLine("实际值 : ");
                    for (cnt = 0; cnt < ytr.Length; cnt++)
                    {
                        sw.Write(string.Format("{0,-20:G15}, ", ytr[cnt]));
                    }
                    sw.WriteLine("\r\n\r\n\r\n");
                    sw.Write(DateTime.Now.ToString("F", CultureInfo.GetCultureInfo("ja-JP")));
                }
            }
            msg = "Log file successfully created.";
            return 日本語使えるんかい;
        }
        static internal bool stage5(out string msg, in DirectoryInfo di)//ログファイルを集計する
        {
            if (!di.Exists)
            {
                msg = "Can not find directory.";
                return false;
            }
            int cnt, cnt2, itemp;
            string stemp, fpth, stemp2;
            string dpth = di.FullName;
            StringBuilder sb = new StringBuilder(dpth);
            if (!dpth.EndsWith("\\"))
            {
                sb.Append("\\");
                dpth = sb.ToString();
            }
            double dtemp;
            Match m;
            bool flg, flg2;
            List<Tuple<int, double>> finf = new List<Tuple<int, double>>();//ファイル番号と決定係数
            List<int[]> fdin = new List<int[]>();//記述子番号
            List<int[]> ffin = new List<int[]>();//関数番号
            for (cnt = 0; ; cnt++)
            {
                int[] fdintemp, ffintemp;
                sb.Clear();
                sb.Append(dpth);
                sb.Append(string.Format("{0}.THLRlog", cnt.ToString("G")));
                fpth = sb.ToString();
                if (!File.Exists(fpth))
                {
                    if (cnt == 0)
                    {
                        msg = "Can not find any log file.";
                        return false;
                    }
                    break;
                }
                using (FileStream fs = new FileStream(fpth, FileMode.Open, FileAccess.Read, FileShare.None, 256))
                {
                    using (StreamReader sr = new StreamReader(fs, Encoding.Unicode, true, 256, false))
                    {
                        flg = true;
                        do
                        {
                            stemp = sr.ReadLine();
                            m = THLRRgx.r1.Match(stemp ?? string.Empty);
                        } while (!m.Success && stemp != null);
                        if (stemp == null)
                        {
                            Console.WriteLine(string.Format("Can not find descriptor amount in log file {0}.", cnt));
                            continue;
                        }
                        flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                        if (!flg || itemp <= 0)
                        {
                            Console.WriteLine(string.Format("Invalid descriptor amount in log file {0}.", cnt));
                            continue;
                        }
                        fdintemp = new int[itemp];
                        ffintemp = new int[itemp];
                        flg2 = true;
                        for (cnt2 = 0; cnt2 < fdintemp.Length; cnt2++)
                        {
                            do
                            {
                                stemp = sr.ReadLine();
                                m = THLRRgx.r2.Match(stemp ?? string.Empty);
                            } while (!m.Success && stemp != null);
                            if (stemp == null)
                            {
                                Console.WriteLine(string.Format("Can not find descriptor {0} index in log file {1}.", cnt2, cnt));
                                flg2 = false;
                                break;
                            }
                            flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                            if (!flg || itemp < 0)
                            {
                                Console.WriteLine(string.Format("Invalid descriptor {1} index in log file {0}.", cnt, cnt2));
                                flg2 = false;
                                break;
                            }
                            fdintemp[cnt2] = itemp;
                            do
                            {
                                stemp = sr.ReadLine();
                                m = THLRRgx.r3.Match(stemp ?? string.Empty);
                            } while (!m.Success && stemp != null);
                            if (stemp == null)
                            {
                                Console.WriteLine(string.Format("Can not find fuction {0} index in log file {1}.", cnt2, cnt));
                                flg2 = false;
                                break;
                            }
                            flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                            if (!flg || (itemp != 9 && itemp != 3 && itemp != 4 && itemp != 8))
                            {
                                Console.WriteLine(string.Format("Invalid function {1} index in log file {0}.", cnt, cnt2));
                                flg2 = false;
                                break;
                            }
                            ffintemp[cnt2] = itemp;
                        }
                        if (!flg2) continue;
                        do
                        {
                            stemp = sr.ReadLine();
                            m = THLRRgx.r10.Match(stemp ?? string.Empty);
                        } while (!m.Success && stemp != null);
                        if (stemp == null)
                        {
                            Console.WriteLine(string.Format("Can not find coefficient of determination in log file {0}.", cnt));
                            continue;
                        }
                        flg = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out dtemp);
                        if (!flg || dtemp > 1.0)
                        {
                            Console.WriteLine(string.Format("Invalid coefficient of determination in log file {0}.", cnt));
                            continue;
                        }
                        fdin.Add(fdintemp);
                        ffin.Add(ffintemp);
                        finf.Add(new Tuple<int, double>(cnt, dtemp));
                    }
                }
            }
            if (fdin.Count != ffin.Count || ffin.Count != finf.Count)
            {
                msg = "Can not verify log file number.";
                return false;
            }
            if (finf.Count == 0)
            {
                msg = "Can not find valid log file.";
                return false;
            }
            sb.Clear();
            sb.Append(dpth);
            sb.Append("THLRls.csv");
            using (FileStream fs = new FileStream(sb.ToString(), FileMode.Create, FileAccess.Write, FileShare.None, 256))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8, 256, false))
                {
                    sw.WriteLine("fileindex,R^2,desindex,funcindex");
                    for (cnt = 0; cnt < finf.Count; cnt++)
                    {
                        if (fdin[cnt].Length != ffin[cnt].Length)
                        {
                            msg = "Can not verify length of descriptor/function index.";
                            return false;
                        }
                        if (fdin[cnt].Length == 0)
                        {
                            msg = "Invalid length of descriptor/function index.";
                            return false;
                        }
                        else if (fdin[cnt].Length == 1)
                        {
                            sw.WriteLine("{0},{1},{2},{3}", finf[cnt].Item1.ToString("G"), finf[cnt].Item2.ToString("G15"), fdin[cnt][0].ToString("G"), ffin[cnt][0].ToString("G"));
                        }
                        else
                        {
                            sb.Clear();
                            sb.Append(string.Format("{0},{1},", finf[cnt].Item1.ToString("G"), finf[cnt].Item2.ToString("G15")));
                            sb.Append(string.Format("\"("));
                            for (cnt2 = 0; cnt2 < fdin[cnt].GetUpperBound(0); cnt2++)
                            {
                                sb.Append(string.Format("{0},", fdin[cnt][cnt2]));
                            }
                            sb.Append(string.Format("{0})\",", fdin[cnt][cnt2]));
                            sb.Append(string.Format("\"("));
                            for (cnt2 = 0; cnt2 < ffin[cnt].GetUpperBound(0); cnt2++)
                            {
                                sb.Append(string.Format("{0},", ffin[cnt][cnt2]));
                            }
                            sb.Append(string.Format("{0})\"", ffin[cnt][cnt2]));
                            sw.WriteLine(sb.ToString());
                        }
                    }
                }
            }
            msg = "Successfully summarized.";
            return true;
        }
        internal bool stage6(out string msg, out double[] pred, out double[] e2)//検証
        {
            if (ﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞ == null || ﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞ.Length == 0)
            {
                msg = "No validation data.";
                pred = null;
                e2 = null;
                return false;
            }
            if (wF.Length != 我看没啥用.Length)
            {
                msg = "Invalid validation descriptor.";
                pred = null;
                e2 = null;
                return false;
            }
            int cnt, cnt2;
            bool flg;
            double[] zv = new double[ﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞ.Length];
            double[][] fv = new double[我看没啥用.Length][];
            for (cnt = 0; cnt < 我看没啥用.Length; cnt++)
            {
                flg = df[cnt].ValC(out msg, in 我看没啥用[cnt], out fv[cnt]);
                if (!flg || fv[cnt].Length != ﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞ.Length)
                {
                    if (msg != null) msg += "\r\nCan not verify data length.";
                    else msg = "Can not verify data length.";
                    pred = null;
                    e2 = null;
                    return false;
                }
                for (cnt2 = 0; cnt2 < zv.Length; cnt2++)
                {
                    zv[cnt2] += wF[cnt] * fv[cnt][cnt2];
                }
            }
            pred = new double[ﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞ.Length];
            e2 = new double[ﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞ.Length];
            for (cnt2 = 0; cnt2 < zv.Length; cnt2++)
            {
                zv[cnt2] += wB;
                pred[cnt2] = sf / (py + Math.Exp(-zv[cnt2]));
                e2[cnt2] = pred[cnt2] - ﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞﾑﾀﾞ[cnt2];
                e2[cnt2] = e2[cnt2] * e2[cnt2];
            }
            msg = "Validation data successfully calculated.";
            return true;
        }
        static internal bool stage7(out string msg, in int Dtn, in double[][] desin, in double[] lrhp, in int[] hs, in int[] desind, in double xxl, in int GSK, in double[] sd, in double 大的一笔的数字, in DirectoryInfo di)//Dtnはデータ数、desinは学習記述子、lrhpはハイパーパラメータ(0は倍率,1は偏移)、hsは関数番号、desindは記述子番号、xxlは学習率、GSKは学習回数、sdは学習真データ、大的一笔的数字は許容誤差、diはログフォルダー
        {
            if (lrhp == null || lrhp.Length != 2)
            {
                msg = "Invalid hyperparameters (lrhp).";
                return false;
            }
            if (大的一笔的数字 <= 0.0)
            {
                msg = "Invalid error threshould.";
                return false;
            }
            if (desin == null || hs == null || desind == null || desin.Length < 1 || desin.Length != hs.Length || hs.Length != desind.Length)
            {
                msg = "Invalid descriptor length.";
                return false;
            }
            if (lrhp[0] == 0.0 || double.IsNaN(lrhp[0]) || double.IsInfinity(lrhp[0]) || lrhp[1] <= 0.0 || double.IsNaN(lrhp[1]) || double.IsInfinity(lrhp[1]))
            {
                msg = "Invalid hyperparameters.";
                return false;
            }
            if (xxl <= 0.0 || double.IsNaN(xxl) || double.IsInfinity(xxl))
            {
                msg = "Invalid learning rate.";
                return false;
            }
            if (GSK < 0)
            {
                msg = "Invalid learning epoch.";
                return false;
            }
            if (Dtn <= 1 || sd == null || sd.Length != Dtn)
            {
                msg = "Invalid training data number.";
                return false;
            }
            if (!di.Exists)
            {
                msg = "Can not find task directory.";
                return false;
            }
            int cnt;
            for (cnt = 0; cnt < desin.Length; cnt++)
            {
                if (desin[cnt] == null || desin[cnt].Length != Dtn) throw new ArgumentOutOfRangeException("desin", "THLogRgr : Invalid validation data number.");
            }
            int cnt2, cnt3;
            int Dtr = Dtn - 1;
            int Dte = 1;
            double dtemp, q2, esum = 0.0, sda = 0.0, sdv = 0.0, r2a = 0.0;
            double[] rtr, predt, e2t;
            double[] pred = new double[Dtn];
            double[] e2 = new double[Dtn];
            double[] r2 = new double[Dtn];
            double[] rte = new double[1];
            double[][] destr = new double[desin.Length][];
            double[][] deste = new double[desin.Length][];
            bool flg;
            List<double> rl = new List<double>();
            List<double>[] desl = new List<double>[desin.Length];
            string dpth = di.FullName;
            StringBuilder sb = new StringBuilder(dpth);
            FileInfo fi;
            THLogRgr モデル;
            if (!dpth.EndsWith("\\"))
            {
                sb.Append("\\");
                dpth = sb.ToString();
            }
            for (cnt = 0; cnt < Dtn; cnt++)
            {
                sda += sd[cnt];
            }
            sda /= Dtn;
            for (cnt = 0; cnt < Dtn; cnt++)
            {
                for (cnt2 = 0; cnt2 < desin.Length; cnt2++)
                {
                    desl[cnt2] = new List<double>();
                    deste[cnt2] = new double[1];
                }
                rl = new List<double>();
                for (cnt2 = 0; cnt2 < Dtn; cnt2++)
                {
                    if (cnt2 != cnt)
                    {
                        for (cnt3 = 0; cnt3 < desin.Length; cnt3++)
                        {
                            desl[cnt3].Add(desin[cnt3][cnt2]);
                        }
                        rl.Add(sd[cnt2]);
                    }
                    else
                    {
                        for (cnt3 = 0; cnt3 < desin.Length; cnt3++)
                        {
                            deste[cnt3][0] = desin[cnt3][cnt2];
                        }
                        rte[0] = sd[cnt2];
                    }
                }
                rtr = rl.ToArray();
                for (cnt2 = 0; cnt2 < desin.Length; cnt2++)
                {
                    destr[cnt2] = desl[cnt2].ToArray();
                }
                sb = new StringBuilder(dpth);
                sb.Append(string.Format("{0}.THLRlog", cnt));
                fi = new FileInfo(sb.ToString());
                try
                {
                    モデル = new THLogRgr(in Dtr, in destr, in lrhp, in hs, in desind, in xxl, in GSK, in rtr, in Dte, in deste, in rte, in 大的一笔的数字, in fi);
                }
                catch (Exception e)
                {
                    msg = e.ToString();
                    return false;
                }
                flg = モデル.FOpt();
                if (!flg)
                {
                    msg = "Can not optimize the model.";
                    return false;
                }
                r2[cnt] = モデル.決定係数;
                r2a += モデル.決定係数;
                flg = モデル.stage6(out msg, out predt, out e2t);
                if (!flg)
                {
                    return false;
                }
                if (predt == null || e2t == null || predt.Length != 1 || e2t.Length != 1)
                {
                    msg = "Can not verify validation result.";
                    return false;
                }
                pred[cnt] = predt[0];
                e2[cnt] = e2t[0];
                esum += e2[cnt];
                dtemp = sd[cnt] - sda;
                sdv += dtemp * dtemp;
            }
            if (sdv == 0.0)
            {
                msg = "Invalid dataset, variance is 0.";
                return false;
            }
            r2a /= r2.Length;
            q2 = 1.0 - esum / sdv;
            sb = new StringBuilder(dpth);
            sb.Append(string.Format("LOO.THLRsum", cnt));
            using (FileStream fs = new FileStream(sb.ToString(), FileMode.Create, FileAccess.Write, FileShare.None, 256))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.Unicode, 256, false))
                {
                    sw.Write("記述子番号：");
                    for (cnt = 0; cnt < desind.GetUpperBound(0); cnt++)
                    {
                        sw.Write(string.Format("{0}, ", desind[cnt].ToString("G")));
                    }
                    sw.WriteLine(desind[cnt].ToString("G"));
                    sw.Write("函数序号：");
                    for (cnt = 0; cnt < hs.GetUpperBound(0); cnt++)
                    {
                        sw.Write(string.Format("{0}, ", hs[cnt].ToString("G")));
                    }
                    sw.WriteLine(hs[cnt].ToString("G"));
                    sw.WriteLine("\r\n\r\n");
                    sw.Write("Average R\x00B2 = ");
                    sw.WriteLine(r2a.ToString("G15"));
                    sw.Write("Q\x00B2 = ");
                    sw.WriteLine(q2.ToString("G15"));
                    sw.WriteLine("\r\n\r\n");
                    sw.WriteLine("预测值 : ");
                    for (cnt = 0; cnt < pred.Length; cnt++)
                    {
                        sw.Write(string.Format("{0,-20:G15}, ", pred[cnt]));
                    }
                    sw.WriteLine();
                    sw.WriteLine("実際値 : ");
                    for (cnt = 0; cnt < sd.Length; cnt++)
                    {
                        sw.Write(string.Format("{0,-20:G15}, ", sd[cnt]));
                    }
                    sw.WriteLine("\r\n\r\n\r\n");
                    sw.Write(DateTime.Now.ToString("F", CultureInfo.GetCultureInfo("ja-JP")));
                }
            }
            msg = "Validation successfully performed.";
            return false;
        }
        static internal bool stage8(in DirectoryInfo di)//平均予測とQ²を求める
        {
            if (!di.Exists)
            {
                Console.WriteLine("Can not find directory.");
                return false;
            }
            DirectoryInfo[] dis = di.GetDirectories();
            if (dis.Length == 0)
            {
                Console.WriteLine("Can not find any model directory.");
                return false;
            }
            int cnt, cnt2, divf = 0, fivf = 0;
            double dtemp, dtemp2 = 0.0, dtemp3 = 0.0, EQ = 0.0, dtemp4 = 0.0;
            double[] preds = new double[0], tds = new double[0], datemp, datemp2;
            List<double[]> PS = new List<double[]>();
            string dpth = di.FullName, dpth2, s;
            bool flg = false, flg2;
            StringBuilder sb = new StringBuilder(dpth), sb2;
            FileInfo fi;
            Match m;
            if (!dpth.EndsWith("\\"))
            {
                sb.Append("\\");
                dpth = sb.ToString();
            }
            for (cnt = 0; cnt < dis.Length; cnt++)
            {
                dpth2 = dis[cnt].FullName;
                sb2 = new StringBuilder(dpth2);
                if (!dpth2.EndsWith("\\"))
                {
                    sb2.Append("\\");
                    dpth2 = sb2.ToString();
                }
                sb2.Append("LOO.THLRsum");
                fi = new FileInfo(sb2.ToString());
                if (!fi.Exists) continue;
                using (FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.None, 256))
                {
                    using (StreamReader sr = new StreamReader(fs, Encoding.Unicode, true, 256, false))
                    {
                        do
                        {
                            s = sr.ReadLine();
                            m = THLRRgx.r13.Match(s ?? string.Empty);
                            if (m.Success) break;
                        } while (s != null);
                        if (s == null)
                        {
                            Console.WriteLine("Can not find predicted value title in {0}", fi.FullName);
                            continue;
                        }
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            Console.WriteLine("Can not find predicted values in {0}", fi.FullName);
                            continue;
                        }
                        m = THLRRgx.r15.Match(s);
                        if (!m.Success)
                        {
                            Console.WriteLine("Invalid predicted values in {0}.", fi.FullName);
                        }
                        if (!flg && fivf == 0)
                        {
                            divf = m.Groups[1].Captures.Count;
                            if (divf <= 0)
                            {
                                Console.WriteLine("Can not verify data number in {0}.", fi.FullName);
                                return false;
                            }
                            preds = new double[divf];
                            tds = new double[divf];
                        }
                        else if (!flg || fivf == 0)
                        {
                            Console.WriteLine("Can not verify file number in {0}.", fi.FullName);
                            return false;
                        }
                        else
                        {
                            if (divf != m.Groups[1].Captures.Count)
                            {
                                Console.WriteLine("Can not verify predicted data number in {0}", fi.FullName);
                                continue;
                            }
                        }
                        datemp = new double[divf];
                        for (cnt2 = 0; cnt2 < divf; cnt2++)
                        {
                            flg2 = double.TryParse(m.Groups[1].Captures[cnt2].Value.Trim(), out dtemp);
                            if (!flg2 || double.IsNaN(dtemp) || double.IsInfinity(dtemp))
                            {
                                Console.WriteLine("Invalid predicted value in {0}.", fi.FullName);
                                continue;
                            }
                            preds[cnt2] += dtemp;
                            datemp[cnt2] = dtemp;
                        }
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            Console.WriteLine("Can not find true value title in {0}", fi.FullName);
                            continue;
                        }
                        m = THLRRgx.r14.Match(s);
                        if (!m.Success)
                        {
                            Console.WriteLine("Invalid true value title in {0}.", fi.FullName);
                        }
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            Console.WriteLine("Can not find true values in {0}", fi.FullName);
                            continue;
                        }
                        m = THLRRgx.r15.Match(s);
                        if (!m.Success)
                        {
                            Console.WriteLine("Invalid true values in {0}.", fi.FullName);
                        }
                        if (divf != m.Groups[1].Captures.Count)
                        {
                            Console.WriteLine("Can not verify true data number in {0}", fi.FullName);
                            continue;
                        }
                        if (!flg && fivf == 0)
                        {
                            for (cnt2 = 0; cnt2 < divf; cnt2++)
                            {
                                flg2 = double.TryParse(m.Groups[1].Captures[cnt2].Value.Trim(), out dtemp);
                                if (!flg2 || double.IsNaN(dtemp) || double.IsInfinity(dtemp))
                                {
                                    Console.WriteLine("Invalid true value in {0}.", fi.FullName);
                                    continue;
                                }
                                tds[cnt2] = dtemp;
                            }
                        }
                        else
                        {
                            for (cnt2 = 0; cnt2 < divf; cnt2++)
                            {
                                flg2 = double.TryParse(m.Groups[1].Captures[cnt2].Value.Trim(), out dtemp);
                                if (!flg2 || double.IsNaN(dtemp) || double.IsInfinity(dtemp))
                                {
                                    Console.WriteLine("Invalid true value in {0}.", fi.FullName);
                                    continue;
                                }
                                if (tds[cnt2] != dtemp)
                                {
                                    Console.WriteLine("Can not verify true data {0} in {1}", cnt2, fi.FullName);
                                    continue;
                                }
                            }
                        }
                    }
                }
                PS.Add(datemp);
                if (!flg) flg = true;
                fivf++;
            }
            if (!flg || fivf <= 0)
            {
                Console.WriteLine("Can not find any proper log file.");
                return false;
            }
            dtemp = 0.0;
            datemp = new double[divf];
            for (cnt = 0; cnt < divf; cnt++)
            {
                for (cnt2 = 0; cnt2 < fivf; cnt2++)
                {
                    datemp[cnt] += PS[cnt2][cnt];
                }
                datemp[cnt] /= fivf;
                preds[cnt] /= fivf;
                dtemp = preds[cnt] - tds[cnt];
                EQ += dtemp * dtemp;
                dtemp2 += tds[cnt];
            }
            dtemp2 /= divf;
            datemp2 = new double[divf];
            for (cnt = 0; cnt < divf; cnt++)
            {
                for (cnt2 = 0; cnt2 < fivf; cnt2++)
                {
                    dtemp = PS[cnt2][cnt] - datemp[cnt];
                    datemp2[cnt] += dtemp * dtemp;
                }
                datemp2[cnt] /= fivf;
                datemp2[cnt] = Math.Sqrt(datemp2[cnt]);
            }
            for (cnt = 0; cnt < divf; cnt++)
            {
                dtemp = tds[cnt] - dtemp2;
                dtemp3 += dtemp * dtemp;
            }
            if (dtemp3 == 0.0)
            {
                Console.WriteLine("Invalid true data, variance is 0.");
                return false;
            }
            EQ = 1.0 - EQ / dtemp3;
            sb.Append("ELOO.THLRsum");
            using (FileStream fs = new FileStream(sb.ToString(), FileMode.Create, FileAccess.Write, FileShare.None, 256))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.Unicode, 256, false))
                {
                    sw.WriteLine("アンサンブルモデル予測：");
                    for (cnt = 0; cnt < preds.Length; cnt++)
                    {
                        sw.Write(string.Format("{0,-20:G15}, ", preds[cnt]));
                    }
                    sw.WriteLine();
                    sw.WriteLine("Standard deviance of prediction：");
                    for (cnt = 0; cnt < datemp2.Length; cnt++)
                    {
                        sw.Write(string.Format("{0,-20:G15}, ", datemp2[cnt]));
                    }
                    sw.WriteLine();
                    sw.WriteLine("真实值：");
                    for (cnt = 0; cnt < tds.Length; cnt++)
                    {
                        sw.Write(string.Format("{0,-20:G15}, ", tds[cnt]));
                    }
                    sw.WriteLine("\r\n\r\n\r\n");
                    sw.Write("EQ\x00B2 = ");
                    sw.WriteLine(EQ.ToString("G15"));
                    sw.WriteLine("\r\n\r\n");
                    sw.Write(DateTime.Now.ToString("F", CultureInfo.GetCultureInfo("ja-JP")));
                }
            }
            return true;
        }
        internal bool stage9()
        {
            Parallel.For(0, Dtn, 汉语也能用, (dcnt) =>
            {
                int pcnt;
                bool flg;
                string msg;
                double dtemp;
                z[dcnt] = 0.0;
                if (!日本語使えるんかい) return;
                for (pcnt = 0; pcnt < df.Length; pcnt++)
                {/*
                    flg = df[pcnt].KS(out msg, in dcnt);
                    if (!flg)
                    {
                        日本語使えるんかい = false;
                        Console.WriteLine(msg);
                        return;
                    }*/
                    flg = df[pcnt].HSZ(out msg, in dcnt, out dtemp);
                    if (!flg)
                    {
                        日本語使えるんかい = false;
                        Console.WriteLine(msg);
                        return;
                    }
                    z[dcnt] += dtemp * wF[pcnt];
                }
                if (!日本語使えるんかい) return;
                z[dcnt] += wB;
                s[dcnt] = sf / (py + Math.Exp(-z[dcnt]));
            });
            return 日本語使えるんかい;
        }
        static internal bool stage10(out string msg, in FileInfo DF, in DirectoryInfo di)//予測、DFは記述子ファイル、diはモデルパス
        {
            throw new NotImplementedException();
            msg = "Unexpected termination";
            if (!DF.Exists)
            {
                msg = "Can not find descriptor file.";
                return false;
            }
            if (!di.Exists)
            {
                msg = "Can not find log file directory.";
                return false;
            }
            int cnt, cnt2, dtn, itemp, fivf = 0;
            int[] desind, ksind;
            double dtemp, bias;
            double[] ksw, ccs, pred, dev;
            double[][] Des, ksp, desin;
            string s;
            bool flg, flg2 = false;
            List<double[]> pl = new List<double[]>();
            FileInfo fi;
            FileInfo[] lfs;
            StringBuilder sb;
            Match m;
            THLogRgr mod;
            //flg = DataRW.DesR(DF.FullName, out Des);
            if (!flg)
            {
                msg = "Can not read the descriptor file.";
                return false;
            }
            dtn = Des[0].Length;
            for (cnt = 1; cnt < Des.Length; cnt++)
            {
                if (Des[cnt].Length != dtn)
                {
                    msg = string.Format("Can not verify the data length of descriptor {0}.", cnt);
                    return false;
                }
            }
            pred = new double[dtn];
            lfs = di.GetFiles();
            for (cnt = 0; cnt < lfs.Length; cnt++)
            {
                if (!lfs[cnt].FullName.EndsWith(".THLRlog")) continue;
                using (FileStream fs = new FileStream(lfs[cnt].FullName, FileMode.Open, FileAccess.Read, FileShare.None, 256))
                {
                    using (StreamReader sr = new StreamReader(fs, Encoding.Unicode, true, 256, false))
                    {
                        do
                        {
                            s = sr.ReadLine();
                            m = THLRRgx.r1.Match(s ?? string.Empty);
                            if (m.Success) break;
                        } while (s != null);
                        if (s == null)
                        {
                            Console.WriteLine("Can not find descirptor amount in {0}", lfs[cnt].FullName);
                            continue; ;
                        }
                        flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                        if (!flg || itemp < 1)
                        {
                            Console.WriteLine("Invalid descirptor amount in {0}", lfs[cnt].FullName);
                            continue;
                        }
                        desind = new int[itemp];
                        ksind = new int[itemp];
                        ksw = new double[itemp];
                        ksp = new double[itemp][];
                        for (cnt2 = 0; cnt2 < desind.Length; cnt2++)
                        {
                            do
                            {
                                s = sr.ReadLine();
                                m = THLRRgx.r2.Match(s ?? string.Empty);
                                if (m.Success) break;
                            } while (s != null);
                            if (s == null)
                            {
                                Console.WriteLine("Can not find descirptor {0} index in {1}", cnt2, lfs[cnt].FullName);
                                continue;
                            }
                            flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                            if (!flg || itemp < 1)
                            {
                                Console.WriteLine("Invalid descirptor {0} index in {1}", cnt2, lfs[cnt].FullName);
                                continue;
                            }
                            desind[cnt2] = itemp;
                            s = sr.ReadLine();
                            m = THLRRgx.r3.Match(s ?? string.Empty);
                            if (s == null || !m.Success)
                            {
                                Console.WriteLine("Can not find function {0} index in {1}", cnt2, lfs[cnt].FullName);
                                continue;
                            }
                            flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                            if (!flg || itemp < 1)
                            {
                                Console.WriteLine("Invalid function {0} index in {1}", cnt2, lfs[cnt].FullName);
                                continue;
                            }
                            ksind[cnt2] = itemp;
                            s = sr.ReadLine();
                            m = THLRRgx.r4.Match(s ?? string.Empty);
                            if (s == null || !m.Success)
                            {
                                Console.WriteLine("Can not find function {0} weight in {1}", cnt2, lfs[cnt].FullName);
                                continue;
                            }
                            flg = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out dtemp);
                            if (!flg || double.IsInfinity(dtemp) || double.IsNaN(dtemp))
                            {
                                Console.WriteLine("Invalid function {0} weight in {1}", cnt2, lfs[cnt].FullName);
                                continue;
                            }
                            ksw[cnt2] = dtemp;
                            s = sr.ReadLine();
                            m = THLRRgx.r5.Match(s ?? string.Empty);
                            if (s == null || !m.Success)
                            {
                                Console.WriteLine("Can not find function {0} descriptor weight in {1}", cnt2, lfs[cnt].FullName);
                                continue;
                            }
                            flg = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out dtemp);
                            if (!flg || double.IsInfinity(dtemp) || double.IsNaN(dtemp))
                            {
                                Console.WriteLine("Invalid function {0} descriptor weight in {1}", cnt2, lfs[cnt].FullName);
                                continue;
                            }
                            ksp[cnt2] = new double[2];
                            ksp[cnt2][0] = dtemp;
                            s = sr.ReadLine();
                            m = THLRRgx.r6.Match(s ?? string.Empty);
                            if (s == null || !m.Success)
                            {
                                Console.WriteLine("Can not find function {0} bias weight in {1}", cnt2, lfs[cnt].FullName);
                                continue;
                            }
                            flg = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out dtemp);
                            if (!flg || double.IsInfinity(dtemp) || double.IsNaN(dtemp))
                            {
                                Console.WriteLine("Invalid function {0} bias weight in {1}", cnt2, lfs[cnt].FullName);
                                continue;
                            }
                            ksp[cnt2][1] = dtemp;
                        }
                        do
                        {
                            s = sr.ReadLine();
                            m = THLRRgx.r7.Match(s ?? string.Empty);
                            if (m.Success) break;
                        } while (s != null);
                        if (s == null)
                        {
                            Console.WriteLine("Can not find bias weight in {0}", lfs[cnt].FullName);
                            continue;
                        }
                        flg = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out dtemp);
                        if (!flg || double.IsInfinity(dtemp) || double.IsNaN(dtemp))
                        {
                            Console.WriteLine("Invalid bias weight in {0}", lfs[cnt].FullName);
                            continue;
                        }
                        bias = dtemp;
                        do
                        {
                            s = sr.ReadLine();
                            m = THLRRgx.r8.Match(s ?? string.Empty);
                            if (m.Success) break;
                        } while (s != null);
                        if (s == null)
                        {
                            Console.WriteLine("Can not find scale (hyperparameter) in {0}", lfs[cnt].FullName);
                            continue;
                        }
                        flg = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out dtemp);
                        if (!flg || double.IsInfinity(dtemp) || double.IsNaN(dtemp) || dtemp == 0.0)
                        {
                            Console.WriteLine("Invalid scale (hyperparameter) in {0}", lfs[cnt].FullName);
                            continue;
                        }
                        ccs = new double[2];
                        ccs[0] = dtemp;
                        s = sr.ReadLine();
                        m = THLRRgx.r9.Match(s ?? string.Empty);
                        if (s == null || !m.Success)
                        {
                            Console.WriteLine("Can not find shift (hyperparameter) in {0}", lfs[cnt].FullName);
                            continue;
                        }
                        flg = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out dtemp);
                        if (!flg || double.IsInfinity(dtemp) || double.IsNaN(dtemp) || dtemp == 0.0)
                        {
                            Console.WriteLine("Invalid shift (hyperparameter) in {0}", lfs[cnt].FullName);
                            continue;
                        }
                        ccs[1] = dtemp;
                    }
                }
                desin = new double[desind.Length][];
                for (cnt2 = 0; cnt2 < desin.Length; cnt2++)
                {
                    desin[cnt2] = Des[desind[cnt2]];
                }
                try
                {
                    mod = new THLogRgr(in dtn, in desin, in ccs, in ksind, in ksw, in bias, in ksp);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    continue;
                }
                flg = mod.stage9();
                if (!flg)
                {
                    Console.WriteLine("Can not calculate with model {0}", lfs[cnt].FullName);
                    continue;
                }
                if (mod.s.Length != dtn)
                {
                    Console.WriteLine("Can not verify data length of model {0}.", lfs[cnt].FullName);
                    continue;
                }
                pl.Add(mod.s);
                for (cnt2 = 0; cnt2 < dtn; cnt2++)
                {
                    pred[cnt2] += mod.s[cnt2];
                }
                fivf++;
                if (!flg2) flg2 = true;
            }
            if (!flg2)
            {
                msg = "Can not find any valid log file.";
                return false;
            }
            if (pl.Count != fivf)
            {
                msg = "Can not verify valid log file amount.";
                return false;
            }
            Console.WriteLine("{0} log files read.", fivf);
            dev = new double[dtn];
            for (cnt = 0; cnt < dtn; cnt++)
            {
                pred[cnt] /= fivf;
                for (cnt2 = 0; cnt2 < fivf; cnt2++)
                {
                    dtemp = pl[cnt2][cnt] - pred[cnt];
                    dev[cnt] += dtemp * dtemp;
                }
                dev[cnt] /= fivf;
                dev[cnt] = Math.Sqrt(dev[cnt]);
            }
            s = DF.DirectoryName;
            if (!Directory.Exists(s))
            {
                msg = "Unknown error, directory of descriptor file is invalid.";
                return false;
            }
            sb = new StringBuilder(s);
            if (!s.EndsWith("\\")) sb.Append("\\");
            sb.Append("Prediction.csv");
            s = sb.ToString();
            using (FileStream fs = new FileStream(s, FileMode.Create, FileAccess.Write, FileShare.None, 256))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8, 256, false))
                {
                    sw.Write("avg,dev");
                    for (cnt = 0; cnt < fivf; cnt++)
                    {
                        sw.Write(string.Format(",{0}", cnt));
                    }
                    for (cnt = 0; cnt < dtn; cnt++)
                    {
                        sw.WriteLine();
                        sw.Write(pred[cnt].ToString("G15"));
                        sw.Write(",");
                        sw.Write(dev[cnt].ToString("G15"));
                        for (cnt2 = 0; cnt2 < fivf; cnt2++)
                        {
                            sw.Write(",");
                            sw.Write(pl[cnt2][cnt].ToString("G15"));
                        }
                    }
                }
            }
            msg = "Successfully predicted";
            return true;
        }
        static private class THLRRgx
        {
            static private readonly TimeSpan tots = TimeSpan.FromSeconds(17.71104);
            static internal readonly Regex r1 = new Regex(@"^描述符数量\s:\s(?>(\d+))$", RegexOptions.CultureInvariant, tots);//記述子数
            static internal readonly Regex r2 = new Regex(@"^描述符序号\s:\s(?>(\d+))$", RegexOptions.CultureInvariant, tots);//記述子番号
            static internal readonly Regex r2a = new Regex(@"^記述子番号：(?>(\d+)(?>,\s)?)+$", RegexOptions.CultureInvariant, tots);//記述子番号
            static internal readonly Regex r3 = new Regex(@"^関数番号\s:\s(?>(\d+))$", RegexOptions.CultureInvariant, tots);//関数番号
            static internal readonly Regex r3a = new Regex(@"^函数序号：(?>(\d+)(?>,\s)?)+$", RegexOptions.CultureInvariant, tots);//関数番号
            static internal readonly Regex r4 = new Regex(@"^Function\sweight\s:\s(?>([-+.\dE]+))$", RegexOptions.CultureInvariant, tots);//関数重み
            static internal readonly Regex r5 = new Regex(@"^記述子重み\s:\s(?>([-+.\dE]+))$", RegexOptions.CultureInvariant, tots);//記述子重み
            static internal readonly Regex r6 = new Regex(@"^Function\sbias\sweight\s:\s(?>([-+.\dE]+))$", RegexOptions.CultureInvariant, tots);//関数バイアス重み
            static internal readonly Regex r7 = new Regex(@"^偏移系数\s:\s(?>([-+.\dE]+))$", RegexOptions.CultureInvariant, tots);//偏移係数
            static internal readonly Regex r8 = new Regex(@"^超参数（倍率）\s:\s(?>([-+.\dE]+))$", RegexOptions.CultureInvariant, tots);//ハイパーパラメータ（倍率）
            static internal readonly Regex r9 = new Regex(@"^超参数（偏移）\s:\s(?>([-+.\dE]+))$", RegexOptions.CultureInvariant, tots);//ハイパーパラメータ（偏移）
            static internal readonly Regex r10 = new Regex(@"^R²\s=\s(?>([-+.\dE]+))$", RegexOptions.CultureInvariant, tots);//決定係数
            static internal readonly Regex r10a = new Regex(@"^Average\sR²\s=\s(?>([-+.\dE]+))$", RegexOptions.CultureInvariant, tots);//平均決定係数
            static internal readonly Regex r10b = new Regex(@"^Q²\s=\s(?>([-+.\dE]+))$", RegexOptions.CultureInvariant, tots);//検証決定係数
            static internal readonly Regex r11 = new Regex(@"^収束\s=\s(?>(False|True))$", RegexOptions.CultureInvariant, tots);//収束状態
            static internal readonly Regex r12 = new Regex(@"^Learning\sepoch\s:\s(?>(\d+))$", RegexOptions.CultureInvariant, tots);//実際学習回数
            static internal readonly Regex r13 = new Regex(@"^预测值\s:\s$", RegexOptions.CultureInvariant, tots);//予測値タイトル
            static internal readonly Regex r14 = new Regex(@"^実際値\s:\s$", RegexOptions.CultureInvariant, tots);//実際値タイトル
            static internal readonly Regex r15 = new Regex(@"^(?>([-+.\dE]+)\s*,\s)+$", RegexOptions.CultureInvariant, tots);//予測値/実際値
        }
    }
    internal abstract class THLRFunc//THLR関数
    {
        private protected readonly int Dtn;//データ数
        private protected readonly double 不是很动态的动态学习率;//学習率
        private protected readonly double[] des;//入力記述子
        private protected double dva;//入力記述子分散
        private protected double dav;//入力記述子平均値
        private protected double ddr;//入力記述子標準偏差逆数
        private protected double wdes;//記述子係数
        private protected double wb;//バイアス係数
        private protected double[] parw;//wに対する偏微分、0はwdes、1はwb
        private protected double[] X;//調整後記述子
        private protected double[] KSC;//関数値
        private protected double[] 上層微分;//上層微分値
        internal double bs//記述子分散
        {
            get
            {
                return dva;
            }
        }
        internal double hk//記述子平均値
        {
            get
            {
                return dav;
            }
        }
        internal double hhg//述子標準偏差逆数
        {
            get
            {
                return ddr;
            }
        }
        internal double 係数//記述子係数
        {
            get
            {
                return wdes;
            }
        }
        internal double バイアス//バイアス係数
        {
            get
            {
                return wb;
            }
        }
        internal object 好像没什么卵用
        {
            get
            {
                return parw.SyncRoot;
            }
        }
        private protected THLRFunc(in double[] desin, in double XXL)//desinは入力記述子、XXLは学習率
        {
            if (desin == null || desin.Length <= 1)
            {
                throw new ArgumentOutOfRangeException("desin", "THLRFunc : Invalid input descriptor.");
            }
            string msg;
            bool flg;
            double dtemp;
            des = desin;
            Dtn = desin.Length;
            flg = Ava(out msg);
            if (!flg)
            {
                throw new Exception(string.Format("THLRFunc : {0}.", msg));
            }
            X = new double[Dtn];
            KSC = new double[Dtn];
            上層微分 = new double[Dtn];
            parw = new double[2];
            dtemp = Math.Sqrt(dva);
            ddr = 1.0 / dtemp;
            wb = -dav * ddr;
            wdes = 1.0 * ddr;
            if (!double.IsNaN(XXL)) 不是很动态的动态学习率 = XXL / (dtemp + Math.Abs(dav));
            else 不是很动态的动态学习率 = double.NaN;
        }
        private protected THLRFunc(in double[] desin, in double[] par)//予測用、desinは入力記述子、parは関数パラメータ
        {
            if (desin == null || desin.Length <= 1)
            {
                throw new ArgumentOutOfRangeException("desin", "THLRFunc : Invalid input descriptor.");
            }
            if (par == null || par.Length != 2 || double.IsNaN(par[0]) || double.IsInfinity(par[0]) || double.IsNaN(par[1]) || double.IsInfinity(par[1]))
            {
                throw new ArgumentOutOfRangeException("par", "THLRFunc : Invalid function parameter.");
            }
            des = desin;
            Dtn = desin.Length;
            不是很动态的动态学习率 = double.NaN;
            dva = double.NaN;
            dav = double.NaN;
            ddr = double.NaN;
            wdes = par[0];
            wb = par[1];
            parw = null;
            X = new double[Dtn];
            KSC = new double[Dtn];
            上層微分 = null;
        }
        internal abstract bool KS(out string msg, in int i);//状態更新
        internal abstract bool DS(out string msg, in int i, out double d);//微分を求める
        internal abstract bool KK(out string msg);//係数更新
        internal bool DesR(out string msg, in int i, out double d)//記述子を読み取る
        {
            if (i < 0 || i >= Dtn)
            {
                msg = "Invalid index.";
                d = double.NaN;
                return false;
            }
            d = des[i];
            msg = "Descriptor read.";
            return true;
        }
        internal bool HSZ(out string msg, in int i, out double d)//関数値
        {
            if (i >= Dtn || i < 0)
            {
                msg = "Invalid index.";
                d = double.NaN;
                return false;
            }
            msg = "Value returned.";
            d = KSC[i];
            return true;
        }
        internal bool JBk(out string msg, in int i, in double d)//上層微分更新
        {
            if (i < 0 || i >= Dtn)
            {
                msg = "Invalid index of ULD.";
                return false;
            }
            上層微分[i] = d;
            msg = "Upper layer differential updated successfully.";
            return true;
        }
        internal void JBr()//上層微分リセット
        {
            for (int cnt = 0; cnt < 上層微分.Length; cnt++)
            {
                上層微分[cnt] = 0.0;
            }
        }
        internal bool PWk(out string msg, in int i, in double d)//wに対する偏微分更新
        {
            if (i < 0 || i >= 2)
            {
                msg = "Invalid index of parw.";
                return false;
            }
            parw[i] += d;
            msg = "Partial differential of w updated successfully.";
            return true;
        }
        internal void PWr()//wに対する偏微分リセット
        {
            parw[0] = 0.0;
            parw[1] = 0.0;
        }
        private bool Ava(out string msg)//平均値と分散の計算
        {
            msg = "Unexpected termination.";
            if (des == null || des.Length != Dtn || Dtn <= 1)
            {
                msg = "Incorrect data length.";
                return false;
            }
            int cnt;
            double dtemp;
            bool flg = false;
            dav = 0.0;
            for (cnt = 0; cnt < Dtn; cnt++)
            {
                if (!flg)
                {
                    if (des[cnt] != des[0]) flg = true;
                }
                dav += des[cnt];
            }
            if (!flg)
            {
                msg = "Data variance is 0.";
                return false;
            }
            dav /= Dtn;
            dva = 0.0;
            for (cnt = 0; cnt < Dtn; cnt++)
            {
                dtemp = des[cnt] - dav;
                dva += dtemp * dtemp;
            }
            dva /= Dtn;
            msg = "Successfully calculated.";
            return true;
        }
        internal abstract bool ValC(out string msg, in double[] desv, out double[] pred);//予測
    }
    internal class Yuyuko : THLRFunc
    {
        internal Yuyuko(in double[] desin, in double XXL, in ParallelOptions po) : base(in desin, in XXL)
        {
            bool flg;
            string msg;
            try
            {
                Parallel.For(0, Dtn, po, (cnt) =>
                {
                    flg = KS(out msg, in cnt);
                    if (!flg) throw new ArgumentOutOfRangeException(string.Format("Yuyuko(KS) : {0}", msg));
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception("Yuyuko : Can not create Yuyuko function.");
            }
        }
        internal Yuyuko(in double[] desin, in double[] par, in ParallelOptions po) : base(in desin, in par)
        {
            bool flg;
            string msg;
            try
            {
                Parallel.For(0, Dtn, po, (cnt) =>
                {
                    flg = KS(out msg, in cnt);
                    if (!flg) throw new ArgumentOutOfRangeException(string.Format("Yuyuko(KS) : {0}", msg));
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception("Yuyuko : Can not create Yuyuko function.");
            }
        }
        override internal bool KS(out string msg, in int i)
        {
            if (i >= Dtn || i < 0)
            {
                msg = "Invalid index.";
                return false;
            }
            X[i] = des[i] * wdes + wb;
            KSC[i] = X[i];
            msg = "Updated successfully.";
            return true;
        }
        override internal bool DS(out string msg, in int i, out double d)
        {
            if (i >= Dtn || i < 0)
            {
                msg = "Invalid index.";
                d = double.NaN;
                return false;
            }
            d = 1.0;
            msg = "Differential calculated.";
            return true;
        }
        override internal bool KK(out string msg)
        {
            if (parw[0] != 0.0)
            {
                wdes -= 不是很动态的动态学习率 * parw[0];
            }
            if (parw[1] != 0.0)
            {
                wb -= 不是很动态的动态学习率 * parw[1];
            }
            msg = "Coefficient updated.";
            return true;
        }
        override internal bool ValC(out string msg, in double[] desv, out double[] pred)
        {
            if (desv == null || desv.Length < 1)
            {
                msg = "Invalid validation descriptor.";
                pred = null;
                return false;
            }
            int cnt;
            pred = new double[desv.Length];
            for (cnt = 0; cnt < desv.Length; cnt++)
            {
                pred[cnt] = desv[cnt] * wdes + wb;
            }
            msg = "Predicted successfully.";
            return true;
        }
    }
    internal class Flandre : THLRFunc
    {
        private double Xdh;//X代表値
        private double Ds;//動的学習率スケーリングファクター
        private double[] Xa;//X絶対値
        private object lo;
        internal Flandre(in double[] desin, in double XXL, in ParallelOptions po) : base(in desin, in XXL)
        {
            bool flg;
            string msg;
            lo = new object();
            Xdh = double.MaxValue;
            Xa = new double[Dtn];
            try
            {
                Parallel.For(0, Dtn, po, (cnt) =>
                {
                    flg = KS(out msg, in cnt);
                    if (!flg) throw new ArgumentOutOfRangeException(string.Format("Flandre(KS) : {0}", msg));
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception("Flandre : Can not create Flandre function.");
            }
        }
        internal Flandre(in double[] desin, in double[] par, in ParallelOptions po) : base(in desin, in par)
        {
            bool flg;
            string msg;
            lo = new object();
            Xdh = double.MaxValue;
            Xa = new double[Dtn];
            try
            {
                Parallel.For(0, Dtn, po, (cnt) =>
                {
                    flg = KS(out msg, in cnt);
                    if (!flg) throw new ArgumentOutOfRangeException(string.Format("Flandre(KS) : {0}", msg));
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception("Flandre : Can not create Flandre function.");
            }
        }
        override internal bool KS(out string msg, in int i)
        {
            if (i >= Dtn || i < 0)
            {
                msg = "Invalid index.";
                return false;
            }
            X[i] = des[i] * wdes + wb;
            Xa[i] = Math.Abs(X[i]);
            lock (lo) if (Xdh > Xa[i]) Xdh = Xa[i];
            KSC[i] = 1.0 / (1.0 + Math.Exp(-X[i]));
            msg = "Updated successfully.";
            return true;
        }
        override internal bool DS(out string msg, in int i, out double d)
        {
            if (i >= Dtn || i < 0)
            {
                msg = "Invalid index.";
                d = double.NaN;
                return false;
            }
            d = KSC[i] * (1 - KSC[i]);
            msg = "Differential calculated.";
            return true;
        }
        override internal bool KK(out string msg)
        {
            if (Xdh <= 1.9) Ds = 1.7;
            else if (Xdh <= 4.95) Ds = 3.434;
            else Ds = 5.14;
            if (parw[0] != 0.0)
            {
                wdes -= 不是很动态的动态学习率 * parw[0] * Ds;
            }
            if (parw[1] != 0.0)
            {
                wb -= 不是很动态的动态学习率 * parw[1] * Ds;
            }
            Xdh = double.MaxValue;
            msg = "Coefficient updated.";
            return true;
        }
        override internal bool ValC(out string msg, in double[] desv, out double[] pred)
        {
            if (desv == null || desv.Length < 1)
            {
                msg = "Invalid validation descriptor.";
                pred = null;
                return false;
            }
            int cnt;
            pred = new double[desv.Length];
            for (cnt = 0; cnt < desv.Length; cnt++)
            {
                pred[cnt] = desv[cnt] * wdes + wb;
                pred[cnt] = 1.0 / (1.0 + Math.Exp(-pred[cnt]));
            }
            msg = "Predicted successfully.";
            return true;
        }
    }
    internal class Reisen : THLRFunc
    {
        private double Xdh;//X代表値
        private double Ds;//動的学習率スケーリングファクター
        private double[] Xa;//X絶対値
        private object lo;
        internal Reisen(in double[] desin, in double XXL, in ParallelOptions po) : base(in desin, in XXL)
        {
            bool flg;
            string msg;
            lo = new object();
            Xdh = 0.0;
            Xa = new double[Dtn];
            try
            {
                Parallel.For(0, Dtn, po, (cnt) =>
                {
                    flg = KS(out msg, in cnt);
                    if (!flg) throw new ArgumentOutOfRangeException(string.Format("Reisen(KS) : {0}", msg));
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception("Reisen : Can not create Reisen function.");
            }
        }
        internal Reisen(in double[] desin, in double[] par, in ParallelOptions po) : base(in desin, in par)
        {
            bool flg;
            string msg;
            lo = new object();
            Xdh = 0.0;
            Xa = new double[Dtn];
            try
            {
                Parallel.For(0, Dtn, po, (cnt) =>
                {
                    flg = KS(out msg, in cnt);
                    if (!flg) throw new ArgumentOutOfRangeException(string.Format("Reisen(KS) : {0}", msg));
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception("Reisen : Can not create Reisen function.");
            }
        }
        override internal bool KS(out string msg, in int i)
        {
            if (i >= Dtn || i < 0)
            {
                msg = "Invalid index.";
                return false;
            }
            X[i] = des[i] * wdes + wb;
            Xa[i] = Math.Abs(X[i]);
            lock (lo) if (Xdh < Xa[i]) Xdh = Xa[i];
            KSC[i] = X[i] * X[i];
            msg = "Updated successfully.";
            return true;
        }
        override internal bool DS(out string msg, in int i, out double d)
        {
            if (i >= Dtn || i < 0)
            {
                msg = "Invalid index.";
                d = double.NaN;
                return false;
            }
            d = 2.0 * X[i];
            msg = "Differential calculated.";
            return true;
        }
        override internal bool KK(out string msg)
        {
            if (Xdh <= 0.999) Ds = 1.04;
            else if (Xdh <= 5.14) Ds = 0.0999;
            else if (Xdh <= 17.71) Ds = 0.003434;
            else Ds = 0.000495;
            if (parw[0] != 0.0)
            {
                wdes -= 不是很动态的动态学习率 * parw[0] * Ds;
            }
            if (parw[1] != 0.0)
            {
                wb -= 不是很动态的动态学习率 * parw[1] * Ds;
            }
            Xdh = 0.0;
            msg = "Coefficient updated.";
            return true;
        }
        override internal bool ValC(out string msg, in double[] desv, out double[] pred)
        {
            if (desv == null || desv.Length < 1)
            {
                msg = "Invalid validation descriptor.";
                pred = null;
                return false;
            }
            int cnt;
            pred = new double[desv.Length];
            for (cnt = 0; cnt < desv.Length; cnt++)
            {
                pred[cnt] = desv[cnt] * wdes + wb;
                pred[cnt] = pred[cnt] * pred[cnt];
            }
            msg = "Predicted successfully.";
            return true;
        }
    }
    internal class Murasa : THLRFunc
    {
        private double Xdh;//X代表値
        private double Ds;//動的学習率スケーリングファクター
        private double[] Xa;//X絶対値
        private object lo;
        internal Murasa(in double[] desin, in double XXL, in ParallelOptions po) : base(in desin, in XXL)
        {
            bool flg;
            string msg;
            lo = new object();
            Xdh = double.MaxValue;
            Xa = new double[Dtn];
            try
            {
                Parallel.For(0, Dtn, po, (cnt) =>
                {
                    flg = KS(out msg, in cnt);
                    if (!flg) throw new ArgumentOutOfRangeException(string.Format("Reisen(KS) : {0}", msg));
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception("Murasa : Can not create Murasa function.");
            }
        }
        internal Murasa(in double[] desin, in double[] par, in ParallelOptions po) : base(in desin, in par)
        {
            bool flg;
            string msg;
            lo = new object();
            Xdh = double.MaxValue;
            Xa = new double[Dtn];
            try
            {
                Parallel.For(0, Dtn, po, (cnt) =>
                {
                    flg = KS(out msg, in cnt);
                    if (!flg) throw new ArgumentOutOfRangeException(string.Format("Reisen(KS) : {0}", msg));
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception("Murasa : Can not create Murasa function.");
            }
        }
        override internal bool KS(out string msg, in int i)
        {
            if (i >= Dtn || i < 0)
            {
                msg = "Invalid index.";
                return false;
            }
            X[i] = des[i] * wdes + wb;
            Xa[i] = Math.Abs(X[i]);
            lock (lo) if (Xdh > Xa[i]) Xdh = Xa[i];
            KSC[i] = 2.0 / (X[i] * X[i] + 2.0);
            msg = "Updated successfully.";
            return true;
        }
        override internal bool DS(out string msg, in int i, out double d)
        {
            if (i >= Dtn || i < 0)
            {
                msg = "Invalid index.";
                d = double.NaN;
                return false;
            }
            d = -2.0 * X[i] * KSC[i] * KSC[i];
            msg = "Differential calculated.";
            return true;
        }
        override internal bool KK(out string msg)
        {
            if (Xdh <= 1.9) Ds = 1.68;
            else if (Xdh <= 5.14) Ds = 4.95;
            else Ds = 5.14;
            if (parw[0] != 0.0)
            {
                wdes -= 不是很动态的动态学习率 * parw[0] * Ds;
            }
            if (parw[1] != 0.0)
            {
                wb -= 不是很动态的动态学习率 * parw[1] * Ds;
            }
            Xdh = double.MaxValue;
            msg = "Coefficient updated.";
            return true;
        }
        override internal bool ValC(out string msg, in double[] desv, out double[] pred)
        {
            if (desv == null || desv.Length < 1)
            {
                msg = "Invalid validation descriptor.";
                pred = null;
                return false;
            }
            int cnt;
            pred = new double[desv.Length];
            for (cnt = 0; cnt < desv.Length; cnt++)
            {
                pred[cnt] = desv[cnt] * wdes + wb;
                pred[cnt] = 2.0 / (pred[cnt] * pred[cnt] + 2.0);
            }
            msg = "Predicted successfully.";
            return true;
        }
    }
}
