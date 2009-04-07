Imports System.Collections.Generic

Namespace Collections
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
#If DebugLocks Then
            Return New CSScopeMgr_Debug(Me, "d:\temp")
#Else
            Return New CSScopeMgr(Me)
#End If
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

        Public ReadOnly Property Values() As System.Collections.Generic.ICollection(Of TItem) Implements System.Collections.Generic.IDictionary(Of TItemKey, TItem).Values
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
                            Dim i As Integer = 0
                            Do While enumerator1.MoveNext
                                Dim c As TItem = enumerator1.Current
                                If i = index Then
                                    Return c
                                End If
                                i += 1
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

End Namespace