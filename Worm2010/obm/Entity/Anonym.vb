Imports System.Collections.Generic
Imports System.ComponentModel
Imports Worm.Entities.Meta
Imports Worm.Cache

Namespace Entities

    <Serializable()> _
    <DefaultProperty("Item")> _
    Public Class AnonymousEntity
        Inherits Entity
        Implements IOptimizedValues, ICustomTypeDescriptor

        Protected _props As New Dictionary(Of String, Object)

        Public Overridable Overloads Function GetValue( _
            ByVal propertyAlias As String, ByVal oschema As Meta.IEntitySchema) As Object Implements IOptimizedValues.GetValueOptimized
            Return _props(propertyAlias)
        End Function

        Public Overridable Sub SetValue( _
            ByVal propertyAlias As String, ByVal oschema As Meta.IEntitySchema, ByVal value As Object) Implements IOptimizedValues.SetValueOptimized
            _props(propertyAlias) = value
        End Sub

        Default Public Property Item(ByVal field As String) As Object
            Get
                Return GetValue(field, Nothing)
            End Get
            Set(ByVal value As Object)
                SetValue(field, Nothing, value)
            End Set
        End Property

        Protected Overrides Function GetValue(ByVal propertyAlias As String) As Object
            Return Me(propertyAlias)
        End Function

        Protected Overrides Sub CopyBody(ByVal from As _IEntity, ByVal [to] As _IEntity)
            For Each p As String In New List(Of String)(_props.Keys)
                CType([to], AnonymousEntity)(p) = CType(from, AnonymousEntity)(p)
            Next
        End Sub

        Protected Overrides Function DumpState() As String
            Dim sb As New StringBuilder
            For Each p As String In _props.Keys
                sb.Append(p).Append("=").Append(Me(p)).Append(";")
            Next
            Return sb.ToString
        End Function

#Region " ICustomTypeDescriptor "
        Protected Class PropDesc
            Inherits PropertyDescriptor

            Private _pi As Reflection.PropertyInfo
            Private _name As String
            Private _t As Type

            Public Sub New(ByVal pi As Reflection.PropertyInfo)
                MyBase.New(pi.Name, Nothing)
            End Sub

            Public Sub New(ByVal name As String, ByVal t As Type)
                MyBase.New(name, Nothing)
                _name = name
                _t = t
            End Sub

            Public Overrides Function CanResetValue(ByVal component As Object) As Boolean
                Return False
            End Function

            Public Overrides ReadOnly Property ComponentType() As System.Type
                Get
                    Return GetType(AnonymousEntity)
                End Get
            End Property

            Public Overrides Function GetValue(ByVal component As Object) As Object
                If _pi Is Nothing Then
                    Return CType(component, AnonymousEntity)(_name)
                Else
                    Return _pi.GetValue(component, Nothing)
                End If
            End Function

            Public Overrides ReadOnly Property IsReadOnly() As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType() As System.Type
                Get
                    If _pi Is Nothing Then
                        Return _t
                    Else
                        Return _pi.PropertyType
                    End If
                End Get
            End Property

            Public Overrides Sub ResetValue(ByVal component As Object)

            End Sub

            Public Overrides Sub SetValue(ByVal component As Object, ByVal value As Object)
                If _pi Is Nothing Then
                    CType(component, AnonymousEntity)(_name) = value
                Else
                    _pi.SetValue(component, value, Nothing)
                End If
            End Sub

            Public Overrides Function ShouldSerializeValue(ByVal component As Object) As Boolean
                Return False
            End Function
        End Class

        Public Function GetAttributes() As System.ComponentModel.AttributeCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetAttributes
            Return New AttributeCollection(Nothing)
        End Function

        Public Function GetClassName() As String Implements System.ComponentModel.ICustomTypeDescriptor.GetClassName
            Return Nothing
        End Function

        Public Function GetComponentName() As String Implements System.ComponentModel.ICustomTypeDescriptor.GetComponentName
            Return Nothing
        End Function

        Public Function GetConverter() As System.ComponentModel.TypeConverter Implements System.ComponentModel.ICustomTypeDescriptor.GetConverter
            Return Nothing
        End Function

        Public Function GetDefaultEvent() As System.ComponentModel.EventDescriptor Implements System.ComponentModel.ICustomTypeDescriptor.GetDefaultEvent
            Return Nothing
        End Function

        Public Function GetDefaultProperty() As System.ComponentModel.PropertyDescriptor Implements System.ComponentModel.ICustomTypeDescriptor.GetDefaultProperty
            Return New PropDesc(Me.GetType.GetProperty("Item"))
        End Function

        Public Function GetEditor(ByVal editorBaseType As System.Type) As Object Implements System.ComponentModel.ICustomTypeDescriptor.GetEditor
            Return Nothing
        End Function

        Public Function GetEvents() As System.ComponentModel.EventDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetEvents
            Return New EventDescriptorCollection(Nothing)
        End Function

        Public Function GetEvents(ByVal attributes() As System.Attribute) As System.ComponentModel.EventDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetEvents
            Return New EventDescriptorCollection(Nothing)
        End Function

        Public Function GetProperties() As System.ComponentModel.PropertyDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetProperties
            Return GetProperties(Nothing)
        End Function

        Private _pdc As PropertyDescriptorCollection

        Public Function GetProperties(ByVal attributes() As System.Attribute) As System.ComponentModel.PropertyDescriptorCollection Implements System.ComponentModel.ICustomTypeDescriptor.GetProperties
            If _pdc Is Nothing Then
                Dim props(_props.Count - 1) As PropertyDescriptor
                Dim i As Integer = 0
                For Each kv As KeyValuePair(Of String, Object) In _props
                    Dim t As Type = Nothing
                    Dim v As Object = kv.Value
                    If v IsNot Nothing Then
                        t = v.GetType
                    End If
                    props(i) = New PropDesc(kv.Key, t)
                    i += 1
                Next
                _pdc = New PropertyDescriptorCollection(props)
            End If

            Return _pdc
        End Function

        Public Function GetPropertyOwner(ByVal pd As System.ComponentModel.PropertyDescriptor) As Object Implements System.ComponentModel.ICustomTypeDescriptor.GetPropertyOwner
            Return Me
        End Function

#End Region

    End Class

    <Serializable()> _
    Public Class AnonymousCachedEntity
        Inherits AnonymousEntity
        Implements _ICachedEntity, IPropertyLazyLoad

        Friend _pk() As String
        Private _hasPK As Boolean
        Private _key As Integer?

        <NonSerialized()> _
        Private _upd As New UpdateCtx
        <NonSerialized()> _
        Private _copy As AnonymousCachedEntity
        <NonSerialized()> _
        Friend _myschema As IEntitySchema
        <NonSerialized()> _
        Private _alterLock As New Object

        Private _loaded As Boolean
        Private _loaded_members As BitArray

        Public Event Added(ByVal sender As ICachedEntity, ByVal args As System.EventArgs) Implements ICachedEntity.Added
        Public Event Deleted(ByVal sender As ICachedEntity, ByVal args As System.EventArgs) Implements ICachedEntity.Deleted
        Public Event OriginalCopyRemoved(ByVal sender As ICachedEntity) Implements ICachedEntity.OriginalCopyRemoved
        Public Event Saved(ByVal sender As ICachedEntity, ByVal args As ObjectSavedArgs) Implements ICachedEntity.Saved
        Public Event Updated(ByVal sender As ICachedEntity, ByVal args As System.EventArgs) Implements ICachedEntity.Updated
        Public Event ChangesAccepted(ByVal sender As ICachedEntity, ByVal args As System.EventArgs) Implements ICachedEntity.ChangesAccepted

        Private Function CheckIsAllLoaded(ByVal schema As ObjectMappingEngine, ByVal loadedColumns As Integer, _
            ByVal map As Collections.IndexedCollection(Of String, MapField2Column)) As Boolean Implements _ICachedEntity.CheckIsAllLoaded
            Using SyncHelper(False)
                Dim allloaded As Boolean = True
                If Not _loaded OrElse _loaded_members.Count <= loadedColumns Then
                    For i As Integer = 0 To _props.Count - 1
                        If Not _members_load_state(i, map, schema) Then
                            allloaded = False
                            Exit For
                        End If
                    Next
                    _loaded = allloaded
                End If
                Return allloaded
            End Using
        End Function

        Public Function ForseUpdate(ByVal propertyAlias As String) As Boolean Implements _ICachedEntity.ForseUpdate
            Return False
        End Function

        Public Overloads Sub Init(ByVal pk() As Meta.PKDesc, ByVal cache As Cache.CacheBase, ByVal schema As ObjectMappingEngine) Implements _ICachedEntity.Init
            MyBase.Init(cache, schema)
            Dim spk As New List(Of String)
            For Each p As PKDesc In pk
                spk.Add(p.PropertyAlias)
                Me(p.PropertyAlias) = p.Value
            Next
            _pk = spk.ToArray
            PKLoaded(pk.Length)
        End Sub

        Public ReadOnly Property IsPKLoaded() As Boolean Implements _ICachedEntity.IsPKLoaded
            Get
                Return _hasPK
            End Get
        End Property

        Public Sub PKLoaded(ByVal pkCount As Integer) Implements _ICachedEntity.PKLoaded
            If _pk Is Nothing Then
                Throw New OrmObjectException("PK is not loaded")
            End If
            _hasPK = True
        End Sub

        Public Sub RaiseCopyRemoved() Implements _ICachedEntity.RaiseCopyRemoved
            RaiseEvent OriginalCopyRemoved(Me)
        End Sub

        Public Sub RaiseSaved(ByVal sa As OrmManager.SaveAction) Implements _ICachedEntity.RaiseSaved
            RaiseEvent Saved(Me, New ObjectSavedArgs(sa))
        End Sub

        Public Function Save(ByVal mc As OrmManager) As Boolean Implements _ICachedEntity.Save
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
            ElseIf (ObjectState = Entities.ObjectState.Modified) Then
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

        Public Sub SetLoaded(ByVal value As Boolean) Implements _ICachedEntity.SetLoaded
            Using SyncHelper(False)
                Using mc As IGetManager = GetMgr()
                    Dim mpe As ObjectMappingEngine = mc.Manager.MappingEngine
                    If value AndAlso Not _loaded Then
                        Dim cnt As Integer = _props.Count
                        For i As Integer = 0 To cnt - 1
                            _members_load_state(i, Nothing, mpe) = True
                        Next
                    ElseIf Not value AndAlso _loaded Then
                        Dim cnt As Integer = _props.Count
                        For i As Integer = 0 To cnt - 1
                            _members_load_state(i, Nothing, mpe) = False
                        Next
                    End If
                    _loaded = value
                    Debug.Assert(_loaded = value)
                End Using
            End Using
        End Sub

        Public Function SetLoaded(ByVal propertyAlias As String, ByVal loaded As Boolean, ByVal check As Boolean, _
            ByVal map As Collections.IndexedCollection(Of String, MapField2Column), ByVal mpe As ObjectMappingEngine) As Boolean Implements _ICachedEntity.SetLoaded
            Dim idx As Integer
            'For Each p As String In _props.Keys
            '    If p = propertyAlias Then
            '        Exit For
            '    End If
            '    idx += 1
            'Next
            idx = map.IndexOf(propertyAlias)
            _members_load_state(idx, map, mpe) = check
        End Function

        'Public Function SetLoaded(ByVal c As EntityPropertyAttribute, ByVal loaded As Boolean, ByVal check As Boolean, ByVal schema As ObjectMappingEngine) As Boolean Implements _ICachedEntity.SetLoaded
        '    Return SetLoaded(c.PropertyAlias, loaded, check, schema)
        'End Function

        Public ReadOnly Property UpdateCtx() As UpdateCtx Implements _ICachedEntity.UpdateCtx
            Get
                Return _upd
            End Get
        End Property

        Public Function AcceptChanges(ByVal updateCache As Boolean, ByVal setState As Boolean) As ICachedEntity Implements ICachedEntity.AcceptChanges
            Dim mo As _ICachedEntity = Nothing
            Using SyncHelper(False)
                If ObjectState = Entities.ObjectState.Created OrElse ObjectState = Entities.ObjectState.Clone Then 'OrElse _state = Orm.ObjectState.NotLoaded Then
                    Throw New OrmObjectException(ObjName & "accepting changes allowed in state Modified, deleted or none")
                End If

                Using gmc As IGetManager = GetMgr()
                    Dim mc As OrmManager = gmc.Manager
                    '_valProcs = HasM2MChanges(mc)

                    'AcceptRelationalChanges(updateCache, mc)

                    If (ObjectState <> Entities.ObjectState.None) Then
                        mo = RemoveVersionData(mc.Cache, mc.MappingEngine, setState)
                        Dim c As OrmCache = TryCast(mc.Cache, OrmCache)
                        If _upd.Deleted Then
                            '_valProcs = False
                            If updateCache AndAlso c IsNot Nothing Then
                                c.UpdateCache(mc.MappingEngine, New Pair(Of _ICachedEntity)() {New Pair(Of _ICachedEntity)(Me, mo)}, mc, AddressOf CachedEntity.ClearCacheFlags, Nothing, Nothing, False, False)
                                'mc.Cache.UpdateCacheOnDelete(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing)
                            End If
                            Accept_AfterUpdateCacheDelete(Me, mc)
                            RaiseEvent Deleted(Me, EventArgs.Empty)
                        ElseIf _upd.Added Then
                            '_valProcs = False
                            Dim dic As IDictionary = mc.GetDictionary(Me.GetType, TryCast(GetEntitySchema(mc.MappingEngine), ICacheBehavior))
                            Dim kw As CacheKey = New CacheKey(Me)
                            Dim o As _ICachedEntity = CType(dic(kw), CachedEntity)
                            If (o Is Nothing) OrElse (Not o.IsLoaded AndAlso IsLoaded) Then
                                dic(kw) = Me
                            End If
                            If updateCache AndAlso c IsNot Nothing Then
                                'mc.Cache.UpdateCacheOnAdd(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing, Nothing)
                                c.UpdateCache(mc.MappingEngine, New Pair(Of _ICachedEntity)() {New Pair(Of _ICachedEntity)(Me, mo)}, mc, AddressOf CachedEntity.ClearCacheFlags, Nothing, Nothing, False, False)
                            End If
                            Accept_AfterUpdateCacheAdd(Me, mc.Cache, mo)
                            RaiseEvent Added(Me, EventArgs.Empty)
                        Else
                            If updateCache Then
                                If c IsNot Nothing Then
                                    c.UpdateCache(mc.MappingEngine, New Pair(Of _ICachedEntity)() {New Pair(Of _ICachedEntity)(Me, mo)}, mc, AddressOf CachedEntity.ClearCacheFlags, Nothing, Nothing, False, True)
                                End If
                                UpdateCacheAfterUpdate(c)
                            End If
                            RaiseEvent Updated(Me, EventArgs.Empty)
                        End If
                        'ElseIf _valProcs AndAlso updateCache Then
                        '    mc.Cache.ValidateSPOnUpdate(Me, Nothing)
                    End If

                    RaiseEvent ChangesAccepted(Me, EventArgs.Empty)
                End Using
            End Using

            Return mo
        End Function

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

        Friend Shared Sub Accept_AfterUpdateCacheDelete(ByVal obj As _ICachedEntity, ByVal mc As OrmManager)
            mc._RemoveObjectFromCache(obj)
            Dim c As OrmCache = TryCast(mc.Cache, OrmCache)
            If c IsNot Nothing Then
                c.RegisterDelete(obj)
            End If
            'obj._needDelete = False
        End Sub

        Friend Shared Sub Accept_AfterUpdateCacheAdd(ByVal obj As _ICachedEntity, ByVal cache As CacheBase, _
            ByVal contextKey As Object)
            'obj._needAdd = False
            Dim nm As INewObjectsStore = cache.NewObjectManager
            If nm IsNot Nothing Then
                Dim mo As _ICachedEntity = TryCast(contextKey, _ICachedEntity)
                If mo Is Nothing Then
                    Dim dic As Generic.Dictionary(Of _ICachedEntity, _ICachedEntity) = TryCast(contextKey, Generic.Dictionary(Of _ICachedEntity, _ICachedEntity))
                    If dic IsNot Nothing Then
                        dic.TryGetValue(obj, mo)
                    End If
                End If
                If mo IsNot Nothing Then
                    nm.RemoveNew(mo)
                End If
            End If
        End Sub

        Public ReadOnly Property ChangeDescription() As String Implements ICachedEntity.ChangeDescription
            Get
                Throw New NotImplementedException
            End Get
        End Property

        Public Sub CreateCopyForSaveNewEntry(ByVal mgr As OrmManager, ByVal pk() As Meta.PKDesc) Implements _ICachedEntity.CreateCopyForSaveNewEntry
            Debug.Assert(_copy Is Nothing)
            Dim clone As AnonymousCachedEntity = CType(CreateClone(), AnonymousCachedEntity)
            clone._myschema = _myschema
            SetObjectState(Entities.ObjectState.Modified)
            _copy = clone
            Dim c As CacheBase = mgr.Cache
            c.RegisterModification(mgr, Me, pk, ObjectModification.ReasonEnum.Unknown, TryCast(GetEntitySchema(mgr.MappingEngine), ICacheBehavior))
            If pk IsNot Nothing Then clone.SetPK(pk, mgr.MappingEngine)
        End Sub

        Public Function GetPKValues() As Meta.PKDesc() Implements ICachedEntity.GetPKValues
            If _pk Is Nothing Then
                Throw New OrmObjectException("PK is not loaded")
            End If
            Dim l As New List(Of PKDesc)
            For Each pk As String In _pk
                l.Add(New PKDesc(pk, Me(pk)))
            Next
            Return l.ToArray
        End Function

        Public ReadOnly Property HasBodyChanges() As Boolean Implements ICachedEntity.HasBodyChanges
            Get
                Return ObjectState = Entities.ObjectState.Modified OrElse ObjectState = Entities.ObjectState.Deleted OrElse ObjectState = Entities.ObjectState.Created
            End Get
        End Property

        Public ReadOnly Property HasChanges() As Boolean Implements ICachedEntity.HasChanges
            Get
                Return HasBodyChanges
            End Get
        End Property

        Friend Sub SetKey(ByVal k As Integer)
            _key = k
        End Sub

        Public ReadOnly Property Key() As Integer Implements ICachedEntity.Key
            Get
                If _key.HasValue Then
                    Return _key.Value
                Else
                    If _pk Is Nothing Then
                        Throw New OrmObjectException("PK is not loaded")
                    End If
                    Dim k As Integer
                    For Each pk As String In _pk
                        k = k Xor Me(pk).GetHashCode
                    Next
                    Return k
                End If
            End Get
        End Property

        Public Overloads Sub Load()
            Load(CStr(Nothing))
        End Sub

        Public Overloads Sub Load(ByVal propertyAlias As String) Implements ICachedEntity.Load
            Using mc As IGetManager = GetMgr()
                If mc Is Nothing Then
                    Throw New InvalidOperationException("OrmManager required")
                End If

                Load(mc.Manager, propertyAlias)
            End Using
        End Sub

        Public ReadOnly Property OriginalCopy() As ICachedEntity Implements ICachedEntity.OriginalCopy
            Get
                Return _copy
            End Get
        End Property

        Public Sub RejectChanges() Implements ICachedEntity.RejectChanges
            Using gmc As IGetManager = GetMgr()
                _RejectChanges(gmc.Manager)
            End Using
        End Sub

        Public Sub RejectRelationChanges(ByVal mgr As OrmManager) Implements ICachedEntity.RejectRelationChanges
            Throw New NotImplementedException
        End Sub

        Public Sub RemoveOriginalCopy(ByVal cache As Cache.CacheBase) Implements ICachedEntity.RemoveOriginalCopy
            Throw New NotImplementedException
        End Sub

        Public Function SaveChanges(ByVal AcceptChanges As Boolean) As Boolean Implements ICachedEntity.SaveChanges
            Throw New NotImplementedException
        End Function

        Public Sub UpdateCache(ByVal mgr As OrmManager, ByVal oldObj As ICachedEntity) Implements _ICachedEntity.UpdateCache
            Dim c As OrmCache = TryCast(mgr.Cache, OrmCache)
            If c IsNot Nothing Then
                c.UpdateCache(mgr.MappingEngine, New Pair(Of _ICachedEntity)() { _
                              New Pair(Of _ICachedEntity)(Me, CType(oldObj, _ICachedEntity))}, _
                    mgr, AddressOf CachedEntity.ClearCacheFlags, Nothing, Nothing, False, _upd.UpdatedFields IsNot Nothing)
            End If
            UpdateCacheAfterUpdate(c)
            For Each el As M2MRelation In New List(Of M2MRelation)(_upd.Relations)
                If c IsNot Nothing Then
                    c.RemoveM2MQueries(el)
                End If
                _upd.Relations.Remove(el)
            Next
        End Sub

        Public Function BeginAlter() As System.IDisposable Implements ICachedEntity.BeginAlter
#If DebugLocks Then
            Return New CSScopeMgr_Debug(_alterLock, "d:\temp")
#Else
            Return New CSScopeMgr(_alterLock)
#End If
        End Function

        Public Function BeginEdit() As System.IDisposable Implements ICachedEntity.BeginEdit
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

        Public Function Delete(ByVal mgr As OrmManager) As Boolean Implements ICachedEntity.Delete
            Return _Delete(mgr, CType(EnsureInCache(mgr), AnonymousCachedEntity))
        End Function

        Protected Shared Function _Delete(ByVal mgr As OrmManager, ByVal obj As AnonymousCachedEntity) As Boolean
            Using obj.SyncHelper(False)
                If obj.ObjectState = Entities.ObjectState.Deleted Then Return False

                If obj.ObjectState = Entities.ObjectState.Clone Then
                    Throw New OrmObjectException(obj.ObjName & "Deleting clone is not allowed")
                End If
                If obj.ObjectState <> Entities.ObjectState.Modified AndAlso obj.ObjectState <> Entities.ObjectState.None AndAlso obj.ObjectState <> Entities.ObjectState.NotLoaded Then
                    Throw New OrmObjectException(obj.ObjName & "Deleting is not allowed for this object")
                End If

                Dim mo As ObjectModification = mgr.Cache.ShadowCopy(obj, TryCast(obj.GetEntitySchema(mgr.MappingEngine), ICacheBehavior))
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
                End If
            End Using

            Return True
        End Function

        Private Overloads Sub _RejectChanges(ByVal mgr As OrmManager) Implements _ICachedEntity.RejectChanges
            Using SyncHelper(False)
                'RejectRelationChanges(mgr)

                If ObjectState = ObjectState.Modified OrElse ObjectState = Entities.ObjectState.Deleted OrElse ObjectState = Entities.ObjectState.Created Then
                    Dim oc As ICachedEntity = OriginalCopy()
                    If ObjectState <> Entities.ObjectState.Deleted Then
                        If oc Is Nothing Then
                            If ObjectState <> Entities.ObjectState.Created Then
                                Throw New OrmObjectException(ObjName & ": When object is in modified state it has to have an original copy")
                            End If
                            Return
                        End If
                    End If

                    Dim mo As ObjectModification = ShadowCopy(mgr)
                    If mo IsNot Nothing Then
                        If mo.User IsNot Nothing AndAlso Not mo.User.Equals(mgr.CurrentUser) Then
                            Throw New OrmObjectException(ObjName & " object in readonly state")
                        End If

                        If ObjectState = Entities.ObjectState.Deleted AndAlso mo.Reason <> ObjectModification.ReasonEnum.Delete Then
                            'Debug.Assert(False)
                            'Throw New OrmObjectException
                            Return
                        End If

                        If ObjectState = Entities.ObjectState.Modified AndAlso (mo.Reason = ObjectModification.ReasonEnum.Delete) Then
                            Debug.Assert(False)
                            Throw New OrmObjectException
                        End If
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
                        RemoveVersionData(mgr.Cache, mgr.MappingEngine, False)
                    End If

                    If newid IsNot Nothing Then
                        SetPK(newid, mgr.MappingEngine)
                    End If

#If TraceSetState Then
                    If mo isnot Nothing then
                        SetObjectState(olds, mo.Reason, mo.StackTrace, mo.DateTime)
                    end if
#Else
                    SetObjectStateClear(olds)
#End If
                    If ObjectState = Entities.ObjectState.Created Then
                        If oldkey.HasValue Then
                            'Using gmc As IGetManager = GetMgr()
                            'Dim mc As OrmManager = gmc.Manager
                            Dim dic As IDictionary = mgr.GetDictionary(Me.GetType, TryCast(GetEntitySchema(mgr.MappingEngine), ICacheBehavior))
                            If dic Is Nothing Then
                                Dim name As String = Me.GetType.Name
                                Throw New OrmObjectException("Collection for " & name & " not exists")
                            End If

                            dic.Remove(oldkey)
                        End If
                        ' End Using

                        mgr.Cache.UnregisterModification(Me, mgr.MappingEngine, TryCast(GetEntitySchema(mgr.MappingEngine), ICacheBehavior))
                        _copy = Nothing
                        _loaded = False
                        '_loaded_members = New BitArray(_loaded_members.Count)
                    End If
                End If
            End Using
        End Sub

        Public Overloads Sub Load(ByVal mgr As OrmManager, Optional ByVal propertyAlias As String = Nothing) Implements _ICachedEntity.Load
            Dim mo As ObjectModification = mgr.Cache.ShadowCopy(Me, TryCast(GetEntitySchema(mgr.MappingEngine), ICacheBehavior))
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
            mgr.LoadObject(Me, propertyAlias)
            'End Using
            If olds = Entities.ObjectState.Created AndAlso ObjectState = Entities.ObjectState.Modified Then
                AcceptChanges(True, True)
            ElseIf IsLoaded Then
                SetObjectState(Entities.ObjectState.None)
            End If
        End Sub

        Protected Overrides ReadOnly Property IsLoaded() As Boolean
            Get
                Return _loaded
            End Get
        End Property

        Protected Overrides Function IsPropertyLoaded(ByVal propertyAlias As String) As Boolean
            Return MyBase.IsPropertyLoaded(propertyAlias)
        End Function

        Protected Function Read(ByVal propertyAlias As String) As System.IDisposable Implements IPropertyLazyLoad.Read
            Return _Read(propertyAlias)
        End Function

        Protected Function Read(ByVal propertyAlias As String, ByVal checkEntity As Boolean) As System.IDisposable Implements IPropertyLazyLoad.Read
            Return _Read(propertyAlias, checkEntity)
        End Function

        Protected Function Write(ByVal propertyAlias As String) As System.IDisposable Implements IPropertyLazyLoad.Write
            Return _Write(propertyAlias)
        End Function

        Protected Overrides Sub PrepareRead(ByVal propertyAlias As String, ByRef d As System.IDisposable)
            If Not _readRaw AndAlso (Not IsLoaded AndAlso (ObjectState = Entities.ObjectState.NotLoaded OrElse ObjectState = Entities.ObjectState.None)) Then
                If Not IsLoaded AndAlso (ObjectState = Entities.ObjectState.NotLoaded OrElse ObjectState = Entities.ObjectState.None) AndAlso Not IsPropertyLoaded(propertyAlias) Then
                    Load(propertyAlias)
                End If
                d = SyncHelper(True)
            End If
        End Sub

        Protected Overrides Sub PrepareUpdate(ByVal mgr As OrmManager)
            If Not IsLoaded Then
                If ObjectState = Entities.ObjectState.None Then
                    Throw New InvalidOperationException(String.Format("Object {0} is not loaded while the state is None", ObjName))
                End If

                If ObjectState = Entities.ObjectState.NotLoaded Then
                    Load(mgr)
                    If ObjectState = Entities.ObjectState.NotFoundInSource Then
                        Throw New OrmObjectException(ObjName & "Object is not editable 'cause it is not found in source")
                    End If
                Else
                    Return
                End If
            End If

            Dim mo As ObjectModification = mgr.Cache.ShadowCopy(Me, TryCast(GetEntitySchema(mgr.MappingEngine), ICacheBehavior))
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

        Protected Sub CreateClone4Edit(ByVal mgr As OrmManager)
            If _copy Is Nothing Then
                Dim clone As Entity = CreateClone()
                SetObjectState(Entities.ObjectState.Modified)
                _copy = CType(clone, AnonymousCachedEntity)
                If Not IsLoading Then
                    Dim mgrLocal As OrmManager = GetCurrent()
                    If mgrLocal IsNot Nothing Then
                        mgrLocal.RaiseBeginUpdate(Me)
                    End If
                End If
            End If
            mgr.Cache.RegisterModification(mgr, Me, ObjectModification.ReasonEnum.Edit, TryCast(GetEntitySchema(mgr.MappingEngine), ICacheBehavior))
        End Sub

        Protected Sub CreateClone4Delete(ByVal mgr As OrmManager)
            SetObjectState(Entities.ObjectState.Deleted)
            mgr.Cache.RegisterModification(mgr, Me, ObjectModification.ReasonEnum.Delete, TryCast(GetEntitySchema(mgr.MappingEngine), ICacheBehavior))
            Dim mgrLocal As OrmManager = GetCurrent()
            If mgrLocal IsNot Nothing Then
                mgrLocal.RaiseBeginDelete(Me)
            End If
        End Sub

        Protected Function EnsureInCache(ByVal mgr As OrmManager) As ICachedEntity
            Return mgr.EnsureInCache(Me, mgr.GetDictionary(Me.GetType, TryCast(GetEntitySchema(mgr.MappingEngine), ICacheBehavior)))
        End Function

        Protected Overrides Function GetEntitySchema(ByVal mpe As ObjectMappingEngine) As IEntitySchema
            If _myschema Is Nothing Then
                'Return mpe.GetEntitySchema(t)
                Throw New InvalidOperationException(String.Format("Schema for type {0} is not set", Me.GetType))
            Else
                Return _myschema
            End If
        End Function

        Public Function ShadowCopy(ByVal mgr As OrmManager) As ObjectModification Implements _ICachedEntity.ShadowCopy
            Return mgr.Cache.ShadowCopy(Me, TryCast(GetEntitySchema(mgr.MappingEngine), ICacheBehavior))
        End Function

        Protected Overridable Sub SetPK(ByVal pk As PKDesc(), ByVal mpe As ObjectMappingEngine)
            Dim schema As IEntitySchema = GetEntitySchema(mpe)
            For Each p As PKDesc In pk
                SetLoaded(p.PropertyAlias, True, True, schema.GetFieldColumnMap, mpe)
            Next
        End Sub

        Protected Sub RevertToOriginalVersion()
            Dim original As ICachedEntity = OriginalCopy
            If original IsNot Nothing Then
                CopyBody(original, Me)
            End If
        End Sub

        Protected Function RemoveVersionData(ByVal cache As CacheBase, _
           ByVal mpe As ObjectMappingEngine, ByVal setState As Boolean) As _ICachedEntity
            Dim mo As _ICachedEntity = Nothing

            If setState Then
                SetObjectStateClear(Entities.ObjectState.None)

                If Not IsLoaded Then
                    Throw New OrmObjectException(ObjName & "Cannot set state None while object is not loaded")
                End If
            End If

            mo = CType(OriginalCopy, _ICachedEntity)
            cache.UnregisterModification(Me, mpe, TryCast(GetEntitySchema(mpe), ICacheBehavior))
            _copy = Nothing

            Return mo
        End Function

        Private Sub InitLoadState(ByVal map As Collections.IndexedCollection(Of String, MapField2Column))
            If _loaded_members Is Nothing Then
                '_loaded_members = New BitArray(GetEntitySchema(mpe).GetFieldColumnMap.Count)
                _loaded_members = New BitArray(map.Count)
            End If
        End Sub

        Protected Property _members_load_state(ByVal idx As Integer, ByVal map As Collections.IndexedCollection(Of String, MapField2Column), _
            ByVal mpe As ObjectMappingEngine) As Boolean
            Get
                InitLoadState(map)
                Return _loaded_members(idx)
            End Get
            Set(ByVal value As Boolean)
                InitLoadState(map)
                _loaded_members(idx) = value
            End Set
        End Property

        Protected Overrides Sub CopyBody(ByVal from As _IEntity, ByVal [to] As _IEntity)
            MyBase.CopyBody(from, [to])
            CType([to], AnonymousCachedEntity)._pk = CType(from, AnonymousCachedEntity)._pk
        End Sub

        Private ReadOnly Property IsReadOnly(ByVal mgr As OrmManager) As Boolean
            Get
                Using SyncHelper(True)
                    If ObjectState = Entities.ObjectState.Modified Then
                        'Using mc As IGetManager = GetMgr()
                        Dim mo As ObjectModification = ShadowCopy(mgr)
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

        Protected Overloads Sub _GetChangedObjectGraph(ByVal gl As System.Collections.Generic.List(Of _ICachedEntity)) Implements _ICachedEntity.FillChangedObjectList
            Throw New NotImplementedException
        End Sub

        Public Function GetChangedObjectGraph() As System.Collections.Generic.List(Of _ICachedEntity) Implements _ICachedEntity.GetChangedObjectList
            Throw New NotImplementedException
        End Function

        Public ReadOnly Property UniqueString() As String Implements IKeyProvider.UniqueString
            Get
                Dim r As New StringBuilder
                For Each pk As String In _pk
                    r.Append(pk).Append(":").Append(Me(pk).ToString)
                Next
                Return r.ToString
            End Get
        End Property
    End Class
End Namespace