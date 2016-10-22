using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace CoreFramework.Collections
{
    public static class ListExtensions
    {
        public static List<T> AddCont<T>(this List<T> arr, T item)
        {
            if (arr != null)                
                arr.Add(item);
            return arr;
        }
        public static List<T> AddRangeCont<T>(this List<T> arr, IEnumerable<T> items)
        {
            if (arr != null)                
                arr.AddRange(items);
            return arr;
        }
        public static string ToHexadecimal(this byte[] ba)
        {
            if (ba == null) return null;
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
        public static byte[] ToBytesFromHexadecimal(this String hex)
        {
            if (string.IsNullOrEmpty(hex)) return null;
            int NumberChars = hex.Length;
            if (NumberChars % 2 > 0) throw new FormatException(string.Format("String has non even length"));
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                try
                {
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                }
                catch(Exception ex)
                {
                    throw new FormatException(string.Format("Error converting string {0} to byte. See inner exception", hex.Substring(i, 2)), ex);
                }
            }
            return bytes;
        }
        public static NameValueCollection ToNameValueCollection(this string str, char pairSeparator, char valueSeparator)
        {
            return ToNameValueCollection(str, new[] { pairSeparator }, new[] { valueSeparator });
        }
        public static NameValueCollection ToNameValueCollection(this string str, char[] pairSeparators = null, char[] valueSeparators = null)
        {
            if (string.IsNullOrEmpty(str)) return null;
            if (pairSeparators == null) pairSeparators = new[] { ';' };
            if (valueSeparators == null) valueSeparators = new[] { '=' };
            var r = new NameValueCollection();
            foreach (var p in str.Split(pairSeparators))
            {
                //var s = Uri.UnescapeDataString(p);
                var ss = p.Split(valueSeparators);
                if (ss.Length == 2)
                {
                    var key = Uri.UnescapeDataString(ss[0]);
                    var val = Uri.UnescapeDataString(ss[1]);
                    r.Add(key, val);
                }
            }

            return r;
        }
    }
}
