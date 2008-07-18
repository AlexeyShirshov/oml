﻿Imports Worm.Query
Imports Worm.Database
Imports Worm.Query.Database
Imports Worm.Criteria.Joins
Imports System.Collections.Generic
Imports Worm.Criteria.Core
Imports Worm.Orm.Meta

Namespace Sorting
    Public Class SortAdv
        Inherits Sort

        Private _agr As AggregateBase
        Private _q As QueryCmdBase

        Public Sub New(ByVal agr As AggregateBase)
            _agr = agr
        End Sub

        Public Sub New(ByVal agr As AggregateBase, ByVal order As Orm.SortType)
            _agr = agr
            Me.Order = order
        End Sub

        Public Sub New(ByVal q As QueryCmdBase)
            _q = q
        End Sub

        Public Sub New(ByVal q As QueryCmdBase, ByVal order As Orm.SortType)
            _q = q
            Me.Order = order
        End Sub

        Public Overridable Sub MakeStmt(ByVal s As SQLGenerator, ByVal almgr As AliasMgr, ByVal columnAliases As List(Of String), _
            ByVal sb As StringBuilder, ByVal t As Type, ByVal filterInfo As Object, ByVal params As ICreateParam)
            If _agr IsNot Nothing Then
                Dim a As Boolean = _agr.AddAlias
                _agr.AddAlias = False
                sb.Append(" order by ")
                sb.Append(_agr.MakeStmt(s, columnAliases, params, almgr))
                If Order = Orm.SortType.Desc Then
                    sb.Append(" desc")
                End If
                _agr.AddAlias = a
            ElseIf _q IsNot Nothing Then
                Dim j As New List(Of OrmJoin)
                Dim f As IFilter = _q.Prepare(j, s, filterInfo, t)
                sb.Append(" order by (")
                sb.Append(DbQueryExecutor.MakeQueryStatement(filterInfo, s, _q, params, t, j, f, almgr))
                sb.Append(")")
                If Order = Orm.SortType.Desc Then
                    sb.Append(" desc")
                End If
            Else
                s.AppendOrder(t, Me, almgr, sb)
            End If
        End Sub
    End Class
End Namespace