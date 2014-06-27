Imports Worm.Entities.Meta
Imports Worm.Cache
Imports Worm.Query
Imports System.Collections.Generic
Imports System.Linq

Namespace Entities

    <Serializable()> _
    Public Class Entity
        Implements _IEntity

        Friend Class ChangedEventHelper
            Implements IDisposable

            Private _value As Object
            Private _fieldName As String
            Private _obj As _IEntity
            Private _d As IDisposable

            Public Sub New(ByVal obj As _IEntity, ByVal propertyAlias As String, ByVal d As IDisposable)
                _fieldName = propertyAlias
                _obj = obj
                _value = GetValue()
                _d = d
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
                _d.Dispose()
                RaisePropertyChanged()
            End Sub

            Private Function GetValue() As Object
                Return ObjectMappingEngine.GetPropertyValue(_obj, _fieldName, _obj.GetEntitySchema(_obj.GetMappingEngine))
            End Function

            Protected Sub RaisePropertyChanged()
                Dim value As Object = GetValue()
                If Not Object.Equals(value, _value) Then
                    _obj.RaisePropertyChanged(New PropertyChangedEventArgs(_fieldName, _value, value))
                End If
            End Sub

        End Class

        Private _state As ObjectState = ObjectState.Created

        <NonSerialized()> _
        Private _loading As Boolean
        <NonSerialized()> _
        Private _mgrStr As String
        '<NonSerialized()> _
        'Protected _dontRaisePropertyChange As Boolean
        '<NonSerialized()> _
        'Protected _readRaw As Boolean
        <NonSerialized()> _
        Private _cm As ICreateManager
        <NonSerialized()> _
        Private _mpe As ObjectMappingEngine

        Public Event ManagerRequired(ByVal sender As IEntity, ByVal args As ManagerRequiredArgs) Implements IEntity.ManagerRequired
        Public Event PropertyChangedEx(ByVal sender As IEntity, ByVal args As PropertyChangedEventArgs) Implements IEntity.PropertyChangedEx

#If DEBUG Then
        Protected Event ObjectStateChanged(ByVal oldState As ObjectState)
#End If

#Region " Loading "
        Protected Property IsLoading() As Boolean Implements _IEntity.IsLoading
            Get
                Return _loading
            End Get
            Set(ByVal value As Boolean)
                _loading = value
            End Set
        End Property

#If TRACELOADING Then
        Public _lstack As String
        Public _estack As String
#End If
        Protected Sub BeginLoading() Implements _IEntity.BeginLoading
#If TRACELOADING Then
            _lstack = Environment.StackTrace
#End If
#If TRACELOADING Then
            _estack = String.Empty
#End If
            _loading = True
        End Sub

        Protected Sub EndLoading() Implements _IEntity.EndLoading
            _loading = False
#If TRACELOADING Then
            _lstack = String.Empty
            _estack = Environment.StackTrace
#End If
        End Sub

        'Protected Overridable Function IsPropertyLoaded(ByVal propertyAlias As String) As Boolean Implements IEntity.IsPropertyLoaded
        '    Return True
        'End Function

        Protected Overridable Property IsLoaded() As Boolean Implements IEntity.IsLoaded
            Get
                Return True
            End Get
            Set(ByVal value As Boolean)
                Throw New NotSupportedException
            End Set
        End Property

        Protected Overridable Sub CorrectStateAfterLoading(ByVal objectWasCreated As Boolean) Implements _IEntity.CorrectStateAfterLoading
            Using SyncHelper(False)
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
            End Using
        End Sub

#End Region

#Region " Synchronization "

        Protected Overridable Function SyncHelper(ByVal reader As Boolean) As IDisposable
#If DebugLocks Then
            Return New CSScopeMgr_Debug(Me, "d:\temp\")
#Else
            Return New CSScopeMgr(Me)
#End If
        End Function

        Public Function AcquareLock() As System.IDisposable Implements _IEntity.AcquareLock
            Return SyncHelper(False)
        End Function

        'Protected Overridable Sub PrepareRead(ByVal propertyAlias As String, ByRef d As IDisposable)
        'End Sub

        'Protected Overridable Sub PrepareUpdate(ByVal mgr As OrmManager)
        'End Sub

        'Protected Sub StartUpdate()
        '    If Not _loading Then 'AndAlso ObjectState <> Orm.ObjectState.Deleted Then
        '        If _state = Entities.ObjectState.Clone Then
        '            Throw New OrmObjectException(ObjName & ": Altering clone is not allowed")
        '        End If

        '        If _state = Entities.ObjectState.Deleted Then
        '            Throw New OrmObjectException(ObjName & ": Altering deleted object is not allowed")
        '        End If

        '        Using mc As IGetManager = GetMgr()
        '            If mc Is Nothing Then
        '                Return
        '            End If

        '            PrepareUpdate(mc.Manager)
        '        End Using
        '        'ElseIf ObjectState = Orm.ObjectState.Created Then
        '        '    _PrepareLoadingUpdate()
        '    End If
        'End Sub

        'Private Function SyncHelper(ByVal reader As Boolean, ByVal propertyAlias As String, ByVal checkEntity As Boolean) As IDisposable
        '    If checkEntity Then
        '        Using mc As IGetManager = GetMgr()
        '            If mc IsNot Nothing Then
        '                Dim mpe As ObjectMappingEngine = mc.Manager.MappingEngine
        '                Dim schema As IEntitySchema = mpe.GetEntitySchema(Me.GetType)
        '                Dim o As ICachedEntity = TryCast(ObjectMappingEngine.GetPropertyValue(Me, propertyAlias, schema), ICachedEntity)
        '                If o IsNot Nothing AndAlso o.ObjectState <> Entities.ObjectState.Created AndAlso Not mc.Manager.IsInCachePrecise(o) Then
        '                    Dim ov As IOptimizedValues = TryCast(Me, IOptimizedValues)
        '                    If ov IsNot Nothing Then
        '                        Dim eo As ICachedEntity = mc.Manager.GetEntityFromCacheOrCreate(o.GetPKValues, o.GetType)
        '                        If eo.CreateManager Is Nothing Then eo.SetCreateManager(CreateManager)
        '                        ov.SetValueOptimized(propertyAlias, schema, eo)
        '                    Else
        '                        Throw New OrmObjectException("Check read requires IOptimizedValues")
        '                    End If
        '                End If
        '            End If
        '        End Using
        '    End If
        '    Return SyncHelper(reader, propertyAlias)
        'End Function

        'Private Function SyncHelper(ByVal reader As Boolean, ByVal propertyAlias As String) As IDisposable
        '    Dim err As Boolean = True
        '    Dim d As IDisposable = New BlankSyncHelper(Nothing)
        '    Try
        '        If reader Then
        '            PrepareRead(propertyAlias, d)
        '        Else
        '            d = SyncHelper(True)
        '            StartUpdate()
        '            If Not _dontRaisePropertyChange AndAlso Not _loading Then
        '                d = New ChangedEventHelper(Me, propertyAlias, d)
        '            End If
        '        End If
        '        err = False
        '    Finally
        '        If err Then
        '            If d IsNot Nothing Then d.Dispose()
        '        End If
        '    End Try

        '    Return d
        'End Function

        'Friend Function _Read(ByVal propertyAlias As String) As IDisposable
        '    Return SyncHelper(True, propertyAlias)
        'End Function

        'Friend Function _Read(ByVal propertyAlias As String, ByVal checkEntity As Boolean) As IDisposable
        '    Return SyncHelper(True, propertyAlias, checkEntity)
        'End Function

        'Friend Function _Write(ByVal propertyAlias As String) As IDisposable
        '    Return SyncHelper(False, propertyAlias)
        'End Function

#End Region

#Region " Manager mgmt "
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
                If _cm IsNot Nothing Then
                    Return New GetManagerDisposable(_cm.CreateManager(Me), _mpe)
                Else
                    Dim a As New ManagerRequiredArgs
                    RaiseEvent ManagerRequired(Me, a)
                    mgr = a.Manager
                    If mgr Is Nothing Then
                        Return Nothing
                    Else
                        If a.DisposeMgr Then
                            Return New GetManagerDisposable(mgr, _mpe)
                        Else
                            Return New ManagerWrapper(mgr, _mpe)
                        End If
                    End If
                End If
            Else
                'don't dispose
                Return New ManagerWrapper(mgr, _mpe)
            End If
        End Function

        Protected Function GetMappingEngine() As ObjectMappingEngine Implements _IEntity.GetMappingEngine
            If _mpe IsNot Nothing Then
                Return _mpe
            Else
                Dim mgr As OrmManager = GetCurrent()
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
        End Function

        Protected Sub SetCreateManager(ByVal createManager As ICreateManager) Implements _IEntity.SetCreateManager
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

        Public Property SpecificMappingEngine() As ObjectMappingEngine Implements _IEntity.SpecificMappingEngine
            Get
                Return _mpe
            End Get
            Set(ByVal value As ObjectMappingEngine)
                _mpe = value
            End Set
        End Property

#End Region

#Region " State mgmt "

        Protected Overridable ReadOnly Property ObjName() As String Implements _IEntity.ObjName
            Get
                Return Me.GetType.Name & " - " & ObjectState.ToString & " (" & DumpState() & "): "
            End Get
        End Property

        Protected Overridable Function DumpState() As String
            Return OrmManager.DumpState(Me)
        End Function

        Protected ReadOnly Property ObjectState() As ObjectState Implements _IEntity.ObjectState
            Get
                Return _state
            End Get
        End Property

        Protected Sub SetObjectStateClear(ByVal value As ObjectState) Implements _IEntity.SetObjectStateClear
            _state = value
        End Sub

        Protected Overridable Sub SetObjectState(ByVal value As ObjectState) Implements _IEntity.SetObjectState
            Using SyncHelper(False)
                If _state = Entities.ObjectState.Deleted Then
                    Assert(_state <> Entities.ObjectState.Deleted, "Object {0} cannot be in Deleted state", ObjName)
                End If

                If _state = Entities.ObjectState.Deleted Then
                    Throw New OrmObjectException(String.Format("Cannot set state while object {0} is in the middle of saving changes", ObjName))
                End If

                Dim olds As ObjectState = _state
                _state = value
                'Debug.Assert(_state = value)

#If DEBUG Then
                RaiseEvent ObjectStateChanged(olds)
#End If
            End Using
        End Sub

        '        Public Overridable Function Clone() As Object Implements System.ICloneable.Clone
        '            Dim o As Entity = CreateSelfInitPK()
        '            Using SyncHelper(True)
        '#If TraceSetState Then
        '                o.SetObjectState(ObjectState, ModifiedObject.ReasonEnum.Unknown, String.Empty, Nothing)
        '#Else
        '                o.SetObjectStateClear(_state)
        '#End If
        '                CopyBody(Me, o)
        '            End Using
        '            Return o
        '        End Function

        'Protected Overridable Function CreateSelf() As Entity
        '    Return CType(Activator.CreateInstance(Me.GetType), Entity)
        'End Function

        Protected Overridable Sub InitNewEntity(ByVal mgr As OrmManager, ByVal en As Entity)
            If mgr Is Nothing Then
                en.Init(Nothing, Nothing)
            Else
                en.Init(mgr.Cache, mgr.MappingEngine)
            End If
        End Sub

        'Protected Function CreateSelfInitPK() As Entity
        '    Dim e As Entity = CreateSelf()
        '    Using gm As IGetManager = GetMgr()
        '        InitNewEntity(If(gm Is Nothing, Nothing, gm.Manager), e)
        '    End Using
        '    Return e
        'End Function

        'Protected Overridable Sub CopyProperties(ByVal [from] As _IEntity, ByVal [to] As _IEntity, _
        '    ByVal oschema As IEntitySchema)

        '    Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap

        '    For Each m As MapField2Column In map
        '        ObjectMappingEngine.SetPropertyValue([to], m.PropertyAlias, ObjectMappingEngine.GetPropertyValue(from, m.PropertyAlias, oschema, m.PropertyInfo), oschema, m.PropertyInfo)
        '    Next
        'End Sub

        'Protected Overridable Sub CopyBody(ByVal [from] As _IEntity, ByVal [to] As _IEntity) Implements IEntity.CopyBody
        '    Using [to].AcquareLock
        '        [to].BeginLoading()
        '        CopyProperties([from], [to], GetEntitySchema(GetMappingEngine()))
        '        [to].EndLoading()
        '    End Using
        'End Sub

        'Protected Function CreateClone() As Entity Implements IEntity.CreateClone
        '    Dim clone As Entity = CreateSelfInitPK()
        '    clone.SetObjectState(Entities.ObjectState.NotLoaded)
        '    CopyBody(Me, clone)
        '    clone._old_state = ObjectState
        '    clone.SetObjectState(Entities.ObjectState.Clone)
        '    Return clone
        'End Function

        'Private Function GetOldState() As ObjectState Implements _IEntity.GetOldState
        '    Return _old_state
        'End Function

        Friend Shared Function IsGoodState(ByVal state As ObjectState) As Boolean
            Return state = ObjectState.Modified OrElse state = ObjectState.Created 'OrElse state = ObjectState.Deleted
        End Function

#End Region

#Region " CreateCmd "
        Public Function CreateCmd() As QueryCmd 'Implements IDataContext.CreateQuery
            If _cm IsNot Nothing Then
                Dim q As QueryCmd = New QueryCmd(_cm)
                q.SpecificMappingEngine = _mpe
                Return q
            Else
                Dim q As QueryCmd = QueryCmd.Create()
                q.SpecificMappingEngine = _mpe
                Return q
            End If
        End Function

        Public Function CreateCmd(ByVal name As String) As QueryCmd
            If _cm IsNot Nothing Then
                Dim q As New QueryCmd(_cm)
                q.Name = name
                q.SpecificMappingEngine = _mpe
                Return q
            Else
                Dim q As QueryCmd = QueryCmd.Create(name)
                q.SpecificMappingEngine = _mpe
                Return q
            End If
        End Function

#End Region

#Region " Create methods "
        Public Shared Function CreateKeyEntity(ByVal id As Object, ByVal t As Type, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine) As ISinglePKEntity
            Dim o As ISinglePKEntity = CType(Activator.CreateInstance(t), ISinglePKEntity)
            o.Init(id, cache, schema)
            Return o
        End Function

        Public Shared Function CreateKeyEntity(Of T As {ISinglePKEntity, New})(ByVal id As Object, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine) As T
            Dim o As New T
            o.Init(id, cache, schema)
            Return o
        End Function

        Public Shared Function CreateObject(Of T As {_ICachedEntity, New})(ByVal pk As IEnumerable(Of PKDesc), ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine) As T
            If GetType(ISinglePKEntity).IsAssignableFrom(GetType(T)) Then
                Return CType(CreateKeyEntity(pk(0).Value, GetType(T), cache, schema), T)
            Else
                Return CreateEntity(Of T)(pk, cache, schema)
            End If
        End Function

        Public Shared Function CreateObject(ByVal pk As IEnumerable(Of PKDesc), ByVal type As Type, ByVal cache As CacheBase, ByVal mpe As ObjectMappingEngine) As Object
            If GetType(ISinglePKEntity).IsAssignableFrom(type) Then
                Return CreateKeyEntity(pk(0).Value, type, cache, mpe)
            ElseIf GetType(ICachedEntity).IsAssignableFrom(type) Then
                Return CreateEntity(pk, type, cache, mpe)
            ElseIf GetType(IEntity).IsAssignableFrom(type) Then
                Return CreateEntity(type, cache, mpe)
            Else
                Dim e As Object = Activator.CreateInstance(type)
                Dim oschema As IEntitySchema = mpe.GetEntitySchema(type)
                For Each p As PKDesc In pk
                    ObjectMappingEngine.SetPropertyValue(e, p.PropertyAlias, p.Value, oschema)
                Next
                Return e
            End If
        End Function

        Public Shared Function CreateEntity(Of T As {_ICachedEntity, New})(ByVal pk As IEnumerable(Of PKDesc), ByVal cache As CacheBase, ByVal mpe As ObjectMappingEngine) As T
            Dim o As New T
            o.Init(pk, cache, mpe)
            Return o
        End Function

        Public Shared Function CreateEntity(ByVal pk As IEnumerable(Of PKDesc), ByVal t As Type, ByVal cache As CacheBase, ByVal mpe As ObjectMappingEngine) As _ICachedEntity
            Dim e As Object = Activator.CreateInstance(t)
            Dim o As _ICachedEntity = CType(e, _ICachedEntity)
            o.Init(pk, cache, mpe)
            Return o
        End Function

        Public Shared Function CreateEntity(ByVal t As Type, ByVal cache As CacheBase, ByVal mpe As ObjectMappingEngine) As _IEntity
            Dim o As _IEntity = CType(Activator.CreateInstance(t), _IEntity)
            o.Init(cache, mpe)
            Return o
        End Function

        Public Shared Function CreateEntity(Of T As {_IEntity, New})(ByVal cache As CacheBase, ByVal mpe As ObjectMappingEngine) As T
            Dim o As New T
            o.Init(cache, mpe)
            Return o
        End Function
#End Region

        Protected Shared Sub Assert(ByVal condition As Boolean, ByVal message As String, ParamArray params As Object())
            'Debug.Assert(condition, String.Format(message, params))
            'Trace.Assert(condition, String.Format(message, params))
            If Not condition Then Throw New OrmObjectException(String.Format(message, params))
        End Sub

        'Protected Overridable Function GetValue(ByVal propertyAlias As String) As Object
        '    Return ObjectMappingEngine.GetPropertyValue(Me, propertyAlias, GetEntitySchema(GetMappingEngine))
        'End Function

        Public Function GetValueReflection(ByVal propertyAlias As String, ByVal oschema As IEntitySchema) As Object
            If oschema Is Nothing Then
                Throw New ArgumentNullException("oschema")
            End If
            'Dim schema As Worm.ObjectMappingEngine = GetMappingEngine()
            'Dim pi As Reflection.PropertyInfo
            'If schema Is Nothing Then
            '    pi = ObjectMappingEngine.GetPropertyInt(Me.GetType, oschema, propertyAlias)
            'Else
            '    pi = schema.GetProperty(Me.GetType, oschema, propertyAlias)
            'End If
            'Return pi.GetValue(Me, Nothing)
            'Return oschema.GetFieldColumnMap(propertyAlias).GetValue(Me)
            Return oschema.FieldColumnMap(propertyAlias).PropertyInfo.GetValue(Me, Nothing)
        End Function

        Public Sub SetValueReflection(ByVal propertyAlias As String, ByVal value As Object, ByVal oschema As IEntitySchema)
            'Dim schema As Worm.ObjectMappingEngine = GetMappingEngine()
            'Dim pi As Reflection.PropertyInfo
            'If schema Is Nothing Then
            '    pi = ObjectMappingEngine.GetPropertyInt(Me.GetType, oschema, propertyAlias)
            'Else
            '    pi = schema.GetProperty(Me.GetType, oschema, propertyAlias)
            'End If
            'pi.SetValue(Me, value, Nothing)
            'oschema.GetFieldColumnMap(propertyAlias).SetValue(Me, value)
            oschema.FieldColumnMap(propertyAlias).PropertyInfo.SetValue(Me, value, Nothing)
        End Sub

        Protected Overridable Overloads Sub Init()
        End Sub

        Public Sub New()
            Init()
        End Sub

        Protected Overridable Overloads Sub Init(ByVal cache As Cache.CacheBase, ByVal schema As ObjectMappingEngine) Implements _IEntity.Init
            If cache IsNot Nothing Then cache.RegisterCreation(Me)

            _state = Entities.ObjectState.Created
            _mpe = schema
        End Sub

        Protected Overridable Function GetEntitySchema(ByVal mpe As ObjectMappingEngine) As Meta.IEntitySchema Implements _IEntity.GetEntitySchema
            If mpe Is Nothing Then
                Return ObjectMappingEngine.GetEntitySchema(Me.GetType, Nothing, Nothing, Nothing)
            Else
                Return mpe.GetEntitySchema(Me.GetType, True)
            End If
        End Function

        Protected Sub RaisePropertyChanged(ByVal propertyChangedEventArgs As PropertyChangedEventArgs) Implements _IEntity.RaisePropertyChanged
            RaiseEvent PropertyChangedEx(Me, propertyChangedEventArgs)
        End Sub

        Public Event PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs) Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged

        Protected Sub RaiseMVVMPropertyChanged(propName As String)
            RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs(propName))
        End Sub

        'Public Function CreateManager1(ctx As Object) As OrmManager Implements ICreateManager.CreateManager

        'End Function

        'Public Event CreateManagerEvent(sender As ICreateManager, args As ICreateManager.CreateManagerEventArgs) Implements ICreateManager.CreateManagerEvent

        'Public ReadOnly Property Cache As CacheBase Implements IDataContext.Cache
        '    Get

        '    End Get
        'End Property

        'Public Property Context As Object Implements IDataContext.Context

    End Class

    'Public Class EntityLazyLoad
    '    Inherits Entity
    '    Implements IPropertyLazyLoad

    '    Protected Function Read(ByVal propertyAlias As String) As System.IDisposable Implements IPropertyLazyLoad.Read
    '        Return _Read(propertyAlias)
    '    End Function

    '    Protected Function Read(ByVal propertyAlias As String, ByVal checkEntity As Boolean) As System.IDisposable Implements IPropertyLazyLoad.Read
    '        Return _Read(propertyAlias, checkEntity)
    '    End Function

    '    Protected Function Write(ByVal propertyAlias As String) As System.IDisposable Implements IPropertyLazyLoad.Write
    '        Return _Write(propertyAlias)
    '    End Function

    '    Protected Overrides Sub PrepareRead(ByVal propertyAlias As String, ByRef d As System.IDisposable)
    '        If Not _readRaw AndAlso (Not IsLoaded AndAlso (ObjectState = Entities.ObjectState.NotLoaded OrElse ObjectState = Entities.ObjectState.None)) Then
    '            If Not IsLoaded AndAlso (ObjectState = Entities.ObjectState.NotLoaded OrElse ObjectState = Entities.ObjectState.None) AndAlso Not IsPropertyLoaded(propertyAlias) Then
    '                Load(propertyAlias)
    '            End If
    '            d = SyncHelper(True)
    '        End If
    '    End Sub

    '    Public Overridable Sub Load(ByVal propertyAlias As String)
    '        Using mc As IGetManager = GetMgr()
    '            If mc Is Nothing Then
    '                Throw New InvalidOperationException("OrmManager required")
    '            End If

    '            Load(mc.Manager, propertyAlias)
    '        End Using
    '    End Sub

    '    Public Sub Load(ByVal mgr As OrmManager, Optional ByVal propertyAlias As String = Nothing)
    '        Dim olds As ObjectState = ObjectState
    '        'Using mc As IGetManager = GetMgr()
    '        mgr.LoadObject(Me, propertyAlias)
    '        'End Using
    '        If olds = Entities.ObjectState.Created AndAlso ObjectState = Entities.ObjectState.Modified Then
    '            AcceptChanges(True, True)
    '        ElseIf IsLoaded Then
    '            SetObjectState(Entities.ObjectState.None)
    '        End If
    '    End Sub
    'End Class
End Namespace