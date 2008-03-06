using System;
using System.Data;
using System.Text;
using System.Collections.Generic;
using DANHibernate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using NHibernate.Cfg;
namespace Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestNHibernate : TestBase
    {
        Configuration cfg = new Configuration();
        ISessionFactory factory;
        ISession session;

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        public TestContext TestContext
        {
            get { return context; }
            set { context = value; }
        }

        static TestNHibernate()
        {
            TestBase.classType = typeof(TestNHibernate);
        }

        [TestInitialize]
        public void TestInit()
        {
            cfg.AddAssembly("DANHibernate");
            factory = cfg.BuildSessionFactory();
            session = factory.OpenSession();
        }


        [TestCleanup]
        public void TestClean()
        {
            session.Close();
        }


        [TestMethod]
		public void SelectWithLoad() 
        {
		    User user  = (User)session.Load(typeof(User), 1);
		}

        [TestMethod]
        public void SelectCollectionWithLoad()
        {
            object recentUsers = session.CreateCriteria(typeof(User)).List();
        }

        [TestMethod]
        public void SelectSmallWithLoad()
        {
            Phone phone = (Phone)session.Load(typeof(Phone), 1);
        }

        [TestMethod]
        public void SelectCollectionSmallWithLoad()
        {
            object recentPhones = session.CreateCriteria(typeof(Phone)).List();
        }

        [TestMethod]
        public void LazySelectWithoutLoad()
        {
            LazyUser user = (LazyUser)session.Load(typeof(LazyUser), 1);
        }

        [TestMethod]
        public void LazySelectWithLoad()
        {
            LazyUser user = (LazyUser)session.Load(typeof(LazyUser), 1);
            string result = string.Format("{0} {1} {2}", user.UserId, user.FirstName, user.LastName);
        }

        [TestMethod]
        public void LazySelectCollectionWithoutLoad()
        {
            object recentUsers = session.CreateCriteria(typeof(LazyUser)).List();
        }

        [TestMethod]
        public void LazySelectCollectionWithLoad()
        {
            System.Collections.IList recentUsers = session.CreateCriteria(typeof(LazyUser)).List();
            foreach (LazyUser user in recentUsers)
            {
                string result = string.Format("{0} {1} {2}", user.UserId, user.FirstName, user.LastName);
            }
        }

        [TestMethod]
        public void LazySelectSmallWithoutLoad()
        {
            LazyPhone phone = (LazyPhone)session.Load(typeof(LazyPhone), 1);
        }

        [TestMethod]
        public void LazySelectSmallWithLoad()
        {
            LazyPhone phone = (LazyPhone)session.Load(typeof(LazyPhone), 1);
            string result = string.Format("{0} {1} {2}", phone.PhoneId, phone.UserId, phone.PhoneNumber);
        }

        [TestMethod]
        public void LazySelectCollectionSmallWithoutLoad()
        {
            System.Collections.IList recentPhones = session.CreateCriteria(typeof(LazyPhone)).List();
        }

        [TestMethod]
        public void LazySelectCollectionSmallWithLoad()
        {
            System.Collections.IList recentPhones = session.CreateCriteria(typeof(LazyPhone)).List();
            foreach (LazyPhone phone in recentPhones)
            {
                string result = string.Format("{0} {1} {2}", phone.PhoneId, phone.UserId, phone.PhoneNumber);
            }
        }

    }
}
