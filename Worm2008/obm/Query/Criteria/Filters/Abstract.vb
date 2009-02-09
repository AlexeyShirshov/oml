Imports Worm.Criteria.Values
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports System.Collections.Generic

Namespace Criteria.Core
    Public Interface IGetFilter
        ReadOnly Property Filter() As IFilter
        'ReadOnly Property Filter(ByVal t As Type) As IFilter
    End Interface

    Public Interface IFilter
        Inherits IGetFilter, ICloneable, IQueryElement
        'Function MakeQueryStmt(ByVal schema As QueryGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As String
        Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, _
                               ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As String
        Function GetAllFilters() As ICollection(Of IFilter)
        Function Equals(ByVal f As IFilter) As Boolean
        Function ReplaceFilter(ByVal replacement As IFilter, ByVal replacer As IFilter) As IFilter
        Function SetUnion(ByVal eu As Query.EntityUnion) As IFilter
        Overloads Function Clone() As IFilter
    End Interface

    Public Interface ITemplateFilterBase
        'Function ReplaceByTemplate(ByVal replacement As ITemplateFilter, ByVal replacer As ITemplateFilter) As ITemplateFilter
        ReadOnly Property Template() As ITemplate
        Function MakeSingleQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As Pair(Of String)
    End Interface

    Public Interface ITemplateFilter
        Inherits IFilter, ITemplateFilterBase
        'Function GetStaticString() As String
    End Interface

    Public Interface IValuableFilter
        ReadOnly Property Value() As IFilterValue
    End Interface

    Public Interface IEntityFilterBase
        Function Eval(ByVal schema As ObjectMappingEngine, ByVal obj As _IEntity, ByVal oschema As IEntitySchema) As IEvaluableValue.EvalResult
        Function GetFilterTemplate() As IOrmFilterTemplate
        'Function PrepareValue(ByVal schema As ObjectMappingEngine, ByVal v As Object) As Object
        Function MakeHash() As String
    End Interface

    Public Interface IEntityFilter
        Inherits ITemplateFilter, IEntityFilterBase
        Property PrepareValue() As Boolean
    End Interface

    Public Interface ITemplate
        ReadOnly Property Operation() As FilterOperation
        ReadOnly Property OperToString() As String
        ReadOnly Property OperToStmt(ByVal stmt As StmtGenerator) As String
        Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String
        Function _ToString() As String
    End Interface

    Public Interface IOrmFilterTemplate
        Inherits ITemplate
        Function MakeHash(ByVal schema As ObjectMappingEngine, ByVal oschema As IEntitySchema, ByVal obj As ICachedEntity) As String
        'Function MakeFilter(ByVal schema As OrmSchemaBase, ByVal oschema As IOrmObjectSchemaBase, ByVal obj As OrmBase) As IEntityFilter
        'Sub SetType(ByVal [alias] As ObjectAlias)
    End Interface
End Namespace