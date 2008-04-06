using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Reflection;
using System.Data.SqlClient;
using System.Text;

using DAAdo;
using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TestAdo : TestBase
    {
        private static AdoProvider adoProvider = new AdoProvider(BaseSqlConnection);

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
            adoProvider.Collection(Constants.Small);
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
            adoProvider.Collection(Constants.Large);
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
                adoProvider.Collection(Constants.Large);
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

        #region Dataset
        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleWithLoad, Syntax.Dataset)]
        public void TypeCycleWithLoadDataset()
        {
            adoProvider.TypeCycleWithLoadDataset(mediumUserIds);
        }


        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionByIdArray, Syntax.Dataset)]
        public void SmallCollectionByIdArrayDataset()
        {
            adoProvider.CollectionByIdArrayDataset(smallUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollection, Syntax.Dataset)]
        public void SmallCollectionDataset()
        {
            adoProvider.CollectionDataset(Constants.Small);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionWithChildrenByIdArray, Syntax.Dataset)]
        public void SmallCollectionWithChildrenByIdArrayDataset()
        {
            adoProvider.CollectionWithChildrenByIdArrayDataset(smallUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionByIdArray, Syntax.Dataset)]
        public void LargeCollectionByIdArrayDataset()
        {
            adoProvider.CollectionByIdArrayDataset(largeUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollection, Syntax.Dataset)]
        public void LargeCollectionDataset()
        {
            adoProvider.CollectionDataset(Constants.Large);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionWithChildrenByIdArray, Syntax.Dataset)]
        public void LargeCollectionWithChildrenByIdArrayDataset()
        {
            adoProvider.CollectionWithChildrenByIdArrayDataset(largeUserIds);
        }


        [TestMethod]
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad, Syntax.Dataset)]
        public void CollectionByPredicateWithLoadDataset()
        {
            adoProvider.CollectionByPredicateWithLoadDataset();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectLargeCollection, Syntax.Dataset)]
        public void SelectLargeCollectionDataset()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                adoProvider.CollectionDataset(Constants.Large);
            }
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad, Syntax.Dataset)]
        public void SameObjectInCycleLoadDataset()
        {
            adoProvider.SameObjectInCycleLoadDataset(smallUserIds[0]);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectBySamePredicate, Syntax.Dataset)]
        public void SelectBySamePredicateDataset()
        {
            adoProvider.SelectBySamePredicateDataset();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess, Syntax.Dataset)]
        public void ObjectsWithLoadWithPropertiesAccessDataset()
        {
            adoProvider.ObjectsWithLoadWithPropertiesAccessDataset();
        }

        #endregion Dataset
    }
}
