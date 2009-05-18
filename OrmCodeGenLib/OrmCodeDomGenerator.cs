using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Worm.CodeGen.Core.CodeDomExtensions;
using Worm.CodeGen.Core.Descriptors;
using Worm.Entities;
using Worm.Entities.Meta;
using Worm.Cache;
using Worm.Query;
using System.Linq;

namespace Worm.CodeGen.Core
{

    public partial class OrmCodeDomGenerator
    {
        #region Events

        protected internal class EntityClassCreatedEventArgs : EventArgs
        {
            private readonly CodeEntityTypeDeclaration m_typeDeclaration;
            private readonly CodeNamespace m_namespace;

            public EntityClassCreatedEventArgs(CodeNamespace typeNamespace, CodeEntityTypeDeclaration typeDeclaration)
            {
                m_typeDeclaration = typeDeclaration;
                m_namespace = typeNamespace;
            }

            public CodeEntityTypeDeclaration TypeDeclaration
            {
                get { return m_typeDeclaration; }
            }

            public CodeNamespace Namespace
            {
                get { return m_namespace; }
            }
        }
        protected internal class EntityPropertyCreatedEventArgs : EventArgs
        {
            private readonly PropertyDescription m_propertyDescription;
            private readonly CodeMemberField m_fieldMember;
            private readonly CodeMemberProperty m_propertyMember;
            private readonly CodeEntityTypeDeclaration m_entityTypeDeclaration;

            public EntityPropertyCreatedEventArgs(PropertyDescription propertyDescription, CodeEntityTypeDeclaration entityTypeDeclaration, CodeMemberField fieldMember, CodeMemberProperty propertyMember)
            {
                m_entityTypeDeclaration = entityTypeDeclaration;
                m_propertyMember = propertyMember;
                m_fieldMember = fieldMember;
                m_propertyDescription = propertyDescription;
            }

            public PropertyDescription PropertyDescription
            {
                get { return m_propertyDescription; }
            }

            public CodeMemberField FieldMember
            {
                get { return m_fieldMember; }
            }

            public CodeMemberProperty PropertyMember
            {
                get { return m_propertyMember; }
            }

            public CodeEntityTypeDeclaration EntityTypeDeclaration
            {
                get { return m_entityTypeDeclaration; }
            }
        }
        protected internal class EntityCtorCreatedEventArgs : EventArgs
        {
            private readonly CodeEntityTypeDeclaration m_entityType;
            private readonly CodeConstructor m_ctor;

            public EntityCtorCreatedEventArgs(CodeEntityTypeDeclaration entityType, CodeConstructor ctor)
            {
                m_entityType = entityType;
                m_ctor = ctor;
            }

            public CodeEntityTypeDeclaration EntityTypeDeclaration
            {
                get { return m_entityType; }
            }

            public CodeConstructor CtorDeclaration
            {
                get { return m_ctor; }
            }
        }

        protected event EventHandler<EntityClassCreatedEventArgs> EntityClassCreated
        {
            add
            {
                s_ctrl.EventDelegates.AddHandler(EntityGeneratorController.EntityClassCreatedKey, value);
            }
            remove
            {
                s_ctrl.EventDelegates.RemoveHandler(EntityGeneratorController.EntityClassCreatedKey, value);
            }
        }
        protected event EventHandler<EntityPropertyCreatedEventArgs> PropertyCreated
        {
            add
            {
                s_ctrl.EventDelegates.AddHandler(EntityGeneratorController.PropertyCreatedKey, value);
            }
            remove
            {
                s_ctrl.EventDelegates.RemoveHandler(EntityGeneratorController.PropertyCreatedKey, value);
            }
        }
        protected event EventHandler<EntityCtorCreatedEventArgs> EntityClassCtorCreated
        {
            add
            {
                s_ctrl.EventDelegates.AddHandler(EntityGeneratorController.EntityClassCtorCreatedKey, value);
            }
            remove
            {
                s_ctrl.EventDelegates.RemoveHandler(EntityGeneratorController.EntityClassCtorCreatedKey, value);
            }

        }

        #endregion

        protected internal class EntityGeneratorController : IDisposable
        {
            public System.ComponentModel.EventHandlerList EventDelegates = new System.ComponentModel.EventHandlerList();

            public static readonly object EntityClassCreatedKey = new object();
            public static readonly object PropertyCreatedKey = new object();
            public static readonly object EntityClassCtorCreatedKey = new object();

            //public event EventHandler<EntityClassCreatedEventArgs> EntityClassCreated;
            //public event EventHandler<EntityPropertyCreatedEventArgs> PropertyCreated;
            //public event EventHandler<EntityCtorCreatedEventArgs> EntityClassCtorCreated;	

            #region IDisposable Members

            public void Dispose()
            {
                if (EventDelegates != null)
                    EventDelegates.Dispose();
            }

            #endregion
        }

        private readonly OrmObjectsDef _ormObjectsDefinition;
        private readonly OrmCodeDomGeneratorSettings _ormCodeDomGeneratorSettings;

        public delegate OrmCodeDomGeneratorSettings GetSettingsDelegate();

        public OrmCodeDomGenerator(OrmObjectsDef ormObjectsDefinition, OrmCodeDomGeneratorSettings settings)
        {
            _ormObjectsDefinition = ormObjectsDefinition;
            _ormCodeDomGeneratorSettings = settings;
            OrmCodeGenNameHelper.OrmCodeDomGeneratorSettingsRequied += GetSettings;
            Delegates.SettingsRequied += GetSettings;
        }

        OrmCodeDomGeneratorSettings GetSettings()
        {
            return Settings;
        }

        protected OrmCodeDomGeneratorSettings Settings
        {
            get { return _ormCodeDomGeneratorSettings; }
        }

        public Dictionary<string, CodeCompileFileUnit> GetFullDom()
        {
            var result = new Dictionary<string, CodeCompileFileUnit>(_ormObjectsDefinition.ActiveEntities.Count());
            foreach (EntityDescription entity in _ormObjectsDefinition.ActiveEntities)
            {
                foreach (var pair in GetEntityCompileUnits(entity.Identifier))
                {
                    string key = pair.Filename;
                    for (int i = 0; result.ContainsKey(key); i++)
                    {
                        key = pair.Filename + i;
                    }

                    result.Add(key, pair);
                }
            }
            return result;
        }

        [ThreadStatic]
        private static EntityGeneratorController s_ctrl;

        public CodeCompileFileUnit GetFullSingleUnit()
        {
            CodeCompileFileUnit unit = new CodeCompileFileUnit();
            foreach (CodeCompileFileUnit u in GetFullDom().Values)
            {
                foreach (CodeNamespace n in u.Namespaces)
                {
                    CodeNamespace ns = n;
                    if (unit.Namespaces.Count == 0)
                    {
                        ns = new CodeNamespace(n.Name);
                        unit.Namespaces.Add(ns);
                    }
                    else
                        ns = unit.Namespaces[0];

                    foreach (CodeTypeDeclaration c in n.Types)
                    {
                        ns.Types.Add(c);
                    }
                }
            }
            CodeCompileFileUnit linq = GetLinqContext();
            if (linq != null)
                foreach (CodeNamespace n in linq.Namespaces)
                {
                    unit.Namespaces.Add(n);
                }

            return unit;
        }

        public CodeCompileFileUnit GetLinqContext()
        {
            if (_ormObjectsDefinition.LinqSettings == null) return null;

            var ctx = new CodeLinqContextDeclaration(_ormObjectsDefinition.LinqSettings);

            ctx.Entities.AddRange(_ormObjectsDefinition.FlatEntities);

            var result = new CodeCompileFileUnit
                             {
                                 Filename =
                                     !string.IsNullOrEmpty(_ormObjectsDefinition.LinqSettings.FileName)
                                         ? _ormObjectsDefinition.LinqSettings.FileName
                                         : ctx.Name
                             };

            var ns = new CodeNamespace(_ormObjectsDefinition.Namespace);
            ns.Types.Add(ctx);
            result.Namespaces.Add(ns);

            return result;

        }

        [Obsolete("Use GetEntityCompileUnits instead.")]
        public Dictionary<string, CodeCompileUnit> GetEntityDom(string entityId, OrmCodeDomGeneratorSettings settings)
        {
            var units = GetEntityCompileUnits(entityId);
            var result = new Dictionary<string, CodeCompileUnit>();
            foreach (var unit in units)
            {
                result.Add(unit.Filename, unit);
            }
            return result;
        }

        public IList<CodeCompileFileUnit> GetEntityCompileUnits(string entityId)
        {
            using (s_ctrl = new EntityGeneratorController())
            {

                //using (new SettingsManager(settings, null))
                //{

                if (String.IsNullOrEmpty(entityId))
                    throw new ArgumentNullException("entityId");

                EntityDescription entity = _ormObjectsDefinition.GetEntity(entityId);


                if (entity == null)
                    throw new ArgumentException("entityId",
                                                string.Format("Entity with id '{0}' not found.", entityId));

                PropertyCreated += OnPropertyDocumentationRequiered;

                if (entity.MakeInterface || (entity.BaseEntity != null && entity.BaseEntity.MakeInterface))
                {
                    EntityClassCreated += OnMakeEntityInterfaceRequired;
                    PropertyCreated += OnPropertyCreatedFillEntityInterface;
                }

                if (!entity.EnableCommonEventRaise)
                {
                    EntityClassCtorCreated += OnEntityCtorCustomPropEventsImplementationRequired;
                    PropertyCreated += OnPropertyChangedImplementationRequired;
                }

                try
                {
                    List<CodeCompileFileUnit> result = new List<CodeCompileFileUnit>();

                    CodeTypeDeclaration propertiesClass;
                    CodeTypeDeclaration fieldsClass = null;
                    CodeTypeDeclaration propertyAliasClass = null;
                    CodeTypeDeclaration instancedPropertyAliasClass = null;

                    CodeMemberMethod method;

                    #region ����������� ������ ��������

                    CodeCompileFileUnit entityUnit = new CodeCompileFileUnit
                                                        {
                                                            Filename = OrmCodeGenNameHelper.GetEntityFileName(entity)
                                                        };
                    result.Add(entityUnit);

                    // ���������
                    CodeNamespace nameSpace = new CodeNamespace(entity.Namespace);
                    entityUnit.Namespaces.Add(nameSpace);

                    // ����� ��������
                    CodeEntityTypeDeclaration entityClass = new CodeEntityTypeDeclaration(entity, Settings.UseTypeInProps);
                    nameSpace.Types.Add(entityClass);

                    // ��������� ������
                    entityClass.IsClass = true;
                    var behaviour = entity.CompleteEntity.Behaviour;
                    entityClass.IsPartial = behaviour == EntityBehaviuor.PartialObjects ||
                                            behaviour == EntityBehaviuor.ForcePartial;
                    entityClass.Attributes = MemberAttributes.Public;
                    entityClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.Public;

                    #endregion ����������� ������ ��������

                    if (!_ormObjectsDefinition.GenerateSchemaOnly)
                    {
                        // ���������
                        SetMemberDescription(entityClass, entity.Description);

                        // �������� ��������

                        // ������� �����
                        if (entity.BaseEntity == null)
                        {
                            CodeTypeReference entityType;
                            if (_ormObjectsDefinition.EntityBaseType == null)
                            {
                                entityType = new CodeTypeReference(entity.HasSinglePk ? typeof(KeyEntity) : entity.HasPk ? typeof(CachedLazyLoad) : typeof(Entity));
                            }
                            else
                            {
                                entityType =
                                    new CodeTypeReference(_ormObjectsDefinition.EntityBaseType.IsEntityType
                                                              ? OrmCodeGenNameHelper.GetEntityClassName(
                                                                    _ormObjectsDefinition.EntityBaseType.Entity,
                                                                    true)
                                                              : _ormObjectsDefinition.EntityBaseType.TypeName);
                                entityType.TypeArguments.Add(
                                    new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity)));
                            }

                            entityClass.BaseTypes.Add(entityType);
                            entityClass.BaseTypes.Add(new CodeTypeReference(typeof(IOptimizedValues)));
                        }

                        else
                            entityClass.BaseTypes.Add(
                                new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity.BaseEntity, true)));

                        RaiseEntityClassCreated(nameSpace, entityClass);

                        #region ����������� ������ Properties

                        {
                            propertiesClass = new CodeTypeDeclaration("Properties");
                            propertiesClass.Attributes = MemberAttributes.Public;
                            propertiesClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.NestedPublic;
                            propertiesClass.IsPartial = true;
                            CodeConstructor propctr = new CodeConstructor();
                            propctr.Attributes = MemberAttributes.Family;
                            propertiesClass.Members.Add(propctr);
                            SetMemberDescription(propertiesClass, "������ ������� ��������� ����������� � ��������� ������.");

                            if (entity.BaseEntity != null)
                                propertiesClass.Attributes |= MemberAttributes.New;

                            entityClass.Members.Add(propertiesClass);
                        }

                        #endregion ����������� ������ Properties

                        #region ����������� ������ Fields
                        if (_ormObjectsDefinition.GenerateEntityName)
                        {
                            fieldsClass = new CodeTypeDeclaration("props")
                                              {
                                                  Attributes = MemberAttributes.Public,
                                                  TypeAttributes = (TypeAttributes.Class | TypeAttributes.NestedPublic),
                                                  IsPartial = true
                                              };
                            var propctr = new CodeConstructor { Attributes = MemberAttributes.Family };
                            fieldsClass.Members.Add(propctr);

                            SetMemberDescription(fieldsClass, "������ �� ���� ���������.");

                            entityClass.Members.Add(fieldsClass);

                            if (entity.BaseEntity != null)
                                fieldsClass.Attributes |= MemberAttributes.New;
                        }

                        #endregion ����������� ������ Fields

                        #region ����������� ������ Descriptor
                        if (_ormObjectsDefinition.GenerateEntityName)
                        {
                            var descriptorClass = new CodeTypeDeclaration
                                                                      {
                                                                          Name = "Descriptor",
                                                                          Attributes = MemberAttributes.Public,
                                                                          TypeAttributes = (TypeAttributes.Class | TypeAttributes.NestedPublic),
                                                                          IsPartial = true
                                                                      };
                            var descConstr = new CodeConstructor
                                                             {
                                                                 Attributes = MemberAttributes.Family
                                                             };
                            descriptorClass.Members.Add(descConstr);

                            SetMemberDescription(descriptorClass, "��������� ��������.");

                            var entityNameField = new CodeMemberField
                                                      {
                                                          Type = new CodeTypeReference(typeof(string)),
                                                          Name = "EntityName",
                                                          InitExpression = new CodePrimitiveExpression(entity.Name),
                                                          Attributes = (MemberAttributes.Public | MemberAttributes.Const)
                                                      };

                            descriptorClass.Members.Add(entityNameField);

                            SetMemberDescription(entityNameField, "��� �������� � ��������� ������.");

                            entityClass.Members.Add(descriptorClass);

                            if (entity.BaseEntity != null)
                                descriptorClass.Attributes |= MemberAttributes.New;
                        }

                        #endregion

                        #region PropertyAlias class
                        if (_ormObjectsDefinition.GenerateEntityName)
                        {
                            propertyAliasClass = new CodeTypeDeclaration
                                                     {
                                                         Name = entity.Name + "Alias",
                                                         Attributes = MemberAttributes.Family,
                                                     };

                            var propertyAliasClassCtor = new CodeConstructor { Attributes = MemberAttributes.Public };

                            if (Settings.UseTypeInProps)
                                propertyAliasClassCtor.BaseConstructorArgs.Add(OrmCodeGenHelper.GetEntityClassTypeReferenceExpression(entity));
                            else
                                propertyAliasClassCtor.BaseConstructorArgs.Add(OrmCodeGenHelper.GetEntityNameReferenceExpression(entity));

                            propertyAliasClass.Members.Add(propertyAliasClassCtor);
                            propertyAliasClass.BaseTypes.Add(new CodeTypeReference(typeof(EntityAlias)));


                            instancedPropertyAliasClass = new CodeTypeDeclaration
                                                              {
                                                                  Name = entity.Name + "Properties",
                                                                  Attributes = MemberAttributes.Family,
                                                              };

                            var instancedPropertyAliasClassCtor = new CodeConstructor { Attributes = MemberAttributes.Public };

                            instancedPropertyAliasClassCtor.Parameters.Add(
                                new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(EntityAlias)), "objectAlias"));

                            instancedPropertyAliasClassCtor.Statements.Add(
                                new CodeAssignStatement(
                                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                                     OrmCodeGenNameHelper.GetPrivateMemberName("objectAlias")),
                                    new CodeArgumentReferenceExpression("objectAlias")));

                            instancedPropertyAliasClass.Members.Add(instancedPropertyAliasClassCtor);

                            var instancedPropertyAliasfield = new CodeMemberField(new CodeTypeReference(typeof(EntityAlias)),
                                                            OrmCodeGenNameHelper.GetPrivateMemberName("objectAlias"));
                            instancedPropertyAliasClass.Members.Add(instancedPropertyAliasfield);

                            instancedPropertyAliasClass.Members.Add(
                                Delegates.CodeMemberOperatorOverride(
                                    CodeDomPatterns.OperatorType.Implicit,
                                    new CodeTypeReference(typeof(EntityAlias)),
                                    new[]{new CodeParameterDeclarationExpression(new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity) + "." +
																		  instancedPropertyAliasClass.Name), "entityAlias")},
                                    new CodeStatement[] { new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeArgumentReferenceExpression("entityAlias"), instancedPropertyAliasfield.Name)) }

                                        )
                                );

                            if (entity.BaseEntity != null)
                            {
                                propertyAliasClass.Attributes |= MemberAttributes.New;
                                instancedPropertyAliasClass.Attributes |= MemberAttributes.New;
                            }

                            entityClass.Members.Add(propertyAliasClass);
                            entityClass.Members.Add(instancedPropertyAliasClass);
                        }

                        #endregion

                        #region ObjectAlias methods
                        if (propertyAliasClass != null)
                        {
                            var createMethod = new CodeMemberMethod
                                                   {
                                                       Name = "CreateAlias",
                                                       ReturnType =
                                                        new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity) + "." +
                                                                              propertyAliasClass.Name),
                                                       Attributes = MemberAttributes.Public | MemberAttributes.Static | MemberAttributes.Final,
                                                   };
                            if (entity.BaseEntity != null)
                                createMethod.Attributes |= MemberAttributes.New;
                            createMethod.Statements.Add(
                                new CodeMethodReturnStatement(
                                    new CodeObjectCreateExpression(
                                        new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity, true) + "." +
                                                              propertyAliasClass.Name))));
                            entityClass.Members.Add(createMethod);

                            var getMethod = new CodeMemberMethod
                                                {
                                                    Name = "GetAlias",
                                                    ReturnType =
                                                        new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity) + "." +
                                                                              instancedPropertyAliasClass.Name),
                                                    Attributes = MemberAttributes.Public | MemberAttributes.Static | MemberAttributes.Final,
                                                };

                            if (entity.BaseEntity != null)
                                getMethod.Attributes |= MemberAttributes.New;
                            getMethod.Parameters.Add(new CodeParameterDeclarationExpression { Name = "objectAlias", Type = new CodeTypeReference(typeof(EntityAlias)) });

                            getMethod.Statements.Add(
                                new CodeMethodReturnStatement(
                                    new CodeObjectCreateExpression(
                                        new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity, true) + "." +
                                                              instancedPropertyAliasClass.Name), new CodeArgumentReferenceExpression("objectAlias"))));
                            entityClass.Members.Add(getMethod);
                        }
                        #endregion

                        #region ������������

                        // ����������� �� ���������
                        CodeConstructor ctr = new CodeConstructor();
                        ctr.Attributes = MemberAttributes.Public;
                        entityClass.Members.Add(ctr);

                        RaiseEntityCtorCreated(entityClass, ctr);

                        //if(
                        if (entity.HasSinglePk)
                        {
                            // ������������������� �����������
                            ctr = new CodeConstructor();
                            ctr.Attributes = MemberAttributes.Public;
                            // ��������� ������������
                            ctr.Parameters.Add(new CodeParameterDeclarationExpression(entity.PkProperty.PropertyType, "id"));
                            ctr.Parameters.Add(new CodeParameterDeclarationExpression(typeof(CacheBase), "cache"));
                            ctr.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ObjectMappingEngine),
                                                                                      "schema"));

                            ctr.Statements.Add(new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), "Init",
                                                                              new CodeArgumentReferenceExpression("id"),
                                                                              new CodeArgumentReferenceExpression("cache"),
                                                                              new CodeArgumentReferenceExpression("schema")));

                            entityClass.Members.Add(ctr);
                            RaiseEntityCtorCreated(entityClass, ctr);
                        }

                        #endregion ������������

                        #region ����� OrmBase.CopyBody(CopyBody(...)


                        EntityDescription superbaseEntity;
                        for (superbaseEntity = entity;
                             superbaseEntity.BaseEntity != null;
                             superbaseEntity = superbaseEntity.BaseEntity)
                        {

                        }

                        bool isInitialImplemantation = entity == superbaseEntity;

                        CodeMemberMethod copyMethod = new CodeMemberMethod();
                        entityClass.Members.Add(copyMethod);
                        copyMethod.Name = "CopyProperties";
                        copyMethod.ReturnType = null;
                        // ������������ �������
                        copyMethod.Attributes = MemberAttributes.Family | MemberAttributes.Override;
                        copyMethod.Parameters.Add(
                            new CodeParameterDeclarationExpression(typeof(_IEntity),
                                                                   "from"));
                        copyMethod.Parameters.Add(
                            new CodeParameterDeclarationExpression(typeof(_IEntity),
                                                                   "to"));
                        copyMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(OrmManager), "mgr"));
                        copyMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IEntitySchema), "oschema"));

                        if (!isInitialImplemantation)
                            copyMethod.Statements.Add(
                                new CodeMethodInvokeExpression(
                                    new CodeBaseReferenceExpression(),
                                    "CopyProperties",
                                    new CodeArgumentReferenceExpression("from"),
                                    new CodeArgumentReferenceExpression("to"),
                                    new CodeArgumentReferenceExpression("mgr"),
                                    new CodeArgumentReferenceExpression("oschema")
                                    )
                                );

                        PropertyCreated += delegate(object sender, EntityPropertyCreatedEventArgs e)
                                               {
                                                   if (e.FieldMember == null) return;
                                                   string fieldName = e.FieldMember.Name;

                                                   CodeTypeReference entityType =
                                                       new CodeTypeReference(
                                                           OrmCodeGenNameHelper.GetEntityClassName(entity, true));

                                                   CodeExpression leftTargetExpression =
                                                       new CodeArgumentReferenceExpression("to");

                                                   CodeExpression rightTargetExpression =
                                                       new CodeArgumentReferenceExpression("from");

                                                   leftTargetExpression = new CodeCastExpression(entityType,
                                                                                                 leftTargetExpression);
                                                   rightTargetExpression = new CodeCastExpression(entityType,
                                                                                                  rightTargetExpression);

                                                   copyMethod.Statements.Add(
                                                       new CodeAssignStatement(
                                                           new CodeFieldReferenceExpression(leftTargetExpression,
                                                                                            fieldName),
                                                           new CodeFieldReferenceExpression(rightTargetExpression,
                                                                                            fieldName))
                                                       );
                                                   //#endregion // ���������� ������ Copy
                                               };

                        #endregion ����� OrmBase.CopyBody(CopyBody(OrmBase from, OrmBase to)

                        #region void SetValue(System.Reflection.PropertyInfo pi, string propertyAlias, object value)

                        CodeMemberMethod setvalueMethod = CreateSetValueMethod(entityClass);

                        #endregion void SetValue(System.Reflection.PropertyInfo pi, string propertyAlias, object value)

                        #region public override object GetValue(string propAlias, Worm.Orm.IOrmObjectSchemaBase schema)

                        CodeMemberMethod getvalueMethod = CreateGetValueMethod(entityClass);

                        #endregion public override object GetValue(string propAlias, Worm.Orm.IOrmObjectSchemaBase schema)

                        #region CachedEntity methods

                        //CodeMemberMethod createobjectMethod = null;

                        if (entity.HasPk)
                        {
                            if (entity.HasSinglePk)
                            {
                                if (entity.BaseEntity == null || entity.GetPKCount(false) > 0)
                                {
                                    OverrideIdentifierProperty(entityClass);
                                    CreateSetPKMethod(entityClass, false);
                                    CreateGetPKValuesMethod(entityClass);
                                }
                                else
                                {
                                    UpdateGetPKValuesMethod(entityClass);
                                    UpdateSetPKMethod(entityClass, false);
                                }
                            }
                            else
                            {
                                if (entity.BaseEntity == null || entity.GetPKCount(false) > 0)
                                {
                                    CreateGetKeyMethodCompositePK(entityClass);
                                    CreateGetPKValuesMethod(entityClass);
                                    CreateSetPKMethod(entityClass, true);
                                }
                                else
                                {
                                    UpdateGetKeyMethodCompositePK(entityClass);
                                    UpdateGetPKValuesMethod(entityClass);
                                    UpdateSetPKMethod(entityClass, true);
                                }

                                OverrideEqualsMethodCompositePK(entityClass);
                            }
                        }
                        #endregion

                        #region ��������

                        CreateProperties(entity, entityClass, setvalueMethod, getvalueMethod, propertiesClass, fieldsClass, propertyAliasClass, instancedPropertyAliasClass);

                        #endregion ��������

                        #region void SetValue(System.Reflection.PropertyInfo pi, EntityPropertyAttribute c, object value)

                        if (entity.BaseEntity != null)
                            setvalueMethod.Statements.Add(
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

                        #endregion void SetValue(System.Reflection.PropertyInfo pi, EntityPropertyAttribute c, object value)

                        #region m2m relation methods

                        if (!Settings.RemoveOldM2M)
                            CreateM2MMethodsSet(entity, entityClass);

                        #endregion

                        if (setvalueMethod.Statements.Count <= 1)
                            entityClass.Members.Remove(setvalueMethod);
                    }

                    #region custom attribute EntityAttribute

                    entityClass.CustomAttributes = new CodeAttributeDeclarationCollection(
                        new[]
			                {
			                    new CodeAttributeDeclaration(
			                        new CodeTypeReference(typeof (EntityAttribute)),
			                        new CodeAttributeArgument(
			                            new CodeTypeOfExpression(
			                                new CodeTypeReference(
			                                    OrmCodeGenNameHelper.GetEntitySchemaDefClassQualifiedName(entity))
			                                )
			                            ),
			                        new CodeAttributeArgument(
			                            new CodePrimitiveExpression(_ormObjectsDefinition.SchemaVersion)
			                            ),
			                        new CodeAttributeArgument(
			                            "EntityName",
			                            OrmCodeGenHelper.GetEntityNameReferenceExpression(entity)
			                            )
			                        ),
			                }
                        );

                    if (!_ormObjectsDefinition.GenerateSchemaOnly)
                        entityClass.CustomAttributes.Add(new CodeAttributeDeclaration(
                            new CodeTypeReference(typeof(SerializableAttribute))
                            )
                        );

                    #endregion custom attribute EntityAttribute

                    #region ���� ��������

                    CreateTablesLinkEnum(entity, entityClass.SchemaDef);

                    #endregion ���� ��������

                    #region bool ChangeValueType(EntityPropertyAttribute c, object value, ref object newvalue)

                    CreateChangeValueTypeMethod(entity, entityClass.SchemaDef);

                    #endregion bool ChangeValueType(EntityPropertyAttribute c, object value, ref object newvalue)

                    #region string[] GetSuppressedColumns()

                    // �������������� ���������� ��� ���� ������� � suppressed properties
                    if (entity.BaseEntity == null ||
                        (entity.BaseEntity.SuppressedProperties.Count + entity.SuppressedProperties.Count != 0) ||
                        entity.BaseEntity.SuppressedProperties.Count != entity.SuppressedProperties.Count ||
                        !(entity.SuppressedProperties.TrueForAll(p => entity.BaseEntity.SuppressedProperties.Exists(pp => pp.Name == p.Name)) &&
                        entity.BaseEntity.SuppressedProperties.TrueForAll(p => entity.SuppressedProperties.Exists(pp => pp.Name == p.Name))))
                    {

                        method = new CodeMemberMethod();
                        entityClass.SchemaDef.Members.Add(method);
                        method.Name = "GetSuppressedFields";
                        // ��� ������������� ��������
                        method.ReturnType = new CodeTypeReference(typeof(string[]));
                        // ������������ �������
                        method.Attributes = MemberAttributes.Public;

                        if (entity.BaseEntity != null)
                            method.Attributes |= MemberAttributes.Override;
                        else
                            // ��������� ����� �������� ������
                            method.ImplementationTypes.Add(typeof(IEntitySchemaBase));
                        CodeArrayCreateExpression arrayExpression = new CodeArrayCreateExpression(
                            new CodeTypeReference(typeof(string[]))
                        );


                        foreach (PropertyDescription suppressedProperty in entity.SuppressedProperties)
                        {
                            arrayExpression.Initializers.Add(
                                new CodePrimitiveExpression(suppressedProperty.PropertyAlias)
                            );
                        }




                        method.Statements.Add(new CodeMethodReturnStatement(arrayExpression));
                    }

                    #endregion EntityPropertyAttribute[] GetSuppressedColumns()

                    #region �������� ��������� �����

                    RelationDescriptionBase relation = _ormObjectsDefinition.ActiveRelations
                        .Find(match => match.UnderlyingEntity == entity);

                    if (relation != null)
                    {
                        SelfRelationDescription sd = relation as SelfRelationDescription;
                        if (sd == null)
                            ImplementIRelation((RelationDescription)relation, entity, entityClass.SchemaDef);
                        else
                            ImplementIRelation(sd, entity, entityClass.SchemaDef);
                    }

                    #endregion �������� ��������� �����

                    #region public void GetSchema(OrmSchemaBase schema, Type t)

                    if (entity.BaseEntity == null)
                    {
                        CodeMemberField schemaField = new CodeMemberField(
                            new CodeTypeReference(typeof(ObjectMappingEngine)),
                            "_schema"
                            );
                        CodeMemberField typeField = new CodeMemberField(
                            new CodeTypeReference(typeof(Type)),
                            "_entityType"
                            );
                        schemaField.Attributes = MemberAttributes.Family;
                        entityClass.SchemaDef.Members.Add(schemaField);
                        typeField.Attributes = MemberAttributes.Family;
                        entityClass.SchemaDef.Members.Add(typeField);
                        method = new CodeMemberMethod();
                        entityClass.SchemaDef.Members.Add(method);
                        method.Name = "GetSchema";
                        // ��� ������������� ��������
                        method.ReturnType = null;
                        // ������������ �������
                        method.Attributes = MemberAttributes.Public | MemberAttributes.Final;

                        method.Parameters.Add(
                            new CodeParameterDeclarationExpression(
                                new CodeTypeReference(typeof(ObjectMappingEngine)),
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
                        method.ImplementationTypes.Add(typeof(ISchemaInit));
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

                    #endregion public void GetSchema(OrmSchemaBase schema, Type t)

                    foreach (CodeCompileFileUnit compileUnit in result)
                    {
                        if ((Settings.LanguageSpecificHacks & LanguageSpecificHacks.AddOptionsExplicit) ==
                            LanguageSpecificHacks.AddOptionsExplicit)
                            compileUnit.UserData.Add("RequireVariableDeclaration",
                                                     (Settings.LanguageSpecificHacks &
                                                      LanguageSpecificHacks.OptionsExplicitOn) ==
                                                     LanguageSpecificHacks.OptionsExplicitOn);
                        if ((Settings.LanguageSpecificHacks & LanguageSpecificHacks.AddOptionsStrict) ==
                            LanguageSpecificHacks.AddOptionsStrict)
                            compileUnit.UserData.Add("AllowLateBound",
                                                     (Settings.LanguageSpecificHacks &
                                                      LanguageSpecificHacks.OptionsStrictOn) !=
                                                     LanguageSpecificHacks.OptionsStrictOn);

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
                                                                      new CodeCommentStatement(
                                                                          commentBuilder.ToString(), false));
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
                finally
                {
                    PropertyCreated -= OnPropertyDocumentationRequiered;

                    if (entity.MakeInterface || (entity.BaseEntity != null && entity.BaseEntity.MakeInterface))
                    {
                        EntityClassCreated -= OnMakeEntityInterfaceRequired;
                        PropertyCreated -= OnPropertyCreatedFillEntityInterface;
                    }

                    if (!entity.EnableCommonEventRaise)
                    {
                        EntityClassCtorCreated -= OnEntityCtorCustomPropEventsImplementationRequired;
                        PropertyCreated -= OnPropertyChangedImplementationRequired;
                    }
                }
            }
        }

        private void OverrideEqualsMethodCompositePK(CodeEntityTypeDeclaration entityClass)
        {
            CodeMemberMethod method = new CodeMemberMethod
                                          {
                                              Name = "Equals",
                                              Attributes = MemberAttributes.Override,
                                          };

            method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(object)), "obj"));

            CodeExpression exp = null;

            foreach (var pk in entityClass.Entity.PkProperties)
            {
                var tExp = new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                            OrmCodeGenNameHelper.GetPrivateMemberName(pk.Name)), "Equals",
                                                          new CodeFieldReferenceExpression(new CodeArgumentReferenceExpression("obj"),
                                                            OrmCodeGenNameHelper.GetPrivateMemberName(pk.Name)));
                if (exp == null)
                    exp = tExp;
                else
                    exp = new CodeBinaryOperatorExpression(exp, CodeBinaryOperatorType.BooleanAnd, tExp);

            }
            if (entityClass.Entity.BaseEntity != null)
                exp = new CodeBinaryOperatorExpression(exp, CodeBinaryOperatorType.BooleanAnd, new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), "Equals", new CodeArgumentReferenceExpression("obj")));
            method.Statements.Add(new CodeMethodReturnStatement(exp));


        }



        private void UpdateSetPKMethod(CodeEntityTypeDeclaration entityClass, bool composite)
        {
            EntityDescription entity = entityClass.Entity;
            if (entity.PkProperties.Count == 0)
                return;

            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "SetPK";

            // ������������ �������
            meth.Attributes = MemberAttributes.Family | MemberAttributes.Override;

            entityClass.Members.Add(meth);

            meth.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(new CodeTypeReference(typeof(PKDesc)), 1), "pks")
            );

            meth.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(typeof(ObjectMappingEngine)), "mpe")
            );

            meth.Statements.Add(new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), meth.Name,
                                                               new CodeArgumentReferenceExpression(
                                                                   meth.Parameters[0].Name)));

            if (composite)
            {
                meth.Statements.Add(
                    Delegates.CodePatternForeachStatement(
                        new CodeTypeReference(typeof(PKDesc)), "pk",
                        new CodeArgumentReferenceExpression("pks"),
                        entity.PkProperties.
                        ConvertAll<CodeStatement>(
                            delegate(PropertyDescription pd_)
                            {
                                //var typeReference = new CodeTypeReference(pd_.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetEntityClassName(pd_.PropertyType.Entity, true) : pd_.PropertyType.TypeName);
                                CodeTypeReference typeReference = pd_.PropertyType;
                                return new CodeConditionStatement(
                                    new CodeBinaryOperatorExpression(
                                        new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("pk"),
                                            "PropertyAlias"),
                                        CodeBinaryOperatorType.ValueEquality,
                                        new CodePrimitiveExpression(pd_.PropertyAlias)),
                                    new CodeAssignStatement(
                                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), OrmCodeGenNameHelper.GetPrivateMemberName(pd_.PropertyName)),
                                        new CodeCastExpression(
                                            typeReference,
                                            new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Convert)), "ChangeType",
                                           new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("pk"), "Value"),
                                           new CodeTypeOfExpression(typeReference))
                                       )
                                    )

                                );
                            }
                         ).ToArray()
                    )
                );
            }
            else
            {
                var pkProperty = entity.PkProperty;
                meth.Statements.Add(
                    new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                                            OrmCodeGenNameHelper.GetPrivateMemberName(pkProperty.PropertyName)),
                        new CodeCastExpression(pkProperty.PropertyType,
                            new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Convert)), "ChangeType",
                                new CodeFieldReferenceExpression(
                                    new CodeIndexerExpression(
                                        new CodeArgumentReferenceExpression("pks"), new CodePrimitiveExpression(0)),
                                        "Value"),
                                new CodeTypeOfExpression(pkProperty.PropertyType)
                            )
                        )
                )
                );
            }
        }

        private void UpdateGetPKValuesMethod(CodeEntityTypeDeclaration entityClass)
        {
            EntityDescription entity = entityClass.Entity;
            if (entity.PkProperties.Count == 0)
                return;

            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "GetPKValues";
            CodeTypeReference tr = new CodeTypeReference(typeof(PKDesc));
            // ��� ������������� ��������
            meth.ReturnType = new CodeTypeReference(tr, 1);

            // ������������ �������
            meth.Attributes = MemberAttributes.Public | MemberAttributes.Override;

            entityClass.Members.Add(meth);

            meth.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(tr, 1), "basePks", new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), meth.Name)));
            meth.Statements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference(tr, 1),
                    "result",
                    new CodeArrayCreateExpression(
                        new CodeTypeReference(tr, 1),
                        new CodeBinaryOperatorExpression(
                            new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("basePks"), "Length"),
                            CodeBinaryOperatorType.Add,
                            new CodePrimitiveExpression(entity.PkProperties.Count)
                        )

                    )
                )
            );

            //int[] v = new int[10];
            //int[] f = new int[] {1, 2};
            //int[] s = new int[] {1, 2, 3};
            //Array.Copy(f, v, f.Length);
            //Array.Copy(s, 0, v, f.Length, s.Length);

            meth.Statements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Array)), "Copy", new CodeVariableReferenceExpression("basePks"), new CodeVariableReferenceExpression("result"), new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("basePks"), "Length")));
            meth.Statements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference(tr, 1),
                    "newPks",
                    new CodeArrayCreateExpression(
                        new CodeTypeReference(tr, 1),
                        entity.PkProperties.ConvertAll<CodeExpression>(pd_ => new CodeObjectCreateExpression(tr, new CodePrimitiveExpression(pd_.PropertyAlias),
                                                              new CodeFieldReferenceExpression(
                                                                  new CodeThisReferenceExpression(),
                                                                  OrmCodeGenNameHelper.GetPrivateMemberName
                                                                      (pd_.PropertyName)))).ToArray()
                        )
                    )
                );
            meth.Statements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Array)), "Copy", new CodeVariableReferenceExpression("basePks"), new CodeVariableReferenceExpression("result"), new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("basePks"), "Length")));

            meth.Statements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Array)), "Copy", new CodeVariableReferenceExpression("newPks"), new CodePrimitiveExpression(0), new CodeVariableReferenceExpression("result"), new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("basePks"), "Length"), new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("newPks"), "Length")));

            meth.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("result")));
        }

        private void UpdateGetKeyMethodCompositePK(CodeEntityTypeDeclaration entityClass)
        {
            EntityDescription entity = entityClass.Entity;

            if (entity.PkProperties.Count == 0)
                return;

            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "GetCacheKey";
            // ��� ������������� ��������
            meth.ReturnType = new CodeTypeReference(typeof(Int32));
            // ������������ �������
            meth.Attributes = MemberAttributes.Family | MemberAttributes.Override;

            entityClass.Members.Add(meth);

            CodeExpression lf = new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), meth.Name);

            foreach (PropertyDescription pd in entity.Properties)
            {
                if (pd.HasAttribute(Field2DbRelations.PK))
                {
                    string fn = OrmCodeGenNameHelper.GetPrivateMemberName(pd.PropertyName);

                    CodeExpression exp = new CodeMethodInvokeExpression(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fn),
                        "GetHashCode", new CodeExpression[0]);

                    lf = Delegates.CodePatternXorStatement(lf, exp);

                }
            }
            meth.Statements.Add(new CodeMethodReturnStatement(lf));
        }

        private void OverrideIdentifierProperty(CodeEntityTypeDeclaration entityClass)
        {
            var property = new CodeMemberProperty
                               {
                                   Name = "Identifier",
                                   Type = new CodeTypeReference(typeof(object)),
                                   HasGet = true,
                                   HasSet = true,
                                   Attributes = MemberAttributes.Public | MemberAttributes.Override
                               };
            PropertyDescription pkProperty = entityClass.Entity.PkProperty;
            property.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                                           OrmCodeGenNameHelper.GetPrivateMemberName(pkProperty.PropertyName))));
            //Convert.ChangeType(object, type);
            //var typeReference = new CodeTypeReference(pkProperty.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetEntityClassName(pkProperty.PropertyType.Entity, true) : pkProperty.PropertyType.TypeName);
            CodeTypeReference typeReference = pkProperty.PropertyType;
            property.SetStatements.Add(
                new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                                           OrmCodeGenNameHelper.GetPrivateMemberName(pkProperty.PropertyName)),
                        new CodeCastExpression(typeReference,
                            new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Convert)), "ChangeType",
                                new CodePropertySetValueReferenceExpression(),
                                new CodeTypeOfExpression(typeReference)
                            )
                        )
                )
            );
            entityClass.Members.Add(property);
        }

        private void CreateSetPKMethod(CodeEntityTypeDeclaration entityClass, bool composite)
        {
            EntityDescription entity = entityClass.Entity;
            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "SetPK";

            // ������������ �������
            meth.Attributes = MemberAttributes.Family | MemberAttributes.Override;

            entityClass.Members.Add(meth);

            meth.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(new CodeTypeReference(typeof(PKDesc)), 1), "pks")
            );
            meth.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(typeof(ObjectMappingEngine)), "mpe")
            );

            if (composite)
            {
                meth.Statements.Add(
                    Delegates.CodePatternForeachStatement(
                        new CodeTypeReference(typeof(PKDesc)), "pk",
                        new CodeArgumentReferenceExpression("pks"),
                        entity.PkProperties.
                        ConvertAll<CodeStatement>(
                            delegate(PropertyDescription pd_)
                            {
                                //var typeReference = new CodeTypeReference(pd_.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetEntityClassName(pd_.PropertyType.Entity, true) : pd_.PropertyType.TypeName);
                                CodeTypeReference typeReference = pd_.PropertyType;
                                return new CodeConditionStatement(
                                    new CodeBinaryOperatorExpression(
                                        new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("pk"),
                                            "PropertyAlias"),
                                        CodeBinaryOperatorType.ValueEquality,
                                        new CodePrimitiveExpression(pd_.PropertyAlias)),
                                    new CodeAssignStatement(
                                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), OrmCodeGenNameHelper.GetPrivateMemberName(pd_.PropertyName)),
                                        new CodeCastExpression(
                                            typeReference,
                                            new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Convert)), "ChangeType",
                                           new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("pk"), "Value"),
                                           new CodeTypeOfExpression(typeReference))
                                       )
                                    )

                                );
                            }
                         ).ToArray()
                    )
                );
            }
            else
            {
                var pkProperty = entity.PkProperty;
                meth.Statements.Add(
                new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                                            OrmCodeGenNameHelper.GetPrivateMemberName(pkProperty.PropertyName)),
                        new CodeCastExpression(pkProperty.PropertyType,
                            new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Convert)), "ChangeType",
                                new CodeFieldReferenceExpression(
                                    new CodeIndexerExpression(
                                        new CodeArgumentReferenceExpression("pks"), new CodePrimitiveExpression(0)),
                                        "Value"),
                                new CodeTypeOfExpression(pkProperty.PropertyType)
                            )
                        )
                )
            );
            }
        }

        private void CreateGetPKValuesMethod(CodeEntityTypeDeclaration entityClass)
        {
            EntityDescription entity = entityClass.Entity;
            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "GetPKValues";
            CodeTypeReference tr = new CodeTypeReference(typeof(PKDesc));
            // ��� ������������� ��������
            meth.ReturnType = new CodeTypeReference(tr, 1);

            // ������������ �������
            meth.Attributes = MemberAttributes.Public | MemberAttributes.Override;

            entityClass.Members.Add(meth);

            meth.Statements.Add(
                new CodeMethodReturnStatement(new CodeArrayCreateExpression(meth.ReturnType,
                    entity.Properties.FindAll(
                        pd_ => pd_.HasAttribute(Field2DbRelations.PK)).
                    ConvertAll<CodeExpression>(
                        pd_ => new CodeObjectCreateExpression(tr, new CodePrimitiveExpression(pd_.PropertyAlias),
                                                              new CodeFieldReferenceExpression(
                                                                  new CodeThisReferenceExpression(),
                                                                  OrmCodeGenNameHelper.GetPrivateMemberName(
                                                                      pd_.PropertyName)))
                        ).ToArray()))
                );
        }


        private void CreateGetKeyMethodCompositePK(CodeEntityTypeDeclaration entityClass)
        {
            EntityDescription entity = entityClass.Entity;

            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "GetCacheKey";
            // ��� ������������� ��������
            meth.ReturnType = new CodeTypeReference(typeof(Int32));
            // ������������ �������
            meth.Attributes = MemberAttributes.Family | MemberAttributes.Override;

            entityClass.Members.Add(meth);

            CodeExpression lf = null;

            foreach (PropertyDescription pd in entity.Properties)
            {
                if (pd.HasAttribute(Field2DbRelations.PK))
                {
                    string fn = OrmCodeGenNameHelper.GetPrivateMemberName(pd.PropertyName);

                    CodeExpression exp = new CodeMethodInvokeExpression(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fn),
                        "GetHashCode", new CodeExpression[0]);

                    lf = lf == null ? exp : Delegates.CodePatternXorStatement(lf, exp);
                }
            }
            meth.Statements.Add(new CodeMethodReturnStatement(lf));
        }

        void OnPropertyChangedImplementationRequired(object sender, EntityPropertyCreatedEventArgs e)
        {
            CodeMemberProperty property = e.PropertyMember;
            PropertyDescription propDesc = e.PropertyDescription;
            CodeMemberField field = e.FieldMember;
            //CodeEntityTypeDeclaration entityClass = e.

            if (!propDesc.EnablePropertyChanged || propDesc.FromBase || !property.HasSet)
                return;

            CodeDomPatterns.CodeUsingStatementBase usingStatement = null;

            foreach (CodeStatement statement in property.SetStatements)
            {
                usingStatement = statement as CodeDomPatterns.CodeUsingStatementBase;
            }
            if (usingStatement != null)
            {
                List<CodeStatement> statements = new List<CodeStatement>(usingStatement.Statements);
                statements.InsertRange(0,
                                       new CodeStatement[]
				                       	{
				                       		new CodeVariableDeclarationStatement(
				                       			typeof (bool),
				                       			"notChanged",
				                       			new CodeBinaryOperatorExpression(
				                       				new CodeFieldReferenceExpression(
				                       					new CodeThisReferenceExpression(),
				                       					field.Name
				                       					),
				                       				propDesc.PropertyType.IsValueType
				                       					? CodeBinaryOperatorType.ValueEquality
				                       					: CodeBinaryOperatorType.IdentityEquality,
				                       				new CodePropertySetValueReferenceExpression()
				                       				)
				                       			),
				                       		new CodeVariableDeclarationStatement(
				                       			field.Type,
				                       			"oldValue",
				                       			new CodeFieldReferenceExpression(
				                       				new CodeThisReferenceExpression(),
				                       				field.Name
				                       				)
				                       			)
				                       	}
                    );
                statements.Add(
                    new CodeConditionStatement(
                        new CodeBinaryOperatorExpression(
                            new CodePrimitiveExpression(false),
                            CodeBinaryOperatorType.ValueEquality,
                            new CodeVariableReferenceExpression("notChanged")
                            ),
                        new CodeExpressionStatement(
                            new CodeMethodInvokeExpression(
                                new CodeThisReferenceExpression(),
                                "RaisePropertyChanged",
                                OrmCodeGenHelper.GetFieldNameReferenceExpression(propDesc),
                                new CodeVariableReferenceExpression("oldValue")
                                )
                            )
                        )
                    );
                usingStatement.Statements = statements.ToArray();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void RaiseEntityCtorCreated(CodeEntityTypeDeclaration entityClass, CodeConstructor ctr)
        {
            EventHandler<EntityCtorCreatedEventArgs> h =
                s_ctrl.EventDelegates[EntityGeneratorController.EntityClassCtorCreatedKey] as EventHandler<EntityCtorCreatedEventArgs>;
            if (h != null)
            {
                h(this, new EntityCtorCreatedEventArgs(entityClass, ctr));
            }
        }

        private void RaiseEntityClassCreated(CodeNamespace nameSpace, CodeEntityTypeDeclaration entityClass)
        {
            EventHandler<EntityClassCreatedEventArgs> h = s_ctrl.EventDelegates[EntityGeneratorController.EntityClassCreatedKey] as EventHandler<EntityClassCreatedEventArgs>;
            if (h != null)
            {
                h(this, new EntityClassCreatedEventArgs(nameSpace, entityClass));
            }
        }

        void OnEntityCtorCustomPropEventsImplementationRequired(object sender, EntityCtorCreatedEventArgs e)
        {
            CodeConstructor con = e.CtorDeclaration;

            con.Statements.Add(
                new CodeAssignStatement(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        "_dontRaisePropertyChange"
                        ),
                        new CodePrimitiveExpression(true)
                    )
                );
        }

        protected virtual void OnPropertyDocumentationRequiered(object sender, EntityPropertyCreatedEventArgs e)
        {
            PropertyDescription propertyDesc = e.PropertyDescription;
            CodeMemberProperty property = e.PropertyMember;

            SetMemberDescription(property, propertyDesc.Description);
        }

        protected virtual void OnPropertyCreatedFillEntityInterface(object sender, EntityPropertyCreatedEventArgs e)
        {
            CodeEntityTypeDeclaration entityClass = e.EntityTypeDeclaration;
            CodeMemberProperty propertyMember = e.PropertyMember;

            CodeEntityInterfaceDeclaration entityPropertiesInterface = entityClass.EntityPropertiesInterfaceDeclaration;

            CreatePropertyInInterface(entityPropertiesInterface, propertyMember);
        }

        private void CreatePropertyInInterface(CodeEntityInterfaceDeclaration entityInterface, CodeMemberProperty propertyMember)
        {
            if ((propertyMember.Attributes & MemberAttributes.Public) != MemberAttributes.Public)
                return;

            CodeMemberProperty interfaceProperty = new CodeMemberProperty();
            entityInterface.Members.Add(interfaceProperty);

            interfaceProperty.HasGet = propertyMember.HasGet;
            interfaceProperty.HasSet = propertyMember.HasSet;
            interfaceProperty.Name = propertyMember.Name;
            interfaceProperty.Type = propertyMember.Type;

            foreach (CodeCommentStatement comment in propertyMember.Comments)
            {
                interfaceProperty.Comments.Add(comment);
            }

            propertyMember.ImplementationTypes.Add(entityInterface.TypeReference);
        }

        protected virtual void OnMakeEntityInterfaceRequired(object sender, EntityClassCreatedEventArgs e)
        {
            CodeEntityTypeDeclaration entityClass = e.TypeDeclaration;
            CodeNamespace entityNamespace = e.Namespace;

            CreateEntityInterfaces(entityNamespace, entityClass);
        }

        private void CreateEntityInterfaces(CodeNamespace entityNamespace, CodeEntityTypeDeclaration entityClass)
        {
            CodeEntityInterfaceDeclaration entityInterface = new CodeEntityInterfaceDeclaration(entityClass);
            CodeEntityInterfaceDeclaration entityPropertiesInterface = new CodeEntityInterfaceDeclaration(entityClass, null, "Properties");
            entityInterface.Attributes = entityPropertiesInterface.Attributes = MemberAttributes.Public;
            entityInterface.TypeAttributes = entityPropertiesInterface.TypeAttributes = TypeAttributes.Public | TypeAttributes.Interface;

            entityInterface.BaseTypes.Add(entityPropertiesInterface.TypeReference);
            if (entityClass.Entity.HasSinglePk)
                entityInterface.BaseTypes.Add(new CodeTypeReference(typeof(_IKeyEntity)));
            else if (entityClass.Entity.HasPk)
                entityInterface.BaseTypes.Add(new CodeTypeReference(typeof(_ICachedEntity)));
            else
                entityInterface.BaseTypes.Add(new CodeTypeReference(typeof(_IEntity)));

            entityClass.EntityInterfaceDeclaration = entityInterface;
            entityClass.EntityPropertiesInterfaceDeclaration = entityPropertiesInterface;
            entityNamespace.Types.Add(entityInterface);
            entityNamespace.Types.Add(entityPropertiesInterface);
        }

        #region M2M methods
        private void CreateM2MMethodsSet(EntityDescription entity, CodeTypeDeclaration entityClass)
        {
            foreach (RelationDescriptionBase relation in entity.GetAllRelations(false))
            {
                if (!relation.HasAccessors)
                    continue;
                CreateM2MGenMethods(CreateM2MAddMethod, entity, relation, entityClass);
                CreateM2MGenMethods(CreateM2MRemoveMethod, entity, relation, entityClass);
                CreateM2MGenMethods(CreateM2MGetMethod, entity, relation, entityClass);
                CreateM2MGenMethods(CreateM2MMergeMethod, entity, relation, entityClass);
            }
        }

        private delegate CodeMemberMethod CreateM2MMethodDelegate(string accessorName, EntityDescription relatedEntity, TypeDescription relatedEntityType, bool? direct);

        private void CreateM2MGenMethods(CreateM2MMethodDelegate del, EntityDescription entity, RelationDescriptionBase relation, CodeTypeDeclaration entityClass)
        {
            RelationDescription rel = relation as RelationDescription;
            if (rel != null)
            {
                LinkTarget link = rel.Left.Entity == entity ? rel.Right : rel.Left;
                string accessorName = link.AccessorName;
                EntityDescription relatedEntity = link.Entity;
                if (!string.IsNullOrEmpty(accessorName))
                    entityClass.Members.Add(del(accessorName, relatedEntity, link.AccessedEntityType, null));
            }

            SelfRelationDescription selfRel = relation as SelfRelationDescription;
            if (selfRel != null)
            {

                if (!string.IsNullOrEmpty(selfRel.Direct.AccessorName))
                    entityClass.Members.Add(del(selfRel.Direct.AccessorName, entity, selfRel.Direct.AccessedEntityType, true));
                if (!string.IsNullOrEmpty(selfRel.Reverse.AccessorName))
                    entityClass.Members.Add(del(selfRel.Reverse.AccessorName, entity, selfRel.Reverse.AccessedEntityType, false));
            }
        }

        private CodeMemberMethod CreateM2MAddMethod(string accessorName, EntityDescription relatedEntity, TypeDescription relatedEntityType, bool? direct)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = "Add" + accessorName;

            CodeParameterDeclarationExpression relObjectPrm = new CodeParameterDeclarationExpression();
            method.Parameters.Add(relObjectPrm);
            method.Attributes = MemberAttributes.Public;
            relObjectPrm.Name = accessorName.ToLower();
            if (relatedEntity.UseGenerics)
            {
                CodeTypeParameter typePrm = new CodeTypeParameter("T" + accessorName);
                typePrm.Constraints.Add(new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(relatedEntity, true)));
                typePrm.HasConstructorConstraint = true;
                method.TypeParameters.Add(typePrm);
                relObjectPrm.Type = new CodeTypeReference(typePrm);
            }
            else
            {
                relObjectPrm.Type = new CodeTypeReference(relatedEntityType == null ? OrmCodeGenNameHelper.GetEntityClassName(relatedEntity, true) : relatedEntityType.TypeName);
            }
            CodeMethodInvokeExpression methodInvokeExpression = new CodeMethodInvokeExpression(
                            new CodePropertyReferenceExpression(
                                new CodeThisReferenceExpression(),
                                "M2M"
                                ),
                            "Add",
                            new CodeArgumentReferenceExpression(relObjectPrm.Name)
                            );
            if (direct.HasValue)
            {
                methodInvokeExpression.Parameters.Add(new CodePrimitiveExpression(direct.Value));
            }
            method.Statements.Add(
                new CodeExpressionStatement(
                    methodInvokeExpression
                    )
                );
            return method;
        }

        private CodeMemberMethod CreateM2MRemoveMethod(string accessorName, EntityDescription relatedEntity, TypeDescription relatedEntityType, bool? direct)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = "Remove" + accessorName;

            CodeParameterDeclarationExpression relObjectPrm = new CodeParameterDeclarationExpression();
            method.Parameters.Add(relObjectPrm);
            method.Attributes = MemberAttributes.Public;
            relObjectPrm.Name = accessorName.ToLower();
            if (relatedEntity.UseGenerics)
            {
                CodeTypeParameter typePrm = new CodeTypeParameter("T" + accessorName);
                typePrm.Constraints.Add(new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(relatedEntity, true)));
                typePrm.HasConstructorConstraint = true;
                method.TypeParameters.Add(typePrm);
                relObjectPrm.Type = new CodeTypeReference(typePrm);
            }
            else
            {
                relObjectPrm.Type = new CodeTypeReference(relatedEntityType == null ? OrmCodeGenNameHelper.GetEntityClassName(relatedEntity, true) : relatedEntityType.TypeName);
            }
            CodeMethodInvokeExpression methodInvokeExpression = new CodeMethodInvokeExpression(
                new CodePropertyReferenceExpression(
                    new CodeThisReferenceExpression(),
                    "M2M"
                    ),
                "Delete",
                new CodeArgumentReferenceExpression(relObjectPrm.Name)
                );
            if (direct.HasValue)
            {
                methodInvokeExpression.Parameters.Add(new CodePrimitiveExpression(direct.Value));
            }
            method.Statements.Add(
                new CodeExpressionStatement(
                    methodInvokeExpression
                    )
                );
            return method;
        }

        private CodeMemberMethod CreateM2MGetMethod(string accessorName, EntityDescription relatedEntity, TypeDescription relatedEntityType, bool? direct)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = "Get" + OrmCodeGenNameHelper.GetMultipleForm(accessorName);
            method.Attributes = MemberAttributes.Public;
            CodeTypeReference callType;
            if (relatedEntity.UseGenerics)
            {
                CodeTypeParameter typePrm = new CodeTypeParameter("T" + accessorName);
                typePrm.Constraints.Add(new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(relatedEntity, true)));
                typePrm.HasConstructorConstraint = true;
                method.TypeParameters.Add(typePrm);
                callType = new CodeTypeReference(typePrm);
            }
            else
            {
                callType = new CodeTypeReference(relatedEntityType == null ? OrmCodeGenNameHelper.GetEntityClassName(relatedEntity, true) : relatedEntityType.TypeName);
            }
            method.ReturnType = new CodeTypeReference(typeof(ReadOnlyList<>).FullName, callType);
            CodeMethodInvokeExpression methodInvokeExpression = new CodeMethodInvokeExpression(
                            new CodeMethodReferenceExpression(
                                new CodePropertyReferenceExpression(
                                    new CodeThisReferenceExpression(),
                                    "M2M"
                                    ),
                                "Find",
                                callType),
                            new CodePrimitiveExpression(null),
                            new CodePrimitiveExpression(null),
                            new CodePrimitiveExpression(false)
                            );
            if (direct.HasValue)
            {
                methodInvokeExpression.Parameters.Add(new CodePrimitiveExpression(direct.Value));
            }
            method.Statements.Add(
                new CodeMethodReturnStatement(
                    methodInvokeExpression
                    )
                );
            return method;
        }

        private CodeMemberMethod CreateM2MMergeMethod(string accessorName, EntityDescription relatedEntity, TypeDescription relatedEntityType, bool? direct)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = "Merge" + OrmCodeGenNameHelper.GetMultipleForm(accessorName);
            method.Attributes = MemberAttributes.Public;

            CodeParameterDeclarationExpression colPrm = new CodeParameterDeclarationExpression();
            method.Parameters.Add(colPrm);
            method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(bool)), "removeNotInList"));

            colPrm.Name = "col";

            CodeTypeReference callType;

            if (relatedEntity.UseGenerics)
            {
                CodeTypeParameter typePrm = new CodeTypeParameter("T" + accessorName);
                typePrm.Constraints.Add(new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(relatedEntity, true)));
                typePrm.HasConstructorConstraint = true;
                method.TypeParameters.Add(typePrm);
                callType = new CodeTypeReference(typePrm);


            }
            else
            {
                callType = new CodeTypeReference(relatedEntityType == null ? OrmCodeGenNameHelper.GetEntityClassName(relatedEntity, true) : relatedEntityType.TypeName);
            }
            colPrm.Type = new CodeTypeReference(typeof(ReadOnlyList<>).FullName, callType);
            CodeMethodInvokeExpression methodInvokeExpression = new CodeMethodInvokeExpression(
                            new CodeMethodReferenceExpression(
                                new CodePropertyReferenceExpression(
                                    new CodeThisReferenceExpression(),
                                    "M2M"
                                    ),
                                "Merge",
                                callType),
                            new CodeArgumentReferenceExpression(colPrm.Name),
                            new CodeArgumentReferenceExpression("removeNotInList")
                            );
            if (direct.HasValue)
            {
                methodInvokeExpression.Parameters.Add(new CodePrimitiveExpression(direct.Value));
            }
            method.Statements.Add(
                new CodeExpressionStatement(
                    methodInvokeExpression
                    )
                );
            return method;
        }
        #endregion

        private void ImplementIRelation(RelationDescription relation, EntityDescription entity, 
            CodeTypeDeclaration entitySchemaDefClass)
        {
            var leftProp = entity.ActiveProperties.Find(match => match.FieldName == relation.Left.FieldName);
            var rightProp = entity.ActiveProperties.Find(match => match.FieldName == relation.Right.FieldName);

            if (leftProp != null && rightProp != null)
            {
                entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(typeof(IRelation)));

                #region Pair<string, Type> GetFirstType()
                CodeMemberMethod method = new CodeMemberMethod();
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
                            OrmCodeGenHelper.GetFieldNameReferenceExpression(leftProp),
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
                            OrmCodeGenHelper.GetFieldNameReferenceExpression(rightProp),
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
        }

        private void ImplementIRelation(SelfRelationDescription relation, EntityDescription entity, CodeTypeDeclaration entitySchemaDefClass)
        {
            entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(typeof(IRelation)));
            #region Pair<string, Type> GetFirstType()
            CodeMemberMethod method = new CodeMemberMethod();
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
                                match => match.FieldName == relation.Direct.FieldName)),
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
                        OrmCodeGenHelper.GetFieldNameReferenceExpression(entity.CompleteEntity.Properties.Find(match => match.FieldName == relation.Reverse.FieldName)),
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
                            new CodePrimitiveExpression(false)
                        )
                    )
                );
            #endregion Pair<string, Type> GetSecondType()
        }

        private static CodeMemberMethod CreateGetValueMethod(CodeEntityTypeDeclaration entityClass)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = "GetValueOptimized";
            method.ReturnType = new CodeTypeReference(typeof(object));
            method.Attributes = MemberAttributes.Public;
            if (entityClass.Entity.BaseEntity != null)
                method.Attributes |= MemberAttributes.Override;
            else
                method.ImplementationTypes.Add(new CodeTypeReference(typeof(IOptimizedValues)));
            CodeParameterDeclarationExpression prm = new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof(string)),
                "propertyAlias"
                );
            method.Parameters.Add(prm);

            prm = new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof(IEntitySchema)),
                "schema"
                );
            method.Parameters.Add(prm);

            if (entityClass.Entity.BaseEntity != null)
            {
                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeMethodInvokeExpression(
                            new CodeBaseReferenceExpression(),
                            method.Name,
                            new CodeArgumentReferenceExpression("propertyAlias"),
                            new CodeArgumentReferenceExpression("schema")
                            )
                        )
                    );
            }
            else
            {
                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeMethodInvokeExpression(
                            new CodePropertyReferenceExpression(
                                new CodeThisReferenceExpression(),
                                "MappingEngine"
                            ),
                            "GetPropertyValue",
                            new CodeThisReferenceExpression(),
                            new CodeArgumentReferenceExpression("propertyAlias")
                        )
                    )
                );
            }
            entityClass.Members.Add(method);
            return method;
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
                method.ImplementationTypes.Add(typeof(IEntitySchemaBase));
                // ���������
                method.Parameters.Add(
                    new CodeParameterDeclarationExpression(
                        new CodeTypeReference(typeof(EntityPropertyAttribute)),
                        "c"
                        )
                    );
                method.Parameters.Add(
                    new CodeParameterDeclarationExpression(
                        new CodeTypeReference(typeof(object)),
                        "value"
                        )
                    );
                CodeParameterDeclarationExpression methodParam = new CodeParameterDeclarationExpression(
                    new CodeTypeReference(typeof(object)),
                    "newvalue"
                    );

                methodParam.Direction = FieldDirection.Ref;
                method.Parameters.Add(methodParam);
                method.Statements.Add(
                    new CodeConditionStatement(
                        new CodeBinaryOperatorExpression(
                            new CodeBinaryOperatorExpression(
                                new CodeBinaryOperatorExpression(
                                    new CodePropertyReferenceExpression(
                                        new CodeArgumentReferenceExpression("c"),
                                        "Behavior"
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

        private void CreateProperties(EntityDescription entity, CodeEntityTypeDeclaration entityClass,
            CodeMemberMethod setvalueMethod, CodeMemberMethod getvalueMethod,
            CodeTypeDeclaration propertiesClass, CodeTypeDeclaration fieldsClass,
            CodeTypeDeclaration propertyAliasClass, CodeTypeDeclaration instancedPropertyAliasClass)
        {
            foreach (PropertyDescription propertyDesc in
                from k in entity.CompleteEntity.Properties
                where !k.Disabled
                select k)
            {

                #region �������� �������� � etc

                FilterPropertyName(propertyDesc);

                var propertyNameField = new CodeMemberField
                                            {
                                                Type = new CodeTypeReference(typeof(string)),
                                                Name = propertyDesc.PropertyAlias,
                                                InitExpression = new CodePrimitiveExpression(propertyDesc.PropertyAlias),
                                                Attributes = (MemberAttributes.Public | MemberAttributes.Const)
                                            };

                propertiesClass.Members.Add(propertyNameField);

                if (!string.IsNullOrEmpty(propertyDesc.Description))
                    SetMemberDescription(propertyNameField, propertyDesc.Description);
                if (propertyAliasClass != null && instancedPropertyAliasClass != null)
                {
                    var propertyAliasProperty = new CodeMemberProperty
                                                    {
                                                        Name = propertyDesc.PropertyAlias,
                                                        Type =
                                                            new CodeTypeReference(typeof(ObjectProperty)),
                                                        HasGet = true,
                                                        HasSet = false,
                                                        Attributes = MemberAttributes.Public | MemberAttributes.Final,
                                                    };
                    propertyAliasProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeObjectCreateExpression(propertyAliasProperty.Type, new CodeThisReferenceExpression(), OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc))));
                    propertyAliasClass.Members.Add(propertyAliasProperty);

                    var instancedPropertyAliasProperty = new CodeMemberProperty
                    {
                        Name = propertyDesc.PropertyAlias,
                        Type =
                            new CodeTypeReference(typeof(ObjectProperty)),
                        HasGet = true,
                        HasSet = false,
                        Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    };
                    instancedPropertyAliasProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeObjectCreateExpression(propertyAliasProperty.Type, new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), OrmCodeGenNameHelper.GetPrivateMemberName("objectAlias")), OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc))));
                    instancedPropertyAliasClass.Members.Add(instancedPropertyAliasProperty);
                }

                if (fieldsClass != null)
                {
                    Type type = typeof(ObjectProperty);
                    var propConst = new CodeMemberField(type, OrmCodeGenNameHelper.GetPrivateMemberName(propertyDesc.PropertyAlias))
                                        {
                                            InitExpression = new CodeObjectCreateExpression(type, Settings.UseTypeInProps ? OrmCodeGenHelper.GetEntityClassTypeReferenceExpression(entity) : OrmCodeGenHelper.GetEntityNameReferenceExpression(entity), OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc)),
                                            Attributes = (MemberAttributes.Private | MemberAttributes.Static | MemberAttributes.Final)
                                        };

                    fieldsClass.Members.Add(propConst);

                    var prop = new CodeMemberProperty
                                   {
                                       Attributes = (MemberAttributes.Public | MemberAttributes.Static | MemberAttributes.Final),
                                       HasGet = true,
                                       HasSet = false,
                                       Name = propertyDesc.PropertyAlias,
                                       Type = new CodeTypeReference(type)
                                   };
                    if (!string.IsNullOrEmpty(propertyDesc.Description))
                        SetMemberDescription(prop, propertyDesc.Description);
                    prop.GetStatements.Add(
                        new CodeMethodReturnStatement(
                            new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(OrmCodeGenNameHelper.GetEntityClassName(entity, true) + "." +
                                                             fieldsClass.Name), propConst.Name)));
                    fieldsClass.Members.Add(prop);
                }

                #endregion �������� �������� � etc

                CodeMemberProperty property = null;
                if (!propertyDesc.FromBase)
                    property = CreateProperty(entityClass, propertyDesc, entity, setvalueMethod, getvalueMethod);
                else if (propertyDesc.IsRefreshed)
                    //CreateProperty(copyMethod, createobjectMethod, entityClass, propertyDesc, settings, setvalueMethod, getvalueMethod);
                    property = CreateUpdatedProperty(entityClass, propertyDesc);

                if (property != null)
                {
                    #region property custom attribute Worm.Orm.EntityPropertyAttribute

                    CreatePropertyColumnAttribute(property, propertyDesc);

                    #endregion property custom attribute Worm.Orm.EntityPropertyAttribute

                    #region property obsoletness

                    CheckPropertyObsoleteAttribute(property, propertyDesc);

                    #endregion
                }
            }
        }

        private void FilterPropertyName(PropertyDescription propertyDesc)
        {
            if (propertyDesc.PropertyName == "Identifier")
                throw new ArgumentException("Used reserved property name 'Identifier'");
        }

        private void RaisePropertyCreated(PropertyDescription propertyDesc, CodeEntityTypeDeclaration entityClass, CodeMemberProperty property, CodeMemberField field)
        {
            EventHandler<EntityPropertyCreatedEventArgs> h = s_ctrl.EventDelegates[EntityGeneratorController.PropertyCreatedKey] as EventHandler<EntityPropertyCreatedEventArgs>;
            if (h != null)
            {
                h(this, new EntityPropertyCreatedEventArgs(propertyDesc, entityClass, field, property));
            }
        }

        private void CheckPropertyObsoleteAttribute(CodeMemberProperty property, PropertyDescription propertyDesc)
        {
            if (propertyDesc.Obsolete != ObsoleteType.None)
            {
                CodeAttributeDeclaration attr =
                    new CodeAttributeDeclaration(new CodeTypeReference(typeof(ObsoleteAttribute)),
                                                 new CodeAttributeArgument(
                                                    new CodePrimitiveExpression(propertyDesc.ObsoleteDescripton)),
                                                 new CodeAttributeArgument(
                                                    new CodePrimitiveExpression(propertyDesc.Obsolete == ObsoleteType.Error)));
                if (property.CustomAttributes == null)
                    property.CustomAttributes = new CodeAttributeDeclarationCollection();
                property.CustomAttributes.Add(attr);
            }
        }

        private CodeMemberProperty CreateUpdatedProperty(CodeEntityTypeDeclaration entityClass, PropertyDescription propertyDesc)
        {
            //propertyType = new CodeTypeReference(propertyDesc.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetEntityClassName(propertyDesc.PropertyType.Entity, true) : propertyDesc.PropertyType.TypeName);
            CodeTypeReference propertyType = propertyDesc.PropertyType;

            CodeMemberProperty property = new CodeMemberProperty();

            property.HasGet = true;
            property.HasSet = true;

            property.Name = propertyDesc.PropertyName;
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
            if (!propertyDesc.HasAttribute(Field2DbRelations.ReadOnly))
            {

                property.SetStatements.Add(
                    new CodeAssignStatement(
                        new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), property.Name),
                        new CodePropertySetValueReferenceExpression()
                        )
                    );
            }
            else
            {
                property.HasSet = false;
            }

            if (propertyDesc.Group != null && propertyDesc.Group.Hide)
                property.Attributes = MemberAttributes.Family;

            RaisePropertyCreated(propertyDesc, entityClass, property, null);

            #region ���������� ������ � �����
            entityClass.Members.Add(property);
            #endregion ���������� ������ � �����

            return property;
        }

        private CodeMemberProperty CreateProperty(CodeEntityTypeDeclaration entityClass,
            PropertyDescription propertyDesc, EntityDescription entity,
            CodeMemberMethod setvalueMethod, CodeMemberMethod getvalueMethod)
        {
            //fieldType = new CodeTypeReference(propertyDesc.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetEntityClassName(propertyDesc.PropertyType.Entity, true) : propertyDesc.PropertyType.TypeName);
            CodeTypeReference fieldType = propertyDesc.PropertyType;
            string fieldName = OrmCodeGenNameHelper.GetPrivateMemberName(propertyDesc.PropertyName);

            CodeMemberField field = new CodeMemberField(fieldType, fieldName);
            field.Attributes = GetMemberAttribute(propertyDesc.FieldAccessLevel);

            CodeMemberProperty property = new CodeMemberProperty();

            property.HasGet = true;
            property.HasSet = true;

            property.Name = propertyDesc.PropertyName;
            property.Type = fieldType;
            property.Attributes = GetMemberAttribute(propertyDesc.PropertyAccessLevel);
            if (propertyDesc.Group != null && propertyDesc.Group.Hide)
                property.Attributes = MemberAttributes.Family;

            if (entity.GetPropertiesFromBase().Exists(k => propertyDesc.Name == k.Name))
            {
                property.Attributes |= MemberAttributes.Override;
            }

            #region property GetStatements

            CodeMethodInvokeExpression getUsingExpression = new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "Read"),
                OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc)
                );

            if (propertyDesc.PropertyType.IsEntityType && propertyDesc.PropertyType.Entity.CacheCheckRequired)
            {
                getUsingExpression.Parameters.Add(new CodePrimitiveExpression(true));
            }

            CodeStatement[] getInUsingStatements = new CodeStatement[]
                {
                    new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName))
                };

            if (entity.HasPkFlatEntity)
                property.GetStatements.AddRange(Delegates.CodePatternUsingStatements(getUsingExpression, getInUsingStatements));
            else
                property.GetStatements.AddRange(getInUsingStatements);

            #endregion property GetStatements

            #region property SetStatements

            if (_ormObjectsDefinition.EnableReadOnlyPropertiesSetter || !propertyDesc.HasAttribute(Field2DbRelations.ReadOnly) || propertyDesc.HasAttribute(Field2DbRelations.PK))
            {
                CodeExpression setUsingExpression = new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "Write"),
                    OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc)
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

                if (entity.HasPkFlatEntity)
                    property.SetStatements.AddRange(Delegates.CodePatternUsingStatements(setUsingExpression, setInUsingStatements));
                else
                    property.SetStatements.AddRange(setInUsingStatements);
            }
            else
                property.HasSet = false;

            #endregion property SetStatements

            RaisePropertyCreated(propertyDesc, entityClass, property, field);

            #region ���������� ������ � �����
            entityClass.Members.Add(field);
            entityClass.Members.Add(property);
            #endregion ���������� ������ � �����

            #region void SetValue(System.Reflection.PropertyInfo pi, EntityPropertyAttribute c, object value)

            Delegates.UpdateSetValueMethodMethod(propertyDesc, setvalueMethod);

            #endregion void SetValue(System.Reflection.PropertyInfo pi, EntityPropertyAttribute c, object value)

            #region public override object GetValue(string propAlias, Worm.Orm.IOrmObjectsSchema schema)

            UpdateGetValueMethod(propertyDesc, getvalueMethod);

            #endregion public override object GetValue(string propAlias, Worm.Orm.IOrmObjectsSchema schema)

            //#region void CreateObject(string fieldName, object value)
            //if (Array.IndexOf(propertyDesc.Attributes, "Factory") != -1)
            //{
            //    UpdateCreateObjectMethod(createobjectMethod, propertyDesc);
            //}

            //#endregion void CreateObject(string fieldName, object value)

            return property;
        }

        private void UpdateGetValueMethod(PropertyDescription propertyDesc, CodeMemberMethod getvalueMethod)
        {
            getvalueMethod.Statements.Insert(getvalueMethod.Statements.Count - 1,
                new CodeConditionStatement(
                    new CodeMethodInvokeExpression(
                        OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc),
                        "Equals",
                        new CodeArgumentReferenceExpression("propertyAlias")
                    ),
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), OrmCodeGenNameHelper.GetPrivateMemberName(propertyDesc.PropertyName))
                    )
                )
            );
        }
        private void CreatePropertyColumnAttribute(CodeMemberProperty property, PropertyDescription propertyDesc)
        {
            CodeAttributeDeclaration declaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(EntityPropertyAttribute)));

            if (!string.IsNullOrEmpty(propertyDesc.PropertyAlias))
            {
                declaration.Arguments.Add(
                    new CodeAttributeArgument("PropertyAlias", OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc)));
            }

            property.CustomAttributes.Add(declaration);
        }

        //private static void UpdateCreateObjectMethod(CodeMemberMethod createobjectMethod, PropertyDescription propertyDesc)
        //{
        //    if (createobjectMethod == null)
        //        return;
        //    createobjectMethod.Statements.Insert(createobjectMethod.Statements.Count - 1,
        //        new CodeConditionStatement(
        //            new CodeBinaryOperatorExpression(
        //                new CodeArgumentReferenceExpression("fieldName"),
        //                CodeBinaryOperatorType.ValueEquality,
        //                new CodePrimitiveExpression(propertyDesc.PropertyAlias)
        //                ),
        //            new CodeThrowExceptionStatement(
        //                new CodeObjectCreateExpression(
        //                    new CodeTypeReference(typeof(NotImplementedException)),
        //                    new CodePrimitiveExpression("The method or operation is not implemented.")
        //                    )
        //                )
        //            )
        //        );
        //}

        //private static CodeMemberMethod CreateCreateObjectMethod(EntityDescription entity, CodeTypeDeclaration entityClass)
        //{
        //    CodeMemberMethod createobjectMethod;
        //    createobjectMethod = new CodeMemberMethod();
        //    if (entity.Behaviour != EntityBehaviuor.PartialObjects)
        //        entityClass.Members.Add(createobjectMethod);
        //    createobjectMethod.Name = "CreateObject";
        //    // ��� ������������� ��������
        //    createobjectMethod.ReturnType = null;
        //    // ������������ �������
        //    createobjectMethod.Attributes = MemberAttributes.Public | MemberAttributes.Override;

        //    createobjectMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "fieldName"));
        //    createobjectMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "value"));

        //    createobjectMethod.Statements.Add(
        //        new CodeThrowExceptionStatement(
        //            new CodeObjectCreateExpression(
        //                new CodeTypeReference(typeof(InvalidOperationException)),
        //                new CodePrimitiveExpression("Invalid method usage.")
        //            )
        //        )
        //        );

        //    return createobjectMethod;
        //}

        private static CodeMemberMethod CreateSetValueMethod(CodeEntityTypeDeclaration entityClass)
        {
            CodeMemberMethod setvalueMethod = new CodeMemberMethod();
            entityClass.Members.Add(setvalueMethod);
            setvalueMethod.Name = "SetValueOptimized";
            // ��� ������������� ��������
            setvalueMethod.ReturnType = null;
            // ������������ �������
            setvalueMethod.Attributes = MemberAttributes.Public;
            if (entityClass.Entity.BaseEntity != null)
                setvalueMethod.Attributes |= MemberAttributes.Override;
            else
                setvalueMethod.ImplementationTypes.Add(new CodeTypeReference(typeof(IOptimizedValues)));
            setvalueMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "propertyAlias"));
            setvalueMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IEntitySchema), "schema"));
            setvalueMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "value"));
            setvalueMethod.Statements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference(typeof(string)),
                    "fieldName",
                    new CodeArgumentReferenceExpression("propertyAlias")
                )
            );
            return setvalueMethod;
        }

        private void CreateTablesLinkEnum(EntityDescription entity, CodeTypeDeclaration entitySchemaDefClass)
        {
            if (!entity.InheritsBaseTables || entity.SourceFragments.Count > 0)
            {
                var fullTables = entity.CompleteEntity.SourceFragments;

                CodeTypeDeclaration tablesEnum = new CodeTypeDeclaration("TablesLink");
                tablesEnum.Attributes = MemberAttributes.Public;
                if (entity.BaseEntity != null)
                    tablesEnum.Attributes |= MemberAttributes.New;

                tablesEnum.IsClass = false;
                tablesEnum.IsEnum = true;
                tablesEnum.IsPartial = false;
                int tableNum = 0;

                tablesEnum.Members.AddRange(fullTables.ConvertAll<CodeTypeMember>(tbl => new CodeMemberField
                                                                                            {
                                                                                                InitExpression =
                                                                                                    new CodePrimitiveExpression(
                                                                                                    tableNum++),
                                                                                                Name =
                                                                                                    OrmCodeGenNameHelper.
                                                                                                    GetSafeName(tbl.Identifier)
                                                                                            }).ToArray());
                entitySchemaDefClass.Members.Add(tablesEnum);
            }
        }

        internal static void SetMemberDescription(CodeTypeMember member, string description)
        {
            if (string.IsNullOrEmpty(description))
                return;
            member.Comments.Add(new CodeCommentStatement(string.Format("<summary>\n{0}\n</summary>", description), true));
        }

        internal static MemberAttributes GetMemberAttribute(AccessLevel accessLevel)
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
    }
}
