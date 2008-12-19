Imports Worm.Entities
Imports Worm.Entities.Meta
Imports System.Collections.Generic

Namespace Query
    Public Class FCtor

#Region " Shared "
        Public Shared Function prop(ByVal t As Type, ByVal propertyAlias As String) As FCtor
            Return prop(New ObjectSource(t), propertyAlias)
        End Function

        Public Shared Function prop(ByVal t As Type, ByVal propertyAlias As String, ByVal fieldAlias As String) As FCtor
            Return prop(New ObjectSource(t), propertyAlias, fieldAlias)
        End Function

        Public Shared Function prop(ByVal entityName As String, ByVal propertyAlias As String) As FCtor
            Return prop(New ObjectSource(entityName), propertyAlias)
        End Function

        Public Shared Function prop(ByVal [alias] As ObjectAlias, ByVal propertyAlias As String) As FCtor
            Return prop(New ObjectSource([alias]), propertyAlias)
        End Function

        Public Shared Function prop(ByVal os As ObjectSource, ByVal propertyAlias As String) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(os, propertyAlias))
            Return f
        End Function

        Public Shared Function prop(ByVal os As ObjectSource, ByVal propertyAlias As String, ByVal fieldAlias As String) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(os, propertyAlias, fieldAlias))
            Return f
        End Function

        Public Shared Function prop(ByVal op As ObjectProperty) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(op.ObjectSource, op.Field))
            Return f
        End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(table, tableColumn))
            Return f
        End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(table, tableColumn, [alias]))
            Return f
        End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String, ByVal attr As Field2DbRelations) As FCtor
            Dim f As New FCtor
            Dim p As New SelectExpression(table, tableColumn, [alias])
            p.Attributes = attr
            f.GetAllProperties.Add(p)
            Return f
        End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal propertyAlias As String, ByVal intoType As Type) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(table, tableColumn, propertyAlias, intoType))
            Return f
        End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal propertyAlias As String, ByVal intoEntityName As String) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(table, tableColumn, propertyAlias, intoEntityName))
            Return f
        End Function

        Public Shared Function custom(ByVal expression As String) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(expression, CType(Nothing, FieldReference())))
            Return f
        End Function

        Public Shared Function custom(ByVal expression As String, ByVal ParamArray params() As FieldReference) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(expression, params))
            Return f
        End Function

        Public Shared Function custom(ByVal [alias] As String, ByVal expression As String, ByVal ParamArray params() As FieldReference) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(expression, params, [alias]))
            Return f
        End Function

        Public Shared Function custom(ByVal propertyAlias As String, ByVal intoType As Type, ByVal expression As String, ByVal ParamArray params() As FieldReference) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(expression, params, propertyAlias, intoType))
            Return f
        End Function

        Public Shared Function custom(ByVal propertyAlias As String, ByVal intoEntityName As String, ByVal expression As String, ByVal ParamArray params() As FieldReference) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(expression, params, propertyAlias, intoEntityName))
            Return f
        End Function

        Public Shared Function count() As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(New Aggregate(AggregateFunction.Count)))
            Return f
        End Function

        Public Shared Function count(ByVal [alias] As String) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(New SelectExpression(New Aggregate(AggregateFunction.Count, [alias])))
            Return f
        End Function

        Public Shared Function Exp(ByVal expression As SelectExpression) As FCtor
            Dim f As New FCtor
            f.GetAllProperties.Add(expression)
            Return f
        End Function
#End Region

        Private _l As List(Of SelectExpression)

#Region " Members "
        Public Function Add_prop(ByVal t As Type, ByVal propertyAlias As String) As FCtor
            GetAllProperties.Add(New SelectExpression(t, propertyAlias))
            Return Me
        End Function

        Public Function Add_prop(ByVal entityName As String, ByVal propertyAlias As String) As FCtor
            GetAllProperties.Add(New SelectExpression(entityName, propertyAlias))
            Return Me
        End Function

        Public Function Add_prop(ByVal [alias] As ObjectAlias, ByVal propertyAlias As String) As FCtor
            GetAllProperties.Add(New SelectExpression([alias], propertyAlias))
            Return Me
        End Function

        Public Function Add_prop(ByVal os As ObjectSource, ByVal propertyAlias As String) As FCtor
            GetAllProperties.Add(New SelectExpression(os, propertyAlias))
            Return Me
        End Function

        Public Function Add_prop(ByVal op As ObjectProperty) As FCtor
            GetAllProperties.Add(New SelectExpression(op.ObjectSource, op.Field))
            Return Me
        End Function

        Public Function Add_column(ByVal table As SourceFragment, ByVal tableColumn As String) As FCtor
            GetAllProperties.Add(New SelectExpression(table, tableColumn))
            Return Me
        End Function

        Public Function Add_column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String) As FCtor
            GetAllProperties.Add(New SelectExpression(table, tableColumn, [alias]))
            Return Me
        End Function

        Public Function Add_column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String, ByVal attr As Field2DbRelations) As FCtor
            Dim p As New SelectExpression(table, tableColumn, [alias])
            p.Attributes = attr
            GetAllProperties.Add(p)
            Return Me
        End Function

        Public Function Add_column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal propertyAlias As String, ByVal intoType As Type) As FCtor
            GetAllProperties.Add(New SelectExpression(table, tableColumn, propertyAlias, intoType))
            Return Me
        End Function

        Public Function Add_column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal propertyAlias As String, ByVal intoEntityName As String) As FCtor
            GetAllProperties.Add(New SelectExpression(table, tableColumn, propertyAlias, intoEntityName))
            Return Me
        End Function

        Public Function Add_custom(ByVal expression As String) As FCtor
            GetAllProperties.Add(New SelectExpression(expression, CType(Nothing, FieldReference())))
            Return Me
        End Function

        Public Function Add_custom(ByVal expression As String, ByVal ParamArray params() As FieldReference) As FCtor
            GetAllProperties.Add(New SelectExpression(expression, params))
            Return Me
        End Function

        Public Function Add_custom(ByVal [alias] As String, ByVal expression As String, ByVal ParamArray params() As FieldReference) As FCtor
            GetAllProperties.Add(New SelectExpression(expression, params, [alias]))
            Return Me
        End Function

        Public Function Add_custom(ByVal propertyAlias As String, ByVal intoType As Type, ByVal expression As String, ByVal ParamArray params() As FieldReference) As FCtor
            GetAllProperties.Add(New SelectExpression(expression, params, propertyAlias, intoType))
            Return Me
        End Function

        Public Function Add_custom(ByVal propertyAlias As String, ByVal intoEntityName As String, ByVal expression As String, ByVal ParamArray params() As FieldReference) As FCtor
            GetAllProperties.Add(New SelectExpression(expression, params, propertyAlias, intoEntityName))
            Return Me
        End Function

        Public Function Add_count() As FCtor
            GetAllProperties.Add(New SelectExpression(New Aggregate(AggregateFunction.Count)))
            Return Me
        End Function

        Public Function Add_count(ByVal [alias] As String) As FCtor
            GetAllProperties.Add(New SelectExpression(New Aggregate(AggregateFunction.Count, [alias])))
            Return Me
        End Function

        Public Function AddExp(ByVal expression As SelectExpression) As FCtor
            GetAllProperties.Add(expression)
            Return Me
        End Function
#End Region

        Public Function GetAllProperties() As List(Of SelectExpression)
            If _l Is Nothing Then
                _l = New List(Of SelectExpression)
            End If
            Return _l
        End Function

        Public Shared Widening Operator CType(ByVal f As FCtor) As SelectExpression()
            Return f.GetAllProperties.ToArray
        End Operator

        Public Shared Widening Operator CType(ByVal f As FCtor) As Grouping()
            Return f.GetAllProperties.ConvertAll(Function(p As SelectExpression) New Grouping(p)).ToArray
        End Operator
    End Class

End Namespace