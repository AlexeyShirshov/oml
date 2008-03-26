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
            ICollection<Tbl_user> users = manager.FindTop<Tbl_user>(10000, null, null, false);
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
                Tbl_user user = new Tbl_user(id, manager.Cache, manager.DbSchema);
            }
        }

        public void TypeCycleWithLoad(int[] userIds)
        {
            foreach (int id in userIds)
            {
                Tbl_user user = new Tbl_user(id, manager.Cache, manager.DbSchema);
                user.Load();
            }
        }

        public void TypeCycleLazyLoad(int[] userIds)
        {
            foreach (int id in userIds)
            {
                Tbl_user user = new Tbl_user(id, manager.Cache, manager.DbSchema);
                string name = user.First_name;
            }
        }

        public void GetCollection(int count)
        {
            ICollection<Tbl_user> users = manager.FindTop<Tbl_user>(count, null, null, true);
        }

        public void CollectionWithChildrenByIdArray(int[] userIds)
        {
            Type tp = typeof(Tbl_phone);
            ICollection<Tbl_user> users = manager.ConvertIds2Objects<Tbl_user>(userIds, false);
            ICollection<Tbl_phone> phones = manager.FindJoin<Tbl_phone>(typeof(Tbl_user), "User_id",
                Worm.Database.Criteria.Ctor.Field(tp, Tbl_phone.Properties.User_id).In(userIds),
                null, true);   
        }

        public void CollectionByIdArray(int[] userIds)
        {
            ICollection<Tbl_user> users = manager.ConvertIds2Objects<Tbl_user>(userIds, false);
        }

        public void CollectionByPredicateWithoutLoad(int iterationCount)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                string id = (i + 1).ToString() + "%";
                Type tp = typeof(Tbl_phone);
                ICollection<Tbl_user> users = manager.FindJoin<Tbl_user>(tp, "ID",
                    Worm.Database.Criteria.Ctor.Field(tp, Tbl_phone.Properties.Phone_number).Like(id), null, false);   
            }
        }

        public void CollectionByPredicateWithLoad(int iterationCount)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                string id = (i + 1).ToString() + "%";
                Type tp = typeof(Tbl_phone);
                ICollection<Tbl_user> users = manager.FindJoin<Tbl_user>(tp, "ID",
                    Worm.Database.Criteria.Ctor.Field(tp, Tbl_phone.Properties.Phone_number).Like(id), null, true);   

                //manager.Find<Tbl_user>(Worm.Database.Criteria.Ctor.Field(tp,Tbl_phone.Properties.Phone_number).Like("1%"), null, false);
            }
        }

        public void SameObjectInCycleLoad(int iterationCount, int userId)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                Tbl_user user = new Tbl_user(userId, manager.Cache, manager.DbSchema);
                user.Load();
            }
        }

        public void SelectBySamePredicate(int iterationCount)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                Type tp = typeof(Tbl_phone);
                ICollection<Tbl_user> users = manager.FindJoin<Tbl_user>(tp, "ID",
                    Worm.Database.Criteria.Ctor.Field(tp, Tbl_phone.Properties.Phone_number).Like("1%"), null, false); 
            }
        }

        public void ObjectsWithLoadWithPropertiesAccess()
        {
            ICollection<Tbl_user> users = manager.FindTop<Tbl_user>(10000, null, null, true);
            foreach (Tbl_user user in users)
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
        //        //Tbl_user user = new Tbl_user(1, cache, schema);
        //        //user.Load();
        //        Tbl_user user = manager.Find<Tbl_user>(1);
        //       // ICollection<Tbl_user> users = manager.FindTop<Tbl_user>(10000, null, null, false);
        //    }
        //}


        //public void SelectCollectionWithoutLoad()
        //{
        //    Worm.PerfCounter p = new Worm.PerfCounter();
        //    OrmCache cache = new OrmCache();
        //    using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, Schema, _connectionString))
        //    {
        //        ICollection<Tbl_user> users = manager.FindTop<Tbl_user>(10000, null, null, false);
        //    }
        //    System.Diagnostics.Debug.WriteLine(p.GetTime());
        //}

        //public void SelectWithLoad()
        //{
        //    OrmCache cache = new OrmCache();
        //    using (OrmReadOnlyDBManager manager = new OrmDBManager(new OrmCache(), Schema, _connectionString))
        //    {
        //        Tbl_user user = new Tbl_user(1, cache, Schema);
        //        user.Load();
        //    }
        //}

        //public void SelectCollectionWithLoad()
        //{
        //    using (OrmReadOnlyDBManager manager = new OrmDBManager(new OrmCache(), Schema, _connectionString))
        //    {
        //        ICollection<Tbl_user> users = manager.FindTop<Tbl_user>(10000, null, null, true);
        //    }
        //}

        //public void SelectSmallWithoutLoad()
        //{
        //    OrmCache cache = new OrmCache();
        //    using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, Schema, _connectionString))
        //    {
        //        Tbl_phone phone = manager.Find<Tbl_phone>(1);
        //    }
        //}

        //public void SelectSmallCollectionWithoutLoad()
        //{
        //    OrmCache cache = new OrmCache();
        //    using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, Schema, _connectionString))
        //    {
        //        ICollection<Tbl_phone> phones = manager.FindTop<Tbl_phone>(1000, null, null, false);
        //    }
        //}

        //public void SelectSmallWithLoad()
        //{
        //    OrmCache cache = new OrmCache();
        //    using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, Schema, _connectionString))
        //    {
        //        Tbl_phone phone = new Tbl_phone(1, cache, Schema);
        //        phone.Load();
        //    }
        //}

        //public void SelectSmallCollectionWithLoad()
        //{
        //    using (OrmReadOnlyDBManager manager = new OrmDBManager(new OrmCache(), Schema, _connectionString))
        //    {
        //        ICollection<Tbl_phone> phones = manager.FindTop<Tbl_phone>(1000, null, null, true);
        //    }
        //}
        #endregion old
    }
}
