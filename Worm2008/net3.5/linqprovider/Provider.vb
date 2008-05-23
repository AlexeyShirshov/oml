Imports System.Linq.Expressions
Imports System.Collections.ObjectModel
Imports Worm.Database.Criteria.Core
Imports Worm.Database.Criteria.Conditions
Imports Worm.Criteria.Values
Imports Worm.Orm

Namespace Linq
    Public Class WormLinqProvider
        Implements IQueryProvider

        Private _ctx As WormContext

        Public Sub New(ByVal ctx As WormContext)
            _ctx = ctx
        End Sub

        Public Function CreateQuery(ByVal expression As System.Linq.Expressions.Expression) As System.Linq.IQueryable Implements System.Linq.IQueryProvider.CreateQuery
            Return _ctx.CreateQueryWrapper(expression.Type, Me, expression)
        End Function

        Public Function CreateQuery(Of TElement)(ByVal expression As System.Linq.Expressions.Expression) As System.Linq.IQueryable(Of TElement) Implements System.Linq.IQueryProvider.CreateQuery
            Return _ctx.CreateQueryWrapper(Of TElement)(Me, expression)
        End Function

        Public Function Execute(ByVal expression As System.Linq.Expressions.Expression) As Object Implements System.Linq.IQueryProvider.Execute
            Using _ctx.CreateReadonlyManager

            End Using
        End Function

        Public Function Execute(Of TResult)(ByVal expression As System.Linq.Expressions.Expression) As TResult Implements System.Linq.IQueryProvider.Execute
            Using _ctx.CreateReadonlyManager
                Dim ev As New QueryVisitor
                ev.Visit(expression)
                Dim q As Worm.Query.QueryCmdBase = ev.Query
                Return Nothing
            End Using
        End Function
    End Class

    Public Class FilterValueVisitor
        Inherits ExpressionVisitor

        Private _v As IFilterValue
        Private _p As Orm.OrmProperty

        Protected Overrides Function VisitConstant(ByVal c As System.Linq.Expressions.ConstantExpression) As System.Linq.Expressions.Expression
            If c.Type.IsPrimitive Then
                _v = New ScalarValue(c.Value)
                Return Nothing
            End If
            Return MyBase.VisitConstant(c)
        End Function

        Protected Overrides Function VisitMemberAccess(ByVal m As System.Linq.Expressions.MemberExpression) As System.Linq.Expressions.Expression
            If m.Type.IsAssignableFrom(GetType(ormbase)) Then
                Return Nothing
            End If
            Return MyBase.VisitMemberAccess(m)
        End Function
    End Class

    Public Class FilterVisitorBase
        Inherits ExpressionVisitor

        Private _f As Criteria.Core.IFilter

        Public Overridable Property Filter() As Criteria.Core.IFilter
            Get
                Return _f
            End Get
            Protected Set(ByVal value As Criteria.Core.IFilter)
                _f = value
            End Set
        End Property

    End Class

    Public Class FilterVisitor
        Inherits FilterVisitorBase

        Private _c As New Condition.ConditionConstructor

        Public Overrides Property Filter() As Criteria.Core.IFilter
            Get
                If MyBase.Filter IsNot Nothing Then
                    Return MyBase.Filter
                Else
                    Return _c.Condition
                End If
            End Get
            Protected Set(ByVal value As Criteria.Core.IFilter)
                MyBase.Filter = value
            End Set
        End Property

        Private Sub ExtractOrAnd(ByVal b As System.Linq.Expressions.BinaryExpression, ByVal oper As Criteria.Conditions.ConditionOperator)
            Dim rf As New FilterVisitor : rf.Visit(b.Right)
            _c.AddFilter(rf.Filter)
            Dim lf As New FilterVisitor : lf.Visit(b.Left)
            _c.AddFilter(lf.Filter, oper)
        End Sub

        Protected Sub ExtractCondition(ByVal b As System.Linq.Expressions.BinaryExpression, ByVal fo As Criteria.FilterOperation)
            Dim rf As New FilterVisitorBase
            rf.Visit(b.Right)
            Filter = rf.Filter
        End Sub

        Protected Overrides Function VisitBinary(ByVal b As System.Linq.Expressions.BinaryExpression) As System.Linq.Expressions.Expression
            Select Case b.NodeType
                Case ExpressionType.And, ExpressionType.AndAlso
                    ExtractOrAnd(b, Criteria.Conditions.ConditionOperator.And)
                Case ExpressionType.Or, ExpressionType.OrElse
                    ExtractOrAnd(b, Criteria.Conditions.ConditionOperator.Or)
                Case ExpressionType.NotEqual
                    ExtractCondition(b, Criteria.FilterOperation.NotEqual)
            End Select
            Return MyBase.VisitBinary(b)
        End Function

        Protected Overrides Function VisitMemberAccess(ByVal m As System.Linq.Expressions.MemberExpression) As System.Linq.Expressions.Expression
            Return MyBase.VisitMemberAccess(m)
        End Function

        Protected Overrides Function VisitUnary(ByVal u As System.Linq.Expressions.UnaryExpression) As System.Linq.Expressions.Expression
            Return MyBase.VisitUnary(u)
        End Function
    End Class

    Public Class QueryVisitor
        Inherits ExpressionVisitor

        Private _q As Query.QueryCmdBase

        Sub New()
            _q = New Query.QueryCmdBase(Nothing)
        End Sub

        Sub New(ByVal q As Query.QueryCmdBase)
            _q = q
        End Sub

        Public ReadOnly Property Query() As Query.QueryCmdBase
            Get
                Return _q
            End Get
        End Property

        Protected Overrides Function VisitMethodCall(ByVal m As System.Linq.Expressions.MethodCallExpression) As System.Linq.Expressions.Expression
            Select Case m.Method.Name
                Case "Where"
                    Dim v = New FilterVisitor()
                    v.Visit(m.Arguments(1))
                    _q.Filter = v.Filter
            End Select
            Return Nothing
        End Function
    End Class
End Namespace