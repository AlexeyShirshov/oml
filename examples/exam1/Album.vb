Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm
Imports Worm.Cache

Namespace test
    <Entity("dbo", "Albums", "1")> _
    Public Class Album
        Inherits KeyEntity

        Private _name As String
        Private _release As System.Nullable(Of Date)
        Private _id As Integer

        Public Sub New()

        End Sub

        Public Sub New(ByVal id As Integer, ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine)
            Init(id, cache, schema)
        End Sub

        <EntityProperty("name")> _
        Public Overridable Property Name() As String
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

        <EntityProperty("release_dt")> _
        Public Overridable Property Release() As Nullable(Of Date)
            Get
                Using Read("Release")
                    Return _release
                End Using
            End Get
            Set(ByVal value As Nullable(Of Date))
                Using Write("Release")
                    _release = value
                End Using
            End Set
        End Property

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
End Namespace