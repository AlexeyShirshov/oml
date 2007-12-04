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

	}
}
