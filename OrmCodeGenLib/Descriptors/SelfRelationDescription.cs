using System;
using System.Collections.Generic;
using System.Text;

namespace OrmCodeGenLib.Descriptors
{
	public class SelfRelationDescription
	{
		private readonly TableDescription _table;
		private readonly EntityDescription _underlyingEntity;
		private readonly EntityDescription _entity;
		private readonly bool _disabled;

		private readonly SelfRelationTarget _direct;
		private readonly SelfRelationTarget _reverse;

		public SelfRelationDescription(EntityDescription entity, SelfRelationTarget direct, SelfRelationTarget reverse, TableDescription table, EntityDescription underlyingEntity, bool disabled)
		{
			_entity = entity;
			_direct = direct;
			_reverse = reverse;
			_table = table;
			_underlyingEntity = underlyingEntity;
			_disabled = disabled;
		}

		public TableDescription Table
		{
			get { return _table; }
		}

		public EntityDescription UnderlyingEntity
		{
			get { return _underlyingEntity; }
		}

		public EntityDescription Entity
		{
			get { return _entity; }
		}

		public bool Disabled
		{
			get { return _disabled; }
		}

		public SelfRelationTarget Direct
		{
			get { return _direct; }
		}

		public SelfRelationTarget Reverse
		{
			get { return _reverse; }
		}
	}
}
