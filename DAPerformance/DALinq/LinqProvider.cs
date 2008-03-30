using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using System.Data.SqlClient;

namespace DALinq
{
    public class LinqProvider
    {
        DatabaseDataContext db;
        System.Data.IDbConnection connection;

        public LinqProvider(System.Data.IDbConnection connection)
        {
            this.connection = connection;
        }

        public void CreateNewDatabaseDataContext()
        {
            db = new DatabaseDataContext(connection);
        }

        public bool LoadingEnabled
        {
            get { return db.DeferredLoadingEnabled; }
            set { db.DeferredLoadingEnabled = value; }
        }
        
        public void TypeCycleWithoutLoad(int[] userIds)
        {
            foreach (int id in userIds)
            {
                var users = (from e in db.tbl_users
                             where e.user_id == id
                             select e).ToList() ;
            }
        }

        public void TypeCycleWithLoad(int[] userIds)
        {
            foreach (int id in userIds)
            {
                var users = (from e in db.tbl_users
                             where e.user_id == id
                             select e).ToList();
            }
        }

        public void TypeCycleLazyLoad(int[] userIds)
        {
            foreach (int id in userIds)
            {
                var users = (from e in db.tbl_users
                             where e.user_id == id
                            select e).ToList();
                foreach (var user in users)
                {
                    string name = user.first_name;
                }
            }
        }

        public void GetCollection(int count)
        {
            var users = (from e in db.tbl_users
                        select e).Take(count).ToList();
        }

        public void CollectionWithChildrenByIdArray(int[] userIds)
        {
            var users = (from e in db.tbl_users
                         where userIds.Contains<int>(e.user_id)
                         from o in e.tbl_phones
                         select new { e.user_id, e.first_name, e.last_name, o.phone_id, o.phone_number }).ToList();

        }

        public void CollectionByIdArray(int[] userIds)
        {
            var users = (from e in db.tbl_users
                         where userIds.Contains<int>(e.user_id)
                         select e).ToList();
        }

        public void CollectionByPredicateWithoutLoad()
        {
            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                var users = (from u in db.tbl_users
                             from p in u.tbl_phones
                             where p.phone_number.StartsWith((i + 1).ToString())
                             select u.user_id).ToList();
            }
        }

        public void CollectionByPredicateWithLoad()
        {
            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                var users = (from u in db.tbl_users
                             from p in u.tbl_phones
                             where p.phone_number.StartsWith((i + 1).ToString())
                             select u)./*Distinct().*/ToList();
            }
        }

        public void SameObjectInCycleLoad(int userId)
        {
           for (int i = 0; i < Constants.SmallIteration; i++)
            {
                var users = (from e in db.tbl_users
                             where e.user_id == userId
                             select e).ToList();
            }
        }

        public void SelectBySamePredicate()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                var users = (from u in db.tbl_users
                             from p in u.tbl_phones
                             where p.phone_number.StartsWith("1")
                             select u).ToList();
            }
        }

        public void ObjectsWithLoadWithPropertiesAccess()
        {
            var users = (from u in db.tbl_users
                          select u).ToList();
            foreach (var user in users)
            {
                string name = user.first_name;
            }
        }
    }
}
