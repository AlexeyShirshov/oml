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
        Private _error As Boolean?
        Private _graphDepth As Integer = 4
        Private _dontResolve As Boolean
        Private _mode As Data.IsolationLevel?

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

            Friend _new2Save As New List(Of Pair(Of ICachedEntity, Action(Of ICachedEntity)))

            Public Sub AddEntityToSaveGraph(e As ICachedEntity, modifier As Action(Of ICachedEntity))
                If e Is Nothing Then
                    Throw New ArgumentNullException("e")
                End If
                If Not _new2Save.Exists(Function(item) item.First.Equals(e)) Then
                    _new2Save.Add(New Pair(Of ICachedEntity, Action(Of ICachedEntity))(e, modifier))
                End If
            End Sub

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

        Friend _dontCheckOnAdd As Boolean

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
        Public Event Commiting(ByVal sender As ObjectListSaver, commit As Boolean)
        Public Event SaveSuccessed(ByVal sender As ObjectListSaver)
        Public Event SaveFailed(ByVal sender As ObjectListSaver)
        Public Event SaveCompleted(ByVal sender As ObjectListSaver)
        Public Event BeginRejecting(ByVal sender As ObjectListSaver)
        Public Event BeginAccepting(ByVal sender As ObjectListSaver)
        Public Event OnAdded(ByVal sender As ObjectListSaver, ByVal o As ICachedEntity, ByVal added As Boolean)
        Public Event OnRemoved(ByVal sender As ObjectListSaver, ByVal o As ICachedEntity)

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

        Public ReadOnly Property [Error]() As Boolean?
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

        Property AcceptOuterTransaction As Boolean

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

        Public Property IsolationMode As Data.IsolationLevel?
            Get
                Return _mode
            End Get
            Set(value As Data.IsolationLevel?)
                _mode = value
            End Set
        End Property

        Public Overridable Function Add(ByVal o As _ICachedEntity) As Boolean
            Dim added As Boolean
            If Not _objects.Contains(o) Then
                added = True
                _objects.Add(o)
                Dim uc As IUndoChanges = TryCast(o, IUndoChanges)
                If uc IsNot Nothing Then
                    AddHandler uc.OriginalCopyRemoved, AddressOf ObjRejected
#If DEBUG Then
                    If OrmManager.HasBodyChanges(uc) Then
                        'Dim mo As ObjectModification = _mgr.Cache.ShadowCopy(o.GetType, o)
                        ''Dim oc As ICachedEntity = o.OriginalCopy
                        'If o.ObjectState = ObjectState.Deleted Then
                        '    _deleted.Add(o)
                        '    Debug.Assert(o IsNot Nothing)
                        '    If mo IsNot Nothing Then
                        '        Debug.Assert(mo.Reason = ObjectModification.ReasonEnum.Delete OrElse mo.Reason = ObjectModification.ReasonEnum.Edit)
                        '    End If
                        'ElseIf o.ObjectState = ObjectState.Modified Then
                        '    _updated.Add(o)
                        '    Debug.Assert(o IsNot Nothing)
                        '    If mo IsNot Nothing Then
                        '        Debug.Assert(mo.Reason = ObjectModification.ReasonEnum.Edit)
                        '    End If
                        'End If
                    End If
#End If
                End If
            End If
            RaiseEvent OnAdded(Me, o, added)
            Return added
        End Function

        Public Sub AddRange(ByVal col As IEnumerable(Of _ICachedEntity))
            '_objects.AddRange(col)
            'For Each o As OrmBase In col
            '    AddHandler o.OriginalCopyRemoved, AddressOf ObjRejected
            'Next
            For Each o As _ICachedEntity In col
                Add(o)
            Next
        End Sub

        Public Sub Remove(o As _ICachedEntity)
            _objects.Remove(o)
            Dim uc As IUndoChanges = TryCast(o, IUndoChanges)
            If uc IsNot Nothing Then
                RemoveHandler uc.OriginalCopyRemoved, AddressOf ObjRejected
            End If
            RaiseEvent OnRemoved(Me, o)
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
            'Throw New InvalidOperationException("Cannot find object")
            Return Nothing
        End Function

        Protected Sub ObjectAcceptedHandler(ByVal sender As ObjectListSaver, ByVal obj As ICachedEntity) Handles Me.ObjectAccepted
            Dim o As ObjectWrap(Of ICachedEntity) = GetObjWrap(obj)
            If o IsNot Nothing Then
                _lockList(o).Dispose()
                _lockList.Remove(o)
            End If
        End Sub

        Protected Sub ObjectRejectedHandler(ByVal sender As ObjectListSaver, ByVal obj As ICachedEntity, ByVal inLockList As Boolean) Handles Me.ObjectRejected
            If inLockList Then
                Dim o As ObjectWrap(Of ICachedEntity) = GetObjWrap(obj)
                If o IsNot Nothing Then
                    _lockList(o).Dispose()
                    _lockList.Remove(o)
                End If
            End If
        End Sub

        Protected Overridable Function CheckObj(ByVal main As ICachedEntity, ByVal o As ICachedEntity) As Boolean
            If o.ObjectState <> ObjectState.Deleted Then Return False
            Dim oSchema As IEntitySchema = _mgr.MappingEngine.GetEntitySchema(o.GetType)
            For Each m As MapField2Column In oSchema.FieldColumnMap
                If main.Equals(ObjectMappingEngine.GetPropertyValue(o, m.PropertyAlias, oSchema)) Then
                    Return True
                End If
            Next
            Return False
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
                Dim adv As IAdvSave = TryCast(o, IAdvSave)
                If adv IsNot Nothing Then
                    adv.Saving(Me)
                End If
                If Not args.Cancel Then
                    If Not CanSaveObj(o, col2save) Then
                        RaiseEvent ObjectPostponed(Me, o)
                        need2save.Add(o)
                        Return False
                    Else

                        For Each ns As Pair(Of ICachedEntity, Action(Of ICachedEntity)) In args._new2Save
                            need2save.Add(ns.First)
                            If ns.Second IsNot Nothing Then
                                _dontCheckOnAdd = True
                                Try
                                    ns.Second()(ns.First)
                                Finally
                                    _dontCheckOnAdd = False
                                End Try
                            End If
                        Next

                        owr = New ObjectWrap(Of ICachedEntity)(o)
                        _lockList.Add(owr, _mgr.GetSyncForSave(o.GetType, o)) 'o.GetSyncRoot)
                        Dim os As ObjectState = o.ObjectState
                        Dim oldME As ObjectMappingEngine = Nothing
                        Dim blb As Boolean
                        Try
                            If Not _mgr.MappingEngine.Equals(o.GetMappingEngine) Then
                                oldME = _mgr._schema
                                _mgr._schema = o.GetMappingEngine
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
                    End If
                End If
                Return args.Cancel
            Catch ex As Exception
                If owr IsNot Nothing Then
                    Dim dsp As IDisposable = Nothing
                    If _lockList.TryGetValue(owr, dsp) Then
                        Dim args As New SaveErrorEventArgs(o, ex)
                        RaiseEvent ObjectSavingError(Me, args)
                        Select Case args.FurtherAction
                            Case FurtherActionEnum.Retry
                                dsp.Dispose()
                                _lockList.Remove(owr)
                                GoTo l1
                            Case FurtherActionEnum.Skip
                                dsp.Dispose()
                                _lockList.Remove(owr)
                                Return True
                            Case FurtherActionEnum.RetryLater
                                dsp.Dispose()
                                _lockList.Remove(owr)
                                RaiseEvent ObjectPostponed(Me, o)
                                need2save.Add(o)
                                Return False
                        End Select
                    End If
                End If
                Throw New OrmManagerException("Error during save " & o.ObjName, ex)
            End Try
        End Function

        Protected Sub Save()
            If _mgr.Cache.IsReadonly Then
                Throw New InvalidOperationException("Cache is readonly")
            End If
            _error = True

            RaiseEvent PreSave(Me)
            Dim hasTransaction As Boolean = _mgr.Transaction IsNot Nothing AndAlso Not AcceptOuterTransaction
            Dim saved As New List(Of Pair(Of ObjectState, _ICachedEntity)), copies As New List(Of Pair(Of ICachedEntity))
            Dim rejectList As New List(Of ICachedEntity), need2save As New List(Of ICachedEntity)
            _startSave = True
#If DebugLocks Then
                Using New CSScopeMgr_Debug(_mgr.Cache, "d:\temp\")
#Else
            'Using _mgr.Cache.SyncSave
#End If
            If _mode.HasValue Then
                _mgr.BeginTransaction(_mode.Value)
            Else
                _mgr.BeginTransaction()
            End If

            Try
                RaiseEvent BeginSave(Me, _objects.Count)
                For Each o As _ICachedEntity In _objects
                    Dim uc As IUndoChanges = TryCast(o, IUndoChanges)
                    If uc IsNot Nothing Then
                        RemoveHandler uc.OriginalCopyRemoved, AddressOf ObjRejected
                    End If

                    Dim pp As Pair(Of ICachedEntity) = Nothing
                    If o.ObjectState = ObjectState.Created Then
                        rejectList.Add(o)
                    ElseIf o.ObjectState = ObjectState.Modified Then
                        pp = New Pair(Of ICachedEntity)(o, CType(_mgr.MappingEngine.CloneFullEntity(o, o.GetEntitySchema(_mgr.MappingEngine)), ICachedEntity))
                        pp.Second.SetObjectState(o.ObjectState)
                        copies.Add(pp)
                    ElseIf (o.ObjectState = ObjectState.None OrElse o.ObjectState = ObjectState.NotLoaded) Then
                        saved.Add(New Pair(Of ObjectState, _ICachedEntity)(o.ObjectState, o))
                        Continue For
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
                            RaiseEvent Commiting(Me, False)
                            _mgr.Rollback()
                        End If

                        RaiseEvent BeginRejecting(Me)
                        Rollback(saved, rejectList, copies, need2save)
                        RaiseEvent SaveFailed(Me)
                    Else
                        If Not hasTransaction Then
                            RaiseEvent Commiting(Me, True)
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
                                Dim mo As ICachedEntity = _mgr.AcceptChanges(o, False, SinglePKEntity.IsGoodState(p.First))
                                'Debug.Assert(_mgr.Cache.ShadowCopy(o.GetType, o, TryCast(o.GetEntitySchema(_mgr.MappingEngine), ICacheBehavior)) Is Nothing)
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
                                    AddressOf OrmManager.ClearCacheFlags, Nothing, _callbacks, False, False)
                            Next
                            For Each o As _ICachedEntity In val
                                o.UpdateCacheAfterUpdate(CType(_mgr.Cache, OrmCache))
                            Next
                        Else
                            Dim svd As New List(Of Pair(Of _ICachedEntity))
                            For Each p As Pair(Of ObjectState, _ICachedEntity) In saved
                                Dim o As _ICachedEntity = p.Second
                                RaiseEvent ObjectAccepting(Me, o)
                                Dim mo As ICachedEntity = _mgr.AcceptChanges(o, False, SinglePKEntity.IsGoodState(p.First))
                                'Debug.Assert(_mgr.Cache.ShadowCopy(o.GetType, o) Is Nothing)
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
                            Exit For
                        Next
                        _lockList.Remove(o)
                    Loop

                    RaiseEvent SaveCompleted(Me)
                End Try
            End Try
            'End Using
        End Sub

        Private Sub Rollback(ByVal saved As List(Of Pair(Of ObjectState, _ICachedEntity)), _
            ByVal rejectList As List(Of ICachedEntity), ByVal copies As List(Of Pair(Of ICachedEntity)), ByVal need2save As List(Of ICachedEntity))
            For Each o As _ICachedEntity In rejectList
                RaiseEvent ObjectRejecting(Me, o)
                _dontTrackRemoved = True
                _mgr.RejectChanges(o)
                _dontTrackRemoved = False
                RaiseEvent ObjectRejected(Me, o, Not need2save.Contains(o))
            Next
            For Each o As Pair(Of ICachedEntity) In copies
                OrmManager.CopyBody(o.Second, o.First, Nothing)
                o.First.SetObjectState(o.Second.ObjectState)
                Dim orm As _ISinglePKEntity = TryCast(o.First, _ISinglePKEntity)
                If orm IsNot Nothing Then
                    orm.RejectM2MIntermidiate()
                End If
                RaiseEvent ObjectRestored(Me, o.First)
            Next
            For Each p As Pair(Of ObjectState, _ICachedEntity) In saved
                Dim o As ICachedEntity = p.Second
                If Not rejectList.Contains(o) Then
                    RaiseEvent ObjectRejecting(Me, o)
                    Dim orm As _ISinglePKEntity = TryCast(o, _ISinglePKEntity)
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

    Public Interface IAdvSave
        Sub Saving(saver As ObjectListSaver)
    End Interface

End Namespace