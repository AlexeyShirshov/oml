using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;
using System.IO;

namespace Worm.CodeGen.Core.CodeDomPatterns
{
    public class CodeVbForeachStatement : CodeForeachStatementBase
    {

        public CodeVbForeachStatement()
        {

        }

        public CodeVbForeachStatement(CodeExpression initExpression,
            CodeExpression iterExpression, params CodeStatement[] statements)
            : base(initExpression, iterExpression, statements)
        {
        }   


        protected override void RefreshValue()
        {
            using (Microsoft.VisualBasic.VBCodeProvider provider = new Microsoft.VisualBasic.VBCodeProvider())
            {
                System.CodeDom.Compiler.CodeGeneratorOptions opts = new System.CodeDom.Compiler.CodeGeneratorOptions();
                using (System.CodeDom.Compiler.IndentedTextWriter tw = new System.CodeDom.Compiler.IndentedTextWriter(new StringWriter(), opts.IndentString))
                {
                    tw.Write("For Each ");
                    provider.GenerateCodeFromExpression(InitStatement, tw, opts);
                    ((StringWriter)tw.InnerWriter).GetStringBuilder().Replace("ByVal pk ", "pk ");
                    tw.Write(" in ");
                    provider.GenerateCodeFromExpression(IterExpression, tw, opts);
                    tw.WriteLine();
                    tw.Indent++;
                    if (Statements != null)
                        foreach (CodeStatement statement in Statements)
                        {
                            provider.GenerateCodeFromStatement(statement, tw, opts);
                        }
                    tw.Indent--;
					tw.WriteLine("Next");
                    Value = tw.InnerWriter.ToString();
                }
            }
        }
    }
}
