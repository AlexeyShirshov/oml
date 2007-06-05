using System;
using System.Collections.Generic;
using System.Text;

namespace OrmCodeGenLib.Descriptors
{
    public class PropertyDescription
    {
        private string _id;
        private string _name;
        private string _propertyAlias;
        private string[] _attributes;
        private string _description;
        private TypeDescription _type;
        private string _fieldName;
        private TableDescription _table;
        private bool _fromBase;
        private AccessLevel _fieldAccessLevel;
        private AccessLevel _propertyAccessLevel;

        public PropertyDescription(string id, string name, string alias, string[] attributes, string description, TypeDescription type, string fieldname, TableDescription table, AccessLevel fieldAccessLevel, AccessLevel propertyAccessLevel) : this(id, name, alias, attributes, description, type, fieldname, table, false, fieldAccessLevel, propertyAccessLevel)
        {
        }

        internal PropertyDescription(string id, string name, string alias, string[] attributes, string description, TypeDescription type, string fieldname, TableDescription table, bool fromBase, AccessLevel fieldAccessLevel, AccessLevel propertyAccessLevel)
        {
            _id = id;
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
        }

        public string Identifier
        {
            get { return _id; }
            set { _id = value; }
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
            get { return _propertyAlias; }
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
    }
}
