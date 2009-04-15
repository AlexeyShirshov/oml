Imports Worm.Cache
Imports System.Collections.Generic

Namespace Query
    Public MustInherit Class QueryExecutor
        Implements IExecutor

        Public Event OnGetCacheItem(ByVal sender As IExecutor, ByVal args As IExecutor.GetCacheItemEventArgs) Implements IExecutor.OnGetCacheItem
        Public Event OnRestoreDefaults(ByVal sender As IExecutor, ByVal mgr As OrmManager, ByVal args As EventArgs) Implements IExecutor.OnRestoreDefaults

        Protected Delegate Function InitTypesDelegate(ByVal mgr As OrmManager, ByVal query As QueryCmd) As Boolean

        Protected Delegate Function GetCeDelegate( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal dic As IDictionary, ByVal id As String, ByVal sync As String, ByVal p2 As OrmManager.ICacheItemProvoderBase) As Worm.Cache.CachedItemBase

        Protected Delegate Function GetListFromCEDelegate(Of ReturnType)( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal p As OrmManager.ICacheItemProvoderBase, _
            ByVal ce As Cache.CachedItemBase, ByVal s As Cache.IListObjectConverter.ExtractListResult, ByVal created As Boolean) As ReturnType

        Protected Delegate Function GetProcessorDelegate() As CacheItemBaseProvider

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
                Dim ce As UpdatableCachedItem = CType(d(p.Id), UpdatableCachedItem)
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

        Friend Sub SetCache(ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal l As IEnumerable)
            Dim p As CacheItemBaseProvider = GetProvider(mgr, query, Nothing)
            Dim d As IDictionary = p.Dic
            If d IsNot Nothing Then
                d(p.Id) = New UpdatableCachedItem(l, mgr)
            End If
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
            Return _Exec(Of ReadonlyMatrix)(mgr, query, _
                Function() GetProcessor(mgr, query), _
                Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                    m.GetFromCacheBase(dic, sync, id, Nothing, p2, Nothing), _
                Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
                    ce.GetMatrix(m, q.propWithLoads, created, m.GetStart, m.GetLength, s) _
                )
        End Function

        Public Function Exec(Of ReturnType As Entities._ICachedEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType) Implements IExecutor.Exec
            'Dim mi As Reflection.MethodInfo = GetType(UpdatableCachedItem).GetMethod("GetObjectList", New Type() {GetType(OrmManager)})
            'Dim mig As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {query.GetSelectedType(mgr.MappingEngine)})
            'Return CType(_Exec(Of ReadOnlyEntityList(Of ReturnType))(mgr, query, _
            '   Function() GetProcessor(Of ReturnType)(mgr, query), _
            '   Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
            '       m.GetFromCache(Of ReturnType)(dic, sync, id, q.propWithLoad, p2), _
            '   Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
            '       CType(mig.Invoke(ce, New Object() {m, q.propWithLoad, created, m.GetStart, m.GetLength, s}), ReadOnlyEntityList(Of ReturnType)) _
            '   ), ReadOnlyEntityList(Of ReturnType))
            Return CType(_Exec(Of ReadOnlyEntityList(Of ReturnType))(mgr, query, _
               Function() GetProcessor(Of ReturnType)(mgr, query), _
               Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                   m.GetFromCache(Of ReturnType)(dic, sync, id, q.propWithLoad, p2), _
               Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
                   CType(ce, UpdatableCachedItem).GetObjectList(Of ReturnType)(m, q.propWithLoad, created, m.GetStart, m.GetLength, s) _
               ), ReadOnlyEntityList(Of ReturnType))
        End Function

        Public Class cls

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
                _cancel = sender._types.Count > 1
                args.Cancel = _cancel
            End Sub
        End Class

        Private _prepared As Boolean
        Public Property Prepared() As Boolean
            Get
                Return _prepared
            End Get
            Set(ByVal value As Boolean)
                _prepared = value
            End Set
        End Property

        Public Function Exec(Of CreateType As {New, Entities._ICachedEntity}, ReturnType As Entities._ICachedEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType) Implements IExecutor.Exec
            Dim c As New cls
            AddHandler query.QueryPrepared, AddressOf c.prepared
            Dim res As ReadOnlyEntityList(Of ReturnType) = _Exec(Of ReadOnlyEntityList(Of ReturnType))(mgr, query, _
                           Function() GetProcessorT(Of CreateType, ReturnType)(mgr, query), _
                           Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                               m.GetFromCache(Of ReturnType)(dic, sync, id, q.propWithLoad, p2), _
                           Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
                               CType(ce, Cache.UpdatableCachedItem).GetObjectList(Of ReturnType)(m, q.propWithLoad, created, m.GetStart, m.GetLength, s) _
                           )
            If res Is Nothing Then
                If c.Cancel Then
                    Prepared = True
                    Dim r As ReadonlyMatrix = Exec(mgr, query)
                    Dim l As New List(Of ReturnType)
                    For Each row As ObjectModel.ReadOnlyCollection(Of Entities._IEntity) In r
                        l.Add(CType(row(0), ReturnType))
                    Next
                    res = CType(OrmManager.CreateReadonlyList(GetType(ReturnType), l), Global.Worm.ReadOnlyEntityList(Of ReturnType))
                Else
                    Throw New InvalidOperationException
                End If
            End If

            Return res
        End Function

        Private Function _ExecEntity(Of ReturnType As {Entities._IEntity})(ByVal mgr As OrmManager, ByVal query As QueryCmd, _
            ByVal d As GetProcessorDelegate) As ReadOnlyObjectList(Of ReturnType)

            Return _Exec(Of ReadOnlyObjectList(Of ReturnType))(mgr, query, d, _
                Function(m As OrmManager, q As QueryCmd, dic As IDictionary, id As String, sync As String, p2 As OrmManager.ICacheItemProvoderBase) _
                    m.GetFromCache2(dic, sync, id, True, p2), _
                Function(m As OrmManager, q As QueryCmd, p2 As OrmManager.ICacheItemProvoderBase, ce As Cache.CachedItemBase, s As Cache.IListObjectConverter.ExtractListResult, created As Boolean) _
                   CType(ce.GetObjectList(Of ReturnType)(m, True, created, m.GetStart, m.GetLength, s), Global.Worm.ReadOnlyObjectList(Of ReturnType)))
        End Function

        Public Function ExecEntity(Of ReturnType As Entities._IEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType) Implements IExecutor.ExecEntity
            Return _ExecEntity(Of ReturnType)(mgr, query, Function() GetProcessorAnonym(Of ReturnType)(mgr, query))
        End Function

        Public Function ExecEntity(Of CreateType As {New, Entities._IEntity}, ReturnType As Entities._IEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType) Implements IExecutor.ExecEntity
            Return _ExecEntity(Of ReturnType)(mgr, query, Function() GetProcessorAnonym(Of CreateType, ReturnType)(mgr, query))
        End Function

        Public Function ExecSimple(Of ReturnType)(ByVal mgr As OrmManager, ByVal query As QueryCmd) As System.Collections.Generic.IList(Of ReturnType) Implements IExecutor.ExecSimple
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
    End Class
End Namespace