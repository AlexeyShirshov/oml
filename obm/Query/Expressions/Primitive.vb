﻿Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta

Namespace Expressions2

    <Serializable()> _
    Public Class UnaryExpression
        Implements IComplexExpression

        Private _oper As UnaryOperationType
        Private _v As IExpression

        Public Sub New(ByVal oper As UnaryOperationType, ByVal operand As IExpression)
            _oper = oper
            _v = operand
        End Sub

        Public ReadOnly Property ShouldUse() As Boolean Implements IExpression.ShouldUse
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property Expression() As IExpression Implements IGetExpression.Expression
            Get
                Return Me
            End Get
        End Property

        Public Overloads Function Equals(ByVal f As IQueryElement) As Boolean Implements IQueryElement.Equals
            Return Equals(TryCast(f, UnaryExpression))
        End Function

        Public Overloads Function Equals(ByVal f As UnaryExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If
            Return _oper = f._oper AndAlso _v.Equals(f._v)
        End Function

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            Return OperationType2String(_oper) & _v.GetDynamicString
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            Return OperationType2String(_oper) & _v.GetStaticString(mpe, contextFilter)
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            _v.Prepare(executor, schema, filterInfo, stmt, isAnonym)
        End Sub

        Public Function Clone() As Object Implements System.ICloneable.Clone
            Return New UnaryExpression(_oper, CloneExpression(_v))
        End Function

        Public Function RemoveExpression(ByVal f As IComplexExpression) As IComplexExpression Implements IComplexExpression.RemoveExpression
            If Equals(f) Then
                Return Nothing
            End If
            Dim e As IComplexExpression = TryCast(_v, IComplexExpression)
            If e IsNot Nothing Then
                Dim v As IExpression = e.RemoveExpression(f)
                If v IsNot _v Then
                    Return New UnaryExpression(_oper, v)
                End If
            End If
            Return Me
        End Function

        Public Function ReplaceExpression(ByVal replacement As IComplexExpression, ByVal replacer As IComplexExpression) As IComplexExpression Implements IComplexExpression.ReplaceExpression
            If Equals(replacement) Then
                Return replacer
            End If
            Dim e As IComplexExpression = TryCast(_v, IComplexExpression)
            If e IsNot Nothing Then
                Dim v As IExpression = e.RemoveExpression(replacement)
                If v IsNot _v Then
                    Return New UnaryExpression(_oper, v)
                End If
            End If
            Return Me
        End Function

        Public Function GetExpressions() As System.Collections.Generic.ICollection(Of IExpression) Implements IExpression.GetExpressions
            Dim l As New List(Of IExpression)
            l.Add(Me)
            l.AddRange(_v.GetExpressions)
            Return l
        End Function

        Public Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal filterInfo As Object, ByVal inSelect As Boolean, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            Return stmt.UnaryOperator2String(_oper) & _v.MakeStatement(schema, fromClause, stmt, paramMgr, almgr, filterInfo, inSelect, executor)
        End Function

        Public Function MakeHash(ByVal schema As ObjectMappingEngine, ByVal oschema As Entities.Meta.IEntitySchema, ByVal obj As Entities.ICachedEntity) As String Implements IHashable.MakeHash
            Return EmptyHash
        End Function

        Public Function Test(ByVal schema As ObjectMappingEngine, ByVal obj As Entities._IEntity, ByVal oschema As Entities.Meta.IEntitySchema) As IParameterExpression.EvalResult Implements IHashable.Test
            Throw New NotSupportedException
        End Function

    End Class

    <Serializable()> _
    Public Class LiteralExpression
        Implements IExpression

        Private _literalValue As String

        Public Sub New(ByVal literal As String)
            _literalValue = literal
        End Sub

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            Return _literalValue
        End Function

        Public Overridable ReadOnly Property ShouldUse() As Boolean Implements IExpression.ShouldUse
            Get
                Return True
            End Get
        End Property

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            Return "litval"
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            'do nothing
        End Sub

        Public Function GetExpressions() As System.Collections.Generic.ICollection(Of IExpression) Implements IExpression.GetExpressions
            Return New IExpression() {Me}
        End Function

        Public Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal inSelect As Boolean, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            Return _literalValue
        End Function

        Public ReadOnly Property Expression() As IExpression Implements IGetExpression.Expression
            Get
                Return Me
            End Get
        End Property

        Public Overloads Function Equals(ByVal f As IQueryElement) As Boolean Implements IQueryElement.Equals
            Return Equals(TryCast(f, LiteralExpression))
        End Function

        Public Overloads Function Equals(ByVal f As LiteralExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If
            Return _literalValue.Equals(f._literalValue)
        End Function
    End Class

    <Serializable()> _
    Public Class DBNullExpression
        Inherits LiteralExpression
        Implements IParameterExpression

        Public Sub New()
            MyBase.New("null")
        End Sub

        Public Function Test(ByVal oper As BinaryOperationType, ByVal v As Object, ByVal mpe As ObjectMappingEngine) As IParameterExpression.EvalResult Implements IParameterExpression.Test
            If oper = BinaryOperationType.Is Then
                If v Is Nothing Then
                    Return IParameterExpression.EvalResult.Found
                Else
                    Return IParameterExpression.EvalResult.NotFound
                End If
            ElseIf oper = BinaryOperationType.IsNot Then
                If v IsNot Nothing Then
                    Return IParameterExpression.EvalResult.Found
                Else
                    Return IParameterExpression.EvalResult.NotFound
                End If
            Else
                Throw New NotSupportedException(String.Format("Operation {0} is not supported for IsNull statement", oper))
            End If
        End Function

        Public ReadOnly Property Value() As Object Implements IParameterExpression.Value
            Get
                Return Nothing
            End Get
        End Property

        Public Event ModifyValue(ByVal sender As IParameterExpression, ByVal args As IParameterExpression.ModifyValueArgs) Implements IParameterExpression.ModifyValue
    End Class

    <Serializable()> _
    Public Class QueryExpression
        Implements IExpression

        Private _q As Query.QueryCmd

        Public Function GetExpressions() As System.Collections.Generic.ICollection(Of IExpression) Implements IExpression.GetExpressions
            Return New IExpression() {Me}
        End Function

        Public Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal inSelect As Boolean, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            Dim sb As New StringBuilder
            sb.Append("(")
            sb.Append(stmt.MakeQueryStatement(schema, fromClause, contextFilter, _q, paramMgr, almgr))
            sb.Append(")")
            Return sb.ToString
        End Function

        Public ReadOnly Property ShouldUse() As Boolean Implements IExpression.ShouldUse
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property Expression() As IExpression Implements IGetExpression.Expression
            Get
                Return Me
            End Get
        End Property

        Public Overloads Function Equals(ByVal f As IQueryElement) As Boolean Implements IQueryElement.Equals
            Return Equals(TryCast(f, QueryExpression))
        End Function

        Public Overloads Function Equals(ByVal f As QueryExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If
            Return _q.Equals(f._q)
        End Function

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            Return _q._ToString
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            Return _q.GetStaticString(mpe, contextFilter)
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextFilter As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            _q.Prepare(executor, schema, contextFilter, stmt, isAnonym)
        End Sub
    End Class

    Module Helper
        Public Const EmptyHash As String = "fd_empty_hash_aldf"

        Public Function CloneExpression(ByVal exp As IExpression) As IExpression
            Dim c As ICloneable = TryCast(exp, ICloneable)
            If c IsNot Nothing Then
                Return CType(c.Clone, IExpression)
            End If
            Return exp
        End Function

        Public Function OperationType2String(ByVal oper As UnaryOperationType) As String
            Select Case oper
                Case UnaryOperationType.Negate
                    Return "neg"
                Case UnaryOperationType.Not
                    Return "not"
                Case Else
                    Throw New NotSupportedException(oper.ToString)
            End Select
        End Function

        Public Function OperationType2String(ByVal oper As BinaryOperationType) As String
            Select Case oper
                Case BinaryOperationType.Equal
                    Return "Equal"
                Case BinaryOperationType.GreaterEqualThan
                    Return "GreaterEqualThan"
                Case BinaryOperationType.GreaterThan
                    Return "GreaterThan"
                Case BinaryOperationType.In
                    Return "In"
                Case BinaryOperationType.LessEqualThan
                    Return "LessEqualThan"
                Case BinaryOperationType.NotEqual
                    Return "NotEqual"
                Case BinaryOperationType.NotIn
                    Return "NotIn"
                Case BinaryOperationType.Like
                    Return "Like"
                Case BinaryOperationType.LessThan
                    Return "LessThan"
                Case BinaryOperationType.Is
                    Return "Is"
                Case BinaryOperationType.IsNot
                    Return "IsNot"
                Case BinaryOperationType.Exists
                    Return "Exists"
                Case BinaryOperationType.NotExists
                    Return "NotExists"
                Case BinaryOperationType.Between
                    Return "Between"
                Case Else
                    Throw New ObjectMappingException("Operation " & oper & " not supported")
            End Select
        End Function

        Public Function Test(ByVal filterValue As Object, ByVal evaluatedValue As Object, _
                             ByVal oper As BinaryOperationType, ByVal [case] As Boolean, _
                             ByVal mpe As ObjectMappingEngine) As IParameterExpression.EvalResult

            Dim r As IParameterExpression.EvalResult

            If r <> IParameterExpression.EvalResult.Unknown Then
                Return r
            Else
                r = IParameterExpression.EvalResult.NotFound
            End If

            If filterValue IsNot Nothing AndAlso evaluatedValue IsNot Nothing Then
                Dim vt As Type = evaluatedValue.GetType()
                Dim valt As Type = filterValue.GetType
                If Not vt.IsAssignableFrom(valt) AndAlso ( _
                    (vt.IsPrimitive AndAlso valt.IsPrimitive) OrElse _
                    (vt.IsValueType AndAlso valt.IsValueType)) Then
                    filterValue = Convert.ChangeType(filterValue, evaluatedValue.GetType)
                ElseIf vt.IsArray <> valt.IsArray Then
                    Return IParameterExpression.EvalResult.Unknown
                End If
            End If

            Select Case oper
                Case BinaryOperationType.Equal
                    If Equals(evaluatedValue, filterValue) Then
                        r = IParameterExpression.EvalResult.Found
                    ElseIf evaluatedValue IsNot Nothing Then
                        Dim vt As Type = evaluatedValue.GetType()
                        If GetType(IKeyEntity).IsAssignableFrom(vt) Then
                            If Equals(CType(evaluatedValue, IKeyEntity).Identifier, filterValue) Then
                                r = IParameterExpression.EvalResult.Found
                            End If
                        ElseIf GetType(ICachedEntity).IsAssignableFrom(vt) Then
                            Dim pks() As PKDesc = CType(evaluatedValue, ICachedEntity).GetPKValues
                            If pks.Length <> 1 Then
                                Throw New ObjectMappingException(String.Format("Type {0} has complex primary key", vt))
                            End If
                            If Equals(pks(0).Value, filterValue) Then
                                r = IParameterExpression.EvalResult.Found
                            End If
                        ElseIf ObjectMappingEngine.IsEntityType(vt, mpe) Then
                            Dim pks As IList(Of EntityPropertyAttribute) = mpe.GetPrimaryKeys(vt)
                            If pks.Count <> 1 Then
                                Throw New ObjectMappingException(String.Format("Type {0} has complex primary key", vt))
                            End If
                            If Equals(mpe.GetPropertyValue(evaluatedValue, pks(0).PropertyAlias, Nothing), filterValue) Then
                                r = IParameterExpression.EvalResult.Found
                            End If
                        End If
                    End If
                Case BinaryOperationType.GreaterEqualThan
                    Dim c As IComparable = CType(evaluatedValue, IComparable)
                    If c Is Nothing Then
                        If filterValue Is Nothing Then
                            r = IParameterExpression.EvalResult.Unknown
                        Else
                            r = IParameterExpression.EvalResult.NotFound
                        End If
                    Else
                        Dim i As Integer = c.CompareTo(filterValue)
                        If i >= 0 Then
                            r = IParameterExpression.EvalResult.Found
                        End If
                    End If
                Case BinaryOperationType.GreaterThan
                    Dim c As IComparable = CType(evaluatedValue, IComparable)
                    If c Is Nothing Then
                        If filterValue Is Nothing Then
                            r = IParameterExpression.EvalResult.Unknown
                        Else
                            r = IParameterExpression.EvalResult.NotFound
                        End If
                    Else
                        Dim i As Integer = c.CompareTo(filterValue)
                        If i > 0 Then
                            r = IParameterExpression.EvalResult.Found
                        End If
                    End If
                Case BinaryOperationType.LessEqualThan
                    Dim c As IComparable = CType(filterValue, IComparable)
                    If c Is Nothing Then
                        If evaluatedValue Is Nothing Then
                            r = IParameterExpression.EvalResult.Unknown
                        Else
                            r = IParameterExpression.EvalResult.NotFound
                        End If
                    Else
                        Dim i As Integer = c.CompareTo(evaluatedValue)
                        If i >= 0 Then
                            r = IParameterExpression.EvalResult.Found
                        End If
                    End If
                Case BinaryOperationType.LessThan
                    Dim c As IComparable = CType(filterValue, IComparable)
                    If c Is Nothing Then
                        If evaluatedValue Is Nothing Then
                            r = IParameterExpression.EvalResult.Unknown
                        Else
                            r = IParameterExpression.EvalResult.NotFound
                        End If
                    Else
                        Dim i As Integer = c.CompareTo(evaluatedValue)
                        If i > 0 Then
                            r = IParameterExpression.EvalResult.Found
                        End If
                    End If
                Case BinaryOperationType.NotEqual
                    If Not Equals(evaluatedValue, filterValue) Then
                        r = IParameterExpression.EvalResult.Found
                    End If
                Case BinaryOperationType.Like
                    If filterValue Is Nothing OrElse evaluatedValue Is Nothing Then
                        r = IParameterExpression.EvalResult.Unknown
                    Else
                        Dim par As String = CStr(filterValue)
                        Dim str As String = CStr(evaluatedValue)
                        r = IParameterExpression.EvalResult.NotFound
                        Dim sc As StringComparison = StringComparison.InvariantCulture
                        If Not [case] Then
                            sc = StringComparison.InvariantCultureIgnoreCase
                        End If
                        If par.StartsWith("%") Then
                            If par.EndsWith("%") Then
                                If str.IndexOf(par.Trim("%"c), sc) >= 0 Then
                                    r = IParameterExpression.EvalResult.Found
                                End If
                            Else
                                If str.EndsWith(par.TrimStart("%"c), sc) Then
                                    r = IParameterExpression.EvalResult.Found
                                End If
                            End If
                        ElseIf par.EndsWith("%") Then
                            If str.StartsWith(par.TrimEnd("%"c), sc) Then
                                r = IParameterExpression.EvalResult.Found
                            End If
                        ElseIf par.Equals(str, sc) Then
                            r = IParameterExpression.EvalResult.Found
                        End If
                    End If
                Case Else
                    r = IParameterExpression.EvalResult.Unknown
            End Select

            Return r
        End Function

    End Module
End Namespace