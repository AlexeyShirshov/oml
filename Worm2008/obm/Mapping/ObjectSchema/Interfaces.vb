Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Criteria.Core
Imports Worm.Orm
Imports Worm.Sorting
Imports Worm.Criteria.Joins
Imports Worm.Cache

Namespace Orm.Meta

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
        Function GetTables() As SourceFragment()
        Function GetSuppressedColumns() As ColumnAttribute()
        Function ChangeValueType(ByVal c As ColumnAttribute, ByVal value As Object, ByRef newvalue As Object) As Boolean
    End Interface

    Public Interface IOrmObjectSchemaBase
        Inherits IObjectSchemaBase
        Function GetFilter(ByVal filter_info As Object) As IFilter
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

    Public Interface IOrmRelationalSchema
        Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment) As OrmJoin
    End Interface

    Public Interface IOrmRelationalSchemaWithM2M
        Inherits IOrmRelationalSchema
        Function GetM2MRelations() As M2MRelation()
    End Interface

    Public Interface IRelMapObjectSchema
        Inherits IOrmRelationalSchemaWithM2M, IObjectSchemaBase
    End Interface

    Public Interface IOrmObjectSchema
        Inherits IOrmObjectSchemaBase, IOrmRelationalSchemaWithM2M
    End Interface

    Public Interface IReadonlyObjectSchema
        Function GetEditableSchema() As IRelMapObjectSchema
    End Interface

    Public Interface IDBValueFilter
        Function CreateValue(ByVal c As ColumnAttribute, ByVal obj As IEntity, ByVal value As Object) As Object
    End Interface

    Public Interface IFactory
        Sub CreateObject(ByVal field As String, ByVal value As Object)
    End Interface

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

    Public Interface IOrmEditable(Of T As {OrmBase})
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
        Function GetJoins(ByVal left As SourceFragment, ByVal right As SourceFragment, ByVal filterInfo As Object) As OrmJoin
    End Interface

    Public Interface IConnectedFilter
        Function ModifyFilterInfo(ByVal filterInfo As Object, ByVal selectedType As Type, ByVal filterType As Type) As Object
    End Interface

End Namespace
