Imports Worm.Entities.Meta
Imports Worm.Cache
Imports System.ComponentModel
Imports System.Collections.Generic
Imports System.Xml
Imports Worm.Query

Namespace Entities
    <Serializable()> _
    Public Class OrmObjectException
        Inherits Exception

        Private _obj As ICachedEntity

        Public Sub New()
            ' Add other code for custom properties here.
        End Sub

        Public Sub New(ByVal message As String)
            MyBase.New(message)
            ' Add other code for custom properties here.
        End Sub

        Public Sub New(ByVal message As String, ByVal obj As ICachedEntity)
            MyBase.New(message)
            _obj = obj
        End Sub

        Public Sub New(ByVal message As String, ByVal inner As Exception)
            MyBase.New(message, inner)
            ' Add other code for custom properties here.
        End Sub

        Protected Sub New( _
            ByVal info As System.Runtime.Serialization.SerializationInfo, _
            ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
            ' Insert code here for custom properties here.
        End Sub
    End Class

    <Serializable()> _
    Public MustInherit Class CachedEntity
        Inherits Entity
        Implements _ICachedEntityEx

        Protected _key As Integer
        Private _loaded As Boolean
        Private _loaded_members As BitArray

        <NonSerialized()> _
        Private _upd As New UpdateCtx

        '<NonSerialized()> _
        'Protected Friend _needAdd As Boolean
        '<NonSerialized()> _
        'Protected Friend _needDelete As Boolean
        '<NonSerialized()> _
        'Protected Friend _upd As IList(Of Worm.Criteria.Core.EntityFilter)
        '<NonSerialized()> _
        'Protected Friend _valProcs As Boolean
        '<NonSerialized()> _
        'Protected Friend _needAccept As New Generic.List(Of AcceptState)
        <NonSerialized()> _
        Protected _hasPK As Boolean
        <NonSerialized()> _
        Private _alterLock As New Object
        <NonSerialized()> _
        Private _copy As CachedEntity

        '<EditorBrowsable(EditorBrowsableState.Never)> _
        'Public Class AcceptState

        'End Class

        Public Class RelatedObject
            Private _dst As CachedEntity
            Private _props() As Pair(Of String)

            Public Sub New(ByVal src As CachedEntity, ByVal dst As CachedEntity, ByVal properties() As String)
                _dst = dst

                Dim l As New List(Of Pair(Of String))
                For Each p As String In properties
                    l.Add(New Pair(Of String)(p, p))
                Next
                _props = l.ToArray

                AddHandler src.Saved, AddressOf Added
            End Sub

            Public Sub New(ByVal src As CachedEntity, ByVal dst As CachedEntity, ByVal properties() As Pair(Of String))
                _dst = dst
                _props = properties
                AddHandler src.Saved, AddressOf Added
            End Sub

            Public Sub Added(ByVal source As ICachedEntity, ByVal args As ObjectSavedArgs)
                Using mc As IGetManager = source.GetMgr
                    Dim mgr As OrmManager = mc.Manager
                    Dim dt As Type = _dst.GetType
                    Dim schema As ObjectMappingEngine = mgr.MappingEngine
                    Dim oschema As IEntitySchema = schema.GetObjectSchema(dt)
                    Dim pk As Boolean, pk_old As PKDesc() = _dst.GetPKValues
                    For Each p As Pair(Of String) In _props
                        'If p = "ID" Then
                        '    Dim nm As OrmManager.INewObjects = mgr.NewObjectManager
                        '    If nm IsNot Nothing Then
                        '        nm.RemoveNew(_dst)
                        '    End If
                        '    _dst.SetPK(source.GetPKValues)
                        '    If nm IsNot Nothing Then
                        '        mgr.NewObjectManager.AddNew(_dst)
                        '    End If
                        'Else
                        Dim dc As ColumnAttribute = schema.GetColumnByPropertyAlias(dt, p.Second, oschema)
                        'Dim sc As New ColumnAttribute(p.First)
                        Dim o As Object = schema.GetPropertyValue(source, p.First, oschema)
                        'Dim pi As Reflection.PropertyInfo = mgr.MappingEngine.GetProperty(dt, oschema, c)
                        '_dst.SetValue(pi, c, oschema, o)
                        schema.SetPropertyValue(_dst, p.Second, o, oschema)
                        If (schema.GetAttributes(oschema, dc) And Field2DbRelations.PK) = Field2DbRelations.PK Then
                            pk = True
                        End If
                        'End If
                    Next
                    If pk Then
                        Dim nm As INewObjectsStore = mgr.Cache.NewObjectManager
                        If nm IsNot Nothing Then
                            nm.RemoveNew(dt, pk_old)

                            nm.AddNew(_dst)
                        End If
                    End If
                    RemoveHandler source.Saved, AddressOf Added
                End Using
            End Sub
        End Class

        <EditorBrowsable(EditorBrowsableState.Never)> _
        Public Class InternalClass
            Private _o As CachedEntity

            Friend Sub New(ByVal o As CachedEntity)
                _o = o
            End Sub

            Public ReadOnly Property GetMgr() As IGetManager
                Get
                    Return _o.GetMgr
                End Get
            End Property

            Public Property IsLoaded() As Boolean
                Get
                    Return _o.IsLoaded
                End Get
                Set(ByVal value As Boolean)
                    Throw New NotImplementedException
                End Set
                'Get
                '    Return _o._loaded
                'End Get
                'Protected Friend Set(ByVal value As Boolean)
                '    Using _o.SyncHelper(False)
                '        If value AndAlso Not _o._loaded Then
                '            Using mc As IGetManager = GetMgr()
                '                Dim arr As Generic.List(Of ColumnAttribute) = mc.Manager.ObjectSchema.GetSortedFieldList(_o.GetType)
                '                For i As Integer = 0 To arr.Count - 1
                '                    _o._members_load_state(i) = True
                '                Next
                '            End Using
                '        ElseIf Not value AndAlso _o._loaded Then
                '            Using mc As IGetManager = GetMgr()
                '                Dim arr As Generic.List(Of ColumnAttribute) = mc.Manager.ObjectSchema.GetSortedFieldList(_o.GetType)
                '                For i As Integer = 0 To arr.Count - 1
                '                    _o._members_load_state(i) = False
                '                Next
                '            End Using
                '        End If
                '        _o._loaded = value
                '        Debug.Assert(_o._loaded = value)
                '    End Using
                'End Set
            End Property

            Public Function IsPropertyLoaded(ByVal propertyAlias As String) As Boolean
                Return _o.IsPropertyLoaded(propertyAlias)
            End Function
            'Public ReadOnly Property OrmCache() As OrmBase
            '    Get
            '        Using mc As IGetManager = GetMgr()
            '            If mc IsNot Nothing Then
            '                Return mc.Manager.Cache
            '            Else
            '                Return Nothing
            '            End If
            '        End Using
            '    End Get
            'End Property

            '''' <summary>
            '''' Объект, на котором можно синхронизировать загрузку
            '''' </summary>
            'Public ReadOnly Property SyncLoad() As Object
            '    Get
            '        Return Me
            '    End Get
            'End Property

            Public ReadOnly Property ObjectState() As ObjectState
                Get
                    Return _o.ObjectState
                End Get
            End Property

            'Public Property ObjectState() As ObjectState
            '    Get
            '        Return _o._state
            '    End Get
            '    Protected Friend Set(ByVal value As ObjectState)
            '        _o.ObjectState = value
            '        'Using _o.SyncHelper(False)
            '        '    _o._state = value
            '        '    Debug.Assert(_o._state = value)
            '        '    Debug.Assert(value <> Orm.ObjectState.None OrElse IsLoaded)
            '        '    If value = Orm.ObjectState.None AndAlso Not IsLoaded Then
            '        '        Throw New OrmObjectException(String.Format("Cannot set state none while object {0} is not loaded", ObjName))
            '        '    End If
            '        'End Using
            '    End Set
            'End Property

            ''' <summary>
            ''' Модифицированная версия объекта
            ''' </summary>
            Public ReadOnly Property OriginalCopy() As ICachedEntity
                Get
                    Return _o.OriginalCopy
                End Get
            End Property

            Public ReadOnly Property IsReadOnly() As Boolean
                Get
                    Using gm As IGetManager = GetMgr
                        Return _o.IsReadOnly(gm.Manager)
                    End Using
                    'Using _o.SyncHelper(True)
                    '    If _o._state = Orm.ObjectState.Modified Then
                    '        _o.CheckCash()
                    '        Dim mo As ModifiedObject = OrmCache.Modified(_o)
                    '        'If mo Is Nothing Then mo = _mo
                    '        If mo IsNot Nothing Then
                    '            Using mc As IGetManager = GetMgr()
                    '                If mo.User IsNot Nothing AndAlso Not mo.User.Equals(mc.Manager.CurrentUser) Then
                    '                    Return True
                    '                End If
                    '            End Using
                    '        End If
                    '        'ElseIf state = Obm.ObjectState.Deleted Then
                    '        'Return True
                    '    End If
                    '    Return False
                    'End Using
                End Get
            End Property

            Public ReadOnly Property Changes(ByVal obj As ICachedEntity) As ColumnAttribute()
                Get
                    Return _o.Changes(obj)
                    'Dim columns As New Generic.List(Of ColumnAttribute)
                    'Dim t As Type = obj.GetType
                    'For Each pi As Reflection.PropertyInfo In _o.GetType.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic)
                    '    Dim c As ColumnAttribute = CType(Attribute.GetCustomAttribute(pi, GetType(ColumnAttribute), True), ColumnAttribute)
                    '    If c IsNot Nothing Then
                    '        Dim original As Object = pi.GetValue(obj, Nothing)
                    '        If (_o.OrmSchema.GetAttributes(t, c) And Field2DbRelations.ReadOnly) <> Field2DbRelations.ReadOnly Then
                    '            Dim current As Object = pi.GetValue(_o, Nothing)
                    '            If (original IsNot Nothing AndAlso Not original.Equals(current)) OrElse _
                    '                (current IsNot Nothing AndAlso Not current.Equals(original)) Then
                    '                columns.Add(c)
                    '            End If
                    '        End If
                    '    End If
                    'Next
                    'Return columns.ToArray
                End Get
            End Property

            Public ReadOnly Property CanEdit() As Boolean
                Get
                    Return _o.CanEdit
                End Get
            End Property

            Public ReadOnly Property CanLoad() As Boolean
                Get
                    Return _o.CanLoad
                End Get
            End Property

            Public ReadOnly Property ObjName() As String
                Get
                    Return _o.ObjName
                End Get
            End Property

            Public Overridable ReadOnly Property ChangeDescription() As String
                Get
                    Return _o.ChangeDescription
                    'Dim sb As New StringBuilder
                    'sb.Append("Аттрибуты:").Append(vbCrLf)
                    'If ObjectState = Orm.ObjectState.Modified Then
                    '    For Each c As ColumnAttribute In Changes(OriginalCopy)
                    '        sb.Append(vbTab).Append(c.FieldName).Append(vbCrLf)
                    '    Next
                    'Else
                    '    Dim t As Type = _o.GetType
                    '    'Dim o As OrmBase = CType(t.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, _
                    '    '    New Object() {Identifier, OrmCache, _schema}), OrmBase)
                    '    'Dim o As OrmBase = GetNew()
                    '    Dim o As OrmBase = CType(Activator.CreateInstance(t), OrmBase)
                    '    o.Init(_o.Identifier, _o.OrmCache, _o.OrmSchema)
                    '    For Each c As ColumnAttribute In Changes(o)
                    '        sb.Append(vbTab).Append(c.FieldName).Append(vbCrLf)
                    '    Next
                    'End If
                    'Return sb.ToString
                End Get
            End Property

            Public ReadOnly Property HasBodyChanges() As Boolean
                Get
                    Return _o.HasBodyChanges
                End Get
            End Property

            Public ReadOnly Property HasM2MChanges() As Boolean
                Get
                    'Using mc As IGetManager = GetMgr
                    Return _o.HasM2MChanges()
                    'End Using
                End Get
            End Property

            Public ReadOnly Property HasChanges() As Boolean
                Get
                    Return _o.HasChanges
                End Get
            End Property

            Public Function GetM2MRelatedChangedObjects() As List(Of CachedEntity)
                Return _o.GetM2MRelatedChangedObjects
            End Function

            Public Function GetRelatedChangedObjects() As List(Of CachedEntity)
                Return _o.GetRelatedChangedObjects
            End Function

            Public Function GetChangedObjectGraph() As List(Of CachedEntity)
                Return _o.GetChangedObjectGraph
            End Function

            Public Function GetChangedObjectGraphWithSelf() As List(Of CachedEntity)
                Return _o.GetChangedObjectGraphWithSelf
            End Function
        End Class

        Public Event Saved(ByVal sender As ICachedEntity, ByVal args As ObjectSavedArgs) Implements ICachedEntity.Saved
        Public Event Added(ByVal sender As ICachedEntity, ByVal args As EventArgs) Implements ICachedEntity.Added
        Public Event Deleted(ByVal sender As ICachedEntity, ByVal args As EventArgs) Implements ICachedEntityEx.Deleted
        Public Event Updated(ByVal sender As ICachedEntity, ByVal args As EventArgs) Implements ICachedEntity.Updated
        Public Event OriginalCopyRemoved(ByVal sender As ICachedEntity) Implements ICachedEntity.OriginalCopyRemoved

        Protected ReadOnly Property Key() As Integer Implements ICachedEntity.Key
            Get
                If Not IsPKLoaded Then Throw New OrmObjectException("Object has no primary key")
                Return _key
            End Get
        End Property

        Protected Overridable Sub CreateCopyForSaveNewEntry(ByVal mgr As OrmManager, ByVal pk() As PKDesc) Implements _ICachedEntity.CreateCopyForSaveNewEntry
            Debug.Assert(_copy Is Nothing)
            Dim clone As CachedEntity = CType(CreateClone(), CachedEntity)
            SetObjectState(Entities.ObjectState.Modified)
            _copy = clone
            'Using mc As IGetManager = GetMgr()
            Dim c As CacheBase = mgr.Cache
            c.RegisterModification(mgr, Me, pk, ObjectModification.ReasonEnum.Unknown)
            'End Using
            If pk IsNot Nothing Then clone.SetPK(pk)
        End Sub

        Protected MustOverride Function GetCacheKey() As Integer

        Protected Overrides Sub Init(ByVal cache As Cache.CacheBase, ByVal schema As ObjectMappingEngine)
            Throw New NotSupportedException
        End Sub

        Protected Overridable Sub PKLoaded(ByVal pkCount As Integer) Implements _ICachedEntityEx.PKLoaded
            _key = GetCacheKey()
            _hasPK = True
        End Sub

        Private Function CheckIsAllLoaded(ByVal schema As ObjectMappingEngine, ByVal loadedColumns As Integer) As Boolean Implements _ICachedEntity.CheckIsAllLoaded
            Using SyncHelper(False)
                Dim allloaded As Boolean = True
                If Not _loaded OrElse _loaded_members.Count <= loadedColumns Then
                    Dim arr As Generic.List(Of ColumnAttribute) = SortedColumnAttributeList()
                    For i As Integer = 0 To arr.Count - 1
                        If Not _members_load_state(i) Then
                            'Dim at As Field2DbRelations = schema.GetAttributes(Me.GetType, arr(i))
                            'If (at And Field2DbRelations.PK) <> Field2DbRelations.PK Then
                            allloaded = False
                            Exit For
                            'End If
                        End If
                    Next
                    _loaded = allloaded
                End If
                Return allloaded
            End Using
        End Function

        Protected Property _members_load_state(ByVal idx As Integer) As Boolean
            Get
                If _loaded_members Is Nothing Then
                    _loaded_members = New BitArray(SortedColumnAttributeCount)
                End If
                Return _loaded_members(idx)
            End Get
            Set(ByVal value As Boolean)
                If _loaded_members Is Nothing Then
                    _loaded_members = New BitArray(SortedColumnAttributeCount)
                End If
                _loaded_members(idx) = value
            End Set
        End Property

        Private Function SetLoaded(ByVal propertyAlias As String, ByVal loaded As Boolean, ByVal check As Boolean, ByVal schema As ObjectMappingEngine) As Boolean Implements _ICachedEntity.SetLoaded

            Dim c As ColumnAttribute = schema.GetColumnByPropertyAlias(Me.GetType, propertyAlias)

            Return SetLoaded(c, loaded, check, schema)
        End Function

        Private Function SetLoaded(ByVal c As ColumnAttribute, ByVal loaded As Boolean, ByVal check As Boolean, ByVal schema As ObjectMappingEngine) As Boolean Implements _ICachedEntity.SetLoaded

            Dim idx As Integer = c.Index
            If idx = -1 Then
                Dim arr As Generic.List(Of ColumnAttribute) = SortedColumnAttributeList()
                idx = arr.BinarySearch(c)
                c.Index = idx
            End If

            If idx < 0 AndAlso check Then Throw New OrmObjectException("There is no such field " & c.PropertyAlias)

            If idx >= 0 Then
                'Using SyncHelper(False)
                Dim old As Boolean = _members_load_state(idx)
                _members_load_state(idx) = loaded
                Return old
                'End Using
            End If
        End Function

        Public Function BeginAlter() As IDisposable Implements ICachedEntity.BeginAlter
#If DebugLocks Then
            Return New CSScopeMgr_Debug(_alterLock, "d:\temp")
#Else
            Return New CSScopeMgr(_alterLock)
#End If
        End Function

        Public Function BeginEdit() As IDisposable Implements ICachedEntity.BeginEdit
#If DebugLocks Then
            Dim d As IDisposable = New CSScopeMgr_Debug(_alterLock, "d:\temp")
#Else
            Dim d As IDisposable = New CSScopeMgr(_alterLock)
#End If
            If Not CanEdit Then
                d.Dispose()
                Throw New ObjectStateException(ObjName & "Object is not editable")
            End If
            Return d
        End Function

        Public Sub CheckEditOrThrow() Implements ICachedEntity.CheckEditOrThrow
            If Not CanEdit Then Throw New ObjectStateException(ObjName & "Object is not editable")
        End Sub

        Public Overridable Sub RemoveFromCache(ByVal cache As CacheBase) Implements ICachedEntity.RemoveFromCache
            _copy = Nothing
        End Sub

        Protected Overrides ReadOnly Property IsLoaded() As Boolean
            Get
                Return _loaded
            End Get
        End Property

        Public ReadOnly Property InternalProperties() As InternalClass
            Get
                Return New InternalClass(Me)
            End Get
        End Property

        Public Overridable Sub Load() Implements ICachedEntity.Load
            Using mc As IGetManager = GetMgr()
                If mc Is Nothing Then
                    Throw New InvalidOperationException("OrmManager required")
                End If

                Load(mc.Manager)
            End Using
        End Sub

        Public Sub Load(ByVal mgr As OrmManager) Implements _ICachedEntity.Load
            Dim mo As ObjectModification = mgr.Cache.ShadowCopy(Me)
            'If mo Is Nothing Then mo = _mo
            If mo IsNot Nothing Then
                If mo.User IsNot Nothing Then
                    'Using mc As IGetManager = GetMgr()
                    If Not mo.User.Equals(mgr.CurrentUser) Then
                        Throw New OrmObjectException(ObjName & "Object in readonly state")
                    End If
                    'End Using
                Else
                    If ObjectState = Entities.ObjectState.Deleted OrElse ObjectState = Entities.ObjectState.Modified Then
                        Throw New OrmObjectException(ObjName & "Cannot load object while its state is deleted or modified!")
                    End If
                End If
            End If
            Dim olds As ObjectState = ObjectState
            'Using mc As IGetManager = GetMgr()
            mgr.LoadObject(Me)
            'End Using
            If olds = Entities.ObjectState.Created AndAlso ObjectState = Entities.ObjectState.Modified Then
                AcceptChanges(True, True)
            ElseIf IsLoaded Then
                SetObjectState(Entities.ObjectState.None)
            End If
            Invariant()
        End Sub

        <EditorBrowsable(EditorBrowsableState.Never)> _
        <Conditional("DEBUG")> _
        Public Sub Invariant()
            Using SyncHelper(True)
                If IsLoaded AndAlso _
                    ObjectState <> Entities.ObjectState.None AndAlso ObjectState <> Entities.ObjectState.Modified AndAlso ObjectState <> Entities.ObjectState.Deleted Then Throw New OrmObjectException(ObjName & "When object is loaded its state has to be None or Modified or Deleted: current state is " & ObjectState.ToString)
                If Not IsLoaded AndAlso _
                   (ObjectState = Entities.ObjectState.None OrElse ObjectState = Entities.ObjectState.Modified OrElse ObjectState = Entities.ObjectState.Deleted) Then Throw New OrmObjectException(ObjName & "When object is not loaded its state has not be None or Modified or Deleted: current state is " & ObjectState.ToString)
                If ObjectState = Entities.ObjectState.Modified AndAlso OrmCache.ShadowCopy(Me) Is Nothing Then
                    'Throw New OrmObjectException(ObjName & "When object is in modified state it has to have an original copy")
                    SetObjectStateClear(Entities.ObjectState.None)
                    Load()
                End If
            End Using
        End Sub

        Protected Overrides Sub SetObjectState(ByVal value As ObjectState)
            Using SyncHelper(False)
                Debug.Assert(value <> Entities.ObjectState.None OrElse IsLoaded)
                If value = Entities.ObjectState.None AndAlso Not IsLoaded Then
                    Throw New OrmObjectException(String.Format("Cannot set state none while object {0} is not loaded", ObjName))
                End If

                Debug.Assert(Not _upd.Deleted)
                If _upd.Deleted Then
                    Throw New OrmObjectException(String.Format("Cannot set state while object {0} is in the middle of saving changes", ObjName))
                End If

                MyBase.SetObjectState(value)
            End Using
        End Sub

        'Friend Overrides ReadOnly Property ObjName() As String
        '    Get
        '        Return Me.GetType.Name & " - " & ObjectState.ToString & " (" & _key & "): "
        '    End Get
        'End Property

        Private Sub SetLoaded(ByVal value As Boolean) Implements _ICachedEntity.SetLoaded
            Using SyncHelper(False)
                Using mc As IGetManager = GetMgr()
                    If value AndAlso Not _loaded Then
                        Dim cnt As Integer = SortedColumnAttributeCount()
                        For i As Integer = 0 To cnt - 1
                            _members_load_state(i) = True
                        Next
                    ElseIf Not value AndAlso _loaded Then
                        Dim cnt As Integer = SortedColumnAttributeCount()
                        For i As Integer = 0 To cnt - 1
                            _members_load_state(i) = False
                        Next
                    End If
                    _loaded = value
                    Debug.Assert(_loaded = value)
                End Using
            End Using
        End Sub

        Public Sub AcceptChanges()
            AcceptChanges(True, IsGoodState(ObjectState))
        End Sub

        Public Function AcceptChanges(ByVal updateCache As Boolean, ByVal setState As Boolean) As ICachedEntity Implements ICachedEntity.AcceptChanges
            Dim mo As _ICachedEntity = Nothing
            Using SyncHelper(False)
                If ObjectState = Entities.ObjectState.Created OrElse ObjectState = Entities.ObjectState.Clone Then 'OrElse _state = Orm.ObjectState.NotLoaded Then
                    Throw New OrmObjectException(ObjName & "accepting changes allowed in state Modified, deleted or none")
                End If

                Using gmc As IGetManager = GetMgr()
                    Dim mc As OrmManager = gmc.Manager
                    '_valProcs = HasM2MChanges(mc)

                    AcceptRelationalChanges(updateCache, mc)

                    If (ObjectState <> Entities.ObjectState.None OrElse Not WrappedProperties) Then
                        mo = RemoveVersionData(mc.Cache, setState)
                        Dim c As OrmCache = TryCast(mc.Cache, OrmCache)
                        If _upd.Deleted Then
                            '_valProcs = False
                            If updateCache AndAlso c IsNot Nothing Then
                                c.UpdateCache(mc.MappingEngine, New Pair(Of _ICachedEntity)() {New Pair(Of _ICachedEntity)(Me, mo)}, mc, AddressOf ClearCacheFlags, Nothing, Nothing, False, False)
                                'mc.Cache.UpdateCacheOnDelete(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing)
                            End If
                            Accept_AfterUpdateCacheDelete(Me, mc)
                            RaiseEvent Deleted(Me, EventArgs.Empty)
                        ElseIf _upd.Added Then
                            '_valProcs = False
                            Dim dic As IDictionary = mc.GetDictionary(Me.GetType)
                            Dim kw As CacheKey = New CacheKey(Me)
                            Dim o As CachedEntity = CType(dic(kw), CachedEntity)
                            If (o Is Nothing) OrElse (Not o.IsLoaded AndAlso IsLoaded) Then
                                dic(kw) = Me
                            End If
                            If updateCache AndAlso c IsNot Nothing Then
                                'mc.Cache.UpdateCacheOnAdd(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing, Nothing)
                                c.UpdateCache(mc.MappingEngine, New Pair(Of _ICachedEntity)() {New Pair(Of _ICachedEntity)(Me, mo)}, mc, AddressOf ClearCacheFlags, Nothing, Nothing, False, False)
                            End If
                            Accept_AfterUpdateCacheAdd(Me, mc.Cache, mo)
                            RaiseEvent Added(Me, EventArgs.Empty)
                        Else
                            If updateCache Then
                                If c IsNot Nothing Then
                                    c.UpdateCache(mc.MappingEngine, New Pair(Of _ICachedEntity)() {New Pair(Of _ICachedEntity)(Me, mo)}, mc, AddressOf ClearCacheFlags, Nothing, Nothing, False, True)
                                End If
                                UpdateCacheAfterUpdate(c)
                            End If
                            RaiseEvent Updated(Me, EventArgs.Empty)
                        End If
                        'ElseIf _valProcs AndAlso updateCache Then
                        '    mc.Cache.ValidateSPOnUpdate(Me, Nothing)
                    End If
                End Using
            End Using

            Return mo
        End Function

        Protected Overridable ReadOnly Property HasBodyChanges() As Boolean
            Get
                Return ObjectState = Entities.ObjectState.Modified OrElse ObjectState = Entities.ObjectState.Deleted OrElse ObjectState = Entities.ObjectState.Created
            End Get
        End Property

        Protected Overridable ReadOnly Property HasM2MChanges() As Boolean
            Get
                Return False
            End Get
        End Property

        Protected Overridable ReadOnly Property HasChanges() As Boolean Implements ICachedEntity.HasChanges
            Get
                Return HasBodyChanges OrElse HasM2MChanges()
            End Get
        End Property

        Protected Friend Shared Sub ClearCacheFlags(ByVal obj As _ICachedEntity, ByVal mc As OrmManager, _
            ByVal contextKey As Object)
            obj.UpdateCtx.Added = False
            obj.UpdateCtx.Deleted = False
        End Sub

        'Protected Overrides Sub CorrectStateAfterLoading(ByVal objectWasCreated As Boolean)
        '    Dim os As ObjectState = ObjectState
        '    MyBase.CorrectStateAfterLoading(objectWasCreated)
        '    If objectWasCreated Then
        '        If os = Orm.ObjectState.Modified Then
        '            OrmCache.UnregisterModification(Me)
        '        End If
        '    End If
        'End Sub

        Protected Function RemoveVersionData(ByVal cache As CacheBase, ByVal setState As Boolean) As _ICachedEntity
            Dim mo As _ICachedEntity = Nothing

            'unreg = unreg OrElse _state <> Orm.ObjectState.Created
            If setState Then
                SetObjectStateClear(Entities.ObjectState.None)
                Debug.Assert(IsLoaded)
                If Not IsLoaded Then
                    Throw New OrmObjectException(ObjName & "Cannot set state None while object is not loaded")
                End If
            End If
            'If unreg Then
            mo = CType(OriginalCopy, _ICachedEntity)
            cache.UnregisterModification(Me)
            _copy = Nothing
            '_mo = Nothing
            'End If

            Return mo
        End Function

        Protected Friend Sub RaiseCopyRemoved() Implements _ICachedEntity.RaiseCopyRemoved
            RaiseEvent OriginalCopyRemoved(Me)
        End Sub

        Protected Overridable Sub AcceptRelationalChanges(ByVal updateCache As Boolean, ByVal mc As OrmManager)
            '_needAccept.Clear()

            'Dim rel As IRelation = mc.ObjectSchema.GetConnectedTypeRelation(t)
            'If rel IsNot Nothing Then
            '    Dim c As New OrmManager.M2MEnum(rel, Me, mc.ObjectSchema)
            '    mc.Cache.ConnectedEntityEnum(t, AddressOf c.Accept)
            'End If
        End Sub

        Friend Shared Sub Accept_AfterUpdateCacheDelete(ByVal obj As CachedEntity, ByVal mc As OrmManager)
            mc._RemoveObjectFromCache(obj)
            Dim c As OrmCache = TryCast(mc.Cache, OrmCache)
            If c IsNot Nothing Then
                c.RegisterDelete(obj)
            End If
            'obj._needDelete = False
        End Sub

        Friend Shared Sub Accept_AfterUpdateCacheAdd(ByVal obj As CachedEntity, ByVal cache As CacheBase, _
            ByVal contextKey As Object)
            'obj._needAdd = False
            Dim nm As INewObjectsStore = cache.NewObjectManager
            If nm IsNot Nothing Then
                Dim mo As CachedEntity = TryCast(contextKey, CachedEntity)
                If mo Is Nothing Then
                    Dim dic As Generic.Dictionary(Of CachedEntity, CachedEntity) = TryCast(contextKey, Generic.Dictionary(Of CachedEntity, CachedEntity))
                    If dic IsNot Nothing Then
                        dic.TryGetValue(obj, mo)
                    End If
                End If
                If mo IsNot Nothing Then
                    nm.RemoveNew(mo)
                End If
            End If
        End Sub

        Protected Sub _Init(ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine)
            MyBase.Init(cache, schema)
            If schema IsNot Nothing Then
                'Dim arr As Generic.List(Of ColumnAttribute) = schema.GetSortedFieldList(Me.GetType)
                _loaded_members = New BitArray(schema.GetProperties(Me.GetType).Count)
            End If
        End Sub

        Protected Overridable Sub SetPK(ByVal pk As PKDesc())
            'Using m As IGetManager = GetMgr()
            Dim tt As Type = Me.GetType
            Dim schema As ObjectMappingEngine = MappingEngine
            Dim oschema As IEntitySchema = schema.GetObjectSchema(tt)
            BeginLoading()
            For Each p As PKDesc In pk
                'Dim c As New ColumnAttribute(p.PropertyAlias)
                schema.SetPropertyValue(Me, p.PropertyAlias, p.Value, oschema)
                SetLoaded(p.PropertyAlias, True, True, schema)
            Next
            EndLoading()
            'End Using
        End Sub

        Protected Overridable Overloads Sub Init(ByVal pk() As PKDesc, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine) Implements _ICachedEntity.Init
            _Init(cache, schema)
            SetPK(pk)
            PKLoaded(pk.Length)
        End Sub

#Region " Xml Serialization "

        Protected Overridable Function GetSchema() As System.Xml.Schema.XmlSchema Implements System.Xml.Serialization.IXmlSerializable.GetSchema
            Return Nothing
        End Function

        Protected Overridable Sub ReadXml(ByVal reader As System.Xml.XmlReader) Implements System.Xml.Serialization.IXmlSerializable.ReadXml
            Dim t As Type = Me.GetType

            'If OrmSchema IsNot Nothing Then
            '    Dim arr As Generic.List(Of ColumnAttribute) = OrmSchema.GetSortedFieldList(Me.GetType)
            '    _members_load_state = New BitArray(arr.Count)
            'End If

            CType(Me, _IEntity).BeginLoading()

            Using mc As IGetManager = GetMgr()
                Dim schema As ObjectMappingEngine = mc.Manager.MappingEngine

                With reader
l1:
                    If .NodeType = XmlNodeType.Element AndAlso .Name = t.Name Then
                        ReadValues(mc.Manager, reader, schema)

                        Do While .Read
                            Select Case .NodeType
                                Case XmlNodeType.Element
                                    ReadValue(mc.Manager, .Name, reader, schema)
                                Case XmlNodeType.EndElement
                                    If .Name = t.Name Then Exit Do
                            End Select
                        Loop
                    Else
                        Do While .Read
                            Select Case .NodeType
                                Case XmlNodeType.Element
                                    If .Name = t.Name Then
                                        GoTo l1
                                    End If
                            End Select
                        Loop
                    End If
                End With

                CType(Me, _IEntity).EndLoading()

                If schema IsNot Nothing Then CheckIsAllLoaded(schema, Integer.MaxValue)
            End Using
        End Sub

        Protected Overridable Sub WriteXml(ByVal writer As System.Xml.XmlWriter) Implements System.Xml.Serialization.IXmlSerializable.WriteXml
            With writer
                Dim t As Type = Me.GetType

                Dim elems As New Generic.List(Of Pair(Of String, Object))
                Dim xmls As New Generic.List(Of Pair(Of String, String))
                Dim objs As New List(Of Pair(Of String, PKDesc()))

                For Each de As DictionaryEntry In MappingEngine.GetProperties(t)
                    Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                    Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                    If c IsNot Nothing AndAlso (MappingEngine.GetAttributes(t, c) And Field2DbRelations.Private) = 0 Then
                        If IsLoaded Then
                            Dim v As Object = pi.GetValue(Me, Nothing)
                            Dim tt As Type = pi.PropertyType
                            If GetType(ICachedEntity).IsAssignableFrom(tt) Then
                                '.WriteAttributeString(c.FieldName, CType(v, ICachedEntity).Identifier.ToString)
                                If v IsNot Nothing Then
                                    objs.Add(New Pair(Of String, PKDesc())(c.PropertyAlias, CType(v, ICachedEntity).GetPKValues))
                                Else
                                    objs.Add(New Pair(Of String, PKDesc())(c.PropertyAlias, Nothing))
                                End If
                            ElseIf tt.IsArray Then
                                elems.Add(New Pair(Of String, Object)(c.PropertyAlias, pi.GetValue(Me, Nothing)))
                            ElseIf tt Is GetType(XmlDocument) Then
                                xmls.Add(New Pair(Of String, String)(c.PropertyAlias, CType(pi.GetValue(Me, Nothing), XmlDocument).OuterXml))
                            Else
                                If v IsNot Nothing Then
                                    .WriteAttributeString(c.PropertyAlias, Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture))
                                Else
                                    .WriteAttributeString(c.PropertyAlias, "xxx:nil")
                                End If
                            End If
                        ElseIf (MappingEngine.GetAttributes(t, c) And Field2DbRelations.PK) = Field2DbRelations.PK Then
                            .WriteAttributeString(c.PropertyAlias, pi.GetValue(Me, Nothing).ToString)
                        End If
                    End If
                Next

                For Each p As Pair(Of String, Object) In elems
                    .WriteStartElement(p.First)
                    .WriteValue(p.Second)
                    .WriteEndElement()
                Next

                For Each p As Pair(Of String, String) In xmls
                    .WriteStartElement(p.First)
                    .WriteCData(p.Second)
                    .WriteEndElement()
                Next

                For Each p As Pair(Of String, PKDesc()) In objs
                    .WriteStartElement(p.First)
                    If p.Second IsNot Nothing Then
                        For Each pk As PKDesc In p.Second
                            .WriteStartElement("pk")
                            'Dim v As String = "xxx:nil"
                            'If pk.Second IsNot Nothing Then
                            '    v = pk.Second.ToString
                            'End If
                            .WriteAttributeString(pk.PropertyAlias, pk.Value.ToString)
                            .WriteEndElement()
                        Next
                    End If
                    .WriteEndElement()
                Next
                '.WriteEndElement() 't.Name
            End With
        End Sub

        Protected Sub ReadValue(ByVal mgr As OrmManager, ByVal propertyAlias As String, ByVal reader As XmlReader, ByVal schema As ObjectMappingEngine)
            reader.Read()
            'Dim c As ColumnAttribute = OrmSchema.GetColumnByFieldName(Me.GetType, fieldName)
            Select Case reader.NodeType
                Case XmlNodeType.CDATA
                    Dim pi As Reflection.PropertyInfo = schema.GetProperty(Me.GetType, propertyAlias)
                    'Dim c As ColumnAttribute = schema.GetColumnByFieldName(Me.GetType, fieldName)
                    Dim x As New XmlDocument
                    x.LoadXml(reader.Value)
                    pi.SetValue(Me, x, Nothing)
                    SetLoaded(propertyAlias, True, True, schema)
                Case XmlNodeType.Text
                    Dim pi As Reflection.PropertyInfo = schema.GetProperty(Me.GetType, propertyAlias)
                    'Dim c As ColumnAttribute = schema.GetColumnByFieldName(Me.GetType, fieldName)
                    Dim v As String = reader.Value
                    pi.SetValue(Me, Convert.FromBase64String(CStr(v)), Nothing)
                    SetLoaded(propertyAlias, True, True, schema)
                    'Using ms As New IO.MemoryStream(Convert.FromBase64String(CStr(v)))
                    '    Dim f As New Runtime.Serialization.Formatters.Binary.BinaryFormatter()
                    '    pi.SetValue(Me, f.Deserialize(ms), Nothing)
                    '    SetLoaded(c, True)
                    'End Using
                Case XmlNodeType.EndElement
                    Dim pi As Reflection.PropertyInfo = schema.GetProperty(Me.GetType, propertyAlias)
                    'Dim c As ColumnAttribute = schema.GetColumnByFieldName(Me.GetType, fieldName)
                    pi.SetValue(Me, Nothing, Nothing)
                    SetLoaded(propertyAlias, True, True, schema)
                Case XmlNodeType.Element
                    Dim pi As Reflection.PropertyInfo = schema.GetProperty(Me.GetType, propertyAlias)
                    Dim c As ColumnAttribute = schema.GetColumnByPropertyAlias(Me.GetType, propertyAlias)
                    Dim o As ICachedEntity = Nothing
                    Dim pk() As PKDesc = GetPKs(reader)
                    'Using mc As IGetManager = GetMgr()
                    If (schema.GetAttributes(Me.GetType, c) And Field2DbRelations.Factory) = Field2DbRelations.Factory Then
                        Dim f As IFactory = TryCast(Me, IFactory)
                        If f IsNot Nothing Then
                            f.CreateObject(pk(0).PropertyAlias, pk(0).Value)
                        Else
                            Throw New OrmObjectException(String.Format("Preperty {0} is factory property. Implementation of IFactory is required.", propertyAlias))
                        End If
                    Else
                        o = mgr.CreateObject(pk, pi.PropertyType)
                        pi.SetValue(Me, o, Nothing)
                        If o IsNot Nothing Then
                            o.SetCreateManager(CreateManager)
                        End If
                    End If
                    'End Using
                    SetLoaded(propertyAlias, True, True, schema)
            End Select
        End Sub

        Private Function GetPKs(ByVal reader As XmlReader) As PKDesc()
            Dim l As New List(Of PKDesc)
            Do
                If reader.NodeType = XmlNodeType.Element AndAlso reader.Name = "pk" Then
                    reader.MoveToFirstAttribute()
                    Do
                        'Dim v As String = reader.Value
                        'If v = "xxx:nil" Then v = Nothing
                        l.Add(New PKDesc(reader.Name, reader.Value))
                    Loop While reader.MoveToNextAttribute
                End If
                reader.Read()
            Loop Until reader.NodeType = XmlNodeType.EndElement
            Return l.ToArray
        End Function

        Protected Sub ReadValues(ByVal mgr As OrmManager, ByVal reader As XmlReader, ByVal schema As ObjectMappingEngine)
            With reader
                .MoveToFirstAttribute()
                Dim t As Type = Me.GetType
                Dim oschema As IEntitySchema = Nothing
                If schema IsNot Nothing Then
                    oschema = schema.GetObjectSchema(t)
                End If

                Dim fv As IDBValueFilter = TryCast(oschema, IDBValueFilter)
                Dim pk_count As Integer

                Do

                    Dim pi As Reflection.PropertyInfo = schema.GetProperty(t, oschema, .Name)
                    Dim c As ColumnAttribute = schema.GetColumnByPropertyAlias(t, .Name)

                    Dim att As Field2DbRelations = schema.GetAttributes(oschema, c)

                    If (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                        Dim value As String = .Value
                        If value = "xxx:nil" Then value = Nothing
                        If fv IsNot Nothing Then
                            value = CStr(fv.CreateValue(.Name, Me, value))
                        End If

                        Dim v As Object = Convert.ChangeType(value, pi.PropertyType)
                        If pi IsNot Nothing Then
                            pi.SetValue(Me, v, Nothing)
                            SetLoaded(.Name, True, True, schema)
                            pk_count += 1
                        End If
                    End If
                Loop While .MoveToNextAttribute
                Dim obj As _ICachedEntity = Me

                If pk_count > 0 Then
                    PKLoaded(pk_count)
                    'Using mc As IGetManager = GetMgr()
                    Dim c As OrmCache = TryCast(mgr.Cache, OrmCache)
                    If c IsNot Nothing AndAlso c.IsDeleted(Me) Then
                        Return
                    End If

                    If ObjectState = ObjectState.Created Then
                        CreateCopyForSaveNewEntry(mgr, Nothing)
                        'Cache.Modified(obj).Reason = ModifiedObject.ReasonEnum.SaveNew
                    Else
                        'Using mc As IGetManager = GetMgr()
                        obj = mgr.NormalizeObject(Me, mgr.GetDictionary(Me.GetType))
                        'End Using

                        If obj.ObjectState = ObjectState.Modified OrElse obj.ObjectState = ObjectState.Deleted Then
                            Return
                        End If

                        obj.BeginLoading()
                    End If
                    'End Using
                End If

                .MoveToFirstAttribute()
                Do

                    Dim pi As Reflection.PropertyInfo = schema.GetProperty(t, oschema, .Name)
                    Dim c As ColumnAttribute = schema.GetColumnByPropertyAlias(t, .Name)

                    Dim att As Field2DbRelations = schema.GetAttributes(oschema, c)
                    'Dim not_pk As Boolean = (att And Field2DbRelations.PK) = 0

                    'Me.IsLoaded = not_pk
                    If (att And Field2DbRelations.PK) <> Field2DbRelations.PK Then
                        Dim value As String = .Value
                        If value = "xxx:nil" Then value = Nothing
                        If fv IsNot Nothing Then
                            value = CStr(fv.CreateValue(.Name, obj, value))
                        End If

                        If GetType(IKeyEntity).IsAssignableFrom(pi.PropertyType) Then
                            'If (att And Field2DbRelations.Factory) = Field2DbRelations.Factory Then
                            '    CreateObject(.Name, value)
                            '    SetLoaded(c, True)
                            'Else
                            'Using mc As IGetManager = GetMgr()
                            Dim type_created As Type = pi.PropertyType
                            Dim en As String = schema.GetEntityNameByType(type_created)
                            If Not String.IsNullOrEmpty(en) Then
                                type_created = schema.GetTypeByEntityName(en)

                                If type_created Is Nothing Then
                                    Throw New OrmManagerException("Cannot find type for entity " & en)
                                End If
                            End If
                            Dim v As IKeyEntity = mgr.GetOrmBaseFromCacheOrCreate(value, type_created)
                            If pi IsNot Nothing Then
                                pi.SetValue(obj, v, Nothing)
                                SetLoaded(.Name, True, True, schema)
                                If v IsNot Nothing Then
                                    v.SetCreateManager(CreateManager)
                                End If
                            End If
                            'End Using
                            'End If
                        Else
                            Dim v As Object = Convert.ChangeType(value, pi.PropertyType)
                            If pi IsNot Nothing Then
                                pi.SetValue(obj, v, Nothing)
                                SetLoaded(.Name, True, True, schema)
                            End If
                        End If

                        'If (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                        '    If OrmCache IsNot Nothing Then OrmCache.RegisterCreation(Me.GetType, Identifier)
                        'End If
                    End If
                Loop While .MoveToNextAttribute
            End With
        End Sub


#End Region

#Region " IComparable "

        Public Function CompareTo(ByVal other As CachedEntity) As Integer
            If other Is Nothing Then
                'Throw New MediaObjectModelException(ObjName & "other parameter cannot be nothing")
                Return 1
            End If
            Return Key.CompareTo(other.Key)
        End Function

        Protected Function _CompareTo(ByVal obj As Object) As Integer Implements System.IComparable.CompareTo
            Return CompareTo(TryCast(obj, CachedEntity))
        End Function

#End Region

        Private ReadOnly Property IsPKLoaded() As Boolean Implements _ICachedEntity.IsPKLoaded
            Get
                Return _hasPK
            End Get
        End Property

        Public Overridable Function GetPKValues() As PKDesc() Implements ICachedEntity.GetPKValues
            Dim l As New List(Of PKDesc)
            'Using mc As IGetManager = GetMgr()
            Dim schema As Worm.ObjectMappingEngine = MappingEngine
            Dim oschema As IEntitySchema = schema.GetObjectSchema(Me.GetType)
            For Each kv As DictionaryEntry In schema.GetProperties(Me.GetType)
                Dim pi As Reflection.PropertyInfo = CType(kv.Value, Reflection.PropertyInfo)
                Dim c As ColumnAttribute = CType(kv.Key, ColumnAttribute)
                If (schema.GetAttributes(oschema, c) And Field2DbRelations.PK) = Field2DbRelations.PK Then
                    l.Add(New PKDesc(c.PropertyAlias, ObjectMappingEngine.GetPropertyValue(Me, c.PropertyAlias, pi, oschema)))
                End If
            Next
            'End Using
            Return l.ToArray
        End Function

        Protected Overridable ReadOnly Property WrappedProperties() As Boolean
            Get
                Return True
            End Get
        End Property

        Protected ReadOnly Property OriginalCopy() As ICachedEntity Implements ICachedEntity.OriginalCopy
            Get
                Using SyncHelper(False)
                    If _copy Is Nothing Then
                        If (ObjectState = Entities.ObjectState.Modified OrElse Not WrappedProperties) AndAlso ObjectState <> Entities.ObjectState.Created Then
                            Using gm As IGetManager = GetMgr()
                                _copy = CType(gm.Manager.GetObjectFromStorage(Me), CachedEntity)
                            End Using
                        End If
                    End If

                    Return _copy
                End Using
                'Return OriginalCopy(OrmCache)
            End Get
        End Property

        'Protected ReadOnly Property OriginalCopy(ByVal cache As CacheBase) As ICachedEntity Implements _ICachedEntity.OriginalCopy
        '    Get
        '        Dim o As ObjectModification = cache.ShadowCopy(Me)
        '        If o Is Nothing Then Return Nothing
        '        Return o.Obj
        '    End Get
        'End Property

        Protected Overridable ReadOnly Property ChangeDescription() As String Implements ICachedEntity.ChangeDescription
            Get
                Dim sb As New StringBuilder
                sb.Append("Attributes:").Append(vbCrLf)
                If ObjectState = Entities.ObjectState.Modified Then
                    For Each c As ColumnAttribute In Changes(OriginalCopy)
                        sb.Append(vbTab).Append(c.PropertyAlias).Append(vbCrLf)
                    Next
                Else
                    Dim t As Type = Me.GetType
                    'Dim o As OrmBase = CType(t.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, _
                    '    New Object() {Identifier, OrmCache, _schema}), OrmBase)
                    'Dim o As OrmBase = GetNew()
                    Dim o As ICachedEntity = CType(CreateObject(), ICachedEntity)
                    For Each c As ColumnAttribute In Changes(o)
                        sb.Append(vbTab).Append(c.PropertyAlias).Append(vbCrLf)
                    Next
                End If
                Return sb.ToString
            End Get
        End Property

        Protected Overridable ReadOnly Property Changes(ByVal obj As ICachedEntity) As ColumnAttribute()
            Get
                Dim columns As New Generic.List(Of ColumnAttribute)
                Dim t As Type = obj.GetType
                'Using mc As IGetManager = GetMgr()
                Dim schema As ObjectMappingEngine = MappingEngine
                Dim oschema As IEntitySchema = schema.GetObjectSchema(t)
                For Each de As DictionaryEntry In schema.GetProperties(t, oschema)
                    Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                    Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                    Dim original As Object = ObjectMappingEngine.GetPropertyValue(obj, c.PropertyAlias, pi, oschema)
                    If (schema.GetAttributes(oschema, c) And Field2DbRelations.ReadOnly) <> Field2DbRelations.ReadOnly Then
                        Dim current As Object = ObjectMappingEngine.GetPropertyValue(Me, c.PropertyAlias, pi, oschema)
                        If (original IsNot Nothing AndAlso Not original.Equals(current)) OrElse _
                            (current IsNot Nothing AndAlso Not current.Equals(original)) Then
                            columns.Add(c)
                        End If
                    End If
                Next
                'End Using
                Return columns.ToArray
            End Get
        End Property

        Protected Overrides Function CreateObject() As Entity
            Using gm As IGetManager = GetMgr()
                Return CType(gm.Manager.CreateEntity(GetPKValues, Me.GetType), Entity)
            End Using
        End Function

        'Protected Overrides Sub _PrepareLoadingUpdate()
        '    CreateCopyForSaveNewEntry(Nothing)
        'End Sub

        Protected Overrides Sub _PrepareUpdate(ByVal mgr As OrmManager)
            If Not IsLoaded Then
                If ObjectState = Entities.ObjectState.None Then
                    Throw New InvalidOperationException(String.Format("Object {0} is not loaded while the state is None", ObjName))
                End If

                If ObjectState = Entities.ObjectState.NotLoaded Then
                    Load()
                    If ObjectState = Entities.ObjectState.NotFoundInSource Then
                        Throw New OrmObjectException(ObjName & "Object is not editable 'cause it is not found in source")
                    End If
                Else
                    Return
                End If
            End If

            Dim mo As ObjectModification = mgr.Cache.ShadowCopy(Me)
            If mo IsNot Nothing Then
                'Using mc As IGetManager = GetMgr()
                If mo.User IsNot Nothing AndAlso Not mo.User.Equals(mgr.CurrentUser) Then
                    Throw New OrmObjectException(ObjName & "Object has already altered by another user")
                End If
                'End Using
                If ObjectState = Entities.ObjectState.Deleted Then SetObjectState(ObjectState.Modified)
            Else
                'Debug.Assert(ObjectState = Orm.ObjectState.None) ' OrElse state = Obm.ObjectState.Created)
                'CreateModified(_id)
                CreateClone4Edit(mgr)
                EnsureInCache(mgr)
                'If modified.old_state = Obm.ObjectState.Created Then
                '    _mo = mo
                'End If
            End If
        End Sub

        Protected Function EnsureInCache(ByVal mgr As OrmManager) As CachedEntity
            'Using mc As IGetManager = GetMgr()
            Return CType(mgr.EnsureInCache(Me), CachedEntity)
            'End Using
        End Function

        Protected Sub CreateClone4Edit(ByVal mgr As OrmManager)
            If _copy Is Nothing Then
                Dim clone As Entity = CreateClone()
                SetObjectState(Entities.ObjectState.Modified)
                _copy = CType(clone, CachedEntity)
                'Using mc As IGetManager = GetMgr()

                'OrmCache.RegisterModification(modified).Reason = ModifiedObject.ReasonEnum.Edit
                If Not IsLoading Then
                    Dim mgrLocal As OrmManager = GetCurrent()
                    If mgrLocal IsNot Nothing Then
                        mgrLocal.RaiseBeginUpdate(Me)
                    End If
                End If
            End If
            mgr.Cache.RegisterModification(mgr, Me, ObjectModification.ReasonEnum.Edit)
            'End Using
        End Sub

        Protected Sub CreateClone4Delete(ByVal mgr As OrmManager)
            SetObjectState(Entities.ObjectState.Deleted)
            mgr.Cache.RegisterModification(mgr, Me, ObjectModification.ReasonEnum.Delete)
            Dim mgrLocal As OrmManager = GetCurrent()
            If mgrLocal IsNot Nothing Then
                mgrLocal.RaiseBeginDelete(Me)
            End If
            'Dim clone As Entity = CreateClone()
            'Using mc As IGetManager = GetMgr()
            'End Using
        End Sub

        Public Function SaveChanges(ByVal acceptChanges As Boolean) As Boolean Implements ICachedEntity.SaveChanges
            Using mc As IGetManager = GetMgr()
                Return mc.Manager.SaveChanges(Me, acceptChanges)
            End Using
        End Function

        Public Sub UpdateCache(ByVal mc As OrmManager, ByVal oldObj As ICachedEntity) Implements _ICachedEntity.UpdateCache
            Dim c As OrmCache = TryCast(mc.Cache, OrmCache)
            If c IsNot Nothing Then
                c.UpdateCache(mc.MappingEngine, New Pair(Of _ICachedEntity)() {New Pair(Of _ICachedEntity)(Me, CType(oldObj, _ICachedEntity))}, mc, AddressOf ClearCacheFlags, Nothing, Nothing, False, _upd.UpdatedFields IsNot Nothing)
            End If
            UpdateCacheAfterUpdate(c)
            For Each el As EditableListBase In New List(Of EditableListBase)(_upd.Relations)
                If c IsNot Nothing Then
                    c.RemoveM2MQueries(el)
                End If
                _upd.Relations.Remove(el)
            Next
        End Sub

        Private ReadOnly Property IsReadOnly(ByVal mgr As OrmManager) As Boolean
            Get
                Using SyncHelper(True)
                    If ObjectState = Entities.ObjectState.Modified Then
                        'Using mc As IGetManager = GetMgr()
                        Dim mo As ObjectModification = mgr.Cache.ShadowCopy(Me)
                        'If mo Is Nothing Then mo = _mo
                        If mo IsNot Nothing Then
                            If mo.User IsNot Nothing AndAlso Not mo.User.Equals(mgr.CurrentUser) Then
                                Return True
                            End If

                        End If
                        'End Using
                    End If
                    Return False
                End Using
            End Get
        End Property

        Public Overridable Sub RejectRelationChanges(ByVal mc As OrmManager) Implements ICachedEntity.RejectRelationChanges

        End Sub

        ''' <summary>
        ''' Отмена изменений
        ''' </summary>
        Public Sub RejectChanges() Implements ICachedEntity.RejectChanges
            Using gmc As IGetManager = GetMgr()
                _RejectChanges(gmc.Manager)
            End Using
        End Sub

        Protected Sub _RejectChanges(ByVal mgr As OrmManager) Implements _ICachedEntity.RejectChanges
            Using SyncHelper(False)
                RejectRelationChanges(mgr)

                If ObjectState = ObjectState.Modified OrElse ObjectState = Entities.ObjectState.Deleted OrElse ObjectState = Entities.ObjectState.Created Then
                    If IsReadOnly(mgr) Then
                        Throw New OrmObjectException(ObjName & " object in readonly state")
                    End If

                    Dim oc As ICachedEntity = OriginalCopy()
                    If ObjectState <> Entities.ObjectState.Deleted Then
                        If oc Is Nothing Then
                            If ObjectState <> Entities.ObjectState.Created Then
                                Throw New OrmObjectException(ObjName & ": When object is in modified state it has to have an original copy")
                            End If
                            Return
                        End If
                    End If

                    Dim mo As ObjectModification = mgr.Cache.ShadowCopy(Me)
                    If ObjectState = Entities.ObjectState.Deleted AndAlso mo.Reason <> ObjectModification.ReasonEnum.Delete Then
                        'Debug.Assert(False)
                        'Throw New OrmObjectException
                        Return
                    End If

                    If ObjectState = Entities.ObjectState.Modified AndAlso (mo.Reason = ObjectModification.ReasonEnum.Delete) Then
                        Debug.Assert(False)
                        Throw New OrmObjectException
                    End If

                    'Debug.WriteLine(Environment.StackTrace)
                    '_needAdd = False
                    '_needDelete = False
                    _upd = New UpdateCtx

                    Dim olds As ObjectState = Entities.ObjectState.None
                    If oc IsNot Nothing Then
                        olds = oc.GetOldState
                    End If

                    Dim oldkey As Integer?
                    If IsPKLoaded Then
                        oldkey = Key
                    End If

                    Dim newid() As PKDesc = Nothing
                    If oc IsNot Nothing Then
                        newid = oc.GetPKValues()
                    End If

                    If olds <> Entities.ObjectState.Created Then
                        '_loaded_members = 
                        RevertToOriginalVersion()
                        RemoveVersionData(mgr.Cache, False)
                    End If

                    If newid IsNot Nothing Then
                        SetPK(newid)
                    End If

#If TraceSetState Then
                    SetObjectState(olds, mo.Reason, mo.StackTrace, mo.DateTime)
#Else
                    SetObjectStateClear(olds)
#End If
                    If ObjectState = Entities.ObjectState.Created Then
                        If oldkey.HasValue Then
                            'Using gmc As IGetManager = GetMgr()
                            'Dim mc As OrmManager = gmc.Manager
                            Dim dic As IDictionary = mgr.GetDictionary(Me.GetType)
                            If dic Is Nothing Then
                                Dim name As String = Me.GetType.Name
                                Throw New OrmObjectException("Collection for " & name & " not exists")
                            End If

                            dic.Remove(oldkey)
                        End If
                        ' End Using

                        mgr.Cache.UnregisterModification(Me)
                        _copy = Nothing
                        _loaded = False
                        '_loaded_members = New BitArray(_loaded_members.Count)
                    End If

                    'ElseIf state = Obm.ObjectState.Deleted Then
                    '    CheckCash()
                    '    using SyncHelper(false)
                    '        state = ObjectState.None
                    '        OrmCache.UnregisterModification(Me)
                    '    End SyncLock
                    'Else
                    '    Throw New OrmObjectException(ObjName & "Rejecting changes in the state " & _state.ToString & " is not allowed")
                End If
                Invariant()
            End Using
        End Sub

        Protected Sub RevertToOriginalVersion()
            Dim original As ICachedEntity = OriginalCopy
            If original IsNot Nothing Then
                CopyBody(original, Me)
            End If
        End Sub

        Protected Overridable Sub ValidateNewObject(ByVal mgr As OrmManager) Implements _ICachedEntityEx.ValidateNewObject
            'Return True
        End Sub

        Protected Overridable Sub ValidateUpdate(ByVal mgr As OrmManager) Implements _ICachedEntityEx.ValidateUpdate
            'Return True
        End Sub

        Protected Overridable Sub ValidateDelete(ByVal mgr As OrmManager) Implements _ICachedEntityEx.ValidateDelete
            'Return True
        End Sub

        Protected Sub RaiseSaved(ByVal sa As OrmManager.SaveAction) Implements _ICachedEntity.RaiseSaved
            RaiseEvent Saved(Me, New ObjectSavedArgs(sa))
        End Sub

        Protected Function Save(ByVal mc As OrmManager) As Boolean Implements _ICachedEntity.Save
            If IsReadOnly(mc) Then
                Throw New OrmObjectException(ObjName & "Object in readonly state")
            End If

            Dim r As Boolean = True
            If ObjectState = Entities.ObjectState.Created OrElse ObjectState = Entities.ObjectState.NotFoundInSource Then
                If IsPKLoaded AndAlso OriginalCopy() IsNot Nothing Then
                    Throw New OrmObjectException(ObjName & " already exists.")
                End If
                Dim o As ICachedEntity = mc.AddObject(Me)
                If o Is Nothing Then
                    r = False
                Else
                    Debug.Assert(ObjectState = Entities.ObjectState.Modified) ' OrElse _state = Orm.ObjectState.None
                    _upd.Added = True
                End If
            ElseIf ObjectState = Entities.ObjectState.Deleted Then
#If TraceSetState Then
                Dim mo As ModifiedObject = mc.Cache.Modified(Me)
                If mo Is Nothing OrElse (mo.Reason <> ModifiedObject.ReasonEnum.Delete AndAlso mo.Reason <> ModifiedObject.ReasonEnum.Edit) Then
                    Debug.Assert(False)
                    Throw New OrmObjectException
                End If
#End If
                mc.DeleteObject(Me)
                _upd.Deleted = True
            ElseIf (ObjectState = Entities.ObjectState.Modified OrElse Not WrappedProperties) Then
#If TraceSetState Then
                Dim mo As ObjectModification = mc.Cache.ShadowCopy(Me)
                If mo Is Nothing OrElse mo.Reason = ObjectModification.ReasonEnum.Delete Then
                    Debug.Assert(False)
                    Throw New OrmObjectException
                End If
#End If
                r = mc.UpdateObject(Me)
#If TraceSetState Then
            Else
                Debug.Assert(False)
#End If
            End If
            Return r
        End Function

        Protected Shared Function _Delete(ByVal mgr As OrmManager, ByVal obj As CachedEntity) As Boolean
            Using obj.SyncHelper(False)
                If obj.ObjectState = Entities.ObjectState.Deleted Then Return False

                If obj.ObjectState = Entities.ObjectState.Clone Then
                    Throw New OrmObjectException(obj.ObjName & "Deleting clone is not allowed")
                End If
                If obj.ObjectState <> Entities.ObjectState.Modified AndAlso obj.ObjectState <> Entities.ObjectState.None AndAlso obj.ObjectState <> Entities.ObjectState.NotLoaded Then
                    Throw New OrmObjectException(obj.ObjName & "Deleting is not allowed for this object")
                End If

                Dim mo As ObjectModification = mgr.Cache.ShadowCopy(obj)
                'If mo Is Nothing Then mo = _mo
                If mo IsNot Nothing Then
                    'Using mc As IGetManager = obj.GetMgr()
                    If mo.User IsNot Nothing AndAlso Not mo.User.Equals(mgr.CurrentUser) Then
                        Throw New OrmObjectException(obj.ObjName & "Object has already altered by user " & mo.User.ToString)
                    End If
                    'End Using
                    Debug.Assert(mo.Reason <> ObjectModification.ReasonEnum.Delete)
                Else
                    If obj.ObjectState = Entities.ObjectState.NotLoaded Then
                        obj.Load(mgr)
                        If obj.ObjectState = Entities.ObjectState.NotFoundInSource Then
                            Return False
                        End If
                    End If

                    Debug.Assert(obj.ObjectState <> Entities.ObjectState.Modified)
                    obj.CreateClone4Delete(mgr)
                    'OrmCache.Modified(Me).Reason = ModifiedObject.ReasonEnum.Delete
                    'Dim modified As OrmBase = CloneMe()
                    'modified._old_state = modified.ObjectState
                    'modified.ObjectState = ObjectState.Clone
                    'OrmCache.RegisterModification(modified)
                End If
                'obj.SetObjectState(ObjectState.Deleted)

                'Using mc As IGetManager = obj.GetMgr()
                'mgr.RaiseBeginDelete(obj)
                'End Using
            End Using

            Return True
        End Function

        Public Overridable Function Delete() As Boolean Implements ICachedEntity.Delete
            Using mc As IGetManager = GetMgr()
                Return _Delete(mc.Manager, EnsureInCache(mc.Manager))
            End Using
        End Function

        Public Function EnsureLoaded() As CachedEntity
            'OrmManager.CurrentMediaContent.LoadObject(Me)
            Using mc As IGetManager = GetMgr()
                Return CType(mc.Manager.GetLoadedObjectFromCacheOrDB(Me, mc.Manager.GetDictionary(Me.GetType)), CachedEntity)
            End Using
        End Function

        Protected ReadOnly Property CanEdit() As Boolean
            Get
                If ObjectState = Entities.ObjectState.Deleted Then 'OrElse _state = Orm.ObjectState.NotFoundInSource Then
                    Return False
                End If
                If ObjectState = Entities.ObjectState.NotLoaded Then
                    Load()
                End If
                Return ObjectState <> Entities.ObjectState.NotFoundInSource
            End Get
        End Property

        Protected ReadOnly Property CanLoad() As Boolean
            Get
                Return ObjectState <> Entities.ObjectState.Deleted AndAlso ObjectState <> Entities.ObjectState.Modified
            End Get
        End Property

        Private ReadOnly Property UpdateCtx() As UpdateCtx Implements _ICachedEntity.UpdateCtx
            Get
                Return _upd
            End Get
        End Property

        Protected Friend Sub UpdateCacheAfterUpdate(ByVal c As OrmCache)
            If _upd.UpdatedFields IsNot Nothing Then
                If c IsNot Nothing Then
                    Dim l As List(Of String) = New List(Of String)
                    For Each f As Criteria.Core.EntityFilter In _upd.UpdatedFields
                        '    Assert(f.Type Is t, "")

                        '    Cache.AddUpdatedFields(obj, f.FieldName)
                        l.Add(f.Template.PropertyAlias)
                    Next

                    c.AddUpdatedFields(Me, l)
                End If
                _upd.UpdatedFields = Nothing
            End If
        End Sub

        Protected Overridable Function ForseUpdate(ByVal c As ColumnAttribute) As Boolean Implements _ICachedEntity.ForseUpdate
            Return False
        End Function

        Protected Function SortedColumnAttributeCount() As Integer
            Dim schema As ObjectMappingEngine = MappingEngine
            If schema Is Nothing Then
                If _attList IsNot Nothing Then
                    Return _attList.Count
                Else
                    Return ObjectMappingEngine.GetMappedProperties(Me.GetType, Nothing).Count
                End If
            Else
                Return schema.GetSortedFieldList(Me.GetType).Count
            End If
        End Function

        Private _attList As List(Of ColumnAttribute)
        Protected Function SortedColumnAttributeList() As List(Of ColumnAttribute)
            Dim schema As ObjectMappingEngine = MappingEngine
            If schema Is Nothing Then
                If _attList Is Nothing Then
                    Dim l As New List(Of ColumnAttribute)
                    For Each cl As ColumnAttribute In ObjectMappingEngine.GetMappedProperties(Me.GetType, Nothing).Keys
                        Dim idx As Integer = l.BinarySearch(cl)
                        If idx < 0 Then
                            l.Insert(Not idx, cl)
                        Else
                            Throw New InvalidOperationException
                        End If
                    Next
                    _attList = l
                End If
                Return _attList
            Else
                Return schema.GetSortedFieldList(Me.GetType)
            End If
        End Function

        Protected Overrides Function IsPropertyLoaded(ByVal propertyAlias As String) As Boolean
            Dim c As New ColumnAttribute(propertyAlias)
            Dim arr As Generic.List(Of ColumnAttribute) = SortedColumnAttributeList()
            Dim idx As Integer = arr.BinarySearch(c)
            If idx < 0 Then Throw New OrmObjectException("There is no such field " & propertyAlias)
            Return _members_load_state(idx)
        End Function

        Protected Overrides Sub PrepareRead(ByVal propertyAlias As String, ByRef d As System.IDisposable)
            If Not _readRaw AndAlso (Not IsLoaded AndAlso (ObjectState = Entities.ObjectState.NotLoaded OrElse ObjectState = Entities.ObjectState.None)) Then
                d = SyncHelper(True)
                If Not IsLoaded AndAlso (ObjectState = Entities.ObjectState.NotLoaded OrElse ObjectState = Entities.ObjectState.None) AndAlso Not IsPropertyLoaded(propertyAlias) Then
                    Load()
                End If
            End If
        End Sub

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, CachedEntity))
        End Function

        Public Overloads Function Equals(ByVal obj As CachedEntity) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Dim pks() As PKDesc = GetPKValues()
            Dim pks2() As PKDesc = obj.GetPKValues()
            For i As Integer = 0 To pks.Length - 1
                Dim pk As PKDesc = pks(i)
                If pk.PropertyAlias <> pks2(i).PropertyAlias OrElse Not pk.Value.Equals(pks2(i).Value) Then
                    Return False
                End If
            Next
            Return True
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return GetCacheKey()
        End Function

        Protected Function GetM2MRelatedChangedObjects() As List(Of CachedEntity)
            Dim l As New List(Of CachedEntity)
            'Using mc As IGetManager = GetMgr()
            '    For Each o As Pair(Of M2MCache, Pair(Of String, String)) In mc.Manager.Cache.GetM2MEntries(Me, Nothing)
            '        Dim s As IListObjectConverter.ExtractListResult
            '        For Each obj As CachedEntity In o.First.GetObjectListNonGeneric(mc.Manager, False, False, s)
            '            If obj.HasChanges Then
            '                l.Add(obj)
            '            End If
            '        Next
            '    Next
            'End Using
            Return l
        End Function

        Protected Overridable Function GetRelatedChangedObjects() As List(Of CachedEntity)
            Dim l As New List(Of CachedEntity)
            For Each kv As DictionaryEntry In MappingEngine.GetProperties(Me.GetType)
                Dim pi As Reflection.PropertyInfo = CType(kv.Value, Reflection.PropertyInfo)
                If GetType(ICachedEntity).IsAssignableFrom(pi.PropertyType) Then
                    Dim o As CachedEntity = CType(ObjectMappingEngine.GetPropertyValue(Me, CType(kv.Key, ColumnAttribute).PropertyAlias, pi, Nothing), CachedEntity)
                    If o IsNot Nothing AndAlso o.HasChanges Then
                        l.Add(o)
                    End If
                End If
            Next
            Return l
        End Function

        Protected Friend Function GetChangedObjectGraph() As List(Of CachedEntity)
            Dim l As New List(Of CachedEntity)
            GetChangedObjectGraph(l)
            Return l
        End Function

        Protected Friend Sub GetChangedObjectGraph(ByVal gl As List(Of CachedEntity))
            Dim l As New List(Of CachedEntity)

            For Each o As CachedEntity In GetRelatedChangedObjects()
                If Not gl.Contains(o) Then
                    gl.Add(o)
                    l.Add(o)
                End If
            Next

            For Each o As CachedEntity In GetM2MRelatedChangedObjects()
                If Not gl.Contains(o) Then
                    gl.Add(o)
                    l.Add(o)
                End If
            Next

            For Each o As CachedEntity In l
                o.GetChangedObjectGraph(gl)
            Next
        End Sub

        Protected Friend Function GetChangedObjectGraphWithSelf() As List(Of CachedEntity)
            Dim l As List(Of CachedEntity) = GetChangedObjectGraph()
            If HasChanges AndAlso Not l.Contains(Me) Then
                l.Add(Me)
            End If
            Return l
        End Function
    End Class

End Namespace
