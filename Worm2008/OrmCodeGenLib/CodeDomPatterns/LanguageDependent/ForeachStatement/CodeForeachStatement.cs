using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.CodeDom;

namespace Worm.CodeGen.Core.CodeDomPatterns
{
    public abstract class CodeForeachStatementBase : CodeSnippetStatement
    {
        private CodeExpression m_initStatement;
        private CodeExpression m_iterExpression;
        private CodeStatement[] m_statements;

        public CodeForeachStatementBase()
        {

        }

        public CodeForeachStatementBase(CodeExpression initExpression,
            CodeExpression iterExpression, params CodeStatement[] statements)
        {
            m_initStatement = initExpression;
            m_iterExpression = iterExpression;
            m_statements = statements;
            RefreshValue();
        }


        public CodeExpression InitStatement
        {
			get { return m_initStatement; }
            set
            {
                m_initStatement = value;
                RefreshValue();
            }
        }

        public CodeExpression IterExpression
        {
            get { return m_iterExpression; }
            set
            {
                m_iterExpression = value;
                RefreshValue();
            }
        }

        protected abstract void RefreshValue();

        public CodeStatement[] Statements
        {
            get { return m_statements; }
            set
            {
                m_statements = value;
                RefreshValue();
            }
        }
    }
}
