using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text;
using Worm.CodeGen.Core.CodeDomExtensions;
using Worm.CodeGen.Core.Descriptors;
using Worm.Orm;
using Worm.Collections;
using Worm.Orm.Meta;
using Worm.Cache;

namespace Worm.CodeGen.Core
{

    public partial class OrmCodeDomGenerator
    {
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

        public Dictionary<string, CodeCompileUnit> GetFullDom()
        {
            var result = new Dictionary<string, CodeCompileUnit>(_ormObjectsDefinition.Entities.Count * (Settings.Split?2:1));
            foreach (EntityDescription entity in _ormObjectsDefinition.Entities)
            {
                foreach (KeyValuePair<string, CodeCompileUnit> pair in GetEntityDom(entity.Identifier, Settings))
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

    	[ThreadStatic] private static EntityGeneratorController s_ctrl;

		public CodeCompileUnit GetFullSingleUnit()
		{
			CodeCompileUnit unit = new CodeCompileUnit();
			foreach (CodeCompileUnit u in GetFullDom().Values)
			{
				foreach (CodeNamespace n in u.Namespaces)
				{
					unit.Namespaces.Add(n);
				}
			}
			return unit;
		}

        public CodeCompileFileUnit GetLinqContext(OrmCodeDomGeneratorSettings settings)
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

			        CodeCompileFileUnit entityUnit;
			        CodeNamespace nameSpace;
			        CodeTypeDeclaration entitySchemaDefClass;
			        CodeEntityTypeDeclaration entityClass;
			        CodeTypeDeclaration propertiesClass;
			        CodeTypeDeclaration fieldsClass = null;
			        CodeTypeDeclaration propertyAliasClass = null;
                    CodeTypeDeclaration instancedPropertyAliasClass = null;

			        CodeConstructor ctr;
			        CodeMemberMethod method;
			        CodeMemberField field;

			        #region определение класса сущности

			        entityUnit = new CodeCompileFileUnit
			                         {
			                             Filename = OrmCodeGenNameHelper.GetEntityFileName(entity)
			                         };
			        result.Add(entityUnit);

			        // неймспейс
			        nameSpace = new CodeNamespace(entity.Namespace);
			        entityUnit.Namespaces.Add(nameSpace);

			        // импорты
			        //nameSpace.Imports.Add(new CodeNamespaceImport("System"));
			        //nameSpace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
			        //nameSpace.Imports.Add(new CodeNamespaceImport("Worm.Orm"));

			        // класс сущности
			        entityClass = new CodeEntityTypeDeclaration(entity);
			        nameSpace.Types.Add(entityClass);

			        // параметры класса
			        entityClass.IsClass = true;
			        entityClass.IsPartial = entity.Behaviour == EntityBehaviuor.PartialObjects ||
			                                entity.Behaviour == EntityBehaviuor.ForcePartial ||
			                                Settings.Split;
			        entityClass.Attributes = MemberAttributes.Public;
			        entityClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.Public;

			        //if (entity.Behaviour == EntityBehaviuor.Abstract)
			        //{
			        //    entityClass.Attributes |= MemberAttributes.Abstract;
			        //    entityClass.TypeAttributes |= TypeAttributes.Abstract;
			        //}

			        // дескрипшн
			        SetMemberDescription(entityClass, entity.Description);

			        // интерфес сущности

			        // базовый класс
			        if (entity.BaseEntity == null)
			        {
			            //entityClass.BaseTypes.Add(new CodeTypeReference(typeof(OrmBaseT)));
			            CodeTypeReference entityType;
			            if (_ormObjectsDefinition.EntityBaseType == null)
			            {
                            if(entity.HasSinglePK)
                                entityType = new CodeTypeReference(typeof(Worm.Orm.OrmBase));
			                else
			                    entityType = new CodeTypeReference(typeof (Worm.Orm.CachedEntity));
                            //else if(entity.HasIntPK)
                                
                            //else
                            //{
                            //    entityType = new CodeTypeReference(typeof (Worm.Orm.OrmBaseT<>));
                            //    entityType.TypeArguments.Add(
                            //        new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity)));
                            //}

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
			        }

			        else
			            entityClass.BaseTypes.Add(
			                new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity.BaseEntity, true)));


                    //if (entity.BaseEntity == null)
                    //{
                    //    if (!entity.HasCompositePK && !entity.HasIntPK)
                    //    {
                    //        CodeTypeReference iOrmEditableType = new CodeTypeReference(typeof (IOrmEditable<>));
                    //        iOrmEditableType.TypeArguments.Add(
                    //            new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity, true)));

                    //        entityClass.BaseTypes.Add(iOrmEditableType);
                    //    }
                    //}

			        #endregion определение класса сущности

			        RaiseEntityClassCreated(nameSpace, entityClass);

			        #region определение схемы

			        entitySchemaDefClass =
			            new CodeTypeDeclaration(OrmCodeGenNameHelper.GetEntitySchemaDefClassName(entity));

			        entitySchemaDefClass.IsClass = true;
			        entitySchemaDefClass.IsPartial = entityClass.IsPartial;
			        entitySchemaDefClass.Attributes = entityClass.Attributes;
			        if (entity.BaseEntity != null)
			            entitySchemaDefClass.Attributes |= MemberAttributes.New;
			        entitySchemaDefClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.NestedPublic;

			        #endregion определение схемы

			        #region определение класса Properties

			        {
			            propertiesClass = new CodeTypeDeclaration("Properties");
			            propertiesClass.Attributes = MemberAttributes.Public;
			            propertiesClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.NestedPublic;
			            propertiesClass.IsPartial = true;
			            CodeConstructor propctr = new CodeConstructor();
			            propctr.Attributes = MemberAttributes.Family;
			            propertiesClass.Members.Add(propctr);
			            SetMemberDescription(propertiesClass, "Алиасы свойств сущностей испльзуемые в объектной модели.");

                        if (entity.BaseEntity != null)
                            propertiesClass.Attributes |= MemberAttributes.New;

			            entityClass.Members.Add(propertiesClass);
			        }

			        #endregion определение класса Properties

                    #region определение класса Fields
                    if(_ormObjectsDefinition.GenerateEntityName)
                    {
                        fieldsClass = new CodeTypeDeclaration("Props")
                                          {
                                              Attributes = MemberAttributes.Public,
                                              TypeAttributes = (TypeAttributes.Class | TypeAttributes.NestedPublic),
                                              IsPartial = true
                                          };
                        var propctr = new CodeConstructor {Attributes = MemberAttributes.Family};
                        fieldsClass.Members.Add(propctr);
                        
                        SetMemberDescription(fieldsClass, "Ссылки на поля сущностей.");

                        entityClass.Members.Add(fieldsClass);

                        if (entity.BaseEntity != null)
                            fieldsClass.Attributes |= MemberAttributes.New;
                    }

                    #endregion определение класса Fields

                    #region определение класса Descriptor
                    if (_ormObjectsDefinition.GenerateEntityName)
                    {
			            var descriptorClass = new CodeTypeDeclaration()
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

			            SetMemberDescription(descriptorClass, "Описатель сущности.");

                        var entityNameField = new CodeMemberField()
                        {
                            Type = new CodeTypeReference(typeof(string)),
                            Name = "EntityName",
                            InitExpression = new CodePrimitiveExpression(entity.Name),
                            Attributes = (MemberAttributes.Public | MemberAttributes.Const)
                        };

                        descriptorClass.Members.Add(entityNameField);

                        SetMemberDescription(entityNameField, "Имя сущности в объектной модели.");

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
			                                         Name = entity.Name + "PropertyAliasBased",
			                                         Attributes = MemberAttributes.Family,
			                                     };

                        var propertyAliasClassCotr = new CodeConstructor { Attributes = MemberAttributes.Public };

			            propertyAliasClassCotr.BaseConstructorArgs.Add(OrmCodeGenHelper.GetEntityNameReferenceExpression(entity));
			            propertyAliasClass.Members.Add(propertyAliasClassCotr);
			            propertyAliasClass.BaseTypes.Add(new CodeTypeReference(typeof (ObjectAlias)));
                                                         

			            instancedPropertyAliasClass = new CodeTypeDeclaration
			                                              {
                                                              Name = entity.Name + "PropertyAlias",
                                                              Attributes = MemberAttributes.Family,
			                                              };

			            var instancedPropertyAliasClassCotr = new CodeConstructor{Attributes= MemberAttributes.Public};

                        instancedPropertyAliasClassCotr.Parameters.Add(
			                new CodeParameterDeclarationExpression(new CodeTypeReference(typeof (ObjectAlias)), "objectAlias"));

			            instancedPropertyAliasClassCotr.Statements.Add(
			                new CodeAssignStatement(
			                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
			                                                     OrmCodeGenNameHelper.GetPrivateMemberName("objectAlias")),
			                    new CodeArgumentReferenceExpression("objectAlias")));

			            instancedPropertyAliasClass.Members.Add(instancedPropertyAliasClassCotr);

			            instancedPropertyAliasClass.Members.Add(new CodeMemberField(new CodeTypeReference(typeof (ObjectAlias)),
			                                                       OrmCodeGenNameHelper.GetPrivateMemberName("objectAlias")));

                        if (entity.BaseEntity != null)
                        {
                            propertyAliasClass.Attributes |= MemberAttributes.New;
                            instancedPropertyAliasClass.Attributes |= MemberAttributes.New;
                        }

                        entityClass.Members.Add(propertyAliasClass);
                        entityClass.Members.Add(instancedPropertyAliasClass);
                    }

                    #endregion

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
			                                new CodeTypeReference(
			                                    OrmCodeGenNameHelper.GetEntitySchemaDefClassQualifiedName(entity))
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
			                        new CodeTypeReference(typeof (SerializableAttribute))
			                        )
			                }
			            );
			        //}

			        #endregion custom attribute EntityAttribute

                    #region OBjectAlias methods
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
			            getMethod.Parameters.Add(new CodeParameterDeclarationExpression
			                                         {Name = "objectAlias", Type = new CodeTypeReference(typeof (ObjectAlias))});

                        getMethod.Statements.Add(
                            new CodeMethodReturnStatement(
                                new CodeObjectCreateExpression(
                                    new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity, true) + "." +
                                                          instancedPropertyAliasClass.Name), new CodeArgumentReferenceExpression("objectAlias"))));
                        entityClass.Members.Add(getMethod);
                    }
                    #endregion

                    #region конструкторы

                    // конструктор по умолчанию
			        ctr = new CodeConstructor();
			        ctr.Attributes = MemberAttributes.Public;
			        entityClass.Members.Add(ctr);

			        RaiseEntityCtorCreated(entityClass, ctr);

			        //if(
			        if (entity.HasSinglePK)
			        {
			            // параметризированный конструктор
			            ctr = new CodeConstructor();
			            ctr.Attributes = MemberAttributes.Public;
			            // параметры конструктора
			            ctr.Parameters.Add(new CodeParameterDeclarationExpression(typeof (int), "id"));
			            ctr.Parameters.Add(new CodeParameterDeclarationExpression(typeof (CacheBase), "cache"));
			            ctr.Parameters.Add(new CodeParameterDeclarationExpression(typeof (Worm.ObjectMappingEngine),
			                                                                      "schema"));
                        
                        ctr.Statements.Add(new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), "Init",
                                                                          new CodeArgumentReferenceExpression("id"),
                                                                          new CodeArgumentReferenceExpression("cache"),
                                                                          new CodeArgumentReferenceExpression("schema")));
                        
                        entityClass.Members.Add(ctr);
			            RaiseEntityCtorCreated(entityClass, ctr);
			        }

			        #endregion конструкторы

			        #region метод OrmBase.CopyBody(CopyBody(...)

			        
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
			            // модификаторы доступа
			            copyMethod.Attributes = MemberAttributes.Family | MemberAttributes.Override;
			            copyMethod.Parameters.Add(
			                new CodeParameterDeclarationExpression(typeof (_IEntity),
			                                                       "from"));
			            copyMethod.Parameters.Add(
			                new CodeParameterDeclarationExpression(typeof (_IEntity),
			                                                       "to"));
			            copyMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof (OrmManager), "mgr"));
                        copyMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IObjectSchemaBase), "oschema"));

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
			                                       //#endregion // реализация метода Copy
			                                   };

                    //}
                    //else
                    //{
                    //    CodeMemberMethod copyMethod = null;
                    //    EntityDescription superbaseEntity;
                    //    for (superbaseEntity = entity;
                    //         superbaseEntity.BaseEntity != null;
                    //         superbaseEntity = superbaseEntity.BaseEntity)
                    //    {

                    //    }

                    //    bool isInitialImplemantation = entity == superbaseEntity;

                    //    copyMethod = new CodeMemberMethod();
                    //    entityClass.Members.Add(copyMethod);
                    //    copyMethod.Name = "CopyBody";
                    //    // тип возвращаемого значения
                    //    copyMethod.ReturnType = null;
                    //    // модификаторы доступа
                    //    copyMethod.Attributes = MemberAttributes.Public;
                    //    //if (entity.BaseEntity != null)
                    //    //    copyMethod.Attributes |= MemberAttributes.Override;

                    //    if (!entity.HasIntPK)
                    //    {
                    //        if (!isInitialImplemantation)
                    //        {
                    //            copyMethod.Attributes |= MemberAttributes.Override;
                    //        }
                    //        else
                    //        {
                    //            CodeTypeReference iOrmEditableType = new CodeTypeReference(typeof (IOrmEditable<>));
                    //            iOrmEditableType.TypeArguments.Add(
                    //                new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(superbaseEntity, true)));

                    //            copyMethod.ImplementationTypes.Add(iOrmEditableType);
                    //        }
                    //    }



                    //    CodeTypeReference targetEntityType =
                    //        new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(superbaseEntity, true));
                    //    CodeTypeReference entityType =
                    //        new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity, true));
                    //    copyMethod.Parameters.Add(
                    //        new CodeParameterDeclarationExpression(targetEntityType,
                    //                                               "from"));
                    //    copyMethod.Parameters.Add(
                    //        new CodeParameterDeclarationExpression(targetEntityType,
                    //                                               "to"));
                    //    if (!isInitialImplemantation)
                    //        copyMethod.Statements.Add(
                    //            new CodeMethodInvokeExpression(
                    //                new CodeBaseReferenceExpression(),
                    //                "CopyBody",
                    //                new CodeArgumentReferenceExpression("from"),
                    //                new CodeArgumentReferenceExpression("to")
                    //                )
                    //            );

                    //    PropertyCreated += delegate(object sender, EntityPropertyCreatedEventArgs e)
                    //                           {
                    //                               if (e.FieldMember == null) return;
                    //                               string fieldName = e.FieldMember.Name;

                    //                               CodeExpression leftTargetExpression =
                    //                                   new CodeArgumentReferenceExpression("to");

                    //                               CodeExpression rightTargetExpression =
                    //                                   new CodeArgumentReferenceExpression("from");

                    //                               if (!isInitialImplemantation)
                    //                               {
                    //                                   leftTargetExpression = new CodeCastExpression(entityType,
                    //                                                                                 leftTargetExpression);
                    //                                   rightTargetExpression = new CodeCastExpression(entityType,
                    //                                                                                  rightTargetExpression);
                    //                               }

                    //                               copyMethod.Statements.Add(
                    //                                   new CodeAssignStatement(
                    //                                       new CodeFieldReferenceExpression(leftTargetExpression,
                    //                                                                        fieldName),
                    //                                       new CodeFieldReferenceExpression(rightTargetExpression,
                    //                                                                        fieldName))
                    //                                   );
                    //                               //#endregion // реализация метода Copy
                    //                           };

                    //}

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

			        #region void SetValue(System.Reflection.PropertyInfo pi, string propertyAlias, object value)

			        CodeMemberMethod setvalueMethod = CreateSetValueMethod(entityClass);

                    #endregion void SetValue(System.Reflection.PropertyInfo pi, string propertyAlias, object value)

                    #region public override object GetValue(string propAlias, Worm.Orm.IOrmObjectSchemaBase schema)

                    CodeMemberMethod getvalueMethod = CreateGetValueMethod(entityClass);

			        #endregion public override object GetValue(string propAlias, Worm.Orm.IOrmObjectSchemaBase schema)

			        #region CachedEntity methods

			        CodeMemberMethod createobjectMethod = null;

			        if (!entity.HasSinglePK)
			        {
                        if (entity.BaseEntity == null)
                        {
                            CreateGetKeyMethodCompositePK(entityClass);
                            CreateGetPKValuesMethodCompositePK(entityClass);
                            CreateSetPKMethodCompositePK(entityClass);
                        }
                        else
                        {
                            UpdateGetKeyMethodCompositePK(entityClass);
                            UpdateGetPKValuesMethodCompositePK(entityClass);
                            UpdateSetPKMethodCompositePK(entityClass);
                        }
			        }
                    else
                    {
                        OverrideIdentifierProperty(entityClass);
                    }

			        #endregion

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

			        CreateProperties(createobjectMethod, entity, entityClass, setvalueMethod, getvalueMethod, propertiesClass, fieldsClass, propertyAliasClass, instancedPropertyAliasClass);

			        #endregion проперти

			        #region void SetValue(System.Reflection.PropertyInfo pi, ColumnAttribute c, object value)

			        setvalueMethod.Statements.Add(
			            new CodeMethodInvokeExpression(
			                new CodeMethodReferenceExpression(
			                    new CodeBaseReferenceExpression(),
			                    "SetValue"
			                    ),
			                new CodeArgumentReferenceExpression("pi"),
			                new CodeArgumentReferenceExpression("propertyAlias"),
			                new CodeArgumentReferenceExpression("schema"),
			                new CodeArgumentReferenceExpression("value")
			                )
			            );

			        #endregion void SetValue(System.Reflection.PropertyInfo pi, ColumnAttribute c, object value)

			        #region // TEntity Get<TEntity>(int id)

			        //CreateGetEntityMethod(entity, entityClass);

			        #endregion

			        #region m2m relation methods

			        CreateM2MMethodsSet(entity, entityClass);

			        #endregion

			        #region обработка директивы Split

			        ProcessSplitOption(entity, entityClass, ref entitySchemaDefClass, result);

			        #endregion обработка директивы Split

			        #region энам табличек

			        CreateTablesLinkEnum(entity, entitySchemaDefClass);

			        #endregion энам табличек

			        #region метод public SourceFragment GetTypeMainTable(Type type)

			        CreateGetTypeMainTableMethod(entity, entitySchemaDefClass);

			        #endregion метод public static SourceFragment GetMainTable()

			        #region поле _idx

			        field =
			            new CodeMemberField(
			                new CodeTypeReference(typeof (Worm.Collections.IndexedCollection<string, MapField2Column>)),
			                "_idx");
			        entitySchemaDefClass.Members.Add(field);

			        #endregion поле _idx

			        #region поле _tables

			        field = new CodeMemberField(new CodeTypeReference(typeof (SourceFragment[])), "_tables");
			        field.Attributes = MemberAttributes.Private;
			        entitySchemaDefClass.Members.Add(field);

			        #endregion поле _tables

			        #region метод SourceFragment[] GetTables()

			        CreateGetTablesMethod(entity, entitySchemaDefClass);

			        #endregion метод SourceFragment[] GetTables()

			        #region метод SourceFragment GetTable(...)

			        CreateGetTableMethod(entity, entitySchemaDefClass);

			        #endregion метод SourceFragment GetTable(...)

			        #region bool ChangeValueType(ColumnAttribute c, object value, ref object newvalue)

			        CreateChangeValueTypeMethod(entity, entitySchemaDefClass);

			        #endregion bool ChangeValueType(ColumnAttribute c, object value, ref object newvalue)

			        #region OrmJoin GetJoins(SourceFragment left, SourceFragment right)

			        if ((entity.Behaviour != EntityBehaviuor.PartialObjects || entity.Tables.Count == 1) &&
			            entity.BaseEntity == null)
			        {
			            method = new CodeMemberMethod();
			            entitySchemaDefClass.Members.Add(method);
			            method.Name = "GetJoins";
			            // тип возвращаемого значения
			            method.ReturnType = new CodeTypeReference(typeof (Worm.Criteria.Joins.OrmJoin));
			            // модификаторы доступа
			            method.Attributes = MemberAttributes.Public;
			            // реализует метод базового класса
			            method.ImplementationTypes.Add(typeof (IOrmObjectSchema));
			            // параметры
			            method.Parameters.Add(
			                new CodeParameterDeclarationExpression(
			                    new CodeTypeReference(typeof (SourceFragment)),
			                    "left"
			                    )
			                );
			            method.Parameters.Add(
			                new CodeParameterDeclarationExpression(
			                    new CodeTypeReference(typeof (SourceFragment)),
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
			                        new CodeDefaultValueExpression(
			                            new CodeTypeReference(typeof (Worm.Database.Criteria.Joins.OrmJoin)))
			                        )
			                    );
			            }
			        }

			        #endregion OrmJoin GetJoins(string left, string right)

			        #region string[] GetSuppressedColumns()

			        if (entity.Behaviour != EntityBehaviuor.PartialObjects)
			        {

			            method = new CodeMemberMethod();
			            entitySchemaDefClass.Members.Add(method);
			            method.Name = "GetSuppressedFields";
			            // тип возвращаемого значения
			            method.ReturnType = new CodeTypeReference(typeof (string[]));
			            // модификаторы доступа
			            method.Attributes = MemberAttributes.Public;

			            if (entity.BaseEntity != null)
			                method.Attributes |= MemberAttributes.Override;
			            else
			                // реализует метод базового класса
			                method.ImplementationTypes.Add(typeof (IOrmObjectSchema));
			            CodeArrayCreateExpression arrayExpression = new CodeArrayCreateExpression(
			                new CodeTypeReference(typeof (string[]))
			                );


			            foreach (PropertyDescription suppressedProperty in entity.SuppressedProperties)
			            {
			                arrayExpression.Initializers.Add(
                                //new CodeObjectCreateExpression(typeof (string),
                                //                               new CodePrimitiveExpression(
                                //                                   suppressedProperty.PropertyAlias)
                                //                               )
                                new CodePrimitiveExpression(suppressedProperty.PropertyAlias)
                            );
			            }




			            method.Statements.Add(new CodeMethodReturnStatement(arrayExpression));
			        }

			        #endregion ColumnAttribute[] GetSuppressedColumns()

			        #region IOrmFilter GetFilter(object filter_info)

			        if (entity.Behaviour != EntityBehaviuor.PartialObjects && entity.BaseEntity == null)
			        {
			            method = new CodeMemberMethod();
			            entitySchemaDefClass.Members.Add(method);
			            method.Name = "GetContextFilter";
			            // тип возвращаемого значения
			            method.ReturnType = new CodeTypeReference(typeof (Worm.Criteria.Core.IFilter));
			            // модификаторы доступа
			            method.Attributes = MemberAttributes.Public;
			            method.Parameters.Add(
			                new CodeParameterDeclarationExpression(
			                    new CodeTypeReference(typeof (object)),
			                    "context"
			                    )
			                );
			            // реализует метод базового класса
			            method.ImplementationTypes.Add(typeof (IOrmObjectSchema));
			            method.Statements.Add(
			                new CodeMethodReturnStatement(
			                    new CodePrimitiveExpression(null)
			                    )
			                );
			        }

			        #endregion IOrmFilter GetFilter(object filter_info)

			        #region сущность имеет связи "многие ко многим"

			        List<RelationDescription> usedM2MRelation;
			        // список релейшенов относящихся к данной сущности
			        usedM2MRelation = entity.GetRelations(false);

			        List<SelfRelationDescription> usedM2MSelfRelation;
			        usedM2MSelfRelation = entity.GetSelfRelations(false);

			        #region поле _m2mRelations

			        field = new CodeMemberField(new CodeTypeReference(typeof (M2MRelation[])), "_m2mRelations");
			        entitySchemaDefClass.Members.Add(field);

			        #endregion поле _m2mRelations

			        #region метод M2MRelation[] GetM2MRelations()

			        method = new CodeMemberMethod();
			        entitySchemaDefClass.Members.Add(method);
			        method.Name = "GetM2MRelations";
			        // тип возвращаемого значения
			        method.ReturnType = new CodeTypeReference(typeof (M2MRelation[]));
			        // модификаторы доступа
			        method.Attributes = MemberAttributes.Public;
			        if (entity.BaseEntity != null)
			        {
			            method.Attributes |= MemberAttributes.Override;
			        }
			        else
			            // реализует метод базового класса
			            method.ImplementationTypes.Add(typeof (IOrmObjectSchema));
			        // параметры
			        //...
			        // для лока
			        CodeMemberField forM2MRelationsLockField = new CodeMemberField(
			            new CodeTypeReference(typeof (object)),
			            "_forM2MRelationsLock"
			            );
			        forM2MRelationsLockField.InitExpression =
			            new CodeObjectCreateExpression(forM2MRelationsLockField.Type);
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
			            new CodeTypeReference(typeof (M2MRelation[]))
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
			                    new CodeTypeReference(typeof (M2MRelation[])),
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
			                        new CodeTypeReferenceExpression(new CodeTypeReference(typeof (Array))),
			                        "Resize",
			                        new CodeTypeReference(typeof (M2MRelation))),
			                    new CodeDirectionExpression(FieldDirection.Ref,
			                                                new CodeVariableReferenceExpression("m2mRelations")),
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
			                    new CodeTypeReferenceExpression(typeof (Array)),
			                    "Copy",
			                    new CodeVariableReferenceExpression("basem2mRelations"),
			                    new CodePrimitiveExpression(0),
			                    new CodeVariableReferenceExpression("m2mRelations"),
			                    new CodeBinaryOperatorExpression(
			                        new CodePropertyReferenceExpression(
			                            new CodeVariableReferenceExpression("m2mRelations"), "Length"),
			                        CodeBinaryOperatorType.Subtract,
			                        new CodePropertyReferenceExpression(
			                            new CodeVariableReferenceExpression("basem2mRelations"), "Length")
			                        ),
			                    new CodePropertyReferenceExpression(
			                        new CodeVariableReferenceExpression("basem2mRelations"), "Length")
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
			        method.ReturnType =
			            new CodeTypeReference(typeof (IndexedCollection<string, MapField2Column>));
			        // модификаторы доступа
			        method.Attributes = MemberAttributes.Public;
			        if (entity.BaseEntity != null)
			        {
			            method.Attributes |= MemberAttributes.Override;
			        }
			        else
			            // реализует метод базового класса
			            method.ImplementationTypes.Add(new CodeTypeReference(typeof (IOrmObjectSchema)));
			        // параметры
			        //...
			        // для лока
			        CodeMemberField forIdxLockField = new CodeMemberField(
			            new CodeTypeReference(typeof (object)),
			            "_forIdxLock"
			            );
			        forIdxLockField.InitExpression = new CodeObjectCreateExpression(forIdxLockField.Type);
			        entitySchemaDefClass.Members.Add(forIdxLockField);
			        List<CodeStatement> condTrueStatements = new List<CodeStatement>();
			        condTrueStatements.Add(
			            new CodeVariableDeclarationStatement(
			                new CodeTypeReference(typeof (Worm.Collections.IndexedCollection<string, MapField2Column>)),
			                "idx",
			                (entity.BaseEntity == null)
			                    ?
			                        (CodeExpression) new CodeObjectCreateExpression(
			                                             new CodeTypeReference(typeof (Worm.Orm.Meta.OrmObjectIndex))
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
			                                                                        new CodeVariableReferenceExpression(
			                                                                            "idx"),
			                                                                        "Add",
			                                                                        GetMapField2ColumObjectCreationExpression
			                                                                            (entity, action)
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

			        #region сущность реализует связь

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
			                ImplementIRelation((RelationDescription) relation, entity, entitySchemaDefClass);
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

			        #endregion сущность реализует связь

			        #region public void GetSchema(OrmSchemaBase schema, Type t)

			        if (entity.BaseEntity == null)
			        {
			            CodeMemberField schemaField = new CodeMemberField(
			                new CodeTypeReference(typeof (Worm.ObjectMappingEngine)),
			                "_schema"
			                );
			            CodeMemberField typeField = new CodeMemberField(
			                new CodeTypeReference(typeof (Type)),
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
			                    new CodeTypeReference(typeof (Worm.ObjectMappingEngine)),
			                    "schema"
			                    )
			                );
			            method.Parameters.Add(
			                new CodeParameterDeclarationExpression(
			                    new CodeTypeReference(typeof (Type)),
			                    "t"
			                    )
			                );
			            // реализует метод базового класса
			            method.ImplementationTypes.Add(typeof (IOrmSchemaInit));
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

			        if (createobjectMethod != null)
			        {
			            if ((createobjectMethod.Statements.Count == 0 ||
			                 entity.Behaviour == EntityBehaviuor.PartialObjects) &&
			                entityClass.Members.Contains(createobjectMethod))
			                entityClass.Members.Remove(createobjectMethod);
			        }
			        if (setvalueMethod.Statements.Count <= 1)
			            entityClass.Members.Remove(setvalueMethod);


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
			
            //}
			}
        }

        private void UpdateSetPKMethodCompositePK(CodeEntityTypeDeclaration entityClass)
        {
            EntityDescription entity = entityClass.Entity;
            if (entity.PkProperties.Count == 0)
                return;

            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "SetPK";

            // модификаторы доступа
            meth.Attributes = MemberAttributes.Family | MemberAttributes.Override;

            entityClass.Members.Add(meth);

            meth.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(new CodeTypeReference(typeof(PKDesc)), (int)1), "pks")
                );

            meth.Statements.Add(new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), meth.Name,
                                                               new CodeArgumentReferenceExpression(
                                                                   meth.Parameters[0].Name)));

            meth.Statements.Add(
                Delegates.CodePatternForeachStatement(
                    new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(PKDesc)), "pk"),
                    new CodeArgumentReferenceExpression("pks"),
                    entity.PkProperties.
                    ConvertAll<CodeStatement>(
                        delegate(PropertyDescription pd_)
                        {
                            var typeReference = new CodeTypeReference(pd_.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetEntityClassName(pd_.PropertyType.Entity, true) : pd_.PropertyType.TypeName);
                            return new CodeConditionStatement(
                                new CodeBinaryOperatorExpression(
                                    new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("pk"),
                                        "PropertyAlias"),
                                    CodeBinaryOperatorType.ValueEquality,
                                    new CodePrimitiveExpression(pd_.PropertyAlias)),
                                new CodeAssignStatement(
                                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), CodeGen.Core.OrmCodeGenNameHelper.GetPrivateMemberName(pd_.PropertyName)),
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

        private void UpdateGetPKValuesMethodCompositePK(CodeEntityTypeDeclaration entityClass)
        {
            EntityDescription entity = entityClass.Entity;
            if (entity.PkProperties.Count == 0)
                return;

            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "GetPKValues";
            CodeTypeReference tr = new CodeTypeReference(typeof(PKDesc));
            //tr.TypeArguments.Add(new CodeTypeReference(typeof(string)));
            //tr.TypeArguments.Add(new CodeTypeReference(typeof(object)));
            // тип возвращаемого значения
            meth.ReturnType = new CodeTypeReference(tr, 1);

            // модификаторы доступа
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

            meth.Statements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(System.Array)), "Copy", new CodeVariableReferenceExpression("basePks"), new CodeVariableReferenceExpression("result"), new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("basePks"), "Length")));
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
            // тип возвращаемого значения
            meth.ReturnType = new CodeTypeReference(typeof(Int32));
            // модификаторы доступа
            meth.Attributes = MemberAttributes.Family | MemberAttributes.Override;

            entityClass.Members.Add(meth);

            //meth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "fieldName"));
            //meth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "value"));

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
                                   Type = new CodeTypeReference(typeof (object)),
                                   HasGet = true,
                                   HasSet = true,
                                   Attributes = MemberAttributes.Public | MemberAttributes.Override
                               };
            PropertyDescription pkProperty = entityClass.Entity.PkProperty;
            property.GetStatements.Add(new CodeMethodReturnStatement(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(),
                                                                           pkProperty.PropertyName)));
            //Convert.ChangeType(object, type);
            var typeReference = new CodeTypeReference(pkProperty.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetEntityClassName(pkProperty.PropertyType.Entity, true) : pkProperty.PropertyType.TypeName);
            property.SetStatements.Add(
                new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(),
                                                                            pkProperty.PropertyName),
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

        private void CreateSetPKMethodCompositePK(CodeEntityTypeDeclaration entityClass)
        {
            EntityDescription entity = entityClass.Entity;
            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "SetPK";

            // модификаторы доступа
            meth.Attributes = MemberAttributes.Family | MemberAttributes.Override;

            entityClass.Members.Add(meth);

            meth.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(new CodeTypeReference(typeof(PKDesc)),(int)1),"pks")
                );

            meth.Statements.Add(
                Delegates.CodePatternForeachStatement(
                    new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(PKDesc)), "pk"),
                    new CodeArgumentReferenceExpression("pks"),
                    entity.PkProperties.
                    ConvertAll<CodeStatement>(
                        delegate(PropertyDescription pd_) {
                            var typeReference = new CodeTypeReference(pd_.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetEntityClassName(pd_.PropertyType.Entity, true) : pd_.PropertyType.TypeName);
                            return new CodeConditionStatement(
                                new CodeBinaryOperatorExpression(
                                    new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("pk"),
                                        "PropertyAlias"),
                                    CodeBinaryOperatorType.ValueEquality,
                                    new CodePrimitiveExpression(pd_.PropertyAlias)),
                                new CodeAssignStatement(
                                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), CodeGen.Core.OrmCodeGenNameHelper.GetPrivateMemberName(pd_.PropertyName)),
                                    new CodeCastExpression(
                                        typeReference,
                                        new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Convert)), "ChangeType",
                                       new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("pk"),"Value"),
                                       new CodeTypeOfExpression(typeReference))
                                   )
                                )
                            
                            );}
                     ).ToArray()
                )
            );
        }

        private void CreateGetPKValuesMethodCompositePK(CodeEntityTypeDeclaration entityClass)
        {
            EntityDescription entity = entityClass.Entity;
            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "GetPKValues";
            CodeTypeReference tr = new CodeTypeReference(typeof(PKDesc));
            //tr.TypeArguments.Add(new CodeTypeReference(typeof(string)));
            //tr.TypeArguments.Add(new CodeTypeReference(typeof(object)));
            // тип возвращаемого значения
            meth.ReturnType = new CodeTypeReference(tr, 1);

            // модификаторы доступа
            meth.Attributes = MemberAttributes.Public | MemberAttributes.Override;

            entityClass.Members.Add(meth);

            meth.Statements.Add(
                new CodeMethodReturnStatement(new CodeArrayCreateExpression(meth.ReturnType,
                    entity.Properties.FindAll(
                        delegate(PropertyDescription pd_){return pd_.HasAttribute(Field2DbRelations.PK);}).
                    ConvertAll<CodeExpression>(
                        delegate(PropertyDescription pd_) {
                            return new CodeObjectCreateExpression(tr, new CodePrimitiveExpression(pd_.PropertyAlias),
                                new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), CodeGen.Core.OrmCodeGenNameHelper.GetPrivateMemberName(pd_.PropertyName)));
                            }
                        ).ToArray()))
                );
        }


        private void CreateGetKeyMethodCompositePK(CodeEntityTypeDeclaration entityClass)
        {
            EntityDescription entity = entityClass.Entity;

            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "GetCacheKey";
            // тип возвращаемого значения
            meth.ReturnType = new CodeTypeReference(typeof(Int32));
            // модификаторы доступа
            meth.Attributes = MemberAttributes.Family | MemberAttributes.Override;

            entityClass.Members.Add(meth);

            //meth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "fieldName"));
            //meth.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "value"));

            CodeExpression lf = null;

            foreach(PropertyDescription pd in entity.Properties)
            {
                if (pd.HasAttribute(Field2DbRelations.PK))
                {
                    string fn = OrmCodeGenNameHelper.GetPrivateMemberName(pd.PropertyName);

                    CodeExpression exp = new CodeMethodInvokeExpression(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),fn),
                        "GetHashCode", new CodeExpression[0]);

                    if (lf == null)
                        lf = exp;
                    else
                    {
                        lf = Delegates.CodePatternXorStatement(lf, exp);
                    }
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
    		if(h != null)
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
    		CodeEntityInterfaceDeclaration entityInterface, entityPropertiesInterface;
    		entityInterface = new CodeEntityInterfaceDeclaration(entityClass);
    		entityPropertiesInterface = new CodeEntityInterfaceDeclaration(entityClass, null, "Properties");
			entityInterface.Attributes = entityPropertiesInterface.Attributes = MemberAttributes.Public;
			entityInterface.TypeAttributes = entityPropertiesInterface.TypeAttributes = TypeAttributes.Public | TypeAttributes.Interface;
    		
            entityInterface.BaseTypes.Add(entityPropertiesInterface.TypeReference);
            if(entityClass.Entity.HasSinglePK)
                entityInterface.BaseTypes.Add(new CodeTypeReference(typeof(_IOrmBase)));
            else
                entityInterface.BaseTypes.Add(new CodeTypeReference(typeof(_ICachedEntity)));
                

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

				//CreateM2MMergeMethod(entity, relation, entityClass);
			}
		}

		private delegate CodeMemberMethod CreateM2MMethodDelegate(string accessorName, EntityDescription relatedEntity, TypeDescription relatedEntityType, bool? direct);

		private void CreateM2MGenMethods(CreateM2MMethodDelegate del, EntityDescription entity, RelationDescriptionBase relation, CodeTypeDeclaration entityClass)
		{
			RelationDescription rel = relation as RelationDescription;
			if (rel != null)
			{
				EntityDescription relatedEntity;
				string accessorName;

				LinkTarget link = rel.Left.Entity == entity ? rel.Right : rel.Left;
				accessorName = link.AccessorName;
				relatedEntity = link.Entity;
				if(!string.IsNullOrEmpty(accessorName))
					entityClass.Members.Add(del(accessorName, relatedEntity, link.AccessedEntityType, null));
			}

			SelfRelationDescription selfRel = relation as SelfRelationDescription;
			if (selfRel != null)
			{

				if(!string.IsNullOrEmpty(selfRel.Direct.AccessorName))
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
			method.ReturnType = new CodeTypeReference(typeof(Worm.ReadOnlyList<>).FullName, callType);
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
			colPrm.Type = new CodeTypeReference(typeof(Worm.ReadOnlyList<>).FullName, callType);
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

    	private void ImplementIRelation(RelationDescription relation, EntityDescription entity, CodeTypeDeclaration entitySchemaDefClass)
    	{
    		CodeMemberMethod method;
    		entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(typeof(IRelation)));
    		#region Pair<string, Type> GetFirstType()
    		method = new CodeMemberMethod();
    		//method.StartDirectives.Add(Regions["IRelation Members"].Start);
    		entitySchemaDefClass.Members.Add(method);
    		method.Name = "GetFirstType";
    		// тип возвращаемого значения
    		method.ReturnType = new CodeTypeReference(typeof(IRelation.RelationDesc));
    		// модификаторы доступа
    		method.Attributes = MemberAttributes.Public;
    		// реализует метод базового класса
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
    		// тип возвращаемого значения
    		method.ReturnType = new CodeTypeReference(typeof(IRelation.RelationDesc));
    		// модификаторы доступа
    		method.Attributes = MemberAttributes.Public;
    		// реализует метод базового класса
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
			// тип возвращаемого значения
			method.ReturnType = new CodeTypeReference(typeof(IRelation.RelationDesc));
			// модификаторы доступа
			method.Attributes = MemberAttributes.Public;
			// реализует метод базового класса
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
			// тип возвращаемого значения
			method.ReturnType = new CodeTypeReference(typeof(IRelation.RelationDesc));
			// модификаторы доступа
			method.Attributes = MemberAttributes.Public;
			// реализует метод базового класса
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
		//                    new CodeTypeReferenceExpression(typeof(OrmManager)), 
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
                new CodeTypeReference(typeof (System.Reflection.PropertyInfo)),
                "pi"
                );
            method.Parameters.Add(prm);

    		prm = new CodeParameterDeclarationExpression(
				new CodeTypeReference(typeof(string)),
                "propertyAlias"
				);
			method.Parameters.Add(prm);

            prm = new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof(IObjectSchemaBase)),
                "schema"
                );
            method.Parameters.Add(prm);

            method.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodeMethodInvokeExpression(
                        new CodeBaseReferenceExpression(),
                        method.Name,
                        new CodeArgumentReferenceExpression("pi"),
						new CodeArgumentReferenceExpression("propertyAlias"),
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
                //(SourceFragment)this.GetTables().GetValue((int)(XMedia.Framework.Media.Objects.ArtistBase.ArtistBaseSchemaDef.TablesLink.tblArtists)))
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
			//if (action.PropertyAlias == "ID")
            if (action.Attributes != null && action.Attributes.Length > 0)
                expression.Parameters.Add(GetPropAttributesEnumValues(action.Attributes));
            else
                expression.Parameters.Add(
                    new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof (Field2DbRelations)),
                                                     Field2DbRelations.None.ToString()));
            if(!string.IsNullOrEmpty(action.DbTypeName))
            {
                expression.Parameters.Add(new CodePrimitiveExpression(action.DbTypeName));
                if(action.DbTypeSize.HasValue)
                    expression.Parameters.Add(new CodePrimitiveExpression(action.DbTypeSize.Value));
                if(action.DbTypeNullable.HasValue)
                    expression.Parameters.Add(new CodePrimitiveExpression(action.DbTypeNullable.Value));
            }
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
        	throw new ArgumentException("To realize m2m relation on self use SelfRelation instead.");
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

            //entityTypeExpression = new CodeMethodInvokeExpression(
            //    new CodeMethodReferenceExpression(
            //        new CodeFieldReferenceExpression(
            //            new CodeThisReferenceExpression(),
            //            "_schema"
            //            ),
            //        "GetTypeByEntityName"
            //        ),
            //    OrmCodeGenHelper.GetEntityNameReferenceExpression(relatedEntity)
            //        //new CodePrimitiveExpression(relatedEntity.Name)
            //    );
            entityTypeExpression = OrmCodeGenHelper.GetEntityNameReferenceExpression(relatedEntity);

            if (underlyingEntity == null)
                tableExpression = new CodeMethodInvokeExpression(
                    //new CodeCastExpression(
                        //new CodeTypeReference(typeof(Worm.IDbSchema)),
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_schema")
                        ,
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
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_schema"),
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
                CodeParameterDeclarationExpression methodParam = new CodeParameterDeclarationExpression(
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

        private void CreateProperties(CodeMemberMethod createobjectMethod, EntityDescription entity, CodeEntityTypeDeclaration entityClass, CodeMemberMethod setvalueMethod, CodeMemberMethod getvalueMethod, CodeTypeDeclaration propertiesClass, CodeTypeDeclaration fieldsClass, CodeTypeDeclaration propertyAliasClass, CodeTypeDeclaration instancedPropertyAliasClass)
        {
            EntityDescription completeEntity = entity.CompleteEntity;

            for (int idx = 0; idx < completeEntity.Properties.Count; idx++)
            {
                #region создание проперти и etc

                PropertyDescription propertyDesc = completeEntity.Properties[idx];
                if (propertyDesc.Disabled)
                    continue;

                FilterPropertyName(propertyDesc);
                
                    var propertyNameField = new CodeMemberField()
                    {
                        Type = new CodeTypeReference(typeof(string)),
                        Name = propertyDesc.PropertyName,
                        InitExpression = new CodePrimitiveExpression(propertyDesc.PropertyAlias),
                        Attributes = (MemberAttributes.Public | MemberAttributes.Const)
                    };

                    propertiesClass.Members.Add(propertyNameField);

                    if (!string.IsNullOrEmpty(propertyDesc.Description))
                        SetMemberDescription(propertyNameField, propertyDesc.Description);

                    //CodeMemberField propConst = new CodeMemberField(typeof(string), propertyDesc.Name);
                    //propConst.InitExpression = new CodePrimitiveExpression(propertyDesc.PropertyAlias);
                    //propConst.Attributes = MemberAttributes.Const | MemberAttributes.Public;
                    //if (!string.IsNullOrEmpty(propertyDesc.Description))
                    //    SetMemberDescription(propConst, propertyDesc.Description);
                    //propertiesClass.Members.Add(propConst);
                if(propertyAliasClass != null && instancedPropertyAliasClass != null)
                {
                    var propertyAliasProperty = new CodeMemberProperty
                                                    {
                                                        Name = propertyDesc.PropertyName,
                                                        Type =
                                                            new CodeTypeReference(typeof (Worm.Criteria.ObjectProperty)),
                                                        HasGet = true,
                                                        HasSet = false,
                                                        Attributes = MemberAttributes.Public | MemberAttributes.Final,
                                                    };
                    propertyAliasProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeObjectCreateExpression(propertyAliasProperty.Type, new CodeThisReferenceExpression(), OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc))));
                    propertyAliasClass.Members.Add(propertyAliasProperty);

                    var instancedPropertyAliasProperty = new CodeMemberProperty
                    {
                        Name = propertyDesc.PropertyName,
                        Type =
                            new CodeTypeReference(typeof(Worm.Criteria.ObjectProperty)),
                        HasGet = true,
                        HasSet = false,
                        Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    };
                    instancedPropertyAliasProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeObjectCreateExpression(propertyAliasProperty.Type, new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), OrmCodeGenNameHelper.GetPrivateMemberName("objectAlias")) , OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc))));
                    instancedPropertyAliasClass.Members.Add(instancedPropertyAliasProperty);
                }
                if(fieldsClass != null)
                {
                    Type type = typeof(Worm.Criteria.ObjectProperty);
                    var propConst = new CodeMemberField(type, OrmCodeGenNameHelper.GetPrivateMemberName(propertyDesc.PropertyName))
                                        {
                                            InitExpression = new CodeObjectCreateExpression(type, OrmCodeGenHelper.GetEntityNameReferenceExpression(entity), OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc)),
                                            Attributes = (MemberAttributes.Private | MemberAttributes.Static |MemberAttributes.Final)
                                        };

                    fieldsClass.Members.Add(propConst);

                    var prop = new CodeMemberProperty
                                   {
                                       Attributes = (MemberAttributes.Public | MemberAttributes.Static | MemberAttributes.Final),
                                       HasGet = true,
                                       HasSet = false,
                                       Name = propertyDesc.PropertyName,
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

            	CodeMemberProperty property = null;
                if (!propertyDesc.FromBase)
                    property = CreateProperty(createobjectMethod, entityClass, propertyDesc, setvalueMethod, getvalueMethod);
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

    	private CodeMemberProperty CreateUpdatedProperty(CodeEntityTypeDeclaration entityClass, PropertyDescription propertyDesc)
        {
            CodeTypeReference propertyType;
            propertyType = new CodeTypeReference(propertyDesc.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetEntityClassName(propertyDesc.PropertyType.Entity, true) : propertyDesc.PropertyType.TypeName);

            CodeMemberProperty property;
            property = new CodeMemberProperty();

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
			if(!propertyDesc.HasAttribute(Field2DbRelations.ReadOnly))
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

    		RaisePropertyCreated(propertyDesc, entityClass, property, null);

            #region добавление членов в класс
            entityClass.Members.Add(property);
            #endregion добавление членов в класс

			return property;
        }

		private CodeMemberProperty CreateProperty(CodeMemberMethod createobjectMethod, CodeEntityTypeDeclaration entityClass, PropertyDescription propertyDesc, CodeMemberMethod setvalueMethod, CodeMemberMethod getvalueMethod)
        {
            CodeMemberField field;
            CodeTypeReference fieldType;
            fieldType = new CodeTypeReference(propertyDesc.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetEntityClassName(propertyDesc.PropertyType.Entity, true) : propertyDesc.PropertyType.TypeName);
            string fieldName;
            fieldName = OrmCodeGenNameHelper.GetPrivateMemberName(propertyDesc.PropertyName);

            field = new CodeMemberField(fieldType, fieldName);
            field.Attributes = GetMemberAttribute(propertyDesc.FieldAccessLevel);

            CodeMemberProperty property;
            property = new CodeMemberProperty();

            property.HasGet = true;
            property.HasSet = true;

            property.Name = propertyDesc.PropertyName;
            property.Type = fieldType;
            property.Attributes = GetMemberAttribute(propertyDesc.PropertyAccessLevel);

            #endregion создание проперти и etc

            #region property GetStatements

            CodeExpression getUsingExpression = new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "Read"),
				OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc)
                );

            CodeStatement[] getInUsingStatements = new CodeStatement[]
                {
                    new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName))
                };

            property.GetStatements.AddRange(Delegates.CodePatternUsingStatements(getUsingExpression, getInUsingStatements));
            
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


                property.SetStatements.AddRange(Delegates.CodePatternUsingStatements(setUsingExpression, setInUsingStatements));
        	}
			else
				property.HasSet = false;

			#endregion property SetStatements

			RaisePropertyCreated(propertyDesc, entityClass, property, field);

            #region добавление членов в класс
            entityClass.Members.Add(field);
            entityClass.Members.Add(property);
            #endregion добавление членов в класс

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

        private void UpdateGetValueMethod(CodeMemberProperty property, PropertyDescription propertyDesc, CodeMemberMethod getvalueMethod)
        {
            //    if (Properties.Song.Equals(propAlias))
            //        return Song;
            getvalueMethod.Statements.Insert(getvalueMethod.Statements.Count - 1, 
                new CodeConditionStatement(
                    new CodeMethodInvokeExpression(
                        OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc),
                        "Equals",
                        //new CodePropertyReferenceExpression(new CodeArgumentReferenceExpression("c"), "FieldName")
                        new CodeArgumentReferenceExpression("propertyAlias")
                    ),
                    new CodeMethodReturnStatement(
                        //new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), property.Name)
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), OrmCodeGenNameHelper.GetPrivateMemberName(propertyDesc.PropertyName))
                    )
                )
            );
        }
        private void CreatePropertyColumnAttribute(CodeMemberProperty property, PropertyDescription propertyDesc)
        {
            CodeAttributeDeclaration declaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(ColumnAttribute)));

            if (!string.IsNullOrEmpty(propertyDesc.PropertyAlias))
            {
                //declaration.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(propertyDesc.PropertyAlias)));
                declaration.Arguments.Add(
                    new CodeAttributeArgument(OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc)));
            }
            //if (propertyDesc.Attributes != null && propertyDesc.Attributes.Length != 0)
            //{
            //    declaration.Arguments.Add(new CodeAttributeArgument(GetPropAttributesEnumValues(propertyDesc.Attributes)));
            //}
            
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
            if (createobjectMethod == null)
                return;
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

        private static CodeMemberMethod CreateSetValueMethod(CodeTypeDeclaration entityClass)
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
            setvalueMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "propertyAlias"));
            setvalueMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IObjectSchemaBase), "schema"));
            setvalueMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "value"));
            setvalueMethod.Statements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference(typeof (string)), 
                    "fieldName",
                    //new CodePropertyReferenceExpression(
                    //    new CodeArgumentReferenceExpression("c"), 
                    //    "FieldName"
                    //)
                    new CodeArgumentReferenceExpression("propertyAlias")
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
            // тип возвращаемого значения
            method.ReturnType = new CodeTypeReference(typeof(SourceFragment));
            // модификаторы доступа
            method.Attributes = MemberAttributes.Family;
            if (entity.BaseEntity != null)
                method.Attributes |= MemberAttributes.New;

            // параметры
            method.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(
                        OrmCodeGenNameHelper.GetEntitySchemaDefClassQualifiedName(entity) + ".TablesLink"), "tbl"
                    )
                );
            //	return (SourceFragment)this.GetTables().GetValue((int)tbl)

			//	SourceFragment[] tables = this.GetTables();
			//	SourceFragment table = null;
			//	int tblIndex = (int)tbl;
			//	if(tables.Length > tblIndex)
			//		table = tables[tblIndex];
			//	return table;
			//string[] strs;
            method.Statements.Add(
				new CodeVariableDeclarationStatement(
					new CodeTypeReference(typeof(SourceFragment[])), 
					"tables", 
					new CodeMethodInvokeExpression(
                        new CodeThisReferenceExpression(),
                        "GetTables"
                        )));
			method.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(SourceFragment)), "table", new CodePrimitiveExpression(null)));
			method.Statements.Add(new CodeVariableDeclarationStatement(
						new CodeTypeReference(typeof(int)), 
						"tblIndex", 
						new CodeCastExpression(
                                new CodeTypeReference(typeof(int)),
                                new CodeArgumentReferenceExpression("tbl")
                                )
						));
			method.Statements.Add(new CodeConditionStatement(
						new CodeBinaryOperatorExpression(
							new CodePropertyReferenceExpression(
								new CodeVariableReferenceExpression("tables"),
								"Length"
								),
							CodeBinaryOperatorType.GreaterThan,
							new CodeVariableReferenceExpression("tblIndex")
						),
						new CodeAssignStatement(
							new CodeVariableReferenceExpression("table"), 
							new CodeIndexerExpression(
								new CodeVariableReferenceExpression("tables"),
								new CodeVariableReferenceExpression("tblIndex")
							))));
			method.Statements.Add(						
                new CodeMethodReturnStatement(
					new CodeVariableReferenceExpression("table")                    
                ));
        }

        private void CreateGetTablesMethod(EntityDescription entity, CodeTypeDeclaration entitySchemaDefClass)
        {
            CodeMemberMethod method;
            method = new CodeMemberMethod();
            entitySchemaDefClass.Members.Add(method);
            method.Name = "GetTables";
            // тип возвращаемого значения
            method.ReturnType = new CodeTypeReference(typeof(SourceFragment[]));
            // модификаторы доступа
            method.Attributes = MemberAttributes.Public;
            if (entity.BaseEntity != null)
            {
                method.Attributes |= MemberAttributes.Override;
            }
            else
            // реализует метод интерфейса
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
                            new CodeTypeReference(typeof (SourceFragment[])),
                            entity.CompleteEntity.Tables.ConvertAll<CodeExpression>(delegate(TableDescription action)
                                                                                    {
                                                                                        return new CodeObjectCreateExpression(
                                                                                            new CodeTypeReference(
                                                                                                typeof (SourceFragment)),
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

            if (entity.BaseEntity == null)
            {
                CodeMemberProperty prop = new CodeMemberProperty();
                entitySchemaDefClass.Members.Add(prop);
                prop.Name = "Table";
                prop.Type = new CodeTypeReference(typeof(SourceFragment));
                prop.Attributes = MemberAttributes.Public;
                prop.HasSet = false;
                prop.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeArrayIndexerExpression(
                            new CodeMethodInvokeExpression(
                                new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), method.Name)),
                            new CodePrimitiveExpression(0)
                        )
                    )
                );
                prop.ImplementationTypes.Add(typeof(IObjectSchemaBase));
            }
        }

        private static void CreateGetTypeMainTableMethod(EntityDescription entity, CodeTypeDeclaration entitySchemaDefClass)
        {
            CodeMemberMethod method;
            method = new CodeMemberMethod();
            entitySchemaDefClass.Members.Add(method);
            method.Name = "GetTypeMainTable";
            // тип возвращаемого значения
            method.ReturnType = new CodeTypeReference(typeof(SourceFragment));
			// модификаторы доступа
			method.Attributes = MemberAttributes.Family;
            if (entity.BaseEntity != null)
            {
                method.Attributes |= MemberAttributes.Override;
            }            
            method.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(typeof(Type)),
                    "type"
                    )
                );
            method.Statements.Add(
                new CodeVariableDeclarationStatement(new CodeTypeReference(typeof (SourceFragment[])), "tables")
                );
            method.Statements.Add(
                new CodeAssignStatement(
                    new CodeVariableReferenceExpression("tables"),
                    new CodeMethodInvokeExpression(
                        //new CodeCastExpression(new CodeTypeReference(typeof(Worm.IDbSchema)), 
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_schema"),
                        "GetTables",
                        new CodeArgumentReferenceExpression("type")
                        )
                    )
                );
            method.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodeCastExpression(
                        new CodeTypeReference(typeof(SourceFragment)),
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
            if (entity.BaseEntity != null)
                tablesEnum.Attributes |= MemberAttributes.New;

            tablesEnum.IsClass = false;
            tablesEnum.IsEnum = true;
            tablesEnum.IsPartial = false;
            int tableNum = 0;
            tablesEnum.Members.AddRange(entity.CompleteEntity.Tables.ConvertAll<CodeTypeMember>(delegate(TableDescription tbl)
                                                                                                {
                                                                                                    var enumMember = new CodeMemberField{
                                                                                                        InitExpression = new CodePrimitiveExpression(tableNum++),
                                                                                                        Name = OrmCodeGenNameHelper.GetSafeName(tbl.Identifier)
                                                                                                            };
                                                                                                    return enumMember;
                                                                                                }).ToArray());
            entitySchemaDefClass.Members.Add(tablesEnum);
        }

        private void ProcessSplitOption(EntityDescription entity, CodeTypeDeclaration entityClass, ref CodeTypeDeclaration entitySchemaDefClass, IList<CodeCompileFileUnit> result)
        {
            CodeNamespace nameSpace;
            if (Settings.Split)
            {
                var entitySchemaDefUnit = new CodeCompileFileUnit
                                                          {
                                                              Filename = OrmCodeGenNameHelper.GetEntitySchemaDefFileName(entity)
                                                          };

				nameSpace = new CodeNamespace(entity.Namespace);
				entitySchemaDefUnit.Namespaces.Add(nameSpace);               

				//nameSpace.Imports.Add(new CodeNamespaceImport("System"));
				//nameSpace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
				//nameSpace.Imports.Add(new CodeNamespaceImport("Worm.Orm"));

				// partial класс сущности для схемы
                var entityClassSchemaDefPart = new CodeTypeDeclaration
                                                   {
                                                       IsClass = entityClass.IsClass,
                                                       IsPartial = entityClass.IsPartial,
                                                       Name = entityClass.Name,
                                                       Attributes = entityClass.Attributes,
                                                       TypeAttributes = entityClass.TypeAttributes
                                                   };

                nameSpace.Types.Add(entityClassSchemaDefPart);

                var entitySchemaDefClassPart = new CodeTypeDeclaration
                                                   {
                                                       IsClass = entitySchemaDefClass.IsClass,
                                                       IsPartial = entitySchemaDefClass.IsPartial,
                                                       Name = entitySchemaDefClass.Name,
                                                       Attributes = entitySchemaDefClass.Attributes,
                                                       TypeAttributes = entitySchemaDefClass.TypeAttributes
                                                   };


                entityClassSchemaDefPart.Members.Add(entitySchemaDefClassPart);

                entitySchemaDefClass = entitySchemaDefClassPart;
                result.Add(entitySchemaDefUnit);
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
            var attrsList = new List<string>(attrs);
            CodeExpression first = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(Field2DbRelations)), attrsList.Count == 0 ? Field2DbRelations.None.ToString() : attrsList[0]);
            if (attrsList.Count > 1)
                return new CodeBinaryOperatorExpression(first, CodeBinaryOperatorType.BitwiseOr, GetPropAttributesEnumValues(attrsList.GetRange(1, attrsList.Count - 1).ToArray()));
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

		public CodeStatement CodePatternDoubleCheckLock(CodeExpression lockExpression, CodeExpression condition, params CodeStatement[] statements)
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
