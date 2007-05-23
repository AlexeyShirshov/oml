Imports Worm
Imports Worm.Orm

<Entity(GetType(Table4Implementation), "1"), Entity(GetType(Table4Implementation2), "2")> _
Public Class Table4
    Inherits OrmBase

    Private _col As Nullable(Of Boolean)
    Private _g As Guid

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As Orm.OrmCacheBase, ByVal schema As Orm.OrmSchemaBase)
        MyBase.New(id, cache, schema)
    End Sub

    Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)
        CopyTable4(CType(from, Table4), CType([to], Table4))
    End Sub

    Protected Shared Sub CopyTable4(ByVal [from] As Table4, ByVal [to] As Table4)
        With [from]
            [to]._col = ._col
            [to]._g = ._g
        End With
    End Sub

    Public Overloads Overrides Function CreateSortComparer(ByVal sort As String, ByVal sortType As Worm.Orm.SortType) As System.Collections.IComparer
        Throw New NotSupportedException
    End Function

    Public Overloads Overrides Function CreateSortComparer(Of T As {New, Worm.Orm.OrmBase})(ByVal sort As String, ByVal sortType As Worm.Orm.SortType) As System.Collections.Generic.IComparer(Of T)
        Throw New NotSupportedException
    End Function

    Protected Overrides Function GetNew() As Worm.Orm.OrmBase
        Return New Table4(Identifier, OrmCache, OrmSchema)
    End Function

    Public Overrides Sub SetValue(ByVal pi As System.Reflection.PropertyInfo, ByVal c As Worm.Orm.ColumnAttribute, ByVal value As Object)
        Select Case c.FieldName
            Case "Col"
                Col = CType(value, Global.System.Nullable(Of Boolean))
            Case "GUID"
                GUID = CType(value, System.Guid)
            Case Else
                MyBase.SetValue(pi, c, value)
        End Select
    End Sub

    <Column("Col")> _
    Public Property Col() As Nullable(Of Boolean)
        Get
            Using SyncHelper(True, "Col")
                Return _col
            End Using
        End Get
        Set(ByVal value As Nullable(Of Boolean))
            Using SyncHelper(False, "Col")
                _col = value
            End Using
        End Set
    End Property

    <Column("GUID", Field2DbRelations.InsertDefault Or Field2DbRelations.SyncInsert)> _
    Public Property GUID() As Guid
        Get
            Using SyncHelper(True, "GUID")
                Return _g
            End Using
        End Get
        Set(ByVal value As Guid)
            Using SyncHelper(False, "GUID")
                _g = value
            End Using
        End Set
    End Property
End Class

Public Class Table4Implementation
    Inherits ObjectSchemaBaseImplementation
    Implements ICacheBehavior

    Private _tables() As OrmTable = {New OrmTable("dbo.[Table]")}

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column)
        Dim idx As New Orm.OrmObjectIndex
        idx.Add(New Orm.MapField2Column("ID", "id", GetTables()(Tables.Main), Field2DbRelations.PK))
        idx.Add(New Orm.MapField2Column("Col", "col", GetTables()(Tables.Main)))
        idx.Add(New Orm.MapField2Column("GUID", "uq", GetTables()(Tables.Main)))
        Return idx
    End Function

    Public Overrides Function GetTables() As OrmTable()
        Return _tables
    End Function

    Public Function GetEntityKey() As String Implements Worm.Orm.ICacheBehavior.GetEntityKey
        Return "kljf"
    End Function

    Public Function GetEntityTypeKey() As Object Implements Worm.Orm.ICacheBehavior.GetEntityTypeKey
        Return "91bn34fh    oebnfklE:"
    End Function
End Class

Public Class Table4Implementation2
    Inherits ObjectSchemaBaseImplementation

    Private _idx As Orm.OrmObjectIndex
    Private _tables() As OrmTable = {New OrmTable("dbo.[Table]")}

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New Orm.OrmObjectIndex
            idx.Add(New Orm.MapField2Column("ID", "id", GetTables()(Tables.Main), Field2DbRelations.PK))
            idx.Add(New Orm.MapField2Column("Col", "col", GetTables()(Tables.Main)))
            idx.Add(New Orm.MapField2Column("GUID", "uq", GetTables()(Tables.Main), Field2DbRelations.InsertDefault))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As OrmTable()
        Return _tables
    End Function
End Class
