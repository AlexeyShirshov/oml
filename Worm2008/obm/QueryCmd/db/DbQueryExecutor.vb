Imports Worm.Database
Imports System.Collections.Generic
Imports Worm.Orm
Imports Worm.Orm.Meta
Imports Worm.Database.Criteria.Joins
Imports Worm.Criteria.Core

Namespace Query.Database

    <Serializable()> _
    Public Class ExecutorException
        Inherits System.Exception

        Public Sub New(ByVal message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(ByVal message As String, ByVal inner As Exception)
            MyBase.New(message, inner)
        End Sub

        Private Sub New( _
            ByVal info As System.Runtime.Serialization.SerializationInfo, _
            ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
        End Sub
    End Class

    Partial Public Class DbQueryExecutor
        Implements IExecutor

        Private Const RowNumberOrder As String = "qiervfnkasdjvn"

        Private _proc As Object
        Private _procT As Object
        Private _m As Integer
        Private _sm As Integer

        Protected Function GetProcessorAnonym(Of ReturnType As {_IEntity})(ByVal mgr As OrmManagerBase, ByVal query As QueryCmd) As ProcessorBase(Of ReturnType)
            If _proc Is Nothing Then
                Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                'If query.Joins IsNot Nothing Then
                '    j.AddRange(query.Joins)
                'End If

                If query.SelectedType Is Nothing Then
                    If String.IsNullOrEmpty(query.EntityName) Then
                        query.SelectedType = GetType(ReturnType)
                    Else
                        query.SelectedType = mgr.ObjectSchema.GetTypeByEntityName(query.EntityName)
                    End If
                End If

                Dim f() As IFilter = query.Prepare(j, mgr.ObjectSchema, mgr.GetFilterInfo, query.SelectedType)
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
                _proc = New ProcessorBase(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
                'End If

                _m = query.Mark
                _sm = query.SMark
            Else
                Dim p As ProcessorBase(Of ReturnType) = CType(_proc, ProcessorBase(Of ReturnType))
                If _m <> query.Mark Then
                    Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                    Dim f() As IFilter = query.Prepare(j, mgr.ObjectSchema, mgr.GetFilterInfo, query.SelectedType)
                    p.Reset(j, f, query.SelectedType)
                Else
                    If _sm <> query.SMark Then
                        p.ResetStmt()
                    End If
                    If query._resDic Then
                        p.ResetDic()
                    End If
                End If
            End If

            Return CType(_proc, ProcessorBase(Of ReturnType))
        End Function

        Protected Function GetProcessor(Of ReturnType As {ICachedEntity})(ByVal mgr As OrmManagerBase, ByVal query As QueryCmd) As Processor(Of ReturnType)
            If _proc Is Nothing Then
                Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                'If query.Joins IsNot Nothing Then
                '    j.AddRange(query.Joins)
                'End If

                If query.SelectedType Is Nothing Then
                    If String.IsNullOrEmpty(query.EntityName) Then
                        query.SelectedType = GetType(ReturnType)
                    Else
                        query.SelectedType = mgr.ObjectSchema.GetTypeByEntityName(query.EntityName)
                    End If
                End If

                Dim f() As IFilter = query.Prepare(j, mgr.ObjectSchema, mgr.GetFilterInfo, query.SelectedType)
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
                    Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                    Dim f() As IFilter = query.Prepare(j, mgr.ObjectSchema, mgr.GetFilterInfo, query.SelectedType)
                    p.Reset(j, f, query.SelectedType)
                Else
                    If _sm <> query.SMark Then
                        p.ResetStmt()
                    End If
                    If query._resDic Then
                        p.ResetDic()
                    End If
                End If
                p.Created = False
            End If

            Return CType(_proc, Processor(Of ReturnType))
        End Function

        Protected Function GetProcessorT(Of SelectType As {ICachedEntity, New}, ReturnType As {ICachedEntity})(ByVal mgr As OrmManagerBase, ByVal query As QueryCmd) As ProcessorT(Of SelectType, ReturnType)
            If _proc Is Nothing Then
                Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                'If query.Joins IsNot Nothing Then
                '    j.AddRange(query.Joins)
                'End If

                Dim f() As IFilter = query.Prepare(j, mgr.ObjectSchema, mgr.GetFilterInfo, GetType(ReturnType))
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
                _proc = New ProcessorT(Of SelectType, ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
                'End If

                _m = query.Mark
                _sm = query.SMark
            Else
                Dim p As Processor(Of ReturnType) = CType(_proc, Processor(Of ReturnType))
                If _m <> query.Mark Then
                    Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                    Dim f() As IFilter = query.Prepare(j, mgr.ObjectSchema, mgr.GetFilterInfo, GetType(ReturnType))
                    p.Reset(j, f, GetType(SelectType))
                Else
                    If _sm <> query.SMark Then
                        p.ResetStmt()
                    End If
                    If query._resDic Then
                        p.ResetDic()
                    End If
                End If
                p.Created = False
            End If

            Return CType(_proc, ProcessorT(Of SelectType, ReturnType))
        End Function

        Public Function ExecSimple(Of SelectType As {New, _ICachedEntity}, ReturnType)(ByVal mgr As OrmManagerBase, ByVal query As QueryCmd) As System.Collections.Generic.IList(Of ReturnType) Implements IExecutor.ExecSimple
            Dim p As Processor(Of SelectType) = GetProcessor(Of SelectType)(mgr, query)

            Return p.GetSimpleValues(Of ReturnType)()
        End Function

        'Private Shared Function _GetCe(Of ReturnType As _ICachedEntity)( _
        '    ByVal mgr As OrmManagerBase, ByVal query As QueryCmd, ByVal p As ProcessorBase(Of ReturnType), ByVal dic As IDictionary, ByVal id As String, ByVal sync As String) As Worm.OrmManagerBase.CachedItem
        '    Return mgr.GetFromCache(Of ReturnType)(dic, sync, id, query.propWithLoad, p)
        'End Function

        'Private Shared Function d2(Of ReturnType As _IEntity)(ByVal mgr As OrmManagerBase, ByVal query As QueryCmd, ByVal p As ProcessorBase(Of ReturnType), ByVal ce As OrmManagerBase.CachedItem, ByVal s As Cache.IListObjectConverter.ExtractListResult) As Worm.ReadOnlyObjectList(Of ReturnType)
        '    Return ce.GetObjectList(Of ReturnType)(mgr, query.propWithLoad, p.Created, s)
        'End Function

        Private Delegate Function GetCeDelegate( _
            ByVal mgr As OrmManagerBase, ByVal query As QueryCmd, ByVal dic As IDictionary, ByVal id As String, ByVal sync As String) As Worm.OrmManagerBase.CachedItem

        Private Delegate Function GetListFromCEDelegate(Of ReturnType As _IEntity)( _
            ByVal mgr As OrmManagerBase, ByVal query As QueryCmd, ByVal p As OrmManagerBase.ICustDelegateBase(Of ReturnType), ByVal ce As OrmManagerBase.CachedItem, ByVal s As Cache.IListObjectConverter.ExtractListResult) As Worm.ReadOnlyObjectList(Of ReturnType)

        Private Function _Exec(Of ReturnType As _IEntity)(ByVal mgr As OrmManagerBase, _
            ByVal query As QueryCmd, ByVal p As ProcessorBase(Of ReturnType), _
            ByVal d As GetCeDelegate, ByVal d2 As GetListFromCEDelegate(Of ReturnType)) As ReadOnlyObjectList(Of ReturnType)

            Dim key As String = Nothing
            Dim dic As IDictionary = Nothing
            Dim id As String = Nothing
            Dim sync As String = Nothing
            Dim oldExp As Date
            Dim oldList As String = Nothing

            Dim oldCache As Boolean = mgr._dont_cache_lists
            Dim oldStart As Integer = mgr._start
            Dim oldLength As Integer = mgr._length
            Dim oldSchema As QueryGenerator = mgr._schema

            If query.ClientPaging IsNot Nothing Then
                mgr._start = query.ClientPaging.First
                mgr._length = query.ClientPaging.Second
            End If

            If query.LiveTime <> New TimeSpan Then
                oldExp = mgr._expiresPattern
                mgr._expiresPattern = Date.Now.Add(query.LiveTime)
            End If

            If Not String.IsNullOrEmpty(query.ExternalCacheMark) Then
                oldList = mgr._list
                mgr._list = query.ExternalCacheMark
            End If

            If query.Schema IsNot Nothing Then
                mgr._schema = query.Schema
            End If

            mgr._dont_cache_lists = query.DontCache OrElse query.OuterQuery IsNot Nothing OrElse p.Dic Is Nothing

            If Not mgr._dont_cache_lists Then
                key = p.Key
                id = p.Id
                dic = p.Dic
                sync = p.Sync
            End If

            Dim ce As OrmManagerBase.CachedItem = d(mgr, query, dic, id, sync)

            query.LastExecitionResult = mgr.GetLastExecitionResult

            mgr._dont_cache_lists = oldCache
            mgr._start = oldStart
            mgr._length = oldLength
            mgr._list = oldList
            mgr._expiresPattern = oldExp
            mgr._schema = oldSchema

            mgr.RaiseOnDataAvailable()

            Dim s As Cache.IListObjectConverter.ExtractListResult
            'Dim r As ReadOnlyList(Of ReturnType) = ce.GetObjectList(Of ReturnType)(mgr, query.WithLoad, p.Created, s)
            'Return r
            Return d2(mgr, query, p, ce, s)
        End Function

        Public Function Exec(Of ReturnType As {_ICachedEntity})(ByVal mgr As OrmManagerBase, _
            ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType) Implements IExecutor.Exec

            'Dim key As String = Nothing
            'Dim dic As IDictionary = Nothing
            'Dim id As String = Nothing
            'Dim sync As String = Nothing
            'Dim oldExp As Date
            'Dim oldList As String = Nothing

            'Dim oldCache As Boolean = mgr._dont_cache_lists
            'Dim oldStart As Integer = mgr._start
            'Dim oldLength As Integer = mgr._length
            'Dim oldSchema As QueryGenerator = mgr._schema

            'If query.ClientPaging IsNot Nothing Then
            '    mgr._start = query.ClientPaging.First
            '    mgr._length = query.ClientPaging.Second
            'End If

            'If query.LiveTime <> New TimeSpan Then
            '    oldExp = mgr._expiresPattern
            '    mgr._expiresPattern = Date.Now.Add(query.LiveTime)
            'End If

            'If Not String.IsNullOrEmpty(query.ExternalCacheMark) Then
            '    oldList = mgr._list
            '    mgr._list = query.ExternalCacheMark
            'End If

            'If query.Schema IsNot Nothing Then
            '    mgr._schema = query.Schema
            'End If

            'Dim p As Processor(Of ReturnType) = GetProcessor(Of ReturnType)(mgr, query)

            'mgr._dont_cache_lists = query.DontCache OrElse query.OuterQuery IsNot Nothing OrElse p.Dic Is Nothing

            'If Not mgr._dont_cache_lists Then
            '    key = p.Key
            '    id = p.Id
            '    dic = p.Dic
            '    sync = p.Sync
            'End If

            'Dim ce As OrmManagerBase.CachedItem = mgr.GetFromCache(Of ReturnType)(dic, sync, id, query.propWithLoad, p)
            'query.LastExecitionResult = mgr.GetLastExecitionResult

            'mgr._dont_cache_lists = oldCache
            'mgr._start = oldStart
            'mgr._length = oldLength
            'mgr._list = oldList
            'mgr._expiresPattern = oldExp
            'mgr._schema = oldSchema

            'mgr.RaiseOnDataAvailable()

            'Dim s As Cache.IListObjectConverter.ExtractListResult
            ''Dim r As ReadOnlyList(Of ReturnType) = ce.GetObjectList(Of ReturnType)(mgr, query.WithLoad, p.Created, s)
            ''Return r
            'Return ce.GetObjectList(Of ReturnType)(mgr, query.propWithLoad, p.Created, s)

            Dim p As Processor(Of ReturnType) = GetProcessor(Of ReturnType)(mgr, query)

            Return CType(_Exec(Of ReturnType)(mgr, query, p, _
                Function(m As OrmManagerBase, q As QueryCmd, dic As IDictionary, id As String, sync As String) _
                    m.GetFromCache(Of ReturnType)(dic, sync, id, q.propWithLoad, p), _
                Function(m As OrmManagerBase, q As QueryCmd, p2 As OrmManagerBase.ICustDelegateBase(Of ReturnType), ce As OrmManagerBase.CachedItem, s As Cache.IListObjectConverter.ExtractListResult) _
                    ce.GetObjectList(Of ReturnType)(m, q.propWithLoad, p2.Created, s) _
                ), ReadOnlyEntityList(Of ReturnType))
        End Function

        Public Function Exec(Of SelectType As {_ICachedEntity, New}, ReturnType As {_ICachedEntity})(ByVal mgr As OrmManagerBase, _
            ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType) Implements IExecutor.Exec

            Dim p As ProcessorT(Of SelectType, ReturnType) = GetProcessorT(Of SelectType, ReturnType)(mgr, query)

            Return CType(_Exec(Of ReturnType)(mgr, query, p, _
                Function(m As OrmManagerBase, q As QueryCmd, dic As IDictionary, id As String, sync As String) _
                    m.GetFromCache(Of ReturnType)(dic, sync, id, q.propWithLoad, p), _
                Function(m As OrmManagerBase, q As QueryCmd, p2 As OrmManagerBase.ICustDelegateBase(Of ReturnType), ce As OrmManagerBase.CachedItem, s As Cache.IListObjectConverter.ExtractListResult) _
                    ce.GetObjectList(Of ReturnType)(m, q.propWithLoad, p2.Created, s) _
                ), ReadOnlyEntityList(Of ReturnType))
        End Function

        Public Function ExecEntity(Of ReturnType As {Orm._IEntity})(ByVal mgr As OrmManagerBase, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType) Implements IExecutor.ExecEntity
            'Dim dontcache As Boolean = query.DontCache

            'Dim key As String = Nothing
            'Dim dic As IDictionary = Nothing
            'Dim id As String = Nothing
            'Dim sync As String = Nothing
            'Dim oldExp As Date
            'Dim oldList As String = Nothing

            'Dim oldCache As Boolean = mgr._dont_cache_lists
            'Dim oldStart As Integer = mgr._start
            'Dim oldLength As Integer = mgr._length
            'Dim oldSchema As QueryGenerator = mgr._schema

            'mgr._dont_cache_lists = dontcache OrElse query.OuterQuery IsNot Nothing
            'If query.ClientPaging IsNot Nothing Then
            '    mgr._start = query.ClientPaging.First
            '    mgr._length = query.ClientPaging.Second
            'End If

            'If query.LiveTime <> New TimeSpan Then
            '    oldExp = mgr._expiresPattern
            '    mgr._expiresPattern = Date.Now.Add(query.LiveTime)
            'End If

            'If Not String.IsNullOrEmpty(query.ExternalCacheMark) Then
            '    oldList = mgr._list
            '    mgr._list = query.ExternalCacheMark
            'End If

            'If query.Schema IsNot Nothing Then
            '    mgr._schema = query.Schema
            'End If

            'Dim p As ProcessorEntity(Of ReturnType) = GetProcessorAnonym(Of ReturnType)(mgr, query)

            'If Not dontcache Then
            '    key = p.Key
            '    id = p.Id
            '    dic = p.Dic
            '    sync = p.Sync
            'End If

            'Dim r As ReadOnlyObjectList(Of ReturnType) = CType(dic(id), Global.Worm.ReadOnlyObjectList(Of ReturnType))
            'Dim _hit As Boolean = True
            'Dim e, f As TimeSpan
            'If r Is Nothing Then
            '    r = p.GetEntities()
            '    _hit = False
            '    e = p.Exec
            '    f = p.Fetch
            '    If Not mgr._dont_cache_lists Then
            '        dic(id) = r
            '    End If
            'End If

            'query.LastExecitionResult = New OrmManagerBase.ExecutionResult(r.Count, e, f, _hit, mgr._loadedInLastFetch)

            'mgr._dont_cache_lists = oldCache
            'mgr._start = oldStart
            'mgr._length = oldLength
            'mgr._list = oldList
            'mgr._expiresPattern = oldExp
            'mgr._schema = oldSchema

            'mgr.RaiseOnDataAvailable()

            'Return r

            Dim p As ProcessorBase(Of ReturnType) = GetProcessorAnonym(Of ReturnType)(mgr, query)

            Return _Exec(Of ReturnType)(mgr, query, p, _
                Function(m As OrmManagerBase, q As QueryCmd, dic As IDictionary, id As String, sync As String) _
                    m.GetFromCache2(Of ReturnType)(dic, sync, id, q.propWithLoad, p), _
                Function(m As OrmManagerBase, q As QueryCmd, p2 As OrmManagerBase.ICustDelegateBase(Of ReturnType), ce As OrmManagerBase.CachedItem, s As Cache.IListObjectConverter.ExtractListResult) _
                    CType(ce.Obj, Global.Worm.ReadOnlyObjectList(Of ReturnType)))
        End Function

#Region " Shared helpers "

        Protected Shared Function GetFields(ByVal gen As QueryGenerator, ByVal type As Type, ByVal q As QueryCmd, ByVal withLoad As Boolean) As List(Of ColumnAttribute)
            Dim c As IList(Of OrmProperty) = q.SelectList
            Dim l As List(Of ColumnAttribute) = Nothing
            If c IsNot Nothing Then
                l = New List(Of ColumnAttribute)
                For Each p As OrmProperty In c
                    'If Not type.Equals(p.Type) Then
                    '    Throw New NotImplementedException
                    'End If
                    If p.Type Is Nothing Then
                        Dim f As String = p.Field
                        If Not String.IsNullOrEmpty(p.Computed) Then
                            f = p.Column
                        End If

                        If String.IsNullOrEmpty(f) Then
                            Throw New InvalidOperationException(String.Format("Column {0} must have a field", p.Column))
                        End If

                        Dim cl As ColumnAttribute = gen.GetColumnByFieldName(type, f)
                        If cl Is Nothing Then
                            cl = New ColumnAttribute
                            cl.FieldName = f
                        End If
                        cl.Column = p.Column
                        l.Add(cl)
                    Else
                        Dim cl As ColumnAttribute = gen.GetColumnByFieldName(p.Type, p.Field)
                        If cl Is Nothing Then
                            cl = gen.GetColumnByFieldName(type, p.Field)
                        End If

                        If cl Is Nothing Then
                            Throw New InvalidOperationException(String.Format("Column {0} must have a field", p.Column))
                        End If

                        l.Add(cl)
                    End If
                Next
                'l.Sort()
            Else
                If withLoad Then
                    l = gen.GetSortedFieldList(type)
                Else
                    l = gen.GetPrimaryKeys(type)
                End If
            End If

            If q.Aggregates IsNot Nothing Then
                For Each p As AggregateBase In q.Aggregates
                    Dim cl As New ColumnAttribute
                    cl.FieldName = p.Alias
                    cl.Column = p.Alias
                    l.Add(cl)
                Next
            End If
            Return l
        End Function

        Protected Shared Sub FormSelectList(ByVal query As QueryCmd, ByVal queryType As Type, _
            ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal os As IOrmObjectSchema, _
            ByVal almgr As AliasMgr, ByVal filterInfo As Object, ByVal params As ICreateParam, _
            ByVal columnAliases As List(Of String), ByVal innerColumns As List(Of String), ByVal withLoad As Boolean)

            Dim b As Boolean
            Dim cols As New StringBuilder
            If os Is Nothing Then
                If query.SelectList IsNot Nothing Then
                    For Each p As OrmProperty In query.SelectList
                        If Not String.IsNullOrEmpty(p.Table.Name) Then
                            cols.Append(s.GetTableName(p.Table)).Append(".")
                        End If
                        cols.Append(p.Column).Append(", ")
                        columnAliases.Add(p.Column)
                    Next
                    cols.Length -= 2
                    sb.Append(cols.ToString)
                    b = True
                ElseIf innerColumns IsNot Nothing Then
                    For Each c As String In innerColumns
                        cols.Append(c).Append(",")
                        columnAliases.Add(c)
                    Next
                    cols.Length -= 1
                    sb.Append(cols.ToString)
                    b = True
                End If
            Else
                If withLoad Then
                    If query.SelectList Is Nothing AndAlso query.Aggregates Is Nothing Then
                        cols.Append(s.GetSelectColumnList(queryType, Nothing, columnAliases, os))
                        sb.Append(cols.ToString)
                        b = True
                    ElseIf query.SelectList IsNot Nothing Then
                        'cols.Append(s.GetSelectColumnList(queryType, GetFields(s, queryType, query, withLoad), columnAliases, os))
                        cols.Append(s.GetSelectColumns(query.SelectList, columnAliases))
                        sb.Append(cols.ToString)
                        b = True
                    End If
                ElseIf query.SelectList IsNot Nothing Then
                    For Each p As OrmProperty In query.SelectList
                        Dim map As MapField2Column = os.GetFieldColumnMap()(p.Field)
                        cols.Append(s.GetTableName(map._tableName)).Append(".")
                        cols.Append(map._columnName).Append(", ")
                        columnAliases.Add(map._columnName)
                    Next
                    cols.Length -= 2
                    sb.Append(cols.ToString)
                    b = True
                ElseIf query.Aggregates Is Nothing Then
                    s.GetPKList(queryType, os, cols, columnAliases)
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
                    sb.Append(a.MakeStmt(s, innerColumns, params, almgr))
                    If columnAliases IsNot Nothing Then
                        columnAliases.Add(a.GetAlias)
                    End If
                Next
            End If

            If query.RowNumberFilter IsNot Nothing Then
                If Not s.SupportRowNumber Then
                    Throw New NotSupportedException("RowNumber statement is not supported by " & s.Name)
                End If
                sb.Append(",row_number() over (")
                If query.propSort IsNot Nothing AndAlso Not query.propSort.IsExternal Then
                    sb.Append(RowNumberOrder)
                    'FormOrderBy(query, t, almgr, sb, s, filterInfo, params)
                Else
                    sb.Append("order by ").Append(cols.ToString)
                End If
                sb.Append(") as ").Append(QueryCmd.RowNumerColumn)
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
                        almgr.AddTable(tables(j), params)
                    End If
                    sb.Append(join.MakeSQLStmt(s, filterInfo, almgr, params))
                End If
            Next
        End Sub

        Protected Shared Sub FormJoins(ByVal filterInfo As Object, ByVal query As QueryCmd, ByVal params As ICreateParam, _
            ByVal j As List(Of Worm.Criteria.Joins.OrmJoin), ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator)
            For i As Integer = 0 To j.Count - 1
                Dim join As OrmJoin = CType(j(i), OrmJoin)

                If Not OrmJoin.IsEmpty(join) Then
                    'Dim tbl As SourceFragment = join.Table
                    'If tbl Is Nothing Then
                    '    If join.Type IsNot Nothing Then
                    '    Else
                    '    End If
                    'End If
                    'almgr.AddTable(tbl, CType(Nothing, ParamMgr))
                    sb.Append(join.MakeSQLStmt(s, filterInfo, almgr, params))

                    Dim tbl As SourceFragment = join.Table
                    If tbl Is Nothing Then
                        tbl = s.GetTables(join.Type)(0)
                    End If
                    almgr.Replace(s, tbl, sb)
                End If
            Next
        End Sub

        Protected Shared Sub FormGroupBy(ByVal query As QueryCmd, ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal selectType As Type)
            If query.Group IsNot Nothing Then
                sb.Append(" group by ")
                For Each g As OrmProperty In query.Group
                    If g.Table IsNot Nothing Then
                        sb.Append(almgr.Aliases(g.Table)).Append(".").Append(g.Column)
                    Else
                        If Not String.IsNullOrEmpty(g.Computed) Then
                            sb.Append(String.Format(g.Computed, QueryGenerator.ExtractValues(s, almgr.Aliases, g.Values).ToArray))
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
                    End If
                    sb.Append(",")
                Next
                sb.Length -= 1
            End If
        End Sub

        Protected Shared Sub FormOrderBy(ByVal query As QueryCmd, ByVal t As Type, _
            ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal filterInfo As Object, _
            ByVal params As ICreateParam, ByVal columnAliases As List(Of String))
            If query.propSort IsNot Nothing AndAlso Not query.propSort.IsExternal Then
                Dim adv As Sorting.SortAdv = TryCast(query.propSort, Sorting.SortAdv)
                If adv IsNot Nothing Then
                    adv.MakeStmt(s, almgr, columnAliases, sb, t, filterInfo, params)
                Else
                    s.AppendOrder(t, query.propSort, almgr, sb, True, query.SelectList, query.Table)
                End If
            End If
        End Sub

        Public Shared Function MakeQueryStatement(ByVal filterInfo As Object, ByVal schema As SQLGenerator, _
            ByVal query As QueryCmd, ByVal params As ICreateParam, ByVal queryType As Type, _
            ByVal joins As List(Of Worm.Criteria.Joins.OrmJoin), ByVal f As IFilter, ByVal almgr As AliasMgr) As String

            Return MakeQueryStatement(filterInfo, schema, query, params, queryType, joins, f, almgr, Nothing, Nothing, Nothing, 0, query.propWithLoad)
        End Function

        Public Shared Function MakeQueryStatement(ByVal filterInfo As Object, ByVal schema As SQLGenerator, _
            ByVal query As QueryCmd, ByVal params As ICreateParam, ByVal queryType As Type, _
            ByVal joins As List(Of Worm.Criteria.Joins.OrmJoin), ByVal f As IFilter, ByVal almgr As AliasMgr, _
            ByVal columnAliases As List(Of String), ByVal inner As String, ByVal innerColumns As List(Of String), _
            ByVal i As Integer, ByVal withLoad As Boolean) As String

            Dim sb As New StringBuilder
            Dim s As SQLGenerator = schema
            Dim os As IOrmObjectSchema = Nothing

            If query.Table Is Nothing Then
                os = s.GetObjectSchema(queryType)
            End If

            sb.Append("select ")

            If query.propDistinct Then
                sb.Append("distinct ")
            End If

            If query.propTop IsNot Nothing Then
                sb.Append(s.TopStatement(query.propTop.Count, query.propTop.Percent, query.propTop.Ties)).Append(" ")
            End If

            FormSelectList(query, queryType, sb, s, os, almgr, filterInfo, params, columnAliases, innerColumns, withLoad)

            sb.Append(" from ")

            If Not String.IsNullOrEmpty(inner) Then
                sb.Append("(").Append(inner).Append(") as src_t").Append(i)
            Else

                Dim tables() As SourceFragment = Nothing
                If os IsNot Nothing Then
                    tables = os.GetTables()
                Else
                    tables = New SourceFragment() {query.Table}
                End If

                FormTypeTables(filterInfo, params, almgr, sb, s, os, tables)

                FormJoins(filterInfo, query, params, joins, almgr, sb, s)
            End If

            s.AppendWhere(os, f, almgr, sb, filterInfo, params, innerColumns)

            FormGroupBy(query, almgr, sb, s, queryType)

            If query.RowNumberFilter Is Nothing Then
                FormOrderBy(query, queryType, almgr, sb, s, filterInfo, params, columnAliases)
            Else
                Dim r As New StringBuilder
                FormOrderBy(query, queryType, almgr, r, s, filterInfo, params, columnAliases)
                sb.Replace(RowNumberOrder, r.ToString)
            End If

            If Not String.IsNullOrEmpty(query.Hint) Then
                sb.Append(" ").Append(query.Hint)
            End If

            If query.RowNumberFilter IsNot Nothing Then
                Dim rs As String = sb.ToString
                sb.Length = 0
                sb.Append("select ")
                For Each col As String In columnAliases
                    If String.IsNullOrEmpty(col) Then
                        Throw New ExecutorException("Column alias is required")
                    End If
                    sb.Append(col).Append(",")
                Next
                sb.Length -= 1
                sb.Append(" from (").Append(rs).Append(") as t0t01 where ")
                sb.Append(query.RowNumberFilter.MakeQueryStmt(s, filterInfo, almgr, params))
            End If

            Return sb.ToString
        End Function

#End Region

        Public Function ExecSimple(Of ReturnType)(ByVal mgr As OrmManagerBase, ByVal query As QueryCmd) As System.Collections.Generic.IList(Of ReturnType) Implements IExecutor.ExecSimple
            Dim ts() As Reflection.MemberInfo = Me.GetType.GetMember("ExecSimple")
            For Each t As Reflection.MethodInfo In ts
                If t.IsGenericMethod AndAlso t.GetGenericArguments.Length = 2 Then
                    t = t.MakeGenericMethod(New Type() {query.SelectedType, GetType(ReturnType)})
                    Return CType(t.Invoke(Me, New Object() {mgr, query}), System.Collections.Generic.IList(Of ReturnType))
                End If
            Next
            Throw New InvalidOperationException
        End Function

        Public Sub Reset(Of ReturnType As _ICachedEntity)(ByVal mgr As OrmManagerBase, ByVal query As QueryCmd) Implements IExecutor.Reset
            GetProcessor(Of ReturnType)(mgr, query).Renew = True
        End Sub

        Public Sub Reset(Of SelectType As {New, _ICachedEntity}, ReturnType As _ICachedEntity)(ByVal mgr As OrmManagerBase, ByVal query As QueryCmd) Implements IExecutor.Reset
            GetProcessorT(Of SelectType, ReturnType)(mgr, query).Renew = True
        End Sub

        Public Sub ResetEntity(Of ReturnType As Orm._IEntity)(ByVal mgr As OrmManagerBase, ByVal query As QueryCmd) Implements IExecutor.ResetEntity
            GetProcessorAnonym(Of ReturnType)(mgr, query).ResetCache()
        End Sub
    End Class

End Namespace