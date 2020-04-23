Imports Worm.Entities
Imports System.Collections.Generic
Imports Worm.Cache
Imports Worm.Entities.Meta

Namespace Database
    Public Class ModificationsTracker
        Implements IDisposable

        Private disposedValue As Boolean
        Private _disposing As Boolean
        Private ReadOnly _disposeMgr As Boolean
        Private _mgr As OrmManager
        Private ReadOnly _saver As ObjectListSaver
        Private _restore As OnErrorEnum
        Private ReadOnly _cm As ICreateManager
        Protected _created As Boolean
        Private ReadOnly _ss As OrmManager.SchemaSwitcher
        Private ReadOnly _syncObj As New Dictionary(Of ICachedEntity, Object)
        Private ReadOnly _intermediate As New List(Of _ICachedEntity)
        Private _inSavepoint As Boolean

        Public Event SaveComplete(ByVal logicalCommited As Boolean, ByVal dbCommit As Boolean)
        Public Event BeginRestore(ByVal count As Integer)

        Public Sub New()
            MyClass.New(CType(OrmManager.CurrentManager, OrmReadOnlyDBManager), False, False)
        End Sub

        Public Sub New(ByVal connString As String)
            MyClass.New(connString, New OrmCache, New ObjectMappingEngine("1"))
        End Sub

        Public Sub New(ByVal connString As String, ByVal cache As OrmCache)
            MyClass.New(connString, cache, New ObjectMappingEngine("1"))
        End Sub

        Public Sub New(ByVal connString As String, ByVal cache As OrmCache, ByVal mpe As ObjectMappingEngine)
            MyClass.New(New CreateManager(Function() New OrmDBManager(connString, mpe, New SQL2000Generator, cache)))
        End Sub

        Public Sub New(ByVal connString As String, ByVal cache As OrmCache, ByVal mpe As ObjectMappingEngine, stmt As SQL2000Generator)
            MyClass.New(New CreateManager(Function() New OrmDBManager(connString, mpe, stmt, cache)))
        End Sub

        Public Sub New(ByVal mgr As OrmReadOnlyDBManager, Optional ByVal disposeMgr As Boolean = False, Optional newSaver As Boolean = False)
            If mgr Is Nothing Then
                Throw New ArgumentNullException(NameOf(mgr))
            End If

            AddHandler mgr.BeginUpdate, AddressOf Add
            AddHandler mgr.BeginDelete, AddressOf Delete
            _mgr = mgr
            If newSaver Then
                _saver = New ObjectListSaver(mgr)
                _created = True
            Else
                _saver = CreateSaver(mgr)
            End If
            _disposeMgr = disposeMgr
        End Sub

        Public Sub New(ByVal getMgr As ICreateManager)
            MyClass.New(CType(getMgr.CreateManager(Nothing), OrmReadOnlyDBManager), True)
            _cm = getMgr
        End Sub

        Public Sub New(ByVal getMgr As CreateManagerDelegate)
            MyClass.New(CType(getMgr(), OrmReadOnlyDBManager), True)
            _cm = New CreateManager(getMgr)
        End Sub

        Public Sub New(ByVal getMgr As ICreateManager, ByVal schema As ObjectMappingEngine)
            MyClass.New(CType(getMgr.CreateManager(Nothing), OrmReadOnlyDBManager), True)
            _cm = getMgr
            If Not _mgr.MappingEngine.Equals(schema) Then
                _ss = New OrmManager.SchemaSwitcher(schema, _mgr)
            End If
        End Sub

        Public Property TrackChanges() As Boolean
            Get
                Return _mgr.ContainsBeginDelete(AddressOf Delete)
            End Get
            Set(ByVal value As Boolean)
                If value Then
                    AddHandler _mgr.BeginUpdate, AddressOf Add
                    AddHandler _mgr.BeginDelete, AddressOf Delete
                Else
                    RemoveHandler _mgr.BeginUpdate, AddressOf Add
                    RemoveHandler _mgr.BeginDelete, AddressOf Delete
                End If
            End Set
        End Property

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

        Public Sub AcceptModifications(Optional acceptOuterTransaction As Boolean = False)
            _saver.Accept()
            _saver.AcceptOuterTransaction = acceptOuterTransaction
        End Sub

        Public Sub OmitModifications()
            _saver.Refuse()
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

        Public ReadOnly Property IsInSavepoint As Boolean
            Get
                Return _inSavepoint
            End Get
        End Property
        Public Function Savepoint(name As String) As Boolean
            Dim mgr = TryCast(_mgr, OrmReadOnlyDBManager)

            If mgr.SQLGenerator.IsSavepointsSupported AndAlso Not _inSavepoint Then
                mgr.Savepoint(mgr.Transaction, name)
                _inSavepoint = True
                Return True
            End If

            Return False
        End Function
        Public Sub RollbackSavepoint(name As String)
            Dim mgr = TryCast(_mgr, OrmReadOnlyDBManager)

            If mgr.SQLGenerator.IsSavepointsSupported AndAlso _inSavepoint Then
                mgr.RollbackSavepoint(mgr.Transaction, name)

                For Each obj In _intermediate
                    Remove(obj)
                Next

                _intermediate.RemoveAll(Function(it) True)

                _inSavepoint = False
            End If

        End Sub
        Public Sub CreateDependency(master As ICachedEntity, slave As ICachedEntity,
                                    Optional del As ObjectListSaver.OnSavedDependencyDelegate = Nothing,
                                    Optional reorderOnSave As Boolean = True)
            _saver.CreateDependency(master, slave, del, reorderOnSave)
        End Sub
        Public Sub CreateDependency(master As ICachedEntity, slave As ICachedEntity)
            CreateDependency(master, slave, Nothing, True)
        End Sub
        Protected Overridable Function CreateSaver(ByVal mgr As OrmReadOnlyDBManager) As ObjectListSaver
            Return mgr.CreateBatchSaver(Of ObjectListSaver)(_created)
        End Function

        Public Overridable Sub AddRange(ByVal objs As IEnumerable(Of _ICachedEntity))
            If objs Is Nothing Then
                Throw New ArgumentNullException(NameOf(objs))
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

        Private Sub ChangesAccepted(ByVal sender As ICachedEntity, ByVal args As EventArgs)
            Dim uc As IUndoChanges = TryCast(sender, IUndoChanges)
            If uc IsNot Nothing Then
                RemoveHandler uc.ChangesAccepted, AddressOf ChangesAccepted
            End If

            Dim o As Object = _syncObj(sender)
            _syncObj.Remove(sender)
            Dim oschema As IEntitySchema = _mgr.MappingEngine.GetEntitySchema(o.GetType)
            ObjectMappingEngine.InitPOCO(o.GetType, oschema, CType(sender, ComponentModel.ICustomTypeDescriptor),
                                         _mgr.MappingEngine, sender, o, _mgr.Cache, _mgr.ContextInfo, crMan:=_mgr.GetCreateManager)
        End Sub
        Public Overridable Sub Delete(ByVal obj As _ICachedEntity)
            If obj Is Nothing Then
                Throw New ArgumentNullException(NameOf(obj))
            End If

            obj.Delete(_mgr)
        End Sub
        Public Overridable Sub Delete(ByVal obj As Object)
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim t As Type = obj.GetType
            Dim mpe As Worm.ObjectMappingEngine = _mgr.MappingEngine
            Dim oschema As IEntitySchema = mpe.GetEntitySchema(t, False)
            If oschema Is Nothing Then
                oschema = ObjectMappingEngine.GetEntitySchema(t, mpe, Nothing, Nothing)
                mpe.AddEntitySchema(t, oschema)
            End If
            Dim ro As _ICachedEntity = _mgr.Cache.GetPOCO(mpe, oschema, obj, _mgr)
            Dim acc As Boolean = ro.ObjectState <> ObjectState.Modified
            _mgr.Cache.SyncPOCO(ro, mpe, oschema, obj)
            'Dim ro As _ICachedEntity = CType(_mgr.Cache.SyncPOCO(mpe, oschema, obj, _mgr), _ICachedEntity)
            _mgr.AcceptChanges(ro, False, True)
            ro.Delete(_mgr)
            Add(ro)
        End Sub

        Public Overridable Sub Add(ByVal obj As Object)
            If obj Is Nothing Then
                Throw New ArgumentNullException("object")
            End If

            Dim ro As _ICachedEntity = TryCast(obj, _ICachedEntity)
            If ro Is Nothing Then
                Dim t As Type = obj.GetType
                Dim mpe As Worm.ObjectMappingEngine = _mgr.MappingEngine
                Dim oschema As IEntitySchema = mpe.GetEntitySchema(t, False)
                If oschema Is Nothing Then
                    oschema = ObjectMappingEngine.GetEntitySchema(t, mpe, Nothing, Nothing)
                    mpe.AddEntitySchema(t, oschema)
                End If
                ro = CType(_mgr.Cache.SyncPOCO(mpe, oschema, obj, _mgr), _ICachedEntity)
                _syncObj(ro) = obj
                AddHandler ro.ChangesAccepted, AddressOf ChangesAccepted
            End If

            Add(ro)
        End Sub

        Public Overridable Sub Add(ByVal obj As _ICachedEntity)
            If obj Is Nothing Then
                Throw New ArgumentNullException("object")
            End If

            If Not _saver._dontCheckOnAdd Then
                If Saver.StartSaving Then
                    Throw New InvalidOperationException("Cannot add object during save")
                End If

                _saver.Add(obj)

                If IsInSavepoint Then
                    _intermediate.Add(obj)
                End If
            End If
        End Sub

        Public Overridable Sub Remove(ByVal obj As _ICachedEntity)
            If obj Is Nothing Then
                Throw New ArgumentNullException(NameOf(obj))
            End If

            If Not _saver._dontCheckOnAdd Then
                If Saver.StartSaving Then
                    Throw New InvalidOperationException("Cannot remove object during save")
                End If

                _saver.Remove(obj)
            End If
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
#Region " Clone entity "
        Public Function CloneKeyEntity(Of T As {ISinglePKEntity, New})(entity As T) As T
            If NewObjectManager Is Nothing Then
                Throw New InvalidOperationException("NewObjectManager is not set")
            End If

            Return CloneNewObject(Of T)(NewObjectManager.GetPKForNewObject(GetType(T), _mgr.MappingEngine), entity)
        End Function

        Public Function CloneNewEntity(Of T As {_ICachedEntity, New})(entity As T) As T
            If NewObjectManager Is Nothing Then
                Throw New InvalidOperationException("NewObjectManager is not set")
            End If

            Dim pk() As PKDesc = NewObjectManager.GetPKForNewObject(GetType(T), _mgr.MappingEngine)
            Return CloneNewObject(Of T)(pk, entity)
        End Function

        Public Function CloneNewObject(entity As _ICachedEntity) As _ICachedEntity
            If NewObjectManager Is Nothing Then
                Throw New InvalidOperationException("NewObjectManager is not set")
            End If

            If entity Is Nothing Then
                Throw New ArgumentNullException(NameOf(entity))
            End If

            Dim pk() As PKDesc = NewObjectManager.GetPKForNewObject(entity.GetType, _mgr.MappingEngine)

            Dim o = entity.Clone(pk, Nothing)
            NewObjectManager.AddNew(o)
            Add(o)
            Return o
        End Function
        Public Function CloneNewObject(Of T As {_ICachedEntity, New})(pk() As PKDesc, entity As T) As T
            Dim o = entity.Clone(pk, Nothing)
            NewObjectManager.AddNew(o)
            Add(o)
            Return o
        End Function
#End Region
#Region " Create entity "
        Public Function CreateNewKeyEntity(Of T As {ISinglePKEntity, New})() As T
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

        Public Overridable Function CreateNewObject(Of T As {ISinglePKEntity, New})(ByVal id As Object) As T
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

        Public Function CreateNewObject(ByVal t As Type) As _ICachedEntity
            If NewObjectManager Is Nothing Then
                Throw New InvalidOperationException("NewObjectManager is not set")
            End If

            Return CreateNewObject(t, NewObjectManager.GetPKForNewObject(t, _mgr.MappingEngine))
        End Function

        Public Function CreateNewObject(ByVal t As Type, ByVal id As Object) As _ICachedEntity
            For Each mi As Reflection.MethodInfo In Me.GetType.GetMember("CreateNewObject", Reflection.MemberTypes.Method,
                Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public)
                If mi.IsGenericMethod AndAlso mi.GetParameters.Length = 1 Then
                    mi = mi.MakeGenericMethod(New Type() {t})
                    Return CType(mi.Invoke(Me, New Object() {id}), _ICachedEntity)
                End If
            Next
            Throw New InvalidOperationException("Cannot find method CreateNewObject")
        End Function
#End Region

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
                        Dim uc As IUndoChanges = TryCast(o, IUndoChanges)
                        If uc IsNot Nothing AndAlso uc.HasChanges() Then
                            'Debug.Assert(_mgr.Cache.Modified(o) IsNot Nothing)
                            'Dim sc As ObjectModification = _mgr.Cache.ShadowCopy(o.GetType, o, TryCast(o.GetEntitySchema(_mgr.MappingEngine), ICacheBehavior))
                            'If sc IsNot Nothing Then
                            '    If sc.Reason = ObjectModification.ReasonEnum.Delete Then
                            '        Debug.Assert(_saver._deleted.Contains(o))
                            '        Debug.Assert(Not _saver._updated.Contains(o))
                            '    ElseIf sc.Reason = ObjectModification.ReasonEnum.Edit Then
                            '        'If _mgr.Cache.Modified(o).Reason = ModifiedObject.ReasonEnum.Delete Then
                            '        Debug.Assert(Not _saver._deleted.Contains(o))
                            '        Debug.Assert(_saver._updated.Contains(o) OrElse (GetType(IRelations).IsAssignableFrom(o.GetType) AndAlso CType(o, IRelations).HasChanges))
                            '        'End If
                            '    End If
                            'End If
                        End If
#End If
                        _mgr.RejectChanges(o)
                    End If
                Next
            End If
        End Sub

        Public Sub SaveChanges()
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

                    If _saver.Error.HasValue AndAlso _saver.Error Then
                        _Rollback()
                    End If

                End Try
                If Not rlb AndAlso Not _saver.IsCommit Then _Rollback()

                RaiseEvent SaveComplete(_saver.IsCommit, _saver.Commited)
            End If
        End Sub

#Region " IDisposable Support "
        ' IDisposable
        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not Me.disposedValue Then

                Try
                    SaveChanges()
                Finally
                    Me.disposedValue = True

                    If _ss IsNot Nothing Then
                        _ss.Dispose()
                    End If

                    If _disposeMgr AndAlso _mgr IsNot Nothing Then
                        _mgr.Dispose()
                        _mgr = Nothing
                    End If
                End Try
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