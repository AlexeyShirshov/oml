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
        Implements ICloneable

        Private _table As String
        Private _schema As String
        Private _uqName As String = Guid.NewGuid.GetHashCode.ToString
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

        Public ReadOnly Property UniqueName(ByVal os As ObjectSource) As String
            Get
                If os Is Nothing OrElse os.Type IsNot Nothing OrElse Not String.IsNullOrEmpty(os.EntityName) Then
                    Return RawName & "^" & _uqName
                Else
                    Return os.ObjectAlias.UniqueName & "$" & RawName & "^" & _uqName
                End If
            End Get
        End Property

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

        Private Function _Clone() As Object Implements System.ICloneable.Clone
            Return Clone()
        End Function

        Public Overridable Function Clone() As Object
            Dim t As New SourceFragment(_schema, _table)
            Return t
        End Function
    End Class

    Public Enum SearchType
        Contains
        Freetext
    End Enum

    Public Class SearchFragment
        Inherits SourceFragment
        Implements ISearchTable

        Private _type As Type
        Private _searchString As String
        Private _st As SearchType
        Private _queryFields() As String
        Private _top As Integer = Integer.MinValue

#Region " Ctors "

        Public Sub New()

        End Sub

        Public Sub New(ByVal searchType As Type, ByVal searchString As String)
            _type = searchType
            _searchString = searchString
        End Sub

        Public Sub New(ByVal searchString As String, ByVal top As Integer)
            _top = top
            _searchString = searchString
        End Sub

        Public Sub New(ByVal searchString As String, ByVal searchType As SearchType, ByVal top As Integer)
            _top = top
            _searchString = searchString
            _st = searchType
        End Sub

        Public Sub New(ByVal searchString As String, ByVal searchType As SearchType, _
                       ByVal top As Integer, ByVal queryFields() As String)
            _top = top
            _searchString = searchString
            _st = searchType
            _queryFields = queryFields
        End Sub

        Public Sub New(ByVal searchString As String, ByVal searchType As SearchType, _
                       ByVal queryFields() As String)
            _searchString = searchString
            _st = searchType
            _queryFields = queryFields
        End Sub

        Public Sub New(ByVal searchType As Type, ByVal searchString As String, ByVal top As Integer)
            _type = searchType
            _searchString = searchString
            _top = top
        End Sub

        Public Sub New(ByVal searchType As Type, ByVal searchString As String, _
                       ByVal search As SearchType)
            _type = searchType
            _searchString = searchString
            _st = search
        End Sub

        Public Sub New(ByVal searchType As Type, ByVal searchString As String, _
                       ByVal search As SearchType, ByVal top As Integer)
            _type = searchType
            _searchString = searchString
            _st = search
            _top = top
        End Sub

        Public Sub New(ByVal searchType As Type, ByVal searchString As String, _
                       ByVal search As SearchType, ByVal queryFields() As String)
            _type = searchType
            _searchString = searchString
            _st = search
            _queryFields = queryFields
        End Sub

        Public Sub New(ByVal searchType As Type, ByVal searchString As String, _
                       ByVal search As SearchType, ByVal queryFields() As String, _
                       ByVal top As Integer)
            _type = searchType
            _searchString = searchString
            _st = search
            _queryFields = queryFields
            _top = top
        End Sub

        Public Sub New(ByVal searchString As String, _
                       ByVal queryFields() As String)
            _searchString = searchString
            _queryFields = queryFields
        End Sub

        Public Sub New(ByVal searchString As String, _
                       ByVal queryField As String)
            _searchString = searchString
            _queryFields = New String() {queryField}
        End Sub

        Public Sub New(ByVal searchString As String)
            _searchString = searchString
        End Sub
#End Region

        Public ReadOnly Property Type() As Type
            Get
                Return _type
            End Get
        End Property

        Public ReadOnly Property SearchText() As String
            Get
                Return _searchString
            End Get
        End Property

        Public ReadOnly Property SearchType() As SearchType
            Get
                Return _st
            End Get
        End Property

        Public ReadOnly Property QueryFields() As String()
            Get
                Return _queryFields
            End Get
        End Property

        Public ReadOnly Property Top() As Integer
            Get
                Return _top
            End Get
        End Property

        Public Function GetSearchTableName() As String
            If _st = Meta.SearchType.Contains Then
                Return "containstable"
            ElseIf _st = Meta.SearchType.Freetext Then
                Return "freetexttable"
            Else
                Throw New NotSupportedException(_st.ToString)
            End If
        End Function
    End Class
End Namespace