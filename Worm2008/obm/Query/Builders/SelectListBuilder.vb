Imports Worm.Entities
Imports Worm.Entities.Meta
Imports System.Collections.Generic

Namespace Query
    Public Class SCtor
        Public Shared Function prop(ByVal t As Type, ByVal propertyAlias As String) As SCtor
            Return prop(New ObjectSource(t), propertyAlias)
        End Function

        Public Shared Function prop(ByVal entityName As String, ByVal propertyAlias As String) As SCtor
            Return prop(New ObjectSource(entityName), propertyAlias)
        End Function

        Public Shared Function prop(ByVal [alias] As ObjectAlias, ByVal propertyAlias As String) As SCtor
            Return prop(New ObjectSource([alias]), propertyAlias)
        End Function

        Public Shared Function prop(ByVal os As ObjectSource, ByVal propertyAlias As String) As SCtor
            Dim f As New SCtor
            f.GetAllProperties.Add(New SelectExpression(os, propertyAlias))
            Return f
        End Function

        Public Shared Function prop(ByVal op As ObjectProperty) As SCtor
            Dim f As New SCtor
            f.GetAllProperties.Add(New SelectExpression(op.ObjectSource, op.Field))
            Return f
        End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String) As SCtor
            Dim f As New SCtor
            f.GetAllProperties.Add(New SelectExpression(table, tableColumn))
            Return f
        End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String) As SCtor
            Dim f As New SCtor
            f.GetAllProperties.Add(New SelectExpression(table, tableColumn, [alias]))
            Return f
        End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String, ByVal attr As Field2DbRelations) As SCtor
            Dim f As New SCtor
            Dim p As New SelectExpression(table, tableColumn, [alias])
            p.Attributes = attr
            f.GetAllProperties.Add(p)
            Return f
        End Function

        Public Shared Function count() As SCtor
            Dim f As New SCtor
            f.GetAllProperties.Add(New SelectExpression(New Aggregate(AggregateFunction.Count)))
            Return f
        End Function

        Public Shared Function count(ByVal [alias] As String) As SCtor
            Dim f As New SCtor
            f.GetAllProperties.Add(New SelectExpression(New Aggregate(AggregateFunction.Count, [alias])))
            Return f
        End Function

        Private _l As List(Of SelectExpression)

        Public Function Add(ByVal t As Type, ByVal propertyAlias As String) As SCtor
            GetAllProperties.Add(New SelectExpression(t, propertyAlias))
            Return Me
        End Function

        Public Function Add(ByVal table As SourceFragment, ByVal tableColumn As String) As SCtor
            GetAllProperties.Add(New SelectExpression(table, tableColumn))
            Return Me
        End Function

        Public Function Add(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String) As SCtor
            GetAllProperties.Add(New SelectExpression(table, tableColumn, [alias]))
            Return Me
        End Function

        Public Function Add(ByVal table As SourceFragment, ByVal tableColumn As String, ByVal [alias] As String, ByVal attr As Field2DbRelations) As SCtor
            Dim p As New SelectExpression(table, tableColumn, [alias])
            p.Attributes = attr
            GetAllProperties.Add(p)
            Return Me
        End Function

        Public Function GetAllProperties() As List(Of SelectExpression)
            If _l Is Nothing Then
                _l = New List(Of SelectExpression)
            End If
            Return _l
        End Function

        Public Shared Widening Operator CType(ByVal f As SCtor) As SelectExpression()
            Return f.GetAllProperties.ToArray
        End Operator

        Public Shared Widening Operator CType(ByVal f As SCtor) As Grouping()
            Return f.GetAllProperties.ConvertAll(Function(p As SelectExpression) New Grouping(p)).ToArray
        End Operator
    End Class

End Namespace