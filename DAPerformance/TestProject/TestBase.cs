using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProject;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Helper;

namespace Tests
{
    public abstract class TestBase
    {
        static DSTestTime dsTestTime = new DSTestTime();
        static ReportCreator reporter;
        static HiPerfTimer performer = new HiPerfTimer();
        protected static TestContext context;
        protected static Type classType;
        protected static int[] smallUserIds = new int[Constants.Small];
        protected static int[] mediumUserIds = new int[Constants.Medium];
        protected static int[] largeUserIds = new int[Constants.Large];
        public static SqlConnection conn;
        private static void InitUserIds()
        {
            DataSet ds = new DataSet();
            string connectionString = ConfigurationManager.AppSettings["ConnectionStringBase"];
            conn = new SqlConnection(connectionString);            
            conn.Open();

            GetIdsArray(Constants.Small, smallUserIds);
            GetIdsArray(Constants.Medium, mediumUserIds);
            GetIdsArray(Constants.Large, largeUserIds);
        }

        private static void GetIdsArray(int count, int[] idsArray)
        {
            DataSet ds = new DataSet();
            SqlCommand command = new SqlCommand("select TOP " + count + " user_id from tbl_user", conn);
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            adapter.Fill(ds);
            for (int i = 0; i < count; i++)
            {
                DataRow row = ds.Tables[0].Rows[i];
                idsArray[i] = (int)row["user_id"];
            }
        }

        static TestBase() {
            Utils.SetDataDirectory();
            InitUserIds();
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
            QueryTypeAttribute attribute = Assembly.GetExecutingAssembly().GetType(classType.ToString())
                .GetMethod(context.TestName).GetCustomAttributes(typeof(Helper.QueryTypeAttribute), false)[0] as QueryTypeAttribute;

            QueryType queryType = attribute.QueryType;
            string typeInfo = TypeInfo.Types[queryType];
            dsTestTime.Time.AddTimeRow(classType.Name, queryType.ToString(), context.TestName, performer.Duration);
        }
        
        [AssemblyCleanup()]
        public static void Clean()
        {
            ReportCreator.Write(dsTestTime);
            conn.Close();
            conn.Dispose();
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
