using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text;
using OrmCodeGenLib.Descriptors;
using Worm.Orm;
using Worm.Collections;
using Worm.Orm.Meta;
using Worm.Cache;

namespace OrmCodeGenLib
{
    public class OrmCodeDomGenerator
    {
        

        private readonly OrmObjectsDef _ormObjectsDefinition;

        public OrmCodeDomGenerator(OrmObjectsDef ormObjectsDefinition)
        {
            _ormObjectsDefinition = ormObjectsDefinition;
        }

        public Dictionary<string, CodeCompileUnit> GetFullDom(OrmCodeDomGeneratorSettings settings)
        {
            Dictionary<string, CodeCompileUnit> result = new Dictionary<string, CodeCompileUnit>(_ormObjectsDefinition.Entities.Count * (settings.Split?2:1));
            foreach (EntityDescription entity in _ormObjectsDefinition.Entities)
            {
                foreach (KeyValuePair<string, CodeCompileUnit> pair in GetEntityDom(entity.Identifier, settings))
                {
                    string key = pair.Key;
                    for (int i = 0; result.ContainsKey(key); i++)
                    {
                        key = pair.Key + i;
                    }

                    result.Add(key, pair.Value);
                }
            }
            return result;
        }

        public Dictionary<string, CodeCompileUnit> GetEntityDom(string entityId, OrmCodeDomGeneratorSettings settings)
        {
            using (new SettingsManager(settings, null))
            {
                Dictionary<string, CodeCompileUnit> result = new Dictionary<string, CodeCompileUnit>();
                if (String.IsNullOrEmpty(entityId))
                    throw new ArgumentNullException("entityId");

                EntityDescription entity;
                entity = _ormObjectsDefinition.GetEntity(entityId);


                if (entity == null)
                    throw new ArgumentException("entityId", string.Format("Entity with id '{0}' not found.", entityId));

                CodeCompileUnit entityUnit;
                CodeNamespace nameSpace;
                CodeTypeDeclaration entityClass, entitySchemaDefClass;
                CodeTypeDeclaration propertiesClass;

                CodeConstructor ctr;
                CodeMemberMethod method;
                CodeMemberField field;

                #region ����������� ������ ��������
                entityUnit = new CodeCompileUnit();
                result.Add(OrmCodeGenNameHelper.GetEntityFileName(entity), entityUnit);

                // ���������
                nameSpace = new CodeNamespace(entity.Namespace);
                entityUnit.Namespaces.Add(nameSpace);

                // �������
                //nameSpace.Imports.Add(new CodeNamespaceImport("System"));
                //nameSpace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
                //nameSpace.Imports.Add(new CodeNamespaceImport("Worm.Orm"));

                // ����� ��������
                entityClass = new CodeTypeDeclaration(OrmCodeGenNameHelper.GetEntityClassName(entity));
                nameSpace.Types.Add(entityClass);

                // ��������� ������
                entityClass.IsClass = true;
                entityClass.IsPartial = entity.Behaviour == EntityBehaviuor.PartialObjects || entity.Behaviour == EntityBehaviuor.ForcePartial || SettingsManager.CurrentManager.OrmCodeDomGeneratorSettings.Split;
                entityClass.Attributes = MemberAttributes.Public;
                entityClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.Public;

                //if (entity.Behaviour == EntityBehaviuor.Abstract)
                //{
                //    entityClass.Attributes |= MemberAttributes.Abstract;
                //    entityClass.TypeAttributes |= TypeAttributes.Abstract;
                //}

                // ���������
                SetMemberDescription(entityClass, entity.Description);

                // ������� �����
                if (entity.BaseEntity == null)
                {
                    //entityClass.BaseTypes.Add(new CodeTypeReference(typeof(OrmBaseT)));
                    CodeTypeReference entityType = new CodeTypeReference(typeof(OrmBaseT<>));
                    entityType.TypeArguments.Add(
                        new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity)));
                    entityClass.BaseTypes.Add(entityType);
                }

                else
                    entityClass.BaseTypes.Add(new CodeTypeReference(OrmCodeGenNameHelper.GetQualifiedEntityName(entity.BaseEntity)));

                CodeTypeReference iOrmEditableType = new CodeTypeReference(typeof(IOrmEditable<>));
                iOrmEditableType.TypeArguments.Add(
                    new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity)));

                entityClass.BaseTypes.Add(iOrmEditableType);

                #endregion ����������� ������ ��������

                #region ����������� �����
                entitySchemaDefClass = new CodeTypeDeclaration(OrmCodeGenNameHelper.GetEntitySchemaDefClassName(entity));

                entitySchemaDefClass.IsClass = true;
                entitySchemaDefClass.IsPartial = entityClass.IsPartial;
                entitySchemaDefClass.Attributes = entityClass.Attributes;
                entitySchemaDefClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.NestedPublic;

                #endregion ����������� �����

                #region ����������� ������ Properties

                {
                    propertiesClass = new CodeTypeDeclaration("Properties");
                    propertiesClass.Attributes = MemberAttributes.Public;
                    propertiesClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.NestedPublic;
                    CodeConstructor propctr = new CodeConstructor();
                    propctr.Attributes = MemberAttributes.Family;
                    propertiesClass.Members.Add(propctr);
                    propertiesClass.Comments.Add(new CodeCommentStatement("Entity properties's aliases", true));

                    entityClass.Members.Add(propertiesClass);
                }

                #endregion ����������� ������ Properties

                {
                    CodeTypeDeclaration descriptorClass = new CodeTypeDeclaration("Descriptor");
                    descriptorClass.Attributes = MemberAttributes.Public;
                    descriptorClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.NestedPublic;
                    CodeConstructor descConstr = new CodeConstructor();
                    descriptorClass.Attributes = MemberAttributes.Private;
                    descriptorClass.Members.Add(descConstr);

                    descriptorClass.Comments.Add(new CodeCommentStatement("Entity's  descriptor", true));

                    CodeMemberField constField = new CodeMemberField(typeof(string), "EntityName");
                    constField.Attributes = MemberAttributes.Const | MemberAttributes.Public;
                    constField.InitExpression = new CodePrimitiveExpression(entity.Name);
                    descriptorClass.Members.Add(constField);

                    entityClass.Members.Add(descriptorClass);
                }

                #region custom attribute EntityAttribute
                //if (settings.Behaviour != OrmObjectGeneratorBehaviour.BaseObjects)
                //{
                entityClass.CustomAttributes = new CodeAttributeDeclarationCollection(
                    new CodeAttributeDeclaration[]
                        {
                            new CodeAttributeDeclaration(
                                new CodeTypeReference(typeof (EntityAttribute)),
                                new CodeAttributeArgument(
                                    new CodeTypeOfExpression(
                                        new CodeTypeReference(OrmCodeGenNameHelper.GetEntitySchemaDefClassQualifiedName(entity))
                                        )
                                    ),
                                new CodeAttributeArgument(
                                    new CodePrimitiveExpression(_ormObjectsDefinition.SchemaVersion)
                                    ),
                                new CodeAttributeArgument(
                                    "EntityName",
                                    //new CodePrimitiveExpression(entity.Name)
                                    OrmCodeGenHelper.GetEntityNameReferenceExpression(entity)
                                    )
                                ),
                            new CodeAttributeDeclaration(
                                new CodeTypeReference(typeof(SerializableAttribute))
                            )
                        }
                    );
                //}

                #endregion custom attribute EntityAttribute

                #region ������������
                // ����������� �� ���������
                ctr = new CodeConstructor();
                ctr.Attributes = MemberAttributes.Public;
                entityClass.Members.Add(ctr);

                // ������������������� �����������
                ctr = new CodeConstructor();
                ctr.Attributes = MemberAttributes.Public;
                // ��������� ������������
                ctr.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "id"));
                ctr.Parameters.Add(new CodeParameterDeclarationExpression(typeof(OrmCacheBase), "cache"));
                ctr.Parameters.Add(new CodeParameterDeclarationExpression(typeof(Worm.OrmSchemaBase), "schema"));
                // �������� ���������� �������� ������������
                ctr.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("id"));
                ctr.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("cache"));
                ctr.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("schema"));
                entityClass.Members.Add(ctr);
                #endregion ������������

                #region ����� OrmBase.CopyBody(CopyBody(...)
                CodeMemberMethod copyMethod;
                copyMethod = new CodeMemberMethod();
                entityClass.Members.Add(copyMethod);
                copyMethod.Name = "CopyBody";
                // ��� ������������� ��������
                copyMethod.ReturnType = null;
                // ������������ �������
                copyMethod.Attributes = MemberAttributes.Public;
                //if (entity.BaseEntity != null)
                //    copyMethod.Attributes |= MemberAttributes.Override;
                copyMethod.ImplementationTypes.Add(iOrmEditableType);
                copyMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity)), "from"));
                copyMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity)), "to"));
                if (entity.BaseEntity != null)
                    copyMethod.Statements.Add(
                        new CodeMethodInvokeExpression(
                            new CodeBaseReferenceExpression(),
                            "CopyBody",
                            new CodeArgumentReferenceExpression("from"),
                            new CodeArgumentReferenceExpression("to")
                        )
                    );
                #endregion ����� OrmBase.CopyBody(CopyBody(OrmBase from, OrmBase to)

                #region // ����� IComparer<T> OrmBase.CreateSortComparer<T>(string sort, SortType sortType)
                //if (settings.Behaviour != OrmObjectGeneratorBehaviour.PartialObjects && entity.BaseEntity == null)
                //{
                //    method = new CodeMemberMethod();
                //    entityClass.Members.Add(method);
                //    method.Name = "CreateSortComparer";
                //    // generic ��������
                //    CodeTypeParameter prm = new CodeTypeParameter("T");
                //    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.DerivedGenericMembersRequireConstraits) == LanguageSpecificHacks.DerivedGenericMembersRequireConstraits)
                //    {
                //        prm.Constraints.Add(new CodeTypeReference(typeof (OrmBase)));
                //        prm.HasConstructorConstraint = true;
                //    }
                //    method.TypeParameters.Add(prm);

                //    // ��� ������������� ��������
                //    CodeTypeReference methodReturnType;
                //    methodReturnType = new CodeTypeReference();
                //    methodReturnType.BaseType = "System.Collections.Generic.IComparer";
                //    methodReturnType.Options = CodeTypeReferenceOptions.GenericTypeParameter;
                //    methodReturnType.TypeArguments.Add("T");
                //    method.ReturnType = methodReturnType;
                //    // ������������ �������
                //    method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
                //    // ��������� ����� �������� ������

                //    // ���������
                //    method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sort"));
                //    method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(SortType), "sortType"));
                //    method.Statements.Add(
                //        new CodeThrowExceptionStatement(
                //            new CodeObjectCreateExpression(
                //                typeof(NotImplementedException),
                //                new CodePrimitiveExpression("The method or operation is not implemented.")
                //            )
                //        )
                //    );
                //}
                #endregion // ����� IComparer<T> OrmBase.CreateSortComparer<T>(string sort, SortType sortType)

                #region // ����� IComparer OrmBase.CreateSortComparer(string sort, SortType sortType)
                //if (settings.Behaviour != OrmObjectGeneratorBehaviour.PartialObjects &&  entity.BaseEntity == null)
                //{
                //    method = new CodeMemberMethod();
                //    entityClass.Members.Add(method);
                //    method.Name = "CreateSortComparer";
                //    // ��� ������������� ��������
                //    method.ReturnType = new CodeTypeReference(typeof(IComparer));
                //    // ������������ �������
                //    method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
                //    // ��������� ����� �������� ������

                //    // ���������
                //    method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sort"));
                //    method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(SortType), "sortType"));
                //    method.Statements.Add(
                //        new CodeThrowExceptionStatement(
                //            new CodeObjectCreateExpression(
                //                typeof(NotImplementedException),
                //                new CodePrimitiveExpression("The method or operation is not implemented.")
                //            )
                //        )
                //    ); 
                //}
                #endregion // ����� IComparer CreateSortComparer(string sort, SortType sortType)

                #region void SetValue(System.Reflection.PropertyInfo pi, ColumnAttribute c, object value)

                CodeMemberMethod setvalueMethod = CreateSetValueMethod(entityClass);

                #endregion void SetValue(System.Reflection.PropertyInfo pi, ColumnAttribute c, object value)

                #region public override object GetValue(string propAlias, Worm.Orm.IOrmObjectSchemaBase schema)

                CodeMemberMethod getvalueMethod = CreateGetValueMethod(entityClass);

                #endregion public override object GetValue(string propAlias, Worm.Orm.IOrmObjectSchemaBase schema)

                #region void CreateObject(string fieldName, object value)

                CodeMemberMethod createobjectMethod = CreateCreateObjectMethod(entity, entityClass);

                #endregion void CreateObject(string fieldName, object value)

                #region // ����� OrmBase GetNew()
                ////if (settings.Behaviour != OrmObjectGeneratorBehaviour.BaseObjects)
                ////{
                //    method = new CodeMemberMethod();
                //    entityClass.Members.Add(method);
                //    method.Name = "GetNew";
                //    // ��� ������������� ��������
                //    method.ReturnType = new CodeTypeReference(typeof (OrmBase));
                //    // ������������ �������
                //    method.Attributes = MemberAttributes.Family | MemberAttributes.Override;
                //    // ��������� ����� �������� ������

                //    // ���������
                //    //if (settings.Behaviour != OrmObjectGeneratorBehaviour.BaseObjects)
                //    //{
                //        method.Statements.Add(
                //            new CodeMethodReturnStatement(
                //                new CodeObjectCreateExpression(
                //                    new CodeTypeReference(
                //                           OrmCodeGenNameHelper.GetQualifiedEntityName(entity, settings, true)
                //                        ),
                //                    new CodePropertyReferenceExpression(
                //                        new CodeThisReferenceExpression(),
                //                        "Identifier"
                //                        ),
                //                    new CodePropertyReferenceExpression(
                //                        new CodeThisReferenceExpression(),
                //                        "OrmCache"
                //                        ),
                //                    new CodePropertyReferenceExpression(
                //                        new CodeThisReferenceExpression(),
                //                        "OrmSchema"
                //                        )
                //                    )
                //                )
                //            );
                //    //}
                ////}

                #endregion // ����� OrmBase GetNew()

                #region ��������

                CreateProperties(createobjectMethod, entity, entityClass, setvalueMethod, getvalueMethod, copyMethod, propertiesClass);

                #endregion ��������

                #region void SetValue(System.Reflection.PropertyInfo pi, ColumnAttribute c, object value)
                setvalueMethod.Statements.Add(
                    new CodeMethodInvokeExpression(
                        new CodeMethodReferenceExpression(
                            new CodeBaseReferenceExpression(),
                            "SetValue"
                        ),
                        new CodeArgumentReferenceExpression("pi"),
                        new CodeArgumentReferenceExpression("c"),
                        new CodeArgumentReferenceExpression("value")
                    )
                );
                #endregion void SetValue(System.Reflection.PropertyInfo pi, ColumnAttribute c, object value)

				#region TEntity Get<TEntity>(int id)

				//CreateGetEntityMethod(entity, entityClass);

				#endregion

                #region ��������� ��������� Split

                ProcessSplitOption(entity, entityClass, ref entitySchemaDefClass, result);

                #endregion ��������� ��������� Split



                #region ���� ��������

                CreateTablesLinkEnum(entity, entitySchemaDefClass);

                #endregion ���� ��������

                #region ����� public OrmTable GetTypeMainTable(Type type)

                CreateGetTypeMainTableMethod(entity, entitySchemaDefClass);

                #endregion ����� public static OrmTable GetMainTable()

                #region ���� _idx
                field = new CodeMemberField(new CodeTypeReference(typeof(IndexedCollection<string, MapField2Column>)), "_idx");
                entitySchemaDefClass.Members.Add(field);
                #endregion ���� _idx

                #region ���� _tables
                field = new CodeMemberField(new CodeTypeReference(typeof(OrmTable[])), "_tables");
                field.Attributes = MemberAttributes.Private;
                entitySchemaDefClass.Members.Add(field);
                #endregion ���� _tables

                #region ����� OrmTable[] GetTables()

                CreateGetTablesMethod(entity, entitySchemaDefClass);

                #endregion ����� OrmTable[] GetTables()

                #region ����� OrmTable GetTable(...)

                CreateGetTableMethod(entity, entitySchemaDefClass);

                #endregion ����� OrmTable GetTable(...)


                #region bool ChangeValueType(ColumnAttribute c, object value, ref object newvalue)

                CreateChangeValueTypeMethod(entity, entitySchemaDefClass);

                #endregion bool ChangeValueType(ColumnAttribute c, object value, ref object newvalue)

                #region OrmJoin GetJoins(OrmTable left, OrmTable right)
                if ((entity.Behaviour != EntityBehaviuor.PartialObjects || entity.Tables.Count == 1) && entity.BaseEntity == null)
                {
                    method = new CodeMemberMethod();
                    entitySchemaDefClass.Members.Add(method);
                    method.Name = "GetJoins";
                    // ��� ������������� ��������
                    method.ReturnType = new CodeTypeReference(typeof(Worm.Criteria.Joins.OrmJoin));
                    // ������������ �������
                    method.Attributes = MemberAttributes.Public;
                    // ��������� ����� �������� ������
                    method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
                    // ���������
                    method.Parameters.Add(
                        new CodeParameterDeclarationExpression(
                            new CodeTypeReference(typeof(OrmTable)),
                            "left"
                        )
                    );
                    method.Parameters.Add(
                        new CodeParameterDeclarationExpression(
                            new CodeTypeReference(typeof(OrmTable)),
                            "right"
                        )
                    );
                    if (entity.Tables.Count > 1)
                    {
                        if (entity.Behaviour != EntityBehaviuor.PartialObjects)
                        {
                            method.Statements.Add(
                                new CodeThrowExceptionStatement(
                                    new CodeObjectCreateExpression(
                                        typeof(NotImplementedException),
                                        new CodePrimitiveExpression(
                                            "Entity has more then one table: this method must be implemented.")
                                        )
                                    )
                                );
                        }

                    }
                    else
                    {
                        method.Statements.Add(
                            new CodeMethodReturnStatement(
                                new CodeDefaultValueExpression(new CodeTypeReference(typeof(Worm.Database.Criteria.Joins.OrmJoin)))
                            )
                        );
                    }
                }
                #endregion OrmJoin GetJoins(string left, string right)

                #region ColumnAttribute[] GetSuppressedColumns()
                if (entity.Behaviour != EntityBehaviuor.PartialObjects && entity.BaseEntity == null)
                {
                    method = new CodeMemberMethod();
                    entitySchemaDefClass.Members.Add(method);
                    method.Name = "GetSuppressedColumns";
                    // ��� ������������� ��������
                    method.ReturnType = new CodeTypeReference(typeof(ColumnAttribute[]));
                    // ������������ �������
                    method.Attributes = MemberAttributes.Public;
                    // ��������� ����� �������� ������
                    method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
                    CodeArrayCreateExpression arrayExpression = new CodeArrayCreateExpression(
                        new CodeTypeReference(typeof(ColumnAttribute[]))
                        );
                    foreach (PropertyDescription suppressedProperty in entity.SuppressedProperties)
                    {
                        arrayExpression.Initializers.Add(
                            new CodeObjectCreateExpression(typeof(ColumnAttribute),
                                                           new CodePrimitiveExpression(suppressedProperty.PropertyAlias)));
                    }
                    method.Statements.Add(new CodeMethodReturnStatement(arrayExpression));
                }
                #endregion ColumnAttribute[] GetSuppressedColumns()

                #region IOrmFilter GetFilter(object filter_info)
                if (entity.Behaviour != EntityBehaviuor.PartialObjects && entity.BaseEntity == null)
                {
                    method = new CodeMemberMethod();
                    entitySchemaDefClass.Members.Add(method);
                    method.Name = "GetFilter";
                    // ��� ������������� ��������
                    method.ReturnType = new CodeTypeReference(typeof(Worm.Criteria.Core.IFilter));
                    // ������������ �������
                    method.Attributes = MemberAttributes.Public;
                    method.Parameters.Add(
                        new CodeParameterDeclarationExpression(
                            new CodeTypeReference(typeof(object)),
                            "filter_info"
                        )
                    );
                    // ��������� ����� �������� ������
                    method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
                    method.Statements.Add(
                        new CodeMethodReturnStatement(
                            new CodePrimitiveExpression(null)
                        )
                    );
                }

                #endregion IOrmFilter GetFilter(object filter_info)

                #region �������� ����� ����� "������ �� ������"

                List<RelationDescription> usedM2MRelation;
                // ������ ���������� ����������� � ������ ��������
                usedM2MRelation = entity.GetRelations(false);

            	List<SelfRelationDescription> usedM2MSelfRelation;
				usedM2MSelfRelation = entity.GetSelfRelations(false);

                #region ���� _m2mRelations
                field = new CodeMemberField(new CodeTypeReference(typeof(M2MRelation[])), "_m2mRelations");
                entitySchemaDefClass.Members.Add(field);
                #endregion ���� _m2mRelations
                #region ����� M2MRelation[] GetM2MRelations()
                method = new CodeMemberMethod();
                entitySchemaDefClass.Members.Add(method);
                method.Name = "GetM2MRelations";
                // ��� ������������� ��������
                method.ReturnType = new CodeTypeReference(typeof(M2MRelation[]));
                // ������������ �������
                method.Attributes = MemberAttributes.Public;
                if (entity.BaseEntity != null)
                {
                    method.Attributes |= MemberAttributes.Override;
                }
                else
                    // ��������� ����� �������� ������
                    method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
                // ���������
                //...
                // ��� ����
                CodeMemberField forM2MRelationsLockField = new CodeMemberField(
                    new CodeTypeReference(typeof(object)),
                    "_forM2MRelationsLock"
                    );
                forM2MRelationsLockField.InitExpression = new CodeObjectCreateExpression(forM2MRelationsLockField.Type);
                entitySchemaDefClass.Members.Add(forM2MRelationsLockField);
                // ����
                CodeExpression condition = new CodeBinaryOperatorExpression(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        "_m2mRelations"
                        ),
                    CodeBinaryOperatorType.IdentityEquality,
                    new CodePrimitiveExpression(null)
                    );
                CodeStatementCollection inlockStatemets = new CodeStatementCollection();
                CodeArrayCreateExpression m2mArrayCreationExpression = new CodeArrayCreateExpression(
                    new CodeTypeReference(typeof(M2MRelation[]))
                    );
                foreach (RelationDescription relationDescription in usedM2MRelation)
                {
                    m2mArrayCreationExpression.Initializers.AddRange(
                        GetM2MRelationCreationExpressions(relationDescription, entity));
                }
            	foreach (SelfRelationDescription selfRelationDescription in usedM2MSelfRelation)
            	{
					m2mArrayCreationExpression.Initializers.AddRange(
						GetM2MRelationCreationExpressions(selfRelationDescription, entity));
            	}
                inlockStatemets.Add(new CodeVariableDeclarationStatement(
                                        method.ReturnType,
                                        "m2mRelations",
                                        m2mArrayCreationExpression
                                        ));
                if (entity.BaseEntity != null)
                {
                    // M2MRelation[] basem2mRelations = base.GetM2MRelations()
                    inlockStatemets.Add(
                        new CodeVariableDeclarationStatement(
                            new CodeTypeReference(typeof(M2MRelation[])),
                            "basem2mRelations",
                            new CodeMethodInvokeExpression(
                                new CodeBaseReferenceExpression(),
                                "GetM2MRelations"
                            )
                        )
                    );
                    // Array.Resize<M2MRelation>(ref m2mRelation, basem2mRelation.Length, m2mRelation.Length)
                    inlockStatemets.Add(
                        new CodeMethodInvokeExpression(
                            new CodeMethodReferenceExpression(
                                new CodeTypeReferenceExpression(new CodeTypeReference(typeof(Array))),
                                "Resize",
                                new CodeTypeReference(typeof(M2MRelation))),
                            new CodeDirectionExpression(FieldDirection.Ref, new CodeVariableReferenceExpression("m2mRelations")),
                            new CodeBinaryOperatorExpression(
                                new CodePropertyReferenceExpression(
                                    new CodeVariableReferenceExpression("basem2mRelations"),
                                    "Length"
                                ),
                                CodeBinaryOperatorType.Add,
                                new CodePropertyReferenceExpression(
                                    new CodeVariableReferenceExpression("m2mRelations"),
                                    "Length"
                                )
                            )
                        )
                    );
                    // Array.Copy(basem2mRelation, 0, m2mRelations, m2mRelations.Length - basem2mRelation.Length, basem2mRelation.Length)
                    inlockStatemets.Add(
                            new CodeMethodInvokeExpression(
                                new CodeTypeReferenceExpression(typeof(Array)),
                                "Copy",
                                new CodeVariableReferenceExpression("basem2mRelations"),
                                new CodePrimitiveExpression(0),
                                new CodeVariableReferenceExpression("m2mRelations"),
                                new CodeBinaryOperatorExpression(
                                    new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("m2mRelations"), "Length"),
                                    CodeBinaryOperatorType.Subtract,
                                    new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("basem2mRelations"), "Length")
                                ),
                                new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("basem2mRelations"), "Length")
                            )
                    );
                }
                inlockStatemets.Add(
                    new CodeAssignStatement(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_m2mRelations"),
                        new CodeVariableReferenceExpression("m2mRelations")
                    )
                    );
                List<CodeStatement> statements = new List<CodeStatement>(inlockStatemets.Count);
                foreach (CodeStatement statemet in inlockStatemets)
                {
                    statements.Add(statemet);
                }
                method.Statements.Add(
                    CodePatternDoubleCheckLock(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(),
                            "_forM2MRelationsLock"
                        ),
                        condition,
                        statements.ToArray()
                    )
                );
                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(
                                new CodeThisReferenceExpression(),
                                "_m2mRelations"
                        )
                    )
                );
                #endregion ����� string[] GetTables()

                #region ����� ��������� ��������� ���������

                //usedM2MRelation.FindAll(delegate(RelationDescription match) { return match.Left.Entity != match.Right.Entity; }).ForEach(
                //    delegate(RelationDescription action)
                //    {
                //        EntityDescription selfEntity = action.Left.Entity == entity ? action.Left.Entity : action.Right.Entity;
                //        EntityDescription relatedEntity = action.Right.Entity == entity ? action.Left.Entity : action.Right.Entity;
                //        CodeMemberMethod relationMethod;
                //        relationMethod = new CodeMemberMethod();
                //        relationMethod.Attributes = MemberAttributes.Final | MemberAttributes.Public;
                //        relationMethod.Name = "Get" + OrmCodeGenNameHelper.GetMultipleForm(relatedEntity.Name);
                //        CodeTypeReference relationMethodReturnType = new CodeTypeReference(typeof (ICollection<>));
                //        relationMethodReturnType.TypeArguments.Add(
                //            new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(relatedEntity, settings)));
                //        relationMethod.ReturnType = relationMethodReturnType;
                //        entityClass.Members.Add(relationMethod);
                //    }
                //    );
                #endregion ����� ��������� ��������� ���������

                #endregion �������� ����� ����� "������ �� ������"

                #region Worm.Orm.Collections.IndexedCollection<string, Worm.Orm.MapField2Column> GetFieldColumnMap()
                method = new CodeMemberMethod();
                entitySchemaDefClass.Members.Add(method);
                method.Name = "GetFieldColumnMap";
                // ��� ������������� ��������
                method.ReturnType = new CodeTypeReference(typeof(IndexedCollection<string, MapField2Column>));
                // ������������ �������
                method.Attributes = MemberAttributes.Public;
                if (entity.BaseEntity != null)
                {
                    method.Attributes |= MemberAttributes.Override;
                }
                else
                    // ��������� ����� �������� ������
                    method.ImplementationTypes.Add(new CodeTypeReference(typeof(IOrmObjectSchema)));
                // ���������
                //...
                // ��� ����
                CodeMemberField forIdxLockField = new CodeMemberField(
                    new CodeTypeReference(typeof(object)),
                    "_forIdxLock"
                );
                forIdxLockField.InitExpression = new CodeObjectCreateExpression(forIdxLockField.Type);
                entitySchemaDefClass.Members.Add(forIdxLockField);
                List<CodeStatement> condTrueStatements = new List<CodeStatement>();
                condTrueStatements.Add(
                    new CodeVariableDeclarationStatement(
                        new CodeTypeReference(typeof(IndexedCollection<string, MapField2Column>)),
                        "idx",
                        (entity.BaseEntity == null) ?
                        (CodeExpression)new CodeObjectCreateExpression(
                                             new CodeTypeReference(typeof(OrmObjectIndex))
                                             )
                        :
                        new CodeMethodInvokeExpression(
                            new CodeBaseReferenceExpression(),
                            "GetFieldColumnMap"
                            )
                    )
                );
                condTrueStatements.AddRange(
                    entity.Properties.ConvertAll<CodeStatement>(delegate(PropertyDescription action)
                                                                {
                                                                    return new CodeExpressionStatement(
                                                                        new CodeMethodInvokeExpression(
                                                                            new CodeVariableReferenceExpression("idx"),
                                                                            "Add",
                                                                            GetMapField2ColumObjectCreationExpression(entity, action)
                                                                        //new CodeArrayIndexerExpression(
                                                                        //    new CodeFieldReferenceExpression(
                                                                        //        new CodeTypeReferenceExpression(
                                                                        //            new CodeTypeReference(GetEntitySchemaDefClassQualifiedName(entity, settings))
                                                                        //        ),
                                                                        //        "Tables"
                                                                        //    ),
                                                                        //    //new CodeMethodInvokeExpression(
                                                                        //    //    new CodeThisReferenceExpression(),
                                                                        //    //    "GetTables")
                                                                        //    //,
                                                                        //    new CodeCastExpression(
                                                                        //        new CodeTypeReference(typeof (int)),
                                                                        //        new CodeFieldReferenceExpression(
                                                                        //            new CodeTypeReferenceExpression(GetEntitySchemaDefClassQualifiedName
                                                                        //                                                (entity
                                                                        //                                                 ,
                                                                        //                                                 settings) +
                                                                        //                                            ".TablesLink"),
                                                                        //            action.Table.Identifier
                                                                        //            ))
                                                                        //    )

                                                                                //new CodePrimitiveExpression(action.Table.Name)
                                                                        //    )
                                                                            )
                                                                        );
                                                                }
                        )
                );

                //method.Statements.Add(
                //    new CodeConditionStatement(
                //        new CodeBinaryOperatorExpression(
                //            new CodeFieldReferenceExpression(
                //                new CodeThisReferenceExpression(),
                //                "_idx"
                //            ),
                //            CodeBinaryOperatorType.IdentityEquality,
                //            new CodePrimitiveExpression(null)
                //        ),
                //        condTrueStatements.ToArray(),
                //        new CodeStatement[] { }

                //    )
                //);
                condTrueStatements.Add(
                    new CodeAssignStatement(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(),
                            "_idx"
                        ),
                        new CodeVariableReferenceExpression("idx")
                    )
                );
                method.Statements.Add(
					CodePatternDoubleCheckLock(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(),
                            "_forIdxLock"
                        ),
                        new CodeBinaryOperatorExpression(
                            new CodeFieldReferenceExpression(
                                new CodeThisReferenceExpression(),
                                "_idx"
                            ),
                            CodeBinaryOperatorType.IdentityEquality,
                            new CodePrimitiveExpression(null)
                        ),
                        condTrueStatements.ToArray()
                    )
                );
                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(
                                new CodeThisReferenceExpression(),
                                "_idx"
                        )
                    )
                );
                #endregion Worm.Orm.Collections.IndexedCollection<string, Worm.Orm.MapField2Column> GetFieldColumnMap()

                #region �������� ��������� �����
                RelationDescriptionBase relation;
                relation = _ormObjectsDefinition.Relations.Find(
                            delegate(RelationDescriptionBase match)
                            {
                                return match.UnderlyingEntity == entity && !match.Disabled;
                            }
                        );
                if (relation != null)
                {
                    SelfRelationDescription sd = relation as SelfRelationDescription;
                    if (sd == null)
                        ImplementIRelation((RelationDescription)relation, entity, entitySchemaDefClass);
                    else
                        ImplementIRelation(sd, entity, entitySchemaDefClass);
                }

                //SelfRelationDescription selfRelation;
                //selfRelation = _ormObjectsDefinition.SelfRelations.Find(
                //            delegate(SelfRelationDescription match)
                //            {
                //                return match.UnderlyingEntity == entity && !match.Disabled;
                //            }
                //        );
                //if (selfRelation != null)
                //{
                //    ImplementIRelation(selfRelation, entity, entitySchemaDefClass);
                //}
                #endregion �������� ��������� �����

                #region public void GetSchema(OrmSchemaBase schema, Type t)

                if (entity.BaseEntity == null)
                {
                    CodeMemberField schemaField = new CodeMemberField(
                        new CodeTypeReference(typeof(Worm.OrmSchemaBase)),
                        "_schema"
                        );
                    CodeMemberField typeField = new CodeMemberField(
                        new CodeTypeReference(typeof(Type)),
                        "_entityType"
                        );
                    schemaField.Attributes = MemberAttributes.Family;
                    entitySchemaDefClass.Members.Add(schemaField);
                    typeField.Attributes = MemberAttributes.Family;
                    entitySchemaDefClass.Members.Add(typeField);
                    method = new CodeMemberMethod();
                    entitySchemaDefClass.Members.Add(method);
                    method.Name = "GetSchema";
                    // ��� ������������� ��������
                    method.ReturnType = null;
                    // ������������ �������
                    method.Attributes = MemberAttributes.Public;
                    if (entity.BaseEntity != null)
                    {
                        method.Attributes |= MemberAttributes.Override;
                    }
                    method.Parameters.Add(
                        new CodeParameterDeclarationExpression(
                            new CodeTypeReference(typeof(Worm.OrmSchemaBase)),
                            "schema"
                            )
                        );
                    method.Parameters.Add(
                        new CodeParameterDeclarationExpression(
                            new CodeTypeReference(typeof(Type)),
                            "t"
                            )
                        );
                    // ��������� ����� �������� ������
                    method.ImplementationTypes.Add(typeof(IOrmSchemaInit));
                    method.Statements.Add(
                        new CodeAssignStatement(
                            new CodeFieldReferenceExpression(
                                new CodeThisReferenceExpression(),
                                "_schema"
                                ),
                            new CodeArgumentReferenceExpression("schema")
                            )
                        );
                    method.Statements.Add(
                        new CodeAssignStatement(
                            new CodeFieldReferenceExpression(
                                new CodeThisReferenceExpression(),
                                "_entityType"
                                ),
                            new CodeArgumentReferenceExpression("t")
                            )
                        );
                }
                //if (entity.BaseEntity != null)
                //    method.Statements.Add(
                //        new CodeMethodInvokeExpression(
                //            new CodeBaseReferenceExpression(),
                //            "GetSchema",
                //            new CodeArgumentReferenceExpression("schema"),
                //            new CodeArgumentReferenceExpression("t")
                //        )
                //    );
                #endregion public void GetSchema(OrmSchemaBase schema, Type t)

                if ((createobjectMethod.Statements.Count == 0 || entity.Behaviour == EntityBehaviuor.PartialObjects) && entityClass.Members.Contains(createobjectMethod))
                    entityClass.Members.Remove(createobjectMethod);
                if (setvalueMethod.Statements.Count <= 1)
                    entityClass.Members.Remove(setvalueMethod);



                foreach (CodeCompileUnit compileUnit in result.Values)
                {
                    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.AddOptionsExplicit) == LanguageSpecificHacks.AddOptionsExplicit)
                        compileUnit.UserData.Add("RequireVariableDeclaration", (settings.LanguageSpecificHacks & LanguageSpecificHacks.OptionsExplicitOn) == LanguageSpecificHacks.OptionsExplicitOn);
                    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.AddOptionsStrict) == LanguageSpecificHacks.AddOptionsStrict)
                        compileUnit.UserData.Add("AllowLateBound", (settings.LanguageSpecificHacks & LanguageSpecificHacks.OptionsStrictOn) != LanguageSpecificHacks.OptionsStrictOn);

                    if (compileUnit.Namespaces.Count > 0)
                    {
                        StringBuilder commentBuilder = new StringBuilder();
                        foreach (string comment in _ormObjectsDefinition.SystemComments)
                        {
                            commentBuilder.AppendLine(comment);
                        }

                        if (_ormObjectsDefinition.UserComments.Count > 0)
                        {
                            commentBuilder.AppendLine();
                            foreach (string comment in _ormObjectsDefinition.UserComments)
                            {
                                commentBuilder.AppendLine(comment);
                            }
                        }
                        compileUnit.Namespaces[0].Comments.Insert(0,
                                                                  new CodeCommentStatement(commentBuilder.ToString(), false));
                    }
                    foreach (CodeNamespace ns in compileUnit.Namespaces)
                    {
                        foreach (CodeTypeDeclaration type in ns.Types)
                        {
                            CodeGenHelper.SetRegions(type);
                        }
                    }
                }

                return result; 
            }
        }

    	private void ImplementIRelation(RelationDescription relation, EntityDescription entity, CodeTypeDeclaration entitySchemaDefClass)
    	{
    		CodeMemberMethod method;
    		entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(typeof(IRelation)));
    		#region Pair<string, Type> GetFirstType()
    		method = new CodeMemberMethod();
    		//method.StartDirectives.Add(Regions["IRelation Members"].Start);
    		entitySchemaDefClass.Members.Add(method);
    		method.Name = "GetFirstType";
    		// ��� ������������� ��������
    		method.ReturnType = new CodeTypeReference(typeof(IRelation.RelationDesc));
    		// ������������ �������
    		method.Attributes = MemberAttributes.Public;
    		// ��������� ����� �������� ������
    		method.ImplementationTypes.Add(typeof(IRelation));
    		method.Statements.Add(
    			new CodeMethodReturnStatement(
    				new CodeObjectCreateExpression(
    					new CodeTypeReference(typeof(IRelation.RelationDesc)),
    					OrmCodeGenHelper.GetFieldNameReferenceExpression(entity.Properties.Find(delegate(PropertyDescription match) { return match.FieldName == relation.Left.FieldName; })),
    					new CodeMethodInvokeExpression(
    						new CodeMethodReferenceExpression(
    							new CodeFieldReferenceExpression(
    								new CodeThisReferenceExpression(),
    								"_schema"
    								),
    							"GetTypeByEntityName"
    							),
    						new CodePrimitiveExpression(relation.Right.Entity.Name)
    						)
    					//new CodeTypeOfExpression(
    					//    new CodeTypeReference(GetEntityClassQualifiedName(relation.Right.Entity, settings))
    					//)
    					)
    				)
    			);
    		#endregion Pair<string, Type> GetFirstType()
    		#region Pair<string, Type> GetSecondType()
    		method = new CodeMemberMethod();
    		//method.EndDirectives.Add(Regions["IRelation Members"].End);
    		entitySchemaDefClass.Members.Add(method);
    		method.Name = "GetSecondType";
    		// ��� ������������� ��������
    		method.ReturnType = new CodeTypeReference(typeof(IRelation.RelationDesc));
    		// ������������ �������
    		method.Attributes = MemberAttributes.Public;
    		// ��������� ����� �������� ������
    		method.ImplementationTypes.Add(typeof(IRelation));
    		method.Statements.Add(
    			new CodeMethodReturnStatement(
    				new CodeObjectCreateExpression(
    					new CodeTypeReference(typeof(IRelation.RelationDesc)),
						OrmCodeGenHelper.GetFieldNameReferenceExpression(entity.CompleteEntity.Properties.Find(delegate(PropertyDescription match) { return match.FieldName == relation.Right.FieldName; })),
    					new CodeMethodInvokeExpression(
    						new CodeMethodReferenceExpression(
    							new CodeFieldReferenceExpression(
    								new CodeThisReferenceExpression(),
    								"_schema"
    								),
    							"GetTypeByEntityName"
    							),
    						new CodePrimitiveExpression(relation.Left.Entity.Name)
    						)
    					//new CodeTypeOfExpression(
    					//    new CodeTypeReference(GetEntityClassQualifiedName(relation.Left.Entity, settings))
    					//)
    					)
    				)
    			);
    		#endregion Pair<string, Type> GetSecondType()
    	}

		private void ImplementIRelation(SelfRelationDescription relation, EntityDescription entity, CodeTypeDeclaration entitySchemaDefClass)
		{
			CodeMemberMethod method;
			entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(typeof(IRelation)));
			#region Pair<string, Type> GetFirstType()
			method = new CodeMemberMethod();
			//method.StartDirectives.Add(Regions["IRelation Members"].Start);
			entitySchemaDefClass.Members.Add(method);
			method.Name = "GetFirstType";
			// ��� ������������� ��������
			method.ReturnType = new CodeTypeReference(typeof(IRelation.RelationDesc));
			// ������������ �������
			method.Attributes = MemberAttributes.Public;
			// ��������� ����� �������� ������
			method.ImplementationTypes.Add(typeof(IRelation));
			method.Statements.Add(
				new CodeMethodReturnStatement(
					new CodeObjectCreateExpression(
						new CodeTypeReference(typeof(IRelation.RelationDesc)),
						OrmCodeGenHelper.GetFieldNameReferenceExpression(
							entity.Properties.Find(
								delegate(PropertyDescription match) { return match.FieldName == relation.Direct.FieldName; })),
						new CodeMethodInvokeExpression(
							new CodeMethodReferenceExpression(
								new CodeFieldReferenceExpression(
									new CodeThisReferenceExpression(),
									"_schema"
									),
								"GetTypeByEntityName"
								),
							new CodePrimitiveExpression(relation.Entity.Name)
				//new CodeTypeOfExpression(
				//    new CodeTypeReference(GetEntityClassQualifiedName(relation.Right.Entity, settings))
				//)
							),
						new CodePrimitiveExpression(true)
						)
					)
				);
			#endregion Pair<string, Type> GetFirstType()
			#region Pair<string, Type> GetSecondType()
			method = new CodeMemberMethod();
			//method.EndDirectives.Add(Regions["IRelation Members"].End);
			entitySchemaDefClass.Members.Add(method);
			method.Name = "GetSecondType";
			// ��� ������������� ��������
			method.ReturnType = new CodeTypeReference(typeof(IRelation.RelationDesc));
			// ������������ �������
			method.Attributes = MemberAttributes.Public;
			// ��������� ����� �������� ������
			method.ImplementationTypes.Add(typeof(IRelation));
			method.Statements.Add(
				new CodeMethodReturnStatement(
					new CodeObjectCreateExpression(
						new CodeTypeReference(typeof(IRelation.RelationDesc)),
						OrmCodeGenHelper.GetFieldNameReferenceExpression(entity.CompleteEntity.Properties.Find(delegate(PropertyDescription match) { return match.FieldName == relation.Reverse.FieldName; })),
						new CodeMethodInvokeExpression(
							new CodeMethodReferenceExpression(
								new CodeFieldReferenceExpression(
									new CodeThisReferenceExpression(),
									"_schema"
									),
								"GetTypeByEntityName"
								),
							new CodePrimitiveExpression(relation.Entity.Name)
							),
				//new CodeTypeOfExpression(
				//    new CodeTypeReference(GetEntityClassQualifiedName(relation.Left.Entity, settings))
				//)
							new CodePrimitiveExpression(false)
						)
					)
				);
			#endregion Pair<string, Type> GetSecondType()
		}

    	//private void CreateGetEntityMethod(EntityDescription entity, CodeTypeDeclaration entityClass)
		//{
		//    CodeMemberMethod method = new CodeMemberMethod();
		//    method.Name = "Get";
		//    CodeTypeParameter typePrm = new CodeTypeParameter("T" + entity.Name);
		//    typePrm.Constraints.Add(new CodeTypeReference(OrmCodeGenNameHelper.GetQualifiedEntityName(entity)));
		//    typePrm.HasConstructorConstraint = true;
		//    CodeTypeReference returnType = new CodeTypeReference();
		//    returnType.BaseType = typeof (ICollection<>).FullName;
		//    returnType.TypeArguments.Add(new CodeTypeReference(typePrm.Name));
		//    method.TypeParameters.Add(typePrm);
		//    method.Parameters.Add(new CodeParameterDeclarationExpression(typeof (int), "id"));
		//    method.Attributes = MemberAttributes.Public;
		//    method.Statements.Add(
		//        new CodeMethodReturnStatement(
		//            new CodeMethodInvokeExpression(
		//                new CodeMethodReferenceExpression(
		//                    new Code
		//                    new CodeTypeReferenceExpression(typeof(OrmManagerBase)), 
		//                    "Find", 
		//                    new CodeTypeReference(typePrm.Name)),
		//                    new CodeArgumentReferenceExpression("id")
		//                )
		//            )
		//        );

		//    entityClass.Members.Add(method);
		//}

    	private static CodeMemberMethod CreateGetValueMethod(CodeTypeDeclaration entityClass)
        {
            //public override object GetValue(string propAlias, Worm.Orm.IOrmObjectSchemaBase schema)
            //{
            //    if (Properties.Song.Equals(propAlias))
            //        return Song;
            //    return base.GetValue(propAlias, schema);
            //}

            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = "GetValue";
            method.ReturnType = new CodeTypeReference(typeof (object));
            method.Attributes = MemberAttributes.Override | MemberAttributes.Public;
            CodeParameterDeclarationExpression prm;
            prm = new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof (string)),
                "propAlias"
                );
            method.Parameters.Add(prm);

            prm = new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof (IOrmObjectSchemaBase)),
                "schema"
                );
            method.Parameters.Add(prm);

            method.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodeMethodInvokeExpression(
                        new CodeBaseReferenceExpression(),
                        method.Name,
                        new CodeArgumentReferenceExpression("propAlias"),
                        new CodeArgumentReferenceExpression("schema")
                    )
                )
                );
            entityClass.Members.Add(method);
            return method;
        }

        private static CodeObjectCreateExpression GetMapField2ColumObjectCreationExpression(EntityDescription entity, PropertyDescription action)
        {
            CodeObjectCreateExpression expression = new CodeObjectCreateExpression(
                new CodeTypeReference(
                    typeof (MapField2Column)));
            expression.Parameters.Add(new CodePrimitiveExpression(action.PropertyAlias));
            expression.Parameters.Add(new CodePrimitiveExpression(action.FieldName));
                //(OrmTable)this.GetTables().GetValue((int)(XMedia.Framework.Media.Objects.ArtistBase.ArtistBaseSchemaDef.TablesLink.tblArtists)))
            expression.Parameters.Add(new CodeMethodInvokeExpression(
                    new CodeThisReferenceExpression(),
                    "GetTable",
                    new CodeFieldReferenceExpression(
                        new CodeTypeReferenceExpression(OrmCodeGenNameHelper.GetEntitySchemaDefClassQualifiedName
                                                            (entity) +
                                                        ".TablesLink"),
                        OrmCodeGenNameHelper.GetSafeName(action.Table.Identifier)
                        )
                    ));
            if (action.PropertyAlias == "ID")
                expression.Parameters.Add(GetPropAttributesEnumValues(action.Attributes));
            return expression;
        }

        private static CodeExpression[] GetM2MRelationCreationExpressions(RelationDescription relationDescription, EntityDescription entity)
        {
            if (relationDescription.Left.Entity != relationDescription.Right.Entity)
            {
                EntityDescription relatedEntity = entity == relationDescription.Left.Entity
                    ? relationDescription.Right.Entity : relationDescription.Left.Entity;
                string fieldName = entity == relationDescription.Left.Entity ? relationDescription.Right.FieldName : relationDescription.Left.FieldName;
                bool cascadeDelete = entity == relationDescription.Left.Entity ? relationDescription.Right.CascadeDelete : relationDescription.Left.CascadeDelete;

                return new CodeExpression[] {GetM2MRelationCreationExpression(relatedEntity, relationDescription.Table, relationDescription.UnderlyingEntity, fieldName, cascadeDelete, null)};
            }
            else
            {
            	throw new ArgumentException("To realize m2m relation on self use SelfRelation instead.");
            }


        }
		private static CodeExpression[] GetM2MRelationCreationExpressions(SelfRelationDescription relationDescription, EntityDescription entity)
		{

			return new CodeExpression[]
				{
					GetM2MRelationCreationExpression(entity, relationDescription.Table, relationDescription.UnderlyingEntity,
					                                 relationDescription.Direct.FieldName, relationDescription.Direct.CascadeDelete,
					                                 true),
					GetM2MRelationCreationExpression(entity, relationDescription.Table, relationDescription.UnderlyingEntity,
					                                 relationDescription.Reverse.FieldName, relationDescription.Reverse.CascadeDelete, false)
				};

		}

    	private static CodeExpression GetM2MRelationCreationExpression(EntityDescription relatedEntity, TableDescription relationTable, EntityDescription underlyingEntity, string fieldName, bool cascadeDelete, bool? direct)
        {
			//if (underlyingEntity != null && direct.HasValue)
			//    throw new NotImplementedException("M2M relation on self cannot have underlying entity.");
            // new Worm.Orm.M2MRelation(this._schema.GetTypeByEntityName("Album"), this.GetTypeMainTable(this._schema.GetTypeByEntityName("Album2ArtistRelation")), "album_id", false, new System.Data.Common.DataTableMapping(), this._schema.GetTypeByEntityName("Album2ArtistRelation")),

            CodeExpression entityTypeExpression;
            CodeExpression tableExpression;
            CodeExpression fieldExpression;
            CodeExpression cascadeDeleteExpression;
            CodeExpression mappingExpression;

            entityTypeExpression = new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        "_schema"
                        ),
                    "GetTypeByEntityName"
                    ),
                OrmCodeGenHelper.GetEntityNameReferenceExpression(relatedEntity)
                    //new CodePrimitiveExpression(relatedEntity.Name)
                );
            if (underlyingEntity == null)
                tableExpression = new CodeMethodInvokeExpression(
                    new CodeCastExpression(
                        new CodeTypeReference(typeof(Worm.IDbSchema)),
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_schema")
                        ),
                    "GetSharedTable",
                    new CodePrimitiveExpression(relationTable.Name)
                    );
            else
                tableExpression = new CodeMethodInvokeExpression(
                    new CodeThisReferenceExpression(),
                    "GetTypeMainTable",
                    new CodeMethodInvokeExpression(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_schema"),
                        "GetTypeByEntityName",
                        OrmCodeGenHelper.GetEntityNameReferenceExpression(underlyingEntity)
                        //new CodePrimitiveExpression(underlyingEntity.Name)
                        )
                    );

            fieldExpression = new CodePrimitiveExpression(fieldName);

            cascadeDeleteExpression = new CodePrimitiveExpression(cascadeDelete);

            mappingExpression = new CodeObjectCreateExpression(new CodeTypeReference(typeof(DataTableMapping)));

    		CodeObjectCreateExpression result =
    			new CodeObjectCreateExpression(
    				new CodeTypeReference(typeof (M2MRelation)),
					entityTypeExpression,
					tableExpression,
					fieldExpression,
					cascadeDeleteExpression,
					mappingExpression);

            if (underlyingEntity != null)
            {
                CodeExpression connectedTypeExpression;
                connectedTypeExpression = new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_schema"),
                        "GetTypeByEntityName"
                        ),
                    new CodePrimitiveExpression(underlyingEntity.Name)
                    );
				result.Parameters.Add(
					connectedTypeExpression
                );
            }
            if (direct.HasValue)
            {
                CodeExpression directExpression = new CodePrimitiveExpression(direct.Value);
                result.Parameters.Add(
					directExpression
                );
            }
    		return result;
        }

        private static void CreateChangeValueTypeMethod(EntityDescription entity, CodeTypeDeclaration entitySchemaDefClass)
        {
            CodeMemberMethod method;
            if (entity.Behaviour != EntityBehaviuor.PartialObjects && entity.BaseEntity == null)
            {
                method = new CodeMemberMethod();
                entitySchemaDefClass.Members.Add(method);
                method.Name = "ChangeValueType";
                // ��� ������������� ��������
                method.ReturnType = new CodeTypeReference(typeof(bool));
                // ������������ �������
                method.Attributes = MemberAttributes.Public;
                // ��������� ����� �������� ������
                method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
                // ���������
                method.Parameters.Add(
                    new CodeParameterDeclarationExpression(
                        new CodeTypeReference(typeof(ColumnAttribute)),
                        "c"
                        )
                    );
                method.Parameters.Add(
                    new CodeParameterDeclarationExpression(
                        new CodeTypeReference(typeof(object)),
                        "value"
                        )
                    );
                CodeParameterDeclarationExpression methodParam;
                methodParam = new CodeParameterDeclarationExpression(
                    new CodeTypeReference(typeof(object)),
                    "newvalue"
                    );
                // if((c._behavior & Field2DbRelations.InsertDefault) == Field2DbRelations.InsertDefault && (value == null || value == Activator.CreateInstance(value.GetType()))
                // {
                //      newvalue = DBNull.Value
                //      return true;
                // }
                methodParam.Direction = FieldDirection.Ref;
                method.Parameters.Add(methodParam);
                method.Statements.Add(
                    new CodeConditionStatement(
                        new CodeBinaryOperatorExpression(
                            new CodeBinaryOperatorExpression(
                                new CodeBinaryOperatorExpression(
                                    new CodePropertyReferenceExpression(
                                        new CodeArgumentReferenceExpression("c"),
                                        "_behavior"
                                        ),
                                    CodeBinaryOperatorType.BitwiseAnd,
                                    new CodeFieldReferenceExpression(
                                        new CodeTypeReferenceExpression(typeof(Field2DbRelations)),
                                        "InsertDefault"
                                        )
                                    ),
                                CodeBinaryOperatorType.ValueEquality,
                                new CodeFieldReferenceExpression(
                                    new CodeTypeReferenceExpression(typeof(Field2DbRelations)),
                                    "InsertDefault"
                                    )
                                ),
                            CodeBinaryOperatorType.BooleanAnd,
                            new CodeBinaryOperatorExpression(
                                new CodeBinaryOperatorExpression(
                                new CodeArgumentReferenceExpression("value"),
                                CodeBinaryOperatorType.IdentityEquality,
                                new CodePrimitiveExpression(null)
                                ),
                                CodeBinaryOperatorType.BooleanOr,
                                new CodeMethodInvokeExpression(
                                    new CodeMethodInvokeExpression(
                                        new CodeTypeReferenceExpression(typeof(Activator)),
                                        "CreateInstance",
                                        new CodeMethodInvokeExpression(
                                            new CodeArgumentReferenceExpression("value"),
                                            "GetType"
                                        )
                                    ),
                                    "Equals",
                                    new CodeArgumentReferenceExpression("value")
                                )
                                
                            )
                            ),
                        new CodeAssignStatement(
                            new CodeArgumentReferenceExpression("newvalue"),
                            new CodeFieldReferenceExpression(
                                new CodeTypeReferenceExpression(typeof(DBNull)),
                                "Value"
                            )
                            ),
                        new CodeMethodReturnStatement(new CodePrimitiveExpression(true))
                        )
                    );
                // newvalue = value;
                method.Statements.Add(
                    new CodeAssignStatement(
                        new CodeArgumentReferenceExpression("newvalue"),
                        new CodeArgumentReferenceExpression("value")
                        )
                    );
                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodePrimitiveExpression(false)
                        )
                    );
            }
        }

        private void CreateProperties(CodeMemberMethod createobjectMethod, EntityDescription entity, CodeTypeDeclaration entityClass, CodeMemberMethod setvalueMethod, CodeMemberMethod getvalueMethod, CodeMemberMethod copyMethod, CodeTypeDeclaration propertiesClass)
        {
            EntityDescription completeEntity = entity.CompleteEntity;

            for (int idx = 0; idx < completeEntity.Properties.Count; idx++)
            {
                #region �������� �������� � etc
                PropertyDescription propertyDesc;
                propertyDesc = completeEntity.Properties[idx];
                if (propertyDesc.Disabled)
                    continue;

                {
                    CodeMemberField propConst = new CodeMemberField(typeof(string), propertyDesc.Name);
                    propConst.InitExpression = new CodePrimitiveExpression(propertyDesc.PropertyAlias);
                    propConst.Attributes = MemberAttributes.Const | MemberAttributes.Public;
                    propertiesClass.Members.Add(propConst);
                }

                if (propertyDesc.PropertyAlias == "ID" || propertyDesc.IsSuppressed)
                    continue;
            	CodeMemberProperty property = null;
                if (!propertyDesc.FromBase)
                    property = CreateProperty(copyMethod, createobjectMethod, entityClass, propertyDesc, setvalueMethod, getvalueMethod);
                else if(propertyDesc.IsRefreshed)
                    //CreateProperty(copyMethod, createobjectMethod, entityClass, propertyDesc, settings, setvalueMethod, getvalueMethod);
                    property = CreateUpdatedProperty(entityClass, propertyDesc);

				if (property != null)
				{
					#region property custom attribute Worm.Orm.ColumnAttribute

					CreatePropertyColumnAttribute(property, propertyDesc);

					#endregion property custom attribute Worm.Orm.ColumnAttribute

					#region property obsoletness

					CheckPropertyObsoleteAttribute(property, propertyDesc);

					#endregion
				}
            }
        }

    	private void CheckPropertyObsoleteAttribute(CodeMemberProperty property, PropertyDescription propertyDesc)
    	{
			if (propertyDesc.Obsolete != ObsoleteType.None)
    		{
    			CodeAttributeDeclaration attr =
    				new CodeAttributeDeclaration(new CodeTypeReference(typeof (ObsoleteAttribute)),
    				                             new CodeAttributeArgument(
    				                             	new CodePrimitiveExpression(propertyDesc.ObsoleteDescripton)),
    				                             new CodeAttributeArgument(
    				                             	new CodePrimitiveExpression(propertyDesc.Obsolete == ObsoleteType.Error)));
				if (property.CustomAttributes == null)
					property.CustomAttributes = new CodeAttributeDeclarationCollection();
    			property.CustomAttributes.Add(attr);
    		}
    	}

    	private static CodeMemberProperty CreateUpdatedProperty(CodeTypeDeclaration entityClass, PropertyDescription propertyDesc)
        {
            CodeTypeReference propertyType;
            propertyType = new CodeTypeReference(propertyDesc.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetQualifiedEntityName(propertyDesc.PropertyType.Entity) : propertyDesc.PropertyType.TypeName);

            CodeMemberProperty property;
            property = new CodeMemberProperty();

            property.HasGet = true;
            property.HasSet = true;

            property.Name = propertyDesc.Name;
            property.Type = propertyType;
            property.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.New;

            property.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeCastExpression(
                        propertyType,
                        new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), property.Name)
                    )
                )
                );
            property.SetStatements.Add(
                new CodeAssignStatement(
                    new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), property.Name),
                    new CodePropertySetValueReferenceExpression()
                )
                );

            #region �������� ��������
            SetMemberDescription(property, propertyDesc.Description);
            #endregion �������� ��������

            #region ���������� ������ � �����
            entityClass.Members.Add(property);
            #endregion ���������� ������ � �����

			return property;
        }

		private static CodeMemberProperty CreateProperty(CodeMemberMethod copyMethod, CodeMemberMethod createobjectMethod, CodeTypeDeclaration entityClass, PropertyDescription propertyDesc, CodeMemberMethod setvalueMethod, CodeMemberMethod getvalueMethod)
        {
            CodeMemberField field;
            CodeTypeReference fieldType;
            fieldType = new CodeTypeReference(propertyDesc.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetQualifiedEntityName(propertyDesc.PropertyType.Entity) : propertyDesc.PropertyType.TypeName);
            string fieldName;
            fieldName = OrmCodeGenNameHelper.GetPrivateMemberName(propertyDesc.Name);

            field = new CodeMemberField(fieldType, fieldName);
            field.Attributes = GetMemberAttribute(propertyDesc.FieldAccessLevel);

            CodeMemberProperty property;
            property = new CodeMemberProperty();

            property.HasGet = true;
            property.HasSet = true;

            property.Name = propertyDesc.Name;
            property.Type = fieldType;
            property.Attributes = GetMemberAttribute(propertyDesc.PropertyAccessLevel);
            #endregion �������� �������� � etc

            #region property GetStatements

            CodeExpression getUsingExpression = new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "SyncHelper"),
                new CodePrimitiveExpression(true),
                new CodePrimitiveExpression(propertyDesc.PropertyAlias)
                );

            CodeStatement[] getInUsingStatements = new CodeStatement[]
                {
                    new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName))
                };

			property.GetStatements.AddRange(Delegates.CodePatternUsingStatements(getUsingExpression, getInUsingStatements));
            
            #endregion property GetStatements

            #region property SetStatements

            CodeExpression setUsingExpression = new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "SyncHelper"),
                new CodePrimitiveExpression(false),
                new CodePrimitiveExpression(propertyDesc.PropertyAlias)
                );

            CodeStatement[] setInUsingStatements = new CodeStatement[]
                {
                    new CodeAssignStatement(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(), 
                            fieldName
                            ), 
                        new CodePropertySetValueReferenceExpression()
                        )
                };

			property.SetStatements.AddRange(Delegates.CodePatternUsingStatements(setUsingExpression, setInUsingStatements));

            #endregion property SetStatements

            #region �������� ��������
            SetMemberDescription(property, propertyDesc.Description);
            #endregion �������� ��������

            #region ���������� ������ � �����
            entityClass.Members.Add(field);
            entityClass.Members.Add(property);
            #endregion ���������� ������ � �����

            #region // ���������� ������ Copy

            copyMethod.Statements.Add(
                new CodeAssignStatement(
                    new CodeFieldReferenceExpression(
                        new CodeArgumentReferenceExpression("to"),
                        fieldName
                        ),
                    new CodeFieldReferenceExpression(
                        new CodeArgumentReferenceExpression("from"),
                        fieldName
                        )
                    )
                );
            #endregion // ���������� ������ Copy

            #region void SetValue(System.Reflection.PropertyInfo pi, ColumnAttribute c, object value)

			Delegates.UpdateSetValueMethodMethod(field, propertyDesc, setvalueMethod);

            #endregion void SetValue(System.Reflection.PropertyInfo pi, ColumnAttribute c, object value)

            #region public override object GetValue(string propAlias, Worm.Orm.IOrmObjectsSchema schema)

            UpdateGetValueMethod(property, propertyDesc, getvalueMethod);

            #endregion public override object GetValue(string propAlias, Worm.Orm.IOrmObjectsSchema schema)

            #region void CreateObject(string fieldName, object value)
            if (Array.IndexOf(propertyDesc.Attributes, "Factory") != -1)
            {
                UpdateCreateObjectMethod(createobjectMethod, propertyDesc);
            }

            #endregion void CreateObject(string fieldName, object value)

			return property;
        }

        private static void UpdateGetValueMethod(CodeMemberProperty property, PropertyDescription propertyDesc, CodeMemberMethod getvalueMethod)
        {
            //    if (Properties.Song.Equals(propAlias))
            //        return Song;
            getvalueMethod.Statements.Insert(getvalueMethod.Statements.Count - 1, 
                new CodeConditionStatement(
                    new CodeMethodInvokeExpression(
                        OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc),
                        "Equals",
                        new CodeArgumentReferenceExpression("propAlias")
                    ),
                    new CodeMethodReturnStatement(
                        new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), property.Name)
                    )
                )
            );
        }
        private static void CreatePropertyColumnAttribute(CodeMemberProperty property, PropertyDescription propertyDesc)
        {
            OrmCodeDomGeneratorSettings settings = SettingsManager.CurrentManager.OrmCodeDomGeneratorSettings;    
            CodeAttributeDeclaration declaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(ColumnAttribute)));

            if (!string.IsNullOrEmpty(propertyDesc.PropertyAlias))
            {
                //declaration.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(propertyDesc.PropertyAlias)));
                declaration.Arguments.Add(
                    new CodeAttributeArgument(OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc)));
            }
            if (propertyDesc.Attributes != null && propertyDesc.Attributes.Length != 0)
            {
                declaration.Arguments.Add(new CodeAttributeArgument(GetPropAttributesEnumValues(propertyDesc.Attributes)));
            }
            
            //new CodeAttributeArgument(
            //        new CodePrimitiveExpression(string.IsNullOrEmpty(propertyDesc.PropertyAlias) ? propertyDesc.Name : propertyDesc.PropertyAlias)
            //        ),
            //    new CodeAttributeArgument(
            //        GetPropAttributesEnumValues(propertyDesc.Attributes)
            //        )
            property.CustomAttributes.Add(declaration);
        }

        private static void UpdateCreateObjectMethod(CodeMemberMethod createobjectMethod, PropertyDescription propertyDesc)
        {
            createobjectMethod.Statements.Insert(createobjectMethod.Statements.Count - 1,
                new CodeConditionStatement(
                    new CodeBinaryOperatorExpression(
                        new CodeArgumentReferenceExpression("fieldName"),
                        CodeBinaryOperatorType.ValueEquality,
						new CodePrimitiveExpression(propertyDesc.PropertyAlias)
                        ),
                    new CodeThrowExceptionStatement(
                        new CodeObjectCreateExpression(
                            new CodeTypeReference(typeof(NotImplementedException)),
                            new CodePrimitiveExpression("The method or operation is not implemented.")
                            )
                        )
                    )
                );
        }

        private static CodeMemberMethod CreateCreateObjectMethod(EntityDescription entity, CodeTypeDeclaration entityClass)
        {
            CodeMemberMethod createobjectMethod;
            createobjectMethod = new CodeMemberMethod();
            if (entity.Behaviour != EntityBehaviuor.PartialObjects)
                entityClass.Members.Add(createobjectMethod);
            createobjectMethod.Name = "CreateObject";
            // ��� ������������� ��������
            createobjectMethod.ReturnType = null;
            // ������������ �������
            createobjectMethod.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            
            createobjectMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "fieldName"));
            createobjectMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "value"));

            createobjectMethod.Statements.Add(
                new CodeThrowExceptionStatement(
                    new CodeObjectCreateExpression(
                        new CodeTypeReference(typeof(InvalidOperationException)),
                        new CodePrimitiveExpression("Invalid method usage.")
                    )
                )
                );

            return createobjectMethod;
        }

        private static CodeMemberMethod CreateSetValueMethod(CodeTypeDeclaration entityClass)
        {
            CodeMemberMethod setvalueMethod;
            setvalueMethod = new CodeMemberMethod();
            entityClass.Members.Add(setvalueMethod);
            setvalueMethod.Name = "SetValue";
            // ��� ������������� ��������
            setvalueMethod.ReturnType = null;
            // ������������ �������
            setvalueMethod.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            setvalueMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(PropertyInfo), "pi"));
            setvalueMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ColumnAttribute), "c"));
            setvalueMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "value"));
            setvalueMethod.Statements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference(typeof (string)), 
                    "fieldName",
                    new CodePropertyReferenceExpression(
                        new CodeArgumentReferenceExpression("c"), 
                        "FieldName"
                    )
                )
            );
            return setvalueMethod;
        }

        private static void CreateGetTableMethod(EntityDescription entity, CodeTypeDeclaration entitySchemaDefClass)
        {
            CodeMemberMethod method;
            method = new CodeMemberMethod();
            entitySchemaDefClass.Members.Add(method);
            method.Name = "GetTable";
            // ��� ������������� ��������
            method.ReturnType = new CodeTypeReference(typeof(OrmTable));
            // ������������ �������
            method.Attributes = MemberAttributes.Family;
            if (entity.BaseEntity != null)
            {
                method.Attributes |= MemberAttributes.New;
            }
            // ��������� ����� �������� ������
            // ���������
            method.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(
                        OrmCodeGenNameHelper.GetEntitySchemaDefClassQualifiedName(entity) + ".TablesLink"), "tbl"
                    )
                );
            // return (OrmTable)this.GetTables().GetValue((int)tbl)
            method.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodeCastExpression(
                        new CodeTypeReference(typeof(OrmTable)),
                        new CodeMethodInvokeExpression(
                            new CodeMethodInvokeExpression(
                                new CodeThisReferenceExpression(),
                                "GetTables"
                                ),
                            "GetValue",
                            new CodeCastExpression(
                                new CodeTypeReference(typeof(int)),
                                new CodeArgumentReferenceExpression("tbl")
                                )
                            )
                        )
                    )
                );
        }

        private static void CreateGetTablesMethod(EntityDescription entity, CodeTypeDeclaration entitySchemaDefClass)
        {
            CodeMemberMethod method;
            method = new CodeMemberMethod();
            entitySchemaDefClass.Members.Add(method);
            method.Name = "GetTables";
            // ��� ������������� ��������
            method.ReturnType = new CodeTypeReference(typeof(OrmTable[]));
            // ������������ �������
            method.Attributes = MemberAttributes.Public;
            if (entity.BaseEntity != null)
            {
                method.Attributes |= MemberAttributes.Override;
            }
            else
            // ��������� ����� ����������
            method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
            // ���������
            //...
            // ��� ����
            CodeMemberField forTablesLockField = new CodeMemberField(
                new CodeTypeReference(typeof (object)),
                "_forTablesLock"
                );
            forTablesLockField.InitExpression = new CodeObjectCreateExpression(forTablesLockField.Type);
            entitySchemaDefClass.Members.Add(forTablesLockField);
            // ����
            method.Statements.Add(
				CodePatternDoubleCheckLock(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        "_forTablesLock"
                        ),
                    new CodeBinaryOperatorExpression(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(),
                            "_tables"
                            ),
                        CodeBinaryOperatorType.IdentityEquality,
                        new CodePrimitiveExpression(null)
                        ),
                    new CodeAssignStatement(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(),
                            "_tables"
                            ),
                        new CodeArrayCreateExpression(
                            new CodeTypeReference(typeof (OrmTable[])),
                            entity.CompleteEntity.Tables.ConvertAll<CodeExpression>(delegate(TableDescription action)
                                                                                    {
                                                                                        return new CodeObjectCreateExpression(
                                                                                            new CodeTypeReference(
                                                                                                typeof (OrmTable)),
                                                                                            new CodePrimitiveExpression(action.Name)
                                                                                            );
                                                                                    }
                                ).ToArray()
                            )
                        )
                    )
                );
            method.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        "_tables"
                        )
                    )
                );
        }

        private static void CreateGetTypeMainTableMethod(EntityDescription entity, CodeTypeDeclaration entitySchemaDefClass)
        {
            CodeMemberMethod method;
            method = new CodeMemberMethod();
            entitySchemaDefClass.Members.Add(method);
            method.Name = "GetTypeMainTable";
            // ��� ������������� ��������
            method.ReturnType = new CodeTypeReference(typeof(OrmTable));
            if (entity.BaseEntity != null)
            {
                method.Attributes |= MemberAttributes.Override;
            }
            // ������������ �������
            method.Attributes = MemberAttributes.Family;
            method.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(typeof(Type)),
                    "type"
                    )
                );
            method.Statements.Add(
                new CodeVariableDeclarationStatement(new CodeTypeReference(typeof (OrmTable[])), "tables")
                );
            method.Statements.Add(
                new CodeAssignStatement(
                    new CodeVariableReferenceExpression("tables"),
                    new CodeMethodInvokeExpression(
                        new CodeCastExpression(new CodeTypeReference(typeof(Worm.IDbSchema)), new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_schema")),
                        "GetTables",
                        new CodeArgumentReferenceExpression("type")
                        )
                    )
                );
            method.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodeCastExpression(
                        new CodeTypeReference(typeof(OrmTable)),
                        new CodeMethodInvokeExpression(
                            new CodeVariableReferenceExpression(
                                "tables"
                                ),
                            "GetValue",
                            new CodePrimitiveExpression(0)
                            )
                        )
                    )
                );
        }

        private static void CreateTablesLinkEnum(EntityDescription entity, CodeTypeDeclaration entitySchemaDefClass)
        {
            CodeTypeDeclaration tablesEnum = new CodeTypeDeclaration("TablesLink");
            tablesEnum.Attributes = MemberAttributes.Public;
            tablesEnum.IsClass = false;
            tablesEnum.IsEnum = true;
            tablesEnum.IsPartial = false;
            int tableNum = 0;
            tablesEnum.Members.AddRange(entity.CompleteEntity.Tables.ConvertAll<CodeTypeMember>(delegate(TableDescription tbl)
                                                                                                {
                                                                                                    CodeMemberField enumMember =
                                                                                                        new CodeMemberField();
                                                                                                    enumMember.InitExpression = new CodePrimitiveExpression(tableNum++);
                                                                                                    enumMember.Name = OrmCodeGenNameHelper.GetSafeName(tbl.Identifier);
                                                                                                    return enumMember;
                                                                                                }).ToArray());
            entitySchemaDefClass.Members.Add(tablesEnum);
        }

        private static void ProcessSplitOption(EntityDescription entity, CodeTypeDeclaration entityClass, ref CodeTypeDeclaration entitySchemaDefClass, IDictionary<string, CodeCompileUnit> result)
        {
            OrmCodeDomGeneratorSettings settings = SettingsManager.CurrentManager.OrmCodeDomGeneratorSettings;
            CodeNamespace nameSpace;
            if (settings.Split)
            {
                CodeCompileUnit entitySchemaDefUnit;
                entitySchemaDefUnit = new CodeCompileUnit();

                nameSpace = new CodeNamespace(entity.Namespace);
                entitySchemaDefUnit.Namespaces.Add(nameSpace);

                nameSpace.Imports.Add(new CodeNamespaceImport("System"));
                nameSpace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
                nameSpace.Imports.Add(new CodeNamespaceImport("Worm.Orm"));

                CodeTypeDeclaration entityClassPart;
                entityClassPart = new CodeTypeDeclaration();
                entityClassPart.IsClass = entityClass.IsClass;
                entityClassPart.IsPartial = entityClass.IsPartial;
                entityClassPart.Name = entityClass.Name;
                entityClassPart.Attributes = entityClass.Attributes;
                entityClassPart.TypeAttributes = entityClass.TypeAttributes;

                nameSpace.Types.Add(entityClassPart);

                CodeTypeDeclaration entitySchemaDefClassPart;
                entitySchemaDefClassPart = new CodeTypeDeclaration();
                entityClassPart.Members.Add(entitySchemaDefClassPart);

                entitySchemaDefClassPart.IsClass = entitySchemaDefClass.IsClass;
                entitySchemaDefClassPart.IsPartial = entitySchemaDefClass.IsPartial;
                entitySchemaDefClassPart.Name = entitySchemaDefClass.Name;
                entitySchemaDefClassPart.Attributes = entitySchemaDefClass.Attributes;
                entitySchemaDefClassPart.TypeAttributes = entitySchemaDefClass.TypeAttributes;

                entitySchemaDefClass = entitySchemaDefClassPart;
                result.Add(OrmCodeGenNameHelper.GetEntitySchemaDefFileName(entity), entitySchemaDefUnit);
            }
            else
            {
                entityClass.Members.Add(entitySchemaDefClass);
            }
            if (entity.BaseEntity == null)
            {
                entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(typeof (IOrmObjectSchema)));
                entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(typeof (IOrmSchemaInit)));
            }
            else
            {
                entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(OrmCodeGenNameHelper.GetEntitySchemaDefClassQualifiedName(entity.BaseEntity)));
            }
        }

        private static CodeExpression GetPropAttributesEnumValues(IEnumerable<string> attrs)
        {
            List<String> attrsList = new List<string>(attrs);
            CodeExpression first;
            first = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(Field2DbRelations)), attrsList.Count == 0 ? Field2DbRelations.None.ToString() : attrsList[0]);
            if (attrsList.Count > 1)
                return new CodeBinaryOperatorExpression(first, CodeBinaryOperatorType.BitwiseOr, GetPropAttributesEnumValues(attrsList.GetRange(1, attrsList.Count - 1).ToArray()));
            else
                return first;
        }




        private static void SetMemberDescription(CodeTypeMember member, string description)
        {
            if (string.IsNullOrEmpty(description))
                return;
            member.Comments.Add(new CodeCommentStatement(string.Format("<summary>\n{0}\n</summary>", description), true));
        }

        private static MemberAttributes GetMemberAttribute(AccessLevel accessLevel)
        {
            switch (accessLevel)
            {
                case AccessLevel.Private:
                    return MemberAttributes.Private;
                case AccessLevel.Family:
                    return MemberAttributes.Family;
                case AccessLevel.Assembly:
                    return MemberAttributes.Assembly;
                case AccessLevel.Public:
                    return MemberAttributes.Public;
				case AccessLevel.FamilyOrAssembly:
            		return MemberAttributes.FamilyOrAssembly;
                default:
                    return 0;
            }
        }

		public static CodeStatement CodePatternDoubleCheckLock(CodeExpression lockExpression, CodeExpression condition, params CodeStatement[] statements)
		{
			if (condition == null)
				throw new ArgumentNullException("condition");

			return new CodeConditionStatement(
				condition,
					Delegates.CodePatternLockStatement(lockExpression,
						new CodeConditionStatement(
							condition,
							statements
						)
					)
				);
		}

        protected static class Delegates
        {
			public delegate void UpdateSetValueMethodDelegate(CodeMemberField field, PropertyDescription propertyDesc, CodeMemberMethod setvalueMethod);
			public delegate CodeStatement[] CodePatternUsingStatementsDelegate(CodeExpression usingExpression, params CodeStatement[] statements);
			public delegate CodeExpression CodePatternIsExpressionDelegate(CodeTypeReference typeReference, CodeExpression expression);
			public delegate CodeExpression CodePatternAsExpressionDelegate(CodeTypeReference typeReference, CodeExpression expression);
			public delegate CodeStatement CodePatternLockStatementDelegate(CodeExpression lockExpression, params CodeStatement[] statements);

			public static UpdateSetValueMethodDelegate UpdateSetValueMethodMethod
			{
				get
				{
					OrmCodeDomGeneratorSettings settings = SettingsManager.CurrentManager.OrmCodeDomGeneratorSettings;
					if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.SafeUnboxToEnum) == LanguageSpecificHacks.SafeUnboxToEnum)
						return UpdateSetValueMethodDelegates.EnumPervUpdateSetValueMethod;
					return UpdateSetValueMethodDelegates.DefaultUpdateSetValueMethod;
				}
			}
			public static CodePatternUsingStatementsDelegate CodePatternUsingStatements
			{
				get
				{
					OrmCodeDomGeneratorSettings settings = SettingsManager.CurrentManager.OrmCodeDomGeneratorSettings;
					if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateCSUsingStatement) == LanguageSpecificHacks.GenerateCSUsingStatement)
						return CodePatternUsingStatementDelegates.CSUsing;
					else if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateVBUsingStatement) == LanguageSpecificHacks.GenerateVBUsingStatement)
						return CodePatternUsingStatementDelegates.VBUsing;
					else
						return CodePatternUsingStatementDelegates.CommonUsing;
				}
			}
			public static CodePatternIsExpressionDelegate CodePatternIsExpression
			{
				get
				{
					OrmCodeDomGeneratorSettings settings = SettingsManager.CurrentManager.OrmCodeDomGeneratorSettings;

					if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateCsIsStatement) == LanguageSpecificHacks.GenerateCsIsStatement)
						return CodePatternIsExpressionDelegates.CsExpression;
					else if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateVbTypeOfIsStatement) == LanguageSpecificHacks.GenerateVbTypeOfIsStatement)
						return CodePatternIsExpressionDelegates.VbExpression;
					else return CodePatternIsExpressionDelegates.CommonExpression;
				}
			}
			public static CodePatternAsExpressionDelegate CodePatternAsExpression
			{
				get
				{
					OrmCodeDomGeneratorSettings settings = SettingsManager.CurrentManager.OrmCodeDomGeneratorSettings;

					if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateCsAsStatement) == LanguageSpecificHacks.GenerateCsAsStatement)
						return CodePatternAsExpressionDelegates.CsExpression;
					else if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateVbTryCastStatement) == LanguageSpecificHacks.GenerateVbTryCastStatement)
						return CodePatternAsExpressionDelegates.VbExpression;
					else return CodePatternAsExpressionDelegates.CommonExpression;
				}
			}
        	public static CodePatternLockStatementDelegate CodePatternLockStatement
        	{
        		get
        		{
					OrmCodeDomGeneratorSettings settings = SettingsManager.CurrentManager.OrmCodeDomGeneratorSettings;
					if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateCsLockStatement) == LanguageSpecificHacks.GenerateCsLockStatement)
						return CodePatternLockStatementDelegates.CsStatement;
					else if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateCsLockStatement) == LanguageSpecificHacks.GenerateCsLockStatement)
						return CodePatternLockStatementDelegates.VbStatement;
					else return CodePatternLockStatementDelegates.CommonStatement;
        		}
        	}

			/// <summary>
			/// void UpdateSetValueMethodDelegate(CodeMemberField field, PropertyDescription propertyDesc, CodeMemberMethod setvalueMethod);
			/// </summary>
			public static class UpdateSetValueMethodDelegates
			{
				public static void DefaultUpdateSetValueMethod(CodeMemberField field, PropertyDescription propertyDesc, CodeMemberMethod setvalueMethod)
				{
					//Type fieldRealType;
					//fieldRealType = Type.GetType(field.Type.BaseType, false);



					CodeConditionStatement setValueStatement = new CodeConditionStatement(
						new CodeMethodInvokeExpression(
							OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc),
							"Equals",
							new CodeVariableReferenceExpression("fieldName"))
						);

					//setValueStatement.TrueStatements.Add(
					//    new CodeVariableDeclarationStatement(typeof(IConvertible), "vConv",
					//        new Codety)
					//    );

					//old: simple cast
					//setValueStatement.TrueStatements.Add(new CodeAssignStatement(
					//                         new CodeFieldReferenceExpression(
					//                             new CodeThisReferenceExpression(), field.Name),
					//                         new CodeCastExpression(field.Type,
					//                                                new CodeArgumentReferenceExpression(
					//                                                    "value"))));

					// new: solves problem of direct casts with Nullable<>
					if (propertyDesc.PropertyType.IsNullableType && propertyDesc.PropertyType.IsClrType && propertyDesc.PropertyType.ClrType.GetGenericArguments()[0].IsValueType)
					{
						setValueStatement.TrueStatements.Add(
							new CodeVariableDeclarationStatement(typeof (IConvertible), "iconvVal",
							                                     CodePatternAsExpression(new CodeTypeReference(typeof (IConvertible)),
							                                                             new CodeArgumentReferenceExpression("value"))));
						setValueStatement.TrueStatements.Add(
							new CodeConditionStatement(
								new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("iconvVal"),
								                                 CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(null)),
								new CodeStatement[]
									{
										new CodeAssignStatement(
											new CodeFieldReferenceExpression(
												new CodeThisReferenceExpression(), field.Name),
											new CodeCastExpression(field.Type,
											                       new CodeArgumentReferenceExpression(
											                       	"value")))
									},
								new CodeStatement[]
									{
										//System.Threading.Thread.CurrentThread.CurrentCulture
										new CodeAssignStatement(
											new CodeFieldReferenceExpression(
												new CodeThisReferenceExpression(), field.Name),
											new CodeMethodInvokeExpression(
											new CodeVariableReferenceExpression("iconvVal"),
											GetIConvertableMethodName(propertyDesc.PropertyType.ClrType.GetGenericArguments()[0]),
											new CodePropertyReferenceExpression(
												new CodePropertyReferenceExpression(
													new CodeTypeReferenceExpression(typeof(System.Threading.Thread)),
													"CurrentThread"
													),
													"CurrentCulture"
												)
											)
											)
										
									}
								)
							);
					}
					else
					{
						//old: simple cast
						setValueStatement.TrueStatements.Add(new CodeAssignStatement(
												 new CodeFieldReferenceExpression(
													 new CodeThisReferenceExpression(), field.Name),
												 new CodeCastExpression(field.Type,
																		new CodeArgumentReferenceExpression(
																			"value"))));
					}
					setValueStatement.TrueStatements.Add(new CodeMethodReturnStatement());
					setvalueMethod.Statements.Add(setValueStatement);
				}

				private static string GetIConvertableMethodName(Type type)
				{
					return "To" + type.Name;
				}

				public static void EnumPervUpdateSetValueMethod(CodeMemberField field, PropertyDescription propertyDesc, CodeMemberMethod setvalueMethod)
				{
					if (propertyDesc.PropertyType.IsEnum)
					{
						CodeConditionStatement setValueStatement = new CodeConditionStatement(
						new CodeMethodInvokeExpression(
							OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc),
							"Equals",
							new CodeVariableReferenceExpression("fieldName"))

						);
						if (propertyDesc.PropertyType.IsNullableType)
						{
							setValueStatement.TrueStatements.Add(
								new CodeConditionStatement(
									new CodeBinaryOperatorExpression(
										new CodeArgumentReferenceExpression("value"),
										CodeBinaryOperatorType.IdentityEquality,
										new CodePrimitiveExpression(null)
									),
									new CodeStatement[]
                                {
                                    new CodeAssignStatement(
                                                         new CodeFieldReferenceExpression(
                                                             new CodeThisReferenceExpression(), field.Name),
                                                         new CodePrimitiveExpression(null))
                                },
									new CodeStatement[]
                                {
                                    new CodeVariableDeclarationStatement(
                                        new CodeTypeReference(typeof(Type)),
                                        "t",
                                        new CodeArrayIndexerExpression(
                                                                new CodeMethodInvokeExpression(
                                                                    new CodeTypeOfExpression(field.Type),
                                                                    "GetGenericArguments"
                                                                ),
                                                                new CodePrimitiveExpression(0)
                                                            )
                                    ),
                                    new CodeAssignStatement(
                                                         new CodeFieldReferenceExpression(
                                                             new CodeThisReferenceExpression(), field.Name),
                                                         new CodeCastExpression(
                                                         field.Type,
                                                         new CodeMethodInvokeExpression(
                                                            new CodeTypeReferenceExpression(typeof(Enum)),
                                                            "ToObject",
                                                            // typeof(Nullable<int>).GetGenericArguments()[0]
                                                            new CodeVariableReferenceExpression("t"),
                                                            new CodeArgumentReferenceExpression(
                                                                                    "value")
                                    )))
                    
                                                            
                                 }

								)
							);
						}
						else
						{
							setValueStatement.TrueStatements.Add(new CodeAssignStatement(
																 new CodeFieldReferenceExpression(
																	 new CodeThisReferenceExpression(), field.Name),
																 new CodeCastExpression(
																 field.Type,
																 new CodeMethodInvokeExpression(
																	new CodeTypeReferenceExpression(typeof(Enum)),
																	"ToObject",
								// typeof(Nullable<int>).GetGenericArguments()[0]
																	new CodeTypeOfExpression(field.Type),
																	new CodeArgumentReferenceExpression(
																							"value")
											))));
						}
						setValueStatement.TrueStatements.Add(new CodeMethodReturnStatement());
						setvalueMethod.Statements.Add(setValueStatement);
					}
					else
					{
						DefaultUpdateSetValueMethod(field, propertyDesc, setvalueMethod);
					}
				}	
			}

			//CodeStatement[] CodePatternUsingStatementsDelegate(CodeExpression usingExpression, params CodeStatement[] statements);
			
            public static class CodePatternUsingStatementDelegates
            {
            	public static CodeStatement[] CSUsing(CodeExpression usingExpression, params CodeStatement[] statements)
            	{
            		return new CodeStatement[] {new CodeDomPatterns.CodeCSUsingStatement(usingExpression, statements)};
            	}

				public static CodeStatement[] VBUsing(CodeExpression usingExpression, params CodeStatement[] statements)
				{
					return new CodeStatement[] { new CodeDomPatterns.CodeVBUsingStatement(usingExpression, statements) };
				}

				public static CodeStatement[] CommonUsing(CodeExpression usingExpression, params CodeStatement[] statements)
				{
					return CodeDomPatterns.CommonPatterns.CodePatternUsingStatement(usingExpression, statements);
				}
            }

			//CodeExpression CodePatternIsExpressionDelegate(CodeTypeReference typeReference, CodeExpression expression);

			public static class CodePatternIsExpressionDelegates
			{
				public static CodeExpression CsExpression(CodeTypeReference typeReference, CodeExpression expression)
				{
					return new CodeDomPatterns.CodeCsIsExpression(typeReference, expression);
				}

				public static CodeExpression VbExpression(CodeTypeReference typeReference, CodeExpression expression)
				{
					return new CodeDomPatterns.CodeVbIsExpression(typeReference, expression);
				}

				public static CodeExpression CommonExpression(CodeTypeReference typeReference, CodeExpression expression)
				{
					return CodeDomPatterns.CommonPatterns.CodeIsExpression(typeReference, expression);
				}
			}

			//CodeExpression CodePatternAsExpressionDelegate(CodeTypeReference typeReference, CodeExpression expression);

			public static class CodePatternAsExpressionDelegates
			{
				public static CodeExpression CsExpression(CodeTypeReference typeReference, CodeExpression expression)
				{
					return new CodeDomPatterns.CodeCsAsExpression(typeReference, expression);
				}

				public static CodeExpression VbExpression(CodeTypeReference typeReference, CodeExpression expression)
				{
					return new CodeDomPatterns.CodeVbAsExpression(typeReference, expression);
				}

				public static CodeExpression CommonExpression(CodeTypeReference typeReference, CodeExpression expression)
				{
					return CodeDomPatterns.CommonPatterns.CodeAsExpression(typeReference, expression);
				}
			}

			//public delegate CodeStatement CodePatternLockStatementDelegate(CodeExpression lockExpression, params CodeStatement[] statements);
			public static class CodePatternLockStatementDelegates
			{
				public static CodeStatement CsStatement(CodeExpression lockExpression, params CodeStatement[] statements)
				{
					return new CodeDomPatterns.CodeCsLockStatement(lockExpression, statements);
				}

				public static CodeStatement VbStatement(CodeExpression lockExpression, params CodeStatement[] statements)
				{
					return new CodeDomPatterns.CodeVbLockStatement(lockExpression, statements);
				}

				public static CodeStatement CommonStatement(CodeExpression lockExpression, params CodeStatement[] statements)
				{
					return CodeDomPatterns.CommonPatterns.CodePatternLock(lockExpression, statements);
				}
			}
        }
    }
}
