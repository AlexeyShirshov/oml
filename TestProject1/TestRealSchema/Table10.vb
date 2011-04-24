Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Cache

<Entity(GetType(Table10Implementation), "1")> _
Public Class Table10
    Inherits KeyEntity
    Implements IOptimizedValues

    Private _tbl1 As Table1

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As OrmCache, ByVal schema As Worm.ObjectMappingEngine)
        Init(id, cache, schema)
    End Sub

    Private _id As Integer

    <EntityProperty(Field2DbRelations.PrimaryKey)> _
    Public Property ID() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property

    Public Overrides Property Identifier() As Object
        Get
            Return _id
        End Get
        Set(ByVal value As Object)
            _id = CInt(value)
        End Set
    End Property

    Protected Overrides Sub CopyProperties(ByVal from As Worm.Entities._IEntity, ByVal [to] As Worm.Entities._IEntity, ByVal oschema As Worm.Entities.Meta.IEntitySchema)
        CType([to], Table10)._tbl1 = _tbl1
        CType([to], Table10)._id = _id
    End Sub

    Public Overridable Sub SetValue( _
        ByVal fieldName As String, ByVal oschema As IEntitySchema, ByVal value As Object) Implements IOptimizedValues.SetValueOptimized
        Select Case fieldName
            Case "Table1"
                Tbl = CType(value, Table1)
            Case Else
                SetValueReflection(fieldName, value, oschema)
                'Throw New NotSupportedException(fieldName)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select
    End Sub

    Public Function GetValueOptimized(ByVal propertyAlias As String, ByVal schema As Worm.Entities.Meta.IEntitySchema) As Object Implements Worm.Entities.IOptimizedValues.GetValueOptimized
        Select Case propertyAlias
            Case "Table1"
                Return _tbl1
            Case Else
                Return GetValueReflection(propertyAlias, schema)
                'Throw New NotSupportedException(propertyAlias)
        End Select
    End Function

    <EntityPropertyAttribute(PropertyAlias:="Table1")> _
    Public Property Tbl() As Table1
        Get
            Using Read("Table1")
                Return _tbl1
            End Using
        End Get
        Set(ByVal value As Table1)
            Using Write("Table1")
                _tbl1 = value
            End Using
        End Set
    End Property

   
End Class

Public Class Table10Implementation
    Inherits ObjectSchemaBaseImplementation

    Private _idx As OrmObjectIndex
    'Private _tables() As SourceFragment = {New SourceFragment("dbo.Table10")}

    Public Sub New()
        _tbl = New SourceFragment("dbo.Table10")
    End Sub

    'Public Enum Tables
    '    Main
    'End Enum

    Public Overrides ReadOnly Property FieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        Get
            If _idx Is Nothing Then
                Dim idx As New OrmObjectIndex
                idx.Add(New MapField2Column("ID", "id", Table))
                idx.Add(New MapField2Column("Table1", "table1_id", Table))
                _idx = idx
            End If
            Return _idx
        End Get
    End Property

    'Public Overrides Function GetTables() As SourceFragment()
    '    Return _tables
    'End Function
End Class