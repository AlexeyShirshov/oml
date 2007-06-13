using System;
using System.Collections.Generic;
using System.Text;

namespace OrmCodeGenLib.Descriptors
{
    public class RelationDescription
    {
        private readonly LinkTarget _left;
        private readonly LinkTarget _right;
        private readonly TableDescription _table;
        private readonly EntityDescription _underlyingEntity;
        private bool _disabled;

        public RelationDescription(LinkTarget left, LinkTarget right, TableDescription table, EntityDescription underlyingEntity)
            : this(left, right, table, underlyingEntity, false)
        {
        }

        public RelationDescription(LinkTarget left, LinkTarget right, TableDescription table, EntityDescription underlyingEntity, bool disabled)
        {
            _table = table;
            _underlyingEntity = underlyingEntity;
            _left = left;
            _right = right;
            _disabled = disabled;
        }

        public TableDescription Table
        {
            get { return _table; }
        }

        public EntityDescription UnderlyingEntity
        {
            get { return _underlyingEntity; }
        } 

        public LinkTarget Left
        {
            get { return _left; }
        }     

        public LinkTarget Right
        {
            get { return _right; }
        }

        public bool Disabled
        {
            get { return _disabled; }
            set { _disabled = value; }
        }

        public static bool IsSimilar(RelationDescription first, RelationDescription second)
        {
            bool yep = false;
            yep |= LinkTarget.IsSimilar(first.Left, second.Left) && LinkTarget.IsSimilar(first.Right, second.Right);
            yep |= LinkTarget.IsSimilar(first.Left, second.Right) && LinkTarget.IsSimilar(first.Right, second.Left);
            return yep;
        }
    }
}
