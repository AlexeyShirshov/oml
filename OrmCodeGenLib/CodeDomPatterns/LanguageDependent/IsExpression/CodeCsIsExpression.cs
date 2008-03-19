using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Worm.CodeGen.Core.CodeDomPatterns
{
	public class CodeCsIsExpression : CodeAsExpressionBase
	{
		public CodeCsIsExpression(CodeTypeReference typeReference, CodeExpression expression) : base(typeReference, expression)
		{
			
		}
		 
		protected override void RefreshValue()
		{
			using (Microsoft.CSharp.CSharpCodeProvider provider = new Microsoft.CSharp.CSharpCodeProvider())
			{
				System.CodeDom.Compiler.CodeGeneratorOptions opts = new System.CodeDom.Compiler.CodeGeneratorOptions();
				using (System.CodeDom.Compiler.IndentedTextWriter tw = new System.CodeDom.Compiler.IndentedTextWriter(new StringWriter(), opts.IndentString))
				{
					tw.Write("(");
					provider.GenerateCodeFromExpression(Expression, tw, opts);
					tw.Write(" is ");
					provider.GenerateCodeFromExpression(new CodeTypeReferenceExpression(TypeReference), tw, opts);
					tw.Write(")");
					Value = tw.InnerWriter.ToString();
				}
			}
		}
	}
}
