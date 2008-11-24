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
            Return _q._ToString()
        End Function

        Public Function GetParam(ByVal schema As ObjectMappingEngine, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, ByVal prepare As Worm.Criteria.Values.PrepareValueDelegate, _
                          ByVal aliases As IList(Of String), ByVal filterInfo As Object) As String Implements Worm.Criteria.Values.IFilterValue.GetParam
            Dim sb As New StringBuilder
            'Dim dbschema As DbSchema = CType(schema, DbSchema)
            sb.Append("(")

            Dim c As New Query.QueryCmd.svct(_q)
            Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
                If _q.SelectedType Is Nothing Then
                    If String.IsNullOrEmpty(_q.EntityName) Then
                        _q.SelectedType = _q.CreateType
                    Else
                        _q.SelectedType = schema.GetTypeByEntityName(_q.EntityName)
                    End If
                End If

                If GetType(Orm.AnonymousEntity).IsAssignableFrom(_q.SelectedType) Then
                    _q.SelectedType = Nothing
                End If

                If _q.CreateType Is Nothing Then
                    _q.CreateType = _q.SelectedType
                End If

                Dim j As New List(Of Worm.Criteria.Joins.OrmJoin)
                Dim sl As List(Of Orm.SelectExpression) = Nothing
                Dim f As IFilter = _q.Prepare(j, schema, filterInfo, _q.SelectedType, sl)

                If _stmtGen Is Nothing Then
                    _stmtGen = TryCast(schema, SQLGenerator)
                End If

                sb.Append(Query.Database.DbQueryExecutor.MakeQueryStatement(filterInfo, _stmtGen, _q, paramMgr, _
                     _q.SelectedType, j, f, almgr, sl))
            End Using

            sb.Append(")")

            Return sb.ToString
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements Worm.Criteria.Values.INonTemplateValue.GetStaticString
            Return _q.ToStaticString(mpe)
        End Function

        Public Function [Get](ByVal mpe As ObjectMappingEngine) As Cache.IDependentTypes Implements Cache.IQueryDependentTypes.Get
            Dim qp As Cache.IDependentTypes = CType(_q, Cache.IQueryDependentTypes).Get(mpe)
            If Cache.IsEmpty(qp) Then
                Dim dt As New Cache.DependentTypes
                dt.AddBoth(_q.SelectedType)
                qp = dt
            End If
            Return qp
        End Function
    End Class
End Namespace