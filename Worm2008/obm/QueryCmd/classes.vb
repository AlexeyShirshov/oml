Imports Worm.Entities
Imports Worm.Entities.Meta
Imports System.Collections.Generic

Namespace Query
    Public Interface IExecutor

        Function ExecEntity(Of ReturnType As {_IEntity})( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType)

        Function ExecEntity(Of CreateType As {_IEntity, New}, ReturnType As {_IEntity})( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType)

        Function Exec(Of ReturnType As _ICachedEntity)( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType)

        Function Exec(Of CreateType As {_ICachedEntity, New}, ReturnType As _ICachedEntity)( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType)

        Function ExecSimple(Of CreateType As {_ICachedEntity, New}, ReturnType)( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As IList(Of ReturnType)

        Function ExecSimple(Of ReturnType)( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As IList(Of ReturnType)

        Sub Reset(Of CreateType As {_ICachedEntity, New}, ReturnType As _ICachedEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd)
        Sub Reset(Of ReturnType As _ICachedEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd)

        Sub ResetEntity(Of ReturnType As _IEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd)
        Sub ResetEntity(Of CreateType As {_IEntity, New}, ReturnType As _IEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd)

    End Interface

    Public Interface ICreateQueryCmd
        Function Create(ByVal table As SourceFragment) As QueryCmd

        Function Create(ByVal selectType As Type) As QueryCmd

        Function CreateByEntityName(ByVal entityName As String) As QueryCmd

        Function Create(ByVal obj As IKeyEntity) As QueryCmd

        Function Create(ByVal obj As IKeyEntity, ByVal key As String) As QueryCmd

        Function Create(ByVal name As String, ByVal table As SourceFragment) As QueryCmd

        Function Create(ByVal name As String, ByVal selectType As Type) As QueryCmd

        Function CreateByEntityName(ByVal name As String, ByVal entityName As String) As QueryCmd

        Function Create(ByVal name As String, ByVal obj As IKeyEntity) As QueryCmd

        Function Create(ByVal name As String, ByVal obj As IKeyEntity, ByVal key As String) As QueryCmd
    End Interface

    Public Class Top
        Private _perc As Boolean
        Private _ties As Boolean
        Private _n As Integer

        Public Sub New(ByVal n As Integer)
            _n = n
        End Sub

        Public Sub New(ByVal n As Integer, ByVal percent As Boolean)
            MyClass.New(n)
            _perc = percent
        End Sub

        Public Sub New(ByVal n As Integer, ByVal percent As Boolean, ByVal ties As Boolean)
            MyClass.New(n, percent)
            _ties = ties
        End Sub

        Public ReadOnly Property Percent() As Boolean
            Get
                Return _perc
            End Get
        End Property

        Public ReadOnly Property Ties() As Boolean
            Get
                Return _ties
            End Get
        End Property

        Public ReadOnly Property Count() As Integer
            Get
                Return _n
            End Get
        End Property

        Public Function GetDynamicKey() As String
            Return "-top-" & _n.ToString & "-"
        End Function

        Public Function GetStaticKey() As String
            Return "-top-"
        End Function
    End Class

    Public Class Paging
        Public Start As Integer
        Public Length As Integer

        Public Sub New()
        End Sub

        Public Sub New(ByVal start As Integer, ByVal length As Integer)
            Me.Start = start
            Me.Length = length
        End Sub
    End Class

    Public Class RevQueryIterator
        Implements IEnumerator(Of QueryCmd), IEnumerable(Of QueryCmd)

        Private _q As QueryCmd
        Private _c As QueryCmd

        Public Sub New(ByVal query As QueryCmd)
            _q = query
        End Sub

        Public ReadOnly Property Current() As QueryCmd Implements System.Collections.Generic.IEnumerator(Of QueryCmd).Current
            Get
                Return _c
            End Get
        End Property

        Private ReadOnly Property _Current() As Object Implements System.Collections.IEnumerator.Current
            Get
                Return Current
            End Get
        End Property

        Public Function MoveNext() As Boolean Implements System.Collections.IEnumerator.MoveNext
            If _c Is Nothing Then
                _c = _q
            ElseIf _c.FromClaus IsNot Nothing Then
                _c = _c.FromClaus.Query
            End If
            Return _c IsNot Nothing
        End Function

        Public Sub Reset() Implements System.Collections.IEnumerator.Reset
            _c = Nothing
        End Sub

#Region " IDisposable Support "
        Private disposedValue As Boolean = False        ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not Me.disposedValue Then
                If disposing Then
                    ' TODO: free other state (managed objects).
                End If

                ' TODO: free your own state (unmanaged objects).
                ' TODO: set large fields to null.
            End If
            Me.disposedValue = True
        End Sub

        ' This code added by Visual Basic to correctly implement the disposable pattern.
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
#End Region

        Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of QueryCmd) Implements System.Collections.Generic.IEnumerable(Of QueryCmd).GetEnumerator
            Return Me
        End Function

        Private Function _GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return GetEnumerator()
        End Function
    End Class

    Public Class QueryIterator
        Implements IEnumerable(Of QueryCmd)

        Private _l As New List(Of QueryCmd)

        Public Sub New(ByVal query As QueryCmd)
            For Each q As QueryCmd In New RevQueryIterator(query)
                _l.Insert(0, q)
            Next
        End Sub

        'Public ReadOnly Property Current() As QueryCmd Implements System.Collections.Generic.IEnumerator(Of QueryCmd).Current
        '    Get
        '        Return _l.GetEnumerator.Current
        '    End Get
        'End Property

        'Private ReadOnly Property _Current() As Object Implements System.Collections.IEnumerator.Current
        '    Get
        '        Return Current
        '    End Get
        'End Property

        'Public Function MoveNext() As Boolean Implements System.Collections.IEnumerator.MoveNext
        '    If _c Is Nothing Then
        '        _c = _q
        '    Else
        '        _c = _c.OuterQuery
        '    End If
        '    Return _c IsNot Nothing
        'End Function

        'Public Sub Reset() Implements System.Collections.IEnumerator.Reset
        '    _c = Nothing
        'End Sub

        '#Region " IDisposable Support "
        '        Private disposedValue As Boolean = False        ' To detect redundant calls

        '        ' IDisposable
        '        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        '            If Not Me.disposedValue Then
        '                If disposing Then
        '                    ' TODO: free other state (managed objects).
        '                End If

        '                ' TODO: free your own state (unmanaged objects).
        '                ' TODO: set large fields to null.
        '            End If
        '            Me.disposedValue = True
        '        End Sub

        '        ' This code added by Visual Basic to correctly implement the disposable pattern.
        '        Public Sub Dispose() Implements IDisposable.Dispose
        '            ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        '            Dispose(True)
        '            GC.SuppressFinalize(Me)
        '        End Sub
        '#End Region

        Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of QueryCmd) Implements System.Collections.Generic.IEnumerable(Of QueryCmd).GetEnumerator
            Return _l.GetEnumerator
        End Function

        Private Function _GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return GetEnumerator()
        End Function
    End Class

    <Serializable()> _
    Public Class QueryCmdException
        Inherits System.Exception

        Private _cmd As QueryCmd

        Public ReadOnly Property QueryCommand() As QueryCmd
            Get
                Return _cmd
            End Get
        End Property

        Public Sub New(ByVal message As String, ByVal cmd As QueryCmd)
            MyBase.New(message)
            _cmd = cmd
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

End Namespace