Imports Worm
Imports Worm.Orm

<Entity(GetType(Table10Implementation), "1")> _
Public Class Table10
    Inherits OrmBase

    Private _tbl1 As Table1

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As Orm.OrmCacheBase, ByVal schema As Orm.OrmSchemaBase)
        MyBase.New(id, cache, schema)
    End Sub

    Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)
        CopyTable2(CType([from], Table10), CType([to], Table10))
    End Sub

    Public Overloads Overrides Function CreateSortComparer(ByVal sort As String, ByVal sort_type As Worm.Orm.SortType) As System.Collections.IComparer
        Throw New NotImplementedException
    End Function

    Public Overloads Overrides Function CreateSortComparer(Of T As {OrmBase, New})(ByVal sort As String, ByVal sort_type As Worm.Orm.SortType) As System.Collections.Generic.IComparer(Of T)
        Throw New NotImplementedException
    End Function

    Protected Overrides Function GetNew() As Worm.Orm.OrmBase
        Return New Table2(Identifier, OrmCache, OrmSchema)
    End Function

    'Public Overrides ReadOnly Property HasChanges() As Boolean
    '    Get
    '        Return False
    '    End Get
    'End Property

    Protected Shared Sub CopyTable2(ByVal [from] As Table10, ByVal [to] As Table10)
        With [from]
            [to]._tbl1 = ._tbl1
        End With
    End Sub

    Public Overrides Sub SetValue(ByVal pi As System.Reflection.PropertyInfo, ByVal c As Worm.Orm.ColumnAttribute, ByVal value As Object)
        Select Case c.FieldName
            Case "Table1"
                Tbl = CType(value, Table1)
            Case Else
                MyBase.SetValue(pi, c, value)
        End Select
    End Sub

    <Column("Table1")> _
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

    Private _idx As Orm.OrmObjectIndex
    Private _tables() As OrmTable = {New OrmTable("dbo.Table10")}

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New Orm.OrmObjectIndex
            idx.Add(New Orm.MapField2Column("ID", "id", GetTables()(Tables.Main)))
            idx.Add(New Orm.MapField2Column("Table1", "table1_id", GetTables()(Tables.Main)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As OrmTable()
        Return _tables
    End Function
End Class