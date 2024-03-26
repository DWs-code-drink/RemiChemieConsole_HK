using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RemiChemieConsole
{
    internal class THNetwork//東方ネットワーク
    {
        private readonly int Dtn;//データ数
        private THNLayer[] Nly;//層
        private double[][] TD;//真データ
        private bool C = false;//収束
        private bool V = false;//検証
        private static Random rnd;
        [ThreadStatic] private Random rndt;
        private static object lo = new object();
        private CancellationTokenSource cts;
        private ParallelOptions po;
        private Stopwatch swj = new Stopwatch();
        private Stopwatch swl = new Stopwatch();
        internal THNetwork(int D, int[] ln, Tuple<int, int>[] Tcn, double[][] Fwp, int[][] Nfp)//Dはデータ数、lnはノード数、Tcnは(0,n]層の連結数範囲、Fwpは超層連結率、Nfpは関数確率
        {
            if (D <= 1) throw new ArgumentOutOfRangeException("D", "THNetwork : Invalid data number.");
            if (ln.Length <= 1) throw new ArgumentOutOfRangeException("ln", "THNetwork : Invalid layer number.");
            if (Tcn.Length != ln.GetUpperBound(0)) throw new ArgumentOutOfRangeException("Tcn/ln", "THNetwork : Can not verify layer number.");
            if (Fwp.Length != ln.GetUpperBound(0)) throw new ArgumentOutOfRangeException("Fwp/ln", "THNetwork : Can not verify layer number.");
            if (Nfp.Length != ln.Length) throw new ArgumentOutOfRangeException("Nfp/ln", "THNetwork : Can not verify layer number.");
            foreach (int itemp in ln) if (itemp <= 0) throw new ArgumentOutOfRangeException("lnn", "THNetwork : Invalid node number.");
            Dtn = D;
            Nly = new THNLayer[ln.Length];
            rnd = new Random();
            cts = new CancellationTokenSource();
            po = new ParallelOptions();
            po.CancellationToken = cts.Token;
            po.TaskScheduler = TaskScheduler.Default;
            po.MaxDegreeOfParallelism = CommonParam.thdn;
            List<int>[][][] inns = new List<int>[ln.GetUpperBound(0)][][];//(0,n]入力連結[入][入ノード][出][出ノード]
            int[][] Nl = new int[ln.GetUpperBound(0)][];//各層ノードインデックス
            int cnt, cnt2;
            for (cnt = 0; cnt < ln.GetUpperBound(0); cnt++)
            {
                Nl[cnt] = new int[ln[cnt]];
                for (cnt2 = 0; ; cnt2++)
                {
                    if (cnt2 < ln[cnt]) Nl[cnt][cnt2] = cnt2;
                    else break;
                }
            }
            bool flg = true;
            try
            {
                Parallel.For(1, ln.Length, po, (lind) =>//入力層
                {
                    if (cts.IsCancellationRequested) return;
                    int cnt3;
                    int incnt = lind - 1;
                    List<int>[][] innt = null;
                    lock (lo) cnt3 = rnd.Next();
                    rndt = new Random(cnt3);
                    try
                    {
                        NSKO(in lind, in Fwp[incnt], in ln, in Tcn[incnt], out innt, in rndt, in Nl, ref po);
                    }
                    catch (ArgumentOutOfRangeException aore)
                    {
                        Console.WriteLine("{0} => {1}", aore.ParamName, aore.Message);
                        flg = false;
                        cts.Cancel();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        flg = false;
                    }
                    inns[incnt] = innt;
                });
            }
            catch (OperationCanceledException oce)
            {
                Console.WriteLine(oce.Message);
                throw new Exception("THNetwork : Can not build network.");
            }
            if (!flg) throw new Exception("THNetwork : Can not build network.");
            List<int>[][][] onns = new List<int>[ln.GetUpperBound(0)][][];//[0,n)出力連結[出][出ノード][入][入ノード]
            try
            {
                Parallel.For(0, ln.GetUpperBound(0), po, (lind) =>//出力層
                {
                    if (cts.IsCancellationRequested) return;
                    int cnt3, cnt4, cnt5, oncnt;
                    int oln = ln.GetUpperBound(0) - lind;
                    List<int>[][] onnt = new List<int>[ln[lind]][];
                    for (cnt3 = 0; cnt3 < ln[lind]; cnt3++)
                    {
                        onnt[cnt3] = new List<int>[oln];
                        for (cnt4 = 0; cnt4 < onnt[cnt3].Length; cnt4++)
                        {
                            onnt[cnt3][cnt4] = new List<int>();
                        }

                    }
                    for (cnt3 = lind; cnt3 < ln.GetUpperBound(0); cnt3++)//入力層
                    {
                        if (cts.IsCancellationRequested) return;
                        if (inns[cnt3].Length != ln[cnt3 + 1])
                        {
                            Console.WriteLine("THNetwork : Can not verify node number.");
                            cts.Cancel();
                            return;
                        }
                        oncnt = cnt3 - lind;
                        for (cnt4 = 0; cnt4 < inns[cnt3].Length; cnt4++)//入力ノード
                        {
                            if (inns[cnt3][cnt4].Length <= lind)
                            {
                                Console.WriteLine("THNetwork : Can not verify connected layer number.");
                                cts.Cancel();
                                return;
                            }
                            for (cnt5 = 0; cnt5 < inns[cnt3][cnt4][lind].Count; cnt5++)//出力ノード
                            {
                                onnt[inns[cnt3][cnt4][lind][cnt5]][oncnt].Add(cnt4);
                            }
                        }
                    }
                    onns[lind] = onnt;
                });
            }
            catch (OperationCanceledException oce)
            {
                Console.WriteLine(oce.Message);
                throw new Exception("THNetwork : Can not build network.");
            }
            int cnt6, cnt7, cnt8, rks, rks2, ivs, ivs2, ivs3;
            int[] lrkst;
            bool flg2 = false;
            for (cnt = 0; cnt < ln.GetUpperBound(0); cnt++)//出力層
            {
                ivs3 = cnt + 1;//上層入力数
                for (cnt2 = 0; cnt2 < ln[cnt]; cnt2++)//出力ノード
                {
                    rks = 0;//出力数
                    ivs = ln.GetUpperBound(0) - cnt;//出力層数
                    if (ivs != onns[cnt][cnt2].Length) throw new ArgumentOutOfRangeException("ivs", "THNetwork : Can not verify layer number.");
                    for (cnt6 = 0; cnt6 < ivs; cnt6++)//入力層
                    {
                        rks += onns[cnt][cnt2][cnt6].Count;
                    }
                    if (rks == 0)
                    {
                        ivs2 = ln[ivs3];//上層ノード数
                        if (ivs2 != inns[cnt].Length) throw new ArgumentOutOfRangeException("ivs2", "THNetwork : Can not verify node number.");
                        flg = false;
                        flg2 = false;
                        lrkst = new int[ivs2];//上層ノード入力数リスト
                        for (cnt6 = Tcn[cnt].Item1; flg == false; cnt6++)//連結
                        {
                            if (!flg2)
                            {
                                for (cnt7 = 0; cnt7 < ivs2; cnt7++)//入力ノード
                                {
                                    rks2 = 0;//上層ノード入力数
                                    if (ivs3 != inns[cnt][cnt7].Length) throw new ArgumentOutOfRangeException("ivs3", "THNetwork : Can not verify layer number.");
                                    for (cnt8 = 0; cnt8 < ivs3; cnt8++)//出力層
                                    {
                                        rks2 += inns[cnt][cnt7][cnt8].Count;
                                    }
                                    if (rks2 <= cnt6)
                                    {
                                        if (inns[cnt][cnt7].GetUpperBound(0) != cnt) throw new ArgumentOutOfRangeException("cnt", "THNetwork : Can not verify layer index.");
                                        onns[cnt][cnt2][0].Add(cnt7);
                                        inns[cnt][cnt7][cnt].Add(cnt2);
                                        flg = true;
                                        break;
                                    }
                                    else
                                    {
                                        lrkst[cnt7] = rks2;
                                    }
                                }
                                flg2 = true;
                            }
                            else
                            {
                                for (cnt7 = 0; cnt7 < ivs2; cnt7++)//ノード
                                {
                                    if (lrkst[cnt7] <= cnt6)
                                    {
                                        if (inns[cnt][cnt7].GetUpperBound(0) != cnt) throw new ArgumentOutOfRangeException("cnt", "THNetwork : Can not verify layer index.");
                                        onns[cnt][cnt2][0].Add(cnt7);
                                        inns[cnt][cnt7][cnt].Add(cnt2);
                                        flg = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Nly = new THNLayer[ln.Length];
            Parallel.For(0, ln.Length, po, (lind) =>
                {
                    if (lind == 0)
                    {
                        List<int>[][] Ld = null;
                        if (Nfp[lind] == null)
                        {
                            int cnt3;
                            lock (lo) cnt3 = rnd.Next();
                            rndt = new Random(cnt3);
                            Nly[lind] = new THNLayer(in Dtn, lind, in ln, ref Ld, ref onns[lind], in rndt);
                        }
                        else if (Nfp[lind].Length == 1)
                        {
                            Nly[lind] = new THNLayer(in Dtn, lind, in ln, ref Ld, ref onns[lind], Nfp[lind][0]);
                        }
                        else if (Nfp[lind].Length != THFunc.GenSouKyou.FncN) throw new ArgumentOutOfRangeException("Nfp[lind]", "THNetwork : Incorrect length.");
                        else
                        {
                            int cnt3;
                            lock (lo) cnt3 = rnd.Next();
                            rndt = new Random(cnt3);
                            Nly[lind] = new THNLayer(in Dtn, lind, in ln, ref Ld, ref onns[lind], in rndt, in Nfp[lind]);
                        }
                    }
                    else if (lind == ln.GetUpperBound(0))
                    {
                        List<int>[][] Ld = null;
                        if (Nfp[lind] == null)
                        {
                            int cnt3;
                            lock (lo) cnt3 = rnd.Next();
                            rndt = new Random(cnt3);
                            Nly[lind] = new THNLayer(in Dtn, lind, in ln, ref inns[inns.GetUpperBound(0)], ref Ld, in rndt);
                        }
                        else if (Nfp[lind].Length == 1)
                        {
                            Nly[lind] = new THNLayer(in Dtn, lind, in ln, ref inns[inns.GetUpperBound(0)], ref Ld, Nfp[lind][0]);
                        }
                        else if (Nfp[lind].Length != THFunc.GenSouKyou.FncN) throw new ArgumentOutOfRangeException("Nfp[lind]", "THNetwork : Incorrect length.");
                        else
                        {
                            int cnt3;
                            lock (lo) cnt3 = rnd.Next();
                            rndt = new Random(cnt3);
                            Nly[lind] = new THNLayer(in Dtn, lind, in ln, ref inns[inns.GetUpperBound(0)], ref Ld, in rndt, in Nfp[lind]);
                        }
                    }
                    else
                    {
                        if (Nfp[lind] == null)
                        {
                            int cnt3;
                            lock (lo) cnt3 = rnd.Next();
                            rndt = new Random(cnt3);
                            Nly[lind] = new THNLayer(in Dtn, lind, in ln, ref inns[lind - 1], ref onns[lind], in rndt);
                        }
                        else if (Nfp[lind].Length == 1)
                        {
                            Nly[lind] = new THNLayer(in Dtn, lind, in ln, ref inns[lind - 1], ref onns[lind], Nfp[lind][0]);
                        }
                        else if (Nfp[lind].Length != THFunc.GenSouKyou.FncN) throw new ArgumentOutOfRangeException("Nfp[lind]", "THNetwork : Incorrect length.");
                        else
                        {
                            int cnt3;
                            lock (lo) cnt3 = rnd.Next();
                            rndt = new Random(cnt3);
                            Nly[lind] = new THNLayer(in Dtn, lind, in ln, ref inns[lind - 1], ref onns[lind], in rndt, in Nfp[lind]);
                        }
                    }
                });
        }
        internal THNetwork(in int D, in double[][] desin, in double[][] gdo, in int[] Nn, in int[][] Nf, in int[][][] lila, in int[][][] lina, in int[][][] lola, in int[][][] lona, in double[][][] Nrk)
        {
            if (D <= 1) throw new ArgumentOutOfRangeException("D", "THNetwork : Invalid data number.");
            if (Nn == null || Nn.Length <= 1) throw new ArgumentOutOfRangeException("Nn", "THNetwork : Invalid layer number.");
            if (desin == null || desin.Length != Nn[0] - 1) throw new ArgumentOutOfRangeException("desin", "THNetwork : Can not verify input descriptor number.");
            if (gdo == null || gdo.Length <= 0) throw new ArgumentOutOfRangeException("desin", "THNetwork : Invalid true data information.");
            if (Nf == null || lila == null || lina == null || lola == null || lona == null || Nrk == null || Nf.Length != Nn.Length || lila.Length != Nn.Length || lina.Length != lila.Length || lola.Length != lina.Length || lona.Length != lola.Length || Nrk.Length != lona.Length) throw new ArgumentOutOfRangeException("Nf/Nn/lila/lina/lola/lona/Nrk", "THNetwork : Can not verify layer number.");
            int cnt, cnt2;
            for (cnt = 0; cnt < desin.Length; cnt++)
            {
                if (desin[cnt] == null || desin[cnt].Length != D) throw new ArgumentOutOfRangeException("D/desin", "THNetwork : Can not verify data number.");
            }
            for (cnt = 0; cnt < gdo.Length; cnt++)
            {
                if (gdo[cnt] == null || gdo[cnt].Length != D) throw new ArgumentOutOfRangeException("D/desin", "THNetwork : Can not verify true data number.");
            }
            for (cnt = 0; cnt < Nn.Length; cnt++)
            {
                if (Nn[cnt] <= 0) throw new ArgumentOutOfRangeException("Nn", "THNetwork : Invalid node number.");
                if (Nf[cnt] == null || Nf[cnt].Length != Nn[cnt]) throw new ArgumentOutOfRangeException("Nf/Nn", "THNetwork : Can not verify node function information.");
                if (cnt == 0)
                {
                    if (lila[cnt] != null || lina[cnt] != null || Nrk[cnt] != null) throw new ArgumentOutOfRangeException("lila/lina/Nrk", string.Format("THNetwork : Invalid layer {0} input information.", cnt));
                }
                else
                {
                    if (lila[cnt] == null || lina[cnt] == null || Nrk[cnt] == null || lila[cnt].Length <= 0 || lila[cnt].Length != lina[cnt].Length || lina[cnt].Length != Nrk[cnt].Length) throw new ArgumentOutOfRangeException("lila/lina/Nrk", string.Format("THNetwork : Invalid layer {0} input information.", cnt));
                    for (cnt2 = 0; cnt2 < lila[cnt].Length; cnt2++)
                    {
                        if (lila[cnt][cnt2] == null || lina[cnt][cnt2] == null || Nrk[cnt][cnt2] == null || lila[cnt][cnt2].Length <= 0 || lila[cnt][cnt2].Length != lina[cnt][cnt2].Length || lina[cnt][cnt2].Length != Nrk[cnt][cnt2].Length) throw new ArgumentOutOfRangeException("lila/lina/Nrk", string.Format("THNetwork : Invalid layer {0} input information.", cnt));
                    }
                }
                if (cnt == Nn[^1])
                {
                    if (lola[cnt] != null || lona[cnt] != null) throw new ArgumentOutOfRangeException("lola/lona", string.Format("THNetwork : Invalid layer {0} output information.", cnt));
                }
                else
                {
                    if (lola[cnt] == null || lona[cnt] == null || lola[cnt].Length <= 0 || lola[cnt].Length != lona[cnt].Length) throw new ArgumentOutOfRangeException("lola/lona", string.Format("THNetwork : Invalid layer {0} output information.", cnt));
                    for (cnt2 = 0; cnt2 < lola[cnt].Length; cnt2++)
                    {
                        if (lola[cnt][cnt2] == null || lona[cnt][cnt2] == null || lola[cnt][cnt2].Length <= 0 || lola[cnt][cnt2].Length != lona[cnt][cnt2].Length) throw new ArgumentOutOfRangeException("lola/lona", string.Format("THNetwork : Invalid layer {0} output information.", cnt));
                    }
                }
            }
            Dtn = D;
            Nly = new THNLayer[Nn.Length];
            TD = gdo;
            rnd = new Random();
            cts = new CancellationTokenSource();
            po = new ParallelOptions();
            po.CancellationToken = cts.Token;
            po.TaskScheduler = TaskScheduler.Default;
            po.MaxDegreeOfParallelism = CommonParam.thdn;
            for (cnt = 0; cnt < Nly.Length; cnt++)
            {
                try
                {
                    Nly[cnt] = new THNLayer(in Dtn, in Nn, in cnt, in lila[cnt], in lina[cnt], in Nrk[cnt], in lola[cnt], in lona[cnt], in Nf[cnt]);
                }
                catch (ArgumentOutOfRangeException aore)
                {
                    Console.WriteLine("{0} => {1}", aore.ParamName, aore.Message);
                    throw new Exception("THNetwork : Can not create Touhou Network.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    if (e.InnerException != null) Console.WriteLine(e.InnerException);
                    throw new Exception("THNetwork : Can not create Touhou Network.");
                }
            }
            for (cnt = 0; cnt < desin.Length; cnt++)
            {
                Nly[0].N(cnt).TGs(desin[cnt]);
            }
            for (cnt = 1; cnt < Nrk.Length; cnt++)
            {
                for (cnt2 = 0; cnt2 < Nn[cnt]; cnt2++)
                {
                    Nly[cnt].N(cnt2).K(Nrk[cnt][cnt2]);
                }
            }
        }
        internal THNetwork(in int Dy, in double[][] desin, in int[] Nn, in int[][] Nf, in int[][][] lila, in int[][][] lina, in int[][][] lola, in int[][][] lona, in double[][][] Nrk)
        {
            if (Dy <= 1) throw new ArgumentOutOfRangeException("Dy", "THNetwork : Invalid data number.");
            if (Nn == null || Nn.Length <= 1) throw new ArgumentOutOfRangeException("Nn", "THNetwork : Invalid layer number.");
            if (desin == null || desin.Length != Nn[0] - 1) throw new ArgumentOutOfRangeException("desin", "THNetwork : Can not verify input descriptor number.");
            if (Nf == null || lila == null || lina == null || lola == null || lona == null || Nrk == null || Nf.Length != Nn.Length || lila.Length != Nn.Length || lina.Length != lila.Length || lola.Length != lina.Length || lona.Length != lola.Length || Nrk.Length != lona.Length) throw new ArgumentOutOfRangeException("Nf/Nn/lila/lina/lola/lona/Nrk", "THNetwork : Can not verify layer number.");
            int cnt, cnt2;
            for (cnt = 0; cnt < desin.Length; cnt++)
            {
                if (desin[cnt] == null || desin[cnt].Length != Dy) throw new ArgumentOutOfRangeException("Dy/desin", "THNetwork : Can not verify data number.");
            }
            for (cnt = 0; cnt < Nn.Length; cnt++)
            {
                if (Nn[cnt] <= 0) throw new ArgumentOutOfRangeException("Nn", "THNetwork : Invalid node number.");
                if (Nf[cnt] == null || Nf[cnt].Length != Nn[cnt]) throw new ArgumentOutOfRangeException("Nf/Nn", "THNetwork : Can not verify node function information.");
                if (lila[cnt] == null || lina[cnt] == null || Nrk[cnt] == null || lila[cnt].Length != Nn[cnt] || lila[cnt].Length != lina[cnt].Length || lina[cnt].Length != Nrk[cnt].Length) throw new ArgumentOutOfRangeException("lila/lina/Nrk", string.Format("THNetwork : Invalid layer {0} input information.", cnt));
                if (lola[cnt] == null || lona[cnt] == null || lola[cnt].Length != Nn[cnt] || lola[cnt].Length != lona[cnt].Length) throw new ArgumentOutOfRangeException("lola/lona", string.Format("THNetwork : Invalid layer {0} output information.", cnt));
                if (cnt == 0)
                {
                    for (cnt2 = 0; cnt2 < Nn[cnt]; cnt2++)
                    {
                        if (lila[cnt][cnt2] != null || lina[cnt][cnt2] != null || Nrk[cnt][cnt2] != null) throw new ArgumentOutOfRangeException("lila/lina/Nrk", string.Format("THNetwork : Invalid layer {0} node {1} input information.", cnt, cnt2));
                    }
                }
                else
                {
                    for (cnt2 = 0; cnt2 < Nn[cnt]; cnt2++)
                    {
                        if (lila[cnt][cnt2] == null || lina[cnt][cnt2] == null || Nrk[cnt][cnt2] == null || lila[cnt][cnt2].Length <= 0 || lila[cnt][cnt2].Length != lina[cnt][cnt2].Length || lina[cnt][cnt2].Length != Nrk[cnt][cnt2].Length) throw new ArgumentOutOfRangeException("lila/lina/Nrk", string.Format("THNetwork : Invalid layer {0} input information.", cnt));
                    }
                }
                if (cnt == Nn.GetUpperBound(0))
                {
                    for (cnt2 = 0; cnt2 < Nn[cnt]; cnt2++)
                    {
                        if (lola[cnt][cnt2] != null || lona[cnt][cnt2] != null) throw new ArgumentOutOfRangeException("lola/lona", string.Format("THNetwork : Invalid layer {0} node {1} output information.", cnt, cnt2));
                    }
                }
                else
                {
                    for (cnt2 = 0; cnt2 < lola[cnt].Length; cnt2++)
                    {
                        if (lola[cnt][cnt2] == null || lona[cnt][cnt2] == null || lola[cnt][cnt2].Length <= 0 || lola[cnt][cnt2].Length != lona[cnt][cnt2].Length) throw new ArgumentOutOfRangeException("lola/lona", string.Format("THNetwork : Invalid layer {0} node {1} output information.", cnt, cnt2));
                    }
                }
            }
            Dtn = Dy;
            Nly = new THNLayer[Nn.Length];
            TD = null;
            rnd = new Random();
            cts = new CancellationTokenSource();
            po = new ParallelOptions();
            po.CancellationToken = cts.Token;
            po.TaskScheduler = TaskScheduler.Default;
            po.MaxDegreeOfParallelism = CommonParam.thdn;
            for (cnt = 0; cnt < Nly.Length; cnt++)
            {
                try
                {
                    Nly[cnt] = new THNLayer(in Dtn, in Nn, in cnt, in lila[cnt], in lina[cnt], in Nrk[cnt], in lola[cnt], in lona[cnt], in Nf[cnt]);
                }
                catch (ArgumentOutOfRangeException aore)
                {
                    Console.WriteLine("{0} => {1}", aore.ParamName, aore.Message);
                    throw new Exception("THNetwork : Can not create Touhou Network.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    if (e.InnerException != null) Console.WriteLine(e.InnerException);
                    throw new Exception("THNetwork : Can not create Touhou Network.");
                }
            }
            for (cnt = 0; cnt < desin.Length; cnt++)
            {
                Nly[0].N(cnt).TGs(desin[cnt]);
            }
            for (cnt = 1; cnt < Nrk.Length; cnt++)
            {
                for (cnt2 = 0; cnt2 < Nn[cnt]; cnt2++)
                {
                    Nly[cnt].N(cnt2).K(Nrk[cnt][cnt2]);
                }
            }
        }
        internal THNetwork()
        {

        }
        static private void NSKO(in int li, in double[] Fw, in int[] ln, in Tuple<int, int> Tcn, out List<int>[][] inn, in Random R, in int[][] Nl, ref ParallelOptions PO)//層連結初期化、liは層インデックス、Fwは入力層からの超層連結率、lnはノード数、Tcnは連結数範囲、innは入力ノード、Nlはノードリスト
        {
            if (li != Fw.Length) throw new ArgumentOutOfRangeException("li/Fw", "NSKO : Incorrect length.");
            if (Tcn.Item1 > Tcn.Item2) throw new ArgumentOutOfRangeException("Tcn", "NSKO : Invalid range.");
            if (PO.CancellationToken.IsCancellationRequested)
            {
                inn = null;
                return;
            }
            double fws = 0.0;//PDF正規化係数
            double[] Fwc = new double[Fw.Length];//CDF
            int cnt, cnt2, itemp;
            for (cnt = 0; cnt < Fw.Length; cnt++)
            {
                fws += Fw[cnt];
                if (cnt == 0) Fwc[cnt] = Fw[cnt];
                else Fwc[cnt] = Fwc[cnt - 1] + Fw[cnt];
            }
            double rdtemp;
            int ritemp;
            List<int>[] Nlc;
            int nc = 0;
            if (Tcn.Item1 == Tcn.Item2) nc = Tcn.Item1;
            int RR = Tcn.Item2 - Tcn.Item1 + 1;//連結範囲幅
            inn = new List<int>[ln[li]][];
            for (cnt = 0; cnt < ln[li]; cnt++)//入力層ノード
            {
                if (PO.CancellationToken.IsCancellationRequested) return;
                if (Tcn.Item1 != Tcn.Item2) nc = R.Next(RR) + Tcn.Item1;
                Nlc = new List<int>[li];
                inn[cnt] = new List<int>[li];
                for (cnt2 = 0; cnt2 < li; cnt2++)
                {
                    Nlc[cnt2] = new List<int>(Nl[cnt2]);
                    inn[cnt][cnt2] = new List<int>();
                }
                for (itemp = nc; itemp > 0; itemp--)//連結カウンター
                {
                    rdtemp = R.NextDouble() * fws;
                    for (cnt2 = 0; cnt2 < li; cnt2++)//出力層
                    {
                        if (rdtemp < Fwc[cnt2])
                        {
                            if (Nlc[cnt2].Count == 0) break;
                            ritemp = R.Next(Nlc[cnt2].Count);
                            inn[cnt][cnt2].Add(Nlc[cnt2][ritemp]);
                            Nlc[cnt2].RemoveAt(ritemp);
                            break;
                        }
                    }
                }
            }
        }
        internal void THniru(double[][] desin, double[][] gdo)//ネットワーク初期化、ランダム(一様分布)、desin[N0-1][Dtn]は入力ノードのデータ、gdo[Nn-1][Dtn]は真データ
        {
            if (gdo.Length != Nly[Nly.GetUpperBound(0)].Nn) throw new ArgumentOutOfRangeException("gdo", "THniru : Incorrect output node length.");
            foreach (double[] datemp in gdo) if (datemp.Length != Dtn) throw new ArgumentOutOfRangeException("gdo", "THniru : Incorrect output data length.");
            TD = gdo;
            int L0l = Nly[0].Nn - 1;
            if (desin.Length != L0l) throw new ArgumentOutOfRangeException("desin", "THniru : Incorrect input node length.");
            double[][] Rrha = new double[Nly.GetUpperBound(0)][];//乱数半幅
            double[][] Rra = new double[Nly.GetUpperBound(0)][];//乱数幅
            Rrha[0] = new double[Nly[0].Nn];
            Rra[0] = new double[Nly[0].Nn];
            Parallel.For(0, L0l, po, (nind) =>//入力層ノード
            {
                if (desin[nind].Length != Dtn) throw new ArgumentOutOfRangeException("desin", "THniru : Incorrect input data length.");
                Nly[0].N(nind).TGs(desin[nind]);
                double Pj = 0.0;//値の平均
                for (int cnt = 0; cnt < Dtn; cnt++)
                {
                    Pj += Nly[0].N(nind).F(cnt);
                }
                Pj /= Dtn;
                if (double.IsNaN(Pj) || double.IsInfinity(Pj)) throw new ArgumentOutOfRangeException("Pj", "THniru : Invalid number.");
                double Fc = 0.0;//値の分散
                double dtemp;
                for (int cnt = 0; cnt < Dtn; cnt++)
                {
                    dtemp = Nly[0].N(nind).F(cnt) - Pj;
                    Fc += dtemp * dtemp;
                }
                Fc /= Dtn;
                dtemp = Math.Sqrt(Fc) + Math.Abs(Pj);
                if (double.IsNaN(dtemp) || double.IsInfinity(dtemp)) throw new ArgumentOutOfRangeException("dtemp", "THniru : Invalid scale.");
                if (dtemp == 0.0) dtemp = double.PositiveInfinity;
                Rrha[0][nind] = 4.0 / dtemp;//乱数半幅
                if (double.IsNaN(Rrha[0][nind]) || double.IsInfinity(Rrha[0][nind])) Rrha[0][nind] = 0.0;
                Rra[0][nind] = 2.0 * Rrha[0][nind];//乱数幅
            });
            Rrha[0][L0l] = 3.0;
            Rra[0][L0l] = 2.0 * Rrha[0][Nly[0].Nn - 1];
            int cnt2;
            for (cnt2 = 1; cnt2 < Nly.Length; cnt2++)//入力層以外
            {
                if (cnt2 != Nly.GetUpperBound(0))
                {
                    Rrha[cnt2] = new double[Nly[cnt2].Nn];
                    Rra[cnt2] = new double[Nly[cnt2].Nn];
                }
                Parallel.For(0, Nly[cnt2].Nn, po, (nind) =>//ノード
                {
                    int cnt, cnt3;
                    lock (lo) cnt = rnd.Next();
                    rndt = new Random(cnt);
                    double sx;
                    for (cnt3 = 0; cnt3 < Nly[cnt2].N(nind).SR; cnt3++)
                    {
                        sx = rndt.NextDouble() * Rra[Nly[cnt2].N(nind).NRL(cnt3)][Nly[cnt2].N(nind).NRN(cnt3)] - Rrha[Nly[cnt2].N(nind).NRL(cnt3)][Nly[cnt2].N(nind).NRN(cnt3)];
                        Nly[cnt2].N(nind).K(cnt3, sx);
                    }
                    for (cnt3 = 0; cnt3 < Dtn; cnt3++)//データ
                    {
                        sx = 0.0;
                        for (cnt = 0; cnt < Nly[cnt2].N(nind).SR; cnt++)//入力
                        {
                            sx += Nly[Nly[cnt2].N(nind).NRL(cnt)].N(Nly[cnt2].N(nind).NRN(cnt)).F(cnt3) * Nly[cnt2].N(nind).K(cnt);
                        }
                        Nly[cnt2].N(nind).TGs(sx, cnt3);
                    }
                    double Pj = 0.0;//値の平均
                    for (cnt = 0; cnt < Dtn; cnt++)
                    {
                        Pj += Nly[cnt2].N(nind).F(cnt);
                    }
                    Pj /= Dtn;
                    if (double.IsNaN(Pj) || double.IsInfinity(Pj)) throw new ArgumentOutOfRangeException("Pj", "THniru : Invalid number.");
                    double Fc = 0.0;//値の分散
                    double dtemp;
                    for (cnt = 0; cnt < Dtn; cnt++)
                    {
                        dtemp = Nly[cnt2].N(nind).F(cnt) - Pj;
                        Fc += dtemp * dtemp;
                    }
                    Fc /= Dtn;
                    dtemp = Math.Sqrt(Fc) + Math.Abs(Pj);
                    if (double.IsNaN(dtemp) || double.IsInfinity(dtemp)) throw new ArgumentOutOfRangeException("dtemp", "THniru : Invalid scale.");
                    if (dtemp == 0.0) dtemp = double.PositiveInfinity;
                    if (cnt2 != Nly.GetUpperBound(0))
                    {
                        Rrha[cnt2][nind] = 4.0 / dtemp;//乱数半幅
                        if (double.IsNaN(Rrha[cnt2][nind]) || double.IsInfinity(Rrha[cnt2][nind])) Rrha[cnt2][nind] = 0.0;
                        Rra[cnt2][nind] = 2 * Rrha[cnt2][nind];//乱数幅q
                    }
                });
            }
        }
        private void FP()//順伝播
        {
            Parallel.For(0, Dtn, po, (dind) =>//データ毎
            {
                double dtemp;
                for (int cnt = 1; cnt < Nly.Length; cnt++)//入力層以外
                {
                    for (int cnt2 = 0; cnt2 < Nly[cnt].Nn; cnt2++)//ノード
                    {
                        dtemp = 0.0;
                        for (int cnt3 = 0; cnt3 < Nly[cnt].N(cnt2).SR; cnt3++)//入力
                        {
                            dtemp += Nly[Nly[cnt].N(cnt2).NRL(cnt3)].N(Nly[cnt].N(cnt2).NRN(cnt3)).F(dind) * Nly[cnt].N(cnt2).K(cnt3);
                        }
                        Nly[cnt].N(cnt2).TGs(dtemp, dind);
                    }
                }
            });
        }
        private bool SSye(in double[][] cdr, in Tuple<double, double>[][] e, out double[][] E)//収束判断(収率/ee%)、cdrは値域区間、eは収束条件(1<2区間、1=2誤差、1>2固定誤差)、Eは最小二乗微分(予測と真値の差)
        {
            if (cdr.Length != Nly[Nly.GetUpperBound(0)].Nn || e.Length != Nly[Nly.GetUpperBound(0)].Nn) throw new ArgumentOutOfRangeException("cdr/e", "SSye : Incorrect length.");
            E = new double[Nly[Nly.GetUpperBound(0)].Nn][];
            bool flg = true;
            bool flg2;
            double dtemp;
            int cnt, cnt2, cnt3;
            for (cnt = 0; cnt < Nly[Nly.GetUpperBound(0)].Nn; cnt++)//ノード
            {
                if (cdr[cnt].Length != e[cnt].Length) throw new ArgumentOutOfRangeException("cdr/e", "SSye : Incorrect length.");
                E[cnt] = new double[Dtn];
                for (cnt2 = 0; cnt2 < Dtn; cnt2++)//データ
                {
                    dtemp = Nly[Nly.GetUpperBound(0)].N(cnt).F(cnt2) - TD[cnt][cnt2];
                    flg2 = false;
                    for (cnt3 = 0; cnt3 < cdr[cnt].Length; cnt3++)//区間
                    {
                        if (TD[cnt][cnt2] < cdr[cnt][cnt3])
                        {
                            flg2 = true;
                            //Console.WriteLine(dtemp);//debug用
                            if (e[cnt][cnt3].Item1 < e[cnt][cnt3].Item2)
                            {
                                if (Nly[Nly.GetUpperBound(0)].N(cnt).F(cnt2) >= e[cnt][cnt3].Item1 && Nly[Nly.GetUpperBound(0)].N(cnt).F(cnt2) <= e[cnt][cnt3].Item2) E[cnt][cnt2] = 0.0;
                                else
                                {
                                    if (flg) flg = false;
                                    E[cnt][cnt2] = dtemp;
                                }
                            }
                            else if (e[cnt][cnt3].Item1 > e[cnt][cnt3].Item2)
                            {
                                if (dtemp < e[cnt][cnt3].Item1 && dtemp > e[cnt][cnt3].Item2) E[cnt][cnt2] = 0.0;
                                else
                                {
                                    if (flg) flg = false;
                                    E[cnt][cnt2] = dtemp;
                                }
                            }
                            else
                            {
                                if (Math.Abs(dtemp) < e[cnt][cnt3].Item1 * TD[cnt][cnt2]) E[cnt][cnt2] = 0.0;
                                else
                                {
                                    if (flg) flg = false;
                                    E[cnt][cnt2] = dtemp;
                                }
                            }
                        }
                    }
                    if (!flg2) throw new ArgumentOutOfRangeException("TD/e", "SSye : Data is out of region.");
                }
            }
            return flg;
        }
        private void BP(in double[][] E, double G)//逆伝播、Gは学習率
        {
            for (int cnt = 0; cnt < Nly[Nly.GetUpperBound(0)].Nn; cnt++)//ノード
            {
                for (int cnt2 = 0; cnt2 < Dtn; cnt2++)//データ
                {
                    Nly[Nly.GetUpperBound(0)].N(cnt).B(E[cnt][cnt2], cnt2);
                }
            }
            bool flg = false;
            for (int cnt = Nly.GetUpperBound(0); cnt > 0; cnt--)//層
            {
                Parallel.For(0, Nly[cnt].Nn, po, (nind) =>//ノード
                {
                    double[,] MIP = new double[Dtn, Nly[cnt].N(nind).SR];//入力行列
                    double[] B = new double[Dtn];//ノードの微分値
                    double[] H = new double[Dtn];//ポストノードの偏微分値
                    double[] L = new double[Dtn];//ノードの偏微分値
                    double[] dW = new double[Nly[cnt].N(nind).SR];//Wの偏微分
                    double[] Fks = new double[Dtn];//更新値
                    int cnt2, cnt3;
                    //double dhb = 0.0;
                    //double dtemp, wtemp;
                    //double Gd = G;
                    //double Gs;
                    try
                    {
                        for (cnt2 = 0; cnt2 < Dtn; cnt2++)//データ
                        {
                            Fks[cnt2] = Nly[cnt].N(nind).KSCn(cnt2);//更新値
                            B[cnt2] = Nly[cnt].N(nind).DSCn(cnt2);
                            H[cnt2] = Nly[cnt].N(nind).B(cnt2);
                            L[cnt2] = H[cnt2] * B[cnt2];
                            for (cnt3 = 0; cnt3 < Nly[cnt].N(nind).SR; cnt3++)//入力
                            {
                                if (Nly[cnt].N(nind).NRL(cnt3) != 0 && L[cnt2] != 0.0) lock (Nly[Nly[cnt].N(nind).NRL(cnt3)].N(Nly[cnt].N(nind).NRN(cnt3)).HL) Nly[Nly[cnt].N(nind).NRL(cnt3)].N(Nly[cnt].N(nind).NRN(cnt3)).Bs(L[cnt2] * Nly[cnt].N(nind).K(cnt3), cnt2);//下層偏微分更新
                                MIP[cnt2, cnt3] = Nly[Nly[cnt].N(nind).NRL(cnt3)].N(Nly[cnt].N(nind).NRN(cnt3)).F(cnt2);//入力行列
                                dW[cnt3] += MIP[cnt2, cnt3] * L[cnt2];
                            }
                            Nly[cnt].N(nind).B(0.0, cnt2);//偏微分リセット
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        if (e.InnerException != null) Console.WriteLine(e);
                        Console.WriteLine("Can not perform back propagaion.");
                        flg = true;
                        return;
                    }
                    SGKC.Tensor tx = new SGKC.Tensor(MIP);//X
                    SGKC.Tensor ty = new SGKC.Tensor(Fks, false);//Y
                    try
                    {
                        //throw new Exception();
                        ty = SGKC.Tensor.TKzsDX(tx.TMPiDX(), ty);//X^(-1)Y
                        tx = new SGKC.Tensor(Nly[cnt].N(nind).K(), false);//W
                        ///*
                        Fks = ty.vector;
                        for (cnt2 = 0; cnt2 < Fks.Length; cnt2++)
                        {
                            if (Math.Abs(Fks[cnt2]) > 92500)
                            {
                                Fks[cnt2] = Math.CopySign(92500, Fks[cnt2]);
                            }
                        }
                        ty = new SGKC.Tensor(Fks, false);//*/
                        tx = SGKC.Tensor.TTzsDX(SGKC.Tensor.TBKzsDX(1 - G, tx), SGKC.Tensor.TBKzsDX(G, ty));//W'
                        Nly[cnt].N(nind).K(tx.vector);
                    }
                    catch (Exception)
                    {
                        double sra, srr;
                        int srn;
                        for (cnt2 = 0; cnt2 < Nly[cnt].N(nind).SR; cnt2++)//入力
                        {
                            sra = 0.0;
                            srn = 0;
                            for (cnt3 = 0; cnt3 < Dtn; cnt3++)//データ
                            {
                                if (Nly[Nly[cnt].N(nind).NRL(cnt2)].N(Nly[cnt].N(nind).NRN(cnt2)).F(cnt3) != 0.0)
                                {
                                    sra += Math.Abs(Nly[Nly[cnt].N(nind).NRL(cnt2)].N(Nly[cnt].N(nind).NRN(cnt2)).F(cnt3));
                                    srn++;
                                }
                            }
                            lock (lo) srr = rnd.NextDouble();
                            if (sra == 0.0 || srn == 0)
                            {
                                Nly[cnt].N(nind).K(cnt2, srr <= 0.5 ? -1.0 : 1.0);
                            }
                            else
                            {
                                sra /= srn;
                                sra = 1 / sra;
                                Nly[cnt].N(nind).K(cnt2, srr <= 0.5 ? -sra : sra);
                            }
                        }
                        /*
                        lock (lo) Gs = rnd.NextDouble();
                        if (Gs < 0.01)
                        {
                            Fks = new double[Nly[cnt].N(nind).SR];
                            for (cnt2 = 0; cnt2 < Fks.Length; cnt2++)
                            {
                                lock (lo) Fks[cnt2] = rnd.NextDouble() * 0.01 - 0.005;
                            }
                            Nly[cnt].N(nind).K(Fks);
                            return;
                        }//*/
                        /*
                        for (cnt2 = 0; cnt2 < Dtn; cnt2++)
                        {
                            dtemp = Math.Abs(B[cnt2]);
                            if (dtemp > dhb) dhb = dtemp;
                            dtemp = Math.Abs(H[cnt2]);
                            if (dtemp > dhb) dhb = dtemp;
                        }
                        if (dhb == 0.0) return;
                        else if (dhb < 0.999)
                        {
                            if (dhb < 0.0019)
                            {
                                Gd *= 9.99;
                            }
                            else if (dhb < 0.00514)
                            {
                                Gd *= 3.4;
                            }
                            else if (dhb < 0.015)
                            {
                                Gd *= 1.04;
                            }
                            else if (dhb < 0.077)
                            {
                                Gd *= 0.495;
                            }
                            else if (dhb < 0.34)
                            {
                                Gd *= 0.34;
                            }
                            else
                            {
                                Gd *= 0.19;
                            }
                        }
                        else if (dhb < 514.0)
                        {
                            if (dhb < 4.95) { }
                            else if (dhb < 17.0)
                            {
                                Gd *= 0.17;
                            }
                            else if (dhb < 69.0)
                            {
                                Gd *= 0.077;
                            }
                            else if (dhb < 99.9)
                            {
                                Gd *= 0.069;
                            }
                            else if (dhb < 170.0)
                            {
                                Gd *= 0.0514;
                            }
                            else if (dhb < 343.4)
                            {
                                Gd *= 0.0495;
                            }
                            else
                            {
                                Gd *= 0.03434;
                            }
                        }
                        else
                        {
                            if (dhb < 999)
                            {
                                Gd *= 0.017;
                            }
                            else if (dhb < 1500.0)
                            {
                                Gd *= 0.00495;
                            }
                            else if (dhb < 4950.0)
                            {
                                Gd *= 0.0019;
                            }
                            else if (dhb < 10400.0)
                            {
                                Gd *= 0.000999;
                            }
                            else if (dhb < 12800.0)
                            {
                                Gd *= 0.000925;
                            }
                            else
                            {
                                Gd = 1.0 / Math.Sqrt(dhb);
                            }
                        }
                        //*/
                        /*
                        Gs = 0.01 * Gd;
                        for (cnt2 = 0; cnt2 < Nly[cnt].N(nind).SR; cnt2++)//入力
                        {
                            wtemp = dW[cnt2] * Gs;
                            if (wtemp == 0.0) continue;
                            if (wtemp > 0.0)
                            {
                                if (Nly[cnt].N(nind).K(cnt2) >= 0.0)
                                {
                                    if (Nly[cnt].N(nind).K(cnt2) <= 0.69)
                                    {
                                        if (Nly[cnt].N(nind).K(cnt2) <= 0.000514)
                                        {
                                            if (wtemp < 0.00104) Nly[cnt].N(nind).K(cnt2, Nly[cnt].N(nind).K(cnt2) - wtemp);
                                            else Nly[cnt].N(nind).K(cnt2, Nly[cnt].N(nind).K(cnt2) - 0.00104);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 0.003434)
                                        {
                                            if (wtemp < 0.000999) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.000999;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 0.000514 ? 0.000514 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 0.0077)
                                        {
                                            if (wtemp < 0.003434) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.003434;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 0.003434 ? 0.003434 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 0.0495)
                                        {
                                            if (wtemp < 0.017) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.017;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 0.0077 ? 0.0077 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 0.19)
                                        {
                                            if (wtemp < 0.077) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.077;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 0.0495 ? 0.0495 : Gd);
                                        }
                                        else
                                        {
                                            if (wtemp < 0.3434) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.3434;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 0.19 ? 0.19 : Gd);
                                        }
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 495.0)
                                    {
                                        if (Nly[cnt].N(nind).K(cnt2) <= 1.7)
                                        {
                                            if (wtemp < 0.514) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.514;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 0.69 ? 0.69 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 5.14)
                                        {
                                            if (wtemp < 1.9) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 1.9;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 1.7 ? 1.7 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 34.34)
                                        {
                                            if (wtemp < 9.99) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 9.99;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 5.14 ? 5.14 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 104.0)
                                        {
                                            if (wtemp < 4.95) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 4.95;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 34.34 ? 34.34 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 170.0)
                                        {
                                            if (wtemp < 69.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 69.0;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 104.0 ? 104.0 : Gd);
                                        }
                                        else
                                        {
                                            if (wtemp < 150.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 150.0;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 170.0 ? 170.0 : Gd);
                                        }
                                    }
                                    else
                                    {
                                        if (Nly[cnt].N(nind).K(cnt2) <= 770.0)
                                        {
                                            if (wtemp < 343.4) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 343.4;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 495.0 ? 495.0 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 3434.0)
                                        {
                                            if (wtemp < 495.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 495.0;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 770.0 ? 770.0 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 10400.0)
                                        {
                                            if (wtemp < 690.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 690.0;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 3434.0 ? 3434.0 : Gd);
                                        }
                                        else
                                        {
                                            Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 10400.0 ? 10400.0 : Gd);
                                        }
                                    }
                                }
                                else
                                {
                                    if (Nly[cnt].N(nind).K(cnt2) >= -0.69)
                                    {
                                        if (Nly[cnt].N(nind).K(cnt2) >= -0.000514)
                                        {
                                            if (wtemp < 0.00019) Nly[cnt].N(nind).K(cnt2, Nly[cnt].N(nind).K(cnt2) - wtemp);
                                            else Nly[cnt].N(nind).K(cnt2, Nly[cnt].N(nind).K(cnt2) - 0.00019);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -0.003434)
                                        {
                                            if (wtemp < 0.0015) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.0015;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -0.0077 ? -0.0077 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -0.0077)
                                        {
                                            if (wtemp < 0.017) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.017;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -0.0495 ? -0.0495 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -0.0495)
                                        {
                                            if (wtemp < 0.069) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.069;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -0.19 ? -0.19 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -0.19)
                                        {
                                            if (wtemp < 0.19) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.19;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -0.69 ? -0.69 : Gd);
                                        }
                                        else
                                        {
                                            if (wtemp < 0.34) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.34;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -1.7 ? -1.7 : Gd);
                                        }
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -495.0)
                                    {
                                        if (Nly[cnt].N(nind).K(cnt2) >= -1.7)
                                        {
                                            if (wtemp < 0.999) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.999;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -5.14 ? -5.14 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -5.14)
                                        {
                                            if (wtemp < 7.7) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 7.7;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -34.34 ? -34.34 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -34.34)
                                        {
                                            if (wtemp < 15.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 15.0;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -104.0 ? -104.0 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -104.0)
                                        {
                                            if (wtemp < 19.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 19.0;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -170.0 ? -170.0 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -170.0)
                                        {
                                            if (wtemp < 34.34) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 34.34;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -495.0 ? -495.0 : Gd);
                                        }
                                        else
                                        {
                                            if (wtemp < 51.4) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 51.4;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -770.0 ? -770.0 : Gd);
                                        }
                                    }
                                    else
                                    {
                                        if (Nly[cnt].N(nind).K(cnt2) >= -770.0)
                                        {
                                            if (wtemp < 99.9) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 99.9;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -3434.0 ? -3434.0 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -3434.0)
                                        {
                                            if (wtemp < 170.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 170.0;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -10400.0 ? -10400.0 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -10400.0)
                                        {
                                            if (wtemp < 343.4) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 343.4;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -51400.0 ? -51400.0 : Gd);
                                        }
                                        else
                                        {
                                            Nly[cnt].N(nind).K(cnt2, wtemp < 495.0 ? (Nly[cnt].N(nind).K(cnt2) - wtemp) : (Nly[cnt].N(nind).K(cnt2) - 495.0));
                                        }
                                    }
                                }

                            }
                            else
                            {
                                if (Nly[cnt].N(nind).K(cnt2) >= 0.0)
                                {
                                    if (Nly[cnt].N(nind).K(cnt2) <= 0.000514)
                                    {
                                        if (wtemp > -0.00019) Nly[cnt].N(nind).K(cnt2, Nly[cnt].N(nind).K(cnt2) - wtemp);
                                        else Nly[cnt].N(nind).K(cnt2, Nly[cnt].N(nind).K(cnt2) + 0.00019);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 0.003434)
                                    {
                                        if (wtemp > -0.0015) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.0015;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 0.0077 ? 0.0077 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 0.0077)
                                    {
                                        if (wtemp > -0.017) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.017;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 0.0495 ? 0.0495 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 0.0495)
                                    {
                                        if (wtemp > -0.069) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.069;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 0.19 ? 0.19 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 0.19)
                                    {
                                        if (wtemp > -0.19) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.19;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 0.69 ? 0.69 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 0.69)
                                    {
                                        if (wtemp > -0.34) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.34;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 1.7 ? 1.7 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 1.7)
                                    {
                                        if (wtemp > -0.999) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.999;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 5.14 ? 5.14 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 5.14)
                                    {
                                        if (wtemp > -7.7) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 7.7;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 34.34 ? 34.34 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 34.34)
                                    {
                                        if (wtemp > -15.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 15.0;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 104.0 ? 104.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 104.0)
                                    {
                                        if (wtemp > -19.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 19.0;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 170.0 ? 170.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 170.0)
                                    {
                                        if (wtemp > -34.34) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 34.34;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 495.0 ? 495.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 495.0)
                                    {
                                        if (wtemp > -51.4) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 51.4;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 770.0 ? 770.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 770.0)
                                    {
                                        if (wtemp > -99.9) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 99.9;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 3434.0 ? 3434.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 3434.0)
                                    {
                                        if (wtemp > -170.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 170.0;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 10400.0 ? 10400.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 10400.0)
                                    {
                                        if (wtemp > -343.4) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 343.4;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 51400.0 ? 51400.0 : Gd);
                                    }
                                    else
                                    {
                                        Nly[cnt].N(nind).K(cnt2, wtemp > -495.0 ? (Nly[cnt].N(nind).K(cnt2) - wtemp) : (Nly[cnt].N(nind).K(cnt2) + 495.0));
                                    }
                                }
                                else
                                {
                                    if (Nly[cnt].N(nind).K(cnt2) >= -0.000514)
                                    {
                                        if (wtemp > -0.00104) Nly[cnt].N(nind).K(cnt2, Nly[cnt].N(nind).K(cnt2) - wtemp);
                                        else Nly[cnt].N(nind).K(cnt2, Nly[cnt].N(nind).K(cnt2) + 0.00104);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -0.003434)
                                    {
                                        if (wtemp > -0.000999) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.000999;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -0.000514 ? -0.000514 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -0.0077)
                                    {
                                        if (wtemp > -0.003434) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.003434;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -0.003434 ? -0.003434 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -0.0495)
                                    {
                                        if (wtemp > -0.017) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.017;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -0.0077 ? -0.0077 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -0.19)
                                    {
                                        if (wtemp > -0.077) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.077;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -0.0495 ? -0.0495 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -0.69)
                                    {
                                        if (wtemp > -0.3434) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.3434;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -0.19 ? -0.19 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -1.7)
                                    {
                                        if (wtemp > -0.514) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.514;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -0.69 ? -0.69 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -5.14)
                                    {
                                        if (wtemp > -1.9) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 1.9;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -1.7 ? -1.7 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -34.34)
                                    {
                                        if (wtemp > -9.99) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 9.99;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -5.14 ? -5.14 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -104.0)
                                    {
                                        if (wtemp > -4.95) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 4.95;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -34.34 ? -34.34 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -170.0)
                                    {
                                        if (wtemp > -69.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 69.0;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -104.0 ? -104.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -495.0)
                                    {
                                        if (wtemp > -150.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 150.0;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -170.0 ? -170.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -770.0)
                                    {
                                        if (wtemp > -343.4) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 343.4;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -495.0 ? -495.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -3434.0)
                                    {
                                        if (wtemp > -495.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 495.0;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -770.0 ? -770.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -10400.0)
                                    {
                                        if (wtemp > -690.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 690.0;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -3434.0 ? -3434.0 : Gd);
                                    }
                                    else
                                    {
                                        Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -10400.0 ? -10400.0 : Gd);
                                    }
                                }
                            }
                        }
                        //*/
                    }
                });
            }
            if (flg) throw new Exception();
        }
        private void BPL(in double[][] E, double G, double KRK, double[][] cdr, Tuple<double, double>[][] e)//逆伝播(層毎確率降下)、Gは学習率、KRKは確率降下受け入れ確率、cdrは値域区間、eは収束条件(1<2区間、1=2誤差、1>2固定誤差)
        {
            if (cdr.Length != Nly[Nly.GetUpperBound(0)].Nn || e.Length != Nly[Nly.GetUpperBound(0)].Nn) throw new ArgumentOutOfRangeException("cdr/e", "BPL : Incorrect length.");
            if (KRK < 0.0 || KRK > 1.0) throw new ArgumentOutOfRangeException("KRK", "BPL : Invalid stochastic descendant probability.");
            bool flg = false, oflg2;
            int ocnt, ocnt2, ocnt3, ocnt4;
            double oET, E2sum = 0.0;
            Random rnd = new Random();
            object lor = new object();
            double[][] ELN;
            double[] oL;
            for (ocnt = 0; ocnt < Nly[Nly.GetUpperBound(0)].Nn; ocnt++)//ノード
            {
                for (ocnt2 = 0; ocnt2 < Dtn; ocnt2++)//データ
                {
                    Nly[Nly.GetUpperBound(0)].N(ocnt).B(E[ocnt][ocnt2], ocnt2);
                }
            }
            for (ocnt = 0; ocnt < E.Length; ocnt++)
            {
                for (ocnt2 = 0; ocnt2 < E[ocnt].Length; ocnt2++)
                {
                    E2sum += E[ocnt][ocnt2] * E[ocnt][ocnt2];
                }
            }
            //Console.WriteLine("E2sum = {0}", E2sum);//debug用
            for (int cnt = Nly.GetUpperBound(0); cnt > 0; cnt--)//層
            {
                int PLn = Nly.GetUpperBound(0) - cnt;//ポスト層数
                Parallel.For(0, Nly[cnt].Nn, po, (nind) =>//ノード
                {
                    double[,] MIP = new double[Dtn, Nly[cnt].N(nind).SR];//入力行列
                    double[] B = new double[Dtn];//ノードの微分値
                    double[] H = new double[Dtn];//ポストノードの偏微分値
                    double[] L = new double[Dtn];//ノードの偏微分値
                    double[] dW = new double[Nly[cnt].N(nind).SR];//Wの偏微分
                    double[] Fks = new double[Dtn];//更新値
                    double[] KS = new double[Nly[cnt].N(nind).SR];//入力係数セーブ
                    double[] NNV = new double[Dtn];//更新ノード値
                    double[][] ETN;//更新誤差
                    double[][][] PNV = new double[PLn][][];//更新ポストノード値
                    Nly[cnt].N(nind).K().CopyTo(KS, 0);
                    int cnt2, cnt3, cnt4, cnt5, itempp;
                    double dhb = 0.0;
                    double dtemp, wtemp, Gs, E2SN, Etemp;
                    double Gd = G;
                    bool flg2;
                    try
                    {
                        for (cnt2 = 0; cnt2 < Dtn; cnt2++)//データ
                        {
                            Fks[cnt2] = Nly[cnt].N(nind).KSCn(cnt2);//更新値
                            B[cnt2] = Nly[cnt].N(nind).DSCn(cnt2);
                            H[cnt2] = Nly[cnt].N(nind).B(cnt2);
                            L[cnt2] = H[cnt2] * B[cnt2];
                            for (cnt3 = 0; cnt3 < Nly[cnt].N(nind).SR; cnt3++)//入力
                            {
                                //if (Nly[cnt].N(nind).NRL(cnt3) != 0 && L[cnt2] != 0.0) lock (Nly[Nly[cnt].N(nind).NRL(cnt3)].N(Nly[cnt].N(nind).NRN(cnt3)).HL) Nly[Nly[cnt].N(nind).NRL(cnt3)].N(Nly[cnt].N(nind).NRN(cnt3)).Bs(L[cnt2] * Nly[cnt].N(nind).K(cnt3), cnt2);//下層偏微分更新
                                MIP[cnt2, cnt3] = Nly[Nly[cnt].N(nind).NRL(cnt3)].N(Nly[cnt].N(nind).NRN(cnt3)).F(cnt2);//入力行列
                                dW[cnt3] += MIP[cnt2, cnt3] * L[cnt2];
                            }
                            //Nly[cnt].N(nind).B(0.0, cnt2);//偏微分リセット
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        if (e.InnerException != null) Console.WriteLine(e);
                        Console.WriteLine("Can not perform back propagaion.");
                        flg = true;
                        return;
                    }
                    SGKC.Tensor tx = new SGKC.Tensor(MIP);//X
                    SGKC.Tensor ty = new SGKC.Tensor(Fks, false);//Y
                    try
                    {
                        //throw new Exception();
                        ty = SGKC.Tensor.TKzsDX(tx.TMPiDX(), ty);//X^(-1)Y
                        tx = new SGKC.Tensor(Nly[cnt].N(nind).K(), false);//W
                        ///*
                        Fks = ty.vector;
                        for (cnt2 = 0; cnt2 < Fks.Length; cnt2++)
                        {
                            if (Math.Abs(Fks[cnt2]) > 92500)
                            {
                                Fks[cnt2] = Math.CopySign(92500, Fks[cnt2]);
                            }
                        }
                        ty = new SGKC.Tensor(Fks, false);//*/
                        tx = SGKC.Tensor.TTzsDX(SGKC.Tensor.TBKzsDX(1 - G, tx), SGKC.Tensor.TBKzsDX(G, ty));//W'
                        Nly[cnt].N(nind).K(tx.vector);
                    }
                    catch (Exception)
                    {
                        /*
                        double sra, srr;
                        int srn;
                        for (cnt2 = 0; cnt2 < Nly[cnt].N(nind).SR; cnt2++)//入力
                        {
                            sra = 0.0;
                            srn = 0;
                            for (cnt3 = 0; cnt3 < Dtn; cnt3++)//データ
                            {
                                if (Nly[Nly[cnt].N(nind).NRL(cnt2)].N(Nly[cnt].N(nind).NRN(cnt2)).F(cnt3) != 0.0)
                                {
                                    sra += Math.Abs(Nly[Nly[cnt].N(nind).NRL(cnt2)].N(Nly[cnt].N(nind).NRN(cnt2)).F(cnt3));
                                    srn++;
                                }
                            }
                            lock (lo) srr = rnd.NextDouble();
                            if (sra == 0.0 || srn == 0)
                            {
                                Nly[cnt].N(nind).K(cnt2, srr <= 0.5 ? -1.0 : 1.0);
                            }
                            else
                            {
                                sra /= srn;
                                sra = 1 / sra;
                                Nly[cnt].N(nind).K(cnt2, srr <= 0.5 ? -sra : sra);
                            }
                        }
                        //*/
                        ///*
                        lock (lo) Gs = rnd.NextDouble();
                        if (Gs < 0.01)
                        {
                            Fks = new double[Nly[cnt].N(nind).SR];
                            for (cnt2 = 0; cnt2 < Fks.Length; cnt2++)
                            {
                                lock (lo) Fks[cnt2] = rnd.NextDouble() * 0.01 - 0.005;
                            }
                            Nly[cnt].N(nind).K(Fks);
                            return;
                        }//*/
                        ///*
                        for (cnt2 = 0; cnt2 < Dtn; cnt2++)
                        {
                            dtemp = Math.Abs(B[cnt2]);
                            if (dtemp > dhb) dhb = dtemp;
                            dtemp = Math.Abs(H[cnt2]);
                            if (dtemp > dhb) dhb = dtemp;
                        }
                        if (dhb == 0.0) return;
                        else if (dhb < 0.999)
                        {
                            if (dhb < 0.0019)
                            {
                                Gd *= 9.99;
                            }
                            else if (dhb < 0.00514)
                            {
                                Gd *= 3.4;
                            }
                            else if (dhb < 0.015)
                            {
                                Gd *= 1.04;
                            }
                            else if (dhb < 0.077)
                            {
                                Gd *= 0.495;
                            }
                            else if (dhb < 0.34)
                            {
                                Gd *= 0.34;
                            }
                            else
                            {
                                Gd *= 0.19;
                            }
                        }
                        else if (dhb < 514.0)
                        {
                            if (dhb < 4.95) { }
                            else if (dhb < 17.0)
                            {
                                Gd *= 0.17;
                            }
                            else if (dhb < 69.0)
                            {
                                Gd *= 0.077;
                            }
                            else if (dhb < 99.9)
                            {
                                Gd *= 0.069;
                            }
                            else if (dhb < 170.0)
                            {
                                Gd *= 0.0514;
                            }
                            else if (dhb < 343.4)
                            {
                                Gd *= 0.0495;
                            }
                            else
                            {
                                Gd *= 0.03434;
                            }
                        }
                        else
                        {
                            if (dhb < 999)
                            {
                                Gd *= 0.017;
                            }
                            else if (dhb < 1500.0)
                            {
                                Gd *= 0.00495;
                            }
                            else if (dhb < 4950.0)
                            {
                                Gd *= 0.0019;
                            }
                            else if (dhb < 10400.0)
                            {
                                Gd *= 0.000999;
                            }
                            else if (dhb < 12800.0)
                            {
                                Gd *= 0.000925;
                            }
                            else
                            {
                                Gd = 1.0 / Math.Sqrt(dhb);
                            }
                        }
                        //*/
                        ///*
                        Gs = 0.01 * Gd;
                        for (cnt2 = 0; cnt2 < Nly[cnt].N(nind).SR; cnt2++)//入力
                        {
                            wtemp = dW[cnt2] * Gs;
                            if (wtemp == 0.0) continue;
                            if (wtemp > 0.0)
                            {
                                if (Nly[cnt].N(nind).K(cnt2) >= 0.0)
                                {
                                    if (Nly[cnt].N(nind).K(cnt2) <= 0.69)
                                    {
                                        if (Nly[cnt].N(nind).K(cnt2) <= 0.000514)
                                        {
                                            if (wtemp < 0.00104) Nly[cnt].N(nind).K(cnt2, Nly[cnt].N(nind).K(cnt2) - wtemp);
                                            else Nly[cnt].N(nind).K(cnt2, Nly[cnt].N(nind).K(cnt2) - 0.00104);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 0.003434)
                                        {
                                            if (wtemp < 0.000999) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.000999;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 0.000514 ? 0.000514 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 0.0077)
                                        {
                                            if (wtemp < 0.003434) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.003434;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 0.003434 ? 0.003434 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 0.0495)
                                        {
                                            if (wtemp < 0.017) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.017;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 0.0077 ? 0.0077 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 0.19)
                                        {
                                            if (wtemp < 0.077) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.077;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 0.0495 ? 0.0495 : Gd);
                                        }
                                        else
                                        {
                                            if (wtemp < 0.3434) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.3434;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 0.19 ? 0.19 : Gd);
                                        }
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 495.0)
                                    {
                                        if (Nly[cnt].N(nind).K(cnt2) <= 1.7)
                                        {
                                            if (wtemp < 0.514) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.514;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 0.69 ? 0.69 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 5.14)
                                        {
                                            if (wtemp < 1.9) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 1.9;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 1.7 ? 1.7 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 34.34)
                                        {
                                            if (wtemp < 9.99) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 9.99;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 5.14 ? 5.14 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 104.0)
                                        {
                                            if (wtemp < 4.95) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 4.95;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 34.34 ? 34.34 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 170.0)
                                        {
                                            if (wtemp < 69.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 69.0;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 104.0 ? 104.0 : Gd);
                                        }
                                        else
                                        {
                                            if (wtemp < 150.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 150.0;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 170.0 ? 170.0 : Gd);
                                        }
                                    }
                                    else
                                    {
                                        if (Nly[cnt].N(nind).K(cnt2) <= 770.0)
                                        {
                                            if (wtemp < 343.4) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 343.4;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 495.0 ? 495.0 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 3434.0)
                                        {
                                            if (wtemp < 495.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 495.0;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 770.0 ? 770.0 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) <= 10400.0)
                                        {
                                            if (wtemp < 690.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 690.0;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 3434.0 ? 3434.0 : Gd);
                                        }
                                        else
                                        {
                                            Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= 10400.0 ? 10400.0 : Gd);
                                        }
                                    }
                                }
                                else
                                {
                                    if (Nly[cnt].N(nind).K(cnt2) >= -0.69)
                                    {
                                        if (Nly[cnt].N(nind).K(cnt2) >= -0.000514)
                                        {
                                            if (wtemp < 0.00019) Nly[cnt].N(nind).K(cnt2, Nly[cnt].N(nind).K(cnt2) - wtemp);
                                            else Nly[cnt].N(nind).K(cnt2, Nly[cnt].N(nind).K(cnt2) - 0.00019);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -0.003434)
                                        {
                                            if (wtemp < 0.0015) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.0015;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -0.0077 ? -0.0077 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -0.0077)
                                        {
                                            if (wtemp < 0.017) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.017;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -0.0495 ? -0.0495 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -0.0495)
                                        {
                                            if (wtemp < 0.069) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.069;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -0.19 ? -0.19 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -0.19)
                                        {
                                            if (wtemp < 0.19) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.19;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -0.69 ? -0.69 : Gd);
                                        }
                                        else
                                        {
                                            if (wtemp < 0.34) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.34;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -1.7 ? -1.7 : Gd);
                                        }
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -495.0)
                                    {
                                        if (Nly[cnt].N(nind).K(cnt2) >= -1.7)
                                        {
                                            if (wtemp < 0.999) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 0.999;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -5.14 ? -5.14 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -5.14)
                                        {
                                            if (wtemp < 7.7) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 7.7;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -34.34 ? -34.34 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -34.34)
                                        {
                                            if (wtemp < 15.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 15.0;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -104.0 ? -104.0 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -104.0)
                                        {
                                            if (wtemp < 19.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 19.0;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -170.0 ? -170.0 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -170.0)
                                        {
                                            if (wtemp < 34.34) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 34.34;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -495.0 ? -495.0 : Gd);
                                        }
                                        else
                                        {
                                            if (wtemp < 51.4) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 51.4;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -770.0 ? -770.0 : Gd);
                                        }
                                    }
                                    else
                                    {
                                        if (Nly[cnt].N(nind).K(cnt2) >= -770.0)
                                        {
                                            if (wtemp < 99.9) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 99.9;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -3434.0 ? -3434.0 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -3434.0)
                                        {
                                            if (wtemp < 170.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 170.0;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -10400.0 ? -10400.0 : Gd);
                                        }
                                        else if (Nly[cnt].N(nind).K(cnt2) >= -10400.0)
                                        {
                                            if (wtemp < 343.4) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                            else Gd = Nly[cnt].N(nind).K(cnt2) - 343.4;
                                            Nly[cnt].N(nind).K(cnt2, Gd <= -51400.0 ? -51400.0 : Gd);
                                        }
                                        else
                                        {
                                            Nly[cnt].N(nind).K(cnt2, wtemp < 495.0 ? (Nly[cnt].N(nind).K(cnt2) - wtemp) : (Nly[cnt].N(nind).K(cnt2) - 495.0));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (Nly[cnt].N(nind).K(cnt2) >= 0.0)
                                {
                                    if (Nly[cnt].N(nind).K(cnt2) <= 0.000514)
                                    {
                                        if (wtemp > -0.00019) Nly[cnt].N(nind).K(cnt2, Nly[cnt].N(nind).K(cnt2) - wtemp);
                                        else Nly[cnt].N(nind).K(cnt2, Nly[cnt].N(nind).K(cnt2) + 0.00019);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 0.003434)
                                    {
                                        if (wtemp > -0.0015) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.0015;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 0.0077 ? 0.0077 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 0.0077)
                                    {
                                        if (wtemp > -0.017) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.017;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 0.0495 ? 0.0495 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 0.0495)
                                    {
                                        if (wtemp > -0.069) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.069;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 0.19 ? 0.19 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 0.19)
                                    {
                                        if (wtemp > -0.19) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.19;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 0.69 ? 0.69 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 0.69)
                                    {
                                        if (wtemp > -0.34) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.34;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 1.7 ? 1.7 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 1.7)
                                    {
                                        if (wtemp > -0.999) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.999;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 5.14 ? 5.14 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 5.14)
                                    {
                                        if (wtemp > -7.7) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 7.7;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 34.34 ? 34.34 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 34.34)
                                    {
                                        if (wtemp > -15.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 15.0;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 104.0 ? 104.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 104.0)
                                    {
                                        if (wtemp > -19.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 19.0;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 170.0 ? 170.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 170.0)
                                    {
                                        if (wtemp > -34.34) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 34.34;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 495.0 ? 495.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 495.0)
                                    {
                                        if (wtemp > -51.4) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 51.4;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 770.0 ? 770.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 770.0)
                                    {
                                        if (wtemp > -99.9) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 99.9;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 3434.0 ? 3434.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 3434.0)
                                    {
                                        if (wtemp > -170.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 170.0;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 10400.0 ? 10400.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) <= 10400.0)
                                    {
                                        if (wtemp > -343.4) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 343.4;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= 51400.0 ? 51400.0 : Gd);
                                    }
                                    else
                                    {
                                        Nly[cnt].N(nind).K(cnt2, wtemp > -495.0 ? (Nly[cnt].N(nind).K(cnt2) - wtemp) : (Nly[cnt].N(nind).K(cnt2) + 495.0));
                                    }
                                }
                                else
                                {
                                    if (Nly[cnt].N(nind).K(cnt2) >= -0.000514)
                                    {
                                        if (wtemp > -0.00104) Nly[cnt].N(nind).K(cnt2, Nly[cnt].N(nind).K(cnt2) - wtemp);
                                        else Nly[cnt].N(nind).K(cnt2, Nly[cnt].N(nind).K(cnt2) + 0.00104);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -0.003434)
                                    {
                                        if (wtemp > -0.000999) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.000999;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -0.000514 ? -0.000514 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -0.0077)
                                    {
                                        if (wtemp > -0.003434) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.003434;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -0.003434 ? -0.003434 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -0.0495)
                                    {
                                        if (wtemp > -0.017) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.017;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -0.0077 ? -0.0077 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -0.19)
                                    {
                                        if (wtemp > -0.077) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.077;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -0.0495 ? -0.0495 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -0.69)
                                    {
                                        if (wtemp > -0.3434) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.3434;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -0.19 ? -0.19 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -1.7)
                                    {
                                        if (wtemp > -0.514) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 0.514;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -0.69 ? -0.69 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -5.14)
                                    {
                                        if (wtemp > -1.9) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 1.9;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -1.7 ? -1.7 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -34.34)
                                    {
                                        if (wtemp > -9.99) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 9.99;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -5.14 ? -5.14 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -104.0)
                                    {
                                        if (wtemp > -4.95) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 4.95;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -34.34 ? -34.34 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -170.0)
                                    {
                                        if (wtemp > -69.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 69.0;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -104.0 ? -104.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -495.0)
                                    {
                                        if (wtemp > -150.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 150.0;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -170.0 ? -170.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -770.0)
                                    {
                                        if (wtemp > -343.4) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 343.4;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -495.0 ? -495.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -3434.0)
                                    {
                                        if (wtemp > -495.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 495.0;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -770.0 ? -770.0 : Gd);
                                    }
                                    else if (Nly[cnt].N(nind).K(cnt2) >= -10400.0)
                                    {
                                        if (wtemp > -690.0) Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        else Gd = Nly[cnt].N(nind).K(cnt2) + 690.0;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -3434.0 ? -3434.0 : Gd);
                                    }
                                    else
                                    {
                                        Gd = Nly[cnt].N(nind).K(cnt2) - wtemp;
                                        Nly[cnt].N(nind).K(cnt2, Gd >= -10400.0 ? -10400.0 : Gd);
                                    }
                                }
                            }
                        }
                        //*/
                    }
                    for (cnt2 = 0; cnt2 < Nly[cnt].N(nind).SR; cnt2++)//ノード値更新
                    {
                        for (cnt3 = 0; cnt3 < Dtn; cnt3++)
                        {
                            NNV[cnt3] += Nly[Nly[cnt].N(nind).NRL(cnt2)].N(Nly[cnt].N(nind).NRN(cnt2)).F(cnt3);
                        }
                    }
                    for (cnt2 = 0; cnt2 < PLn; cnt2++)//ポストノード値計算
                    {
                        itempp = cnt + cnt2 + 1;
                        PNV[cnt2] = new double[Nly[itempp].Nn][];
                        for (cnt3 = 0; cnt3 < Nly[itempp].Nn; cnt3++)
                        {
                            PNV[cnt2][cnt3] = new double[Dtn];
                            for (cnt4 = 0; cnt4 < Nly[itempp].N(cnt3).SR; cnt4++)
                            {
                                if (Nly[itempp].N(cnt3).NRL(cnt4) == cnt && Nly[itempp].N(cnt3).NRN(cnt4) == nind)
                                {
                                    for (cnt5 = 0; cnt5 < Dtn; cnt5++)
                                    {
                                        PNV[cnt2][cnt3][cnt5] += Nly[itempp].N(cnt3).K(cnt4) * NNV[cnt5];
                                    }
                                }
                                else
                                {
                                    for (cnt5 = 0; cnt5 < Dtn; cnt5++)
                                    {
                                        PNV[cnt2][cnt3][cnt5] += Nly[itempp].N(cnt3).K(cnt4) * Nly[Nly[itempp].N(cnt3).NRL(cnt4)].N(Nly[itempp].N(cnt3).NRN(cnt4)).F(cnt5);
                                    }
                                }
                            }
                        }
                    }
                    if (cnt == Nly.GetUpperBound(0))
                    {
                        PNV = new double[1][][];
                        PNV[0] = new double[Nly[Nly.GetUpperBound(0)].Nn][];
                        for (cnt2 = 0; cnt2 < PNV[0].Length; cnt2++)//出力ノード
                        {
                            PNV[0][cnt2] = new double[Dtn];
                            if (nind == cnt2)
                            {
                                PNV[0][cnt2] = NNV;
                            }
                            else
                            {
                                for (cnt3 = 0; cnt3 < Dtn; cnt3++)//データ
                                {
                                    PNV[0][cnt2][cnt3] = Nly[Nly.GetUpperBound(0)].N(cnt2).F(cnt3);
                                }
                            }
                        }
                    }
                    if (PNV[PNV.GetUpperBound(0)].Length != Nly[Nly.GetUpperBound(0)].Nn)
                    {
                        throw new ArgumentOutOfRangeException("PNV/Nly", "BPL : Can not verify length of output layer.");
                    }
                    ETN = new double[Nly[Nly.GetUpperBound(0)].Nn][];
                    for (cnt4 = 0; cnt4 < PNV[PNV.GetUpperBound(0)].Length; cnt4++)//出力ノード
                    {
                        if (cdr[cnt4].Length != e[cnt4].Length) throw new ArgumentOutOfRangeException("cdr/e", "BPL : Incorrect length.");
                        ETN[cnt4] = new double[Dtn];
                        for (cnt2 = 0; cnt2 < Dtn; cnt2++)//データ
                        {
                            Etemp = PNV[PNV.GetUpperBound(0)][cnt4][cnt2] - TD[cnt4][cnt2];
                            flg2 = false;
                            for (cnt3 = 0; cnt3 < cdr[cnt4].Length; cnt3++)//区間
                            {
                                if (TD[cnt4][cnt2] < cdr[cnt4][cnt3])
                                {
                                    flg2 = true;
                                    //Console.WriteLine(Etemp);//debug用
                                    if (e[cnt4][cnt3].Item1 < e[cnt4][cnt3].Item2)
                                    {
                                        if (PNV[PNV.GetUpperBound(0)][cnt4][cnt2] >= e[cnt4][cnt3].Item1 && PNV[PNV.GetUpperBound(0)][cnt4][cnt2] <= e[cnt4][cnt3].Item2) ETN[cnt4][cnt2] = 0.0;
                                        else
                                        {
                                            if (flg) flg = false;
                                            ETN[cnt4][cnt2] = Etemp;
                                        }
                                    }
                                    else if (e[cnt4][cnt3].Item1 > e[cnt4][cnt3].Item2)
                                    {
                                        if (Etemp < e[cnt4][cnt3].Item1 && Etemp > e[cnt4][cnt3].Item2) ETN[cnt4][cnt2] = 0.0;
                                        else
                                        {
                                            if (flg) flg = false;
                                            ETN[cnt4][cnt2] = Etemp;
                                        }
                                    }
                                    else
                                    {
                                        if (Math.Abs(Etemp) < e[cnt4][cnt3].Item1 * TD[cnt4][cnt2]) ETN[cnt4][cnt2] = 0.0;
                                        else
                                        {
                                            if (flg) flg = false;
                                            ETN[cnt4][cnt2] = Etemp;
                                        }
                                    }
                                }
                            }
                            if (!flg2) throw new ArgumentOutOfRangeException("TD/e", "BPL : Data is out of region.");
                        }
                    }
                    E2SN = 0.0;
                    for (cnt2 = 0; cnt2 < ETN.Length; cnt2++)//出力ノード
                    {
                        for (cnt3 = 0; cnt3 < ETN[cnt2].Length; cnt3++)//データ
                        {
                            E2SN += ETN[cnt2][cnt3] * ETN[cnt2][cnt3];
                        }
                    }
                    Console.WriteLine("E2sum = {0}, E2SN = {1}", E2sum, E2SN);//debug用
                    if (E2SN > E2sum)
                    {
                        if (KRK == 0.0)
                        {
                            for (cnt2 = 0; cnt2 < Nly[cnt].N(nind).SR; cnt2++)
                            {
                                Nly[cnt].N(nind).K(cnt2, KS[cnt2]);
                            }
                        }
                        else
                        {
                            lock (lor) Etemp = rnd.NextDouble();
                            if (Etemp >= KRK)
                            {
                                for (cnt2 = 0; cnt2 < Nly[cnt].N(nind).SR; cnt2++)
                                {
                                    Nly[cnt].N(nind).K(cnt2, KS[cnt2]);
                                }
                            }
                        }
                    }
                });
                FP();
                ELN = new double[Nly[Nly.GetUpperBound(0)].Nn][];
                for (ocnt = 0; ocnt < Nly[Nly.GetUpperBound(0)].Nn; ocnt++)//出力ノード
                {
                    if (cdr[ocnt].Length != e[ocnt].Length) throw new ArgumentOutOfRangeException("cdr/e", "BPL : Incorrect length.");
                    ELN[ocnt] = new double[Dtn];
                    for (ocnt2 = 0; ocnt2 < Dtn; ocnt2++)//データ
                    {
                        oET = Nly[Nly.GetUpperBound(0)].N(ocnt).F(ocnt2) - TD[ocnt][ocnt2];
                        oflg2 = false;
                        for (ocnt3 = 0; ocnt3 < cdr[ocnt].Length; ocnt3++)//区間
                        {
                            if (TD[ocnt][ocnt2] < cdr[ocnt][ocnt3])
                            {
                                oflg2 = true;
                                //Console.WriteLine(Etemp);//debug用
                                if (e[ocnt][ocnt3].Item1 < e[ocnt][ocnt3].Item2)
                                {
                                    if (Nly[Nly.GetUpperBound(0)].N(ocnt).F(ocnt2) >= e[ocnt][ocnt3].Item1 && Nly[Nly.GetUpperBound(0)].N(ocnt).F(ocnt2) <= e[ocnt][ocnt3].Item2) ELN[ocnt][ocnt2] = 0.0;
                                    else
                                    {
                                        if (flg) flg = false;
                                        ELN[ocnt][ocnt2] = oET;
                                    }
                                }
                                else if (e[ocnt][ocnt3].Item1 > e[ocnt][ocnt3].Item2)
                                {
                                    if (oET < e[ocnt][ocnt3].Item1 && oET > e[ocnt][ocnt3].Item2) ELN[ocnt][ocnt2] = 0.0;
                                    else
                                    {
                                        if (flg) flg = false;
                                        ELN[ocnt][ocnt2] = oET;
                                    }
                                }
                                else
                                {
                                    if (Math.Abs(oET) < e[ocnt][ocnt3].Item1 * TD[ocnt][ocnt2]) ELN[ocnt][ocnt2] = 0.0;
                                    else
                                    {
                                        if (flg) flg = false;
                                        ELN[ocnt][ocnt2] = oET;
                                    }
                                }
                            }
                        }
                        if (!oflg2) throw new ArgumentOutOfRangeException("TD/e", "BPL : Data is out of region.");
                    }
                }
                E2sum = 0.0;
                for (ocnt = 0; ocnt < ELN.Length; ocnt++)
                {
                    for (ocnt2 = 0; ocnt2 < ELN[ocnt].Length; ocnt2++)
                    {
                        E2sum += ELN[ocnt][ocnt2] * ELN[ocnt][ocnt2];
                    }
                }
                for (ocnt = 0; ocnt < Nly[cnt].Nn; ocnt++)//現在のポストノード偏微分を消す
                {
                    for (ocnt2 = 0; ocnt2 < Dtn; ocnt2++)
                    {
                        Nly[cnt].N(ocnt).B(0.0, ocnt2);
                    }
                }
                for (ocnt = 0; ocnt < Nly[Nly.GetUpperBound(0)].Nn; ocnt++)//入力層ノード
                {
                    for (ocnt2 = 0; ocnt2 < Dtn; ocnt2++)//データ
                    {
                        Nly[Nly.GetUpperBound(0)].N(ocnt).B(ELN[ocnt][ocnt2], ocnt2);
                    }
                }
                for (ocnt = Nly.GetUpperBound(0); ocnt >= cnt; ocnt--)
                {
                    for (ocnt2 = 0; ocnt2 < Nly[ocnt].Nn; ocnt2++)
                    {
                        oL = new double[Dtn];
                        for (ocnt3 = 0; ocnt3 < Dtn; ocnt3++)
                        {
                            oL[ocnt3] = Nly[ocnt].N(ocnt2).DSCn(ocnt3) * Nly[ocnt].N(ocnt2).B(ocnt3);
                            for (ocnt4 = 0; ocnt4 < Nly[ocnt].N(ocnt2).SR; ocnt4++)//入力
                            {
                                if (Nly[ocnt].N(ocnt2).NRL(ocnt4) != 0 && oL[ocnt3] != 0.0) Nly[Nly[ocnt].N(ocnt2).NRL(ocnt4)].N(Nly[ocnt].N(ocnt2).NRN(ocnt4)).Bs(oL[ocnt3] * Nly[ocnt].N(ocnt2).K(ocnt4), ocnt3);//下層偏微分更新
                            }
                            Nly[ocnt].N(ocnt2).B(0.0, ocnt3);
                        }
                    }
                }
            }
            if (flg) throw new Exception();
        }
        private int XX(int NK, in double[][] cdr, in Tuple<double, double>[][] e, in double G, in double[][] desin, in double[][] gdo, ref int cnt)//学習、NKは学習回数、cdrは値域区間、eは収束条件(1<2区間、1=2誤差、1>2固定誤差)、Gは学習率、desinは入力記述子、gdoは真データ
        {
            swj = new Stopwatch();
            swl = new Stopwatch();
            double[][] E;
            int cnt2 = 0;
            swj.Start();
            swl.Start();
            TimeSpan tsj, tsl;
            for (cnt = 0; cnt < NK; cnt++)
            {
                if (cnt % 3 == 0)
                {
                    tsl = swl.Elapsed;
                    tsj = swj.Elapsed;
                    if (tsj.TotalSeconds >= CommonParam.tthnj.TotalSeconds) return -1;
                    if (tsl.TotalSeconds >= CommonParam.tthnl.TotalSeconds)
                    {
                        if (cnt2 >= 34) return -1;
                        THniru(desin, gdo);
                        cnt = 0;
                        cnt2++;
                    }
                    swl.Restart();
                }
                FP();
                C = SSye(in cdr, in e, out E);
                if (C) break;
                try
                {
                    BP(in E, G);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    if (ex.InnerException != null) Console.WriteLine(ex.InnerException);
                    return -1;
                }
            }
            swj.Stop();
            swl.Stop();
            return cnt;
        }
        private int XX(int NK, in double[][] cdr, in Tuple<double, double>[][] e, in double G, in double KRK, in bool L, in double[][] desin, in double[][] gdo, ref int cnt)//学習、NKは学習回数、cdrは値域区間、eは収束条件(1<2区間、1=2誤差、1>2固定誤差)、Gは学習率、KRKは確率降下受け入れ確率、L trueは層毎に更新,falseはノード毎に更新、desinは入力記述子、gdoは真データ、cntは学習回数
        {
            swj = new Stopwatch();
            swl = new Stopwatch();
            double[][] E;
            int cnt2 = 0;
            swj.Start();
            swl.Start();
            TimeSpan tsj, tsl;
            for (cnt = 0; cnt < NK; cnt++)
            {
                if (cnt % 3 == 0)
                {
                    tsl = swl.Elapsed;
                    tsj = swj.Elapsed;
                    if (tsj.TotalSeconds >= CommonParam.tthnj.TotalSeconds) return -1;
                    ///*
                    if (tsl.TotalSeconds >= CommonParam.tthnl.TotalSeconds)
                    {
                        if (cnt2 >= 34) return -1;
                        THniru(desin, gdo);
                        cnt = 0;
                        cnt2++;
                    }//*/
                    swl.Restart();
                }
                FP();
                C = SSye(in cdr, in e, out E);
                if (C) break;
                try
                {
                    if (L) BPL(in E, G, KRK, cdr, e);
                    else throw new NotImplementedException("Nodewise updating not implemented yet.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    if (ex.InnerException != null) Console.WriteLine(ex.InnerException);
                    return -1;
                }
            }
            swj.Stop();
            swl.Stop();
            return cnt;
        }
        private double[][] YS(int Dy, double[][] desy)//予測、Dyはデータ数、desyは記述子
        {
            int IND = Nly[0].Nn - 1;
            if (desy.Length != IND) throw new ArgumentOutOfRangeException("desy", "YS : Incorrect descriptor length.");
            int cnt, cnt2;
            for (cnt = 0; cnt < IND; cnt++)
            {
                if (desy[cnt].Length != Dy) throw new ArgumentOutOfRangeException("desy", "YS : Incorrect data length.");
            }
            double[][] dres = new double[Nly[Nly.GetUpperBound(0)].Nn][];
            for (cnt = 0; cnt < Nly[Nly.GetUpperBound(0)].Nn; cnt++)
            {
                dres[cnt] = new double[Dy];
            }
            for (cnt = 0; cnt < Nly.Length; cnt++)//層
            {
                for (cnt2 = 0; cnt2 < ((cnt == 0) ? IND : Nly[cnt].Nn); cnt2++)//ノード
                {
                    Nly[cnt].N(cnt2).YSI(Dy);
                }
            }
            Parallel.For(0, Dy, po, (dind) =>//データ
            {
                int pcnt, pcnt2, pcnt3;
                double dtemp;
                for (pcnt = 0; pcnt < IND; pcnt++)//ノード
                {
                    Nly[0].N(pcnt).TGsY(desy[pcnt][dind], dind);
                }
                for (pcnt = 1; pcnt < Nly.Length; pcnt++)//層
                {
                    for (pcnt2 = 0; pcnt2 < Nly[pcnt].Nn; pcnt2++)//ノード
                    {
                        dtemp = 0.0;
                        for (pcnt3 = 0; pcnt3 < Nly[pcnt].N(pcnt2).SR; pcnt3++)//入力
                        {
                            dtemp += Nly[Nly[pcnt].N(pcnt2).NRL(pcnt3)].N(Nly[pcnt].N(pcnt2).NRN(pcnt3)).FY(dind) * Nly[pcnt].N(pcnt2).K(pcnt3);
                        }
                        Nly[pcnt].N(pcnt2).TGsY(dtemp, dind);
                    }
                }
                for (pcnt = 0; pcnt < Nly[Nly.GetUpperBound(0)].Nn; pcnt++)//ノード
                {
                    dres[pcnt][dind] = Nly[Nly.GetUpperBound(0)].N(pcnt).FY(dind);
                }
            });
            return dres;
        }
        private void NVT(int Dt, in double[][] dest, in double[][] gdt, in double[][] cdr, in Tuple<double, double>[][] e, out double[][] S)//検証、Dtは検証データ数、destは検証記述子、gdtは検証真データ、cdrは値域区間、eは収束条件(1<2区間、1=2誤差、1>2固定誤差)
        {
            if (dest.Length != Nly[0].Nn - 1) throw new ArgumentNullException("dest", "NVT : Incorrect node length.");
            if (gdt.Length != Nly[Nly.GetUpperBound(0)].Nn) throw new ArgumentNullException("gdt", "NVT : Incorrect node length.");
            int cnt;
            for (cnt = 0; cnt < dest.Length; cnt++)
            {
                if (dest[cnt].Length != Dt) throw new ArgumentNullException("dest", "NVT : Incorrect genuine data length.");
            }
            for (cnt = 0; cnt < gdt.Length; cnt++)
            {
                if (gdt[cnt].Length != Dt) throw new ArgumentNullException("gdt", "NVT : Incorrect genuine data length.");
            }
            double[][] dres = YS(Dt, dest);
            double EAbs;//誤差絶対値和
            double E2;//誤差二乗和
            double gds;//真値和
            double ysw;//予測値和
            double gd2;//真値二乗和
            double ys2;//予測値二乗和
            double YSS;//予測値/真値積和(ドット積)
            double gda;//真値平均
            double ysh;//予測値平均
            double gdv;//真値分散和
            double ysb;//予測値分散和
            double YSkb;//予測値/真値共分散和
            S = new double[Nly[Nly.GetUpperBound(0)].Nn][];//誤差絶対値和、誤差二乗和、予測値/真値cos、R28、R27、R21、r、R23
            bool flg = true;
            bool flg2;
            double dtemp, dtemp2;
            int cnt2, cnt3;
            for (cnt = 0; cnt < Nly[Nly.GetUpperBound(0)].Nn; cnt++)//ノード
            {
                if (cdr[cnt].Length != e[cnt].Length) throw new ArgumentOutOfRangeException("cdr/e", "NVT : Incorrect length.");
                EAbs = 0.0;
                E2 = 0.0;
                gds = 0.0;
                ysw = 0.0;
                gd2 = 0.0;
                ys2 = 0.0;
                YSS = 0.0;
                S[cnt] = new double[8];//誤差絶対値和、誤差二乗和、予測値/真値cos、R28、R27、R21、r、R23
                for (cnt2 = 0; cnt2 < Dt; cnt2++)//データ
                {
                    dtemp = dres[cnt][cnt2] - gdt[cnt][cnt2];
                    EAbs += Math.Abs(dtemp);
                    E2 += dtemp * dtemp;
                    gds += gdt[cnt][cnt2];
                    ysw += dres[cnt][cnt2];
                    gd2 += gdt[cnt][cnt2] * gdt[cnt][cnt2];
                    ys2 += dres[cnt][cnt2] * dres[cnt][cnt2];
                    YSS += dres[cnt][cnt2] * gdt[cnt][cnt2];
                    flg2 = false;
                    for (cnt3 = 0; cnt3 < cdr[cnt].Length; cnt3++)//区間
                    {
                        if (gdt[cnt][cnt2] < cdr[cnt][cnt3])
                        {
                            flg2 = true;
                            if (e[cnt][cnt3].Item1 < e[cnt][cnt3].Item2)
                            {
                                if (dres[cnt][cnt2] > e[cnt][cnt3].Item1 && dres[cnt][cnt2] < e[cnt][cnt3].Item2) break;
                                else if (flg) flg = false;
                            }
                            else if (e[cnt][cnt3].Item1 > e[cnt][cnt3].Item2)
                            {
                                if (dtemp < e[cnt][cnt3].Item1 && dtemp > e[cnt][cnt3].Item2) break;
                                else if (flg) flg = false;
                            }
                            else
                            {
                                if (Math.Abs(dtemp) < e[cnt][cnt3].Item1 * TD[cnt][cnt2]) break;
                                else if (flg) flg = false;
                            }
                        }
                    }
                    if (!flg2) throw new ArgumentOutOfRangeException("TD/e", "NVT : Data is out of region.");
                }
                S[cnt][0] = EAbs;//誤差絶対値和
                S[cnt][1] = E2;//誤差二乗和
                gda = gds / Dt;
                ysh = ysw / Dt;
                S[cnt][2] = YSS / Math.Sqrt(gd2 * ys2); //予測値/真値cos (ドット積/ルート(真値二乗和*予測値二乗和))
                S[cnt][3] = ys2 / gd2;//R28 (予測値二乗和/真値二乗和)
                S[cnt][4] = 1 - E2 / gd2;//R27 (1-誤差二乗和/真値二乗和)
                gdv = 0.0;
                ysb = 0.0;
                YSkb = 0.0;
                for (cnt2 = 0; cnt2 < Dt; cnt2++)//データ
                {
                    dtemp = gdt[cnt][cnt2] - gda;
                    gdv += dtemp * dtemp;
                    dtemp2 = dres[cnt][cnt2] - ysh;
                    ysb += dtemp2 * dtemp2;
                    YSkb += dtemp * dtemp2;
                }
                S[cnt][5] = 1 - E2 / gdv;//R21 (1-誤差分散/真値分散)
                S[cnt][6] = YSkb / Math.Sqrt(gdv * ysb);//r(標本相関係数/ピアソンの積率相関係数) (予測値/真値共分散/(真値分散*予測値分散))
                S[cnt][7] = ysb / gdv;//R23 (予測値分散/真値分散) 
            }
            V = flg;
        }
        static private void THNRi(in THNetwork thn, in Tuple<int, int>[] Tcn, in double[][] Fwp, in int[][] Nfp, in double[][] cdr, in Tuple<double, double>[][] e, in double[][] gdo, in int NK, in double G, in int[] Di, in double[] Dc, in double[] Dn, in FileInfo fi, in string JN, in string ResFP, in int[] Resi, in string DesFP)//東方ネットワーク記録ファイル作成(未収束)(シングルジョブ)、thnは東方ネットワーク、Tcnは(0,n]層の連結数範囲、Fwpは超層連結率、Nfpは関数確率(一万分の)、cdrは値域区間、eは収束条件(1<2区間、1=2誤差、1>2固定誤差)、gdo[Nn-1][Dtn]は真データ、NKは学習回数、Gは学習率、Diは記述子インデックス、Dcは記述子中心化係数、Dnは記述子正規化スケール、fiは記録ファイル、JNはジョブ名、ResFPは真データファイルパス、Resiは真データインデックス、DesFPは記述子ファイルパス
        {
            StringBuilder sb = new StringBuilder();
            int cnt, cnt2, cnt3, itemp;
            sb.Append("任务名 : ");
            sb.AppendLine(JN);
            sb.AppendLine();
            sb.Append("記述子ファイルパス : ");
            sb.AppendLine(DesFP);
            sb.Append(string.Format("Descriptor index ({0}): ", Di.Length));
            for (cnt = 0; cnt < Di.GetUpperBound(0); cnt++)
            {
                sb.Append(string.Format("{0}, ", Di[cnt]));
            }
            sb.Append(Di[Di.GetUpperBound(0)]);
            sb.AppendLine();
            sb.Append(string.Format("中心化常数 ({0})： ", Dc.Length));
            for (cnt = 0; cnt < Dc.GetUpperBound(0); cnt++)
            {
                sb.Append(string.Format("{0:G15}, ", Dc[cnt]));
            }
            sb.Append(Dc[Dc.GetUpperBound(0)].ToString("G15"));
            sb.AppendLine();
            sb.Append(string.Format("正規化係数 ({0})： ", Dn.Length));
            for (cnt = 0; cnt < Dn.GetUpperBound(0); cnt++)
            {
                sb.Append(string.Format("{0:G15}, ", Dn[cnt]));
            }
            sb.Append(Dn[Dn.GetUpperBound(0)].ToString("G15"));
            sb.AppendLine();
            sb.AppendLine();
            sb.Append("Result File Path : ");
            sb.AppendLine(ResFP);
            sb.Append(string.Format("真数序数 ({0}): ", Resi.Length));
            for (cnt = 0; cnt < Resi.GetUpperBound(0); cnt++)
            {
                sb.Append(string.Format("{0}, ", Resi[cnt]));
            }
            sb.AppendLine(Resi[Resi.GetUpperBound(0)].ToString());
            sb.AppendLine();
            sb.AppendLine("Network Generation Parameters =>");
            sb.Append(string.Format("层连接范围 ({0}): ", Tcn.Length));
            for (cnt = 0; cnt < Tcn.Length; cnt++)
            {
                sb.Append("(");
                sb.Append(Tcn[cnt].Item1);
                sb.Append(", ");
                sb.Append(Tcn[cnt].Item2);
                sb.Append("), ");
            }
            sb.AppendLine();
            sb.Append(string.Format("超層連結率 ({0}): ", Fwp.Length));
            for (cnt = 0; cnt < Fwp.Length; cnt++)
            {
                sb.Append("(");
                for (cnt2 = 0; cnt2 < Fwp[cnt].GetUpperBound(0); cnt2++)
                {
                    sb.Append(Fwp[cnt][cnt2]);
                    sb.Append(", ");
                }
                sb.Append(Fwp[cnt][Fwp[cnt].GetUpperBound(0)]);
                sb.Append("), ");
            }
            sb.AppendLine();
            sb.Append(string.Format("Node Function Probability ({0}): ", Nfp.Length));
            for (cnt = 0; cnt < Nfp.Length; cnt++)
            {
                sb.Append("(");
                if (Nfp[cnt] == null) sb.Append("R");
                else
                {
                    for (cnt2 = 0; cnt2 < Nfp[cnt].GetUpperBound(0); cnt2++)
                    {
                        sb.Append(Nfp[cnt][cnt2]);
                        sb.Append(", ");
                    }
                    sb.Append(Nfp[cnt][Nfp[cnt].GetUpperBound(0)]);
                }
                sb.Append("), ");
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("东方Network结构 =>");
            sb.Append(string.Format("{0}層(", thn.Nly.Length));
            for (cnt = 0; cnt < thn.Nly.GetUpperBound(0); cnt++)
            {
                sb.Append(thn.Nly[cnt].Nn);
                sb.Append(", ");
            }
            sb.Append(thn.Nly[thn.Nly.GetUpperBound(0)].Nn);
            sb.AppendLine(")");
            sb.Append("データ数 : ");
            sb.AppendLine(thn.Dtn.ToString());
            for (cnt = 0; cnt < thn.Nly.Length; cnt++)//層
            {
                sb.Append("Layer ");
                sb.Append(thn.Nly[cnt].Ln.ToString());
                sb.Append(" : ");
                sb.AppendLine(thn.Nly[cnt].Nn.ToString());
                for (cnt2 = 0; cnt2 < thn.Nly[cnt].Nn; cnt2++)//ノード
                {
                    sb.AppendLine(thn.Nly[cnt].N(cnt2).NM);
                    sb.Append(string.Format("输入层 ({0}): ", thn.Nly[cnt].N(cnt2).SR));
                    if (thn.Nly[cnt].N(cnt2).SR == 0) sb.AppendLine("null");
                    else
                    {
                        itemp = thn.Nly[cnt].N(cnt2).SR - 1;
                        for (cnt3 = 0; cnt3 < itemp; cnt3++)//入力
                        {
                            sb.Append(thn.Nly[cnt].N(cnt2).NRL(cnt3));
                            sb.Append(", ");
                        }
                        sb.AppendLine(thn.Nly[cnt].N(cnt2).NRL(itemp).ToString());
                    }
                    sb.Append(string.Format("入力ノード ({0}): ", thn.Nly[cnt].N(cnt2).SR));
                    if (thn.Nly[cnt].N(cnt2).SR == 0) sb.AppendLine("null");
                    else
                    {
                        itemp = thn.Nly[cnt].N(cnt2).SR - 1;
                        for (cnt3 = 0; cnt3 < itemp; cnt3++)//入力
                        {
                            sb.Append(thn.Nly[cnt].N(cnt2).NRN(cnt3));
                            sb.Append(", ");
                        }
                        sb.AppendLine(thn.Nly[cnt].N(cnt2).NRN(itemp).ToString());
                    }
                    sb.Append(string.Format("Input Coefficient ({0}): ", thn.Nly[cnt].N(cnt2).SR));
                    if (thn.Nly[cnt].N(cnt2).SR == 0) sb.AppendLine("null");
                    else
                    {
                        itemp = thn.Nly[cnt].N(cnt2).SR - 1;
                        for (cnt3 = 0; cnt3 < itemp; cnt3++)//入力
                        {
                            sb.Append(thn.Nly[cnt].N(cnt2).K(cnt3).ToString("G15"));
                            sb.Append(", ");
                        }
                        sb.AppendLine(thn.Nly[cnt].N(cnt2).K(itemp).ToString("G15"));
                    }
                    sb.Append(string.Format("输出层 ({0}): ", thn.Nly[cnt].N(cnt2).SC));
                    if (thn.Nly[cnt].N(cnt2).SC == 0) sb.AppendLine("null");
                    else
                    {
                        itemp = thn.Nly[cnt].N(cnt2).SC - 1;
                        for (cnt3 = 0; cnt3 < itemp; cnt3++)//入力
                        {
                            sb.Append(thn.Nly[cnt].N(cnt2).SRL(cnt3));
                            sb.Append(", ");
                        }
                        sb.AppendLine(thn.Nly[cnt].N(cnt2).SRL(itemp).ToString());
                    }
                    sb.Append(string.Format("出力ノード ({0}): ", thn.Nly[cnt].N(cnt2).SC));
                    if (thn.Nly[cnt].N(cnt2).SC == 0) sb.AppendLine("null");
                    else
                    {
                        itemp = thn.Nly[cnt].N(cnt2).SC - 1;
                        for (cnt3 = 0; cnt3 < itemp; cnt3++)//入力
                        {
                            sb.Append(thn.Nly[cnt].N(cnt2).SRN(cnt3));
                            sb.Append(", ");
                        }
                        sb.AppendLine(thn.Nly[cnt].N(cnt2).SRN(itemp).ToString());
                    }
                }
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.Append("ジョブ実行時間 : ");
            sb.Append(thn.swj.Elapsed.TotalSeconds.ToString("G15"));
            sb.AppendLine(" seconds");
            sb.AppendLine();
            sb.Append("现在时间 : ");
            sb.AppendLine(DateTime.Now.ToString("F"));
            if (!fi.Exists) using (fi.Create()) { };
            using (FileStream fs = new FileStream(fi.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.Unicode, 128, false))
                {
                    sw.Write(sb.ToString());
                }
            }
        }
        static private void THNRncs(in THNetwork thn, in Tuple<int, int>[] Tcn, in double[][] Fwp, in int[][] Nfp, in double[][] cdr, in Tuple<double, double>[][] e, in double[][] gdo, in int NK, in double G, in int[] Di, in double[] Dc, in double[] Dn, in FileInfo fi, in string JN, in string ResFP, in int[] Resi, in string DesFP)//東方ネットワーク記録ファイル作成(未収束)(シングルジョブ)、thnは東方ネットワーク、Tcnは(0,n]層の連結数範囲、Fwpは超層連結率、Nfpは関数確率(一万分の)、cdrは値域区間、eは収束条件(1<2区間、1=2誤差、1>2固定誤差)、gdo[Nn-1][Dtn]は真データ、NKは学習回数、Gは学習率、Diは記述子インデックス、Dcは記述子中心化係数、Dnは記述子正規化スケール、fiは記録ファイル、JNはジョブ名、ResFPは真データファイルパス、Resiは真データインデックス、DesFPは記述子ファイルパス
        {
            StringBuilder sb = new StringBuilder();
            int cnt, cnt2, cnt3, itemp;
            sb.Append("任务名 : ");
            sb.AppendLine(JN);
            sb.AppendLine();
            sb.Append("記述子ファイルパス : ");
            sb.AppendLine(DesFP);
            sb.Append(string.Format("Descriptor index ({0}): ", Di.Length));
            for (cnt = 0; cnt < Di.GetUpperBound(0); cnt++)
            {
                sb.Append(string.Format("{0}, ", Di[cnt]));
            }
            sb.Append(Di[Di.GetUpperBound(0)]);
            sb.AppendLine();
            sb.Append(string.Format("中心化常数 ({0})： ", Dc.Length));
            for (cnt = 0; cnt < Dc.GetUpperBound(0); cnt++)
            {
                sb.Append(string.Format("{0:G15}, ", Dc[cnt]));
            }
            sb.Append(Dc[Dc.GetUpperBound(0)].ToString("G15"));
            sb.AppendLine();
            sb.Append(string.Format("正規化係数 ({0})： ", Dn.Length));
            for (cnt = 0; cnt < Dn.GetUpperBound(0); cnt++)
            {
                sb.Append(string.Format("{0:G15}, ", Dn[cnt]));
            }
            sb.Append(Dn[Dn.GetUpperBound(0)].ToString("G15"));
            sb.AppendLine();
            sb.AppendLine();
            sb.Append("Result File Path : ");
            sb.AppendLine(ResFP);
            sb.Append(string.Format("真数序数 ({0}): ", Resi.Length));
            for (cnt = 0; cnt < Resi.GetUpperBound(0); cnt++)
            {
                sb.Append(string.Format("{0}, ", Resi[cnt]));
            }
            sb.AppendLine(Resi[Resi.GetUpperBound(0)].ToString());
            sb.AppendLine();
            sb.AppendLine("Network Generation Parameters =>");
            sb.Append(string.Format("层连接范围 ({0}): ", Tcn.Length));
            for (cnt = 0; cnt < Tcn.Length; cnt++)
            {
                sb.Append("(");
                sb.Append(Tcn[cnt].Item1);
                sb.Append(", ");
                sb.Append(Tcn[cnt].Item2);
                sb.Append("), ");
            }
            sb.AppendLine();
            sb.Append(string.Format("超層連結率 ({0}): ", Fwp.Length));
            for (cnt = 0; cnt < Fwp.Length; cnt++)
            {
                sb.Append("(");
                for (cnt2 = 0; cnt2 < Fwp[cnt].GetUpperBound(0); cnt2++)
                {
                    sb.Append(Fwp[cnt][cnt2]);
                    sb.Append(", ");
                }
                sb.Append(Fwp[cnt][Fwp[cnt].GetUpperBound(0)]);
                sb.Append("), ");
            }
            sb.AppendLine();
            sb.Append(string.Format("Node Function Probability ({0}): ", Nfp.Length));
            for (cnt = 0; cnt < Nfp.Length; cnt++)
            {
                sb.Append("(");
                if (Nfp[cnt] == null) sb.Append("R");
                else
                {
                    for (cnt2 = 0; cnt2 < Nfp[cnt].GetUpperBound(0); cnt2++)
                    {
                        sb.Append(Nfp[cnt][cnt2]);
                        sb.Append(", ");
                    }
                    sb.Append(Nfp[cnt][Nfp[cnt].GetUpperBound(0)]);
                }
                sb.Append("), ");
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("东方Network结构 =>");
            sb.Append(string.Format("{0}層(", thn.Nly.Length));
            for (cnt = 0; cnt < thn.Nly.GetUpperBound(0); cnt++)
            {
                sb.Append(thn.Nly[cnt].Nn);
                sb.Append(", ");
            }
            sb.Append(thn.Nly[thn.Nly.GetUpperBound(0)].Nn);
            sb.AppendLine(")");
            sb.Append("データ数 : ");
            sb.AppendLine(thn.Dtn.ToString());
            for (cnt = 0; cnt < thn.Nly.Length; cnt++)//層
            {
                sb.Append("Layer ");
                sb.Append(thn.Nly[cnt].Ln.ToString());
                sb.Append(" : ");
                sb.AppendLine(thn.Nly[cnt].Nn.ToString());
                for (cnt2 = 0; cnt2 < thn.Nly[cnt].Nn; cnt2++)//ノード
                {
                    sb.AppendLine(thn.Nly[cnt].N(cnt2).NM);
                    sb.Append(string.Format("输入层 ({0}): ", thn.Nly[cnt].N(cnt2).SR));
                    if (thn.Nly[cnt].N(cnt2).SR == 0) sb.AppendLine("null");
                    else
                    {
                        itemp = thn.Nly[cnt].N(cnt2).SR - 1;
                        for (cnt3 = 0; cnt3 < itemp; cnt3++)//入力
                        {
                            sb.Append(thn.Nly[cnt].N(cnt2).NRL(cnt3));
                            sb.Append(", ");
                        }
                        sb.AppendLine(thn.Nly[cnt].N(cnt2).NRL(itemp).ToString());
                    }
                    sb.Append(string.Format("入力ノード ({0}): ", thn.Nly[cnt].N(cnt2).SR));
                    if (thn.Nly[cnt].N(cnt2).SR == 0) sb.AppendLine("null");
                    else
                    {
                        itemp = thn.Nly[cnt].N(cnt2).SR - 1;
                        for (cnt3 = 0; cnt3 < itemp; cnt3++)//入力
                        {
                            sb.Append(thn.Nly[cnt].N(cnt2).NRN(cnt3));
                            sb.Append(", ");
                        }
                        sb.AppendLine(thn.Nly[cnt].N(cnt2).NRN(itemp).ToString());
                    }
                    sb.Append(string.Format("Input Coefficient ({0}): ", thn.Nly[cnt].N(cnt2).SR));
                    if (thn.Nly[cnt].N(cnt2).SR == 0) sb.AppendLine("null");
                    else
                    {
                        itemp = thn.Nly[cnt].N(cnt2).SR - 1;
                        for (cnt3 = 0; cnt3 < itemp; cnt3++)//入力
                        {
                            sb.Append(thn.Nly[cnt].N(cnt2).K(cnt3).ToString("G15"));
                            sb.Append(", ");
                        }
                        sb.AppendLine(thn.Nly[cnt].N(cnt2).K(itemp).ToString("G15"));
                    }
                    sb.Append(string.Format("输出层 ({0}): ", thn.Nly[cnt].N(cnt2).SC));
                    if (thn.Nly[cnt].N(cnt2).SC == 0) sb.AppendLine("null");
                    else
                    {
                        itemp = thn.Nly[cnt].N(cnt2).SC - 1;
                        for (cnt3 = 0; cnt3 < itemp; cnt3++)//入力
                        {
                            sb.Append(thn.Nly[cnt].N(cnt2).SRL(cnt3));
                            sb.Append(", ");
                        }
                        sb.AppendLine(thn.Nly[cnt].N(cnt2).SRL(itemp).ToString());
                    }
                    sb.Append(string.Format("出力ノード ({0}): ", thn.Nly[cnt].N(cnt2).SC));
                    if (thn.Nly[cnt].N(cnt2).SC == 0) sb.AppendLine("null");
                    else
                    {
                        itemp = thn.Nly[cnt].N(cnt2).SC - 1;
                        for (cnt3 = 0; cnt3 < itemp; cnt3++)//入力
                        {
                            sb.Append(thn.Nly[cnt].N(cnt2).SRN(cnt3));
                            sb.Append(", ");
                        }
                        sb.AppendLine(thn.Nly[cnt].N(cnt2).SRN(itemp).ToString());
                    }
                }
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("学習 =>");
            sb.AppendLine("Converge Threshold : ");
            for (cnt = 0; cnt < cdr.Length; cnt++)
            {
                sb.Append("数值区间 : ");
                for (cnt2 = 0; cnt2 < cdr[cnt].GetUpperBound(0); cnt2++)
                {
                    sb.Append(cdr[cnt][cnt2].ToString("G15"));
                    sb.Append(", ");
                }
                sb.AppendLine(cdr[cnt][cdr[cnt].GetUpperBound(0)].ToString("G15"));
                sb.Append("誤差範囲 : ");
                for (cnt2 = 0; cnt2 < e[cnt].Length; cnt2++)
                {
                    sb.Append("(");
                    sb.Append(e[cnt][cnt2].Item1.ToString("G15"));
                    sb.Append(", ");
                    sb.Append(e[cnt][cnt2].Item2.ToString("G15"));
                    sb.Append(")");
                }
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendLine(string.Format("学习次数 : {0}", NK));
            sb.AppendLine(string.Format("学習率 : {0}", G));
            sb.AppendLine(string.Format("Convergence : {0}", thn.C));
            sb.AppendLine();
            sb.AppendLine("优化结果 : ");
            for (cnt = 0; cnt < thn.Nly[thn.Nly.GetUpperBound(0)].Nn; cnt++)
            {
                sb.AppendLine(string.Format("{0} : ", cnt));
                sb.Append("予測 ： ");
                for (cnt2 = 0; cnt2 < thn.Dtn; cnt2++)
                {
                    sb.Append(string.Format("{0,-22} ", thn.Nly[thn.Nly.GetUpperBound(0)].N(cnt).F(cnt2).ToString("G15")));
                }
                sb.AppendLine();
                sb.Append("真値 ： ");
                for (cnt2 = 0; cnt2 < thn.Dtn; cnt2++)
                {
                    sb.Append(string.Format("{0,-22} ", gdo[cnt][cnt2].ToString("G15")));
                }
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.Append("ジョブ実行時間 : ");
            sb.Append(thn.swj.Elapsed.TotalSeconds.ToString("G15"));
            sb.AppendLine(" seconds");
            sb.AppendLine();
            sb.Append("现在时间 : ");
            sb.AppendLine(DateTime.Now.ToString("F"));
            if (!fi.Exists) using (fi.Create()) { };
            using (FileStream fs = new FileStream(fi.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.Unicode, 128, false))
                {
                    sw.Write(sb.ToString());
                }
            }
        }
        static private void THNRcs(in THNetwork thn, in double[][] S, in Tuple<int, int>[] Tcn, in double[][] Fwp, in int[][] Nfp, in double[][] cdr, in Tuple<double, double>[][] e, in double[][] gdo, in double[][] gdt, in int NK, in int XXC, in double G, in int[] Di, in double[] Dc, in double[] Dn, in FileInfo fi, in int[] Dti, in string JN, in string ResFP, in int[] Resi, in string DesFP)//東方ネットワーク記録ファイル作成(収束)(シングルジョブ)、thnは東方ネットワーク、Sは統計量、Tcnは(0,n]層の連結数範囲、Fwpは超層連結率、Nfpは関数確率(一万分の)、cdrは値域区間、eは収束条件(1<2区間、1=2誤差、1>2固定誤差)、gdo[Nn-1][Dtn]は真データ、gdtはテスト真データ、NKは学習回数、XXCは実際学習回数、Gは学習率、Diはデータインデックス、Dcはデータ中心化係数、Dnは正規化スケール、fiは記録ファイル、Dtiはテストデータインデックス、JNはジョブ名、ResFPは真データファイルパス、Resiは真データインデックス、DesFPは記述子ファイルパス
        {
            StringBuilder sb = new StringBuilder();
            int cnt, cnt2, cnt3, itemp;
            sb.Append("任务名 : ");
            sb.AppendLine(JN);
            sb.AppendLine();
            sb.Append("記述子ファイルパス : ");
            sb.AppendLine(DesFP);
            sb.Append(string.Format("Descriptor index ({0}): ", Di.Length));
            for (cnt = 0; cnt < Di.GetUpperBound(0); cnt++)
            {
                sb.Append(string.Format("{0}, ", Di[cnt]));
            }
            sb.Append(Di[Di.GetUpperBound(0)]);
            sb.AppendLine();
            sb.Append(string.Format("中心化常数 ({0})： ", Dc.Length));
            for (cnt = 0; cnt < Dc.GetUpperBound(0); cnt++)
            {
                sb.Append(string.Format("{0:G15}, ", Dc[cnt]));
            }
            sb.Append(Dc[Dc.GetUpperBound(0)].ToString("G15"));
            sb.AppendLine();
            sb.Append(string.Format("正規化係数 ({0})： ", Dn.Length));
            for (cnt = 0; cnt < Dn.GetUpperBound(0); cnt++)
            {
                sb.Append(string.Format("{0:G15}, ", Dn[cnt]));
            }
            sb.Append(Dn[Dn.GetUpperBound(0)].ToString("G15"));
            sb.AppendLine();
            sb.AppendLine();
            sb.Append("Result File Path : ");
            sb.AppendLine(ResFP);
            sb.Append(string.Format("真数序数 ({0}): ", Resi.Length));
            for (cnt = 0; cnt < Resi.GetUpperBound(0); cnt++)
            {
                sb.Append(string.Format("{0}, ", Resi[cnt]));
            }
            sb.AppendLine(Resi[Resi.GetUpperBound(0)].ToString());
            sb.AppendLine();
            sb.AppendLine("Network Generation Parameters =>");
            sb.Append(string.Format("层连接范围 ({0}): ", Tcn.Length));
            for (cnt = 0; cnt < Tcn.Length; cnt++)
            {
                sb.Append("(");
                sb.Append(Tcn[cnt].Item1);
                sb.Append(", ");
                sb.Append(Tcn[cnt].Item2);
                sb.Append("), ");
            }
            sb.AppendLine();
            sb.Append(string.Format("超層連結率 ({0}): ", Fwp.Length));
            for (cnt = 0; cnt < Fwp.Length; cnt++)
            {
                sb.Append("(");
                for (cnt2 = 0; cnt2 < Fwp[cnt].GetUpperBound(0); cnt2++)
                {
                    sb.Append(Fwp[cnt][cnt2]);
                    sb.Append(", ");
                }
                sb.Append(Fwp[cnt][Fwp[cnt].GetUpperBound(0)]);
                sb.Append("), ");
            }
            sb.AppendLine();
            sb.Append(string.Format("Node Function Probability ({0}): ", Nfp.Length));
            for (cnt = 0; cnt < Nfp.Length; cnt++)
            {
                sb.Append("(");
                if (Nfp[cnt] == null) sb.Append("R");
                else
                {
                    for (cnt2 = 0; cnt2 < Nfp[cnt].GetUpperBound(0); cnt2++)
                    {
                        sb.Append(Nfp[cnt][cnt2]);
                        sb.Append(", ");
                    }
                    sb.Append(Nfp[cnt][Nfp[cnt].GetUpperBound(0)]);
                }
                sb.Append("), ");
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("东方Network结构 =>");
            sb.Append(string.Format("{0}層(", thn.Nly.Length));
            for (cnt = 0; cnt < thn.Nly.GetUpperBound(0); cnt++)
            {
                sb.Append(thn.Nly[cnt].Nn);
                sb.Append(", ");
            }
            sb.Append(thn.Nly[thn.Nly.GetUpperBound(0)].Nn);
            sb.AppendLine(")");
            sb.Append("データ数 : ");
            sb.AppendLine(thn.Dtn.ToString());
            for (cnt = 0; cnt < thn.Nly.Length; cnt++)//層
            {
                sb.Append("Layer ");
                sb.Append(thn.Nly[cnt].Ln.ToString());
                sb.Append(" : ");
                sb.AppendLine(thn.Nly[cnt].Nn.ToString());
                for (cnt2 = 0; cnt2 < thn.Nly[cnt].Nn; cnt2++)//ノード
                {
                    sb.AppendLine(thn.Nly[cnt].N(cnt2).NM);
                    sb.Append(string.Format("输入层 ({0}): ", thn.Nly[cnt].N(cnt2).SR));
                    if (thn.Nly[cnt].N(cnt2).SR == 0) sb.AppendLine("null");
                    else
                    {
                        itemp = thn.Nly[cnt].N(cnt2).SR - 1;
                        for (cnt3 = 0; cnt3 < itemp; cnt3++)//入力
                        {
                            sb.Append(thn.Nly[cnt].N(cnt2).NRL(cnt3));
                            sb.Append(", ");
                        }
                        sb.AppendLine(thn.Nly[cnt].N(cnt2).NRL(itemp).ToString());
                    }
                    sb.Append(string.Format("入力ノード ({0}): ", thn.Nly[cnt].N(cnt2).SR));
                    if (thn.Nly[cnt].N(cnt2).SR == 0) sb.AppendLine("null");
                    else
                    {
                        itemp = thn.Nly[cnt].N(cnt2).SR - 1;
                        for (cnt3 = 0; cnt3 < itemp; cnt3++)//入力
                        {
                            sb.Append(thn.Nly[cnt].N(cnt2).NRN(cnt3));
                            sb.Append(", ");
                        }
                        sb.AppendLine(thn.Nly[cnt].N(cnt2).NRN(itemp).ToString());
                    }
                    sb.Append(string.Format("Input Coefficient ({0}): ", thn.Nly[cnt].N(cnt2).SR));
                    if (thn.Nly[cnt].N(cnt2).SR == 0) sb.AppendLine("null");
                    else
                    {
                        itemp = thn.Nly[cnt].N(cnt2).SR - 1;
                        for (cnt3 = 0; cnt3 < itemp; cnt3++)//入力
                        {
                            sb.Append(thn.Nly[cnt].N(cnt2).K(cnt3).ToString("G15"));
                            sb.Append(", ");
                        }
                        sb.AppendLine(thn.Nly[cnt].N(cnt2).K(itemp).ToString("G15"));
                    }
                    sb.Append(string.Format("输出层 ({0}): ", thn.Nly[cnt].N(cnt2).SC));
                    if (thn.Nly[cnt].N(cnt2).SC == 0) sb.AppendLine("null");
                    else
                    {
                        itemp = thn.Nly[cnt].N(cnt2).SC - 1;
                        for (cnt3 = 0; cnt3 < itemp; cnt3++)//入力
                        {
                            sb.Append(thn.Nly[cnt].N(cnt2).SRL(cnt3));
                            sb.Append(", ");
                        }
                        sb.AppendLine(thn.Nly[cnt].N(cnt2).SRL(itemp).ToString());
                    }
                    sb.Append(string.Format("出力ノード ({0}): ", thn.Nly[cnt].N(cnt2).SC));
                    if (thn.Nly[cnt].N(cnt2).SC == 0) sb.AppendLine("null");
                    else
                    {
                        itemp = thn.Nly[cnt].N(cnt2).SC - 1;
                        for (cnt3 = 0; cnt3 < itemp; cnt3++)//入力
                        {
                            sb.Append(thn.Nly[cnt].N(cnt2).SRN(cnt3));
                            sb.Append(", ");
                        }
                        sb.AppendLine(thn.Nly[cnt].N(cnt2).SRN(itemp).ToString());
                    }
                }
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("学習 =>");
            sb.AppendLine("Converge Threshold : ");
            for (cnt = 0; cnt < cdr.Length; cnt++)
            {
                sb.Append("数值区间 : ");
                for (cnt2 = 0; cnt2 < cdr[cnt].GetUpperBound(0); cnt2++)
                {
                    sb.Append(cdr[cnt][cnt2].ToString("G15"));
                    sb.Append(", ");
                }
                sb.AppendLine(cdr[cnt][cdr[cnt].GetUpperBound(0)].ToString("G15"));
                sb.Append("誤差範囲 : ");
                for (cnt2 = 0; cnt2 < e[cnt].Length; cnt2++)
                {
                    sb.Append("(");
                    sb.Append(e[cnt][cnt2].Item1.ToString("G15"));
                    sb.Append(", ");
                    sb.Append(e[cnt][cnt2].Item2.ToString("G15"));
                    sb.Append(")");
                }
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendLine(string.Format("学习次数 : {0}", NK));
            sb.AppendLine(string.Format("学習率 : {0}", G));
            sb.AppendLine(string.Format("Convergence : {0}", thn.C));
            sb.AppendLine(string.Format("实际学习次数 : {0}", XXC));
            sb.AppendLine();
            sb.AppendLine("优化结果 : ");
            for (cnt = 0; cnt < thn.Nly[thn.Nly.GetUpperBound(0)].Nn; cnt++)
            {
                sb.AppendLine(string.Format("{0} : ", cnt));
                sb.Append("予測 ： ");
                for (cnt2 = 0; cnt2 < thn.Dtn; cnt2++)
                {
                    sb.Append(string.Format("{0,-22} ", thn.Nly[thn.Nly.GetUpperBound(0)].N(cnt).F(cnt2).ToString("G15")));
                }
                sb.AppendLine();
                sb.Append("真値 ： ");
                for (cnt2 = 0; cnt2 < thn.Dtn; cnt2++)
                {
                    sb.Append(string.Format("{0,-22} ", gdo[cnt][cnt2].ToString("G15")));
                }
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("検証 => ");
            sb.Append(string.Format("検証インデックス ({0}): ", Dti.Length));
            for (cnt = 0; cnt < Dti.GetUpperBound(0); cnt++)
            {
                sb.Append(string.Format("{0}, ", Dti[cnt]));
            }
            sb.AppendLine(Dti[Dti.GetUpperBound(0)].ToString());
            sb.AppendLine();
            sb.Append("PASS : ");
            sb.AppendLine(thn.V.ToString());
            sb.AppendLine();
            sb.AppendLine("统计量 : ");//誤差絶対値和、誤差二乗和、予測値/真値cos、R28、R27、R21、r、R23
            for (cnt = 0; cnt < S.Length; cnt++)
            {
                sb.AppendLine(string.Format("Node{0} : ", cnt));
                sb.Append("EAbs = ");
                sb.Append(S[cnt][0].ToString("G15"));
                sb.Append(", E2 = ");
                sb.Append(S[cnt][1].ToString("G15"));
                sb.Append(", cos(");
                sb.Append('\u0177');
                sb.Append('\u00b7');
                sb.Append("y) = ");
                sb.AppendLine(S[cnt][2].ToString("G15"));
                sb.Append("R28 = ");
                sb.Append(S[cnt][3].ToString("G15"));
                sb.Append(", R27 = ");
                sb.Append(S[cnt][4].ToString("G15"));
                sb.Append(", R21 = ");
                sb.AppendLine(S[cnt][5].ToString("G15"));
                sb.Append("r = ");
                sb.Append(S[cnt][6].ToString("G15"));
                sb.Append(", R23 = ");
                sb.AppendLine(S[cnt][7].ToString("G15"));
            }
            sb.AppendLine();
            sb.AppendLine("検証結果 : ");
            for (cnt = 0; cnt < thn.Nly[thn.Nly.GetUpperBound(0)].Nn; cnt++)
            {
                sb.AppendLine(string.Format("{0} : ", cnt));
                sb.Append("Prediction ： ");
                for (cnt2 = 0; cnt2 < thn.Nly[thn.Nly.GetUpperBound(0)].N(cnt).DY; cnt2++)
                {
                    sb.Append(string.Format("{0,-22} ", thn.Nly[thn.Nly.GetUpperBound(0)].N(cnt).FY(cnt2).ToString("G15")));
                }
                sb.AppendLine();
                sb.Append("True value ： ");
                for (cnt2 = 0; cnt2 < gdt[cnt].Length; cnt2++)
                {
                    sb.Append(string.Format("{0,-22} ", gdt[cnt][cnt2].ToString("G15")));
                }
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.Append("ジョブ実行時間 : ");
            sb.Append(thn.swj.Elapsed.TotalSeconds.ToString("G15"));
            sb.AppendLine(" seconds");
            sb.AppendLine();
            sb.Append("现在时间 : ");
            sb.AppendLine(DateTime.Now.ToString("F"));
            if (!fi.Exists) using (fi.Create()) { };
            using (FileStream fs = new FileStream(fi.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.Unicode, 128, false))
                {
                    sw.Write(sb.ToString());
                }
            }
        }
        static internal bool THNM(in int D, in int[] ln, in Tuple<int, int>[] Tcn, in double[][] Fwp, in int[][] Nfp, in double[][] desin, in double[][] gdo, in double[][] cdr, in Tuple<double, double>[][] e, in int NK, in double G, in int[] Di, in double[] Dc, in double[] Dn, in FileInfo fi, in int Dt, in double[][] dest, in double[][] gdt, in int[] Dti, in string JN, in string ResFP, in int[] Resi, in string DesFP, bool sj, ref int cnt)//東方ネットワークモデルを作る、Dはデータ数、lnはノード数、Tcnは(0,n]層の連結数範囲、Fwpは超層連結率、Nfpは関数確率(一万分の)、desin[N0-1][Dtn]は入力ノードのデータ、gdo[Nn-1][Dtn]は真データ、cdrは値域区間、eは収束条件(1<2区間、1=2誤差、1>2固定誤差)、NKは学習回数、Gは学習率、Diは記述子インデックス、Dcは記述子中心化係数、Dnは記述子正規化スケール、fiは記録ファイル、Dtはテストデータ数、destはテスト記述子、gdtはテスト真データ、Dtiはテストデータインデックス、JNはジョブ名、ResFPは真データファイルパス、Resiは真データインデックス、DesFPは記述子ファイルパス、sj trueはシングルジョブ
        {
            try
            {
                THNetwork thn = new THNetwork(D, ln, Tcn, Fwp, Nfp);
                thn.THniru(desin, gdo);
                THNRi(in thn, in Tcn, in Fwp, in Nfp, in cdr, e, in gdo, in NK, in G, in Di, in Dc, in Dn, in fi, in JN, in ResFP, in Resi, in DesFP);
                int XXC = thn.XX(NK, in cdr, in e, in G, in desin, in gdo, ref cnt);
                if (XXC == -1)
                {
                    if (sj) THNRncs(in thn, in Tcn, in Fwp, in Nfp, in cdr, in e, in gdo, in NK, in G, in Di, in Dc, in Dn, in fi, in JN, in ResFP, in Resi, in DesFP);
                }
                else if (thn.C)
                {
                    double[][] S;
                    thn.NVT(Dt, in dest, in gdt, in cdr, in e, out S);
                    if (sj) THNRcs(in thn, in S, in Tcn, in Fwp, in Nfp, in cdr, in e, in gdo, in gdt, in NK, in XXC, in G, in Di, in Dc, in Dn, in fi, in Dti, in JN, in ResFP, in Resi, in DesFP);
                }
                else
                {
                    if (XXC != NK) throw new ArgumentOutOfRangeException("XXC/NK", "THNM : Can not verify epoch.");
                    if (sj) THNRncs(in thn, in Tcn, in Fwp, in Nfp, in cdr, e, in gdo, in NK, in G, in Di, in Dc, in Dn, in fi, in JN, in ResFP, in Resi, in DesFP);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                if (ex.InnerException != null) Console.WriteLine(ex.InnerException);
                return false;
            }
            return true;
        }
        static internal bool THNM(in int D, in int[] ln, in Tuple<int, int>[] Tcn, in double[][] Fwp, in int[][] Nfp, in double[][] desin, in double[][] gdo, in double[][] cdr, in Tuple<double, double>[][] e, in int NK, in double G, in double KRK, in bool L, in int[] Di, in double[] Dc, in double[] Dn, in FileInfo fi, in int Dt, in double[][] dest, in double[][] gdt, in int[] Dti, in string JN, in string ResFP, in int[] Resi, in string DesFP, bool sj, ref int cnt)//東方ネットワークモデルを作る(層毎更新する)、Dはデータ数、lnはノード数、Tcnは(0,n]層の連結数範囲、Fwpは超層連結率、Nfpは関数確率(一万分の)、desin[N0-1][Dtn]は入力ノードのデータ、gdo[Nn-1][Dtn]は真データ、cdrは値域区間、eは収束条件(1<2区間、1=2誤差、1>2固定誤差)、NKは学習回数、Gは学習率、KRKは確率降下受け入れ確率、L trueは層毎に更新,falseはノード毎に更新、Diは記述子インデックス、Dcは記述子中心化係数、Dnは記述子正規化スケール、fiは記録ファイル、Dtはテストデータ数、destはテスト記述子、gdtはテスト真データ、Dtiはテストデータインデックス、JNはジョブ名、ResFPは真データファイルパス、Resiは真データインデックス、DesFPは記述子ファイルパス、sj trueはシングルジョブ
        {
            try
            {
                THNetwork thn = new THNetwork(D, ln, Tcn, Fwp, Nfp);
                thn.THniru(desin, gdo);
                THNRi(in thn, in Tcn, in Fwp, in Nfp, in cdr, e, in gdo, in NK, in G, in Di, in Dc, in Dn, in fi, in JN, in ResFP, in Resi, in DesFP);
                int XXC = thn.XX(NK, in cdr, in e, in G, in KRK, in L, in desin, in gdo, ref cnt);
                if (XXC == -1)
                {
                    if (sj) THNRncs(in thn, in Tcn, in Fwp, in Nfp, in cdr, in e, in gdo, in NK, in G, in Di, in Dc, in Dn, in fi, in JN, in ResFP, in Resi, in DesFP);
                }
                else if (thn.C)
                {
                    double[][] S;
                    thn.NVT(Dt, in dest, in gdt, in cdr, in e, out S);
                    if (sj) THNRcs(in thn, in S, in Tcn, in Fwp, in Nfp, in cdr, in e, in gdo, in gdt, in NK, in XXC, in G, in Di, in Dc, in Dn, in fi, in Dti, in JN, in ResFP, in Resi, in DesFP);
                }
                else
                {
                    if (XXC != NK) throw new ArgumentOutOfRangeException("XXC/NK", "THNM : Can not verify epoch.");
                    if (sj) THNRncs(in thn, in Tcn, in Fwp, in Nfp, in cdr, e, in gdo, in NK, in G, in Di, in Dc, in Dn, in fi, in JN, in ResFP, in Resi, in DesFP);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                if (ex.InnerException != null) Console.WriteLine(ex.InnerException);
                return false;
            }
            return true;
        }
        static internal bool THNJskk(in string rwm, in string[] desp, in string resp, in string rwp, in int[] resi, in int[] resg, in int[][] resgi, in int[] desn, in double[][] cdr, in Tuple<double, double>[][] err, in int NK, in double G, in double KRK, in byte thnn)//東方ネットワークジョブ初期化、rwmはジョブ名、despは記述子ファイルパス、respは真データファイルパス、rwpはジョブディレクトリ、resiは真データインデックス、resgは検証用データの個数(グループ毎)、resgiは検証用データインデックス(グループ毎)、desnは入力記述子個数(ファイル毎)、cdrは真値域区間、errは誤差許容範囲(収束条件)(区間毎)、NKは学習回数、Gは学習率、KRKは確率降下の確率(0は貪欲法、1は確定受け入れる)、thnn 0は正規化しない,1は標準スコア正規化,2は東方ネットワーク正規化
        {
            if (desp == null || desn == null || desp.Length != desn.Length || desp.Length == 0) return false;
            if (resi == null || resg == null || resgi == null || cdr == null || err == null || resi.Length != cdr.Length || cdr.Length != err.Length || resi.Length == 0) return false;
            if (!Directory.Exists(rwp))
            {
                Directory.CreateDirectory(rwp);
            }
            DirectoryInfo di = new DirectoryInfo(rwp);
            if ((di.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly || (di.Attributes & FileAttributes.System) == FileAttributes.System) return false;
            StringBuilder sb = new StringBuilder(rwp);
            if (!rwp.EndsWith('\\')) sb.Append('\\');
            sb.Append("CSH.RCjob");
            string CSHf = sb.ToString();
            bool fhf = false;
            bool frf = false;
            if (!File.Exists(CSHf)) using (File.Create(CSHf)) { }
            else
            {
                if ((File.GetAttributes(CSHf) & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    File.SetAttributes(CSHf, File.GetAttributes(CSHf) & ~FileAttributes.Hidden);
                    fhf = true;
                }
                if ((File.GetAttributes(CSHf) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(CSHf, File.GetAttributes(CSHf) & ~FileAttributes.ReadOnly);
                    frf = true;
                }
            }
            sb = new StringBuilder();
            sb.Append("ジョブ名：");
            sb.AppendLine(rwm);
            int cnt;
            for (cnt = 0; cnt < desp.Length; cnt++)
            {
                sb.Append("描述符文件路径：");
                sb.AppendLine(desp[cnt]);
                sb.Append("Descriptor amount : ");
                sb.AppendLine(desn[cnt].ToString("G"));
            }
            sb.AppendLine();
            sb.Append("真データファイルパス：");
            sb.AppendLine(resp);
            int cnt2;
            for (cnt = 0; cnt < resi.Length; cnt++)
            {
                sb.Append("真值序数：");
                sb.AppendLine(resi[cnt].ToString("G"));
                sb.Append("误差区间：");
                if (cdr[cnt] == null || cdr[cnt].Length == 0) return false;
                for (cnt2 = 0; cnt2 < cdr[cnt].Length; cnt2++)
                {
                    sb.Append(cdr[cnt][cnt2].ToString("G15"));
                    sb.Append(", ");
                }
                sb.AppendLine();
                if (err[cnt] == null || err[cnt].Length == 0 || cdr[cnt].Length != err[cnt].Length) return false;
                sb.Append("Error definition : ");
                for (cnt2 = 0; cnt2 < err[cnt].Length; cnt2++)
                {
                    if (err[cnt][cnt2] == null) return false;
                    sb.Append("(");
                    sb.Append(err[cnt][cnt2].Item1.ToString("G15"));
                    sb.Append(", ");
                    sb.Append(err[cnt][cnt2].Item2.ToString("G15"));
                    sb.Append(")");
                    sb.Append(", ");
                }
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.Append("Verification data amount : ");
            if (resg.Length == 0 || resg.Length != resgi.Length) return false;
            for (cnt = 0; cnt < resg.GetUpperBound(0); cnt++)
            {
                sb.Append(resg[cnt].ToString("G"));
                sb.Append(",");
            }
            sb.Append(resg[cnt].ToString("G"));
            sb.AppendLine();
            sb.AppendLine("検証データグループ：");
            for (cnt = 0; cnt < resgi.Length; cnt++)
            {
                if (resgi.Length == 1 && resgi[0].Length == 1 && resgi[0][0] == -1)
                {
                    sb.AppendLine("-1");
                }
                else
                {
                    if (resgi[cnt].Length < resg[cnt]) return false;
                    for (cnt2 = 0; cnt2 < resgi[cnt].Length; cnt2++)
                    {
                        sb.Append(resgi[cnt][cnt2].ToString("G"));
                        sb.Append(", ");
                    }
                    sb.AppendLine();
                }
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.Append("学習回数：");
            sb.AppendLine(NK.ToString("G"));
            sb.Append("学习率：");
            sb.AppendLine(G.ToString("G15"));
            sb.Append("確率降下：");
            sb.AppendLine(KRK.ToString("G15"));
            sb.Append("Normalization method : ");
            sb.AppendLine(thnn.ToString("G"));
            sb.AppendLine();
            sb.Append(DateTime.Now.ToString("F"));
            using (FileStream fs = new FileStream(CSHf, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 128))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.Unicode, 128, false))
                {
                    sw.Write(sb.ToString());
                }
            }
            if (fhf)
            {
                File.SetAttributes(CSHf, File.GetAttributes(CSHf) | FileAttributes.Hidden);
            }
            if (frf)
            {
                File.SetAttributes(CSHf, File.GetAttributes(CSHf) | FileAttributes.ReadOnly);
            }
            return true;
        }
        static internal bool THNJR(out string msg, out string rwm, out string[] desp, out string resp, in string rwp, out int[] resi, out int[] resg, out int[][] resgi, out int[] desn, out double[][] cdr, out Tuple<double, double>[][] err, out int NK, out double G, out double KRK, out byte thnn)//東方ネットワークジョブ読み込み、msgはメッセージ、rwmはジョブ名、despは記述子ファイルパス、respは真データファイルパス、rwpはジョブディレクトリ、resiは真データインデックス、resgは検証用データの個数(グループ毎)、resgiは検証用データインデックス(グループ毎)、desnは入力記述子個数(ファイル毎)、cdrは真値域区間、errは誤差許容範囲(収束条件)(区間毎)、NKは学習回数、Gは学習率、thnn 0は正規化しない,1は標準スコア正規化,2は東方ネットワーク正規化
        {
            msg = "Unexpected termination";
            rwm = null;
            desp = null;
            resp = null;
            resi = null;
            resg = null;
            resgi = null;
            desn = null;
            cdr = null;
            err = null;
            NK = 0;
            G = 0.0;
            KRK = double.NaN;
            thnn = (byte)0;
            StringBuilder sb = new StringBuilder(rwp);
            if (!rwp.EndsWith('\\')) sb.Append('\\');
            sb.Append("CSH.RCjob");
            string CSHf = sb.ToString();
            string s;
            Match m;
            List<string> despl = new List<string>();
            List<int> desnl = new List<int>();
            List<int> resil = new List<int>();
            List<double[]> cdrl = new List<double[]>();
            double[] cdra;
            List<Tuple<double, double>[]> errl = new List<Tuple<double, double>[]>();
            Tuple<double, double>[] erra;
            int[] vdg;
            bool flg;
            int itemp, ivf, cnt, ivf2, cnt2;
            double dtemp, dtemp2;
            if (!File.Exists(CSHf))
            {
                msg = "Jobfile does not exist.";
                return false;
            }
            using (FileStream fs = new FileStream(CSHf, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.Unicode, true, 128, false))
                {
                    s = sr.ReadLine();
                    if (s == null)
                    {
                        msg = "Can not find job name.";
                        return false;
                    }
                    m = THNRgx.r1a.Match(s);
                    if (!m.Success)
                    {
                        msg = "Can not find job name.";
                        return false;
                    }
                    rwm = m.Groups[1].Captures[0].Value.Trim();
                    if (rwm.Length == 0)
                    {
                        msg = "Invalid job name.";
                        return false;
                    }
                    s = sr.ReadLine();
                    if (s == null)
                    {
                        msg = "Can not find descriptor file path.";
                        return false;
                    }
                    m = THNRgx.r2a.Match(s);
                    if (!m.Success)
                    {
                        msg = "Can not find descriptor file path.";
                        return false;
                    }
                    despl.Add(m.Groups[1].Captures[0].Value.Trim());
                    if (despl[0].Length == 0)
                    {
                        msg = "Invalid descriptor file path.";
                        return false;
                    }
                    if (!File.Exists(despl[0]))
                    {
                        msg = string.Format("Descriptor file doesn't exist : {0}.", despl[0]);
                        return false;
                    }
                    s = sr.ReadLine();
                    if (s == null)
                    {
                        msg = "Can not find descriptor amount information.";
                        return false;
                    }
                    m = THNRgx.r2b.Match(s);
                    if (!m.Success)
                    {
                        msg = "Can not find descriptor amount information.";
                        return false;
                    }
                    s = m.Groups[1].Captures[0].Value.Trim();
                    flg = int.TryParse(s, out itemp);
                    if (!flg || itemp <= 0)
                    {
                        msg = "Invalid descriptor amount.";
                        return false;
                    }
                    desnl.Add(itemp);
                    ivf = 1;
                    while (true)
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = "Can not find true data file path.";
                            return false;
                        }
                        m = THNRgx.r2a.Match(s);
                        if (!m.Success) break;
                        despl.Add(m.Groups[1].Captures[0].Value.Trim());
                        if (despl[ivf].Length == 0)
                        {
                            msg = "Invalid descriptor file path.";
                            return false;
                        }
                        if (!File.Exists(despl[ivf]))
                        {
                            msg = string.Format("Descriptor file doesn't exist : {0}.", despl[ivf]);
                            return false;
                        }
                        s = sr.ReadLine();
                        m = THNRgx.r2b.Match(s);
                        if (!m.Success)
                        {
                            msg = "Can not find descriptor amount information.";
                            return false;
                        }
                        s = m.Groups[1].Captures[0].Value.Trim();
                        flg = int.TryParse(s, out itemp);
                        if (!flg || itemp <= 0)
                        {
                            msg = "Invalid descriptor amount.";
                            return false;
                        }
                        desnl.Add(itemp);
                        ivf++;
                    }
                    if (despl.Count != desnl.Count || despl.Count != ivf)
                    {
                        msg = "Can not verify descriptor file information.";
                        return false;
                    }
                    desp = despl.ToArray();
                    despl = null;
                    desn = desnl.ToArray();
                    desnl = null;
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = "Can not find true data file path.";
                            return false;
                        }
                    }
                    while (s.Trim() == string.Empty);
                    m = THNRgx.r6a.Match(s);
                    if (!m.Success)
                    {
                        msg = "Can not find true data file path.";
                        return false;
                    }
                    resp = m.Groups[1].Captures[0].Value.Trim();
                    if (!File.Exists(resp))
                    {
                        msg = string.Format("True data file doesn't exist : {0}.", resp);
                        return false;
                    }
                    s = sr.ReadLine();
                    if (s == null)
                    {
                        msg = "Can not find true data index information.";
                        return false;
                    }
                    m = THNRgx.r7a.Match(s);
                    if (!m.Success)
                    {
                        msg = "Can not find true data index information.";
                        return false;
                    }
                    s = m.Groups[1].Captures[0].Value.Trim();
                    flg = int.TryParse(s, out itemp);
                    if (!flg || itemp < 0)
                    {
                        msg = "Invalid true data index.";
                        return false;
                    }
                    resil.Add(itemp);
                    s = sr.ReadLine();
                    if (s == null)
                    {
                        msg = "Can not find true data region information.";
                        return false;
                    }
                    m = THNRgx.r21a.Match(s);
                    if (!m.Success)
                    {
                        msg = "Can not find true data region information.";
                        return false;
                    }
                    cdra = new double[m.Groups[1].Captures.Count];
                    for (cnt = 0; cnt < cdra.Length; cnt++)
                    {
                        flg = double.TryParse(m.Groups[1].Captures[cnt].Value.Trim(), out dtemp);
                        if (!flg)
                        {
                            msg = "Incorrect true data region format.";
                            return false;
                        }
                        cdra[cnt] = dtemp;
                    }
                    cdrl.Add(cdra);
                    s = sr.ReadLine();
                    if (s == null)
                    {
                        msg = "Can not find error definition.";
                        return false;
                    }
                    m = THNRgx.r22a.Match(s);
                    if (!m.Success)
                    {
                        msg = "Can not find error definition.";
                        return false;
                    }
                    if (m.Groups[1].Captures.Count != cdrl[0].Length || m.Groups[2].Captures.Count != cdrl[0].Length)
                    {
                        msg = "Incorrect error definition format.";
                        return false;
                    }
                    erra = new Tuple<double, double>[cdrl[0].Length];
                    for (cnt = 0; cnt < erra.Length; cnt++)
                    {
                        flg = double.TryParse(m.Groups[1].Captures[cnt].Value.Trim(), out dtemp);
                        if (!flg)
                        {
                            msg = "Incorrect error definition format.";
                            return false;
                        }
                        flg = double.TryParse(m.Groups[2].Captures[cnt].Value.Trim(), out dtemp2);
                        if (!flg)
                        {
                            msg = "Incorrect error definition format.";
                            return false;
                        }
                        erra[cnt] = new Tuple<double, double>(dtemp, dtemp2);
                    }
                    errl.Add(erra);
                    ivf2 = 1;
                    while (true)
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = "Can not find verification data information.";
                            return false;
                        }
                        m = THNRgx.r7a.Match(s);
                        if (!m.Success)
                        {
                            break;
                        }
                        s = m.Groups[1].Captures[0].Value.Trim();
                        flg = int.TryParse(s, out itemp);
                        if (!flg || itemp < 0)
                        {
                            msg = "Invalid true data index.";
                            return false;
                        }
                        resil.Add(itemp);
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = "Can not find true data region information.";
                            return false;
                        }
                        m = THNRgx.r21a.Match(s);
                        if (!m.Success)
                        {
                            msg = "Can not find true data region information.";
                            return false;
                        }
                        cdra = new double[m.Groups[1].Captures.Count];
                        for (cnt = 0; cnt < cdra.Length; cnt++)
                        {
                            flg = double.TryParse(m.Groups[1].Captures[cnt].Value.Trim(), out dtemp);
                            if (!flg)
                            {
                                msg = "Incorrect true data region format.";
                                return false;
                            }
                            cdra[cnt] = dtemp;
                        }
                        cdrl.Add(cdra);
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = "Can not find error definition.";
                            return false;
                        }
                        m = THNRgx.r22a.Match(s);
                        if (!m.Success)
                        {
                            msg = "Can not find error definition.";
                            return false;
                        }
                        if (m.Groups[1].Captures.Count != cdrl[ivf2].Length || m.Groups[2].Captures.Count != cdrl[ivf2].Length)
                        {
                            msg = "Incorrect error definition format.";
                            return false;
                        }
                        erra = new Tuple<double, double>[cdrl[ivf2].Length];
                        for (cnt = 0; cnt < erra.Length; cnt++)
                        {
                            flg = double.TryParse(m.Groups[1].Captures[cnt].Value.Trim(), out dtemp);
                            if (!flg)
                            {
                                msg = "Incorrect error definition format.";
                                return false;
                            }
                            flg = double.TryParse(m.Groups[2].Captures[cnt].Value.Trim(), out dtemp2);
                            if (!flg)
                            {
                                msg = "Incorrect error definition format.";
                                return false;
                            }
                            erra[cnt] = new Tuple<double, double>(dtemp, dtemp2);
                        }
                        errl.Add(erra);
                        ivf2++;
                    }
                    if (ivf != ivf2 || resil.Count != ivf2 || cdrl.Count != ivf2 || errl.Count != ivf2)
                    {
                        msg = "Can not verify error definition information.";
                        return false;
                    }
                    resi = resil.ToArray();
                    resil = null;
                    cdr = cdrl.ToArray();
                    cdrl = null;
                    err = errl.ToArray();
                    errl = null;
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = "Can not find verification data information.";
                            return false;
                        }
                    }
                    while (s.Trim() == string.Empty);
                    m = THNRgx.r32a.Match(s);
                    if (!m.Success)
                    {
                        msg = "Can not find verification data information.";
                        return false;
                    }
                    ivf = m.Groups[1].Captures.Count;
                    resg = new int[ivf];
                    for (cnt = 0; cnt < ivf; cnt++)
                    {
                        flg = int.TryParse(m.Groups[1].Captures[cnt].Value.Trim(), out resg[cnt]);
                        if (!flg)
                        {
                            msg = "Invalid verification data amount format.";
                            return false;
                        }
                    }
                    s = sr.ReadLine();
                    if (s == null)
                    {
                        msg = "Can not find verification data group.";
                        return false;
                    }
                    m = THNRgx.r32b.Match(s);
                    if (!m.Success)
                    {
                        msg = "Can not find verification data group title.";
                        return false;
                    }
                    resgi = new int[ivf][];
                    for (cnt = 0; cnt < ivf; cnt++)
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = "Can not find verification data group information.";
                            return false;
                        }
                        m = THNRgx.r32c.Match(s);
                        if (!m.Success)
                        {
                            msg = "Can not find verification data group information.";
                            return false;
                        }
                        if (ivf == 1)
                        {
                            if (m.Groups[1].Captures.Count != 0)
                            {
                                if (m.Groups[2].Captures.Count != 0)
                                {
                                    msg = "Incorrect verification data group information.";
                                    return false;
                                }
                                vdg = new int[m.Groups[1].Captures.Count];
                                for (cnt2 = 0; cnt2 < vdg.Length; cnt2++)
                                {
                                    flg = int.TryParse(m.Groups[1].Captures[cnt2].Value.Trim(), out vdg[cnt2]);
                                    if (!flg)
                                    {
                                        msg = "Incorrect verification data group information.";
                                        return false;
                                    }
                                }
                                resgi[cnt] = vdg;
                            }
                            else if (m.Groups[2].Captures.Count == 1)
                            {
                                if (m.Groups[1].Captures.Count != 0)
                                {
                                    msg = "Incorrect verification data group information.";
                                    return false;
                                }
                                resgi[cnt] = new int[1] { -1 };
                            }
                            else
                            {
                                msg = "Incorrect verification data group information.";
                                return false;
                            }
                        }
                        else
                        {
                            if (m.Groups[2].Captures.Count != 0)
                            {
                                msg = "Incorrect verification data group information.";
                                return false;
                            }
                            vdg = new int[m.Groups[1].Captures.Count];
                            for (cnt2 = 0; cnt2 < vdg.Length; cnt2++)
                            {
                                flg = int.TryParse(m.Groups[1].Captures[cnt2].Value.Trim(), out vdg[cnt2]);
                                if (!flg)
                                {
                                    msg = "Incorrect verification data group information.";
                                    return false;
                                }
                            }
                            resgi[cnt] = vdg;
                        }
                    }
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = "Can not find learning epoch information.";
                            return false;
                        }
                    }
                    while (s.Trim() == string.Empty);
                    m = THNRgx.r23a.Match(s);
                    if (!m.Success)
                    {
                        msg = "Can not find learning epoch information.";
                        return false;
                    }
                    flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out NK);
                    if (!flg)
                    {
                        msg = "Incorrect verification data group information.";
                        return false;
                    }
                    s = sr.ReadLine();
                    if (s == null)
                    {
                        msg = "Can not find learning rate information.";
                        return false;
                    }
                    m = THNRgx.r24a.Match(s);
                    if (!m.Success)
                    {
                        msg = "Can not find learning rate information.";
                        return false;
                    }
                    flg = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out G);
                    if (!flg)
                    {
                        msg = "Invalid learning rate.";
                        return false;
                    }
                    s = sr.ReadLine();
                    if (s == null)
                    {
                        msg = "Can not find stochastic descendant probability information.";
                        return false;
                    }
                    m = THNRgx.r57.Match(s);
                    if (!m.Success)
                    {
                        msg = "Can not find stochastic descendant probability information.";
                        return false;
                    }
                    flg = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out KRK);
                    if (!flg || KRK < 0.0 || KRK > 1.0)
                    {
                        msg = "Invalid stochastic descendant probability.";
                        return false;
                    }
                    s = sr.ReadLine();
                    if (s == null)
                    {
                        msg = "Can not find normalization method information.";
                        return false;
                    }
                    m = THNRgx.r44.Match(s);
                    if (!m.Success)
                    {
                        msg = "Can not find normalization method information.";
                        return false;
                    }
                    flg = byte.TryParse(m.Groups[1].Captures[0].Value.Trim(), out thnn);
                    if (!flg)
                    {
                        msg = "Invalid normalization method.";
                        return false;
                    }
                    if (thnn > 2)
                    {
                        msg = "Unexpected Touhou Network normalization type.";
                        return false;
                    }
                }
            }
            return true;
        }
        static internal bool THNHset(in string rwp, in int[] Lyn, in int[][] LNn, in Tuple<int, int>[][] Tcn, in double[][][] Fwp, in int[][][] Nfp)//東方ネットワークハイパーパラメーター、rwpはジョブディレクトリ、Lynは層数範囲、LNnは各層ノード数、Tcnは層の連結数範囲、Fwpは超層連結率、Nfpは関数確率
        {
            if (Lyn == null || LNn == null || Tcn == null || Fwp == null || Nfp == null || Lyn.Length != LNn.Length || LNn.Length != Tcn.Length || Tcn.Length != Fwp.Length || Fwp.Length != Nfp.Length || Lyn.Length == 0) return false;
            if (!Directory.Exists(rwp))
            {
                Directory.CreateDirectory(rwp);
            }
            DirectoryInfo di = new DirectoryInfo(rwp);
            if ((di.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly || (di.Attributes & FileAttributes.System) == FileAttributes.System) return false;
            StringBuilder sb = new StringBuilder(rwp);
            if (!rwp.EndsWith('\\')) sb.Append('\\');
            sb.Append("CCS.RChyp");
            string CCSf = sb.ToString();
            bool fhf = false;
            bool frf = false;
            if (!File.Exists(CCSf)) using (File.Create(CCSf)) { }
            else
            {
                if ((File.GetAttributes(CCSf) & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    File.SetAttributes(CCSf, File.GetAttributes(CCSf) & ~FileAttributes.Hidden);
                    fhf = true;
                }
                if ((File.GetAttributes(CCSf) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(CCSf, File.GetAttributes(CCSf) & ~FileAttributes.ReadOnly);
                    frf = true;
                }
            }
            sb = new StringBuilder();
            int cnt, cnt2, cnt3, itemp, itemp2, inn;
            for (cnt = 0; cnt < Lyn.Length; cnt++)
            {
                if (Lyn[cnt] < 2 || Lyn[cnt] > 11) return false;
                itemp = Lyn[cnt] - 1;
                inn = itemp - 1;
                if (LNn[cnt] == null || LNn[cnt].Length != inn) return false;
                if (Tcn[cnt] == null || Tcn[cnt].Length != itemp) return false;
                if (Fwp[cnt] == null || Fwp[cnt].Length != itemp) return false;
                if (Nfp[cnt] == null || Nfp[cnt].Length != Lyn[cnt]) return false;
                sb.Append("Layer number : ");
                sb.AppendLine(Lyn[cnt].ToString("G"));
                sb.AppendLine();
                sb.Append("層インデックス : ");
                sb.AppendLine(0.ToString("G"));
                sb.Append("函数概率：");
                if (Nfp[cnt][0] == null) sb.AppendLine("(R)");
                else
                {
                    sb.Append("(");
                    for (cnt2 = 0; cnt2 < Nfp[cnt][0].GetUpperBound(0); cnt2++)
                    {
                        sb.Append(Nfp[cnt][0][cnt2].ToString("G"));
                        sb.Append(", ");
                    }
                    sb.Append(Nfp[cnt][0][^1].ToString("G"));
                    sb.AppendLine(")");
                }
                sb.AppendLine();
                for (cnt2 = 0; cnt2 < itemp; cnt2++)
                {
                    itemp2 = cnt2 + 1;
                    if (Fwp[cnt][cnt2] == null || Fwp[cnt][cnt2].Length != itemp2) return false;
                    sb.Append("層インデックス : ");
                    sb.AppendLine(itemp2.ToString("G"));
                    if (cnt2 != inn)
                    {
                        sb.Append("节点数：");
                        sb.AppendLine(LNn[cnt][cnt2].ToString("G"));
                    }
                    sb.Append("Layer link range : ");
                    sb.Append(Tcn[cnt][cnt2].Item1.ToString("G"));
                    sb.Append(", ");
                    sb.AppendLine(Tcn[cnt][cnt2].Item2.ToString("G"));
                    sb.Append("超層連結率：");
                    for (cnt3 = 0; cnt3 < cnt2; cnt3++)
                    {
                        sb.Append(Fwp[cnt][cnt2][cnt3].ToString("G15"));
                        sb.Append(", ");
                    }
                    sb.AppendLine(Fwp[cnt][cnt2][cnt2].ToString("G15"));
                    sb.Append("函数概率：");
                    if (Nfp[cnt][itemp2] == null) sb.AppendLine("(R)");
                    else
                    {
                        sb.Append("(");
                        for (cnt3 = 0; cnt3 < Nfp[cnt][itemp2].GetUpperBound(0); cnt3++)
                        {
                            sb.Append(Nfp[cnt][itemp2][cnt3].ToString("G"));
                            sb.Append(", ");
                        }
                        sb.Append(Nfp[cnt][itemp2][^1].ToString("G"));
                        sb.AppendLine(")");
                    }
                    sb.AppendLine();
                }
                if (cnt != Lyn.GetUpperBound(0))
                {
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine();
                }
            }
            sb.Append(DateTime.Now.ToString("F"));
            using (FileStream fs = new FileStream(CCSf, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 128))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.Unicode, 128, false))
                {
                    sw.Write(sb.ToString());
                }
            }
            if (fhf)
            {
                File.SetAttributes(CCSf, File.GetAttributes(CCSf) | FileAttributes.Hidden);
            }
            if (frf)
            {
                File.SetAttributes(CCSf, File.GetAttributes(CCSf) | FileAttributes.ReadOnly);
            }
            return true;
        }
        static internal bool THNHR(out string msg, in string rwp, out int[] Lyn, out int[][] LNn, out Tuple<int, int>[][] Tcn, out double[][][] Fwp, out int[][][] Nfp)//東方ネットワークハイパーパラメーター、rwpはジョブディレクトリ、Lynは層数範囲、LNnは各層ノード数、Tcnは層の連結数範囲、Fwpは超層連結率、Nfpは関数確率
        {
            Lyn = null;
            LNn = null;
            Tcn = null;
            Fwp = null;
            Nfp = null;
            msg = "Unexpected termination";
            StringBuilder sb = new StringBuilder(rwp);
            if (!rwp.EndsWith('\\')) sb.Append('\\');
            sb.Append("CCS.RChyp");
            string CCSf = sb.ToString();
            string s;
            Match m;
            bool flg;
            int itemp, itemp2, cnt, cnt2;
            int ivf = -1;
            double dtemp;
            List<int> Lynl = new List<int>();
            List<int[]> LNnl = new List<int[]>();
            List<Tuple<int, int>[]> Tcnl = new List<Tuple<int, int>[]>();
            List<double[][]> Fwpl = new List<double[][]>();
            List<int[][]> Nfpl = new List<int[][]>();
            int[] LNna;
            Tuple<int, int>[] Tcna;
            double[][] Fwpa;
            double[] Fwpat;
            int[][] Nfpa;
            int[] Nfpat;
            using (FileStream fs = new FileStream(CCSf, FileMode.Open, FileAccess.Read, FileShare.None, 128))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.Unicode, true, 128, false))
                {
                    while (true)
                    {
                        do
                        {
                            s = sr.ReadLine();
                            if (s == null)
                            {
                                msg = "Can not find layer number information.";
                                return false;
                            }
                        }
                        while (s.Trim() == string.Empty);
                        ivf++;
                        m = THNRgx.r13a.Match(s);
                        if (!m.Success)
                        {
                            DateTime dtt;
                            flg = DateTime.TryParse(s, out dtt);
                            if (flg) break;
                            msg = "Can not find layer number information.";
                            return false;
                        }
                        flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                        if (!flg)
                        {
                            msg = "Invalid layer number.";
                            return false;
                        }
                        Lynl.Add(itemp);
                        itemp2 = itemp - 1;
                        LNna = new int[itemp2 - 1];
                        Tcna = new Tuple<int, int>[itemp2];
                        Fwpa = new double[itemp2][];
                        Nfpa = new int[itemp][];
                        do
                        {
                            s = sr.ReadLine();
                            if (s == null)
                            {
                                msg = "Can not find layer index.";
                                return false;
                            }
                        }
                        while (s.Trim() == string.Empty);
                        m = THNRgx.r13b.Match(s);
                        if (!m.Success)
                        {
                            msg = "Can not find layer index.";
                            return false;
                        }
                        flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                        if (!flg || itemp != 0)
                        {
                            msg = "Invalid layer index.";
                            return false;
                        }
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = "Can not find function probability.";
                            return false;
                        }
                        m = THNRgx.r45.Match(s);
                        if (!m.Success)
                        {
                            msg = "Can not find function probability.";
                            return false;
                        }
                        if (m.Groups[1].Captures.Count != 1 && m.Groups[1].Captures.Count != THFunc.GenSouKyou.FncN)
                        {
                            msg = "Invalid function probability.";
                            return false;
                        }
                        Nfpat = new int[m.Groups[1].Captures.Count];
                        for (cnt = 0; cnt < Nfpat.Length; cnt++)
                        {
                            flg = int.TryParse(m.Groups[1].Captures[cnt].Value.Trim(), out itemp);
                            if (!flg)
                            {
                                msg = "Invalid function probability.";
                                return false;
                            }
                            if (itemp < 0.0)
                            {
                                if (itemp == -1.0 && Nfpat.Length == 1)
                                {
                                    Nfpat = null;
                                    break;
                                }
                                else
                                {
                                    msg = "Invalid function probability.";
                                    return false;
                                }
                            }
                            Nfpat[cnt] = itemp;
                        }
                        Nfpa[0] = Nfpat;
                        for (cnt = 0; cnt < Tcna.Length; cnt++)
                        {
                            itemp2 = cnt + 1;
                            do { s = sr.ReadLine(); } while (s.Trim() == string.Empty);
                            if (s == null)
                            {
                                msg = "Can not find layer index.";
                                return false;
                            }
                            m = THNRgx.r13b.Match(s);
                            if (!m.Success)
                            {
                                msg = "Can not find layer index.";
                                return false;
                            }
                            flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                            if (itemp != itemp2)
                            {
                                msg = "Incorrect layer index.";
                                return false;
                            }
                            if (itemp != Tcna.Length)
                            {
                                s = sr.ReadLine();
                                if (s == null)
                                {
                                    msg = "Can not find node number.";
                                    return false;
                                }
                                m = THNRgx.r13c.Match(s);
                                if (!m.Success)
                                {
                                    msg = "Can not find node number.";
                                    return false;
                                }
                                flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                                if (!flg || itemp <= 0)
                                {
                                    msg = "Invalid node number.";
                                    return false;
                                }
                                LNna[cnt] = itemp;
                            }
                            s = sr.ReadLine();
                            if (s == null)
                            {
                                msg = "Can not find layer link range.";
                                return false;
                            }
                            m = THNRgx.r9a.Match(s);
                            if (!m.Success)
                            {
                                msg = "Can not find layer link range.";
                                return false;
                            }
                            flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                            if (!flg || itemp <= 0)
                            {
                                msg = "Invalid layer link range start.";
                                return false;
                            }
                            flg = int.TryParse(m.Groups[2].Captures[0].Value.Trim(), out cnt2);
                            if (!flg || cnt2 < itemp)
                            {
                                msg = "Invalid layer link range end.";
                                return false;
                            }
                            Tcna[cnt] = new Tuple<int, int>(itemp, cnt2);
                            s = sr.ReadLine();
                            if (s == null)
                            {
                                msg = "Can not find superlayer link probability.";
                                return false;
                            }
                            m = THNRgx.r10a.Match(s);
                            if (!m.Success)
                            {
                                msg = "Can not find superlayer link probability.";
                                return false;
                            }
                            Fwpat = new double[itemp2];
                            for (cnt2 = 0; cnt2 < itemp2; cnt2++)
                            {
                                flg = double.TryParse(m.Groups[1].Captures[cnt2].Value.Trim(), out dtemp);
                                if (!flg || dtemp < 0.0)
                                {
                                    msg = "Invalid superlayer link probability.";
                                    return false;
                                }
                                Fwpat[cnt2] = dtemp;
                            }
                            Fwpa[cnt] = Fwpat;
                            s = sr.ReadLine();
                            if (s == null)
                            {
                                msg = "Can not find function probability.";
                                return false;
                            }
                            m = THNRgx.r45.Match(s);
                            if (!m.Success)
                            {
                                msg = "Can not find function probability.";
                                return false;
                            }
                            if (m.Groups[1].Captures.Count != 1 && m.Groups[1].Captures.Count != THFunc.GenSouKyou.FncN)
                            {
                                msg = "Invalid function probability.";
                                return false;
                            }
                            Nfpat = new int[m.Groups[1].Captures.Count];
                            for (cnt2 = 0; cnt2 < Nfpat.Length; cnt2++)
                            {
                                flg = int.TryParse(m.Groups[1].Captures[cnt2].Value.Trim(), out itemp);
                                if (!flg)
                                {
                                    msg = "Invalid function probability.";
                                    return false;
                                }
                                if (itemp < 0.0)
                                {
                                    if (itemp == -1.0 && Nfpat.Length == 1)
                                    {
                                        Nfpat = null;
                                        break;
                                    }
                                    else
                                    {
                                        msg = "Invalid function probability.";
                                        return false;
                                    }
                                }
                                Nfpat[cnt2] = itemp;
                            }
                            Nfpa[itemp2] = Nfpat;
                        }
                        LNnl.Add(LNna);
                        Tcnl.Add(Tcna);
                        Fwpl.Add(Fwpa);
                        Nfpl.Add(Nfpa);
                    }
                }
            }
            Lyn = Lynl.ToArray();
            LNn = LNnl.ToArray();
            Tcn = Tcnl.ToArray();
            Fwp = Fwpl.ToArray();
            Nfp = Nfpl.ToArray();
            if (Lyn.Length != ivf || LNn.Length != ivf || Tcn.Length != ivf || Fwp.Length != ivf || Nfp.Length != ivf)
            {
                msg = "Can not verify information amount.";
                return false;
            }
            msg = "Successfully read.";
            return true;
        }
        static internal bool THNJRR(out string msg, in string fpth, out int[] Nn, out int[][] Nf, out int[][][] lila, out int[][][] lina, out int[][][] lola, out int[][][] lona, out double[][][] Nrk, out int Dtn, out int[] Di, out double[] Dc, out double[] Dn, out bool[] ConVal, out double[][] Stat, out int[] Dti, out int XXC)//東方ネットワークログファイルを読み込む、msgはメッセージ、fpthはRClogファイルパス、Nnは各層ノード数、Nfはノード関数、lilaは入力層インデックス、linaは入力ノードインデックス、lolaは入力層インデックス、lonaは入力ノードインデックス、Nrkは入力係数、Dtnはデータ数、Diはデータインデックス、Dcはデータ中心化係数、Dnは正規化スケール、ConVal0は収束状況1は検証状況、Statは統計量、Dtiはテストデータインデックス、XXCは実際学習回数
        {
            Nn = null;
            Nf = null;
            lila = null;
            lina = null;
            lola = null;
            lona = null;
            Nrk = null;
            Dtn = 0;
            Di = null;
            Dc = null;
            Dn = null;
            ConVal = null;
            Stat = null;
            Dti = null;
            XXC = -1;
            bool flg;
            int cnt, cnt2, cnt3, itemp;
            Match m;
            string s;
            double dtemp;
            if (!File.Exists(fpth))
            {
                msg = "Logfile not found.";
                return false;
            }
            using (FileStream fs = new FileStream(fpth, FileMode.Open, FileAccess.Read, FileShare.None, 128))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.Unicode, true, 128, false))
                {
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = string.Format("{0} : Can not find descriptor index information.", fpth);
                            return false;
                        }
                        m = THNRgx.r3.Match(s);
                    }
                    while (!m.Success);
                    flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                    if (!flg || itemp != m.Groups[2].Captures.Count)
                    {
                        msg = string.Format("{0} : Invalid descriptor index information.", fpth);
                        return false;
                    }
                    Di = new int[itemp];
                    Dc = new double[itemp];
                    Dn = new double[itemp];
                    for (cnt = 0; cnt < Di.Length; cnt++)
                    {
                        flg = int.TryParse(m.Groups[2].Captures[cnt].Value.Trim(), out itemp);
                        if (!flg || itemp < 0)
                        {
                            msg = string.Format("{0}: Invalid descriptor index.", fpth);
                            return false;
                        }
                        Di[cnt] = itemp;
                    }
                    s = sr.ReadLine();
                    m = THNRgx.r4.Match(s ?? string.Empty);
                    if (s == null || !m.Success)
                    {
                        msg = string.Format("{0}: Invalid descriptor centralization constant information.", fpth);
                        return false;
                    }
                    flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                    if (!flg || itemp != m.Groups[2].Captures.Count || itemp != Dc.Length)
                    {
                        msg = string.Format("{0} : Invalid descriptor centralization constant information.", fpth);
                        return false;
                    }
                    for (cnt = 0; cnt < Dc.Length; cnt++)
                    {
                        flg = double.TryParse(m.Groups[2].Captures[cnt].Value.Trim(), out dtemp);
                        if (!flg)
                        {
                            msg = string.Format("{0}: Invalid descriptor centralization constant.", fpth);
                            return false;
                        }
                        Dc[cnt] = dtemp;
                    }
                    s = sr.ReadLine();
                    m = THNRgx.r5.Match(s ?? string.Empty);
                    if (s == null || !m.Success)
                    {
                        msg = string.Format("{0}: Invalid descriptor scaling fator information.", fpth);
                        return false;
                    }
                    flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                    if (!flg || itemp != m.Groups[2].Captures.Count || itemp != Dn.Length)
                    {
                        msg = string.Format("{0} : Invalid descriptor scaling fator information.", fpth);
                        return false;
                    }
                    for (cnt = 0; cnt < Dn.Length; cnt++)
                    {
                        flg = double.TryParse(m.Groups[2].Captures[cnt].Value.Trim(), out dtemp);
                        if (!flg)
                        {
                            msg = string.Format("{0}: Invalid descriptor scaling fator.", fpth);
                            return false;
                        }
                        Dn[cnt] = dtemp;
                    }
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = string.Format("{0} : Can not find Touhou Network structure title.", fpth);
                            return false;
                        }
                        m = THNRgx.r12.Match(s);
                    }
                    while (!m.Success);
                    s = sr.ReadLine();
                    m = THNRgx.r13.Match(s ?? string.Empty);
                    if (s == null || !m.Success)
                    {
                        msg = string.Format("{0} : Can not find Touhou Network layer infromation.", fpth);
                        return false;
                    }
                    flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                    if (!flg)
                    {
                        msg = string.Format("{0} : Invalid Touhou Network layer infromation.", fpth);
                        return false;
                    }
                    Nn = new int[itemp];
                    Nf = new int[itemp][];
                    lina = new int[itemp][][];
                    lila = new int[itemp][][];
                    lona = new int[itemp][][];
                    lola = new int[itemp][][];
                    Nrk = new double[itemp][][];
                    if (itemp != m.Groups[2].Captures.Count || itemp != Nn.Length)
                    {
                        msg = string.Format("{0} : Can not verify Touhou Network layer number.", fpth);
                        return false;
                    }
                    for (cnt = 0; cnt < Nn.Length; cnt++)
                    {
                        flg = int.TryParse(m.Groups[2].Captures[cnt].Value.Trim(), out itemp);
                        if (!flg || itemp <= 0)
                        {
                            msg = string.Format("{0}: Invalid Touhou Network node number.", fpth);
                            return false;
                        }
                        Nn[cnt] = itemp;
                        Nf[cnt] = new int[itemp];
                        lina[cnt] = new int[itemp][];
                        lila[cnt] = new int[itemp][];
                        lona[cnt] = new int[itemp][];
                        lola[cnt] = new int[itemp][];
                        Nrk[cnt] = new double[itemp][];
                    }
                    Stat = new double[Nn[Nn.GetUpperBound(0)]][];
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = string.Format("{0} : Can not find data amount.", fpth);
                            return false;
                        }
                        m = THNRgx.r14.Match(s);
                    }
                    while (!m.Success);
                    flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                    if (!flg || itemp <= 0)
                    {
                        msg = string.Format("{0}: Invalid data amount.", fpth);
                        return false;
                    }
                    Dtn = itemp;
                    for (cnt = 0; cnt < Nn.Length; cnt++)
                    {
                        do
                        {
                            s = sr.ReadLine();
                            if (s == null)
                            {
                                msg = string.Format("{0} : Can not find Touhou Network layer{1} infromation.", fpth, cnt);
                                return false;
                            }
                            m = THNRgx.r15.Match(s);
                        }
                        while (!m.Success);
                        flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                        if (!flg || itemp != cnt)
                        {
                            msg = string.Format("{0} : Invalid Touhou Network layer{1} infromation.", fpth, cnt);
                            return false;
                        }
                        flg = int.TryParse(m.Groups[2].Captures[0].Value.Trim(), out itemp);
                        if (!flg || itemp != Nn[cnt])
                        {
                            msg = string.Format("{0} : Invalid Touhou Network layer{1} node number.", fpth, cnt);
                            return false;
                        }
                        for (cnt2 = 0; cnt2 < Nn[cnt]; cnt2++)
                        {
                            s = sr.ReadLine();
                            itemp = THFunc.THFStB(s);
                            if (itemp < 0 || itemp > THFunc.GenSouKyou.FncN)
                            {
                                msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} function.", fpth, cnt, cnt2);
                                return false;
                            }
                            Nf[cnt][cnt2] = itemp;
                            if (cnt == 0)
                            {
                                s = sr.ReadLine();
                                m = THNRgx.r16a.Match(s);
                                if (!m.Success || !m.Groups[1].Captures[0].Value.Equals("入", StringComparison.InvariantCulture))
                                {
                                    msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} input layer information.", fpth, cnt, cnt2);
                                    return false;
                                }
                                lila[cnt][cnt2] = null;
                                s = sr.ReadLine();
                                m = THNRgx.r17a.Match(s);
                                if (!m.Success || !m.Groups[1].Captures[0].Value.Equals("入", StringComparison.InvariantCulture))
                                {
                                    msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} input node information.", fpth, cnt, cnt2);
                                    return false;
                                }
                                lina[cnt][cnt2] = null;
                                s = sr.ReadLine();
                                m = THNRgx.r18a.Match(s);
                                if (!m.Success)
                                {
                                    msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} input coefficient information.", fpth, cnt, cnt2);
                                    return false;
                                }
                                Nrk[cnt][cnt2] = null;
                            }
                            else
                            {
                                s = sr.ReadLine();
                                m = THNRgx.r16.Match(s);
                                if (!m.Success || !m.Groups[1].Captures[0].Value.Equals("入", StringComparison.InvariantCulture))
                                {
                                    msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} input layer information.", fpth, cnt, cnt2);
                                    return false;
                                }
                                flg = int.TryParse(m.Groups[2].Captures[0].Value.Trim(), out itemp);
                                if (!flg || itemp < 0 || itemp != m.Groups[3].Captures.Count)
                                {
                                    msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} input layer information.", fpth, cnt, cnt2);
                                    return false;
                                }
                                lila[cnt][cnt2] = new int[itemp];
                                lina[cnt][cnt2] = new int[itemp];
                                Nrk[cnt][cnt2] = new double[itemp];
                                for (cnt3 = 0; cnt3 < lila[cnt][cnt2].Length; cnt3++)
                                {
                                    flg = int.TryParse(m.Groups[3].Captures[cnt3].Value.Trim(), out itemp);
                                    if (!flg || itemp < 0 || itemp >= cnt)
                                    {
                                        msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} input layer information.", fpth, cnt, cnt2);
                                        return false;
                                    }
                                    lila[cnt][cnt2][cnt3] = itemp;
                                }
                                s = sr.ReadLine();
                                m = THNRgx.r17.Match(s);
                                if (!m.Success || !m.Groups[1].Captures[0].Value.Equals("入", StringComparison.InvariantCulture))
                                {
                                    msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} input node information.", fpth, cnt, cnt2);
                                    return false;
                                }
                                flg = int.TryParse(m.Groups[2].Captures[0].Value.Trim(), out itemp);
                                if (!flg || itemp != m.Groups[3].Captures.Count || itemp != lila[cnt][cnt2].Length)
                                {
                                    msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} input node information.", fpth, cnt, cnt2);
                                    return false;
                                }
                                for (cnt3 = 0; cnt3 < lina[cnt][cnt2].Length; cnt3++)
                                {
                                    flg = int.TryParse(m.Groups[3].Captures[cnt3].Value.Trim(), out itemp);
                                    if (!flg || itemp < 0 || itemp >= Nn[lila[cnt][cnt2][cnt3]])
                                    {
                                        msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} input node information.", fpth, cnt, cnt2);
                                        return false;
                                    }
                                    lina[cnt][cnt2][cnt3] = itemp;
                                }
                                s = sr.ReadLine();
                                m = THNRgx.r18.Match(s);
                                if (!m.Success)
                                {
                                    msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} input coefficient information.", fpth, cnt, cnt2);
                                    return false;
                                }
                                flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                                if (!flg || itemp != m.Groups[2].Captures.Count || itemp != Nrk[cnt][cnt2].Length)
                                {
                                    msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} input coefficient information.", fpth, cnt, cnt2);
                                    return false;
                                }
                                for (cnt3 = 0; cnt3 < Nrk[cnt][cnt2].Length; cnt3++)
                                {
                                    flg = double.TryParse(m.Groups[2].Captures[cnt3].Value.Trim(), out dtemp);
                                    if (!flg || double.IsNaN(dtemp))
                                    {
                                        msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} input coefficient information.", fpth, cnt, cnt2);
                                        return false;
                                    }
                                    Nrk[cnt][cnt2][cnt3] = dtemp;
                                }
                            }
                            if (cnt == Nn.GetUpperBound(0))
                            {
                                s = sr.ReadLine();
                                m = THNRgx.r16a.Match(s);
                                if (!m.Success || !m.Groups[1].Captures[0].Value.Equals("出", StringComparison.InvariantCulture))
                                {
                                    msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} output layer information.", fpth, cnt, cnt2);
                                    return false;
                                }
                                lola[cnt][cnt2] = null;
                                s = sr.ReadLine();
                                m = THNRgx.r17a.Match(s);
                                if (!m.Success || !m.Groups[1].Captures[0].Value.Equals("出", StringComparison.InvariantCulture))
                                {
                                    msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} output node information.", fpth, cnt, cnt2);
                                    return false;
                                }
                                lona[cnt][cnt2] = null;
                            }
                            else
                            {
                                s = sr.ReadLine();
                                m = THNRgx.r16.Match(s);
                                if (!m.Success || !m.Groups[1].Captures[0].Value.Equals("出", StringComparison.InvariantCulture))
                                {
                                    msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} output layer information.", fpth, cnt, cnt2);
                                    return false;
                                }
                                flg = int.TryParse(m.Groups[2].Captures[0].Value.Trim(), out itemp);
                                lola[cnt][cnt2] = new int[itemp];
                                lona[cnt][cnt2] = new int[itemp];
                                if (!flg || itemp < 0 || itemp != m.Groups[3].Captures.Count)
                                {
                                    msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} output layer information.", fpth, cnt, cnt2);
                                    return false;
                                }
                                for (cnt3 = 0; cnt3 < lola[cnt][cnt2].Length; cnt3++)
                                {
                                    flg = int.TryParse(m.Groups[3].Captures[cnt3].Value.Trim(), out itemp);
                                    if (!flg || itemp < 0 || itemp <= cnt)
                                    {
                                        msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} output layer information.", fpth, cnt, cnt2);
                                        return false;
                                    }
                                    lola[cnt][cnt2][cnt3] = itemp;
                                }
                                s = sr.ReadLine();
                                m = THNRgx.r17.Match(s);
                                if (!m.Success || !m.Groups[1].Captures[0].Value.Equals("出", StringComparison.InvariantCulture))
                                {
                                    msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} output node information.", fpth, cnt, cnt2);
                                    return false;
                                }
                                flg = int.TryParse(m.Groups[2].Captures[0].Value.Trim(), out itemp);
                                if (!flg || itemp != m.Groups[3].Captures.Count || itemp != lona[cnt][cnt2].Length)
                                {
                                    msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} output node information.", fpth, cnt, cnt2);
                                    return false;
                                }
                                for (cnt3 = 0; cnt3 < lona[cnt][cnt2].Length; cnt3++)
                                {
                                    flg = int.TryParse(m.Groups[3].Captures[cnt3].Value.Trim(), out itemp);
                                    if (!flg || itemp < 0 || itemp >= Nn[lola[cnt][cnt2][cnt3]])
                                    {
                                        msg = string.Format("{0} : Invalid Touhou Network layer{1} node{2} output layer information.", fpth, cnt, cnt2);
                                        return false;
                                    }
                                    lona[cnt][cnt2][cnt3] = itemp;
                                }
                            }
                        }
                    }
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = string.Format("{0} : Can not find Touhou Network learning information.", fpth);
                            return false;
                        }
                        m = THNRgx.r19.Match(s);
                    }
                    while (!m.Success);
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = string.Format("{0} : Can not find Touhou Network convergence information.", fpth);
                            return false;
                        }
                        m = THNRgx.r25.Match(s);
                    }
                    while (!m.Success);
                    ConVal = new bool[2];
                    ConVal[0] = false;
                    ConVal[1] = false;
                    flg = bool.TryParse(m.Groups[1].Captures[0].Value.Trim(), out ConVal[0]);
                    if (!flg)
                    {
                        msg = string.Format("{0} : Invalid Touhou Network convergence information.", fpth);
                        return false;
                    }
                    if (!ConVal[0])
                    {
                        msg = "{0} : Successfully read.";
                        return true;
                    }
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = string.Format("{0} : Can not find Touhou Network actual leaning epoch.", fpth);
                            return false;
                        }
                        m = THNRgx.r26.Match(s);
                    }
                    while (!m.Success);
                    flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out XXC);
                    if (!flg || XXC < 0)
                    {
                        msg = string.Format("{0} : Invalid Touhou Network actual leaning epoch.", fpth);
                        return false;
                    }
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = string.Format("{0} : Can not find Touhou Network validation infromation.", fpth);
                            return false;
                        }
                        m = THNRgx.r31.Match(s);
                    }
                    while (!m.Success);
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = string.Format("{0} : Can not find Touhou Network validation data index.", fpth);
                            return false;
                        }
                        m = THNRgx.r32.Match(s);
                    }
                    while (!m.Success);
                    flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                    if (!flg || itemp <= 0 || itemp != m.Groups[2].Captures.Count)
                    {
                        msg = string.Format("{0} : Incorrect validation index information.", fpth);
                        return false;
                    }
                    Dti = new int[itemp];
                    for (cnt = 0; cnt < Dti.Length; cnt++)
                    {
                        flg = int.TryParse(m.Groups[2].Captures[cnt].Value.Trim(), out itemp);
                        if (!flg || itemp < 0)
                        {
                            msg = string.Format("{0} : Invalid validation index.", fpth);
                            return false;
                        }
                        Dti[cnt] = itemp;
                    }
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = string.Format("{0} : Can not find Touhou Network validation result.", fpth);
                            return false;
                        }
                        m = THNRgx.r33.Match(s);
                    }
                    while (!m.Success);
                    flg = bool.TryParse(m.Groups[1].Captures[0].Value.Trim(), out ConVal[1]);
                    if (!flg)
                    {
                        msg = string.Format("{0} : Invalid Touhou Network validation result.", fpth);
                        return false;
                    }
                    if (ConVal[1])
                    {
                        //マジすか？すげぇぇぇ！
                    }
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = string.Format("{0} : Can not find Touhou Network validation statistical amount.", fpth);
                            return false;
                        }
                        m = THNRgx.r34.Match(s);
                    }
                    while (!m.Success);
                    Stat = new double[Nn[^1]][];
                    for (cnt = 0; cnt < Stat.Length; cnt++)
                    {
                        do
                        {
                            s = sr.ReadLine();
                            if (s == null)
                            {
                                msg = string.Format("{0} : Can not find Touhou Network validation statistical amount of node{1}.", fpth, cnt);
                                return false;
                            }
                            m = THNRgx.r35.Match(s);
                        }
                        while (!m.Success);
                        flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                        if (!flg || itemp != cnt)
                        {
                            msg = string.Format("{0} : Can not find Touhou Network validation statistical amount of node{1}.", fpth, cnt);
                            return false;
                        }
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = string.Format("{0} : Can not find Touhou Network validation statistical amount of node{1}.", fpth, cnt);
                            return false;
                        }
                        m = THNRgx.r36.Match(s);
                        if (!m.Success)
                        {
                            msg = string.Format("{0} : Invalid Touhou Network validation statistical amount of node{1}.", fpth, cnt);
                            return false;
                        }
                        Stat[0] = new double[8];
                        flg = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out Stat[cnt][0]);
                        if (!flg || Stat[cnt][0] < 0.0)
                        {
                            msg = string.Format("{0} : Invalid Touhou Network validation EAbs of node{1}.", fpth, cnt);
                            return false;
                        }
                        flg = double.TryParse(m.Groups[2].Captures[0].Value.Trim(), out Stat[cnt][1]);
                        if (!flg || Stat[cnt][1] < 0.0)
                        {
                            msg = string.Format("{0} : Invalid Touhou Network validation E2 of node{1}.", fpth, cnt);
                            return false;
                        }
                        flg = double.TryParse(m.Groups[3].Captures[0].Value.Trim(), out Stat[cnt][2]);
                        if (!flg || Stat[cnt][2] < -1.0 || Stat[cnt][2] > 1.0)
                        {
                            msg = string.Format("{0} : Invalid Touhou Network validation cos(ŷ·y) of node{1}.", fpth, cnt);
                            return false;
                        }
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = string.Format("{0} : Can not find Touhou Network validation statistical amount of node{1}.", fpth, cnt);
                            return false;
                        }
                        m = THNRgx.r37.Match(s);
                        if (!m.Success)
                        {
                            msg = string.Format("{0} : Invalid Touhou Network validation statistical amount of node{1}.", fpth, cnt);
                            return false;
                        }
                        flg = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out Stat[cnt][3]);
                        if (!flg || Stat[cnt][3] < 0.0)
                        {
                            msg = string.Format("{0} : Invalid Touhou Network validation R28 of node{1}.", fpth, cnt);
                            return false;
                        }
                        flg = double.TryParse(m.Groups[2].Captures[0].Value.Trim(), out Stat[cnt][4]);
                        if (!flg || Stat[cnt][4] > 1.0)
                        {
                            msg = string.Format("{0} : Invalid Touhou Network validation R27 of node{1}.", fpth, cnt);
                            return false;
                        }
                        flg = double.TryParse(m.Groups[3].Captures[0].Value.Trim(), out Stat[cnt][5]);
                        if (!flg || Stat[cnt][5] > 1.0)
                        {
                            msg = string.Format("{0} : Invalid Touhou Network validation R21 of node{1}.", fpth, cnt);
                            return false;
                        }
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = string.Format("{0} : Can not find Touhou Network validation statistical amount of node{1}.", fpth, cnt);
                            return false;
                        }
                        m = THNRgx.r38.Match(s);
                        if (!m.Success)
                        {
                            msg = string.Format("{0} : Invalid Touhou Network validation statistical amount of node{1}.", fpth, cnt);
                            return false;
                        }
                        flg = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out Stat[cnt][6]);
                        if (!flg || Stat[cnt][6] < -1.0 || Stat[cnt][6] > 1.0)
                        {
                            msg = string.Format("{0} : Invalid Touhou Network validation r of node{1}.", fpth, cnt);
                            return false;
                        }
                        flg = double.TryParse(m.Groups[2].Captures[0].Value.Trim(), out Stat[cnt][7]);
                        if (!flg || Stat[cnt][6] < 0.0)
                        {
                            msg = string.Format("{0} : Invalid Touhou Network validation R23 of node{1}.", fpth, cnt);
                            return false;
                        }
                    }
                }
            }
            msg = "Successfully read.";
            return true;
        }
        static internal bool THNMtjgx(out string msg, in DirectoryInfo di, in bool[] ht, byte ut, double[] rr, double[] uw, in double[] LtP)//東方ネットワークモデル集計/(ハイパーパラメータ)更新、diはジョブフォルダーパス、htは更新するハイパーパラメーター(0は記述子、1は層数、2はノード数、3は連結数、4は超層連結率、5は関数確率)、utは更新参照値、rrは統計結果区間、uwはハイパーパラメータ更新重さ(区間毎)、LtPは結果対初期値重さ(順番はhtと同じ)
        {
            if (ht.Length != 6 || LtP.Length != 6)
            {
                msg = "Incorrect hyperparameter updating type or updating weight.";
                return false;
            }
            if (rr.Length + 1 != uw.Length)
            {
                msg = "Can not verify validation and updating region length.";
                return false;
            }
            msg = "Unexpected termination";
            if (!di.Exists)
            {
                msg = "Job folder doesn't exists.";
                return false;
            }
            if ((di.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly || (di.Attributes & FileAttributes.System) == FileAttributes.System)
            {
                msg = "Can not access directory.";
                return false;
            }
            if (ut > 7)
            {
                msg = "Invalid updating method.";
                return false;
            }
            string dpth = di.FullName;
            if (!dpth.EndsWith('\\')) dpth += '\\';
            string tj = dpth + "TJJ.RCsum";
            string gxm = dpth + "GXM.RCdpo";
            string gxc = dpth + "GXC.RCnhp";
            int cnt, cnt2, cnt3, itemp, itemp2, foind, inn, RS = 0;
            StringBuilder sb;
            StringBuilder sbm = new StringBuilder();
            bool flg = false;
            string dit, s;
            object losbm = new object();
            object lotm = new object();
            List<int>[] ilrr = new List<int>[uw.Length];
            object[] ilrrlo = new object[uw.Length];
            for (cnt = 0; cnt < uw.Length; cnt++)
            {
                ilrr[cnt] = new List<int>();
                ilrrlo[cnt] = new object();
            }
            List<double> desw = new List<double>();
            object lodes = new object();
            int[] Lyn;
            int[][] LNn;
            Tuple<int, int>[][] Tcn;
            double[][][] Fwp;
            int[][][] Nfp;
            bool flg2 = THNHR(out s, in dpth, out Lyn, out LNn, out Tcn, out Fwp, out Nfp);
            if (!flg2)
            {
                msg = s;
                return false;
            }
            for (cnt = 0; cnt < Lyn.Length; cnt++)
            {
                itemp = Lyn[cnt] - 1;
                inn = itemp - 1;
                if (LNn[cnt] == null || LNn[cnt].Length != inn)
                {
                    msg = "Can not verify layer node information.";
                    return false;
                }
                if (Tcn[cnt] == null || Tcn[cnt].Length != itemp)
                {
                    msg = "Can not verify layer linkage information.";
                    return false;
                }
                if (Fwp[cnt] == null || Fwp[cnt].Length != itemp)
                {
                    msg = "Can not verify superlayer linkage information.";
                    return false;
                }
                if (Nfp[cnt] == null || Nfp[cnt].Length != Lyn[cnt])
                {
                    msg = "Can not verify node function information.";
                    return false;
                }
            }
            double[] Lyw = new double[Lyn.Length];
            double[][][] Tnw = new double[Lyn.Length][][];
            Tuple<double, double>[][][] Fww = new Tuple<double, double>[Lyn.Length][][];
            double[][][] Nfw = new double[Lyn.Length][][];
            CancellationTokenSource cts = new CancellationTokenSource();
            ParallelOptions po = new ParallelOptions();
            cts = new CancellationTokenSource();
            po = new ParallelOptions();
            po.CancellationToken = cts.Token;
            po.TaskScheduler = TaskScheduler.Default;
            po.MaxDegreeOfParallelism = CommonParam.thdn;
            for (cnt = 0; cnt < Lyn.Length; cnt++)
            {
                itemp = Lyn[cnt] - 1;
                Tnw[cnt] = new double[itemp][];
                Fww[cnt] = new Tuple<double, double>[itemp - 1][];
                Nfw[cnt] = new double[Lyn[cnt]][];
                for (cnt2 = 0; cnt2 < itemp; cnt2++)
                {
                    Tnw[cnt][cnt2] = new double[Tcn[cnt][cnt2].Item2 - Tcn[cnt][cnt2].Item1 + 1];
                    if (Nfp[cnt][cnt2] == null || Nfp[cnt][cnt2].Length == THFunc.GenSouKyou.FncN) Nfw[cnt][cnt2] = new double[THFunc.GenSouKyou.FncN];
                    else Nfw[cnt][cnt2] = null;
                }
                for (cnt2 = 0; cnt2 < Fww[cnt].Length; cnt2++)
                {
                    Fww[cnt][cnt2] = new Tuple<double, double>[cnt2 + 1];
                    for (cnt3 = 0; cnt3 <= cnt2; cnt3++)
                    {
                        Fww[cnt][cnt2][cnt3] = new Tuple<double, double>(0.0, 0.0);
                    }
                }
                if (Nfp[cnt][itemp] == null || Nfp[cnt][itemp].Length == THFunc.GenSouKyou.FncN) Nfw[cnt][itemp] = new double[THFunc.GenSouKyou.FncN];
                else Nfw[cnt][itemp] = null;
            }
            for (cnt = 0; ; cnt++)
            {
                foind = cnt * 1000;
                sb = new StringBuilder(dpth);
                sb.Append(string.Format("{0}k\\", cnt));
                dit = sb.ToString();
                if (!Directory.Exists(dit))
                {
                    flg = true;
                    break;
                }
                Parallel.For(0, 1000, po, (rind) =>
                {
                    int itempp, itempp2, cntp, lind, lnitemp = 0, lnotemp = 0, cnt2p, cnt3p, itempp3, itempp4, fsind = 0, nntempp;
                    int resind = foind + rind;
                    int[] lntemp;
                    int[][] lltemp;
                    int[][] nftemp;
                    double[][] fwtemp;
                    double[] EAbstemp, E2temp, costemp, R28temp, R27temp, R21temp, rtemp, R23temp;
                    double lnsum, fstemp = 0.0;
                    string sp;
                    Match m;
                    StringBuilder sb2 = new StringBuilder(dit);
                    sb2.Append(string.Format("{0}.RClog", rind));
                    bool flgp, btempp;
                    List<int> destemp = new List<int>();
                    if (!File.Exists(sb2.ToString()))
                    {
                        flg = true;
                        return;
                    }
                    using (FileStream fs = new FileStream(sb2.ToString(), FileMode.Open, FileAccess.Read, FileShare.None, 128))
                    {
                        using (StreamReader sr = new StreamReader(fs, Encoding.Unicode, true, 128, false))
                        {
                            do
                            {
                                sp = sr.ReadLine();
                                if (sp == null)
                                {
                                    lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not find descriptor index information.", resind));
                                    return;
                                }
                                m = THNRgx.r3.Match(sp);
                            }
                            while (!m.Success);
                            flgp = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itempp);
                            if (!flgp || itempp != m.Groups[2].Captures.Count)
                            {
                                lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid descriptor index information.", resind));
                                return;
                            }
                            for (cntp = 0; cntp < itempp; cntp++)
                            {
                                flgp = int.TryParse(m.Groups[2].Captures[cntp].Value.Trim(), out itempp2);
                                if (!flgp || itempp2 < 0)
                                {
                                    lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid descriptor index information.", resind));
                                    return;
                                }
                                destemp.Add(itempp2);
                            }
                            do
                            {
                                sp = sr.ReadLine();
                                if (sp == null)
                                {
                                    lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not find Touhou Network structure title.", resind));
                                    return;
                                }
                                m = THNRgx.r12.Match(sp);
                            }
                            while (!m.Success);
                            do
                            {
                                sp = sr.ReadLine();
                                if (sp == null)
                                {
                                    lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not find Touhou Network layer infromation.", resind));
                                    return;
                                }
                                m = THNRgx.r13.Match(sp);
                            }
                            while (!m.Success);
                            flgp = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itempp);
                            if (!flgp)
                            {
                                lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer infromation.", resind));
                                return;
                            }
                            for (lind = 0; lind < Lyn.Length; lind++)
                            {
                                if (Lyn[lind] == itempp)
                                {
                                    flgp = false;
                                    break;
                                }
                            }
                            if (flgp)
                            {
                                lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Abnormal Touhou Network layer number.", resind));
                                return;
                            }
                            if (itempp != m.Groups[2].Captures.Count)
                            {
                                lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not verify Touhou Network layer number.", resind));
                                return;
                            }
                            nftemp = new int[itempp][];
                            itempp--;
                            lltemp = new int[itempp][];
                            itempp--;
                            lntemp = new int[itempp];
                            fwtemp = new double[itempp][];
                            for (cntp = 0; cntp < nftemp.Length; cntp++)
                            {
                                nftemp[cntp] = new int[THFunc.GenSouKyou.FncN];
                            }
                            for (cntp = 0; cntp < fwtemp.Length;)
                            {
                                fwtemp[cntp] = new double[++cntp];
                            }
                            for (cntp = 0; cntp < nftemp.Length; cntp++)
                            {

                                flgp = int.TryParse(m.Groups[2].Captures[cntp].Value.Trim(), out itempp);
                                if (!flgp || itempp <= 0)
                                {
                                    lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network node number.", resind));
                                    return;
                                }
                                itempp2 = cntp - 1;
                                if (cntp == 0)
                                {
                                    lnitemp = itempp;
                                }
                                else if (cntp == nftemp.GetUpperBound(0))
                                {
                                    lnotemp = itempp;
                                    lltemp[itempp2] = new int[itempp];
                                }
                                else
                                {
                                    lntemp[itempp2] = itempp;
                                    lltemp[itempp2] = new int[itempp];
                                }
                            }
                            EAbstemp = new double[lnotemp];
                            E2temp = new double[lnotemp];
                            costemp = new double[lnotemp];
                            R28temp = new double[lnotemp];
                            R27temp = new double[lnotemp];
                            R21temp = new double[lnotemp];
                            rtemp = new double[lnotemp];
                            R23temp = new double[lnotemp];
                            for (cntp = 0; cntp < nftemp.Length; cntp++)
                            {
                                itempp2 = cntp - 1;
                                do
                                {
                                    sp = sr.ReadLine();
                                    if (sp == null)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not find Touhou Network layer{1} infromation.", resind, cntp));
                                        return;
                                    }
                                    m = THNRgx.r15.Match(sp);
                                }
                                while (!m.Success);
                                flgp = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itempp);
                                if (!flgp || itempp != cntp)
                                {
                                    lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} infromation.", resind, cntp));
                                    return;
                                }
                                flgp = int.TryParse(m.Groups[2].Captures[0].Value.Trim(), out itempp);
                                if (!flgp || itempp <= 0)
                                {
                                    lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node number.", resind, cntp));
                                    return;
                                }
                                if (cntp == 0)
                                {
                                    if (itempp != lnitemp)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node number.", resind, cntp));
                                        return;
                                    }
                                }
                                else if (cntp == nftemp.GetUpperBound(0))
                                {
                                    if (itempp != lnotemp)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node number.", resind, cntp));
                                        return;
                                    }
                                }
                                else
                                {
                                    if (itempp != lntemp[cntp - 1])
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node number.", resind, cntp));
                                        return;
                                    }
                                }
                                lnsum = 0.0;
                                nntempp = itempp;
                                for (cnt2p = 0; cnt2p < nntempp; cnt2p++)
                                {
                                    sp = sr.ReadLine();
                                    itempp = THFunc.THFStB(sp);
                                    if (itempp < 0 || itempp > THFunc.GenSouKyou.FncN)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} function.", resind, cntp, cnt2p));
                                        return;
                                    }
                                    nftemp[cntp][itempp]++;
                                    if (cntp == 0)
                                    {
                                        sp = sr.ReadLine();
                                        m = THNRgx.r16a.Match(sp);
                                        if (!m.Success || !m.Groups[1].Captures[0].Value.Equals("入", StringComparison.InvariantCulture))
                                        {
                                            lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} input layer information.", resind, cntp, cnt2p));
                                            return;
                                        }
                                        sp = sr.ReadLine();
                                        m = THNRgx.r17a.Match(sp);
                                        if (!m.Success || !m.Groups[1].Captures[0].Value.Equals("入", StringComparison.InvariantCulture))
                                        {
                                            lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} input node information.", resind, cntp, cnt2p));
                                            return;
                                        }
                                        sp = sr.ReadLine();
                                        m = THNRgx.r18a.Match(sp);
                                        if (!m.Success)
                                        {
                                            lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} input coefficient information.", resind, cntp, cnt2p));
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        sp = sr.ReadLine();
                                        m = THNRgx.r16.Match(sp);
                                        if (!m.Success || !m.Groups[1].Captures[0].Value.Equals("入", StringComparison.InvariantCulture))
                                        {
                                            lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} input layer information.", resind, cntp, cnt2p));
                                            return;
                                        }
                                        flgp = int.TryParse(m.Groups[2].Captures[0].Value.Trim(), out itempp);
                                        if (!flgp || itempp <= 0 || itempp != m.Groups[3].Captures.Count)
                                        {
                                            lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} input layer information.", resind, cntp, cnt2p));
                                            return;
                                        }
                                        lltemp[itempp2][cnt2p] = itempp - 1;
                                        if (cntp > 1)
                                        {
                                            itempp3 = m.Groups[3].Captures.Count - 1;
                                            lnsum += itempp3;
                                            for (cnt3p = 0; cnt3p < itempp3; cnt3p++)
                                            {
                                                flgp = int.TryParse(m.Groups[3].Captures[cnt3p].Value.Trim(), out itempp4);
                                                if (!flgp || itempp4 < 0 || itempp4 >= cntp)
                                                {
                                                    lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} input layer information.", resind, cntp, cnt2p));
                                                    return;
                                                }
                                                if (itempp4 == itempp2) break;
                                                fwtemp[cntp - 2][itempp4] += 1.0;
                                            }
                                        }
                                        sp = sr.ReadLine();
                                        m = THNRgx.r17.Match(sp);
                                        if (!m.Success || !m.Groups[1].Captures[0].Value.Equals("入", StringComparison.InvariantCulture))
                                        {
                                            lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} input node information.", resind, cntp, cnt2p));
                                            return;
                                        }
                                        flgp = int.TryParse(m.Groups[2].Captures[0].Value.Trim(), out itempp3);
                                        if (!flgp || itempp != itempp3 || itempp3 <= 0 || itempp3 != m.Groups[3].Captures.Count)
                                        {
                                            lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} input node information.", resind, cntp, cnt2p));
                                            return;
                                        }
                                        sp = sr.ReadLine();
                                        m = THNRgx.r18.Match(sp);
                                        if (!m.Success)
                                        {
                                            lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} input coefficient information.", resind, cntp, cnt2p));
                                            return;
                                        }
                                        flgp = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itempp3);
                                        if (!flgp || itempp != itempp3 || itempp3 <= 0 || itempp3 != m.Groups[2].Captures.Count)
                                        {
                                            lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} input coefficient information.", resind, cntp, cnt2p));
                                            return;
                                        }
                                    }
                                    if (cntp == nftemp.GetUpperBound(0))
                                    {
                                        sp = sr.ReadLine();
                                        m = THNRgx.r16a.Match(sp);
                                        if (!m.Success || !m.Groups[1].Captures[0].Value.Equals("出", StringComparison.InvariantCulture))
                                        {
                                            lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} output layer information.", resind, cntp, cnt2p));
                                            return;
                                        }
                                        sp = sr.ReadLine();
                                        m = THNRgx.r17a.Match(sp);
                                        if (!m.Success || !m.Groups[1].Captures[0].Value.Equals("出", StringComparison.InvariantCulture))
                                        {
                                            lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} output node information.", resind, cntp, cnt2p));
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        sp = sr.ReadLine();
                                        m = THNRgx.r16.Match(sp);
                                        if (!m.Success || !m.Groups[1].Captures[0].Value.Equals("出", StringComparison.InvariantCulture))
                                        {
                                            lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} output layer information.", resind, cntp, cnt2p));
                                            return;
                                        }
                                        flgp = int.TryParse(m.Groups[2].Captures[0].Value.Trim(), out itempp);
                                        if (!flgp || itempp <= 0 || itempp != m.Groups[3].Captures.Count)
                                        {
                                            lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} output layer information.", resind, cntp, cnt2p));
                                            return;
                                        }
                                        sp = sr.ReadLine();
                                        m = THNRgx.r17.Match(sp);
                                        if (!m.Success || !m.Groups[1].Captures[0].Value.Equals("出", StringComparison.InvariantCulture))
                                        {
                                            lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} output node information.", resind, cntp, cnt2p));
                                            return;
                                        }
                                        flgp = int.TryParse(m.Groups[2].Captures[0].Value.Trim(), out itempp3);
                                        if (!flgp || itempp != itempp3 || itempp3 <= 0 || itempp3 != m.Groups[3].Captures.Count)
                                        {
                                            lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1} node{2} output node information.", resind, cntp, cnt2p));
                                            return;
                                        }
                                    }
                                }
                                if (cntp > 1)
                                {
                                    if (lnsum < 0.0)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network layer{1}  input node total number.", resind, cntp));
                                        return;
                                    }
                                    itempp = cntp - 2;
                                    if (fwtemp[itempp].GetUpperBound(0) != itempp)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Unexpected error : Can not verify Touhou Network layer{1} fwtemp.", resind, cntp));
                                        return;
                                    }
                                    for (cnt2p = 0; cnt2p < fwtemp[itempp].Length; cnt2p++)
                                    {
                                        fwtemp[itempp][cnt2p] /= lnsum;
                                    }
                                }
                            }
                            do
                            {
                                sp = sr.ReadLine();
                                if (sp == null)
                                {
                                    lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not find Touhou Network learning information.", resind));
                                    return;
                                }
                                m = THNRgx.r19.Match(sp);
                            }
                            while (!m.Success);
                            do
                            {
                                sp = sr.ReadLine();
                                if (sp == null)
                                {
                                    lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not find Touhou Network convergence information.", resind));
                                    return;
                                }
                                m = THNRgx.r25.Match(sp);
                            }
                            while (!m.Success);
                            flgp = bool.TryParse(m.Groups[1].Captures[0].Value.Trim(), out btempp);
                            if (!flgp)
                            {
                                lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network convergence information.", resind));
                                return;
                            }
                            Interlocked.Increment(ref RS);
                            if (!btempp)
                            {
                                return;
                            }
                            if (rr.Length == 0)
                            {
                                fsind = 0;
                            }
                            else
                            {
                                do
                                {
                                    sp = sr.ReadLine();
                                    if (sp == null)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not find Touhou Network validation infromation.", resind));
                                        return;
                                    }
                                    m = THNRgx.r31.Match(sp);
                                }
                                while (!m.Success);
                                do
                                {
                                    sp = sr.ReadLine();
                                    if (sp == null)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not find Touhou Network validation result.", resind));
                                        return;
                                    }
                                    m = THNRgx.r33.Match(sp);
                                }
                                while (!m.Success);
                                flgp = bool.TryParse(m.Groups[1].Captures[0].Value.Trim(), out btempp);
                                if (!flgp)
                                {
                                    lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network validation result.", resind));
                                    return;
                                }
                                if (btempp)
                                {
                                    //マジすか？すげぇぇぇ！
                                }
                                do
                                {
                                    sp = sr.ReadLine();
                                    if (sp == null)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not find Touhou Network validation statistical amount.", resind));
                                        return;
                                    }
                                    m = THNRgx.r34.Match(sp);
                                }
                                while (!m.Success);
                                for (cntp = 0; cntp < lnotemp; cntp++)
                                {
                                    do
                                    {
                                        sp = sr.ReadLine();
                                        if (sp == null)
                                        {
                                            lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not find Touhou Network validation statistical amount of node{1}.", resind, cntp));
                                            return;
                                        }
                                        m = THNRgx.r35.Match(sp);
                                    }
                                    while (!m.Success);
                                    flgp = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itempp);
                                    if (!flgp || itempp != cntp)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not find Touhou Network validation statistical amount of node{1}.", resind, cntp));
                                        return;
                                    }
                                    sp = sr.ReadLine();
                                    if (sp == null)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not find Touhou Network validation statistical amount of node{1}.", resind, cntp));
                                        return;
                                    }
                                    m = THNRgx.r36.Match(sp);
                                    if (!m.Success)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network validation statistical amount of node{1}.", resind, cntp));
                                        return;
                                    }
                                    flgp = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out EAbstemp[cntp]);
                                    if (!flgp || EAbstemp[cntp] < 0.0)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network validation EAbs of node{1}.", resind, cntp));
                                        return;
                                    }
                                    flgp = double.TryParse(m.Groups[2].Captures[0].Value.Trim(), out E2temp[cntp]);
                                    if (!flgp || E2temp[cntp] < 0.0)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network validation E2 of node{1}.", resind, cntp));
                                        return;
                                    }
                                    flgp = double.TryParse(m.Groups[3].Captures[0].Value.Trim(), out costemp[cntp]);
                                    if (!flgp || costemp[cntp] < -1.0 || costemp[cntp] > 1.0)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network validation cos(ŷ·y) of node{1}.", resind, cntp));
                                        return;
                                    }
                                    sp = sr.ReadLine();
                                    if (sp == null)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not find Touhou Network validation statistical amount of node{1}.", resind, cntp));
                                        return;
                                    }
                                    m = THNRgx.r37.Match(sp);
                                    if (!m.Success)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network validation statistical amount of node{1}.", resind, cntp));
                                        return;
                                    }
                                    flgp = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out R28temp[cntp]);
                                    if (!flgp || R28temp[cntp] < 0.0)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network validation R28 of node{1}.", resind, cntp));
                                        return;
                                    }
                                    flgp = double.TryParse(m.Groups[2].Captures[0].Value.Trim(), out R27temp[cntp]);
                                    if (!flgp || R27temp[cntp] > 1.0)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network validation R27 of node{1}.", resind, cntp));
                                        return;
                                    }
                                    flgp = double.TryParse(m.Groups[3].Captures[0].Value.Trim(), out R21temp[cntp]);
                                    if (!flgp || R21temp[cntp] > 1.0)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network validation R21 of node{1}.", resind, cntp));
                                        return;
                                    }
                                    sp = sr.ReadLine();
                                    if (sp == null)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not find Touhou Network validation statistical amount of node{1}.", resind, cntp));
                                        return;
                                    }
                                    m = THNRgx.r38.Match(sp);
                                    if (!m.Success)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network validation statistical amount of node{1}.", resind, cntp));
                                        return;
                                    }
                                    flgp = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out rtemp[cntp]);
                                    if (!flgp || rtemp[cntp] < -1.0 || rtemp[cntp] > 1.0)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network validation r of node{1}.", resind, cntp));
                                        return;
                                    }
                                    flgp = double.TryParse(m.Groups[2].Captures[0].Value.Trim(), out R23temp[cntp]);
                                    if (!flgp || R23temp[cntp] < 0.0)
                                    {
                                        lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Invalid Touhou Network validation R23 of node{1}.", resind, cntp));
                                        return;
                                    }
                                }
                                switch (ut)
                                {
                                    case 0:
                                        {
                                            foreach (double tkrtemp in EAbstemp) fstemp += tkrtemp;
                                            fstemp /= EAbstemp.Length;
                                            break;
                                        }
                                    case 1:
                                        {
                                            foreach (double tkrtemp in E2temp) fstemp += tkrtemp;
                                            fstemp /= E2temp.Length;
                                            break;
                                        }
                                    case 2:
                                        {
                                            foreach (double tkrtemp in costemp) fstemp += tkrtemp;
                                            fstemp /= costemp.Length;
                                            break;
                                        }
                                    case 3:
                                        {
                                            foreach (double tkrtemp in R28temp) fstemp += tkrtemp;
                                            fstemp /= R28temp.Length;
                                            break;
                                        }
                                    case 4:
                                        {
                                            foreach (double tkrtemp in R27temp) fstemp += tkrtemp;
                                            fstemp /= R27temp.Length;
                                            break;
                                        }
                                    case 5:
                                        {
                                            foreach (double tkrtemp in R21temp) fstemp += tkrtemp;
                                            fstemp /= R21temp.Length;
                                            break;
                                        }
                                    case 6:
                                        {
                                            foreach (double tkrtemp in rtemp) fstemp += tkrtemp;
                                            fstemp /= rtemp.Length;
                                            break;
                                        }
                                    case 7:
                                        {
                                            foreach (double tkrtemp in R23temp) fstemp += tkrtemp;
                                            fstemp /= R23temp.Length;
                                            break;
                                        }
                                    default: return;
                                }
                                for (cntp = 0; cntp < uw.Length; cntp++)
                                {
                                    if (cntp == uw.GetUpperBound(0) || fstemp < rr[cntp])
                                    {
                                        fsind = cntp;
                                        break;
                                    }
                                }
                            }
                            lock (ilrrlo[fsind]) ilrr[fsind].Add(resind);
                            for (cntp = 0; cntp < destemp.Count; cntp++)
                            {
                                lock (lodes)
                                {
                                    while (desw.Count <= destemp[cntp])
                                    {
                                        desw.Add(0.0);
                                    }
                                    desw[destemp[cntp]] += uw[fsind];
                                }
                            }
                            lock (Lyw.SyncRoot) Lyw[lind] += uw[fsind];
                            for (cntp = 0; cntp < lntemp.Length; cntp++)
                            {
                                for (cnt2p = 0; cnt2p < lntemp[cntp]; cnt2p++)
                                {
                                    itempp2 = lltemp[cntp][cnt2p] - Tcn[lind][cntp].Item1;
                                    if (itempp2 < Tnw[lind][cntp].Length) lock (Tnw[lind][cntp].SyncRoot) Tnw[lind][cntp][itempp2] += uw[fsind];
                                }
                            }
                            for (cnt2p = 0; cnt2p < lnotemp; cnt2p++)
                            {
                                itempp2 = lltemp[cntp][cnt2p] - Tcn[lind][cntp].Item1;
                                if (itempp2 < Tnw[lind][cntp].Length) lock (Tnw[lind][cntp].SyncRoot) Tnw[lind][cntp][itempp2] += uw[fsind];
                            }
                            if (Fww[lind].Length != fwtemp.Length)
                            {
                                lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not verify Touhou Network superlayer linkage information.", resind));
                                return;
                            }
                            for (cntp = 0; cntp < fwtemp.Length; cntp++)
                            {
                                itempp = cntp + 1;
                                if (fwtemp[cntp].Length != itempp)
                                {
                                    lock (losbm) sbm.AppendLine(string.Format("{0}.RClog : Can not verify Touhou Network superlayer linkage information.", resind));
                                    return;
                                }
                                for (cnt2p = 0; cnt2p < itempp; cnt2p++)
                                {
                                    lock (Fww[lind][cntp].SyncRoot) Fww[lind][cntp][cnt2p] = new Tuple<double, double>(Fww[lind][cntp][cnt2p].Item1 + fwtemp[cntp][cnt2p] * uw[fsind], Fww[lind][cntp][cnt2p].Item2 + uw[fsind]);
                                }
                            }
                            for (cntp = 0; cntp < nftemp.Length; cntp++)
                            {
                                if (Nfw[lind][cntp] == null) continue;
                                for (cnt2p = 0; cnt2p < THFunc.GenSouKyou.FncN; cnt2p++)
                                {
                                    lock (Nfw[lind][cntp].SyncRoot) Nfw[lind][cntp][cnt2p] += nftemp[cntp][cnt2p] * uw[fsind];
                                }
                            }
                        }
                    }
                });
                if (flg) break;
            }
            if (!flg)
            {
                sbm.AppendLine("Unexpected termination.");
                msg = sbm.ToString();
                return false;
            }
            if (RS == 0)
            {
                sbm.AppendLine("No valid result file found.");
                msg = sbm.ToString();
                return false;
            }
            sb.Clear();
            switch (ut)
            {
                case 0:
                    {
                        sb.AppendLine("Sorting reference : EAbs");
                        break;
                    }
                case 1:
                    {
                        sb.AppendLine("Sorting reference : E2");
                        break;
                    }
                case 2:
                    {
                        sb.AppendLine("Sorting reference : costemp");
                        break;
                    }
                case 3:
                    {
                        sb.AppendLine("Sorting reference : R28");
                        break;
                    }
                case 4:
                    {
                        sb.AppendLine("Sorting reference : R27");
                        break;
                    }
                case 5:
                    {
                        sb.AppendLine("Sorting reference : R21");
                        break;
                    }
                case 6:
                    {
                        sb.AppendLine("Sorting reference : r");
                        break;
                    }
                case 7:
                    {
                        sb.AppendLine("Sorting reference : R23");
                        break;
                    }
                default:
                    {
                        sbm.AppendLine("Unexpected sorting reference.");
                        msg = sbm.ToString();
                        return false;
                    }
            }
            sb.Append("区間 : ");
            for (cnt = 0; cnt < rr.Length; cnt++)
            {
                sb.Append(string.Format("{0:G15}, ", rr[cnt]));
            }
            sb.AppendLine();
            sb.Append("权重 : ");
            for (cnt = 0; cnt < uw.Length; cnt++)
            {
                sb.Append(string.Format("{0:G15}, ", uw[cnt]));
            }
            sb.AppendLine("\r\n\r\n\r\n");
            for (cnt = 0; cnt < uw.Length; cnt++)
            {
                sb.AppendLine(string.Format("Group {0}", cnt));
                sb.AppendLine(string.Format("{0:G15} ： {1:G}/{2:G}", (double)ilrr[cnt].Count / RS, ilrr[cnt].Count, RS));
                for (cnt2 = 0; cnt2 < ilrr[cnt].Count; cnt2++)
                {
                    sb.Append(string.Format("{0:G}, ", ilrr[cnt][cnt2]));
                }
                sb.AppendLine("\r\n");
            }
            sb.AppendLine("\r\n\r\n\r\n");
            sb.Append("現在時間 : ");
            sb.AppendLine(DateTime.Now.ToString("F"));
            using (FileStream fs = new FileStream(tj, FileMode.Create, FileAccess.Write, FileShare.None, 128))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.Unicode, 128, false))
                {
                    sw.Write(sb.ToString());
                }
            }
            if (ht[0] && desw.Count != 0)
            {
                double dwtemp = 0.0;
                sb.Clear();
                sb.AppendLine(string.Format("記述子長さ：{0:G}", desw.Count));
                foreach (double dwp in desw)
                {
                    dwtemp += dwp;
                }
                if (dwtemp != 0.0)
                {
                    sb.AppendLine(string.Format("权重和：{0:G15}", dwtemp));
                    sb.AppendLine(string.Format("LtP：{0:G15}", LtP[0]));
                    sb.AppendLine("Weights : ");
                    for (cnt = 0; cnt < desw.Count; cnt++)
                    {
                        sb.Append(string.Format("{0:G15}, ", desw[cnt] * LtP[0] / dwtemp));
                    }
                    sb.AppendLine("\r\n\r\n\r\n\r\n");
                    sb.Append("Time Now：");
                    sb.AppendLine(DateTime.Now.ToString("F"));
                    using (FileStream fs = new FileStream(gxm, FileMode.Create, FileAccess.Write, FileShare.None, 128))
                    {
                        using (StreamWriter sw = new StreamWriter(fs, Encoding.Unicode, 128, false))
                        {
                            sw.Write(sb.ToString());
                        }
                    }
                }
            }
            if (ht[1] || ht[2] || ht[3] || ht[4] || ht[5])
            {
                double hwtemp = 0.0;
                sb.Clear();
                sb.Append("更新项目：");
                for (cnt = 1; cnt < ht.Length; cnt++)
                {
                    if (ht[cnt]) sb.Append(cnt);
                }
                sb.AppendLine();
                for (cnt = 0; cnt < Lyn.Length; cnt++)
                {
                    if (Lyn.Length != Lyw.Length)
                    {
                        sbm.AppendLine("Can not verify layer number length");
                        msg = sbm.ToString();
                        return false;
                    }
                    itemp = Lyn[cnt] - 1;
                    inn = itemp - 1;
                    //if (LNn[cnt] == null || LNn[cnt].Length != inn) return false;
                    if (Tnw[cnt] == null || Tnw[cnt].Length != itemp)
                    {
                        sbm.AppendLine("Can not verify layer linkage information.");
                        msg = sbm.ToString();
                        return false;
                    }
                    if (Fww[cnt] == null || Fww[cnt].Length != itemp - 1)
                    {
                        sbm.AppendLine("Can not verify superlayer linkage information.");
                        msg = sbm.ToString();
                        return false;
                    }
                    if (Nfw[cnt] == null || Nfw[cnt].Length != Lyn[cnt]) return false;
                    sb.Append("Layer number : ");
                    sb.AppendLine(Lyn[cnt].ToString("G"));
                    sb.AppendLine();
                    if (ht[1])
                    {
                        hwtemp = 0.0;
                        for (cnt2 = 0; cnt2 < Lyw.Length; cnt2++)
                        {
                            hwtemp += Lyw[cnt2];
                        }
                        if (hwtemp != 0.0)
                        {
                            sb.Append("Layer weight (");
                            sb.Append(LtP[1].ToString("G15"));
                            sb.Append(") : ");
                            sb.AppendLine((Lyw[cnt] * Lyw.Length * LtP[1] / hwtemp + 1.0).ToString("G15"));
                        }
                    }
                    sb.Append("層インデックス : ");
                    sb.AppendLine(0.ToString("G"));
                    sb.Append("函数概率：");
                    if (Nfp[cnt][0] == null) sb.AppendLine("(R)");
                    else if (Nfp[cnt][0].Length == 1)
                    {
                        sb.AppendLine(string.Format("({0})", Nfp[cnt][0][0]));
                    }
                    else
                    {
                        sb.Append("(");
                        if (ht[5])
                        {
                            hwtemp = 0.0;
                            double hwtemp2 = 0.0;
                            for (cnt2 = 0; cnt2 < Nfp[cnt][0].Length; cnt2++)
                            {
                                hwtemp += Nfp[cnt][0][cnt2];
                            }
                            for (cnt2 = 0; cnt2 < Nfw[cnt][0].Length; cnt2++)
                            {
                                if (cnt2 == 2) continue;
                                hwtemp2 += Nfw[cnt][0][cnt2];
                            }
                            for (cnt2 = 0; cnt2 < Nfw[cnt][0].Length; cnt2++)
                            {
                                if (cnt2 == 2) Nfw[cnt][0][cnt2] = 0.0;
                                Nfw[cnt][0][cnt2] = Math.Floor(Nfw[cnt][0][cnt2] * hwtemp2 * LtP[5] / hwtemp + Nfp[cnt][0][cnt2]);
                            }
                            hwtemp2 = 0.0;
                            for (cnt2 = 0; cnt2 < Nfw[cnt][0].Length; cnt2++)
                            {
                                hwtemp2 += Nfw[cnt][0][cnt2];
                            }
                            for (cnt2 = 0; cnt2 < Nfw[cnt][0].GetUpperBound(0); cnt2++)
                            {
                                sb.Append(Math.Floor(Nfw[cnt][0][cnt2] * 10000.0 / hwtemp2).ToString("G"));
                                sb.Append(", ");
                            }
                            sb.Append(Math.Floor(Nfw[cnt][0][cnt2] * 10000.0 / hwtemp2).ToString("G"));
                        }
                        else
                        {
                            for (cnt2 = 0; cnt2 < Nfp[cnt][0].GetUpperBound(0); cnt2++)
                            {
                                sb.Append(Nfp[cnt][0][cnt2].ToString("G"));
                                sb.Append(", ");
                            }
                            sb.Append(Nfp[cnt][0][^1].ToString("G"));
                        }
                        sb.AppendLine(")");
                        if (ht[5])
                        {
                            sb.AppendLine(string.Format("函数权重：{0}", LtP[5]));
                        }
                    }
                    sb.AppendLine();
                    for (cnt2 = 0; cnt2 < itemp; cnt2++)
                    {
                        itemp2 = cnt2 + 1;
                        if (Fwp[cnt][cnt2] == null || Fwp[cnt][cnt2].Length != itemp2) return false;
                        sb.Append("層インデックス : ");
                        sb.AppendLine(itemp2.ToString("G"));
                        if (cnt2 != inn)
                        {
                            sb.Append("节点数：");
                            sb.AppendLine(LNn[cnt][cnt2].ToString("G"));
                        }
                        sb.Append("Layer link range : ");
                        sb.Append(Tcn[cnt][cnt2].Item1.ToString("G"));
                        sb.Append(", ");
                        sb.AppendLine(Tcn[cnt][cnt2].Item2.ToString("G"));
                        if (ht[3])
                        {
                            hwtemp = 0.0;
                            for (cnt3 = 0; cnt3 < Tnw[cnt][cnt2].Length; cnt3++)
                            {
                                hwtemp += Tnw[cnt][cnt2][cnt3];
                            }
                            if (hwtemp != 0.0)
                            {
                                sb.Append("Linkage weight (");
                                sb.Append(LtP[3].ToString("G15"));
                                sb.Append(") : ");
                                for (cnt3 = 0; cnt3 < Tnw[cnt][cnt2].GetUpperBound(0); cnt3++)
                                {
                                    sb.Append((Tnw[cnt][cnt2][cnt3] * Tnw[cnt][cnt2].Length * LtP[3] / hwtemp + 1.0).ToString("G15"));
                                    sb.Append(", ");
                                }
                                sb.AppendLine((Tnw[cnt][cnt2][^1] * Tnw[cnt][cnt2].Length * LtP[3] / hwtemp + 1.0).ToString("G15"));
                            }
                        }
                        sb.Append("超層連結率：");
                        if (cnt2 == 0) sb.AppendLine("1.0");
                        else
                        {
                            if (ht[4])
                            {
                                foind = cnt2 - 1;
                                for (cnt3 = 0; cnt3 < cnt2; cnt3++)
                                {
                                    sb.Append(((Fww[cnt][foind][cnt3].Item1 / Fww[cnt][foind][cnt3].Item2 * LtP[4] + Fwp[cnt][cnt2][cnt3]) / (LtP[4] + 1.0)).ToString("G15"));
                                    sb.Append(", ");
                                }
                                sb.AppendLine(string.Format("1.0 (重み：{0})", LtP[4]));
                            }
                            else
                            {
                                for (cnt3 = 0; cnt3 < Fwp[cnt][cnt2].GetUpperBound(0); cnt3++)
                                {
                                    sb.Append(Fwp[cnt][cnt2][cnt3].ToString("G15"));
                                    sb.Append(", ");
                                }
                                sb.AppendLine(Fwp[cnt][cnt2][cnt3].ToString("G15"));
                            }
                        }
                        sb.Append("函数概率：");
                        if (Nfp[cnt][itemp2] == null) sb.AppendLine("(R)");
                        else if (Nfp[cnt][itemp2].Length == 1)
                        {
                            sb.AppendLine(string.Format("({0})", Nfp[cnt][itemp2][0]));
                        }
                        else
                        {
                            sb.Append("(");
                            if (ht[5])
                            {
                                hwtemp = 0.0;
                                double hwtemp2 = 0.0;
                                for (cnt3 = 0; cnt3 < Nfp[cnt][itemp2].Length; cnt3++)
                                {
                                    hwtemp += Nfp[cnt][itemp2][cnt3];
                                }
                                for (cnt3 = 0; cnt3 < Nfw[cnt][itemp2].Length; cnt3++)
                                {
                                    if (cnt3 == 2) continue;
                                    hwtemp2 += Nfw[cnt][itemp2][cnt3];
                                }
                                for (cnt3 = 0; cnt3 < Nfw[cnt][itemp2].Length; cnt3++)
                                {
                                    if (cnt3 == 2) Nfw[cnt][itemp2][cnt3] = 0.0;
                                    Nfw[cnt][itemp2][cnt3] = Math.Floor(Nfw[cnt][itemp2][cnt3] * hwtemp2 * LtP[5] / hwtemp + Nfp[cnt][itemp2][cnt3]);
                                }
                                hwtemp2 = 0.0;
                                for (cnt3 = 0; cnt3 < Nfw[cnt][itemp2].Length; cnt3++)
                                {
                                    hwtemp2 += Nfw[cnt][itemp2][cnt3];
                                }
                                for (cnt3 = 0; cnt3 < Nfw[cnt][itemp2].GetUpperBound(0); cnt3++)
                                {
                                    sb.Append(Math.Floor(Nfw[cnt][itemp2][cnt3] * 10000.0 / hwtemp2).ToString("G"));
                                    sb.Append(", ");
                                }
                                sb.Append(Math.Floor(Nfw[cnt][itemp2][cnt3] * 10000.0 / hwtemp2).ToString("G"));
                            }
                            else
                            {
                                for (cnt3 = 0; cnt3 < Nfp[cnt][itemp2].GetUpperBound(0); cnt3++)
                                {
                                    sb.Append(Nfp[cnt][itemp2][cnt3].ToString("G"));
                                    sb.Append(", ");
                                }
                                sb.Append(Nfp[cnt][itemp2][^1].ToString("G"));
                            }
                            sb.AppendLine(")");
                            if (ht[5])
                            {
                                sb.AppendLine(string.Format("函数权重：{0}", LtP[5]));
                            }
                        }
                        sb.AppendLine();
                    }
                    if (cnt != Lyn.GetUpperBound(0))
                    {
                        sb.AppendLine();
                        sb.AppendLine();
                        sb.AppendLine();
                    }
                }
                sb.Append(DateTime.Now.ToString("F"));
                using (FileStream fs = new FileStream(gxc, FileMode.Create, FileAccess.Write, FileShare.None, 128))
                {
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.Unicode, 128, false))
                    {
                        sw.Write(sb.ToString());
                    }
                }
            }
            msg = "Successfully analyzed.";
            return true;
        }
        static internal bool THNTJR(out string msg, in FileInfo fi, out int[] grind, out int[][] lfind)//東方ネットワーク集計結果を読み込む、fiは集計ファイルのfileinfo、grindはgroup毎のlog file数、lfindはgroup毎のlog fileインデックス
        {
            grind = null;
            lfind = null;
            msg = null;
            if (!fi.Exists)
            {
                msg = "Can not find summary file.";
                return false;
            }
            string s;
            Match m;
            int itemp, givf, livf, cnt, cnt2;
            double dtemp;
            bool flg;
            using (FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.None, 256))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.Unicode, true, 256, false))
                {
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = "Can not find region information.";
                            return false;
                        }
                        m = THNRgx.r46.Match(s);
                    }
                    while (!m.Success);
                    givf = m.Groups[1].Captures.Count;
                    for (cnt = 0; cnt < givf; cnt++)
                    {
                        flg = double.TryParse(m.Groups[1].Captures[cnt].Value.Trim(), out dtemp);
                        if (!flg)
                        {
                            msg = "Invalid region information.";
                            return false;
                        }
                    }
                    givf++;
                    grind = new int[givf];
                    lfind = new int[givf][];
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = "Can not find weight information.";
                            return false;
                        }
                        m = THNRgx.r47.Match(s);
                    }
                    while (!m.Success);
                    if (givf != m.Groups[1].Captures.Count)
                    {
                        msg = "Can not verify group amount.";
                        return false;
                    }
                    for (cnt = 0; cnt < givf; cnt++)
                    {
                        flg = double.TryParse(m.Groups[1].Captures[cnt].Value.Trim(), out dtemp);
                        if (!flg)
                        {
                            msg = "Invalid weight information.";
                            return false;
                        }
                    }
                    for (cnt = 0; cnt < givf; cnt++)
                    {
                        do
                        {
                            s = sr.ReadLine();
                            if (s == null)
                            {
                                msg = string.Format("Can not find group {0} information.", cnt);
                                return false;
                            }
                            m = THNRgx.r48.Match(s);
                        }
                        while (!m.Success);
                        flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out itemp);
                        if (!flg || itemp != cnt)
                        {
                            msg = "Invalid group index.";
                            return false;
                        }
                        s = sr.ReadLine();
                        m = THNRgx.r49.Match(s ?? string.Empty);
                        if (s == null || !m.Success)
                        {
                            msg = string.Format("Can not find group {0} information.", cnt);
                            return false;
                        }
                        flg = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out dtemp);
                        if (!flg || dtemp < 0.0 || dtemp > 1.0)
                        {
                            msg = string.Format("Invalid group {0} portion.", cnt);
                            return false;
                        }
                        flg = int.TryParse(m.Groups[2].Captures[0].Value.Trim(), out livf);
                        if (!flg || livf < 0)
                        {
                            msg = string.Format("Invalid group {0} log file number.", cnt);
                            return false;
                        }
                        flg = int.TryParse(m.Groups[3].Captures[0].Value.Trim(), out itemp);
                        if (!flg || itemp < livf)
                        {
                            msg = string.Format("Invalid group {0} log file number.", cnt);
                            return false;
                        }
                        if (livf != 0)
                        {
                            s = sr.ReadLine();
                            m = THNRgx.r50.Match(s ?? string.Empty);
                            if (s == null || !m.Success)
                            {
                                msg = string.Format("Can not find group {0} indices.", cnt);
                                return false;
                            }
                            if (m.Groups[1].Captures.Count != livf)
                            {
                                msg = string.Format("Can not verify group {0} indices number.", cnt);
                                return false;
                            }
                        }
                        grind[cnt] = livf;
                        lfind[cnt] = new int[livf];
                        for (cnt2 = 0; cnt2 < livf; cnt2++)
                        {
                            flg = int.TryParse(m.Groups[1].Captures[cnt2].Value.Trim(), out lfind[cnt][cnt2]);
                            if (!flg || lfind[cnt][cnt2] < 0)
                            {
                                msg = string.Format("Invalid group {0} log file index.", cnt);
                                return false;
                            }
                        }
                    }
                }
            }
            msg = "Succesfully read.";
            return true;
        }
        static internal bool THNYSKR(out string msg, in DirectoryInfo di, in FileInfo desfi, in double[] wt)//東方ネットワーク予測(Raw)、diはジョブディレクトリー、desfiは記述子fileinfo、wtはグループ重さ
        {
            msg = null;
            if (!di.Exists)
            {
                msg = string.Format("Can not find directory : {0}.", di.FullName);
                return false;
            }
            if ((di.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly || (di.Attributes & FileAttributes.System) == FileAttributes.System)
            {
                msg = "Can not access task directory.";
                return false;
            }
            if (!desfi.Exists)
            {
                msg = string.Format("Can not find descriptor file : {0}.", desfi.FullName);
                return false;
            }
            int Ydn, cnt, cnt2, cnt3, cnt4, lfidx, lidx, Dtn, XXC;
            int[] grind, desI, Nn, Di, Dti;
            int[][] lfind, Nf;
            int[][][] lila, lina, lola, lona;
            bool flg;
            bool[] ConVal;
            double[] Dc, Dn, predt;
            double[][] desL, Stat, desin;
            double[][][] Nrk;
            string dpth, despth;
            string[] desN;
            StringBuilder sb, sbr, sbw = new StringBuilder();
            FileInfo fi;
            THNetwork thn;
            List<double[]> Pred = new List<double[]>();
            dpth = di.FullName;
            sb = new StringBuilder(dpth);
            if (!dpth.EndsWith("\\"))
            {
                sb.Append("\\");
                dpth = sb.ToString();
            }
            sb.Append("TJJ.RCsum");
            fi = new FileInfo(sb.ToString());
            if (!fi.Exists)
            {
                msg = "Can not find summary file, please do summary first.";
                return false;
            }
            flg = THNetwork.THNTJR(out msg, in fi, out grind, out lfind);
            if (!flg)
            {
                return true;
            }
            if (grind == null || lfind == null || grind.Length == 0 || grind.Length != lfind.Length)
            {
                msg = "Invalid summay file.";
                return false;
            }
            if (wt == null || wt.Length != grind.Length)
            {
                msg = "Invalid weight array.";
                return false;
            }
            for (cnt = 0; cnt < wt.Length; cnt++)
            {
                sbw.Append(string.Format("{0}, ", wt[cnt]));
            }
            try
            {
                DataProc.CSVRdouble(desfi.FullName, out desN, out desL, out desI, out Ydn);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (e.InnerException != null) Console.WriteLine(e.InnerException);
                return false;
            }
            if (desL == null || desI == null || desL.Length != desI.Length)
            {
                msg = "Invalid descriptor length, can not verify.";
                return false;
            }
            for (cnt = 0; cnt < desL.Length; cnt++)
            {
                if (desL[cnt].Length != Ydn)
                {
                    msg = "Invalid library length, can not verify.";
                    return false;
                }
            }
            sbr = new StringBuilder();
            for (cnt = 0; cnt < grind.Length; cnt++)
            {
                if (grind[cnt] != lfind[cnt].Length)
                {
                    msg = string.Format("Can not verify group {0} log file amount.", cnt);
                    return false;
                }
                if (wt[cnt] <= 0.0) continue;
                for (cnt2 = 0; cnt2 < grind[cnt]; cnt2++)
                {
                    sbr.Append(string.Format("{0:G}({1:G}),", lfind[cnt][cnt2], cnt));
                    lfidx = Math.DivRem(lfind[cnt][cnt2], 1000, out lidx);
                    sb = new StringBuilder(dpth);
                    sb.Append(string.Format("{0}k\\{1}.RClog", lfidx, lidx));
                    flg = THNJRR(out msg, sb.ToString(), out Nn, out Nf, out lila, out lina, out lola, out lona, out Nrk, out Dtn, out Di, out Dc, out Dn, out ConVal, out Stat, out Dti, out XXC);
                    if (!flg) return false;
                    desin = new double[Di.Length][];
                    for (cnt3 = 0; cnt3 < desin.Length; cnt3++)
                    {
                        desin[cnt3] = new double[Ydn];
                        if (desin[cnt3].Length != Ydn)
                        {
                            msg = string.Format("Can not verify descriptor {0} length.", Di[cnt3]);
                            return false;
                        }
                        for (cnt4 = 0; cnt4 < desin[cnt3].Length; cnt4++)
                        {
                            desin[cnt3][cnt4] = (desL[Di[cnt3]][cnt4] - Dc[cnt3]) / Dn[cnt3];
                        }
                    }
                    try
                    {
                        thn = new THNetwork(in Ydn, in desin, in Nn, in Nf, in lila, in lina, in lola, in lona, in Nrk);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        if (e.InnerException != null) Console.WriteLine(e.InnerException);
                        msg = "Can not create Touhou Network.";
                        return false;
                    }
                    thn.FP();
                    predt = new double[Ydn];
                    for (cnt3 = 0; cnt3 < Ydn; cnt3++)
                    {
                        predt[cnt3] = thn.Nly[thn.Nly.GetUpperBound(0)].N(0).F(cnt3);
                    }
                    Pred.Add(predt);
                }
            }
            sbr.AppendLine();
            Dtn = Ydn - 1;
            cnt3 = Pred.Count - 1;
            for (cnt = 0; cnt < Ydn; cnt++)
            {
                for (cnt2 = 0; cnt2 < cnt3; cnt2++)
                {
                    sbr.Append(string.Format("{0:G15},", Pred[cnt2][cnt]));
                }
                sbr.Append(Pred[cnt3][cnt].ToString("G15"));
                if (cnt != Dtn)
                {
                    sbr.AppendLine();
                }
            }
            despth = desfi.DirectoryName;
            sb = new StringBuilder(despth);
            if (!despth.EndsWith("\\"))
            {
                sb.Append("\\");
                despth = sb.ToString();
            }
            DirectoryInfo dides = new DirectoryInfo(despth);
            if ((dides.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly || (dides.Attributes & FileAttributes.System) == FileAttributes.System)
            {
                msg = "Can not access directory of descriptor file.";
                return false;
            }
            sb.Append("GroupWeight.csv");
            using (FileStream fs = new FileStream(sb.ToString(), FileMode.Create, FileAccess.Write, FileShare.None, 256))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8, 256, false))
                {
                    sw.Write(sbw.ToString());
                }
            }
            sb = new StringBuilder(despth);
            sb.Append("Prediction_raw.csv");
            using (FileStream fs = new FileStream(sb.ToString(), FileMode.Create, FileAccess.Write, FileShare.None, 256))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8, 256, false))
                {
                    sw.Write(sbr.ToString());
                }
            }
            msg = "Successfully predicted.";
            return true;
        }
        static internal bool THNYSKSK(out string msg, in DirectoryInfo di, in double ul, in double ll)//東方ネットワーク予測(集計)、diは記述子ディレクトリー、ulは上限、llは下限
        {
            msg = null;
            if (!di.Exists)
            {
                msg = "Can not find descriptor directory.";
                return false;
            }
            if ((di.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly || (di.Attributes & FileAttributes.System) == FileAttributes.System)
            {
                msg = "Can not access directory.";
                return false;
            }
            if (double.IsNaN(ul) || double.IsNaN(ll) || ul < ll)
            {
                msg = "Invalid limit.";
                return false;
            }
            int cnt, cnt2, lgn, libn = 0, ignv;
            int[] lfi, gi, mng;
            double dtemp, ws;
            double[] w, prtemp, avg, var;
            List<double[]> predR = new List<double[]>();
            bool flg;
            string dpth, s;
            StringBuilder sb, sbp;
            Match m;
            dpth = di.FullName;
            sb = new StringBuilder(dpth);
            if (!dpth.EndsWith("\\"))
            {
                sb.Append("\\");
                dpth = sb.ToString();
            }
            sb.Append("GroupWeight.csv");
            s = sb.ToString();
            if (!File.Exists(s))
            {
                msg = "Can not find GroupWeight.csv file.";
                return false;
            }
            using (FileStream fs = new FileStream(s, FileMode.Open, FileAccess.Read, FileShare.None, 256))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8, true, 256, false))
                {
                    s = sr.ReadLine();
                    if (s == null)
                    {
                        msg = string.Format("{0} : Can not find weight information.", sb.ToString());
                        return false;
                    }
                    m = THNRgx.r51.Match(s);
                    if (!m.Success)
                    {
                        msg = string.Format("{0} : Invalid weight information.", sb.ToString());
                        return false;
                    }
                    w = new double[m.Groups[1].Captures.Count];
                    for (cnt = 0; cnt < m.Groups[1].Captures.Count; cnt++)
                    {
                        flg = double.TryParse(m.Groups[1].Captures[cnt].Value.Trim(), out dtemp);
                        if (!flg || dtemp < 0.0)
                        {
                            msg = string.Format("Invalid weight, {0}.", dtemp);
                            return false;
                        }
                        w[cnt] = dtemp;
                    }
                }
            }
            sb = new StringBuilder(dpth);
            sb.Append("Prediction_raw.csv");
            s = sb.ToString();
            if (!File.Exists(s))
            {
                msg = "Can not find Prediction_raw.csv file.";
                return false;
            }
            using (FileStream fs = new FileStream(s, FileMode.Open, FileAccess.Read, FileShare.None, 256))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8, true, 256, false))
                {
                    s = sr.ReadLine();
                    if (s == null)
                    {
                        msg = string.Format("{0} : Can not find raw prediction title.", sb.ToString());
                        return false;
                    }
                    m = THNRgx.r52.Match(s);
                    if (!m.Success)
                    {
                        msg = string.Format("{0} : Invalid raw prediction title.", sb.ToString());
                        return false;
                    }
                    if (m.Groups[1].Captures.Count != m.Groups[2].Captures.Count || m.Groups[1].Captures.Count <= 0)
                    {
                        msg = string.Format("{0} : Can not verify prediction title length.", sb.ToString());
                        return false;
                    }
                    lfi = new int[m.Groups[1].Captures.Count];
                    gi = new int[m.Groups[2].Captures.Count];
                    for (cnt = 0; cnt < gi.Length; cnt++)
                    {
                        flg = int.TryParse(m.Groups[1].Captures[cnt].Value.Trim(), out lfi[cnt]);
                        if (!flg || lfi[cnt] < 0)
                        {
                            msg = string.Format("Invalid log file index, {0}.", lfi[cnt]);
                            return false;
                        }
                        flg = int.TryParse(m.Groups[2].Captures[cnt].Value.Trim(), out gi[cnt]);
                        if (!flg || gi[cnt] < 0)
                        {
                            msg = string.Format("Invalid group index, {0}.", gi[cnt]);
                            return false;
                        }
                    }
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            if (libn == 0)
                            {
                                msg = string.Format("{0} : Can not find raw prediction information.", sb.ToString());
                                return false;
                            }
                            else break;
                        }
                        m = THNRgx.r51.Match(s);
                        if (!m.Success)
                        {
                            msg = string.Format("{0} : Invalid raw prediction information.", sb.ToString());
                            return false;
                        }
                        if (m.Groups[1].Captures.Count != gi.Length)
                        {
                            msg = string.Format("{0} : Can not verify raw prediction length.", sb.ToString());
                            return false;
                        }
                        prtemp = new double[m.Groups[1].Captures.Count];
                        for (cnt = 0; cnt < m.Groups[1].Captures.Count; cnt++)
                        {
                            flg = double.TryParse(m.Groups[1].Captures[cnt].Value.Trim(), out prtemp[cnt]);
                            if (!flg || double.IsNaN(prtemp[cnt]))
                            {
                                msg = string.Format("{0} : Invalid raw prediction, {0}.", prtemp[cnt]);
                                return false;
                            }
                        }
                        predR.Add(prtemp);
                        libn++;
                    }
                    while (true);
                }
            }
            for (cnt = 0; cnt < gi.Length; cnt++)
            {
                if (gi[cnt] >= w.Length)
                {
                    msg = string.Format("{0} : Invalid raw group index, {0}.", gi[cnt]);
                    return false;
                }
            }
            sbp = new StringBuilder("Average(w),Variance(w),Models,");
            for (cnt = 0; cnt < w.Length; cnt++)
            {
                if (w[cnt] == 0.0) continue;
                sbp.Append(string.Format("Average({0}),Variance({0}),Models({0}),", cnt));
            }
            sbp.AppendLine();
            lgn = w.Length + 1;
            for (cnt = 0; cnt < libn; cnt++)
            {
                mng = new int[lgn];
                avg = new double[lgn];
                var = new double[lgn];
                ws = 0.0;
                for (cnt2 = 0; cnt2 < predR[cnt].Length; cnt2++)
                {
                    if (predR[cnt][cnt2] <= ul && predR[cnt][cnt2] >= ll)
                    {
                        mng[gi[cnt2]]++;
                        mng[w.Length]++;
                        avg[gi[cnt2]] += predR[cnt][cnt2];
                        avg[w.Length] += predR[cnt][cnt2] * w[gi[cnt2]];
                        ws += w[gi[cnt2]];
                    }
                }
                ignv = 0;
                for (cnt2 = 0; cnt2 < lgn; cnt2++)
                {
                    if (cnt2 != w.Length)
                    {
                        ignv += mng[cnt2];
                        if (mng[cnt2] != 0) avg[cnt2] /= mng[cnt2];
                    }
                    else
                    {
                        if (ignv != mng[cnt2])
                        {
                            msg = "Can not verify amount of model used for groups.";
                            return false;
                        }
                        if (mng[cnt2] != 0)
                        {
                            if (ws <= 0.0)
                            {
                                msg = "Unexpected sum of weight of groups.";
                                return false;
                            }
                            avg[cnt2] /= ws;
                        }
                    }
                }
                if (mng[w.Length] != 0)
                {
                    for (cnt2 = 0; cnt2 < predR[cnt].Length; cnt2++)
                    {
                        if (predR[cnt][cnt2] <= ul && predR[cnt][cnt2] >= ll)
                        {
                            dtemp = predR[cnt][cnt2] - avg[gi[cnt2]];
                            var[gi[cnt2]] += dtemp * dtemp;
                            dtemp = predR[cnt][cnt2] - avg[w.Length];
                            var[w.Length] += dtemp * dtemp * w[gi[cnt2]];
                        }
                    }
                    for (cnt2 = 0; cnt2 < w.Length; cnt2++)
                    {
                        var[cnt2] /= mng[cnt2];
                    }
                    var[w.Length] /= ws;
                    sbp.Append(string.Format("{0},{1},{2},", avg[w.Length], var[w.Length], mng[w.Length]));
                    for (cnt2 = 0; cnt2 < w.Length; cnt2++)
                    {
                        if (w[cnt2] == 0.0) continue;
                        sbp.Append(string.Format("{0},{1},{2},", avg[cnt2], var[cnt2], mng[cnt2]));
                    }
                    sbp.AppendLine();
                }
                else
                {
                    sbp.Append("NaN,NaN,0,");
                    for (cnt2 = 0; cnt2 < w.Length; cnt2++)
                    {
                        if (w[cnt2] == 0.0) continue;
                        sbp.Append("NaN,NaN,0,");
                    }
                    sbp.AppendLine();
                }
            }
            sb = new StringBuilder(dpth);
            sb.Append("Prediction_summary.csv");
            using (FileStream fs = new FileStream(sb.ToString(), FileMode.Create, FileAccess.Write, FileShare.None, 256))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8, 256, false))
                {
                    sw.Write(sbp.ToString());
                }
            }
            msg = "Summarized successfully.";
            return true;
        }
        static internal void THNGen(in DirectoryInfo di, in string tjgxr)
        {

        }
        static internal bool THNChk(out string msg, in DirectoryInfo di)
        {
            string dpth = di.FullName;
            if (!Directory.Exists(dpth))
            {
                msg = "Can not find directory.";
                return false;
            }
            if (!dpth.EndsWith('\\')) dpth += '\\';
            StringBuilder sb, sb2, sb3;
            string dit, msg2, fpth, rwm, resp, chkpth;
            string[] desp, desN, yN;
            string[][] saatemp;
            int cnt, cnt2, Dtn, Dtna, NK, foind, find, cnt3, cnt4, Lnind, itemp, itemp2, XXC, restn = 0, ccc;
            int[] Lyn, Nn, Di, Dti, resi, resg, desn, desI, yi, iaatemp;
            int[][] LNn, Nf, resgi;
            int[][][] Nfp, lila, lina, lola, lona;
            Tuple<int, int>[][] Tcn;
            Tuple<double, double>[][] err;
            double G, KRK;
            double[] Dc, Dn;
            double[][] Stat, cdr, desL, yL, gdo, gdt, desin, dest, E;
            double[][][] Fwp, Nrk;
            bool flg, flg2;
            bool[] ConVal;
            byte thnn;
            FileInfo fi;
            flg = THNJR(out msg2, out rwm, out desp, out resp, in dpth, out resi, out resg, out resgi, out desn, out cdr, out err, out NK, out G, out KRK, out thnn);
            if (!flg)
            {
                msg = "Fail reading job file.";
                return false;
            }
            for (cnt = 0; cnt < resg.Length; cnt++)
            {
                restn += resg[cnt];
            }
            flg = THNHR(out msg2, in dpth, out Lyn, out LNn, out Tcn, out Fwp, out Nfp);
            if (!flg)
            {
                msg = "Fail reading hyperparameter file.";
                return false;
            }
            if (!File.Exists(desp[0]) || !File.Exists(resp))
            {
                if (!File.Exists(desp[0]))
                {
                    msg = "Descriptor file does't exists.";
                    return false;
                }
                else
                {
                    msg = "Result file does't exists.";
                    return false;
                }
            }
            try
            {
                DataProc.CSVRdouble(in desp[0], out desN, out desL, out desI, out Dtna);
            }
            catch (Exception e)
            {
                msg = string.Format("Fail reading => {0}", e);
                if (e.InnerException != null) Console.WriteLine(e.InnerException);
                return false;
            }
            try
            {
                DataProc.CSVRdoubleJ(in resp, out yN, out yL, out yi, out saatemp, out iaatemp, out cnt);
            }
            catch (Exception e)
            {
                msg = string.Format("Fail reading => {0}", e);
                if (e.InnerException != null) Console.WriteLine(e.InnerException);
                return false;
            }
            if (cnt != Dtna)
            {
                msg = "Can not verify data number.";
                return false;
            }
            sb = new StringBuilder(dpth);
            sb.Append("chk\\");
            chkpth = sb.ToString();
            for (cnt = 0; cnt < 1000; cnt++)
            {
                foind = 1000 * cnt;
                sb = new StringBuilder(dpth);
                sb.Append(string.Format("{0}k\\", cnt));
                dit = sb.ToString();
                if (!Directory.Exists(dit))
                {
                    if (cnt == 0)
                    {
                        msg = "No RClog file found.";
                        return false;
                    }
                    break;
                }
                flg = false;
                for (cnt2 = 0; cnt2 < 1000; cnt2++)
                {
                    ccc = 0;
                    find = foind + cnt2;
                    sb2 = new StringBuilder(dit);
                    sb2.Append(string.Format("{0}.RClog", cnt2));
                    fpth = sb2.ToString();
                    if (!File.Exists(fpth))
                    {
                        if (cnt == 0 && cnt2 == 0)
                        {
                            msg = "No RClog file found.";
                            return false;
                        }
                        flg = true;
                        break;
                    }
                    flg2 = THNJRR(out msg2, in fpth, out Nn, out Nf, out lila, out lina, out lola, out lona, out Nrk, out Dtn, out Di, out Dc, out Dn, out ConVal, out Stat, out Dti, out XXC);
                    if (!flg2)
                    {
                        msg = string.Format("{0} : Fail reading logfile.", find);
                        continue;
                    }
                    if (restn != Dti.Length)
                    {
                        msg = string.Format("{0} : Can not verify validation data amount", find);
                        continue;
                    }
                    if (!ConVal[0]) continue;
                    Lnind = -1;
                    for (cnt3 = 0; cnt3 < Lyn.Length; cnt3++)
                    {
                        if (Lyn[cnt] == Nn.Length)
                        {
                            Lnind = cnt3;
                            break;
                        }
                    }
                    if (cnt3 == Lyn.Length)
                    {
                        msg = string.Format("{0} : Unexpected layer number.", find);
                        continue;
                    }
                    gdt = new double[1][];
                    gdo = new double[1][];
                    gdt[0] = new double[Dti.Length];
                    gdo[0] = new double[Dtn];
                    dest = new double[Di.Length][];
                    desin = new double[Di.Length][];
                    for (cnt3 = 0; cnt3 < Di.Length; cnt3++)
                    {
                        dest[cnt3] = new double[Dti.Length];
                        desin[cnt3] = new double[Dtn];
                    }
                    itemp = 0;
                    itemp2 = 0;
                    for (cnt3 = 0; cnt3 < Dtna; cnt3++)
                    {
                        if (Dti[itemp] == cnt3)
                        {
                            gdt[0][itemp] = yL[resi[0]][cnt3];
                            for (cnt4 = 0; cnt4 < Di.Length; cnt4++)
                            {
                                dest[cnt4][itemp] = (desL[Di[cnt4]][cnt3] - Dc[cnt4]) / Dn[cnt4];
                            }
                            itemp++;
                        }
                        else
                        {
                            gdo[0][itemp2] = yL[resi[0]][cnt3];
                            for (cnt4 = 0; cnt4 < Di.Length; cnt4++)
                            {
                                desin[cnt4][itemp2] = (desL[Di[cnt4]][cnt3] - Dc[cnt4]) / Dn[cnt4];
                            }
                            itemp2++;
                        }
                    }
                    THNetwork thn = new THNetwork(in Dtn, in desin, in gdo, in Nn, in Nf, in lila, in lina, in lola, in lona, in Nrk);
                    thn.FP();
                    thn.C = thn.SSye(in cdr, in err, out E);
                    if (thn.C)
                    {
                        sb3 = new StringBuilder(chkpth);
                        sb3.Append(string.Format("{0}k\\", cnt));
                        if (!Directory.Exists(sb3.ToString())) Directory.CreateDirectory(chkpth);
                        sb3.Append(string.Format("{0}.RClog", cnt2));
                        File.Copy(fpth, sb3.ToString(), true);
                    }
                    else
                    {
                        try
                        {
                            itemp = thn.XX(NK, cdr, err, G, desin, gdo, ref ccc);
                            if (itemp >= 0 && thn.C)
                            {
                                thn.NVT(Dti.Length, in dest, in gdt, in cdr, in err, out Stat);
                                sb3 = new StringBuilder(chkpth);
                                sb3.Append(string.Format("{0}k\\", cnt));
                                if (!Directory.Exists(sb3.ToString())) Directory.CreateDirectory(chkpth);
                                sb3.Append(string.Format("{0}.RClog", cnt2));
                                using (FileStream fs = File.Create(sb3.ToString())) { }
                                fi = new FileInfo(sb3.ToString());
                                THNRcs(in thn, in Stat, in Tcn[Lnind], in Fwp[Lnind], in Nfp[Lnind], in cdr, in err, in gdo, in gdt, in NK, in XXC, in G, in Di, in Dc, in Dn, in fi, in Dti, in rwm, in resp, in resi, in desp[0]);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            if (e.InnerException != null) Console.WriteLine(e.InnerException);
                            continue;
                        }
                    }
                }
                if (flg == true) break;
            }
            msg = "Check done successfully.";
            return true;
        }
        static internal bool DesStWR(out string msg, in FileInfo fi, in int ln0, in double[][] desL, in int[] desI, in int[] Dti, out double[][] desin, out double[][] dest, out int[] Di, out double[] Dc, out double[] Dn, in byte thnn)//記述子選択(重み付きランダム)、fiはRCdpoファイル、ln0は入力記述子数、desLは全記述子列、desIは全記述子インデックス、Dtiはテストデータインデックス、destは入力記述子列、destはテスト記述子、Diは入力記述子インデックス、Diは記述子インデックス、Dcは記述子中心化係数、thnn 0は正規化しない,1は標準スコア正規化,2は東方ネットワーク正規化
        {
            msg = null;
            desin = null;
            dest = null;
            Di = null;
            Dc = null;
            Dn = null;
            if (ln0 > desL.Length)
            {
                msg = "DesStWR (ln0) : Not enough descriptors.";
                return false;
            }
            if (desL.Length != desI.Length)
            {
                msg = "DesStWR (desL/desI) : Can not verify descriptor length.";
                return false;
            }
            if (!fi.Exists)
            {
                msg = "DesStWR (fi) : Can not find descriptor weight file.";
                return false;
            }
            Random rnd = new Random();
            desin = new double[ln0][];
            Di = new int[ln0];
            Dc = new double[ln0];
            Dn = new double[ln0];
            dest = new double[ln0][];
            List<int> dil = new List<int>(desI);
            List<int> deslt = new List<int>();
            double DWS, ltp, drnd;
            double[] dint, dtt, pdf, pdft, cdf;
            int cnt;
            int dic = desL[0].Length - Dti.Length;
            int cnt2, cnt3, cnt4, cnt5, dwl;
            double[] destp = null;
            Tuple<double, double> ttemp = null;
            bool flg = false;
            string s;
            Match m;
            using (FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.None, 256))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.Unicode, true, 256, false))
                {
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = "Can not find descriptor length information.";
                            return false;
                        }
                        m = THNRgx.r53.Match(s);
                    }
                    while (!m.Success);
                    flg = int.TryParse(m.Groups[1].Captures[0].Value.Trim(), out dwl);
                    if (!flg || dwl <= 0)
                    {
                        msg = "Invalid descriptor length.";
                        return false;
                    }
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = "Can not find descriptor LtP information.";
                            return false;
                        }
                        m = THNRgx.r55.Match(s);
                    }
                    while (!m.Success);
                    flg = double.TryParse(m.Groups[1].Captures[0].Value.Trim(), out ltp);
                    if (!flg || ltp <= 0.0)
                    {
                        msg = "Invalid LtP.";
                        return false;
                    }
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = "Can not find descriptor weights title.";
                            return false;
                        }
                        m = THNRgx.r56.Match(s);
                    }
                    while (!m.Success);
                    do
                    {
                        s = sr.ReadLine();
                        if (s == null)
                        {
                            msg = "Can not find descriptor weights.";
                            return false;
                        }
                        m = THNRgx.r51.Match(s);
                    }
                    while (!m.Success);
                    if (m.Groups[1].Captures.Count != dwl)
                    {
                        msg = "Can not verify descriptor weights number.";
                        return false;
                    }
                    pdft = new double[dwl];
                    DWS = 0.0;
                    for (cnt = 0; cnt < dwl; cnt++)
                    {
                        flg = double.TryParse(m.Groups[1].Captures[cnt].Value.Trim(), out pdft[cnt]);
                        if (!flg || pdft[cnt] < 0.0 || double.IsNaN(pdft[cnt]) || double.IsInfinity(pdft[cnt]))
                        {
                            msg = "Invalid weight.";
                            return false;
                        }
                        DWS += pdft[cnt];
                    }
                }
            }
            if (DWS <= 0.0)
            {
                msg = "Invalid weights.";
                return false;
            }
            ltp = desL.Length * ltp / DWS;
            DWS = 0.0;
            pdf = new double[desL.Length];
            cdf = new double[desL.Length];
            for (cnt = 0; cnt < pdf.Length; cnt++)
            {
                if (cnt < dwl)
                {
                    pdf[cnt] = pdft[cnt] * ltp + 1.0;
                }
                else
                {
                    pdf[cnt] = 1.0;
                }
                DWS += pdf[cnt];
                cdf[cnt] = DWS;
            }
            cnt = 0;
            while (cnt < ln0)
            {
                if (dil.Count == 0)
                {
                    msg = "DesStWR (dil) : Not enough suitable descriptors.";
                    return false;
                }
                drnd = rnd.NextDouble() * DWS;
                cnt3 = 0;
                for (cnt2 = 0; cnt2 < dil.Count; cnt2++)
                {
                    while (true)
                    {
                        if (cnt3 < deslt.Count)
                        {
                            if (dil[cnt2] > deslt[cnt3])
                            {
                                drnd += pdf[deslt[cnt3]];
                                cnt3++;
                            }
                            else break;
                        }
                        else break;
                    }
                    if (drnd < cdf[dil[cnt2]]) break;
                }
                if (cnt2 >= dil.Count)
                {
                    msg = "DesStWR (drnd/cdf) : Unknown error. Can not find proper descriptor.";
                    return false;
                }
                deslt.Add(dil[cnt2]);
                deslt.Sort();
                DWS -= pdf[dil[cnt2]];
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
                            flg = DataProc.SkkSC(in desL[dil[cnt2]], out destp, out ttemp);
                            if (!flg) ttemp = null;
                            break;
                        }
                    case 2:
                        {
                            flg = DataProc.THNSkk(in desL[dil[cnt2]], out destp, out ttemp);
                            break;
                        }
                    default:
                        {
                            msg = "DesStWR (thnn) : Unexpected Touhou Network normalization type.";
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
                        msg = "DesStWR (ttemp) : Unexpected value.";
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
        static internal bool TH_N_T_R_(out string msg, in DirectoryInfo di, in int mn, in bool desjq, ref int ccc)//東方ネットワークタスク実行、diはジョブディレクトリー、mnはモデル数、desjq trueは記述子重み付きランダム選択,falseは一様ランダム、
        {
            msg = null;
            if (!di.Exists)
            {
                msg = "Can not find directory.";
                return false;
            }
            if (mn <= 0)
            {
                msg = "Incorrect model amount.";
                return false;
            }
            string dpth = di.FullName;
            StringBuilder sb = new StringBuilder(dpth);
            if (!dpth.EndsWith("\\"))
            {
                sb.Append("\\");
                dpth = sb.ToString();
            }
            sb.Append("CSH.RCjob");
            if (!File.Exists(sb.ToString()))
            {
                msg = "Can not find task file. Create job first.";
                return false;
            }
            sb = new StringBuilder(dpth);
            sb.Append("CCS.RChyp");
            if (!File.Exists(sb.ToString()))
            {
                msg = "Can not find hyperparameter file. Set hyperparameters first.";
                return false;
            }
            string rwm, resp;
            string[] desp, desN, satemp;
            string[][] saatemp;
            int cnt, cnt2, NK, Dtn, itemp, errcnt = 0, Dl, Dt, lyn;
            int[] resi, resg, desn, Lyn, desI, iatemp, iatemp2, Dti, Di;
            int[][] resgi, LNn, lni;
            int[][][] Nfp;
            double G, KRK;
            double[] y, Dc, Dn;
            double[][] cdr, desL, daatemp, gdo, gdt, desin, dest;
            double[][][] Fwp;
            Tuple<double, double>[][] err;
            Tuple<int, int>[][] Tcn;
            byte thnn;
            bool flg;
            Random rnd;
            FileInfo fi;//記録ファイル
            flg = THNJR(out msg, out rwm, out desp, out resp, in dpth, out resi, out resg, out resgi, out desn, out cdr, out err, out NK, out G, out KRK, out thnn);
            if (!flg) return false;
            flg = THNHR(out msg, in dpth, out Lyn, out LNn, out Tcn, out Fwp, out Nfp);
            if (!flg) return false;
            lni = new int[LNn.Length][];
            for (cnt = 0; cnt < lni.Length; cnt++)
            {
                lni[cnt] = new int[LNn[cnt].Length + 2];
                for (cnt2 = 0; cnt2 < lni[cnt].Length; cnt2++)
                {
                    if (cnt2 == 0)
                    {
                        lni[cnt][cnt2] = desn[0];
                    }
                    else if (cnt2 == lni[cnt].GetUpperBound(0))
                    {
                        lni[cnt][cnt2] = resi.Length;
                    }
                    else
                    {
                        lni[cnt][cnt2] = LNn[cnt][cnt2 - 1];
                    }
                }
            }
            DESREAD:
            try//記述子を読み込む
            {
                DataProc.CSVRdouble(in desp[0], out desN, out desL, out desI, out Dtn);
            }
            catch (IndexOutOfRangeException iore)
            {
                Console.WriteLine(iore);
                errcnt++;
                if (errcnt == 10)
                {
                    msg = "Can not read file. Please restart the program.";
                    return false;
                }
                goto DESREAD;
            }
            catch (Exception e)
            {
                msg = e.ToString();
                return false;
            }
            ERREAD:
            errcnt = 0;
            try//実験データを読み込む
            {
                DataProc.CSVRdoubleJ(in resp, out satemp, out daatemp, out iatemp, out saatemp, out iatemp2, out itemp);
                y = daatemp[resi[0]];
                if (itemp != Dtn)
                {
                    msg = "Can not verify data length.";
                    return false;
                }
            }
            catch (IndexOutOfRangeException iore)
            {
                Console.WriteLine(iore);
                errcnt++;
                if (errcnt == 10)
                {
                    msg = "Can not read file. Please restart the program.";
                    return false;
                }
                goto ERREAD;
            }
            catch (Exception e)
            {
                msg = e.ToString();
                return false;
            }
            satemp = null;
            iatemp = null;
            saatemp = null;
            iatemp2 = null;
            gdo = new double[1][];
            gdt = new double[1][];
            Dt = 0;
            for (cnt = 0; cnt < resg.Length; cnt++)
            {
                Dt += resg[cnt];
            }
            Dl = Dtn - Dt;
            rnd = new Random();
            for (cnt = 0; cnt < mn; cnt++)
            {
                flg = DataProc.MbzFg(out msg, in y, out gdo[0], out gdt[0], out Dti, in resgi, in resg);//真データを分割する
                if (!flg) return false;
                if (desjq)
                {
                    sb = new StringBuilder(dpth);
                    sb.Append("GXM.RCdpo");
                    FileInfo wfi;
                    if (!File.Exists(sb.ToString()))
                    {
                        msg = "Can not find descriptor weight file. Analyze logfiles first.";
                        return false;
                    }
                    wfi = new FileInfo(sb.ToString());
                    flg = DesStWR(out msg, in wfi, desn[0], in desL, in desI, in Dti, out desin, out dest, out Di, out Dc, out Dn, thnn);//記述子選択(重み付きランダム)
                    if (!flg) return false;
                }
                else
                {
                    flg = DataProc.DesStR(out msg, desn[0], in desL, in desI, in Dti, out desin, out dest, out Di, out Dc, out Dn, thnn);//記述子選択(ランダム)
                    if (!flg) return false;
                }
                flg = DataProc.BfiI(out msg, in dpth, out itemp, out fi);
                if (!flg) return false;
                lyn = rnd.Next(Lyn.Length);
                if (KRK == 1.0)
                {
                    try
                    {
                        THNM(in Dl, in lni[lyn], in Tcn[lyn], in Fwp[lyn], in Nfp[lyn], in desin, in gdo, in cdr, in err, in NK, in G, in Di, in Dc, in Dn, in fi, in Dt, in dest, in gdt, in Dti, in rwm, in resp, in resi, in desp[0], true, ref ccc);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        if (e.InnerException != null) Console.WriteLine(e.InnerException);
                    }
                }
                else
                {
                    try
                    {
                        THNM(in Dl, in lni[lyn], in Tcn[lyn], in Fwp[lyn], in Nfp[lyn], in desin, in gdo, in cdr, in err, in NK, in G, in KRK, true, in Di, in Dc, in Dn, in fi, in Dt, in dest, in gdt, in Dti, in rwm, in resp, in resi, in desp[0], true, ref ccc);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        if (e.InnerException != null) Console.WriteLine(e.InnerException);
                    }
                }
            }
            return true;
        }
        static private class THNRgx
        {
            static internal readonly Regex r1 = new Regex(@"^任务名\s：\s(.+)$", RegexOptions.CultureInvariant, CommonParam.ts);//ジョブ名
            static internal readonly Regex r1a = new Regex(@"^ジョブ名：(.+)$", RegexOptions.CultureInvariant, CommonParam.ts);//ジョブ名
            static internal readonly Regex r2 = new Regex(@"^記述子ファイルパス\s：\s(.+)$", RegexOptions.CultureInvariant, CommonParam.ts);//記述子ファイルパス
            static internal readonly Regex r2a = new Regex(@"^描述符文件路径：(.+)$", RegexOptions.CultureInvariant, CommonParam.ts);//記述子ファイルパス
            static internal readonly Regex r2b = new Regex(@"^Descriptor\samount\s:\s(\d+)$", RegexOptions.CultureInvariant, CommonParam.ts);//入力記述子個数
            static internal readonly Regex r3 = new Regex(@"^Descriptor\sindex\s\((\d+)\):\s(?:(?>(\d+))(?:,\s)?)+$", RegexOptions.CultureInvariant, CommonParam.ts);//記述子インデックス
            static internal readonly Regex r4 = new Regex(@"^中心化常数\s\((\d+)\)：\s(?:(?>([-+.\dE]+))(?:,\s)?)+$", RegexOptions.CultureInvariant, CommonParam.ts);//中心化定数
            static internal readonly Regex r5 = new Regex(@"^正規化係数\s\((\d+)\)：\s(?:(?>([-+.\dE]+))(?:,\s)?)+$", RegexOptions.CultureInvariant, CommonParam.ts);//正規化係数
            static internal readonly Regex r6 = new Regex(@"^Result\sFile\sPath\s:\s(.+)$", RegexOptions.CultureInvariant, CommonParam.ts);//真データファイルパス
            static internal readonly Regex r6a = new Regex(@"^真データファイルパス：(.+)$", RegexOptions.CultureInvariant, CommonParam.ts);//真データファイルパス
            static internal readonly Regex r7 = new Regex(@"^真数序数\s\((\d+)\)：\s(?:(?>(\d+))(?:,\s)?)+$", RegexOptions.CultureInvariant, CommonParam.ts);//真データインデックス
            static internal readonly Regex r7a = new Regex(@"^真值序数：(\d)+$", RegexOptions.CultureInvariant, CommonParam.ts);//真データインデックス
            static internal readonly Regex r8 = new Regex(@"^Network Generation Parameters =>$", RegexOptions.CultureInvariant, CommonParam.ts);//東方ネットワーク生成パラメータ
            static internal readonly Regex r9 = new Regex(@"^层连接范围\s\((\d+)\):\s(?:\((?>(\d+))(?:,\s)?\),\s)+$", RegexOptions.CultureInvariant, CommonParam.ts);//層連結範囲
            static internal readonly Regex r9a = new Regex(@"^Layer\slink\srange\s:\s(\d+),\s(\d+)$", RegexOptions.CultureInvariant, CommonParam.ts);//層連結範囲
            static internal readonly Regex r10 = new Regex(@"^超層連結率\s\((\d+)\):\s(\((?>([-+.\dE]+))(?:,\s)?\),\s)+$", RegexOptions.CultureInvariant, CommonParam.ts);//超層連結率
            static internal readonly Regex r10a = new Regex(@"^超層連結率：(?:(?>([.\d]+))(?:,\s)?)+$", RegexOptions.CultureInvariant, CommonParam.ts);//超層連結率
            static internal readonly Regex r11 = new Regex(@"^Node\sFunction\sProbability\s\((\d+)\):\s(\((?>(\d+|R))(?:,\s)?\),\s)+$", RegexOptions.CultureInvariant, CommonParam.ts);//ノード関数確率
            static internal readonly Regex r12 = new Regex(@"^东方Network结构\s=>$", RegexOptions.CultureInvariant, CommonParam.ts);//東方ネットワーク構造タイトル
            static internal readonly Regex r13 = new Regex(@"^(\d+)層\((?>(?>(\d+))(?:,\s)?)+\)$", RegexOptions.CultureInvariant, CommonParam.ts);//層/ノード数
            static internal readonly Regex r13a = new Regex(@"^Layer\snumber\s:\s(\d+)$", RegexOptions.CultureInvariant, CommonParam.ts);//層数
            static internal readonly Regex r13b = new Regex(@"^層インデックス\s:\s(\d+)$", RegexOptions.CultureInvariant, CommonParam.ts);//層インデックス
            static internal readonly Regex r13c = new Regex(@"^节点数：(\d+)$", RegexOptions.CultureInvariant, CommonParam.ts);//ノード数
            static internal readonly Regex r14 = new Regex(@"^データ数\s:\s(\d+)$", RegexOptions.CultureInvariant, CommonParam.ts);//真データ数
            static internal readonly Regex r15 = new Regex(@"^Layer\s(\d+)\s:\s(\d+)$", RegexOptions.CultureInvariant, CommonParam.ts);//層情報
            static internal readonly Regex r16 = new Regex(@"^输([入出])层\s\((\d+)\):\s(?:(?>(\d+))(?:,\s)?)+$", RegexOptions.CultureInvariant, CommonParam.ts);//ノード入力/出力層
            static internal readonly Regex r16a = new Regex(@"^输([入出])层\s\(0\):\snull$", RegexOptions.CultureInvariant, CommonParam.ts);//ノード入力層(入力層)/出力層((出力層))
            static internal readonly Regex r17 = new Regex(@"^([入出])力ノード\s\((\d+)\):\s(?:(?>(\d+))(?:,\s)?)+$", RegexOptions.CultureInvariant, CommonParam.ts);//ノード入力/出力ノード
            static internal readonly Regex r17a = new Regex(@"^([入出])力ノード\s\(0\):\snull$", RegexOptions.CultureInvariant, CommonParam.ts);//ノード入力ノード(入力層)/出力ノード(出力層)
            static internal readonly Regex r18 = new Regex(@"^Input\sCoefficient\s\((\d+)\):\s(?:(?>([-+.\dE]+))(?:,\s)?)+$", RegexOptions.CultureInvariant, CommonParam.ts);//ノード入力係数
            static internal readonly Regex r18a = new Regex(@"^Input\sCoefficient\s\(0\):\snull$", RegexOptions.CultureInvariant, CommonParam.ts);//ノード入力係数(入力層)
            static internal readonly Regex r19 = new Regex(@"^学習\s=>$", RegexOptions.CultureInvariant, CommonParam.ts);//学習結果タイトル
            static internal readonly Regex r20 = new Regex(@"^Converge Threshold : $", RegexOptions.CultureInvariant, CommonParam.ts);//収束条件
            static internal readonly Regex r21 = new Regex(@"^数值区间\s:\s(?:(?>([-+.\dE]+))(?:,\s))+$", RegexOptions.CultureInvariant, CommonParam.ts);//データ値域区間
            static internal readonly Regex r21a = new Regex(@"^误差区间：(?:(?>([-+.\dE]+)),\s)+$", RegexOptions.CultureInvariant, CommonParam.ts);//データ値域区間
            static internal readonly Regex r22 = new Regex(@"^誤差範囲\s:\s(?:\((?>([-+.\dE]+)),\s(?>([-+.\dE]+))\))+$", RegexOptions.CultureInvariant, CommonParam.ts);//誤差範囲
            static internal readonly Regex r22a = new Regex(@"^Error\sdefinition\s:\s(?:\((?>([-+.\dE]+)),\s(?>([-+.\dE]+))\),\s)+$", RegexOptions.CultureInvariant, CommonParam.ts);//誤差範囲
            static internal readonly Regex r23 = new Regex(@"^学习次数\s:\s(\d+)$", RegexOptions.CultureInvariant, CommonParam.ts);//学習回数
            static internal readonly Regex r23a = new Regex(@"^学習回数：(\d+)$", RegexOptions.CultureInvariant, CommonParam.ts);//学習回数
            static internal readonly Regex r24 = new Regex(@"^学習率\s:\s([-+.\dE]+)$", RegexOptions.CultureInvariant, CommonParam.ts);//学習率
            static internal readonly Regex r24a = new Regex(@"^学习率：([-+.\dE]+)$", RegexOptions.CultureInvariant, CommonParam.ts);//学習率
            static internal readonly Regex r25 = new Regex(@"^Convergence\s:\s(True|False)$", RegexOptions.CultureInvariant, CommonParam.ts);//学習収束状況
            static internal readonly Regex r26 = new Regex(@"^实际学习次数\s:\s(\d+)$", RegexOptions.CultureInvariant, CommonParam.ts);//実際学習回数
            static internal readonly Regex r27 = new Regex(@"^优化结果\s:\s$", RegexOptions.CultureInvariant, CommonParam.ts);//最適化結果
            static internal readonly Regex r28 = new Regex(@"^(\d+)\s:\s$", RegexOptions.CultureInvariant, CommonParam.ts);//ノードインデックス
            static internal readonly Regex r29 = new Regex(@"^予測\s：\s(?:(?>([-+.\dE]+))(?>\s+))+$", RegexOptions.CultureInvariant, CommonParam.ts);//学習予測結果
            static internal readonly Regex r30 = new Regex(@"^真値\s：\s(?:(?>([-+.\dE]+))(?>\s+))+$", RegexOptions.CultureInvariant, CommonParam.ts);//学習真データ
            static internal readonly Regex r31 = new Regex(@"^検証\s=>\s$", RegexOptions.CultureInvariant, CommonParam.ts);//検証結果タイトル
            static internal readonly Regex r32 = new Regex(@"^検証インデックス\s\((\d+)\):\s(?:(?>(\d+))(?:,\s)?)+$", RegexOptions.CultureInvariant, CommonParam.ts);//検証結果インデックス
            static internal readonly Regex r32a = new Regex(@"^Verification\sdata\samount\s:\s(?:(?>(\d+))(?:,)?)+$", RegexOptions.CultureInvariant, CommonParam.ts);//検証結果数(グループ毎)検証データグループ：
            static internal readonly Regex r32b = new Regex(@"^検証データグループ：$", RegexOptions.CultureInvariant, CommonParam.ts);//検証データグループタイトル
            static internal readonly Regex r32c = new Regex(@"^(?:(\d+),\s)+|(-1)$", RegexOptions.CultureInvariant, CommonParam.ts);//検証データグループ
            static internal readonly Regex r33 = new Regex(@"^PASS\s:\s(True|False)$", RegexOptions.CultureInvariant, CommonParam.ts);//検証状況
            static internal readonly Regex r34 = new Regex(@"^统计量\s:\s$", RegexOptions.CultureInvariant, CommonParam.ts);//検証結果統計量
            static internal readonly Regex r35 = new Regex(@"^Node(\d+)\s:\s$", RegexOptions.CultureInvariant, CommonParam.ts);//ノードインデックス(検証結果統計量)
            static internal readonly Regex r36 = new Regex(@"^EAbs\s=\s([-+.\dE]+),\sE2\s=\s([-+.\dE]+),\scos\(ŷ·y\)\s=\s([-+.\dE]+)$", RegexOptions.CultureInvariant, CommonParam.ts);//誤差絶対値和、誤差二乗和、予測/真値cos(検証結果統計量)
            static internal readonly Regex r37 = new Regex(@"^R28\s=\s([-+.\dE]+),\sR27\s=\s([-+.\dE]+),\sR21\s=\s([-+.\dE]+)$", RegexOptions.CultureInvariant, CommonParam.ts);//R28、R27、R21(検証結果統計量)
            static internal readonly Regex r38 = new Regex(@"^r\s=\s([-+.\dE]+),\sR23\s=\s([-+.\dE]+)$", RegexOptions.CultureInvariant, CommonParam.ts);//r、R23(検証結果統計量)
            static internal readonly Regex r39 = new Regex(@"^検証結果\s:\s$", RegexOptions.CultureInvariant, CommonParam.ts);//検証結果出力
            static internal readonly Regex r40 = new Regex(@"^Prediction\s：\s(?:(-?[-+.\dE]+)\s+)+$", RegexOptions.CultureInvariant, CommonParam.ts);//検証予測結果
            static internal readonly Regex r41 = new Regex(@"^True\svalue\s：\s(?:(-?[-+.\dE]+)\s+)+$", RegexOptions.CultureInvariant, CommonParam.ts);//検証真データ
            static internal readonly Regex r42 = new Regex(@"^ジョブ実行時間\s:\s([-+.\dE]+)\sseconds$", RegexOptions.CultureInvariant, CommonParam.ts);//ジョブ実行時間
            static internal readonly Regex r43 = new Regex(@"^现在时间\s:\s(.+)$", RegexOptions.CultureInvariant, CommonParam.ts);//現在時間
            static internal readonly Regex r44 = new Regex(@"^Normalization\smethod\s:\s(\d)+$", RegexOptions.CultureInvariant, CommonParam.ts);//正規化方法
            static internal readonly Regex r45 = new Regex(@"^函数概率：\((?:(?>(\d+))(?:,\s)?)+\)$", RegexOptions.CultureInvariant, CommonParam.ts);//関数確率
            static internal readonly Regex r46 = new Regex(@"^区間\s:\s(?>([-+.\dE]+),\s)+$", RegexOptions.CultureInvariant, CommonParam.ts);//集計結果グループ区間
            static internal readonly Regex r47 = new Regex(@"^权重\s:\s(?>([-+.\dE]+),\s)+$", RegexOptions.CultureInvariant, CommonParam.ts);//集計結果グループ重さ
            static internal readonly Regex r48 = new Regex(@"^Group\s(\d+)$", RegexOptions.CultureInvariant, CommonParam.ts);//集計結果グループインデックス
            static internal readonly Regex r49 = new Regex(@"^([-+.\dE]+)\s：\s(\d+)/(\d+)$", RegexOptions.CultureInvariant, CommonParam.ts);//集計結果グループ情報
            static internal readonly Regex r50 = new Regex(@"^(?>(\d+),\s)+$", RegexOptions.CultureInvariant, CommonParam.ts);//集計結果ログインデックス
            static internal readonly Regex r51 = new Regex(@"^(?>([-+.\dE]+)(?>,?\s?))+$", RegexOptions.CultureInvariant, CommonParam.ts);//予測CSV、記述子重さ
            static internal readonly Regex r52 = new Regex(@"^(?>([\d]+)\(([\d]+)\)(?>,)?)+$", RegexOptions.CultureInvariant, CommonParam.ts);//予測CSVタイトル
            static internal readonly Regex r53 = new Regex(@"^記述子長さ：([\d]+)$", RegexOptions.CultureInvariant, CommonParam.ts);//記述子重み情報長さ
            static internal readonly Regex r54 = new Regex(@"^权重和：([-+.\dE]+)$", RegexOptions.CultureInvariant, CommonParam.ts);//記述子重さ総和
            static internal readonly Regex r55 = new Regex(@"^LtP：([-+.\dE]+)$", RegexOptions.CultureInvariant, CommonParam.ts);//記述子重み分布対一様分布重み
            static internal readonly Regex r56 = new Regex(@"^Weights\s:\s$", RegexOptions.CultureInvariant, CommonParam.ts);//記述子重さタイトル
            static internal readonly Regex r57 = new Regex(@"^確率降下：([-+.\dE]+)$", RegexOptions.CultureInvariant, CommonParam.ts);//確率降下法の受け入れ確率
        }
    }
    internal class THNLayer
    {
        private int Dtn;//データ数
        private int Ndn;//ノード数
        private int Lyi;//層インデックス
        private THFunc.GenSouKyou[] nf;//ノード
        internal int Nn//ノード数
        {
            get
            {
                return Ndn;
            }
        }
        internal int Ln//層インデックス
        {
            get
            {
                return Lyi;
            }
        }
        internal THNLayer(in int D, int li, in int[] ln, ref List<int>[][] lin, ref List<int>[][] lon, int ft)//同関数生成、Dはデータ数、liは層インデックス、lnは層、linは入力、lonは出力、ftは関数番号
        {
            if (D <= 1) throw new ArgumentOutOfRangeException("D", "THNLayer : Invalid data number.");
            if (ln[li] <= 0) throw new ArgumentOutOfRangeException("nn", "THNLayer : Invalid node number.");
            if (li != 0 && lin.Length != ln[li]) throw new ArgumentOutOfRangeException("lin", "THNLayer : Link length is invalid.");
            if (li != ln.GetUpperBound(0) && lon.Length != ln[li]) throw new ArgumentOutOfRangeException("lon", "THNLayer : Link length is invalid.");
            Dtn = D;
            Ndn = (li == 0) ? (ln[li] + 1) : ln[li];
            Lyi = li;
            int[][] lila = null;
            int[][] lina = null;
            int[][] lola = null;
            int[][] lona = null;
            try
            {
                if (li != 0) LLL(in li, in ln, ref lin, out lila, out lina, true);
                if (li != ln.GetUpperBound(0)) LLL(in li, in ln, ref lon, out lola, out lona, false);
            }
            catch (ArgumentOutOfRangeException aroe)
            {
                Console.WriteLine("{0} => {1}", aroe.ParamName, aroe.Message);
                throw new Exception("THNLayer : Can not create layer.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception("THNLayer : Can not create layer.");
            }
            int cnt;
            nf = new THFunc.GenSouKyou[Ndn];
            for (cnt = 0; cnt < ln[li]; cnt++)
            {
                switch (ft)
                {
                    case 0:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Remilia(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Remilia(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Remilia(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 1:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Youmu(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Youmu(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Youmu(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 2:
                        {
                            throw new ArgumentOutOfRangeException("ft", "THNLayer : Constant layer is invalid.");
                        }
                    case 3:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Flandre(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Flandre(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Flandre(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 4:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Reisen(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Reisen(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Reisen(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 5:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Tei(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Tei(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Tei(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 6:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Satori(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Satori(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Satori(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 7:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Nue(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Nue(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Nue(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 8:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Murasa(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Murasa(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Murasa(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 9:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Yuyuko(D, lola[cnt], lona[cnt], false, double.PositiveInfinity, double.NegativeInfinity);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Yuyuko(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Yuyuko(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    default: throw new ArgumentOutOfRangeException("fni", "THNLayer : Unexpected node function.");
                }
            }
            if (li == 0) nf[ln[0]] = new THFunc.Reimu(D, lola[ln[0]], lona[ln[0]], false);
        }
        internal THNLayer(in int D, int li, in int[] ln, ref List<int>[][] lin, ref List<int>[][] lon, in Random R)//ランダム生成(一様)、Dはデータ数、liは層インデックス、lnは層、linは入力、lonは出力
        {
            if (D <= 1) throw new ArgumentOutOfRangeException("D", "THNLayer : Invalid data number.");
            if (ln[li] <= 0) throw new ArgumentOutOfRangeException("nn", "THNLayer : Invalid node number.");
            if (li != 0 && lin.Length != ln[li]) throw new ArgumentOutOfRangeException("lin", "THNLayer : Link length is invalid.");
            if (li != ln.GetUpperBound(0) && lon.Length != ln[li]) throw new ArgumentOutOfRangeException("lon", "THNLayer : Link length is invalid.");
            Dtn = D;
            Ndn = (li == 0) ? (ln[li] + 1) : ln[li];
            Lyi = li;
            int[][] lila = null;
            int[][] lina = null;
            int[][] lola = null;
            int[][] lona = null;
            try
            {
                if (li != 0) LLL(in li, in ln, ref lin, out lila, out lina, true);
                if (li != ln.GetUpperBound(0)) LLL(in li, in ln, ref lon, out lola, out lona, false);
            }
            catch (ArgumentOutOfRangeException aroe)
            {
                Console.WriteLine("{0} => {1}", aroe.ParamName, aroe.Message);
                throw new Exception("THNLayer : Can not create layer.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception("THNLayer : Can not create layer.");
            }
            int cnt, fni;
            nf = new THFunc.GenSouKyou[Ndn];
            for (cnt = 0; cnt < ln[li]; cnt++)
            {
                fni = R.Next(THFunc.GenSouKyou.FncN);
                switch (fni)
                {
                    case 0:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Remilia(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Remilia(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Remilia(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 1:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Youmu(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Youmu(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Youmu(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 2:
                        {
                            cnt--;
                            continue;
                        }
                    case 3:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Flandre(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Flandre(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Flandre(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 4:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Reisen(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Reisen(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Reisen(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 5:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Tei(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Tei(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Tei(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 6:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Satori(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Satori(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Satori(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 7:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Nue(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Nue(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Nue(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 8:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Murasa(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Murasa(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Murasa(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 9:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Yuyuko(D, lola[cnt], lona[cnt], false, double.PositiveInfinity, double.NegativeInfinity);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Yuyuko(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Yuyuko(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    default: throw new ArgumentOutOfRangeException("fni", "THNLayer : Unexpected node function.");
                }
            }
            if (li == 0) nf[ln[0]] = new THFunc.Reimu(D, lola[ln[0]], lona[ln[0]], false);
        }
        internal THNLayer(in int D, int li, in int[] ln, ref List<int>[][] lin, ref List<int>[][] lon, in Random R, in int[] Fp)//ランダム生成(確率付き)、Dはデータ数、liは層インデックス、lnは層、linは入力、lonは出力、Fpは関数確率(一万分の)
        {
            if (D <= 1) throw new ArgumentOutOfRangeException("D", "THNLayer : Invalid data number.");
            if (ln[li] <= 0) throw new ArgumentOutOfRangeException("nn", "THNLayer : Invalid node number.");
            if (li != 0 && lin.Length != ln[li]) throw new ArgumentOutOfRangeException("lin", "THNLayer : Link length is invalid.");
            if (li != ln.GetUpperBound(0) && lon.Length != ln[li]) throw new ArgumentOutOfRangeException("lon", "THNLayer : Link length is invalid.");
            if (Fp.Length != THFunc.GenSouKyou.FncN) throw new ArgumentOutOfRangeException("Fp", "THNLayer : Invalid probability length.");
            Dtn = D;
            Ndn = (li == 0) ? (ln[li] + 1) : ln[li];
            Lyi = li;
            int[][] lila = null;
            int[][] lina = null;
            int[][] lola = null;
            int[][] lona = null;
            try
            {
                if (li != 0) LLL(in li, in ln, ref lin, out lila, out lina, true);
                if (li != ln.GetUpperBound(0)) LLL(in li, in ln, ref lon, out lola, out lona, false);
            }
            catch (ArgumentOutOfRangeException aroe)
            {
                Console.WriteLine("{0} => {1}", aroe.ParamName, aroe.Message);
                throw new Exception("THNLayer : Can not create layer.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception("THNLayer : Can not create layer.");
            }
            int cnt, cnt2;
            nf = new THFunc.GenSouKyou[Ndn];
            bool flg;
            double[] Fpc = new double[THFunc.GenSouKyou.FncN];
            double Fps = 0.0;
            for (cnt = 0; cnt < Fp.Length; cnt++)
            {
                double dtemp = (double)Fp[cnt];
                Fps += dtemp;
                Fpc[cnt] = dtemp;
            }
            if (Fps == 0.0) throw new ArgumentOutOfRangeException("Fps", "THNLayer : Probability is 0.");
            for (cnt = 1; cnt < Fp.Length; cnt++)
            {
                Fpc[cnt] = Fpc[cnt] + Fpc[cnt - 1];
            }
            double fni;
            for (cnt = 0; cnt < ln[li]; cnt++)
            {
                fni = R.NextDouble() * Fps;
                flg = false;
                for (cnt2 = 0; cnt2 < Fpc.Length; cnt2++)
                {
                    if (fni < Fpc[cnt2])
                    {
                        flg = true;
                        break;
                    }
                }
                if (!flg) throw new ArgumentOutOfRangeException("Fp", "THNLayer : Can not find proper index. Wrong input.");
                switch (cnt2)
                {
                    case 0:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Remilia(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Remilia(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Remilia(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 1:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Youmu(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Youmu(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Youmu(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 2:
                        {
                            cnt--;
                            continue;
                        }
                    case 3:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Flandre(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Flandre(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Flandre(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 4:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Reisen(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Reisen(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Reisen(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 5:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Tei(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Tei(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Tei(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 6:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Satori(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Satori(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Satori(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 7:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Nue(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Nue(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Nue(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 8:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Murasa(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Murasa(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Murasa(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 9:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Yuyuko(D, lola[cnt], lona[cnt], false, double.PositiveInfinity, double.NegativeInfinity);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Yuyuko(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Yuyuko(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    default: throw new ArgumentOutOfRangeException("fni", "THNLayer : Unexpected node function.");
                }
            }
            if (li == 0) nf[ln[0]] = new THFunc.Reimu(D, lola[ln[0]], lona[ln[0]], false);
        }
        internal THNLayer(in int D, in int[] ln, in int li, in int[][] lila, in int[][] lina, in double[][] Nrk, in int[][] lola, in int[][] lona, in int[] Nf)
        {
            if (D <= 1) throw new ArgumentOutOfRangeException("D", "THNLayer : Incorrect data amount.");
            if (li < 0) throw new ArgumentOutOfRangeException("li", "THNLayer : Incorrect layer index.");
            Dtn = D;
            Lyi = li;
            Ndn = lila.Length;
            nf = new THFunc.GenSouKyou[ln[li]];
            int cnt;
            for (cnt = 0; cnt < Nn; cnt++)
            {
                switch (Nf[cnt])
                {
                    case 0:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Remilia(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Remilia(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Remilia(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 1:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Youmu(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Youmu(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Youmu(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 2:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Reimu(D, lola[cnt], lona[cnt], false);
                            else
                            {
                                throw new ArgumentOutOfRangeException("Nf", string.Format("THNLayer : There shouldn't be Reimu node in layer {0}.", li));
                            }
                            break;
                        }
                    case 3:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Flandre(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Flandre(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Flandre(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 4:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Reisen(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Reisen(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Reisen(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 5:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Tei(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Tei(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Tei(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 6:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Satori(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Satori(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Satori(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 7:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Nue(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Nue(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Nue(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 8:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Murasa(D, lola[cnt], lona[cnt], false);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Murasa(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Murasa(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    case 9:
                        {
                            if (li == 0) nf[cnt] = new THFunc.Yuyuko(D, lola[cnt], lona[cnt], false, double.PositiveInfinity, double.NegativeInfinity);
                            else if (li == ln.GetUpperBound(0)) nf[cnt] = new THFunc.Yuyuko(D, lila[cnt], lina[cnt], true);
                            else nf[cnt] = new THFunc.Yuyuko(D, lila[cnt], lina[cnt], lola[cnt], lona[cnt]);
                            break;
                        }
                    default: throw new ArgumentOutOfRangeException("Nf", "THNLayer : Unexpected node function.");
                }
            }
        }
        private void LLL(in int li, in int[] ln, ref List<int>[][] ll, out int[][] la, out int[][] na, bool lin)//連結リスト変換、liは層インデクス、lnは層、llはリスト、laは層配列、naはノード配列、lin trueは入力リスト
        {
            if (li == 0 && lin) throw new ArgumentOutOfRangeException("li/lin", "LLL : No input for input layer.");
            if (li == ln.GetUpperBound(0) && !lin) throw new ArgumentOutOfRangeException("li/lin", "LLL : No output for output layer.");
            if (ll.Length != ln[li]) throw new ArgumentOutOfRangeException("ll", "LLL : Can not verify node number.");
            if (!(li == 0 && !lin))
            {
                la = new int[ll.Length][];
                na = new int[ll.Length][];
            }
            else
            {
                la = new int[ll.Length + 1][];
                na = new int[ll.Length + 1][];
            }
            List<int> lat, nat;
            int lcnt = lin ? li : (ln.GetUpperBound(0) - li);
            int cnt, cnt2, lind;
            for (cnt = 0; cnt < ll.Length; cnt++)//ノード
            {
                if (ll[cnt].Length != lcnt) throw new ArgumentOutOfRangeException("ll", "LLL : Incorrect layer number.");
                lat = new List<int>();
                nat = new List<int>();
                for (cnt2 = 0; cnt2 < lcnt; cnt2++)//層
                {
                    lind = lin ? cnt2 : (li + 1 + cnt2);
                    foreach (int itemp in ll[cnt][cnt2])
                    {
                        lat.Add(lind);
                        nat.Add(itemp);
                    }
                    ll[cnt][cnt2] = null;
                }
                if (li != 0 && lin)
                {
                    lat.Add(0);
                    nat.Add(ln[0]);
                }
                la[cnt] = lat.ToArray();
                na[cnt] = nat.ToArray();
                ll[cnt] = null;
            }
            if (li == 0 && !lin)//定数ノード
            {
                lat = new List<int>();
                nat = new List<int>();
                for (cnt = 1; cnt < ln.Length; cnt++)
                {
                    for (cnt2 = 0; cnt2 < ln[cnt]; cnt2++)
                    {
                        lat.Add(cnt);
                        nat.Add(cnt2);
                    }
                }
                la[ln[0]] = lat.ToArray();
                na[ln[0]] = nat.ToArray();
            }
            ll = null;
        }
        internal THFunc.GenSouKyou N(int i)//ノード
        {
            return nf[i];
        }
        private enum GSKFl : int//関数リスト
        {
            Remilia = 0,
            Youmu = 1,
            Reimu = 2,
            Flandre = 3,
            Reisen = 4,
            Tei = 5,
            Satori = 6,
            Nue = 7,
            Murasa = 8,
            Yuyuko = 9
        }
    }
    internal class THFunc
    {
        static internal int THFStB(string s)
        {
            switch (s)
            {
                case "Remilia":
                case "レミリア":
                    {
                        return 0;
                    }
                case "Youmu":
                case "妖夢":
                    {
                        return 1;
                    }
                case "Reimu":
                case "霊夢":
                    {
                        return 2;
                    }
                case "Flandre":
                case "フランドール":
                    {
                        return 3;
                    }
                case "Reisen":
                case "鈴仙":
                    {
                        return 4;
                    }
                case "Tei":
                case "帝":
                    {
                        return 5;
                    }
                case "Satori":
                case "さとり":
                    {
                        return 6;
                    }
                case "Nue":
                case "ぬえ":
                    {
                        return 7;
                    }
                case "Murasa":
                case "水蜜":
                    {
                        return 8;
                    }
                case "Yuyuko":
                case "幽々子":
                    {
                        return 9;
                    }
                default: return -1;
            }
        }
        internal abstract class GenSouKyou//ノード
        {
            static private readonly string GSK = "幻想郷";
            static internal readonly int FncN = 10;
            private protected readonly int Dtn;//データ数
            private protected double[] Val;//定義値
            private protected double[] CFn;//関数値
            private protected double[] hbb;//偏微分
            private protected readonly int[] nrl;//入力層
            private protected readonly int[] nrn;//入力ノード
            private protected double[] nrk;//入力係数
            private protected readonly int[] srl;//出力層
            private protected readonly int[] srn;//出力ノード
            private protected readonly double GSR;//学習率
            private protected double DGSR;//動的学習率
            private protected double dav;//入力平均値
            private protected double dva;//入力分散
            private protected double bzc;//入力標準偏差
            private protected double dhc;//代表値
            private protected readonly int sr;//入力数
            private protected readonly int sc;//出力数
            private protected int DtnY;//予測用データ数
            private protected double[] ValY;//予測用定義値
            private protected double[] CFnY;//予測用関数値
            internal int SR
            {
                get
                {
                    return sr;
                }
            }
            internal int SC
            {
                get
                {
                    return sc;
                }
            }
            internal object HL
            {
                get
                {
                    return hbb.SyncRoot;
                }
            }
            internal int D
            {
                get
                {
                    return Dtn;
                }
            }
            internal int DY
            {
                get
                {
                    return DtnY;
                }
            }
            internal double HK
            {
                get
                {
                    return dav;
                }
            }
            internal double BS
            {
                get
                {
                    return dva;
                }
            }
            internal double HH
            {
                get
                {
                    return bzc;
                }
            }
            private protected GenSouKyou(int D)
            {
                Dtn = D;
                nrl = null;
                nrn = null;
                srl = null;
                srn = null;
                nrk = null;
                hbb = null;
                Val = new double[D];
                CFn = new double[D];
                sr = 0;
                sc = 0;
            }
            private protected GenSouKyou(int D, int[] nsl, int[] nsn, bool n)
            {
                if (nsl.Length != nsn.Length) throw new ArgumentOutOfRangeException("nsl/nsn", "GenSouKyou : Incorrect connection.");
                Dtn = D;
                if (n)
                {
                    nrl = nsl;
                    nrn = nsn;
                    srl = null;
                    srn = null;
                    nrk = new double[nsl.Length];
                    sr = nsl.Length;
                    sc = 0;
                    hbb = new double[D];
                }
                else
                {
                    nrl = null;
                    nrn = null;
                    srl = nsl;
                    srn = nsn;
                    nrk = null;
                    hbb = null;
                    sr = 0;
                    sc = nsl.Length;
                }
                Val = new double[D];
                CFn = new double[D];
            }
            private protected GenSouKyou(int D, int[] nl, int[] nn, int[] sl, int[] sn)
            {
                if (nl.Length != nn.Length) throw new ArgumentOutOfRangeException("nl/nn", "GenSouKyou : Incorrect input connection.");
                if (sl.Length != sn.Length) throw new ArgumentOutOfRangeException("sl/sn", "GenSouKyou : Incorrect output connection.");
                Dtn = D;
                nrl = nl;
                nrn = nn;
                srl = sl;
                srn = sn;
                nrk = new double[nl.Length];
                Val = new double[D];
                CFn = new double[D];
                hbb = new double[D];
                sr = nl.Length;
                sc = sl.Length;
            }
            internal virtual double F(int i)//関数値(読み)
            {
                if (i >= CFn.Length || i < 0) throw new ArgumentOutOfRangeException("i", "GenSouKyou (F) : Invalid index.");
                return CFn[i];
            }
            internal virtual double[] F()//関数値(読み)
            {
                return CFn;
            }
            internal void F(double d, int i)//関数値(書き)
            {
                if (i >= CFn.Length || i < 0) throw new ArgumentOutOfRangeException("i", "GenSouKyou (F) : Invalid index.");
                CFn[i] = d;
            }
            internal double[] K()//入力係数配列(読み)
            {
                return nrk;
            }
            internal void K(double[] kin)//入力係数配列(書き)
            {
                if (kin.Length != kin.Length) throw new ArgumentOutOfRangeException("kin", "GenSouKyou (K) : Invalid coefficient length,");
                nrk = kin;
            }
            internal double K(int i)//入力係数(読み)
            {
                if (i >= sr || i < 0) throw new ArgumentOutOfRangeException("i", "GenSouKyou (NRN) : Invalid index.");
                return nrk[i];
            }
            internal void K(int i, double kin)//入力係数(書き)
            {
                if (i >= sr || i < 0) throw new ArgumentOutOfRangeException("i", "GenSouKyou (NRN) : Invalid index.");
                nrk[i] = kin;
            }
            internal double B(int i)//偏微分(読み)
            {
                if (i >= hbb.Length || i < 0) throw new ArgumentOutOfRangeException("i", "GenSouKyou (B) : Invalid index.");
                return hbb[i];
            }
            internal void B(double d, int i)//偏微分(書き)
            {
                if (i >= hbb.Length || i < 0) throw new ArgumentOutOfRangeException("i", "GenSouKyou (B) : Invalid index.");
                hbb[i] = d;
            }
            internal void Bs(double d, int i)//偏微分(更新)
            {
                if (i >= hbb.Length || i < 0) throw new ArgumentOutOfRangeException("i", "GenSouKyou (B) : Invalid index.");
                hbb[i] += d;
            }
            internal int NRL(int i)//入力層
            {
                if (i >= sr || i < 0) throw new ArgumentOutOfRangeException("i", "GenSouKyou (NRL) : Invalid index.");
                return nrl[i];
            }
            internal int NRN(int i)//入力ノード
            {
                if (i >= sr || i < 0) throw new ArgumentOutOfRangeException("i", "GenSouKyou (NRN) : Invalid index.");
                return nrn[i];
            }
            internal int SRL(int i)//出力層
            {
                if (i >= sc || i < 0) throw new ArgumentOutOfRangeException("i", "GenSouKyou (SRL) : Invalid index.");
                return srl[i];
            }
            internal int SRN(int i)//出力ノード
            {
                if (i >= sc || i < 0) throw new ArgumentOutOfRangeException("i", "GenSouKyou (SRN) : Invalid index.");
                return srn[i];
            }
            internal void YSI(int D)//予測初期化
            {
                DtnY = D;
                ValY = new double[D];
                CFnY = new double[D];
            }
            internal bool AvgC(out string msg)//入力平均値計算
            {
                if (Val == null || Val.Length != Dtn)
                {
                    msg = "GenSouKyou (AvgC) : Invalid input.";
                    return false;
                }
                dav = 0.0;
                for (int cnt = 0; cnt < Dtn; cnt++)
                {
                    dav += Val[cnt];
                }
                dav /= Dtn;
                msg = "Average successfully calculated.";
                return true;
            }
            internal bool VarC(out string msg)//入力分散計算
            {
                if (Val == null || Val.Length != Dtn)
                {
                    msg = "GenSouKyou (VarC) : Invalid input.";
                    return false;
                }
                dva = 0.0;
                double dtemp;
                for (int cnt = 0; cnt < Dtn; cnt++)
                {
                    dtemp = Val[cnt] - dav;
                    dav += dtemp * dtemp;
                }
                dva /= Dtn;
                msg = "Variance successfully calculated.";
                return true;
            }
            internal bool DevC(out string msg)//入力標準偏差計算
            {
                if (dva < 0.0)
                {
                    msg = "GenSouKyou (DevC) : Invalid deviance.";
                    return false;
                }
                bzc = Math.Sqrt(dva);
                msg = "Deviance successfully calculated.";
                return true;
            }
            internal virtual double FY(int i)//関数値(読み)(予測用)
            {
                if (i >= CFnY.Length || i < 0) throw new ArgumentOutOfRangeException("i", "GenSouKyou (FY) : Invalid index.");
                return CFnY[i];
            }
            internal abstract string NM { get; }//ノード関数名
            internal abstract void TGs(double[] d);//定義値(書き)
            internal abstract void TGs(double d, int i);//定義値(書き)
            internal abstract double KSCn(int i);//更新値
            internal abstract double DSCn(int i);//微分値
            internal abstract bool RepC(string msg);//代表値計算
            internal virtual bool AVDK(string msg)//平均・分散・標準偏差更新
            {
                if(Val==null||Val.Length<1)
                {
                    msg = "Invalid input data.";
                    return false;
                }
                int cnt;
                dav = 0.0;
                for(cnt=0;cnt<Dtn;cnt++)
                {
                    dav += Val[cnt];
                }
                dav /= Dtn;
                dva = 0.0;
                double dtemp;
                for(cnt=0;cnt<Dtn;cnt++)
                {
                    dtemp = Val[cnt] - dav;
                    dva += dtemp * dtemp;
                }
                dva /= Dtn;
                bzc = Math.Sqrt(dva);
                msg = "Successfully calculated.";
                return true;
            }
            internal abstract bool DGK(string msg);//動的学習率更新
            internal abstract bool KS2(string msg);//更新2
            internal abstract void TGsY(double d, int i);//定義値(書き)(予測用)
        }
        internal abstract class HakuReiJinJya : GenSouKyou//定数族
        {
            static private readonly string Cm = "博麗神社";
            private protected HakuReiJinJya(int D) : base(D) { }
            private protected HakuReiJinJya(int D, int[] nsl, int[] nsn, bool n) : base(D, nsl, nsn, n) { }
            private protected HakuReiJinJya(int D, int[] nl, int[] nn, int[] sl, int[] sn) : base(D, nl, nn, sl, sn) { }
        }
        internal abstract class KouMaKan : GenSouKyou//指数関数族
        {
            static private readonly string Cm = "紅魔館";
            private protected KouMaKan(int D) : base(D) { }
            private protected KouMaKan(int D, int[] nsl, int[] nsn, bool n) : base(D, nsl, nsn, n) { }
            private protected KouMaKan(int D, int[] nl, int[] nn, int[] sl, int[] sn) : base(D, nl, nn, sl, sn) { }
        }
        internal abstract class HakuGyokuRou : GenSouKyou//線形関数族
        {
            static private readonly string Cm = "白玉楼";
            private protected HakuGyokuRou(int D) : base(D) { }
            private protected HakuGyokuRou(int D, int[] nsl, int[] nsn, bool n) : base(D, nsl, nsn, n) { }
            private protected HakuGyokuRou(int D, int[] nl, int[] nn, int[] sl, int[] sn) : base(D, nl, nn, sl, sn) { }
        }
        internal abstract class EiEnTei : GenSouKyou//冪関数族
        {
            static private readonly string Cm = "永遠亭";
            private protected EiEnTei(int D) : base(D) { }
            private protected EiEnTei(int D, int[] nsl, int[] nsn, bool n) : base(D, nsl, nsn, n) { }
            private protected EiEnTei(int D, int[] nl, int[] nn, int[] sl, int[] sn) : base(D, nl, nn, sl, sn) { }
        }
        internal abstract class ChiReiDen : GenSouKyou//三角関数族
        {
            static private readonly string Cm = "地霊殿";
            private protected ChiReiDen(int D) : base(D) { }
            private protected ChiReiDen(int D, int[] nsl, int[] nsn, bool n) : base(D, nsl, nsn, n) { }
            private protected ChiReiDen(int D, int[] nl, int[] nn, int[] sl, int[] sn) : base(D, nl, nn, sl, sn) { }
        }
        internal abstract class MyouRenJi : GenSouKyou//擬似分布関数
        {
            static private readonly string Cm = "命蓮寺";
            private protected MyouRenJi(int D) : base(D) { }
            private protected MyouRenJi(int D, int[] nsl, int[] nsn, bool n) : base(D, nsl, nsn, n) { }
            private protected MyouRenJi(int D, int[] nl, int[] nn, int[] sl, int[] sn) : base(D, nl, nn, sl, sn) { }
        }
        internal sealed class Reimu : HakuReiJinJya//定数 y=a
        {
            static private readonly string Mj = "博麗";
            static private readonly string Nm = "霊夢";
            static private readonly string Mje = "Hakurei";
            static private readonly string Nme = "Reimu";
            static internal readonly int THFI = 2;
            internal override sealed string NM
            {
                get
                {
                    return Nm;
                }
            }
            internal Reimu(int D) : base(D)
            {
                Val = null;
                CFn = new double[1];
                CFn[0] = 1.0;
                hbb = null;
            }
            internal Reimu(int D, int[] nsl, int[] nsn, bool n) : base(D, nsl, nsn, n)
            {
                if (n) throw new ArgumentOutOfRangeException("n", "Reimu : No imput.");
                Val = null;
                CFn = new double[1];
                CFn[0] = 1.0;
                hbb = null;
            }
            internal Reimu(int D, double f) : base(D)
            {
                Val = null;
                CFn = new double[1];
                CFn[0] = f;
                hbb = null;
            }
            internal Reimu(int D, int[] nsl, int[] nsn, bool n, double f) : base(D, nsl, nsn, n)
            {
                if (n) throw new ArgumentOutOfRangeException("n", "Reimu : No imput.");
                Val = null;
                CFn = new double[1];
                CFn[0] = f;
                hbb = null;
            }
            internal override double F(int i)//関数値(読み)
            {
                return CFn[0];
            }
            internal override void TGs(double[] d)
            {
                throw new MethodAccessException("Reimu : No domain.");
            }
            internal override void TGs(double d, int i)
            {
                throw new MethodAccessException("Reimu : No domain.");
            }
            internal override double KSCn(int i)
            {
                throw new MethodAccessException("Reimu : No domain.");
            }
            internal override double DSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Reimu : Invalid index.");
                return 0.0;
            }
            static internal double DS2C()
            {
                return 0.0;
            }
            internal override bool RepC(string msg)
            {
                msg = "Reimu(RepC) : No representative value.";
                return false;
            }
            internal override bool AVDK(string msg)
            {
                msg = "Reimu (AVDK) : Can not calculate.";
                return false;
            }
            internal override bool KS2(string msg)
            {
                msg = "Reimu (KS2) : Can not update.";
                return false;
            }
            internal override bool DGK(string msg)
            {
                msg = "Reimu (DGK) : Can not update.";
                return false;
            }
            internal override void TGsY(double d, int i)
            {
                throw new MethodAccessException("Reimu : No domain.");
            }
            internal override double FY(int i)//関数値(読み)(予測用)
            {
                return CFn[0];
            }
        }
        internal sealed class Remilia : KouMaKan//指数関数、y=e^x
        {
            static private readonly string Mj = "スカーレット";
            static private readonly string Nm = "レミリア";
            static private readonly string Mje = "Scarlet";
            static private readonly string Nme = "Remilia";
            static internal readonly int THFI = 0;
            private double dhc2;//代表値2
            private double dThs = 7.0;//定義上限
            private double cdThs;//値上限
            private double dThx = -5.0;//定義下限
            private double cdThx;//値下限
            internal override sealed string NM
            {
                get
                {
                    return Nm;
                }
            }
            internal double cdTs
            {
                get
                {
                    return cdThs;
                }
                set
                {
                    if (value <= 0.0) throw new ArgumentOutOfRangeException("cdTs", "Remilia : Codomain threshold is out of range");
                    cdThs = value;
                    double dstemp = dThs;
                    dThs = Math.Log(value);
                    if (dThs < dThx) throw new ArgumentOutOfRangeException("cdTs", "Remilia : Domain region is invalid.");
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] > dstemp && Val[cnt] < dThs) CFn[cnt] = KSC(Val[cnt]);
                        else if (Val[cnt] > dThs) CFn[cnt] = value;
                    }
                }
            }
            internal double dTx
            {
                get
                {
                    return dThx;
                }
                set
                {
                    if (value > dThs) throw new ArgumentOutOfRangeException("dTx", "Remilia : Domain region is invalid.");
                    double dxtemp = dThx;
                    dThx = value;
                    cdThx = KSC(value);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] < dxtemp && Val[cnt] > dThx) CFn[cnt] = KSC(Val[cnt]);
                        else if (Val[cnt] < value) CFn[cnt] = 0.0;
                    }
                }
            }
            internal Remilia(int D) : base(D)
            {
                cdThs = KSC(dThs);
                cdThx = KSC(dThx);
            }
            internal Remilia(int D, int[] nsl, int[] nsn, bool n) : base(D, nsl, nsn, n)
            {
                cdThs = KSC(dThs);
                cdThx = KSC(dThx);
            }
            internal Remilia(int D, int[] nl, int[] nn, int[] sl, int[] sn) : base(D, nl, nn, sl, sn)
            {
                cdThs = KSC(dThs);
                cdThx = KSC(dThx);
            }
            internal override void TGs(double[] d)
            {
                if (d.Length != Dtn) throw new ArgumentOutOfRangeException("d", "Remilia : Wrong input length.");
                Val = d;
                for (int cnt = 0; cnt < Dtn; cnt++)
                {
                    if (Val[cnt] >= dThs) CFn[cnt] = cdThs;
                    else if (Val[cnt] <= dThx) CFn[cnt] = 0.0;
                    else CFn[cnt] = KSC(Val[cnt]);
                }
            }
            internal override void TGs(double d, int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Remilia : Invalid index.");
                Val[i] = d;
                if (Val[i] >= dThs) CFn[i] = cdThs;
                else if (Val[i] <= dThx) CFn[i] = 0.0;
                else CFn[i] = KSC(Val[i]);
            }
            static internal double KSC(double d)
            {
                return Math.Exp(d);
            }
            internal override double KSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Remilia : Invalid index.");
                //if (Val[i] > dThs) return dThs;
                //if (Val[i] < dThx) return dThx;
                if (hbb[i] == 0.0) return Val[i];
                else if (hbb[i] < 0.0)
                {
                    if (Val[i] > dThs) return Val[i];
                    else if (Val[i] < 0.0)
                    {
                        double dtemp;
                        if (hbb[i] <= -1.0) dtemp = Val[i] + 0.999;
                        else dtemp = Val[i] - 0.999 * hbb[i];
                        if (dtemp < dThx) return dThx;
                        return (dtemp > 0.0) ? 0.0 : dtemp;
                    }
                    else
                    {
                        double dtemp = Math.Pow(Math.PI, Val[i]); ;
                        if (Val[i] < 2.0)
                        {
                            if (Math.Abs(hbb[i]) >= dtemp) dtemp = Val[i] + 0.3434;
                            else dtemp = Val[i] - 0.0495 * hbb[i] / dtemp;
                            if (dtemp >= dThs) return dThs;
                            return (dtemp >= 2.0) ? 2.0 : dtemp;
                        }
                        else if (Val[i] < 4.0)
                        {
                            if (Math.Abs(hbb[i]) >= dtemp) dtemp = Val[i] + 0.0999;
                            else dtemp = Val[i] - 0.0999 * hbb[i] / dtemp;
                            if (dtemp >= dThs) return dThs;
                            return (dtemp >= 4.0) ? 4.0 : dtemp;
                        }
                        else if (Val[i] < 6.0)
                        {
                            if (Math.Abs(hbb[i]) >= dtemp) dtemp = Val[i] + 0.0495;
                            else dtemp = Val[i] - 0.0495 * hbb[i] / dtemp;
                            if (dtemp >= dThs) return dThs;
                            return (dtemp >= 6.0) ? 6.0 : dtemp;
                        }
                        else
                        {
                            if (Math.Abs(hbb[i]) >= dtemp) dtemp = Val[i] + 0.00514;
                            else dtemp = Val[i] - 0.00514 * hbb[i] / dtemp;
                            if (dtemp >= dThs) return dThs;
                            return dtemp;
                        }
                    }
                }
                else
                {
                    if (Val[i] < dThx) return Val[i];
                    else if (Val[i] <= 0.0)
                    {
                        double dtemp;
                        if (hbb[i] >= 1.0) dtemp = Val[i] - 0.0514;
                        else dtemp = Val[i] - 0.0514 * hbb[i];
                        return (dtemp < dThx) ? dThx : dtemp;
                    }
                    else
                    {
                        double dtemp = Math.Pow(Math.PI, Val[i]);
                        if (Val[i] <= 2.0)
                        {
                            if (hbb[i] >= dtemp) dtemp = Val[i] - 0.514;
                            else dtemp = Val[i] - 0.514 * hbb[i] / dtemp;
                            if (dtemp >= dThs) return dThs;
                            return (dtemp <= 0.0) ? 0.0 : dtemp;
                        }
                        else if (Val[i] <= 4.0)
                        {
                            if (hbb[i] >= dtemp) dtemp = Val[i] - 0.19;
                            else dtemp = Val[i] - 0.19 * hbb[i] / dtemp;
                            if (dtemp >= dThs) return dThs;
                            return (dtemp <= 2.0) ? 2.0 : dtemp;
                        }
                        else if (Val[i] <= 6.0)
                        {
                            if (hbb[i] >= dtemp) dtemp = Val[i] - 0.104;
                            else dtemp = Val[i] - 0.104 * hbb[i] / dtemp;
                            if (dtemp >= dThs) return dThs;
                            return (dtemp <= 4.0) ? 4.0 : dtemp;
                        }
                        else
                        {
                            if (hbb[i] >= dtemp) dtemp = Val[i] - 0.0495;
                            else dtemp = Val[i] - 0.0495 * hbb[i] / dtemp;
                            if (dtemp >= dThs) return dThs;
                            return dtemp;
                        }
                    }
                }
            }
            static internal double DSC(double d)
            {
                return Math.Exp(d);
            }
            internal override double DSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Remilia : Invalid index.");
                if (hbb[i] == 0.0) return 0.0;
                bool Po = (hbb[i] < 0.0) ? true : false;
                if (Val[i] > dThs)
                {
                    if (Po) return 0.0;
                    else return cdThs;
                }
                else if (Val[i] < dThx)
                {
                    if (Po) return cdThx;
                    else return 0.0;
                }
                return CFn[i];
            }
            internal static double D2SC(double d)
            {
                return Math.Exp(d);
            }
            internal override bool RepC(string msg)
            {
                if (Val == null || Val.Length < 1)
                {
                    msg = "Invalid input data.";
                    return false;
                }
                int cnt;
                dhc = 0.0;
                dhc2 = double.MaxValue;
                for (cnt = 0; cnt < Dtn; cnt++)
                {
                    if (Val[cnt] > dhc) dhc = Val[cnt];
                    if (Val[cnt] < dhc2) dhc2 = Val[cnt];
                }
                msg = "Successfully found.";
                return true;
            }
            internal override void TGsY(double d, int i)
            {
                if (i >= DtnY || i < 0) throw new ArgumentOutOfRangeException("i", "Remilia : Invalid index.");
                ValY[i] = d;
                if (ValY[i] >= dThs) CFnY[i] = cdThs;
                else if (ValY[i] <= dThx) CFnY[i] = 0.0;
                else CFnY[i] = KSC(ValY[i]);
            }
            internal override bool DGK(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool KS2(string msg)
            {
                throw new NotImplementedException();
            }
        }
        internal sealed class Flandre : KouMaKan//シグモイド関数、y=1/(1+e^(-ax))
        {
            static private readonly string Mj = "スカーレット";
            static private readonly string Nm = "フランドール";
            static private readonly string Mje = "Scarlet";
            static private readonly string Nme = "Flandre";
            static internal readonly int THFI = 3;
            private double atv = 1.0;//ゲインa
            private double dThs = 10.0;//定義上限
            private double DThs;//上限導関数
            private double dThx = -10.0;//定義下限
            private double DThx;//下限導関数
            internal override sealed string NM
            {
                get
                {
                    return Nm;
                }
            }
            internal double dTs
            {
                get
                {
                    return dThs;
                }
                set
                {
                    if (value < dThx) throw new ArgumentOutOfRangeException("dTs", "Flandre : Domain region is invalid.");
                    double dstemp = dThs;
                    dThs = value;
                    DThs = DSC(value, atv);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] > dstemp && Val[cnt] < dThs) CFn[cnt] = KSC(Val[cnt], atv);
                        else if (Val[cnt] > value) CFn[cnt] = 1.0;
                    }
                }
            }
            internal double dTx
            {
                get
                {
                    return dThx;
                }
                set
                {
                    if (value > dThs) throw new ArgumentOutOfRangeException("dTx", "Flandre : Domain region is invalid.");
                    double dxtemp = dThx;
                    dThx = value;
                    DThx = DSC(value, atv);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] < dxtemp && Val[cnt] > dThx) CFn[cnt] = KSC(Val[cnt], atv);
                        else if (Val[cnt] < value) CFn[cnt] = 0.0;
                    }
                }
            }
            internal double a
            {
                get
                {
                    return atv;
                }
                set
                {
                    atv = a;
                    DThs = DSC(dThs, atv);
                    DThx = DSC(dThx, atv);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] < dThs && Val[cnt] > dThx) CFn[cnt] = KSC(Val[cnt], atv);
                    }
                }
            }
            internal Flandre(int D) : base(D)
            {
                DThs = DSC(dThs, atv);
                DThx = DSC(dThx, atv);
            }
            internal Flandre(int D, int[] nsl, int[] nsn, bool n) : base(D, nsl, nsn, n)
            {
                DThs = DSC(dThs, atv);
                DThx = DSC(dThx, atv);
            }
            internal Flandre(int D, int[] nl, int[] nn, int[] sl, int[] sn) : base(D, nl, nn, sl, sn)
            {
                DThs = DSC(dThs, atv);
                DThx = DSC(dThx, atv);
            }
            internal Flandre(int D, double a) : base(D)
            {
                atv = a;
                DThs = DSC(dThs, atv);
                DThx = DSC(dThx, atv);
            }
            internal Flandre(int D, int[] nsl, int[] nsn, bool n, double a) : base(D, nsl, nsn, n)
            {
                atv = a;
                DThs = DSC(dThs, atv);
                DThx = DSC(dThx, atv);
            }
            internal Flandre(int D, int[] nl, int[] nn, int[] sl, int[] sn, double a) : base(D, nl, nn, sl, sn)
            {
                atv = a;
                DThs = DSC(dThs, atv);
                DThx = DSC(dThx, atv);
            }
            internal override void TGs(double[] d)
            {
                if (d.Length != Dtn) throw new ArgumentOutOfRangeException("d", "Flandre : Wrong input length.");
                Val = d;
                for (int cnt = 0; cnt < Dtn; cnt++)
                {
                    if (Val[cnt] >= dThs) CFn[cnt] = 1.0;
                    else if (Val[cnt] <= dThx) CFn[cnt] = 0.0;
                    else CFn[cnt] = KSC(Val[cnt], atv);
                }
            }
            internal override void TGs(double d, int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Flandre : Invalid index.");
                Val[i] = d;
                if (Val[i] >= dThs) CFn[i] = 1.0;
                else if (Val[i] <= dThx) CFn[i] = 0.0;
                else CFn[i] = KSC(Val[i], atv);
            }
            static internal double KSC(double d, double a)
            {
                return 1.0 / (1.0 + Math.Exp(-a * d));
            }
            internal override double KSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Flandre : Invalid index.");
                //if (Val[i] > dThs) return dThs;
                //if (Val[i] < dThx) return dThx;
                if (hbb[i] == 0.0) return Val[i];
                else if (hbb[i] < 0.0)
                {
                    if (Val[i] > dThs) return Val[i];
                    else
                    {
                        double dtemp;
                        if (Val[i] <= -3.0)
                        {
                            if (hbb[i] < -1.0) dtemp = Val[i] + atv / 0.495;
                            else dtemp = Val[i] - hbb[i] * atv / 0.495;
                            if (Val[i] <= dThx) return dThx;
                            if (Val[i] >= dThs) return dThs;
                            return (dtemp >= -1.0) ? -1.0 : dtemp;
                        }
                        else if (Val[i] <= -1.0)
                        {
                            if (hbb[i] < -1.0) dtemp = Val[i] + atv / 4.95;
                            else dtemp = Val[i] - hbb[i] * atv / 4.95;
                            if (Val[i] <= dThx) return dThx;
                            if (Val[i] >= dThs) return dThs;
                            return (dtemp >= 1.0) ? 1.0 : dtemp;
                        }
                        else if (Val[i] <= 1.0)
                        {
                            if (hbb[i] < -1.0) dtemp = Val[i] + atv / 49.5;
                            else dtemp = Val[i] - hbb[i] * atv / 49.5;
                            if (Val[i] <= dThx) return dThx;
                            if (Val[i] >= dThs) return dThs;
                            return (dtemp >= 3.0) ? 3.0 : dtemp;
                        }
                        else if (Val[i] <= 3.0)
                        {
                            if (hbb[i] < -1.0) dtemp = Val[i] + atv / 4.95;
                            else dtemp = Val[i] - hbb[i] * atv / 4.95;
                            if (Val[i] <= dThx) return dThx;
                            if (Val[i] >= dThs) return dThs;
                            return (dtemp >= 6.0) ? 6.0 : dtemp;
                        }
                        else
                        {
                            if (hbb[i] < -1.0) dtemp = Val[i] + atv / 0.495;
                            else dtemp = Val[i] - hbb[i] * atv / 0.495;
                            if (Val[i] <= dThx) return dThx;
                            if (Val[i] >= dThs) return dThs;
                            return dtemp;
                        }
                    }
                }
                else
                {
                    if (Val[i] < dThx) return Val[i];
                    else
                    {
                        double dtemp;
                        if (Val[i] >= 3.0)
                        {
                            if (hbb[i] > 1.0) dtemp = Val[i] - atv / 0.495;
                            else dtemp = Val[i] - hbb[i] * atv / 0.495;
                            if (Val[i] <= dThx) return dThx;
                            if (Val[i] >= dThs) return dThs;
                            return (dtemp <= 1.0) ? 1.0 : dtemp;
                        }
                        else if (Val[i] >= 1.0)
                        {
                            if (hbb[i] < -1.0) dtemp = Val[i] - atv / 4.95;
                            else dtemp = Val[i] - hbb[i] * atv / 4.95;
                            if (Val[i] <= dThx) return dThx;
                            if (Val[i] >= dThs) return dThs;
                            return (dtemp <= -1.0) ? -1.0 : dtemp;
                        }
                        else if (Val[i] >= -1.0)
                        {
                            if (hbb[i] < -1.0) dtemp = Val[i] - atv / 49.5;
                            else dtemp = Val[i] - hbb[i] * atv / 49.5;
                            if (Val[i] <= dThx) return dThx;
                            if (Val[i] >= dThs) return dThs;
                            return (dtemp <= -3.0) ? -3.0 : dtemp;
                        }
                        else if (Val[i] >= -3.0)
                        {
                            if (hbb[i] < -1.0) dtemp = Val[i] - atv / 4.95;
                            else dtemp = Val[i] - hbb[i] * atv / 4.95;
                            if (Val[i] <= dThx) return dThx;
                            if (Val[i] >= dThs) return dThs;
                            return (dtemp <= -6.0) ? -6.0 : dtemp;
                        }
                        else
                        {
                            if (hbb[i] < -1.0) dtemp = Val[i] - atv / 0.495;
                            else dtemp = Val[i] - hbb[i] * atv / 0.495;
                            if (Val[i] <= dThx) return dThx;
                            if (Val[i] >= dThs) return dThs;
                            return dtemp;
                        }
                    }
                }
            }
            static internal double DSC(double d, double a)
            {
                double dtemp = KSC(d, a);
                return a * dtemp * (1.0 - dtemp);
            }
            internal double DSCn()
            {
                if (Val[0] > dThs || Val[0] < dThx) return 0.0;
                return DSC(Val[0], atv);
            }
            internal override double DSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Flandre : Invalid index.");
                if (hbb[i] == 0.0) return 0.0;
                bool Po = (hbb[i] < 0.0) ? true : false;
                if (Val[i] > dThs)
                {
                    if (Po) return 0.0;
                    else return DThs;
                }
                else if (Val[i] < dThx)
                {
                    if (Po) return DThx;
                    else return 0.0;
                }
                return DSC(Val[i], atv);
            }
            internal static double D2SC(double d, double a)
            {
                double dtemp = KSC(d, a);
                return a * a * dtemp * (1.0 - dtemp) * (1.0 - 2.0 * dtemp);
            }
            internal override void TGsY(double d, int i)
            {
                if (i >= DtnY || i < 0) throw new ArgumentOutOfRangeException("i", "Flandre : Invalid index.");
                ValY[i] = d;
                if (ValY[i] >= dThs) CFnY[i] = 1.0;
                else if (ValY[i] <= dThx) CFnY[i] = 0.0;
                else CFnY[i] = KSC(ValY[i], atv);
            }
            internal override bool RepC(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool DGK(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool KS2(string msg)
            {
                throw new NotImplementedException();
            }
        }
        internal sealed class Youmu : HakuGyokuRou//ReLU/ランプ関数、y=kx
        {
            static private readonly string Mj = "魂魄";
            static private readonly string Nm = "妖夢";
            static private readonly string Mje = "KonPaku";
            static private readonly string Nme = "Youmu";
            static internal readonly int THFI = 1;
            private double kz = 0.0;//係数(左)
            private double ky = 1.0;//係数(右)
            private double dThs = 1000.0;//定義上限
            private double cdThs;//上限値
            private double dThx = 0.0;//定義下限
            private double cdThx;//下限値
            internal override sealed string NM
            {
                get
                {
                    return Nm;
                }
            }
            internal double dTs
            {
                get
                {
                    return dThs;
                }
                set
                {
                    if (value < 0.0) throw new ArgumentOutOfRangeException("dTs", "Youmu : Domain region is invalid.");
                    double dstemp = dThs;
                    dThs = value;
                    cdThs = KSC(value, kz, ky);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] > dstemp && Val[cnt] < dThs) CFn[cnt] = KSC(Val[cnt], kz, ky);
                        if (Val[cnt] > value) CFn[cnt] = cdThs;
                    }
                }
            }
            internal double dTx
            {
                get
                {
                    return dThx;
                }
                set
                {
                    if (value > 0.0) throw new ArgumentOutOfRangeException("dTx", "Youmu : Domain region is invalid.");
                    double dxtemp = dThx;
                    dThx = value;
                    cdThx = KSC(value, kz, ky);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] < dxtemp && Val[cnt] > dThx) CFn[cnt] = KSC(Val[cnt], kz, ky);
                        if (Val[cnt] < value) CFn[cnt] = cdThx;
                    }
                }
            }
            internal double z
            {
                get
                {
                    return kz;
                }
                set
                {
                    if (value == 0.0 && ky == 0.0) throw new ArgumentOutOfRangeException("z", "Youmu : Both gradient is 0.");
                    kz = value;
                    cdThx = KSC(dThx, kz, ky);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] <= dThx) CFn[cnt] = cdThx;
                        else if (Val[cnt] < 0) CFn[cnt] = KSC(Val[cnt], kz, ky);
                    }
                }
            }
            internal double y
            {
                get
                {
                    return ky;
                }
                set
                {
                    if (value == 0.0 && kz == 0.0) throw new ArgumentOutOfRangeException("y", "Youmu : Both gradient is 0.");
                    ky = value;
                    cdThs = KSC(dThs, kz, ky);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] >= dThs) CFn[cnt] = cdThs;
                        else if (Val[cnt] > 0) CFn[cnt] = KSC(Val[cnt], kz, ky);
                    }
                }
            }
            internal Youmu(int D) : base(D)
            {
                cdThs = dThs;
                cdThx = 0.0;
            }
            internal Youmu(int D, int[] nsl, int[] nsn, bool n) : base(D, nsl, nsn, n)
            {
                cdThs = dThs;
                cdThx = 0.0;
            }
            internal Youmu(int D, int[] nl, int[] nn, int[] sl, int[] sn) : base(D, nl, nn, sl, sn)
            {
                cdThs = dThs;
                cdThx = 0.0;
            }
            internal Youmu(int D, double z, double y) : base(D)
            {
                if (z == 0.0 && y == 0.0) throw new ArgumentOutOfRangeException("z/y", "Youmu : Both gradient is 0.");
                kz = z;
                ky = y;
                cdThs = dThs * y;
                cdThx = 0.0;
            }
            internal Youmu(int D, int[] nsl, int[] nsn, bool n, double z, double y) : base(D, nsl, nsn, n)
            {
                if (z == 0.0 && y == 0.0) throw new ArgumentOutOfRangeException("z/y", "Youmu : Both gradient is 0.");
                kz = z;
                ky = y;
                cdThs = dThs * y;
                cdThx = 0.0;
            }
            internal Youmu(int D, int[] nl, int[] nn, int[] sl, int[] sn, double z, double y) : base(D, nl, nn, sl, sn)
            {
                if (z == 0.0 && y == 0.0) throw new ArgumentOutOfRangeException("z/y", "Youmu : Both gradient is 0.");
                kz = z;
                ky = y;
                cdThs = dThs * y;
                cdThx = 0.0;
            }
            internal Youmu(int D, double z, double y, double s, double x) : base(D)
            {
                if (s < 0.0 || x > 0.0) throw new ArgumentOutOfRangeException("s/x", "Youmu : Region is invalid.");
                if (z == 0.0 && y == 0.0) throw new ArgumentOutOfRangeException("z/y", "Youmu : Both gradient is 0.");
                kz = z;
                ky = y;
                dThs = s;
                dThx = x;
                cdThs = s * y;
                cdThx = x * z;
            }
            internal Youmu(int D, int[] nsl, int[] nsn, bool n, double z, double y, double s, double x) : base(D, nsl, nsn, n)
            {
                if (s < 0.0 || x > 0.0) throw new ArgumentOutOfRangeException("s/x", "Youmu : Region is invalid.");
                if (z == 0.0 && y == 0.0) throw new ArgumentOutOfRangeException("z/y", "Youmu : Both gradient is 0.");
                kz = z;
                ky = y;
                dThs = s;
                dThx = x;
                cdThs = s * y;
                cdThx = x * z;
            }
            internal Youmu(int D, int[] nl, int[] nn, int[] sl, int[] sn, double z, double y, double s, double x) : base(D, nl, nn, sl, sn)
            {
                if (s < 0.0 || x > 0.0) throw new ArgumentOutOfRangeException("s/x", "Youmu : Region is invalid.");
                if (z == 0.0 && y == 0.0) throw new ArgumentOutOfRangeException("z/y", "Youmu : Both gradient is 0.");
                kz = z;
                ky = y;
                dThs = s;
                dThx = x;
                cdThs = s * y;
                cdThx = x * z;
            }
            internal override void TGs(double[] d)
            {
                if (d.Length != Dtn) throw new ArgumentOutOfRangeException("d", "Youmu : Wrong input length.");
                Val = d;
                for (int cnt = 0; cnt < Dtn; cnt++)
                {
                    if (Val[cnt] >= dThs) CFn[cnt] = cdThs;
                    else if (Val[cnt] <= dThx) CFn[cnt] = cdThx;
                    else CFn[cnt] = KSC(Val[cnt], kz, ky);
                }
            }
            internal override void TGs(double d, int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Youmu : Invalid index.");
                Val[i] = d;
                if (Val[i] >= dThs) CFn[i] = cdThs;
                else if (Val[i] <= dThx) CFn[i] = cdThx;
                else CFn[i] = KSC(Val[i], kz, ky);
            }
            static internal double KSC(double d, double z, double y)
            {
                if (d >= 0.0) return d * y;
                else return d * z;
            }
            internal override double KSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Youmu : Invalid index.");
                //if (Val[i] > dThs) return dThs;
                //if (Val[i] < dThx) return dThx;
                if (hbb[i] == 0.0) return Val[i];
                else if (hbb[i] < 0.0)
                {
                    if (kz == 0.0 && ky > 0.0)
                    {
                        if (Val[i] >= dThs) return Val[i];
                        double dtemp = -ky * hbb[i];
                        if (Val[i] < 0.0)
                        {
                            if (Val[i] >= -1.0)
                            {
                                if (dtemp > 0.104) dtemp = 0.104;
                                dtemp += Val[i];
                                if (dtemp > 0.0) dtemp = 0.0;
                            }
                            else if (Val[i] >= -10.0)
                            {
                                if (dtemp > 1.7) dtemp = 1.7;
                                dtemp += Val[i];
                                if (dtemp > -1.0) dtemp = -1.0;
                            }
                            else if (Val[i] >= -100.0)
                            {
                                if (dtemp > 19.0) dtemp = 19.0;
                                dtemp += Val[i];
                                if (dtemp > -10.0) dtemp = -10.0;
                            }
                            else
                            {
                                if (dtemp > 343.4) dtemp = 343.4;
                                dtemp += Val[i];
                                if (dtemp > -100.0) dtemp = -100.0;
                            }
                        }
                        else
                        {
                            dtemp *= 0.3434;
                            if (Val[i] < 1.0)
                            {
                                if (dtemp > 0.03434) dtemp = 0.03434;
                                dtemp += Val[i];
                                if (dtemp > 1.0) dtemp = 1.0;
                            }
                            else if (Val[i] < 10.0)
                            {
                                if (dtemp > 0.19) dtemp = 0.19;
                                dtemp += Val[i];
                                if (dtemp > 10.0) dtemp = 10.0;
                            }
                            else if (Val[i] < 100.0)
                            {
                                if (dtemp > 1.7) dtemp = 1.7;
                                dtemp += Val[i];
                                if (dtemp > 100.0) dtemp = 100.0;
                            }
                            else if (Val[i] < 1000.0)
                            {
                                if (dtemp > 10.4) dtemp = 10.4;
                                dtemp += Val[i];
                                if (dtemp > 1000.0) dtemp = 1000.0;
                            }
                            else
                            {
                                dtemp = (dtemp <= 34.34) ? (dtemp + Val[i]) : (Val[i] + 34.34);
                            }
                        }
                        return (dtemp >= dThs) ? dThs : dtemp;
                    }
                    else if (kz == 0.0 && ky < 0.0)
                    {
                        if (Val[i] <= 0.0) return Val[i];
                        else
                        {
                            double dtemp = -ky * hbb[i] * 0.34;
                            if (Val[i] <= 10.0)
                            {
                                dtemp += Val[i];
                                return (dtemp <= 0.0) ? 0.0 : dtemp;
                            }
                            else if (Val[i] <= 100.0) dtemp = (dtemp <= -10.0) ? (Val[i] - 10.0) : (dtemp + Val[i]);
                            else if (Val[i] <= 1000.0) dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            else
                            {
                                dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            }
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                    }
                    else if (kz < 0.0 && ky == 0.0)
                    {
                        if (Val[i] <= dThx) return Val[i];
                        double dtemp = -kz * hbb[i] * 0.34;
                        if (Val[i] > -10.0)
                        {
                            dtemp += Val[i];
                            if (dtemp <= dThx) return dThx;
                            return (dtemp >= -10.0) ? -10.0 : dtemp;
                        }
                        else if (Val[i] >= -100.0) dtemp = (dtemp <= -10.0) ? (Val[i] - 10.0) : (dtemp + Val[i]);
                        else if (Val[i] >= -1000.0) dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                        else
                        {
                            dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                        }
                        return (dtemp <= dThx) ? dThx : dtemp;
                    }
                    else if (kz > 0.0 && ky == 0.0)
                    {
                        if (Val[i] >= 0.0) return Val[i];
                        else
                        {
                            double dtemp = -kz * hbb[i] * 0.34;
                            if (Val[i] >= -10.0)
                            {
                                dtemp += Val[i];
                                return (dtemp >= 0.0) ? 0.0 : dtemp;
                            }
                            else if (Val[i] >= -100.0) dtemp = (dtemp >= 10.0) ? (Val[i] + 10.0) : (dtemp + Val[i]);
                            else if (Val[i] >= -1000.0) dtemp = (dtemp >= 100.0) ? (Val[i] + 100.0) : (dtemp + Val[i]);
                            else
                            {
                                dtemp = (dtemp >= 100.0) ? (Val[i] + 100.0) : (dtemp + Val[i]);
                            }
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                    }
                    else if (kz > 0.0 && ky > 0.0)
                    {
                        if (Val[i] >= 0.0)
                        {
                            if (Val[i] >= dThs) return Val[i];
                            double dtemp = -ky * hbb[i] * 0.34;
                            if (Val[i] < 10.0)
                            {
                                dtemp += Val[i];
                                if (dtemp >= dThs) return dThs;
                                return (dtemp <= 10.0) ? dtemp : 10.0;
                            }
                            else if (Val[i] <= 100.0) dtemp = (dtemp <= 10.0) ? (dtemp + Val[i]) : (Val[i] + 10.0);
                            else if (Val[i] <= 1000.0) dtemp = (dtemp <= 100.0) ? (dtemp + Val[i]) : (Val[i] + 100.0);
                            else
                            {
                                dtemp = (dtemp <= 100.0) ? (dtemp + Val[i]) : (Val[i] + 100.0);
                            }
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                        else
                        {
                            double dtemp = -kz * hbb[i] * 0.34;
                            if (Val[i] >= -10.0)
                            {
                                dtemp += Val[i];
                                return (dtemp >= 10.0) ? 10.0 : dtemp;
                            }
                            else if (Val[i] >= -100.0) dtemp = (dtemp >= 10.0) ? (Val[i] + 10.0) : (dtemp + Val[i]);
                            else if (Val[i] >= -1000.0) dtemp = (dtemp >= 100.0) ? (Val[i] + 100.0) : (dtemp + Val[i]);
                            else
                            {
                                dtemp = (dtemp >= 100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            }
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                    }
                    else if (kz < 0.0 && ky < 0.0)
                    {
                        if (Val[i] <= 0.0)
                        {
                            if (Val[i] <= dThx) return Val[i];
                            double dtemp = -kz * hbb[i] * 0.34;
                            if (Val[i] > -10.0)
                            {
                                dtemp += Val[i];
                                if (dtemp <= dThx) return dThx;
                                return (dtemp >= -10.0) ? -10.0 : dtemp;
                            }
                            else if (Val[i] >= -100.0) dtemp = (dtemp <= -10.0) ? (Val[i] - 10.0) : (dtemp + Val[i]);
                            else if (Val[i] >= -1000.0) dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            else
                            {
                                dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            }
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                        else
                        {
                            double dtemp = -ky * hbb[i] * 0.34;
                            if (Val[i] <= 10.0)
                            {
                                dtemp += Val[i];
                                return (dtemp <= -10.0) ? -10.0 : dtemp;
                            }
                            else if (Val[i] <= 100.0) dtemp = (dtemp <= -10.0) ? (Val[i] - 10.0) : (dtemp + Val[i]);
                            else if (Val[i] <= 1000.0) dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            else
                            {
                                dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            }
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                    }
                    else if (kz < 0.0 && ky > 0.0)
                    {
                        if (Val[i] < 0.0)
                        {
                            if (Val[i] <= dThx) return Val[i];
                            double dtemp = -kz * hbb[i] * 0.34;
                            if (Val[i] > -10.0)
                            {
                                dtemp += Val[i];
                                if (dtemp <= dThx) return dThx;
                                return (dtemp >= -10.0) ? -10.0 : dtemp;
                            }
                            else if (Val[i] >= -100.0) dtemp = (dtemp <= -10.0) ? (Val[i] - 10.0) : (dtemp + Val[i]);
                            else if (Val[i] >= -1000.0) dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            else
                            {
                                dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            }
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                        else
                        {
                            if (Val[i] >= dThs) return Val[i];
                            double dtemp = -ky * hbb[i] * 0.34;
                            if (Val[i] < 10.0)
                            {
                                dtemp += Val[i];
                                if (dtemp >= dThs) return dThs;
                                return (dtemp <= 10.0) ? dtemp : 10.0;
                            }
                            else if (Val[i] <= 100.0) dtemp = (dtemp <= 10.0) ? (dtemp + Val[i]) : (Val[i] + 10.0);
                            else if (Val[i] <= 1000.0) dtemp = (dtemp <= 100.0) ? (dtemp + Val[i]) : (Val[i] + 100.0);
                            else
                            {
                                dtemp = (dtemp <= 100.0) ? (dtemp + Val[i]) : (Val[i] + 100.0);
                            }
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                    }
                    else if (kz > 0.0 && ky < 0.0)
                    {
                        if (Val[i] < 0.0)
                        {
                            double dtemp = -kz * hbb[i] * 0.34;
                            if (Val[i] >= -10.0)
                            {
                                dtemp += Val[i];
                                return (dtemp >= 0.0) ? 0.0 : dtemp;
                            }
                            else if (Val[i] >= -100.0) dtemp = (dtemp >= 10.0) ? (Val[i] + 10.0) : (dtemp + Val[i]);
                            else if (Val[i] >= -1000.0) dtemp = (dtemp >= 100.0) ? (Val[i] + 100.0) : (dtemp + Val[i]);
                            else
                            {
                                dtemp = (dtemp >= 100.0) ? (Val[i] + 100.0) : (dtemp + Val[i]);
                            }
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                        else
                        {
                            double dtemp = -ky * hbb[i] * 0.34;
                            if (Val[i] <= 10.0)
                            {
                                dtemp += Val[i];
                                return (dtemp <= 0.0) ? 0.0 : dtemp;
                            }
                            else if (Val[i] <= 100.0) dtemp = (dtemp <= -10.0) ? (Val[i] - 10.0) : (dtemp + Val[i]);
                            else if (Val[i] <= 1000.0) dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            else
                            {
                                dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            }
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                    }
                    else throw new ArgumentOutOfRangeException("kz/ky", "Youmu : Unexpected error.");
                }
                else
                {
                    if (kz == 0.0 && ky > 0.0)
                    {
                        if (Val[i] <= 0.0) return Val[i];
                        else
                        {
                            double dtemp = -ky * hbb[i];
                            if (Val[i] <= 1.0)
                            {
                                if (dtemp < -0.104) dtemp = -0.104;
                                dtemp += Val[i];
                                if (dtemp < 0.0) dtemp = 0.0;
                            }
                            else if (Val[i] <= 10.0)
                            {
                                if (dtemp < -1.7) dtemp = -1.7;
                                dtemp += Val[i];
                                if (dtemp < 1.0) dtemp = 1.0;
                            }
                            else if (Val[i] <= 100.0)
                            {
                                if (dtemp < -19.0) dtemp = -19.0;
                                dtemp += Val[i];
                                if (dtemp < 10.0) dtemp = 10.0;
                            }
                            else
                            {
                                if (dtemp < -343.4) dtemp = -343.4;
                                dtemp += Val[i];
                                if (dtemp < 100.0) dtemp = 100.0;
                            }
                            if (dtemp >= dThs) return dThs;
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                    }
                    else if (kz == 0.0 && ky < 0.0)
                    {
                        if (Val[i] >= dThs) return Val[i];
                        double dtemp = -ky * hbb[i] * 0.34;
                        if (Val[i] < 10.0)
                        {
                            dtemp += Val[i];
                            if (dtemp >= dThs) return dThs;
                            return (dtemp <= 10.0) ? dtemp : 10.0;
                        }
                        else if (Val[i] <= 100.0) dtemp = (dtemp <= 10.0) ? (dtemp + Val[i]) : (Val[i] + 10.0);
                        else if (Val[i] <= 1000.0) dtemp = (dtemp <= 100.0) ? (dtemp + Val[i]) : (Val[i] + 100.0);
                        else
                        {
                            dtemp = (dtemp <= 100.0) ? (dtemp + Val[i]) : (Val[i] + 100.0);
                        }
                        return (dtemp >= dThs) ? dThs : dtemp;
                    }
                    else if (kz < 0.0 && ky == 0.0)
                    {
                        if (Val[i] >= 0.0) return Val[i];
                        else
                        {
                            double dtemp = -kz * hbb[i] * 0.34;
                            if (Val[i] >= -10.0)
                            {
                                dtemp += Val[i];
                                return (dtemp >= 0.0) ? 0.0 : dtemp;
                            }
                            else if (Val[i] >= -100.0) dtemp = (dtemp >= 10.0) ? (Val[i] + 10.0) : (dtemp + Val[i]);
                            else if (Val[i] >= -1000.0) dtemp = (dtemp >= 100.0) ? (Val[i] + 100.0) : (dtemp + Val[i]);
                            else
                            {
                                dtemp = (dtemp >= 100.0) ? (Val[i] + 100.0) : (dtemp + Val[i]);
                            }
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                    }
                    else if (kz > 0.0 && ky == 0.0)
                    {
                        if (Val[i] <= dThx) return Val[i];
                        double dtemp = -kz * hbb[i] * 0.34;
                        if (Val[i] > -10.0)
                        {
                            dtemp += Val[i];
                            if (dtemp <= dThx) return dThx;
                            return (dtemp >= -10.0) ? -10.0 : dtemp;
                        }
                        else if (Val[i] >= -100.0) dtemp = (dtemp <= -10.0) ? (Val[i] - 10.0) : (dtemp + Val[i]);
                        else if (Val[i] >= -1000.0) dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                        else
                        {
                            dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                        }
                        return (dtemp <= dThx) ? dThx : dtemp;
                    }
                    else if (kz > 0.0 && ky > 0.0)
                    {
                        if (Val[i] <= 0.0)
                        {
                            if (Val[i] <= dThx) return Val[i];
                            double dtemp = -kz * hbb[i] * 0.34;
                            if (Val[i] > -10.0)
                            {
                                dtemp += Val[i];
                                if (dtemp <= dThx) return dThx;
                                return (dtemp >= -10.0) ? -10.0 : dtemp;
                            }
                            else if (Val[i] >= -100.0) dtemp = (dtemp <= -10.0) ? (Val[i] - 10.0) : (dtemp + Val[i]);
                            else if (Val[i] >= -1000.0) dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            else
                            {
                                dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            }
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                        else
                        {
                            double dtemp = -ky * hbb[i] * 0.34;
                            if (Val[i] <= 10.0)
                            {
                                dtemp += Val[i];
                                return (dtemp <= -10.0) ? -10.0 : dtemp;
                            }
                            else if (Val[i] <= 100.0) dtemp = (dtemp <= -10.0) ? (Val[i] - 10.0) : (dtemp + Val[i]);
                            else if (Val[i] <= 1000.0) dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            else
                            {
                                dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            }
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                    }
                    else if (kz < 0.0 && ky < 0.0)
                    {
                        if (Val[i] >= 0.0)
                        {
                            if (Val[i] >= dThs) return Val[i];
                            double dtemp = -ky * hbb[i] * 0.34;
                            if (Val[i] < 10.0)
                            {
                                dtemp += Val[i];
                                if (dtemp >= dThs) return dThs;
                                return (dtemp <= 10.0) ? dtemp : 10.0;
                            }
                            else if (Val[i] <= 100.0) dtemp = (dtemp <= 10.0) ? (dtemp + Val[i]) : (Val[i] + 10.0);
                            else if (Val[i] <= 1000.0) dtemp = (dtemp <= 100.0) ? (dtemp + Val[i]) : (Val[i] + 100.0);
                            else
                            {
                                dtemp = (dtemp <= 100.0) ? (dtemp + Val[i]) : (Val[i] + 100.0);
                            }
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                        else
                        {
                            double dtemp = -kz * hbb[i] * 0.34;
                            if (Val[i] >= -10.0)
                            {
                                dtemp += Val[i];
                                return (dtemp >= 10.0) ? 10.0 : dtemp;
                            }
                            else if (Val[i] >= -100.0) dtemp = (dtemp >= 10.0) ? (Val[i] + 10.0) : (dtemp + Val[i]);
                            else if (Val[i] >= -1000.0) dtemp = (dtemp >= 100.0) ? (Val[i] + 100.0) : (dtemp + Val[i]);
                            else
                            {
                                dtemp = (dtemp >= 100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            }
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                    }
                    else if (kz < 0.0 && ky > 0.0)
                    {
                        if (Val[i] < 0.0)
                        {
                            double dtemp = -kz * hbb[i] * 0.34;
                            if (Val[i] >= -10.0)
                            {
                                dtemp += Val[i];
                                return (dtemp >= 0.0) ? 0.0 : dtemp;
                            }
                            else if (Val[i] >= -100.0) dtemp = (dtemp >= 10.0) ? (Val[i] + 10.0) : (dtemp + Val[i]);
                            else if (Val[i] >= -1000.0) dtemp = (dtemp >= 100.0) ? (Val[i] + 100.0) : (dtemp + Val[i]);
                            else
                            {
                                dtemp = (dtemp >= 100.0) ? (Val[i] + 100.0) : (dtemp + Val[i]);
                            }
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                        else
                        {
                            double dtemp = -ky * hbb[i] * 0.34;
                            if (Val[i] <= 10.0)
                            {
                                dtemp += Val[i];
                                return (dtemp <= 0.0) ? 0.0 : dtemp;
                            }
                            else if (Val[i] <= 100.0) dtemp = (dtemp <= -10.0) ? (Val[i] - 10.0) : (dtemp + Val[i]);
                            else if (Val[i] <= 1000.0) dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            else
                            {
                                dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            }
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                    }
                    else if (kz > 0.0 && ky < 0.0)
                    {
                        if (Val[i] < 0.0)
                        {
                            if (Val[i] <= dThx) return Val[i];
                            double dtemp = -kz * hbb[i] * 0.34;
                            if (Val[i] > -10.0)
                            {
                                dtemp += Val[i];
                                if (dtemp <= dThx) return dThx;
                                return (dtemp >= -10.0) ? -10.0 : dtemp;
                            }
                            else if (Val[i] >= -100.0) dtemp = (dtemp <= -10.0) ? (Val[i] - 10.0) : (dtemp + Val[i]);
                            else if (Val[i] >= -1000.0) dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            else
                            {
                                dtemp = (dtemp <= -100.0) ? (Val[i] - 100.0) : (dtemp + Val[i]);
                            }
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                        else
                        {
                            if (Val[i] >= dThs) return Val[i];
                            double dtemp = -ky * hbb[i] * 0.34;
                            if (Val[i] < 10.0)
                            {
                                dtemp += Val[i];
                                if (dtemp >= dThs) return dThs;
                                return (dtemp <= 10.0) ? dtemp : 10.0;
                            }
                            else if (Val[i] <= 100.0) dtemp = (dtemp <= 10.0) ? (dtemp + Val[i]) : (Val[i] + 10.0);
                            else if (Val[i] <= 1000.0) dtemp = (dtemp <= 100.0) ? (dtemp + Val[i]) : (Val[i] + 100.0);
                            else
                            {
                                dtemp = (dtemp <= 100.0) ? (dtemp + Val[i]) : (Val[i] + 100.0);
                            }
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                    }
                    else throw new ArgumentOutOfRangeException("kz/ky", "Youmu : Unexpected error.");
                }
            }
            static internal double DSC(double d, double z, double y)
            {
                if (d >= 0.0) return y;
                else return z;
            }
            internal override double DSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Youmu : Invalid index.");
                if (hbb[i] == 0.0) return 0.0;
                bool Po = (hbb[i] < 0.0) ? true : false;
                if (Val[i] < 0.0 && kz == 0.0)
                {
                    if (Po && ky > 0.0) return ky;
                    else if (!Po && ky < 0) return ky;
                }
                else if (Val[i] >= 0.0 && ky == 0.0)
                {
                    if (Po && kz < 0.0) return kz;
                    else if (!Po && ky > 0.0) return kz;
                }
                if (Val[i] > dThs)
                {
                    if (Po)
                    {
                        if (cdThs < cdThx && ky >= 0.0) return kz;
                        return 0.0;
                    }
                    else
                    {
                        if (cdThs > cdThx && ky <= 0.0) return kz;
                        return 0.0;
                    }
                }
                else if (Val[i] < dThx)
                {
                    if (Po)
                    {
                        if (cdThx < cdThs && kz <= 0.0) return ky;
                        return 0.0;
                    }
                    else
                    {
                        if (cdThx > cdThs && kz >= 0.0) return ky;
                        return 0.0;
                    }
                }
                return DSC(Val[i], kz, ky);
            }
            static internal double DS2C()
            {
                return 0.0;
            }
            internal override void TGsY(double d, int i)
            {
                if (i >= DtnY || i < 0) throw new ArgumentOutOfRangeException("i", "Youmu : Invalid index.");
                ValY[i] = d;
                if (ValY[i] >= dThs) CFnY[i] = cdThs;
                else if (ValY[i] <= dThx) CFnY[i] = cdThx;
                else CFnY[i] = KSC(ValY[i], kz, ky);
            }
            internal override bool RepC(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool DGK(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool KS2(string msg)
            {
                throw new NotImplementedException();
            }
        }
        internal sealed class Yuyuko : HakuGyokuRou//恒等関数、y=x
        {
            static private readonly string Mj = "西行寺";
            static private readonly string Nm = "幽々子";
            static private readonly string Mje = "Saigyouji";
            static private readonly string Nme = "Yuyuko";
            static internal readonly int THFI = 9;
            static private double dThs = 1000.0;//定義上限
            static private double dThx = -1000.0;//定義下限
            internal override sealed string NM
            {
                get
                {
                    return Nm;
                }
            }
            internal double dTs
            {
                get
                {
                    return dThs;
                }
                set
                {
                    if (value < 0.0) throw new ArgumentOutOfRangeException("dTs", "Yuyuko : Domain region is invalid.");
                    double dstemp = dThs;
                    dThs = value;
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] > dstemp && Val[cnt] < dThs) CFn[cnt] = Val[cnt];
                        else if (Val[cnt] > value) CFn[cnt] = dThs;
                    }
                }
            }
            internal double dTx
            {
                get
                {
                    return dThx;
                }
                set
                {
                    if (value > 0.0) throw new ArgumentOutOfRangeException("dTx", "Yuyuko : Domain region is invalid.");
                    double dstemp = dThx;
                    dThx = value;
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] < dstemp && Val[cnt] > dThx) CFn[cnt] = Val[cnt];
                        else if (Val[cnt] < value) CFn[cnt] = dThx;
                    }
                }
            }
            internal Yuyuko(int D) : base(D) { }
            internal Yuyuko(int D, int[] nsl, int[] nsn, bool n) : base(D, nsl, nsn, n) { }
            internal Yuyuko(int D, int[] nl, int[] nn, int[] sl, int[] sn) : base(D, nl, nn, sl, sn) { }
            internal Yuyuko(int D, double s, double x) : base(D)
            {
                if (s < 0.0) throw new ArgumentOutOfRangeException("dTs", "Yuyuko : Domain region is invalid.");
                if (x > 0.0) throw new ArgumentOutOfRangeException("dTx", "Yuyuko : Domain region is invalid.");
                dThs = s;
                dThx = x;
            }
            internal Yuyuko(int D, int[] nsl, int[] nsn, bool n, double s, double x) : base(D, nsl, nsn, n)
            {
                if (s < 0.0) throw new ArgumentOutOfRangeException("dTs", "Yuyuko : Domain region is invalid.");
                if (x > 0.0) throw new ArgumentOutOfRangeException("dTx", "Yuyuko : Domain region is invalid.");
                dThs = s;
                dThx = x;
            }
            internal Yuyuko(int D, int[] nl, int[] nn, int[] sl, int[] sn, double s, double x) : base(D, nl, nn, sl, sn)
            {
                if (s < 0.0) throw new ArgumentOutOfRangeException("dTs", "Yuyuko : Domain region is invalid.");
                if (x > 0.0) throw new ArgumentOutOfRangeException("dTx", "Yuyuko : Domain region is invalid.");
                dThs = s;
                dThx = x;
            }
            internal override void TGs(double[] d)
            {
                if (d.Length != Dtn) throw new ArgumentOutOfRangeException("d", "Yuyuko : Wrong input length.");
                Val = d;
                for (int cnt = 0; cnt < Dtn; cnt++)
                {
                    if (Val[cnt] >= dThs) CFn[cnt] = dThs;
                    else if (Val[cnt] <= dThx) CFn[cnt] = dThx;
                    else CFn[cnt] = Val[cnt];
                }
            }
            internal override void TGs(double d, int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Yuyuko : Invalid index.");
                Val[i] = d;
                if (Val[i] >= dThs) CFn[i] = dThs;
                else if (Val[i] <= dThx) CFn[i] = dThx;
                else CFn[i] = d;
            }
            static internal double KSC(double d)
            {
                return d;
            }
            internal override double KSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Yuyuko : Invalid index.");
                //if (Val[i] > dThs) return dThs;
                //if (Val[i] < dThx) return dThx;
                if (hbb[i] == 0.0) return Val[i];
                double dtemp;
                if (hbb[i] < 0.0)
                {
                    if (Val[i] < 1000.0 && Val[i] >= -1000.0)
                    {
                        if (Val[i] < 1.0 && Val[i] >= -1.0)
                        {
                            if (Val[i] >= 0.0)
                            {
                                if (hbb[i] > -0.34)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp > 1.0) dtemp = 1.0;
                                }
                                else
                                {
                                    dtemp = Val[i] + 0.34;
                                    if (dtemp > 1.0) dtemp = 1.0;
                                }
                            }
                            else
                            {
                                if (hbb[i] > -0.495)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp > 0.0) dtemp = 0.0;
                                }
                                else
                                {
                                    dtemp = Val[i] + 0.495;
                                    if (dtemp > 0.0) dtemp = 0.0;
                                }
                            }
                        }
                        else if (Val[i] < 10.0 && Val[i] >= -10.0)
                        {
                            if (Val[i] >= 0.0)
                            {
                                if (hbb[i] > -1.04)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp > 10.0) dtemp = 10.0;
                                }
                                else
                                {
                                    dtemp = Val[i] + 1.04;
                                    if (dtemp > 10.0) dtemp = 10.0;
                                }
                            }
                            else
                            {
                                if (hbb[i] > -3.4)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp > -1.0) dtemp = -1.0;
                                }
                                else
                                {
                                    dtemp = Val[i] + 3.4;
                                    if (dtemp > -1.0) dtemp = -1.0;
                                }
                            }
                        }
                        else if (Val[i] < 100.0 && Val[i] >= -100.0)
                        {
                            if (Val[i] >= 0.0)
                            {
                                if (hbb[i] > -6.9)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp > 100.0) dtemp = 100.0;
                                }
                                else
                                {
                                    dtemp = Val[i] + 6.9;
                                    if (dtemp > 100.0) dtemp = 100.0;
                                }
                            }
                            else
                            {
                                if (hbb[i] > -15.0)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp > -10.0) dtemp = -10.0;
                                }
                                else
                                {
                                    dtemp = Val[i] + 15.0;
                                    if (dtemp > -10.0) dtemp = -10.0;
                                }
                            }
                        }
                        else
                        {
                            if (Val[i] >= 0.0)
                            {
                                if (hbb[i] > -17.0)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp > 1000.0) dtemp = 1000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] + 17.0;
                                    if (dtemp > 1000.0) dtemp = 1000.0;
                                }
                            }
                            else
                            {
                                if (hbb[i] > -49.5)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp > -100.0) dtemp = -100.0;
                                }
                                else
                                {
                                    dtemp = Val[i] + 49.5;
                                    if (dtemp > -100.0) dtemp = -100.0;
                                }
                            }
                        }
                    }
                    else if (Val[i] < 10000000.0 && Val[i] >= -10000000.0)
                    {
                        if (Val[i] < 10000.0 && Val[i] >= -10000.0)
                        {
                            if (Val[i] >= 0.0)
                            {
                                if (hbb[i] > -19.0)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp > 10000.0) dtemp = 10000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] + 19.0;
                                    if (dtemp > 10000.0) dtemp = 10000.0;
                                }
                            }
                            else
                            {
                                if (hbb[i] > -99.9)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp > -1000.0) dtemp = -1000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] + 99.9;
                                    if (dtemp > -1000.0) dtemp = -1000.0;
                                }
                            }
                        }
                        else if (Val[i] < 100000.0 && Val[i] >= -100000.0)
                        {
                            if (Val[i] >= 0.0)
                            {
                                if (hbb[i] > -34.34)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp > 100000.0) dtemp = 100000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] + 34.34;
                                    if (dtemp > 100000.0) dtemp = 100000.0;
                                }
                            }
                            else
                            {
                                if (hbb[i] > -343.4)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp > -10000.0) dtemp = -10000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] + 343.4;
                                    if (dtemp > -10000.0) dtemp = -10000.0;
                                }
                            }
                        }
                        else if (Val[i] < 1000000.0 && Val[i] >= -1000000.0)
                        {
                            if (Val[i] >= 0.0)
                            {
                                if (hbb[i] > -51.4)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp > 1000000.0) dtemp = 1000000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] + 51.4;
                                    if (dtemp > 1000000.0) dtemp = 1000000.0;
                                }
                            }
                            else
                            {
                                if (hbb[i] > -4950.0)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp > -100000.0) dtemp = -100000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] + 4950.0;
                                    if (dtemp > -100000.0) dtemp = -100000.0;
                                }
                            }
                        }
                        else
                        {
                            if (Val[i] >= 0.0)
                            {
                                if (hbb[i] > -170.0)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp > 10000000.0) dtemp = 10000000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] + 170.0;
                                    if (dtemp > 10000000.0) dtemp = 10000000.0;
                                }
                            }
                            else
                            {
                                if (hbb[i] > -51400.0)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp > -1000000.0) dtemp = -1000000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] + 51400.0;
                                    if (dtemp > -1000000.0) dtemp = -1000000.0;
                                }
                            }
                        }
                    }
                    else if (Val[i] < 100000000.0 && Val[i] >= -100000000.0)
                    {
                        if (Val[i] >= 0.0)
                        {
                            if (hbb[i] > -690.0)
                            {
                                dtemp = Val[i] - hbb[i];
                                if (dtemp > 100000000.0) dtemp = 100000000.0;
                            }
                            else
                            {
                                dtemp = Val[i] + 690.0;
                                if (dtemp > 100000000.0) dtemp = 100000000.0;
                            }
                        }
                        else
                        {
                            if (hbb[i] > -770000.0)
                            {
                                dtemp = Val[i] - hbb[i];
                                if (dtemp > -10000000.0) dtemp = -10000000.0;
                            }
                            else
                            {
                                dtemp = Val[i] + 770000.0;
                                if (dtemp > -10000000.0) dtemp = -10000000.0;
                            }
                        }
                    }
                    else
                    {
                        double dr = 1000000000.0;
                        double dt = 100000000.0;
                        while (true)
                        {
                            if (Val[i] < dr && Val[i] > -dr) dtemp = (hbb[i] > -dt) ? (Val[i] + dt) : (Val[i] - hbb[i]);
                            dr *= 10.0;
                            dt *= 10.0;
                            if (double.IsInfinity(dr) || double.IsNaN(dr)) throw new ArgumentOutOfRangeException("dr", "Yuyuko : Invalid number.");
                        }
                    }
                }
                else
                {
                    if (Val[i] <= 1000.0 && Val[i] > -1000.0)
                    {
                        if (Val[i] <= 1.0 && Val[i] > -1.0)
                        {
                            if (Val[i] <= 0.0)
                            {
                                if (hbb[i] < 0.34)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp < -1.0) dtemp = -1.0;
                                }
                                else
                                {
                                    dtemp = Val[i] - 0.34;
                                    if (dtemp < -1.0) dtemp = -1.0;
                                }
                            }
                            else
                            {
                                if (hbb[i] < 0.495)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp < 0.0) dtemp = 0.0;
                                }
                                else
                                {
                                    dtemp = Val[i] - 0.495;
                                    if (dtemp < 0.0) dtemp = 0.0;
                                }
                            }
                        }
                        else if (Val[i] <= 10.0 && Val[i] > -10.0)
                        {
                            if (Val[i] <= 0.0)
                            {
                                if (hbb[i] < 1.04)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp < -10.0) dtemp = -10.0;
                                }
                                else
                                {
                                    dtemp = Val[i] - 1.04;
                                    if (dtemp < -10.0) dtemp = -10.0;
                                }
                            }
                            else
                            {
                                if (hbb[i] < 3.4)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp < 1.0) dtemp = 1.0;
                                }
                                else
                                {
                                    dtemp = Val[i] - 3.4;
                                    if (dtemp < 1.0) dtemp = 1.0;
                                }
                            }
                        }
                        else if (Val[i] <= 100.0 && Val[i] > -100.0)
                        {
                            if (Val[i] <= 0.0)
                            {
                                if (hbb[i] < 6.9)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp < -100.0) dtemp = -100.0;
                                }
                                else
                                {
                                    dtemp = Val[i] - 6.9;
                                    if (dtemp < -100.0) dtemp = -100.0;
                                }
                            }
                            else
                            {
                                if (hbb[i] < 15.0)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp < 10.0) dtemp = 10.0;
                                }
                                else
                                {
                                    dtemp = Val[i] - 15.0;
                                    if (dtemp < 10.0) dtemp = 10.0;
                                }
                            }
                        }
                        else
                        {
                            if (Val[i] <= 0.0)
                            {
                                if (hbb[i] < 17.0)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp < -1000.0) dtemp = -1000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] - 17.0;
                                    if (dtemp < -1000.0) dtemp = -1000.0;
                                }
                            }
                            else
                            {
                                if (hbb[i] < 49.5)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp < 100.0) dtemp = 100.0;
                                }
                                else
                                {
                                    dtemp = Val[i] - 49.5;
                                    if (dtemp < 100.0) dtemp = 100.0;
                                }
                            }
                        }
                    }
                    else if (Val[i] <= 10000000.0 && Val[i] > -10000000.0)
                    {
                        if (Val[i] <= 10000.0 && Val[i] > -10000.0)
                        {
                            if (Val[i] <= 0.0)
                            {
                                if (hbb[i] < 19.0)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp < -10000.0) dtemp = -10000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] - 19.0;
                                    if (dtemp < -10000.0) dtemp = -10000.0;
                                }
                            }
                            else
                            {
                                if (hbb[i] < -99.9)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp < 1000.0) dtemp = 1000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] - 99.9;
                                    if (dtemp < 1000.0) dtemp = 1000.0;
                                }
                            }
                        }
                        else if (Val[i] <= 100000.0 && Val[i] > -100000.0)
                        {
                            if (Val[i] <= 0.0)
                            {
                                if (hbb[i] < 34.34)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp < -100000.0) dtemp = -100000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] - 34.34;
                                    if (dtemp < -100000.0) dtemp = -100000.0;
                                }
                            }
                            else
                            {
                                if (hbb[i] < 343.4)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp < 10000.0) dtemp = 10000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] - 343.4;
                                    if (dtemp < 10000.0) dtemp = 10000.0;
                                }
                            }
                        }
                        else if (Val[i] <= 1000000.0 && Val[i] > -1000000.0)
                        {
                            if (Val[i] <= 0.0)
                            {
                                if (hbb[i] < 51.4)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp < -1000000.0) dtemp = -1000000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] - 51.4;
                                    if (dtemp < -1000000.0) dtemp = -1000000.0;
                                }
                            }
                            else
                            {
                                if (hbb[i] < 4950.0)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp < 100000.0) dtemp = 100000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] - 4950.0;
                                    if (dtemp < 100000.0) dtemp = 100000.0;
                                }
                            }
                        }
                        else
                        {
                            if (Val[i] <= 0.0)
                            {
                                if (hbb[i] < 170.0)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp < -10000000.0) dtemp = -10000000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] - 170.0;
                                    if (dtemp < -10000000.0) dtemp = -10000000.0;
                                }
                            }
                            else
                            {
                                if (hbb[i] < 51400.0)
                                {
                                    dtemp = Val[i] - hbb[i];
                                    if (dtemp < 1000000.0) dtemp = 1000000.0;
                                }
                                else
                                {
                                    dtemp = Val[i] - 51400.0;
                                    if (dtemp < 1000000.0) dtemp = 1000000.0;
                                }
                            }
                        }
                    }
                    else if (Val[i] <= 100000000.0 && Val[i] > -100000000.0)
                    {
                        if (Val[i] <= 0.0)
                        {
                            if (hbb[i] < 690.0)
                            {
                                dtemp = Val[i] - hbb[i];
                                if (dtemp < -100000000.0) dtemp = -100000000.0;
                            }
                            else
                            {
                                dtemp = Val[i] - 690.0;
                                if (dtemp < -100000000.0) dtemp = -100000000.0;
                            }
                        }
                        else
                        {
                            if (hbb[i] < 770000.0)
                            {
                                dtemp = Val[i] - hbb[i];
                                if (dtemp < 10000000.0) dtemp = 10000000.0;
                            }
                            else
                            {
                                dtemp = Val[i] - 770000.0;
                                if (dtemp < 10000000.0) dtemp = 10000000.0;
                            }
                        }
                    }
                    else
                    {
                        double dr = 1000000000.0;
                        double dt = 100000000.0;
                        while (true)
                        {
                            if (Val[i] < dr && Val[i] > -dr) dtemp = (hbb[i] < dt) ? (Val[i] - dt) : (Val[i] - hbb[i]);
                            dr *= 10.0;
                            dt *= 10.0;
                            if (double.IsInfinity(dr) || double.IsNaN(dr)) throw new ArgumentOutOfRangeException("dr", "Yuyuko : Invalid number.");
                        }
                    }
                }
                if (dtemp >= dThs) return dThs;
                if (dtemp <= dThx) return dThx;
                return dtemp;
            }
            static internal double DSC()
            {
                return 1.0;
            }
            internal override double DSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Yuyuko : Invalid index.");
                return 1.0;
            }
            static internal double DS2C()
            {
                return 0.0;
            }
            internal override void TGsY(double d, int i)
            {
                if (i >= DtnY || i < 0) throw new ArgumentOutOfRangeException("i", "Yuyuko : Invalid index.");
                ValY[i] = d;
                if (ValY[i] >= dThs) CFnY[i] = dThs;
                else if (ValY[i] <= dThx) CFnY[i] = dThx;
                else CFnY[i] = d;
            }
            internal override bool RepC(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool DGK(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool KS2(string msg)
            {
                throw new NotImplementedException();
            }
        }
        internal sealed class Reisen : EiEnTei//二/四/六...次関数、y=x^2、y=x^4...
        {
            static private readonly string Mj = "イナバ";
            static private readonly string Mn = "優曇華院";
            static private readonly string Nm = "鈴仙";
            static private readonly string Mje = "Inaba";
            static private readonly string Mne = "Udongein";
            static private readonly string Nme = "Reisen";
            static internal readonly int THFI = 4;
            private double idx = 2.0;
            private double dThs = 30.0;//定義上限
            private double cdThs;//上限値
            private double DThs;//上限微分値
            private double dThx = -30.0;//定義下限
            private double cdThx;//下限値
            private double DThx;//下限微分値
            private double DTho;//原点微分値
            private double dTho = 0.001;//原点定義範囲
            internal override sealed string NM
            {
                get
                {
                    return Nm;
                }
            }
            internal double S
            {
                get
                {
                    return idx;
                }
                set
                {
                    if (value < 2.0 || value % 2.0 != 0.0) throw new ArgumentOutOfRangeException("S", "Reisen : Index must be positive even number.");
                    idx = value;
                    cdThs = KSC(dThs, idx);
                    DThs = DSC(dThs, idx);
                    cdThx = KSC(dThx, idx);
                    DThx = DSC(dThx, idx);
                    DTho = DSC(dTho, idx);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] > dThs) CFn[cnt] = cdThs;
                        else if (Val[cnt] < dThx) CFn[cnt] = cdThx;
                        else CFn[cnt] = KSC(Val[cnt], idx);
                    }
                }
            }
            internal double dTs
            {
                get
                {
                    return dThs;
                }
                set
                {
                    if (value < 0.0) throw new ArgumentOutOfRangeException("dTs", "Reisen : Domain region is invalid.");
                    double dstemp = dThs;
                    dThs = value;
                    cdThs = KSC(value, idx);
                    DThs = DSC(value, idx);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] > dstemp && Val[cnt] < dThs) CFn[cnt] = KSC(Val[cnt], idx);
                        else if (Val[cnt] > value) CFn[cnt] = cdThs;
                    }
                }
            }
            internal double dTx
            {
                get
                {
                    return dThx;
                }
                set
                {
                    if (value > 0.0) throw new ArgumentOutOfRangeException("dTx", "Reisen : Domain region is invalid.");
                    double dxtemp = dThx;
                    dThx = value;
                    cdThx = KSC(value, idx);
                    DThx = DSC(value, idx);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] < dxtemp && Val[cnt] > dThx) CFn[cnt] = KSC(Val[cnt], idx);
                        else if (Val[cnt] < value) CFn[cnt] = cdThx;
                    }

                }
            }
            internal Reisen(int D) : base(D)
            {
                DTho = DSC(dTho, idx);
                cdThs = KSC(dThs, idx);
                DThs = DSC(dThs, idx);
                cdThx = KSC(dThx, idx);
                DThx = DSC(dThx, idx);
            }
            internal Reisen(int D, int[] nsl, int[] nsn, bool n) : base(D, nsl, nsn, n)
            {
                DTho = DSC(dTho, idx);
                cdThs = KSC(dThs, idx);
                DThs = DSC(dThs, idx);
                cdThx = KSC(dThx, idx);
                DThx = DSC(dThx, idx);
            }
            internal Reisen(int D, int[] nl, int[] nn, int[] sl, int[] sn) : base(D, nl, nn, sl, sn)
            {
                DTho = DSC(dTho, idx);
                cdThs = KSC(dThs, idx);
                DThs = DSC(dThs, idx);
                cdThx = KSC(dThx, idx);
                DThx = DSC(dThx, idx);
            }
            internal Reisen(int D, double i) : base(D)
            {
                if (i < 2.0 || i % 2.0 != 0.0) throw new ArgumentOutOfRangeException("i", "Reisen : Index must be positive even number.");
                idx = i;
                DTho = DSC(dTho, idx);
                cdThs = KSC(dThs, idx);
                DThs = DSC(dThs, idx);
                cdThx = KSC(dThx, idx);
                DThx = DSC(dThx, idx);
            }
            internal Reisen(int D, int[] nsl, int[] nsn, bool n, double i) : base(D, nsl, nsn, n)
            {
                if (i < 2.0 || i % 2.0 != 0.0) throw new ArgumentOutOfRangeException("i", "Reisen : Index must be positive even number.");
                idx = i;
                DTho = DSC(dTho, idx);
                cdThs = KSC(dThs, idx);
                DThs = DSC(dThs, idx);
                cdThx = KSC(dThx, idx);
                DThx = DSC(dThx, idx);
            }
            internal Reisen(int D, int[] nl, int[] nn, int[] sl, int[] sn, double i) : base(D, nl, nn, sl, sn)
            {
                if (i < 2.0 || i % 2.0 != 0.0) throw new ArgumentOutOfRangeException("i", "Reisen : Index must be positive even number.");
                idx = i;
                DTho = DSC(dTho, idx);
                cdThs = KSC(dThs, idx);
                DThs = DSC(dThs, idx);
                cdThx = KSC(dThx, idx);
                DThx = DSC(dThx, idx);
            }
            internal override void TGs(double[] d)
            {
                if (d.Length != Dtn) throw new ArgumentOutOfRangeException("d", "Reisen : Wrong input length.");
                Val = d;
                for (int cnt = 0; cnt < Dtn; cnt++)
                {
                    if (Val[cnt] <= dTho && Val[cnt] >= -dTho) CFn[cnt] = 0.0;
                    else if (Val[cnt] >= dThs) CFn[cnt] = cdThs;
                    else if (Val[cnt] <= dThx) CFn[cnt] = cdThx;
                    else CFn[cnt] = KSC(Val[cnt], idx);
                }
            }
            internal override void TGs(double d, int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Reisen : Invalid index.");
                Val[i] = d;
                if (Val[i] <= dTho && Val[i] >= -dTho) CFn[i] = 0.0;
                else if (Val[i] >= dThs) CFn[i] = cdThs;
                else if (Val[i] <= dThx) CFn[i] = cdThx;
                else CFn[i] = KSC(Val[i], idx);
            }
            static internal double KSC(double d, double i)
            {
                if (i < 2.0 || i % 2.0 != 0.0) throw new ArgumentOutOfRangeException("i", "Reisen : Index must be positive even number.");
                if (i == 2.0) return d * d;
                else if (i == 4.0)
                {
                    double dres = d * d;
                    return dres * dres;
                }
                else if (i == 6.0)
                {
                    double dres = d * d;
                    return dres * dres * dres;
                }
                else if (i == 8.0)
                {
                    double dres = d * d;
                    dres *= dres;
                    return dres * dres;
                }
                else return Math.Pow(d, i);
            }
            internal override double KSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Reisen : Invalid index.");
                //if (Val[i] > dThs) return dThs;
                //if (Val[i] < dThx) return dThx;
                if (hbb[i] == 0.0) return Val[i];
                if (idx == 2.0)
                {
                    if (hbb[i] < 0.0)
                    {
                        if (Val[i] >= 0)
                        {
                            if (Val[i] > dThs) return Val[i];
                            double dtemp;
                            if (Val[i] <= dTho)
                            {
                                dtemp = -DTho * hbb[i];
                                dtemp = (dtemp >= 1.0) ? (Val[i] + 1.0) : (Val[i] + dtemp);
                            }
                            else if (Val[i] <= 3.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.01) : (Val[i] - 0.01 * hbb[i]);
                            else if (Val[i] <= 10.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.03) : (Val[i] - 0.03 * hbb[i]);
                            else if (Val[i] <= 30.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.1) : (Val[i] - 0.1 * hbb[i]);
                            else if (Val[i] <= 100.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.3) : (Val[i] - 0.3 * hbb[i]);
                            else if (Val[i] <= 300.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 1.0) : (Val[i] - hbb[i]);
                            else if (Val[i] <= 1000.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 3.0) : (Val[i] - 3.0 * hbb[i]);
                            else if (Val[i] <= 3000.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 10.0) : (Val[i] - 10.0 * hbb[i]);
                            else if (Val[i] <= 10000.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 30.0) : (Val[i] - 30.0 * hbb[i]);
                            else
                            {
                                double d3 = 30000.0;
                                double d1 = 10000.0;
                                while (true)
                                {
                                    if (Val[i] <= d3)
                                    {
                                        dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.0001 * d1) : (Val[i] - 0.0001 * d1 * hbb[i]);
                                        break;
                                    }
                                    d1 *= 10.0;
                                    if (double.IsInfinity(d1) || double.IsNaN(d1)) throw new ArgumentOutOfRangeException("d1", "Reisen : Invalid number.");
                                    if (Val[i] <= d1)
                                    {
                                        dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.0001 * d3) : (Val[i] - 0.0001 * d3 * hbb[i]);
                                        break;
                                    }
                                    d3 *= 10.0;
                                    if (double.IsInfinity(d3) || double.IsNaN(d3)) throw new ArgumentOutOfRangeException("d3", "Reisen : Invalid number.");
                                }
                            }
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                        else
                        {
                            if (Val[i] < dThx) return Val[i];
                            double dtemp;
                            if (Val[i] >= -dTho)
                            {
                                dtemp = DTho * hbb[i];
                                dtemp = (dtemp <= -1.0) ? (Val[i] - 1.0) : (Val[i] + dtemp);
                            }
                            else if (Val[i] >= -3.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] - 0.01) : (Val[i] + 0.01 * hbb[i]);
                            else if (Val[i] >= -10.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] - 0.03) : (Val[i] + 0.03 * hbb[i]);
                            else if (Val[i] >= -30.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] - 0.1) : (Val[i] + 0.1 * hbb[i]);
                            else if (Val[i] >= -100.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] - 0.3) : (Val[i] + 0.3 * hbb[i]);
                            else if (Val[i] >= -300.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] - 1.0) : (Val[i] + hbb[i]);
                            else if (Val[i] >= -1000.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] - 3.0) : (Val[i] + 3.0 * hbb[i]);
                            else if (Val[i] >= -3000.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] - 10.0) : (Val[i] + 10.0 * hbb[i]);
                            else if (Val[i] >= -10000.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] - 30.0) : (Val[i] + 30.0 * hbb[i]);
                            else
                            {
                                double d3 = -30000.0;
                                double d1 = -10000.0;
                                while (true)
                                {
                                    if (Val[i] >= d3)
                                    {
                                        dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.0001 * d1) : (Val[i] - 0.0001 * d1 * hbb[i]);
                                        break;
                                    }
                                    d1 *= 10.0;
                                    if (double.IsInfinity(d1) || double.IsNaN(d1)) throw new ArgumentOutOfRangeException("d1", "Reisen : Invalid number.");
                                    if (Val[i] >= d1)
                                    {
                                        dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.0001 * d3) : (Val[i] - 0.0001 * d3 * hbb[i]);
                                        break;
                                    }
                                    d3 *= 10.0;
                                    if (double.IsInfinity(d3) || double.IsNaN(d3)) throw new ArgumentOutOfRangeException("d3", "Reisen : Invalid number.");
                                }
                            }
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                    }
                    else
                    {
                        if (Val[i] >= 0)
                        {
                            if (Val[i] <= dTho) return 0.0;
                            double dtemp;
                            if (Val[i] <= 3.0)
                            {
                                dtemp = Val[i] - hbb[i];
                                if (dtemp >= dThs) return dThs;
                                return (dtemp <= 0.0) ? 0.0 : dtemp;
                            }
                            else if (Val[i] <= 10.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 0.3) : (Val[i] - 0.3 * hbb[i]);
                            else if (Val[i] <= 30.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 1.0) : (Val[i] - hbb[i]);
                            else if (Val[i] <= 100.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 3.0) : (Val[i] - 3.0 * hbb[i]);
                            else if (Val[i] <= 300.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 10.0) : (Val[i] - 10.0 * hbb[i]);
                            else if (Val[i] <= 1000.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 30.0) : (Val[i] - 30.0 * hbb[i]);
                            else if (Val[i] <= 3000.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 100.0) : (Val[i] - 100.0 * hbb[i]);
                            else if (Val[i] <= 10000.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 300.0) : (Val[i] - 300.0 * hbb[i]);
                            else
                            {
                                double d3 = 30000.0;
                                double d1 = 10000.0;
                                while (true)
                                {
                                    if (Val[i] <= d3)
                                    {
                                        dtemp = (hbb[i] >= 1.0) ? (Val[i] - d1) : (Val[i] - d1 * hbb[i]);
                                        break;
                                    }
                                    d1 *= 10.0;
                                    if (double.IsInfinity(d1) || double.IsNaN(d1)) throw new ArgumentOutOfRangeException("d1", "Reisen : Invalid number.");
                                    if (Val[i] <= d1)
                                    {
                                        dtemp = (hbb[i] >= 1.0) ? (Val[i] - d3) : (Val[i] - d3 * hbb[i]);
                                        break;
                                    }
                                    d3 *= 10.0;
                                    if (double.IsInfinity(d3) || double.IsNaN(d3)) throw new ArgumentOutOfRangeException("d3", "Reisen : Invalid number.");
                                }
                            }
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                        else
                        {
                            if (Val[i] >= -dTho) return 0.0;
                            double dtemp;
                            if (Val[i] >= -3.0)
                            {
                                dtemp = Val[i] + hbb[i];
                                if (dtemp <= dThx) return dThx;
                                return (dtemp >= 0.0) ? 0.0 : dtemp;
                            }
                            else if (Val[i] >= -10.0) return (hbb[i] >= 1.0) ? (Val[i] + 0.3) : (Val[i] - 0.3 * hbb[i]);
                            else if (Val[i] >= -30.0) return (hbb[i] >= 1.0) ? (Val[i] + 1.0) : (Val[i] - hbb[i]);
                            else if (Val[i] >= -100.0) return (hbb[i] >= 1.0) ? (Val[i] + 3.0) : (Val[i] - 3.0 * hbb[i]);
                            else if (Val[i] >= -300.0) return (hbb[i] >= 1.0) ? (Val[i] + 10.0) : (Val[i] - 10.0 * hbb[i]);
                            else if (Val[i] >= -1000.0) return (hbb[i] >= 1.0) ? (Val[i] + 30.0) : (Val[i] - 30.0 * hbb[i]);
                            else if (Val[i] >= -3000.0) return (hbb[i] >= 1.0) ? (Val[i] + 100.0) : (Val[i] - 100.0 * hbb[i]);
                            else if (Val[i] >= -10000.0) return (hbb[i] >= 1.0) ? (Val[i] + 300.0) : (Val[i] - 300.0 * hbb[i]);
                            else
                            {
                                double d3 = -30000.0;
                                double d1 = -10000.0;
                                while (true)
                                {
                                    if (Val[i] >= d3) return (hbb[i] >= 1.0) ? (Val[i] + d1) : (Val[i] + d1 * hbb[i]);
                                    d1 *= 10.0;
                                    if (double.IsInfinity(d1) || double.IsNaN(d1)) throw new ArgumentOutOfRangeException("d1", "Reisen : Invalid number.");
                                    if (Val[i] >= d1) return (hbb[i] >= 1.0) ? (Val[i] + d3) : (Val[i] + d3 * hbb[i]);
                                    d3 *= 10.0;
                                    if (double.IsInfinity(d3) || double.IsNaN(d3)) throw new ArgumentOutOfRangeException("d3", "Reisen : Invalid number.");
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException("Reisen : Method not implemented.");
                }
            }
            static internal double DSC(double d, double i)
            {
                if (i < 2.0 || i % 2.0 != 0.0) throw new ArgumentOutOfRangeException("i", "Reisen : Index must be positive even number.");
                if (i == 2.0) return 2.0 * d;
                else if (i == 4.0)
                {
                    return 4.0 * d * d * d;
                }
                else if (i == 6.0)
                {
                    double dres = d * d;
                    return 6.0 * dres * dres * d;
                }
                else if (i == 8.0)
                {
                    double dres = d * d;
                    return 8.0 * dres * dres * dres * d;
                }
                else return i * Math.Pow(d, i - 1.0);
            }
            internal override double DSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Reisen : Invalid index.");
                if (hbb[i] == 0.0) return 0.0;
                bool Po = (hbb[i] < 0.0) ? true : false;
                if (Val[i] <= dTho && Val[i] >= -dTho)
                {
                    if (Val[i] >= 0)
                    {
                        if (Po) return DTho;
                        else return 0.0;
                    }
                    else
                    {
                        if (Po) return -DTho;
                        else return 0.0;
                    }
                }
                else if (Val[i] > dThs)
                {
                    if (!Po) return DThs;
                    else if (0.925 * dThx > dThs) return DThx;
                    return 0.0;
                }
                else if (Val[i] < dThx)
                {
                    if (!Po) return DThx;
                    else if (0.925 * dThs > dThx) return DThs;
                    return 0.0;
                }
                else return DSC(Val[i], idx);
            }
            static internal double DS2C(double d, double i)
            {
                if (i < 2.0 || i % 2.0 != 0.0) throw new ArgumentOutOfRangeException("i", "Reisen : Index must be positive even number.");
                if (i == 2.0) return 2.0;
                else if (i == 4.0)
                {
                    return 12.0 * d * d;
                }
                else if (i == 6.0)
                {
                    double dres = d * d;
                    return 30.0 * dres * dres;
                }
                else if (i == 8.0)
                {
                    double dres = d * d;
                    return 56.0 * dres * dres * dres;
                }
                else return i * (i - 1.0) * Math.Pow(d, i - 2.0);
            }
            internal override void TGsY(double d, int i)
            {
                if (i >= DtnY || i < 0) throw new ArgumentOutOfRangeException("i", "Reisen : Invalid index.");
                ValY[i] = d;
                if (ValY[i] <= dTho && ValY[i] >= -dTho) CFnY[i] = 0.0;
                else if (ValY[i] >= dThs) CFnY[i] = cdThs;
                else if (ValY[i] <= dThx) CFnY[i] = cdThx;
                else CFnY[i] = KSC(ValY[i], idx);
            }
            internal override bool RepC(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool DGK(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool KS2(string msg)
            {
                throw new NotImplementedException();
            }
        }
        internal sealed class Tei : EiEnTei//三/五/七...次関数、y=x^3、y=x^5...
        {
            static private readonly string Mj = "因幡";
            static private readonly string Nm = "帝";
            static private readonly string Mje = "Inaba";
            static private readonly string Nme = "Tei";
            static internal readonly int THFI = 5;
            private double idx = 3.0;
            private double dThs = 10.0;//定義上限
            private double cdThs;//上限値
            private double DThs;//上限微分値
            private double dThx = -10.0;//定義下限
            private double cdThx;//下限値
            private double DThx;//下限微分値
            private double DTho;//原点微分値
            private double dTho = 0.01;//原点定義範囲
            internal override sealed string NM
            {
                get
                {
                    return Nm;
                }
            }
            internal double S
            {
                get
                {
                    return idx;
                }
                set
                {
                    if (value < 3.0 || (value - 1.0) % 2.0 != 0.0) throw new ArgumentOutOfRangeException("S", "Tei : Index must be positive odd number.");
                    idx = value;
                    cdThs = KSC(dThs, idx);
                    DThs = DSC(dThs, idx);
                    cdThx = KSC(dThx, idx);
                    DThx = DSC(dThx, idx);
                    DTho = DSC(dTho, idx);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] > dThs) CFn[cnt] = cdThs;
                        else if (Val[cnt] < dThx) CFn[cnt] = cdThx;
                        else CFn[cnt] = KSC(Val[cnt], idx);
                    }
                }
            }
            internal double dTs
            {
                get
                {
                    return dThs;
                }
                set
                {
                    if (value < 0.0) throw new ArgumentOutOfRangeException("dTs", "Tei : Domain region is invalid.");
                    double dstemp = dThs;
                    dThs = value;
                    cdThs = KSC(value, idx);
                    DThs = DSC(value, idx);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] > dstemp && Val[cnt] < dThs) CFn[cnt] = KSC(Val[cnt], idx);
                        else if (Val[cnt] > value) CFn[cnt] = cdThs;
                    }
                }
            }
            internal double dTx
            {
                get
                {
                    return dThx;
                }
                set
                {
                    if (value > 0.0) throw new ArgumentOutOfRangeException("dTx", "Tei : Domain region is invalid.");
                    double dxtemp = dThx;
                    dThx = value;
                    cdThx = KSC(value, idx);
                    DThx = DSC(value, idx);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] < dxtemp && Val[cnt] > dThx) CFn[cnt] = KSC(Val[cnt], idx);
                        else if (Val[cnt] < value) CFn[cnt] = cdThx;
                    }
                }
            }
            internal Tei(int D) : base(D)
            {
                DTho = DSC(dTho, idx);
                cdThs = KSC(dThs, idx);
                DThs = DSC(dThs, idx);
                cdThx = KSC(dThx, idx);
                DThx = DSC(dThx, idx);
            }
            internal Tei(int D, int[] nsl, int[] nsn, bool n) : base(D, nsl, nsn, n)
            {
                DTho = DSC(dTho, idx);
                cdThs = KSC(dThs, idx);
                DThs = DSC(dThs, idx);
                cdThx = KSC(dThx, idx);
                DThx = DSC(dThx, idx);
            }
            internal Tei(int D, int[] nl, int[] nn, int[] sl, int[] sn) : base(D, nl, nn, sl, sn)
            {
                DTho = DSC(dTho, idx);
                cdThs = KSC(dThs, idx);
                DThs = DSC(dThs, idx);
                cdThx = KSC(dThx, idx);
                DThx = DSC(dThx, idx);
            }
            internal Tei(int D, double i) : base(D)
            {
                if (i < 3.0 || (i - 1.0) % 2.0 != 0.0) throw new ArgumentOutOfRangeException("i", "Tei : Index must be positive odd number.");
                idx = i;
                DTho = DSC(dTho, idx);
                cdThs = KSC(dThs, idx);
                DThs = DSC(dThs, idx);
                cdThx = KSC(dThx, idx);
                DThx = DSC(dThx, idx);
            }
            internal Tei(int D, int[] nsl, int[] nsn, bool n, double i) : base(D, nsl, nsn, n)
            {
                if (i < 3.0 || (i - 1.0) % 2.0 != 0.0) throw new ArgumentOutOfRangeException("i", "Tei : Index must be positive odd number.");
                idx = i;
                DTho = DSC(dTho, idx);
                cdThs = KSC(dThs, idx);
                DThs = DSC(dThs, idx);
                cdThx = KSC(dThx, idx);
                DThx = DSC(dThx, idx);
            }
            internal Tei(int D, int[] nl, int[] nn, int[] sl, int[] sn, double i) : base(D, nl, nn, sl, sn)
            {
                if (i < 3.0 || (i - 1.0) % 2.0 != 0.0) throw new ArgumentOutOfRangeException("i", "Tei : Index must be positive odd number.");
                idx = i;
                DTho = DSC(dTho, idx);
                cdThs = KSC(dThs, idx);
                DThs = DSC(dThs, idx);
                cdThx = KSC(dThx, idx);
                DThx = DSC(dThx, idx);
            }
            internal override void TGs(double[] d)
            {
                if (d.Length != Dtn) throw new ArgumentOutOfRangeException("d", "Tei : Wrong input length.");
                Val = d;
                for (int cnt = 0; cnt < Dtn; cnt++)
                {
                    if (Val[cnt] <= dTho && Val[cnt] >= -dTho) CFn[cnt] = 0.0;
                    else if (Val[cnt] >= dThs) CFn[cnt] = cdThs;
                    else if (Val[cnt] <= dThx) CFn[cnt] = cdThx;
                    else CFn[cnt] = KSC(Val[cnt], idx);
                }
            }
            internal override void TGs(double d, int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Tei : Invalid index.");
                Val[i] = d;
                if (Val[i] <= dTho && Val[i] >= -dTho) CFn[i] = 0.0;
                else if (Val[i] >= dThs) CFn[i] = cdThs;
                else if (Val[i] <= dThx) CFn[i] = cdThx;
                else CFn[i] = KSC(Val[i], idx);
            }
            static internal double KSC(double d, double i)
            {
                if (i < 3.0 || (i - 1.0) % 2.0 != 0.0) throw new ArgumentOutOfRangeException("i", "Tei : Index must be positive odd number.");
                if (i == 3.0) return d * d * d;
                else if (i == 5.0)
                {
                    double dres = d * d;
                    return dres * dres * d;
                }
                else if (i == 7.0)
                {
                    double dres = d * d;
                    return dres * dres * dres * d;
                }
                else if (i == 9.0)
                {
                    double dres = d * d;
                    dres *= dres;
                    return dres * dres * d;
                }
                else return Math.Pow(d, i);
            }
            internal override double KSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Tei : Invalid index.");
                //if (Val[i] > dThs) return dThs;
                //if (Val[i] < dThx) return dThx;
                if (hbb[i] == 0.0) return Val[i];
                if (idx == 3.0)
                {
                    if (hbb[i] < 0.0)
                    {
                        if (Val[i] > dThs) return Val[i];
                        double dtemp;
                        if (Val[i] <= dTho && Val[i] >= -dTho)
                        {
                            dtemp = -DTho * hbb[i];
                            dtemp = (dtemp >= 0.2) ? (Val[i] + 0.2) : (Val[i] + dtemp);
                        }
                        else if (Val[i] <= 1.0 && Val[i] >= -1.0)
                        {
                            if (Val[i] < 0.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.2) : (Val[i] - 0.2 * hbb[i]);
                            else dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.02) : (Val[i] - 0.02 * hbb[i]);
                        }
                        else if (Val[i] <= 2.0 && Val[i] >= -2.0)
                        {
                            if (Val[i] < 0.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.5) : (Val[i] - 0.5 * hbb[i]);
                            else dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.05) : (Val[i] - 0.05 * hbb[i]);
                        }
                        else if (Val[i] <= 5.0 && Val[i] >= -5.0)
                        {
                            if (Val[i] < 0.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 1.0) : (Val[i] - hbb[i]);
                            else dtemp = (hbb[i] <= -0.1) ? (Val[i] + 0.1) : (Val[i] - hbb[i]);
                        }
                        else if (Val[i] <= 10.0 && Val[i] >= -10.0)
                        {
                            if (Val[i] < 0.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 2.0) : (Val[i] - 2.0 * hbb[i]);
                            else dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.2) : (Val[i] - 0.2 * hbb[i]);
                        }
                        else if (Val[i] <= 20.0 && Val[i] >= -20.0)
                        {
                            if (Val[i] < 0.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 5.0) : (Val[i] - 5.0 * hbb[i]);
                            else dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.5) : (Val[i] - 0.5 * hbb[i]);
                        }
                        else if (Val[i] <= 50.0 && Val[i] >= -50.0)
                        {
                            if (Val[i] < 0.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 10.0) : (Val[i] - 10.0 * hbb[i]);
                            else dtemp = (hbb[i] <= -1.0) ? (Val[i] + 1.0) : (Val[i] - 1.0 * hbb[i]);
                        }
                        else if (Val[i] <= 100.0 && Val[i] >= -100.0)
                        {
                            if (Val[i] < 0.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 20.0) : (Val[i] - 20.0 * hbb[i]);
                            else dtemp = (hbb[i] <= -1.0) ? (Val[i] + 2.0) : (Val[i] - 2.0 * hbb[i]);
                        }
                        else if (Val[i] <= 200.0 && Val[i] >= -200.0)
                        {
                            if (Val[i] < 0.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 50.0) : (Val[i] - 50.0 * hbb[i]);
                            else dtemp = (hbb[i] <= -1.0) ? (Val[i] + 5.0) : (Val[i] - 5.0 * hbb[i]);
                        }
                        else if (Val[i] <= 500.0 && Val[i] >= -500.0)
                        {
                            if (Val[i] < 0.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 100.0) : (Val[i] - 100.0 * hbb[i]);
                            else dtemp = (hbb[i] <= -1.0) ? (Val[i] + 10.0) : (Val[i] - 10.0 * hbb[i]);
                        }
                        else if (Val[i] <= 1000.0 && Val[i] >= -1000.0)
                        {
                            if (Val[i] < 0.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + 200.0) : (Val[i] - 200.0 * hbb[i]);
                            else dtemp = (hbb[i] <= -1.0) ? (Val[i] + 20.0) : (Val[i] - 20.0 * hbb[i]);
                        }
                        else
                        {
                            double d2 = 2000.0;
                            double d5 = 500.0;
                            double d1 = 1000.0;
                            while (true)
                            {
                                if (Val[i] <= d2 && Val[i] >= -d2)
                                {
                                    if (Val[i] < 0.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + d5) : (Val[i] - d5 * hbb[i]);
                                    else dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.1 * d5) : (Val[i] - 0.1 * d5 * hbb[i]);
                                    break;
                                }
                                d5 *= 10.0;
                                if (double.IsInfinity(d5) || double.IsNaN(d5)) throw new ArgumentOutOfRangeException("d5", "Tei : Invalid number.");
                                if (Val[i] <= d5 && Val[i] >= -d5)
                                {
                                    if (Val[i] < 0.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + d1) : (Val[i] - d1 * hbb[i]);
                                    else dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.1 * d1) : (Val[i] - 0.1 * d1 * hbb[i]);
                                    break;
                                }
                                d1 *= 10.0;
                                if (double.IsInfinity(d1) || double.IsNaN(d1)) throw new ArgumentOutOfRangeException("d1", "Tei : Invalid number.");
                                if (Val[i] <= d1 && Val[i] >= -d1)
                                {
                                    if (Val[i] < 0.0) dtemp = (hbb[i] <= -1.0) ? (Val[i] + d2) : (Val[i] - d2 * hbb[i]);
                                    else dtemp = (hbb[i] <= -1.0) ? (Val[i] + 0.1 * d2) : (Val[i] - 0.1 * d2 * hbb[i]);
                                    break;
                                }
                                d2 *= 10.0;
                                if (double.IsInfinity(d2) || double.IsNaN(d2)) throw new ArgumentOutOfRangeException("d2", "Tei : Invalid number.");
                            }
                        }
                        if (dtemp <= dThx) return dThx;
                        return (dtemp >= dThs) ? dThs : dtemp;

                    }
                    else
                    {
                        if (Val[i] < dThx) return Val[i];
                        double dtemp;
                        if (Val[i] <= dTho && Val[i] >= -dTho)
                        {
                            dtemp = -DTho * hbb[i];
                            dtemp = (dtemp <= -0.2) ? (Val[i] - 0.2) : (Val[i] + dtemp);
                        }
                        else if (Val[i] <= 1.0 && Val[i] >= -1.0)
                        {
                            if (Val[i] > 0.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 0.2) : (Val[i] - 0.2 * hbb[i]);
                            else dtemp = (hbb[i] >= 1.0) ? (Val[i] - 0.02) : (Val[i] - 0.02 * hbb[i]);
                        }
                        else if (Val[i] <= 2.0 && Val[i] >= -2.0)
                        {
                            if (Val[i] > 0.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 0.5) : (Val[i] - 0.5 * hbb[i]);
                            else dtemp = (hbb[i] >= 1.0) ? (Val[i] - 0.05) : (Val[i] - 0.05 * hbb[i]);
                        }
                        else if (Val[i] <= 5.0 && Val[i] >= -5.0)
                        {
                            if (Val[i] > 0.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 1.0) : (Val[i] - hbb[i]);
                            else dtemp = (hbb[i] >= 1.0) ? (Val[i] - 0.1) : (Val[i] - 0.1 * hbb[i]);
                        }
                        else if (Val[i] <= 10.0 && Val[i] >= -10.0)
                        {
                            if (Val[i] > 0.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 2.0) : (Val[i] - 2.0 * hbb[i]);
                            else dtemp = (hbb[i] >= 1.0) ? (Val[i] - 0.2) : (Val[i] - 0.2 * hbb[i]);
                        }
                        else if (Val[i] <= 20.0 && Val[i] >= -20.0)
                        {
                            if (Val[i] > 0.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 5.0) : (Val[i] - 5.0 * hbb[i]);
                            else dtemp = (hbb[i] >= 1.0) ? (Val[i] - 0.5) : (Val[i] - 0.5 * hbb[i]);
                        }
                        else if (Val[i] <= 50.0 && Val[i] >= -50.0)
                        {
                            if (Val[i] > 0.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 10.0) : (Val[i] - 10.0 * hbb[i]);
                            else dtemp = (hbb[i] >= 1.0) ? (Val[i] - 1.0) : (Val[i] - 1.0 * hbb[i]);
                        }
                        else if (Val[i] <= 100.0 && Val[i] >= -100.0)
                        {
                            if (Val[i] > 0.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 20.0) : (Val[i] - 20.0 * hbb[i]);
                            dtemp = (hbb[i] >= 1.0) ? (Val[i] - 2.0) : (Val[i] - 2.0 * hbb[i]);
                        }
                        else if (Val[i] <= 200.0 && Val[i] >= -200.0)
                        {
                            if (Val[i] > 0.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 50.0) : (Val[i] - 50.0 * hbb[i]);
                            dtemp = (hbb[i] >= 1.0) ? (Val[i] - 5.0) : (Val[i] - 5.0 * hbb[i]);
                        }
                        else if (Val[i] <= 500.0 && Val[i] >= -500.0)
                        {
                            if (Val[i] > 0.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 100.0) : (Val[i] - 100.0 * hbb[i]);
                            dtemp = (hbb[i] >= 1.0) ? (Val[i] - 10.0) : (Val[i] - 10.0 * hbb[i]);
                        }
                        else if (Val[i] <= 1000.0 && Val[i] >= -1000.0)
                        {
                            if (Val[i] > 0.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - 200.0) : (Val[i] - 200.0 * hbb[i]);
                            dtemp = (hbb[i] >= 1.0) ? (Val[i] - 20.0) : (Val[i] - 20.0 * hbb[i]);
                        }
                        else
                        {
                            double d2 = 2000.0;
                            double d5 = 500.0;
                            double d1 = 1000.0;
                            while (true)
                            {
                                if (Val[i] <= d2 && Val[i] >= -d2)
                                {
                                    if (Val[i] > 0.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - d5) : (Val[i] - d5 * hbb[i]);
                                    else dtemp = (hbb[i] >= 1.0) ? (Val[i] - 0.1 * d5) : (Val[i] - 0.1 * d5 * hbb[i]);
                                    break;
                                }
                                d5 *= 10.0;
                                if (double.IsInfinity(d5) || double.IsNaN(d5)) throw new ArgumentOutOfRangeException("d5", "Tei : Invalid number.");
                                if (Val[i] <= d5 && Val[i] >= -d5)
                                {
                                    if (Val[i] > 0.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - d1) : (Val[i] - d1 * hbb[i]);
                                    else dtemp = (hbb[i] >= 1.0) ? (Val[i] - 0.1 * d1) : (Val[i] - 0.1 * d1 * hbb[i]);
                                    break;
                                }
                                d1 *= 10.0;
                                if (double.IsInfinity(d1) || double.IsNaN(d1)) throw new ArgumentOutOfRangeException("d1", "Tei : Invalid number.");
                                if (Val[i] <= d1 && Val[i] >= -d1)
                                {
                                    if (Val[i] > 0.0) dtemp = (hbb[i] >= 1.0) ? (Val[i] - d2) : (Val[i] - d2 * hbb[i]);
                                    else dtemp = (hbb[i] >= 1.0) ? (Val[i] - 0.1 * d2) : (Val[i] - 0.1 * d2 * hbb[i]);
                                    break;
                                }
                                d2 *= 10.0;
                                if (double.IsInfinity(d2) || double.IsNaN(d2)) throw new ArgumentOutOfRangeException("d2", "Tei : Invalid number.");
                            }
                        }
                        if (dtemp >= dThs) return dThs;
                        return (dtemp <= dThx) ? dThx : dtemp;
                    }
                }
                else
                {
                    throw new NotImplementedException("Tei : Method not implemented.");
                }
            }
            static internal double DSC(double d, double i)
            {
                if (i < 3.0 || (i - 1.0) % 2.0 != 0.0) throw new ArgumentOutOfRangeException("i", "Tei : Index must be positive odd number.");
                if (i == 3.0) return 3.0 * d * d;
                else if (i == 5.0)
                {
                    double dres = d * d;
                    return 5.0 * dres * dres;
                }
                else if (i == 7.0)
                {
                    double dres = d * d;
                    return 7.0 * dres * dres * dres;
                }
                else if (i == 9.0)
                {
                    double dres = d * d;
                    dres *= dres;
                    return 9.0 * dres;
                }
                else return i * Math.Pow(d, i - 1.0);
            }
            internal override double DSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Tei : Invalid index.");
                if (hbb[i] == 0.0) return 0.0;
                bool Po = (hbb[i] < 0.0) ? true : false;
                if (Val[i] <= dTho && Val[i] >= -dTho)
                {
                    return DTho;
                }
                else if (Val[i] > dThs)
                {
                    if (!Po) return DThs;
                    return 0.0;
                }
                else if (Val[i] < dThx)
                {
                    if (Po) return DThx;
                    return 0.0;
                }
                else return DSC(Val[i], idx);
            }
            static internal double DS2C(double d, double i)
            {
                if (i < 3.0 || (i - 1.0) % 2.0 != 0.0) throw new ArgumentOutOfRangeException("i", "Tei : Index must be positive odd number.");
                if (i == 3.0) return 6.0 * d;
                else if (i == 5.0)
                {
                    return 20.0 * d * d * d;
                }
                else if (i == 7.0)
                {
                    double dres = d * d;
                    return 42.0 * dres * dres * d;
                }
                else if (i == 9.0)
                {
                    double dres = d * d;
                    return 72.0 * dres * dres * dres * d;
                }
                else return i * (i - 1.0) * Math.Pow(d, i - 2.0);
            }
            internal override void TGsY(double d, int i)
            {
                if (i >= DtnY || i < 0) throw new ArgumentOutOfRangeException("i", "Tei : Invalid index.");
                ValY[i] = d;
                if (ValY[i] <= dTho && ValY[i] >= -dTho) CFnY[i] = 0.0;
                else if (ValY[i] >= dThs) CFnY[i] = cdThs;
                else if (ValY[i] <= dThx) CFnY[i] = cdThx;
                else CFnY[i] = KSC(ValY[i], idx);
            }
            internal override bool RepC(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool DGK(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool KS2(string msg)
            {
                throw new NotImplementedException();
            }
        }
        internal sealed class Satori : ChiReiDen//正弦関数、y=sin ax
        {
            static private readonly string Mj = "古明地";
            static private readonly string Nm = "さとり";
            static private readonly string Mje = "Komeiji";
            static private readonly string Nme = "Satori";
            static internal readonly int THFI = 6;
            private double o = 1.0;//角周波数
            private double xDTh = 1E-5;//微分値下限
            private double oks = 4.0;//更新式用o
            private double dThs = 200.0 * Math.PI;//定義上限
            private double dThx = -200.0 * Math.PI;//定義下限
            internal override sealed string NM
            {
                get
                {
                    return Nm;
                }
            }
            internal double Jp
            {
                get
                {
                    return o;
                }
                set
                {
                    if (Math.Abs(value) < 1E-4 || Math.Abs(value) > 1E4) throw new ArgumentOutOfRangeException("Jp", "Satori : Invalid number.");
                    o = value;
                    xDTh = Math.Abs(xDTh * o);
                    oks = o * 4.0;
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        CFn[cnt] = KSC(Val[cnt], o);
                    }
                }
            }
            internal double xDT
            {
                get
                {
                    return xDTh;
                }
                set
                {
                    if (value <= 0 || value > Math.Abs(o)) throw new ArgumentOutOfRangeException("xDT", "Satori : Invalid threshold.");
                    xDTh = value;
                }
            }
            internal Satori(int D) : base(D) { }
            internal Satori(int D, int[] nsl, int[] nsn, bool n) : base(D, nsl, nsn, n) { }
            internal Satori(int D, int[] nl, int[] nn, int[] sl, int[] sn) : base(D, nl, nn, sl, sn) { }
            internal Satori(int D, double z) : base(D)
            {
                if (Math.Abs(z) < 1E-4 || Math.Abs(z) > 1E4) throw new ArgumentOutOfRangeException("Jp", "Satori : Invalid number.");
                o = z;
                xDTh = Math.Abs(xDTh * o);
                oks = o * 4.0;
                dThs = 20.0 * Math.PI / o;
                dThx = -dThs;
            }
            internal Satori(int D, int[] nsl, int[] nsn, bool n, double z) : base(D, nsl, nsn, n)
            {
                if (Math.Abs(z) < 1E-4 || Math.Abs(z) > 1E4) throw new ArgumentOutOfRangeException("Jp", "Satori : Invalid number.");
                o = z;
                xDTh = Math.Abs(xDTh * o);
                oks = o * 4.0;
                dThs = 20.0 * Math.PI / o;
                dThx = -dThs;
            }
            internal Satori(int D, int[] nl, int[] nn, int[] sl, int[] sn, double z) : base(D, nl, nn, sl, sn)
            {
                if (Math.Abs(z) < 1E-4 || Math.Abs(z) > 1E4) throw new ArgumentOutOfRangeException("Jp", "Satori : Invalid number.");
                o = z;
                xDTh = Math.Abs(xDTh * o);
                oks = o * 4.0;
                dThs = 20.0 * Math.PI / o;
                dThx = -dThs;
            }
            internal override void TGs(double[] d)
            {
                if (d.Length != Dtn) throw new ArgumentOutOfRangeException("d", "Satori : Wrong input length.");
                Val = d;
                for (int cnt = 0; cnt < Dtn; cnt++)
                {
                    CFn[cnt] = KSC(Val[cnt], o);
                }
            }
            internal override void TGs(double d, int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Satori : Invalid index.");
                Val[i] = d;
                CFn[i] = KSC(Val[i], o);
            }
            static internal double KSC(double d, double z)
            {
                if (Math.Abs(z) < 1E-4 || Math.Abs(z) > 1E4) throw new ArgumentOutOfRangeException("z", "Satori : Invalid number.");
                return Math.Sin(z * d);
            }
            internal override double KSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Satori : Invalid index.");
                if (hbb[i] == 0.0) return Val[i];
                double dr, db;
                db = -DSCn(i) * hbb[i] * 0.514;
                if (db == 0.0) return Val[i];
                if ((Val[i] > 0.0 && db > 0.0) || (Val[i] < 0.0 && db < 0.0))
                {
                    if (Val[i] > dThs) return dThs;
                    if (Val[i] < dThx) return dThx;
                    if (Math.Abs(Val[i]) < 10.0)
                    {
                        dr = Math.PI / 10.0 / oks;
                        if (Math.Abs(db) >= Math.Abs(dr)) return Val[i] + Math.CopySign(dr, db);
                        else if (Math.Abs(db) <= xDTh) return Val[i] + Math.CopySign(xDTh, db);
                        else return Val[i] + db;
                    }
                    else if (Math.Abs(Val[i]) > 100.0)
                    {
                        db *= 0.1;
                        dr = Math.PI / 100.0 / oks;
                        if (Math.Abs(db) >= Math.Abs(dr)) return Val[i] + Math.CopySign(dr, db);
                        else if (Math.Abs(db) <= xDTh) return Val[i] + Math.CopySign(xDTh, db);
                        else return Val[i] + db;
                    }
                    else if (Math.Abs(Val[i]) > 1000.0)
                    {
                        db *= 00.1;
                        dr = Math.PI / 1000.0 / oks;
                        if (Math.Abs(db) >= Math.Abs(dr)) return Val[i] + Math.CopySign(dr, db);
                        else if (Math.Abs(db) <= xDTh) return Val[i] + Math.CopySign(xDTh, db);
                        else return Val[i] + db;
                    }
                    else
                    {
                        db *= 000.1;
                        dr = Math.PI / 10000.0 / oks;
                        if (Math.Abs(db) >= Math.Abs(dr)) return Val[i] + Math.CopySign(dr, db);
                        else if (Math.Abs(db) <= xDTh) return Val[i] + Math.CopySign(xDTh, db);
                        else return Val[i] + db;
                    }
                }
                else
                {
                    if (Val[i] > dThs) return dThs;
                    if (Val[i] < dThx) return dThx;
                    if (Math.Abs(Val[i]) < 10.0)
                    {
                        dr = Math.PI / oks;
                        if (Math.Abs(db) >= Math.Abs(dr)) return Val[i] + Math.CopySign(dr, db);
                        else if (Math.Abs(db) <= xDTh) return Val[i] + Math.CopySign(xDTh, db);
                        else return Val[i] + db;
                    }
                    else if (Math.Abs(Val[i]) < 100.0)
                    {
                        db *= 2.0;
                        dr = 2.0 * Math.PI / oks;
                        if (Math.Abs(db) >= Math.Abs(dr)) return Val[i] + Math.CopySign(dr, db);
                        else if (Math.Abs(db) <= xDTh) return Val[i] + Math.CopySign(xDTh, db);
                        else return Val[i] + db;
                    }
                    else if (Math.Abs(Val[i]) < 1000.0)
                    {
                        db *= 20.0;
                        dr = 20.0 * Math.PI / oks;
                        if (Math.Abs(db) >= Math.Abs(dr)) return Val[i] + Math.CopySign(dr, db);
                        else if (Math.Abs(db) <= xDTh) return Val[i] + Math.CopySign(xDTh, db);
                        else return Val[i] + db;
                    }
                    else
                    {
                        db *= 200.0;
                        dr = 200.0 * Math.PI / oks;
                        if (Math.Abs(db) >= Math.Abs(dr)) return Val[i] + Math.CopySign(dr, db);
                        else if (Math.Abs(db) <= xDTh) return Val[i] + Math.CopySign(xDTh, db);
                        else return Val[i] + db;
                    }
                }
            }
            static internal double DSC(double d, double z)
            {
                if (Math.Abs(z) < 1E-4 || Math.Abs(z) > 1E4) throw new ArgumentOutOfRangeException("z", "Satori : Invalid number.");
                return z * Math.Cos(z * d);
            }
            internal override double DSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Satori : Invalid index.");
                double dtemp = DSC(Val[i], o);
                if (dtemp == 0.0) return Math.CopySign(xDTh, Val[i]);
                if (Math.Abs(dtemp) < xDTh) return Math.CopySign(xDTh, dtemp);
                return dtemp;
            }
            static internal double DS2C(double d, double z)
            {
                if (Math.Abs(z) < 1E-4 || Math.Abs(z) > 1E4) throw new ArgumentOutOfRangeException("z", "Satori : Invalid number.");
                return -z * z * Math.Sin(z * d);
            }
            internal override void TGsY(double d, int i)
            {
                if (i >= DtnY || i < 0) throw new ArgumentOutOfRangeException("i", "Satori : Invalid index.");
                ValY[i] = d;
                CFnY[i] = KSC(ValY[i], o);
            }
            internal override bool RepC(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool DGK(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool KS2(string msg)
            {
                throw new NotImplementedException();
            }
        }
        internal sealed class Nue : MyouRenJi//擬似正規分布、e^(x^2/c)
        {
            static private readonly string Mj = "封獣";
            static private readonly string Nm = "ぬえ";
            static private readonly string Mje = "Houjuu";
            static private readonly string Nme = "Nue";
            static internal readonly int THFI = 7;
            private double dThs = 10.0;//定義上限
            private double DThs;//上限微分値
            private double dThx = -10.0;//定義下限
            private double DThx;//上限微分値
            private double DTho;//原点微分値
            private double dTho = 1E-2;//原点定義範囲
            private double c = -20.0;//スケール
            private double ckt = 4.47213595499958;//更新式用閾値
            private double cks1 = 0.0447213595499958;//更新式用スケール1
            private double cks2 = 0.244721359549996;//更新式用スケール2
            internal override sealed string NM
            {
                get
                {
                    return Nm;
                }
            }
            internal double dTs
            {
                get
                {
                    return dThs;
                }
                set
                {
                    if (value < 0.0) throw new ArgumentOutOfRangeException("dTs", "Nue : Domain region is invalid.");
                    double dstemp = dThs;
                    dThs = value;
                    DThs = DSC(value, c);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] > dstemp && Val[cnt] < dThs) CFn[cnt] = KSC(Val[cnt], c);
                        else if (Val[cnt] > value) CFn[cnt] = 0.0;
                    }
                }
            }
            internal double dTx
            {
                get
                {
                    return dThx;
                }
                set
                {
                    if (value > 0.0) throw new ArgumentOutOfRangeException("dTx", "Nue : Domain region is invalid.");
                    double dxtemp = dThx;
                    dThx = value;
                    DThx = DSC(value, c);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] < dxtemp && Val[cnt] > dThx) CFn[cnt] = KSC(Val[cnt], c);
                        else if (Val[cnt] < value) CFn[cnt] = 0.0;
                    }
                }
            }
            internal double CD
            {
                get
                {
                    return c;
                }
                set
                {
                    if (value >= 0.0) throw new ArgumentOutOfRangeException("CD", "Nue : Invalid scale.");
                    c = CD;
                    DThs = DSC(dThs, value);
                    DThx = DSC(dThx, value);
                    DTho = DSC(dTho, value);
                    ckt = Math.Sqrt(-c);
                    cks1 = ckt * 0.1;
                    cks2 = cks1 - 0.1 * c;
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if ((Val[cnt] > dThx && Val[cnt] < -dTho) || (Val[cnt] < dThs && Val[cnt] > dTho)) CFn[cnt] = KSC(Val[cnt], value);
                    }
                }
            }
            internal Nue(int D) : base(D)
            {
                DThs = DSC(dThs, c);
                DThx = DSC(dThx, c);
                DTho = DSC(dTho, c);
            }
            internal Nue(int D, int[] nsl, int[] nsn, bool n) : base(D, nsl, nsn, n)
            {
                DThs = DSC(dThs, c);
                DThx = DSC(dThx, c);
                DTho = DSC(dTho, c);
            }
            internal Nue(int D, int[] nl, int[] nn, int[] sl, int[] sn) : base(D, nl, nn, sl, sn)
            {
                DThs = DSC(dThs, c);
                DThx = DSC(dThx, c);
                DTho = DSC(dTho, c);
            }
            internal Nue(int D, double s) : base(D)
            {
                if (s >= 0.0) throw new ArgumentOutOfRangeException("s", "Nue : Invalid scale.");
                c = s;
                DThs = DSC(dThs, c);
                DThx = DSC(dThx, c);
                DTho = DSC(dTho, c);
                ckt = Math.Sqrt(-c);
                cks1 = ckt * 0.1;
                cks2 = cks1 - 0.1 * c;
            }
            internal Nue(int D, int[] nsl, int[] nsn, bool n, double s) : base(D, nsl, nsn, n)
            {
                if (s >= 0.0) throw new ArgumentOutOfRangeException("s", "Nue : Invalid scale.");
                c = s;
                DThs = DSC(dThs, c);
                DThx = DSC(dThx, c);
                DTho = DSC(dTho, c);
                ckt = Math.Sqrt(-c);
                cks1 = ckt * 0.1;
                cks2 = cks1 - 0.1 * c;
            }
            internal Nue(int D, int[] nl, int[] nn, int[] sl, int[] sn, double s) : base(D, nl, nn, sl, sn)
            {
                if (s >= 0.0) throw new ArgumentOutOfRangeException("s", "Nue : Invalid scale.");
                c = s;
                DThs = DSC(dThs, c);
                DThx = DSC(dThx, c);
                DTho = DSC(dTho, c);
                ckt = Math.Sqrt(-c);
                cks1 = ckt * 0.1;
                cks2 = cks1 - 0.1 * c;
            }
            internal override void TGs(double[] d)
            {
                if (d.Length != Dtn) throw new ArgumentOutOfRangeException("d", "Nue : Wrong input length.");
                Val = d;
                for (int cnt = 0; cnt < Dtn; cnt++)
                {
                    if (Val[cnt] >= dThs || Val[cnt] <= dThx) CFn[cnt] = 0.0;
                    else if (Val[cnt] <= dTho && Val[cnt] >= dTho) CFn[cnt] = 1.0;
                    else CFn[cnt] = KSC(Val[cnt], c);
                }
            }
            internal override void TGs(double d, int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Nue : Invalid index.");
                Val[i] = d;
                if (Val[i] >= dThs || Val[i] <= dThx) CFn[i] = 0.0;
                else if (Val[i] <= dTho && Val[i] >= dTho) CFn[i] = 1.0;
                else CFn[i] = KSC(Val[i], c);
            }
            static internal double KSC(double d, double s)
            {
                if (s >= 0.0) throw new ArgumentOutOfRangeException("s", "Nue : Invalid scale.");
                return Math.Exp(d * d / s);
            }
            internal override double KSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Nue : Invalid index.");
                //if (Val[i] > dThs) return dThs;
                //if (Val[i] < dThx) return dThx;
                if (hbb[i] == 0.0) return Val[i];
                if ((Val[i] >= dThs || Val[i] <= dThx) && hbb[i] > 0.0) return Val[i];
                if (Val[i] >= -dTho && Val[i] <= dTho && hbb[i] < 0.0) return Val[i];
                double dtemp;
                if (hbb[i] < 0.0)
                {
                    if (Val[i] >= 0.0)
                    {
                        if (Val[i] > ckt)
                        {
                            dtemp = (hbb[i] <= -cks2) ? (Val[i] - cks2) : (Val[i] + hbb[i]);
                            if (dtemp >= dThs) return dThs;
                            if (dtemp <= ckt) return ckt;
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                        else
                        {
                            dtemp = (hbb[i] <= -cks1) ? (Val[i] - cks1) : (Val[i] + hbb[i]);
                            if (dtemp >= dThs) return dThs;
                            if (dtemp <= 0.0) return 0.0;
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                    }
                    else
                    {
                        if (Val[i] < -ckt)
                        {
                            dtemp = (hbb[i] <= -cks2) ? (Val[i] + cks2) : (Val[i] - hbb[i]);
                            if (dtemp <= dThx) return dThx;
                            if (dtemp >= -ckt) return -ckt;
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                        else
                        {
                            dtemp = (hbb[i] <= -cks1) ? (Val[i] + cks1) : (Val[i] - hbb[i]);
                            if (dtemp <= dThx) return dThx;
                            if (dtemp >= 0.0) return 0.0;
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                    }
                }
                else
                {
                    if (Val[i] >= 0.0)
                    {
                        if (Val[i] >= ckt)
                        {
                            dtemp = (hbb[i] >= cks2) ? (Val[i] + cks2) : (Val[i] + hbb[i]);
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                        else
                        {
                            dtemp = (hbb[i] >= cks1) ? (Val[i] + cks1) : (Val[i] + hbb[i]);
                            if (dtemp >= dThs) return dThs;
                            return (dtemp >= ckt) ? ckt : dtemp;
                        }
                    }
                    else
                    {
                        if (Val[i] <= -ckt)
                        {
                            dtemp = (hbb[i] >= cks2) ? (Val[i] - cks2) : (Val[i] - hbb[i]);
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                        else
                        {
                            dtemp = (hbb[i] >= cks1) ? (Val[i] - cks1) : (Val[i] - hbb[i]);
                            if (dtemp <= dThx) return dThx;
                            return (dtemp <= -ckt) ? -ckt : dtemp;
                        }
                    }
                }
            }
            static internal double DSC(double d, double s)
            {
                if (s >= 0.0) throw new ArgumentOutOfRangeException("s", "Nue : Invalid scale.");
                double dtemp = d / s;
                return 2.0 * dtemp * Math.Exp(d * dtemp);
            }
            internal override double DSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Nue : Invalid index.");
                if (hbb[i] == 0.0) return 0.0;
                bool Po = (hbb[i] < 0.0) ? true : false;
                if (Val[i] >= dThs)
                {
                    if (Po) return DThs;
                    else return 0.0;
                }
                else if (Val[i] <= dThx)
                {
                    if (Po) return DThx;
                    return 0.0;
                }
                else if (Val[i] <= dTho && Val[i] >= -dTho)
                {
                    if (Po) return 0.0;
                    else if (Val[i] == 0.0) return DTho;
                    else return Math.CopySign(DTho, Val[i]);
                }
                else return DSC(Val[i], c);
            }
            static internal double DS2C(double d, double s)
            {
                if (s >= 0.0) throw new ArgumentOutOfRangeException("s", "Nue : Invalid scale.");
                double dtemp = d / s;
                return (4.0 * dtemp * dtemp + 2.0 / s) * Math.Exp(d * dtemp);
            }
            internal override void TGsY(double d, int i)
            {
                if (i >= DtnY || i < 0) throw new ArgumentOutOfRangeException("i", "Nue : Invalid index.");
                ValY[i] = d;
                if (ValY[i] >= dThs || ValY[i] <= dThx) CFnY[i] = 0.0;
                else if (ValY[i] <= dTho && ValY[i] >= dTho) CFnY[i] = 1.0;
                else CFnY[i] = KSC(ValY[i], c);
            }
            internal override bool RepC(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool DGK(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool KS2(string msg)
            {
                throw new NotImplementedException();
            }
        }
        internal sealed class Murasa : MyouRenJi//擬似コーシー分布、y=c/(x^2+c)
        {
            static private readonly string Mj = "村紗";
            static private readonly string Nm = "水蜜";
            static private readonly string Mje = "Murasa";
            static private readonly string Nme = "Minamitsu";
            static internal readonly int THFI = 8;
            private double dThs = 33.3333333333333;//定義上限
            private double DThs;//上限微分値
            private double dThx = -33.3333333333333;//定義下限
            private double DThx;//上限微分値
            private double DTho;//原点微分値
            private double dTho = 1E-2;//原点定義範囲
            private double c = 0.333333333333333;//スケール
            private double cks = 0.0333333333333333;//更新式用スケール
            internal override sealed string NM
            {
                get
                {
                    return Nm;
                }
            }
            internal double dTs
            {
                get
                {
                    return dThs;
                }
                set
                {
                    if (value < 0.0) throw new ArgumentOutOfRangeException("dTs", "Murasa : Domain region is invalid.");
                    double dstemp = dThs;
                    dThs = value;
                    DThs = DSC(value, c);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] > dstemp && Val[cnt] < dThs) CFn[cnt] = KSC(Val[cnt], c);
                        else if (Val[cnt] > value) CFn[cnt] = 0.0;
                    }
                }
            }
            internal double dTx
            {
                get
                {
                    return dThx;
                }
                set
                {
                    if (value > 0.0) throw new ArgumentOutOfRangeException("dTx", "Murasa : Domain region is invalid.");
                    double dxtemp = dThx;
                    dThx = value;
                    DThx = DSC(value, c);
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if (Val[cnt] < dxtemp && Val[cnt] > dThx) CFn[cnt] = KSC(Val[cnt], c);
                        else if (Val[cnt] < value) CFn[cnt] = 0.0;
                    }
                }
            }
            internal double CD
            {
                get
                {
                    return c;
                }
                set
                {
                    if (CD <= 0.0) throw new ArgumentOutOfRangeException("CD", "Murasa : Invalid scale.");
                    c = CD;
                    DThs = DSC(dThs, value);
                    DThx = DSC(dThx, value);
                    DTho = DSC(dTho, value);
                    cks = c * 0.1;
                    for (int cnt = 0; cnt < Dtn; cnt++)
                    {
                        if ((Val[cnt] > dThx && Val[cnt] < -dTho) || (Val[cnt] < dThs && Val[cnt] > dTho)) CFn[cnt] = KSC(Val[cnt], value);
                    }
                }
            }
            internal Murasa(int D) : base(D)
            {
                DThs = DSC(dThs, c);
                DThx = DSC(dThx, c);
                DTho = DSC(dTho, c);
            }
            internal Murasa(int D, int[] nsl, int[] nsn, bool n) : base(D, nsl, nsn, n)
            {
                DThs = DSC(dThs, c);
                DThx = DSC(dThx, c);
                DTho = DSC(dTho, c);
            }
            internal Murasa(int D, int[] nl, int[] nn, int[] sl, int[] sn) : base(D, nl, nn, sl, sn)
            {
                DThs = DSC(dThs, c);
                DThx = DSC(dThx, c);
                DTho = DSC(dTho, c);
            }
            internal Murasa(int D, double s) : base(D)
            {
                if (s <= 0.0) throw new ArgumentOutOfRangeException("s", "Murasa : Invalid scale.");
                c = s;
                cks = c * 0.1;
                DThs = DSC(dThs, c);
                DThx = DSC(dThx, c);
                DTho = DSC(dTho, c);
            }
            internal Murasa(int D, int[] nsl, int[] nsn, bool n, double s) : base(D, nsl, nsn, n)
            {
                if (s <= 0.0) throw new ArgumentOutOfRangeException("s", "Murasa : Invalid scale.");
                c = s;
                cks = c * 0.1;
                DThs = DSC(dThs, c);
                DThx = DSC(dThx, c);
                DTho = DSC(dTho, c);
            }
            internal Murasa(int D, int[] nl, int[] nn, int[] sl, int[] sn, double s) : base(D, nl, nn, sl, sn)
            {
                if (s <= 0.0) throw new ArgumentOutOfRangeException("s", "Murasa : Invalid scale.");
                c = s;
                cks = c * 0.1;
                DThs = DSC(dThs, c);
                DThx = DSC(dThx, c);
                DTho = DSC(dTho, c);
            }
            internal override void TGs(double[] d)
            {
                if (d.Length != Dtn) throw new ArgumentOutOfRangeException("d", "Murasa : Wrong input length.");
                Val = d;
                for (int cnt = 0; cnt < Dtn; cnt++)
                {
                    if (Val[cnt] >= dThs || Val[cnt] <= dThx) CFn[cnt] = 0.0;
                    else if (Val[cnt] <= dTho && Val[cnt] >= dTho) CFn[cnt] = 1.0;
                    else CFn[cnt] = KSC(Val[cnt], c);
                }
            }
            internal override void TGs(double d, int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Murasa : Invalid index.");
                Val[i] = d;
                if (Val[i] >= dThs || Val[i] <= dThx) CFn[i] = 0.0;
                else if (Val[i] <= dTho && Val[i] >= dTho) CFn[i] = 1.0;
                else CFn[i] = KSC(Val[i], c);
            }
            static internal double KSC(double d, double s)
            {
                if (s <= 0.0) throw new ArgumentOutOfRangeException("s", "Murasa : Invalid scale.");
                return s / (d * d + s);
            }
            internal override double KSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Murasa : Invalid index.");
                //if (Val[i] > dThs) return dThs;
                //if (Val[i] < dThx) return dThx;
                if (hbb[i] == 0.0) return Val[i];
                if ((Val[i] >= dThs || Val[i] <= dThx) && hbb[i] > 0.0) return Val[i];
                if (Val[i] >= -dTho && Val[i] <= dTho && hbb[i] < 0.0) return Val[i];
                double dtemp;
                if (hbb[i] < 0.0)
                {
                    if (Val[i] >= 0.0)
                    {
                        if (Val[i] > c)
                        {
                            dtemp = (hbb[i] <= -c) ? (Val[i] - c) : (Val[i] + hbb[i]);
                            if (dtemp >= dThs) return dThs;
                            if (dtemp <= c) return c;
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                        else
                        {
                            dtemp = (hbb[i] <= -cks) ? (Val[i] - cks) : (Val[i] + hbb[i]);
                            if (dtemp >= dThs) return dThs;
                            if (dtemp <= 0.0) return 0.0;
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                    }
                    else
                    {
                        if (Val[i] < -c)
                        {
                            dtemp = (hbb[i] <= -c) ? (Val[i] + c) : (Val[i] - hbb[i]);
                            if (dtemp <= dThx) return dThx;
                            if (dtemp >= -c) return -c;
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                        else
                        {
                            dtemp = (hbb[i] <= -cks) ? (Val[i] + cks) : (Val[i] - hbb[i]);
                            if (dtemp <= dThx) return dThx;
                            if (dtemp >= 0.0) return 0.0;
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                    }
                }
                else
                {
                    if (Val[i] >= 0.0)
                    {
                        if (Val[i] >= c)
                        {
                            dtemp = (hbb[i] >= c) ? (Val[i] + c) : (Val[i] + hbb[i]);
                            return (dtemp >= dThs) ? dThs : dtemp;
                        }
                        else
                        {
                            dtemp = (hbb[i] >= cks) ? (Val[i] + cks) : (Val[i] + hbb[i]);
                            if (dtemp >= dThs) return dThs;
                            return (dtemp >= c) ? c : dtemp;
                        }
                    }
                    else
                    {
                        if (Val[i] <= -c)
                        {
                            dtemp = (hbb[i] >= c) ? (Val[i] - c) : (Val[i] - hbb[i]);
                            return (dtemp <= dThx) ? dThx : dtemp;
                        }
                        else
                        {
                            dtemp = (hbb[i] >= cks) ? (Val[i] - cks) : (Val[i] - hbb[i]);
                            if (dtemp <= dThx) return dThx;
                            return (dtemp <= -c) ? -c : dtemp;
                        }
                    }
                }
            }
            static internal double DSC(double d, double s)
            {
                if (s <= 0.0) throw new ArgumentOutOfRangeException("s", "Murasa : Invalid scale.");
                double dtemp = d * d + s;
                return -2.0 * d * s / dtemp / dtemp;
            }
            internal override double DSCn(int i)
            {
                if (i >= Dtn || i < 0) throw new ArgumentOutOfRangeException("i", "Murasa : Invalid index.");
                if (hbb[i] == 0.0) return 0.0;
                bool Po = (hbb[i] < 0.0) ? true : false;
                if (Val[i] >= dThs)
                {
                    if (Po) return DThs;
                    else return 0.0;
                }
                else if (Val[i] <= dThx)
                {
                    if (Po) return DThx;
                    return 0.0;
                }
                else if (Val[i] <= dTho && Val[i] >= -dTho)
                {
                    if (Po) return 0.0;
                    else if (Val[i] == 0.0) return DTho;
                    else return Math.CopySign(DTho, Val[i]);
                }
                else return DSC(Val[i], c);
            }
            static internal double DS2C(double d, double s)
            {
                if (s <= 0.0) throw new ArgumentOutOfRangeException("s", "Murasa : Invalid scale.");
                double dtemp = d * d + s;
                return -2.0 * s * (1 - 4.0 * d * d / dtemp) / dtemp / dtemp;
            }
            internal override void TGsY(double d, int i)
            {
                if (i >= DtnY || i < 0) throw new ArgumentOutOfRangeException("i", "Murasa : Invalid index.");
                ValY[i] = d;
                if (ValY[i] >= dThs || ValY[i] <= dThx) CFnY[i] = 0.0;
                else if (ValY[i] <= dTho && ValY[i] >= dTho) CFnY[i] = 1.0;
                else CFnY[i] = KSC(ValY[i], c);
            }
            internal override bool RepC(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool DGK(string msg)
            {
                throw new NotImplementedException();
            }
            internal override bool KS2(string msg)
            {
                throw new NotImplementedException();
            }
        }
    }
}