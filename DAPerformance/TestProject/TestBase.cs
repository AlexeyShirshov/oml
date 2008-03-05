using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProject;

namespace Tests
{
    public abstract class TestBase
    {
        static DSTime dsTime = new DSTime();
        static ReportCreator reporter = new ReportCreator();
        static HiPerfTimer performer = new HiPerfTimer();
        protected static TestContext context;
        DSTime.ClassRow currentRow;

        protected abstract Type ClassType { get; }

        public TestBase()
        {
            currentRow = dsTime.Class.AddClassRow(ClassType.Name);
        }

        [TestInitialize()]
        public void TestTimeInit()
        {            
            performer.Start();
        }

        [TestCleanup()]
        public void TestTimeCleaning()
        {
            performer.Stop();
            dsTime.Test.AddTestRow(currentRow, context.TestName, performer.Duration);
        }

    }
}
