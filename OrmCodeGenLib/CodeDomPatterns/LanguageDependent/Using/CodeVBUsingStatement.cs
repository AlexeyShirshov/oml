using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;
using System.IO;

namespace Worm.CodeGen.Core.CodeDomPatterns
{
    public class CodeVBUsingStatement : CodeUsingStatementBase
    {

        public CodeVBUsingStatement()
        {

        }

        public CodeVBUsingStatement(CodeExpression usingExpression, params CodeStatement[] statements)
            : base(usingExpression, statements)
        {
        }


        protected override void RefreshValue()
        {
            using (Microsoft.VisualBasic.VBCodeProvider provider = new Microsoft.VisualBasic.VBCodeProvider())
            {
                System.CodeDom.Compiler.CodeGeneratorOptions opts = new System.CodeDom.Compiler.CodeGeneratorOptions();
                using (System.CodeDom.Compiler.IndentedTextWriter tw = new System.CodeDom.Compiler.IndentedTextWriter(new StringWriter(), opts.IndentString))
                {
                    tw.Write("Using ");
                    if (UsingExpression != null)
                        provider.GenerateCodeFromExpression(UsingExpression, tw, opts);
                    tw.WriteLine();
                    tw.Indent++;
                    if (Statements != null)
                        foreach (CodeStatement statement in Statements)
                        {
                            provider.GenerateCodeFromStatement(statement, tw, opts);
                        }
                    tw.Indent--;
                    tw.Write("End Using");
                    Value = tw.InnerWriter.ToString();
                }
            }
        }
    }
}