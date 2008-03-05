using System;
using System.Configuration;
using System.Text;
using System.Collections.Generic;
using DAWorm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TestWorm : TestBase
    {
        static WormProvider wormProvider = new WormProvider(ConfigurationSettings.AppSettings["connectionString"]);

        [ClassInitialize()]
        public static void Init(TestContext testContext)
        {
            context = testContext;
            Utils.SetDataDirectory();
            wormProvider.OpenConn();
        }

        protected override Type ClassType
        {
            get { return this.GetType(); }
        }

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
