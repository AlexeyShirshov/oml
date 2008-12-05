Imports System.Collections.Generic
Imports Worm.Criteria.Core
Imports Worm.Entities.Meta

Namespace Criteria.Values
    Public Interface IQueryElement
        Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String
        Function _ToString() As String
    End Interface

    Public Interface INonTemplateValue
        Inherits IQueryElement
    End Interface

    Public Interface IFilterValue
        Inherits IQueryElement
        Function GetParam(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, ByVal prepare As PrepareValueDelegate, _
                          ByVal aliases As IList(Of String), ByVal filterInfo As Object, ByVal inSelect As Boolean) As String
    End Interface

    Public Delegate Function PrepareValueDelegate(ByVal schema As ObjectMappingEngine, ByVal v As Object) As Object

    Public Interface IParamFilterValue
        Inherits IFilterValue
        ReadOnly Property ShouldUse() As Boolean
    End Interface

    Public Interface IEvaluableValue
        Inherits IParamFilterValue

        Enum EvalResult
            Found
            NotFound
            Unknown
        End Enum

        ReadOnly Property Value() As Object
        Function Eval(ByVal v As Object, ByVal template As OrmFilterTemplate) As EvalResult
    End Interface
End Namespace