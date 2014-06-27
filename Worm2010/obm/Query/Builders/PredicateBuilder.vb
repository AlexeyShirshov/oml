Imports Worm.Entities
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Core
Imports Worm.Criteria.Joins
Imports Worm.Criteria
Imports Worm.Entities.Meta
Imports Worm.Expressions2

Namespace Query

    Public Class Ctor
        'Implements ICtor

        Private _os As EntityUnion
        Private _tbl As Worm.Entities.Meta.SourceFragment

        'Public Sub New(ByVal t As Type)
        '    MyClass.new(Nothing, t)
        'End Sub

        'Public Sub New(ByVal tbl As Entities.Meta.SourceFragment)
        '    MyClass.New(Nothing, tbl)
        'End Sub

        'Public Sub New(ByVal entityName As String)
        '    MyClass.New(Nothing, entityName)
        'End Sub

        Public Sub New(ByVal t As Type)
            If t Is Nothing Then
                Throw New ArgumentNullException("t")
            End If

            _os = New EntityUnion(t)
        End Sub

        Public Sub New(ByVal tbl As Entities.Meta.SourceFragment)
            If tbl Is Nothing Then
                Throw New ArgumentNullException("tbl")
            End If

            _tbl = tbl
        End Sub

        Public Sub New(ByVal entityName As String)
            _os = New EntityUnion(entityName)
        End Sub

        Public Sub New(ByVal os As EntityUnion)
            _os = os
        End Sub

        Protected Function _Field(ByVal propertyAlias As String) As Worm.Criteria.PropertyPredicate 'Implements ICtor.Field
            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("fieldName")
            End If

            'If _t Is Nothing AndAlso Not String.IsNullOrEmpty(_en) Then
            '    Return New CriteriaField(_stmtGen, _en, propertyAlias)
            'Else
            '    Return New CriteriaField(_stmtGen, _t, propertyAlias)
            'End If
            Return New PropertyPredicate(_os, propertyAlias)
        End Function

        Public Function prop(ByVal propertyAlias As String) As Criteria.PropertyPredicate
            Return CType(_Field(propertyAlias), PropertyPredicate)
        End Function

        'Public Shared Function Field(ByVal t As Type, ByVal propertyAlias As String) As CriteriaField
        '    Return Field(t, propertyAlias)
        'End Function

        Public Shared Function prop(ByVal [alias] As QueryAlias, ByVal propertyAlias As String) As PropertyPredicate
            Return prop(New EntityUnion([alias]), propertyAlias)
        End Function

        Public Shared Function query(ByVal cmd As QueryCmd) As QueryPredicate
            Return New QueryPredicate(cmd)
        End Function

        Public Shared Function prop(ByVal t As Type, ByVal propertyAlias As String) As PropertyPredicate
            If t Is Nothing Then
                Throw New ArgumentNullException("t")
            End If

            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("fieldName")
            End If

            Return New PropertyPredicate(t, propertyAlias)
        End Function

        Public Shared Function prop(ByVal os As EntityUnion, ByVal propertyAlias As String) As PropertyPredicate
            If os Is Nothing Then
                Throw New ArgumentNullException("os")
            End If

            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("propertyAlias")
            End If

            Return New PropertyPredicate(os, propertyAlias)
        End Function

        Public Shared Function prop(ByVal objField As ObjectProperty) As PropertyPredicate
            Return prop(objField.Entity, objField.PropertyAlias)
        End Function

        Public Shared Function prop(ByVal entityName As String, ByVal propertyAlias As String) As PropertyPredicate
            If String.IsNullOrEmpty(entityName) Then
                Throw New ArgumentNullException("entityName")
            End If

            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("fieldName")
            End If

            Return New PropertyPredicate(entityName, propertyAlias)
        End Function

        Public Shared Function Filter(ByVal f As IGetFilter) As PredicateLink
            Dim con As New Condition.ConditionConstructor
            con.AddFilter(f.Filter)
            Return New PredicateLink(con)
        End Function

        Public Shared Function ColumnOnInlineTable(ByVal columnName As String) As ColumnPredicate
            Return column(Nothing, columnName)
        End Function

        'Public Shared Function Column(ByVal table As Entities.Meta.SourceFragment, ByVal columnName As String) As CriteriaColumn
        '    Return Column(table, columnName)
        'End Function

        Public Shared Function column(ByVal table As Entities.Meta.SourceFragment, ByVal columnName As String) As ColumnPredicate
            'If table Is Nothing Then
            '    Throw New ArgumentNullException("table")
            'End If

            If String.IsNullOrEmpty(columnName) Then
                Throw New ArgumentNullException("columnName")
            End If

            Return New ColumnPredicate(table, columnName)
        End Function

        'Public Shared Function AutoTypeField(ByVal propertyAlias As String) As CriteriaField
        '    If String.IsNullOrEmpty(propertyAlias) Then
        '        Throw New ArgumentNullException("propertyAlias")
        '    End If

        '    Return New CriteriaField(Nothing, CType(Nothing, Type), propertyAlias)
        'End Function

        Public Shared Function exists(ByVal t As Type, ByVal gf As IGetFilter) As PredicateLink
            Return New PredicateLink(New Condition.ConditionConstructor).and_exists(t, gf.Filter)
        End Function

        Public Shared Function not_exists(ByVal t As Type, ByVal gf As IGetFilter) As PredicateLink
            Return New PredicateLink(New Condition.ConditionConstructor).and_not_exists(t, gf.Filter)
        End Function

        Public Shared Function exists(ByVal e As EntityUnion, ByVal gf As IGetFilter) As PredicateLink
            Return New PredicateLink(New Condition.ConditionConstructor).and_exists(e, gf.Filter)
        End Function

        Public Shared Function not_exists(ByVal e As EntityUnion, ByVal gf As IGetFilter) As PredicateLink
            Return New PredicateLink(New Condition.ConditionConstructor).and_not_exists(e, gf.Filter)
        End Function

        'Public Shared Function exists(ByVal t As Type, ByVal gf As IGetFilter, ByVal joins() As QueryJoin) As PredicateLink
        '    Return New PredicateLink(New Condition.ConditionConstructor).and_exists(t, gf.Filter, joins)
        'End Function

        'Public Shared Function not_exists(ByVal t As Type, ByVal gf As IGetFilter, ByVal joins() As QueryJoin) As PredicateLink
        '    Return New PredicateLink(New Condition.ConditionConstructor).and_not_exists(t, gf.Filter, joins)
        'End Function

        Public Shared Function exists(ByVal cmd As Query.QueryCmd) As PredicateLink
            Return New PredicateLink(New Condition.ConditionConstructor).and_exists(cmd)
        End Function

        Public Shared Function not_exists(ByVal cmd As Query.QueryCmd) As PredicateLink
            Return New PredicateLink(New Condition.ConditionConstructor).and_not_exists(cmd)
        End Function

        Public Shared Function custom(ByVal format As String, ParamArray exp() As IExpression) As PredicateBase
            Return New CustomPredicate(format, exp)
        End Function

        'Public Shared Function custom(ByVal format As String, ByVal values() As SelectExpressionOld) As PredicateBase
        '    Return New CustomPredicate(format, values)
        'End Function

        Public Shared Function count() As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Count))
        End Function

        Public Shared Function max(ByVal p As ObjectProperty) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Max, New EntityExpression(p)))
        End Function

        Public Shared Function max(ByVal t As Type, ByVal propertyAlias As String) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Max, New EntityExpression(propertyAlias, t)))
        End Function

        Public Shared Function max(ByVal en As String, ByVal propertyAlias As String) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Max, New EntityExpression(propertyAlias, en)))
        End Function

        Public Shared Function max(ByVal [alias] As QueryAlias, ByVal propertyAlias As String) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Max, New EntityExpression(New ObjectProperty([alias], propertyAlias))))
        End Function

        Public Shared Function max(ByVal eu As EntityUnion, ByVal propertyAlias As String) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Max, New EntityExpression(New ObjectProperty(eu, propertyAlias))))
        End Function

        Public Shared Function max(ByVal t As SourceFragment, ByVal column As String) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Max, New TableExpression(t, column)))
        End Function

        Public Shared Function min(ByVal p As ObjectProperty) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Min, New EntityExpression(p)))
        End Function

        Public Shared Function min(ByVal t As Type, ByVal propertyAlias As String) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Min, New EntityExpression(propertyAlias, t)))
        End Function

        Public Shared Function min(ByVal en As String, ByVal propertyAlias As String) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Min, New EntityExpression(propertyAlias, en)))
        End Function

        Public Shared Function min(ByVal [alias] As QueryAlias, ByVal propertyAlias As String) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Min, New EntityExpression(New ObjectProperty([alias], propertyAlias))))
        End Function

        Public Shared Function min(ByVal eu As EntityUnion, ByVal propertyAlias As String) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Min, New EntityExpression(New ObjectProperty(eu, propertyAlias))))
        End Function

        Public Shared Function min(ByVal t As SourceFragment, ByVal column As String) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Min, New TableExpression(t, column)))
        End Function

        Public Shared Function sum(ByVal p As ObjectProperty) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Sum, New EntityExpression(p)))
        End Function

        Public Shared Function sum(ByVal t As Type, ByVal propertyAlias As String) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Sum, New EntityExpression(propertyAlias, t)))
        End Function

        Public Shared Function sum(ByVal en As String, ByVal propertyAlias As String) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Sum, New EntityExpression(propertyAlias, en)))
        End Function

        Public Shared Function sum(ByVal [alias] As QueryAlias, ByVal propertyAlias As String) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Sum, New EntityExpression(New ObjectProperty([alias], propertyAlias))))
        End Function

        Public Shared Function sum(ByVal eu As EntityUnion, ByVal propertyAlias As String) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Sum, New EntityExpression(New ObjectProperty(eu, propertyAlias))))
        End Function

        Public Shared Function sum(ByVal t As SourceFragment, ByVal column As String) As PredicateBase
            Return New AggPredicate(New AggregateExpression(AggregateExpression.AggregateFunction.Sum, New TableExpression(t, column)))
        End Function

        Public Function column(ByVal columnName As String) As ColumnPredicate
            Return CType(_Column(columnName), ColumnPredicate)
        End Function

        Protected Function _Column(ByVal columnName As String) As Worm.Criteria.ColumnPredicate 'Implements Worm.Criteria.ICtor.Column
            If String.IsNullOrEmpty(columnName) Then
                Throw New ArgumentNullException("columnName")
            End If

            Return New ColumnPredicate(_tbl, columnName)
        End Function

        Public Shared Function param(v As Object) As PredicateBase
            Return param(New Values.ScalarValue(v))
        End Function

        Public Shared Function param(v As Values.IFilterValue) As PredicateBase
            Return New UnaryPredicate(v)
        End Function
    End Class

End Namespace