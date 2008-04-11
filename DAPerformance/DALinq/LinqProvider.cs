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
        
         int[] smallUserIds;
         int[] mediumUserIds;
         int[] largeUserIds;

        public LinqProvider(System.Data.IDbConnection connection, int[] smallUserIds, int[] mediumUserIds, int[] largeUserIds)
        {
            this.connection = connection;
            this.smallUserIds = smallUserIds;
            this.mediumUserIds = mediumUserIds;
            this.largeUserIds = largeUserIds;

            CreateNewDatabaseDataContext();
            var users = (from e in db.tbl_users
                         where e.user_id == 1
                         select e).ToList();
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

        [QueryTypeAttribute(QueryType.TypeCycleWithoutLoad)]
        public void TypeCycleWithoutLoad()
        {
            LoadingEnabled = true;
            TypeCycleWithoutLoad(mediumUserIds);
        }

        [QueryTypeAttribute(QueryType.TypeCycleWithLoad)]
        public void TypeCycleWithLoad()
        {
            LoadingEnabled = false;
            TypeCycleWithLoad(mediumUserIds);
        }

        [QueryTypeAttribute(QueryType.TypeCycleLazyLoad)]
        public void TypeCycleLazyLoad()
        {
            LoadingEnabled = true;
            TypeCycleLazyLoad(mediumUserIds);
        }

        [QueryTypeAttribute(QueryType.SmallCollectionByIdArray)]
        public void SmallCollectionByIdArray()
        {
            LoadingEnabled = false;
            CollectionByIdArray(smallUserIds);
        }

        [QueryTypeAttribute(QueryType.SmallCollection)]
        public void SmallCollection()
        {
            LoadingEnabled = false;
            GetCollection(Constants.Small);
        }

        [QueryTypeAttribute(QueryType.SmallCollectionWithChildrenByIdArray)]
        public void SmallCollectionWithChildrenByIdArray()
        {
            LoadingEnabled = false;
            CollectionWithChildrenByIdArray(smallUserIds);
        }

        [QueryTypeAttribute(QueryType.LargeCollectionByIdArray)]
        public void LargeCollectionByIdArray()
        {
            LoadingEnabled = false;
            CollectionByIdArray(largeUserIds);
        }

        [QueryTypeAttribute(QueryType.LargeCollection)]
        public void LargeCollection()
        {
            LoadingEnabled = false;
            GetCollection(Constants.Large);
        }

        [QueryTypeAttribute(QueryType.LargeCollectionWithChildrenByIdArray)]
        public void LargeCollectionWithChildrenByIdArray()
        {
            LoadingEnabled = false;
            CollectionWithChildrenByIdArray(largeUserIds);
        }

        [QueryTypeAttribute(QueryType.CollectionByPredicateWithoutLoad)]
        public void CollectionByPredicateWithoutLoad()
        {
            LoadingEnabled = true;
            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                var users = (from u in db.tbl_users
                             from p in u.tbl_phones
                             where p.phone_number.StartsWith((i + 1).ToString())
                             select u.user_id).ToList();
            }
        }

        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad)]
        public void CollectionByPredicateWithLoad()
        {
            LoadingEnabled = false;
            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                var users = (from u in db.tbl_users
                             from p in u.tbl_phones
                             where p.phone_number.StartsWith((i + 1).ToString())
                             select u)./*Distinct().*/ToList();
            }
        }

        [QueryTypeAttribute(QueryType.SelectLargeCollection)]
        public void SelectLargeCollection()
        {
            LoadingEnabled = false;
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                GetCollection(Constants.Large);
            }
        }

        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad)]
        public void SameObjectInCycleLoad()
        {
            LoadingEnabled = false;
            SameObjectInCycleLoad(smallUserIds[0]);
        }

        [QueryTypeAttribute(QueryType.SelectBySamePredicate)]
        public void SelectBySamePredicate()
        {
            LoadingEnabled = true;
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                var users = (from u in db.tbl_users
                             from p in u.tbl_phones
                             where p.phone_number.StartsWith("1")
                             select u).ToList();
            }
        }

        [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess)]
        public void ObjectsWithLoadWithPropertiesAccess()
        {
            LoadingEnabled = false;
            var users = (from u in db.tbl_users
                         select u).ToList();
            foreach (var user in users)
            {
                string name = user.first_name;
            }
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

    
        public void SameObjectInCycleLoad(int userId)
        {
           for (int i = 0; i < Constants.SmallIteration; i++)
            {
                var users = (from e in db.tbl_users
                             where e.user_id == userId
                             select e).ToList();
            }
        }

      

    
    }
}
