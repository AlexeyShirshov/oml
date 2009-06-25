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

        Public Sub New(ByVal type As AggregateFunction, ByVal exp As IExpression)
            _t = type
            _exp = exp
        End Sub

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Return New IExpression() {Me, _exp}
        End Function

        Public Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal inSelect As Boolean, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            Return stmt.FormatAggregate(_t, _exp.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextFilter, inSelect, executor), _custom)
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

            Return _t = f._t AndAlso _custom = f._custom AndAlso _exp.Equals(f._exp)
        End Function

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            Return _t.ToString & "$" & _custom & "$" & _exp.GetDynamicString
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            Return _t.ToString & "$" & _custom & "$" & _exp.GetStaticString(mpe, contextFilter)
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            _exp.Prepare(executor, mpe, contextFilter, stmt, isAnonym)
        End Sub
    End Class
End Namespace