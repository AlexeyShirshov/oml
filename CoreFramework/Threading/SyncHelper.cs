using System;
using System.Collections.Generic;
using System.Text;
using CoreFramework.Structures;
using System.Threading;

namespace CoreFramework.Threading
{
    public static class SyncHelper
    {
        class Pair
        {
            public readonly string Key;
            public int Cnt;
            public Pair(string key)
            {
                this.Key = key;
                Cnt = 1;
            }
        }

        class PairObj
        {
            public readonly object Key;
            public int Cnt;
            public readonly SpinLockRef sLock;
            public PairObj(object key)
            {
                this.Key = key;
                Cnt = 1;
                sLock = new SpinLockRef();
            }

            public void EnterLock(ref bool flag)
            {
                flag = false;
                if (!sLock.IsHeldByCurrentThread)
                {                    
                    sLock.Enter(ref flag);
                }
            }

            public void ExitLock(bool flag)
            {
                if (flag)
                    sLock.Exit();
            }
        }

        private static Dictionary<String, Pair> _pool = new Dictionary<String, Pair>();
        private static SpinLockRef _poolLock = new SpinLockRef();
        private static Dictionary<object, PairObj> _poolObj = new Dictionary<object, PairObj>();
        private static SpinLockRef _poolObjLock = new SpinLockRef();

        public static IDisposable AcquireDynamicLock(String key)
        {
            return new DynamicLock(key);
        }

        public static IDisposable AcquireDynamicLockSlim(object key)
        {
            return new DynamicLockSlim(key);
        }

        public static IDisposable AcquireDynamicLock_Debug(String key, string dir)
        {
            return new CSScopeMgr_DebugWithStack4Strings(key, dir);
        }

        public static void Lock(String key)
        {
            Pair result;
            using (new CSScopeMgrLite(_poolLock))
            {
                if (_pool.TryGetValue(key, out result))
                {
                    _pool[key].Cnt++;
                }
                else
                {
                    result = new Pair(key);
                    _pool.Add(key, result);
                }
            }

            Monitor.Enter(result.Key);
        }

        public static void Unlock(String key)
        {
            Pair result;
            using (new CSScopeMgrLite(_poolLock))
            {
                if (!_pool.TryGetValue(key, out result))
                {
                    throw new ArgumentException("key");
                }
            }

            Monitor.Exit(result.Key);

            using (new CSScopeMgrLite(_poolLock))
            {
                if (--result.Cnt == 0)
                {
                    _pool.Remove(key);
                }
            }
        }

        public static void LockSlim(object lockObject, ref bool flag)
        {
            PairObj result;
            using(new CSScopeMgrLite(_poolObjLock))
            {
                if (_poolObj.TryGetValue(lockObject, out result))
                {
                    _poolObj[lockObject].Cnt++;
                }
                else
                {
                    result = new PairObj(lockObject);
                    _poolObj.Add(lockObject, result);
                }
            }

            result.EnterLock(ref flag);
        }

        public static void UnlockSlim(object key, bool flag)
        {
            PairObj result;
            using (new CSScopeMgrLite(_poolObjLock))
            {
                if (!_poolObj.TryGetValue(key, out result))
                {
                    throw new ArgumentException("key");
                }
            }

            result.ExitLock(flag);

            using (new CSScopeMgrLite(_poolObjLock))
            {
                if (--result.Cnt == 0)
                {
                    _poolObj.Remove(key);
                }
            }
        }
    }   //SyncHelper

    public class DynamicLock : IDisposable
    {
        private String _key;

        internal DynamicLock(String key)
        {
            SyncHelper.Lock(_key = key);
        }

        public void Dispose()
        {
            SyncHelper.Unlock(_key);
        }
    }   //DynamicLock

    public class DynamicLockSlim : IDisposable
    {
        private object _key;
        private bool _flag;
        internal DynamicLockSlim(object lockObject)
        {
            _flag = false;
            SyncHelper.LockSlim(_key = lockObject, ref _flag);
        }

        public void Dispose()
        {
            SyncHelper.UnlockSlim(_key, _flag);
        }
    }   //DynamicLock
}
