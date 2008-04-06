using System;
using System.Text;
using System.Collections.Generic;
using DALinq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common;
using System.Threading;

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

        [TestInitialize]
        public override void TestInitialize()
        {
            linqProvider.CreateNewDatabaseDataContext();
            base.TestInitialize();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleWithoutLoad)]
        public void TypeCycleWithoutLoad()
        {
            linqProvider.LoadingEnabled = true;
            linqProvider.TypeCycleWithoutLoad(mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleWithLoad)]
        public void TypeCycleWithLoad()
        {
            linqProvider.LoadingEnabled = false;
            linqProvider.TypeCycleWithLoad(mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleLazyLoad)]
        public void TypeCycleLazyLoad()
        {
            linqProvider.LoadingEnabled = true;
            linqProvider.TypeCycleLazyLoad(mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionByIdArray)]
        public void SmallCollectionByIdArray()
        {
            linqProvider.LoadingEnabled = false;
            linqProvider.CollectionByIdArray(smallUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollection)]
        public void SmallCollection()
        {
            linqProvider.LoadingEnabled = false;
            linqProvider.GetCollection(Constants.Small);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionWithChildrenByIdArray)]
        public void SmallCollectionWithChildrenByIdArray()
        {
            linqProvider.LoadingEnabled = false;
            linqProvider.CollectionWithChildrenByIdArray(smallUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionByIdArray)]
        public void LargeCollectionByIdArray()
        {
            linqProvider.LoadingEnabled = false;
            linqProvider.CollectionByIdArray(largeUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollection)]
        public void LargeCollection()
        {
            linqProvider.LoadingEnabled = false;
            linqProvider.GetCollection(Constants.Large);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionWithChildrenByIdArray)]
        public void LargeCollectionWithChildrenByIdArray()
        {
            linqProvider.LoadingEnabled = false;
            linqProvider.CollectionWithChildrenByIdArray(largeUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithoutLoad)]
        public void CollectionByPredicateWithoutLoad()
        {
            linqProvider.LoadingEnabled = true;
            linqProvider.CollectionByPredicateWithoutLoad();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad)]
        public void CollectionByPredicateWithLoad()
        {
            linqProvider.LoadingEnabled = false;
            linqProvider.CollectionByPredicateWithLoad();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectLargeCollection)]
        public void SelectLargeCollection()
        {
            linqProvider.LoadingEnabled = false;
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                linqProvider.GetCollection(Constants.Large);
            }
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad)]
        public void SameObjectInCycleLoad()
        {
            linqProvider.LoadingEnabled = false;
            linqProvider.SameObjectInCycleLoad(smallUserIds[0]);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectBySamePredicate)]
        public void SelectBySamePredicate()
        {
            linqProvider.LoadingEnabled = true;
            linqProvider.SelectBySamePredicate();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess)]
        public void ObjectsWithLoadWithPropertiesAccess()
        {
            linqProvider.LoadingEnabled = false;
            linqProvider.ObjectsWithLoadWithPropertiesAccess();
        }


        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollection)]
        public void a1()
        {
            for (ulong i = 0; i < 1000000; i++)
            {
            }
        }


        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollection)]
        public void a2()
        {
            for (ulong i = 0; i < 1000000; i++)
            {
            }
        }
        
        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollection)]
        public void a3()
        {
            for (ulong i = 0; i < 1000000; i++)
            {
            }
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollection)]
        public void a4()
        {
            for (ulong i = 0; i < 1000000; i++)
            {
            }
        }

    }
}
