using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
    public class Helper
    {
        public static string Convert(int[] ids, string separator)
        {
            System.Text.StringBuilder s = new System.Text.StringBuilder();
            foreach (int i in ids)
            {
                s.Append(i.ToString() + ",");
            }
            if (s.Length > 0) s.Remove(s.Length - 1, 1); //remove last comma
            return s.ToString();
        }
    }
}
