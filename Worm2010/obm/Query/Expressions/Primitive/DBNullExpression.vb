Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query
Imports Worm.Cache

Namespace Expressions2

    <Serializable()> _
    Public Class DBNullExpression
        Inherits LiteralExpression
        Implements IParameterExpression

        Public Sub New()
            MyBase.New("null")
        End Sub

        'Public Function Test(ByVal oper As BinaryOperationType, ByVal v As Object, ByVal mpe As ObjectMappingEngine) As IParameterExpression.EvalResult Implements IParameterExpression.Test
        '    If oper = BinaryOperationType.Is Then
        '        If v Is Nothing Then
        '            Return IParameterExpression.EvalResult.Found
        '        Else
        '            Return IParameterExpression.EvalResult.NotFound
        '        End If
        '    ElseIf oper = BinaryOperationType.IsNot Then
        '        If v IsNot Nothing Then
        '            Return IParameterExpression.EvalResult.Found
        '        Else
        '            Return IParameterExpression.EvalResult.NotFound
        '        End If
        '    Else
        '        Throw New NotSupportedException(String.Format("Operation {0} is not supported for IsNull statement", oper))
        '    End If
        'End Function

        Public ReadOnly Property Value() As Object Implements IParameterExpression.Value
            Get
                Return Nothing
            End Get
        End Property

        <NonSerialized()>
        Public Event ModifyValue(ByVal sender As IParameterExpression, ByVal args As IParameterExpression.ModifyValueArgs) Implements IParameterExpression.ModifyValue

        Protected Overrides Function _Clone() As Object
            Return New DBNullExpression
        End Function
    End Class
End Namespace