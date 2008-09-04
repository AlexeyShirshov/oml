Imports Worm.Orm
Imports Worm.Orm.Meta

<Entity("1", Tablename:="dbo.guid_table")> _
Public Class GuidPK
    Inherits OrmBase

    Private _code As Integer
    Private _id As Guid

    <Column(column:="code")> _
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

    <Column("ID", Field2DbRelations.PrimaryKey, column:="pk")> _
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
    Inherits CachedEntity

    Private _code As String
    <Column(Field2DbRelations.PK, column:="code")> Public Property Code() As String
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
    <Column(Field2DbRelations.PK, column:="i")> Public Property Int() As Integer
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
    <Column(column:="name")> Public Property Name() As String
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