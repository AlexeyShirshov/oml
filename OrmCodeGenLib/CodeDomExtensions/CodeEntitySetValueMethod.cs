﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using Worm.CodeGen.Core.Descriptors;
using Worm.Entities;
using Worm.Entities.Meta;

namespace Worm.CodeGen.Core.CodeDomExtensions
{
	public class CodeEntitySetValueMethod : CodeMemberMethod
	{
		public CodeEntitySetValueMethod(EntityDescription entity)
		{
			Name = "SetValueOptimized";
			// тип возвращаемого значения
			ReturnType = null;
			// модификаторы доступа
			Attributes = MemberAttributes.Public;
			if (entity.BaseEntity != null)
				Attributes |= MemberAttributes.Override;
			else
				ImplementationTypes.Add(new CodeTypeReference(typeof(IOptimizedValues)));
			Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "propertyAlias"));
			Parameters.Add(new CodeParameterDeclarationExpression(typeof(IEntitySchema), "schema"));
			Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "value"));
			Statements.Add(
				new CodeVariableDeclarationStatement(
					new CodeTypeReference(typeof(string)),
					"fieldName",
					new CodeArgumentReferenceExpression("propertyAlias")
				)
			);

			foreach (var property in entity.CompleteEntity.Properties)
			{
				if (property.Disabled)
					continue;

				WXMLModelToCodeDomConverter.Delegates.UpdateSetValueMethodMethod(property, this);
			}

			if (entity.BaseEntity != null)
				Statements.Add(
					new CodeMethodInvokeExpression(
						new CodeMethodReferenceExpression(
							new CodeBaseReferenceExpression(),
							"SetValueOptimized"
							),
						new CodeArgumentReferenceExpression("propertyAlias"),
						new CodeArgumentReferenceExpression("schema"),
						new CodeArgumentReferenceExpression("value")
						)
					);
		}
	}
}
