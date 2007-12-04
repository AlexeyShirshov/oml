Imports Worm
Imports Worm.Orm
Imports CoreFramework.Structures

<Entity(GetType(Tables1to1.TablesImplementation), "1")> _
Public Class Tables1to1
    Inherits OrmBaseT(Of Tables1to1)
    Implements IOrmEditable(Of Tables1to1)

    Private _table1 As Table1
    Private _table1back As Table1
    Private _k As String

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As Orm.OrmCacheBase, ByVal schema As Orm.OrmSchemaBase)
        MyBase.New(id, cache, schema)
    End Sub

    'Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)
    '    CopyRelation(CType(from, Tables1to3), CType([to], Tables1to3))
    'End Sub

    Protected Sub CopyRelation(ByVal [from] As Tables1to1, ByVal [to] As Tables1to1) Implements IOrmEditable(Of Tables1to1).CopyBody
        With [from]
            [to]._table1 = ._table1
            [to]._table1back = ._table1back
            [to]._k = ._k
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

    Public Overrides Sub SetValue(ByVal pi As System.Reflection.PropertyInfo, ByVal c As Worm.Orm.ColumnAttribute, ByVal value As Object)
        Select Case c.FieldName
            Case "K"
                K = CStr(value)
            Case "Table1"
                Table1 = CType(value, TestProject1.Table1)
            Case "Table1Back"
                Table1Back = CType(value, TestProject1.Table1)
            Case Else
                MyBase.SetValue(pi, c, value)
        End Select
    End Sub

    <Column("K")> _
    Public Property K() As String
        Get
            Using SyncHelper(True, "K")
                Return _k
            End Using
        End Get
        Set(ByVal value As String)
            Using SyncHelper(False, "K")
                _k = value
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

    <Column("Table1Back")> _
    Public Property Table1Back() As Table1
        Get
            Using SyncHelper(True, "Table1")
                Return _table1back
            End Using
        End Get
        Set(ByVal value As Table1)
            Using SyncHelper(False, "Table1")
                _table1back = value
            End Using
        End Set
    End Property

    Public Class TablesImplementation
        Inherits ObjectSchemaBaseImplementation
        Implements IRelation

        Private _idx As Orm.OrmObjectIndex
        Public Shared _tables() As OrmTable = {New OrmTable("dbo.Table1to1")}

        Public Enum Tables
            Main
        End Enum

        Public Overrides Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column)
            If _idx Is Nothing Then
                Dim idx As New Orm.OrmObjectIndex
                idx.Add(New Orm.MapField2Column("ID", "id", GetTables()(Tables.Main)))
                idx.Add(New Orm.MapField2Column("K", "k", GetTables()(Tables.Main)))
                idx.Add(New Orm.MapField2Column("Table1", "table1", GetTables()(Tables.Main)))
                idx.Add(New Orm.MapField2Column("Table1Back", "table1_back", GetTables()(Tables.Main)))
                _idx = idx
            End If
            Return _idx
        End Function

        Public Overrides Function GetTables() As OrmTable()
            Return _tables
        End Function

        Public Function GetFirstType() As IRelation.RelationDesc Implements Worm.Orm.IRelation.GetFirstType
            Return New IRelation.RelationDesc("Table1", GetType(Table1), False)
        End Function

        Public Function GetSecondType() As IRelation.RelationDesc Implements Worm.Orm.IRelation.GetSecondType
            Return New IRelation.RelationDesc("Table1Back", GetType(Table1), True)
        End Function

    End Class
End Class