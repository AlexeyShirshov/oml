Imports Worm.Database.Criteria.Values
Imports Worm.Database
Imports Worm.Orm.Meta
Imports System.Collections.Generic
Imports Worm.Criteria.Core

Namespace Query.Database.Filters

    Public Class SubQuery
        Implements IDatabaseFilterValue, Worm.Criteria.Values.INonTemplateValue

        Private _q As QueryCmdBase
        Private _j As List(Of Worm.Criteria.Joins.OrmJoin)
        Private _f As IFilter

        Public Sub New(ByVal query As QueryCmdBase)
            _q = query
        End Sub

        Public Function _ToString() As String Implements Criteria.Values.IFilterValue._ToString
            Prepare(Nothing, Nothing)
            Return _q.GetDynamicKey(_j, _f)
        End Function

        Public Function GetStaticString() As String Implements Criteria.Values.INonTemplateValue.GetStaticString
            Prepare(Nothing, Nothing)
            Return _q.GetStaticKey(String.Empty, _j, _f)
        End Function

        Public Function GetParam(ByVal schema As SQLGenerator, ByVal filterInfo As Object, ByVal paramMgr As Orm.Meta.ICreateParam, ByVal almgr As AliasMgr) As String Implements IDatabaseFilterValue.GetParam
            Dim sb As New StringBuilder
            'Dim dbschema As DbSchema = CType(schema, DbSchema)
            sb.Append("(")

            FormStmt(schema, filterInfo, paramMgr, almgr, sb)

            sb.Append(")")

            Return sb.ToString
        End Function

        Private Sub Prepare(ByVal dbschema As SQLGenerator, ByVal filterInfo As Object)
            If _j Is Nothing Then
                Dim j As New List(Of Worm.Criteria.Joins.OrmJoin)

                _f = _q.Prepare(j, dbschema, filterInfo, _q.SelectedType)

                _j = j
            End If
        End Sub

        Protected Overridable Sub FormStmt(ByVal dbschema As SQLGenerator, ByVal filterInfo As Object, ByVal paramMgr As ICreateParam, ByVal almgr As AliasMgr, ByVal sb As StringBuilder)
            Prepare(dbschema, filterInfo)

            sb.Append(DbQueryExecutor.MakeQueryStatement(filterInfo, dbschema, _q, paramMgr, _q.SelectedType, _j, _f))
        End Sub
    End Class
End Namespace