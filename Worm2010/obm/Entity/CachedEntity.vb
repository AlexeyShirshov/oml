Option Infer On

Imports Worm.Entities.Meta
Imports Worm.Cache
Imports System.ComponentModel
Imports System.Collections.Generic
Imports System.Xml
Imports Worm.Query
Imports System.Linq

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
    Public Class CachedEntity
        Inherits Entity
        Implements _ICachedEntityEx

        Protected _key As Integer

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
        '<NonSerialized()> _
        'Private _copy As ICachedEntity
        <NonSerialized()> _
        Private _props As IDictionary

        '<EditorBrowsable(EditorBrowsableState.Never)> _
        'Public Class AcceptState

        'End Class

        Public Class RelatedObject
            Private _dst As CachedEntity
            Private _srcProps() As String
            Private _dstProps() As String

            Public Sub New(ByVal src As CachedEntity, ByVal properties() As String, _
                           ByVal dst As CachedEntity, ByVal dstProps() As String)
                _dst = dst
                _srcProps = properties
                _dstProps = dstProps
                AddHandler src.Saved, AddressOf Added
            End Sub

            Public Sub Added(ByVal source As ICachedEntity, ByVal args As ObjectSavedArgs)
                Using mc As IGetManager = source.GetMgr
                    Dim mgr As OrmManager = mc.Manager
                    Dim dt As Type = _dst.GetType
                    Dim mpe As ObjectMappingEngine = mgr.MappingEngine
                    Dim oschema As IEntitySchema = mpe.GetEntitySchema(dt)
                    Dim pk As Boolean, pk_old As IEnumerable(Of PKDesc) = OrmManager.GetPKValues(_dst, oschema)
                    For i As Integer = 0 To _srcProps.Length - 1
                        Dim srcProp As String = _srcProps(i)
                        Dim dstProp As String = _dstProps(i)

                        ObjectMappingEngine.SetPropertyValue(_dst, dstProp, ObjectMappingEngine.GetPropertyValue(source, srcProp, oschema, Nothing), oschema, Nothing)

                        If oschema.FieldColumnMap(dstProp).IsPK Then
                            pk = True
                        End If

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

            Public Sub New(ByVal o As CachedEntity)
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
                '                Dim arr As Generic.List(Of EntityPropertyAttribute) = mc.Manager.ObjectSchema.GetSortedFieldList(_o.GetType)
                '                For i As Integer = 0 To arr.Count - 1
                '                    _o._members_load_state(i) = True
                '                Next
                '            End Using
                '        ElseIf Not value AndAlso _o._loaded Then
                '            Using mc As IGetManager = GetMgr()
                '                Dim arr As Generic.List(Of EntityPropertyAttribute) = mc.Manager.ObjectSchema.GetSortedFieldList(_o.GetType)
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
                'Dim ll As IPropertyLazyLoad = TryCast(_o, IPropertyLazyLoad)
                'If ll IsNot Nothing Then
                'Using mc As IGetManager = _o.GetMgr
                Dim mpe As ObjectMappingEngine = _o.GetMappingEngine
                Dim map As Collections.IndexedCollection(Of String, MapField2Column) = _o.GetEntitySchema(mpe).FieldColumnMap
                Return OrmManager.IsPropertyLoaded(_o, propertyAlias, map, mpe)
                'End Using
                'End If
                'Return True
            End Function

            Public Function GetMappingEngine() As ObjectMappingEngine
                Return _o.GetMappingEngine
            End Function

            Public ReadOnly Property ObjectState() As ObjectState
                Get
                    Return _o.ObjectState
                End Get
            End Property

            Public ReadOnly Property Obj() As CachedEntity
                Get
                    Return _o
                End Get
            End Property

            Public ReadOnly Property OriginalCopy() As ICachedEntity
                Get
                    Dim uc As IUndoChanges = TryCast(_o, IUndoChanges)
                    If uc IsNot Nothing Then
                        Return uc.OriginalCopy
                    End If
                    Return Nothing
                End Get
            End Property

            'Public ReadOnly Property IsReadOnly() As Boolean
            '    Get
            '        Using gm As IGetManager = GetMgr
            '            Return _o.IsReadOnly(gm.Manager)
            '        End Using
            '        'Using _o.SyncHelper(True)
            '        '    If _o._state = Orm.ObjectState.Modified Then
            '        '        _o.CheckCash()
            '        '        Dim mo As ModifiedObject = OrmCache.Modified(_o)
            '        '        'If mo Is Nothing Then mo = _mo
            '        '        If mo IsNot Nothing Then
            '        '            Using mc As IGetManager = GetMgr()
            '        '                If mo.User IsNot Nothing AndAlso Not mo.User.Equals(mc.Manager.CurrentUser) Then
            '        '                    Return True
            '        '                End If
            '        '            End Using
            '        '        End If
            '        '        'ElseIf state = Obm.ObjectState.Deleted Then
            '        '        'Return True
            '        '    End If
            '        '    Return False
            '        'End Using
            '    End Get
            'End Property

            'Public ReadOnly Property Changes(ByVal obj As ICachedEntity) As String()
            '    Get
            '        Return _o.Changes(obj)
            '        'Dim columns As New Generic.List(Of EntityPropertyAttribute)
            '        'Dim t As Type = obj.GetType
            '        'For Each pi As Reflection.PropertyInfo In _o.GetType.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic)
            '        '    Dim c As EntityPropertyAttribute = CType(Attribute.GetCustomAttribute(pi, GetType(EntityPropertyAttribute), True), EntityPropertyAttribute)
            '        '    If c IsNot Nothing Then
            '        '        Dim original As Object = pi.GetValue(obj, Nothing)
            '        '        If (_o.OrmSchema.GetAttributes(t, c) And Field2DbRelations.ReadOnly) <> Field2DbRelations.ReadOnly Then
            '        '            Dim current As Object = pi.GetValue(_o, Nothing)
            '        '            If (original IsNot Nothing AndAlso Not original.Equals(current)) OrElse _
            '        '                (current IsNot Nothing AndAlso Not current.Equals(original)) Then
            '        '                columns.Add(c)
            '        '            End If
            '        '        End If
            '        '    End If
            '        'Next
            '        'Return columns.ToArray
            '    End Get
            'End Property

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

            'Public Overridable ReadOnly Property ChangeDescription() As String
            '    Get
            '        Return _o.ChangeDescription
            '        'Dim sb As New StringBuilder
            '        'sb.Append("Аттрибуты:").Append(vbCrLf)
            '        'If ObjectState = Orm.ObjectState.Modified Then
            '        '    For Each c As EntityPropertyAttribute In Changes(OriginalCopy)
            '        '        sb.Append(vbTab).Append(c.FieldName).Append(vbCrLf)
            '        '    Next
            '        'Else
            '        '    Dim t As Type = _o.GetType
            '        '    'Dim o As OrmBase = CType(t.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, _
            '        '    '    New Object() {Identifier, OrmCache, _schema}), OrmBase)
            '        '    'Dim o As OrmBase = GetNew()
            '        '    Dim o As OrmBase = CType(Activator.CreateInstance(t), OrmBase)
            '        '    o.Init(_o.Identifier, _o.OrmCache, _o.OrmSchema)
            '        '    For Each c As EntityPropertyAttribute In Changes(o)
            '        '        sb.Append(vbTab).Append(c.FieldName).Append(vbCrLf)
            '        '    Next
            '        'End If
            '        'Return sb.ToString
            '    End Get
            'End Property

            Public ReadOnly Property HasChanges() As Boolean
                Get
                    Return OrmManager.HasChanges(_o)
                End Get
            End Property

            Public Function GetM2MRelatedChangedObjects() As List(Of ICachedEntity)
                Return _o.GetChildChangedObjects
            End Function

            Public Function GetRelatedChangedObjects() As List(Of ICachedEntity)
                Return _o.GetParentChangedObjects
            End Function

            Public Function GetChangedObjectGraph() As List(Of _ICachedEntity)
                Return _o.GetChangedObjectList
            End Function

            Public Function GetChangedObjectGraphWithSelf() As List(Of _ICachedEntity)
                Return _o.GetChangedObjectListWithSelf
            End Function

            Public Sub SetCreateManager(ByVal createManager As ICreateManager)
                _o.SetCreateManager(createManager)
            End Sub
        End Class

        Public Event Saved(ByVal sender As ICachedEntity, ByVal args As ObjectSavedArgs) Implements ICachedEntity.Saved
        Public Event Added(ByVal sender As ICachedEntity, ByVal args As EventArgs) Implements ICachedEntity.Added
        Public Event Deleted(ByVal sender As ICachedEntity, ByVal args As EventArgs) Implements ICachedEntity.Deleted
        Public Event Updated(ByVal sender As ICachedEntity, ByVal args As EventArgs) Implements ICachedEntity.Updated
        Public Event ChangesAccepted(ByVal sender As ICachedEntity, ByVal args As EventArgs) Implements ICachedEntity.ChangesAccepted

        Protected ReadOnly Property Key() As Integer Implements IKeyProvider.Key
            Get
                If Not IsPKLoaded Then Throw New OrmObjectException(String.Format("Entity of type {0} has no primary key", Me.GetType))
                Return _key
            End Get
        End Property

        Protected Overridable Function GetCacheKey() As Integer
            Dim r As Integer
            For Each pk As PKDesc In OrmManager.GetPKValues(Me, Nothing)
                r = r Xor pk.Value.GetHashCode
            Next
            Return r
        End Function

        Protected Overrides Sub Init(ByVal cache As Cache.CacheBase, ByVal schema As ObjectMappingEngine)
            Throw New NotSupportedException
        End Sub

        Protected Overridable Sub PKLoaded(ByVal pkCount As Integer) Implements _ICachedEntityEx.PKLoaded
            _key = GetCacheKey()
            _hasPK = True
        End Sub

        'Private Function CheckIsAllLoaded(ByVal mpe As ObjectMappingEngine, _
        '    ByVal loadedColumns As Integer, ByVal map As Collections.IndexedCollection(Of String, MapField2Column)) As Boolean Implements _ICachedEntity.CheckIsAllLoaded
        '    Using SyncHelper(False)
        '        Dim allloaded As Boolean = True
        '        If Not _loaded OrElse _loaded_members.Count <= loadedColumns Then
        '            For i As Integer = 0 To map.Count - 1
        '                If Not _members_load_state(i, map, mpe) Then
        '                    'Dim at As Field2DbRelations = schema.GetAttributes(Me.GetType, arr(i))
        '                    'If (at And Field2DbRelations.PK) <> Field2DbRelations.PK Then
        '                    allloaded = False
        '                    Exit For
        '                    'End If
        '                End If
        '            Next
        '            _loaded = allloaded
        '        End If
        '        Return allloaded
        '    End Using
        'End Function

        'Private Sub InitLoadState(ByVal map As Collections.IndexedCollection(Of String, MapField2Column), ByVal mpe As ObjectMappingEngine)
        '    If _loaded_members Is Nothing OrElse _sver <> If(mpe Is Nothing, "w-x", mpe.Version) Then
        '        _loaded_members = New BitArray(map.Count)
        '        _sver = If(mpe Is Nothing, "w-x", mpe.Version)
        '        If IsPKLoaded Then
        '            For i As Integer = 0 To map.Count - 1
        '                If Not _loaded_members(i) Then
        '                    If map(i).IsPK Then
        '                        _loaded_members(i) = True
        '                    End If
        '                End If
        '            Next
        '        End If
        '    End If
        'End Sub

        'Protected Property _members_load_state(ByVal idx As Integer, _
        '    ByVal map As Collections.IndexedCollection(Of String, MapField2Column), ByVal mpe As ObjectMappingEngine) As Boolean
        '    Get
        '        InitLoadState(map, mpe)
        '        Return _loaded_members(idx)
        '    End Get
        '    Set(ByVal value As Boolean)
        '        InitLoadState(map, mpe)
        '        _loaded_members(idx) = value
        '    End Set
        'End Property

        'Private Function SetLoaded(ByVal propertyAlias As String, ByVal loaded As Boolean, _
        '    ByVal map As Collections.IndexedCollection(Of String, MapField2Column), ByVal mpe As ObjectMappingEngine) As Boolean Implements _ICachedEntity.SetLoaded

        '    Dim idx As Integer = map.IndexOf(propertyAlias)
        '    If idx < 0 Then
        '        'Throw New OrmObjectException(String.Format("There is no property in type {0} with alias {1}", Me.GetType, propertyAlias))
        '        Return False
        '    End If
        '    Dim old As Boolean = _members_load_state(idx, map, mpe)
        '    _members_load_state(idx, map, mpe) = loaded
        '    Return old
        'End Function

        'Private Function SetLoaded(ByVal c As EntityPropertyAttribute, ByVal loaded As Boolean, ByVal check As Boolean, ByVal mpe As ObjectMappingEngine) As Boolean Implements _ICachedEntity.SetLoaded

        '    Dim idx As Integer = c.Index
        '    Dim cnt As Integer
        '    If idx = -1 OrElse (mpe IsNot Nothing AndAlso mpe.Version <> c.SchemaVersion) Then
        '        Dim arr As Generic.List(Of EntityPropertyAttribute) = SortedColumnAttributeList(mpe)
        '        idx = arr.BinarySearch(c)
        '        cnt = arr.Count
        '        c.Index = idx
        '        If mpe IsNot Nothing Then c.SchemaVersion = mpe.Version
        '    End If

        '    If idx < 0 AndAlso check Then Throw New OrmObjectException(String.Format("There is no property in type {0} with alias {1}", Me.GetType, c.PropertyAlias))

        '    If idx >= 0 Then
        '        Dim old As Boolean = _members_load_state(idx, cnt, mpe)
        '        _members_load_state(idx, cnt, mpe) = loaded
        '        Return old
        '        'End Using
        '    End If
        'End Function

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

        'Protected Overridable Sub RemoveOriginalCopy(ByVal cache As CacheBase) Implements ICachedEntity.RemoveOriginalCopy
        '    _copy = Nothing
        'End Sub

        Public ReadOnly Property InternalProperties() As InternalClass
            Get
                Return New InternalClass(Me)
            End Get
        End Property

        Public Sub Load()
            Load(CStr(Nothing))
        End Sub

        Public Sub Load(ByVal propertyAlias As String)
            Using mc As IGetManager = GetMgr()
                If mc Is Nothing Then
                    Throw New InvalidOperationException("OrmManager required")
                End If

                mc.Manager.Load(Me, propertyAlias)
            End Using
        End Sub

        'Public Sub Load(ByVal mgr As OrmManager, Optional ByVal propertyAlias As String = Nothing) Implements _ICachedEntity.Load
        '    Throw New NotSupportedException
        'End Sub
        '        Public Sub Load(ByVal mgr As OrmManager, Optional ByVal propertyAlias As String = Nothing) Implements _ICachedEntity.Load
        '            Dim oschema As IEntitySchema = GetEntitySchema(mgr.MappingEngine)
        '            Dim mo As ObjectModification = mgr.Cache.ShadowCopy(Me.GetType, Me, TryCast(oschema, ICacheBehavior))
        '            'If mo Is Nothing Then mo = _mo
        '            If mo IsNot Nothing Then
        '                If mo.User IsNot Nothing Then
        '                    'Using mc As IGetManager = GetMgr()
        '                    If Not mo.User.Equals(mgr.CurrentUser) Then
        '                        Throw New OrmObjectException(ObjName & "Object in readonly state")
        '                    End If
        '                    'End Using
        '                Else
        '                    If ObjectState = Entities.ObjectState.Deleted OrElse ObjectState = Entities.ObjectState.Modified Then
        '                        Throw New OrmObjectException(ObjName & "Cannot load object while its state is deleted or modified!")
        '                    End If
        '                End If
        '            End If
        '            Dim olds As ObjectState = ObjectState
        '            Dim robj As CachedEntity = CType(mgr.NormalizeObject(Me, mgr.GetDictionary(Me.GetType), False, False, oschema), CachedEntity)
        '            If robj IsNot Nothing AndAlso Not ReferenceEquals(robj, Me) Then
        '                If String.IsNullOrEmpty(propertyAlias) Then
        '                    If robj.IsLoaded Then
        'l1:
        '                        CopyBody(robj, Me)
        '                        Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap
        '                        For Each m As MapField2Column In map
        '                            SetLoaded(m.PropertyAlias, True, map, mgr.MappingEngine)
        '                        Next
        '                        CheckIsAllLoaded(mgr.MappingEngine, map.Count, map)
        '                    Else
        '                        mgr.LoadObject(Me, propertyAlias)
        '                        GoTo l1
        '                    End If
        '                ElseIf robj.IsPropertyLoaded(propertyAlias) Then
        '                    Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap
        '                    Dim m As MapField2Column = Nothing
        '                    map.TryGetValue(propertyAlias, m)
        '                    ObjectMappingEngine.SetPropertyValue(Me, propertyAlias, ObjectMappingEngine.GetPropertyValue(robj, propertyAlias, oschema, m.PropertyInfo), oschema, m.PropertyInfo)
        '                    SetLoaded(propertyAlias, True, map, mgr.MappingEngine)
        '                Else
        '                    mgr.LoadObject(Me, propertyAlias)
        '                    GoTo l1
        '                End If
        '            Else
        '                mgr.LoadObject(Me, propertyAlias)
        '            End If

        '            If olds = Entities.ObjectState.Created AndAlso ObjectState = Entities.ObjectState.Modified Then
        '                AcceptChanges(True, True)
        '            ElseIf IsLoaded Then
        '                SetObjectState(Entities.ObjectState.None)
        '            End If
        '            Invariant(mgr)
        '        End Sub

        '<EditorBrowsable(EditorBrowsableState.Never)> _
        '<Conditional("DEBUG")> _
        'Public Sub Invariant(ByVal mgr As OrmManager)
        '    Using SyncHelper(True)
        '        If IsLoaded AndAlso _
        '            ObjectState <> Entities.ObjectState.None AndAlso ObjectState <> Entities.ObjectState.Modified AndAlso ObjectState <> Entities.ObjectState.Deleted Then Throw New OrmObjectException(ObjName & "When object is loaded its state has to be None or Modified or Deleted: current state is " & ObjectState.ToString)
        '        If Not IsLoaded AndAlso _
        '           (ObjectState = Entities.ObjectState.None OrElse ObjectState = Entities.ObjectState.Modified OrElse ObjectState = Entities.ObjectState.Deleted) Then Throw New OrmObjectException(ObjName & "When object is not loaded its state has not be None or Modified or Deleted: current state is " & ObjectState.ToString)
        '        If ObjectState = Entities.ObjectState.Modified AndAlso mgr.Cache.ShadowCopy(Me.GetType, Me, TryCast(GetEntitySchema(mgr.MappingEngine), ICacheBehavior)) Is Nothing Then
        '            'Throw New OrmObjectException(ObjName & "When object is in modified state it has to have an original copy")
        '            SetObjectStateClear(Entities.ObjectState.None)
        '            Load()
        '        End If
        '    End Using
        'End Sub

        Protected Overrides Sub SetObjectState(ByVal value As ObjectState)
            Using SyncHelper(False)
                'Debug.Assert(value <> Entities.ObjectState.None OrElse IsLoaded, String.Format("Cannot set state none while object {0} is not loaded", ObjName))
                If value = Entities.ObjectState.None AndAlso Not IsLoaded Then
                    Throw New OrmObjectException(String.Format("Cannot set state none while object {0} is not loaded", ObjName))
                End If

                'Debug.Assert(Not _upd.Deleted, String.Format("Cannot set state while object {0} is in the middle of saving changes", ObjName))
                If _upd.Deleted Then
                    Throw New OrmObjectException(String.Format("Cannot set state while object {0} is in the middle of saving changes", ObjName))
                End If

                MyBase.SetObjectState(value)
            End Using
        End Sub

        'Private Sub SetLoaded(ByVal value As Boolean) Implements _ICachedEntity.SetLoaded
        '    Using SyncHelper(False)
        '        Dim mpe As ObjectMappingEngine = GetMappingEngine()
        '        'If mpe Is Nothing Then
        '        '    Throw New OrmObjectException(String.Format("Cannot get MappingEngine for type {0}", Me.GetType))
        '        'End If
        '        Dim oschema As IEntitySchema = GetEntitySchema(mpe)

        '        Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap

        '        If value AndAlso Not _loaded Then
        '            For i As Integer = 0 To map.Count - 1
        '                _members_load_state(i, map, mpe) = True
        '            Next
        '        ElseIf Not value AndAlso _loaded Then
        '            For i As Integer = 0 To map.Count - 1
        '                _members_load_state(i, map, mpe) = False
        '            Next
        '        End If
        '        _loaded = value
        '        Debug.Assert(_loaded = value)
        '    End Using
        'End Sub

        'Public Sub AcceptChanges()
        '    AcceptChanges(True, IsGoodState(ObjectState))
        'End Sub

        'Public ReadOnly Property IsPropertiesLazyLoad() As Boolean
        '    Get
        '        Return GetType(IPropertyLazyLoad).IsAssignableFrom(Me.GetType)
        '    End Get
        'End Property

        'Public Function AcceptChanges(ByVal updateCache As Boolean, ByVal setState As Boolean) As ICachedEntity Implements ICachedEntity.AcceptChanges
        '    Dim mo As _ICachedEntity = Nothing
        '    Using SyncHelper(False)
        '        If ObjectState = Entities.ObjectState.Created OrElse ObjectState = Entities.ObjectState.Clone Then 'OrElse _state = Orm.ObjectState.NotLoaded Then
        '            Throw New OrmObjectException(ObjName & "accepting changes allowed in state Modified, deleted or none")
        '        End If

        '        Using gmc As IGetManager = GetMgr()
        '            Dim mc As OrmManager = gmc.Manager
        '            '_valProcs = HasM2MChanges(mc)

        '            AcceptRelationalChanges(updateCache, mc)

        '            If (ObjectState <> Entities.ObjectState.None OrElse Not IsPropertiesLazyLoad) Then
        '                mo = RemoveVersionData(mc.Cache, mc.MappingEngine, setState)
        '                Dim c As OrmCache = TryCast(mc.Cache, OrmCache)
        '                If _upd.Deleted Then
        '                    '_valProcs = False
        '                    If updateCache AndAlso c IsNot Nothing Then
        '                        c.UpdateCache(mc.MappingEngine, New Pair(Of _ICachedEntity)() {New Pair(Of _ICachedEntity)(Me, mo)}, mc, AddressOf ClearCacheFlags, Nothing, Nothing, False, False)
        '                        'mc.Cache.UpdateCacheOnDelete(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing)
        '                    End If
        '                    Accept_AfterUpdateCacheDelete(Me, mc)
        '                    RaiseEvent Deleted(Me, EventArgs.Empty)
        '                ElseIf _upd.Added Then
        '                    '_valProcs = False
        '                    Dim dic As IDictionary = mc.GetDictionary(Me.GetType, TryCast(GetEntitySchema(mc.MappingEngine), ICacheBehavior))
        '                    Dim kw As CacheKey = New CacheKey(Me)
        '                    'Dim o As CachedEntity = CType(dic(kw), CachedEntity)
        '                    'If (o Is Nothing) OrElse (Not o.IsLoaded AndAlso IsLoaded) Then
        '                    '    dic(kw) = Me
        '                    'End If
        '                    CacheBase.AddObjectInternal(Me, kw, dic)
        '                    If updateCache AndAlso c IsNot Nothing Then
        '                        'mc.Cache.UpdateCacheOnAdd(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing, Nothing)
        '                        c.UpdateCache(mc.MappingEngine, New Pair(Of _ICachedEntity)() {New Pair(Of _ICachedEntity)(Me, mo)}, mc, AddressOf ClearCacheFlags, Nothing, Nothing, False, False)
        '                    End If
        '                    Accept_AfterUpdateCacheAdd(Me, mc.Cache, mo)
        '                    RaiseEvent Added(Me, EventArgs.Empty)
        '                Else
        '                    If updateCache Then
        '                        If c IsNot Nothing Then
        '                            c.UpdateCache(mc.MappingEngine, New Pair(Of _ICachedEntity)() {New Pair(Of _ICachedEntity)(Me, mo)}, mc, AddressOf ClearCacheFlags, Nothing, Nothing, False, True)
        '                        End If
        '                        UpdateCacheAfterUpdate(c)
        '                    End If
        '                    RaiseEvent Updated(Me, EventArgs.Empty)
        '                End If
        '                'ElseIf _valProcs AndAlso updateCache Then
        '                '    mc.Cache.ValidateSPOnUpdate(Me, Nothing)
        '            End If

        '            RaiseEvent ChangesAccepted(Me, EventArgs.Empty)
        '        End Using
        '    End Using

        '    Return mo
        'End Function

        'Protected Overridable ReadOnly Property HasBodyChanges() As Boolean Implements ICachedEntity.HasBodyChanges
        '    Get
        '        Return ObjectState = Entities.ObjectState.Modified OrElse ObjectState = Entities.ObjectState.Deleted OrElse ObjectState = Entities.ObjectState.Created
        '    End Get
        'End Property

        ''Protected Overridable ReadOnly Property HasM2MChanges() As Boolean
        ''    Get
        ''        Return False
        ''    End Get
        ''End Property

        'Protected Overridable ReadOnly Property HasChanges() As Boolean Implements ICachedEntity.HasChanges
        '    Get
        '        Return HasBodyChanges 'OrElse HasM2MChanges()
        '    End Get
        'End Property

        'Protected Overrides Sub CorrectStateAfterLoading(ByVal objectWasCreated As Boolean)
        '    Dim os As ObjectState = ObjectState
        '    MyBase.CorrectStateAfterLoading(objectWasCreated)
        '    If objectWasCreated Then
        '        If os = Orm.ObjectState.Modified Then
        '            OrmCache.UnregisterModification(Me)
        '        End If
        '    End If
        'End Sub

        Protected Overridable Sub AcceptRelationalChanges(ByVal updateCache As Boolean, ByVal mc As OrmManager) Implements ICachedEntity.AcceptRelationalChanges
            '_needAccept.Clear()

            'Dim rel As IRelation = mc.ObjectSchema.GetConnectedTypeRelation(t)
            'If rel IsNot Nothing Then
            '    Dim c As New OrmManager.M2MEnum(rel, Me, mc.ObjectSchema)
            '    mc.Cache.ConnectedEntityEnum(t, AddressOf c.Accept)
            'End If
        End Sub

        Protected Sub _Init(ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine)
            MyBase.Init(cache, schema)
            'If schema IsNot Nothing Then
            '    'Dim arr As Generic.List(Of EntityPropertyAttribute) = schema.GetSortedFieldList(Me.GetType)
            '    _loaded_members = New BitArray(GetProperties(schema).Count)
            'End If
        End Sub


        Protected Overridable Overloads Sub Init(ByVal pk As IEnumerable(Of PKDesc), ByVal cache As CacheBase, ByVal mpe As ObjectMappingEngine) Implements _ICachedEntity.Init
            _Init(cache, mpe)
            OrmManager.SetPK(Me, pk, mpe)
            PKLoaded(pk.Count)
        End Sub

#Region " Xml Serialization "

        Protected Overridable Function GetSchema() As System.Xml.Schema.XmlSchema Implements System.Xml.Serialization.IXmlSerializable.GetSchema
            Return Nothing
        End Function

        Protected Overridable Sub ReadXml(ByVal reader As System.Xml.XmlReader) Implements System.Xml.Serialization.IXmlSerializable.ReadXml
            Dim t As Type = Me.GetType

            'If OrmSchema IsNot Nothing Then
            '    Dim arr As Generic.List(Of EntityPropertyAttribute) = OrmSchema.GetSortedFieldList(Me.GetType)
            '    _members_load_state = New BitArray(arr.Count)
            'End If

            CType(Me, _IEntity).BeginLoading()

            Using mc As IGetManager = GetMgr()
                Dim mpe As ObjectMappingEngine = mc.Manager.MappingEngine
                Dim oschema As IEntitySchema = GetEntitySchema(mpe)
                Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap

                With reader
l1:
                    If .NodeType = XmlNodeType.Element AndAlso .Name = t.Name Then
                        ReadValues(mc.Manager, reader, oschema, mpe)
                        Do While .Read
                            Select Case .NodeType
                                Case XmlNodeType.Element
                                    ReadValue(mc.Manager, .Name, reader, map, oschema, mpe)
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


                If mpe IsNot Nothing Then
                    Dim ll As IPropertyLazyLoad = TryCast(Me, IPropertyLazyLoad)
                    If ll IsNot Nothing Then
                        OrmManager.CheckIsAllLoaded(ll, mpe, Integer.MaxValue, map)
                    End If
                End If
            End Using
        End Sub

        Protected Overridable Sub WriteXml(ByVal writer As System.Xml.XmlWriter) Implements System.Xml.Serialization.IXmlSerializable.WriteXml
            With writer
                Dim t As Type = Me.GetType
                Dim mpe As ObjectMappingEngine = GetMappingEngine()
                Dim oschema As IEntitySchema = GetEntitySchema(mpe)
                Dim elems As New Generic.List(Of Pair(Of String, Object))
                Dim xmls As New Generic.List(Of Pair(Of String, String))
                Dim objs As New List(Of Pair(Of String, IEnumerable(Of PKDesc)))

                For Each m As MapField2Column In oschema.FieldColumnMap
                    Dim pi As Reflection.PropertyInfo = m.PropertyInfo
                    If (m.Attributes And Field2DbRelations.NotSerialized) <> Field2DbRelations.NotSerialized Then
                        Dim propertyAlias As String = m.PropertyAlias
                        If IsLoaded Then
                            Dim v As Object = pi.GetValue(Me, Nothing)
                            Dim tt As Type = pi.PropertyType
                            If GetType(ICachedEntity).IsAssignableFrom(tt) Then
                                '.WriteAttributeString(c.FieldName, CType(v, ICachedEntity).Identifier.ToString)
                                If v IsNot Nothing Then
                                    objs.Add(New Pair(Of String, IEnumerable(Of PKDesc))(propertyAlias, OrmManager.GetPKValues(CType(v, _ICachedEntity), Nothing)))
                                Else
                                    objs.Add(New Pair(Of String, IEnumerable(Of PKDesc))(propertyAlias, Nothing))
                                End If
                            ElseIf tt.IsArray Then
                                elems.Add(New Pair(Of String, Object)(propertyAlias, pi.GetValue(Me, Nothing)))
                            ElseIf tt Is GetType(XmlDocument) Then
                                Dim xdoc As XmlDocument = CType(pi.GetValue(Me, Nothing), XmlDocument)
                                If xdoc Is Nothing Then
                                    xmls.Add(New Pair(Of String, String)(propertyAlias, Nothing))
                                Else
                                    xmls.Add(New Pair(Of String, String)(propertyAlias, xdoc.OuterXml))
                                End If
                            Else
                                If v IsNot Nothing Then
                                    .WriteAttributeString(propertyAlias, Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture))
                                Else
                                    .WriteAttributeString(propertyAlias, "xxx:nil")
                                End If
                            End If
                        ElseIf m.IsPK Then
                            .WriteAttributeString(propertyAlias, pi.GetValue(Me, Nothing).ToString)
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

                For Each p As Pair(Of String, IEnumerable(Of PKDesc)) In objs
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

        Protected Sub ReadValue(ByVal mgr As OrmManager, ByVal propertyAlias As String, ByVal reader As XmlReader, _
            ByVal map As Collections.IndexedCollection(Of String, MapField2Column), ByVal oschema As IEntitySchema, ByVal mpe As ObjectMappingEngine)
            reader.Read()
            Dim m As MapField2Column = map(propertyAlias)
            Select Case reader.NodeType
                Case XmlNodeType.CDATA
                    Dim x As New XmlDocument
                    x.LoadXml(reader.Value)
                    ObjectMappingEngine.SetPropertyValue(Me, m.PropertyAlias, x, oschema, m.PropertyInfo)
                    SetLoaded(propertyAlias, True, map, mpe)
                Case XmlNodeType.Text
                    Dim v As String = reader.Value
                    ObjectMappingEngine.SetPropertyValue(Me, m.PropertyAlias, Convert.FromBase64String(v), oschema, m.PropertyInfo)
                    SetLoaded(propertyAlias, True, map, mpe)
                Case XmlNodeType.EndElement
                    ObjectMappingEngine.SetPropertyValue(Me, m.PropertyAlias, Nothing, oschema, m.PropertyInfo)
                    SetLoaded(propertyAlias, True, map, mpe)
                Case XmlNodeType.Element
                    Dim o As Object = Nothing
                    Dim pk() As PKDesc = GetPKs(reader)
                    If m.IsFactory Then
                        Dim f As IEntityFactory = TryCast(Me, IEntityFactory)
                        If f IsNot Nothing Then
                            Dim e As _IEntity = f.CreateContainingEntity(mgr, propertyAlias, pk)
                            'If e IsNot Nothing Then
                            '    e.SetMgrString(IdentityString)
                            '    RaiseObjectLoaded(e)
                            'End If
                        Else
                            Throw New OrmObjectException(String.Format("Property {0} is factory property. Implementation of IFactory is required.", propertyAlias))
                        End If
                    Else
                        o = mgr.CreateObject(pk, m.PropertyInfo.PropertyType)
                    End If
                    ObjectMappingEngine.SetPropertyValue(Me, m.PropertyAlias, o, oschema, m.PropertyInfo)
                    If o IsNot Nothing AndAlso GetType(_ICachedEntity).IsAssignableFrom(o.GetType) Then
                        CType(o, _ICachedEntity).SetCreateManager(CreateManager)
                        'RaiseObjectLoaded(o)
                    End If
                    'End If
                    'End Using
                    SetLoaded(propertyAlias, True, map, mpe)
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

        Protected Sub ReadValues(ByVal mgr As OrmManager, ByVal reader As XmlReader, _
            ByVal oschema As IEntitySchema, ByVal mpe As ObjectMappingEngine)
            With reader
                .MoveToFirstAttribute()
                Dim t As Type = Me.GetType
                Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap
                Dim fv As IStorageValueConverter = TryCast(Me, IStorageValueConverter)
                Dim pk_count As Integer

                Do
                    Dim m As MapField2Column = map(.Name)
                    Dim pi As Reflection.PropertyInfo = m.PropertyInfo

                    If m.IsPK Then
                        Dim value As String = .Value
                        If value = "xxx:nil" Then value = Nothing
                        If fv IsNot Nothing Then
                            value = CStr(fv.CreateValue(oschema, m, .Name, value))
                        End If

                        Dim v As Object = Convert.ChangeType(value, pi.PropertyType)
                        If pi IsNot Nothing Then
                            pi.SetValue(Me, v, Nothing)
                            SetLoaded(.Name, True, map, mpe)
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
                        mgr.CreateCopyForSaveNewEntry(Me, oschema, Nothing)
                        'Cache.Modified(obj).Reason = ModifiedObject.ReasonEnum.SaveNew
                    Else
                        'Using mc As IGetManager = GetMgr()
                        obj = mgr.NormalizeObject(Me, mgr.GetDictionary(Me.GetType), True, True, oschema)
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

                    Dim m As MapField2Column = map(.Name)
                    Dim pi As Reflection.PropertyInfo = m.PropertyInfo

                    If Not m.IsPK Then
                        Dim value As String = .Value
                        If value = "xxx:nil" Then value = Nothing
                        If fv IsNot Nothing Then
                            value = CStr(fv.CreateValue(oschema, m, .Name, value))
                        End If

                        If GetType(ISinglePKEntity).IsAssignableFrom(pi.PropertyType) Then
                            'If (att And Field2DbRelations.Factory) = Field2DbRelations.Factory Then
                            '    CreateObject(.Name, value)
                            '    SetLoaded(c, True)
                            'Else
                            'Using mc As IGetManager = GetMgr()
                            Dim type_created As Type = pi.PropertyType
                            Dim en As String = mpe.GetEntityNameByType(type_created)
                            If Not String.IsNullOrEmpty(en) Then
                                type_created = mpe.GetTypeByEntityName(en)

                                If type_created Is Nothing Then
                                    Throw New OrmManagerException("Cannot find type for entity " & en)
                                End If
                            End If
                            Dim v As ISinglePKEntity = mgr.GetKeyEntityFromCacheOrCreate(value, type_created)
                            If pi IsNot Nothing Then
                                pi.SetValue(obj, v, Nothing)
                                SetLoaded(.Name, True, map, mpe)
                                If v IsNot Nothing Then
                                    v.SetCreateManager(CreateManager)
                                    'RaiseObjectLoaded(v)
                                End If
                            End If
                            'End Using
                            'End If
                        Else
                            Dim v As Object = Convert.ChangeType(value, pi.PropertyType)
                            If pi IsNot Nothing Then
                                pi.SetValue(obj, v, Nothing)
                                SetLoaded(.Name, True, map, mpe)
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

        Private ReadOnly Property IsPKLoaded() As Boolean Implements _ICachedEntity.IsPKLoaded
            Get
                Return _hasPK
            End Get
        End Property

        ' ''' <summary>
        ' ''' Возвращает массив полей и значений первичного ключа
        ' ''' </summary>
        ' ''' <returns>массив первичных ключей</returns>
        ' ''' <remarks>Для получения схемы типа используется движок маппинга, возвращаемый функцией <see cref="GetMappingEngine"/></remarks>
        ' ''' <exception cref="InvalidOperationException">Если невозможно получить схему для типа</exception>
        ' ''' <exception cref="ArgumentException">Если тип не реализует интерфейс <see cref="IOptimizedValues"/> и значение первичного ключа невозможно получить по рефлекшену.</exception>
        'Public Overridable Function GetPKValues() As PKDesc() Implements ICachedEntity.GetPKValues
        '    Dim mpe As Worm.ObjectMappingEngine = GetMappingEngine()

        '    Dim oschema As IEntitySchema = GetEntitySchema(mpe)

        '    If oschema Is Nothing Then
        '        Throw New InvalidOperationException(String.Format("Cannot get entity schema for type {0}", Me.GetType))
        '    End If

        '    Return ObjectMappingEngine.GetPKs(Me, oschema)
        'End Function

        'Protected Property OriginalCopy() As ICachedEntity Implements ICachedEntity.OriginalCopy
        '    Get
        '        'Using SyncHelper(False)
        '        '    If _copy Is Nothing Then
        '        '        If (ObjectState = Entities.ObjectState.Modified OrElse Not IsPropertiesLazyLoad) AndAlso ObjectState <> Entities.ObjectState.Created AndAlso ObjectState <> Entities.ObjectState.Deleted Then
        '        '            Using gm As IGetManager = GetMgr()
        '        '                _copy = CType(gm.Manager.GetEntityCloneFromStorage(Me), CachedEntity)
        '        '            End Using
        '        '        End If
        '        '    End If

        '        '    Return _copy
        '        'End Using
        '        Return _copy
        '    End Get
        '    Set(ByVal value As ICachedEntity)
        '        _copy = value
        '    End Set
        'End Property

        'Protected ReadOnly Property OriginalCopy(ByVal cache As CacheBase) As ICachedEntity Implements _ICachedEntity.OriginalCopy
        '    Get
        '        Dim o As ObjectModification = cache.ShadowCopy(Me)
        '        If o Is Nothing Then Return Nothing
        '        Return o.Obj
        '    End Get
        'End Property

        'Protected Overridable ReadOnly Property ChangeDescription() As String Implements ICachedEntity.ChangeDescription
        '    Get
        '        Dim sb As New StringBuilder
        '        sb.Append("Attributes:").Append(vbCrLf)
        '        If ObjectState = Entities.ObjectState.Modified Then
        '            For Each pa As String In Changes(OriginalCopy)
        '                sb.Append(vbTab).Append(pa).Append(vbCrLf)
        '            Next
        '        Else
        '            Dim o As ICachedEntity = CType(CreateSelfInitPK(), ICachedEntity)
        '            For Each pa As String In Changes(o)
        '                sb.Append(vbTab).Append(pa).Append(vbCrLf)
        '            Next
        '        End If
        '        Return sb.ToString
        '    End Get
        'End Property

        'Protected Overridable ReadOnly Property Changes(ByVal obj As ICachedEntity) As String()
        '    Get
        '        Dim l As New List(Of String)
        '        Dim schema As ObjectMappingEngine = GetMappingEngine()
        '        Dim oschema As IEntitySchema = GetEntitySchema(schema)
        '        If Not Object.Equals(obj.SpecificMappingEngine, SpecificMappingEngine) Then
        '            obj.SpecificMappingEngine = SpecificMappingEngine()
        '        End If
        '        For Each m As MapField2Column In oschema.FieldColumnMap
        '            Dim original As Object = ObjectMappingEngine.GetPropertyValue(obj, m.PropertyAlias, oschema, m.PropertyInfo)
        '            If Not m.IsReadOnly Then
        '                Dim current As Object = ObjectMappingEngine.GetPropertyValue(Me, m.PropertyAlias, oschema, m.PropertyInfo)
        '                If (original IsNot Nothing AndAlso Not original.Equals(current)) OrElse _
        '                    (current IsNot Nothing AndAlso Not current.Equals(original)) Then
        '                    l.Add(m.PropertyAlias)
        '                End If
        '            End If
        '        Next
        '        Return l.ToArray
        '    End Get
        'End Property

        'Protected Overrides Sub InitNewEntity(ByVal mgr As OrmManager, ByVal en As Entity)
        '    If mgr Is Nothing Then
        '        CType(en, CachedEntity).Init(OrmManager.GetPKValues(Me), Nothing, Nothing)
        '    Else
        '        CType(en, CachedEntity).Init(OrmManager.GetPKValues(Me), mgr.Cache, mgr.MappingEngine)
        '    End If
        'End Sub

        'Protected Overrides Sub _PrepareLoadingUpdate()
        '    CreateCopyForSaveNewEntry(Nothing)
        'End Sub

        Protected Function EnsureInCache(ByVal mgr As OrmManager) As CachedEntity
            'Using mc As IGetManager = GetMgr()
            Return CType(mgr.EnsureInCache(Me), CachedEntity)
            'End Using
        End Function

        Protected Sub CreateClone4Delete(ByVal mgr As OrmManager)
            SetObjectState(Entities.ObjectState.Deleted)
            'mgr.Cache.RegisterModification(mgr, Me, ObjectModification.ReasonEnum.Delete, TryCast(GetEntitySchema(mgr.MappingEngine), ICacheBehavior))
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

        Protected Sub UpdateCache(ByVal mc As OrmManager, ByVal oldObj As ICachedEntity) Implements _ICachedEntity.UpdateCache
            Dim c As OrmCache = TryCast(mc.Cache, OrmCache)
            If c IsNot Nothing Then
                c.UpdateCache(mc.MappingEngine, New Pair(Of _ICachedEntity)() {New Pair(Of _ICachedEntity)(Me, CType(oldObj, _ICachedEntity))}, mc, AddressOf OrmManager.ClearCacheFlags, Nothing, Nothing, False, _upd.UpdatedFields IsNot Nothing)
            End If
            UpdateCacheAfterUpdate(c)
            For Each el As M2MRelation In New List(Of M2MRelation)(_upd.Relations)
                If c IsNot Nothing Then
                    c.RemoveM2MQueries(el)
                End If
                _upd.Relations.Remove(el)
            Next
        End Sub

        'Private ReadOnly Property IsReadOnly(ByVal mgr As OrmManager) As Boolean
        '    Get
        '        Using SyncHelper(True)
        '            If ObjectState = Entities.ObjectState.Modified Then
        '                'Using mc As IGetManager = GetMgr()
        '                Dim mo As ObjectModification = mgr.Cache.ShadowCopy(Me.GetType, Me, TryCast(GetEntitySchema(mgr.MappingEngine), ICacheBehavior))
        '                'If mo Is Nothing Then mo = _mo
        '                If mo IsNot Nothing Then
        '                    If mo.User IsNot Nothing AndAlso Not mo.User.Equals(mgr.CurrentUser) Then
        '                        Return True
        '                    End If

        '                End If
        '                'End Using
        '            End If
        '            Return False
        '        End Using
        '    End Get
        'End Property

        Public Overridable Sub RejectRelationChanges(ByVal mc As OrmManager) Implements ICachedEntity.RejectRelationChanges

        End Sub

        ' ''' <summary>
        ' ''' Отмена изменений
        ' ''' </summary>
        'Public Sub RejectChanges() Implements ICachedEntity.RejectChanges
        '    Using gmc As IGetManager = GetMgr()
        '        _RejectChanges(gmc.Manager)
        '    End Using
        'End Sub

        '        Protected Sub _RejectChanges(ByVal mgr As OrmManager) Implements _ICachedEntity.RejectChanges
        '            Using SyncHelper(False)
        '                RejectRelationChanges(mgr)

        '                If ObjectState = ObjectState.Modified OrElse ObjectState = Entities.ObjectState.Deleted OrElse ObjectState = Entities.ObjectState.Created Then
        '                    'If IsReadOnly(mgr) Then
        '                    '    Throw New OrmObjectException(ObjName & " object in readonly state")
        '                    'End If

        '                    Dim oc As ICachedEntity '= OriginalCopy()
        '                    If ObjectState <> Entities.ObjectState.Deleted Then
        '                        If oc Is Nothing Then
        '                            If ObjectState <> Entities.ObjectState.Created Then
        '                                Throw New OrmObjectException(ObjName & ": When object is in modified state it has to have an original copy")
        '                            End If
        '                            Return
        '                        End If
        '                    End If

        '                    Dim mo As ObjectModification = mgr.Cache.ShadowCopy(Me.GetType, Me, TryCast(GetEntitySchema(mgr.MappingEngine), ICacheBehavior))
        '                    If mo IsNot Nothing Then
        '                        If mo.User IsNot Nothing AndAlso Not mo.User.Equals(mgr.CurrentUser) Then
        '                            Throw New OrmObjectException(ObjName & " object in readonly state")
        '                        End If

        '                        If ObjectState = Entities.ObjectState.Deleted AndAlso mo.Reason <> ObjectModification.ReasonEnum.Delete Then
        '                            'Debug.Assert(False)
        '                            'Throw New OrmObjectException
        '                            Return
        '                        End If

        '                        If ObjectState = Entities.ObjectState.Modified AndAlso (mo.Reason = ObjectModification.ReasonEnum.Delete) Then
        '                            Debug.Assert(False)
        '                            Throw New OrmObjectException
        '                        End If
        '                    End If

        '                    'Debug.WriteLine(Environment.StackTrace)
        '                    '_needAdd = False
        '                    '_needDelete = False
        '                    _upd = New UpdateCtx

        '                    Dim olds As ObjectState = Entities.ObjectState.None
        '                    If oc IsNot Nothing Then
        '                        olds = oc.GetOldState
        '                    End If

        '                    Dim oldkey As Integer?
        '                    If IsPKLoaded Then
        '                        oldkey = Key
        '                    End If

        '                    Dim newid() As PKDesc = Nothing
        '                    If oc IsNot Nothing Then
        '                        newid = oc.GetPKValues()
        '                    End If

        '                    If olds <> Entities.ObjectState.Created Then
        '                        '_loaded_members = 
        '                        OrmManager.RevertToOriginalVersion(Me)
        '                        OrmManager.RemoveVersionData(Me, mgr.Cache, mgr.MappingEngine, False)
        '                    End If

        '                    If newid IsNot Nothing Then
        '                        SetPK(newid, mgr.MappingEngine)
        '                    End If

        '#If TraceSetState Then
        '                    If mo isnot Nothing then
        '                        SetObjectState(olds, mo.Reason, mo.StackTrace, mo.DateTime)
        '                    end if
        '#Else
        '                    SetObjectStateClear(olds)
        '#End If
        '                    If ObjectState = Entities.ObjectState.Created Then
        '                        If oldkey.HasValue Then
        '                            'Using gmc As IGetManager = GetMgr()
        '                            'Dim mc As OrmManager = gmc.Manager
        '                            Dim dic As IDictionary = mgr.GetDictionary(Me.GetType)
        '                            If dic Is Nothing Then
        '                                Dim name As String = Me.GetType.Name
        '                                Throw New OrmObjectException("Collection for " & name & " not exists")
        '                            End If

        '                            dic.Remove(oldkey)
        '                        End If
        '                        ' End Using

        '                        mgr.Cache.UnregisterModification(Me, mgr.MappingEngine, TryCast(GetEntitySchema(mgr.MappingEngine), ICacheBehavior))
        '                        _copy = Nothing
        '                        Dim ll As IPropertyLazyLoad = TryCast(Me, IPropertyLazyLoad)
        '                        If ll IsNot Nothing Then
        '                            ll.IsLoaded = False
        '                        End If
        '                        '_loaded_members = New BitArray(_loaded_members.Count)
        '                    End If

        '                    'ElseIf state = Obm.ObjectState.Deleted Then
        '                    '    CheckCash()
        '                    '    using SyncHelper(false)
        '                    '        state = ObjectState.None
        '                    '        OrmCache.UnregisterModification(Me)
        '                    '    End SyncLock
        '                    'Else
        '                    '    Throw New OrmObjectException(ObjName & "Rejecting changes in the state " & _state.ToString & " is not allowed")
        '                End If
        '                'Invariant(mgr)
        '            End Using
        '        End Sub

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
            'If IsReadOnly(mc) Then
            '    Throw New OrmObjectException(ObjName & "Object in readonly state")
            'End If

            Dim r As Boolean = True
            If ObjectState = Entities.ObjectState.Created OrElse ObjectState = Entities.ObjectState.NotFoundInSource Then
                'If IsPKLoaded AndAlso OriginalCopy() IsNot Nothing Then
                '    Throw New OrmObjectException(ObjName & " already exists.")
                'End If
                Dim o As ICachedEntity = mc.AddObject(Me)
                If o Is Nothing Then
                    r = False
                Else
                    If ObjectState <> Entities.ObjectState.Modified Then
                        Assert(ObjectState = Entities.ObjectState.Modified, "Object {0} must be in Modified state", ObjName) ' OrElse _state = Orm.ObjectState.None
                    End If

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
            ElseIf ObjectState = Entities.ObjectState.Modified OrElse TryCast(Me, IUndoChanges) Is Nothing Then
#If TraceSetState Then
                Dim mo As ObjectModification = mc.Cache.ShadowCopy(Me)
                If mo Is Nothing OrElse mo.Reason = ObjectModification.ReasonEnum.Delete Then
                    Debug.Assert(False)
                    Throw New OrmObjectException
                End If
#End If
                Dim co As CachedEntity = CType(mc.Cache.GetFromCache(Me, mc.MappingEngine), CachedEntity)
                If co IsNot Nothing AndAlso Not ReferenceEquals(Me, co) Then
                    Throw New OrmObjectException("Object in different cache", Me)
                End If
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

                'If obj.ObjectState = Entities.ObjectState.Clone Then
                '    Throw New OrmObjectException(obj.ObjName & "Deleting clone is not allowed")
                'End If

                If obj.ObjectState <> Entities.ObjectState.Modified AndAlso obj.ObjectState <> Entities.ObjectState.None AndAlso obj.ObjectState <> Entities.ObjectState.NotLoaded Then
                    Throw New OrmObjectException(obj.ObjName & "Deleting is not allowed for this object")
                End If

                'Dim mo As ObjectModification = mgr.Cache.ShadowCopy(obj.GetType, obj, TryCast(obj.GetEntitySchema(mgr.MappingEngine), ICacheBehavior))
                ''If mo Is Nothing Then mo = _mo
                'If mo IsNot Nothing Then
                '    'Using mc As IGetManager = obj.GetMgr()
                '    If mo.User IsNot Nothing AndAlso Not mo.User.Equals(mgr.CurrentUser) Then
                '        Throw New OrmObjectException(obj.ObjName & "Object has already altered by user " & mo.User.ToString)
                '    End If
                '    'End Using
                '    Debug.Assert(mo.Reason <> ObjectModification.ReasonEnum.Delete)
                'Else
                If obj.ObjectState = Entities.ObjectState.NotLoaded Then
                    mgr.Load(obj)
                    If obj.ObjectState = Entities.ObjectState.NotFoundInSource Then
                        Return False
                    End If
                End If
                If obj.ObjectState = Entities.ObjectState.Modified Then
                    Assert(obj.ObjectState <> Entities.ObjectState.Modified, "Object {0} cannot be in Modified state", obj.ObjName)
                End If

                obj.CreateClone4Delete(mgr)
                'OrmCache.Modified(Me).Reason = ModifiedObject.ReasonEnum.Delete
                'Dim modified As OrmBase = CloneMe()
                'modified._old_state = modified.ObjectState
                'modified.ObjectState = ObjectState.Clone
                'OrmCache.RegisterModification(modified)
                'End If
                'obj.SetObjectState(ObjectState.Deleted)

                'Using mc As IGetManager = obj.GetMgr()
                'mgr.RaiseBeginDelete(obj)
                'End Using
            End Using

            Return True
        End Function

        Public Function Delete() As Boolean
            Using mc As IGetManager = GetMgr()
                Return Delete(mc.Manager)
            End Using
        End Function

        Public Overridable Function Delete(ByVal mgr As OrmManager) As Boolean Implements ICachedEntity.Delete
            Return _Delete(mgr, EnsureInCache(mgr))
        End Function

        Public Function EnsureLoaded() As CachedEntity
            Using mc As IGetManager = GetMgr()
                Return EnsureLoaded(mc.Manager)
            End Using
        End Function

        Public Function EnsureLoaded(ByVal mgr As OrmManager) As CachedEntity
            Return CType(mgr.GetFromCacheLoadedOrLoadFromDB(Me, mgr.GetDictionary(Me.GetType)), CachedEntity)
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

        Private Property UpdateCtx() As UpdateCtx Implements _ICachedEntity.UpdateCtx
            Get
                Return _upd
            End Get
            Set(ByVal value As UpdateCtx)
                _upd = value
            End Set
        End Property

        Protected Friend Sub UpdateCacheAfterUpdate(ByVal c As OrmCache) Implements _ICachedEntity.UpdateCacheAfterUpdate
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

        Protected Overridable Function ForseUpdate(ByVal propertyAlias As String) As Boolean Implements _ICachedEntity.ForseUpdate
            Return False
        End Function

        'Protected Function SortedColumnAttributeCount(ByVal sch As ObjectMappingEngine) As Integer
        '    Dim schema As ObjectMappingEngine = sch
        '    If schema Is Nothing Then
        '        schema = GetMappingEngine()
        '    End If
        '    If schema Is Nothing Then
        '        If _attList IsNot Nothing Then
        '            Return _attList.Count
        '        Else
        '            Return SortedColumnAttributeList(schema).Count
        '        End If
        '    Else
        '        Return GetProperties(schema).Count
        '    End If
        'End Function

        'Private _attList As List(Of EntityPropertyAttribute)
        'Protected Function SortedColumnAttributeList(ByVal sch As ObjectMappingEngine) As List(Of EntityPropertyAttribute)
        '    Dim schema As ObjectMappingEngine = sch
        '    If schema Is Nothing Then
        '        schema = GetMappingEngine()
        '    End If
        '    If schema Is Nothing Then
        '        If _attList Is Nothing Then
        '            Dim l As New List(Of EntityPropertyAttribute)
        '            For Each cl As EntityPropertyAttribute In ObjectMappingEngine.GetMappedProperties(Me.GetType, False).Keys
        '                Dim idx As Integer = l.BinarySearch(cl)
        '                If idx < 0 Then
        '                    l.Insert(Not idx, cl)
        '                Else
        '                    Throw New InvalidOperationException
        '                End If
        '            Next
        '            _attList = l
        '        End If
        '        Return _attList
        '    Else
        '        Return schema.GetSortedFieldList(Me.GetType)
        '    End If
        'End Function

        'Protected Function GetProperties(ByVal schema As ObjectMappingEngine) As IDictionary
        '    If _props Is Nothing Then
        '        _props = schema.GetProperties(Me.GetType)
        '    End If
        '    Return _props
        'End Function

        'Protected Overrides Function IsPropertyLoaded(ByVal propertyAlias As String) As Boolean
        '    Dim mpe As ObjectMappingEngine = GetMappingEngine()
        '    Dim map As Collections.IndexedCollection(Of String, MapField2Column) = GetEntitySchema(mpe).FieldColumnMap
        '    Dim idx As Integer = map.IndexOf(propertyAlias)
        '    If idx < 0 Then
        '        Throw New OrmObjectException(String.Format("Property {0} not found in type {1}. Ensure it is not suppressed", propertyAlias, Me.GetType))
        '    End If
        '    Return _members_load_state(idx, map, mpe)
        'End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, CachedEntity))
        End Function

        Public Overloads Function Equals(ByVal obj As CachedEntity) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            If Me.GetType IsNot obj.GetType Then
                Return False
            End If
            Dim pks As IEnumerable(Of PKDesc) = OrmManager.GetPKValues(Me, Nothing)
            Dim pks2 As IEnumerable(Of PKDesc) = OrmManager.GetPKValues(obj, Nothing)
            For i As Integer = 0 To pks.count - 1
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

#Region " Operators "

        Public Shared Operator <>(ByVal obj1 As CachedEntity, ByVal obj2 As CachedEntity) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then Return False
                'Throw New MediaObjectModelException("obj1 parameter cannot be nothing")
                Return Not obj2.Equals(obj1)
            End If
            Return Not obj1.Equals(obj2)
        End Operator

        Public Shared Operator =(ByVal obj1 As CachedEntity, ByVal obj2 As CachedEntity) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then Return True
                'Throw New MediaObjectModelException("obj1 parameter cannot be nothing")
                Return obj2.Equals(obj1)
            End If
            Return obj1.Equals(obj2)
        End Operator

#End Region

#Region " Graph "
        Protected Overridable Function GetChildChangedObjects() As List(Of ICachedEntity)
            Dim l As New List(Of ICachedEntity)
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

        Protected Overridable Function GetParentChangedObjects() As List(Of ICachedEntity)
            Dim l As New List(Of ICachedEntity)
            Dim oschema As IEntitySchema = GetEntitySchema(GetMappingEngine)
            For Each m As MapField2Column In oschema.FieldColumnMap
                Dim pi As Reflection.PropertyInfo = m.PropertyInfo
                If GetType(ICachedEntity).IsAssignableFrom(pi.PropertyType) Then
                    Dim o As ICachedEntity = CType(ObjectMappingEngine.GetPropertyValue(Me, m.PropertyAlias, oschema, m.PropertyInfo), ICachedEntity)
                    If o IsNot Nothing AndAlso OrmManager.HasChanges(o) Then
                        l.Add(o)
                    End If
                End If
            Next
            Return l
        End Function

        Protected Friend Function GetChangedObjectList() As List(Of _ICachedEntity) Implements _ICachedEntity.GetChangedObjectList
            Dim l As New List(Of _ICachedEntity)
            FillChangedObjectList(l)
            Return l
        End Function

        Protected Friend Sub FillChangedObjectList(ByVal objectList As List(Of _ICachedEntity)) Implements _ICachedEntity.FillChangedObjectList
            Dim l As New List(Of _ICachedEntity)

            For Each o As _ICachedEntity In GetParentChangedObjects()
                If Not objectList.Contains(o) Then
                    objectList.Add(o)
                    l.Add(o)
                End If
            Next

            For Each o As _ICachedEntity In GetChildChangedObjects()
                If Not objectList.Contains(o) Then
                    objectList.Add(o)
                    l.Add(o)
                End If
            Next

            For Each o As _ICachedEntity In l
                o.FillChangedObjectList(objectList)
            Next
        End Sub

        Protected Friend Function GetChangedObjectListWithSelf() As List(Of _ICachedEntity)
            Dim l As List(Of _ICachedEntity) = GetChangedObjectList()
            If OrmManager.HasChanges(Me) AndAlso Not l.Contains(Me) Then
                l.Add(Me)
            End If
            Return l
        End Function

#End Region

        'Public Overridable Function ShadowCopy(ByVal mgr As OrmManager) As ObjectModification 'Implements _ICachedEntity.ShadowCopy
        '    Return mgr.Cache.ShadowCopy(Me.GetType, Me, TryCast(GetEntitySchema(mgr.MappingEngine), ICacheBehavior))
        'End Function

        Private _us As String
        Public Overridable ReadOnly Property UniqueString() As String Implements IKeyProvider.UniqueString
            Get
                If String.IsNullOrEmpty(_us) Then
                    If Not IsPKLoaded Then Throw New OrmObjectException(String.Format("Entity of type {0} has no primary key", Me.GetType))
                    Dim r As New StringBuilder
                    For Each pk As PKDesc In OrmManager.GetPKValues(Me, Nothing)
                        r.Append(pk.PropertyAlias).Append(":").Append(pk.Value.ToString).Append(",")
                    Next
                    _us = r.ToString
                End If
                Return _us
            End Get
        End Property

        Private Sub SetLoaded(ByVal propertyAlias As String, ByVal p2 As Boolean, ByVal map As Collections.IndexedCollection(Of String, MapField2Column), ByVal mpe As ObjectMappingEngine)
            Dim ll As IPropertyLazyLoad = TryCast(Me, IPropertyLazyLoad)
            If ll IsNot Nothing Then
                OrmManager.SetLoaded(ll, propertyAlias, p2, map, mpe)
            End If
        End Sub

        Public Sub RaiseAdded(ByVal args As System.EventArgs) Implements ICachedEntity.RaiseAdded
            RaiseEvent Added(Me, args)
        End Sub

        Public Sub RaiseChangesAccepted(ByVal args As System.EventArgs) Implements ICachedEntity.RaiseChangesAccepted
            RaiseEvent ChangesAccepted(Me, args)
        End Sub

        Public Sub RaiseDeleted(ByVal args As System.EventArgs) Implements ICachedEntity.RaiseDeleted
            RaiseEvent Deleted(Me, args)
        End Sub

        Public Sub RaiseUpdated(ByVal args As System.EventArgs) Implements ICachedEntity.RaiseUpdated
            RaiseEvent Updated(Me, args)
        End Sub

        Public Sub RejectChanges() Implements _ICachedEntity.RejectChanges
            Using mc As IGetManager = GetMgr()
                If mc Is Nothing Then
                    OrmManager.RejectChanges(Me, GetMappingEngine, Nothing)
                Else
                    mc.Manager.RejectChanges(Me)
                End If
            End Using
        End Sub
    End Class

    <Serializable()> _
    Public MustInherit Class CachedLazyLoad
        Inherits CachedEntity
        Implements IPropertyLazyLoad, IUndoChanges

        Private _loaded As Boolean
        Private _loaded_members As BitArray
        'Private _sver As String
        <NonSerialized()> _
        Private _readRaw As Boolean
        <NonSerialized()> _
        Private _copy As ICachedEntity
        '<NonSerialized()> _
        'Private _old_state As ObjectState

        Public Event OriginalCopyRemoved(ByVal sender As ICachedEntity) Implements IUndoChanges.OriginalCopyRemoved

        Protected Friend Sub RaiseCopyRemoved() Implements IUndoChanges.RaiseOriginalCopyRemoved
            RaiseEvent OriginalCopyRemoved(Me)
        End Sub

        Protected Function Read(ByVal propertyAlias As String) As System.IDisposable Implements IPropertyLazyLoad.Read
            Return OrmManager.RegisterRead(Me, propertyAlias)
        End Function

        Protected Function Read(ByVal propertyAlias As String, ByVal checkEntity As Boolean) As System.IDisposable Implements IPropertyLazyLoad.Read
            Return OrmManager.RegisterRead(Me, propertyAlias, checkEntity)
        End Function

        Protected Function Write(ByVal propertyAlias As String) As System.IDisposable Implements IUndoChanges.Write
            Return OrmManager.RegisterChange(Me, propertyAlias)
        End Function

        Protected Overrides Property IsLoaded() As Boolean Implements IPropertyLazyLoad.IsLoaded
            Get
                Return _loaded
            End Get
            Set(ByVal value As Boolean)
                _loaded = value
            End Set
        End Property

        Protected Property PropertyLoadState As BitArray Implements IPropertyLazyLoad.PropertyLoadState
            Get
                Return _loaded_members
            End Get
            Set(ByVal value As BitArray)
                _loaded_members = value
            End Set
        End Property

        Public Property LazyLoadDisabled As Boolean Implements IPropertyLazyLoad.LazyLoadDisabled
            Get
                Return _readRaw
            End Get
            Set(ByVal value As Boolean)
                _readRaw = value
            End Set
        End Property

        Public Overridable ReadOnly Property DontRaisePropertyChange As Boolean Implements IUndoChanges.DontRaisePropertyChange
            Get
                Return False
            End Get
        End Property

        'Protected Overridable Sub RemoveOriginalCopy(ByVal cache As CacheBase) Implements IUndoChanges.RemoveOriginalCopy
        '    _copy = Nothing
        'End Sub

        Protected Property OriginalCopy() As ICachedEntity Implements IUndoChanges.OriginalCopy
            Get
                'Using SyncHelper(False)
                '    If _copy Is Nothing Then
                '        If (ObjectState = Entities.ObjectState.Modified OrElse Not IsPropertiesLazyLoad) AndAlso ObjectState <> Entities.ObjectState.Created AndAlso ObjectState <> Entities.ObjectState.Deleted Then
                '            Using gm As IGetManager = GetMgr()
                '                _copy = CType(gm.Manager.GetEntityCloneFromStorage(Me), CachedEntity)
                '            End Using
                '        End If
                '    End If

                '    Return _copy
                'End Using
                Return _copy
            End Get
            Set(ByVal value As ICachedEntity)
                _copy = value
            End Set
        End Property

        'Protected Overridable ReadOnly Property HasBodyChanges() As Boolean Implements IUndoChanges.HasBodyChanges
        '    Get
        '        Return ObjectState = Entities.ObjectState.Modified OrElse ObjectState = Entities.ObjectState.Deleted OrElse ObjectState = Entities.ObjectState.Created
        '    End Get
        'End Property

        'Protected Overridable ReadOnly Property HasChanges() As Boolean Implements IUndoChanges.HasChanges
        '    Get
        '        Return HasBodyChanges 'OrElse HasM2MChanges()
        '    End Get
        'End Property

        'Public Property OldObjectState As ObjectState Implements IUndoChanges.OldObjectState
        '    Get
        '        Return _old_state
        '    End Get
        '    Set(ByVal value As ObjectState)
        '        _old_state = value
        '    End Set
        'End Property
    End Class
End Namespace
