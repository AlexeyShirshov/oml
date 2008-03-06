using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Reflection;
using System.Text;

using DAAdo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestAdo : TestBase
    {
        static AdoProvider adoProvider = new AdoProvider(ConfigurationSettings.AppSettings["connectionString"]);

        public TestContext TestContext
        {
            get { return context; }
            set { context = value; }
        }

        static TestAdo()
        {
            TestBase.classType = typeof(TestAdo);
        }

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

        [TestMethod]
        public void SelectWithLoad()
        {
            DataSet ds = adoProvider.Select();
            Assert.AreEqual(1, ds.Tables.Count);
        }

        [TestMethod]
        public void SelectCollectionWithLoad()
        {
           adoProvider.SelectCollection();
        }

        [TestMethod]
        public void SelectWithReaderWithLoad()
        {
            adoProvider.SelectWithReader();
        }

        [TestMethod]
        public void SelectCollectionWithReaderWithLoad()
        {
            adoProvider.SelectCollectionWithReader();
        }

        [TestMethod]
        public void SelectSmallWithLoad()
        {
            DataSet ds = adoProvider.SelectSmall();
            Assert.AreEqual(1, ds.Tables.Count);
        }

        [TestMethod]
        public void SelectCollectionSmallWithLoad()
        {
            DataSet ds = adoProvider.SelectCollectionSmall();
            Assert.AreEqual(1, ds.Tables.Count);
        }

       

        [TestMethod]
        public void SelectSmallWithReaderWithLoad()
        {
            adoProvider.SelectSmallWithReader();
        }

        [TestMethod]
        public void SelectCollectionSmallWithReaderWithLoad()
        {
            adoProvider.SelectCollectionSmallWithReader();
        }
    }
}
