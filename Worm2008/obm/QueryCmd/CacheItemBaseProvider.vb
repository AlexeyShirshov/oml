Imports System.Collections.Generic
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.OrmManager
Imports Worm.Criteria.Joins
Imports Worm.Sorting
Imports Worm.Cache

Namespace Query
    Public MustInherit Class CacheItemBaseProvider
        Implements ICacheItemProvoderBase

        Private _created As Boolean
        Private _renew As Boolean

        Private _m As Guid
        Private _sm As Guid

        Protected Sub New()

        End Sub

        Protected _mgr As OrmManager
        'Protected _j As List(Of List(Of Worm.Criteria.Joins.QueryJoin))
        'Protected _f() As IFilter
        'Protected _sl As List(Of List(Of SelectExpression))
        Protected _q As QueryCmd
        Protected _key As String
        Protected _id As String
        Protected _sync As String
        Protected _dic As IDictionary
        Private _dp() As Cache.IDependentTypes

        Public Sub SetMark(ByVal q As QueryCmd)
            _m = q.Mark
            _sm = q.SMark
        End Sub

        Public ReadOnly Property QMark() As Guid
            Get
                Return _m
            End Get
        End Property

        Public ReadOnly Property QSMark() As Guid
            Get
                Return _sm
            End Get
        End Property

        Public Sub New(ByVal mgr As OrmManager, ByVal q As QueryCmd)
            Reset(mgr, q)
        End Sub

        'Protected Sub New(ByVal mgr As OrmManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.QueryJoin)), _
        '    ByVal f() As IFilter, ByVal q As QueryCmd, ByVal t As Type, ByVal sl As List(Of List(Of SelectExpression)))

        '    Reset(mgr, j, f, sl, q)
        'End Sub

        Public Overridable Property Created() As Boolean Implements ICacheItemProvoderBase.Created
            Get
                Return _created
            End Get
            Set(ByVal Value As Boolean)
                _created = Value
            End Set
        End Property

        Public Sub CreateDepends(ByVal ce As CachedItemBase) Implements OrmManager.ICacheItemProvoderBase.CreateDepends
            Dim uce As UpdatableCachedItem = TryCast(ce, UpdatableCachedItem)
            If uce IsNot Nothing Then
                If _q.Filter IsNot Nothing Then
                    uce.Filter = _q.Filter.Filter
                End If
                uce.Sort = _q.propSort
            End If

            Dim cache As Cache.OrmCache = TryCast(Me.Cache, Cache.OrmCache)

            If cache IsNot Nothing AndAlso Not String.IsNullOrEmpty(_key) Then
                'Dim notPreciseDependsAD As Boolean
                'Dim notPreciseDependsU As Boolean
                'If _j IsNot Nothing Then
                '    For Each js As List(Of Worm.Criteria.Joins.QueryJoin) In _j
                '        For Each j As QueryJoin In js
                '            If j.ObjectSource IsNot Nothing Then
                '                Dim jt As Type = j.ObjectSource.GetRealType(MappingEngine)
                '                'notPreciseDependsAD = True
                '                'notPreciseDependsU = True
                '                '_mgr.Cache.AddFilterlessDependType(_mgr.GetFilterInfo, j.Type, _key, _id, _mgr.MappingEngine)
                '                cache.validate_AddDeleteType(New Type() {jt}, _key, _id)
                '                cache.validate_UpdateType(New Type() {jt}, _key, _id)
                '            End If
                '            'If j.Type IsNot Nothing Then
                '            '    notPreciseDependsAD = True
                '            '    notPreciseDependsU = True
                '            '    '_mgr.Cache.AddFilterlessDependType(_mgr.GetFilterInfo, j.Type, _key, _id, _mgr.MappingEngine)
                '            '    cache.validate_AddDeleteType(j.Type, _key, _id)
                '            '    cache.validate_UpdateType(j.Type, _key, _id)
                '            'ElseIf Not String.IsNullOrEmpty(j.EntityName) Then
                '            '    notPreciseDependsAD = True
                '            '    notPreciseDependsU = True
                '            '    '_mgr.Cache.AddFilterlessDependType(_mgr.GetFilterInfo, _mgr.MappingEngine.GetTypeByEntityName(j.EntityName), _key, _id, _mgr.MappingEngine)
                '            '    cache.validate_AddDeleteType(MappingEngine.GetTypeByEntityName(j.EntityName), _key, _id)
                '            '    cache.validate_UpdateType(MappingEngine.GetTypeByEntityName(j.EntityName), _key, _id)
                '            'End If
                '        Next
                '    Next
                'End If

                'Dim i As Integer = 0
                'For Each q As QueryCmd In New QueryIterator(_q)
                '    CreateDepends(q, i)
                '    i += 1
                'Next
                'Dim types As IEnumerable(Of Type)
                'Dim rightType As Boolean = _q.GetSelectedTypes(MappingEngine, types)
                Dim ldp As New List(Of Cache.IDependentTypes)
                Dim i As Integer = 0
                For Each q As QueryCmd In New MetaDataQueryIterator(_q)
                    For Each j As QueryJoin In q._js
                        If j.ObjectSource IsNot Nothing Then
                            Dim jt As Type = j.ObjectSource.GetRealType(MappingEngine)
                            cache.validate_AddDeleteType(New Type() {jt}, _key, _id)
                            cache.validate_UpdateType(New Type() {jt}, _key, _id)
                        End If
                    Next

                    Dim dp As Cache.IDependentTypes = CType(q, Cache.IQueryDependentTypes).Get(MappingEngine)

                    ldp.Add(dp)

                    Worm.Cache.Add2Cache(cache, dp, _key, _id)

                    Dim types As ICollection(Of Type) = Nothing
                    Dim rightType As Boolean = q.GetSelectedTypes(MappingEngine, types)

                    'If _f IsNot Nothing AndAlso _f.Length > i Then
                    'Dim vf As IValuableFilter = TryCast(_f(i), IValuableFilter)
                    Dim vf As IValuableFilter = TryCast(_q._f, IValuableFilter)
                    If vf IsNot Nothing Then
                        Dim v As Criteria.Values.EntityValue = TryCast(vf.Value, Criteria.Values.EntityValue)
                        If v IsNot Nothing Then
                            cache.validate_AddDependentObject(v.GetOrmValue(_mgr), _key, _id)
                        End If
                    End If
                    'End If

                    'Dim ef As IEntityFilter = Nothing
                    'If _f IsNot Nothing AndAlso _f.Length > i Then
                    '    ef = TryCast(_f(i), IEntityFilter)
                    'End If

                    If cache.ValidateBehavior = Worm.Cache.ValidateBehavior.Immediate Then
                        'notPreciseDependsAD = notPreciseDependsAD OrElse Not Worm.Cache.IsEmpty(dp)
                        'notPreciseDependsU = notPreciseDependsAD
                        'If _f IsNot Nothing AndAlso _f.Length > i Then
                        Dim fl As IFilter = _q._f
                        Dim added As Boolean = False
                        If rightType Then
                            added = cache.validate_AddCalculatedType(types, _key, _id, fl, MappingEngine, Mgr.GetContextFilter)
                        End If

                        If Not added AndAlso fl IsNot Nothing Then
                            For Each fff As IFilter In fl.GetAllFilters
                                Dim ef As IEntityFilter = TryCast(fff, IEntityFilter)

                                If ef Is Nothing Then
                                    If rightType Then
                                        cache.validate_AddDeleteType(types, _key, _id)
                                        cache.validate_UpdateType(types, _key, _id)
                                        'notPreciseDependsAD = True
                                        'notPreciseDependsU = True
                                    End If
                                Else
                                    Dim tmpl As OrmFilterTemplate = CType(ef.Template, OrmFilterTemplate)
                                    Dim t As Type = tmpl.ObjectSource.GetRealType(MappingEngine)
                                    If t Is Nothing Then
                                        Throw New NullReferenceException("Type or entity name for OrmFilterTemplate must be specified")
                                    End If

                                    'Dim t As Type = tmpl.Type
                                    'If t Is Nothing Then
                                    '    t = MappingEngine.GetTypeByEntityName(tmpl.EntityName)
                                    'End If

                                    Dim p As New Pair(Of String, Type)(tmpl.PropertyAlias, t)
                                    cache.validate_AddDependentFilterField(p, _key, _id)
                                End If
                            Next
                        End If
                        'ElseIf rightType Then
                        '    cache.validate_AddDeleteType(types, _key, _id)
                        'End If
                    Else
                        If rightType Then
                            cache.validate_AddDeleteType(types, _key, _id)
                            'notPreciseDependsAD = True
                        End If

                        'If _f IsNot Nothing AndAlso _f.Length > i Then
                        Dim fl As IFilter = _q._f
                        If fl IsNot Nothing Then
                            For Each fff As IFilter In fl.GetAllFilters
                                Dim ef As IEntityFilter = TryCast(fff, IEntityFilter)

                                If ef Is Nothing Then
                                    If rightType Then
                                        cache.validate_UpdateType(types, _key, _id)
                                        'notPreciseDependsU = True
                                    End If
                                Else
                                    Dim tmpl As OrmFilterTemplate = CType(ef.Template, OrmFilterTemplate)
                                    Dim rt As Type = tmpl.ObjectSource.GetRealType(MappingEngine)

                                    If rt Is Nothing Then
                                        Throw New NullReferenceException("Type for OrmFilterTemplate must be specified")
                                    End If

                                    Dim p As New Pair(Of String, Type)(tmpl.PropertyAlias, rt)
                                    cache.validate_AddDependentFilterField(p, _key, _id)
                                End If
                            Next
                        End If
                    End If

                    For Each s As Sort In New Sort.Iterator(q.propSort)
                        If Not String.IsNullOrEmpty(s.SortBy) Then
                            Dim t As Type = Nothing
                            If s.ObjectSource IsNot Nothing Then
                                t = s.ObjectSource.GetRealType(MappingEngine)
                            End If

                            If t Is Nothing Then
                                If rightType Then
                                    cache.validate_UpdateType(types, _key, _id)
                                    'notPreciseDependsU = True
                                End If
                            Else
                                Dim p As New Pair(Of String, Type)(s.SortBy, t)
                                cache.validate_AddDependentSortField(p, _key, _id)
                            End If
                        ElseIf rightType Then
                            cache.validate_UpdateType(types, _key, _id)
                            'notPreciseDependsU = True
                        End If
                    Next

                    If q.Group IsNot Nothing Then
                        For Each g As Grouping In q.Group
                            If Not String.IsNullOrEmpty(g.PropertyAlias) Then
                                Dim p As New Pair(Of String, Type)(g.PropertyAlias, g.ObjectSource.GetRealType(MappingEngine))
                                cache.validate_AddDependentGroupField(p, _key, _id)
                            ElseIf rightType Then
                                cache.validate_UpdateType(types, _key, _id)
                                'notPreciseDependsU = True
                            End If
                        Next
                    End If

                    If q.Obj IsNot Nothing Then
                        Debug.Assert(types IsNot Nothing AndAlso CType(types, IList(Of Type)).Count = 1)
                        cache.AddM2MSimpleQuery(CType(q.Obj.GetRelation(CType(types, IList(Of Type))(0), q.M2MKey), M2MRelation), _key, _id)
                    End If

                    'If Not (Cache.IsCalculated(dp) OrElse notPreciseDepends) Then
                    '    Dim ef As IEntityFilter = TryCast(_f(i), IEntityFilter)

                    'End If
                    i += 1
                Next

                _dp = ldp.ToArray
            End If
        End Sub

        Public ReadOnly Property Filter() As Criteria.Core.IFilter Implements OrmManager.ICacheItemProvoderBase.Filter
            Get
                Throw New NotSupportedException
                'Return _f(0)
            End Get
        End Property

        Public MustOverride Function GetCacheItem(ByVal ctx As TypeWrap(Of Object)) As CachedItemBase Implements OrmManager.ICacheItemProvoderBase.GetCacheItem
        Public MustOverride Sub Reset(ByVal mgr As OrmManager, ByVal q As QueryCmd)

        Protected Function GetExternalDic(ByVal key As String) As IDictionary
            Dim args As New QueryCmd.ExternalDictionaryEventArgs(key)
            _q.RaiseExternalDictionary(args)
            Return args.Dictionary
        End Function

        Public Sub Clear()
            _mgr = Nothing
            _renew = False
            _q = Nothing
        End Sub

        Public Sub Init(ByVal mgr As OrmManager, ByVal query As QueryCmd)
            _mgr = mgr
            _q = query
        End Sub

        Public ReadOnly Property Mgr() As OrmManager
            Get
                Return _mgr
            End Get
            'Protected Friend Set(ByVal value As OrmManager)
            '    _mgr = value
            'End Set
        End Property

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

        Protected ReadOnly Property Cache() As Cache.CacheBase
            Get
                Return _mgr.Cache
            End Get
        End Property

        Protected ReadOnly Property MappingEngine() As ObjectMappingEngine
            Get
                Return _mgr.MappingEngine
            End Get
        End Property

        Public Overridable Property Renew() As Boolean Implements ICacheItemProvoderBase.Renew
            Get
                Return _renew
            End Get
            Set(ByVal Value As Boolean)
                _renew = Value
            End Set
        End Property

        Public Overridable ReadOnly Property SmartSort() As Boolean Implements ICacheItemProvoderBase.SmartSort
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property Sort() As Sort Implements OrmManager.ICacheItemProvoderBase.Sort
            Get
                Return _q.propSort
            End Get
        End Property

        Public ReadOnly Property Group() As ObjectModel.ReadOnlyCollection(Of Grouping)
            Get
                Return _q.Group
            End Get
        End Property

        'Public ReadOnly Property SelectedType() As Type
        '    Get
        '        Return _q.SelectedType
        '    End Get
        'End Property

        Protected Function ValidateFromCache() As Boolean
            Dim cache As Cache.OrmCache = TryCast(Mgr.Cache, Cache.OrmCache)
            If cache IsNot Nothing AndAlso cache.ValidateBehavior = Worm.Cache.ValidateBehavior.Deferred Then
                Dim l As New List(Of Type)

                'If _j IsNot Nothing Then
                'For Each js As List(Of Worm.Criteria.Joins.QueryJoin) In _j
                For Each j As QueryJoin In _q._js
                    l.Add(j.ObjectSource.GetRealType(MappingEngine))
                    'If j.Type IsNot Nothing Then
                    '    l.Add(j.Type)
                    'ElseIf Not String.IsNullOrEmpty(j.EntityName) Then
                    '    l.Add(MappingEngine.GetTypeByEntityName(j.EntityName))
                    'End If
                Next
                'Next
                'End If

                'For Each q As QueryCmd In New QueryIterator(_q)
                '    Dim dp As Cache.IDependentTypes = q.Get(MappingEngine)
                For Each dp As Cache.IDependentTypes In _dp
                    If Not Worm.Cache.IsEmpty(dp) Then
                        l.AddRange(dp.GetAddDelete)
                        l.AddRange(dp.GetUpdate)
                    End If
                Next

                'If SelectedType IsNot Nothing AndAlso GetType(_ICachedEntity).IsAssignableFrom(SelectedType) Then
                '    l.Add(SelectedType)
                'End If
                Dim el As ICollection(Of Type) = Nothing
                If _q.GetSelectedTypes(Mgr.MappingEngine, el) Then
                    l.AddRange(el)
                End If

                Dim f As IEntityFilter = TryCast(_q._f, IEntityFilter)
                'If _f IsNot Nothing AndAlso _f.Length > 0 Then
                '    f = TryCast(_f(0), IEntityFilter)
                'End If

                Return cache.UpdateCacheDeferred(Mgr.MappingEngine, l, f, Sort, Group)
            End If
            Return True
        End Function
    End Class
End Namespace