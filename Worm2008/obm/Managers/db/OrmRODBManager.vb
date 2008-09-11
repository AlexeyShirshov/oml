Imports Worm
Imports Worm.Sorting
Imports System.Collections.Generic
Imports Worm.Orm
Imports Worm.Criteria.Core
Imports Worm.Cache
Imports Worm.Orm.Query
Imports Worm.Orm.Meta
Imports Worm.Criteria.Joins
Imports Worm.Criteria.Values

Namespace Database
    Partial Public Class OrmReadOnlyDBManager
        Inherits OrmManagerBase

        Protected Friend Enum ConnAction
            Leave
            Destroy
            Close
        End Enum

#Region " Classes "

        Public Class BatchSaver
            Implements IDisposable

            Private _disposedValue As Boolean
            Private _objects As New List(Of ICachedEntity)
            Private _mgr As OrmReadOnlyDBManager
            Private _acceptInBatch As Boolean
            Private _callbacks As OrmCacheBase.IUpdateCacheCallbacks
            Private _save As Nullable(Of Boolean)
            'Private _disposeMgr As Boolean
            Private _commited As Boolean
            Private _lockList As New Dictionary(Of ObjectWrap(Of ICachedEntity), IDisposable)
            Private _removed As New List(Of ICachedEntity)
            Private _dontTrackRemoved As Boolean
            Private _startSave As Boolean
            Private _error As Boolean = True

#If DEBUG Then
            Friend _deleted As New List(Of ICachedEntity)
            Friend _updated As New List(Of ICachedEntity)
#End If

            Public Class CancelEventArgs
                Inherits EventArgs

                Private _cancel As Boolean
                Private _o As ICachedEntity

                Public Sub New(ByVal o As ICachedEntity)
                    _o = o
                End Sub

                Public Property Cancel() As Boolean
                    Get
                        Return _cancel
                    End Get
                    Set(ByVal value As Boolean)
                        _cancel = value
                    End Set
                End Property

                Public Property SavedObject() As ICachedEntity
                    Get
                        Return _o
                    End Get
                    Set(ByVal value As ICachedEntity)
                        _o = value
                    End Set
                End Property
            End Class

            Public Class CannotSaveEventArgs
                Inherits EventArgs

                Private _skip As Boolean
                Public Property Skip() As Boolean
                    Get
                        Return _skip
                    End Get
                    Set(ByVal value As Boolean)
                        _skip = value
                    End Set
                End Property

                Private _objs As List(Of ICachedEntity)
                Public ReadOnly Property Objects() As List(Of ICachedEntity)
                    Get
                        Return _objs
                    End Get
                End Property

                Public Sub New(ByVal objs As List(Of ICachedEntity))
                    _objs = objs
                End Sub

            End Class

            Public Event BeginSave(ByVal count As Integer)
            Public Event CannotSave(ByVal sender As BatchSaver, ByVal args As CannotSaveEventArgs)
            Public Event EndSave()
            Public Event ObjectSaving(ByVal sender As BatchSaver, ByVal args As CancelEventArgs)
            Public Event ObjectPostponed(ByVal o As ICachedEntity)
            Public Event ObjectSaved(ByVal o As ICachedEntity)
            Public Event ObjectAccepting(ByVal o As ICachedEntity)
            Public Event ObjectAccepted(ByVal o As ICachedEntity)
            Public Event ObjectRejecting(ByVal o As ICachedEntity)
            Public Event ObjectRejected(ByVal o As ICachedEntity, ByVal inLockList As Boolean)
            Public Event ObjectRestored(ByVal o As ICachedEntity)
            Public Event SaveSuccessed()
            Public Event SaveFailed()
            Public Event BeginRejecting()
            Public Event BeginAccepting()

            'Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal dispose As Boolean)
            '    _mgr = mgr
            '    _disposeMgr = dispose
            'End Sub

            Friend Sub New(ByVal mgr As OrmReadOnlyDBManager)
                _mgr = mgr
            End Sub

            'Public Sub New()
            '    _mgr = CType(OrmManagerBase.CurrentManager, OrmReadOnlyDBManager)
            'End Sub

            Public ReadOnly Property [Error]() As Boolean
                Get
                    Return _error
                End Get
            End Property

            Public ReadOnly Property AffectedObjects() As ICollection(Of ICachedEntity)
                Get
                    Dim l As New List(Of ICachedEntity)(_objects)
                    For Each o As ICachedEntity In _removed
                        l.Remove(o)
                    Next
                    Return l
                End Get
            End Property

            Public ReadOnly Property Manager() As OrmReadOnlyDBManager
                Get
                    Return _mgr
                End Get
            End Property

            Public Property IsCommit() As Boolean
                Get
                    If _save.HasValue Then
                        Return _save.Value
                    End If
                    Return False
                End Get
                Friend Set(ByVal value As Boolean)
                    _save = value
                End Set
            End Property

            Public Sub Commit()
                _save = True
            End Sub

            Public Sub Rollback()
                _save = False
            End Sub

            Public ReadOnly Property Commited() As Boolean
                Get
                    Return _commited
                End Get
            End Property

            Public Property AcceptInBatch() As Boolean
                Get
                    Return _acceptInBatch
                End Get
                Set(ByVal value As Boolean)
                    _acceptInBatch = value
                End Set
            End Property

            Public Property CacheCallbacks() As OrmCache.IUpdateCacheCallbacks
                Get
                    Return _callbacks
                End Get
                Set(ByVal value As OrmCache.IUpdateCacheCallbacks)
                    _callbacks = value
                End Set
            End Property

            Public Sub Add(ByVal o As CachedEntity)
                If Not _objects.Contains(o) Then
                    _objects.Add(o)
                    AddHandler o.OriginalCopyRemoved, AddressOf ObjRejected
#If DEBUG Then
                    Dim mo As ModifiedObject = _mgr.Cache.Modified(o)
                    If o.ObjectState = ObjectState.Deleted Then
                        _deleted.Add(o)
                        Debug.Assert(mo IsNot Nothing)
                        Debug.Assert(mo.Reason = ModifiedObject.ReasonEnum.Delete OrElse mo.Reason = ModifiedObject.ReasonEnum.Edit)
                    ElseIf o.ObjectState = ObjectState.Modified Then
                        _updated.Add(o)
                        Debug.Assert(mo IsNot Nothing)
                        Debug.Assert(mo.Reason = ModifiedObject.ReasonEnum.Edit)
                    End If
#End If
                End If
            End Sub

            Public Sub AddRange(ByVal col As ICollection(Of CachedEntity))
                '_objects.AddRange(col)
                'For Each o As OrmBase In col
                '    AddHandler o.OriginalCopyRemoved, AddressOf ObjRejected
                'Next
                For Each o As CachedEntity In col
                    Add(o)
                Next
            End Sub

            Protected Sub ObjRejected(ByVal o As ICachedEntity)
                If Not _startSave Then _removed.Add(o)
            End Sub

            Protected Function GetObjWrap(ByVal obj As ICachedEntity) As ObjectWrap(Of ICachedEntity)
                For Each o As ObjectWrap(Of ICachedEntity) In _lockList.Keys
                    If ReferenceEquals(o.Value, obj) Then
                        Return o
                    End If
                Next
                Throw New InvalidOperationException("Cannot find object")
            End Function

            Protected Sub ObjectAcceptedHandler(ByVal obj As ICachedEntity) Handles Me.ObjectAccepted
                Dim o As ObjectWrap(Of ICachedEntity) = GetObjWrap(obj)
                _lockList(o).Dispose()
                _lockList.Remove(o)
            End Sub

            Protected Sub ObjectRejectedHandler(ByVal obj As ICachedEntity, ByVal inLockList As Boolean) Handles Me.ObjectRejected
                If inLockList Then
                    Dim o As ObjectWrap(Of ICachedEntity) = GetObjWrap(obj)
                    _lockList(o).Dispose()
                    _lockList.Remove(o)
                End If
            End Sub

            Protected Function SaveObj(ByVal o As ICachedEntity, ByVal need2save As List(Of ICachedEntity), ByVal saved As List(Of Pair(Of ObjectState, ICachedEntity))) As Boolean
                Try
                    Dim args As New CancelEventArgs(o)
                    RaiseEvent ObjectSaving(Me, args)
                    If Not args.Cancel Then
                        Dim owr As New ObjectWrap(Of ICachedEntity)(o)
                        _lockList.Add(owr, _mgr.GetSyncForSave(o.GetType, o)) 'o.GetSyncRoot)
                        Dim os As ObjectState = o.ObjectState
                        If o.SaveChanges(False) Then
                            _lockList(owr).Dispose()
                            _lockList.Remove(owr)
                            need2save.Add(o)
                            RaiseEvent ObjectPostponed(o)
                        Else
                            saved.Add(New Pair(Of ObjectState, ICachedEntity)(os, o))
                            RaiseEvent ObjectSaved(o)
                        End If
                    End If
                    Return args.Cancel
                Catch ex As Exception
                    Throw New OrmManagerException("Error during save " & o.ObjName, ex)
                End Try
            End Function

            Protected Sub Save()
                Dim hasTransaction As Boolean = _mgr.Transaction IsNot Nothing
                Dim saved As New List(Of Pair(Of ObjectState, ICachedEntity)), copies As New List(Of Pair(Of ICachedEntity))
                Dim rejectList As New List(Of ICachedEntity), need2save As New List(Of ICachedEntity)
                _startSave = True
#If DebugLocks Then
                Using New CSScopeMgr_Debug(_mgr.Cache, "d:\temp\")
#Else
                Using New CSScopeMgr(_mgr.Cache)
#End If
                    _mgr.BeginTransaction()
                    Try
                        RaiseEvent BeginSave(_objects.Count)
                        For Each o As ICachedEntity In _objects
                            Dim pp As Pair(Of ICachedEntity) = Nothing
                            If o.ObjectState = ObjectState.Created Then
                                rejectList.Add(o)
                            ElseIf o.ObjectState = ObjectState.Modified Then
                                pp = New Pair(Of ICachedEntity)(o, CType(o.CreateClone, ICachedEntity))
                                copies.Add(pp)
                            End If
                            If SaveObj(o, need2save, saved) Then
                                rejectList.Remove(o)
                                If pp IsNot Nothing Then
                                    copies.Remove(pp)
                                End If
                            End If
                            'Try
                            '    Dim args As New CancelEventArgs(o)
                            '    RaiseEvent ObjectSaving(Me, args)
                            '    If Not args.Cancel Then
                            '        _lockList.Add(New ObjectWrap(Of OrmBase)(o), _mgr.GetSyncForSave(o.GetType, o)) 'o.GetSyncRoot)
                            '        Dim os As ObjectState = o.ObjectState
                            '        If o.SaveChanges(False) Then
                            '            need2save.Add(o)
                            '        Else
                            '            saved.Add(New Pair(Of ObjectState, OrmBase)(os, o))
                            '            RaiseEvent ObjectSaved(o)
                            '        End If
                            '    End If

                            'Catch ex As Exception
                            '    Throw New OrmManagerException("Error during save " & o.ObjName, ex)
                            'End Try
                        Next

                        For i As Integer = 0 To 4
                            Dim ns As New List(Of ICachedEntity)(need2save)
                            need2save.Clear()
                            For Each o As ICachedEntity In ns
                                SaveObj(o, need2save, saved)
                            Next
                            If need2save.Count = 0 Then
                                Exit For
                            End If
                        Next
                        If need2save.Count > 0 Then
                            Dim args As New CannotSaveEventArgs(need2save)
                            RaiseEvent CannotSave(Me, args)
                            If Not args.Skip Then
                                Throw New OrmManagerException(String.Format("Cannot save object graph"))
                            End If
                        End If

                        RaiseEvent EndSave()
                        _error = False
                    Finally
                        Try
                            If _error Then
                                If Not hasTransaction Then
                                    _mgr.Rollback()
                                End If

                                RaiseEvent BeginRejecting()
                                Rollback(saved, rejectList, copies, need2save)
                                RaiseEvent SaveFailed()
                            Else
                                If Not hasTransaction Then
                                    _mgr.Commit()
                                    _commited = True
                                End If

                                RaiseEvent BeginAccepting()
                                If _acceptInBatch Then
                                    'Dim l As New Dictionary(Of OrmBase, OrmBase)
                                    Dim l2 As New Dictionary(Of Type, List(Of ICachedEntity))
                                    Dim val As New List(Of ICachedEntity)
                                    For Each p As Pair(Of ObjectState, ICachedEntity) In saved
                                        Dim o As ICachedEntity = p.Second
                                        RaiseEvent ObjectAccepting(o)
                                        Dim mo As ICachedEntity = o.AcceptChanges(False, OrmBase.IsGoodState(p.First))
                                        Debug.Assert(_mgr.Cache.Modified(o) Is Nothing)
                                        'l.Add(o, mo)
                                        RaiseEvent ObjectAccepted(o)
                                        If CType(o, _ICachedEntity).UpdateCtx.Added OrElse CType(o, _ICachedEntity).UpdateCtx.Deleted Then
                                            Dim ls As List(Of ICachedEntity) = Nothing
                                            If Not l2.TryGetValue(o.GetType, ls) Then
                                                ls = New List(Of ICachedEntity)
                                                l2.Add(o.GetType, ls)
                                            End If
                                            ls.Add(o)
                                        Else
                                            val.Add(o)
                                        End If
                                    Next
                                    For Each o As CachedEntity In val
                                        o.UpdateCacheAfterUpdate()
                                    Next
                                    For Each t As Type In l2.Keys
                                        Dim ls As List(Of ICachedEntity) = l2(t)
                                        '_mgr.Cache.UpdateCache(_mgr.ObjectSchema, ls, _mgr, _
                                        '    AddressOf OrmBase.Accept_AfterUpdateCache, l, _callbacks)
                                        _mgr.Cache.UpdateCache(_mgr.ObjectSchema, ls, _mgr, _
                                            AddressOf CachedEntity.ClearCacheFlags, Nothing, _callbacks)
                                    Next
                                Else
                                    For Each p As Pair(Of ObjectState, ICachedEntity) In saved
                                        Dim o As ICachedEntity = p.Second
                                        RaiseEvent ObjectAccepting(o)
                                        o.AcceptChanges(False, OrmBase.IsGoodState(p.First))
                                        Debug.Assert(_mgr.Cache.Modified(o) Is Nothing)
                                        RaiseEvent ObjectAccepted(o)
                                    Next
                                    For Each p As Pair(Of ObjectState, ICachedEntity) In saved
                                        Dim o As ICachedEntity = p.Second
                                        o.UpdateCache()
                                    Next
                                End If

                                RaiseEvent SaveSuccessed()
                            End If
                        Finally
                            Do While _lockList.Count > 0
                                Dim o As ObjectWrap(Of ICachedEntity) = Nothing
                                For Each kv As KeyValuePair(Of ObjectWrap(Of ICachedEntity), IDisposable) In _lockList
                                    o = kv.Key
                                    kv.Value.Dispose()
                                Next
                                _lockList.Remove(o)
                            Loop
                        End Try
                    End Try
                End Using
            End Sub

            Private Sub Rollback(ByVal saved As List(Of Pair(Of ObjectState, ICachedEntity)), _
                ByVal rejectList As List(Of ICachedEntity), ByVal copies As List(Of Pair(Of ICachedEntity)), ByVal need2save As List(Of ICachedEntity))
                For Each o As ICachedEntity In rejectList
                    RaiseEvent ObjectRejecting(o)
                    _dontTrackRemoved = True
                    o.RejectChanges()
                    _dontTrackRemoved = False
                    RaiseEvent ObjectRejected(o, Not need2save.Contains(o))
                Next
                For Each o As Pair(Of ICachedEntity) In copies
                    o.First.CopyBody(o.Second, o.First)
                    o.First.SetObjectState(o.Second.GetOldState)
                    Dim orm As _IOrmBase = TryCast(o.First, _IOrmBase)
                    If orm IsNot Nothing Then
                        orm.RejectM2MIntermidiate()
                    End If
                    RaiseEvent ObjectRestored(o.First)
                Next
                For Each p As Pair(Of ObjectState, ICachedEntity) In saved
                    Dim o As ICachedEntity = p.Second
                    If Not rejectList.Contains(o) Then
                        RaiseEvent ObjectRejecting(o)
                        Dim orm As _IOrmBase = TryCast(o, _IOrmBase)
                        If orm IsNot Nothing Then
                            orm.RejectM2MIntermidiate()
                        End If
                        RaiseEvent ObjectRejected(o, True)
                    End If
                Next
            End Sub

#Region " IDisposable Support "
            Protected Overrides Sub Finalize()
                Dispose(False)
            End Sub

            Protected Overridable Sub Dispose(ByVal disposing As Boolean)
                If Not Me._disposedValue Then
                    'If Not _save.HasValue Then
                    '    Throw New InvalidOperationException("You should commit or rollback Saver")
                    'End If
                    If disposing AndAlso _save.HasValue AndAlso _save.Value Then
                        Save()
                    End If
                    'If _disposeMgr Then
                    '    _mgr.Dispose()
                    'End If
                    _mgr._batchSaver = Nothing
                End If
                Me._disposedValue = True
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
                Dispose(True)
                GC.SuppressFinalize(Me)
            End Sub
#End Region

        End Class

        Public Class OrmTransactionalScope
            Implements IDisposable

            Private disposedValue As Boolean
            Private _disposing As Boolean
            'Private _objs As New List(Of OrmBase)
            Private _mgr As OrmManagerBase
            Private _saver As BatchSaver
            Private _rollbackChanges As Boolean = True
            Private _created As Boolean

            Public Event SaveComplete(ByVal logicalCommited As Boolean, ByVal dbCommit As Boolean)
            Public Event BeginRollback(ByVal count As Integer)

            Public Sub New()
                MyClass.New(CType(OrmManagerBase.CurrentManager, OrmReadOnlyDBManager))
            End Sub

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager)
                If mgr Is Nothing Then
                    Throw New ArgumentNullException("mgr")
                End If

                AddHandler mgr.BeginUpdate, AddressOf Add
                AddHandler mgr.BeginDelete, AddressOf Delete
                _saver = mgr.CreateBatchSaver(_created)
                _mgr = mgr
            End Sub

            Public ReadOnly Property IsCommit() As Boolean
                Get
                    Return _saver.IsCommit
                End Get
            End Property

            Public Sub Commit()
                _saver.Commit()
            End Sub

            Public Sub Rollback()
                _saver.Rollback()
            End Sub

            Public Property RollbackChanges() As Boolean
                Get
                    Return _rollbackChanges
                End Get
                Set(ByVal value As Boolean)
                    _rollbackChanges = value
                End Set
            End Property

            Public ReadOnly Property Saver() As BatchSaver
                Get
                    Return _saver
                End Get
            End Property

            Public Overridable Sub AddRange(ByVal objs As ICollection(Of CachedEntity))
                If objs Is Nothing Then
                    Throw New ArgumentNullException("objects")
                End If

                If _disposing Then
                    Throw New InvalidOperationException("Cannot add object during save")
                End If

                '_objs.AddRange(objs)
                _saver.AddRange(objs)
            End Sub

            Public Overridable Sub Add(ByVal obj As ICachedEntity)
                If obj Is Nothing Then
                    Throw New ArgumentNullException("object")
                End If

                If _disposing Then
                    Throw New InvalidOperationException("Cannot add object during save")
                End If

                '_objs.Add(obj)
                _saver.Add(CType(obj, CachedEntity))
            End Sub

            Protected Sub Delete(ByVal obj As ICachedEntity)
                If obj Is Nothing Then
                    Throw New ArgumentNullException("object")
                End If

                If _disposing Then
                    Throw New InvalidOperationException("Cannot add object during save")
                End If

                'If Not _objs.Contains(obj) Then
                '    _objs.Add(obj)
                _saver.Add(CType(obj, CachedEntity))
                'End If
            End Sub

            Public Function CreateNewObject(Of T As {IOrmBase, New})() As T
                If _mgr.NewObjectManager Is Nothing Then
                    Throw New InvalidOperationException("NewObjectManager is not set")
                End If

                Return CreateNewObject(Of T)(_mgr.NewObjectManager.GetIdentity(GetType(T)))
            End Function

            Public Overridable Function CreateNewObject(Of T As {_ICachedEntity, New})(ByVal pk() As Pair(Of String, Object)) As T
                If _mgr.NewObjectManager Is Nothing Then
                    Throw New InvalidOperationException("NewObjectManager is not set")
                End If
                Dim o As T = _mgr.CreateEntity(Of T)(pk)
                'Dim o As New T
                'o.Init(id, _mgr.Cache, _mgr.ObjectSchema)
                _mgr.NewObjectManager.AddNew(o)
                '_objs.Add(o)
                '_saver.Add(o)
                Add(o)
                Return o
            End Function

            Public Overridable Function CreateNewObject(Of T As {IOrmBase, New})(ByVal id As Object) As T
                If _mgr.NewObjectManager Is Nothing Then
                    Throw New InvalidOperationException("NewObjectManager is not set")
                End If
                Dim o As T = _mgr.CreateOrmBase(Of T)(id)
                'Dim o As New T
                'o.Init(id, _mgr.Cache, _mgr.ObjectSchema)
                _mgr.NewObjectManager.AddNew(o)
                '_objs.Add(o)
                '_saver.Add(o)
                Add(o)
                Return o
            End Function

            Public Function CreateNewObject(ByVal t As Type) As IOrmBase
                If _mgr.NewObjectManager Is Nothing Then
                    Throw New InvalidOperationException("NewObjectManager is not set")
                End If

                Return CreateNewObject(t, _mgr.NewObjectManager.GetIdentity(t))
            End Function

            Public Function CreateNewObject(ByVal t As Type, ByVal id As Object) As IOrmBase
                For Each mi As Reflection.MethodInfo In Me.GetType.GetMember("CreateNewObject", Reflection.MemberTypes.Method, _
                    Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public)
                    If mi.IsGenericMethod AndAlso mi.GetParameters.Length = 1 Then
                        mi = mi.MakeGenericMethod(New Type() {t})
                        Return CType(mi.Invoke(Me, New Object() {id}), IOrmBase)
                    End If
                Next
                Throw New InvalidOperationException("Cannot find method CreateNewObject")
            End Function

            Protected Sub _Rollback()
                If _rollbackChanges Then
                    RaiseEvent BeginRollback(_saver.AffectedObjects.Count)
                    'Debug.WriteLine("_rollback: " & _saver.AffectedObjects.Count)
                    For Each o As _ICachedEntity In _saver.AffectedObjects
                        'Debug.WriteLine("_rollback: " & o.ObjName)
                        If o.ObjectState = ObjectState.Created Then
                            If _mgr.NewObjectManager IsNot Nothing Then
                                _mgr.NewObjectManager.RemoveNew(o)
                            End If
                        Else
#If DEBUG Then
                            If o.HasChanges Then
                                'Debug.Assert(_mgr.Cache.Modified(o) IsNot Nothing)
                                If _mgr.Cache.Modified(o) IsNot Nothing Then
                                    If _mgr.Cache.Modified(o).Reason = ModifiedObject.ReasonEnum.Delete Then
                                        Debug.Assert(_saver._deleted.Contains(o))
                                        Debug.Assert(Not _saver._updated.Contains(o))
                                    ElseIf _mgr.Cache.Modified(o).Reason = ModifiedObject.ReasonEnum.Edit Then
                                        'If _mgr.Cache.Modified(o).Reason = ModifiedObject.ReasonEnum.Delete Then
                                        Debug.Assert(Not _saver._deleted.Contains(o))
                                        Debug.Assert(_saver._updated.Contains(o))
                                        'End If
                                    End If
                                End If
                            End If
#End If
                            o.RejectChanges()
                        End If
                    Next
                End If
            End Sub

#Region " IDisposable Support "
            ' IDisposable
            Protected Overridable Sub Dispose(ByVal disposing As Boolean)
                If Not Me.disposedValue AndAlso _created Then
                    Dim rlb As Boolean = True
                    Try
                        _disposing = True
                        _saver.Dispose()
                        'rlb = False
                    Finally
                        _disposing = False
                        If _saver.Error Then
                            _Rollback()
                        End If

                        RemoveHandler _mgr.BeginDelete, AddressOf Delete
                        RemoveHandler _mgr.BeginUpdate, AddressOf Add

                        Me.disposedValue = True
                    End Try
                    If Not rlb AndAlso Not _saver.IsCommit Then _Rollback()

                    RaiseEvent SaveComplete(_saver.IsCommit, _saver.Commited)
                End If
            End Sub

            ' This code added by Visual Basic to correctly implement the disposable pattern.
            Public Sub Dispose() Implements IDisposable.Dispose
                ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
                Dispose(True)
                GC.SuppressFinalize(Me)
            End Sub
#End Region

        End Class

#End Region

        Private _connStr As String
        Private _tran As System.Data.Common.DbTransaction
        Private _closeConnOnCommit As ConnAction
        Private _conn As System.Data.Common.DbConnection
        Private _exec As TimeSpan
        Private _fetch As TimeSpan
        Private _batchSaver As BatchSaver
        Private Shared _tsStmt As New TraceSource("Worm.Diagnostics.DB.Stmt", SourceLevels.Information)

        Protected Shared _LoadMultipleObjectsMI As Reflection.MethodInfo = Nothing
        Protected Shared _LoadMultipleObjectsMI4 As Reflection.MethodInfo = Nothing

        Public Sub New(ByVal cache As OrmCacheBase, ByVal schema As SQLGenerator, ByVal connectionString As String)
            MyBase.New(cache, schema)
            _connStr = connectionString
        End Sub

        Protected Sub New(ByVal schema As SQLGenerator, ByVal connectionString As String)
            MyBase.New(schema)

            _connStr = connectionString
        End Sub

        Public Shared ReadOnly Property StmtSource() As TraceSource
            Get
                Return _tsStmt
            End Get
        End Property

        Public Function CreateBatchSaver(ByRef createdNew As Boolean) As BatchSaver
            createdNew = False
            If _batchSaver Is Nothing Then
                _batchSaver = New BatchSaver(Me)
                createdNew = True
            End If
            Return _batchSaver
        End Function

        Public Overridable ReadOnly Property AlwaysAdd2Cache() As Boolean
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property DbSchema() As SQLGenerator
            Get
                Return CType(_schema, SQLGenerator)
            End Get
        End Property

        Protected Function CreateConn() As System.Data.Common.DbConnection
            Return DbSchema.CreateConnection(_connStr)
        End Function

        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            Try
                If Not _disposed Then
                    If _conn IsNot Nothing Then Rollback()
                End If
            Finally
                MyBase.Dispose(disposing)
            End Try
        End Sub

        Public ReadOnly Property Transaction() As System.Data.Common.DbTransaction
            Get
                Return _tran
            End Get
        End Property

        Public Function BeginTransaction(ByVal mode As System.Data.IsolationLevel) As System.Data.Common.DbTransaction
            If _conn Is Nothing Then
                _conn = CreateConn()
                _closeConnOnCommit = ConnAction.Destroy
                _conn.Open()
                _tran = _conn.BeginTransaction(mode)
            ElseIf _tran Is Nothing Then
                Dim cs As System.Data.ConnectionState = _conn.State
                If cs = System.Data.ConnectionState.Broken OrElse cs = System.Data.ConnectionState.Closed Then
                    _closeConnOnCommit = ConnAction.Close
                    _conn.Open()
                End If
                _tran = _conn.BeginTransaction(mode)
            End If

            Return _tran
        End Function

        Public Function BeginTransaction() As System.Data.Common.DbTransaction
            If _conn Is Nothing Then
                _conn = CreateConn()
                _closeConnOnCommit = ConnAction.Destroy
                _conn.Open()
                _tran = _conn.BeginTransaction()
            ElseIf _tran Is Nothing Then
                Dim cs As System.Data.ConnectionState = _conn.State
                If cs = System.Data.ConnectionState.Broken OrElse cs = System.Data.ConnectionState.Closed Then
                    _closeConnOnCommit = ConnAction.Close
                    _conn.Open()
                End If
                _tran = _conn.BeginTransaction
            End If

            Return _tran
        End Function

        Public Sub Commit()
            Assert(_conn IsNot Nothing, "Commit operation requires connection")

            If _tran IsNot Nothing Then
                _tran.Commit()
                _tran.Dispose()
                _tran = Nothing

                Select Case _closeConnOnCommit
                    Case ConnAction.Close
                        _conn.Close()
                    Case ConnAction.Destroy
                        _conn.Dispose()
                        _conn = Nothing
                End Select
            End If
        End Sub

        ''' <summary>
        ''' Отменяет изменения, выполненные в транзакции
        ''' </summary>
        ''' <remarks>Состояние измененных объектов после отмены изменений на уровне БД не меняется, так что нужно в ручную снова выполнить загрузку их из БД</remarks>
        Public Sub Rollback()
            Assert(_conn IsNot Nothing, "Rollback operation requires connection")

            If _tran IsNot Nothing Then
                _tran.Rollback()
                _tran.Dispose()
                _tran = Nothing

                Select Case _closeConnOnCommit
                    Case ConnAction.Close
                        _conn.Close()
                    Case ConnAction.Destroy
                        _conn.Dispose()
                        _conn = Nothing
                End Select
            End If
        End Sub

        Protected Friend Overrides ReadOnly Property IdentityString() As String
            Get
                Return MyBase.IdentityString & _connStr
            End Get
        End Property

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, IOrmBase})(ByVal relation As M2MRelation, ByVal filter As IFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String) As OrmManagerBase.ICustDelegate(Of T)
            Return New DistinctRelationFilterCustDelegate(Of T)(Me, relation, CType(filter, IFilter), sort, key, id)
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, IOrmBase})(ByVal aspect As QueryAspect, ByVal join() As Worm.Criteria.Joins.OrmJoin, ByVal filter As IFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String, Optional ByVal cols As List(Of ColumnAttribute) = Nothing) As OrmManagerBase.ICustDelegate(Of T)
            Return New JoinCustDelegate(Of T)(Me, join, filter, sort, key, id, aspect, cols)
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, IOrmBase})(ByVal filter As IFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String) As OrmManagerBase.ICustDelegate(Of T)
            Return New FilterCustDelegate(Of T)(Me, filter, sort, key, id)
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, IOrmBase})(ByVal filter As IFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String, ByVal cols() As String) As OrmManagerBase.ICustDelegate(Of T)
            If cols Is Nothing Then
                Throw New ArgumentNullException("cols")
            End If
            Dim l As New List(Of ColumnAttribute)
            Dim has_id As Boolean = False
            For Each c As String In cols
                Dim col As ColumnAttribute = DbSchema.GetColumnByFieldName(GetType(T), c)
                If col Is Nothing Then
                    Throw New ArgumentException("Invalid column name " & c)
                End If
                If c = "ID" Then
                    has_id = True
                End If
                l.Add(col)
            Next
            If Not has_id Then
                l.Add(DbSchema.GetColumnByFieldName(GetType(T), "ID"))
            End If
            Return New FilterCustDelegate(Of T)(Me, CType(filter, IFilter), l, sort, key, id)
        End Function

        'Protected Overrides Function GetCustDelegate4Top(Of T As {New, OrmBase})(ByVal top As Integer, ByVal filter As IOrmFilter, _
        '    ByVal sort As Sort, ByVal key As String, ByVal id As String) As OrmManagerBase.ICustDelegate(Of T)
        '    Return New FilterCustDelegate4Top(Of T)(Me, top, filter, sort, key, id)
        'End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T2 As {New, IOrmBase})( _
            ByVal obj As _IOrmBase, ByVal filter As IFilter, ByVal sort As Sort, ByVal queryAscpect() As QueryAspect, _
            ByVal id As String, ByVal key As String, ByVal direct As String) As OrmManagerBase.ICustDelegate(Of T2)
            Return New M2MDataProvider(Of T2)(Me, obj, CType(filter, IFilter), sort, queryAscpect, id, key, direct)
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T2 As {New, IOrmBase})( _
            ByVal obj As _IOrmBase, ByVal filter As IFilter, ByVal sort As Sort, _
            ByVal id As String, ByVal key As String, ByVal direct As String) As OrmManagerBase.ICustDelegate(Of T2)
            Return New M2MDataProvider(Of T2)(Me, obj, CType(filter, IFilter), sort, New QueryAspect() {}, id, key, direct)
        End Function

        'Protected Overrides Function GetCustDelegateTag(Of T As {New, OrmBase})( _
        '    ByVal obj As T, ByVal filter As IOrmFilter, ByVal sort As String, ByVal sortType As SortType, _
        '    ByVal id As String, ByVal sync As String, ByVal key As String) As OrmManagerBase.ICustDelegate(Of T)
        '    '    Return New M2MCustDelegate(Of T)(Me, obj, filter, sort, sortType, id, sync, key, True)
        '    Throw New NotImplementedException
        'End Function

        Protected Function FindConnected(ByVal ct As Type, ByVal selectedType As Type, _
            ByVal filterType As Type, ByVal connectedFilter As IFilter, _
            ByVal filter As IFilter, ByVal withLoad As Boolean, _
            ByVal sort As Sort, ByVal q() As QueryAspect) As IList
            Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                Dim arr As Generic.List(Of ColumnAttribute) = Nothing

                With cmd
                    .CommandType = System.Data.CommandType.Text

                    Dim almgr As AliasMgr = AliasMgr.Create
                    Dim params As New ParamMgr(DbSchema, "p")
                    Dim schema2 As IOrmObjectSchema = DbSchema.GetObjectSchema(selectedType)
                    Dim cs As IOrmObjectSchema = DbSchema.GetObjectSchema(ct)
                    Dim mms As IConnectedFilter = TryCast(cs, IConnectedFilter)
                    Dim cfi As Object = GetFilterInfo()
                    If mms IsNot Nothing Then
                        cfi = mms.ModifyFilterInfo(cfi, selectedType, filterType)
                    End If
                    'Dim r1 As M2MRelation = Schema.GetM2MRelation(selectedType, filterType)
                    Dim r2 As M2MRelation = DbSchema.GetM2MRelation(filterType, selectedType, True)
                    Dim id_clm As String = r2.Column

                    Dim sb As New StringBuilder
                    arr = ObjectSchema.GetSortedFieldList(ct)
                    If withLoad Then
                        'Dim jc As New OrmFilter(ct, field, selectedType, "ID", FilterOperation.Equal)
                        'Dim j As New OrmJoin(Schema.GetTables(selectedType)(0), JoinType.Join, jc)
                        'Dim js As New List(Of OrmJoin)
                        'js.Add(j)
                        'js.AddRange(Schema.GetAllJoins(selectedType))
                        Dim columns As String = DbSchema.GetSelectColumnList(selectedType, Nothing, Nothing, schema2)
                        sb.Append(DbSchema.Select(ct, almgr, params, q, arr, columns, cfi))
                    Else
                        sb.Append(DbSchema.Select(ct, almgr, params, q, arr, Nothing, cfi))
                    End If
                    'If withLoad Then
                    '    arr = DatabaseSchema.GetSortedFieldList(ct)
                    '    sb.Append(Schema.Select(ct, almgr, params, arr))
                    'Else
                    '    arr = New Generic.List(Of ColumnAttribute)
                    '    arr.Add(New ColumnAttribute("ID", Field2DbRelations.PK))
                    '    sb.Append(Schema.SelectID(ct, almgr, params))
                    'End If
                    Dim appendMainTable As Boolean = filter IsNot Nothing OrElse schema2.GetFilter(GetFilterInfo) IsNot Nothing OrElse withLoad OrElse (sort IsNot Nothing AndAlso Not sort.IsExternal) OrElse SQLGenerator.NeedJoin(schema2)
                    'Dim table As String = schema2.GetTables(0)
                    DbSchema.AppendNativeTypeJoins(selectedType, almgr, schema2.GetTables, sb, params, DbSchema.GetObjectSchema(ct).GetTables(0), id_clm, appendMainTable, GetFilterInfo)
                    If withLoad Then
                        For Each tbl As SourceFragment In schema2.GetTables
                            If almgr.Aliases.ContainsKey(tbl) Then
                                'Dim [alias] As String = almgr.Aliases(tbl)
                                'sb = sb.Replace(tbl.TableName & ".", [alias] & ".")
                                almgr.Replace(DbSchema, tbl, sb)
                            End If
                        Next
                    End If
                    Dim con As New Database.Criteria.Conditions.Condition.ConditionConstructor
                    con.AddFilter(connectedFilter)
                    con.AddFilter(filter)
                    con.AddFilter(schema2.GetFilter(GetFilterInfo))
                    DbSchema.AppendWhere(ct, con.Condition, almgr, sb, cfi, params)

                    If sort IsNot Nothing AndAlso Not sort.IsExternal Then
                        DbSchema.AppendOrder(selectedType, sort, almgr, sb, True, Nothing, Nothing)
                    End If

                    params.AppendParams(.Parameters)
                    .CommandText = sb.ToString
                End With

                Dim values As IList = CType(GetType(List(Of )).MakeGenericType(New Type() {ct}).InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), System.Collections.IList)
                If withLoad Then
                    LoadMultipleObjects(ct, selectedType, cmd, values, Nothing, Nothing)
                Else
                    LoadMultipleObjects(ct, cmd, True, values, arr)
                End If
                Return values
            End Using
        End Function

        'Protected Function FindConnected(ByVal ct As Type, ByVal selectedType As Type, ByVal filterType As Type, _
        '    ByVal connectedFilter As IOrmFilter, ByVal filter As IOrmFilter, ByVal withLoad As Boolean) As IList
        '    Using cmd As System.Data.Common.DbCommand = Schema.CreateDBCommand
        '        Dim arr As Generic.List(Of ColumnAttribute) = Nothing

        '        With cmd
        '            .CommandType = System.Data.CommandType.Text

        '            Dim almgr As AliasMgr = AliasMgr.Create
        '            Dim params As New ParamMgr(Schema, "p")
        '            Dim sb As New StringBuilder
        '            If withLoad Then
        '                arr = DatabaseSchema.GetSortedFieldList(ct)
        '                sb.Append(Schema.Select(ct, almgr, params, arr))
        '            Else
        '                arr = New Generic.List(Of ColumnAttribute)
        '                arr.Add(New ColumnAttribute("ID", Field2DbRelations.PK))
        '                sb.Append(Schema.SelectID(ct, almgr, params))
        '            End If
        '            Dim schema2 As IOrmObjectSchema = Schema.GetObjectSchema(selectedType)
        '            'Dim schema1 As IOrmObjectSchema = Schema.GetObjectSchema(filterType)
        '            'Dim r1 As M2MRelation = Schema.GetM2MRelation(selectedType, filterType)
        '            Dim r2 As M2MRelation = Schema.GetM2MRelation(filterType, selectedType)
        '            Dim id_clm As String = r2.Column
        '            Dim appendMainTable As Boolean = filter IsNot Nothing OrElse schema2.GetFilter(GetFilterInfo) IsNot Nothing
        '            'Dim table As String = schema2.GetTables(0)
        '            Schema.AppendJoins(selectedType, almgr, schema2.GetTables, sb, params, Schema.GetObjectSchema(ct).GetTables(0), id_clm, appendMainTable)

        '            Dim con As New OrmCondition.OrmConditionConstructor
        '            con.AddFilter(connectedFilter)
        '            con.AddFilter(filter)
        '            con.AddFilter(schema2.GetFilter(GetFilterInfo))
        '            Schema.AppendWhere(ct, con.Condition, almgr, sb, GetFilterInfo, params)

        '            params.AppendParams(.Parameters)
        '            .CommandText = sb.ToString
        '        End With

        '        Return LoadMultipleObjects(ct, cmd, withLoad, arr)
        '    End Using
        'End Function

        Protected Sub LoadMultipleObjects(ByVal firstType As Type, _
            ByVal secondType As Type, ByVal cmd As System.Data.Common.DbCommand, ByVal values As IList, _
            ByVal first_cols As List(Of ColumnAttribute), ByVal sec_cols As List(Of ColumnAttribute))

            'Dim values As IList
            'values = CType(GetType(List(Of )).MakeGenericType(New Type() {firstType}).InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), System.Collections.IList)
            If first_cols Is Nothing Then
                first_cols = DbSchema.GetSortedFieldList(firstType)
            End If

            If sec_cols Is Nothing Then
                sec_cols = DbSchema.GetSortedFieldList(secondType)
            End If

            Dim b As ConnAction = TestConn(cmd)
            Try
                _cache.BeginTrackDelete(firstType)
                _cache.BeginTrackDelete(secondType)

                Dim et As New PerfCounter
                Using dr As System.Data.IDataReader = cmd.ExecuteReader
                    _exec = et.GetTime
                    Dim firstidx As Integer = 0
                    Dim ss() As String = _schema.GetPrimaryKeysName(firstType, False)
                    If ss.Length > 1 Then
                        Throw New OrmManagerException("Connected type must use single primary key")
                    End If
                    Dim pk_name As String = ss(0)
                    Try
                        firstidx = dr.GetOrdinal(pk_name)
                    Catch ex As IndexOutOfRangeException
                        If _mcSwitch.TraceError Then
                            Trace.WriteLine("Invalid column name " & pk_name & " in " & cmd.CommandText)
                            Trace.WriteLine(Environment.StackTrace)
                        End If
                        Throw New OrmManagerException("Cannot get first primary key ordinal", ex)
                    End Try

                    Dim secidx As Integer = 0
                    ss = _schema.GetPrimaryKeysName(secondType, False)
                    If ss.Length > 1 Then
                        Throw New OrmManagerException("Connected type must use single primary key")
                    End If
                    Dim pk2_name As String = ss(0)
                    Try
                        secidx = dr.GetOrdinal(pk2_name)
                    Catch ex As IndexOutOfRangeException
                        If _mcSwitch.TraceError Then
                            Trace.WriteLine("Invalid column name " & pk2_name & " in " & cmd.CommandText)
                            Trace.WriteLine(Environment.StackTrace)
                        End If
                        Throw New OrmManagerException("Cannot get second primary key ordinal", ex)
                    End Try

                    Dim dic1 As IDictionary = GetDictionary(firstType)
                    Dim dic2 As IDictionary = GetDictionary(secondType)
                    Dim oschema As IOrmObjectSchema = DbSchema.GetObjectSchema(firstType)
                    Dim oschema2 As IOrmObjectSchema = DbSchema.GetObjectSchema(secondType)
                    Dim ft As New PerfCounter
                    Do While dr.Read
                        Dim id1 As Object = dr.GetValue(firstidx)
                        Dim obj1 As IOrmBase = GetOrmBaseFromCacheOrCreate(id1, firstType)
                        If Not _cache.IsDeleted(obj1) AndAlso obj1.ObjectState <> ObjectState.Modified Then
                            Using obj1.GetSyncRoot()
                                'If obj1.IsLoaded Then obj1.IsLoaded = False
                                Dim lock As IDisposable = Nothing
                                Try
                                    Dim ro As _IEntity = LoadFromDataReader(obj1, dr, first_cols, False, 0, dic1, True, lock, oschema, oschema.GetFieldColumnMap)
                                    AfterLoadingProcess(dic1, obj1, lock, ro)
                                    values.Add(ro)
                                Finally
                                    If lock IsNot Nothing Then
                                        lock.Dispose()
                                    End If
                                End Try
                            End Using
                        Else
                            values.Add(obj1)
                        End If

                        Dim id2 As Object = dr.GetValue(secidx)
                        Dim obj2 As IOrmBase = GetOrmBaseFromCacheOrCreate(id2, secondType)
                        If Not _cache.IsDeleted(obj2) Then
                            If obj2.ObjectState <> ObjectState.Modified Then
                                Using obj2.GetSyncRoot()
                                    'If obj2.IsLoaded Then obj2.IsLoaded = False
                                    Dim lock As IDisposable = Nothing
                                    Try
                                        Dim ro2 As _IEntity = LoadFromDataReader(obj2, dr, sec_cols, False, first_cols.Count, dic2, True, lock, oschema2, oschema2.GetFieldColumnMap)
                                        AfterLoadingProcess(dic2, obj2, lock, ro2)
                                    Finally
                                        If lock IsNot Nothing Then
                                            'Threading.Monitor.Exit(dic2)
                                            lock.Dispose()
                                        End If
                                    End Try
                                End Using
                            End If
                        End If
                    Loop
                    _fetch = ft.GetTime

                End Using
            Finally
                _cache.EndTrackDelete(secondType)
                _cache.EndTrackDelete(firstType)
                CloseConn(b)
            End Try

        End Sub

        Public Sub LoadMultipleObjects(ByVal t As Type, _
            ByVal cmd As System.Data.Common.DbCommand, _
            ByVal withLoad As Boolean, _
            ByVal values As IList, ByVal arr As Generic.List(Of ColumnAttribute))

            Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance

            If _LoadMultipleObjectsMI4 Is Nothing Then
                For Each mi2 As Reflection.MethodInfo In Me.GetType.GetMethods(flags)
                    If mi2.Name = "LoadMultipleObjects" AndAlso mi2.IsGenericMethod AndAlso mi2.GetParameters.Length = 4 Then
                        _LoadMultipleObjectsMI4 = mi2
                        Exit For
                    End If
                Next

                If _LoadMultipleObjectsMI4 Is Nothing Then
                    Throw New OrmManagerException("Cannot find method LoadMultipleObjects")
                End If
            End If

            Dim mi_real As Reflection.MethodInfo = _LoadMultipleObjectsMI4.MakeGenericMethod(New Type() {t})

            mi_real.Invoke(Me, flags, Nothing, _
                New Object() {cmd, withLoad, values, arr}, Nothing)

        End Sub

        Public Sub LoadMultipleObjects(ByVal t As Type, _
            ByVal cmd As System.Data.Common.DbCommand, _
            ByVal withLoad As Boolean, _
            ByVal values As IList, ByVal arr As Generic.List(Of ColumnAttribute), _
            ByVal oschema As IOrmObjectSchemaBase, _
            ByVal fields_idx As Collections.IndexedCollection(Of String, MapField2Column))
            'Dim ltg As Type = GetType(IList(Of ))
            'Dim lt As Type = ltg.MakeGenericType(New Type() {t})
            Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance

            If _LoadMultipleObjectsMI Is Nothing Then
                For Each mi2 As Reflection.MethodInfo In Me.GetType.GetMethods(flags)
                    If mi2.Name = "LoadMultipleObjects" AndAlso mi2.IsGenericMethod AndAlso mi2.GetParameters.Length = 6 Then
                        _LoadMultipleObjectsMI = mi2
                        Exit For
                    End If
                Next

                If _LoadMultipleObjectsMI Is Nothing Then
                    Throw New OrmManagerException("Cannot find method LoadMultipleObjects")
                End If
            End If

            Dim mi_real As Reflection.MethodInfo = _LoadMultipleObjectsMI.MakeGenericMethod(New Type() {t})

            mi_real.Invoke(Me, flags, Nothing, _
                New Object() {cmd, withLoad, values, arr, oschema, fields_idx}, Nothing)

        End Sub

        'Protected Overrides Function GetDataTableInternal(ByVal t As System.Type, ByVal obj As OrmBase, _
        '    ByVal filter As IOrmFilter, ByVal appendJoins As Boolean, Optional ByVal tag As String = Nothing) As System.Data.DataTable

        '    Throw New NotSupportedException

        '    'If obj Is Nothing Then
        '    '    Throw New ArgumentNullException("obj")
        '    'End If

        '    'Dim mt As Type = obj.GetType
        '    'Dim ct As Type = Schema.GetConnectedType(mt, t)
        '    'If ct IsNot Nothing Then
        '    '    Dim f1 As String = Schema.GetConnectedTypeField(ct, mt)
        '    '    Dim f2 As String = Schema.GetConnectedTypeField(ct, t)
        '    '    Dim fl As New OrmFilter(ct, f1, obj, FilterOperation.Equal)
        '    '    Dim dt As New System.Data.DataTable()
        '    '    dt.TableName = "table1"
        '    '    dt.Locale = System.Globalization.CultureInfo.CurrentCulture
        '    '    Dim col1 As String = mt.Name & "ID"
        '    '    Dim col2 As String = t.Name & "ID"
        '    '    dt.Columns.Add(col1, GetType(Integer))
        '    '    dt.Columns.Add(col2, GetType(Integer))

        '    '    For Each o As OrmBase In FindConnected(ct, t, mt, fl, filter, True)
        '    '        Dim id1 As Integer = CType(Schema.GetFieldValue(o, f1), OrmBase).Identifier
        '    '        Dim id2 As Integer = CType(Schema.GetFieldValue(o, f2), OrmBase).Identifier
        '    '        Dim dr As System.Data.DataRow = dt.NewRow
        '    '        dr(col1) = id1
        '    '        dr(col2) = id2
        '    '        dt.Rows.Add(dr)
        '    '    Next

        '    '    Return dt
        '    'Else
        '    '    Using cmd As System.Data.Common.DbCommand = Schema.CreateDBCommand
        '    '        With cmd
        '    '            .CommandType = System.Data.CommandType.Text
        '    '            Dim params As IList(Of System.Data.Common.DbParameter) = Nothing
        '    '            .CommandText = Schema.SelectM2M(obj, t, filter, GetFilterInfo, appendJoins, params, tag)
        '    '            For Each p As System.Data.Common.DbParameter In params
        '    '                .Parameters.Add(p)
        '    '            Next
        '    '        End With

        '    '        Dim b As ConnAction = TestConn(cmd)
        '    '        Try
        '    '            Using da As System.Data.Common.DbDataAdapter = Schema.CreateDataAdapter()

        '    '                da.SelectCommand = cmd
        '    '                da.TableMappings.Add(CType(Schema.GetJoinSelectMapping(mt, t), ICloneable).Clone)
        '    '                Dim dt As New System.Data.DataTable()
        '    '                dt.TableName = "table1"
        '    '                dt.Locale = System.Globalization.CultureInfo.CurrentCulture
        '    '                da.Fill(dt)
        '    '                Return dt
        '    '            End Using
        '    '        Finally
        '    '            CloseConn(b)
        '    '        End Try
        '    '    End Using
        '    'End If

        'End Function

        Protected Overrides Function GetObjects(Of T As {IOrmBase, New})(ByVal type As Type, ByVal ids As Generic.IList(Of Object), ByVal f As IFilter, _
            ByVal relation As M2MRelation, ByVal idsSorted As Boolean, ByVal withLoad As Boolean) As IDictionary(Of Object, EditableList)
            Invariant()

            If ids Is Nothing Then
                Throw New ArgumentNullException("ids")
            End If

            If ids.Count < 1 Then
                Return Nothing
            End If

            Dim almgr As AliasMgr = AliasMgr.Create
            Dim params As New ParamMgr(DbSchema, "p")
            'Dim arr As Generic.List(Of ColumnAttribute) = Nothing
            Dim sb As New StringBuilder
            Dim type2load As Type = GetType(T)
            Dim ct As Type = DbSchema.GetConnectedType(type, type2load)
            Dim direct As String = relation.Key

            'Dim dt As New System.Data.DataTable()
            'dt.TableName = "table1"
            'dt.Locale = System.Globalization.CultureInfo.CurrentCulture

            Dim edic As New Dictionary(Of Object, EditableList)

            If ct IsNot Nothing Then
                'If Not direct Then
                '    Throw New NotSupportedException("Tag is not supported with connected type")
                'End If

                'Dim oschema2 As IOrmObjectSchema = DbSchema.GetObjectSchema(type2load)
                'Dim r2 As M2MRelation = DbSchema.GetM2MRelation(type2load, type, direct)
                Dim f1 As String = DbSchema.GetConnectedTypeField(ct, type, M2MRelation.GetRevKey(direct))
                Dim f2 As String = DbSchema.GetConnectedTypeField(ct, type2load, direct)
                'Dim col1 As String = type.Name & "ID"
                'Dim col2 As String = orig_type.Name & "ID"
                'dt.Columns.Add(col1, GetType(Integer))
                'dt.Columns.Add(col2, GetType(Integer))
                Dim oschema As IOrmObjectSchema = DbSchema.GetObjectSchema(ct)

                For Each o As IOrmBase In GetObjects(ct, ids, f, withLoad, f1, idsSorted)
                    'Dim o1 As OrmBase = CType(DbSchema.GetFieldValue(o, f1), OrmBase)
                    'Dim o2 As OrmBase = CType(DbSchema.GetFieldValue(o, f2), OrmBase)
                    Dim o1 As IOrmBase = CType(o.GetValue(Nothing, New ColumnAttribute(f1), oschema), IOrmBase)
                    Dim o2 As IOrmBase = CType(o.GetValue(Nothing, New ColumnAttribute(f2), oschema), IOrmBase)

                    Dim id1 As Object = o1.Identifier
                    Dim id2 As Object = o2.Identifier
                    'Dim k As Integer = o1.Identifier
                    'Dim v As Integer = o2.Identifier
                    If o2.GetType Is type Then
                        id1 = o2.Identifier
                        id2 = o1.Identifier
                    End If

                    Dim el As EditableList = Nothing
                    If edic.TryGetValue(id1, el) Then
                        el.Add(id2)
                    Else
                        Dim l As New List(Of Object)
                        l.Add(id2)
                        el = New EditableList(id1, l, type, type2load, Nothing)
                        edic.Add(id1, el)
                    End If
                Next
            Else
                Dim oschema2 As IOrmObjectSchema = DbSchema.GetObjectSchema(type2load)
                Dim r2 As M2MRelation = DbSchema.GetM2MRelation(type2load, type, direct)
                Dim appendMainTable As Boolean = f IsNot Nothing OrElse oschema2.GetFilter(GetFilterInfo) IsNot Nothing
                sb.Append(DbSchema.SelectM2M(type2load, type, New QueryAspect() {}, appendMainTable, True, GetFilterInfo, params, almgr, withLoad, direct))

                If Not DbSchema.AppendWhere(type2load, CType(f, IFilter), almgr, sb, GetFilterInfo, params) Then
                    sb.Append(" where 1=1 ")
                End If
                Dim dic As IDictionary = CType(GetDictionary(Of T)(), System.Collections.IDictionary)

                Dim pcnt As Integer = params.Params.Count
                Dim nidx As Integer = pcnt
                For Each cmd_str As Pair(Of String, Integer) In GetFilters(CType(ids, List(Of Object)), relation.Table, r2.Column, almgr, params, idsSorted)
                    Dim sb_cmd As New StringBuilder
                    sb_cmd.Append(sb.ToString).Append(cmd_str.First)
                    'Dim msort As Boolean = False
                    'If Not String.IsNullOrEmpty(sort) AndAlso Schema.GetObjectSchema(original_type).IsExternalSort(sort) Then
                    '    msort = True
                    'Else
                    '    Schema.AppendOrder(original_type, sort, sortType, almgr, sb_cmd)
                    'End If

                    Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                        With cmd
                            .CommandType = System.Data.CommandType.Text
                            .CommandText = sb_cmd.ToString
                            params.AppendParams(.Parameters, 0, pcnt)
                            params.AppendParams(.Parameters, nidx, cmd_str.Second - nidx)
                            nidx = cmd_str.Second
                        End With
                        Dim arr As Generic.IList(Of ColumnAttribute) = Nothing
                        If withLoad Then
                            arr = DbSchema.GetSortedFieldList(type2load)
                        End If
                        Dim b As ConnAction = TestConn(cmd)
                        Try
                            If withLoad Then
                                _cache.BeginTrackDelete(type2load)
                            End If
                            Dim et As New PerfCounter
                            Using dr As System.Data.IDataReader = cmd.ExecuteReader
                                _exec = et.GetTime

                                Dim ft As New PerfCounter
                                Do While dr.Read
                                    Dim id1 As Object = dr.GetValue(0)
                                    Dim id2 As Object = dr.GetValue(1)
                                    Dim el As EditableList = Nothing
                                    If edic.TryGetValue(id1, el) Then
                                        el.Add(id2)
                                    Else
                                        Dim l As New List(Of Object)
                                        l.Add(id2)
                                        el = New EditableList(id1, l, type, type2load, Nothing)
                                        edic.Add(id1, el)
                                    End If
                                    Dim obj As T = GetOrmBaseFromCacheOrCreate(Of T)(id2)
                                    If withLoad AndAlso Not _cache.IsDeleted(type2load, obj.Key) Then
                                        If obj.ObjectState <> ObjectState.Modified Then
                                            Using obj.GetSyncRoot()
                                                'If obj.IsLoaded Then obj.IsLoaded = False
                                                Dim lock As IDisposable = Nothing
                                                Try
                                                    Dim ro As _IEntity = LoadFromDataReader(obj, dr, arr, False, 2, dic, True, lock, oschema2, oschema2.GetFieldColumnMap)
                                                    AfterLoadingProcess(dic, obj, lock, ro)
                                                Finally
                                                    If lock IsNot Nothing Then
                                                        'Threading.Monitor.Exit(dic)
                                                        lock.Dispose()
                                                    End If
                                                End Try
                                            End Using
                                        End If
                                    End If
                                Loop
                                _fetch = ft.GetTime
                            End Using
                        Finally
                            If withLoad Then
                                _cache.EndTrackDelete(type2load)
                            End If
                            CloseConn(b)
                        End Try
                    End Using
                Next
            End If
            Return edic
        End Function

        Protected Overloads Function GetObjects(ByVal ct As Type, ByVal ids As Generic.IList(Of Object), _
            ByVal f As IFilter, ByVal withLoad As Boolean, ByVal fieldName As String, ByVal idsSorted As Boolean) As IList
            If ids Is Nothing Then
                Throw New ArgumentNullException("ids")
            End If

            Dim values As IList = CType(GetType(List(Of )).MakeGenericType(New Type() {ct}).InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), System.Collections.IList)
            If ids.Count < 1 Then
                Return values
            End If

            Dim original_type As Type = ct
            Dim almgr As AliasMgr = AliasMgr.Create
            Dim params As New ParamMgr(DbSchema, "p")
            Dim arr As Generic.List(Of ColumnAttribute) = Nothing
            Dim sb As New StringBuilder
            If withLoad Then
                arr = _schema.GetSortedFieldList(original_type)
                sb.Append(DbSchema.Select(original_type, almgr, params, arr, Nothing, GetFilterInfo))
            Else
                arr = New Generic.List(Of ColumnAttribute)
                arr.Add(New ColumnAttribute("ID", Field2DbRelations.PK))
                sb.Append(DbSchema.SelectID(original_type, almgr, params, GetFilterInfo))
            End If

            If Not DbSchema.AppendWhere(original_type, CType(f, IFilter), almgr, sb, GetFilterInfo, params) Then
                sb.Append(" where 1=1 ")
            End If

            Dim pcnt As Integer = params.Params.Count
            Dim nidx As Integer = pcnt
            For Each cmd_str As Pair(Of String, Integer) In GetFilters(CType(ids, List(Of Object)), fieldName, almgr, params, original_type, idsSorted)
                Dim sb_cmd As New StringBuilder
                sb_cmd.Append(sb.ToString).Append(cmd_str.First)
                'Dim msort As Boolean = False
                'If Not String.IsNullOrEmpty(sort) AndAlso Schema.GetObjectSchema(original_type).IsExternalSort(sort) Then
                '    msort = True
                'Else
                '    Schema.AppendOrder(original_type, sort, sortType, almgr, sb_cmd)
                'End If

                Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                    With cmd
                        .CommandType = System.Data.CommandType.Text
                        .CommandText = sb_cmd.ToString
                        params.AppendParams(.Parameters, 0, pcnt)
                        params.AppendParams(.Parameters, nidx, cmd_str.Second - nidx)
                        nidx = cmd_str.Second
                    End With
                    LoadMultipleObjects(original_type, cmd, withLoad, values, arr)
                    'If msort Then
                    '    objs = Schema.GetObjectSchema(original_type).ExternalSort(sort, sortType, objs)
                    'End If
                End Using

                'params.Clear(pcnt)
            Next
            Return values
        End Function

        Protected Overrides Function GetObjects(Of T As {IOrmBase, New})(ByVal ids As Generic.IList(Of Object), ByVal f As IFilter, ByVal objs As List(Of T), _
            ByVal withLoad As Boolean, ByVal fieldName As String, ByVal idsSorted As Boolean) As Generic.IList(Of T)
            Invariant()

            If ids Is Nothing Then
                Throw New ArgumentNullException("ids")
            End If

            If ids.Count < 1 Then
                Return objs
            End If

            Dim original_type As Type = GetType(T)
            Dim almgr As AliasMgr = AliasMgr.Create
            Dim params As New ParamMgr(DbSchema, "p")
            Dim arr As Generic.List(Of ColumnAttribute) = Nothing
            Dim sb As New StringBuilder
            If withLoad Then
                arr = _schema.GetSortedFieldList(original_type)
                sb.Append(DbSchema.Select(original_type, almgr, params, arr, Nothing, GetFilterInfo))
            Else
                arr = New Generic.List(Of ColumnAttribute)
                arr.Add(New ColumnAttribute("ID", Field2DbRelations.PK))
                sb.Append(DbSchema.SelectID(original_type, almgr, params, GetFilterInfo))
            End If

            If Not DbSchema.AppendWhere(original_type, CType(f, IFilter), almgr, sb, GetFilterInfo, params) Then
                sb.Append(" where 1=1 ")
            End If

            Dim pcnt As Integer = params.Params.Count
            Dim nidx As Integer = pcnt
            For Each cmd_str As Pair(Of String, Integer) In GetFilters(CType(ids, List(Of Object)), fieldName, almgr, params, original_type, idsSorted)
                Dim sb_cmd As New StringBuilder
                sb_cmd.Append(sb.ToString).Append(cmd_str.First)
                'Dim msort As Boolean = False
                'If Not String.IsNullOrEmpty(sort) AndAlso Schema.GetObjectSchema(original_type).IsExternalSort(sort) Then
                '    msort = True
                'Else
                '    Schema.AppendOrder(original_type, sort, sortType, almgr, sb_cmd)
                'End If

                Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                    With cmd
                        .CommandType = System.Data.CommandType.Text
                        .CommandText = sb_cmd.ToString
                        params.AppendParams(.Parameters, 0, pcnt)
                        params.AppendParams(.Parameters, nidx, cmd_str.Second - nidx)
                        nidx = cmd_str.Second
                    End With
                    LoadMultipleObjects(Of T)(cmd, withLoad, objs, arr)
                    'If msort Then
                    '    objs = Schema.GetObjectSchema(original_type).ExternalSort(sort, sortType, objs)
                    'End If
                End Using

                'params.Clear(pcnt)
            Next
            Return objs
        End Function

        Protected Friend Overrides Function GetStaticKey() As String
            Return String.Empty
        End Function

        'Protected Overridable Function GetNewObject(ByVal type As Type, ByVal id As Integer) As OrmBase
        '    Dim o As OrmBase = Nothing
        '    If  IsNot Nothing Then
        '        o = FindNewDelegate(type, id)
        '    End If
        '    Return o
        'End Function

        Protected Friend Overrides Sub LoadObject(ByVal obj As _ICachedEntity)
            Invariant()

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim original_type As Type = obj.GetType

            'Dim filter As New Database.Criteria.Core.EntityFilter(original_type, "ID", _
            '    New EntityValue(obj), Worm.Criteria.FilterOperation.Equal)
            Dim c As New Worm.Database.Criteria.Conditions.Condition.ConditionConstructor '= Database.Criteria.Conditions.Condition.ConditionConstructor
            For Each p As Pair(Of String, Object) In obj.GetPKValues
                c.AddFilter(New Database.Criteria.Core.EntityFilter(original_type, p.First, New ScalarValue(p.Second), Worm.Criteria.FilterOperation.Equal))
            Next
            Dim filter As IFilter = c.Condition

            Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                Dim arr As Generic.List(Of ColumnAttribute) = _schema.GetSortedFieldList(original_type)

                With cmd
                    .CommandType = System.Data.CommandType.Text

                    Dim almgr As AliasMgr = AliasMgr.Create
                    Dim params As New ParamMgr(DbSchema, "p")
                    Dim sb As New StringBuilder
                    sb.Append(DbSchema.Select(original_type, almgr, params, Nothing, Nothing, GetFilterInfo))
                    DbSchema.AppendWhere(original_type, filter, almgr, sb, GetFilterInfo, params)

                    params.AppendParams(.Parameters)
                    .CommandText = sb.ToString
                End With

                'Dim olds As ObjectState = obj.ObjectState
                Dim b As ConnAction = TestConn(cmd)
                Try
                    'Dim newObj As Boolean = obj.ObjectState = ObjectState.Created
                    Dim sync_key As String = "LoadSngObj" & obj.Key & original_type.ToString
                    'Using obj.GetSyncRoot()
                    Using SyncHelper.AcquireDynamicLock(sync_key)
                        'Using obj.GetSyncRoot()
                        LoadSingleObject(cmd, arr, obj, True, True, False)
                        'If newObj AndAlso obj.ObjectState = ObjectState.None OrElse obj.ObjectState = ObjectState.NotLoaded Then
                        '    _cache.UnregisterModification(obj)
                        '    NormalizeObject(obj, GetDictionary(obj.GetType))
                        'End If
                        If obj.ObjectState <> ObjectState.NotFoundInSource Then
                            Add2Cache(obj)
                            obj.AcceptChanges(True, True)
                        End If
                    End Using
                Finally
                    CloseConn(b)
                End Try
                'If olds = obj.ObjectState OrElse obj.ObjectState = ObjectState.Created Then obj.ObjectState = ObjectState.None
                '(olds = ObjectState.Created AndAlso obj.ObjectState = ObjectState.Modified) OrElse
                'If olds = obj.ObjectState Then
                '    obj.ObjectState = ObjectState.None
                'End If
            End Using
        End Sub

        Protected Sub LoadSingleObject(ByVal cmd As System.Data.Common.DbCommand, _
            ByVal arr As Generic.IList(Of ColumnAttribute), ByVal obj As _ICachedEntity, _
            ByVal check_pk As Boolean, ByVal load As Boolean, ByVal modifiedloaded As Boolean)
            Invariant()

            Dim dic As IDictionary = GetDictionary(obj.GetType)
            'Dim b As ConnAction = TestConn(cmd)
            Try
                If load Then
                    _cache.BeginTrackDelete(obj.GetType)
                End If
                Dim et As New PerfCounter
                Using dr As System.Data.IDataReader = cmd.ExecuteReader
                    Dim k As String = String.Empty
                    If obj.IsPKLoaded Then
                        k = obj.Key.ToString
                    End If
                    Dim sync_key As String = "LoadSngObj" & k & obj.GetType.ToString
                    'Using obj.GetSyncRoot()
                    Using SyncHelper.AcquireDynamicLock(sync_key)
                        'Dim old As Boolean = obj.IsLoaded
                        'If Not modifiedloaded Then obj.IsLoaded = False
                        'obj.IsLoaded = False
                        Dim loaded As Boolean = False
                        Dim oschema As IOrmObjectSchema = DbSchema.GetObjectSchema(obj.GetType)
                        'obj = NormalizeObject(obj, dic)
                        Do While dr.Read
                            If loaded Then
                                Throw New OrmManagerException(String.Format("Statement [{0}] returns more than one record", cmd.CommandText))
                            End If
                            If obj.ObjectState <> ObjectState.Deleted AndAlso (Not load OrElse Not _cache.IsDeleted(obj)) Then
                                Dim lock As IDisposable = Nothing
                                Try
                                    LoadFromDataReader(obj, dr, arr, check_pk, 0, dic, False, lock, oschema, oschema.GetFieldColumnMap)
                                    obj.CorrectStateAfterLoading(False)
                                Finally
                                    If lock IsNot Nothing Then
                                        lock.Dispose()
                                    End If
                                End Try
                                'If lock Then
                                '    Dim co As _ICachedEntity = TryCast(obj, _ICachedEntity)
                                '    If co IsNot Nothing Then
                                '        _cache.UnregisterModification(co)

                                '        If lock Then
                                '            Threading.Monitor.Exit(dic)
                                '            lock = False
                                '        End If
                                '    End If
                                'End If
                                'obj = o
                            Else
                                Exit Do
                            End If
                            loaded = True
                        Loop
                        If dr.RecordsAffected = 0 Then
                            Throw DbSchema.PrepareConcurrencyException(obj)
                        End If

                        If Not obj.IsLoaded AndAlso loaded Then
                            If load Then
                                'Throw New ApplicationException
                                _cache.UnregisterModification(obj)
                                obj.SetObjectState(ObjectState.NotFoundInSource)
                                RemoveObjectFromCache(obj)
                            End If
                        ElseIf Not obj.IsLoaded AndAlso Not loaded AndAlso dr.RecordsAffected > 0 Then
                            'insert without select
                            obj.CreateCopyForSaveNewEntry(Nothing)
                        Else
                            If dr.RecordsAffected <> -1 Then
                                'obj.CreateCopyForSaveNewEntry(Nothing)
                                'obj.ObjectState = ObjectState.None
                                'Throw New ApplicationException
                                'obj.BeginLoading()
                                'obj.Identifier = obj.Identifier
                                'obj.EndLoading()
                            Else
                                If Not obj.IsLoaded Then
                                    'loading non-existent object
                                    _cache.UnregisterModification(obj)
                                    obj.SetObjectState(ObjectState.NotFoundInSource)
                                    RemoveObjectFromCache(obj)
                                End If
                            End If
                        End If
                    End Using
                End Using
                _cache.LogLoadTime(obj, et.GetTime)
            Finally
                If load Then
                    _cache.EndTrackDelete(obj.GetType)
                End If
                'CloseConn(b)
            End Try

        End Sub

        Public Function GetSimpleValues(Of T)(ByVal cmd As System.Data.Common.DbCommand) As IList(Of T)
            Dim l As New List(Of T)
            Dim b As ConnAction = TestConn(cmd)
            Try
                Dim n As Boolean = GetType(T).FullName.StartsWith("System.Nullable")
                Using dr As System.Data.IDataReader = cmd.ExecuteReader
                    Do While dr.Read
                        'l.Add(CType(Convert.ChangeType(dr.GetValue(0), GetType(T)), T))
                        If dr.IsDBNull(0) Then
                            l.Add(Nothing)
                        Else
                            If n Then
                                Dim rt As Type = GetType(T).GetGenericArguments(0)
                                Dim o As Object = Convert.ChangeType(dr.GetValue(0), rt)
                                l.Add(CType(o, T))
                            Else
                                l.Add(CType(dr.GetValue(0), T))
                            End If
                        End If

                    Loop
                    Return l
                End Using
            Finally
                CloseConn(b)
            End Try
        End Function

        Protected Friend Sub LoadMultipleObjects(Of T As {_IEntity, New})( _
            ByVal cmd As System.Data.Common.DbCommand, _
            ByVal withLoad As Boolean, ByVal values As IList, _
            ByVal arr As Generic.List(Of ColumnAttribute))

            Dim oschema As IOrmObjectSchema = CType(_schema.GetObjectSchema(GetType(T), False), IOrmObjectSchema)
            Dim fields_idx As Collections.IndexedCollection(Of String, MapField2Column) = oschema.GetFieldColumnMap

            LoadMultipleObjects(Of T)(cmd, withLoad, values, arr, oschema, fields_idx)
        End Sub

        Protected Friend Sub LoadMultipleObjects(Of T As {_IEntity, New})( _
            ByVal cmd As System.Data.Common.DbCommand, _
            ByVal withLoad As Boolean, ByVal values As IList, _
            ByVal arr As Generic.List(Of ColumnAttribute), ByVal oschema As IOrmObjectSchemaBase, ByVal fields_idx As Collections.IndexedCollection(Of String, MapField2Column))

            If values Is Nothing Then
                'values = New Generic.List(Of T)
                Throw New ArgumentNullException("values")
            End If

            Invariant()

            'Dim idx As Integer = -1
            Dim b As ConnAction = TestConn(cmd)
            Dim original_type As Type = GetType(T)
            Try
                _loadedInLastFetch = 0
                If withLoad Then
                    _cache.BeginTrackDelete(original_type)
                End If

                Dim et As New PerfCounter
                Using dr As System.Data.IDataReader = cmd.ExecuteReader
                    _exec = et.GetTime

                    'If idx = -1 Then
                    '    Dim pk_name As String = _schema.GetPrimaryKeysName(original_type, False)(0)
                    '    Try
                    '        idx = dr.GetOrdinal(pk_name)
                    '    Catch ex As IndexOutOfRangeException
                    '        If _mcSwitch.TraceError Then
                    '            Trace.WriteLine("Invalid column name " & pk_name & " in " & cmd.CommandText)
                    '            Trace.WriteLine(Environment.StackTrace)
                    '        End If
                    '        Throw New OrmManagerException("Cannot get primary key ordinal", ex)
                    '    End Try
                    'End If

                    'If arr Is Nothing Then arr = Schema.GetSortedFieldList(original_type)

                    'Dim idx As Integer = GetPrimaryKeyIdx(cmd.CommandText, original_type, dr)
                    Dim dic As Generic.IDictionary(Of Object, T) = GetDictionary(Of T)(oschema)
                    Dim il As IListEdit = TryCast(values, IListEdit)
                    If il IsNot Nothing Then
                        values = il.List
                    End If
                    Dim ft As New PerfCounter
                    Do While dr.Read
                        LoadFromResultSet(Of T)(withLoad, values, arr, dr, dic, _loadedInLastFetch, oschema, fields_idx)
                    Loop
                    _fetch = ft.GetTime
                End Using
            Finally
                If withLoad Then
                    _cache.EndTrackDelete(original_type)
                End If
                CloseConn(b)
            End Try
        End Sub

        Protected Function GetPrimaryKeyIdx(ByVal cmdtext As String, ByVal original_type As Type, ByVal dr As System.Data.IDataReader) As Integer
            Dim idx As Integer = -1
            Dim pk_name As String = _schema.GetPrimaryKeysName(original_type, False)(0)
            Try
                idx = dr.GetOrdinal(pk_name)
            Catch ex As IndexOutOfRangeException
                If _mcSwitch.TraceError Then
                    Trace.WriteLine("Invalid column name " & pk_name & " in " & cmdtext)
                    Trace.WriteLine(Environment.StackTrace)
                End If
                Throw New OrmManagerException("Cannot get primary key ordinal", ex)
            End Try
            Return idx
        End Function

        Private Sub AfterLoadingProcess(ByVal dic As IDictionary, ByVal obj As _IEntity, ByRef lock As IDisposable, ByRef ro As _IEntity)
            Dim notFromCache As Boolean = Object.ReferenceEquals(ro, obj)
            ro.CorrectStateAfterLoading(notFromCache)
            'If notFromCache Then
            '    If ro.ObjectState = ObjectState.None OrElse ro.ObjectState = ObjectState.NotLoaded Then
            '        Dim co As _ICachedEntity = TryCast(ro, _ICachedEntity)
            '        If co IsNot Nothing Then
            '            _cache.UnregisterModification(co)
            '            If co.IsPKLoaded Then
            '                ro = NormalizeObject(co, dic)
            '            End If
            '            If lock Then
            '                Threading.Monitor.Exit(dic)
            '                lock = False
            '            End If
            '        End If
            '    End If
            'End If
        End Sub

        Protected Friend Sub LoadFromResultSet(Of T As {_IEntity, New})( _
            ByVal withLoad As Boolean, _
            ByVal values As IList, ByVal arr As Generic.List(Of ColumnAttribute), _
            ByVal dr As System.Data.IDataReader, _
            ByVal dic As IDictionary(Of Object, T), ByRef loaded As Integer, _
            ByVal oschema As IOrmObjectSchemaBase, ByVal fields_idx As Collections.IndexedCollection(Of String, MapField2Column))

            'Dim id As Integer = CInt(dr.GetValue(idx))
            'Dim obj As OrmBase = CreateDBObject(Of T)(id, dic, withLoad OrElse AlwaysAdd2Cache OrElse Not ListConverter.IsWeak)
            If GetType(IOrmBase).IsAssignableFrom(GetType(T)) Then
            Else
            End If
            'Dim obj As _ICachedEntity = CType(CreateEntity(Of T)(), _ICachedEntity)
            'If obj IsNot Nothing Then
            'If _raiseCreated Then
            'RaiseObjectCreated(obj)
            'End If
            Dim lock As IDisposable = Nothing
            Try
                Dim obj As New T
                Dim ro As _IEntity = LoadFromDataReader(obj, dr, arr, False, 0, CType(dic, System.Collections.IDictionary), True, lock, oschema, fields_idx)
                AfterLoadingProcess(CType(dic, System.Collections.IDictionary), obj, lock, ro)
#If DEBUG Then
                Dim ce As CachedEntity = TryCast(ro, CachedEntity)
                If ce IsNot Nothing Then ce.Invariant()
#End If
                values.Add(ro)
                If ro.IsLoaded Then
                    loaded += 1
                End If
            Finally
                If lock IsNot Nothing Then
                    'Threading.Monitor.Exit(dic)
                    lock.Dispose()
                End If
            End Try
            '            If withLoad AndAlso Not _cache.IsDeleted(TryCast(obj, ICachedEntity)) Then
            '                Using obj.GetSyncRoot()
            '                    If obj.ObjectState <> ObjectState.Modified AndAlso obj.ObjectState <> ObjectState.Deleted Then
            '                        'If obj.IsLoaded Then obj.IsLoaded = False

            '                        'If Not obj.IsLoaded Then
            '                        '    obj.ObjectState = ObjectState.NotFoundInDB
            '                        '    RemoveObjectFromCache(obj)
            '                        'Else
            '                        obj.CorrectStateAfterLoading()
            '                        values.Add(obj)
            '                        loaded += 1
            '                        'End If
            '                    ElseIf obj.ObjectState = ObjectState.Modified Then
            '                        GoTo l1
            '                    End If
            '                End Using
            '            Else
            'l1:
            '                values.Add(obj)
            '                If obj.IsLoaded Then
            '                    loaded += 1
            '                End If
            '            End If
            'Else
            'If _mcSwitch.TraceVerbose Then
            '    WriteLine("Attempt to load unallowed object " & GetType(T).Name & " (" & id & ")")
            'End If
            'End If
        End Sub

        Protected Function LoadFromDataReader(ByVal obj As _IEntity, ByVal dr As System.Data.IDataReader, _
            ByVal arr As Generic.IList(Of ColumnAttribute), ByVal check_pk As Boolean, ByVal displacement As Integer, _
            ByVal dic As IDictionary, ByVal fromRS As Boolean, ByRef lock As IDisposable, ByVal oschema As IOrmObjectSchemaBase, _
            ByVal fields_idx As Collections.IndexedCollection(Of String, MapField2Column)) As _IEntity

            Debug.Assert(obj.ObjectState <> ObjectState.Deleted)

            Dim original_type As Type = obj.GetType
            Dim fv As IDBValueFilter = TryCast(oschema, IDBValueFilter)
            'Dim fields_idx As Collections.IndexedCollection(Of String, MapField2Column) = oschema.GetFieldColumnMap
            Dim fac As New List(Of Pair(Of String, Object))
            Dim ce As _ICachedEntity = TryCast(obj, _ICachedEntity)
            'Dim load As Boolean = a
            'Using obj.GetSyncRoot()
            'Dim d As IDisposable = Nothing
            obj.BeginLoading()
            Dim loading As Boolean = False
            Try
                Dim pk_count As Integer = 0
                Dim has_pk As Boolean = False
                Dim pi_cache(arr.Count - 1) As Reflection.PropertyInfo
                Dim idic As IDictionary = Nothing
                If oschema IsNot Nothing Then
                    idic = _schema.GetProperties(original_type, oschema)
                End If
                'Dim bl As Boolean
                Dim oldpk() As Pair(Of String, Object) = Nothing
                If ce IsNot Nothing AndAlso Not fromRS Then oldpk = ce.GetPKValues()
                For idx As Integer = 0 To arr.Count - 1
                    Dim c As ColumnAttribute = arr(idx)
                    Dim pi As Reflection.PropertyInfo = If(idic IsNot Nothing, CType(idic(c), Reflection.PropertyInfo), Nothing)
                    pi_cache(idx) = pi

                    If fields_idx.ContainsKey(c.FieldName) Then
                        If idx >= 0 AndAlso (fields_idx(c.FieldName).GetAttributes(c) And Field2DbRelations.PK) = Field2DbRelations.PK Then
                            Assert(idx + displacement < dr.FieldCount, c.FieldName)
                            'If dr.FieldCount <= idx + displacement Then
                            '    If _mcSwitch.TraceError Then
                            '        Dim dt As System.Data.DataTable = dr.GetSchemaTable
                            '        Dim sb As New StringBuilder
                            '        For Each drow As System.Data.DataRow In dt.Rows
                            '            If sb.Length > 0 Then
                            '                sb.Append(", ")
                            '            End If
                            '            sb.Append(drow("ColumnName")).Append("(").Append(drow("ColumnOrdinal")).Append(")")
                            '        Next
                            '        WriteLine(sb.ToString)
                            '    End If
                            'End If
                            has_pk = True
                            Dim value As Object = dr.GetValue(idx + displacement)
                            If fv IsNot Nothing Then
                                value = fv.CreateValue(c, obj, value)
                            End If

                            If Not dr.IsDBNull(idx + displacement) Then
                                'If ce IsNot Nothing AndAlso obj.ObjectState = ObjectState.Created Then
                                '    ce.CreateCopyForSaveNewEntry()
                                '    'bl = True
                                'End If

                                Try
                                    If pi Is Nothing Then
                                        obj.SetValue(pi, c, oschema, value)
                                        If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                                    Else
                                        If (pi.PropertyType Is GetType(Boolean) AndAlso value.GetType Is GetType(Short)) OrElse (pi.PropertyType Is GetType(Integer) AndAlso value.GetType Is GetType(Long)) Then
                                            Dim v As Object = Convert.ChangeType(value, pi.PropertyType)
                                            obj.SetValue(pi, c, oschema, v)
                                            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                                        ElseIf pi.PropertyType Is GetType(Byte()) AndAlso value.GetType Is GetType(Date) Then
                                            Dim dt As DateTime = CDate(value)
                                            Dim l As Long = dt.ToBinary
                                            Using ms As New IO.MemoryStream
                                                Dim sw As New IO.StreamWriter(ms)
                                                sw.Write(l)
                                                sw.Flush()
                                                obj.SetValue(pi, c, oschema, ms.ToArray)
                                                If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                                            End Using
                                        Else
                                            'If c.FieldName = "ID" Then
                                            '    obj.Identifier = CInt(value)
                                            'Else
                                            obj.SetValue(pi, c, oschema, value)
                                            'End If
                                            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                                        End If
                                    End If
                                Catch ex As ArgumentException When ex.Message.StartsWith("Object of type 'System.DateTime' cannot be converted to type 'System.Byte[]'")
                                    Dim dt As DateTime = CDate(value)
                                    Dim l As Long = dt.ToBinary
                                    Using ms As New IO.MemoryStream
                                        Dim sw As New IO.StreamWriter(ms)
                                        sw.Write(l)
                                        sw.Flush()
                                        obj.SetValue(pi, c, oschema, ms.ToArray)
                                        If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                                    End Using
                                Catch ex As ArgumentException When ex.Message.IndexOf("cannot be converted") > 0
                                    Dim v As Object = Convert.ChangeType(value, pi.PropertyType)
                                    obj.SetValue(pi, c, oschema, v)
                                    If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                                End Try
                                pk_count += 1
                            End If
                        End If
                    End If
                Next

                If has_pk Then
                    If ce IsNot Nothing Then
                        ce.PKLoaded(pk_count)

                        If _cache.IsDeleted(ce) Then
                            Return obj
                        End If

                        'Threading.Monitor.Enter(dic)
                        'lock = True

                        Dim robj As ICachedEntity = NormalizeObject(ce, dic, fromRS)
                        Dim fromCache As Boolean = Not Object.ReferenceEquals(robj, ce)

                        obj = robj
                        ce = CType(obj, _ICachedEntity)
                        SyncLock dic
                            If fromCache Then
                                If obj.ObjectState = ObjectState.Created Then
                                    Using obj.GetSyncRoot
                                        Return obj
                                    End Using
                                ElseIf obj.ObjectState = ObjectState.Modified OrElse obj.ObjectState = ObjectState.Deleted Then
                                    Return obj
                                Else
                                    obj.BeginLoading()
                                End If
                            Else
                                If _raiseCreated Then
                                    RaiseObjectCreated(ce)
                                End If

                                If fromRS Then
                                Else
                                    If obj.ObjectState = ObjectState.Created Then
                                        ce.CreateCopyForSaveNewEntry(oldpk)
                                        'Cache.Modified(obj).Reason = ModifiedObject.ReasonEnum.SaveNew
                                    End If
                                End If
                            End If
                        End SyncLock
                    End If
                ElseIf ce IsNot Nothing AndAlso Not fromRS AndAlso obj.ObjectState = ObjectState.Created Then
                    ce.CreateCopyForSaveNewEntry(Nothing)
                End If

                If pk_count < arr.Count Then
                    lock = obj.GetSyncRoot
                    If obj.ObjectState = ObjectState.Deleted OrElse obj.ObjectState = ObjectState.NotFoundInSource Then
                        Return obj
                    End If
                    'Try
                    For idx As Integer = 0 To arr.Count - 1
                        Dim c As ColumnAttribute = arr(idx)
                        Dim pi As Reflection.PropertyInfo = pi_cache(idx) '_schema.GetProperty(original_type, c)

                        Dim value As Object = dr.GetValue(idx + displacement)
                        If fv IsNot Nothing Then
                            value = fv.CreateValue(c, obj, value)
                        End If

                        If pi Is Nothing Then
                            obj.SetValue(pi, c, oschema, value)
                            If ce IsNot Nothing Then ce.SetLoaded(c, True, False, _schema)
                        Else
                            Dim att As Field2DbRelations = fields_idx(c.FieldName).GetAttributes(c)
                            If check_pk AndAlso (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                                Dim v As Object = pi.GetValue(obj, Nothing)
                                If Not value.GetType Is pi.PropertyType AndAlso pi.PropertyType IsNot GetType(Object) Then
                                    value = Convert.ChangeType(value, pi.PropertyType)
                                End If
                                If Not v.Equals(value) Then
                                    Throw New OrmManagerException("PK values is not equals (" & dr.GetName(idx + displacement) & "): value from db: " & value.ToString & "; value from object: " & v.ToString)
                                End If
                            ElseIf Not dr.IsDBNull(idx + displacement) AndAlso (att And Field2DbRelations.PK) <> Field2DbRelations.PK Then
                                If (att And Field2DbRelations.Factory) = Field2DbRelations.Factory Then
                                    fac.Add(New Pair(Of String, Object)(c.FieldName, value))
                                    If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                                    '    'obj.CreateObject(c.FieldName, value)
                                    '    obj.SetValue(pi, c, )
                                    '    obj.SetLoaded(c, True, True)
                                    '    'If GetType(OrmBase) Is pi.PropertyType Then
                                    '    '    obj.CreateObject(CInt(value))
                                    '    '    obj.SetLoaded(c, True)
                                    '    'Else
                                    '    '    Dim type_created As Type = pi.PropertyType
                                    '    '    Dim o As OrmBase = CreateDBObject(CInt(value), type_created)
                                    '    '    obj.SetValue(pi, c, o)
                                    '    '    obj.SetLoaded(c, True)
                                    '    'End If
                                ElseIf GetType(IOrmBase).IsAssignableFrom(pi.PropertyType) Then
                                    Dim type_created As Type = pi.PropertyType
                                    Dim o As IOrmBase = GetOrmBaseFromCacheOrCreate(value, type_created)
                                    obj.SetValue(pi, c, oschema, o)
                                    If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                                ElseIf GetType(System.Xml.XmlDocument) Is pi.PropertyType AndAlso TypeOf (value) Is String Then
                                    Dim o As New System.Xml.XmlDocument
                                    o.LoadXml(CStr(value))
                                    obj.SetValue(pi, c, oschema, o)
                                    If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                                ElseIf pi.PropertyType.IsEnum AndAlso TypeOf (value) Is String Then
                                    Dim svalue As String = CStr(value).Trim
                                    If svalue = String.Empty Then
                                        obj.SetValue(pi, c, oschema, 0)
                                        If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                                    Else
                                        obj.SetValue(pi, c, oschema, [Enum].Parse(pi.PropertyType, svalue, True))
                                        If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                                    End If
                                ElseIf pi.PropertyType.IsGenericType AndAlso GetType(Nullable(Of )).Name = pi.PropertyType.Name Then
                                    Dim t As Type = pi.PropertyType.GetGenericArguments()(0)
                                    Dim v As Object = Nothing
                                    If t.IsPrimitive Then
                                        v = Convert.ChangeType(value, t)
                                    ElseIf t.IsEnum Then
                                        If TypeOf (value) Is String Then
                                            Dim svalue As String = CStr(value).Trim
                                            If svalue = String.Empty Then
                                                v = [Enum].ToObject(t, 0)
                                            Else
                                                v = [Enum].Parse(t, svalue, True)
                                            End If
                                        Else
                                            v = [Enum].ToObject(t, value)
                                        End If
                                    ElseIf t Is value.GetType Then
                                        v = value
                                    Else
                                        Try
                                            v = t.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, _
                                                Nothing, Nothing, New Object() {value})
                                        Catch ex As MissingMethodException
                                            'Debug.WriteLine(c.FieldName & ": " & original_type.Name)
                                            'v = Convert.ChangeType(value, t)
                                        End Try
                                    End If
                                    Dim v2 As Object = pi.PropertyType.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, _
                                        Nothing, Nothing, New Object() {v})
                                    obj.SetValue(pi, c, oschema, v2)
                                    If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                                Else
                                    Try
                                        If (pi.PropertyType.IsPrimitive AndAlso value.GetType.IsPrimitive) OrElse (pi.PropertyType Is GetType(Long) AndAlso value.GetType Is GetType(Decimal)) Then
                                            Dim v As Object = Convert.ChangeType(value, pi.PropertyType)
                                            obj.SetValue(pi, c, oschema, v)
                                            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                                        ElseIf pi.PropertyType Is GetType(Byte()) AndAlso value.GetType Is GetType(Date) Then
                                            Dim dt As DateTime = CDate(value)
                                            Dim l As Long = dt.ToBinary
                                            Using ms As New IO.MemoryStream
                                                Dim sw As New IO.StreamWriter(ms)
                                                sw.Write(l)
                                                sw.Flush()
                                                'pi.SetValue(obj, ms.ToArray, Nothing)
                                                obj.SetValue(pi, c, oschema, ms.ToArray)
                                                If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                                            End Using
                                            'ElseIf pi.PropertyType Is GetType(ReleaseDate) AndAlso value.GetType Is GetType(Integer) Then
                                            '    obj.SetValue(pi, c, pi.PropertyType.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, _
                                            '        Nothing, New Object() {value}))
                                            '    obj.SetLoaded(c, True)
                                        Else
                                            obj.SetValue(pi, c, oschema, value)
                                            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                                        End If
                                        'Catch ex As ArgumentException When ex.Message.StartsWith("Object of type 'System.DateTime' cannot be converted to type 'System.Byte[]'")
                                        '    Dim dt As DateTime = CDate(value)
                                        '    Dim l As Long = dt.ToBinary
                                        '    Using ms As New IO.MemoryStream
                                        '        Dim sw As New IO.StreamWriter(ms)
                                        '        sw.Write(l)
                                        '        sw.Flush()
                                        '        obj.SetValue(pi, c, ms.ToArray)
                                        '        obj.SetLoaded(c, True)
                                        '    End Using
                                    Catch ex As ArgumentException When ex.Message.IndexOf("cannot be converted") > 0
                                        Dim v As Object = Convert.ChangeType(value, pi.PropertyType)
                                        obj.SetValue(pi, c, oschema, v)
                                        If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                                    End Try
                                End If
                            ElseIf dr.IsDBNull(idx + displacement) Then
                                obj.SetValue(pi, c, oschema, Nothing)
                                If ce IsNot Nothing Then ce.SetLoaded(c, True, True, _schema)
                            End If
                        End If
                    Next

                    Dim f As IFactory = TryCast(oschema, IFactory)
                    If f IsNot Nothing Then
                        For Each p As Pair(Of String, Object) In fac
                            f.CreateObject(p.First, p.Second)
                        Next
                    End If
                    'Finally
                    '    obj.EndLoading()
                    '    loading = True
                    'End Try
                    'End Using
                End If
            Finally
                If Not loading Then obj.EndLoading()
            End Try

            If ce IsNot Nothing Then ce.CheckIsAllLoaded(ObjectSchema, arr.Count)
            'End Using

            Return obj
        End Function

        Protected Friend Function TestConn(ByVal cmd As System.Data.Common.DbCommand) As ConnAction
            Invariant()

            Dim r As ConnAction

            If _conn Is Nothing Then
                _conn = CreateConn()
                _conn.Open()
                r = ConnAction.Destroy
                If cmd IsNot Nothing Then cmd.Connection = _conn
            Else
                If _tran IsNot Nothing Then
                    If cmd IsNot Nothing Then
                        cmd.Connection = _conn
                        cmd.Transaction = _tran
                    End If
                Else
                    Dim cs As System.Data.ConnectionState = _conn.State
                    If cs = System.Data.ConnectionState.Closed OrElse cs = System.Data.ConnectionState.Broken Then
                        _conn.Open()
                        r = ConnAction.Close
                    End If
                    If cmd IsNot Nothing Then cmd.Connection = _conn
                End If
            End If

            TraceStmt(cmd)
            Return r
        End Function

        Protected Friend Sub CloseConn(ByVal b As ConnAction)
            Invariant()

            Assert(_conn IsNot Nothing, "Connection cannot be nothing")

            Select Case b
                Case ConnAction.Close
                    _conn.Close()
                Case ConnAction.Destroy
                    _conn.Dispose()
                    _conn = Nothing
            End Select
        End Sub

        '<Conditional("TRACE")> _
        Protected Sub TraceStmt(ByVal cmd As System.Data.Common.DbCommand)
            Dim sb As New StringBuilder
            If _tsStmt.Switch.ShouldTrace(TraceEventType.Information) Then
                'SyncLock _tsStmt
                For Each p As System.Data.Common.DbParameter In cmd.Parameters
                    With p
                        Dim t As Type = GetType(DBNull)
                        Dim val As String = "null"
                        Dim tp As String = "nvarchar(1)"
                        If p.Value IsNot Nothing AndAlso p.Value IsNot DBNull.Value Then
                            t = p.Value.GetType
                            val = p.Value.ToString
                            If TypeOf p.Value Is String Then
                                val = "'" & val & "'"
                            End If
                            tp = DbTypeConvertor.ToSqlDbType(p.DbType).ToString
                            If p.DbType = System.Data.DbType.String Then
                                tp &= "(" & p.Size.ToString & ")"
                            End If
                        End If
                        sb.Append(DbSchema.DeclareVariable(p.ParameterName, tp))
                        sb.AppendLine(";set " & p.ParameterName & " = " & val)
                    End With
                Next
                sb.AppendLine(cmd.CommandText)
                'End SyncLock
            End If
            helper.WriteInfo(_tsStmt, sb.ToString)
            WriteLineInfo(sb.ToString)
        End Sub

        'Protected Friend Overrides Function LoadObjectsInternal(Of T As {OrmBase, New})( _
        '    ByVal objs As ReadOnlyList(Of T), ByVal start As Integer, ByVal length As Integer, _
        '    ByVal remove_not_found As Boolean) As ReadOnlyList(Of T)

        '    'Invariant()

        '    'If objs.Count < 1 Then
        '    '    Return objs
        '    'End If

        '    'If start > objs.Count Then
        '    '    Throw New ArgumentException(String.Format("The range {0},{1} is greater than array length: " & objs.Count, start, length))
        '    'End If

        '    'length = Math.Min(length, objs.Count - start)

        '    'Dim ids As Generic.List(Of Integer) = FormPKValues(Of T)(Me, objs, start, length, True, columns)
        '    'If ids.Count < 1 Then
        '    '    Return objs
        '    'End If

        '    'Dim original_type As Type = GetType(T)
        '    'Dim almgr As AliasMgr = AliasMgr.Create
        '    'Dim params As New ParamMgr(DbSchema, "p")
        '    'Dim sb As New StringBuilder
        '    'sb.Append(DbSchema.Select(original_type, almgr, params, columns, Nothing, GetFilterInfo))
        '    'If Not DbSchema.AppendWhere(original_type, Nothing, almgr, sb, GetFilterInfo, params) Then
        '    '    sb.Append(" where 1=1 ")
        '    'End If
        '    'Dim values As New Generic.List(Of T)
        '    'Dim pcnt As Integer = params.Params.Count
        '    'Dim nextp As Integer = pcnt
        '    'For Each cmd_str As Pair(Of String, Integer) In GetFilters(ids, "ID", almgr, params, original_type, True)
        '    '    Dim sb_cmd As New StringBuilder
        '    '    sb_cmd.Append(sb.ToString).Append(cmd_str.First)

        '    '    Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
        '    '        With cmd
        '    '            .CommandType = System.Data.CommandType.Text
        '    '            .CommandText = sb_cmd.ToString
        '    '            params.AppendParams(.Parameters, 0, pcnt)
        '    '            params.AppendParams(.Parameters, nextp, cmd_str.Second - nextp)
        '    '            nextp = cmd_str.Second
        '    '        End With
        '    '        LoadMultipleObjects(Of T)(cmd, True, values, columns)
        '    '    End Using
        '    'Next

        '    'values.Clear()
        '    'For Each o As T In objs
        '    '    If o.IsLoaded Then
        '    '        values.Add(o)
        '    '    End If
        '    'Next
        '    'Return New ReadOnlyList(Of T)(values)

        '    Dim original_type As Type = GetType(T)
        '    Dim columns As Generic.List(Of ColumnAttribute) = _schema.GetSortedFieldList(original_type)

        '    Return LoadObjectsInternal(Of T)(objs, start, length, remove_not_found, columns, True)
        'End Function

        Public Overrides Function LoadObjectsInternal(Of T As {IOrmBase, New}, T2 As {IOrmBase})( _
            ByVal objs As ReadOnlyList(Of T2), ByVal start As Integer, ByVal length As Integer, _
            ByVal remove_not_found As Boolean, ByVal columns As Generic.List(Of ColumnAttribute), _
            ByVal withLoad As Boolean) As ReadOnlyList(Of T2)
            Invariant()

            If objs.Count < 1 Then
                Return objs
            End If

            If start > objs.Count Then
                Throw New ArgumentException(String.Format("The range {0},{1} is greater than array length: " & objs.Count, start, length))
            End If

            length = Math.Min(length, objs.Count - start)

            Dim ids As Generic.List(Of Object) = FormPKValues(Of T2)(Me, objs, start, length)
            If ids.Count < 1 Then
                'Dim l As New List(Of T)
                'For Each o As T In objs
                '    l.Add(o)
                'Next
                'Return l
                Return objs
            End If

            Dim original_type As Type = GetType(T)
            Dim almgr As AliasMgr = AliasMgr.Create
            Dim params As New ParamMgr(DbSchema, "p")
            Dim sb As New StringBuilder
            sb.Append(DbSchema.Select(original_type, almgr, params, columns, Nothing, GetFilterInfo))
            If Not DbSchema.AppendWhere(original_type, Nothing, almgr, sb, GetFilterInfo, params) Then
                sb.Append(" where 1=1 ")
            End If
            Dim values As New Generic.List(Of T2)
            Dim pcnt As Integer = params.Params.Count
            Dim nextp As Integer = pcnt
            For Each cmd_str As Pair(Of String, Integer) In GetFilters(ids, "ID", almgr, params, original_type, True)
                Dim sb_cmd As New StringBuilder
                sb_cmd.Append(sb.ToString).Append(cmd_str.First)

                Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                    With cmd
                        .CommandType = System.Data.CommandType.Text
                        .CommandText = sb_cmd.ToString
                        params.AppendParams(.Parameters, 0, pcnt)
                        params.AppendParams(.Parameters, nextp, cmd_str.Second - nextp)
                        nextp = cmd_str.Second
                    End With
                    LoadMultipleObjects(Of T)(cmd, True, values, columns)
                End Using
            Next

            Dim result As New ReadOnlyList(Of T2)(original_type)
            Dim ar As IListEdit = result
            Dim dic As IDictionary(Of Object, T) = GetDictionary(Of T)()
            If remove_not_found Then
                'For Each o As T In objs
                '    If Not withLoad Then
                '        If values.Contains(o) OrElse Not ids.Contains(o.Identifier) Then
                '            ar.Add(o)
                '        Else
                '            o.SetObjectState(ObjectState.NotFoundInSource)
                '        End If
                '    Else
                '        If o.IsLoaded Then
                '            ar.Add(o)
                '        ElseIf ListConverter.IsWeak Then
                '            Dim obj As T = Nothing
                '            If dic.TryGetValue(o.Key, obj) AndAlso (o.IsLoaded OrElse values.Contains(o)) Then
                '                ar.Add(obj)
                '            Else
                '                Dim idx As Integer = values.IndexOf(o)
                '                If idx >= 0 Then
                '                    Dim ro As T = values(idx)
                '                    Debug.Assert(ro.IsLoaded)
                '                    Add2Cache(ro)
                '                    ar.Add(ro)
                '                End If
                '            End If
                '        Else

                '        End If
                '    End If
                'Next
                For Each o As T2 In objs
                    Dim pos As Integer = values.IndexOf(o)
                    If pos >= 0 Then
                        If withLoad Then
                            If values(pos).IsLoaded Then
                                ar.Add(values(pos))
                            End If
                        Else
                            ar.Add(values(pos))
                        End If
                    End If
                Next
            Else
                result = New ReadOnlyList(Of T2)(original_type, values)
                'If ListConverter.IsWeak Then
                '    For Each o As T In objs
                '        Dim obj As T = Nothing
                '        If dic.TryGetValue(o.Key, obj) Then
                '            ar.Add(obj)
                '        Else
                '            ar.Add(o)
                '        End If
                '    Next
                'Else
                '    Return objs
                'End If
            End If
            Return result
            'Return New ReadOnlyList(Of T)(values)
        End Function

        Public Overrides Function LoadObjectsInternal(Of T2 As {IOrmBase})(ByVal realType As Type, _
            ByVal objs As ReadOnlyList(Of T2), ByVal start As Integer, ByVal length As Integer, _
            ByVal remove_not_found As Boolean, ByVal columns As Generic.List(Of ColumnAttribute), _
            ByVal withLoad As Boolean) As ReadOnlyList(Of T2)
            Invariant()

            If objs.Count < 1 Then
                Return objs
            End If

            If start > objs.Count Then
                Throw New ArgumentException(String.Format("The range {0},{1} is greater than array length: " & objs.Count, start, length))
            End If

            length = Math.Min(length, objs.Count - start)

            Dim ids As Generic.List(Of Object) = FormPKValues(Of T2)(Me, objs, start, length)
            If ids.Count < 1 Then
                'Dim l As New List(Of T)
                'For Each o As T In objs
                '    l.Add(o)
                'Next
                'Return l
                Return objs
            End If

            'Dim original_type As Type = realType
            Dim almgr As AliasMgr = AliasMgr.Create
            Dim params As New ParamMgr(DbSchema, "p")
            Dim sb As New StringBuilder
            sb.Append(DbSchema.Select(realType, almgr, params, columns, Nothing, GetFilterInfo))
            If Not DbSchema.AppendWhere(realType, Nothing, almgr, sb, GetFilterInfo, params) Then
                sb.Append(" where 1=1 ")
            End If
            Dim values As New Generic.List(Of T2)
            Dim pcnt As Integer = params.Params.Count
            Dim nextp As Integer = pcnt
            For Each cmd_str As Pair(Of String, Integer) In GetFilters(ids, "ID", almgr, params, realType, True)
                Dim sb_cmd As New StringBuilder
                sb_cmd.Append(sb.ToString).Append(cmd_str.First)

                Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                    With cmd
                        .CommandType = System.Data.CommandType.Text
                        .CommandText = sb_cmd.ToString
                        params.AppendParams(.Parameters, 0, pcnt)
                        params.AppendParams(.Parameters, nextp, cmd_str.Second - nextp)
                        nextp = cmd_str.Second
                    End With
                    LoadMultipleObjects(realType, cmd, True, values, columns)
                End Using
            Next

            Dim result As New ReadOnlyList(Of T2)(realType)
            Dim ar As IListEdit = result
            'Dim dic As IDictionary(Of Integer, T) = GetDictionary(realtype)
            If remove_not_found Then
                'For Each o As T In objs
                '    If Not withLoad Then
                '        If values.Contains(o) OrElse Not ids.Contains(o.Identifier) Then
                '            ar.Add(o)
                '        Else
                '            o.SetObjectState(ObjectState.NotFoundInSource)
                '        End If
                '    Else
                '        If o.IsLoaded Then
                '            ar.Add(o)
                '        ElseIf ListConverter.IsWeak Then
                '            Dim obj As T = Nothing
                '            If dic.TryGetValue(o.Key, obj) AndAlso (o.IsLoaded OrElse values.Contains(o)) Then
                '                ar.Add(obj)
                '            Else
                '                Dim idx As Integer = values.IndexOf(o)
                '                If idx >= 0 Then
                '                    Dim ro As T = values(idx)
                '                    Debug.Assert(ro.IsLoaded)
                '                    Add2Cache(ro)
                '                    ar.Add(ro)
                '                End If
                '            End If
                '        Else

                '        End If
                '    End If
                'Next
                For Each o As T2 In objs
                    Dim pos As Integer = values.IndexOf(o)
                    If pos >= 0 Then
                        If withLoad Then
                            If values(pos).IsLoaded Then
                                ar.Add(values(pos))
                            End If
                        Else
                            ar.Add(values(pos))
                        End If
                    End If
                Next
            Else
                result = New ReadOnlyList(Of T2)(realType, values)
                'If ListConverter.IsWeak Then
                '    For Each o As T In objs
                '        Dim obj As T = Nothing
                '        If dic.TryGetValue(o.Key, obj) Then
                '            ar.Add(obj)
                '        Else
                '            ar.Add(o)
                '        End If
                '    Next
                'Else
                '    Return objs
                'End If
            End If
            Return result
            'Return New ReadOnlyList(Of T)(values)
        End Function

        Protected Function GetFilters(ByVal ids_ As Generic.List(Of Object), ByVal fieldName As String, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal original_type As Type, ByVal idsSorted As Boolean) As Generic.IEnumerable(Of Pair(Of String, Integer))

            Dim mr As MergeResult = Nothing
            Dim l As New Generic.List(Of Pair(Of String, Integer))

            If ids_.Count > 0 Then
                Dim int As Integer
                Dim ids As Generic.List(Of Integer) = Nothing
                If Integer.TryParse(ids_(0).ToString, int) Then
                    ids = ids_.ConvertAll(Function(i) Convert.ToInt32(i))
                    mr = MergeIds(ids, Not idsSorted)
                Else
                    Dim sb As New StringBuilder
                    For Each o As Object In ids_
                        sb.Append(o.ToString).Append(",")
                    Next
                    sb.Length -= 1
                    Dim f As New Database.Criteria.Core.EntityFilter(original_type, fieldName, New LiteralValue(sb.ToString), Worm.Criteria.FilterOperation.In)
                    l.Add(New Pair(Of String, Integer)(f.MakeQueryStmt(DbSchema, GetFilterInfo, almgr, params), params.Params.Count))
                End If
            End If

            If mr IsNot Nothing Then
                Dim sb As New StringBuilder

                For Each p As Pair(Of Integer) In mr.Pairs
                    Dim con As New Database.Criteria.Conditions.Condition.ConditionConstructor
                    con.AddFilter(New Database.Criteria.Core.EntityFilter(original_type, fieldName, New ScalarValue(p.First), Worm.Criteria.FilterOperation.GreaterEqualThan))
                    con.AddFilter(New Database.Criteria.Core.EntityFilter(original_type, fieldName, New ScalarValue(p.Second), Worm.Criteria.FilterOperation.LessEqualThan))
                    Dim bf As IFilter = con.Condition
                    'Dim f As IFilter = TryCast(bf, Worm.Database.Criteria.Core.IFilter)
                    'If f IsNot Nothing Then
                    sb.Append(bf.MakeQueryStmt(DbSchema, GetFilterInfo, almgr, params, Nothing))
                    'Else
                    'sb.Append(bf.MakeSQLStmt(DbSchema, params))
                    'End If
                    If sb.Length > SQLGenerator.QueryLength Then
                        l.Add(New Pair(Of String, Integer)(" and (" & sb.ToString & ")", params.Params.Count))
                        sb.Length = 0
                    Else
                        sb.Append(" or ")
                    End If
                Next

                If mr.Rest.Count > 0 Then
                    Dim sb2 As New StringBuilder
                    sb2.Append("(")
                    For Each id As Integer In mr.Rest
                        sb2.Append(id).Append(",")

                        If sb2.Length > SQLGenerator.QueryLength - sb.Length Then
                            sb2.Length -= 1
                            sb2.Append(")")
                            Dim f As New Database.Criteria.Core.EntityFilter(original_type, fieldName, New LiteralValue(sb2.ToString), Worm.Criteria.FilterOperation.In)

                            sb.Append(f.MakeQueryStmt(DbSchema, GetFilterInfo, almgr, params, Nothing))

                            sb.Insert(0, " and (")
                            l.Add(New Pair(Of String, Integer)(sb.ToString & ")", params.Params.Count))
                            sb.Length = 0
                            sb2.Length = 0
                            sb2.Append("(")
                        End If
                    Next
                    If sb2.Length <> 1 Then
                        sb2.Length -= 1
                        sb2.Append(")")
                        Dim f As New Database.Criteria.Core.EntityFilter(original_type, fieldName, New LiteralValue(sb2.ToString), Worm.Criteria.FilterOperation.In)
                        sb.Append(f.MakeQueryStmt(DbSchema, GetFilterInfo, almgr, params))

                        sb.Insert(0, " and (")
                        l.Add(New Pair(Of String, Integer)(sb.ToString & ")", params.Params.Count))
                        sb.Length = 0
                    End If
                End If

                If sb.Length > 0 Then
                    sb.Length -= 4
                    If sb.Length > 0 Then
                        l.Add(New Pair(Of String, Integer)(" and (" & sb.ToString & ")", params.Params.Count))
                    End If
                End If

            End If

            Return l
        End Function

        Protected Function GetFilters(ByVal ids_ As Generic.List(Of Object), ByVal table As SourceFragment, ByVal column As String, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal idsSorted As Boolean) As Generic.IEnumerable(Of Pair(Of String, Integer))

            Dim mr As MergeResult = Nothing
            Dim l As New Generic.List(Of Pair(Of String, Integer))

            If ids_.Count > 0 Then
                Dim int As Integer
                Dim ids As Generic.List(Of Integer) = Nothing
                If Integer.TryParse(ids_(0).ToString, int) Then
                    ids = ids_.ConvertAll(Function(i) Convert.ToInt32(i))
                    mr = MergeIds(ids, Not idsSorted)
                Else
                    Dim sb As New StringBuilder
                    For Each o As Object In ids_
                        sb.Append(o.ToString).Append(",")
                    Next
                    sb.Length -= 1
                    Dim f As New Database.Criteria.Core.TableFilter(table, column, New LiteralValue(sb.ToString), Worm.Criteria.FilterOperation.In)
                    l.Add(New Pair(Of String, Integer)(f.MakeQueryStmt(DbSchema, GetFilterInfo, almgr, params), params.Params.Count))
                End If
            End If

            If mr IsNot Nothing Then
                Dim sb As New StringBuilder

                For Each p As Pair(Of Integer) In mr.Pairs
                    Dim con As New Database.Criteria.Conditions.Condition.ConditionConstructor
                    con.AddFilter(New Database.Criteria.Core.TableFilter(table, column, New ScalarValue(p.First), Worm.Criteria.FilterOperation.GreaterEqualThan))
                    con.AddFilter(New Database.Criteria.Core.TableFilter(table, column, New ScalarValue(p.Second), Worm.Criteria.FilterOperation.LessEqualThan))
                    Dim bf As IFilter = con.Condition
                    'Dim f As Worm.Database.Criteria.Core.IFilter = TryCast(bf, Worm.Database.Criteria.Core.IFilter)
                    'If f IsNot Nothing Then
                    sb.Append(bf.MakeQueryStmt(DbSchema, GetFilterInfo, almgr, params, Nothing))
                    'Else
                    'sb.Append(bf.MakeSQLStmt(DbSchema, params))
                    'End If
                    If sb.Length > SQLGenerator.QueryLength Then
                        l.Add(New Pair(Of String, Integer)(" and (" & sb.ToString & ")", params.Params.Count))
                        sb.Length = 0
                    Else
                        sb.Append(" or ")
                    End If
                Next

                If mr.Rest.Count > 0 Then
                    Dim sb2 As New StringBuilder
                    sb2.Append("(")
                    For Each id As Integer In mr.Rest
                        sb2.Append(id).Append(",")

                        If sb2.Length > SQLGenerator.QueryLength - sb.Length Then
                            sb2.Length -= 1
                            sb2.Append(")")
                            Dim f As New Database.Criteria.Core.TableFilter(table, column, New LiteralValue(sb2.ToString), Worm.Criteria.FilterOperation.In)

                            sb.Append(f.MakeQueryStmt(DbSchema, GetFilterInfo, almgr, params, Nothing))

                            sb.Insert(0, " and (")
                            l.Add(New Pair(Of String, Integer)(sb.ToString & ")", params.Params.Count))
                            sb.Length = 0
                            sb2.Length = 0
                            sb2.Append("(")
                        End If
                    Next
                    If sb2.Length <> 1 Then
                        sb2.Length -= 1
                        sb2.Append(")")
                        Dim f As New Database.Criteria.Core.TableFilter(table, column, New LiteralValue(sb2.ToString), Worm.Criteria.FilterOperation.In)
                        sb.Append(f.MakeQueryStmt(DbSchema, GetFilterInfo, almgr, params))

                        sb.Insert(0, " and (")
                        l.Add(New Pair(Of String, Integer)(sb.ToString & ")", params.Params.Count))
                        sb.Length = 0
                    End If
                End If

                If sb.Length > 0 Then
                    sb.Length -= 4
                    If sb.Length > 0 Then
                        l.Add(New Pair(Of String, Integer)(" and (" & sb.ToString & ")", params.Params.Count))
                    End If
                End If

            End If

            Return l
        End Function

        Protected Overrides Function Search(Of T As {New, IOrmBase})(ByVal type2search As Type, _
            ByVal contextKey As Object, ByVal sort As Sort, ByVal filter As IFilter, _
            ByVal frmt As IFtsStringFormater, Optional ByVal js() As OrmJoin = Nothing) As ReadOnlyList(Of T)

            Dim fields As New List(Of Pair(Of String, Type))
            Dim searchSchema As IOrmObjectSchema = DbSchema.GetObjectSchema(type2search)
            Dim selectType As System.Type = GetType(T)
            Dim selSchema As IOrmObjectSchema = DbSchema.GetObjectSchema(selectType)
            Dim fsearch As IOrmFullTextSupport = TryCast(searchSchema, IOrmFullTextSupport)
            Dim queryFields As String() = Nothing
            Dim selCols, searchCols As New List(Of ColumnAttribute)
            Dim ssearch As IOrmFullTextSupport = TryCast(selSchema, IOrmFullTextSupport)

            Dim joins As New List(Of OrmJoin)
            Dim appendMain As Boolean = PrepareSearch(selectType, type2search, filter, sort, contextKey, fields, _
                joins, selCols, searchCols, queryFields, searchSchema, selSchema)

            Dim col As New List(Of T)
            Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand

                With cmd
                    .CommandType = System.Data.CommandType.Text

                    Dim params As New ParamMgr(DbSchema, "p")
                    .CommandText = DbSchema.MakeSearchStatement(type2search, selectType, frmt, fields, _
                        GetSearchSection, joins, SortType.Desc, params, GetFilterInfo, queryFields, _
                        Integer.MinValue, _
                        "containstable", sort, appendMain, CType(filter, IFilter), contextKey, _
                         selSchema, searchSchema)
                    params.AppendParams(.Parameters)
                End With

                If Not String.IsNullOrEmpty(cmd.CommandText) Then
                    If type2search Is selectType OrElse searchCols.Count = 0 Then
                        LoadMultipleObjects(Of T)(cmd, fields IsNot Nothing, col, selCols)
                    Else
                        LoadMultipleObjects(selectType, type2search, cmd, col, selCols, searchCols)
                    End If
                End If
            End Using

            Dim col2 As New List(Of T)
            Dim f2 As IOrmFullTextSupportEx = TryCast(searchSchema, IOrmFullTextSupportEx)
            Dim tokens() As String = frmt.GetTokens
            If tokens.Length > 1 AndAlso (f2 Is Nothing OrElse f2.UseFreeText) Then
                Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                    With cmd
                        .CommandType = System.Data.CommandType.Text

                        Dim params As New ParamMgr(DbSchema, "p")
                        .CommandText = DbSchema.MakeSearchStatement(type2search, selectType, frmt, fields, _
                            GetSearchSection, joins, SortType.Desc, params, GetFilterInfo, queryFields, 500, _
                            "freetexttable", sort, appendMain, CType(filter, IFilter), _
                            contextKey, selSchema, searchSchema)
                        params.AppendParams(.Parameters)
                    End With

                    If Not String.IsNullOrEmpty(cmd.CommandText) Then
                        If type2search Is selectType OrElse searchCols.Count = 0 Then
                            LoadMultipleObjects(Of T)(cmd, fields IsNot Nothing, col2, selCols)
                        Else
                            LoadMultipleObjects(selectType, type2search, cmd, col2, selCols, searchCols)
                        End If
                    End If
                End Using
            End If

            Dim res As List(Of T) = Nothing
            If fields IsNot Nothing AndAlso selectType Is type2search AndAlso sort Is Nothing AndAlso col.Count > 0 Then
                Dim query As String, sb As New StringBuilder
                For Each tk As String In tokens
                    sb.Append(tk).Append(" ")
                Next
                sb.Length -= 1
                query = sb.ToString
                Dim full As New List(Of T)
                Dim full_part1 As New List(Of T)
                Dim full_part As New List(Of T)
                Dim starts As New List(Of T)
                Dim other As New List(Of T)
                For Each o As T In col
                    Dim str As Boolean = False
                    For Each p As Pair(Of String, Type) In fields
                        'If p.Second Is selectType Then
                        Dim f As String = p.First
                        Dim s As String = CStr(o.GetValue(Nothing, New ColumnAttribute(f), searchSchema))
                        If s IsNot Nothing Then
                            If s.Equals(query, StringComparison.InvariantCultureIgnoreCase) Then
                                full.Add(o)
                                GoTo l1
                            ElseIf s.Replace(".", "").Equals(query, StringComparison.InvariantCultureIgnoreCase) Then
                                full.Add(o)
                                GoTo l1
                            End If

                            Dim ss() As String = s.Split(New Char() {" "c, ","c})
                            For i As Integer = 0 To ss.Length - 1
                                Dim ps As String = ss(i)
                                If Not String.IsNullOrEmpty(ps) Then
                                    If ps.Equals(query, StringComparison.InvariantCultureIgnoreCase) Then
                                        If i = 0 Then
                                            full_part1.Add(o)
                                        Else
                                            full_part.Add(o)
                                        End If
                                        GoTo l1
                                    ElseIf ps.Replace(".", "").Equals(query, StringComparison.InvariantCultureIgnoreCase) Then
                                        If i = 0 Then
                                            full_part1.Add(o)
                                        Else
                                            full_part.Add(o)
                                        End If
                                        GoTo l1
                                    End If
                                End If
                            Next

                            If Not str AndAlso s.StartsWith(query, StringComparison.InvariantCultureIgnoreCase) Then
                                str = True
                            End If
                        End If
                        'End If
                    Next
                    If str Then
                        starts.Add(o)
                    Else
                        other.Add(o)
                    End If
l1:
                Next
                Dim cnt As Integer = full.Count
                _er = New ExecutionResult(cnt + full_part1.Count + full_part.Count + starts.Count + other.Count, Nothing, Nothing, False, 0)

                RaiseOnDataAvailable()

                Dim rf As Integer = Math.Max(0, _start - cnt)
                full.RemoveRange(0, Math.Min(_start, cnt))
                cnt = full.Count
                If cnt < _length Then
                    'If full_part1.Count + cnt < _length Then
                    '    full.AddRange(full_part1)
                    'Else
                    '    full_part1.RemoveRange(0, _length - cnt)
                    '    full.AddRange(full_part1)
                    '    GoTo l2
                    'End If
                    If AddPart(full, full_part1, cnt, rf) Then
                        If AddPart(full, full_part, cnt, rf) Then
                            If AddPart(full, starts, cnt, rf) Then
                                AddPart(full, other, cnt, rf)
                            End If
                        End If
                    End If
                    'full.AddRange(full_part)
                    'full.AddRange(starts)
                    'full.AddRange(other)
                Else
                    full.RemoveRange(0, _length)
                End If
l2:
                res = full
            Else
                _er = New ExecutionResult(col.Count, Nothing, Nothing, False, 0)
                RaiseOnDataAvailable()

                If _length = Integer.MaxValue AndAlso _start = 0 Then
                    res = New List(Of T)(col)
                Else
                    Dim l As IList(Of T) = CType(col, Global.System.Collections.Generic.IList(Of T))
                    res = New List(Of T)
                    For i As Integer = _start To Math.Min(_start + _length, col.Count) - 1
                        res.Add(l(i))
                    Next
                End If
            End If

            If col2 IsNot Nothing AndAlso res.Count < _length Then
                Dim dic As New Dictionary(Of Object, T)
                For Each o As T In res
                    dic.Add(o.Identifier, o)
                Next
                Dim i As Integer = res.Count
                For Each o As T In col2
                    If Not dic.ContainsKey(o.Identifier) Then
                        res.Add(o)
                        i += 1
                        If i = _start + _length Then
                            Exit For
                        End If
                    End If
                Next
                _er = New ExecutionResult(_er.Count + col2.Count, Nothing, Nothing, False, 0)
            End If

            Return New ReadOnlyList(Of T)(res)
        End Function

        Protected Overrides Function SearchEx(Of T As {IOrmBase, New})(ByVal type2search As Type, _
            ByVal contextKey As Object, ByVal sort As Sort, ByVal filter As IFilter, ByVal ftsText As String, _
            ByVal limit As Integer, ByVal fts As IFtsStringFormater) As ReadOnlyList(Of T)

            Dim selectType As System.Type = GetType(T)
            Dim fields As New List(Of Pair(Of String, Type))
            Dim joins As New List(Of OrmJoin)
            Dim selCols, searchCols As New List(Of ColumnAttribute)
            Dim queryFields As String() = Nothing

            Dim searchSchema As IOrmObjectSchema = DbSchema.GetObjectSchema(type2search)
            Dim selSchema As IOrmObjectSchema = DbSchema.GetObjectSchema(selectType)

            Dim appendMain As Boolean = PrepareSearch(selectType, type2search, filter, sort, contextKey, fields, _
                joins, selCols, searchCols, queryFields, searchSchema, selSchema)

            Return New ReadOnlyList(Of T)(MakeSqlStmtSearch(Of T)(type2search, selectType, fields, queryFields, joins.ToArray, sort, appendMain, _
                filter, selCols, searchCols, ftsText, limit, fts, contextKey))
        End Function

        Public Function PrepareSearch(ByVal selectType As Type, ByVal type2search As Type, ByVal filter As IFilter, _
            ByVal sort As Sort, ByVal contextKey As Object, ByVal fields As IList(Of Pair(Of String, Type)), _
            ByVal joins As IList(Of OrmJoin), ByVal selCols As IList(Of ColumnAttribute), _
            ByVal searchCols As IList(Of ColumnAttribute), ByRef queryFields As String(), _
            ByVal searchSchema As IOrmObjectSchema, ByVal selSchema As IOrmObjectSchema) As Boolean

            'Dim searchSchema As IOrmObjectSchema = DbSchema.GetObjectSchema(type2search)
            'Dim selSchema As IOrmObjectSchema = DbSchema.GetObjectSchema(selectType)
            Dim fsearch As IOrmFullTextSupport = TryCast(searchSchema, IOrmFullTextSupport)
            'Dim queryFields As String() = Nothing
            Dim ssearch As IOrmFullTextSupport = TryCast(selSchema, IOrmFullTextSupport)
            'If ssearch IsNot Nothing Then
            '    Dim ss() As String = fsearch.GetIndexedFields
            '    If ss IsNot Nothing Then
            '        For Each s As String In ss
            '            fields.Add(New Pair(Of String, Type)(s, selectType))
            '            selCols.Add(New ColumnAttribute(s))
            '        Next
            '    End If
            '    If selCols.Count > 0 Then
            '        selCols.Insert(0, New ColumnAttribute("ID"))
            '        fields.Insert(0, New Pair(Of String, Type)("ID", selectType))
            '    End If
            'End If
            'If selectType IsNot type2search Then
            selCols.Insert(0, New ColumnAttribute("ID", Field2DbRelations.PK))
            fields.Insert(0, New Pair(Of String, Type)("ID", selectType))
            'End If

            Dim types As New List(Of Type)

            If selectType IsNot type2search Then
                Dim field As String = _schema.GetJoinFieldNameByType(selectType, type2search, selSchema)

                If String.IsNullOrEmpty(field) Then
                    field = _schema.GetJoinFieldNameByType(type2search, selectType, searchSchema)

                    If String.IsNullOrEmpty(field) Then
                        Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2search))
                    End If

                    joins.Add(MakeJoin(selectType, type2search, field, Worm.Criteria.FilterOperation.Equal, JoinType.Join))
                Else
                    joins.Add(MakeJoin(type2search, selectType, field, Worm.Criteria.FilterOperation.Equal, JoinType.Join, True))
                End If

                types.Add(selectType)
            End If

            Dim appendMain As Boolean = False
            If sort IsNot Nothing Then
                Dim ns As Sort = sort
                Do
                    Dim sortType As System.Type = ns.Type
                    ns = ns.Previous
                    If sortType Is Nothing Then
                        sortType = selectType
                    End If
                    appendMain = type2search Is sortType OrElse appendMain
                    If Not types.Contains(sortType) Then
                        If type2search IsNot sortType Then
                            Dim srtschema As IOrmObjectSchemaBase = _schema.GetObjectSchema(sortType)
                            Dim field As String = _schema.GetJoinFieldNameByType(type2search, sortType, searchSchema)
                            If Not String.IsNullOrEmpty(field) Then
                                joins.Add(MakeJoin(sortType, type2search, field, Worm.Criteria.FilterOperation.Equal, JoinType.Join))
                                types.Add(sortType)
                                Continue Do
                            End If

                            'field = _schema.GetJoinFieldNameByType(sortType, type2search, srtschema)
                            'If Not String.IsNullOrEmpty(field) Then
                            '    joins.Add(MakeJoin(type2search, sortType, field, FilterOperation.Equal, JoinType.Join, True))
                            '    Continue Do
                            'End If

                            If selectType IsNot type2search Then
                                'field = _schema.GetJoinFieldNameByType(sortType, selectType, srtschema)
                                'If Not String.IsNullOrEmpty(field) Then
                                '    joins.Add(MakeJoin(sortType, selectType, field, FilterOperation.Equal, JoinType.Join))
                                '    Continue Do
                                'End If

                                field = _schema.GetJoinFieldNameByType(selectType, sortType, selSchema)
                                If Not String.IsNullOrEmpty(field) Then
                                    joins.Add(MakeJoin(selectType, sortType, field, Worm.Criteria.FilterOperation.Equal, JoinType.Join, True))
                                    types.Add(sortType)
                                    Continue Do
                                End If
                            End If

                            If String.IsNullOrEmpty(field) Then
                                Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2search))
                            End If
                        End If
                    End If
                Loop While ns IsNot Nothing
            End If

            If filter IsNot Nothing Then
                For Each f As IFilter In filter.GetAllFilters
                    Dim ef As IEntityFilter = TryCast(f, IEntityFilter)
                    If ef IsNot Nothing Then
                        Dim type2join As System.Type = CType(ef.GetFilterTemplate, OrmFilterTemplateBase).Type
                        If type2join Is Nothing Then
                            Throw New NullReferenceException("Type for OrmFilterTemplate must be specified")
                        End If
                        appendMain = type2search Is type2join OrElse appendMain
                        If type2search IsNot type2join Then
                            If Not types.Contains(type2join) Then
                                Dim field As String = _schema.GetJoinFieldNameByType(type2search, type2join, searchSchema)

                                If Not String.IsNullOrEmpty(field) Then
                                    joins.Add(MakeJoin(type2join, type2search, field, Worm.Criteria.FilterOperation.Equal, JoinType.Join))
                                    types.Add(type2join)
                                Else
                                    If selectType IsNot type2search Then
                                        field = _schema.GetJoinFieldNameByType(selectType, type2join, selSchema)
                                        If Not String.IsNullOrEmpty(field) Then
                                            joins.Add(MakeJoin(selectType, type2join, field, Worm.Criteria.FilterOperation.Equal, JoinType.Join, True))
                                            types.Add(type2join)
                                            Continue For
                                        End If
                                    End If

                                    Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2join))
                                End If
                            End If
                        End If
                    Else
                        Dim tf As Database.Criteria.Core.TableFilter = TryCast(f, Database.Criteria.Core.TableFilter)
                        If tf IsNot Nothing Then
                            If tf.Template.Table IsNot selSchema.GetTables(0) Then
                                Throw New NotImplementedException
                            Else
                                appendMain = True
                            End If
                        Else
                            appendMain = True
                            'Throw New NotImplementedException
                        End If
                    End If
                Next
            End If

            If fsearch IsNot Nothing Then
                Dim ss() As String = fsearch.GetIndexedFields
                If ss IsNot Nothing Then
                    For Each s As String In ss
                        fields.Add(New Pair(Of String, Type)(s, type2search))
                        searchCols.Add(New ColumnAttribute(s))
                    Next
                End If

                If searchCols.Count > 0 Then
                    For Each c As ColumnAttribute In searchCols
                        selCols.Add(c)
                    Next
                    'searchCols.Insert(0, New ColumnAttribute("ID", Field2DbRelations.PK))
                    'fields.Insert(0, New Pair(Of String, Type)("ID", type2search))
                End If

                If contextKey IsNot Nothing Then
                    queryFields = fsearch.GetQueryFields(contextKey)
                End If
            End If

            Return appendMain
        End Function

        Public Function MakeSqlStmtSearch(Of T As {IOrmBase, New})(ByVal type2search As Type, _
            ByVal selectType As Type, ByVal fields As ICollection(Of Pair(Of String, Type)), ByVal queryFields() As String, _
            ByVal joins() As OrmJoin, ByVal sort As Sort, ByVal appendMain As Boolean, ByVal filter As IFilter, _
            ByVal selCols As List(Of ColumnAttribute), ByVal searchCols As List(Of ColumnAttribute), _
            ByVal ftsText As String, ByVal limit As Integer, ByVal fts As IFtsStringFormater, ByVal contextkey As Object) As ReadOnlyList(Of T)

            Dim selSchema As IOrmObjectSchema = CType(_schema.GetObjectSchema(selectType), IOrmObjectSchema)
            Dim searchSchema As IOrmObjectSchema = CType(_schema.GetObjectSchema(type2search), IOrmObjectSchema)

            Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand

                With cmd
                    .CommandType = System.Data.CommandType.Text

                    Dim params As New ParamMgr(DbSchema, "p")
                    .CommandText = DbSchema.MakeSearchStatement(type2search, selectType, fts, fields, _
                        GetSearchSection, joins, SortType.Desc, params, GetFilterInfo, queryFields, _
                        limit, ftsText, sort, appendMain, CType(filter, IFilter), contextkey, selSchema, searchSchema)
                    params.AppendParams(.Parameters)
                End With

                Dim r As New List(Of T)
                If type2search Is selectType OrElse searchCols.Count = 0 Then
                    LoadMultipleObjects(Of T)(cmd, fields IsNot Nothing, r, selCols)
                Else
                    LoadMultipleObjects(selectType, type2search, cmd, r, selCols, searchCols)
                End If
                Return New ReadOnlyList(Of T)(r)
            End Using

        End Function

        Private Function AddPart(Of T)(ByVal full As List(Of T), ByVal part As List(Of T), ByRef cnt As Integer, ByRef rf As Integer) As Boolean
            Dim r As Integer = rf
            Dim pcnt As Integer = part.Count
            rf = Math.Max(0, rf - pcnt)
            part.RemoveRange(0, Math.Min(r, pcnt))
            pcnt = part.Count
            If pcnt + cnt <= _length Then
                full.AddRange(part)
                cnt = full.Count
            Else
                part.RemoveRange(_length - cnt, pcnt - (_length - cnt))
                full.AddRange(part)
                Return False
            End If
            Return cnt < _length
        End Function

        Protected Overrides Function BuildDictionary(Of T As {New, IOrmBase})(ByVal level As Integer, ByVal filter As IFilter, ByVal joins() As OrmJoin) As DicIndex(Of T)
            Invariant()
            Dim params As New ParamMgr(DbSchema, "p")
            Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                cmd.CommandText = DbSchema.GetDictionarySelect(GetType(T), level, params, CType(filter, IFilter), joins, GetFilterInfo)
                cmd.CommandType = System.Data.CommandType.Text
                params.AppendParams(cmd.Parameters)
                Dim b As ConnAction = TestConn(cmd)
                Try
                    Dim root As DicIndex(Of T) = BuildDictionaryInternal(Of T)(cmd, level, Me, Nothing, Nothing)
                    root.Filter = filter
                    root.Join = joins
                    Return root
                Finally
                    CloseConn(b)
                End Try
            End Using
        End Function

        Protected Overrides Function BuildDictionary(Of T As {New, IOrmBase})(ByVal level As Integer, _
            ByVal filter As IFilter, ByVal joins() As OrmJoin, ByVal firstField As String, ByVal secondField As String) As DicIndex(Of T)
            Invariant()
            Dim params As New ParamMgr(DbSchema, "p")
            Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                cmd.CommandText = DbSchema.GetDictionarySelect(GetType(T), level, params, CType(filter, IFilter), joins, GetFilterInfo, firstField, secondField)
                cmd.CommandType = System.Data.CommandType.Text
                params.AppendParams(cmd.Parameters)
                Dim b As ConnAction = TestConn(cmd)
                Try
                    Dim root As DicIndex(Of T) = BuildDictionaryInternal(Of T)(cmd, level, Me, firstField, secondField)
                    root.Filter = filter
                    root.Join = joins
                    Return root
                Finally
                    CloseConn(b)
                End Try
            End Using
        End Function

        Protected Shared Function BuildDictionaryInternal(Of T As {New, IOrmBase})(ByVal cmd As System.Data.Common.DbCommand, ByVal level As Integer, ByVal mgr As OrmReadOnlyDBManager, _
            ByVal firstField As String, ByVal secField As String) As DicIndex(Of T)
            Dim last As DicIndex(Of T) = New DicIndex(Of T)("ROOT", Nothing, 0, firstField, secField)
            Dim root As DicIndex(Of T) = last
            'Dim arr As New Hashtable
            'Dim arr1 As New ArrayList
            Dim first As Boolean = True
            Dim et As New PerfCounter
            Using dr As System.Data.IDataReader = cmd.ExecuteReader
                If mgr IsNot Nothing Then
                    mgr._exec = et.GetTime
                End If
                Dim ft As New PerfCounter
                Do While dr.Read

                    Dim name As String = dr.GetString(0)
                    Dim cnt As Integer = dr.GetInt32(1)

                    BuildDic(Of T)(name, cnt, level, root, last, first, firstField, secField)
                Loop
                If mgr IsNot Nothing Then
                    mgr._fetch = ft.GetTime
                End If
            End Using

            'Dim tt As Type = GetType(MediaIndex(Of T))
            'Return CType(arr1.ToArray(tt), MediaIndex(Of T)())
            Return root
        End Function

        Protected Friend Overrides Function UpdateObject(ByVal obj As _ICachedEntity) As Boolean
            Throw New NotImplementedException()
        End Function

        'Public Overrides Function AddObject(ByVal obj As OrmBase) As OrmBase
        '    Throw New NotImplementedException()
        'End Function

        Protected Overrides Function InsertObject(ByVal obj As _ICachedEntity) As Boolean
            Throw New NotImplementedException()
        End Function

        Protected Friend Overrides Sub DeleteObject(ByVal obj As ICachedEntity)
            Throw New NotImplementedException()
        End Sub

        'Protected Overrides Sub Obj2ObjRelationSave2(ByVal obj As OrmBase, ByVal dt As System.Data.DataTable, ByVal sync As String, ByVal t As System.Type)
        '    Throw New NotImplementedException()
        'End Sub

        Protected Overrides Sub M2MSave(ByVal obj As Orm.IOrmBase, ByVal t As System.Type, ByVal key As String, ByVal el As EditableListBase)
            Throw New NotImplementedException
        End Sub

        'Public Overrides Function SaveChanges(ByVal obj As OrmBase, ByVal AcceptChanges As Boolean) As Boolean
        '    Throw New NotImplementedException()
        'End Function

        Protected Overrides Function GetSearchSection() As String
            Return String.Empty
        End Function

        Protected Friend Overrides ReadOnly Property Exec() As System.TimeSpan
            Get
                Return _exec
            End Get
        End Property

        Protected Friend Overrides ReadOnly Property Fecth() As System.TimeSpan
            Get
                Return _fetch
            End Get
        End Property

        Protected Friend Function LoadM2M(Of T As {IOrmBase, New})(ByVal cmd As System.Data.Common.DbCommand, ByVal withLoad As Boolean, _
            ByVal obj As IOrmBase, ByVal sort As Sort, ByVal columns As IList(Of ColumnAttribute)) As List(Of Object)
            Dim b As ConnAction = TestConn(cmd)
            Dim tt As Type = GetType(T)
            Try
                If withLoad Then
                    _cache.BeginTrackDelete(tt)
                End If
                _loadedInLastFetch = 0
                Dim dic As IDictionary = CType(GetDictionary(Of T)(), System.Collections.IDictionary)
                Dim et As New PerfCounter
                Dim l As New List(Of Object)
                Using dr As System.Data.IDataReader = cmd.ExecuteReader
                    _exec = et.GetTime
                    Dim oschema As IOrmObjectSchema = DbSchema.GetObjectSchema(tt)
                    Dim ft As New PerfCounter
                    Do While dr.Read
                        Dim id1 As Object = dr.GetValue(0)
                        If Not id1.Equals(obj.Identifier) Then
                            Throw New OrmManagerException("Wrong relation statement")
                        End If
                        Dim id2 As Object = dr.GetValue(1)
                        l.Add(id2)
                        Dim o As T = GetOrmBaseFromCacheOrCreate(Of T)(id2)
                        If withLoad AndAlso Not _cache.IsDeleted(tt, o.Key) Then
                            If o.ObjectState <> ObjectState.Modified Then
                                Using o.GetSyncRoot()
                                    'If obj.IsLoaded Then obj.IsLoaded = False
                                    Dim lock As IDisposable = Nothing
                                    Try
                                        Dim ro As _IEntity = LoadFromDataReader(o, dr, columns, False, 2, dic, True, lock, oschema, oschema.GetFieldColumnMap)
                                        AfterLoadingProcess(dic, o, lock, ro)
                                    Finally
                                        If lock IsNot Nothing Then
                                            'Threading.Monitor.Exit(dic)
                                            lock.Dispose()
                                        End If
                                    End Try
                                    'ro.CorrectStateAfterLoading(Object.ReferenceEquals(ro, o))
                                    _loadedInLastFetch += 1
                                End Using
                            End If
                        End If
                    Loop
                    _fetch = ft.GetTime
                End Using

                If sort IsNot Nothing AndAlso sort.IsExternal Then
                    Dim l2 As New List(Of Object)
                    For Each o As T In DbSchema.ExternalSort(Of T)(Me, sort, ConvertIds2Objects(Of T)(l, False))
                        l2.Add(o.Identifier)
                    Next
                    l = l2
                End If
                Return l
            Finally
                If withLoad Then
                    _cache.EndTrackDelete(tt)
                End If
                CloseConn(b)
            End Try
        End Function
    End Class
End Namespace
