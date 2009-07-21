using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;

namespace Worm.CodeGen.Core.CodeDomExtensions
{
    public class CodeCompileFileUnit : CodeCompileUnit
    {
        public string Filename
        {
            get;
            set;
        }
    }
}
