using System;
using System.Collections.Generic;
using System.Text;

using Worm.Database;
using Worm.Orm;
using Worm.Cache;

namespace DAWorm
{
    public class WormProvider
    {
        private string _connectionString;

        public WormProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void OpenConn()
        {
            using (OrmReadOnlyDBManager manager = new OrmDBManager(new OrmCache(), new DbSchema("1"), _connectionString))
            {
                ICollection<Tbl_user> users = manager.FindTop<Tbl_user>(10000, null, null, false);
                ICollection<Tbl_phone> phones = manager.FindTop<Tbl_phone>(10000, null, null, false);
            }
        }

        public void SelectWithoutLoad()
        {
            DbSchema schema = new DbSchema("1");
            OrmCache cache = new OrmCache();
            using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, schema, _connectionString))
            {
                //Tbl_user user = new Tbl_user(1, cache, schema);
                //user.Load();
                Tbl_user user = manager.Find<Tbl_user>(1);
               // ICollection<Tbl_user> users = manager.FindTop<Tbl_user>(10000, null, null, false);
            }
        }


        public void SelectCollectionWithoutLoad()
        {
            DbSchema schema = new DbSchema("1");
            OrmCache cache = new OrmCache();
            using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, schema, _connectionString))
            {
                ICollection<Tbl_user> users = manager.FindTop<Tbl_user>(10000, null, null, false);
            }
        }

        public void SelectWithLoad()
        {
            DbSchema schema = new DbSchema("1");
            OrmCache cache = new OrmCache();
            using (OrmReadOnlyDBManager manager = new OrmDBManager(new OrmCache(), new DbSchema("1"), _connectionString))
            {
                Tbl_user user = new Tbl_user(1, cache, schema);
                user.Load();
            }
        }

        public void SelectCollectionWithLoad()
        {
            using (OrmReadOnlyDBManager manager = new OrmDBManager(new OrmCache(), new DbSchema("1"), _connectionString))
            {
                ICollection<Tbl_user> users = manager.FindTop<Tbl_user>(10000, null, null, true);
            }
        }

        public void SelectSmallWithoutLoad()
        {
            DbSchema schema = new DbSchema("1");
            OrmCache cache = new OrmCache();
            using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, schema, _connectionString))
            {
                Tbl_phone phone = manager.Find<Tbl_phone>(1);
            }
        }

        public void SelectSmallCollectionWithoutLoad()
        {
            DbSchema schema = new DbSchema("1");
            OrmCache cache = new OrmCache();
            using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, schema, _connectionString))
            {
                ICollection<Tbl_phone> phones = manager.FindTop<Tbl_phone>(1000, null, null, false);
            }
        }

        public void SelectSmallWithLoad()
        {
            DbSchema schema = new DbSchema("1");
            OrmCache cache = new OrmCache();
            using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, schema, _connectionString))
            {
                Tbl_phone phone = new Tbl_phone(1, cache, schema);
                phone.Load();
            }
        }

        public void SelectSmallCollectionWithLoad()
        {
            using (OrmReadOnlyDBManager manager = new OrmDBManager(new OrmCache(), new DbSchema("1"), _connectionString))
            {
                ICollection<Tbl_phone> phones = manager.FindTop<Tbl_phone>(1000, null, null, true);
            }
        }
    }
}
