Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Cache

<Entity(GetType(Table2Implementation), "1")> _
Public Class Table2
    Inherits OrmBaseT(Of Table2)
    Implements IOrmEditable(Of Table2), IOptimizedValues

    Private _tbl1 As Table1
    Private _blob As Byte()
    Private _m As Decimal
    Private _dt As Nullable(Of Date)

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As CacheBase, ByVal schema As Worm.ObjectMappingEngine)
        MyBase.New(id, cache, schema)
    End Sub

    'Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)
    '    CopyTable2(CType([from], Table2), CType([to], Table2))
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

    Protected Sub CopyTable2(ByVal [from] As Table2, ByVal [to] As Table2) Implements IOrmEditable(Of Table2).CopyBody
        With [from]
            [to]._tbl1 = ._tbl1
            [to]._blob = ._blob
            [to]._m = ._m
            [to]._dt = ._dt
        End With
    End Sub

    Public Overridable Sub SetValue( _
        ByVal fieldName As String, ByVal oschema As IEntitySchema, ByVal value As Object) Implements IOptimizedValues.SetValueOptimized
        Select Case fieldName
            Case "Table1"
                Tbl = CType(value, Table1)
            Case "Blob"
                Blob = CType(value, Byte())
            Case "Money"
                Money = CDec(value)
            Case "ID"
                Identifier = value
            Case "DT"
                DT = CType(value, Date?)
            Case Else
                Throw New NotSupportedException(fieldName)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select
    End Sub

    Public Function GetValueOptimized(ByVal propertyAlias As String, ByVal schema As Worm.Entities.Meta.IEntitySchema) As Object Implements Worm.Entities.IOptimizedValues.GetValueOptimized
        Select Case propertyAlias
            Case "Table1"
                Return _tbl1
            Case "Blob"
                Return _blob
            Case "Money"
                Return _m
            Case "DT"
                Return _dt
            Case Else
                Return MappingEngine.GetProperty(Me.GetType, schema, propertyAlias).GetValue(Me, Nothing)
                'Throw New NotSupportedException(propertyAlias)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select
    End Function

    <EntityPropertyAttribute("Table1")> _
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

    <EntityPropertyAttribute("Blob")> _
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

    <EntityPropertyAttribute("Money")> _
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

    <EntityPropertyAttribute("DT")> _
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

    Private _idx As OrmObjectIndex
    Private _tables() As SourceFragment = {New SourceFragment("dbo.Table2")}

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New OrmObjectIndex
            idx.Add(New MapField2Column("ID", "id", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("Table1", "table1_id", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("Blob", "blob", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("Money", "m", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("DT", "dt2", GetTables()(Tables.Main)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As SourceFragment()
        Return _tables
    End Function
End Class
