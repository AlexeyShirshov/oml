using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using System.Data;
using System.Data.EntityClient;
using System.Data.SqlClient;
using Common;

namespace Tests
{
    public abstract class TestBase
    {
        static DSTestTime dsTestTime = new DSTestTime();
        static Timer performer = new Timer();
        //static HiPerfTimer performer = new HiPerfTimer();
        protected static TestContext context;
        protected static Type classType;
        protected static int[] smallUserIds = new int[Constants.Small];
        protected static int[] mediumUserIds = new int[Constants.Medium];
        protected static int[] largeUserIds = new int[Constants.Large];
        public static readonly EntityConnection BaseEntityConnection;
        public static readonly SqlConnection BaseSqlConnection;

        static TestBase()
        {
            Utils.SetDataDirectory();
            string connectionString = ConfigurationManager.ConnectionStrings["EntitiesConnection"].ToString();
            BaseEntityConnection = new EntityConnection(connectionString);
            BaseEntityConnection.Open();
            BaseSqlConnection = (SqlConnection)BaseEntityConnection.StoreConnection;
            InitUserIds();
        }

        private static void InitUserIds()
        {
            GetIdsArray(Constants.Small, smallUserIds);
            GetIdsArray(Constants.Medium, mediumUserIds);
            GetIdsArray(Constants.Large, largeUserIds);
        }

        private static void GetIdsArray(int count, int[] idsArray)
        {
            DataSet ds = new DataSet();
            SqlCommand command = new SqlCommand("select TOP " + count + " user_id from tbl_user", BaseSqlConnection);
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            adapter.Fill(ds);
            for (int i = 0; i < count; i++)
            {
                DataRow row = ds.Tables[0].Rows[i];
                idsArray[i] = (int)row["user_id"];
            }
        }

        [TestInitialize]
        public virtual void TestInitialize()
        {
            performer.Start();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.EmptyTest)]
        public void a()
        {
        }

        [TestCleanup]
        public virtual void TestCleanup()
        {
            performer.Stop();
            Type type = Assembly.GetExecutingAssembly().GetType(classType.ToString());
            MethodInfo info = type.GetMethod(context.TestName);
            object[] attributesCollection = info.GetCustomAttributes(typeof(Common.QueryTypeAttribute), false);
            if (attributesCollection.Length != 1)
            {
                throw new Exception("Method with 'UnitTest' attribute must have 'QueryTypeAttribute'");
            }

            QueryTypeAttribute attribute = attributesCollection[0] as QueryTypeAttribute;

            if (attribute.QueryType != QueryType.EmptyTest)
            {
                QueryType queryType = attribute.QueryType;
                string typeInfo = TypeInfo.Types[queryType];

                string syntaxType = attribute.SyntaxType.ToString();
                dsTestTime.Time.AddTimeRow(classType.Name, queryType.ToString(), context.TestName, performer.Duration.Ticks, typeInfo, syntaxType);
            }
        }

        [AssemblyCleanup]
        public static void Clean()
        {
            ReportCreator.Write(dsTestTime);
            BaseEntityConnection.Close();
            BaseEntityConnection.Dispose();
        }
    }

    [TestClass]
    public class Fake : TestBase
    {
        public TestContext TestContext
        {
            get { return context; }
            set { context = value; }
        }

        static Fake()
        {
            TestBase.classType = typeof(Fake);
        }

        [AssemblyCleanup()]
        public static void AssemblyCleanup()
        {
            TestBase.Clean();
        }
    }
}
