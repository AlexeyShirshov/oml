Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Query
Imports Worm.Cache
Imports Worm.Query.Sorting

Namespace Entities

    <Serializable()> _
    Public Class Relation
        Private _host As IKeyEntity
        Private _addedList As New List(Of IKeyEntity)
        Private _deletedList As New List(Of IKeyEntity)
        'Private _subType As Type
        'Private _key As String
        Private _desc As RelationDesc
        Private _syncRoot As New Object

        Sub New(ByVal main As IKeyEntity, ByVal subType As Type)
            MyClass.New(main, New RelationDesc(New EntityUnion(subType), Nothing, Nothing))
        End Sub

        Sub New(ByVal main As IKeyEntity, ByVal subType As Type, ByVal key As String)
            MyClass.New(main, New RelationDesc(New EntityUnion(subType), Nothing, key))
        End Sub

        Sub New(ByVal main As IKeyEntity, ByVal desc As RelationDesc)
            _host = main
            _desc = desc
        End Sub

        Public Shared Function MetaEquals(ByVal r1 As Relation, ByVal r2 As Relation, ByVal schema As ObjectMappingEngine) As Boolean
            If Object.ReferenceEquals(r1, r2) Then
                Return True
            End If

            If r2.Relation IsNot Nothing AndAlso r1.Relation IsNot Nothing Then
                Dim e1 As EntityUnion = r1.Relation.Rel
                Dim e2 As EntityUnion = r2.Relation.Rel
                If Not String.IsNullOrEmpty(e1.EntityName) AndAlso Not String.IsNullOrEmpty(e2.EntityName) Then
                    Return e1.EntityName = e2.EntityName
                ElseIf e1.Type IsNot Nothing AndAlso e2.Type IsNot Nothing Then
                    Return e1.Type Is e2.Type
                Else
                    Return e1.GetRealType(schema) Is e2.GetRealType(schema)
                End If
            End If

            Return False
        End Function

        Protected Overridable ReadOnly Property _mainId() As Object
            Get
                Return _host.Identifier
            End Get
        End Property

        Protected Overridable ReadOnly Property _mainType() As Type
            Get
                Return _host.GetType
            End Get
        End Property

        Public Overridable ReadOnly Property Host() As IKeyEntity
            Get
                Return _host
            End Get
        End Property

        Public Property Relation() As RelationDesc
            Get
                Return _desc
            End Get
            Friend Set(ByVal value As RelationDesc)
                _desc = value
            End Set
        End Property

        Public ReadOnly Property Key() As String
            Get
                If _desc IsNot Nothing Then
                    Return _desc.Key
                Else
                    Return Nothing
                End If
            End Get
        End Property

        Public Property Deleted() As IList(Of IKeyEntity)
            Get
                Return _deletedList
            End Get
            Protected Set(ByVal value As IList(Of IKeyEntity))
                _deletedList = CType(value, List(Of IKeyEntity))
            End Set
        End Property

        Public Property Added() As IList(Of IKeyEntity)
            Get
                Return _addedList
            End Get
            Protected Set(ByVal value As IList(Of IKeyEntity))
                _addedList = CType(value, List(Of IKeyEntity))
            End Set
        End Property

        Public ReadOnly Property HasDeleted() As Boolean
            Get
                Return _deletedList.Count > 0
            End Get
        End Property

        Public ReadOnly Property HasAdded() As Boolean
            Get
                Return _addedList.Count > 0
            End Get
        End Property

        Public Overridable Property MainId() As Object
            Get
                Return _mainId
            End Get
            Protected Friend Set(ByVal value As Object)
                'do nothing
            End Set
        End Property

        Public ReadOnly Property MainType() As Type
            Get
                Return _mainType
            End Get
        End Property

        Public ReadOnly Property HasChanges() As Boolean
            Get
                Return HasDeleted OrElse HasAdded
            End Get
        End Property

        Public Sub AddRange(ByVal ids As IEnumerable(Of IKeyEntity))
            For Each obj As IKeyEntity In ids
                Add(obj)
            Next
        End Sub

        Protected Overridable Function PreAdd(ByVal obj As IKeyEntity) As Boolean
            Return False
        End Function

        Public Sub Merge(ByVal cmd As RelationCmd, ByVal col As IList(Of IKeyEntity), ByVal removeNotInList As Boolean)
            'Dim cmd As RelationCmd = Relation.CreateCmd(Host)
            Dim cur As IList = cmd.ToList
            If removeNotInList Then
                For Each o As IKeyEntity In cur
                    If Not col.Contains(o) Then
                        Delete(o)
                    End If
                Next
            End If
            For Each o As IKeyEntity In col
                If Not cur.Contains(o) Then
                    Add(o)
                End If
            Next
        End Sub

        Public Sub Add(ByVal obj As IKeyEntity)

            Using SyncRoot
                If _deletedList.Contains(obj) Then
                    _deletedList.Remove(obj)
                Else
                    If Not PreAdd(obj) Then
                        _addedList.Add(obj)
                    End If
                End If
            End Using
        End Sub

        Public Sub Add(ByVal obj As IKeyEntity, ByVal idx As Integer)
            Using SyncRoot
                If _deletedList.Contains(obj) Then
                    _deletedList.Remove(obj)
                Else
                    _addedList.Insert(idx, obj)
                End If
            End Using
        End Sub

        Public Sub Delete(ByVal obj As IKeyEntity)
            Using SyncRoot
                If _addedList.Contains(obj) Then
                    _addedList.Remove(obj)
                Else
                    _deletedList.Add(obj)
                End If
            End Using
        End Sub

        Public ReadOnly Property SyncRoot() As IDisposable
            Get
#If DebugLocks Then
                Return New CSScopeMgr_DebugWithStack(_syncRoot, "d:\temp\")
#Else
                Return New CSScopeMgr(_syncRoot)
#End If
            End Get
        End Property

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, Relation))
        End Function

        Public Overloads Function Equals(ByVal obj As Relation) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Return _mainType Is obj._mainType AndAlso _mainId.Equals(obj._mainId) AndAlso Object.Equals(_desc, obj._desc)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _mainType.GetHashCode Xor _mainId.GetHashCode Xor _desc.GetHashCode
        End Function

        Protected Overridable Function IsMainObjectNew(ByVal mgr As OrmManager) As Boolean
            Return _host.ObjectState = ObjectState.Created
        End Function

        Protected Overridable Function GetCopy() As Relation
            Return New Relation(_host, _desc)
        End Function
    End Class

    <Serializable()> _
    Public Class M2MRelation
        Inherits Relation
        'Private _main As IKeyEntity
        'Protected _addedList As New List(Of IKeyEntity)
        'Protected _deletedList As New List(Of IKeyEntity)
        'Protected _subType As Type
        Private _new As List(Of IKeyEntity)
        'Private _key As String
        Protected Friend _savedIds As New List(Of IKeyEntity)
        'Private _syncRoot As New Object

        Public Sub New(ByVal main As IKeyEntity, ByVal subType As Type)
            MyClass.New(main, New M2MRelationDesc(subType))
        End Sub

        Public Sub New(ByVal main As IKeyEntity, ByVal subType As Type, ByVal direct As Boolean)
            'MyClass.New(main, subType, Meta.M2MRelationDesc.GetKey(direct))
            MyClass.New(main, New M2MRelationDesc(subType, Meta.M2MRelationDesc.GetKey(direct)))
        End Sub

        Public Sub New(ByVal main As IKeyEntity, ByVal subType As Type, ByVal key As String)
            'MyBase.New(main, subType, key)
            MyClass.New(main, New M2MRelationDesc(subType, key))
        End Sub

        Sub New(ByVal main As IKeyEntity, ByVal desc As M2MRelationDesc)
            MyBase.New(main, desc)
        End Sub

        Public ReadOnly Property HasNew() As Boolean
            Get
                Return _new IsNot Nothing AndAlso _new.Count > 0
            End Get
        End Property

        Public ReadOnly Property SavedObjsCount() As Integer
            Get
                Return _savedIds.Count
            End Get
        End Property

        Public Shadows ReadOnly Property Relation() As M2MRelationDesc
            Get
                Return CType(MyBase.Relation, M2MRelationDesc)
            End Get
        End Property

        Public ReadOnly Property Direct() As Boolean
            Get
                Return String.IsNullOrEmpty(Key) OrElse Not Key.StartsWith(M2MRelationDesc.ReversePrefix)
            End Get
        End Property

        Protected Friend Sub Reject2()
            Using SyncRoot
                _savedIds.Clear()
            End Using
        End Sub

        Public Sub Reject(ByVal rejectDual As Boolean)
            Reject(Nothing, rejectDual)
        End Sub

        Public Sub Reject(ByVal mgr As OrmManager, ByVal rejectDual As Boolean)
            Using SyncRoot
                If rejectDual Then
                    For Each obj As _IKeyEntity In Added
                        RejectRelated(mgr, obj, True)
                    Next
                End If
                Added.Clear()
                If rejectDual Then
                    For Each obj As _IKeyEntity In Deleted
                        RejectRelated(mgr, obj, False)
                    Next
                End If
                Deleted.Clear()
                RemoveNew()
                Reject2()
            End Using
        End Sub

        'Private ReadOnly Property _non_direct() As Boolean
        '    Get
        '        Return _key = M2MRelation.RevKey
        '    End Get
        'End Property

        Protected Sub RejectRelated(ByVal mgr As OrmManager, ByVal obj As _IKeyEntity, ByVal add As Boolean)
            'Dim m As M2MCache = mgr.FindM2MNonGeneric(mgr.CreateDBObject(id, SubType), MainType, GetRealDirect).First
            Dim el As M2MRelation = GetRevert(mgr, obj)

            Dim l As IList(Of IKeyEntity) = el.Added
            If Not add Then
                l = el.Deleted
            End If
            Dim idx As Integer = FindMainIdx(l)
            If idx >= 0 Then
                l.RemoveAt(idx)
            End If
        End Sub

        Protected Function FindMainIdx(ByVal l As IList(Of IKeyEntity)) As Integer
            Return FindIdIdx(l, _mainId)
        End Function

        Protected Shared Function FindIdIdx(ByVal l As IList(Of IKeyEntity), ByVal id As Object) As Integer
            For i As Integer = 0 To l.Count - 1
                If l(i).Identifier.Equals(id) Then
                    Return i
                End If
            Next
            Return -1
        End Function

        Protected Overridable Function GetRevert(ByVal mgr As OrmManager, ByVal obj As _IKeyEntity) As M2MRelation
            Return CType(obj.GetRelation(Relation), M2MRelation)
        End Function

        Protected Overridable Function GetRevert(ByVal mgr As OrmManager) As IList(Of Relation)
            Return Host.GetAllRelation
        End Function

        'Protected Overridable Function GetRevert(ByVal mgr As OrmManager) As List(Of EditableListBase)
        '    Throw New NotSupportedException
        '    'Dim l As New List(Of EditableListBase)
        '    'For Each o As IOrmBase In Main.Find(SubType).ToList(mgr)
        '    '    Dim el As EditableListBase = mgr.Cache.GetM2M(o, MainType, _key)
        '    '    If el IsNot Nothing Then
        '    '        l.Add(el)
        '    '    End If
        '    'Next
        '    'Return l
        'End Function

        Protected Overridable Sub AcceptDual(ByVal mgr As OrmManager)
            For Each el As M2MRelation In GetRevert(mgr)
                If el.Added.Contains(Host) OrElse el.Deleted.Contains(Host) Then
                    el.Accept(mgr, Host)
                End If
            Next
        End Sub

        Protected Friend Sub Update(ByVal obj As IKeyEntity, ByVal oldId As Object)
            'Dim idx As Integer = FindIdIdx(_addedList, oldId)
            'If idx < 0 Then
            '    Throw New ArgumentException("Old id is not found: " & oldId.ToString)
            'End If

            '_addedList.RemoveAt(idx)
            '_addedList.Add(obj)

            If HasNew Then
                _new.Remove(obj)
                'idx = FindIdIdx(_new, oldId)
                'If idx >= 0 Then
                '    _new.RemoveAt(idx)
                'End If
            End If
        End Sub

        Protected Sub RemoveNew()
            If _new IsNot Nothing Then
                _new.Clear()
            End If
        End Sub

        Public Overloads Function Accept() As Boolean
            Return Accept(Nothing)
        End Function

        Public Overridable Overloads Function Accept(ByVal mgr As OrmManager) As Boolean
            Using SyncRoot
                Dim needaccept As Boolean = Added.Count > 0 OrElse Deleted.Count > 0
                Added.Clear()
                Deleted.Clear()
                _savedIds.Clear()
                RemoveNew()

                If needaccept Then
                    AcceptDual(mgr)
                End If
            End Using
            Return True
        End Function

        Public Overridable Function _AcceptAdd(ByVal obj As IKeyEntity, ByVal mgr As OrmManager) As Boolean
            Return True
        End Function

        Public Overridable Sub _AcceptDelete(ByVal obj As IKeyEntity)

        End Sub

        Public Overridable Overloads Function Accept(ByVal mgr As OrmManager, ByVal obj As IKeyEntity) As Boolean
            Using SyncRoot
                Dim idx As Integer = FindIdIdx(Added, obj.Identifier)
                If idx >= 0 Then
                    If Not _AcceptAdd(obj, mgr) Then
                        Return False
                    End If
                    'If _sort Is Nothing Then
                    '    CType(_mainList, List(Of Integer)).Add(id)
                    '    _addedList.Remove(id)
                    'Else
                    '    Dim sr As IOrmSorting = Nothing
                    '    Dim col As New ArrayList(mgr.ConvertIds2Objects(_subType, _mainList, False))
                    '    If Not mgr.CanSortOnClient(_subType, col, _sort, sr) Then
                    '        Return False
                    '    End If
                    '    Dim c As IComparer = Nothing
                    '    If sr Is Nothing Then
                    '        c = New OrmComparer(Of OrmBase)(_subType, _sort)
                    '    Else
                    '        c = sr.CreateSortComparer(_sort)
                    '    End If
                    '    If c Is Nothing Then
                    '        Return False
                    '    End If
                    '    Dim ad As OrmBase = mgr.CreateDBObject(id, _subType)
                    '    Dim pos As Integer = col.BinarySearch(ad, c)
                    '    If pos < 0 Then
                    '        _mainList.Insert(Not pos, id)
                    '    End If
                    'End If
                    Added.RemoveAt(idx)
                Else
                    idx = FindIdIdx(Deleted, obj.Identifier)
                    If idx >= 0 Then
                        'CType(_mainList, List(Of Integer)).Remove(id)
                        _AcceptDelete(obj)
                        Deleted.RemoveAt(idx)
                    End If
                End If
            End Using

            Return True
        End Function

        Friend Function PrepareSave(ByVal mgr As OrmManager) As M2MRelation
            Dim newl As M2MRelation = Nothing
            'Dim mo As _ICachedEntity = mgr.GetOrmBaseFromCacheOrCreate(_mainId, _mainType, False)
            'If Not mgr.Cache.IsNewObject(_mainType, mo.GetPKValues) Then
            If Not IsMainObjectNew(mgr) Then
                Dim ad As New List(Of IKeyEntity)
                For Each ao As _IKeyEntity In Added
                    'Dim ao As _ICachedEntity = mgr.GetOrmBaseFromCacheOrCreate(id, _subType, False)
                    'If mgr.Cache.IsNewObject(SubType, ao.GetPKValues) Then
                    If ao.ObjectState = ObjectState.Created Then
                        If _new Is Nothing Then
                            _new = New List(Of IKeyEntity)
                        End If
                        Dim newIdx As Integer = FindIdIdx(_new, ao.Identifier)
                        If newIdx < 0 Then
                            _new.Add(ao)
                        End If
                    Else
                        If FindIdIdx(_savedIds, ao.Identifier) < 0 AndAlso CheckDual(mgr, ao) Then
                            ad.Add(ao)
                        End If
                    End If
                Next
                If ad.Count > 0 OrElse Deleted.Count > 0 Then
                    newl = CType(GetCopy(), M2MRelation)
                    newl.Deleted = Deleted
                    newl.Added = ad
                End If
            End If
            Return newl
        End Function

        Protected Overridable Function CheckDual(ByVal mgr As OrmManager, ByVal obj As _IKeyEntity) As Boolean
            Return FindIdIdx(CType(obj.GetRelation(_mainType, Key), M2MRelation)._savedIds, _mainId) < 0
        End Function

        Protected Overrides Function GetCopy() As Relation
            Return New M2MRelation(Host, Relation)
        End Function
    End Class

    <Serializable()> _
    Public Class CachedM2MRelation
        Inherits M2MRelation

        Protected _cantgetCurrent As Boolean
        Private _mainId_ As Object
        Private _mainType_ As Type
        Private _mainList As IList(Of Object)
        'Private _addedList As New List(Of Integer)
        'Private _deletedList As New List(Of Integer)
        Private _saved As Boolean
        Private _sort As Sort

        Sub New(ByVal mainId As Object, ByVal mainList As IList(Of Object), ByVal mainType As Type, ByVal subType As Type, ByVal sort As Sort)
            MyBase.New(Nothing, subType)
            _mainList = mainList
            _sort = sort
            _mainId_ = mainId
            _mainType_ = mainType
        End Sub

        Sub New(ByVal mainId As Object, ByVal mainList As IList(Of Object), ByVal mainType As Type, ByVal subType As Type, ByVal direct As Boolean, ByVal sort As Sort)
            MyBase.New(Nothing, subType, direct)
            _mainList = mainList
            _sort = sort
            _mainId_ = mainId
            _mainType_ = mainType
        End Sub

        Sub New(ByVal mainId As Object, ByVal mainList As IList(Of Object), ByVal mainType As Type, ByVal subType As Type, ByVal key As String, ByVal sort As Sort)
            MyBase.New(Nothing, subType, key)
            _mainList = mainList
            _sort = sort
            _mainId_ = mainId
            _mainType_ = mainType
        End Sub

#Region " Public properties "
        Protected Function GetRealDirect() As String
            If SubType Is MainType Then
                Return Meta.M2MRelationDesc.GetRevKey(Key)
            Else
                Return Key
            End If
        End Function

        Public ReadOnly Property SubType() As Type
            Get
                Dim mpe As ObjectMappingEngine = OrmManager.CurrentManager.MappingEngine
                Return Relation.Rel.GetRealType(mpe)
            End Get
        End Property

        Public ReadOnly Property CurrentCount() As Integer
            Get
                Using SyncRoot
                    Return _mainList.Count + Added.Count - Deleted.Count
                End Using
            End Get
        End Property

        Public ReadOnly Property Current() As IList(Of Object)
            Get
                If _cantgetCurrent Then
                    Throw New InvalidOperationException("Cannot prepare current data view. Use Original and Added or save changes.")
                End If

                Using SyncRoot
                    Dim arr As New List(Of Object)
                    If _mainList.Count <> 0 OrElse Added.Count <> 0 Then
                        If Added.Count <> 0 AndAlso _mainList.Count <> 0 Then
                            Dim mgr As OrmManager = OrmManager.CurrentManager
                            Dim col As New ArrayList
                            Dim c As IComparer = Nothing
                            If _sort Is Nothing Then
                                arr.AddRange(_mainList)
                                arr.AddRange(CType(Added, List(Of IKeyEntity)).ConvertAll(Function(o As IKeyEntity) o.Identifier))
                            Else
                                Dim sr As IOrmSorting = Nothing
                                col.AddRange(mgr.ConvertIds2Objects(SubType, _mainList, False))
                                If OrmManager.CanSortOnClient(SubType, col, _sort, sr) Then
                                    If sr Is Nothing Then
                                        c = New OrmComparer(Of KeyEntity)(SubType, _sort)
                                    Else
                                        c = sr.CreateSortComparer(_sort)
                                    End If
                                    If c Is Nothing Then
                                        Throw New InvalidOperationException("Cannot prepare current data view. Use Original and Added or save changes.")
                                    End If
                                Else
                                    Throw New InvalidOperationException("Cannot prepare current data view. Use Original and Added or save changes.")
                                End If

                                Dim i, j As Integer
                                Do
                                    If i = _mainList.Count Then
                                        For k As Integer = j To Added.Count - 1
                                            arr.Add(Added(k).Identifier)
                                        Next
                                        Exit Do
                                    End If
                                    If j = Added.Count Then
                                        For k As Integer = i To _mainList.Count - 1
                                            arr.Add(_mainList(k))
                                        Next
                                        Exit Do
                                    End If
                                    Dim ex As IKeyEntity = CType(col(i), IKeyEntity)
                                    Dim ad As IKeyEntity = Added(j) 'mgr.GetOrmBaseFromCacheOrCreate(Added(j), subtype, False)
                                    If c.Compare(ex, ad) < 0 Then
                                        arr.Add(ex.Identifier)
                                        i += 1
                                    Else
                                        arr.Add(ad.Identifier)
                                        j += 1
                                    End If
                                Loop While True
                            End If
                        Else
                            arr.AddRange(_mainList)
                            arr.AddRange(CType(Added, List(Of IKeyEntity)).ConvertAll(Function(o As IKeyEntity) o.Identifier))
                        End If

                        For Each obj As IKeyEntity In Deleted
                            arr.Remove(obj.Identifier)
                        Next
                    End If
                    Return arr
                End Using
            End Get
        End Property

        Public ReadOnly Property Original() As IList(Of Object)
            Get
                Return _mainList
            End Get
        End Property

#End Region

#Region " Public functions "

        Public Overrides Function Accept(ByVal mgr As OrmManager) As Boolean
            _cantgetCurrent = False
            Using SyncRoot
                Dim needaccept As Boolean = Added.Count > 0
                If _sort Is Nothing Then
                    CType(_mainList, List(Of Object)).AddRange(CType(Added, List(Of IKeyEntity)).ConvertAll(Function(o As IKeyEntity) o.Identifier))
                    Added.Clear()
                Else
                    If Added.Count > 0 Then
                        needaccept = True
                        Dim sr As IOrmSorting = Nothing
                        Dim c As IComparer = Nothing
                        Dim col As ArrayList = Nothing

                        If _mainList.Count > 0 Then
                            col = New ArrayList(mgr.ConvertIds2Objects(SubType, _mainList, False))
                            If Not OrmManager.CanSortOnClient(SubType, col, _sort, sr) Then
                                AcceptDual(mgr)
                                Return False
                            End If
                            If sr Is Nothing Then
                                c = New OrmComparer(Of KeyEntity)(SubType, _sort)
                            Else
                                c = sr.CreateSortComparer(_sort)
                            End If
                            If c Is Nothing Then
                                AcceptDual(mgr)
                                Return False
                            End If
                        End If

                        Dim ml As New List(Of Object)
                        Dim i, j As Integer
                        Do
                            If i = _mainList.Count Then
                                For k As Integer = j To Added.Count - 1
                                    ml.Add(Added(k).Identifier)
                                Next
                                Exit Do
                            End If
                            If j = Added.Count Then
                                For k As Integer = i To _mainList.Count - 1
                                    ml.Add(_mainList(k))
                                Next
                                Exit Do
                            End If
                            Dim ex As IKeyEntity = CType(col(i), IKeyEntity)
                            Dim ad As IKeyEntity = Added(j) 'mgr.GetOrmBaseFromCacheOrCreate(Added(j), subtype, False)
                            If c.Compare(ex, ad) < 0 Then
                                ml.Add(ex.Identifier)
                                i += 1
                            Else
                                ml.Add(ad.Identifier)
                                j += 1
                            End If
                        Loop While True

                        _mainList = ml
                    End If

                    Added.Clear()
                End If

                For Each obj As IKeyEntity In Deleted
                    CType(_mainList, List(Of Object)).Remove(obj.Identifier)
                Next
                needaccept = needaccept OrElse Deleted.Count > 0
                Deleted.Clear()
                _saved = False
                RemoveNew()

                If needaccept Then
                    AcceptDual(mgr)
                End If
            End Using
            Return True
        End Function

#End Region

#Region " Protected functions "

        Protected Overrides Function IsMainObjectNew(ByVal mgr As OrmManager) As Boolean
            Return mgr.Cache.IsNewObject(_mainType_, New PKDesc() {New PKDesc(mgr.MappingEngine.GetPrimaryKeys(_mainType_)(0).PropertyAlias, _mainId_)})
        End Function

        Public Overrides Sub _AcceptDelete(ByVal obj As IKeyEntity)
            CType(_mainList, List(Of Object)).Remove(obj.Identifier)
        End Sub

        Public Overrides Function _AcceptAdd(ByVal obj As IKeyEntity, ByVal mgr As OrmManager) As Boolean
            If _sort Is Nothing Then
                CType(_mainList, List(Of Object)).Add(obj.Identifier)
                Added.Remove(obj)
            Else
                Dim sr As IOrmSorting = Nothing
                Dim col As New ArrayList(mgr.ConvertIds2Objects(SubType, _mainList, False))
                If Not OrmManager.CanSortOnClient(SubType, col, _sort, sr) Then
                    Return False
                End If
                Dim c As IComparer = Nothing
                If sr Is Nothing Then
                    c = New OrmComparer(Of KeyEntity)(SubType, _sort)
                Else
                    c = sr.CreateSortComparer(_sort)
                End If
                If c Is Nothing Then
                    Return False
                End If
                Dim ad As IKeyEntity = obj 'mgr.GetOrmBaseFromCacheOrCreate(id, subtype, False)
                Dim pos As Integer = col.BinarySearch(ad, c)
                If pos < 0 Then
                    _mainList.Insert(Not pos, obj.Identifier)
                End If
            End If
            Return True
        End Function

        Protected Overrides Function PreAdd(ByVal obj As IKeyEntity) As Boolean
            If _sort IsNot Nothing Then
                Dim sr As IOrmSorting = Nothing
                'Dim mgr As OrmManager = OrmManager.CurrentManager
                If OrmManager.CanSortOnClient(SubType, CType(Added, List(Of IKeyEntity)), _sort, sr) Then
                    Dim c As IComparer = Nothing
                    If sr Is Nothing Then
                        c = New OrmComparer(Of KeyEntity)(SubType, _sort)
                    Else
                        c = sr.CreateSortComparer(_sort)
                    End If
                    If c IsNot Nothing Then
                        Dim o As IKeyEntity = obj 'mgr.GetOrmBaseFromCacheOrCreate(id, subtype, False)
                        Dim col As New ArrayList(CType(Added, List(Of IKeyEntity)))
                        Dim pos As Integer = col.BinarySearch(o, c)
                        If pos < 0 Then
                            Added.Insert(Not pos, obj)
                            Return True
                        End If
                    Else
                        _cantgetCurrent = True
                    End If
                Else
                    _cantgetCurrent = True
                End If
            End If
            Return False
        End Function

        Protected Overrides Function GetRevert(ByVal mgr As OrmManager) As IList(Of Relation)
            Dim l As New List(Of Relation)
            For Each id As Object In _mainList
                Dim m As M2MCache = mgr.GetM2MNonGeneric(id, SubType, _mainType, GetRealDirect)
                If m IsNot Nothing Then
                    l.Add(m.Entry)
                End If
            Next
            Return l
        End Function

        Protected Overrides Function GetRevert(ByVal mgr As OrmManager, ByVal obj As _IKeyEntity) As M2MRelation
            Dim m As M2MCache = mgr.GetM2MNonGeneric(obj, _mainType, GetRealDirect)
            If m IsNot Nothing Then
                Return m.Entry
            End If
            Return Nothing
        End Function

        'Protected Sub AcceptDual()
        '    Dim mgr As OrmManager = OrmManager.CurrentManager
        '    For Each id As Integer In _mainList
        '        Dim m As M2MCache = mgr.GetM2MNonGeneric(id.ToString, subtype, _mainType, GetRealDirect)
        '        If m IsNot Nothing Then
        '            If m.Entry.Added.Contains(_mainId) OrElse m.Entry.Deleted.Contains(_mainId) Then
        '                If Not m.Entry.Accept(mgr, _mainId) Then
        '                    Dim obj As OrmBase = mgr.CreateDBObject(id, SubType)
        '                    mgr.M2MCancel(obj, MainType)
        '                End If
        '            End If
        '        End If
        '    Next
        'End Sub

        Protected Overrides Function CheckDual(ByVal mgr As OrmManager, ByVal obj As _IKeyEntity) As Boolean
            Dim m As M2MCache = mgr.FindM2MNonGeneric(obj, MainType, Key).First
            Dim c As Boolean = True
            For Each i As Object In m.Entry.Original
                If i.Equals(_mainId) Then
                    c = False
                    Exit For
                End If
            Next
            If c AndAlso m.Entry.Saved Then
                For Each i As Object In m.Entry.Current
                    If i.Equals(_mainId) Then
                        c = False
                        Exit For
                    End If
                Next
            End If
            Return c
        End Function

        Protected Overrides Function GetCopy() As Relation
            Return New CachedM2MRelation(_mainId, _mainList, _mainType, SubType, Key, _sort)
        End Function

        Protected Overrides Sub AcceptDual(ByVal mgr As OrmManager)
            For Each el As M2MRelation In GetRevert(mgr)
                Dim main As _IKeyEntity = CType(mgr.CreateKeyEntity(_mainId_, _mainType_), _IKeyEntity)
                If el.Added.Contains(main) OrElse el.Deleted.Contains(main) Then
                    If Not el.Accept(mgr, main) Then
                        Dim smain As _IKeyEntity = CType(mgr.CreateKeyEntity(el.MainId, el.MainType), _IKeyEntity)
                        mgr.M2MCancel(smain, MainType)
                    End If
                End If
            Next
        End Sub

        Public Overrides Function Accept(ByVal mgr As OrmManager, ByVal obj As Entities.IKeyEntity) As Boolean
            Dim b As Boolean = MyBase.Accept(mgr, obj)
            If Added.Count = 0 Then
                _cantgetCurrent = False
            End If

            Return b
        End Function
#End Region

#Region " Protected properties "
        Protected Overrides ReadOnly Property _mainId() As Object
            Get
                Return _mainId_
            End Get
        End Property

        Protected Overrides ReadOnly Property _mainType() As System.Type
            Get
                Return _mainType_
            End Get
        End Property

        Protected Friend Property Saved() As Boolean
            Get
                Return _saved
            End Get
            Set(ByVal value As Boolean)
                _saved = value
            End Set
        End Property

        Public Overrides Property MainId() As Object
            Get
                Return MyBase.MainId
            End Get
            Protected Friend Set(ByVal value As Object)
                _mainId_ = value
            End Set
        End Property
#End Region

    End Class

End Namespace
