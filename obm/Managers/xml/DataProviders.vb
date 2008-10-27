Imports Worm.Orm
Imports Worm.Sorting
Imports Worm.Criteria.Core
Imports System.Collections.Generic
Imports Worm.Cache
Imports Worm.Criteria.Values
Imports Worm.Orm.Meta

Namespace Xml
    Partial Public Class QueryManager

        Protected MustInherit Class BaseDataProvider(Of T As {New, IOrmBase})
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
                                Dim idx As Integer = fields.IndexOf(tmpl.FieldName)
                                If idx >= 0 Then
                                    Dim p As New Pair(Of String, Type)(tmpl.FieldName, tmpl.Type)
                                        c.ResetFieldDepends(p)
                                        c.RemoveUpdatedFields(tmpl.Type, tmpl.FieldName)
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
                                    Dim p As New Pair(Of String, Type)(tmpl.FieldName, tmpl.Type)
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

            Public Sub New(ByVal mgr As QueryManager, ByVal f As Worm.Criteria.Core.IFilter, _
                ByVal sort As Sort, ByVal key As String, ByVal id As String)
                MyBase.New(mgr, f, sort, key, id)
            End Sub

            Public Sub New(ByVal mgr As QueryManager, ByVal f As IFilter, ByVal cols As List(Of ColumnAttribute), _
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

                Dim c As New Criteria.Conditions.Condition.ConditionConstructor
                c.AddFilter(_f)
                c.AddFilter(AppendWhere)
                Schema.AppendWhere(original_type, c.Condition, sb, _mgr.GetFilterInfo)
                If _sort IsNot Nothing AndAlso Not _sort.IsExternal Then
                    Schema.AppendOrder(original_type, _sort, sb)
                End If

                Dim r As ReadOnlyList(Of T) = CType(_mgr.LoadMultipleObjects(Of T)(sb.ToString, withLoad, Nothing), Global.Worm.ReadOnlyList(Of T))

                If _sort IsNot Nothing AndAlso _sort.IsExternal Then
                    r = CType(Schema.ExternalSort(Of T)(_mgr, _sort, r), Global.Worm.ReadOnlyList(Of T))
                End If
                Return r
            End Function

            Protected ReadOnly Property Schema() As XPathGenerator
                Get
                    Return CType(_mgr.MappingEngine, XPathGenerator)
                End Get
            End Property

            'Protected ReadOnly Property Mgr() As 
            '    Get
            '        Return CType(_mgr, OrmReadOnlyDBManager)
            '    End Get
            'End Property

            Protected Overridable Function AppendWhere() As IFilter
                Return Nothing
            End Function

            Protected Overridable Sub AppendSelect(ByVal sb As StringBuilder, ByVal t As Type, ByVal arr As IList(Of ColumnAttribute))
                Throw New NotImplementedException
            End Sub

            Protected Overridable Sub AppendSelectID(ByVal sb As StringBuilder, ByVal t As Type)
                sb.Append(Schema.SelectID(t))
            End Sub
        End Class

    End Class
End Namespace