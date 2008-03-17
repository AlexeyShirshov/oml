using System;
using System.Configuration;
using System.Text;
using System.Collections.Generic;
using DAWorm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [Ignore]
    [TestClass]
    public class TestWorm : TestBase
    {
        static WormProvider wormProvider = new WormProvider(ConfigurationSettings.AppSettings["connectionString"]);
        public TestContext TestContext
        {
            get { return context; }
            set { context = value; }
        }

        static TestWorm()
        {
            TestBase.classType = typeof(TestWorm);
        }

        [TestMethod]
        public void SelectWithoutLoad()
        {
            wormProvider.SelectWithoutLoad();
        }

        [TestMethod]
        public void SelectWithLoad()
        {
            wormProvider.SelectWithLoad();
        }

        [TestMethod]
        public void SelectCollectionWithLoad()
        {
            wormProvider.SelectCollectionWithLoad();
        }

        [TestMethod]
        public void SelectCollectionWithoutLoad()
        {
            wormProvider.SelectCollectionWithoutLoad();
        }

        [TestMethod]
        public void SelectSmallWithoutLoad()
        {
            wormProvider.SelectSmallWithoutLoad();
        }

        [TestMethod]
        public void SelectSmallWithLoad()
        {
            wormProvider.SelectSmallWithLoad();
        }

        [TestMethod]
        public void SelectSmallCollectionWithLoad()
        {
            wormProvider.SelectSmallCollectionWithLoad();
        }

        [TestMethod]
        public void SelectSmallCollectionWithoutLoad()
        {
            wormProvider.SelectSmallCollectionWithoutLoad();
        }

   
    }
}
