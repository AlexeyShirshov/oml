Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Entities.Meta

<Entity(GetType(Table4Implementation), "1"), Entity(GetType(Table4Implementation2), "2")> _
Public Class Table4
    Inherits KeyEntity
    Implements IOptimizedValues

    Private _col As Nullable(Of Boolean)
    Private _g As Guid

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As CacheBase, ByVal schema As Worm.ObjectMappingEngine)
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
        With CType([from], Table4)
            CType([to], Table4)._col = ._col
            CType([to], Table4)._g = ._g
            CType([to], Table4)._id = ._id
        End With
    End Sub

    Public Overridable Sub SetValue( _
        ByVal fieldName As String, ByVal oschema As IEntitySchema, ByVal value As Object) Implements IOptimizedValues.SetValueOptimized
        Select Case fieldName
            Case "Col"
                Col = CType(value, Global.System.Nullable(Of Boolean))
            Case "GUID"
                GUID = CType(value, System.Guid)
            Case "ID"
                Identifier = value
            Case Else
                Throw New NotSupportedException(fieldName)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select
    End Sub

    Public Function GetValueOptimized(ByVal propertyAlias As String, ByVal schema As Worm.Entities.Meta.IEntitySchema) As Object Implements Worm.Entities.IOptimizedValues.GetValueOptimized
        Select Case propertyAlias
            Case "Col"
                Return _col
            Case "GUID"
                Return _g
            Case Else
                Return GetValueReflection(propertyAlias, schema)
                'Throw New NotSupportedException(propertyAlias)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select
    End Function

    <EntityPropertyAttribute(PropertyAlias:="Col")> _
    Public Property Col() As Nullable(Of Boolean)
        Get
            Using Read("Col")
                Return _col
            End Using
        End Get
        Set(ByVal value As Nullable(Of Boolean))
            Using Write("Col")
                _col = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="GUID", behavior:=Field2DbRelations.InsertDefault Or Field2DbRelations.SyncInsert)> _
    Public Property GUID() As Guid
        Get
            Using Read("GUID")
                Return _g
            End Using
        End Get
        Set(ByVal value As Guid)
            Using Write("GUID")
                _g = value
            End Using
        End Set
    End Property

End Class

Public Class Table4Implementation
    Inherits ObjectSchemaBaseImplementation
    Implements ICacheBehavior

    'Private _tables() As SourceFragment = {New SourceFragment("dbo.[Table]")}

    'Public Enum Tables
    '    Main
    'End Enum

    Public Sub New()
        _tbl = New SourceFragment("dbo.[Table]")
    End Sub

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        Dim idx As New OrmObjectIndex
        idx.Add(New MapField2Column("ID", "id", Table, Field2DbRelations.PK))
        idx.Add(New MapField2Column("Col", "col", Table))
        idx.Add(New MapField2Column("GUID", "uq", Table))
        Return idx
    End Function

    'Public Overrides Function GetTables() As SourceFragment()
    '    Return _tables
    'End Function

    Public Function GetEntityKey(ByVal filterInfo As Object) As String Implements ICacheBehavior.GetEntityKey
        Return "kljf"
    End Function

    Public Function GetEntityTypeKey(ByVal filterInfo As Object) As Object Implements ICacheBehavior.GetEntityTypeKey
        Return "91bn34fh    oebnfklE:"
    End Function
End Class

Public Class Table4Implementation2
    Inherits ObjectSchemaBaseImplementation

    Private _idx As OrmObjectIndex
    'Private _tables() As SourceFragment = {New SourceFragment("dbo.[Table]")}

    'Public Enum Tables
    '    Main
    'End Enum

    Public Sub New()
        _tbl = New SourceFragment("dbo.[Table]")
    End Sub

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New OrmObjectIndex
            idx.Add(New MapField2Column("ID", "id", Table, Field2DbRelations.PK))
            idx.Add(New MapField2Column("Col", "col", Table))
            idx.Add(New MapField2Column("GUID", "uq", Table, Field2DbRelations.InsertDefault))
            _idx = idx
        End If
        Return _idx
    End Function

    'Public Overrides Function GetTables() As SourceFragment()
    '    Return _tables
    'End Function
End Class
