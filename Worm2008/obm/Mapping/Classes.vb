Namespace Orm.Meta

    Public Class PKDesc
        Public ReadOnly PropertyAlias As String
        Public ReadOnly Value As Object

        Public Sub New(ByVal propAlias As String, ByVal value As Object)
            Me.PropertyAlias = propAlias
            Me.Value = value
        End Sub
    End Class

    Public Class SourceFragment

        Private _table As String
        Private _schema As String
#If DEBUG Then
        Private _stack As String = Environment.StackTrace
#End If

        Public Sub New()

        End Sub

        Public Sub New(ByVal tableName As String)
            _table = tableName
        End Sub

        Public Sub New(ByVal schema As String, ByVal tableName As String)
            _table = tableName
            _schema = schema
        End Sub

        'Public Property TableName() As String
        '    Get
        '        Return _table
        '    End Get
        '    Set(ByVal value As String)
        '        _table = value
        '    End Set
        'End Property

        Public Overrides Function ToString() As String
            Throw New NotSupportedException
        End Function

        Public Overridable Function OnTableAdd(ByVal pmgr As ICreateParam) As SourceFragment
            Return Nothing
        End Function

        Public ReadOnly Property RawName() As String
            Get
                Return _schema & "^" & _table
            End Get
        End Property

        Public ReadOnly Property Schema() As String
            Get
                Return _schema
            End Get
        End Property

        Public ReadOnly Property Name() As String
            Get
                Return _table
            End Get
        End Property
    End Class

End Namespace