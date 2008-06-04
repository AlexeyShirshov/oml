Imports Worm.Database
Imports System.Collections.Generic
Imports Worm.Orm
Imports Worm.Orm.Meta
Imports Worm.Database.Criteria.Joins
Imports Worm.Criteria.Core

Namespace Query.Database

    Partial Public Class DbQueryExecutor
        Implements IExecutor

        Private Const RowNumberOrder As String = "qiervfnkasdjvn"

        Private _proc As Object
        Private _m As Integer
        Private _sm As Integer

        Protected Function GetProcessor(Of ReturnType As {New, Orm.OrmBase})(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As Processor(Of ReturnType)
            If _proc Is Nothing Then
                Dim j As New List(Of Worm.Criteria.Joins.OrmJoin)
                'If query.Joins IsNot Nothing Then
                '    j.AddRange(query.Joins)
                'End If

                Dim f As IFilter = query.Prepare(j, mgr.ObjectSchema, mgr.GetFilterInfo, GetType(ReturnType))
                'If query.Filter IsNot Nothing Then
                '    f = query.Filter.Filter(GetType(ReturnType))
                'End If

                'If query.AutoJoins Then
                '    Dim joins() As Worm.Criteria.Joins.OrmJoin = Nothing
                '    If mgr.HasJoins(GetType(ReturnType), f, query.Sort, joins) Then
                '        j.AddRange(joins)
                '    End If
                'End If

                'If query.Obj IsNot Nothing Then
                '    _proc = New M2MProcessor(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
                'Else
                _proc = New Processor(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
                'End If

                _m = query.Mark
                _sm = query.SMark
            Else
                Dim p As Processor(Of ReturnType) = CType(_proc, Processor(Of ReturnType))
                If _m <> query.Mark Then
                    Dim j As New List(Of Worm.Criteria.Joins.OrmJoin)
                    Dim f As IFilter = query.Prepare(j, mgr.ObjectSchema, mgr.GetFilterInfo, GetType(ReturnType))
                    p.Reset(j, f)
                ElseIf _sm <> query.SMark Then
                    p.ResetStmt()
                End If
                p.Created = False
            End If

            Return CType(_proc, Processor(Of ReturnType))
        End Function

        Public Function Exec(Of SelectType As {New, Orm.OrmBase}, ReturnType)(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As System.Collections.Generic.IList(Of ReturnType) Implements IExecutor.Exec
            Dim p As Processor(Of SelectType) = GetProcessor(Of SelectType)(mgr, query)

            Return p.GetSimpleValues(Of ReturnType)()
        End Function

        Public Function Exec(Of ReturnType As {New, Orm.OrmBase})(ByVal mgr As OrmManagerBase, _
            ByVal query As QueryCmdBase) As ReadOnlyList(Of ReturnType) Implements IExecutor.Exec

            Dim dontcache As Boolean = query.DontCache

            Dim key As String = Nothing
            Dim dic As IDictionary = Nothing
            Dim id As String = Nothing
            Dim sync As String = Nothing

            Dim p As Processor(Of ReturnType) = GetProcessor(Of ReturnType)(mgr, query)

            If Not dontcache Then
                key = p.Key
                id = p.Id
                dic = p.Dic
                sync = p.Sync
            End If

            Dim oldCache As Boolean = mgr._dont_cache_lists
            Dim oldStart As Integer = mgr._start
            Dim oldLength As Integer = mgr._length

            mgr._dont_cache_lists = dontcache
            If query.ClientPaging IsNot Nothing Then
                mgr._start = query.ClientPaging.First
                mgr._length = query.ClientPaging.Second
            End If

            Dim ce As OrmManagerBase.CachedItem = mgr.GetFromCache(Of ReturnType)(dic, sync, id, query.WithLoad, p)

            mgr._dont_cache_lists = oldCache
            mgr._start = oldStart
            mgr._length = oldLength

            mgr.RaiseOnDataAvailable()

            Dim s As Cache.IListObjectConverter.ExtractListResult
            Dim r As ReadOnlyList(Of ReturnType) = ce.GetObjectList(Of ReturnType)(mgr, query.WithLoad, p.Created, s)
            Return r
        End Function

#Region " Shared helpers "

        Protected Shared Function GetFields(ByVal gen As QueryGenerator, ByVal type As Type, ByVal c As IList(Of OrmProperty)) As List(Of ColumnAttribute)
            If c IsNot Nothing Then
                Dim l As New List(Of ColumnAttribute)
                For Each p As OrmProperty In c
                    'If Not type.Equals(p.Type) Then
                    '    Throw New NotImplementedException
                    'End If
                    If p.Type Is Nothing Then
                        Dim cl As New ColumnAttribute(p.Field)
                        cl.Column = p.Column
                        l.Add(cl)
                    Else
                        l.Add(New ColumnAttribute(p.Field))
                    End If
                Next
                'l.Sort()
                Return l
            Else
                Return gen.GetSortedFieldList(type)
            End If
        End Function

        Protected Shared Sub FormSelectList(ByVal query As QueryCmdBase, ByVal t As Type, _
            ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal os As IOrmObjectSchema, _
            ByVal almgr As AliasMgr, ByVal filterInfo As Object, ByVal params As ICreateParam)

            Dim b As Boolean
            Dim cols As New StringBuilder
            If os Is Nothing Then
                If query.SelectList IsNot Nothing Then
                    For Each p As OrmProperty In query.SelectList
                        cols.Append(s.GetTableName(p.Table)).Append(".").Append(p.Column).Append(", ")
                    Next
                    cols.Length -= 2
                    sb.Append(cols.ToString)
                    b = True
                End If
            Else
                If query.WithLoad Then
                    If query.SelectList Is Nothing AndAlso query.Aggregates Is Nothing Then
                        cols.Append(s.GetSelectColumnList(t))
                        sb.Append(cols.ToString)
                        b = True
                    ElseIf query.SelectList IsNot Nothing Then
                        cols.Append(s.GetSelectColumnList(t, GetFields(s, t, query.SelectList)))
                        sb.Append(cols.ToString)
                        b = True
                    End If
                ElseIf query.Aggregates Is Nothing Then
                    s.GetPKList(os, cols)
                    sb.Append(cols.ToString)
                    b = True
                End If
            End If

            If query.Aggregates IsNot Nothing Then
                For Each a As AggregateBase In query.Aggregates
                    If b Then
                        sb.Append(",")
                    Else
                        b = True
                    End If
                    sb.Append(a.MakeStmt(t, s))
                Next
            End If

            If query.RowNumberFilter IsNot Nothing Then
                If Not s.SupportRowNumber Then
                    Throw New NotSupportedException("RowNumber statement is not supported by " & s.Name)
                End If
                sb.Append(",row_number() over (")
                If query.Sort IsNot Nothing AndAlso Not query.Sort.IsExternal Then
                    sb.Append(RowNumberOrder)
                    'FormOrderBy(query, t, almgr, sb, s, filterInfo, params)
                Else
                    sb.Append("order by ").Append(cols.ToString)
                End If
                sb.Append(") as ").Append(QueryCmdBase.RowNumerColumn)
            End If
        End Sub

        Protected Shared Sub FormTypeTables(ByVal filterInfo As Object, ByVal params As ICreateParam, ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal os As IOrmObjectSchema, ByVal tables() As SourceFragment)
            Dim tbl As SourceFragment = tables(0)
            Dim tbl_real As SourceFragment = tbl
            Dim [alias] As String = Nothing
            If Not almgr.Aliases.TryGetValue(tbl, [alias]) Then
                [alias] = almgr.AddTable(tbl_real, params)
            Else
                tbl_real = tbl.OnTableAdd(params)
                If tbl_real Is Nothing Then
                    tbl_real = tbl
                End If
            End If

            'selectcmd = selectcmd.Replace(tbl.TableName & ".", [alias] & ".")
            almgr.Replace(s, tbl, sb)

            sb.Append(s.GetTableName(tbl_real)).Append(" ").Append([alias])

            For j As Integer = 1 To tables.Length - 1
                Dim join As OrmJoin = CType(s.GetJoins(os, tbl, tables(j), filterInfo), OrmJoin)

                If Not OrmJoin.IsEmpty(join) Then
                    If Not almgr.Aliases.ContainsKey(tables(j)) Then
                        almgr.AddTable(tables(j), CType(Nothing, ParamMgr))
                    End If
                    sb.Append(join.MakeSQLStmt(s, filterInfo, almgr, params))
                End If
            Next
        End Sub

        Protected Shared Sub FormJoins(ByVal filterInfo As Object, ByVal query As QueryCmdBase, ByVal params As ICreateParam, _
            ByVal j As List(Of Worm.Criteria.Joins.OrmJoin), ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator)
            For i As Integer = 0 To j.Count - 1
                Dim join As OrmJoin = CType(j(i), OrmJoin)

                If Not OrmJoin.IsEmpty(join) Then
                    almgr.AddTable(join.Table, CType(Nothing, ParamMgr))
                    sb.Append(join.MakeSQLStmt(s, filterInfo, almgr, params))
                End If
            Next
        End Sub

        Protected Shared Sub FormGroupBy(ByVal query As QueryCmdBase, ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal selectType As Type)
            If query.Group IsNot Nothing Then
                sb.Append(" group by ")
                For Each g As OrmProperty In query.Group
                    If g.Table IsNot Nothing Then
                        sb.Append(almgr.Aliases(g.Table)).Append(".").Append(g.Column)
                    Else
                        Dim t As Type = g.Type
                        If t Is Nothing Then
                            t = selectType
                        End If
                        Dim schema As IOrmObjectSchema = s.GetObjectSchema(t)
                        Dim cm As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()
                        Dim map As MapField2Column = Nothing
                        If cm.TryGetValue(g.Field, map) Then
                            sb.Append(almgr.Aliases(map._tableName)).Append(".").Append(map._columnName)
                        Else
                            Throw New ArgumentException(String.Format("Field {0} of type {1} is not defined", g.Field, g.Type))
                        End If
                    End If
                    sb.Append(",")
                Next
                sb.Length -= 1
            End If
        End Sub

        Protected Shared Sub FormOrderBy(ByVal query As QueryCmdBase, ByVal t As Type, ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal filterInfo As Object, ByVal params As ICreateParam)
            If query.Sort IsNot Nothing AndAlso Not query.Sort.IsExternal Then
                Dim adv As Sorting.SortAdv = TryCast(query.Sort, Sorting.SortAdv)
                If adv IsNot Nothing Then
                    adv.MakeStmt(s, almgr, sb, t, filterInfo, params)
                Else
                    s.AppendOrder(t, query.Sort, almgr, sb)
                End If
            End If
        End Sub

        Public Shared Function MakeQueryStatement(ByVal filterInfo As Object, ByVal schema As SQLGenerator, _
            ByVal query As QueryCmdBase, ByVal params As ICreateParam, ByVal t As Type, _
            ByVal joins As List(Of Worm.Criteria.Joins.OrmJoin), ByVal f As IFilter, ByVal almgr As AliasMgr) As String

            Dim sb As New StringBuilder
            Dim s As SQLGenerator = schema
            Dim os As IOrmObjectSchema = Nothing

            If query.Table Is Nothing Then
                os = s.GetObjectSchema(t)
            End If

            sb.Append("select ")

            If query.Distinct Then
                sb.Append("distinct ")
            End If

            If query.Top IsNot Nothing Then
                sb.Append(s.TopStatement(query.Top.Count, query.Top.Percent, query.Top.Ties)).Append(" ")
            End If

            FormSelectList(query, t, sb, s, os, almgr, filterInfo, params)

            sb.Append(" from ")

            Dim tables() As SourceFragment = Nothing
            If os IsNot Nothing Then
                tables = os.GetTables()
            Else
                tables = New SourceFragment() {query.Table}
            End If

            FormTypeTables(filterInfo, params, almgr, sb, s, os, tables)

            FormJoins(filterInfo, query, params, joins, almgr, sb, s)

            s.AppendWhere(os, f, almgr, sb, filterInfo, params)

            FormGroupBy(query, almgr, sb, s, t)

            If query.RowNumberFilter Is Nothing Then
                FormOrderBy(query, t, almgr, sb, s, filterInfo, params)
            Else
                Dim r As New StringBuilder
                FormOrderBy(query, t, almgr, r, s, filterInfo, params)
                sb.Replace(RowNumberOrder, r.ToString)
            End If

            If Not String.IsNullOrEmpty(query.Hint) Then
                sb.Append(" ").Append(query.Hint)
            End If

            If query.RowNumberFilter IsNot Nothing Then
                Dim rs As String = sb.ToString
                sb.Length = 0
                sb.Append("select * from (").Append(rs).Append(") as t0t01 where ")
                sb.Append(query.RowNumberFilter.MakeQueryStmt(s, filterInfo, almgr, params))
            End If

            Return sb.ToString
        End Function

#End Region
    End Class

End Namespace