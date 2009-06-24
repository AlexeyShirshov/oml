Imports Worm.Cache
Imports Worm.Entities
Imports Worm.Entities.Meta

<Entity(GetType(TablesImplementation), "1")> _
Public Class Tables1to3
    Inherits OrmBaseT(Of Tables1to3)
    Implements IOrmEditable(Of Tables1to3), IOptimizedValues

    Private _name As String
    Private _table1 As Table1
    Private _table3 As Table33

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As CacheBase, ByVal schema As Worm.ObjectMappingEngine)
        MyBase.New(id, cache, schema)
    End Sub

    'Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)
    '    CopyRelation(CType(from, Tables1to3), CType([to], Tables1to3))
    'End Sub

    Protected Sub CopyRelation(ByVal [from] As Tables1to3, ByVal [to] As Tables1to3) Implements IOrmEditable(Of Tables1to3).CopyBody
        With [from]
            [to]._name = ._name
            [to]._table1 = ._table1
            [to]._table3 = ._table3
        End With
    End Sub

    'Public Overloads Overrides Function CreateSortComparer(ByVal sort As String, ByVal sortType As Worm.Orm.SortType) As System.Collections.IComparer
    '    Throw New NotSupportedException
    'End Function

    'Public Overloads Overrides Function CreateSortComparer(Of T As {New, Worm.Orm.OrmBase})(ByVal sort As String, ByVal sortType As Worm.Orm.SortType) As System.Collections.Generic.IComparer(Of T)
    '    Throw New NotSupportedException
    'End Function

    'Protected Overrides Function GetNew() As Worm.Orm.OrmBase
    '    Return New Tables1to3(Identifier, OrmCache, OrmSchema)
    'End Function

    Public Overridable Sub SetValue( _
        ByVal fieldName As String, ByVal oschema As IEntitySchema, ByVal value As Object) Implements IOptimizedValues.SetValueOptimized
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
                Throw New NotSupportedException(fieldName)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select
    End Sub

    Public Function GetValueOptimized(ByVal propertyAlias As String, ByVal schema As Worm.Entities.Meta.IEntitySchema) As Object Implements Worm.Entities.IOptimizedValues.GetValueOptimized
        Select Case propertyAlias
            Case "Title"
                Return _name
            Case "Table1"
                Return _table1
            Case "Table3"
                Return _table3
            Case Else
                Return GetMappingEngine.GetProperty(Me.GetType, schema, propertyAlias).GetValue(Me, Nothing)
                'Throw New NotSupportedException(propertyAlias)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select
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

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New OrmObjectIndex
            idx.Add(New MapField2Column("ID", "id", Table))
            idx.Add(New MapField2Column("Title", "name", Table))
            idx.Add(New MapField2Column("Table1", "table1", Table))
            idx.Add(New MapField2Column("Table3", "table3", Table))
            _idx = idx
        End If
        Return _idx
    End Function

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
