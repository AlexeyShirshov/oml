Imports System.Collections.Generic
Imports System.ComponentModel

Namespace Orm

    <DefaultProperty("Item")> _
    Public Class AnonymousEntity
        Inherits Entity

        Private _props As New Dictionary(Of String, Object)

        Public Overrides Function GetValue(ByVal pi As System.Reflection.PropertyInfo, ByVal c As Meta.ColumnAttribute, ByVal oschema As Meta.IOrmObjectSchemaBase) As Object
            Return _props(c.FieldName)
        End Function

        Public Overrides Sub SetValue(ByVal pi As System.Reflection.PropertyInfo, ByVal c As Meta.ColumnAttribute, ByVal schema As Meta.IOrmObjectSchemaBase, ByVal value As Object)
            _props(c.FieldName) = value
        End Sub

        Public ReadOnly Property Item(ByVal field As String) As Object
            Get
                Return GetValue(field)
            End Get
        End Property
    End Class

End Namespace