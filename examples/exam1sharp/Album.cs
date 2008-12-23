using System;
using Worm.Entities.Meta;
using Worm.Entities;
using Worm.Cache;
using Worm;

namespace test
{
	[Entity("dbo.albums", "id", "1")]
	class Album : OrmBaseT<Album>
	{
		private string _name;
		private DateTime? _release;

		public Album() {}

		public Album(int id, CacheBase cache, ObjectMappingEngine schema)
			: base(id, cache, schema)
		{
		}

        [EntityProperty]
		public virtual string Name
		{
			get { using (Read("Name")) { return _name; } }
			set { using (Write("Name")) { _name = value; } }
		}

        [EntityProperty]
		public virtual DateTime? Release
		{
			get { using (Read("Release")) { return _release; } }
			set { using (Write("Release")) { _release = value; } }
		}
	}
}
