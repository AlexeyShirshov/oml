Imports Worm.Entities.Meta
Imports Worm.Entities
Imports Worm.Cache
Imports Worm.Sorting
Imports Worm.Criteria.Core
Imports System.Collections.Generic

Partial Public Class OrmManager

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
            Dim oschema As IEntitySchema = schema.GetObjectSchema(obj.GetType)
            o1 = CType(obj.GetValueOptimized(Nothing, p1.PropertyName, oschema), IKeyEntity)
            o2 = CType(obj.GetValueOptimized(Nothing, p2.PropertyName, oschema), IKeyEntity)
        End Sub

        Public Function Add(ByVal mgr As OrmManager, ByVal e As M2MCache) As Boolean
            If e Is Nothing Then
                Throw New ArgumentNullException("e")
            End If

            Dim el As EditableList = e.Entry
            Dim obj As IKeyEntity = Nothing, subobj As IKeyEntity = Nothing
            Dim main As IKeyEntity = mgr.GetOrmBaseFromCacheOrCreate(el.MainId, el.MainType)
            If Main.Equals(o1) Then
                obj = o1
                subobj = o2
            ElseIf Main.Equals(o2) Then
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

            Dim el As EditableList = e.Entry
            Dim main As IKeyEntity = mgr.GetOrmBaseFromCacheOrCreate(el.MainId, el.MainType)
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

            Dim el As EditableList = e.Entry
            Dim main As IKeyEntity = mgr.GetOrmBaseFromCacheOrCreate(el.MainId, el.MainType)
            If main.Equals(o1) OrElse main.Equals(o2) Then
                Return el.Accept(mgr)
            End If
            Return True
        End Function

        Public Function Reject(ByVal mgr As OrmManager, ByVal e As M2MCache) As Boolean
            If e Is Nothing Then
                Throw New ArgumentNullException("e")
            End If

            Dim el As EditableList = e.Entry
            Dim main As IKeyEntity = mgr.GetOrmBaseFromCacheOrCreate(el.MainId, el.MainType)
            If main.Equals(o1) OrElse main.Equals(o2) Then
                el.Reject(mgr, False)
            End If
            Return True
        End Function
    End Class

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
            CType(obj, _IEntity).SetSpecificSchema(mgr.MappingEngine)
        End Sub

        Public Sub New(ByVal schema As ObjectMappingEngine, ByVal mgr As OrmManager)
            _mgr = mgr
            _oldSchema = mgr.MappingEngine
            mgr.SetSchema(schema)
            '_r = mgr.RaiseObjectCreation
            If Not _oldSchema.Equals(schema) Then
                'mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectLoaded, AddressOf ObjectCreated
            End If
        End Sub

        ' IDisposable
        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not Me._disposedValue Then
                _mgr.SetSchema(_oldSchema)
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

    Public Class CachedItem
        Protected _sort As Sort
        'Protected _st As SortType
        Protected _obj As Object
        'Protected _mark As Object
        'Protected _cache As OrmBase
        Protected _f As IFilter
        Protected Friend _expires As Date
        Protected _sortExpires As Date
        Protected _execTime As TimeSpan
        Protected _fetchTime As TimeSpan

        'Public Sub New(ByVal sort As String, ByVal obj As Object)
        '    If sort Is Nothing Then sort = String.Empty
        '    _sort = sort
        '    _obj = obj
        'End Sub

        'Public Sub New(ByVal sort As String, ByVal sortType As SortType, ByVal obj As IEnumerable, ByVal mark As Object, ByVal mc As OrmManager)
        '    _sort = sort
        '    _st = sortType
        '    _obj = mc.ListConverter.ToWeakList(obj)
        '    _mark = mark
        '    _cache = mc.Cache
        '    If obj IsNot Nothing Then _cache.RegisterCreationCacheItem()
        'End Sub

        Protected Sub New()
        End Sub

        Public Sub New(ByVal sort As Sort, ByVal sortExpire As Date, ByVal filter As IFilter, _
            ByVal obj As IEnumerable, ByVal mgr As OrmManager)
            If sort IsNot Nothing Then
                _sort = CType(sort.Clone, Sorting.Sort)
            End If
            '_st = sortType
            'Using p As New CoreFramework.Debuging.OutputTimer("To week list")
            _obj = mgr.ListConverter.ToWeakList(obj)
            'End Using
            '_cache = mgr.Cache
            If obj IsNot Nothing Then mgr._cache.RegisterCreationCacheItem(Me.GetType)
            _f = filter
            _expires = mgr._expiresPattern
            _sortExpires = sortExpire
            _execTime = mgr.Exec
            _fetchTime = mgr.Fecth
        End Sub

        Friend Sub New(ByVal obj As IEnumerable, ByVal cache As CacheBase)
            _obj = obj
            '_cache = cache
            If obj IsNot Nothing Then cache.RegisterCreationCacheItem(Me.GetType)
        End Sub

        Public Sub New(ByVal filter As IFilter, ByVal obj As IEnumerable, ByVal mgr As OrmManager)
            _sort = Nothing
            '_st = sortType
            'Using p As New CoreFramework.Debuging.OutputTimer("To week list")
            _obj = mgr.ListConverter.ToWeakList(obj)
            'End Using
            '_cache = mgr.Cache
            If obj IsNot Nothing Then mgr._cache.RegisterCreationCacheItem(Me.GetType)
            _f = filter
            _expires = mgr._expiresPattern
            _execTime = mgr.Exec
            _fetchTime = mgr.Fecth
        End Sub

        Public Overridable ReadOnly Property CanRenewAfterSort() As Boolean
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property ExecutionTime() As TimeSpan
            Get
                Return _execTime
            End Get
        End Property

        Public ReadOnly Property FetchTime() As TimeSpan
            Get
                Return _fetchTime
            End Get
        End Property

        'Public Shared Function CreateEmpty(ByVal sort As String) As CachedItem
        '    Return New CachedItem(sort, Nothing, Nothing, OrmManager.CurrentManager)
        'End Function

        'Public Overrides Function Equals(ByVal obj As Object) As Boolean
        '    Return Equals(TryCast(obj, CachedItem))
        'End Function

        'Public Overrides Function GetHashCode() As Integer
        '    Return MyBase.GetHashCode()
        'End Function

        'Protected Function GetSortExp() As String
        '    If _sort IsNot Nothing Then
        '        Return _sort.ToString
        '    End If
        '    Return Nothing
        'End Function

        'Public Overloads Function Equals(ByVal obj As CachedItem) As Boolean
        '    If obj Is Nothing Then Return False

        '    Dim sortExpression As String = GetSortExp()
        '    If Not String.IsNullOrEmpty(sortExpression) Then
        '        'If _mark Is Nothing Then
        '        Return sortExpression.Equals(obj.GetSortExp)
        '        'ElseIf obj._mark IsNot Nothing Then
        '        '    Return sortExpression.Equals(obj.GetSortExp) AndAlso _mark.Equals(obj._mark)
        '        'End If
        '    ElseIf String.IsNullOrEmpty(obj.GetSortExp) Then
        '        Return True
        '    End If
        '    Return False
        'End Function

        Public Overridable Function GetCount(ByVal mgr As OrmManager) As Integer
            Return mgr.Cache.ListConverter.GetCount(_obj)
        End Function

        Public Sub Expire()
            _expires = Nothing
            SortExpire()
        End Sub

        Public Sub SortExpire()
            _sortExpires = Nothing
        End Sub

        Public ReadOnly Property Expires() As Boolean
            Get
                If _expires <> Date.MinValue Then
                    Return _expires < Date.Now
                End If
                Return False
            End Get
        End Property

        Public ReadOnly Property SortExpires() As Boolean
            Get
                If _sortExpires <> Date.MinValue Then
                    Return _sortExpires < Date.Now
                End If
                Return False
            End Get
        End Property

        Public Function SortEquals(ByVal sort As Sort) As Boolean
            If _sort Is Nothing Then
                If sort Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            Else
                Return _sort.Equals(sort)
            End If
        End Function

        Public Overridable Function GetObjectList(Of T As {_ICachedEntity})(ByVal mgr As OrmManager, _
            ByVal withLoad As Boolean, ByVal created As Boolean, ByRef successed As IListObjectConverter.ExtractListResult) As ReadOnlyEntityList(Of T)
            'Using p As New CoreFramework.Debuging.OutputTimer("From week list")
            Return mgr.ListConverter.FromWeakList(Of T)(_obj, mgr, mgr.GetStart, mgr.GetLength, withLoad, created, successed)
            'End Using
        End Function

        Public Overridable Function GetObjectList(Of T As {_ICachedEntity})(ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T)
            Return mgr.ListConverter.FromWeakList(Of T)(_obj, mgr)
        End Function

        'Public Sub SetObjectList(ByVal mc As OrmManager, ByVal value As OrmBase())
        '    _obj = mc.ListConverter.ToWeakList(value)
        'End Sub

        'Public ReadOnly Property Mark() As Object
        '    Get
        '        Return _mark
        '    End Get
        'End Property

        Shared Operator =(ByVal o1 As CachedItem, ByVal o2 As CachedItem) As Boolean
            If o1 Is Nothing Then
                If o2 Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            Else
                Return o1.Equals(o2)
            End If
        End Operator

        Shared Operator <>(ByVal o1 As CachedItem, ByVal o2 As CachedItem) As Boolean
            Return Not (o1 = o2)
        End Operator

        Public ReadOnly Property Sort() As Sort
            Get
                Return _sort
            End Get
        End Property

        'Protected Overrides Sub Finalize()
        '    If _obj IsNot Nothing Then _cache.RegisterRemovalCacheItem(Me)
        'End Sub

        Public Overridable Function Add(ByVal mgr As OrmManager, ByVal obj As ICachedEntity) As Boolean
            Return mgr.ListConverter.Add(_obj, mgr, obj, _sort)
        End Function

        Public Overridable Sub Delete(ByVal mgr As OrmManager, ByVal obj As ICachedEntity)
            mgr.ListConverter.Delete(_obj, obj)
        End Sub

        Public ReadOnly Property Filter() As IFilter
            Get
                Return _f
            End Get
        End Property

        Friend ReadOnly Property Obj() As Object
            Get
                Return _obj
            End Get
        End Property
    End Class

    Public Class M2MCache
        Inherits CachedItem

        Public Sub New(ByVal sort As Sort, ByVal filter As IFilter, _
            ByVal mainId As Object, ByVal obj As IList(Of Object), ByVal mgr As OrmManager, _
            ByVal mainType As Type, ByVal subType As Type, ByVal direct As Boolean)
            MyClass.new(sort, filter, mainId, obj, mgr, mainType, subType, M2MRelation.GetKey(direct))
        End Sub

        Public Sub New(ByVal sort As Sort, ByVal filter As IFilter, _
            ByVal mainId As Object, ByVal obj As IList(Of Object), ByVal mgr As OrmManager, _
            ByVal mainType As Type, ByVal subType As Type, ByVal key As String)
            If sort IsNot Nothing Then
                _sort = CType(sort.Clone, Sorting.Sort)
            End If

            '_st = sortType
            '_cache = mgr.Cache
            If obj IsNot Nothing Then
                mgr._cache.RegisterCreationCacheItem(Me.GetType)
                _obj = New EditableList(mainId, obj, mainType, subType, key, sort)
            End If
            _f = filter
            _expires = mgr._expiresPattern
            _execTime = mgr.Exec
            _fetchTime = mgr.Fecth
        End Sub

        Public Sub New(ByVal sort As Sort, ByVal filter As IFilter, _
            ByVal el As EditableList, ByVal mgr As OrmManager)
            If sort IsNot Nothing Then
                _sort = CType(sort.Clone, Sorting.Sort)
            End If

            '_st = SortType
            '_cache = mgr.Cache
            If el IsNot Nothing Then
                mgr._cache.RegisterCreationCacheItem(Me.GetType)
                _obj = el
            End If
            _f = filter
            _expires = mgr._expiresPattern
            _execTime = mgr.Exec
            _fetchTime = mgr.Fecth
        End Sub

        Public Overrides ReadOnly Property CanRenewAfterSort() As Boolean
            Get
                Return Not Entry.HasChanges
            End Get
        End Property

        'Public ReadOnly Property List() As EditableList
        '    Get
        '        Return CType(_obj, EditableList)
        '    End Get
        'End Property

        Public Function GetObjectListNonGeneric(ByVal mgr As OrmManager, _
            ByVal withLoad As Boolean, ByVal created As Boolean, ByRef successed As IListObjectConverter.ExtractListResult) As ICollection

            Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public

            Dim types As Type() = New Type() {GetType(OrmManager), GetType(Boolean), GetType(Boolean), GetType(IListObjectConverter.ExtractListResult)}

            Dim mi As Reflection.MethodInfo = GetType(OrmManager).GetMethod("GetObjectList", flags, Nothing, Reflection.CallingConventions.Any, types, Nothing)
            Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {Entry.SubType})

            Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {mgr, withLoad, created, New IListObjectConverter.ExtractListResult}, Nothing), System.Collections.ICollection)
        End Function

        Public Overrides Function GetObjectList(Of T As {_ICachedEntity})(ByVal mgr As OrmManager, _
            ByVal withLoad As Boolean, ByVal created As Boolean, ByRef successed As IListObjectConverter.ExtractListResult) As ReadOnlyEntityList(Of T)
            successed = IListObjectConverter.ExtractListResult.Successed
            Dim tt As Type = GetType(T)
            Dim r As ReadOnlyEntityList(Of T) = CType(OrmManager.CreateReadonlyList(tt), Global.Worm.ReadOnlyEntityList(Of T))
            If withLoad OrElse mgr._externalFilter IsNot Nothing Then
                Dim c As Integer = mgr.GetLoadedCount(tt, Entry.Current)
                Dim cnt As Integer = Entry.CurrentCount
#If TraceM2M Then
                Debug.WriteLine(Environment.StackTrace)
                Debug.WriteLine(cnt)
#End If
                If c < cnt Then
                    If mgr._externalFilter IsNot Nothing AndAlso Not OrmManager.IsGoodTime4Load(_fetchTime, _execTime, cnt, c) Then
                        successed = IListObjectConverter.ExtractListResult.NeedLoad
                        Return r
                    Else
                        r = mgr.LoadObjectsIds(Of T)(tt, Entry.Current, mgr.GetStart, mgr.GetLength)
                    End If
                Else
                    'r = mgr.LoadObjectsIds(Of T)(tt, Entry.Current, mgr.GetStart, mgr.GetLength)
                    r = CType(mgr.ConvertIds2Objects(tt, Entry.Current, mgr.GetStart, mgr.GetLength, False), Global.Worm.ReadOnlyEntityList(Of T))
                End If
                Dim s As Boolean = True
                r = CType(mgr.ApplyFilter(r, mgr._externalFilter, s), Global.Worm.ReadOnlyEntityList(Of T))
                If Not s Then
                    successed = IListObjectConverter.ExtractListResult.CantApplyFilter
                End If
            Else
                Try
#If TraceM2M Then
                    Debug.WriteLine(Entry.Current.Count)
#End If
                    r = CType(mgr.ConvertIds2Objects(tt, Entry.Current, mgr.GetStart, mgr.GetLength, False), Global.Worm.ReadOnlyEntityList(Of T))
                Catch ex As InvalidOperationException When ex.Message.StartsWith("Cannot prepare current data view")
                    r = CType(mgr.ConvertIds2Objects(tt, Entry.Original, mgr.GetStart, mgr.GetLength, False), Global.Worm.ReadOnlyEntityList(Of T))
                End Try
            End If
            Return r
        End Function

        Public Overrides Function GetObjectList(Of T As {_ICachedEntity})(ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T)
            Try
#If TraceM2M Then
                Debug.WriteLine(Entry.Current.Count)
#End If
                Return CType(mgr.ConvertIds2Objects(GetType(T), Entry.Current, False), Global.Worm.ReadOnlyEntityList(Of T))
            Catch ex As InvalidOperationException When ex.Message.StartsWith("Cannot prepare current data view")
                Return CType(mgr.ConvertIds2Objects(GetType(T), Entry.Original, False), Global.Worm.ReadOnlyEntityList(Of T))
            End Try
        End Function

        Public Overrides Function Add(ByVal mgr As OrmManager, ByVal obj As ICachedEntity) As Boolean
            'If obj Is Nothing Then
            '    Throw New ArgumentNullException("obj")
            'End If
            'If Entry IsNot Nothing Then
            '    Entry.Add(obj.Identifier)
            '    Entry.Accept()
            'End If
            Throw New NotSupportedException
        End Function

        Public Overrides Sub Delete(ByVal mgr As OrmManager, ByVal obj As ICachedEntity)
            'If obj Is Nothing Then
            '    Throw New ArgumentNullException("obj")
            'End If
            'If Entry IsNot Nothing Then
            '    Entry.Delete(obj.Identifier)
            '    Entry.Accept()
            'End If
            Throw New NotSupportedException
        End Sub

        Public ReadOnly Property Entry() As EditableList
            Get
                Return CType(_obj, EditableList)
            End Get
        End Property

        Public Overrides Function GetCount(ByVal mgr As OrmManager) As Integer
            Return Entry.CurrentCount
        End Function
    End Class

    Public Structure ExecutionResult
        Public ReadOnly Count As Integer
        Public ReadOnly ExecutionTime As TimeSpan
        Public ReadOnly FetchTime As TimeSpan
        Public ReadOnly CacheHit As Boolean
        Public ReadOnly LoadedInResultset As Nullable(Of Integer)

        Public Sub New(ByVal count As Integer, ByVal execTime As TimeSpan, ByVal fetchTime As TimeSpan, _
            ByVal hit As Boolean, ByVal loaded As Nullable(Of Integer))
            Me.Count = count
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
            sb.AppendLine("Resultset count: " & Me.Count)
            sb.AppendLine("Execution time: " & Me.ExecutionTime.ToString)
            sb.AppendLine("Fetch time: " & Me.FetchTime.ToString)
            sb.AppendLine("Cache hit: " & Me.CacheHit)
            sb.AppendLine("Count of loaded objects in resultset: " & Me.LoadedInResultset)
            Return sb.ToString
        End Function
    End Structure

    Public Interface ICacheItemProvoderBase
        'Function GetValues(ByVal withLoad As Boolean) As ReadOnlyList(Of T)
        Property Created() As Boolean
        Property Renew() As Boolean
        ReadOnly Property SmartSort() As Boolean
        ReadOnly Property Sort() As Sort
        'ReadOnly Property SortType() As SortType
        ReadOnly Property Filter() As IFilter
        Sub CreateDepends()
        Function GetCacheItem(ByVal withLoad As Boolean) As CachedItem
    End Interface

    Public Interface ICacheItemProvoder(Of T As {ICachedEntity})
        Inherits ICacheItemProvoderBase
        Overloads Function GetCacheItem(ByVal col As ReadOnlyEntityList(Of T)) As CachedItem
    End Interface

    Public Class PagerSwitcher
        Implements IDisposable

        Private _disposedValue As Boolean = False        ' To detect redundant calls
        Private _oldStart As Integer
        Private _oldLength As Integer
        Private _mgr As OrmManager
        Private _p As Web.IPager

        Public Sub New(ByVal mgr As OrmManager, ByVal start As Integer, ByVal length As Integer)
            _mgr = mgr
            _oldStart = mgr._start
            mgr._start = start
            _oldLength = mgr._length
            mgr._length = length
        End Sub

        Public Sub New(ByVal mgr As OrmManager, ByVal pager As Web.IPager)
            _mgr = mgr
            _p = pager
            AddHandler mgr.DataAvailable, AddressOf OnDataAvailable
        End Sub

        Public Sub New(ByVal start As Integer, ByVal length As Integer)
            MyClass.new(OrmManager.CurrentManager, start, length)
        End Sub

        Protected Sub OnDataAvailable(ByVal mgr As OrmManager, ByVal er As ExecutionResult)
            _p.SetTotalCount(er.Count)
            _oldStart = mgr._start
            mgr._start = _p.GetCurrentPageOffset
            _oldLength = mgr._length
            mgr._length = _p.GetPageSize
        End Sub

        ' IDisposable
        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not Me._disposedValue Then
                _mgr._length = _oldLength
                _mgr._start = _oldStart
                RemoveHandler _mgr.DataAvailable, AddressOf OnDataAvailable
            End If
            Me._disposedValue = True
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

    End Class

    Public Class ApplyCriteria
        Implements IDisposable

        Private _disposedValue As Boolean = False        ' To detect redundant calls
        Private _f As IFilter
        Private _mgr As OrmManager

        Public Sub New(ByVal f As IFilter)
            _mgr = OrmManager.CurrentManager
            _f = _mgr._externalFilter
            _mgr._externalFilter = f
        End Sub

        Public Sub New(ByVal c As IGetFilter)
            MyClass.new(GetFilter(c))
        End Sub

        Protected Shared Function GetFilter(ByVal c As IGetFilter) As IFilter
            If c IsNot Nothing Then
                Return CType(c.Filter, IFilter)
            End If
            Return Nothing
        End Function

        Public Sub New(ByVal mgr As OrmManager, ByVal f As IFilter)
            _mgr = mgr
            _f = mgr._externalFilter
            mgr._externalFilter = f
        End Sub

        Protected Friend ReadOnly Property oldfilter() As IFilter
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

    Public Interface ICacheValidator
        Function ValidateItemFromCache(ByVal ce As CachedItem) As Boolean
        Function ValidateBeforCacheProbe() As Boolean
    End Interface

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

        Public Overridable Property Created() As Boolean Implements ICacheItemProvoderBase.Created
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
        Public MustOverride Sub CreateDepends() Implements ICacheItemProvoderBase.CreateDepends
        Public MustOverride ReadOnly Property Filter() As IFilter Implements ICacheItemProvoderBase.Filter
        Public MustOverride ReadOnly Property Sort() As Sort Implements ICacheItemProvoderBase.Sort
        'Public MustOverride ReadOnly Property SortType() As SortType Implements ICustDelegate(Of T).SortType
        Public MustOverride Function GetCacheItem(ByVal withLoad As Boolean) As CachedItem Implements ICacheItemProvoderBase.GetCacheItem
        Public MustOverride Function GetCacheItem(ByVal col As ReadOnlyEntityList(Of T)) As CachedItem Implements ICacheItemProvoder(Of T).GetCacheItem
    End Class

    Public MustInherit Class CustDelegate(Of T As {IKeyEntity, New})
        Inherits CustDelegateBase(Of T)

        Public Overrides Function GetEntities(ByVal withLoad As Boolean) As ReadOnlyEntityList(Of T)
            Return GetValues(withLoad)
        End Function

        Public MustOverride Function GetValues(ByVal withLoad As Boolean) As ReadOnlyList(Of T)
    End Class

End Class
