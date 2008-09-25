Imports Worm.Database
Imports System.Collections.Generic
Imports Worm.Orm
Imports Worm.Orm.Meta
Imports Worm.Database.Criteria.Joins
Imports Worm.Criteria.Core
Imports Worm.OrmManagerBase

Namespace Query.Database

    Partial Public Class DbQueryExecutor

        Class ProcessorBase(Of ReturnType As {_IEntity})
            Implements ICustDelegateBase(Of ReturnType)

            Private _created As Boolean
            Private _renew As Boolean

            Protected Sub New()

            End Sub

            Private _stmt As String
            Protected _params As ParamMgr
            Private _cmdType As System.Data.CommandType

            Protected _mgr As OrmReadOnlyDBManager
            Protected _j As List(Of List(Of Worm.Criteria.Joins.OrmJoin))
            Protected _f() As IFilter
            Protected _q As QueryCmd
            Private _key As String
            Private _id As String
            Private _sync As String
            Private _dic As IDictionary

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.OrmJoin)), _
                ByVal f() As IFilter, ByVal q As QueryCmd)
                _mgr = mgr
                _q = q

                Reset(j, f, q.SelectedType)
            End Sub

            Protected Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.OrmJoin)), _
                ByVal f() As IFilter, ByVal q As QueryCmd, ByVal t As Type)
                _mgr = mgr
                _q = q

                Reset(j, f, t)
            End Sub

            Public Sub ResetStmt()
                _stmt = Nothing
            End Sub

            Public Sub Reset(ByVal j As List(Of List(Of Worm.Criteria.Joins.OrmJoin)), ByVal f() As IFilter, ByVal t As Type)
                _j = j
                _f = f

                _key = _q.GetStaticKey(_mgr.GetStaticKey(), _j, _f, t, _mgr.Cache.CacheListBehavior)
                _id = _q.GetDynamicKey(_j, _f)
                _sync = _id & _mgr.GetStaticKey()

                ResetStmt()
                ResetDic()
            End Sub

            Public Sub ResetDic()
                If Not String.IsNullOrEmpty(_key) Then
                    _dic = _mgr.GetDic(_mgr.Cache, _key)
                Else
                    _dic = Nothing
                End If
            End Sub

            Public Function GetEntities() As ReadOnlyObjectList(Of ReturnType)
                Dim r As ReadOnlyObjectList(Of ReturnType)
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)

                Using cmd As System.Data.Common.DbCommand = dbm.DbSchema.CreateDBCommand
                    ', dbm, Query, GetType(ReturnType), _j, _f
                    MakeStatement(cmd)

                    r = ExecStmt(cmd)
                End Using

                If _q.propSort IsNot Nothing AndAlso _q.propSort.IsExternal Then
                    r = CType(dbm.DbSchema.ExternalSort(Of ReturnType)(dbm, _q.propSort, r), ReadOnlyObjectList(Of ReturnType))
                End If

                Return r
            End Function

            Protected Overridable Sub MakeStatement(ByVal cmd As System.Data.Common.DbCommand)
                'Dim mgr As OrmReadOnlyDBManager = _mgr
                'Dim t As Type = GetType(ReturnType)
                'Dim joins As List(Of Worm.Criteria.Joins.OrmJoin) = _j
                'Dim f As IFilter = _f

                If String.IsNullOrEmpty(_stmt) Then
                    _cmdType = System.Data.CommandType.Text

                    _params = New ParamMgr(_mgr.DbSchema, "p")
                    _stmt = _MakeStatement()
                End If

                cmd.CommandText = _stmt
                cmd.CommandType = _cmdType
                _params.AppendParams(cmd.Parameters)
            End Sub

            Protected Overridable Function _MakeStatement() As String
                Dim almgr As AliasMgr = AliasMgr.Create
                Dim fi As Object = _mgr.GetFilterInfo
                Dim t As Type = _q.SelectedType
                Dim i As Integer = 0
                'Dim q As QueryCmd = _q
                'Dim sb As New StringBuilder
                Dim inner As String = Nothing
                Dim innerColumns As List(Of String) = Nothing
                'Do While q IsNot Nothing
                For Each q As QueryCmd In New QueryIterator(_q)
                    Dim columnAliases As New List(Of String)
                    Dim j As List(Of Worm.Criteria.Joins.OrmJoin) = _j(i)
                    Dim f As IFilter = _f(i)
                    inner = MakeQueryStatement(fi, _mgr.DbSchema, q, _params, t, j, f, almgr, columnAliases, inner, innerColumns, i, WithLoad)
                    innerColumns = New List(Of String)(columnAliases)
                    q = q.OuterQuery
                    i += 1
                Next
                'Loop
                Return inner
            End Function

            Protected Overridable ReadOnly Property WithLoad() As Boolean
                Get
                    Return True
                End Get
            End Property

            Protected Overridable Function ExecStmt(ByVal cmd As System.Data.Common.DbCommand) As ReadOnlyObjectList(Of ReturnType)
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)
                Dim rr As New List(Of ReturnType)

                Dim oschema As IOrmObjectSchemaBase = dbm.DbSchema.GetObjectSchema(_q.SelectedType, False)
                Dim fields As Collections.IndexedCollection(Of String, MapField2Column) = Nothing
                If oschema IsNot Nothing Then
                    fields = oschema.GetFieldColumnMap
                Else
                    fields = GetFieldsIdx(_q)
                End If

                Dim t As Type = _q.CreateType
                If t Is Nothing Then
                    t = _q.SelectedType
                Else
                    oschema = dbm.DbSchema.GetObjectSchema(t, False)
                End If

                dbm.LoadMultipleObjects(t, cmd, True, rr, GetFields(dbm.DbSchema, _q.SelectedType, _q, True), oschema, fields)
                _q.ExecCount += 1
                Return New ReadOnlyObjectList(Of ReturnType)(rr)
            End Function

            Public Function GetSimpleValues(Of T)() As IList(Of T)
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)

                Using cmd As System.Data.Common.DbCommand = dbm.DbSchema.CreateDBCommand
                    ', dbm, Query, GetType(ReturnType), _j, _f
                    MakeStatement(cmd)

                    Return ExecStmt(Of T)(cmd)
                End Using
            End Function

            Protected Overridable Function ExecStmt(Of T)(ByVal cmd As System.Data.Common.DbCommand) As IList(Of T)
                Dim l As IList(Of T) = _mgr.GetSimpleValues(Of T)(cmd)
                _q.ExecCount += 1
                Return l
            End Function

            Protected Function GetFieldsIdx(ByVal q As QueryCmd) As Collections.IndexedCollection(Of String, MapField2Column)
                Dim c As New Cache.OrmObjectIndex

                For Each p As OrmProperty In q.SelectList
                    c.Add(New MapField2Column(p.Field, p.Column, p.Table))
                Next

                If q.Aggregates IsNot Nothing Then
                    For Each p As AggregateBase In q.Aggregates
                        c.Add(New MapField2Column(p.Alias, p.Alias, Nothing))
                    Next
                End If

                Return c
            End Function

            Public Sub ResetCache()
                _dic.Remove(Id)
            End Sub

            Public Overridable Property Renew() As Boolean Implements ICustDelegateBase(Of ReturnType).Renew
                Get
                    Return _renew
                End Get
                Set(ByVal Value As Boolean)
                    _renew = Value
                End Set
            End Property

            Public Overridable Property Created() As Boolean Implements ICustDelegateBase(Of ReturnType).Created
                Get
                    Return _created
                End Get
                Set(ByVal Value As Boolean)
                    _created = Value
                End Set
            End Property

            Public Overridable ReadOnly Property SmartSort() As Boolean Implements ICustDelegateBase(Of ReturnType).SmartSort
                Get
                    Return True
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

            Public ReadOnly Property Fetch() As TimeSpan
                Get
                    Return _mgr.Fecth
                End Get
            End Property

            Public ReadOnly Property Exec() As TimeSpan
                Get
                    Return _mgr.Exec
                End Get
            End Property

            Public Sub CreateDepends() Implements OrmManagerBase.ICustDelegateBase(Of ReturnType).CreateDepends
                If _j IsNot Nothing Then
                    For Each js As List(Of Worm.Criteria.Joins.OrmJoin) In _j
                        For Each j As OrmJoin In js
                            If j.Type IsNot Nothing Then
                                _mgr.Cache.AddFilterlessDependType(_mgr.GetFilterInfo, j.Type, _key, _id, _mgr.ObjectSchema)
                            ElseIf Not String.IsNullOrEmpty(j.EntityName) Then
                                _mgr.Cache.AddFilterlessDependType(_mgr.GetFilterInfo, _mgr.ObjectSchema.GetTypeByEntityName(j.EntityName), _key, _id, _mgr.ObjectSchema)
                            End If
                        Next
                    Next
                End If

                Dim i As Integer = 0
                For Each q As QueryCmd In New QueryIterator(_q)
                    CreateDepends(q, i)
                    i += 1
                Next
            End Sub

            Protected Sub CreateDepends(ByVal q As QueryCmd, ByVal i As Integer)
                If q.SelectedType IsNot Nothing AndAlso GetType(_ICachedEntity).IsAssignableFrom(q.SelectedType) Then

                    If _f IsNot Nothing AndAlso _f.Length > i Then
                        _mgr.Cache.AddDependType(_mgr.GetFilterInfo, q.SelectedType, _key, _id, _f(i), _mgr.ObjectSchema)
                    End If

                    If q.Obj IsNot Nothing Then
                        _mgr.Cache.AddM2MQuery(q.Obj.GetM2M(q.SelectedType, q.M2MKey), _key, _id)
                    End If

                End If
            End Sub

            Public ReadOnly Property Filter() As Criteria.Core.IFilter Implements OrmManagerBase.ICustDelegateBase(Of ReturnType).Filter
                Get
                    Return _f(0)
                End Get
            End Property

            Public Overridable Function GetCacheItem(ByVal withLoad As Boolean) As OrmManagerBase.CachedItem Implements OrmManagerBase.ICustDelegateBase(Of ReturnType).GetCacheItem
                Return New CachedItem(GetEntities(), _mgr.Cache)
            End Function

            Public ReadOnly Property Sort() As Sorting.Sort Implements OrmManagerBase.ICustDelegateBase(Of ReturnType).Sort
                Get
                    Return _q.propSort
                End Get
            End Property
        End Class

        Class Processor(Of ReturnType As {ICachedEntity})
            Inherits ProcessorBase(Of ReturnType)
            Implements OrmManagerBase.ICacheValidator, ICustDelegate(Of ReturnType)

            'Private _stmt As String
            'Private _params As ParamMgr
            'Private _cmdType As System.Data.CommandType

            'Private _mgr As OrmReadOnlyDBManager
            'Private _j As List(Of List(Of Worm.Criteria.Joins.OrmJoin))
            'Private _f() As IFilter
            'Private _q As QueryCmd
            'Private _key As String
            'Private _id As String
            'Private _sync As String
            'Private _dic As IDictionary

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.OrmJoin)), _
                ByVal f() As IFilter, ByVal q As QueryCmd)
                MyBase.New(mgr, j, f, q)
                '_mgr = mgr
                '_q = q

                'Reset(j, f, q.SelectedType)
            End Sub

            Protected Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.OrmJoin)), _
                ByVal f() As IFilter, ByVal q As QueryCmd, ByVal t As Type)
                MyBase.New(mgr, j, f, q, t)
                '_mgr = mgr
                '_q = q

                'Reset(j, f, t)
            End Sub

            'Public Sub ResetDic()
            '    If Not String.IsNullOrEmpty(_key) Then
            '        Dim c As Boolean
            '        _dic = _mgr.GetDic(_mgr.Cache, _key, c)
            '    Else
            '        _dic = Nothing
            '    End If
            '    'If Not c Then
            '    '    _mgr.Cache.Filters.Remove(_key)
            '    '    _dic = _mgr.GetDic(_mgr.Cache, _key, c)
            '    'End If
            'End Sub

            'Public Overrides Sub CreateDepends()
            '    If _f IsNot Nothing AndAlso _f.Length = 1 Then
            '        _mgr.Cache.AddDependType(_mgr.GetFilterInfo, _q.SelectedType, _key, _id, _f(0), _mgr.ObjectSchema)
            '    End If

            '    If _j IsNot Nothing AndAlso _j.Count = 1 Then
            '        For Each j As OrmJoin In _j(0)
            '            If j.Type IsNot Nothing Then
            '                _mgr.Cache.AddFilterlessDependType(_mgr.GetFilterInfo, j.Type, _key, _id, _mgr.ObjectSchema)
            '            ElseIf Not String.IsNullOrEmpty(j.EntityName) Then
            '                _mgr.Cache.AddFilterlessDependType(_mgr.GetFilterInfo, _mgr.ObjectSchema.GetTypeByEntityName(j.EntityName), _key, _id, _mgr.ObjectSchema)
            '            End If
            '        Next
            '    End If

            '    If _q.Obj IsNot Nothing Then
            '        _mgr.Cache.AddM2MQuery(_q.Obj.GetM2M(_q.SelectedType, _q.M2MKey), _key, _id)
            '    End If
            'End Sub

            'Public Overrides ReadOnly Property Filter() As Criteria.Core.IFilter
            '    Get
            '        'Throw New NotSupportedException
            '        Return _f(0)
            '    End Get
            'End Property

            Public Overloads Overrides Function GetCacheItem(ByVal withLoad As Boolean) As OrmManagerBase.CachedItem
                Dim r As ReadOnlyEntityList(Of ReturnType) = CType(GetEntities(), ReadOnlyEntityList(Of ReturnType))
                Return GetCacheItem(r)
            End Function

            Public Overloads Function GetCacheItem(ByVal col As ReadOnlyEntityList(Of ReturnType)) As OrmManagerBase.CachedItem Implements ICustDelegate(Of ReturnType).GetCacheItem
                Dim sortex As IOrmSorting2 = TryCast(_mgr.ObjectSchema.GetObjectSchema(_q.SelectedType), IOrmSorting2)
                Dim s As Date = Nothing
                If sortex IsNot Nothing Then
                    Dim ts As TimeSpan = sortex.SortExpiration(_q.propSort)
                    If ts <> TimeSpan.MaxValue AndAlso ts <> TimeSpan.MinValue Then
                        s = Now.Add(ts)
                    End If
                End If
                Return New OrmManagerBase.CachedItem(_q.propSort, s, _f(0), col, _mgr)
            End Function

            'Public Overrides Function GetEntities(ByVal withLoad As Boolean) As ReadOnlyEntityList(Of ReturnType)
            '    Dim r As ReadOnlyEntityList(Of ReturnType)
            '    Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)

            '    Using cmd As System.Data.Common.DbCommand = dbm.DbSchema.CreateDBCommand
            '        ', dbm, Query, GetType(ReturnType), _j, _f
            '        MakeStatement(cmd)

            '        r = ExecStmt(cmd)
            '    End Using

            '    If Query.propSort IsNot Nothing AndAlso Query.propSort.IsExternal Then
            '        r = CType(dbm.DbSchema.ExternalSort(Of ReturnType)(dbm, Query.propSort, r), Global.Worm.ReadOnlyEntityList(Of ReturnType))
            '    End If

            '    Return r
            'End Function

            'Public Function GetSimpleValues(Of T)() As IList(Of T)
            '    Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)

            '    Using cmd As System.Data.Common.DbCommand = dbm.DbSchema.CreateDBCommand
            '        ', dbm, Query, GetType(ReturnType), _j, _f
            '        MakeStatement(cmd)

            '        Return ExecStmt(Of T)(cmd)
            '    End Using
            'End Function

            'ByVal cmd As System.Data.Common.DbCommand, _
            '   ByVal mgr As OrmReadOnlyDBManager, ByVal query As QueryCmdBase, ByVal t As Type, _
            '    ByVal joins As List(Of Worm.Criteria.Joins.OrmJoin), ByVal f As IFilter

            'Protected Overridable Sub MakeStatement(ByVal cmd As System.Data.Common.DbCommand)
            '    'Dim mgr As OrmReadOnlyDBManager = _mgr
            '    'Dim t As Type = GetType(ReturnType)
            '    'Dim joins As List(Of Worm.Criteria.Joins.OrmJoin) = _j
            '    'Dim f As IFilter = _f

            '    If String.IsNullOrEmpty(_stmt) Then
            '        _cmdType = System.Data.CommandType.Text

            '        _params = New ParamMgr(Mgr.DbSchema, "p")
            '        _stmt = _MakeStatement()
            '    End If

            '    cmd.CommandText = _stmt
            '    cmd.CommandType = _cmdType
            '    _params.AppendParams(cmd.Parameters)
            'End Sub

            'Protected Overrides Function _MakeStatement() As String
            '    Dim almgr As AliasMgr = AliasMgr.Create
            '    Dim fi As Object = _mgr.GetFilterInfo
            '    Dim t As Type = _q.SelectedType
            '    Dim i As Integer = 0
            '    Dim q As QueryCmd = _q
            '    'Dim sb As New StringBuilder
            '    Dim inner As String = Nothing
            '    Dim innerColumns As List(Of String) = Nothing
            '    Do While q IsNot Nothing
            '        Dim columnAliases As New List(Of String)
            '        Dim j As List(Of Worm.Criteria.Joins.OrmJoin) = _j(i)
            '        Dim f As IFilter = _f(i)
            '        inner = MakeQueryStatement(fi, _mgr.DbSchema, q, _params, t, j, f, almgr, columnAliases, inner, innerColumns, i, q.propWithLoad)
            '        innerColumns = New List(Of String)(columnAliases)
            '        q = q.OuterQuery
            '        i += 1
            '    Loop
            '    Return inner
            'End Function

            'Protected Overridable Function ExecStmt(Of T)(ByVal cmd As System.Data.Common.DbCommand) As IList(Of T)
            '    Dim l As IList(Of T) = _mgr.GetSimpleValues(Of T)(cmd)
            '    _q.ExecCount += 1
            '    Return l
            'End Function

            Protected Overrides ReadOnly Property WithLoad() As Boolean
                Get
                    Return _q.propWithLoad
                End Get
            End Property

            Protected Overrides Function ExecStmt(ByVal cmd As System.Data.Common.DbCommand) As ReadOnlyObjectList(Of ReturnType)
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)
                Dim rr As New List(Of ReturnType)
                'If GetType(ReturnType) IsNot Query.SelectedType Then
                dbm.LoadMultipleObjects(_q.SelectedType, cmd, _q.propWithLoad, rr, GetFields(dbm.DbSchema, _q.SelectedType, _q, _q.propWithLoad))
                _q.ExecCount += 1
                'Else
                'dbm.LoadMultipleObjects(Of ReturnType)(cmd, Query.WithLoad, rr, GetFields(dbm.DbSchema, GetType(ReturnType), Query))
                'End If
                Return CType(OrmManagerBase.CreateReadonlyList(GetType(ReturnType), rr), Global.Worm.ReadOnlyObjectList(Of ReturnType))
            End Function

            'Protected ReadOnly Property Mgr() As OrmReadOnlyDBManager
            '    Get
            '        Return _mgr
            '    End Get
            'End Property

            'Public Overrides ReadOnly Property Sort() As Sorting.Sort
            '    Get
            '        Return _q.propSort
            '    End Get
            'End Property

            'Protected ReadOnly Property Query() As QueryCmd
            '    Get
            '        Return _q
            '    End Get
            'End Property

            'Public ReadOnly Property Key() As String
            '    Get
            '        Return _key
            '    End Get
            'End Property

            'Public ReadOnly Property Id() As String
            '    Get
            '        Return _id
            '    End Get
            'End Property

            'Public ReadOnly Property Sync() As String
            '    Get
            '        Return _sync
            '    End Get
            'End Property

            'Public ReadOnly Property Dic() As IDictionary
            '    Get
            '        Return _dic
            '    End Get
            'End Property

            'Public Sub ResetStmt()
            '    _stmt = Nothing
            'End Sub

            'Public Sub Reset(ByVal j As List(Of List(Of Worm.Criteria.Joins.OrmJoin)), ByVal f() As IFilter, ByVal t As Type)
            '    _j = j
            '    _f = f
            '    _key = Query.GetStaticKey(_mgr.GetStaticKey(), _j, _f, t, _mgr.Cache.CacheListBehavior)
            '    '_dic = _mgr.GetDic(_mgr.Cache, _key)
            '    _id = Query.GetDynamicKey(_j, _f)
            '    _sync = _id & _mgr.GetStaticKey()

            '    ResetStmt()
            '    ResetDic()
            'End Sub

            Public Overridable Function Validate() As Boolean Implements OrmManagerBase.ICacheValidator.Validate
                If _f IsNot Nothing Then
                    For Each f_ As IFilter In _f
                        If f_ IsNot Nothing Then
                            For Each fl As IFilter In f_.GetAllFilters
                                Dim f As IEntityFilter = TryCast(fl, IEntityFilter)
                                If f IsNot Nothing Then
                                    Dim tmpl As OrmFilterTemplateBase = CType(f.Template, OrmFilterTemplateBase)

                                    Dim fields As List(Of String) = Nothing
                                    If _mgr.Cache.GetUpdatedFields(tmpl.Type, fields) Then
                                        Dim idx As Integer = fields.IndexOf(tmpl.FieldName)
                                        If idx >= 0 Then
                                            Dim p As New Pair(Of String, Type)(tmpl.FieldName, tmpl.Type)
                                            _mgr.Cache.ResetFieldDepends(p)
                                            _mgr.Cache.RemoveUpdatedFields(tmpl.Type, tmpl.FieldName)
                                            Return False
                                        End If
                                    End If
                                End If
                            Next
                        End If
                    Next
                End If
                Return True
            End Function

            Public Overridable Function Validate(ByVal ce As OrmManagerBase.CachedItem) As Boolean Implements OrmManagerBase.ICacheValidator.Validate
                Return True
            End Function
        End Class

        Class ProcessorT(Of SelectType As {ICachedEntity, New}, ReturnType As {ICachedEntity})
            Inherits Processor(Of ReturnType)

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.OrmJoin)), _
                ByVal f() As IFilter, ByVal q As QueryCmd)
                MyBase.New(mgr, j, f, q, GetType(SelectType))
            End Sub

            Protected Overrides Function ExecStmt(ByVal cmd As System.Data.Common.DbCommand) As ReadOnlyObjectList(Of ReturnType)
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)
                Dim rr As New List(Of ReturnType)
                dbm.LoadMultipleObjects(Of SelectType)(cmd, _q.propWithLoad, rr, GetFields(dbm.DbSchema, GetType(ReturnType), _q, _q.propWithLoad))
                _q.ExecCount += 1
                Return CType(OrmManagerBase.CreateReadonlyList(GetType(ReturnType), rr), ReadOnlyObjectList(Of ReturnType))
            End Function
        End Class

        'Class M2MPublicProcessor(Of ReturnType As {New, Orm.OrmBase})
        '    Inherits Processor(Of ReturnType)

        '    Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal j As List(Of Worm.Criteria.Joins.OrmJoin), _
        '        ByVal f As IFilter, ByVal q As QueryCmdBase)
        '        MyBase.New(mgr, j, f, q)
        '    End Sub


        'End Class


        'Class M2MProcessor(Of ReturnType As {New, IOrmBase})
        '    Inherits Processor(Of ReturnType)

        '    Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.OrmJoin)), _
        '        ByVal f() As IFilter, ByVal q As QueryCmdBase)
        '        MyBase.New(mgr, j, f, q)
        '    End Sub

        '    Protected Overrides Function ExecStmt(ByVal cmd As System.Data.Common.DbCommand) As ReadOnlyList(Of ReturnType)
        '        Throw New NotSupportedException
        '    End Function

        '    Public Overrides Function GetValues(ByVal withLoad As Boolean) As ReadOnlyList(Of ReturnType)
        '        Throw New NotSupportedException
        '    End Function

        '    Public Overrides Function GetCacheItem(ByVal col As ReadOnlyList(Of ReturnType)) As OrmManagerBase.CachedItem
        '        Dim ids As New List(Of Object)
        '        For Each o As ReturnType In col
        '            ids.Add(o.Identifier)
        '        Next
        '        Return GetCacheItem(ids)
        '    End Function

        '    Public Overrides Function GetCacheItem(ByVal withLoad As Boolean) As OrmManagerBase.CachedItem
        '        Return GetCacheItem(GetValuesInternal(withLoad))
        '    End Function

        '    Protected Overloads Function GetCacheItem(ByVal ids As List(Of Object)) As OrmManagerBase.CachedItem
        '        Return New OrmManagerBase.M2MCache(Query.Sort, Filter, Query.Obj.Identifier, ids, Mgr, Query.Obj.GetType, GetType(ReturnType), Query.Key)
        '    End Function

        '    Protected Function GetValuesInternal(ByVal withLoad As Boolean) As List(Of Object)
        '        Using cmd As System.Data.Common.DbCommand = Mgr.DbSchema.CreateDBCommand
        '            ', dbm, Query, GetType(ReturnType), _j, _f
        '            MakeStatement(cmd)

        '            Return ExecStmtInternal(cmd)
        '        End Using
        '    End Function

        '    Protected Function ExecStmtInternal(ByVal cmd As System.Data.Common.DbCommand) As List(Of Object)
        '        Throw New NotImplementedException
        '    End Function

        '    Protected Overrides Function _MakeStatement() As String

        '    End Function
        'End Class
    End Class

End Namespace
