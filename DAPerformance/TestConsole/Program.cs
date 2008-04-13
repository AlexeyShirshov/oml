using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.EntityClient;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using DAAdo;
using DAWorm;
using DaAdoEF;
using DALinq;
using Common;
using Tests;

using Worm.Cache;
using Worm.Database;
using Worm.Orm;

using DANHibernate;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Hql;
using NHibernate.Expression;
using NHibernate.Cfg;
using Common.Runnable;

namespace TestConsole
{
    class Program
    {
        static SQLGenerator _schema;
        static WormProvider wormProvider;

        static DSTestTime dsTestTime = new DSTestTime();
        static HiPerfTimer performer = new HiPerfTimer();
        static int[] smallUserIds = new int[Constants.Small];
        static int[] mediumUserIds = new int[Constants.Medium];
        static int[] largeUserIds = new int[Constants.Large];
        public static  EntityConnection BaseEntityConnection;
        public static  SqlConnection BaseSqlConnection;

        static NHibernate.Cfg.Configuration cfg;
        static ISessionFactory factory;
        static ISession session;


        protected static SQLGenerator GetSchema()
        {
            if (_schema == null)
                _schema = new SQLGenerator("1");
            return _schema;
        }

        protected static OrmCache GetCache()
        {
            return new OrmCache();
        }

        public static void SetDataDirectory()
        {
            string _executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.GetFullPath(_executingPath.Substring(6) + @"..\..\..\..\DB"));
        }

        private static void InitConnections()
        {
            SetDataDirectory();
            string connectionString = ConfigurationManager.ConnectionStrings["TestDAEntities"].ToString();
            BaseEntityConnection = new EntityConnection(connectionString);
            //BaseEntityConnection.Open();
            BaseSqlConnection = (SqlConnection)BaseEntityConnection.StoreConnection;
        }

        static void Main(string[] args)
        {
            InitConnections();
            InitUserIds();
            HiPerfTimer performer = new HiPerfTimer();
            DSTestTime dsTestTime = new DSTestTime();

            BaseSqlConnection.Open();
            AdoProvider adoProvider = new AdoProvider(BaseSqlConnection, smallUserIds, mediumUserIds, largeUserIds);
            Run("ADO", performer, dsTestTime, adoProvider);
            BaseSqlConnection.Close();

            BaseSqlConnection.Open();
            LinqProvider linqProvider = new LinqProvider(BaseSqlConnection, smallUserIds, mediumUserIds, largeUserIds);
            RunLinq("Linq", performer, dsTestTime, linqProvider);
            BaseSqlConnection.Close();

            BaseEntityConnection.Open();
            AdoEFProvider adoEFProvider = new AdoEFProvider(BaseEntityConnection, smallUserIds, mediumUserIds, largeUserIds);
            Run("ADO EF", performer, dsTestTime, adoEFProvider);
            BaseEntityConnection.Close();

            //NHibernateProvider nHibProvider = new NHibernateProvider(smallUserIds, mediumUserIds, largeUserIds);
            //RunnableFunc beforeFunc = new RunnableFunc(nHibProvider.OpenSession);
            //RunnableFunc afterFunc = new RunnableFunc(nHibProvider.CloseSession);
            //Run("NHibernate", performer, dsTestTime, nHibProvider, beforeFunc, afterFunc);

            WormProvider wormProvider = new WormProvider(smallUserIds, mediumUserIds, largeUserIds);
            RunWorm("Worm", performer, dsTestTime, wormProvider);

            ReportCreator.Write(dsTestTime);
            BaseEntityConnection.Close();
            BaseEntityConnection.Dispose();
        }

        private static void Run(string providerName, HiPerfTimer performer, DSTestTime dsTestTime, 
            object provider, RunnableFunc beforeRun, RunnableFunc afterRun)
        {
            IEnumerable<MethodInfo> methodInfos = provider.GetType().GetMethods()
                .Where(m => m.GetCustomAttributes(false).Count() > 0);
            foreach (MethodInfo methodInfo in methodInfos)
            {
                if (beforeRun != null) { beforeRun.Invoke(); }
                performer.Start();
                methodInfo.Invoke(provider, new object[] { });
                performer.Stop();
                if (afterRun != null) { afterRun.Invoke(); }
                object[] attrs = methodInfo.GetCustomAttributes(typeof(QueryTypeAttribute), false);
                QueryTypeAttribute attr = (QueryTypeAttribute)attrs[0];
                string typeInfo = TypeInfo.Types[attr.QueryType];
                dsTestTime.Time.AddTimeRow(providerName, attr.QueryType.ToString(),
                    methodInfo.Name, performer.Duration, typeInfo, attr.SyntaxType.ToString());

            }
        }

        private static void Run(string providerName, HiPerfTimer performer, DSTestTime dsTestTime, object provider)
        {
            Run(providerName, performer, dsTestTime, provider, null, null);
        }

        private static void RunLinq(string providerName, HiPerfTimer performer, DSTestTime dsTestTime, object provider)
        {
            IEnumerable<MethodInfo> methodInfos = provider.GetType().GetMethods()
                .Where(m => m.GetCustomAttributes(false).Count() > 0);
            foreach (MethodInfo methodInfo in methodInfos)
            {
                ((LinqProvider)provider).CreateNewDatabaseDataContext();
                performer.Start();
                methodInfo.Invoke(provider, new object[] { });
                performer.Stop();
                object[] attrs = methodInfo.GetCustomAttributes(typeof(QueryTypeAttribute), false);
                QueryTypeAttribute attr = (QueryTypeAttribute)attrs[0];
                string typeInfo = TypeInfo.Types[attr.QueryType];
                dsTestTime.Time.AddTimeRow(providerName, attr.QueryType.ToString(),
                    methodInfo.Name, performer.Duration, typeInfo, attr.SyntaxType.ToString());

            }
        }

        private static void RunWorm(string providerName, HiPerfTimer performer, DSTestTime dsTestTime, object provider)
        {
            IEnumerable<MethodInfo> methodInfos = provider.GetType().GetMethods()
                .Where(m => m.GetCustomAttributes(false).Count() > 0);
            foreach (MethodInfo methodInfo in methodInfos)
            {
                ((WormProvider)provider).SetDefaultContext();
                performer.Start();
                methodInfo.Invoke(provider, new object[] { });
                performer.Stop();
                ((WormProvider)provider).ClearContext();
                object[] attrs = methodInfo.GetCustomAttributes(typeof(QueryTypeAttribute), false);
                QueryTypeAttribute attr = (QueryTypeAttribute)attrs[0];
                string typeInfo = TypeInfo.Types[attr.QueryType];
                dsTestTime.Time.AddTimeRow(providerName, attr.QueryType.ToString(),
                    methodInfo.Name, performer.Duration, typeInfo, attr.SyntaxType.ToString());

            }
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
    }
}
