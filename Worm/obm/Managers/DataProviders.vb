Imports Worm
Imports System.Collections.Generic
Imports CoreFramework.Structures
Imports CoreFramework.Threading

Namespace Orm

    Partial Public Class OrmReadOnlyDBManager

        Protected MustInherit Class BaseDataProvider(Of T As {New, OrmBase})
            Inherits CustDelegate(Of T)
            Implements ICacheValidator

            Protected _f As IOrmFilter
            Protected _sort As Sort
            Protected _mgr As OrmReadOnlyDBManager
            Protected _key As String
            Protected _id As String

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal f As IOrmFilter, _
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
                    For Each f As OrmFilter In _f.GetAllFilters
                        Dim fields As List(Of String) = Nothing
                        If _mgr.Cache.GetUpdatedFields(f.Type, fields) Then
                            Dim idx As Integer = fields.IndexOf(f.FieldName)
                            If idx >= 0 Then
                                Dim p As New Pair(Of String, Type)(f.FieldName, f.Type)
                                _mgr.Cache.ResetFieldDepends(p)
                                _mgr.Cache.RemoveUpdatedFields(f.Type, f.FieldName)
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
                    _mgr.Cache.AddDependType(GetType(T), _key, _id)

                    If _f IsNot Nothing Then
                        For Each f As OrmFilter In _f.GetAllFilters
                            If f.IsParamOrm Then
                                'Dim tp As Type = f.Value.GetType 'Schema.GetFieldTypeByName(f.Type, f.FieldName)
                                'If GetType(OrmBase).IsAssignableFrom(tp) Then
                                _mgr.Cache.AddDepend(f.ValueOrm(_mgr), _key, _id)
                                'End If
                            Else
                                Dim p As New Pair(Of String, Type)(f.FieldName, f.Type)
                                _mgr.Cache.AddFieldDepend(p, _key, _id)
                            End If
                        Next
                    End If
                End If
            End Sub

            Public Overrides ReadOnly Property Filter() As IOrmFilter
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

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal f As IOrmFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String)
                MyBase.New(mgr, f, sort, key, id)
            End Sub

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal f As IOrmFilter, ByVal cols As List(Of ColumnAttribute), _
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

                        Dim c As New OrmCondition.OrmConditionConstructor
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

            Protected Overridable Function AppendWhere() As IOrmFilter
                Return Nothing
            End Function

            Protected Overridable Sub AppendSelect(ByVal sb As StringBuilder, ByVal t As Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As IList(Of ColumnAttribute))
                sb.Append(_mgr.DbSchema.Select(t, almgr, pmgr, arr))
            End Sub

            Protected Overridable Sub AppendSelectID(ByVal sb As StringBuilder, ByVal t As Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As IList(Of ColumnAttribute))
                sb.Append(_mgr.DbSchema.SelectID(t, almgr, pmgr))
            End Sub
        End Class

        Protected Class FilterCustDelegate4Top(Of T As {New, OrmBase})
            Inherits FilterCustDelegate(Of T)

            Private _top As Integer

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal top As Integer, ByVal f As IOrmFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String)
                MyBase.New(mgr, f, sort, key, id)
                _top = top
            End Sub

            Protected Overrides Sub AppendSelect(ByVal sb As System.Text.StringBuilder, ByVal t As System.Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As System.Collections.Generic.IList(Of ColumnAttribute))
                sb.Append(Schema.SelectTop(_top, t, almgr, pmgr, arr))
            End Sub

            Protected Overrides Sub AppendSelectID(ByVal sb As System.Text.StringBuilder, ByVal t As System.Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As System.Collections.Generic.IList(Of ColumnAttribute))
                sb.Append(Schema.SelectIDTop(_top, t, almgr, pmgr))
            End Sub
        End Class

        '        Protected Class M2MCustDelegate(Of T As {New, OrmBase})
        '            Inherits BaseDataProvider(Of T)

        '            Private _obj As OrmBase
        '            Private _sync As String
        '            Private _rev As Boolean
        '            Private _soft_renew As Boolean

        '            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal obj As OrmBase, ByVal filter As IOrmFilter, ByVal sort As String, ByVal st As SortType, _
        '                ByVal id As String, ByVal sync As String, ByVal key As String, ByVal rev As Boolean)
        '                MyBase.New(mgr, filter, sort, st, key, id)
        '                _obj = obj
        '                _sync = sync & OrmManagerBase.GetTablePostfix
        '                _rev = rev
        '            End Sub

        '            Public Overrides Function GetValues(ByVal withLoad As Boolean) As System.Collections.Generic.ICollection(Of T)
        '                Dim type As Type = GetType(T)
        '                Dim dt As System.Data.DataTable = _mgr.GetDataTable(_id, _key & OrmManagerBase.GetTablePostfix, _sync, type, _obj, _f, _rev, True, Renew And Not _soft_renew)
        '                Dim id_clm As String = type.Name & "ID"
        '                If type Is _obj.GetType Then
        '                    id_clm = id_clm & "Rev"
        '                End If
        '                Dim forse_load As Boolean = False
        '                Dim loaded As Integer = 0, not_loaded As Integer = 0
        'l1:
        '                Dim present As List(Of T) = New List(Of T)
        '                Dim load_ids As New Collections.IntList

        '                Using SyncHelper.AcquireDynamicLock(_sync)
        '                    Dim dic_ids As New Specialized.OrderedDictionary

        '                    For Each dr As System.Data.DataRow In dt.Rows
        '                        If dr.RowState = System.Data.DataRowState.Deleted Then Continue For
        '                        Dim id As Integer = CInt(dr(id_clm))
        '                        If forse_load Then
        '                            If Not dic_ids.Contains(id) Then
        '                                dic_ids.Add(id, Nothing)
        '                                not_loaded += 1
        '                            End If
        '                            load_ids.Append(id)
        '                        Else
        '                            Dim dic As IDictionary(Of Integer, T) = _mgr.GetDictionary(Of T)()

        '                            Dim obj As T = Nothing
        '                            If Not dic.TryGetValue(id, obj) Then
        '                                If _mgr.NewObjectManager IsNot Nothing Then
        '                                    obj = CType(_mgr.NewObjectManager.GetNew(type, id), T)
        '                                End If

        '                                If obj Is Nothing Then
        '                                    load_ids.Append(id)
        '                                Else
        '                                    present.Add(obj)
        '                                    'If obj.IsLoaded Then loaded += 1
        '                                End If
        '                            Else
        '                                present.Add(obj)
        '                                'If obj.IsLoaded Then loaded += 1
        '                            End If

        '                            If Not dic_ids.Contains(id) Then
        '                                dic_ids.Add(id, Nothing)

        '                                If obj Is Nothing Then
        '                                    not_loaded += 1
        '                                ElseIf obj.IsLoaded Then
        '                                    loaded += 1
        '                                End If
        '                                'Else
        '                                'Throw New OrmManagerException(String.Format("Many to many relation is not seems to be unique. Relation {0} to {1}. Duplicate value {2}:{3}.", type, _obj.GetType, id_clm, id))
        '                            End If
        '                        End If
        '                    Next
        '                End Using

        '                If load_ids.Count > 0 Then
        '                    Dim sort_on_client As Boolean = not_loaded < 10
        '                    If Not sort_on_client Then
        '                        For Each o As OrmBase In present
        '                            If o.ObjectState <> ObjectState.Created Then load_ids.Append(o.Identifier)
        '                        Next
        '                    End If

        '                    Dim loaded_objects As New List(Of T)

        '                    If String.IsNullOrEmpty(_sort) Then
        '                        loaded_objects.AddRange(_mgr.ConvertIds2Objects(Of T)(load_ids.Ints, False))
        '                        If withLoad Then
        '                            _mgr.LoadObjects(loaded_objects)
        '                        End If

        '                        For Each o As T In present
        '                            If o.ObjectState = ObjectState.Created OrElse sort_on_client Then
        '                                loaded_objects.Add(o)
        '                            End If
        '                        Next

        '                        present = loaded_objects
        '                    Else
        '                        _mgr.GetObjects(Of T)(load_ids.Ints, Nothing, loaded_objects, sort_on_client OrElse withLoad, "ID", False)

        '                        If sort_on_client Then
        '                            loaded_objects.AddRange(present)
        '                        Else
        '                            For Each o As T In present
        '                                If o.ObjectState = ObjectState.Created Then
        '                                    loaded_objects.Add(o)
        '                                End If
        '                            Next
        '                        End If

        '                        If _mgr.Schema.IsExternalSort(_sort, type) Then
        '                            present = _mgr.Schema.ExternalSort(Of T)(_sort, _st, loaded_objects)
        '                        Else
        '                            If sort_on_client Then
        '                                CType(loaded_objects, List(Of T)).Sort(CType(loaded_objects(0), OrmBase).CreateSortComparer(Of T)(_sort, _st))
        '                            End If
        '                            present = loaded_objects
        '                        End If
        '                    End If
        '                ElseIf Not String.IsNullOrEmpty(_sort) Then
        '                    If _mgr.Schema.IsExternalSort(_sort, type) Then
        '                        present = _mgr.Schema.ExternalSort(Of T)(_sort, _st, present)
        '                    ElseIf present.Count > 0 Then
        '                        Dim sort_on_client As Boolean = (dt.Rows.Count - loaded) < 10

        '                        If sort_on_client Then
        '                            CType(present, List(Of T)).Sort(CType(present(0), OrmBase).CreateSortComparer(Of T)(_sort, _st))
        '                        Else
        '                            forse_load = True
        '                            For Each obj As T In New List(Of T)(present)
        '                                If obj.ObjectState <> ObjectState.Created Then present.Remove(obj)
        '                            Next
        '                            GoTo l1
        '                        End If
        '                    End If
        '                End If

        '                Return present
        '            End Function

        '            Public Overrides Function Validate(ByVal ce As OrmManagerBase.CachedItem) As Boolean
        '                Dim tt As System.Type = GetType(T)

        '                'Dim cnt As Type = _mgr.DatabaseSchema.GetConnectedType(_obj.GetType, tt)

        '                'If cnt IsNot Nothing Then
        '                '    If _mgr.Cache.IsAddOrDelete(cnt) Then
        '                '        Return False
        '                '    End If
        '                'End If

        '                Dim dt As System.Data.DataTable = _mgr.GetDataTable(_id, _key & OrmManagerBase.GetTablePostfix, _sync, tt, _obj, _f, _rev, True, False)
        '                Dim existing As ICollection(Of T) = ce.GetObjectList(Of T)(_mgr, False, False)
        '                Using SyncHelper.AcquireDynamicLock(_sync)
        '                    Dim b As Boolean = dt.Select(Nothing, Nothing, System.Data.DataViewRowState.CurrentRows).Length = existing.Count
        '                    If Not b Then
        '                        _soft_renew = True
        '                    End If
        '                    Return b
        '                End Using
        '            End Function

        '            Public Overrides ReadOnly Property SmartSort() As Boolean
        '                Get
        '                    Return False
        '                End Get
        '            End Property

        '            Public Overrides Sub CreateDepends()
        '                Dim tt As System.Type = GetType(T)

        '                _mgr.Cache.AddDependType(tt, _key, _id)
        '                _mgr.Cache.AddDependType(tt, _key & OrmManagerBase.GetTablePostfix, _id)

        '                Dim cnt As Type = _mgr.DatabaseSchema.GetConnectedType(_obj.GetType, tt)
        '                If cnt IsNot Nothing Then
        '                    _mgr.Cache.AddDependType(cnt, _key, _id)
        '                    _mgr.Cache.AddDependType(cnt, _key & OrmManagerBase.GetTablePostfix, _id)
        '                End If

        '                If _f IsNot Nothing Then
        '                    For Each f As OrmFilter In _f.GetAllFilters
        '                        'Dim tp As Type = _mgr.Schema.GetFieldTypeByName(f.Type, f.FieldName)
        '                        If f.IsParamOrm Then 'AndAlso GetType(OrmBase).IsAssignableFrom(tp) Then
        '                            'Dim tp As Type = f.Value.GetType
        '                            Dim k As OrmBase = f.ValueOrm(_mgr) 'CType(f.Value, OrmBase)
        '                            _mgr.Cache.AddDepend(k, _key, _id)
        '                            _mgr.Cache.AddDepend(k, _key & OrmManagerBase.GetTablePostfix, _id)
        '                        Else 'If f.IsParamValue Then
        '                            Dim p As New Pair(Of String, Type)(f.FieldName, f.Type)
        '                            _mgr.Cache.AddFieldDepend(p, _key, _id)
        '                            _mgr.Cache.AddFieldDepend(p, _key & OrmManagerBase.GetTablePostfix, _id)
        '                        End If
        '                    Next
        '                End If
        '            End Sub

        '        End Class

        Protected Class DistinctFilterCustDelegate(Of T As {New, OrmBase})
            Inherits FilterCustDelegate(Of T)

            Private _join() As OrmJoin

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal join() As OrmJoin, ByVal f As IOrmFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String)
                MyBase.New(mgr, f, sort, key, id)
                _join = join
            End Sub

            Protected Overrides Sub AppendSelect(ByVal sb As System.Text.StringBuilder, ByVal t As System.Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As System.Collections.Generic.IList(Of ColumnAttribute))
                sb.Append(Schema.SelectWithJoin(t, almgr, pmgr, _join, True, True, Nothing, arr))
            End Sub

            Protected Overrides Sub AppendSelectID(ByVal sb As System.Text.StringBuilder, ByVal t As System.Type, ByVal almgr As AliasMgr, ByVal pmgr As ParamMgr, ByVal arr As System.Collections.Generic.IList(Of ColumnAttribute))
                sb.Append(Schema.SelectWithJoin(t, almgr, pmgr, _join, False, True, Nothing))
            End Sub
        End Class

        Protected Class DistinctRelationFilterCustDelegate(Of T As {New, OrmBase})
            Inherits FilterCustDelegate(Of T)

            Private _rel As M2MRelation
            Private _appendSecong As Boolean

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal relation As M2MRelation, ByVal f As IOrmFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String)
                MyBase.New(mgr, f, sort, key, id)
                _rel = relation

                If mgr.ObjectSchema.GetObjectSchema(relation.Type).GetFilter(mgr.GetFilterInfo) IsNot Nothing Then
                    _appendSecong = True
                Else
                    If f IsNot Nothing Then
                        For Each fl As OrmFilter In f.GetAllFilters
                            If fl.Type Is relation.Type Then
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

            Protected Overrides Function AppendWhere() As IOrmFilter
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

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal obj As OrmBase, ByVal filter As IOrmFilter, _
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
                        Using dr As System.Data.IDataReader = cmd.ExecuteReader
                            Dim l As New List(Of Integer)
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
                                        End Using
                                    End If
                                End If
                            Loop

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
                    Dim fl As New OrmFilter(ct, f1, _obj, FilterOperation.Equal)
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
                MyBase.CreateDepends()

                If Not _mgr._dont_cache_lists Then
                    Dim mt As Type = _obj.GetType
                    Dim t As Type = GetType(T)
                    Dim ct As Type = _mgr.DbSchema.GetConnectedType(mt, t)
                    If ct Is Nothing Then
                        _mgr.Cache.AddM2MObjDependent(_obj, _key, _id)
                    End If
                End If
            End Sub

            'Public Overrides Function Validate(ByVal ce As OrmManagerBase.CachedItem) As Boolean
            '    Dim m As M2MCache = CType(ce, M2MCache)
            'End Function
        End Class
    End Class

End Namespace