Imports Worm.Query
Imports Worm.Entities
Imports System.Collections.Generic
Imports Worm

Public Delegate Function CreateCmdDelegate() As Query.QueryCmd

Public Interface IDataContext
    Inherits ICreateManager

    Function CreateQuery() As QueryCmd
    Function CreateOrmManager() As OrmManager

    Function [GetByID](Of T As {New, ISinglePKEntity})(ByVal id As Object) As T
    Function [GetByID](Of T As {New, ISinglePKEntity})(ByVal id As Object, ByVal options As GetByIDOptions) As T
    Function [GetByIds](Of T As {New, ISinglePKEntity})(
                ByVal ids As IEnumerable(Of Object),
                ByVal options As GetByIDOptions) As ReadOnlyList(Of T)

    Function [GetByIds](Of T As {New, ISinglePKEntity})(ByVal ids As IEnumerable(Of Object)) As ReadOnlyList(Of T)
    Function [GetByIDDyn](Of T As {ISinglePKEntity})(ByVal id As Object) As T
    Function [GetByIDDyn](Of T As {ISinglePKEntity})(ByVal id As Object, ByVal options As GetByIDOptions) As T
    Function [GetByID](ByVal id As Object, t As Type) As ISinglePKEntity
    Function [GetByID](ByVal id As Object, t As Type, ByVal options As GetByIDOptions) As ISinglePKEntity
    ReadOnly Property Cache As Cache.CacheBase
    ReadOnly Property StmtGenerator As StmtGenerator
    ReadOnly Property MappingEngine As ObjectMappingEngine
    ReadOnly Property IsReadOnly As Boolean
    Property Context As IDictionary
End Interface

Public Enum GetByIDOptions
    GetAsIs
    EnsureExistsInStore
    EnsureLoadedFromStore
End Enum

Public MustInherit Class DataContextBase
    Implements IDataContext

    Protected _cache As Cache.CacheBase
    Protected _stmtGen As StmtGenerator
    Protected _mpe As ObjectMappingEngine

    Private _ctx As IDictionary

    Protected Sub New()

    End Sub

    Public Sub New(stmtGen As StmtGenerator)
        _stmtGen = stmtGen
        _cache = New Cache.ReadonlyCache
        _mpe = OrmManager.DefaultMappingEngine
    End Sub
    Public Sub New(stmtGen As StmtGenerator, mpe As ObjectMappingEngine)
        _stmtGen = stmtGen
        _mpe = mpe
        _cache = New Cache.ReadonlyCache
    End Sub
    Public Sub New(stmtGen As StmtGenerator, mpe As ObjectMappingEngine, cache As Cache.CacheBase)
        _stmtGen = stmtGen
        _cache = cache
        _mpe = mpe
    End Sub

    Public Property Context As IDictionary Implements IDataContext.Context
        Get
            Return _ctx
        End Get
        Set(value As IDictionary)
            _ctx = value
        End Set
    End Property

    Public MustOverride Function CreateOrmManager() As OrmManager Implements IDataContext.CreateOrmManager

    Public Function CreateQuery() As Query.QueryCmd Implements IDataContext.CreateQuery
        Return New QueryCmd(AddressOf CreateOrmManager)
    End Function

#Region " GetByID "
    Public Function [GetByID](Of T As {New, ISinglePKEntity})(ByVal id As Object) As T Implements IDataContext.GetByID
        Return GetByID(Of T)(id, GetByIDOptions.EnsureExistsInStore)
    End Function

    Public Function [GetByID](Of T As {New, ISinglePKEntity})(ByVal id As Object, ByVal options As GetByIDOptions) As T Implements IDataContext.GetByID
        Using gm = GetManager()

            Dim mgr = gm.Manager
            If mgr Is Nothing Then
                Throw New DataContextException("OrmManager required")
            End If

            mgr.GetCreateManager = New CreateManager(AddressOf CreateOrmManager)

            Dim o As ISinglePKEntity = Nothing

            Select Case options
                Case GetByIDOptions.EnsureExistsInStore
                    o = mgr.GetKeyEntityFromCacheOrDB(Of T)(id)
                    If o IsNot Nothing AndAlso o.ObjectState = ObjectState.NotFoundInSource Then
                        o = Nothing
                    End If
                Case GetByIDOptions.GetAsIs
                    o = mgr.GetKeyEntityFromCacheOrCreate(Of T)(id)
                Case GetByIDOptions.EnsureLoadedFromStore
                    o = mgr.GetKeyEntityFromCacheLoadedOrDB(Of T)(id)
                    If o IsNot Nothing AndAlso o.ObjectState = ObjectState.NotFoundInSource Then
                        o = Nothing
                    End If
                Case Else
                    Throw New NotImplementedException
            End Select

            'If o IsNot Nothing Then
            '    If o.GetICreateManager Is Nothing Then
            '        o.SetCreateManager(New CreateManager(AddressOf CreateOrmManager))
            '    End If
            'End If
            Return CType(o, T)
        End Using
    End Function

    Friend Shared Sub ConvertIdsToObjects(Of T As {New, ISinglePKEntity})(ByVal rt As Type, ByVal list As IListEdit,
        ByVal ids As IEnumerable(Of Object), ByVal mgr As OrmManager)
        For Each id As Object In ids
            Dim obj As T = mgr.GetKeyEntityFromCacheOrCreate(Of T)(id, True)

            If obj IsNot Nothing Then
                list.Add(obj)
            ElseIf mgr.Cache.NewObjectManager IsNot Nothing Then
                obj = CType(mgr.Cache.NewObjectManager.GetNew(rt, obj.GetPKValues(Nothing)), T)
                If obj IsNot Nothing Then list.Add(obj)
            End If
        Next
    End Sub

    Friend Shared Sub ConvertIdsToObjects(ByVal rt As Type, ByVal list As IListEdit,
        ByVal ids As IEnumerable(Of Object), ByVal mgr As OrmManager)
        For Each id As Object In ids
            Dim obj As ISinglePKEntity = mgr.GetKeyEntityFromCacheOrCreate(id, rt, True)

            If obj IsNot Nothing Then
                list.Add(obj)
            ElseIf mgr.Cache.NewObjectManager IsNot Nothing Then
                obj = CType(mgr.Cache.NewObjectManager.GetNew(rt, obj.GetPKValues(Nothing)), ISinglePKEntity)
                If obj IsNot Nothing Then list.Add(obj)
            End If
        Next
    End Sub

    Public Function [GetByIds](Of T As {New, ISinglePKEntity})(
                ByVal ids As IEnumerable(Of Object),
                ByVal options As GetByIDOptions) As ReadOnlyList(Of T) Implements IDataContext.GetByIds

        Using gm = GetManager()

            Dim mgr = gm.Manager
            If mgr Is Nothing Then
                Throw New DataContextException("OrmManager required")
            End If

            mgr.GetCreateManager = New CreateManager(AddressOf CreateOrmManager)


            Dim ro As New ReadOnlyList(Of T)
            Dim list As IListEdit = ro
            Dim tp As Type = GetType(T)

            Select Case options
                Case GetByIDOptions.GetAsIs
                    If GetType(T) IsNot tp Then
                        ConvertIdsToObjects(Of T)(tp, list, ids, mgr)
                    Else
                        ConvertIdsToObjects(tp, list, ids, mgr)
                    End If
                Case GetByIDOptions.EnsureExistsInStore
                    If GetType(T) IsNot tp Then
                        ConvertIdsToObjects(Of T)(tp, list, ids, mgr)
                    Else
                        ConvertIdsToObjects(tp, list, ids, mgr)
                    End If
                    ro = CType(Query.QueryCmd.LoadObjects(ro, 0, list.Count, False, mgr), ReadOnlyList(Of T))
                Case GetByIDOptions.EnsureLoadedFromStore
                    If GetType(T) IsNot tp Then
                        ConvertIdsToObjects(Of T)(tp, list, ids, mgr)
                    Else
                        ConvertIdsToObjects(tp, list, ids, mgr)
                    End If
                    ro = CType(Query.QueryCmd.LoadObjects(ro, 0, list.Count, True, mgr), ReadOnlyList(Of T))
                Case Else
                    Throw New NotImplementedException
            End Select

            'For Each o As ISinglePKEntity In list
            '    If o IsNot Nothing Then
            '        If o.GetICreateManager Is Nothing Then
            '            o.SetCreateManager(New CreateManager(AddressOf CreateOrmManager))
            '        End If
            '    End If
            'Next

            Return ro
        End Using
    End Function

    Public Function [GetByIds](Of T As {New, ISinglePKEntity})(ByVal ids As IEnumerable(Of Object)) As ReadOnlyList(Of T) Implements IDataContext.GetByIds
        Return GetByIds(Of T)(ids, GetByIDOptions.GetAsIs)
    End Function

    Public Function [GetByIDDyn](Of T As {ISinglePKEntity})(ByVal id As Object) As T Implements IDataContext.GetByIDDyn
        Return GetByIDDyn(Of T)(id, GetByIDOptions.GetAsIs)
    End Function

    Public Function [GetByIDDyn](Of T As {ISinglePKEntity})(ByVal id As Object, ByVal options As GetByIDOptions) As T Implements IDataContext.GetByIDDyn

        Dim tp As Type = GetType(T)
        Dim o As ISinglePKEntity = Nothing

        Using gm = GetManager()

            Dim mgr = gm.Manager
            If mgr Is Nothing Then
                Throw New DataContextException("OrmManager required")
            End If

            mgr.GetCreateManager = New CreateManager(AddressOf CreateOrmManager)


            Select Case options
                Case GetByIDOptions.EnsureExistsInStore
                    o = mgr.GetKeyEntityFromCacheOrDB(id, tp)
                Case GetByIDOptions.GetAsIs
                    o = mgr.GetKeyEntityFromCacheOrCreate(id, tp)
                Case GetByIDOptions.EnsureLoadedFromStore
                    o = mgr.GetKeyEntityFromCacheLoadedOrDB(id, tp)
                Case Else
                    Throw New NotImplementedException
            End Select

            'If o IsNot Nothing Then
            '    If o.GetICreateManager Is Nothing Then
            '        o.SetCreateManager(New CreateManager(AddressOf CreateOrmManager))
            '    End If
            'End If

            Return CType(o, T)
        End Using

    End Function
#End Region

    Public Overridable ReadOnly Property Cache As Cache.CacheBase Implements IDataContext.Cache
        Get
            Return _cache
        End Get
    End Property

    Public Overridable ReadOnly Property MappingEngine As ObjectMappingEngine Implements IDataContext.MappingEngine
        Get
            Return _mpe
        End Get
    End Property

    Public Overridable ReadOnly Property StmtGenerator As StmtGenerator Implements IDataContext.StmtGenerator
        Get
            Return _stmtGen
        End Get
    End Property

    Public ReadOnly Property IsReadOnly As Boolean Implements IDataContext.IsReadOnly
        Get
            Using mgr = CreateManager(Context)
                Return mgr.IsReadOnly
            End Using
        End Get
    End Property

    Public Function CreateManager(ctx As Object) As OrmManager Implements ICreateManager.CreateManager
        Dim mgr As OrmManager = CreateOrmManager()
        If mgr IsNot Nothing AndAlso mgr.GetCreateManager Is Nothing Then
            mgr.GetCreateManager = New CreateManager(AddressOf CreateOrmManager)
        End If
        Try
            RaiseEvent CreateManagerEvent(Me, New ICreateManager.CreateManagerEventArgs(mgr, Context))
        Catch ex As Exception
            If mgr IsNot Nothing Then
                mgr.Dispose()
                mgr = Nothing
                CoreFramework.CFDebugging.Stack.PreserveStackTrace(ex)
                Throw ex
            End If
        End Try
        Return mgr
    End Function

    Protected Function GetManager() As IGetManager
        Dim mgr As OrmManager = OrmManager.CurrentManager
        If mgr Is Nothing Then
            Return New GetManagerDisposable(CreateManager(Context))
        Else
            'don't dispose
            Return New ManagerWrapper(mgr)
        End If
    End Function

    Public Event CreateManagerEvent(sender As ICreateManager, args As ICreateManager.CreateManagerEventArgs) Implements ICreateManager.CreateManagerEvent

    Public Function GetByID(id As Object, t As Type) As ISinglePKEntity Implements IDataContext.GetByID
        Return GetByID(id, t, GetByIDOptions.GetAsIs)
    End Function

    Public Function GetByID(id As Object, tp As Type, options As GetByIDOptions) As ISinglePKEntity Implements IDataContext.GetByID
        Dim o As ISinglePKEntity = Nothing

        Using gm = GetManager()
            Dim mgr = gm.Manager
            If mgr Is Nothing Then
                Throw New DataContextException("OrmManager required")
            End If

            mgr.GetCreateManager = New CreateManager(AddressOf CreateOrmManager)

            Select Case options
                Case GetByIDOptions.EnsureExistsInStore
                    o = mgr.GetKeyEntityFromCacheOrDB(id, tp)
                Case GetByIDOptions.GetAsIs
                    o = mgr.GetKeyEntityFromCacheOrCreate(id, tp)
                Case GetByIDOptions.EnsureLoadedFromStore
                    o = mgr.GetKeyEntityFromCacheLoadedOrDB(id, tp)
                Case Else
                    Throw New NotImplementedException
            End Select

            'If o IsNot Nothing Then
            '    If o.GetICreateManager Is Nothing Then
            '        o.SetCreateManager()
            '    End If
            'End If

            Return o
        End Using
    End Function
End Class

Public Class DataContext
    Inherits DataContextBase

    Private _del As ICreateManager
    Private _delEx As CreateManagerDelegateEx

    Public Sub New(ByVal getMgr As CreateManagerDelegate)
        _del = New CreateManager(getMgr)
    End Sub

    Public Sub New(ByVal getMgr As ICreateManager)
        _del = getMgr
    End Sub

    Public Sub New(stmtGen As StmtGenerator, ByVal getMgr As CreateManagerDelegateEx)
        MyBase.New(stmtGen)
        _delEx = getMgr
    End Sub

    Public Sub New(stmtGen As StmtGenerator, mpe As ObjectMappingEngine, ByVal getMgr As CreateManagerDelegateEx)
        MyBase.New(stmtGen, mpe)
        _delEx = getMgr
    End Sub

    Public Sub New(stmtGen As StmtGenerator, mpe As ObjectMappingEngine, cache As Cache.CacheBase, ByVal getMgr As CreateManagerDelegateEx)
        MyBase.New(stmtGen, mpe, cache)
        _delEx = getMgr
    End Sub

    Public Overrides Function CreateOrmManager() As OrmManager
        If _del IsNot Nothing Then
            Dim m As OrmManager = _del.CreateManager(Context)
            m.GetCreateManager = Me
            Return m
        ElseIf _delEx IsNot Nothing Then
            Dim m As OrmManager = _delEx(StmtGenerator, MappingEngine, Cache)
            m.GetCreateManager = Me
            Return m
        Else
            Throw New DataContextException("Manager is required")
        End If
    End Function

    Public Overrides ReadOnly Property Cache As Cache.CacheBase
        Get
            If _del IsNot Nothing Then
                Using mgr As OrmManager = _del.CreateManager(Me)
                    Return mgr.Cache
                End Using
            ElseIf _delEx IsNot Nothing Then
                Return _cache
            Else
                Throw New DataContextException("Manager is required")
            End If
        End Get
    End Property

    Public Overrides ReadOnly Property MappingEngine As ObjectMappingEngine
        Get
            If _del IsNot Nothing Then
                Using mgr As OrmManager = _del.CreateManager(Me)
                    Return mgr.MappingEngine
                End Using
            ElseIf _delEx IsNot Nothing Then
                Return _mpe
            Else
                Throw New DataContextException("Manager is required")
            End If

        End Get
    End Property

    Public Overrides ReadOnly Property StmtGenerator As StmtGenerator
        Get
            If _del IsNot Nothing Then
                Using mgr As OrmManager = _del.CreateManager(Me)
                    Return mgr.StmtGenerator
                End Using
            ElseIf _delEx IsNot Nothing Then
                Return _stmtGen
            Else
                Throw New DataContextException("Manager is required")
            End If

        End Get
    End Property
End Class