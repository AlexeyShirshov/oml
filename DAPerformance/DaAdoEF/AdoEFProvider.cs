﻿using System;
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
        TestDAEntities entities = new TestDAEntities();

        /*Single entity*/
        public void SelectWithObjectServicesFactoryWithoutLoad()
        {
            var sql = "SELECT VALUE c FROM TestDAEntities.tbl_user AS c WHERE c.user_id=1";
            var query = entities.CreateQuery<tbl_user>(sql);
        }

        public void SelectWithObjectServicesFactoryWithLoad()
        {
            var sql = "SELECT VALUE c FROM TestDAEntities.tbl_user AS c WHERE c.user_id=1";
            var query = entities.CreateQuery<tbl_user>(sql);

            foreach (var user in query)
            {
                string result = string.Format("{0} {1} {2}", user.user_id, user.first_name, user.last_name);
            }
        }

        public void SelectWithObjectServicesWithoutLoad()
        {
            var sql = "SELECT VALUE c FROM TestDAEntities.tbl_user AS c WHERE c.user_id=1";
            ObjectQuery<tbl_user> query = new ObjectQuery<tbl_user>(sql, entities);
        }

        public void SelectWithObjectServicesWithLoad()
        {
            var sql = "SELECT VALUE c FROM TestDAEntities.tbl_user AS c WHERE c.user_id=1";
            ObjectQuery<tbl_user> query = new ObjectQuery<tbl_user>(sql, entities);

            foreach (var user in query)
            {
                string result = string.Format("{0} {1} {2}", user.user_id, user.first_name, user.last_name);
            }
        }

        public void SelectWithObjectServicesAnonimousWithoutLoad()
        {
            var sql = "SELECT c.user_id, c.first_name, c.last_name FROM TestDAEntities.tbl_user AS c WHERE c.user_id=1";
            var query = entities.CreateQuery<DbDataRecord>(sql);
        }

        public void SelectWithObjectServicesAnonimousWithLoad()
        {
            var sql = "SELECT c.user_id, c.first_name, c.last_name FROM TestDAEntities.tbl_user AS c WHERE c.user_id=1";
            var query = entities.CreateQuery<DbDataRecord>(sql);

            foreach (var user in query)
            {
                string result = string.Format("{0} {1} {2}", user["user_id"], user["first_name"], user["last_name"]);
            }
        }

        public void SelectWithEntityClientWithoutLoad()
        {
            using (EntityConnection conn = new EntityConnection("Name=TestDAEntities"))
            {
                conn.Open();
                EntityCommand cmd = new EntityCommand(
                  "SELECT VALUE c FROM TestDAEntities.tbl_user AS c WHERE c.user_id=1", conn);
                DbDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);
            }
        }

        public void SelectWithEntityClientWithLoad()
        {
            using (EntityConnection conn = new EntityConnection("Name=TestDAEntities"))
            {
                conn.Open();
                EntityCommand cmd = new EntityCommand(
                  "SELECT VALUE c FROM TestDAEntities.tbl_user AS c WHERE c.user_id=1", conn);
                DbDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);

                while (reader.Read())
                {
                    string result = string.Format("{0} {1} {2}", reader["user_id"], reader["first_name"], reader["last_name"]);
                }
            }
        }

        public void SelectWithEntityClientAnonimousWithoutLoad()
        {
            using (EntityConnection conn = new EntityConnection("Name=TestDAEntities"))
            {
                conn.Open();
                EntityCommand cmd = new EntityCommand(
                  "SELECT c.user_id, c.first_name, c.last_name FROM TestDAEntities.tbl_user AS c WHERE c.user_id=1", conn);
                DbDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);
            }
        }

        public void SelectWithEntityClientAnonimousWithLoad()
        {
            using (EntityConnection conn = new EntityConnection("Name=TestDAEntities"))
            {
                conn.Open();
                EntityCommand cmd = new EntityCommand(
                  "SELECT c.user_id, c.first_name, c.last_name FROM TestDAEntities.tbl_user AS c WHERE c.user_id=1", conn);
                DbDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);

                while (reader.Read())
                {
                    string result = string.Format("{0} {1} {2}", reader["user_id"], reader["first_name"], reader["last_name"]);
                }
            }
        }

        public void SelectWithLinqWithoutLoad()
        {
            var query = from e in entities.tbl_user select new { e.user_id, e.first_name, e.last_name };
        }

        public void SelectWithLinqWithLoad()
        {
            var query = from e in entities.tbl_user where e.user_id == 1
                        select new { e.user_id, e.first_name, e.last_name };

            foreach (var user in query)
            {
                string result = string.Format("{0} {1} {2}", user.user_id, user.first_name, user.last_name);
            }
        }

   

        /*Collection*/
        public void SelectCollectionWithObjectServicesFactoryWithoutLoad()
        {
            var sql = "SELECT VALUE c FROM TestDAEntities.tbl_user AS c";
            var query = entities.CreateQuery<tbl_user>(sql);
        }

        public void SelectCollectionWithObjectServicesFactoryWithLoad()
        {
            var sql = "SELECT VALUE c FROM TestDAEntities.tbl_user AS c";
            var query = entities.CreateQuery<tbl_user>(sql);

            foreach (var user in query)
            {
                string result = string.Format("{0} {1} {2}", user.user_id, user.first_name, user.last_name);
            }
        }

        public void SelectCollectionWithObjectServicesWithoutLoad()
        {
            var sql = "TestDAEntities.tbl_user";
            ObjectQuery<tbl_user> query = new ObjectQuery<tbl_user>(sql, entities);
        }

        public void SelectCollectionWithObjectServicesWithLoad()
        {
            var sql = "TestDAEntities.tbl_user";
            ObjectQuery<tbl_user> query = new ObjectQuery<tbl_user>(sql, entities);

            foreach (var user in query)
            {
                string result = string.Format("{0} {1} {2}", user.user_id, user.first_name, user.last_name);
            }
        }

        public void SelectCollectionWithObjectServicesAnonimousWithoutLoad()
        {
            var sql = "SELECT c.user_id, c.first_name, c.last_name FROM TestDAEntities.tbl_user AS c";
            var query = entities.CreateQuery<DbDataRecord>(sql);
        }

         public void SelectCollectionWithObjectServicesAnonimousWithLoad()
        {
            var sql = "SELECT c.user_id, c.first_name, c.last_name FROM TestDAEntities.tbl_user AS c";
            var query = entities.CreateQuery<DbDataRecord>(sql);

            foreach (var user in query)
            {
                string result = string.Format("{0} {1} {2}", user["user_id"], user["first_name"], user["last_name"]);
            }
        }

         public void SelectCollectionWithEntityClientWithoutLoad()
         {
             using (EntityConnection conn = new EntityConnection("Name=TestDAEntities"))
             {
                 conn.Open();
                 EntityCommand cmd = new EntityCommand(
                   "SELECT VALUE c FROM TestDAEntities.tbl_user AS c", conn);
                 DbDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);
             }
         }

         public void SelectCollectionWithEntityClientWithLoad()
         {
             using (EntityConnection conn = new EntityConnection("Name=TestDAEntities"))
             {
                 conn.Open();
                 EntityCommand cmd = new EntityCommand(
                   "SELECT VALUE c FROM TestDAEntities.tbl_user AS c", conn);
                 DbDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);

                 while (reader.Read())
                 {
                     string result = string.Format("{0} {1} {2}", reader["user_id"], reader["first_name"], reader["last_name"]);
                 }
             }
         }

         public void SelectCollectionWithEntityClientAnonimousWithoutLoad()
         {
             using (EntityConnection conn = new EntityConnection("Name=TestDAEntities"))
             {
                 conn.Open();
                 EntityCommand cmd = new EntityCommand(
                   "SELECT c.user_id, c.first_name, c.last_name FROM TestDAEntities.tbl_user AS c", conn);
                 DbDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);
             }
         }

         public void SelectCollectionWithEntityClientAnonimousWithLoad()
         {
             using (EntityConnection conn = new EntityConnection("Name=TestDAEntities"))
             {
                 conn.Open();
                 EntityCommand cmd = new EntityCommand(
                   "SELECT c.user_id, c.first_name, c.last_name FROM TestDAEntities.tbl_user AS c", conn);
                 DbDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);

                 while (reader.Read())
                 {
                     string result = string.Format("{0} {1} {2}", reader["user_id"], reader["first_name"], reader["last_name"]);
                 }
             }
         }

         public void SelectCollectionWithLinqWithoutLoad()
         {
             var query = from e in entities.tbl_user select new { e.user_id, e.first_name, e.last_name };
         }

         public void SelectCollectionWithLinqWithLoad()
        {
            var query = from e in entities.tbl_user
                        select new { e.user_id, e.first_name, e.last_name };

            foreach (var user in query)
            {
                string result = string.Format("{0} {1} {2}", user.user_id, user.first_name, user.last_name);
            }
        }

       
             /*Single entity*/
        public void SelectSmallWithObjectServicesFactoryWithoutLoad()
        {
            var sql = "SELECT VALUE c FROM TestDAEntities.tbl_phone AS c WHERE c.phone_id=1";
            var query = entities.CreateQuery<tbl_phone>(sql);
        }

        public void SelectSmallWithObjectServicesFactoryWithLoad()
        {
            var sql = "SELECT VALUE c FROM TestDAEntities.tbl_phone AS c WHERE c.phone_id=1";
            var query = entities.CreateQuery<tbl_phone>(sql);

            foreach (var phone in query)
            {
                string result = string.Format("{0} {1} {2}", phone.phone_id, phone.user_id, phone.phone_number);
            }
        }

        public void SelectSmallWithObjectServicesWithoutLoad()
        {
            var sql = "SELECT VALUE c FROM TestDAEntities.tbl_phone AS c WHERE c.phone_id=1";
            ObjectQuery<tbl_phone> query = new ObjectQuery<tbl_phone>(sql, entities);
        }

        public void SelectSmallWithObjectServicesWithLoad()
        {
            var sql = "SELECT VALUE c FROM TestDAEntities.tbl_phone AS c WHERE c.phone_id=1";
            ObjectQuery<tbl_phone> query = new ObjectQuery<tbl_phone>(sql, entities);

            foreach (var phone in query)
            {
                string result = string.Format("{0} {1} {2}", phone.phone_id, phone.user_id, phone.phone_number);
            }
        }

        public void SelectSmallWithObjectServicesAnonimousWithoutLoad()
        {
            var sql = "SELECT c.phone_id, c.user_id, c.phone_number FROM TestDAEntities.tbl_phone AS c WHERE c.phone_id=1";
            var query = entities.CreateQuery<DbDataRecord>(sql);
        }

        public void SelectSmallWithObjectServicesAnonimousWithLoad()
        {
            var sql = "SELECT c.phone_id, c.user_id, c.phone_number FROM TestDAEntities.tbl_phone AS c WHERE c.phone_id=1";
            var query = entities.CreateQuery<DbDataRecord>(sql);

            foreach (var phone in query)
            {
                string result = string.Format("{0} {1} {2}", phone["phone_id"], phone["user_id"], phone["phone_number"]);
            }
        }

        public void SelectSmallWithEntityClientWithoutLoad()
        {
            using (EntityConnection conn = new EntityConnection("Name=TestDAEntities"))
            {
                conn.Open();
                EntityCommand cmd = new EntityCommand(
                  "SELECT VALUE c FROM TestDAEntities.tbl_phone AS c WHERE c.phone_id=1", conn);
                DbDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);
            }
        }

        public void SelectSmallWithEntityClientWithLoad()
        {
            using (EntityConnection conn = new EntityConnection("Name=TestDAEntities"))
            {
                conn.Open();
                EntityCommand cmd = new EntityCommand(
                  "SELECT VALUE c FROM TestDAEntities.tbl_phone AS c WHERE c.phone_id=1", conn);
                DbDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);

                while (reader.Read())
                {
                    string result = string.Format("{0} {1} {2}", reader["phone_id"], reader["user_id"], reader["phone_number"]);
                }
            }
        }

        public void SelectSmallWithEntityClientAnonimousWithoutLoad()
        {
            using (EntityConnection conn = new EntityConnection("Name=TestDAEntities"))
            {
                conn.Open();
                EntityCommand cmd = new EntityCommand(
                  "SELECT c.phone_id, c.user_id, c.phone_number FROM TestDAEntities.tbl_phone AS c WHERE c.phone_id=1", conn);
                DbDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);
            }
        }

        public void SelectSmallWithEntityClientAnonimousWithLoad()
        {
            using (EntityConnection conn = new EntityConnection("Name=TestDAEntities"))
            {
                conn.Open();
                EntityCommand cmd = new EntityCommand(
                  "SELECT c.phone_id, c.user_id, c.phone_number FROM TestDAEntities.tbl_phone AS c WHERE c.phone_id=1", conn);
                DbDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);

                while (reader.Read())
                {
                    string result = string.Format("{0} {1} {2}", reader["phone_id"], reader["user_id"], reader["phone_number"]);
                }
            }
        }

        public void SelectSmallWithLinqWithoutLoad()
        {
            var query = from e in entities.tbl_phone select new { e.phone_id, e.user_id, e.phone_number };
        }

        public void SelectSmallWithLinqWithLoad()
        {
            var query = from e in entities.tbl_phone where e.phone_id == 1
                        select new { e.phone_id, e.user_id, e.phone_number };

            foreach (var phone in query)
            {
                string result = string.Format("{0} {1} {2}", phone.phone_id, phone.user_id, phone.phone_number);
            }
        }

   

        /*Collection*/
        public void SelectSmallCollectionWithObjectServicesFactoryWithoutLoad()
        {
            var sql = "SELECT VALUE c FROM TestDAEntities.tbl_phone AS c";
            var query = entities.CreateQuery<tbl_phone>(sql);
        }

        public void SelectSmallCollectionWithObjectServicesFactoryWithLoad()
        {
            var sql = "SELECT VALUE c FROM TestDAEntities.tbl_phone AS c";
            var query = entities.CreateQuery<tbl_phone>(sql);

            foreach (var phone in query)
            {
                string result = string.Format("{0} {1} {2}", phone.phone_id, phone.user_id, phone.phone_number);
            }
        }

        public void SelectSmallCollectionWithObjectServicesWithoutLoad()
        {
            var sql = "TestDAEntities.tbl_phone";
            ObjectQuery<tbl_phone> query = new ObjectQuery<tbl_phone>(sql, entities);
        }

        public void SelectSmallCollectionWithObjectServicesWithLoad()
        {
            var sql = "TestDAEntities.tbl_phone";
            ObjectQuery<tbl_phone> query = new ObjectQuery<tbl_phone>(sql, entities);

            foreach (var phone in query)
            {
                string result = string.Format("{0} {1} {2}", phone.phone_id, phone.user_id, phone.phone_number);
            }
        }

        public void SelectSmallCollectionWithObjectServicesAnonimousWithoutLoad()
        {
            var sql = "SELECT c.phone_id, c.user_id, c.phone_number FROM TestDAEntities.tbl_phone AS c";
            var query = entities.CreateQuery<DbDataRecord>(sql);
        }

         public void SelectSmallCollectionWithObjectServicesAnonimousWithLoad()
        {
            var sql = "SELECT c.phone_id, c.user_id, c.phone_number FROM TestDAEntities.tbl_phone AS c";
            var query = entities.CreateQuery<DbDataRecord>(sql);

            foreach (var phone in query)
            {
                string result = string.Format("{0} {1} {2}", phone["phone_id"], phone["user_id"], phone["phone_number"]);
            }
        }

         public void SelectSmallCollectionWithEntityClientWithoutLoad()
         {
             using (EntityConnection conn = new EntityConnection("Name=TestDAEntities"))
             {
                 conn.Open();
                 EntityCommand cmd = new EntityCommand(
                   "SELECT VALUE c FROM TestDAEntities.tbl_phone AS c", conn);
                 DbDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);
             }
         }

         public void SelectSmallCollectionWithEntityClientWithLoad()
         {
             using (EntityConnection conn = new EntityConnection("Name=TestDAEntities"))
             {
                 conn.Open();
                 EntityCommand cmd = new EntityCommand(
                   "SELECT VALUE c FROM TestDAEntities.tbl_phone AS c", conn);
                 DbDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);

                 while (reader.Read())
                 {
                     string result = string.Format("{0} {1} {2}", reader["phone_id"], reader["user_id"], reader["phone_number"]);
                 }
             }
         }

         public void SelectSmallCollectionWithEntityClientAnonimousWithoutLoad()
         {
             using (EntityConnection conn = new EntityConnection("Name=TestDAEntities"))
             {
                 conn.Open();
                 EntityCommand cmd = new EntityCommand(
                   "SELECT c.phone_id, c.user_id, c.phone_number FROM TestDAEntities.tbl_phone AS c", conn);
                 DbDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);
             }
         }

         public void SelectSmallCollectionWithEntityClientAnonimousWithLoad()
         {
             using (EntityConnection conn = new EntityConnection("Name=TestDAEntities"))
             {
                 conn.Open();
                 EntityCommand cmd = new EntityCommand(
                   "SELECT c.phone_id, c.user_id, c.phone_number FROM TestDAEntities.tbl_phone AS c", conn);
                 DbDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);

                 while (reader.Read())
                 {
                     string result = string.Format("{0} {1} {2}", reader["phone_id"], reader["user_id"], reader["phone_number"]);
                 }
             }
         }

         public void SelectSmallCollectionWithLinqWithoutLoad()
         {
             var query = from e in entities.tbl_phone select new { e.phone_id, e.user_id, e.phone_number };
         }

         public void SelectSmallCollectionWithLinqWithLoad()
        {
            var query = from e in entities.tbl_phone
                        select new { e.phone_id, e.user_id, e.phone_number };

            foreach (var phone in query)
            {
                string result = string.Format("{0} {1} {2}", phone.phone_id, phone.user_id, phone.phone_number);
            }
        }

  
    }
}