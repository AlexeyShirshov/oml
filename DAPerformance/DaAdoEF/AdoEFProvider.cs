using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Data.Common;
using System.Data.EntityClient;
using System.Linq;
using System.Text;
using Common;
using TestDAModel;

namespace DaAdoEF
{
    public class AdoEFProvider
    {
        TestDAEntities entities;
        EntityConnection connection;
        int[] smallUserIds;
        int[] mediumUserIds;
        int[] largeUserIds;

        public AdoEFProvider(string connectionString)
        {
            connection = new EntityConnection(connectionString);
            connection.Open();
            entities = new TestDAEntities(connection);
        }

        public AdoEFProvider(EntityConnection connection, int[] smallUserIds, int[] mediumUserIds, int[] largeUserIds)
        {
            this.smallUserIds = smallUserIds;
            this.mediumUserIds = mediumUserIds;
            this.largeUserIds = largeUserIds;

            entities = new TestDAEntities(connection);
        }

        public void Dispose()
        {
            entities.Dispose();
        }

        #region Default syntax

        [QueryTypeAttribute(QueryType.TypeCycleWithLoad)]
        public void TypeCycleWithLoad()
        {
            foreach (int id in mediumUserIds)
            {
                var users = entities.tbl_user.Where("it.user_id = @user_id", new ObjectParameter("user_id", id)).ToList();
            }
        }

        [QueryTypeAttribute(QueryType.TypeCycleWithoutLoad)]
        public void TypeCycleWithoutLoad()
        {
            foreach (int id in mediumUserIds)
            {
                //var users = entities.tbl_user.Where("it.user_id = @user_id", new ObjectParameter("user_id", id)).ToList();
                var users = entities.tbl_user.Where("it.user_id = @user_id", new ObjectParameter("user_id", id)).Select(t => t.user_id).ToList();               
                foreach (var userId in users)
                {
                    var user = entities.tbl_user.First(u => u.user_id == userId);
                    string name = user.first_name;
                }
            }
        }

        [QueryTypeAttribute(QueryType.TypeCycleLazyLoad)]
        public void TypeCycleLazyLoad()
        {
            foreach (int id in mediumUserIds)
            {
                var users = entities.tbl_user.Where("it.user_id = @user_id",
                    new ObjectParameter("user_id", id)).ToList();
                foreach (tbl_user user in users)
                {
                    string first_name = user.first_name;
                }
            }
        }

        [QueryTypeAttribute(QueryType.SmallCollection)]
        public void SmallCollection()
        {
            List<tbl_user> users = entities.tbl_user.Take(Constants.Small).ToList();
        }

        [QueryTypeAttribute(QueryType.LargeCollection)]
        public void LargeCollection()
        {
            List<tbl_user> users = entities.tbl_user.Take(Constants.Large).ToList();
        }

        [QueryTypeAttribute(QueryType.SelectLargeCollection)]
        public void SelectLargeCollectionLinq()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                List<tbl_user> users = entities.tbl_user.Take(Constants.Large).ToList();
            }
        }

        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad)]
        public void SameObjectInCycleLoad()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
               var users = entities.tbl_user.Where("it.user_id = @user_id", 
                    new ObjectParameter("user_id", smallUserIds[0])).ToList();
            }
        }

        [QueryTypeAttribute(QueryType.CollectionByPredicateWithoutLoad)]
        public void CollectionByPredicateWithoutLoad()
        {
            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                string id = (i + 1).ToString();
                var users = entities.tbl_user.Join(entities.tbl_phone,
                    u => u.user_id, p => p.user_id, (u, p) => new {U = u, P = p}).
                        Where(jn => jn.P.phone_number.StartsWith(id)).
                        Select(jn => jn.U.user_id).ToList();
                foreach (var userId in users)
                {
                    var user = entities.tbl_user.First(u => u.user_id == userId);
                    string name = user.first_name;
                }
            }
        }
        
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad)]
        public void CollectionByPredicateWithLoad()
        {
            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                string id = (i + 1).ToString();
                var users = entities.tbl_user.Join(entities.tbl_phone,
                    u => u.user_id, p => p.user_id, (u, p) => new { U = u, P = p }).
                        Where(jn => jn.P.phone_number.StartsWith(id)).
                        Select(jn => jn.U).ToList();
            }
        }


        [QueryTypeAttribute(QueryType.SelectBySamePredicate)]
        public void SelectBySamePredicate()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                var users = entities.tbl_user.Join(entities.tbl_phone,
                    u => u.user_id, p => p.user_id, (u, p) => new { U = u, P = p }).
                        Where(jn => jn.P.phone_number.StartsWith("1")).
                        Select(jn => jn.U).ToList();
            }
        }

        [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess)]
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
        [QueryTypeAttribute(QueryType.TypeCycleWithoutLoad, Syntax.Linq)]
        public void TypeCycleWithoutLoadLinq()
        {

            foreach (int id in mediumUserIds)
            {
                var users = (from e in entities.tbl_user
                             where e.user_id == id
                             select e).ToList();
                foreach (var userId in users)
                {
                    var user = users.Single();
                    string name = user.first_name;
                }
            }

        }
        [QueryTypeAttribute(QueryType.TypeCycleWithLoad, Syntax.Linq)]
        public void TypeCycleWithLoadLinq()
        {
            foreach (int id in mediumUserIds)
            {
                var users = (from e in entities.tbl_user
                             where e.user_id == id
                             select e).ToList();
            }
        }
        [QueryTypeAttribute(QueryType.TypeCycleLazyLoad, Syntax.Linq)]
        public void TypeCycleLazyLoadLinq()
        {
            foreach (int id in mediumUserIds)
            {
                var users = (from e in entities.tbl_user
                             where e.user_id == id
                             select e).ToList();
                foreach (tbl_user user in users)
                {
                    string first_name = user.first_name;
                }
            }
        }

        [QueryTypeAttribute(QueryType.SmallCollection, Syntax.Linq)]
        public void SmallCollectionLinq()
        {
            var users = (from e in entities.tbl_user
                         select e).Take(Constants.Small).ToList();
        }

        [QueryTypeAttribute(QueryType.LargeCollection, Syntax.Linq)]
        public void LargeCollectionLinq()
        {
            var users = (from e in entities.tbl_user
                         select e).Take(Constants.Large).ToList();
        }

        [QueryTypeAttribute(QueryType.CollectionByPredicateWithoutLoad, Syntax.Linq)]
        public void CollectionByPredicateWithoutLoadLinq()
        {
            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                string id = (i + 1).ToString();
                var users = (from u in entities.tbl_user
                             from p in u.tbl_phone
                             where p.phone_number.StartsWith(id)
                             select u.user_id).ToList();
                foreach (var userId in users)
                {
                    var user = entities.tbl_user.First(u => u.user_id == userId);
                    string name = user.first_name;
                }
            }
        }
        [QueryTypeAttribute(QueryType.CollectionByPredicateWithLoad, Syntax.Linq)]
        public void CollectionByPredicateWithLoadLinq()
        {
            for (int i = 0; i < Constants.LargeIteration; i++)
            {
                string id = (i + 1).ToString();
                var users = (from u in entities.tbl_user
                             from p in u.tbl_phone
                             where p.phone_number.StartsWith(id)
                             select u).ToList();
            }
        }

        [QueryTypeAttribute(QueryType.SameObjectInCycleLoad, Syntax.Linq)]
        public void SameObjectInCycleLoadLinq()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                var users = (from e in entities.tbl_user
                             where e.user_id == smallUserIds[0]
                             select e).ToList();
            }
        }
        [QueryTypeAttribute(QueryType.SelectBySamePredicate, Syntax.Linq)]
        public void SelectBySamePredicateLinq()
        {
            for (int i = 0; i < Constants.SmallIteration; i++)
            {
                var users = (from u in entities.tbl_user
                             from p in u.tbl_phone
                             where p.phone_number.StartsWith("1")
                             select u).ToList();
            }
        }
        [QueryTypeAttribute(QueryType.ObjectsWithLoadWithPropertiesAccess, Syntax.Linq)]
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
