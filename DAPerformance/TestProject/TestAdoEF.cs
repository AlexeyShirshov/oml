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

        #region Linq Syntax
        [Ignore]
        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleWithoutLoad, Syntax.Linq)]
        public void TypeCycleWithoutLoadLinq()
        {
            adoEFProvider.TypeCycleWithoutLoadLinq(mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleWithLoad, Syntax.Linq)]
        public void TypeCycleWithLoadLinq()
        {
            adoEFProvider.TypeCycleWithLoadLinq(mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleLazyLoad, Syntax.Linq)]
        public void TypeCycleLazyLoadLinq()
        {
            adoEFProvider.TypeCycleLazyLoadLinq(mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollection, Syntax.Linq)]
        public void SmallCollectionLinq()
        {
            adoEFProvider.GetCollectionLinq(Constants.Small);
        }

        [Ignore]//incorrrect in Ado EF beta 3 ("Contains()" method in Linq to Entities)
        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionByIdArray, Syntax.Linq)]
        public void SmallCollectionByIdArrayLinq()
        {
            adoEFProvider.CollectionWithChildrenByIdArrayLinq(smallUserIds);
        }

        [Ignore]//incorrrect in Ado EF beta 3 ("Contains()" method in Linq to Entities)
        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionWithChildrenByIdArray, Syntax.Linq)]
        public void SmallCollectionWithChildrenByIdArrayLinq()
        {
            adoEFProvider.CollectionWithChildrenByIdArrayLinq(smallUserIds);
        }

        [Ignore]//incorrrect in Ado EF beta 3 ("Contains()" method in Linq to Entities)
        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionByIdArray, Syntax.Linq)]
        public void LargeCollectionByIdArrayLinq()
        {
            adoEFProvider.CollectionByIdArrayLinq(largeUserIds);
        }

        [Ignore]//incorrrect in Ado EF beta 3 ("Contains()" method in Linq to Entities)
        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionWithChildrenByIdArray, Syntax.Linq)]
        public void LargeCollectionWithChildrenByIdArrayLinq()
        {
            adoEFProvider.CollectionWithChildrenByIdArrayLinq(largeUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollection, Syntax.Linq)]
        public void LargeCollectionLinq()
        {
            adoEFProvider.GetCollectionLinq(Constants.Large);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectLargeCollection, Syntax.Linq)]
        public void SelectLargeCollectionLinq()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                adoEFProvider.GetCollectionLinq(Constants.Large);
            }
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad, Syntax.Linq)]
        public void SameObjectInCycleLoadLinq()
        {
            adoEFProvider.SameObjectInCycleLoadLinq(Constants.SmallIteration, smallUserIds[0]);
        }

        [Ignore]
        [TestMethod]
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithoutLoad, Syntax.Linq)]
        public void CollectionByPredicateWithoutLoadLinq()
        {
            adoEFProvider.CollectionByPredicateWithoutLoadLinq(Constants.LargeIteration);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad, Syntax.Linq)]
        public void CollectionByPredicateWithLoadLinq()
        {
            adoEFProvider.CollectionByPredicateWithLoadLinq(Constants.LargeIteration);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectBySamePredicate, Syntax.Linq)]
        public void SelectBySamePredicateLinq()
        {
            adoEFProvider.SelectBySamePredicateLinq(Constants.SmallIteration);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess, Syntax.Linq)]
        public void ObjectsWithLoadWithPropertiesAccessLinq()
        {
            adoEFProvider.ObjectsWithLoadWithPropertiesAccessLinq();
        }

        #endregion Linq Syntax


        #region Default Syntax
        [Ignore]
        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleWithoutLoad  )]
        public void TypeCycleWithoutLoad ()
        {
            adoEFProvider.TypeCycleWithoutLoad (mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleWithLoad  )]
        public void TypeCycleWithLoad ()
        {
            adoEFProvider.TypeCycleWithLoad (mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleLazyLoad  )]
        public void TypeCycleLazyLoad ()
        {
            adoEFProvider.TypeCycleLazyLoad (mediumUserIds);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollection  )]
        public void SmallCollection ()
        {
            adoEFProvider.GetCollection (Constants.Small);
        }

        [Ignore]//incorrrect in Ado EF beta 3 ("Contains()" method in Linq to Entities)
        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionByIdArray)]
        public void SmallCollectionByIdArray ()
        {
            
        }

        [Ignore]//incorrrect in Ado EF beta 3 ("Contains()" method in Linq to Entities)     
        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionWithChildrenByIdArray  )]
        public void SmallCollectionWithChildrenByIdArray ()
        {
            
        }


        [Ignore]//incorrrect in Ado EF beta 3 ("Contains()" method in Linq to Entities)
        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionByIdArray  )]
        public void LargeCollectionByIdArray ()
        {
            
        }


        [Ignore]//incorrrect in Ado EF beta 3 ("Contains()" method in Linq to Entities)
        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionWithChildrenByIdArray  )]
        public void LargeCollectionWithChildrenByIdArray ()
        {
            
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollection  )]
        public void LargeCollection ()
        {
            adoEFProvider.GetCollection (Constants.Large);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectLargeCollection  )]
        public void SelectLargeCollection ()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                adoEFProvider.GetCollection (Constants.Large);
            }
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad  )]
        public void SameObjectInCycleLoad ()
        {
            adoEFProvider.SameObjectInCycleLoad (Constants.SmallIteration, smallUserIds[0]);
        }

        [Ignore]
        [TestMethod]
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithoutLoad)]
        public void CollectionByPredicateWithoutLoad ()
        {
            adoEFProvider.CollectionByPredicateWithoutLoad (Constants.LargeIteration);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad)]
        public void CollectionByPredicateWithLoad()
        {
            adoEFProvider.CollectionByPredicateWithLoad (Constants.LargeIteration);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectBySamePredicate)]
        public void SelectBySamePredicate()
        {
            adoEFProvider.SelectBySamePredicate (Constants.SmallIteration);
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess)]
        public void ObjectsWithLoadWithPropertiesAccess()
        {
            adoEFProvider.ObjectsWithLoadWithPropertiesAccess ();
        }

        #endregion Default Syntax
    }
}