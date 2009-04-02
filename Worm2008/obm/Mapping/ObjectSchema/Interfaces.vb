Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.Sorting
Imports Worm.Criteria.Joins
Imports Worm.Cache

Namespace Entities.Meta

    Public Interface IRelation
        Structure RelationDesc
            Public PropertyName As String
            Public EntityType As Type
            Public Key As String

            Public ReadOnly Property Direction() As Boolean
                Get
                    Return Not (M2MRelationDesc.RevKey = Key)
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
        Function GetFieldColumnMap() As Collections.IndexedCollection(Of String, MapField2Column)
    End Interface

    Public Interface IEntitySchema
        Inherits IPropertyMap
        ReadOnly Property Table() As SourceFragment
        'Function GetSuppressedFields() As String()
        'Function ChangeValueType(ByVal c As ColumnAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean
    End Interface

    Public Interface IContextObjectSchema
        Inherits IEntitySchema
        Function GetContextFilter(ByVal context As Object) As IFilter
    End Interface

    Public Interface IOrmSorting
        'ReadOnly Property IsExternalSort(ByVal s As Sort) As Boolean
        'Function ExternalSort(Of T As {OrmBase, New})(ByVal s As Sort, ByVal objs As ReadOnlyList(Of T)) As ReadOnlyList(Of T)
        Function CreateSortComparer(ByVal s As Sort) As IComparer
        Function CreateSortComparer(Of T As {_IEntity})(ByVal s As Sort) As Generic.IComparer(Of T)
    End Interface

    Public Interface IOrmSorting2
        ReadOnly Property SortExpiration(ByVal s As Sort) As TimeSpan
    End Interface

    Public Interface IMultiTableObjectSchema
        Inherits IEntitySchema
        Function GetTables() As SourceFragment()
        Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As QueryJoin
    End Interface

    Public Interface ISchemaWithM2M
        Inherits IEntitySchema
        Function GetM2MRelations() As M2MRelationDesc()
    End Interface

    'Public Interface IRelMapObjectSchema
    '    Inherits IMultiTableWithM2MSchema
    'End Interface

    'Public Interface IOrmObjectSchema
    '    Inherits IEntitySchemaBase
    '    Inherits IMultiTableObjectSchema
    '    Inherits IContextObjectSchema, ISchemaWithM2M
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
        Function GetValue(ByVal propertyAlias As String) As String
    End Interface

    Public Interface IChangeOutputOnInsert
        Function GetColumn(ByVal propertyAlias As String, ByVal column As String) As String
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
        Function GetQueryFields(ByVal contextKey As Object) As String()
        Function GetIndexedFields() As String()
        ReadOnly Property ApplayAsterisk() As Boolean
    End Interface

    Public Interface IFullTextSupportEx
        Inherits IFullTextSupport

        ReadOnly Property UseFreeText() As Boolean
        Sub MakeSearchString(ByVal contextKey As Object, ByVal tokens() As String, ByVal sb As StringBuilder)
    End Interface

    Public Interface ISupportAlphabet
        Function GetFirstDicField() As String
        Function GetSecondDicField() As String
    End Interface

    Public Interface ISchemaInit
        Sub GetSchema(ByVal schema As ObjectMappingEngine, ByVal t As Type)
    End Interface

    Public Interface ICacheBehavior
        Function GetEntityKey(ByVal filterInfo As Object) As String
        Function GetEntityTypeKey(ByVal filterInfo As Object) As Object
    End Interface

    Public Interface IJoinBehavior
        ReadOnly Property AlwaysJoinMainTable() As Boolean
        Function GetJoinField(ByVal t As Type) As String
    End Interface

    Public Interface IFtsStringFormatter
        Function GetFtsString(ByVal section As String, ByVal contextKey As Object, ByVal f As IFullTextSupport, ByVal type2search As Type, ByVal ftsString As String) As String
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
        Function ChangeValueType(ByVal c As Worm.Entities.Meta.EntityPropertyAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean
        Function GetSuppressedFields() As String()
    End Interface

    Public Interface IDefferedLoading
        Function GetDefferedLoadPropertiesGroups() As String()()
    End Interface
End Namespace
