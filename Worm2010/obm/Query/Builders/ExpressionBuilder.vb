Imports Worm.Entities
Imports Worm.Entities.Meta
Imports System.Collections.Generic
Imports Worm.Expressions
Imports Worm.Criteria.Values
Imports Worm.Expressions2
Imports System.ComponentModel

Namespace Query
    Public Class ExpCtorBase(Of T As {New, IntBase})

        <EditorBrowsable(EditorBrowsableState.Never)> _
        Public Class IntBase
            Implements IGetExpression

            Protected _l As List(Of IExpression)

            Public Function AddExpression(ByVal exp As IExpression) As T
                GetAllProperties.Add(Wrap(exp))
                Return CType(Me, T)
            End Function

            Protected Overridable Function Wrap(ByVal exp As IExpression) As IExpression
                Return exp
            End Function

            Public Function GetAllProperties() As List(Of IExpression)
                If _l Is Nothing Then
                    _l = New List(Of IExpression)
                End If
                Return _l
            End Function

            'Public Shared Widening Operator CType(ByVal f As IntBase) As IExpression()
            '    Return f.GetAllProperties.ToArray
            'End Operator

#Region " Members "
            Public Function prop(ByVal propertyAlias As String) As T
                Return prop(New PropertyAliasExpression(propertyAlias))
            End Function

            Public Function prop(ByVal t As Type, ByVal propertyAlias As String) As T
                Return prop(New EntityUnion(t), propertyAlias)
            End Function

            Public Function prop(ByVal entityName As String, ByVal propertyAlias As String) As T
                Return prop(New EntityUnion(entityName), propertyAlias)
            End Function

            Public Function prop(ByVal [alias] As QueryAlias, ByVal propertyAlias As String) As T
                Return prop(New EntityUnion([alias]), propertyAlias)
            End Function

            Public Function prop(ByVal os As EntityUnion, ByVal propertyAlias As String) As T
                Return prop(New EntityExpression(propertyAlias, os))
            End Function

            Public Function prop(ByVal op As ObjectProperty) As T
                Return prop(New EntityExpression(op))
            End Function

            Public Function prop(ByVal exp As IGetExpression) As T
                AddExpression(exp.Expression)
                Return CType(Me, T)
            End Function

            Public Function column(ByVal table As SourceFragment, ByVal tableColumn As String) As T
                AddExpression(New TableExpression(table, tableColumn))
                Return CType(Me, T)
            End Function

            Public Function column(ByVal inner As QueryCmd) As T
                AddExpression(New QueryExpression(inner))
                Return CType(Me, T)
            End Function

            Public Function custom(ByVal expression As String) As T
                AddExpression(New CustomExpression(expression))
                Return CType(Me, T)
            End Function

            Public Function custom(ByVal expression As String, ByVal ParamArray params() As IGetExpression) As T
                AddExpression(New CustomExpression(expression, params))
                Return CType(Me, T)
            End Function

            Public Function Exp(ByVal expression As IGetExpression) As T
                GetAllProperties.Add(expression.Expression)
                Return CType(Me, T)
            End Function
#End Region

            Public ReadOnly Property Expression() As Expressions2.IExpression Implements Expressions2.IGetExpression.Expression
                Get
                    Return GetAllProperties()(0)
                End Get
            End Property
        End Class

    End Class

    Public Class ExpCtor(Of T As {New, Int})

#Region " Shared "
        Public Shared Function prop(ByVal propertyAlias As String) As T
            Return prop(New PropertyAliasExpression(propertyAlias))
        End Function

        Public Shared Function prop(ByVal t As Type, ByVal propertyAlias As String) As T
            Return prop(New EntityUnion(t), propertyAlias)
        End Function

        Public Shared Function prop(ByVal entityName As String, ByVal propertyAlias As String) As T
            Return prop(New EntityUnion(entityName), propertyAlias)
        End Function

        Public Shared Function prop(ByVal [alias] As QueryAlias, ByVal propertyAlias As String) As T
            Return prop(New EntityUnion([alias]), propertyAlias)
        End Function

        Public Shared Function prop(ByVal os As EntityUnion, ByVal propertyAlias As String) As T
            Return prop(New EntityExpression(propertyAlias, os))
        End Function

        Public Shared Function prop(ByVal op As ObjectProperty) As T
            Return prop(New EntityExpression(op))
        End Function

        Public Shared Function prop(ByVal exp As IGetExpression) As T
            Dim f As New T
            f.AddExpression(exp.Expression)
            Return f
        End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String) As T
            Dim f As New T
            f.AddExpression(New TableExpression(table, tableColumn))
            Return f
        End Function

        Public Shared Function column(ByVal inner As QueryCmd) As T
            Dim f As New T
            f.AddExpression(New QueryExpression(inner))
            Return f
        End Function

        Public Shared Function custom(ByVal expression As String) As T
            Dim f As New T
            f.AddExpression(New CustomExpression(expression))
            Return f
        End Function

        Public Shared Function custom(ByVal expression As String, ByVal ParamArray params() As IGetExpression) As T
            Dim f As New T
            f.AddExpression(New CustomExpression(expression, params))
            Return f
        End Function

        Public Shared Function count() As T
            Dim f As New T
            f.AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Count))
            Return f
        End Function

        Public Shared Function count_distinct(ByVal tbl As SourceFragment, ByVal col As String) As T
            Return count_distinct(New TableExpression(tbl, col))
        End Function

        Public Shared Function count_distinct(ByVal op As ObjectProperty) As T
            Return count_distinct(New EntityExpression(op))
        End Function

        Public Shared Function count_distinct(ByVal exp As IGetExpression) As T
            Dim f As New T
            f.AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Count, exp) With {.Distinct = True})
            Return f
        End Function

        Public Shared Function sum(ByVal table As SourceFragment, ByVal column As String) As T
            Return sum(New TableExpression(table, column))
        End Function

        Public Shared Function sum(ByVal op As ObjectProperty) As T
            Return sum(New EntityExpression(op))
        End Function

        Public Shared Function sum(ByVal exp As IExpression) As T
            Dim f As New T
            f.AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Sum, exp))
            Return f
        End Function

        Public Shared Function max(ByVal exp As IGetExpression) As T
            Dim f As New T
            f.AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Max, exp))
            Return f
        End Function

        Public Shared Function max(ByVal op As ObjectProperty) As T
            Return max(New EntityExpression(op))
        End Function

        Public Shared Function max(ByVal tbl As SourceFragment, ByVal column As String) As T
            Return max(New TableExpression(tbl, column))
        End Function

        Public Shared Function min(ByVal exp As IGetExpression) As T
            Dim f As New T
            f.AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Min, exp))
            Return f
        End Function

        Public Shared Function min(ByVal op As ObjectProperty) As T
            Return min(New EntityExpression(op))
        End Function

        Public Shared Function min(ByVal tbl As SourceFragment, ByVal column As String) As T
            Return min(New TableExpression(tbl, column))
        End Function

        Public Shared Function avg(ByVal tbl As SourceFragment, ByVal column As String) As T
            Return avg(New TableExpression(tbl, column))
        End Function

        Public Shared Function avg(ByVal op As ObjectProperty) As T
            Return avg(New EntityExpression(op))
        End Function

        Public Shared Function avg(ByVal exp As IGetExpression) As T
            Dim f As New T
            f.AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Average, exp))
            Return f
        End Function

        Public Shared Function query(ByVal q As QueryCmd) As T
            Dim f As New T
            f.AddExpression(New QueryExpression(q))
            Return f
        End Function

        Public Shared Function Exp(ByVal expression As IGetExpression) As T
            Dim f As New T
            f.AddExpression(expression.Expression)
            Return f
        End Function
#End Region

        <EditorBrowsable(EditorBrowsableState.Never)> _
        Public Class Int
            Inherits ExpCtorBase(Of T).IntBase

            Public Function count() As T
                AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Count))
                Return CType(Me, T)
            End Function

            Public Function count_distinct(ByVal tbl As SourceFragment, ByVal col As String) As T
                Return count_distinct(New TableExpression(tbl, col))
            End Function

            Public Function count_distinct(ByVal op As ObjectProperty) As T
                Return count_distinct(New EntityExpression(op))
            End Function

            Public Function count_distinct(ByVal pa As String) As T
                Return count_distinct(New PropertyAliasExpression(pa))
            End Function

            Public Function count_distinct(ByVal exp As IGetExpression) As T
                AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Count, exp) With {.Distinct = True})
                Return CType(Me, T)
            End Function

            Public Function sum(ByVal table As SourceFragment, ByVal column As String) As T
                Return sum(New TableExpression(table, column))
            End Function

            Public Function sum(ByVal op As ObjectProperty) As T
                Return sum(New EntityExpression(op))
            End Function

            Public Function sum(ByVal pa As String) As T
                Return sum(New PropertyAliasExpression(pa))
            End Function

            Public Function sum(ByVal exp As IGetExpression) As T
                AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Sum, exp))
                Return CType(Me, T)
            End Function

            Public Function max(ByVal exp As IGetExpression) As T
                AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Max, exp))
                Return CType(Me, T)
            End Function

            Public Function max(ByVal op As ObjectProperty) As T
                Return max(New EntityExpression(op))
            End Function

            Public Function max(ByVal pa As String) As T
                Return max(New PropertyAliasExpression(pa))
            End Function

            Public Function max(ByVal tbl As SourceFragment, ByVal column As String) As T
                Return max(New TableExpression(tbl, column))
            End Function

            Public Function min(ByVal exp As IGetExpression) As T
                AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Min, exp))
                Return CType(Me, T)
            End Function

            Public Function min(ByVal pa As String) As T
                Return min(New PropertyAliasExpression(pa))
            End Function

            Public Function min(ByVal op As ObjectProperty) As T
                Return min(New EntityExpression(op))
            End Function

            Public Function min(ByVal tbl As SourceFragment, ByVal column As String) As T
                Return min(New TableExpression(tbl, column))
            End Function

            Public Function avg(ByVal tbl As SourceFragment, ByVal column As String) As T
                Return avg(New TableExpression(tbl, column))
            End Function

            Public Function avg(ByVal op As ObjectProperty) As T
                Return avg(New EntityExpression(op))
            End Function

            Public Function avg(ByVal pa As String) As T
                Return avg(New PropertyAliasExpression(pa))
            End Function

            Public Function avg(ByVal exp As IGetExpression) As T
                AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Average, exp))
                Return CType(Me, T)
            End Function

            Public Function query(ByVal q As QueryCmd) As T
                AddExpression(New QueryExpression(q))
                Return CType(Me, T)
            End Function
        End Class
    End Class

End Namespace