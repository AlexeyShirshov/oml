using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using Worm.CodeGen.Core.Descriptors;

namespace Worm.CodeGen.Core.CodeDomExtensions
{
	public class CodeEntityPropertyField : CodeMemberField
	{
		public CodeEntityPropertyField(PropertyDescription property)
		{
			Type = property.PropertyType;
			Name = OrmCodeGenNameHelper.GetPrivateMemberName(property.PropertyName);
			Attributes = OrmCodeDomGenerator.GetMemberAttribute(property.FieldAccessLevel);
		}
	}
}
