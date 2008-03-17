﻿using System;

namespace Helper
{
    public class QueryTypeAttribute : Attribute
    {
        QueryType methodType;

        public QueryType QueryType
        {
            get { return methodType; }
        }

        public QueryTypeAttribute(QueryType methodType)
        {
            this.methodType = methodType;
        }
    }
}
