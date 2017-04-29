## The class definition
The class you want to use with Worm must
* Be derived from OrmBaseT generic
* Have entity attribute
Actualy, its not a strong requirement. Once you mark a class with Entity attribute, all derived classes become Worm classes
* Have properties related to database fields with Column attribute implemented in a special manner.
Here is an example in VB.NET
{{
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
        Public Property Name() As String
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
        Public Property Release() As Nullable(Of Date)
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
}}
In order to support lasy load all properties related to database must have using statement with Read and Write method class for reading or writing data accordingly.