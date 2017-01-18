using System;
using System.Collections.Generic;
using System.Text;

namespace CoreFramework.Debugging
{
    public class OutputTimer : IDisposable
    {
        private DateTime _t = DateTime.Now;
        private string _name;

        public OutputTimer(string name)
        {
            _name = name;
        }

        #region IDisposable Members

        public void Dispose()
        {
            System.Diagnostics.Trace.WriteLine(string.Format("{0}: {1} s", _name, DateTime.Now.Subtract(_t).TotalSeconds));
        }

        #endregion
    }

    public class HighPerfOutputTimer : IDisposable
    {
        private System.Diagnostics.Stopwatch _pf;
        private string _name;

        public HighPerfOutputTimer(string name)
        {
            _name = name;
            _pf = new System.Diagnostics.Stopwatch();
            _pf.Start();
        }

        #region IDisposable Members

        public void Dispose()
        {
            _pf.Stop();
            System.Diagnostics.Trace.WriteLine(string.Format("{0}: {1} s", _name, _pf.Elapsed.TotalSeconds));
        }

        #endregion
    }
}
