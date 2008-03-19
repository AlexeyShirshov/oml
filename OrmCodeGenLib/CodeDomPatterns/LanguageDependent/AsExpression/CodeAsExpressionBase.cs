using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;

namespace Worm.CodeGen.Core.CodeDomPatterns
{
	public abstract class CodeAsExpressionBase : CodeSnippetExpression
	{
		private CodeTypeReference m_typeReference;
		private CodeExpression m_expression;

		public CodeAsExpressionBase(CodeTypeReference type, CodeExpression expression)
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
