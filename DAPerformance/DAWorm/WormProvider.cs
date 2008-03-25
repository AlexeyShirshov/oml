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
            ICollection<Tbl_user> users = manager.ConvertIds2Objects<Tbl_user>(userIds, false);
            ICollection<Tbl_phone> phones = manager.FindJoin<Tbl_phone>(typeof(Tbl_user), "User_id", null, null, true);   
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
                //Worm.Database.Criteria.Core.EntityFilter filter = new Worm.Database.Criteria.Core.EntityFilter
                //    (typeof(Tbl_phone), "Phone_number", new Worm.Criteria.Values.LiteralValue(id), Worm.Criteria.FilterOperation.Like);
                ICollection<Tbl_user> users = manager.FindJoin<Tbl_user>(typeof(Tbl_phone), "User_id", null, null, false);   

                //OrmTable tbl = manager.DbSchema.GetTables(typeof(Tbl_user))[0];
                //JoinFilter filter = new JoinFilter((tbl, "User_id", typeof(c), "Phone_number", Worm.Criteria.FilterOperation.Like);
                //OrmJoin join = new OrmJoin(tbl, Worm.Criteria.Joins.JoinType.Join, filter);
                //OrmJoin[] joins = new OrmJoin[] { join };
                //ICollection<Tbl_user> users = manager.FindWithJoins<Tbl_user>(null, joins, null, null, false);

                // Relation DAWorm.Tbl_user to DAWorm.Tbl_phone is ambiguous or not exist. Use FindJoin method.
                //string id = (i + 1).ToString() + "%";
                //ICollection<Tbl_user> users = manager.Find<Tbl_user>(
                //    Worm.Database.Criteria.Ctor.Field(typeof(Tbl_phone), "Phone_number").Like(id), null, false);
            }
        }

        public void CollectionByPredicateWithLoad(int iterationCount)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                //OrmTable tbl = manager.DbSchema.GetTables(typeof(Tbl_user))[0];
                //JoinFilter filter = new JoinFilter(tbl, "user_id", typeof(Tbl_phone), "user_id", Worm.Criteria.FilterOperation.Equal);
                //OrmJoin join = new OrmJoin(tbl, Worm.Criteria.Joins.JoinType.Join, filter);
                //OrmJoin[] joins = new OrmJoin[] { join };
                //ICollection<Tbl_user> users = manager.FindWithJoins<Tbl_user>(null, joins, null, null, true);
                Type tp = typeof(Tbl_phone);
                manager.Find<Tbl_user>(Worm.Database.Criteria.Ctor.Field(tp,Tbl_phone.Properties.Phone_number).Like("1%"), null, false);
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
                //var users = (from u in db.tbl_users
                //             from p in u.tbl_phones
                //             where p.phone_number.StartsWith("1")
                //             select u);
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
