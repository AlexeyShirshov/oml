using System.CodeDom;
using System.Collections.Generic;
using Worm.CodeGen.Core.Descriptors;

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
	    private readonly Dictionary<string, CodePropertiesAccessorTypeDeclaration> m_propertiesAccessor;

		public CodeEntityTypeDeclaration()
		{
			m_typeReference = new CodeTypeReference();
		    m_propertiesAccessor = new Dictionary<string, CodePropertiesAccessorTypeDeclaration>();
            PopulateMembers += OnPopulateMembers;
		}

        protected virtual void OnPopulateMembers(object sender, System.EventArgs e)
        {
            PopulatePropertiesAccessors();
        }

	    protected virtual void PopulatePropertiesAccessors()
	    {
	        foreach (var propertyDescription in Entity.Properties)
	        {
                if(propertyDescription.Group == null)
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

	    public CodeEntityTypeDeclaration(EntityDescription entity) : this()
		{
			Entity = entity;
			m_typeReference.BaseType = FullName;
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
				if(m_entityInterface != null)
				{
					m_entityInterface.EnsureData();
					// удалить существующий из списка дочерних типов
					if(this.BaseTypes.Contains(m_entityInterface.TypeReference))
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

	    public CodeTypeReference TypeReference
		{
			get { return m_typeReference; }
		}

		protected void EnsureName()
		{
			base.Name = Name;
		}
	}
}
