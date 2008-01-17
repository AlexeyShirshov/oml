using System;
using System.Collections.Generic;
using System.Text;

namespace OrmCodeGenLib.Descriptors
{
	public class SelfRelationTarget
	{
		private readonly string _fieldName;
		private readonly bool _cascadeDelete;

		public SelfRelationTarget(string fieldName, bool cascadeDelete)
        {
            _fieldName = fieldName;
            _cascadeDelete = cascadeDelete;
        }
        
        public string FieldName
        {
            get { return _fieldName; }
        }   

        public bool CascadeDelete
        {
            get { return _cascadeDelete; }
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
	}
}
