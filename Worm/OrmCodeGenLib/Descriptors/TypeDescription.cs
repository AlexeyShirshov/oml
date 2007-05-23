using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace OrmCodeGenLib.Descriptors
{
    public class TypeDescription
    {
        private readonly string _id;
        private readonly string _userType;
        private readonly Type _clrType;
        private readonly EntityDescription _entity;

        public TypeDescription(string id, string typeName, bool treatAsUserType)
            : this(id, typeName, null, treatAsUserType)
        {
        }

        public TypeDescription(string id, string typeName)
            : this(id, typeName, null, false)
        {
        }

        public TypeDescription(string id, EntityDescription entity)
            : this(id, null, entity, false)
        {
        }

        public TypeDescription(string id, Type type) : this(id, null, null, false)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            _clrType = type;
        }

        protected TypeDescription(string id, string typeName, EntityDescription entity, bool treatAsUserType)
        {
            _id = id;
            if(!string.IsNullOrEmpty(typeName))
                if (treatAsUserType)
                {
                    _userType = typeName;
                }
                else
                {
                    _clrType = GetTypeByName(typeName);
                }
            _entity = entity;
        }

        public string Identifier
        {
            get { return _id; }
        }

        public Type ClrType
        {
            get
            {
                if (_clrType == null)
                    throw new InvalidOperationException("Valid only for ClrType. Use 'IsClrType' at first.");
                return _clrType;
            }
        }

        public string ClrTypeName
        {
            get
            {
                if (_clrType == null)
                    throw new InvalidOperationException("Valid only for ClrType. Use 'IsClrType' at first.");
                return _clrType.FullName;
            }
        }

        public EntityDescription Entity
        {
            get { return _entity; }
        }

        public string TypeName
        {
            get
            {
                if (IsClrType)
                    return _clrType.FullName;
                if (IsUserType)
                    return _userType;
                return _entity.QualifiedName;
            }
        }

        public bool IsClrType
        {
            get
            {
                return _clrType != null;
            }
        }

        public bool IsUserType
        {
            get
            {
                return _clrType == null && !string.IsNullOrEmpty(_userType);
            }
        }

        public bool IsEntityType
        {
            get
            {
                return _entity != null;
            }
        }

        public override string ToString()
        {
            return TypeName;
        }

        private Type GetTypeByName(string typeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName, false, true);
                if (type != null)
                    return type;
            }
            throw new TypeLoadException(String.Format("Cannot find type by given name '{0}'", typeName));
        }
    }
}
