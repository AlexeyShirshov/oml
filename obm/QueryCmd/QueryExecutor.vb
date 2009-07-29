Imports Worm.Cache
Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Expressions2

Namespace Query
    Public MustInherit Class QueryExecutor
        Implements IExecutor

        Public Class cls
            Private _mgr As OrmManager
            Private _pk() As String
            Private _oschema As IEntitySchema

            Public Sub New(ByVal mgr As OrmManager)
                _mgr = mgr
            End Sub

            Private _cancel As Boolean
            Public Property Cancel() As Boolean
                Get
                    Return _cancel
                End Get
                Set(ByVal value As Boolean)
                    _cancel = value
                End Set
            End Property

            Public Sub prepared(ByVal sender As QueryCmd, ByVal args As QueryCmd.QueryPreparedEventArgs)
                RemoveHandler sender.QueryPrepared, AddressOf prepared
                If sender.CreateType IsNot Nothing Then
                    Dim createType As Type = sender.CreateType.GetRealType(_mgr.MappingEngine)
                    If GetType(AnonymousCachedEntity).IsAssignableFrom(createType) Then
                        Dim oschema As IEntitySchema = sender.GetSchemaForSelectType(_mgr.MappingEngine)
                        Dim l As New List(Of String)
                        If oschema Is Nothing Then
l1:
                            For Each se As SelectExpression In sender._sl
                                If (se.Attributes And Field2DbRelations.PK) = Field2DbRelations.PK Then
                                    l.Add(se.GetIntoPropertyAlias)
                                End If
                            Next
                            _oschema = New SimpleObjectSchema(SelectExpression.GetMapping(sender._sl))
                            'sender.AddPOCO(GetType(AnonymousCachedEntity), _oschema)
                        Else
                            'If Not _mgr.MappingEngine.HasEntitySchema(createType) Then
                            _oschema = oschema
                            'End If
                            For Each pk As EntityPropertyAttribute In _mgr.MappingEngine.GetPrimaryKeys(createType, oschema)
                                l.Add(pk.PropertyAlias)
                            Next
                            If l.Count = 0 Then
                                GoTo l1
                            End If
                        End If
                        _pk = l.ToArray
                        AddHandler _mgr.ObjectCreated, AddressOf ObjectCreated
                    ElseIf Not GetType(AnonymousEntity).IsAssignableFrom(createType) Then
                        'Dim dic As New Dictionary(Of Type, Object)
                        Dim moreThanOneType As Type = Nothing
                        Dim an As New EntityUnion(GetType(AnonymousEntity))
                        For Each se As SelectExpression In sender._sl
                            For Each su As SelectUnion In Expressions2.Helper.GetSelectedEntities(se)
                                Dim d As EntityUnion = su.EntityUnion
                                If d IsNot Nothing Then
                                    Dim rt As Type = d.GetRealType(_mgr.MappingEngine)
                                    If Not GetType(_IEntity).IsAssignableFrom(rt) Then
                                        sender._createTypes(d) = an
                                    End If
                                    If moreThanOneType Is Nothing Then
                                        moreThanOneType = rt
                                    ElseIf Not _cancel Then
                                        _cancel = moreThanOneType IsNot rt
                                    End If
                                End If
                            Next
                        Next
l2:
                        args.Cancel = _cancel
                        'If Not _cancel Then
                        '    If (cnt <> 0 And cnt <> sender._types.Count) Then
                        '        args.Cancel = True
                        '        _cancel = True
                        '        Dim an As New EntityUnion(GetType(AnonymousEntity))
                        '        For Each d As EntityUnion In sender._types.Keys
                        '            If Not GetType(_IEntity).IsAssignableFrom(d.GetRealType(_mgr.MappingEngine)) Then
                        '                sender._createTypes(d) = an
                        '            End If
                        '        Next
                        '    End If
                        'End If
                    End If
                    'Else
                    '    Throw New InvalidOperationException
                End If
            End Sub

            Public Sub ObjectCreated(ByVal sender As OrmManager, ByVal o As IEntity)
                Dim a As AnonymousCachedEntity = CType(o, AnonymousCachedEntity)
                a._pk = _pk
                a._myschema = _oschema
            End Sub

            Public Sub RemoveEvent()
                RemoveHandler _mgr.ObjectCreated, AddressOf ObjectCreated
            End Sub
        End Class

        Private Class CIStore
            Public ci As Object

            Public Function GetMatrix(ByVal m As OrmManager, ByVal q As QueryCmd, ByVal p2 As OrmManager.ICacheItemProvoderBase, ByVal ce As Cache.CachedItemBase, ByVal s As Cache.IListObjectConverter.ExtractListResult, ByVal created As Boolean) As ReadonlyMatrix
                ci = ce.CustomInfo
                Return ce.GetMatrix(m, q.propWithLoads, created, m.GetStart, m.GetLength, s)
            End Function

            Public Function GetEntityList(Of ReturnType As _ICachedEntity)(ByVal m As OrmManager, ByVal q As QueryCmd, ByVal p2 As OrmManager.ICacheItemProvoderBase, ByVal ce As Cache.CachedItemBase, ByVal s As Cache.IListObjectConverter.ExtractListResult, ByVal created As Boolean) As ReadOnlyEntityList(Of ReturnType)
                ci = ce.CustomInfo
                Return CType(ce, UpdatableCachedItem).GetObjectList(Of ReturnType)(m, q.propWithLoad, created, m.GetStart, m.GetLength, s)
            End Function

            Public Function GetObjectList(Of ReturnType As _IEntity)(ByVal m As OrmManager, ByVal q As QueryCmd, ByVal p2 As OrmManager.ICacheItemProvoderBase, ByVal ce As Cache.CachedItemBase, ByVal s As Cache.IListObjectConverter.ExtractListResult, ByVal created As Boolean) As ReadOnlyObjectList(Of ReturnType)
                ci = ce.CustomInfo
                Return ce.GetObjectList(Of ReturnType)(m, True, created, m.GetStart, m.GetLength, s)
            End Function

            Public Function GetList(Of ReturnType)(ByVal m As OrmManager, ByVal q As QueryCmd, ByVal p2 As OrmManager.ICacheItemProvoderBase, ByVal ce As Cache.CachedItemBase, ByVal s As Cache.IListObjectConverter.ExtractListResult, ByVal created As Boolean) As IList(Of ReturnType)
                ci = ce.CustomInfo
                Return ce.GetObjectList(Of ReturnType)(m, m.GetStart, m.GetLength)
            End Function
        End Class

        Public Event OnGetCacheItem(ByVal sender As IExecutor, ByVal args As IExecutor.GetCacheItemEventArgs) Implements IExecutor.OnGetCacheItem
        Public Event OnRestoreDefaults(ByVal sender As IExecutor, ByVal mgr As OrmManager, ByVal args As EventArgs) Implements IExecutor.OnRestoreDefaults

        Protected Delegate Function InitTypesDelegate(ByVal mgr As OrmManager, ByVal query As QueryCmd) As Boolean

        Protected Delegate Function GetCeDelegate( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal dic As IDictionary, ByVal id As String, ByVal sync As String, ByVal p2 As OrmManager.ICacheItemProvoderBase) As Worm.Cache.CachedItemBase

        Protected Delegate Function GetListFromCEDelegate(Of ReturnType)( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal p As OrmManager.ICacheItemProvoderBase, _
            ByVal ce As Cache.CachedItemBase, ByVal s As Cache.IListObjectConverter.ExtractListResult, ByVal created As Boolean) As ReturnType

        Protected Delegate Function GetProcessorDelegate() As CacheItemBaseProvider

        Private _prepared As Boolean
        Public Property Prepared() As Boolean
            Get
                Return _prepared
            End Get
            Set(ByVal value As Boolean)
                _prepared = value
            End Set
        End Property

        Public Sub ClearCache(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.ClearCache
            GetProvider(mgr, query, Nothing).ResetCache()
        End Sub

        Public Sub RenewCache(ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal v As Boolean) Implements IExecutor.RenewCache
            GetProvider(mgr, query, Nothing).Renew = v
        End Sub

        Public Sub ResetObjects(ByVal mgr As OrmManager, ByVal query As QueryCmd) Implements IExecutor.ResetObjects
            Dim p As CacheItemBaseProvider = GetProvider(mgr, query, Nothing)
            Dim d As IDictionary = p.Dic
            If d IsNot Nothing Then
                Dim ce As CachedItemBase = CType(d(p.Id), CachedItemBase)
                If ce IsNot Nothing Then
                    ce.Clear(mgr)
                End If
            End If
        End Sub

        Public ReadOnly Property IsInCache(ByVal mgr As OrmManager, ByVal query As QueryCmd) As Boolean Implements IExecutor.IsInCache
            Get
                Dim p As CacheItemBaseProvider = GetProvider(mgr, query, Nothing)
                Dim d As IDictionary = p.Dic
                If d IsNot Nothing Then
                    Dim ce As CachedItemBase = CType(d(p.Id), CachedItemBase)
                    If ce IsNot Nothing Then
                        Dim uce As UpdatableCachedItem = TryCast(ce, UpdatableCachedItem)
                        If uce IsNot Nothing Then
                            Dim cv As ICacheValidator = TryCast(p, ICacheValidator)
                            Return cv Is Nothing OrElse (cv.ValidateBeforCacheProbe AndAlso cv.ValidateItemFromCache(uce))
                        Else
                            Return True
                        End If
                    Else
                        Return False
                    End If
                Else
                    Return False
                End If
            End Get
        End Property

        Public Sub SetCache(ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal l As ICollection) Implements IExecutor.SetCache
            Dim p As CacheItemBaseProvider = GetProvider(mgr, query, Nothing)
            Dim d As IDictionary = p.Dic
            Dim ce As CachedItemBase = Nothing
            If d IsNot Nothing Then
                If GetType(IReadOnlyList).IsAssignableFrom(l.GetType) Then
                    ce = New UpdatableCachedItem(l, mgr)
                    CType(ce, UpdatableCachedItem).Filter = query._f
                    CType(ce, UpdatableCachedItem).Sort = query.Sort
                ElseIf GetType(ReadonlyMatrix).IsAssignableFrom(l.GetType) Then
                    ce = New CachedItemBase(CType(l, ReadonlyMatrix), mgr.Cache)
                Else
                    ce = New CachedItemBase(l, mgr.Cache)
                End If
            End If
            d(p.Id) = ce
            p.CreateDepends(ce)
        End Sub

        Protected MustOverride Function GetProvider(ByVal mgr As OrmManager, ByVal query As QueryCmd, _
            ByVal d As InitTypesDelegate) As CacheItemBaseProvider

        Protected MustOverride Function _Exec(Of ReturnType)(ByVal mgr As OrmManager, _
            ByVal query As QueryCmd, ByVal gp As GetProcessorDelegate, _
            ByVal d As GetCeDelegate, ByVal d2 As GetListFromCEDelegate(Of ReturnType)) As ReturnType

        Protected MustOverride Function GetProcessorS(Of T)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As CacheItemBaseProvider
        Protected MustOverride Function GetProcessor(Of ReturnType As {Entities.ICachedEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As CacheItemBaseProvider
        Protected MustOverride Function GetProcessorT(Of CreateType As {Entities.ICachedEntity, New}, ReturnType As {Entities.ICachedEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As CacheItemBaseProvider
        Protected MustOverride Function GetProcessorAnonym(Of ReturnType As {Entities._IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As CacheItemBaseProvider
        Protected MustOverride Function GetProcessorAnonym(Of CreateType As {New, Entities._IEntity}, ReturnType As {Entities._IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd) As CacheItemBaseProvider

        Protected Sub RaiseOnGetCacheItem(ByVal args As IExecutor.GetCacheItemEventArgs)
            RaiseEvent OnGetCacheItem(Me, args)
        End Sub

        Protected Sub RaiseOnRestoreDefaults(ByVal mgr As OrmManager)
            RaiseEvent OnRestoreDefaults(Me, mgr, EventArgs.Empty)
        End Sub

        Protected Function GetProcessor(ByVal mgr As OrmManager, ByVal query As QueryCmd) As CacheItemBaseProvider
            Return GetProvider(mgr, query, Nothing)
        End Function

        Public Function Exec(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadonlyMatrix Implements IExecutor.Exec
            Dim cis As New CIStore

            Dim res As ReadonlyMatrix = _Exec(Of ReadonlyMatrix)(mgr, query, _
                Function() GetProcessor(mgr, query), _
                Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                    m.GetFromCacheBase(dic, sync, id, Nothing, p2, Nothing), _
                AddressOf cis.GetMatrix)

            If mgr.LastExecutionResult.CacheHit Then
                Dim args As QueryCmd.ModifyResultArgs = query.RaiseModifyResult(mgr, res, cis.ci)
                Return args.Matrix
            Else
                Return res
            End If
        End Function

        Public Function Exec(Of ReturnType As Entities._ICachedEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType) Implements IExecutor.Exec
            Dim c As New cls(mgr)
            Try
                AddHandler query.QueryPrepared, AddressOf c.prepared
                Dim cis As New CIStore
                Dim res As ReadOnlyEntityList(Of ReturnType) = _Exec(Of ReadOnlyEntityList(Of ReturnType))(mgr, query, _
                   Function() GetProcessor(Of ReturnType)(mgr, query), _
                   Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                       m.GetFromCache(Of ReturnType)(dic, sync, id, q.propWithLoad, p2), _
                   AddressOf cis.GetEntityList(Of ReturnType))

                If res Is Nothing Then
                    If c.Cancel Then
                        Prepared = True
                        Dim r As ReadonlyMatrix = Exec(mgr, query)
                        Dim l As New List(Of ReturnType)
                        For Each row As ObjectModel.ReadOnlyCollection(Of Entities._IEntity) In r
                            l.Add(CType(row(0), ReturnType))
                        Next
                        res = CType(OrmManager._CreateReadOnlyList(GetType(ReturnType), l), Global.Worm.ReadOnlyEntityList(Of ReturnType))
                    Else
                        Throw New InvalidOperationException
                    End If
                End If

                If mgr.LastExecutionResult.CacheHit Then
                    Dim args As QueryCmd.ModifyResultArgs = query.RaiseModifyResult(mgr, res, cis.ci)
                    Return CType(args.ReadOnlyList, ReadOnlyEntityList(Of ReturnType))
                Else
                    Return res
                End If
            Finally
                c.RemoveEvent()
            End Try
        End Function

        Public Function Exec(Of CreateType As {New, Entities._ICachedEntity}, ReturnType As Entities._ICachedEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType) Implements IExecutor.Exec
            Dim c As New cls(mgr)
            Try
                AddHandler query.QueryPrepared, AddressOf c.prepared
                Dim cis As New CIStore
                Dim res As ReadOnlyEntityList(Of ReturnType) = _Exec(Of ReadOnlyEntityList(Of ReturnType))(mgr, query, _
                   Function() GetProcessorT(Of CreateType, ReturnType)(mgr, query), _
                   Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                       m.GetFromCache(Of ReturnType)(dic, sync, id, q.propWithLoad, p2), _
                   AddressOf cis.GetEntityList(Of ReturnType))

                If res Is Nothing Then
                    If c.Cancel Then
                        Prepared = True
                        Dim r As ReadonlyMatrix = Exec(mgr, query)
                        Dim l As New List(Of ReturnType)
                        'If query.Includes IsNot Nothing Then

                        'End If
                        For Each row As ObjectModel.ReadOnlyCollection(Of Entities._IEntity) In r
                            l.Add(CType(row(0), ReturnType))
                        Next
                        res = CType(OrmManager._CreateReadOnlyList(GetType(ReturnType), l), Global.Worm.ReadOnlyEntityList(Of ReturnType))
                    Else
                        Throw New InvalidOperationException
                    End If
                End If

                If mgr.LastExecutionResult.CacheHit Then
                    Dim args As QueryCmd.ModifyResultArgs = query.RaiseModifyResult(mgr, res, cis.ci)
                    Return CType(args.ReadOnlyList, ReadOnlyEntityList(Of ReturnType))
                Else
                    Return res
                End If
            Finally
                c.RemoveEvent()
            End Try
        End Function

        Private Function _ExecEntity(Of ReturnType As {Entities._IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd, _
            ByVal d As GetProcessorDelegate) As ReadOnlyObjectList(Of ReturnType)
            'If GetType(AnonymousCachedEntity).IsAssignableFrom(query.CreateType) Then
            Dim c As New cls(mgr)
            Try
                AddHandler query.QueryPrepared, AddressOf c.prepared
                Dim cis As New CIStore
                Dim res As ReadOnlyObjectList(Of ReturnType) = _Exec(Of ReadOnlyObjectList(Of ReturnType))(mgr, query, d, _
                    Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                        m.GetFromCache2(dic, sync, id, True, p2), _
                    AddressOf cis.GetObjectList(Of ReturnType))

                If mgr.LastExecutionResult.CacheHit Then
                    Dim args As QueryCmd.ModifyResultArgs = query.RaiseModifyResult(mgr, res, cis.ci)
                    Return CType(args.ReadOnlyList, ReadOnlyObjectList(Of ReturnType))
                Else
                    Return res
                End If
            Finally
                c.RemoveEvent()
            End Try
            'Else
            'Return _Exec(Of ReadOnlyObjectList(Of ReturnType))(mgr, query, d, _
            '    Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
            '        m.GetFromCache2(dic, sync, id, True, p2), _
            '    Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
            '       CType(ce.GetObjectList(Of ReturnType)(m, True, created, m.GetStart, m.GetLength, s), Global.Worm.ReadOnlyObjectList(Of ReturnType)))
            'End If
        End Function

        Public Function ExecEntity(Of ReturnType As Entities._IEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType) Implements IExecutor.ExecEntity
            Return _ExecEntity(Of ReturnType)(mgr, query, Function() GetProcessorAnonym(Of ReturnType)(mgr, query))
        End Function

        Public Function ExecEntity(Of CreateType As {New, Entities._IEntity}, ReturnType As Entities._IEntity)( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType) Implements IExecutor.ExecEntity
            If GetType(AnonymousCachedEntity).IsAssignableFrom(GetType(CreateType)) Then
                Dim c As New cls(mgr)
                Try
                    AddHandler query.QueryPrepared, AddressOf c.prepared
                    Return _ExecEntity(Of ReturnType)(mgr, query, _
                            Function() GetProcessorAnonym(Of CreateType, ReturnType)(mgr, query))
                Finally
                    c.RemoveEvent()
                End Try
            Else
                Return _ExecEntity(Of ReturnType)(mgr, query, _
                        Function() GetProcessorAnonym(Of CreateType, ReturnType)(mgr, query))
            End If
        End Function

        Public Function ExecSimple(Of ReturnType)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As IList(Of ReturnType) Implements IExecutor.ExecSimple
            Dim olds As Boolean = query.CacheSort
            query.CacheSort = True
            Try
                Dim cis As New CIStore
                Dim res As IList(Of ReturnType) = _Exec(mgr, query, Function() GetProcessorS(Of ReturnType)(mgr, query), _
                    Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                        m.GetFromCacheBase(dic, sync, id, Nothing, p2, Nothing), _
                    AddressOf cis.GetList(Of ReturnType))

                If mgr.LastExecutionResult.CacheHit Then
                    Dim args As QueryCmd.ModifyResultArgs = query.RaiseModifyResult(mgr, CType(res, ICollection), cis.ci)
                    Return CType(args.SimpleList, IList(Of ReturnType))
                Else
                    Return res
                End If
            Finally
                query._cacheSort = olds
            End Try
        End Function
    End Class
End Namespace