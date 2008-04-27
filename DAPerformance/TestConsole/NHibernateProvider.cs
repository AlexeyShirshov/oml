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
                User u = session.Get<User>(id);
                //IList results = session.CreateCriteria(typeof(User))
                //    .SetProjection(Projections.Property("UserId"))
                //    .Add(Expression.Eq("UserId", id))
                //    .List();
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
                IList results = session.CreateCriteria(typeof(User))
                   .SetProjection(Projections.Property("UserId"))
                   .Add(Expression.Eq("UserId", id))
                   .List();
                foreach (int userId in results)
                {
                    IList name = session.CreateCriteria(typeof(User))
                    .SetProjection(Projections.Property("FirstName"))
                    .Add(Expression.Eq("UserId", userId))
                    .List();
                }
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
                IList results = session.CreateCriteria(typeof(User))
                   .SetProjection(Projections.Property("UserId"))
                   .Add(Expression.Eq("UserId", i))
                   .List();
                
            }
        }

        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad)]
        public void CollectionByPredicateWithLoad()
        {

            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                IList results = session.CreateCriteria(typeof(User)).Add(Expression.Eq("UserId", i)).List();  
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
                IList results = session.CreateCriteria(typeof(User))
                    .SetProjection(Projections.Property("UserId"))
                    .Add(Expression.Eq("UserId", 1))
                    .List();
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
                //var ids = (from e in session.Linq<LazyUser>()
                //           where e.UserId == id
                //           select e.UserId).ToList();
                LazyUser ur = session.Linq<LazyUser>().Single<LazyUser>(u => u.UserId == id);
            }
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
        }


        [QueryTypeAttribute(QueryType.TypeCycleLazyLoad, Syntax.Linq)]
        public void TypeCycleLazyLoadLinq()
        {
            foreach (int id in mediumUserIds)
            {
                var ids = (from e in session.Linq<LazyUser>()
                           where e.UserId == id
                           select e.UserId).ToList();
                foreach (int userId in ids)
                {
                    var first_name = (from e in session.Linq<LazyUser>()
                                      where e.UserId == userId
                                      select e.FirstName).ToList();
                }
            }
        }


        [QueryTypeAttribute(QueryType.CollectionByPredicateWithoutLoad, Syntax.Linq)]
        public void CollectionByPredicateWithoutLoadLinq()
        {
            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                var users = (from e in session.Linq<LazyUser>()
                             where e.UserId == i
                             select e.UserId).ToList();
            }
        }

        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad, Syntax.Linq)]
        public void CollectionByPredicateWithLoadLinq()
        {

            for (int i = 0; i < Constants.LargeIteration; i++)
            {
               
                var users = (from e in session.Linq<LazyUser>()
                             where e.UserId == i
                             select e).ToList();
            }
        }



        [QueryTypeAttribute(QueryType.SmallCollection, Syntax.Linq)]
        public void SmallCollectionLinq()
        {
            var users = (from e in session.Linq<User>()
                         select e).Take(Constants.Small).ToList();
        }
  
        [QueryTypeAttribute(QueryType.LargeCollection, Syntax.Linq)]
        public void LargeCollectionLinq()
        {
            var users = (from e in session.Linq<User>()
                         select e).Take(Constants.Large).ToList();
        }

        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad, Syntax.Linq)]
        public void SameObjectInCycleLoadLinq()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                var users = (from e in session.Linq<User>()
                             where e.UserId == 1
                             select e).ToList();
            }
        }


        [QueryTypeAttribute(QueryType.SelectLargeCollection, Syntax.Linq)]
        public void SelectLargeCollectionLinq()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                var users = (from e in session.Linq<User>() select e).
                    Take(Constants.Large).ToList();
            }
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
        }

        [QueryTypeAttribute(QueryType.SelectBySamePredicate, Syntax.Linq)]
        public void SelectBySamePredicateLinq()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                var users = (from e in session.Linq<User>()
                             where e.UserId == 1
                             select e.UserId).ToList();
            }
        }
        #endregion Linq syntax
       
    }
}
