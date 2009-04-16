using System;
using System.Collections.Generic;
using System.Linq;

namespace Worm.CodeGen.Core.Descriptors
{
	public class EntityDescription
	{
		#region Private Fields
		private readonly string _id;
		private readonly string _name;
		private readonly string _description;
	    private readonly List<SourceFragmentDescription> _sourceFragments;
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
			: this(id, name, nameSpace, description, ormObjectsDef, baseEntity, EntityBehaviuor.ForcePartial)
		{

		}

		public EntityDescription(string id, string name, string nameSpace, string description, OrmObjectsDef ormObjectsDef, EntityDescription baseEntity, EntityBehaviuor behaviour)
		{
			_id = id;
			_name = name;
			_description = description;
			_sourceFragments = new List<SourceFragmentDescription>();
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

		public List<SourceFragmentDescription> SourceFragments
		{
			get { return _sourceFragments; }
		}

		public List<PropertyDescription> Properties
		{
			get { return _properties; }
		}

		public OrmObjectsDef OrmObjectsDef
		{
			get { return _ormObjectsDef; }
		}

		public bool HasPk
		{
			get
			{
				return GetPKCount(false) > 0;
			}
		}

		public bool HasPkFlatEntity
		{
			get
			{
				return GetPKCount() > 0;
			}
		}



        public bool HasSinglePk
        {
            get
            {
				int s = GetPKCount();
				return (BaseEntity == null && s == 1) || (BaseEntity != null && BaseEntity.HasSinglePk);   	
            }
        }

		protected int GetPKCount()
		{
			return GetPKCount(true);
		}

		protected int GetPKCount(bool flatEntity)
		{
			int s = 0;
			var properties = flatEntity ? CompleteEntity.Properties : Properties;
			foreach (var propertyDescription in properties)
			{
				if (propertyDescription.HasAttribute(Entities.Meta.Field2DbRelations.PK) 
					//&& propertyDescription.PropertyType.IsClrType && propertyDescription.PropertyType.ClrType.IsAssignableFrom(typeof(Int32))
					)
					s++;
			}
			return s;
		}

		public PropertyDescription GetProperty(string propertyId)
		{
			return GetProperty(propertyId, false);
		}

		public PropertyDescription GetProperty(string propertyName, bool throwNotFoundException)
		{
			PropertyDescription result = Properties.Find(match => match.Name == propertyName);
			if (result == null && throwNotFoundException)
				throw new KeyNotFoundException(
					string.Format("Property with name '{0}' in entity '{1}' not found.", propertyName, Identifier));
			return result;
		}

		public SourceFragmentDescription GetSourceFragments(string sourceFragmentId)
		{
			return GetSourceFragments(sourceFragmentId, false);
		}

		public SourceFragmentDescription GetSourceFragments(string tableId, bool throwNotFoundException)
		{
			//System.Text.RegularExpressions.Match nameMatch = Worm.CodeGen.Core.OrmObjectsDef.GetNsNameMatch(tableId);
			//string localTableId = tableId;
			//if(nameMatch.Success && nameMatch.Groups["name"].Success)
			//{
			//    localTableId = nameMatch.Groups["name"].Value;
			//}
			var table = SourceFragments.Find(match => match.Identifier == tableId);

			if (table == null && throwNotFoundException)
				throw new KeyNotFoundException(
					string.Format("SourceFragment with id '{0}' in entity '{1}' not found.", tableId, Identifier));
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
            Dictionary<string, int> relationUniques = new Dictionary<string, int>();
            FillUniqueRelations(l, relationUniques);
            if(BaseEntity != null)
            {
                var baseEntityRealation = from r in BaseEntity.GetRelations(withDisabled)
                                          where !l.Contains(r)
                                          select r;
                FillUniqueRelations(baseEntityRealation, relationUniques);
            }
            foreach (var relationUnique in relationUniques)
            {
                if (relationUnique.Value > 1)
                    throw new OrmCodeGenException("���������� ������������� M2M �����." + relationUnique.Key);
            }
			return l;
		}

	    private static void FillUniqueRelations<T>(IEnumerable<T> baseEntityRealation, IDictionary<string, int> relationUniques)
            where T : RelationDescriptionBase
	    {
	        foreach (var relationDescription in baseEntityRealation)
	        {
	            string key = string.Join("$$$", new[]
	                                                {
	                                                    relationDescription.SourceFragment.Name,
	                                                    relationDescription.Left.ToString(),
	                                                    relationDescription.Right.ToString(),
	                                                });
                if (relationDescription.UnderlyingEntity != null)
                {
                    EntityDescription superBaseEntity = relationDescription.UnderlyingEntity.SuperBaseEntity;
                    key += "$" + (superBaseEntity == null ? relationDescription.UnderlyingEntity.Name : superBaseEntity.Name);
                }

	            int val;
	            if(!relationUniques.TryGetValue(key, out val))
	                val = 0;
	            relationUniques[key] = ++val;

	        }
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
            Dictionary<string, int> relationUniques = new Dictionary<string, int>();
            FillUniqueRelations(l, relationUniques);
            if (BaseEntity != null)
            {
                var baseEntityRealation = BaseEntity.GetRelations(withDisabled);
                FillUniqueRelations(baseEntityRealation, relationUniques);
            }
            foreach (var relationUnique in relationUniques)
            {
                if (relationUnique.Value > 1)
                    throw new OrmCodeGenException("���������� ������������� M2M �����." + relationUnique.Key);
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

	    public EntityDescription SuperBaseEntity
	    {
	        get
	        {
                EntityDescription superbaseEntity;
                for (superbaseEntity = this;
                     superbaseEntity.BaseEntity != null;
                     superbaseEntity = superbaseEntity.BaseEntity)
                {

                }
                if (superbaseEntity == this)
                    superbaseEntity = null;
	            return superbaseEntity;
	        }
	    }

	    private readonly List<EntityRelationDescription> _relations = new List<EntityRelationDescription>();

	    public ICollection<EntityRelationDescription> EntityRelations
	    {
	        get
	        {
	            return _relations;
	        }
	    }

        public List<EntityRelationDescription> GetEntityRelations(bool withDisabled)
        {
            return _relations.FindAll(r => !r.Disabled);
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
			if (oldOne != null)
			{
				resultOne.CacheCheckRequired = oldOne.CacheCheckRequired;
				resultOne.Behaviour = oldOne.Behaviour;
				resultOne.MakeInterface = oldOne.MakeInterface;
				resultOne.UseGenerics = oldOne.UseGenerics;
			}
			else
			{
				resultOne.CacheCheckRequired = newOne.CacheCheckRequired;
				resultOne.Behaviour = newOne.Behaviour;
				resultOne.MakeInterface = newOne.MakeInterface;
				resultOne.UseGenerics = newOne.UseGenerics;
			}

			//��������� ����� ��������
			foreach (var newTable in newOne.SourceFragments)
			{
				resultOne.SourceFragments.Add(newTable);
			}
			// ��������� ����� ��������
			foreach (PropertyDescription newProperty in newOne.Properties)
			{
				PropertyDescription prop = newProperty.CloneSmart();
			    PropertyDescription newProperty1 = newProperty;
			    if (newOne.SuppressedProperties.Exists(match => match.Name == newProperty1.Name))
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
					foreach (var oldTable in oldOne.SourceFragments)
					{
					    var oldTable1 = oldTable;
					    if (!resultOne.SourceFragments.Exists(tableMatch => oldTable1.Name == tableMatch.Name && oldTable1.Selector == tableMatch.Selector))
							resultOne.SourceFragments.Insert(oldOne.SourceFragments.IndexOf(oldTable), oldTable);
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
						SourceFragmentDescription newTable = null; 
						if(oldProperty.SourceFragment != null)
							newTable = resultOne.GetSourceFragments(oldProperty.SourceFragment.Identifier);
						TypeDescription newType = oldProperty.PropertyType;
					    PropertyDescription oldProperty1 = oldProperty;
					    bool isSuppressed =
							resultOne.SuppressedProperties.Exists(match => match.Name == oldProperty1.Name);
						bool isRefreshed = false;
						const bool fromBase = true;
						if (newType.IsEntityType)
						{
						    TypeDescription newType1 = newType;
						    EntityDescription newEntity =
								resultOne.OrmObjectsDef.ActiveEntities.Find(
								    matchEntity =>
								    matchEntity.BaseEntity != null && matchEntity.BaseEntity.Identifier == newType1.Entity.Identifier);
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
				baseEntity = _baseEntity == null ? null : _baseEntity.CompleteEntity;
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

		public bool Disabled { get; set; }

		public bool CacheCheckRequired { get; set; }

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
                if (HasSinglePk) 
                    foreach (var propertyDescription in CompleteEntity.Properties)
                    {
                        if (propertyDescription.HasAttribute(Entities.Meta.Field2DbRelations.PK) 
                            //&& propertyDescription.PropertyType.IsClrType && propertyDescription.PropertyType.ClrType.IsAssignableFrom(typeof(Int32))
                            )
                            return propertyDescription;
                    }
                throw new InvalidOperationException("Only usable with single PK");
            }
	    }

	    public List<PropertyDescription> PkProperties
	    {
	        get
	        {
	            return Properties.FindAll(p => p.HasAttribute(Entities.Meta.Field2DbRelations.PK));
	        }
	    }

		public bool HasDefferedLoadableProperties
		{
			get
			{
				return Properties.Exists(p => !p.Disabled && !string.IsNullOrEmpty(p.DefferedLoadGroup));
			}
		}

		public bool HasDefferedLoadablePropertiesInHierarhy
		{
			get
			{
				return CompleteEntity.Properties.Exists(p => !p.Disabled && !string.IsNullOrEmpty(p.DefferedLoadGroup));
			}
		}

		public Dictionary<string, List<PropertyDescription>> GetDefferedLoadProperties()
		{
			Dictionary<string, List<PropertyDescription>> groups = new Dictionary<string, List<PropertyDescription>>();

			foreach (var property in Properties)
			{
				if (property.Disabled || string.IsNullOrEmpty(property.DefferedLoadGroup))
					continue;

				List<PropertyDescription> lst;
				if (!groups.TryGetValue(property.DefferedLoadGroup, out lst))
					groups[property.DefferedLoadGroup] = lst = new List<PropertyDescription>();

				lst.Add(property);
			}
			return groups;
			//var res = new List<PropertyDescription[]>();
			//foreach (var list in groups.Values)
			//{
			//    res.Add(list.ToArray());
			//}
			//return res.ToArray();
		}


		public bool IsMultitable
		{
			get
			{
				bool multitable = false;

				var entity = this;
				do
				{
					multitable |= entity.CompleteEntity.SourceFragments.Count > 1;
					entity = entity.BaseEntity;
				} while (!multitable && entity != null);
				return multitable;
			}
		}
	}
}
