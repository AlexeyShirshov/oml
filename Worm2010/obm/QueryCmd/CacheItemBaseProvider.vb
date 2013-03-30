Imports System.Collections.Generic
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.OrmManager
Imports Worm.Criteria.Joins
Imports Worm.Query.Sorting
Imports Worm.Cache
Imports Worm.Expressions2

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
        Protected _stmt As String

#Region " Cache "
        Private _dp() As Cache.IDependentTypes
#End Region

        Public Sub SetDependency(ByVal p As CacheItemBaseProvider)
            _dp = p._dp
        End Sub

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
            'If uce IsNot Nothing AndAlso _q.propSort IsNot Nothing Then
            'If _q.Sort IsNot Nothing Then
            '    Dim srt As Sort = _q.Sort
            '    If srt.Query IsNot Nothing Then
            '        ce.Sort = Sort.GetOnlyKey(_mgr.MappingEngine, _mgr.GetContextInfo)
            '    Else
            '        ce.Sort = srt
            '    End If
            'End If
            ce.Sort = _q.Sort

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
                    Dim hasJoins As Boolean = False
                    Dim flst As List(Of String) = Nothing
                    For Each de As KeyValuePair(Of EntityUnion, List(Of String)) In q.GetDependentEntities(MappingEngine, flst)
                        Dim jt As Type = de.Key.GetRealType(MappingEngine)
                        cache.validate_AddDeleteType(New Type() {jt}, _key, _id)
                        If de.Value.Count = 0 Then
                            cache.validate_UpdateType(New Type() {jt}, _key, _id)
                        Else
                            For Each s As String In de.Value
                                cache.validate_AddDependentFilterField(New Pair(Of String, Type)(s, jt), _key, _id)
                            Next
                        End If
                        hasJoins = True
                    Next

                    Dim ft As Type = Nothing
                    If q.FromClause IsNot Nothing AndAlso q.FromClause.QueryEU IsNot Nothing Then
                        ft = q.FromClause.QueryEU.GetRealType(MappingEngine)

                        For Each s As String In flst
                            cache.validate_AddDependentFilterField(New Pair(Of String, Type)(s, ft), _key, _id)
                        Next
                    End If

                    If _q._f IsNot Nothing Then
                        For Each ff As IFilter In _q._f.GetAllFilters
                            Dim vf As IValuableFilter = TryCast(ff, IValuableFilter)
                            If vf IsNot Nothing Then
                                Dim v As Criteria.Values.EntityValue = TryCast(vf.Value, Criteria.Values.EntityValue)
                                If v IsNot Nothing Then
                                    cache.validate_AddDependentObject(v.GetOrmValue(_mgr), _key, _id)
                                End If
                            End If
                        Next
                    End If

                    If cache.ValidateBehavior = Worm.Cache.ValidateBehavior.Immediate Then
                        'notPreciseDependsAD = notPreciseDependsAD OrElse Not Worm.Cache.IsEmpty(dp)
                        'notPreciseDependsU = notPreciseDependsAD
                        'If _f IsNot Nothing AndAlso _f.Length > i Then
                        Dim fl As IFilter = _q._f
                        Dim added As Boolean = False
                        If ft IsNot Nothing Then
                            Dim evalSort As Boolean = _q.Sort Is Nothing OrElse _q.Sort.CanEvaluate(MappingEngine)
                            If evalSort Then
                                If uce IsNot Nothing Then
                                    uce.Sort = _q.Sort
                                End If
                                If Not hasJoins Then
                                    added = cache.validate_AddCalculatedType(New Type() {ft}, _key, _id, fl, MappingEngine)
                                    If uce IsNot Nothing AndAlso added Then
                                        uce.Filter = fl
                                        uce.Joins = _q.Joins
                                        uce.QueryEU = _q.FromClause.QueryEU
                                    End If
                                End If
                            End If
                        End If

                        Dim ad As Boolean
                        If Not added AndAlso fl IsNot Nothing Then
                            For Each fff As IFilter In fl.GetAllFilters
                                Dim ef As IEntityFilter = TryCast(fff, IEntityFilter)

                                If ef Is Nothing Then
                                    If ft IsNot Nothing AndAlso Not ad Then
                                        cache.validate_AddDeleteType(New Type() {ft}, _key, _id)
                                        cache.validate_UpdateType(New Type() {ft}, _key, _id)
                                        ad = True
                                    End If
                                Else
                                    Dim tmpl As OrmFilterTemplate = CType(ef.Template, OrmFilterTemplate)
                                    Dim t As Type = tmpl.ObjectSource.GetRealType(MappingEngine)
                                    Dim p As New Pair(Of String, Type)(tmpl.PropertyAlias, t)
                                    cache.validate_AddDependentFilterField(p, _key, _id)
                                End If
                            Next
                        End If

                        If Not ad AndAlso Not added AndAlso ft IsNot Nothing Then
                            cache.validate_AddDeleteType(New Type() {ft}, _key, _id)
                            cache.validate_UpdateType(New Type() {ft}, _key, _id)

                            'If (_q.FromClause.ObjectSource IsNot Nothing AndAlso Not selectTypes.Contains(_q.FromClause.ObjectSource.GetRealType(MappingEngine))) Then
                            '    cache.validate_AddDeleteType(New Type() {_q.FromClause.ObjectSource.GetRealType(MappingEngine)}, _key, _id)
                            '    cache.validate_UpdateType(New Type() {_q.FromClause.ObjectSource.GetRealType(MappingEngine)}, _key, _id)
                            'Else
                            '    Dim rcmd As RelationCmd = TryCast(_q, RelationCmd)
                            '    If rcmd IsNot Nothing Then
                            '        cache.validate_AddDeleteType(New Type() {rcmd.RelationDesc.Entity.GetRealType(MappingEngine)}, _key, _id)
                            '        cache.validate_UpdateType(New Type() {rcmd.RelationDesc.Entity.GetRealType(MappingEngine)}, _key, _id)
                            '    End If
                            'End If
                        End If
                    Else
                        If ft IsNot Nothing Then
                            cache.validate_AddDeleteType(New Type() {ft}, _key, _id)
                            'notPreciseDependsAD = True
                        End If

                        'If _f IsNot Nothing AndAlso _f.Length > i Then
                        Dim fl As IFilter = _q._f
                        If fl IsNot Nothing Then
                            For Each fff As IFilter In fl.GetAllFilters
                                Dim ef As IEntityFilter = TryCast(fff, IEntityFilter)

                                If ef Is Nothing Then
                                    If ft IsNot Nothing Then
                                        cache.validate_UpdateType(New Type() {ft}, _key, _id)
                                        'notPreciseDependsU = True
                                    End If
                                Else
                                    Dim tmpl As OrmFilterTemplate = CType(ef.Template, OrmFilterTemplate)
                                    Dim rt As Type = tmpl.ObjectSource.GetRealType(MappingEngine)

                                    Dim p As New Pair(Of String, Type)(tmpl.PropertyAlias, rt)
                                    cache.validate_AddDependentFilterField(p, _key, _id)
                                End If
                            Next
                        End If
                    End If

                    If q.Sort IsNot Nothing Then
                        For Each s As SortExpression In q.Sort
                            For Each exp As IExpression In s.GetExpressions
                                Dim ee As EntityExpression = TryCast(exp, EntityExpression)
                                If ee IsNot Nothing Then
                                    Dim t As Type = ee.ObjectProperty.Entity.GetRealType(MappingEngine)
                                    Dim p As New Pair(Of String, Type)(ee.ObjectProperty.PropertyAlias, t)
                                    cache.validate_AddDependentSortField(p, _key, _id)
                                    'ElseIf ft IsNot Nothing Then
                                    '    cache.validate_AddDeleteType(New Type() {ft}, _key, _id)
                                End If
                            Next
                        Next
                    End If

                    If q.Group IsNot Nothing Then
                        For Each exp As IExpression In q.Group.GetExpressions
                            Dim ee As EntityExpression = TryCast(exp, EntityExpression)
                            If ee IsNot Nothing Then
                                Dim p As New Pair(Of String, Type)(ee.ObjectProperty.PropertyAlias, ee.ObjectProperty.Entity.GetRealType(MappingEngine))
                                cache.validate_AddDependentGroupField(p, _key, _id)
                                'ElseIf ft IsNot Nothing Then
                                '    cache.validate_AddDeleteType(New Type() {ft}, _key, _id)
                            End If
                        Next
                    End If

                    Dim rq As RelationCmd = TryCast(q, RelationCmd)

                    If rq IsNot Nothing Then
                        Dim m2m As M2MRelation = TryCast(rq.Relation, M2MRelation)
                        If m2m IsNot Nothing Then
                            'Debug.Assert(types Is Nothing OrElse CType(types, IList(Of Type)).Count = 1)
                            cache.AddM2MSimpleQuery(m2m, _key, _id)
                            If m2m.Relation.ConnectedType IsNot Nothing Then
                                cache.validate_AddDeleteType(New Type() {m2m.Relation.ConnectedType}, _key, _id)
                                cache.validate_UpdateType(New Type() {m2m.Relation.ConnectedType}, _key, _id)
                            End If
                        End If
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

        Public Overridable Sub Reset(ByVal mgr As OrmManager, ByVal q As QueryCmd)
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

            _key = QueryCmd.GetStaticKey(_q, _mgr.GetStaticKey(), _mgr.Cache.CacheListBehavior, fromKey, _mgr.MappingEngine, _dic, mgr.GetContextInfo)

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
            If _dic IsNot Nothing Then
                _dic.Remove(_id)
            End If
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

        Public ReadOnly Property Sort() As OrderByClause Implements OrmManager.ICacheItemProvoderBase.Sort
            Get
                Return _q.Sort
            End Get
        End Property

        Public ReadOnly Property Group() As GroupExpression
            Get
                Return _q.Group
            End Get
        End Property

        Public Overridable Sub CopyTo(ByVal cp As CacheItemBaseProvider)
            With cp
                ._created = _created
                ._dic = _dic
                ._dp = _dp
                ._id = _id
                ._key = _key
                ._m = _m
                ._renew = _renew
                ._sm = _sm
                ._sync = _sync

                ._mgr = _mgr
                ._q = _q
            End With
        End Sub

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