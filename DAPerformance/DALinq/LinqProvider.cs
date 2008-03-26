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
        public LinqProvider(System.Data.IDbConnection connection)
        {
            db = new DatabaseDataContext(connection);
        }
        
        public void TypeCycleWithoutLoad(int[] userIds)
        {
            foreach (int id in userIds)
            {
                var users = (from e in db.tbl_users
                             where e.user_id == id
                             select e);
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
                var users = from e in db.tbl_users
                             where e.user_id == id
                             select e;
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
                             select u).Distinct();
            }
        }

        public void CollectionByPredicateWithLoad()
        {
            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                var users = (from u in db.tbl_users
                             from p in u.tbl_phones
                             where p.phone_number.StartsWith((i + 1).ToString())
                             select u).Distinct().ToList();
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
                             select u).Distinct();
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

        #region Old

        public void SelectWithoutLoad()
        {
            var query = from e in db.tbl_users
                        where e.user_id == 1
                        select new { e.user_id, e.first_name, e.last_name };

        }

        public void SelectShortWithoutLoad()
        {
            var query = db.tbl_users.Where(c => c.user_id == 1);
        }

        public void SelectShortWithListLoad()
        {
            var query = db.tbl_users.Where(c => c.user_id == 1).ToList();
        }

        public void SelectShortWithLoad()
        {
            var query = db.tbl_users.Where(c => c.user_id == 1);
            foreach (var user in query)
            {
                string result = string.Format("{0} {1} {2}", user.user_id, user.first_name, user.last_name);
            }
        }

        public void SelectWithLoad()
        {
            var users = (from e in db.tbl_users
                         where e.user_id == 1
                         select new { e.user_id, e.first_name, e.last_name });
            foreach (var user in users)
            {
                string result = string.Format("{0} {1} {2}", user.user_id, user.first_name, user.last_name);
            }
        }

        public void SelectWithListLoad()
        {
            var users = (from e in db.tbl_users
                         where e.user_id == 1
                         select new { e.user_id, e.first_name, e.last_name }).ToList();
        }

        public void SelectCollectionWithoutLoad()
        {
            var query = from e in db.tbl_users select new { e.user_id, e.first_name, e.last_name };
        }

        public void SelectCollectionShortWithoutLoad()
        {
            var query = db.tbl_users;
        }

        public void SelectCollectionShortWithListLoad()
        {
            var query = db.tbl_users.ToList();
        }

        public void SelectCollectionShortWithLoad()
        {
            var query = db.tbl_users;
            foreach (var user in query)
            {
                string result = string.Format("{0} {1} {2}", user.user_id, user.first_name, user.last_name);
            }
        }

        public void SelectCollectionWithLoad()
        {
            var query = from e in db.tbl_users
                        select new { e.user_id, e.first_name, e.last_name };

            foreach (var user in query)
            {
                string result = string.Format("{0} {1} {2}", user.user_id, user.first_name, user.last_name);
            }
        }

        public void SelectCollectionWithListLoad()
        {
            List<tbl_user> users = db.tbl_users.ToList();
        }

        public void SelectSmallCollectionWithoutLoad()
        {
            var phones = db.tbl_phones;
        }

        public void SelectSmallCollectionWithListLoad()
        {
            var phones = db.tbl_phones.ToList();
        }

        public void SelectSmallCollectionWithLoad()
        {
            var phones = db.tbl_phones;
            foreach (var phone in phones)
            {
                string result = string.Format("{0} {1} {2}", phone.phone_id, phone.user_id, phone.phone_number);
            }
        }

        public void SelectSmallWithoutLoad()
        {
            var phones = (from e in db.tbl_phones
                          where e.phone_id == 1
                          select new { e.phone_id, e.user_id, e.phone_number });
        }

        public void SelectSmallWithListLoad()
        {
            var phones = (from e in db.tbl_phones
                          where e.phone_id == 1
                          select new { e.phone_id, e.user_id, e.phone_number }).ToList();

        }

        public void SelectSmallWithLoad()
        {
            var phones = (from e in db.tbl_phones
                          where e.phone_id == 1
                          select new { e.phone_id, e.user_id, e.phone_number });
            foreach (var phone in phones)
            {
                string result = string.Format("{0} {1} {2}", phone.phone_id, phone.user_id, phone.phone_number);
            }
        }

        #endregion Old

    }
}
