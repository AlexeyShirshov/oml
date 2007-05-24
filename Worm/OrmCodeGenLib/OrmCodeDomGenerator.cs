using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using OrmCodeGenLib.Descriptors;
using Worm.Orm;
using Worm.Orm.Collections;
//using XMedia.Framework;
using CoreFramework.Structures;
using System.Text.RegularExpressions;
using System.Text;

namespace OrmCodeGenLib
{
    public class OrmCodeDomGenerator
    {
        const string REGION_PROPERTIES = "Description Properties";
        const string REGION_STATIC_MEMBERS = "Static members";
        const string REGION_BASE_TYPE_RELATED = "Base type related members";
        const string REGION_PRIVATE_FIELDS = "Private Fields";
        const string REGION_NESTED_TYPES = "Nested Types";
        const string REGION_CONSTRUCTORS = "Constructors";
        #region Nested Types

        protected class CodeRegion
        {
            public readonly CodeRegionDirective Start;
            public readonly CodeRegionDirective End;

            public CodeRegion(string title)
            {
                Start = new CodeRegionDirective(CodeRegionMode.Start, title);
                End = new CodeRegionDirective(CodeRegionMode.End, title);
            }
        }

        protected class RegionsDictionary
        {
            private readonly Dictionary<string, CodeRegion> _dictionary;

            public RegionsDictionary()
            {
                _dictionary = new Dictionary<string, CodeRegion>();
            }

            public CodeRegion Add(string name, string title)
            {
                _dictionary.Add(name, new CodeRegion(title));
                return _dictionary[name];
            }

            public CodeRegion Add(string name)
            {
                return Add(name, name);
            }

            public void Remove(string name)
            {
                _dictionary.Remove(name);
            }

            public CodeRegion this[string name]
            {
                get
                {
                    if (_dictionary.ContainsKey(name))
                        return _dictionary[name];
                    else
                        return Add(name);
                }
            }
        }

        #endregion Nested Types

        private readonly OrmObjectsDef _ormObjectsDefinition;
        
        private static RegionsDictionary Regions;

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
                    result.Add(pair.Key, pair.Value);
                }
            }
            return result;
        }

        public Dictionary<string, CodeCompileUnit> GetEntityDom(string entityId, OrmCodeDomGeneratorSettings settings)
        {
            Dictionary<string, CodeCompileUnit> result = new Dictionary<string, CodeCompileUnit>();
            if (String.IsNullOrEmpty(entityId))
                throw new ArgumentNullException("entityId");
            if (settings == null)
                throw new ArgumentNullException("settings");

            EntityDescription entity;
            entity = _ormObjectsDefinition.GetEntity(entityId);
            Regions = new RegionsDictionary();

            if (entity == null)
                throw new ArgumentException("entityId", string.Format("Entity with id '{0}' not found.", entityId));

            CodeCompileUnit entityUnit;
            CodeNamespace nameSpace;
            CodeTypeDeclaration entityClass, entitySchemaDefClass;

            CodeConstructor ctr;
            CodeMemberMethod method;
            CodeMemberField field;

            #region определение класса сущности
            entityUnit = new CodeCompileUnit();
            result.Add(GetEntityFileName(entity, settings), entityUnit);

            // неймспейс
            nameSpace = new CodeNamespace(_ormObjectsDefinition.Namespace);
            entityUnit.Namespaces.Add(nameSpace);

            // импорты
            nameSpace.Imports.Add(new CodeNamespaceImport("System"));
            nameSpace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            nameSpace.Imports.Add(new CodeNamespaceImport("Worm.Orm"));

            // класс сущности
            entityClass = new CodeTypeDeclaration(GetEntityClassName(entity, settings, false));
            nameSpace.Types.Add(entityClass);

            // параметры класса
            entityClass.IsClass = true;
            entityClass.IsPartial = settings.IsPartial;
            entityClass.Attributes = MemberAttributes.Public;
            entityClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.Public;

            if(settings.Behaviour == OrmObjectGeneratorBehaviour.BaseObjects)
            {
                entityClass.Attributes |= MemberAttributes.Abstract;
                entityClass.TypeAttributes |= TypeAttributes.Abstract;
            }

            // дескрипшн
            SetMemberDescription(entityClass, entity.Description);

            // базовый класс
            if(entity.BaseEntity == null)
                entityClass.BaseTypes.Add(new CodeTypeReference(typeof(OrmBase)));
            else
                entityClass.BaseTypes.Add(new CodeTypeReference(entity.BaseEntity.QualifiedName));
            #endregion определение класса сущности

            #region определение схемы
            entitySchemaDefClass = new CodeTypeDeclaration(GetEntitySchemaDefClassName(entity, settings, false));

            entitySchemaDefClass.IsClass = true;
            entitySchemaDefClass.IsPartial = settings.IsPartial;
            entitySchemaDefClass.Attributes = MemberAttributes.Public;
            entitySchemaDefClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.NestedPublic;

			//if (settings.Behaviour == OrmObjectGeneratorBehaviour.BaseObjects)
			//{
			//    entitySchemaDefClass.Attributes |= MemberAttributes.Abstract;
			//    entitySchemaDefClass.TypeAttributes |= TypeAttributes.Abstract;
			//}
            #endregion определение схемы

            #region custom attribute EntityAttribute
            if (settings.Behaviour != OrmObjectGeneratorBehaviour.BaseObjects)
            {
                entityClass.CustomAttributes = new CodeAttributeDeclarationCollection(
                    new CodeAttributeDeclaration[]
                        {
                            new CodeAttributeDeclaration(
                                new CodeTypeReference(typeof (EntityAttribute)),
                                new CodeAttributeArgument(
                                    new CodeTypeOfExpression(
                                        new CodeTypeReference(GetEntitySchemaDefClassQualifiedName(entity, settings, false))
                                        )
                                    ),
                                new CodeAttributeArgument(
                                    new CodePrimitiveExpression(_ormObjectsDefinition.SchemaVersion)
                                    ),
                                new CodeAttributeArgument(
                                    "EntityName",
                                    new CodePrimitiveExpression(entity.Name)
                                    )
                                )
                        }
                    );
            }

            #endregion custom attribute EntityAttribute

            #region конструкторы
            // конструктор по умолчанию
            ctr = new CodeConstructor();
            ctr.Attributes = MemberAttributes.Public;
            entityClass.Members.Add(ctr);

            // параметризированный конструктор
            ctr = new CodeConstructor();
            ctr.Attributes = MemberAttributes.Public;
            // параметры конструктора
            ctr.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "id"));
            ctr.Parameters.Add(new CodeParameterDeclarationExpression(typeof(OrmCacheBase), "cache"));
            ctr.Parameters.Add(new CodeParameterDeclarationExpression(typeof(OrmSchemaBase), "schema"));
            // передача параметров базовому конструктору
            ctr.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("id"));
            ctr.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("cache"));
            ctr.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("schema"));
            entityClass.Members.Add(ctr);
            #endregion конструкторы

            #region метод Copy копирования значений пропертей
            CodeMemberMethod copyMethod;
            copyMethod = new CodeMemberMethod();
            entityClass.Members.Add(copyMethod);
            copyMethod.Name = "Copy" + entity.Name;
            copyMethod.ReturnType = null;
            copyMethod.Attributes = MemberAttributes.Static | MemberAttributes.Family;
            SetMemberDescription(copyMethod, "Метод копирует значения полей одного объекта в другой");
            copyMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(GetQualifiedEntityName(entity, settings, false)), "from"));
            copyMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(GetQualifiedEntityName(entity, settings, false)), "to"));
            copyMethod.StartDirectives.Add(Regions[REGION_STATIC_MEMBERS].Start);
            copyMethod.EndDirectives.Add(Regions[REGION_STATIC_MEMBERS].End);
            #endregion метод Copy копирования значений пропертей

            #region метод OrmBase.CopyBody(CopyBody(OrmBase from, OrmBase to)
            method = new CodeMemberMethod();
            entityClass.Members.Add(method);
            method.Name = "CopyBody";
            // тип возвращаемого значения
            method.ReturnType = null;
            // модификаторы доступа
            method.Attributes = MemberAttributes.Family | MemberAttributes.Override;
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(OrmBase), "from"));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(OrmBase), "to"));
            method.Statements.Add(
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        new CodeTypeReferenceExpression(new CodeTypeReference(GetQualifiedEntityName(entity, settings, false))),
                        "Copy" + entity.Name
                    ),
                    new CodeCastExpression(new CodeTypeReference(GetQualifiedEntityName(entity, settings,false)), new CodeArgumentReferenceExpression("from")),
                    new CodeCastExpression(new CodeTypeReference(GetQualifiedEntityName(entity, settings,false)), new CodeArgumentReferenceExpression("to"))
                )
            );
            #endregion метод OrmBase.CopyBody(CopyBody(OrmBase from, OrmBase to)

            #region метод IComparer<T> OrmBase.CreateSortComparer<T>(string sort, SortType sortType)
            if (settings.Behaviour != OrmObjectGeneratorBehaviour.PartialObjects && entity.BaseEntity == null)
            {
                method = new CodeMemberMethod();
                entityClass.Members.Add(method);
                method.Name = "CreateSortComparer";
                // generic параметр
                CodeTypeParameter prm = new CodeTypeParameter("T");
                if (Convert.ToBoolean(settings.LanguageSpecificHacks & LanguageSpecificHacks.DerivedGenericMembersRequireConstraits))
                {
                    prm.Constraints.Add(new CodeTypeReference(typeof (OrmBase)));
                    prm.HasConstructorConstraint = true;
                }
                method.TypeParameters.Add(prm);
                
                // тип возвращаемого значения
                CodeTypeReference methodReturnType;
                methodReturnType = new CodeTypeReference();
                methodReturnType.BaseType = "System.Collections.Generic.IComparer";
                methodReturnType.Options = CodeTypeReferenceOptions.GenericTypeParameter;
                methodReturnType.TypeArguments.Add("T");
                method.ReturnType = methodReturnType;
                // модификаторы доступа
                method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
                // реализует метод базового класса
                
                // параметры
                method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sort"));
                method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(SortType), "sortType"));
                method.Statements.Add(
                    new CodeThrowExceptionStatement(
                        new CodeObjectCreateExpression(
                            typeof(NotImplementedException),
                            new CodePrimitiveExpression("The method or operation is not implemented.")
                        )
                    )
                );
            }
            #endregion метод IComparer<T> OrmBase.CreateSortComparer<T>(string sort, SortType sortType)

            #region метод IComparer OrmBase.CreateSortComparer(string sort, SortType sortType)
            if (settings.Behaviour != OrmObjectGeneratorBehaviour.PartialObjects &&  entity.BaseEntity == null)
            {
                method = new CodeMemberMethod();
                entityClass.Members.Add(method);
                method.Name = "CreateSortComparer";
                // тип возвращаемого значения
                method.ReturnType = new CodeTypeReference(typeof(IComparer));
                // модификаторы доступа
                method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
                // реализует метод базового класса
                
                // параметры
                method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sort"));
                method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(SortType), "sortType"));
                method.Statements.Add(
                    new CodeThrowExceptionStatement(
                        new CodeObjectCreateExpression(
                            typeof(NotImplementedException),
                            new CodePrimitiveExpression("The method or operation is not implemented.")
                        )
                    )
                ); 
            }
            #endregion метод IComparer CreateSortComparer(string sort, SortType sortType)

            #region void SetValue(System.Reflection.PropertyInfo pi, ColumnAttribute c, object value)
            CodeMemberMethod setvalueMethod;
            setvalueMethod = new CodeMemberMethod();
            entityClass.Members.Add(setvalueMethod);
            setvalueMethod.Name = "SetValue";
            // тип возвращаемого значения
            setvalueMethod.ReturnType = null;
            // модификаторы доступа
            setvalueMethod.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            setvalueMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(PropertyInfo), "pi"));
            setvalueMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ColumnAttribute), "c"));
            setvalueMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "value"));
            #endregion void SetValue(System.Reflection.PropertyInfo pi, ColumnAttribute c, object value)

            #region void CreateObject(string fieldName, object value)
            CodeMemberMethod createobjectMethod;
            createobjectMethod = new CodeMemberMethod();
            entityClass.Members.Add(createobjectMethod);
            createobjectMethod.Name = "CreateObject";
            // тип возвращаемого значения
            createobjectMethod.ReturnType = null;
            // модификаторы доступа
            createobjectMethod.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            
            createobjectMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "fieldName"));
            createobjectMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "value"));
            #endregion void CreateObject(string fieldName, object value)

            #region метод OrmBase GetNew()
            if (settings.Behaviour != OrmObjectGeneratorBehaviour.BaseObjects)
            {
                method = new CodeMemberMethod();
                entityClass.Members.Add(method);
                method.Name = "GetNew";
                // тип возвращаемого значения
                method.ReturnType = new CodeTypeReference(typeof (OrmBase));
                // модификаторы доступа
                method.Attributes = MemberAttributes.Family | MemberAttributes.Override;
                // реализует метод базового класса

                // параметры
                if (settings.Behaviour != OrmObjectGeneratorBehaviour.BaseObjects)
                {
                    method.Statements.Add(
                        new CodeMethodReturnStatement(
                            new CodeObjectCreateExpression(
                                new CodeTypeReference(
                                       GetQualifiedEntityName(entity, settings, true)
                                    ),
                                new CodePropertyReferenceExpression(
                                    new CodeThisReferenceExpression(),
                                    "Identifier"
                                    ),
                                new CodePropertyReferenceExpression(
                                    new CodeThisReferenceExpression(),
                                    "OrmCache"
                                    ),
                                new CodePropertyReferenceExpression(
                                    new CodeThisReferenceExpression(),
                                    "OrmSchema"
                                    )
                                )
                            )
                        );
                }
            }

            #endregion метод OrmBase GetNew()

            #region проперти

            EntityDescription completeEntity = entity.CompleteEntity;

            for (int idx = 0; idx < completeEntity.Properties.Count; idx++)
            {
                #region создание проперти и etc
                PropertyDescription propertyDesc;
                propertyDesc = completeEntity.Properties[idx];

                if (propertyDesc.Name == "ID")
                    continue;

                CodeTypeReference fieldType;
                fieldType = new CodeTypeReference(propertyDesc.PropertyTypeString);
                string fieldName;
                fieldName = GetPrivateMemberName(propertyDesc.Name);

                field = new CodeMemberField(fieldType, fieldName);
                field.Attributes = MemberAttributes.Private;

                CodeMemberProperty property;
                property = new CodeMemberProperty();

                property.HasGet = true;
                property.HasSet = true;

                property.Name = propertyDesc.Name;
                property.Type = fieldType;
                property.Attributes = MemberAttributes.Public;
                if (propertyDesc.FromBase)
                    property.Attributes |= MemberAttributes.New;
                #endregion создание проперти и etc

                #region property GetStatements
                property.GetStatements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(IDisposable)), "syncHelper"));
                property.GetStatements.Add(
                    new CodeAssignStatement(
                        new CodeVariableReferenceExpression("syncHelper"),
                        new CodePrimitiveExpression(null)
                    )
                );
                property.GetStatements.Add(new CodeTryCatchFinallyStatement(
                    new CodeStatement[] {
                        new CodeAssignStatement(
                            new CodeArgumentReferenceExpression("syncHelper"),
                            new CodeMethodInvokeExpression(
                                new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "SyncHelper"),
                                new CodePrimitiveExpression(true),
                                new CodePrimitiveExpression(property.Name)
                            )
                        ),
                        new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName))
                    },
                    new CodeCatchClause[] {
                    },
                    new CodeStatement[] {
                        new CodeConditionStatement(
                            new CodeBinaryOperatorExpression(
                                new CodeVariableReferenceExpression("syncHelper"),
                                CodeBinaryOperatorType.IdentityInequality,
                                new CodePrimitiveExpression(null)
                            ),
                            new CodeExpressionStatement(
                                new CodeMethodInvokeExpression(
                                    new CodeCastExpression(typeof(IDisposable), new CodeArgumentReferenceExpression("syncHelper")),
                                    "Dispose",
                                    new CodeExpression[]{}
                                )
                            )
                        )
                    })
                );
                #endregion property GetStatements

                #region property SetStatements
                property.SetStatements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(IDisposable)), "syncHelper"));
                property.SetStatements.Add(
                    new CodeAssignStatement(
                        new CodeVariableReferenceExpression("syncHelper"),
                        new CodePrimitiveExpression(null)
                    )
                );
                property.SetStatements.Add(new CodeTryCatchFinallyStatement(
                    new CodeStatement[] {
                        new CodeAssignStatement(
                            new CodeVariableReferenceExpression("syncHelper"),
                            new CodeMethodInvokeExpression(
                                new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "SyncHelper"),
                                new CodePrimitiveExpression(false),
                                new CodePrimitiveExpression(property.Name)
                            )
                        ),
                        new CodeAssignStatement(
                            new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName),
                            new CodePropertySetValueReferenceExpression()
                        )
                    },
                    new CodeCatchClause[] {
                    },
                    new CodeStatement[] {
                        new CodeConditionStatement(
                            new CodeBinaryOperatorExpression(
                                new CodeVariableReferenceExpression("syncHelper"),
                                CodeBinaryOperatorType.IdentityInequality,
                                new CodePrimitiveExpression(null)
                            ),
                            new CodeExpressionStatement(
                                new CodeMethodInvokeExpression(
                                    new CodeCastExpression(typeof(IDisposable), new CodeArgumentReferenceExpression("syncHelper")),
                                    "Dispose",
                                    new CodeExpression[]{}
                                )
                            )
                        )
                    })
                );
                #endregion property SetStatements

                #region property custom attribute Worm.Orm.ColumnAttribute
                property.CustomAttributes.Add(
                            new CodeAttributeDeclaration(
                                new CodeTypeReference(typeof(ColumnAttribute)),
                                new CodeAttributeArgument(
                                    new CodePrimitiveExpression(propertyDesc.Name)
                                ),
                                new CodeAttributeArgument(
                                    GetPropAttributesEnumValues(propertyDesc.Attributes)
                                    //new CodeCastExpression(
                                    //    new CodeTypeReference(typeof(Worm.Orm.Field2DbRelations)),
                                        
                                    //    new CodePrimitiveExpression(
                                    //        (int)GetPropAttributesEnumValue(propertyDesc.Attributes)
                                    //    )
                                    //)
                                )
                            )
                        );
                #endregion property custom attribute Worm.Orm.ColumnAttribute

                #region описание проперти
                SetMemberDescription(property, propertyDesc.Description);
                #endregion описание проперти

                #region добавление членов в класс
                entityClass.Members.Add(field);
                entityClass.Members.Add(property);
                #endregion добавление членов в класс

                #region реализация метода Copy
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
                #endregion реализация метода Copy

                #region void SetValue(System.Reflection.PropertyInfo pi, ColumnAttribute c, object value)
                Type fieldRealType;
                fieldRealType = Type.GetType(fieldType.BaseType, false);

                if (fieldRealType != null)
                {
                    setvalueMethod.Statements.Add(
                        new CodeConditionStatement(
                            new CodeBinaryOperatorExpression(
                                new CodeFieldReferenceExpression(
                                    new CodeArgumentReferenceExpression("c"),
                                    "FieldName"
                                ),
                                CodeBinaryOperatorType.ValueEquality,
                                new CodePrimitiveExpression(property.Name)
                            ),
                            new CodeAssignStatement(
                                new CodeFieldReferenceExpression(
                                    new CodeThisReferenceExpression(),
                                    fieldName
                                ),
                                new CodeCastExpression(
                                    fieldType,
                                    new CodeArgumentReferenceExpression("value")
                                )
                            ),
                            new CodeMethodReturnStatement()
                        )
                    );
                }
                #endregion void SetValue(System.Reflection.PropertyInfo pi, ColumnAttribute c, object value)

                #region void CreateObject(string fieldName, object value)
                if (Array.IndexOf(propertyDesc.Attributes, "Factory") != -1)
                {
                    createobjectMethod.Statements.Add(
                        new CodeConditionStatement(
                            new CodeBinaryOperatorExpression(
                                new CodeArgumentReferenceExpression("fieldName"),
                                CodeBinaryOperatorType.ValueEquality,
                                new CodePrimitiveExpression(property.Name)
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
                #endregion void CreateObject(string fieldName, object value)
            }
            #endregion проперти

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

            #region обработка директивы Split
            if (settings.Split)
            {
                CodeCompileUnit entitySchemaDefUnit;
                entitySchemaDefUnit = new CodeCompileUnit();

                nameSpace = new CodeNamespace(_ormObjectsDefinition.Namespace);
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
                result.Add(GetEntitySchemaDefFileName(entity, settings), entitySchemaDefUnit);
            }
            else
            {
                entityClass.Members.Add(entitySchemaDefClass);
            }
            #endregion обработка директивы Split

            if (entity.BaseEntity == null)
            {
                entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(typeof (IOrmObjectSchema)));
                entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(typeof (IOrmSchemaInit)));
            }
            else
            {
                entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(GetEntitySchemaDefClassQualifiedName(entity.BaseEntity, settings, true)));
            }

            #region энумератор табличек
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
                                                                                     enumMember.Name = GetSafeName(tbl.Identifier);
                                                                                     return enumMember;
                                                                                 }).ToArray());
            entitySchemaDefClass.Members.Add(tablesEnum);
            #endregion энумератор табличек

            #region конструктор типа

            //CodeTypeConstructor staticCtr = new CodeTypeConstructor();
            //staticCtr.Statements.Add(
            //        new CodeAssignStatement(
            //            new CodeFieldReferenceExpression(
            //                new CodeTypeReferenceExpression(
            //                    new CodeTypeReference(GetEntitySchemaDefClassQualifiedName(entity, settings))
            //                ),
            //                "_tables"
            //            ),
            //            new CodeArrayCreateExpression(
            //                new CodeTypeReference(typeof(Worm.Orm.OrmTable[])),
            //                entity.Tables.ConvertAll<CodeExpression>(delegate(TableDescription action)
            //                {
            //                    return new CodeObjectCreateExpression(
            //                        new CodeTypeReference(typeof(Worm.Orm.OrmTable)),
            //                        new CodePrimitiveExpression(action.Name)
            //                        );
            //                }
            //                ).ToArray()
            //            )
            //        )
            //        );
            //entitySchemaDefClass.Members.Add(staticCtr);
            #endregion конструктор типа

            #region метод public OrmTable GetTypeMainTable(Type type)
            method = new CodeMemberMethod();
            entitySchemaDefClass.Members.Add(method);
            method.Name = "GetTypeMainTable";
            // тип возвращаемого значения
            method.ReturnType = new CodeTypeReference(typeof(OrmTable));
            if (entity.BaseEntity != null)
            {
                method.Attributes |= MemberAttributes.Override;
            }
            // модификаторы доступа
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
                        new CodeCastExpression(new CodeTypeReference(typeof(IDbSchema)), new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_schema")),
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
                    //new CodeArrayIndexerExpression(
                    //    new CodePropertyReferenceExpression(
                    //        new CodeTypeReferenceExpression(
                    //            new CodeTypeReference(GetEntitySchemaDefClassQualifiedName(entity, settings))
                    //        ),
                    //        "_tables"
                    //    ),
                        
                    //)
                //new CodeFieldReferenceExpression(
                //        new CodeTypeReferenceExpression(
                //            new CodeTypeReference(GetEntitySchemaDefClassQualifiedName(entity, settings))
                //        ),
                //        "Tables"
                //)
                )
            );
            #endregion метод public static OrmTable GetMainTable()

            #region поле _idx
            field = new CodeMemberField(new CodeTypeReference(typeof(Worm.Orm.Collections.IndexedCollection<string, Worm.Orm.MapField2Column>)), "_idx");
            entitySchemaDefClass.Members.Add(field);
            #endregion поле _idx

            #region поле _tables
            field = new CodeMemberField(new CodeTypeReference(typeof(OrmTable[])), "_tables");
            field.Attributes = MemberAttributes.Private;
            entitySchemaDefClass.Members.Add(field);
            #endregion поле _tables

            #region // проперти Tables

            //property = new CodeMemberProperty();
            //property.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            //property.HasGet = true;
            //property.HasSet = false;
            //property.GetStatements.Add(
            //        new CodeMethodReturnStatement(
            //            new CodeFieldReferenceExpression(
            //                new CodeTypeReferenceExpression(
            //                    new CodeTypeReference(GetEntitySchemaDefClassQualifiedName(entity, settings))
            //                ),
            //                "_tables"
            //            )
            //        )
            //);
            //property.Type = new CodeTypeReference(typeof (Worm.Orm.OrmTable[]));
            //property.Name = "Tables";
            //entitySchemaDefClass.Members.Add(property);
            #endregion // проперти Tables

            #region // поле private static Dictionary<string,OrmTable> _relationTables

            //field =
            //    new CodeMemberField(new CodeTypeReference(typeof (Hashtable)),
            //                        "_relationTables");
            //field.Attributes = MemberAttributes.Private | MemberAttributes.Static;
            //field.InitExpression = new CodeMethodInvokeExpression(
            //    new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(typeof(Hashtable)), "Synchronized"),
            //    new CodeObjectCreateExpression(field.Type)
            //    );
            //entitySchemaDefClass.Members.Add(field);
            #endregion // поле private Dictionary<string,OrmTable> _relationTables

            #region // метод OrmTable GetRelationTable(string tableName)

            //method = new CodeMemberMethod();
            //entitySchemaDefClass.Members.Add(method);
            //method.Name = "GetRelationTable";
            //method.ReturnType = new CodeTypeReference(typeof (OrmTable));
            //method.Attributes = MemberAttributes.Static | MemberAttributes.Public;
            //method.Parameters.Add(
            //    new CodeParameterDeclarationExpression(
            //        new CodeTypeReference(typeof(string)), 
            //        "tableName"
            //    )
            //);
            //method.Statements.Add(
            //    new CodeConditionStatement(
            //        new CodeBinaryOperatorExpression(
            //            new CodeMethodInvokeExpression(
            //                new CodeFieldReferenceExpression(
            //                    new CodeTypeReferenceExpression(GetEntitySchemaDefClassQualifiedName(entity, settings)),
            //                    "_relationTables"
            //                ),
            //                "ContainsKey",
            //                new CodeArgumentReferenceExpression("tableName")
            //            ),
            //        CodeBinaryOperatorType.ValueEquality,
            //        new CodePrimitiveExpression(false)
            //        ),
            //        new CodeExpressionStatement(
            //            new CodeMethodInvokeExpression(
            //                new CodeFieldReferenceExpression(
            //                    new CodeTypeReferenceExpression(GetEntitySchemaDefClassQualifiedName(entity, settings)),
            //                    "_relationTables"
            //                ),
            //                "Add",
            //                new CodeArgumentReferenceExpression("tableName"),
            //                new CodeObjectCreateExpression(
            //                    new CodeTypeReference(typeof(OrmTable)),
            //                    new CodeArgumentReferenceExpression("tableName")
            //                )
            //            )
            //        )
            //    )
            //);
            //method.Statements.Add(
            //    new CodeMethodReturnStatement(
            //        new CodeCastExpression(method.ReturnType, 
            //            new CodeArrayIndexerExpression(
            //                new CodeFieldReferenceExpression(
            //                    new CodeTypeReferenceExpression(GetEntitySchemaDefClassQualifiedName(entity, settings)),
            //                    "_relationTables"
            //                ),
            //                new CodeArgumentReferenceExpression("tableName")
            //            )
            //        )
            //    )
            //);

            #endregion // метод OrmTable GetRelationTable(string tableName)


            #region метод OrmTable[] GetTables()
            method = new CodeMemberMethod();
            method.StartDirectives.Add(Regions[REGION_BASE_TYPE_RELATED].Start);
            entitySchemaDefClass.Members.Add(method);
            method.Name = "GetTables";
            // тип возвращаемого значения
            method.ReturnType = new CodeTypeReference(typeof(OrmTable[]));
            // модификаторы доступа
            method.Attributes = MemberAttributes.Public;
            if (entity.BaseEntity != null)
            {
                method.Attributes |= MemberAttributes.Override;
            }
            // реализует метод базового класса
            method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
            // параметры
            //...
            // для лока
            CodeMemberField forTablesLockField = new CodeMemberField(
                new CodeTypeReference(typeof (object)),
                "_forTablesLock"
                );
            forTablesLockField.InitExpression = new CodeObjectCreateExpression(forTablesLockField.Type);
            entitySchemaDefClass.Members.Add(forTablesLockField);
            // тело
            method.Statements.Add(
                CodeGenPatterns.CodePatternDoubleCheckLock(
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
            #endregion метод OrmTable[] GetTables()

            #region метод OrmTable GetTable(...)
            method = new CodeMemberMethod();
            entitySchemaDefClass.Members.Add(method);
            method.Name = "GetTable";
            // тип возвращаемого значения
            method.ReturnType = new CodeTypeReference(typeof(OrmTable));
            // модификаторы доступа
            method.Attributes = MemberAttributes.Family;
            if (entity.BaseEntity != null)
            {
                method.Attributes |= MemberAttributes.New;
            }
            // реализует метод базового класса
            // параметры
            method.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(
                        GetEntitySchemaDefClassQualifiedName(entity, settings, false) + ".TablesLink"), "tbl"
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
            #endregion метод OrmTable GetTable(...)
            
            
            #region bool ChangeValueType(ColumnAttribute c, object value, ref object newvalue)
            if (settings.Behaviour != OrmObjectGeneratorBehaviour.PartialObjects && entity.BaseEntity == null)
            {
                method = new CodeMemberMethod();
                entitySchemaDefClass.Members.Add(method);
                method.Name = "ChangeValueType";
                // тип возвращаемого значения
                method.ReturnType = new CodeTypeReference(typeof(bool));
                // модификаторы доступа
                method.Attributes = MemberAttributes.Public;
                // реализует метод базового класса
                method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
                // параметры
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
                methodParam.Direction = FieldDirection.Ref;
                method.Parameters.Add(methodParam);

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
            #endregion bool ChangeValueType(ColumnAttribute c, object value, ref object newvalue)

            #region IList ExternalSort(string sort, SortType sortType, IList objs)
            if (settings.Behaviour != OrmObjectGeneratorBehaviour.PartialObjects && entity.BaseEntity == null)
            {
                method = new CodeMemberMethod();
                entitySchemaDefClass.Members.Add(method);
                method.Name = "ExternalSort";
                // тип возвращаемого значения
                method.ReturnType = new CodeTypeReference(typeof(IList));
                // модификаторы доступа
                method.Attributes = MemberAttributes.Public;
                // реализует метод базового класса
                method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
                // параметры
                method.Parameters.Add(
                    new CodeParameterDeclarationExpression(
                        new CodeTypeReference(typeof(string)),
                        "sort"
                    )
                );
                method.Parameters.Add(
                    new CodeParameterDeclarationExpression(
                        new CodeTypeReference(typeof(SortType)),
                        "sortType"
                    )
                );
                method.Parameters.Add(
                    new CodeParameterDeclarationExpression(
                        new CodeTypeReference(typeof(IList)),
                        "objs"
                    )
                );
                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeArgumentReferenceExpression("objs")
                    )
                );
            }
            #endregion IList ExternalSort(string sort, SortType sortType, IList objs)         

            #region OrmJoin GetJoins(OrmTable left, OrmTable right)
            if (settings.Behaviour != OrmObjectGeneratorBehaviour.PartialObjects && entity.BaseEntity == null)
            {
                method = new CodeMemberMethod();
                entitySchemaDefClass.Members.Add(method);
                method.Name = "GetJoins";
                // тип возвращаемого значения
                method.ReturnType = new CodeTypeReference(typeof(OrmJoin));
                // модификаторы доступа
                method.Attributes = MemberAttributes.Public;
                // реализует метод базового класса
                method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
                // параметры
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
                    if (settings.Behaviour == OrmObjectGeneratorBehaviour.Objects)
                    {
                        method.Statements.Add(
                            new CodeThrowExceptionStatement(
                                new CodeObjectCreateExpression(
                                    typeof (NotImplementedException),
                                    new CodePrimitiveExpression(
                                        "Entity has more then one table: this method must be implemented.")
                                    )
                                )
                            );
                    }
                    else
                    {
                        method.Attributes |= MemberAttributes.Abstract;
                    }
                }
                else
                {
                    method.Statements.Add(
                        new CodeMethodReturnStatement(
                            new CodeDefaultValueExpression(new CodeTypeReference(typeof(OrmJoin)))
                        )
                    );
                } 
            }
            #endregion OrmJoin GetJoins(string left, string right)

            #region ColumnAttribute[] GetSuppressedColumns()
            if (settings.Behaviour != OrmObjectGeneratorBehaviour.PartialObjects && entity.BaseEntity == null)
            {
                method = new CodeMemberMethod();
                entitySchemaDefClass.Members.Add(method);
                method.Name = "GetSuppressedColumns";
                // тип возвращаемого значения
                method.ReturnType = new CodeTypeReference(typeof(ColumnAttribute[]));
                // модификаторы доступа
                method.Attributes = MemberAttributes.Public;
                // реализует метод базового класса
                method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeArrayCreateExpression(
                            new CodeTypeReference(typeof(ColumnAttribute[]))
                        )
                    )
                );
            }
            #endregion ColumnAttribute[] GetSuppressedColumns()

            #region IOrmFilter GetFilter(object filter_info)
            if (settings.Behaviour != OrmObjectGeneratorBehaviour.PartialObjects && entity.BaseEntity == null)
            {
                method = new CodeMemberMethod();
                entitySchemaDefClass.Members.Add(method);
                method.Name = "GetFilter";
                // тип возвращаемого значения
                method.ReturnType = new CodeTypeReference(typeof(IOrmFilter));
                // модификаторы доступа
                method.Attributes = MemberAttributes.Public;
                method.Parameters.Add(
                    new CodeParameterDeclarationExpression(
                        new CodeTypeReference(typeof(object)),
                        "filter_info"
                    )
                );
                // реализует метод базового класса
                method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodePrimitiveExpression(null)
                    )
                ); 
            }

            #endregion IOrmFilter GetFilter(object filter_info)

            #region string MapSort2FieldName(string )

            if (settings.Behaviour != OrmObjectGeneratorBehaviour.PartialObjects && entity.BaseEntity == null)
            {
                method = new CodeMemberMethod();
                entitySchemaDefClass.Members.Add(method);
                method.Name = "MapSort2FieldName";
                // тип возвращаемого значения
                method.ReturnType = new CodeTypeReference(typeof(string));
                // модификаторы доступа
                method.Attributes = MemberAttributes.Public;
                method.Parameters.Add(
                    new CodeParameterDeclarationExpression(
                        new CodeTypeReference(typeof(string)),
                        "sort"
                    )
                );
                // реализует метод базового класса
                method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodePrimitiveExpression(null)
                    )
                );
            }

            #endregion string MapSort2FieldName(string sort)

            #region bool get_IsExternalSort(string sort)
            if (settings.Behaviour != OrmObjectGeneratorBehaviour.PartialObjects && entity.BaseEntity == null)
            {
                if (Convert.ToBoolean(settings.LanguageSpecificHacks & LanguageSpecificHacks.MethodsInsteadParametrizedProperties))
                {
                    method = new CodeMemberMethod();
                    entitySchemaDefClass.Members.Add(method);
                    method.Name = "get_IsExternalSort";
                    // тип возвращаемого значения
                    method.ReturnType = new CodeTypeReference(typeof(bool));
                    // модификаторы доступа
                    method.Attributes = MemberAttributes.Public;
                    method.Parameters.Add(
                        new CodeParameterDeclarationExpression(
                            new CodeTypeReference(typeof(string)),
                            "sort"
                            )
                        );
                    // реализует метод базового класса
                    method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
                    method.Statements.Add(
                        new CodeMethodReturnStatement(
                            new CodePrimitiveExpression(false)
                            )
                        );
                }
                else
                {
                    CodeMemberProperty property = new CodeMemberProperty();
                    entitySchemaDefClass.Members.Add(property);
                    property.Name = "IsExternalSort";
                    // тип возвращаемого значения
                    property.Type = new CodeTypeReference(typeof(bool));
                    // модификаторы доступа
                    property.Attributes = MemberAttributes.Public;
                    property.Parameters.Add(
                        new CodeParameterDeclarationExpression(
                            new CodeTypeReference(typeof(string)),
                            "sort"
                            )
                        );
                    // реализует метод базового класса
                    property.ImplementationTypes.Add(typeof(IOrmObjectSchema));
                    property.GetStatements.Add(
                        new CodeMethodReturnStatement(
                            new CodePrimitiveExpression(false)
                            )
                        );    
                }
            }
            #endregion bool get_IsExternalSort(string sort)

            #region сущность имеет связи "многие ко многим"

            List<RelationDescription> usedM2MRelation;
            // список релейшенов относящихся к данной сущности
            usedM2MRelation = _ormObjectsDefinition.Relations.FindAll(
                delegate(RelationDescription match)
                {
                    return match.Left.Entity == entity || match.Right.Entity == entity;
                }
            );

            #region поле _m2mRelations
            field = new CodeMemberField(new CodeTypeReference(typeof(M2MRelation[])), "_m2mRelations");
            entitySchemaDefClass.Members.Add(field);
            #endregion поле _m2mRelations
            #region метод M2MRelation[] GetM2MRelations()
            method = new CodeMemberMethod();
            entitySchemaDefClass.Members.Add(method);
            method.Name = "GetM2MRelations";
            // тип возвращаемого значения
            method.ReturnType = new CodeTypeReference(typeof(M2MRelation[]));
            // модификаторы доступа
            method.Attributes = MemberAttributes.Public;
            if(entity.BaseEntity !=null)
            {
                method.Attributes |= MemberAttributes.Override;
            }
            // реализует метод базового класса
            method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
            // параметры
            //...
            // для лока
            CodeMemberField forM2MRelationsLockField = new CodeMemberField(
                new CodeTypeReference(typeof(object)),
                "_forM2MRelationsLock"
                );
            forM2MRelationsLockField.InitExpression = new CodeObjectCreateExpression(forM2MRelationsLockField.Type);
            entitySchemaDefClass.Members.Add(forM2MRelationsLockField);
            // тело
            CodeExpression condition = new CodeBinaryOperatorExpression(
                new CodeFieldReferenceExpression(
                    new CodeThisReferenceExpression(),
                    "_m2mRelations"
                    ),
                CodeBinaryOperatorType.IdentityEquality,
                new CodePrimitiveExpression(null)
                );
            CodeStatementCollection inlockStatemets = new CodeStatementCollection();
            inlockStatemets.Add(new CodeVariableDeclarationStatement(
                                    method.ReturnType,
                                    "m2mRelations",
                                    new CodeArrayCreateExpression(
                                        new CodeTypeReference(typeof (M2MRelation[])),
                                        usedM2MRelation.ConvertAll<CodeExpression>(delegate(RelationDescription action)
                                                                                   {
                                                                                       return
                                                                                           new CodeObjectCreateExpression
                                                                                               (
                                                                                               new CodeTypeReference(
                                                                                                   typeof (M2MRelation)),
                                                                                               new CodeMethodInvokeExpression
                                                                                                   (
                                                                                                   new CodeMethodReferenceExpression
                                                                                                       (
                                                                                                       new CodeFieldReferenceExpression
                                                                                                           (
                                                                                                           new CodeThisReferenceExpression
                                                                                                               (),
                                                                                                           "_schema"
                                                                                                           ),
                                                                                                       "GetTypeByEntityName"
                                                                                                       ),
                                                                                                   new CodePrimitiveExpression
                                                                                                       (
                                                                                                       (action.Left.
                                                                                                            Entity ==
                                                                                                        entity)
                                                                                                           ?
                                                                                                       action.Right.
                                                                                                           Entity.Name
                                                                                                           :
                                                                                                       action.Left.
                                                                                                           Entity.Name
                                                                                                       )
                                                                                                   ),
                                                                                               action.UnderlyingEntity ==
                                                                                               null
                                                                                                   ?
                                                                                               new CodeMethodInvokeExpression
                                                                                                   (
                                                                                                   new CodeCastExpression
                                                                                                       (
                                                                                                       new CodeTypeReference
                                                                                                           (typeof (
                                                                                                                IDbSchema
                                                                                                                )),
                                                                                                       new CodeFieldReferenceExpression
                                                                                                           (
                                                                                                           new CodeThisReferenceExpression
                                                                                                               (),
                                                                                                           "_schema"
                                                                                                           )
                                                                                                       ),
                                                                                                   "GetSharedTable",
                                                                                                   new CodePrimitiveExpression
                                                                                                       (action.Table.
                                                                                                            Name)
                                                                                                   )
                                                                                                   :
                                                                                               new CodeMethodInvokeExpression
                                                                                                   (
                                                                                                   new CodeThisReferenceExpression
                                                                                                       (),
                                                                                                   "GetTypeMainTable",
                                                                                                   new CodeMethodInvokeExpression
                                                                                                       (
                                                                                                       new CodeFieldReferenceExpression
                                                                                                           (
                                                                                                           new CodeThisReferenceExpression
                                                                                                               (),
                                                                                                           "_schema"
                                                                                                           ),
                                                                                                       "GetTypeByEntityName",
                                                                                                       new CodePrimitiveExpression
                                                                                                           (action.
                                                                                                                UnderlyingEntity
                                                                                                                .Name)
                                                                                                       )
                                                                                                   ),
                                                                                               new CodePrimitiveExpression
                                                                                                   ((action.Left.Entity ==
                                                                                                     entity)
                                                                                                        ? action.Right.
                                                                                                              FieldName
                                                                                                        : action.Left.
                                                                                                              FieldName),
                                                                                               new CodePrimitiveExpression
                                                                                                   ((action.Left.Entity ==
                                                                                                     entity)
                                                                                                        ? action.Right.
                                                                                                              CascadeDelete
                                                                                                        : action.Left.
                                                                                                              CascadeDelete),
                                                                                               new CodeObjectCreateExpression
                                                                                                   (new CodeTypeReference
                                                                                                        (typeof (
                                                                                                             DataTableMapping
                                                                                                             ))),
                                                                                               (action.UnderlyingEntity !=
                                                                                                null)
                                                                                                   ?
                                                                                               (CodeExpression)
                                                                                               new CodeMethodInvokeExpression
                                                                                                   (
                                                                                                   new CodeMethodReferenceExpression
                                                                                                       (
                                                                                                       new CodeFieldReferenceExpression
                                                                                                           (
                                                                                                           new CodeThisReferenceExpression
                                                                                                               (),
                                                                                                           "_schema"
                                                                                                           ),
                                                                                                       "GetTypeByEntityName"
                                                                                                       ),
                                                                                                   new CodePrimitiveExpression
                                                                                                       (action.
                                                                                                            UnderlyingEntity
                                                                                                            .Name)
                                                                                                   )
                                                                                                   :
                                                                                               new CodeCastExpression(
                                                                                                   new CodeTypeReference
                                                                                                       (typeof (Type)),
                                                                                                   (CodeExpression)
                                                                                                   new CodePrimitiveExpression
                                                                                                       (null))
                                                                                               );
                                                                                   }
                                            ).ToArray()
                                        )
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
                CodeGenPatterns.CodePatternDoubleCheckLock(
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
            #endregion метод string[] GetTables()

            #endregion сущность имеет связи "многие ко многим"

            #region Worm.Orm.Collections.IndexedCollection<string, Worm.Orm.MapField2Column> GetFieldColumnMap()
            method = new CodeMemberMethod();
            method.EndDirectives.Add(Regions[REGION_BASE_TYPE_RELATED].End);
            entitySchemaDefClass.Members.Add(method);
            method.Name = "GetFieldColumnMap";
            // тип возвращаемого значения
            method.ReturnType = new CodeTypeReference(typeof(IndexedCollection<string, MapField2Column>));
            // модификаторы доступа
            method.Attributes = MemberAttributes.Public;
            if(entity.BaseEntity != null)
            {
                method.Attributes |= MemberAttributes.Override;
            }
            // реализует метод базового класса
            method.ImplementationTypes.Add(new CodeTypeReference(typeof (Worm.Orm.IOrmObjectSchema)));
            // параметры
            //...
            // для лока
            CodeMemberField forIdxLockField = new CodeMemberField(
                new CodeTypeReference(typeof(object)),
                "_forIdxLock"
            );
            forIdxLockField.InitExpression = new CodeObjectCreateExpression(forIdxLockField.Type);
            entitySchemaDefClass.Members.Add(forIdxLockField);
            List<CodeStatement> condTrueStatements = new List<CodeStatement>();
            condTrueStatements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference(typeof(Worm.Orm.Collections.IndexedCollection<string, Worm.Orm.MapField2Column>)),
                    "idx",
                    (entity.BaseEntity == null) ? 
                    (CodeExpression)
                    new CodeObjectCreateExpression(
                        new CodeTypeReference(typeof(OrmObjectIndex))
                    )
                    :
                    (CodeExpression)
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
                                                                        new CodeObjectCreateExpression(
                                                                            new CodeTypeReference(
                                                                                typeof (MapField2Column)),
                                                                            new CodePrimitiveExpression(action.Name),
                                                                            new CodePrimitiveExpression(action.FieldName),
                                                                            //(OrmTable)this.GetTables().GetValue((int)(XMedia.Framework.Media.Objects.ArtistBase.ArtistBaseSchemaDef.TablesLink.tblArtists)))
                                                                            new CodeMethodInvokeExpression(
                                                                                new CodeThisReferenceExpression(),
                                                                                "GetTable",
                                                                                new CodeFieldReferenceExpression(
                                                                                        new CodeTypeReferenceExpression(GetEntitySchemaDefClassQualifiedName
                                                                                                                            (entity
                                                                                                                             ,
                                                                                                                             settings, false) +
                                                                                                                        ".TablesLink"),
                                                                                        GetSafeName(action.Table.Identifier)
                                                                                        )
                                                                            )
                                                                            //new CodeCastExpression(
                                                                            //    new CodeTypeReference(typeof(Worm.Orm.OrmTable)),
                                                                            //    new CodeMethodInvokeExpression(
                                                                            //    new CodeMethodInvokeExpression(
                                                                            //        new CodeThisReferenceExpression(),
                                                                            //        "GetTables"
                                                                            //    ) ,"GetValue",
                                                                            //    new CodeCastExpression(
                                                                            //        new CodeTypeReference(typeof (int)),
                                                                            //        new CodeFieldReferenceExpression(
                                                                            //            new CodeTypeReferenceExpression(GetEntitySchemaDefClassQualifiedName
                                                                            //                                                (entity
                                                                            //                                                 ,
                                                                            //                                                 settings) +
                                                                            //                                            ".TablesLink"),
                                                                            //            action.Table.Identifier
                                                                            //            )
                                                                            //        )
                                                                            //    )
                                                                            //)
                                                                        )
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
                CodeGenPatterns.CodePatternDoubleCheckLock(
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

            #region сущность реализует связь
            RelationDescription relation;
            relation = _ormObjectsDefinition.Relations.Find(
                        delegate(RelationDescription match)
                        {
                            return match.UnderlyingEntity == entity;
                        }
                    );
            if (relation != null)
            {
                entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(typeof(IRelation)));
                #region Pair<string, Type> GetFirstType()
                method = new CodeMemberMethod();
                method.StartDirectives.Add(Regions["IRelation Members"].Start);
                entitySchemaDefClass.Members.Add(method);
                method.Name = "GetFirstType";
                // тип возвращаемого значения
                method.ReturnType = new CodeTypeReference(typeof(Pair<string, Type>));
                // модификаторы доступа
                method.Attributes = MemberAttributes.Public;
                // реализует метод базового класса
                method.ImplementationTypes.Add(typeof(IRelation));
                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeObjectCreateExpression(
                            new CodeTypeReference(typeof(Pair<string, Type>)),
                            new CodePrimitiveExpression(entity.Properties.Find(delegate(PropertyDescription match) {return match.FieldName == relation.Left.FieldName;}).Name),
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
                method.EndDirectives.Add(Regions["IRelation Members"].End);
                entitySchemaDefClass.Members.Add(method);
                method.Name = "GetSecondType";
                // тип возвращаемого значения
                method.ReturnType = new CodeTypeReference(typeof(Pair<string, Type>));
                // модификаторы доступа
                method.Attributes = MemberAttributes.Public;
                // реализует метод базового класса
                method.ImplementationTypes.Add(typeof(IRelation));
                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeObjectCreateExpression(
                            new CodeTypeReference(typeof(Pair<string, Type>)),
                            new CodePrimitiveExpression(entity.CompleteEntity.Properties.Find(delegate(PropertyDescription match) { return match.FieldName == relation.Right.FieldName; }).Name),
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
            #endregion сущность реализует связь

            #region public void GetSchema(OrmSchemaBase schema, Type t)

            CodeMemberField schemaField = new CodeMemberField(
                new CodeTypeReference(typeof (OrmSchemaBase)),
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
            // тип возвращаемого значения
            method.ReturnType = null;
            // модификаторы доступа
            method.Attributes = MemberAttributes.Public;
            if (entity.BaseEntity != null)
            {
                method.Attributes |= MemberAttributes.Override;
            }
            method.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(typeof(OrmSchemaBase)),
                    "schema"
                )
            );
            method.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(typeof(Type)),
                    "t"
                )
                );
            // реализует метод базового класса
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
            if (entity.BaseEntity != null)
                method.Statements.Add(
                    new CodeMethodInvokeExpression(
                        new CodeBaseReferenceExpression(),
                        "GetSchema",
                        new CodeArgumentReferenceExpression("schema"),
                        new CodeArgumentReferenceExpression("t")
                    )
                );
            #endregion public void GetSchema(OrmSchemaBase schema, Type t)

            if (createobjectMethod.Statements.Count == 0)
                entityClass.Members.Remove(createobjectMethod);
            if (setvalueMethod.Statements.Count == 0)
                entityClass.Members.Remove(setvalueMethod);

            SetRegions(entityClass);

            foreach (CodeCompileUnit compileUnit in result.Values)
            {
                if(Convert.ToBoolean(settings.LanguageSpecificHacks & LanguageSpecificHacks.AddOptionsExplicit))
                    compileUnit.UserData.Add("RequireVariableDeclaration", Convert.ToBoolean(settings.LanguageSpecificHacks & LanguageSpecificHacks.OptionsExplicitOn));
                if (Convert.ToBoolean(settings.LanguageSpecificHacks & LanguageSpecificHacks.AddOptionsStrict))
                    compileUnit.UserData.Add("AllowLateBound", !Convert.ToBoolean(settings.LanguageSpecificHacks & LanguageSpecificHacks.OptionsExplicitOn));

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
            }

            return result;
        }

        private string GetSafeName(string p)
        {
            Regex regex = new Regex("\\W");
            return regex.Replace(p, "_");
        }

        private static CodeExpression GetPropAttributesEnumValues(string[] attrs)
        {
            List<String> attrsList = new List<string>(attrs);
            CodeExpression first;
            first = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(Field2DbRelations)), attrsList.Count == 0 ? Field2DbRelations.None.ToString() : attrsList[0]);
            if (attrsList.Count > 1)
                return new CodeBinaryOperatorExpression(first, CodeBinaryOperatorType.BitwiseOr, GetPropAttributesEnumValues(attrsList.GetRange(1, attrsList.Count - 1).ToArray()));
            else
                return first;
        }

        private void SetRegions(CodeTypeDeclaration typeDeclaration)
        {
            List<CodeTypeMember> members = new List<CodeTypeMember>(typeDeclaration.Members.Count);

            foreach (CodeTypeMember mem in typeDeclaration.Members)
            {
                members.Add(mem);
            }

            CodeTypeMember member;
            member = members.Find(IsCodeMemeberField);
            if (member != null)
                member.StartDirectives.Add(Regions[REGION_PRIVATE_FIELDS].Start);
            member = members.FindLast(IsCodeMemeberField);
            if (member != null)
                member.EndDirectives.Add(Regions[REGION_PRIVATE_FIELDS].End);

            member = members.Find(IsCodeMemeberProperty);
            if (member != null)
                member.StartDirectives.Add(Regions[REGION_PROPERTIES].Start);
            member = members.FindLast(IsCodeMemeberProperty);
            if (member != null)
                member.EndDirectives.Add(Regions[REGION_PROPERTIES].End);

            member = members.Find(IsCodeConstructor);
            if (member != null)
                member.StartDirectives.Add(Regions[REGION_CONSTRUCTORS].Start);
            member = members.FindLast(IsCodeConstructor);
            if (member != null)
                member.EndDirectives.Add(Regions[REGION_CONSTRUCTORS].End);

            member = members.Find(IsCodeMemberNestedTypes);
            if (member != null)
                member.StartDirectives.Add(Regions[REGION_NESTED_TYPES].Start);
            member = members.FindLast(IsCodeMemberNestedTypes);
            if (member != null)
                member.EndDirectives.Add(Regions[REGION_NESTED_TYPES].End);

            members.FindAll(IsCodeMemberNestedTypes).ForEach(
                delegate(CodeTypeMember action)
                {
                    SetRegions(action as CodeTypeDeclaration);
                }
            );

            members.FindAll(IsCodeMemeberProperty).ForEach(SetSignatureRegion);
        }
        private static bool IsCodeMemeberField(CodeTypeMember match)
        {
            return match is CodeMemberField;
        }

        private static bool IsCodeMemeberProperty(CodeTypeMember match)
        {
            return match is CodeMemberProperty;
        }

        private static bool IsCodeMemberNestedTypes(CodeTypeMember match)
        {
            return match is CodeTypeDeclaration;
        }

        private static bool IsCodeConstructor(CodeTypeMember match)
        {
            return match is CodeConstructor;
        }

        private static void SetSignatureRegion(CodeTypeMember action)
        {

        }

        private static void SetMemberDescription(CodeTypeMember member, string description)
        {
            if (string.IsNullOrEmpty(description))
                return;
            member.Comments.Add(new CodeCommentStatement(string.Format("<summary>\n{0}\n</summary>", description), true));
        }

        /// <summary>
        /// Полное имя сущности которая должна быть сгенерированна
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private string GetQualifiedEntityName(EntityDescription entity, OrmCodeDomGeneratorSettings settings, bool final)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(entity.OrmObjectsDef.Namespace))
                result += entity.OrmObjectsDef.Namespace;
            if (!string.IsNullOrEmpty(entity.Namespace))
                result += (string.IsNullOrEmpty(result) ? string.Empty : ".") + entity.Name;
            result += (string.IsNullOrEmpty(result) ? string.Empty : ".") + GetEntityClassName(entity, settings, final);
            return result;
        }

        private string GetEntityFileName(EntityDescription entity, OrmCodeDomGeneratorSettings settings)
        {
            if (settings.Behaviour == OrmObjectGeneratorBehaviour.PartialObjects)
                return settings.PartialAutoGeneratedFilePrefix + GetEntityClassName(entity, settings, false);
            else
                return GetEntityClassName(entity, settings, false);
        }

        private string GetEntitySchemaDefFileName(EntityDescription entity, OrmCodeDomGeneratorSettings settings)
        {
            if (settings.Behaviour == OrmObjectGeneratorBehaviour.PartialObjects)
                return settings.PartialAutoGeneratedFilePrefix + GetEntitySchemaDefClassName(entity, settings, false);
            else
                return GetEntitySchemaDefClassName(entity, settings, false);
        }

        /// <summary>
        /// Имя класа сущности которая должна быть сгенерированна
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private string GetEntityClassName(EntityDescription entity, OrmCodeDomGeneratorSettings settings, bool final)
        {
            if (settings.Behaviour == OrmObjectGeneratorBehaviour.BaseObjects && !final)
                return entity.Name + settings.BaseClassNameSuffix;
            else
                return entity.Name;
        }

        private string GetEntitySchemaDefClassName(EntityDescription entity, OrmCodeDomGeneratorSettings settings, bool final)
        {
            return GetEntityClassName(entity, settings, final) + settings.EntitySchemaDefClassNameSuffix;
        }

        private string GetEntitySchemaDefClassQualifiedName(EntityDescription entity, OrmCodeDomGeneratorSettings settings, bool final)
        {
            return string.Format("{0}.{1}", GetQualifiedEntityName(entity, settings, final), GetEntitySchemaDefClassName(entity, settings, final));
        }


        private static string GetPrivateMemberName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;
            return "m_" + name.Substring(0, 1).ToLower() + name.Substring(1);
        }
    }
}
