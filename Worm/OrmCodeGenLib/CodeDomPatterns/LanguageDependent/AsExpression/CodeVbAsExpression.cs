using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.CodeDom;

namespace OrmCodeGenLib.CodeDomPatterns
{
	public class CodeVbAsExpression : CodeAsExpressionBase
	{
		public CodeVbAsExpression(CodeTypeReference typeReference, CodeExpression expression) : base(typeReference, expression)
		{
			
		}
		protected override void RefreshValue()
		{
			using (Microsoft.VisualBasic.VBCodeProvider provider = new Microsoft.VisualBasic.VBCodeProvider())
			{
				System.CodeDom.Compiler.CodeGeneratorOptions opts = new System.CodeDom.Compiler.CodeGeneratorOptions();
				using (System.CodeDom.Compiler.IndentedTextWriter tw = new System.CodeDom.Compiler.IndentedTextWriter(new StringWriter(), opts.IndentString))
				{
					tw.Write("TryCast(");
					provider.GenerateCodeFromExpression(Expression, tw, opts);
					tw.Write(", ");
					provider.GenerateCodeFromExpression(new CodeTypeReferenceExpression(TypeReference), tw, opts);
					tw.Write(")");
					Value = tw.InnerWriter.ToString();
				}
			}
		}
	}
}
