using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

using Worm.Cache;
using Worm.Database;
using Worm.Database.Criteria.Joins;
using Worm.Orm;
using Worm.Orm.Meta;

using Common;

namespace DAWorm
{
    public class WormProvider
    {
         static SQLGenerator _schema;
        static WormProvider wormProvider;
        int[] smallUserIds;
        int[] mediumUserIds;
        int[] largeUserIds;

        protected static SQLGenerator GetSchema()
        {
            if (_schema == null)
                _schema = new SQLGenerator("1");
            return _schema;
        }

        protected static OrmCache GetCache()
        {
            return new OrmCache();
        }


        OrmReadOnlyDBManager manager;       

        public WormProvider(int[] smallUserIds, int[] mediumUserIds, int[] largeUserIds)
        {
            this.smallUserIds = smallUserIds;
            this.mediumUserIds = mediumUserIds;
            this.largeUserIds = largeUserIds;

            manager = new OrmDBManager(GetCache(), GetSchema(), ConfigurationSettings.AppSettings["ConnectionStringBase"]);
            // Opens connection 
            ICollection<User> users = manager.FindTop<User>(10000, null, null, false);
        }


        public void SetDefaultContext()
        {
             manager = new OrmDBManager(GetCache(), GetSchema(), ConfigurationSettings.AppSettings["ConnectionStringBase"]);
        }

        public void ClearContext()
        {
            manager.Dispose();
            manager = null;
        }
     
        #region new
        [QueryTypeAttribute(QueryType.TypeCycleWithoutLoad)]
        public void TypeCycleWithoutLoad()
        {
            TypeCycleWithoutLoad(mediumUserIds);
        }

        [QueryTypeAttribute(QueryType.TypeCycleWithLoad)]
        public void TypeCycleWithLoad()
        {
            TypeCycleWithLoad(mediumUserIds);
        }

        [QueryTypeAttribute(QueryType.TypeCycleLazyLoad)]
        public void TypeCycleLazyLoad()
        {
            TypeCycleLazyLoad(mediumUserIds);
        }

        [QueryTypeAttribute(QueryType.SmallCollectionByIdArray)]
        public void SmallCollectionByIdArray()
        {
            CollectionByIdArray(smallUserIds);
        }

        [QueryTypeAttribute(QueryType.SmallCollection)]
        public void SmallCollection()
        {
            GetCollection(Constants.Small);
        }

        [QueryTypeAttribute(QueryType.SmallCollectionWithChildrenByIdArray)]
        public void SmallCollectionWithChildrenByIdArray()
        {
            CollectionWithChildrenByIdArray(smallUserIds);
        }

        [QueryTypeAttribute(QueryType.LargeCollectionByIdArray)]
        public void LargeCollectionByIdArray()
        {
            CollectionByIdArray(largeUserIds);
        }

        [QueryTypeAttribute(QueryType.LargeCollection)]
        public void LargeCollection()
        {
            GetCollection(Constants.Large);
        }

        [QueryTypeAttribute(QueryType.LargeCollectionWithChildrenByIdArray)]
        public void LargeCollectionWithChildrenByIdArray()
        {
            CollectionWithChildrenByIdArray(largeUserIds);
        }

        [QueryTypeAttribute(QueryType.CollectionByPredicateWithoutLoad)]
        public void CollectionByPredicateWithoutLoad()
        {
            CollectionByPredicateWithoutLoad(Constants.LargeIteration);
        }

        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad)]
        public void CollectionByPredicateWithLoad()
        {
            CollectionByPredicateWithLoad(Constants.LargeIteration);
        }

        [QueryTypeAttribute(QueryType.SelectLargeCollection)]
        public void SelectLargeCollection()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                GetCollection(Constants.Large);
            }
        }

        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad)]
        public void SameObjectInCycleLoad()
        {
            SameObjectInCycleLoad(Constants.SmallIteration, smallUserIds[0]);
        }

        [QueryTypeAttribute(QueryType.SelectBySamePredicate)]
        public void SelectBySamePredicate()
        {
            SelectBySamePredicate(Constants.SmallIteration);
        }

        [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess)]
        public void ObjectsWithLoadWithPropertiesAccess()
        {
            ICollection<User> users = manager.FindTop<User>(10000, null, null, true);
            foreach (User user in users)
            {
                string name = user.First_name;
            }
        }

        public void TypeCycleWithoutLoad(int[] userIds)
        {
            foreach (int id in userIds)
            {
                User user = new User(id, manager.Cache, manager.DbSchema);
            }
        }

        public void TypeCycleWithLoad(int[] userIds)
        {
            foreach (int id in userIds)
            {
                User user = new User(id, manager.Cache, manager.DbSchema);
                user.Load();
            }
        }

        public void TypeCycleLazyLoad(int[] userIds)
        {
            foreach (int id in userIds)
            {
                User user = new User(id, manager.Cache, manager.DbSchema);
                string name = user.First_name;
            }
        }

        public void GetCollection(int count)
        {
            ICollection<User> users = manager.FindTop<User>(count, null, null, true);
        }

        public void CollectionWithChildrenByIdArray(int[] userIds)
        {
            Type tp = typeof(Phone);
            ICollection<User> users = manager.ConvertIds2Objects<User>(userIds, false);
            ICollection<Phone> phones = manager.FindJoin<Phone>(typeof(User), "User_id",
                Worm.Database.Criteria.Ctor.Field(tp, Phone.Properties.User_id).In(userIds),
                null, true);   
        }

        public void CollectionByIdArray(int[] userIds)
        {
            ICollection<User> users = manager.ConvertIds2Objects<User>(userIds, false);
        }

        public void CollectionByPredicateWithoutLoad(int iterationCount)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                string id = (i + 1).ToString() + "%";
                Type tp = typeof(Phone);
                ICollection<User> users = manager.FindJoin<User>(tp, "ID",
                    Worm.Database.Criteria.Ctor.Field(tp, Phone.Properties.Phone_number).Like(id), null, false);   
            }
        }

        public void CollectionByPredicateWithLoad(int iterationCount)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                string id = (i + 1).ToString() + "%";
                Type tp = typeof(Phone);
                ICollection<User> users = manager.FindJoin<User>(tp, "ID",
                    Worm.Database.Criteria.Ctor.Field(tp, Phone.Properties.Phone_number).Like(id), null, true);   

                //manager.Find<User>(Worm.Database.Criteria.Ctor.Field(tp,Phone.Properties.Phone_number).Like("1%"), null, false);
            }
        }

        public void SameObjectInCycleLoad(int iterationCount, int userId)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                User user = new User(userId, manager.Cache, manager.DbSchema);
                user.Load();
            }
        }

        public void SelectBySamePredicate(int iterationCount)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                Type tp = typeof(Phone);
                ICollection<User> users = manager.FindJoin<User>(tp, "ID",
                    Worm.Database.Criteria.Ctor.Field(tp, Phone.Properties.Phone_number).Like("1%"), null, false); 
            }
        }

       
        #endregion new

        #region old
        //public void SelectWithoutLoad()
        //{
        //    OrmCache cache = new OrmCache();
        //    using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, schema, _connectionString))
        //    {
        //        //User user = new User(1, cache, schema);
        //        //user.Load();
        //        User user = manager.Find<User>(1);
        //       // ICollection<User> users = manager.FindTop<User>(10000, null, null, false);
        //    }
        //}


        //public void SelectCollectionWithoutLoad()
        //{
        //    Worm.PerfCounter p = new Worm.PerfCounter();
        //    OrmCache cache = new OrmCache();
        //    using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, Schema, _connectionString))
        //    {
        //        ICollection<User> users = manager.FindTop<User>(10000, null, null, false);
        //    }
        //    System.Diagnostics.Debug.WriteLine(p.GetTime());
        //}

        //public void SelectWithLoad()
        //{
        //    OrmCache cache = new OrmCache();
        //    using (OrmReadOnlyDBManager manager = new OrmDBManager(new OrmCache(), Schema, _connectionString))
        //    {
        //        User user = new User(1, cache, Schema);
        //        user.Load();
        //    }
        //}

        //public void SelectCollectionWithLoad()
        //{
        //    using (OrmReadOnlyDBManager manager = new OrmDBManager(new OrmCache(), Schema, _connectionString))
        //    {
        //        ICollection<User> users = manager.FindTop<User>(10000, null, null, true);
        //    }
        //}

        //public void SelectSmallWithoutLoad()
        //{
        //    OrmCache cache = new OrmCache();
        //    using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, Schema, _connectionString))
        //    {
        //        Phone phone = manager.Find<Phone>(1);
        //    }
        //}

        //public void SelectSmallCollectionWithoutLoad()
        //{
        //    OrmCache cache = new OrmCache();
        //    using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, Schema, _connectionString))
        //    {
        //        ICollection<Phone> phones = manager.FindTop<Phone>(1000, null, null, false);
        //    }
        //}

        //public void SelectSmallWithLoad()
        //{
        //    OrmCache cache = new OrmCache();
        //    using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, Schema, _connectionString))
        //    {
        //        Phone phone = new Phone(1, cache, Schema);
        //        phone.Load();
        //    }
        //}

        //public void SelectSmallCollectionWithLoad()
        //{
        //    using (OrmReadOnlyDBManager manager = new OrmDBManager(new OrmCache(), Schema, _connectionString))
        //    {
        //        ICollection<Phone> phones = manager.FindTop<Phone>(1000, null, null, true);
        //    }
        //}
        #endregion old
    }
}
