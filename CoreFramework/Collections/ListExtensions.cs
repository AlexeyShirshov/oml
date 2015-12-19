using System;
using System.Collections.Generic;
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
    }
}
