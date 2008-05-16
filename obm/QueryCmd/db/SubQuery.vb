Imports Worm.Database.Criteria.Values
Imports Worm.Database
Imports Worm.Orm.Meta
Imports System.Collections.Generic
Imports Worm.Criteria.Core

Namespace Query.Database.Filters

    Public Class SubQuery
        Implements IDatabaseFilterValue, Worm.Criteria.Values.INonTemplateValue

        Private _q As QueryCmdBase

        Public Sub New(ByVal query As QueryCmdBase)
            _q = query
        End Sub

        Public Function _ToString() As String Implements Criteria.Values.IFilterValue._ToString
            Return _q.GetDynamicKey(
        End Function

        Public Function GetStaticString() As String Implements Criteria.Values.INonTemplateValue.GetStaticString
            Return _q.GetStaticKey(
        End Function

        Public Function GetParam(ByVal schema As SQLGenerator, ByVal paramMgr As Orm.Meta.ICreateParam, ByVal almgr As AliasMgr) As String Implements IDatabaseFilterValue.GetParam
            Dim sb As New StringBuilder
            'Dim dbschema As DbSchema = CType(schema, DbSchema)
            sb.Append("(")

            FormStmt(schema, paramMgr, almgr, sb)

            sb.Append(")")

            Return sb.ToString
        End Function

        Protected Overridable Sub FormStmt(ByVal dbschema As SQLGenerator, ByVal paramMgr As ICreateParam, ByVal almgr As AliasMgr, ByVal sb As StringBuilder)
            Dim j As New List(Of Worm.Criteria.Joins.OrmJoin)
            'If query.Joins IsNot Nothing Then
            '    j.AddRange(query.Joins)
            'End If

            Dim f As IFilter = _q.Prepare(j, mgr, _q.SelectedType)

            sb.Append(DbQueryExecutor.MakeQueryStatement(mgr, _q, paramMgr, _q.SelectedType, j, f))
        End Sub
    End Class
End Namespace