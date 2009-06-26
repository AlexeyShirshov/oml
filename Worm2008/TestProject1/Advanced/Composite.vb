Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Cache
Imports Worm.Criteria.Joins

<Entity(GetType(CompositeSchema), "1")> _
Public Class Composite
    Inherits KeyEntity

    Private _m As String
    Private _m2 As String

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

    <EntityPropertyAttribute(PropertyAlias:="Title")> _
    Public Property Message() As String
        Get
            Using Read("Title")
                Return _m
            End Using
        End Get
        Set(ByVal value As String)
            Using Write("Title")
                _m = value
            End Using
        End Set
    End Property

    <EntityPropertyAttribute(PropertyAlias:="Title2", Behavior:=Field2DbRelations.ReadOnly)> _
    Public Property Message2() As String
        Get
            Using Read("Title2")
                Return _m2
            End Using
        End Get
        Set(ByVal value As String)
            Using Write("Title2")
                _m2 = value
            End Using
        End Set
    End Property

    Protected Overrides Sub CopyProperties(ByVal from As Worm.Entities._IEntity, ByVal [to] As Worm.Entities._IEntity, ByVal mgr As Worm.OrmManager, ByVal oschema As Worm.Entities.Meta.IEntitySchema)
        With CType([from], Composite)
            CType([to], Composite)._id = ._id
            CType([to], Composite)._m = ._m
            CType([to], Composite)._m2 = ._m2
        End With
    End Sub
End Class


Public Class CompositeSchema
    Inherits ObjectSchemaBaseImplementation
    Implements IReadonlyObjectSchema, IMultiTableObjectSchema

    Private _idx As OrmObjectIndex
    Private _tables() As SourceFragment = {New SourceFragment("dbo.m1"), New SourceFragment("dbo.m2")}

    Public Enum Tables
        First
        Second
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New OrmObjectIndex
            idx.Add(New MapField2Column("ID", "id", GetTables()(Tables.First)))
            idx.Add(New MapField2Column("Title", "msg", GetTables()(Tables.First)))
            idx.Add(New MapField2Column("Title2", "msg", GetTables()(Tables.Second)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Function GetTables() As SourceFragment() Implements IMultiTableObjectSchema.GetTables
        Return _tables
    End Function

    Public Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As Worm.Criteria.Joins.QueryJoin Implements IMultiTableObjectSchema.GetJoins
        If left.Equals(GetTables()(Tables.First)) AndAlso right.Equals(GetTables()(Tables.Second)) Then
            Return New QueryJoin(right, Worm.Criteria.Joins.JoinType.Join, New JoinFilter(right, "id", _objectType, "ID", Worm.Criteria.FilterOperation.Equal))
        End If
        Throw New NotSupportedException
    End Function

    Public Function GetEditableSchema() As IEntitySchema Implements IReadonlyObjectSchema.GetEditableSchema
        Return New CompositeEditableSchema
    End Function

    Public ReadOnly Property SupportedOperation() As Worm.Entities.Meta.IReadonlyObjectSchema.Operation Implements Worm.Entities.Meta.IReadonlyObjectSchema.SupportedOperation
        Get
            Return IReadonlyObjectSchema.Operation.All
        End Get
    End Property

    Public Overrides ReadOnly Property Table() As Worm.Entities.Meta.SourceFragment
        Get
            Return GetTables(0)
        End Get
    End Property
End Class

Public Class CompositeEditableSchema
    Implements IEntitySchema

    Private _idx As OrmObjectIndex
    Private _tables() As SourceFragment = {New SourceFragment("dbo.m1")}

    Public Enum Tables
        First
    End Enum

    Public Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column) Implements IEntitySchema.GetFieldColumnMap
        If _idx Is Nothing Then
            Dim idx As New OrmObjectIndex
            idx.Add(New MapField2Column("ID", "id", Table))
            idx.Add(New MapField2Column("Title", "msg", Table))
            _idx = idx
        End If
        Return _idx
    End Function

    'Public Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As Worm.Criteria.Joins.QueryJoin Implements IMultiTableWithM2MSchema.GetJoins
    '    Return Nothing
    'End Function

    'Public Function GetTables() As SourceFragment() Implements IMultiTableWithM2MSchema.GetTables
    '    Return _tables
    'End Function

    'Public Function GetM2MRelations() As Worm.Entities.Meta.M2MRelationDesc() Implements Worm.Entities.Meta.ISchemaWithM2M.GetM2MRelations
    '    Return Nothing
    'End Function

    'Public Function GetSuppressedFields() As String() Implements Worm.Entities.Meta.IEntitySchema.GetSuppressedFields
    '    Return Nothing
    'End Function

    'Public Function ChangeValueType(ByVal c As Worm.Entities.Meta.ColumnAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean Implements Worm.Entities.Meta.IEntitySchem.ChangeValueType
    '    newvalue = value
    '    Return False
    'End Function

    Public ReadOnly Property Table() As Worm.Entities.Meta.SourceFragment Implements Worm.Entities.Meta.IEntitySchema.Table
        Get
            Return _tables(0)
        End Get
    End Property
End Class