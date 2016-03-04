using System;
using System.Collections.Generic;
using System.Text;

namespace CoreFramework.Globalization
{
    public class CultureSwitcher : IDisposable
    {
        private System.Globalization.CultureInfo _old_culture;
        public CultureSwitcher(string culture)
        {
            _old_culture = System.Threading.Thread.CurrentThread.CurrentUICulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(culture);
        }

        #region IDisposable Members

        public void Dispose()
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = _old_culture;
        }

        #endregion
    }

    public class RuTranslit
    {
        private static byte[] translit_table = new byte[256]{
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,40, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 7, 0, 0, 0, 0, 0, 0, 0,
            34,35,36,37,38,39,41,42,43,44,45,46,47,48,49,50,
            51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,
            1, 2, 3, 4, 5, 6, 8, 9,10,11,12,13,14,15,16,17,
            18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33};
        private const int win1251_RLen = 66;
        private static string[] win1251_R = new string[win1251_RLen] {
            "a", "b", "v", "g", "d", "e", "yo", "zh", "z", "i",
            "y", "k", "l", "m", "n", "o", "p", "r", "s", "t",
            "u", "f", "h", "c", "ch", "sh", "sch", "", "y", "",
            "e", "yu", "ya",
            "A", "B", "V", "G", "D", "E", "YO", "ZH", "Z", "I",
            "Y", "K", "L", "M", "N", "O", "P", "R", "S", "T",
            "U", "F", "H", "c", "CH", "SH", "SCH", "", "Y", "",
            "E", "YU", "YA"};

        public static string Translate(string val)
        {
            byte[] arr = Encoding.Default.GetBytes(val);
            StringBuilder res = new StringBuilder();
            foreach (byte b in arr)
            {
                int k = translit_table[b];
                res.Append((k > 0) ? win1251_R[k - 1] : Encoding.Default.GetString(new byte[1] { b }));
            }
            return res.ToString();
        }
    }
}
