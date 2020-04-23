Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Entities.Meta

<Entity(GetType(Table4Implementation), "1"), Entity(GetType(Table4Implementation2), "2")> _
Public Class Table4
    Inherits SinglePKEntity
    Implements IOptimizedValues, ICopyProperties

    Private _col As Nullable(Of Boolean)
    Private _g As Guid

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer)
        _id = id
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
        With Me
            CType([to], Table4)._col = ._col
            CType([to], Table4)._g = ._g
            CType([to], Table4)._id = ._id
        End With
    End Sub

    Public Overridable Function SetValueOptimized(ByVal fieldName As String, ByVal oschema As IEntitySchema, ByVal value As Object) As Boolean Implements IOptimizedValues.SetValueOptimized
        Select Case fieldName
            Case "Col"
                Col = CType(value, Global.System.Nullable(Of Boolean))
            Case "GUID"
                GUID = CType(value, System.Guid)
            Case "ID"
                Identifier = value
            Case Else
                Return False
                'Throw New NotSupportedException(fieldName)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select

        Return True
    End Function

    Public Function GetValueOptimized(ByVal propertyAlias As String, ByVal schema As Worm.Entities.Meta.IEntitySchema, ByRef found As Boolean) As Object Implements Worm.Entities.IOptimizedValues.GetValueOptimized
        found = True
        Select Case propertyAlias
            Case "Col"
                Return _col
            Case "GUID"
                Return _g
            Case Else
                found = False
                'Return GetValueReflection(propertyAlias, schema)
                'Throw New NotSupportedException(propertyAlias)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select

        Return Nothing
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

    Private _idx As OrmObjectIndex
    'Private _tables() As SourceFragment = {New SourceFragment("dbo.[Table]")}

    'Public Enum Tables
    '    Main
    'End Enum

    Public Sub New()
        _tbl = New SourceFragment("dbo.[Table]")
    End Sub

    Public Overrides ReadOnly Property FieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        Get
            If _idx Is Nothing Then
                _idx = New OrmObjectIndex
                _idx.Add(New MapField2Column("ID", Table, Field2DbRelations.PK, New SourceField("id")))
                _idx.Add(New MapField2Column("Col", Table, "col"))
                _idx.Add(New MapField2Column("GUID", Table, "uq"))
            End If
            Return _idx
        End Get
    End Property

    'Public Overrides Function GetTables() As SourceFragment()
    '    Return _tables
    'End Function

    Public Function GetEntityKey() As String Implements ICacheBehavior.GetEntityKey
        Return "kljf"
    End Function

    Public Function GetEntityTypeKey() As Object Implements ICacheBehavior.GetEntityTypeKey
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

    Public Overrides ReadOnly Property FieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        Get
            If _idx Is Nothing Then
                Dim idx As New OrmObjectIndex
                idx.Add(New MapField2Column("ID", Table, Field2DbRelations.PK, New SourceField("id")))
                idx.Add(New MapField2Column("Col", Table, "col"))
                idx.Add(New MapField2Column("GUID", Table, Field2DbRelations.InsertDefault, New SourceField("uq")))
                _idx = idx
            End If
            Return _idx
        End Get
    End Property

    'Public Overrides Function GetTables() As SourceFragment()
    '    Return _tables
    'End Function
End Class
