using System.CodeDom;
using OrmCodeGenLib.Descriptors;

namespace OrmCodeGenLib.CodeDomExtensions
{
	/// <summary>
	/// Обертка над <see cref="CodeTypeDeclaration"/> применительно к <see cref="EntityDescription"/>
	/// </summary>
	public class CodeEntityTypeDeclaration : CodeTypeDeclaration
	{
		private EntityDescription m_entity;
		private CodeEntityInterfaceDeclaration m_entityInterface;
		private readonly CodeTypeReference m_typeReference;

		public CodeEntityTypeDeclaration()
		{
			m_typeReference = new CodeTypeReference();
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
