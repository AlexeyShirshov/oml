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
                    Return Not (M2MRelation.RevKey = Key)
                End Get
            End Property

            Public Sub New(ByVal propertyName As String, ByVal entityType As Type)
                Me.PropertyName = propertyName
                Me.EntityType = entityType
            End Sub

            Public Sub New(ByVal propertyName As String, ByVal entityType As Type, ByVal direction As Boolean)
                Me.PropertyName = propertyName
                Me.EntityType = entityType
                Me.Key = M2MRelation.GetKey(direction)
            End Sub
        End Structure

        Function GetFirstType() As RelationDesc
        Function GetSecondType() As RelationDesc
    End Interface

    Public Interface IOrmPropertyMap
        Function GetFieldColumnMap() As Collections.IndexedCollection(Of String, MapField2Column)
    End Interface

    Public Interface IObjectSchemaBase
        Inherits IOrmPropertyMap
        ReadOnly Property Table() As SourceFragment
        Function GetSuppressedFields() As String()
        Function ChangeValueType(ByVal c As ColumnAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean
    End Interface

    Public Interface IContextObjectSchema
        Inherits IObjectSchemaBase
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
        Inherits IObjectSchemaBase
        Function GetTables() As SourceFragment()
        Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As QueryJoin
    End Interface

    Public Interface IMultiTableWithM2MSchema
        Inherits IMultiTableObjectSchema
        Function GetM2MRelations() As M2MRelation()
    End Interface

    'Public Interface IRelMapObjectSchema
    '    Inherits IMultiTableWithM2MSchema
    'End Interface

    Public Interface IOrmObjectSchema
        Inherits IContextObjectSchema, IMultiTableWithM2MSchema
    End Interface

    Public Interface IReadonlyObjectSchema
        Function GetEditableSchema() As IMultiTableWithM2MSchema
    End Interface

    Public Interface IDBValueFilter
        Function CreateValue(ByVal c As ColumnAttribute, ByVal obj As IEntity, ByVal value As Object) As Object
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

    Public Interface IOrmFullTextSupport
        Function GetQueryFields(ByVal contextKey As Object) As String()
        Function GetIndexedFields() As String()
        ReadOnly Property ApplayAsterisk() As Boolean
    End Interface

    Public Interface IOrmFullTextSupportEx
        Inherits IOrmFullTextSupport

        ReadOnly Property UseFreeText() As Boolean
        Sub MakeSearchString(ByVal contextKey As Object, ByVal tokens() As String, ByVal sb As StringBuilder)
    End Interface

    Public Interface IOrmDictionary
        Function GetFirstDicField() As String
        Function GetSecondDicField() As String
    End Interface

    Public Interface IOrmSchemaInit
        Sub GetSchema(ByVal schema As ObjectMappingEngine, ByVal t As Type)
    End Interface

    Public Interface ICacheBehavior
        Function GetEntityKey(ByVal filterInfo As Object) As String
        Function GetEntityTypeKey(ByVal filterInfo As Object) As Object
    End Interface

    Public Interface IOrmEditable(Of T As {KeyEntity})
        Sub CopyBody(ByVal from As T, ByVal [to] As T)
    End Interface

    Public Interface IJoinBehavior
        ReadOnly Property AlwaysJoinMainTable() As Boolean
        Function GetJoinField(ByVal t As Type) As String
    End Interface

    Public Interface IFtsStringFormater
        Function GetFtsString(ByVal section As String, ByVal contextKey As Object, ByVal f As IOrmFullTextSupport, ByVal type2search As Type, ByVal ftsString As String) As String
        Function GetTokens() As String()
    End Interface

    Public Interface IGetJoinsWithContext
        Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment, ByVal filterInfo As Object) As QueryJoin
    End Interface

    Public Interface IConnectedFilter
        Function ModifyFilterInfo(ByVal filterInfo As Object, ByVal selectedType As Type, ByVal filterType As Type) As Object
    End Interface

End Namespace
