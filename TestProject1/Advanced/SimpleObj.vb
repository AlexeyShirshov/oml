Imports Worm
Imports Worm.Entities
Imports Worm.Entities.Meta

<Entity("dbo", "table1", "1")> _
Public Class SimpleObj
    Inherits KeyEntity

    Private _title As String

    <EntityPropertyAttribute(column:="name")> _
    Public Property Title() As String
        Get
            Using Read("Title")
                Return _title
            End Using
        End Get
        Set(ByVal value As String)
            Using Write("Title")
                _title = value
            End Using
        End Set
    End Property

    Private _id As Integer
    <EntityProperty("id", Field2DbRelations.PrimaryKey, DBType:="int")> _
    Public Overrides Property Identifier() As Object
        Get
            Return _id
        End Get
        Set(ByVal value As Object)
            _id = CInt(value)
        End Set
    End Property
End Class

<Entity("dbo", "table1", "1")> _
Public Class SimpleObjWithoutLazyLoad
    Inherits KeyEntityBase

    Private _title As String

    <EntityPropertyAttribute(column:="name")> _
    Public Property Title() As String
        Get
            Return _title
        End Get
        Set(ByVal value As String)
            _title = value
        End Set
    End Property

    Private _code As Integer
    <EntityPropertyAttribute(column:="code")> _
    Public Property Code() As Integer
        Get
            Return _code
        End Get
        Set(ByVal value As Integer)
            _code = value
        End Set
    End Property

    Private _id As Integer
    <EntityProperty("id", Field2DbRelations.PrimaryKey, DBType:="int")> _
    Public Overrides Property Identifier() As Object
        Get
            Return _id
        End Get
        Set(ByVal value As Object)
            _id = CInt(value)
        End Set
    End Property
End Class

<Entity("dbo", "table2", "1")> _
Public Class SimpleObj2
    Inherits KeyEntity

    Private _s As SimpleObjWithoutLazyLoad

    <EntityProperty("table1_id")> _
    Public Property Obj1() As SimpleObjWithoutLazyLoad
        Get
            Using Read("Obj1")
                Return _s
            End Using
        End Get
        Set(ByVal value As SimpleObjWithoutLazyLoad)
            Using Write("Obj1")
                _s = value
            End Using
        End Set
    End Property

    Private _m As Decimal
    <EntityPropertyAttribute(column:="m")> _
    Public Property Money() As Decimal
        Get
            Using Read("Money")
                Return _m
            End Using
        End Get
        Set(ByVal value As Decimal)
            Using Write("Money")
                _m = value
            End Using
        End Set
    End Property

    Private _id As Integer
    <EntityProperty("id", Field2DbRelations.PrimaryKey, DBType:="int")> _
    Public Overrides Property Identifier() As Object
        Get
            Return _id
        End Get
        Set(ByVal value As Object)
            _id = CInt(value)
        End Set
    End Property
End Class

<Entity("dbo", "table2", "1")> _
Public Class SimpleObj3
    Inherits KeyEntityBase

    Private _s As SimpleObjWithoutLazyLoad

    <EntityProperty("table1_id")> _
    Public Property Obj1() As SimpleObjWithoutLazyLoad
        Get
            Return _s
        End Get
        Set(ByVal value As SimpleObjWithoutLazyLoad)
            _s = value
        End Set
    End Property

    Private _m As Decimal
    <EntityPropertyAttribute(column:="m")> _
    Public Property Money() As Decimal
        Get
            Return _m
        End Get
        Set(ByVal value As Decimal)
            _m = value
        End Set
    End Property

    Private _id As Integer
    <EntityProperty("id", Field2DbRelations.PrimaryKey, DBType:="int")> _
    Public Overrides Property Identifier() As Object
        Get
            Return _id
        End Get
        Set(ByVal value As Object)
            _id = CInt(value)
        End Set
    End Property
End Class