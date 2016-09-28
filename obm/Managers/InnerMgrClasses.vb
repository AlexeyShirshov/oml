Imports Worm.Entities.Meta
Imports Worm.Entities
Imports Worm.Cache
Imports Worm.Query.Sorting
Imports Worm.Criteria.Core
Imports System.Collections.Generic
Imports Worm.Expressions2
Imports Worm.Query

Partial Public Class OrmManager
#If OLDM2M Then
    Friend Class M2MEnum
        Public ReadOnly o As IRelation
        'Public ReadOnly obj As OrmBase
        Dim p1 As IRelation.RelationDesc 'Pair(Of String, Type)
        Dim p2 As IRelation.RelationDesc 'Pair(Of String, Type)
        Dim o1 As IKeyEntity
        Dim o2 As IKeyEntity

        Public Sub New(ByVal r As IRelation, ByVal obj As IKeyEntity, ByVal schema As ObjectMappingEngine)
            Me.o = r
            'Me.obj = obj
            p1 = o.GetFirstType
            p2 = o.GetSecondType
            'o1 = CType(schema.GetFieldValue(obj, p1.First), OrmBase)
            'o2 = CType(schema.GetFieldValue(obj, p2.First), OrmBase)
            Dim oschema As IEntitySchema = schema.GetEntitySchema(obj.GetType)
            o1 = CType(schema.GetPropertyValue(obj, p1.PropertyName, oschema), IKeyEntity)
            o2 = CType(schema.GetPropertyValue(obj, p2.PropertyName, oschema), IKeyEntity)
        End Sub


        Public Function Add(ByVal mgr As OrmManager, ByVal e As M2MCache) As Boolean
            If e Is Nothing Then
                Throw New ArgumentNullException("e")
            End If

            Dim el As CachedM2MRelation = e.Entry
            Dim obj As IKeyEntity = Nothing, subobj As IKeyEntity = Nothing
            Dim main As IKeyEntity = mgr.GetKeyEntityFromCacheOrCreate(el.MainId, el.MainType)
            If main.Equals(o1) Then
                obj = o1
                subobj = o2
            ElseIf main.Equals(o2) Then
                obj = o2
                subobj = o1
            End If

            If obj IsNot Nothing Then
                If e.Sort Is Nothing Then
                    el.Add(subobj)
                Else
                    Dim s As IOrmSorting = Nothing
                    'Dim col As New ArrayList(mgr.ConvertIds2Objects(el.SubType, el.Added, False))
                    Dim col As New ArrayList(CType(el.Added, ICollection))
                    If Not OrmManager.CanSortOnClient(el.SubType, col, e.Sort, s) Then
                        Return False
                    End If
                    Dim c As IComparer = Nothing
                    If s Is Nothing Then
                        c = New OrmComparer(Of _IEntity)(el.SubType, e.Sort)
                    Else
                        c = s.CreateSortComparer(e.Sort)
                    End If
                    If c Is Nothing Then
                        Return False
                    End If
                    Dim pos As Integer = col.BinarySearch(subobj, c)
                    If pos < 0 Then
                        el.Add(subobj, Not pos)
                    End If
                End If
            End If
            Return True
        End Function

        Public Function Remove(ByVal mgr As OrmManager, ByVal e As M2MCache) As Boolean
            If e Is Nothing Then
                Throw New ArgumentNullException("e")
            End If

            Dim el As CachedM2MRelation = e.Entry
            Dim main As IKeyEntity = mgr.GetKeyEntityFromCacheOrCreate(el.MainId, el.MainType)
            If main.Equals(o1) Then
                el.Delete(o2)
            ElseIf main.Equals(o2) Then
                el.Delete(o1)
            End If
            Return True
        End Function

        Public Function Accept(ByVal mgr As OrmManager, ByVal e As M2MCache) As Boolean
            If e Is Nothing Then
                Throw New ArgumentNullException("e")
            End If

            Dim el As CachedM2MRelation = e.Entry
            Dim main As IKeyEntity = mgr.GetKeyEntityFromCacheOrCreate(el.MainId, el.MainType)
            If main.Equals(o1) OrElse main.Equals(o2) Then
                Return el.Accept(mgr)
            End If
            Return True
        End Function

        Public Function Reject(ByVal mgr As OrmManager, ByVal e As M2MCache) As Boolean
            If e Is Nothing Then
                Throw New ArgumentNullException("e")
            End If

            Dim el As CachedM2MRelation = e.Entry
            Dim main As IKeyEntity = mgr.GetKeyEntityFromCacheOrCreate(el.MainId, el.MainType)
            If main.Equals(o1) OrElse main.Equals(o2) Then
                el.Reject(mgr, False)
            End If
            Return True
        End Function
    End Class
#End If

    Public Enum SaveAction
        Update
        Delete
        Insert
    End Enum

    Public Class SchemaSwitcher
        Implements IDisposable

        Private _disposedValue As Boolean = False        ' To detect redundant calls
        Private _oldSchema As ObjectMappingEngine
        Private _mgr As OrmManager
        'Private _r As Boolean

        Public Sub New(ByVal schema As ObjectMappingEngine)
            MyClass.New(schema, OrmManager.CurrentManager)
        End Sub

        Private Sub ObjectCreated(ByVal mgr As OrmManager, ByVal obj As IEntity)
            CType(obj, _IEntity).SpecificMappingEngine = mgr.MappingEngine
        End Sub

        Public Sub New(ByVal schema As ObjectMappingEngine, ByVal mgr As OrmManager)
            _mgr = mgr
            _oldSchema = mgr.MappingEngine
            mgr.SetMapping(schema)
            '_r = mgr.RaiseObjectCreation
            If Not _oldSchema.Equals(schema) Then
                'mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectLoaded, AddressOf ObjectCreated
            End If
        End Sub

        ' IDisposable
        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not Me._disposedValue Then
                _mgr.SetMapping(_oldSchema)
                '_mgr.RaiseObjectCreation = _r
                RemoveHandler _mgr.ObjectLoaded, AddressOf ObjectCreated
            End If
            Me._disposedValue = True
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

    End Class

    Public Class CacheListBehavior
        Implements IDisposable

        Private _disposedValue As Boolean
        Private _mgr As OrmManager
        Private _oldvalue As Boolean
        Private _oldExp As Date
        Private _oldMark As String

        Public Sub New(ByVal mgr As OrmManager, ByVal cache_lists As Boolean)
            _mgr = mgr
            _oldvalue = mgr._dont_cache_lists
            mgr._dont_cache_lists = Not cache_lists
        End Sub

        Public Sub New(ByVal mgr As OrmManager, ByVal liveTime As TimeSpan)
            _mgr = mgr
            _oldvalue = mgr._dont_cache_lists
            _oldExp = mgr._expiresPattern
            mgr._expiresPattern = Date.Now.Add(liveTime)
        End Sub

        Public Sub New(ByVal mgr As OrmManager, ByVal cache_lists As Boolean, ByVal liveTime As TimeSpan)
            _mgr = mgr
            _oldvalue = mgr._dont_cache_lists
            mgr._dont_cache_lists = Not cache_lists
            _oldExp = mgr._expiresPattern
            mgr._expiresPattern = Date.Now.Add(liveTime)
        End Sub

        Public Sub New(ByVal mgr As OrmManager, ByVal cache_lists As Boolean, ByVal mark As String)
            _mgr = mgr
            _oldvalue = mgr._dont_cache_lists
            mgr._dont_cache_lists = Not cache_lists
            _oldMark = mgr._list
            mgr._list = mark
        End Sub

        Public Sub New(ByVal mgr As OrmManager, ByVal liveTime As TimeSpan, ByVal mark As String)
            _mgr = mgr
            _oldvalue = mgr._dont_cache_lists
            _oldExp = mgr._expiresPattern
            mgr._expiresPattern = Date.Now.Add(liveTime)
            _oldMark = mgr._list
            mgr._list = mark
        End Sub

        Public Sub New(ByVal mgr As OrmManager, ByVal cache_lists As Boolean, ByVal liveTime As TimeSpan, ByVal mark As String)
            _mgr = mgr
            _oldvalue = mgr._dont_cache_lists
            mgr._dont_cache_lists = Not cache_lists
            _oldExp = mgr._expiresPattern
            mgr._expiresPattern = Date.Now.Add(liveTime)
            _oldMark = mgr._list
            mgr._list = mark
        End Sub

        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not Me._disposedValue Then
                _mgr._dont_cache_lists = _oldvalue
                _mgr._expiresPattern = _oldExp
                _mgr._list = _oldMark
            End If
            Me._disposedValue = True
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
    End Class

    <Serializable()> _
    Public Structure ExecutionResult
        Public ReadOnly RowCount As Integer
        Public ReadOnly ExecutionTime As TimeSpan
        Public ReadOnly FetchTime As TimeSpan
        Public ReadOnly CacheHit As Boolean
        Public ReadOnly LoadedInResultset As Nullable(Of Integer)

        Public Sub New(ByVal count As Integer, ByVal execTime As TimeSpan, ByVal fetchTime As TimeSpan, _
            ByVal hit As Boolean, ByVal loaded As Nullable(Of Integer))
            Me.RowCount = count
            Me.ExecutionTime = execTime
            Me.FetchTime = fetchTime
            Me.CacheHit = hit
            Me.LoadedInResultset = loaded
            If _tsExec.Switch.ShouldTrace(TraceEventType.Information) Then
                helper.WriteLineInfo(_tsExec, GetTraceStr)
            End If
        End Sub

        Private Function GetTraceStr() As String
            Dim sb As New StringBuilder
            sb.AppendLine("Resultset count: " & Me.RowCount)
            sb.AppendLine("Execution time: " & Me.ExecutionTime.ToString)
            sb.AppendLine("Fetch time: " & Me.FetchTime.ToString)
            sb.AppendLine("Cache hit: " & Me.CacheHit)
            sb.AppendLine("Count of loaded objects in resultset: " & Me.LoadedInResultset)
            Return sb.ToString
        End Function
    End Structure

    Public Interface ICacheItemProvoderBase
        'Function GetValues(ByVal withLoad As Boolean) As ReadOnlyList(Of T)
        Property CacheMiss() As Boolean
        Property Renew() As Boolean
        ReadOnly Property SmartSort() As Boolean
        ReadOnly Property Sort() As OrderByClause
        'ReadOnly Property SortType() As SortType
        ReadOnly Property Filter() As IFilter
        Sub CreateDepends(ByVal ce As CachedItemBase)
        Function GetCacheItem(ByVal ctx As TypeWrap(Of Object)) As CachedItemBase
    End Interface

    Public Interface ICacheItemProvoder(Of T As {ICachedEntity})
        Inherits ICacheItemProvoderBase
        Overloads Function GetCacheItem(ByVal col As ReadOnlyEntityList(Of T)) As UpdatableCachedItem
    End Interface

    'Public Class PagerSwitcher
    '    Implements IDisposable

    '    Private _disposedValue As Boolean = False        ' To detect redundant calls
    '    Private _oldStart As Integer
    '    Private _oldLength As Integer
    '    Private _mgr As OrmManager
    '    Private _p As Query.IPager

    '    Public Sub New(ByVal mgr As OrmManager, ByVal start As Integer, ByVal length As Integer)
    '        _mgr = mgr
    '        _oldStart = mgr._start
    '        mgr._start = start
    '        _oldLength = mgr._length
    '        mgr._length = length
    '    End Sub

    '    Public Sub New(ByVal mgr As OrmManager, ByVal pager As Query.IPager)
    '        _mgr = mgr
    '        _p = pager
    '        AddHandler mgr.DataAvailable, AddressOf OnDataAvailable
    '    End Sub

    '    Public Sub New(ByVal start As Integer, ByVal length As Integer)
    '        MyClass.new(OrmManager.CurrentManager, start, length)
    '    End Sub

    '    Protected Sub OnDataAvailable(ByVal mgr As OrmManager, ByVal er As ExecutionResult)
    '        _p.SetTotalCount(er.RowCount)
    '        _oldStart = mgr._start
    '        mgr._start = _p.GetCurrentPageOffset
    '        _oldLength = mgr._length
    '        mgr._length = _p.GetPageSize
    '    End Sub

    '    ' IDisposable
    '    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
    '        If Not Me._disposedValue Then
    '            _mgr._length = _oldLength
    '            _mgr._start = _oldStart
    '            RemoveHandler _mgr.DataAvailable, AddressOf OnDataAvailable
    '        End If
    '        Me._disposedValue = True
    '    End Sub

    '    Public Sub Dispose() Implements IDisposable.Dispose
    '        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
    '        Dispose(True)
    '        GC.SuppressFinalize(Me)
    '    End Sub

    'End Class

    Public Class ApplyFilterHelper
        Implements IApplyFilter

        Private _f As IGetFilter
        Private _j As Criteria.Joins.QueryJoin()
        Private _eu As EntityUnion

        Public Sub New(f As IFilter)
            _f = f
        End Sub

        Public Sub New(f As IFilter, joins() As Criteria.Joins.QueryJoin, objEU As EntityUnion)
            _f = f
            _j = joins
            _eu = objEU
        End Sub

        Public ReadOnly Property Filter As Criteria.Core.IFilter Implements Criteria.Core.IApplyFilter.Filter
            Get
                Return _f.Filter
            End Get
        End Property

        Public ReadOnly Property Joins As Criteria.Joins.QueryJoin() Implements Criteria.Core.IApplyFilter.Joins
            Get
                Return _j
            End Get
        End Property

        Public ReadOnly Property RootObjectUnion As Query.EntityUnion Implements Criteria.Core.IApplyFilter.RootObjectUnion
            Get
                Return _eu
            End Get
        End Property
    End Class

    Public Class ApplyCriteria
        Implements IDisposable

        Private _disposedValue As Boolean = False        ' To detect redundant calls
        Private _f As IApplyFilter
        Private _mgr As OrmManager

        Public Sub New(ByVal f As IApplyFilter)
            Throw New NotSupportedException
            _mgr = OrmManager.CurrentManager
            _f = _mgr._externalFilter
            _mgr._externalFilter = f
        End Sub

        Public Sub New(ByVal c As IGetFilter)
            MyClass.new(New ApplyFilterHelper(GetFilter(c)))
        End Sub

        Protected Shared Function GetFilter(ByVal c As IGetFilter) As IFilter
            If c IsNot Nothing Then
                Return CType(c.Filter, IFilter)
            End If
            Return Nothing
        End Function

        Public Sub New(ByVal mgr As OrmManager, ByVal f As IApplyFilter)
            _mgr = mgr
            _f = mgr._externalFilter
            mgr._externalFilter = f
        End Sub

        Protected Friend ReadOnly Property oldfilter() As IApplyFilter
            Get
                Return _f
            End Get
        End Property

        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not Me._disposedValue Then
                If disposing Then
                    _mgr._externalFilter = _f
                End If
            End If
            Me._disposedValue = True
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
    End Class

    Public MustInherit Class CustDelegateBase(Of T As {ICachedEntity})
        Implements ICacheItemProvoder(Of T)

        Private _created As Boolean
        Private _renew As Boolean

        Protected Sub New()

        End Sub

        Public Overridable Property Renew() As Boolean Implements ICacheItemProvoderBase.Renew
            Get
                Return _renew
            End Get
            Set(ByVal Value As Boolean)
                _renew = Value
            End Set
        End Property

        Public Overridable Property Created() As Boolean Implements ICacheItemProvoderBase.CacheMiss
            Get
                Return _created
            End Get
            Set(ByVal Value As Boolean)
                _created = Value
            End Set
        End Property

        Public Overridable ReadOnly Property SmartSort() As Boolean Implements ICacheItemProvoderBase.SmartSort
            Get
                Return True
            End Get
        End Property

        Public MustOverride Function GetEntities(ByVal withLoad As Boolean) As ReadOnlyEntityList(Of T) 'Implements ICustDelegate(Of T).GetValues
        Public MustOverride Sub CreateDepends()
        Public MustOverride ReadOnly Property Filter() As IFilter Implements ICacheItemProvoderBase.Filter
        Public MustOverride ReadOnly Property Sort() As OrderByClause Implements ICacheItemProvoderBase.Sort
        'Public MustOverride ReadOnly Property SortType() As SortType Implements ICustDelegate(Of T).SortType
        Public MustOverride Function GetCacheItem(ByVal withLoad As Boolean) As UpdatableCachedItem

        Private Sub CreateDepends(ByVal ce As CachedItemBase) Implements ICacheItemProvoderBase.CreateDepends
            CType(ce, UpdatableCachedItem).Filter = Filter
            CType(ce, UpdatableCachedItem).Sort = Sort
            CreateDepends()
        End Sub

        Private Function GetCacheItem(ByVal ctx As TypeWrap(Of Object)) As CachedItemBase Implements ICacheItemProvoderBase.GetCacheItem
            Return GetCacheItem(CType(ctx.Value, Boolean())(0))
        End Function

        Public MustOverride Function GetCacheItem(ByVal col As ReadOnlyEntityList(Of T)) As UpdatableCachedItem Implements ICacheItemProvoder(Of T).GetCacheItem
    End Class

    Public MustInherit Class CustDelegate(Of T As {ISinglePKEntity, New})
        Inherits CustDelegateBase(Of T)

        Public Overrides Function GetEntities(ByVal withLoad As Boolean) As ReadOnlyEntityList(Of T)
            Return GetValues(withLoad)
        End Function

        Public MustOverride Function GetValues(ByVal withLoad As Boolean) As ReadOnlyList(Of T)
    End Class

    Public Property GetCreateManager As ICreateManager
        Get
            Return _crMan
        End Get
        Friend Set(value As ICreateManager)
            _crMan = value
        End Set
    End Property

    Protected Delegate Function LoadObjectFromStorageDelegate(ByVal obj As Object, _
            ByVal selectList As IList(Of SelectExpression), _
            ByVal entityDictionary As IDictionary, ByVal modificationSync As Boolean, ByRef lock As IDisposable, _
            ByVal oschema As IEntitySchema,
            ByVal propertyMap As Collections.IndexedCollection(Of String, MapField2Column), _
            ByVal rownum As Integer, ByVal baseIdx As Integer) As Object

    Protected Class LoadTypeDescriptor
        Public Load As Boolean
        Public Properties2Load As List(Of EntityExpression)
        Public ChildPropertyAlias As String
        'Public arr As List(Of EntityPropertyAttribute)
        Public EntitySchema As IEntitySchema
        'Public PI As Reflection.PropertyInfo

        Private _props As New List(Of Pair(Of String, Reflection.PropertyInfo))
        Public ReadOnly Property ParentProperties() As IList(Of Pair(Of String, Reflection.PropertyInfo))
            Get
                Return _props
            End Get
        End Property

        Public Sub New(ByVal load As Boolean, ByVal cols As List(Of EntityExpression), _
                       ByVal oschema As IEntitySchema)
            Me.Load = load
            Me.Properties2Load = cols
            'Me.arr = arr
            Me.EntitySchema = oschema
        End Sub
    End Class

End Class
