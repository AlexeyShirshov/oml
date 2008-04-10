using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Runnable
{
    public delegate void RunnableFunc();

    public class RunningInfo
    {
        private MulticastDelegate funcDelegate;
        private QueryType funcType;

        public MulticastDelegate FuncDelegate { get { return funcDelegate; } }
        public QueryType FuncType { get { return funcType; } }

        public RunningInfo(RunnableFunc func, QueryType type)
        {
            this.funcDelegate = func;
            this.funcType = type;
        }
    }

    public interface IRunnable
    {
        IList<RunningInfo> FuncCollection { get; }
    }
}
