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



namespace TestConsole
{
    class Program
    {
        private static AdoProvider adoProvider;
        static AdoEFProvider adoEFProvider;

        static SQLGenerator _schema;
        static WormProvider wormProvider;

        static DSTestTime dsTestTime = new DSTestTime();
        static HiPerfTimer performer = new HiPerfTimer();
        protected static int[] smallUserIds = new int[Constants.Small];
        protected static int[] mediumUserIds = new int[Constants.Medium];
        protected static int[] largeUserIds = new int[Constants.Large];
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

        static void Main(string[] args)
        {
            SetDataDirectory();
            string connectionString = ConfigurationManager.ConnectionStrings["EntitiesConnection"].ToString();
            BaseEntityConnection = new EntityConnection(connectionString);
            BaseEntityConnection.Open();
            BaseSqlConnection = (SqlConnection)BaseEntityConnection.StoreConnection;

            adoEFProvider = new AdoEFProvider(ConfigurationManager.ConnectionStrings["TestDAEntities"].ToString());
            adoProvider = new AdoProvider(BaseSqlConnection);
            using (OrmDBManager manager = new OrmDBManager(GetCache(), GetSchema(), ConfigurationSettings.AppSettings["ConnectionStringBase"]))
            {
                wormProvider = new WormProvider(manager);
            }
            wormProvider.Manager = new OrmDBManager(GetCache(), GetSchema(), ConfigurationSettings.AppSettings["ConnectionStringBase"]);

            cfg = new NHibernate.Cfg.Configuration();
            cfg.AddAssembly("DANHibernate");
            factory = cfg.BuildSessionFactory();

            InitUserIds();

            //TypeCycleWithLoad();
            //Console.WriteLine(performer.Duration.ToString() + " TypeCycleWithLoad");

            //TypeCycleWithLoadLinq();
            //Console.WriteLine(performer.Duration.ToString() + " TypeCycleWithLoadLinq");

            //TypeCycleWithLoadWorm();
            //Console.WriteLine(performer.Duration.ToString() + " TypeCycleWithLoadWorm");

            //LargeCollectionWithChildrenByIdArrayDataset();
            //Console.WriteLine(performer.Duration.ToString() + " LargeCollectionWithChildrenByIdArrayDataset");
            
            //LargeCollectionWithChildrenByIdArrayWorm();
            //Console.WriteLine(performer.Duration.ToString() + " LargeCollectionWithChildrenByIdArrayWorm");

            //LargeCollectionWithChildrenByIdArrayNH();
            //Console.WriteLine(performer.Duration.ToString() + " LargeCollectionWithChildrenByIdArrayNH");

           /// FFF1();
           // Console.WriteLine(performer.Duration.ToString() + " FFF");
           // ZZZ1();
           // Console.WriteLine(performer.Duration.ToString() + " ZZZ");
            LargeCollectionDataset();
            Console.WriteLine(performer.Duration.ToString() + " ZZZ");
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

        [QueryTypeAttribute(QueryType.SmallCollection)]
        public static void FFF1()
        {
            performer.Start();
            for (ulong i = 0; i < 1000000; i++)
            {
            }
            performer.Stop();
        }

        [QueryTypeAttribute(QueryType.SmallCollection)]
        public static void ZZZ1()
        {
            performer.Start();
            for (ulong i = 0; i < 1000000; i++)
            {
            }
            performer.Stop();
        }

        public static void LargeCollectionDataset()
        {
            performer.Start();
            adoProvider.CollectionDataset(Constants.Large);
            performer.Stop();
        }

        public static void TypeCycleWithLoad()
        {
            performer.Start();
            adoProvider.TypeCycleWithLoadDataset(mediumUserIds);
            performer.Stop();
        }

        public static void TypeCycleWithLoadLinq()
        {
            performer.Start();
            adoEFProvider.TypeCycleWithLoadLinq(mediumUserIds);
            performer.Stop();
        }

        public static void TypeCycleWithLoadWorm()
        {
            performer.Start();
            wormProvider.TypeCycleWithLoad(mediumUserIds);
            performer.Stop();
        }

        public static void LargeCollectionWithChildrenByIdArrayDataset()
        {
            performer.Start();
            adoProvider.CollectionWithChildrenByIdArrayDataset(largeUserIds);
            performer.Stop();
        }

        public static void LargeCollectionWithChildrenByIdArrayWorm()
        {
            performer.Start();
            wormProvider.CollectionWithChildrenByIdArray(largeUserIds);
            performer.Stop();
        }

        public static void LargeCollectionWithChildrenByIdArrayNH()
        {
            session = factory.OpenSession();
            performer.Start();
            IList users = session.CreateCriteria(typeof(FullUser))
              .Add(Expression.In("UserId", largeUserIds)).List();
            performer.Stop();
            session.Close();
        }
    }
}
