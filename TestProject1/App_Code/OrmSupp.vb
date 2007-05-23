Imports Worm

Public MustInherit Class ObjectSchemaBaseImplementationWeb
    Implements Orm.IOrmObjectSchema, Orm.IOrmSchemaInit

    Protected _schema As Orm.IDbSchema

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

    Public Overridable Function MapSort2FieldName(ByVal sort As String) As String Implements Worm.Orm.IOrmObjectSchema.MapSort2FieldName
        Return Nothing
    End Function

    Public MustOverride Function GetTables() As Orm.OrmTable() Implements Worm.Orm.IOrmObjectSchema.GetTables

    Public MustOverride Function GetFieldColumnMap() As Worm.Orm.Collections.IndexedCollection(Of String, Worm.Orm.MapField2Column) Implements Worm.Orm.IOrmObjectSchema.GetFieldColumnMap

    Public Function ExternalSort(ByVal sort As String, ByVal sortType As Orm.SortType, ByVal objs As Collections.IList) As Collections.IList Implements Worm.Orm.IOrmObjectSchema.ExternalSort
        Return objs
    End Function

    Public ReadOnly Property IsExternalSort(ByVal sort As String) As Boolean Implements Worm.Orm.IOrmObjectSchema.IsExternalSort
        Get
            Return False
        End Get
    End Property

    Public Overridable Function GetM2MRelations() As Worm.Orm.M2MRelation() Implements Worm.Orm.IOrmObjectSchema.GetM2MRelations
        Return New Orm.M2MRelation() {}
    End Function

    Public Sub GetSchema(ByVal schema As Worm.Orm.OrmSchemaBase, ByVal t As System.Type) Implements Worm.Orm.IOrmSchemaInit.GetSchema
        _schema = CType(schema, Orm.IDbSchema)
    End Sub
End Class
