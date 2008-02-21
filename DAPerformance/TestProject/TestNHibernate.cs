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
    public class TestNHibernate
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

        public TestNHibernate()
        {
            Utils.SetDataDirectory();
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
		public void TestSelect() 
        {
		    User user  = (User)session.Load(typeof(User), 1);
            object recentUsers = session.CreateCriteria(typeof(User)).List();
		}
    }
}
