using CoreFramework.Threading;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CoreFramework.Collections
{
    public class ConcurrentList<T> : IList<T>
    {
        private List<T> internalList;

        private readonly SpinLockRef _lock = new SpinLockRef();

        public ConcurrentList()
        {
            internalList = new List<T>();
        }

        // Other Elements of IList implementation

        public IEnumerator<T> GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        public List<T> Clone()
        {
            ThreadLocal<List<T>> threadClonedList = new ThreadLocal<List<T>>();

            using(new CSScopeMgrLite(_lock))
            {
                internalList.ForEach(element => { threadClonedList.Value.Add(element); });
            }

            return (threadClonedList.Value);
        }

        public void Add(T item)
        {
            using(new CSScopeMgrLite(_lock))
            {
                internalList.Add(item);
            }
        }

        public bool Remove(T item)
        {
            bool isRemoved;

            using(new CSScopeMgrLite(_lock))
            {
                isRemoved = internalList.Remove(item);
            }

            return (isRemoved);
        }

        public void Clear()
        {
            using(new CSScopeMgrLite(_lock))
            {
                internalList.Clear();
            }
        }

        public bool Contains(T item)
        {
            bool containsItem;

            using(new CSScopeMgrLite(_lock))
            {
                containsItem = internalList.Contains(item);
            }

            return (containsItem);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            using(new CSScopeMgrLite(_lock))
            {
                internalList.CopyTo(array, arrayIndex);
            }
        }

        public int Count
        {
            get
            {
                int count;

                using(new CSScopeMgrLite(_lock))
                {
                    count = internalList.Count;
                }

                return (count);
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public int IndexOf(T item)
        {
            int itemIndex;

            using(new CSScopeMgrLite(_lock))
            {
                itemIndex = internalList.IndexOf(item);
            }

            return (itemIndex);
        }

        public void Insert(int index, T item)
        {
            using(new CSScopeMgrLite(_lock))
            {
                internalList.Insert(index, item);
            }
        }

        public void RemoveAt(int index)
        {
            using(new CSScopeMgrLite(_lock))
            {
                internalList.RemoveAt(index);
            }
        }

        public T this[int index]
        {
            get
            {
                using(new CSScopeMgrLite(_lock))
                {
                    return internalList[index];
                }
            }
            set
            {
                using(new CSScopeMgrLite(_lock))
                {
                    internalList[index] = value;
                }
            }
        }

        public IDisposable CreateSyncLockScope
        {
            get
            {
                return new CSScopeMgrLite(_lock);
            }
        }
    }
}