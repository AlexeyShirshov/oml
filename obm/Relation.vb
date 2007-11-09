Imports System.Collections.Generic

Namespace Orm
    Public Class EditableList
        Private _mainId As Integer
        Private _mainList As IList(Of Integer)
        Private _addedList As New List(Of Integer)
        Private _deletedList As New List(Of Integer)
        Private _non_direct As Boolean
        Private _saved As Boolean
        Private _mainType As Type
        Private _subType As Type
        Private _new As Generic.List(Of Integer)
        Private _sort As Sort
        Private _cantgetCurrent As Boolean
        Private _syncLock As New Object

        Sub New(ByVal mainId As Integer, ByVal mainList As IList(Of Integer), ByVal mainType As Type, ByVal subType As Type, ByVal sort As Sort)
            _mainList = mainList
            _mainId = mainId
            _mainType = mainType
            _subType = subType
            _sort = sort
        End Sub

        Sub New(ByVal mainId As Integer, ByVal mainList As IList(Of Integer), ByVal mainType As Type, ByVal subType As Type, ByVal direct As Boolean, ByVal sort As Sort)
            MyClass.New(mainId, mainList, mainType, subType, sort)
            _non_direct = Not direct
        End Sub

        Public ReadOnly Property CurrentCount() As Integer
            Get
                SyncLock _syncLock
                    Return _mainList.Count + _addedList.Count - _deletedList.Count
                End SyncLock
            End Get
        End Property

        Public ReadOnly Property Current() As IList(Of Integer)
            Get
                If _cantgetCurrent Then
                    Throw New InvalidOperationException("Cannot prepare current data view. Use Original and Added or save changes.")
                End If

                SyncLock _syncLock
                    Dim arr As New List(Of Integer)
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
                                    Dim ex As OrmBase = CType(col(i), OrmBase)
                                    Dim ad As OrmBase = mgr.CreateDBObject(_addedList(j), _subType)
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
                End SyncLock
            End Get
        End Property

        Public ReadOnly Property Original() As IList(Of Integer)
            Get
                Return _mainList
            End Get
        End Property

        Public Function Accept(ByVal mgr As OrmDBManager) As Boolean
            _cantgetCurrent = False
            SyncLock _syncLock
                Dim needaccept As Boolean = _addedList.Count > 0
                If _sort Is Nothing Then
                    CType(_mainList, List(Of Integer)).AddRange(_addedList)
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

                        Dim ml As New List(Of Integer)
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
                            Dim ex As OrmBase = CType(col(i), OrmBase)
                            Dim ad As OrmBase = mgr.CreateDBObject(_addedList(j), _subType)
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
                    CType(_mainList, List(Of Integer)).Remove(o)
                Next
                needaccept = needaccept OrElse _deletedList.Count > 0
                _deletedList.Clear()
                _saved = False
                RemoveNew()

                If needaccept Then
                    AcceptDual()
                End If
            End SyncLock
            Return True
        End Function

        Public Function Accept(ByVal mgr As OrmDBManager, ByVal id As Integer) As Boolean
            SyncLock _syncLock
                If _addedList.Contains(id) Then
                    If _sort Is Nothing Then
                        CType(_mainList, List(Of Integer)).Add(id)
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
                        Dim ad As OrmBase = mgr.CreateDBObject(id, _subType)
                        Dim pos As Integer = col.BinarySearch(ad, c)
                        If pos < 0 Then
                            _mainList.Insert(Not pos, id)
                        End If
                    End If
                    _addedList.Remove(id)
                    If _addedList.Count = 0 Then
                        _cantgetCurrent = False
                    End If
                ElseIf _deletedList.Contains(id) Then
                    CType(_mainList, List(Of Integer)).Remove(id)
                    _deletedList.Remove(id)
                End If
            End SyncLock

            Return True
        End Function

        Protected Sub AcceptDual()
            Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
            For Each id As Integer In _mainList
                Dim m As OrmManagerBase.M2MCache = mgr.GetM2MNonGeneric(id.ToString, _subType, _mainType, GetRealDirect)
                If m IsNot Nothing Then
                    If m.Entry.Added.Contains(_mainId) OrElse m.Entry.Deleted.Contains(_mainId) Then
                        If Not m.Entry.Accept(CType(mgr, OrmDBManager), _mainId) Then
                            Dim obj As OrmBase = mgr.CreateDBObject(id, SubType)
                            mgr.M2MCancel(obj, MainType)
                        End If
                    End If
                End If
            Next
        End Sub

        Protected Sub RejectRelated(ByVal id As Integer, ByVal add As Boolean)
            Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
            Dim m As OrmManagerBase.M2MCache = mgr.FindM2MNonGeneric(mgr.CreateDBObject(id, SubType), MainType, GetRealDirect).First

            Dim l As IList(Of Integer) = m.Entry.Added
            If Not add Then
                l = m.Entry.Deleted
            End If
            If l.Contains(_mainId) Then
                l.Remove(_mainId)
            End If

        End Sub

        Public Sub Reject(ByVal rejectDual As Boolean)
            SyncLock _syncLock
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
            End SyncLock
        End Sub

        Public ReadOnly Property Deleted() As IList(Of Integer)
            Get
                Return _deletedList
            End Get
        End Property

        Public ReadOnly Property Added() As IList(Of Integer)
            Get
                Return _addedList
            End Get
        End Property

        Public Sub AddRange(ByVal ids As IEnumerable(Of Integer))
            For Each id As Integer In ids
                Add(id)
            Next
        End Sub

        Public Sub Add(ByVal id As Integer)
            SyncLock _syncLock
                If _deletedList.Contains(id) Then
                    _deletedList.Remove(id)
                Else
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
                                Dim pos As Integer = col.BinarySearch(mgr.CreateDBObject(id, _subType), c)
                                If pos < 0 Then
                                    _addedList.Insert(Not pos, id)
                                    Return
                                End If
                            Else
                                _cantgetCurrent = True
                            End If
                        Else
                            _cantgetCurrent = True
                        End If
                    End If
                    _addedList.Add(id)
                End If
            End SyncLock
        End Sub

        Public Sub Add(ByVal id As Integer, ByVal idx As Integer)
            SyncLock _syncLock
                If _deletedList.Contains(id) Then
                    _deletedList.Remove(id)
                Else
                    _addedList.Insert(idx, id)
                End If
            End SyncLock
        End Sub

        Public Sub Delete(ByVal id As Integer)
            SyncLock _syncLock
                If _addedList.Contains(id) Then
                    _addedList.Remove(id)
                Else
                    _deletedList.Add(id)
                End If
            End SyncLock
        End Sub

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

        Public ReadOnly Property HasChanges() As Boolean
            Get
                Return HasDeleted OrElse HasAdded
            End Get
        End Property

        Public ReadOnly Property Direct() As Boolean
            Get
                Return Not _non_direct
            End Get
        End Property

        Public Property MainId() As Integer
            Get
                Return _mainId
            End Get
            Protected Friend Set(ByVal value As Integer)
                _mainId = value
            End Set
        End Property

        Public ReadOnly Property Main() As OrmBase
            Get
                Return OrmManagerBase.CurrentManager.CreateDBObject(_mainId, _mainType)
            End Get
        End Property

        'Public Function Clone(ByVal mgr As OrmManagerBase, ByVal main As Type, ByVal subType As Type, ByVal added As List(Of Integer)) As EditableList
        '    Dim newl As EditableList = Nothing
        '    If Not mgr.IsNewObject(main, _mainId) Then
        '        newl = New EditableList(_mainId, _mainList, _mainType, _subType, Direct)
        '        newl._deletedList = _deletedList
        '        Dim ad As New List(Of Integer)
        '        For Each id As Integer In _addedList
        '            If Not mgr.IsNewObject(subType, id) Then
        '                ad.Add(id)
        '            ElseIf added IsNot Nothing Then
        '                added.Add(id)
        '            End If
        '        Next
        '        newl._addedList = ad
        '        _saved = True
        '    End If
        '    Return newl
        'End Function

        Protected Function GetRealDirect() As Boolean
            If SubType Is MainType Then
                Return Not Direct
            Else
                Return Direct
            End If
        End Function

        Protected Function CheckDual(ByVal mgr As OrmManagerBase, ByVal id As Integer) As Boolean
            Dim m As OrmManagerBase.M2MCache = mgr.FindM2MNonGeneric(mgr.CreateDBObject(id, SubType), MainType, GetRealDirect).First
            Dim c As Boolean = True
            For Each i As Integer In m.Entry.Original
                If i = _mainId Then
                    c = False
                    Exit For
                End If
            Next
            If c AndAlso m.Entry.Saved Then
                For Each i As Integer In m.Entry.Current
                    If i = _mainId Then
                        c = False
                        Exit For
                    End If
                Next
            End If
            Return c
        End Function

        Public Function PrepareSave(ByVal mgr As OrmManagerBase) As EditableList
            Dim newl As EditableList = Nothing
            If Not mgr.IsNewObject(_mainType, _mainId) Then
                Dim ad As New List(Of Integer)
                For Each id As Integer In _addedList
                    If mgr.IsNewObject(SubType, id) Then
                        If _new Is Nothing Then
                            _new = New List(Of Integer)
                        End If
                        _new.Add(id)
                    ElseIf CheckDual(mgr, id) Then
                        ad.Add(id)
                    End If
                Next
                If ad.Count > 0 OrElse _deletedList.Count > 0 Then
                    newl = New EditableList(_mainId, _mainList, _mainType, _subType, Direct, _sort)
                    newl._deletedList = _deletedList
                    newl._addedList = ad
                End If
            End If
            Return newl
        End Function

        'Public Function PrepareNewSave(ByVal mgr As OrmManagerBase) As EditableList
        '    Dim newl As EditableList = Nothing
        '    If Not mgr.IsNewObject(_mainType, _mainId) AndAlso HasNew Then
        '        Dim ad As New List(Of Integer)
        '        For Each id As Integer In _new
        '            If mgr.IsNewObject(SubType, id) Then
        '                Throw New InvalidOperationException("List has new object " & id)
        '            ElseIf CheckDual(mgr, id) Then
        '                ad.Add(id)
        '            End If
        '        Next
        '        If ad.Count > 0 Then
        '            newl = New EditableList(_mainId, New List(Of Integer), _mainType, _subType, Direct, _sort)
        '            newl.AddRange(ad)
        '        End If
        '    End If
        '    Return newl
        'End Function

        Protected Friend Property Saved() As Boolean
            Get
                Return _saved
            End Get
            Set(ByVal value As Boolean)
                _saved = value
            End Set
        End Property

        Protected Friend Sub Update(ByVal id As Integer, ByVal oldId As Integer)
            Dim idx As Integer = _addedList.IndexOf(oldId)
            If idx < 0 Then
                Throw New ArgumentException("Old id is not found: " & oldId)
            End If

            _addedList.RemoveAt(idx)
            _addedList.Add(id)

            If HasNew Then
                If _new.Remove(oldId) Then
                    '_new.Add(id)
                End If
            End If
        End Sub

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

        Protected Sub RemoveNew()
            If _new IsNot Nothing Then
                _new.Clear()
            End If
        End Sub

        Public ReadOnly Property HasNew() As Boolean
            Get
                Return _new IsNot Nothing AndAlso _new.Count > 0
            End Get
        End Property
    End Class
End Namespace
