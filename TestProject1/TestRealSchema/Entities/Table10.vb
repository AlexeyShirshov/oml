Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Cache

<Entity(GetType(Table10Implementation), "1")> _
Public Class Table10
    Inherits SinglePKEntity
    Implements IOptimizedValues, ICopyProperties

    Private _tbl1 As Table1

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer)
        _id = id
        PKLoaded(1, "ID")
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

    Protected Sub CopyProperties(ByVal [to] As Object) Implements ICopyProperties.CopyTo
        CType([to], Table10)._tbl1 = _tbl1
        CType([to], Table10)._id = _id
    End Sub

    Public Overridable Function SetValueOptimized(
        ByVal fieldName As String, ByVal oschema As IEntitySchema, ByVal value As Object) As Boolean Implements IOptimizedValues.SetValueOptimized
        Select Case fieldName
            Case "Table1"
                Tbl = CType(value, Table1)
            Case Else
                Return False
                'SetValueReflection(fieldName, value, oschema)
                'Throw New NotSupportedException(fieldName)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select

        Return True
    End Function

    Public Function GetValueOptimized(ByVal propertyAlias As String, ByVal schema As Worm.Entities.Meta.IEntitySchema, ByRef found As Boolean) As Object Implements Worm.Entities.IOptimizedValues.GetValueOptimized
        found = True
        Select Case propertyAlias
            Case "Table1"
                Return _tbl1
            Case Else
                found = False
                'Return GetValueReflection(propertyAlias, schema)
                'Throw New NotSupportedException(propertyAlias)
        End Select

        Return Nothing
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
                idx.Add(New MapField2Column("ID", Table, "id"))
                idx.Add(New MapField2Column("Table1", Table, "table1_id"))
                _idx = idx
            End If
            Return _idx
        End Get
    End Property

    'Public Overrides Function GetTables() As SourceFragment()
    '    Return _tables
    'End Function
End Class