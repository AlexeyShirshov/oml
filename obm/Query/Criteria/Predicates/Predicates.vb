Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Core
Imports Worm.Criteria.Values
Imports Worm.Entities
Imports Worm.Criteria.Joins
Imports Worm.Criteria
Imports System.Collections.Generic
Imports Worm.Query
Imports Worm.Entities.Meta
Imports Worm.Expressions2
Imports Worm.Expressions

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
        Private _exp() As IExpression

        Protected Friend Sub New(ByVal format As String, ByVal exp() As IExpression)
            MyBase.New(Nothing, Nothing)
            _format = format
            _exp = exp
        End Sub

        'Protected Friend Sub New(ByVal format As String, ByVal exp As IGetExpression)
        '    MyBase.New(Nothing, Nothing)
        '    _format = format
        '    _exp = exp.Expression
        'End Sub

        Protected Friend Sub New(ByVal format As String, ByVal exp() As IExpression, _
            ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
            MyBase.New(con, ct)
            _format = format
            _exp = exp
        End Sub

        'Protected Friend Sub New(ByVal format As String, ByVal exp As IGetExpression, _
        '    ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
        '    MyBase.New(con, ct)
        '    _format = format
        '    _exp = exp.Expression
        'End Sub

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
            Return New CustomFilter(_format, v, oper, _exp)
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
            Dim j As New JoinFilter(op, New CustomValue(fo, _format, _exp), fo)
            Return j
        End Function

        Protected Overrides Function CreateJoinFilter(ByVal t As SourceFragment, ByVal column As String, ByVal fo As FilterOperation) As Core.IFilter
            Throw New NotImplementedException
        End Function
    End Class

    Public Class UnaryPredicate
        Inherits PredicateBase

        Private _v As IFilterValue

        Protected Friend Sub New(ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
            MyBase.New(con, ct)
            '_con = con
            '_ct = ct
        End Sub

        Protected Friend Sub New(v As IFilterValue)
            _v = v
        End Sub

        Protected Friend Sub New(v As IFilterValue, ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
            MyBase.New(con, ct)
            _v = v
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
            Return New ExpressionFilter(New UnaryExp(_v), New UnaryExp(v), oper)
        End Function

        Protected Overrides Function GetLink(ByVal fl As Worm.Criteria.Core.IFilter) As Worm.Criteria.PredicateLink
            If ConditionCtor Is Nothing Then
                ConditionCtor = New Condition.ConditionConstructor
            End If
            ConditionCtor.AddFilter(fl, ConditionOper)
            Return New PredicateLink(CType(ConditionCtor, Condition.ConditionConstructor))
        End Function

        Protected Overrides Function CreateJoinFilter(ByVal op As ObjectProperty, ByVal fo As FilterOperation) As Core.IFilter
            Return New EntityFilter(op, _v, fo)
        End Function

        Protected Overrides Function CreateJoinFilter(ByVal t As SourceFragment, ByVal column As String, ByVal fo As FilterOperation) As Core.IFilter
            Return New TableFilter(t, column, _v, fo)
        End Function
    End Class

    'Public Class BinaryPredicate
    '    Inherits PredicateBase

    '    Protected Friend Sub New(ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
    '        MyBase.New(con, ct)
    '    End Sub

    '    Protected Friend Sub New(left As unaryexp)

    '    End Sub

    '    Protected Overrides Function CreateFilter(v As Values.IFilterValue, oper As FilterOperation) As Core.IFilter

    '    End Function

    '    Protected Overloads Overrides Function CreateJoinFilter(t As Entities.Meta.SourceFragment, column As String, fo As FilterOperation) As Core.IFilter

    '    End Function

    '    Protected Overloads Overrides Function CreateJoinFilter(op As Query.ObjectProperty, fo As FilterOperation) As Core.IFilter

    '    End Function

    '    Protected Overrides Function GetLink(fl As Core.IFilter) As PredicateLink

    '    End Function
    'End Class

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

        Private _exp As AggregateExpression

        Public Sub New(ByVal exp As AggregateExpression)
            _exp = exp
        End Sub

        Protected Friend Sub New(ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
            MyBase.New(con, ct)
        End Sub

        Protected Overrides Function CreateFilter(ByVal v As Values.IFilterValue, ByVal oper As FilterOperation) As Core.IFilter
            Return New AggFilter(_exp, oper, v)
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
