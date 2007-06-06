using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using OrmCodeGenLib.Descriptors;
using Worm.Orm;
using Worm.Orm.Collections;
using XMedia.Framework;
using System.Text.RegularExpressions;
using System.Text;

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
            result.Add(OrmCodeGenNameHelper.GetEntityFileName(entity, settings), entityUnit);

            // неймспейс
            nameSpace = new CodeNamespace(entity.Namespace);
            entityUnit.Namespaces.Add(nameSpace);

            // импорты
            //nameSpace.Imports.Add(new CodeNamespaceImport("System"));
            //nameSpace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            //nameSpace.Imports.Add(new CodeNamespaceImport("Worm.Orm"));

            // класс сущности
            entityClass = new CodeTypeDeclaration(OrmCodeGenNameHelper.GetEntityClassName(entity, settings));
            nameSpace.Types.Add(entityClass);

            // параметры класса
            entityClass.IsClass = true;
            entityClass.IsPartial = entity.Behaviour == EntityBehaviuor.PartialObjects || entity.Behaviour == EntityBehaviuor.ForcePartial || settings.Split;
            entityClass.Attributes = MemberAttributes.Public;
            entityClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.Public;

            //if (entity.Behaviour == EntityBehaviuor.Abstract)
            //{
            //    entityClass.Attributes |= MemberAttributes.Abstract;
            //    entityClass.TypeAttributes |= TypeAttributes.Abstract;
            //}

            // дескрипшн
            SetMemberDescription(entityClass, entity.Description);

            // базовый класс
            if(entity.BaseEntity == null)
            {
                //entityClass.BaseTypes.Add(new CodeTypeReference(typeof(OrmBaseT)));
                CodeTypeReference entityType = new CodeTypeReference(typeof (Worm.Orm.OrmBaseT<>));
                entityType.TypeArguments.Add(
                    new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity, settings)));
                entityClass.BaseTypes.Add(entityType);
            }
            
            else
                entityClass.BaseTypes.Add(new CodeTypeReference(OrmCodeGenNameHelper.GetQualifiedEntityName(entity.BaseEntity, settings, false)));

            CodeTypeReference iOrmEditableType = new CodeTypeReference(typeof(IOrmEditable<>));
            iOrmEditableType.TypeArguments.Add(
                new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity, settings)));

            entityClass.BaseTypes.Add(iOrmEditableType);

            #endregion определение класса сущности

            #region определение схемы
            entitySchemaDefClass = new CodeTypeDeclaration(OrmCodeGenNameHelper.GetEntitySchemaDefClassName(entity, settings, false));

            entitySchemaDefClass.IsClass = true;
            entitySchemaDefClass.IsPartial = entityClass.IsPartial;
            entitySchemaDefClass.Attributes = entityClass.Attributes;
            entitySchemaDefClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.NestedPublic;

            #endregion определение схемы

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
                                        new CodeTypeReference(OrmCodeGenNameHelper.GetEntitySchemaDefClassQualifiedName(entity, settings, false))
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
            //}

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

            #region метод OrmBase.CopyBody(CopyBody(...)
            CodeMemberMethod copyMethod;
            copyMethod = new CodeMemberMethod();
            entityClass.Members.Add(copyMethod);
            copyMethod.Name = "CopyBody";
            // тип возвращаемого значения
            copyMethod.ReturnType = null;
            // модификаторы доступа
            copyMethod.Attributes = MemberAttributes.Public;
            //if (entity.BaseEntity != null)
            //    copyMethod.Attributes |= MemberAttributes.Override;
            copyMethod.ImplementationTypes.Add(iOrmEditableType);
            copyMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity, settings)), "from"));
            copyMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity, settings)), "to"));
            if (entity.BaseEntity != null)
                copyMethod.Statements.Add(
                    new CodeMethodInvokeExpression(
                        new CodeBaseReferenceExpression(),
                        "CopyBody",
                        new CodeArgumentReferenceExpression("from"),
                        new CodeArgumentReferenceExpression("to")
                    )
                );
            #endregion метод OrmBase.CopyBody(CopyBody(OrmBase from, OrmBase to)

            #region // метод IComparer<T> OrmBase.CreateSortComparer<T>(string sort, SortType sortType)
            //if (settings.Behaviour != OrmObjectGeneratorBehaviour.PartialObjects && entity.BaseEntity == null)
            //{
            //    method = new CodeMemberMethod();
            //    entityClass.Members.Add(method);
            //    method.Name = "CreateSortComparer";
            //    // generic параметр
            //    CodeTypeParameter prm = new CodeTypeParameter("T");
            //    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.DerivedGenericMembersRequireConstraits) == LanguageSpecificHacks.DerivedGenericMembersRequireConstraits)
            //    {
            //        prm.Constraints.Add(new CodeTypeReference(typeof (OrmBase)));
            //        prm.HasConstructorConstraint = true;
            //    }
            //    method.TypeParameters.Add(prm);
                
            //    // тип возвращаемого значения
            //    CodeTypeReference methodReturnType;
            //    methodReturnType = new CodeTypeReference();
            //    methodReturnType.BaseType = "System.Collections.Generic.IComparer";
            //    methodReturnType.Options = CodeTypeReferenceOptions.GenericTypeParameter;
            //    methodReturnType.TypeArguments.Add("T");
            //    method.ReturnType = methodReturnType;
            //    // модификаторы доступа
            //    method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            //    // реализует метод базового класса
                
            //    // параметры
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
            #endregion // метод IComparer<T> OrmBase.CreateSortComparer<T>(string sort, SortType sortType)

            #region // метод IComparer OrmBase.CreateSortComparer(string sort, SortType sortType)
            //if (settings.Behaviour != OrmObjectGeneratorBehaviour.PartialObjects &&  entity.BaseEntity == null)
            //{
            //    method = new CodeMemberMethod();
            //    entityClass.Members.Add(method);
            //    method.Name = "CreateSortComparer";
            //    // тип возвращаемого значения
            //    method.ReturnType = new CodeTypeReference(typeof(IComparer));
            //    // модификаторы доступа
            //    method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            //    // реализует метод базового класса
                
            //    // параметры
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
            #endregion // метод IComparer CreateSortComparer(string sort, SortType sortType)

            #region void SetValue(System.Reflection.PropertyInfo pi, ColumnAttribute c, object value)

            CodeMemberMethod setvalueMethod = CreateSetValueMethod(entityClass);

            #endregion void SetValue(System.Reflection.PropertyInfo pi, ColumnAttribute c, object value)

            #region void CreateObject(string fieldName, object value)
            
            CodeMemberMethod createobjectMethod = CreateCreateObjectMethod(entity, entityClass, settings);

            #endregion void CreateObject(string fieldName, object value)

            #region // метод OrmBase GetNew()
            ////if (settings.Behaviour != OrmObjectGeneratorBehaviour.BaseObjects)
            ////{
            //    method = new CodeMemberMethod();
            //    entityClass.Members.Add(method);
            //    method.Name = "GetNew";
            //    // тип возвращаемого значения
            //    method.ReturnType = new CodeTypeReference(typeof (OrmBase));
            //    // модификаторы доступа
            //    method.Attributes = MemberAttributes.Family | MemberAttributes.Override;
            //    // реализует метод базового класса

            //    // параметры
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

            #endregion // метод OrmBase GetNew()

            #region проперти

            CreateProperties(createobjectMethod, entity, entityClass, settings, setvalueMethod, copyMethod);

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

            ProcessSplitOption(entity, entityClass, ref entitySchemaDefClass, result, settings);

            #endregion обработка директивы Split



            #region энам табличек

            CreateTablesLinkEnum(entity, entitySchemaDefClass);

            #endregion энам табличек

            #region метод public OrmTable GetTypeMainTable(Type type)

            CreateGetTypeMainTableMethod(entity, entitySchemaDefClass);

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

            CreateGetTablesMethod(entity, entitySchemaDefClass);

            #endregion метод OrmTable[] GetTables()

            #region метод OrmTable GetTable(...)

            CreateGetTableMethod(entity, entitySchemaDefClass, settings);

            #endregion метод OrmTable GetTable(...)
            
            
            #region bool ChangeValueType(ColumnAttribute c, object value, ref object newvalue)

            CreateChangeValueTypeMethod(entity, entitySchemaDefClass, settings);

            #endregion bool ChangeValueType(ColumnAttribute c, object value, ref object newvalue)

            #region // IList ExternalSort(string sort, SortType sortType, IList objs)
            //if (settings.Behaviour != OrmObjectGeneratorBehaviour.PartialObjects && entity.BaseEntity == null)
            //{
            //    method = new CodeMemberMethod();
            //    entitySchemaDefClass.Members.Add(method);
            //    method.Name = "ExternalSort";
            //    // тип возвращаемого значения
            //    method.ReturnType = new CodeTypeReference(typeof(IList));
            //    // модификаторы доступа
            //    method.Attributes = MemberAttributes.Public;
            //    // реализует метод базового класса
            //    method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
            //    // параметры
            //    method.Parameters.Add(
            //        new CodeParameterDeclarationExpression(
            //            new CodeTypeReference(typeof(string)),
            //            "sort"
            //        )
            //    );
            //    method.Parameters.Add(
            //        new CodeParameterDeclarationExpression(
            //            new CodeTypeReference(typeof(SortType)),
            //            "sortType"
            //        )
            //    );
            //    method.Parameters.Add(
            //        new CodeParameterDeclarationExpression(
            //            new CodeTypeReference(typeof(IList)),
            //            "objs"
            //        )
            //    );
            //    method.Statements.Add(
            //        new CodeMethodReturnStatement(
            //            new CodeArgumentReferenceExpression("objs")
            //        )
            //    );
            //}
            #endregion // IList ExternalSort(string sort, SortType sortType, IList objs)         

            #region OrmJoin GetJoins(OrmTable left, OrmTable right)
            if ((entity.Behaviour != EntityBehaviuor.PartialObjects || entity.Tables.Count == 1) && entity.BaseEntity == null)
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
                    if (entity.Behaviour != EntityBehaviuor.PartialObjects)
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
            if (entity.Behaviour != EntityBehaviuor.PartialObjects && entity.BaseEntity == null)
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
            if (entity.Behaviour != EntityBehaviuor.PartialObjects && entity.BaseEntity == null)
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

            #region // string MapSort2FieldName(string )

            //if (settings.Behaviour != OrmObjectGeneratorBehaviour.PartialObjects && entity.BaseEntity == null)
            //{
            //    method = new CodeMemberMethod();
            //    entitySchemaDefClass.Members.Add(method);
            //    method.Name = "MapSort2FieldName";
            //    // тип возвращаемого значения
            //    method.ReturnType = new CodeTypeReference(typeof(string));
            //    // модификаторы доступа
            //    method.Attributes = MemberAttributes.Public;
            //    method.Parameters.Add(
            //        new CodeParameterDeclarationExpression(
            //            new CodeTypeReference(typeof(string)),
            //            "sort"
            //        )
            //    );
            //    // реализует метод базового класса
            //    method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
            //    method.Statements.Add(
            //        new CodeMethodReturnStatement(
            //            new CodePrimitiveExpression(null)
            //        )
            //    );
            //}

            #endregion // string MapSort2FieldName(string sort)

            #region // bool get_IsExternalSort(string sort)
            //if (settings.Behaviour != OrmObjectGeneratorBehaviour.PartialObjects && entity.BaseEntity == null)
            //{
            //    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.MethodsInsteadParametrizedProperties) == LanguageSpecificHacks.MethodsInsteadParametrizedProperties)
            //    {
            //        method = new CodeMemberMethod();
            //        entitySchemaDefClass.Members.Add(method);
            //        method.Name = "get_IsExternalSort";
            //        // тип возвращаемого значения
            //        method.ReturnType = new CodeTypeReference(typeof(bool));
            //        // модификаторы доступа
            //        method.Attributes = MemberAttributes.Public;
            //        method.Parameters.Add(
            //            new CodeParameterDeclarationExpression(
            //                new CodeTypeReference(typeof(string)),
            //                "sort"
            //                )
            //            );
            //        // реализует метод базового класса
            //        method.ImplementationTypes.Add(typeof(IOrmObjectSchema));
            //        method.Statements.Add(
            //            new CodeMethodReturnStatement(
            //                new CodePrimitiveExpression(false)
            //                )
            //            );
            //    }
            //    else
            //    {
            //        CodeMemberProperty property = new CodeMemberProperty();
            //        entitySchemaDefClass.Members.Add(property);
            //        property.Name = "IsExternalSort";
            //        // тип возвращаемого значения
            //        property.Type = new CodeTypeReference(typeof(bool));
            //        // модификаторы доступа
            //        property.Attributes = MemberAttributes.Public;
            //        property.Parameters.Add(
            //            new CodeParameterDeclarationExpression(
            //                new CodeTypeReference(typeof(string)),
            //                "sort"
            //                )
            //            );
            //        // реализует метод базового класса
            //        property.ImplementationTypes.Add(typeof(IOrmObjectSchema));
            //        property.GetStatements.Add(
            //            new CodeMethodReturnStatement(
            //                new CodePrimitiveExpression(false)
            //                )
            //            );    
            //    }
            //}
            #endregion bool get_IsExternalSort(string sort)

            #region сущность имеет связи "многие ко многим"

            List<RelationDescription> usedM2MRelation;
            // список релейшенов относящихся к данной сущности
            usedM2MRelation = entity.GetRelations();

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
            CodeArrayCreateExpression m2mArrayCreationExpression = new CodeArrayCreateExpression(
                new CodeTypeReference(typeof(M2MRelation[]))
                );
            foreach (RelationDescription relationDescription in usedM2MRelation)
            {
                m2mArrayCreationExpression.Initializers.AddRange(
                    GetM2MRelationCreationExpressions(relationDescription, entity));
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

            #region метод получения связанных сущностей

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
            #endregion метод получения связанных сущностей

            #endregion сущность имеет связи "многие ко многим"

            #region Worm.Orm.Collections.IndexedCollection<string, Worm.Orm.MapField2Column> GetFieldColumnMap()
            method = new CodeMemberMethod();
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
                                                                        GetMapField2ColumObjectCreationExpression(entity, action, settings)
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
                //method.StartDirectives.Add(Regions["IRelation Members"].Start);
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
                            new CodePrimitiveExpression(entity.Properties.Find(delegate(PropertyDescription match) {return match.FieldName == relation.Left.FieldName;}).PropertyAlias),
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
                            new CodePrimitiveExpression(entity.CompleteEntity.Properties.Find(delegate(PropertyDescription match) { return match.FieldName == relation.Right.FieldName; }).PropertyAlias),
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

            if (createobjectMethod.Statements.Count == 0 && entityClass.Members.Contains(createobjectMethod))
                entityClass.Members.Remove(createobjectMethod);
            if (setvalueMethod.Statements.Count <= 1)
                entityClass.Members.Remove(setvalueMethod);

            

            foreach (CodeCompileUnit compileUnit in result.Values)
            {
                if((settings.LanguageSpecificHacks & LanguageSpecificHacks.AddOptionsExplicit) == LanguageSpecificHacks.AddOptionsExplicit)
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

        private CodeObjectCreateExpression GetMapField2ColumObjectCreationExpression(EntityDescription entity, PropertyDescription action, OrmCodeDomGeneratorSettings settings)
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
                                                            (entity
                                                             ,
                                                             settings, false) +
                                                        ".TablesLink"),
                        OrmCodeGenNameHelper.GetSafeName(action.Table.Identifier)
                        )
                    ));
            if (action.PropertyAlias == "ID")
                expression.Parameters.Add(GetPropAttributesEnumValues(action.Attributes));
            return expression;
        }

        private CodeExpression[] GetM2MRelationCreationExpressions(RelationDescription relationDescription, EntityDescription entity)
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
                return new CodeExpression[]
                    {
                        GetM2MRelationCreationExpression(entity, relationDescription.Table, relationDescription.UnderlyingEntity, relationDescription.Right.FieldName, relationDescription.Right.CascadeDelete, true),
                        GetM2MRelationCreationExpression(entity, relationDescription.Table, relationDescription.UnderlyingEntity, relationDescription.Left.FieldName, relationDescription.Left.CascadeDelete, false)
                    };
            }
        }

        private CodeExpression GetM2MRelationCreationExpression(EntityDescription relatedEntity, TableDescription relationTable, EntityDescription underlyingEntity, string fieldName, bool cascadeDelete, bool? direct)
        {
            if (underlyingEntity != null && direct.HasValue)
                throw new NotImplementedException("M2M relation on sefl cannot have underlying entity.");
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
                    new CodePrimitiveExpression(relatedEntity.Name)
                );
            if (underlyingEntity == null)
                tableExpression = new CodeMethodInvokeExpression(
                    new CodeCastExpression(
                        new CodeTypeReference(typeof(IDbSchema)),
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
                        new CodePrimitiveExpression(underlyingEntity.Name)
                        )
                    );

            fieldExpression = new CodePrimitiveExpression(fieldName);

            cascadeDeleteExpression = new CodePrimitiveExpression(cascadeDelete);

            mappingExpression = new CodeObjectCreateExpression(new CodeTypeReference(typeof(DataTableMapping)));

            if (underlyingEntity != null)
            {
                CodeExpression connectedTypeExpression = null;
                connectedTypeExpression = new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_schema"),
                        "GetTypeByEntityName"
                        ),
                    new CodePrimitiveExpression(underlyingEntity.Name)
                    );
                return new CodeObjectCreateExpression(
                new CodeTypeReference(typeof(M2MRelation)),
                entityTypeExpression,
                tableExpression,
                fieldExpression,
                cascadeDeleteExpression,
                mappingExpression,
                connectedTypeExpression
                );
            }
            else if (direct.HasValue)
            {
                CodeExpression directExpression = new CodePrimitiveExpression(direct.Value);
                return new CodeObjectCreateExpression(
                new CodeTypeReference(typeof(M2MRelation)),
                entityTypeExpression,
                tableExpression,
                fieldExpression,
                cascadeDeleteExpression,
                mappingExpression,
                directExpression
                );
            }
            else
            {
                return new CodeObjectCreateExpression(
                new CodeTypeReference(typeof(M2MRelation)),
                entityTypeExpression,
                tableExpression,
                fieldExpression,
                cascadeDeleteExpression,
                mappingExpression
                );
            }

        }

        private void CreateChangeValueTypeMethod(EntityDescription entity, CodeTypeDeclaration entitySchemaDefClass, OrmCodeDomGeneratorSettings settings)
        {
            CodeMemberMethod method;
            if (entity.Behaviour != EntityBehaviuor.PartialObjects && entity.BaseEntity == null)
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

        private void CreateProperties(CodeMemberMethod createobjectMethod, EntityDescription entity, CodeTypeDeclaration entityClass, OrmCodeDomGeneratorSettings settings, CodeMemberMethod setvalueMethod, CodeMemberMethod copyMethod)
        {
            EntityDescription completeEntity = entity.CompleteEntity;

            for (int idx = 0; idx < completeEntity.Properties.Count; idx++)
            {
                #region создание проперти и etc
                PropertyDescription propertyDesc;
                propertyDesc = completeEntity.Properties[idx];

                if (propertyDesc.PropertyAlias == "ID")
                    continue;
                if (!propertyDesc.FromBase)
                    CreateProperty(copyMethod, createobjectMethod, entityClass, propertyDesc, settings, setvalueMethod);
                else
                    CreateProperty(copyMethod, createobjectMethod, entityClass, propertyDesc, settings, setvalueMethod);
                    //CreateUpdatedProperty(entityClass, propertyDesc, settings);
            }
        }

        private void CreateUpdatedProperty(CodeTypeDeclaration entityClass, PropertyDescription propertyDesc, OrmCodeDomGeneratorSettings settings)
        {
            CodeTypeReference propertyType;
            propertyType = new CodeTypeReference(propertyDesc.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetQualifiedEntityName(propertyDesc.PropertyType.Entity, settings, false) : propertyDesc.PropertyType.TypeName);

            CodeMemberProperty property;
            property = new CodeMemberProperty();

            property.HasGet = true;
            property.HasSet = true;

            property.Name = propertyDesc.Name;
            property.Type = propertyType;
            property.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.New;


            CreatePropertyColumnAttribute(property, propertyDesc);
        }

        private void CreateProperty(CodeMemberMethod copyMethod, CodeMemberMethod createobjectMethod, CodeTypeDeclaration entityClass, PropertyDescription propertyDesc, OrmCodeDomGeneratorSettings settings, CodeMemberMethod setvalueMethod)
        {
            CodeMemberField field;
            CodeTypeReference fieldType;
            fieldType = new CodeTypeReference(propertyDesc.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetQualifiedEntityName(propertyDesc.PropertyType.Entity, settings, false) : propertyDesc.PropertyType.TypeName);
            string fieldName;
            fieldName = OrmCodeGenNameHelper.GetPrivateMemberName(propertyDesc.Name, settings);

            field = new CodeMemberField(fieldType, fieldName);
            field.Attributes = GetMemberAttribute(propertyDesc.FieldAccessLevel);

            CodeMemberProperty property;
            property = new CodeMemberProperty();

            property.HasGet = true;
            property.HasSet = true;

            property.Name = propertyDesc.Name;
            property.Type = fieldType;
            property.Attributes = GetMemberAttribute(propertyDesc.PropertyAccessLevel) | MemberAttributes.Final;
            #endregion создание проперти и etc

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

            if((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateCSUsingStatement) == LanguageSpecificHacks.GenerateCSUsingStatement)
                property.GetStatements.Add(new CodeDomPatterns.CodeCSUsingStatement(getUsingExpression, getInUsingStatements));
            else if((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateVBUsingStatement) == LanguageSpecificHacks.GenerateVBUsingStatement)
                property.GetStatements.Add(new CodeDomPatterns.CodeVBUsingStatement(getUsingExpression, getInUsingStatements));
            else
                property.GetStatements.AddRange(CodeGenPatterns.CodePatternUsingStatement(getUsingExpression,getInUsingStatements));
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

            if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateCSUsingStatement) == LanguageSpecificHacks.GenerateCSUsingStatement)
                property.SetStatements.Add(new CodeDomPatterns.CodeCSUsingStatement(setUsingExpression, setInUsingStatements));
            else if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateVBUsingStatement) == LanguageSpecificHacks.GenerateVBUsingStatement)
                property.SetStatements.Add(new CodeDomPatterns.CodeVBUsingStatement(setUsingExpression, setInUsingStatements));
            else
                property.SetStatements.AddRange(CodeGenPatterns.CodePatternUsingStatement(setUsingExpression, setInUsingStatements));

            #endregion property SetStatements

            #region property custom attribute Worm.Orm.ColumnAttribute

            CreatePropertyColumnAttribute(property, propertyDesc);

            #endregion property custom attribute Worm.Orm.ColumnAttribute

            #region описание проперти
            SetMemberDescription(property, propertyDesc.Description);
            #endregion описание проперти

            #region добавление членов в класс
            entityClass.Members.Add(field);
            entityClass.Members.Add(property);
            #endregion добавление членов в класс

            #region // реализация метода Copy

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
            #endregion // реализация метода Copy

            #region void SetValue(System.Reflection.PropertyInfo pi, ColumnAttribute c, object value)

            UpdateSetValueMethod(property, field, propertyDesc, setvalueMethod);

            #endregion void SetValue(System.Reflection.PropertyInfo pi, ColumnAttribute c, object value)

            #region void CreateObject(string fieldName, object value)
            if (Array.IndexOf(propertyDesc.Attributes, "Factory") != -1)
            {
                UpdateCreateObjectMethod(createobjectMethod, property);
            }

            #endregion void CreateObject(string fieldName, object value)
        }
        private void CreatePropertyColumnAttribute(CodeMemberProperty property, PropertyDescription propertyDesc)
        {
            
            CodeAttributeDeclaration declaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(ColumnAttribute)));
            
            if (!string.IsNullOrEmpty(propertyDesc.PropertyAlias))
                declaration.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(propertyDesc.PropertyAlias)));
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

        private void UpdateCreateObjectMethod(CodeMemberMethod createobjectMethod, CodeMemberProperty property)
        {
            createobjectMethod.Statements.Insert(createobjectMethod.Statements.Count - 1,
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

        private void UpdateSetValueMethod(CodeMemberProperty property, CodeMemberField field, PropertyDescription propertyDesc, CodeMemberMethod setvalueMethod)
        {
            Type fieldRealType;
            fieldRealType = Type.GetType(field.Type.BaseType, false);

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
                            new CodePrimitiveExpression(propertyDesc.PropertyAlias)
                            ),
                        new CodeAssignStatement(
                            new CodeFieldReferenceExpression(
                                new CodeThisReferenceExpression(),
                                field.Name
                                ),
                            new CodeCastExpression(
                                field.Type,
                                new CodeArgumentReferenceExpression("value")
                                )
                            ),
                        new CodeMethodReturnStatement()
                        )
                    );
            }
        }

        private CodeMemberMethod CreateCreateObjectMethod(EntityDescription entity, CodeTypeDeclaration entityClass, OrmCodeDomGeneratorSettings settings)
        {
            CodeMemberMethod createobjectMethod;
            createobjectMethod = new CodeMemberMethod();
            if (entity.Behaviour != EntityBehaviuor.PartialObjects)
                entityClass.Members.Add(createobjectMethod);
            createobjectMethod.Name = "CreateObject";
            // тип возвращаемого значения
            createobjectMethod.ReturnType = null;
            // модификаторы доступа
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

        private CodeMemberMethod CreateSetValueMethod(CodeTypeDeclaration entityClass)
        {
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
            return setvalueMethod;
        }

        private static void CreateGetTableMethod(EntityDescription entity, CodeTypeDeclaration entitySchemaDefClass, OrmCodeDomGeneratorSettings settings)
        {
            CodeMemberMethod method;
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
                        OrmCodeGenNameHelper.GetEntitySchemaDefClassQualifiedName(entity, settings, false) + ".TablesLink"), "tbl"
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
        }

        private void CreateGetTypeMainTableMethod(EntityDescription entity, CodeTypeDeclaration entitySchemaDefClass)
        {
            CodeMemberMethod method;
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
                    )
                );
        }

        private void CreateTablesLinkEnum(EntityDescription entity, CodeTypeDeclaration entitySchemaDefClass)
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

        private void ProcessSplitOption(EntityDescription entity, CodeTypeDeclaration entityClass, ref CodeTypeDeclaration entitySchemaDefClass, Dictionary<string, CodeCompileUnit> result, OrmCodeDomGeneratorSettings settings)
        {
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
                result.Add(OrmCodeGenNameHelper.GetEntitySchemaDefFileName(entity, settings), entitySchemaDefUnit);
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
                entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(OrmCodeGenNameHelper.GetEntitySchemaDefClassQualifiedName(entity.BaseEntity, settings, true)));
            }
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
                default:
                    return (MemberAttributes)0;
            }
        }
    }
}
