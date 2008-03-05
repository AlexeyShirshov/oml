using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.LoadTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProject;

namespace Tests
{
    public abstract class TestBase
    {
        static DSTime dsTime = new DSTime();
        static ReportCreator reporter;
        static HiPerfTimer performer = new HiPerfTimer();
        protected static TestContext context;
        protected static Type classType;
        static DSTime.ClassRow currentRow;

        [ClassInitialize()]
        public static void ClassInit()
        {
            Utils.SetDataDirectory();
            currentRow = dsTime.Class.AddClassRow(classType.Name);
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
            string group = Regex.Replace(context.TestName, "(Select)(Collection)?.+(With)(out)?(Load)", "$1 $2 $3$4 $5");
            dsTime.Test.AddTestRow(currentRow, context.TestName, performer.Duration, group);
        }

        [AssemblyCleanup()]
        public static void Clean()
        {
            ReportCreator.Write(dsTime);
        }
    }
}
