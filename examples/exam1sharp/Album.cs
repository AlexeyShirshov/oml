using System;
using Worm.Orm;

namespace test
{
	[Entity("dbo.albums", "id", "1")]
	class Album : OrmBaseT<Album>
	{
		private string _name;
		private DateTime? _release;

		public Album() {}

		public Album(int id, Worm.Orm.OrmCacheBase cache, Worm.Orm.OrmSchemaBase schema)
			: base(id, cache, schema)
		{
		}

		[Worm.Orm.ColumnAttribute()]
		public virtual string Name
		{
			get { using (Read("Name")) { return _name; } }
			set { using (Write("Name")) { _name = value; } }
		}

		[Worm.Orm.ColumnAttribute()]
		public virtual DateTime? Release
		{
			get { using (Read("Release")) { return _release; } }
			set { using (Write("Release")) { _release = value; } }
		}
	}
}
