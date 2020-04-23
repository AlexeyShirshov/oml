Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Core
Imports Worm.Criteria.Values
Imports Worm.Query
Imports Worm.Entities.Meta
Imports Worm.Entities
Imports Worm.Expressions2

Namespace Criteria

    Public MustInherit Class PredicateBase
        Private _con As Condition.ConditionConstructor
        Private _ct As ConditionOperator

        Public Sub New()
        End Sub

        Public Sub New(ByVal con As Condition.ConditionConstructor, ByVal ct As ConditionOperator)
            _con = con
            _ct = ct
        End Sub

        Protected Property ConditionCtor() As Condition.ConditionConstructor
            Get
                Return _con
            End Get
            Set(ByVal value As Condition.ConditionConstructor)
                _con = value
            End Set
        End Property

        Protected ReadOnly Property ConditionOper() As ConditionOperator
            Get
                Return _ct
            End Get
        End Property

        Protected MustOverride Function GetLink(ByVal fl As IFilter) As PredicateLink
        Protected MustOverride Function CreateFilter(ByVal v As IFilterValue, ByVal oper As FilterOperation) As IFilter
        Protected MustOverride Function CreateJoinFilter(ByVal op As ObjectProperty, ByVal fo As FilterOperation) As IFilter
        Protected MustOverride Function CreateJoinFilter(ByVal t As SourceFragment, ByVal column As String, ByVal fo As FilterOperation) As IFilter

        Protected Function cjf(ByVal t As Type, ByVal joinPropertyAlias As String, ByVal jfo As FilterOperation, ByVal fo As FilterOperation) As IFilter
            Dim j As IFilter = CreateJoinFilter(New ObjectProperty(t, joinPropertyAlias), jfo)
            Return New NonTemplateUnaryFilter(New SubQueryCmd(New QueryCmd().SelectEntity(t).From(t).Where(j)), fo)
        End Function

        Public Function eq(ByVal value As IFilterValue) As PredicateLink
            If value Is Nothing OrElse value Is DBNull.Value Then
                Return is_null()
            Else
                Return GetLink(CreateFilter(value, FilterOperation.Equal))
            End If
        End Function

        Public Function eq(ByVal value As Object) As PredicateLink
            If value Is Nothing OrElse value Is DBNull.Value Then
                Return is_null()
            Else
                Return GetLink(CreateFilter(New ScalarValue(value), FilterOperation.Equal))
            End If
        End Function

        Public Function eq(ByVal cmd As QueryCmd) As PredicateLink
            If cmd Is Nothing Then
                Return is_null()
            Else
                Return GetLink(CreateFilter(New SubQueryCmd(cmd), FilterOperation.Equal))
            End If
        End Function

        Public Function not_eq(ByVal value As Object) As PredicateLink
            If value Is Nothing OrElse value Is DBNull.Value Then
                Return is_not_null()
            Else
                Return GetLink(CreateFilter(New ScalarValue(value), FilterOperation.NotEqual))
            End If
        End Function

        Public Function not_eq(ByVal op As ObjectProperty) As PredicateLink
            Return GetLink(CreateJoinFilter(op, FilterOperation.NotEqual))
        End Function

        Public Function eq(ByVal value As ISinglePKEntity) As PredicateLink
            If value Is Nothing Then
                Return is_null()
            Else
                Return GetLink(CreateFilter(New EntityValue(value), FilterOperation.Equal))
            End If
        End Function
        Public Function eq(ByVal value As ICachedEntity) As PredicateLink
            If value Is Nothing Then
                Return is_null()
            Else
                Return GetLink(CreateFilter(New CachedEntityValue(value), FilterOperation.Equal))
            End If
        End Function
        Public Function eq(ByVal al As QueryAlias, ByVal propertyAlias As String) As PredicateLink
            Return GetLink(CreateJoinFilter(New ObjectProperty(al, propertyAlias), FilterOperation.Equal))
        End Function

        Public Function eq(ByVal t As Type, ByVal propertyAlias As String) As PredicateLink
            Return GetLink(CreateJoinFilter(New ObjectProperty(t, propertyAlias), FilterOperation.Equal))
        End Function

        Public Function eq(ByVal op As ObjectProperty) As PredicateLink
            Return GetLink(CreateJoinFilter(op, FilterOperation.Equal))
        End Function

        Public Function eq(ByVal tbl As SourceFragment, ByVal column As String) As PredicateLink
            Return GetLink(CreateJoinFilter(tbl, column, FilterOperation.Equal))
        End Function
        Public Function not_eq(ByVal value As ISinglePKEntity) As PredicateLink
            If value Is Nothing Then
                Return is_not_null()
            Else
                Return GetLink(CreateFilter(New EntityValue(value), FilterOperation.NotEqual))
            End If
        End Function
        Public Function not_eq(ByVal value As ICachedEntity) As PredicateLink
            If value Is Nothing Then
                Return is_not_null()
            Else
                Return GetLink(CreateFilter(New CachedEntityValue(value), FilterOperation.NotEqual))
            End If
        End Function
        Public Function greater_than_eq(ByVal value As Object) As PredicateLink
            Return GetLink(CreateFilter(New ScalarValue(value), FilterOperation.GreaterEqualThan))
        End Function

        Public Function less_than_eq(ByVal value As Object) As PredicateLink
            Return GetLink(CreateFilter(New ScalarValue(value), FilterOperation.LessEqualThan))
        End Function

        Public Function greater_than_eq(ByVal format As String, ByVal exp() As IExpression) As PredicateLink
            Return GetLink(CreateFilter(New CustomValue(format, exp), FilterOperation.GreaterEqualThan))
        End Function

        Public Function less_than_eq(ByVal format As String, ByVal exp() As IExpression) As PredicateLink
            Return GetLink(CreateFilter(New CustomValue(format, exp), FilterOperation.LessEqualThan))
        End Function

        Public Function greater_than_eq(ByVal format As String) As PredicateLink
            Return GetLink(CreateFilter(New CustomValue(format), FilterOperation.GreaterEqualThan))
        End Function

        Public Function less_than_eq(ByVal format As String) As PredicateLink
            Return GetLink(CreateFilter(New CustomValue(format), FilterOperation.LessEqualThan))
        End Function

        Public Function greater_than(ByVal value As Object) As PredicateLink
            Return GetLink(CreateFilter(New ScalarValue(value), FilterOperation.GreaterThan))
        End Function

        Public Function less_than(ByVal value As Object) As PredicateLink
            Return GetLink(CreateFilter(New ScalarValue(value), FilterOperation.LessThan))
        End Function

        Public Function greater_than(ByVal cmd As QueryCmd) As PredicateLink
            Return GetLink(CreateFilter(New SubQueryCmd(cmd), FilterOperation.GreaterThan))
        End Function

        Public Function less_than(ByVal cmd As QueryCmd) As PredicateLink
            Return GetLink(CreateFilter(New SubQueryCmd(cmd), FilterOperation.LessThan))
        End Function

        Public Function greater_than_eq(ByVal cmd As QueryCmd) As PredicateLink
            Return GetLink(CreateFilter(New SubQueryCmd(cmd), FilterOperation.GreaterEqualThan))
        End Function

        Public Function less_than_eq(ByVal cmd As QueryCmd) As PredicateLink
            Return GetLink(CreateFilter(New SubQueryCmd(cmd), FilterOperation.LessEqualThan))
        End Function

        Public Function greater_than(ByVal value As IFilterValue) As PredicateLink
            Return GetLink(CreateFilter(value, FilterOperation.GreaterThan))
        End Function

        Public Function less_than(ByVal value As IFilterValue) As PredicateLink
            Return GetLink(CreateFilter(value, FilterOperation.LessThan))
        End Function

        Public Function [like](ByVal value As String) As PredicateLink
            If String.IsNullOrEmpty(value) Then
                Throw New ArgumentNullException("value")
            End If
            Return GetLink(CreateFilter(New ScalarValue(value), FilterOperation.Like))
        End Function

        Public Function is_null() As PredicateLink
            Return GetLink(CreateFilter(New DBNullValue(), FilterOperation.Is))
        End Function

        Public Function is_not_null() As PredicateLink
            Return GetLink(CreateFilter(New DBNullValue(), FilterOperation.IsNot))
        End Function

        Public Overloads Function [in](ByVal ParamArray arr() As Object) As PredicateLink
            Return GetLink(CreateFilter(New InValue(arr), FilterOperation.In))
        End Function

        Public Overloads Function not_in(ByVal ParamArray arr() As Object) As PredicateLink
            Return GetLink(CreateFilter(New InValue(arr), FilterOperation.NotIn))
        End Function

        Public Overloads Function [in](ByVal arr As IEnumerable) As PredicateLink
            Return GetLink(CreateFilter(New InValue(arr), FilterOperation.In))
        End Function

        Public Overloads Function not_in(ByVal arr As IEnumerable) As PredicateLink
            Return GetLink(CreateFilter(New InValue(arr), FilterOperation.NotIn))
        End Function

        Public Function between(ByVal left As Object, ByVal right As Object) As PredicateLink
            Return GetLink(CreateFilter(New BetweenValue(left, right), FilterOperation.Between))
        End Function

        Public Function between(ByVal left As IFilterValue, ByVal right As IFilterValue) As PredicateLink
            Return GetLink(CreateFilter(New BetweenValue(left, right), FilterOperation.Between))
        End Function

        Public Function Op(ByVal oper As FilterOperation, ByVal value As IFilterValue) As PredicateLink
            Return GetLink(CreateFilter(value, oper))
        End Function

        Public Overloads Function [in](ByVal t As Type) As PredicateLink
            Return GetLink(CreateFilter(New SubQueryCmd(New QueryCmd().SelectEntity(t).From(t)), FilterOperation.In))
        End Function

        Public Overloads Function not_in(ByVal t As Type) As PredicateLink
            Return GetLink(CreateFilter(New SubQueryCmd(New QueryCmd().SelectEntity(t).From(t)), FilterOperation.NotIn))
        End Function

        Public Overloads Function [in](ByVal t As Type, ByVal propertyAlias As String) As PredicateLink
            Return GetLink(CreateFilter(New SubQueryCmd(New QueryCmd().Select(FCtor.prop(t, propertyAlias)).From(t)), FilterOperation.In))
        End Function

        Public Overloads Function not_in(ByVal t As Type, ByVal propertyAlias As String) As PredicateLink
            Return GetLink(CreateFilter(New SubQueryCmd(New QueryCmd().Select(FCtor.prop(t, propertyAlias)).From(t)), FilterOperation.NotIn))
        End Function

        Public Overloads Function [in](ByVal cmd As QueryCmd) As PredicateLink
            Return GetLink(CreateFilter(New SubQueryCmd(cmd), FilterOperation.In))
        End Function

        Public Overloads Function [not_in](ByVal cmd As QueryCmd) As PredicateLink
            Return GetLink(CreateFilter(New SubQueryCmd(cmd), FilterOperation.NotIn))
        End Function

        Public Function exists(ByVal cmd As Query.QueryCmd) As PredicateLink
            Return GetLink(New NonTemplateUnaryFilter(New SubQueryCmd(cmd), FilterOperation.Exists))
        End Function

        Public Function not_exists(ByVal cmd As Query.QueryCmd) As PredicateLink
            Return GetLink(New NonTemplateUnaryFilter(New SubQueryCmd(cmd), FilterOperation.NotExists))
        End Function

        Public Function exists(ByVal t As Type, ByVal f As IGetFilter) As PredicateLink
            Return exists(New QueryCmd().SelectEntity(t).From(t).Where(f))
        End Function

        Public Function exists(ByVal t As EntityUnion, ByVal f As IGetFilter) As PredicateLink
            Return exists(New QueryCmd().SelectEntity(t).From(t).Where(f))
        End Function

        Public Function not_exists(ByVal t As Type, ByVal f As IGetFilter) As PredicateLink
            Return not_exists(New QueryCmd().SelectEntity(t).From(t).Where(f))
        End Function

        Public Function not_exists(ByVal t As EntityUnion, ByVal f As IGetFilter) As PredicateLink
            Return not_exists(New QueryCmd().SelectEntity(t).From(t).Where(f))
        End Function

#Region " Operators "
        Public Shared Operator =(ByVal a As PredicateBase, ByVal b As Object) As PredicateLink
            Return a.eq(b)
        End Operator

        Public Shared Operator <>(ByVal a As PredicateBase, ByVal b As Object) As PredicateLink
            Return a.not_eq(b)
        End Operator

        Public Shared Operator >(ByVal a As PredicateBase, ByVal b As Object) As PredicateLink
            Return a.greater_than(b)
        End Operator

        Public Shared Operator <(ByVal a As PredicateBase, ByVal b As Object) As PredicateLink
            Return a.less_than(b)
        End Operator

        Public Shared Operator >=(ByVal a As PredicateBase, ByVal b As Object) As PredicateLink
            Return a.greater_than_eq(b)
        End Operator

        Public Shared Operator <=(ByVal a As PredicateBase, ByVal b As Object) As PredicateLink
            Return a.less_than_eq(b)
        End Operator

        Public Shared Operator =(ByVal a As PredicateBase, ByVal b As ObjectProperty) As PredicateLink
            Return a.eq(b)
        End Operator

        Public Shared Operator <>(ByVal a As PredicateBase, ByVal b As ObjectProperty) As PredicateLink
            Return a.not_eq(b)
        End Operator

        'Public Shared Operator >(ByVal a As PredicateBase, ByVal b As ObjectProperty) As PredicateLink
        '    Return a.greater_than(b)
        'End Operator

        'Public Shared Operator <(ByVal a As PredicateBase, ByVal b As ObjectProperty) As PredicateLink
        '    Return a.less_than(b)
        'End Operator

        'Public Shared Operator >=(ByVal a As PredicateBase, ByVal b As ObjectProperty) As PredicateLink
        '    Return a.greater_than_eq(b)
        'End Operator

        'Public Shared Operator <=(ByVal a As PredicateBase, ByVal b As ObjectProperty) As PredicateLink
        '    Return a.less_than_eq(b)
        'End Operator
#End Region
    End Class

End Namespace