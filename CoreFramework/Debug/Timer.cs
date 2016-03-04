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
        private PerfCounter _pf;
        private string _name;

        public HighPerfOutputTimer(string name)
        {
            _name = name;
            _pf = new PerfCounter();
        }

        #region IDisposable Members

        public void Dispose()
        {
            System.Diagnostics.Trace.WriteLine(string.Format("{0}: {1} s", _name, _pf.GetTime().TotalSeconds));
        }

        #endregion
    }
}
