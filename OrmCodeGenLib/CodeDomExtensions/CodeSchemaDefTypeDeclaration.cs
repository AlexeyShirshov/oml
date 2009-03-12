using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Worm.CodeGen.Core.Descriptors;

namespace Worm.CodeGen.Core.CodeDomExtensions
{
	public class CodeSchemaDefTypeDeclaration : CodeTypeDeclaration
	{
		private CodeEntityTypeDeclaration m_entityClass;
		private readonly CodeTypeReference m_typeReference;

		public CodeSchemaDefTypeDeclaration()
		{
			m_typeReference = new CodeTypeReference();
			IsClass = true;
			TypeAttributes = TypeAttributes.Class | TypeAttributes.NestedPublic;
			PopulateBaseTypes += OnPopulateBaseTypes;
			PopulateMembers += OnPopulateMembers;
		}

		protected void OnPopulateMembers(object sender, EventArgs e)
		{
			OnPopulateIDefferedLoadingInterfaceMemebers();
		}

		protected void OnPopulateIDefferedLoadingInterfaceMemebers()
		{
			if (m_entityClass == null || m_entityClass.Entity == null || !m_entityClass.Entity.HasDefferedLoadableProperties)
				return;

			var method = new CodeMemberMethod
			             	{
			             		Name = "GetDefferedLoadPropertiesGroups",
			             		Attributes = MemberAttributes.Public,
			             		ReturnType = new CodeTypeReference(typeof (string[][]))
			             	};

			// string[][] result;
			//method.Statements.Add(new CodeVariableDeclarationStatement(method.ReturnType, "result"));

			var defferedLoadPropertiesGrouped = m_entityClass.Entity.GetDefferedLoadProperties();

			var baseFieldName = method.Name;

			var fieldName = OrmCodeGenNameHelper.GetPrivateMemberName(method.Name);
			var dicFieldName = OrmCodeGenNameHelper.GetPrivateMemberName(baseFieldName + "Dic");
			var dicFieldTypeReference = new CodeTypeReference(typeof (Dictionary<string, List<string>>));

			if (m_entityClass.Entity.BaseEntity == null ||
			    !m_entityClass.Entity.BaseEntity.HasDefferedLoadablePropertiesInHierarhy)
			{

				var dicField = new CodeMemberField(dicFieldTypeReference, dicFieldName)
				               	{
				               		Attributes = MemberAttributes.Family,
				               		InitExpression = new CodeObjectCreateExpression(dicFieldTypeReference)
				               	};
				Members.Add(dicField);
			}

			var field = new CodeMemberField(method.ReturnType, fieldName);
			Members.Add(field);

			var lockObjFieldName = OrmCodeGenNameHelper.GetPrivateMemberName(baseFieldName + "Lock");

			var lockObj = new CodeMemberField(new CodeTypeReference(typeof (object)), lockObjFieldName);
			lockObj.InitExpression = new CodeObjectCreateExpression(lockObj.Type);
			Members.Add(lockObj);

			CodeExpression condition = new CodeBinaryOperatorExpression(
				new CodeFieldReferenceExpression(
					new CodeThisReferenceExpression(),
					field.Name
					),
				CodeBinaryOperatorType.IdentityEquality,
				new CodePrimitiveExpression(null));

			CodeStatementCollection inlockStatemets = new CodeStatementCollection();

			CodeVariableDeclarationStatement listVar =
				new CodeVariableDeclarationStatement(new CodeTypeReference(typeof (List<string>)), "lst");
			inlockStatemets.Add(listVar);

			foreach (var propertyDescriptions in defferedLoadPropertiesGrouped)
			{
				inlockStatemets.Add(
					new CodeConditionStatement(
						new CodeBinaryOperatorExpression(
							new CodeMethodInvokeExpression(
								new CodeFieldReferenceExpression(
									new CodeThisReferenceExpression(),
									dicFieldName
									),
								"TryGetValue",
								new CodePrimitiveExpression(propertyDescriptions.Key),
								new CodeDirectionExpression(FieldDirection.Out, new CodeVariableReferenceExpression(listVar.Name))
								),
							CodeBinaryOperatorType.ValueEquality,
							new CodePrimitiveExpression(false)

							),
						new CodeAssignStatement(new CodeVariableReferenceExpression(listVar.Name),
						                        new CodeObjectCreateExpression(
						                        	new CodeTypeReference(typeof (List<string>)))),
						new CodeExpressionStatement(new CodeMethodInvokeExpression(
						                            	new CodeFieldReferenceExpression(
						                            		new CodeThisReferenceExpression(), dicFieldName
						                            		),
						                            	"Add",
						                            	new CodePrimitiveExpression(propertyDescriptions.Key),
						                            	new CodeVariableReferenceExpression(listVar.Name))

							))
					);

				foreach (var propertyDescription in propertyDescriptions.Value)
				{
					inlockStatemets.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(listVar.Name), "Add",
					                                                   OrmCodeGenHelper.GetFieldNameReferenceExpression(
					                                                   	propertyDescription)));
				}
			}
			// List<string[]> res = new List<string[]>();
			// foreach(List<string> lst in m_GetDefferedLoadPropertiesGroupsDic.Values)
			// {
			//		res.Add(lst.ToArray());
			// }
			// m_GetDefferedLoadPropertiesGroups = res.ToArray()


			inlockStatemets.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(List<string[]>)), "res", new CodeObjectCreateExpression(new CodeTypeReference(typeof(List<string[]>)))));
			inlockStatemets.Add(
				OrmCodeDomGenerator.Delegates.CodePatternForeachStatement(
					new CodeTypeReference(typeof(List<string>)), "l",
					new CodePropertyReferenceExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName + "Dic"), "Values"),
					new CodeExpressionStatement(new CodeMethodInvokeExpression(
						new CodeVariableReferenceExpression("res"),
                        "Add",
						new CodeMethodInvokeExpression(
							new CodeArgumentReferenceExpression("l"),
							"ToArray"
						)
			                    	))));

			inlockStatemets.Add(
				new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName),
				                        new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("res"), "ToArray")));


			//inlockStatemets.Add(new CodeVariableDeclarationStatement(
			//                            method.ReturnType,
			//                            "groups",
			//                            array
			//                            ));

			if (m_entityClass.Entity.BaseEntity != null && m_entityClass.Entity.BaseEntity.CompleteEntity.HasDefferedLoadableProperties)
			{
				//method.Attributes |= MemberAttributes.Override;

				//// string[][] baseArray;
				//var tempVar = new CodeVariableDeclarationStatement(method.ReturnType, "baseGroups");

				//inlockStatemets.Add(tempVar);
				//// baseArray = base.GetDefferedLoadPropertiesGroups()
				//inlockStatemets.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("baseGroups"),
				//                                              new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(),
				//                                                                             method.Name)));

				//// Array.Resize<string[]>(ref groups, baseGroups.Length, groups.Length)
				//inlockStatemets.Add(
				//    new CodeMethodInvokeExpression(
				//        new CodeMethodReferenceExpression(
				//            new CodeTypeReferenceExpression(new CodeTypeReference(typeof(Array))),
				//            "Resize",
				//            new CodeTypeReference(typeof(string[]))),
				//        new CodeDirectionExpression(FieldDirection.Ref,
				//                                    new CodeVariableReferenceExpression("groups")),
				//        new CodeBinaryOperatorExpression(
				//            new CodePropertyReferenceExpression(
				//                new CodeVariableReferenceExpression("baseGroups"),
				//                "Length"
				//                ),
				//            CodeBinaryOperatorType.Add,
				//            new CodePropertyReferenceExpression(
				//                new CodeVariableReferenceExpression("groups"),
				//                "Length"
				//                )
				//            )
				//        )
				//    );
				//// Array.Copy(baseGroups, 0, groups, groups.Length - baseGroups.Length, baseGroups.Length)
				//inlockStatemets.Add(
				//    new CodeMethodInvokeExpression(
				//        new CodeTypeReferenceExpression(typeof(Array)),
				//        "Copy",
				//        new CodeVariableReferenceExpression("baseGroups"),
				//        new CodePrimitiveExpression(0),
				//        new CodeVariableReferenceExpression("groups"),
				//        new CodeBinaryOperatorExpression(
				//            new CodePropertyReferenceExpression(
				//                new CodeVariableReferenceExpression("groups"), "Length"),
				//            CodeBinaryOperatorType.Subtract,
				//            new CodePropertyReferenceExpression(
				//                new CodeVariableReferenceExpression("baseGroups"), "Length")
				//            ),
				//        new CodePropertyReferenceExpression(
				//            new CodeVariableReferenceExpression("baseGroups"), "Length")
				//        )
				//    );
			}
			else
			{				
				method.ImplementationTypes.Add(new CodeTypeReference(typeof (Worm.Entities.Meta.IDefferedLoading)));
			}

			//inlockStatemets.Add(
			//        new CodeAssignStatement(
			//            ,
			//            new CodeVariableReferenceExpression("groups")
			//            )
			//        );

			List<CodeStatement> statements = new List<CodeStatement>(inlockStatemets.Count);
			foreach (CodeStatement statemet in inlockStatemets)
			{
				statements.Add(statemet);
			}
			method.Statements.Add(
				OrmCodeDomGenerator.CodePatternDoubleCheckLock(
					new CodeFieldReferenceExpression(
						new CodeThisReferenceExpression(),
						lockObj.Name
						),
					condition,
					statements.ToArray()
					)
				);

			

			method.Statements.Add(
				new CodeMethodReturnStatement(
					new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName)
				)
			);

			Members.Add(method);
		}

		protected void OnPopulateBaseTypes(object sender, EventArgs e)
		{
			OnPupulateSchemaInterfaces();
			OnPopulateIDefferedLoadingInterface();
			OnPupulateBaseClass();
		}

		private void OnPupulateBaseClass()
		{
			if (EntityClass.Entity.BaseEntity != null)
				BaseTypes.Add(new CodeTypeReference(OrmCodeGenNameHelper.GetEntitySchemaDefClassQualifiedName(EntityClass.Entity.BaseEntity)));
		}

		private void OnPupulateSchemaInterfaces()
		{
			if (EntityClass.Entity.BaseEntity == null)
			{
				BaseTypes.Add(new CodeTypeReference(typeof (Worm.Entities.Meta.IOrmObjectSchema)));
				BaseTypes.Add(new CodeTypeReference(typeof (Worm.Entities.Meta.ISchemaInit)));
			}
		}

		private void OnPopulateIDefferedLoadingInterface()
		{
			if (m_entityClass == null || m_entityClass.Entity == null || !m_entityClass.Entity.HasDefferedLoadableProperties || (m_entityClass.Entity.BaseEntity != null && m_entityClass.Entity.BaseEntity.CompleteEntity.HasDefferedLoadableProperties))
				return;

			BaseTypes.Add(new CodeTypeReference(typeof (Worm.Entities.Meta.IDefferedLoading)));
		}

		public CodeSchemaDefTypeDeclaration(CodeEntityTypeDeclaration entityClass) : this()
		{
			EntityClass = entityClass;
		}

		public CodeEntityTypeDeclaration EntityClass
		{
			get
			{
				return m_entityClass;
			}
			set
			{
				m_entityClass = value;
				RenewEntityClass();
			}
		}

		public CodeTypeReference TypeReference
		{
			get { return m_typeReference; }
		}

		public new string Name
		{
			get
			{
				if (m_entityClass != null && m_entityClass.Entity != null)
					return OrmCodeGenNameHelper.GetEntitySchemaDefClassName(m_entityClass.Entity);
				return null;
			}
		}

		public string FullName
		{
			get
			{
				if (m_entityClass != null && m_entityClass.Entity != null)
					return OrmCodeGenNameHelper.GetEntitySchemaDefClassQualifiedName(m_entityClass.Entity);
				return null;
			}
		}

		protected void RenewEntityClass()
		{
			base.Name = Name;
			m_typeReference.BaseType = FullName;
			
			IsPartial = m_entityClass.IsPartial;
			Attributes = m_entityClass.Attributes;
			if (m_entityClass.Entity.BaseEntity != null)
				Attributes |= MemberAttributes.New;
			
		}

	}
}
