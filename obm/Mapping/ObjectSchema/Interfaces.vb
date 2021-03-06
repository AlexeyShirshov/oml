
Imports Worm.Criteria.Core
Imports Worm.Criteria.Joins
Imports Worm.Expressions2

Namespace Entities.Meta

    Public Interface IRelation
        Structure RelationDesc
            Public PropertyName As String
            Public EntityType As Type
            Public Key As String

            Public ReadOnly Property Direction() As Boolean
                Get
                    Return String.IsNullOrEmpty(Key) OrElse Not Key.StartsWith(M2MRelationDesc.ReversePrefix)
                End Get
            End Property

            Public Sub New(ByVal propertyName As String, ByVal entityType As Type)
                Me.PropertyName = propertyName
                Me.EntityType = entityType
            End Sub

            Public Sub New(ByVal propertyName As String, ByVal entityType As Type, ByVal direction As Boolean)
                Me.PropertyName = propertyName
                Me.EntityType = entityType
                Me.Key = M2MRelationDesc.GetKey(direction)
            End Sub
        End Structure

        Function GetFirstType() As RelationDesc
        Function GetSecondType() As RelationDesc
    End Interface

    Public Interface IPropertyMap
        ReadOnly Property FieldColumnMap() As Collections.IndexedCollection(Of String, MapField2Column)
    End Interface

    Public Interface IEntitySchema
        Inherits IPropertyMap
        ReadOnly Property Table() As SourceFragment
        'Function GetSuppressedFields() As String()
        'Function ChangeValueType(ByVal c As ColumnAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean
    End Interface
    Public Interface ISerializeSave

    End Interface
    Public Interface IContextObjectSchema
        Inherits IEntitySchema
        Function GetContextFilter(ByVal context As IDictionary) As IFilter
    End Interface

    Public Interface IOrmSorting
        Function CreateSortComparer(ByVal s As SortExpression) As IComparer
        Function CreateSortComparer(Of T As {_IEntity})(ByVal s As SortExpression) As Generic.IComparer(Of T)
    End Interface

    'Public Interface IOrmSorting2
    '    ReadOnly Property SortExpiration(ByVal s As SortExpression) As TimeSpan
    'End Interface

    Public Interface IMultiTableObjectSchema
        Inherits IEntitySchema
        Function GetTables() As SourceFragment()
        Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As QueryJoin
    End Interface

    Public Interface ISchemaWithM2M
        Inherits IEntitySchema
        Function GetM2MRelations() As M2MRelationDesc()
    End Interface

    'Public Interface ITypedSchema
    '    ReadOnly Property EntityType() As Type
    'End Interface

    Public Interface IReadonlyObjectSchema
        <Flags()> _
        Enum Operation
            Delete = 1
            Insert = 2
            Update = 4
            M2M = 8
            All = 15
        End Enum

        Function GetEditableSchema() As IEntitySchema
        ReadOnly Property SupportedOperation() As Operation
    End Interface

    Public Interface IPKInsertValues
        Function GetValue(ByVal column As String) As String
    End Interface

    Public Interface IChangeOutputOnInsert
        Function GetColumn(ByVal table As String, ByVal column As String) As String
    End Interface
    'Public Interface IFactory
    '    Sub CreateObject(ByVal field As String, ByVal value As Object)
    'End Interface

    'Public Interface IGetFilterValue
    '    ReadOnly Property FilterValue() As IDBValueFilter
    'End Interface

    'Public Interface IGetFactory
    '    ReadOnly Property Factory() As IFactory
    'End Interface

    'Public Interface IOrmTableFunction
    '    Function GetFunction(ByVal table As SourceFragment, ByVal pmgr As ParamMgr) As SourceFragment
    'End Interface

    Public Interface IFullTextSupport
        'Function GetQueryFields(ByVal contextKey As Object) As String()
        Function GetIndexedFields() As String()
        ReadOnly Property ApplayAsterisk() As Boolean
    End Interface

    Public Interface IFullTextSupportEx
        Inherits IFullTextSupport

        ReadOnly Property UseFreeText() As Boolean
        Sub MakeSearchString(ByVal tokens() As String, ByVal sb As StringBuilder)
    End Interface

    Public Interface ISupportAlphabet
        Function GetFirstDicField() As String
        Function GetSecondDicField() As String
    End Interface

    Public Interface ISchemaInit
        Sub InitSchema(ByVal mpe As ObjectMappingEngine, ByVal declaredType As Type)
    End Interface

    Public Interface ICacheBehavior
        Function GetEntityKey() As String
        Function GetEntityTypeKey() As Object
    End Interface

    Public Interface IJoinBehavior
        ReadOnly Property AlwaysJoinMainTable() As Boolean
        Function GetJoinField(ByVal t As Type) As String
    End Interface

    Public Interface IFtsStringFormatter
        Function GetFtsString(ByVal section As String, ByVal f As IFullTextSupport, ByVal type2search As Type, ByVal ftsString As String) As String
        Function GetTokens() As String()
    End Interface

    Public Interface IGetJoinsWithContext
        Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment, ByVal filterInfo As Object) As QueryJoin
    End Interface

    Public Interface IConnectedFilter
        Function ModifyFilterInfo(ByVal filterInfo As Object, ByVal selectedType As Type, ByVal filterType As Type) As Object
    End Interface

    Public Interface IEntitySchemaBase
        Inherits IEntitySchema
        Function ReplaceValueOnSave(ByVal propertyAlias As String, ByVal value As Object, ByRef newvalue As Object) As Boolean
        'Function GetSuppressedFields() As String()
    End Interface

    Public Interface IDefferedLoading
        Function GetDefferedLoadPropertiesGroups() As String()()
    End Interface

    Public Interface IStringValueConverter
        Function Convert(mpe As ObjectMappingEngine, prop As String, s As String, ByRef val As Object) As Boolean
        Delegate Function FallBackDelegate() As Object
    End Interface
    Public Interface IVersionable
        Property SchemaVersion() As String
        Property SchemaVersionOperator() As SchemaVersionOperatorEnum
        Property Feature As String
    End Interface
End Namespace
