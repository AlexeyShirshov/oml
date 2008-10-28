Imports Worm.Cache
Imports Worm.Orm
Imports Worm.Orm.Meta

<Entity(GetType(Table4Implementation), "1"), Entity(GetType(Table4Implementation2), "2")> _
Public Class Table4
    Inherits OrmBaseT(Of Table4)
    Implements IOrmEditable(Of Table4)

    Private _col As Nullable(Of Boolean)
    Private _g As Guid

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As CacheBase, ByVal schema As Worm.ObjectMappingEngine)
        MyBase.New(id, cache, schema)
    End Sub

    'Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)
    '    CopyTable4(CType(from, Table4), CType([to], Table4))
    'End Sub

    Protected Sub CopyTable4(ByVal [from] As Table4, ByVal [to] As Table4) Implements IOrmEditable(Of Table4).CopyBody
        With [from]
            [to]._col = ._col
            [to]._g = ._g
        End With
    End Sub

    'Public Overloads Overrides Function CreateSortComparer(ByVal sort As String, ByVal sortType As Worm.Orm.SortType) As System.Collections.IComparer
    '    Throw New NotSupportedException
    'End Function

    'Public Overloads Overrides Function CreateSortComparer(Of T As {New, Worm.Orm.OrmBase})(ByVal sort As String, ByVal sortType As Worm.Orm.SortType) As System.Collections.Generic.IComparer(Of T)
    '    Throw New NotSupportedException
    'End Function

    'Protected Overrides Function GetNew() As Worm.Orm.OrmBase
    '    Return New Table4(Identifier, OrmCache, OrmSchema)
    'End Function

    Public Overrides Sub SetValue(ByVal pi As System.Reflection.PropertyInfo, ByVal c As ColumnAttribute, ByVal oschema As IObjectSchemaBase, ByVal value As Object)
        Select Case c.FieldName
            Case "Col"
                Col = CType(value, Global.System.Nullable(Of Boolean))
            Case "GUID"
                GUID = CType(value, System.Guid)
            Case Else
                MyBase.SetValue(pi, c, oschema, value)
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

    Private _tables() As SourceFragment = {New SourceFragment("dbo.[Table]")}

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        Dim idx As New OrmObjectIndex
        idx.Add(New MapField2Column("ID", "id", GetTables()(Tables.Main), Field2DbRelations.PK))
        idx.Add(New MapField2Column("Col", "col", GetTables()(Tables.Main)))
        idx.Add(New MapField2Column("GUID", "uq", GetTables()(Tables.Main)))
        Return idx
    End Function

    Public Overrides Function GetTables() As SourceFragment()
        Return _tables
    End Function

    Public Function GetEntityKey(ByVal filterInfo As Object) As String Implements ICacheBehavior.GetEntityKey
        Return "kljf"
    End Function

    Public Function GetEntityTypeKey(ByVal filterInfo As Object) As Object Implements ICacheBehavior.GetEntityTypeKey
        Return "91bn34fh    oebnfklE:"
    End Function
End Class

Public Class Table4Implementation2
    Inherits ObjectSchemaBaseImplementation

    Private _idx As OrmObjectIndex
    Private _tables() As SourceFragment = {New SourceFragment("dbo.[Table]")}

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New OrmObjectIndex
            idx.Add(New MapField2Column("ID", "id", GetTables()(Tables.Main), Field2DbRelations.PK))
            idx.Add(New MapField2Column("Col", "col", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("GUID", "uq", GetTables()(Tables.Main), Field2DbRelations.InsertDefault))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As SourceFragment()
        Return _tables
    End Function
End Class
