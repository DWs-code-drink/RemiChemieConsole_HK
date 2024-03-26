using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemiChemieConsole
{
    internal class SGKC
    {
        internal class Tensor : IFormattable
        {
            internal double[] vector;
            internal double[,] matrix;
            internal double[,,] tensor;
            internal double[,,,] tensor2;
            internal bool? tnc;
            internal int trank;
            internal int[] tsize;
            internal Tensor()
            {
                vector = null;
                matrix = null;
                tensor = null;
                tensor2 = null;
                tnc = null;
                tsize = null;
            }
            internal Tensor(double[] v, bool? b)
            {
                if (v.Length <= 0) throw new ArgumentOutOfRangeException("v.Length", "Tensor : Vector length is less than 1.");
                vector = v;
                matrix = null;
                tensor = null;
                tensor2 = null;
                if (v.Rank != 1) throw new ArgumentOutOfRangeException("rank", "Tensor : Unexpected rank.");
                if (v.Length == 1) tnc = null;
                else tnc = b;
                trank = 1;
                tsize = new int[1];
                tsize[0] = v.GetUpperBound(0) + 1;//ベクトル(配列)の長さ
            }
            internal Tensor(double[,] m)
            {
                if (m.GetUpperBound(0) < 0) throw new ArgumentOutOfRangeException("m.GetUpperBound(0)", "Tensor : Matrix row length is less than 1.");
                if (m.GetUpperBound(1) < 0) throw new ArgumentOutOfRangeException("m.GetUpperBound(1)", "Tensor : Matrix colomn length is less than 1.");
                matrix = m;
                vector = null;
                tensor = null;
                tensor2 = null;
                if (m.Rank != 2) throw new ArgumentOutOfRangeException("rank", "Tensor : Unexpected rank.");
                tnc = null;
                trank = 2;
                tsize = new int[2];
                tsize[0] = m.GetUpperBound(0) + 1;//行数(列の長さ)
                tsize[1] = m.GetUpperBound(1) + 1;//列数(行の長さ)
            }
            internal Tensor(double[,,] t)
            {
                if (t.GetUpperBound(0) < 0) throw new ArgumentOutOfRangeException("t.GetUpperBound(0)", "Tensor : Tensor row length is less than 1.");
                if (t.GetUpperBound(1) < 0) throw new ArgumentOutOfRangeException("t.GetUpperBound(1)", "Tensor : Tensor colomn length is less than 1.");
                if (t.GetUpperBound(2) < 0) throw new ArgumentOutOfRangeException("t.GetUpperBound(2)", "Tensor : Tensor depth length is less than 1.");
                tensor = t;
                vector = null;
                matrix = null;
                tensor2 = null;
                if (t.Rank != 3) throw new ArgumentOutOfRangeException("rank", "Tensor : Unexpected rank.");
                tnc = null;
                trank = 3;
                tsize = new int[3];
                tsize[0] = t.GetUpperBound(0) + 1;//次元1の長さ
                tsize[1] = t.GetUpperBound(1) + 1;//次元2の長さ
                tsize[2] = t.GetUpperBound(2) + 1;//次元3の長さ
            }
            internal Tensor(double[,,,] t2)
            {
                if (t2.GetUpperBound(0) < 0) throw new ArgumentOutOfRangeException("t2.GetUpperBound(0)", "Tensor : Tensor2 row length is less than 1.");
                if (t2.GetUpperBound(1) < 0) throw new ArgumentOutOfRangeException("t2.GetUpperBound(1)", "Tensor : Tensor2 colomn length is less than 1.");
                if (t2.GetUpperBound(2) < 0) throw new ArgumentOutOfRangeException("t2.GetUpperBound(2)", "Tensor : Tensor2 depth length is less than 1.");
                if (t2.GetUpperBound(3) < 0) throw new ArgumentOutOfRangeException("t2.GetUpperBound(3)", "Tensor : Tensor2 hyperdepth length is less than 1.");
                tensor2 = t2;
                vector = null;
                matrix = null;
                tensor = null;
                if (t2.Rank != 4) throw new ArgumentOutOfRangeException("rank", "Tensor : Unexpected rank.");
                tnc = null;
                trank = 4;
                tsize = new int[4];
                tsize[0] = t2.GetUpperBound(0) + 1;//次元1の長さ
                tsize[1] = t2.GetUpperBound(1) + 1;//次元2の長さ
                tsize[2] = t2.GetUpperBound(2) + 1;//次元3の長さ
                tsize[3] = t2.GetUpperBound(3) + 1;//次元4の長さ
            }
            internal double TSeki()//跡を求める
            {
                switch (trank)
                {
                    case 2:
                        {
                            if (tsize[0] != tsize[1]) throw new ArgumentOutOfRangeException("tsize", "TS eki : Can not calculate trace of non-square matrix.");
                            double res = 0;
                            for (int cnt = 0; cnt < tsize[0]; cnt++)
                            {
                                res += matrix[cnt, cnt];
                            }
                            return res;
                        }
                    case 3:
                        {
                            if (tsize[0] != tsize[1] || tsize[1] != tsize[2]) throw new ArgumentOutOfRangeException("tsize", "TS eki : Can not calculate trace of non-cube tensor.");
                            double res = 0;
                            for (int cnt = 0; cnt < tsize[0]; cnt++)
                            {
                                res += tensor[cnt, cnt, cnt];
                            }
                            return res;
                        }
                    case 4:
                        {
                            if (tsize[0] != tsize[1] || tsize[1] != tsize[2] || tsize[2] != tsize[3]) throw new ArgumentOutOfRangeException("tsize", "TS eki : Can not calculate trace of non-hypercube tensor2.");
                            double res = 0;
                            for (int cnt = 0; cnt < tsize[0]; cnt++)
                            {
                                res += tensor2[cnt, cnt, cnt, cnt];
                            }
                            return res;
                        }
                    default: throw new NotImplementedException("TS eki : Not implemented.");
                }
            }
            static internal double TSekis(Tensor t)//跡を求める(静的メソッド)
            {
                switch (t.trank)
                {
                    case 2:
                        {
                            if (t.tsize[0] != t.tsize[1]) throw new ArgumentOutOfRangeException("tsize", "TS eki : Can not calculate trace of non-square matrix.");
                            double res = 0;
                            for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                            {
                                res += t.matrix[cnt, cnt];
                            }
                            return res;
                        }
                    case 3:
                        {
                            if (t.tsize[0] != t.tsize[1] || t.tsize[1] != t.tsize[2]) throw new ArgumentOutOfRangeException("tsize", "TS eki : Can not calculate trace of non-cube tensor.");
                            double res = 0;
                            for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                            {
                                res += t.tensor[cnt, cnt, cnt];
                            }
                            return res;
                        }
                    case 4:
                        {
                            if (t.tsize[0] != t.tsize[1] || t.tsize[1] != t.tsize[2] || t.tsize[2] != t.tsize[3]) throw new ArgumentOutOfRangeException("tsize", "TS eki : Can not calculate trace of non-hypercube tensor2.");
                            double res = 0;
                            for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                            {
                                res += t.tensor2[cnt, cnt, cnt, cnt];
                            }
                            return res;
                        }
                    default: throw new NotImplementedException("TS eki : Not implemented.");
                }
            }
            internal double EucN()//ベクトルのユークリッドノルムを求める
            {
                if (trank != 1) throw new NotImplementedException("EucN : Only Euclidian norm of vector can be calculated.");
                double res = 0;
                ParallelOptions po = new ParallelOptions();
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                po.CancellationToken = ct;
                po.MaxDegreeOfParallelism = CommonParam.thdn;
                po.TaskScheduler = TaskScheduler.Default;
                object lo = new object();
                Parallel.ForEach(vector, po, (dtemp) =>
                {
                    double d = dtemp * dtemp;
                    lock (lo) res += d;
                });
                return Math.Sqrt(res);
            }
            static internal double EucNs(in double[] vector)//ベクトルのユークリッドノルムを求める(静的メソッド)
            {
                double res = 0;
                ParallelOptions po = new ParallelOptions();
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                po.CancellationToken = ct;
                po.MaxDegreeOfParallelism = CommonParam.thdn;
                po.TaskScheduler = TaskScheduler.Default;
                object lo = new object();
                Parallel.ForEach(vector, po, (dtemp) =>
                {
                    double d = dtemp * dtemp;
                    lock (lo) res += d;
                });
                return Math.Sqrt(res);
            }
            static internal double MahNs(in double[] vector)//ベクトルのマンハッタンノルムを求める(静的メソッド)
            {
                double res = 0;
                ParallelOptions po = new ParallelOptions();
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                po.CancellationToken = ct;
                po.MaxDegreeOfParallelism = CommonParam.thdn;
                po.TaskScheduler = TaskScheduler.Default;
                object lo = new object();
                Parallel.ForEach(vector, po, (dtemp) =>
                {
                    double d = Math.Abs(dtemp);
                    lock (lo) res += d;
                });
                return res;
            }
            static internal double DproVVs(double[] v1, double[] v2)//ベクトルとベクトルのドット積を求める(静的メソッド)
            {
                if (v1.Length != v2.Length) throw new ArgumentOutOfRangeException("DproVVs : Vectors' length must be the same.");
                ParallelOptions po = new ParallelOptions();
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                po.CancellationToken = ct;
                po.MaxDegreeOfParallelism = CommonParam.thdn;
                po.TaskScheduler = TaskScheduler.Default;
                object lo = new object();
                double dres = 0;
                Parallel.For(0, v1.Length, po, (cnt) =>
                {
                    double dtemp = v1[cnt] * v2[cnt];
                    lock (lo) dres += dtemp;
                });
                return dres;
            }
            static internal double CosVVs(double[] v1, double[] v2)//ベクトル間のcosθを求める
            {
                if (v1.Length != v2.Length) throw new ArgumentOutOfRangeException("CosVVs : Vectors' length must be the same.");
                ParallelOptions po = new ParallelOptions();
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                po.CancellationToken = ct;
                po.MaxDegreeOfParallelism = CommonParam.thdn;
                po.TaskScheduler = TaskScheduler.Default;
                object lo = new object();
                double ds = 0;
                double eu1 = 0;
                double eu2 = 0;
                Parallel.For(0, v1.Length, po, (cnt) =>
                {
                    double dstemp = v1[cnt] * v2[cnt];
                    double eu1temp = v1[cnt] * v1[cnt];
                    double eu2temp = v2[cnt] * v2[cnt];
                    lock (lo)
                    {
                        ds += dstemp;
                        eu1 += eu1temp;
                        eu2 += eu2temp;
                    }
                });
                if (eu1 == 0 || eu2 == 0) throw new ArgumentOutOfRangeException("eu2", "CosVVs : vector norm is 0.");
                ds /= eu1 * eu2;
                if (ds > 1.00000000000925 || ds < -1.00000000000925) throw new ArgumentOutOfRangeException(String.Format("ds={0}", ds), "CosVVs : cosθ is larger than 1 or smaller than -1.");
                if (ds > 1) ds = 1;
                else if (ds < -1) ds = -1;
                return ds;
            }
            static internal double[] Cseki(double[] v1, double[] v2)//三次元ベクトルのクロス積v1×v2を求める
            {
                if (v1.Length != 3 || v2.Length != 3) throw new ArgumentOutOfRangeException("v1/v2", "Ks eki : Vector dimension is not 3.");
                double d0 = v1[1] * v2[2] - v1[2] * v2[1];
                double d1 = v1[2] * v2[0] - v1[0] * v2[2];
                double d2 = v1[0] * v2[1] - v1[1] * v2[0];
                double[] ds = new double[3] { d0, d1, d2 };
                return ds;
            }
            static internal double Kseki(double[] v1, double[] v2)//ベクトルの外積v1v2を求める
            {
                if (v1.Length != 2 || v2.Length != 2) throw new NotImplementedException("Ks eki : Method for this vector legnth is not implemented");
                double dres = v1[0] * v2[1] - v1[1] * v2[0];
                return dres;
            }
            static internal double Kseki(double[] v1, double[] v2, double[] v3)//ベクトルの外積v1v2v3(スカラー三重積)を求める
            {
                if (v1.Length != 2 || v2.Length != 2) throw new NotImplementedException("Ks eki : Method for this vector legnth is not implemented");
                double dres = v1[0] * v2[1] * v3[2] - v1[0] * v2[2] * v3[1] + v1[1] * v2[2] * v3[0] - v1[1] * v2[0] * v3[2] + v1[2] * v2[0] * v3[1] - v1[2] * v2[1] * v3[0];
                return dres;
            }
            static internal double[] BSseki(double[] v1, double[] v2, double[] v3)//ベクトルのベクトル三重積を求める
            {
                if (v1.Length != 3 || v2.Length != 3 || v3.Length != 3) throw new ArgumentOutOfRangeException("v1/v2/v3", "BSs eki : Method is only for 3 dimension vectors.");
                return Cseki(v1, Cseki(v2, v3));
            }
            static internal double PSinVVVs(double[] v1, double[] v2, double[] v3)//三つベクトルのポーラー正弦を求める
            {
                if (v1.Length != 3 || v2.Length != 3 || v3.Length != 3) throw new NotImplementedException("PSinVVVs : Method for non-3D vectors is not implemented.");
                double[] vc = Cseki(v2, v3);
                double[] dres = new double[4] { 0.0, 0.0, 0.0, 0.0 };
                ParallelOptions po = new ParallelOptions();
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                po.CancellationToken = ct;
                po.MaxDegreeOfParallelism = CommonParam.thdn;
                po.TaskScheduler = TaskScheduler.Default;
                Parallel.For(0, 3, po, (cnt) =>
                {
                    double dtemp = v1[cnt] * vc[cnt];
                    double dtemp2 = v1[cnt] * v1[cnt];
                    double dtemp3 = v2[cnt] * v2[cnt];
                    double dtemp4 = v3[cnt] * v3[cnt];
                    lock (dres.SyncRoot)
                    {
                        dres[0] += dtemp;
                        dres[1] += dtemp2;
                        dres[2] += dtemp3;
                        dres[3] += dtemp4;
                    }
                });
                if (dres[1] == 0 || dres[2] == 0 || dres[3] == 0) throw new ArgumentOutOfRangeException("dres[1]/dres[2]/dres[3]", "PSinVVVs : Vector norm is 0.");
                double ds = dres[0] / (dres[1] * dres[2] * dres[3]);
                if (ds > 1.00000000000925 || ds < -1.00000000000925) throw new ArgumentOutOfRangeException(String.Format("ds={0}", ds), "PSinVVVs : psin is larger than 1 or smaller than -1.");
                if (ds > 1) ds = 1;
                else if (ds < -1) ds = -1;
                return ds;
            }
            internal double FbN()//フロベニウスノルムを求める
            {
                double res = 0;
                switch (trank)
                {
                    case 1:
                        {
                            res = EucN();
                            break;
                        }
                    case 2:
                        {
                            ParallelOptions po = new ParallelOptions();
                            CancellationTokenSource cts = new CancellationTokenSource();
                            CancellationToken ct = cts.Token;
                            po.CancellationToken = ct;
                            po.MaxDegreeOfParallelism = CommonParam.thdn;
                            po.TaskScheduler = TaskScheduler.Default;
                            object lo = new object();
                            if (tsize[0] >= tsize[1])
                            {
                                Parallel.For(0, tsize[0], po, (cnt) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                    {
                                        dtemp += Math.Pow(matrix[cnt, cnt2], 2);
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else
                            {
                                Parallel.For(0, tsize[1], po, (cnt2) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < tsize[0]; cnt++)
                                    {
                                        dtemp += Math.Pow(matrix[cnt, cnt2], 2);
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            break;
                        }
                    case 3:
                        {
                            ParallelOptions po = new ParallelOptions();
                            CancellationTokenSource cts = new CancellationTokenSource();
                            CancellationToken ct = cts.Token;
                            po.CancellationToken = ct;
                            po.MaxDegreeOfParallelism = CommonParam.thdn;
                            po.TaskScheduler = TaskScheduler.Default;
                            object lo = new object();
                            if (tsize[0] >= tsize[1] && tsize[0] >= tsize[2])
                            {
                                Parallel.For(0, tsize[0], po, (cnt) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                    {
                                        for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                        {
                                            dtemp += Math.Pow(tensor[cnt, cnt2, cnt3], 2);
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else if (tsize[1] >= tsize[2] && tsize[1] >= tsize[0])
                            {
                                Parallel.For(0, tsize[1], po, (cnt2) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < tsize[0]; cnt++)
                                    {
                                        for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                        {
                                            dtemp += Math.Pow(tensor[cnt, cnt2, cnt3], 2);
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else
                            {
                                Parallel.For(0, tsize[2], po, (cnt3) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < tsize[0]; cnt++)
                                    {
                                        for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                        {
                                            dtemp += Math.Pow(tensor[cnt, cnt2, cnt3], 2);
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            break;
                        }
                    case 4:
                        {
                            ParallelOptions po = new ParallelOptions();
                            CancellationTokenSource cts = new CancellationTokenSource();
                            CancellationToken ct = cts.Token;
                            po.CancellationToken = ct;
                            po.MaxDegreeOfParallelism = CommonParam.thdn;
                            po.TaskScheduler = TaskScheduler.Default;
                            object lo = new object();
                            if (tsize[0] >= tsize[1] && tsize[0] >= tsize[2] && tsize[0] >= tsize[3])
                            {
                                Parallel.For(0, tsize[0], po, (cnt) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                    {
                                        for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                        {
                                            for (int cnt4 = 0; cnt4 < tsize[3]; cnt4++)
                                            {
                                                dtemp += Math.Pow(tensor2[cnt, cnt2, cnt3, cnt4], 2);
                                            }
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else if (tsize[1] >= tsize[2] && tsize[1] >= tsize[0] && tsize[1] >= tsize[3])
                            {
                                Parallel.For(0, tsize[1], po, (cnt2) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < tsize[0]; cnt++)
                                    {
                                        for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                        {
                                            for (int cnt4 = 0; cnt4 < tsize[3]; cnt4++)
                                            {
                                                dtemp += Math.Pow(tensor2[cnt, cnt2, cnt3, cnt4], 2);
                                            }
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else if (tsize[2] >= tsize[1] && tsize[2] >= tsize[0] && tsize[2] >= tsize[3])
                            {
                                Parallel.For(0, tsize[2], po, (cnt3) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < tsize[0]; cnt++)
                                    {
                                        for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                        {
                                            for (int cnt4 = 0; cnt4 < tsize[3]; cnt4++)
                                            {
                                                dtemp += Math.Pow(tensor2[cnt, cnt2, cnt3, cnt4], 2);
                                            }
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else
                            {
                                Parallel.For(0, tsize[3], po, (cnt4) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < tsize[0]; cnt++)
                                    {
                                        for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                        {
                                            for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                            {
                                                dtemp += Math.Pow(tensor2[cnt, cnt2, cnt3, cnt4], 2);
                                            }
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            break;
                        }
                    default: throw new NotImplementedException("FbN : Method for this rank is not implemented.");
                }
                return Math.Sqrt(res);
            }
            static internal double FbNs(Tensor t)//フロベニウスノルムを求める(静的メソッド)
            {
                double res = 0;
                switch (t.trank)
                {
                    case 1:
                        {
                            res = t.EucN();
                            break;
                        }
                    case 2:
                        {
                            ParallelOptions po = new ParallelOptions();
                            CancellationTokenSource cts = new CancellationTokenSource();
                            CancellationToken ct = cts.Token;
                            po.CancellationToken = ct;
                            po.MaxDegreeOfParallelism = CommonParam.thdn;
                            po.TaskScheduler = TaskScheduler.Default;
                            object lo = new object();
                            if (t.tsize[0] >= t.tsize[1])
                            {
                                Parallel.For(0, t.tsize[0], po, (cnt) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt2 = 0; cnt2 < t.tsize[1]; cnt2++)
                                    {
                                        dtemp += Math.Pow(t.matrix[cnt, cnt2], 2);
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else
                            {
                                Parallel.For(0, t.tsize[1], po, (cnt2) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                                    {
                                        dtemp += Math.Pow(t.matrix[cnt, cnt2], 2);
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            break;
                        }
                    case 3:
                        {
                            ParallelOptions po = new ParallelOptions();
                            CancellationTokenSource cts = new CancellationTokenSource();
                            CancellationToken ct = cts.Token;
                            po.CancellationToken = ct;
                            po.MaxDegreeOfParallelism = CommonParam.thdn;
                            po.TaskScheduler = TaskScheduler.Default;
                            object lo = new object();
                            if (t.tsize[0] >= t.tsize[1] && t.tsize[0] >= t.tsize[2])
                            {
                                Parallel.For(0, t.tsize[0], po, (cnt) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt2 = 0; cnt2 < t.tsize[1]; cnt2++)
                                    {
                                        for (int cnt3 = 0; cnt3 < t.tsize[2]; cnt3++)
                                        {
                                            dtemp += Math.Pow(t.tensor[cnt, cnt2, cnt3], 2);
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else if (t.tsize[1] >= t.tsize[2] && t.tsize[1] >= t.tsize[0])
                            {
                                Parallel.For(0, t.tsize[1], po, (cnt2) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                                    {
                                        for (int cnt3 = 0; cnt3 < t.tsize[2]; cnt3++)
                                        {
                                            dtemp += Math.Pow(t.tensor[cnt, cnt2, cnt3], 2);
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else
                            {
                                Parallel.For(0, t.tsize[2], po, (cnt3) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                                    {
                                        for (int cnt2 = 0; cnt2 < t.tsize[1]; cnt2++)
                                        {
                                            dtemp += Math.Pow(t.tensor[cnt, cnt2, cnt3], 2);
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            break;
                        }
                    case 4:
                        {
                            ParallelOptions po = new ParallelOptions();
                            CancellationTokenSource cts = new CancellationTokenSource();
                            CancellationToken ct = cts.Token;
                            po.CancellationToken = ct;
                            po.MaxDegreeOfParallelism = CommonParam.thdn;
                            po.TaskScheduler = TaskScheduler.Default;
                            object lo = new object();
                            if (t.tsize[0] >= t.tsize[1] && t.tsize[0] >= t.tsize[2] && t.tsize[0] >= t.tsize[3])
                            {
                                Parallel.For(0, t.tsize[0], po, (cnt) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt2 = 0; cnt2 < t.tsize[1]; cnt2++)
                                    {
                                        for (int cnt3 = 0; cnt3 < t.tsize[2]; cnt3++)
                                        {
                                            for (int cnt4 = 0; cnt4 < t.tsize[3]; cnt4++)
                                            {
                                                dtemp += Math.Pow(t.tensor2[cnt, cnt2, cnt3, cnt4], 2);
                                            }
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else if (t.tsize[1] >= t.tsize[2] && t.tsize[1] >= t.tsize[0] && t.tsize[1] >= t.tsize[3])
                            {
                                Parallel.For(0, t.tsize[1], po, (cnt2) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                                    {
                                        for (int cnt3 = 0; cnt3 < t.tsize[2]; cnt3++)
                                        {
                                            for (int cnt4 = 0; cnt4 < t.tsize[3]; cnt4++)
                                            {
                                                dtemp += Math.Pow(t.tensor2[cnt, cnt2, cnt3, cnt4], 2);
                                            }
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else if (t.tsize[2] >= t.tsize[1] && t.tsize[2] >= t.tsize[0] && t.tsize[2] >= t.tsize[3])
                            {
                                Parallel.For(0, t.tsize[2], po, (cnt3) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                                    {
                                        for (int cnt2 = 0; cnt2 < t.tsize[1]; cnt2++)
                                        {
                                            for (int cnt4 = 0; cnt4 < t.tsize[3]; cnt4++)
                                            {
                                                dtemp += Math.Pow(t.tensor2[cnt, cnt2, cnt3, cnt4], 2);
                                            }
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else
                            {
                                Parallel.For(0, t.tsize[3], po, (cnt4) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                                    {
                                        for (int cnt2 = 0; cnt2 < t.tsize[1]; cnt2++)
                                        {
                                            for (int cnt3 = 0; cnt3 < t.tsize[2]; cnt3++)
                                            {
                                                dtemp += Math.Pow(t.tensor2[cnt, cnt2, cnt3, cnt4], 2);
                                            }
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            break;
                        }
                    default: throw new NotImplementedException("FbN : Method for this rank is not implemented.");
                }
                return Math.Sqrt(res);
            }
            internal double MhN()//p=1ノルムを求める
            {
                double res = 0;
                switch (trank)
                {
                    case 1:
                        {
                            res = MahNs(in vector);
                            break;
                        }
                    case 2:
                        {
                            ParallelOptions po = new ParallelOptions();
                            CancellationTokenSource cts = new CancellationTokenSource();
                            CancellationToken ct = cts.Token;
                            po.CancellationToken = ct;
                            po.MaxDegreeOfParallelism = CommonParam.thdn;
                            po.TaskScheduler = TaskScheduler.Default;
                            object lo = new object();
                            if (tsize[0] >= tsize[1])
                            {
                                Parallel.For(0, tsize[0], po, (cnt) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                    {
                                        dtemp += Math.Abs(matrix[cnt, cnt2]);
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else
                            {
                                Parallel.For(0, tsize[1], po, (cnt2) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < tsize[0]; cnt++)
                                    {
                                        dtemp += Math.Abs(matrix[cnt, cnt2]);
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            break;
                        }
                    case 3:
                        {
                            ParallelOptions po = new ParallelOptions();
                            CancellationTokenSource cts = new CancellationTokenSource();
                            CancellationToken ct = cts.Token;
                            po.CancellationToken = ct;
                            po.MaxDegreeOfParallelism = CommonParam.thdn;
                            po.TaskScheduler = TaskScheduler.Default;
                            object lo = new object();
                            if (tsize[0] >= tsize[1] && tsize[0] >= tsize[2])
                            {
                                Parallel.For(0, tsize[0], po, (cnt) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                    {
                                        for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                        {
                                            dtemp += Math.Abs(tensor[cnt, cnt2, cnt3]);
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else if (tsize[1] >= tsize[2] && tsize[1] >= tsize[0])
                            {
                                Parallel.For(0, tsize[1], po, (cnt2) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < tsize[0]; cnt++)
                                    {
                                        for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                        {
                                            dtemp += Math.Abs(tensor[cnt, cnt2, cnt3]);
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else
                            {
                                Parallel.For(0, tsize[2], po, (cnt3) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < tsize[0]; cnt++)
                                    {
                                        for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                        {
                                            dtemp += Math.Abs(tensor[cnt, cnt2, cnt3]);
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            break;
                        }
                    case 4:
                        {
                            ParallelOptions po = new ParallelOptions();
                            CancellationTokenSource cts = new CancellationTokenSource();
                            CancellationToken ct = cts.Token;
                            po.CancellationToken = ct;
                            po.MaxDegreeOfParallelism = CommonParam.thdn;
                            po.TaskScheduler = TaskScheduler.Default;
                            object lo = new object();
                            if (tsize[0] >= tsize[1] && tsize[0] >= tsize[2] && tsize[0] >= tsize[3])
                            {
                                Parallel.For(0, tsize[0], po, (cnt) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                    {
                                        for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                        {
                                            for (int cnt4 = 0; cnt4 < tsize[3]; cnt4++)
                                            {
                                                dtemp += Math.Abs(tensor2[cnt, cnt2, cnt3, cnt4]);
                                            }
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else if (tsize[1] >= tsize[2] && tsize[1] >= tsize[0] && tsize[1] >= tsize[3])
                            {
                                Parallel.For(0, tsize[1], po, (cnt2) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < tsize[0]; cnt++)
                                    {
                                        for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                        {
                                            for (int cnt4 = 0; cnt4 < tsize[3]; cnt4++)
                                            {
                                                dtemp += Math.Abs(tensor2[cnt, cnt2, cnt3, cnt4]);
                                            }
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else if (tsize[2] >= tsize[1] && tsize[2] >= tsize[0] && tsize[2] >= tsize[3])
                            {
                                Parallel.For(0, tsize[2], po, (cnt3) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < tsize[0]; cnt++)
                                    {
                                        for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                        {
                                            for (int cnt4 = 0; cnt4 < tsize[3]; cnt4++)
                                            {
                                                dtemp += Math.Abs(tensor2[cnt, cnt2, cnt3, cnt4]);
                                            }
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else
                            {
                                Parallel.For(0, tsize[3], po, (cnt4) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < tsize[0]; cnt++)
                                    {
                                        for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                        {
                                            for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                            {
                                                dtemp += Math.Abs(tensor2[cnt, cnt2, cnt3, cnt4]);
                                            }
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            break;
                        }
                    default: throw new NotImplementedException("FbN : Method for this rank is not implemented.");
                }
                return res;
            }
            static internal double MhNs(Tensor t)//p=1ノルムを求める(静的メソッド)
            {
                double res = 0;
                switch (t.trank)
                {
                    case 1:
                        {
                            res = MahNs(in t.vector);
                            break;
                        }
                    case 2:
                        {
                            ParallelOptions po = new ParallelOptions();
                            CancellationTokenSource cts = new CancellationTokenSource();
                            CancellationToken ct = cts.Token;
                            po.CancellationToken = ct;
                            po.MaxDegreeOfParallelism = CommonParam.thdn;
                            po.TaskScheduler = TaskScheduler.Default;
                            object lo = new object();
                            if (t.tsize[0] >= t.tsize[1])
                            {
                                Parallel.For(0, t.tsize[0], po, (cnt) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt2 = 0; cnt2 < t.tsize[1]; cnt2++)
                                    {
                                        dtemp += Math.Abs(t.matrix[cnt, cnt2]);
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else
                            {
                                Parallel.For(0, t.tsize[1], po, (cnt2) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                                    {
                                        dtemp += Math.Abs(t.matrix[cnt, cnt2]);
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            break;
                        }
                    case 3:
                        {
                            ParallelOptions po = new ParallelOptions();
                            CancellationTokenSource cts = new CancellationTokenSource();
                            CancellationToken ct = cts.Token;
                            po.CancellationToken = ct;
                            po.MaxDegreeOfParallelism = CommonParam.thdn;
                            po.TaskScheduler = TaskScheduler.Default;
                            object lo = new object();
                            if (t.tsize[0] >= t.tsize[1] && t.tsize[0] >= t.tsize[2])
                            {
                                Parallel.For(0, t.tsize[0], po, (cnt) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt2 = 0; cnt2 < t.tsize[1]; cnt2++)
                                    {
                                        for (int cnt3 = 0; cnt3 < t.tsize[2]; cnt3++)
                                        {
                                            dtemp += Math.Abs(t.tensor[cnt, cnt2, cnt3]);
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else if (t.tsize[1] >= t.tsize[2] && t.tsize[1] >= t.tsize[0])
                            {
                                Parallel.For(0, t.tsize[1], po, (cnt2) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                                    {
                                        for (int cnt3 = 0; cnt3 < t.tsize[2]; cnt3++)
                                        {
                                            dtemp += Math.Abs(t.tensor[cnt, cnt2, cnt3]);
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else
                            {
                                Parallel.For(0, t.tsize[2], po, (cnt3) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                                    {
                                        for (int cnt2 = 0; cnt2 < t.tsize[1]; cnt2++)
                                        {
                                            dtemp += Math.Abs(t.tensor[cnt, cnt2, cnt3]);
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            break;
                        }
                    case 4:
                        {
                            ParallelOptions po = new ParallelOptions();
                            CancellationTokenSource cts = new CancellationTokenSource();
                            CancellationToken ct = cts.Token;
                            po.CancellationToken = ct;
                            po.MaxDegreeOfParallelism = CommonParam.thdn;
                            po.TaskScheduler = TaskScheduler.Default;
                            object lo = new object();
                            if (t.tsize[0] >= t.tsize[1] && t.tsize[0] >= t.tsize[2] && t.tsize[0] >= t.tsize[3])
                            {
                                Parallel.For(0, t.tsize[0], po, (cnt) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt2 = 0; cnt2 < t.tsize[1]; cnt2++)
                                    {
                                        for (int cnt3 = 0; cnt3 < t.tsize[2]; cnt3++)
                                        {
                                            for (int cnt4 = 0; cnt4 < t.tsize[3]; cnt4++)
                                            {
                                                dtemp += Math.Abs(t.tensor2[cnt, cnt2, cnt3, cnt4]);
                                            }
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else if (t.tsize[1] >= t.tsize[2] && t.tsize[1] >= t.tsize[0] && t.tsize[1] >= t.tsize[3])
                            {
                                Parallel.For(0, t.tsize[1], po, (cnt2) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                                    {
                                        for (int cnt3 = 0; cnt3 < t.tsize[2]; cnt3++)
                                        {
                                            for (int cnt4 = 0; cnt4 < t.tsize[3]; cnt4++)
                                            {
                                                dtemp += Math.Abs(t.tensor2[cnt, cnt2, cnt3, cnt4]);
                                            }
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else if (t.tsize[2] >= t.tsize[1] && t.tsize[2] >= t.tsize[0] && t.tsize[2] >= t.tsize[3])
                            {
                                Parallel.For(0, t.tsize[2], po, (cnt3) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                                    {
                                        for (int cnt2 = 0; cnt2 < t.tsize[1]; cnt2++)
                                        {
                                            for (int cnt4 = 0; cnt4 < t.tsize[3]; cnt4++)
                                            {
                                                dtemp += Math.Abs(t.tensor2[cnt, cnt2, cnt3, cnt4]);
                                            }
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            else
                            {
                                Parallel.For(0, t.tsize[3], po, (cnt4) =>
                                {
                                    double dtemp = 0;
                                    for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                                    {
                                        for (int cnt2 = 0; cnt2 < t.tsize[1]; cnt2++)
                                        {
                                            for (int cnt3 = 0; cnt3 < t.tsize[2]; cnt3++)
                                            {
                                                dtemp += Math.Abs(t.tensor2[cnt, cnt2, cnt3, cnt4]);
                                            }
                                        }
                                    }
                                    lock (lo) res += dtemp;
                                });
                            }
                            break;
                        }
                    default: throw new NotImplementedException("FbN : Method for this rank is not implemented.");
                }
                return res;
            }
            internal double HLSmin()//小行列式を使って行列式を求める
            {
                if (trank != 2 || tsize[0] != tsize[1]) throw new ArgumentOutOfRangeException("trank/tsize", "HLS : Not square matrix.");
                List<int> li = new List<int>();
                for (int cnt = 0; cnt < tsize[0]; cnt++)
                {
                    li.Add(cnt);
                }
                double dres = 0;
                ParallelOptions po = new ParallelOptions();
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                po.CancellationToken = ct;
                po.MaxDegreeOfParallelism = CommonParam.thdn;
                po.TaskScheduler = TaskScheduler.Default;
                object lo = new object();
                object lo2 = new object();
                int lil = tsize[0] - 1;
                Parallel.ForEach(li, po, (ind) =>
                {
                    List<int> lic;
                    lock (lo) lic = new List<int>(li);
                    lic.RemoveAt(ind);
                    double drt;
                    if (matrix[0, ind] == 0) return;
                    drt = HLSminsub(in lic, 1) * (ind % 2 == 0 ? 1 : -1) * matrix[0, ind];
                    lock (lo2) dres += drt;
                });
                return dres;
            }
            private double HLSminsub(in List<int> li, int ily)//小行列式の小行列式分解
            {
                if (li.Count == 1)
                {
                    return matrix[ily, li[0]];
                }
                int nily = ily + 1;
                double dres = 0;
                int cnt = 0;
                for (int cnt2 = 0; cnt2 < li.Count; cnt2++)
                {
                    if (matrix[ily, li[cnt2]] == 0)
                    {
                        cnt++;
                        continue;
                    }
                    List<int> lic = new List<int>(li);
                    lic.RemoveAt(cnt2);
                    dres += HLSminsub(in lic, nily) * (cnt % 2 == 0 ? 1 : -1) * matrix[ily, li[cnt2]];
                    cnt++;
                }
                return dres;
            }
            internal double HLSLU(bool d)//LU分解を使って行列式を求める、d trueはドゥーリトル法
            {
                if (trank != 2) throw new ArgumentOutOfRangeException("trank", "HLSLU : Method can only be applied to matrix.");
                if (tsize[0] != tsize[1]) throw new ArgumentOutOfRangeException("tsize", "HLSLU : Method can only be applied to square matrix.");
                Tensor tl = new Tensor();
                Tensor tu = new Tensor();
                int[] p = new int[0];
                bool c = false;
                bool e;
                if (d) e = TLUbkD(out tl, out tu, out p, out c);
                else e = TLUbkC(out tl, out tu, out p, out c);
                if (!e) return 0;
                double dres = 1;
                if (d)
                {
                    for (int cnt = 0; cnt < tsize[0]; cnt++)
                    {
                        dres *= tu.matrix[cnt, cnt];
                    }
                }
                else
                {
                    for (int cnt = 0; cnt < tsize[0]; cnt++)
                    {
                        dres *= tl.matrix[cnt, cnt];
                    }
                }
                dres *= c ? 1 : -1;
                return dres;
            }
            internal Tensor TSHk(bool hang)//正方行列化、trueは行数(列の長さ)に揃える（TT'）、falseは列数(行の長さ)に揃える（T'T）
            {
                if (trank != 2) throw new NotImplementedException("TSHk : Method for tensor other than rank 2 are not implemented.");
                double[,] res;
                ParallelOptions po = new ParallelOptions();
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                po.CancellationToken = ct;
                po.MaxDegreeOfParallelism = CommonParam.thdn;
                po.TaskScheduler = TaskScheduler.Default;
                if (hang)
                {
                    res = new double[tsize[0], tsize[0]];
                    Parallel.For(0, tsize[0], po, (ind) =>
                    {
                        double rtemp;
                        for (int ind2 = 0; ind2 < tsize[0]; ind2++)
                        {
                            rtemp = 0;
                            for (int ind3 = 0; ind3 < tsize[1]; ind3++)
                            {
                                rtemp += matrix[ind, ind3] * matrix[ind2, ind3];
                            }
                            res[ind, ind2] = rtemp;
                        }
                    });
                }
                else
                {
                    res = new double[tsize[1], tsize[1]];
                    Parallel.For(0, tsize[1], po, (ind) =>
                    {
                        double rtemp;
                        for (int ind2 = 0; ind2 < tsize[1]; ind2++)
                        {
                            rtemp = 0;
                            for (int ind3 = 0; ind3 < tsize[0]; ind3++)
                            {
                                rtemp += matrix[ind3, ind] * matrix[ind3, ind2];
                            }
                            res[ind, ind2] = rtemp;
                        }
                    });
                }
                return new Tensor(res);
            }
            static internal Tensor TSHks(Tensor t, bool hang)//正方行列化(静的メソッド)、trueは行数(列の長さ)に揃える（TT'）、falseは列数(行の長さ)に揃える（T'T）
            {
                if (t.trank != 2) throw new NotImplementedException("TSHk : Method for tensor other than rank 2 are not implemented.");
                double[,] res;
                ParallelOptions po = new ParallelOptions();
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                po.CancellationToken = ct;
                po.MaxDegreeOfParallelism = CommonParam.thdn;
                po.TaskScheduler = TaskScheduler.Default;
                if (hang)
                {
                    res = new double[t.tsize[0], t.tsize[0]];
                    Parallel.For(0, t.tsize[0], po, (ind) =>
                    {
                        double rtemp;
                        for (int ind2 = 0; ind2 < t.tsize[0]; ind2++)
                        {
                            rtemp = 0;
                            for (int ind3 = 0; ind3 < t.tsize[1]; ind3++)
                            {
                                rtemp += t.matrix[ind, ind3] * t.matrix[ind2, ind3];
                            }
                            res[ind, ind2] = rtemp;
                        }
                    });
                }
                else
                {
                    res = new double[t.tsize[1], t.tsize[1]];
                    Parallel.For(0, t.tsize[1], po, (ind) =>
                    {
                        double rtemp;
                        for (int ind2 = 0; ind2 < t.tsize[1]; ind2++)
                        {
                            rtemp = 0;
                            for (int ind3 = 0; ind3 < t.tsize[0]; ind3++)
                            {
                                rtemp += t.matrix[ind3, ind] * t.matrix[ind3, ind2];
                            }
                            res[ind, ind2] = rtemp;
                        }
                    });
                }
                return new Tensor(res);
            }
            static internal Tensor TKzs(Tensor t1, Tensor t2)//テンソルの積を求める(静的メソッド)
            {
                if (t1.trank == 2 && t2.trank == 2)
                {
                    double[,] mres = new double[t1.tsize[0], t2.tsize[1]];
                    int itemp = t1.tsize[1] <= t2.tsize[0] ? t1.tsize[1] : t2.tsize[0];
                    ParallelOptions po = new ParallelOptions();
                    CancellationTokenSource cts = new CancellationTokenSource();
                    CancellationToken ct = cts.Token;
                    po.CancellationToken = ct;
                    po.MaxDegreeOfParallelism = CommonParam.thdn;
                    po.TaskScheduler = TaskScheduler.Default;
                    if (t1.tsize[0] >= t2.tsize[1])
                    {
                        Parallel.For(0, t1.tsize[0], po, (ind) =>
                        {
                            double rtemp;
                            for (int ind2 = 0; ind2 < t2.tsize[1]; ind2++)
                            {
                                rtemp = 0;
                                for (int ind3 = 0; ind3 < itemp; ind3++)
                                {
                                    rtemp += t1.matrix[ind, ind3] * t2.matrix[ind3, ind2];
                                }
                                mres[ind, ind2] = rtemp;
                            }
                        });
                    }
                    else
                    {
                        Parallel.For(0, t2.tsize[1], po, (ind2) =>
                        {
                            double rtemp;
                            for (int ind = 0; ind < t1.tsize[0]; ind++)
                            {
                                rtemp = 0;
                                for (int ind3 = 0; ind3 < itemp; ind3++)
                                {
                                    rtemp += t1.matrix[ind, ind3] * t2.matrix[ind3, ind2];
                                }
                                mres[ind, ind2] = rtemp;
                            }
                        });
                    }
                    return new Tensor(mres);
                }
                else if (t1.trank == 1 && t2.trank == 2)
                {
                    if (t1.tnc == true)
                    {
                        double[] vres = new double[t2.tsize[1]];
                        int itemp = t1.tsize[0] >= t2.tsize[0] ? t2.tsize[0] : t1.tsize[0];
                        ParallelOptions po = new ParallelOptions();
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        po.CancellationToken = ct;
                        po.MaxDegreeOfParallelism = CommonParam.thdn;
                        po.TaskScheduler = TaskScheduler.Default;
                        Parallel.For(0, t2.tsize[1], po, (cnt) =>
                        {
                            double dtemp = 0;
                            for (int cnt2 = 0; cnt2 < itemp; cnt2++)
                            {
                                dtemp += t1.vector[cnt2] * t2.matrix[cnt2, cnt];
                            }
                            vres[cnt] = dtemp;
                        });
                        return new Tensor(vres, true);
                    }
                    else if (t1.tnc == false)
                    {
                        double[,] mres = new double[t1.tsize[0], t2.tsize[1]];
                        ParallelOptions po = new ParallelOptions();
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        po.CancellationToken = ct;
                        po.MaxDegreeOfParallelism = CommonParam.thdn;
                        po.TaskScheduler = TaskScheduler.Default;
                        if (t1.tsize[0] >= t2.tsize[1])
                        {
                            Parallel.For(0, t1.tsize[0], po, (cnt) =>
                            {
                                for (int cnt2 = 0; cnt2 < t2.tsize[1]; cnt2++)
                                {
                                    mres[cnt, cnt2] = t1.vector[cnt] * t2.matrix[0, cnt2];
                                }
                            });
                        }
                        else
                        {
                            Parallel.For(0, t2.tsize[1], po, (cnt) =>
                            {
                                for (int cnt2 = 0; cnt2 < t1.tsize[0]; cnt2++)
                                {
                                    mres[cnt2, cnt] = t1.vector[cnt2] * t2.matrix[0, cnt];
                                }
                            });
                        }
                        return new Tensor(mres);
                    }
                    else
                    {
                        double[,] mres = new double[t2.tsize[0], t2.tsize[1]];
                        ParallelOptions po = new ParallelOptions();
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        po.CancellationToken = ct;
                        po.MaxDegreeOfParallelism = CommonParam.thdn;
                        po.TaskScheduler = TaskScheduler.Default;
                        if (t2.tsize[0] >= t2.tsize[1])
                        {
                            Parallel.For(0, t2.tsize[0], po, (cnt) =>
                            {
                                for (int cnt2 = 0; cnt2 < t2.tsize[1]; cnt2++)
                                {
                                    mres[cnt, cnt2] = t1.vector[0] * t2.matrix[cnt, cnt2];
                                }
                            });
                        }
                        else
                        {
                            Parallel.For(0, t2.tsize[1], po, (cnt) =>
                            {
                                for (int cnt2 = 0; cnt2 < t2.tsize[0]; cnt2++)
                                {
                                    mres[cnt2, cnt] = t1.vector[0] * t2.matrix[cnt2, cnt];
                                }
                            });
                        }
                        return new Tensor(mres);
                    }
                }
                else if (t1.trank == 2 && t2.trank == 1)
                {
                    if (t2.tnc == true)
                    {
                        double[,] mres = new double[t1.tsize[0], t2.tsize[0]];
                        ParallelOptions po = new ParallelOptions();
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        po.CancellationToken = ct;
                        po.MaxDegreeOfParallelism = CommonParam.thdn;
                        po.TaskScheduler = TaskScheduler.Default;
                        if (t1.tsize[0] >= t2.tsize[0])
                        {
                            Parallel.For(0, t1.tsize[0], po, (cnt) =>
                            {
                                for (int cnt2 = 0; cnt2 < t2.tsize[0]; cnt2++)
                                {
                                    mres[cnt, cnt2] = t1.matrix[0, cnt] * t2.vector[cnt2];
                                }
                            });
                        }
                        else
                        {
                            Parallel.For(0, t2.tsize[0], po, (cnt) =>
                            {
                                for (int cnt2 = 0; cnt2 < t1.tsize[0]; cnt2++)
                                {
                                    mres[cnt2, cnt] = t1.matrix[0, cnt2] * t2.vector[cnt];
                                }
                            });
                        }
                        return new Tensor(mres);
                    }
                    else if (t2.tnc == false)
                    {
                        double[] vres = new double[t1.tsize[0]];
                        ParallelOptions po = new ParallelOptions();
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        po.CancellationToken = ct;
                        po.MaxDegreeOfParallelism = CommonParam.thdn;
                        po.TaskScheduler = TaskScheduler.Default;
                        int itemp = t1.tsize[1] <= t2.tsize[0] ? t1.tsize[1] : t2.tsize[0];
                        Parallel.For(0, t1.tsize[0], po, (cnt) =>
                        {
                            double dtemp = 0;
                            for (int cnt2 = 0; cnt2 < itemp; cnt2++)
                            {
                                dtemp += t1.matrix[cnt, cnt2] * t2.vector[cnt2];
                            }
                            vres[cnt] = dtemp;
                        });
                        return new Tensor(vres, false);
                    }
                    else
                    {
                        double[,] mres = new double[t1.tsize[0], t1.tsize[1]];
                        ParallelOptions po = new ParallelOptions();
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        po.CancellationToken = ct;
                        po.MaxDegreeOfParallelism = CommonParam.thdn;
                        po.TaskScheduler = TaskScheduler.Default;
                        if (t1.tsize[0] >= t1.tsize[1])
                        {
                            Parallel.For(0, t1.tsize[0], po, (cnt) =>
                            {
                                for (int cnt2 = 0; cnt2 < t1.tsize[1]; cnt2++)
                                {
                                    mres[cnt, cnt2] = t2.vector[0] * t1.matrix[cnt, cnt2];
                                }
                            });
                        }
                        else
                        {
                            Parallel.For(0, t1.tsize[1], po, (cnt) =>
                            {
                                for (int cnt2 = 0; cnt2 < t1.tsize[0]; cnt2++)
                                {
                                    mres[cnt2, cnt] = t2.vector[0] * t1.matrix[cnt2, cnt];
                                }
                            });
                        }
                        return new Tensor(mres);
                    }
                }
                else if (t1.trank == 1 && t2.trank == 1)
                {
                    if (t1.tnc == true && t2.tnc == false)
                    {
                        double dres = 0;
                        ParallelOptions po = new ParallelOptions();
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        po.CancellationToken = ct;
                        po.MaxDegreeOfParallelism = CommonParam.thdn;
                        po.TaskScheduler = TaskScheduler.Default;
                        int itemp = t1.tsize[0] <= t2.tsize[0] ? t1.tsize[0] : t2.tsize[0];
                        object lo = new object();
                        Parallel.For(0, itemp, po, (cnt) =>
                        {
                            lock (lo) dres += t1.vector[cnt] * t2.vector[cnt];
                        });
                        return new Tensor(new double[1] { dres }, null);
                    }
                    else if (t1.tnc == true && t2.tnc == true)
                    {
                        double[] vres = new double[t2.tsize[0]];
                        ParallelOptions po = new ParallelOptions();
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        po.CancellationToken = ct;
                        po.MaxDegreeOfParallelism = CommonParam.thdn;
                        po.TaskScheduler = TaskScheduler.Default;
                        Parallel.For(0, t2.tsize[0], po, (cnt) =>
                        {
                            vres[cnt] = t1.vector[0] * t2.vector[cnt];
                        });
                        return new Tensor(vres, true);
                    }
                    else if (t1.tnc == false && t2.tnc == false)
                    {
                        double[] vres = new double[t1.tsize[0]];
                        ParallelOptions po = new ParallelOptions();
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        po.CancellationToken = ct;
                        po.MaxDegreeOfParallelism = CommonParam.thdn;
                        po.TaskScheduler = TaskScheduler.Default;
                        Parallel.For(0, t1.tsize[0], po, (cnt) =>
                        {
                            vres[cnt] = t1.vector[cnt] * t2.vector[0];
                        });
                        return new Tensor(vres, false);
                    }
                    else if (t1.tnc == false && t2.tnc == true)
                    {
                        double[,] mres = new double[t1.tsize[0], t2.tsize[0]];
                        ParallelOptions po = new ParallelOptions();
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        po.CancellationToken = ct;
                        po.MaxDegreeOfParallelism = CommonParam.thdn;
                        po.TaskScheduler = TaskScheduler.Default;
                        if (t1.tsize[0] >= t2.tsize[0])
                        {
                            Parallel.For(0, t1.tsize[0], po, (cnt) =>
                            {
                                for (int cnt2 = 0; cnt2 < t2.tsize[0]; cnt2++)
                                {
                                    mres[cnt, cnt2] = t1.vector[cnt] * t2.vector[cnt2];
                                }
                            });
                        }
                        else
                        {
                            Parallel.For(0, t2.tsize[0], po, (cnt) =>
                            {
                                for (int cnt2 = 0; cnt2 < t1.tsize[0]; cnt2++)
                                {
                                    mres[cnt2, cnt] = t1.vector[cnt2] * t2.vector[cnt];
                                }
                            });
                        }
                        return new Tensor(mres);
                    }
                    else if (t1.tnc == null)
                    {
                        return TBKzs(t1.vector[0], t2);
                    }
                    else return TBKzs(t2.vector[0], t1);
                }
                else throw new NotImplementedException("TKzs : Multiplications for tensor other than rank 1 or 2 are not implemented.");
            }
            static internal Tensor TKzsDX(Tensor t1, Tensor t2)//テンソルの積を求める(静的メソッド)(シングルスレッド)
            {
                if (t1.tsize[1] != t2.tsize[0]) throw new ArgumentOutOfRangeException("t1/t2", "TKzsDX : Incorrect input tensor size.");
                if (t1.trank == 2 && t2.trank == 2)
                {
                    double[,] mres = new double[t1.tsize[0], t2.tsize[1]];
                    for (int ind = 0; ind < t1.tsize[0]; ind++)
                    {
                        double rtemp;
                        for (int ind2 = 0; ind2 < t2.tsize[1]; ind2++)
                        {
                            rtemp = 0;
                            for (int ind3 = 0; ind3 < t2.tsize[0]; ind3++)
                            {
                                rtemp += t1.matrix[ind, ind3] * t2.matrix[ind3, ind2];
                            }
                            mres[ind, ind2] = rtemp;
                        }
                    }
                    return new Tensor(mres);
                }
                else if (t1.trank == 2 && t2.trank == 1)
                {
                    if (t2.tnc == false)
                    {
                        double[] vres = new double[t1.tsize[0]];
                        for (int cnt = 0; cnt < t1.tsize[0]; cnt++)
                        {
                            double dtemp = 0;
                            for (int cnt2 = 0; cnt2 < t2.tsize[0]; cnt2++)
                            {
                                dtemp += t1.matrix[cnt, cnt2] * t2.vector[cnt2];
                            }
                            vres[cnt] = dtemp;
                        }
                        return new Tensor(vres, false);
                    }
                }
                throw new NotImplementedException("TKzsDX : Method not implemented.");
            }
            static internal Tensor TBKzs(double d, Tensor t)//テンソルのスカラー積を求める(静的メソッド)
            {
                if (t.trank == 2)
                {
                    double[,] mres = new double[t.tsize[0], t.tsize[1]];
                    ParallelOptions po = new ParallelOptions();
                    CancellationTokenSource cts = new CancellationTokenSource();
                    CancellationToken ct = cts.Token;
                    po.CancellationToken = ct;
                    po.MaxDegreeOfParallelism = CommonParam.thdn;
                    po.TaskScheduler = TaskScheduler.Default;
                    if (t.tsize[0] >= t.tsize[1])
                    {
                        Parallel.For(0, t.tsize[0], po, (cnt) =>
                        {
                            for (int cnt2 = 0; cnt2 < t.tsize[1]; cnt2++)
                            {
                                mres[cnt, cnt2] = t.matrix[cnt, cnt2] * d;
                            }
                        });
                    }
                    else
                    {
                        Parallel.For(0, t.tsize[1], po, (cnt) =>
                        {
                            for (int cnt2 = 0; cnt2 < t.tsize[0]; cnt2++)
                            {
                                mres[cnt2, cnt] = t.matrix[cnt2, cnt] * d;
                            }
                        });
                    }
                    return new Tensor(mres);
                }
                else if (t.trank == 1)
                {
                    if (t.tnc == null)
                    {
                        return new Tensor(new double[1] { t.vector[0] * d }, null);
                    }
                    else
                    {
                        double[] vres = new double[t.tsize[0]];
                        ParallelOptions po = new ParallelOptions();
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        po.CancellationToken = ct;
                        po.MaxDegreeOfParallelism = CommonParam.thdn;
                        po.TaskScheduler = TaskScheduler.Default;
                        Parallel.For(0, t.tsize[0], po, (cnt) =>
                        {
                            vres[cnt] = t.vector[cnt] * d;
                        });
                        return new Tensor(vres, t.tnc);
                    }
                }
                else throw new NotImplementedException("TBKzs : Multiplications for tensor other than rank 1 or 2 are not implemented.");
            }
            static internal Tensor TBKzsDX(double d, Tensor t)//テンソルのスカラー積を求める(静的メソッド)(シングルスレッド)
            {
                if (t.trank == 1)
                {
                    if (t.tnc == false)
                    {
                        double[] vres = new double[t.tsize[0]];
                        for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                        {
                            vres[cnt] = t.vector[cnt] * d;
                        }
                        return new Tensor(vres, t.tnc);
                    }
                }
                throw new NotImplementedException("TBKzsDX : Method not implemented.");
            }
            static internal Tensor TTzs(Tensor t1, Tensor t2)//テンソルの和を求める(静的メソッド)
            {
                if (t1.trank == 2 && t2.trank == 2)
                {
                    double[,] dres;
                    ParallelOptions po = new ParallelOptions();
                    CancellationTokenSource cts = new CancellationTokenSource();
                    CancellationToken ct = cts.Token;
                    po.CancellationToken = ct;
                    po.MaxDegreeOfParallelism = CommonParam.thdn;
                    po.TaskScheduler = TaskScheduler.Default;
                    if ((t1.tsize[0] == t2.tsize[0]) && (t1.tsize[1] == t2.tsize[1]))
                    {
                        dres = new double[t1.tsize[0], t1.tsize[1]];
                        if (t1.tsize[0] >= t1.tsize[1])
                        {
                            Parallel.For(0, t1.tsize[0], po, (cnt) =>
                            {
                                for (int cnt2 = 0; cnt2 < t1.tsize[1]; cnt2++)
                                {
                                    dres[cnt, cnt2] = t1.matrix[cnt, cnt2] + t2.matrix[cnt, cnt2];
                                }
                            });
                        }
                        else
                        {
                            Parallel.For(0, t1.tsize[1], po, (cnt) =>
                            {
                                for (int cnt2 = 0; cnt2 < t1.tsize[0]; cnt2++)
                                {
                                    dres[cnt2, cnt] = t1.matrix[cnt2, cnt] + t2.matrix[cnt2, cnt];
                                }
                            });
                        }
                    }
                    else
                    {
                        int itemp1 = t1.tsize[0] >= t2.tsize[0] ? t1.tsize[0] : t2.tsize[0];
                        int itemp2 = t1.tsize[1] >= t2.tsize[1] ? t1.tsize[1] : t2.tsize[1];
                        dres = new double[itemp1, itemp2];
                        if (itemp1 >= itemp2)
                        {
                            Parallel.For(0, itemp1, po, (cnt) =>
                            {
                                for (int cnt2 = 0; cnt2 < itemp2; cnt2++)
                                {
                                    dres[cnt, cnt2] = (((cnt >= t1.tsize[0]) || (cnt2 >= t1.tsize[1])) ? 0 : t1.matrix[cnt, cnt2]) + (((cnt >= t2.tsize[0]) || (cnt2 >= t2.tsize[1])) ? 0 : t2.matrix[cnt, cnt2]);
                                }
                            });
                        }
                        else
                        {
                            Parallel.For(0, itemp2, po, (cnt2) =>
                            {
                                for (int cnt = 0; cnt < itemp1; cnt++)
                                {
                                    dres[cnt, cnt2] = (((cnt >= t1.tsize[0]) || (cnt2 >= t1.tsize[1])) ? 0 : t1.matrix[cnt, cnt2]) + (((cnt >= t2.tsize[0]) || (cnt2 >= t2.tsize[1])) ? 0 : t2.matrix[cnt, cnt2]);
                                }
                            });
                        }
                    }
                    return new Tensor(dres);
                }
                else if (t1.trank == 1 && t2.trank == 1)
                {
                    if (t1.tnc == t2.tnc)
                    {
                        double[] dres;
                        ParallelOptions po = new ParallelOptions();
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        po.CancellationToken = ct;
                        po.MaxDegreeOfParallelism = CommonParam.thdn;
                        po.TaskScheduler = TaskScheduler.Default;
                        if (t1.tsize[0] == t2.tsize[0])
                        {
                            dres = new double[t1.tsize[0]];
                            Parallel.For(0, t1.tsize[0], po, (cnt) =>
                            {
                                dres[cnt] = t1.vector[cnt] + t2.vector[cnt];
                            });
                        }
                        else
                        {
                            int itemp = t1.tsize[0] >= t2.tsize[0] ? t1.tsize[0] : t2.tsize[0];
                            dres = new double[itemp];
                            Parallel.For(0, itemp, po, (cnt) =>
                            {
                                dres[cnt] = (cnt >= t1.tsize[0] ? 0 : t1.vector[cnt]) + (cnt >= t2.tsize[0] ? 0 : t2.vector[cnt]);
                            });
                        }
                        return new Tensor(dres, t1.tnc);
                    }
                    else throw new NotImplementedException("TTzs : Method not implemented.");
                }
                else throw new NotImplementedException("TTzs : Method not implemented.");
            }
            static internal Tensor TTzsDX(Tensor t1, Tensor t2)//テンソルの和を求める(静的メソッド)(シングルスレッド)
            {
                if (t1.trank == 1 && t2.trank == 1)
                {
                    if (t1.tnc == false && t2.tnc == false)
                    {
                        if (t1.tsize[0] == t2.tsize[0])


                        {
                            double[] dres;
                            dres = new double[t1.tsize[0]];
                            for (int cnt = 0; cnt < t1.tsize[0]; cnt++)
                            {
                                dres[cnt] = t1.vector[cnt] + t2.vector[cnt];
                            }
                            return new Tensor(dres, t1.tnc);
                        }

                    }
                }
                throw new NotImplementedException("TTzsDX : Method not implemented.");
            }
            static internal Tensor THzs(Tensor t1, Tensor t2)//テンソルの差を求める(静的メソッド)
            {
                if (t1.trank == 2 && t2.trank == 2)
                {
                    double[,] dres;
                    ParallelOptions po = new ParallelOptions();
                    CancellationTokenSource cts = new CancellationTokenSource();
                    CancellationToken ct = cts.Token;
                    po.CancellationToken = ct;
                    po.MaxDegreeOfParallelism = CommonParam.thdn;
                    po.TaskScheduler = TaskScheduler.Default;
                    if ((t1.tsize[0] == t2.tsize[0]) && (t1.tsize[1] == t2.tsize[1]))
                    {
                        dres = new double[t1.tsize[0], t1.tsize[1]];
                        if (t1.tsize[0] >= t1.tsize[1])
                        {
                            Parallel.For(0, t1.tsize[0], po, (cnt) =>
                            {
                                for (int cnt2 = 0; cnt2 < t1.tsize[1]; cnt2++)
                                {
                                    dres[cnt, cnt2] = t1.matrix[cnt, cnt2] - t2.matrix[cnt, cnt2];
                                }
                            });
                        }
                        else
                        {
                            Parallel.For(0, t1.tsize[1], po, (cnt) =>
                            {
                                for (int cnt2 = 0; cnt2 < t1.tsize[0]; cnt2++)
                                {
                                    dres[cnt2, cnt] = t1.matrix[cnt2, cnt] - t2.matrix[cnt2, cnt];
                                }
                            });
                        }
                    }
                    else
                    {
                        int itemp1 = t1.tsize[0] >= t2.tsize[0] ? t1.tsize[0] : t2.tsize[0];
                        int itemp2 = t1.tsize[1] >= t2.tsize[1] ? t1.tsize[1] : t2.tsize[1];
                        dres = new double[itemp1, itemp2];
                        if (itemp1 >= itemp2)
                        {
                            Parallel.For(0, itemp1, po, (cnt) =>
                            {
                                for (int cnt2 = 0; cnt2 < itemp2; cnt2++)
                                {
                                    dres[cnt, cnt2] = (((cnt >= t1.tsize[0]) || (cnt2 >= t1.tsize[1])) ? 0 : t1.matrix[cnt, cnt2]) - (((cnt >= t2.tsize[0]) || (cnt2 >= t2.tsize[1])) ? 0 : t2.matrix[cnt, cnt2]);
                                }
                            });
                        }
                        else
                        {
                            Parallel.For(0, itemp2, po, (cnt2) =>
                            {
                                for (int cnt = 0; cnt < itemp1; cnt++)
                                {
                                    dres[cnt, cnt2] = (((cnt >= t1.tsize[0]) || (cnt2 >= t1.tsize[1])) ? 0 : t1.matrix[cnt, cnt2]) - (((cnt >= t2.tsize[0]) || (cnt2 >= t2.tsize[1])) ? 0 : t2.matrix[cnt, cnt2]);
                                }
                            });
                        }
                    }
                    return new Tensor(dres);
                }
                else if (t1.trank == 1 && t2.trank == 1)
                {
                    if (t1.tnc == t2.tnc)
                    {
                        double[] dres;
                        ParallelOptions po = new ParallelOptions();
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        po.CancellationToken = ct;
                        po.MaxDegreeOfParallelism = CommonParam.thdn;
                        po.TaskScheduler = TaskScheduler.Default;
                        if (t1.tsize[0] == t2.tsize[0])
                        {
                            dres = new double[t1.tsize[0]];
                            Parallel.For(0, t1.tsize[0], po, (cnt) =>
                            {
                                dres[cnt] = t1.vector[cnt] - t2.vector[cnt];
                            });
                        }
                        else
                        {
                            int itemp = t1.tsize[0] >= t2.tsize[0] ? t1.tsize[0] : t2.tsize[0];
                            dres = new double[itemp];
                            Parallel.For(0, itemp, po, (cnt) =>
                            {
                                dres[cnt] = (cnt >= t1.tsize[0] ? 0 : t1.vector[cnt]) - (cnt >= t2.tsize[0] ? 0 : t2.vector[cnt]);
                            });
                        }
                        return new Tensor(dres, t1.tnc);
                    }
                    else throw new NotImplementedException("THzs : Method not implemented.");
                }
                else throw new NotImplementedException("THzs : Method not implemented.");
            }
            internal Tensor TJN()//テンソルの反テンソルを求める
            {
                if (trank == 1)
                {
                    double[] vres = new double[tsize[0]];
                    ParallelOptions po = new ParallelOptions();
                    CancellationTokenSource cts = new CancellationTokenSource();
                    CancellationToken ct = cts.Token;
                    po.CancellationToken = ct;
                    po.MaxDegreeOfParallelism = CommonParam.thdn;
                    po.TaskScheduler = TaskScheduler.Default;
                    Parallel.For(0, tsize[0], po, (cnt) =>
                    {
                        vres[cnt] = -vector[cnt];
                    });
                    return new Tensor(vres, tnc);
                }
                else if (trank == 2)
                {
                    double[,] mres = new double[tsize[0], tsize[1]];
                    ParallelOptions po = new ParallelOptions();
                    CancellationTokenSource cts = new CancellationTokenSource();
                    CancellationToken ct = cts.Token;
                    po.CancellationToken = ct;
                    po.MaxDegreeOfParallelism = CommonParam.thdn;
                    po.TaskScheduler = TaskScheduler.Default;
                    if (tsize[0] >= tsize[1])
                    {
                        Parallel.For(0, tsize[0], po, (cnt) =>
                        {
                            for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                            {
                                mres[cnt, cnt2] = -matrix[cnt, cnt2];
                            }
                        });
                    }
                    else
                    {
                        Parallel.For(0, tsize[1], po, (cnt2) =>
                        {
                            for (int cnt = 0; cnt < tsize[0]; cnt++)
                            {
                                mres[cnt, cnt2] = -matrix[cnt, cnt2];
                            }
                        });
                    }
                    return new Tensor(mres);
                }
                else throw new NotImplementedException("TJN : Method not implemented.");
            }
            static internal Tensor TJNs(Tensor t)//テンソルの反テンソルを求める(静的メソッド)
            {
                if (t.trank == 1)
                {
                    double[] vres = new double[t.tsize[0]];
                    ParallelOptions po = new ParallelOptions();
                    CancellationTokenSource cts = new CancellationTokenSource();
                    CancellationToken ct = cts.Token;
                    po.CancellationToken = ct;
                    po.MaxDegreeOfParallelism = CommonParam.thdn;
                    po.TaskScheduler = TaskScheduler.Default;
                    Parallel.For(0, t.tsize[0], po, (cnt) =>
                    {
                        vres[cnt] = -t.vector[cnt];
                    });
                    return new Tensor(vres, t.tnc);
                }
                else if (t.trank == 2)
                {
                    double[,] mres = new double[t.tsize[0], t.tsize[1]];
                    ParallelOptions po = new ParallelOptions();
                    CancellationTokenSource cts = new CancellationTokenSource();
                    CancellationToken ct = cts.Token;
                    po.CancellationToken = ct;
                    po.MaxDegreeOfParallelism = CommonParam.thdn;
                    po.TaskScheduler = TaskScheduler.Default;
                    if (t.tsize[0] >= t.tsize[1])
                    {
                        Parallel.For(0, t.tsize[0], po, (cnt) =>
                        {
                            for (int cnt2 = 0; cnt2 < t.tsize[1]; cnt2++)
                            {
                                mres[cnt, cnt2] = -t.matrix[cnt, cnt2];
                            }
                        });
                    }
                    else
                    {
                        Parallel.For(0, t.tsize[1], po, (cnt2) =>
                        {
                            for (int cnt = 0; cnt < t.tsize[0]; cnt++)
                            {
                                mres[cnt, cnt2] = -t.matrix[cnt, cnt2];
                            }
                        });
                    }
                    return new Tensor(mres);
                }
                else throw new NotImplementedException("TJN : Method not implemented.");
            }
            internal bool TLUbkD(out Tensor tl, out Tensor tu, out int[] P, out bool TOFJ)//LU分解(ドゥーリトル法)
            {
                if (trank != 2) throw new ArgumentOutOfRangeException("trank", "TLUbkD : Method can only be applied to matrix.");
                if (tsize[0] != tsize[1]) throw new ArgumentOutOfRangeException("tsize", "TLUbkD : Method can only be applied to square matrix.");
                Tensor tlt = new Tensor(new double[tsize[0], tsize[0]]);
                Tensor tut = new Tensor(new double[tsize[0], tsize[0]]);
                ParallelOptions po = new ParallelOptions();
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                po.CancellationToken = ct;
                po.MaxDegreeOfParallelism = CommonParam.thdn;
                po.TaskScheduler = TaskScheduler.Default;
                int[] Prm = new int[tsize[0]];
                Parallel.For(0, tsize[0], po, (ind) =>
                {
                    Prm[ind] = ind;
                    tlt.matrix[ind, ind] = 1;
                    tut.matrix[ind, ind] = 0;
                    for (int cnt = 0; cnt < ind; cnt++)
                    {
                        tlt.matrix[cnt, ind] = 0;
                        tut.matrix[ind, cnt] = 0;
                    }
                });
                bool C = true;//TOFJ
                int itemp;
                int iptemp;
                bool tlv = true;
                for (int cnt = 0; cnt < tsize[0]; cnt++)
                {
                    itemp = cnt + 1;
                    while (tut.matrix[cnt, cnt] == 0)
                    {
                        tut.matrix[cnt, cnt] = matrix[Prm[cnt], cnt];
                        Parallel.For(0, cnt, po, (cnt2) =>
                        {
                            double dtemp = tlt.matrix[cnt, cnt2] * tut.matrix[cnt2, cnt];
                            lock (tut.matrix.SyncRoot) tut.matrix[cnt, cnt] -= dtemp;
                        });
                        if (tut.matrix[cnt, cnt] == 0)
                        {
                            if (itemp >= tsize[0])
                            {
                                tl = null;
                                tu = null;
                                P = null;
                                TOFJ = false;
                                return false;
                            }
                            iptemp = Prm[itemp];
                            Prm[itemp] = Prm[cnt];
                            Prm[cnt] = iptemp;
                            Parallel.For(0, cnt, po, (cnt2) =>
                            {
                                double dptemp = tlt.matrix[cnt, cnt2];
                                tlt.matrix[cnt, cnt2] = tlt.matrix[itemp, cnt2];
                                tlt.matrix[itemp, cnt2] = dptemp;
                            });
                            C = !C;
                            itemp++;
                        }
                    }
                    Parallel.For(cnt + 1, tsize[0], po, (cnt2) =>
                    {
                        tut.matrix[cnt, cnt2] = matrix[Prm[cnt], cnt2];
                        for (int cnt3 = 0; cnt3 < cnt; cnt3++)
                        {
                            tut.matrix[cnt, cnt2] -= tlt.matrix[cnt, cnt3] * tut.matrix[cnt3, cnt2];
                        }
                    });
                    Parallel.For(cnt + 1, tsize[0], po, (cnt2) =>
                    {
                        if (!tlv) return;
                        tlt.matrix[cnt2, cnt] = matrix[Prm[cnt2], cnt];
                        for (int cnt3 = 0; cnt3 < cnt; cnt3++)
                        {
                            tlt.matrix[cnt2, cnt] -= tlt.matrix[cnt2, cnt3] * tut.matrix[cnt3, cnt];
                        }
                        tlt.matrix[cnt2, cnt] /= tut.matrix[cnt, cnt];
                        if (Double.IsInfinity(tlt.matrix[cnt2, cnt]) || tlt.matrix[cnt2, cnt] == Double.NaN)
                        {
                            tlv = false;
                        }
                    });
                    if (!tlv)
                    {
                        tl = null;
                        tu = null;
                        P = null;
                        TOFJ = false;
                        return false;
                    }
                }
                tl = tlt;
                tu = tut;
                P = Prm;
                TOFJ = C;
                return true;
            }
            internal bool TLUbkDDX(out Tensor tl, out Tensor tu, out int[] P, out bool TOFJ)//LU分解(ドゥーリトル法)(シングルスレッド)
            {
                if (trank != 2) throw new ArgumentOutOfRangeException("trank", "TLUbkD : Method can only be applied to matrix.");
                if (tsize[0] != tsize[1]) throw new ArgumentOutOfRangeException("tsize", "TLUbkD : Method can only be applied to square matrix.");
                Tensor tlt = new Tensor(new double[tsize[0], tsize[0]]);
                Tensor tut = new Tensor(new double[tsize[0], tsize[0]]);
                int[] Prm = new int[tsize[0]];
                int ind, cnt;
                for (ind = 0; ind < tsize[0]; ind++)
                {
                    Prm[ind] = ind;
                    tlt.matrix[ind, ind] = 1;
                    tut.matrix[ind, ind] = 0;
                    for (cnt = 0; cnt < ind; cnt++)
                    {
                        tlt.matrix[cnt, ind] = 0;
                        tut.matrix[ind, cnt] = 0;
                    }
                }
                bool C = true;//TOFJ
                int itemp;
                int iptemp;
                bool tlv = true;
                int cnt2;
                for (cnt = 0; cnt < tsize[0]; cnt++)
                {
                    itemp = cnt + 1;
                    while (tut.matrix[cnt, cnt] == 0)
                    {
                        tut.matrix[cnt, cnt] = matrix[Prm[cnt], cnt];
                        for (cnt2 = 0; cnt2 < cnt; cnt2++)
                        {
                            double dtemp = tlt.matrix[cnt, cnt2] * tut.matrix[cnt2, cnt];
                            lock (tut.matrix.SyncRoot) tut.matrix[cnt, cnt] -= dtemp;
                        }
                        if (tut.matrix[cnt, cnt] == 0)
                        {
                            if (itemp >= tsize[0])
                            {
                                tl = null;
                                tu = null;
                                P = null;
                                TOFJ = false;
                                return false;
                            }
                            iptemp = Prm[itemp];
                            Prm[itemp] = Prm[cnt];
                            Prm[cnt] = iptemp;
                            for (cnt2 = 0; cnt2 < cnt; cnt2++)
                            {
                                double dptemp = tlt.matrix[cnt, cnt2];
                                tlt.matrix[cnt, cnt2] = tlt.matrix[itemp, cnt2];
                                tlt.matrix[itemp, cnt2] = dptemp;
                            }
                            C = !C;
                            itemp++;
                        }
                    }
                    for (cnt2 = cnt + 1; cnt2 < tsize[0]; cnt2++)
                    {
                        tut.matrix[cnt, cnt2] = matrix[Prm[cnt], cnt2];
                        for (int cnt3 = 0; cnt3 < cnt; cnt3++)
                        {
                            tut.matrix[cnt, cnt2] -= tlt.matrix[cnt, cnt3] * tut.matrix[cnt3, cnt2];
                        }
                    }
                    for (cnt2 = cnt + 1; cnt2 < tsize[0]; cnt2++)
                    {
                        if (!tlv) break;
                        tlt.matrix[cnt2, cnt] = matrix[Prm[cnt2], cnt];
                        for (int cnt3 = 0; cnt3 < cnt; cnt3++)
                        {
                            tlt.matrix[cnt2, cnt] -= tlt.matrix[cnt2, cnt3] * tut.matrix[cnt3, cnt];
                        }
                        tlt.matrix[cnt2, cnt] /= tut.matrix[cnt, cnt];
                        if (Double.IsInfinity(tlt.matrix[cnt2, cnt]) || tlt.matrix[cnt2, cnt] == Double.NaN)
                        {
                            tlv = false;
                        }
                    }
                    if (!tlv)
                    {
                        tl = null;
                        tu = null;
                        P = null;
                        TOFJ = false;
                        return false;
                    }
                }
                tl = tlt;
                tu = tut;
                P = Prm;
                TOFJ = C;
                return true;
            }
            internal bool TLUbkC(out Tensor tl, out Tensor tu, out int[] P, out bool TOFJ)//LU分解(クラウト法)
            {
                if (trank != 2) throw new ArgumentOutOfRangeException("trank", "TLUbkC : Method can only be applied to matrix.");
                if (tsize[0] != tsize[1]) throw new ArgumentOutOfRangeException("tsize", "TLUbkC : Method can only be applied to square matrix.");
                Tensor tlt = new Tensor(new double[tsize[0], tsize[0]]);
                Tensor tut = new Tensor(new double[tsize[0], tsize[0]]);
                ParallelOptions po = new ParallelOptions();
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                po.CancellationToken = ct;
                po.MaxDegreeOfParallelism = CommonParam.thdn;
                po.TaskScheduler = TaskScheduler.Default;
                int[] Prm = new int[tsize[0]];
                Parallel.For(0, tsize[0], po, (ind) =>
                {
                    Prm[ind] = ind;
                    tut.matrix[ind, ind] = 1;
                    tlt.matrix[ind, ind] = 0;
                    for (int cnt = 0; cnt < ind; cnt++)
                    {
                        tlt.matrix[cnt, ind] = 0;
                        tut.matrix[ind, cnt] = 0;
                    }
                });
                bool C = true;//TOFJ
                int itemp;
                int iptemp;
                bool tlv = true;
                for (int cnt = 0; cnt < tsize[0]; cnt++)
                {
                    itemp = cnt + 1;
                    while (tlt.matrix[cnt, cnt] == 0)
                    {
                        tlt.matrix[cnt, cnt] = matrix[Prm[cnt], cnt];
                        Parallel.For(0, cnt, po, (cnt2) =>
                        {
                            double dtemp = tlt.matrix[cnt, cnt2] * tut.matrix[cnt2, cnt];
                            lock (tlt.matrix.SyncRoot) tlt.matrix[cnt, cnt] -= dtemp;
                        });
                        if (tlt.matrix[cnt, cnt] == 0)
                        {
                            if (itemp >= tsize[0])
                            {
                                tl = null;
                                tu = null;
                                P = null;
                                TOFJ = false;
                                return false;
                            }
                            iptemp = Prm[itemp];
                            Prm[itemp] = Prm[cnt];
                            Prm[cnt] = iptemp;
                            Parallel.For(0, cnt, po, (cnt2) =>
                            {
                                double dptemp = tlt.matrix[cnt, cnt2];
                                tlt.matrix[cnt, cnt2] = tlt.matrix[itemp, cnt2];
                                tlt.matrix[itemp, cnt2] = dptemp;
                            });
                            C = !C;
                            itemp++;
                        }
                    }
                    Parallel.For(cnt + 1, tsize[0], po, (cnt2) =>
                    {
                        tlt.matrix[cnt2, cnt] = matrix[Prm[cnt2], cnt];
                        for (int cnt3 = 0; cnt3 < cnt; cnt3++)
                        {
                            tlt.matrix[cnt2, cnt] -= tlt.matrix[cnt2, cnt3] * tut.matrix[cnt3, cnt];
                        }
                    });
                    Parallel.For(cnt + 1, tsize[0], po, (cnt2) =>
                    {
                        if (!tlv) return;
                        tut.matrix[cnt, cnt2] = matrix[Prm[cnt], cnt2];
                        for (int cnt3 = 0; cnt3 < cnt; cnt3++)
                        {
                            tut.matrix[cnt, cnt2] -= tlt.matrix[cnt, cnt3] * tut.matrix[cnt3, cnt2];
                        }
                        tut.matrix[cnt, cnt2] /= tlt.matrix[cnt, cnt];
                        if (Double.IsInfinity(tut.matrix[cnt, cnt2]) || tut.matrix[cnt, cnt2] == Double.NaN)
                        {
                            tlv = false;
                        }
                    });
                    if (!tlv)
                    {
                        tl = null;
                        tu = null;
                        P = null;
                        TOFJ = false;
                        return false;
                    }
                }
                tl = tlt;
                tu = tut;
                P = Prm;
                TOFJ = C;
                return true;
            }
            internal bool TLUbkCDX(out Tensor tl, out Tensor tu, out int[] P, out bool TOFJ)//LU分解(クラウト法)(シングルスレッド)
            {
                if (trank != 2) throw new ArgumentOutOfRangeException("trank", "TLUbkC : Method can only be applied to matrix.");
                if (tsize[0] != tsize[1]) throw new ArgumentOutOfRangeException("tsize", "TLUbkC : Method can only be applied to square matrix.");
                Tensor tlt = new Tensor(new double[tsize[0], tsize[0]]);
                Tensor tut = new Tensor(new double[tsize[0], tsize[0]]);
                int[] Prm = new int[tsize[0]];
                int ind;
                for (ind = 0; ind < tsize[0]; ind++)
                {
                    Prm[ind] = ind;
                    tut.matrix[ind, ind] = 1;
                    tlt.matrix[ind, ind] = 0;
                    for (int cnt = 0; cnt < ind; cnt++)
                    {
                        tlt.matrix[cnt, ind] = 0;
                        tut.matrix[ind, cnt] = 0;
                    }
                }
                bool C = true;//TOFJ
                int itemp;
                int iptemp;
                bool tlv = true;
                int cnt2;
                for (int cnt = 0; cnt < tsize[0]; cnt++)
                {
                    itemp = cnt + 1;
                    while (tlt.matrix[cnt, cnt] == 0)
                    {
                        tlt.matrix[cnt, cnt] = matrix[Prm[cnt], cnt];
                        for (cnt2 = 0; cnt2 < cnt; cnt2++)
                        {
                            double dtemp = tlt.matrix[cnt, cnt2] * tut.matrix[cnt2, cnt];
                            lock (tlt.matrix.SyncRoot) tlt.matrix[cnt, cnt] -= dtemp;
                        }
                        if (tlt.matrix[cnt, cnt] == 0)
                        {
                            if (itemp >= tsize[0])
                            {
                                tl = null;
                                tu = null;
                                P = null;
                                TOFJ = false;
                                return false;
                            }
                            iptemp = Prm[itemp];
                            Prm[itemp] = Prm[cnt];
                            Prm[cnt] = iptemp;
                            for (cnt2 = 0; cnt2 < cnt; cnt2++)
                            {
                                double dptemp = tlt.matrix[cnt, cnt2];
                                tlt.matrix[cnt, cnt2] = tlt.matrix[itemp, cnt2];
                                tlt.matrix[itemp, cnt2] = dptemp;
                            }
                            C = !C;
                            itemp++;
                        }
                    }
                    for (cnt2 = cnt + 1; cnt2 < tsize[0]; cnt2++)
                    {
                        tlt.matrix[cnt2, cnt] = matrix[Prm[cnt2], cnt];
                        for (int cnt3 = 0; cnt3 < cnt; cnt3++)
                        {
                            tlt.matrix[cnt2, cnt] -= tlt.matrix[cnt2, cnt3] * tut.matrix[cnt3, cnt];
                        }
                    }
                    for (cnt2 = cnt + 1; cnt2 < tsize[0]; cnt2++)
                    {
                        if (!tlv) continue;
                        tut.matrix[cnt, cnt2] = matrix[Prm[cnt], cnt2];
                        for (int cnt3 = 0; cnt3 < cnt; cnt3++)
                        {
                            tut.matrix[cnt, cnt2] -= tlt.matrix[cnt, cnt3] * tut.matrix[cnt3, cnt2];
                        }
                        tut.matrix[cnt, cnt2] /= tlt.matrix[cnt, cnt];
                        if (Double.IsInfinity(tut.matrix[cnt, cnt2]) || tut.matrix[cnt, cnt2] == Double.NaN)
                        {
                            tlv = false;
                        }
                    }
                    if (!tlv)
                    {
                        tl = null;
                        tu = null;
                        P = null;
                        TOFJ = false;
                        return false;
                    }
                }
                tl = tlt;
                tu = tut;
                P = Prm;
                TOFJ = C;
                return true;
            }
            internal Tensor ZS1(int i, int j)//行列の(i,j)一次小行列を求める
            {
                if (trank != 2) throw new ArgumentOutOfRangeException("trank", "MTc : Method is for matrix only.");
                if (tsize[0] <= 1 || tsize[1] <= 1) return new Tensor(new double[0, 0]);
                int g = tsize[0] - 1;
                int r = tsize[1] - 1;
                double[,] dres = new double[g, r];
                int ind = 0, ind2 = 0;
                for (int cnt = 0; cnt < g; cnt++)
                {
                    for (int cnt2 = 0; cnt2 < r; cnt2++)
                    {
                        if (ind == i) ind++;
                        if (ind2 == j) ind2++;
                        dres[cnt, cnt2] = matrix[ind, ind2];
                        ind++;
                        ind2++;
                    }
                }
                return new Tensor(dres);
            }
            internal Tensor MTc()//行列の転置行列を求める
            {
                if (trank != 2 && trank != 1) throw new ArgumentOutOfRangeException("trank", "MTc : Method is for matrix only.");
                if (trank == 2)
                {
                    ParallelOptions po = new ParallelOptions();
                    CancellationTokenSource cts = new CancellationTokenSource();
                    CancellationToken ct = cts.Token;
                    po.CancellationToken = ct;
                    po.MaxDegreeOfParallelism = CommonParam.thdn;
                    po.TaskScheduler = TaskScheduler.Default;
                    double[,] dres = new double[tsize[1], tsize[0]];
                    if (tsize[0] > tsize[1])
                    {
                        Parallel.For(0, tsize[0], po, (cnt) =>
                        {
                            for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                            {
                                dres[cnt2, cnt] = matrix[cnt, cnt2];
                            }
                        });
                    }
                    else
                    {
                        Parallel.For(0, tsize[1], po, (cnt) =>
                        {
                            for (int cnt2 = 0; cnt2 < tsize[0]; cnt2++)
                            {
                                dres[cnt, cnt2] = matrix[cnt2, cnt];
                            }
                        });
                    }
                    return new Tensor(dres);
                }
                else
                {
                    tnc = !tnc;
                    return this;
                }
            }
            internal Tensor MTcDX()//行列の転置行列を求める(シングルスレッド)
            {
                if (trank != 2 && trank != 1) throw new ArgumentOutOfRangeException("trank", "MTc : Method is for matrix only.");
                if (trank == 2)
                {
                    double[,] dres = new double[tsize[1], tsize[0]];
                    for (int cnt = 0; cnt < tsize[0]; cnt++)
                    {
                        for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                        {
                            dres[cnt2, cnt] = matrix[cnt, cnt2];
                        }
                    }
                    return new Tensor(dres);
                }
                else
                {
                    tnc = !tnc;
                    return this;
                }
            }
            static internal Tensor MTcs(Tensor t)//行列の転置行列を求める(静的メソッド)
            {
                if (t.trank != 2 && t.trank != 1) throw new ArgumentOutOfRangeException("trank", "MTc : Method is for matrix only.");
                if (t.trank == 2)
                {
                    ParallelOptions po = new ParallelOptions();
                    CancellationTokenSource cts = new CancellationTokenSource();
                    CancellationToken ct = cts.Token;
                    po.CancellationToken = ct;
                    po.MaxDegreeOfParallelism = CommonParam.thdn;
                    po.TaskScheduler = TaskScheduler.Default;
                    double[,] dres = new double[t.tsize[1], t.tsize[0]];
                    if (t.tsize[0] > t.tsize[1])
                    {
                        Parallel.For(0, t.tsize[0], po, (cnt) =>
                        {
                            for (int cnt2 = 0; cnt2 < t.tsize[1]; cnt2++)
                            {
                                dres[cnt2, cnt] = t.matrix[cnt, cnt2];
                            }
                        });
                    }
                    else
                    {
                        Parallel.For(0, t.tsize[1], po, (cnt) =>
                        {
                            for (int cnt2 = 0; cnt2 < t.tsize[0]; cnt2++)
                            {
                                dres[cnt, cnt2] = t.matrix[cnt2, cnt];
                            }
                        });
                    }
                    return new Tensor(dres);
                }
                else
                {
                    Tensor tres = new Tensor(t.vector, !t.tnc);
                    return t;
                }
            }
            internal Tensor Vtc()//ベクトルの転置を求める
            {
                if (trank != 1) throw new ArgumentOutOfRangeException("Vtc : Method is for vector only. ");
                tnc = !tnc;
                return this;
            }
            static internal Tensor Vtcs(Tensor t)//ベクトルの転置を求める(静的メソッド)
            {
                if (t.trank != 1) throw new ArgumentOutOfRangeException("Vtc : Method is for vector only. ");
                t.tnc = !t.tnc;
                return t;
            }
            internal Tensor GgLU(bool d)//LU分解してから逆行列を求める。dはドゥーリトル法
            {
                if (trank != 2) throw new ArgumentOutOfRangeException("trank", "GgLU : Method is for matrix only.");
                if (tsize[0] != tsize[1]) throw new ArgumentOutOfRangeException("tsize", "GgLU : Method can only be applied to square matrix.");
                Tensor tl = new Tensor();
                Tensor tu = new Tensor();
                int[] p = new int[0];
                bool c = false;
                bool e;
                if (d)
                {
                    e = TLUbkD(out tl, out tu, out p, out c);
                    if (!e) e = TLUbkC(out tl, out tu, out p, out c);
                }
                else
                {
                    e = TLUbkC(out tl, out tu, out p, out c);
                    if (!e) e = TLUbkD(out tl, out tu, out p, out c);
                }
                if (!e) throw new ArgumentOutOfRangeException("e", "GgLU : Can not be inverted");
                double[,] tt = new double[tsize[0], tsize[1]];
                ParallelOptions po = new ParallelOptions();
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                po.CancellationToken = ct;
                po.MaxDegreeOfParallelism = CommonParam.thdn;
                po.TaskScheduler = TaskScheduler.Default;
                Parallel.For(0, tsize[0], po, (cnt) =>
                {
                    for (int cnt2 = 0; cnt2 <= p[cnt]; cnt2++)
                    {
                        if (cnt2 == p[cnt]) tt[cnt2, cnt] = 1;
                        else tt[cnt2, cnt] = 0;
                    }
                });
                for (int cnt = 1; cnt < tsize[0]; cnt++)
                {
                    Parallel.For(0, cnt, po, (cnt2) =>
                    {
                        tt[cnt, p[cnt2]] = 0;
                        for (int cnt3 = 0; cnt3 < cnt; cnt3++)
                        {
                            tt[cnt, p[cnt2]] -= tt[cnt3, p[cnt2]] * tl.matrix[cnt, cnt3];
                        }
                    });
                }
                double[,] tres = new double[tsize[0], tsize[1]];
                int ind = tsize[0] - 1;
                for (int cnt = ind; cnt >= 0; cnt--)
                {
                    Parallel.For(0, tsize[0], po, (cnt2) =>
                    {
                        tres[cnt, cnt2] = tt[cnt, cnt2];
                        for (int cnt3 = ind; cnt3 > cnt; cnt3--)
                        {
                            tres[cnt, cnt2] -= tu.matrix[cnt, cnt3] * tres[cnt3, cnt2];
                        }
                        tres[cnt, cnt2] /= tu.matrix[cnt, cnt];
                    });
                }
                return new Tensor(tres);
            }
            internal Tensor GgLUDX()//LU分解してから逆行列を求める(ドゥーリトル法)(シングルスレッド)
            {
                if (trank != 2) throw new ArgumentOutOfRangeException("trank", "GgLU : Method is for matrix only.");
                if (tsize[0] != tsize[1]) throw new ArgumentOutOfRangeException("tsize", "GgLU : Method can only be applied to square matrix.");
                Tensor tl = new Tensor();
                Tensor tu = new Tensor();
                int[] p = new int[0];
                bool c = false;
                bool e;
                e = TLUbkDDX(out tl, out tu, out p, out c);
                if (!e) e = TLUbkCDX(out tl, out tu, out p, out c);
                if (!e) throw new ArgumentOutOfRangeException("e", "GgLU : Can not be inverted");
                double[,] tt = new double[tsize[0], tsize[1]];
                int cnt, cnt2, cnt3;
                for (cnt = 0; cnt < tsize[0]; cnt++)
                {
                    for (cnt2 = 0; cnt2 <= p[cnt]; cnt2++)
                    {
                        if (cnt2 == p[cnt]) tt[cnt2, cnt] = 1;
                        else tt[cnt2, cnt] = 0;
                    }
                }
                for (cnt = 1; cnt < tsize[0]; cnt++)
                {
                    for (cnt2 = 0; cnt2 < cnt; cnt2++)
                    {
                        tt[cnt, p[cnt2]] = 0;
                        for (cnt3 = 0; cnt3 < cnt; cnt3++)
                        {
                            tt[cnt, p[cnt2]] -= tt[cnt3, p[cnt2]] * tl.matrix[cnt, cnt3];
                        }
                    }
                }
                double[,] tres = new double[tsize[0], tsize[1]];
                int ind = tsize[0] - 1;
                for (cnt = ind; cnt >= 0; cnt--)
                {
                    for (cnt2 = 0; cnt2 < tsize[0]; cnt2++)
                    {
                        tres[cnt, cnt2] = tt[cnt, cnt2];
                        for (cnt3 = ind; cnt3 > cnt; cnt3--)
                        {
                            tres[cnt, cnt2] -= tu.matrix[cnt, cnt3] * tres[cnt3, cnt2];
                        }
                        tres[cnt, cnt2] /= tu.matrix[cnt, cnt];
                    }
                }
                return new Tensor(tres);
            }
            internal Tensor TMPi()//ムーア-ペンローズの擬似逆行列を求める
            {
                if (trank != 2) throw new ArgumentOutOfRangeException("trank", "TMPi : Method is for matrix only.");
                else if (tsize[0] >= tsize[1])
                {
                    Tensor ttemp = this.MTc();
                    return TKzs(TKzs(ttemp, this).GgLU(true), ttemp);
                }
                else
                {
                    Tensor ttemp = this.MTc();
                    return TKzs(ttemp, TKzs(this, ttemp).GgLU(true));
                }
            }
            internal Tensor TMPiDX()//ムーア-ペンローズの擬似逆行列を求める(シングルスレッド)
            {
                if (trank != 2) throw new ArgumentOutOfRangeException("trank", "TMPi : Method is for matrix only.");
                else if (tsize[0] >= tsize[1])
                {
                    Tensor ttemp = this.MTcDX();
                    try
                    {
                        return TKzsDX(TKzsDX(ttemp, this).GgLUDX(), ttemp);
                    }
                    catch (Exception) { throw new Exception(); }
                }
                else
                {
                    Tensor ttemp = this.MTcDX();
                    try
                    {
                        return TKzsDX(ttemp, TKzsDX(this, ttemp).GgLUDX());
                    }
                    catch (Exception) { throw new Exception(); }
                }
            }
            internal Tensor GSOU(in Tensor t, ref Tensor tRi)//グラム・シュミットの正規直交化、返り値はU行列(非正規Q行列)、refはRの逆行列
            {
                throw new NotImplementedException();
                //if (t.trank != 2) throw new NotImplementedException("GSOU : Method for tensor other than rank 2 are not implemented.");
                //double[,] res = new double[t.tsize[0], t.tsize[1]];
                //return new Tensor(res);
            }
            static internal Tensor SjMat(int s1, int s2, double k, double c)//ランダム行列
            {
                double[,] d = new double[s1, s2];
                Random r = new Random();
                for (int cnt = 0; cnt < d.GetLength(0); cnt++)
                {
                    for (int cnt2 = 0; cnt2 < d.GetLength(1); cnt2++)
                    {
                        d[cnt, cnt2] = r.NextDouble() * k - c;
                    }
                }
                return new Tensor(d);
            }
            public override string ToString()
            {
                return ToString(null, null);
            }
            public string ToString(IFormatProvider ifp)
            {
                return ToString(null, ifp);
            }
            public string ToString(string fmt)
            {
                return ToString(fmt, null);
            }
            public string ToString(string fmt, IFormatProvider ifp)
            {
                if (String.IsNullOrEmpty(fmt))
                {
                    if (trank != 1) fmt = "L";
                    else
                    {
                        if (tnc == true) fmt = "G";
                        else if (tnc == false || tnc == null) fmt = "L";
                    }
                }
                fmt = fmt.Trim();
                if (ifp == null) ifp = CultureInfo.GetCultureInfo("ja-JP");
                byte btemp = 0;
                if (byte.TryParse(fmt, out btemp))
                {
                    if (trank != 1) fmt = "LJ";
                    else
                    {
                        if (tnc == true) fmt = "GJ";
                        else if (tnc == false || tnc == null) fmt = "LJ";
                    }
                }
                else if (fmt.StartsWith("L", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (byte.TryParse(fmt.Substring(1), out btemp))
                    {
                        fmt = "LJ";
                    }
                    else fmt = "L";
                }
                else if (fmt.StartsWith("G", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (byte.TryParse(fmt.Substring(1), out btemp))
                    {
                        fmt = "GJ";
                    }
                    else fmt = "G";
                }
                else
                {
                    if (trank != 1) fmt = "L";
                    else
                    {
                        if (tnc == true) fmt = "G";
                        else if (tnc == false || tnc == null) fmt = "L";
                    }
                }
                if (btemp == 0 || btemp > 15) btemp = 15;
                StringBuilder sb = new StringBuilder();
                switch (fmt.ToUpperInvariant())
                {
                    case "L":
                        {
                            switch (trank)
                            {
                                case 1:
                                    {
                                        for (int cnt = 0; cnt < tsize[0]; cnt++)
                                        {
                                            sb.AppendLine(vector[cnt].ToString("G15", ifp));
                                        }
                                        return sb.ToString();
                                    }
                                case 2:
                                    {
                                        for (int cnt = 0; cnt < tsize[0]; cnt++)
                                        {
                                            for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                            {
                                                sb.Append(matrix[cnt, cnt2].ToString("G15", ifp));
                                                sb.Append(" ");
                                            }
                                            sb.AppendLine();
                                        }
                                        return sb.ToString();
                                    }
                                case 3:
                                    {
                                        for (int cnt = 0; cnt < tsize[0]; cnt++)
                                        {
                                            for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                            {
                                                for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                                {
                                                    sb.Append(tensor[cnt, cnt2, cnt3].ToString("G15", ifp));
                                                    sb.Append(" ");
                                                }
                                                sb.Append("\t");
                                            }
                                            sb.AppendLine();
                                        }
                                        return sb.ToString();
                                    }
                                case 4:
                                    {
                                        for (int cnt = 0; cnt < tsize[0]; cnt++)
                                        {
                                            for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                            {
                                                for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                                {
                                                    for (int cnt4 = 0; cnt4 < tsize[3]; cnt4++)
                                                    {
                                                        sb.Append(tensor2[cnt, cnt2, cnt3, cnt4].ToString("G15", ifp));
                                                        sb.Append(" ");
                                                    }
                                                    sb.Append("\t");
                                                }
                                                sb.AppendLine();
                                            }
                                            sb.AppendLine();
                                            sb.AppendLine();
                                            sb.AppendLine();
                                        }
                                        return sb.ToString();
                                    }
                                default: throw new NotImplementedException("ToString : Not implemented for higher rank tensor.");
                            }
                        }
                    case "LJ":
                        {
                            switch (trank)
                            {
                                case 1:
                                    {
                                        for (int cnt = 0; cnt < tsize[0]; cnt++)
                                        {
                                            sb.AppendLine(vector[cnt].ToString(string.Format("G{0}", btemp), ifp));
                                        }
                                        return sb.ToString();
                                    }
                                case 2:
                                    {
                                        for (int cnt = 0; cnt < tsize[0]; cnt++)
                                        {
                                            for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                            {
                                                sb.Append(matrix[cnt, cnt2].ToString(string.Format("G{0}", btemp), ifp));
                                                sb.Append(" ");
                                            }
                                            sb.AppendLine();
                                        }
                                        return sb.ToString();
                                    }
                                case 3:
                                    {
                                        for (int cnt = 0; cnt < tsize[0]; cnt++)
                                        {
                                            for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                            {
                                                for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                                {
                                                    sb.Append(tensor[cnt, cnt2, cnt3].ToString(string.Format("G{0}", btemp), ifp));
                                                    sb.Append(" ");
                                                }
                                                sb.Append("\t");
                                            }
                                            sb.AppendLine();
                                        }
                                        return sb.ToString();
                                    }
                                case 4:
                                    {
                                        for (int cnt = 0; cnt < tsize[0]; cnt++)
                                        {
                                            for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                            {
                                                for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                                {
                                                    for (int cnt4 = 0; cnt4 < tsize[3]; cnt4++)
                                                    {
                                                        sb.Append(tensor2[cnt, cnt2, cnt3, cnt4].ToString(string.Format("G{0}", btemp), ifp));
                                                        sb.Append(" ");
                                                    }
                                                    sb.Append("\t");
                                                }
                                                sb.AppendLine();
                                            }
                                            sb.AppendLine();
                                            sb.AppendLine();
                                            sb.AppendLine();
                                        }
                                        return sb.ToString();
                                    }
                                default: throw new NotImplementedException("ToString : Not implemented for higher rank tensor.");
                            }
                        }
                    case "G":
                        {
                            switch (trank)
                            {
                                case 1:
                                    {
                                        for (int cnt = 0; cnt < tsize[0]; cnt++)
                                        {
                                            sb.Append(vector[cnt].ToString("G15", ifp));
                                            sb.Append(" ");
                                        }
                                        return sb.ToString();
                                    }
                                case 2:
                                    {
                                        for (int cnt = 0; cnt < tsize[0]; cnt++)
                                        {
                                            for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                            {
                                                sb.Append(matrix[cnt, cnt2].ToString("G15", ifp));
                                                sb.Append(" ");
                                            }
                                            sb.AppendLine();
                                        }
                                        return sb.ToString();
                                    }
                                case 3:
                                    {
                                        for (int cnt = 0; cnt < tsize[0]; cnt++)
                                        {
                                            for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                            {
                                                for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                                {
                                                    sb.Append(tensor[cnt, cnt2, cnt3].ToString("G15", ifp));
                                                    sb.Append(" ");
                                                }
                                                sb.Append("\t");
                                            }
                                            sb.AppendLine();
                                        }
                                        return sb.ToString();
                                    }
                                case 4:
                                    {
                                        for (int cnt = 0; cnt < tsize[0]; cnt++)
                                        {
                                            for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                            {
                                                for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                                {
                                                    for (int cnt4 = 0; cnt4 < tsize[3]; cnt4++)
                                                    {
                                                        sb.Append(tensor2[cnt, cnt2, cnt3, cnt4].ToString("G15", ifp));
                                                        sb.Append(" ");
                                                    }
                                                    sb.Append("\t");
                                                }
                                                sb.AppendLine();
                                            }
                                            sb.AppendLine();
                                            sb.AppendLine();
                                            sb.AppendLine();
                                        }
                                        return sb.ToString();
                                    }
                                default: throw new NotImplementedException("ToString : Not implemented for higher rank tensor.");
                            }
                        }
                    case "GJ":
                        {
                            switch (trank)
                            {
                                case 1:
                                    {
                                        for (int cnt = 0; cnt < tsize[0]; cnt++)
                                        {
                                            sb.Append(vector[cnt].ToString(string.Format("G{0}", btemp), ifp));
                                            sb.Append(" ");
                                        }
                                        return sb.ToString();
                                    }
                                case 2:
                                    {
                                        for (int cnt = 0; cnt < tsize[0]; cnt++)
                                        {
                                            for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                            {
                                                sb.Append(matrix[cnt, cnt2].ToString(string.Format("G{0}", btemp), ifp));
                                                sb.Append(" ");
                                            }
                                            sb.AppendLine();
                                        }
                                        return sb.ToString();
                                    }
                                case 3:
                                    {
                                        for (int cnt = 0; cnt < tsize[0]; cnt++)
                                        {
                                            for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                            {
                                                for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                                {
                                                    sb.Append(tensor[cnt, cnt2, cnt3].ToString(string.Format("G{0}", btemp), ifp));
                                                    sb.Append(" ");
                                                }
                                                sb.Append("\t");
                                            }
                                            sb.AppendLine();
                                        }
                                        return sb.ToString();
                                    }
                                case 4:
                                    {
                                        for (int cnt = 0; cnt < tsize[0]; cnt++)
                                        {
                                            for (int cnt2 = 0; cnt2 < tsize[1]; cnt2++)
                                            {
                                                for (int cnt3 = 0; cnt3 < tsize[2]; cnt3++)
                                                {
                                                    for (int cnt4 = 0; cnt4 < tsize[3]; cnt4++)
                                                    {
                                                        sb.Append(tensor2[cnt, cnt2, cnt3, cnt4].ToString(string.Format("G{0}", btemp), ifp));
                                                        sb.Append(" ");
                                                    }
                                                    sb.Append("\t");
                                                }
                                                sb.AppendLine();
                                            }
                                            sb.AppendLine();
                                            sb.AppendLine();
                                            sb.AppendLine();
                                        }
                                        return sb.ToString();
                                    }
                                default: throw new NotImplementedException("ToString : Not implemented for higher rank tensor.");
                            }
                        }
                    default: goto case "L";
                }
            }
        }
        internal struct Tensoru
        {
            internal Tensor t;
            internal Tensoru(double[,,] d)
            {
                t = new Tensor(d);
            }
        }
        internal struct Matorix : IFormattable
        {
            internal Tensor t;
            internal Matorix(double[,] d)
            {
                t = new Tensor(d);
            }

            public static Matorix operator *(Matorix m1, Matorix m2)
            {
                return new Matorix(Tensor.TKzs(m1.t, m2.t).matrix);
            }
            public static Matorix operator +(Matorix m1, Matorix m2)
            {
                return new Matorix(Tensor.TTzs(m1.t, m2.t).matrix);
            }
            public static Matorix operator -(Matorix m1, Matorix m2)
            {
                return new Matorix(Tensor.THzs(m1.t, m2.t).matrix);
            }
            public static Matorix operator -(Matorix m1)
            {
                return new Matorix(m1.t.TJN().matrix);
            }
            public static Matorix operator !(Matorix m1)
            {
                return new Matorix(m1.t.MTc().matrix);
            }
            public static Matorix operator ~(Matorix m1)
            {
                return new Matorix(m1.t.GgLU(true).matrix);
            }
            internal Matorix Inv()
            {
                return new Matorix(t.TMPi().matrix);
            }
            static internal Matorix Invs(Matorix m)
            {
                return new Matorix(m.t.TMPi().matrix);
            }
            public override string ToString()
            {
                return t.ToString();
            }
            public string ToString(IFormatProvider ifp)
            {
                return t.ToString(ifp);
            }
            public string ToString(string fmt)
            {
                return t.ToString(fmt);
            }
            public string ToString(string fmt, IFormatProvider ifp)
            {
                return t.ToString(fmt, ifp);
            }
        }
        internal struct Bector : IFormattable
        {
            internal Tensor t;
            internal Bector(double[] d, bool? b)
            {
                t = new Tensor(d, b);
            }
            public static double operator *(Bector b1, Bector b2)
            {
                Tensor ttemp = Tensor.TKzs(b1.t, b2.t);
                if (ttemp.trank != 1 || ttemp.tnc == null) throw new ArgumentOutOfRangeException("Bector", "* : Invalid calculation.");
                return ttemp.vector[0];
            }
            public static Bector operator *(Bector b1, Matorix m2)
            {
                Tensor ttemp = Tensor.TKzs(b1.t, m2.t);
                if (ttemp.trank != 1) throw new ArgumentOutOfRangeException("Bector", "* : Invalid calculation.");
                if (b1.t.tnc != ttemp.tnc) throw new ArgumentOutOfRangeException("Bector", "* : Invalid calculation.");
                return new Bector(ttemp.vector, ttemp.tnc);
            }
            public static Bector operator *(Matorix m1, Bector b2)
            {
                Tensor ttemp = Tensor.TKzs(m1.t, b2.t);
                if (ttemp.trank != 1) throw new ArgumentOutOfRangeException("Bector", "* : Invalid calculation.");
                if (b2.t.tnc != ttemp.tnc) throw new ArgumentOutOfRangeException("Bector", "* : Invalid calculation.");
                return new Bector(ttemp.vector, ttemp.tnc);
            }
            public static Bector operator +(Bector b1, Bector b2)
            {
                Tensor ttemp = Tensor.TTzs(b1.t, b2.t);
                if (b1.t.tnc != ttemp.tnc || b2.t.tnc != ttemp.tnc) throw new ArgumentOutOfRangeException("Bector", "+ : Invalid calculation.");
                return new Bector(ttemp.vector, ttemp.tnc);
            }
            public static Bector operator -(Bector b1, Bector b2)
            {
                Tensor ttemp = Tensor.THzs(b1.t, b2.t);
                if (b1.t.tnc != ttemp.tnc || b2.t.tnc != ttemp.tnc) throw new ArgumentOutOfRangeException("Bector", "+ : Invalid calculation.");
                return new Bector(ttemp.vector, ttemp.tnc);
            }
            public static Bector operator -(Bector b1)
            {
                Tensor ttemp = b1.t.TJN();
                if (b1.t.tnc != ttemp.tnc) throw new ArgumentOutOfRangeException("Bector", "- : Invalid calculation.");
                return new Bector(ttemp.vector, ttemp.tnc);
            }
            public static Bector operator !(Bector b1)
            {
                Tensor ttemp = b1.t.Vtc();
                if (b1.t.tnc != ttemp.tnc) throw new ArgumentOutOfRangeException("Bector", "- : Invalid calculation.");
                return new Bector(ttemp.vector, ttemp.tnc);
            }
            public override string ToString()
            {
                return t.ToString();
            }
            public string ToString(IFormatProvider ifp)
            {
                return t.ToString(ifp);
            }
            public string ToString(string fmt)
            {
                return t.ToString(fmt);
            }
            public string ToString(string fmt, IFormatProvider ifp)
            {
                return t.ToString(fmt, ifp);
            }
        }
        internal class AvxYSFF
        {
            static internal double Avx2Tz(in double[] ds)//Avx2を使う足し算
            {
                double dres = 0;
                if (ds.Length >= 8)
                {
                    int rem;
                    int l = Math.DivRem(ds.Length, 4, out rem);
                    unsafe
                    {
                        fixed (double* dsp = ds)
                        {
                            Vector256<double> v = Avx2.LoadVector256(dsp);
                            Vector256<double> v2;
                            for (int pind = 1; pind < l; pind++)
                            {
                                v2 = Avx2.LoadVector256(dsp + pind * 4);
                                v = Avx2.Add(v, v2);
                            }
                            dres = v.GetElement<double>(0) + v.GetElement<double>(1) + v.GetElement<double>(2) + v.GetElement<double>(3);
                        }
                    }
                    for (int cnt = 1; cnt <= rem; cnt++)
                    {
                        dres += ds[ds.Length - cnt];
                    }
                }
                else if (ds.Length >= 4)
                {
                    int rem;
                    int l = Math.DivRem(ds.Length, 2, out rem);
                    unsafe
                    {
                        fixed (double* dsp = ds)
                        {
                            Vector128<double> v = Avx2.LoadVector128(dsp);
                            Vector128<double> v2;
                            for (int pind = 1; pind < l; pind++)
                            {
                                v2 = Avx2.LoadVector128(dsp + pind * 2);
                                v = Avx2.Add(v, v2);
                            }
                            dres = v.GetElement<double>(0) + v.GetElement<double>(1);
                        }
                    }
                    for (int cnt = 1; cnt <= rem; cnt++)
                    {
                        dres += ds[ds.Length - cnt];
                    }
                }
                else
                {
                    for (int cnt = 0; cnt < ds.Length; cnt++)
                    {
                        dres += ds[cnt];
                    }
                }
                return dres;
            }
            static internal double Avx2Kz(in double[] ds)//Avx2を使う掛け算
            {
                double dres = 1;
                if (ds.Length >= 8)
                {
                    int rem;
                    int l = Math.DivRem(ds.Length, 4, out rem);
                    unsafe
                    {
                        fixed (double* dsp = ds)
                        {
                            Vector256<double> v = Avx2.LoadVector256(dsp);
                            Vector256<double> v2;
                            for (int pind = 1; pind < l; pind++)
                            {
                                v2 = Avx2.LoadVector256(dsp + pind * 4);
                                v = Avx2.Multiply(v, v2);
                            }
                            dres = v.GetElement<double>(0) * v.GetElement<double>(1) * v.GetElement<double>(2) * v.GetElement<double>(3);
                        }
                    }
                    for (int cnt = 1; cnt <= rem; cnt++)
                    {
                        dres *= ds[ds.Length - cnt];
                    }
                }
                else if (ds.Length >= 4)
                {
                    int rem;
                    int l = Math.DivRem(ds.Length, 2, out rem);
                    unsafe
                    {
                        fixed (double* dsp = ds)
                        {
                            Vector128<double> v = Avx2.LoadVector128(dsp);
                            Vector128<double> v2;
                            for (int pind = 1; pind < l; pind++)
                            {
                                v2 = Avx2.LoadVector128(dsp + pind * 2);
                                v = Avx2.Multiply(v, v2);
                            }
                            dres = v.GetElement<double>(0) * v.GetElement<double>(1);
                        }
                    }
                    for (int cnt = 1; cnt <= rem; cnt++)
                    {
                        dres *= ds[ds.Length - cnt];
                    }
                }
                else
                {
                    for (int cnt = 0; cnt < ds.Length; cnt++)
                    {
                        dres *= ds[cnt];
                    }
                }
                return dres;
            }
        }
        static internal class Exl2610
        {
            static internal string DtA(int s)//(1から始まるの)10進数から(Aから始まるの)26進数へ変換
            {
                if (s <= 0) return "NaN";
                StringBuilder sb = new StringBuilder();
                int r;
                int d = s;
                while (true)
                {
                    d = Math.DivRem(d, 26, out r);
                    r--;
                    if (r == -1) sb.Append('Z');
                    else
                    {
                        sb.Insert(0, (char)(65 + r));
                    }
                    if (d == 0) break;
                }
                return sb.ToString();
            }
            static internal int AtD(string s)//(1から始まるの)10進数から(Aから始まるの)26進数へ変換
            {
                int r = 0;
                int itemp = s.Length - 1;
                int o, itemp2;
                for (int cnt = 0; cnt < s.Length; cnt++)
                {
                    o = 1;
                    for (int cnt2 = 0; cnt2 < itemp - cnt; cnt2++)
                    {
                        o *= 26;
                    }
                    itemp2 = Convert.ToInt32(s[cnt]) - 64;
                    if (itemp2 < 1 || itemp2 > 26)
                    {
                        return -1;
                    }
                    r += o * itemp2;
                }
                return r;
            }
            static internal string DtA0(int s)//(0から始まるの)10進数から(Aから始まるの)26進数へ変換
            {
                if (s < 0) return "NaN";
                StringBuilder sb = new StringBuilder();
                int r;
                int d = Math.DivRem(s, 26, out r);
                sb.Insert(0, (char)(65 + r));
                if (d == 0) return sb.ToString();
                while (true)
                {
                    d = Math.DivRem(d, 26, out r);
                    sb.Insert(0, (char)(64 + r));
                    if (d == 0) break;
                }
                return sb.ToString();
            }
        }
        internal class BMR//Box-Muller random number
        {
            private readonly Random r1, r2;
            private readonly double c = 0.0;
            private readonly double s = 1.0;

            internal BMR()
            {
                r1 = new Random();
                r2 = new Random();
                c = 0.0;
                s = 1.0;
            }
            internal BMR(int sr1, int sr2)
            {
                r1 = new Random(sr1);
                r2 = new Random(sr2);
                c = 0.0;
                s = 1.0;
            }
            internal BMR(double exp, double var)
            {
                r1 = new Random();
                r2 = new Random();
                c = exp;
                s = var;
            }
            internal BMR(int sr1, int sr2, double exp, double var)
            {
                r1 = new Random(sr1);
                r2 = new Random(sr2);
                c = 0.0;
                s = 1.0;
            }
            internal double GetRandC()
            {
                return Math.Sqrt(-2 * Math.Log(r1.NextDouble())) * Math.Cos(2 * Math.PI * r2.NextDouble()) * s + c;
            }
            internal double GetRandS()
            {
                return Math.Sqrt(-2 * Math.Log(r1.NextDouble())) * Math.Sin(2 * Math.PI * r2.NextDouble()) * s + c;
            }
        }
        internal class GammaFunc//Γ関数
        {
            private double d;
            private double dtemp = 1.0;
            internal GammaFunc(double din)
            {
                d = din;
                dtemp = 1.0;
            }
            internal double GetValue()
            {
                if (d > 0.0)
                {
                    if (d > int.MaxValue)
                    {
                        return double.PositiveInfinity;
                    }
                    if (d - (int)d == 0.0)
                    {
                        return factorial();
                    }
                    if ((int)(d * 2) - d * 2 == 0.0)
                    {
                        return hfactorial();
                    }
                    return GN();
                }
                else
                {
                    if (d < int.MinValue)
                    {
                        return double.NaN;
                    }
                    if (d - (int)d == 0.0)
                    {
                        return double.NaN;
                    }
                    if ((int)(d * 2) - d * 2 == 0.0)
                    {
                        return hfactorial();
                    }
                    return GN();
                }
            }
            internal double hfactorial()
            {
                if (d < 0.5)
                {
                    dtemp *= 1 / d;
                    d++;
                    return hfactorial();
                }
                else if (d > 0.5)
                {
                    dtemp *= --d;
                    return hfactorial();
                }
                else
                {
                    return Math.Sqrt(Math.PI) * dtemp;
                }
            }
            internal double factorial()
            {
                if (d > 2.0)
                {
                    dtemp *= --d;
                    return factorial();
                }
                else
                {
                    return dtemp;
                }
            }
            internal double GN()
            {
                if (d <= 34.34)
                {
                    dtemp *= 1.0 / d;
                    d++;
                    return GN();
                }
                return Math.Exp(0.5 * (Math.Log(2.0 * Math.PI) - Math.Log(d)) + d * (Math.Log(d + 1.0 / (12.0 * d - 0.1 / d)) - 1.0)) * dtemp;
            }
        }
    }
}
