Namespace Entities.Meta

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

    Public Class AliasMgr
        Implements IPrepareTable

        Private _defaultAliases As Generic.IDictionary(Of SourceFragment, String)
        Private _objectAlises As Generic.IDictionary(Of ObjectAlias, Generic.IDictionary(Of SourceFragment, String))
        Private _cnt As Integer

        Private Sub New(ByVal aliases As Generic.IDictionary(Of SourceFragment, String))
            _defaultAliases = aliases
            _objectAlises = New Generic.Dictionary(Of ObjectAlias, Generic.IDictionary(Of SourceFragment, String))
        End Sub

        Public Shared Function Create() As AliasMgr
            Return New AliasMgr(New Generic.Dictionary(Of SourceFragment, String))
        End Function

        Public Function AddTable(ByRef table As SourceFragment, ByVal os As Entities.ObjectSource) As String Implements IPrepareTable.AddTable
            Return AddTable(table, os, CType(Nothing, ICreateParam))
        End Function

        Public Function AddTable(ByRef table As SourceFragment, ByVal os As Entities.ObjectSource, ByVal pmgr As ICreateParam) As String Implements IPrepareTable.AddTable
            'Dim tf As IOrmTableFunction = TryCast(schema, IOrmTableFunction)
            Dim t As SourceFragment = table
            Dim tt As SourceFragment = table.OnTableAdd(pmgr)
            If tt IsNot Nothing Then
                '    Dim f As SourceFragment = tf.GetFunction(table, pmgr)
                '    If f IsNot Nothing Then
                table = tt
                '    End If
            End If
            Dim i As Integer = _cnt + 1
            Dim [alias] As String = "t" & i
            If os Is Nothing OrElse os.Type IsNot Nothing OrElse Not String.IsNullOrEmpty(os.EntityName) Then
                _defaultAliases.Add(t, [alias])
            Else
                Dim dic As Generic.IDictionary(Of SourceFragment, String) = Nothing
                If Not _objectAlises.TryGetValue(os.ObjectAlias, dic) Then
                    dic = New Generic.Dictionary(Of SourceFragment, String)
                    _objectAlises.Add(os.ObjectAlias, dic)
                End If
                dic.Add(t, [alias])
            End If
            _cnt += 1
            Return [alias]
        End Function

        Friend Sub AddTable(ByVal tbl As SourceFragment, ByVal [alias] As String)
            _defaultAliases.Add(tbl, [alias])
        End Sub

        'Public Function GetAlias(ByVal table As String) As String
        '    Return _aliases(table)
        'End Function

        'Public ReadOnly Property Aliases() As IDictionary(Of SourceFragment, String) Implements IPrepareTable.Aliases
        '    Get
        '        Return _aliases
        '    End Get
        'End Property

        'Public ReadOnly Property IsEmpty() As Boolean
        '    Get
        '        Return _defaultAliases Is Nothing
        '    End Get
        'End Property

        Public Sub Replace(ByVal schema As ObjectMappingEngine, ByVal gen As StmtGenerator, ByVal table As Entities.Meta.SourceFragment, ByVal os As ObjectSource, ByVal sb As System.Text.StringBuilder) Implements IPrepareTable.Replace
            If os Is Nothing OrElse os.Type IsNot Nothing OrElse Not String.IsNullOrEmpty(os.EntityName) Then
                sb.Replace(table.UniqueName(Nothing) & schema.Delimiter, _defaultAliases(table) & gen.Selector)
            Else
                sb.Replace(table.UniqueName(os) & schema.Delimiter, _objectAlises(os.ObjectAlias)(table) & gen.Selector)
            End If
        End Sub

        Public Function GetAlias(ByVal table As Entities.Meta.SourceFragment, ByVal os As Entities.ObjectSource) As String Implements IPrepareTable.GetAlias
            If os Is Nothing OrElse os.Type IsNot Nothing OrElse Not String.IsNullOrEmpty(os.EntityName) Then
                Return _defaultAliases(table)
            Else
                Return _objectAlises(os.ObjectAlias)(table)
            End If
        End Function

        Public Function ContainsKey(ByVal table As Entities.Meta.SourceFragment, ByVal os As Entities.ObjectSource) As Boolean Implements IPrepareTable.ContainsKey
            If os Is Nothing OrElse os.Type IsNot Nothing OrElse Not String.IsNullOrEmpty(os.EntityName) Then
                Return _defaultAliases.ContainsKey(table)
            Else
                Return _objectAlises(os.ObjectAlias).ContainsKey(table)
            End If
        End Function
    End Class

End Namespace