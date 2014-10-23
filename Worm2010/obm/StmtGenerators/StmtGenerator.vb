Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Criteria.Joins
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Core
Imports Worm.Query
Imports System.Collections.Generic
Imports Worm.Expressions2

Public MustInherit Class StmtGenerator
    'Protected map As IDictionary = Hashtable.Synchronized(New Hashtable)

    Public Delegate Function GetCustomTableNameDelegate(source As StmtGenerator, ByVal t As SourceFragment, ByVal contextInfo As IDictionary) As String
    Public MustOverride Function ParamName(ByVal name As String, ByVal i As Integer) As String
    Public MustOverride Function TopStatement(ByVal top As Integer) As String
    Public MustOverride ReadOnly Property Left() As String
    Public MustOverride ReadOnly Property GetYear() As String
    Public MustOverride ReadOnly Property GetDate() As String
    Public MustOverride ReadOnly Property Selector() As String
    Public MustOverride ReadOnly Property FTSKey() As String
    Public MustOverride Function Comment(ByVal s As String) As String
    Public MustOverride ReadOnly Property PlanHint() As String

#If Not ExcludeFindMethods Then
    Public MustOverride Function CreateTopAspect(ByVal top As Integer) As Worm.Entities.Query.TopAspect
    Public MustOverride Function CreateTopAspect(ByVal top As Integer, ByVal sort As SortExpression) As Worm.Entities.Query.TopAspect
#End If

    Public MustOverride Function GetTableName(ByVal t As SourceFragment, ByVal contextInfo As IDictionary) As String

    Public MustOverride Function CreateExecutor() As Worm.Query.IExecutor

    'Public MustOverride Function CreateSelectExpressionFormater() As ISelectExpressionFormater

    Public MustOverride Function BinaryOperator2String(ByVal oper As BinaryOperationType) As String
    Public MustOverride Function UnaryOperator2String(ByVal oper As UnaryOperationType) As String

    Public MustOverride Function Oper2String(ByVal oper As Worm.Criteria.FilterOperation) As String

    Public MustOverride Sub FormStmt(ByVal dbschema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, _
                                   ByVal contextInfo As IDictionary, ByVal paramMgr As ICreateParam, ByVal almgr As IPrepareTable, _
                                   ByVal sb As StringBuilder, ByVal _t As Type, ByVal _tbl As SourceFragment, _
                                   ByVal _joins() As QueryJoin, ByVal _field As String, ByVal _f As IFilter)

    Public MustOverride Function MakeQueryStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef,
                                                    ByVal contextInfo As IDictionary, _
            ByVal query As QueryCmd, ByVal params As ICreateParam, _
            ByVal almgr As IPrepareTable) As String

    Public MustOverride Function FormatGroupBy(ByVal t As GroupExpression.SummaryValues, ByVal fields As String, ByVal custom As String) As String
    Public MustOverride Function FormatOrderBy(ByVal t As SortExpression.SortType, ByVal fields As String, ByVal collation As String) As String
    Public MustOverride Function FormatAggregate(ByVal t As AggregateExpression.AggregateFunction, ByVal fields As String, ByVal custom As String, ByVal distinct As Boolean) As String

    Public Overridable Function MakeRowNumber(mpe As ObjectMappingEngine, q As QueryCmd) As String
        Return String.Empty
    End Function

    Public Overridable Sub FormatRowNumber(mpe As ObjectMappingEngine, q As QueryCmd, ByVal contextInfo As IDictionary, _
            ByVal params As ICreateParam, ByVal almgr As IPrepareTable, sb As StringBuilder)

    End Sub

    Public MustOverride ReadOnly Property SupportParams() As Boolean

    Public Overridable ReadOnly Property IncludeCallStack() As Boolean
        Get
            Return False
        End Get
    End Property

    Public Overridable ReadOnly Property SupportRowNumber() As Boolean
        Get
            Return False
        End Get
    End Property

    Public Overridable ReadOnly Property SupportTopParam() As Boolean
        Get
            Return True
        End Get
    End Property

End Class
