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
        private DbSchema _schema = new DbSchema("1");

        public WormProvider(string connectionString)
        {
            _connectionString = connectionString;
            object o = Schema.GetObjectSchema(typeof(Tbl_phone));
        }

        public DbSchema Schema
        {
            get
            {
                return _schema;
            }
        }

        public void OpenConn()
        {
            using (OrmReadOnlyDBManager manager = new OrmDBManager(new OrmCache(), Schema, _connectionString))
            {
                ICollection<Tbl_user> users = manager.FindTop<Tbl_user>(10000, null, null, false);
                ICollection<Tbl_phone> phones = manager.FindTop<Tbl_phone>(10000, null, null, false);
            }
        }

        public void SelectWithoutLoad()
        {
            OrmCache cache = new OrmCache();
            using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, Schema, _connectionString))
            {
                //Tbl_user user = new Tbl_user(1, cache, schema);
                //user.Load();
                Tbl_user user = manager.Find<Tbl_user>(1);
               // ICollection<Tbl_user> users = manager.FindTop<Tbl_user>(10000, null, null, false);
            }
        }


        public void SelectCollectionWithoutLoad()
        {
            Worm.PerfCounter p = new Worm.PerfCounter();
            OrmCache cache = new OrmCache();
            using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, Schema, _connectionString))
            {
                ICollection<Tbl_user> users = manager.FindTop<Tbl_user>(10000, null, null, false);
            }
            System.Diagnostics.Debug.WriteLine(p.GetTime());
        }

        public void SelectWithLoad()
        {
            OrmCache cache = new OrmCache();
            using (OrmReadOnlyDBManager manager = new OrmDBManager(new OrmCache(), Schema, _connectionString))
            {
                Tbl_user user = new Tbl_user(1, cache, Schema);
                user.Load();
            }
        }

        public void SelectCollectionWithLoad()
        {
            using (OrmReadOnlyDBManager manager = new OrmDBManager(new OrmCache(), Schema, _connectionString))
            {
                ICollection<Tbl_user> users = manager.FindTop<Tbl_user>(10000, null, null, true);
            }
        }

        public void SelectSmallWithoutLoad()
        {
            OrmCache cache = new OrmCache();
            using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, Schema, _connectionString))
            {
                Tbl_phone phone = manager.Find<Tbl_phone>(1);
            }
        }

        public void SelectSmallCollectionWithoutLoad()
        {
            OrmCache cache = new OrmCache();
            using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, Schema, _connectionString))
            {
                ICollection<Tbl_phone> phones = manager.FindTop<Tbl_phone>(1000, null, null, false);
            }
        }

        public void SelectSmallWithLoad()
        {
            OrmCache cache = new OrmCache();
            using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, Schema, _connectionString))
            {
                Tbl_phone phone = new Tbl_phone(1, cache, Schema);
                phone.Load();
            }
        }

        public void SelectSmallCollectionWithLoad()
        {
            using (OrmReadOnlyDBManager manager = new OrmDBManager(new OrmCache(), Schema, _connectionString))
            {
                ICollection<Tbl_phone> phones = manager.FindTop<Tbl_phone>(1000, null, null, true);
            }
        }
    }
}
