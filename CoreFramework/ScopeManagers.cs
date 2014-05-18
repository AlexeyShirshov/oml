using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CoreFramework.Threading
{
    public class RWScopeMgr : IDisposable
    {
        protected bool _reader;
        protected ReaderWriterLockSlim _rw;
        private Boolean disposedValue = false;        // To detect redundant calls
        /// <summary>
        /// Acquare lock.
        /// </summary>
        /// <param name="reader">If true, acquares not upgradable reader lock</param>
        /// <param name="rw"></param>
        public RWScopeMgr(bool reader, ReaderWriterLockSlim rw) : 
            this(reader, rw, false)
        {
        }

        public RWScopeMgr(bool reader, ReaderWriterLockSlim rw, bool upgradable)
        {
            _reader = reader;
            _rw = rw;
            if (reader)
            {
                if (upgradable)
                    rw.EnterUpgradeableReadLock();
                else
                    rw.EnterReadLock();
            }
            else
            {
                rw.EnterWriteLock();
            }
        }

        protected virtual void _Dispose()
        {
            if (!this.disposedValue)
            {
                if (_rw != null)
                {
                    if (_rw.IsReadLockHeld)
                    {
                        _rw.ExitReadLock();
                    }
                    else if (_rw.IsUpgradeableReadLockHeld)
                    {
                        _rw.ExitUpgradeableReadLock();
                    }
                    else
                    {
                        _rw.ExitWriteLock();
                    }
                    _rw = null;
                }
            }
            this.disposedValue = true;
        }

        public static RWScopeMgr AcquareReaderLock(ReaderWriterLockSlim rw)
        {
            return new RWScopeMgr(true, rw);
        }

        public static RWScopeMgr AcquareWriterLock(ReaderWriterLockSlim rw)
        {
            return new RWScopeMgr(false, rw);
        }
        public static RWScopeMgr AcquareUpgradableReaderLock(ReaderWriterLockSlim rw)
        {
            return new RWScopeMgr(true, rw, true);
        }

        public void Dispose()
        {
            _Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class CSScopeMgr : IDisposable
    {
        private object _lock_obj;
        private bool _flag;
        public CSScopeMgr(object lock_obj)
        {
            _flag = false;
            System.Threading.Monitor.Enter(lock_obj, ref _flag);
            _lock_obj = lock_obj;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_flag)
                System.Threading.Monitor.Exit(_lock_obj);
        }

        #endregion
    }

    public class SpinLockRef
    {
        private SpinLock _sl = new SpinLock();

        public bool IsHeldByCurrentThread
        {
            get
            {
                return _sl.IsHeldByCurrentThread;
            }
        }

        public bool IsHeld
        {
            get
            {
                return _sl.IsHeld;
            }
        }

        public void Enter(ref bool lockTaken)
        {
            _sl.Enter(ref lockTaken);
        }

        public void Exit()
        {
            _sl.Exit();
        }
    }

    public class CSScopeMgrLite : IDisposable
    {
        private SpinLockRef _sl;
        private bool _flag;
        public CSScopeMgrLite(SpinLockRef sl)
        {
            _sl = sl;
            _flag = false;
            if (!_sl.IsHeldByCurrentThread)
            {
                //bool flag = false;
                _sl.Enter(ref _flag);
                //_flag = flag;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_flag)
                _sl.Exit();
        }

        #endregion
    }

    public class BlankSyncHelper : IDisposable
    {
        public BlankSyncHelper(object lock_obj)
        {
        }

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion
    }

    public class CSScopeMgr_Debug : IDisposable
    {
        private object _lock_obj;
        private int _tick;

        //protected static System.Xml.XmlDocument _xdoc;
        //private bool _first;
        private bool _closeWriter;
        private string _fn;

        [ThreadStatic]
        private static System.Xml.XmlTextWriter _wr;
        [ThreadStatic]
        private static System.IO.TextWriter _tw;
        [ThreadStatic]
        private static System.Text.StringBuilder _sb;
        [ThreadStatic]
        private static int _cnt;

        public CSScopeMgr_Debug(object lock_obj, string dir)
        {
            Lock(lock_obj);
            _lock_obj = lock_obj;
            _tick = Environment.TickCount;
            _fn = dir + "cs-scope-" + System.Diagnostics.Process.GetCurrentProcess().Id.ToString() + ".xml";
            //lock (typeof(CSScopeMgr_Debug))
            //{
            //    if (_xdoc == null)
            //    {
            //        _first = true;
            //    }
            //}
            if (_sb == null)
            {
                _sb = new StringBuilder();
                _tw = new System.IO.StringWriter(_sb);
                _wr = new System.Xml.XmlTextWriter(_tw);
                _closeWriter = true;
            }
            WriteStart(_wr);
        }

        protected virtual void WriteStart(System.Xml.XmlTextWriter wr)
        {
            _cnt++;
            wr.WriteStartElement("l");
            wr.WriteAttributeString("tick", _tick.ToString());
            wr.WriteAttributeString("object", GetLockString(_lock_obj));
            System.Diagnostics.Debug.WriteLine(string.Format("thread[{2}]{0}: {1}", _cnt, GetLockString(_lock_obj),_sb.GetHashCode()));
        }

        protected virtual void Lock(object lock_obj)
        {
            System.Threading.Monitor.Enter(lock_obj);
        }

        protected virtual void Unlock(object lock_obj)
        {
            System.Threading.Monitor.Exit(lock_obj);
        }

        protected virtual string GetLockString(object lock_obj)
        {
            return lock_obj.GetHashCode().ToString();
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_closeWriter)
            {
                _wr.Close();
                string str = _sb.ToString();
                _wr = null;
                _tw = null;
                _sb = null;
                if (_cnt > 1)
                {
                    lock (this.GetType())
                    {
                        System.Xml.XmlDocument xdoc = null;
                        bool cl = xdoc == null;
                        if (cl)
                        {
                            xdoc = new System.Xml.XmlDocument();
                            if (!System.IO.File.Exists(_fn))
                            {
                                System.Xml.XmlElement root = xdoc.CreateElement("root");
                                xdoc.AppendChild(root);
                            }
                            else
                                xdoc.Load(_fn);
                        }
                        System.Xml.XmlDocumentFragment e = xdoc.CreateDocumentFragment();
                        e.InnerXml = str;
                        xdoc.DocumentElement.AppendChild(e);
                        if (cl)
                        {
                            xdoc.Save(_fn);
                            xdoc = null;
                        }
                    }
                }
                _cnt = 0;
            }
            else
                _wr.WriteEndElement();

            //lock (typeof(CSScopeMgr_Debug))
            //{
            //    if (_first)
            //    {
            //        _xdoc.Save(_fn);
            //        _xdoc = null;
            //    }
            //}

            Unlock(_lock_obj);
        }

        #endregion
    }

    public class CSScopeMgr_DebugWithStack : CSScopeMgr_Debug
    {
        public CSScopeMgr_DebugWithStack(object lock_obj, string dir)
            : base(lock_obj, dir)
        {
        }

        protected override void WriteStart(System.Xml.XmlTextWriter wr)
        {
            base.WriteStart(wr);
            wr.WriteStartElement("stack");
            try
            {
                wr.WriteCData(Environment.StackTrace);
            }
            finally
            {
                wr.WriteEndElement();   //stack
            }
        }
    }

    class CSScopeMgr_DebugWithStack4Strings : CSScopeMgr_DebugWithStack
    {
        public CSScopeMgr_DebugWithStack4Strings(string lock_obj, string dir)
            : base(lock_obj, dir)
        {
        }

        protected override void Lock(object lock_obj)
        {
            SyncHelper.Lock((string)lock_obj);
        }

        protected override void Unlock(object lock_obj)
        {
            SyncHelper.Unlock((string)lock_obj);
        }

        protected override string GetLockString(object lock_obj)
        {
            return (string)lock_obj;
        }
    }
}
