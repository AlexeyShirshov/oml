using System;
using System.CodeDom;
using Worm.CodeGen.Core.CodeDomPatterns;
using Worm.CodeGen.Core.Descriptors;

namespace Worm.CodeGen.Core
{
    public partial class OrmCodeDomGenerator
    {
        public static class Delegates
        {
            public delegate void UpdateSetValueMethodDelegate(PropertyDescription propertyDesc, CodeMemberMethod setvalueMethod);
            public delegate CodeStatement[] CodePatternUsingStatementsDelegate(CodeExpression usingExpression, params CodeStatement[] statements);
            public delegate CodeExpression CodePatternIsExpressionDelegate(CodeTypeReference typeReference, CodeExpression expression);
            public delegate CodeExpression CodePatternAsExpressionDelegate(CodeTypeReference typeReference, CodeExpression expression);
            public delegate CodeStatement CodePatternLockStatementDelegate(CodeExpression lockExpression, params CodeStatement[] statements);
            public delegate CodeExpression CodePatternXorExpressionDelegate(CodeExpression left, CodeExpression right);
			public delegate CodeStatement CodePatternForeachStatementDelegate(CodeTypeReference iterationItemType, string iterationItemName, CodeExpression iterExpression, params CodeStatement[] statements);

        	public delegate CodeTypeMember CodeMemberOperatorOverrideDelegate(
        		OperatorType op, CodeTypeReference returnType, CodeParameterDeclarationExpression[] prms,
        		CodeStatement[] statements);

            public static event GetSettingsDelegate SettingsRequied;

            private static OrmCodeDomGeneratorSettings GetSettings()
            {
                OrmCodeDomGeneratorSettings settings = null;
                var h = SettingsRequied;
                if (h != null)
                    settings = h();
                if (settings == null) throw new Exception("OrmCodeDomGeneratorSettings requied.");
                return settings;
            }

            public static UpdateSetValueMethodDelegate UpdateSetValueMethodMethod
            {
                get
                {
                    OrmCodeDomGeneratorSettings settings = GetSettings();
                    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.SafeUnboxToEnum) ==
                        LanguageSpecificHacks.SafeUnboxToEnum)
                        return UpdateSetValueMethodDelegates.EnumPervUpdateSetValueMethod;
                    return UpdateSetValueMethodDelegates.DefaultUpdateSetValueMethod;
                }
            }

            public static CodePatternUsingStatementsDelegate CodePatternUsingStatements
            {
                get
                {
                    OrmCodeDomGeneratorSettings settings = GetSettings();
                    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateCSUsingStatement) ==
                        LanguageSpecificHacks.GenerateCSUsingStatement)
                        return CodePatternUsingStatementDelegates.CSUsing;
                    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateVBUsingStatement) ==
                        LanguageSpecificHacks.GenerateVBUsingStatement)
                        return CodePatternUsingStatementDelegates.VBUsing;
                    return CodePatternUsingStatementDelegates.CommonUsing;
                }
            }

            public static CodePatternIsExpressionDelegate CodePatternIsExpression
            {
                get
                {
                    OrmCodeDomGeneratorSettings settings = GetSettings();
                    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateCsIsStatement) == LanguageSpecificHacks.GenerateCsIsStatement)
                        return CodePatternIsExpressionDelegates.CsExpression;
                    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateVbTypeOfIsStatement) == LanguageSpecificHacks.GenerateVbTypeOfIsStatement)
                        return CodePatternIsExpressionDelegates.VbExpression;
                    return CodePatternIsExpressionDelegates.CommonExpression;
                }
            }

            public static CodePatternAsExpressionDelegate CodePatternAsExpression
            {
                get
                {
                    OrmCodeDomGeneratorSettings settings = GetSettings();
                    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateCsAsStatement) ==
                        LanguageSpecificHacks.GenerateCsAsStatement)
                        return CodePatternAsExpressionDelegates.CsExpression;
                    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateVbTryCastStatement) ==
                        LanguageSpecificHacks.GenerateVbTryCastStatement)
                        return CodePatternAsExpressionDelegates.VbExpression;
                    return CodePatternAsExpressionDelegates.CommonExpression;
                }
            }
            public static CodePatternLockStatementDelegate CodePatternLockStatement
            {
                get
                {
                    OrmCodeDomGeneratorSettings settings = GetSettings();
                    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateCsLockStatement) == LanguageSpecificHacks.GenerateCsLockStatement)
                        return CodePatternLockStatementDelegates.CsStatement;
                    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateCsLockStatement) == LanguageSpecificHacks.GenerateCsLockStatement)
                        return CodePatternLockStatementDelegates.VbStatement;
                    return CodePatternLockStatementDelegates.CommonStatement;
                }
            }

            public static CodePatternXorExpressionDelegate CodePatternXorStatement
            {
                get
                {
                    OrmCodeDomGeneratorSettings settings = GetSettings();
                    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateCsXorStatement) == LanguageSpecificHacks.GenerateCsXorStatement)
                        return CodePatternXorExpressionDelegates.CsExpression;
                    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateVbXorStatement) == LanguageSpecificHacks.GenerateVbXorStatement)
                        return CodePatternXorExpressionDelegates.VbExpression;
                    return CodePatternXorExpressionDelegates.CommonExpression;
                }
            }

            public static CodePatternForeachStatementDelegate CodePatternForeachStatement
            {
                get
                {
                    OrmCodeDomGeneratorSettings settings = GetSettings();
                    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateCsForeachStatement) == LanguageSpecificHacks.GenerateCsForeachStatement)
                        return CodePatternForeachStatementDelegates.CsStatement;
                    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateVbForeachStatement) == LanguageSpecificHacks.GenerateVbForeachStatement)
                        return CodePatternForeachStatementDelegates.VbStatement;
                    return CodePatternForeachStatementDelegates.CommonStatement;
                }
            }

        	public static CodeMemberOperatorOverrideDelegate CodeMemberOperatorOverride
        	{
        		get
        		{
					OrmCodeDomGeneratorSettings settings = GetSettings();
					if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateCsForeachStatement) == LanguageSpecificHacks.GenerateCsForeachStatement)
						return CodeMemberOperatorOverrideDelegates.CsStatement;
					if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.GenerateVbForeachStatement) == LanguageSpecificHacks.GenerateVbForeachStatement)
						return CodeMemberOperatorOverrideDelegates.VbStatement;
					return CodeMemberOperatorOverrideDelegates.CommonStatement;
        		}
        	}
            /// <summary>
            /// void UpdateSetValueMethodDelegate(CodeMemberField field, PropertyDescription propertyDesc, CodeMemberMethod setvalueMethod);
            /// </summary>
            public static class UpdateSetValueMethodDelegates
            {
                public static void DefaultUpdateSetValueMethod(PropertyDescription propertyDesc, CodeMemberMethod setvalueMethod)
                {
                    //Type fieldRealType;
                    //fieldRealType = Type.GetType(field.Type.BaseType, false);

                    var setValueStatement = new CodeConditionStatement(
                        new CodeMethodInvokeExpression(
                            OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc),
                            "Equals",
                            new CodeVariableReferenceExpression("fieldName"))
                        );

                	var fieldName = OrmCodeGenNameHelper.GetPrivateMemberName(propertyDesc.PropertyName);

                    //setValueStatement.TrueStatements.Add(
                    //    new CodeVariableDeclarationStatement(typeof(IConvertible), "vConv",
                    //        new Codety)
                    //    );

                    //old: simple cast
                    //setValueStatement.TrueStatements.Add(new CodeAssignStatement(
                    //                         new CodeFieldReferenceExpression(
                    //                             new CodeThisReferenceExpression(), field.Name),
                    //                         new CodeCastExpression(field.Type,
                    //                                                new CodeArgumentReferenceExpression(
                    //                                                    "value"))));

                    // new: solves problem of direct casts with Nullable<>
                    if (propertyDesc.PropertyType.IsNullableType && propertyDesc.PropertyType.IsClrType && propertyDesc.PropertyType.ClrType.GetGenericArguments()[0].IsValueType && !propertyDesc.PropertyType.ClrType.GetGenericArguments()[0].Equals(typeof(Guid)))
                    {
                        setValueStatement.TrueStatements.Add(
                            new CodeVariableDeclarationStatement(typeof(IConvertible), "iconvVal",
                                                                 CodePatternAsExpression(new CodeTypeReference(typeof(IConvertible)),
                                                                                         new CodeArgumentReferenceExpression("value"))));
                        setValueStatement.TrueStatements.Add(
                            new CodeConditionStatement(
                                new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("iconvVal"),
                                                                 CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(null)),
                                new CodeStatement[]
									{
										new CodeAssignStatement(
											new CodeFieldReferenceExpression(
												new CodeThisReferenceExpression(), fieldName),
											new CodeCastExpression(propertyDesc.PropertyType,
											                       new CodeArgumentReferenceExpression(
											                       	"value")))
									},
                                new CodeStatement[]
									{
										//System.Threading.Thread.CurrentThread.CurrentCulture
										new CodeAssignStatement(
											new CodeFieldReferenceExpression(
												new CodeThisReferenceExpression(), fieldName),
											new CodeMethodInvokeExpression(
											new CodeVariableReferenceExpression("iconvVal"),
											GetIConvertableMethodName(propertyDesc.PropertyType.ClrType.GetGenericArguments()[0]),
											new CodePropertyReferenceExpression(
												new CodePropertyReferenceExpression(
													new CodeTypeReferenceExpression(typeof(System.Threading.Thread)),
													"CurrentThread"
													),
													"CurrentCulture"
												)
											)
											)
										
									}
                                )
                            );
                    }
                    else if (propertyDesc.PropertyType.IsValueType && (!propertyDesc.PropertyType.IsNullableType || !(propertyDesc.PropertyType.IsClrType && propertyDesc.PropertyType.ClrType.GetGenericArguments()[0].Equals(typeof(Guid)))))
                    {
                        //old: simple cast
                        setValueStatement.TrueStatements.Add(new CodeAssignStatement(
                                                 new CodeFieldReferenceExpression(
                                                     new CodeThisReferenceExpression(), fieldName),
                                                 new CodeCastExpression(propertyDesc.PropertyType,
                                                     new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(Convert))), "ChangeType",
																		new CodeArgumentReferenceExpression("value"), new CodeTypeOfExpression(propertyDesc.PropertyType)))));
                    }
                    else
                    {
                        setValueStatement.TrueStatements.Add(new CodeAssignStatement(
                                                 new CodeFieldReferenceExpression(
                                                     new CodeThisReferenceExpression(), fieldName),
                                                 new CodeCastExpression(propertyDesc.PropertyType, new CodeArgumentReferenceExpression("value"))));
                    }
                    setValueStatement.TrueStatements.Add(new CodeMethodReturnStatement());
                    setvalueMethod.Statements.Add(setValueStatement);
                }

                private static string GetIConvertableMethodName(Type type)
                {
                    return "To" + type.Name;
                }

                public static void EnumPervUpdateSetValueMethod(PropertyDescription propertyDesc, CodeMemberMethod setvalueMethod)
                {
                	var fieldName = OrmCodeGenNameHelper.GetPrivateMemberName(propertyDesc.PropertyName);
                    if (propertyDesc.PropertyType.IsEnum)
                    {
                        var setValueStatement = new CodeConditionStatement(
                        new CodeMethodInvokeExpression(
                            OrmCodeGenHelper.GetFieldNameReferenceExpression(propertyDesc),
                            "Equals",
                            new CodeVariableReferenceExpression("fieldName"))

                        );
                        if (propertyDesc.PropertyType.IsNullableType)
                        {
                            setValueStatement.TrueStatements.Add(
                                new CodeConditionStatement(
                                    new CodeBinaryOperatorExpression(
                                        new CodeArgumentReferenceExpression("value"),
                                        CodeBinaryOperatorType.IdentityEquality,
                                        new CodePrimitiveExpression(null)
                                    ),
                                    new CodeStatement[]
                                {
                                    new CodeAssignStatement(
                                                         new CodeFieldReferenceExpression(
                                                             new CodeThisReferenceExpression(), fieldName),
                                                         new CodePrimitiveExpression(null))
                                },
                                    new CodeStatement[]
                                {
                                    new CodeVariableDeclarationStatement(
                                        new CodeTypeReference(typeof(Type)),
                                        "t",
                                        new CodeArrayIndexerExpression(
                                                                new CodeMethodInvokeExpression(
                                                                    new CodeTypeOfExpression(propertyDesc.PropertyType),
                                                                    "GetGenericArguments"
                                                                ),
                                                                new CodePrimitiveExpression(0)
                                                            )
                                    ),
                                    new CodeAssignStatement(
                                                         new CodeFieldReferenceExpression(
                                                             new CodeThisReferenceExpression(), fieldName),
                                                         new CodeCastExpression(
                                                         propertyDesc.PropertyType,
                                                         new CodeMethodInvokeExpression(
                                                            new CodeTypeReferenceExpression(typeof(Enum)),
                                                            "ToObject",
                                                            // typeof(Nullable<int>).GetGenericArguments()[0]
                                                            new CodeVariableReferenceExpression("t"),
                                                            new CodeArgumentReferenceExpression(
                                                                                    "value")
                                    )))
                    
                                                            
                                 }

                                )
                            );
                        }
                        else
                        {
                            setValueStatement.TrueStatements.Add(new CodeAssignStatement(
                                                                 new CodeFieldReferenceExpression(
                                                                     new CodeThisReferenceExpression(), fieldName),
                                                                 new CodeCastExpression(
																 propertyDesc.PropertyType,
                                                                 new CodeMethodInvokeExpression(
                                                                    new CodeTypeReferenceExpression(typeof(Enum)),
                                                                    "ToObject",
                                // typeof(Nullable<int>).GetGenericArguments()[0]
																	new CodeTypeOfExpression(propertyDesc.PropertyType),
                                                                    new CodeArgumentReferenceExpression(
                                                                                            "value")
                                            ))));
                        }
                        setValueStatement.TrueStatements.Add(new CodeMethodReturnStatement());
                        setvalueMethod.Statements.Add(setValueStatement);
                    }
                    else
                    {
                        DefaultUpdateSetValueMethod(propertyDesc, setvalueMethod);
                    }
                }
            }

            //CodeStatement[] CodePatternUsingStatementsDelegate(CodeExpression usingExpression, params CodeStatement[] statements);

            public static class CodePatternUsingStatementDelegates
            {
                public static CodeStatement[] CSUsing(CodeExpression usingExpression, params CodeStatement[] statements)
                {
                    return new CodeStatement[] { new CodeCSUsingStatement(usingExpression, statements) };
                }

                public static CodeStatement[] VBUsing(CodeExpression usingExpression, params CodeStatement[] statements)
                {
                    return new CodeStatement[] { new CodeVBUsingStatement(usingExpression, statements) };
                }

                public static CodeStatement[] CommonUsing(CodeExpression usingExpression, params CodeStatement[] statements)
                {
                    return CommonPatterns.CodePatternUsingStatement(usingExpression, statements);
                }
            }

            //CodeExpression CodePatternIsExpressionDelegate(CodeTypeReference typeReference, CodeExpression expression);

            public static class CodePatternIsExpressionDelegates
            {
                public static CodeExpression CsExpression(CodeTypeReference typeReference, CodeExpression expression)
                {
                    return new CodeCsIsExpression(typeReference, expression);
                }

                public static CodeExpression VbExpression(CodeTypeReference typeReference, CodeExpression expression)
                {
                    return new CodeVbIsExpression(typeReference, expression);
                }

                public static CodeExpression CommonExpression(CodeTypeReference typeReference, CodeExpression expression)
                {
                    return CommonPatterns.CodeIsExpression(typeReference, expression);
                }
            }

            //CodeExpression CodePatternAsExpressionDelegate(CodeTypeReference typeReference, CodeExpression expression);

            public static class CodePatternAsExpressionDelegates
            {
                public static CodeExpression CsExpression(CodeTypeReference typeReference, CodeExpression expression)
                {
                    return new CodeCsAsExpression(typeReference, expression);
                }

                public static CodeExpression VbExpression(CodeTypeReference typeReference, CodeExpression expression)
                {
                    return new CodeVbAsExpression(typeReference, expression);
                }

                public static CodeExpression CommonExpression(CodeTypeReference typeReference, CodeExpression expression)
                {
                    return CommonPatterns.CodeAsExpression(typeReference, expression);
                }
            }

            //public delegate CodeStatement CodePatternLockStatementDelegate(CodeExpression lockExpression, params CodeStatement[] statements);
            public static class CodePatternLockStatementDelegates
            {
                public static CodeStatement CsStatement(CodeExpression lockExpression, params CodeStatement[] statements)
                {
                    return new CodeCsLockStatement(lockExpression, statements);
                }

                public static CodeStatement VbStatement(CodeExpression lockExpression, params CodeStatement[] statements)
                {
                    return new CodeVbLockStatement(lockExpression, statements);
                }

                public static CodeStatement CommonStatement(CodeExpression lockExpression, params CodeStatement[] statements)
                {
                    return CommonPatterns.CodePatternLock(lockExpression, statements);
                }
            }

            public static class CodePatternXorExpressionDelegates
            {
                public static CodeExpression CsExpression(CodeExpression left, CodeExpression right)
                {
                    return new CodeCsXorExpression(left, right);
                }

                public static CodeExpression VbExpression(CodeExpression left, CodeExpression right)
                {
                    return new CodeVbXorExpression(left, right);
                }

                public static CodeExpression CommonExpression(CodeExpression left, CodeExpression right)
                {
                    throw new NotImplementedException();
                }
            }

            public static class CodePatternForeachStatementDelegates
            {
				public static CodeStatement CsStatement(CodeTypeReference iterationItemType, string iterationItemName,
			CodeExpression iterExpression, params CodeStatement[] statements)
                {
                    return new CodeCsForeachStatement(iterationItemType, iterationItemName, iterExpression, statements);
                }

				public static CodeStatement VbStatement(CodeTypeReference iterationItemType, string iterationItemName,
			CodeExpression iterExpression, params CodeStatement[] statements)
                {
					return new CodeVbForeachStatement(iterationItemType, iterationItemName, iterExpression, statements);
                }

				public static CodeStatement CommonStatement(CodeTypeReference iterationItemType, string iterationItemName,
			CodeExpression iterExpression, params CodeStatement[] statements)
                {
                    throw new NotImplementedException();
                }
            }

			public static class CodeMemberOperatorOverrideDelegates
			{
				public static CodeMemberOperatorOverride CsStatement(
					OperatorType op, CodeTypeReference returnType, CodeParameterDeclarationExpression[] prms,
					CodeStatement[] statements)
				{
					return new CodeCsMemberOperatorOverride
					       	{

					       		Operator = op,
					       		ReturnType = returnType,
					       		Parameters = prms,
					       		Statements = statements
					       	};
				}

				public static CodeMemberOperatorOverride VbStatement(
					OperatorType op, CodeTypeReference returnType, CodeParameterDeclarationExpression[] prms,
					CodeStatement[] statements)
				{
					return new CodeVbMemberOperatorOverride
					       	{

						Operator = op,
						ReturnType = returnType,
						Parameters = prms,
						Statements = statements
					};
				}

				public static CodeMemberOperatorOverride CommonStatement(
					OperatorType op, CodeTypeReference returnType, CodeParameterDeclarationExpression[] prms,
					CodeStatement[] statements)
				{
					throw new NotImplementedException();
				}
			}
        }
    }
}
