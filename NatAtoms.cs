using System;

namespace RemiChemieConsole
{
    class NatAtoms
    {
        private static readonly NatAtomT H = new NatAtomT("H", 1, 120.0, 53.0, 25.0, new Tuple<double, double>(0.0, 140.0), 72.8, 2.20, 2.3, 1.0, 1312.0);
        private static readonly NatAtomT He = new NatAtomT("He", 2);
        private static readonly NatAtomT Li = new NatAtomT("Li", 3, 182.0, 167.0, 145.0, new Tuple<double, double>(76.0, 174.0), 59.6, 0.98, 0.912, 1.279, 520.0);
        private static readonly NatAtomT B = new NatAtomT("B", 5, 192.0, 87.0, 85.0, new Tuple<double, double>(27.0, 158.0), 27.0, 2.04, 2.051, 2.421, 800.6);
        private static readonly NatAtomT C = new NatAtomT("C", 6, 170.0, 67.0, 70.0, new Tuple<double, double>(16.0, 152.0), 121.8, 2.55, 2.544, 3.136, 1086.5);
        private static readonly NatAtomT N = new NatAtomT("N", 7, 155.0, 56.0, 65.0, new Tuple<double, double>(13.0, 146.0), -0.07, 3.04, 3.066, 3.834, 1402.3);
        private static readonly NatAtomT O = new NatAtomT("O", 8, 152.0, 48.0, 60.0, new Tuple<double, double>(10.0, 140.0), 141.0, 3.44, 3.610, 4.453, 1313.9);
        private static readonly NatAtomT F = new NatAtomT("F", 9, 147.0, 42.0, 50.0, new Tuple<double, double>(8.0, 133.0), 328.0, 3.98, 4.193, 5.100, 1681.0);
        private static readonly NatAtomT Ne = new NatAtomT("Ne", 10);
        private static readonly NatAtomT Na = new NatAtomT("Na", 11);
        private static readonly NatAtomT Mg = new NatAtomT("Mg", 12);
        private static readonly NatAtomT Al = new NatAtomT("Al", 13);
        private static readonly NatAtomT Si = new NatAtomT("Si", 14, 210.0, 111.0, 110.0, new Tuple<double, double>(40.0, 190.0), 133.6, 1.9, 1.916, 4.285, 786.5);
        private static readonly NatAtomT P = new NatAtomT("P", 15, 180.0, 98.0, 100.0, new Tuple<double, double>(38.0, 187.0), 72.0, 2.19, 2.253, 4.886, 1011.8);
        private static readonly NatAtomT S = new NatAtomT("S", 16, 180.0, 88.0, 100.0, new Tuple<double, double>(29.0, 184.0), 200.0, 2.58, 2.589, 5.482, 999.6);
        private static readonly NatAtomT Cl = new NatAtomT("Cl", 17, 175.0, 79.0, 100.0, new Tuple<double, double>(27.0, 181.0), 349.0, 3.16, 2.869, 6.116, 1251.2);
        private static readonly NatAtomT Ar = new NatAtomT("Ar", 18);
        private static readonly NatAtomT K = new NatAtomT("K", 19);
        private static readonly NatAtomT Ca = new NatAtomT("Ca", 20);
        private static readonly NatAtomT Ti = new NatAtomT("Ti", 22);
        private static readonly NatAtomT Fe = new NatAtomT("Fe", 26);
        private static readonly NatAtomT Co = new NatAtomT("Co", 27);
        private static readonly NatAtomT Ni = new NatAtomT("Ni", 28);
        private static readonly NatAtomT Cu = new NatAtomT("Cu", 29);
        private static readonly NatAtomT Zn = new NatAtomT("Zn", 30);
        private static readonly NatAtomT Ge = new NatAtomT("Ge", 32, 211.0, 125.0, 125.0, new Tuple<double, double>(53.0, 210.0), 119.0, 2.01, 1.994, 6.78, 762.0);
        private static readonly NatAtomT As = new NatAtomT("As", 33, 185.0, 114.0, 115.0, new Tuple<double, double>(46.0, 200.0), 79.0, 2.18, 2.211, 7.449, 947.0);
        private static readonly NatAtomT Se = new NatAtomT("Se", 34, 190.0, 103.0, 115.0, new Tuple<double, double>(42.0, 198.0), 195.0, 2.55, 2.424, 8.287, 941.0);
        private static readonly NatAtomT Br = new NatAtomT("Br", 35, 185.0, 94.0, 115.0, new Tuple<double, double>(39.0, 196.0), 324.6, 2.96, 2.685, 9.028, 1139.9);
        private static readonly NatAtomT Kr = new NatAtomT("Kr", 36);
        private static readonly NatAtomT Tc = new NatAtomT("Tc", 43);
        private static readonly NatAtomT Pd = new NatAtomT("Pd", 46);
        private static readonly NatAtomT Sn = new NatAtomT("Sn", 50, 217.0, 145.0, 145.0, new Tuple<double, double>(69.0, 230.0), 107.3, 1.96, 1.824, 9.102, 708.6);
        private static readonly NatAtomT Sb = new NatAtomT("Sb", 51);
        private static readonly NatAtomT Te = new NatAtomT("Te", 52, 206.0, 123.0, 140.0, new Tuple<double, double>(56.0, 221.0), 190.2, 2.1, 2.158, 10.809, 869.3);
        private static readonly NatAtomT I = new NatAtomT("I", 53, 198.0, 115.0, 140.0, new Tuple<double, double>(53.0, 220.0), 295.2, 2.66, 2.359, 11.612, 1008.4);
        private static readonly NatAtomT Xe = new NatAtomT("Xe", 54);
        private static readonly NatAtomT Ba = new NatAtomT("Ba", 56);
        private static readonly NatAtomT Ce = new NatAtomT("Ce", 58);
        private static readonly NatAtomT Os = new NatAtomT("Os", 76);
        private static readonly NatAtomT Ir = new NatAtomT("Ir", 77);
        private static readonly NatAtomT Hg = new NatAtomT("Hg", 80);
        private static readonly NatAtomT Pb = new NatAtomT("Pb", 82);
        private static readonly NatAtomT Rn = new NatAtomT("Rn", 86);
        private static readonly NatAtomT Th = new NatAtomT("Th", 90);
        private static readonly NatAtomT U = new NatAtomT("U", 92);
        private static readonly NatAtomT Pu = new NatAtomT("Pu", 94);
        private NatAtomT fthfL(string s)
        {
            switch (s)
            {
                case "H": return H;
                case "He": return He;
                case "Li": return Li;
                case "B": return B;
                case "C": return C;
                case "N": return N;
                case "O": return O;
                case "F": return F;
                case "Ne": return Ne;
                case "Na": return Na;
                case "Mg": return Mg;
                case "Al": return Al;
                case "Si": return Si;
                case "P": return P;
                case "S": return S;
                case "Cl": return Cl;
                case "Ar": return Ar;
                case "K": return K;
                case "Ca": return Ca;
                case "Ti": return Ti;
                case "Fe": return Fe;
                case "Zn": return Zn;
                case "Ge": return Ge;
                case "As": return As;
                case "Se": return Se;
                case "Br": return Br;
                case "Kr": return Kr;
                case "Tc": return Tc;
                case "Pd": return Pd;
                case "Sn": return Sn;
                case "Sb": return Sb;
                case "Te": return Te;
                case "I": return I;
                case "Xe": return Xe;
                case "Ba": return Ba;
                case "Os": return Os;
                case "Ir": return Ir;
                case "Pb": return Pb;
                case "Rn": return Rn;
                case "Th": return Th;
                case "U": return U;
                case "Pu": return Pu;
                default: throw new ArgumentException("fthfL : Atom not supported.", "s");
            }
        }
        private NatAtomT fthfP(byte b)
        {
            switch (b)
            {
                case 1: return H;
                case 2: return He;
                case 3: return Li;
                case 5: return B;
                case 6: return C;
                case 7: return N;
                case 8: return O;
                case 9: return F;
                case 10: return Ne;
                case 11: return Na;
                case 12: return Mg;
                case 13: return Al;
                case 14: return Si;
                case 15: return P;
                case 16: return S;
                case 17: return Cl;
                case 18: return Ar;
                case 19: return K;
                case 20: return Ca;
                case 22: return Ti;
                case 26: return Fe;
                case 30: return Zn;
                case 32: return Ge;
                case 33: return As;
                case 34: return Se;
                case 35: return Br;
                case 36: return Kr;
                case 43: return Tc;
                case 46: return Pd;
                case 50: return Sn;
                case 51: return Sb;
                case 52: return Te;
                case 53: return I;
                case 54: return Xe;
                case 56: return Ba;
                case 76: return Os;
                case 77: return Ir;
                case 82: return Pb;
                case 86: return Rn;
                case 90: return Th;
                case 92: return U;
                case 94: return Pu;
                default: throw new ArgumentException("fthfL : Atom not supported.", "s");
            }
        }
        internal byte? fthPfL(string s)//原子名から原子番号に変換
        {
            byte? b = null;
            try
            {
                NatAtomT a = fthfL(s);
                b = a.atmprtn;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException({0}) => {1}\r\nSource : {2}", e.ParamName, e.Message, e.Source ?? "unknown");
                if (e.InnerException != null) Console.WriteLine("Inner Exception => {0}\r\nSource : {1}", e.InnerException.Message, e.Source ?? "unknown");
                return b;
            }
            return b;
        }
        internal string fthLfP(byte b)//原子番号から原子名に変換
        {
            string s = null;
            try
            {
                NatAtomT a = fthfP(b);
                s = a.atmlb;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException(0) => {1}\r\nSource : {2}", e.ParamName, e.Message, e.Source ?? "unknown");
                if (e.InnerException != null) Console.WriteLine("Inner Exception => {0}\r\nSource : {1}", e.InnerException.Message, e.Source ?? "unknown");
                return s;
            }
            return s;
        }
        internal double fthvdWRfP(byte b)//原子番号からファンデルワールス半径に変換
        {
            double d = double.NaN;
            try
            {
                NatAtomT a = fthfP(b);
                d = a.vdWR;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException(0) => {1}\r\nSource : {2}", e.ParamName, e.Message, e.Source ?? "unknown");
                if (e.InnerException != null) Console.WriteLine("Inner Exception => {0}\r\nSource : {1}", e.InnerException.Message, e.Source ?? "unknown");
                return d;
            }
            return d;
        }
        internal double fthAtCRfP(byte b)//原子番号から計算原子半径に変換
        {
            double d = double.NaN;
            try
            {
                NatAtomT a = fthfP(b);
                d = a.AtCR;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException(0) => {1}\r\nSource : {2}", e.ParamName, e.Message, e.Source ?? "unknown");
                if (e.InnerException != null) Console.WriteLine("Inner Exception => {0}\r\nSource : {1}", e.InnerException.Message, e.Source ?? "unknown");
                return d;
            }
            return d;
        }
        internal double fthAtERfP(byte b)//原子番号から経験原子半径に変換
        {
            double d = double.NaN;
            try
            {
                NatAtomT a = fthfP(b);
                d = a.AtER;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException(0) => {1}\r\nSource : {2}", e.ParamName, e.Message, e.Source ?? "unknown");
                if (e.InnerException != null) Console.WriteLine("Inner Exception => {0}\r\nSource : {1}", e.InnerException.Message, e.Source ?? "unknown");
                return d;
            }
            return d;
        }
        internal Tuple<double, double> fthIoRfP(byte b)//原子番号からイオン半径に変換
        {
            Tuple<double, double> tdd = new Tuple<double, double>(double.NaN, double.NaN);
            try
            {
                NatAtomT a = fthfP(b);
                tdd = a.IoR;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException(0) => {1}\r\nSource : {2}", e.ParamName, e.Message, e.Source ?? "unknown");
                if (e.InnerException != null) Console.WriteLine("Inner Exception => {0}\r\nSource : {1}", e.InnerException.Message, e.Source ?? "unknown");
                return tdd;
            }
            return tdd;
        }
        internal double fthEAffP(byte b)//原子番号から電子親和力に変換
        {
            double d = double.NaN;
            try
            {
                NatAtomT a = fthfP(b);
                d = a.EAf;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException(0) => {1}\r\nSource : {2}", e.ParamName, e.Message, e.Source ?? "unknown");
                if (e.InnerException != null) Console.WriteLine("Inner Exception => {0}\r\nSource : {1}", e.InnerException.Message, e.Source ?? "unknown");
                return d;
            }
            return d;
        }
        internal double fthEnPfP(byte b)//原子番号からポーリングの電気陰性度に変換
        {
            double d = double.NaN;
            try
            {
                NatAtomT a = fthfP(b);
                d = a.EnP;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException(0) => {1}\r\nSource : {2}", e.ParamName, e.Message, e.Source ?? "unknown");
                if (e.InnerException != null) Console.WriteLine("Inner Exception => {0}\r\nSource : {1}", e.InnerException.Message, e.Source ?? "unknown");
                return d;
            }
            return d;
        }
        internal double fthEnAfP(byte b)//原子番号からアレンの電気陰性度に変換
        {
            double d = double.NaN;
            try
            {
                NatAtomT a = fthfP(b);
                d = a.EnA;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException(0) => {1}\r\nSource : {2}", e.ParamName, e.Message, e.Source ?? "unknown");
                if (e.InnerException != null) Console.WriteLine("Inner Exception => {0}\r\nSource : {1}", e.InnerException.Message, e.Source ?? "unknown");
                return d;
            }
            return d;
        }
        internal double fthEnMfP(byte b)//原子番号からマリケンの電気陰性度に変換
        {
            double d = double.NaN;
            try
            {
                NatAtomT a = fthfP(b);
                d = (a.IE1 + a.EAf) / 2;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException(0) => {1}\r\nSource : {2}", e.ParamName, e.Message, e.Source ?? "unknown");
                if (e.InnerException != null) Console.WriteLine("Inner Exception => {0}\r\nSource : {1}", e.InnerException.Message, e.Source ?? "unknown");
                return d;
            }
            return d;
        }
        internal double fthEnARfPr(byte b, double r)//原子番号からオールレッド・ロコウの電気陰性度に変換,rは共有結合半径(pm)
        {
            double d = double.NaN;
            try
            {
                NatAtomT a = fthfP(b);
                d = 3590 * (a.EffNC / Math.Pow(r, 2)) + 0.744;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException(0) => {1}\r\nSource : {2}", e.ParamName, e.Message, e.Source ?? "unknown");
                if (e.InnerException != null) Console.WriteLine("Inner Exception => {0}\r\nSource : {1}", e.InnerException.Message, e.Source ?? "unknown");
                return d;
            }
            return d;
        }
        internal double fthEffNCfP(byte b)//原子番号から有効核電荷に変換
        {
            double d = double.NaN;
            try
            {
                NatAtomT a = fthfP(b);
                d = a.EffNC;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException(0) => {1}\r\nSource : {2}", e.ParamName, e.Message, e.Source ?? "unknown");
                if (e.InnerException != null) Console.WriteLine("Inner Exception => {0}\r\nSource : {1}", e.InnerException.Message, e.Source ?? "unknown");
                return d;
            }
            return d;
        }
        internal double fthIE1fP(byte b)//原子番号から第1イオン化エネルギーに変換
        {
            double d = double.NaN;
            try
            {
                NatAtomT a = fthfP(b);
                d = a.IE1;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException(0) => {1}\r\nSource : {2}", e.ParamName, e.Message, e.Source ?? "unknown");
                if (e.InnerException != null) Console.WriteLine("Inner Exception => {0}\r\nSource : {1}", e.InnerException.Message, e.Source ?? "unknown");
                return d;
            }
            return d;
        }
    }
    class NatAtomT
    {
        private readonly string _atmlb;//原子名
        private readonly byte _atmprtn;//原子番号
        private readonly double _vdWR;//ファンデルワールス半径(pm)
        private readonly double _AtCR;//計算原子半径(pm)
        private readonly double _AtER;//経験原子半径(pm)
        private readonly Tuple<double, double> _IoR;//イオン半径(空/満)(pm)
        private readonly double _EAf;//電子親和力(kJ/mol)
        private readonly double _EnP;//ポーリングの電気陰性度
        private readonly double _EnA;//アレンの電気陰性度
        private readonly double _EffNC;//有効核電荷
        private readonly double _IE1;//第1イオン化エネルギー(kJ/mol)
        internal NatAtomT(string s, byte b, double vdWR, double AtCR, double AtER, Tuple<double, double> IoR, double EAf, double EnP, double EnA, double EffNC, double IE1)
        {
            _atmlb = s;
            _atmprtn = b;
            _vdWR = vdWR;
            _AtCR = AtCR;
            _AtER = AtER;
            _IoR = IoR;
            _EAf = EAf;
            _EnP = EnP;
            _EnA = EnA;
            _EffNC = EffNC;
            _IE1 = IE1;
        }
        internal NatAtomT(string s, byte b)
        {
            _atmlb = s;
            _atmprtn = b;
        }
        internal string atmlb
        {
            get { return _atmlb; }
        }
        internal byte atmprtn
        {
            get { return _atmprtn; }
        }
        internal double vdWR
        {
            get { return _vdWR; }
        }
        internal double AtCR
        {
            get { return _AtCR; }
        }
        internal double AtER
        {
            get { return _AtER; }
        }
        internal Tuple<double, double> IoR
        {
            get { return _IoR; }
        }
        internal double EAf
        {
            get { return _EAf; }
        }
        internal double EnP
        {
            get { return _EnP; }
        }
        internal double EnA
        {
            get { return _EnA; }
        }
        internal double EffNC
        {
            get { return _EffNC; }
        }
        internal double IE1
        {
            get { return _IE1; }
        }
    }
}
