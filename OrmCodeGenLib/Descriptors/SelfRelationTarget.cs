﻿namespace Worm.CodeGen.Core.Descriptors
{
	public class SelfRelationTarget
	{
		private string _fieldName;
		private bool _cascadeDelete;
		private string _accessorName;
		private TypeDescription _accessedEntityType;

		public SelfRelationTarget(string fieldName, bool cascadeDelete) : this(fieldName, cascadeDelete, null)
		{
		}

		public SelfRelationTarget(string fieldName, bool cascadeDelete, string accessorName)
        {
            _fieldName = fieldName;
            _cascadeDelete = cascadeDelete;
			_accessorName = accessorName;
        }
        
        public string FieldName
        {
            get { return _fieldName; }
			set { _fieldName = value; }
        }   

        public bool CascadeDelete
        {
            get { return _cascadeDelete; }
			set { _cascadeDelete = value; }
        }

		public string AccessorName
		{
			get { return _accessorName; }
			set { _accessorName = value; }
		}

		public TypeDescription AccessedEntityType
		{
			get { return _accessedEntityType; }
			set { _accessedEntityType = value; }
		}

		public override bool Equals(object obj)
        {
            return base.Equals(obj as SelfRelationTarget);
        }

        public bool Equals(SelfRelationTarget obj)
        {
            if (obj == null)
                return false;
            return _fieldName == obj._fieldName && _cascadeDelete == obj._cascadeDelete;
        }

        public override int GetHashCode()
        {
            return _fieldName.GetHashCode() ^ _cascadeDelete.GetHashCode();
        }

        public static bool operator ==(SelfRelationTarget f, SelfRelationTarget s)
        {
            if (!ReferenceEquals(f, null))
                return f.Equals(s);
            else if (!ReferenceEquals(s, null))
                return false;
            return true;
        }

        public static bool operator !=(SelfRelationTarget f, SelfRelationTarget s)
        {
            if (!ReferenceEquals(f, null))
                return !f.Equals(s);
            else if (!ReferenceEquals(s, null))
                return true;
            return false;
        }

        public override string ToString()
        {
            return FieldName;
        }
	}
}
