﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using Worm.CodeGen.Core.Descriptors;

namespace Worm.CodeGen.Core.CodeDomExtensions
{
    public class CodeLinqContextDeclaration : CodeTypeDeclaration
    {
        public CodeLinqContextDeclaration(LinqSettingsDescriptor linqSettings)
        {
            PopulateMembers += OnPopulateMembers;
            PopulateBaseTypes += OnPopulateBaseTypes;
            LinqSettings = linqSettings;
            if (ContextClassBehaviour == ContextClassBehaviourType.PartialClass || ContextClassBehaviour == ContextClassBehaviourType.BasePartialClass)
            {
                IsPartial = true;
            }

            Name = !String.IsNullOrEmpty(LinqSettings.ContextName) ? LinqSettings.ContextName : "LinqContext";
        }

        protected LinqSettingsDescriptor LinqSettings
        {
            get;
            private set;
        }

        protected virtual void OnPopulateBaseTypes(object sender, EventArgs e)
        {
            if (ContextClassBehaviour == ContextClassBehaviourType.BaseClass || ContextClassBehaviour == ContextClassBehaviourType.BasePartialClass)
            {
                BaseTypes.Add(new CodeTypeReference("Worm.Linq.WormDBContext"));
            }
            if (ContextClassBehaviour == ContextClassBehaviourType.PartialClass)
            {
                BaseTypes.Add(new CodeTypeReference("Worm.Linq.WormContext"));
            }
        }

        protected virtual void OnPopulateMembers(object sender, EventArgs e)
        {
            foreach (var entityDescription in m_entities)
            {
                Members.Add(new CodeContextEntityWraperMember(entityDescription));
            }
            if(ContextClassBehaviour == ContextClassBehaviourType.BaseClass || ContextClassBehaviour == ContextClassBehaviourType.BasePartialClass)
            {
                var ctor = new CodeConstructor();
                ctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(Cache.OrmCache)),"cache"));
                ctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(ObjectMappingEngine)), "schema"));
                ctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(string)), "conn"));
                ctor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("cache"));
                ctor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("schema"));
                ctor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("conn"));
                Members.Add(ctor);
                ctor = new CodeConstructor();
                ctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(string)), "conn"));
                ctor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("conn"));
                Members.Add(ctor);
            }
        }

        public ContextClassBehaviourType ContextClassBehaviour
        {
            get
            {
                return LinqSettings.ContextClassBehaviour ?? ContextClassBehaviourType.BaseClass;
            }
        }

        public List<EntityDescription> Entities
        {
            get { return m_entities; }
        }

        private readonly List<EntityDescription> m_entities = new List<EntityDescription>();
    }

    public class CodeContextEntityWraperMember : CodeMemberProperty
    {
        private readonly EntityDescription m_entity;

        public CodeContextEntityWraperMember(EntityDescription entity)
        {
            m_entity = entity;
            Attributes = MemberAttributes.Public | MemberAttributes.Final;
            Name = (entity.RawNamespace ?? string.Empty) + entity.Name;
            var entityTypeReference = new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(Entity, true));
            Type = new CodeTypeReference("Worm.Linq.QueryWrapperT", entityTypeReference);
            SetStatements.Clear();
            GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeMethodInvokeExpression(
                        new CodeMethodReferenceExpression(
                            new CodeThisReferenceExpression(), "CreateQueryWrapper", entityTypeReference)
                        )
                    )
                );
        }

        public EntityDescription Entity
        {
            get
            {
                return m_entity;
            }
        }
    }
}
