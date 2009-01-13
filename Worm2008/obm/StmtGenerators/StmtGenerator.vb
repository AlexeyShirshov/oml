Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Criteria.Joins
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Core
Imports Worm.Query
Imports System.Collections.Generic

Public MustInherit Class StmtGenerator
    'Protected map As IDictionary = Hashtable.Synchronized(New Hashtable)


    Public MustOverride Function ParamName(ByVal name As String, ByVal i As Integer) As String
    Public MustOverride Function TopStatement(ByVal top As Integer) As String
    Public MustOverride ReadOnly Property Left() As String
    Public MustOverride ReadOnly Property GetYear() As String
    Public MustOverride ReadOnly Property GetDate() As String
    Public MustOverride ReadOnly Property Selector() As String
    Public MustOverride ReadOnly Property FTSKey() As String
    Public MustOverride Function Comment(ByVal s As String) As String

    'Public MustOverride Function CreateCriteria(ByVal os As ObjectSource) As Criteria.ICtor
    'Public MustOverride Function CreateCriteria(ByVal os As ObjectSource, ByVal propertyAlias As String) As Criteria.CriteriaField
    'Public MustOverride Function CreateCriteria(ByVal table As SourceFragment) As Criteria.ICtor
    'Public MustOverride Function CreateCriteria(ByVal table As SourceFragment, ByVal columnName As String) As Criteria.CriteriaColumn
    'Public MustOverride Function CreateCustom(ByVal format As String, ByVal value As Criteria.Values.IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation, ByVal ParamArray values() As Pair(Of Object, String)) As Worm.Criteria.Core.CustomFilter
    'Public MustOverride Function CreateConditionCtor() As Condition.ConditionConstructor
    'Public MustOverride Function CreateCriteriaLink(ByVal con As Condition.ConditionConstructor) As Criteria.CriteriaLink

    Public MustOverride Function CreateTopAspect(ByVal top As Integer) As Worm.Entities.Query.TopAspect
    Public MustOverride Function CreateTopAspect(ByVal top As Integer, ByVal sort As Sorting.Sort) As Worm.Entities.Query.TopAspect

    Public MustOverride Function GetTableName(ByVal t As SourceFragment) As String

    Public MustOverride Function CreateExecutor() As Worm.Query.IExecutor

    Public MustOverride Function CreateSelectExpressionFormater() As ISelectExpressionFormater

    'Protected Friend MustOverride Function MakeJoin(ByVal type2join As Type, ByVal selectType As Type, ByVal field As String, _
    '    ByVal oper As Criteria.FilterOperation, ByVal joinType As JoinType, Optional ByVal switchTable As Boolean = False) As OrmJoin

    'Protected Friend MustOverride Function MakeM2MJoin(ByVal m2m As M2MRelation, ByVal type2join As Type) As OrmJoin()

    Public MustOverride Function Oper2String(ByVal oper As Worm.Criteria.FilterOperation) As String

    Public MustOverride Sub FormStmt(ByVal dbschema As ObjectMappingEngine, _
                                   ByVal filterInfo As Object, ByVal paramMgr As ICreateParam, ByVal almgr As IPrepareTable, _
                                   ByVal sb As StringBuilder, ByVal _t As Type, ByVal _tbl As SourceFragment, _
                                   ByVal _joins() As QueryJoin, ByVal _field As String, ByVal _f As IFilter)

    Public MustOverride Function MakeQueryStatement(ByVal mpe As ObjectMappingEngine, ByVal filterInfo As Object, _
            ByVal query As QueryCmd, ByVal params As ICreateParam, _
            ByVal almgr As IPrepareTable) As String

    Public MustOverride ReadOnly Property SupportParams() As Boolean

    Public Overridable ReadOnly Property IncludeCallStack() As Boolean
        Get
            Return False
        End Get
    End Property
End Class
