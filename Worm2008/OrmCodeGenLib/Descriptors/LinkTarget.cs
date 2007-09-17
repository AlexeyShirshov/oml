using System;
using System.Collections.Generic;
using System.Text;

namespace OrmCodeGenLib.Descriptors
{
    public class LinkTarget
    {
        private EntityDescription _entity;
        private string _fieldName;
        private bool _cascadeDelete;

        public LinkTarget(EntityDescription entity, string fieldName, bool cascadeDelete)
        {
            _entity = entity;
            _fieldName = fieldName;
            _cascadeDelete = cascadeDelete;
        }

        public EntityDescription Entity
        {
            get { return _entity; }
            set { _entity = value; }
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

        public static bool IsSimilar(LinkTarget first, LinkTarget second)
        {
            bool yep = true;
            yep &= first.Entity.Name == second.Entity.Name;
            yep &= first.FieldName == second.FieldName;
            return yep;
        }
    }
}
