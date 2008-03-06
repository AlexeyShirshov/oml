using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProject;

namespace Tests
{
    public abstract class TestBase
    {
        static DSTestTime dsTestTime = new DSTestTime();
        static ReportCreator reporter;
        static HiPerfTimer performer = new HiPerfTimer();
        protected static TestContext context;
        protected static Type classType;

        public TestBase()
        {
            Utils.SetDataDirectory();
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
            string groupName = Regex.Replace(context.TestName, ".*(Select)(Collection)?.*(With)(out)?(Load)", "$1 $2 $3$4 $5").Replace("  ", " ").ToLower();
            dsTestTime.Time.AddTimeRow(classType.Name, groupName, context.TestName, performer.Duration);
        }
        
        [AssemblyCleanup()]
        public static void Clean()
        {
            ReportCreator.Write(dsTestTime);
        }
    }

    [TestClass]
    public class Fake : TestBase
    {
        [AssemblyCleanup()]
        public static void AssemblyCleanup()
        {
            TestBase.Clean();
        }
    }
}
