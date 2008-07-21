Imports Worm.Orm.Meta
Imports Worm.Cache
Imports System.ComponentModel
Imports System.Collections.Generic
Imports System.Xml

Namespace Orm
    <Serializable()> _
    Public MustInherit Class CachedEntity
        Inherits Entity
        Implements _ICachedEntity

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
        'Protected Friend _upd As IList(Of Worm.Criteria.Core.EntityFilterBase)
        '<NonSerialized()> _
        'Protected Friend _valProcs As Boolean
        '<NonSerialized()> _
        'Protected Friend _needAccept As New Generic.List(Of AcceptState)
        <NonSerialized()> _
        Protected _hasPK As Boolean

        '<EditorBrowsable(EditorBrowsableState.Never)> _
        'Public Class AcceptState

        'End Class

        Public Class ObjectSavedArgs
            Inherits EventArgs

            Private _sa As OrmManagerBase.SaveAction

            Public Sub New(ByVal saveAction As OrmManagerBase.SaveAction)
                _sa = saveAction
            End Sub

            Public ReadOnly Property SaveAction() As OrmManagerBase.SaveAction
                Get
                    Return _sa
                End Get
            End Property
        End Class

        Public Class RelatedObject
            Private _dst As CachedEntity
            Private _props() As String

            Public Sub New(ByVal src As OrmBase, ByVal dst As OrmBase, ByVal properties() As String)
                _dst = dst
                _props = properties
                AddHandler src.Saved, AddressOf Added
            End Sub

            Public Sub Added(ByVal source As CachedEntity, ByVal args As ObjectSavedArgs)
                Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
                Dim oschema As IOrmObjectSchemaBase = mgr.ObjectSchema.GetObjectSchema(_dst.GetType)
                For Each p As String In _props
                    If p = "ID" Then
                        Dim nm As OrmManagerBase.INewObjects = mgr.NewObjectManager
                        If nm IsNot Nothing Then
                            nm.RemoveNew(_dst)
                        End If
                        _dst.SetPK(source.GetPKValues)
                        If nm IsNot Nothing Then
                            mgr.NewObjectManager.AddNew(_dst)
                        End If
                    Else
                        Dim o As Object = source.GetValue(p)
                        Dim c As New ColumnAttribute(p)
                        Dim pi As Reflection.PropertyInfo = mgr.ObjectSchema.GetProperty(_dst.GetType, oschema, c)
                        _dst.SetValue(pi, c, oschema, o)
                    End If
                Next
                RemoveHandler source.Saved, AddressOf Added
            End Sub
        End Class

        Public Event Saved(ByVal sender As CachedEntity, ByVal args As ObjectSavedArgs)
        Public Event Added(ByVal sender As CachedEntity, ByVal args As EventArgs)
        Public Event Deleted(ByVal sender As CachedEntity, ByVal args As EventArgs)
        Public Event Updated(ByVal sender As CachedEntity, ByVal args As EventArgs)
        Public Event OriginalCopyRemoved(ByVal sender As CachedEntity)

        Public ReadOnly Property Key() As Integer Implements ICachedEntity.Key
            Get
                If Not IsPKLoaded Then Throw New OrmObjectException("Object has no primary key")
                Return _key
            End Get
        End Property

        Public Sub CreateCopyForSaveNewEntry() Implements ICachedEntity.CreateCopyForSaveNewEntry
            Dim clone As Entity = CreateClone()
            SetObjectState(Orm.ObjectState.Modified)
            Using mc As IGetManager = GetMgr()
                mc.Manager.Cache.RegisterModification(CType(clone, _ICachedEntity), ModifiedObject.ReasonEnum.Unknown)
            End Using
        End Sub

        Protected MustOverride Function GetCacheKey() As Integer

        Protected Overrides Sub Init(ByVal cache As Cache.OrmCacheBase, ByVal schema As QueryGenerator, ByVal mgrIdentityString As String)
            Throw New NotSupportedException
        End Sub

        Protected Overridable Sub PKLoaded(ByVal pkCount As Integer) Implements _ICachedEntity.PKLoaded
            _key = GetCacheKey()
            _hasPK = True
        End Sub

        Private Function CheckIsAllLoaded(ByVal schema As QueryGenerator, ByVal loadedColumns As Integer) As Boolean Implements _ICachedEntity.CheckIsAllLoaded
            Using SyncHelper(False)
                Dim allloaded As Boolean = True
                If Not _loaded OrElse _loaded_members.Count <= loadedColumns Then
                    Dim arr As Generic.List(Of ColumnAttribute) = schema.GetSortedFieldList(Me.GetType)
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
                    Using mc As IGetManager = GetMgr()
                        Dim arr As Generic.List(Of ColumnAttribute) = mc.Manager.ObjectSchema.GetSortedFieldList(Me.GetType)
                        _loaded_members = New BitArray(arr.Count)
                    End Using
                End If
                Return _loaded_members(idx)
            End Get
            Set(ByVal value As Boolean)
                If _loaded_members Is Nothing Then
                    Using mc As IGetManager = GetMgr()
                        Dim arr As Generic.List(Of ColumnAttribute) = mc.Manager.ObjectSchema.GetSortedFieldList(Me.GetType)
                        _loaded_members = New BitArray(arr.Count)
                    End Using
                End If
                _loaded_members(idx) = value
            End Set
        End Property

        Private Function SetLoaded(ByVal c As Meta.ColumnAttribute, ByVal loaded As Boolean, ByVal check As Boolean) As Boolean Implements _ICachedEntity.SetLoaded

            Dim idx As Integer = c.Index
            If idx = -1 Then
                Using mc As IGetManager = GetMgr()
                    Dim arr As Generic.List(Of ColumnAttribute) = mc.Manager.ObjectSchema.GetSortedFieldList(Me.GetType)
                    idx = arr.BinarySearch(c)
                End Using
                c.Index = idx
            End If

            If idx < 0 AndAlso check Then Throw New OrmObjectException("There is no such field " & c.FieldName)

            If idx >= 0 Then
                Using SyncHelper(False)
                    Dim old As Boolean = _members_load_state(idx)
                    _members_load_state(idx) = loaded
                    Return old
                End Using
            End If
        End Function

        Public Overridable Sub RemoveFromCache(ByVal cache As OrmCacheBase) Implements ICachedEntity.RemoveFromCache

        End Sub

        Public Overrides ReadOnly Property IsLoaded() As Boolean
            Get
                Return _loaded
            End Get
        End Property

        Public Overridable Sub Load() Implements ICachedEntity.Load
            Dim mo As ModifiedObject = OrmCache.Modified(Me)
            'If mo Is Nothing Then mo = _mo
            If mo IsNot Nothing Then
                If mo.User IsNot Nothing Then
                    Using mc As IGetManager = GetMgr()
                        If Not mo.User.Equals(mc.Manager.CurrentUser) Then
                            Throw New OrmObjectException(ObjName & "Object in readonly state")
                        End If
                    End Using
                Else
                    If ObjectState = Orm.ObjectState.Deleted OrElse ObjectState = Orm.ObjectState.Modified Then
                        Throw New OrmObjectException(ObjName & "Cannot load object while its state is deleted or modified!")
                    End If
                End If
            End If
            Dim olds As ObjectState = ObjectState
            Using mc As IGetManager = GetMgr()
                mc.Manager.LoadObject(Me)
            End Using
            If olds = Orm.ObjectState.Created AndAlso ObjectState = Orm.ObjectState.Modified Then
                AcceptChanges(True, True)
            ElseIf IsLoaded Then
                SetObjectState(Orm.ObjectState.None)
            End If
            Invariant()
        End Sub

        <EditorBrowsable(EditorBrowsableState.Never)> _
        <Conditional("DEBUG")> _
        Public Sub Invariant()
            Using SyncHelper(True)
                If IsLoaded AndAlso _
                    ObjectState <> Orm.ObjectState.None AndAlso ObjectState <> Orm.ObjectState.Modified AndAlso ObjectState <> Orm.ObjectState.Deleted Then Throw New OrmObjectException(ObjName & "When object is loaded its state has to be None or Modified or Deleted: current state is " & ObjectState.ToString)
                If Not IsLoaded AndAlso _
                   (ObjectState = Orm.ObjectState.None OrElse ObjectState = Orm.ObjectState.Modified OrElse ObjectState = Orm.ObjectState.Deleted) Then Throw New OrmObjectException(ObjName & "When object is not loaded its state has not be None or Modified or Deleted: current state is " & ObjectState.ToString)
                If ObjectState = Orm.ObjectState.Modified AndAlso OrmCache.Modified(Me) Is Nothing Then
                    'Throw New OrmObjectException(ObjName & "When object is in modified state it has to have an original copy")
                    SetObjectStateClear(Orm.ObjectState.None)
                    Load()
                End If
            End Using
        End Sub

        Protected Overrides Sub SetObjectState(ByVal value As ObjectState)
            Using SyncHelper(False)
                Debug.Assert(value <> Orm.ObjectState.None OrElse IsLoaded)
                If value = Orm.ObjectState.None AndAlso Not IsLoaded Then
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
                        Dim arr As Generic.List(Of ColumnAttribute) = mc.Manager.ObjectSchema.GetSortedFieldList(Me.GetType)
                        For i As Integer = 0 To arr.Count - 1
                            _members_load_state(i) = True
                        Next
                    ElseIf Not value AndAlso _loaded Then
                        Dim arr As Generic.List(Of ColumnAttribute) = mc.Manager.ObjectSchema.GetSortedFieldList(Me.GetType)
                        For i As Integer = 0 To arr.Count - 1
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
                If ObjectState = Orm.ObjectState.Created OrElse ObjectState = Orm.ObjectState.Clone Then 'OrElse _state = Orm.ObjectState.NotLoaded Then
                    Throw New OrmObjectException(ObjName & "accepting changes allowed in state Modified, deleted or none")
                End If

                Using gmc As IGetManager = GetMgr()
                    Dim mc As OrmManagerBase = gmc.Manager
                    '_valProcs = HasM2MChanges(mc)

                    AcceptRelationalChanges(mc)

                    If ObjectState <> Orm.ObjectState.None Then
                        mo = RemoveVersionData(setState)

                        If _upd.Deleted Then
                            '_valProcs = False
                            If updateCache Then
                                mc.Cache.UpdateCache(mc.ObjectSchema, New CachedEntity() {Me}, mc, AddressOf ClearCacheFlags, Nothing, Nothing)
                                'mc.Cache.UpdateCacheOnDelete(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing)
                            End If
                            Accept_AfterUpdateCacheDelete(Me, mc)
                            RaiseEvent Deleted(Me, EventArgs.Empty)
                        ElseIf _upd.Added Then
                            '_valProcs = False
                            Dim dic As IDictionary = mc.GetDictionary(Me.GetType)
                            Dim o As OrmBase = CType(dic(Key), OrmBase)
                            If (o Is Nothing) OrElse (Not o.IsLoaded AndAlso IsLoaded) Then
                                dic(Key) = Me
                            End If
                            If updateCache Then
                                'mc.Cache.UpdateCacheOnAdd(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing, Nothing)
                                mc.Cache.UpdateCache(mc.ObjectSchema, New CachedEntity() {Me}, mc, AddressOf ClearCacheFlags, Nothing, Nothing)
                            End If
                            Accept_AfterUpdateCacheAdd(Me, mc, mo)
                            RaiseEvent Added(Me, EventArgs.Empty)
                        Else
                            If updateCache Then
                                UpdateCacheAfterUpdate()
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

        Public Overridable ReadOnly Property HasBodyChanges() As Boolean
            Get
                Return ObjectState = Orm.ObjectState.Modified OrElse ObjectState = Orm.ObjectState.Deleted OrElse ObjectState = Orm.ObjectState.Created
            End Get
        End Property

        Public Overridable Function HasM2MChanges(ByVal mgr As OrmManagerBase) As Boolean
            Return False
        End Function

        Protected Friend Shared Sub ClearCacheFlags(ByVal obj As _ICachedEntity, ByVal mc As OrmManagerBase, _
            ByVal contextKey As Object)
            obj.UpdateCtx.Added = False
            obj.UpdateCtx.Deleted = False
        End Sub

        Protected Overrides Sub CorrectStateAfterLoading(ByVal objectWasCreated As Boolean)
            Dim os As ObjectState = ObjectState
            MyBase.CorrectStateAfterLoading(objectWasCreated)
            If objectWasCreated Then
                If os = Orm.ObjectState.Modified Then
                    OrmCache.UnregisterModification(Me)
                End If
            End If
        End Sub

        Protected Function RemoveVersionData(ByVal setState As Boolean) As _ICachedEntity
            Dim mo As _ICachedEntity = Nothing

            'unreg = unreg OrElse _state <> Orm.ObjectState.Created
            If setState Then
                SetObjectStateClear(Orm.ObjectState.None)
                Debug.Assert(IsLoaded)
                If Not IsLoaded Then
                    Throw New OrmObjectException(ObjName & "Cannot set state None while object is not loaded")
                End If
            End If
            'If unreg Then
            mo = CType(OriginalCopy, _ICachedEntity)
            OrmCache.UnregisterModification(Me)
            '_mo = Nothing
            'End If

            Return mo
        End Function

        Protected Friend Sub RaiseCopyRemoved()
            RaiseEvent OriginalCopyRemoved(Me)
        End Sub

        Protected Overridable Sub AcceptRelationalChanges(ByVal mc As OrmManagerBase)
            '_needAccept.Clear()

            'Dim rel As IRelation = mc.ObjectSchema.GetConnectedTypeRelation(t)
            'If rel IsNot Nothing Then
            '    Dim c As New OrmManagerBase.M2MEnum(rel, Me, mc.ObjectSchema)
            '    mc.Cache.ConnectedEntityEnum(t, AddressOf c.Accept)
            'End If
        End Sub

        Friend Shared Sub Accept_AfterUpdateCacheDelete(ByVal obj As CachedEntity, ByVal mc As OrmManagerBase)
            mc._RemoveObjectFromCache(obj)
            mc.Cache.RegisterDelete(obj)
            'obj._needDelete = False
        End Sub

        Friend Shared Sub Accept_AfterUpdateCacheAdd(ByVal obj As CachedEntity, ByVal mc As OrmManagerBase, _
            ByVal contextKey As Object)
            'obj._needAdd = False
            Dim nm As OrmManagerBase.INewObjects = mc.NewObjectManager
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

        Protected Sub _Init(ByVal cache As OrmCacheBase, ByVal schema As QueryGenerator, ByVal mgrIdentityString As String)
            MyBase.Init(cache, schema, mgrIdentityString)
            If schema IsNot Nothing Then
                Dim arr As Generic.List(Of ColumnAttribute) = schema.GetSortedFieldList(Me.GetType)
                _loaded_members = New BitArray(arr.Count)
            End If
        End Sub

        Protected Overridable Sub SetPK(ByVal pk As Pair(Of String, Object)())
            Using m As IGetManager = GetMgr()
                Dim tt As Type = Me.GetType
                Dim oschema As IOrmObjectSchemaBase = m.Manager.ObjectSchema.GetObjectSchema(tt)
                For Each p As Pair(Of String, Object) In pk
                    Dim c As New ColumnAttribute(p.First)
                    SetValue(Nothing, c, oschema, p.Second)
                    SetLoaded(c, True, True)
                Next
            End Using
        End Sub

        Protected Overridable Overloads Sub Init(ByVal pk() As Pair(Of String, Object), ByVal cache As OrmCacheBase, ByVal schema As QueryGenerator, ByVal mgrIdentityString As String) Implements _ICachedEntity.Init
            _Init(cache, schema, mgrIdentityString)
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

            With reader
l1:
                If .NodeType = XmlNodeType.Element AndAlso .Name = t.Name Then
                    ReadValues(reader)

                    Do While .Read
                        Select Case .NodeType
                            Case XmlNodeType.Element
                                ReadValue(.Name, reader)
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

            Using mc As IGetManager = GetMgr()
                Dim schema As QueryGenerator = mc.Manager.ObjectSchema
                If schema IsNot Nothing Then CheckIsAllLoaded(schema, Integer.MaxValue)
            End Using
        End Sub

        Protected Overridable Sub WriteXml(ByVal writer As System.Xml.XmlWriter) Implements System.Xml.Serialization.IXmlSerializable.WriteXml
            With writer
                Dim t As Type = Me.GetType

                Dim elems As New Generic.List(Of Pair(Of String, Object))
                Dim xmls As New Generic.List(Of Pair(Of String, String))

                For Each de As DictionaryEntry In OrmSchema.GetProperties(t)
                    Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                    Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                    If c IsNot Nothing AndAlso (OrmSchema.GetAttributes(t, c) And Field2DbRelations.Private) = 0 Then
                        If IsLoaded Then
                            Dim v As Object = pi.GetValue(Me, Nothing)
                            Dim tt As Type = pi.PropertyType
                            If v IsNot Nothing Then
                                If GetType(OrmBase).IsAssignableFrom(tt) Then
                                    .WriteAttributeString(c.FieldName, CType(v, OrmBase).Identifier.ToString)
                                ElseIf tt.IsArray Then
                                    elems.Add(New Pair(Of String, Object)(c.FieldName, pi.GetValue(Me, Nothing)))
                                ElseIf tt Is GetType(XmlDocument) Then
                                    xmls.Add(New Pair(Of String, String)(c.FieldName, CType(pi.GetValue(Me, Nothing), XmlDocument).OuterXml))
                                Else
                                    .WriteAttributeString(c.FieldName, Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture))
                                End If
                            End If
                        ElseIf (OrmSchema.GetAttributes(t, c) And Field2DbRelations.PK) = Field2DbRelations.PK Then
                            .WriteAttributeString(c.FieldName, pi.GetValue(Me, Nothing).ToString)
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
                '.WriteEndElement() 't.Name
            End With
        End Sub

        Protected Sub ReadValue(ByVal fieldName As String, ByVal reader As XmlReader)
            reader.Read()
            'Dim c As ColumnAttribute = OrmSchema.GetColumnByFieldName(Me.GetType, fieldName)
            Select Case reader.NodeType
                Case XmlNodeType.CDATA
                    Dim pi As Reflection.PropertyInfo = OrmSchema.GetProperty(Me.GetType, fieldName)
                    Dim c As ColumnAttribute = OrmSchema.GetColumnByFieldName(Me.GetType, fieldName)
                    Dim x As New XmlDocument
                    x.LoadXml(reader.Value)
                    pi.SetValue(Me, x, Nothing)
                    SetLoaded(c, True, True)
                Case XmlNodeType.Text
                    Dim pi As Reflection.PropertyInfo = OrmSchema.GetProperty(Me.GetType, fieldName)
                    Dim c As ColumnAttribute = OrmSchema.GetColumnByFieldName(Me.GetType, fieldName)
                    Dim v As String = reader.Value
                    pi.SetValue(Me, Convert.FromBase64String(CStr(v)), Nothing)
                    SetLoaded(c, True, True)
                    'Using ms As New IO.MemoryStream(Convert.FromBase64String(CStr(v)))
                    '    Dim f As New Runtime.Serialization.Formatters.Binary.BinaryFormatter()
                    '    pi.SetValue(Me, f.Deserialize(ms), Nothing)
                    '    SetLoaded(c, True)
                    'End Using
            End Select
        End Sub

        Protected Sub ReadValues(ByVal reader As XmlReader)
            With reader
                .MoveToFirstAttribute()
                Dim t As Type = Me.GetType
                Dim oschema As IOrmObjectSchemaBase = Nothing
                If OrmSchema IsNot Nothing Then
                    oschema = OrmSchema.GetObjectSchema(t)
                End If

                Dim fv As IDBValueFilter = TryCast(oschema, IDBValueFilter)
                Do

                    Dim pi As Reflection.PropertyInfo = OrmSchema.GetProperty(t, oschema, .Name)
                    Dim c As ColumnAttribute = OrmSchema.GetColumnByFieldName(t, .Name)

                    Dim att As Field2DbRelations = OrmSchema.GetAttributes(t, c)
                    'Dim not_pk As Boolean = (att And Field2DbRelations.PK) = 0

                    'Me.IsLoaded = not_pk

                    Dim value As String = .Value
                    If fv IsNot Nothing Then
                        value = CStr(fv.CreateValue(c, Me, value))
                    End If

                    If GetType(OrmBase).IsAssignableFrom(pi.PropertyType) Then
                        'If (att And Field2DbRelations.Factory) = Field2DbRelations.Factory Then
                        '    CreateObject(.Name, value)
                        '    SetLoaded(c, True)
                        'Else
                        Using mc As IGetManager = GetMgr()
                            Dim v As IOrmBase = mc.Manager.GetOrmBaseFromCacheOrCreate(value, pi.PropertyType)
                            If pi IsNot Nothing Then
                                pi.SetValue(Me, v, Nothing)
                                SetLoaded(c, True, True)
                            End If
                        End Using
                        'End If
                    Else
                        Dim v As Object = Convert.ChangeType(value, pi.PropertyType)
                        If pi IsNot Nothing Then
                            pi.SetValue(Me, v, Nothing)
                            SetLoaded(c, True, True)
                        End If
                    End If

                    'If (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                    '    If OrmCache IsNot Nothing Then OrmCache.RegisterCreation(Me.GetType, Identifier)
                    'End If

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

        Public Overridable Function GetPKValues() As Pair(Of String, Object)() Implements ICachedEntity.GetPKValues
            Dim l As New List(Of Pair(Of String, Object))
            Using mc As IGetManager = GetMgr()
                Dim oschema As IOrmObjectSchemaBase = mc.Manager.ObjectSchema.GetObjectSchema(Me.GetType)
                For Each kv As DictionaryEntry In mc.Manager.ObjectSchema.GetProperties(Me.GetType)
                    Dim pi As Reflection.PropertyInfo = CType(kv.Value, Reflection.PropertyInfo)
                    Dim c As ColumnAttribute = CType(kv.Key, ColumnAttribute)
                    l.Add(New Pair(Of String, Object)(c.FieldName, GetValue(pi, c, oschema)))
                Next
            End Using
            Return l.ToArray
        End Function

        Public ReadOnly Property OriginalCopy() As ICachedEntity Implements ICachedEntity.OriginalCopy
            Get
                If OrmCache.Modified(Me) Is Nothing Then Return Nothing
                Return OrmCache.Modified(Me).Obj
            End Get
        End Property

        Public Overridable ReadOnly Property ChangeDescription() As String
            Get
                Dim sb As New StringBuilder
                sb.Append("Аттрибуты:").Append(vbCrLf)
                If ObjectState = Orm.ObjectState.Modified Then
                    For Each c As ColumnAttribute In Changes(OriginalCopy)
                        sb.Append(vbTab).Append(c.FieldName).Append(vbCrLf)
                    Next
                Else
                    Dim t As Type = Me.GetType
                    'Dim o As OrmBase = CType(t.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, _
                    '    New Object() {Identifier, OrmCache, _schema}), OrmBase)
                    'Dim o As OrmBase = GetNew()
                    Dim o As ICachedEntity = CType(CreateObject(), ICachedEntity)
                    For Each c As ColumnAttribute In Changes(o)
                        sb.Append(vbTab).Append(c.FieldName).Append(vbCrLf)
                    Next
                End If
                Return sb.ToString
            End Get
        End Property

        Public ReadOnly Property Changes(ByVal obj As ICachedEntity) As ColumnAttribute()
            Get
                Dim columns As New Generic.List(Of ColumnAttribute)
                Dim t As Type = obj.GetType
                Dim oschema As IOrmObjectSchemaBase = OrmSchema.GetObjectSchema(t)
                For Each de As DictionaryEntry In OrmSchema.GetProperties(t, oschema)
                    Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                    Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                    Dim original As Object = obj.GetValue(pi, c, oschema)
                    If (OrmSchema.GetAttributes(t, c) And Field2DbRelations.ReadOnly) <> Field2DbRelations.ReadOnly Then
                        Dim current As Object = GetValue(pi, c, oschema)
                        If (original IsNot Nothing AndAlso Not original.Equals(current)) OrElse _
                            (current IsNot Nothing AndAlso Not current.Equals(original)) Then
                            columns.Add(c)
                        End If
                    End If
                Next
                Return columns.ToArray
            End Get
        End Property

        Protected Overrides Function CreateObject() As Entity
            Using gm As IGetManager = GetMgr()
                Return CType(gm.Manager.CreateEntity(GetPKValues, Me.GetType), Entity)
            End Using
        End Function

        Protected Overrides Sub _PrepareUpdate()
            If Not IsLoaded Then
                If ObjectState = Orm.ObjectState.None Then
                    Throw New InvalidOperationException(String.Format("Object {0} is not loaded while the state is None", ObjName))
                End If

                If ObjectState = Orm.ObjectState.NotLoaded Then
                    Load()
                    If ObjectState = Orm.ObjectState.NotFoundInSource Then
                        Throw New OrmObjectException(ObjName & "Object is not editable 'cause it is not found in source")
                    End If
                Else
                    Return
                End If
            End If

            Dim mo As ModifiedObject = OrmCache.Modified(Me)
            If mo IsNot Nothing Then
                Using mc As IGetManager = GetMgr()
                    If mo.User IsNot Nothing AndAlso Not mo.User.Equals(mc.Manager.CurrentUser) Then
                        Throw New OrmObjectException(ObjName & "Object has already altered by another user")
                    End If
                End Using
                If ObjectState = Orm.ObjectState.Deleted Then SetObjectState(ObjectState.Modified)
            Else
                Debug.Assert(ObjectState = Orm.ObjectState.None) ' OrElse state = Obm.ObjectState.Created)
                'CreateModified(_id)
                CreateClone4Edit()
                EnsureInCache()
                'If modified.old_state = Obm.ObjectState.Created Then
                '    _mo = mo
                'End If
            End If
        End Sub

        Protected Sub EnsureInCache()
            Using mc As IGetManager = GetMgr()
                mc.Manager.EnsureInCache(Me)
            End Using
        End Sub

        Protected Sub CreateClone4Edit()
            Dim clone As Entity = CreateClone()
            SetObjectState(Orm.ObjectState.Modified)
            Using mc As IGetManager = GetMgr()
                mc.Manager.Cache.RegisterModification(CType(clone, _ICachedEntity), ModifiedObject.ReasonEnum.Edit)
                'OrmCache.RegisterModification(modified).Reason = ModifiedObject.ReasonEnum.Edit
                If Not IsLoading Then
                    mc.Manager.RaiseBeginUpdate(Me)
                End If
            End Using
        End Sub

        Protected Sub CreateClone4Delete()
            Dim clone As Entity = CreateClone()
            SetObjectState(Orm.ObjectState.Modified)
            Using mc As IGetManager = GetMgr()
                mc.Manager.Cache.RegisterModification(CType(clone, _ICachedEntity), ModifiedObject.ReasonEnum.Delete)
            End Using
        End Sub

        Public Function SaveChanges(ByVal AcceptChanges As Boolean) As Boolean Implements ICachedEntity.SaveChanges
            Using mc As IGetManager = GetMgr()
                Return mc.Manager.SaveChanges(Me, AcceptChanges)
            End Using
        End Function

        Public Sub UpdateCache() Implements ICachedEntity.UpdateCache
            Using gmc As IGetManager = GetMgr()
                Dim mc As OrmManagerBase = gmc.Manager
                UpdateCacheAfterUpdate()
                If _upd.Deleted OrElse _upd.Added Then
                    mc.Cache.UpdateCache(mc.ObjectSchema, New ICachedEntity() {Me}, mc, AddressOf ClearCacheFlags, Nothing, Nothing)
                End If
            End Using
        End Sub

        Friend ReadOnly Property IsReadOnly() As Boolean
            Get
                Using SyncHelper(True)
                    If ObjectState = Orm.ObjectState.Modified Then
                        Dim mo As ModifiedObject = OrmCache.Modified(Me)
                        'If mo Is Nothing Then mo = _mo
                        If mo IsNot Nothing Then
                            Using mc As IGetManager = GetMgr()
                                If mo.User IsNot Nothing AndAlso Not mo.User.Equals(mc.Manager.CurrentUser) Then
                                    Return True
                                End If
                            End Using
                        End If
                        'ElseIf state = Obm.ObjectState.Deleted Then
                        'Return True
                    End If
                    Return False
                End Using
            End Get
        End Property

        Public Overridable Sub RejectRelationChanges() Implements ICachedEntity.RejectRelationChanges

        End Sub

        ''' <summary>
        ''' Отмена изменений
        ''' </summary>
        Public Sub RejectChanges() Implements ICachedEntity.RejectChanges
            Using SyncHelper(False)
                RejectRelationChanges()

                If ObjectState = ObjectState.Modified OrElse ObjectState = Orm.ObjectState.Deleted OrElse ObjectState = Orm.ObjectState.Created Then
                    If IsReadOnly Then
                        Throw New OrmObjectException(ObjName & " object in readonly state")
                    End If

                    If OriginalCopy Is Nothing Then
                        If ObjectState <> Orm.ObjectState.Created AndAlso ObjectState <> Orm.ObjectState.Deleted Then
                            Throw New OrmObjectException(ObjName & ": When object is in modified state it has to have an original copy")
                        End If
                        Return
                    End If

                    Dim mo As ModifiedObject = OrmCache.Modified(Me)
                    If ObjectState = Orm.ObjectState.Deleted AndAlso mo.Reason <> ModifiedObject.ReasonEnum.Delete Then
                        'Debug.Assert(False)
                        'Throw New OrmObjectException
                        Return
                    End If

                    If ObjectState = Orm.ObjectState.Modified AndAlso (mo.Reason = ModifiedObject.ReasonEnum.Delete) Then
                        Debug.Assert(False)
                        Throw New OrmObjectException
                    End If

                    'Debug.WriteLine(Environment.StackTrace)
                    '_needAdd = False
                    '_needDelete = False
                    _upd = New UpdateCtx

                    Dim olds As ObjectState = OriginalCopy.GetOldState

                    Dim oldkey As Integer = Key
                    Dim newid() As Pair(Of String, Object) = OriginalCopy.GetPKValues
                    If olds <> Orm.ObjectState.Created Then
                        '_loaded_members = 
                        RevertToOriginalVersion()
                        RemoveVersionData(False)
                    End If
                    SetPK(newid)
#If TraceSetState Then
                    SetObjectState(olds, mo.Reason, mo.StackTrace, mo.DateTime)
#Else
                    SetObjectState(olds)
#End If
                    If ObjectState = Orm.ObjectState.Created Then
                        Dim name As String = Me.GetType.Name
                        Using gmc As IGetManager = GetMgr()
                            Dim mc As OrmManagerBase = gmc.Manager
                            Dim dic As IDictionary = mc.GetDictionary(Me.GetType)
                            If dic Is Nothing Then
                                Throw New OrmObjectException("Collection for " & name & " not exists")
                            End If

                            dic.Remove(oldkey)
                        End Using

                        OrmCache.UnregisterModification(Me)

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

        Protected Friend Overridable Function ValidateNewObject(ByVal mgr As OrmManagerBase) As Boolean
            Return True
        End Function

        Protected Friend Overridable Function ValidateUpdate(ByVal mgr As OrmManagerBase) As Boolean
            Return True
        End Function

        Protected Friend Overridable Function ValidateDelete(ByVal mgr As OrmManagerBase) As Boolean
            Return True
        End Function

        Protected Friend Sub RaiseSaved(ByVal sa As OrmManagerBase.SaveAction)
            RaiseEvent Saved(Me, New ObjectSavedArgs(sa))
        End Sub

        Protected Friend Function Save(ByVal mc As OrmManagerBase) As Boolean
            If IsReadOnly Then
                Throw New OrmObjectException(ObjName & "Object in readonly state")
            End If

            Dim r As Boolean = True
            If ObjectState = Orm.ObjectState.Modified Then
#If TraceSetState Then
                Dim mo As ModifiedObject = mc.Cache.Modified(Me)
                If mo Is Nothing OrElse mo.Reason = ModifiedObject.ReasonEnum.Delete Then
                    Debug.Assert(False)
                    Throw New OrmObjectException
                End If
#End If
                r = mc.UpdateObject(Me)
            ElseIf ObjectState = Orm.ObjectState.Created OrElse ObjectState = Orm.ObjectState.NotFoundInSource Then
                If OriginalCopy IsNot Nothing Then
                    Throw New OrmObjectException(ObjName & " already exists.")
                End If
                Dim o As ICachedEntity = mc.AddObject(Me)
                If o Is Nothing Then
                    r = False
                Else
                    Debug.Assert(ObjectState = Orm.ObjectState.Modified) ' OrElse _state = Orm.ObjectState.None
                    _upd.Added = True
                End If
            ElseIf ObjectState = Orm.ObjectState.Deleted Then
#If TraceSetState Then
                Dim mo As ModifiedObject = mc.Cache.Modified(Me)
                If mo Is Nothing OrElse (mo.Reason <> ModifiedObject.ReasonEnum.Delete AndAlso mo.Reason <> ModifiedObject.ReasonEnum.Edit) Then
                    Debug.Assert(False)
                    Throw New OrmObjectException
                End If
#End If
                mc.DeleteObject(Me)
                _upd.Deleted = True
#If TraceSetState Then
            Else
                Debug.Assert(False)
#End If
            End If
            Return r
        End Function

        Public Overridable Function Delete() As Boolean
            Using SyncHelper(False)
                If ObjectState = Orm.ObjectState.Deleted Then Return False

                If ObjectState = Orm.ObjectState.Clone Then
                    Throw New OrmObjectException(ObjName & "Deleting clone is not allowed")
                End If
                If ObjectState <> Orm.ObjectState.Modified AndAlso ObjectState <> Orm.ObjectState.None AndAlso ObjectState <> Orm.ObjectState.NotLoaded Then
                    Throw New OrmObjectException(ObjName & "Deleting is not allowed for this object")
                End If
                Dim mo As ModifiedObject = OrmCache.Modified(Me)
                'If mo Is Nothing Then mo = _mo
                If mo IsNot Nothing Then
                    Using mc As IGetManager = GetMgr()
                        If mo.User IsNot Nothing AndAlso Not mo.User.Equals(mc.Manager.CurrentUser) Then
                            Throw New OrmObjectException(ObjName & "Object has already altered by user " & mo.User.ToString)
                        End If
                    End Using
                    Debug.Assert(mo.Reason <> ModifiedObject.ReasonEnum.Delete)
                Else
                    If ObjectState = Orm.ObjectState.NotLoaded Then
                        Load()
                        If ObjectState = Orm.ObjectState.NotFoundInSource Then
                            Return False
                        End If
                    End If

                    Debug.Assert(ObjectState <> Orm.ObjectState.Modified)
                    CreateClone4Delete()
                    'OrmCache.Modified(Me).Reason = ModifiedObject.ReasonEnum.Delete
                    'Dim modified As OrmBase = CloneMe()
                    'modified._old_state = modified.ObjectState
                    'modified.ObjectState = ObjectState.Clone
                    'OrmCache.RegisterModification(modified)
                End If
                SetObjectState(ObjectState.Deleted)
                EnsureInCache()
                Using mc As IGetManager = GetMgr()
                    mc.Manager.RaiseBeginDelete(Me)
                End Using
            End Using

            Return True
        End Function

        Public Function EnsureLoaded() As CachedEntity
            'OrmManagerBase.CurrentMediaContent.LoadObject(Me)
            Using mc As IGetManager = GetMgr()
                Return CType(mc.Manager.GetLoadedObjectFromCacheOrDB(Me, mc.Manager.GetDictionary(Me.GetType)), CachedEntity)
            End Using
        End Function

        Public ReadOnly Property CanEdit() As Boolean
            Get
                If ObjectState = Orm.ObjectState.Deleted Then 'OrElse _state = Orm.ObjectState.NotFoundInSource Then
                    Return False
                End If
                If ObjectState = Orm.ObjectState.NotLoaded Then
                    Load()
                End If
                Return ObjectState <> Orm.ObjectState.NotFoundInSource
            End Get
        End Property

        Public ReadOnly Property CanLoad() As Boolean
            Get
                Return ObjectState <> Orm.ObjectState.Deleted AndAlso ObjectState <> Orm.ObjectState.Modified
            End Get
        End Property

        Private ReadOnly Property UpdateCtx() As UpdateCtx Implements _ICachedEntity.UpdateCtx
            Get
                Return _upd
            End Get
        End Property

        Protected Friend Sub UpdateCacheAfterUpdate()
            Dim l As List(Of String) = Nothing
            If _upd.UpdatedFields IsNot Nothing Then
                l = New List(Of String)
                For Each f As Criteria.Core.EntityFilterBase In _upd.UpdatedFields
                    '    Assert(f.Type Is t, "")

                    '    Cache.AddUpdatedFields(obj, f.FieldName)
                    l.Add(f.Template.FieldName)
                Next
            End If
            OrmCache.AddUpdatedFields(Me, l)
            _upd.UpdatedFields = Nothing
        End Sub

        Protected Overridable Function ForseUpdate(ByVal c As ColumnAttribute) As Boolean Implements _ICachedEntity.ForseUpdate
            Return False
        End Function

        Public Overrides Function IsFieldLoaded(ByVal fieldName As String) As Boolean
            Dim c As New ColumnAttribute(fieldName)
            Using mc As IGetManager = GetMgr()
                Dim arr As Generic.List(Of ColumnAttribute) = mc.Manager.ObjectSchema.GetSortedFieldList(Me.GetType)
                Dim idx As Integer = arr.BinarySearch(c)
                If idx < 0 Then Throw New OrmObjectException("There is no such field " & fieldName)
                Return _members_load_state(idx)
            End Using
        End Function

        Protected Overrides Sub PrepareRead(ByVal fieldName As String, ByRef d As System.IDisposable)
            If Not IsLoaded AndAlso (ObjectState = Orm.ObjectState.NotLoaded OrElse ObjectState = Orm.ObjectState.None) Then
                d = SyncHelper(True)
                If Not IsLoaded AndAlso (ObjectState = Orm.ObjectState.NotLoaded OrElse ObjectState = Orm.ObjectState.None) AndAlso Not IsFieldLoaded(fieldName) Then
                    Load()
                End If
            End If
        End Sub
    End Class

End Namespace