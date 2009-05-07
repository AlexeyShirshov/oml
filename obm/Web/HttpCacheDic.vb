Imports System.Collections
Imports System.Collections.Generic
Imports System.Web
Imports System.Runtime.CompilerServices
Imports Worm.Entities

Namespace Cache

    Friend Module DicKeys
        Private _cnt As Integer = 1

        Public Function [Get]() As Integer
            SyncLock GetType(DicKeys)
                Dim old As Integer = _cnt
                _cnt += 1
                Return old
            End SyncLock
        End Function
    End Module

    <Serializable()> _
    Public Class WebCacheDictionary(Of TValue)
        Implements System.Collections.Generic.IDictionary(Of String, TValue), IDictionary

        Private _keys As New Generic.List(Of String)
        Private _abs_expiration As Date
        Private _sld_expiration As TimeSpan
        Private _priority As Caching.CacheItemPriority
        <NonSerialized()> _
        Private _dep As Caching.CacheDependency
        Private _remove_del As Caching.CacheItemRemovedCallback
        Private _name2 As String

        Public Sub New()
            _abs_expiration = Caching.Cache.NoAbsoluteExpiration
            _sld_expiration = Caching.Cache.NoSlidingExpiration
            _priority = Caching.CacheItemPriority.Default
        End Sub

        Public Sub New(ByVal absolute_expiration As Date, ByVal sliding_expiration As TimeSpan, _
            ByVal priority As Caching.CacheItemPriority, ByVal dependency As Caching.CacheDependency)
            _abs_expiration = absolute_expiration
            _sld_expiration = sliding_expiration
            _priority = priority
            _dep = dependency
        End Sub

        Protected ReadOnly Property _name() As String
            Get
                If String.IsNullOrEmpty(_name2) Then
                    SyncLock GetType(WebCacheDictionary(Of ))
                        If String.IsNullOrEmpty(_name2) Then
                            _name2 = "Worm.Cache." & DicKeys.Get
                        End If
                    End SyncLock
                End If
                Return _name2
            End Get
        End Property

        Protected Function GetKey(ByVal key As String) As String
            'Return _name & ":" & (key Or (_code And Not _mask)).ToString
            Return _name & ":" & key
        End Function

        Protected Function GetID(ByVal key As String) As String
            Return key.Remove(0, _name.Length + 1)
        End Function

        Protected ReadOnly Property Cache() As System.Web.Caching.Cache
            Get
                'Dim ctx As HttpContext = System.Web.HttpContext.Current
                'If ctx IsNot Nothing Then
                '    Return ctx.Cache
                'End If
                'Return Nothing
                Return HttpRuntime.Cache
            End Get
        End Property

        Public Property CacheItemRemovedCallback() As Caching.CacheItemRemovedCallback
            Get
                Return _remove_del
            End Get
            Set(ByVal value As Caching.CacheItemRemovedCallback)
                _remove_del = value
            End Set
        End Property

        Protected Class Enumerator
            Implements IEnumerator, IEnumerator(Of KeyValuePair(Of String, TValue))

            'Private dic As Dictionary(Of Integer, T)
            Private list As New List(Of KeyValuePair(Of String, TValue))
            Private idx As Integer

            Public Sub New(ByVal dic As IDictionary(Of String, TValue))
                'Me.dic = dic
                SyncLock dic
                    list.AddRange(CType(dic, ICollection(Of KeyValuePair(Of String, TValue))))
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

            Public ReadOnly Property Current() As System.Collections.Generic.KeyValuePair(Of String, TValue) Implements System.Collections.Generic.IEnumerator(Of System.Collections.Generic.KeyValuePair(Of String, TValue)).Current
                Get
                    If idx = -1 Then Throw New InvalidOperationException("You should call MoveNext first.")
                    If idx = -2 Then Throw New InvalidOperationException("You are at the end of the collection")
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
            Public Overloads Sub Dispose() Implements IDisposable.Dispose
                Dispose(True)
                GC.SuppressFinalize(Me)
            End Sub

            Protected Overrides Sub Finalize()
                Dispose(False)
                MyBase.Finalize()
            End Sub
#End Region

        End Class

        Private Shared Sub AddKey(ByRef keys As Generic.List(Of String), ByVal key As String)
            Dim idx As Integer = keys.BinarySearch(key)
            If idx >= 0 Then
                Throw New ArgumentOutOfRangeException(String.Format("Key {0} already presents in list.", key))
            Else
                keys.Insert(Not idx, key)
            End If
        End Sub

        Private Shared Sub RemoveKey(ByRef keys As Generic.List(Of String), ByVal key As String)
            Dim idx As Integer = keys.BinarySearch(key)
            If idx >= 0 Then
                '    Throw New KeyNotFoundException("Key in Cache not found.")
                'Else
                keys.RemoveAt(idx)
            End If
        End Sub

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Protected Overridable Sub CacheItemRemovedCallback1(ByVal key As String, ByVal value As Object, _
            ByVal reason As Caching.CacheItemRemovedReason)
            '_keys.Remove(key)
            RemoveKey(_keys, key)
            If _remove_del IsNot Nothing Then
                _remove_del(key, value, reason)
            End If
        End Sub

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub AddItem(ByVal item As System.Collections.Generic.KeyValuePair(Of String, TValue)) Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of String, TValue)).Add
            'If IsNothing(item) Then
            '    Throw New ArgumentNullException("item")
            'End If

            'Dim realKey As String = GetKey(item.Key)
            'Dim o As Object = Cache.Add(realKey, item.Value, _dep, _abs_expiration, _sld_expiration, _
            '    _priority, AddressOf CacheItemRemovedCallback1)
            'If IsNothing(o) Then
            '    AddKey(_keys, realKey)
            'Else
            '    Throw New ArgumentException("The key is already in collection: " & item.Key)
            'End If
            AddItem(item, False)
        End Sub

        Protected Sub AddItem(ByVal item As System.Collections.Generic.KeyValuePair(Of String, TValue), ByVal asSet As Boolean)
            If IsNothing(item) Then
                Throw New ArgumentNullException("item")
            End If

            Dim realKey As String = GetKey(item.Key)
            Dim o As Object = Cache.Add(realKey, item.Value, _dep, _abs_expiration, _sld_expiration, _
                _priority, AddressOf CacheItemRemovedCallback1)
            If Not asSet Then
                If IsNothing(o) Then
                    AddKey(_keys, realKey)
                Else
                    Throw New ArgumentException("The key is already in collection: " & item.Key)
                End If
            Else
                Dim idx As Integer = _keys.BinarySearch(realKey)
                If idx >= 0 Then
                    _keys(idx) = realKey
                Else
                    _keys.Insert(Not idx, realKey)
                End If
            End If
        End Sub

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub Clear() Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of String, TValue)).Clear
            For Each key As String In New Generic.List(Of String)(_keys)
                Cache.Remove(key)
            Next
            _keys.Clear()
        End Sub

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Function ContainsItem(ByVal item As System.Collections.Generic.KeyValuePair(Of String, TValue)) As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of String, TValue)).Contains
            If IsNothing(item) Then
                Throw New ArgumentNullException("item")
            End If

            Dim v As TValue = Me.Item(item.Key)
            Return v IsNot Nothing AndAlso v.Equals(item.Value)
        End Function

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub CopyTo(ByVal array() As System.Collections.Generic.KeyValuePair(Of String, TValue), ByVal arrayIndex As Integer) Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of String, TValue)).CopyTo
            Dim l As New Generic.List(Of KeyValuePair(Of String, TValue))
            For Each key As String In _keys
                Dim rk As String = GetID(key)
                l.Add(New KeyValuePair(Of String, TValue)(rk, Me.Item(rk)))
            Next
            l.CopyTo(array, arrayIndex)
        End Sub

        Public ReadOnly Property Count() As Integer Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of String, TValue)).Count
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return _keys.Count
            End Get
        End Property

        Public ReadOnly Property IsReadOnly() As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of String, TValue)).IsReadOnly
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return False
            End Get
        End Property

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Function RemoveItem(ByVal item As System.Collections.Generic.KeyValuePair(Of String, TValue)) As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of String, TValue)).Remove
            If IsNothing(item) Then
                Throw New ArgumentNullException("item")
            End If

            Dim realKey As String = GetKey(item.Key)
            Cache.Remove(realKey)
            RemoveKey(_keys, realKey)
            Return True
        End Function

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub Add(ByVal key As String, ByVal value As TValue) Implements System.Collections.Generic.IDictionary(Of String, TValue).Add
            If IsNothing(key) Then
                Throw New ArgumentNullException("key")
            End If

            Dim realKey As String = GetKey(key)
            Dim o As Object = Cache.Add(realKey, value, _dep, _abs_expiration, _sld_expiration, _
                _priority, AddressOf CacheItemRemovedCallback1)
            If IsNothing(o) Then
                AddKey(_keys, realKey)
            Else
                Throw New ArgumentException("The key is already in collection: " & key)
            End If
        End Sub

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Function ContainsKey(ByVal key As String) As Boolean Implements System.Collections.Generic.IDictionary(Of String, TValue).ContainsKey
            If IsNothing(key) Then
                Throw New ArgumentNullException("key")
            End If

            Dim realKey As String = GetKey(key)
            Dim exists As Boolean = Cache(realKey) IsNot Nothing
            'System.Diagnostics.Debug.Assert(r OrElse Not _keys.Contains(key))
            If Not exists Then
                Dim pos As Integer = _keys.BinarySearch(realKey)
                If pos >= 0 Then
                    _keys.RemoveAt(pos)
                End If
            End If
            Return exists
        End Function

        Default Public Property Item(ByVal key As String) As TValue Implements System.Collections.Generic.IDictionary(Of String, TValue).Item
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                If IsNothing(key) Then
                    Throw New ArgumentNullException("key")
                End If

                Dim o As Object = Cache(GetKey(key))
                If o Is Nothing Then
                    Throw New KeyNotFoundException(String.Format("Key {0} not found", key))
                Else
                    Return CType(o, TValue)
                End If
            End Get
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Set(ByVal value As TValue)
                If IsNothing(key) Then
                    Throw New ArgumentNullException("key")
                End If

                'Dim realKey As String = GetKey(key)
                'If Not ContainsKey(realKey) Then
                AddItem(New KeyValuePair(Of String, TValue)(key, value), True)
                'Else
                'Cache(realKey) = value
                'End If
            End Set
        End Property

        Public ReadOnly Property Keys() As System.Collections.Generic.ICollection(Of String) Implements System.Collections.Generic.IDictionary(Of String, TValue).Keys
            Get
                Dim l As New Generic.List(Of String)
                For Each key As String In _keys
                    l.Add(GetID(key))
                Next
                Return l
            End Get
        End Property

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Function Remove(ByVal key As String) As Boolean Implements System.Collections.Generic.IDictionary(Of String, TValue).Remove
            If IsNothing(key) Then
                Throw New ArgumentNullException("key")
            End If
            Dim realKey As String = GetKey(key)
            Cache.Remove(realKey)
            RemoveKey(_keys, realKey)
            Return True
        End Function

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Function TryGetValue(ByVal key As String, ByRef value As TValue) As Boolean Implements System.Collections.Generic.IDictionary(Of String, TValue).TryGetValue
            If IsNothing(key) Then
                Throw New ArgumentNullException("key")
            End If

            Dim o As Object = Cache(GetKey(key))
            If o IsNot Nothing Then
                value = CType(o, TValue)
                Return True
            End If
            Return False
        End Function

        Public ReadOnly Property Values() As System.Collections.Generic.ICollection(Of TValue) Implements System.Collections.Generic.IDictionary(Of String, TValue).Values
            Get
                Dim arr As New List(Of TValue)
                For Each key As String In _keys
                    arr.Add(Me.Item(GetID(key)))
                Next
                Return arr
            End Get
        End Property

        Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of System.Collections.Generic.KeyValuePair(Of String, TValue)) Implements System.Collections.Generic.IEnumerable(Of System.Collections.Generic.KeyValuePair(Of String, TValue)).GetEnumerator
            Return New Enumerator(Me)
        End Function

        Protected Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return New Enumerator(Me)
        End Function

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Protected Sub CopyTo1(ByVal array As System.Array, ByVal index As Integer) Implements System.Collections.ICollection.CopyTo
            Dim l As New ArrayList
            For Each key As String In _keys
                Dim rk As String = GetID(key)
                l.Add(New DictionaryEntry(rk, Me.Item(rk)))
            Next
            l.CopyTo(array, index)
        End Sub

        Protected ReadOnly Property Count1() As Integer Implements System.Collections.ICollection.Count
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
                Return Me
            End Get
        End Property

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Protected Sub Add2(ByVal key As Object, ByVal value As Object) Implements System.Collections.IDictionary.Add
            Add(CStr(key), CType(value, TValue))
        End Sub

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Protected Sub Clear1() Implements System.Collections.IDictionary.Clear
            Me.Clear()
        End Sub

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Protected Function Contains1(ByVal key As Object) As Boolean Implements System.Collections.IDictionary.Contains
            Return Me.ContainsKey(CStr(key))
        End Function

        Protected Function GetEnumerator2() As System.Collections.IDictionaryEnumerator Implements System.Collections.IDictionary.GetEnumerator
            Throw New NotImplementedException
            'Return Cache.GetEnumerator
        End Function

        Public ReadOnly Property IsFixedSize() As Boolean Implements System.Collections.IDictionary.IsFixedSize
            Get
                Return False
            End Get
        End Property

        Protected ReadOnly Property IsReadOnly1() As Boolean Implements System.Collections.IDictionary.IsReadOnly
            Get
                Return False
            End Get
        End Property

        Private Overloads Property Item1(ByVal key As Object) As Object Implements System.Collections.IDictionary.Item
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Dim o As TValue = Nothing
                TryGetValue(CStr(key), o)
                Return o
            End Get
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Set(ByVal value As Object)
                Me(CStr(key)) = CType(value, TValue)
            End Set
        End Property

        Protected ReadOnly Property Keys1() As System.Collections.ICollection Implements System.Collections.IDictionary.Keys
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return CType(Me.Keys, System.Collections.ICollection)
            End Get
        End Property

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Protected Sub Remove2(ByVal key As Object) Implements System.Collections.IDictionary.Remove
            Me.Remove(CStr(key))
        End Sub

        Protected ReadOnly Property Values1() As System.Collections.ICollection Implements System.Collections.IDictionary.Values
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return CType(Me.Values, ICollection)
            End Get
        End Property
    End Class

    Public Class WebCacheDictionaryPolicy
        Public AbsoluteExpiration As Date
        Public SlidingExpiration As TimeSpan
        Public Priority As Caching.CacheItemPriority
        Public Dependency As Caching.CacheDependency

        Protected Sub New()

        End Sub

        Public Shared Function CreateDefault() As WebCacheDictionaryPolicy
            Dim dp As New WebCacheDictionaryPolicy
            With dp
                .AbsoluteExpiration = Caching.Cache.NoAbsoluteExpiration
                .SlidingExpiration = Caching.Cache.NoSlidingExpiration
                .Priority = Caching.CacheItemPriority.Default
                .Dependency = Nothing
            End With
            Return dp
        End Function
    End Class

    <Serializable()> _
    Public Class WebCacheEntityDictionary(Of TValue As ICachedEntity)
        Implements IDictionary(Of Object, TValue), IDictionary

        'Private _mask As Integer
        'Private _code As Integer
        Private _dic As WebCacheDictionary(Of TValue)

        <NonSerialized()> _
        Private _mc As CacheBase
        'Private _name As String = Guid.NewGuid.ToString

        Public Sub New(ByVal mediaCache As OrmCache)
            '_mask = mask
            '_code = code
            _dic = New WebCacheDictionary(Of TValue)
            _mc = mediaCache

            _dic.CacheItemRemovedCallback = AddressOf CacheItemRemovedCallback1
        End Sub

        Public Sub New(ByVal mediaCache As CacheBase, _
            ByVal absolute_expiration As Date, ByVal sliding_expiration As TimeSpan, _
            ByVal priority As Caching.CacheItemPriority, ByVal dependency As Caching.CacheDependency)

            '_mask = mask
            '_code = code
            _dic = New WebCacheDictionary(Of TValue)(absolute_expiration, sliding_expiration, priority, dependency)
            _mc = mediaCache

            _dic.CacheItemRemovedCallback = AddressOf CacheItemRemovedCallback1
        End Sub

        'Protected Function GetKey(ByVal key As Object) As String
        '    Return key.ToString
        'End Function

        Protected Function GetKey(ByVal key As Object) As String
            Return key.ToString
        End Function

        Protected Function GetID(ByVal key As String) As Integer
            Return CInt(key)
        End Function

        Protected Function GetPair(ByVal item As KeyValuePair(Of Object, TValue)) As KeyValuePair(Of String, TValue)
            If IsNothing(item) Then
                Throw New ArgumentNullException("item")
            End If

            Return New KeyValuePair(Of String, TValue)(GetKey(item.Key), item.Value)
        End Function

        Protected Overridable Sub CacheItemRemovedCallback1(ByVal key As String, ByVal value As Object, _
            ByVal reason As Caching.CacheItemRemovedReason)
            _mc.RegisterRemoval(CType(value, KeyEntity), Nothing, Nothing)
        End Sub

        Protected ReadOnly Property collection() As ICollection(Of KeyValuePair(Of String, TValue))
            Get
                Return CType(_dic, Global.System.Collections.Generic.ICollection(Of KeyValuePair(Of String, TValue)))
            End Get
        End Property

        Protected ReadOnly Property dictionary() As IDictionary
            Get
                Return _dic
            End Get
        End Property

        Public Sub AddItem(ByVal item As System.Collections.Generic.KeyValuePair(Of Object, TValue)) Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Object, TValue)).Add
            collection.Add(GetPair(item))
        End Sub

        Public Sub Clear() Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Object, TValue)).Clear
            collection.Clear()
        End Sub

        Public Function ContainsItem(ByVal item As System.Collections.Generic.KeyValuePair(Of Object, TValue)) As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Object, TValue)).Contains
            collection.Contains(GetPair(item))
        End Function

        Public Sub CopyTo(ByVal array() As System.Collections.Generic.KeyValuePair(Of Object, TValue), ByVal arrayIndex As Integer) Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Object, TValue)).CopyTo
            Throw New NotImplementedException()
        End Sub

        Protected ReadOnly Property Count1() As Integer Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Object, TValue)).Count
            Get
                Return collection.Count
            End Get
        End Property

        Protected ReadOnly Property IsReadOnly() As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Object, TValue)).IsReadOnly
            Get
                Return collection.IsReadOnly
            End Get
        End Property

        Public Function RemoveItem(ByVal item As System.Collections.Generic.KeyValuePair(Of Object, TValue)) As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Object, TValue)).Remove
            Return collection.Remove(GetPair(item))
        End Function

        Public Sub Add(ByVal key As Object, ByVal value As TValue) Implements System.Collections.Generic.IDictionary(Of Object, TValue).Add
            _dic.Add(GetKey(key), value)
        End Sub

        Public Function ContainsKey(ByVal key As Object) As Boolean Implements System.Collections.Generic.IDictionary(Of Object, TValue).ContainsKey
            Return _dic.ContainsKey(GetKey(key))
        End Function

        Default Public Property Item(ByVal key As Object) As TValue Implements System.Collections.Generic.IDictionary(Of Object, TValue).Item
            Get
                Return _dic.Item(GetKey(key))
            End Get
            Set(ByVal value As TValue)
                _dic(GetKey(key)) = value
            End Set
        End Property

        Public ReadOnly Property Keys() As System.Collections.Generic.ICollection(Of Object) Implements System.Collections.Generic.IDictionary(Of Object, TValue).Keys
            Get
                Dim l As New Generic.List(Of Object)
                For Each k As String In _dic.Keys
                    l.Add(GetID(k))
                Next
                Return l
            End Get
        End Property

        Public Function Remove(ByVal key As Object) As Boolean Implements System.Collections.Generic.IDictionary(Of Object, TValue).Remove
            Return _dic.Remove(GetKey(key))
        End Function

        Public Function TryGetValue(ByVal key As Object, ByRef value As TValue) As Boolean Implements System.Collections.Generic.IDictionary(Of Object, TValue).TryGetValue
            Return _dic.TryGetValue(GetKey(key), value)
        End Function

        Public ReadOnly Property Values() As System.Collections.Generic.ICollection(Of TValue) Implements System.Collections.Generic.IDictionary(Of Object, TValue).Values
            Get
                Return _dic.Values
            End Get
        End Property

        Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of System.Collections.Generic.KeyValuePair(Of Object, TValue)) Implements System.Collections.Generic.IEnumerable(Of System.Collections.Generic.KeyValuePair(Of Object, TValue)).GetEnumerator
            Throw New NotImplementedException
        End Function

        Protected Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Throw New NotImplementedException
        End Function

        Protected Sub CopyTo1(ByVal array As System.Array, ByVal index As Integer) Implements System.Collections.ICollection.CopyTo
            Throw New NotImplementedException
        End Sub

        Protected ReadOnly Property Count() As Integer Implements System.Collections.ICollection.Count
            Get
                Return dictionary.Count
            End Get
        End Property

        Public ReadOnly Property IsSynchronized() As Boolean Implements System.Collections.ICollection.IsSynchronized
            Get
                Return dictionary.IsSynchronized
            End Get
        End Property

        Public ReadOnly Property SyncRoot() As Object Implements System.Collections.ICollection.SyncRoot
            Get
                Return dictionary.SyncRoot
            End Get
        End Property

        Protected Sub Add2(ByVal key As Object, ByVal value As Object) Implements System.Collections.IDictionary.Add
            dictionary.Add(GetKey(key), value)
        End Sub

        Protected Sub Clear1() Implements System.Collections.IDictionary.Clear
            dictionary.Clear()
        End Sub

        Protected Function Contains1(ByVal key As Object) As Boolean Implements System.Collections.IDictionary.Contains
            Return dictionary.Contains(GetKey(key))
        End Function

        Protected Function GetEnumerator2() As System.Collections.IDictionaryEnumerator Implements System.Collections.IDictionary.GetEnumerator
            Return dictionary.GetEnumerator
        End Function

        Public ReadOnly Property IsFixedSize() As Boolean Implements System.Collections.IDictionary.IsFixedSize
            Get
                Return dictionary.IsFixedSize
            End Get
        End Property

        Public ReadOnly Property IsReadOnly1() As Boolean Implements System.Collections.IDictionary.IsReadOnly
            Get
                Return dictionary.IsReadOnly
            End Get
        End Property

        Private Overloads Property Item1(ByVal key As Object) As Object Implements System.Collections.IDictionary.Item
            Get
                Return dictionary.Item(GetKey(key))
            End Get
            Set(ByVal value As Object)
                dictionary(GetKey(key)) = value
            End Set
        End Property

        Protected ReadOnly Property Keys1() As System.Collections.ICollection Implements System.Collections.IDictionary.Keys
            Get
                Return CType(Me.Keys, System.Collections.ICollection)
            End Get
        End Property

        Protected Sub Remove2(ByVal key As Object) Implements System.Collections.IDictionary.Remove
            dictionary.Remove(GetKey(key))
        End Sub

        Protected ReadOnly Property Values1() As System.Collections.ICollection Implements System.Collections.IDictionary.Values
            Get
                Return dictionary.Values
            End Get
        End Property
    End Class

End Namespace