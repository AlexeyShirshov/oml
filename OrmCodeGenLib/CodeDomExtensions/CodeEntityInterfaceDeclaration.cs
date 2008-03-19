using System.CodeDom;
using Worm.CodeGen.Core.Descriptors;

namespace Worm.CodeGen.Core.CodeDomExtensions
{
	public class CodeEntityInterfaceDeclaration : CodeTypeDeclaration
	{
		private CodeEntityTypeDeclaration m_entityTypeDeclaration;
		private readonly CodeTypeReference m_typeReference;

		public CodeEntityInterfaceDeclaration()
		{
			m_typeReference = new CodeTypeReference();

			IsClass = false;
			IsInterface = true;
		}

		public CodeEntityInterfaceDeclaration(CodeEntityTypeDeclaration entityTypeDeclaration) : this()
		{
			EntityTypeDeclaration = entityTypeDeclaration;

			m_typeReference.BaseType = FullName;
		}

		public string FullName
		{
			get
			{
				return OrmCodeGenNameHelper.GetEntityInterfaceName(Entity, true);
			}
		}

		public new string Name
		{
			get
			{
				if (Entity != null)
					return OrmCodeGenNameHelper.GetEntityInterfaceName(Entity, false);
				return null;
			}
		}

		public EntityDescription Entity
		{
			get
			{
				EntityDescription entity = null;
				if (EntityTypeDeclaration != null)
					entity = EntityTypeDeclaration.Entity;
				return entity;
			}
		}

		public CodeTypeReference TypeReference
		{
			get { return m_typeReference; }
		}

		public CodeEntityTypeDeclaration EntityTypeDeclaration
		{
			get { return m_entityTypeDeclaration; }
			set
			{
				m_entityTypeDeclaration = value;
				EnsureData();
			}
		}

		protected internal void EnsureData()
		{
			base.Name = Name;
			BaseTypes.Clear();
			if(Entity != null && Entity.BaseEntity != null && Entity.BaseEntity.MakeInterface)
			{
				BaseTypes.Add(new CodeTypeReference(OrmCodeGenNameHelper.GetEntityInterfaceName(Entity.BaseEntity, true)));
			}
		}
	}
}
