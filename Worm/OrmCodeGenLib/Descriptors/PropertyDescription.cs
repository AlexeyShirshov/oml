using System;
using System.Collections.Generic;
using System.Text;

namespace OrmCodeGenLib.Descriptors
{
    public class PropertyDescription
    {
        private string _id;
        private string _name;
        private string[] _attributes;
        private string _description;
        private TypeDescription _type;
        private string _fieldName;
        private TableDescription _table;
        private bool _fromBase;

        public PropertyDescription(string id, string name, string[] attributes, string description, TypeDescription type, string fieldname, TableDescription table)
            : this(id, name, attributes, description, type, fieldname, table, false)
        {
        }

        public PropertyDescription(string id, string name, string[] attributes, string description, TypeDescription type, string fieldname, TableDescription table, bool fromBase)
        {
            _id = id;
            _name = name;
            _attributes = attributes;
            _description = description;
            _type = type;
            _fieldName = fieldname;
            _table = table;
            _fromBase = fromBase;
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
                
        public string PropertyTypeString
        {
            get { return _type.TypeName; }
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
    }
}
