Namespace Expressions2
    Public Class GroupExpressions
        Implements IExpression

        Public Enum SummaryValues
            None
            Cube
            Rollup
            GroupingSets
            Custom
        End Enum

        Private _type As SummaryValues
        Private _exp As IExpression
        Private _custom As String

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Return New IExpression() {Me, _exp}
        End Function

        Public Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal inSelect As Boolean, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            Return stmt.FormatGroupBy(_type, _exp.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextFilter, inSelect, executor), _custom)
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
            Return Equals(TryCast(f, GroupExpressions))
        End Function

        Public Overloads Function Equals(ByVal f As GroupExpressions) As Boolean
            If f Is Nothing Then
                Return False
            End If

            Return _type = f._type AndAlso _custom = f._custom AndAlso _exp.Equals(f._exp)
        End Function

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            Return _type.ToString & _custom & _exp.GetDynamicString()
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            Return _type.ToString & _custom & _exp.GetStaticString(mpe, contextFilter)
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            _exp.Prepare(executor, mpe, contextFilter, stmt, isAnonym)
        End Sub
    End Class

    Public Class SortExpression
        Implements IExpression

        Public Enum SortType
            Asc
            Desc
        End Enum

        Private _order As SortType
        Private _exp As IExpression
        Private _collation As String

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Return New IExpression() {Me, _exp}
        End Function

        Public Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal inSelect As Boolean, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            Return stmt.FormatOrderBy(_order, _exp.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextFilter, inSelect, executor), _collation)
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
            Return Equals(TryCast(f, SortExpression))
        End Function

        Public Overloads Function Equals(ByVal f As SortExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If

            Return _order = f._order AndAlso _collation = f._collation AndAlso _exp.Equals(f._exp)
        End Function

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            Return _order.ToString & _collation & _exp.GetDynamicString
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            Return _order.ToString & _collation & _exp.GetStaticString(mpe, contextFilter)
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            _exp.Prepare(executor, mpe, contextFilter, stmt, isAnonym)
        End Sub
    End Class
End Namespace