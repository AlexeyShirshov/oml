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

        public void Select()
        {
            DbSchema schema = new DbSchema("1");
            OrmCache cache = new OrmCache();
            using (OrmReadOnlyDBManager manager = new OrmDBManager(cache, schema, _connectionString))
            {
                //Tbl_user user = new Tbl_user(1, cache, schema);
                //user.Load();
                Tbl_user user = manager.Find<Tbl_user>(1);
                ICollection<Tbl_user> users = manager.FindTop<Tbl_user>(1000, null, null, false);
            }
        }
    }
}
