Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query
Imports Worm.Cache

Namespace Expressions2

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

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Return New IExpression() {Me}
        End Function

        Public Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
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

        'Public Function Test(ByVal oper As BinaryOperationType, ByVal v As Object, ByVal mpe As ObjectMappingEngine) As IParameterExpression.EvalResult Implements IParameterExpression.Test
        '    If oper = BinaryOperationType.Is Then
        '        If v Is Nothing Then
        '            Return IParameterExpression.EvalResult.Found
        '        Else
        '            Return IParameterExpression.EvalResult.NotFound
        '        End If
        '    ElseIf oper = BinaryOperationType.IsNot Then
        '        If v IsNot Nothing Then
        '            Return IParameterExpression.EvalResult.Found
        '        Else
        '            Return IParameterExpression.EvalResult.NotFound
        '        End If
        '    Else
        '        Throw New NotSupportedException(String.Format("Operation {0} is not supported for IsNull statement", oper))
        '    End If
        'End Function

        Public ReadOnly Property Value() As Object Implements IParameterExpression.Value
            Get
                Return Nothing
            End Get
        End Property

        Public Event ModifyValue(ByVal sender As IParameterExpression, ByVal args As IParameterExpression.ModifyValueArgs) Implements IParameterExpression.ModifyValue
    End Class

    <Serializable()> _
    Public Class QueryExpression
        Implements IExpression ', IDependentTypes

        Private _q As Query.QueryCmd

        Public Sub New(ByVal q As Query.QueryCmd)
            _q = q
        End Sub

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Return New IExpression() {Me}
        End Function

        Public Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
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

        'Public Function GetAddDelete() As System.Collections.Generic.IEnumerable(Of System.Type) Implements Cache.IDependentTypes.GetAddDelete
        '    Return CType(_q, IDependentTypes).GetAddDelete
        'End Function

        'Public Function GetUpdate() As System.Collections.Generic.IEnumerable(Of System.Type) Implements Cache.IDependentTypes.GetUpdate
        '    Return CType(_q, IDependentTypes).GetUpdate
        'End Function
    End Class

    Public Class ExpressionsArray
        Implements IExpression

        Private _v() As IExpression

        Public Sub New()

        End Sub

        Public Sub New(ParamArray exps() As IExpression)
            _v = exps
        End Sub

        Public Overridable Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Return _v
        End Function

        Public Overridable Function MakeStatement(mpe As ObjectMappingEngine, fromClause As Query.QueryCmd.FromClauseDef, stmt As StmtGenerator, paramMgr As Entities.Meta.ICreateParam, almgr As IPrepareTable, contextFilter As Object, stmtMode As MakeStatementMode, executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            If _v IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In _v
                    l.Add(v.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextFilter, stmtMode And Not MakeStatementMode.AddColumnAlias, executor))
                Next
                Return String.Join(",", l.ToArray)
            Else
                Return String.Empty
            End If
        End Function

        Public Overridable ReadOnly Property ShouldUse As Boolean Implements IExpression.ShouldUse
            Get
                Return _v IsNot Nothing
            End Get
        End Property

        Public ReadOnly Property Expression As IExpression Implements IGetExpression.Expression
            Get
                Return Me
            End Get
        End Property

        Public Overridable Overloads Function Equals(f As IQueryElement) As Boolean Implements IQueryElement.Equals
            Return Equals(CType(f, ExpressionsArray))
        End Function

        Public Overloads Function Equals(ByVal f As ExpressionsArray) As Boolean
            If _v Is Nothing Then
                Return False
            End If

            Return GetDynamicString() = f.GetDynamicString
        End Function

        Public Overridable Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            If _v IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In _v
                    l.Add(v.GetDynamicString())
                Next
                Return String.Join(",", l.ToArray)
            Else
                Return String.Empty
            End If
        End Function

        Public Overridable Function GetStaticString(mpe As ObjectMappingEngine, contextFilter As Object) As String Implements IQueryElement.GetStaticString
            If _v IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In _v
                    l.Add(v.GetStaticString(mpe, contextFilter))
                Next
                Return String.Join(",", l.ToArray)
            Else
                Return String.Empty
            End If
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            If _v IsNot Nothing Then
                For Each e As IExpression In _v
                    e.Prepare(executor, mpe, contextFilter, stmt, isAnonym)
                Next
            End If
        End Sub
    End Class

    <Serializable()> _
    Public Class CustomExpression
        Inherits ExpressionsArray

        Public Sub New(ByVal format As String)
            MyBase.New()
            _f = format
        End Sub

        Public Sub New(ByVal format As String, ByVal ParamArray v() As IExpression)
            MyBase.New(v)
            _f = format
        End Sub

        Public Sub New(ByVal format As String, ByVal ParamArray v() As IGetExpression)
            MyBase.New(Array.ConvertAll(v, Function(p) p.Expression))
            _f = format
        End Sub

        Private _f As String
        Public ReadOnly Property Format() As String
            Get
                Return _f
            End Get
        End Property

        Public ReadOnly Property Values() As IExpression()
            Get
                Return MyBase.GetExpressions
            End Get
        End Property

        Public Overrides Function GetExpressions() As IExpression()
            Dim l As New List(Of IExpression)
            l.Add(Me)
            If Values IsNot Nothing Then
                For Each e As IExpression In Values
                    l.AddRange(e.GetExpressions)
                Next
            End If
            Return l.ToArray
        End Function

        Public Overrides Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, _
            ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String
            If Values IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In Values
                    l.Add(v.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextFilter, stmtMode And Not MakeStatementMode.AddColumnAlias, executor))
                Next
                Return String.Format(_f, l.ToArray)
            Else
                Return _f
            End If
        End Function

        Public Overrides ReadOnly Property ShouldUse() As Boolean
            Get
                Return Not String.IsNullOrEmpty(_f)
            End Get
        End Property

        Public Overloads Overrides Function Equals(ByVal f As IQueryElement) As Boolean
            Return Equals(TryCast(f, CustomExpression))
        End Function

        Public Overloads Function Equals(ByVal f As CustomExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If

            Return _f = f._f AndAlso GetDynamicString() = f.GetDynamicString
        End Function

        Public Overrides Function GetDynamicString() As String
            If Values IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In Values
                    l.Add(v.GetDynamicString())
                Next
                Return String.Format(_f, l.ToArray)
            Else
                Return _f
            End If
        End Function

        Public Overrides Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String
            If Values IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In Values
                    l.Add(v.GetStaticString(mpe, contextFilter))
                Next
                Return String.Format(_f, l.ToArray)
            Else
                Return _f
            End If
        End Function
    End Class

End Namespace