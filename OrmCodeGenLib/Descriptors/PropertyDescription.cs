using System;
using System.Collections.Generic;
using System.Text;

namespace OrmCodeGenLib.Descriptors
{
    public class PropertyDescription : ICloneable
    {
        private string _name;
        private string _propertyAlias;
        private string[] _attributes;
        private string _description;
        private TypeDescription _type;
        private string _fieldName;
        private TableDescription _table;
        private bool _fromBase;
        private bool _isRefreshed;
        private AccessLevel _fieldAccessLevel;
        private AccessLevel _propertyAccessLevel;
        private bool _isSuppressed;
        private EntityDescription _entity;

        public PropertyDescription(EntityDescription entity, string name)
            : this(entity, name, null, null, null, null, null, null, false, default(AccessLevel), default(AccessLevel), true, false)
        {
        }

        public PropertyDescription(string name)
            : this(null, name, null, null, null, null, null, null, false, default(AccessLevel), default(AccessLevel), true, false)
        {
        }

        public PropertyDescription(EntityDescription entity, string name, string alias, string[] attributes, string description, TypeDescription type, string fieldname, TableDescription table, AccessLevel fieldAccessLevel, AccessLevel propertyAccessLevel)
            : this(entity,name, alias, attributes, description, type, fieldname, table, false, fieldAccessLevel, propertyAccessLevel, false, false)
        {
        }

        public PropertyDescription(string name, string alias, string[] attributes, string description, TypeDescription type, string fieldname, TableDescription table, AccessLevel fieldAccessLevel, AccessLevel propertyAccessLevel) : this(null,name, alias, attributes, description, type, fieldname, table, false, fieldAccessLevel, propertyAccessLevel, false, false)
        {
        }

        internal PropertyDescription(EntityDescription entity, string name, string alias, string[] attributes, string description, TypeDescription type, string fieldname, TableDescription table, bool fromBase, AccessLevel fieldAccessLevel, AccessLevel propertyAccessLevel, bool isSuppressed, bool isRefreshed)
        {
            _name = name;
            _propertyAlias = alias;
            _attributes = attributes;
            _description = description;
            _type = type;
            _fieldName = fieldname;
            _table = table;
            _fromBase = fromBase;
            _fieldAccessLevel = fieldAccessLevel;
            _propertyAccessLevel = propertyAccessLevel;
            _isSuppressed = isSuppressed;
            _isRefreshed = isRefreshed;
            _entity = entity;
        }
        
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
                
        public string[] Attributes
        {
            get { return _attributes; }
            set { _attributes = value; }
        }
        
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public TypeDescription PropertyType
        {
            get { return _type; }
            set { _type = value; }
        }
                
        public string FieldName
        {
            get { return _fieldName; }
            set { _fieldName = value; }
        }
                
        public TableDescription Table
        {
            get { return _table; }
            set { _table = value; }
        }

        public bool FromBase
        {
            get { return _fromBase; }
            set { _fromBase = value; }
        }

        public string PropertyAlias
        {
            get { return string.IsNullOrEmpty(_propertyAlias) ? _name : _propertyAlias; }
            set { _propertyAlias = value; }
        }

        public AccessLevel FieldAccessLevel
        {
            get { return _fieldAccessLevel; }
            set { _fieldAccessLevel = value; }
        }

        public AccessLevel PropertyAccessLevel
        {
            get { return _propertyAccessLevel; }
            set { _propertyAccessLevel = value; }
        }

        public bool IsSuppressed
        {
            get { return _isSuppressed; }
            set { _isSuppressed = value; }
        }

        public bool IsRefreshed
        {
            get { return _isRefreshed; }
            set { _isRefreshed = value; }
        }

        public EntityDescription Entity
        {
            get { return _entity; }
            set { _entity = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            PropertyDescription prop = (PropertyDescription)MemberwiseClone();
            prop._attributes = this._attributes;
            prop._description = this._description;
            prop._fieldAccessLevel = this._fieldAccessLevel;
            prop._fieldName = _fieldName;
            prop._fromBase = _fromBase;
            prop._isSuppressed = _isSuppressed;
            prop._name = _name;
            prop._propertyAccessLevel = _propertyAccessLevel;
            prop._propertyAlias = _propertyAlias;
            prop._table = _table;
            prop._type = _type;
            return prop;
        }

        public PropertyDescription CloneSmart()
        {
            return (PropertyDescription) Clone();
        }

        #endregion
    }
}
