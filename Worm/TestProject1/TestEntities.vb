Imports System.Collections.Generic
Imports Worm

<Orm.Entity(GetType(EntitySchema1v1Implementation), "1"), _
Orm.Entity(GetType(EntitySchema1v2Implementation), "2"), _
Orm.Entity(GetType(EntitySchema1v3Implementation), "3")> _
Public Class Entity
    Inherits Orm.OrmBaseT(Of Entity)

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As Orm.OrmCacheBase, ByVal schema As Orm.OrmSchemaBase)
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
    Implements Orm.IOrmObjectSchema, Orm.IOrmSchemaInit

    Protected _schema As Orm.OrmSchemaBase
    Protected _objectType As Type

    Public Overridable Function ChangeValueType(ByVal c As Worm.Orm.ColumnAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean Implements Worm.Orm.IOrmObjectSchema.ChangeValueType
        newvalue = value
        Return False
    End Function

    Public Overridable Function GetFilter(ByVal filter_info As Object) As Worm.Orm.IOrmFilter Implements Worm.Orm.IOrmObjectSchema.GetFilter
        Return Nothing
    End Function

    Public Overridable Function GetJoins(ByVal left As Orm.OrmTable, ByVal right As Orm.OrmTable) As Worm.Orm.OrmJoin Implements Worm.Orm.IOrmObjectSchema.GetJoins
        Return Nothing
    End Function

    Public Overridable Function GetSuppressedColumns() As Worm.Orm.ColumnAttribute() Implements Worm.Orm.IOrmObjectSchema.GetSuppressedColumns
        Return New Orm.ColumnAttribute() {}
    End Function

    'Public Overridable Function MapSort2FieldName(ByVal sort As String) As String Implements Worm.Orm.IOrmObjectSchema.MapSort2FieldName
    '    Return Nothing
    'End Function

    Public MustOverride Function GetTables() As Orm.OrmTable() Implements Worm.Orm.IOrmObjectSchema.GetTables

    Public MustOverride Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column) Implements Worm.Orm.IOrmObjectSchema.GetFieldColumnMap

    'Public Function ExternalSort(ByVal sort As String, ByVal sortType As Orm.SortType, ByVal objs As Collections.IList) As Collections.IList Implements Worm.Orm.IOrmObjectSchema.ExternalSort
    '    Return objs
    'End Function

    'Public ReadOnly Property IsExternalSort(ByVal sort As String) As Boolean Implements Worm.Orm.IOrmObjectSchema.IsExternalSort
    '    Get
    '        Return False
    '    End Get
    'End Property

    Public Overridable Function GetM2MRelations() As Worm.Orm.M2MRelation() Implements Worm.Orm.IOrmObjectSchema.GetM2MRelations
        Return New Orm.M2MRelation() {}
    End Function

    Public Sub GetSchema(ByVal schema As Worm.Orm.OrmSchemaBase, ByVal t As System.Type) Implements Worm.Orm.IOrmSchemaInit.GetSchema
        _schema = schema
        _objectType = t
    End Sub
End Class

Public Class EntitySchema1v1Implementation
    Inherits ObjectSchemaBaseImplementation

    Private _idx As Orm.OrmObjectIndex
    Protected _tables() As Orm.OrmTable = {New Orm.OrmTable("dbo.ent1")}
    Protected _rels() As Orm.M2MRelation

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New Orm.OrmObjectIndex
            idx.Add(New Orm.MapField2Column("ID", "id", GetTables()(Tables.Main)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As Orm.OrmTable()
        Return _tables
    End Function

    Public Overrides Function GetM2MRelations() As Worm.Orm.M2MRelation()
        If _rels Is Nothing Then
            _rels = New Orm.M2MRelation() { _
                New Orm.M2MRelation(GetType(Entity4), New Orm.OrmTable("dbo.[1to2]"), "ent2_id", False, New System.Data.Common.DataTableMapping) _
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

    Public Overrides Function GetJoins(ByVal left As Orm.OrmTable, ByVal right As Orm.OrmTable) As Worm.Orm.OrmJoin
        If left.Equals(GetTables()(Tables2.Main)) AndAlso right.Equals(GetTables()(Tables2.Second)) Then
            Return New Orm.OrmJoin(right, Orm.JoinType.Join, New Orm.OrmFilter(right, "i", _objectType, "ID", Orm.FilterOperation.Equal))
        End If
        Return MyBase.GetJoins(left, right)
    End Function

    Public Overrides Function GetTables() As Orm.OrmTable()
        If _tables.Length = 1 Then
            Dim s As New List(Of Orm.OrmTable)(MyBase.GetTables())
            s.Add(New Orm.OrmTable("dbo.t1"))
            _tables = s.ToArray
        End If
        Return _tables
    End Function

    Public Overrides Function GetM2MRelations() As Worm.Orm.M2MRelation()
        If _rels Is Nothing Then
            _rels = New Orm.M2MRelation() { _
                New Orm.M2MRelation(GetType(Entity4), New Orm.OrmTable("dbo.[1to2]"), "ent2_id", True, New System.Data.Common.DataTableMapping) _
                }
        End If
        Return _rels
    End Function

End Class

Public Class EntitySchema1v3Implementation
    Inherits EntitySchema1v2Implementation

    Public Overrides Function GetJoins(ByVal left As Orm.OrmTable, ByVal right As Orm.OrmTable) As Worm.Orm.OrmJoin
        If left.Equals(GetTables()(Tables2.Main)) AndAlso right.Equals(GetTables()(Tables2.Second)) Then
            Dim orc As New Orm.OrmCondition.OrmConditionConstructor
            orc.AddFilter(New Orm.OrmFilter(right, "i", _objectType, "ID", Orm.FilterOperation.Equal))
            orc.AddFilter(New Orm.OrmFilter(right, "s", New TypeWrap(Of Object)("a"), Orm.FilterOperation.Equal))
            Return New Orm.OrmJoin(right, Orm.JoinType.Join, orc.Condition)
        End If
        Return MyBase.GetJoins(left, right)
    End Function

End Class

<Orm.Entity(GetType(EntitySchema2v1Implementation), "1"), Orm.Entity(GetType(EntitySchema2v2Implementation), "2")> _
Public Class Entity2
    Inherits Entity

    Private _s As String

    Public Sub New()

    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As Orm.OrmCacheBase, ByVal schema As Orm.OrmSchemaBase)
        MyBase.New(id, cache, schema)
    End Sub

    Public Overrides Function Clone() As Object
        Dim e As New Entity2(Identifier, OrmCache, OrmSchema)
        Return e
    End Function

    <Orm.Column("Str")> _
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

    Public Overrides Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column)
        Dim idx As Orm.Collections.IndexedCollection(Of String, Orm.MapField2Column) = MyBase.GetFieldColumnMap()
        If Not _coladded Then
            Try
                idx.Add(New Orm.MapField2Column("Str", "s", GetTables()(Tables2.Second)))
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

    Public Overrides Function GetJoins(ByVal left As Orm.OrmTable, ByVal right As Orm.OrmTable) As Worm.Orm.OrmJoin
        If left.Equals(GetTables()(Tables2.Main)) AndAlso right.Equals(GetTables()(Tables2.Second)) Then
            Return New Orm.OrmJoin(right, Orm.JoinType.Join, New Orm.OrmFilter(right, "i", _objectType, "ID", Orm.FilterOperation.Equal))
        End If
        Return MyBase.GetJoins(left, right)
    End Function

    Public Overrides Function GetTables() As Orm.OrmTable()
        If _tables.Length = 1 Then
            Dim s As New List(Of Orm.OrmTable)(MyBase.GetTables())
            s.Add(New Orm.OrmTable("dbo.t2"))
            _tables = s.ToArray
        End If
        Return _tables
    End Function

    Public Overrides Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column)
        Dim idx As Orm.Collections.IndexedCollection(Of String, Orm.MapField2Column) = MyBase.GetFieldColumnMap()
        If Not _coladded Then
            Try
                idx.Add(New Orm.MapField2Column("Str", "s", GetTables()(Tables2.Second)))
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

    Public Sub New(ByVal id As Integer, ByVal cache As Orm.OrmCacheBase, ByVal schema As Orm.DbSchema)
        MyBase.New(id, cache, schema)
    End Sub
End Class

<Orm.Entity(GetType(EntitySchema4v1Implementation), "1"), Orm.Entity(GetType(EntitySchema4v2Implementation), "2")> _
Public Class Entity4
    Inherits Orm.OrmBaseT(Of Entity4)
    Implements Orm.IOrmEditable(Of Entity4)

    Private _name As String

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As Orm.OrmCacheBase, ByVal schema As Orm.OrmSchemaBase)
        MyBase.New(id, cache, schema)
    End Sub

    'Protected Overrides Function GetNew() As Worm.Orm.OrmBase
    '    Return New Entity4(Identifier, OrmCache, OrmSchema)
    'End Function

    'Protected Overrides Sub CopyBody(ByVal from As Worm.Orm.OrmBase, ByVal [to] As Worm.Orm.OrmBase)
    '    CopyEntity4(CType(from, Entity4), CType([to], Entity4))
    'End Sub

    Protected Sub CopyEntity4(ByVal from As Entity4, ByVal [to] As Entity4) Implements Orm.IOrmEditable(Of Entity4).CopyBody
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

    <Orm.Column("Title")> _
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
    Implements Orm.IOrmSorting

    Private _idx As Orm.OrmObjectIndex
    Protected _tables() As Orm.OrmTable = {New Orm.OrmTable("dbo.ent2")}
    Protected _rels() As Orm.M2MRelation

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New Orm.OrmObjectIndex
            idx.Add(New Orm.MapField2Column("ID", "id", GetTables()(Tables.Main)))
            idx.Add(New Orm.MapField2Column("Title", "name", GetTables()(Tables.Main)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As Orm.OrmTable()
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

    Public Overrides Function GetM2MRelations() As Worm.Orm.M2MRelation()
        If _rels Is Nothing Then
            _rels = New Orm.M2MRelation() {New Orm.M2MRelation(GetType(Entity), New Orm.OrmTable("dbo.[1to2]"), "ent1_id", False, New System.Data.Common.DataTableMapping)}
        End If
        Return _rels
    End Function

    Public Function CreateSortComparer(ByVal s As Worm.Orm.Sort) As System.Collections.IComparer Implements Worm.Orm.IOrmSorting.CreateSortComparer
        If s.FieldName = "Title" Then
            Return New Comparer(Entity4Sort.Name, s.Order)
        End If
        Return Nothing
    End Function

    Public Function CreateSortComparer1(Of T As {New, Worm.Orm.OrmBase})(ByVal s As Worm.Orm.Sort) As System.Collections.Generic.IComparer(Of T) Implements Worm.Orm.IOrmSorting.CreateSortComparer
        If s.FieldName = "Title" Then
            Return CType(New Comparer(Entity4Sort.Name, s.Order), Global.System.Collections.Generic.IComparer(Of T))
        End If
        Return Nothing
    End Function

    Public Function ExternalSort(Of T As {New, Worm.Orm.OrmBase})(ByVal s As Worm.Orm.Sort, ByVal objs As System.Collections.Generic.ICollection(Of T)) As System.Collections.Generic.ICollection(Of T) Implements Worm.Orm.IOrmSorting.ExternalSort
        Throw New NotSupportedException
    End Function

    Public Enum Entity4Sort
        Name
    End Enum

    Public Class Comparer
        Implements System.Collections.IComparer, System.Collections.Generic.IComparer(Of Entity4)

        Private _s As Entity4Sort
        Private _st As Integer = -1

        Public Sub New(ByVal s As Entity4Sort, ByVal st As Orm.SortType)
            _s = s
            If st = Orm.SortType.Asc Then
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

End Class

Public Class EntitySchema4v2Implementation
    Inherits EntitySchema4v1Implementation

    Public Enum Tables2
        Main
        Second
    End Enum

    Public Overrides Function GetJoins(ByVal left As Orm.OrmTable, ByVal right As Orm.OrmTable) As Worm.Orm.OrmJoin
        If left.Equals(GetTables()(Tables2.Main)) AndAlso right.Equals(GetTables()(Tables2.Second)) Then
            Return New Orm.OrmJoin(right, Orm.JoinType.Join, New Orm.OrmFilter(right, "i", _objectType, "ID", Orm.FilterOperation.Equal))
        End If
        Return MyBase.GetJoins(left, right)
    End Function

    Public Overrides Function GetTables() As Orm.OrmTable()
        If _tables.Length = 1 Then
            Dim s As New List(Of Orm.OrmTable)(MyBase.GetTables())
            s.Add(New Orm.OrmTable("dbo.t1"))
            _tables = s.ToArray
        End If
        Return _tables
    End Function

    Public Overrides Function GetFilter(ByVal filter_info As Object) As Worm.Orm.IOrmFilter
        If filter_info Is Nothing Then
            Return Nothing
        End If

        If filter_info.GetType IsNot GetType(String) Then
            Throw New Orm.OrmObjectException("Invalid filter_info type " & filter_info.GetType.Name)
        End If

        Return New Orm.OrmFilter(GetTables()(Tables2.Main), "s", New TypeWrap(Of Object)(filter_info), Orm.FilterOperation.Equal)
    End Function

    Public Overrides Function GetM2MRelations() As Worm.Orm.M2MRelation()
        If _rels Is Nothing Then
            _rels = New Orm.M2MRelation() {New Orm.M2MRelation(GetType(Entity), New Orm.OrmTable("dbo.[1to2]"), "ent1_id", True, New System.Data.Common.DataTableMapping)}
        End If
        Return _rels
    End Function

End Class

<Orm.Entity(GetType(EntitySchema5v1Implementation), "1")> _
Public Class Entity5
    Inherits Orm.OrmBaseT(Of Entity5)
    Implements Orm.IOrmEditable(Of Entity5)

    Private _name As String
    Private _upd() As Byte

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal id As Integer, ByVal cache As Orm.OrmCacheBase, ByVal schema As Orm.OrmSchemaBase)
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

    Protected Sub CopyEntity5(ByVal [from] As Entity5, ByVal [to] As Entity5) Implements Orm.IOrmEditable(Of Entity5).CopyBody
        With [from]
            [to]._name = ._name
            [to]._upd = ._upd
        End With
    End Sub

    <Orm.Column("Title")> _
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

    <Orm.Column("Version", Orm.Field2DbRelations.RowVersion)> _
    Protected Property Version() As Byte()
        Get
            Using SyncHelper(True, "Title")
                Return _upd
            End Using
        End Get
        Set(ByVal value As Byte())
            Using SyncHelper(False, "Title")
                _upd = value
            End Using
        End Set
    End Property

End Class

Public Class EntitySchema5v1Implementation
    Inherits ObjectSchemaBaseImplementation

    Private _idx As Orm.OrmObjectIndex
    Protected _tables() As Orm.OrmTable = {New Orm.OrmTable("dbo.ent3")}
    Protected _rels() As Orm.M2MRelation

    Public Enum Tables
        Main
    End Enum

    Public Overrides Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column)
        If _idx Is Nothing Then
            Dim idx As New Orm.OrmObjectIndex
            idx.Add(New Orm.MapField2Column("ID", "id", GetTables()(Tables.Main)))
            idx.Add(New Orm.MapField2Column("Title", "name", GetTables()(Tables.Main)))
            idx.Add(New Orm.MapField2Column("Version", "version", GetTables()(Tables.Main)))
            _idx = idx
        End If
        Return _idx
    End Function

    Public Overrides Function GetTables() As Orm.OrmTable()
        Return _tables
    End Function

    Public Overrides Function GetM2MRelations() As Worm.Orm.M2MRelation()
        If _rels Is Nothing Then
            Dim t As New Orm.OrmTable("dbo.[3to3]")
            _rels = New Orm.M2MRelation() { _
                New Orm.M2MRelation(GetType(Entity5), t, "ent3_id2", True, New System.Data.Common.DataTableMapping, True), _
                New Orm.M2MRelation(GetType(Entity5), t, "ent3_id1", True, New System.Data.Common.DataTableMapping, False) _
            }
        End If
        Return _rels
    End Function
End Class