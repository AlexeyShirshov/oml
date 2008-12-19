using System;
using System.Collections.Generic;
using System.Text;

namespace Worm.CodeGen.Core.Descriptors
{
	public class EntityDescription
	{
		#region Private Fields
		private readonly string _id;
		private readonly string _name;
		private readonly string _description;
	    private readonly List<TableDescription> _tables;
		private readonly List<PropertyDescription> _properties;
		private readonly List<PropertyDescription> _suppressedProperties;
		private readonly OrmObjectsDef _ormObjectsDef;
		private EntityDescription _baseEntity;

	    #endregion Private Fields

		public EntityDescription(string id, string name, string nameSpace, string description, OrmObjectsDef ormObjectsDef)
			: this(id, name, nameSpace, description, ormObjectsDef, null)
		{
		}

		public EntityDescription(string id, string name, string nameSpace, string description, OrmObjectsDef ormObjectsDef, EntityDescription baseEntity)
			: this(id, name, nameSpace, description, ormObjectsDef, baseEntity, EntityBehaviuor.Default)
		{

		}

		public EntityDescription(string id, string name, string nameSpace, string description, OrmObjectsDef ormObjectsDef, EntityDescription baseEntity, EntityBehaviuor behaviour)
		{
			_id = id;
			_name = name;
			_description = description;
			_tables = new List<TableDescription>();
			_properties = new List<PropertyDescription>();
			_suppressedProperties = new List<PropertyDescription>();
			_ormObjectsDef = ormObjectsDef;
			RawNamespace = nameSpace;
			_baseEntity = baseEntity;
			Behaviour = behaviour;
		}

		public string Identifier
		{
			get { return _id; }
		}

		public string Name
		{
			get { return _name; }
		}

		public string Description
		{
			get { return _description; }
		}

		public List<TableDescription> Tables
		{
			get { return _tables; }
		}

		public List<PropertyDescription> Properties
		{
			get { return _properties; }
		}

		public OrmObjectsDef OrmObjectsDef
		{
			get { return _ormObjectsDef; }
		}

        public bool HasSinglePK
        {
            get
            {
                int s = 0;
                foreach (var propertyDescription in this.CompleteEntity.Properties)
                {
                    if (propertyDescription.HasAttribute(Worm.Orm.Meta.Field2DbRelations.PK) && propertyDescription.PropertyType.IsClrType && propertyDescription.PropertyType.ClrType.IsAssignableFrom(typeof(Int32)))
                        s++;
                }
                return (BaseEntity == null && s == 1) || (BaseEntity != null && BaseEntity.HasSinglePK);
            }
        }

		public PropertyDescription GetProperty(string propertyId)
		{
			return GetProperty(propertyId, false);
		}

		public PropertyDescription GetProperty(string propertyName, bool throwNotFoundException)
		{
			PropertyDescription result = this.Properties.Find(delegate(PropertyDescription match)
			{
				return match.Name == propertyName;
			});
			if (result == null && throwNotFoundException)
				throw new KeyNotFoundException(
					string.Format("Property with name '{0}' in entity '{1}' not found.", propertyName, this.Identifier));
			return result;
		}

		public TableDescription GetTable(string tableId)
		{
			return GetTable(tableId, false);
		}

		public TableDescription GetTable(string tableId, bool throwNotFoundException)
		{
			TableDescription table;
			//System.Text.RegularExpressions.Match nameMatch = Worm.CodeGen.Core.OrmObjectsDef.GetNsNameMatch(tableId);
			//string localTableId = tableId;
			//if(nameMatch.Success && nameMatch.Groups["name"].Success)
			//{
			//    localTableId = nameMatch.Groups["name"].Value;
			//}
			table = this.Tables.Find(delegate(TableDescription match)
			{
				return match.Identifier == tableId;
			});

			if (table == null && throwNotFoundException)
				throw new KeyNotFoundException(
					string.Format("Table with id '{0}' in entity '{1}' not found.", tableId, this.Identifier));
			return table;
		}

		public List<RelationDescription> GetRelations(bool withDisabled)
		{
			List<RelationDescription> l = new List<RelationDescription>();
			foreach (RelationDescriptionBase rel in _ormObjectsDef.Relations)
			{
				RelationDescription match = rel as RelationDescription;
				if (match != null && (match.IsEntityTakePart(this)) &&
						(!match.Disabled || withDisabled))
				{
					l.Add(match);
				}
			}
			return l;
		}

		public List<SelfRelationDescription> GetSelfRelations(bool withDisabled)
		{
			List<SelfRelationDescription> l = new List<SelfRelationDescription>();
			foreach (RelationDescriptionBase rel in _ormObjectsDef.Relations)
			{
				SelfRelationDescription match = rel as SelfRelationDescription;
				if (match != null && (match.IsEntityTakePart(this)) &&
						(!match.Disabled || withDisabled))
				{
					l.Add(match);
				}
			}
			return l;
		}

		public List<RelationDescriptionBase> GetAllRelations(bool withDisabled)
		{
			List<RelationDescriptionBase> l = new List<RelationDescriptionBase>();

			foreach (RelationDescriptionBase relation in _ormObjectsDef.Relations)
			{
				if (relation.IsEntityTakePart(this) && (!relation.Disabled || withDisabled))
				{
					l.Add(relation);
				}
			}
			return l;
		}

		public string Namespace
		{
			get { return string.IsNullOrEmpty(RawNamespace) ? _ormObjectsDef.Namespace : RawNamespace; }
			set { RawNamespace = value; }
		}

	    public string RawNamespace { get; private set; }

	    public EntityDescription BaseEntity
		{
			get { return _baseEntity; }
			set { _baseEntity = value; }
		}

		//public string QualifiedIdentifier
		//{
		//    get
		//    {
		//        return
		//            (OrmObjectsDef != null && !string.IsNullOrEmpty(OrmObjectsDef.NS))
		//                ? OrmObjectsDef.NS + ":" + Identifier
		//                : Identifier;
		//    }
		//}

		private static EntityDescription MergeEntities(EntityDescription oldOne, EntityDescription newOne)
		{
			EntityDescription resultOne =
				new EntityDescription(newOne.Identifier, newOne.Name, newOne.Namespace, newOne.Description ?? oldOne.Description,
									  newOne.OrmObjectsDef);

			//��������� ����� ��������
			foreach (TableDescription newTable in newOne.Tables)
			{
				resultOne.Tables.Add(newTable);
			}
			// ��������� ����� ��������
			foreach (PropertyDescription newProperty in newOne.Properties)
			{
				PropertyDescription prop = newProperty.CloneSmart();
				if (newOne.SuppressedProperties.Exists(delegate(PropertyDescription match) { return match.Name == newProperty.Name; }))
					prop.IsSuppressed = true;
				resultOne.Properties.Add(prop);
			}

			foreach (PropertyDescription newProperty in newOne.SuppressedProperties)
			{
				PropertyDescription prop = newProperty.CloneSmart();
				resultOne.SuppressedProperties.Add(prop);
			}

			if (oldOne != null)
			{
				// ��������� ������ ��������, ���� �����
				if (newOne.InheritsBaseTables)
					foreach (TableDescription oldTable in oldOne.Tables)
					{
						if (!resultOne.Tables.Exists(delegate(TableDescription tableMatch) { return oldTable.Name == tableMatch.Name; }))
							resultOne.Tables.Insert(oldOne.Tables.IndexOf(oldTable), oldTable);
					}

				foreach (PropertyDescription oldProperty in oldOne.SuppressedProperties)
				{
					PropertyDescription prop = oldProperty.CloneSmart();
					resultOne.SuppressedProperties.Add(prop);
				}

				// ��������� ������ ��������, ���� �����
				foreach (PropertyDescription oldProperty in oldOne.Properties)
				{
					PropertyDescription newProperty = resultOne.GetProperty(oldProperty.Name);
					if (newProperty == null)
					{
						TableDescription newTable = null; 
						if(oldProperty.Table != null)
							newTable = resultOne.GetTable(oldProperty.Table.Identifier);
						TypeDescription newType = oldProperty.PropertyType;
						bool isSuppressed =
							resultOne.SuppressedProperties.Exists(delegate(PropertyDescription match) { return match.Name == oldProperty.Name; });
						bool isRefreshed = false;
						bool fromBase = true;
						if (newType.IsEntityType)
						{
							EntityDescription newEntity =
								resultOne.OrmObjectsDef.Entities.Find(delegate(EntityDescription matchEntity)
																	  {
																		  return matchEntity.BaseEntity != null && matchEntity.BaseEntity.Identifier == newType.Entity.Identifier;
																	  });
							if (newEntity != null)
							{
								newType = new TypeDescription(newType.Identifier, newEntity);
								isRefreshed = true;
							}
						}
						resultOne.Properties.Insert(resultOne.Properties.Count - newOne.Properties.Count,
												new PropertyDescription(resultOne, oldProperty.Name, oldProperty.PropertyAlias,
																		oldProperty.Attributes,
																		oldProperty.Description,
																		newType,
																		oldProperty.FieldName, newTable, fromBase, oldProperty.FieldAccessLevel, oldProperty.PropertyAccessLevel, isSuppressed, isRefreshed));
					}
				}
			}

			return resultOne;
		}

		public EntityDescription CompleteEntity
		{
			get
			{
				EntityDescription baseEntity;
				if (_baseEntity == null)
					baseEntity = null;
				else
					baseEntity = _baseEntity.CompleteEntity;
				return MergeEntities(baseEntity, this);
			}
		}

	    public EntityBehaviuor Behaviour { get; set; }

	    public List<PropertyDescription> SuppressedProperties
		{
			get { return _suppressedProperties; }
		}

	    public bool InheritsBaseTables { get; set; }

	    public bool UseGenerics { get; set; }

	    public bool MakeInterface { get; set; }

	    public bool EnableCommonEventRaise
		{
			get
			{
				return _ormObjectsDef.EnableCommonPropertyChangedFire && !_properties.Exists(prop => prop.EnablePropertyChanged);
			}
		}

	    public PropertyDescription PkProperty
	    {
            get
            {
                if (HasSinglePK) 
                    foreach (var propertyDescription in CompleteEntity.Properties)
                    {
                        if (propertyDescription.HasAttribute(Worm.Orm.Meta.Field2DbRelations.PK) && propertyDescription.PropertyType.IsClrType && propertyDescription.PropertyType.ClrType.IsAssignableFrom(typeof(Int32)))
                            return propertyDescription;
                    }
                throw new InvalidOperationException("Only usable with single PK");
            }
	    }

	    public List<PropertyDescription> PkProperties
	    {
	        get
	        {
	            return Properties.FindAll(p => p.HasAttribute(Worm.Orm.Meta.Field2DbRelations.PK));
	        }
	    }
	}
}
