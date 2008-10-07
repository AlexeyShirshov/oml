Imports Worm.Database.Criteria.Values
Imports Worm.Database
Imports Worm.Orm.Meta
Imports System.Collections.Generic
Imports Worm.Criteria.Core

Namespace Database.Criteria.Values

    Public Class SubQueryCmd
        Implements Worm.Criteria.Values.IFilterValue, Worm.Criteria.Values.INonTemplateValue,  _
        Cache.IQueryDependentTypes

        Private _q As Query.QueryCmd
        Private _stmtGen As SQLGenerator

        Public Sub New(ByVal q As Query.QueryCmd)
            MyClass.New(Nothing, q)
        End Sub

        Public Sub New(ByVal stmtGen As SQLGenerator, ByVal q As Query.QueryCmd)
            _q = q
            _stmtGen = stmtGen
        End Sub

        Public Function _ToString() As String Implements Worm.Criteria.Values.IFilterValue._ToString
            Return _q.ToString()
        End Function

        Public Function GetParam(ByVal schema As ObjectMappingEngine, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, ByVal prepare As Worm.Criteria.Values.PrepareValueDelegate, _
                          ByVal aliases As IList(Of String), ByVal filterInfo As Object) As String Implements Worm.Criteria.Values.IFilterValue.GetParam
            Dim sb As New StringBuilder
            'Dim dbschema As DbSchema = CType(schema, DbSchema)
            sb.Append("(")

            Dim t As Type = If(_q.CreateType Is Nothing, _q.SelectedType, _q.CreateType)

            Dim j As New List(Of Worm.Criteria.Joins.OrmJoin)
            Dim sl As List(Of Orm.SelectExpression) = Nothing
            Dim f As IFilter = _q.Prepare(j, schema, filterInfo, t, sl)

            If _stmtGen Is Nothing Then
                _stmtGen = TryCast(schema, SQLGenerator)
            End If

            sb.Append(Query.Database.DbQueryExecutor.MakeQueryStatement(filterInfo, _stmtGen, _q, paramMgr, _
                 t, j, f, almgr, sl))

            sb.Append(")")

            Return sb.ToString
        End Function

        Public Function GetStaticString() As String Implements Worm.Criteria.Values.INonTemplateValue.GetStaticString
            Return _q.ToStaticString
        End Function

        Public Function [Get](ByVal mpe As ObjectMappingEngine) As Cache.IDependentTypes Implements Cache.IQueryDependentTypes.Get
            Return _q.Get(mpe)
        End Function
    End Class
End Namespace