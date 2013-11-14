Imports System.ComponentModel
Imports Worm.Expressions2
Imports Worm.Entities.Meta
Imports System.Collections.Generic

Namespace Query
    Public Class FCtor

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

        Public Shared Function count() As Int
            Dim f As New Int
            f.AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Count))
            Return f
        End Function

        Public Shared Function count_distinct(ByVal tbl As SourceFragment, ByVal col As String) As Int
            Return count_distinct(New TableExpression(tbl, col))
        End Function

        Public Shared Function count_distinct(ByVal op As ObjectProperty) As Int
            Return count_distinct(New EntityExpression(op))
        End Function

        Public Shared Function count_distinct(ByVal pa As String) As Int
            Return count_distinct(New PropertyAliasExpression(pa))
        End Function

        Public Shared Function count_distinct(ByVal exp As IGetExpression) As Int
            Dim f As New Int
            f.AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Count, exp) With {.Distinct = True})
            Return f
        End Function

        Public Shared Function sum(ByVal table As SourceFragment, ByVal column As String) As Int
            Return sum(New TableExpression(table, column))
        End Function

        Public Shared Function sum(ByVal op As ObjectProperty) As Int
            Return sum(New EntityExpression(op))
        End Function

        Public Shared Function sum(ByVal pa As String) As Int
            Return sum(New PropertyAliasExpression(pa))
        End Function

        Public Shared Function sum(ByVal exp As IGetExpression) As Int
            Dim f As New Int
            f.AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Sum, exp))
            Return f
        End Function

        Public Shared Function max(ByVal exp As IGetExpression) As Int
            Dim f As New Int
            f.AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Max, exp))
            Return f
        End Function

        Public Shared Function max(ByVal op As ObjectProperty) As Int
            Return max(New EntityExpression(op))
        End Function

        Public Shared Function max(ByVal tbl As SourceFragment, ByVal column As String) As Int
            Return max(New TableExpression(tbl, column))
        End Function

        Public Shared Function max(ByVal pa As String) As Int
            Return max(New PropertyAliasExpression(pa))
        End Function

        Public Shared Function min(ByVal exp As IGetExpression) As Int
            Dim f As New Int
            f.AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Min, exp))
            Return f
        End Function

        Public Shared Function min(ByVal pa As String) As Int
            Return min(New PropertyAliasExpression(pa))
        End Function

        Public Shared Function min(ByVal op As ObjectProperty) As Int
            Return min(New EntityExpression(op))
        End Function

        Public Shared Function min(ByVal tbl As SourceFragment, ByVal column As String) As Int
            Return min(New TableExpression(tbl, column))
        End Function

        Public Shared Function avg(ByVal tbl As SourceFragment, ByVal column As String) As Int
            Return avg(New TableExpression(tbl, column))
        End Function

        Public Shared Function avg(ByVal op As ObjectProperty) As Int
            Return avg(New EntityExpression(op))
        End Function

        Public Shared Function avg(ByVal pa As String) As Int
            Return avg(New PropertyAliasExpression(pa))
        End Function

        Public Shared Function avg(ByVal exp As IGetExpression) As Int
            Dim f As New Int
            f.AppendExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Average, exp))
            Return f
        End Function

        Public Shared Function query(ByVal q As QueryCmd) As Int
            Dim f As New Int
            f.AppendExpression(New QueryExpression(q))
            Return f
        End Function

        Public Shared Function Exp(ByVal expression As IGetExpression) As Int
            Dim f As New Int
            If expression IsNot Nothing Then
                f.AppendExpression(expression.Expression)
            End If
            Return f
        End Function

        Public Shared Function literal(s As Object) As Int
            Dim f As New Int
            If s IsNot Nothing Then
                f.AppendExpression(LiteralExpression.Create(s))
            End If
            Return f
        End Function

        Public Shared Function param(value As Object) As Int
            Dim f As New Int
            If value Is Nothing Then
                f.AppendExpression(New DBNullExpression())
            Else
                f.AppendExpression(New ParameterExpression(value))
            End If
            Return f
        End Function

        'Public Shared Function Exp(ByVal ParamArray expressions() As ECtor.Int) As SelectExpression()
        '    Dim f As New List(Of SelectExpression)
        '    For Each ei As ECtor.Int In expressions
        '        For Each e As IGetExpression In ei.GetExpressions
        '            If TypeOf e Is SelectExpression Then
        '                f.Add(CType(e, SelectExpression))
        '            Else
        '                f.Add(New SelectExpression(e.Expression))
        '            End If
        '        Next
        '    Next
        '    Return f.ToArray
        'End Function
#End Region

        <EditorBrowsable(EditorBrowsableState.Never)> _
        Class Int
            Inherits ExpCtor(Of Int).Int

            Public Function [as](ByVal columnAlias As String) As Int
                If _l IsNot Nothing AndAlso _l.Count > 0 Then
                    CType(_l(_l.Count - 1), SelectExpression).ColumnAlias = columnAlias
                End If
                Return Me
            End Function

            Public Function into(ByVal propertyAlias As String) As Int
                If _l IsNot Nothing AndAlso _l.Count > 0 Then
                    CType(_l(_l.Count - 1), SelectExpression).IntoPropertyAlias = propertyAlias
                End If
                Return Me
            End Function

            Public Function into(ByVal propertyAlias As String, ByVal attr As Field2DbRelations) As Int
                If _l IsNot Nothing AndAlso _l.Count > 0 Then
                    CType(_l(_l.Count - 1), SelectExpression).IntoPropertyAlias = propertyAlias
                    CType(_l(_l.Count - 1), SelectExpression).Attributes = attr
                End If
                Return Me
            End Function

            Public Function into(ByVal op As ObjectProperty) As Int
                If _l IsNot Nothing AndAlso _l.Count > 0 Then
                    CType(_l(_l.Count - 1), SelectExpression).IntoPropertyAlias = op.PropertyAlias
                    CType(_l(_l.Count - 1), SelectExpression).Into = op.Entity
                End If
                Return Me
            End Function

            Public Function into(ByVal t As Type, ByVal propertyAlias As String) As Int
                If _l IsNot Nothing AndAlso _l.Count > 0 Then
                    CType(_l(_l.Count - 1), SelectExpression).IntoPropertyAlias = propertyAlias
                    CType(_l(_l.Count - 1), SelectExpression).Into = New EntityUnion(t)
                End If
                Return Me
            End Function

            Public Function into(ByVal entityName As String, ByVal propertyAlias As String) As Int
                If _l IsNot Nothing AndAlso _l.Count > 0 Then
                    CType(_l(_l.Count - 1), SelectExpression).IntoPropertyAlias = propertyAlias
                    CType(_l(_l.Count - 1), SelectExpression).Into = New EntityUnion(entityName)
                End If
                Return Me
            End Function

            Protected Overrides Function Wrap(ByVal exp As Expressions2.IExpression) As Expressions2.IExpression
                If TypeOf exp Is SelectExpression Then
                    Return exp
                Else
                    Return New SelectExpression(exp)
                End If
            End Function

            Public Overloads Shared Widening Operator CType(ByVal f As Int) As ObjectModel.ReadOnlyCollection(Of SelectExpression)
                Return New ObjectModel.ReadOnlyCollection(Of SelectExpression)(f._l.ConvertAll(Function(e) CType(e, SelectExpression)))
            End Operator

            Public Overloads Shared Widening Operator CType(ByVal f As Int) As SelectExpression
                Return CType(f._l(0), SelectExpression)
            End Operator

        End Class

    End Class
End Namespace