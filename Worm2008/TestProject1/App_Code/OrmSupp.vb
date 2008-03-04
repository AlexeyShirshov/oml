Imports Worm.Orm.Meta

Public MustInherit Class ObjectSchemaBaseImplementationWeb
    Implements IOrmObjectSchema, IOrmSchemaInit

    Protected _schema As Worm.QueryGenerator

    Public Overridable Function ChangeValueType(ByVal c As ColumnAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean Implements IOrmObjectSchema.ChangeValueType
        newvalue = value
        Return False
    End Function

    Public Overridable Function GetFilter(ByVal filter_info As Object) As Worm.Criteria.Core.IFilter Implements IOrmObjectSchema.GetFilter
        Return Nothing
    End Function

    Public Overridable Function GetJoins(ByVal left As OrmTable, ByVal right As OrmTable) As Worm.Criteria.Joins.OrmJoin Implements IOrmObjectSchema.GetJoins
        Return Nothing
    End Function

    Public Overridable Function GetSuppressedColumns() As ColumnAttribute() Implements IOrmObjectSchema.GetSuppressedColumns
        Return New ColumnAttribute() {}
    End Function

    'Public Overridable Function MapSort2FieldName(ByVal sort As String) As String Implements Worm.Orm.IOrmObjectSchema.MapSort2FieldName
    '    Return Nothing
    'End Function

    Public MustOverride Function GetTables() As OrmTable() Implements IOrmObjectSchema.GetTables

    Public MustOverride Function GetFieldColumnMap() As Worm.Collections.IndexedCollection(Of String, MapField2Column) Implements IOrmObjectSchema.GetFieldColumnMap

    'Public Function ExternalSort(ByVal sort As String, ByVal sortType As Orm.SortType, ByVal objs As Collections.IList) As Collections.IList Implements Worm.Orm.IOrmObjectSchema.ExternalSort
    '    Return objs
    'End Function

    'Public ReadOnly Property IsExternalSort(ByVal sort As String) As Boolean Implements Worm.Orm.IOrmObjectSchema.IsExternalSort
    '    Get
    '        Return False
    '    End Get
    'End Property

    Public Overridable Function GetM2MRelations() As M2MRelation() Implements IOrmObjectSchema.GetM2MRelations
        Return New M2MRelation() {}
    End Function

    Public Sub GetSchema(ByVal schema As Worm.QueryGenerator, ByVal t As System.Type) Implements IOrmSchemaInit.GetSchema
        _schema = schema
    End Sub
End Class
