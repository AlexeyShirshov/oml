﻿Imports Worm.Database
Imports System.Collections.Generic
Imports Worm.Criteria.Core
Imports Worm.Entities.Meta
Imports Worm.Entities
Imports System.Collections.ObjectModel
Imports Worm.Query.QueryCmd
Imports Worm.Expressions2

Namespace Query.Database

    Partial Public Class DbQueryExecutor

        Public Class BaseProvider
            Inherits CacheItemBaseProvider

            Protected _params As ParamMgr
            Protected _almgr As IPrepareTable
            Private _cmdType As System.Data.CommandType

            Private _oldct As Dictionary(Of EntityUnion, EntityUnion)
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
                q._createTypes = _oldct
                If _types IsNot Nothing AndAlso q._sel Is Nothing Then
                    q._sel = New SelectClauseDef(_types)
                End If
            End Sub

            Public Sub New(ByVal mgr As OrmManager, ByVal q As QueryCmd)
                _oldct = New Dictionary(Of EntityUnion, EntityUnion)(q._createTypes)
                If q.SelectClause IsNot Nothing AndAlso q.SelectClause.SelectTypes IsNot Nothing Then
                    _types = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(q.SelectClause.SelectTypes)
                End If
                _f = q.FromClause

                Reset(mgr, q)
            End Sub

            Public Overloads Overrides Function GetCacheItem(ByVal ctx As TypeWrap(Of Object)) As Cache.CachedItemBase
                Dim m As ReadonlyMatrix = GetMatrix()
                'Dim args As QueryCmd.ModifyResultArgs = _q.RaiseModifyResult(_mgr, m)
                'Return New Cache.CachedItemBase(args.Matrix, _mgr.Cache) With {.CustomInfo = args.CustomInfo}
                Return New Cache.CachedItemBase(m, _mgr)
            End Function

            Protected Function GetMatrix() As ReadonlyMatrix
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)

                Using cmd As System.Data.Common.DbCommand = dbm.CreateDBCommand
                    ', dbm, Query, GetType(ReturnType), _j, _f
                    MakeStatement(cmd)

                    Return ExecMatrix(cmd)
                End Using
            End Function

            'Public Overrides Sub Reset(ByVal mgr As OrmManager, ByVal q As QueryCmd)
            '    _mgr = mgr
            '    _q = q
            '    _dic = Nothing

            '    Dim fromKey As String = Nothing
            '    'If _q.Table IsNot Nothing Then
            '    '    fromKey = _q.Table.RawName
            '    'ElseIf _q.FromClause IsNot Nothing AndAlso _q.FromClause.AnyQuery IsNot Nothing Then
            '    '    fromKey = _q.FromClause.AnyQuery.ToStaticString(mgr.MappingEngine)
            '    'Else
            '    '    fromKey = mgr.MappingEngine.GetEntityKey(mgr.GetFilterInfo, _q.GetSelectedType(mgr.MappingEngine))
            '    'End If

            '    _key = QueryCmd.GetStaticKey(_q, _mgr.GetStaticKey(), _mgr.Cache.CacheListBehavior, fromKey, _mgr.MappingEngine, _dic, mgr.GetContextInfo)

            '    If _dic Is Nothing Then
            '        _dic = GetExternalDic(_key)
            '        If _dic IsNot Nothing Then
            '            _key = Nothing
            '        End If
            '    End If

            '    If Not String.IsNullOrEmpty(_key) OrElse _dic IsNot Nothing Then
            '        _id = QueryCmd.GetDynamicKey(_q)
            '        _sync = _id & _mgr.GetStaticKey()
            '    End If

            '    ResetDic()
            '    ResetStmt()
            'End Sub

            'Public Sub ResetStmt()
            '    _stmt = Nothing
            'End Sub

            'Public Overridable Sub ResetDic()
            '    If Not String.IsNullOrEmpty(_key) AndAlso _dic Is Nothing Then
            '        _dic = _mgr.GetDic(_mgr.Cache, _key)
            '    End If
            'End Sub

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
                Dim fi = _mgr.ContextInfo
                Dim i As Integer = 0
                Dim stmtGen As DbGenerator = CType(_mgr, OrmReadOnlyDBManager).SQLGenerator
                _almgr = AliasMgr.Create
                If _q._optimizeIn IsNot Nothing AndAlso _q._f IsNot Nothing Then
                    _q._f = _q._f.RemoveFilter(_q._optimizeIn)
                End If
                If _q._optimizeOr IsNot Nothing OrElse _q._optimizeIn IsNot Nothing Then
                    If _q._f Is Nothing Then
                        _q._f = New CustomFilter("'optin'", Criteria.FilterOperation.Equal, New Criteria.Values.LiteralValue("'optin'"))
                    Else
                        _q._f = Ctor.Filter(_q._f).and(New CustomFilter("'optin'", Criteria.FilterOperation.Equal, New Criteria.Values.LiteralValue("'optin'"))).Filter
                    End If
                End If

                Return MakeQueryStatement(_mgr.MappingEngine, fi, stmtGen, _q, _params, _almgr)
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
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)
                Dim l As New List(Of ReadOnlyCollection(Of _IEntity))
                Dim sl As List(Of SelectExpression) = _q._sl
                Dim batch As Pair(Of List(Of Object), FieldReference) = _q.GetBatchStruct
                If batch IsNot Nothing Then
                    Dim pcnt As Integer = _params.Params.Count
                    Dim nidx As Integer = pcnt
                    For Each cmd_str As Pair(Of String, Integer) In dbm.GetFilters(batch.First, batch.Second, _almgr, _params, False)
                        Using newCmd As System.Data.Common.DbCommand = dbm.CreateDBCommand
                            With newCmd
                                .CommandText = cmd.CommandText.Replace("'optin' = 'optin'", cmd_str.First)
                                _params.AppendParams(.Parameters, 0, pcnt)
                                _params.AppendParams(.Parameters, nidx, cmd_str.Second - nidx)
                                nidx = cmd_str.Second
                            End With
                            dbm.QueryMultiTypeObjects(_q.CreateTypes, newCmd, l, _q._types, sl)
                        End Using
                    Next
                Else
                    Dim oo As List(Of Criteria.PredicateLink) = _q.GetBatchOrStruct
                    If oo IsNot Nothing Then
                        Dim pcnt As Integer = _params.Params.Count
                        Dim nidx As Integer = pcnt
                        For Each cmd_str As Pair(Of String, Integer) In dbm.GetFilters(oo, _almgr, _params)
                            Using newCmd As System.Data.Common.DbCommand = dbm.CreateDBCommand
                                With newCmd
                                    .CommandText = cmd.CommandText.Replace("'optin' = 'optin'", cmd_str.First)
                                    _params.AppendParams(.Parameters, 0, pcnt)
                                    _params.AppendParams(.Parameters, nidx, cmd_str.Second - nidx)
                                    nidx = cmd_str.Second
                                End With
                                dbm.QueryMultiTypeObjects(_q.CreateTypes, newCmd, l, _q._types, sl)
                            End Using
                        Next
                    Else
                        dbm.QueryMultiTypeObjects(_q.CreateTypes, cmd, l, _q._types, sl)
                    End If
                End If
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
                Dim col As ICollection = CType(GetSimpleValues(Of T)(), ICollection)
                'Dim args As QueryCmd.ModifyResultArgs = _q.RaiseModifyResult(_mgr, col)
                'Return New Cache.CachedItemBase(args.SimpleList, _mgr.Cache) With {.CustomInfo = args.CustomInfo}
                Return New Cache.CachedItemBase(col, _mgr)
            End Function
        End Class
    End Class
End Namespace