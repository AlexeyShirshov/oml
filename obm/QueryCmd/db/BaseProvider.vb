Imports Worm.Database
Imports System.Collections.Generic
Imports Worm.Criteria.Core
Imports Worm.Entities.Meta

Namespace Query.Database

    Partial Public Class DbQueryExecutor

        Public Class BaseProvider
            Inherits CacheItemBaseProvider

            Private _stmt As String
            Protected _params As ParamMgr
            Private _cmdType As System.Data.CommandType

            Public Overloads Overrides Function GetCacheItem(ByVal withLoad As Boolean) As Cache.CachedItemBase
                Throw New NotImplementedException
            End Function

            Public Overrides Sub Reset(ByVal mgr As OrmManager, ByVal j As System.Collections.Generic.List(Of System.Collections.Generic.List(Of Criteria.Joins.QueryJoin)), ByVal f() As Criteria.Core.IFilter, ByVal sl As System.Collections.Generic.List(Of System.Collections.Generic.List(Of Entities.SelectExpression)), ByVal q As QueryCmd)
                Throw New NotImplementedException
            End Sub

            Public Sub ResetStmt()
                _stmt = Nothing
            End Sub

            Protected Overridable Sub MakeStatement(ByVal cmd As System.Data.Common.DbCommand)
                'Dim mgr As OrmReadOnlyDBManager = _mgr
                'Dim t As Type = GetType(ReturnType)
                'Dim joins As List(Of Worm.Criteria.Joins.OrmJoin) = _j
                'Dim f As IFilter = _f

                If String.IsNullOrEmpty(_stmt) Then
                    _cmdType = System.Data.CommandType.Text

                    _params = New ParamMgr(CType(_mgr, OrmReadOnlyDBManager).SQLGenerator, "p")
                    _stmt = _MakeStatement()
                End If

                cmd.CommandText = _stmt
                cmd.CommandType = _cmdType
                _params.AppendParams(cmd.Parameters)
            End Sub

            Protected Overridable Function _MakeStatement() As String
                Dim almgr As AliasMgr = AliasMgr.Create
                Dim fi As Object = _mgr.GetFilterInfo
                Dim i As Integer = 0
                'Dim q As QueryCmd = _q
                'Dim sb As New StringBuilder
                Dim inner As String = Nothing
                Dim innerColumns As List(Of String) = Nothing
                Dim stmtGen As SQLGenerator = CType(_mgr, OrmReadOnlyDBManager).SQLGenerator
                For Each q As QueryCmd In New QueryIterator(_q)
                    Dim columnAliases As New List(Of String)
                    Dim j As List(Of Worm.Criteria.Joins.QueryJoin) = _j(i)
                    Dim f As IFilter = Nothing
                    If _f.Length > i Then
                        f = _f(i)
                    End If
                    inner = MakeQueryStatement(_mgr.MappingEngine, fi, stmtGen, q, _params, j, f, almgr, columnAliases, inner, innerColumns, i, _sl(i))
                    innerColumns = New List(Of String)(columnAliases)
                    'q = q.OuterQuery
                    i += 1
                Next
                'Loop
                Return inner
            End Function

            Public Function GetSimpleValues(Of T)() As IList(Of T)
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)

                Using cmd As System.Data.Common.DbCommand = dbm.CreateDBCommand
                    ', dbm, Query, GetType(ReturnType), _j, _f
                    MakeStatement(cmd)

                    Return ExecStmt(Of T)(cmd)
                End Using
            End Function

            Protected Overridable Function ExecStmt(Of T)(ByVal cmd As System.Data.Common.DbCommand) As IList(Of T)
                Dim l As IList(Of T) = CType(_mgr, OrmReadOnlyDBManager).GetSimpleValues(Of T)(cmd)
                _q.ExecCount += 1
                Return l
            End Function

            Public Function GetSimpleValues(ByVal t As Type) As IList
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)

                Using cmd As System.Data.Common.DbCommand = dbm.CreateDBCommand
                    ', dbm, Query, GetType(ReturnType), _j, _f
                    MakeStatement(cmd)

                    Return ExecStmt(cmd, t)
                End Using
            End Function

            Protected Overridable Function ExecStmt(ByVal cmd As System.Data.Common.DbCommand, ByVal t As Type) As IList
                Dim l As IList = CType(_mgr, OrmReadOnlyDBManager).GetSimpleValues(cmd, t)
                _q.ExecCount += 1
                Return l
            End Function
        End Class
    End Class
End Namespace