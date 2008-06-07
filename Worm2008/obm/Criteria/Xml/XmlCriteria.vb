Imports Worm.Xml.Criteria.Conditions
Imports Worm.Criteria.Values
Imports Worm.Xml.Criteria.Core
Imports Worm.Xml.Criteria

Namespace Xml
    Namespace Criteria

        Public Class Ctor
            Implements Worm.Criteria.ICtor

            Private _t As Type

            Public Sub New(ByVal t As Type)
                If t Is Nothing Then
                    Throw New ArgumentNullException("t")
                End If

                _t = t
            End Sub

            Protected Function _Field(ByVal fieldName As String) As Worm.Criteria.CriteriaField Implements Worm.Criteria.ICtor.Field
                If String.IsNullOrEmpty(fieldName) Then
                    Throw New ArgumentNullException("fieldName")
                End If

                Return New XmlCriteriaField(_t, fieldName)
            End Function

            Public Function Field(ByVal fieldName As String) As XmlCriteriaField
                Return CType(_Field(fieldName), XmlCriteriaField)
            End Function

            Public Function Column(ByVal columnName As String) As Worm.Criteria.CriteriaColumn Implements Worm.Criteria.ICtor.Column
                Throw New NotImplementedException
            End Function
        End Class

        Public Class XmlCriteriaField
            Inherits Worm.Criteria.CriteriaField

            Protected Friend Sub New(ByVal t As Type, ByVal fieldName As String)
                MyBase.New(t, fieldName)
            End Sub

            Protected Friend Sub New(ByVal t As Type, ByVal fieldName As String, _
                ByVal con As Condition.ConditionConstructor, ByVal ct As Worm.Criteria.Conditions.ConditionOperator)
                MyBase.New(t, fieldName, con, ct)
            End Sub

            Protected Overrides Function CreateFilter(ByVal v As IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation) As Worm.Criteria.Core.IFilter
                Return New XmlEntityFilter(Type, Field, v, oper)
            End Function

            Protected Overrides Function GetLink(ByVal fl As Worm.Criteria.Core.IFilter) As Worm.Criteria.CriteriaLink
                If ConditionCtor Is Nothing Then
                    ConditionCtor = New Condition.ConditionConstructor
                End If
                ConditionCtor.AddFilter(fl, ConditionOper)
                Return New XmlCriteriaLink(Type, CType(ConditionCtor, Condition.ConditionConstructor))
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

            Protected Overrides Function CreateField(ByVal t As System.Type, ByVal fieldName As String, ByVal con As Criteria.Conditions.Condition.ConditionConstructorBase, ByVal oper As Worm.Criteria.Conditions.ConditionOperator) As Worm.Criteria.CriteriaField
                Return New XmlCriteriaField(t, fieldName, CType(con, Condition.ConditionConstructor), oper)
            End Function

            Protected Overrides Function _Clone() As Object
                Return New XmlCriteriaLink(Type, CType(ConditionCtor, Condition.ConditionConstructor))
            End Function

            Protected Overrides Function CreateColumn(ByVal table As Orm.Meta.SourceFragment, ByVal columnName As String, ByVal con As Criteria.Conditions.Condition.ConditionConstructorBase, ByVal oper As Worm.Criteria.Conditions.ConditionOperator) As Worm.Criteria.CriteriaColumn
                Throw New NotImplementedException
            End Function
        End Class
    End Namespace
End Namespace