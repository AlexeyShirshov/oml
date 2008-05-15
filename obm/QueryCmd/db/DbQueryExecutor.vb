Imports Worm.Database
Imports System.Collections.Generic
Imports Worm.Orm
Imports Worm.Orm.Meta
Imports Worm.Database.Criteria.Joins
Imports Worm.Criteria.Core

Namespace Query.Database

    Public Class DbQueryExecutor
        Implements IExecutor

        Class Processor(Of ReturnType As {New, Orm.OrmBase})
            Inherits OrmManagerBase.CustDelegate(Of ReturnType)

            Private _stmt As String
            Private _params As ParamMgr
            Private _cmdType As System.Data.CommandType
            Private _mgr As OrmReadOnlyDBManager
            Private _j As List(Of Worm.Criteria.Joins.OrmJoin)
            Private _f As IFilter
            Private _q As QueryCmdBase
            Private _key As String
            Private _id As String
            Private _sync As String
            Private _dic As IDictionary

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal j As List(Of Worm.Criteria.Joins.OrmJoin), _
                ByVal f As IFilter, ByVal q As QueryCmdBase)
                _mgr = mgr
                _j = j
                _f = f
                _q = q

                _key = Query.GetStaticKey(mgr, j, f)
                _dic = mgr.GetDic(mgr.Cache, _key)
                _id = Query.GetDynamicKey(j, f)
                _sync = _id & mgr.GetStaticKey()
            End Sub

            Public Overrides Sub CreateDepends()

            End Sub

            Public Overrides ReadOnly Property Filter() As Criteria.Core.IFilter
                Get
                    Return _f
                End Get
            End Property

            Public Overloads Overrides Function GetCacheItem(ByVal withLoad As Boolean) As OrmManagerBase.CachedItem
                Return GetCacheItem(GetValues(withLoad))
            End Function

            Public Overloads Overrides Function GetCacheItem(ByVal col As ReadOnlyList(Of ReturnType)) As OrmManagerBase.CachedItem
                Dim sortex As IOrmSorting2 = TryCast(_mgr.ObjectSchema.GetObjectSchema(GetType(ReturnType)), IOrmSorting2)
                Dim s As Date = Nothing
                If sortex IsNot Nothing Then
                    Dim ts As TimeSpan = sortex.SortExpiration(_q.Sort)
                    If ts <> TimeSpan.MaxValue AndAlso ts <> TimeSpan.MinValue Then
                        s = Now.Add(ts)
                    End If
                End If
                Return New OrmManagerBase.CachedItem(_q.Sort, s, _f, col, _mgr)
            End Function

            Public Overrides Function GetValues(ByVal withLoad As Boolean) As ReadOnlyList(Of ReturnType)
                Dim r As ReadOnlyList(Of ReturnType)
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)

                Using cmd As System.Data.Common.DbCommand = dbm.DbSchema.CreateDBCommand
                    MakeStatement(cmd, dbm, Query, GetType(ReturnType), _j, _f)
                    r = New ReadOnlyList(Of ReturnType)(dbm.LoadMultipleObjects(Of ReturnType)( _
                        cmd, Query.WithLoad, Nothing, GetFields(dbm.DbSchema, GetType(ReturnType), Query.SelectList)))
                End Using

                If Query.Sort IsNot Nothing AndAlso Query.Sort.IsExternal Then
                    r = dbm.DbSchema.ExternalSort(Of ReturnType)(dbm, Query.Sort, r)
                End If

                Return r
            End Function

            Protected Sub MakeStatement(ByVal cmd As System.Data.Common.DbCommand, _
               ByVal mgr As OrmReadOnlyDBManager, ByVal query As QueryCmdBase, ByVal t As Type, _
                ByVal joins As List(Of Worm.Criteria.Joins.OrmJoin), ByVal f As IFilter)

                If String.IsNullOrEmpty(_stmt) Then
                    _cmdType = Data.CommandType.Text

                    _params = New ParamMgr(mgr.DbSchema, "p")
                    _stmt = _MakeStatement(mgr, query, _params, t, joins, f)
                End If

                cmd.CommandText = _stmt
                cmd.CommandType = _cmdType
                _params.AppendParams(cmd.Parameters)
            End Sub

            Public Overrides ReadOnly Property Sort() As Sorting.Sort
                Get
                    Return _q.Sort
                End Get
            End Property

            Protected ReadOnly Property Query() As QueryCmdBase
                Get
                    Return _q
                End Get
            End Property

            Public ReadOnly Property Key() As String
                Get
                    Return _key
                End Get
            End Property

            Public ReadOnly Property Id() As String
                Get
                    Return _id
                End Get
            End Property

            Public ReadOnly Property Sync() As String
                Get
                    Return _sync
                End Get
            End Property

            Public ReadOnly Property Dic() As IDictionary
                Get
                    Return _dic
                End Get
            End Property
        End Class

        Private _proc As Object

        Protected Function GetProcessor(Of ReturnType As {New, Orm.OrmBase})(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As Processor(Of ReturnType)
            If _proc Is Nothing Then
                Dim j As New List(Of Worm.Criteria.Joins.OrmJoin)
                If query.Joins IsNot Nothing Then
                    j.AddRange(query.Joins)
                End If

                Dim f As IFilter = Nothing
                If query.Filter IsNot Nothing Then
                    f = query.Filter.Filter(GetType(ReturnType))
                End If

                If query.AutoJoins Then
                    Dim joins() As Worm.Criteria.Joins.OrmJoin = Nothing
                    If mgr.HasJoins(GetType(ReturnType), f, query.Sort, joins) Then
                        j.AddRange(joins)
                    End If
                End If

                _proc = New Processor(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)

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
                dic = p.dic
                sync = p.sync
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

        Protected Shared Function GetFields(ByVal gen As QueryGenerator, ByVal type As Type, ByVal c As List(Of OrmProperty)) As List(Of ColumnAttribute)
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

        Private Shared Sub FormSelectList(ByVal query As QueryCmdBase, ByVal t As Type, _
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

        Private Shared Sub FormTypeTables(ByVal mgr As OrmReadOnlyDBManager, ByVal params As ICreateParam, ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal os As IOrmObjectSchema, ByVal tables() As OrmTable)
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
                Dim join As OrmJoin = CType(s.GetJoins(os, tbl, tables(j), mgr.GetFilterInfo), OrmJoin)

                If Not OrmJoin.IsEmpty(join) Then
                    If Not almgr.Aliases.ContainsKey(tables(j)) Then
                        almgr.AddTable(tables(j), CType(Nothing, ParamMgr))
                    End If
                    sb.Append(join.MakeSQLStmt(s, almgr, params))
                End If
            Next
        End Sub

        Private Shared Sub FormJoins(ByVal mgr As OrmReadOnlyDBManager, ByVal query As QueryCmdBase, ByVal params As ICreateParam, _
            ByVal j As List(Of Worm.Criteria.Joins.OrmJoin), ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator)
            For i As Integer = 0 To j.Count - 1
                Dim join As OrmJoin = CType(j(i), OrmJoin)

                If Not OrmJoin.IsEmpty(join) Then
                    almgr.AddTable(join.Table, CType(Nothing, ParamMgr))
                    sb.Append(join.MakeSQLStmt(s, almgr, params))
                End If
            Next
        End Sub

        Private Shared Sub FormGroupBy(ByVal query As QueryCmdBase, ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator)
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

        Private Shared Sub FormOrderBy(ByVal query As QueryCmdBase, ByVal t As Type, ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator)
            If query.Sort IsNot Nothing AndAlso Not query.Sort.IsExternal Then
                s.AppendOrder(t, query.Sort, almgr, sb)
            End If
        End Sub

        Protected Shared Function _MakeStatement(ByVal mgr As OrmReadOnlyDBManager, _
            ByVal query As QueryCmdBase, ByVal params As ICreateParam, ByVal t As Type, _
            ByVal joins As List(Of Worm.Criteria.Joins.OrmJoin), ByVal f As IFilter) As String

            Dim almgr As AliasMgr = AliasMgr.Create
            Dim sb As New StringBuilder
            Dim s As SQLGenerator = mgr.DbSchema
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

            FormTypeTables(mgr, params, almgr, sb, s, os, tables)

            FormJoins(mgr, query, params, joins, almgr, sb, s)

            s.AppendWhere(os, f, almgr, sb, mgr.GetFilterInfo, params)

            FormGroupBy(query, almgr, sb, s)

            FormOrderBy(query, t, almgr, sb, s)

            Return sb.ToString
        End Function
    End Class

End Namespace