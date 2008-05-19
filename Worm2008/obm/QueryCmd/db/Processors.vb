Imports Worm.Database
Imports System.Collections.Generic
Imports Worm.Orm
Imports Worm.Orm.Meta
Imports Worm.Database.Criteria.Joins
Imports Worm.Criteria.Core

Namespace Query.Database

    Partial Public Class DbQueryExecutor

        Class Processor(Of ReturnType As {New, Orm.OrmBase})
            Inherits OrmManagerBase.CustDelegate(Of ReturnType)
            Implements OrmManagerBase.ICacheValidator

            Private _stmt As String
            Private _params As ParamMgr
            Private _cmdType As System.Data.CommandType

            Private _mgr As OrmReadOnlyDBManager
            Private _j As List(Of Worm.Criteria.Joins.OrmJoin)
            Private _f As IFilter
            Private _q As QueryCmdBase
            Private _key As String
            Private _id As String
            Private _sync As String
            Private _dic As IDictionary

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal j As List(Of Worm.Criteria.Joins.OrmJoin), _
                ByVal f As IFilter, ByVal q As QueryCmdBase)
                _mgr = mgr
                _j = j
                _f = f
                _q = q

                Reset()
            End Sub

            Public Overrides Sub CreateDepends()

            End Sub

            Public Overrides ReadOnly Property Filter() As Criteria.Core.IFilter
                Get
                    Return _f
                End Get
            End Property

            Public Overloads Overrides Function GetCacheItem(ByVal withLoad As Boolean) As OrmManagerBase.CachedItem
                Return GetCacheItem(GetValues(withLoad))
            End Function

            Public Overloads Overrides Function GetCacheItem(ByVal col As ReadOnlyList(Of ReturnType)) As OrmManagerBase.CachedItem
                Dim sortex As IOrmSorting2 = TryCast(_mgr.ObjectSchema.GetObjectSchema(GetType(ReturnType)), IOrmSorting2)
                Dim s As Date = Nothing
                If sortex IsNot Nothing Then
                    Dim ts As TimeSpan = sortex.SortExpiration(_q.Sort)
                    If ts <> TimeSpan.MaxValue AndAlso ts <> TimeSpan.MinValue Then
                        s = Now.Add(ts)
                    End If
                End If
                Return New OrmManagerBase.CachedItem(_q.Sort, s, _f, col, _mgr)
            End Function

            Public Overrides Function GetValues(ByVal withLoad As Boolean) As ReadOnlyList(Of ReturnType)
                Dim r As ReadOnlyList(Of ReturnType)
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)

                Using cmd As System.Data.Common.DbCommand = dbm.DbSchema.CreateDBCommand
                    ', dbm, Query, GetType(ReturnType), _j, _f
                    MakeStatement(cmd)

                    r = ExecStmt(cmd)
                End Using

                If Query.Sort IsNot Nothing AndAlso Query.Sort.IsExternal Then
                    r = dbm.DbSchema.ExternalSort(Of ReturnType)(dbm, Query.Sort, r)
                End If

                Return r
            End Function

            'ByVal cmd As System.Data.Common.DbCommand, _
            '   ByVal mgr As OrmReadOnlyDBManager, ByVal query As QueryCmdBase, ByVal t As Type, _
            '    ByVal joins As List(Of Worm.Criteria.Joins.OrmJoin), ByVal f As IFilter

            Protected Overridable Sub MakeStatement(ByVal cmd As System.Data.Common.DbCommand)
                Dim mgr As OrmReadOnlyDBManager = _mgr
                Dim t As Type = GetType(ReturnType)
                Dim joins As List(Of Worm.Criteria.Joins.OrmJoin) = _j
                Dim f As IFilter = _f

                If String.IsNullOrEmpty(_stmt) Then
                    _cmdType = Data.CommandType.Text

                    _params = New ParamMgr(mgr.DbSchema, "p")
                    _stmt = MakeQueryStatement(mgr.GetFilterInfo, mgr.DbSchema, Query, _params, t, joins, f)
                End If

                cmd.CommandText = _stmt
                cmd.CommandType = _cmdType
                _params.AppendParams(cmd.Parameters)
            End Sub

            Protected Overridable Function ExecStmt(ByVal cmd As System.Data.Common.DbCommand) As ReadOnlyList(Of ReturnType)
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)
                Return New ReadOnlyList(Of ReturnType)(dbm.LoadMultipleObjects(Of ReturnType)( _
                        cmd, Query.WithLoad, Nothing, GetFields(dbm.DbSchema, GetType(ReturnType), Query.SelectList)))
            End Function

            Public Overrides ReadOnly Property Sort() As Sorting.Sort
                Get
                    Return _q.Sort
                End Get
            End Property

            Protected ReadOnly Property Query() As QueryCmdBase
                Get
                    Return _q
                End Get
            End Property

            Public ReadOnly Property Key() As String
                Get
                    Return _key
                End Get
            End Property

            Public ReadOnly Property Id() As String
                Get
                    Return _id
                End Get
            End Property

            Public ReadOnly Property Sync() As String
                Get
                    Return _sync
                End Get
            End Property

            Public ReadOnly Property Dic() As IDictionary
                Get
                    Return _dic
                End Get
            End Property

            Public Sub ResetStmt()
                _stmt = Nothing
            End Sub

            Public Sub Reset()
                _key = Query.GetStaticKey(_mgr.GetStaticKey(), _j, _f)
                _dic = _mgr.GetDic(_mgr.Cache, _key)
                _id = Query.GetDynamicKey(_j, _f)
                _sync = _id & _mgr.GetStaticKey()

                ResetStmt()
            End Sub

            Public Overridable Function Validate() As Boolean Implements OrmManagerBase.ICacheValidator.Validate

            End Function

            Public Overridable Function Validate(ByVal ce As OrmManagerBase.CachedItem) As Boolean Implements OrmManagerBase.ICacheValidator.Validate

            End Function
        End Class

        Class M2MProcessor(Of ReturnType As {New, Orm.OrmBase})
            Inherits Processor(Of ReturnType)

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal j As List(Of Worm.Criteria.Joins.OrmJoin), _
                ByVal f As IFilter, ByVal q As QueryCmdBase)
                MyBase.New(mgr, j, f, q)
            End Sub

            Protected Overrides Function ExecStmt(ByVal cmd As System.Data.Common.DbCommand) As ReadOnlyList(Of ReturnType)
                Throw New NotImplementedException
            End Function

            Protected Overrides Sub MakeStatement(ByVal cmd As System.Data.Common.DbCommand)
                Throw New NotImplementedException
            End Sub
        End Class
    End Class

End Namespace
