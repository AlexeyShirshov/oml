using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreFramework
{
    public static class StringExtensions
    {
        public static string Format2(this String s, params object[] args)
        {
            return string.Format(s, args);
        }

        public static string Format2(this String s, object arg)
        {
            return string.Format(s, arg);
        }

        public static string Format2(this String s, object arg0, object arg1)
        {
            return string.Format(s, arg0, arg1);
        }

        public static string Format2(this String s, object arg0, object arg1, object arg2)
        {
            return string.Format(s, arg0, arg1, arg2);
        }
        public static string Format2(this String s, IFormatProvider provider, object arg)
        {
            return string.Format(provider, s, arg);
        }

        public static string Wrap(this String s, string w)
        {
            return "{0}{1}{0}".Format2(w, s);
        }

        public static string LeftNull(this String s, int length)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            if (s.Length <= length)
                return s;

            return s.Substring(0, length);
        }
        public static IEnumerable<string> SplitByLength(this string stringToSplit, int length)
        {
            if (stringToSplit == null)
                return null;

            List<string> l = new List<string>();
            while (stringToSplit.Length > length)
            {
                l.Add(stringToSplit.Substring(0, length));
                stringToSplit = stringToSplit.Substring(length);
            }

            if (stringToSplit.Length > 0)
            {
                l.Add(stringToSplit);
            }

            return l;
        }
    }
}
