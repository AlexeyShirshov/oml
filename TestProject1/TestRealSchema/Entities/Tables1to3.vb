Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Entities.Meta

<Entity(GetType(TablesImplementation), "1")> _
Public Class Tables1to3
    Inherits SinglePKEntity
    Implements IOptimizedValues, ICopyProperties

    Private _name As String
    Private _table1 As Table1
    Private _table3 As Table33

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer)
        _id = id
        PKLoaded("ID")
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

    Protected Function CopyProperties(ByVal [to] As Object) As Boolean Implements ICopyProperties.CopyTo
        Dim dst = TryCast([to], Tables1to3)
        If dst IsNot Nothing Then
            With Me
                dst._id = ._id
                dst._name = ._name
                dst._table1 = ._table1
                dst._table3 = ._table3
            End With

            Return True
        End If

        Return False
    End Function

    Public Overridable Function SetValueOptimized(ByVal fieldName As String, ByVal oschema As IEntitySchema, ByVal value As Object) As Boolean Implements IOptimizedValues.SetValueOptimized
        Select Case fieldName
            Case "Title"
                Title = CStr(value)
            Case "Table1"
                Table1 = CType(value, TestProject1.Table1)
            Case "Table3"
                Table3 = CType(value, TestProject1.Table33)
            Case "ID"
                Identifier = value
            Case Else
                Return False
                'Throw New NotSupportedException(fieldName)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select

        Return True
    End Function

    Public Function GetValueOptimized(ByVal propertyAlias As String, ByVal oschema As Worm.Entities.Meta.IEntitySchema, ByRef found As Boolean) As Object Implements Worm.Entities.IOptimizedValues.GetValueOptimized
        found = True
        Select Case propertyAlias
            Case "Title"
                Return _name
            Case "Table1"
                Return _table1
            Case "Table3"
                Return _table3
            Case Else
                found = False
                'Return GetValueReflection(propertyAlias, oschema)
                'Return schema.GetFieldColumnMap(propertyAlias).GetValue(Me)
                'Return GetMappingEngine.GetProperty(Me.GetType, schema, propertyAlias).GetValue(Me, Nothing)
                'Throw New NotSupportedException(propertyAlias)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select
        Return Nothing
    End Function

    <EntityPropertyAttribute(PropertyAlias:="Title")> _
    Public Property Title() As String
        Get
            Using Read("Title")
                Return _name
            End Using
        End Get
        Set(ByVal value As String)
            Using Write("Title")
                _name = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="Table1")> _
    Public Property Table1() As Table1
        Get
            Using Read("Table1")
                Return _table1
            End Using
        End Get
        Set(ByVal value As Table1)
            Using Write("Table1")
                _table1 = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="Table3")> _
    Public Property Table3() As Table33
        Get
            Using Read("Table3")
                Return _table3
            End Using
        End Get
        Set(ByVal value As Table33)
            Using Write("Table3")
                _table3 = value
            End Using
        End Set
    End Property

End Class


Public Class TablesImplementation
    Inherits ObjectSchemaBaseImplementation
    Implements IRelation

    Private _idx As OrmObjectIndex
    'Public Shared _tables() As SourceFragment = {New SourceFragment("dbo.Tables1to3Relation")}

    'Public Enum Tables
    '    Main
    'End Enum

    Public Sub New()
        _tbl = New SourceFragment("dbo.Tables1to3Relation")
    End Sub

    Public Overrides ReadOnly Property FieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        Get
            If _idx Is Nothing Then
                Dim idx As New OrmObjectIndex
                idx.Add(New MapField2Column("ID", Table, "id"))
                idx.Add(New MapField2Column("Title", Table, "name"))
                idx.Add(New MapField2Column("Table1", Table, "table1"))
                idx.Add(New MapField2Column("Table3", Table, "table3"))
                _idx = idx
            End If
            Return _idx
        End Get
    End Property

    'Public Overrides Function GetTables() As SourceFragment()
    '    Return _tables
    'End Function

    Public Function GetFirstType() As IRelation.RelationDesc Implements IRelation.GetFirstType
        Return New IRelation.RelationDesc("Table1", GetType(Table33))
    End Function

    Public Function GetSecondType() As IRelation.RelationDesc Implements IRelation.GetSecondType
        Return New IRelation.RelationDesc("Table3", GetType(Table1))
    End Function

End Class
