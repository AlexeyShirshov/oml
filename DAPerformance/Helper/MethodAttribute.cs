using System;

namespace Common
{
    public class QueryTypeAttribute : Attribute
    {
        QueryType methodType;
        Syntax syntax = Syntax.Default;

        public QueryType QueryType
        {
            get { return methodType; }
        }

        public Syntax SyntaxType
        {
            get { return syntax; }
        }

        public QueryTypeAttribute(QueryType methodType)
        {
            this.methodType = methodType;
        }

        public QueryTypeAttribute(QueryType methodType, Syntax syntax) : this(methodType)
        {
            this.syntax = syntax;
        }
    }
}
