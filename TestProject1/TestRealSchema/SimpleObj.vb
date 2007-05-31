Imports Worm
Imports Worm.Orm

<Entity("dbo.table1", "id", "1")> _
Public Class SimpleObj
    Inherits OrmBaseT(Of SimpleObj)

    Private _title As String

    <Column(column:="name")> _
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
End Class

<Entity("dbo.table1", "id", "1")> _
Public Class SimpleObj2
    Inherits OrmBaseT(Of SimpleObj)

    Private _title As String

    <Column(column:="name")> _
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
End Class
