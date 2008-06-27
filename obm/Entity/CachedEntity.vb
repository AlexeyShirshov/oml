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
        Protected Friend _needAdd As Boolean
        <NonSerialized()> _
        Protected Friend _needDelete As Boolean
        <NonSerialized()> _
        Protected Friend _upd As IList(Of Worm.Criteria.Core.EntityFilterBase)
        <NonSerialized()> _
        Protected Friend _valProcs As Boolean
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

        Public Event Saved(ByVal sender As CachedEntity, ByVal args As ObjectSavedArgs)
        Public Event Added(ByVal sender As CachedEntity, ByVal args As EventArgs)
        Public Event Deleted(ByVal sender As CachedEntity, ByVal args As EventArgs)
        Public Event Updated(ByVal sender As CachedEntity, ByVal args As EventArgs)
        Public Event OriginalCopyRemoved(ByVal sender As CachedEntity)

        Public ReadOnly Property Key() As Integer Implements ICachedEntity.Key
            Get
                Return _key
            End Get
        End Property

        Public Sub CreateCopyForSaveNewEntry() Implements ICachedEntity.CreateCopyForSaveNewEntry

        End Sub

        Protected MustOverride Function GetCacheKey() As Integer

        Protected Overrides Sub Init(ByVal cache As Cache.OrmCacheBase, ByVal schema As QueryGenerator, ByVal mgrIdentityString As String)
            MyBase.Init(cache, schema, mgrIdentityString)
            If schema IsNot Nothing Then
                Dim arr As Generic.List(Of ColumnAttribute) = schema.GetSortedFieldList(Me.GetType)
                _loaded_members = New BitArray(arr.Count)
            End If
        End Sub

        Private Sub PKLoaded() Implements _ICachedEntity.PKLoaded
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

        Public ReadOnly Property IsLoaded() As Boolean Implements ICachedEntity.IsLoaded
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

                Debug.Assert(Not _needDelete)
                If _needDelete Then
                    Throw New OrmObjectException(String.Format("Cannot set state while object {0} is in the middle of saving changes", ObjName))
                End If

                MyBase.SetObjectState(value)
            End Using
        End Sub

        Friend Overrides ReadOnly Property ObjName() As String
            Get
                Return Me.GetType.Name & " - " & ObjectState.ToString & " (" & _key & "): "
            End Get
        End Property

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

        Protected Friend Function AcceptChanges(ByVal updateCache As Boolean, ByVal setState As Boolean) As _ICachedEntity
            Dim mo As _ICachedEntity = Nothing
            Using SyncHelper(False)
                If ObjectState = Orm.ObjectState.Created OrElse ObjectState = Orm.ObjectState.Clone Then 'OrElse _state = Orm.ObjectState.NotLoaded Then
                    Throw New OrmObjectException(ObjName & "accepting changes allowed in state Modified, deleted or none")
                End If

                Using gmc As IGetManager = GetMgr()
                    Dim mc As OrmManagerBase = gmc.Manager
                    _valProcs = HasM2MChanges(mc)

                    AcceptRelationalChanges(mc)

                    If ObjectState <> Orm.ObjectState.None Then
                        mo = RemoveVersionData(setState)

                        If _needDelete Then
                            _valProcs = False
                            If updateCache Then
                                mc.Cache.UpdateCache(mc.ObjectSchema, New CachedEntity() {Me}, mc, AddressOf ClearCacheFlags, Nothing, Nothing)
                                'mc.Cache.UpdateCacheOnDelete(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing)
                            End If
                            Accept_AfterUpdateCacheDelete(Me, mc)
                            RaiseEvent Deleted(Me, EventArgs.Empty)
                        ElseIf _needAdd Then
                            _valProcs = False
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
                            If (_upd IsNot Nothing OrElse _valProcs) AndAlso updateCache Then
                                mc.InvalidateCache(Me, CType(_upd, System.Collections.ICollection))
                            End If
                            RaiseEvent Updated(Me, EventArgs.Empty)
                        End If
                    ElseIf _valProcs AndAlso updateCache Then
                        mc.Cache.ValidateSPOnUpdate(Me, Nothing)
                    End If
                End Using
            End Using

            Return mo
        End Function

        Public Overridable Function HasM2MChanges(ByVal mgr As OrmManagerBase) As Boolean
            Return False
        End Function

        Protected Friend Shared Sub ClearCacheFlags(ByVal obj As CachedEntity, ByVal mc As OrmManagerBase, _
            ByVal contextKey As Object)
            obj._needAdd = False
            obj._needDelete = False
        End Sub

        Friend ReadOnly Property OriginalCopy() As _ICachedEntity
            Get
                If OrmCache.Modified(Me) Is Nothing Then Return Nothing
                Return OrmCache.Modified(Me).Obj
            End Get
        End Property

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
            mo = OriginalCopy
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
                            Dim v As OrmBase = mc.Manager.CreateDBObject(CInt(value), pi.PropertyType)
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
    End Class

End Namespace