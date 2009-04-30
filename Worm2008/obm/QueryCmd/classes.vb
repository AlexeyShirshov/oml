Imports Worm.Entities
Imports Worm.Entities.Meta
Imports System.Collections.Generic

Namespace Query
    Public Interface IExecutor
        Class GetCacheItemEventArgs
            Inherits EventArgs

            Private _notCreated As Boolean
            Public Property Created() As Boolean
                Get
                    Return Not _notCreated
                End Get
                Set(ByVal value As Boolean)
                    _notCreated = Not value
                End Set
            End Property
        End Class

        Event OnGetCacheItem(ByVal sender As IExecutor, ByVal args As GetCacheItemEventArgs)
        Event OnRestoreDefaults(ByVal sender As IExecutor, ByVal mgr As OrmManager, ByVal args As EventArgs)

        Function ExecEntity(Of ReturnType As {_IEntity})( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType)

        Function ExecEntity(Of CreateType As {_IEntity, New}, ReturnType As {_IEntity})( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType)

        Function Exec(Of ReturnType As _ICachedEntity)( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType)

        Function Exec(Of CreateType As {_ICachedEntity, New}, ReturnType As _ICachedEntity)( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType)

        Function Exec(ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadonlyMatrix

        'Function ExecSimple(Of CreateType As {_ICachedEntity, New}, ReturnType)( _
        '    ByVal mgr As OrmManager, ByVal query As QueryCmd) As IList(Of ReturnType)

        Function ExecSimple(Of ReturnType)( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As IList(Of ReturnType)

        Sub RenewCache(ByVal mgr As OrmManager, ByVal query As QueryCmd, ByVal v As Boolean)
        Sub ClearCache(ByVal mgr As OrmManager, ByVal query As QueryCmd)
        Sub ResetObjects(ByVal mgr As OrmManager, ByVal query As QueryCmd)

        ReadOnly Property IsInCache(ByVal mgr As OrmManager, ByVal query As QueryCmd) As Boolean

    End Interface

    Public Interface ICreateQueryCmd
        'Function Create(ByVal table As SourceFragment) As QueryCmd

        'Function Create(ByVal selectType As Type) As QueryCmd

        'Function CreateByEntityName(ByVal entityName As String) As QueryCmd

        Function Create(ByVal rel As Relation) As RelationCmd

        Function Create(ByVal desc As RelationDesc) As RelationCmd

        Function Create(ByVal obj As IKeyEntity, ByVal en As EntityUnion) As RelationCmd

        Function Create(ByVal obj As IKeyEntity, ByVal en As EntityUnion, ByVal key As String) As RelationCmd

        'Function Create(ByVal name As String, ByVal table As SourceFragment) As QueryCmd

        'Function Create(ByVal name As String, ByVal selectType As Type) As QueryCmd

        'Function CreateByEntityName(ByVal name As String, ByVal entityName As String) As QueryCmd

        Function Create() As QueryCmd

        Function Create(ByVal name As String) As QueryCmd

        Function Create(ByVal name As String, ByVal obj As IKeyEntity, ByVal en As EntityUnion) As RelationCmd

        Function Create(ByVal name As String, ByVal obj As IKeyEntity, ByVal en As EntityUnion, ByVal key As String) As RelationCmd
    End Interface

    Public Interface IExecutionContext
        Function GetEntitySchema(ByVal mpe As ObjectMappingEngine, ByVal t As Type) As IEntitySchema
        Sub ReplaceSchema(ByVal mpe As ObjectMappingEngine, ByVal t As Type, ByVal newMap As OrmObjectIndex)
        Function GetFieldColumnMap(ByVal oschema As IEntitySchema, ByVal t As Type) As Collections.IndexedCollection(Of String, MapField2Column)
    End Interface

    <Serializable()> _
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

    <Serializable()> _
    Public Structure Paging
        Private _start As Integer
        Public ReadOnly Property Start() As Integer
            Get
                Return _start
            End Get
            'Set(ByVal value As Integer)
            '    _start = value
            '    _ne = True
            'End Set
        End Property

        Private _len As Integer
        Public ReadOnly Property Length() As Integer
            Get
                Return _len
            End Get
            'Set(ByVal value As Integer)
            '    _len = value
            '    _ne = True
            'End Set
        End Property

        Private _ne As Boolean

        Public Sub New(ByVal start As Integer, ByVal length As Integer)
            Me._start = start
            Me._len = length
            _ne = True
        End Sub

        Public Sub New(ByVal start As Integer, ByVal length As Integer, ByVal optimizeCache As Boolean)
            Me._start = start
            Me._len = length
            _ne = True
            Me._oc = optimizeCache
        End Sub

        Private _oc As Boolean
        Public ReadOnly Property OptimizeCache() As Boolean
            Get
                Return _oc
            End Get
        End Property

        Public ReadOnly Property IsEmpty() As Boolean
            Get
                Return Not _ne
            End Get
        End Property
    End Structure

    Public Class InnerQueryIterator
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
            ElseIf _c.FromClause IsNot Nothing Then
                _c = _c.FromClause.Query
            ElseIf _c.FromClause Is Nothing Then
                _c = Nothing
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

    Public Class StmtQueryIterator
        Implements IEnumerable(Of QueryCmd)

        Private _l As New List(Of QueryCmd)

        Public Sub New(ByVal root As QueryCmd)
            _l.Add(root)
            If root.Unions IsNot Nothing Then
                For Each p As Pair(Of Boolean, QueryCmd) In root.Unions
                    Dim q As QueryCmd = p.Second
                    _l.Add(q)
                Next
            End If
        End Sub

        Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of QueryCmd) Implements System.Collections.Generic.IEnumerable(Of QueryCmd).GetEnumerator
            Return _l.GetEnumerator
        End Function

        Private Function _GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return GetEnumerator()
        End Function
    End Class

    Public Class MetaDataQueryIterator
        Implements IEnumerable(Of QueryCmd)

        Private _l As New List(Of QueryCmd)

        Public Sub New(ByVal root As QueryCmd)
            AddQuery(root)
        End Sub

        Protected Sub AddQuery(ByVal query As QueryCmd)
            Dim iq As QueryCmd = query
            'For Each q As QueryCmd In New InnerQueryIterator(query)
            '    iq = q
            'Next
            _l.Add(iq)
            If iq.Unions IsNot Nothing Then
                For Each p As Pair(Of Boolean, QueryCmd) In iq.Unions
                    Dim q As QueryCmd = p.Second
                    AddQuery(q)
                Next
            End If
        End Sub

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