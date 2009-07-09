Imports System.Collections.Generic

Namespace Expressions2
    <Serializable()> _
    Public Class AggregateExpression
        Implements IExpression

        Public Enum AggregateFunction
            Max
            Min
            Average
            Count
            BigCount
            Sum
            StandardDeviation
            StandardDeviationOfPopulation
            Variance
            VarianceOfPopulation
            Custom
        End Enum

        Private _t As AggregateFunction
        Private _custom As String
        Private _exp As IExpression
        Private _distinct As Boolean

        Public Sub New(ByVal type As AggregateFunction)
            _t = type
        End Sub

        Public Sub New(ByVal type As AggregateFunction, ByVal exp As IExpression)
            _t = type
            _exp = exp
        End Sub

        Public Sub New(ByVal type As AggregateFunction, ByVal exp As IGetExpression)
            _t = type
            _exp = exp.Expression
        End Sub

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Dim l As New List(Of IExpression)
            l.Add(Me)
            If _exp IsNot Nothing Then
                l.AddRange(_exp.GetExpressions)
            End If
            Return l.ToArray
        End Function

        Public Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            Dim s As String = String.Empty
            If _exp IsNot Nothing Then
                s = _exp.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextFilter, stmtMode, executor)
            ElseIf _t = AggregateFunction.BigCount Or _t = AggregateFunction.Count Then
                s = "*"
            End If
            Return stmt.FormatAggregate(_t, s, _custom, _distinct)
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
            Return Equals(TryCast(f, AggregateExpression))
        End Function

        Public Overloads Function Equals(ByVal f As AggregateExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If

            Return _t = f._t AndAlso _custom = f._custom AndAlso Object.Equals(_exp, f._exp) AndAlso _distinct = f._distinct
        End Function

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            Dim s As String = _t.ToString & "$" & _distinct & _custom & "$"
            If _exp IsNot Nothing Then
                s &= _exp.GetDynamicString
            End If
            Return s
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            Dim s As String = _t.ToString & "$" & _distinct & _custom & "$"
            If _exp IsNot Nothing Then
                s &= _exp.GetStaticString(mpe, contextFilter)
            End If
            Return s
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            If _exp IsNot Nothing Then
                _exp.Prepare(executor, mpe, contextFilter, stmt, isAnonym)
            End If
        End Sub

        Public Property Distinct() As Boolean
            Get
                Return _distinct
            End Get
            Set(ByVal value As Boolean)
                _distinct = value
            End Set
        End Property
    End Class
End Namespace