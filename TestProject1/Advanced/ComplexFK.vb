Imports Worm
Imports Worm.Collections
Imports Worm.Entities
Imports Worm.Entities.Meta

<Entity(GetType(ComplexFK.ComplexFKSchema), "1")>
Public Class ComplexFK
    Inherits SinglePKEntity

    Private _id As Integer
    Private _x As Integer
    Public Overrides Property Identifier() As Object
        Get
            Return _id
        End Get
        Set(ByVal value As Object)
            _id = CInt(value)
        End Set
    End Property
#Disable Warning IDE1006 ' Naming Styles
    Public Property x As Integer
        Get
            Using Read("x")
                Return _x
            End Using
        End Get
        Set(value As Integer)
            Using Write("x")
                _x = value
            End Using
        End Set
    End Property
#Enable Warning IDE1006 ' Naming Styles
    Public Property Parent As ComplexPK
    Public Class ComplexFKSchema
        Implements IEntitySchema, IOptimizeSetValue

        Private _idx As OrmObjectIndex
        Public ReadOnly Property Table As New SourceFragment("dbo.complex_fk") Implements IEntitySchema.Table

        Public ReadOnly Property FieldColumnMap As IndexedCollection(Of String, MapField2Column) Implements IPropertyMap.FieldColumnMap
            Get
                If _idx Is Nothing Then
                    Dim idx As New OrmObjectIndex From {
                        New MapField2Column("ID", Table, Field2DbRelations.PrimaryKey, New SourceField("id")),
                        New MapField2Column("Parent", Table, "code", "i"),
                        New MapField2Column("x", Table, "x")
                    }
                    _idx = idx
                End If
                Return _idx
            End Get
        End Property

        Public Function GetOptimizedDelegate(propertyAlias As String) As IOptimizeSetValue.SetValueDelegate Implements IOptimizeSetValue.GetOptimizedDelegate
            If propertyAlias = "x" Then
                Return Sub(entity As Object, value As Object)
                           CType(entity, ComplexFK)._x = CInt(value)
                       End Sub
            Else
                Return MapField2Column.EmptyOptimizedSetValue
            End If
        End Function
    End Class
End Class
