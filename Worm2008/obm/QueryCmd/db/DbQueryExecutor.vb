Imports Worm.Database
Imports System.Collections.Generic
Imports Worm.Orm
Imports Worm.Orm.Meta
Imports Worm.Database.Criteria.Joins
Imports Worm.Criteria.Core
'Imports Worm.Database.Sorting

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
        Private _procA As Object
        Private _procAT As Object
        Private _m As Guid
        Private _sm As Guid

        Protected Function GetProcessorAnonym(Of ReturnType As {_IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ProviderAnonym(Of ReturnType)
            If _procA Is Nothing Then
                Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                'If query.Joins IsNot Nothing Then
                '    j.AddRange(query.Joins)
                'End If

                If query.SelectedType Is Nothing Then
                    If String.IsNullOrEmpty(query.EntityName) Then
                        'query.SelectedType = GetType(ReturnType)
                        query.SelectedType = If(query.CreateType IsNot Nothing, query.CreateType, GetType(ReturnType))
                    Else
                        query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.EntityName)
                    End If
                End If

                If GetType(AnonymousEntity).IsAssignableFrom(query.SelectedType) Then
                    query.SelectedType = Nothing
                End If

                If query.CreateType Is Nothing Then
                    query.CreateType = query.SelectedType
                End If

                Dim sl As New List(Of List(Of SelectExpression))
                Dim f() As IFilter = query.Prepare(j, mgr.MappingEngine, mgr.GetFilterInfo, query.SelectedType, sl)
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
                _procA = New ProviderAnonym(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query, sl)
                'End If
            Else
                Dim p As ProviderAnonym(Of ReturnType) = CType(_procA, ProviderAnonym(Of ReturnType))

                If query.SelectedType Is Nothing Then
                    If String.IsNullOrEmpty(query.EntityName) Then
                        'query.SelectedType = GetType(ReturnType)
                        query.SelectedType = If(query.CreateType IsNot Nothing, query.CreateType, GetType(ReturnType))
                    Else
                        query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.EntityName)
                    End If
                End If

                If GetType(AnonymousEntity).IsAssignableFrom(query.SelectedType) Then
                    query.SelectedType = Nothing
                End If

                If query.CreateType Is Nothing Then
                    query.CreateType = query.SelectedType
                End If

                If _m <> query.Mark Then
                    Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                    Dim sl As New List(Of List(Of SelectExpression))
                    Dim f() As IFilter = query.Prepare(j, mgr.MappingEngine, mgr.GetFilterInfo, query.SelectedType, sl)
                    p.Reset(mgr, j, f, query.SelectedType, sl, query)
                Else
                    p.Init(mgr, query)
                    If _sm <> query.SMark Then
                        p.ResetStmt()
                    End If
                    If query._resDic Then
                        p.ResetDic()
                    End If
                End If
            End If

            _m = query.Mark
            _sm = query.SMark

            Return CType(_procA, ProviderAnonym(Of ReturnType))
        End Function

        Protected Function GetProcessorAnonym(Of CreateType As {New, _IEntity}, ReturnType As {_IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ProviderAnonym(Of ReturnType)
            If _procAT Is Nothing Then
                Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                'If query.Joins IsNot Nothing Then
                '    j.AddRange(query.Joins)
                'End If

                If query.SelectedType Is Nothing AndAlso Not String.IsNullOrEmpty(query.EntityName) Then
                    query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.EntityName)
                End If

                If GetType(AnonymousEntity).IsAssignableFrom(query.SelectedType) Then
                    query.SelectedType = Nothing
                End If

                If query.CreateType Is Nothing Then
                    query.CreateType = GetType(CreateType)
                End If

                Dim sl As New List(Of List(Of SelectExpression))
                Dim f() As IFilter = query.Prepare(j, mgr.MappingEngine, mgr.GetFilterInfo, query.SelectedType, sl)
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
                _procAT = New ProviderAnonym(Of CreateType, ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query, sl)
                'End If
            Else
                Dim p As ProviderAnonym(Of CreateType, ReturnType) = CType(_procAT, ProviderAnonym(Of CreateType, ReturnType))

                If query.SelectedType Is Nothing AndAlso Not String.IsNullOrEmpty(query.EntityName) Then
                    query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.EntityName)
                End If

                If GetType(AnonymousEntity).IsAssignableFrom(query.SelectedType) Then
                    query.SelectedType = Nothing
                End If

                If _m <> query.Mark Then
                    Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                    Dim sl As New List(Of List(Of SelectExpression))
                    Dim f() As IFilter = query.Prepare(j, mgr.MappingEngine, mgr.GetFilterInfo, query.SelectedType, sl)
                    p.Reset(mgr, j, f, query.SelectedType, sl, query)
                Else
                    p.Init(mgr, query)
                    If _sm <> query.SMark Then
                        p.ResetStmt()
                    End If
                    If query._resDic Then
                        p.ResetDic()
                    End If
                End If
            End If

            _m = query.Mark
            _sm = query.SMark

            Return CType(_procAT, ProviderAnonym(Of CreateType, ReturnType))
        End Function

        Protected Function GetProcessor(Of ReturnType As {ICachedEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As Provider(Of ReturnType)
            If _proc Is Nothing Then
                Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                'If query.Joins IsNot Nothing Then
                '    j.AddRange(query.Joins)
                'End If

                If query.SelectedType Is Nothing Then
                    If String.IsNullOrEmpty(query.EntityName) Then
                        query.SelectedType = If(query.CreateType IsNot Nothing, query.CreateType, GetType(ReturnType))
                    Else
                        query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.EntityName)
                    End If
                End If

                If query.CreateType Is Nothing Then
                    query.CreateType = query.SelectedType
                End If

                Dim sl As New List(Of List(Of SelectExpression))
                Dim f() As IFilter = query.Prepare(j, mgr.MappingEngine, mgr.GetFilterInfo, query.SelectedType, sl)
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
                _proc = New Provider(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query, sl)
                'End If
            Else
                Dim p As Provider(Of ReturnType) = CType(_proc, Provider(Of ReturnType))

                If query.SelectedType Is Nothing Then
                    If String.IsNullOrEmpty(query.EntityName) Then
                        query.SelectedType = If(query.CreateType IsNot Nothing, query.CreateType, GetType(ReturnType))
                    Else
                        query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.EntityName)
                    End If
                End If

                If query.CreateType Is Nothing Then
                    query.CreateType = query.SelectedType
                End If

                If _m <> query.Mark Then
                    Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                    Dim sl As New List(Of List(Of SelectExpression))
                    Dim f() As IFilter = query.Prepare(j, mgr.MappingEngine, mgr.GetFilterInfo, query.SelectedType, sl)
                    p.Reset(mgr, j, f, query.SelectedType, sl, query)
                Else
                    p.Init(mgr, query)
                    If _sm <> query.SMark Then
                        p.ResetStmt()
                    End If
                    If query._resDic Then
                        p.ResetDic()
                    End If
                End If
                p.Created = False
            End If

            _m = query.Mark
            _sm = query.SMark

            Return CType(_proc, Provider(Of ReturnType))
        End Function

        Protected Function GetProcessorT(Of CreateType As {ICachedEntity, New}, ReturnType As {ICachedEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ProviderT(Of CreateType, ReturnType)
            If _procT Is Nothing Then
                Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                'If query.Joins IsNot Nothing Then
                '    j.AddRange(query.Joins)
                'End If

                If query.SelectedType Is Nothing Then
                    If Not String.IsNullOrEmpty(query.EntityName) Then
                        query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.EntityName)
                    Else
                        query.SelectedType = GetType(CreateType)
                    End If
                End If

                If GetType(AnonymousCachedEntity).IsAssignableFrom(query.SelectedType) Then
                    query.SelectedType = Nothing
                End If

                If query.CreateType Is Nothing Then
                    query.CreateType = GetType(CreateType)
                End If

                Dim sl As New List(Of List(Of SelectExpression))
                Dim f() As IFilter = query.Prepare(j, mgr.MappingEngine, mgr.GetFilterInfo, query.SelectedType, sl)
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
                _procT = New ProviderT(Of CreateType, ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query, sl)
                'End If
            Else
                Dim p As Provider(Of ReturnType) = CType(_procT, Provider(Of ReturnType))

                If query.SelectedType Is Nothing Then
                    If Not String.IsNullOrEmpty(query.EntityName) Then
                        query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.EntityName)
                    Else
                        query.SelectedType = GetType(CreateType)
                    End If
                End If

                If _m <> query.Mark Then
                    Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                    Dim sl As New List(Of List(Of SelectExpression))
                    Dim f() As IFilter = query.Prepare(j, mgr.MappingEngine, mgr.GetFilterInfo, query.SelectedType, sl)
                    p.Reset(mgr, j, f, GetType(CreateType), sl, query)
                Else
                    p.Init(mgr, query)
                    If _sm <> query.SMark Then
                        p.ResetStmt()
                    End If
                    If query._resDic Then
                        p.ResetDic()
                    End If
                End If
                p.Created = False
            End If

            _m = query.Mark
            _sm = query.SMark

            Return CType(_procT, ProviderT(Of CreateType, ReturnType))
        End Function

        Public Function ExecSimple(Of CreatedType As {New, _ICachedEntity}, ReturnType)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As System.Collections.Generic.IList(Of ReturnType) Implements IExecutor.ExecSimple
            Dim p As Provider(Of CreatedType) = GetProcessor(Of CreatedType)(mgr, query)

            Return p.GetSimpleValues(Of ReturnType)()
        End Function

        'Private Shared Function _GetCe(Of ReturnType As _ICachedEntity)( _
        '    ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal p As ProcessorBase(Of ReturnType), ByVal dic As IDictionary, ByVal id As String, ByVal sync As String) As Worm.OrmManager.CachedItem
        '    Return mgr.GetFromCache(Of ReturnType)(dic, sync, id, query.propWithLoad, p)
        'End Function

        'Private Shared Function d2(Of ReturnType As _IEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal p As ProcessorBase(Of ReturnType), ByVal ce As OrmManager.CachedItem, ByVal s As Cache.IListObjectConverter.ExtractListResult) As Worm.ReadOnlyObjectList(Of ReturnType)
        '    Return ce.GetObjectList(Of ReturnType)(mgr, query.propWithLoad, p.Created, s)
        'End Function

        Private Delegate Function GetCeDelegate( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal dic As IDictionary, ByVal id As String, ByVal sync As String, ByVal p2 As OrmManager.ICacheItemProvoderBase) As Worm.OrmManager.CachedItem

        Private Delegate Function GetListFromCEDelegate(Of ReturnType As _IEntity)( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal p As OrmManager.ICacheItemProvoderBase, ByVal ce As OrmManager.CachedItem, ByVal s As Cache.IListObjectConverter.ExtractListResult, ByVal created As Boolean) As Worm.ReadOnlyObjectList(Of ReturnType)

        Private Delegate Function GetProcessorDelegate(Of ReturnType As _IEntity)() As ProviderAnonym(Of ReturnType)

        Private Sub SetSchema4Object(ByVal o As ICachedEntity, ByVal mgr As OrmManager)
            CType(o, _ICachedEntity).SetSpecificSchema(mgr.MappingEngine)
        End Sub

        Private Function _Exec(Of ReturnType As _IEntity)(ByVal mgr As OrmManager, _
            ByVal query As QueryCmd, ByVal gp As GetProcessorDelegate(Of ReturnType), _
            ByVal d As GetCeDelegate, ByVal d2 As GetListFromCEDelegate(Of ReturnType)) As ReadOnlyObjectList(Of ReturnType)

            Dim dbm As OrmReadOnlyDBManager = CType(mgr, OrmReadOnlyDBManager)

            Dim key As String = Nothing
            Dim dic As IDictionary = Nothing
            Dim id As String = Nothing
            Dim sync As String = Nothing
            Dim oldExp As Date
            Dim oldList As String = Nothing

            Dim oldCache As Boolean = mgr._dont_cache_lists
            Dim oldStart As Integer = mgr._start
            Dim oldLength As Integer = mgr._length
            Dim oldSchema As ObjectMappingEngine = mgr.MappingEngine
            Dim timeout As Nullable(Of Integer) = dbm.CommandTimeout
            Dim oldC As Boolean = mgr.RaiseObjectCreation

            If query.ClientPaging IsNot Nothing Then
                mgr._start = query.ClientPaging.Start
                mgr._length = query.ClientPaging.Length
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
                mgr.RaiseObjectCreation = True
                mgr.SetSchema(query.Schema)
                AddHandler mgr.ObjectCreated, AddressOf SetSchema4Object
            End If

            If query.CommandTimed.HasValue Then
                dbm.CommandTimeout = query.CommandTimed
            End If

            Dim c As New QueryCmd.svct(query)
            Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)

                Dim p As ProviderAnonym(Of ReturnType) = gp()

                mgr._dont_cache_lists = query.DontCache OrElse query.OuterQuery IsNot Nothing OrElse p.Dic Is Nothing

                If Not mgr._dont_cache_lists Then
                    key = p.Key
                    id = p.Id
                    dic = p.Dic
                    sync = p.Sync
                End If

                'Debug.WriteLine(key)
                'Debug.WriteLine(query.Mark)
                'Debug.WriteLine(query.SMark)

                Dim oldLoad As Boolean = query._load
                Dim created As Boolean = True
                If query.ClientPaging IsNot Nothing Then
                    query._load = False
                    created = False
                End If

                Dim ce As OrmManager.CachedItem = d(mgr, query, dic, id, sync, p)
                p.Clear()

                query._load = oldLoad

                query.LastExecitionResult = mgr.GetLastExecitionResult

                mgr.RaiseOnDataAvailable()

                Dim s As Cache.IListObjectConverter.ExtractListResult
                'Dim r As ReadOnlyList(Of ReturnType) = ce.GetObjectList(Of ReturnType)(mgr, query.WithLoad, p.Created, s)
                'Return r
                Dim res As ReadOnlyObjectList(Of ReturnType) = d2(mgr, query, p, ce, s, p.Created AndAlso created)

                mgr._dont_cache_lists = oldCache
                mgr._start = oldStart
                mgr._length = oldLength
                mgr._list = oldList
                mgr._expiresPattern = oldExp
                mgr.SetSchema(oldSchema)
                RemoveHandler mgr.ObjectCreated, AddressOf SetSchema4Object
                dbm.CommandTimeout = timeout
                mgr.RaiseObjectCreation = oldC

                Return res
            End Using
        End Function

        Public Function Exec(Of ReturnType As {_ICachedEntity})(ByVal mgr As OrmManager, _
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

            'Dim ce As OrmManager.CachedItem = mgr.GetFromCache(Of ReturnType)(dic, sync, id, query.propWithLoad, p)
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

            'Dim p As Processor(Of ReturnType) = GetProcessor(Of ReturnType)(mgr, query)

            Return CType(_Exec(Of ReturnType)(mgr, query, _
                Function() GetProcessor(Of ReturnType)(mgr, query), _
                Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                    m.GetFromCache(Of ReturnType)(dic, sync, id, q.propWithLoad, p2), _
                Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As OrmManager.CachedItem, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
                    ce.GetObjectList(Of ReturnType)(m, q.propWithLoad, created, s) _
                ), ReadOnlyEntityList(Of ReturnType))
        End Function

        Public Function Exec(Of SelectType As {_ICachedEntity, New}, ReturnType As {_ICachedEntity})(ByVal mgr As OrmManager, _
            ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType) Implements IExecutor.Exec

            'Dim p As ProcessorT(Of SelectType, ReturnType) = GetProcessorT(Of SelectType, ReturnType)(mgr, query)

            Return CType(_Exec(Of ReturnType)(mgr, query, _
                Function() GetProcessorT(Of SelectType, ReturnType)(mgr, query), _
                Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                    m.GetFromCache(Of ReturnType)(dic, sync, id, q.propWithLoad, p2), _
                Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As OrmManager.CachedItem, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
                    ce.GetObjectList(Of ReturnType)(m, q.propWithLoad, created, s) _
                ), ReadOnlyEntityList(Of ReturnType))
        End Function

        Public Function ExecEntity(Of ReturnType As {Orm._IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType) Implements IExecutor.ExecEntity
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

            'query.LastExecitionResult = New OrmManager.ExecutionResult(r.Count, e, f, _hit, mgr._loadedInLastFetch)

            'mgr._dont_cache_lists = oldCache
            'mgr._start = oldStart
            'mgr._length = oldLength
            'mgr._list = oldList
            'mgr._expiresPattern = oldExp
            'mgr._schema = oldSchema

            'mgr.RaiseOnDataAvailable()

            'Return r

            'Dim p As ProcessorBase(Of ReturnType) = GetProcessorAnonym(Of ReturnType)(mgr, query)

            Return _Exec(Of ReturnType)(mgr, query, _
                Function() GetProcessorAnonym(Of ReturnType)(mgr, query), _
                Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                    m.GetFromCache2(Of ReturnType)(dic, sync, id, q.propWithLoad, p2), _
                Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As OrmManager.CachedItem, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
                    CType(ce.Obj, Global.Worm.ReadOnlyObjectList(Of ReturnType)))
        End Function

        Public Function ExecEntity(Of CreateType As {New, _IEntity}, ReturnType As {Orm._IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType) Implements IExecutor.ExecEntity
            Return _Exec(Of ReturnType)(mgr, query, _
               Function() GetProcessorAnonym(Of CreateType, ReturnType)(mgr, query), _
               Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                   m.GetFromCache2(Of ReturnType)(dic, sync, id, q.propWithLoad, p2), _
               Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As OrmManager.CachedItem, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
                   CType(ce.Obj, Global.Worm.ReadOnlyObjectList(Of ReturnType)))
        End Function

#Region " Shared helpers "

        Protected Shared Function GetFields(ByVal gen As ObjectMappingEngine, ByVal selectType As Type, _
            ByVal q As QueryCmd, ByVal withLoad As Boolean, ByVal c As IList(Of SelectExpression)) As List(Of ColumnAttribute)

            Dim l As List(Of ColumnAttribute) = Nothing
            If c IsNot Nothing Then
                l = New List(Of ColumnAttribute)
                For Each p As SelectExpression In c
                    'If Not type.Equals(p.Type) Then
                    '    Throw New NotImplementedException
                    'End If
                    If p.Type Is Nothing Then
                        Dim f As String = p.PropertyAlias
                        If Not String.IsNullOrEmpty(p.Computed) Then
                            f = p.Column
                        End If

                        If String.IsNullOrEmpty(f) Then
                            Throw New InvalidOperationException(String.Format("Column {0} must have a field", p.Column))
                        End If

                        Dim cl As ColumnAttribute = If(selectType IsNot Nothing, gen.GetColumnByFieldName(selectType, f), Nothing)

                        If cl Is Nothing Then
                            cl = New ColumnAttribute
                            cl.PropertyAlias = f
                        End If
                        cl.Column = p.Column
                        l.Add(cl)
                    Else
                        Dim cl As ColumnAttribute = gen.GetColumnByFieldName(p.Type, p.PropertyAlias)
                        If cl Is Nothing Then
                            cl = gen.GetColumnByFieldName(selectType, p.PropertyAlias)
                        End If

                        If cl Is Nothing Then
                            Throw New InvalidOperationException(String.Format("Column {0} must have a field", p.Column))
                        End If

                        l.Add(cl)
                    End If
                Next

                'If type IsNot Nothing Then
                '    For Each pk As ColumnAttribute In gen.GetPrimaryKeys(type)
                '        If Not l.Contains(pk) Then
                '            l.Add(pk)
                '        End If
                '    Next
                'End If
                'l.Sort()
            ElseIf selectType IsNot Nothing Then
                If withLoad Then
                    l = gen.GetSortedFieldList(selectType)
                Else
                    l = gen.GetPrimaryKeys(selectType)
                End If
            End If

            If q.Aggregates IsNot Nothing Then
                For Each p As AggregateBase In q.Aggregates
                    Dim cl As New ColumnAttribute
                    cl.PropertyAlias = p.Alias
                    cl.Column = p.Alias
                    l.Add(cl)
                Next
            End If
            Return l
        End Function

        Protected Shared Sub FormSelectList(ByVal query As QueryCmd, ByVal queryType As Type, _
            ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal os As IObjectSchemaBase, _
            ByVal almgr As IPrepareTable, ByVal filterInfo As Object, ByVal params As ICreateParam, _
            ByVal columnAliases As List(Of String), ByVal innerColumns As List(Of String), _
            ByVal withLoad As Boolean, ByVal selList As IEnumerable(Of SelectExpression))

            Dim b As Boolean
            Dim cols As New StringBuilder
            If os Is Nothing Then
                If selList IsNot Nothing Then
                    For Each p As SelectExpression In selList
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
                    If selList Is Nothing AndAlso query.Aggregates Is Nothing Then
                        cols.Append(s.GetSelectColumnList(queryType, Nothing, columnAliases, os))
                        sb.Append(cols.ToString)
                        b = True
                    ElseIf selList IsNot Nothing Then
                        'cols.Append(s.GetSelectColumnList(queryType, GetFields(s, queryType, query, withLoad), columnAliases, os))
                        cols.Append(s.GetSelectColumns(selList, columnAliases))
                        sb.Append(cols.ToString)
                        b = True
                    End If
                ElseIf selList IsNot Nothing Then
                    For Each p As SelectExpression In selList
                        Dim map As MapField2Column = os.GetFieldColumnMap()(p.PropertyAlias)
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
                    sb.Append(a.MakeStmt(s, innerColumns, params, almgr, filterInfo))
                    If columnAliases IsNot Nothing Then
                        columnAliases.Add(a.GetAlias)
                    End If
                Next
            End If

            If Not b Then
                If os IsNot Nothing Then
                    Throw New NotSupportedException("Select columns must be specified")
                End If
                sb.Append("*")
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

        Protected Delegate Function Func(Of T)() As T

        Protected Shared Function FormatSearchTable(ByVal sb As StringBuilder, ByVal st As SearchFragment, _
            ByVal s As SQLGenerator, ByVal os As IObjectSchemaBase, ByVal params As ICreateParam, ByVal selectType As Type) As Boolean

            If os Is Nothing Then
                os = s.GetObjectSchema(If(st.Type Is Nothing, selectType, st.Type))
            End If

            Dim searchTable As SourceFragment = os.Table
            If st.QueryFields IsNot Nothing AndAlso st.QueryFields.Length = 1 Then
                searchTable = os.GetFieldColumnMap(st.QueryFields(0))._tableName
            End If

            Dim table As String = st.GetSearchTableName
            Dim pname As String = params.CreateParam(st.SearchText)
            Dim appendMain As Boolean

            sb.Append(table).Append("(")
            Dim tf As ITableFunction = TryCast(searchTable, ITableFunction)
            If tf Is Nothing Then
                sb.Append(s.GetTableName(searchTable))
            Else
                sb.Append(tf.GetRealTable)
                appendMain = True
            End If
            sb.Append(",")
            If st.QueryFields Is Nothing OrElse st.QueryFields.Length = 0 Then
                sb.Append("*")
            Else
                sb.Append("(")
                For Each f As String In st.QueryFields
                    Dim m As MapField2Column = os.GetFieldColumnMap(f)
                    sb.Append(m._columnName).Append(",")
                Next
                sb.Length -= 1
                sb.Append(")")
            End If
            sb.Append(",")
            sb.Append(pname)
            If st.Top <> Integer.MinValue Then
                sb.Append(",").Append(st.Top)
            End If
            sb.Append(")")

            Return appendMain
        End Function

        Protected Shared Function FormTypeTables(ByVal filterInfo As Object, ByVal params As ICreateParam, _
            ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal s As SQLGenerator, _
            ByVal os As IObjectSchemaBase, ByVal tables() As SourceFragment, _
            ByVal filter As IFilter, ByVal selectType As Type, ByVal appendMain As Boolean?, _
            Optional ByVal apd As Func(Of String) = Nothing) As Pair(Of SourceFragment, String)

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
            'Dim appendMain As Boolean

            Dim st As SearchFragment = TryCast(tbl_real, SearchFragment)
            If st IsNot Nothing Then
                appendMain = appendMain OrElse FormatSearchTable(sb, st, s, os, params, selectType)
            Else
                sb.Append(s.GetTableName(tbl_real))
            End If

            sb.Append(" ").Append([alias])
            If apd IsNot Nothing Then
                sb.Append(apd())
            End If

            Dim pk As Pair(Of SourceFragment, String) = Nothing

            If st IsNot Nothing Then
                Dim stt As Type = st.Type
                If stt Is Nothing Then
                    stt = selectType
                End If

                If Not appendMain.HasValue AndAlso filter IsNot Nothing Then
                    For Each f As IFilter In filter.GetAllFilters
                        Dim ef As EntityFilterBase = TryCast(f, EntityFilterBase)
                        If ef IsNot Nothing Then
                            If ef.Template.Type IsNot Nothing Then
                                If ef.Template.Type Is stt Then
                                    appendMain = True
                                    Exit For
                                End If
                            Else
                                Dim t As Type = s.GetTypeByEntityName(ef.Template.EntityName)
                                If t Is stt Then
                                    appendMain = True
                                    Exit For
                                End If
                            End If
                        End If
                    Next
                End If

                If appendMain Then
                    Dim j As New OrmJoin(stt, Worm.Criteria.Joins.JoinType.Join, _
                        New JoinFilter(tbl_real, "[key]", stt, s.GetPrimaryKeys(stt, os)(0).PropertyAlias, _
                                       Criteria.FilterOperation.Equal))

                    sb.Append(j.MakeSQLStmt(s, filterInfo, almgr, params))
                Else
                    pk = New Pair(Of SourceFragment, String)(tbl_real, "[key]")
                End If
            End If

            Dim fs As IMultiTableObjectSchema = TryCast(os, IMultiTableObjectSchema)

            If fs IsNot Nothing Then
                For j As Integer = 1 To tables.Length - 1
                    Dim join As OrmJoin = CType(s.GetJoins(fs, tbl, tables(j), filterInfo), OrmJoin)

                    If Not OrmJoin.IsEmpty(join) Then
                        If Not almgr.Aliases.ContainsKey(tables(j)) Then
                            almgr.AddTable(tables(j), params)
                        End If
                        sb.Append(join.MakeSQLStmt(s, filterInfo, almgr, params))
                    End If
                Next
            End If

            Return pk
        End Function

        Protected Shared Sub FormJoins(ByVal filterInfo As Object, ByVal query As QueryCmd, _
            ByVal params As ICreateParam, _
            ByVal j As List(Of Worm.Criteria.Joins.OrmJoin), ByVal almgr As IPrepareTable, _
            ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal selectType As Type, _
            ByVal pk As Pair(Of SourceFragment, String), ByVal filter As IFilter)
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

                    If pk IsNot Nothing AndAlso join.Condition IsNot Nothing Then
                        join.InjectJoinFilter(selectType, OrmBaseT.PKName, pk.First, pk.Second)
                    End If

                    If join.Table Is Nothing Then
                        Dim t As Type = join.Type
                        If t Is Nothing Then
                            t = s.GetTypeByEntityName(join.EntityName)
                        End If

                        Dim oschema As IObjectSchemaBase = s.GetObjectSchema(t)
                        Dim tables() As SourceFragment
                        Dim fs As IMultiTableObjectSchema = TryCast(oschema, IMultiTableObjectSchema)
                        If fs Is Nothing Then
                            tables = New SourceFragment() {oschema.Table}
                        Else
                            tables = fs.GetTables()
                        End If

                        Dim needAppend As Boolean = True
                        Dim cond As IFilter = join.Condition

                        If cond Is Nothing AndAlso (join.M2MJoinType IsNot Nothing OrElse join.M2MJoinEntityName IsNot Nothing) Then
                            Dim t2 As Type = join.M2MJoinType
                            If t2 Is Nothing Then
                                t2 = s.GetTypeByEntityName(join.M2MJoinEntityName)
                            End If

                            Dim oschema2 As IObjectSchemaBase = s.GetObjectSchema(t2)

                            Dim t12t2 As Orm.Meta.M2MRelation = s.GetM2MRelation(t, oschema, t2, join.M2MKey)
                            Dim t22t1 As Orm.Meta.M2MRelation = s.GetM2MRelation(t2, oschema2, t, join.M2MKey)

                            Dim t2_pk As String = s.GetPrimaryKeys(t2)(0).PropertyAlias
                            Dim t1_pk As String = s.GetPrimaryKeys(t)(0).PropertyAlias

                            'Dim jl As JoinLink = JCtor.Join(t22t1.Table).On(t22t1.Table, t22t1.Column).Eq(t, t1_pk)
                            Dim jl As JoinLink = Nothing
                            If pk IsNot Nothing Then
                                jl = JCtor.Join(t22t1.Table).On(t22t1.Table, t12t2.Column).Eq(pk.First, pk.Second)
                            Else
                                jl = JCtor.Join(t22t1.Table).On(t22t1.Table, t12t2.Column).Eq(t2, t2_pk)
                            End If

                            If almgr.Aliases.ContainsKey(oschema.Table) Then
                                jl.And(t22t1.Table, t22t1.Column).Eq(t, t1_pk)
                                needAppend = False
                            Else
                                cond = JoinCondition.Create(t22t1.Table, t22t1.Column).Eq(t, t1_pk).Filter
                            End If

                            Dim js() As OrmJoin = jl

                            sb.Append(js(0).MakeSQLStmt(s, filterInfo, almgr, params))
                        End If

                        If needAppend Then
                            sb.Append(join.JoinTypeString())

                            FormTypeTables(filterInfo, params, almgr, sb, s, oschema, tables, filter, selectType, query.AppendMain, _
                                           Function() " on " & cond.MakeQueryStmt(s, filterInfo, almgr, params, Nothing))
                        End If
                        'tbl = s.GetTables(t)(0)
                    Else
                        sb.Append(join.MakeSQLStmt(s, filterInfo, almgr, params))
                        almgr.Replace(s, join.Table, sb)
                    End If
                End If
            Next
        End Sub

        Protected Shared Sub FormGroupBy(ByVal query As QueryCmd, ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal selectType As Type)
            If query.Group IsNot Nothing Then
                sb.Append(" group by ")
                For Each g As SelectExpression In query.Group
                    If g.Table IsNot Nothing Then
                        sb.Append(almgr.Aliases(g.Table)).Append(".").Append(g.Column)
                    Else
                        If Not String.IsNullOrEmpty(g.Computed) Then
                            sb.Append(String.Format(g.Computed, ObjectMappingEngine.ExtractValues(s, almgr.Aliases, g.Values).ToArray))
                        Else
                            Dim t As Type = g.Type
                            If t Is Nothing Then
                                t = selectType
                            End If
                            Dim schema As IObjectSchemaBase = s.GetObjectSchema(t)
                            Dim cm As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()
                            Dim map As MapField2Column = Nothing
                            If cm.TryGetValue(g.PropertyAlias, map) Then
                                sb.Append(almgr.Aliases(map._tableName)).Append(".").Append(map._columnName)
                            Else
                                Throw New ArgumentException(String.Format("Field {0} of type {1} is not defined", g.PropertyAlias, g.Type))
                            End If
                        End If
                    End If
                    sb.Append(",")
                Next
                sb.Length -= 1
            End If
        End Sub

        Protected Shared Sub FormOrderBy(ByVal query As QueryCmd, ByVal t As Type, _
            ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal filterInfo As Object, _
            ByVal params As ICreateParam, ByVal columnAliases As List(Of String))
            If query.propSort IsNot Nothing AndAlso Not query.propSort.IsExternal Then
                s.CreateSelectExpressionFormater().Format(query.propSort, sb, s, t, almgr, params, columnAliases, filterInfo, query.SelectList, query.Table)
                'Dim adv As DbSort = TryCast(query.propSort, DbSort)
                'If adv IsNot Nothing Then
                '    adv.MakeStmt(s, almgr, columnAliases, sb, t, filterInfo, params)
                'Else
                '    s.AppendOrder(t, query.propSort, almgr, sb, True, query.SelectList, query.Table)
                'End If
            End If
        End Sub

        Public Shared Function MakeQueryStatement(ByVal filterInfo As Object, ByVal schema As SQLGenerator, _
            ByVal query As QueryCmd, ByVal params As ICreateParam, ByVal queryType As Type, _
            ByVal joins As List(Of Worm.Criteria.Joins.OrmJoin), ByVal f As IFilter, ByVal almgr As IPrepareTable, ByVal selList As IEnumerable(Of SelectExpression)) As String

            Return MakeQueryStatement(filterInfo, schema, query, params, queryType, joins, f, almgr, Nothing, Nothing, Nothing, 0, query.propWithLoad, selList)
        End Function

        Public Shared Function MakeQueryStatement(ByVal filterInfo As Object, ByVal schema As SQLGenerator, _
            ByVal query As QueryCmd, ByVal params As ICreateParam, ByVal selectType As Type, _
            ByVal joins As List(Of Worm.Criteria.Joins.OrmJoin), ByVal f As IFilter, ByVal almgr As IPrepareTable, _
            ByVal columnAliases As List(Of String), ByVal inner As String, ByVal innerColumns As List(Of String), _
            ByVal i As Integer, ByVal withLoad As Boolean, ByVal selList As IEnumerable(Of SelectExpression)) As String

            Dim sb As New StringBuilder
            Dim s As SQLGenerator = schema
            Dim os As IObjectSchemaBase = Nothing

            If query.Table Is Nothing Then
                os = s.GetObjectSchema(selectType)
            End If

            sb.Append("select ")

            If query.propDistinct Then
                sb.Append("distinct ")
            End If

            If query.propTop IsNot Nothing Then
                sb.Append(s.TopStatement(query.propTop.Count, query.propTop.Percent, query.propTop.Ties)).Append(" ")
            End If

            FormSelectList(query, selectType, sb, s, os, almgr, filterInfo, params, columnAliases, innerColumns, withLoad, selList)

            sb.Append(" from ")

            If Not String.IsNullOrEmpty(inner) Then
                sb.Append("(").Append(inner).Append(") as src_t").Append(i)
            Else

                Dim tables() As SourceFragment = Nothing
                If os IsNot Nothing Then
                    Dim fs As IMultiTableObjectSchema = TryCast(os, IMultiTableObjectSchema)
                    If fs Is Nothing Then
                        tables = New SourceFragment() {os.Table}
                    Else
                        tables = fs.GetTables()
                    End If
                Else
                    tables = New SourceFragment() {query.Table}
                End If

                Dim newPK As Pair(Of SourceFragment, String) = FormTypeTables(filterInfo, params, almgr, sb, s, os, tables, f, selectType, query.AppendMain)

                FormJoins(filterInfo, query, params, joins, almgr, sb, s, selectType, newPK, f)
            End If

            s.AppendWhere(os, f, almgr, sb, filterInfo, params, innerColumns)

            FormGroupBy(query, almgr, sb, s, selectType)

            If query.RowNumberFilter Is Nothing Then
                FormOrderBy(query, selectType, almgr, sb, s, filterInfo, params, columnAliases)
            Else
                Dim r As New StringBuilder
                FormOrderBy(query, selectType, almgr, r, s, filterInfo, params, columnAliases)
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

        Public Function ExecSimple(Of ReturnType)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As System.Collections.Generic.IList(Of ReturnType) Implements IExecutor.ExecSimple
            Dim ts() As Reflection.MemberInfo = Me.GetType.GetMember("ExecSimple")
            For Each t As Reflection.MethodInfo In ts
                If t.IsGenericMethod AndAlso t.GetGenericArguments.Length = 2 Then
                    t = t.MakeGenericMethod(New Type() {query.SelectedType, GetType(ReturnType)})
                    Return CType(t.Invoke(Me, New Object() {mgr, query}), System.Collections.Generic.IList(Of ReturnType))
                End If
            Next
            Throw New InvalidOperationException
        End Function

        Public Sub Reset(Of ReturnType As _ICachedEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.Reset
            GetProcessor(Of ReturnType)(mgr, query).Renew = True
        End Sub

        Public Sub Reset(Of CreateType As {New, _ICachedEntity}, ReturnType As _ICachedEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.Reset
            GetProcessorT(Of CreateType, ReturnType)(mgr, query).Renew = True
        End Sub

        Public Sub ResetEntity(Of ReturnType As Orm._IEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.ResetEntity
            GetProcessorAnonym(Of ReturnType)(mgr, query).ResetCache()
        End Sub

        Public Sub ResetEntity(Of CreateType As {New, _IEntity}, ReturnType As Orm._IEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.ResetEntity
            GetProcessorAnonym(Of CreateType, ReturnType)(mgr, query).ResetCache()
        End Sub
    End Class

End Namespace