Imports Worm.Database
Imports Worm.Cache
Imports Worm

Module Module2

    <Orm.Meta.Entity("junk","id","1")
    Class TestEditTable
        Inherits Orm.OrmBaseT(Of TestEditTable)

        Private _name As String

        <Orm.Meta.Column()> _
        Public Property Name() As String
            Get
                Using Read("Name")
                    Return _name
                End Using
            End Get
            Set(ByVal value As String)
                Using write("Name")
                    _name = value
                End Using
            End Set
        End Property


    End Class
    Sub main()

    End Sub
End Module
