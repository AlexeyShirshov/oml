Imports Worm
Imports Worm.Orm
Imports CoreFramework.Structures

<Entity(GetType(TablesImplementation), "1")> _
Public Class Tables1to3
    Inherits OrmBase

    Private _name As String
    Private _table1 As Table1
    Private _table3 As Table33

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As Orm.OrmCacheBase, ByVal schema As Orm.OrmSchemaBase)
        MyBase.New(id, cache, schema)
    End Sub

    Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)
        CopyRelation(CType(from, Tables1to3), CType([to], Tables1to3))
    End Sub

    Protected Shared Sub CopyRelation(ByVal [from] As Tables1to3, ByVal [to] As Tables1to3)
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

    Protected Overrides Function GetNew() As Worm.Orm.OrmBase
        Return New Tables1to3(Identifier, OrmCache, OrmSchema)
    End Function

    Public Overrides Sub SetValue(ByVal pi As System.Reflection.PropertyInfo, ByVal c As Worm.Orm.ColumnAttribute, ByVal value As Object)
        Select Case c.FieldName
            Case "Title"
                Title = CStr(value)
            Case "Table1"
                Table1 = CType(value, TestProject1.Table1)
            Case "Table3"
                Table3 = CType(value, TestProject1.Table33)
            Case Else
                MyBase.SetValue(pi, c, value)
        End Select
    End Sub

    <Column("Title")> _
    Public Property Title() As String
        Get
            Using SyncHelper(True, "Title")
                Return _name
            End Using
        End Get
        Set(ByVal value As String)
            Using SyncHelper(False, "Title")
                _name = value
            End Using
        End Set
    End Property

    <Column("Table1")> _
    Public Property Table1() As Table1
        Get
            Using SyncHelper(True, "Table1")
                Return _table1
            End Using
        End Get
        Set(ByVal value As Table1)
            Using SyncHelper(False, "Table1")
                _table1 = value
            End Using
        End Set
    End Property

    <Column("Table3")> _
    Public Property Table3() As Table33
        Get
            Using SyncHelper(True, "Table3")
                Return _table3
            End Using
        End Get
        Set(ByVal value As Table33)
            Using SyncHelper(False, "Table3")
                _table3 = value
            End Using
        End Set
    End Property

End Class


Public Class TablesImplementation
    Inherits ObjectSchemaBaseImplementation
    Implements IRelation

    Private _idx As Orm.OrmObjectIndex
    Public Shared _tables() As OrmTable = {New OrmTable("dbo.Tables1to3Relation")}

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New Orm.OrmObjectIndex
            idx.Add(New Orm.MapField2Column("ID", "id", GetTables()(Tables.Main)))
            idx.Add(New Orm.MapField2Column("Title", "name", GetTables()(Tables.Main)))
            idx.Add(New Orm.MapField2Column("Table1", "table1", GetTables()(Tables.Main)))
            idx.Add(New Orm.MapField2Column("Table3", "table3", GetTables()(Tables.Main)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As OrmTable()
        Return _tables
    End Function

    Public Function GetFirstType() As Pair(Of String, System.Type) Implements Worm.Orm.IRelation.GetFirstType
        Return New Pair(Of String, Type)("Table1", GetType(Table33))
    End Function

    Public Function GetSecondType() As Pair(Of String, System.Type) Implements Worm.Orm.IRelation.GetSecondType
        Return New Pair(Of String, Type)("Table3", GetType(Table1))
    End Function
End Class
