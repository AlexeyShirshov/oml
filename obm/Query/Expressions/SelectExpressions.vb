﻿Imports System.Collections.Generic
Imports Worm.Entities.Meta

Namespace Expressions2
    Public Class GroupExpression
        Inherits UnaryExpressionBase

        Public Enum SummaryValues
            None
            Cube
            Rollup
            GroupingSets
            Custom
        End Enum

        Private _type As SummaryValues
        Private _custom As String

        Public Sub New(ByVal type As SummaryValues, ByVal exp As IExpression)
            MyBase.New(exp)
            _type = type
        End Sub

        Public Sub New(ByVal exp As IExpression)
            MyBase.New(exp)
        End Sub

        Public Overrides Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String
            Return stmt.FormatGroupBy(_type, MyBase.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextFilter, stmtMode, executor), _custom)
        End Function

        Public Overrides Function Equals(ByVal f As IQueryElement) As Boolean
            Return Equals(TryCast(f, GroupExpression))
        End Function

        Public Overloads Function Equals(ByVal f As GroupExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If

            Return _type = f._type AndAlso _custom = f._custom AndAlso Operand.Equals(f.Operand)
        End Function

        Public Overrides Function GetDynamicString() As String
            Return _type.ToString & "$" & _custom & "$" & MyBase.GetDynamicString()
        End Function

        Public Overrides Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String
            Return _type.ToString & "$" & _custom & "$" & MyBase.GetStaticString(mpe, contextFilter)
        End Function

        Protected Overrides Function Clone(ByVal operand As IExpression) As IUnaryExpression
            Return New GroupExpression(_type, operand) With {._custom = _custom}
        End Function
    End Class

    Public Class SortExpression
        Inherits UnaryExpressionBase
        Implements IEvaluable

        Public Enum SortType
            Asc
            Desc
        End Enum

        Private _order As SortType
        Private _collation As String

        Public Sub New(ByVal type As SortType, ByVal exp As IExpression)
            MyBase.New(exp)
            _order = type
        End Sub

        Public Sub New(ByVal exp As IExpression)
            MyBase.New(exp)
        End Sub

        Public Overrides Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String
            Return stmt.FormatOrderBy(_order, MyBase.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextFilter, stmtMode, executor), _collation)
        End Function

        Public Property Order() As SortType
            Get
                Return _order
            End Get
            Set(ByVal value As SortType)
                _order = value
            End Set
        End Property

        Public Overrides Function Equals(ByVal f As IQueryElement) As Boolean
            Return Equals(TryCast(f, SortExpression))
        End Function

        Public Overloads Function Equals(ByVal f As SortExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If

            Return _order = f._order AndAlso _collation = f._collation AndAlso Operand.Equals(f.Operand)
        End Function

        Public Overrides Function GetDynamicString() As String
            Return _collation & "$" & MyBase.GetDynamicString
        End Function

        Public Overrides Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String
            Return _collation & "$" & MyBase.GetStaticString(mpe, contextFilter)
        End Function

        'Public ReadOnly Property CanEvaluate() As Boolean
        '    Get
        '        For Each e As IExpression In GetExpressions()
        '            If Me IsNot e AndAlso ( _
        '                Not GetType(IEntityPropertyExpression).IsAssignableFrom(e.GetType) OrElse _
        '                Not GetType(IEvaluable).IsAssignableFrom(e.GetType) _
        '            ) Then
        '                Return False
        '            End If
        '        Next
        '        Return True
        '    End Get
        'End Property

        Protected Overrides Function Clone(ByVal operand As IExpression) As IUnaryExpression
            Return New SortExpression(_order, operand)
        End Function

        Public Function CanEval(ByVal mpe As ObjectMappingEngine) As Boolean Implements IEvaluable.CanEval
            Return Helper.CanEval(Operand, mpe)
        End Function

        Public Function Eval(ByVal mpe As ObjectMappingEngine, ByVal obj As Entities._IEntity, ByVal oschema As IEntitySchema, ByRef v As Object) As Boolean Implements IEvaluable.Eval
            Return GetValue(mpe, obj, oschema, Operand, v)
        End Function
    End Class

    <Serializable()> _
    Public Class OrderByClause
        Inherits ObjectModel.ReadOnlyCollection(Of SortExpression)

        Public Sub New(ByVal list As System.Collections.Generic.IList(Of SortExpression))
            MyBase.New(list)
        End Sub

        Public Function CanEvaluate(ByVal mpe As ObjectMappingEngine) As Boolean
            For Each s As SortExpression In Me
                If Not s.CanEval(mpe) Then
                    Return False
                End If
            Next
            Return True
        End Function
    End Class

End Namespace