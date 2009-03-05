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
								ReturnType = new CodeTypeReference(typeof(string[][]))
			             	};

			// string[][] result;
			method.Statements.Add(new CodeVariableDeclarationStatement(method.ReturnType, "result"));

			var defferedLoadPropertiesGrouped = m_entityClass.Entity.GetDefferedLoadProperties();

			var field = new CodeMemberField(method.ReturnType, OrmCodeGenNameHelper.GetPrivateMemberName(method.Name));
			Members.Add(field);
			var lockObj = new CodeMemberField(new CodeTypeReference(typeof (object)),
			                                  OrmCodeGenNameHelper.GetPrivateMemberName(method.Name));
			lockObj.InitExpression = new CodeObjectCreateExpression(lockObj.Type);
			Members.Add(lockObj);

			CodeExpression condition = new CodeBinaryOperatorExpression(
					new CodeFieldReferenceExpression(
						new CodeThisReferenceExpression(),
						field.Name
						),
					CodeBinaryOperatorType.IdentityEquality,
					new CodePrimitiveExpression(null)
					);

			CodeStatementCollection inlockStatemets = new CodeStatementCollection();

			var array = new CodeArrayCreateExpression(new CodeTypeReference(typeof(string[])));

			foreach (var propertyDescriptions in defferedLoadPropertiesGrouped)
			{
				var innerArray = new CodeArrayCreateExpression(new CodeTypeReference(typeof(string)));
				array.Initializers.Add(innerArray);
				foreach (var propertyDescription in propertyDescriptions)
				{
					innerArray.Initializers.Add(OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDescription));
				}
			}

			inlockStatemets.Add(new CodeVariableDeclarationStatement(
										method.ReturnType,
										"groups",
										array
										));

			if (m_entityClass.Entity.BaseEntity != null && m_entityClass.Entity.BaseEntity.CompleteEntity.HasDefferedLoadableProperties)
			{
				method.Attributes |= MemberAttributes.Override;

				// string[][] baseArray;
				var tempVar = new CodeVariableDeclarationStatement(method.ReturnType, "baseGroups");

				inlockStatemets.Add(tempVar);
				// baseArray = base.GetDefferedLoadPropertiesGroups()
				inlockStatemets.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("baseGroups"),
				                                              new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(),
				                                                                             method.Name)));

				// Array.Resize<string[]>(ref groups, baseGroups.Length, groups.Length)
				inlockStatemets.Add(
					new CodeMethodInvokeExpression(
						new CodeMethodReferenceExpression(
							new CodeTypeReferenceExpression(new CodeTypeReference(typeof(Array))),
							"Resize",
							new CodeTypeReference(typeof(string[]))),
						new CodeDirectionExpression(FieldDirection.Ref,
													new CodeVariableReferenceExpression("groups")),
						new CodeBinaryOperatorExpression(
							new CodePropertyReferenceExpression(
								new CodeVariableReferenceExpression("baseGroups"),
								"Length"
								),
							CodeBinaryOperatorType.Add,
							new CodePropertyReferenceExpression(
								new CodeVariableReferenceExpression("groups"),
								"Length"
								)
							)
						)
					);
				// Array.Copy(baseGroups, 0, groups, groups.Length - baseGroups.Length, baseGroups.Length)
				inlockStatemets.Add(
					new CodeMethodInvokeExpression(
						new CodeTypeReferenceExpression(typeof(Array)),
						"Copy",
						new CodeVariableReferenceExpression("baseGroups"),
						new CodePrimitiveExpression(0),
						new CodeVariableReferenceExpression("groups"),
						new CodeBinaryOperatorExpression(
							new CodePropertyReferenceExpression(
								new CodeVariableReferenceExpression("groups"), "Length"),
							CodeBinaryOperatorType.Subtract,
							new CodePropertyReferenceExpression(
								new CodeVariableReferenceExpression("baseGroups"), "Length")
							),
						new CodePropertyReferenceExpression(
							new CodeVariableReferenceExpression("baseGroups"), "Length")
						)
					);
			}
			else
			{				
				method.ImplementationTypes.Add(new CodeTypeReference(typeof (Worm.Entities.Meta.IDefferedLoading)));
			}

			inlockStatemets.Add(
					new CodeAssignStatement(
						new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), field.Name),
						new CodeVariableReferenceExpression("groups")
						)
					);

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
					new CodeFieldReferenceExpression(
						new CodeThisReferenceExpression(),
						field.Name
						)
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
			if (m_entityClass == null || m_entityClass.Entity == null || !m_entityClass.Entity.HasDefferedLoadableProperties || m_entityClass.Entity.BaseEntity.CompleteEntity.HasDefferedLoadableProperties)
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
