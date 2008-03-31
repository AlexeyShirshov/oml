using System;
using System.Collections.Generic;
using System.Text;

using Worm.Cache;
using Worm.Database;
using Worm.Database.Criteria.Joins;
using Worm.Orm;
using Worm.Orm.Meta;

namespace DAWorm
{
    public class WormProvider
    {
        OrmReadOnlyDBManager manager;       

        public WormProvider(OrmReadOnlyDBManager manager)
        {
            this.manager = manager;
           
            // Opens connection ?
            ICollection<User> users = manager.FindTop<User>(10000, null, null, false);
        }

        public OrmReadOnlyDBManager Manager
        {
            get { return manager;  }
            set { manager = value;  }
        }
     
        #region new
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

        public void ObjectsWithLoadWithPropertiesAccess()
        {
            ICollection<User> users = manager.FindTop<User>(10000, null, null, true);
            foreach (User user in users)
            {
                string name = user.First_name;
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
