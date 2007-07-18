using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;
using OrmCodeGenLib.Descriptors;

namespace OrmCodeGenLib
{
    static class OrmCodeGenHelper
    {
        public static CodeExpression GetFieldNameReferenceExpression(PropertyDescription propertyDesc, OrmCodeDomGeneratorSettings settings)
        {
            string className = OrmCodeGenNameHelper.GetEntityClassName(propertyDesc.Entity, settings) + ".Properties";
            return new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(className),
                                             propertyDesc.Name);
        }

        public static CodeExpression GetEntityClassReferenceExpression(EntityDescription entityDesc, OrmCodeDomGeneratorSettings settings)
        {
            string className = OrmCodeGenNameHelper.GetEntityClassName(entityDesc, settings);
            return new CodeTypeReferenceExpression(className);
        }

        public static CodeExpression GetPropertyReferenceExpression(PropertyDescription propertyDesc, OrmCodeDomGeneratorSettings settings)
        {
            return new CodePropertyReferenceExpression(GetEntityClassReferenceExpression(propertyDesc.Entity, settings),
                                                propertyDesc.Name);
        }
    }
}
