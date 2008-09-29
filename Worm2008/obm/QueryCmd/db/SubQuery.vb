Imports Worm.Database.Criteria.Values
Imports Worm.Database
Imports Worm.Orm.Meta
Imports System.Collections.Generic
Imports Worm.Criteria.Core

Namespace Database.Criteria.Values

    Public Class SubQueryCmd
        Implements IDatabaseFilterValue, Worm.Criteria.Values.INonTemplateValue

        Private _q As Query.QueryCmd

        Public Sub New(ByVal q As Query.QueryCmd)
            _q = q
        End Sub

        Public Function _ToString() As String Implements Worm.Criteria.Values.IFilterValue._ToString
            Return _q.ToString()
        End Function

        Public Function GetParam(ByVal schema As SQLGenerator, ByVal filterInfo As Object, ByVal paramMgr As Orm.Meta.ICreateParam, ByVal almgr As AliasMgr) As String Implements IDatabaseFilterValue.GetParam
            Dim sb As New StringBuilder
            'Dim dbschema As DbSchema = CType(schema, DbSchema)
            sb.Append("(")

            Dim t As Type = If(_q.CreateType Is Nothing, _q.SelectedType, _q.CreateType)

            Dim j As New List(Of Worm.Criteria.Joins.OrmJoin)
            Dim sl As List(Of Orm.OrmProperty) = Nothing
            Dim f As IFilter = _q.Prepare(j, schema, filterInfo, t, sl)

            sb.Append(Query.Database.DbQueryExecutor.MakeQueryStatement(filterInfo, schema, _q, paramMgr, _
                 t, j, f, almgr, sl))

            sb.Append(")")

            Return sb.ToString
        End Function

        Public Function GetStaticString() As String Implements Worm.Criteria.Values.INonTemplateValue.GetStaticString
            Return _q.ToStaticString
        End Function
    End Class
End Namespace