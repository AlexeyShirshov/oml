using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.CodeDom;

namespace Worm.CodeGen.Core.CodeDomPatterns
{
	public class CodeVbIsExpression : CodeAsExpressionBase
	{
		public CodeVbIsExpression(CodeTypeReference typeReference, CodeExpression expression) : base(typeReference, expression)
		{
			
		}
		protected override void RefreshValue()
		{
			using (Microsoft.VisualBasic.VBCodeProvider provider = new Microsoft.VisualBasic.VBCodeProvider())
			{
				System.CodeDom.Compiler.CodeGeneratorOptions opts = new System.CodeDom.Compiler.CodeGeneratorOptions();
				using (System.CodeDom.Compiler.IndentedTextWriter tw = new System.CodeDom.Compiler.IndentedTextWriter(new StringWriter(), opts.IndentString))
				{
					tw.Write("( TypeOf ");
					provider.GenerateCodeFromExpression(Expression, tw, opts);
					tw.Write(" Is ");
					provider.GenerateCodeFromExpression(new CodeTypeReferenceExpression(TypeReference), tw, opts);
					tw.Write(")");
					Value = tw.InnerWriter.ToString();
				}
			}
		}
	}
}
