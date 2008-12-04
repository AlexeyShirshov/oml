Imports Worm.Criteria.Values
Imports Worm.Orm.Meta

Namespace Xml
    Namespace Criteria.Core
        Public Class XmlEntityTemplate
            Inherits Worm.Criteria.Core.OrmFilterTemplateBase

            Public Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal oper As Worm.Criteria.FilterOperation)
                MyBase.New(t, propertyAlias, oper)
            End Sub

            Public Sub New(ByVal entityName As String, ByVal propertyAlias As String, ByVal oper As Worm.Criteria.FilterOperation)
                MyBase.New(entityName, propertyAlias, oper)
            End Sub

            Public Sub New(ByVal [alias] As Orm.ObjectAlias, ByVal propertyAlias As String, ByVal oper As Worm.Criteria.FilterOperation)
                MyBase.New([alias], propertyAlias, oper)
            End Sub

            Public Sub New(ByVal os As Orm.ObjectSource, ByVal propertyAlias As String, ByVal oper As Worm.Criteria.FilterOperation)
                MyBase.New(os, propertyAlias, oper)
            End Sub

            Protected Overrides Function CreateEntityFilter(ByVal t As System.Type, ByVal propertyAlias As String, ByVal value As IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation) As Worm.Criteria.Core.EntityFilterBase
                Return New XmlEntityFilter(t, propertyAlias, value, operation)
            End Function

            Protected Overrides Function CreateEntityFilter(ByVal entityName As String, ByVal propertyAlias As String, ByVal value As Worm.Criteria.Values.IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation) As Worm.Criteria.Core.EntityFilterBase
                Return New XmlEntityFilter(entityName, propertyAlias, value, operation)
            End Function

            Public Overrides ReadOnly Property OperToStmt() As String
                Get
                    Return Oper2String(Operation)
                End Get
            End Property

            Protected Friend Shared Function Oper2String(ByVal oper As Worm.Criteria.FilterOperation) As String
                Select Case oper
                    Case Worm.Criteria.FilterOperation.Equal
                        Return " = "
                    Case Worm.Criteria.FilterOperation.GreaterEqualThan
                        Return " >= "
                    Case Worm.Criteria.FilterOperation.GreaterThan
                        Return " > "
                    Case Worm.Criteria.FilterOperation.NotEqual
                        Return " != "
                    Case Worm.Criteria.FilterOperation.LessEqualThan
                        Return " <= "
                    Case Worm.Criteria.FilterOperation.LessThan
                        Return " < "
                    Case Else
                        Throw New ObjectMappingException("invalid opration " & oper.ToString)
                End Select
            End Function

            Protected Overloads Overrides Function CreateEntityFilter(ByVal oa As Orm.ObjectAlias, ByVal propertyAlias As String, ByVal value As Worm.Criteria.Values.IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation) As Worm.Criteria.Core.EntityFilterBase
                Return New XmlEntityFilter(oa, propertyAlias, value, operation)
            End Function
        End Class

        Public Class XmlEntityFilter
            Inherits Worm.Criteria.Core.EntityFilterBase

            Public Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal value As IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
                MyBase.New(value, New XmlEntityTemplate(t, propertyAlias, operation))
            End Sub

            Public Sub New(ByVal entityName As String, ByVal propertyAlias As String, ByVal value As IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
                MyBase.New(value, New XmlEntityTemplate(entityName, propertyAlias, operation))
            End Sub

            Public Sub New(ByVal [alias] As Orm.ObjectAlias, ByVal propertyAlias As String, ByVal value As IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
                MyBase.New(value, New XmlEntityTemplate([alias], propertyAlias, operation))
            End Sub

            Public Sub New(ByVal os As Orm.ObjectSource, ByVal propertyAlias As String, ByVal value As IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
                MyBase.New(value, New XmlEntityTemplate(os, propertyAlias, operation))
            End Sub

            Protected Sub New(ByVal value As IFilterValue, ByVal tmp As XmlEntityTemplate)
                MyBase.New(value, tmp)
            End Sub

            Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String
                If _oschema Is Nothing Then
                    If Template.ObjectSource.AnyType IsNot Nothing Then
                        _oschema = schema.GetObjectSchema(Template.ObjectSource.AnyType)
                    Else
                        _oschema = schema.GetObjectSchema(schema.GetTypeByEntityName(Template.ObjectSource.AnyEntityName))
                    End If
                End If

                Return MakeQueryStmt(_oschema, filterInfo, schema, almgr, pname, columns)
            End Function

            Protected Overrides Function _Clone() As Object
                Return New XmlEntityFilter(val, CType(Template, XmlEntityTemplate))
            End Function

            Public Overloads Overrides Function MakeQueryStmt(ByVal oschema As Orm.Meta.IObjectSchemaBase, ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String
                If schema Is Nothing Then
                    Throw New ArgumentNullException("schema")
                End If

                Dim map As MapField2Column = oschema.GetFieldColumnMap()(Template.PropertyAlias)

                Return map._columnName & Template.OperToStmt & "'" & val._ToString & "'"
            End Function
        End Class
    End Namespace
End Namespace