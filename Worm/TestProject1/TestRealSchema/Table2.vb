Imports Worm
Imports Worm.Orm

<Entity(GetType(Table2Implementation), "1")> _
Public Class Table2
    Inherits OrmBase

    Private _tbl1 As Table1
    Private _blob As Byte()
    Private _m As Decimal
    Private _dt As Nullable(Of Date)

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As Orm.OrmCacheBase, ByVal schema As Orm.OrmSchemaBase)
        MyBase.New(id, cache, schema)
    End Sub

    Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)
        CopyTable2(CType([from], Table2), CType([to], Table2))
    End Sub

    'Public Overloads Overrides Function CreateSortComparer(ByVal sort As String, ByVal sort_type As Worm.Orm.SortType) As System.Collections.IComparer
    '    Throw New NotImplementedException
    'End Function

    'Public Overloads Overrides Function CreateSortComparer(Of T As {OrmBase, New})(ByVal sort As String, ByVal sort_type As Worm.Orm.SortType) As System.Collections.Generic.IComparer(Of T)
    '    Throw New NotImplementedException
    'End Function

    Protected Overrides Function GetNew() As Worm.Orm.OrmBase
        Return New Table2(Identifier, OrmCache, OrmSchema)
    End Function

    'Public Overrides ReadOnly Property HasChanges() As Boolean
    '    Get
    '        Return False
    '    End Get
    'End Property

    Protected Shared Sub CopyTable2(ByVal [from] As Table2, ByVal [to] As Table2)
        With [from]
            [to]._tbl1 = ._tbl1
            [to]._blob = ._blob
            [to]._m = ._m
            [to]._dt = ._dt
        End With
    End Sub

    Public Overrides Sub SetValue(ByVal pi As System.Reflection.PropertyInfo, ByVal c As Worm.Orm.ColumnAttribute, ByVal value As Object)
        Select Case c.FieldName
            Case "Table1"
                Tbl = CType(value, Table1)
            Case "Blob"
                Blob = CType(value, Byte())
            Case "Money"
                Money = CDec(value)
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

    <Column("Blob")> _
    Public Property Blob() As Byte()
        Get
            Using SyncHelper(True, "Blob")
                Return _blob
            End Using
        End Get
        Set(ByVal value As Byte())
            Using SyncHelper(False, "Blob")
                _blob = value
            End Using
        End Set
    End Property

    <Column("Money")> _
    Public Property Money() As Decimal
        Get
            Using SyncHelper(True, "Money")
                Return _m
            End Using
        End Get
        Set(ByVal value As Decimal)
            Using SyncHelper(False, "Money")
                _m = value
            End Using
        End Set
    End Property

    <Column("DT")> _
    Public Property DT() As Nullable(Of Date)
        Get
            Using SyncHelper(True, "DT")
                Return _dt
            End Using
        End Get
        Set(ByVal value As Nullable(Of Date))
            Using SyncHelper(False, "DT")
                _dt = value
            End Using
        End Set
    End Property
End Class

Public Class Table2Implementation
    Inherits ObjectSchemaBaseImplementation

    Private _idx As Orm.OrmObjectIndex
    Private _tables() As OrmTable = {New OrmTable("dbo.Table2")}

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New Orm.OrmObjectIndex
            idx.Add(New Orm.MapField2Column("ID", "id", GetTables()(tables.Main)))
            idx.Add(New Orm.MapField2Column("Table1", "table1_id", GetTables()(tables.Main)))
            idx.Add(New Orm.MapField2Column("Blob", "blob", GetTables()(tables.Main)))
            idx.Add(New Orm.MapField2Column("Money", "m", GetTables()(tables.Main)))
            idx.Add(New Orm.MapField2Column("DT", "dt2", GetTables()(tables.Main)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As OrmTable()
        Return _tables
    End Function
End Class
