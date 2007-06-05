using System;
using System.Collections.Generic;
using System.Text;

namespace OrmCodeGenLib.Descriptors
{
    public class EntityDescription
    {
        #region Private Fields
        private readonly string _id;
        private readonly string _name;
        private readonly string _description;
        private string _namespace;
        private readonly List<TableDescription> _tables;
        private readonly List<PropertyDescription> _properties;
        private readonly OrmObjectsDef _ormObjectsDef;
        private EntityDescription _baseEntity;
        private EntityBehaviuor _behaviour;
        #endregion Private Fields

        public EntityDescription(string id, string name, string nameSpace, string description, OrmObjectsDef ormObjectsDef)
            : this(id, name, nameSpace, description, ormObjectsDef, null)
        {
        }

        public EntityDescription(string id, string name, string nameSpace, string description, OrmObjectsDef ormObjectsDef, EntityDescription baseEntity)
            : this(id, name ,nameSpace, description, ormObjectsDef, baseEntity, EntityBehaviuor.Default)
        {
            
        }

        public EntityDescription(string id, string name, string nameSpace, string description, OrmObjectsDef ormObjectsDef, EntityDescription baseEntity, EntityBehaviuor behaviour)
        {
            _id = id;
            _name = name;
            _description = description;
            _tables = new List<TableDescription>();
            _properties = new List<PropertyDescription>();
            _ormObjectsDef = ormObjectsDef;
            _namespace = nameSpace;
            _baseEntity = baseEntity;
            _behaviour = behaviour;
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

        public PropertyDescription GetProperty(string propertyId)
        {
            return GetProperty(propertyId, false);
        }

        public PropertyDescription GetProperty(string propertyId, bool throwNotFoundException)
        {
            PropertyDescription result = this.Properties.Find(delegate(PropertyDescription match)
            {
                return match.Identifier == propertyId;
            });
            if (result == null && throwNotFoundException)
                throw new KeyNotFoundException(
                    string.Format("Property with id '{0}' in entity '{1}' not found.", propertyId, this.Identifier));
            return result;
        }

        public TableDescription GetTable(string tableId)
        {
            return GetTable(tableId, false);
        }

        public TableDescription GetTable(string tableId, bool throwNotFoundException)
        {
            TableDescription table;
            //System.Text.RegularExpressions.Match nameMatch = OrmCodeGenLib.OrmObjectsDef.GetNsNameMatch(tableId);
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

        public List<RelationDescription> GetRelations()
        {
            return _ormObjectsDef.Relations.FindAll(
                delegate(RelationDescription match) { return match.Left.Entity == this || match.Right.Entity == this; }
                );
        }

        public string Namespace
        {
            get { return string.IsNullOrEmpty(_namespace) ? _ormObjectsDef.Namespace : _namespace; }
            set { _namespace = value; }
        }

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
                new EntityDescription(newOne.Identifier, newOne.Name, newOne.Namespace, newOne.Description,
                                      newOne.OrmObjectsDef);

            //��������� ����� ��������
            foreach (TableDescription newTable in newOne.Tables)
            {
                resultOne.Tables.Add(newTable);
            }
            // ��������� ����� ��������
            foreach (PropertyDescription newProperty in newOne.Properties)
            {
                resultOne.Properties.Add(newProperty);
            }

            if(oldOne != null)
            {
                // ��������� ������ ��������, ���� �����
                foreach (TableDescription oldTable in oldOne.Tables)
                {
                    if (!resultOne.Tables.Exists(delegate(TableDescription tableMatch) { return oldTable.Name == tableMatch.Name; }))
                        resultOne.Tables.Add(oldTable);
                }

                // ��������� ������ ��������, ���� �����
                foreach (PropertyDescription oldProperty in oldOne.Properties)
                {
                    PropertyDescription newProperty = resultOne.GetProperty(oldProperty.Identifier);
                    if (newProperty == null)
                    {
                        TableDescription newTable = resultOne.GetTable(oldProperty.Table.Identifier);
                        TypeDescription newType = oldProperty.PropertyType;
                        if(newType.IsEntityType)
                        {
                            EntityDescription newEntity =
                                resultOne.OrmObjectsDef.Entities.Find(delegate(EntityDescription matchEntity)
                                                                      {
                                                                          return matchEntity.BaseEntity != null && matchEntity.BaseEntity.Identifier == newType.Entity.Identifier;
                                                                      });
                            if(newEntity != null)
                                newType = new TypeDescription(newType.Identifier, newEntity);
                        }
                        if(newType != oldProperty.PropertyType)
                            resultOne.Properties.Insert(resultOne.Properties.Count - newOne.Properties.Count,
                                                    new PropertyDescription(oldProperty.Identifier, oldProperty.Name, oldProperty.PropertyAlias,
                                                                            oldProperty.Attributes,
                                                                            oldProperty.Description,
                                                                            newType,
                                                                            oldProperty.FieldName, newTable, true, oldProperty.FieldAccessLevel, oldProperty.PropertyAccessLevel));
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
                if(_baseEntity == null)
                    baseEntity = null;
                else
                    baseEntity = _baseEntity.CompleteEntity;
                return MergeEntities(baseEntity, this);
            }
        }

        public EntityBehaviuor Behaviour
        {
            get { return _behaviour; }
            set { _behaviour = value; }
        }
    }
}
