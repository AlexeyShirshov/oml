Imports Worm
Imports System.Collections.Generic
Imports Worm.Orm
'Imports Worm.Database.Criteria.Core
Imports Worm.Sorting
Imports Worm.Orm.Meta
Imports Worm.Cache
Imports Worm.Criteria.Values
Imports Worm.Database.Criteria.Joins
Imports Worm.Orm.Query
Imports Worm.Criteria.Core

Namespace Database

    Partial Public Class OrmReadOnlyDBManager

        Protected MustInherit Class BaseDataProvider(Of T As {New, IOrmBase})
            Inherits CustDelegate(Of T)
            Implements ICacheValidator

            Protected _f As Worm.Criteria.Core.IFilter
            Protected _sort As Sort
            Protected _mgr As OrmReadOnlyDBManager
            Protected _key As String
            Protected _id As String

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal f As Worm.Criteria.Core.IFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String)

                If mgr Is Nothing Then
                    Throw New ArgumentNullException("mgr")
                End If

                _mgr = mgr
                _f = f
                _sort = sort
                '_st = st
                _key = key
                _id = id
            End Sub

            Public Overridable Function Validate() As Boolean Implements OrmManager.ICacheValidator.ValidateBeforCacheProbe
                If _f IsNot Nothing Then
                    Dim c As OrmCache = TryCast(_mgr.Cache, OrmCache)
                    If c IsNot Nothing Then
                        For Each fl As IFilter In _f.GetAllFilters
                            Dim f As IEntityFilter = TryCast(fl, IEntityFilter)
                            If f IsNot Nothing Then
                                Dim tmpl As OrmFilterTemplateBase = CType(f.Template, OrmFilterTemplateBase)

                                Dim fields As List(Of String) = Nothing
                                If c.GetUpdatedFields(tmpl.Type, fields) Then
                                    Dim idx As Integer = fields.IndexOf(tmpl.PropertyAlias)
                                    If idx >= 0 Then
                                        Dim p As New Pair(Of String, Type)(tmpl.PropertyAlias, tmpl.Type)
                                        c.ResetFieldDepends(p)
                                        c.RemoveUpdatedFields(tmpl.Type, tmpl.PropertyAlias)
                                        Return False
                                    End If
                                End If
                            End If
                        Next
                    End If
                End If
                Return True
            End Function

            Public Overridable Function Validate(ByVal ce As OrmManager.CachedItem) As Boolean Implements OrmManager.ICacheValidator.ValidateItemFromCache
                Return True
            End Function

            Public Overrides Sub CreateDepends()
                If Not _mgr._dont_cache_lists AndAlso _f IsNot Nothing Then
                    Dim cache As OrmCache = TryCast(_mgr.Cache, OrmCache)
                    If cache IsNot Nothing Then
                        Dim tt As System.Type = GetType(T)
                        cache.AddDependType(_mgr.GetFilterInfo, tt, _key, _id, _f, _mgr.MappingEngine)

                        For Each fl As IFilter In _f.GetAllFilters
                            Dim f As IEntityFilter = TryCast(fl, IEntityFilter)
                            If f IsNot Nothing Then
                                Dim tmpl As OrmFilterTemplateBase = CType(f.Template, OrmFilterTemplateBase)
                                If tmpl.Type Is Nothing Then
                                    Throw New NullReferenceException("Type for OrmFilterTemplate must be specified")
                                End If
                                Dim v As EntityValue = TryCast(CType(f, EntityFilterBase).Value, EntityValue)
                                If v IsNot Nothing Then
                                    'Dim tp As Type = f.Value.GetType 'Schema.GetFieldTypeByName(f.Type, f.FieldName)
                                    'If GetType(OrmBase).IsAssignableFrom(tp) Then
                                    cache.AddDepend(v.GetOrmValue(_mgr), _key, _id)
                                    'End If
                                Else
                                    Dim p As New Pair(Of String, Type)(tmpl.PropertyAlias, tmpl.Type)
                                    cache.AddFieldDepend(p, _key, _id)
                                End If
                                If tt IsNot tmpl.Type Then
                                    cache.AddJoinDepend(tmpl.Type, tt)
                                End If
                            End If
                        Next
                    End If
                End If
            End Sub

            Public Overrides ReadOnly Property Filter() As Worm.Criteria.Core.IFilter
                Get
                    Return _f
                End Get
            End Property

            Public Overrides ReadOnly Property Sort() As Sort
                Get
                    Return _sort
                End Get
            End Property

            'Public Overrides ReadOnly Property SortType() As SortType
            '    Get
            '        Return _st
            '    End Get
            'End Property

            Public Overrides Function GetCacheItem(ByVal withLoad As Boolean) As OrmManager.CachedItem
                Dim sortex As IOrmSorting2 = TryCast(_mgr.MappingEngine.GetObjectSchema(GetType(T)), IOrmSorting2)
                Dim s As Date = Nothing
                If sortex IsNot Nothing Then
                    Dim ts As TimeSpan = sortex.SortExpiration(_sort)
                    If ts <> TimeSpan.MaxValue AndAlso ts <> TimeSpan.MinValue Then
                        s = Now.Add(ts)
                    End If
                End If
                Return New CachedItem(_sort, s, _f, GetValues(withLoad), _mgr)
            End Function

            Public Overrides Function GetCacheItem(ByVal col As ReadOnlyEntityList(Of T)) As OrmManager.CachedItem
                Dim sortex As IOrmSorting2 = TryCast(_mgr.MappingEngine.GetObjectSchema(GetType(T)), IOrmSorting2)
                Dim s As Date = Nothing
                If sortex IsNot Nothing Then
                    Dim ts As TimeSpan = sortex.SortExpiration(_sort)
                    If ts <> TimeSpan.MaxValue AndAlso ts <> TimeSpan.MinValue Then
                        s = Now.Add(ts)
                    End If
                End If
                Return New CachedItem(_sort, s, _f, col, _mgr)
            End Function
        End Class

        Protected Class FilterCustDelegate(Of T As {New, IOrmBase})
            Inherits BaseDataProvider(Of T)

            Private _cols As List(Of ColumnAttribute)

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal f As Worm.Criteria.Core.IFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String)
                MyBase.New(mgr, f, sort, key, id)
            End Sub

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal f As IFilter, ByVal cols As List(Of ColumnAttribute), _
                ByVal sort As Sort, ByVal key As String, ByVal id As String)
                MyBase.New(mgr, f, sort, key, id)
                _cols = cols
            End Sub

            Public Overrides Function GetValues(ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Dim original_type As Type = GetType(T)

                Using cmd As System.Data.Common.DbCommand = _mgr.CreateDBCommand
                    Dim arr As Generic.List(Of ColumnAttribute) = _cols

                    With cmd
                        .CommandType = System.Data.CommandType.Text

                        Dim almgr As AliasMgr = AliasMgr.Create
                        Dim params As New ParamMgr(_mgr.SQLGenerator, "p")
                        Dim sb As New StringBuilder
                        If withLoad Then
                            If arr Is Nothing Then
                                arr = _mgr.MappingEngine.GetSortedFieldList(original_type)
                            End If
                            AppendSelect(sb, original_type, almgr, params, arr)
                        Else
                            arr = New Generic.List(Of ColumnAttribute)
                            arr.Add(New ColumnAttribute(OrmBaseT.PKName, Field2DbRelations.PK))
                            AppendSelectID(sb, original_type, almgr, params, arr)
                        End If

                        Dim c As New Criteria.Conditions.Condition.ConditionConstructor
                        c.AddFilter(_f)
                        c.AddFilter(AppendWhere)
                        _mgr.SQLGenerator.AppendWhere(original_type, c.Condition, almgr, sb, _mgr.GetFilterInfo, params)
                        If _sort IsNot Nothing AndAlso Not _sort.IsExternal Then
                            _mgr.SQLGenerator.AppendOrder(original_type, _sort, almgr, sb, True, Nothing, Nothing)
                        End If

                        params.AppendParams(.Parameters)
                        .CommandText = sb.ToString
                    End With

                    Dim r As New ReadOnlyList(Of T)
                    _mgr.LoadMultipleObjects(Of T)(cmd, withLoad, r, arr)

                    If _sort IsNot Nothing AndAlso _sort.IsExternal Then
                        r = CType(_mgr.SQLGenerator.ExternalSort(Of T)(_mgr, _sort, r.List), ReadOnlyList(Of T))
                    End If
                    Return r
                End Using
            End Function

            Protected ReadOnly Property Schema() As SQLGenerator
                Get
                    Return _mgr.SQLGenerator
                End Get
            End Property

            Protected ReadOnly Property Mgr() As OrmReadOnlyDBManager
                Get
                    Return CType(_mgr, OrmReadOnlyDBManager)
                End Get
            End Property

            Protected Overridable Function AppendWhere() As IFilter
                Return Nothing
            End Function

            Protected Overridable Sub AppendSelect(ByVal sb As StringBuilder, ByVal t As Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As IList(Of ColumnAttribute))
                sb.Append(_mgr.SQLGenerator.Select(t, almgr, pmgr, arr, Nothing, _mgr.GetFilterInfo))
            End Sub

            Protected Overridable Sub AppendSelectID(ByVal sb As StringBuilder, ByVal t As Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As IList(Of ColumnAttribute))
                sb.Append(_mgr.SQLGenerator.SelectID(t, almgr, pmgr, _mgr.GetFilterInfo))
            End Sub
        End Class

        Protected Class JoinCustDelegate(Of T As {New, IOrmBase})
            Inherits FilterCustDelegate(Of T)

            Private _join() As Worm.Criteria.Joins.OrmJoin
            Private _asc() As QueryAspect

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal join() As Worm.Criteria.Joins.OrmJoin, ByVal f As IFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String, ByVal distinct As Boolean)
                MyBase.New(mgr, f, sort, key, id)
                _join = join
                If distinct Then
                    _asc = New QueryAspect() {New DistinctAspect()}
                End If
            End Sub

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal join() As Worm.Criteria.Joins.OrmJoin, ByVal f As IFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String, ByVal top As Integer)
                MyBase.New(mgr, f, sort, key, id)
                _join = join
                _asc = New QueryAspect() {New TopAspect(top)}
            End Sub

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal join() As Worm.Criteria.Joins.OrmJoin, ByVal f As IFilter, _
               ByVal sort As Sort, ByVal key As String, ByVal id As String, ByVal aspect As QueryAspect)
                MyBase.New(mgr, f, sort, key, id)
                _join = join
                If aspect IsNot Nothing Then
                    _asc = New QueryAspect() {aspect}
                End If
            End Sub

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal join() As Worm.Criteria.Joins.OrmJoin, ByVal f As IFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String, ByVal distinct As Boolean, ByVal cols As List(Of ColumnAttribute))
                MyBase.New(mgr, f, cols, sort, key, id)
                _join = join
                If distinct Then
                    _asc = New QueryAspect() {New DistinctAspect()}
                End If
            End Sub

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal join() As Worm.Criteria.Joins.OrmJoin, ByVal f As IFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String, ByVal top As Integer, ByVal cols As List(Of ColumnAttribute))
                MyBase.New(mgr, f, cols, sort, key, id)
                _join = join
                _asc = New QueryAspect() {New TopAspect(top)}
            End Sub

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal join() As Worm.Criteria.Joins.OrmJoin, ByVal f As IFilter, _
               ByVal sort As Sort, ByVal key As String, ByVal id As String, ByVal aspect As QueryAspect, ByVal cols As List(Of ColumnAttribute))
                MyBase.New(mgr, f, cols, sort, key, id)
                _join = join
                If aspect IsNot Nothing Then
                    _asc = New QueryAspect() {aspect}
                End If
            End Sub

            Protected Overrides Sub AppendSelect(ByVal sb As System.Text.StringBuilder, ByVal t As System.Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As System.Collections.Generic.IList(Of ColumnAttribute))
                sb.Append(Schema.SelectWithJoin(t, almgr, pmgr, _join, True, _asc, Nothing, _mgr.GetFilterInfo, arr))
            End Sub

            Protected Overrides Sub AppendSelectID(ByVal sb As System.Text.StringBuilder, ByVal t As System.Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As System.Collections.Generic.IList(Of ColumnAttribute))
                sb.Append(Schema.SelectWithJoin(t, almgr, pmgr, _join, False, _asc, Nothing, _mgr.GetFilterInfo, Nothing))
            End Sub

            Public Overrides Sub CreateDepends()
                MyBase.CreateDepends()
                If _asc IsNot Nothing AndAlso _asc.Length > 0 AndAlso _f Is Nothing Then
                    Dim cache As OrmCache = TryCast(_mgr.Cache, OrmCache)
                    If cache IsNot Nothing Then
                        Dim tt As System.Type = GetType(T)
                        cache.AddFilterlessDependType(_mgr.GetFilterInfo, tt, _key, _id, _mgr.MappingEngine)
                    End If
                End If
            End Sub
        End Class

        Protected Class DistinctRelationFilterCustDelegate(Of T As {New, IOrmBase})
            Inherits FilterCustDelegate(Of T)

            Private _rel As M2MRelation
            Private _appendSecong As Boolean

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal relation As M2MRelation, ByVal f As IFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String)
                MyBase.New(mgr, f, sort, key, id)

                If relation Is Nothing Then
                    Throw New ArgumentNullException("relation")
                End If

                _rel = relation
                Dim s As IObjectSchemaBase = mgr.MappingEngine.GetObjectSchema(relation.Type)
                Dim cs As IContextObjectSchema = TryCast(s, IContextObjectSchema)

                If s IsNot Nothing AndAlso cs.GetContextFilter(mgr.GetFilterInfo) IsNot Nothing Then
                    _appendSecong = True
                Else
                    If f IsNot Nothing Then
                        For Each fl As EntityFilterBase In f.GetAllFilters
                            If fl.Template.Type Is Nothing Then
                                Throw New NullReferenceException("Type for OrmFilterTemplate must be specified")
                            End If
                            If fl.Template.Type Is relation.Type Then
                                _appendSecong = True
                                Exit For
                            End If
                        Next
                    End If
                End If
            End Sub

            Protected Overrides Sub AppendSelect(ByVal sb As System.Text.StringBuilder, ByVal t As System.Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As System.Collections.Generic.IList(Of ColumnAttribute))
                sb.Append(Schema.SelectDistinct(t, almgr, pmgr, _rel, True, _appendSecong, _mgr.GetFilterInfo, arr))
            End Sub

            Protected Overrides Sub AppendSelectID(ByVal sb As System.Text.StringBuilder, ByVal t As System.Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As System.Collections.Generic.IList(Of ColumnAttribute))
                sb.Append(Schema.SelectDistinct(t, almgr, pmgr, _rel, False, _appendSecong, _mgr.GetFilterInfo, Nothing))
            End Sub

            Protected Overrides Function AppendWhere() As IFilter
                Dim s As IObjectSchemaBase = Mgr.MappingEngine.GetObjectSchema(_rel.Type)
                Dim cs As IContextObjectSchema = TryCast(s, IContextObjectSchema)
                If cs IsNot Nothing Then
                    Return CType(cs.GetContextFilter(Mgr.GetFilterInfo), IFilter)
                Else
                    Return Nothing
                End If
            End Function

        End Class

        Protected Class M2MDataProvider(Of T As {New, IOrmBase})
            Inherits BaseDataProvider(Of T)

            Private _obj As _IOrmBase
            Private _direct As String
            'Private _sync As String
            'Private _rev As Boolean
            'Private _soft_renew As Boolean
            Private _qa() As QueryAspect

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal obj As _IOrmBase, ByVal filter As IFilter, _
                ByVal sort As Sort, ByVal queryAscpect() As QueryAspect, _
                ByVal id As String, ByVal key As String, ByVal direct As String)
                MyBase.New(mgr, filter, sort, key, id)
                _obj = obj
                _direct = direct
                _qa = queryAscpect
                '_sync = sync & OrmManager.GetTablePostfix
                '_rev = rev
            End Sub

            Public Overrides Function GetValues(ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Throw New NotSupportedException
            End Function

            Protected Function GetValuesInternal(ByVal withLoad As Boolean) As System.Collections.Generic.IList(Of Object)
                Dim t As Type = GetType(T)

                Using cmd As System.Data.Common.DbCommand = _mgr.CreateDBCommand
                    With cmd
                        Dim params As IList(Of System.Data.Common.DbParameter) = Nothing
                        Dim almgr As AliasMgr = AliasMgr.Create

                        Dim sb As New StringBuilder
                        sb.Append(_mgr.SQLGenerator.SelectM2M(almgr, _obj, t, _f, _mgr.GetFilterInfo, True, withLoad, _sort IsNot Nothing, params, _direct, _qa))

                        If _sort IsNot Nothing AndAlso Not _sort.IsExternal Then
                            _mgr.SQLGenerator.AppendOrder(t, _sort, almgr, sb, True, Nothing, Nothing)
                        End If

                        .CommandText = sb.ToString
                        For Each p As System.Data.Common.DbParameter In params
                            .Parameters.Add(p)
                        Next
                    End With
                    Dim arr As Generic.IList(Of ColumnAttribute) = Nothing
                    If withLoad Then
                        arr = _mgr.SQLGenerator.GetSortedFieldList(t)
                    End If
                    Return _mgr.LoadM2M(Of T)(cmd, withLoad, _obj, _sort, arr)
                    '              Dim b As ConnAction = _mgr.TestConn(cmd)
                    '              Try
                    '                  If withLoad Then
                    '                      _mgr._cache.BeginTrackDelete(t)
                    '                  End If
                    '                  _mgr._loadedInLastFetch = 0
                    'Dim et As New PerfCounter
                    'Dim l As New List(Of Integer)
                    '                  Using dr As System.Data.IDataReader = cmd.ExecuteReader
                    '                      _mgr._exec = et.GetTime                            
                    '                      Dim ft As New PerfCounter
                    '                      Do While dr.Read
                    '                          Dim id1 As Integer = CInt(dr.GetValue(0))
                    '                          If id1 <> _obj.Identifier Then
                    '                              Throw New OrmManagerException("Wrong relation statement")
                    '                          End If
                    '                          Dim id2 As Integer = CInt(dr.GetValue(1))
                    '                          l.Add(id2)
                    '                          If withLoad AndAlso Not _mgr._cache.IsDeleted(t, id2) Then
                    '                              Dim obj As T = _mgr.CreateDBObject(Of T)(id2)
                    '                              If obj.ObjectState <> ObjectState.Modified Then
                    '                                  Using obj.GetSyncRoot()
                    '                                      'If obj.IsLoaded Then obj.IsLoaded = False
                    '                                      _mgr.LoadFromDataReader(obj, dr, arr, False, 2)
                    '                                      If obj.ObjectState = ObjectState.NotLoaded AndAlso obj.IsLoaded Then obj.ObjectState = ObjectState.None
                    '                                      _mgr._loadedInLastFetch += 1
                    '                                  End Using
                    '                              End If
                    '                          End If
                    '                      Loop
                    '                      _mgr._fetch = ft.GetTime
                    '                  End Using

                    'If _sort IsNot Nothing AndAlso _sort.IsExternal Then
                    '	Dim l2 As New List(Of Integer)
                    '	For Each o As T In _mgr.DbSchema.ExternalSort(Of T)(_mgr, _sort, _mgr.ConvertIds2Objects(Of T)(l, False))
                    '		l2.Add(o.Identifier)
                    '	Next
                    '	l = l2
                    'End If
                    'Return l
                    '              Finally
                    '                  If withLoad Then
                    '                      _mgr._cache.EndTrackDelete(t)
                    '                  End If
                    '                  _mgr.CloseConn(b)
                    '              End Try
                End Using
            End Function

            Public Overrides Function GetCacheItem(ByVal withLoad As Boolean) As OrmManager.CachedItem
                Dim mt As Type = _obj.GetType
                Dim t As Type = GetType(T)
                Dim ct As Type = _mgr.SQLGenerator.GetConnectedType(mt, t)
                If ct IsNot Nothing Then
                    'If Not _direct Then
                    '    Throw New NotSupportedException("Tag is not supported with connected type")
                    'End If
                    Dim f1 As String = _mgr.SQLGenerator.GetConnectedTypeField(ct, mt, M2MRelation.GetRevKey(_direct))
                    Dim f2 As String = _mgr.SQLGenerator.GetConnectedTypeField(ct, t, _direct)
                    Dim fl As New Worm.Database.Criteria.Core.EntityFilter(ct, f1, New EntityValue(_obj), Worm.Criteria.FilterOperation.Equal)
                    Dim l As New List(Of Object)
                    'Dim external_sort As Boolean = False

                    'If Not String.IsNullOrEmpty(_sort) AndAlso _mgr.DbSchema.GetObjectSchema(t).IsExternalSort(_sort) Then
                    '    external_sort = True
                    'End If
                    Dim oschema As IObjectSchemaBase = _mgr.MappingEngine.GetObjectSchema(ct)
                    For Each o As IOrmBase In _mgr.FindConnected(ct, t, mt, fl, Filter, withLoad, _sort, _qa)
                        'Dim id1 As Integer = CType(_mgr.DbSchema.GetFieldValue(o, f1), OrmBase).Identifier
                        'Dim id2 As Integer = CType(_mgr.DbSchema.GetFieldValue(o, f2), OrmBase).Identifier
                        Dim id1 As Object = CType(o.GetValueOptimized(Nothing, f1, oschema), IOrmBase).Identifier
                        Dim id2 As Object = CType(o.GetValueOptimized(Nothing, f2, oschema), IOrmBase).Identifier

                        If Not id1.Equals(_obj.Identifier) Then
                            Throw New OrmManagerException("Wrong relation statement")
                        End If

                        l.Add(id2)
                    Next

                    If _sort IsNot Nothing AndAlso Sort.IsExternal Then
                        Dim l2 As New List(Of Object)
                        For Each o As T In _mgr.SQLGenerator.ExternalSort(Of T)(_mgr, _sort, _mgr.ConvertIds2Objects(Of T)(l, False).List)
                            l2.Add(o.Identifier)
                        Next
                        l = l2
                    End If

                    Dim c As OrmCache = TryCast(_mgr.Cache, OrmCache)
                    If c IsNot Nothing AndAlso Not _mgr._dont_cache_lists Then
                        c.AddConnectedDepend(ct, _key, _id)
                    End If

                    Return New M2MCache(_sort, _f, _obj.Identifier, l, _mgr, mt, t, _direct)
                Else
                    Return New M2MCache(_sort, _f, _obj.Identifier, GetValuesInternal(withLoad), _mgr, mt, t, _direct)
                End If
            End Function

            Public Overrides Function GetCacheItem(ByVal col As ReadOnlyEntityList(Of T)) As OrmManager.CachedItem
                Dim ids As New List(Of Object)
                For Each o As T In col
                    ids.Add(o.Identifier)
                Next
                Return New M2MCache(_sort, _f, _obj.Identifier, ids, _mgr, _obj.GetType, GetType(T), _direct)
            End Function

            Public Overrides Sub CreateDepends()
                If Not _mgr._dont_cache_lists Then
                    Dim tt As Type = GetType(T)
                    Dim cache As OrmCache = TryCast(_mgr.Cache, OrmCache)
                    If _f IsNot Nothing AndAlso cache IsNot Nothing Then
                        'cache.AddDependType(tt, _key, _id, _f)

                        For Each bf As IFilter In _f.GetAllFilters
                            Dim f As IEntityFilter = TryCast(bf, IEntityFilter)
                            If f IsNot Nothing Then
                                Dim v As EntityValue = TryCast(CType(f, EntityFilterBase).Value, EntityValue)
                                Dim tmpl As OrmFilterTemplateBase = CType(f.Template, OrmFilterTemplateBase)
                                If tmpl.Type Is Nothing Then
                                    Throw New NullReferenceException("Type for OrmFilterTemplate must be specified")
                                End If
                                If v IsNot Nothing Then
                                    'Dim tp As Type = f.Value.GetType 'Schema.GetFieldTypeByName(f.Type, f.FieldName)
                                    'If GetType(OrmBase).IsAssignableFrom(tp) Then
                                    cache.AddDepend(v.GetOrmValue(_mgr), _key, _id)
                                    'End If
                                Else
                                    Dim p As New Pair(Of String, Type)(tmpl.PropertyAlias, tmpl.Type)
                                    cache.AddFieldDepend(p, _key, _id)
                                End If
                                'If tt IsNot f.Template.Type Then
                                '    cache.AddJoinDepend(f.Template.Type, tt)
                                'End If
                            End If
                        Next
                    End If

                    Dim mt As Type = _obj.GetType
                    Dim ct As Type = _mgr.SQLGenerator.GetConnectedType(mt, tt)
                    If ct Is Nothing Then
                        _mgr.Cache.AddM2MObjDependent(_obj, _key, _id)
                    End If
                End If

            End Sub

            'Public Overrides Function Validate(ByVal ce As OrmManager.CachedItem) As Boolean
            '    Dim m As M2MCache = CType(ce, M2MCache)
            'End Function
        End Class
    End Class

End Namespace