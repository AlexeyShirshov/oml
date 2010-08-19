Imports Worm.Entities
Imports Worm.Query.Sorting
Imports Worm.Entities.Meta
Imports Worm.Criteria.Values
Imports Worm.Expressions2

Namespace Query

    Public Class GCtor

#Region " Shared "
        Public Shared Function prop(ByVal propertyAlias As String) As Int
            Return Exp(New PropertyAliasExpression(propertyAlias))
        End Function

        Public Shared Function prop(ByVal t As Type, ByVal propertyAlias As String) As Int
            Return prop(New EntityUnion(t), propertyAlias)
        End Function

        Public Shared Function prop(ByVal entityName As String, ByVal propertyAlias As String) As Int
            Return prop(New EntityUnion(entityName), propertyAlias)
        End Function

        Public Shared Function prop(ByVal [alias] As QueryAlias, ByVal propertyAlias As String) As Int
            Return prop(New EntityUnion([alias]), propertyAlias)
        End Function

        Public Shared Function prop(ByVal os As EntityUnion, ByVal propertyAlias As String) As Int
            Return Exp(New EntityExpression(propertyAlias, os))
        End Function

        Public Shared Function prop(ByVal op As ObjectProperty) As Int
            Return Exp(New EntityExpression(op))
        End Function

        'Public Shared Function prop(ByVal exp As IGetExpression) As Int
        '    Dim f As New Int
        '    f.AddExpression(exp.Expression)
        '    Return f
        'End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String) As Int
            Dim f As New Int
            f.AppendExpression(New TableExpression(table, tableColumn))
            Return f
        End Function

        Public Shared Function column(ByVal inner As QueryCmd) As Int
            Dim f As New Int
            f.AppendExpression(New QueryExpression(inner))
            Return f
        End Function

        Public Shared Function custom(ByVal expression As String) As Int
            Dim f As New Int
            f.AppendExpression(New CustomExpression(expression))
            Return f
        End Function

        Public Shared Function custom(ByVal expression As String, ByVal ParamArray params() As IGetExpression) As Int
            Dim f As New Int
            f.AppendExpression(New CustomExpression(expression, params))
            Return f
        End Function

        Public Shared Function Exp(ByVal expression As IGetExpression) As Int
            Dim f As New Int
            If expression IsNot Nothing Then
                f.AppendExpression(expression.Expression)
            End If
            Return f
        End Function
#End Region

        Class Int
            Inherits ExpCtorBase(Of Int).IntBase

            Private _type As GroupExpression.SummaryValues

            Public Overloads Shared Widening Operator CType(ByVal so As Int) As GroupExpression
                Return New GroupExpression(so._type, BinaryExpressionBase.CreateFromEnumerable(so.GetExpressions))
            End Operator
        End Class
    End Class

End Namespace