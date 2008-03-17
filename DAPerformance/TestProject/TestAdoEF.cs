using System;
using System.Configuration;
using System.Data;
using System.Text;
using System.Collections.Generic;
using DaAdoEF;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [Ignore]
    [TestClass]
    public class TestAdoEF : TestBase
    {
        static AdoEFProvider adoEFProvider = new AdoEFProvider();

        static TestAdoEF()
        {
            TestBase.classType = typeof(TestAdoEF);
        }

        public TestContext TestContext
        {
            get { return context; }
            set { context = value; }
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

     
        //Single
        [TestMethod]
        public void SelectWithLinqWithLoad()
        {
            adoEFProvider.SelectWithLinqWithLoad();
        }

        [TestMethod]
        public void SelectWithLinqWithoutLoad()
        {
            adoEFProvider.SelectWithLinqWithoutLoad();
        }

        [TestMethod]
        public void SelectWithObjectServicesAnonimousWithLoad()
        {
            adoEFProvider.SelectWithObjectServicesAnonimousWithLoad();
        }

        [TestMethod]
        public void SelectWithObjectServicesAnonimousWithoutLoad()
        {
            adoEFProvider.SelectWithObjectServicesAnonimousWithoutLoad();
        }

        [TestMethod]
        public void SelectWithObjectServicesFactoryWithLoad()
        {
            adoEFProvider.SelectWithObjectServicesFactoryWithLoad();
        }

        [TestMethod]
        public void SelectWithObjectServicesFactoryWithoutLoad()
        {
            adoEFProvider.SelectWithObjectServicesFactoryWithoutLoad();
        }

        [TestMethod]
        public void SelectWithObjectServicesWithLoad()
        {
            adoEFProvider.SelectWithObjectServicesWithLoad();
        }

        [TestMethod]
        public void SelectWithObjectServicesWithoutLoad()
        {
            adoEFProvider.SelectWithObjectServicesWithoutLoad();
        }

        [TestMethod]
        public void SelectWithEntityClientAnonimousWithLoad()
        {
            adoEFProvider.SelectWithEntityClientAnonimousWithLoad();
        }

        [TestMethod]
        public void SelectWithEntityClientAnonimousWithoutLoad()
        {
            adoEFProvider.SelectWithEntityClientAnonimousWithoutLoad();
        }



     //Collection
        [TestMethod]
        public void SelectCollectionWithLinqWithLoad()
        {
            adoEFProvider.SelectCollectionWithLinqWithLoad();
        }

        [TestMethod]
        public void SelectCollectionWithLinqWithoutLoad()
        {
            adoEFProvider.SelectCollectionWithLinqWithoutLoad();
        }

        [TestMethod]
        public void SelectCollectionWithObjectServicesAnonimousWithLoad()
        {
            adoEFProvider.SelectCollectionWithObjectServicesAnonimousWithLoad();
        }

        [TestMethod]
        public void SelectCollectionWithObjectServicesAnonimousWithoutLoad()
        {
            adoEFProvider.SelectCollectionWithObjectServicesAnonimousWithoutLoad();
        }

        [TestMethod]
        public void SelectCollectionWithObjectServicesFactoryWithLoad()
        {
            adoEFProvider.SelectCollectionWithObjectServicesFactoryWithLoad();
        }

        [TestMethod]
        public void SelectCollectionWithObjectServicesFactoryWithoutLoad()
        {
            adoEFProvider.SelectCollectionWithObjectServicesFactoryWithoutLoad();
        }

        [TestMethod]
        public void SelectCollectionWithObjectServicesWithLoad()
        {
            adoEFProvider.SelectCollectionWithObjectServicesWithLoad();
        }

        [TestMethod]
        public void SelectCollectionWithObjectServicesWithoutLoad()
        {
            adoEFProvider.SelectCollectionWithObjectServicesWithoutLoad();
        }

        [TestMethod]
        public void SelectCollectionWithEntityClientAnonimousWithLoad()
        {
            adoEFProvider.SelectCollectionWithEntityClientAnonimousWithLoad();
        }

        [TestMethod]
        public void SelectCollectionWithEntityClientAnonimousWithoutLoad()
        {
            adoEFProvider.SelectCollectionWithEntityClientAnonimousWithoutLoad();
        }


        //Single
        [TestMethod]
        public void SelectSmallWithLinqWithLoad()
        {
            adoEFProvider.SelectSmallWithLinqWithLoad();
        }

        [TestMethod]
        public void SelectSmallWithLinqWithoutLoad()
        {
            adoEFProvider.SelectSmallWithLinqWithoutLoad();
        }

        [TestMethod]
        public void SelectSmallWithObjectServicesAnonimousWithLoad()
        {
            adoEFProvider.SelectSmallWithObjectServicesAnonimousWithLoad();
        }

        [TestMethod]
        public void SelectSmallWithObjectServicesAnonimousWithoutLoad()
        {
            adoEFProvider.SelectSmallWithObjectServicesAnonimousWithoutLoad();
        }

        [TestMethod]
        public void SelectSmallWithObjectServicesFactoryWithLoad()
        {
            adoEFProvider.SelectSmallWithObjectServicesFactoryWithLoad();
        }

        [TestMethod]
        public void SelectSmallWithObjectServicesFactoryWithoutLoad()
        {
            adoEFProvider.SelectSmallWithObjectServicesFactoryWithoutLoad();
        }

        [TestMethod]
        public void SelectSmallWithObjectServicesWithLoad()
        {
            adoEFProvider.SelectSmallWithObjectServicesWithLoad();
        }

        [TestMethod]
        public void SelectSmallWithObjectServicesWithoutLoad()
        {
            adoEFProvider.SelectSmallWithObjectServicesWithoutLoad();
        }

        [TestMethod]
        public void SelectSmallWithEntityClientAnonimousWithLoad()
        {
            adoEFProvider.SelectSmallWithEntityClientAnonimousWithLoad();
        }

        [TestMethod]
        public void SelectSmallWithEntityClientAnonimousWithoutLoad()
        {
            adoEFProvider.SelectSmallWithEntityClientAnonimousWithoutLoad();
        }



        //Collection
        [TestMethod]
        public void SelectSmallCollectionWithLinqWithLoad()
        {
            adoEFProvider.SelectSmallCollectionWithLinqWithLoad();
        }

        [TestMethod]
        public void SelectSmallCollectionWithLinqWithoutLoad()
        {
            adoEFProvider.SelectSmallCollectionWithLinqWithoutLoad();
        }

        [TestMethod]
        public void SelectSmallCollectionWithObjectServicesAnonimousWithLoad()
        {
            adoEFProvider.SelectSmallCollectionWithObjectServicesAnonimousWithLoad();
        }

        [TestMethod]
        public void SelectSmallCollectionWithObjectServicesAnonimousWithoutLoad()
        {
            adoEFProvider.SelectSmallCollectionWithObjectServicesAnonimousWithoutLoad();
        }

        [TestMethod]
        public void SelectSmallCollectionWithObjectServicesFactoryWithLoad()
        {
            adoEFProvider.SelectSmallCollectionWithObjectServicesFactoryWithLoad();
        }

        [TestMethod]
        public void SelectSmallCollectionWithObjectServicesFactoryWithoutLoad()
        {
            adoEFProvider.SelectSmallCollectionWithObjectServicesFactoryWithoutLoad();
        }

        [TestMethod]
        public void SelectSmallCollectionWithObjectServicesWithLoad()
        {
            adoEFProvider.SelectSmallCollectionWithObjectServicesWithLoad();
        }

        [TestMethod]
        public void SelectSmallCollectionWithObjectServicesWithoutLoad()
        {
            adoEFProvider.SelectSmallCollectionWithObjectServicesWithoutLoad();
        }

        [TestMethod]
        public void SelectSmallCollectionWithEntityClientAnonimousWithLoad()
        {
            adoEFProvider.SelectSmallCollectionWithEntityClientAnonimousWithLoad();
        }

        [TestMethod]
        public void SelectSmallCollectionWithEntityClientAnonimousWithoutLoad()
        {
            adoEFProvider.SelectSmallCollectionWithEntityClientAnonimousWithoutLoad();
        }

       
    }
}
