using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreFramework.Collections
{
    public static class LinqExtensions
    {
        public static IEnumerable<DateTime> Range(DateTime start, DateTime end)
        {
            foreach (var i in System.Linq.Enumerable.Range(0, (end-start).Days))
            {
                yield return start.AddDays(i).Date;
            }
        }
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> coll, Action<T> action)
        {
            if (action != null && coll != null)
            foreach (var item in coll)
            {
                action(item);
            }
            return coll;
        }
    }
}
