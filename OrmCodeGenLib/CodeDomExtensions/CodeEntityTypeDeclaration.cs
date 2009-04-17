using System;
using System.CodeDom;
using System.Collections.Generic;
using Worm.CodeGen.Core.Descriptors;
using Worm.Entities.Meta;
using Worm.Query;

namespace Worm.CodeGen.Core.CodeDomExtensions
{
    /// <summary>
    /// Обертка над <see cref="CodeTypeDeclaration"/> применительно к <see cref="EntityDescription"/>
    /// </summary>
    public class CodeEntityTypeDeclaration : CodeTypeDeclaration
    {
        private EntityDescription m_entity;
        private CodeEntityInterfaceDeclaration m_entityInterface;
        private readonly CodeTypeReference m_typeReference;
        private CodeSchemaDefTypeDeclaration m_schema;
        private readonly Dictionary<string, CodePropertiesAccessorTypeDeclaration> m_propertiesAccessor;
        private bool _useType;

        public CodeEntityTypeDeclaration(bool useType)
        {
            m_typeReference = new CodeTypeReference();
            m_propertiesAccessor = new Dictionary<string, CodePropertiesAccessorTypeDeclaration>();
            PopulateMembers += OnPopulateMembers;
            _useType = useType;
        }

        protected virtual void OnPopulateMembers(object sender, System.EventArgs e)
        {
            OnPopulatePropertiesAccessors();
            OnPupulateEntityRelations();
            OnPupulateM2MRelations();
            OnPopulateSchema();
        }

        protected virtual void OnPopulateSchema()
        {
            Members.Add(SchemaDef);
        }

        protected virtual void OnPupulateM2MRelations()
        {
            var relationDescType = new CodeTypeReference(typeof(M2MRelationDesc));
            foreach (var relation in m_entity.GetRelations(false))
            {
                if (relation.Left.Entity == relation.Right.Entity)
                    throw new ArgumentException("To realize m2m relation on self use SelfRelation instead.");

                LinkTarget link = relation.Left.Entity == m_entity ? relation.Right : relation.Left;

                var accessorName = link.AccessorName;
                var relatedEntity = link.Entity;

                if (string.IsNullOrEmpty(accessorName))
                {
                    // существуют похожие релейшены, но не имеющие имени акссесора
                    var lst =
                        link.Entity.GetRelations(false).FindAll(
                            r =>
                            r.Left != link && r.Right != link &&
                            ((r.Left.Entity == m_entity && string.IsNullOrEmpty(r.Right.AccessorName))
                                || (r.Right.Entity == m_entity && string.IsNullOrEmpty(r.Left.AccessorName))));

                    if (lst.Count > 0)
                        throw new OrmCodeGenException(
                            string.Format(
                                "Существуют неоднозначные связи между '{0}' и '{1}'. конкретизируйте их через accessorName.",
                                lst[0].Left.Entity.Name, lst[0].Right.Entity.Name));
                    accessorName = relatedEntity.Name;
                }
                accessorName = OrmCodeGenNameHelper.GetMultipleForm(accessorName);

                var entityTypeExpression = _useType ? OrmCodeGenHelper.GetEntityClassTypeReferenceExpression(relatedEntity) : OrmCodeGenHelper.GetEntityNameReferenceExpression(relatedEntity);

                var desc = new CodeObjectCreateExpression(
                    new CodeTypeReference(typeof(M2MRelationDesc)),
                    entityTypeExpression);

                var staticProperty = new CodeMemberProperty
                {
                    Name = accessorName + "Relation",
                    HasGet = true,
                    HasSet = false,
                    Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                    Type = relationDescType
                };

                staticProperty.GetStatements.Add(new CodeMethodReturnStatement(desc));

                Members.Add(staticProperty);


                var memberProperty = new CodeMemberProperty
                {
                    Name = accessorName,
                    HasGet = true,
                    HasSet = false,
                    Attributes =
                        MemberAttributes.Public | MemberAttributes.Final,
                    Type = new CodeTypeReference(typeof(RelationCmd))
                };
                memberProperty.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeMethodInvokeExpression(
                            new CodeThisReferenceExpression(),
                            "GetCmd",
                            new CodePropertyReferenceExpression(
                                OrmCodeGenHelper.GetEntityClassReferenceExpression(m_entity),
                                staticProperty.Name
                            )
                        )
                    )
                );
                Members.Add(memberProperty);
            }

            foreach (var relation in m_entity.GetSelfRelations(false))
            {
                var accessorName = relation.Direct.AccessorName;

                if (!string.IsNullOrEmpty(accessorName))
                {
                    var entityTypeExpression = OrmCodeGenHelper.GetEntityNameReferenceExpression(m_entity);
                    var desc = new CodeObjectCreateExpression(
                        new CodeTypeReference(typeof(M2MRelationDesc)),
                        entityTypeExpression);

                    accessorName = OrmCodeGenNameHelper.GetMultipleForm(accessorName);

                    var staticProperty = new CodeMemberProperty
                    {
                        Name = accessorName + "Relation",
                        HasGet = true,
                        HasSet = false,
                        Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                        Type = relationDescType
                    };

                    staticProperty.GetStatements.Add(new CodeMethodReturnStatement(desc));
                    //desc.Parameters.Add(new CodePrimitiveExpression(relation.Direct.FieldName));
                    //desc.Parameters.Add(new CodeFieldReferenceExpression(
                    //    new CodeTypeReferenceExpression(typeof(M2MRelationDesc)),"DirKey")
                    //);
                    desc.Parameters.Add(new CodePrimitiveExpression(relation.SourceFragment.Identifier));

                    Members.Add(staticProperty);


                    var memberProperty = new CodeMemberProperty
                    {
                        Name = accessorName,
                        HasGet = true,
                        HasSet = false,
                        Attributes =
                            MemberAttributes.Public | MemberAttributes.Final,
                        Type = new CodeTypeReference(typeof(RelationCmd))
                    };
                    memberProperty.GetStatements.Add(
                        new CodeMethodReturnStatement(
                            new CodeMethodInvokeExpression(
                                new CodeThisReferenceExpression(),
                                "GetCmd",
                                new CodePropertyReferenceExpression(
                                    OrmCodeGenHelper.GetEntityClassReferenceExpression(m_entity),
                                    staticProperty.Name
                                )
                            )
                        )
                    );
                    Members.Add(memberProperty);
                }

                accessorName = relation.Reverse.AccessorName;

                if (!string.IsNullOrEmpty(accessorName))
                {
                    var entityTypeExpression = OrmCodeGenHelper.GetEntityNameReferenceExpression(m_entity);
                    var desc = new CodeObjectCreateExpression(
                        new CodeTypeReference(typeof(M2MRelationDesc)),
                        entityTypeExpression);

                    accessorName = OrmCodeGenNameHelper.GetMultipleForm(accessorName);

                    var staticProperty = new CodeMemberProperty
                    {
                        Name = accessorName + "Relation",
                        HasGet = true,
                        HasSet = false,
                        Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                        Type = relationDescType
                    };

                    staticProperty.GetStatements.Add(new CodeMethodReturnStatement(desc));
                    //desc.Parameters.Add(new CodePrimitiveExpression(relation.Reverse.FieldName));
                    //desc.Parameters.Add(new CodeFieldReferenceExpression(
                    //    new CodeTypeReferenceExpression(typeof(M2MRelationDesc)),"RevKey")
                    //);
                    desc.Parameters.Add(new CodePrimitiveExpression(M2MRelationDesc.ReversePrefix+relation.SourceFragment.Identifier));

                    Members.Add(staticProperty);
                    
                    var memberProperty = new CodeMemberProperty
                    {
                        Name = accessorName,
                        HasGet = true,
                        HasSet = false,
                        Attributes =
                            MemberAttributes.Public | MemberAttributes.Final,
                        Type = new CodeTypeReference(typeof(RelationCmd))
                    };
                    memberProperty.GetStatements.Add(
                        new CodeMethodReturnStatement(
                            new CodeMethodInvokeExpression(
                                new CodeThisReferenceExpression(),
                                "GetCmd",
                                new CodePropertyReferenceExpression(
                                    OrmCodeGenHelper.GetEntityClassReferenceExpression(m_entity),
                                    staticProperty.Name
                                )
                            )
                        )
                    );
                    Members.Add(memberProperty);
                }




            }
        }

        protected virtual void OnPupulateEntityRelations()
        {
            var relationDescType = new CodeTypeReference(typeof(RelationDesc));

            foreach (var entityRelation in m_entity.GetEntityRelations(false))
            {

                string accessorName = string.IsNullOrEmpty(entityRelation.AccessorName) ? OrmCodeGenNameHelper.GetMultipleForm(entityRelation.Entity.Name) : entityRelation.AccessorName;

                var staticProperty = new CodeMemberProperty
                                         {
                                             Name = accessorName + "Relation",
                                             HasGet = true,
                                             HasSet = false,
                                             Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                                             Type = relationDescType
                                         };


                staticProperty.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeObjectCreateExpression(
                            relationDescType,
                            new CodeObjectCreateExpression(
                                new CodeTypeReference(typeof(EntityUnion)),
                                OrmCodeGenHelper.GetEntityNameReferenceExpression(entityRelation.Entity)
                            ),
                            OrmCodeGenHelper.GetFieldNameReferenceExpression(entityRelation.Property),
                            new CodePrimitiveExpression(entityRelation.Name ?? "default")
                        )
                    )
                );

                Members.Add(staticProperty);

                var memberProperty = new CodeMemberProperty
                                         {
                                             Name = accessorName,
                                             HasGet = true,
                                             HasSet = false,
                                             Attributes =
                                                 MemberAttributes.Public | MemberAttributes.Final,
                                             Type = new CodeTypeReference(typeof(RelationCmd))
                                         };
                memberProperty.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeMethodInvokeExpression(
                            new CodeThisReferenceExpression(),
                            "GetCmd",
                            new CodePropertyReferenceExpression(
                                OrmCodeGenHelper.GetEntityClassReferenceExpression(m_entity),
                                staticProperty.Name
                            )
                        )
                    )
                );
                Members.Add(memberProperty);
            }
        }

        protected virtual void OnPopulatePropertiesAccessors()
        {
            foreach (var propertyDescription in Entity.Properties)
            {
                if (propertyDescription.Group == null)
                    continue;
                CodePropertiesAccessorTypeDeclaration accessor;
                if (!m_propertiesAccessor.TryGetValue(propertyDescription.Group.Name, out accessor))
                    m_propertiesAccessor[propertyDescription.Group.Name] =
                        new CodePropertiesAccessorTypeDeclaration(Entity, propertyDescription.Group);

            }
            foreach (var accessor in m_propertiesAccessor.Values)
            {
                Members.Add(accessor);
                //var field = new CodeMemberField(new CodeTypeReference(accessor.FullName),
                //                                OrmCodeGenNameHelper.GetPrivateMemberName(accessor.Name));
                //field.InitExpression = new CodeObjectCreateExpression(field.Type, new CodeThisReferenceExpression());

                var property = new CodeMemberProperty
                                   {
                                       Type = new CodeTypeReference(accessor.FullName),
                                       Name = accessor.Group.Name,
                                       HasGet = true,
                                       HasSet = false
                                   };
                property.GetStatements.Add(new CodeMethodReturnStatement(new CodeObjectCreateExpression(property.Type, new CodeThisReferenceExpression())));
                Members.Add(property);
            }


        }

        public CodeEntityTypeDeclaration(EntityDescription entity, bool useType)
            : this(useType)
        {
            Entity = entity;
            m_typeReference.BaseType = FullName;
        }

        public CodeSchemaDefTypeDeclaration SchemaDef
        {
            get
            {
                if (m_schema == null)
                {
                    m_schema = new CodeSchemaDefTypeDeclaration(this);
                }
                return m_schema;
            }
        }

        public new string Name
        {
            get
            {
                if (Entity != null)
                    return OrmCodeGenNameHelper.GetEntityClassName(Entity, false);
                return null;
            }
        }

        public string FullName
        {
            get
            {
                return OrmCodeGenNameHelper.GetEntityClassName(Entity, true);
            }
        }

        public EntityDescription Entity
        {
            get { return m_entity; }
            set
            {
                m_entity = value;
                EnsureName();
            }
        }

        public CodeEntityInterfaceDeclaration EntityInterfaceDeclaration
        {
            get
            {
                return m_entityInterface;
            }
            set
            {
                if (m_entityInterface != null)
                {
                    m_entityInterface.EnsureData();
                    // удалить существующий из списка дочерних типов
                    if (this.BaseTypes.Contains(m_entityInterface.TypeReference))
                    {
                        BaseTypes.Remove(m_entityInterface.TypeReference);
                    }

                }
                m_entityInterface = value;
                if (m_entityInterface != null)
                {
                    BaseTypes.Add(m_entityInterface.TypeReference);
                    m_entityInterface.EnsureData();
                }
            }
        }

        public CodeEntityInterfaceDeclaration EntityPropertiesInterfaceDeclaration { get; set; }

        public CodeSchemaDefTypeDeclaration SchemaDefDeclaration { get; set; }

        public CodeTypeReference TypeReference
        {
            get { return m_typeReference; }
        }

        protected void EnsureName()
        {
            base.Name = Name;
            m_typeReference.BaseType = FullName;
        }
    }
}
