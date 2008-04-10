using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Runnable;

namespace TestConsole
{

    public class C : IRunnable
    {

        public IList<RunningInfo> FuncCollection
        {
            get { return funcCollection; }
        }
        private List<RunningInfo> funcCollection;
        
        public C()
        {
            funcCollection = new List<RunningInfo>();
            RunningInfo runningInfo = new RunningInfo(new RunnableFunc(Func), QueryType.LargeCollection);
            funcCollection.Add(runningInfo);
        }
        public void Func() { Console.WriteLine("lalala"); }
    }

    
    class Runner
    {
        C c = new C();
    }
}
