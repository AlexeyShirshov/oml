Imports Worm.Xml.Criteria.Conditions
Imports Worm.Criteria.Values
Imports Worm.Xml.Criteria.Core
Imports Worm.Xml.Criteria
Imports Worm.Orm

Namespace Xml
    Namespace Criteria

        Public Class Ctor
            Implements Worm.Criteria.ICtor

            Private _os As ObjectSource

            Public Sub New(ByVal t As Type)
                If t Is Nothing Then
                    Throw New ArgumentNullException("t")
                End If

                _os = New ObjectSource(t)
            End Sub

            Public Sub New(ByVal os As ObjectSource)
                If os Is Nothing Then
                    Throw New ArgumentNullException("os")
                End If

                _os = os
            End Sub

            Protected Function _Field(ByVal propertyAlias As String) As Worm.Criteria.CriteriaField Implements Worm.Criteria.ICtor.Field
                If String.IsNullOrEmpty(propertyAlias) Then
                    Throw New ArgumentNullException("propertyAlias")
                End If

                Return New XmlCriteriaField(_os, propertyAlias)
            End Function

            Public Function Field(ByVal propertyAlias As String) As XmlCriteriaField
                Return CType(_Field(propertyAlias), XmlCriteriaField)
            End Function

            Public Function Column(ByVal columnName As String) As Worm.Criteria.CriteriaColumn Implements Worm.Criteria.ICtor.Column
                Throw New NotImplementedException
            End Function
        End Class

        Public Class XmlCriteriaField
            Inherits Worm.Criteria.CriteriaField

            Protected Friend Sub New(ByVal os As ObjectSource, ByVal propertyAlias As String)
                MyBase.New(os, propertyAlias)
            End Sub

            Protected Friend Sub New(ByVal t As Type, ByVal propertyAlias As String)
                MyBase.New(t, propertyAlias)
            End Sub

            Protected Friend Sub New(ByVal t As Type, ByVal propertyAlias As String, _
                ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
                MyBase.New(t, propertyAlias, con, ct)
            End Sub

            Protected Friend Sub New(ByVal os As ObjectSource, ByVal propertyAlias As String, _
                ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
                MyBase.New(os, propertyAlias, con, ct)
            End Sub

            Protected Friend Sub New(ByVal entityName As String, ByVal propertyAlias As String)
                MyBase.New(entityName, propertyAlias)
            End Sub

            Protected Friend Sub New(ByVal entityName As String, ByVal propertyAlias As String, _
                ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
                MyBase.New(entityName, propertyAlias, con, ct)
            End Sub

            Protected Overrides Function CreateFilter(ByVal v As IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation) As Worm.Criteria.Core.IFilter
                Return New XmlEntityFilter(ObjectSource, Field, v, oper)
            End Function

            Protected Overrides Function GetLink(ByVal fl As Worm.Criteria.Core.IFilter) As Worm.Criteria.CriteriaLink
                If ConditionCtor Is Nothing Then
                    ConditionCtor = New Condition.ConditionConstructor
                End If
                ConditionCtor.AddFilter(fl, ConditionOper)
                Return New XmlCriteriaLink(ObjectSource, CType(ConditionCtor, Condition.ConditionConstructor))
            End Function
        End Class

        Public Class XmlCriteriaLink
            Inherits Worm.Criteria.CriteriaLink

            Protected Friend Sub New(ByVal con As Worm.Criteria.Conditions.Condition.ConditionConstructorBase)
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

            Protected Friend Sub New(ByVal os As ObjectSource, ByVal con As Condition.ConditionConstructorBase)
                MyBase.New(os, con)
            End Sub

            'Protected Overrides Function CreateField(ByVal entityName As String, ByVal fieldName As String, ByVal con As Criteria.Conditions.Condition.ConditionConstructorBase, ByVal oper As Worm.Criteria.Conditions.ConditionOperator) As Worm.Criteria.CriteriaField
            '    Return New XmlCriteriaField(entityName, fieldName, CType(con, Condition.ConditionConstructor), oper)
            'End Function

            Protected Overrides Function CreateField(ByVal os As ObjectSource, ByVal fieldName As String, ByVal con As Criteria.Conditions.Condition.ConditionConstructorBase, ByVal oper As Worm.Criteria.Conditions.ConditionOperator) As Worm.Criteria.CriteriaField
                Return New XmlCriteriaField(os, fieldName, CType(con, Condition.ConditionConstructor), oper)
            End Function

            Protected Overrides Function _Clone() As Object
                Return New XmlCriteriaLink(ObjectSource, CType(ConditionCtor, Condition.ConditionConstructor))
            End Function

            Protected Overrides Function CreateColumn(ByVal table As Orm.Meta.SourceFragment, ByVal columnName As String, ByVal con As Criteria.Conditions.Condition.ConditionConstructorBase, ByVal oper As Worm.Criteria.Conditions.ConditionOperator) As Worm.Criteria.CriteriaColumn
                Throw New NotImplementedException
            End Function

            Protected Overrides Function CreateCtor() As Criteria.Conditions.Condition.ConditionConstructorBase
                Return New Condition.ConditionConstructor
            End Function
        End Class
    End Namespace
End Namespace