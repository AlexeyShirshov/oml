﻿Imports Worm.Entities
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Core
Imports Worm.Criteria.Joins

Namespace Criteria

    Public Class PredicateLink
        Implements IGetFilter, ICloneable

        Private _con As Condition.ConditionConstructor
        Private _tbl As Meta.SourceFragment
        Private _os As ObjectSource

#Region " Ctors "
        Protected Friend Sub New(ByVal con As Condition.ConditionConstructor)
            _con = con
        End Sub

        Public Sub New()

        End Sub

        Public Sub New(ByVal t As Type)
            _os = New ObjectSource(t)
        End Sub

        Protected Friend Sub New(ByVal t As Type, ByVal con As Condition.ConditionConstructor)
            _con = con
            _os = New ObjectSource(t)
        End Sub

        Public Sub New(ByVal table As Meta.SourceFragment)
            _tbl = table
        End Sub

        Public Sub New(ByVal os As ObjectSource)
            _os = os
        End Sub

        Public Sub New(ByVal oa As ObjectAlias)
            _os = New ObjectSource(oa)
        End Sub

        Protected Friend Sub New(ByVal table As Meta.SourceFragment, ByVal con As Condition.ConditionConstructor)
            _con = con
            _tbl = table
        End Sub

        Public Sub New(ByVal entityName As String)
            _os = New ObjectSource(entityName)
        End Sub

        Protected Friend Sub New(ByVal entityName As String, ByVal con As Condition.ConditionConstructor)
            _con = con
            _os = New ObjectSource(entityName)
        End Sub

        Protected Friend Sub New(ByVal os As ObjectSource, ByVal con As Condition.ConditionConstructor)
            _con = con
            _os = os
        End Sub

#End Region

        Protected Function CreateField(ByVal entityName As String, ByVal propertyAlias As String, ByVal con As Condition.ConditionConstructor, ByVal oper As ConditionOperator) As PropertyPredicate
            Return CreateField(New ObjectSource(entityName), propertyAlias, con, oper)
        End Function

        Protected Function CreateField(ByVal t As Type, ByVal propertyAlias As String, ByVal con As Condition.ConditionConstructor, ByVal oper As ConditionOperator) As PropertyPredicate
            Return CreateField(New ObjectSource(t), propertyAlias, con, oper)
        End Function

        Protected Function _Clone() As Object Implements System.ICloneable.Clone
            If Table IsNot Nothing Then
                Return New PredicateLink(Table, CType(ConditionCtor.Clone, Condition.ConditionConstructor))
            Else
                Return New PredicateLink(ObjectSource, CType(ConditionCtor.Clone, Condition.ConditionConstructor))
            End If
        End Function

        'Protected MustOverride Function CreateField(ByVal entityName As String, ByVal fieldName As String, ByVal con As Condition.ConditionConstructorBase, ByVal oper As ConditionOperator) As CriteriaField
        Protected Function CreateField(ByVal os As ObjectSource, ByVal propertyAlias As String, ByVal con As Condition.ConditionConstructor, ByVal oper As ConditionOperator) As PropertyPredicate
            Return New PropertyPredicate(os, propertyAlias, con, oper)
        End Function

        Protected Function CreateColumn(ByVal table As Meta.SourceFragment, ByVal columnName As String, ByVal con As Condition.ConditionConstructor, ByVal oper As ConditionOperator) As ColumnPredicate
            Return New ColumnPredicate(table, columnName, con, oper)
        End Function

        Protected Function CreateCtor() As Condition.ConditionConstructor
            Return New Condition.ConditionConstructor
        End Function

        Public Function [and](ByVal t As Type, ByVal propertyAlias As String) As PropertyPredicate
            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("propertyAlias")
            End If

            If t Is Nothing Then
                Throw New ArgumentNullException("t")
            End If

            Return CreateField(t, propertyAlias, _con, ConditionOperator.And)
        End Function

        Public Function [and](ByVal oa As ObjectAlias, ByVal propertyAlias As String) As PropertyPredicate
            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("propertyAlias")
            End If

            If oa Is Nothing Then
                Throw New ArgumentNullException("oa")
            End If

            Return CreateField(New ObjectSource(oa), propertyAlias, _con, ConditionOperator.And)
        End Function

        Public Function [and](ByVal os As ObjectSource, ByVal propertyAlias As String) As PropertyPredicate
            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("fieldName")
            End If

            If os Is Nothing Then
                Throw New ArgumentNullException("os")
            End If

            Return CreateField(os, propertyAlias, _con, ConditionOperator.And)
        End Function

        Public Function [and](ByVal objField As ObjectProperty) As PropertyPredicate
            Return [and](objField.ObjectSource, objField.Field)
        End Function

        Public Function [and](ByVal entityName As String, ByVal propertyAlias As String) As PropertyPredicate
            If String.IsNullOrEmpty(entityName) Then
                Throw New ArgumentNullException("entityName")
            End If

            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("propertyAlias")
            End If

            Return CreateField(entityName, propertyAlias, _con, ConditionOperator.And)
        End Function

        Public Function [or](ByVal t As Type, ByVal propertyAlias As String) As PropertyPredicate
            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("propertyAlias")
            End If

            If t Is Nothing Then
                Throw New ArgumentNullException("t")
            End If

            Return CreateField(t, propertyAlias, _con, ConditionOperator.Or)
        End Function

        Public Function [or](ByVal oa As ObjectAlias, ByVal propertyAlias As String) As PropertyPredicate
            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("propertyAlias")
            End If

            If oa Is Nothing Then
                Throw New ArgumentNullException("oa")
            End If

            Return CreateField(New ObjectSource(oa), propertyAlias, _con, ConditionOperator.Or)
        End Function

        Public Function [or](ByVal os As ObjectSource, ByVal propertyAlias As String) As PropertyPredicate
            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("propertyAlias")
            End If

            If os Is Nothing Then
                Throw New ArgumentNullException("os")
            End If

            Return CreateField(os, propertyAlias, _con, ConditionOperator.Or)
        End Function

        Public Function [or](ByVal objField As ObjectProperty) As PropertyPredicate
            Return [or](objField.ObjectSource, objField.Field)
        End Function

        Public Function [or](ByVal entityName As String, ByVal propertyAlias As String) As PropertyPredicate
            If String.IsNullOrEmpty(entityName) Then
                Throw New ArgumentNullException("entityName")
            End If

            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("propertyAlias")
            End If

            Return CreateField(entityName, propertyAlias, _con, ConditionOperator.Or)
        End Function

        Public Function [and](ByVal table As Meta.SourceFragment, ByVal columnName As String) As ColumnPredicate
            If String.IsNullOrEmpty(columnName) Then
                Throw New ArgumentNullException("columnName")
            End If

            If table Is Nothing Then
                Throw New ArgumentNullException("table")
            End If

            Return CreateColumn(table, columnName, _con, ConditionOperator.And)
        End Function

        Public Function [or](ByVal table As Meta.SourceFragment, ByVal columnName As String) As ColumnPredicate
            If String.IsNullOrEmpty(columnName) Then
                Throw New ArgumentNullException("columnName")
            End If

            If table Is Nothing Then
                Throw New ArgumentNullException("table")
            End If

            Return CreateColumn(table, columnName, _con, ConditionOperator.Or)
        End Function

        Public Function [and](ByVal propertyAlias As String) As PredicateBase
            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("propertyAlias")
            End If

            If _tbl IsNot Nothing Then
                Return CreateColumn(_tbl, propertyAlias, _con, ConditionOperator.And)
            Else
                Return CreateField(_os, propertyAlias, _con, ConditionOperator.And)
            End If
        End Function

        Public Function [Or](ByVal propertyAlias As String) As PredicateBase
            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("propertyAlias")
            End If

            If _tbl IsNot Nothing Then
                Return CreateColumn(_tbl, propertyAlias, _con, ConditionOperator.Or)
            Else
                Return CreateField(_os, propertyAlias, _con, ConditionOperator.Or)
            End If
        End Function

        Public Function [and](ByVal link As PredicateLink) As PredicateLink
            If link IsNot Nothing Then
                _con.AddFilter(link._con.Condition, ConditionOperator.And)
            End If

            Return Me
        End Function

        Public Function [and](ByVal f As IGetFilter) As PredicateLink
            If f IsNot Nothing Then
                If _con Is Nothing Then
                    _con = CreateCtor()
                End If
                _con.AddFilter(f.Filter, ConditionOperator.And)
            End If
            Return Me
        End Function

        Public Function [or](ByVal f As IGetFilter) As PredicateLink
            If f IsNot Nothing Then
                If _con Is Nothing Then
                    _con = CreateCtor()
                End If
                _con.AddFilter(f.Filter, ConditionOperator.Or)
            End If
            Return Me
        End Function

        Public Function [or](ByVal link As PredicateLink) As PredicateLink
            If link IsNot Nothing Then
                _con.AddFilter(link._con.Condition, ConditionOperator.Or)
            End If

            Return Me
        End Function

        Public Function [and](ByVal custom As String, ByVal ParamArray values() As FieldReference) As PredicateBase
            Return New CustomPredicate(custom, values, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And)
        End Function

        Public Function [or](ByVal custom As String, ByVal ParamArray values() As FieldReference) As PredicateBase
            Return New CustomPredicate(custom, values, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.Or)
        End Function

        'Public Function CustomAnd(ByVal t As Type, ByVal field As String, ByVal format As String) As CriteriaField
        '    Return New CustomCF(t, field, format, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And)
        'End Function

        'Public Function CustomOr(ByVal t As Type, ByVal field As String, ByVal format As String) As CriteriaField
        '    Return New CustomCF(t, field, format, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.Or)
        'End Function

        Public Function and_exists(ByVal t As Type, ByVal joinFilter As IGetFilter) As PredicateLink
            Return New UnaryPredicate(CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And).exists(t, joinFilter.Filter)
        End Function

        Public Function and_not_exists(ByVal t As Type, ByVal joinFilter As IGetFilter) As PredicateLink
            Return New UnaryPredicate(CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And).not_exists(t, joinFilter.Filter)
        End Function

        Public Function and_exists(ByVal t As Type, ByVal joinFilter As IGetFilter, ByVal joins() As QueryJoin) As PredicateLink
            Return New UnaryPredicate(CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And).exists(t, joinFilter.Filter, joins)
        End Function

        Public Function and_not_exists(ByVal t As Type, ByVal joinFilter As IGetFilter, ByVal joins() As QueryJoin) As PredicateLink
            Return New UnaryPredicate(CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And).not_exists(t, joinFilter.Filter, joins)
        End Function

        'Public Function AndExistsM2M(ByVal t As Type, ByVal t2 As Type, ByVal filter As IGetFilter, ByVal mpe As ObjectMappingEngine) As PredicateLink
        '    Dim t12t2 As Entities.Meta.M2MRelation = mpe.GetM2MRelation(t, t2, True)
        '    Dim t22t1 As Entities.Meta.M2MRelation = mpe.GetM2MRelation(t2, t, True)
        '    Dim t2_pk As String = mpe.GetPrimaryKeys(t2)(0).PropertyAlias
        '    Dim t1_pk As String = mpe.GetPrimaryKeys(t)(0).PropertyAlias

        '    Return and_exists(t, JoinCondition.Create(t12t2.Table, t12t2.Column).eq(t2, t2_pk), _
        '                     JCtor.join(t22t1.Table).[on](t22t1.Table, t22t1.Column).eq(t, t1_pk))
        'End Function

        Public Function or_exists(ByVal t As Type, ByVal joinFilter As IFilter) As PredicateLink
            Return New UnaryPredicate(ConditionCtor, Worm.Criteria.Conditions.ConditionOperator.Or).exists(t, joinFilter)
        End Function

        Public Function or_not_exists(ByVal t As Type, ByVal joinFilter As IFilter) As PredicateLink
            Return New UnaryPredicate(ConditionCtor, Worm.Criteria.Conditions.ConditionOperator.Or).not_exists(t, joinFilter)
        End Function

        Public Function and_exists(ByVal cmd As Query.QueryCmd) As PredicateLink
            Return New UnaryPredicate(ConditionCtor, Worm.Criteria.Conditions.ConditionOperator.And).exists(cmd)
        End Function

        Public Function and_not_exists(ByVal cmd As Query.QueryCmd) As PredicateLink
            Return New UnaryPredicate(CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And).not_exists(cmd)
        End Function

        Public Function or_exists(ByVal cmd As Query.QueryCmd) As PredicateLink
            Return New UnaryPredicate(CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.Or).exists(cmd)
        End Function

        Public Function or_not_exists(ByVal cmd As Query.QueryCmd) As PredicateLink
            Return New UnaryPredicate(CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.Or).not_exists(cmd)
        End Function

        Public ReadOnly Property Filter() As IFilter Implements IGetFilter.Filter
            Get
                If _con IsNot Nothing Then
                    Return _con.Condition
                Else
                    Return Nothing
                End If
            End Get
        End Property

        'Public Overridable ReadOnly Property Filter(ByVal t As Type) As IFilter Implements IGetFilter.Filter
        '    Get
        '        If _con IsNot Nothing Then
        '            Dim f As IFilter = _con.Condition
        '            Dim ef As IEntityFilter = TryCast(f, IEntityFilter)
        '            If ef IsNot Nothing Then
        '                ef.GetFilterTemplate.SetType(New ObjectAlias(t))
        '            End If
        '            Return f
        '        Else
        '            Return Nothing
        '        End If
        '    End Get
        'End Property

        Protected ReadOnly Property ConditionCtor() As Condition.ConditionConstructor
            Get
                Return _con
            End Get
        End Property

        Protected ReadOnly Property ObjectSource() As ObjectSource
            Get
                Return _os
            End Get
        End Property

        'Protected ReadOnly Property Type() As Type
        '    Get
        '        Return _t
        '    End Get
        'End Property

        Protected ReadOnly Property Table() As Meta.SourceFragment
            Get
                Return _tbl
            End Get
        End Property

        Public Function Clone() As PredicateLink
            Return CType(_Clone(), PredicateLink)
        End Function
    End Class

End Namespace