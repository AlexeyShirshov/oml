Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Cache

<Entity(GetType(Table2Implementation), "1")> _
Public Class Table2
    Inherits KeyEntity
    Implements IOptimizedValues

    Private _tbl1 As Table1
    Private _blob As Byte()
    Private _m As Decimal
    Private _dt As Nullable(Of Date)

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As CacheBase, ByVal schema As Worm.ObjectMappingEngine)
        Init(id, cache, schema)
    End Sub

    Private _id As Integer

    <EntityProperty(Field2DbRelations.PrimaryKey)> _
    Public Property ID() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property

    Public Overrides Property Identifier() As Object
        Get
            Return _id
        End Get
        Set(ByVal value As Object)
            _id = CInt(value)
        End Set
    End Property

    Protected Overrides Sub CopyProperties(ByVal from As Worm.Entities._IEntity, ByVal [to] As Worm.Entities._IEntity, ByVal oschema As Worm.Entities.Meta.IEntitySchema)
        With CType([from], Table2)
            CType([to], Table2)._id = ._id
            CType([to], Table2)._tbl1 = ._tbl1
            CType([to], Table2)._blob = ._blob
            CType([to], Table2)._m = ._m
            CType([to], Table2)._dt = ._dt
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
                Return schema.GetFieldColumnMap(propertyAlias).GetValue(Me)
                'Return GetMappingEngine.GetProperty(Me.GetType, schema, propertyAlias).GetValue(Me, Nothing)
                'Throw New NotSupportedException(propertyAlias)
                'MyBase.SetValue(pi, fieldName, oschema, value)
        End Select
    End Function

    <EntityPropertyAttribute(PropertyAlias:="Table1")> _
    Public Property Tbl() As Table1
        Get
            Using Read("Table1")
                Return _tbl1
            End Using
        End Get
        Set(ByVal value As Table1)
            Using Write("Table1")
                _tbl1 = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="Blob")> _
    Public Property Blob() As Byte()
        Get
            Using Read("Blob")
                Return _blob
            End Using
        End Get
        Set(ByVal value As Byte())
            Using Write("Blob")
                _blob = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="Money")> _
    Public Property Money() As Decimal
        Get
            Using Read("Money")
                Return _m
            End Using
        End Get
        Set(ByVal value As Decimal)
            Using Write("Money")
                _m = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="DT")> _
    Public Property DT() As Nullable(Of Date)
        Get
            Using Read("DT")
                Return _dt
            End Using
        End Get
        Set(ByVal value As Nullable(Of Date))
            Using Write("DT")
                _dt = value
            End Using
        End Set
    End Property

End Class

Public Class Table2Implementation
    Inherits ObjectSchemaBaseImplementation

    Private _idx As OrmObjectIndex
    'Private _tables() As SourceFragment = {New SourceFragment("dbo.Table2")}

    'Public Enum Tables
    '    Main
    'End Enum

    Public Sub New()
        _tbl = New SourceFragment("dbo.Table2")
    End Sub

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New OrmObjectIndex
            idx.Add(New MapField2Column("ID", "id", Table))
            idx.Add(New MapField2Column("Table1", "table1_id", Table))
            idx.Add(New MapField2Column("Blob", "blob", Table))
            idx.Add(New MapField2Column("Money", "m", Table))
            idx.Add(New MapField2Column("DT", "dt2", Table))
            _idx = idx
        End If
        Return _idx
    End Function

    'Public Overrides Function GetTables() As SourceFragment()
    '    Return _tables
    'End Function
End Class
