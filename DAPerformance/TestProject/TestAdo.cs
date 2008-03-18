using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Reflection;
using System.Text;

using DAAdo;
using Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestAdo : TestBase
    {
        static AdoProvider adoProvider = new AdoProvider(conn);

        public TestContext TestContext
        {
            get { return context; }
            set { context = value; }
        }

        static TestAdo()
        {
            TestBase.classType = typeof(TestAdo);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleWithLoad)]
        public void TypeCycleWithLoad()
        {
            adoProvider.TypeCycleWithLoad(mediumUserIds);
        }


        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionByIdArray)]
        public void SmallCollectionByIdArray()
        {
            adoProvider.CollectionByIdArray(smallUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollection)]
        public void SmallCollection()
        {
            adoProvider.SmallCollection();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionWithChildrenByIdArray)]
        public void SmallCollectionWithChildrenByIdArray()
        {
            adoProvider.CollectionWithChildrenByIdArray(smallUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionByIdArray)]
        public void LargeCollectionByIdArray()
        {
            adoProvider.CollectionByIdArray(largeUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollection)]
        public void LargeCollection()
        {
            adoProvider.LargeCollection();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionWithChildrenByIdArray)]
        public void LargeCollectionWithChildrenByIdArray()
        {
            adoProvider.CollectionWithChildrenByIdArray(largeUserIds);
        }


        [TestMethod]
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad)]
        public void CollectionByPredicateWithLoad()
        {
            adoProvider.CollectionByPredicateWithLoad();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectLargeCollection)]
        public void SelectLargeCollection()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                adoProvider.LargeCollection();
            }
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad)]
        public void SameObjectInCycleLoad()
        {
            adoProvider.SameObjectInCycleLoad(smallUserIds[0]);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectBySamePredicate)]
        public void SelectBySamePredicate()
        {
            adoProvider.SelectBySamePredicate();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess)]
        public void ObjectsWithLoadWithPropertiesAccess()
        {
            adoProvider.ObjectsWithLoadWithPropertiesAccess();
        }

    }
}
