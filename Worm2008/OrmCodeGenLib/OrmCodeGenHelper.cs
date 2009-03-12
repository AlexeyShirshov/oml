using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;
using Worm.CodeGen.Core.Descriptors;

namespace Worm.CodeGen.Core
{
    static class OrmCodeGenHelper
    {
        public static CodeExpression GetFieldNameReferenceExpression(PropertyDescription propertyDesc)
        {
            string className = OrmCodeGenNameHelper.GetEntityClassName(propertyDesc.Entity, true) + ".Properties";
            return new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(className),
                                             propertyDesc.PropertyName);
        }

        public static CodeExpression GetEntityNameReferenceExpression(EntityDescription entityDescription)
        {
            string className = OrmCodeGenNameHelper.GetEntityClassName(entityDescription, true) + ".Descriptor";
            return new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(className), "EntityName");
        }

        public static CodeExpression GetEntityClassReferenceExpression(EntityDescription entityDesc)
        {
            string className = OrmCodeGenNameHelper.GetEntityClassName(entityDesc, true);
            return new CodeTypeReferenceExpression(className);
        }

        public static CodeTypeReference GetEntityClassTypeReference(EntityDescription entityDesc)
        {
            string className = OrmCodeGenNameHelper.GetEntityClassName(entityDesc);
            return new CodeTypeReference(className);
        }

        public static CodeTypeOfExpression GetEntityClassTypeReferenceExpression(EntityDescription entityDesc)
        {
            return new CodeTypeOfExpression(GetEntityClassTypeReference(entityDesc));
        }

        public static CodeExpression GetPropertyReferenceExpression(PropertyDescription propertyDesc, OrmCodeDomGeneratorSettings settings)
        {
            return new CodePropertyReferenceExpression(GetEntityClassReferenceExpression(propertyDesc.Entity), propertyDesc.PropertyName);
        }
    }
}
