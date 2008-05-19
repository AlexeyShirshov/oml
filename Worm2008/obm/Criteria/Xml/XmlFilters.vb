Imports Worm.Criteria.Values
Imports Worm.Orm.Meta

Namespace Xml
    Namespace Criteria.Core
        Public Class XmlEntityTemplate
            Inherits Worm.Criteria.Core.OrmFilterTemplateBase

            Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal oper As Worm.Criteria.FilterOperation)
                MyBase.New(t, fieldName, oper)
            End Sub

            Protected Overrides Function CreateEntityFilter(ByVal t As System.Type, ByVal fieldName As String, ByVal value As IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation) As Worm.Criteria.Core.EntityFilterBase
                Return New XmlEntityFilter(t, fieldName, value, operation)
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
                        Throw New QueryGeneratorException("invalid opration " & oper.ToString)
                End Select
            End Function
        End Class

        Public Class XmlEntityFilter
            Inherits Worm.Criteria.Core.EntityFilterBase

            Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal value As IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
                MyBase.New(value, New XmlEntityTemplate(t, fieldName, operation))
            End Sub

            Protected Sub New(ByVal value As IFilterValue, ByVal tmp As XmlEntityTemplate)
                MyBase.New(value, tmp)
            End Sub

            Public Overrides Function MakeQueryStmt(ByVal schema As QueryGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam) As String
                If _oschema Is Nothing Then
                    _oschema = schema.GetObjectSchema(Template.Type)
                End If

                Return MakeQueryStmt(_oschema, filterInfo, schema, almgr, pname)
            End Function

            Protected Overrides Function _Clone() As Object
                Return New XmlEntityFilter(val, CType(Template, XmlEntityTemplate))
            End Function

            Public Overloads Overrides Function MakeQueryStmt(ByVal oschema As Orm.Meta.IObjectSchemaBase, ByVal filterInfo As Object, ByVal schema As QueryGenerator, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam) As String
                If schema Is Nothing Then
                    Throw New ArgumentNullException("schema")
                End If

                Dim map As MapField2Column = oschema.GetFieldColumnMap()(Template.FieldName)

                Return map._columnName & Template.OperToStmt & "'" & val._ToString & "'"
            End Function
        End Class
    End Namespace
End Namespace