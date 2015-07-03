Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Values
Imports Worm.Entities
Imports Worm.Expressions
Imports Worm.Query
Imports Worm.Expressions2
Imports Worm.Criteria.Joins
Imports System.Linq

Namespace Criteria.Core

    <Serializable()> _
    Public Class NonTemplateUnaryFilter
        Inherits Worm.Criteria.Core.FilterBase
        Implements Cache.IQueryDependentTypes

        Private _oper As Worm.Criteria.FilterOperation

        Public Sub New(ByVal value As IFilterValue, ByVal oper As Worm.Criteria.FilterOperation)
            MyBase.New(value)
            _oper = oper
        End Sub

        Private _str As String
        Protected Overrides Function _ToString() As String
            If String.IsNullOrEmpty(_str) Then
                _str = val._ToString & Worm.Criteria.Core.TemplateBase.OperToStringInternal(_oper)
            End If
            Return _str
        End Function

        Public Overrides Function GetAllFilters() As IFilter()
            Return New NonTemplateUnaryFilter() {Me}
        End Function

        Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef,
                                                ByVal stmt As StmtGenerator, ByVal executor As IExecutionContext, ByVal contextInfo As IDictionary,
                                                ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As String
            'Return TemplateBase.Oper2String(_oper) & GetParam(schema, pname)
            'Dim id As Values.IDatabaseFilterValue = TryCast(val, Values.IDatabaseFilterValue)
            'If id IsNot Nothing Then
            Return stmt.Oper2String(_oper) & Value.GetParam(schema, fromClause, stmt, pname, almgr, Nothing, contextInfo, False, executor)
            'Else
            'Return MakeQueryStmt(schema, filterInfo, almgr, pname, columns)
            'End If
        End Function

        Public Overrides Function ToStaticString(ByVal mpe As ObjectMappingEngine) As String
            Dim v As INonTemplateValue = TryCast(val, INonTemplateValue)
            If v Is Nothing Then
                Throw New NotImplementedException("Value is not implement INonTemplateValue")
            End If
            Return v.GetStaticString(mpe) & "$" & Worm.Criteria.Core.TemplateBase.OperToStringInternal(_oper)
        End Function

        Protected Overrides Function _Clone() As Object
            Return Clone()
        End Function

        Public Overloads Function Clone() As NonTemplateUnaryFilter
            Return New NonTemplateUnaryFilter(Value, _oper)
        End Function

        Protected Overrides Function _CopyTo(target As ICopyable) As Boolean
            Return CopyTo(TryCast(target, NonTemplateUnaryFilter))
        End Function

        Public Overloads Function CopyTo(target As NonTemplateUnaryFilter) As Boolean
            If MyBase.CopyTo(target) Then

                If target Is Nothing Then
                    Return False
                End If

                target._oper = _oper

                Return True
            End If

            Return False
        End Function

        'Public Overloads Function MakeSQLStmt1(ByVal schema As DbSchema, ByVal almgr As AliasMgr, ByVal pname As Orm.Meta.ICreateParam) As String Implements IFilter.MakeSQLStmt
        '    Dim id As Values.IDatabaseFilterValue = TryCast(val, Values.IDatabaseFilterValue)
        '    If id IsNot Nothing Then
        '        Return TemplateBase.Oper2String(_oper) & id.GetParam(schema, pname, almgr)
        '    Else
        '        Return MakeQueryStmt(schema, almgr, pname)
        '    End If
        'End Function

        Public Function [Get](ByVal mpe As ObjectMappingEngine) As Cache.IDependentTypes Implements Cache.IQueryDependentTypes.Get
            Return Cache.QueryDependentTypes(mpe, Value)
        End Function
    End Class
End Namespace