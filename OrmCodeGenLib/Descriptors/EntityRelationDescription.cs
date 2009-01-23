using System;
using System.Collections.Generic;
using System.Text;

namespace Worm.CodeGen.Core.Descriptors
{
    public class EntityRelationDescription
    {
        public EntityDescription SourceEntity
        {
            get;
            set;
        }

        public EntityDescription Entity
        {
            get;
            set;
        }

        public string PropertyAlias
        {
            get;
            set;
        }

        public bool Disabled
        {
            get;
            set;
        }

        public string AccessorName
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public PropertyDescription Property
        {
            get
            {
                PropertyDescription res = null;
                if(!string.IsNullOrEmpty(PropertyAlias))
                {
                    res = Entity.Properties.Find(p => p.PropertyAlias == PropertyAlias);
                }
                else
                {
                    var lst = Entity.Properties.FindAll(p => p.PropertyType.IsEntityType && p.PropertyType.Entity == SourceEntity);
                    if (lst.Count > 1)
                    {
                        throw new OrmCodeGenException(
                            string.Format(
                                "Возможно несколько вариантов связи от сущности '{0}' к '{1}'. Конкретизируйте связи.",
                                SourceEntity.Name, Entity.Name));
                    }
                    else if (lst.Count > 0)
                    {
                        res = lst[0];
                    }
                }
                return res;
            }
        }
    }
}
