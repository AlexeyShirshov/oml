Imports System.Data.SqlServerCe
Imports Worm.Entities.Meta
Imports Worm.Expressions2
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Core
Imports System.Collections.Generic
Imports Worm.Criteria.Joins

Namespace Database
    Public Class SQLCE40Generator
        Inherits DbGenerator

        Public Overrides ReadOnly Property Name As String
            Get
                Return "Microsoft SQL Server CE 4.0"
            End Get
        End Property
#Region " data factory "
        Public Overrides Function CreateDBCommand(ByVal timeout As Integer) As System.Data.Common.DbCommand
            Dim cmd As New SqlCeCommand
            cmd.CommandTimeout = timeout
            Return cmd
        End Function

        Public Overrides Function CreateDBCommand() As System.Data.Common.DbCommand
            Dim cmd As New SqlCeCommand
            'cmd.CommandTimeout = XMedia.Framework.Configuration.ApplicationConfiguration.CommandTimeout \ 1000
            Return cmd
        End Function

        Public Overrides Function CreateDataAdapter() As System.Data.Common.DbDataAdapter
            Return New SqlCeDataAdapter
        End Function

        Public Overrides Function CreateDBParameter() As System.Data.Common.DbParameter
            Return New SqlCeParameter
        End Function

        Public Overrides Function CreateDBParameter(ByVal name As String, ByVal value As Object) As System.Data.Common.DbParameter
            Return New SqlCeParameter(name, value)
        End Function

        Public Overrides Function CreateCommandBuilder(ByVal da As System.Data.Common.DbDataAdapter) As System.Data.Common.DbCommandBuilder
            Return New SqlCeCommandBuilder(CType(da, SqlCeDataAdapter))
        End Function

        Public Overrides Function CreateConnection(ByVal connectionString As String, info As InfoMessageDelagate) As System.Data.Common.DbConnection
            Dim conn As New SqlCeConnection(connectionString)
            If info IsNot Nothing Then
                AddHandler conn.InfoMessage, Sub(s, e)
                                                 info(e)
                                             End Sub
            End If
            Return conn
        End Function

#End Region

        Public Overrides Function TopStatement(ByVal top As Integer) As String
            Return "top (" & top & ") "
        End Function

        Public Overrides ReadOnly Property SupportTopParam As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property SupportIf As Boolean
            Get
                Return False
            End Get
        End Property

        'Public Overrides ReadOnly Property SupportRowNumber As Boolean
        '    Get
        '        Return True
        '    End Get
        'End Property

        'Public Overrides Sub FormatRowNumber(mpe As ObjectMappingEngine, q As Query.QueryCmd, contextInfo As IDictionary,
        '                                     params As Entities.Meta.ICreateParam, almgr As IPrepareTable, sb As StringBuilder)
        '    If q.TopParam IsNot Nothing Then
        '        Dim stmt = " OFFSET {0} ROWS"

        '        'q.RowNumberFilter
        '        sb.AppendFormat(stmt, q.TopParam.Count)
        '    End If

        '    If q.Take Then

        '    End If
        'End Sub

        Protected Overrides Function FormInsert(mpe As ObjectMappingEngine, inserted_tables As Generic.List(Of InsertedTable),
                                                ins_cmd As StringBuilder, type As Type, os As Entities.Meta.IEntitySchema,
                                                selectedProperties As Generic.List(Of Expressions2.EntityExpression),
                                                params As Entities.Meta.ICreateParam, contextInfo As IDictionary) As Generic.ICollection(Of Data.Common.DbParameter)
            Throw New NotImplementedException
        End Function

        Public Overrides Function DeclareVariable(name As String, type As String) As String
            Throw New NotSupportedException
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
                    Return "count_big(" & fields & ")"
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
                    Throw New NotSupportedException("group by with cube is not supported")
                Case Expressions2.GroupExpression.SummaryValues.Rollup
                    Throw New NotSupportedException("group by with rollup is not supported")
                Case Else
                    Throw New NotSupportedException(t.ToString)
            End Select
        End Function

        Public Overrides Function FormatOrderBy(t As Expressions2.SortExpression.SortType, fields As String, collation As String) As String
            Dim sb As New StringBuilder

            sb.Append(fields)

            If t = Expressions2.SortExpression.SortType.Desc Then
                sb.Append(" desc")
            End If

            Return sb.ToString
        End Function

        Public Overrides Sub FormStmt(dbschema As ObjectMappingEngine, fromClause As Query.QueryCmd.FromClauseDef,
                                      contextInfo As IDictionary, paramMgr As Entities.Meta.ICreateParam, almgr As IPrepareTable,
                                      sb As StringBuilder, type As Type, sourceFragment As Entities.Meta.SourceFragment,
                                      joins() As Criteria.Joins.QueryJoin,
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
                Throw New NotSupportedException
            End Get
        End Property

        Public Overrides ReadOnly Property GetDate As String
            Get
                Return "getdate()"
            End Get
        End Property


        Public Overrides Function GetTableName(t As Entities.Meta.SourceFragment, contextInfo As IDictionary) As String
            Return t.Name
        End Function

        Public Overrides Function ParamName(name As String, i As Integer) As String
            Return "@" & name & i
        End Function

        Public Overrides ReadOnly Property PlanHint As String
            Get
                Throw New NotSupportedException
            End Get
        End Property
        Public Overrides ReadOnly Property Selector As String
            Get
                Return "."
            End Get
        End Property

        Public Overrides Function ReleaseLockCommand(pmgr As Entities.Meta.ICreateParam, name As String) As Data.Common.DbCommand
            Throw New NotSupportedException
        End Function

        Public Overrides Function GetLockCommand(pmgr As Entities.Meta.ICreateParam, name As String, Optional lockTimeout As Integer? = Nothing, Optional lockType As LockTypeEnum = LockTypeEnum.Exclusive) As Data.Common.DbCommand
            Throw New NotSupportedException
        End Function

        Public Overrides Function TestLockError(v As Object) As Boolean
            Throw New NotSupportedException
        End Function

        Public Overrides Function Update(mpe As ObjectMappingEngine, obj As Entities.ICachedEntity, contextInfo As IDictionary, originalCopy As Entities.ICachedEntity, ByRef dbparams As Generic.IEnumerable(Of Data.Common.DbParameter), ByRef selectedProperties As Generic.List(Of SelectExpression), ByRef updated_fields As Generic.IList(Of Criteria.Core.EntityFilter)) As String

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj parameter cannot be nothing")
            End If

            selectedProperties = Nothing
            updated_fields = Nothing

            Dim le As ILastError = TryCast(Me, ILastError)

            Using obj.AcquareLock()
                Dim upd_cmd As New StringBuilder
                dbparams = Nothing
                If originalCopy IsNot Nothing Then
                    'If obj.ObjectState = ObjectState.Modified Then
                    'If obj.OriginalCopy Is Nothing Then
                    '    Throw New ObjectStateException(obj.ObjName & "Object in state modified have to has an original copy")
                    'End If

                    Dim syncUpdateProps As New List(Of EntityExpression)
                    Dim updated_tables As New Dictionary(Of SourceFragment, TableUpdate)
                    Dim rt As Type = obj.GetType

                    'Dim sb_updates As New StringBuilder
                    Dim oschema As IEntitySchema = obj.GetEntitySchema(mpe)
                    Dim esch As IEntitySchema = oschema
                    Dim ro As IReadonlyObjectSchema = TryCast(oschema, IReadonlyObjectSchema)
                    If ro IsNot Nothing AndAlso (ro.SupportedOperation And IReadonlyObjectSchema.Operation.Update) = IReadonlyObjectSchema.Operation.Update Then
                        esch = ro.GetEditableSchema
                        Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap
                        For Each ep As MapField2Column In esch.FieldColumnMap
                            Dim m As MapField2Column = Nothing
                            If map.TryGetValue(ep.PropertyAlias, m) Then
                                ep.PropertyInfo = m.PropertyInfo
                                ep.Schema = m.Schema
                                If ep.Attributes = Field2DbRelations.None Then
                                    ep.Attributes = m.Attributes
                                End If
                            End If
                        Next
                    End If

                    Dim exec As Query.IExecutionContext = GetChangedFields(mpe, obj, esch, updated_tables, syncUpdateProps, originalCopy)

                    Dim l As New List(Of EntityFilter)
                    For Each tu As TableUpdate In updated_tables.Values
                        l.AddRange(tu._updates)
                    Next
                    updated_fields = l

                    GetUpdateConditions(mpe, obj, esch, updated_tables, contextInfo)

                    selectedProperties = syncUpdateProps.ConvertAll(Function(e) New SelectExpression(e))

                    If updated_tables.Count > 0 Then
                        'Dim sch As IOrmObjectSchema = GetObjectSchema(rt)
                        Dim pk_table As SourceFragment = Nothing
                        For Each m As MapField2Column In esch.FieldColumnMap
                            If m.IsPK Then
                                pk_table = m.Table 'mpe.GetPropertyTable(esch, c.PropertyAlias)
                                Exit For
                            End If
                        Next

                        'Dim amgr As AliasMgr = AliasMgr.Create
                        Dim params As New ParamMgr(Me, "p")
                        Dim hasSyncUpdate As Boolean = False
                        Dim lastTbl As SourceFragment = Nothing, lastUT As TableUpdate = Nothing
                        For Each item As Generic.KeyValuePair(Of SourceFragment, TableUpdate) In updated_tables
                            Dim tbl As SourceFragment = item.Key

                            lastTbl = tbl
                            lastUT = item.Value

                            upd_cmd.Append("update ").Append(GetTableName(tbl, contextInfo)).Append(" set ")
                            For Each f As EntityFilter In item.Value._updates
                                upd_cmd.Append(f.MakeQueryStmt(esch, Nothing, Me, exec, contextInfo, mpe, Nothing, params, rt)).Append(",")
                            Next
                            upd_cmd.Length -= 1
                            upd_cmd.Append(" where ")
                            Dim fl As IFilter = CType(item.Value._where4update.Condition, IFilter)
                            Dim ef As EntityFilter = TryCast(fl, EntityFilter)
                            If ef IsNot Nothing Then
                                upd_cmd.Append(ef.MakeQueryStmt(esch, Nothing, Me, exec, contextInfo, mpe, Nothing, params, rt))
                            Else
                                upd_cmd.Append(fl.MakeQueryStmt(mpe, Nothing, Me, exec, contextInfo, Nothing, params))
                            End If
                        Next

                        dbparams = params.Params
                    End If

                End If
                Return upd_cmd.ToString
            End Using
        End Function

        Public Overrides ReadOnly Property LastInsertID As String
            Get
                Return "@@identity"
            End Get
        End Property
    End Class
End Namespace