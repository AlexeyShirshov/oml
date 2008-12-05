Imports Worm.Query
Imports Worm.Database
Imports Worm.Query.Database
Imports Worm.Criteria.Joins
Imports System.Collections.Generic
Imports Worm.Criteria.Core
Imports Worm.Entities.Meta

'Namespace Database.Sorting
'    Public Class DbSort
'        Inherits Worm.Sorting.Sort

'        Private _agr As AggregateBase

'        Public Sub New(ByVal agr As AggregateBase)
'            _agr = agr
'        End Sub

'        Public Sub New(ByVal agr As AggregateBase, ByVal order As Orm.SortType)
'            _agr = agr
'            Me.Order = order
'        End Sub

'        Public Sub New(ByVal q As QueryCmd)
'            MyBase.new(q)
'        End Sub

'        Public Sub New(ByVal q As QueryCmd, ByVal order As Orm.SortType)
'            MyBase.New(q)
'            Me.Order = order
'        End Sub

'        Public Overridable Sub MakeStmt(ByVal s As SQLGenerator, ByVal almgr As IPrepareTable, ByVal columnAliases As List(Of String), _
'            ByVal sb As StringBuilder, ByVal t As Type, ByVal filterInfo As Object, ByVal params As ICreateParam)
'            If _agr IsNot Nothing Then
'                Dim a As Boolean = _agr.AddAlias
'                _agr.AddAlias = False
'                sb.Append(" order by ")
'                sb.Append(_agr.MakeStmt(s, columnAliases, params, almgr, filterInfo))
'                If Order = Orm.SortType.Desc Then
'                    sb.Append(" desc")
'                End If
'                _agr.AddAlias = a
'            ElseIf Query IsNot Nothing Then
'                Dim j As New List(Of OrmJoin)
'                Dim sl As List(Of Orm.OrmProperty) = Nothing
'                Dim f As IFilter = Query.Prepare(j, s, filterInfo, t, sl)
'                sb.Append(" order by (")
'                sb.Append(DbQueryExecutor.MakeQueryStatement(filterInfo, s, Query, params, t, j, f, almgr, sl))
'                sb.Append(")")
'                If Order = Orm.SortType.Desc Then
'                    sb.Append(" desc")
'                End If
'            Else
'                s.AppendOrder(t, Me, almgr, sb, True, Nothing, Nothing)
'            End If
'        End Sub

'        Public Overrides Function ToString() As String
'            If _agr IsNot Nothing Then
'                Return _agr.ToString
'            Else
'                Return MyBase.ToString
'            End If
'        End Function
'    End Class
'End Namespace