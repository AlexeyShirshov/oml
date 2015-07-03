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

    Public Class AggFilter
        Inherits FilterBase

        Private _exp As AggregateExpression
        Private _fo As FilterOperation

        Protected Sub New()
            MyBase.New()
        End Sub

        Public Sub New(ByVal exp As AggregateExpression, ByVal fo As FilterOperation, ByVal val As IFilterValue)
            MyBase.New(val)
            _exp = exp
            _fo = fo
        End Sub

        Protected Overrides Function _Clone() As Object 'Implements System.ICloneable.Clone
            Return Clone()
        End Function

        Public Overloads Function Clone() As AggFilter
            Dim n As New AggFilter
            CopyTo(n)
            Return n
        End Function
        
        Protected Overrides Function _CopyTo(target As ICopyable) As Boolean
            Return _CopyTo(TryCast(target, AggFilter))
        End Function

        Public Overloads Function CopyTo(target As AggFilter) As Boolean
            If MyBase.CopyTo(target) Then
                If target Is Nothing Then
                    Return False
                End If

                If _exp IsNot Nothing Then
                    target._exp = _exp.clone
                End If

                target._fo = _fo
                Return True
            End If

            Return False
        End Function
        Public Overrides Function GetAllFilters() As IFilter() 'Implements IFilter.GetAllFilters
            Return New IFilter() {Me}
        End Function

        Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef,
                                                ByVal stmt As StmtGenerator, ByVal executor As Query.IExecutionContext, _
            ByVal contextInfo As IDictionary, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam) As String 'Implements IFilter.MakeQueryStmt

            Return _exp.MakeStatement(schema, fromClause, stmt, pname, almgr, contextInfo, MakeStatementMode.None, executor) & stmt.Oper2String(_fo) & Value.GetParam(schema, fromClause, stmt, pname, almgr, Nothing, contextInfo, False, executor)
        End Function

        'Public Function ReplaceFilter(ByVal replacement As IFilter, ByVal replacer As IFilter) As IFilter Implements IFilter.ReplaceFilter
        '    If Equals(replacement) Then
        '        Return replacer
        '    End If
        '    Return Nothing
        'End Function

        'Public ReadOnly Property Filter() As IFilter Implements IGetFilter.Filter
        '    Get
        '        Return Me
        '    End Get
        'End Property

        Protected Overrides Function _ToString() As String 'Implements Values.IQueryElement._ToString
            Return _exp.GetDynamicString & TemplateBase.OperToStringInternal(_fo)
        End Function

        Public Overrides Function ToStaticString(ByVal mpe As ObjectMappingEngine) As String 'Implements Values.IQueryElement.GetStaticString
            Return _exp.GetStaticString(mpe) & TemplateBase.OperToStringInternal(_fo)
        End Function

        Public Overrides Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) 'Implements Values.IQueryElement.Prepare
            _exp.Prepare(executor, schema, contextInfo, stmt, isAnonym)
            MyBase.Prepare(executor, schema, contextInfo, stmt, isAnonym)
        End Sub
    End Class
End Namespace