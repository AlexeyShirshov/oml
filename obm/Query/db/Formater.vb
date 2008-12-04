Imports System.Collections.Generic
Imports Worm.Orm.Meta
Imports Worm.Criteria.Joins
Imports Worm.Criteria.Core

Namespace Orm

    Public Interface ISelectExpressionFormater
        Sub Format(ByVal se As SelectExpression, ByVal sb As StringBuilder, ByVal schema As ObjectMappingEngine, ByVal t As Type, _
                   ByVal almgr As IPrepareTable, ByVal pmgr As ICreateParam, ByVal columnAliases As List(Of String), _
                   ByVal context As Object, ByVal selList As ObjectModel.ReadOnlyCollection(Of SelectExpression), _
                   ByVal defaultTable As SourceFragment)

    End Interface

End Namespace

Namespace Database
    Public Class SelectExpressionFormater
        Implements Orm.ISelectExpressionFormater

        Private _s As SQLGenerator

        Public Sub New(ByVal s As SQLGenerator)
            _s = s
        End Sub

        Public Sub Format(ByVal se As Orm.SelectExpression, ByVal sb As System.Text.StringBuilder, _
                          ByVal schema As ObjectMappingEngine, ByVal selectType As System.Type, ByVal almgr As IPrepareTable, ByVal pmgr As ICreateParam, _
                          ByVal columnAliases As System.Collections.Generic.List(Of String), _
                          ByVal context As Object, ByVal selList As System.Collections.ObjectModel.ReadOnlyCollection(Of Orm.SelectExpression), ByVal defaultTable As Orm.Meta.SourceFragment) Implements Orm.ISelectExpressionFormater.Format
            Dim s As Sorting.Sort = TryCast(se, Sorting.Sort)
            If s IsNot Nothing Then
                Select Case se.PropType
                    Case Orm.PropType.Aggregate
                        Dim a As Boolean = se.aggregate.AddAlias
                        se.Aggregate.AddAlias = False
                        sb.Append(" order by ")
                        sb.Append(se.Aggregate.MakeStmt(schema, columnAliases, pmgr, almgr, context))
                        If s.Order = Orm.SortType.Desc Then
                            sb.Append(" desc")
                        End If
                        se.Aggregate.AddAlias = a
                    Case Orm.PropType.Subquery
                        Dim j As New List(Of OrmJoin)
                        Dim sl As List(Of Orm.SelectExpression) = Nothing

                        Dim _q As Query.QueryCmd = se.Query

                        Dim c As New Query.QueryCmd.svct(_q)
                        Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
                            If _q.SelectedType Is Nothing Then
                                If String.IsNullOrEmpty(_q.SelectedEntityName) Then
                                    _q.SelectedType = _q.CreateType
                                Else
                                    _q.SelectedType = schema.GetTypeByEntityName(_q.SelectedEntityName)
                                End If
                            End If

                            If GetType(Orm.AnonymousEntity).IsAssignableFrom(_q.SelectedType) Then
                                _q.SelectedType = Nothing
                            End If

                            If _q.CreateType Is Nothing Then
                                _q.CreateType = _q.SelectedType
                            End If

                            Dim f As IFilter = se.Query.Prepare(j, schema, context, _q.SelectedType, sl)
                            sb.Append(" order by (")
                            sb.Append(Query.Database.DbQueryExecutor.MakeQueryStatement(context, _s, _q, pmgr, _q.SelectedType, j, f, almgr, sl))
                        End Using
                        sb.Append(")")
                        If s.Order = Orm.SortType.Desc Then
                            sb.Append(" desc")
                        End If
                    Case Else
                        _s.AppendOrder(selectType, s, almgr, sb, True, selList, defaultTable)
                        'Throw New NotSupportedException(se.PropType.ToString)
                End Select
            Else

            End If

        End Sub
    End Class
End Namespace
