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
    class TestRunner
    {
        SQLGenerator _schema;

        DSTestTime dsTestTime;
        HiPerfTimer performer;
        int[] smallUserIds;
        int[] mediumUserIds;
        int[] largeUserIds;
        public EntityConnection BaseEntityConnection;
        public SqlConnection BaseSqlConnection;

        NHibernate.Cfg.Configuration cfg;
        ISessionFactory factory;
        ISession session;

        public TestRunner()
        {
            performer = new HiPerfTimer();
            dsTestTime = new DSTestTime();
        }

        protected SQLGenerator GetSchema()
        {
            if (_schema == null)
                _schema = new SQLGenerator("1");
            return _schema;
        }

        protected OrmCache GetCache()
        {
            return new OrmCache();
        }

        public void SetDataDirectory()
        {
            string _executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.GetFullPath(_executingPath.Substring(6) + @"..\..\..\..\DB"));
        }

        private void InitConnections()
        {
            SetDataDirectory();
            string connectionString = ConfigurationManager.ConnectionStrings["TestDAEntities"].ToString();
            BaseEntityConnection = new EntityConnection(connectionString);
            //BaseEntityConnection.Open();
            BaseSqlConnection = (SqlConnection)BaseEntityConnection.StoreConnection;
        }

        private void InitUserIds()
        {
            GetIdsArray(Constants.Small, smallUserIds);
            GetIdsArray(Constants.Medium, mediumUserIds);
            GetIdsArray(Constants.Large, largeUserIds);
        }

        private void GetIdsArray(int count, int[] idsArray)
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

        private void Run(string providerName, HiPerfTimer performer, DSTestTime dsTestTime, object provider)
        {
            Run(providerName, performer, dsTestTime, provider, null, null);
        }

        private void Run(string providerName, HiPerfTimer performer, DSTestTime dsTestTime,
            object provider, RunnableFunc beforeRun, RunnableFunc afterRun)
        {
            Console.WriteLine("Run " + providerName);
            try
            {
                IEnumerable<MethodInfo> methodInfos = provider.GetType().GetMethods()
                    .Where(m => m.GetCustomAttributes(typeof(QueryTypeAttribute), false).Count() > 0);
                foreach (MethodInfo methodInfo in methodInfos)
                {
                    Console.Write(methodInfo.Name + " ... ");
                    if (beforeRun != null) { beforeRun.Invoke(); }
                    performer.Start();
                    methodInfo.Invoke(provider, new object[] { });
                    performer.Stop();
                    if (afterRun != null) { afterRun.Invoke(); }
                    object[] attrs = methodInfo.GetCustomAttributes(typeof(QueryTypeAttribute), false);
                    QueryTypeAttribute attr = (QueryTypeAttribute)attrs[0];
                    string typeInfo = TypeInfo.Types[attr.QueryType];
                    double duration = Math.Round(performer.Duration, 3);
                    Console.Write(duration + "\n");

                    dsTestTime.Time.AddTimeRow(providerName, attr.QueryType.ToString(),
                        methodInfo.Name, duration, typeInfo, attr.SyntaxType.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
            finally
            {
                Console.WriteLine("End " + providerName);
                Console.WriteLine("");
            }
        }

        public void RunAdo()
        {
            BaseSqlConnection.Open();
            AdoProvider adoProvider = new AdoProvider(BaseSqlConnection, smallUserIds, mediumUserIds, largeUserIds);
            Run("ADO", performer, dsTestTime, adoProvider);
            BaseSqlConnection.Close();
        }

        public void RunNHibernate()
        {
            NHibernateProvider nHibProvider = new NHibernateProvider(smallUserIds, mediumUserIds, largeUserIds);
            RunnableFunc beforeFunc = new RunnableFunc(nHibProvider.OpenSession);
            RunnableFunc afterFunc = new RunnableFunc(nHibProvider.CloseSession);
            Run("NHibernate", performer, dsTestTime, nHibProvider, beforeFunc, afterFunc);
        }

        public void RunLinq()
        {
            BaseSqlConnection.Open();
            LinqProvider linqProvider = new LinqProvider(BaseSqlConnection, smallUserIds, mediumUserIds, largeUserIds);
            RunnableFunc beforeFunc = new RunnableFunc(linqProvider.CreateNewDatabaseDataContext);
            Run("Linq", performer, dsTestTime, linqProvider, beforeFunc, null);
            BaseSqlConnection.Close();
        }

        public void RunAdoEF()
        {
            BaseEntityConnection.Open();
            AdoEFProvider adoEFProvider = new AdoEFProvider(BaseEntityConnection, smallUserIds, mediumUserIds, largeUserIds);
            Run("ADO EF", performer, dsTestTime, adoEFProvider);
            BaseEntityConnection.Close();
        }

        public void RunWorm()
        {
            WormProvider wormProvider = new WormProvider(smallUserIds, mediumUserIds, largeUserIds);
            RunnableFunc beforeFunc = new RunnableFunc(wormProvider.SetDefaultContext);
            RunnableFunc afterFunc = new RunnableFunc(wormProvider.ClearContext);
            Run("Worm", performer, dsTestTime, wormProvider, beforeFunc, afterFunc);
        }

        public void Start()
        {
            dsTestTime.Clear();
            smallUserIds = new int[Constants.Small];
            mediumUserIds = new int[Constants.Medium];
            largeUserIds = new int[Constants.Large];

            InitConnections();
            InitUserIds();
        }

        public void End()
        {
            ReportCreator.Write(dsTestTime);
            BaseEntityConnection.Close();
            BaseEntityConnection.Dispose();
        }
    }
}
