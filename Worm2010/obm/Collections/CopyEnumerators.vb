Imports System.Collections.Generic

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

End Namespace
