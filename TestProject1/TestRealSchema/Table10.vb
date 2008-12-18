Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Cache

<Entity(GetType(Table10Implementation), "1")> _
Public Class Table10
    Inherits OrmBaseT(Of Table10)
    Implements IOrmEditable(Of Table10), IOptimizedValues

    Private _tbl1 As Table1

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As OrmCache, ByVal schema As Worm.ObjectMappingEngine)
        MyBase.New(id, cache, schema)
    End Sub

    'Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)
    '    CopyTable2(CType([from], Table10), CType([to], Table10))
    'End Sub

    'Public Overloads Overrides Function CreateSortComparer(ByVal sort As String, ByVal sort_type As Worm.Orm.SortType) As System.Collections.IComparer
    '    Throw New NotImplementedException
    'End Function

    'Public Overloads Overrides Function CreateSortComparer(Of T As {OrmBase, New})(ByVal sort As String, ByVal sort_type As Worm.Orm.SortType) As System.Collections.Generic.IComparer(Of T)
    '    Throw New NotImplementedException
    'End Function

    'Protected Overrides Function GetNew() As Worm.Orm.OrmBase
    '    Return New Table2(Identifier, OrmCache, OrmSchema)
    'End Function

    'Public Overrides ReadOnly Property HasChanges() As Boolean
    '    Get
    '        Return False
    '    End Get
    'End Property

    Protected Sub CopyTable2(ByVal [from] As Table10, ByVal [to] As Table10) Implements IOrmEditable(Of Table10).CopyBody
        With [from]
            [to]._tbl1 = ._tbl1
        End With
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
            Using SyncHelper(True, "Table1")
                Return _tbl1
            End Using
        End Get
        Set(ByVal value As Table1)
            Using SyncHelper(False, "Table1")
                _tbl1 = value
            End Using
        End Set
    End Property

End Class

Public Class Table10Implementation
    Inherits ObjectSchemaBaseImplementation

    Private _idx As OrmObjectIndex
    Private _tables() As SourceFragment = {New SourceFragment("dbo.Table10")}

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New OrmObjectIndex
            idx.Add(New MapField2Column("ID", "id", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("Table1", "table1_id", GetTables()(Tables.Main)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As SourceFragment()
        Return _tables
    End Function
End Class