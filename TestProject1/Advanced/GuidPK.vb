Imports Worm.Entities
Imports Worm.Entities.Meta

<Entity("1", Tablename:="dbo.guid_table")> _
Public Class GuidPK
    Inherits SinglePKEntity

    Private _code As Integer
    Private _id As Guid

    <EntityPropertyAttribute("code")> _
    Public Property Code() As Integer
        Get
            Using Read("Code")
                Return _code
            End Using
        End Get
        Set(ByVal value As Integer)
            Using Write("Code")
                _code = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute("pk", Field2DbRelations.PrimaryKey, "ID", DBType:="uniqueidentifier")> _
    Public Overrides Property Identifier() As Object
        Get
            Return _id
        End Get
        Set(ByVal value As Object)
            _id = CType(value, Guid)
        End Set
    End Property

    Public ReadOnly Property Guid() As Guid
        Get
            Return CType(Identifier, System.Guid)
        End Get
    End Property

    'Public Overrides Function GetPKValues() As Pair(Of String, Object)()
    '    Return New Pair(Of String, Object)() {New Pair(Of String, Object)("ID", _id)}
    'End Function

    'Protected Overrides Sub SetPK(ByVal pk() As CoreFramework.Structures.Pair(Of String, Object))
    '    _id = CType(pk(0).Second, System.Guid)
    'End Sub
End Class

<Entity("1", Tablename:="dbo.complex_pk")> _
Public Class ComplexPK
    Inherits CachedLazyLoad

    Private _code As String
    <EntityPropertyAttribute(Field2DbRelations.PK, column:="code")> Public Property Code() As String
        Get
            Using Read("Code")
                Return _code
            End Using
        End Get
        Set(ByVal value As String)
            Using Write("Code")
                _code = value
            End Using
        End Set
    End Property

    Private _i As Integer
    <EntityPropertyAttribute(Field2DbRelations.PK, column:="i")> Public Property Int() As Integer
        Get
            Using Read("Int")
                Return _i
            End Using
        End Get
        Set(ByVal value As Integer)
            Using Write("Int")
                _i = value
            End Using
        End Set
    End Property

    Private _name As String
    <EntityPropertyAttribute(column:="name")> Public Property Name() As String
        Get
            Using Read("Name")
                Return _name
            End Using
        End Get
        Set(ByVal value As String)
            Using Write("Name")
                _name = value
            End Using
        End Set
    End Property

    Protected Overrides Function GetCacheKey() As Integer
        Return _code.GetHashCode Xor _i
    End Function
End Class

<Entity("1", Tablename:="dbo.guid_table")> _
Public Class NonCache
    Inherits Worm.Entities.Entity

    Private _code As Integer
    Private _id As Guid

    <EntityPropertyAttribute(column:="code")> _
    Public Property Code() As Integer
        Get
            'Using Read("Code")
            Return _code
            'End Using
        End Get
        Set(ByVal value As Integer)
            'Using Write("Code")
            _code = value
            'End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="ID", Behavior:=Field2DbRelations.PrimaryKey, column:="pk")> _
    Public Property Identifier() As Object
        Get
            'Using Read("ID")
            Return _id
            'End Using
        End Get
        Set(ByVal value As Object)
            'Using Write("ID")
            _id = CType(value, Guid)
            'End Using
        End Set
    End Property
End Class
