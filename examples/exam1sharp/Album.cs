using System;
using Worm.Entities.Meta;
using Worm.Entities;
using Worm.Cache;
using Worm;

namespace test
{
    [Entity("dbo", "albums", "1")]
	class Album : KeyEntity
	{
		private string _name;
		private DateTime? _release;
        private int _id;

		public Album() {}

		public Album(int id, CacheBase cache, ObjectMappingEngine schema)
		{
            Init(id, cache, schema);
		}

        [EntityProperty("name")]
		public virtual string Name
		{
			get { using (Read("Name")) { return _name; } }
			set { using (Write("Name")) { _name = value; } }
		}

        [EntityProperty("release_dt")]
		public virtual DateTime? Release
		{
			get { using (Read("Release")) { return _release; } }
			set { using (Write("Release")) { _release = value; } }
		}

        [EntityProperty("id", Field2DbRelations.PrimaryKey, DBType="int")]
        public override object Identifier
        {
            get { return _id; }
            set { _id = (int)value; }
        }
    }
}
