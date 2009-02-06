Imports Worm.Entities
Imports Worm.Sorting
Imports Worm.Criteria.Core
Imports System.Collections.Generic
Imports Worm.Cache
Imports Worm.Criteria.Values
Imports Worm.Entities.Meta
Imports Worm.Criteria.Conditions

Namespace Xml
    Partial Public Class QueryManager

        Protected MustInherit Class BaseDataProvider(Of T As {New, IKeyEntity})
            Inherits CustDelegate(Of T)
            Implements ICacheValidator

            Protected _f As Worm.Criteria.Core.IFilter
            Protected _sort As Sort
            Protected _mgr As QueryManager
            Protected _key As String
            Protected _id As String

            Public Sub New(ByVal mgr As QueryManager, ByVal f As Worm.Criteria.Core.IFilter, _
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

            Public Overridable Function Validate() As Boolean Implements ICacheValidator.ValidateBeforCacheProbe
                If _f IsNot Nothing Then
                    Dim c As OrmCache = TryCast(_mgr.Cache, OrmCache)
                    If c IsNot Nothing Then
                        For Each fl As IFilter In _f.GetAllFilters
                            Dim f As IEntityFilter = TryCast(fl, IEntityFilter)
                            If f IsNot Nothing Then
                                Dim tmpl As OrmFilterTemplate = CType(f.Template, OrmFilterTemplate)

                                Dim fields As List(Of String) = Nothing
                                Dim rt As Type = tmpl.ObjectSource.GetRealType(_mgr.MappingEngine)
                                If c.GetUpdatedFields(rt, fields) Then
                                    Dim idx As Integer = fields.IndexOf(tmpl.PropertyAlias)
                                    If idx >= 0 Then
                                        Dim p As New Pair(Of String, Type)(tmpl.PropertyAlias, rt)
                                        c.ResetFieldDepends(p)
                                        c.RemoveUpdatedFields(rt, tmpl.PropertyAlias)
                                        Return False
                                    End If
                                End If
                            End If
                        Next
                    End If
                End If
                Return True
            End Function

            Public Overridable Function Validate(ByVal ce As UpdatableCachedItem) As Boolean Implements ICacheValidator.ValidateItemFromCache
                Return True
            End Function

            Public Overrides Sub CreateDepends()
                If Not _mgr._dont_cache_lists AndAlso _f IsNot Nothing Then
                    Dim cache As OrmCache = TryCast(_mgr.Cache, OrmCache)
                    If cache IsNot Nothing Then
                        Dim tt As System.Type = GetType(T)
                        cache.AddDependType(_mgr.GetContextFilter, tt, _key, _id, _f, _mgr.MappingEngine)

                        For Each fl As IFilter In _f.GetAllFilters
                            Dim f As IEntityFilter = TryCast(fl, IEntityFilter)
                            If f IsNot Nothing Then
                                Dim tmpl As OrmFilterTemplate = CType(f.Template, OrmFilterTemplate)
                                Dim rt As Type = tmpl.ObjectSource.GetRealType(_mgr.MappingEngine)
                                If rt Is Nothing Then
                                    Throw New NullReferenceException("Type for OrmFilterTemplate must be specified")
                                End If
                                Dim v As EntityValue = TryCast(CType(f, EntityFilter).Value, EntityValue)
                                If v IsNot Nothing Then
                                    'Dim tp As Type = f.Value.GetType 'Schema.GetFieldTypeByName(f.Type, f.FieldName)
                                    'If GetType(OrmBase).IsAssignableFrom(tp) Then
                                    cache.AddDepend(v.GetOrmValue(_mgr), _key, _id)
                                    'End If
                                Else
                                    Dim p As New Pair(Of String, Type)(tmpl.PropertyAlias, rt)
                                    cache.AddFieldDepend(p, _key, _id)
                                End If
                                If tt IsNot rt Then
                                    cache.AddJoinDepend(rt, tt)
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

            Public Overrides Function GetCacheItem(ByVal withLoad As Boolean) As UpdatableCachedItem
                Dim sortex As IOrmSorting2 = TryCast(_mgr.MappingEngine.GetEntitySchema(GetType(T)), IOrmSorting2)
                Dim s As Date = Nothing
                If sortex IsNot Nothing Then
                    Dim ts As TimeSpan = sortex.SortExpiration(_sort)
                    If ts <> TimeSpan.MaxValue AndAlso ts <> TimeSpan.MinValue Then
                        s = Now.Add(ts)
                    End If
                End If
                Return New UpdatableCachedItem(s, GetValues(withLoad), _mgr)
                'Return New UpdatableCachedItem(_sort, s, _f, GetValues(withLoad), _mgr)
            End Function

            Public Overrides Function GetCacheItem(ByVal col As ReadOnlyEntityList(Of T)) As UpdatableCachedItem
                Dim sortex As IOrmSorting2 = TryCast(_mgr.MappingEngine.GetEntitySchema(GetType(T)), IOrmSorting2)
                Dim s As Date = Nothing
                If sortex IsNot Nothing Then
                    Dim ts As TimeSpan = sortex.SortExpiration(_sort)
                    If ts <> TimeSpan.MaxValue AndAlso ts <> TimeSpan.MinValue Then
                        s = Now.Add(ts)
                    End If
                End If
                Return New UpdatableCachedItem(s, col, _mgr)
                'Return New UpdatableCachedItem(_sort, s, _f, col, _mgr)
            End Function
        End Class


        Protected Class FilterCustDelegate(Of T As {New, IKeyEntity})
            Inherits BaseDataProvider(Of T)

            Private _cols As List(Of EntityPropertyAttribute)

            Public Sub New(ByVal mgr As QueryManager, ByVal f As Worm.Criteria.Core.IFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String)
                MyBase.New(mgr, f, sort, key, id)
            End Sub

            Public Sub New(ByVal mgr As QueryManager, ByVal f As IFilter, ByVal cols As List(Of EntityPropertyAttribute), _
                ByVal sort As Sort, ByVal key As String, ByVal id As String)
                MyBase.New(mgr, f, sort, key, id)
                _cols = cols
            End Sub

            Public Overrides Function GetValues(ByVal withLoad As Boolean) As ReadOnlyList(Of T)
                Dim original_type As Type = GetType(T)

                'hardcoded
                withLoad = True

                'Dim arr As Generic.List(Of ColumnAttribute) = _cols

                Dim sb As New StringBuilder
                'If withLoad Then
                '    If arr Is Nothing Then
                '        arr = _mgr.ObjectSchema.GetSortedFieldList(original_type)
                '    End If
                '    AppendSelect(sb, original_type, arr)
                'Else
                '    arr = New Generic.List(Of ColumnAttribute)
                '    arr.Add(New ColumnAttribute("ID", Field2DbRelations.PK))
                '    AppendSelectID(sb, original_type, arr)
                'End If

                AppendSelectID(sb, original_type)

                Dim c As New Condition.ConditionConstructor
                c.AddFilter(_f)
                c.AddFilter(AppendWhere)
                _mgr.XPathGenerator.AppendWhere(_mgr.MappingEngine, original_type, c.Condition, sb, _mgr.GetContextFilter)
                If _sort IsNot Nothing AndAlso Not _sort.IsExternal Then
                    _mgr.XPathGenerator.AppendOrder(original_type, _sort, sb)
                End If

                Dim r As New ReadOnlyList(Of T)
                _mgr.LoadMultipleObjects(Of T)(sb.ToString, CType(r.List, System.Collections.IList))

                If _sort IsNot Nothing AndAlso _sort.IsExternal Then
                    r = CType(_mgr.MappingEngine.ExternalSort(Of T)(_mgr, _sort, r.List), Global.Worm.ReadOnlyList(Of T))
                End If
                Return r
            End Function

            'Protected ReadOnly Property Schema() As XPathGenerator
            '    Get
            '        Return CType(_mgr.s, XPathGenerator)
            '    End Get
            'End Property

            'Protected ReadOnly Property Mgr() As 
            '    Get
            '        Return CType(_mgr, OrmReadOnlyDBManager)
            '    End Get
            'End Property

            Protected Overridable Function AppendWhere() As IFilter
                Return Nothing
            End Function

            Protected Overridable Sub AppendSelect(ByVal sb As StringBuilder, ByVal t As Type, ByVal arr As IList(Of EntityPropertyAttribute))
                Throw New NotImplementedException
            End Sub

            Protected Overridable Sub AppendSelectID(ByVal sb As StringBuilder, ByVal t As Type)
                sb.Append(_mgr.XPathGenerator.SelectID(_mgr.MappingEngine, t))
            End Sub
        End Class

    End Class
End Namespace