using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;

namespace OrmCodeGenLib.CodeDomPatterns
{
	public abstract class CodeIsExpressionBase : CodeSnippetExpression
	{
		private CodeTypeReference m_typeReference;
		private CodeExpression m_expression;

		public CodeIsExpressionBase(CodeTypeReference type, CodeExpression expression)
		{
			m_typeReference = type;
			m_expression = expression;
			RefreshValue();
			
		}

		public CodeTypeReference TypeReference
		{
			get { return m_typeReference; }
			set 
			{
				if (m_typeReference != value)
				{
					m_typeReference = value;
					RefreshValue();
				}
			}
		}

		public CodeExpression Expression
		{
			get { return m_expression; }
			set
			{
				if (m_expression != value)
				{
					m_expression = value;
					RefreshValue();
				}
			}
		}

		protected abstract void RefreshValue();
	}
}
