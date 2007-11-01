Imports Worm
Imports System.Collections.Generic
Imports CoreFramework.Structures
Imports CoreFramework.Threading

Namespace Orm

    Partial Public Class OrmReadOnlyDBManager

        Protected MustInherit Class BaseDataProvider(Of T As {New, OrmBase})
            Inherits CustDelegate(Of T)
            Implements ICacheValidator

            Protected _f As IEntityFilter
            Protected _sort As Sort
            Protected _mgr As OrmReadOnlyDBManager
            Protected _key As String
            Protected _id As String

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal f As IEntityFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String)
                _mgr = mgr
                _f = f
                _sort = sort
                '_st = st
                _key = key
                _id = id
            End Sub

            Public Overridable Function Validate() As Boolean Implements OrmManagerBase.ICacheValidator.Validate
                If _f IsNot Nothing Then
                    For Each f As EntityFilter In _f.GetAllFilters
                        Dim fields As List(Of String) = Nothing
                        If _mgr.Cache.GetUpdatedFields(f.Template.Type, fields) Then
                            Dim idx As Integer = fields.IndexOf(f.Template.FieldName)
                            If idx >= 0 Then
                                Dim p As New Pair(Of String, Type)(f.Template.FieldName, f.Template.Type)
                                _mgr.Cache.ResetFieldDepends(p)
                                _mgr.Cache.RemoveUpdatedFields(f.Template.Type, f.Template.FieldName)
                                Return False
                            End If
                        End If
                    Next
                End If
                Return True
            End Function

            Public Overridable Function Validate(ByVal ce As OrmManagerBase.CachedItem) As Boolean Implements OrmManagerBase.ICacheValidator.Validate
                Return True
            End Function

            Public Overrides Sub CreateDepends()
                If Not _mgr._dont_cache_lists Then
                    If _f IsNot Nothing Then
                        Dim tt As System.Type = GetType(T)
                        Dim cache As OrmCacheBase = _mgr.Cache
                        cache.AddDependType(tt, _key, _id, _f)

                        For Each f As EntityFilter In _f.GetAllFilters
                            Dim v As EntityValue = TryCast(f.Value, EntityValue)
                            If v IsNot Nothing Then
                                'Dim tp As Type = f.Value.GetType 'Schema.GetFieldTypeByName(f.Type, f.FieldName)
                                'If GetType(OrmBase).IsAssignableFrom(tp) Then
                                cache.AddDepend(v.GetOrmValue(_mgr), _key, _id)
                                'End If
                            Else
                                Dim p As New Pair(Of String, Type)(f.Template.FieldName, f.Template.Type)
                                cache.AddFieldDepend(p, _key, _id)
                            End If
                            If tt IsNot f.Template.Type Then
                                cache.AddJoinDepend(f.Template.Type, tt)
                            End If
                        Next
                    End If
                End If
            End Sub

            Public Overrides ReadOnly Property Filter() As IEntityFilter
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

            Public Overrides Function GetCacheItem(ByVal withLoad As Boolean) As OrmManagerBase.CachedItem
                Dim sortex As IOrmSortingEx = TryCast(_mgr.ObjectSchema.GetObjectSchema(GetType(T)), IOrmSortingEx)
                Dim s As Date = Nothing
                If sortex IsNot Nothing Then
                    Dim ts As TimeSpan = sortex.SortExpiration(_sort)
                    If ts <> TimeSpan.MaxValue AndAlso ts <> TimeSpan.MinValue Then
                        s = Now.Add(ts)
                    End If
                End If
                Return New CachedItem(_sort, s, _f, GetValues(withLoad), _mgr)
            End Function

            Public Overrides Function GetCacheItem(ByVal col As System.Collections.Generic.ICollection(Of T)) As OrmManagerBase.CachedItem
                Dim sortex As IOrmSortingEx = TryCast(_mgr.ObjectSchema.GetObjectSchema(GetType(T)), IOrmSortingEx)
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

        Protected Class FilterCustDelegate(Of T As {New, OrmBase})
            Inherits BaseDataProvider(Of T)

            Private _cols As List(Of ColumnAttribute)

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal f As IEntityFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String)
                MyBase.New(mgr, f, sort, key, id)
            End Sub

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal f As IEntityFilter, ByVal cols As List(Of ColumnAttribute), _
                ByVal sort As Sort, ByVal key As String, ByVal id As String)
                MyBase.New(mgr, f, sort, key, id)
                _cols = cols
            End Sub

            Public Overrides Function GetValues(ByVal withLoad As Boolean) As Generic.ICollection(Of T)
                Dim original_type As Type = GetType(T)

                Using cmd As System.Data.Common.DbCommand = _mgr.DbSchema.CreateDBCommand
                    Dim arr As Generic.List(Of ColumnAttribute) = _cols

                    With cmd
                        .CommandType = System.Data.CommandType.Text

                        Dim almgr As AliasMgr = AliasMgr.Create
                        Dim params As New ParamMgr(_mgr.DbSchema, "p")
                        Dim sb As New StringBuilder
                        If withLoad Then
                            If arr Is Nothing Then
                                arr = _mgr.ObjectSchema.GetSortedFieldList(original_type)
                            End If
                            AppendSelect(sb, original_type, almgr, params, arr)
                        Else
                            arr = New Generic.List(Of ColumnAttribute)
                            arr.Add(New ColumnAttribute("ID", Field2DbRelations.PK))
                            AppendSelectID(sb, original_type, almgr, params, arr)
                        End If

                        Dim c As New Orm.Condition.ConditionConstructor
                        c.AddFilter(_f)
                        c.AddFilter(AppendWhere)
                        _mgr.DbSchema.AppendWhere(original_type, c.Condition, almgr, sb, _mgr.GetFilterInfo, params)
                        If _sort IsNot Nothing AndAlso Not _sort.IsExternal Then
                            _mgr.DbSchema.AppendOrder(original_type, _sort, almgr, sb)
                        End If

                        params.AppendParams(.Parameters)
                        .CommandText = sb.ToString
                    End With

                    Dim r As ICollection(Of T) = _mgr.LoadMultipleObjects(Of T)(cmd, withLoad, Nothing, arr)
                    If _sort IsNot Nothing AndAlso _sort.IsExternal Then
                        r = _mgr.DbSchema.ExternalSort(Of T)(_sort, r)
                    End If
                    Return r
                End Using
            End Function

            Protected ReadOnly Property Schema() As DbSchema
                Get
                    Return _mgr.DbSchema
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
                sb.Append(_mgr.DbSchema.Select(t, almgr, pmgr, arr))
            End Sub

            Protected Overridable Sub AppendSelectID(ByVal sb As StringBuilder, ByVal t As Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As IList(Of ColumnAttribute))
                sb.Append(_mgr.DbSchema.SelectID(t, almgr, pmgr))
            End Sub
        End Class

        Protected Class JoinCustDelegate(Of T As {New, OrmBase})
            Inherits FilterCustDelegate(Of T)

            Private _join() As OrmJoin
            Private _distinct As Boolean
            Private _top As Integer = -1
            Private _asc() As QueryAspect

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal join() As OrmJoin, ByVal f As IEntityFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String, ByVal distinct As Boolean)
                MyBase.New(mgr, f, sort, key, id)
                _join = join
                If distinct Then
                    _asc = New QueryAspect() {New DistinctAspect()}
                End If
            End Sub

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal join() As OrmJoin, ByVal f As IEntityFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String, ByVal top As Integer)
                MyBase.New(mgr, f, sort, key, id)
                _join = join
                _asc = New QueryAspect() {New TopAspect(top)}
            End Sub

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal join() As OrmJoin, ByVal f As IEntityFilter, _
               ByVal sort As Sort, ByVal key As String, ByVal id As String, ByVal aspect As QueryAspect)
                MyBase.New(mgr, f, sort, key, id)
                _join = join
                If aspect IsNot Nothing Then
                    _asc = New QueryAspect() {aspect}
                End If
            End Sub

            Protected Overrides Sub AppendSelect(ByVal sb As System.Text.StringBuilder, ByVal t As System.Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As System.Collections.Generic.IList(Of ColumnAttribute))
                sb.Append(Schema.SelectWithJoin(t, almgr, pmgr, _join, True, _asc, Nothing, arr))
            End Sub

            Protected Overrides Sub AppendSelectID(ByVal sb As System.Text.StringBuilder, ByVal t As System.Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As System.Collections.Generic.IList(Of ColumnAttribute))
                sb.Append(Schema.SelectWithJoin(t, almgr, pmgr, _join, False, _asc, Nothing))
            End Sub
        End Class

        Protected Class DistinctRelationFilterCustDelegate(Of T As {New, OrmBase})
            Inherits FilterCustDelegate(Of T)

            Private _rel As M2MRelation
            Private _appendSecong As Boolean

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal relation As M2MRelation, ByVal f As IEntityFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String)
                MyBase.New(mgr, f, sort, key, id)
                _rel = relation

                If mgr.ObjectSchema.GetObjectSchema(relation.Type).GetFilter(mgr.GetFilterInfo) IsNot Nothing Then
                    _appendSecong = True
                Else
                    If f IsNot Nothing Then
                        For Each fl As EntityFilter In f.GetAllFilters
                            If fl.Template.Type Is relation.Type Then
                                _appendSecong = True
                                Exit For
                            End If
                        Next
                    End If
                End If
            End Sub

            Protected Overrides Sub AppendSelect(ByVal sb As System.Text.StringBuilder, ByVal t As System.Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As System.Collections.Generic.IList(Of ColumnAttribute))
                sb.Append(Schema.SelectDistinct(t, almgr, pmgr, _rel, True, _appendSecong, arr))
            End Sub

            Protected Overrides Sub AppendSelectID(ByVal sb As System.Text.StringBuilder, ByVal t As System.Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As System.Collections.Generic.IList(Of ColumnAttribute))
                sb.Append(Schema.SelectDistinct(t, almgr, pmgr, _rel, False, _appendSecong))
            End Sub

            Protected Overrides Function AppendWhere() As IFilter
                Return Mgr.ObjectSchema.GetObjectSchema(_rel.Type).GetFilter(Mgr.GetFilterInfo)
            End Function

        End Class

        Protected Class M2MDataProvider(Of T As {New, OrmBase})
            Inherits BaseDataProvider(Of T)

            Private _obj As OrmBase
            Private _direct As Boolean
            'Private _sync As String
            'Private _rev As Boolean
            'Private _soft_renew As Boolean

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal obj As OrmBase, ByVal filter As IEntityFilter, _
                ByVal sort As Sort, _
                ByVal id As String, ByVal key As String, ByVal direct As Boolean)
                MyBase.New(mgr, filter, sort, key, id)
                _obj = obj
                _direct = direct
                '_sync = sync & OrmManagerBase.GetTablePostfix
                '_rev = rev
            End Sub

            Public Overrides Function GetValues(ByVal withLoad As Boolean) As System.Collections.Generic.ICollection(Of T)
                Throw New NotSupportedException
            End Function

            Protected Function GetValuesInternal(ByVal withLoad As Boolean) As System.Collections.Generic.IList(Of Integer)
                Dim t As Type = GetType(T)

                Using cmd As System.Data.Common.DbCommand = _mgr.DbSchema.CreateDBCommand
                    With cmd
                        Dim params As IList(Of System.Data.Common.DbParameter) = Nothing
                        Dim almgr As AliasMgr = AliasMgr.Create

                        Dim sb As New StringBuilder
                        sb.Append(_mgr.DbSchema.SelectM2M(almgr, _obj, t, _f, _mgr.GetFilterInfo, True, withLoad, _sort IsNot Nothing, params, _direct))

                        If _sort IsNot Nothing AndAlso Not _sort.IsExternal Then
                            _mgr.DbSchema.AppendOrder(t, _sort, almgr, sb)
                        End If

                        .CommandText = sb.ToString
                        For Each p As System.Data.Common.DbParameter In params
                            .Parameters.Add(p)
                        Next
                    End With
                    Dim arr As Generic.IList(Of ColumnAttribute) = Nothing
                    If withLoad Then
                        arr = _mgr.DbSchema.GetSortedFieldList(t)
                    End If
                    Dim b As ConnAction = _mgr.TestConn(cmd)
                    Try
                        _mgr._loadedInLastFetch = 0
                        Dim et As New PerfCounter
                        Using dr As System.Data.IDataReader = cmd.ExecuteReader
                            _mgr._exec = et.GetTime
                            Dim l As New List(Of Integer)
                            Dim ft As New PerfCounter
                            Do While dr.Read
                                Dim id1 As Integer = CInt(dr.GetValue(0))
                                If id1 <> _obj.Identifier Then
                                    Throw New OrmManagerException("Wrong relation statement")
                                End If
                                Dim id2 As Integer = CInt(dr.GetValue(1))
                                l.Add(id2)
                                If withLoad Then
                                    Dim obj As T = _mgr.CreateDBObject(Of T)(id2)
                                    If obj.ObjectState <> ObjectState.Modified Then
                                        Using obj.GetSyncRoot()
                                            If obj.IsLoaded Then obj.IsLoaded = False
                                            _mgr.LoadFromDataReader(obj, dr, arr, False, 2)
                                            If obj.ObjectState = ObjectState.NotLoaded Then obj.ObjectState = ObjectState.None
                                            _mgr._loadedInLastFetch += 1
                                        End Using
                                    End If
                                End If
                            Loop
                            _mgr._fetch = ft.GetTime

                            If _sort IsNot Nothing AndAlso _sort.IsExternal Then
                                Dim l2 As New List(Of Integer)
                                For Each o As T In _mgr.DbSchema.ExternalSort(Of T)(_sort, _mgr.ConvertIds2Objects(Of T)(l, False))
                                    l2.Add(o.Identifier)
                                Next
                                l = l2
                            End If
                            Return l
                        End Using
                    Finally
                        _mgr.CloseConn(b)
                    End Try
                End Using
            End Function

            Public Overrides Function GetCacheItem(ByVal withLoad As Boolean) As OrmManagerBase.CachedItem
                Dim mt As Type = _obj.GetType
                Dim t As Type = GetType(T)
                Dim ct As Type = _mgr.DbSchema.GetConnectedType(mt, t)
                If ct IsNot Nothing Then
                    If Not _direct Then
                        Throw New NotSupportedException("Tag is not supported with connected type")
                    End If
                    Dim f1 As String = _mgr.DbSchema.GetConnectedTypeField(ct, mt)
                    Dim f2 As String = _mgr.DbSchema.GetConnectedTypeField(ct, t)
                    Dim fl As New EntityFilter(ct, f1, New EntityValue(_obj), FilterOperation.Equal)
                    Dim l As New List(Of Integer)
                    'Dim external_sort As Boolean = False

                    'If Not String.IsNullOrEmpty(_sort) AndAlso _mgr.DbSchema.GetObjectSchema(t).IsExternalSort(_sort) Then
                    '    external_sort = True
                    'End If

                    For Each o As OrmBase In _mgr.FindConnected(ct, t, mt, fl, Filter, withLoad, _sort)
                        'Dim id1 As Integer = CType(_mgr.DbSchema.GetFieldValue(o, f1), OrmBase).Identifier
                        'Dim id2 As Integer = CType(_mgr.DbSchema.GetFieldValue(o, f2), OrmBase).Identifier
                        Dim id1 As Integer = CType(o.GetValue(f1), OrmBase).Identifier
                        Dim id2 As Integer = CType(o.GetValue(f2), OrmBase).Identifier

                        If id1 <> _obj.Identifier Then
                            Throw New OrmManagerException("Wrong relation statement")
                        End If

                        l.Add(id2)
                    Next

                    If _sort IsNot Nothing AndAlso Sort.IsExternal Then
                        Dim l2 As New List(Of Integer)
                        For Each o As T In _mgr.DbSchema.ExternalSort(Of T)(_sort, _mgr.ConvertIds2Objects(Of T)(l, False))
                            l2.Add(o.Identifier)
                        Next
                        l = l2
                    End If

                    If Not _mgr._dont_cache_lists Then
                        _mgr.Cache.AddConnectedDepend(ct, _key, _id)
                    End If

                    Return New M2MCache(_sort, _f, _obj.Identifier, l, _mgr, mt, t, _direct)
                Else
                    Return New M2MCache(_sort, _f, _obj.Identifier, GetValuesInternal(withLoad), _mgr, mt, t, _direct)
                End If
            End Function

            Public Overrides Function GetCacheItem(ByVal col As System.Collections.Generic.ICollection(Of T)) As OrmManagerBase.CachedItem
                Dim ids As New List(Of Integer)
                For Each o As T In col
                    ids.Add(o.Identifier)
                Next
                Return New M2MCache(_sort, _f, _obj.Identifier, ids, _mgr, _obj.GetType, GetType(T), _direct)
            End Function

            Public Overrides Sub CreateDepends()
                If Not _mgr._dont_cache_lists Then
                    Dim tt As Type = GetType(T)
                    If _f IsNot Nothing Then
                        Dim cache As OrmCacheBase = _mgr.Cache
                        'cache.AddDependType(tt, _key, _id, _f)

                        For Each f As EntityFilter In _f.GetAllFilters
                            Dim v As EntityValue = TryCast(f.Value, EntityValue)
                            If v IsNot Nothing Then
                                'Dim tp As Type = f.Value.GetType 'Schema.GetFieldTypeByName(f.Type, f.FieldName)
                                'If GetType(OrmBase).IsAssignableFrom(tp) Then
                                cache.AddDepend(v.GetOrmValue(_mgr), _key, _id)
                                'End If
                            Else
                                Dim p As New Pair(Of String, Type)(f.Template.FieldName, f.Template.Type)
                                cache.AddFieldDepend(p, _key, _id)
                            End If
                            'If tt IsNot f.Template.Type Then
                            '    cache.AddJoinDepend(f.Template.Type, tt)
                            'End If
                        Next
                    End If

                    Dim mt As Type = _obj.GetType
                    Dim ct As Type = _mgr.DbSchema.GetConnectedType(mt, tt)
                    If ct Is Nothing Then
                        _mgr.Cache.AddM2MObjDependent(_obj, _key, _id)
                    End If
                End If

                If Not _mgr._dont_cache_lists Then
                End If
            End Sub

            'Public Overrides Function Validate(ByVal ce As OrmManagerBase.CachedItem) As Boolean
            '    Dim m As M2MCache = CType(ce, M2MCache)
            'End Function
        End Class
    End Class

End Namespace