using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;

namespace OrmCodeGenLib
{
    public static class CodeGenPatterns
    {
        public static CodeStatement[] CodePatternLock(CodeExpression lockExpression, params CodeStatement[] statements)
        {
            if (lockExpression == null)
                throw new ArgumentNullException("lockExpression");

            CodeStatement[] resutStatements;
            resutStatements = new CodeStatement[3];
            
            string lockCachedExpression = GetUniqueId("lockCachedExpression");

            resutStatements[0] = new CodeVariableDeclarationStatement(
                    new CodeTypeReference(typeof(object)),
                    lockCachedExpression,
                    lockExpression
                );

            resutStatements[1] = new CodeExpressionStatement(
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        new CodeTypeReferenceExpression(new CodeTypeReference(typeof (System.Threading.Monitor))),
                        typeof (System.Threading.Monitor).GetMethod("Enter").Name
                        ),
                    new CodeVariableReferenceExpression(lockCachedExpression)
                    )
                );

            resutStatements[2] = new CodeTryCatchFinallyStatement(
                    statements,
                    new CodeCatchClause[] {},
                    new CodeStatement[] {
                        (CodeStatement)
                        new CodeExpressionStatement(
                            new CodeMethodInvokeExpression(
                                new CodeMethodReferenceExpression(
                                    new CodeTypeReferenceExpression(new CodeTypeReference(typeof(System.Threading.Monitor))),
                                    typeof(System.Threading.Monitor).GetMethod("Exit").Name
                                ),
                                new CodeVariableReferenceExpression(lockCachedExpression)
                            )
                        )
                    }
                );

            return resutStatements;
        }

        public static CodeStatement CodePatternDoubleCheckLock(CodeExpression lockExpression, CodeExpression condition, params CodeStatement[] statements)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");

            return new CodeConditionStatement(
                condition,
                    CodePatternLock(lockExpression,
                        new CodeConditionStatement(
                            condition,
                            statements
                        )
                    )
                );
        }

        public static CodeStatement[] CodePatternUsingStatement(CodeExpression usingExpression, params CodeStatement[] statements)
        {
            List<CodeStatement> result = new List<CodeStatement>();
            string usingVariableName = GetUniqueId("usingVariable");
            result.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(IDisposable)), usingVariableName));
            result.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(usingVariableName),new CodePrimitiveExpression(null)));
            CodeStatement[] tryStatements = new CodeStatement[statements.Length + 1];
            Array.Copy(statements, 0, tryStatements, 1, statements.Length);
            tryStatements[0] =
                new CodeAssignStatement(new CodeArgumentReferenceExpression(usingVariableName), usingExpression);
            result.Add(
                new CodeTryCatchFinallyStatement(
                    tryStatements,
               new CodeCatchClause[] {
                                     },
               new CodeStatement[] {
                                       new CodeConditionStatement(
                                           new CodeBinaryOperatorExpression(
                                               new CodeVariableReferenceExpression(usingVariableName),
                                               CodeBinaryOperatorType.IdentityInequality,
                                               new CodePrimitiveExpression(null)
                                               ),
                                           new CodeExpressionStatement(
                                               new CodeMethodInvokeExpression(
                                                   new CodeCastExpression(typeof(IDisposable), new CodeArgumentReferenceExpression(usingVariableName)),
                                                   "Dispose",
                                                   new CodeExpression[]{}
                                                   )
                                               )
                                           )
                                   }
                )
            );
            return result.ToArray();
        }

        private static string GetUniqueId()
        {
            Guid guid = Guid.NewGuid();
            return guid.ToString("N");
        }

        private static string GetUniqueId(string prefix)
        {
            return prefix + "_" + GetUniqueId();
        }
    }
}
