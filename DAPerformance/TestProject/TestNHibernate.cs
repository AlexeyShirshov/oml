using System;
using System.Data;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using DANHibernate;
using Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using NHibernate.Hql;
using NHibernate.Expression;
using NHibernate.Cfg;
namespace Tests
{
    [TestClass]
    public class TestNHibernate : TestBase
    {
        static Configuration cfg;
        static ISessionFactory factory;
        static ISession session;

        public TestContext TestContext
        {
            get { return context; }
            set { context = value; }
        }

        static TestNHibernate()
        {
            TestBase.classType = typeof(TestNHibernate);

            cfg = new Configuration();
            cfg.AddAssembly("DANHibernate");
            factory = cfg.BuildSessionFactory();
            session = factory.OpenSession();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            session.Close();
        }

        //[TestInitialize]
        //public override void TestInitialize()
        //{
        //    session = factory.OpenSession();
        //    base.TestInitialize();
        //}

        //[TestCleanup]
        //public override void TestCleanup()
        //{
        //    base.TestCleanup();
        //    session.Close();
        //}

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleWithoutLoad)]
        public void TypeCycleWithoutLoad()
        {
            foreach (int id in mediumUserIds)
            {
                LazyUser user = (LazyUser)session.Load(typeof(LazyUser), id);
            }
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleWithLoad)]
        public void TypeCycleWithLoad()
        {
            foreach (int id in mediumUserIds)
            {
                User user = (User)session.Load(typeof(User), id);
            }
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.TypeCycleLazyLoad)]
        public void TypeCycleLazyLoad()
        {
            foreach (int id in mediumUserIds)
            {
                LazyUser user = (LazyUser)session.Load(typeof(LazyUser), id);
                string name = user.FirstName;
            }
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionByIdArray)]
        public void SmallCollectionByIdArray()
        {
            object users = session.CreateCriteria(typeof(User))
                .Add(Expression.In("UserId", smallUserIds)).List();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollection)]
        public void SmallCollection()
        {
            object users = session.CreateCriteria(typeof(User)).SetMaxResults(Constants.Small).List();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SmallCollectionWithChildrenByIdArray)]
        public void SmallCollectionWithChildrenByIdArray()
        {
            IList users = session.CreateCriteria(typeof(FullUser))
               .Add(Expression.In("UserId", smallUserIds)).List();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionByIdArray)]
        public void LargeCollectionByIdArray()
        {
            object users = session.CreateCriteria(typeof(User))
               .Add(Expression.In("UserId", largeUserIds)).List();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollection)]
        public void LargeCollection()
        {
            object users = session.CreateCriteria(typeof(User)).SetMaxResults(Constants.Large).List();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.LargeCollectionWithChildrenByIdArray)]
        public void LargeCollectionWithChildrenByIdArray()
        {
            IList users = session.CreateCriteria(typeof(FullUser))
              .Add(Expression.In("UserId", largeUserIds)).List();
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithoutLoad)]
        public void CollectionByPredicateWithoutLoad()
        {
             for (int i = 0; i < Constants.LargeIteration; i++)
            {
                // 4 minutes!
                // IList users = session.CreateCriteria(typeof(LazyUser))
                // .CreateCriteria("Phones")
                //.Add(Expression.Like("PhoneNumber", (i + 1) + "%")).List();

                 IQuery q = session.CreateQuery(
                     "select u.UserId,  u.FirstName,  u.LastName from LazyUser as u " +
                 "inner join u.Phones as p where p.PhoneNumber like '" + (i + 1) + "%'");
                 IList users = q.List();
            }
            
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad)]
        public void CollectionByPredicateWithLoad()
        {
            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                // 4 minutes!
                //IList users = session.CreateCriteria(typeof(User))
                //   .CreateCriteria("Phones")
                //    .Add(Expression.Like("PhoneNumber", (i + 1) + "%")).List();

                IQuery q = session.CreateQuery(
                     "select u.UserId,  u.FirstName,  u.LastName from User as u " +
                 "inner join u.Phones as p where p.PhoneNumber like '" + (i + 1) + "%'");
                IList users = q.List();
            }
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectLargeCollection)]
        public void SelectLargeCollection()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                object users = session.CreateCriteria(typeof(User)).SetMaxResults(Constants.Large).List();
            }
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad)]
        public void SameObjectInCycleLoad()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                User user = (User)session.Load(typeof(User), smallUserIds[0]);
            }
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.SelectBySamePredicate)]
        public void SelectBySamePredicate()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                IList users = session.CreateCriteria(typeof(User))
                    .CreateCriteria("Phones")
                    .Add(Expression.Like("PhoneNumber", "1%")).List();
            }
        }

        [TestMethod]
        [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess)]
        public void ObjectsWithLoadWithPropertiesAccess()
        {
            IList users = session.CreateCriteria(typeof(User)).List();
            foreach (User user in users)
            {
                string name = user.FirstName;
            }
        }
    }
}
