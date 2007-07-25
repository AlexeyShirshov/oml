Imports System.Xml
Imports CoreFramework.Threading
Imports CoreFramework.Structures

Namespace Orm

    <Serializable()> _
    Public Class OrmObjectException
        Inherits Exception

        Public Sub New()
            ' Add other code for custom properties here.
        End Sub

        Public Sub New(ByVal message As String)
            MyBase.New(message)
            ' Add other code for custom properties here.
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

    ''' <summary>
    ''' Базовый класс для Orm
    ''' </summary>
    <Serializable()> _
    Public MustInherit Class OrmBase
        Implements icomparable, Serialization.IXmlSerializable

        'Class AcceptState
        '    Public ReadOnly DT As System.Data.DataTable
        '    Public ReadOnly id As String
        '    Public ReadOnly key As String
        '    Public t As Type

        '    Public Sub New(ByVal dt As System.Data.DataTable, ByVal id As String, ByVal key As String, ByVal t As Type)
        '        Me.DT = dt
        '        Me.id = id
        '        Me.key = key
        '        Me.t = t
        '    End Sub

        'End Class

        Public Class AcceptState2
            'Public ReadOnly el As EditableList
            'Public ReadOnly sort As Sort
            'Public added As Generic.List(Of Integer)

            Private _key As String
            Private _id As String
            Private _e As OrmManagerBase.M2MCache
            'Public Sub New(ByVal el As EditableList, ByVal sort As Sort, ByVal key As String, ByVal id As String)
            '    Me.el = el
            '    Me.sort = sort
            '    _key = key
            '    _id = id
            'End Sub

            Public ReadOnly Property el() As EditableList
                Get
                    Return _e.Entry
                End Get
            End Property

            Public Sub New(ByVal e As OrmManagerBase.M2MCache, ByVal key As String, ByVal id As String)
                _e = e
                _key = key
                _id = id
            End Sub

            Public Function Accept2(ByVal obj As OrmBase, ByVal mgr As OrmDBManager) As Boolean
                If _e IsNot Nothing Then
                    Dim leave As Boolean = _e.Entry.Accept(mgr) AndAlso _e.Filter Is Nothing
                    If Not leave Then
                        Dim dic As IDictionary = OrmManagerBase.GetDic(mgr.Cache, _key)
                        dic.Remove(_id)
                    End If
                End If
                'If el IsNot Nothing Then
                '    If Not el.Accept(mgr, Sort) Then
                '        Return False
                '    End If
                'End If
                'For Each o As Pair(Of OrmManagerBase.M2MCache, Pair(Of String, String)) In mgr.Cache.GetM2MEtries(obj, Nothing)
                '    Dim m As OrmManagerBase.M2MCache = o.First
                '    If m.Entry.SubType Is el.SubType AndAlso m.Filter IsNot Nothing Then
                '        Dim dic As IDictionary = OrmManagerBase.GetDic(mgr.Cache, o.Second.First)
                '        dic.Remove(o.Second.Second)
                '    End If
                'Next
                Return True
            End Function
        End Class

        ''' <summary>
        ''' Состояние объекта
        ''' </summary>
        Private _state As ObjectState = ObjectState.Created
        ''' <summary>
        ''' Загружен ли объект полностью
        ''' </summary>
        Private _loaded As Boolean
        ''' <summary>
        ''' Идентификатор объекта
        ''' </summary>
        Private _id As Integer
        '<NonSerialized()> _
        'Protected cache As MediaCacheBase
        <NonSerialized()> _
        Friend _old_state As ObjectState
        '<NonSerialized()> _
        'Private _mo As ModifiedObject
        Private _loaded_members As BitArray
        '<NonSerialized()> _
        'Private _rw As System.Threading.ReaderWriterLock
        Public Const ObmNamespace As String = "http://www.worm.ru/orm/"
        <NonSerialized()> _
        Friend _loading As Boolean
        <NonSerialized()> _
        Protected Friend _needAdd As Boolean
        <NonSerialized()> _
        Protected Friend _needDelete As Boolean
        <NonSerialized()> _
        Protected Friend _needAccept As New Generic.List(Of AcceptState2)

        'for xml serialization
        Public Sub New()
            'Dim arr As Generic.List(Of ColumnAttribute) = OrmManagerBase.CurrentMediaContent.DatabaseSchema.GetSortedFieldList(Me.GetType)
            'members_load_state = New BitArray(arr.Count)

            Init()
        End Sub

        Protected Sub New(ByVal id As Integer, ByVal cache As OrmCacheBase, ByVal schema As OrmSchemaBase)
            Init(id, cache, schema)
            Init()
        End Sub

        Friend Sub Init(ByVal id As Integer, ByVal cache As OrmCacheBase, ByVal schema As OrmSchemaBase)
            Me._id = id

            If schema IsNot Nothing Then
                Dim arr As Generic.List(Of ColumnAttribute) = schema.GetSortedFieldList(Me.GetType)
                _loaded_members = New BitArray(arr.Count)
            End If

            If cache IsNot Nothing Then cache.RegisterCreation(Me.GetType, id)
        End Sub

        Protected Sub Init()
            '_rw = New System.Threading.ReaderWriterLock
        End Sub

        <Runtime.Serialization.OnDeserialized()> _
        Private Sub Init(ByVal context As Runtime.Serialization.StreamingContext)
            Init()
            If OrmManagerBase.CurrentManager IsNot Nothing AndAlso ObjectState <> Orm.ObjectState.Created Then
                OrmManagerBase.CurrentManager.RegisterInCashe(Me)
            End If
        End Sub

        Protected ReadOnly Property OrmSchema() As OrmSchemaBase
            Get
                Return OrmManagerBase.CurrentManager.ObjectSchema
            End Get
        End Property

        Protected Property _members_load_state(ByVal idx As Integer) As Boolean
            Get
                If _loaded_members Is Nothing Then
                    Dim arr As Generic.List(Of ColumnAttribute) = OrmManagerBase.CurrentManager.ObjectSchema.GetSortedFieldList(Me.GetType)
                    _loaded_members = New BitArray(arr.Count)
                End If
                Return _loaded_members(idx)
            End Get
            Set(ByVal value As Boolean)
                If _loaded_members Is Nothing Then
                    Dim arr As Generic.List(Of ColumnAttribute) = OrmManagerBase.CurrentManager.ObjectSchema.GetSortedFieldList(Me.GetType)
                    _loaded_members = New BitArray(arr.Count)
                End If
                _loaded_members(idx) = value
            End Set
        End Property

        Public Property IsLoaded() As Boolean
            Get
                Return _loaded
            End Get
            Protected Friend Set(ByVal value As Boolean)
                Using SyncHelper(False)
                    If value AndAlso Not _loaded Then
                        Dim arr As Generic.List(Of ColumnAttribute) = OrmManagerBase.CurrentManager.ObjectSchema.GetSortedFieldList(Me.GetType)
                        For i As Integer = 0 To arr.Count - 1
                            _members_load_state(i) = True
                        Next
                    ElseIf Not value AndAlso _loaded Then
                        Dim arr As Generic.List(Of ColumnAttribute) = OrmManagerBase.CurrentManager.ObjectSchema.GetSortedFieldList(Me.GetType)
                        For i As Integer = 0 To arr.Count - 1
                            _members_load_state(i) = False
                        Next
                    End If
                    _loaded = value
                    Debug.Assert(_loaded = value)
                End Using
            End Set
        End Property

        Public Function SetLoaded(ByVal c As ColumnAttribute, ByVal loaded As Boolean, Optional ByVal check As Boolean = True) As Boolean

            Dim idx As Integer = c.Index
            If idx = -1 Then
                Dim arr As Generic.List(Of ColumnAttribute) = OrmManagerBase.CurrentManager.ObjectSchema.GetSortedFieldList(Me.GetType)
                idx = arr.BinarySearch(c)
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

        Public Function CheckIsAllLoaded() As Boolean
            Using SyncHelper(False)
                Dim allloaded As Boolean = True
                If Not _loaded Then
                    Dim arr As Generic.List(Of ColumnAttribute) = OrmManagerBase.CurrentManager.ObjectSchema.GetSortedFieldList(Me.GetType)
                    For i As Integer = 0 To arr.Count - 1
                        If Not _members_load_state(i) Then
                            allloaded = False
                            Exit For
                        End If
                    Next
                    _loaded = allloaded
                End If
                Return allloaded
            End Using
        End Function

        Public ReadOnly Property OrmCache() As OrmCacheBase
            Get
                Return OrmManagerBase.CurrentManager.Cache
            End Get
        End Property

        ''' <summary>
        ''' Идентификатор объекта
        ''' </summary>
        ''' <remarks>Если производный класс имеет составной первичный ключ, это свойство лучше переопределить</remarks>
        <ColumnAttribute("ID", Field2DbRelations.PrimaryKey)> _
        Public Overridable Property Identifier() As Integer
            Get
                'Using SyncHelper(True)
                Return _id
                'End Using
            End Get
            Protected Friend Set(ByVal value As Integer)
                Using SyncHelper(False)
                    If _state = Orm.ObjectState.Created Then
                        CreateModified(value)
                    End If
                    _id = value
                    Debug.Assert(_id = value)
                End Using
            End Set
        End Property

        '''' <summary>
        '''' Объект, на котором можно синхронизировать загрузку
        '''' </summary>
        'Public ReadOnly Property SyncLoad() As Object
        '    Get
        '        Return Me
        '    End Get
        'End Property

        Public Property ObjectState() As ObjectState
            Get
                Return _state
            End Get
            Protected Friend Set(ByVal value As ObjectState)
                Using SyncHelper(False)
                    _state = value
                    Debug.Assert(_state = value)
                End Using
            End Set
        End Property

#Region " Synchronization "

        Friend Function GetSyncRoot() As IDisposable
            Return SyncHelper(False)
        End Function

        Protected Overridable Function SyncHelper(ByVal reader As Boolean) As IDisposable
            Return New CSScopeMgr(Me)
        End Function

        Protected Function Read(ByVal fieldName As String) As IDisposable
            Return SyncHelper(True, fieldName)
        End Function

        Protected Function Write(ByVal fieldName As String) As IDisposable
            Return SyncHelper(False, fieldName)
        End Function

        Protected Friend Function SyncHelper(ByVal reader As Boolean, ByVal fieldName As String) As IDisposable
            Dim err As Boolean = True
            Dim d As IDisposable = New CoreFramework.Threading.BlankSyncHelper(Nothing)
            Try
                If reader Then
                    d = PrepareRead(fieldName, d)
                Else
                    d = SyncHelper(True)
                    PrepareUpdate()
                End If
                err = False
            Finally
                If err Then
                    If d IsNot Nothing Then d.Dispose()
                End If
            End Try

            Return d
        End Function

#End Region

        Protected Sub CheckCash()
            If OrmCache Is Nothing Then
                Throw New OrmObjectException(ObjName & "The object is floating and has not cashe that is needed to perform this operation")
            End If
            If OrmManagerBase.CurrentManager Is Nothing Then
                Throw New OrmObjectException(ObjName & "You have to create MediaContent object to perform this operation")
            End If
        End Sub

        ''' <summary>
        ''' Модифицированная версия объекта
        ''' </summary>
        Protected Friend ReadOnly Property GetModifiedObject() As OrmBase
            Get
                'If _mo Is Nothing Then
                CheckCash()
                If OrmCache.Modified(Me) Is Nothing Then Return Nothing
                Return OrmCache.Modified(Me).Obj
                'Else
                'Return _mo.Obj
                'End If
            End Get
        End Property

        ''' <summary>
        ''' Загрузка объекта из БД
        ''' </summary>
        Public Overridable Sub Load()
            CheckCash()
            'If OrmCache.MediaContent Is Nothing Then
            '    Throw New MediaObjectModelException(ObjName & "You have to create MediaContent object to perform this operation")
            'End If
            Dim mo As ModifiedObject = OrmCache.Modified(Me)
            'If mo Is Nothing Then mo = _mo
            If mo IsNot Nothing Then
                If mo.User IsNot Nothing Then
                    If Not mo.User.Equals(OrmManagerBase.CurrentManager.CurrentUser) Then
                        Throw New OrmObjectException(ObjName & "Object in readonly state")
                    Else
                        'If HasChanges Then
                        '    Throw New OrmObjectException(ObjName & "Object has changes!")
                        'End If
                        RejectChanges()
                        'state = ObjectState.None
                        'OrmCache.UnregisterModification(Me)
                    End If
                End If
            End If
            Dim olds As ObjectState = _state
            OrmManagerBase.CurrentManager.LoadObject(Me)
            If olds = Orm.ObjectState.Created AndAlso _state = Orm.ObjectState.Modified Then
                AcceptChanges(True)
            End If
        End Sub

        Protected Friend Sub Save(ByVal mc As OrmManagerBase)
            CheckCash()
            If IsReadOnly Then
                Throw New OrmObjectException(ObjName & "Object in readonly state")
            End If

            If _state = Orm.ObjectState.Modified Then
                mc.SaveObject(Me)
            ElseIf _state = Orm.ObjectState.Created OrElse _state = Orm.ObjectState.NotFoundInDB Then
                If GetModifiedObject IsNot Nothing Then
                    Throw New OrmObjectException(ObjName & "Object with identifier " & Identifier & " already exists.")
                End If
                mc.Add(Me)
                Debug.Assert(_state = Orm.ObjectState.Modified) ' OrElse _state = Orm.ObjectState.None
                _needAdd = True
                'Debug.WriteLine("need add: " & Me.GetType.Name)
            ElseIf _state = Orm.ObjectState.Deleted Then
                mc.DeleteObject(Me)
                _needDelete = True
            End If
        End Sub

        Public Function Save(ByVal AcceptChanges As Boolean) As Boolean
            Return OrmManagerBase.CurrentManager.SaveAll(Me, AcceptChanges)
        End Function

        ''' <param name="obj">The System.Object to compare with the current System.Object.</param>
        ''' <returns>true if the specified System.Object is equal to the current System.Object; otherwise, false.</returns>
        Public Overloads Overrides Function Equals(ByVal obj As System.Object) As System.Boolean
            If obj Is Nothing Then Return False
            If Me.GetType.IsAssignableFrom(obj.GetType) Then
                Return Equals(CType(obj, OrmBase))
            ElseIf TypeOf obj Is Integer Then
                Return Equals(CInt(obj))
            Else
                Return False
            End If
        End Function

        Public Overridable Overloads Function Equals(ByVal other_id As Integer) As Boolean
            Return Me.Identifier = other_id
        End Function

        Public Overridable Overloads Function Equals(ByVal obj As OrmBase) As Boolean
            If obj Is Nothing Then Return False
            If Me.GetType.IsAssignableFrom(obj.GetType) Then
                Return Me.Identifier = obj.Identifier
            Else
                Return False
            End If
        End Function

        ''' <returns>A hash code for the current System.Object.</returns>
        Public Overrides Function GetHashCode() As System.Int32
            Return Identifier
        End Function

        Public Overrides Function ToString() As String
            Return _id.ToString()
        End Function

        'Public MustOverride ReadOnly Property HasChanges() As Boolean

        'Protected MustOverride Function GetNew() As OrmBase

        Public Sub RejectRelationChanges()
            Using SyncHelper(False)
                Dim t As Type = Me.GetType
                Dim mc As OrmManagerBase = OrmManagerBase.CurrentManager
                Dim rel As IRelation = mc.ObjectSchema.GetConnectedTypeRelation(t)
                If rel IsNot Nothing Then
                    Dim c As New OrmDBManager.M2MEnum(rel, Me, mc.ObjectSchema)
                    mc.Cache.ConnectedEntityEnum(t, AddressOf c.Reject)
                End If

                For Each acs As AcceptState2 In _needAccept
                    If acs.el IsNot Nothing Then
                        acs.el.Reject(True)
                    End If
                Next
                _needAccept.Clear()
            End Using
        End Sub

        ''' <summary>
        ''' Отмена изменений
        ''' </summary>
        Public Sub RejectChanges()
            Using SyncHelper(False)
                RejectRelationChanges()

                If _state = ObjectState.Modified OrElse _state = Orm.ObjectState.Deleted OrElse _state = Orm.ObjectState.Created Then
                    If IsReadOnly Then
                        Throw New OrmObjectException(ObjName & "Object in readonly state")
                    End If
                    'Debug.WriteLine(Environment.StackTrace)
                    _needAdd = False
                    _needDelete = False

                    If GetModifiedObject Is Nothing Then
                        RejectChangesInternal()
                        'OrmManagerBase.WriteError("ModifiedObject is nothing (" & ObjName & ")")
                        Return
                    End If
                    Dim oldid As Integer = Identifier
                    Dim olds As ObjectState = GetModifiedObject._old_state
                    If olds <> Orm.ObjectState.Created Then RejectChangesInternal()
                    _state = Orm.ObjectState.Modified
                    Dim newid As Integer = GetModifiedObject.Identifier
                    AcceptChanges(True)
                    Identifier = newid
                    _state = olds
                    If _state = Orm.ObjectState.Created Then
                        Dim name As String = Me.GetType.Name
                        Dim mc As OrmManagerBase = OrmManagerBase.CurrentManager
                        Dim dic As IDictionary = mc.GetDictionary(Me.GetType)
                        If dic Is Nothing Then
                            Throw New OrmObjectException("Collection for " & name & " not exists")
                        End If

                        dic.Remove(oldid)

                        OrmCache.UnregisterModification(Me)

                        _loaded = False
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
            End Using
        End Sub

        Public Sub AcceptChanges()
            AcceptChanges(True)
        End Sub

        Protected Friend Function AcceptChanges(ByVal updateCache As Boolean) As OrmBase
            CheckCash()
            Dim mo As OrmBase = Nothing
            Using SyncHelper(False)
                Dim t As Type = Me.GetType
                Dim mc As OrmManagerBase = OrmManagerBase.CurrentManager
                'Debug.Write("Accept " & t.Name)
                For Each acs As AcceptState2 In _needAccept
                    acs.Accept2(Me, CType(mc, OrmDBManager))
                    'If Not String.IsNullOrEmpty(acs.id) Then
                    '    mc.ResetAllM2MRelations(acs.id, acs.key)
                    'End If
                    'If acs.t IsNot Nothing Then
                    '    mc.Obj2ObjRelationRemove(Me, acs.t)
                    'End If
                Next
                _needAccept.Clear()

                Dim rel As IRelation = mc.ObjectSchema.GetConnectedTypeRelation(t)
                If rel IsNot Nothing Then
                    Dim c As New OrmDBManager.M2MEnum(rel, Me, mc.ObjectSchema)
                    mc.Cache.ConnectedEntityEnum(t, AddressOf c.Accept)
                    'Dim f As Pair(Of String, Type) = rel.GetFirstType
                    'Dim fv As OrmBase = CType(mc.DatabaseSchema.GetFieldValue(Me, f.First), OrmBase)
                    'If mc.HasTableInCache(fv, f.Second) Then
                    '    mc.Obj2ObjRelationReset(fv, f.Second)
                    '    mc.Obj2ObjRelationRemove(fv, f.Second)
                    'End If

                    'Dim s As Pair(Of String, Type) = rel.GetSecondType
                    'Dim sv As OrmBase = CType(mc.DatabaseSchema.GetFieldValue(Me, s.First), OrmBase)
                    'If mc.HasTableInCache(sv, s.Second) Then
                    '    mc.Obj2ObjRelationReset(sv, s.Second)
                    '    mc.Obj2ObjRelationRemove(sv, s.Second)
                    'End If
                End If
                'Debug.WriteLine(" state is: " & _state)
                If _state <> Orm.ObjectState.None Then
                    'Debug.WriteLine(t.Name & ": " & _needAdd)
                    'If _state = Orm.ObjectState.Deleted Then
                    '    mc.RemoveObjectFromCache(Me)
                    'End If

                    Dim unreg As Boolean = _state <> Orm.ObjectState.Created
                    _state = Orm.ObjectState.None
                    If unreg Then
                        mo = GetModifiedObject
                        OrmCache.UnregisterModification(Me)
                        '_mo = Nothing
                    End If

                    If _needDelete Then
                        If updateCache Then
                            mc.Cache.UpdateCache(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing, Nothing, Nothing)
                            'mc.Cache.UpdateCacheOnDelete(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing)
                            Accept_AfterUpdateCacheDelete(Me, mc)
                        End If
                    ElseIf _needAdd Then
                        Dim dic As IDictionary = mc.GetDictionary(Me.GetType)
                        Dim o As OrmBase = CType(dic(Identifier), OrmBase)
                        If (o Is Nothing) OrElse (Not o.IsLoaded AndAlso IsLoaded) Then
                            dic(Identifier) = Me
                        End If
                        If updateCache Then
                            'mc.Cache.UpdateCacheOnAdd(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing, Nothing)
                            mc.Cache.UpdateCache(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing, Nothing, Nothing)
                            Accept_AfterUpdateCacheAdd(Me, mc, mo)
                        End If
                    End If
                End If
            End Using

            Return mo
        End Function

        'Protected Sub AcceptChanges(ByVal cashe As MediaCacheBase)
        '    Debug.Assert(_state = Obm.ObjectState.Created)
        '    Me.OrmCache = cashe
        '    Using SyncHelper(False)
        '        _state = ObjectState.None
        '    End Using
        'End Sub

        Friend Shared Sub Accept_AfterUpdateCache(ByVal obj As OrmBase, ByVal mc As OrmManagerBase, _
            ByVal contextKey As Object)

            If obj._needDelete Then
                Accept_AfterUpdateCacheDelete(obj, mc)
            ElseIf obj._needAdd Then
                Accept_AfterUpdateCacheAdd(obj, mc, contextKey)
            End If
        End Sub

        Friend Shared Sub Accept_AfterUpdateCacheDelete(ByVal obj As OrmBase, ByVal mc As OrmManagerBase)
            mc.RemoveObjectFromCache(obj)
            obj._needDelete = False
        End Sub

        Friend Shared Sub Accept_AfterUpdateCacheAdd(ByVal obj As OrmBase, ByVal mc As OrmManagerBase, _
            ByVal contextKey As Object)
            obj._needAdd = False
            Dim nm As OrmManagerBase.INewObjects = mc.NewObjectManager
            If nm IsNot Nothing Then
                Dim mo As OrmBase = TryCast(contextKey, OrmBase)
                If mo Is Nothing Then
                    Dim dic As Generic.Dictionary(Of OrmBase, OrmBase) = TryCast(contextKey, Generic.Dictionary(Of OrmBase, OrmBase))
                    If dic IsNot Nothing Then
                        dic.TryGetValue(obj, mo)
                    End If
                End If
                If mo IsNot Nothing Then
                    nm.RemoveNew(mo)
                End If
            End If
        End Sub

        Protected Friend Sub PrepareUpdate()
            If _state = Orm.ObjectState.Clone Then
                Throw New OrmObjectException(ObjName & ": Altering clone is not allowed")
            End If

            If _state = Orm.ObjectState.Deleted Then
                Throw New OrmObjectException(ObjName & ": Altering deleted object is not allowed")
            End If

            If Not _loading Then 'AndAlso ObjectState <> Orm.ObjectState.Deleted Then
                If OrmCache Is Nothing Then
                    Return
                End If

                If Not IsLoaded Then
                    If _state = Orm.ObjectState.None Then
                        Throw New InvalidOperationException
                    End If

                    If _state = Orm.ObjectState.NotLoaded Then
                        Load()
                    Else
                        Return
                    End If
                End If

                Dim mo As ModifiedObject = OrmCache.Modified(Me)
                If mo IsNot Nothing Then
                    If mo.User IsNot Nothing AndAlso Not mo.User.Equals(OrmManagerBase.CurrentManager.CurrentUser) Then
                        Throw New OrmObjectException(ObjName & "Object has already altered by another user")
                    End If
                    If _state = Orm.ObjectState.Deleted Then _state = ObjectState.Modified
                Else
                    Debug.Assert(_state = Orm.ObjectState.None) ' OrElse state = Obm.ObjectState.Created)
                    'CreateModified(_id)
                    CreateModified()
                    'If modified.old_state = Obm.ObjectState.Created Then
                    '    _mo = mo
                    'End If
                End If
            End If
        End Sub

        Protected Friend Function GetFullClone() As OrmBase
            Dim modified As OrmBase = CloneMe()
            modified._old_state = modified.ObjectState
            modified.ObjectState = ObjectState.Clone
            Return modified
        End Function

        Public ReadOnly Property IsReadOnly() As Boolean
            Get
                Using SyncHelper(True)
                    If _state = Orm.ObjectState.Modified Then
                        CheckCash()
                        Dim mo As ModifiedObject = OrmCache.Modified(Me)
                        'If mo Is Nothing Then mo = _mo
                        If mo IsNot Nothing Then
                            If mo.User IsNot Nothing AndAlso Not mo.User.Equals(OrmManagerBase.CurrentManager.CurrentUser) Then
                                Return True
                            End If
                        End If
                        'ElseIf state = Obm.ObjectState.Deleted Then
                        'Return True
                    End If
                    Return False
                End Using
            End Get
        End Property

        Protected Function PrepareRead(ByVal FieldName As String, ByVal d As IDisposable) As IDisposable
            If Not IsLoaded AndAlso (_state = Orm.ObjectState.NotLoaded OrElse _state = Orm.ObjectState.None) Then
                d = SyncHelper(True)
                If Not IsLoaded AndAlso (_state = Orm.ObjectState.NotLoaded OrElse _state = Orm.ObjectState.None) Then
                    Dim c As New ColumnAttribute(FieldName)
                    Dim arr As Generic.List(Of ColumnAttribute) = OrmManagerBase.CurrentManager.ObjectSchema.GetSortedFieldList(Me.GetType)
                    Dim idx As Integer = arr.BinarySearch(c)
                    If idx < 0 Then Throw New OrmObjectException("There is no such field " & c.FieldName)

                    If Not _members_load_state(idx) Then Load()
                End If
            End If
            Return d
        End Function

        'Protected Sub PrepareRead()
        '    If Not IsLoaded AndAlso state = Obm.ObjectState.None Then
        '        '    Dim c As New ColumnAttribute(FieldName)
        '        '    Dim arr As Generic.List(Of ColumnAttribute) = OrmManagerBase.CurrentMediaContent.DatabaseSchema.GetSortedFieldList(Me.GetType)
        '        '    Dim idx As Integer = arr.BinarySearch(c)
        '        '    If idx < 0 Then Throw New MediaObjectModelException("There is no such field " & c.FieldName)

        '        '    If Not members_load_state(idx) Then Load()
        '        Load()
        '    End If
        'End Sub

        Public Overridable Sub Delete()
            Using SyncHelper(False)
                If _state = Orm.ObjectState.Clone Then
                    Throw New OrmObjectException(ObjName & "Deleting clone is not allowed")
                End If
                If _state <> Orm.ObjectState.Modified AndAlso _state <> Orm.ObjectState.None AndAlso _state <> Orm.ObjectState.NotLoaded Then
                    Throw New OrmObjectException(ObjName & "Deleting is not allowed for this object")
                End If
                Dim mo As ModifiedObject = OrmCache.Modified(Me)
                'If mo Is Nothing Then mo = _mo
                If mo IsNot Nothing Then
                    If mo.User IsNot Nothing AndAlso Not mo.User.Equals(OrmManagerBase.CurrentManager.CurrentUser) Then
                        Throw New OrmObjectException(ObjName & "Object has already altered by another user")
                    End If
                Else
                    CreateModified(Identifier)
                    'Dim modified As OrmBase = CloneMe()
                    'modified._old_state = modified.ObjectState
                    'modified.ObjectState = ObjectState.Clone
                    'OrmCache.RegisterModification(modified)
                End If
                _state = ObjectState.Deleted
                OrmManagerBase.CurrentManager.RaiseBeginDelete(Me)
            End Using
        End Sub

        Protected Friend Sub CreateModified(ByVal id As Integer)
            Dim modified As OrmBase = GetSoftClone()
            _state = Orm.ObjectState.Modified
            OrmCache.RegisterModification(modified, id)
            If Not _loading Then
                OrmManagerBase.CurrentManager.RaiseBeginUpdate(Me)
            End If
        End Sub

        Protected Sub CreateModified()
            Dim modified As OrmBase = GetFullClone()
            _state = Orm.ObjectState.Modified
            OrmCache.RegisterModification(modified)
            If Not _loading Then
                OrmManagerBase.CurrentManager.RaiseBeginUpdate(Me)
            End If
        End Sub

        'Public Sub test()
        '    Dim f As New IO.FileInfo("f:\temp\Diagram_1.txt")
        '    f.AppendText().WriteLine("hi!")
        'End Sub

        Protected Function CompareTo(ByVal other As OrmBase) As Integer
            If other Is Nothing Then
                'Throw New MediaObjectModelException(ObjName & "other parameter cannot be nothing")
                Return 1
            End If
            Return Math.Sign(_id - other._id)
        End Function

        Protected Function CompareTo1(ByVal obj As Object) As Integer Implements System.IComparable.CompareTo
            Return CompareTo(TryCast(obj, OrmBase))
        End Function


        Public Shared Operator <>(ByVal obj1 As OrmBase, ByVal obj2 As OrmBase) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then Return False
                'Throw New MediaObjectModelException("obj1 parameter cannot be nothing")
                Return Not obj2.Equals(obj1)
            End If
            Return Not obj1.Equals(obj2)
        End Operator

        Public Shared Operator =(ByVal obj1 As OrmBase, ByVal obj2 As OrmBase) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then Return True
                'Throw New MediaObjectModelException("obj1 parameter cannot be nothing")
                Return obj2.Equals(obj1)
            End If
            Return obj1.Equals(obj2)
        End Operator

        Public Shared Operator <(ByVal obj1 As OrmBase, ByVal obj2 As OrmBase) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then
                    Return False
                End If
                Return True
            End If
            Return obj1.CompareTo(obj2) < 0
        End Operator

        Public Shared Operator >(ByVal obj1 As OrmBase, ByVal obj2 As OrmBase) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then
                    Return False
                End If
                Return False
            End If
            Return obj1.CompareTo(obj2) > 0
        End Operator

        <Conditional("DEBUG")> _
        Public Sub Invariant()
            If IsLoaded AndAlso _
                _state <> Orm.ObjectState.None AndAlso _state <> Orm.ObjectState.Modified AndAlso _state <> Orm.ObjectState.NotLoaded Then Throw New OrmObjectException(ObjName & "When object is loaded its state has to be None or Modified: current state is " & _state.ToString)
        End Sub

        'Public MustOverride Function CreateSortComparer(ByVal sort As String, ByVal sortType As SortType) As IComparer
        'Public MustOverride Function CreateSortComparer(Of T As {OrmBase, New})(ByVal sort As String, ByVal sortType As SortType) As Generic.IComparer(Of T)

        Public ReadOnly Property Changes(ByVal obj As OrmBase) As ColumnAttribute()
            Get
                Dim columns As New Generic.List(Of ColumnAttribute)
                Dim t As Type = obj.GetType
                For Each pi As Reflection.PropertyInfo In Me.GetType.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic)
                    Dim c As ColumnAttribute = CType(Attribute.GetCustomAttribute(pi, GetType(ColumnAttribute), True), ColumnAttribute)
                    If c IsNot Nothing Then
                        Dim original As Object = pi.GetValue(obj, Nothing)
                        If (OrmSchema.GetAttributes(t, c) And Field2DbRelations.ReadOnly) <> Field2DbRelations.ReadOnly Then
                            Dim current As Object = pi.GetValue(Me, Nothing)
                            If (original IsNot Nothing AndAlso Not original.Equals(current)) OrElse _
                                (current IsNot Nothing AndAlso Not current.Equals(original)) Then
                                columns.Add(c)
                            End If
                        End If
                    End If
                Next
                Return columns.ToArray
            End Get
        End Property

#Region " Xml Serialization "

        Public Function GetSchema() As System.Xml.Schema.XmlSchema Implements System.Xml.Serialization.IXmlSerializable.GetSchema
            Return Nothing
        End Function

        Public Overridable Sub ReadXml(ByVal reader As System.Xml.XmlReader) Implements System.Xml.Serialization.IXmlSerializable.ReadXml
            Dim t As Type = Me.GetType

            'If OrmSchema IsNot Nothing Then
            '    Dim arr As Generic.List(Of ColumnAttribute) = OrmSchema.GetSortedFieldList(Me.GetType)
            '    _members_load_state = New BitArray(arr.Count)
            'End If

            _loading = True

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

            _loading = False
            CheckIsAllLoaded()

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
                    SetLoaded(c, True)
                Case XmlNodeType.Text
                    Dim pi As Reflection.PropertyInfo = OrmSchema.GetProperty(Me.GetType, fieldName)
                    Dim c As ColumnAttribute = OrmSchema.GetColumnByFieldName(Me.GetType, fieldName)
                    Dim v As String = reader.Value
                    pi.SetValue(Me, Convert.FromBase64String(CStr(v)), Nothing)
                    SetLoaded(c, True)
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
                Do
                    Dim t As Type = Me.GetType

                    Dim pi As Reflection.PropertyInfo = OrmSchema.GetProperty(t, .Name)
                    Dim c As ColumnAttribute = OrmSchema.GetColumnByFieldName(t, .Name)

                    Dim att As Field2DbRelations = OrmSchema.GetAttributes(t, c)
                    'Dim not_pk As Boolean = (att And Field2DbRelations.PK) = 0

                    'Me.IsLoaded = not_pk

                    If GetType(OrmBase).IsAssignableFrom(pi.PropertyType) Then
                        If (att And Field2DbRelations.Factory) = Field2DbRelations.Factory Then
                            CreateObject(.Name, .Value)
                            SetLoaded(c, True)
                        Else
                            Dim v As OrmBase = OrmManagerBase.CurrentManager.CreateDBObject(CInt(.Value), pi.PropertyType)
                            If pi IsNot Nothing Then
                                pi.SetValue(Me, v, Nothing)
                                SetLoaded(c, True)
                            End If
                        End If
                    Else
                        Dim v As Object = Convert.ChangeType(.Value, pi.PropertyType)
                        If pi IsNot Nothing Then
                            pi.SetValue(Me, v, Nothing)
                            SetLoaded(c, True)
                        End If
                    End If

                    'If (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                    '    If OrmCache IsNot Nothing Then OrmCache.RegisterCreation(Me.GetType, Identifier)
                    'End If

                Loop While .MoveToNextAttribute
            End With
        End Sub

        Public Overridable Sub WriteXml(ByVal writer As System.Xml.XmlWriter) Implements System.Xml.Serialization.IXmlSerializable.WriteXml
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

#End Region

        Public ReadOnly Property ObjName() As String
            Get
                Return Me.GetType.Name & " - " & ObjectState.ToString & " (" & _id & "): "
            End Get
        End Property

        Protected Friend Function GetName() As String
            Return Me.GetType.Name & _id
        End Function

        Protected Friend Function GetOldName(ByVal id As Integer) As String
            Return Me.GetType.Name & id
        End Function

        Friend Overridable Function ForseUpdate(ByVal c As ColumnAttribute) As Boolean
            Return False
        End Function

        Public Function Reload() As OrmBase
            'OrmManagerBase.CurrentMediaContent.LoadObject(Me)
            Return OrmManagerBase.CurrentManager.LoadType(_id, Me.GetType, True, True)
        End Function

        Public Overridable Sub SetValue(ByVal pi As Reflection.PropertyInfo, ByVal c As ColumnAttribute, ByVal value As Object)
            If pi Is Nothing Then
                Throw New ArgumentNullException("pi")
            End If

            pi.SetValue(Me, value, Nothing)
        End Sub

        Public Overridable Function GetValue(ByVal propAlias As String) As Object
            If propAlias = "ID" Then
                'Throw New OrmObjectException("Use Identifier property to get ID")
                Return Identifier
            Else
                Return OrmSchema.GetFieldValue(Me, propAlias, Nothing)
            End If
        End Function

        Public Overridable Function GetValue(ByVal propAlias As String, ByVal schema As IOrmObjectSchemaBase) As Object
            If propAlias = "ID" Then
                'Throw New OrmObjectException("Use Identifier property to get ID")
                Return Identifier
            Else
                Return OrmSchema.GetFieldValue(Me, propAlias, schema)
            End If
        End Function

        Public Overridable Sub CreateObject(ByVal fieldName As String, ByVal value As Object)

        End Sub

        Protected Friend MustOverride Sub CopyBodyInternal(ByVal [from] As OrmBase, ByVal [to] As OrmBase)

        Protected Friend Overridable Sub RemoveFromCache(ByVal cache As OrmCacheBase)

        End Sub

        'Protected Friend Sub CopyBodyInternal(ByVal from As OrmBase, ByVal [to] As OrmBase)
        '    Dim t As Type = GetType(IOrmEditable(Of ))
        '    Dim rt As Type = t.MakeGenericType(New Type() {Me.GetType})
        '    If Not rt.IsAssignableFrom(Me.GetType) Then
        '        rt = t.MakeGenericType(New TypeCode() {GetType(T)})
        '        Throw New OrmObjectException(String.Format("Object {0} must implement IOrmEditable to perform this operation", ObjName))
        '    End If
        '    rt.InvokeMember("CopyBody", _
        '        Reflection.BindingFlags.Public Or Reflection.BindingFlags.Instance Or Reflection.BindingFlags.InvokeMethod, Nothing, _
        '        Me, New Object() {from, [to]})
        'End Sub

        Protected Friend Function AddAccept(ByVal acs As AcceptState2) As Boolean
            Using SyncHelper(False)
                For Each a As AcceptState2 In _needAccept
                    If a.el Is acs.el Then
                        Return False
                    End If
                Next
                _needAccept.Add(acs)
            End Using
            Return True
        End Function

#Region " Helpers "

        Public Function Find(ByVal t As Type, ByVal criteria As CriteriaLink, ByVal sort As Sort, ByVal withLoad As Boolean) As IList
            Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public
            Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("Find", flags, Nothing, Reflection.CallingConventions.Any, _
                New Type() {GetType(CriteriaLink), GetType(Sort), GetType(Boolean)}, Nothing)
            Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
            Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {criteria, sort, withLoad}, Nothing), IList)
        End Function

        Public Function Find(Of T As {New, OrmBase})(ByVal criteria As CriteriaLink, ByVal sort As Sort, ByVal withLoad As Boolean) As Generic.ICollection(Of T)
            Return OrmManagerBase.CurrentManager.FindMany2Many2(Of T)(Me, criteria, sort, True, withLoad)
        End Function

        Public Function Find(Of T As {New, OrmBase})(ByVal criteria As CriteriaLink, ByVal sort As Sort, ByVal direct As Boolean, ByVal withLoad As Boolean) As Generic.ICollection(Of T)
            Return OrmManagerBase.CurrentManager.FindMany2Many2(Of T)(Me, criteria, sort, direct, withLoad)
        End Function

        Public Sub Add(ByVal obj As OrmBase)
            OrmManagerBase.CurrentManager.M2MAdd(Me, obj, True)
        End Sub

        Public Sub Add(ByVal obj As OrmBase, ByVal direct As Boolean)
            OrmManagerBase.CurrentManager.M2MAdd(Me, obj, direct)
        End Sub

        Public Sub Delete(ByVal t As Type)
            OrmManagerBase.CurrentManager.M2MDelete(Me, t, True)
        End Sub

        Public Sub Delete(ByVal t As Type, ByVal direct As Boolean)
            OrmManagerBase.CurrentManager.M2MDelete(Me, t, direct)
        End Sub

        Public Sub Delete(ByVal obj As OrmBase)
            OrmManagerBase.CurrentManager.M2MDelete(Me, obj, True)
        End Sub

        Public Sub Delete(ByVal obj As OrmBase, ByVal direct As Boolean)
            OrmManagerBase.CurrentManager.M2MDelete(Me, obj, direct)
        End Sub

        Public Sub Cancel(ByVal t As Type)
            OrmManagerBase.CurrentManager.M2MCancel(Me, t)
        End Sub

#End Region

        Protected MustOverride Function CloneMe() As OrmBase
        Protected MustOverride Sub RejectChangesInternal()
        Protected Friend MustOverride Function GetSoftClone() As OrmBase
    End Class

    Public Enum ObjectState

        Created
        Modified
        ''' <summary>
        ''' Объект загружен из БД
        ''' </summary>
        None
        ''' <summary>
        ''' Попытка загрузить данный обьект из БД не удалась. Это может быть из-за того, что, например, он был удален.
        ''' </summary>
        NotFoundInDB
        ''' <summary>
        ''' Объект является копией редактируемого объекта
        ''' </summary>
        Clone
        Deleted
        ''' <summary>
        ''' Специальное состояние, между Created и None, когда объект ожидается что есть в базе, но еще не загружен
        ''' </summary>
        NotLoaded
    End Enum

    <Serializable()> _
    Public MustInherit Class OrmBaseT(Of T As {New, OrmBaseT(Of T)})
        Inherits OrmBase
        Implements IComparable(Of T), ICloneable

        Protected Sub New()

        End Sub

        Protected Sub New(ByVal id As Integer, ByVal cache As OrmCacheBase, ByVal schema As OrmSchemaBase)
            MyBase.New(id, cache, schema)
        End Sub

        'Protected Overridable Function GetNew() As T
        '    Return New T()
        'End Function

        Public Shadows Function CompareTo(ByVal other As T) As Integer Implements System.IComparable(Of T).CompareTo
            If other Is Nothing Then
                'Throw New MediaObjectModelException(ObjName & "other parameter cannot be nothing")
                Return 1
            End If
            Return Math.Sign(Identifier - other.Identifier)
        End Function

        Public Overridable ReadOnly Property ChangeDescription() As String
            Get
                Dim sb As New StringBuilder
                sb.Append("Аттрибуты:").Append(vbCrLf)
                If ObjectState = Orm.ObjectState.Modified Then
                    For Each c As ColumnAttribute In Changes(GetModifiedObject)
                        sb.Append(vbTab).Append(c.FieldName).Append(vbCrLf)
                    Next
                Else
                    Dim t As Type = Me.GetType
                    'Dim o As OrmBase = CType(t.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, _
                    '    New Object() {Identifier, OrmCache, _schema}), OrmBase)
                    'Dim o As OrmBase = GetNew()
                    Dim o As OrmBase = CType(Activator.CreateInstance(t), OrmBase)
                    o.Init(Identifier, OrmCache, OrmSchema)
                    For Each c As ColumnAttribute In Changes(o)
                        sb.Append(vbTab).Append(c.FieldName).Append(vbCrLf)
                    Next
                End If
                Return sb.ToString
            End Get
        End Property

        Public Overridable Function Clone() As Object Implements System.ICloneable.Clone
            Dim o As OrmBase = CType(Activator.CreateInstance(Me.GetType), OrmBase)
            o.Init(Identifier, OrmCache, OrmSchema)
            Using SyncHelper(True)
                o.ObjectState = ObjectState
                Dim editable As IOrmEditable(Of T) = TryCast(Me, IOrmEditable(Of T))
                If editable Is Nothing Then
                    Throw New OrmObjectException(String.Format("Object {0} must implement IOrmEditable to perform this operation", ObjName))
                End If
                editable.CopyBody(CType(Me, T), CType(o, T))
            End Using
            Return o
        End Function

        Protected Overrides Function CloneMe() As OrmBase
            Return CType(Clone(), OrmBase)
        End Function

        Protected Overrides Sub RejectChangesInternal()
            Dim modified As OrmBase = GetModifiedObject
            If modified IsNot Nothing Then
                Dim editable As IOrmEditable(Of T) = TryCast(Me, IOrmEditable(Of T))
                If editable Is Nothing Then
                    Throw New OrmObjectException(String.Format("Object {0} must implement IOrmEditable to perform this operation", ObjName))
                End If
                Using SyncHelper(False)
                    editable.CopyBody(CType(modified, T), CType(Me, T))
                End Using
            End If
        End Sub

        Protected Friend Overrides Function GetSoftClone() As OrmBase
            Dim clone As OrmBase = CType(Activator.CreateInstance(Me.GetType), OrmBase)
            clone.Init(Identifier, OrmCache, OrmSchema)
            Dim editable As IOrmEditable(Of T) = TryCast(Me, IOrmEditable(Of T))
            If editable IsNot Nothing Then
                Using SyncHelper(False)
                    editable.CopyBody(CType(Me, T), CType(clone, T))
                End Using
            End If
            clone._old_state = ObjectState
            clone.ObjectState = ObjectState.Clone
            Return clone
        End Function

        Protected Friend Overrides Sub CopyBodyInternal(ByVal from As OrmBase, ByVal [to] As OrmBase)
            Dim editable As IOrmEditable(Of T) = TryCast(Me, IOrmEditable(Of T))
            If editable Is Nothing Then
                Throw New OrmObjectException(String.Format("Object {0} must implement IOrmEditable to perform this operation", ObjName))
            End If
            editable.CopyBody(CType(from, T), CType([to], T))
        End Sub
    End Class
End Namespace
