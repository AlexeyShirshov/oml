Imports Worm.Criteria.Values
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports System.Collections.Generic
Imports Worm.Query

Namespace Criteria.Core
    Public Interface IGetFilter
        ReadOnly Property Filter() As IFilter
        'ReadOnly Property Filter(ByVal t As Type) As IFilter
    End Interface

    Public Interface IFilter
        Inherits IGetFilter, IQueryElement
        Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, _
            ByVal executor As Query.IExecutionContext, _
            ByVal contextInfo As IDictionary, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As String
        Function GetAllFilters() As IFilter()
        Function Equals(ByVal f As IFilter) As Boolean
        Function ReplaceFilter(ByVal oldValue As IFilter, ByVal newValue As IFilter) As IFilter
        Function SetUnion(ByVal eu As Query.EntityUnion) As IFilter
        Overloads Function Clone() As IFilter
        Function RemoveFilter(ByVal f As IFilter) As IFilter
    End Interface

    Public Interface ITemplateFilterBase
        'Function ReplaceByTemplate(ByVal replacement As ITemplateFilter, ByVal replacer As ITemplateFilter) As ITemplateFilter
        ReadOnly Property Template() As ITemplate
        Function MakeSingleQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator,
                                     ByVal almgr As IPrepareTable, ByVal pname As ICreateParam, ByVal executor As Query.IExecutionContext) As IEnumerable(Of ColParam)
        Class ColParam
            Public Column As String
            Public Param As String
        End Class
    End Interface

    Public Interface ITemplateFilter
        Inherits IFilter, ITemplateFilterBase
        'Function GetStaticString() As String
    End Interface

    Public Interface IValuableFilter
        ReadOnly Property Value() As IFilterValue
    End Interface

    Public Interface IEvaluableFilter
        Function Eval(ByVal mpe As ObjectMappingEngine, d As GetObj4IEntityFilterDelegate,
                              joins As IEnumerable(Of Joins.QueryJoin), objEU As EntityUnion) As IEvaluableValue.EvalResult
    End Interface

    Public Interface IApplyFilter
        ReadOnly Property Filter As IFilter
        ReadOnly Property Joins As Criteria.Joins.QueryJoin()
        ReadOnly Property RootObjectUnion As EntityUnion
    End Interface

    Public Delegate Function GetObj4IEntityFilterDelegate(f As IEntityFilterBase) As Pair(Of _IEntity, IEntitySchema)

    Public Interface IEntityFilterBase
        Inherits IEvaluableFilter
        Function EvalObj(ByVal mpe As ObjectMappingEngine, ByVal obj As _IEntity, ByVal oschema As IEntitySchema,
                              joins As IEnumerable(Of Joins.QueryJoin), objEU As EntityUnion) As IEvaluableValue.EvalResult
        Function GetFilterTemplate() As IOrmFilterTemplate
        'Function PrepareValue(ByVal schema As ObjectMappingEngine, ByVal v As Object) As Object
        Function MakeHash() As String
        ReadOnly Property IsHashable As Boolean
    End Interface

    Public Interface IEntityFilter
        Inherits ITemplateFilter, IEntityFilterBase
        Property PrepareValue() As Boolean
    End Interface

    Public Interface ITemplate
        ReadOnly Property Operation() As FilterOperation
        ReadOnly Property OperToString() As String
        ReadOnly Property OperToStmt(ByVal stmt As StmtGenerator) As String
        Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String
        Function _ToString() As String
    End Interface

    Public Interface IOrmFilterTemplate
        Inherits ITemplate
        Function MakeHash(ByVal schema As ObjectMappingEngine, ByVal oschema As IEntitySchema, ByVal obj As ICachedEntity) As String
        'Function MakeFilter(ByVal schema As OrmSchemaBase, ByVal oschema As IOrmObjectSchemaBase, ByVal obj As OrmBase) As IEntityFilter
        Sub ReplaceDerived(mpe As ObjectMappingEngine, ByVal eu As EntityUnion)
    End Interface
End Namespace