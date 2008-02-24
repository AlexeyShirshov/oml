using System;
using System.Configuration;
using System.Text;
using System.Collections.Generic;
using DAWorm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestWorm
    {
        WormProvider wormProvider = new WormProvider(ConfigurationSettings.AppSettings["connectionString"]);

        public TestWorm()
        {
            Utils.SetDataDirectory();
            wormProvider.OpenConn();
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
        public void TestSelect()
        {
            wormProvider.SelectWithoutLoad();
        }

        [TestMethod]
        public void TestSelectWithLoad()
        {
            wormProvider.SelectWithLoad();
        }
    }
}
