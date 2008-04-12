using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace CoreFramework.Structures
{
    [Serializable]
    public class Pair<T>
    {
        private T _first;
        private T _second;

        public Pair()
        {
        }

        public Pair(T first, T second)
        {
            _first = first;
            _second = second;
        }

        public T First
        {
            get { return _first; }
        }

        public T Second
        {
            get { return _second; }
        }
    }

    [Serializable]
    public class Pair<T1, T2>
    {
        private T1 _first;
        private T2 _second;

        public Pair()
        {
        }

        public Pair(T1 first, T2 second)
        {
            _first = first;
            _second = second;
        }

        public T1 First
        {
            get { return _first; }
        }

        public T2 Second
        {
            get { return _second; }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Pair<T1, T2>);
        }

        protected bool Equals(Pair<T1, T2> obj)
        {
            if (obj == null)
                return false;
            else
                return _first.Equals(obj._first) && _second.Equals(obj._second);
        }

        public override int GetHashCode()
        {
            return _first.GetHashCode() ^ _second.GetHashCode();
        }
    }

    public class PairFirstComparer<T1, T2> : IComparer
    {
        #region IComparer Members

        public int Compare(object x, object y)
        {
            return Compare(x as Pair<T1, T2>, y as Pair<T1, T2>);
        }

        protected int Compare(Pair<T1, T2> x, Pair<T1, T2> y)
        {
            if (y != null)
                return ((IComparable)y.First).CompareTo(x.First);
            else if (x != null)
                return 1;
            else
                return 0;

        }

        #endregion
    }

    public class PairSecondComparer<T1, T2> : IComparer
    {
        #region IComparer Members

        public int Compare(object x, object y)
        {
            return Compare(x as Pair<T1, T2>, y as Pair<T1, T2>);
        }

        protected int Compare(Pair<T1, T2> x, Pair<T1, T2> y)
        {
            if (y != null)
                return ((IComparable)y.Second).CompareTo(x.Second);
            else if (x != null)
                return 1;
            else
                return 0;
        }

        #endregion
    }
}
