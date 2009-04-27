Imports Worm.Entities
Imports System.Collections.Generic
Imports Worm.Cache
Imports Worm.Entities.Meta

Namespace Database

    Public Class ObjectListSaver
        Implements IDisposable

        Public Enum FurtherActionEnum
            [Default]
            Retry
            RetryLater
            Skip
        End Enum

        Private _disposedValue As Boolean
        Private _objects As New List(Of _ICachedEntity)
        Private _mgr As OrmReadOnlyDBManager
        Private _acceptInBatch As Boolean
        Private _callbacks As IUpdateCacheCallbacks
        Private _save As Nullable(Of Boolean)
        'Private _disposeMgr As Boolean
        Private _commited As Boolean
        Private _lockList As New Dictionary(Of ObjectWrap(Of ICachedEntity), IDisposable)
        Private _removed As New List(Of ICachedEntity)
        Private _dontTrackRemoved As Boolean
        Private _startSave As Boolean
        Private _error As Boolean = True
        Private _graphDepth As Integer = 4
        Private _dontResolve As Boolean

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

        Public Class SaveErrorEventArgs
            Inherits EventArgs

            Private _o As ICachedEntity
            Public ReadOnly Property Entity() As ICachedEntity
                Get
                    Return _o
                End Get
            End Property

            Private _action As FurtherActionEnum
            Public Property FurtherAction() As FurtherActionEnum
                Get
                    Return _action
                End Get
                Set(ByVal value As FurtherActionEnum)
                    _action = value
                End Set
            End Property

            Private _ex As Exception
            Public ReadOnly Property Exception() As Exception
                Get
                    Return _ex
                End Get
            End Property

            Public Sub New(ByVal obj As ICachedEntity, ByVal exception As Exception)
                _o = obj
                _ex = exception
            End Sub
        End Class

        Public Event PreSave(ByVal sender As ObjectListSaver)
        Public Event BeginSave(ByVal sender As ObjectListSaver, ByVal count As Integer)
        Public Event CannotSave(ByVal sender As ObjectListSaver, ByVal args As CannotSaveEventArgs)
        Public Event EndSave(ByVal sender As ObjectListSaver)
        Public Event ObjectSaving(ByVal sender As ObjectListSaver, ByVal args As CancelEventArgs)
        Public Event ObjectSavingError(ByVal sender As ObjectListSaver, ByVal args As SaveErrorEventArgs)
        Public Event ObjectPostponed(ByVal sender As ObjectListSaver, ByVal o As ICachedEntity)
        Public Event ObjectSaved(ByVal sender As ObjectListSaver, ByVal o As ICachedEntity)
        Public Event ObjectAccepting(ByVal sender As ObjectListSaver, ByVal o As ICachedEntity)
        Public Event ObjectAccepted(ByVal sender As ObjectListSaver, ByVal o As ICachedEntity)
        Public Event ObjectRejecting(ByVal sender As ObjectListSaver, ByVal o As ICachedEntity)
        Public Event ObjectRejected(ByVal sender As ObjectListSaver, ByVal o As ICachedEntity, ByVal inLockList As Boolean)
        Public Event ObjectRestored(ByVal sender As ObjectListSaver, ByVal o As ICachedEntity)
        Public Event SaveSuccessed(ByVal sender As ObjectListSaver)
        Public Event SaveFailed(ByVal sender As ObjectListSaver)
        Public Event BeginRejecting(ByVal sender As ObjectListSaver)
        Public Event BeginAccepting(ByVal sender As ObjectListSaver)
        Public Event OnAdded(ByVal sender As ObjectListSaver, ByVal o As ICachedEntity, ByVal added As Boolean)

        'Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal dispose As Boolean)
        '    _mgr = mgr
        '    _disposeMgr = dispose
        'End Sub

        Public Sub New()
        End Sub

        Friend Sub New(ByVal mgr As OrmReadOnlyDBManager)
            _mgr = mgr
        End Sub

        'Public Sub New()
        '    _mgr = CType(OrmManager.CurrentManager, OrmReadOnlyDBManager)
        'End Sub

        Public ReadOnly Property [Error]() As Boolean
            Get
                Return _error
            End Get
        End Property

        Public ReadOnly Property AffectedObjects() As ICollection(Of _ICachedEntity)
            Get
                Dim l As New List(Of _ICachedEntity)(_objects)
                Using SyncHelper.AcquireDynamicLock("akdfvnd")
                    For Each o As _ICachedEntity In _removed
                        l.Remove(o)
                    Next
                End Using
                Return l
            End Get
        End Property

        Public Property Manager() As OrmReadOnlyDBManager
            Get
                Return _mgr
            End Get
            Friend Set(ByVal value As OrmReadOnlyDBManager)
                _mgr = value
            End Set
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

        Public Property GraphDepth() As Integer
            Get
                Return _graphDepth
            End Get
            Set(ByVal value As Integer)
                _graphDepth = value
            End Set
        End Property

        Public Property ResolveDepends() As Boolean
            Get
                Return Not _dontResolve
            End Get
            Set(ByVal value As Boolean)
                _dontResolve = Not value
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

        Public ReadOnly Property StartSaving() As Boolean
            Get
                Return _startSave
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

        Public Property CacheCallbacks() As IUpdateCacheCallbacks
            Get
                Return _callbacks
            End Get
            Set(ByVal value As IUpdateCacheCallbacks)
                _callbacks = value
            End Set
        End Property

        Public Overridable Function Add(ByVal o As _ICachedEntity) As Boolean
            Dim added As Boolean
            If Not _objects.Contains(o) Then
                added = True
                _objects.Add(o)
                AddHandler o.OriginalCopyRemoved, AddressOf ObjRejected
#If DEBUG Then
                If o.HasChanges Then
                    Dim mo As ObjectModification = o.ShadowCopy(_mgr)
                    Dim oc As ICachedEntity = o.OriginalCopy
                    If o.ObjectState = ObjectState.Deleted Then
                        _deleted.Add(o)
                        Debug.Assert(o IsNot Nothing)
                        If mo IsNot Nothing Then
                            Debug.Assert(mo.Reason = ObjectModification.ReasonEnum.Delete OrElse mo.Reason = ObjectModification.ReasonEnum.Edit)
                        End If
                    ElseIf o.ObjectState = ObjectState.Modified Then
                        _updated.Add(o)
                        Debug.Assert(o IsNot Nothing)
                        If mo IsNot Nothing Then
                            Debug.Assert(mo.Reason = ObjectModification.ReasonEnum.Edit)
                        End If
                    End If
                End If
#End If
            End If
            RaiseEvent OnAdded(Me, o, added)
            Return added
        End Function

        Public Sub AddRange(ByVal col As ICollection(Of _ICachedEntity))
            '_objects.AddRange(col)
            'For Each o As OrmBase In col
            '    AddHandler o.OriginalCopyRemoved, AddressOf ObjRejected
            'Next
            For Each o As _ICachedEntity In col
                Add(o)
            Next
        End Sub

        Protected Sub ObjRejected(ByVal o As ICachedEntity)
            If Not _startSave Then
                Using SyncHelper.AcquireDynamicLock("akdfvnd")
                    _removed.Add(o)
                End Using
            End If
        End Sub

        Protected Function GetObjWrap(ByVal obj As ICachedEntity) As ObjectWrap(Of ICachedEntity)
            For Each o As ObjectWrap(Of ICachedEntity) In _lockList.Keys
                If ReferenceEquals(o.Value, obj) Then
                    Return o
                End If
            Next
            Throw New InvalidOperationException("Cannot find object")
        End Function

        Protected Sub ObjectAcceptedHandler(ByVal sender As ObjectListSaver, ByVal obj As ICachedEntity) Handles Me.ObjectAccepted
            Dim o As ObjectWrap(Of ICachedEntity) = GetObjWrap(obj)
            _lockList(o).Dispose()
            _lockList.Remove(o)
        End Sub

        Protected Sub ObjectRejectedHandler(ByVal sender As ObjectListSaver, ByVal obj As ICachedEntity, ByVal inLockList As Boolean) Handles Me.ObjectRejected
            If inLockList Then
                Dim o As ObjectWrap(Of ICachedEntity) = GetObjWrap(obj)
                _lockList(o).Dispose()
                _lockList.Remove(o)
            End If
        End Sub

        Protected Overridable Function CheckObj(ByVal main As _ICachedEntity, ByVal o As ICachedEntity) As Boolean
            If o.ObjectState <> ObjectState.Deleted Then Return False
            Dim oSchema As IEntitySchema = _mgr.MappingEngine.GetEntitySchema(o.GetType)
            For Each m As MapField2Column In _mgr.MappingEngine.GetMappedFields(oSchema)
                If main.Equals(_mgr.MappingEngine.GetPropertyValue(o, m._propertyAlias, oSchema)) Then
                    Return True
                End If
            Next
        End Function

        Protected Overridable Function CanSaveObj(ByVal o As _ICachedEntity, ByVal col2save As IList) As Boolean
            If _dontResolve Then Return True
            If o.ObjectState <> ObjectState.Deleted Then Return True
            If col2save Is Nothing Then col2save = _objects
            Dim pos As Integer = col2save.IndexOf(o)
            For i As Integer = pos + 1 To col2save.Count - 1
                If CheckObj(o, _objects(i)) Then
                    Return False
                End If
            Next
            Return True
        End Function

        Protected Function SaveObj(ByVal o As _ICachedEntity, ByVal need2save As List(Of ICachedEntity), _
            ByVal saved As List(Of Pair(Of ObjectState, _ICachedEntity)), _
            Optional ByVal col2save As IList = Nothing) As Boolean
            Dim owr As ObjectWrap(Of ICachedEntity) = Nothing
            Try
l1:
                Dim args As New CancelEventArgs(o)
                RaiseEvent ObjectSaving(Me, args)
                If Not args.Cancel Then
                    If CanSaveObj(o, col2save) Then
                        owr = New ObjectWrap(Of ICachedEntity)(o)
                        _lockList.Add(owr, _mgr.GetSyncForSave(o.GetType, o)) 'o.GetSyncRoot)
                        Dim os As ObjectState = o.ObjectState
                        Dim oldME As ObjectMappingEngine = Nothing
                        Dim blb As Boolean
                        Try
                            If Not _mgr.MappingEngine.Equals(o.MappingEngine) Then
                                oldME = _mgr._schema
                                _mgr._schema = o.MappingEngine
                            End If
                            blb = _mgr.SaveChanges(o, False)
                        Finally
                            If oldME IsNot Nothing Then
                                _mgr._schema = oldME
                            End If
                        End Try
                        If blb Then
                            _lockList(owr).Dispose()
                            _lockList.Remove(owr)
                            RaiseEvent ObjectPostponed(Me, o)
                            need2save.Add(o)
                        Else
                            saved.Add(New Pair(Of ObjectState, _ICachedEntity)(os, o))
                            RaiseEvent ObjectSaved(Me, o)
                        End If
                    Else
                        RaiseEvent ObjectPostponed(Me, o)
                        need2save.Add(o)
                        Return False
                    End If
                End If
                Return args.Cancel
            Catch ex As Exception
                If owr IsNot Nothing Then
                    Dim args As New SaveErrorEventArgs(o, ex)
                    RaiseEvent ObjectSavingError(Me, args)
                    Select Case args.FurtherAction
                        Case FurtherActionEnum.Retry
                            _lockList(owr).Dispose()
                            _lockList.Remove(owr)
                            GoTo l1
                        Case FurtherActionEnum.Skip
                            _lockList(owr).Dispose()
                            _lockList.Remove(owr)
                            Return True
                        Case FurtherActionEnum.RetryLater
                            _lockList(owr).Dispose()
                            _lockList.Remove(owr)
                            RaiseEvent ObjectPostponed(Me, o)
                            need2save.Add(o)
                            Return False
                    End Select
                End If

                Throw New OrmManagerException("Error during save " & o.ObjName, ex)
            End Try
        End Function

        Protected Sub Save()
            If _mgr.Cache.IsReadonly Then
                Throw New InvalidOperationException("Cache is readonly")
            End If

            RaiseEvent PreSave(Me)
            Dim hasTransaction As Boolean = _mgr.Transaction IsNot Nothing
            Dim saved As New List(Of Pair(Of ObjectState, _ICachedEntity)), copies As New List(Of Pair(Of ICachedEntity))
            Dim rejectList As New List(Of ICachedEntity), need2save As New List(Of ICachedEntity)
            _startSave = True
#If DebugLocks Then
                Using New CSScopeMgr_Debug(_mgr.Cache, "d:\temp\")
#Else
            Using _mgr.Cache.SyncRoot2
#End If
                _mgr.BeginTransaction()
                Try
                    RaiseEvent BeginSave(Me, _objects.Count)
                    For Each o As _ICachedEntity In _objects
                        RemoveHandler o.OriginalCopyRemoved, AddressOf ObjRejected

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

                    For i As Integer = 0 To _graphDepth
                        Dim ns As New List(Of ICachedEntity)(need2save)
                        need2save.Clear()
                        For Each o As _ICachedEntity In ns
                            If SaveObj(o, need2save, saved, ns) Then
                                rejectList.Remove(o)
                                Dim idx As Integer = -1
                                For j As Integer = 0 To copies.Count - 1
                                    Dim p As Pair(Of ICachedEntity) = copies(j)
                                    If p.First.Equals(o) Then
                                        idx = j
                                        Exit For
                                    End If
                                Next
                                If idx >= 0 Then
                                    copies.RemoveAt(idx)
                                End If
                            End If
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

                    RaiseEvent EndSave(Me)
                    _error = False
                Finally
                    _startSave = False
                    Try
                        If _error Then
                            If Not hasTransaction Then
                                _mgr.Rollback()
                            End If

                            RaiseEvent BeginRejecting(Me)
                            Rollback(saved, rejectList, copies, need2save)
                            RaiseEvent SaveFailed(Me)
                        Else
                            If Not hasTransaction Then
                                _mgr.Commit()
                                _commited = True
                            End If

                            RaiseEvent BeginAccepting(Me)
                            If _acceptInBatch Then
                                'Dim l As New Dictionary(Of OrmBase, OrmBase)
                                Dim l2 As New Dictionary(Of Type, List(Of Pair(Of _ICachedEntity)))
                                Dim val As New List(Of ICachedEntity)
                                For Each p As Pair(Of ObjectState, _ICachedEntity) In saved
                                    Dim o As _ICachedEntity = p.Second
                                    RaiseEvent ObjectAccepting(Me, o)
                                    Dim mo As ICachedEntity = o.AcceptChanges(False, KeyEntity.IsGoodState(p.First))
                                    Debug.Assert(_mgr.Cache.ShadowCopy(o, _mgr) Is Nothing)
                                    'l.Add(o, mo)
                                    RaiseEvent ObjectAccepted(Me, o)
                                    If o.UpdateCtx.UpdatedFields IsNot Nothing Then
                                        val.Add(o)
                                    End If
                                    Dim ls As List(Of Pair(Of _ICachedEntity)) = Nothing
                                    If Not l2.TryGetValue(o.GetType, ls) Then
                                        ls = New List(Of Pair(Of _ICachedEntity))
                                        l2.Add(o.GetType, ls)
                                    End If
                                    ls.Add(New Pair(Of _ICachedEntity)(o, CType(mo, _ICachedEntity)))
                                Next
                                For Each t As Type In l2.Keys
                                    Dim ls As List(Of Pair(Of _ICachedEntity)) = l2(t)
                                    '_mgr.Cache.UpdateCache(_mgr.ObjectSchema, ls, _mgr, _
                                    '    AddressOf OrmBase.Accept_AfterUpdateCache, l, _callbacks)
                                    CType(_mgr.Cache, OrmCache).UpdateCache(_mgr.MappingEngine, ls, _mgr, _
                                        AddressOf CachedEntity.ClearCacheFlags, Nothing, _callbacks, False, False)
                                Next
                                For Each o As _ICachedEntity In val
                                    o.UpdateCacheAfterUpdate(CType(_mgr.Cache, OrmCache))
                                Next
                            Else
                                Dim svd As New List(Of Pair(Of _ICachedEntity))
                                For Each p As Pair(Of ObjectState, _ICachedEntity) In saved
                                    Dim o As _ICachedEntity = p.Second
                                    RaiseEvent ObjectAccepting(Me, o)
                                    Dim mo As ICachedEntity = o.AcceptChanges(False, KeyEntity.IsGoodState(p.First))
                                    Debug.Assert(o.ShadowCopy(_mgr) Is Nothing)
                                    RaiseEvent ObjectAccepted(Me, o)
                                    svd.Add(New Pair(Of _ICachedEntity)(o, CType(mo, _ICachedEntity)))
                                Next
                                For Each p As Pair(Of _ICachedEntity) In svd
                                    Dim o As _ICachedEntity = p.First
                                    o.UpdateCache(_mgr, p.Second)
                                Next
                            End If

                            RaiseEvent SaveSuccessed(Me)
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

        Private Sub Rollback(ByVal saved As List(Of Pair(Of ObjectState, _ICachedEntity)), _
            ByVal rejectList As List(Of ICachedEntity), ByVal copies As List(Of Pair(Of ICachedEntity)), ByVal need2save As List(Of ICachedEntity))
            For Each o As _ICachedEntity In rejectList
                RaiseEvent ObjectRejecting(Me, o)
                _dontTrackRemoved = True
                o.RejectChanges(_mgr)
                _dontTrackRemoved = False
                RaiseEvent ObjectRejected(Me, o, Not need2save.Contains(o))
            Next
            For Each o As Pair(Of ICachedEntity) In copies
                o.First.CopyBody(o.Second, o.First)
                o.First.SetObjectState(o.Second.GetOldState)
                Dim orm As _IKeyEntity = TryCast(o.First, _IKeyEntity)
                If orm IsNot Nothing Then
                    orm.RejectM2MIntermidiate()
                End If
                RaiseEvent ObjectRestored(Me, o.First)
            Next
            For Each p As Pair(Of ObjectState, _ICachedEntity) In saved
                Dim o As ICachedEntity = p.Second
                If Not rejectList.Contains(o) Then
                    RaiseEvent ObjectRejecting(Me, o)
                    Dim orm As _IKeyEntity = TryCast(o, _IKeyEntity)
                    If orm IsNot Nothing Then
                        orm.RejectM2MIntermidiate()
                    End If
                    RaiseEvent ObjectRejected(Me, o, True)
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

    Public Enum OnErrorEnum
        RestoreObjectsStateAndRemoveNewObjects
        RestoreObjectsStateOnly
        DontRestore
    End Enum

    Public Class ModificationsTracker
        Implements IDisposable

        Private disposedValue As Boolean
        Private _disposing As Boolean
        Private _disposeMgr As Boolean
        Private _mgr As OrmManager
        Private _saver As ObjectListSaver
        Private _restore As OnErrorEnum
        Private _cm As ICreateManager
        Protected _created As Boolean
        Private _ss As OrmManager.SchemaSwitcher

        Public Event SaveComplete(ByVal logicalCommited As Boolean, ByVal dbCommit As Boolean)
        Public Event BeginRestore(ByVal count As Integer)

        Public Sub New()
            MyClass.New(CType(OrmManager.CurrentManager, OrmReadOnlyDBManager))
        End Sub

        Public Sub New(ByVal connString As String)
            MyClass.New(New OrmCache, New ObjectMappingEngine("1"), connString)
        End Sub

        Public Sub New(ByVal cache As OrmCache, ByVal connString As String)
            MyClass.New(cache, New ObjectMappingEngine("1"), connString)
        End Sub

        Public Sub New(ByVal cache As OrmCache, ByVal mpe As ObjectMappingEngine, ByVal connString As String)
            MyClass.New(New CreateManager(Function() New OrmDBManager(cache, mpe, New SQLGenerator, connString)))
        End Sub

        Public Sub New(ByVal mgr As OrmReadOnlyDBManager)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            AddHandler mgr.BeginUpdate, AddressOf Add
            AddHandler mgr.BeginDelete, AddressOf Delete
            _mgr = mgr
            _saver = CreateSaver(mgr)
        End Sub

        Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal disposeMgr As Boolean)
            MyClass.New(mgr)
            _disposeMgr = disposeMgr
        End Sub

        Public Sub New(ByVal getMgr As ICreateManager)
            MyClass.New(CType(getMgr.CreateManager, OrmReadOnlyDBManager), True)
            _cm = getMgr
        End Sub

        Public Sub New(ByVal getMgr As ICreateManager, ByVal schema As ObjectMappingEngine)
            MyClass.New(CType(getMgr.CreateManager, OrmReadOnlyDBManager), True)
            _cm = getMgr
            If Not _mgr.MappingEngine.Equals(schema) Then
                _ss = New OrmManager.SchemaSwitcher(schema, _mgr)
            End If
        End Sub

        Protected ReadOnly Property NewObjectManager() As INewObjectsStore
            Get
                Return _mgr.Cache.NewObjectManager
            End Get
        End Property

        Public ReadOnly Property IsCommit() As Boolean
            Get
                Return _saver.IsCommit
            End Get
        End Property

        Public ReadOnly Property CreateManager() As ICreateManager
            Get
                Return _cm
            End Get
        End Property

        Public Sub AcceptModifications()
            _saver.Commit()
        End Sub

        Public Sub OmitModifications()
            _saver.Rollback()
        End Sub

        Public Property OnErrorBehaviour() As OnErrorEnum
            Get
                Return _restore
            End Get
            Set(ByVal value As OnErrorEnum)
                _restore = value
            End Set
        End Property

        Public ReadOnly Property Saver() As ObjectListSaver
            Get
                Return _saver
            End Get
        End Property

        Protected Overridable Function CreateSaver(ByVal mgr As OrmReadOnlyDBManager) As ObjectListSaver
            Return mgr.CreateBatchSaver(Of ObjectListSaver)(_created)
        End Function

        Public Overridable Sub AddRange(ByVal objs As ICollection(Of _ICachedEntity))
            If objs Is Nothing Then
                Throw New ArgumentNullException("objects")
            End If

            If Saver.StartSaving Then
                Throw New InvalidOperationException("Cannot add object during save")
            End If

            '_objs.AddRange(objs)
            _saver.AddRange(objs)
        End Sub

        Private Sub Add(ByVal sender As OrmManager, ByVal obj As ICachedEntity)
            Add(CType(obj, _ICachedEntity))
        End Sub

        Public Overridable Sub Add(ByVal obj As Object)
            If obj Is Nothing Then
                Throw New ArgumentNullException("object")
            End If

            Dim t As Type = obj.GetType
            Dim mpe As Worm.ObjectMappingEngine = _mgr.MappingEngine
            Dim oschema As IEntitySchema = mpe.GetEntitySchema(t, False)
            If oschema Is Nothing Then
                oschema = ObjectMappingEngine.GetEntitySchema(t, mpe, Nothing, Nothing)
                mpe.AddEntitySchema(t, oschema)
            End If
            Add(CType(_mgr.Cache.SyncPOCO(mpe, oschema, obj), _ICachedEntity))
        End Sub

        Public Overridable Sub Add(ByVal obj As _ICachedEntity)
            If obj Is Nothing Then
                Throw New ArgumentNullException("object")
            End If

            If Saver.StartSaving Then
                Throw New InvalidOperationException("Cannot add object during save")
            End If

            _saver.Add(obj)
        End Sub

        Private Sub Delete(ByVal sender As OrmManager, ByVal obj As ICachedEntity)
            If obj Is Nothing Then
                Throw New ArgumentNullException("object")
            End If

            If Saver.StartSaving Then
                Throw New InvalidOperationException("Cannot add object during save")
            End If

            'If Not _objs.Contains(obj) Then
            '    _objs.Add(obj)
            _saver.Add(CType(obj, _ICachedEntity))
            'End If
        End Sub

        Public Function CreateNewObject(Of T As {IKeyEntity, New})() As T
            If NewObjectManager Is Nothing Then
                Throw New InvalidOperationException("NewObjectManager is not set")
            End If

            Return CreateNewObject(Of T)(NewObjectManager.GetPKForNewObject(GetType(T), _mgr.MappingEngine)(0).Value)
        End Function

        Public Function CreateNewEntity(Of T As {_ICachedEntity, New})() As T
            If NewObjectManager Is Nothing Then
                Throw New InvalidOperationException("NewObjectManager is not set")
            End If

            Dim pk() As PKDesc = NewObjectManager.GetPKForNewObject(GetType(T), _mgr.MappingEngine)
            Return CreateNewObject(Of T)(pk)
        End Function

        Public Overridable Function CreateNewObject(Of T As {_ICachedEntity, New})(ByVal pk() As PKDesc) As T
            If NewObjectManager Is Nothing Then
                Throw New InvalidOperationException("NewObjectManager is not set")
            End If
            Dim o As T = _mgr.CreateEntity(Of T)(pk)
            o.SetCreateManager(_cm)
            NewObjectManager.AddNew(o)
            '_objs.Add(o)
            '_saver.Add(o)
            Add(o)
            Return o
        End Function

        Public Overridable Function CreateNewObject(Of T As {IKeyEntity, New})(ByVal id As Object) As T
            If NewObjectManager Is Nothing Then
                Throw New InvalidOperationException("NewObjectManager is not set")
            End If
            Dim o As T = _mgr.CreateKeyEntity(Of T)(id)
            o.SetCreateManager(_cm)
            NewObjectManager.AddNew(o)
            '_objs.Add(o)
            '_saver.Add(o)
            Add(o)
            Return o
        End Function

        Public Function CreateNewObject(ByVal t As Type) As IKeyEntity
            If NewObjectManager Is Nothing Then
                Throw New InvalidOperationException("NewObjectManager is not set")
            End If

            Return CreateNewObject(t, NewObjectManager.GetPKForNewObject(t, _mgr.MappingEngine))
        End Function

        Public Function CreateNewObject(ByVal t As Type, ByVal id As Object) As IKeyEntity
            For Each mi As Reflection.MethodInfo In Me.GetType.GetMember("CreateNewObject", Reflection.MemberTypes.Method, _
                Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public)
                If mi.IsGenericMethod AndAlso mi.GetParameters.Length = 1 Then
                    mi = mi.MakeGenericMethod(New Type() {t})
                    Return CType(mi.Invoke(Me, New Object() {id}), IKeyEntity)
                End If
            Next
            Throw New InvalidOperationException("Cannot find method CreateNewObject")
        End Function

        Protected Sub _Rollback()
            If _restore <> OnErrorEnum.DontRestore Then
                RaiseEvent BeginRestore(_saver.AffectedObjects.Count)
                'Debug.WriteLine("_rollback: " & _saver.AffectedObjects.Count)
                For Each o As _ICachedEntity In _saver.AffectedObjects
                    'Debug.WriteLine("_rollback: " & o.ObjName)
                    If o.ObjectState = ObjectState.Created Then
                        If _restore = OnErrorEnum.RestoreObjectsStateAndRemoveNewObjects AndAlso NewObjectManager IsNot Nothing Then
                            NewObjectManager.RemoveNew(o)
                        End If
                    Else
#If DEBUG Then
                        If o.HasChanges Then
                            'Debug.Assert(_mgr.Cache.Modified(o) IsNot Nothing)
                            If o.ShadowCopy(_mgr) IsNot Nothing Then
                                If _mgr.Cache.ShadowCopy(o, _mgr).Reason = ObjectModification.ReasonEnum.Delete Then
                                    Debug.Assert(_saver._deleted.Contains(o))
                                    Debug.Assert(Not _saver._updated.Contains(o))
                                ElseIf o.ShadowCopy(_mgr).Reason = ObjectModification.ReasonEnum.Edit Then
                                    'If _mgr.Cache.Modified(o).Reason = ModifiedObject.ReasonEnum.Delete Then
                                    Debug.Assert(Not _saver._deleted.Contains(o))
                                    Debug.Assert(_saver._updated.Contains(o))
                                    'End If
                                End If
                            End If
                        End If
#End If
                        o.RejectChanges(_mgr)
                    End If
                Next
            End If
        End Sub

#Region " IDisposable Support "
        ' IDisposable
        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not Me.disposedValue Then
                If _created Then
                    Dim rlb As Boolean = True
                    Try
                        _disposing = True
                        _saver.Dispose()
                        'rlb = False
                    Finally
                        _disposing = False

                        RemoveHandler _mgr.BeginDelete, AddressOf Delete
                        RemoveHandler _mgr.BeginUpdate, AddressOf Add

                        If _saver.Error Then
                            _Rollback()
                        End If

                        Me.disposedValue = True
                    End Try
                    If Not rlb AndAlso Not _saver.IsCommit Then _Rollback()

                    RaiseEvent SaveComplete(_saver.IsCommit, _saver.Commited)
                End If

                If _ss IsNot Nothing Then
                    _ss.Dispose()
                End If

                If _disposeMgr AndAlso _mgr IsNot Nothing Then
                    _mgr.Dispose()
                    _mgr = Nothing
                End If
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

End Namespace