Imports Worm.Database
Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.OrmManager
Imports Worm.Cache

Namespace Query.Database

    Partial Public Class DbQueryExecutor

        Class ProviderAnonym(Of ReturnType As {_IEntity})
            Inherits CacheItemBaseProvider

            'Private _created As Boolean
            'Private _renew As Boolean

            Protected Sub New()

            End Sub

            Private _stmt As String
            Protected _params As ParamMgr
            Private _cmdType As System.Data.CommandType

            'Protected _mgr As OrmReadOnlyDBManager
            'Protected _j As List(Of List(Of Worm.Criteria.Joins.OrmJoin))
            'Protected _f() As IFilter
            'Protected _sl As List(Of List(Of SelectExpression))
            'Protected _q As QueryCmd
            'Private _key As String
            'Private _id As String
            'Private _sync As String
            'Private _dic As IDictionary

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.QueryJoin)), _
                ByVal f() As IFilter, ByVal q As QueryCmd, ByVal sl As List(Of List(Of SelectExpression)))

                Reset(mgr, j, f, sl, q)
            End Sub

            'Protected Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.QueryJoin)), _
            '    ByVal f() As IFilter, ByVal q As QueryCmd, ByVal sl As List(Of List(Of SelectExpression)))

            '    Reset(mgr, j, f, sl, q)
            'End Sub

            Public Sub ResetStmt()
                _stmt = Nothing
            End Sub

            Public Overrides Sub Reset(ByVal mgr As OrmManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.QueryJoin)), _
                             ByVal f() As IFilter, ByVal sl As List(Of List(Of SelectExpression)), ByVal q As QueryCmd)
                _j = j
                _f = f
                _sl = sl
                _mgr = mgr
                _q = q
                _dic = Nothing

                Dim str As String
                If _q.Table IsNot Nothing Then
                    str = _q.Table.RawName
                Else
                    str = mgr.MappingEngine.GetEntityKey(mgr.GetFilterInfo, _q.GetSelectedType(mgr.MappingEngine))
                End If

                _key = _q.GetStaticKey(_mgr.GetStaticKey(), _j, _f, _mgr.Cache.CacheListBehavior, sl, str, _mgr.MappingEngine, _dic)

                If _dic Is Nothing Then
                    _dic = GetExternalDic(_key)
                    If _dic IsNot Nothing Then
                        _key = Nothing
                    End If
                End If

                If Not String.IsNullOrEmpty(_key) OrElse _dic IsNot Nothing Then
                    _id = _q.GetDynamicKey(_j, _f)
                    _sync = _id & _mgr.GetStaticKey()
                End If

                ResetStmt()
                ResetDic()
            End Sub

            Public Sub ResetDic()
                If Not String.IsNullOrEmpty(_key) AndAlso _dic Is Nothing Then
                    _dic = _mgr.GetDic(_mgr.Cache, _key)
                End If
            End Sub

            Public Function GetEntities() As ReadOnlyObjectList(Of ReturnType)
                Dim r As ReadOnlyObjectList(Of ReturnType)
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)

                Using cmd As System.Data.Common.DbCommand = dbm.CreateDBCommand
                    ', dbm, Query, GetType(ReturnType), _j, _f
                    MakeStatement(cmd)

                    r = ExecStmt(cmd)
                End Using

                If Sort IsNot Nothing AndAlso Sort.IsExternal Then
                    r = CType(dbm.MappingEngine.ExternalSort(Of ReturnType)(dbm, Sort, r.List), ReadOnlyObjectList(Of ReturnType))
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

            'Protected Overridable ReadOnly Property WithLoad() As Boolean
            '    Get
            '        Return True
            '    End Get
            'End Property

            Protected Overridable Function ExecStmt(ByVal cmd As System.Data.Common.DbCommand) As ReadOnlyObjectList(Of ReturnType)
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)
                Dim rr As New List(Of ReturnType)

                Dim oschema As IEntitySchema = _q.GetSchemaForSelectedType(_mgr.MappingEngine)
                Dim fields As Collections.IndexedCollection(Of String, MapField2Column) = Nothing
                If oschema IsNot Nothing Then
                    fields = oschema.GetFieldColumnMap
                Else
                    fields = GetFieldsIdx(_q)
                End If

                Dim t As Type = _q.CreateType.GetRealType(dbm.MappingEngine)
                'If t Is Nothing Then
                '    t = SelectedType
                'End If
                oschema = dbm.MappingEngine.GetObjectSchema(t, False)

                dbm.LoadMultipleObjects(t, cmd, True, rr, GetFields(dbm.MappingEngine, _q, _sl(0)), oschema, fields)
                _q.ExecCount += 1
                Return New ReadOnlyObjectList(Of ReturnType)(rr)
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

            Protected Function GetFieldsIdx(ByVal q As QueryCmd) As Collections.IndexedCollection(Of String, MapField2Column)
                Dim c As New OrmObjectIndex

                'For Each p As SelectExpression In q.SelectList
                '    c.Add(New MapField2Column(p.Field, p.Column, p.Table, p.Attributes))
                'Next

                SelectExpression.GetMapping(c, q.SelectList)

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


            'Protected Sub CreateDepends(ByVal q As QueryCmd, ByVal i As Integer)
            '    If q.SelectedType IsNot Nothing AndAlso GetType(_ICachedEntity).IsAssignableFrom(q.SelectedType) Then

            '        If _f IsNot Nothing AndAlso _f.Length > i Then
            '            _mgr.Cache.AddDependType(_mgr.GetFilterInfo, q.SelectedType, _key, _id, _f(i), _mgr.MappingEngine)

            '            Dim ef As IEntityFilter = TryCast(_f(i), IEntityFilter)
            '            If ef IsNot Nothing AndAlso Not _notPreciseDepends Then
            '            Else
            '                Dim f As Cache.IDependentTypes = Cache.QueryDependentTypes(_f(i))
            '                If f IsNot Nothing Then

            '                End If
            '            End If
            '        End If

            '        If q.Obj IsNot Nothing Then
            '            _mgr.Cache.AddM2MQuery(q.Obj.GetM2M(q.SelectedType, q.M2MKey), _key, _id)
            '        End If

            '    End If
            'End Sub

            Public Overrides Function GetCacheItem(ByVal withLoad As Boolean) As CachedItem
                Return New CachedItem(GetEntities(), _mgr.Cache)
            End Function
        End Class

        Class ProviderAnonym(Of CreateType As {New, _IEntity}, ReturnType As {_IEntity})
            Inherits ProviderAnonym(Of ReturnType)

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.QueryJoin)), _
                ByVal f() As IFilter, ByVal q As QueryCmd, ByVal sl As List(Of List(Of SelectExpression)))
                MyBase.New(mgr, j, f, q, sl)
            End Sub

            Protected Overrides Function ExecStmt(ByVal cmd As System.Data.Common.DbCommand) As ReadOnlyObjectList(Of ReturnType)
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)
                Dim rr As New List(Of ReturnType)

                Dim oschema As IEntitySchema = _q.GetSchemaForSelectedType(_mgr.MappingEngine)
                Dim fields As Collections.IndexedCollection(Of String, MapField2Column) = Nothing
                If oschema IsNot Nothing Then
                    fields = oschema.GetFieldColumnMap
                Else
                    fields = GetFieldsIdx(_q)
                End If

                Dim t As Type = _q.CreateType.GetRealType(dbm.MappingEngine)
                'If t Is Nothing Then
                '    t = SelectedType
                'End If
                If t IsNot SelectedType AndAlso t IsNot Nothing Then
                    oschema = dbm.MappingEngine.GetObjectSchema(t, False)
                End If

                dbm.QueryObjects(Of CreateType)(cmd, _q.propWithLoad, rr, GetFields(dbm.MappingEngine, _q, _sl(0)), oschema, fields)
                _q.ExecCount += 1
                Return CType(OrmManager.CreateReadonlyList(GetType(ReturnType), rr), ReadOnlyObjectList(Of ReturnType))
            End Function
        End Class

        Class Provider(Of ReturnType As {ICachedEntity})
            Inherits ProviderAnonym(Of ReturnType)
            Implements OrmManager.ICacheValidator, ICacheItemProvoder(Of ReturnType)

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

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.QueryJoin)), _
                ByVal f() As IFilter, ByVal q As QueryCmd, ByVal sl As List(Of List(Of SelectExpression)))
                MyBase.New(mgr, j, f, q, sl)
                '_mgr = mgr
                '_q = q

                'Reset(j, f, q.SelectedType)
            End Sub

            'Protected Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.QueryJoin)), _
            '    ByVal f() As IFilter, ByVal q As QueryCmd, ByVal t As Type, ByVal sl As List(Of List(Of SelectExpression)))
            '    MyBase.New(mgr, j, f, q, sl)
            '    '_mgr = mgr
            '    '_q = q

            '    'Reset(j, f, t)
            'End Sub

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

            Public Overloads Overrides Function GetCacheItem(ByVal withLoad As Boolean) As CachedItem
                Dim r As ReadOnlyEntityList(Of ReturnType) = CType(GetEntities(), ReadOnlyEntityList(Of ReturnType))
                Return GetCacheItem(r)
            End Function

            Public Overridable Overloads Function GetCacheItem(ByVal col As ReadOnlyEntityList(Of ReturnType)) As CachedItem Implements ICacheItemProvoder(Of ReturnType).GetCacheItem
                Return _GetCacheItem(col)
            End Function

            Protected Function _GetCacheItem(ByVal col As ReadOnlyEntityList(Of ReturnType)) As CachedItem

                Dim sortex As IOrmSorting2 = TryCast(_mgr.MappingEngine.GetObjectSchema(t, False), IOrmSorting2)
                Dim s As Date = Nothing
                If sortex IsNot Nothing Then
                    Dim ts As TimeSpan = sortex.SortExpiration(Sort)
                    If ts <> TimeSpan.MaxValue AndAlso ts <> TimeSpan.MinValue Then
                        s = Now.Add(ts)
                    End If
                End If
                Dim f As IFilter = Nothing
                If _f.Length > 0 Then
                    f = _f(0)
                End If
                Return New CachedItem(Sort, s, f, col, _mgr)
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

            'Protected Overrides ReadOnly Property WithLoad() As Boolean
            '    Get
            '        Return _q.propWithLoad
            '    End Get
            'End Property

            Protected Overrides Function ExecStmt(ByVal cmd As System.Data.Common.DbCommand) As ReadOnlyObjectList(Of ReturnType)
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)
                Dim rr As New List(Of ReturnType)
                'If GetType(ReturnType) IsNot Query.SelectedType Then
                dbm.LoadMultipleObjects(_q.CreateType.GetRealType(dbm.MappingEngine), cmd, _q.propWithLoad, rr, GetFields(dbm.MappingEngine, _q, _sl(0)))
                _q.ExecCount += 1
                'Else
                'dbm.LoadMultipleObjects(Of ReturnType)(cmd, Query.WithLoad, rr, GetFields(dbm.DbSchema, GetType(ReturnType), Query))
                'End If
                Return CType(OrmManager.CreateReadonlyList(GetType(ReturnType), rr), Global.Worm.ReadOnlyObjectList(Of ReturnType))
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

            Public Overridable Function Validate() As Boolean Implements OrmManager.ICacheValidator.ValidateBeforCacheProbe
                'If _f IsNot Nothing Then
                '    For Each f_ As IFilter In _f
                '        If f_ IsNot Nothing Then
                '            For Each fl As IFilter In f_.GetAllFilters
                '                Dim f As IEntityFilter = TryCast(fl, IEntityFilter)
                '                If f IsNot Nothing Then
                '                    Dim tmpl As OrmFilterTemplate = CType(f.Template, OrmFilterTemplate)

                '                    Dim fields As List(Of String) = Nothing
                '                    If _mgr.Cache.GetUpdatedFields(tmpl.Type, fields) Then
                '                        Dim idx As Integer = fields.IndexOf(tmpl.FieldName)
                '                        If idx >= 0 Then
                '                            Dim p As New Pair(Of String, Type)(tmpl.FieldName, tmpl.Type)
                '                            _mgr.Cache.ResetFieldDepends(p)
                '                            _mgr.Cache.RemoveUpdatedFields(tmpl.Type, tmpl.FieldName)
                '                            Return False
                '                        End If
                '                    End If
                '                End If
                '            Next
                '        End If
                '    Next
                'End If
                Return True
            End Function

            Public Overridable Function Validate(ByVal ce As CachedItem) As Boolean Implements OrmManager.ICacheValidator.ValidateItemFromCache
                Return ValidateFromCache()
            End Function
        End Class

        Class ProviderT(Of CreateType As {ICachedEntity, New}, ReturnType As {ICachedEntity})
            Inherits Provider(Of ReturnType)

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.QueryJoin)), _
                ByVal f() As IFilter, ByVal q As QueryCmd, ByVal sl As List(Of List(Of SelectExpression)))
                MyBase.New(mgr, j, f, q, sl)
            End Sub

            Protected Overrides Function ExecStmt(ByVal cmd As System.Data.Common.DbCommand) As ReadOnlyObjectList(Of ReturnType)
                Dim dbm As OrmReadOnlyDBManager = CType(_mgr, OrmReadOnlyDBManager)
                Dim rr As New List(Of ReturnType)

                Dim oschema As IEntitySchema = _q.GetSchemaForSelectedType(_mgr.MappingEngine)
                Dim fields As Collections.IndexedCollection(Of String, MapField2Column) = Nothing
                If oschema IsNot Nothing Then
                    fields = oschema.GetFieldColumnMap
                Else
                    fields = GetFieldsIdx(_q)
                End If

                Dim t As Type = _q.CreateType.GetRealType(dbm.MappingEngine)
                'If t Is Nothing Then
                '    t = SelectedType
                'End If
                If t IsNot SelectedType AndAlso t IsNot Nothing Then
                    oschema = dbm.MappingEngine.GetObjectSchema(t, False)
                End If

                dbm.QueryObjects(Of CreateType)(cmd, _q.propWithLoad, rr, GetFields(dbm.MappingEngine, _q, _sl(0)), oschema, fields)
                _q.ExecCount += 1
                Return CType(OrmManager.CreateReadonlyList(GetType(ReturnType), rr), ReadOnlyObjectList(Of ReturnType))
            End Function

            'Protected Overrides Function _MakeStatement() As String
            '    Return _MakeStatement(SelectedType)
            'End Function

            'Public Overrides Function GetCacheItem(ByVal col As ReadOnlyEntityList(Of ReturnType)) As CachedItem
            '    Return _GetCacheItem(col, SelectedType)
            'End Function
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

        '    Public Overrides Function GetCacheItem(ByVal col As ReadOnlyList(Of ReturnType)) As CachedItem
        '        Dim ids As New List(Of Object)
        '        For Each o As ReturnType In col
        '            ids.Add(o.Identifier)
        '        Next
        '        Return GetCacheItem(ids)
        '    End Function

        '    Public Overrides Function GetCacheItem(ByVal withLoad As Boolean) As CachedItem
        '        Return GetCacheItem(GetValuesInternal(withLoad))
        '    End Function

        '    Protected Overloads Function GetCacheItem(ByVal ids As List(Of Object)) As CachedItem
        '        Return New M2MCache(Query.Sort, Filter, Query.Obj.Identifier, ids, Mgr, Query.Obj.GetType, GetType(ReturnType), Query.Key)
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
