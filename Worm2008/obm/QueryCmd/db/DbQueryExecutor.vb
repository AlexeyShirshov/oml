Imports Worm.Database
Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.Criteria.Joins
Imports Worm.Query.QueryCmd
Imports Worm.Cache

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

        Private _proc As BaseProvider
        'Private _procT As BaseProvider
        'Private _procA As BaseProvider
        'Private _procAT As BaseProvider
        'Private _procS As BaseProvider
        'Private _procSM As BaseProvider

        Public Event OnGetCacheItem(ByVal sender As IExecutor, ByVal args As IExecutor.GetCacheItemEventArgs) Implements IExecutor.OnGetCacheItem
        Public Event OnRestoreDefaults(ByVal sender As IExecutor, ByVal mgr As OrmManager, ByVal args As EventArgs) Implements IExecutor.OnRestoreDefaults

#Region " Get providers "

        Protected Delegate Function InitTypesDelegate(ByVal mgr As OrmManager, ByVal query As QueryCmd) As Boolean

        Protected Function GetProvider(ByVal mgr As OrmManager, ByVal query As QueryCmd, _
            ByVal d As InitTypesDelegate) As BaseProvider
            If _proc Is Nothing Then
                Dim j As New List(Of List(Of Worm.Criteria.Joins.QueryJoin))
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
                If d Is Nothing Then
                    If query.NeedSelectType(mgr.MappingEngine) Then
                        Throw New ExecutorException("Cannot get provider")
                    End If
                Else
                    r = d(mgr, query)
                End If

                'Dim ct As EntityUnion = query.CreateType
                'Dim types As ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?)) = Nothing
                'If query.SelectClause IsNot Nothing AndAlso query.SelectClause.SelectTypes IsNot Nothing Then
                '    types = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(query.SelectClause.SelectTypes)
                'End If
                'Dim f As FromClauseDef = query.FromClause

                QueryCmd.Prepare(query, Me, mgr.MappingEngine, mgr.GetContextFilter, mgr.StmtGenerator)
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
                    QueryCmd.Prepare(query, Me, mgr.MappingEngine, mgr.GetContextFilter, mgr.StmtGenerator)
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
                p.Created = False
            End If

            _proc.SetMark(query)

            Return _proc
        End Function

        Protected Shared Function InitTypes(ByVal mgr As OrmManager, ByVal query As QueryCmd, _
                                       ByVal type As Type) As Boolean
            Dim r As Boolean

            If Not type.IsInterface Then
                If query.CreateType Is Nothing Then
                    query.Into(type)
                End If
            End If

            If Not GetType(AnonymousEntity).IsAssignableFrom(type) Then
                If query.NeedSelectType(mgr.MappingEngine) Then
                    query.SelectInt(query.CreateType.AnyType, mgr.MappingEngine)
                    r = True
                End If
            End If

            If query.CreateType Is Nothing Then
                query.Into(query.GetSelectedType(mgr.MappingEngine))
            End If

            Return r
        End Function

        Protected Function GetProcessorAnonym(Of ReturnType As {_IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ProviderAnonym(Of ReturnType)
            'If _procA Is Nothing Then
            '    Dim j As New List(Of List(Of Worm.Criteria.Joins.QueryJoin))
            '    'If query.Joins IsNot Nothing Then
            '    '    j.AddRange(query.Joins)
            '    'End If

            '    'If query.SelectedType Is Nothing Then
            '    '    If String.IsNullOrEmpty(query.SelectedEntityName) Then
            '    '        'query.SelectedType = GetType(ReturnType)
            '    '        query.SelectedType = If(query.CreateType IsNot Nothing, query.CreateType, GetType(ReturnType))
            '    '    Else
            '    '        query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.SelectedEntityName)
            '    '    End If
            '    'End If

            '    'If GetType(AnonymousEntity).IsAssignableFrom(query.SelectedType) Then
            '    '    query.SelectedType = Nothing
            '    'End If

            '    InitTypes(mgr, query, GetType(ReturnType))

            '    QueryCmd.Prepare(query, Me, mgr.MappingEngine, mgr.GetContextFilter, mgr.StmtGenerator)
            '    'If query.Filter IsNot Nothing Then
            '    '    f = query.Filter.Filter(GetType(ReturnType))
            '    'End If

            '    'If query.AutoJoins Then
            '    '    Dim joins() As Worm.Criteria.Joins.OrmJoin = Nothing
            '    '    If mgr.HasJoins(GetType(ReturnType), f, query.Sort, joins) Then
            '    '        j.AddRange(joins)
            '    '    End If
            '    'End If

            '    'If query.Obj IsNot Nothing Then
            '    '    _proc = New M2MProcessor(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
            '    'Else
            '    _procA = New ProviderAnonym(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), query)
            '    'End If
            'Else
            '    Dim p As ProviderAnonym(Of ReturnType) = CType(_procA, ProviderAnonym(Of ReturnType))

            '    'If query.SelectedType Is Nothing Then
            '    '    If String.IsNullOrEmpty(query.SelectedEntityName) Then
            '    '        'query.SelectedType = GetType(ReturnType)
            '    '        query.SelectedType = If(query.CreateType IsNot Nothing, query.CreateType, GetType(ReturnType))
            '    '    Else
            '    '        query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.SelectedEntityName)
            '    '    End If
            '    'End If

            '    'If GetType(AnonymousEntity).IsAssignableFrom(query.SelectedType) Then
            '    '    query.SelectedType = Nothing
            '    'End If

            '    InitTypes(mgr, query, GetType(ReturnType))

            '    If _procA.QMark <> query.Mark Then
            '        QueryCmd.Prepare(query, Me, mgr.MappingEngine, mgr.GetContextFilter, mgr.StmtGenerator)
            '        p.Reset(mgr, query)
            '    Else
            '        p.Init(mgr, query)
            '        If _procA.QSMark <> query.SMark Then
            '            p.ResetStmt()
            '        End If
            '        If query._resDic Then
            '            p.ResetDic()
            '        End If
            '    End If
            'End If

            '_procA.SetMark(query)

            'Return CType(_procA, ProviderAnonym(Of ReturnType))
            Return New ProviderAnonym(Of ReturnType)(GetProvider(mgr, query, Function(m As OrmManager, q As QueryCmd) InitTypes(m, q, GetType(ReturnType))))
        End Function

        Protected Function GetProcessorAnonym(Of CreateType As {New, _IEntity}, ReturnType As {_IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ProviderAnonym(Of ReturnType)
            'If _procAT Is Nothing Then
            '    Dim j As New List(Of List(Of Worm.Criteria.Joins.QueryJoin))
            '    'If query.Joins IsNot Nothing Then
            '    '    j.AddRange(query.Joins)
            '    'End If

            '    'If query.SelectedType Is Nothing AndAlso Not String.IsNullOrEmpty(query.SelectedEntityName) Then
            '    '    query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.SelectedEntityName)
            '    'End If

            '    'If GetType(AnonymousEntity).IsAssignableFrom(query.SelectedType) Then
            '    '    query.SelectedType = Nothing
            '    'End If

            '    InitTypes(mgr, query, GetType(CreateType))

            '    QueryCmd.Prepare(query, Me, mgr.MappingEngine, mgr.GetContextFilter, mgr.StmtGenerator)
            '    'If query.Filter IsNot Nothing Then
            '    '    f = query.Filter.Filter(GetType(ReturnType))
            '    'End If

            '    'If query.AutoJoins Then
            '    '    Dim joins() As Worm.Criteria.Joins.OrmJoin = Nothing
            '    '    If mgr.HasJoins(GetType(ReturnType), f, query.Sort, joins) Then
            '    '        j.AddRange(joins)
            '    '    End If
            '    'End If

            '    'If query.Obj IsNot Nothing Then
            '    '    _proc = New M2MProcessor(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
            '    'Else
            '    _procAT = New ProviderAnonym(Of CreateType, ReturnType)(CType(mgr, OrmReadOnlyDBManager), query)
            '    'End If
            'Else
            '    Dim p As ProviderAnonym(Of CreateType, ReturnType) = CType(_procAT, ProviderAnonym(Of CreateType, ReturnType))

            '    'If query.SelectedType Is Nothing AndAlso Not String.IsNullOrEmpty(query.SelectedEntityName) Then
            '    '    query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.SelectedEntityName)
            '    'End If

            '    'If GetType(AnonymousEntity).IsAssignableFrom(query.SelectedType) Then
            '    '    query.SelectedType = Nothing
            '    'End If

            '    InitTypes(mgr, query, GetType(CreateType))

            '    If _procAT.QMark <> query.Mark Then
            '        QueryCmd.Prepare(query, Me, mgr.MappingEngine, mgr.GetContextFilter, mgr.StmtGenerator)
            '        p.Reset(mgr, query)
            '    Else
            '        p.Init(mgr, query)
            '        If _procAT.QSMark <> query.SMark Then
            '            p.ResetStmt()
            '        End If
            '        If query._resDic Then
            '            p.ResetDic()
            '        End If
            '    End If
            'End If

            '_procAT.SetMark(query)

            'Return CType(_procAT, ProviderAnonym(Of CreateType, ReturnType))
            Return New ProviderAnonym(Of CreateType, ReturnType)(GetProvider(mgr, query, Function(m As OrmManager, q As QueryCmd) InitTypes(m, q, GetType(CreateType))))
        End Function

        Protected Function GetProcessor(Of ReturnType As {ICachedEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As Provider(Of ReturnType)
            'If _proc Is Nothing Then
            '    Dim j As New List(Of List(Of Worm.Criteria.Joins.QueryJoin))
            '    'If query.Joins IsNot Nothing Then
            '    '    j.AddRange(query.Joins)
            '    'End If

            '    'If query.SelectedType Is Nothing Then
            '    '    If String.IsNullOrEmpty(query.SelectedEntityName) Then
            '    '        query.SelectedType = If(query.CreateType IsNot Nothing, query.CreateType, GetType(ReturnType))
            '    '    Else
            '    '        query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.SelectedEntityName)
            '    '    End If
            '    'End If

            '    InitTypes(mgr, query, GetType(ReturnType))

            '    QueryCmd.Prepare(query, Me, mgr.MappingEngine, mgr.GetContextFilter, mgr.StmtGenerator)
            '    'If query.Filter IsNot Nothing Then
            '    '    f = query.Filter.Filter(GetType(ReturnType))
            '    'End If

            '    'If query.AutoJoins Then
            '    '    Dim joins() As Worm.Criteria.Joins.OrmJoin = Nothing
            '    '    If mgr.HasJoins(GetType(ReturnType), f, query.Sort, joins) Then
            '    '        j.AddRange(joins)
            '    '    End If
            '    'End If

            '    'If query.Obj IsNot Nothing Then
            '    '    _proc = New M2MProcessor(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
            '    'Else
            '    _proc = New Provider(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), query)
            '    'End If
            'Else
            '    Dim p As Provider(Of ReturnType) = CType(_proc, Provider(Of ReturnType))

            '    'If query.SelectedType Is Nothing Then
            '    '    If String.IsNullOrEmpty(query.SelectedEntityName) Then
            '    '        query.SelectedType = If(query.CreateType IsNot Nothing, query.CreateType, GetType(ReturnType))
            '    '    Else
            '    '        query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.SelectedEntityName)
            '    '    End If
            '    'End If

            '    InitTypes(mgr, query, GetType(ReturnType))

            '    If _proc.QMark <> query.Mark Then
            '        query.Prepare(Me, mgr.MappingEngine, mgr.GetContextFilter, mgr.StmtGenerator)
            '        p.Reset(mgr, query)
            '    Else
            '        p.Init(mgr, query)
            '        If _proc.QSMark <> query.SMark Then
            '            p.ResetStmt()
            '        End If
            '        If query._resDic Then
            '            p.ResetDic()
            '        End If
            '    End If
            '    p.Created = False
            'End If

            '_proc.SetMark(query)

            'Return CType(_proc, Provider(Of ReturnType))
            Return New Provider(Of ReturnType)(GetProvider(mgr, query, Function(m As OrmManager, q As QueryCmd) InitTypes(m, q, GetType(ReturnType))))
        End Function

        Protected Function GetProcessorT(Of CreateType As {ICachedEntity, New}, ReturnType As {ICachedEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ProviderT(Of CreateType, ReturnType)
            'If _procT Is Nothing Then
            '    Dim j As New List(Of List(Of Worm.Criteria.Joins.QueryJoin))
            '    'If query.Joins IsNot Nothing Then
            '    '    j.AddRange(query.Joins)
            '    'End If

            '    'If query.SelectedType Is Nothing Then
            '    '    If query.Src Is Nothing Then
            '    '        If Not String.IsNullOrEmpty(query.SelectedEntityName) Then
            '    '            query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.SelectedEntityName)
            '    '        Else
            '    '            query.SelectedType = GetType(CreateType)
            '    '        End If
            '    '    Else
            '    '        query.SelectedType = query.Src.GetRealType(mgr.MappingEngine)
            '    '    End If
            '    'End If

            '    'If GetType(AnonymousCachedEntity).IsAssignableFrom(query.SelectedType) Then
            '    '    query.SelectedType = Nothing
            '    'End If

            '    InitTypes(mgr, query, GetType(CreateType))

            '    QueryCmd.Prepare(query, Me, mgr.MappingEngine, mgr.GetContextFilter, mgr.StmtGenerator)
            '    'If query.Filter IsNot Nothing Then
            '    '    f = query.Filter.Filter(GetType(ReturnType))
            '    'End If

            '    'If query.AutoJoins Then
            '    '    Dim joins() As Worm.Criteria.Joins.OrmJoin = Nothing
            '    '    If mgr.HasJoins(GetType(ReturnType), f, query.Sort, joins) Then
            '    '        j.AddRange(joins)
            '    '    End If
            '    'End If

            '    'If query.Obj IsNot Nothing Then
            '    '    _proc = New M2MProcessor(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
            '    'Else
            '    _procT = New ProviderT(Of CreateType, ReturnType)(CType(mgr, OrmReadOnlyDBManager), query)
            '    'End If
            'Else
            '    Dim p As Provider(Of ReturnType) = CType(_procT, Provider(Of ReturnType))

            '    'If query.SelectedType Is Nothing Then
            '    '    If query.Src Is Nothing Then
            '    '        If Not String.IsNullOrEmpty(query.SelectedEntityName) Then
            '    '            query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.SelectedEntityName)
            '    '        Else
            '    '            query.SelectedType = GetType(CreateType)
            '    '        End If
            '    '    Else
            '    '        query.SelectedType = query.Src.GetRealType(mgr.MappingEngine)
            '    '    End If
            '    'End If

            '    InitTypes(mgr, query, GetType(CreateType))

            '    If _procT.QMark <> query.Mark Then
            '        QueryCmd.Prepare(query, Me, mgr.MappingEngine, mgr.GetContextFilter, mgr.StmtGenerator)
            '        p.Reset(mgr, query)
            '    Else
            '        p.Init(mgr, query)
            '        If _procT.QSMark <> query.SMark Then
            '            p.ResetStmt()
            '        End If
            '        If query._resDic Then
            '            p.ResetDic()
            '        End If
            '    End If
            '    p.Created = False
            'End If

            '_procT.SetMark(query)

            'Return CType(_procT, ProviderT(Of CreateType, ReturnType))
            Return New ProviderT(Of CreateType, ReturnType)(GetProvider(mgr, query, Function(m As OrmManager, q As QueryCmd) InitTypes(m, q, GetType(CreateType))))
        End Function

        Protected Function GetProcessorS(Of T)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As BaseProvider
            'If _procSM Is Nothing Then
            '    Dim j As New List(Of List(Of Worm.Criteria.Joins.QueryJoin))
            '    'If query.Joins IsNot Nothing Then
            '    '    j.AddRange(query.Joins)
            '    'End If

            '    'If query.SelectedType Is Nothing Then
            '    '    If String.IsNullOrEmpty(query.SelectedEntityName) Then
            '    '        query.SelectedType = If(query.CreateType IsNot Nothing, query.CreateType, GetType(ReturnType))
            '    '    Else
            '    '        query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.SelectedEntityName)
            '    '    End If
            '    'End If

            '    'InitTypes(mgr,query,GetType(CreateType))

            '    QueryCmd.Prepare(query, Me, mgr.MappingEngine, mgr.GetContextFilter, mgr.StmtGenerator)
            '    'If query.Filter IsNot Nothing Then
            '    '    f = query.Filter.Filter(GetType(ReturnType))
            '    'End If

            '    'If query.AutoJoins Then
            '    '    Dim joins() As Worm.Criteria.Joins.OrmJoin = Nothing
            '    '    If mgr.HasJoins(GetType(ReturnType), f, query.Sort, joins) Then
            '    '        j.AddRange(joins)
            '    '    End If
            '    'End If

            '    'If query.Obj IsNot Nothing Then
            '    '    _proc = New M2MProcessor(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
            '    'Else
            '    _procSM = New SimpleProvider(Of T)(mgr, query)
            '    'End If
            'Else
            '    Dim p As BaseProvider = CType(_procSM, BaseProvider)

            '    'If query.SelectedType Is Nothing Then
            '    '    If String.IsNullOrEmpty(query.SelectedEntityName) Then
            '    '        query.SelectedType = If(query.CreateType IsNot Nothing, query.CreateType, GetType(ReturnType))
            '    '    Else
            '    '        query.SelectedType = mgr.MappingEngine.GetTypeByEntityName(query.SelectedEntityName)
            '    '    End If
            '    'End If

            '    'InitTypes(mgr,query,GetType(CreateType))

            '    If _procSM.QMark <> query.Mark Then
            '        QueryCmd.Prepare(query, Me, mgr.MappingEngine, mgr.GetContextFilter, mgr.StmtGenerator)
            '        p.Reset(mgr, query)
            '    Else
            '        p.Init(mgr, query)
            '        If _procSM.QSMark <> query.SMark Then
            '            p.ResetStmt()
            '        End If
            '        'If query._resDic Then
            '        '    p.ResetDic()
            '        'End If
            '    End If
            '    p.Created = False
            'End If

            '_procSM.SetMark(query)

            'Return CType(_procSM, BaseProvider)
            Return New SimpleProvider(Of T)(GetProvider(mgr, query, Nothing))
        End Function

        Protected Function GetProcessor(ByVal mgr As OrmManager, ByVal query As QueryCmd) As BaseProvider
            Return GetProvider(mgr, query, Nothing)
        End Function
#End Region

        Protected Delegate Function GetCeDelegate( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal dic As IDictionary, ByVal id As String, ByVal sync As String, ByVal p2 As OrmManager.ICacheItemProvoderBase) As Worm.Cache.CachedItemBase

        Protected Delegate Function GetListFromCEDelegate(Of ReturnType)( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal p As OrmManager.ICacheItemProvoderBase, _
            ByVal ce As Cache.CachedItemBase, ByVal s As Cache.IListObjectConverter.ExtractListResult, ByVal created As Boolean) As ReturnType

        Protected Delegate Function GetProcessorDelegate() As BaseProvider

        Private Sub SetSchema4Object(ByVal mgr As OrmManager, ByVal created As Boolean, ByVal o As IEntity)
            CType(o, _IEntity).SetSpecificSchema(mgr.MappingEngine)
        End Sub

        Private Sub SetSchema4Object(ByVal mgr As OrmManager, ByVal o As IEntity)
            CType(o, _IEntity).SetSpecificSchema(mgr.MappingEngine)
        End Sub

        Protected Function ExecBase(Of ReturnType)(ByVal mgr As OrmManager, _
            ByVal query As QueryCmd, ByVal gp As GetProcessorDelegate, _
            ByVal d As GetCeDelegate, ByVal d2 As GetListFromCEDelegate(Of ReturnType)) As ReturnType

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
            'Dim oldC As Boolean = mgr.RaiseObjectCreation

            If Not query.ClientPaging.IsEmpty Then
                mgr._start = query.ClientPaging.Start
                mgr._length = query.ClientPaging.Length
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

            If query.Schema IsNot Nothing Then
                'mgr.RaiseObjectCreation = True
                mgr.SetSchema(query.Schema)
                AddHandler mgr.ObjectLoaded, AddressOf SetSchema4Object
                AddHandler mgr.ObjectRestoredFromCache, AddressOf SetSchema4Object
            End If

            Dim c As New QueryCmd.svct(query)
            Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)

                Dim p As BaseProvider = gp()

                mgr._dont_cache_lists = query.DontCache OrElse query.HasInnerQuery OrElse p.Dic Is Nothing OrElse query.IsFTS

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

                Dim ce As Cache.CachedItemBase = d(mgr, query, dic, id, sync, p)
                p.Clear()
                _proc.Clear()
                _proc.SetDependency(p)
                Dim args As New IExecutor.GetCacheItemEventArgs
                RaiseEvent OnGetCacheItem(Me, args)
                Dim created As Boolean = args.Created
                'query._load = oldLoad

                query.LastExecutionResult = mgr.GetLastExecutionResult

                mgr.RaiseOnDataAvailable()

                Dim s As Cache.IListObjectConverter.ExtractListResult
                'Dim r As ReadOnlyList(Of ReturnType) = ce.GetObjectList(Of ReturnType)(mgr, query.WithLoad, p.Created, s)
                'Return r
                Dim res As ReturnType = d2(mgr, query, p, ce, s, p.Created AndAlso created)

                mgr._dont_cache_lists = oldCache
                mgr._start = oldStart
                mgr._length = oldLength
                mgr._list = oldList
                mgr._expiresPattern = oldExp
                mgr.SetSchema(oldSchema)
                RaiseEvent OnRestoreDefaults(Me, mgr, EventArgs.Empty)

                RemoveHandler mgr.ObjectLoaded, AddressOf SetSchema4Object
                RemoveHandler mgr.ObjectRestoredFromCache, AddressOf SetSchema4Object
                'mgr.RaiseObjectCreation = oldC

                Return res
            End Using
        End Function

        Private Function _Exec(Of ReturnType)(ByVal mgr As OrmManager, _
            ByVal query As QueryCmd, ByVal gp As GetProcessorDelegate, _
            ByVal d As GetCeDelegate, ByVal d2 As GetListFromCEDelegate(Of ReturnType)) As ReturnType

            Dim dbm As OrmReadOnlyDBManager = CType(mgr, OrmReadOnlyDBManager)
            Dim timeout As Nullable(Of Integer) = dbm.CommandTimeout

            If query.CommandTimed.HasValue Then
                dbm.CommandTimeout = query.CommandTimed
            End If

            Dim res As ReturnType = ExecBase(Of ReturnType)(mgr, query, gp, d, d2)

            dbm.CommandTimeout = timeout

            Return res
        End Function

        Public Function Exec(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadonlyMatrix Implements IExecutor.Exec
            Return _Exec(Of ReadonlyMatrix)(mgr, query, _
                Function() GetProcessor(mgr, query), _
                Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                    m.GetFromCacheBase(dic, sync, id, Nothing, p2, Nothing), _
                Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
                    ce.GetMatrix(m, q.propWithLoads, created, m.GetStart, m.GetLength, s) _
                )
        End Function

        Public Function ExecSimple(Of ReturnType)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As IList(Of ReturnType) Implements IExecutor.ExecSimple
            'Dim ts() As Reflection.MemberInfo = Me.GetType.GetMember("ExecSimple")
            'For Each t As Reflection.MethodInfo In ts
            '    If t.IsGenericMethod AndAlso t.GetGenericArguments.Length = 2 Then
            '        t = t.MakeGenericMethod(New Type() {query.SelectedType, GetType(ReturnType)})
            '        Return CType(t.Invoke(Me, New Object() {mgr, query}), System.Collections.Generic.IList(Of ReturnType))
            '    End If
            'Next
            'Throw New InvalidOperationException
            'Dim p As BaseProvider = GetProcessor(mgr, query)

            'Return p.GetSimpleValues(Of ReturnType)()
            Dim olds As Boolean = query.CacheSort
            query.CacheSort = True
            Try
                Return _Exec(mgr, query, Function() GetProcessorS(Of ReturnType)(mgr, query), _
                                Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                                    m.GetFromCacheBase(dic, sync, id, Nothing, p2, Nothing), _
                                Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
                                    ce.GetObjectList(Of ReturnType)(mgr, m.GetStart, m.GetLength))
            Finally
                query._cacheSort = olds
            End Try
        End Function

        Public Function Exec(Of ReturnType As {_ICachedEntity})(ByVal mgr As OrmManager, _
            ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType) Implements IExecutor.Exec

            Return CType(_Exec(Of ReadOnlyEntityList(Of ReturnType))(mgr, query, _
                Function() GetProcessor(Of ReturnType)(mgr, query), _
                Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                    m.GetFromCache(Of ReturnType)(dic, sync, id, q.propWithLoad, p2), _
                Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
                    CType(ce, Cache.UpdatableCachedItem).GetObjectList(Of ReturnType)(m, q.propWithLoad, created, m.GetStart, m.GetLength, s) _
                ), ReadOnlyEntityList(Of ReturnType))
        End Function

        Public Function Exec(Of SelectType As {_ICachedEntity, New}, ReturnType As {_ICachedEntity})(ByVal mgr As OrmManager, _
            ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType) Implements IExecutor.Exec

            Return CType(_Exec(Of ReadOnlyEntityList(Of ReturnType))(mgr, query, _
                Function() GetProcessorT(Of SelectType, ReturnType)(mgr, query), _
                Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                    m.GetFromCache(Of ReturnType)(dic, sync, id, q.propWithLoad, p2), _
                Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
                    CType(ce, Cache.UpdatableCachedItem).GetObjectList(Of ReturnType)(m, q.propWithLoad, created, m.GetStart, m.GetLength, s) _
                ), ReadOnlyEntityList(Of ReturnType))
        End Function

        Private Function _ExecEntity(Of ReturnType As {Entities._IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd, _
            ByVal d As GetProcessorDelegate) As ReadOnlyObjectList(Of ReturnType)

            Return _Exec(Of ReadOnlyObjectList(Of ReturnType))(mgr, query, d, _
                Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                    m.GetFromCache2(dic, sync, id, True, p2), _
                Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
                   CType(ce.GetObjectList(Of ReturnType)(m, True, created, m.GetStart, m.GetLength, s), Global.Worm.ReadOnlyObjectList(Of ReturnType)))
        End Function

        Public Function ExecEntity(Of ReturnType As {Entities._IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType) Implements IExecutor.ExecEntity
            Return _ExecEntity(Of ReturnType)(mgr, query, _
                Function() GetProcessorAnonym(Of ReturnType)(mgr, query))
            'Return _Exec(Of ReturnType)(mgr, query, _
            '    Function() GetProcessorAnonym(Of ReturnType)(mgr, query), _
            '    Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
            '        m.GetFromCache2(dic, sync, id, q.propWithLoad, p2), _
            '    Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
            '       CType(ce.GetObjectList(Of ReturnType)(m, q.propWithLoad, created, m.GetStart, m.GetLength, s), Global.Worm.ReadOnlyObjectList(Of ReturnType)))
        End Function

        Public Function ExecEntity(Of CreateType As {New, _IEntity}, ReturnType As {Entities._IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType) Implements IExecutor.ExecEntity
            Return _ExecEntity(Of ReturnType)(mgr, query, _
                Function() GetProcessorAnonym(Of CreateType, ReturnType)(mgr, query))
            'Return _Exec(Of ReturnType)(mgr, query, _
            '   Function() GetProcessorAnonym(Of CreateType, ReturnType)(mgr, query), _
            '   Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
            '       m.GetFromCache2(dic, sync, id, q.propWithLoad, p2), _
            '   Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
            '       CType(ce.GetObjectList(Of ReturnType)(m, q.propWithLoad, created, m.GetStart, m.GetLength, s), Global.Worm.ReadOnlyObjectList(Of ReturnType)))
        End Function

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

        Protected Shared Sub FormSelectList(ByVal mpe As ObjectMappingEngine, ByVal query As QueryCmd, _
            ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal os As IEntitySchema, ByVal selectedType As Type, _
            ByVal almgr As IPrepareTable, ByVal filterInfo As Object, ByVal params As ICreateParam, _
            ByVal selList As IEnumerable(Of SelectExpression), ByVal defaultTable As SourceFragment)

            Dim b As Boolean
            Dim cols As New StringBuilder

            For Each p As SelectExpression In selList
                s.CreateSelectExpressionFormater().Format(p, cols, mpe, almgr, params, _
                    filterInfo, Nothing, defaultTable, os, True)
                cols.Append(", ")
            Next
            cols.Length -= 2
            sb.Append(cols.ToString)
            b = True

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

        Protected Shared Sub ReplaceSelectList(ByVal mpe As ObjectMappingEngine, ByVal query As QueryCmd, _
            ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal os As IEntitySchema, _
            ByVal almgr As IPrepareTable, ByVal filterInfo As Object, ByVal params As ICreateParam, _
            ByVal selList As IEnumerable(Of SelectExpression))

            Dim tbl As SourceFragment = Nothing
            If query.FromClause IsNot Nothing Then
                tbl = query.FromClause.Table
            End If

            For Each p As SelectExpression In selList
                If Not String.IsNullOrEmpty(p._tempMark) Then
                    s.CreateSelectExpressionFormater().Format(p, sb, mpe, almgr, params, _
                        filterInfo, Nothing, tbl, os, True)
                End If
            Next
        End Sub

        Protected Delegate Function Func(Of T)() As T

        Protected Shared Function FormatSearchTable(ByVal mpe As ObjectMappingEngine, ByVal sb As StringBuilder, ByVal st As SearchFragment, _
            ByVal s As SQLGenerator, ByVal os As IEntitySchema, ByVal params As ICreateParam, ByVal selectType As Type) As Boolean

            If os Is Nothing Then
                os = mpe.GetEntitySchema(If(st.Type Is Nothing, selectType, st.Type))
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

        Protected Shared Function FormTypeTables(ByVal mpe As ObjectMappingEngine, ByVal filterInfo As Object, ByVal params As ICreateParam, _
            ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal s As SQLGenerator, _
            ByVal os As IEntitySchema, ByVal osrc As EntityUnion, _
            ByVal filter As IFilter, ByVal from As QueryCmd.FromClauseDef, ByVal appendMain As Boolean?, _
            ByVal apd As Func(Of String)) As Pair(Of SourceFragment, String)

            Dim tables() As SourceFragment = Nothing
            Dim osrc_ As EntityUnion = Nothing
            If from IsNot Nothing AndAlso from.ObjectSource IsNot Nothing Then
                osrc_ = from.ObjectSource
            ElseIf from Is Nothing Then
                osrc_ = osrc
            End If

            If from Is Nothing OrElse from.Table Is Nothing Then
                Dim mts As IMultiTableObjectSchema = TryCast(os, IMultiTableObjectSchema)
                If mts Is Nothing Then
                    tables = New SourceFragment() {os.Table}
                Else
                    tables = mts.GetTables()
                End If
            Else
                tables = New SourceFragment() {from.Table}
            End If

            'If tables.Length = 0 OrElse tables(0) Is Nothing Then
            '    Throw New QueryCmdException("Source table is not specified", Query)
            'End If

            Dim tbl As SourceFragment = tables(0)
            Dim tbl_real As SourceFragment = tbl
            Dim [alias] As String = Nothing
            'If Not almgr.Aliases.TryGetValue(tbl, [alias]) Then
            '    [alias] = almgr.AddTable(tbl_real, params)
            'Else
            '    tbl_real = tbl.OnTableAdd(params)
            '    If tbl_real Is Nothing Then
            '        tbl_real = tbl
            '    End If
            'End If
            [alias] = almgr.AddTable(tbl_real, osrc_, params)
            'selectcmd = selectcmd.Replace(tbl.TableName & ".", [alias] & ".")
            almgr.Replace(mpe, s, tbl, osrc_, sb)
            'Dim appendMain As Boolean

            Dim selectType As Type = Nothing
            If osrc IsNot Nothing Then
                selectType = osrc.GetRealType(mpe)
            End If

            Dim st As SearchFragment = TryCast(tbl_real, SearchFragment)
            If st IsNot Nothing Then
                appendMain = FormatSearchTable(mpe, sb, st, s, os, params, selectType) OrElse appendMain
            Else
                sb.Append(s.GetTableName(tbl_real))
            End If

            sb.Append(" ").Append([alias])
            If apd IsNot Nothing Then
                sb.Append(apd())
            End If
            'sb.Append(s.EndLine)

            Dim pk As Pair(Of SourceFragment, String) = Nothing

            If st IsNot Nothing Then
                Dim stt As Type = st.Type
                If stt Is Nothing Then
                    stt = selectType
                End If

                If Not appendMain.HasValue AndAlso filter IsNot Nothing Then
                    For Each f As IFilter In filter.GetAllFilters
                        Dim ef As EntityFilter = TryCast(f, EntityFilter)
                        If ef IsNot Nothing Then
                            Dim rt As Type = ef.Template.ObjectSource.GetRealType(mpe)
                            'If ef.Template.Type IsNot Nothing Then
                            '    If rt Is stt Then
                            '        appendMain = True
                            '        Exit For
                            '    End If
                            'Else
                            '    Dim t As Type = s.GetTypeByEntityName(ef.Template.EntityName)
                            '    If t Is stt Then
                            '        appendMain = True
                            '        Exit For
                            '    End If
                            'End If
                            If rt Is stt Then
                                appendMain = True
                                Exit For
                            End If
                        End If
                    Next
                End If

                If appendMain Then
                    Dim j As New QueryJoin(stt, Worm.Criteria.Joins.JoinType.Join, _
                        New JoinFilter(tbl_real, s.FTSKey, stt, mpe.GetPrimaryKeys(stt, os)(0).PropertyAlias, _
                                       Criteria.FilterOperation.Equal))

                    sb.Append(s.EndLine).Append(j.MakeSQLStmt(mpe, s, filterInfo, almgr, params, osrc_))
                Else
                    pk = New Pair(Of SourceFragment, String)(tbl_real, s.FTSKey)
                End If
            End If

            Dim fs As IMultiTableObjectSchema = TryCast(os, IMultiTableObjectSchema)

            If fs IsNot Nothing Then
                For j As Integer = 1 To tables.Length - 1
                    Dim join As QueryJoin = CType(mpe.GetJoins(fs, tbl, tables(j), filterInfo), QueryJoin)

                    If Not QueryJoin.IsEmpty(join) Then
                        If Not almgr.ContainsKey(tables(j), osrc) Then
                            almgr.AddTable(tables(j), osrc, params)
                        End If
                        sb.Append(s.EndLine).Append(join.MakeSQLStmt(mpe, s, filterInfo, almgr, params, osrc_))
                        almgr.Replace(mpe, s, join.Table, osrc, sb)
                    End If
                Next
            End If

            Return pk
        End Function

        Protected Shared Sub FormJoins(ByVal mpe As ObjectMappingEngine, ByVal filterInfo As Object, ByVal query As QueryCmd, _
            ByVal params As ICreateParam, ByVal selSchema As IEntitySchema, _
            ByVal j As List(Of Worm.Criteria.Joins.QueryJoin), ByVal almgr As IPrepareTable, _
            ByVal sb As StringBuilder, ByVal s As SQLGenerator, _
            ByVal pk As Pair(Of SourceFragment, String), ByVal filter As IFilter)

            Dim pkname As String = Nothing
            Dim selectedType As Type = query.GetSelectedType(mpe)
            If selectedType IsNot Nothing Then
                'Dim selSchema As IObjectSchemaBase = mpe.GetObjectSchema(selectType)
                pkname = mpe.GetPrimaryKeys(selectedType, selSchema)(0).PropertyAlias
            End If

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
                        join.InjectJoinFilter(mpe, selectedType, pkname, pk.First, pk.Second)
                    End If

                    If join.Table Is Nothing Then
                        Dim t As Type = join.ObjectSource.GetRealType(mpe)
                        'If t Is Nothing Then
                        '    t = s.GetTypeByEntityName(join.EntityName)
                        'End If

                        Dim oschema As IEntitySchema = mpe.GetEntitySchema(t)

                        Dim needAppend As Boolean = True
                        Dim cond As IFilter = join.Condition

                        If cond Is Nothing AndAlso (join.M2MJoinType IsNot Nothing OrElse join.M2MJoinEntityName IsNot Nothing) Then
                            Dim t2 As Type = join.M2MJoinType
                            If t2 Is Nothing Then
                                t2 = mpe.GetTypeByEntityName(join.M2MJoinEntityName)
                            End If

                            Dim oschema2 As IEntitySchema = mpe.GetEntitySchema(t2)

                            Dim t12t2 As Entities.Meta.M2MRelationDesc = mpe.GetM2MRelation(t, oschema, t2, join.M2MKey)
                            Dim t22t1 As Entities.Meta.M2MRelationDesc = mpe.GetM2MRelation(t2, oschema2, t, join.M2MKey)

                            Dim t2_pk As String = mpe.GetPrimaryKeys(t2)(0).PropertyAlias
                            Dim t1_pk As String = mpe.GetPrimaryKeys(t)(0).PropertyAlias

                            'Dim jl As JoinLink = JCtor.Join(t22t1.Table).On(t22t1.Table, t22t1.Column).Eq(t, t1_pk)
                            Dim tbl As SourceFragment = CType(t22t1.Table.Clone, SourceFragment)
                            Dim jl As JoinLink = Nothing
                            If pk IsNot Nothing Then
                                jl = JCtor.join(tbl).[on](tbl, t12t2.Column).eq(pk.First, pk.Second)
                            Else
                                jl = JCtor.join(tbl).[on](tbl, t12t2.Column).eq(t2, t2_pk)
                            End If

                            If almgr.ContainsKey(oschema.Table, join.ObjectSource) Then
                                jl.[and](tbl, t22t1.Column).eq(t, t1_pk)
                                needAppend = False
                            Else
                                cond = Ctor.column(tbl, t22t1.Column).eq(t, t1_pk).Filter
                            End If

                            Dim js() As QueryJoin = jl

                            sb.Append(s.EndLine).Append(js(0).MakeSQLStmt(mpe, s, filterInfo, almgr, params, join.ObjectSource))
                        End If

                        If needAppend Then
                            'Dim tables() As SourceFragment
                            'Dim mts As IMultiTableObjectSchema = TryCast(oschema, IMultiTableObjectSchema)
                            'If mts Is Nothing Then
                            '    tables = New SourceFragment() {oschema.Table}
                            'Else
                            '    tables = mts.GetTables()
                            'End If

                            sb.Append(join.JoinTypeString())

                            Dim f As QueryCmd.FromClauseDef = Nothing 'New QueryCmd.FromClause(join.ObjectSource)
                            FormTypeTables(mpe, filterInfo, params, almgr, sb, s, oschema, join.ObjectSource, filter, f, query.AppendMain, _
                                           Function() " on " & cond.MakeQueryStmt(mpe, s, filterInfo, almgr, params))
                        End If
                        'tbl = s.GetTables(t)(0)
                    Else
                        sb.Append(s.EndLine).Append(join.MakeSQLStmt(mpe, s, filterInfo, almgr, params, Nothing))
                        almgr.Replace(mpe, s, join.Table, Nothing, sb)
                    End If
                End If
            Next
        End Sub

        Protected Shared Sub FormHaving(ByVal mpe As ObjectMappingEngine, ByVal query As QueryCmd, _
            ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal s As SQLGenerator, _
            ByVal pmgr As ICreateParam)

            If query.propHaving IsNot Nothing Then
                sb.Append(" having ").Append(query.propHaving.Filter.MakeQueryStmt(mpe, s, Nothing, almgr, pmgr))
            End If
        End Sub

        Protected Shared Sub FormGroupBy(ByVal mpe As ObjectMappingEngine, ByVal query As QueryCmd, _
            ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal s As SQLGenerator, _
            ByVal defTbl As SourceFragment, ByVal defOS As IEntitySchema)
            If query.Group IsNot Nothing Then
                sb.Append(" group by ")
                For Each g As SelectExpression In query.Group
                    'If g.Table IsNot Nothing Then
                    '    sb.Append(almgr.GetAlias(g.Table, Nothing)).Append(s.Selector).Append(g.Column)
                    'Else
                    '    If Not String.IsNullOrEmpty(g.Computed) Then
                    '        sb.Append(String.Format(g.Computed, ObjectMappingEngine.ExtractValues(mpe, s, almgr, g.Values).ToArray))
                    '    Else
                    '        Dim t As Type = g.ObjectSource.GetRealType(mpe)
                    '        Dim schema As IEntitySchema = mpe.GetObjectSchema(t)
                    '        Dim cm As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()
                    '        Dim map As MapField2Column = Nothing
                    '        If cm.TryGetValue(g.PropertyAlias, map) Then
                    '            sb.Append(almgr.GetAlias(map._tableName, g.ObjectSource)).Append(s.Selector).Append(map._columnName)
                    '        Else
                    '            Throw New ArgumentException(String.Format("Field {0} of type {1} is not defined", g.PropertyAlias, g.ObjectSource.ToStaticString))
                    '        End If
                    '    End If
                    'End If
                    s.CreateSelectExpressionFormater().Format(g, sb, mpe, almgr, Nothing, _
                        Nothing, query.SelectList, defTbl, defOS, False)
                    sb.Append(",")
                Next
                sb.Length -= 1
            End If
        End Sub

        Protected Shared Sub FormOrderBy(ByVal mpe As ObjectMappingEngine, ByVal query As QueryCmd, _
            ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal filterInfo As Object, _
            ByVal params As ICreateParam)
            If query.propSort IsNot Nothing AndAlso Not query.propSort.IsExternal Then
                s.CreateSelectExpressionFormater().Format(query.propSort, sb, mpe, almgr, params, filterInfo, query.SelectList, query.Table, query.GetSchemaForSelectType(mpe), False)
                'Dim adv As DbSort = TryCast(query.propSort, DbSort)
                'If adv IsNot Nothing Then
                '    adv.MakeStmt(s, almgr, columnAliases, sb, t, filterInfo, params)
                'Else
                '    s.AppendOrder(t, query.propSort, almgr, sb, True, query.SelectList, query.Table)
                'End If
            End If
        End Sub

        Public Shared Sub MakeInnerQueryStatement(ByVal mpe As ObjectMappingEngine, ByVal filterInfo As Object, ByVal schema As SQLGenerator, _
            ByVal query As QueryCmd, ByVal params As ICreateParam, _
            ByVal eu As EntityUnion, ByVal sb As StringBuilder, ByVal almgr As IPrepareTable)

            Dim t As SourceFragment = QueryCmd.InnerTbl
            'Dim almgr As IPrepareTable = AliasMgr.Create
            Dim al As String = almgr.AddTable(t, eu)

            sb.Append("(")
            sb.Append(MakeQueryStatement(mpe, filterInfo, schema, query, params))
            sb.Append(") ").Append(al)

            almgr.Replace(mpe, schema, t, eu, sb)
        End Sub

        Public Shared Function MakeQueryStatement(ByVal mpe As ObjectMappingEngine, ByVal filterInfo As Object, ByVal schema As SQLGenerator, _
            ByVal query As QueryCmd, ByVal params As ICreateParam) As String

            Return MakeQueryStatement(mpe, filterInfo, schema, query, params, AliasMgr.Create)
        End Function

        Public Shared Function MakeQueryStatement(ByVal mpe As ObjectMappingEngine, ByVal filterInfo As Object, ByVal schema As SQLGenerator, _
            ByVal query As QueryCmd, ByVal params As ICreateParam, ByVal almgr As IPrepareTable) As String

            Dim sb As New StringBuilder
            Dim s As SQLGenerator = schema
            'Dim almgr As IPrepareTable = AliasMgr.Create

            FormSingleQuery(mpe, sb, query, s, almgr, filterInfo, params)

            FormUnions(mpe, query, sb, filterInfo, s, params)

            If query.RowNumberFilter Is Nothing Then
                FormOrderBy(mpe, query, almgr, sb, s, filterInfo, params)
            Else
                Dim r As New StringBuilder
                FormOrderBy(mpe, query, almgr, r, s, filterInfo, params)
                sb.Replace(RowNumberOrder, r.ToString)
            End If

            If Not String.IsNullOrEmpty(query.Hint) Then
                sb.Append(" ").Append(query.Hint)
            End If

            If query.RowNumberFilter IsNot Nothing Then
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
                sb.Append(query.RowNumberFilter.MakeQueryStmt(mpe, s, filterInfo, almgr, params))
            End If

            Return sb.ToString
        End Function

        Public Shared Sub FormUnions(ByVal mpe As ObjectMappingEngine, ByVal query As QueryCmd, _
            ByVal sb As StringBuilder, ByVal filterInfo As Object, ByVal s As SQLGenerator, _
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

                    FormSingleQuery(mpe, sb, p.Second, s, AliasMgr.Create, filterInfo, param)
                Next
            End If
        End Sub

        Public Shared Sub FormSingleQuery(ByVal mpe As ObjectMappingEngine, ByVal sb As StringBuilder, _
            ByVal query As QueryCmd, ByVal s As SQLGenerator, ByVal almgr As IPrepareTable, ByVal filterInfo As Object, _
            ByVal params As ICreateParam)

            Dim os As IEntitySchema = Nothing
            Dim selType As Type = query.GetSelectedType(mpe)

            If selType IsNot Nothing Then
                os = mpe.GetEntitySchema(selType)
            End If

            Dim defaultTbl As SourceFragment = Nothing
            If query.Table IsNot Nothing Then
                defaultTbl = query.FromClause.Table
            Else
                defaultTbl = QueryCmd.InnerTbl
            End If

            sb.Append("select ")

            If query.propDistinct Then
                sb.Append("distinct ")
            End If

            If query.propTop IsNot Nothing Then
                sb.Append(s.TopStatement(query.propTop.Count, query.propTop.Percent, query.propTop.Ties)).Append(" ")
            End If

            FormSelectList(mpe, query, sb, s, os, selType, almgr, filterInfo, params, query._sl, defaultTbl)

            sb.Append(" from ")

            If query.FromClause.AnyQuery IsNot Nothing Then
                MakeInnerQueryStatement(mpe, filterInfo, s, query.FromClause.Query, _
                                        params, query.FromClause.QueryEU, sb, almgr)
            Else
                Dim newPK As Pair(Of SourceFragment, String) = FormTypeTables( _
                    mpe, filterInfo, params, almgr, sb, s, os, query.GetSelectedOS, query._f, _
                    query.FromClause, query.AppendMain, Nothing)

                FormJoins(mpe, filterInfo, query, params, os, query._js, almgr, sb, s, newPK, query._f)
            End If

            ReplaceSelectList(mpe, query, sb, s, os, almgr, filterInfo, params, query._sl)

            s.AppendWhere(mpe, os, query._f, almgr, sb, filterInfo, params)

            FormGroupBy(mpe, query, almgr, sb, s, defaultTbl, os)

            FormHaving(mpe, query, almgr, sb, s, params)
        End Sub
#End Region

        Public Sub ClearCache(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.ClearCache
            GetProvider(mgr, query, Nothing).ResetCache()
        End Sub

        Public Sub RenewCache(ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal v As Boolean) Implements IExecutor.RenewCache
            GetProvider(mgr, query, Nothing).Renew = v
        End Sub

        Public Sub ResetObjects(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.ResetObjects
            Dim p As BaseProvider = GetProvider(mgr, query, Nothing)
            Dim d As IDictionary = p.Dic
            If d IsNot Nothing Then
                Dim ce As UpdatableCachedItem = CType(d(p.Id), UpdatableCachedItem)
                If ce IsNot Nothing Then
                    ce.Clear(mgr)
                End If
            End If
        End Sub

        Public ReadOnly Property IsInCache(ByVal mgr As OrmManager, ByVal query As QueryCmd) As Boolean Implements IExecutor.IsInCache
            Get
                Dim p As BaseProvider = GetProvider(mgr, query, Nothing)
                Dim d As IDictionary = p.Dic
                If d IsNot Nothing Then
                    Dim ce As UpdatableCachedItem = CType(d(p.Id), UpdatableCachedItem)
                    If ce IsNot Nothing Then
                        Dim cv As ICacheValidator = TryCast(p, ICacheValidator)
                        Return cv Is Nothing OrElse (cv.ValidateBeforCacheProbe AndAlso cv.ValidateItemFromCache(ce))
                    Else
                        Return False
                    End If
                Else
                    Return False
                End If
            End Get
        End Property

        'Public Sub Reset(Of ReturnType As _ICachedEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.Reset
        '    GetProcessor(Of ReturnType)(mgr, query).Renew = True
        'End Sub

        'Public Sub Reset(Of CreateType As {New, _ICachedEntity}, ReturnType As _ICachedEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.Reset
        '    GetProcessorT(Of CreateType, ReturnType)(mgr, query).Renew = True
        'End Sub

        'Public Sub ResetEntity(Of ReturnType As Entities._IEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.ResetEntity
        '    GetProcessorAnonym(Of ReturnType)(mgr, query).ResetCache()
        'End Sub

        'Public Sub ResetEntity(Of CreateType As {New, _IEntity}, ReturnType As Entities._IEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.ResetEntity
        '    GetProcessorAnonym(Of CreateType, ReturnType)(mgr, query).ResetCache()
        'End Sub
    End Class

End Namespace