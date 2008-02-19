using System;
using System.Collections.Generic;
using System.Text;
using Worm.Orm;

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
            OrmReadOnlyDBManager manager = new OrmDBManager(cache, schema, _connectionString);
            Tbl_user user = new Tbl_user(1, cache, schema);
            user.Load();
        }
    }
}
