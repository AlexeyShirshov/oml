Imports Worm.Entities
Imports Worm.Entities.Meta
Imports System.Collections.Generic
Imports Worm.Expressions
Imports Worm.Criteria.Values

Namespace Query
    Public Class FCtor

#Region " Shared "
        Public Shared Function prop(ByVal t As Type, ByVal propertyAlias As String) As Int
            Return prop(New EntityUnion(t), propertyAlias)
        End Function

        Public Shared Function prop(ByVal t As Type, ByVal propertyAlias As String, ByVal fieldAlias As String) As Int
            Return prop(New EntityUnion(t), propertyAlias, fieldAlias)
        End Function

        Public Shared Function prop(ByVal entityName As String, ByVal propertyAlias As String) As Int
            Return prop(New EntityUnion(entityName), propertyAlias)
        End Function

        Public Shared Function prop(ByVal [alias] As EntityAlias, ByVal propertyAlias As String) As Int
            Return prop(New EntityUnion([alias]), propertyAlias)
        End Function

        Public Shared Function prop(ByVal os As EntityUnion, ByVal propertyAlias As String) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(os, propertyAlias))
            Return f
        End Function

        Public Shared Function prop(ByVal os As EntityUnion, ByVal propertyAlias As String, ByVal fieldAlias As String) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(os, propertyAlias, fieldAlias))
            Return f
        End Function

        Public Shared Function prop(ByVal op As ObjectProperty, ByVal [alias] As String) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(op, [alias]))
            Return f
        End Function

        Public Shared Function prop(ByVal op As ObjectProperty) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(op))
            Return f
        End Function

        Public Shared Function prop(ByVal op As ObjectProperty, ByVal intoPropertyAlias As String, ByVal into As Type) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(op, intoPropertyAlias, into))
            Return f
        End Function

        Public Shared Function prop(ByVal op As ObjectProperty, ByVal intoPropertyAlias As String, ByVal intoEntityName As String) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(op, intoPropertyAlias, intoEntityName))
            Return f
        End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(table, tableColumn))
            Return f
        End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(table, tableColumn, [alias]))
            Return f
        End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String, ByVal attr As Field2DbRelations) As Int
            Dim f As New FCtor.Int
            Dim p As New SelectExpression(table, tableColumn, [alias])
            p.Attributes = attr
            f.GetAllProperties.Add(p)
            Return f
        End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal propertyAlias As String, ByVal intoType As Type) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(table, tableColumn, propertyAlias, intoType))
            Return f
        End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal propertyAlias As String, ByVal intoEntityName As String) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(table, tableColumn, propertyAlias, intoEntityName))
            Return f
        End Function

        Public Shared Function column(ByVal inner As QueryCmd) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(inner))
            Return f
        End Function

        Public Shared Function column(ByVal inner As QueryCmd, ByVal fieldAlias As String) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(inner, fieldAlias))
            Return f
        End Function

        Public Shared Function custom(ByVal expression As String) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(expression, CType(Nothing, FieldReference())))
            Return f
        End Function

        Public Shared Function custom(ByVal expression As String, ByVal ParamArray params() As FieldReference) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(expression, params))
            Return f
        End Function

        Public Shared Function custom(ByVal [alias] As String, ByVal expression As String, ByVal ParamArray params() As FieldReference) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(expression, params, [alias]))
            Return f
        End Function

        Public Shared Function custom(ByVal intoProp As ObjectProperty, ByVal attr As Field2DbRelations, ByVal expression As String, ByVal ParamArray params() As FieldReference) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(expression, params, intoProp, attr))
            Return f
        End Function

        Public Shared Function custom(ByVal intoProp As ObjectProperty, ByVal expression As String, ByVal ParamArray params() As FieldReference) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(expression, params, intoProp))
            Return f
        End Function

        Public Shared Function custom(ByVal propertyAlias As String, ByVal intoType As Type, ByVal expression As String, ByVal ParamArray params() As FieldReference) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(expression, params, propertyAlias, intoType))
            Return f
        End Function

        Public Shared Function custom(ByVal propertyAlias As String, ByVal intoEntityName As String, ByVal expression As String, ByVal ParamArray params() As FieldReference) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(expression, params, propertyAlias, intoEntityName))
            Return f
        End Function

        Public Shared Function count() As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(New Aggregate(AggregateFunction.Count)))
            Return f
        End Function

        Public Shared Function count(ByVal [alias] As String) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(New Aggregate(AggregateFunction.Count, [alias])))
            Return f
        End Function

        Public Shared Function max(ByVal exp As SelectExpression, ByVal [alias] As String) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(New SelectExpression(New Aggregate(AggregateFunction.Max, [alias], New UnaryExp(New FieldValue(exp)))))
            Return f
        End Function

        Public Shared Function Exp(ByVal expression As SelectExpression) As Int
            Dim f As New FCtor.Int
            f.GetAllProperties.Add(expression)
            Return f
        End Function
#End Region

        Class Int
            Private _l As List(Of SelectExpression)

            Public Function GetAllProperties() As List(Of SelectExpression)
                If _l Is Nothing Then
                    _l = New List(Of SelectExpression)
                End If
                Return _l
            End Function

            Public Shared Widening Operator CType(ByVal f As Int) As SelectExpression()
                Return f.GetAllProperties.ToArray
            End Operator

            Public Shared Widening Operator CType(ByVal f As Int) As Grouping()
                Return f.GetAllProperties.ConvertAll(Function(p As SelectExpression) New Grouping(p)).ToArray
            End Operator

#Region " Members "
            Public Function prop(ByVal t As Type, ByVal propertyAlias As String) As Int
                GetAllProperties.Add(New SelectExpression(t, propertyAlias))
                Return Me
            End Function

            Public Function prop(ByVal entityName As String, ByVal propertyAlias As String) As Int
                GetAllProperties.Add(New SelectExpression(entityName, propertyAlias))
                Return Me
            End Function

            Public Function prop(ByVal [alias] As EntityAlias, ByVal propertyAlias As String) As Int
                GetAllProperties.Add(New SelectExpression([alias], propertyAlias))
                Return Me
            End Function

            Public Function prop(ByVal os As EntityUnion, ByVal propertyAlias As String) As Int
                GetAllProperties.Add(New SelectExpression(os, propertyAlias))
                Return Me
            End Function

            Public Function prop(ByVal op As ObjectProperty) As Int
                GetAllProperties.Add(New SelectExpression(op.ObjectSource, op.Field))
                Return Me
            End Function

            Public Function column(ByVal table As SourceFragment, ByVal tableColumn As String) As Int
                GetAllProperties.Add(New SelectExpression(table, tableColumn))
                Return Me
            End Function

            Public Function column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String) As Int
                GetAllProperties.Add(New SelectExpression(table, tableColumn, [alias]))
                Return Me
            End Function

            Public Function column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String, ByVal attr As Field2DbRelations) As Int
                Dim p As New SelectExpression(table, tableColumn, [alias])
                p.Attributes = attr
                GetAllProperties.Add(p)
                Return Me
            End Function

            Public Function column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal propertyAlias As String, ByVal intoType As Type) As Int
                GetAllProperties.Add(New SelectExpression(table, tableColumn, propertyAlias, intoType))
                Return Me
            End Function

            Public Function column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal propertyAlias As String, ByVal intoEntityName As String) As Int
                GetAllProperties.Add(New SelectExpression(table, tableColumn, propertyAlias, intoEntityName))
                Return Me
            End Function

            Public Function column(ByVal query As QueryCmd) As Int
                GetAllProperties.Add(New SelectExpression(query))
                Return Me
            End Function

            Public Function column(ByVal query As QueryCmd, ByVal fieldAlias As String) As Int
                GetAllProperties.Add(New SelectExpression(query, fieldAlias))
                Return Me
            End Function

            Public Function custom(ByVal expression As String) As Int
                GetAllProperties.Add(New SelectExpression(expression, CType(Nothing, FieldReference())))
                Return Me
            End Function

            Public Function custom(ByVal expression As String, ByVal ParamArray params() As FieldReference) As Int
                GetAllProperties.Add(New SelectExpression(expression, params))
                Return Me
            End Function

            Public Function custom(ByVal [alias] As String, ByVal expression As String, ByVal ParamArray params() As FieldReference) As Int
                GetAllProperties.Add(New SelectExpression(expression, params, [alias]))
                Return Me
            End Function

            Public Function custom(ByVal propertyAlias As String, ByVal intoType As Type, ByVal expression As String, ByVal ParamArray params() As FieldReference) As Int
                GetAllProperties.Add(New SelectExpression(expression, params, propertyAlias, intoType))
                Return Me
            End Function

            Public Function custom(ByVal propertyAlias As String, ByVal intoEntityName As String, ByVal expression As String, ByVal ParamArray params() As FieldReference) As Int
                GetAllProperties.Add(New SelectExpression(expression, params, propertyAlias, intoEntityName))
                Return Me
            End Function

            Public Function count() As Int
                GetAllProperties.Add(New SelectExpression(New Aggregate(AggregateFunction.Count)))
                Return Me
            End Function

            Public Function count(ByVal [alias] As String) As Int
                GetAllProperties.Add(New SelectExpression(New Aggregate(AggregateFunction.Count, [alias])))
                Return Me
            End Function

            Public Function sum(ByVal column As String) As Int
                GetAllProperties.Add(New SelectExpression(New Aggregate(AggregateFunction.Sum, New UnaryExp(New LiteralValue(column)))))
                Return Me
            End Function

            Public Function sum(ByVal column As String, ByVal [alias] As String) As Int
                GetAllProperties.Add(New SelectExpression(New Aggregate(AggregateFunction.Sum, [alias], New UnaryExp(New LiteralValue(column)))))
                Return Me
            End Function

            Public Function max(ByVal exp As SelectExpression, ByVal [alias] As String) As Int
                GetAllProperties.Add(New SelectExpression(New Aggregate(AggregateFunction.Max, [alias], New UnaryExp(New FieldValue(exp)))))
                Return Me
            End Function

            Public Function Exp(ByVal expression As SelectExpression) As Int
                GetAllProperties.Add(expression)
                Return Me
            End Function
#End Region

        End Class
    End Class

End Namespace