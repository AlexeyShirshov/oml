using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using Worm.CodeGen.Core.Descriptors;
using Worm.Entities.Meta;
using LinqToCodedom.CodeDomPatterns;

namespace Worm.CodeGen.Core.CodeDomExtensions
{
	public class CodeEntityProperty : CodeMemberProperty
	{
		public CodeEntityProperty(PropertyDescription property)
		{
			Type = property.PropertyType;
			HasGet = true;
			HasSet = true;
			Name = property.PropertyName;
			Attributes = WXMLModelToCodeDomConverter.GetMemberAttribute(property.PropertyAccessLevel);
			if (property.Group != null && property.Group.Hide)
				Attributes = MemberAttributes.Family;

			var fieldName = OrmCodeGenNameHelper.GetPrivateMemberName(property.PropertyName);
			if (!property.FromBase)
			{
				CodeMethodInvokeExpression getUsingExpression = new CodeMethodInvokeExpression(
					new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "Read"),
					OrmCodeGenHelper.GetFieldNameReferenceExpression(property)
					);

				if (property.PropertyType.IsEntityType && property.PropertyType.Entity.CacheCheckRequired)
				{
					getUsingExpression.Parameters.Add(new CodePrimitiveExpression(true));
				}

				CodeStatement[] getInUsingStatements = new CodeStatement[]
				                                       	{
				                                       		new CodeMethodReturnStatement(
				                                       			new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
				                                       			                                 fieldName))
				                                       	};

                if (property.Entity.HasPkFlatEntity)
                    GetStatements.Add(new LinqToCodedom.CodeDomPatterns.CodeUsingStatement(
                        getUsingExpression,
                        getInUsingStatements)
                    );
                else
                    GetStatements.AddRange(getInUsingStatements);

				if (property.Entity.OrmObjectsDef.EnableReadOnlyPropertiesSetter ||
				    !property.HasAttribute(Field2DbRelations.ReadOnly) || property.HasAttribute(Field2DbRelations.PK))
				{
					CodeExpression setUsingExpression = new CodeMethodInvokeExpression(
						new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "Write"),
						OrmCodeGenHelper.GetFieldNameReferenceExpression(property)
						);

					List<CodeStatement> setInUsingStatements = new List<CodeStatement>();
					if(!property.Entity.EnableCommonEventRaise)
					{
						setInUsingStatements.Add(new CodeVariableDeclarationStatement(
						                         	typeof (bool),
						                         	"notChanged",
						                         	new CodeBinaryOperatorExpression(
						                         		new CodeFieldReferenceExpression(
						                         			new CodeThisReferenceExpression(),
						                         			fieldName
						                         			),
						                         		property.PropertyType.IsValueType
						                         			? CodeBinaryOperatorType.ValueEquality
						                         			: CodeBinaryOperatorType.IdentityEquality,
						                         		new CodePropertySetValueReferenceExpression()
						                         		)
						                         	));
						setInUsingStatements.Add(new CodeVariableDeclarationStatement(
													property.PropertyType,
						                         	"oldValue",
						                         	new CodeFieldReferenceExpression(
						                         		new CodeThisReferenceExpression(),
						                         		fieldName
						                         		)
						                         	));
					}
					setInUsingStatements.Add(new CodeAssignStatement(
					                                       			new CodeFieldReferenceExpression(
					                                       				new CodeThisReferenceExpression(),
					                                       				fieldName
					                                       				),
					                                       			new CodePropertySetValueReferenceExpression()
					                                       			)
					                                       	);
					if(!property.Entity.EnableCommonEventRaise)
					{
						setInUsingStatements.Add(new CodeConditionStatement(
						new CodeBinaryOperatorExpression(
							new CodePrimitiveExpression(false),
							CodeBinaryOperatorType.ValueEquality,
							new CodeVariableReferenceExpression("notChanged")
							),
						new CodeExpressionStatement(
							new CodeMethodInvokeExpression(
								new CodeThisReferenceExpression(),
								"RaisePropertyChanged",
								OrmCodeGenHelper.GetFieldNameReferenceExpression(property),
								new CodeVariableReferenceExpression("oldValue")
								)
							)
						));
					}

                    if (property.Entity.HasPkFlatEntity)
                        SetStatements.Add(new CodeUsingStatement(setUsingExpression,setInUsingStatements.ToArray()));
                    else
                        SetStatements.AddRange(setInUsingStatements.ToArray());
                }
				else
					HasSet = false;
			}
			else if( property.IsRefreshed)
			{
				Attributes |= MemberAttributes.New;
				GetStatements.Add(
				new CodeMethodReturnStatement(
					new CodeCastExpression(
						property.PropertyType,
						new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), property.Name)
					)
				)
				);
				if (property.Entity.OrmObjectsDef.EnableReadOnlyPropertiesSetter ||
				    !property.HasAttribute(Field2DbRelations.ReadOnly) || property.HasAttribute(Field2DbRelations.PK))
				{
					SetStatements.Add(
						new CodeAssignStatement(
							new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), property.Name),
							new CodePropertySetValueReferenceExpression()
							)
						);
				}
				else
				{
					HasSet = false;
				}
			}
			
			CodeAttributeDeclaration declaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(EntityPropertyAttribute)));

            if (!string.IsNullOrEmpty(property.PropertyAlias))
            {
                declaration.Arguments.Add(
                    new CodeAttributeArgument("PropertyAlias", OrmCodeGenHelper.GetFieldNameReferenceExpression(property)));
            }

			CustomAttributes.Add(declaration);

			WXMLModelToCodeDomConverter.SetMemberDescription(this, property.Description);	
		}
	}
}
