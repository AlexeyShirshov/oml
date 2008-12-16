Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Joins
Imports Worm.Criteria.Core
Imports Worm.Sorting
Imports System.Collections.ObjectModel

Namespace Entities

    Public Interface ISelectExpressionFormater
        Sub Format(ByVal se As SelectExpression, ByVal sb As StringBuilder, ByVal schema As ObjectMappingEngine, _
                   ByVal almgr As IPrepareTable, ByVal pmgr As ICreateParam, ByVal columnAliases As List(Of String), _
                   ByVal context As Object, ByVal selList As ObjectModel.ReadOnlyCollection(Of SelectExpression), _
                   ByVal defaultTable As SourceFragment, ByVal inSelect As Boolean)

    End Interface

End Namespace

Namespace Database
    Public Class SelectExpressionFormater
        Implements Entities.ISelectExpressionFormater

        Private _s As SQLGenerator

        Public Sub New(ByVal s As SQLGenerator)
            _s = s
        End Sub

        Public Sub Format(ByVal se As Entities.SelectExpression, ByVal sb As System.Text.StringBuilder, _
                          ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pmgr As ICreateParam, _
                          ByVal columnAliases As System.Collections.Generic.List(Of String), _
                          ByVal context As Object, ByVal selList As ReadOnlyCollection(Of Entities.SelectExpression), _
                          ByVal defaultTable As Entities.Meta.SourceFragment, ByVal inSelect As Boolean) Implements Entities.ISelectExpressionFormater.Format
            Dim s As Sorting.Sort = TryCast(se, Sorting.Sort)
            If s IsNot Nothing Then
                Select Case se.PropType
                    Case Entities.PropType.Aggregate
                        Dim a As Boolean = se.Aggregate.AddAlias
                        se.Aggregate.AddAlias = False
                        sb.Append(" order by ")
                        sb.Append(se.Aggregate.MakeStmt(schema, _s, columnAliases, pmgr, almgr, context, False))
                        If s.Order = SortType.Desc Then
                            sb.Append(" desc")
                        End If
                        se.Aggregate.AddAlias = a
                    Case Entities.PropType.Subquery
                        Dim j As New List(Of QueryJoin)
                        Dim sl As List(Of Entities.SelectExpression) = Nothing

                        Dim _q As Query.QueryCmd = se.Query

                        Dim c As New Query.QueryCmd.svct(_q)
                        Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
                            'If _q.SelectedType Is Nothing Then
                            '    If String.IsNullOrEmpty(_q.SelectedEntityName) Then
                            '        _q.SelectedType = _q.CreateType
                            '    Else
                            '        _q.SelectedType = schema.GetTypeByEntityName(_q.SelectedEntityName)
                            '    End If
                            'End If

                            'If GetType(Entities.AnonymousEntity).IsAssignableFrom(_q.SelectedType) Then
                            '    _q.SelectedType = Nothing
                            'End If

                            'If _q.CreateType Is Nothing AndAlso _q.SelectedType IsNot Nothing Then
                            '    _q.Into(_q.SelectedType)
                            'End If

                            Dim f As IFilter = se.Query.Prepare(j, schema, context, sl, _s)
                            sb.Append(" order by (")
                            sb.Append(Query.Database.DbQueryExecutor.MakeQueryStatement(schema, context, _s, _q, pmgr, j, f, almgr, sl))
                        End Using
                        sb.Append(")")
                        If s.Order = SortType.Desc Then
                            sb.Append(" desc")
                        End If
                    Case Else
                        _s.AppendOrder(schema, s, almgr, sb, True, selList, defaultTable)
                        'Throw New NotSupportedException(se.PropType.ToString)
                End Select
            Else
                Select Case se.PropType
                    Case Entities.PropType.Aggregate
                        se.Aggregate.MakeStmt(schema, _s, columnAliases, pmgr, almgr, context, inSelect)
                    Case Entities.PropType.TableColumn
                        If Not String.IsNullOrEmpty(se.Table.Name) Then
                            sb.Append(se.Table.UniqueName(se.ObjectSource)).Append(schema.Delimiter)
                        End If
                        sb.Append(se.Column).Append(", ")
                    Case Else
                        Throw New NotImplementedException
                End Select
            End If

        End Sub
    End Class
End Namespace
