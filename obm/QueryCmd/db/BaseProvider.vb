Imports Worm.Database
Imports System.Collections.Generic
Imports Worm.Criteria.Core
Imports Worm.Entities.Meta
Imports Worm.Entities
Imports System.Collections.ObjectModel
Imports Worm.Query.QueryCmd

Namespace Query.Database

    Partial Public Class DbQueryExecutor

        Public Class BaseProvider
            Inherits CacheItemBaseProvider

            Private _stmt As String
            Protected _params As ParamMgr
            Private _cmdType As System.Data.CommandType

            Private _oldct As EntityUnion
            Private _types As ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))
            Private _f As FromClauseDef

            Protected Sub New()
            End Sub

            'Public Sub New(ByVal mgr As OrmManager, ByVal q As QueryCmd, ByVal ct As EntityUnion, _
            '               ByVal types As ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?)), _
            '               ByVal f As FromClauseDef)
            '    _oldct = ct
            '    _types = types
            '    _f = f
            '    Reset(mgr, q)
            'End Sub

            Public Sub SetTemp(ByVal q As QueryCmd)
                q._from = _f
                q._createType = _oldct
                If _types IsNot Nothing AndAlso q._sel Is Nothing Then
                    q._sel = New SelectClauseDef(_types)
                End If
            End Sub

            Public Sub New(ByVal mgr As OrmManager, ByVal q As QueryCmd)
                _oldct = q.CreateType
                If q.SelectClause IsNot Nothing AndAlso q.SelectClause.SelectTypes IsNot Nothing Then
                    _types = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(q.SelectClause.SelectTypes)
                End If
                _f = q.FromClause

                Reset(mgr, q)
            End Sub

            Public Overloads Overrides Function GetCacheItem(ByVal ctx As TypeWrap(Of Object)) As Cache.CachedItemBase
                Return New Cache.CachedItemBase(GetMatrix(), _mgr.Cache)
            End Function

            Protected Function GetMatrix() As ReadonlyMatrix
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)

                Using cmd As System.Data.Common.DbCommand = dbm.CreateDBCommand
                    ', dbm, Query, GetType(ReturnType), _j, _f
                    MakeStatement(cmd)

                    Return ExecMatrix(cmd)
                End Using
            End Function

            Public Overrides Sub Reset(ByVal mgr As OrmManager, ByVal q As QueryCmd)
                _mgr = mgr
                _q = q
                _dic = Nothing

                Dim fromKey As String = Nothing
                'If _q.Table IsNot Nothing Then
                '    fromKey = _q.Table.RawName
                'ElseIf _q.FromClause IsNot Nothing AndAlso _q.FromClause.AnyQuery IsNot Nothing Then
                '    fromKey = _q.FromClause.AnyQuery.ToStaticString(mgr.MappingEngine)
                'Else
                '    fromKey = mgr.MappingEngine.GetEntityKey(mgr.GetFilterInfo, _q.GetSelectedType(mgr.MappingEngine))
                'End If

                _key = QueryCmd.GetStaticKey(_q, _mgr.GetStaticKey(), _mgr.Cache.CacheListBehavior, fromKey, _mgr.MappingEngine, _dic, mgr.GetContextFilter)

                If _dic Is Nothing Then
                    _dic = GetExternalDic(_key)
                    If _dic IsNot Nothing Then
                        _key = Nothing
                    End If
                End If

                If Not String.IsNullOrEmpty(_key) OrElse _dic IsNot Nothing Then
                    _id = QueryCmd.GetDynamicKey(_q)
                    _sync = _id & _mgr.GetStaticKey()
                End If

                ResetDic()
                ResetStmt()
            End Sub

            Public Sub ResetStmt()
                _stmt = Nothing
            End Sub

            Public Overridable Sub ResetDic()
                If Not String.IsNullOrEmpty(_key) AndAlso _dic Is Nothing Then
                    _dic = _mgr.GetDic(_mgr.Cache, _key)
                End If
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
                'Dim almgr As AliasMgr = AliasMgr.Create
                Dim fi As Object = _mgr.GetContextFilter
                Dim i As Integer = 0
                'Dim q As QueryCmd = _q
                'Dim sb As New StringBuilder
                'Dim inner As String = Nothing
                'Dim innerColumns As List(Of String) = Nothing
                Dim stmtGen As SQLGenerator = CType(_mgr, OrmReadOnlyDBManager).SQLGenerator
                'For Each q As QueryCmd In New StmtQueryIterator(_q)
                'Dim columnAliases As New List(Of String)
                'Dim j As List(Of Worm.Criteria.Joins.QueryJoin) = q._js '_j(i)
                'Dim f As IFilter = q._f
                'If _f.Length > i Then
                '    f = _f(i)
                'End If
                'Dim sl As List(Of SelectExpression) = _sl(i)
                'Dim sl As List(Of SelectExpression) = q._sl
                'inner = MakeQueryStatement(_mgr.MappingEngine, fi, stmtGen, q, _params, almgr)
                'innerColumns = New List(Of String)(columnAliases)
                'q = q.OuterQuery
                'i += 1
                'Next
                'Loop
                Return MakeQueryStatement(_mgr.MappingEngine, fi, stmtGen, _q, _params)
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

            Protected Overridable Function ExecMatrix(ByVal cmd As System.Data.Common.DbCommand) As ReadonlyMatrix
                Dim l As New List(Of ReadOnlyCollection(Of _IEntity))
                'Dim sl As List(Of SelectExpression) = _sl(_sl.Count - 1)
                Dim sl As List(Of SelectExpression) = _q._sl
                CType(_mgr, OrmReadOnlyDBManager).QueryMultiTypeObjects(Nothing, cmd, l, _q._types, _q._pdic, sl)
                _q.ExecCount += 1
                Return New ReadonlyMatrix(l)
            End Function

            Public Overrides Sub CopyTo(ByVal cp As CacheItemBaseProvider)
                MyBase.CopyTo(cp)
                With CType(cp, BaseProvider)
                    ._cmdType = _cmdType
                    ._stmt = _stmt
                    ._params = _params
                End With
            End Sub
        End Class

        Public Class SimpleProvider(Of T)
            Inherits BaseProvider

            'Public Sub New(ByVal mgr As OrmManager, ByVal q As QueryCmd)
            '    MyBase.New(mgr, q)
            'End Sub

            Public Sub New(ByVal bp As BaseProvider)
                bp.CopyTo(Me)
            End Sub

            Public Overrides Sub ResetDic()
                If Not String.IsNullOrEmpty(_key) AndAlso _dic Is Nothing Then
                    _dic = Cache.GetSimpleDictionary(_key)
                End If
            End Sub

            Public Overrides Function GetCacheItem(ByVal ctx As TypeWrap(Of Object)) As Cache.CachedItemBase
                Return New Cache.CachedItemBase(CType(GetSimpleValues(Of T)(), ICollection), _mgr.Cache)
            End Function
        End Class
    End Class
End Namespace