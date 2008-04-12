using System;
using System.Collections.Generic;
using System.Text;

namespace CoreFramework.Debuging
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
            System.Diagnostics.Debug.WriteLine(_name + ": " + DateTime.Now.Subtract(_t).TotalSeconds);
        }

        #endregion
    }
}
