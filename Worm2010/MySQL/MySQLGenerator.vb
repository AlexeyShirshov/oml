Imports Worm.Database
Imports MySql.Data.MySqlClient

Public Class MySQLGenerator
    Inherits DbGenerator

    Public Overrides Function CreateCommandBuilder(da As System.Data.Common.DbDataAdapter) As System.Data.Common.DbCommandBuilder

    End Function

    Public Overrides Function CreateConnection(connectionString As String) As System.Data.Common.DbConnection

    End Function

    Public Overrides Function CreateDataAdapter() As System.Data.Common.DbDataAdapter

    End Function

    Public Overloads Overrides Function CreateDBCommand() As System.Data.Common.DbCommand

    End Function

    Public Overloads Overrides Function CreateDBCommand(timeout As Integer) As System.Data.Common.DbCommand

    End Function

    Public Overloads Overrides Function CreateDBParameter() As System.Data.Common.DbParameter

    End Function

    Public Overloads Overrides Function CreateDBParameter(name As String, value As Object) As System.Data.Common.DbParameter

    End Function

    Public Overrides Function DeclareVariable(name As String, type As String) As String

    End Function

    Public Overrides Function FormatAggregate(t As Expressions2.AggregateExpression.AggregateFunction, fields As String, custom As String, distinct As Boolean) As String

    End Function

    Public Overrides Function FormatGroupBy(t As Expressions2.GroupExpression.SummaryValues, fields As String, custom As String) As String

    End Function

    Public Overrides Function FormatOrderBy(t As Expressions2.SortExpression.SortType, fields As String, collation As String) As String

    End Function

    Public Overrides Sub FormStmt(dbschema As ObjectMappingEngine, fromClause As Query.QueryCmd.FromClauseDef, filterInfo As Object, paramMgr As Entities.Meta.ICreateParam, almgr As IPrepareTable, sb As System.Text.StringBuilder, _t As System.Type, _tbl As Entities.Meta.SourceFragment, _joins() As Criteria.Joins.QueryJoin, _field As String, _f As Criteria.Core.IFilter)

    End Sub

    Public Overrides ReadOnly Property FTSKey As String
        Get

        End Get
    End Property

    Public Overrides ReadOnly Property GetDate As String
        Get

        End Get
    End Property

    Public Overrides Function GetTableName(t As Entities.Meta.SourceFragment) As String

    End Function

    Public Overrides ReadOnly Property Name As String
        Get

        End Get
    End Property

    Public Overrides Function ParamName(name As String, i As Integer) As String

    End Function

    Public Overrides ReadOnly Property PlanHint As String
        Get

        End Get
    End Property

    Public Overrides ReadOnly Property Selector As String
        Get

        End Get
    End Property

    Public Overrides Function TopStatement(top As Integer) As String

    End Function
End Class
