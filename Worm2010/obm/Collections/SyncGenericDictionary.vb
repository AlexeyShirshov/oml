Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Namespace Collections

    <Serializable()> _
    Public Class SynchronizedDictionary(Of T)
        Implements System.Collections.Generic.IDictionary(Of Object, T), IDictionary

        Protected Class Enumerator
            Implements IEnumerator, IEnumerator(Of KeyValuePair(Of Object, T))

            'Private dic As Dictionary(Of Integer, T)
            Private list As New List(Of KeyValuePair(Of Object, T))
            Private idx As Integer

            Public Sub New(ByVal dic As Dictionary(Of Object, T))
                'Me.dic = dic
                SyncLock dic
                    list.AddRange(CType(dic, ICollection(Of KeyValuePair(Of Object, T))))
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

            Public ReadOnly Property Current() As System.Collections.Generic.KeyValuePair(Of Object, T) Implements System.Collections.Generic.IEnumerator(Of System.Collections.Generic.KeyValuePair(Of Object, T)).Current
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

        Private dic As New Dictionary(Of Object, T)

        Private ReadOnly Property collection() As ICollection(Of KeyValuePair(Of Object, T))
            Get
                Return CType(dic, ICollection(Of KeyValuePair(Of Object, T)))
            End Get
        End Property


        Private ReadOnly Property dictionary() As IDictionary
            Get
                Return CType(dic, IDictionary)
            End Get
        End Property

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Protected Sub Add1(ByVal item As System.Collections.Generic.KeyValuePair(Of Object, T)) Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Object, T)).Add
            collection.Add(item)
        End Sub

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub Clear() Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Object, T)).Clear
            collection.Clear()
        End Sub

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Function Contains(ByVal item As System.Collections.Generic.KeyValuePair(Of Object, T)) As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Object, T)).Contains
            Return collection.Contains(item)
        End Function

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub CopyTo(ByVal array() As System.Collections.Generic.KeyValuePair(Of Object, T), ByVal arrayIndex As Integer) Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Object, T)).CopyTo
            collection.CopyTo(array, arrayIndex)
        End Sub

        Public ReadOnly Property Count() As Integer Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Object, T)).Count
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return collection.Count
            End Get
        End Property

        Public ReadOnly Property IsReadOnly() As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Object, T)).IsReadOnly
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return collection.IsReadOnly
            End Get
        End Property

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Protected Function Remove1(ByVal item As System.Collections.Generic.KeyValuePair(Of Object, T)) As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of Object, T)).Remove
            Return collection.Remove(item)
        End Function

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Sub Add(ByVal key As Object, ByVal value As T) Implements System.Collections.Generic.IDictionary(Of Object, T).Add
            dic.Add(key, value)
        End Sub

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Function ContainsKey(ByVal key As Object) As Boolean Implements System.Collections.Generic.IDictionary(Of Object, T).ContainsKey
            Return dic.ContainsKey(key)
        End Function

        Default Public Property Item(ByVal key As Object) As T Implements System.Collections.Generic.IDictionary(Of Object, T).Item
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Get
                Return dic(key)
            End Get
            <MethodImpl(MethodImplOptions.Synchronized)> _
            Set(ByVal value As T)
                dic(key) = value
            End Set
        End Property

        Public ReadOnly Property Keys() As System.Collections.Generic.ICollection(Of Object) Implements System.Collections.Generic.IDictionary(Of Object, T).Keys
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
        Public Function Remove(ByVal key As Object) As Boolean Implements System.Collections.Generic.IDictionary(Of Object, T).Remove
            Return dic.Remove(key)
        End Function

        <MethodImpl(MethodImplOptions.Synchronized)> _
        Public Function TryGetValue(ByVal key As Object, ByRef value As T) As Boolean Implements System.Collections.Generic.IDictionary(Of Object, T).TryGetValue
            Return dic.TryGetValue(key, value)
        End Function

        Public ReadOnly Property Values() As System.Collections.Generic.ICollection(Of T) Implements System.Collections.Generic.IDictionary(Of Object, T).Values
            Get
                'Dim arr As New List(Of T)
                'SyncLock Me
                '    arr.AddRange(dic.Values)
                'End SyncLock
                'Return arr
                Return dic.Values
            End Get
        End Property

        Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of System.Collections.Generic.KeyValuePair(Of Object, T)) Implements System.Collections.Generic.IEnumerable(Of System.Collections.Generic.KeyValuePair(Of Object, T)).GetEnumerator
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