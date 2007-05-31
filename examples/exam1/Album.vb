Imports Worm.Orm

Namespace test
    <Entity("dbo.Albums", "id", "1")> _
    Public Class Album
        Inherits OrmBaseT(Of Album)

        Private _name As String
        Private _release As System.Nullable(Of Date)

        Public Sub New()

        End Sub

        Public Sub New(ByVal id As Integer, ByVal cache As OrmCacheBase, ByVal schema As OrmSchemaBase)
            MyBase.New(id, cache, schema)
        End Sub

        <ColumnAttribute()> _
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

        <ColumnAttribute()> _
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
    End Class
End Namespace