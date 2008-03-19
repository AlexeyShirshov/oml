using System;
using System.Reflection;

namespace Worm.CodeGen.Core.Descriptors
{
    public class TypeDescription
    {
        private readonly string _id;
        private readonly string _userType;
        private readonly Type _clrType;
        private readonly EntityDescription _entity;
        private readonly UserTypeHintFlags? _userTpeHint;

        public TypeDescription(string id, string typeName, bool treatAsUserType)
            : this(id, typeName, null, treatAsUserType, null)
        {
        }

        public TypeDescription(string id, string typeName)
            : this(id, typeName, null, false, null)
        {
        }

        public TypeDescription(string id, EntityDescription entity)
            : this(id, null, entity, false, null)
        {
        }

        public TypeDescription(string id, Type type) : this(id, null, null, false, null)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            _clrType = type;
        }

        public TypeDescription(string id, string typeName, UserTypeHintFlags? userTypeHint)
            : this(id, typeName, null, true, userTypeHint)
        {
            
        }

        protected TypeDescription(string id, string typeName, EntityDescription entity, bool treatAsUserType, UserTypeHintFlags? userTypeHint)
        {
            _id = id;
            if(!string.IsNullOrEmpty(typeName))
                if (treatAsUserType)
                {
                    _userType = typeName;
                    _userTpeHint = userTypeHint;
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
            	return OrmCodeGenNameHelper.GetEntityClassName(_entity, true);

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

        public bool IsValueType
        {
            get
            {
                return
                    (!IsEntityType) &&
                    (
                        ( IsClrType && typeof(ValueType).IsAssignableFrom(ClrType) )
                         ||
                        (IsUserType && UserTypeHint.HasValue && UserTypeHint.Value != UserTypeHintFlags.None)
                    );
                
            }
        }

        public bool IsEnum
        {
            get
            {
                return (IsValueType) &&
                       (
                           (IsClrType && ClrType.IsEnum)
                           ||
                           (IsUserType && UserTypeHint.HasValue &&
                            ((UserTypeHint.Value & UserTypeHintFlags.Enum) == UserTypeHintFlags.Enum))
                       );
            }
        }

        public bool IsNullableType
        {
            get
            {
                return (IsValueType) && 
                (
                    (IsClrType && ClrType.IsGenericType && typeof(Nullable<>).Equals(ClrType.GetGenericTypeDefinition()))
                    || (IsUserType && UserTypeHint.HasValue && ((UserTypeHint.Value & UserTypeHintFlags.Nullable) == UserTypeHintFlags.Nullable))
                );
            }
        }

        public UserTypeHintFlags? UserTypeHint
        {
            get { return _userTpeHint; }
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

    [Flags]
    public enum UserTypeHintFlags
    {
        None = 0x0000,
        Enum = 0x0001,
        ValueType = 0x0002,
        Nullable = 0x0004,
		Char = 0 >> 1,
    }
}
