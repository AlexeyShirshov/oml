Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Database
Imports Worm.Cache
Imports Worm.Entities.Meta
Imports Worm.Criteria.Values
Imports Worm.Sorting
Imports Worm.Criteria.Core
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Joins
Imports Worm.Query

Public Interface IEnt
    Inherits _IKeyEntity

End Interface

<Serializable()> _
<Worm.Entities.Meta.Entity(GetType(EntitySchema1v1Implementation), "1"), _
Worm.Entities.Meta.Entity(GetType(EntitySchema1v2Implementation), "2"), _
Worm.Entities.Meta.Entity(GetType(EntitySchema1v3Implementation), "3"), _
Worm.Entities.Meta.Entity(GetType(EntitySchema1v2Implementation), "joins")> _
Public Class Entity
    Inherits OrmBaseT(Of Entity)
    Implements IEnt

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As CacheBase, ByVal schema As Worm.ObjectMappingEngine)
        MyBase.New(id, cache, schema)
    End Sub

    'Protected Overrides Function GetNew() As Worm.Orm.OrmBase
    '    Return New Entity(Identifier, OrmCache, OrmSchema)
    'End Function

    'Public Overloads Overrides Function CreateSortComparer(ByVal sort As String, ByVal sort_type As Worm.Orm.SortType) As System.Collections.IComparer
    '    Throw New NotImplementedException
    'End Function

    'Public Overloads Overrides Function CreateSortComparer(Of T As {Orm.OrmBase, New})(ByVal sort As String, ByVal sort_type As Worm.Orm.SortType) As System.Collections.Generic.IComparer(Of T)
    '    Throw New NotImplementedException
    'End Function

    'Public Overrides ReadOnly Property HasChanges() As Boolean
    '    Get
    '        Return False
    '    End Get
    'End Property

    'Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)

    'End Sub
End Class

Public MustInherit Class ObjectSchemaBaseImplementation
    Implements IOrmObjectSchema, ISchemaInit

    Protected _schema As Worm.ObjectMappingEngine
    Protected _objectType As Type

    Public Overridable Function ChangeValueType(ByVal c As EntityPropertyAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean Implements IOrmObjectSchema.ChangeValueType
        newvalue = value
        Return False
    End Function

    Public Overridable Function GetFilter(ByVal filter_info As Object) As Worm.Criteria.Core.IFilter Implements IOrmObjectSchema.GetContextFilter
        Return Nothing
    End Function

    Public Overridable Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As Worm.Criteria.Joins.QueryJoin Implements IOrmObjectSchema.GetJoins
        Return Nothing
    End Function

    Public Overridable Function GetSuppressedFields() As String() Implements IOrmObjectSchema.GetSuppressedFields
        Return New String() {}
    End Function

    'Public Overridable Function MapSort2FieldName(ByVal sort As String) As String Implements Worm.Orm.IOrmObjectSchema.MapSort2FieldName
    '    Return Nothing
    'End Function

    Public MustOverride Function GetTables() As SourceFragment() Implements IOrmObjectSchema.GetTables

    Public MustOverride Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column) Implements IOrmObjectSchema.GetFieldColumnMap

    'Public Function ExternalSort(ByVal sort As String, ByVal sortType As Orm.SortType, ByVal objs As Collections.IList) As Collections.IList Implements Worm.Orm.IOrmObjectSchema.ExternalSort
    '    Return objs
    'End Function

    'Public ReadOnly Property IsExternalSort(ByVal sort As String) As Boolean Implements Worm.Orm.IOrmObjectSchema.IsExternalSort
    '    Get
    '        Return False
    '    End Get
    'End Property

    Public Overridable Function GetM2MRelations() As M2MRelationDesc() Implements IOrmObjectSchema.GetM2MRelations
        Return New M2MRelationDesc() {}
    End Function

    Public Sub GetSchema(ByVal schema As Worm.ObjectMappingEngine, ByVal t As System.Type) Implements ISchemaInit.GetSchema
        _schema = schema
        _objectType = t
    End Sub

    Public ReadOnly Property Table() As Worm.Entities.Meta.SourceFragment Implements Worm.Entities.Meta.IEntitySchema.Table
        Get
            Return GetTables(0)
        End Get
    End Property
End Class

Public Class EntitySchema1v1Implementation
    Inherits ObjectSchemaBaseImplementation

    Private _idx As OrmObjectIndex
    Protected _tables() As SourceFragment = {New SourceFragment("dbo.ent1")}
    Protected _rels() As M2MRelationDesc

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New OrmObjectIndex
            idx.Add(New MapField2Column("ID", "id", GetTables()(Tables.Main)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As SourceFragment()
        Return _tables
    End Function

    Public Overrides Function GetM2MRelations() As M2MRelationDesc()
        If _rels Is Nothing Then
            _rels = New M2MRelationDesc() { _
                New M2MRelationDesc(GetType(Entity4), _schema.GetSharedSourceFragment("dbo", "[1to2]"), "ent2_id", False, New System.Data.Common.DataTableMapping, M2MRelationDesc.DirKey, Nothing, Ctor.column(_schema.GetSharedSourceFragment("dbo", "[1to2]"), "xxx").eq("yyy")) _
                }
        End If
        Return _rels
    End Function
End Class

Public Class EntitySchema1v2Implementation
    Inherits EntitySchema1v1Implementation

    Public Enum Tables2
        Main
        Second
    End Enum

    Public Overrides Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As Worm.Criteria.Joins.QueryJoin
        If left.Equals(GetTables()(Tables2.Main)) AndAlso right.Equals(GetTables()(Tables2.Second)) Then
            Return New QueryJoin(right, Worm.Criteria.Joins.JoinType.Join, New JoinFilter(right, "i", _objectType, "ID", Worm.Criteria.FilterOperation.Equal))
        End If
        Return MyBase.GetJoins(left, right)
    End Function

    Public Overrides Function GetTables() As SourceFragment()
        If _tables.Length = 1 Then
            Dim s As New List(Of SourceFragment)(MyBase.GetTables())
            s.Add(New SourceFragment("dbo.t1"))
            _tables = s.ToArray
        End If
        Return _tables
    End Function

    Public Overrides Function GetM2MRelations() As M2MRelationDesc()
        If _rels Is Nothing Then
            _rels = New M2MRelationDesc() { _
                New M2MRelationDesc(GetType(Entity4), New SourceFragment("dbo.[1to2]"), "ent2_id", True, New System.Data.Common.DataTableMapping) _
                }
        End If
        Return _rels
    End Function

End Class

Public Class EntitySchema1v3Implementation
    Inherits EntitySchema1v2Implementation

    Public Overrides Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As Worm.Criteria.Joins.QueryJoin
        If left.Equals(GetTables()(Tables2.Main)) AndAlso right.Equals(GetTables()(Tables2.Second)) Then
            Dim orc As New Condition.ConditionConstructor
            orc.AddFilter(New JoinFilter(right, "i", _objectType, "ID", Worm.Criteria.FilterOperation.Equal))
            orc.AddFilter(New TableFilter(right, "s", New ScalarValue("a"), Worm.Criteria.FilterOperation.Equal))
            Return New QueryJoin(right, Worm.Criteria.Joins.JoinType.Join, CType(orc.Condition, IFilter))
        End If
        Return MyBase.GetJoins(left, right)
    End Function

End Class

<Entity(GetType(EntitySchema2v1Implementation), "1"), _
Entity(GetType(EntitySchema2v2Implementation), "2")> _
Public Class Entity2
    Inherits Entity

    Private _s As String

    Public Sub New()

    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As CacheBase, ByVal schema As Worm.ObjectMappingEngine)
        MyBase.New(id, cache, schema)
    End Sub

    'Public Overrides Function Clone() As Object
    '    Dim e As New Entity2(Identifier, InternalProperties.OrmCache, OrmSchema)
    '    Return e
    'End Function

    <EntityPropertyAttribute(PropertyAlias:="Str")> _
    Public Property Str() As String
        Get
            Using SyncHelper(True, "Str")
                Return _s
            End Using
        End Get
        Set(ByVal value As String)
            Using SyncHelper(False, "Str")
                _s = value
            End Using
        End Set
    End Property
End Class

Public Class EntitySchema2v1Implementation
    Inherits EntitySchema1v2Implementation

    Private _coladded As Boolean = False

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        Dim idx As Worm.Collections.IndexedCollection(Of String, MapField2Column) = MyBase.GetFieldColumnMap()
        If Not _coladded Then
            Try
                idx.Add(New MapField2Column("Str", "s", GetTables()(Tables2.Second)))
            Catch ex As ArgumentException
                'just eat
                'Diagnostics.Debug.WriteLine("duplicate add")
            End Try
            _coladded = True
        End If
        Return idx
    End Function
End Class

Public Class EntitySchema2v2Implementation
    Inherits EntitySchema1v1Implementation

    Private _coladded As Boolean = False

    Public Enum Tables2
        Main
        Second
    End Enum

    Public Overrides Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As Worm.Criteria.Joins.QueryJoin
        If left.Equals(GetTables()(Tables2.Main)) AndAlso right.Equals(GetTables()(Tables2.Second)) Then
            Return New QueryJoin(right, Worm.Criteria.Joins.JoinType.Join, New JoinFilter(right, "i", _objectType, "ID", Worm.Criteria.FilterOperation.Equal))
        End If
        Return MyBase.GetJoins(left, right)
    End Function

    Public Overrides Function GetTables() As SourceFragment()
        If _tables.Length = 1 Then
            Dim s As New List(Of SourceFragment)(MyBase.GetTables())
            s.Add(New SourceFragment("dbo.t2"))
            _tables = s.ToArray
        End If
        Return _tables
    End Function

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        Dim idx As Worm.Collections.IndexedCollection(Of String, MapField2Column) = MyBase.GetFieldColumnMap()
        If Not _coladded Then
            Try
                idx.Add(New MapField2Column("Str", "s", GetTables()(Tables2.Second)))
            Catch ex As ArgumentException
                'just eat
                'Diagnostics.Debug.WriteLine("duplicate add")
            End Try
            _coladded = True
        End If
        Return idx
    End Function
End Class

Public Class Entity3
    Inherits Entity

    'Public Sub New(ByVal id As Integer, ByVal cache As OrmCacheBase, ByVal schema As SQLGenerator)
    '    MyBase.New(id, cache, schema)
    'End Sub
End Class

<Serializable()> _
<Entity(GetType(EntitySchema4v1Implementation), "1"), _
Entity(GetType(EntitySchema4v1Implementation), "joins"), _
Entity(GetType(EntitySchema4v2Implementation), "2")> _
Public Class Entity4
    Inherits OrmBaseT(Of Entity4)
    Implements IOrmEditable(Of Entity4)

    Private _name As String

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As CacheBase, ByVal schema As Worm.ObjectMappingEngine)
        MyBase.New(id, cache, schema)
    End Sub

    'Protected Overrides Function GetNew() As Worm.Orm.OrmBase
    '    Return New Entity4(Identifier, OrmCache, OrmSchema)
    'End Function

    'Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)
    '    CopyEntity4(CType(from, Entity4), CType([to], Entity4))
    'End Sub

    Protected Sub CopyEntity4(ByVal from As Entity4, ByVal [to] As Entity4) Implements IOrmEditable(Of Entity4).CopyBody
        With [from]
            [to]._name = ._name
        End With
    End Sub

    'Public Overloads Overrides Function CreateSortComparer(ByVal sort As String, ByVal sortType As Worm.Orm.SortType) As System.Collections.IComparer
    '    Return New Comparer(CType(System.Enum.Parse(GetType(Entity4.Entity4Sort), sort), Entity4.Entity4Sort), sortType)
    'End Function

    'Public Overloads Overrides Function CreateSortComparer(Of T As {Orm.OrmBase, New})(ByVal sort As String, ByVal sortType As Worm.Orm.SortType) As System.Collections.Generic.IComparer(Of T)
    '    Return CType(New Comparer(CType(System.Enum.Parse(GetType(Entity4.Entity4Sort), sort), Entity4.Entity4Sort), sortType), Global.System.Collections.Generic.IComparer(Of T))
    'End Function

    'Public Overrides ReadOnly Property HasChanges() As Boolean
    '    Get
    '        Return False
    '    End Get
    'End Property

    <EntityPropertyAttribute(PropertyAlias:="Title")> _
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

End Class

Public Class EntitySchema4v1Implementation
    Inherits ObjectSchemaBaseImplementation
    Implements IOrmSorting2, IOrmSorting

    Private _idx As OrmObjectIndex
    Protected _tables() As SourceFragment = {New SourceFragment("dbo.ent2")}
    Protected _rels() As M2MRelationDesc

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New OrmObjectIndex
            idx.Add(New MapField2Column("ID", "id", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("Title", "name", GetTables()(Tables.Main)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As SourceFragment()
        Return _tables
    End Function

    'Public Overrides Function MapSort2FieldName(ByVal sort As String) As String
    '    Select Case CType(System.Enum.Parse(GetType(Entity4.Entity4Sort), sort), Entity4.Entity4Sort)
    '        Case Entity4.Entity4Sort.Name
    '            Return "Title"
    '        Case Else
    '            Throw New NotSupportedException("Sorting " & sort & " is not supported")
    '    End Select
    'End Function

    Public Overrides Function GetM2MRelations() As M2MRelationDesc()
        If _rels Is Nothing Then
            _rels = New M2MRelationDesc() {New M2MRelationDesc(GetType(Entity), _schema.GetSharedSourceFragment("dbo", "[1to2]"), "ent1_id", False, New System.Data.Common.DataTableMapping)}
        End If
        Return _rels
    End Function

    Public Function CreateSortComparer(ByVal s As Sort) As System.Collections.IComparer Implements IOrmSorting.CreateSortComparer
        If s.SortBy = "Title" Then
            Return New Comparer(Entity4Sort.Name, s.Order)
        End If
        Return Nothing
    End Function

    Public Function CreateSortComparer1(Of T As {_IEntity})(ByVal s As Sort) As System.Collections.Generic.IComparer(Of T) Implements IOrmSorting.CreateSortComparer
        If s.SortBy = "Title" Then
            Return CType(New Comparer(Entity4Sort.Name, s.Order), Global.System.Collections.Generic.IComparer(Of T))
        End If
        Return Nothing
    End Function

    'Public Function ExternalSort(Of T As {New, Worm.Orm.OrmBase})(ByVal s As Sort, ByVal objs As Worm.ReadOnlyList(Of T)) As Worm.ReadOnlyList(Of T) Implements IOrmSorting.ExternalSort
    '    Throw New NotSupportedException
    'End Function

    Public Enum Entity4Sort
        Name
    End Enum

    Public Class Comparer
        Implements System.Collections.IComparer, System.Collections.Generic.IComparer(Of Entity4)

        Private _s As Entity4Sort
        Private _st As Integer = -1

        Public Sub New(ByVal s As Entity4Sort, ByVal st As SortType)
            _s = s
            If st = SortType.Asc Then
                _st = 1
            End If
        End Sub

        Protected Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements System.Collections.IComparer.Compare
            Return Compare(TryCast(x, Entity4), TryCast(y, Entity4))
        End Function

        Public Function Compare(ByVal x As Entity4, ByVal y As Entity4) As Integer Implements System.Collections.Generic.IComparer(Of Entity4).Compare
            If x Is Nothing Then
                If y Is Nothing Then
                    Return 0
                Else
                    Return -1 * _st
                End If
            Else
                If y Is Nothing Then
                    Return _st
                Else
                    Select Case _s
                        Case Entity4Sort.Name
                            Return x.Title.CompareTo(y.Title) * _st
                    End Select
                End If
            End If
        End Function
    End Class

    Public ReadOnly Property SortExpiration(ByVal s As Sort) As System.TimeSpan Implements IOrmSorting2.SortExpiration
        Get
            Return TimeSpan.MaxValue
        End Get
    End Property
End Class

Public Class EntitySchema4v2Implementation
    Inherits EntitySchema4v1Implementation

    Public Enum Tables2
        Main
        Second
    End Enum

    Public Overrides Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As Worm.Criteria.Joins.QueryJoin
        If left.Equals(GetTables()(Tables2.Main)) AndAlso right.Equals(GetTables()(Tables2.Second)) Then
            Return New QueryJoin(right, Worm.Criteria.Joins.JoinType.Join, New JoinFilter(right, "i", _objectType, "ID", Worm.Criteria.FilterOperation.Equal))
        End If
        Return MyBase.GetJoins(left, right)
    End Function

    Public Overrides Function GetTables() As SourceFragment()
        If _tables.Length = 1 Then
            Dim s As New List(Of SourceFragment)(MyBase.GetTables())
            s.Add(New SourceFragment("dbo.t1"))
            _tables = s.ToArray
        End If
        Return _tables
    End Function

    Public Overrides Function GetFilter(ByVal filter_info As Object) As Worm.Criteria.Core.IFilter
        If filter_info Is Nothing Then
            Return Nothing
        End If

        If filter_info.GetType IsNot GetType(String) Then
            Throw New OrmObjectException("Invalid filter_info type " & filter_info.GetType.Name)
        End If

        Return New TableFilter(GetTables()(Tables2.Main), "s", New ScalarValue(filter_info), Worm.Criteria.FilterOperation.Equal)
    End Function

    Public Overrides Function GetM2MRelations() As M2MRelationDesc()
        If _rels Is Nothing Then
            _rels = New M2MRelationDesc() {New M2MRelationDesc(GetType(Entity), New SourceFragment("dbo.[1to2]"), "ent1_id", True, New System.Data.Common.DataTableMapping)}
        End If
        Return _rels
    End Function

End Class

<Entity(GetType(EntitySchema5v1Implementation), "1")> _
Public Class Entity5
    Inherits OrmBaseT(Of Entity5)
    Implements IOrmEditable(Of Entity5)

    Private _name As String
    Private _mark() As Byte

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As CacheBase, ByVal schema As Worm.ObjectMappingEngine)
        MyBase.New(id, cache, schema)
    End Sub

    'Protected Overrides Function GetNew() As Worm.Orm.OrmBase
    '    Return New Entity5(Identifier, OrmCache, OrmSchema)
    'End Function

    'Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)
    '    CopyEntity5(CType([from], Entity5), CType([to], Entity5))
    'End Sub

    'Public Overloads Overrides Function CreateSortComparer(ByVal sort As String, ByVal sort_type As Worm.Orm.SortType) As System.Collections.IComparer
    '    Throw New NotImplementedException
    'End Function

    'Public Overloads Overrides Function CreateSortComparer(Of T As {Orm.OrmBase, New})(ByVal sort As String, ByVal sort_type As Worm.Orm.SortType) As System.Collections.Generic.IComparer(Of T)
    '    Throw New NotImplementedException
    'End Function

    'Public Overrides ReadOnly Property HasChanges() As Boolean
    '    Get
    '        Return False
    '    End Get
    'End Property

    Protected Sub CopyEntity5(ByVal [from] As Entity5, ByVal [to] As Entity5) Implements IOrmEditable(Of Entity5).CopyBody
        With [from]
            [to]._name = ._name
            [to]._mark = ._mark
        End With
    End Sub

    <EntityPropertyAttribute(PropertyAlias:="Title")> _
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

    <EntityPropertyAttribute(PropertyAlias:="Version", behavior:=Field2DbRelations.RowVersion)> _
    Protected Property Version() As Byte()
        Get
            Using SyncHelper(True, "Title")
                Return _mark
            End Using
        End Get
        Set(ByVal value As Byte())
            Using SyncHelper(False, "Title")
                _mark = value
            End Using
        End Set
    End Property

End Class

Public Class EntitySchema5v1Implementation
    Inherits ObjectSchemaBaseImplementation

    Private _idx As OrmObjectIndex
    Protected _tables() As SourceFragment = {New SourceFragment("dbo.ent3")}
    Protected _rels() As M2MRelationDesc

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New OrmObjectIndex
            idx.Add(New MapField2Column("ID", "id", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("Title", "name", GetTables()(Tables.Main)))
            idx.Add(New MapField2Column("Version", "version", GetTables()(Tables.Main)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As SourceFragment()
        Return _tables
    End Function

    Public Overrides Function GetM2MRelations() As M2MRelationDesc()
        If _rels Is Nothing Then
            Dim t As New SourceFragment("dbo.[3to3]")
            _rels = New M2MRelationDesc() { _
                New M2MRelationDesc(GetType(Entity5), t, "ent3_id2", True, New System.Data.Common.DataTableMapping, M2MRelationDesc.DirKey), _
                New M2MRelationDesc(GetType(Entity5), t, "ent3_id1", True, New System.Data.Common.DataTableMapping, M2MRelationDesc.RevKey) _
            }
        End If
        Return _rels
    End Function

End Class