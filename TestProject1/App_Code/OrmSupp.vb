Imports Worm.Entities.Meta

Public MustInherit Class ObjectSchemaBaseImplementationWeb
    Implements IOrmObjectSchema, ISchemaInit

    Protected _schema As Worm.ObjectMappingEngine

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
    End Sub

    Public ReadOnly Property Table() As Worm.Entities.Meta.SourceFragment Implements Worm.Entities.Meta.IEntitySchema.Table
        Get
            Return GetTables(0)
        End Get
    End Property
End Class
