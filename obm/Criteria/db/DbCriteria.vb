﻿Imports Worm.Criteria
Imports Worm.Criteria.Values
Imports cc = Worm.Criteria.Core

Imports Worm.Database.Criteria.Values
Imports Worm.Database.Criteria.Core
Imports Worm.Database.Criteria.Joins
Imports Worm.Database.Criteria.Conditions

Namespace Database
    Namespace Criteria
        Public Class Ctor
            Implements ICtor
            Private _t As Type

            Public Sub New(ByVal t As Type)
                If t Is Nothing Then
                    Throw New ArgumentNullException("t")
                End If

                _t = t
            End Sub

            Protected Function _Field(ByVal fieldName As String) As Worm.Criteria.CriteriaField Implements ICtor.Field
                If String.IsNullOrEmpty(fieldName) Then
                    Throw New ArgumentNullException("fieldName")
                End If

                Return New CriteriaField(_t, fieldName)
            End Function

            Public Function Field(ByVal fieldName As String) As Criteria.CriteriaField
                Return CType(_Field(fieldName), CriteriaField)
            End Function

            Public Shared Function Field(ByVal t As Type, ByVal fieldName As String) As CriteriaField
                If t Is Nothing Then
                    Throw New ArgumentNullException("t")
                End If

                If String.IsNullOrEmpty(fieldName) Then
                    Throw New ArgumentNullException("fieldName")
                End If

                Return New CriteriaField(t, fieldName)
            End Function

            Public Shared Function AutoTypeField(ByVal fieldName As String) As CriteriaField
                If String.IsNullOrEmpty(fieldName) Then
                    Throw New ArgumentNullException("fieldName")
                End If

                Return New CriteriaField(Nothing, fieldName)
            End Function

            Public Shared Function Exists(ByVal t As Type, ByVal filter As IFilter) As CriteriaLink
                Return New CriteriaLink(New Condition.ConditionConstructor).AndExists(t, filter)
            End Function

            Public Shared Function NotExists(ByVal t As Type, ByVal filter As IFilter) As CriteriaLink
                Return New CriteriaLink(New Conditions.Condition.ConditionConstructor).AndNotExists(t, filter)
            End Function

            Public Shared Function Custom(ByVal format As String, ByVal ParamArray values() As Pair(Of Object, String)) As CriteriaBase
                Return New CustomCF(format, values)
            End Function

            'Public Shared Function Custom(ByVal t As Type, ByVal field As String, ByVal format As String) As CriteriaField
            '    Return New CustomCF(t, field, format)
            'End Function
        End Class

        Public Class CriteriaField
            Inherits Worm.Criteria.CriteriaField

            Protected Friend Sub New(ByVal t As Type, ByVal fieldName As String)
                MyBase.New(t, fieldName)
            End Sub

            Protected Friend Sub New(ByVal t As Type, ByVal fieldName As String, _
                ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
                MyBase.New(t, fieldName, con, ct)
            End Sub

            Protected Overrides Function GetLink(ByVal fl As Worm.Criteria.Core.IFilter) As Worm.Criteria.CriteriaLink
                If ConditionCtor Is Nothing Then
                    ConditionCtor = New Condition.ConditionConstructor
                End If
                ConditionCtor.AddFilter(fl, ConditionOper)
                Return New CriteriaLink(Type, CType(ConditionCtor, Condition.ConditionConstructor))
            End Function

            Protected Function GetLink2(ByVal fl As Worm.Criteria.Core.IFilter) As CriteriaLink
                Return CType(GetLink(fl), CriteriaLink)
            End Function

            Protected Overrides Function CreateFilter(ByVal v As IParamFilterValue, ByVal oper As FilterOperation) As Worm.Criteria.Core.IFilter
                Return New EntityFilter(Type, Field, v, oper)
            End Function

            Public Overloads Function [In](ByVal t As Type) As CriteriaLink
                Return GetLink2(New EntityFilter(Type, Field, New SubQuery(t, Nothing), FilterOperation.In))
            End Function

            Public Overloads Function NotIn(ByVal t As Type) As CriteriaLink
                Return GetLink2(New EntityFilter(Type, Field, New SubQuery(t, Nothing), FilterOperation.NotIn))
            End Function

            Public Overloads Function [In](ByVal t As Type, ByVal fieldName As String) As CriteriaLink
                Return GetLink2(New EntityFilter(Type, Field, New SubQuery(t, Nothing, fieldName), FilterOperation.In))
            End Function

            Public Overloads Function NotIn(ByVal t As Type, ByVal fieldName As String) As CriteriaLink
                Return GetLink2(New EntityFilter(Type, Field, New SubQuery(t, Nothing, fieldName), FilterOperation.NotIn))
            End Function

            Public Function Exists(ByVal t As Type, ByVal joinField As String) As CriteriaLink
                Dim j As New JoinFilter(Type, Field, t, joinField, FilterOperation.Equal)
                Return GetLink2(New NonTemplateFilter(New SubQuery(t, j), FilterOperation.Exists))
            End Function

            Public Function NotExists(ByVal t As Type, ByVal joinField As String) As CriteriaLink
                Dim j As New JoinFilter(Type, Field, t, joinField, FilterOperation.Equal)
                Return GetLink2(New NonTemplateFilter(New SubQuery(t, j), FilterOperation.NotExists))
            End Function

            Public Function Exists(ByVal t As Type) As CriteriaLink
                Return Exists(t, "ID")
            End Function

            Public Function NotExists(ByVal t As Type) As CriteriaLink
                Return NotExists(t, "ID")
            End Function

            Public Function Exists(ByVal t As Type, ByVal f As IFilter) As CriteriaLink
                Return GetLink2(New NonTemplateFilter(New SubQuery(t, f), FilterOperation.Exists))
            End Function

            Public Function NotExists(ByVal t As Type, ByVal f As IFilter) As CriteriaLink
                Return GetLink2(New NonTemplateFilter(New SubQuery(t, f), FilterOperation.NotExists))
            End Function
        End Class

        Public Class CustomCF
            Inherits CriteriaBase

            Private _format As String
            Private _values() As Pair(Of Object, String)

            Protected Friend Sub New(ByVal format As String, ByVal values() As Pair(Of Object, String))
                MyBase.New(Nothing, Nothing)
                _format = format
                _values = values
            End Sub

            Protected Friend Sub New(ByVal format As String, ByVal values() As Pair(Of Object, String), _
                ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
                MyBase.New(con, ct)
                _format = format
                _values = values
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

            Protected Overrides Function CreateFilter(ByVal v As IParamFilterValue, ByVal oper As FilterOperation) As Worm.Criteria.Core.IFilter
                'If String.IsNullOrEmpty(_format) Then
                Return New CustomFilter(_format, v, oper, _values)
                'Else
                'Return New CustomFilter(Type, Field, _format, v, oper)
                'End If
            End Function

            Protected Overrides Function GetLink(ByVal fl As Worm.Criteria.Core.IFilter) As Worm.Criteria.CriteriaLink
                If ConditionCtor Is Nothing Then
                    ConditionCtor = New Condition.ConditionConstructor
                End If
                ConditionCtor.AddFilter(fl, ConditionOper)
                Return New CriteriaLink(CType(ConditionCtor, Condition.ConditionConstructor))
            End Function
        End Class

        Public Class CriteriaNonField
            Inherits CriteriaBase
            'Private _con As Condition.ConditionConstructor
            'Private _ct As Worm.Criteria.Conditions.ConditionOperator

            Protected Friend Sub New(ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
                MyBase.New(con, ct)
                '_con = con
                '_ct = ct
            End Sub

            'Protected Overrides Function GetLink(ByVal fl As IFilter) As CriteriaLink

            'End Function

            Public Function Exists(ByVal t As Type, ByVal joinFilter As IFilter) As CriteriaLink
                Return CType(GetLink(New NonTemplateFilter(New SubQuery(t, joinFilter), FilterOperation.Exists)), CriteriaLink)
            End Function

            Public Function NotExists(ByVal t As Type, ByVal joinFilter As IFilter) As CriteriaLink
                Return CType(GetLink(New NonTemplateFilter(New SubQuery(t, joinFilter), FilterOperation.NotExists)), CriteriaLink)
            End Function

            'Public Function Custom(ByVal t As Type, ByVal field As String, ByVal oper As FilterOperation, ByVal value As IFilterValue) As CriteriaLink
            '    Return GetLink(New CustomFilter(t, field, value, oper))
            'End Function

            'Public Function Custom(ByVal t As Type, ByVal field As String, ByVal format As String, ByVal oper As FilterOperation, ByVal value As IFilterValue) As CriteriaLink
            '    Return GetLink(New CustomFilter(t, field, format, value, oper))
            'End Function

            Protected Overrides Function CreateFilter(ByVal v As Worm.Criteria.Values.IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation) As Worm.Criteria.Core.IFilter
                Throw New NotImplementedException
            End Function

            Protected Overrides Function GetLink(ByVal fl As Worm.Criteria.Core.IFilter) As Worm.Criteria.CriteriaLink
                If ConditionCtor Is Nothing Then
                    ConditionCtor = New Conditions.Condition.ConditionConstructor
                End If
                ConditionCtor.AddFilter(fl, ConditionOper)
                Return New CriteriaLink(CType(ConditionCtor, Worm.Database.Criteria.Conditions.Condition.ConditionConstructor))
            End Function
        End Class

        Public Class CriteriaLink
            Inherits Worm.Criteria.CriteriaLink

            Protected Friend Sub New(ByVal con As Condition.ConditionConstructor)
                MyBase.New(con)
            End Sub

            Public Sub New()
            End Sub

            Public Sub New(ByVal t As Type)
                MyBase.New(t)
            End Sub

            Protected Friend Sub New(ByVal t As Type, ByVal con As Condition.ConditionConstructor)
                MyBase.New(t, con)
            End Sub

            Protected Overrides Function CreateField(ByVal t As System.Type, ByVal fieldName As String, ByVal con As Condition.ConditionConstructorBase, ByVal oper As Worm.Criteria.Conditions.ConditionOperator) As Worm.Criteria.CriteriaField
                Return New CriteriaField(t, fieldName, CType(con, Condition.ConditionConstructor), oper)
            End Function

            Public Function CustomAnd(ByVal format As String, ByVal ParamArray values() As Pair(Of Object, String)) As CriteriaBase
                Return New CustomCF(format, values, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And)
            End Function

            Public Function CustomOr(ByVal format As String, ByVal ParamArray values() As Pair(Of Object, String)) As CriteriaBase
                Return New CustomCF(format, values, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.Or)
            End Function

            'Public Function CustomAnd(ByVal t As Type, ByVal field As String, ByVal format As String) As CriteriaField
            '    Return New CustomCF(t, field, format, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And)
            'End Function

            'Public Function CustomOr(ByVal t As Type, ByVal field As String, ByVal format As String) As CriteriaField
            '    Return New CustomCF(t, field, format, CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.Or)
            'End Function

            Public Function AndExists(ByVal t As Type, ByVal joinFilter As IFilter) As CriteriaLink
                Return New CriteriaNonField(CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And).Exists(t, joinFilter)
            End Function

            Public Function AndNotExists(ByVal t As Type, ByVal joinFilter As IFilter) As CriteriaLink
                Return New CriteriaNonField(CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.And).NotExists(t, joinFilter)
            End Function

            Public Function OrExists(ByVal t As Type, ByVal joinFilter As IFilter) As CriteriaLink
                Return New CriteriaNonField(CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.Or).Exists(t, joinFilter)
            End Function

            Public Function OrNotExists(ByVal t As Type, ByVal joinFilter As IFilter) As CriteriaLink
                Return New CriteriaNonField(CType(ConditionCtor, Condition.ConditionConstructor), Worm.Criteria.Conditions.ConditionOperator.Or).NotExists(t, joinFilter)
            End Function

        End Class
    End Namespace
End Namespace