Imports Worm.Database
Imports System.Collections.Generic
Imports Worm.Orm
Imports Worm.Orm.Meta
Imports Worm.Database.Criteria.Joins
Imports Worm.Criteria.Core

Namespace Query.Database

    Partial Public Class DbQueryExecutor
        Implements IExecutor

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

                If query.Obj IsNot Nothing Then
                    _proc = New M2MProcessor(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
                Else
                    _proc = New Processor(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
                End If

                _m = query.Mark
                _sm = query.SMark
            Else
                If _m <> query.Mark Then
                    CType(_proc, Processor(Of ReturnType)).Reset()
                ElseIf _sm <> query.SMark Then
                    CType(_proc, Processor(Of ReturnType)).ResetStmt()
                End If
                CType(_proc, Processor(Of ReturnType)).Created = False
            End If

            Return CType(_proc, Processor(Of ReturnType))
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
            ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal os As IOrmObjectSchema)

            If os Is Nothing Then
                For Each p As OrmProperty In query.SelectList
                    sb.Append(s.GetTableName(p.Table)).Append(".").Append(p.Column).Append(", ")
                Next
                sb.Length -= 2
            Else
                If query.WithLoad Then
                    If query.SelectList IsNot Nothing Then
                        Dim columns As String = s.GetSelectColumnList(t, GetFields(s, t, query.SelectList))
                        sb.Append(columns)
                    End If
                Else
                    s.GetPKList(os, sb)
                End If
            End If

            If query.Aggregates IsNot Nothing Then
                For Each a As AggregateBase In query.Aggregates
                    sb.Append(",").Append(a.MakeStmt)
                Next
            End If
        End Sub

        Protected Shared Sub FormTypeTables(ByVal filterInfo As Object, ByVal params As ICreateParam, ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal os As IOrmObjectSchema, ByVal tables() As OrmTable)
            Dim tbl As OrmTable = tables(0)
            Dim tbl_real As OrmTable = tbl
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

        Protected Shared Sub FormGroupBy(ByVal query As QueryCmdBase, ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator)
            If query.Group IsNot Nothing Then
                sb.Append(" group by ")
                For Each g As OrmProperty In query.Group
                    Dim schema As IOrmObjectSchema = s.GetObjectSchema(g.Type)
                    Dim cm As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()
                    Dim map As MapField2Column = Nothing
                    If cm.TryGetValue(g.Field, map) Then
                        sb.Append(almgr.Aliases(map._tableName)).Append(".").Append(map._columnName)
                    Else
                        Throw New ArgumentException(String.Format("Field {0} of type {1} is not defined", g.Field, g.Type))
                    End If
                Next
            End If
        End Sub

        Protected Shared Sub FormOrderBy(ByVal query As QueryCmdBase, ByVal t As Type, ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator)
            If query.Sort IsNot Nothing AndAlso Not query.Sort.IsExternal Then
                s.AppendOrder(t, query.Sort, almgr, sb)
            End If
        End Sub

        Public Shared Function MakeQueryStatement(ByVal filterInfo As Object, ByVal schema As SQLGenerator, _
            ByVal query As QueryCmdBase, ByVal params As ICreateParam, ByVal t As Type, _
            ByVal joins As List(Of Worm.Criteria.Joins.OrmJoin), ByVal f As IFilter) As String

            Dim almgr As AliasMgr = AliasMgr.Create
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

            FormSelectList(query, t, sb, s, os)

            sb.Append(" from ")

            Dim tables() As OrmTable = Nothing
            If os IsNot Nothing Then
                tables = os.GetTables()
            Else
                tables = New OrmTable() {query.Table}
            End If

            FormTypeTables(filterInfo, params, almgr, sb, s, os, tables)

            FormJoins(filterInfo, query, params, joins, almgr, sb, s)

            s.AppendWhere(os, f, almgr, sb, filterInfo, params)

            FormGroupBy(query, almgr, sb, s)

            FormOrderBy(query, t, almgr, sb, s)

            If Not String.IsNullOrEmpty(query.Hint) Then
                sb.Append(" ").Append(query.Hint)
            End If

            Return sb.ToString
        End Function

#End Region

    End Class

End Namespace