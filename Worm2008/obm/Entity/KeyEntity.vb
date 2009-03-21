Imports System.Xml
Imports Worm.Sorting
Imports Worm.Cache
Imports Worm.Entities.Meta
Imports Worm.Criteria
Imports System.Collections.Generic
Imports System.ComponentModel
Imports Worm.Criteria.Core
Imports Worm.Query

#Const TraceSetState = False
#Const OLDM2M = True

Namespace Entities

    <Serializable()> _
    Public Class ObjectStateException
        Inherits System.Exception

        Public Sub New()
        End Sub

        Public Sub New(ByVal message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(ByVal message As String, ByVal inner As Exception)
            MyBase.New(message, inner)
        End Sub

        Private Sub New( _
            ByVal info As System.Runtime.Serialization.SerializationInfo, _
            ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
        End Sub
    End Class

    ''' <summary>
    ''' Базовый класс для всех типов
    ''' </summary>
    ''' <remarks>
    ''' Класс является потокобезопасным как на чтение так и на запись.
    ''' Предоставляет следующий функционал:
    ''' XML сериализация/десериализация. Реализована с некоторыми ограничениями. Для изменения поведения необходимо переопределить <see cref="KeyEntity.ReadXml" /> и <see cref="KeyEntity.WriteXml"/>.
    ''' <code lang="vb">Это код</code>
    ''' <example>Это пример</example>
    ''' </remarks>
    <Serializable()> _
    Public MustInherit Class KeyEntity
        Inherits CachedEntity
        Implements _IKeyEntity

#Region " Classes "
        'Public Class ObjectSavedArgs
        '    Inherits EventArgs

        '    Private _sa As OrmManager.SaveAction

        '    Public Sub New(ByVal saveAction As OrmManager.SaveAction)
        '        _sa = saveAction
        '    End Sub

        '    Public ReadOnly Property SaveAction() As OrmManager.SaveAction
        '        Get
        '            Return _sa
        '        End Get
        '    End Property
        'End Class

        'Public Class RelatedObject
        '    Private _dst As CachedEntity
        '    Private _props() As String

        '    Public Sub New(ByVal src As OrmBase, ByVal dst As OrmBase, ByVal properties() As String)
        '        _dst = dst
        '        _props = properties
        '        AddHandler src.Saved, AddressOf Added
        '    End Sub

        '    Public Sub Added(ByVal source As CachedEntity, ByVal args As ObjectSavedArgs)
        '        Dim mgr As OrmManager = OrmManager.CurrentManager
        '        For Each p As String In _props
        '            If p = "ID" Then
        '                Dim nm As OrmManager.INewObjects = mgr.NewObjectManager
        '                If nm IsNot Nothing Then
        '                    nm.RemoveNew(_dst)
        '                End If
        '                _dst. = source._id
        '                If nm IsNot Nothing Then
        '                    mgr.NewObjectManager.AddNew(_dst)
        '                End If
        '            Else
        '                Dim o As Object = source.GetValue(p)
        '                mgr.ObjectSchema.SetFieldValue(_dst, p, o)
        '            End If
        '        Next
        '        RemoveHandler source.Saved, AddressOf Added
        '    End Sub
        'End Class

        <EditorBrowsable(EditorBrowsableState.Never)> _
        Public Class M2MClass
            Private _o As KeyEntity

            Friend Sub New(ByVal o As KeyEntity)
                _o = o
            End Sub

            Protected ReadOnly Property GetMgr() As IGetManager
                Get
                    Return _o.GetMgr
                End Get
            End Property

            Public Function Find(ByVal t As Type, ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As IList
                Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public
                Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("Find", flags, Nothing, Reflection.CallingConventions.Any, _
                    New Type() {GetType(PredicateLink), GetType(Sort), GetType(Boolean)}, Nothing)
                Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
                Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {criteria, sort, withLoad}, Nothing), IList)
            End Function

            Public Function Find(Of T As {New, IKeyEntity})(ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Return mc.Manager.FindMany2Many2(Of T)(_o, criteria, sort, M2MRelationDesc.DirKey, withLoad)
                End Using
            End Function

            Public Function Find(Of T As {New, IKeyEntity})(ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal direct As Boolean, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Return mc.Manager.FindMany2Many2(Of T)(_o, criteria, sort, M2MRelationDesc.GetKey(direct), withLoad)
                End Using
            End Function

            Public Function Find(Of T As {New, IKeyEntity})() As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Return mc.Manager.FindMany2Many2(Of T)(_o, Nothing, Nothing, M2MRelationDesc.DirKey, False)
                End Using
            End Function

            Public Function Find(Of T As {New, IKeyEntity})(ByVal direct As Boolean) As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Return mc.Manager.FindMany2Many2(Of T)(_o, Nothing, Nothing, M2MRelationDesc.GetKey(direct), False)
                End Using
            End Function

            Public Function Find(Of T As {New, IKeyEntity})(ByVal sort As Sort) As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Return mc.Manager.FindMany2Many2(Of T)(_o, Nothing, sort, M2MRelationDesc.DirKey, False)
                End Using
            End Function

            Public Function Find(Of T As {New, IKeyEntity})(ByVal sort As Sort, ByVal direct As Boolean) As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Return mc.Manager.FindMany2Many2(Of T)(_o, Nothing, sort, M2MRelationDesc.GetKey(direct), False)
                End Using
            End Function

            Public Function Find(ByVal top As Integer, ByVal t As Type, ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As IList
                Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public
                Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("Find", flags, Nothing, Reflection.CallingConventions.Any, _
                    New Type() {GetType(PredicateLink), GetType(Sort), GetType(Boolean)}, Nothing)
                Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
                Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {top, criteria, sort, withLoad}, Nothing), IList)
            End Function

            Public Function Find(Of T As {New, IKeyEntity})(ByVal top As Integer, ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Return mc.Manager.FindMany2Many2(Of T)(_o, criteria, sort, M2MRelationDesc.GetKey(True), withLoad, top)
                End Using
            End Function

            Public Function Find(Of T As {New, IKeyEntity})(ByVal top As Integer, ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal direct As Boolean, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Return mc.Manager.FindMany2Many2(Of T)(_o, criteria, sort, M2MRelationDesc.GetKey(direct), withLoad, top)
                End Using
            End Function

            Public Function FindDistinct(ByVal t As Type, ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As IList
                Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public
                Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("FindDistinct", flags, Nothing, Reflection.CallingConventions.Any, _
                    New Type() {GetType(PredicateLink), GetType(Sort), GetType(Boolean)}, Nothing)
                Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
                Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {criteria, sort, withLoad}, Nothing), IList)
            End Function

            Public Function FindDistinct(Of T As {New, IKeyEntity})(ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                'Dim rel As M2MRelation = GetMgr.ObjectSchema.GetM2MRelation(Me.GetType, GetType(T), True)
                'Return GetMgr.FindDistinct(Of T)(rel, criteria, sort, withLoad)
                Return FindDistinct(Of T)(criteria, sort, True, withLoad)
            End Function

            Public Function FindDistinct(Of T As {New, IKeyEntity})(ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal direct As Boolean, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Using mc As IGetManager = GetMgr
                    Dim rel As M2MRelationDesc = mc.Manager.MappingEngine.GetM2MRelation(GetType(T), _o.GetType, direct)
                    Dim crit As PredicateLink = New PropertyPredicate(New EntityUnion(_o.GetType), OrmBaseT.PKName).eq(_o).[and](CType(criteria, PredicateLink))
                    Return mc.Manager.FindDistinct(Of T)(rel, crit, sort, withLoad)
                End Using
            End Function

            Public Sub Add(ByVal obj As _IKeyEntity)
                Using mc As IGetManager = GetMgr
                    mc.Manager.M2MAdd(_o, obj, M2MRelationDesc.DirKey)
                End Using
            End Sub

            Public Sub Add(ByVal obj As _IKeyEntity, ByVal direct As Boolean)
                Using mc As IGetManager = GetMgr
                    mc.Manager.M2MAdd(_o, obj, M2MRelationDesc.GetKey(direct))
                End Using
            End Sub

            Public Sub Delete(ByVal t As Type)
                Using mc As IGetManager = GetMgr
                    mc.Manager.M2MDelete(_o, t, M2MRelationDesc.DirKey)
                End Using
            End Sub

            Public Sub Delete(ByVal t As Type, ByVal direct As Boolean)
                Using mc As IGetManager = GetMgr
                    mc.Manager.M2MDelete(_o, t, M2MRelationDesc.GetKey(direct))
                End Using
            End Sub

            Public Sub Delete(ByVal obj As _IKeyEntity)
                Using mc As IGetManager = GetMgr
                    mc.Manager.M2MDelete(_o, obj, M2MRelationDesc.DirKey)
                End Using
            End Sub

            Public Sub Delete(ByVal obj As _IKeyEntity, ByVal direct As Boolean)
                Using mc As IGetManager = GetMgr
                    mc.Manager.M2MDelete(_o, obj, M2MRelationDesc.GetKey(direct))
                End Using
            End Sub

            Public Sub Cancel(ByVal t As Type)
                Using mc As IGetManager = GetMgr
                    mc.Manager.M2MCancel(_o, t)
                End Using
            End Sub

            Public Sub Merge(Of T As {_IKeyEntity, New})(ByVal col As ReadOnlyList(Of T), ByVal removeNotInList As Boolean)
                If removeNotInList Then
                    For Each o As T In Find(Of T)(Nothing, Nothing, False)
                        If Not col.Contains(o) Then
                            Delete(o)
                        End If
                    Next
                End If
                For Each o As T In col
                    If Not Find(Of T)(Nothing, Nothing, False).Contains(o) Then
                        Add(o)
                    End If
                Next
            End Sub

            Public Sub Merge(Of T As {_IKeyEntity, New})(ByVal col As ReadOnlyList(Of T), ByVal removeNotInList As Boolean, ByVal direct As Boolean)
                If removeNotInList Then
                    For Each o As T In Find(Of T)(Nothing, Nothing, False)
                        If Not col.Contains(o) Then
                            Delete(o, direct)
                        End If
                    Next
                End If
                For Each o As T In col
                    If Not Find(Of T)(Nothing, Nothing, False).Contains(o) Then
                        Add(o, direct)
                    End If
                Next
            End Sub

            Public Function Search(Of T As {IKeyEntity, New})(ByVal text As String) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function Search(Of T As {IKeyEntity, New})(ByVal text As String, ByVal sort As Sort) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function Search(Of T As {IKeyEntity, New})(ByVal text As String, ByVal direct As Boolean) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function Search(Of T As {IKeyEntity, New})(ByVal text As String, ByVal sort As Boolean, ByVal direct As Boolean) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function Search(Of T As {IKeyEntity, New})(ByVal text As String, ByVal criteria As IGetFilter, ByVal sort As Boolean) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function Search(Of T As {IKeyEntity, New})(ByVal text As String, ByVal criteria As IGetFilter, ByVal sort As Boolean, ByVal direct As Boolean) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function GetTable(ByVal t As Type, ByVal key As String) As SourceFragment
                Dim m2m As M2MRelationDesc = _o.MappingEngine.GetM2MRelation(_o.GetType, t, key)
                If m2m Is Nothing Then
                    Throw New ArgumentException(String.Format("Invalid type {0} or key {1}", t.ToString, key))
                Else
                    Return m2m.Table
                End If
            End Function

            Public Function GetTable(ByVal t As Type) As SourceFragment
                Return GetTable(t, Nothing)
            End Function
        End Class

        '<EditorBrowsable(EditorBrowsableState.Never)> _
        'Public Class M2MClassNew
        '    Private _o As OrmBase

        '    Friend Sub New(ByVal o As OrmBase)
        '        _o = o
        '    End Sub

        '    Protected ReadOnly Property GetMgr() As IGetManager
        '        Get
        '            Return _o.GetMgr
        '        End Get
        '    End Property

        '    Public Function Search(Of T As {IOrmBase, New})(ByVal text As String) As Worm.ReadOnlyList(Of T)
        '        Throw New NotImplementedException
        '    End Function

        '    Public Function Search(Of T As {IOrmBase, New})(ByVal text As String, ByVal sort As Sort) As Worm.ReadOnlyList(Of T)
        '        Throw New NotImplementedException
        '    End Function

        '    Public Function Search(Of T As {IOrmBase, New})(ByVal text As String, ByVal direct As Boolean) As Worm.ReadOnlyList(Of T)
        '        Throw New NotImplementedException
        '    End Function

        '    Public Function Search(Of T As {IOrmBase, New})(ByVal text As String, ByVal sort As Boolean, ByVal direct As Boolean) As Worm.ReadOnlyList(Of T)
        '        Throw New NotImplementedException
        '    End Function

        '    Public Function Search(Of T As {IOrmBase, New})(ByVal text As String, ByVal criteria As IGetFilter, ByVal sort As Boolean) As Worm.ReadOnlyList(Of T)
        '        Throw New NotImplementedException
        '    End Function

        '    Public Function Search(Of T As {IOrmBase, New})(ByVal text As String, ByVal criteria As IGetFilter, ByVal sort As Boolean, ByVal direct As Boolean) As Worm.ReadOnlyList(Of T)
        '        Throw New NotImplementedException
        '    End Function

        '    Public Function GetTable(ByVal t As Type, ByVal key As String) As SourceFragment
        '        Using mc As IGetManager = _o.GetMgr()
        '            Dim s As ObjectMappingEngine = mc.Manager.MappingEngine
        '            Dim m2m As M2MRelation = s.GetM2MRelation(_o.GetType, t, key)
        '            If m2m Is Nothing Then
        '                Throw New ArgumentException(String.Format("Invalid type {0} or key {1}", t.ToString, key))
        '            Else
        '                Return m2m.Table
        '            End If
        '        End Using
        '    End Function

        '    Public Function GetTable(ByVal t As Type) As SourceFragment
        '        Return GetTable(t, Nothing)
        '    End Function
        'End Class

        'Private Class ChangedEventHelper
        '    Implements IDisposable

        '    Private _value As Object
        '    Private _fieldName As String
        '    Private _obj As KeyEntity
        '    Private _d As IDisposable

        '    Public Sub New(ByVal obj As KeyEntity, ByVal propertyAlias As String, ByVal d As IDisposable)
        '        _fieldName = propertyAlias
        '        _obj = obj
        '        _value = obj.GetValue(propertyAlias)
        '        _d = d
        '    End Sub

        '    Public Sub Dispose() Implements IDisposable.Dispose
        '        _d.Dispose()
        '        _obj.RaisePropertyChanged(_fieldName, _value)
        '    End Sub
        'End Class

        'Public Class ManagerRequiredArgs
        '    Inherits EventArgs

        '    Private _mgr As OrmManager
        '    Public Property Manager() As OrmManager
        '        Get
        '            Return _mgr
        '        End Get
        '        Set(ByVal value As OrmManager)
        '            _mgr = value
        '        End Set
        '    End Property

        'End Class
#End Region

        '''' <summary>
        '''' Состояние объекта
        '''' </summary>
        ''Private _state As ObjectState = ObjectState.Created
        '''' <summary>
        '''' Загружен ли объект полностью
        '''' </summary>
        'Private _loaded As Boolean
        '''' <summary>
        '''' Идентификатор объекта
        '''' </summary>

        '<NonSerialized()> _
        'Protected cache As MediaCacheBase
        '<NonSerialized()> _
        'Friend _old_state As ObjectState
        '<NonSerialized()> _
        'Private _mo As ModifiedObject
        'Private _loaded_members As BitArray
        '<NonSerialized()> _
        'Private _rw As System.Threading.ReaderWriterLock
        'Public Const OrmNamespace As String = "http://www.worm.ru/orm/"
        <NonSerialized()> _
        Friend _loading As Boolean
        '<NonSerialized()> _
        'Protected Friend _needAdd As Boolean
        '<NonSerialized()> _
        'Protected Friend _needDelete As Boolean
        <NonSerialized()> _
        Protected Friend _needAccept As New Generic.List(Of AcceptState2)
        '<NonSerialized()> _
        'Protected Friend _mgrStr As String
        '<NonSerialized()> _
        'Protected _dontRaisePropertyChange As Boolean
        <NonSerialized()> _
        Protected _saved As Boolean
        '<NonSerialized()> _
        'Protected Friend _upd As IList(Of Worm.Criteria.Core.EntityFilter)
        '<NonSerialized()> _
        'Protected Friend _valProcs As Boolean
        <NonSerialized()> _
        Protected Friend _relations As New List(Of Relation)

        '        Public Event Saved(ByVal sender As OrmBase, ByVal args As ObjectSavedArgs)
        '        Public Event Added(ByVal sender As OrmBase, ByVal args As EventArgs)
        '        Public Event Deleted(ByVal sender As OrmBase, ByVal args As EventArgs)
        '        Public Event Updated(ByVal sender As OrmBase, ByVal args As EventArgs)
        '        Public Event PropertyChanged(ByVal sender As OrmBase, ByVal args As PropertyChangedEventArgs)
        '        Public Event OriginalCopyRemoved(ByVal sender As OrmBase)
        '        Public Event ManagerRequired(ByVal sender As OrmBase, ByVal args As ManagerRequiredArgs)

        '#If DEBUG Then
        '        Protected Event ObjectStateChanged(ByVal oldState As ObjectState)
        '#End If
        'for xml serialization

#Region " Protected functions "
        'Protected Friend ReadOnly Property HasBodyChanges() As Boolean
        '    Get
        '        Return _state = Orm.ObjectState.Modified OrElse _state = Orm.ObjectState.Deleted OrElse _state = Orm.ObjectState.Created
        '    End Get
        'End Property


        Protected Overrides ReadOnly Property HasM2MChanges() As Boolean
            Get
                If _needAccept IsNot Nothing AndAlso _needAccept.Count > 0 Then
                    Return True
                End If

                For Each r As Relation In _relations
                    Dim el As M2MRelation = TryCast(r, M2MRelation)
                    If el IsNot Nothing Then
                        If el.HasChanges Then
                            Return True
                        End If
                    End If
                Next
                Return MyBase.HasM2MChanges()
            End Get
        End Property

        'Protected Friend ReadOnly Property HasM2MChanges() As Boolean
        '    Get
        '        Return _needAccept IsNot Nothing AndAlso _needAccept.Count > 0
        '    End Get
        'End Property

        'Friend Property IsLoaded() As Boolean
        '    Get
        '        Return _loaded
        '    End Get
        '    Set(ByVal value As Boolean)
        '        Using SyncHelper(False)
        '            Using mc As IGetManager = GetMgr()
        '                If value AndAlso Not _loaded Then
        '                    Dim arr As Generic.List(Of ColumnAttribute) = mc.Manager.ObjectSchema.GetSortedFieldList(Me.GetType)
        '                    For i As Integer = 0 To arr.Count - 1
        '                        _members_load_state(i) = True
        '                    Next
        '                ElseIf Not value AndAlso _loaded Then
        '                    Dim arr As Generic.List(Of ColumnAttribute) = mc.Manager.ObjectSchema.GetSortedFieldList(Me.GetType)
        '                    For i As Integer = 0 To arr.Count - 1
        '                        _members_load_state(i) = False
        '                    Next
        '                End If
        '                _loaded = value
        '                Debug.Assert(_loaded = value)
        '            End Using
        '        End Using
        '    End Set
        'End Property

        'Friend ReadOnly Property OrmCache() As OrmCache
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

        'Private ReadOnly Property _os() As ObjectState Implements IEntity.ObjectState
        '    Get
        '        Return ObjectState
        '    End Get
        'End Property

        '        Protected Friend Property ObjectState() As ObjectState
        '            Get
        '                Return _state
        '            End Get
        '            Set(ByVal value As ObjectState)
        '                Using SyncHelper(False)
        '                    Debug.Assert(_state <> Orm.ObjectState.Deleted)
        '                    If _state = Orm.ObjectState.Deleted Then
        '                        Throw New OrmObjectException(String.Format("Cannot set state while object {0} is in the middle of saving changes", ObjName))
        '                    End If

        '                    Debug.Assert(Not _needDelete)
        '                    If _needDelete Then
        '                        Throw New OrmObjectException(String.Format("Cannot set state while object {0} is in the middle of saving changes", ObjName))
        '                    End If

        '                    Debug.Assert(value <> Orm.ObjectState.None OrElse IsLoaded)
        '                    If value = Orm.ObjectState.None AndAlso Not IsLoaded Then
        '                        Throw New OrmObjectException(String.Format("Cannot set state none while object {0} is not loaded", ObjName))
        '                    End If

        '                    Dim olds As ObjectState = _state
        '                    _state = value
        '                    Debug.Assert(_state = value)

        '#If DEBUG Then
        '                    RaiseEvent ObjectStateChanged(olds)
        '#End If
        '                End Using
        '            End Set
        '        End Property

        '''' <summary>
        '''' Модифицированная версия объекта
        '''' </summary>
        'Friend ReadOnly Property OriginalCopy() As OrmBase
        '    Get
        '        'If _mo Is Nothing Then
        '        CheckCash()
        '        If OrmCache.Modified(Me) Is Nothing Then Return Nothing
        '        Return OrmCache.Modified(Me).Obj
        '        'Else
        '        'Return _mo.Obj
        '        'End If
        '    End Get
        'End Property

        'Friend ReadOnly Property IsReadOnly() As Boolean
        '    Get
        '        Using SyncHelper(True)
        '            If _state = Orm.ObjectState.Modified Then
        '                CheckCash()
        '                Dim mo As ModifiedObject = OrmCache.Modified(Me)
        '                'If mo Is Nothing Then mo = _mo
        '                If mo IsNot Nothing Then
        '                    Using mc As IGetManager = GetMgr()
        '                        If mo.User IsNot Nothing AndAlso Not mo.User.Equals(mc.Manager.CurrentUser) Then
        '                            Return True
        '                        End If
        '                    End Using
        '                End If
        '                'ElseIf state = Obm.ObjectState.Deleted Then
        '                'Return True
        '            End If
        '            Return False
        '        End Using
        '    End Get
        'End Property

        'Friend ReadOnly Property ObjName() As String
        '    Get
        '        Return Me.GetType.Name & " - " & ObjectState.ToString & " (" & _id & "): "
        '    End Get
        'End Property

        'Protected Sub RaisePropertyChanged(ByVal fieldName As String, ByVal oldValue As Object)
        '    Dim value As Object = GetValue(fieldName)
        '    If Not Object.Equals(value, oldValue) Then
        '        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(fieldName, oldValue, value))
        '    End If
        'End Sub

        'Protected Sub New(ByVal id As Integer, ByVal cache As OrmCache, ByVal schema As QueryGenerator)
        '    Init(id, cache, schema)
        '    Init()
        'End Sub

        'Friend Sub Init(ByVal id As Integer, ByVal cache As OrmCache, ByVal schema As QueryGenerator, ByVal mgrString As String) Implements IOrmBase.Init
        '    MyBase.Init(cache, cache, mgrString)
        '    Me._id = id
        '    PKLoaded()
        'End Sub

        'Friend Sub Init(ByVal cache As OrmCache, ByVal schema As QueryGenerator)

        '    If schema IsNot Nothing Then
        '        Dim arr As Generic.List(Of ColumnAttribute) = schema.GetSortedFieldList(Me.GetType)
        '        _loaded_members = New BitArray(arr.Count)
        '    End If

        '    If cache IsNot Nothing Then cache.RegisterCreation(Me.GetType, Identifier)

        '    ObjectState = Orm.ObjectState.NotLoaded
        'End Sub

        Protected Overrides Sub Init(ByVal pk() As PKDesc, ByVal cache As Cache.CacheBase, ByVal schema As ObjectMappingEngine)
            Throw New NotSupportedException
        End Sub

        Protected Overrides Sub Init()
            MyBase.Init()
        End Sub

        Protected Overridable Overloads Sub Init(ByVal id As Object, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine) Implements _IKeyEntity.Init
            MyBase._Init(cache, schema)
            Identifier = id
            PKLoaded(1)
            CType(Me, _ICachedEntity).SetLoaded(GetPKValues(0).PropertyAlias, True, True, schema)
        End Sub

        <Runtime.Serialization.OnDeserialized()> _
        Private Overloads Sub Init(ByVal context As Runtime.Serialization.StreamingContext)
            Init()
            If ObjectState <> Entities.ObjectState.Created Then
                Using mc As IGetManager = GetMgr()
                    If mc IsNot Nothing Then
                        mc.Manager.RegisterInCashe(Me)
                    End If
                End Using
            End If
        End Sub

        'Protected ReadOnly Property OrmSchema() As QueryGenerator
        '    Get
        '        Using mc As IGetManager = GetMgr()
        '            If mc Is Nothing Then
        '                Return Nothing
        '            Else
        '                Return mc.Manager.ObjectSchema
        '            End If
        '        End Using
        '    End Get
        'End Property

        'Protected Property _members_load_state(ByVal idx As Integer) As Boolean
        '    Get
        '        If _loaded_members Is Nothing Then
        '            Using mc As IGetManager = GetMgr()
        '                Dim arr As Generic.List(Of ColumnAttribute) = mc.Manager.ObjectSchema.GetSortedFieldList(Me.GetType)
        '                _loaded_members = New BitArray(arr.Count)
        '            End Using
        '        End If
        '        Return _loaded_members(idx)
        '    End Get
        '    Set(ByVal value As Boolean)
        '        If _loaded_members Is Nothing Then
        '            Using mc As IGetManager = GetMgr()
        '                Dim arr As Generic.List(Of ColumnAttribute) = mc.Manager.ObjectSchema.GetSortedFieldList(Me.GetType)
        '                _loaded_members = New BitArray(arr.Count)
        '            End Using
        '        End If
        '        _loaded_members(idx) = value
        '    End Set
        'End Property

        'Friend Function SetLoaded(ByVal c As ColumnAttribute, ByVal loaded As Boolean, ByVal check As Boolean) As Boolean Implements IEntity.SetLoaded

        '    Dim idx As Integer = c.Index
        '    If idx = -1 Then
        '        Using mc As IGetManager = GetMgr()
        '            Dim arr As Generic.List(Of ColumnAttribute) = mc.Manager.ObjectSchema.GetSortedFieldList(Me.GetType)
        '            idx = arr.BinarySearch(c)
        '        End Using
        '        c.Index = idx
        '    End If

        '    If idx < 0 AndAlso check Then Throw New OrmObjectException("There is no such field " & c.FieldName)

        '    If idx >= 0 Then
        '        Using SyncHelper(False)
        '            Dim old As Boolean = _members_load_state(idx)
        '            _members_load_state(idx) = loaded
        '            Return old
        '        End Using
        '    End If
        'End Function

        'Friend Function CheckIsAllLoaded(ByVal schema As QueryGenerator, ByVal loadedColumns As Integer) As Boolean Implements IEntity.CheckIsAllLoaded
        '    Using SyncHelper(False)
        '        Dim allloaded As Boolean = True
        '        If Not _loaded OrElse _loaded_members.Count <= loadedColumns Then
        '            Dim arr As Generic.List(Of ColumnAttribute) = schema.GetSortedFieldList(Me.GetType)
        '            For i As Integer = 0 To arr.Count - 1
        '                If Not _members_load_state(i) Then
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

#If TraceSetState Then
        Public Structure SetState
            Private _dt As Date
            Private _r As ModifiedObject.ReasonEnum
            Private _modifiedStack As String
            Private _stack As String
            Private _oldState As ObjectState
            Private _newState As ObjectState
            Private _md As Date

            Public Sub New(ByVal dt As Date, ByVal r As ModifiedObject.ReasonEnum, ByVal mstack As String, _
                ByVal stack As String, ByVal os As ObjectState, ByVal ns As ObjectState, ByVal md As Date)
                _dt = dt
                _r = r
                _modifiedStack = mstack
                _stack = stack
                _oldState = os
                _newState = ns
                _md = md
            End Sub
        End Structure

        'Private _s As New List(Of Pair(Of Date, Pair(Of ModifiedObject.ReasonEnum, Pair(Of String, Pair(Of ObjectState, ObjectState)))))
        Private _s As New List(Of SetState)
#End If

        '#If TraceSetState Then
        '        Friend Sub SetObjectState(ByVal state As ObjectState, ByVal r As ModifiedObject.ReasonEnum, ByVal stack As String, ByVal md As Date)
        '            '_s.Add(New Pair(Of Date, Pair(Of ModifiedObject.ReasonEnum, Pair(Of String, Pair(Of ObjectState, ObjectState))))(Now, New Pair(Of ModifiedObject.ReasonEnum, Pair(Of String, Pair(Of ObjectState, ObjectState)))(r, New Pair(Of String, Pair(Of ObjectState, ObjectState))(Environment.StackTrace, New Pair(Of ObjectState, ObjectState)(_state, state)))))
        '            _s.Add(New SetState(Now, r, stack, Environment.StackTrace, _state, state, md))
        '#Else
        '        Friend Sub SetObjectState(ByVal state As ObjectState)
        '#End If
        '            _state = state
        '        End Sub

        Protected Sub CheckCash()
            'If OrmCache Is Nothing Then
            '    Throw New OrmObjectException(ObjName & "The object is floating and has not cashe that is needed to perform this operation")
            'End If
            Using mc As IGetManager = GetMgr()
                If mc Is Nothing Then
                    Throw New OrmObjectException(ObjName & "You have to create MediaContent object to perform this operation")
                End If
            End Using
        End Sub

        '        Protected Friend Function Save(ByVal mc As OrmManager) As Boolean
        '            CheckCash()
        '            If IsReadOnly Then
        '                Throw New OrmObjectException(ObjName & "Object in readonly state")
        '            End If

        '            Dim r As Boolean = True
        '            If _state = Orm.ObjectState.Modified Then
        '#If TraceSetState Then
        '                Dim mo As ModifiedObject = mc.Cache.Modified(Me)
        '                If mo Is Nothing OrElse mo.Reason = ModifiedObject.ReasonEnum.Delete Then
        '                    Debug.Assert(False)
        '                    Throw New OrmObjectException
        '                End If
        '#End If
        '                r = mc.UpdateObject(Me)
        '            ElseIf _state = Orm.ObjectState.Created OrElse _state = Orm.ObjectState.NotFoundInSource Then
        '                If OriginalCopy IsNot Nothing Then
        '                    Throw New OrmObjectException(ObjName & " already exists.")
        '                End If
        '                Dim o As OrmBase = mc.AddObject(Me)
        '                If o Is Nothing Then
        '                    r = False
        '                Else
        '                    Debug.Assert(_state = Orm.ObjectState.Modified) ' OrElse _state = Orm.ObjectState.None
        '                    _needAdd = True
        '                End If
        '            ElseIf _state = Orm.ObjectState.Deleted Then
        '#If TraceSetState Then
        '                Dim mo As ModifiedObject = mc.Cache.Modified(Me)
        '                If mo Is Nothing OrElse (mo.Reason <> ModifiedObject.ReasonEnum.Delete AndAlso mo.Reason <> ModifiedObject.ReasonEnum.Edit) Then
        '                    Debug.Assert(False)
        '                    Throw New OrmObjectException
        '                End If
        '#End If
        '                mc.DeleteObject(Me)
        '                _needDelete = True
        '#If TraceSetState Then
        '            Else
        '                Debug.Assert(False)
        '#End If
        '            End If
        '            Return r
        '        End Function

        'Protected Friend Function GetMgr() As IGetManager
        '    Dim mgr As OrmManager = OrmManager.CurrentManager
        '    If Not String.IsNullOrEmpty(_mgrStr) Then
        '        Do While mgr IsNot Nothing AndAlso mgr.IdentityString <> _mgrStr
        '            mgr = mgr._prev
        '        Loop
        '    End If
        '    If mgr Is Nothing Then
        '        Dim a As New ManagerRequiredArgs
        '        RaiseEvent ManagerRequired(Me, a)
        '        mgr = a.Manager
        '        Return mgr
        '    Else
        '        Return New ManagerWrapper(mgr)
        '    End If
        'End Function

        'Protected Function RemoveVersionData(ByVal setState As Boolean) As OrmBase
        '    Dim mo As OrmBase = Nothing

        '    'unreg = unreg OrElse _state <> Orm.ObjectState.Created
        '    If setState Then
        '        ObjectState = Orm.ObjectState.None
        '        Debug.Assert(IsLoaded)
        '        If Not IsLoaded Then
        '            Throw New OrmObjectException(ObjName & "Cannot set state None while object is not loaded")
        '        End If
        '    End If
        '    'If unreg Then
        '    mo = OriginalCopy
        '    OrmCache.UnregisterModification(Me)
        '    '_mo = Nothing
        '    'End If

        '    Return mo
        'End Function

        Protected Overrides Sub AcceptRelationalChanges(ByVal updateCache As Boolean, ByVal mgr As OrmManager)
            Dim t As Type = Me.GetType
            Dim cache As OrmCache = TryCast(mgr.Cache, OrmCache)

            For Each acs As AcceptState2 In _needAccept
                acs.Accept(Me, mgr)
            Next
            _needAccept.Clear()

            Dim rel As IRelation = mgr.MappingEngine.GetConnectedTypeRelation(t)

            If rel IsNot Nothing AndAlso cache IsNot Nothing Then
                Dim c As New OrmManager.M2MEnum(rel, Me, mgr.MappingEngine)
                cache.ConnectedEntityEnum(mgr, t, AddressOf c.Accept)
            End If

            For Each rl As Relation In _relations
                Dim el As M2MRelation = TryCast(rl, M2MRelation)
                If el Is Nothing Then Continue For
                SyncLock "1efb139gf8bh"
                    For Each o As _IKeyEntity In el.Added
                        'Dim o As _IOrmBase = CType(mgr.GetOrmBaseFromCacheOrCreate(id, el.SubType), _IOrmBase)
                        Dim m As M2MRelation = CType(o.GetRelation(New M2MRelationDesc(Me.GetType, el.Key)), M2MRelation)
                        m.Added.Remove(Me)
                        m._savedIds.Remove(Me)
                        If updateCache AndAlso cache IsNot Nothing Then
                            cache.RemoveM2MQueries(m)
                        Else
                            Dim l As List(Of M2MRelation) = CType(Me, _ICachedEntity).UpdateCtx.Relations
                            If Not l.Contains(m) Then
                                l.Add(m)
                            End If
                        End If
                    Next
                    el.Added.Clear()

                    For Each o As _IKeyEntity In el.Deleted
                        'Dim o As _IOrmBase = CType(mgr.GetOrmBaseFromCacheOrCreate(id, el.SubType), _IOrmBase)
                        Dim m As M2MRelation = CType(o.GetRelation(New M2MRelationDesc(Me.GetType, el.Key)), M2MRelation)
                        m.Deleted.Remove(Me)
                        If updateCache AndAlso cache IsNot Nothing Then
                            cache.RemoveM2MQueries(el)
                        Else
                            Dim l As List(Of M2MRelation) = CType(Me, _ICachedEntity).UpdateCtx.Relations
                            If Not l.Contains(m) Then
                                l.Add(m)
                            End If
                        End If
                    Next
                    el.Deleted.Clear()
                    el.Reject2()

                    If updateCache AndAlso cache IsNot Nothing Then
                        cache.RemoveM2MQueries(el)
                    Else
                        Dim l As List(Of M2MRelation) = CType(Me, _ICachedEntity).UpdateCtx.Relations
                        If Not l.Contains(el) Then
                            l.Add(el)
                        End If
                    End If
                End SyncLock
            Next
        End Sub

        'Protected Friend Function AcceptChanges(ByVal updateCache As Boolean, ByVal setState As Boolean) As OrmBase
        '    CheckCash()
        '    Dim mo As OrmBase = Nothing
        '    Using SyncHelper(False)
        '        If _state = Orm.ObjectState.Created OrElse _state = Orm.ObjectState.Clone Then 'OrElse _state = Orm.ObjectState.NotLoaded Then
        '            Throw New OrmObjectException(ObjName & "accepting changes allowed in state Modified, deleted or none")
        '        End If

        '        Using gmc As IGetManager = GetMgr()
        '            Dim mc As OrmManager = gmc.Manager
        '            _valProcs = _needAccept.Count > 0

        '            AcceptRelationalChanges(mc)

        '            If _state <> Orm.ObjectState.None Then
        '                mo = RemoveVersionData(setState)

        '                If _needDelete Then
        '                    _valProcs = False
        '                    If updateCache Then
        '                        mc.Cache.UpdateCache(mc.ObjectSchema, New OrmBase() {Me}, mc, AddressOf ClearCacheFlags, Nothing, Nothing)
        '                        'mc.Cache.UpdateCacheOnDelete(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing)
        '                    End If
        '                    Accept_AfterUpdateCacheDelete(Me, mc)
        '                    RaiseEvent Deleted(Me, EventArgs.Empty)
        '                ElseIf _needAdd Then
        '                    _valProcs = False
        '                    Dim dic As IDictionary = mc.GetDictionary(Me.GetType)
        '                    Dim o As OrmBase = CType(dic(Identifier), OrmBase)
        '                    If (o Is Nothing) OrElse (Not o.IsLoaded AndAlso IsLoaded) Then
        '                        dic(Identifier) = Me
        '                    End If
        '                    If updateCache Then
        '                        'mc.Cache.UpdateCacheOnAdd(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing, Nothing)
        '                        mc.Cache.UpdateCache(mc.ObjectSchema, New OrmBase() {Me}, mc, AddressOf ClearCacheFlags, Nothing, Nothing)
        '                    End If
        '                    Accept_AfterUpdateCacheAdd(Me, mc, mo)
        '                    RaiseEvent Added(Me, EventArgs.Empty)
        '                Else
        '                    If (_upd IsNot Nothing OrElse _valProcs) AndAlso updateCache Then
        '                        mc.InvalidateCache(Me, CType(_upd, System.Collections.ICollection))
        '                    End If
        '                    RaiseEvent Updated(Me, EventArgs.Empty)
        '                End If
        '            ElseIf _valProcs AndAlso updateCache Then
        '                mc.Cache.ValidateSPOnUpdate(Me, Nothing)
        '            End If
        '        End Using
        '    End Using

        '    Return mo
        'End Function

        'Friend Sub UpdateCache()
        '    Using gmc As IGetManager = GetMgr()
        '        Dim mc As OrmManager = gmc.Manager
        '        If _upd IsNot Nothing OrElse _valProcs Then
        '            mc.InvalidateCache(Me, CType(_upd, System.Collections.ICollection))
        '        Else
        '            mc.Cache.UpdateCache(mc.ObjectSchema, New OrmBase() {Me}, mc, AddressOf ClearCacheFlags, Nothing, Nothing)
        '        End If
        '    End Using
        'End Sub

        'Protected Sub AcceptChanges(ByVal cashe As MediaCacheBase)
        '    Debug.Assert(_state = Obm.ObjectState.Created)
        '    Me.OrmCache = cashe
        '    Using SyncHelper(False)
        '        _state = ObjectState.None
        '    End Using
        'End Sub

        'Friend Shared Sub Accept_AfterUpdateCache(ByVal obj As OrmBase, ByVal mc As OrmManager, _
        '    ByVal contextKey As Object)

        '    If obj._needDelete Then
        '        Accept_AfterUpdateCacheDelete(obj, mc)
        '    ElseIf obj._needAdd Then
        '        Accept_AfterUpdateCacheAdd(obj, mc, contextKey)
        '    End If
        'End Sub

        'Protected Friend Shared Sub ClearCacheFlags(ByVal obj As OrmBase, ByVal mc As OrmManager, _
        '    ByVal contextKey As Object)
        '    obj._needAdd = False
        '    obj._needDelete = False
        'End Sub

        'Friend Shared Sub Accept_AfterUpdateCacheDelete(ByVal obj As OrmBase, ByVal mc As OrmManager)
        '    mc._RemoveObjectFromCache(obj)
        '    mc.Cache.RegisterDelete(obj)
        '    'obj._needDelete = False
        'End Sub

        'Friend Shared Sub Accept_AfterUpdateCacheAdd(ByVal obj As OrmBase, ByVal mc As OrmManager, _
        '    ByVal contextKey As Object)
        '    'obj._needAdd = False
        '    Dim nm As OrmManager.INewObjects = mc.NewObjectManager
        '    If nm IsNot Nothing Then
        '        Dim mo As OrmBase = TryCast(contextKey, OrmBase)
        '        If mo Is Nothing Then
        '            Dim dic As Generic.Dictionary(Of OrmBase, OrmBase) = TryCast(contextKey, Generic.Dictionary(Of OrmBase, OrmBase))
        '            If dic IsNot Nothing Then
        '                dic.TryGetValue(obj, mo)
        '            End If
        '        End If
        '        If mo IsNot Nothing Then
        '            nm.RemoveNew(mo)
        '        End If
        '    End If
        'End Sub

        'Protected Friend Sub PrepareUpdate()
        '    If _state = Orm.ObjectState.Clone Then
        '        Throw New OrmObjectException(ObjName & ": Altering clone is not allowed")
        '    End If

        '    If _state = Orm.ObjectState.Deleted Then
        '        Throw New OrmObjectException(ObjName & ": Altering deleted object is not allowed")
        '    End If

        '    If Not _loading Then 'AndAlso ObjectState <> Orm.ObjectState.Deleted Then
        '        If OrmCache Is Nothing Then
        '            Return
        '        End If

        '        If Not IsLoaded Then
        '            If _state = Orm.ObjectState.None Then
        '                Throw New InvalidOperationException(String.Format("Object {0} is not loaded while the state is None", ObjName))
        '            End If

        '            If _state = Orm.ObjectState.NotLoaded Then
        '                Load()
        '                If _state = Orm.ObjectState.NotFoundInSource Then
        '                    Throw New OrmObjectException(ObjName & "Object is not editable 'cause it is not found in source")
        '                End If
        '            Else
        '                Return
        '            End If
        '        End If

        '        Dim mo As ModifiedObject = OrmCache.Modified(Me)
        '        If mo IsNot Nothing Then
        '            Using mc As IGetManager = GetMgr()
        '                If mo.User IsNot Nothing AndAlso Not mo.User.Equals(mc.Manager.CurrentUser) Then
        '                    Throw New OrmObjectException(ObjName & "Object has already altered by another user")
        '                End If
        '            End Using
        '            If _state = Orm.ObjectState.Deleted Then _state = ObjectState.Modified
        '        Else
        '            Debug.Assert(_state = Orm.ObjectState.None) ' OrElse state = Obm.ObjectState.Created)
        '            'CreateModified(_id)
        '            CreateModified()
        '            EnsureInCache()
        '            'If modified.old_state = Obm.ObjectState.Created Then
        '            '    _mo = mo
        '            'End If
        '        End If
        '    End If
        'End Sub

        'Protected Sub EnsureInCache()
        '    Using mc As IGetManager = GetMgr()
        '        mc.Manager.EnsureInCache(Me)
        '    End Using
        'End Sub

        'Protected Friend Function GetFullClone() As OrmBase
        '    Dim modified As OrmBase = CType(Clone(), OrmBase)
        '    modified._old_state = modified.ObjectState
        '    modified.ObjectState = ObjectState.Clone
        '    modified._loaded_members = _loaded_members
        '    Return modified
        'End Function

        'Protected Sub PrepareRead(ByVal fieldName As String, ByRef d As IDisposable)
        '    If Not IsLoaded AndAlso (_state = Orm.ObjectState.NotLoaded OrElse _state = Orm.ObjectState.None) Then
        '        d = SyncHelper(True)
        '        If Not IsLoaded AndAlso (_state = Orm.ObjectState.NotLoaded OrElse _state = Orm.ObjectState.None) AndAlso Not IsFieldLoaded(fieldName) Then
        '            Load()
        '        End If
        '    End If
        'End Sub

        'Protected Friend Sub RaiseCopyRemoved()
        '    RaiseEvent OriginalCopyRemoved(Me)
        'End Sub

        'Protected Friend Sub RaiseBeginModification(ByVal reason As ModifiedObject.ReasonEnum)
        '    Dim modified As OrmBase = CreateOriginalVersion()
        '    ObjectState = Orm.ObjectState.Modified
        '    Using mc As IGetManager = GetMgr()
        '        mc.Manager.Cache.RegisterModification(modified, Identifier, reason)
        '        If Not _loading Then
        '            mc.Manager.RaiseBeginUpdate(Me)
        '        End If
        '    End Using
        'End Sub

        '<Obsolete()> _
        'Protected Friend Sub CreateModified(ByVal id As Integer, ByVal reason As ModifiedObject.ReasonEnum)
        '    Dim modified As OrmBase = CreateOriginalVersion()
        '    ObjectState = Orm.ObjectState.Modified
        '    Using mc As IGetManager = GetMgr()
        '        mc.Manager.Cache.RegisterModification(modified, id, reason)
        '        If Not _loading AndAlso reason <> ModifiedObject.ReasonEnum.Delete Then
        '            mc.Manager.RaiseBeginUpdate(Me)
        '        End If
        '    End Using
        'End Sub

        'Protected Sub CreateModified()
        '    Dim modified As OrmBase = CreateOriginalVersion()
        '    ObjectState = Orm.ObjectState.Modified
        '    Using mc As IGetManager = GetMgr()
        '        mc.Manager.Cache.RegisterModification(modified, ModifiedObject.ReasonEnum.Edit)
        '        'OrmCache.RegisterModification(modified).Reason = ModifiedObject.ReasonEnum.Edit
        '        If Not _loading Then
        '            mc.Manager.RaiseBeginUpdate(Me)
        '        End If
        '    End Using
        'End Sub

        'Public Sub test()
        '    Dim f As New IO.FileInfo("f:\temp\Diagram_1.txt")
        '    f.AppendText().WriteLine("hi!")
        'End Sub

        'Protected Function CompareTo(ByVal other As OrmBase) As Integer
        '    If other Is Nothing Then
        '        'Throw New MediaObjectModelException(ObjName & "other parameter cannot be nothing")
        '        Return 1
        '    End If
        '    Return Math.Sign(_id - other._id)
        'End Function

        'Protected Function CompareTo1(ByVal obj As Object) As Integer Implements System.IComparable.CompareTo
        '    Return CompareTo(TryCast(obj, OrmBase))
        'End Function

        Private Function GetName() As String Implements _IKeyEntity.GetName
            Return Me.GetType.Name & Identifier.ToString
        End Function

        Private Function GetOldName(ByVal id As Object) As String Implements _IKeyEntity.GetOldName
            Return Me.GetType.Name & id.ToString
        End Function

        'Friend Overridable Function ForseUpdate(ByVal c As ColumnAttribute) As Boolean
        '    Return False
        'End Function

        'Protected Friend Sub SetPK(ByVal fieldName As String, ByVal value As Object)
        '    _id = CInt(value)
        'End Sub

        'Protected Friend Overridable Sub RemoveFromCache(ByVal cache As OrmBase)

        'End Sub

        Private Function AddAccept(ByVal acs As AcceptState2) As Boolean Implements _IKeyEntity.AddAccept
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

        Private Function GetAccept(ByVal m As M2MCache) As AcceptState2 Implements _IKeyEntity.GetAccept
            Using SyncHelper(False)
                For Each a As AcceptState2 In _needAccept
                    If a.CacheItem Is m Then
                        Return a
                    End If
                Next
            End Using
            Return Nothing
        End Function

        'Protected Friend Sub RaiseSaved(ByVal sa As OrmManager.SaveAction)
        '    RaiseEvent Saved(Me, New ObjectSavedArgs(sa))
        'End Sub
#End Region

#Region " Public properties "

#If OLDM2M Then
        Public ReadOnly Property M2M() As M2MClass
            Get
                Return New M2MClass(Me)
            End Get
        End Property
#End If

        Public MustOverride Property Identifier() As Object Implements IKeyEntity.Identifier

#End Region

#Region " Synchronization "
        Protected Friend Property ObjSaved() As Boolean
            Get
                Return _saved
            End Get
            Set(ByVal value As Boolean)
                _saved = value
            End Set
        End Property

        '        Friend Function GetSyncRoot() As IDisposable Implements IEntity.GetSyncRoot
        '            Return SyncHelper(False)
        '        End Function

        '        Protected Overridable Function SyncHelper(ByVal reader As Boolean) As IDisposable
        '#If DebugLocks Then
        '            Return New CSScopeMgr_Debug(Me, "d:\temp\")
        '#Else
        '            Return New CSScopeMgr(Me)
        '#End If
        '        End Function

        'Protected Friend Function SyncHelper(ByVal reader As Boolean, ByVal fieldName As String) As IDisposable
        '    Dim err As Boolean = True
        '    Dim d As IDisposable = New BlankSyncHelper(Nothing)
        '    Try
        '        If reader Then
        '            PrepareRead(fieldName, d)
        '        Else
        '            d = SyncHelper(True)
        '            PrepareUpdate()
        '            If Not _dontRaisePropertyChange AndAlso Not _loading Then
        '                d = New ChangedEventHelper(Me, fieldName, d)
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

#End Region

#Region " Public functions "

        '''' <summary>
        '''' Загрузка объекта из БД
        '''' </summary>
        'Public Overridable Sub Load()
        '    CheckCash()
        '    'If OrmCache.MediaContent Is Nothing Then
        '    '    Throw New MediaObjectModelException(ObjName & "You have to create MediaContent object to perform this operation")
        '    'End If
        '    Dim mo As ModifiedObject = OrmCache.Modified(Me)
        '    'If mo Is Nothing Then mo = _mo
        '    If mo IsNot Nothing Then
        '        If mo.User IsNot Nothing Then
        '            Using mc As IGetManager = GetMgr()
        '                If Not mo.User.Equals(mc.Manager.CurrentUser) Then
        '                    Throw New OrmObjectException(ObjName & "Object in readonly state")
        '                End If
        '            End Using
        '        Else
        '            If _state = Orm.ObjectState.Deleted OrElse _state = Orm.ObjectState.Modified Then
        '                Throw New OrmObjectException(ObjName & "Cannot load object while its state is deleted or modified!")
        '            End If
        '        End If
        '    End If
        '    Dim olds As ObjectState = _state
        '    Using mc As IGetManager = GetMgr()
        '        mc.Manager.LoadObject(Me)
        '    End Using
        '    If olds = Orm.ObjectState.Created AndAlso _state = Orm.ObjectState.Modified Then
        '        AcceptChanges(True, True)
        '    ElseIf IsLoaded Then
        '        ObjectState = Orm.ObjectState.None
        '    End If
        '    Invariant()
        'End Sub

        'Public ReadOnly Property CanEdit() As Boolean
        '    Get
        '        If _state = Orm.ObjectState.Deleted Then 'OrElse _state = Orm.ObjectState.NotFoundInSource Then
        '            Return False
        '        End If
        '        If _state = Orm.ObjectState.NotLoaded Then
        '            Load()
        '        End If
        '        Return _state <> Orm.ObjectState.NotFoundInSource
        '    End Get
        'End Property

        'Public ReadOnly Property CanLoad() As Boolean
        '    Get
        '        Return _state <> Orm.ObjectState.Deleted AndAlso _state <> Orm.ObjectState.Modified
        '    End Get
        'End Property

        'Public Function SaveChanges(ByVal AcceptChanges As Boolean) As Boolean
        '    Using mc As IGetManager = GetMgr()
        '        Return mc.Manager.SaveChanges(Me, AcceptChanges)
        '    End Using
        'End Function

        ''' <param name="obj">The System.Object to compare with the current System.Object.</param>
        ''' <returns>true if the specified System.Object is equal to the current System.Object; otherwise, false.</returns>
        Public Overloads Overrides Function Equals(ByVal obj As Object) As System.Boolean
            If obj Is Nothing Then Return False
            If Me.GetType.IsAssignableFrom(obj.GetType) Then
                Return Equals(CType(obj, KeyEntity))
            Else
                Return False 'Identifier.Equals(obj)
            End If
        End Function

        'Public Overridable Overloads Function Equals(ByVal other_id As Integer) As Boolean
        '    Return Me.Identifier = other_id
        'End Function

        Public Overridable Overloads Function Equals(ByVal obj As KeyEntity) As Boolean
            If obj Is Nothing Then Return False
            If Me.GetType.IsAssignableFrom(obj.GetType) Then
                Return Me.Identifier.Equals(obj.Identifier)
            Else
                Return False
            End If
        End Function

        ''' <returns>A hash code for the current System.Object.</returns>
        Public Overrides Function GetHashCode() As System.Int32
            Return Identifier.GetHashCode
        End Function

        Public Overrides Function ToString() As String
            Return Identifier.ToString()
        End Function

        'Public MustOverride ReadOnly Property HasChanges() As Boolean

        'Protected MustOverride Function GetNew() As OrmBase

        Private Sub _RejectM2MIntermidiate() Implements _IKeyEntity.RejectM2MIntermidiate
            Using SyncHelper(False)
                For Each acs As AcceptState2 In _needAccept
                    If acs.el IsNot Nothing Then
                        acs.el.Reject2()
                    End If
                Next

                For Each rl As Relation In _relations
                    Dim el As M2MRelation = TryCast(rl, M2MRelation)
                    If el IsNot Nothing Then el.Reject2()
                Next
            End Using
        End Sub

        Public Overrides Sub RejectRelationChanges(ByVal mc As OrmManager)
            Using SyncHelper(False)
                Dim t As Type = Me.GetType
                'Using gmc As IGetManager = GetMgr()
                'Dim mc As OrmManager = gmc.Manager
                Dim rel As IRelation = mc.MappingEngine.GetConnectedTypeRelation(t)
                Dim cache As OrmCache = TryCast(mc.Cache, OrmCache)
                If rel IsNot Nothing AndAlso cache IsNot Nothing Then
                    Dim c As New OrmManager.M2MEnum(rel, Me, mc.MappingEngine)
                    cache.ConnectedEntityEnum(mc, t, AddressOf c.Reject)
                End If
                For Each acs As AcceptState2 In _needAccept
                    If acs.el IsNot Nothing Then
                        acs.el.Reject(mc, True)
                    End If
                Next
                _needAccept.Clear()

                For Each rl As Relation In _relations
                    Dim el As M2MRelation = TryCast(rl, M2MRelation)
                    If el IsNot Nothing Then el.Reject(mc, True)
                Next
                'End Using
            End Using
        End Sub

        '        ''' <summary>
        '        ''' Отмена изменений
        '        ''' </summary>
        '        Public Sub RejectChanges()
        '            Using SyncHelper(False)
        '                RejectRelationChanges()

        '                If _state = ObjectState.Modified OrElse _state = Orm.ObjectState.Deleted OrElse _state = Orm.ObjectState.Created Then
        '                    If IsReadOnly Then
        '                        Throw New OrmObjectException(ObjName & " object in readonly state")
        '                    End If

        '                    If OriginalCopy Is Nothing Then
        '                        If _state <> Orm.ObjectState.Created AndAlso _state <> Orm.ObjectState.Deleted Then
        '                            Throw New OrmObjectException(ObjName & ": When object is in modified state it has to have an original copy")
        '                        End If
        '                        Return
        '                    End If

        '                    Dim mo As ModifiedObject = OrmCache.Modified(Me)
        '                    If _state = Orm.ObjectState.Deleted AndAlso mo.Reason <> ModifiedObject.ReasonEnum.Delete Then
        '                        'Debug.Assert(False)
        '                        'Throw New OrmObjectException
        '                        Return
        '                    End If

        '                    If _state = Orm.ObjectState.Modified AndAlso (mo.Reason = ModifiedObject.ReasonEnum.Delete) Then
        '                        Debug.Assert(False)
        '                        Throw New OrmObjectException
        '                    End If

        '                    'Debug.WriteLine(Environment.StackTrace)
        '                    _needAdd = False
        '                    _needDelete = False
        '                    _upd = Nothing

        '                    Dim oldid As Integer = Identifier
        '                    Dim olds As ObjectState = OriginalCopy._old_state
        '                    Dim newid As Integer = OriginalCopy.Identifier
        '                    If olds <> Orm.ObjectState.Created Then
        '                        '_loaded_members = 
        '                        RevertToOriginalVersion()
        '                        RemoveVersionData(False)
        '                    End If
        '                    _id = newid
        '#If TraceSetState Then
        '                    SetObjectState(olds, mo.Reason, mo.StackTrace, mo.DateTime)
        '#Else
        '                    SetObjectState(olds)
        '#End If
        '                    If _state = Orm.ObjectState.Created Then
        '                        Dim name As String = Me.GetType.Name
        '                        Using gmc As IGetManager = GetMgr()
        '                            Dim mc As OrmManager = gmc.Manager
        '                            Dim dic As IDictionary = mc.GetDictionary(Me.GetType)
        '                            If dic Is Nothing Then
        '                                Throw New OrmObjectException("Collection for " & name & " not exists")
        '                            End If

        '                            dic.Remove(oldid)
        '                        End Using

        '                        OrmCache.UnregisterModification(Me)

        '                        _loaded = False
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
        '                Invariant()
        '            End Using
        '        End Sub

        'Public Sub AcceptChanges()
        '    AcceptChanges(True, IsGoodState(_state))
        'End Sub

        'Public Function IsFieldLoaded(ByVal fieldName As String) As Boolean
        '    Dim c As New ColumnAttribute(fieldName)
        '    Using mc As IGetManager = GetMgr()
        '        Dim arr As Generic.List(Of ColumnAttribute) = mc.Manager.ObjectSchema.GetSortedFieldList(Me.GetType)
        '        Dim idx As Integer = arr.BinarySearch(c)
        '        If idx < 0 Then Throw New OrmObjectException("There is no such field " & fieldName)
        '        Return _members_load_state(idx)
        '    End Using
        'End Function

        'Protected Sub PrepareRead()
        '    If Not IsLoaded AndAlso state = Obm.ObjectState.None Then
        '        '    Dim c As New ColumnAttribute(FieldName)
        '        '    Dim arr As Generic.List(Of ColumnAttribute) = OrmManager.CurrentMediaContent.DatabaseSchema.GetSortedFieldList(Me.GetType)
        '        '    Dim idx As Integer = arr.BinarySearch(c)
        '        '    If idx < 0 Then Throw New MediaObjectModelException("There is no such field " & c.FieldName)

        '        '    If Not members_load_state(idx) Then Load()
        '        Load()
        '    End If
        'End Sub

        'Public Overridable Function Delete() As Boolean
        '    Using SyncHelper(False)
        '        If _state = Orm.ObjectState.Deleted Then Return False

        '        If _state = Orm.ObjectState.Clone Then
        '            Throw New OrmObjectException(ObjName & "Deleting clone is not allowed")
        '        End If
        '        If _state <> Orm.ObjectState.Modified AndAlso _state <> Orm.ObjectState.None AndAlso _state <> Orm.ObjectState.NotLoaded Then
        '            Throw New OrmObjectException(ObjName & "Deleting is not allowed for this object")
        '        End If
        '        Dim mo As ModifiedObject = OrmCache.Modified(Me)
        '        'If mo Is Nothing Then mo = _mo
        '        If mo IsNot Nothing Then
        '            Using mc As IGetManager = GetMgr()
        '                If mo.User IsNot Nothing AndAlso Not mo.User.Equals(mc.Manager.CurrentUser) Then
        '                    Throw New OrmObjectException(ObjName & "Object has already altered by user " & mo.User.ToString)
        '                End If
        '            End Using
        '            Debug.Assert(mo.Reason <> ModifiedObject.ReasonEnum.Delete)
        '        Else
        '            If _state = Orm.ObjectState.NotLoaded Then
        '                Load()
        '                If _state = Orm.ObjectState.NotFoundInSource Then
        '                    Return False
        '                End If
        '            End If

        '            Debug.Assert(_state <> Orm.ObjectState.Modified)
        '            CreateModified(Identifier, ModifiedObject.ReasonEnum.Delete)
        '            'OrmCache.Modified(Me).Reason = ModifiedObject.ReasonEnum.Delete
        '            'Dim modified As OrmBase = CloneMe()
        '            'modified._old_state = modified.ObjectState
        '            'modified.ObjectState = ObjectState.Clone
        '            'OrmCache.RegisterModification(modified)
        '        End If
        '        ObjectState = ObjectState.Deleted
        '        EnsureInCache()
        '        Using mc As IGetManager = GetMgr()
        '            mc.Manager.RaiseBeginDelete(Me)
        '        End Using
        '    End Using

        '    Return True
        'End Function

        '<EditorBrowsable(EditorBrowsableState.Never)> _
        '<Conditional("DEBUG")> _
        'Public Sub Invariant()
        '    Using SyncHelper(True)
        '        If IsLoaded AndAlso _
        '            _state <> Orm.ObjectState.None AndAlso _state <> Orm.ObjectState.Modified AndAlso _state <> Orm.ObjectState.Deleted Then Throw New OrmObjectException(ObjName & "When object is loaded its state has to be None or Modified or Deleted: current state is " & _state.ToString)
        '        If Not IsLoaded AndAlso _
        '           (_state = Orm.ObjectState.None OrElse _state = Orm.ObjectState.Modified OrElse _state = Orm.ObjectState.Deleted) Then Throw New OrmObjectException(ObjName & "When object is not loaded its state has not be None or Modified or Deleted: current state is " & _state.ToString)
        '        If _state = Orm.ObjectState.Modified AndAlso OrmCache.Modified(Me) Is Nothing Then
        '            'Throw New OrmObjectException(ObjName & "When object is in modified state it has to have an original copy")
        '            _state = Orm.ObjectState.None
        '            Load()
        '        End If
        '    End Using
        'End Sub

        'Public Function EnsureLoaded() As OrmBase
        '    'OrmManager.CurrentMediaContent.LoadObject(Me)
        '    Using mc As IGetManager = GetMgr()
        '        Return mc.Manager.LoadType(_id, Me.GetType, True, True)
        '    End Using
        'End Function

        'Public Overridable Sub SetValue(ByVal pi As Reflection.PropertyInfo, ByVal c As ColumnAttribute, ByVal value As Object) Implements IEntity.SetValue
        '    If pi Is Nothing Then
        '        Throw New ArgumentNullException("pi")
        '    End If

        '    pi.SetValue(Me, value, Nothing)
        'End Sub

        'Public Overridable Function GetValue(ByVal propAlias As String) As Object Implements IEntity.GetValue
        '    If propAlias = "ID" Then
        '        'Throw New OrmObjectException("Use Identifier property to get ID")
        '        Return Identifier
        '    Else
        '        Dim s As OrmManager = OrmSchema
        '        If s Is Nothing Then
        '            Return OrmManager.GetFieldValueSchemaless(Me, propAlias)
        '        Else
        '            Return s.GetFieldValue(Me, propAlias, Nothing)
        '        End If
        '    End If
        'End Function

        'Public Overridable Function GetValue(ByVal propAlias As String, ByVal schema As IOrmObjectSchemaBase) As Object Implements IEntity.GetValue
        '    If propAlias = "ID" Then
        '        'Throw New OrmObjectException("Use Identifier property to get ID")
        '        Return Identifier
        '    Else
        '        Dim s As OrmManager = OrmSchema
        '        If s Is Nothing Then
        '            Return OrmManager.GetFieldValueSchemaless(Me, propAlias, schema)
        '        Else
        '            Return s.GetFieldValue(Me, propAlias, schema)
        '        End If
        '    End If
        'End Function

        '<EditorBrowsable(EditorBrowsableState.Never)> _
        'Public Overridable Sub CreateObject(ByVal fieldName As String, ByVal value As Object)

        'End Sub
#End Region

        Public Shared Shadows Operator <>(ByVal obj1 As KeyEntity, ByVal obj2 As KeyEntity) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then Return False
                'Throw New MediaObjectModelException("obj1 parameter cannot be nothing")
                Return Not obj2.Equals(obj1)
            End If
            Return Not obj1.Equals(obj2)
        End Operator

        Public Shared Shadows Operator =(ByVal obj1 As KeyEntity, ByVal obj2 As KeyEntity) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then Return True
                'Throw New MediaObjectModelException("obj1 parameter cannot be nothing")
                Return obj2.Equals(obj1)
            End If
            Return obj1.Equals(obj2)
        End Operator

        Protected Overrides Function GetCacheKey() As Integer
            Return Identifier.GetHashCode
        End Function

        Protected Overrides Function CreateObject() As Entity
            Using gm As IGetManager = GetMgr()
                Return CType(gm.Manager.CreateKeyEntity(Identifier, Me.GetType), Entity)
            End Using
        End Function

        Protected Overrides Sub PKLoaded(ByVal pkCount As Integer)
            If pkCount <> 1 Then
                Throw New OrmObjectException(String.Format("KeyEntity derived class must have only one PK. The value is {0}", pkCount))
            End If
            MyBase.PKLoaded(pkCount)
        End Sub

#Region " Relations "
        Protected Sub _ReplaceRel(ByVal oldRel As Relation, ByVal newRel As Relation, ByVal schema As ObjectMappingEngine) Implements IRelations._ReplaceRel
            Using GetSyncRoot()
                If _relations.Count > 0 Then
                    For Each rl As Relation In _relations
                        If Relation.MetaEquals(rl, oldRel, schema) Then
                            _relations.Remove(rl)
                            _relations.Add(newRel)
                            Return
                        End If
                    Next
                    If oldRel.Relation Is Nothing Then
                        _relations.Add(newRel)
                    Else
                        Throw New KeyNotFoundException("Relation is not found")
                    End If
                Else
                    _relations.Add(newRel)
                End If
            End Using
        End Sub

        Protected Sub AddRel(ByVal rel As Relation)
            Using GetSyncRoot()
                For Each rl As Relation In _relations
                    If rel.Equals(rl) Then
                        Return
                    End If
                Next
                _relations.Add(rel)
            End Using
        End Sub

        Public Function CreateRelCmd(ByVal eu As EntityUnion) As Worm.Query.RelationCmd
            Dim q As Worm.Query.RelationCmd = Nothing
            If CreateManager IsNot Nothing Then
                q = New Worm.Query.RelationCmd(Me, eu, CreateManager)
            Else
                q = Worm.Query.RelationCmd.Create(Me, eu)
            End If
            AddRel(q._rel)
            Return q
        End Function

        Public Function CreateRelCmd(ByVal eu As EntityUnion, ByVal key As String) As Worm.Query.QueryCmd
            Dim q As Worm.Query.RelationCmd = Nothing
            If CreateManager IsNot Nothing Then
                q = New Worm.Query.RelationCmd(Me, eu, key, CreateManager)
            Else
                q = Worm.Query.RelationCmd.Create(Me, eu, key)
            End If
            AddRel(q._rel)
            Return q
        End Function

        Public Function CreateRelCmd(ByVal desc As RelationDesc) As Worm.Query.QueryCmd
            Dim q As Worm.Query.RelationCmd = Nothing
            Dim mr As M2MRelationDesc = TryCast(desc, M2MRelationDesc)
            If CreateManager IsNot Nothing Then
                If mr IsNot Nothing Then
                    q = New Worm.Query.RelationCmd(New M2MRelation(Me, mr), CreateManager)
                Else
                    q = New Worm.Query.RelationCmd(New Relation(Me, desc), CreateManager)
                End If
            Else
                If mr IsNot Nothing Then
                    q = Worm.Query.RelationCmd.Create(New M2MRelation(Me, mr))
                Else
                    q = Worm.Query.RelationCmd.Create(New Relation(Me, desc))
                End If
            End If
            AddRel(q._rel)
            Return q
        End Function

        Protected Function GetCmd(ByVal t As System.Type) As Worm.Query.RelationCmd Implements IRelations.GetCmd
            Return CType(CreateRelCmd(New EntityUnion(t)).Select(t), RelationCmd)
        End Function

        Protected Function GetCmd(ByVal t As System.Type, ByVal key As String) As Worm.Query.RelationCmd Implements IRelations.GetCmd
            Return CType(CreateRelCmd(New EntityUnion(t), key).Select(t), RelationCmd)
        End Function

        Protected Function GetCmd(ByVal entityName As String) As Worm.Query.RelationCmd Implements IRelations.GetCmd
            Return CType(CreateRelCmd(New EntityUnion(entityName)).Select(entityName), RelationCmd)
        End Function

        Protected Function GetCmd(ByVal entityName As String, ByVal key As String) As Worm.Query.RelationCmd Implements IRelations.GetCmd
            Return CType(CreateRelCmd(New EntityUnion(entityName), key).Select(entityName), RelationCmd)
        End Function

        Protected Function GetCmd(ByVal desc As RelationDesc) As Worm.Query.RelationCmd Implements IRelations.GetCmd
            Return CType(CreateRelCmd(desc).Select(desc.Rel), RelationCmd)
        End Function

        Protected Function GetM2M(ByVal t As Type, ByVal key As String) As M2MRelation 'Implements _IOrmBase.GetM2M
            Dim el As M2MRelation = Nothing
            Using GetSyncRoot()
                For Each rl As Relation In _relations
                    Dim e As M2MRelation = TryCast(rl, M2MRelation)
                    If M2MRelationDesc.CompareKeys(e.Key, key) Then
                        If e.Relation.Type Is Nothing Then
                            If e.Relation.EntityName = MappingEngine.GetEntityNameByType(t) Then
                                el = e
                                Exit For
                            End If
                        ElseIf e.Relation.Type Is t Then
                            el = e
                            Exit For
                        End If
                    End If
                Next
                If el Is Nothing Then
                    el = New M2MRelation(Me, t, key)
                    _relations.Add(el)
                End If
            End Using
            Return el
        End Function

        Protected Sub _Add(ByVal obj As IKeyEntity) Implements IRelations.Add
            _Add(obj, Nothing)
        End Sub

        Protected Sub _Add(ByVal obj As IKeyEntity, ByVal key As String) Implements IRelations.Add
            Dim el As M2MRelation = GetM2M(obj.GetType, key)
            Using el.SyncRoot
                If Not el.Added.Contains(obj) Then
                    Dim el2 As M2MRelation = CType(obj.GetRelation(New M2MRelationDesc(Me.GetType, el.Key)), M2MRelation)
                    SyncLock "1efb139gf8bh"
                        If Not el2.Added.Contains(Me) Then
                            If el.Deleted.Contains(obj) Then
                                el.Deleted.Remove(obj)
                                el2.Deleted.Remove(Me)
                            Else
                                el.Add(obj)
                                el2.Add(Me)
                                Dim mc As OrmManager = GetCurrent()
                                If mc IsNot Nothing Then
                                    mc.RaiseBeginUpdate(Me)
                                    mc.RaiseBeginUpdate(obj)
                                    mc.Cache.RaiseBeginUpdate(Me)
                                    mc.Cache.RaiseBeginUpdate(obj)
                                Else
                                    Using gm As IGetManager = GetMgr()
                                        If gm IsNot Nothing Then
                                            gm.Manager.Cache.RaiseBeginUpdate(Me)
                                            gm.Manager.Cache.RaiseBeginUpdate(obj)
                                        End If
                                    End Using
                                End If
                            End If
                        End If
                    End SyncLock
                End If
            End Using
        End Sub

        'Protected Sub _Delete(ByVal t As Type) Implements IM2M.Delete
        '    Using mc As IGetManager = GetMgr
        '        mc.Manager.M2MDelete(_o, t, M2MRelation.DirKey)
        '    End Using
        'End Sub

        'Protected Sub _Delete(ByVal t As Type, ByVal key As String) Implements IM2M.Delete
        '    Using mc As IGetManager = GetMgr()
        '        Dim el As EditableListBase = GetM2M(t, key)

        '    End Using
        'End Sub

        Protected Sub _DeleteM2M(ByVal obj As IKeyEntity) Implements IRelations.Remove
            _DeleteM2M(obj, Nothing)
        End Sub

        Protected Sub _DeleteM2M(ByVal obj As IKeyEntity, ByVal key As String) Implements IRelations.Remove
            Dim el As M2MRelation = GetM2M(obj.GetType, key)
            Using el.SyncRoot
                If Not el.Deleted.Contains(obj) Then
                    Dim el2 As M2MRelation = CType(obj.GetRelation(New M2MRelationDesc(Me.GetType, key)), M2MRelation)
                    SyncLock "1efb139gf8bh"
                        If Not el2.Deleted.Contains(Me) Then
                            If el.Added.Contains(obj) Then
                                el.Added.Remove(obj)
                                el2.Added.Remove(Me)
                            Else
                                el.Delete(obj)
                                el2.Delete(Me)
                                Dim mc As OrmManager = GetCurrent()
                                If mc IsNot Nothing Then
                                    mc.RaiseBeginDelete(Me)
                                    mc.RaiseBeginDelete(obj)
                                    mc.Cache.RaiseBeginUpdate(Me)
                                    mc.Cache.RaiseBeginUpdate(obj)
                                Else
                                    Using gm As IGetManager = GetMgr()
                                        If gm IsNot Nothing Then
                                            gm.Manager.Cache.RaiseBeginUpdate(Me)
                                            gm.Manager.Cache.RaiseBeginUpdate(obj)
                                        End If
                                    End Using
                                End If
                            End If
                        End If
                    End SyncLock
                End If
            End Using
        End Sub

        Protected Sub _Cancel(ByVal en As String) Implements IRelations.Cancel
            _Cancel(New RelationDesc(New EntityUnion(en), Nothing, Nothing))
        End Sub

        Protected Sub _Cancel(ByVal en As String, ByVal key As String) Implements IRelations.Cancel
            _Cancel(New RelationDesc(New EntityUnion(en), Nothing, key))
        End Sub

        Protected Sub _Cancel(ByVal t As Type) Implements IRelations.Cancel
            _Cancel(t, Nothing)
        End Sub

        Protected Sub _Cancel(ByVal t As Type, ByVal key As String) Implements IRelations.Cancel
            Using mc As IGetManager = GetMgr()
                Dim el As M2MRelation = GetM2M(t, key)
                el.Reject(mc.Manager, True)
            End Using
        End Sub

        Protected Sub _Cancel(ByVal desc As RelationDesc) Implements IRelations.Cancel
            Throw New NotImplementedException
        End Sub

        Protected Function _GetAll() As IList(Of Relation) 'Implements _IOrmBase.GetAllEditable
            Return _relations
        End Function

        Public ReadOnly Property Relations() As IRelations
            Get
                Return Me
            End Get
        End Property

        Public Function GetRelation(ByVal desc As RelationDesc) As Entities.Relation Implements IRelations.GetRelation
            Using GetSyncRoot()
                For Each rl As Relation In _relations
                    If rl.Relation.Equals(desc) Then
                        Return rl
                    End If
                Next
                Dim nrl As Relation = Nothing
                If TypeOf desc Is M2MRelationDesc Then
                    nrl = New M2MRelation(Me, CType(desc, M2MRelationDesc))
                Else
                    nrl = New Relation(Me, desc)
                End If
                _relations.Add(nrl)
                Return nrl
            End Using
        End Function

        Public Function GetRelation(ByVal en As String) As Entities.Relation Implements IRelations.GetRelation
            Return GetRelation(New RelationDesc(New EntityUnion(en), Nothing, Nothing))
        End Function

        Public Function GetRelation(ByVal en As String, ByVal key As String) As Entities.Relation Implements IRelations.GetRelation
            Return GetRelation(New RelationDesc(New EntityUnion(en), Nothing, key))
        End Function

        Public Function GetRelation(ByVal t As System.Type) As Entities.Relation Implements IRelations.GetRelation
            Return GetM2M(t, Nothing)
        End Function

        Public Function GetRelation(ByVal t As System.Type, ByVal key As String) As Entities.Relation Implements IRelations.GetRelation
            Return GetM2M(t, key)
        End Function

        Public Function GetRelationSchema(ByVal en As String) As Meta.RelationDesc Implements IRelations.GetRelationDesc
            Throw New NotImplementedException
        End Function

        Public Function GetRelationSchema(ByVal en As String, ByVal key As String) As Meta.RelationDesc Implements IRelations.GetRelationDesc
            Throw New NotImplementedException
        End Function

        Public Function GetRelationSchema(ByVal t As System.Type) As Meta.RelationDesc Implements IRelations.GetRelationDesc
            Return GetRelationSchema(t, Nothing)
        End Function

        Public Function GetRelationSchema(ByVal t As System.Type, ByVal key As String) As Meta.RelationDesc Implements IRelations.GetRelationDesc
            Dim s As ObjectMappingEngine = MappingEngine
            Dim m2m As M2MRelationDesc = s.GetM2MRelation(Me.GetType, t, key)
            If m2m Is Nothing Then
                Throw New ArgumentException(String.Format("Invalid type {0} or key {1}", t.ToString, key))
            Else
                Return m2m
            End If
        End Function

        Public Function GetAllRelation() As System.Collections.Generic.IList(Of Entities.Relation) Implements IRelations.GetAllRelation
            Return _GetAll()
        End Function

        Private Function Search(ByVal text As String, ByVal t As System.Type) As Worm.Query.RelationCmd Implements IRelations.Search
            Dim q As Worm.Query.QueryCmd = CreateRelCmd(New EntityUnion(t)).From(New SearchFragment(t, text))
            Return CType(q, RelationCmd)
        End Function

        Private Function Search(ByVal text As String, ByVal t As System.Type, ByVal key As String) As Worm.Query.RelationCmd Implements IRelations.Search
            Dim q As Worm.Query.QueryCmd = CreateRelCmd(New EntityUnion(t), key).From(New SearchFragment(t, text))
            Return CType(q, RelationCmd)
        End Function

        Public Function Search(ByVal text As String, ByVal top As Integer, ByVal t As System.Type, ByVal key As String) As Worm.Query.RelationCmd Implements IRelations.Search
            Dim q As Worm.Query.QueryCmd = CreateRelCmd(New EntityUnion(t), key).From(New SearchFragment(t, text, top))
            Return CType(q, RelationCmd)
        End Function

        Public Function Search(ByVal text As String, ByVal type As Meta.SearchType, ByVal top As Integer, ByVal t As System.Type, ByVal key As String) As Worm.Query.RelationCmd Implements IRelations.Search
            Dim q As Worm.Query.QueryCmd = CreateRelCmd(New EntityUnion(t), key).From(New SearchFragment(t, text, type, top))
            Return CType(q, RelationCmd)
        End Function

        Public Function Search(ByVal text As String, ByVal type As Meta.SearchType, ByVal queryFields() As String, ByVal top As Integer, ByVal t As System.Type, ByVal key As String) As Worm.Query.RelationCmd Implements IRelations.Search
            Dim q As Worm.Query.QueryCmd = CreateRelCmd(New EntityUnion(t), key).From(New SearchFragment(t, text, type, queryFields, top))
            Return CType(q, RelationCmd)
        End Function

        Public Function Search(ByVal text As String, ByVal type As Meta.SearchType, ByVal queryFields() As String, ByVal t As System.Type, ByVal key As String) As Worm.Query.RelationCmd Implements IRelations.Search
            Dim q As Worm.Query.QueryCmd = CreateRelCmd(New EntityUnion(t), key).From(New SearchFragment(t, text, type, queryFields))
            Return CType(q, RelationCmd)
        End Function
        Public Function Search(ByVal text As String, ByVal type As Meta.SearchType, ByVal t As System.Type, ByVal key As String) As Worm.Query.RelationCmd Implements IRelations.Search
            Dim q As Worm.Query.QueryCmd = CreateRelCmd(New EntityUnion(t), key).From(New SearchFragment(t, text, type))
            Return CType(q, RelationCmd)
        End Function

        'Public Function Search(ByVal text As String) As Worm.Query.RelationCmd Implements IRelations.Search
        '    Dim q As Worm.Query.QueryCmd = CreateRelCmd(New EntityUnion(t)).From(New SearchFragment(text))
        '    Return CType(q, RelationCmd)
        'End Function

        'Public Function Search(ByVal text As String, ByVal top As Integer, ByVal key As String) As Worm.Query.RelationCmd Implements IRelations.Search
        '    Dim q As Worm.Query.QueryCmd = CreateRelCmd(New EntityUnion(t), key).From(New SearchFragment(text, top))
        '    Return CType(q, RelationCmd)
        'End Function

        'Public Function Search(ByVal text As String, ByVal key As String) As Worm.Query.RelationCmd Implements IRelations.Search
        '    Dim q As Worm.Query.QueryCmd = CreateRelCmd(New EntityUnion(t), key).From(New SearchFragment(text))
        '    Return CType(q, RelationCmd)
        'End Function

        'Public Function Search(ByVal text As String, ByVal type As Meta.SearchType, ByVal top As Integer, ByVal key As String) As Worm.Query.RelationCmd Implements IRelations.Search
        '    Dim q As Worm.Query.QueryCmd = CreateRelCmd(New EntityUnion(t), key).From(New SearchFragment(text, type, top))
        '    Return CType(q, RelationCmd)
        'End Function

        'Public Function Search(ByVal text As String, ByVal type As Meta.SearchType, ByVal key As String) As Worm.Query.RelationCmd Implements IRelations.Search
        '    Dim q As Worm.Query.QueryCmd = CreateRelCmd(New EntityUnion(t), key).From(New SearchFragment(text, type))
        '    Return CType(q, RelationCmd)
        'End Function

        'Public Function Search(ByVal text As String, ByVal type As Meta.SearchType, ByVal queryFields() As String, ByVal top As Integer, ByVal key As String) As Worm.Query.RelationCmd Implements IRelations.Search
        '    Dim q As Worm.Query.QueryCmd = CreateRelCmd(New EntityUnion(t), key).From(New SearchFragment(text, type, top, queryFields))
        '    Return CType(q, RelationCmd)
        'End Function

        'Public Function Search(ByVal text As String, ByVal type As Meta.SearchType, ByVal queryFields() As String, ByVal key As String) As Worm.Query.RelationCmd Implements IRelations.Search
        '    Dim q As Worm.Query.QueryCmd = CreateRelCmd(New EntityUnion(t), key).From(New SearchFragment(text, type, queryFields))
        '    Return CType(q, RelationCmd)
        'End Function

#End Region

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
        NotFoundInSource
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

    'Public Interface IOrmProxy(Of T As {OrmBase, New})
    '    ReadOnly Property ID() As Integer
    '    ReadOnly Property Entity() As T
    'End Interface

    '<Serializable()> _
    'Public Class OrmProxy(Of T As {OrmBase, New})
    '    Implements IOrmProxy(Of T)

    '    Private _id As Integer

    '    Public Sub New(ByVal id As Integer)
    '        _id = id
    '    End Sub

    '    Public ReadOnly Property Entity() As T Implements IOrmProxy(Of T).Entity
    '        Get
    '            Return OrmManager.CurrentManager.Find(Of T)(_id)
    '        End Get
    '    End Property

    '    Public ReadOnly Property ID() As Integer Implements IOrmProxy(Of T).ID
    '        Get
    '            Return _id
    '        End Get
    '    End Property
    'End Class

    Public Module OrmBaseT
        Public Const PKName As String = "ID"
    End Module

    <Serializable()> _
    Public MustInherit Class OrmBaseT(Of T As {New, OrmBaseT(Of T)})
        Inherits KeyEntity
        Implements IComparable(Of T), IPropertyConverter, IComparable

        Private _id As Object

        Protected Sub New()

        End Sub

        Protected Sub New(ByVal id As Integer, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine)
            MyBase.New()
            MyBase.Init(id, cache, schema)
            'SetObjectStateClear(Orm.ObjectState.Created)
            'Throw New NotSupportedException
        End Sub

        Protected Overrides Sub Init(ByVal id As Object, ByVal cache As Cache.CacheBase, ByVal schema As ObjectMappingEngine)
            MyBase.Init(id, cache, schema)
        End Sub

        Protected Overrides Sub Init()
            MyBase.Init()
        End Sub
        'Protected Overridable Function GetNew() As T
        '    Return New T()
        'End Function

        Public Function CompareTo(ByVal other As T) As Integer Implements System.IComparable(Of T).CompareTo
            If other Is Nothing Then
                'Throw New MediaObjectModelException(ObjName & "other parameter cannot be nothing")
                Return 1
            End If
            Return Math.Sign(ID - other.ID)
            'Return Math.Sign(Identifier - other.Identifier)
        End Function

        'Protected Function CloneMe() As T
        '    Return CType(Clone(), T)
        'End Function

        'Protected Overrides Sub RevertToOriginalVersion()
        '    Dim modified As OrmBase = OriginalCopy
        '    If modified IsNot Nothing Then
        '        Dim editable As IOrmEditable(Of T) = TryCast(Me, IOrmEditable(Of T))
        '        If editable Is Nothing Then
        '            Throw New OrmObjectException(String.Format("Object {0} must implement IOrmEditable to perform this operation", ObjName))
        '        End If
        '        Using SyncHelper(False)
        '            editable.CopyBody(CType(modified, T), CType(Me, T))
        '        End Using
        '    End If
        'End Sub

        'Protected Friend Overrides Function CreateOriginalVersion() As OrmBase
        '    Dim clone As OrmBase = CType(Activator.CreateInstance(Me.GetType), OrmBase)
        '    clone.Init(Identifier, OrmCache, Nothing)
        '    Dim editable As IOrmEditable(Of T) = TryCast(Me, IOrmEditable(Of T))
        '    If editable IsNot Nothing Then
        '        Using SyncHelper(False)
        '            editable.CopyBody(CType(Me, T), CType(clone, T))
        '        End Using
        '    End If
        '    clone._old_state = ObjectState
        '    clone.ObjectState = ObjectState.Clone
        '    'clone._loaded_members = _loaded_members
        '    'clone._loaded = _loaded
        '    Return clone
        'End Function

        'Protected Friend Overrides Sub CopyBodyInternal(ByVal from As OrmBase, ByVal [to] As OrmBase)
        '    Dim editable As IOrmEditable(Of T) = TryCast(Me, IOrmEditable(Of T))
        '    If editable Is Nothing Then
        '        Throw New OrmObjectException(String.Format("Object {0} must implement IOrmEditable to perform this operation", ObjName))
        '    End If
        '    editable.CopyBody(CType(from, T), CType([to], T))
        'End Sub

        'Public ReadOnly Property Entity() As T 'Implements IOrmProxy(Of T).Entity
        '    Get
        '        Return CType(Me, T)
        '    End Get
        'End Property

        Public ReadOnly Property ID() As Integer 'Implements IOrmProxy(Of T).ID
            Get
                Return CInt(Identifier)
            End Get
        End Property

        ''' <summary>
        ''' Идентификатор объекта
        ''' </summary>
        ''' <remarks>Если производный класс имеет составной первичный ключ, это свойство лучше переопределить</remarks>
        <EntityPropertyAttribute(Propertyalias:=OrmBaseT.PKName, Behavior:=Field2DbRelations.PrimaryKey)> _
        Public Overrides Property Identifier() As Object
            Get
                'Using SyncHelper(True)
                Return _id
                'End Using
            End Get
            Set(ByVal value As Object)
                'Using SyncHelper(False)
                'If _id <> value Then
                'If _state = Orm.ObjectState.Created Then
                '    CreateModified(value, ModifiedObject.ReasonEnum.Unknown)
                'End If
                _id = value
                If Not CType(Me, _ICachedEntity).IsPKLoaded Then
                    PKLoaded(1)
                    Dim schema As ObjectMappingEngine = MappingEngine
                    If schema IsNot Nothing Then
                        CType(Me, _ICachedEntity).SetLoaded(GetPKValues()(0).PropertyAlias, True, True, schema)
                    End If
                End If
                'Debug.Assert(_id.Equals(value))
                'End If
                'End Using
            End Set
        End Property

        Public Overrides Function GetPKValues() As PKDesc()
            Return New PKDesc() {New PKDesc(OrmBaseT.PKName, _id)}
        End Function

        Protected Overrides Sub SetPK(ByVal pk() As PKDesc)
            _id = pk(0).Value
        End Sub

        Protected Overrides Sub CopyBody(ByVal from As _IEntity, ByVal [to] As _IEntity)
            Dim editable As IOrmEditable(Of T) = TryCast(Me, IOrmEditable(Of T))
            If editable IsNot Nothing Then
                editable.CopyBody(CType(from, T), CType([to], T))
            Else
                MyBase.CopyBody(from, [to])
            End If
        End Sub

        'Public Overridable Overloads Sub SetValue(ByVal pi As System.Reflection.PropertyInfo, ByVal c As ColumnAttribute, ByVal value As Object)
        '    MyBase.SetValue(pi, c, Nothing, value)
        'End Sub

        Public Overridable Overloads Function CreateObject(ByVal mgr As OrmManager, ByVal propertyAlias As String, ByVal value As Object) As _IEntity Implements IPropertyConverter.CreateObject
            Return Nothing
        End Function

        Protected Overrides Function IsPropertyLoaded(ByVal propertyAlias As String) As Boolean
            If propertyAlias = OrmBaseT.PKName Then
                Return True
            Else
                Return MyBase.IsPropertyLoaded(propertyAlias)
            End If
        End Function

#Region " Operators "

        Public Shared Operator <(ByVal obj1 As OrmBaseT(Of T), ByVal obj2 As OrmBaseT(Of T)) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then
                    Return False
                End If
                Return True
            End If
            Return obj1.CompareTo(CType(obj2, T)) < 0
        End Operator

        Public Shared Operator >(ByVal obj1 As OrmBaseT(Of T), ByVal obj2 As OrmBaseT(Of T)) As Boolean
            If obj1 Is Nothing Then
                If obj2 Is Nothing Then
                    Return False
                End If
                Return False
            End If
            Return obj1.CompareTo(CType(obj2, T)) > 0
        End Operator

#End Region

        Protected Function _CompareTo(ByVal obj As Object) As Integer Implements System.IComparable.CompareTo
            Return CompareTo(TryCast(obj, T))
        End Function
    End Class

End Namespace
