using System;
using System.Text;
using System.Collections.Generic;
using DALinq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common;

namespace Tests
{
    [TestClass]
    public class TestLinq : TestBase
    {
        static LinqProvider linqProvider = new LinqProvider(BaseSqlConnection);

        public TestContext TestContext
        {
            get { return context; }
            set { context = value; }
        }

        static TestLinq()
        {
            TestBase.classType = typeof(TestLinq);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleWithoutLoad)]
        public void TypeCycleWithoutLoad()
        {
            linqProvider.TypeCycleWithoutLoad(mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleWithLoad)]
        public void TypeCycleWithLoad()
        {
            linqProvider.TypeCycleWithLoad(mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleLazyLoad)]
        public void TypeCycleLazyLoad()
        {
            linqProvider.TypeCycleLazyLoad(mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionByIdArray)]
        public void SmallCollectionByIdArray()
        {
            linqProvider.CollectionByIdArray(smallUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollection)]
        public void SmallCollection()
        {
            linqProvider.GetCollection(Constants.Small);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionWithChildrenByIdArray)]
        public void SmallCollectionWithChildrenByIdArray()
        {
            linqProvider.CollectionWithChildrenByIdArray(smallUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionByIdArray)]
        public void LargeCollectionByIdArray()
        {
            linqProvider.CollectionByIdArray(largeUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollection)]
        public void LargeCollection()
        {
            linqProvider.GetCollection(Constants.Large);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionWithChildrenByIdArray)]
        public void LargeCollectionWithChildrenByIdArray()
        {
            linqProvider.CollectionWithChildrenByIdArray(largeUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithoutLoad)]
        public void CollectionByPredicateWithoutLoad()
        {
            linqProvider.CollectionByPredicateWithoutLoad();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad)]
        public void CollectionByPredicateWithLoad()
        {
            linqProvider.CollectionByPredicateWithLoad();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectLargeCollection)]
        public void SelectLargeCollection()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                linqProvider.GetCollection(Constants.Large);
            }
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad)]
        public void SameObjectInCycleLoad()
        {
            linqProvider.SameObjectInCycleLoad(smallUserIds[0]);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectBySamePredicate)]
        public void SelectBySamePredicate()
        {
            linqProvider.SelectBySamePredicate();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess)]
        public void ObjectsWithLoadWithPropertiesAccess()
        {
            linqProvider.ObjectsWithLoadWithPropertiesAccess();
        }
    }
}
