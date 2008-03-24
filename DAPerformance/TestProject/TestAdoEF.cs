using System;
using System.Configuration;
using System.Data;
using System.Text;
using System.Collections.Generic;
using System.Data.EntityClient;
using DaAdoEF;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common;

namespace Tests
{
    [TestClass]
    public class TestAdoEF : TestBase
    {
        AdoEFProvider adoEFProvider;

        #region constructors
        static TestAdoEF()
        {
            TestBase.classType = typeof(TestAdoEF);
        }

        public TestAdoEF()
        {
            adoEFProvider = new AdoEFProvider(ConfigurationManager.ConnectionStrings["TestDAEntities"].ToString());
        }

        ~TestAdoEF()
        {
            if (adoEFProvider != null)
            {
                adoEFProvider.Dispose();
            }
        }
        #endregion constructors

        public TestContext TestContext
        {
            get { return context; }
            set { context = value; }
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleWithoutLoad, Syntax.Linq)]
        public void TypeCycleWithoutLoad()
        {
            adoEFProvider.TypeCycleWithoutLoadLinq(mediumUserIds);
        }

        [Ignore]//Incorrect linq syntax
        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleWithLoad, Syntax.Linq)]
        public void TypeCycleWithLoad()
        {
            adoEFProvider.TypeCycleWithLoadLinq(mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleLazyLoad, Syntax.Linq)]
        public void TypeCycleLazyLoad()
        {
            adoEFProvider.TypeCycleLazyLoadLinq(mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollection, Syntax.Linq)]
        public void SmallCollection()
        {
            adoEFProvider.GetCollectionLinq(Constants.Small);
        }

        [Ignore]
        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionByIdArray, Syntax.Linq)]
        public void SmallCollectionByIdArray()
        {
            adoEFProvider.CollectionByIdArrayLinq(smallUserIds);
        }

        [Ignore]
        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionWithChildrenByIdArray, Syntax.Linq)]
        public void SmallCollectionWithChildrenByIdArray()
        {
            //adoEFProvider.CollectionWithChildrenByIdArray(smallUserIds);
        }

        [Ignore]
        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionByIdArray, Syntax.Linq)]
        public void LargeCollectionByIdArray()
        {
           // adoEFProvider.CollectionByIdArray(largeUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollection, Syntax.Linq)]
        public void LargeCollection()
        {
            adoEFProvider.GetCollectionLinq(Constants.Large);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectLargeCollection, Syntax.Linq)]
        public void SelectLargeCollection()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                adoEFProvider.GetCollectionLinq(Constants.Large);
            }
        }

        [Ignore]
        [TestMethod]
        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad, Syntax.Linq)]
        public void SameObjectInCycleLoad()
        {
            adoEFProvider.SameObjectInCycleLoadLinq(Constants.SmallIteration, smallUserIds[0]);
        }

/*
                [TestMethod]
                [QueryTypeAttribute(QueryType.LargeCollectionWithChildrenByIdArray)]
                public void LargeCollectionWithChildrenByIdArray()
                {
                    adoEFProvider.CollectionWithChildrenByIdArray(largeUserIds);
                }

                [TestMethod]
                [QueryTypeAttribute(QueryType.CollectionByPredicateWithoutLoad)]
                public void CollectionByPredicateWithoutLoad()
                {
                    adoEFProvider.CollectionByPredicateWithoutLoad();
                }

                [TestMethod]
                [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad)]
                public void CollectionByPredicateWithLoad()
                {
                    adoEFProvider.CollectionByPredicateWithLoad();
                }


                [TestMethod]
                [QueryTypeAttribute(QueryType.SelectBySamePredicate)]
                public void SelectBySamePredicate()
                {
                    adoEFProvider.SelectBySamePredicate();
                }

                [TestMethod]
                [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess)]
                public void ObjectsWithLoadWithPropertiesAccess()
                {
                    adoEFProvider.ObjectsWithLoadWithPropertiesAccess();
                }
                */

        #region old
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

        #endregion old
    }
}