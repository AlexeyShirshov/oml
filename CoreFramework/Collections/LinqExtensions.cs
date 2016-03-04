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
            foreach (var i in System.Linq.Enumerable.Range(0, (end-start).Days+1))
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

        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> eu, Func<T, T, bool> eq, Func<T, int> hash)
        {
            return eu.Distinct(new EqualityFunc<T>(eq, hash));
        }

        public static IEnumerable<IGrouping<TKey, T>> GroupBy<T, TKey>(this IEnumerable<T> eu, Func<T, TKey> keySelector, 
            Func<TKey, TKey, bool> eq, Func<TKey, int> hash)
        {
            return eu.GroupBy(keySelector, new EqualityFunc<TKey>(eq, hash));
        }

        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<T, TKey, TElement>(this IEnumerable<T> eu, 
            Func<T, TKey> keySelector,
            Func<T, TElement> elementSelector,
            Func<TKey, TKey, bool> eq, Func<TKey, int> hash)
        {
            return eu.GroupBy(keySelector, elementSelector, new EqualityFunc<TKey>(eq, hash));
        }
    }
}
