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

'<Entity("dbo", "table1", "1")> _
'Public Class SimpleObj2
'    Inherits KeyEntity

'    Private _title As String

'    <EntityPropertyAttribute(column:="name")> _
'    Public Property Title() As String
'        Get
'            Using Read("Title")
'                Return _title
'            End Using
'        End Get
'        Set(ByVal value As String)
'            Using Write("Title")
'                _title = value
'            End Using
'        End Set
'    End Property
'End Class
