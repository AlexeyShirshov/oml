Imports Worm.Database
Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.Criteria.Joins
Imports Worm.Query.QueryCmd
Imports Worm.Cache
Imports Worm.Expressions2
Imports System.Linq
Imports Worm.Criteria

'Imports Worm.Database.Sorting

Namespace Query.Database

    <Serializable()>
    Public Class ExecutorException
        Inherits System.Exception

        Public Sub New(ByVal message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(ByVal message As String, ByVal inner As Exception)
            MyBase.New(message, inner)
        End Sub

        Private Sub New(
            ByVal info As System.Runtime.Serialization.SerializationInfo,
            ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
        End Sub
    End Class

    Partial Public Class DbQueryExecutor
        Inherits QueryExecutor

        Public Const RowNumberOrder As String = "qiervfnkasdjvn"

        Private _proc As BaseProvider
        'Private _procT As BaseProvider
        'Private _procA As BaseProvider
        'Private _procAT As BaseProvider
        'Private _procS As BaseProvider
        'Private _procSM As BaseProvider

#Region " Get providers "

        Protected Overrides Function GetProvider(ByVal mgr As OrmManager, ByVal query As QueryCmd,
            ByVal initTypes As InitTypesDelegate) As CacheItemBaseProvider
            If Prepared Then
                _proc = New BaseProvider(mgr, query)
            Else
                If _proc Is Nothing Then
                    'Dim j As New List(Of List(Of Worm.Criteria.Joins.QueryJoin))
                    'If query.Joins IsNot Nothing Then
                    '    j.AddRange(query.Joins)
                    'End If

                    'If query.SelectedType Is Nothing Then
                    '    If String.IsNullOrEmpty(query.SelectedEntityName) Then
                    '        query.SelectedType = If(query.CreateType IsNot Nothing, query.CreateType, GetType(ReturnType))
                    '    Else
                    '        query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.SelectedEntityName)
                    '    End If
                    'End If

                    Dim r As Boolean
                    If initTypes Is Nothing Then
                        If query.NeedSelectType(mgr.MappingEngine) Then
                            Throw New ExecutorException("Cannot get provider")
                        End If
                    Else
                        r = initTypes(mgr, query)
                    End If

                    'Dim ct As EntityUnion = query.CreateType
                    'Dim types As ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?)) = Nothing
                    'If query.SelectClause IsNot Nothing AndAlso query.SelectClause.SelectTypes IsNot Nothing Then
                    '    types = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(query.SelectClause.SelectTypes)
                    'End If
                    'Dim f As FromClauseDef = query.FromClause

                    QueryCmd.Prepare(query, Me, mgr.MappingEngine, mgr.ContextInfo, mgr.StmtGenerator)
                    If query._cancel Then
                        Return Nothing
                    End If
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
                    _proc = New BaseProvider(mgr, query)
                    'End If
                Else
                    Dim p As BaseProvider = _proc
                    'If query.SelectedType Is Nothing Then
                    '    If String.IsNullOrEmpty(query.SelectedEntityName) Then
                    '        query.SelectedType = If(query.CreateType IsNot Nothing, query.CreateType, GetType(ReturnType))
                    '    Else
                    '        query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.SelectedEntityName)
                    '    End If
                    'End If

                    'InitTypes(mgr,query,GetType(CreateType))

                    If _proc.QMark <> query.Mark Then
                        Dim r As Boolean
                        If initTypes Is Nothing Then
                            If query.NeedSelectType(mgr.MappingEngine) Then
                                Throw New ExecutorException("Cannot get provider")
                            End If
                        Else
                            r = initTypes(mgr, query)
                        End If
                        QueryCmd.Prepare(query, Me, mgr.MappingEngine, mgr.ContextInfo, mgr.StmtGenerator)
                        p.Reset(mgr, query)
                    Else
                        p.Init(mgr, query)
                        p.SetTemp(query)
                        If _proc.QSMark <> query.SMark Then
                            p.ResetStmt()
                        End If
                        'If query._resDic Then
                        '    p.ResetDic()
                        'End If
                    End If
                    p.CacheMiss = False
                End If
            End If

            _proc.SetMark(query)

            Return _proc
        End Function

        Protected Shared Function InitTypes(ByVal mgr As OrmManager, ByVal query As QueryCmd,
                                       ByVal type As Type) As Boolean
            Dim r As Boolean

            If Not GetType(AnonymousEntity).IsAssignableFrom(type) Then
                If query.NeedSelectType(mgr.MappingEngine) Then
                    Dim tt As Type = Nothing
                    If query.CreateType IsNot Nothing Then
                        tt = query.CreateType.AnyType
                    End If
                    If tt Is Nothing Then
                        tt = type
                    End If
                    query.SelectInt(tt, mgr.MappingEngine)
                    r = True
                End If
            End If

            If query.CreateType Is Nothing Then
                If Not type.IsInterface Then
                    query.Into(type)
                Else
                    query.Into(query.GetSelectedType(mgr.MappingEngine))
                End If
            End If

            Return r
        End Function

        Protected Overrides Function GetCacheItemProvoderAnonym(Of ReturnType As {_IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As CacheItemBaseProvider
            Return New ProviderAnonym(Of ReturnType)(CType(GetProvider(mgr, query, Function(m As OrmManager, q As QueryCmd) InitTypes(m, q, mgr.MappingEngine.NormalType(GetType(ReturnType)))), BaseProvider))
        End Function

        Protected Overrides Function GetCacheItemProvoderAnonym(Of CreateType As {New, _IEntity}, ReturnType As {_IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As CacheItemBaseProvider
            Return New ProviderAnonym(Of CreateType, ReturnType)(CType(GetProvider(mgr, query, Function(m As OrmManager, q As QueryCmd) InitTypes(m, q, mgr.MappingEngine.NormalType(GetType(CreateType)))), BaseProvider))
        End Function

        Protected Overrides Function GetCacheItemProvoder(Of ReturnType As {ICachedEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As CacheItemBaseProvider
            Return New Provider(Of ReturnType)(CType(GetProvider(mgr, query, Function(m As OrmManager, q As QueryCmd) InitTypes(m, q, mgr.MappingEngine.NormalType(GetType(ReturnType)))), BaseProvider))
        End Function

        Protected Overrides Function GetCacheItemProvoderT(Of CreateType As {ICachedEntity, New},
                                                       ReturnType As {ICachedEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As CacheItemBaseProvider
            Dim bp As BaseProvider = CType(GetProvider(mgr, query, Function(m As OrmManager, q As QueryCmd) InitTypes(m, q, mgr.MappingEngine.NormalType(GetType(CreateType)))), BaseProvider)
            If bp Is Nothing Then
                Return Nothing
            Else
                Return New ProviderT(Of CreateType, ReturnType)(bp)
            End If
        End Function

        Protected Overrides Function GetCacheItemProvoderS(Of T)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As CacheItemBaseProvider
            Return New SimpleProvider(Of T)(CType(GetProvider(mgr, query, Nothing), BaseProvider))
        End Function

#End Region

        Private Sub SetSchema4Object(ByVal mgr As OrmManager, ByVal created As Boolean, ByVal o As IEntity)
            CType(o, _IEntity).SpecificMappingEngine = mgr.MappingEngine
        End Sub

        Private Sub SetSchema4Object(ByVal mgr As OrmManager, ByVal o As IEntity)
            CType(o, _IEntity).SpecificMappingEngine = mgr.MappingEngine
        End Sub

        Protected Function ExecBase(Of ReturnType)(ByVal mgr As OrmManager,
            ByVal query As QueryCmd, ByVal cacheItemProvoder As GetCacheItemProvoderDelegate,
            ByVal cachedItem As GetCachedItemDelegate,
            ByVal resultFromCachedItem As GetListFromCachedItemDelegate(Of ReturnType)) As ReturnType

            Dim key As String = Nothing
            Dim dic As IDictionary = Nothing
            Dim id As String = Nothing
            Dim sync As String = Nothing
            Dim oldExp As Date
            Dim oldList As String = Nothing

            Dim oldCache As Boolean = mgr._dont_cache_lists
            Dim oldStart As Integer = mgr._start
            Dim oldLength As Integer = mgr._length
            Dim oldRev As Boolean = mgr._rev
            Dim oldSchema As ObjectMappingEngine = mgr.MappingEngine
            Dim op As Boolean = mgr.IsPagingOptimized
            Dim oldThreads As Integer? = Nothing

            If Not query.ClientPaging.IsEmpty Then
                mgr._start = query.ClientPaging.Start
                mgr._length = query.ClientPaging.Length
                mgr._op = query.ClientPaging.OptimizeCache
            ElseIf query.Pager IsNot Nothing Then
                AddHandler mgr.DataAvailable, AddressOf query.OnDataAvailable
                AddHandler OnRestoreDefaults, AddressOf query.OnRestoreDefaults
            End If

            If query.LiveTime <> New TimeSpan Then
                oldExp = mgr._expiresPattern
                mgr._expiresPattern = Date.Now.Add(query.LiveTime)
            End If

            If Not String.IsNullOrEmpty(query.ExternalCacheMark) Then
                oldList = mgr._list
                mgr._list = query.ExternalCacheMark
            End If

            If query.SpecificMappingEngine IsNot Nothing Then
                'mgr.RaiseObjectCreation = True
                mgr.SetMapping(query.SpecificMappingEngine)
                AddHandler mgr.ObjectLoaded, AddressOf SetSchema4Object
                AddHandler mgr.ObjectRestoredFromCache, AddressOf SetSchema4Object
            End If

            Dim dbMgr = CType(mgr, OrmReadOnlyDBManager)
            If dbMgr.LoadResultsetThreadCount <> query.LoadResultsetThreadCount Then
                oldThreads = dbMgr.LoadResultsetThreadCount
                dbMgr.LoadResultsetThreadCount = query.LoadResultsetThreadCount
            End If

            Try
                Dim c As New QueryCmd.svct(query)
                Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)

                    Dim p As BaseProvider = CType(cacheItemProvoder(), BaseProvider)
                    If p Is Nothing Then
                        c.DontReset = True
                        Return Nothing
                    Else
                        Prepared = False
                    End If

                    mgr._dont_cache_lists = query.DontCache OrElse p.Dic Is Nothing OrElse query.IsRealFTS

                    If Not mgr._dont_cache_lists Then
                        key = p.Key
                        id = p.Id
                        dic = p.Dic
                        sync = p.Sync
                    End If

                    'Debug.WriteLine(key)
                    'Debug.WriteLine(query.Mark)
                    'Debug.WriteLine(query.SMark)

                    'Dim oldLoad As Boolean = query._load
                    'Dim created As Boolean = True
                    'If query.ClientPaging IsNot Nothing Then
                    '    query._load = False
                    '    created = False
                    'End If

                    Dim ce As Cache.CachedItemBase = cachedItem(mgr, query, dic, id, sync, p)
                    p.Clear()
                    _proc.Clear()
                    _proc.SetDependency(p)
                    Dim args As New IExecutor.GetCacheItemEventArgs
                    RaiseOnGetCacheItem(args)
                    'query._load = oldLoad

                    query.LastExecutionResult = mgr.LastExecutionResult

                    mgr.RaiseOnDataAvailable()

                    Dim s As Cache.IListObjectConverter.ExtractListResult
                    'Dim r As ReadOnlyList(Of ReturnType) = ce.GetObjectList(Of ReturnType)(mgr, query.WithLoad, p.Created, s)
                    'Return r
                    Dim res As ReturnType = resultFromCachedItem(mgr, query, p, ce, s, Not p.CacheMiss OrElse args.ForceLoad)

                    'mgr.RaiseObjectCreation = oldC

                    Return res
                End Using
            Finally
                If oldThreads.HasValue Then
                    dbMgr.LoadResultsetThreadCount = oldThreads.Value
                End If

                mgr._dont_cache_lists = oldCache
                mgr._start = oldStart
                mgr._length = oldLength
                mgr._op = op
                mgr._list = oldList
                mgr._expiresPattern = oldExp
                mgr.SetMapping(oldSchema)
                RaiseOnRestoreDefaults(mgr)

                RemoveHandler mgr.ObjectLoaded, AddressOf SetSchema4Object
                RemoveHandler mgr.ObjectRestoredFromCache, AddressOf SetSchema4Object
            End Try
        End Function

        Protected Overrides Function _Exec(Of ReturnType)(ByVal mgr As OrmManager,
                                                          ByVal query As QueryCmd, ByVal cacheItemProvoder As GetCacheItemProvoderDelegate,
                                                          ByVal cachedItem As GetCachedItemDelegate, ByVal resultFromCachedItem As GetListFromCachedItemDelegate(Of ReturnType)) As ReturnType

            Dim dbm As OrmReadOnlyDBManager = CType(mgr, OrmReadOnlyDBManager)

            Dim connHandler As OrmReadOnlyDBManager.ConnectionExceptionEventHandler = Nothing
            Dim cmdHandler As OrmReadOnlyDBManager.CommandExceptionEventHandler = Nothing
            Dim addInfo = Sub(s As OrmReadOnlyDBManager, args As EventArgs)
                              query._messages = args
                          End Sub
            Try
                connHandler = SubscribeToConnectionEvents(query, dbm)
                cmdHandler = SubscribeCommandToEvents(query, dbm)

                AddHandler dbm.InfoMessage, addInfo

                Dim timeout As Nullable(Of Integer) = dbm.CommandTimeout

                If query.CommandTimeout.HasValue Then
                    dbm.CommandTimeout = query.CommandTimeout
                End If

                Dim res As ReturnType = ExecBase(Of ReturnType)(mgr, query, cacheItemProvoder, cachedItem, resultFromCachedItem)

                dbm.CommandTimeout = timeout

                Return res
            Finally
                RemoveHandler dbm.InfoMessage, addInfo

                UnsubscribeFromCommandEvents(dbm, cmdHandler)
                UnsubscribeFromConnectionEvents(dbm, connHandler)
            End Try
        End Function

        'Public Function Exec(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadonlyMatrix Implements IExecutor.Exec
        '    Return _Exec(Of ReadonlyMatrix)(mgr, query, _
        '        Function() GetProcessor(mgr, query), _
        '        Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
        '            m.GetFromCacheBase(dic, sync, id, Nothing, p2, Nothing), _
        '        Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
        '            ce.GetMatrix(m, q.propWithLoads, created, m.GetStart, m.GetLength, s) _
        '        )
        'End Function

        'Public Function ExecSimple(Of ReturnType)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As IList(Of ReturnType) Implements IExecutor.ExecSimple
        '    'Dim ts() As Reflection.MemberInfo = Me.GetType.GetMember("ExecSimple")
        '    'For Each t As Reflection.MethodInfo In ts
        '    '    If t.IsGenericMethod AndAlso t.GetGenericArguments.Length = 2 Then
        '    '        t = t.MakeGenericMethod(New Type() {query.SelectedType, GetType(ReturnType)})
        '    '        Return CType(t.Invoke(Me, New Object() {mgr, query}), System.Collections.Generic.IList(Of ReturnType))
        '    '    End If
        '    'Next
        '    'Throw New InvalidOperationException
        '    'Dim p As BaseProvider = GetProcessor(mgr, query)

        '    'Return p.GetSimpleValues(Of ReturnType)()
        '    Dim olds As Boolean = query.CacheSort
        '    query.CacheSort = True
        '    Try
        '        Return _Exec(mgr, query, Function() GetProcessorS(Of ReturnType)(mgr, query), _
        '                        Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
        '                            m.GetFromCacheBase(dic, sync, id, Nothing, p2, Nothing), _
        '                        Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
        '                            ce.GetObjectList(Of ReturnType)(mgr, m.GetStart, m.GetLength))
        '    Finally
        '        query._cacheSort = olds
        '    End Try
        'End Function

        'Public Function Exec(Of ReturnType As {_ICachedEntity})(ByVal mgr As OrmManager, _
        '    ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType) Implements IExecutor.Exec

        '    Return CType(_Exec(Of ReadOnlyEntityList(Of ReturnType))(mgr, query, _
        '        Function() GetProcessor(Of ReturnType)(mgr, query), _
        '        Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
        '            m.GetFromCache(Of ReturnType)(dic, sync, id, q.propWithLoad, p2), _
        '        Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
        '            CType(ce, Cache.UpdatableCachedItem).GetObjectList(Of ReturnType)(m, q.propWithLoad, created, m.GetStart, m.GetLength, s) _
        '        ), ReadOnlyEntityList(Of ReturnType))
        'End Function

        'Public Function Exec(Of SelectType As {_ICachedEntity, New}, ReturnType As {_ICachedEntity})(ByVal mgr As OrmManager, _
        '    ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType) Implements IExecutor.Exec

        '    Return CType(_Exec(Of ReadOnlyEntityList(Of ReturnType))(mgr, query, _
        '        Function() GetProcessorT(Of SelectType, ReturnType)(mgr, query), _
        '        Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
        '            m.GetFromCache(Of ReturnType)(dic, sync, id, q.propWithLoad, p2), _
        '        Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
        '            CType(ce, Cache.UpdatableCachedItem).GetObjectList(Of ReturnType)(m, q.propWithLoad, created, m.GetStart, m.GetLength, s) _
        '        ), ReadOnlyEntityList(Of ReturnType))
        'End Function

        'Public Function ExecEntity(Of ReturnType As {Entities._IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType) Implements IExecutor.ExecEntity
        '    Return _ExecEntity(Of ReturnType)(mgr, query, _
        '        Function() GetProcessorAnonym(Of ReturnType)(mgr, query))
        '    'Return _Exec(Of ReturnType)(mgr, query, _
        '    '    Function() GetProcessorAnonym(Of ReturnType)(mgr, query), _
        '    '    Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
        '    '        m.GetFromCache2(dic, sync, id, q.propWithLoad, p2), _
        '    '    Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
        '    '       CType(ce.GetObjectList(Of ReturnType)(m, q.propWithLoad, created, m.GetStart, m.GetLength, s), Global.Worm.ReadOnlyObjectList(Of ReturnType)))
        'End Function

        'Public Function ExecEntity(Of CreateType As {New, _IEntity}, ReturnType As {Entities._IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType) Implements IExecutor.ExecEntity
        '    Return _ExecEntity(Of ReturnType)(mgr, query, _
        '        Function() GetProcessorAnonym(Of CreateType, ReturnType)(mgr, query))
        '    'Return _Exec(Of ReturnType)(mgr, query, _
        '    '   Function() GetProcessorAnonym(Of CreateType, ReturnType)(mgr, query), _
        '    '   Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
        '    '       m.GetFromCache2(dic, sync, id, q.propWithLoad, p2), _
        '    '   Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
        '    '       CType(ce.GetObjectList(Of ReturnType)(m, q.propWithLoad, created, m.GetStart, m.GetLength, s), Global.Worm.ReadOnlyObjectList(Of ReturnType)))
        'End Function

#Region " Shared helpers "

        'Protected Shared Function GetFields(ByVal mpe As ObjectMappingEngine, _
        '    ByVal q As QueryCmd, ByVal c As IList(Of SelectExpression)) As List(Of ColumnAttribute)

        '    Dim selectType As Type = q.SelectedType
        '    Dim withLoad As Boolean = q.propWithLoad OrElse Not GetType(ICachedEntity).IsAssignableFrom(selectType)
        '    Dim l As List(Of ColumnAttribute) = Nothing
        '    If c IsNot Nothing Then
        '        l = New List(Of ColumnAttribute)
        '        For Each p As SelectExpression In c
        '            'If Not type.Equals(p.Type) Then
        '            '    Throw New NotImplementedException
        '            'End If
        '            If p.Table IsNot Nothing OrElse p.IsCustom Then
        '                Dim f As String = p.PropertyAlias
        '                If Not String.IsNullOrEmpty(p.Computed) Then
        '                    f = p.Column
        '                End If

        '                If String.IsNullOrEmpty(f) Then
        '                    Throw New InvalidOperationException(String.Format("Column {0} must have a field", p.Column))
        '                End If

        '                Dim cl As ColumnAttribute = If(selectType IsNot Nothing, mpe.GetColumnByPropertyAlias(selectType, f), Nothing)

        '                If cl Is Nothing Then
        '                    cl = New ColumnAttribute
        '                    cl.PropertyAlias = f
        '                Else
        '                    cl = cl.Clone
        '                End If

        '                cl.Column = p.Column
        '                l.Add(cl)
        '            Else
        '                Dim cl As ColumnAttribute = mpe.GetColumnByPropertyAlias(p.ObjectSource.GetRealType(mpe), p.PropertyAlias)
        '                If cl Is Nothing Then
        '                    cl = mpe.GetColumnByPropertyAlias(selectType, p.PropertyAlias)
        '                End If

        '                If cl Is Nothing Then
        '                    Throw New InvalidOperationException(String.Format("Column {0} must have a field", p.Column))
        '                End If

        '                l.Add(cl)
        '            End If
        '        Next

        '        'If type IsNot Nothing Then
        '        '    For Each pk As ColumnAttribute In gen.GetPrimaryKeys(type)
        '        '        If Not l.Contains(pk) Then
        '        '            l.Add(pk)
        '        '        End If
        '        '    Next
        '        'End If
        '        'l.Sort()
        '    ElseIf selectType IsNot Nothing Then
        '        If withLoad Then
        '            l = mpe.GetSortedFieldList(selectType)
        '        Else
        '            l = mpe.GetPrimaryKeys(selectType)
        '        End If
        '    End If

        '    If q.Aggregates IsNot Nothing Then
        '        For Each p As AggregateBase In q.Aggregates
        '            Dim cl As New ColumnAttribute
        '            cl.PropertyAlias = p.Alias
        '            cl.Column = p.Alias
        '            l.Add(cl)
        '        Next
        '    End If
        '    Return l
        'End Function

        'Protected Shared Sub FormSelectList(ByVal mpe As ObjectMappingEngine, ByVal query As QueryCmd, _
        '    ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal os As IEntitySchema, ByVal selectedType As Type, _
        '    ByVal almgr As IPrepareTable, ByVal filterInfo As Object, ByVal params As ICreateParam, _
        '    ByVal columnAliases As List(Of String), ByVal innerColumns As List(Of String), _
        '    ByVal selList As IEnumerable(Of SelectExpression), ByVal i As Integer)

        '    Dim b As Boolean
        '    Dim cols As New StringBuilder
        '    If innerColumns IsNot Nothing Then
        '        If query.propWithLoad Then
        '            For Each c As String In innerColumns
        '                cols.Append("src_t").Append(i).Append(s.Selector).Append(c).Append(",")
        '                columnAliases.Add(c)
        '            Next
        '        ElseIf selectedType IsNot Nothing Then
        '            Dim c As String = mpe.GetPrimaryKeysName(selectedType, mpe, False, columnAliases, os, Nothing)(0)
        '            c = c.Replace(mpe.Delimiter, s.Selector)
        '            cols.Append("src_t").Append(i).Append(s.Selector).Append(c).Append(",")
        '        Else
        '        End If
        '        cols.Length -= 1
        '        sb.Append(cols.ToString)
        '        b = True
        '    ElseIf os Is Nothing Then
        '        If selList IsNot Nothing Then
        '            For Each p As SelectExpression In selList
        '                'If Not String.IsNullOrEmpty(p.Table.Name) Then
        '                '    cols.Append(p.Table.UniqueName(p.ObjectSource)).Append(mpe.Delimiter)
        '                'End If
        '                'cols.Append(p.Column).Append(", ")
        '                'columnAliases.Add(p.Column)
        '                s.CreateSelectExpressionFormater().Format(p, cols, mpe, almgr, params, columnAliases, filterInfo, Nothing, Nothing, True)
        '            Next
        '            cols.Length -= 2
        '            sb.Append(cols.ToString)
        '            b = True
        '        End If
        '    Else
        '        Dim queryType As Type = selectedType
        '        Dim withLoad As Boolean = query.propWithLoad OrElse Not GetType(ICachedEntity).IsAssignableFrom(queryType)
        '        If withLoad Then
        '            If selList Is Nothing AndAlso query.Aggregates Is Nothing Then
        '                cols.Append(mpe.GetSelectColumnList(queryType, mpe, Nothing, columnAliases, os, query.GetSelectedOS))
        '                sb.Append(cols.ToString)
        '                b = True
        '            ElseIf selList IsNot Nothing Then
        '                'cols.Append(s.GetSelectColumnList(queryType, GetFields(s, queryType, query, withLoad), columnAliases, os))
        '                cols.Append(mpe.GetSelectColumns(mpe, selList, columnAliases))
        '                sb.Append(cols.ToString)
        '                b = True
        '            End If
        '        ElseIf selList IsNot Nothing Then
        '            For Each p As SelectExpression In selList
        '                Dim oschema As IEntitySchema = os
        '                If p.ObjectSource IsNot Nothing AndAlso p.ObjectSource.GetRealType(mpe) IsNot selectedType Then
        '                    oschema = mpe.GetObjectSchema(p.ObjectSource.GetRealType(mpe))
        '                End If
        '                Dim map As MapField2Column = oschema.GetFieldColumnMap()(p.PropertyAlias)
        '                cols.Append(map._tableName.UniqueName(p.ObjectSource)).Append(mpe.Delimiter)
        '                Dim col As String = mpe.GetColumnNameByPropertyAlias(oschema, p.PropertyAlias, False, columnAliases, p.ObjectSource)
        '                cols.Append(col).Append(", ")
        '                'columnAliases.Add(map._columnName)
        '            Next
        '            cols.Length -= 2
        '            sb.Append(cols.ToString)
        '            b = True
        '        ElseIf query.Aggregates Is Nothing Then
        '            mpe.GetPKList(queryType, mpe, os, cols, columnAliases, query.GetSelectedOS)
        '            sb.Append(cols.ToString)
        '            b = True
        '        End If
        '    End If

        '    If query.Aggregates IsNot Nothing Then
        '        For Each a As AggregateBase In query.Aggregates
        '            If b Then
        '                sb.Append(",")
        '            Else
        '                b = True
        '            End If
        '            sb.Append(a.MakeStmt(mpe, s, innerColumns, params, almgr, filterInfo, True))
        '            If columnAliases IsNot Nothing Then
        '                columnAliases.Add(a.GetAlias)
        '            End If
        '        Next
        '    End If

        '    If Not b Then
        '        If os IsNot Nothing Then
        '            Throw New NotSupportedException("Select columns must be specified")
        '        End If
        '        sb.Append("*")
        '    End If

        '    If query.RowNumberFilter IsNot Nothing Then
        '        If Not s.SupportRowNumber Then
        '            Throw New NotSupportedException("RowNumber statement is not supported by " & s.Name)
        '        End If
        '        sb.Append(",row_number() over (")
        '        If query.propSort IsNot Nothing AndAlso Not query.propSort.IsExternal Then
        '            sb.Append(RowNumberOrder)
        '            'FormOrderBy(query, t, almgr, sb, s, filterInfo, params)
        '        Else
        '            sb.Append("order by ").Append(cols.ToString)
        '        End If
        '        sb.Append(") as ").Append(QueryCmd.RowNumerColumn)
        '    End If
        'End Sub

        Protected Shared Sub FormSelectList(ByVal mpe As ObjectMappingEngine, ByVal execCtx As IExecutionContext,
                                            ByVal sb As StringBuilder, ByVal s As DbGenerator, ByVal from As FromClauseDef,
                                            ByVal almgr As IPrepareTable, ByVal filterInfo As IDictionary, ByVal params As ICreateParam,
                                            ByVal selList As IEnumerable(Of SelectExpression))

            Dim b As Boolean
            Dim cols As New StringBuilder
            Dim be As BinaryExpressionBase = BinaryExpressionBase.CreateFromEnumerable(selList)
            If be IsNot Nothing Then
                cols.Append(be.MakeStatement(mpe, from, s,
                       params, almgr, filterInfo, MakeStatementMode.Select Or MakeStatementMode.AddColumnAlias, execCtx))
            End If
            If cols.Length > 0 Then
                b = True
            End If
            sb.Append(cols.ToString)

            If Not b Then
                'If os IsNot Nothing Then
                '    Throw New NotSupportedException("Select columns must be specified")
                'End If
                sb.Append("*")
            End If
        End Sub

        'Protected Shared Sub ReplaceSelectList(ByVal mpe As ObjectMappingEngine, ByVal query As QueryCmd, _
        '    ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal os As IEntitySchema, _
        '    ByVal almgr As IPrepareTable, ByVal filterInfo As Object, ByVal params As ICreateParam, _
        '    ByVal selList As IEnumerable(Of SelectExpression))

        '    'Dim tbl As SourceFragment = Nothing
        '    'If query.FromClause IsNot Nothing Then
        '    '    tbl = query.FromClause.Table
        '    'End If

        '    For Each p As SelectExpression In selList
        '        'If Not String.IsNullOrEmpty(p._tempMark) Then
        '        '    s.CreateSelectExpressionFormater().Format(p, sb, query, Nothing, mpe, almgr, params, _
        '        '        filterInfo, Nothing, query.FromClause, True)
        '        'End If
        '        p.MakeStatement(mpe, query.FromClause, s, params, almgr, filterInfo, MakeStatementMode.Replace, query)
        '    Next
        'End Sub

        Public Delegate Function Func(Of T)() As T

        Protected Shared Function FormatSearchTable(ByVal mpe As ObjectMappingEngine, ByVal sb As StringBuilder, ByVal st As SearchFragment,
            ByVal s As DbGenerator, ByVal os As IEntitySchema, ByVal params As ICreateParam,
            ByVal selectType As Type, ByVal contextInfo As IDictionary) As Boolean

            Dim searcht As Type = If(st.Entity Is Nothing, selectType, st.Entity.GetRealType(mpe))
            'If os Is Nothing Then
            '    os = mpe.GetEntitySchema(searcht)
            'End If

            Dim searchTable As SourceFragment = os.Table
            If st.QueryFields IsNot Nothing AndAlso st.QueryFields.Length = 1 Then
                searchTable = os.FieldColumnMap(st.QueryFields(0)).Table
            End If

            Dim table As String = st.GetSearchTableName
            Dim value As String = st.GetFtsString(TryCast(os, IFullTextSupport), searcht)
            Dim pname As String = params.CreateParam(value)
            Dim appendMain As Boolean

            sb.Append(table).Append("(")
            Dim replaced As Boolean = False
            Dim tf As ISearchTable = TryCast(searchTable, ISearchTable)
            If tf Is Nothing Then
                sb.Append(s.GetTableName(searchTable, contextInfo))
            Else
                sb.Append("{290ghern}")
                appendMain = True
            End If
            sb.Append(",")
            Dim ifields() As String = st.QueryFields
            If ifields Is Nothing OrElse ifields.Length = 0 Then
                Dim ifts As IFullTextSupport = TryCast(os, IFullTextSupport)
                If ifts IsNot Nothing Then
                    ifields = ifts.GetIndexedFields
                    If ifields IsNot Nothing AndAlso ifields.Length > 0 Then
                        GoTo l1
                    End If
                End If
                sb.Append("*")
            Else
l1:
                sb.Append("(")
                For Each f As String In ifields
                    Dim m As MapField2Column = os.FieldColumnMap(f)
                    Throw New NotImplementedException
                    'sb.Append(m.SourceFieldExpression).Append(",")
                    'If tf IsNot Nothing AndAlso Not replaced Then
                    '    sb.Replace("{290ghern}", tf.GetRealTable(m.SourceFieldExpression))
                    '    replaced = True
                    'End If
                Next
                sb.Length -= 1
                sb.Append(")")
            End If
            If tf IsNot Nothing AndAlso Not replaced Then
                sb.Replace("{290ghern}", tf.GetRealTable("*"))
            End If
            sb.Append(",")
            sb.Append(pname)
            If st.Top <> Integer.MinValue Then
                sb.Append(",").Append(st.Top)
            End If
            sb.Append(")")

            Return appendMain
        End Function

        Public Shared Function FormTypeTables(ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal params As ICreateParam,
                                              ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal s As DbGenerator,
                                              ByVal osrc As EntityUnion, ByVal q As QueryCmd, ByVal execCtx As IExecutionContext,
                                              ByVal from As QueryCmd.FromClauseDef, ByVal appendMain As Boolean?,
                                              ByVal apd As Func(Of String), ByVal predi As Criteria.PredicateLink) As Pair(Of SourceFragment, String)

            Dim tables() As SourceFragment = Nothing
            Dim osrc_ As EntityUnion = Nothing
            If from IsNot Nothing AndAlso from.ObjectSource IsNot Nothing Then
                osrc_ = from.ObjectSource
            ElseIf from Is Nothing Then
                osrc_ = osrc
            End If

            If [from] Is Nothing Then
                Throw New ExecutorException("From clause must be specified")
            End If

            Dim fromOS As IEntitySchema = Nothing
            Dim pkTable As SourceFragment = Nothing
            Dim fe As EntityUnion = from.GetFromEntity
            If from.Table Is Nothing Then
                'If from IsNot Nothing AndAlso from.ObjectSource IsNot Nothing Then
                '    If q IsNot Nothing Then
                '        fromOS = q.GetEntitySchema(mpe, from.ObjectSource.GetRealType(mpe))
                '    Else
                '        fromOS = mpe.GetEntitySchema(from.ObjectSource.GetRealType(mpe))
                '    End If
                'Else
                '    fromOS = os
                'End If
                If q IsNot Nothing Then
                    If Not q._types.TryGetValue(fe, fromOS) Then
                        'Debug.Assert(from IsNot Nothing, String.Format("From entity {0} must be in _types", from.GetFromEntity.GetRealType(mpe)))
                        fromOS = q.GetEntitySchema(mpe, fe.GetRealType(mpe))
                    End If
                ElseIf execCtx IsNot Nothing Then
                    fromOS = execCtx.GetEntitySchema(mpe, fe.GetRealType(mpe))
                Else
                    fromOS = mpe.GetEntitySchema(fe.GetRealType(mpe))
                End If

#If nlog Then
                'NLog.LogManager.GetCurrentClassLogger?.Trace("From IEntitySchema {1}-{0}. Type hash code: {2}", fromOS.GetHashCode, fromOS.GetType, fe.GetRealType(mpe).GetHashCode)
#End If

                Dim mts As IMultiTableObjectSchema = TryCast(fromOS, IMultiTableObjectSchema)
                If mts Is Nothing Then
                    tables = New SourceFragment() {fromOS.Table}
                    pkTable = fromOS.Table
                Else
                    tables = mts.GetTables()
                    pkTable = fromOS.GetPKTable(osrc.GetRealType(mpe))
                End If
            Else
                pkTable = from.Table
                tables = New SourceFragment() {from.Table}

                Dim selOS As EntityUnion = fe
                If selOS Is Nothing AndAlso q IsNot Nothing Then
                    selOS = q.GetSelectedOS
                End If

                If selOS IsNot Nothing Then
                    If q IsNot Nothing Then
                        If Not q._types.TryGetValue(selOS, fromOS) Then
                            'Debug.Assert(from IsNot Nothing, String.Format("From entity {0} must be in _types", from.GetFromEntity.GetRealType(mpe)))
                            fromOS = q.GetEntitySchema(mpe, selOS.GetRealType(mpe))
                        End If
                    ElseIf execCtx IsNot Nothing Then
                        fromOS = execCtx.GetEntitySchema(mpe, fe.GetRealType(mpe))
                    Else
                        fromOS = mpe.GetEntitySchema(selOS.GetRealType(mpe))
                    End If
                End If
            End If

            'If tables.Length = 0 OrElse tables(0) Is Nothing Then
            '    Throw New QueryCmdException("Source table is not specified", Query)
            'End If

            Dim tbl_real As SourceFragment = pkTable
            Dim [alias] As String = Nothing
            If Not almgr.ContainsKey(pkTable, osrc_) Then
                [alias] = almgr.AddTable(tbl_real, osrc_, params)
            Else
                [alias] = almgr.GetAlias(pkTable, osrc_)
                tbl_real = pkTable.OnTableAdd(params)
                If tbl_real Is Nothing Then
                    tbl_real = pkTable
                End If
            End If
            'selectcmd = selectcmd.Replace(tbl.TableName & ".", [alias] & ".")
            'almgr.Replace(mpe, s, pkTable, osrc_, sb)
            'Dim appendMain As Boolean

            Dim selectType As Type = Nothing
            If osrc IsNot Nothing Then
                selectType = osrc.GetRealType(mpe)
            End If

            Dim st As SearchFragment = TryCast(tbl_real, SearchFragment)
            If st IsNot Nothing Then
                appendMain = FormatSearchTable(mpe, sb, st, s, fromOS, params, selectType, contextInfo) OrElse appendMain
            Else
                sb.Append(s.GetTableName(tbl_real, contextInfo))
            End If

            Dim hint = CoreFramework.StringExtensions.Coalesce(from.Hint, osrc_?.Hint, tbl_real.Hint)

            sb.Append(" ").Append([alias])

            If Not String.IsNullOrEmpty(hint) Then
                sb.Append(" ").Append(hint)
            End If

            If apd IsNot Nothing Then
                sb.Append(apd())
            End If
            'sb.Append(s.EndLine)

            Dim pk As Pair(Of SourceFragment, String) = Nothing

            If st IsNot Nothing Then
                Dim stt As Type = selectType, eus As EntityUnion = osrc
                If st.Entity IsNot Nothing Then
                    stt = st.Entity.GetRealType(mpe)
                    eus = st.Entity
                End If

                'If os Is Nothing Then
                '    os = mpe.GetEntitySchema(stt)
                'End If

                If appendMain Is Nothing OrElse Not appendMain.Value Then
                    Dim jb As IJoinBehavior = TryCast(fromOS, IJoinBehavior)

                    If jb IsNot Nothing AndAlso jb.AlwaysJoinMainTable Then
                        appendMain = True
                    End If
                End If

                Dim cs As IContextObjectSchema = TryCast(fromOS, IContextObjectSchema)
                Dim ctxF As IFilter = Nothing
                If cs IsNot Nothing Then
                    ctxF = cs.GetContextFilter(contextInfo)
                    If ctxF IsNot Nothing Then
                        appendMain = True
                    End If
                End If

                'If Not appendMain.HasValue AndAlso filter IsNot Nothing Then
                If Not appendMain.HasValue Then
                    'For Each f As IFilter In filter.GetAllFilters
                    '    Dim ef As EntityFilter = TryCast(f, EntityFilter)
                    '    If ef IsNot Nothing Then
                    '        Dim rt As Type = ef.Template.ObjectSource.GetRealType(mpe)
                    '        'If ef.Template.Type IsNot Nothing Then
                    '        '    If rt Is stt Then
                    '        '        appendMain = True
                    '        '        Exit For
                    '        '    End If
                    '        'Else
                    '        '    Dim t As Type = s.GetTypeByEntityName(ef.Template.EntityName)
                    '        '    If t Is stt Then
                    '        '        appendMain = True
                    '        '        Exit For
                    '        '    End If
                    '        'End If
                    '        If rt Is stt Then
                    '            appendMain = True
                    '            Exit For
                    '        End If
                    '    End If
                    'Next
                    If q._ftypes.ContainsKey(eus) Then
                        appendMain = True
                    ElseIf q._stypes.ContainsKey(eus) Then
                        appendMain = True
                        'ElseIf q._types.ContainsKey(eus) Then
                        '    appendMain = True
                    End If
                End If

                If appendMain Then
                    'Dim j As New QueryJoin(stt, Worm.Criteria.Joins.JoinType.Join, _
                    '    New JoinFilter(tbl_real, s.FTSKey, stt, mpe.GetPrimaryKeys(stt, os)(0).PropertyAlias, _
                    '                   Criteria.FilterOperation.Equal))

                    'sb.Append(s.EndLine).Append(j.MakeSQLStmt(mpe, s, filterInfo, almgr, params, osrc_))

                    Dim frompk As String = mpe.GetPrimaryKey(stt, fromOS) 'mpe.GetPrimaryKeys(stt, fromOS)(0).PropertyAlias 
                    Dim jf As New JoinFilter(tbl_real, s.FTSKey, stt, frompk, Criteria.FilterOperation.Equal)

                    If ctxF IsNot Nothing Then
                        predi.and(ctxF.SetUnion(osrc))
                    End If

                    sb.Append(s.EndLine).Append(QueryJoin.JoinTypeString(JoinType.Join))

                    FormTypeTables(mpe, contextInfo, params, almgr, sb, s, eus, q, execCtx, New QueryCmd.FromClauseDef(eus), False,
                        Function() " on " & jf.MakeQueryStmt(mpe, from, s, q, contextInfo, almgr, params), predi)
                Else
                    pk = New Pair(Of SourceFragment, String)(tbl_real, s.FTSKey)
                End If
            End If

            Dim fs As IMultiTableObjectSchema = TryCast(fromOS, IMultiTableObjectSchema)

            If fs IsNot Nothing Then
                For j As Integer = 0 To tables.Length - 1
                    If tables(j) Is pkTable Then Continue For

                    Dim join As QueryJoin = CType(mpe.GetJoins(fs, pkTable, tables(j), contextInfo), QueryJoin)

                    If Not QueryJoin.IsEmpty(join) Then
                        If Not almgr.ContainsKey(tables(j), osrc) Then
                            almgr.AddTable(tables(j), osrc, params)
                        End If
                        sb.Append(s.EndLine)
                        join.MakeSQLStmt(mpe, from, s, q, contextInfo, almgr, params, osrc_, sb)
                        'almgr.Replace(mpe, s, join.MakeSQLStmt(mpe, from, s, q, filterInfo, almgr, params, osrc_, sb), osrc_, sb)
                    End If
                Next
            End If

            Return pk
        End Function

        Public Shared Sub FormJoins(ByVal mpe As ObjectMappingEngine, ByVal filterInfo As IDictionary, ByVal query As QueryCmd,
                                    ByVal params As ICreateParam, ByVal from As FromClauseDef,
                                    ByVal joins As List(Of Worm.Criteria.Joins.QueryJoin), ByVal almgr As IPrepareTable,
                                    ByVal sb As StringBuilder, ByVal s As DbGenerator, ByVal execCtx As IExecutionContext,
                                    ByVal pk As Pair(Of SourceFragment, String),
                                    ByVal predi As Criteria.PredicateLink, ByVal selOS As EntityUnion)

            Dim selectOS As EntityUnion = Nothing
            Dim selectedType As Type = Nothing
            Dim selSchema As IEntitySchema = Nothing
            Dim setted As Boolean

            selectOS = from.GetFromEntity
            If selectOS Is Nothing Then
                selectOS = selOS
            End If

            Dim j = OrderJoins(selectOS, joins, mpe)

            For i As Integer = 0 To j.Count - 1
                Dim join As QueryJoin = CType(j(i), QueryJoin)

                If Not QueryJoin.IsEmpty(join) Then
                    'Dim tbl As SourceFragment = join.Table
                    'If tbl Is Nothing Then
                    '    If join.Type IsNot Nothing Then
                    '    Else
                    '    End If
                    'End If
                    'almgr.AddTable(tbl, CType(Nothing, ParamMgr))

                    If pk IsNot Nothing AndAlso join.Condition IsNot Nothing Then
                        If Not setted Then

                            selectedType = selectOS.GetRealType(mpe)
                            If query Is Nothing OrElse Not query._types.TryGetValue(selectOS, selSchema) Then
                                selSchema = mpe.GetEntitySchema(selectedType)
                            End If
                            setted = True
                        End If

                        Dim pkname As String = Nothing
                        'If selectedType IsNot Nothing Then
                        pkname = mpe.GetPrimaryKey(selectedType, selSchema) 'mpe.GetPrimaryKeys(selectedType, selSchema)(0).PropertyAlias
                        'End If
                        join.InjectJoinFilter(mpe, selectedType, pkname, pk.First, pk.Second)
                    End If

                    'If join.ObjectSource IsNot Nothing AndAlso join.ObjectSource.IsQuery Then
                    '    sb.Append(s.EndLine).Append(join.JoinTypeString()).Append("(")

                    '    Dim al As QueryAlias = join.ObjectSource.ObjectAlias
                    '    Dim q As QueryCmd = al.Query
                    '    'Dim c As New Query.QueryCmd.svct(q)
                    '    'Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
                    '    '    QueryCmd.Prepare(q, Nothing, mpe, filterInfo, s)
                    '    sb.Append(s.MakeQueryStatement(mpe, q.FromClause, filterInfo, q, params, AliasMgr.Create))
                    '    'Dim almgr2 As AliasMgr = AliasMgr.Create
                    '    'FormSingleQuery(mpe, sb, q, s, AliasMgr.Create, filterInfo, params)
                    '    'End Using

                    '    Dim tbl As SourceFragment = al.Tbl
                    '    If tbl Is Nothing Then
                    '        tbl = New SourceFragment
                    '        al.Tbl = tbl
                    '    End If

                    '    Dim als As String = almgr.AddTable(tbl, join.ObjectSource)

                    '    sb.Append(") as ").Append(als).Append(" on ")
                    '    sb.Append(join.Condition.MakeQueryStmt(mpe, query.FromClause, s, query, filterInfo, almgr, params))
                    '    almgr.Replace(mpe, s, tbl, join.ObjectSource, sb)
                    'Else
                    If join.Table Is Nothing Then
                        Dim t As Type = join.ObjectSource.GetRealType(mpe)
                        'If t Is Nothing Then
                        '    t = s.GetTypeByEntityName(join.EntityName)
                        'End If

                        'Dim oschema As IEntitySchema = mpe.GetEntitySchema(t, False)
                        'If oschema Is Nothing Then
                        '    oschema = query.GetEntitySchema(t)
                        'End If
                        Dim oschema As IEntitySchema = Nothing
                        If execCtx Is Nothing Then
                            oschema = mpe.GetEntitySchema(t)
                        Else
                            oschema = execCtx.GetEntitySchema(mpe, t)
                        End If

                        Dim needAppend As Boolean = True
                        Dim cond As IFilter = join.Condition

                        If cond Is Nothing AndAlso join.M2MObjectSource IsNot Nothing Then
                            Dim t2 As Type = join.M2MObjectSource.GetRealType(mpe)

                            Dim oschema2 As IEntitySchema = mpe.GetEntitySchema(t2)

                            Dim t12t2 As Entities.Meta.M2MRelationDesc = mpe.GetM2MRelation(t, oschema, t2, join.M2MKey)

                            If t12t2 Is Nothing Then
                                Throw New ExecutorException(String.Format("M2M relation between {0} and {1} not found", t, t2))
                            End If

                            Dim t2_pk As String = mpe.GetPrimaryKey(t2, oschema2)
                            Dim t1_pk As String = mpe.GetPrimaryKey(t, oschema)

                            Dim rcmd As RelationCmd = TryCast(query, RelationCmd)
                            If rcmd IsNot Nothing Then
                                If t12t2.Equals(rcmd.RelationDesc) Then
                                    'Dim flt As IGetFilter = Ctor.column(t12t2.Table, t12t2.Column).eq(New ObjectProperty(join.M2MObjectSource, t2_pk))
                                    Dim flt As IGetFilter = New JoinFilter(t12t2.Table, t12t2.Columns, New ObjectProperty(join.M2MObjectSource, t2_pk), FilterOperation.Equal)
                                    predi.and(flt.Filter.SetUnion(rcmd.RelationDesc.Entity))
                                    Continue For
                                End If
                            End If

                            Dim t22t1 As Entities.Meta.M2MRelationDesc = Nothing
                            If t2 Is t Then
                                t22t1 = mpe.GetM2MRelation(t2, oschema2, t, M2MRelationDesc.GetRevKey(join.M2MKey))
                            Else
                                t22t1 = mpe.GetM2MRelation(t2, oschema2, t, join.M2MKey)
                            End If

                            'Dim jl As JoinLink = JCtor.Join(t22t1.Table).On(t22t1.Table, t22t1.Column).Eq(t, t1_pk)
                            Dim tbl As SourceFragment = CType(t22t1.Table.Clone, SourceFragment)
                            Dim jl As JoinLink = Nothing
                            join.TmpTable = tbl
                            Dim prevJ As QueryJoin = GetJoin(j, join)

                            If prevJ Is Nothing Then
                                Dim ftbl As SourceFragment = query.FromClause.Table

                                If ftbl Is Nothing Then
                                    'If Not join.M2MObjectSource.Equals(query.FromClause.ObjectSource) Then
                                    '    Throw New QueryCmdException("Cannot find join for " & join.M2MObjectSource._ToString, query)
                                    'End If

                                    If pk IsNot Nothing Then
                                        jl = JCtor.join(tbl).[on](tbl, t12t2.Columns).eq(pk.First, pk.Second)
                                    Else
                                        jl = JCtor.join(tbl).[on](tbl, t12t2.Columns).eq(New ObjectProperty(join.M2MObjectSource, t2_pk))
                                    End If

                                    'If join.M2MObjectSource.Equals(
                                    If almgr.ContainsKey(oschema.Table, join.ObjectSource) Then
                                        jl.[and](tbl, t22t1.Columns).eq(New ObjectProperty(join.ObjectSource, t1_pk))
                                        needAppend = False
                                    End If
                                Else
                                    If pk IsNot Nothing Then
                                        jl = JCtor.join(tbl).[on](tbl, t12t2.Columns).eq(pk.First, pk.Second)
                                    Else
                                        jl = JCtor.join(tbl).[on](tbl, t12t2.Columns).eq(ftbl, t22t1.Columns)
                                    End If
                                End If
                                needAppend = query.Need2Join(join.ObjectSource)
                            Else
                                If prevJ.Table IsNot Nothing Then
                                    jl = JCtor.join(tbl).[on](tbl, t12t2.Columns).eq(prevJ.Table, t22t1.Columns)
                                Else
                                    jl = JCtor.join(tbl).[on](tbl, t12t2.Columns).eq(prevJ.TmpTable, t22t1.Columns)
                                End If
                                needAppend = query.Need2Join(join.ObjectSource)
                            End If

                            Dim js() As QueryJoin = jl
                            js(0).ObjectSource = join.ObjectSource
                            sb.Append(s.EndLine)
                            js(0).MakeSQLStmt(mpe, query.FromClause, s, query, filterInfo, almgr, params, join.M2MObjectSource, sb)

                            If needAppend Then
                                cond = New JoinFilter(tbl, t22t1.Columns, New ObjectProperty(join.ObjectSource, t1_pk), FilterOperation.Equal) 'Ctor.column(tbl, t22t1.Column).eq(New ObjectProperty(join.ObjectSource, t1_pk)).Filter
                            Else
                                If almgr.ContainsKey(tbl, join.ObjectSource) Then
                                    'almgr.Replace(mpe, s, t22t1.Table, join.ObjectSource, sb)
                                    sb.Replace(t22t1.Table.UniqueName(join.ObjectSource) & mpe.Delimiter, almgr.GetAlias(tbl, join.ObjectSource) & s.Selector)
                                End If
                            End If
                        End If

                        If join.ObjectSource IsNot Nothing AndAlso join.ObjectSource.IsQuery Then
                            sb.Append(s.EndLine)
                            join.MakeSQLStmt(mpe, query.FromClause, s, query, filterInfo, almgr, params, Nothing, sb)
                            'almgr.Replace(mpe, s, join.MakeSQLStmt(mpe, query.FromClause, s, query, filterInfo, almgr, params, Nothing, sb), join.ObjectSource, sb)
                        ElseIf needAppend Then
                            Dim mts As IMultiTableObjectSchema = TryCast(oschema, IMultiTableObjectSchema)
                            If mts Is Nothing OrElse join.JoinType = JoinType.Join Then
                                sb.Append(join.JoinTypeString())

                                Dim cs As IContextObjectSchema = TryCast(oschema, IContextObjectSchema)
                                If cs IsNot Nothing Then
                                    Dim ctxF As IFilter = cs.GetContextFilter(filterInfo)
                                    If ctxF IsNot Nothing Then
                                        predi.and(ctxF.SetUnion(join.ObjectSource))
                                    End If
                                End If

                                Dim f As QueryCmd.FromClauseDef = New QueryCmd.FromClauseDef(join.ObjectSource)
                                Dim fn = Function() " on " & cond.SetUnion(join.M2MObjectSource).SetUnion(join.ObjectSource).MakeQueryStmt(mpe, from, s, execCtx, filterInfo, almgr, params)
                                If cond Is Nothing Then
                                    fn = Function() String.Empty
                                End If

                                FormTypeTables(mpe, filterInfo, params, almgr, sb, s, join.ObjectSource, query, execCtx, f, Nothing, fn, predi)
                            Else
                                'Throw New NotImplementedException
                                sb.Append(s.EndLine).Append(join.JoinTypeString()).Append("(")

                                Dim q As QueryCmd = New QueryCmd().SelectEntity(join.ObjectSource, True)

                                'For Each sf As SourceFragment In mts.GetTables
                                '    If query._f IsNot Nothing Then
                                '        For Each fl As IFilter In query._f.GetAllFilters
                                '            Dim tf As TableFilter = TryCast(fl, TableFilter)
                                '            If tf IsNot Nothing AndAlso tf.Template.Table Is sf Then
                                '                'query._f = query._f.ReplaceFilter(fl, New TableFilter(tbl, tf.Template.Column, tf.Value, tf.Template.Operation).SetUnion(join.ObjectSource))
                                '                q.WhereAdd(tf)
                                '                query._f = query._f.RemoveFilter(fl)
                                '            End If
                                '        Next
                                '    End If
                                'Next

                                q.Prepare(Nothing, mpe, filterInfo, s, False)
                                Dim tbl As New SourceFragment
                                'join.TmpTable = tbl

                                Dim als As String = almgr.AddTable(tbl, join.ObjectSource)
                                query.ReplaceSchema(mpe, t, CreateNewMap(oschema, tbl))

                                sb.Append(s.MakeQueryStatement(mpe, q.FromClause, filterInfo, q, params, AliasMgr.Create))

                                sb.Append(") as ").Append(als).Append(" on ")
                                sb.Append(join.Condition.MakeQueryStmt(mpe, query.FromClause, s, query, filterInfo, almgr, params))

                                For Each sf As SourceFragment In mts.GetTables
                                    'almgr.Replace(mpe, s, sf, join.ObjectSource, sb)
                                    sb.Replace(sf.UniqueName(join.ObjectSource) & mpe.Delimiter, als & s.Selector)

                                    If query._f IsNot Nothing Then
                                        For Each fl As IFilter In query._f.GetAllFilters
                                            Dim tf As TableFilter = TryCast(fl, TableFilter)
                                            If tf IsNot Nothing AndAlso tf.Template.Table Is sf Then
                                                'query._f = query._f.ReplaceFilter(fl, New TableFilter(tbl, tf.Template.Column, tf.Value, tf.Template.Operation).SetUnion(join.ObjectSource))
                                                query._f = query._f.RemoveFilter(fl)
                                            Else
                                                Dim jf As JoinFilter = TryCast(fl, JoinFilter)
                                                If jf IsNot Nothing Then
                                                    If (jf.Left.Table IsNot Nothing AndAlso jf.Left.Table Is sf) OrElse
                                                        (jf.Right.Table IsNot Nothing AndAlso jf.Right.Table Is sf) Then
                                                        query._f = query._f.RemoveFilter(fl)
                                                    End If
                                                End If
                                            End If
                                        Next
                                    End If
                                Next
                            End If
                        End If
                    Else
                        sb.Append(s.EndLine)
                        join.MakeSQLStmt(mpe, query.FromClause, s, query, filterInfo, almgr, params, Nothing, sb)
                        'almgr.Replace(mpe, s, join.MakeSQLStmt(mpe, query.FromClause, s, query, filterInfo, almgr, params, Nothing, sb), join.ObjectSource, sb)
                    End If
                End If
            Next
        End Sub

        Private Shared Function CreateNewMap(ByVal oschema As IEntitySchema, ByVal tbl As SourceFragment) As OrmObjectIndex
            Dim newcol As New OrmObjectIndex
            oschema.FieldColumnMap.CopyTo(newcol)
            For Each m As MapField2Column In newcol
                m.Table = tbl
                For Each sf As SourceField In m.SourceFields
                    If Not String.IsNullOrEmpty(sf.SourceFieldAlias) Then
                        sf.SourceFieldExpression = sf.SourceFieldAlias
                        sf.SourceFieldAlias = Nothing
                    End If
                Next
            Next
            Return newcol
        End Function

        Protected Shared Function GetJoin(ByVal js As IEnumerable(Of QueryJoin), ByVal join As QueryJoin) As QueryJoin
            For Each j As QueryJoin In js
                If Not join.Equals(j) AndAlso join.M2MObjectSource.Equals(j.ObjectSource) AndAlso j.TmpTable IsNot Nothing Then
                    Return j
                End If
            Next
            Return Nothing
        End Function

        Protected Shared Sub FormHaving(ByVal mpe As ObjectMappingEngine, ByVal query As QueryCmd,
                                        ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal s As DbGenerator,
                                        ByVal pmgr As ICreateParam)

            If query.HavingFilter IsNot Nothing Then
                sb.Append(" having ").Append(query.HavingFilter.Filter.MakeQueryStmt(mpe, query.FromClause, s, query, Nothing, almgr, pmgr))
            End If
        End Sub

        Protected Shared Sub FormGroupBy(ByVal mpe As ObjectMappingEngine, ByVal query As QueryCmd,
                                         ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal s As DbGenerator)
            If query.Group IsNot Nothing Then
                sb.Append(" ").Append(query.Group.MakeStatement(mpe, query.FromClause, s, Nothing, almgr, Nothing, MakeStatementMode.None, query))
            End If
        End Sub

        Protected Shared Sub FormOrderBy(ByVal mpe As ObjectMappingEngine, ByVal query As QueryCmd,
                                         ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal s As DbGenerator, ByVal contextInfo As IDictionary,
                                         ByVal params As ICreateParam)
            If query.Sort IsNot Nothing Then
                's.CreateSelectExpressionFormater().Format(query.Sort, sb, query, Nothing, mpe, almgr, params, filterInfo, query.SelectList, query.FromClause, False)
                'Dim adv As DbSort = TryCast(query.propSort, DbSort)
                'If adv IsNot Nothing Then
                '    adv.MakeStmt(s, almgr, columnAliases, sb, t, filterInfo, params)
                'Else
                '    s.AppendOrder(t, query.propSort, almgr, sb, True, query.SelectList, query.Table)
                'End If
                sb.Append(" order by ").Append(BinaryExpressionBase.CreateFromEnumerable(query.Sort).MakeStatement(
                    mpe, query.FromClause, s, params, almgr, contextInfo, MakeStatementMode.None, query))
            End If
        End Sub

        Public Shared Sub MakeInnerQueryStatement(ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal schema As DbGenerator, 
                                                  ByVal query As QueryCmd, ByVal params As ICreateParam, 
                                                  ByVal eu As EntityUnion, ByVal sb As StringBuilder, ByVal almgr As IPrepareTable)

            Dim t As SourceFragment = Nothing 'QueryCmd.InnerTbl

            If eu.IsQuery Then
                If eu.ObjectAlias.Tbl IsNot Nothing Then
                    t = eu.ObjectAlias.Tbl
                Else
                    t = New SourceFragment
                    eu.ObjectAlias.Tbl = t
                End If
            End If

            Dim al As String = almgr.AddTable(t, eu)

            sb.Append("(")
            sb.Append(MakeQueryStatement(mpe, contextInfo, schema, query, params))
            sb.Append(") ").Append(al)

            'almgr.Replace(mpe, schema, t, eu, sb)
        End Sub

        Public Shared Function MakeQueryStatement(dx As IDataContext,
                                                  ByVal query As QueryCmd, ByVal params As ICreateParam) As String

            Return MakeQueryStatement(dx.MappingEngine, dx.Context, CType(dx.StmtGenerator, DbGenerator), query, params, AliasMgr.Create)
        End Function

        Public Shared Function MakeQueryStatement(ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal schema As DbGenerator,
                                                  ByVal query As QueryCmd, ByVal params As ICreateParam) As String

            Return MakeQueryStatement(mpe, contextInfo, schema, query, params, AliasMgr.Create)
        End Function

        Public Shared Function MakeQueryStatement(ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal schema As DbGenerator,
                                                  ByVal query As QueryCmd, ByVal params As ICreateParam, ByVal almgr As IPrepareTable) As String

            If Not query._prepared Then
                Throw New QueryCmdException("Query not prepared", query)
            End If

            Dim sb As New StringBuilder
            Dim s As DbGenerator = schema
            'Dim almgr As IPrepareTable = AliasMgr.Create

            FormSingleQuery(mpe, sb, query, s, almgr, contextInfo, params)

            FormUnions(mpe, query, sb, contextInfo, s, params)

            If query.RowNumberFilter Is Nothing OrElse sb.ToString.IndexOf(RowNumberOrder) < 0 Then
                FormOrderBy(mpe, query, almgr, sb, s, contextInfo, params)
            Else
                Dim r As New StringBuilder
                FormOrderBy(mpe, query, almgr, r, s, contextInfo, params)
                sb.Replace(RowNumberOrder, r.ToString)
            End If

            If query.RowNumberFilter IsNot Nothing OrElse (Not s.SupportTopParam AndAlso query.TopParam IsNot Nothing) Then
                s.FormatRowNumber(mpe, query, contextInfo, params, almgr, sb)
            End If

            If Not String.IsNullOrEmpty(query.Hint) AndAlso Not String.IsNullOrEmpty(s.PlanHint) Then
                sb.Append(String.Format(s.PlanHint, query.Hint))
            End If

            Return sb.ToString
        End Function

        Public Shared Sub FormUnions(ByVal mpe As ObjectMappingEngine, ByVal query As QueryCmd, _
            ByVal sb As StringBuilder, ByVal contextInfo As IDictionary, ByVal s As DbGenerator, _
            ByVal param As ICreateParam)

            If query.Unions IsNot Nothing AndAlso query.Unions.Count > 0 Then
                For i As Integer = 0 To query.Unions.Count - 1
                    Dim p As Pair(Of Boolean, QueryCmd) = query.Unions(i)

                    If p.First Then
                        sb.Append(" union all ")
                    Else
                        sb.Append(" union ")
                    End If
                    sb.Append(s.EndLine)

                    FormSingleQuery(mpe, sb, p.Second, s, AliasMgr.Create, contextInfo, param)
                Next
            End If
        End Sub

        Public Shared Function FormWhere(ByVal mpe As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal filter As Worm.Criteria.Core.IFilter, _
            ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal contextInfo As IDictionary, ByVal pmgr As ICreateParam, _
            ByVal query As QueryCmd) As Boolean

            Dim os As EntityUnion = query.FromClause.GetFromEntity
            If os Is Nothing Then
                os = query.GetSelectedOS
            End If

            Dim con As New Criteria.Conditions.Condition.ConditionConstructor
            con.AddFilter(filter)

            'If t IsNot Nothing Then
            '    Dim schema As IOrmObjectSchema = GetObjectSchema(t)
            '    con.AddFilter(schema.GetFilter(filter_info))
            'End If

            If os IsNot Nothing AndAlso Not os.IsQuery Then
                Dim osSchema As IEntitySchema = Nothing
                If Not query._types.TryGetValue(os, osSchema) Then
                    osSchema = query.GetEntitySchema(mpe, os.GetRealType(mpe))
                End If

                Dim cs As IContextObjectSchema = TryCast(osSchema, IContextObjectSchema)
                If cs IsNot Nothing Then
                    Dim f As IFilter = cs.GetContextFilter(contextInfo)
                    If f IsNot Nothing Then
                        If os IsNot Nothing Then
                            f.SetUnion(os)
                        End If
                        con.AddFilter(f)
                    End If
                End If
            End If

            If Not con.IsEmpty Then
                'Dim bf As Worm.Criteria.Core.IFilter = TryCast(con.Condition, Worm.Criteria.Core.IFilter)
                Dim f As IFilter = TryCast(con.Condition, IFilter)
                'If f IsNot Nothing Then
                Dim s As String = f.MakeQueryStmt(mpe, Nothing, stmt, query, contextInfo, almgr, pmgr)
                If Not String.IsNullOrEmpty(s) Then
                    sb.Append(" where ").Append(s)
                End If
                'Else
                '    sb.Append(" where ").Append(bf.MakeQueryStmt(Me, pmgr))
                'End If
                Return True
            End If
            Return False
        End Function

        Public Shared Sub FormSingleQuery(ByVal mpe As ObjectMappingEngine, ByVal sb As StringBuilder, _
            ByVal query As QueryCmd, ByVal s As DbGenerator, ByVal almgr As IPrepareTable, ByVal contextInfo As IDictionary, _
            ByVal params As ICreateParam)

            'Dim os As IEntitySchema = Nothing
            'Dim selType As Type = query.GetSelectedType(mpe)

            'If selType IsNot Nothing Then
            '    'os = mpe.GetEntitySchema(selType, False)
            '    'If os Is Nothing Then
            '    '    os = query.GetEntitySchema(selType)
            '    'End If
            '    os = query.GetEntitySchema(mpe, selType)
            'End If

            Dim sbStart As Integer = sb.Length

            Dim p As New Criteria.PredicateLink
            Dim newPK As Pair(Of SourceFragment, String) = Nothing

            If query.FromClause.AnyQuery IsNot Nothing Then
                MakeInnerQueryStatement(mpe, contextInfo, s, query.FromClause.AnyQuery, _
                                        params, query.FromClause.QueryEU, sb, almgr)
            Else
                newPK = FormTypeTables( _
                    mpe, contextInfo, params, almgr, sb, s, query.GetSelectedOS, query, query, _
                    query.FromClause, query.AppendMain, Nothing, p)
            End If

#If nlog Then
            'NLog.LogManager.GetCurrentClassLogger?.Trace("FormQuery almgr dump {0}", TryCast(almgr, AliasMgr)?.Dump)
#End If

            FormJoins(mpe, contextInfo, query, params, query.FromClause, query._js, almgr, sb, s, query, newPK, p, query.GetSelectedOS)

            'ReplaceSelectList(mpe, query, sb, s, os, almgr, filterInfo, params, query._sl)
            Dim selSb As New StringBuilder
            selSb.Append("select ")

            If query.IsDistinct Then
                selSb.Append("distinct ")
            End If

            If query.TopParam IsNot Nothing Then
                Dim tstmt As ITopStatement = TryCast(s, ITopStatement)
                If tstmt IsNot Nothing Then
                    selSb.Append(tstmt.TopStatementPercent(query.TopParam.Count, query.TopParam.Percent, query.TopParam.Ties)).Append(" ")
                ElseIf s.SupportTopParam Then
                    selSb.Append(s.TopStatement(query.TopParam.Count)).Append(" ")
                End If
            End If

            FormSelectList(mpe, query, selSb, s, query.FromClause, almgr, contextInfo, params, query._sl)
            If query.RowNumberFilter IsNot Nothing Then
                If Not s.SupportRowNumber Then
                    Throw New NotSupportedException("RowNumber statement is not supported by " & s.Name)
                End If
                selSb.Append(s.MakeRowNumber(mpe, query))
            End If

            selSb.Append(" from ")
            sb.Insert(sbStart, selSb.ToString)

            FormWhere(mpe, s, p.and(query._f).Filter, almgr, sb, contextInfo, params, query)

            FormGroupBy(mpe, query, almgr, sb, s)

            FormHaving(mpe, query, almgr, sb, s, params)
        End Sub
#End Region

        Friend Shared Function SubscribeToConnectionEvents(query As QueryCmd, mgr As OrmReadOnlyDBManager) As OrmReadOnlyDBManager.ConnectionExceptionEventHandler
            Dim h As OrmReadOnlyDBManager.ConnectionExceptionEventHandler = Nothing
            If query.HasConnectionErrorSubscribers Then

                Dim cm As ICreateManager = query.CreateManager

                h = Sub(sender, args)
                        Dim pargs As New QueryCmd.ConnectionExceptionArgs(args.Exception, args.Connection, sender)

                        Dim qh As QueryCmd.ConnectionExceptionEventHandler = query.RaiseConnectionErrorEvent(pargs)

                        If qh IsNot Nothing Then
                            Dim newErrorHandler As OrmReadOnlyDBManager.ConnectionExceptionEventHandler =
                               Sub(mm, args_)

                                   Dim pargs_ As New QueryCmd.ConnectionExceptionArgs(args_.Exception, args_.Connection, mm)
                                   qh(Nothing, pargs_)

                                   args_.Action = CType(CInt(pargs_.Action), OrmReadOnlyDBManager.ConnectionExceptionArgs.ActionEnum)
                                   args_.Context = pargs_.Context
                               End Sub

                            Dim cmh As ICreateManager.CreateManagerEventEventHandler =
                                Sub(cm_ As ICreateManager, cm_args As ICreateManager.CreateManagerEventArgs)

                                    Dim q As QueryCmd = TryCast(cm_args.Context, QueryCmd)

                                    If q Is Nothing OrElse Not q.ContainsConnectionExceptionSubscriber(qh) Then

                                        Dim mgr_ As OrmManager = cm_args.Manager

                                        AddHandler CType(mgr_, OrmReadOnlyDBManager).ConnectionException, newErrorHandler

                                        Dim mgd As OrmManager.ManagerGoingDownEventHandler =
                                            Sub(m As OrmManager)
                                                RemoveHandler m.ManagerGoingDown, mgd
                                                RemoveHandler CType(m, OrmReadOnlyDBManager).ConnectionException, newErrorHandler
                                                'RemoveHandler cm.CreateManagerEvent, cmh
                                            End Sub

                                        AddHandler mgr_.ManagerGoingDown, mgd
                                    End If
                                End Sub

                            AddHandler cm.CreateManagerEvent, cmh

                        End If

                        args.Action = CType(CInt(pargs.Action), OrmReadOnlyDBManager.ConnectionExceptionArgs.ActionEnum)
                        args.Context = pargs.Context
                    End Sub

                AddHandler mgr.ConnectionException, h
            End If

            Return h
        End Function

        Friend Shared Sub UnsubscribeFromConnectionEvents(mgr As OrmReadOnlyDBManager, h As OrmReadOnlyDBManager.ConnectionExceptionEventHandler)
            If h IsNot Nothing Then
                RemoveHandler mgr.ConnectionException, h
            End If
        End Sub

        Friend Shared Function SubscribeCommandToEvents(query As QueryCmd, mgr As OrmReadOnlyDBManager) As OrmReadOnlyDBManager.CommandExceptionEventHandler
            Dim h As OrmReadOnlyDBManager.CommandExceptionEventHandler = Nothing
            If query.HasCommandErrorSubscribers Then

                Dim cm As ICreateManager = query.CreateManager

                h = Sub(sender, args)
                        Dim pargs As New QueryCmd.CommandExceptionArgs(args.Exception, args.Command, sender)

                        Dim qh As QueryCmd.CommandExceptionEventHandler = query.RaiseCommandErrorEvent(pargs)

                        If qh IsNot Nothing Then
                            Dim newErrorHandler As OrmReadOnlyDBManager.CommandExceptionEventHandler =
                               Sub(mm, args_)

                                   Dim pargs_ As New QueryCmd.CommandExceptionArgs(args_.Exception, args_.Command, mm)
                                   qh(Nothing, pargs_)

                                   args_.Action = CType(CInt(pargs_.Action), OrmReadOnlyDBManager.CommandExceptionArgs.ActionEnum)
                                   args_.Context = pargs_.Context
                               End Sub

                            Dim cmh As ICreateManager.CreateManagerEventEventHandler =
                                Sub(cm_ As ICreateManager, cm_args As ICreateManager.CreateManagerEventArgs)

                                    Dim q As QueryCmd = TryCast(cm_args.Context, QueryCmd)

                                    If q Is Nothing OrElse Not q.ContainsCommandExceptionSubscriber(qh) Then

                                        Dim mgr_ As OrmManager = cm_args.Manager

                                        AddHandler CType(mgr_, OrmReadOnlyDBManager).CommandException, newErrorHandler

                                        Dim mgd As OrmManager.ManagerGoingDownEventHandler =
                                            Sub(m As OrmManager)
                                                RemoveHandler m.ManagerGoingDown, mgd
                                                RemoveHandler CType(m, OrmReadOnlyDBManager).CommandException, newErrorHandler
                                                'RemoveHandler cm.CreateManagerEvent, cmh
                                            End Sub

                                        AddHandler mgr_.ManagerGoingDown, mgd
                                    End If
                                End Sub

                            AddHandler cm.CreateManagerEvent, cmh

                        End If

                        args.Action = CType(CInt(pargs.Action), OrmReadOnlyDBManager.CommandExceptionArgs.ActionEnum)
                        args.Context = pargs.Context
                    End Sub

                AddHandler mgr.CommandException, h
            End If

            Return h
        End Function

        Friend Shared Sub UnsubscribeFromCommandEvents(mgr As OrmReadOnlyDBManager, h As OrmReadOnlyDBManager.CommandExceptionEventHandler)
            If h IsNot Nothing Then
                RemoveHandler mgr.CommandException, h
            End If
        End Sub

        Public Overrides Function SubscribeToErrorHandling(mgr As OrmManager, query As QueryCmd) As System.IDisposable
            Dim dbm As OrmReadOnlyDBManager = CType(mgr, OrmReadOnlyDBManager)
            Dim connHandler As OrmReadOnlyDBManager.ConnectionExceptionEventHandler = SubscribeToConnectionEvents(query, dbm)
            Dim cmdHandler As OrmReadOnlyDBManager.CommandExceptionEventHandler = SubscribeCommandToEvents(query, dbm)

            Return New OnExitScopeAction(
                Sub()
                    If connHandler IsNot Nothing Then
                        UnsubscribeFromConnectionEvents(dbm, connHandler)
                    End If

                    If cmdHandler IsNot Nothing Then
                        UnsubscribeFromCommandEvents(dbm, cmdHandler)
                    End If
                End Sub)
        End Function

        Private Shared Function OrderJoins(root As EntityUnion, js As IEnumerable(Of QueryJoin), mpe As ObjectMappingEngine) As IEnumerable(Of QueryJoin)
            For Each j In js
                Dim fr = GetFieldRef(j, root, mpe)
                If fr IsNot Nothing Then
                    Dim l As New List(Of QueryJoin)

                    l.Add(j)

                    If fr.Property.Entity Is Nothing Then
                        l.AddRange(OrderJoins(fr.Table, From k In js
                                                        Where Not k.Equals(j), mpe))
                    Else
                        l.AddRange(OrderJoins(fr.Property.Entity, From k In js
                                   Where Not k.Equals(j), mpe))
                    End If

                    Return l
                End If
            Next

            Return js
        End Function

        Private Shared Function OrderJoins(root As SourceFragment, js As IEnumerable(Of QueryJoin), mpe As ObjectMappingEngine) As IEnumerable(Of QueryJoin)
            For Each j In js
                Dim fr = GetFieldRef(j, root)
                If fr IsNot Nothing Then
                    Dim l As New List(Of QueryJoin)

                    l.Add(j)

                    If fr.Property.Entity Is Nothing Then
                        l.AddRange(OrderJoins(fr.Table, From k In js
                                                        Where Not k.Equals(j), mpe))
                    Else
                        l.AddRange(OrderJoins(fr.Property.Entity, From k In js
                                   Where Not k.Equals(j), mpe))
                    End If

                    Return l
                End If
            Next

            Return js
        End Function

        Private Shared Function GetFieldRef(j As QueryJoin, root As EntityUnion, mpe As ObjectMappingEngine) As FieldReference
            If j.Condition IsNot Nothing Then
                For Each jf In j.Condition.GetAllFilters.OfType(Of JoinFilter)()
                    If jf.Left.Property.Entity = root Then
                        Return jf.Right
                    ElseIf jf.Right.Property.Entity = root Then
                        Return jf.Left
                        'ElseIf jf.Left.Property.Entity IsNot Nothing AndAlso jf.Left.Property.Entity.GetRealType(mpe) = root.GetRealType(mpe) Then
                        '    Return jf.Right
                        'ElseIf jf.Right.Property.Entity IsNot Nothing AndAlso jf.Right.Property.Entity.GetRealType(mpe) = root.GetRealType(mpe) Then
                        '    Return jf.Left
                    End If
                Next
            End If

            Return Nothing
        End Function

        Private Shared Function GetFieldRef(j As QueryJoin, root As SourceFragment) As FieldReference
            For Each jf In j.Condition.GetAllFilters.OfType(Of JoinFilter)()
                If jf.Left.Table IsNot Nothing AndAlso jf.Left.Table.Equals(root) Then
                    Return jf.Right
                ElseIf jf.Right.Table IsNot Nothing AndAlso jf.Right.Table.Equals(root) Then
                    Return jf.Left
                End If
            Next

            Return Nothing
        End Function

        Public Overrides Function Clone() As QueryExecutor
            Dim q As New DbQueryExecutor()
            'CopyTo(q)
            Return q
        End Function

        'Public Overrides Sub CopyTo(q As QueryExecutor)
        '    Dim dbq = TryCast(q, DbQueryExecutor)
        '    If dbq IsNot Nothing Then
        '        dbq._proc = _proc
        '    End If
        '    MyBase.CopyTo(q)
        'End Sub

    End Class

End Namespace
