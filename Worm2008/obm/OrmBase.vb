Imports System.Xml
Imports Worm.Sorting
Imports Worm.Cache
Imports Worm.Orm.Meta
Imports Worm.Criteria
Imports System.Collections.Generic
Imports System.ComponentModel
Imports Worm.Criteria.Core

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
    ''' Базовый класс для всех типов
    ''' </summary>
    ''' <remarks>
    ''' Класс является потокобезопасным как на чтение так и на запись.
    ''' Предоставляет следующий функционал:
    ''' XML сериализация/десериализация. Реализована с некоторыми ограничениями. Для изменения поведения необходимо переопределить <see cref="OrmBase.ReadXml" /> и <see cref="OrmBase.WriteXml"/>.
    ''' <code lang="vb">Это код</code>
    ''' <example>Это пример</example>
    ''' </remarks>
    <Serializable()> _
    Public MustInherit Class OrmBase
        Implements IComparable, Serialization.IXmlSerializable

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

        <EditorBrowsable(EditorBrowsableState.Never)> _
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

            Public ReadOnly Property CacheItem() As OrmManagerBase.M2MCache
                Get
                    Return _e
                End Get
            End Property

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

            Public Function Accept(ByVal obj As OrmBase, ByVal mgr As OrmManagerBase) As Boolean
                If _e IsNot Nothing Then
                    Dim leave As Boolean = _e.Filter Is Nothing AndAlso _e.Entry.Accept(mgr)
                    If Not leave Then
                        Dim dic As IDictionary = mgr.GetDic(mgr.Cache, _key)
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
            Private _dst As OrmBase
            Private _props() As String

            Public Sub New(ByVal src As OrmBase, ByVal dst As OrmBase, ByVal properties() As String)
                _dst = dst
                _props = properties
                AddHandler src.Saved, AddressOf Added
            End Sub

            Public Sub Added(ByVal source As OrmBase, ByVal args As OrmBase.ObjectSavedArgs)
                Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
                For Each p As String In _props
                    If p = "ID" Then
                        Dim nm As OrmManagerBase.INewObjects = mgr.NewObjectManager
                        If nm IsNot Nothing Then
                            nm.RemoveNew(_dst)
                        End If
                        _dst._id = source._id
                        If nm IsNot Nothing Then
                            mgr.NewObjectManager.AddNew(_dst)
                        End If
                    Else
                        Dim o As Object = source.GetValue(p)
                        mgr.ObjectSchema.SetFieldValue(_dst, p, o)
                    End If
                Next
                RemoveHandler source.Saved, AddressOf Added
            End Sub
        End Class

        <EditorBrowsable(EditorBrowsableState.Never)> _
        Public Class M2MClass
            Private _o As OrmBase

            Friend Sub New(ByVal o As OrmBase)
                _o = o
            End Sub

            Protected ReadOnly Property GetMgr() As OrmManagerBase
                Get
                    Return _o.GetMgr
                End Get
            End Property

            Public Function Find(ByVal t As Type, ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As IList
                Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public
                Dim mi As Reflection.MethodInfo = _o.GetType.GetMethod("Find", flags, Nothing, Reflection.CallingConventions.Any, _
                    New Type() {GetType(CriteriaLink), GetType(Sort), GetType(Boolean)}, Nothing)
                Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
                Return CType(mi_real.Invoke(_o, flags, Nothing, New Object() {criteria, sort, withLoad}, Nothing), IList)
            End Function

            Public Function Find(Of T As {New, OrmBase})(ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Return GetMgr.FindMany2Many2(Of T)(_o, criteria, sort, True, withLoad)
            End Function

            Public Function Find(Of T As {New, OrmBase})(ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal direct As Boolean, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Return GetMgr.FindMany2Many2(Of T)(_o, criteria, sort, direct, withLoad)
            End Function

            Public Function Find(Of T As {New, OrmBase})() As ReadOnlyList(Of T)
                Return GetMgr.FindMany2Many2(Of T)(_o, Nothing, Nothing, True, False)
            End Function

            Public Function Find(Of T As {New, OrmBase})(ByVal direct As Boolean) As ReadOnlyList(Of T)
                Return GetMgr.FindMany2Many2(Of T)(_o, Nothing, Nothing, True, direct)
            End Function

            Public Function Find(Of T As {New, OrmBase})(ByVal sort As Sort) As ReadOnlyList(Of T)
                Return GetMgr.FindMany2Many2(Of T)(_o, Nothing, sort, True, False)
            End Function

            Public Function Find(Of T As {New, OrmBase})(ByVal sort As Sort, ByVal direct As Boolean) As ReadOnlyList(Of T)
                Return GetMgr.FindMany2Many2(Of T)(_o, Nothing, sort, direct, False)
            End Function

            Public Function Find(ByVal top As Integer, ByVal t As Type, ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As IList
                Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public
                Dim mi As Reflection.MethodInfo = _o.GetType.GetMethod("Find", flags, Nothing, Reflection.CallingConventions.Any, _
                    New Type() {GetType(CriteriaLink), GetType(Sort), GetType(Boolean)}, Nothing)
                Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
                Return CType(mi_real.Invoke(_o, flags, Nothing, New Object() {top, criteria, sort, withLoad}, Nothing), IList)
            End Function

            Public Function Find(Of T As {New, OrmBase})(ByVal top As Integer, ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Return GetMgr.FindMany2Many2(Of T)(_o, criteria, sort, True, withLoad, top)
            End Function

            Public Function Find(Of T As {New, OrmBase})(ByVal top As Integer, ByVal criteria As igetfilter, ByVal sort As Sort, ByVal direct As Boolean, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Return GetMgr.FindMany2Many2(Of T)(_o, criteria, sort, direct, withLoad, top)
            End Function

            Public Function FindDistinct(ByVal t As Type, ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As IList
                Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public
                Dim mi As Reflection.MethodInfo = _o.GetType.GetMethod("FindDistinct", flags, Nothing, Reflection.CallingConventions.Any, _
                    New Type() {GetType(CriteriaLink), GetType(Sort), GetType(Boolean)}, Nothing)
                Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
                Return CType(mi_real.Invoke(_o, flags, Nothing, New Object() {criteria, sort, withLoad}, Nothing), IList)
            End Function

            Public Function FindDistinct(Of T As {New, OrmBase})(ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Return GetMgr.FindMany2Many2(Of T)(_o, criteria, sort, True, withLoad)
            End Function

            Public Function FindDistinct(Of T As {New, OrmBase})(ByVal criteria As IGetFilter, ByVal sort As Sort, ByVal direct As Boolean, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Return GetMgr.FindMany2Many2(Of T)(_o, criteria, sort, direct, withLoad)
            End Function

            Public Sub Add(ByVal obj As OrmBase)
                GetMgr.M2MAdd(_o, obj, True)
            End Sub

            Public Sub Add(ByVal obj As OrmBase, ByVal direct As Boolean)
                GetMgr.M2MAdd(_o, obj, direct)
            End Sub

            Public Sub Delete(ByVal t As Type)
                GetMgr.M2MDelete(_o, t, True)
            End Sub

            Public Sub Delete(ByVal t As Type, ByVal direct As Boolean)
                GetMgr.M2MDelete(_o, t, direct)
            End Sub

            Public Sub Delete(ByVal obj As OrmBase)
                GetMgr.M2MDelete(_o, obj, True)
            End Sub

            Public Sub Delete(ByVal obj As OrmBase, ByVal direct As Boolean)
                GetMgr.M2MDelete(_o, obj, direct)
            End Sub

            Public Sub Cancel(ByVal t As Type)
                GetMgr.M2MCancel(_o, t)
            End Sub

            Public Sub Merge(Of T As {OrmBase, New})(ByVal col As ReadOnlyList(Of T), ByVal removeNotInList As Boolean)
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

            Public Sub Merge(Of T As {OrmBase, New})(ByVal col As ReadOnlyList(Of T), ByVal removeNotInList As Boolean, ByVal direct As Boolean)
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

            Public Function Search(Of T As {OrmBase, New})(ByVal text As String) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function Search(Of T As {OrmBase, New})(ByVal text As String, ByVal sort As Sort) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function Search(Of T As {OrmBase, New})(ByVal text As String, ByVal direct As Boolean) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function Search(Of T As {OrmBase, New})(ByVal text As String, ByVal sort As Boolean, ByVal direct As Boolean) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function Search(Of T As {OrmBase, New})(ByVal text As String, ByVal criteria As IGetFilter, ByVal sort As Boolean) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function

            Public Function Search(Of T As {OrmBase, New})(ByVal text As String, ByVal criteria As IGetFilter, ByVal sort As Boolean, ByVal direct As Boolean) As Worm.ReadOnlyList(Of T)
                Throw New NotImplementedException
            End Function
        End Class

        <EditorBrowsable(EditorBrowsableState.Never)> _
        Public Class InternalClass
            Private _o As OrmBase

            Friend Sub New(ByVal o As OrmBase)
                _o = o
            End Sub

            Protected ReadOnly Property GetMgr() As OrmManagerBase
                Get
                    Return _o.GetMgr
                End Get
            End Property

            Public Property IsLoaded() As Boolean
                Get
                    Return _o._loaded
                End Get
                Protected Friend Set(ByVal value As Boolean)
                    Using _o.SyncHelper(False)
                        If value AndAlso Not _o._loaded Then
                            Dim arr As Generic.List(Of ColumnAttribute) = GetMgr.ObjectSchema.GetSortedFieldList(_o.GetType)
                            For i As Integer = 0 To arr.Count - 1
                                _o._members_load_state(i) = True
                            Next
                        ElseIf Not value AndAlso _o._loaded Then
                            Dim arr As Generic.List(Of ColumnAttribute) = GetMgr.ObjectSchema.GetSortedFieldList(_o.GetType)
                            For i As Integer = 0 To arr.Count - 1
                                _o._members_load_state(i) = False
                            Next
                        End If
                        _o._loaded = value
                        Debug.Assert(_o._loaded = value)
                    End Using
                End Set
            End Property

            Public ReadOnly Property OrmCache() As OrmCacheBase
                Get
                    If GetMgr() IsNot Nothing Then
                        Return GetMgr.Cache
                    Else
                        Return Nothing
                    End If
                End Get
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
                    Return _o._state
                End Get
                Protected Friend Set(ByVal value As ObjectState)
                    Using _o.SyncHelper(False)
                        _o._state = value
                        Debug.Assert(_o._state = value)
                        Debug.Assert(value <> Orm.ObjectState.None OrElse IsLoaded)
                        If value = Orm.ObjectState.None AndAlso Not IsLoaded Then
                            Throw New OrmObjectException(String.Format("Cannot set state none while object {0} is not loaded", ObjName))
                        End If
                    End Using
                End Set
            End Property

            ''' <summary>
            ''' Модифицированная версия объекта
            ''' </summary>
            Public ReadOnly Property OriginalCopy() As OrmBase
                Get
                    'If _mo Is Nothing Then
                    _o.CheckCash()
                    If OrmCache.Modified(_o) Is Nothing Then Return Nothing
                    Return OrmCache.Modified(_o).Obj
                    'Else
                    'Return _mo.Obj
                    'End If
                End Get
            End Property

            Public ReadOnly Property IsReadOnly() As Boolean
                Get
                    Using _o.SyncHelper(True)
                        If _o._state = Orm.ObjectState.Modified Then
                            _o.CheckCash()
                            Dim mo As ModifiedObject = OrmCache.Modified(_o)
                            'If mo Is Nothing Then mo = _mo
                            If mo IsNot Nothing Then
                                If mo.User IsNot Nothing AndAlso Not mo.User.Equals(GetMgr.CurrentUser) Then
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

            Public ReadOnly Property Changes(ByVal obj As OrmBase) As ColumnAttribute()
                Get
                    Dim columns As New Generic.List(Of ColumnAttribute)
                    Dim t As Type = obj.GetType
                    For Each pi As Reflection.PropertyInfo In _o.GetType.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic)
                        Dim c As ColumnAttribute = CType(Attribute.GetCustomAttribute(pi, GetType(ColumnAttribute), True), ColumnAttribute)
                        If c IsNot Nothing Then
                            Dim original As Object = pi.GetValue(obj, Nothing)
                            If (_o.OrmSchema.GetAttributes(t, c) And Field2DbRelations.ReadOnly) <> Field2DbRelations.ReadOnly Then
                                Dim current As Object = pi.GetValue(_o, Nothing)
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

            Public ReadOnly Property ObjName() As String
                Get
                    Return _o.GetType.Name & " - " & ObjectState.ToString & " (" & _o._id & "): "
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
                        Dim t As Type = _o.GetType
                        'Dim o As OrmBase = CType(t.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, _
                        '    New Object() {Identifier, OrmCache, _schema}), OrmBase)
                        'Dim o As OrmBase = GetNew()
                        Dim o As OrmBase = CType(Activator.CreateInstance(t), OrmBase)
                        o.Init(_o.Identifier, _o.OrmCache, _o.OrmSchema)
                        For Each c As ColumnAttribute In Changes(o)
                            sb.Append(vbTab).Append(c.FieldName).Append(vbCrLf)
                        Next
                    End If
                    Return sb.ToString
                End Get
            End Property
        End Class

        Public Class PropertyChangedEventArgs
            Inherits EventArgs

            Private _prev As Object
            Public ReadOnly Property PreviousValue() As Object
                Get
                    Return _prev
                End Get
            End Property

            Private _current As Object
            Public ReadOnly Property CurrentValue() As Object
                Get
                    Return _current
                End Get
            End Property

            Private _fieldName As String
            Public ReadOnly Property FieldName() As String
                Get
                    Return _fieldName
                End Get
            End Property

            Public Sub New(ByVal fieldName As String, ByVal prevValue As Object, ByVal currentValue As Object)
                _fieldName = fieldName
                _prev = prevValue
                _current = currentValue
            End Sub
        End Class

        Private Class ChangedEventHelper
            Implements IDisposable

            Private _value As Object
            Private _fieldName As String
            Private _obj As OrmBase
            Private _d As IDisposable

            Public Sub New(ByVal obj As OrmBase, ByVal fieldName As String, ByVal d As IDisposable)
                _fieldName = fieldName
                _obj = obj
                _value = obj.GetValue(fieldName)
                _d = d
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
                _d.Dispose()
                _obj.RaisePropertyChanged(_fieldName, _value)
            End Sub
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
        Public Const OrmNamespace As String = "http://www.worm.ru/orm/"
        <NonSerialized()> _
        Friend _loading As Boolean
        <NonSerialized()> _
        Protected Friend _needAdd As Boolean
        <NonSerialized()> _
        Protected Friend _needDelete As Boolean
        <NonSerialized()> _
        Protected Friend _needAccept As New Generic.List(Of AcceptState2)
        <NonSerialized()> _
        Protected Friend _mgrStr As String
        <NonSerialized()> _
        Protected _dontRaisePropertyChange As Boolean

        Public Event Saved(ByVal sender As OrmBase, ByVal args As ObjectSavedArgs)
        Public Event Added(ByVal sender As OrmBase, ByVal args As EventArgs)
        Public Event Deleted(ByVal sender As OrmBase, ByVal args As EventArgs)
        Public Event Updated(ByVal sender As OrmBase, ByVal args As EventArgs)
        Public Event PropertyChanged(ByVal sender As OrmBase, ByVal args As PropertyChangedEventArgs)

        'for xml serialization
        Public Sub New()
            'Dim arr As Generic.List(Of ColumnAttribute) = OrmManagerBase.CurrentMediaContent.DatabaseSchema.GetSortedFieldList(Me.GetType)
            'members_load_state = New BitArray(arr.Count)

            Init()
        End Sub

#Region " Protected functions "
        Friend Overridable ReadOnly Property ChangeDescription() As String
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
                    Dim o As OrmBase = CType(Activator.CreateInstance(t), OrmBase)
                    o.Init(Identifier, OrmCache, OrmSchema)
                    For Each c As ColumnAttribute In Changes(o)
                        sb.Append(vbTab).Append(c.FieldName).Append(vbCrLf)
                    Next
                End If
                Return sb.ToString
            End Get
        End Property

        Friend Property IsLoaded() As Boolean
            Get
                Return _loaded
            End Get
            Set(ByVal value As Boolean)
                Using SyncHelper(False)
                    If value AndAlso Not _loaded Then
                        Dim arr As Generic.List(Of ColumnAttribute) = GetMgr.ObjectSchema.GetSortedFieldList(Me.GetType)
                        For i As Integer = 0 To arr.Count - 1
                            _members_load_state(i) = True
                        Next
                    ElseIf Not value AndAlso _loaded Then
                        Dim arr As Generic.List(Of ColumnAttribute) = GetMgr.ObjectSchema.GetSortedFieldList(Me.GetType)
                        For i As Integer = 0 To arr.Count - 1
                            _members_load_state(i) = False
                        Next
                    End If
                    _loaded = value
                    Debug.Assert(_loaded = value)
                End Using
            End Set
        End Property

        Friend ReadOnly Property OrmCache() As OrmCacheBase
            Get
                If GetMgr() IsNot Nothing Then
                    Return GetMgr.Cache
                Else
                    Return Nothing
                End If
            End Get
        End Property

        '''' <summary>
        '''' Объект, на котором можно синхронизировать загрузку
        '''' </summary>
        'Public ReadOnly Property SyncLoad() As Object
        '    Get
        '        Return Me
        '    End Get
        'End Property

        Protected Friend Property ObjectState() As ObjectState
            Get
                Return _state
            End Get
            Set(ByVal value As ObjectState)
                Using SyncHelper(False)
                    _state = value
                    Debug.Assert(_state = value)
                    Debug.Assert(value <> Orm.ObjectState.None OrElse IsLoaded)
                    If value = Orm.ObjectState.None AndAlso Not IsLoaded Then
                        Throw New OrmObjectException(String.Format("Cannot set state none while object {0} is not loaded", ObjName))
                    End If
                End Using
            End Set
        End Property

        ''' <summary>
        ''' Модифицированная версия объекта
        ''' </summary>
        Friend ReadOnly Property OriginalCopy() As OrmBase
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

        Friend ReadOnly Property IsReadOnly() As Boolean
            Get
                Using SyncHelper(True)
                    If _state = Orm.ObjectState.Modified Then
                        CheckCash()
                        Dim mo As ModifiedObject = OrmCache.Modified(Me)
                        'If mo Is Nothing Then mo = _mo
                        If mo IsNot Nothing Then
                            If mo.User IsNot Nothing AndAlso Not mo.User.Equals(GetMgr.CurrentUser) Then
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

        Friend ReadOnly Property Changes(ByVal obj As OrmBase) As ColumnAttribute()
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

        Friend ReadOnly Property ObjName() As String
            Get
                Return Me.GetType.Name & " - " & ObjectState.ToString & " (" & _id & "): "
            End Get
        End Property

        Protected Sub RaisePropertyChanged(ByVal fieldName As String, ByVal oldValue As Object)
            Dim value As Object = GetValue(fieldName)
            If Not Object.Equals(value, oldValue) Then
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(fieldName, oldValue, value))
            End If
        End Sub

        Protected Sub New(ByVal id As Integer, ByVal cache As OrmCacheBase, ByVal schema As QueryGenerator)
            Init(id, cache, schema)
            Init()
        End Sub

        <Obsolete()> _
        Friend Sub Init(ByVal id As Integer, ByVal cache As OrmCacheBase, ByVal schema As QueryGenerator)
            Me._id = id

            If schema IsNot Nothing Then
                Dim arr As Generic.List(Of ColumnAttribute) = schema.GetSortedFieldList(Me.GetType)
                _loaded_members = New BitArray(arr.Count)
            End If

            If cache IsNot Nothing Then cache.RegisterCreation(Me.GetType, id)
        End Sub

        Friend Sub Init(ByVal cache As OrmCacheBase, ByVal schema As QueryGenerator)

            If schema IsNot Nothing Then
                Dim arr As Generic.List(Of ColumnAttribute) = schema.GetSortedFieldList(Me.GetType)
                _loaded_members = New BitArray(arr.Count)
            End If

            If cache IsNot Nothing Then cache.RegisterCreation(Me.GetType, Identifier)

            ObjectState = Orm.ObjectState.NotLoaded
        End Sub

        Protected Sub Init()
            'If OrmManagerBase.CurrentManager IsNot Nothing Then
            '    _mgrStr = OrmManagerBase.CurrentManager.IdentityString
            'End If
        End Sub

        <Runtime.Serialization.OnDeserialized()> _
        Private Sub Init(ByVal context As Runtime.Serialization.StreamingContext)
            Init()
            If OrmManagerBase.CurrentManager IsNot Nothing AndAlso ObjectState <> Orm.ObjectState.Created Then
                OrmManagerBase.CurrentManager.RegisterInCashe(Me)
            End If
        End Sub

        Protected ReadOnly Property OrmSchema() As QueryGenerator
            Get
                If GetMgr() Is Nothing Then
                    Return Nothing
                Else
                    Return GetMgr.ObjectSchema
                End If
            End Get
        End Property

        Protected Property _members_load_state(ByVal idx As Integer) As Boolean
            Get
                If _loaded_members Is Nothing Then
                    Dim arr As Generic.List(Of ColumnAttribute) = GetMgr.ObjectSchema.GetSortedFieldList(Me.GetType)
                    _loaded_members = New BitArray(arr.Count)
                End If
                Return _loaded_members(idx)
            End Get
            Set(ByVal value As Boolean)
                If _loaded_members Is Nothing Then
                    Dim arr As Generic.List(Of ColumnAttribute) = GetMgr.ObjectSchema.GetSortedFieldList(Me.GetType)
                    _loaded_members = New BitArray(arr.Count)
                End If
                _loaded_members(idx) = value
            End Set
        End Property

        Friend Function SetLoaded(ByVal c As ColumnAttribute, ByVal loaded As Boolean, Optional ByVal check As Boolean = True) As Boolean

            Dim idx As Integer = c.Index
            If idx = -1 Then
                Dim arr As Generic.List(Of ColumnAttribute) = GetMgr.ObjectSchema.GetSortedFieldList(Me.GetType)
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

        Friend Function CheckIsAllLoaded(ByVal schema As QueryGenerator) As Boolean
            Using SyncHelper(False)
                Dim allloaded As Boolean = True
                If Not _loaded Then
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

        Friend Sub SetObjectState(ByVal state As ObjectState)
            _state = state
        End Sub

        Protected Sub CheckCash()
            If OrmCache Is Nothing Then
                Throw New OrmObjectException(ObjName & "The object is floating and has not cashe that is needed to perform this operation")
            End If
            If GetMgr() Is Nothing Then
                Throw New OrmObjectException(ObjName & "You have to create MediaContent object to perform this operation")
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
                If OriginalCopy IsNot Nothing Then
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

        Protected Friend Function GetMgr() As OrmManagerBase
            Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
            If Not String.IsNullOrEmpty(_mgrStr) Then
                Do While mgr IsNot Nothing AndAlso mgr.IdentityString <> _mgrStr
                    mgr = mgr._prev
                Loop
            End If
            Return mgr
        End Function

        Protected Friend Function AcceptChanges(ByVal updateCache As Boolean, ByVal setState As Boolean) As OrmBase
            CheckCash()
            Dim mo As OrmBase = Nothing
            Using SyncHelper(False)
                Dim t As Type = Me.GetType
                Dim mc As OrmManagerBase = GetMgr()
                Dim valProcs As Boolean
                For Each acs As AcceptState2 In _needAccept
                    acs.Accept(Me, mc)
                    valProcs = True
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
                    Dim c As New OrmManagerBase.M2MEnum(rel, Me, mc.ObjectSchema)
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
                    If setState Then
                        ObjectState = Orm.ObjectState.None
                        Debug.Assert(IsLoaded)
                        If Not IsLoaded Then
                            Throw New OrmObjectException("Cannot set state None while object is not loaded")
                        End If
                    End If
                    If unreg Then
                        mo = OriginalCopy
                        OrmCache.UnregisterModification(Me)
                        '_mo = Nothing
                    End If

                    If _needDelete Then
                        If updateCache Then
                            mc.Cache.UpdateCache(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing, Nothing, Nothing)
                            'mc.Cache.UpdateCacheOnDelete(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing)
                            Accept_AfterUpdateCacheDelete(Me, mc)
                        End If
                        RaiseEvent Deleted(Me, EventArgs.Empty)
                    ElseIf _needAdd Then
                        Dim dic As IDictionary = mc.GetDictionary(Me.GetType)
                        Dim o As OrmBase = CType(dic(Identifier), OrmBase)
                        If (o Is Nothing) OrElse (Not o.IsLoaded AndAlso IsLoaded) Then
                            dic(Identifier) = Me
                        End If
                        RaiseEvent Added(Me, EventArgs.Empty)
                        If updateCache Then
                            'mc.Cache.UpdateCacheOnAdd(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing, Nothing)
                            mc.Cache.UpdateCache(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing, Nothing, Nothing)
                            Accept_AfterUpdateCacheAdd(Me, mc, mo)
                        End If
                    Else
                        If valProcs Then
                            mc.Cache.ValidateSPOnUpdate(Me, Nothing)
                        End If
                        RaiseEvent Updated(Me, EventArgs.Empty)
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
                        Throw New InvalidOperationException(String.Format("Object {0} is not loaded while the state is None", ObjName))
                    End If

                    If _state = Orm.ObjectState.NotLoaded Then
                        Load()
                    Else
                        Return
                    End If
                End If

                Dim mo As ModifiedObject = OrmCache.Modified(Me)
                If mo IsNot Nothing Then
                    If mo.User IsNot Nothing AndAlso Not mo.User.Equals(GetMgr.CurrentUser) Then
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

        Protected Function PrepareRead(ByVal fieldName As String, ByVal d As IDisposable) As IDisposable
            If Not IsLoaded AndAlso (_state = Orm.ObjectState.NotLoaded OrElse _state = Orm.ObjectState.None) Then
                d = SyncHelper(True)
                If Not IsLoaded AndAlso (_state = Orm.ObjectState.NotLoaded OrElse _state = Orm.ObjectState.None) AndAlso Not IsFieldLoaded(fieldName) Then
                    Load()
                End If
            End If
            Return d
        End Function

        Protected Friend Sub RaiseBeginModification()
            Dim modified As OrmBase = GetSoftClone()
            ObjectState = Orm.ObjectState.Modified
            OrmCache.RegisterModification(modified, Identifier)
            If Not _loading Then
                GetMgr.RaiseBeginUpdate(Me)
            End If
        End Sub

        <Obsolete()> _
        Protected Friend Sub CreateModified(ByVal id As Integer)
            Dim modified As OrmBase = GetSoftClone()
            ObjectState = Orm.ObjectState.Modified
            OrmCache.RegisterModification(modified, id)
            If Not _loading Then
                GetMgr.RaiseBeginUpdate(Me)
            End If
        End Sub

        Protected Sub CreateModified()
            Dim modified As OrmBase = GetFullClone()
            ObjectState = Orm.ObjectState.Modified
            OrmCache.RegisterModification(modified)
            If Not _loading Then
                GetMgr.RaiseBeginUpdate(Me)
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

        Protected Friend Function GetName() As String
            Return Me.GetType.Name & _id
        End Function

        Protected Friend Function GetOldName(ByVal id As Integer) As String
            Return Me.GetType.Name & id
        End Function

        Friend Overridable Function ForseUpdate(ByVal c As ColumnAttribute) As Boolean
            Return False
        End Function

        Protected Friend Sub SetPK(ByVal fieldName As String, ByVal value As Object)
            _id = CInt(value)
        End Sub

        Protected Friend Overridable Sub RemoveFromCache(ByVal cache As OrmCacheBase)

        End Sub

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

        Protected Friend Function GetAccept(ByVal m As OrmManagerBase.M2MCache) As AcceptState2
            Using SyncHelper(False)
                For Each a As AcceptState2 In _needAccept
                    If a.CacheItem Is m Then
                        Return a
                    End If
                Next
            End Using
            Return Nothing
        End Function

        Protected Friend Sub RaiseSaved(ByVal sa As OrmManagerBase.SaveAction)
            RaiseEvent Saved(Me, New ObjectSavedArgs(sa))
        End Sub
#End Region

#Region " Public properties "
        Public ReadOnly Property M2M() As M2MClass
            Get
                Return New M2MClass(Me)
            End Get
        End Property

        Public ReadOnly Property InternalProperties() As InternalClass
            Get
                Return New InternalClass(Me)
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

#End Region

#Region " Synchronization "

        Friend Function GetSyncRoot() As IDisposable
            Return SyncHelper(False)
        End Function

        Protected Overridable Function SyncHelper(ByVal reader As Boolean) As IDisposable
#If DebugLocks Then
            Return New CSScopeMgr_Debug(Me, "d:\temp\")
#Else
            Return New CSScopeMgr(Me)
#End If
        End Function

        Protected Function Read(ByVal fieldName As String) As IDisposable
            Return SyncHelper(True, fieldName)
        End Function

        Protected Function Write(ByVal fieldName As String) As IDisposable
            Return SyncHelper(False, fieldName)
        End Function

        Protected Friend Function SyncHelper(ByVal reader As Boolean, ByVal fieldName As String) As IDisposable
            Dim err As Boolean = True
            Dim d As IDisposable = New BlankSyncHelper(Nothing)
            Try
                If reader Then
                    d = PrepareRead(fieldName, d)
                Else
                    d = SyncHelper(True)
                    PrepareUpdate()
                    If Not _dontRaisePropertyChange AndAlso Not _loading Then
                        d = New ChangedEventHelper(Me, fieldName, d)
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

#End Region

#Region " Public functions "

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
                    If Not mo.User.Equals(GetMgr.CurrentUser) Then
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
            GetMgr.LoadObject(Me)
            If olds = Orm.ObjectState.Created AndAlso _state = Orm.ObjectState.Modified Then
                AcceptChanges(True, True)
            ElseIf IsLoaded Then
                ObjectState = Orm.ObjectState.None
            End If
            Invariant()
        End Sub

        Public Function Save(ByVal AcceptChanges As Boolean) As Boolean
            Return GetMgr.SaveAll(Me, AcceptChanges)
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
                Dim mc As OrmManagerBase = GetMgr()
                Dim rel As IRelation = mc.ObjectSchema.GetConnectedTypeRelation(t)
                If rel IsNot Nothing Then
                    Dim c As New OrmManagerBase.M2MEnum(rel, Me, mc.ObjectSchema)
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
                        Throw New OrmObjectException(ObjName & " object in readonly state")
                    End If
                    'Debug.WriteLine(Environment.StackTrace)
                    _needAdd = False
                    _needDelete = False

                    If OriginalCopy Is Nothing Then
                        RejectChangesInternal()
                        'OrmManagerBase.WriteError("ModifiedObject is nothing (" & ObjName & ")")
                        Return
                    End If
                    Dim oldid As Integer = Identifier
                    Dim olds As ObjectState = OriginalCopy._old_state
                    If olds <> Orm.ObjectState.Created Then RejectChangesInternal()
                    ObjectState = Orm.ObjectState.Modified
                    Dim newid As Integer = OriginalCopy.Identifier
                    AcceptChanges(True, True)
                    Identifier = newid
                    ObjectState = olds
                    If _state = Orm.ObjectState.Created Then
                        Dim name As String = Me.GetType.Name
                        Dim mc As OrmManagerBase = GetMgr()
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
            AcceptChanges(True, True)
        End Sub

        Public Function IsFieldLoaded(ByVal fieldName As String) As Boolean
            Dim c As New ColumnAttribute(fieldName)
            Dim arr As Generic.List(Of ColumnAttribute) = GetMgr.ObjectSchema.GetSortedFieldList(Me.GetType)
            Dim idx As Integer = arr.BinarySearch(c)
            If idx < 0 Then Throw New OrmObjectException("There is no such field " & fieldName)
            Return _members_load_state(idx)
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
                    If mo.User IsNot Nothing AndAlso Not mo.User.Equals(GetMgr.CurrentUser) Then
                        Throw New OrmObjectException(ObjName & "Object has already altered by user " & mo.User.ToString)
                    End If
                Else
                    If _state = Orm.ObjectState.NotLoaded Then
                        Load()
                    End If

                    CreateModified(Identifier)
                    'Dim modified As OrmBase = CloneMe()
                    'modified._old_state = modified.ObjectState
                    'modified.ObjectState = ObjectState.Clone
                    'OrmCache.RegisterModification(modified)
                End If
                ObjectState = ObjectState.Deleted
                GetMgr.RaiseBeginDelete(Me)
            End Using
        End Sub

        <EditorBrowsable(EditorBrowsableState.Never)> _
        <Conditional("DEBUG")> _
        Public Sub Invariant()
            If IsLoaded AndAlso _
                _state <> Orm.ObjectState.None AndAlso _state <> Orm.ObjectState.Modified AndAlso _state <> Orm.ObjectState.Deleted Then Throw New OrmObjectException(ObjName & "When object is loaded its state has to be None or Modified: current state is " & _state.ToString)
        End Sub

        Public Function EnsureLoaded() As OrmBase
            'OrmManagerBase.CurrentMediaContent.LoadObject(Me)
            Return GetMgr.LoadType(_id, Me.GetType, True, True)
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
                Dim s As QueryGenerator = OrmSchema
                If s Is Nothing Then
                    Return QueryGenerator.GetFieldValueSchemaless(Me, propAlias)
                Else
                    Return s.GetFieldValue(Me, propAlias, Nothing)
                End If
            End If
        End Function

        Public Overridable Function GetValue(ByVal propAlias As String, ByVal schema As IOrmObjectSchemaBase) As Object
            If propAlias = "ID" Then
                'Throw New OrmObjectException("Use Identifier property to get ID")
                Return Identifier
            Else
                Dim s As QueryGenerator = OrmSchema
                If s Is Nothing Then
                    Return QueryGenerator.GetFieldValueSchemaless(Me, propAlias, schema)
                Else
                    Return s.GetFieldValue(Me, propAlias, schema)
                End If
            End If
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)> _
        Public Overridable Sub CreateObject(ByVal fieldName As String, ByVal value As Object)

        End Sub
#End Region

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
            Dim schema As QueryGenerator = GetMgr.ObjectSchema
            If schema IsNot Nothing Then CheckIsAllLoaded(schema)

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
                            Dim v As OrmBase = GetMgr.CreateDBObject(CInt(.Value), pi.PropertyType)
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


#End Region

        Public Shared Function IsGoodState(ByVal state As ObjectState) As Boolean
            Return state = ObjectState.Modified OrElse state = ObjectState.Created OrElse state = ObjectState.Deleted
        End Function

#Region " Operators "

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

#End Region

        '#Region " Obsolete "

        '        <Obsolete("Use M2M.Find")> _
        '        Public Function Find(ByVal t As Type, ByVal criteria As CriteriaLink, ByVal sort As Sort, ByVal withLoad As Boolean) As IList
        '            Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public
        '            Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("Find", flags, Nothing, Reflection.CallingConventions.Any, _
        '                New Type() {GetType(CriteriaLink), GetType(Sort), GetType(Boolean)}, Nothing)
        '            Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
        '            Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {criteria, sort, withLoad}, Nothing), IList)
        '        End Function

        '        <Obsolete("Use M2M.Find")> _
        '        Public Function Find(Of T As {New, OrmBase})(ByVal criteria As CriteriaLink, ByVal sort As Sort, ByVal withLoad As Boolean) As Generic.ICollection(Of T)
        '            Return GetMgr.FindMany2Many2(Of T)(Me, criteria, sort, True, withLoad)
        '        End Function

        '        <Obsolete("Use M2M.Find")> _
        '        Public Function Find(Of T As {New, OrmBase})(ByVal criteria As CriteriaLink, ByVal sort As Sort, ByVal direct As Boolean, ByVal withLoad As Boolean) As Generic.ICollection(Of T)
        '            Return GetMgr.FindMany2Many2(Of T)(Me, criteria, sort, direct, withLoad)
        '        End Function

        '        <Obsolete("Use M2M.Add")> _
        '        Public Sub Add(ByVal obj As OrmBase)
        '            GetMgr.M2MAdd(Me, obj, True)
        '        End Sub

        '        <Obsolete("Use M2M.Add")> _
        '        Public Sub Add(ByVal obj As OrmBase, ByVal direct As Boolean)
        '            GetMgr.M2MAdd(Me, obj, direct)
        '        End Sub

        '        <Obsolete("Use M2M.Delete")> _
        '        Public Sub Delete(ByVal t As Type)
        '            GetMgr.M2MDelete(Me, t, True)
        '        End Sub

        '        <Obsolete("Use M2M.Delete")> _
        '        Public Sub Delete(ByVal t As Type, ByVal direct As Boolean)
        '            GetMgr.M2MDelete(Me, t, direct)
        '        End Sub

        '        <Obsolete("Use M2M.Delete")> _
        '        Public Sub Delete(ByVal obj As OrmBase)
        '            GetMgr.M2MDelete(Me, obj, True)
        '        End Sub

        '        <Obsolete("Use M2M.Delete")> _
        '        Public Sub Delete(ByVal obj As OrmBase, ByVal direct As Boolean)
        '            GetMgr.M2MDelete(Me, obj, direct)
        '        End Sub

        '        <Obsolete("Use M2M.Cancel")> _
        '        Public Sub Cancel(ByVal t As Type)
        '            GetMgr.M2MCancel(Me, t)
        '        End Sub

        '#End Region

        Protected Friend MustOverride Sub CopyBodyInternal(ByVal [from] As OrmBase, ByVal [to] As OrmBase)
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

    Public Interface IOrmProxy(Of T As {OrmBase, New})
        ReadOnly Property ID() As Integer
        ReadOnly Property Entity() As T
    End Interface

    <Serializable()> _
    Public Class OrmProxy(Of T As {OrmBase, New})
        Implements IOrmProxy(Of T)

        Private _id As Integer

        Public Sub New(ByVal id As Integer)
            _id = id
        End Sub

        Public ReadOnly Property Entity() As T Implements IOrmProxy(Of T).Entity
            Get
                Return OrmManagerBase.CurrentManager.Find(Of T)(_id)
            End Get
        End Property

        Public ReadOnly Property ID() As Integer Implements IOrmProxy(Of T).ID
            Get
                Return _id
            End Get
        End Property
    End Class

    <Serializable()> _
    Public MustInherit Class OrmBaseT(Of T As {New, OrmBaseT(Of T)})
        Inherits OrmBase
        Implements IComparable(Of T), ICloneable, IOrmProxy(Of T)

        Protected Sub New()

        End Sub

        Protected Sub New(ByVal id As Integer, ByVal cache As OrmCacheBase, ByVal schema As QueryGenerator)
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

        Public Overridable Function Clone() As Object Implements System.ICloneable.Clone
            Dim o As OrmBase = CType(Activator.CreateInstance(Me.GetType), OrmBase)
            o.Init(Identifier, OrmCache, OrmSchema)
            Using SyncHelper(True)
                o.SetObjectState(ObjectState)
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
            Dim modified As OrmBase = OriginalCopy
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

        Public ReadOnly Property Entity() As T Implements IOrmProxy(Of T).Entity
            Get
                Return CType(Me, T)
            End Get
        End Property

        Public ReadOnly Property ID() As Integer Implements IOrmProxy(Of T).ID
            Get
                Return Identifier
            End Get
        End Property
    End Class

End Namespace
