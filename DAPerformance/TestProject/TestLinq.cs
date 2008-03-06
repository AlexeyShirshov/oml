using System;
using System.Text;
using System.Collections.Generic;
using DALinq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestLinq : TestBase
    {
        static LinqProvider linqProvider = new LinqProvider();

        public TestContext TestContext
        {
            get { return context; }
            set { context = value; }
        }

        static TestLinq()
        {
            TestBase.classType = typeof(TestLinq);
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
            linqProvider.SelectWithLoad();
        }

        [TestMethod]
        public void SelectWithoutLoad()
        {
            linqProvider.SelectWithoutLoad();
        }

        [TestMethod]
        public void SelectWithListWithLoad()
        {
            linqProvider.SelectWithListLoad();
        }

         [TestMethod]
        public void SelectShortWithoutLoad()
        {
            linqProvider.SelectShortWithoutLoad();
        }

         [TestMethod]
         public void SelectShortWithLoad()
        {
            linqProvider.SelectShortWithLoad();
        }

         [TestMethod]
         public void SelectShortWithListWithLoad()
        {
            linqProvider.SelectShortWithListLoad();
        }

        

        [TestMethod]
        public void SelectCollectionWithoutLoad()
        {
            linqProvider.SelectCollectionWithoutLoad();
        }

        [TestMethod]
        public void SelectCollectionWithLoad()
        {
            linqProvider.SelectCollectionWithLoad();
        }      
      

        [TestMethod]
        public void SelectCollectionWithListWithLoad()
        {
            linqProvider.SelectCollectionWithListLoad();
        }



        [TestMethod]
        public void SelectSmallWithLoad()
        {
            linqProvider.SelectSmallWithLoad();
        }

        [TestMethod]
        public void SelectSmallWithoutLoad()
        {
            linqProvider.SelectSmallWithoutLoad();
        }

        [TestMethod]
        public void SelectSmallWithListWithLoad()
        {
            linqProvider.SelectSmallWithListLoad();
        }

        [TestMethod]
        public void SelectSmallCollectionWithoutLoad()
        {
            linqProvider.SelectSmallCollectionWithoutLoad();
        }

        [TestMethod]
        public void SelectSmallCollectionWithLoad()
        {
            linqProvider.SelectSmallCollectionWithLoad();
        }

        [TestMethod]
        public void SelectSmallCollectionWithListWithLoad()
        {
            linqProvider.SelectSmallCollectionWithListLoad();
        }

        [TestMethod]
        public void SelectCollectionShortWithoutLoad()
        {
            linqProvider.SelectShortWithoutLoad();
        }

        [TestMethod]
        public void SelectCollectionShortWithLoad()
        {
            linqProvider.SelectShortWithLoad();
        }

        [TestMethod]
        public void SelectCollectionShortWithListWithLoad()
        {
            linqProvider.SelectShortWithListLoad();
        }
   
    }
}
