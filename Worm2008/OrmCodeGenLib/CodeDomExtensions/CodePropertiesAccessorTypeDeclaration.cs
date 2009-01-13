using System;
using System.CodeDom;
using Worm.CodeGen.Core.Descriptors;

namespace Worm.CodeGen.Core.CodeDomExtensions
{
    public class CodePropertiesAccessorTypeDeclaration : CodeTypeDeclaration
    {
        public CodePropertiesAccessorTypeDeclaration(EntityDescription entity, PropertyGroup group)
        {
            Entity = entity;
            Group = group;
            IsClass = true;
            Name = group.Name + "Accessor";
            PopulateMembers += OnPopulateMemebers;
        }

        void OnPopulateMemebers(object sender, EventArgs e)
        {
            if (Entity.BaseEntity != null && Entity.BaseEntity.Properties.Exists(p => p.Group != null && p.Group.Name == Group.Name))
                throw new OrmCodeGenException(
                    string.Format(
                        "В сущности {0} описана группа {1} перекрывающая одноименную группу базовой сущности {2}.",
                        Entity.Name, Group.Name, Entity.BaseEntity.Name));

            var properties = Entity.Properties.FindAll(p => p.Group == Group);
            CodeTypeReference entityClassTypeReference = OrmCodeGenHelper.GetEntityClassTypeReference(Entity);

            var entityField = new CodeMemberField(entityClassTypeReference,
                                                  OrmCodeGenNameHelper.GetPrivateMemberName("entity"));
            Members.Add(entityField);

            var ctor = new CodeConstructor
                           {
                               Attributes = MemberAttributes.Public
                           };
            
            ctor.Parameters.Add(new CodeParameterDeclarationExpression(entityClassTypeReference, "entity"));
            ctor.Statements.Add(
                new CodeAssignStatement(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), entityField.Name),
                    new CodeArgumentReferenceExpression("entity")
                    ));

            Members.Add(ctor);

            foreach (var propertyDesc in properties)
            {
                var property = new CodeMemberProperty
                                   {
                                       Name = propertyDesc.Name,
                                       Type = propertyDesc.PropertyType,
                                       HasGet = true,
                                       HasSet = false,
                                   };

                property.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodePropertyReferenceExpression(
                            new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), entityField.Name),
                            property.Name)));

                Members.Add(property);
            }
        }

        public PropertyGroup Group { get; private set; }

        public EntityDescription Entity { get; private set; }

        public string FullName
        {
            get
            {
                return string.Format("{0}.{1}", OrmCodeGenNameHelper.GetEntityClassName(Entity, true), Name);
            }
        }


    }
}
