Imports System.Collections.Generic
Imports Worm.Criteria.Core
Imports Worm.Orm
Imports Worm.OrmManager
Imports Worm.Criteria.Joins

Namespace Query
    Public MustInherit Class CacheItemBaseProvider
        Implements ICacheItemProvoderBase

        Private _created As Boolean
        Private _renew As Boolean

        Protected Sub New()

        End Sub

        Protected _mgr As OrmManager
        Protected _j As List(Of List(Of Worm.Criteria.Joins.OrmJoin))
        Protected _f() As IFilter
        Protected _sl As List(Of List(Of SelectExpression))
        Protected _q As QueryCmd
        Protected _key As String
        Protected _id As String
        Protected _sync As String
        Protected _dic As IDictionary

        Public Sub New(ByVal mgr As OrmManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.OrmJoin)), _
            ByVal f() As IFilter, ByVal q As QueryCmd, ByVal sl As List(Of List(Of SelectExpression)))
            _q = q

            Reset(mgr, j, f, q.SelectedType, sl)
        End Sub

        Protected Sub New(ByVal mgr As OrmManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.OrmJoin)), _
            ByVal f() As IFilter, ByVal q As QueryCmd, ByVal t As Type, ByVal sl As List(Of List(Of SelectExpression)))
            _q = q

            Reset(mgr, j, f, t, sl)
        End Sub

        Public Overridable Property Created() As Boolean Implements ICacheItemProvoderBase.Created
            Get
                Return _created
            End Get
            Set(ByVal Value As Boolean)
                _created = Value
            End Set
        End Property

        Public Sub CreateDepends() Implements OrmManager.ICacheItemProvoderBase.CreateDepends
            Dim notPreciseDependsAD As Boolean
            Dim notPreciseDependsU As Boolean
            If _j IsNot Nothing Then
                For Each js As List(Of Worm.Criteria.Joins.OrmJoin) In _j
                    For Each j As OrmJoin In js
                        If j.Type IsNot Nothing Then
                            notPreciseDependsAD = True
                            notPreciseDependsU = True
                            '_mgr.Cache.AddFilterlessDependType(_mgr.GetFilterInfo, j.Type, _key, _id, _mgr.MappingEngine)
                            Cache.validate_AddDeleteType(j.Type, _key, _id)
                            Cache.validate_UpdateType(j.Type, _key, _id)
                        ElseIf Not String.IsNullOrEmpty(j.EntityName) Then
                            notPreciseDependsAD = True
                            notPreciseDependsU = True
                            '_mgr.Cache.AddFilterlessDependType(_mgr.GetFilterInfo, _mgr.MappingEngine.GetTypeByEntityName(j.EntityName), _key, _id, _mgr.MappingEngine)
                            Cache.validate_AddDeleteType(MappingEngine.GetTypeByEntityName(j.EntityName), _key, _id)
                            Cache.validate_UpdateType(MappingEngine.GetTypeByEntityName(j.EntityName), _key, _id)
                        End If
                    Next
                Next
            End If

            'Dim i As Integer = 0
            'For Each q As QueryCmd In New QueryIterator(_q)
            '    CreateDepends(q, i)
            '    i += 1
            'Next
            Dim rightType As Boolean = _q.SelectedType IsNot Nothing AndAlso GetType(_ICachedEntity).IsAssignableFrom(_q.SelectedType)

            Dim i As Integer = 0
            For Each q As QueryCmd In New QueryIterator(_q)
                Dim dp As Cache.IDependentTypes = q.Get(MappingEngine)
                Worm.Cache.Add2Cache(Cache, dp, _key, _id)

                If _f IsNot Nothing AndAlso _f.Length > i Then
                    Dim vf As IValuableFilter = TryCast(_f(i), IValuableFilter)
                    If vf IsNot Nothing Then
                        Dim v As Criteria.Values.EntityValue = TryCast(vf.Value, Criteria.Values.EntityValue)
                        If v IsNot Nothing Then
                            Cache.validate_AddDependentObject(v.GetOrmValue(_mgr), _key, _id)
                        End If
                    End If
                End If

                'Dim ef As IEntityFilter = Nothing
                'If _f IsNot Nothing AndAlso _f.Length > i Then
                '    ef = TryCast(_f(i), IEntityFilter)
                'End If

                If Cache.ValidateBehavior = Worm.Cache.ValidateBehavior.Immediate Then
                    notPreciseDependsAD = notPreciseDependsAD OrElse Not Worm.Cache.IsEmpty(dp)
                    notPreciseDependsU = notPreciseDependsAD
                    If _f IsNot Nothing AndAlso _f.Length > i Then
                        Dim added As Boolean = False
                        If rightType AndAlso Not notPreciseDependsAD Then
                            added = Cache.validate_AddCalculatedType(q.SelectedType, _key, _id, _f(i))
                        End If

                        If Not added Then
                            For Each fff As IFilter In _f(i).GetAllFilters
                                Dim ef As IEntityFilter = TryCast(fff, IEntityFilter)

                                If ef Is Nothing Then
                                    If rightType AndAlso Not notPreciseDependsAD Then
                                        Cache.validate_AddDeleteType(q.SelectedType, _key, _id)
                                        Cache.validate_UpdateType(q.SelectedType, _key, _id)
                                        notPreciseDependsAD = True
                                        notPreciseDependsU = True
                                    End If
                                Else
                                    Dim tmpl As OrmFilterTemplateBase = CType(ef.Template, OrmFilterTemplateBase)

                                    If tmpl.Type Is Nothing Then
                                        Throw New NullReferenceException("Type for OrmFilterTemplate must be specified")
                                    End If

                                    Dim p As New Pair(Of String, Type)(tmpl.FieldName, tmpl.Type)
                                    Cache.validate_AddDependentFilterField(p, _key, _id)
                                End If
                            Next
                        End If
                    End If
                Else
                    If rightType Then
                        Cache.validate_AddDeleteType(q.SelectedType, _key, _id)
                        notPreciseDependsAD = True
                    End If

                    If _f IsNot Nothing AndAlso _f.Length > i Then
                        For Each fff As IFilter In _f(i).GetAllFilters
                            Dim ef As IEntityFilter = TryCast(fff, IEntityFilter)

                            If ef Is Nothing Then
                                If rightType AndAlso Not notPreciseDependsU Then
                                    Cache.validate_UpdateType(q.SelectedType, _key, _id)
                                    notPreciseDependsU = True
                                End If
                            Else
                                Dim tmpl As OrmFilterTemplateBase = CType(ef.Template, OrmFilterTemplateBase)

                                If tmpl.Type Is Nothing Then
                                    Throw New NullReferenceException("Type for OrmFilterTemplate must be specified")
                                End If

                                Dim p As New Pair(Of String, Type)(tmpl.FieldName, tmpl.Type)
                                Cache.validate_AddDependentFilterField(p, _key, _id)
                            End If
                        Next
                    End If
                End If

                For Each s As Sorting.Sort In New Sorting.Sort.Iterator(q.propSort)

                    'If ef Is Nothing Then
                    '    If rightType Then
                    '        _mgr.Cache.validate_UpdateType(q.SelectedType, _key, _id)
                    '        notPreciseDependsU = True
                    '    End If
                    'Else
                    '    Dim tmpl As OrmFilterTemplateBase = CType(ef.Template, OrmFilterTemplateBase)

                    '    If tmpl.Type Is Nothing Then
                    '        Throw New NullReferenceException("Type for OrmFilterTemplate must be specified")
                    '    End If

                    '    Dim p As New Pair(Of String, Type)(tmpl.FieldName, tmpl.Type)
                    '    _mgr.Cache.validate_AddDependentFilterField(p, _key, _id)
                    'End If
                Next

                If q.Obj IsNot Nothing Then
                    Cache.AddM2MQuery(q.Obj.GetRelation(q.SelectedType, q.M2MKey), _key, _id)
                End If

                'If Not (Cache.IsCalculated(dp) OrElse notPreciseDepends) Then
                '    Dim ef As IEntityFilter = TryCast(_f(i), IEntityFilter)

                'End If
                i += 1
            Next
        End Sub

        Public ReadOnly Property Filter() As Criteria.Core.IFilter Implements OrmManager.ICacheItemProvoderBase.Filter
            Get
                Return _f(0)
            End Get
        End Property

        Public MustOverride Function GetCacheItem(ByVal withLoad As Boolean) As OrmManager.CachedItem Implements OrmManager.ICacheItemProvoderBase.GetCacheItem
        Public MustOverride Sub Reset(ByVal mgr As OrmManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.OrmJoin)), ByVal f() As IFilter, ByVal t As Type, ByVal sl As List(Of List(Of SelectExpression)))

        Public Property Mgr() As OrmManager
            Get
                Return _mgr
            End Get
            Protected Friend Set(ByVal value As OrmManager)
                _mgr = value
            End Set
        End Property


        Protected ReadOnly Property Cache() As Cache.OrmCacheBase
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

        Public ReadOnly Property Sort() As Sorting.Sort Implements OrmManager.ICacheItemProvoderBase.Sort
            Get
                Return _q.propSort
            End Get
        End Property

        Protected Function ValidateFromCache() As Boolean
            If Cache.ValidateBehavior = Worm.Cache.ValidateBehavior.Deferred Then
                Dim f As IEntityFilter = Nothing
                If _f IsNot Nothing AndAlso _f.Length > 0 Then
                    f = TryCast(_f(0), IEntityFilter)
                End If

                Return _mgr.Cache.UpdateCacheDeferred(_q.SelectedType, f, _q.propSort, _q.Group)
            End If
            Return True
        End Function
    End Class
End Namespace