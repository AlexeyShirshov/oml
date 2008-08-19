Imports System.Collections.Generic
Imports Worm.Sorting
Imports Worm.Orm.Meta
Imports Worm.Orm

Namespace Cache
    Public Class EditableListBase
        Protected _mainId As Object
        Protected _addedList As New List(Of Object)
        Protected _deletedList As New List(Of Object)
        Protected _mainType As Type
        Protected _subType As Type
        Private _new As List(Of Object)
        Private _key As String
        Protected _cantgetCurrent As Boolean
        Protected Friend _savedIds As New List(Of Object)
        Private _syncRoot As New Object

        Sub New(ByVal mainId As Object, ByVal mainType As Type, ByVal subType As Type)
            _mainId = mainId
            _mainType = mainType
            _subType = subType
        End Sub

        Sub New(ByVal mainId As Object, ByVal mainType As Type, ByVal subType As Type, ByVal direct As Boolean)
            MyClass.New(mainId, mainType, subType, M2MRelation.GetKey(direct))
        End Sub

        Sub New(ByVal mainId As Object, ByVal mainType As Type, ByVal subType As Type, ByVal key As String)
            MyClass.New(mainId, mainType, subType)
            _key = key
        End Sub

        Public ReadOnly Property SavedObjsCount() As Integer
            Get
                Return _savedIds.Count
            End Get
        End Property

        Public ReadOnly Property Key() As String
            Get
                Return _key
            End Get
        End Property

        Public ReadOnly Property Deleted() As IList(Of Object)
            Get
                Return _deletedList
            End Get
        End Property

        Public ReadOnly Property Added() As IList(Of Object)
            Get
                Return _addedList
            End Get
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

        Public Property MainId() As Object
            Get
                Return _mainId
            End Get
            Protected Friend Set(ByVal value As Object)
                _mainId = value
            End Set
        End Property

        Public ReadOnly Property Main() As IOrmBase
            Get
                Return OrmManagerBase.CurrentManager.GetOrmBaseFromCacheOrCreate(_mainId, _mainType)
            End Get
        End Property

        Public ReadOnly Property MainType() As Type
            Get
                Return _mainType
            End Get
        End Property

        Public ReadOnly Property SubType() As Type
            Get
                Return _subType
            End Get
        End Property

        Public ReadOnly Property HasNew() As Boolean
            Get
                Return _new IsNot Nothing AndAlso _new.Count > 0
            End Get
        End Property

        Public ReadOnly Property HasChanges() As Boolean
            Get
                Return HasDeleted OrElse HasAdded
            End Get
        End Property

        Public ReadOnly Property Direct() As Boolean
            Get
                Return _key <> M2MRelation.RevKey
            End Get
        End Property

        Protected Friend Sub Reject2()
            Using SyncRoot
                _savedIds.clear()
            End Using
        End Sub

        Public Sub Reject(ByVal rejectDual As Boolean)
            Using SyncRoot
                If rejectDual Then
                    For Each id As Integer In _addedList
                        RejectRelated(id, True)
                    Next
                End If
                _addedList.Clear()
                If rejectDual Then
                    For Each id As Integer In _deletedList
                        RejectRelated(id, False)
                    Next
                End If
                _deletedList.Clear()
                RemoveNew()
                Reject2()
            End Using
        End Sub

        Public Sub AddRange(ByVal ids As IEnumerable(Of Object))
            For Each id As Integer In ids
                Add(id)
            Next
        End Sub

        Public Sub Add(ByVal id As Object)
            Using SyncRoot
                If _deletedList.Contains(id) Then
                    _deletedList.Remove(id)
                Else
                    If Not PreAdd(id) Then
                        _addedList.Add(id)
                    End If
                End If
            End Using
        End Sub

        Public Sub Add(ByVal id As Object, ByVal idx As Integer)
            Using SyncRoot
                If _deletedList.Contains(id) Then
                    _deletedList.Remove(id)
                Else
                    _addedList.Insert(idx, id)
                End If
            End Using
        End Sub

        Public Sub Delete(ByVal id As Object)
            Using SyncRoot
                If _addedList.Contains(id) Then
                    _addedList.Remove(id)
                Else
                    _deletedList.Add(id)
                End If
            End Using
        End Sub

        Protected Function GetRealDirect() As String
            If SubType Is MainType Then
                Return M2MRelation.GetRevKey(_key)
            Else
                Return _key
            End If
        End Function

        'Private ReadOnly Property _non_direct() As Boolean
        '    Get
        '        Return _key = M2MRelation.RevKey
        '    End Get
        'End Property

        Protected Sub RejectRelated(ByVal id As Object, ByVal add As Boolean)
            Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
            'Dim m As OrmManagerBase.M2MCache = mgr.FindM2MNonGeneric(mgr.CreateDBObject(id, SubType), MainType, GetRealDirect).First
            Dim el As EditableListBase = GetRevert(mgr, id)

            Dim l As IList(Of Object) = el.Added
            If Not add Then
                l = el.Deleted
            End If
            If l.Contains(_mainId) Then
                l.Remove(_mainId)
            End If
        End Sub

        Protected Overridable Function GetRevert(ByVal mgr As OrmManagerBase, ByVal id As Object) As EditableListBase
            Return CType(mgr.GetOrmBaseFromCacheOrCreate(id, SubType), _IOrmBase).GetM2M(MainType, Key)
        End Function

        Protected Overridable Function GetRevert(ByVal mgr As OrmManagerBase) As List(Of EditableListBase)
            Throw New NotSupportedException
            'Dim l As New List(Of EditableListBase)
            'For Each o As IOrmBase In Main.Find(SubType).ToList(mgr)
            '    Dim el As EditableListBase = mgr.Cache.GetM2M(o, MainType, _key)
            '    If el IsNot Nothing Then
            '        l.Add(el)
            '    End If
            'Next
            'Return l
        End Function

        Protected Sub AcceptDual()
            Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
            For Each el As EditableListBase In GetRevert(mgr)
                'Dim m As OrmManagerBase.M2MCache = mgr.GetM2MNonGeneric(id.ToString, _subType, _mainType, GetRealDirect)
                'If m IsNot Nothing Then
                If el.Added.Contains(_mainId) OrElse el.Deleted.Contains(_mainId) Then
                    If Not el.Accept(mgr, _mainId) Then
                        'Dim obj As OrmBase = mgr.CreateDBObject(id, SubType)
                        mgr.M2MCancel(CType(el.Main, _IOrmBase), MainType)
                    End If
                End If
                'End If
            Next
        End Sub

        Protected Friend Sub Update(ByVal id As Object, ByVal oldId As Object)
            Dim idx As Integer = _addedList.IndexOf(oldId)
            If idx < 0 Then
                Throw New ArgumentException("Old id is not found: " & oldId.ToString)
            End If

            _addedList.RemoveAt(idx)
            _addedList.Add(id)

            If HasNew Then
                If _new.Remove(oldId) Then
                    '_new.Add(id)
                End If
            End If
        End Sub

        Protected Sub RemoveNew()
            If _new IsNot Nothing Then
                _new.Clear()
            End If
        End Sub

        Protected Overridable Function PreAdd(ByVal id As Object) As Boolean
            Return False
        End Function

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
            Return Equals(TryCast(obj, EditableListBase))
        End Function

        Public Overloads Function Equals(ByVal obj As EditableListBase) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Return _mainType Is obj._mainType AndAlso _subType Is obj._subType AndAlso _mainId.Equals(obj._mainId) AndAlso _key = obj._key
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _mainType.GetHashCode Xor _subType.GetHashCode Xor _mainId.GetHashCode Xor _key.GetHashCode
        End Function

        Public Overridable Overloads Function Accept(ByVal mgr As OrmManagerBase) As Boolean
            Using SyncRoot
                Dim needaccept As Boolean = _addedList.Count > 0 OrElse _deletedList.Count > 0
                _addedList.Clear()
                _deletedList.Clear()
                _savedIds.Clear()
                RemoveNew()

                If needaccept Then
                    AcceptDual()
                End If
            End Using
            Return True
        End Function

        Public Overridable Function _AcceptAdd(ByVal id As Object, ByVal mgr As OrmManagerBase) As Boolean
            Return True
        End Function

        Public Overridable Sub _AcceptDelete(ByVal id As Object)

        End Sub

        Public Overridable Overloads Function Accept(ByVal mgr As OrmManagerBase, ByVal id As Object) As Boolean
            Using SyncRoot
                If _addedList.Contains(id) Then
                    If Not _AcceptAdd(id, mgr) Then
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
                    _addedList.Remove(id)
                    If _addedList.Count = 0 Then
                        _cantgetCurrent = False
                    End If
                ElseIf _deletedList.Contains(id) Then
                    'CType(_mainList, List(Of Integer)).Remove(id)
                    _AcceptDelete(id)
                    _deletedList.Remove(id)
                End If
            End Using

            Return True
        End Function

        Friend Function PrepareSave(ByVal mgr As OrmManagerBase) As EditableListBase
            Dim newl As EditableListBase = Nothing
            If Not mgr.IsNewObject(_mainType, _mainId) Then
                Dim ad As New List(Of Object)
                For Each id As Object In _addedList
                    If mgr.IsNewObject(SubType, id) Then
                        If _new Is Nothing Then
                            _new = New List(Of Object)
                        End If
                        If Not _new.Contains(id) Then
                            _new.Add(id)
                        End If
                    ElseIf Not _savedIds.Contains(id) AndAlso CheckDual(mgr, id) Then
                        ad.Add(id)
                    End If
                Next
                If ad.Count > 0 OrElse _deletedList.Count > 0 Then
                    newl = GetCopy()
                    newl._deletedList = _deletedList
                    newl._addedList = ad
                End If
            End If
            Return newl
        End Function

        Protected Overridable Function CheckDual(ByVal mgr As OrmManagerBase, ByVal id As Object) As Boolean
            Return True
        End Function

        Protected Overridable Function GetCopy() As EditableListBase
            Return New EditableListBase(_mainId, _mainType, _subType, _key)
        End Function
    End Class

    Public Class EditableList
        Inherits EditableListBase

        'Private _mainId As Integer
        Private _mainList As IList(Of Object)
        'Private _addedList As New List(Of Integer)
        'Private _deletedList As New List(Of Integer)
        Private _saved As Boolean
        Private _sort As Sort

        Sub New(ByVal mainId As Object, ByVal mainList As IList(Of Object), ByVal mainType As Type, ByVal subType As Type, ByVal sort As Sort)
            MyBase.New(mainId, mainType, subType)
            _mainList = mainList
            _sort = sort
        End Sub

        Sub New(ByVal mainId As Object, ByVal mainList As IList(Of Object), ByVal mainType As Type, ByVal subType As Type, ByVal direct As Boolean, ByVal sort As Sort)
            MyBase.New(mainId, mainType, subType, direct)
            _mainList = mainList
            _sort = sort
        End Sub

        Sub New(ByVal mainId As Object, ByVal mainList As IList(Of Object), ByVal mainType As Type, ByVal subType As Type, ByVal key As String, ByVal sort As Sort)
            MyBase.New(mainId, mainType, subType, key)
            _mainList = mainList
            _sort = sort
        End Sub

#Region " Public properties "

        Public ReadOnly Property CurrentCount() As Integer
            Get
                Using SyncRoot
                    Return _mainList.Count + _addedList.Count - _deletedList.Count
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
                    If _mainList.Count <> 0 OrElse _addedList.Count <> 0 Then
                        If _addedList.Count <> 0 AndAlso _mainList.Count <> 0 Then
                            Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
                            Dim col As New ArrayList
                            Dim c As IComparer = Nothing
                            If _sort Is Nothing Then
                                arr.AddRange(_mainList)
                                arr.AddRange(_addedList)
                            Else
                                Dim sr As IOrmSorting = Nothing
                                col.AddRange(mgr.ConvertIds2Objects(_subType, _mainList, False))
                                If mgr.CanSortOnClient(_subType, col, _sort, sr) Then
                                    If sr Is Nothing Then
                                        c = New OrmComparer(Of OrmBase)(_subType, _sort)
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
                                        For k As Integer = j To _addedList.Count - 1
                                            arr.Add(_addedList(k))
                                        Next
                                        Exit Do
                                    End If
                                    If j = _addedList.Count Then
                                        For k As Integer = i To _mainList.Count - 1
                                            arr.Add(_mainList(k))
                                        Next
                                        Exit Do
                                    End If
                                    Dim ex As IOrmBase = CType(col(i), OrmBase)
                                    Dim ad As IOrmBase = mgr.GetOrmBaseFromCacheOrCreate(_addedList(j), _subType)
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
                            arr.AddRange(_addedList)
                        End If

                        For Each o As Integer In _deletedList
                            arr.Remove(o)
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

        Public Overrides Function Accept(ByVal mgr As OrmManagerBase) As Boolean
            _cantgetCurrent = False
            Using SyncRoot
                Dim needaccept As Boolean = _addedList.Count > 0
                If _sort Is Nothing Then
                    CType(_mainList, List(Of Object)).AddRange(_addedList)
                    _addedList.Clear()
                Else
                    If _addedList.Count > 0 Then
                        needaccept = True
                        Dim sr As IOrmSorting = Nothing
                        Dim c As IComparer = Nothing
                        Dim col As ArrayList = Nothing

                        If _mainList.Count > 0 Then
                            col = New ArrayList(mgr.ConvertIds2Objects(_subType, _mainList, False))
                            If Not mgr.CanSortOnClient(_subType, col, _sort, sr) Then
                                AcceptDual()
                                Return False
                            End If
                            If sr Is Nothing Then
                                c = New OrmComparer(Of OrmBase)(_subType, _sort)
                            Else
                                c = sr.CreateSortComparer(_sort)
                            End If
                            If c Is Nothing Then
                                AcceptDual()
                                Return False
                            End If
                        End If

                        Dim ml As New List(Of Object)
                        Dim i, j As Integer
                        Do
                            If i = _mainList.Count Then
                                For k As Integer = j To _addedList.Count - 1
                                    ml.Add(_addedList(k))
                                Next
                                Exit Do
                            End If
                            If j = _addedList.Count Then
                                For k As Integer = i To _mainList.Count - 1
                                    ml.Add(_mainList(k))
                                Next
                                Exit Do
                            End If
                            Dim ex As IOrmBase = CType(col(i), IOrmBase)
                            Dim ad As IOrmBase = mgr.GetOrmBaseFromCacheOrCreate(_addedList(j), _subType)
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

                    _addedList.Clear()
                End If

                For Each o As Integer In _deletedList
                    CType(_mainList, List(Of Object)).Remove(o)
                Next
                needaccept = needaccept OrElse _deletedList.Count > 0
                _deletedList.Clear()
                _saved = False
                RemoveNew()

                If needaccept Then
                    AcceptDual()
                End If
            End Using
            Return True
        End Function

#End Region

#Region " Protected functions "
        Public Overrides Sub _AcceptDelete(ByVal id As Object)
            CType(_mainList, List(Of Object)).Remove(id)
        End Sub

        Public Overrides Function _AcceptAdd(ByVal id As Object, ByVal mgr As OrmManagerBase) As Boolean
            If _sort Is Nothing Then
                CType(_mainList, List(Of Object)).Add(id)
                _addedList.Remove(id)
            Else
                Dim sr As IOrmSorting = Nothing
                Dim col As New ArrayList(mgr.ConvertIds2Objects(_subType, _mainList, False))
                If Not mgr.CanSortOnClient(_subType, col, _sort, sr) Then
                    Return False
                End If
                Dim c As IComparer = Nothing
                If sr Is Nothing Then
                    c = New OrmComparer(Of OrmBase)(_subType, _sort)
                Else
                    c = sr.CreateSortComparer(_sort)
                End If
                If c Is Nothing Then
                    Return False
                End If
                Dim ad As IOrmBase = mgr.GetOrmBaseFromCacheOrCreate(id, _subType)
                Dim pos As Integer = col.BinarySearch(ad, c)
                If pos < 0 Then
                    _mainList.Insert(Not pos, id)
                End If
            End If
            Return True
        End Function

        Protected Overrides Function PreAdd(ByVal id As Object) As Boolean
            If _sort IsNot Nothing Then
                Dim sr As IOrmSorting = Nothing
                Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
                Dim col As New ArrayList(mgr.ConvertIds2Objects(_subType, _addedList, False))
                If mgr.CanSortOnClient(_subType, col, _sort, sr) Then
                    Dim c As IComparer = Nothing
                    If sr Is Nothing Then
                        c = New OrmComparer(Of OrmBase)(_subType, _sort)
                    Else
                        c = sr.CreateSortComparer(_sort)
                    End If
                    If c IsNot Nothing Then
                        Dim pos As Integer = col.BinarySearch(mgr.GetOrmBaseFromCacheOrCreate(id, _subType), c)
                        If pos < 0 Then
                            _addedList.Insert(Not pos, id)
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

        Protected Overrides Function GetRevert(ByVal mgr As OrmManagerBase) As System.Collections.Generic.List(Of EditableListBase)
            Dim l As New List(Of EditableListBase)
            For Each id As Integer In _mainList
                Dim m As OrmManagerBase.M2MCache = mgr.GetM2MNonGeneric(id.ToString, _subType, _mainType, GetRealDirect)
                If m IsNot Nothing Then
                    l.Add(m.Entry)
                End If
            Next
            Return l
        End Function

        Protected Overrides Function GetRevert(ByVal mgr As OrmManagerBase, ByVal id As Object) As EditableListBase
            Dim m As OrmManagerBase.M2MCache = mgr.GetM2MNonGeneric(id.ToString, _subType, _mainType, GetRealDirect)
            If m IsNot Nothing Then
                Return m.Entry
            End If
            Return Nothing
        End Function

        'Protected Sub AcceptDual()
        '    Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
        '    For Each id As Integer In _mainList
        '        Dim m As OrmManagerBase.M2MCache = mgr.GetM2MNonGeneric(id.ToString, _subType, _mainType, GetRealDirect)
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

        Protected Overrides Function CheckDual(ByVal mgr As OrmManagerBase, ByVal id As Object) As Boolean
            Dim m As OrmManagerBase.M2MCache = mgr.FindM2MNonGeneric(mgr.CreateOrmBase(id, SubType), MainType, Key).First
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

        Protected Overrides Function GetCopy() As EditableListBase
            Return New EditableList(_mainId, _mainList, _mainType, _subType, Key, _sort)
        End Function
#End Region

#Region " Protected properties "

        Protected Friend Property Saved() As Boolean
            Get
                Return _saved
            End Get
            Set(ByVal value As Boolean)
                _saved = value
            End Set
        End Property

#End Region

    End Class
End Namespace
