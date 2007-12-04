Imports Worm
Imports Worm.Orm

<Entity(GetType(CompositeSchema), "1")> _
Public Class Composite
    Inherits OrmBaseT(Of Composite)
    Implements IOrmEditable(Of Composite)

    Private _m As String
    Private _m2 As String

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As Orm.OrmCacheBase, ByVal schema As Orm.OrmSchemaBase)
        MyBase.New(id, cache, schema)
    End Sub

    <Column("Title")> _
    Public Property Message() As String
        Get
            Using SyncHelper(True, "Title")
                Return _m
            End Using
        End Get
        Set(ByVal value As String)
            Using SyncHelper(False, "Title")
                _m = value
            End Using
        End Set
    End Property

    <Column("Title2", Field2DbRelations.ReadOnly)> _
    Public Property Message2() As String
        Get
            Using SyncHelper(True, "Title2")
                Return _m2
            End Using
        End Get
        Set(ByVal value As String)
            Using SyncHelper(False, "Title2")
                _m2 = value
            End Using
        End Set
    End Property

    Public Sub CopyBody(ByVal from As Composite, ByVal [to] As Composite) Implements Worm.Orm.IOrmEditable(Of Composite).CopyBody
        With [from]
            [to]._m = ._m
            [to]._m2 = ._m2
        End With
    End Sub
End Class


Public Class CompositeSchema
    Inherits ObjectSchemaBaseImplementation
    Implements IReadonlyObjectSchema

    Private _idx As Orm.OrmObjectIndex
    Private _tables() As OrmTable = {New OrmTable("dbo.m1"), New OrmTable("dbo.m2")}

    Public Enum Tables
        First
        Second
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New Orm.OrmObjectIndex
            idx.Add(New Orm.MapField2Column("ID", "id", GetTables()(Tables.First)))
            idx.Add(New Orm.MapField2Column("Title", "msg", GetTables()(Tables.First)))
            idx.Add(New Orm.MapField2Column("Title2", "msg", GetTables()(Tables.Second)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As Worm.Orm.OrmTable()
        Return _tables
    End Function

    Public Overrides Function GetJoins(ByVal left As Worm.Orm.OrmTable, ByVal right As Worm.Orm.OrmTable) As Worm.Orm.OrmJoin
        If left.Equals(GetTables()(Tables.First)) AndAlso right.Equals(GetTables()(Tables.Second)) Then
            Return New Orm.OrmJoin(right, Orm.JoinType.Join, New Orm.JoinFilter(right, "id", _objectType, "ID", Orm.FilterOperation.Equal))
        End If
        Return MyBase.GetJoins(left, right)
    End Function

    Public Function GetEditableSchema() As Worm.Orm.IRelMapObjectSchema Implements Worm.Orm.IReadonlyObjectSchema.GetEditableSchema
        Return New CompositeEditableSchema
    End Function
End Class

Public Class CompositeEditableSchema
    Implements IRelMapObjectSchema

    Private _idx As Orm.OrmObjectIndex
    Private _tables() As OrmTable = {New OrmTable("dbo.m1")}

    Public Enum Tables
        First
    End Enum

    Public Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column) Implements Worm.Orm.IRelMapObjectSchema.GetFieldColumnMap
        If _idx Is Nothing Then
            Dim idx As New Orm.OrmObjectIndex
            idx.Add(New Orm.MapField2Column("ID", "id", GetTables()(Tables.First)))
            idx.Add(New Orm.MapField2Column("Title", "msg", GetTables()(Tables.First)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Function GetJoins(ByVal left As Worm.Orm.OrmTable, ByVal right As Worm.Orm.OrmTable) As Worm.Orm.OrmJoin Implements Worm.Orm.IOrmRelationalSchema.GetJoins
        Return Nothing
    End Function

    Public Function GetTables() As Worm.Orm.OrmTable() Implements Worm.Orm.IOrmRelationalSchema.GetTables
        Return _tables
    End Function
End Class