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
        static DSTime dsTime = new DSTime();
        static ReportCreator reporter;
        static HiPerfTimer performer = new HiPerfTimer();
        protected static TestContext context;
        protected static Type classType;

        public TestBase()
        {
            Utils.SetDataDirectory();
        }

        [ClassInitialize()]
        public static void ClassInit()
        {
         
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
            DSTime.ClassRow classRow = GetClassRow();
            DSTime.GroupRow groupRow = GetGroupRow();
            dsTime.Test.AddTestRow(classRow, context.TestName, performer.Duration, groupRow);
        }

        private DSTime.ClassRow GetClassRow()
        {
            DSTime.ClassRow[] classRows = (DSTime.ClassRow[])dsTime.Class.Select(string.Format("Name='{0}'", classType.Name));
            DSTime.ClassRow classRow;
            if (classRows.Length == 1)
            {
                classRow = dsTime.Class.FindById(classRows[0].Id);
                classRow.Name = classType.Name;
            }
            else
            {
                classRow = dsTime.Class.AddClassRow(classType.Name);
            }
            return classRow;
        }

        private DSTime.GroupRow GetGroupRow()
        {
            string groupName = Regex.Replace(context.TestName, ".*(Select)(Collection)?.*(With)(out)?(Load)", "$1 $2 $3$4 $5").Replace("  ", " ").ToLower();
            DSTime.GroupRow[] groupRows = (DSTime.GroupRow[])dsTime.Group.Select(string.Format("Name='{0}'", groupName));
            DSTime.GroupRow groupRow;
            if (groupRows.Length == 1)
            {
                groupRow = dsTime.Group.FindById(groupRows[0].Id);
                groupRow.Name = groupName;
            }
            else
            {
                groupRow = dsTime.Group.AddGroupRow(groupName);
            }
            return groupRow;
        }

        [AssemblyCleanup()]
        public static void Clean()
        {
            ReportCreator.Write(dsTime);
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
