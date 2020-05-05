Imports Worm.Entities.Meta
Imports Worm.Cache
Imports Worm.Query
Imports System.Collections.Generic
Imports System.Linq
Imports CoreFramework

#Const TraceSetState = False

Namespace Entities

    <Serializable()>
    Public Class Entity
        Implements _IEntity

        Friend Class ChangedEventHelper
            Implements IDisposable

            Private ReadOnly _value As Object
            Private ReadOnly _fieldName As String
            Private ReadOnly _obj As _IEntity
            Private ReadOnly _d As IDisposable

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

        <NonSerialized()>
        Private _loading As Boolean
        <NonSerialized()>
        Private _mgrStr As String
        '<NonSerialized()> _
        'Protected _dontRaisePropertyChange As Boolean
        '<NonSerialized()> _
        'Protected _readRaw As Boolean
        <NonSerialized()>
        Private _cm As ICreateManager
        <NonSerialized()>
        Private _mpe As ObjectMappingEngine
        Private _isLoaded As Boolean
        <NonSerialized()>
        Public Event ManagerRequired(ByVal sender As IEntity, ByVal args As ManagerRequiredArgs) Implements IEntity.ManagerRequired
        <NonSerialized()>
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
                Return _isLoaded
            End Get
            Set(ByVal value As Boolean)
                _isLoaded = value
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
                        'ElseIf ObjectState = ObjectState.Modified Then 'after insert or update
                        '    If IsLoaded Then
                        '        SetObjectState(ObjectState.None)
                        '    Else
                        '        SetObjectState(ObjectState.NotLoaded)
                        '    End If
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

        Public ReadOnly Property CreateManager() As ICreateManager Implements IEntity.GetICreateManager
            Get
                Return _cm
            End Get
        End Property

        Public ReadOnly Property GetDataContext() As IDataContext Implements IEntity.GetDataContext
            Get
                If _cm Is Nothing Then
                    Return Nothing
                End If

                If GetType(IDataContext).IsAssignableFrom(_cm.GetType) Then
                    Return CType(_cm, IDataContext)
                Else
                    Return New DataContext(_cm)
                End If
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

#If TraceSetState Then
        Private _setStateStack As String
#End If
        Protected Sub SetObjectStateClear(ByVal value As ObjectState) Implements _IEntity.SetObjectStateClear
            _state = value
#If TraceSetState Then
            _setStateStack = Environment.StackTrace
#End If
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
                SetObjectStateClear(value)
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

        'Protected Overridable Sub InitNewEntity(ByVal mgr As OrmManager, ByVal en As Entity)
        '    If mgr Is Nothing Then
        '        en.Init(Nothing)
        '    Else
        '        en.Init(mgr.MappingEngine)
        '    End If
        'End Sub

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
                Dim q As QueryCmd = New QueryCmd(_cm) With {
                    .SpecificMappingEngine = _mpe
                }
                Return q
            Else
                Dim q As QueryCmd = QueryCmd.Create()
                q.SpecificMappingEngine = _mpe
                Return q
            End If
        End Function

        Public Function CreateCmd(ByVal name As String) As QueryCmd
            If _cm IsNot Nothing Then
                Dim q As New QueryCmd(_cm) With {
                    .Name = name,
                    .SpecificMappingEngine = _mpe
                }
                Return q
            Else
                Dim q As QueryCmd = QueryCmd.Create(name)
                q.SpecificMappingEngine = _mpe
                Return q
            End If
        End Function

#End Region

#Region " Create methods "
        Public Shared Function CreateKeyEntity(ByVal id As Object, ByVal t As Type, ByVal mpe As ObjectMappingEngine) As ISinglePKEntity
            If id Is Nothing Then
                Throw New ArgumentNullException("id")
            End If
            Dim o As ISinglePKEntity = CType(Activator.CreateInstance(t), ISinglePKEntity)
            o.Init(mpe)

            Dim oschema As IEntitySchema = mpe.GetEntitySchema(t)
            InitSinglePK(id, o, oschema)

            Return o
        End Function

        Private Shared Sub InitSinglePK(id As Object, o As ISinglePKEntity, oschema As IEntitySchema)
            Dim m = oschema.GetPK
            Dim col = m.SourceFields(0).SourceFieldExpression
            Dim propertyAlias = m.PropertyAlias
            Dim value = id
            Dim fv = TryCast(o, IStorageValueConverter)
            If fv IsNot Nothing Then
                value = fv.CreateValue(oschema, m, propertyAlias, col, id)
            End If

            Dim pi = m.PropertyInfo

            If pi Is Nothing Then
            Else
                If m.Converter Is Nothing Then
                    If pi.PropertyType Is value.GetType Then
                        m.Converter = MapField2Column.EmptyConverter
                    Else
                        m.Converter = Converters.GetConverter(pi.PropertyType, value.GetType)
                    End If
                End If

                If m.Converter IsNot MapField2Column.EmptyConverter Then
                    value = m.Converter(pi.PropertyType, value.GetType, value)
                End If
            End If

            Dim svo = TryCast(oschema, IOptimizeSetValue)
            If m.OptimizedSetValue Is Nothing Then
                If svo IsNot Nothing Then
                    m.OptimizedSetValue = svo.GetOptimizedDelegate(col)
                Else
                    m.OptimizedSetValue = MapField2Column.EmptyOptimizedSetValue
                End If
            End If

            If m.OptimizedSetValue Is Nothing OrElse m.OptimizedSetValue Is MapField2Column.EmptyOptimizedSetValue Then

                o.Identifier = id
                o.PKLoaded(1, StringExtensions.Coalesce(propertyAlias, MapField2Column.PK))

            Else
                m.OptimizedSetValue(o, value)
            End If

            'o.SetObjectStateClear(ObjectState.NotLoaded)
        End Sub

        Public Shared Function CreateKeyEntity(Of T As {ISinglePKEntity, New})(ByVal id As Object, ByVal mpe As ObjectMappingEngine) As T
            If id Is Nothing Then
                Throw New ArgumentNullException("id")
            End If
            Dim o As New T

            o.Init(mpe)
            Dim oschema As IEntitySchema = mpe.GetEntitySchema(GetType(T))

            InitSinglePK(id, o, oschema)

            Return o
        End Function

        Public Shared Function CreateObject(Of T As {_ICachedEntity, New})(ByVal pk As IPKDesc, ByVal mpe As ObjectMappingEngine) As T
            If pk Is Nothing Then
                Throw New ArgumentNullException("id")
            End If
            If GetType(ISinglePKEntity).IsAssignableFrom(GetType(T)) Then
                Return CType(CreateKeyEntity(pk(0).Value, GetType(T), mpe), T)
            Else
                Return CreateEntity(Of T)(pk, mpe)
            End If
        End Function

        Public Shared Function CreateObject(ByVal pk As IPKDesc, ByVal type As Type, ByVal mpe As ObjectMappingEngine) As Object
            If pk Is Nothing Then
                Throw New ArgumentNullException("id")
            End If
            If GetType(ISinglePKEntity).IsAssignableFrom(type) Then
                Return CreateKeyEntity(pk(0).Value, type, mpe)
            ElseIf GetType(ICachedEntity).IsAssignableFrom(type) Then
                Return CreateEntity(pk, type, mpe)
            ElseIf GetType(IEntity).IsAssignableFrom(type) Then
                Return CreateEntity(type, mpe)
            Else
                Dim e As Object = Activator.CreateInstance(type)
                InitPK(pk, type, mpe, e)

                Return e
            End If
        End Function

        Private Shared Sub InitPK(pk As IPKDesc, type As Type, mpe As ObjectMappingEngine, e As Object)
            Dim oschema As IEntitySchema = mpe.GetEntitySchema(type)
            Dim fv = TryCast(e, IStorageValueConverter)
            Dim m = oschema.GetPK
            Dim propertyAlias = m.PropertyAlias
            Dim svo = TryCast(oschema, IOptimizeSetValue)

            If pk.Count = 1 Then
                If pk(0).Column <> m.SourceFields(0).SourceFieldExpression Then
                    'Throw New ObjectMappingException($"PK has field {pk(0).Column} different to pk field {m.SourceFields(0).SourceFieldExpression}")
                End If

                Dim col = m.SourceFields(0).SourceFieldExpression 'pk(0).Column
                Dim value As Object = pk(0).Value

                If fv IsNot Nothing Then
                    value = fv.CreateValue(oschema, m, propertyAlias, col, value)
                End If

                Dim pi = m.PropertyInfo

                If pi Is Nothing Then
                Else
                    If m.Converter Is Nothing Then
                        If pi.PropertyType Is value.GetType Then
                            m.Converter = MapField2Column.EmptyConverter
                        Else
                            m.Converter = Converters.GetConverter(pi.PropertyType, value.GetType)
                        End If
                    End If

                    If m.Converter IsNot MapField2Column.EmptyConverter Then
                        value = m.Converter(pi.PropertyType, value.GetType, value)
                    End If
                End If

                If m.OptimizedSetValue Is Nothing Then
                    If svo IsNot Nothing Then
                        m.OptimizedSetValue = svo.GetOptimizedDelegate(col)
                    Else
                        m.OptimizedSetValue = MapField2Column.EmptyOptimizedSetValue
                    End If
                End If

                If m.OptimizedSetValue Is Nothing OrElse m.OptimizedSetValue Is MapField2Column.EmptyOptimizedSetValue Then
                    ObjectMappingEngine.SetPK(e, {New PKDesc2(col, value, pi)}, oschema, propertyAlias)
                Else
                    m.OptimizedSetValue(e, value)
                End If

            Else
                Dim pks As New List(Of PKDesc2)

                For j = 0 To m.SourceFields.Count - 1
                    Dim sf = m.SourceFields(j)
                    Dim colName As String = sf.SourceFieldExpression
                    Dim pkf = pk(j)
                    'Dim pkf = pk.FirstOrDefault(Function(it) it.Column = colName)
                    'If pkf Is Nothing Then
                    '    Throw New ObjectMappingException($"PK doesnot contain field {colName}")
                    'End If

                    Dim value As Object = pkf.Value

                    If fv IsNot Nothing Then
                        value = fv.CreateValue(oschema, m, propertyAlias, colName, value)
                    End If

                    Dim pi = sf.PropertyInfo

                    If pi Is Nothing Then
                    Else
                        If sf.Converter Is Nothing Then
                            If pi.PropertyType Is value.GetType Then
                                sf.Converter = MapField2Column.EmptyConverter
                            Else
                                sf.Converter = Converters.GetConverter(pi.PropertyType, value.GetType)
                            End If
                        End If

                        If sf.Converter IsNot MapField2Column.EmptyConverter Then
                            value = sf.Converter(pi.PropertyType, value.GetType, value)
                        End If
                    End If

                    If sf.OptimizedSetValue Is Nothing Then
                        If svo IsNot Nothing Then
                            sf.OptimizedSetValue = svo.GetOptimizedDelegate(colName)
                        Else
                            sf.OptimizedSetValue = MapField2Column.EmptyOptimizedSetValue
                        End If
                    End If

                    pks.Add(New PKDesc2(colName, value, sf.PropertyInfo, sf.OptimizedSetValue))

                Next

                If pks.All(Function(it) it.svo IsNot Nothing AndAlso it.svo IsNot MapField2Column.EmptyOptimizedSetValue) Then
                    For Each pkasd In pks
                        pkasd.svo(e, pkasd.Value)
                    Next
                Else
                    ObjectMappingEngine.SetPK(e, pks, oschema, propertyAlias)
                End If
            End If

            Dim ce = TryCast(e, _ICachedEntity)
            If ce IsNot Nothing Then
                ce.PKLoaded(pk.Count, oschema)
            Else
                Dim ll = TryCast(e, IPropertyLazyLoad)
                If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True)
            End If

            'Dim ie = TryCast(e, _IEntity)
            'If ie IsNot Nothing Then
            '    ie.SetObjectStateClear(ObjectState.NotLoaded)
            'End If
        End Sub

        Public Shared Function CreateEntity(Of T As {_ICachedEntity, New})(ByVal pk As IPKDesc, ByVal mpe As ObjectMappingEngine) As T
            If pk Is Nothing Then
                Throw New ArgumentNullException("id")
            End If
            Dim o As New T
            InitPK(pk, GetType(T), mpe, o)
            Return o
        End Function

        Public Shared Function CreateEntity(ByVal pk As IPKDesc, ByVal t As Type, ByVal mpe As ObjectMappingEngine) As _ICachedEntity
            If pk Is Nothing Then
                Throw New ArgumentNullException("id")
            End If
            Dim e As Object = Activator.CreateInstance(t)
            Dim o As _ICachedEntity = CType(e, _ICachedEntity)
            InitPK(pk, t, mpe, o)
            Return o
        End Function

        Public Shared Function CreateEntity(ByVal t As Type, ByVal mpe As ObjectMappingEngine) As _IEntity
            Dim o As _IEntity = CType(Activator.CreateInstance(t), _IEntity)
            o.Init(mpe)
            Return o
        End Function

        Public Shared Function CreateEntity(Of T As {_IEntity, New})(ByVal mpe As ObjectMappingEngine) As T
            Dim o As New T
            o.Init(mpe)
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

        Public Function GetValue(ByVal propertyAlias As String, ByVal oschema As IEntitySchema) As Object
            'If oschema Is Nothing Then
            '    Throw New ArgumentNullException("oschema")
            'End If
            'Dim schema As Worm.ObjectMappingEngine = GetMappingEngine()
            'Dim pi As Reflection.PropertyInfo
            'If schema Is Nothing Then
            '    pi = ObjectMappingEngine.GetPropertyInt(Me.GetType, oschema, propertyAlias)
            'Else
            '    pi = schema.GetProperty(Me.GetType, oschema, propertyAlias)
            'End If
            'Return pi.GetValue(Me, Nothing)
            'Return oschema.GetFieldColumnMap(propertyAlias).GetValue(Me)
            'Return oschema.FieldColumnMap(propertyAlias).PropertyInfo.GetValue(Me, Nothing)
            Return ObjectMappingEngine.GetPropertyValue(Me, propertyAlias, oschema)
        End Function

        Public Sub SetValue(ByVal propertyAlias As String, ByVal value As Object, ByVal oschema As IEntitySchema)
            'Dim schema As Worm.ObjectMappingEngine = GetMappingEngine()
            'Dim pi As Reflection.PropertyInfo
            'If schema Is Nothing Then
            '    pi = ObjectMappingEngine.GetPropertyInt(Me.GetType, oschema, propertyAlias)
            'Else
            '    pi = schema.GetProperty(Me.GetType, oschema, propertyAlias)
            'End If
            'pi.SetValue(Me, value, Nothing)
            'oschema.GetFieldColumnMap(propertyAlias).SetValue(Me, value)
            'oschema.FieldColumnMap(propertyAlias).PropertyInfo.SetValue(Me, value, Nothing)
            ObjectMappingEngine.SetPropertyValue(Me, propertyAlias, value, oschema)
        End Sub

        Protected Overridable Overloads Sub Init()
        End Sub

        Public Sub New()
            Init()
        End Sub

        Protected Overridable Overloads Sub Init(ByVal schema As ObjectMappingEngine) Implements _IEntity.Init
            'If Cache IsNot Nothing Then Cache.RegisterCreation(Me)

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
            Dim propName = GetEntitySchema(GetMappingEngine())?.GetPropertyByAlias(Me.GetType, propertyChangedEventArgs.PropertyAlias)?.Name
            If Not String.IsNullOrEmpty(propName) Then
                RaiseMVVMPropertyChanged(propName)
            End If
        End Sub
        <NonSerialized>
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
        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, Entity))
        End Function

        Public Overloads Function Equals(ByVal obj As Entity) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            If Me.GetType IsNot obj.GetType Then
                Return False
            End If

            Return EntityExtensions.Equals(Me, obj)
        End Function
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