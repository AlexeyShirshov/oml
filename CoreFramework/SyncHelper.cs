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

        private static Dictionary<String, Pair> _pool = new Dictionary<String, Pair>();

        public static IDisposable AcquireDynamicLock(String key)
        {
            return new DynamicLock(key);
        }

        public static IDisposable AcquireDynamicLock_Debug(String key, string dir)
        {
            return new CSScopeMgr_DebugWithStack4Strings(key, dir);
        }

        public static void Lock(String key)
        {
            Pair result;
            lock (_pool)
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
            lock (_pool)
            {
                if (!_pool.TryGetValue(key, out result))
                {
                    throw new ArgumentException("key");
                }
            }

            Monitor.Exit(result.Key);

            lock (_pool)
            {
                if (--result.Cnt == 0)
                {
                    _pool.Remove(key);
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
}
