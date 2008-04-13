using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using DANHibernate;
using Common;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Hql;
using NHibernate.Expression;
using NHibernate.Cfg;

namespace TestConsole
{
    class NHibernateProvider
    {
        Configuration cfg;
        ISessionFactory factory;
        ISession session;

        int[] smallUserIds;
        int[] mediumUserIds;
        int[] largeUserIds;

        public NHibernateProvider(int[] smallUserIds, int[] mediumUserIds, int[] largeUserIds)
        {
            this.smallUserIds = smallUserIds;
            this.mediumUserIds = mediumUserIds;
            this.largeUserIds = largeUserIds;

            cfg = new Configuration();
            cfg.AddAssembly("DANHibernate");
            factory = cfg.BuildSessionFactory();
        }

        public void OpenSession()
        {
            session = factory.OpenSession();
        }

        public void CloseSession()
        {
            session.Close();
        }

        #region Default syntax
        
        [QueryTypeAttribute(QueryType.TypeCycleWithoutLoad)]
        public void TypeCycleWithoutLoad()
        {
            foreach (int id in mediumUserIds)
            {
                LazyUser user = (LazyUser)session.Load(typeof(LazyUser), id);
            }
        }

        
        [QueryTypeAttribute(QueryType.TypeCycleWithLoad)]
        public void TypeCycleWithLoad()
        {
            foreach (int id in mediumUserIds)
            {
                User user = (User)session.Load(typeof(User), id);
            }
        }

        
        [QueryTypeAttribute(QueryType.TypeCycleLazyLoad)]
        public void TypeCycleLazyLoad()
        {
            foreach (int id in mediumUserIds)
            {
                LazyUser user = new LazyUser();
                Phone p = (Phone)session.Load(typeof(Phone), id);
                string name = user.FirstName;
            }
        }

        
        [QueryTypeAttribute(QueryType.SmallCollectionByIdArray)]
        public void SmallCollectionByIdArray()
        {
            object users = session.CreateCriteria(typeof(User))
                .Add(Expression.In("UserId", smallUserIds)).List();
        }

        
        [QueryTypeAttribute(QueryType.SmallCollection)]
        public void SmallCollection()
        {
            object users = session.CreateCriteria(typeof(User)).SetMaxResults(Constants.Small).List();
        }

        
        [QueryTypeAttribute(QueryType.SmallCollectionWithChildrenByIdArray)]
        public void SmallCollectionWithChildrenByIdArray()
        {
            IList users = session.CreateCriteria(typeof(FullUser))
               .Add(Expression.In("UserId", smallUserIds)).List();
        }

        
        [QueryTypeAttribute(QueryType.LargeCollectionByIdArray)]
        public void LargeCollectionByIdArray()
        {
            object users = session.CreateCriteria(typeof(User))
               .Add(Expression.In("UserId", largeUserIds)).List();
        }

        
        [QueryTypeAttribute(QueryType.LargeCollection)]
        public void LargeCollection()
        {
            object users = session.CreateCriteria(typeof(User)).SetMaxResults(Constants.Large).List();
        }

        
        [QueryTypeAttribute(QueryType.LargeCollectionWithChildrenByIdArray)]
        public void LargeCollectionWithChildrenByIdArray()
        {
            IList users = session.CreateCriteria(typeof(FullUser))
              .Add(Expression.In("UserId", largeUserIds)).List();
        }

        
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithoutLoad)]
        public void CollectionByPredicateWithoutLoad()
        {
            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                IList users1 = session.CreateCriteria(typeof(Phone)).
                    //   .SetFetchMode("Phones", FetchMode.Lazy).
                    //CreateCriteria("Phones").
                    Add(Expression.Like("PhoneNumber", (i + 1) + "%")).List();
            }
        }

        //[Ignore]
        
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad)]
        public void CollectionByPredicateWithLoad()
        {
            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                IList users1 = session.CreateCriteria(typeof(User)).
                    CreateCriteria("Phones").
                    Add(Expression.Like("PhoneNumber", (i + 1) + "%")).List();
            }
        }

        
        [QueryTypeAttribute(QueryType.SelectLargeCollection)]
        public void SelectLargeCollection()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                object users = session.CreateCriteria(typeof(User)).SetMaxResults(Constants.Large).List();
            }
        }

        
        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad)]
        public void SameObjectInCycleLoad()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                User user = (User)session.Load(typeof(User), smallUserIds[0]);
            }
        }

        
        [QueryTypeAttribute(QueryType.SelectBySamePredicate)]
        public void SelectBySamePredicate()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                IList users = session.CreateCriteria(typeof(LazyUser))
                    .CreateCriteria("Phones")
                    .Add(Expression.Like("PhoneNumber", "1%"))
                    .SetProjection(Projections.Distinct(Projections.Property("UserId"))).List();
            }
        }

        
        [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess)]
        public void ObjectsWithLoadWithPropertiesAccess()
        {
            IList users = session.CreateCriteria(typeof(User)).List();
            foreach (User user in users)
            {
                string name = user.FirstName;
            }
        }

        #endregion Default syntax


        #region Linq syntax


        [QueryTypeAttribute(QueryType.TypeCycleWithoutLoad, Syntax.Linq)]
        public void TypeCycleWithoutLoadLinq()
        {
            foreach (int id in mediumUserIds)
            {
                var users = (from e in session.Linq<User>()
                             where e.UserId == id
                             select e).ToList();
                foreach (var userId in users)
                {
                    var user = users.Single();
                    string name = user.FirstName;
                }
            }
            //foreach (int id in mediumUserIds)
            //{
            //    var users = (from e in session.Linq<User>()
            //                 where e.UserId == id
            //                 select e);
            //}
        }


        [QueryTypeAttribute(QueryType.TypeCycleWithLoad, Syntax.Linq)]
        public void TypeCycleWithLoadLinq()
        {
            foreach (int id in mediumUserIds)
            {
                var users = (from e in session.Linq<User>()
                             where e.UserId == id
                             select e).ToList();
            }
            //foreach (int id in mediumUserIds)
            //{
            //    var users = (from e in session.Linq<User>()
            //                 where e.UserId == id
            //                 select e).ToList();
            //}
        }


        [QueryTypeAttribute(QueryType.TypeCycleLazyLoad, Syntax.Linq)]
        public void TypeCycleLazyLoadLinq()
        {
            foreach (int id in mediumUserIds)
            {
                var users = (from e in session.Linq<LazyUser>()
                             where e.UserId == id
                             select e).ToList();
                foreach (var user in users)
                {
                    string first_name = user.FirstName;
                }
            }
            //foreach (int id in mediumUserIds)
            //{
            //    var users = from e in session.Linq<LazyUser>()
            //                where e.UserId == id
            //                select e;
            //    foreach (var user in users)
            //    {
            //        string name = user.FirstName;
            //    }
            //}
        }


        [QueryTypeAttribute(QueryType.SmallCollection, Syntax.Linq)]
        public void SmallCollectionLinq()
        {
            var users = (from e in session.Linq<User>()
                         select e).Take(Constants.Small).ToList();
            //var users = (from e in session.Linq<User>()
            //             select e).Take(Constants.Small).ToList();
        }
  
        [QueryTypeAttribute(QueryType.LargeCollection, Syntax.Linq)]
        public void LargeCollectionLinq()
        {
            var users = (from e in session.Linq<User>()
                         select e).Take(Constants.Large).ToList();
            //var users = (from e in session.Linq<User>()
            //             select e).Take(Constants.Large).ToList();
        }

        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad, Syntax.Linq)]
        public void SameObjectInCycleLoadLinq()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                var users = (from e in session.Linq<User>()
                             where e.UserId == smallUserIds[0]
                             select e).ToList();
            }
            //for (int i = 0; i < Constants.SmallIteration; i++)
            //{
            //    var users = (from e in session.Linq<User>()
            //                 where e.UserId == smallUserIds[0]
            //                 select e).ToList();
            //}
        }


        [QueryTypeAttribute(QueryType.SelectLargeCollection, Syntax.Linq)]
        public void SelectLargeCollectionLinq()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                var users = (from e in session.Linq<User>() select e).
                    Take(Constants.Large).ToList();
            }
            //for (int i = 0; i < Constants.SmallIteration; i++)
            //{
            //    var users = (from e in session.Linq<User>()
            //                 select e).Take(Constants.Large).ToList();
            //}
        }

        [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess, Syntax.Linq)]
        public void ObjectsWithLoadWithPropertiesAccessLinq()
        {
            var users = (from u in session.Linq<User>()
                         select u).ToList();
            foreach (var user in users)
            {
                string name = user.FirstName;
            }
            //var users = (from u in session.Linq<User>()
            //             select u).ToList();
            //foreach (var user in users)
            //{
            //    string name = user.FirstName;
            //}
        }
        #endregion Linq syntax

        //#region Linq syntax

        
        //[QueryTypeAttribute(QueryType.TypeCycleWithoutLoad, Syntax.Linq)]
        //public void TypeCycleWithoutLoadLinq()
        //{
        //    foreach (int id in mediumUserIds)
        //    {
        //        var users = (from e in session.Linq<User>()
        //                     where e.UserId == id
        //                     select e);
        //    }
        //}

        
        //[QueryTypeAttribute(QueryType.TypeCycleWithLoad, Syntax.Linq)]
        //public void TypeCycleWithLoadLinq()
        //{
        //    foreach (int id in mediumUserIds)
        //    {
        //        var users = (from e in session.Linq<User>()
        //                     where e.UserId == id
        //                     select e).ToList();
        //    }
        //}

        
        //[QueryTypeAttribute(QueryType.TypeCycleLazyLoad, Syntax.Linq)]
        //public void TypeCycleLazyLoadLinq()
        //{
        //    foreach (int id in mediumUserIds)
        //    {
        //        var users = from e in session.Linq<LazyUser>()
        //                    where e.UserId == id
        //                    select e;
        //        foreach (var user in users)
        //        {
        //            string name = user.FirstName;
        //        }
        //    }
        //}

        
        //[QueryTypeAttribute(QueryType.SmallCollection, Syntax.Linq)]
        //public void SmallCollectionLinq()
        //{
        //    var users = (from e in session.Linq<User>()
        //                 select e).Take(Constants.Small).ToList();
        //}

        ////[Ignore]//("Contains()" method in Linq to Entities)
        
        //[QueryTypeAttribute(QueryType.SmallCollectionWithChildrenByIdArray, Syntax.Linq)]
        //public void SmallCollectionWithChildrenByIdArrayLinq()
        //{
        //    //var users = (from e in db.tbl_users
        //    //             where userIds.Contains<int>(e.user_id)
        //    //             from o in e.tbl_phones
        //    //             select new { e.user_id, e.first_name, e.last_name, o.phone_id, o.phone_number }).ToList();

        //}

        ////[Ignore]//("Contains()" method in Linq to Entities)
        
        //[QueryTypeAttribute(QueryType.LargeCollectionWithChildrenByIdArray, Syntax.Linq)]
        //public void LargeCollectionWithChildrenByIdArrayLinq()
        //{
        //    //var users = (from e in db.tbl_users
        //    //             where userIds.Contains<int>(e.user_id)
        //    //             from o in e.tbl_phones
        //    //             select new { e.user_id, e.first_name, e.last_name, o.phone_id, o.phone_number }).ToList();

        //}

        ////[Ignore]//("Contains()" method in Linq to Entities)
        //[QueryTypeAttribute(QueryType.SmallCollectionByIdArray, Syntax.Linq)]
        //public void SmallCollectionByIdArrayLinq()
        //{
        //    var users = (from e in session.Linq<User>()
        //                 where smallUserIds.Contains<int>(e.UserId)
        //                 select e).ToList();
        //}

        ////[Ignore]//("Contains()" method in Linq to Entities)
        
        //[QueryTypeAttribute(QueryType.LargeCollectionByIdArray, Syntax.Linq)]
        //public void LargeCollectionByIdArrayLinq()
        //{
        //    var users = (from e in session.Linq<User>()
        //                 where largeUserIds.Contains<int>(e.UserId)
        //                 select e).ToList();
        //}

        
        //[QueryTypeAttribute(QueryType.LargeCollection, Syntax.Linq)]
        //public void LargeCollectionLinq()
        //{
        //    var users = (from e in session.Linq<User>()
        //                 select e).Take(Constants.Large).ToList();
        //}

        ////[Ignore]//Join is not implemanted
        
        //[QueryTypeAttribute(QueryType.CollectionByPredicateWithoutLoad, Syntax.Linq)]
        //public void CollectionByPredicateWithoutLoadLinq()
        //{
        //    for (int i = 0; i < Constants.LargeIteration; i++)
        //    {
        //        var users = (from u in session.Linq<LazyUser>()
        //                     from p in session.Linq<LazyPhone>()
        //                     where p.PhoneNumber.StartsWith((i + 1).ToString())
        //                     select u);
        //    }
        //}

        ////[Ignore]//Join is not implemanted
        
        //[QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad, Syntax.Linq)]
        //public void CollectionByPredicateWithLoadLinq()
        //{
        //    for (int i = 0; i < Constants.LargeIteration; i++)
        //    {
        //        var users = (from u in session.Linq<User>()
        //                     from p in session.Linq<Phone>()
        //                     where p.PhoneNumber.StartsWith((i + 1).ToString())
        //                     select u).ToList();
        //    }
        //}

        
        //[QueryTypeAttribute(QueryType.SameObjectInCycleLoad, Syntax.Linq)]
        //public void SameObjectInCycleLoadLinq()
        //{
        //    int userId = smallUserIds[0];
        //    for (int i = 0; i < Constants.SmallIteration; i++)
        //    {
        //        var users = (from e in session.Linq<User>()
        //                     where e.UserId == userId
        //                     select e).ToList();
        //    }
        //}

        
        //[QueryTypeAttribute(QueryType.SelectLargeCollection, Syntax.Linq)]
        //public void SelectLargeCollectionLinq()
        //{
        //    for (int i = 0; i < Constants.SmallIteration; i++)
        //    {
        //        var users = (from e in session.Linq<User>()
        //                     select e).Take(Constants.Large).ToList();
        //    }
        //}

        ////[Ignore]//Join is not implemanted
        
        //[QueryTypeAttribute(QueryType.SelectBySamePredicate, Syntax.Linq)]
        //public void SelectBySamePredicateLinq()
        //{
        //    for (int i = 0; i < Constants.SmallIteration; i++)
        //    {
        //        var users = (from u in session.Linq<User>()
        //                     join p in session.Linq<Phone>() on u.UserId equals p.UserId
        //                     where p.PhoneNumber.StartsWith("1")
        //                     select u);
        //    }
        //}

        
        //[QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess, Syntax.Linq)]
        //public void ObjectsWithLoadWithPropertiesAccessLinq()
        //{
        //    var users = (from u in session.Linq<User>()
        //                 select u).ToList();
        //    foreach (var user in users)
        //    {
        //        string name = user.FirstName;
        //    }
        //}
        //#endregion Linq syntax
    }
}
