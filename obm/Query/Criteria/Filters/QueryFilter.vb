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

    Public Class QueryFilter
        Inherits FilterBase

        Private _cmd As QueryCmd
        Private _fo As FilterOperation

        Public Sub New(ByVal cmd As QueryCmd, ByVal fo As FilterOperation, ByVal val As IFilterValue)
            MyBase.New(val)
            _cmd = cmd
            _fo = fo
        End Sub

        Protected Overrides Function _Clone() As Object 'Implements System.ICloneable.Clone
            Return Clone()
        End Function

        Public Overloads Function Clone() As QueryFilter
            Return New QueryFilter(_cmd, _fo, Value)
        End Function

        Protected Overrides Function _CopyTo(target As ICopyable) As Boolean
            Return CopyTo(TryCast(target, QueryFilter))
        End Function

        Public Overloads Function CopyTo(target As QueryFilter) As Boolean
            If MyBase.CopyTo(target) Then

                If target Is Nothing Then
                    Return False
                End If

                If _cmd IsNot Nothing Then
                    target._cmd = _cmd.Clone
                End If

                target._fo = _fo

                Return True
            End If

            Return False
        End Function
        'Public Overloads Function Equals(ByVal f As IFilter) As Boolean Implements IFilter.Equals
        '    Dim fl As AggFilter = TryCast(f, AggFilter)
        '    If fl Is Nothing Then
        '        Return False
        '    End If
        '    Return _agg.Equals(fl._agg) AndAlso _fo = fl._fo
        'End Function

        Public Overrides Function GetAllFilters() As IFilter() 'Implements IFilter.GetAllFilters
            Return New IFilter() {Me}
        End Function

        Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef,
                                                ByVal stmt As StmtGenerator, ByVal executor As Query.IExecutionContext, _
            ByVal contextInfo As IDictionary, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam) As String 'Implements IFilter.MakeQueryStmt

            Return "(" & stmt.MakeQueryStatement(schema, fromClause, contextInfo, _cmd, pname, almgr) & ")" & stmt.Oper2String(_fo) & Value.GetParam(schema, fromClause, stmt, pname, almgr, Nothing, contextInfo, False, executor)
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
            Return _cmd._ToString & TemplateBase.OperToStringInternal(_fo)
        End Function

        Public Overrides Function ToStaticString(ByVal mpe As ObjectMappingEngine) As String 'Implements Values.IQueryElement.GetStaticString
            Return _cmd.GetStaticString(mpe) & TemplateBase.OperToStringInternal(_fo)
        End Function

        Public Overrides Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) 'Implements Values.IQueryElement.Prepare
            _cmd.Prepare(executor, schema, contextInfo, stmt, isAnonym)
            MyBase.Prepare(executor, schema, contextInfo, stmt, isAnonym)
        End Sub
    End Class
End Namespace
