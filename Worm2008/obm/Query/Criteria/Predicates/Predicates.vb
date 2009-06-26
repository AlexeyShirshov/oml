Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Core
Imports Worm.Criteria.Values
Imports Worm.Entities
Imports Worm.Criteria.Joins
Imports Worm.Criteria
Imports System.Collections.Generic
Imports Worm.Query
Imports Worm.Entities.Meta

Namespace Criteria

    Public Enum FilterOperation
        Equal
        NotEqual
        GreaterThan
        GreaterEqualThan
        LessEqualThan
        LessThan
        [In]
        NotIn
        [Like]
        [Is]
        [IsNot]
        Exists
        NotExists
        Between
    End Enum

    'Public Interface ICtor
    '    Function Field(ByVal propertyAlias As String) As PropertyPredicate
    '    Function Column(ByVal columnName As String) As ColumnPredicate
    'End Interface

    'Public Interface IFullCtor
    '    Function Field(ByVal t As Type, ByVal fieldName As String) As CriteriaField
    '    Function Field(ByVal entityName As String, ByVal fieldName As String) As CriteriaField
    '    Function ColumnOnInlineTable(ByVal columnName As String) As CriteriaColumn
    '    Function Column(ByVal table As Orm.Meta.SourceFragment, ByVal columnName As String) As CriteriaColumn
    '    Function AutoTypeField(ByVal fieldName As String) As CriteriaField
    '    Function Exists(ByVal t As Type, ByVal gf As IGetFilter) As CriteriaLink
    '    Function NotExists(ByVal t As Type, ByVal gf As IGetFilter) As CriteriaLink
    '    Function Exists(ByVal t As Type, ByVal gf As IGetFilter, ByVal joins() As OrmJoin) As CriteriaLink
    '    Function NotExists(ByVal t As Type, ByVal gf As IGetFilter, ByVal joins() As OrmJoin) As CriteriaLink
    '    Function Exists(ByVal cmd As Query.QueryCmd) As CriteriaLink
    '    Function NotExists(ByVal cmd As Query.QueryCmd) As CriteriaLink
    '    Function Custom(ByVal format As String, ByVal ParamArray values() As Pair(Of Object, String)) As CriteriaBase
    'End Interface

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
            Return New NonTemplateUnaryFilter(New SubQuery(t, j), fo)
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

        Public Function eq(ByVal value As IKeyEntity) As PredicateLink
            If value Is Nothing Then
                Return is_null()
            Else
                Return GetLink(CreateFilter(New EntityValue(value), FilterOperation.Equal))
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

        Public Function not_eq(ByVal value As IKeyEntity) As PredicateLink
            If value Is Nothing Then
                Return is_not_null()
            Else
                Return GetLink(CreateFilter(New EntityValue(value), FilterOperation.NotEqual))
            End If
        End Function

        Public Function greater_than_eq(ByVal value As Object) As PredicateLink
            Return GetLink(CreateFilter(New ScalarValue(value), FilterOperation.GreaterEqualThan))
        End Function

        Public Function less_than_eq(ByVal value As Object) As PredicateLink
            Return GetLink(CreateFilter(New ScalarValue(value), FilterOperation.LessEqualThan))
        End Function

        Public Function greater_than_eq(ByVal format As String, ByVal ParamArray params() As IFilterValue) As PredicateLink
            Return GetLink(CreateFilter(New CustomValue(format, params), FilterOperation.GreaterEqualThan))
        End Function

        Public Function less_than_eq(ByVal format As String, ByVal ParamArray params() As IFilterValue) As PredicateLink
            Return GetLink(CreateFilter(New CustomValue(format, params), FilterOperation.LessEqualThan))
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

        Public Overloads Function [in](ByVal arr As ICollection) As PredicateLink
            Return GetLink(CreateFilter(New InValue(arr), FilterOperation.In))
        End Function

        Public Overloads Function not_in(ByVal arr As ICollection) As PredicateLink
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
            Return GetLink(CreateFilter(New SubQuery(t, Nothing), FilterOperation.In))
        End Function

        Public Overloads Function not_in(ByVal t As Type) As PredicateLink
            Return GetLink(CreateFilter(New SubQuery(t, Nothing), FilterOperation.NotIn))
        End Function

        Public Overloads Function [in](ByVal t As Type, ByVal propertyAlias As String) As PredicateLink
            Return GetLink(CreateFilter(New SubQuery(t, Nothing, propertyAlias), FilterOperation.In))
        End Function

        Public Overloads Function not_in(ByVal t As Type, ByVal propertyAlias As String) As PredicateLink
            Return GetLink(CreateFilter(New SubQuery(t, Nothing, propertyAlias), FilterOperation.NotIn))
        End Function

        Public Overloads Function [in](ByVal cmd As QueryCmd) As PredicateLink
            Return GetLink(CreateFilter(New SubQueryCmd(cmd), FilterOperation.In))
        End Function

        Public Overloads Function [not_in](ByVal cmd As QueryCmd) As PredicateLink
            Return GetLink(CreateFilter(New SubQueryCmd(cmd), FilterOperation.NotIn))
        End Function

        'Public Function exists(ByVal t As Type, ByVal joinField As String) As PredicateLink
        '    'Dim j As New JoinFilter(ObjectSource, Field, t, joinField, FilterOperation.Equal)
        '    'Return GetLink(New NonTemplateUnaryFilter(New SubQuery(t, j), FilterOperation.Exists))
        '    Return GetLink(cjf(t, joinField, FilterOperation.Equal, FilterOperation.Exists))
        'End Function

        'Public Function not_exists(ByVal t As Type, ByVal joinField As String) As PredicateLink
        '    'Dim j As New JoinFilter(ObjectSource, Field, t, joinField, FilterOperation.Equal)
        '    'Return GetLink(New NonTemplateUnaryFilter(New SubQuery(t, j), FilterOperation.NotExists))
        '    Return GetLink(cjf(t, joinField, FilterOperation.Equal, FilterOperation.NotExists))
        'End Function

        'Public Function exists(ByVal t As Type) As PredicateLink
        '    Return exists(t, Entities.OrmBaseT.PKName)
        'End Function

        'Public Function not_exists(ByVal t As Type) As PredicateLink
        '    Return not_exists(t, Entities.OrmBaseT.PKName)
        'End Function

        Public Function exists(ByVal t As Type, ByVal f As IGetFilter) As PredicateLink
            Return GetLink(New NonTemplateUnaryFilter(New SubQuery(t, f.Filter), FilterOperation.Exists))
        End Function

        Public Function not_exists(ByVal t As Type, ByVal f As IGetFilter) As PredicateLink
            Return GetLink(New NonTemplateUnaryFilter(New SubQuery(t, f.Filter), FilterOperation.NotExists))
        End Function

        Public Function exists(ByVal cmd As Query.QueryCmd) As PredicateLink
            Return GetLink(New NonTemplateUnaryFilter(New SubQueryCmd(cmd), FilterOperation.Exists))
        End Function

        Public Function not_exists(ByVal cmd As Query.QueryCmd) As PredicateLink
            Return GetLink(New NonTemplateUnaryFilter(New SubQueryCmd(cmd), FilterOperation.NotExists))
        End Function

        Public Function exists(ByVal t As Type, ByVal joinFilter As IFilter, ByVal joins() As QueryJoin) As PredicateLink
            Dim sq As New SubQuery(t, joinFilter)
            sq.Joins = joins
            Return CType(GetLink(New NonTemplateUnaryFilter(sq, FilterOperation.Exists)), PredicateLink)
        End Function

        Public Function not_exists(ByVal t As Type, ByVal joinFilter As IFilter, ByVal joins() As QueryJoin) As PredicateLink
            Dim sq As New SubQuery(t, joinFilter)
            sq.Joins = joins
            Return CType(GetLink(New NonTemplateUnaryFilter(sq, FilterOperation.NotExists)), PredicateLink)
        End Function
    End Class

    Public Class PropertyPredicate
        Inherits PredicateBase

        Private _op As ObjectProperty

        Protected Friend Sub New(ByVal op As ObjectProperty)
            _op = op
        End Sub

        Protected Friend Sub New(ByVal t As Type, ByVal propertyAlias As String)
            'If t Is Nothing Then
            '    Throw New ArgumentNullException("t")
            'End If

            'If String.IsNullOrEmpty(fieldName) Then
            '    Throw New ArgumentNullException("fieldName")
            'End If

            '_os = New ObjectSource(t)
            '_f = propertyAlias
            _op = New ObjectProperty(t, propertyAlias)
        End Sub

        Protected Friend Sub New(ByVal t As Type, ByVal propertyAlias As String, _
            ByVal con As Condition.ConditionConstructor, ByVal ct As ConditionOperator)
            MyBase.New(con, ct)
            '_os = New ObjectSource(t)
            '_f = propertyAlias
            _op = New ObjectProperty(t, propertyAlias)
        End Sub

        Protected Friend Sub New(ByVal os As EntityUnion, ByVal propertyAlias As String)
            '_os = os
            '_f = propertyAlias
            _op = New ObjectProperty(os, propertyAlias)
        End Sub

        Protected Friend Sub New(ByVal os As EntityUnion, ByVal propertyAlias As String, _
            ByVal con As Condition.ConditionConstructor, ByVal ct As ConditionOperator)
            MyBase.New(con, ct)
            '_os = os
            '_f = propertyAlias
            _op = New ObjectProperty(os, propertyAlias)
        End Sub

        Protected Friend Sub New(ByVal en As String, ByVal propertyAlias As String)
            '_os = New ObjectSource(en)
            '_f = propertyAlias
            _op = New ObjectProperty(en, propertyAlias)
        End Sub

        Protected Friend Sub New(ByVal en As String, ByVal propertyAlias As String, _
            ByVal con As Condition.ConditionConstructor, ByVal ct As ConditionOperator)
            MyBase.New(con, ct)
            '_os = New ObjectSource(en)
            '_f = propertyAlias
            _op = New ObjectProperty(en, propertyAlias)
        End Sub

        'Protected ReadOnly Property ObjectSource() As ObjectSource
        '    Get
        '        Return _os
        '    End Get
        'End Property

        'Protected ReadOnly Property Field() As String
        '    Get
        '        Return _f
        '    End Get
        'End Property

        Protected ReadOnly Property ObjectProp() As ObjectProperty
            Get
                Return _op
            End Get
        End Property

        Protected Overrides Function CreateFilter(ByVal v As Values.IFilterValue, ByVal oper As FilterOperation) As Core.IFilter
            Return New EntityFilter(_op, v, oper)
        End Function

        Protected Overrides Function GetLink(ByVal fl As Core.IFilter) As PredicateLink
            If ConditionCtor Is Nothing Then
                ConditionCtor = New Condition.ConditionConstructor
            End If
            ConditionCtor.AddFilter(fl, ConditionOper)
            Return New PredicateLink(_op.Entity, CType(ConditionCtor, Condition.ConditionConstructor))
        End Function

        Protected Overrides Function CreateJoinFilter(ByVal op As ObjectProperty, ByVal fo As FilterOperation) As Core.IFilter
            Dim j As New JoinFilter(ObjectProp, op, fo)
            Return j
        End Function

        Protected Overrides Function CreateJoinFilter(ByVal t As SourceFragment, ByVal column As String, ByVal fo As FilterOperation) As Core.IFilter
            Dim j As New JoinFilter(t, column, ObjectProp, fo)
            Return j
        End Function
    End Class

    Public Class ColumnPredicate
        Inherits PredicateBase

        Private _tbl As Meta.SourceFragment
        Private _col As String

        Protected Friend Sub New(ByVal table As Meta.SourceFragment, ByVal column As String)
            'If t Is Nothing Then
            '    Throw New ArgumentNullException("t")
            'End If

            'If String.IsNullOrEmpty(fieldName) Then
            '    Throw New ArgumentNullException("fieldName")
            'End If

            _tbl = table
            _col = column
        End Sub

        Protected Friend Sub New(ByVal table As Meta.SourceFragment, ByVal column As String, _
            ByVal con As Condition.ConditionConstructor, ByVal ct As ConditionOperator)
            MyBase.New(con, ct)
            _tbl = table
            _col = column
        End Sub

        Protected ReadOnly Property Table() As Meta.SourceFragment
            Get
                Return _tbl
            End Get
        End Property

        Protected ReadOnly Property Column() As String
            Get
                Return _col
            End Get
        End Property

        Protected Overrides Function CreateFilter(ByVal v As Values.IFilterValue, ByVal oper As FilterOperation) As Core.IFilter
            If Table Is Nothing Then
                Return New TableFilter(Column, v, oper)
            Else
                Return New TableFilter(Table, Column, v, oper)
            End If
        End Function

        Protected Overrides Function GetLink(ByVal fl As Core.IFilter) As PredicateLink
            If ConditionCtor Is Nothing Then
                ConditionCtor = New Condition.ConditionConstructor
            End If
            ConditionCtor.AddFilter(fl, ConditionOper)
            Return New PredicateLink(Table, CType(ConditionCtor, Condition.ConditionConstructor))
        End Function

        Protected Overrides Function CreateJoinFilter(ByVal op As ObjectProperty, ByVal fo As FilterOperation) As Core.IFilter
            Dim j As New JoinFilter(Table, Column, op, fo)
            Return j
        End Function

        Protected Overrides Function CreateJoinFilter(ByVal t As SourceFragment, ByVal column As String, ByVal fo As FilterOperation) As Core.IFilter
            Dim j As New JoinFilter(Table, Me.Column, t, column, fo)
            Return j
        End Function
    End Class

    Public Class CustomPredicate
        Inherits PredicateBase

        Private _format As String
        Private _values() As IFilterValue

        Protected Friend Sub New(ByVal format As String, ByVal ParamArray values() As IFilterValue)
            MyBase.New(Nothing, Nothing)
            _format = format
            _values = values
        End Sub

        Protected Friend Sub New(ByVal format As String, ByVal ParamArray values() As SelectExpression)
            MyBase.New(Nothing, Nothing)
            _format = format
            _values = Array.ConvertAll(values, Function(se As SelectExpression) New SelectExpressionValue(se))
        End Sub

        Protected Friend Sub New(ByVal format As String, ByVal values() As IFilterValue, _
            ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
            MyBase.New(con, ct)
            _format = format
            _values = values
        End Sub

        Protected Friend Sub New(ByVal format As String, ByVal values() As SelectExpression, _
            ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
            MyBase.New(con, ct)
            _format = format
            _values = Array.ConvertAll(values, Function(se As SelectExpression) New SelectExpressionValue(se))
        End Sub

        'Protected Friend Sub New(ByVal t As Type, ByVal field As String, ByVal format As String)
        '    MyBase.New(t, field)
        '    _format = format
        'End Sub

        'Protected Friend Sub New(ByVal t As Type, ByVal field As String)
        '    MyBase.New(t, field)
        'End Sub

        'Protected Friend Sub New(ByVal t As Type, ByVal field As String, ByVal format As String, _
        '    ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
        '    MyBase.New(t, field, con, ct)
        '    _format = format
        'End Sub

        'Protected Friend Sub New(ByVal t As Type, ByVal field As String, _
        '    ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
        '    MyBase.New(t, field, con, ct)
        'End Sub

        Protected Overrides Function CreateFilter(ByVal v As IFilterValue, ByVal oper As FilterOperation) As Worm.Criteria.Core.IFilter
            'If String.IsNullOrEmpty(_format) Then
            Return New CustomFilter(_format, v, oper, _values)
            'Else
            'Return New CustomFilter(Type, Field, _format, v, oper)
            'End If
        End Function

        Protected Overrides Function GetLink(ByVal fl As Worm.Criteria.Core.IFilter) As Worm.Criteria.PredicateLink
            If ConditionCtor Is Nothing Then
                ConditionCtor = New Condition.ConditionConstructor
            End If
            ConditionCtor.AddFilter(fl, ConditionOper)
            Return New PredicateLink(CType(ConditionCtor, Condition.ConditionConstructor))
        End Function

        Protected Overrides Function CreateJoinFilter(ByVal op As ObjectProperty, ByVal fo As FilterOperation) As Core.IFilter
            Dim j As New JoinFilter(op, New CustomValue(fo, _format, _values), fo)
            Return j
        End Function

        Protected Overrides Function CreateJoinFilter(ByVal t As SourceFragment, ByVal column As String, ByVal fo As FilterOperation) As Core.IFilter
            Throw New NotImplementedException
        End Function
    End Class

    Public Class UnaryPredicate
        Inherits PredicateBase
        'Private _con As Condition.ConditionConstructor
        'Private _ct As Worm.Criteria.Conditions.ConditionOperator

        Protected Friend Sub New(ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
            MyBase.New(con, ct)
            '_con = con
            '_ct = ct
        End Sub

        'Protected Overrides Function GetLink(ByVal fl As IFilter) As CriteriaLink

        'End Function

        'Public Function Exists(ByVal t As Type, ByVal joinFilter As IFilter) As CriteriaLink
        '    Return Exists(t, joinFilter, Nothing)
        'End Function

        'Public Function NotExists(ByVal t As Type, ByVal joinFilter As IFilter) As CriteriaLink
        '    Return NotExists(t, joinFilter, Nothing)
        'End Function

        'Public Function Exists(ByVal t As Type, ByVal joinFilter As IFilter, ByVal joins() As QueryJoin) As CriteriaLink
        '    Dim sq As New SubQuery(t, joinFilter)
        '    sq.Joins = joins
        '    Return CType(GetLink(New NonTemplateUnaryFilter(sq, FilterOperation.Exists)), CriteriaLink)
        'End Function

        'Public Function NotExists(ByVal t As Type, ByVal joinFilter As IFilter, ByVal joins() As QueryJoin) As CriteriaLink
        '    Dim sq As New SubQuery(t, joinFilter)
        '    sq.Joins = joins
        '    Return CType(GetLink(New NonTemplateUnaryFilter(sq, FilterOperation.NotExists)), CriteriaLink)
        'End Function

        'Public Function Exists(ByVal cmd As Query.QueryCmd) As CriteriaLink
        '    Return CType(GetLink(New NonTemplateUnaryFilter(New SubQueryCmd(cmd), FilterOperation.Exists)), CriteriaLink)
        'End Function

        'Public Function NotExists(ByVal cmd As Query.QueryCmd) As CriteriaLink
        '    Return CType(GetLink(New NonTemplateUnaryFilter(New SubQueryCmd(cmd), FilterOperation.NotExists)), CriteriaLink)
        'End Function

        'Public Function Custom(ByVal t As Type, ByVal field As String, ByVal oper As FilterOperation, ByVal value As IFilterValue) As CriteriaLink
        '    Return GetLink(New CustomFilter(t, field, value, oper))
        'End Function

        'Public Function Custom(ByVal t As Type, ByVal field As String, ByVal format As String, ByVal oper As FilterOperation, ByVal value As IFilterValue) As CriteriaLink
        '    Return GetLink(New CustomFilter(t, field, format, value, oper))
        'End Function

        Protected Overrides Function CreateFilter(ByVal v As IFilterValue, ByVal oper As Worm.Criteria.FilterOperation) As Worm.Criteria.Core.IFilter
            Throw New NotImplementedException
        End Function

        Protected Overrides Function GetLink(ByVal fl As Worm.Criteria.Core.IFilter) As Worm.Criteria.PredicateLink
            If ConditionCtor Is Nothing Then
                ConditionCtor = New Condition.ConditionConstructor
            End If
            ConditionCtor.AddFilter(fl, ConditionOper)
            Return New PredicateLink(CType(ConditionCtor, Condition.ConditionConstructor))
        End Function

        Protected Overrides Function CreateJoinFilter(ByVal op As ObjectProperty, ByVal fo As FilterOperation) As Core.IFilter
            Throw New NotImplementedException
        End Function

        Protected Overrides Function CreateJoinFilter(ByVal t As SourceFragment, ByVal column As String, ByVal fo As FilterOperation) As Core.IFilter
            Throw New NotImplementedException
        End Function
    End Class

    Module FilterHlp
        Public Function Invert(ByVal fo As FilterOperation) As FilterOperation
            Select Case fo
                Case FilterOperation.GreaterEqualThan
                    Return FilterOperation.LessEqualThan
                Case FilterOperation.GreaterThan
                    Return FilterOperation.LessThan
                Case FilterOperation.LessThan
                    Return FilterOperation.GreaterThan
                Case FilterOperation.LessEqualThan
                    Return FilterOperation.GreaterEqualThan
                Case Else
                    Return fo
            End Select
        End Function
    End Module

    Public Class AggPredicate
        Inherits PredicateBase

        Private _agg As AggregateBase

        Public Sub New(ByVal agg As AggregateBase)
            _agg = agg
        End Sub

        Protected Friend Sub New(ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
            MyBase.New(con, ct)
        End Sub

        Protected Overrides Function CreateFilter(ByVal v As Values.IFilterValue, ByVal oper As FilterOperation) As Core.IFilter
            Return New AggFilter(_agg, oper, v)
        End Function

        Protected Overloads Overrides Function CreateJoinFilter(ByVal t As Entities.Meta.SourceFragment, ByVal column As String, ByVal fo As FilterOperation) As Core.IFilter
            Throw New NotImplementedException
        End Function

        Protected Overloads Overrides Function CreateJoinFilter(ByVal op As Query.ObjectProperty, ByVal fo As FilterOperation) As Core.IFilter
            Throw New NotImplementedException
        End Function

        Protected Overrides Function GetLink(ByVal fl As Core.IFilter) As PredicateLink
            If ConditionCtor Is Nothing Then
                ConditionCtor = New Condition.ConditionConstructor
            End If
            ConditionCtor.AddFilter(fl, ConditionOper)
            Return New PredicateLink(CType(ConditionCtor, Condition.ConditionConstructor))
        End Function
    End Class

    Public Class QueryPredicate
        Inherits PredicateBase

        Private _q As QueryCmd

        Public Sub New(ByVal q As QueryCmd)
            _q = q
        End Sub

        Protected Friend Sub New(ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
            MyBase.New(con, ct)
        End Sub

        Protected Overrides Function CreateFilter(ByVal v As Values.IFilterValue, ByVal oper As FilterOperation) As Core.IFilter
            Return New QueryFilter(_q, oper, v)
        End Function

        Protected Overloads Overrides Function CreateJoinFilter(ByVal t As Entities.Meta.SourceFragment, ByVal column As String, ByVal fo As FilterOperation) As Core.IFilter
            Throw New NotImplementedException
        End Function

        Protected Overloads Overrides Function CreateJoinFilter(ByVal op As Query.ObjectProperty, ByVal fo As FilterOperation) As Core.IFilter
            Throw New NotImplementedException
        End Function

        Protected Overrides Function GetLink(ByVal fl As Core.IFilter) As PredicateLink
            If ConditionCtor Is Nothing Then
                ConditionCtor = New Condition.ConditionConstructor
            End If
            ConditionCtor.AddFilter(fl, ConditionOper)
            Return New PredicateLink(CType(ConditionCtor, Condition.ConditionConstructor))
        End Function
    End Class
End Namespace
