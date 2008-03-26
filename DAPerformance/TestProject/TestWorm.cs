using System;
using System.Configuration;
using System.Text;
using System.Collections.Generic;

using Common;
using DAWorm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Worm.Cache;
using Worm.Database;
using Worm.Orm;

namespace Tests
{
    [TestClass]
    public class TestWorm : TestBase
    {
        static OrmReadOnlyDBManager manager;
        static WormProvider wormProvider;

        public TestContext TestContext
        {
            get { return context; }
            set { context = value; }
        }

        static TestWorm()
        {
            TestBase.classType = typeof(TestWorm);
            manager = new OrmDBManager(new OrmCache(), new SQLGenerator("1"), ConfigurationSettings.AppSettings["ConnectionStringBase"]);
            wormProvider = new WormProvider(manager);
        }

        [TestInitialize]
        public override void TestInitialize()
        {
            wormProvider.Manager = new OrmDBManager(new OrmCache(), new SQLGenerator("1"), ConfigurationSettings.AppSettings["ConnectionStringBase"]);
            base.TestInitialize();
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            base.TestCleanup();
           // manager.Dispose();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleWithoutLoad)]
        public void TypeCycleWithoutLoad()
        {
            wormProvider.TypeCycleWithoutLoad(mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleWithLoad)]
        public void TypeCycleWithLoad()
        {
            wormProvider.TypeCycleWithLoad(mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleLazyLoad)]
        public void TypeCycleLazyLoad()
        {
            wormProvider.TypeCycleLazyLoad(mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionByIdArray)]
        public void SmallCollectionByIdArray()
        {
            wormProvider.CollectionByIdArray(smallUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollection)]
        public void SmallCollection()
        {
            wormProvider.GetCollection(Constants.Small);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionWithChildrenByIdArray)]
        public void SmallCollectionWithChildrenByIdArray()
        {
            wormProvider.CollectionWithChildrenByIdArray(smallUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionByIdArray)]
        public void LargeCollectionByIdArray()
        {
            wormProvider.CollectionByIdArray(largeUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollection)]
        public void LargeCollection()
        {
            wormProvider.GetCollection(Constants.Large);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionWithChildrenByIdArray)]
        public void LargeCollectionWithChildrenByIdArray()
        {
            wormProvider.CollectionWithChildrenByIdArray(largeUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithoutLoad)]
        public void CollectionByPredicateWithoutLoad()
        {
            wormProvider.CollectionByPredicateWithoutLoad(Constants.LargeIteration);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad)]
        public void CollectionByPredicateWithLoad()
        {
            wormProvider.CollectionByPredicateWithLoad(Constants.LargeIteration);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectLargeCollection)]
        public void SelectLargeCollection()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                wormProvider.GetCollection(Constants.Large);
            }
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad)]
        public void SameObjectInCycleLoad()
        {
            wormProvider.SameObjectInCycleLoad(Constants.SmallIteration, smallUserIds[0]);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectBySamePredicate)]
        public void SelectBySamePredicate()
        {
            wormProvider.SelectBySamePredicate(Constants.SmallIteration);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess)]
        public void ObjectsWithLoadWithPropertiesAccess()
        {
            wormProvider.ObjectsWithLoadWithPropertiesAccess();
        }
    }
}
