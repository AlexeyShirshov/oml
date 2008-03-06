Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

'<Serializable()> _
'Public Class ReadOnlyList(Of T)
'    Implements ICollection, ICollection(Of T), IEnumerable(Of T), IEnumerable, IList(Of T)
'    ' Methods
'    Friend Sub New(ByVal items As IList(Of T))
'        Me._items = items
'    End Sub

'    Public Sub CopyTo(ByVal array As T(), ByVal arrayIndex As Integer) Implements ICollection(Of T).CopyTo
'        _items.CopyTo(array, arrayIndex)
'    End Sub

'    Public Function _GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
'        Return New Enumerator(Of T)(Me._items)
'    End Function

'    Private Sub Add(ByVal value As T) Implements ICollection(Of T).Add
'        Throw New NotSupportedException
'    End Sub

'    Private Sub Clear() Implements ICollection(Of T).Clear
'        Throw New NotSupportedException
'    End Sub

'    Private Function Contains(ByVal value As T) As Boolean Implements ICollection(Of T).Contains
'        Return _items.Contains(value)
'    End Function

'    Private Function Remove(ByVal value As T) As Boolean Implements ICollection(Of T).Remove
'        Throw New NotSupportedException
'    End Function

'    Public Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
'        Return New Enumerator(Of T)(Me._items)
'    End Function

'    Private Sub CopyTo(ByVal array As Array, ByVal arrayIndex As Integer) Implements ICollection.CopyTo
'        CopyTo(CType(array, T()), arrayIndex)
'    End Sub

'    ' Properties
'    Public ReadOnly Property Count() As Integer Implements ICollection(Of T).Count
'        Get
'            Return Me._items.Count
'        End Get
'    End Property

'    Private ReadOnly Property _Count() As Integer Implements ICollection.Count
'        Get
'            Return Me._items.Count
'        End Get
'    End Property

'    Private ReadOnly Property IsReadOnly() As Boolean Implements ICollection(Of T).IsReadOnly
'        Get
'            Return True
'        End Get
'    End Property

'    Private ReadOnly Property IsSynchronized() As Boolean Implements ICollection.IsSynchronized
'        Get
'            Return False
'        End Get
'    End Property

'    Private ReadOnly Property SyncRoot() As Object Implements ICollection.SyncRoot
'        Get
'            Return Me._items
'        End Get
'    End Property


'    ' Fields
'    Private _items As IList(Of T)

'    ' Nested Types
'    <Serializable()> _
'    Friend Structure Enumerator(Of K)
'        Implements IEnumerator(Of K), IDisposable, IEnumerator

'        Private _items As IList(Of K)
'        Private _index As Integer

'        Friend Sub New(ByVal items As IList(Of K))
'            Me._items = items
'            Me._index = -1
'        End Sub

'        Public Sub Dispose() Implements IDisposable.Dispose
'        End Sub

'        Public Function MoveNext() As Boolean Implements IEnumerator.MoveNext
'            Return (++Me._index < Me._items.Count)
'        End Function

'        Private ReadOnly Property _Current() As Object Implements IEnumerator.Current
'            Get
'                Return _items(Me._index)
'            End Get
'        End Property

'        Public ReadOnly Property Current() As K Implements IEnumerator(Of K).Current
'            Get
'                Return _items(Me._index)
'            End Get
'        End Property

'        Private Sub Reset() Implements IEnumerator.Reset
'            Me._index = -1
'        End Sub
'    End Structure

'    Public Function IndexOf(ByVal item As T) As Integer Implements System.Collections.Generic.IList(Of T).IndexOf
'        Return _items.IndexOf(item)
'    End Function

'    Public Sub Insert(ByVal index As Integer, ByVal item As T) Implements System.Collections.Generic.IList(Of T).Insert
'        Throw New NotSupportedException
'    End Sub

'    Default Public Property Item(ByVal index As Integer) As T Implements System.Collections.Generic.IList(Of T).Item
'        Get
'            Return _items(index)
'        End Get
'        Set(ByVal value As T)
'            Throw New NotSupportedException
'        End Set
'    End Property

'    Public Sub RemoveAt(ByVal index As Integer) Implements System.Collections.Generic.IList(Of T).RemoveAt
'        Throw New NotSupportedException
'    End Sub
'End Class

Friend Interface IListEdit
    Inherits IList
    Overloads Sub Add(ByVal o As Orm.OrmBase)
    Overloads Sub Remove(ByVal o As Orm.OrmBase)
    Overloads Sub Insert(ByVal pos As Integer, ByVal o As Orm.OrmBase)
End Interface

Public Class ReadOnlyList(Of T As Orm.OrmBase)
    Inherits ObjectModel.ReadOnlyCollection(Of T)
    Implements IListEdit

    Private _l As List(Of T)
    Protected Friend ReadOnly Property List() As IList(Of T)
        Get
            Return _l
        End Get
    End Property

    Public Sub New()
        MyClass.New(New List(Of T))
    End Sub

    Public Sub New(ByVal col As IEnumerable(Of T))
        MyBase.New(New List(Of T)(col))
    End Sub

    Public Sub New(ByVal list As List(Of T))
        MyBase.New(list)
        _l = list
    End Sub

    Public Sub New(ByVal list As ReadOnlyList(Of T))
        MyClass.New(New List(Of T)(list))
    End Sub

    Public Sub Add(ByVal o As T)
        _l.Add(o)
    End Sub

    Public Sub AddRange(ByVal col As IEnumerable(Of T))
        'For Each o As T In col
        '    Add(o)
        'Next
        _l.AddRange(col)
    End Sub

    Public Sub Sort(ByVal cs As IComparer(Of T))
        _l.Sort(cs)
    End Sub

    Private Sub _Add(ByVal o As Orm.OrmBase) Implements IListEdit.Add
        CType(_l, IList).Add(o)
    End Sub

    Public Overloads Sub Insert(ByVal pos As Integer, ByVal o As Orm.OrmBase) Implements IListEdit.Insert
        CType(_l, IList).Insert(pos, o)
    End Sub

    Public Overloads Sub Remove(ByVal o As Orm.OrmBase) Implements IListEdit.Remove
        CType(_l, IList).Remove(o)
    End Sub
End Class

Namespace Collections

    Public Class CopyDictionaryEnumerator
        Inherits CopyEnumerator(Of DictionaryEntry)
        Implements IDictionaryEnumerator

        Public Sub New(ByVal coll As ICollection)
            MyBase.New(CType(New ArrayList(coll).ToArray(GetType(DictionaryEntry)), Global.System.Collections.Generic.ICollection(Of Global.System.Collections.DictionaryEntry)))
        End Sub

        Public ReadOnly Property Entry() As System.Collections.DictionaryEntry Implements System.Collections.IDictionaryEnumerator.Entry
            Get
                Return Current
            End Get
        End Property

        Public ReadOnly Property Key() As Object Implements System.Collections.IDictionaryEnumerator.Key
            Get
                Return Entry.Key
            End Get
        End Property

        Public ReadOnly Property Value() As Object Implements System.Collections.IDictionaryEnumerator.Value
            Get
                Return Entry.Value
            End Get
        End Property
    End Class

    Public Class CopyEnumerator(Of T)
        Implements IEnumerator, IEnumerator(Of T)

        Private list() As T
        Private idx As Integer

        Public Sub New(ByVal coll As ICollection(Of T))
            list = New T(coll.Count - 1) {}
            coll.CopyTo(list, 0)
            Reset()
        End Sub

        Public ReadOnly Property Current1() As Object Implements System.Collections.IEnumerator.Current
            Get
                Return Current
            End Get
        End Property

        Public Function MoveNext() As Boolean Implements System.Collections.IEnumerator.MoveNext
            If idx < 0 Then idx = 0 Else idx += 1
            If idx = list.Length Then
                idx = -2
                Return False
            Else
                Return True
            End If
        End Function

        Public Sub Reset() Implements System.Collections.IEnumerator.Reset
            idx = -1
        End Sub

        Public ReadOnly Property Current() As T Implements System.Collections.Generic.IEnumerator(Of T).Current
            Get
                If idx = -1 Then Throw New InvalidOperationException("Call MoveNext first")
                If idx = -2 Then Throw New InvalidOperationException("Call Reset first")
                Return list(idx)
            End Get
        End Property

#Region " IDisposable Support "
        Private disposed As Boolean = False

        ' IDisposable
        Private Overloads Sub Dispose(ByVal disposing As Boolean)
            If Not Me.disposed Then
                If disposing Then

                End If
            End If
            Me.disposed = True
        End Sub

        ' This code added by Visual Basic to correctly implement the disposable pattern.
        <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")> _
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063")> _
        Protected Overrides Sub Finalize()
            ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
            Dispose(False)
            MyBase.Finalize()
        End Sub
#End Region

    End Class

    <Serializable()> _
    Public NotInheritable Class IndexedCollectionException
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

        Private Sub New( _
            ByVal info As System.Runtime.Serialization.SerializationInfo, _
            ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
            ' Insert code here for custom properties here.
        End Sub
    End Class

    Public MustInherit Class IndexedCollection(Of TItemKey, TItem)
        'Inherits ObjectModel.Collection(Of TItem)
        Implements IDictionary(Of TItemKey, TItem), IDictionary,  _
        IList(Of TItem), IList

        Private _dic As IDictionary(Of TItemKey, TItem)
        Private _coll As IList(Of TItem)
        Private _keyCount As Integer
        Private _threshold As Integer = 1
        Private _rw As System.Threading.ReaderWriterLock

        Public Sub New()
            _coll = GetCollection()
            _rw = New System.Threading.ReaderWriterLock()
        End Sub

        Public Sub New(ByVal threshold As Integer)
            MyBase.New()
            _threshold = threshold
        End Sub

        Public Overridable Function SyncHelper(ByVal reader As Boolean) As IDisposable
            'Return New RWScopeMgr(reader, _rw)
            Return New CSScopeMgr(Me)
        End Function

        Protected MustOverride Function GetKeyForItem(ByVal item As TItem) As TItemKey

        Protected Function GetItemFromCollection(ByVal key As TItemKey, ByVal trowexception As Boolean) As TItem
            Using SyncHelper(True)
                Debug.Assert(_coll IsNot Nothing)
                'Using enumerator1 As IEnumerator(Of TItem) = DirectCast(GetEnumerator(), IEnumerator(Of TItem))
                'Do While enumerator1.MoveNext
                '    Dim local1 As TItem = enumerator1.Current
                '    If GetKeyForItem(local1).Equals(key) Then
                '        Return local1
                '    End If
                'Loop
                'End Using
                For Each l As TItem In _coll
                    If GetKeyForItem(l).Equals(key) Then
                        Return l
                    End If
                Next
            End Using
            If trowexception Then
                Throw New KeyNotFoundException
            End If
            Return Nothing
        End Function

        Protected Overridable Function GetDictionary() As IDictionary(Of TItemKey, TItem)
            Return New Dictionary(Of TItemKey, TItem)
        End Function

        Protected Overridable Function GetCollection() As IList(Of TItem)
            Return New List(Of TItem)
        End Function

        Protected Sub CreateDictionary()
            Using SyncHelper(False)
                If _dic Is Nothing Then
                    _dic = GetDictionary()

                    'Using enumerator1 As IEnumerator(Of TItem) = GetEnumerator()
                    '    Do While enumerator1.MoveNext
                    '        Dim item As TItem = enumerator1.Current
                    '        Dim key As TItemKey = GetKeyForItem(item)
                    '        If key Is Nothing Then
                    '            Throw New IndexedCollectionException(String.Format("Key value for item {0} is nothing", item))
                    '        End If
                    '        _dic.Add(key, item)
                    '    Loop
                    'End Using
                    For Each l As TItem In _coll
                        Dim key As TItemKey = GetKeyForItem(l)
                        If key Is Nothing Then
                            Throw New IndexedCollectionException(String.Format("Key value for item {0} is nothing", l))
                        End If
                        _dic.Add(key, l)
                    Next
                    _coll = Nothing
                End If
            End Using
        End Sub

        Public Sub AddRange(ByVal items As ICollection(Of TItem))
            For Each i As TItem In items
                Add(i)
            Next
        End Sub

#Region " IDictionary (Of) implementation "

        Protected Sub Add1(ByVal item As System.Collections.Generic.KeyValuePair(Of TItemKey, TItem)) Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of TItemKey, TItem)).Add
            Add2(item.Key, item.Value)
        End Sub

        Protected Sub Clear1() Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of TItemKey, TItem)).Clear
            Using SyncHelper(False)
                Invariant()

                If _coll IsNot Nothing Then
                    _coll.Clear()
                Else
                    _dic.Clear()
                End If

            End Using
        End Sub

        Protected Function Contains1(ByVal item As System.Collections.Generic.KeyValuePair(Of TItemKey, TItem)) As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of TItemKey, TItem)).Contains
            Using SyncHelper(True)
                Invariant()

                If _coll IsNot Nothing Then
                    Return GetItemFromCollection(item.Key, False) IsNot Nothing
                Else
                    Return _dic.Contains(item)
                End If
            End Using
        End Function

        Protected Sub CopyTo1(ByVal array() As System.Collections.Generic.KeyValuePair(Of TItemKey, TItem), ByVal arrayIndex As Integer) Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of TItemKey, TItem)).CopyTo
            CreateDictionary()
            Using SyncHelper(True)
                _dic.CopyTo(array, arrayIndex)
            End Using
        End Sub

        Protected ReadOnly Property Count1() As Integer Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of TItemKey, TItem)).Count
            Get
                Using SyncHelper(True)
                    Return Count
                End Using
            End Get
        End Property

        Protected ReadOnly Property IsReadOnly() As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of TItemKey, TItem)).IsReadOnly
            Get
                Return False
            End Get
        End Property

        Protected Function Remove1(ByVal item As System.Collections.Generic.KeyValuePair(Of TItemKey, TItem)) As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of TItemKey, TItem)).Remove
            Using SyncHelper(False)
                Invariant()

                If _coll IsNot Nothing Then
                    Return _coll.Remove(item.Value)
                Else
                    Return CType(_dic, ICollection(Of KeyValuePair(Of TItemKey, TItem))).Remove(item)
                End If

            End Using
        End Function

        Protected Sub Add2(ByVal key As TItemKey, ByVal value As TItem) Implements System.Collections.Generic.IDictionary(Of TItemKey, TItem).Add
            Using SyncHelper(False)
                Invariant()

                If _coll IsNot Nothing Then
                    Dim item As TItem = GetItemFromCollection(key, False)
                    If item IsNot Nothing Then
                        Throw New ArgumentException(String.Format("Adding duplicate key {0}", key), "item")
                    ElseIf Not GetKeyForItem(value).Equals(key) Then
                        Throw New ArgumentException(String.Format("Key {0} is not corresponds to item. Item key is {1}.", key, GetKeyForItem(value)), "key")
                    Else
                        _coll.Add(value)
                    End If

                    If _coll.Count = _threshold Then
                        CreateDictionary()
                    End If
                Else
                    _dic.Add(key, value)
                End If
            End Using
        End Sub

        Public Function ContainsKey(ByVal key As TItemKey) As Boolean Implements System.Collections.Generic.IDictionary(Of TItemKey, TItem).ContainsKey
            Using SyncHelper(True)
                If _coll IsNot Nothing Then
                    Return GetItemFromCollection(key, False) IsNot Nothing
                Else
                    Return _dic.ContainsKey(key)
                End If
            End Using
        End Function

        Default Public Overloads Property Item(ByVal key As TItemKey) As TItem Implements System.Collections.Generic.IDictionary(Of TItemKey, TItem).Item
            Get
                Using SyncHelper(True)
                    Invariant()

                    If _coll IsNot Nothing Then
                        Return GetItemFromCollection(key, True)
                    Else
                        Return _dic.Item(key)
                    End If
                End Using
            End Get
            Set(ByVal value As TItem)
                Using SyncHelper(False)
                    CreateDictionary()
                    If _dic IsNot Nothing Then
                        _dic.Item(key) = value
                    End If
                End Using
            End Set
        End Property

        Public ReadOnly Property Keys() As System.Collections.Generic.ICollection(Of TItemKey) Implements System.Collections.Generic.IDictionary(Of TItemKey, TItem).Keys
            Get
                Using SyncHelper(True)
                    Invariant()

                    If _coll IsNot Nothing Then
                        Dim l As New List(Of TItemKey)
                        Using enumerator1 As IEnumerator(Of TItem) = _coll.GetEnumerator
                            Do While enumerator1.MoveNext
                                Dim local1 As TItem = enumerator1.Current
                                l.Add(GetKeyForItem(local1))
                            Loop
                        End Using
                        Return l.ToArray
                    Else
                        Return _dic.Keys
                    End If

                End Using
            End Get
        End Property

        Public Function Remove(ByVal key As TItemKey) As Boolean Implements System.Collections.Generic.IDictionary(Of TItemKey, TItem).Remove
            Using SyncHelper(False)
                If _coll IsNot Nothing Then
                    Dim item As TItem = GetItemFromCollection(key, False)
                    If item Is Nothing Then Return False
                    Return _coll.Remove(item)
                Else
                    Return _dic.Remove(key)
                End If
            End Using
        End Function

        Public Function TryGetValue(ByVal key As TItemKey, ByRef value As TItem) As Boolean Implements System.Collections.Generic.IDictionary(Of TItemKey, TItem).TryGetValue
            Using SyncHelper(True)
                Invariant()

                If _dic IsNot Nothing Then
                    Return _dic.TryGetValue(key, value)
                Else
                    value = GetItemFromCollection(key, False)
                    Return value IsNot Nothing
                End If
            End Using
        End Function

        Protected ReadOnly Property Values() As System.Collections.Generic.ICollection(Of TItem) Implements System.Collections.Generic.IDictionary(Of TItemKey, TItem).Values
            Get
                Using SyncHelper(True)
                    Invariant()

                    If _coll IsNot Nothing Then
                        Return _coll
                    Else
                        Return _dic.Values
                    End If
                End Using
            End Get
        End Property

#End Region

#Region " ICollection (Of) implementation "

        Public Sub Add(ByVal item As TItem) Implements System.Collections.Generic.ICollection(Of TItem).Add
            Using SyncHelper(False)
                Invariant()

                If _coll IsNot Nothing Then
                    _coll.Add(item)
                End If

                If _coll IsNot Nothing AndAlso _coll.Count = _threshold Then
                    CreateDictionary()
                ElseIf _dic IsNot Nothing Then
                    _dic.Add(GetKeyForItem(item), item)
                End If
            End Using
        End Sub

        Public Sub Clear() Implements System.Collections.Generic.ICollection(Of TItem).Clear
            Using SyncHelper(False)
                Invariant()

                If _coll IsNot Nothing Then
                    _coll.Clear()
                Else
                    _dic.Clear()
                End If
            End Using
        End Sub

        Public Function Contains(ByVal item As TItem) As Boolean Implements System.Collections.Generic.ICollection(Of TItem).Contains
            Using SyncHelper(True)
                Invariant()

                If _coll IsNot Nothing Then
                    Return _coll.Contains(item)
                Else
                    Return _dic.ContainsKey(GetKeyForItem(item))
                End If
            End Using
        End Function

        Public Sub CopyTo(ByVal array() As TItem, ByVal arrayIndex As Integer) Implements System.Collections.Generic.ICollection(Of TItem).CopyTo
            Using SyncHelper(True)
                Invariant()

                If _coll IsNot Nothing Then
                    _coll.CopyTo(array, arrayIndex)
                Else
                    Values.CopyTo(array, arrayIndex)
                End If
            End Using
        End Sub

        Public ReadOnly Property Count() As Integer Implements System.Collections.Generic.ICollection(Of TItem).Count
            Get
                Using SyncHelper(True)
                    If _coll IsNot Nothing Then
                        Return _coll.Count
                    Else
                        Return _dic.Count
                    End If
                End Using
            End Get
        End Property

        Protected ReadOnly Property IsReadOnly1() As Boolean Implements System.Collections.Generic.ICollection(Of TItem).IsReadOnly
            Get
                Return False
            End Get
        End Property

        Public Function Remove(ByVal item As TItem) As Boolean Implements System.Collections.Generic.ICollection(Of TItem).Remove
            Using SyncHelper(False)
                Invariant()

                If _coll IsNot Nothing Then
                    Return _coll.Remove(item)
                Else
                    Return _dic.Remove(GetKeyForItem(item))
                End If
            End Using
        End Function

#End Region

#Region " IList (Of) implementation "

        Public Function IndexOf(ByVal item As TItem) As Integer Implements System.Collections.Generic.IList(Of TItem).IndexOf
            Using SyncHelper(True)
                Invariant()

                If _coll IsNot Nothing Then
                    Return _coll.IndexOf(item)
                Else
                    Using enumerator1 As IEnumerator(Of TItem) = GetEnumerator()
                        Dim i As Integer = 0
                        Do While enumerator1.MoveNext
                            If enumerator1.Current.Equals(item) Then
                                Return i
                            End If
                            i += 1
                        Loop
                    End Using
                End If
            End Using
        End Function

        Public Sub Insert(ByVal index As Integer, ByVal item As TItem) Implements System.Collections.Generic.IList(Of TItem).Insert
            Throw New NotSupportedException
        End Sub

        Default Public Overloads Property Item(ByVal index As Integer) As TItem Implements System.Collections.Generic.IList(Of TItem).Item
            Get
                Using SyncHelper(True)
                    Invariant()

                    If _coll IsNot Nothing Then
                        Return _coll.Item(index)
                    Else
                        Using enumerator1 As IEnumerator(Of TItem) = GetEnumerator()
                            Do While enumerator1.MoveNext
                                Dim c As TItem = enumerator1.Current
                                If c.Equals(Item) Then
                                    Return c
                                End If
                            Loop
                        End Using
                    End If
                End Using
            End Get
            Set(ByVal value As TItem)
                Using SyncHelper(False)
                    Invariant()

                    If _coll IsNot Nothing Then
                        _coll.Item(index) = value
                    End If

                    If _coll.Count >= _threshold Then
                        CreateDictionary()
                    End If

                    If _dic IsNot Nothing Then
                        Dim i As TItem = Item(index)
                        _dic.Item(GetKeyForItem(i)) = value
                    End If
                End Using
            End Set
        End Property

        Public Sub RemoveAt(ByVal index As Integer) Implements System.Collections.Generic.IList(Of TItem).RemoveAt
            Using SyncHelper(False)
                Invariant()

                If _coll IsNot Nothing Then
                    _coll.RemoveAt(index)
                Else
                    Dim i As TItem = Item(index)
                    _dic.Remove(GetKeyForItem(i))
                End If
            End Using
        End Sub

#End Region

#Region " ICollection implemenation "

        Public Sub CopyToArray(ByVal array As System.Array, ByVal index As Integer) Implements System.Collections.ICollection.CopyTo
            Using SyncHelper(True)
                Invariant()

                If _coll IsNot Nothing Then
                    CType(_coll, ICollection).CopyTo(array, index)
                Else
                    [ICollection_Values].CopyTo(array, index)
                End If
            End Using
        End Sub

        Protected ReadOnly Property Count2() As Integer Implements System.Collections.ICollection.Count
            Get
                Return Count
            End Get
        End Property

        Public ReadOnly Property IsSynchronized() As Boolean Implements System.Collections.ICollection.IsSynchronized
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property SyncRoot() As Object Implements System.Collections.ICollection.SyncRoot
            Get
                Throw New NotImplementedException
            End Get
        End Property
#End Region

#Region " IDictionary implemenation "

        Protected Sub Add3(ByVal key As Object, ByVal value As Object) Implements System.Collections.IDictionary.Add
            Add2(CType(key, TItemKey), CType(value, TItem))
        End Sub

        Protected Sub Clear2() Implements System.Collections.IDictionary.Clear
            Clear()
        End Sub

        Protected Function Contains2(ByVal key As Object) As Boolean Implements System.Collections.IDictionary.Contains
            Return ContainsKey(CType(key, TItemKey))
        End Function

        Public ReadOnly Property IsFixedSize() As Boolean Implements System.Collections.IDictionary.IsFixedSize
            Get
                Return False
            End Get
        End Property

        Protected ReadOnly Property IsReadOnly2() As Boolean Implements System.Collections.IDictionary.IsReadOnly
            Get
                Return IsReadOnly
            End Get
        End Property

        Protected Overloads Property [IDictionary_Item](ByVal key As Object) As Object Implements System.Collections.IDictionary.Item
            Get
                Return Item(CType(key, TItemKey))
            End Get
            Set(ByVal value As Object)
                Item(CType(key, TItemKey)) = CType(value, TItem)
            End Set
        End Property

        Protected ReadOnly Property Keys1() As System.Collections.ICollection Implements System.Collections.IDictionary.Keys
            Get
                Using SyncHelper(True)
                    Invariant()

                    If _coll IsNot Nothing Then
                        Dim l As New ArrayList
                        For Each item As TItem In _coll
                            l.Add(GetKeyForItem(item))
                        Next
                        Return l
                    Else
                        Return CType(_dic, IDictionary).Keys
                    End If

                End Using
            End Get
        End Property

        Protected Sub Remove3(ByVal key As Object) Implements System.Collections.IDictionary.Remove
            Remove(CType(key, TItemKey))
        End Sub

        Protected ReadOnly Property [ICollection_Values]() As System.Collections.ICollection Implements System.Collections.IDictionary.Values
            Get
                Using SyncHelper(True)
                    Invariant()

                    If _coll IsNot Nothing Then
                        Return CType(_coll, System.Collections.ICollection)
                    Else
                        Return CType(_dic, IDictionary).Values
                    End If

                End Using
            End Get
        End Property

#End Region

#Region " IList implementation "

        Protected Function Add4(ByVal value As Object) As Integer Implements System.Collections.IList.Add
            Add(CType(value, TItem))
        End Function

        Protected Sub Clear3() Implements System.Collections.IList.Clear
            Clear()
        End Sub

        Protected Function Contains3(ByVal value As Object) As Boolean Implements System.Collections.IList.Contains
            Return Contains(CType(value, TItem))
        End Function

        Protected Function IndexOf1(ByVal value As Object) As Integer Implements System.Collections.IList.IndexOf
            Return IndexOf(CType(value, TItem))
        End Function

        Protected Sub Insert1(ByVal index As Integer, ByVal value As Object) Implements System.Collections.IList.Insert
            Insert(index, CType(value, TItem))
        End Sub

        Protected ReadOnly Property IsFixedSize1() As Boolean Implements System.Collections.IList.IsFixedSize
            Get
                Return IsFixedSize
            End Get
        End Property

        Protected ReadOnly Property IsReadOnly3() As Boolean Implements System.Collections.IList.IsReadOnly
            Get
                Return IsReadOnly
            End Get
        End Property

        Protected Overloads Property [IList_Item](ByVal index As Integer) As Object Implements System.Collections.IList.Item
            Get
                Return Item(index)
            End Get
            Set(ByVal value As Object)
                Item(index) = CType(value, TItem)
            End Set
        End Property

        Protected Sub Remove4(ByVal value As Object) Implements System.Collections.IList.Remove
            Remove(CType(value, TItem))
        End Sub

        Protected Sub RemoveAt1(ByVal index As Integer) Implements System.Collections.IList.RemoveAt
            RemoveAt(index)
        End Sub
#End Region

#Region " Enumerators "

        Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of TItem) Implements System.Collections.Generic.IEnumerable(Of TItem).GetEnumerator
            Using SyncHelper(True)
                Return New CopyEnumerator(Of TItem)(Me)
            End Using
        End Function

        Protected Function GetEnumerator1() As System.Collections.Generic.IEnumerator(Of System.Collections.Generic.KeyValuePair(Of TItemKey, TItem)) Implements System.Collections.Generic.IEnumerable(Of System.Collections.Generic.KeyValuePair(Of TItemKey, TItem)).GetEnumerator
            Using SyncHelper(True)
                Return New CopyEnumerator(Of System.Collections.Generic.KeyValuePair(Of TItemKey, TItem))(Me)
            End Using
        End Function

        Protected Function GetEnumerator2() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Using SyncHelper(True)
                Return New CopyEnumerator(Of TItem)(Me)
            End Using
        End Function

        Protected Function GetEnumerator3() As System.Collections.IDictionaryEnumerator Implements System.Collections.IDictionary.GetEnumerator
            Using SyncHelper(True)
                Dim l As New ArrayList
                For Each item As TItem In Me
                    l.Add(New DictionaryEntry(GetKeyForItem(item), item))
                Next

                Return New CopyDictionaryEnumerator(l)
            End Using
        End Function

#End Region

        <Conditional("DEBUG")> _
        Protected Sub Invariant()
            If _coll Is Nothing AndAlso _dic Is Nothing Then
                Throw New IndexedCollectionException("Invalid state. Both nulls.")
            End If

            If _coll IsNot Nothing AndAlso _dic IsNot Nothing AndAlso _coll.Count <> _threshold Then
                Throw New IndexedCollectionException("Invalid state. Boths not nulls.")
            End If
        End Sub
    End Class

    Public Class IntList
        Private _i As New Generic.List(Of Integer)

        Public Sub Append(ByVal i As Integer)
            _i.Add(i)
        End Sub

        'Public Overrides Function ToString() As String
        '    Dim sb As New StringBuilder
        '    For Each i As Integer In _i
        '        sb.Append(i).Append(",")
        '    Next
        '    sb.Length -= 1
        '    Return sb.ToString
        'End Function

        'Public Function ToArray() As Integer()
        '    Return _i.ToArray
        'End Function

        Public ReadOnly Property Count() As Integer
            Get
                Return _i.Count
            End Get
        End Property

        Public ReadOnly Property Ints() As IList(Of Integer)
            Get
                Return _i
            End Get
        End Property

    End Class

    Public Class HybridDictionary(Of T)
        Implements System.Collections.Generic.IDictionary(Of Integer, T), IDictionary

        Protected Class Enumerator
            Implements IEnumerator, IEnumerator(Of KeyValuePair(Of Integer, T))

            'Private dic As Dictionary(Of Integer, T)
            Private list As New List(Of KeyValuePair(Of Integer, T))
            Private idx As Integer

            Public Sub New(ByVal dic As Dictionary(Of Integer, T))
                'Me.dic = dic
                SyncLock dic
                    list.AddRange(CType(dic, ICollection(Of KeyValuePair(Of Integer, T))))
                End SyncLock
                Reset()
            End Sub

            Public ReadOnly Property Current1() As Object Implements System.Collections.IEnumerator.Current
                Get
                    Return Current
                End Get
            End Property

            Public Function MoveNext() As Boolean Implements System.Collections.IEnumerator.MoveNext
                If idx < 0 Then idx = 0 Else idx += 1
                If idx = list.Count Then
                    idx = -2
                    Return False
                Else
                    Return True
                End If
            End Function

            Public Sub Reset() Implements System.Collections.IEnumerator.Reset
                idx = -1
            End Sub

            Public ReadOnly Property Current() As System.Collections.Generic.KeyValuePair(Of Integer, T) Implements System.Collections.Generic.IEnumerator(Of System.Collections.Generic.KeyValuePair(Of Integer, T)).Current
                Get
                    If idx = -1 Then Throw New InvalidOperationException("You should call MoveNext first.")
                    If idx = -2 Then Throw New InvalidOperationException("You are at the end of the collection.")
                    Return list(idx)
                End Get
            End Property

            Private disposed As Boolean = False

            ' IDisposable
            Private Overloads Sub Dispose(ByVal disposing As Boolean)
                If Not Me.disposed Then
                    If disposing Then

                    End If


                End If
                Me.disposed = True
            End Sub

#Region " IDisposable Support "
            ' This code added by Visual Basic to correctly implement the disposable pattern.
            <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")> _
            Public Overloads Sub Dispose() Implements IDisposable.Dispose
                ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
                Dispose(True)
                GC.SuppressFinalize(Me)
            End Sub

            <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063")> _
            Protected Overrides Sub Finalize()
                ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
                Dispose(False)
                MyBase.Finalize()
            End Sub
#End Region

        End Class

        Private dic As New Dictionary(Of Integer, T)

        Private ReadOnly Property collection() As ICollection(Of KeyValuePair(Of Integer, T))
            Get
                Return CType(dic, ICollection(Of KeyValuePair(Of Integer, T)))
            End Get
        End Property


        Private ReadOnly Property dictionary() As IDictionary
            Get
                Return CType(dic, IDictionary)
            End Get
        End Property

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Protected Sub Add1(ByVal item As System.Collections.Generic.KeyValuePair(Of Integer, T)) Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, T)).Add
            collection.Add(item)
        End Sub

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub Clear() Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, T)).Clear
            collection.Clear()
        End Sub

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Function Contains(ByVal item As System.Collections.Generic.KeyValuePair(Of Integer, T)) As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, T)).Contains
            Return collection.Contains(item)
        End Function

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub CopyTo(ByVal array() As System.Collections.Generic.KeyValuePair(Of Integer, T), ByVal arrayIndex As Integer) Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, T)).CopyTo
            collection.CopyTo(array, arrayIndex)
        End Sub

        Public ReadOnly Property Count() As Integer Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, T)).Count
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return collection.Count
            End Get
        End Property

        Public ReadOnly Property IsReadOnly() As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, T)).IsReadOnly
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return collection.IsReadOnly
            End Get
        End Property

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Protected Function Remove1(ByVal item As System.Collections.Generic.KeyValuePair(Of Integer, T)) As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Integer, T)).Remove
            Return collection.Remove(item)
        End Function

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub Add(ByVal key As Integer, ByVal value As T) Implements System.Collections.Generic.IDictionary(Of Integer, T).Add
            dic.Add(key, value)
        End Sub

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Function ContainsKey(ByVal key As Integer) As Boolean Implements System.Collections.Generic.IDictionary(Of Integer, T).ContainsKey
            Return dic.ContainsKey(key)
        End Function

        Default Public Property Item(ByVal key As Integer) As T Implements System.Collections.Generic.IDictionary(Of Integer, T).Item
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return dic(key)
            End Get
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Set(ByVal value As T)
                dic(key) = value
            End Set
        End Property

        Public ReadOnly Property Keys() As System.Collections.Generic.ICollection(Of Integer) Implements System.Collections.Generic.IDictionary(Of Integer, T).Keys
            Get
                'Dim arr As New List(Of Integer)
                'SyncLock Me
                '    arr.AddRange(dic.Keys)
                'End SyncLock
                'Return arr
                Return dic.Keys
            End Get
        End Property

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Function Remove(ByVal key As Integer) As Boolean Implements System.Collections.Generic.IDictionary(Of Integer, T).Remove
            Return dic.Remove(key)
        End Function

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Function TryGetValue(ByVal key As Integer, ByRef value As T) As Boolean Implements System.Collections.Generic.IDictionary(Of Integer, T).TryGetValue
            Return dic.TryGetValue(key, value)
        End Function

        Public ReadOnly Property Values() As System.Collections.Generic.ICollection(Of T) Implements System.Collections.Generic.IDictionary(Of Integer, T).Values
            Get
                'Dim arr As New List(Of T)
                'SyncLock Me
                '    arr.AddRange(dic.Values)
                'End SyncLock
                'Return arr
                Return dic.Values
            End Get
        End Property

        Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of System.Collections.Generic.KeyValuePair(Of Integer, T)) Implements System.Collections.Generic.IEnumerable(Of System.Collections.Generic.KeyValuePair(Of Integer, T)).GetEnumerator
            Return New Enumerator(dic)
        End Function

        Protected Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return New Enumerator(dic)
        End Function

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub CopyTo1(ByVal array As System.Array, ByVal index As Integer) Implements System.Collections.ICollection.CopyTo
            dictionary.CopyTo(array, index)
        End Sub

        Public ReadOnly Property Count1() As Integer Implements System.Collections.ICollection.Count
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return Count
            End Get
        End Property

        Public ReadOnly Property IsSynchronized() As Boolean Implements System.Collections.ICollection.IsSynchronized
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return dictionary.IsSynchronized
            End Get
        End Property

        Public ReadOnly Property SyncRoot() As Object Implements System.Collections.ICollection.SyncRoot
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return dictionary.SyncRoot
            End Get
        End Property

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub Add2(ByVal key As Object, ByVal value As Object) Implements System.Collections.IDictionary.Add
            dictionary.Add(key, value)
        End Sub

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub Clear1() Implements System.Collections.IDictionary.Clear
            dictionary.Clear()
        End Sub

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Function Contains1(ByVal key As Object) As Boolean Implements System.Collections.IDictionary.Contains
            Return dictionary.Contains(key)
        End Function

        Public Function GetEnumerator2() As System.Collections.IDictionaryEnumerator Implements System.Collections.IDictionary.GetEnumerator
            Throw New NotImplementedException
        End Function

        Public ReadOnly Property IsFixedSize() As Boolean Implements System.Collections.IDictionary.IsFixedSize
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return dictionary.IsFixedSize
            End Get
        End Property

        Public ReadOnly Property IsReadOnly1() As Boolean Implements System.Collections.IDictionary.IsReadOnly
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return dictionary.IsReadOnly
            End Get
        End Property

        Private Overloads Property Item1(ByVal key As Object) As Object Implements System.Collections.IDictionary.Item
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return dictionary(key)
            End Get
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Set(ByVal value As Object)
                dictionary(key) = value
            End Set
        End Property

        Public ReadOnly Property Keys1() As System.Collections.ICollection Implements System.Collections.IDictionary.Keys
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return dictionary.Keys
            End Get
        End Property

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub Remove2(ByVal key As Object) Implements System.Collections.IDictionary.Remove
            dictionary.Remove(key)
        End Sub

        Public ReadOnly Property Values1() As System.Collections.ICollection Implements System.Collections.IDictionary.Values
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return dictionary.Values
            End Get
        End Property
    End Class

End Namespace

''' <summary>
''' Модуль небольших функций для внутреннего использования по всему солюшену
''' </summary>
''' <remarks></remarks>
Public Module helper

    ''' <summary>
    ''' Метод определяет нужно ли добавлять псевдоним таблицы для поля в БД
    ''' </summary>
    ''' <param name="str">Название поля в БД</param>
    ''' <returns><b>true</b> если псевдоним необходим. В противном случае <b>false</b></returns>
    ''' <remarks>Для вычисляемых полей или скалярных подзапросов префикс (псевдоним) таблицы не нужен.</remarks>
    Public Function ShouldPrefix(ByVal str As String) As Boolean
        If str IsNot Nothing Then
            Dim pos As Integer = str.IndexOf("select ")
            If pos = -1 Then pos = str.IndexOf("case ")
            Return pos = -1
        Else
            Return True
        End If
    End Function

    ''' <summary>
    ''' Метод используется для подсчета кол-ва безымяных параметров в выражении
    ''' </summary>
    ''' <param name="stmt">Вырежение</param>
    ''' <returns>Кол-во безымянных параметров</returns>
    ''' <remarks></remarks>
    Public Function ExtractParamsCount(ByVal stmt As String) As Integer
        Dim pos As Integer = 0
        Dim cnt As Integer = 0

        If stmt IsNot Nothing Then

            Do
                pos = stmt.IndexOf("?", pos)
                If pos >= 0 Then
                    cnt += 1
                Else
                    Exit Do
                End If

                pos += 1
            Loop While True
        End If

        Return cnt
    End Function

    ''' <summary>
    ''' Сортирует словарь в соответствии с порядком ключей в коллекции
    ''' </summary>
    ''' <typeparam name="TKey">Ключ словаря</typeparam>
    ''' <typeparam name="TValue">Значение словаря</typeparam>
    ''' <param name="dic">Словарь</param>
    ''' <param name="model">Упорядоченная коллекция ключей</param>
    ''' <returns>Список пар ключ/значение из словаря, упорядоченный по коллекции <b>model</b></returns>
    ''' <exception cref="InvalidOperationException">Если ключ из словаря не найден в коллекции <b>model</b></exception>
    Public Function Sort(Of TKey, TValue)(ByVal dic As IDictionary(Of TKey, TValue), ByVal model() As TKey) As List(Of Pair(Of TKey, TValue))
        Dim l As New List(Of Pair(Of TKey, TValue))

        If dic IsNot Nothing Then
            Dim arr(model.Length - 1) As TValue
            For Each de As KeyValuePair(Of TKey, TValue) In dic

                Dim idx As Integer = Array.IndexOf(model, de.Key)

                If idx < 0 Then
                    Throw New InvalidOperationException("Unknown key " + Convert.ToString(de.Key))
                End If

                arr(idx) = de.Value
            Next

            For i As Integer = 0 To arr.Length - 1
                l.Add(New Pair(Of TKey, TValue)(model(i), arr(i)))
            Next
        End If

        Return l
    End Function

    ''' <summary>
    ''' Сравнение массива байт
    ''' </summary>
    ''' <param name="arr1">Первый массив</param>
    ''' <param name="arr2">Второй массив</param>
    ''' <returns><b>true</b> если массивы идентичны</returns>
    ''' <remarks></remarks>
    Public Function IsEqualByteArray(ByVal arr1() As Byte, ByVal arr2() As Byte) As Boolean
        If arr1 Is Nothing AndAlso arr2 Is Nothing Then
            Return True
        End If

        If (arr1 Is Nothing AndAlso arr2 IsNot Nothing) _
            OrElse (arr2 Is Nothing AndAlso arr1 IsNot Nothing) Then
            Return False
        End If

        If arr1.Length <> arr2.Length Then
            Return False
        End If

        For i As Integer = 0 To arr1.Length - 1
            Dim b1 As Byte = arr1(i)
            Dim b2 As Byte = arr2(i)
            If b1 <> b2 Then
                Return False
            End If
        Next

        Return True
    End Function

    ''' <summary>
    ''' Класс представляет собой результат склейки коллекции чисел
    ''' </summary>
    ''' <remarks></remarks>
    Public Class MergeResult
        Private _pairs As ICollection(Of Pair(Of Integer))
        Private _rest As ICollection(Of Integer)

        ''' <summary>
        ''' Конструктор класса
        ''' </summary>
        ''' <param name="pairs">Коллекция диапазонов чисел (от <see cref="Pair(Of Integer).First"/> до <see cref="Pair(Of Integer).Second"/>)</param>
        ''' <param name="rest">Остаток (числа сами по себе)</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal pairs As ICollection(Of Pair(Of Integer)), ByVal rest As ICollection(Of Integer))
            _pairs = pairs
            _rest = rest
        End Sub

        ''' <summary>
        ''' Диапазон чисел
        ''' </summary>
        ''' <returns>Коллекция диапазонов чисел (от <see cref="Pair(Of Integer).First"/> до <see cref="Pair(Of Integer).Second"/>)</returns>
        ''' <remarks></remarks>
        Public ReadOnly Property Pairs() As ICollection(Of Pair(Of Integer))
            Get
                Return _pairs
            End Get
        End Property

        ''' <summary>
        ''' Остаток
        ''' </summary>
        ''' <returns>Коллекция чисел</returns>
        ''' <remarks></remarks>
        Public ReadOnly Property Rest() As ICollection(Of Integer)
            Get
                Return _rest
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Cклейка коллекции чисел для оптимизации запросов
    ''' </summary>
    ''' <param name="ids">Коллекция чисел</param>
    ''' <param name="sort"><b>true</b> если коллекция <b>ids</b> уже упорядочена</param>
    ''' <returns>Экземпляр типа <see cref="MergeResult"/></returns>
    ''' <remarks>Метод выполняет оптимизацию коллекции чисел для уменьшения размер строки.
    ''' Используется для оптимизации условий в условии in (...). Например, вместо
    ''' in (1,2,3,4,5,6,7) получается between 1 and 7
    ''' </remarks>
    Public Function MergeIds(ByVal ids As Generic.List(Of Integer), ByVal sort As Boolean) As MergeResult
        If ids Is Nothing OrElse ids.Count = 0 Then
            Return Nothing
        End If

        If ids.Count = 1 Then
            Return New MergeResult(New Generic.List(Of Pair(Of Integer)), ids)
        End If

        If sort Then
            ids.Sort()
        End If

        Dim pairs As New Generic.List(Of Pair(Of Integer))
        Dim rest As New Generic.List(Of Integer)
        Dim start As Integer = 0
        For i As Integer = 1 To ids.Count - 1
            Dim d As Integer = ids(i) - ids(i - 1)
            If d = 1 Then
                Continue For
            ElseIf d > 1 Then
                If i - start > 1 Then
                    Dim p As New Pair(Of Integer)(ids(start), ids(i - 1))
                    pairs.Add(p)
                Else
                    rest.Add(ids(start))
                End If
                start = i
            Else
                Throw New ArgumentException("Collection of integer is invalid. Not sorted or countans duplicates")
            End If
        Next

        If start < ids.Count - 1 Then
            Dim p As New Pair(Of Integer)(ids(start), ids(ids.Count - 1))
            pairs.Add(p)
        Else
            rest.Add(ids(start))
        End If

        Return New MergeResult(pairs, rest)
    End Function

End Module

''' <summary>
''' Обертка над типом
''' </summary>
''' <typeparam name="T">Тип</typeparam>
''' <remarks>Необходима для устранения операций неявного приведения типов</remarks>
Public Class TypeWrap(Of T)
    Private _o As T

    ''' <summary>
    ''' Конструктор
    ''' </summary>
    ''' <param name="o">Экземпляр типа</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal o As T)
        _o = o
    End Sub

    ''' <summary>
    ''' Экземпляр типа
    ''' </summary>
    ''' <value>Устанавливаемое значение</value>
    ''' <returns>Установленое значение</returns>
    ''' <remarks></remarks>
    Public Property Value() As T
        Get
            Return _o
        End Get
        Set(ByVal value As T)
            _o = value
        End Set
    End Property

    ''' <summary>
    ''' Определение равенства объектов
    ''' </summary>
    ''' <param name="obj">Объект</param>
    ''' <returns><b>true</b> если объекты равны</returns>
    ''' <remarks></remarks>
    Public Overrides Function Equals(ByVal obj As Object) As Boolean
        Dim tw As TypeWrap(Of T) = TryCast(obj, TypeWrap(Of T))
        Return Equals(tw)
    End Function

    ''' <summary>
    ''' Типизированое определение равенства объектов
    ''' </summary>
    ''' <param name="obj">Объект</param>
    ''' <returns><b>true</b> если объекты равны</returns>
    ''' <remarks>Операция сравнение с типом Т дает <b>false</b></remarks>
    Public Overloads Function Equals(ByVal obj As TypeWrap(Of T)) As Boolean
        If obj IsNot Nothing Then
            Return Object.Equals(_o, obj._o)
        Else
            Return False
        End If
    End Function

    ''' <summary>
    ''' Преобразование типа в строку
    ''' </summary>
    ''' <returns>Строка</returns>
    ''' <remarks>Делегирует вызов внутренему объекту</remarks>
    Public Overrides Function ToString() As String
        If _o IsNot Nothing Then
            Return _o.ToString
        Else
            Return String.Empty
        End If
    End Function

    ''' <summary>
    ''' Преобразование в число
    ''' </summary>
    ''' <returns>Число</returns>
    ''' <remarks>Делегирует вызов внутренему объекту</remarks>
    Public Overrides Function GetHashCode() As Integer
        If _o IsNot Nothing Then
            Return _o.GetHashCode
        Else
            Return 1
        End If
    End Function
End Class

''' <summary>
''' Класс, повзволяющий точно замерять промежутки времени
''' </summary>
''' <remarks></remarks>
Public Class PerfCounter
    Private _start As Long

    ''' <summary>
    ''' The QueryPerformanceCounter function retrieves the current value of the high-resolution performance counter
    ''' </summary>
    ''' <param name="X">Variable that receives the current performance-counter value, in counts</param>
    ''' <returns>If the function succeeds, the return value is <b>true</b></returns>
    ''' <remarks>Делегация системному вызову</remarks>
    Declare Function QueryPerformanceCounter Lib "Kernel32" (ByRef X As Long) As Boolean
    ''' <summary>
    ''' The QueryPerformanceFrequency function retrieves the frequency of the high-resolution performance counter, if one exists. The frequency cannot change while the system is running
    ''' </summary>
    ''' <param name="X">variable that receives the current performance-counter frequency, in counts per second. If the installed hardware does not support a high-resolution performance counter, this parameter can be zero.</param>
    ''' <returns>If the function succeeds, the return value is <b>true</b></returns>
    ''' <remarks>Делегация системному вызову</remarks>
    Declare Function QueryPerformanceFrequency Lib "Kernel32" (ByRef X As Long) As Boolean

    ''' <summary>
    ''' Констуктор
    ''' </summary>
    ''' <remarks>Начала отсчета</remarks>
    Public Sub New()
        QueryPerformanceCounter(_start)
    End Sub

    ''' <summary>
    ''' Функция окончания отсчета времени
    ''' </summary>
    ''' <returns>Временой промежуток прошедщий с момента создания данного экземпляра</returns>
    ''' <remarks></remarks>
    Public Function GetTime() As TimeSpan
        Dim [end] As Long
        QueryPerformanceCounter([end])
        Dim f As Long
        QueryPerformanceFrequency(f)
        Return TimeSpan.FromSeconds(([end] - _start) / f)
    End Function
End Class

