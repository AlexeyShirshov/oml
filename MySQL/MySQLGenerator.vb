Imports Worm.Database
Imports MySql.Data.MySqlClient
Imports System.Text
Imports Worm.Entities.Meta
Imports Worm.Expressions2

Public Class MySQLGenerator
    Inherits DbGenerator

    'Implements ITopStatement

    Public Overrides Function CreateCommandBuilder(da As System.Data.Common.DbDataAdapter) As System.Data.Common.DbCommandBuilder
        Return New MySqlCommandBuilder(CType(da, MySqlDataAdapter))
    End Function

    Public Overrides Function CreateConnection(connectionString As String, info As InfoMessageDelagate) As System.Data.Common.DbConnection
        Dim conn As New MySqlConnection(connectionString)
        If info IsNot Nothing Then
            AddHandler conn.InfoMessage, Sub(s, e)
                                             info(e)
                                         End Sub
        End If
        Return conn
    End Function

    Public Overrides Function CreateDataAdapter() As System.Data.Common.DbDataAdapter
        Return New MySqlDataAdapter
    End Function

    Public Overloads Overrides Function CreateDBCommand() As System.Data.Common.DbCommand
        Return New MySqlCommand
    End Function

    Public Overloads Overrides Function CreateDBCommand(timeout As Integer) As System.Data.Common.DbCommand
        Return New MySqlCommand() With {.CommandTimeout = timeout}
    End Function

    Public Overloads Overrides Function CreateDBParameter() As System.Data.Common.DbParameter
        Return New MySqlParameter()
    End Function

    Public Overloads Overrides Function CreateDBParameter(name As String, value As Object) As System.Data.Common.DbParameter
        Return New MySqlParameter(name, value)
    End Function

    Public Overrides Function DeclareVariable(name As String, type As String) As String
        If String.IsNullOrEmpty(name) Then
            Throw New ArgumentNullException("name")
        End If

        Return "declare " & name & " " & type
    End Function

    Public Overrides Function FormatAggregate(t As Expressions2.AggregateExpression.AggregateFunction, fields As String, custom As String, distinct As Boolean) As String
        If distinct Then
            fields = " distinct " & fields
        End If
        Select Case t
            Case Expressions2.AggregateExpression.AggregateFunction.Max
                Return "max(" & fields & ")"
            Case Expressions2.AggregateExpression.AggregateFunction.Min
                Return "min(" & fields & ")"
            Case Expressions2.AggregateExpression.AggregateFunction.Average
                Return "avg(" & fields & ")"
            Case Expressions2.AggregateExpression.AggregateFunction.Count
                Return "count(" & fields & ")"
            Case Expressions2.AggregateExpression.AggregateFunction.BigCount
                Return "count(" & fields & ")"
            Case Expressions2.AggregateExpression.AggregateFunction.Sum
                Return "sum(" & fields & ")"
            Case Expressions2.AggregateExpression.AggregateFunction.StandardDeviation
                Return "stdev(" & fields & ")"
            Case Expressions2.AggregateExpression.AggregateFunction.StandardDeviationOfPopulation
                Return "stdevp(" & fields & ")"
            Case Expressions2.AggregateExpression.AggregateFunction.Variance
                Return "var(" & fields & ")"
            Case Expressions2.AggregateExpression.AggregateFunction.VarianceOfPopulation
                Return "varp(" & fields & ")"
            Case Else
                Throw New NotImplementedException(t.ToString)
        End Select
    End Function

    Public Overrides Function FormatGroupBy(t As Expressions2.GroupExpression.SummaryValues, fields As String, custom As String) As String
        Select Case t
            Case Expressions2.GroupExpression.SummaryValues.None
                Return "group by " & fields
            Case Expressions2.GroupExpression.SummaryValues.Cube
                Throw New NotSupportedException
            Case Expressions2.GroupExpression.SummaryValues.Rollup
                Return "group by " & fields & " with rollup"
            Case Else
                Throw New NotSupportedException(t.ToString)
        End Select
    End Function

    Public Overrides Function FormatOrderBy(t As Expressions2.SortExpression.SortType, fields As String, collation As String) As String
        Dim sb As New StringBuilder

        sb.Append(fields)
        If Not String.IsNullOrEmpty(collation) Then
            sb.Append(" collate ").Append(collation)
        End If

        If t = Expressions2.SortExpression.SortType.Desc Then
            sb.Append(" desc")
        End If

        Return sb.ToString
    End Function

    Public Overrides Sub FormStmt(dbschema As ObjectMappingEngine, fromClause As Query.QueryCmd.FromClauseDef, contextInfo As IDictionary,
                                  paramMgr As Entities.Meta.ICreateParam, almgr As IPrepareTable, sb As System.Text.StringBuilder,
                                  type As System.Type, sourceFragment As Entities.Meta.SourceFragment, joins() As Criteria.Joins.QueryJoin,
                                  propertyAlias As String, filter As Criteria.Core.IFilter)
        If type Is Nothing Then
            sb.Append(SelectWithJoin(dbschema, Nothing, New SourceFragment() {sourceFragment}, _
                almgr, paramMgr, joins, _
                False, Nothing, Nothing, Nothing, Nothing, contextInfo))
        Else
            Dim arr As Generic.IList(Of EntityExpression) = Nothing
            If Not String.IsNullOrEmpty(propertyAlias) Then
                arr = New Generic.List(Of EntityExpression)
                arr.Add(New EntityExpression(propertyAlias, type))
            End If
            sb.Append(SelectWithJoin(dbschema, type, almgr, paramMgr, joins, _
                arr IsNot Nothing, Nothing, Nothing, contextInfo, arr))
        End If

        AppendWhere(dbschema, type, filter, almgr, sb, contextInfo, paramMgr)
    End Sub

    Public Overrides ReadOnly Property FTSKey As String
        Get
            Return Nothing
        End Get
    End Property

    Public Overrides ReadOnly Property GetDate As String
        Get
            Return "now()"
        End Get
    End Property

    Public Overrides Function GetTableName(t As Entities.Meta.SourceFragment, ByVal contextInfo As IDictionary) As String
        Return t.Name
    End Function

    Public Overrides ReadOnly Property Name As String
        Get
            Return "MySQL"
        End Get
    End Property

    Public Overrides Function ParamName(name As String, i As Integer) As String
        Return "@" & name & i
    End Function

    Public Overrides ReadOnly Property PlanHint As String
        Get
            Return Nothing
        End Get
    End Property

    Public Overrides ReadOnly Property Selector As String
        Get
            Return "."
        End Get
    End Property

    Public Overrides Function TopStatement(top As Integer) As String
        Throw New NotImplementedException
    End Function

    Public Overrides ReadOnly Property SupportTopParam As Boolean
        Get
            Return False
        End Get
    End Property

    Public Overrides ReadOnly Property SupportRowNumber As Boolean
        Get
            Return True
        End Get
    End Property
    Public Overrides Function SupportMultiline() As Boolean
        Return False
    End Function
    Public Overrides Sub FormatRowNumber(mpe As ObjectMappingEngine, q As Query.QueryCmd, contextInfo As IDictionary, params As ICreateParam,
                                         almgr As IPrepareTable, sb As StringBuilder)
        If q.TopParam IsNot Nothing Then
            Dim stmt = " limit {0} offset {1}"
            Dim off = "0"

            'q.RowNumberFilter
            sb.AppendFormat(stmt, q.TopParam.Count, off)
        End If
    End Sub

    Public Overrides Function GetLockCommand(pmgr As ICreateParam, name As String,
                                             Optional lockTimeout As Integer? = Nothing,
                                             Optional lockType As LockTypeEnum = Database.LockTypeEnum.Exclusive) As Common.DbCommand
        Dim cmd = CreateDBCommand()
        cmd.CommandType = CommandType.Text

        Dim nameParam = pmgr.AddParam("name", name)

        Dim lt = -1
        If lockTimeout.HasValue Then
            lt = lockTimeout.Value
        End If
        Dim timeoutParam = pmgr.AddParam("timeout", lt)

        cmd.CommandText = String.Format("select getlock({0},{1})", nameParam, timeoutParam)

        'cmd.Parameters.Add(CreateDBParameter(nameParam, name))
        'cmd.Parameters.Add(CreateDBParameter(timeoutParam, lt))

        Return cmd
    End Function

    Public Overrides Function ReleaseLockCommand(pmgr As ICreateParam, name As String) As Common.DbCommand
        Dim cmd = CreateDBCommand()
        cmd.CommandType = CommandType.Text
        Dim nameParam = pmgr.AddParam("name", name)
        cmd.CommandText = String.Format("select release_lock({0})", nameParam)

        'cmd.Parameters.Add(CreateDBParameter(nameParam, name))

        Return cmd
    End Function

    Public Overrides Function TestLockError(v As Object) As Boolean
        If v Is Nothing OrElse v Is DBNull.Value Then
            Return True
        End If

        Dim r As Integer
        If Not Integer.TryParse(v.ToString, r) OrElse r < 0 Then
            Return True
        End If

        Return False
    End Function

    'Public Overrides ReadOnly Property NamedParams As Boolean
    '    Get
    '        Return False
    '    End Get
    'End Property
End Class
