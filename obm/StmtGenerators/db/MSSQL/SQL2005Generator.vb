Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Expressions2
Imports Worm.Entities.Meta
Imports Worm.Query
Imports Worm.Query.Database

Namespace Database

    Public Class MSSQL2005Generator
        Inherits SQL2000Generator

        'Public Sub New(ByVal version As String)
        '    MyBase.New(version)
        'End Sub

        'Public Sub New(ByVal version As String, ByVal resolveEntity As ResolveEntity)
        '    MyBase.New(version, resolveEntity)
        'End Sub

        'Public Sub New(ByVal version As String, ByVal resolveName As ResolveEntityName)
        '    MyBase.New(version, resolveName)
        'End Sub

        'Public Sub New(ByVal version As String, ByVal resolveEntity As ResolveEntity, ByVal resolveName As ResolveEntityName)
        '    MyBase.New(version, resolveEntity, resolveName)
        'End Sub

        Public Overrides Function TopStatement(ByVal top As Integer) As String
            Return "top (" & top & ") "
        End Function

        Public Overrides Function TopStatementPercent(ByVal top As Integer, ByVal percent As Boolean, ByVal ties As Boolean) As String
            Dim sb As New StringBuilder
            sb.Append("top (").Append(top).Append(") ")
            If percent Then
                sb.Append("percent ")
            End If
            If ties Then
                sb.Append("with ties ")
            End If
            Return sb.ToString
        End Function

        Public Overrides ReadOnly Property SupportRowNumber() As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides Function MakeRowNumber(mpe As ObjectMappingEngine, query As Query.QueryCmd) As String
            Dim selSb As New StringBuilder
            selSb.Append(",row_number() over (")
            If query.Sort IsNot Nothing Then
                selSb.Append(DbQueryExecutor.RowNumberOrder)
                'FormOrderBy(query, t, almgr, sb, s, filterInfo, params)
            Else
                selSb.Append("order by ")
                Dim ob As List(Of SelectExpression) = query._sl.FindAll(Function(se) (se.Attributes And Field2DbRelations.PK) = Field2DbRelations.PK)
                If ob.Count = 0 Then
                    ob = query._sl
                End If
                For Each se As SelectExpression In ob
                    For Each cs As String In CType(query, IExecutionContext).FindColumn(mpe, se.GetIntoPropertyAlias)
                        selSb.Append(cs)
                    Next
                    selSb.Append(",")
                Next
                If ob.Count > 0 Then
                    selSb.Length -= 1
                End If
            End If
            selSb.Append(") as ").Append(QueryCmd.RowNumerColumn)

            Return selSb.ToString
        End Function

        Public Overrides Sub FormatRowNumber(mpe As ObjectMappingEngine, query As QueryCmd, ByVal contextInfo As IDictionary, _
            ByVal params As ICreateParam, ByVal almgr As IPrepareTable, sb As StringBuilder)
            'Throw New NotImplementedException
            Dim rs As String = sb.ToString
            sb.Length = 0
            sb.Append("select *")
            'For Each col As String In columnAliases
            '    If String.IsNullOrEmpty(col) Then
            '        Throw New ExecutorException("Column alias is required")
            '    End If
            '    sb.Append(col).Append(",")
            'Next
            'sb.Length -= 1
            sb.Append(" from (").Append(rs).Append(") as t0t01 where ")
            sb.Append(query.RowNumberFilter.MakeQueryStmt(mpe, query.FromClause, Me, query, contextInfo, almgr, params))
        End Sub

        Protected Overrides Function DeclareOutput(ByVal sb As System.Text.StringBuilder, _
            ByVal pks As IEnumerable(Of Pair(Of String, Pair(Of String)))) As String
            Dim tblName As String = "@tmp_tbl"
            Dim clm As New StringBuilder
            clm.Append("table(")
            For Each p As Pair(Of String, Pair(Of String)) In pks
                Dim s As Pair(Of String) = p.Second
                clm.Append(s.First).Append(" ").Append(s.Second).Append(",")
            Next
            clm.Length -= 1
            If clm.Length > 5 Then
                clm.Append(")")
                sb.Append(DeclareVariable(tblName, clm.ToString))
                Return tblName
            Else
                Return Nothing
            End If
        End Function

        Protected Overrides Function InsertOutput(ByVal table As String, _
            ByVal syncInsertPK As IEnumerable(Of Pair(Of String, Pair(Of String))), _
            ByVal notSyncInsertPK As List(Of Pair(Of String)), ByVal co As Entities.Meta.IChangeOutputOnInsert) As String
            Dim sb As New StringBuilder
            sb.Append("output ")
            For Each pp As Pair(Of String, Pair(Of String)) In syncInsertPK
                Dim p As Pair(Of String) = pp.Second
                Dim clm As String = p.First
                If co IsNot Nothing Then
                    clm = co.GetColumn(pp.First, clm)
                End If
                sb.Append("inserted.").Append(clm).Append(",")
            Next
            sb.Length -= 1
            If sb.Length > 7 Then
                sb.Append(" into ").Append(table).Append("(")
                For Each pp As Pair(Of String, Pair(Of String)) In syncInsertPK
                    sb.Append(pp.Second.First).Append(",")

                    Dim propertyAlias As String = pp.First
                    Dim idx As Integer = notSyncInsertPK.FindIndex(Function(p As Pair(Of String)) p.First = propertyAlias)
                    If idx >= 0 Then
                        notSyncInsertPK(idx) = New Pair(Of String)(propertyAlias, pp.Second.First)
                    Else
                        notSyncInsertPK.Add(New Pair(Of String)(propertyAlias, pp.Second.First))
                    End If
                Next
                sb.Length -= 1
                sb.Append(")")
                Return sb.ToString
            Else
                Return String.Empty
            End If
        End Function

        '    Protected Overrides Function FormInsert(ByVal inserted_tables As System.Collections.Generic.Dictionary(Of String, System.Collections.Generic.IList(Of OrmFilter)), ByVal ins_cmd As System.Text.StringBuilder, ByVal type As System.Type, ByVal sel_columns As System.Collections.Generic.List(Of ColumnAttribute), ByVal unions() As String, ByVal params As ICreateParam) As System.Collections.Generic.ICollection(Of System.Data.Common.DbParameter)
        '        If params Is Nothing Then
        '            params = New ParamMgr(Me, "p")
        '        End If

        '        If inserted_tables.Count > 0 Then
        '            Dim l As New List(Of Structures.Pair(Of ColumnAttribute, String))
        '            If sel_columns IsNot Nothing AndAlso sel_columns.Count > 0 Then
        '                sel_columns.Sort()

        '                For Each c As ColumnAttribute In sel_columns
        '                    l.Add(DeclareVariable(type, c, ins_cmd))
        '                Next

        '                If inserted_tables.Count > 1 Then
        '                    ins_cmd.Append(DeclareVariable("@err", "int"))
        '                    ins_cmd.Append(EndLine)
        '                End If
        '            End If

        '            Dim b As Boolean = False
        '            Dim os As IOrmObjectSchema = GetObjectSchema(type)
        '            Dim pk_table As String = os.GetTables(0)
        '            For Each item As Generic.KeyValuePair(Of String, IList(Of OrmFilter)) In inserted_tables
        '                If b Then
        '                    ins_cmd.Append(EndLine)
        '                    If SupportIf() Then
        '                        ins_cmd.Append("if @err = 0 ")
        '                    End If
        '                Else
        '                    b = True
        '                End If

        '                If item.Value Is Nothing OrElse item.Value.Count = 0 Then
        '                    ins_cmd.Append("insert into ").Append(item.Key).Append(" ")
        '                    If pk_table = CStr(item.Key) AndAlso sel_columns IsNot Nothing Then
        '                        AddOutput(type, sel_columns, ins_cmd)
        '                    End If
        '                    ins_cmd.Append(" ").Append(DefaultValues)
        '                Else
        '                    ins_cmd.Append("insert into ").Append(item.Key).Append(" (")
        '                    Dim values_sb As New StringBuilder
        '                    values_sb.Append(") ")
        '                    If pk_table = CStr(item.Key) AndAlso sel_columns IsNot Nothing Then
        '                        AddOutput(type, sel_columns, values_sb)
        '                    End If
        '                    values_sb.Append(" values(")
        '                    For Each f As OrmFilter In item.Value
        '                        Dim p As Structures.Pair(Of String) = f.MakeSignleStmt(Me, params)
        '                        ins_cmd.Append(p.First).Append(",")
        '                        values_sb.Append(p.Second).Append(",")
        '                    Next

        '                    ins_cmd.Length -= 1
        '                    values_sb.Length -= 1
        '                    ins_cmd.Append(values_sb.ToString).Append(")")
        '                End If

        '                If pk_table = CStr(item.Key) AndAlso sel_columns IsNot Nothing Then
        '                    ins_cmd.Append(EndLine)
        '                    ins_cmd.Append("select @rcount = ").Append(RowCount)
        '                    If inserted_tables.Count > 1 Then
        '                        ins_cmd.Append(", @err = ").Append(LastError)
        '                    End If
        '                End If
        '            Next

        '            If sel_columns IsNot Nothing AndAlso sel_columns.Count > 0 Then
        '                ins_cmd.Append(EndLine)
        '                If SupportIf() Then
        '                    ins_cmd.Append("if @rcount > 0 ")
        '                End If
        '                ins_cmd.Append("select ")

        '            End If
        '        End If

        '        Return params.Params
        '    End Function

        Public Overrides ReadOnly Property Name() As String
            Get
                Return "Microsoft SQL Server 2005"
            End Get
        End Property

        Public Overrides Function FormatGroupBy(ByVal t As Expressions2.GroupExpression.SummaryValues, ByVal fields As String, ByVal custom As String) As String
            If t = Expressions2.GroupExpression.SummaryValues.Custom Then
                Return "group by " & custom & "(" & fields & ")"
            Else
                Return MyBase.FormatGroupBy(t, fields, custom)
            End If
        End Function

        Public Overrides Function FormatAggregate(ByVal t As Expressions2.AggregateExpression.AggregateFunction, ByVal fields As String, ByVal custom As String, ByVal distinct As Boolean) As String
            If t = Expressions2.AggregateExpression.AggregateFunction.Custom Then
                Return custom & "(" & fields & ")"
            Else
                Return MyBase.FormatAggregate(t, fields, custom, distinct)
            End If
        End Function

        '    Protected sub AddOutput(ByVal type As Type, ByVal sel_column As ICollection(Of ColumnAttribute), ByVal sb As StringBuilder) As String
        '        For Each c As ColumnAttribute In sel_column
        '            sb.Append("inserted.")
        '            sb.Append(GetColumnNameByFieldName(type, c.FieldName))
        '            sb.Append(",")
        '        Next
        '        sb.Length -= 1
        '    End Sub

        '    Protected Overloads Function DeclareVariable(ByVal type As Type, ByVal c As ColumnAttribute, ByVal sb As StringBuilder) As Structures.Pair(Of ColumnAttribute, String)
        '        Dim p As Data.Common.DbParameter = CreateDBParameter("@" & c.FieldName, Convert.ChangeType(Nothing, GetFieldTypeByName(type, c.FieldName)))
        '        p.DbType.
        '    End Function

    End Class

End Namespace