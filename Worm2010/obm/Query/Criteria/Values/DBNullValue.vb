Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.Query
Imports Worm.Expressions2
Imports System.Linq

Namespace Criteria.Values
    <Serializable()> _
    Public Class DBNullValue
        Inherits LiteralValue
        Implements IEvaluableValue

        Public Sub New()
            MyBase.New("null")
        End Sub

        Public Function Eval(ByVal v As Object, ByVal mpe As ObjectMappingEngine, ByVal template As Core.OrmFilterTemplate) As IEvaluableValue.EvalResult Implements IEvaluableValue.Eval
            If template.Operation = FilterOperation.Is Then
                If v Is Nothing Then
                    Return IEvaluableValue.EvalResult.Found
                Else
                    Return IEvaluableValue.EvalResult.NotFound
                End If
            ElseIf template.Operation = FilterOperation.IsNot Then
                If v IsNot Nothing Then
                    Return IEvaluableValue.EvalResult.Found
                Else
                    Return IEvaluableValue.EvalResult.NotFound
                End If
            Else
                Throw New NotSupportedException(String.Format("Operation {0} is not supported for IsNull statement", template.OperToString))
            End If
        End Function

        Public ReadOnly Property Value() As Object Implements IEvaluableValue.Value
            Get
                Return Nothing
            End Get
        End Property

        Protected Overrides Function _Clone() As Object
            Return Clone()
        End Function

        Public Overloads Function Clone() As DBNullValue
            Return New DBNullValue()
        End Function
    End Class

End Namespace