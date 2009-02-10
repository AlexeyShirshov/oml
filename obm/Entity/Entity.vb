Imports Worm.Entities.Meta
Imports Worm.Cache
Imports Worm.Query

Namespace Entities

    <Serializable()> _
    Public Class Entity
        Implements _IEntity

        Private Class ChangedEventHelper
            Implements IDisposable

            Private _value As Object
            Private _fieldName As String
            Private _obj As Entity
            Private _d As IDisposable

            Public Sub New(ByVal obj As Entity, ByVal propertyAlias As String, ByVal d As IDisposable)
                _fieldName = propertyAlias
                _obj = obj
                _value = obj.GetValue(propertyAlias)
                _d = d
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
                _d.Dispose()
                _obj.RaisePropertyChanged(_fieldName, _value)
            End Sub
        End Class

        Private _state As ObjectState = ObjectState.Created

        <NonSerialized()> _
        Private _loading As Boolean
        <NonSerialized()> _
        Private _mgrStr As String
        <NonSerialized()> _
        Protected _dontRaisePropertyChange As Boolean
        <NonSerialized()> _
        Private _old_state As ObjectState
        <NonSerialized()> _
        Protected _readRaw As Boolean
        <NonSerialized()> _
        Private _cm As ICreateManager
        <NonSerialized()> _
        Private _schema As ObjectMappingEngine

        Public Event ManagerRequired(ByVal sender As IEntity, ByVal args As ManagerRequiredArgs) Implements IEntity.ManagerRequired
        Public Event PropertyChanged(ByVal sender As IEntity, ByVal args As PropertyChangedEventArgs) Implements IEntity.PropertyChanged

#If DEBUG Then
        Protected Event ObjectStateChanged(ByVal oldState As ObjectState)
#End If

        Protected ReadOnly Property IsLoading() As Boolean Implements _IEntity.IsLoading
            Get
                Return _loading
            End Get
        End Property

        Protected Sub BeginLoading() Implements _IEntity.BeginLoading
            _loading = True
        End Sub

        Protected Overridable Function SyncHelper(ByVal reader As Boolean) As IDisposable
#If DebugLocks Then
            Return New CSScopeMgr_Debug(Me, "d:\temp\")
#Else
            Return New CSScopeMgr(Me)
#End If
        End Function

        Protected Sub RaisePropertyChanged(ByVal propertyAlias As String, ByVal oldValue As Object)
            Dim value As Object = GetValue(propertyAlias)
            If Not Object.Equals(value, oldValue) Then
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyAlias, oldValue, value))
            End If
        End Sub

        Protected Overridable Sub PrepareRead(ByVal propertyAlias As String, ByRef d As IDisposable)
        End Sub

        Protected Sub PrepareUpdate()
            If Not _loading Then 'AndAlso ObjectState <> Orm.ObjectState.Deleted Then
                If _state = Entities.ObjectState.Clone Then
                    Throw New OrmObjectException(ObjName & ": Altering clone is not allowed")
                End If

                If _state = Entities.ObjectState.Deleted Then
                    Throw New OrmObjectException(ObjName & ": Altering deleted object is not allowed")
                End If

                Using mc As IGetManager = GetMgr()
                    If mc Is Nothing Then
                        Return
                    End If

                    _PrepareUpdate(mc.Manager)
                End Using
                'ElseIf ObjectState = Orm.ObjectState.Created Then
                '    _PrepareLoadingUpdate()
            End If
        End Sub

        Protected Overridable Sub _PrepareUpdate(ByVal mgr As OrmManager)

        End Sub

        Protected Function SyncHelper(ByVal reader As Boolean, ByVal propertyAlias As String, ByVal checkEntity As Boolean) As IDisposable
            If checkEntity Then
                Using mc As IGetManager = GetMgr()
                    If mc IsNot Nothing Then
                        Dim mpe As ObjectMappingEngine = mc.Manager.MappingEngine
                        Dim schema As IEntitySchema = mpe.GetEntitySchema(Me.GetType)
                        Dim o As ICachedEntity = TryCast(mpe.GetPropertyValue(Me, propertyAlias, schema), ICachedEntity)
                        If o IsNot Nothing AndAlso Not mc.Manager.IsInCachePrecise(o) Then
                            Dim ov As IOptimizedValues = TryCast(Me, IOptimizedValues)
                            If ov IsNot Nothing Then
                                ov.SetValueOptimized(propertyAlias, schema, mc.Manager.GetEntityFromCacheOrCreate(o.GetPKValues, o.GetType))
                            Else
                                Throw New OrmObjectException("Check read requires IOptimizedValues")
                            End If
                        End If
                    End If
                End Using
            End If
            Return SyncHelper(reader, propertyAlias)
        End Function

        Protected Function SyncHelper(ByVal reader As Boolean, ByVal propertyAlias As String) As IDisposable Implements _IEntity.SyncHelper
            Dim err As Boolean = True
            Dim d As IDisposable = New BlankSyncHelper(Nothing)
            Try
                If reader Then
                    PrepareRead(propertyAlias, d)
                Else
                    d = SyncHelper(True)
                    PrepareUpdate()
                    If Not _dontRaisePropertyChange AndAlso Not _loading Then
                        d = New ChangedEventHelper(Me, propertyAlias, d)
                    End If
                End If
                err = False
            Finally
                If err Then
                    If d IsNot Nothing Then d.Dispose()
                End If
            End Try

            Return d
        End Function

        Protected Function Read(ByVal propertyAlias As String) As IDisposable
            Return SyncHelper(True, propertyAlias)
        End Function

        Protected Function Read(ByVal propertyAlias As String, ByVal checkEntity As Boolean) As IDisposable
            Return SyncHelper(True, propertyAlias, checkEntity)
        End Function

        Protected Function Write(ByVal propertyAlias As String) As IDisposable
            Return SyncHelper(False, propertyAlias)
        End Function

        Protected Function GetCurrent() As OrmManager
            Dim mgr As OrmManager = OrmManager.CurrentManager
            If Not String.IsNullOrEmpty(_mgrStr) Then
                Do While mgr IsNot Nothing AndAlso mgr.IdentityString <> _mgrStr
                    mgr = mgr._prev
                Loop
            End If
            Return mgr
        End Function

        Protected Function GetMgr() As IGetManager Implements _IEntity.GetMgr
            Dim mgr As OrmManager = GetCurrent()
            If mgr Is Nothing Then
                If _cm Is Nothing Then
                    Dim a As New ManagerRequiredArgs
                    RaiseEvent ManagerRequired(Me, a)
                    mgr = a.Manager
                    If mgr Is Nothing Then
                        Return Nothing
                    Else
                        If a.DisposeMgr Then
                            Return New GetManagerDisposable(mgr, _schema)
                        Else
                            Return New ManagerWrapper(mgr, _schema)
                        End If
                    End If
                Else
                    Return New GetManagerDisposable(_cm.CreateManager, _schema)
                End If
            Else
                'don't dispose
                Return New ManagerWrapper(mgr, _schema)
            End If
        End Function

        Protected ReadOnly Property MappingEngine() As ObjectMappingEngine Implements _IEntity.MappingEngine
            Get
                If _schema IsNot Nothing Then
                    Return _schema
                Else
                    Dim mgr As OrmManager = OrmManager.CurrentManager
                    If mgr IsNot Nothing Then
                        Return mgr.MappingEngine
                    Else
                        Using mc As IGetManager = GetMgr()
                            If mc IsNot Nothing Then
                                Return mc.Manager.MappingEngine
                            Else
                                Return Nothing
                            End If
                        End Using
                    End If
                End If
            End Get
        End Property

        Protected ReadOnly Property OrmCache() As CacheBase
            Get
                Using mc As IGetManager = GetMgr()
                    If mc IsNot Nothing Then
                        Return mc.Manager.Cache
                    Else
                        Return Nothing
                    End If
                End Using
            End Get
        End Property

        Protected Sub EndLoading() Implements _IEntity.EndLoading
            _loading = False
        End Sub

        Protected Overridable ReadOnly Property ObjName() As String Implements _IEntity.ObjName
            Get
                Return Me.GetType.Name & " - " & ObjectState.ToString & " (" & DumpState() & "): "
            End Get
        End Property

        Protected Overridable Function DumpState() As String
            Dim sb As New StringBuilder
            Dim schema As ObjectMappingEngine = MappingEngine
            If schema Is Nothing Then
                sb.Append("Cannot get object dump")
            Else
                Dim oschema As IEntitySchema = schema.GetEntitySchema(Me.GetType)
                Dim olr As Boolean = _readRaw
                _readRaw = True
                Try
                    For Each kv As DictionaryEntry In schema.GetProperties(Me.GetType)
                        Dim pi As Reflection.PropertyInfo = CType(kv.Value, Reflection.PropertyInfo)
                        Dim c As EntityPropertyAttribute = CType(kv.Key, EntityPropertyAttribute)
                        sb.Append(c.PropertyAlias).Append("=").Append(ObjectMappingEngine.GetPropertyValue(Me, c.PropertyAlias, pi, oschema)).Append(";")
                    Next
                Finally
                    _readRaw = olr
                End Try
            End If
            Return sb.ToString
        End Function

        Public Function GetSyncRoot() As System.IDisposable Implements _IEntity.GetSyncRoot
            Return SyncHelper(False)
        End Function

        Protected Function GetValue(ByVal propertyAlias As String) As Object
            Dim schema As Worm.ObjectMappingEngine = MappingEngine
            If schema Is Nothing Then
                Return ObjectMappingEngine.GetPropertyInt(Me.GetType, propertyAlias)
            Else
                Return schema.GetPropertyValue(Me, propertyAlias)
            End If
        End Function

        Public Function GetValueReflection(ByVal propertyAlias As String, ByVal oschema As IEntitySchema) As Object
            Dim schema As Worm.ObjectMappingEngine = MappingEngine
            Dim pi As Reflection.PropertyInfo
            If schema Is Nothing Then
                pi = ObjectMappingEngine.GetPropertyInt(Me.GetType, oschema, propertyAlias)
            Else
                pi = schema.GetProperty(Me.GetType, oschema, propertyAlias)
            End If
            Return pi.GetValue(Me, Nothing)
        End Function

        Public Sub SetValueReflection(ByVal propertyAlias As String, ByVal value As Object, ByVal oschema As IEntitySchema)
            Dim schema As Worm.ObjectMappingEngine = MappingEngine
            Dim pi As Reflection.PropertyInfo
            If schema Is Nothing Then
                pi = ObjectMappingEngine.GetPropertyInt(Me.GetType, oschema, propertyAlias)
            Else
                pi = schema.GetProperty(Me.GetType, oschema, propertyAlias)
            End If
            pi.SetValue(Me, value, Nothing)
        End Sub

        'Public Overridable Function GetValue(ByVal pi As Reflection.PropertyInfo, _
        '    ByVal propertyAlias As String, ByVal oschema As IEntitySchema) As Object Implements IEntity.GetValueOptimized
        '    If pi Is Nothing Then
        '        Dim s As ObjectMappingEngine = MappingEngine
        '        If s Is Nothing Then
        '            Return ObjectMappingEngine.GetFieldValueSchemaless(Me, propertyAlias, oschema, pi)
        '        Else
        '            Return s.GetFieldValue(Me, propertyAlias, oschema, pi)
        '        End If
        '    End If
        '    Return pi.GetValue(Me, Nothing)
        'End Function

        Protected ReadOnly Property ObjectState() As ObjectState Implements _IEntity.ObjectState
            Get
                Return _state
            End Get
        End Property

        Protected Sub SetObjectStateClear(ByVal value As ObjectState)
            _state = value
        End Sub

        Protected Overridable Sub SetObjectState(ByVal value As ObjectState) Implements _IEntity.SetObjectState
            Using SyncHelper(False)
                Debug.Assert(_state <> Entities.ObjectState.Deleted)
                If _state = Entities.ObjectState.Deleted Then
                    Throw New OrmObjectException(String.Format("Cannot set state while object {0} is in the middle of saving changes", ObjName))
                End If

                Dim olds As ObjectState = _state
                _state = value
                Debug.Assert(_state = value)

#If DEBUG Then
                RaiseEvent ObjectStateChanged(olds)
#End If
            End Using
        End Sub

        'Public Overridable Sub SetValue(ByVal pi As System.Reflection.PropertyInfo, _
        '    ByVal propertyAlias As String, ByVal schema As IEntitySchema, ByVal value As Object) Implements IEntity.SetValueOptimized

        '    If pi Is Nothing Then
        '        pi = MappingEngine.GetProperty(Me.GetType, schema, propertyAlias)
        '    End If

        '    pi.SetValue(Me, value, Nothing)
        'End Sub
        Protected Overridable Overloads Sub Init()
            'If OrmManager.CurrentManager IsNot Nothing Then
            '    _mgrStr = OrmManager.CurrentManager.IdentityString
            'End If
        End Sub

        Public Sub New()
            'Dim arr As Generic.List(Of EntityPropertyAttribute) = OrmManager.CurrentMediaContent.DatabaseSchema.GetSortedFieldList(Me.GetType)
            'members_load_state = New BitArray(arr.Count)

            Init()
        End Sub

        Protected Overridable Overloads Sub Init(ByVal cache As Cache.CacheBase, ByVal schema As ObjectMappingEngine) Implements _IEntity.Init
            If cache IsNot Nothing Then cache.RegisterCreation(Me)

            _state = Entities.ObjectState.Created
        End Sub

        Public Overridable Function Clone() As Object Implements System.ICloneable.Clone
            Dim o As Entity = CreateObject()
            Using SyncHelper(True)
#If TraceSetState Then
                o.SetObjectState(ObjectState, ModifiedObject.ReasonEnum.Unknown, String.Empty, Nothing)
#Else
                o.SetObjectStateClear(_state)
#End If
                CopyBody(Me, o)
            End Using
            Return o
        End Function

        Protected Overridable Function CreateObject() As Entity
            Using gm As IGetManager = GetMgr()
                Return CType(gm.Manager.CreateEntity(Me.GetType), Entity)
            End Using
        End Function

        Protected Overridable Sub CopyProperties(ByVal [from] As _IEntity, ByVal [to] As _IEntity, ByVal mgr As OrmManager, ByVal oschema As IEntitySchema)
            Dim schema As ObjectMappingEngine = mgr.MappingEngine
            For Each kv As DictionaryEntry In schema.GetProperties(Me.GetType)
                Dim pi As Reflection.PropertyInfo = CType(kv.Value, Reflection.PropertyInfo)
                Dim c As EntityPropertyAttribute = CType(kv.Key, EntityPropertyAttribute)
                ObjectMappingEngine.SetPropertyValue([to], c.PropertyAlias, pi, ObjectMappingEngine.GetPropertyValue(from, c.PropertyAlias, pi, oschema), oschema)
            Next
        End Sub

        Protected Overridable Sub CopyBody(ByVal [from] As _IEntity, ByVal [to] As _IEntity) Implements IEntity.CopyBody
            Using mc As IGetManager = GetMgr()
                Dim oschema As IEntitySchema = mc.Manager.MappingEngine.GetEntitySchema(Me.GetType)
                [to].BeginLoading()
                CopyProperties([from], [to], mc.Manager, oschema)
                [to].EndLoading()
            End Using
        End Sub

        Protected Function CreateClone() As Entity Implements IEntity.CreateClone
            Dim clone As Entity = CreateObject()
            clone.SetObjectState(Entities.ObjectState.NotLoaded)
            CopyBody(Me, clone)
            clone._old_state = ObjectState
            clone.SetObjectState(Entities.ObjectState.Clone)
            Return clone
        End Function

        Private Function GetOldState() As ObjectState Implements _IEntity.GetOldState
            Return _old_state
        End Function

        Protected Overridable Function IsPropertyLoaded(ByVal propertyAlias As String) As Boolean Implements IEntity.IsPropertyLoaded
            Return True
        End Function

        Protected Overridable ReadOnly Property IsLoaded() As Boolean Implements IEntity.IsLoaded
            Get
                Return True
            End Get
        End Property

        Protected Overridable Sub CorrectStateAfterLoading(ByVal objectWasCreated As Boolean) Implements _IEntity.CorrectStateAfterLoading
            If objectWasCreated Then
                If ObjectState = Entities.ObjectState.Modified OrElse ObjectState = Entities.ObjectState.Created Then
                    If IsLoaded Then
                        SetObjectState(ObjectState.None)
                    Else
                        SetObjectState(ObjectState.NotLoaded)
                    End If
                    'ElseIf ObjectState = Orm.ObjectState.Created Then
                    '    Debug.Assert(Not IsLoaded)
                    '    SetObjectState(ObjectState.NotLoaded)
                ElseIf ObjectState = ObjectState.NotLoaded Then
                    If IsLoaded Then SetObjectState(ObjectState.None)
                ElseIf ObjectState = Entities.ObjectState.None Then
                    'Else
                    '    Debug.Assert(False)
                End If
            Else
                If ObjectState = ObjectState.NotLoaded Then
                    If IsLoaded Then
                        SetObjectState(ObjectState.None)
                        'Else
                        '    SetObjectState(ObjectState.NotFoundInSource)
                    End If
                End If
            End If
        End Sub

        Public Shared Function IsGoodState(ByVal state As ObjectState) As Boolean
            Return state = ObjectState.Modified OrElse state = ObjectState.Created 'OrElse state = ObjectState.Deleted
        End Function

        Public Sub SetCreateManager(ByVal createManager As ICreateManager) Implements _IEntity.SetCreateManager
            _cm = createManager
        End Sub

        Public ReadOnly Property CreateManager() As ICreateManager Implements IEntity.CreateManager
            Get
                Return _cm
            End Get
        End Property

        Protected Sub SetMgrString(ByVal str As String) Implements _IEntity.SetMgrString
            _mgrStr = str
        End Sub

        Protected Sub SetSpecificSchema(ByVal mpe As ObjectMappingEngine) Implements _IEntity.SetSpecificSchema
            _schema = mpe
        End Sub

#Region " CreateCmd "
        Public Function CreateCmd() As QueryCmd
            If _cm IsNot Nothing Then
                Return New QueryCmd(_cm)
            Else
                Return QueryCmd.Create()
            End If
        End Function

        'Public Function CreateCmd(ByVal selectType As Type) As QueryCmd
        '    If _cm IsNot Nothing Then
        '        Return New QueryCmd(selectType, _cm)
        '    Else
        '        Return QueryCmd.Create(selectType)
        '    End If
        'End Function

        'Public Function CreateCmdByEntityName(ByVal entityName As String) As QueryCmd
        '    If _cm IsNot Nothing Then
        '        Return New QueryCmd(entityName, _cm)
        '    Else
        '        Return QueryCmd.CreateByEntityName(entityName)
        '    End If
        'End Function

        Public Function CreateCmd(ByVal name As String) As QueryCmd
            If _cm IsNot Nothing Then
                Dim q As New QueryCmd(_cm)
                q.Name = name
                Return q
            Else
                Return QueryCmd.Create(name)
            End If
        End Function

        'Public Function CreateCmd(ByVal name As String, ByVal selectType As Type) As QueryCmd
        '    If _cm IsNot Nothing Then
        '        Dim q As New QueryCmd(selectType, _cm)
        '        q.Name = name
        '        Return q
        '    Else
        '        Return QueryCmd.Create(name, selectType)
        '    End If
        'End Function

        'Public Function CreateCmdByEntityName(ByVal name As String, ByVal entityName As String) As QueryCmd
        '    If _cm IsNot Nothing Then
        '        Dim q As New QueryCmd(entityName, _cm)
        '        q.Name = name
        '        Return q
        '    Else
        '        Return QueryCmd.CreateByEntityName(name, entityName)
        '    End If
        'End Function
#End Region

#Region " Create methods "
        Public Shared Function CreateOrmBase(ByVal id As Object, ByVal t As Type, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine) As IKeyEntity
            Dim o As IKeyEntity = CType(Activator.CreateInstance(t), IKeyEntity)
            o.Init(id, cache, schema)
            Return o
        End Function

        Public Shared Function CreateOrmBase(Of T As {IKeyEntity, New})(ByVal id As Object, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine) As T
            Dim o As New T
            o.Init(id, cache, schema)
            Return o
        End Function

        Public Shared Function CreateObject(Of T As {_ICachedEntity, New})(ByVal pk() As PKDesc, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine) As T
            If GetType(IKeyEntity).IsAssignableFrom(GetType(T)) Then
                Return CType(CreateOrmBase(pk(0).Value, GetType(T), cache, schema), T)
            Else
                Return CreateEntity(Of T)(pk, cache, schema)
            End If
        End Function

        Public Shared Function CreateObject(ByVal pk() As PKDesc, ByVal type As Type, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine) As _ICachedEntity
            If GetType(IKeyEntity).IsAssignableFrom(type) Then
                Return CreateOrmBase(pk(0).Value, type, cache, schema)
            Else
                Return CreateEntity(pk, type, cache, schema)
            End If
        End Function

        Public Shared Function CreateEntity(Of T As {_ICachedEntity, New})(ByVal pk() As PKDesc, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine) As T
            Dim o As New T
            o.Init(pk, cache, schema)
            Return o
        End Function

        Public Shared Function CreateEntity(ByVal pk() As PKDesc, ByVal t As Type, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine) As _ICachedEntity
            Dim o As _ICachedEntity = CType(Activator.CreateInstance(t), _ICachedEntity)
            o.Init(pk, cache, schema)
            Return o
        End Function

        Public Shared Function CreateEntity(ByVal t As Type, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine) As IEntity
            Dim o As _IEntity = CType(Activator.CreateInstance(t), _IEntity)
            o.Init(cache, schema)
            Return o
        End Function

        Public Shared Function CreateEntity(Of T As {_IEntity, New})(ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine) As T
            Dim o As New T
            o.Init(cache, schema)
            Return o
        End Function
#End Region
    End Class

End Namespace