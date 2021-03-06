Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Cache
Imports Worm.Criteria.Joins

<Entity(GetType(CompositeSchema), "1")> _
Public Class Composite
    Inherits SinglePKEntity
    Implements ICopyProperties

    Private _m As String
    Private _m2 As String

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer)
        _id = id
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

    Protected Function CopyProperties(ByVal [to] As Object) As Boolean Implements ICopyProperties.CopyTo
        Dim dst = TryCast([to], Composite)
        If dst IsNot Nothing Then

            With Me
                dst._id = ._id
                dst._m = ._m
                dst._m2 = ._m2
            End With

            Return True
        End If

        Return False
    End Function
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

    Public Overrides ReadOnly Property FieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        Get
            If _idx Is Nothing Then
                Dim idx As New OrmObjectIndex
                idx.Add(New MapField2Column("ID", GetTables()(Tables.First), "id"))
                idx.Add(New MapField2Column("Title", GetTables()(Tables.First), "msg"))
                idx.Add(New MapField2Column("Title2", GetTables()(Tables.Second), "msg"))
                _idx = idx
            End If
            Return _idx
        End Get
    End Property

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
            Return GetTables()(0)
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

    Public ReadOnly Property FieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column) Implements IEntitySchema.FieldColumnMap
        Get
            If _idx Is Nothing Then
                Dim idx As New OrmObjectIndex
                idx.Add(New MapField2Column("ID", Table, "id"))
                idx.Add(New MapField2Column("Title", Table, "msg"))
                _idx = idx
            End If
            Return _idx
        End Get
    End Property

    Public ReadOnly Property Table() As Worm.Entities.Meta.SourceFragment Implements Worm.Entities.Meta.IEntitySchema.Table
        Get
            Return _tables(0)
        End Get
    End Property
End Class