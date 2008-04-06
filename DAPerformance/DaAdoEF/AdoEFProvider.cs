using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Data.Common;
using System.Data.EntityClient;
using System.Linq;
using System.Text;

using TestDAModel;

namespace DaAdoEF
{
    public class AdoEFProvider
    {
        TestDAEntities entities;
        EntityConnection connection;
        
        public AdoEFProvider(string connectionString)
        {
            connection = new EntityConnection(connectionString);
            connection.Open();
            entities = new TestDAEntities(connection);
        }

        public void Dispose()
        {
            entities.Dispose();
        }

        #region Default syntax

        public void TypeCycleWithoutLoad(int[] userIds)
        {
            foreach (int id in userIds)
            {
                ObjectQuery<tbl_user> query = entities.tbl_user.Where("it.user_id = @user_id", new ObjectParameter("user_id", id));                
            }
        }

        public void TypeCycleWithLoad(int[] userIds)
        {
            foreach (int id in userIds)
            {
                List<tbl_user> user = entities.tbl_user.Where("it.user_id = @user_id", new ObjectParameter("user_id", id)).ToList<tbl_user>();
            }
        }

        public void TypeCycleLazyLoad(int[] userIds)
        {
            foreach (int id in userIds)
            {
                ObjectQuery<tbl_user> query = entities.tbl_user.Where("it.user_id = @user_id", new ObjectParameter("user_id", id));
                string first_name = query.First().first_name;
            }
        }

        public void GetCollection(int count)
        {
            List<tbl_user> users = entities.tbl_user.Take(count).ToList();
        }


        public void SameObjectInCycleLoad(int iterationCount, int userId)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                List<tbl_user> user = entities.tbl_user.Where("it.user_id = @user_id", new ObjectParameter("user_id", userId)).ToList<tbl_user>();
            }
        }

        public void CollectionByPredicateWithoutLoad(int iterationCount)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                string id = (i + 1).ToString();
                var users = entities.tbl_user.Join(entities.tbl_phone,
                    u => u.user_id, p => p.user_id, (u, p) => new {U = u, P = p}).
                        Where(jn => jn.P.phone_number.StartsWith(id)).
                        Select(jn => jn.U).Distinct();
            }
        }

        public void CollectionByPredicateWithLoad(int iterationCount)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                string id = (i + 1).ToString();
                var users = entities.tbl_user.Join(entities.tbl_phone,
                    u => u.user_id, p => p.user_id, (u, p) => new { U = u, P = p }).
                        Where(jn => jn.P.phone_number.StartsWith(id)).
                        Select(jn => jn.U).Distinct().ToList();
            }
        }



        public void SelectBySamePredicate(int iterationCount)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                var users = entities.tbl_user.Join(entities.tbl_phone,
                    u => u.user_id, p => p.user_id, (u, p) => new { U = u, P = p }).
                        Where(jn => jn.P.phone_number.StartsWith("1")).
                        Select(jn => jn.U).Distinct();
            }
        }

        public void ObjectsWithLoadWithPropertiesAccess()
        {
            List<tbl_user> users = entities.tbl_user.ToList();
            foreach (tbl_user user in users)
            {
                string name = user.first_name;
            }
        }

        #endregion Default Syntax

        #region Linq syntax

        public void TypeCycleWithoutLoadLinq(int[] userIds)
        {
            foreach (int id in userIds)
            {
                var users = (from e in entities.tbl_user
                             where e.user_id == id
                             select e);
            }
        }

        public void TypeCycleWithLoadLinq(int[] userIds)
        {
            foreach (int id in userIds)
            {
                var users = (from e in entities.tbl_user
                             where e.user_id == id
                             select e).ToList();
            }
        }

        public void TypeCycleLazyLoadLinq(int[] userIds)
        {
            foreach (int id in userIds)
            {
                var users = from e in entities.tbl_user
                            where e.user_id == id
                            select e;
                foreach (var user in users)
                {
                    string name = user.first_name;
                }
            }
        }

        public void GetCollectionLinq(int count)
        {
            var users = (from e in entities.tbl_user
                         select e).Take(count).ToList();
        }

        public void CollectionWithChildrenByIdArrayLinq(int[] userIds)
        {
            var users = (from e in entities.tbl_user
                         where userIds.Contains<int>(e.user_id)
                         from o in e.tbl_phone
                         select new { e.user_id, e.first_name, e.last_name, o.phone_id, o.phone_number }).ToList();

        }

        public void CollectionByIdArrayLinq(int[] userIds)
        {
            var users = (from e in entities.tbl_user
                         where userIds.Contains<int>(e.user_id)
                         select e).ToList();
        }

        public void CollectionByPredicateWithoutLoadLinq(int iterationCount)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                string id = (i + 1).ToString();
                var users = (from u in entities.tbl_user
                             from p in u.tbl_phone
                             where p.phone_number.StartsWith(id)
                             select u).Distinct();
            }
        }

        public void CollectionByPredicateWithLoadLinq(int iterationCount)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                string id = (i + 1).ToString();
                var users = (from u in entities.tbl_user
                             from p in u.tbl_phone
                             where p.phone_number.StartsWith(id)
                             select u).Distinct().ToList();
            }
        }

        public void SameObjectInCycleLoadLinq(int iterationCount, int userId)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                var users = (from e in entities.tbl_user
                             where e.user_id == userId
                             select e).ToList();
            }
        }

        public void SelectBySamePredicateLinq(int iterationCount)
        {
            for (int i = 0; i < iterationCount; i++)
            {
                var users = (from u in entities.tbl_user
                             from p in u.tbl_phone
                             where p.phone_number.StartsWith("1")
                             select u).Distinct();
            }
        }

        public void ObjectsWithLoadWithPropertiesAccessLinq()
        {
            var users = (from u in entities.tbl_user
                         select u).ToList();
            foreach (var user in users)
            {
                string name = user.first_name;
            }
        }
        #endregion Linq syntax
    }
}
