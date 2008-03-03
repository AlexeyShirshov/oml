Imports Worm.Criteria
Imports Worm.Criteria.Joins
Imports Worm.Cache
Imports Worm.Orm.Meta
Imports Worm.Orm.Query
Imports Worm.Criteria.Values
Imports Worm.Criteria.Core
Imports Worm.Sorting
Imports Worm.Orm
Imports System.Collections.Generic

#Const DontUseStringIntern = True


'Namespace Managers

<Serializable()> _
Public NotInheritable Class OrmManagerException
    Inherits Exception

    Public Sub New()
        ' Add other code for custom properties here.
    End Sub

    Public Sub New(ByVal message As String)
        MyBase.New(message)
        ' Add other code for custom properties here.
    End Sub

    Public Sub New(ByVal message As String, ByVal inner As Exception)
        MyBase.New(message, inner)
        ' Add other code for custom properties here.
    End Sub

    Private Sub New( _
        ByVal info As System.Runtime.Serialization.SerializationInfo, _
        ByVal context As System.Runtime.Serialization.StreamingContext)
        MyBase.New(info, context)
        ' Insert code here for custom properties here.
    End Sub
End Class

Public MustInherit Class OrmManagerBase
    Implements IDisposable

#Region " Interfaces and classes "

    Friend Class M2MEnum
        Public ReadOnly o As IRelation
        'Public ReadOnly obj As OrmBase
        Dim p1 As IRelation.RelationDesc 'Pair(Of String, Type)
        Dim p2 As IRelation.RelationDesc 'Pair(Of String, Type)
        Dim o1 As OrmBase
        Dim o2 As OrmBase

        Public Sub New(ByVal r As IRelation, ByVal obj As OrmBase, ByVal schema As OrmSchemaBase)
            Me.o = r
            'Me.obj = obj
            p1 = o.GetFirstType
            p2 = o.GetSecondType
            'o1 = CType(schema.GetFieldValue(obj, p1.First), OrmBase)
            'o2 = CType(schema.GetFieldValue(obj, p2.First), OrmBase)
            o1 = CType(obj.GetValue(p1.PropertyName), OrmBase)
            o2 = CType(obj.GetValue(p2.PropertyName), OrmBase)
        End Sub

        Public Function Add(ByVal e As M2MCache) As Boolean
            If e Is Nothing Then
                Throw New ArgumentNullException("e")
            End If

            Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
            Dim el As EditableList = e.Entry
            Dim obj As OrmBase = Nothing, subobj As OrmBase = Nothing
            If el.Main.Equals(o1) Then
                obj = o1
                subobj = o2
            ElseIf el.Main.Equals(o2) Then
                obj = o2
                subobj = o1
            End If

            If obj IsNot Nothing Then
                If e.Sort Is Nothing Then
                    el.Add(subobj.Identifier)
                Else
                    Dim s As IOrmSorting = Nothing
                    Dim col As New ArrayList(mgr.ConvertIds2Objects(el.SubType, el.Added, False))
                    If Not mgr.CanSortOnClient(el.SubType, col, e.Sort, s) Then
                        Return False
                    End If
                    Dim c As IComparer = Nothing
                    If s Is Nothing Then
                        c = New OrmComparer(Of OrmBase)(el.SubType, e.Sort)
                    Else
                        c = s.CreateSortComparer(e.Sort)
                    End If
                    If c Is Nothing Then
                        Return False
                    End If
                    Dim pos As Integer = col.BinarySearch(subobj, c)
                    If pos < 0 Then
                        el.Add(subobj.Identifier, Not pos)
                    End If
                End If
            End If
            Return True
        End Function

        Public Function Remove(ByVal e As M2MCache) As Boolean
            If e Is Nothing Then
                Throw New ArgumentNullException("e")
            End If

            Dim el As EditableList = e.Entry
            If el.Main.Equals(o1) Then
                el.Delete(o2.Identifier)
            ElseIf el.Main.Equals(o2) Then
                el.Delete(o1.Identifier)
            End If
            Return True
        End Function

        Public Function Accept(ByVal e As M2MCache) As Boolean
            If e Is Nothing Then
                Throw New ArgumentNullException("e")
            End If

            Dim el As EditableList = e.Entry
            If el.Main.Equals(o1) OrElse el.Main.Equals(o2) Then
                Return el.Accept(OrmManagerBase.CurrentManager)
            End If
            Return True
        End Function

        Public Function Reject(ByVal e As M2MCache) As Boolean
            If e Is Nothing Then
                Throw New ArgumentNullException("e")
            End If

            Dim el As EditableList = e.Entry
            If el.Main.Equals(o1) OrElse el.Main.Equals(o2) Then
                el.Reject(False)
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
        Private _oldSchema As OrmSchemaBase

        Public Sub New(ByVal schema As OrmSchemaBase)
            Dim mgr As OrmManagerBase = OrmManagerBase.CurrentManager
            _oldSchema = mgr.ObjectSchema
            mgr._schema = schema
        End Sub

        ' IDisposable
        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not Me._disposedValue Then
                OrmManagerBase.CurrentManager._schema = _oldSchema
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
        Private _mgr As OrmManagerBase
        Private _oldvalue As Boolean
        Private _oldExp As Date
        Private _oldMark As String

        Public Sub New(ByVal mgr As OrmManagerBase, ByVal cache_lists As Boolean)
            _mgr = mgr
            _oldvalue = mgr._dont_cache_lists
            mgr._dont_cache_lists = Not cache_lists
        End Sub

        Public Sub New(ByVal mgr As OrmManagerBase, ByVal liveTime As TimeSpan)
            _mgr = mgr
            _oldvalue = mgr._dont_cache_lists
            _oldExp = mgr._expiresPattern
            mgr._expiresPattern = Date.Now.Add(liveTime)
        End Sub

        Public Sub New(ByVal mgr As OrmManagerBase, ByVal cache_lists As Boolean, ByVal liveTime As TimeSpan)
            _mgr = mgr
            _oldvalue = mgr._dont_cache_lists
            mgr._dont_cache_lists = Not cache_lists
            _oldExp = mgr._expiresPattern
            mgr._expiresPattern = Date.Now.Add(liveTime)
        End Sub

        Public Sub New(ByVal mgr As OrmManagerBase, ByVal cache_lists As Boolean, ByVal mark As String)
            _mgr = mgr
            _oldvalue = mgr._dont_cache_lists
            mgr._dont_cache_lists = Not cache_lists
            _oldMark = mgr._list
            mgr._list = mark
        End Sub

        Public Sub New(ByVal mgr As OrmManagerBase, ByVal liveTime As TimeSpan, ByVal mark As String)
            _mgr = mgr
            _oldvalue = mgr._dont_cache_lists
            _oldExp = mgr._expiresPattern
            mgr._expiresPattern = Date.Now.Add(liveTime)
            _oldMark = mgr._list
            mgr._list = mark
        End Sub

        Public Sub New(ByVal mgr As OrmManagerBase, ByVal cache_lists As Boolean, ByVal liveTime As TimeSpan, ByVal mark As String)
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
        Protected _cache As OrmCacheBase
        Protected _f As IFilter
        Protected _expires As Date
        Protected _sortExpires As Date
        Protected _execTime As TimeSpan
        Protected _fetchTime As TimeSpan

        'Public Sub New(ByVal sort As String, ByVal obj As Object)
        '    If sort Is Nothing Then sort = String.Empty
        '    _sort = sort
        '    _obj = obj
        'End Sub

        'Public Sub New(ByVal sort As String, ByVal sortType As SortType, ByVal obj As IEnumerable, ByVal mark As Object, ByVal mc As OrmManagerBase)
        '    _sort = sort
        '    _st = sortType
        '    _obj = mc.ListConverter.ToWeakList(obj)
        '    _mark = mark
        '    _cache = mc.Cache
        '    If obj IsNot Nothing Then _cache.RegisterCreationCacheItem()
        'End Sub

        Protected Sub New()
        End Sub

        Public Sub New(ByVal sort As Sort, ByVal sortExpire As Date, ByVal filter As IFilter, ByVal obj As IEnumerable, ByVal mgr As OrmManagerBase)
            _sort = sort
            '_st = sortType
            'Using p As New CoreFramework.Debuging.OutputTimer("To week list")
            _obj = mgr.ListConverter.ToWeakList(obj)
            'End Using
            _cache = mgr.Cache
            If obj IsNot Nothing Then _cache.RegisterCreationCacheItem(Me.GetType)
            _f = filter
            _expires = mgr._expiresPattern
            _sortExpires = sortExpire
            _execTime = mgr.Exec
            _fetchTime = mgr.Fecth
        End Sub

        Public Sub New(ByVal filter As IFilter, ByVal obj As IEnumerable, ByVal mgr As OrmManagerBase)
            _sort = Nothing
            '_st = sortType
            'Using p As New CoreFramework.Debuging.OutputTimer("To week list")
            _obj = mgr.ListConverter.ToWeakList(obj)
            'End Using
            _cache = mgr.Cache
            If obj IsNot Nothing Then _cache.RegisterCreationCacheItem(Me.GetType)
            _f = filter
            _expires = mgr._expiresPattern
            _execTime = mgr.Exec
            _fetchTime = mgr.Fecth
        End Sub

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
        '    Return New CachedItem(sort, Nothing, Nothing, OrmManagerBase.CurrentManager)
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

        Public Overridable Function GetCount(ByVal mgr As OrmManagerBase) As Integer
            Return mgr._list_converter.GetCount(_obj)
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

        Public Overridable Function GetObjectList(Of T As {OrmBase, New})(ByVal mgr As OrmManagerBase, _
            ByVal withLoad As Boolean, ByVal created As Boolean, ByRef successed As IListObjectConverter.ExtractListResult) As ICollection(Of T)
            'Using p As New CoreFramework.Debuging.OutputTimer("From week list")
            Return mgr.ListConverter.FromWeakList(Of T)(_obj, mgr, mgr.GetStart, mgr.GetLength, withLoad, created, successed)
            'End Using
        End Function

        Public Overridable Function GetObjectList(Of T As {OrmBase, New})(ByVal mgr As OrmManagerBase) As ICollection(Of T)
            Return mgr.ListConverter.FromWeakList(Of T)(_obj, mgr)
        End Function

        'Public Sub SetObjectList(ByVal mc As OrmManagerBase, ByVal value As OrmBase())
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

        Protected Overrides Sub Finalize()
            If _obj IsNot Nothing Then _cache.RegisterRemovalCacheItem(Me)
        End Sub

        Public Overridable Function Add(ByVal mgr As OrmManagerBase, ByVal obj As OrmBase) As Boolean
            Return mgr.ListConverter.Add(_obj, mgr, obj, _sort)
        End Function

        Public Overridable Sub Delete(ByVal mgr As OrmManagerBase, ByVal obj As OrmBase)
            mgr.ListConverter.Delete(_obj, obj)
        End Sub

        Public ReadOnly Property Filter() As IFilter
            Get
                Return _f
            End Get
        End Property
    End Class

    Public Class M2MCache
        Inherits CachedItem

        Public Sub New(ByVal sort As Sort, ByVal filter As IFilter, _
            ByVal mainId As Integer, ByVal obj As IList(Of Integer), ByVal mgr As OrmManagerBase, _
            ByVal mainType As Type, ByVal subType As Type, ByVal direct As Boolean)
            _sort = sort
            '_st = sortType
            _cache = mgr.Cache
            If obj IsNot Nothing Then
                _cache.RegisterCreationCacheItem(Me.GetType)
                _obj = New EditableList(mainId, obj, mainType, subType, direct, sort)
            End If
            _f = filter
            _expires = mgr._expiresPattern
            _execTime = mgr.Exec
            _fetchTime = mgr.Fecth
        End Sub

        Public Sub New(ByVal sort As Sort, ByVal filter As IFilter, _
            ByVal el As EditableList, ByVal mgr As OrmManagerBase)
            _sort = sort
            '_st = SortType
            _cache = mgr.Cache
            If el IsNot Nothing Then
                _cache.RegisterCreationCacheItem(Me.GetType)
                _obj = el
            End If
            _f = filter
            _expires = mgr._expiresPattern
            _execTime = mgr.Exec
            _fetchTime = mgr.Fecth
        End Sub

        'Public ReadOnly Property List() As EditableList
        '    Get
        '        Return CType(_obj, EditableList)
        '    End Get
        'End Property

        Public Overrides Function GetObjectList(Of T As {New, OrmBase})(ByVal mgr As OrmManagerBase, _
            ByVal withLoad As Boolean, ByVal created As Boolean, ByRef successed As IListObjectConverter.ExtractListResult) As System.Collections.Generic.ICollection(Of T)
            successed = IListObjectConverter.ExtractListResult.Successed
            Dim r As ICollection(Of T) = Nothing
            If withLoad OrElse mgr._externalFilter IsNot Nothing Then
                Dim c As Integer = mgr.GetLoadedCount(Of T)(Entry.Current)
                Dim cnt As Integer = Entry.CurrentCount
                If c < cnt Then
                    If Not OrmManagerBase.IsGoodTime4Load(_fetchTime, _execTime, cnt, c) Then
                        successed = IListObjectConverter.ExtractListResult.NeedLoad
                        Return r
                    Else
                        r = mgr.LoadObjectsIds(Of T)(Entry.Current, mgr.GetStart, mgr.GetLength)
                    End If
                Else
                    r = mgr.LoadObjectsIds(Of T)(Entry.Current, mgr.GetStart, mgr.GetLength)
                End If
                Dim s As Boolean = True
                r = mgr.ApplyFilter(r, mgr._externalFilter, s)
                If Not s Then
                    successed = IListObjectConverter.ExtractListResult.CantApplyFilter
                End If
            Else
                Try
                    r = mgr.ConvertIds2Objects(Of T)(Entry.Current, mgr.GetStart, mgr.GetLength, False)
                Catch ex As InvalidOperationException When ex.Message.StartsWith("Cannot prepare current data view")
                    r = mgr.ConvertIds2Objects(Of T)(Entry.Original, mgr.GetStart, mgr.GetLength, False)
                End Try
            End If
            Return r
        End Function

        Public Overrides Function GetObjectList(Of T As {New, OrmBase})(ByVal mgr As OrmManagerBase) As System.Collections.Generic.ICollection(Of T)
            Try
                Return mgr.ConvertIds2Objects(Of T)(Entry.Current, False)
            Catch ex As InvalidOperationException When ex.Message.StartsWith("Cannot prepare current data view")
                Return mgr.ConvertIds2Objects(Of T)(Entry.Original, False)
            End Try
        End Function

        Public Overrides Function Add(ByVal mgr As OrmManagerBase, ByVal obj As OrmBase) As Boolean
            'If obj Is Nothing Then
            '    Throw New ArgumentNullException("obj")
            'End If
            'If Entry IsNot Nothing Then
            '    Entry.Add(obj.Identifier)
            '    Entry.Accept()
            'End If
            Throw New NotSupportedException
        End Function

        Public Overrides Sub Delete(ByVal mgr As OrmManagerBase, ByVal obj As OrmBase)
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

        Public Overrides Function GetCount(ByVal mgr As OrmManagerBase) As Integer
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
        End Sub
    End Structure

    Public Interface ICustDelegate(Of T As {OrmBase, New})
        Function GetValues(ByVal withLoad As Boolean) As Generic.ICollection(Of T)
        Property Created() As Boolean
        Property Renew() As Boolean
        ReadOnly Property SmartSort() As Boolean
        ReadOnly Property Sort() As Sort
        'ReadOnly Property SortType() As SortType
        ReadOnly Property Filter() As IFilter
        Sub CreateDepends()
        Function GetCacheItem(ByVal withLoad As Boolean) As CachedItem
        Function GetCacheItem(ByVal col As ICollection(Of T)) As CachedItem
    End Interface

    Public Class PagerSwitcher
        Implements IDisposable

        Private _disposedValue As Boolean = False        ' To detect redundant calls
        Private _oldStart As Integer
        Private _oldLength As Integer
        Private _mgr As OrmManagerBase

        Public Sub New(ByVal mgr As OrmManagerBase, ByVal start As Integer, ByVal length As Integer)
            _mgr = mgr
            _oldStart = mgr._start
            mgr._start = start
            _oldLength = mgr._length
            mgr._length = length
        End Sub

        Public Sub New(ByVal start As Integer, ByVal length As Integer)
            MyClass.new(OrmManagerBase.CurrentManager, start, length)
        End Sub

        ' IDisposable
        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not Me._disposedValue Then
                If disposing Then
                    _mgr._length = _oldLength
                    _mgr._start = _oldStart
                End If
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
        Private _mgr As OrmManagerBase

        Public Sub New(ByVal f As IFilter)
            _mgr = OrmManagerBase.CurrentManager
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

        Public Sub New(ByVal mgr As OrmManagerBase, ByVal f As IFilter)
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
        Function Validate(ByVal ce As CachedItem) As Boolean
        Function Validate() As Boolean
    End Interface

    Public Interface INewObjects
        Function GetIdentity() As Integer
        Function GetNew(ByVal t As Type, ByVal id As Integer) As OrmBase
        Sub AddNew(ByVal obj As OrmBase)
        Sub RemoveNew(ByVal obj As OrmBase)
        Sub RemoveNew(ByVal t As Type, ByVal id As Integer)
    End Interface

    Public Interface INewObjectsEx
        Inherits INewObjects
        Overloads Function GetNew(ByVal t As Type) As ICollection(Of OrmBase)
        Overloads Function GetNew(Of T As OrmBase)() As ICollection(Of T)
    End Interface

    Public MustInherit Class CustDelegate(Of T As {OrmBase, New})
        Implements ICustDelegate(Of T)

        Private _created As Boolean
        Private _renew As Boolean

        Protected Sub New()

        End Sub

        Public Overridable Property Renew() As Boolean Implements ICustDelegate(Of T).Renew
            Get
                Return _renew
            End Get
            Set(ByVal Value As Boolean)
                _renew = Value
            End Set
        End Property

        Public Overridable Property Created() As Boolean Implements ICustDelegate(Of T).Created
            Get
                Return _created
            End Get
            Set(ByVal Value As Boolean)
                _created = Value
            End Set
        End Property

        Public Overridable ReadOnly Property SmartSort() As Boolean Implements ICustDelegate(Of T).SmartSort
            Get
                Return True
            End Get
        End Property

        Public MustOverride Function GetValues(ByVal withLoad As Boolean) As Generic.ICollection(Of T) Implements ICustDelegate(Of T).GetValues
        Public MustOverride Sub CreateDepends() Implements ICustDelegate(Of T).CreateDepends
        Public MustOverride ReadOnly Property Filter() As IFilter Implements ICustDelegate(Of T).Filter
        Public MustOverride ReadOnly Property Sort() As Sort Implements ICustDelegate(Of T).Sort
        'Public MustOverride ReadOnly Property SortType() As SortType Implements ICustDelegate(Of T).SortType
        Public MustOverride Function GetCacheItem(ByVal withLoad As Boolean) As CachedItem Implements ICustDelegate(Of T).GetCacheItem
        Public MustOverride Function GetCacheItem(ByVal col As System.Collections.Generic.ICollection(Of T)) As CachedItem Implements ICustDelegate(Of T).GetCacheItem
    End Class

    'Public Interface IComplexDelegate
    '    Function GetDT(ByVal filter_key As String, ByVal main As OrmBase, _
    '        ByVal t As Type, ByVal sort As String, ByVal sort_type As SortType, _
    '        ByVal preserveSort As Boolean, ByVal mc As OrmManagerBase) As System.Data.DataTable

    'End Interface

#End Region

    Protected Friend Const Const_KeyStaticString As String = " - key -"
    Protected Friend Const Const_JoinStaticString As String = " - join - "
    Protected Friend Const GetTablePostfix As String = " - GetTable"
    Protected Const WithLoadConst As Boolean = False
    Protected Const myConstLocalStorageString As String = "afoivnaodfvodfviogb3159fhbdofvad"
    'Public Const CustomSort As String = "q890h5f130nmv90h1nv9b1v-9134fb"

    Protected _cache As OrmCacheBase
    'Private _dispose_cash As Boolean
    Friend _prev As OrmManagerBase = Nothing
    'Protected hide_deleted As Boolean = True
    'Protected _check_status As Boolean = True
    Protected _schema As OrmSchemaBase
    'Private _findnew As FindNew
    'Private _remnew As RemoveNew
    Protected Friend _disposed As Boolean = False
    'Protected _sites() As Partner
    Protected Shared _mcSwitch As New TraceSwitch("mcSwitch", "Switch for OrmManagerBase", "3") 'info
    Protected Shared _LoadObjectsMI As Reflection.MethodInfo = Nothing
    Private Shared _realCreateDbObjectDic As Hashtable = Hashtable.Synchronized(New Hashtable())
    Private Shared _realLoadTypeDic As Hashtable = Hashtable.Synchronized(New Hashtable())

    Protected _findload As Boolean = False
    '#If DEBUG Then
    '        Protected _next As OrmManagerBase
    '#End If
    'Public Delegate Function FindNew(ByVal type As Type, ByVal id As Integer) As OrmBase
    'Public Delegate Sub RemoveNew(ByVal type As Type, ByVal id As Integer)

    Protected _cs As String
    'Protected _prevs As String

    <ThreadStatic()> _
    Private Shared _cur As OrmManagerBase
    'Public Delegate Function GetLocalStorageDelegate(ByVal str As String) As Object
    'Protected _get_cur As GetLocalStorageDelegate

    'Public Function RegisterGetLocalStorage(ByVal getLocalStorage As GetLocalStorageDelegate) As GetLocalStorageDelegate
    '    Dim i As GetLocalStorageDelegate = _get_cur
    '    _get_cur = getLocalStorage
    '    Return i
    'End Function
    Private _list_converter As IListObjectConverter
    Protected Friend _dont_cache_lists As Boolean
    Private _newMgr As INewObjects
    Private _expiresPattern As Date
    Protected _start As Integer
    Protected _length As Integer = Integer.MaxValue
    Protected _er As ExecutionResult
    Friend _externalFilter As IFilter
    Protected Friend _loadedInLastFetch As Integer
    Private _list As String
#If TraceManagerCreation Then
        private _callstack as string
#End If
    Private Function GetStart() As Integer
        If _externalFilter IsNot Nothing Then
            Return 0
        End If
        Return _start
    End Function

    Private Function GetLength() As Integer
        If _externalFilter IsNot Nothing Then
            Return Integer.MaxValue
        End If
        Return _length
    End Function

    Public ReadOnly Property GetLastExecitionResult() As ExecutionResult
        Get
            Return _er
        End Get
    End Property

    Public Event BeginUpdate(ByVal o As OrmBase)
    Public Event BeginDelete(ByVal o As OrmBase)

    Public Delegate Function ValueForSearchDelegate(ByVal tokens() As String, ByVal sectionName As String, ByVal fs As IOrmFullTextSupport, ByVal contextKey As Object) As String

    <CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1805")> _
    Protected Sub New(ByVal cache As OrmCacheBase, ByVal schema As OrmSchemaBase)

        If cache Is Nothing Then
            Throw New ArgumentNullException("cache")
        End If

        If schema Is Nothing Then
            Throw New ArgumentNullException("schema")
        End If

        _cache = cache
        _schema = schema
        '_check_on_find = True

        _list_converter = CreateListConverter()
        CreateInternal()
    End Sub

    <CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1805")> _
    Protected Sub New(ByVal schema As OrmSchemaBase)
        '_dispose_cash = True
        _cache = New OrmCache
        _schema = schema
        _list_converter = CreateListConverter()
        '_check_on_find = True
        CreateInternal()
    End Sub

    Protected Overridable Function CreateListConverter() As IListObjectConverter
        Return New FakeListConverter
    End Function

    Protected Friend Overridable ReadOnly Property IdentityString() As String
        Get
            Return Me.GetType.ToString
        End Get
    End Property

    Protected Function IsIdentical(ByVal mgr As OrmManagerBase) As Boolean
        Return IdentityString = mgr.IdentityString
    End Function

    Protected Sub CreateInternal()
        _prev = CurrentManager
        Dim p As OrmManagerBase = _prev
#If DEBUG Then
        Do While p IsNot Nothing
            If p._schema.Version <> _schema.Version AndAlso IsIdentical(p) Then
                Throw New OrmManagerException("Cannot create nested managers with different schema versions")
            End If
            p = p._prev
        Loop
#End If
#If TraceManagerCreation Then
_callstack = environment.StackTrace
#End If
            'Thread.SetData(LocalStorage, Me)

            CurrentManager = Me
            '_cs = Environment.StackTrace.ToString
            'If prev IsNot Nothing Then
            '    _prevs = prev._s
            'End If
    End Sub

    'Public Property CheckOnFind() As Boolean
    '    Get
    '        Return _check_on_find
    '    End Get
    '    Set(ByVal value As Boolean)
    '        _check_on_find = value
    '    End Set
    'End Property

    Public ReadOnly Property ObjectSchema() As OrmSchemaBase
        Get
            Return _schema
        End Get
    End Property

    Public Sub ResetLocalStorage()
        If _prev IsNot Nothing Then
            Assert(Not _prev._disposed, "Previous MediaContent cannot be disposed. CallStack: " & _cs)
            'Assert(Not prev.disposed, "Previous MediaContent cannot be disposed. CallStack: " & _s & "PrevCallStack: " & _prevs)
            'Assert(Not prev.disposed, "Previous MediaContent cannot be disposed.")
        End If
        CurrentManager = _prev
        'Thread.SetData(LocalStorage, prev)
        ''If prev Is Nothing Then
        ''    Thread.FreeNamedDataSlot("MediaContent")
        ''End If
    End Sub

    'Protected Shared ReadOnly Property LocalStorage() As LocalDataStoreSlot
    '    Get
    '        Return Thread.GetNamedDataSlot("MediaContent")
    '    End Get
    'End Property

    Public ReadOnly Property ListConverter() As IListObjectConverter
        Get
            Return _list_converter
        End Get
        'Set(ByVal value As IListObjectConverter)
        '    _list_converter = value
        'End Set
    End Property

    Public Shared Property CurrentManager() As OrmManagerBase
        Get
            'If System.Web.HttpContext.Current IsNot Nothing Then
            '    Return CType(System.Web.HttpContext.Current.Items(myConstLocalStorageString), OrmManagerBase)
            'Else
            '    Return _cur
            'End If
            'Return CType(Thread.GetData(LocalStorage), OrmManagerBase)
            Return _cur
        End Get
        Protected Set(ByVal value As OrmManagerBase)
            'If System.Web.HttpContext.Current IsNot Nothing Then
            '    System.Web.HttpContext.Current.Items(myConstLocalStorageString) = value
            'Else
            '    _cur = value
            'End If
            _cur = value
        End Set
    End Property

    Public Overridable ReadOnly Property CurrentUser() As Object
        Get
            Return Nothing
        End Get
    End Property

    Public ReadOnly Property Cache() As OrmCacheBase
        Get
            Invariant()
            Return _cache
        End Get
    End Property

    Friend Sub RaiseBeginUpdate(ByVal o As OrmBase)
        RaiseEvent BeginUpdate(o)
    End Sub

    Friend Sub RaiseBeginDelete(ByVal o As OrmBase)
        RaiseEvent BeginDelete(o)
    End Sub

    'Public Property FindNewDelegate() As FindNew
    '    Get
    '        Return _findnew
    '    End Get
    '    Set(ByVal value As FindNew)
    '        _findnew = value
    '    End Set
    'End Property

    'Public Property RemoveNewDelegate() As RemoveNew
    '    Get
    '        Invariant()
    '        Return _remnew
    '    End Get
    '    Set(ByVal value As RemoveNew)
    '        Invariant()
    '        _remnew = value
    '    End Set
    'End Property

    Public Property NewObjectManager() As INewObjects
        Get
            Return _newMgr
        End Get
        Set(ByVal value As INewObjects)
            _newMgr = value
        End Set
    End Property

    Public Function IsNewObject(ByVal t As Type, ByVal id As Integer) As Boolean
        Return _newMgr IsNot Nothing AndAlso _newMgr.GetNew(t, id) IsNot Nothing
    End Function

    ' IDisposable
    Protected Overridable Overloads Sub Dispose(ByVal disposing As Boolean)
        SyncLock Me.GetType
            If Not Me._disposed Then
                ResetLocalStorage()
                '#If DEBUG Then
                '                    Assert(_next Is Nothing OrElse _next.disposed = True, "MediaContent disposing before the next")
                '#End If
            End If
            Me._disposed = True
#If TraceManagerCreation Then
                If Not disposing Then
                    Throw New OrmManagerException("Manager finalize stack: " & _callstack)
                End If
#End If
        End SyncLock
    End Sub

#Region " IDisposable Support "
    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Try
            If _mcSwitch.TraceVerbose Then
                WriteLine("MediaContent disposed")
            End If
            Dispose(True)
        Finally
            GC.SuppressFinalize(Me)
        End Try
    End Sub

    Protected Overrides Sub Finalize()
        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Dispose(False)
        MyBase.Finalize()
    End Sub
#End Region

#Region " Lookup object "

    Public Function LoadObjects(Of T As {OrmBase, New})(ByVal fieldName As String, ByVal criteria As CriteriaLink, _
        ByVal col As ICollection) As ICollection(Of T)
        Return LoadObjects(Of T)(fieldName, criteria, col, 0, col.Count)
    End Function

    ''' <summary>
    ''' Load child collections from parents
    ''' </summary>
    ''' <typeparam name="T">Type to load. This is child type.</typeparam>
    ''' <param name="fieldName">Field name of property in child type what references to parent type</param>
    ''' <param name="criteria">Additional criteria</param>
    ''' <param name="ecol">Collection of parent objects.</param>
    ''' <param name="start">Point in parent collection from where start to load</param>
    ''' <param name="length">Length of loaded window</param>
    ''' <returns>Collection of child objects arranged in order of parent in parent collection</returns>
    ''' <remarks></remarks>
    Public Function LoadObjects(Of T As {OrmBase, New})(ByVal fieldName As String, ByVal criteria As CriteriaLink, _
        ByVal ecol As IEnumerable, ByVal start As Integer, ByVal length As Integer) As ICollection(Of T)
        Dim tt As Type = GetType(T)

        If ecol Is Nothing Then
            Throw New ArgumentNullException("col")
        End If

        If String.IsNullOrEmpty(fieldName) Then
            Throw New ArgumentNullException(fieldName)
        End If

        Dim cc As ICollection = TryCast(ecol, ICollection)
        If cc IsNot Nothing Then
            If cc.Count = 0 OrElse length = 0 OrElse start + length > cc.Count Then
                Return New List(Of T)
            End If
            'Else
            '    col = New ArrayList(ecol)
        End If

#If DEBUG Then
        Dim ft As Type = _schema.GetFieldTypeByName(tt, fieldName)
        For Each o As OrmBase In ecol
            If o.GetType IsNot ft Then
                Throw New ArgumentNullException(String.Format("Cannot load {0} with such collection. There is not relation", tt.Name))
            End If
            Exit For
        Next
#End If
        Dim lookups As New Dictionary(Of OrmBase, List(Of T))
        Dim newc As New List(Of OrmBase)
        Dim hasInCache As New Dictionary(Of OrmBase, Object)

        Using SyncHelper.AcquireDynamicLock("9h13bhpqergfbjadflbq34f89h134g")
            If Not _dont_cache_lists Then
                Dim i As Integer
                For Each o As OrmBase In ecol
                    If i < start Then
                        i += 1
                        Continue For
                    Else
                        i += 1
                    End If
                    'Dim con As New OrmCondition.OrmConditionConstructor
                    'con.AddFilter(New OrmFilter(tt, fieldName, o, FilterOperation.Equal))
                    'con.AddFilter(criteria.Filter)
                    Dim cl As CriteriaLink = ObjectSchema.CreateCriteria(tt, fieldName).Eq(o).And(criteria)
                    Dim f As IFilter = cl.Filter
                    Dim key As String = FindGetKey(Of T)(f) '_schema.GetEntityKey(tt) & f.GetStaticString & GetStaticKey()
                    Dim dic As IDictionary = GetDic(_cache, key)
                    Dim id As String = f.ToString
                    If dic.Contains(id) Then
                        'Dim fs As List(Of String) = Nothing
                        Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(f, Nothing, key, id)
                        Dim v As ICacheValidator = TryCast(del, ICacheValidator)
                        If v Is Nothing OrElse v.Validate() Then
                            'l.AddRange(Find(Of T)(cl, Nothing, True))
                            lookups.Add(o, New List(Of T)(Find(Of T)(cl, Nothing, True)))
                            hasInCache.Add(o, Nothing)
                        Else
                            newc.Add(o)
                        End If
                    Else
                        newc.Add(o)
                    End If
                    If i - start = length Then
                        Exit For
                    End If
                Next
            End If

            Dim ids As New Collections.IntList
            For Each o As OrmBase In newc
                ids.Append(o.Identifier)
            Next
            Dim c As New List(Of T)

            'If ids.Ints.Count > 0 Then
            GetObjects(Of T)(ids.Ints, GetFilter(criteria, tt), c, True, fieldName, False)

            Dim oschema As IOrmObjectSchemaBase = _schema.GetObjectSchema(tt)
            For Each o As T In c
                'Dim v As OrmBase = CType(_schema.GetFieldValue(o, fieldName), OrmBase)
                Dim v As OrmBase = CType(o.GetValue(fieldName, oschema), OrmBase)
                Dim ll As List(Of T) = Nothing
                If Not lookups.TryGetValue(v, ll) Then
                    ll = New List(Of T)
                    lookups.Add(v, ll)
                End If
                ll.Add(o)
            Next

            Dim l As New List(Of T)
            Dim j As Integer
            For Each obj As OrmBase In ecol
                If j < start Then
                    j += 1
                    Continue For
                Else
                    j += 1
                End If
                Dim v As List(Of T) = Nothing
                If lookups.TryGetValue(obj, v) Then
                    l.AddRange(v)
                Else
                    v = New List(Of T)
                End If
                If Not _dont_cache_lists AndAlso Not hasInCache.ContainsKey(obj) Then
                    'Dim con As New OrmCondition.OrmConditionConstructor
                    'con.AddFilter(New OrmFilter(tt, fieldName, k, FilterOperation.Equal))
                    'con.AddFilter(filter)
                    'Dim f As IOrmFilter = con.Condition
                    Dim cl As CriteriaLink = ObjectSchema.CreateCriteria(tt, fieldName).Eq(obj).And(criteria)
                    Dim f As IFilter = cl.Filter
                    Dim key As String = FindGetKey(Of T)(f) '_schema.GetEntityKey(tt) & f.GetStaticString & GetStaticKey()
                    Dim dic As IDictionary = GetDic(_cache, key)
                    Dim id As String = f.ToString
                    dic(id) = New CachedItem(f, v, Me)
                End If
                If j - start = length Then
                    Exit For
                End If
            Next
            'End If

            Return l
        End Using
    End Function

    Public Sub LoadObjects(Of T As {OrmBase, New})(ByVal relation As M2MRelation, ByVal criteria As CriteriaLink, _
        ByVal col As ICollection, ByVal target As ICollection(Of T))
        LoadObjects(Of T)(relation, criteria, col, target, 0, col.Count)
    End Sub

    Public Sub LoadObjects(Of T As {OrmBase, New})(ByVal relation As M2MRelation, ByVal criteria As CriteriaLink, _
        ByVal col As ICollection, ByVal target As ICollection(Of T), ByVal start As Integer, ByVal length As Integer)
        Dim type2load As Type = GetType(T)

        If col Is Nothing Then
            Throw New ArgumentNullException("col")
        End If

        If relation Is Nothing Then
            Throw New ArgumentNullException("relation")
        End If

        If col.Count = 0 OrElse length = 0 OrElse start + length > col.Count Then
            Return
        End If

        If type2load IsNot relation.Type Then
            Throw New ArgumentException("Relation is not suit for type " & type2load.Name)
        End If

        '#If DEBUG Then
        '            Dim ft As Type = _schema.GetM2MRelation(
        '            For Each o As OrmBase In col
        '                If o.GetType IsNot ft Then
        '                    Throw New ArgumentNullException(String.Format("Cannot load {0} with such collection. There is not relation", tt.Name))
        '                End If
        '                Exit For
        '            Next
        '#End If

        'Dim l As New List(Of T)
        Dim newc As New List(Of OrmBase)
        Dim direct As Boolean = Not relation.non_direct
        Using SyncHelper.AcquireDynamicLock("13498nfb134g8l;adnfvioh")
            If Not _dont_cache_lists Then
                Dim i As Integer = start
                For Each o As OrmBase In col
                    Dim tt1 As Type = o.GetType
                    Dim key As String = GetM2MKey(tt1, type2load, direct)
                    If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                        key &= criteria.Filter(type2load).tostaticstring
                    End If

                    Dim dic As IDictionary = GetDic(_cache, key)

                    Dim id As String = o.Identifier.ToString
                    If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                        id &= criteria.Filter(type2load).ToString
                    End If

                    If dic.Contains(id) Then
                        'Dim sync As String = GetSync(key, id)
                        Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(o, GetFilter(criteria, type2load), Nothing, id, key, direct)
                        Dim v As ICacheValidator = TryCast(del, ICacheValidator)
                        If v Is Nothing OrElse v.Validate() Then
                            Dim e As M2MCache = CType(dic(id), M2MCache)
                            If Not v.Validate(e) Then
                                newc.Add(o)
                            End If
                            'l.AddRange(FindMany2Many(Of T)(o, filter, Nothing, SortType.Asc, True))
                        Else
                            newc.Add(o)
                        End If
                    Else
                        newc.Add(o)
                    End If
                    i += 1
                    If i = length Then
                        Exit For
                    End If
                Next
            End If

            Dim ids As New Collections.IntList
            Dim type As Type = Nothing
            For Each o As OrmBase In newc
                ids.Append(o.Identifier)
                If type Is Nothing Then
                    type = o.GetType
                End If
            Next
            Dim edic As IDictionary(Of Integer, EditableList) = GetObjects(Of T)(type, ids.Ints, GetFilter(criteria, type2load), relation, False, True)
            'l.AddRange(c)

            If (target IsNot Nothing OrElse Not _dont_cache_lists) AndAlso edic IsNot Nothing Then
                For Each o As OrmBase In col
                    Dim el As EditableList = Nothing
                    If edic.TryGetValue(o.Identifier, el) Then
                        For Each id As Integer In el.Current
                            If target IsNot Nothing Then
                                target.Add(CreateDBObject(Of T)(id))
                            End If
                        Next

                        If Not _dont_cache_lists Then
                            Dim tt1 As Type = o.GetType
                            Dim key As String = GetM2MKey(tt1, type2load, direct)
                            If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                                key &= criteria.Filter(type2load).toStaticString
                            End If

                            Dim dic As IDictionary = GetDic(_cache, key)

                            Dim id As String = o.Identifier.ToString
                            If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                                id &= criteria.Filter(type2load).ToString
                            End If

                            'Dim sync As String = GetSync(key, id)
                            el.Accept(Nothing)
                            dic(id) = New M2MCache(Nothing, GetFilter(criteria, type2load), el, Me)
                        End If

                        'Cache.AddRelationValue(o.GetType, type2load)
                    End If
                Next
            End If
        End Using
    End Sub

    Protected Friend Function Find(ByVal id As Integer, ByVal t As Type) As OrmBase
        Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public
        Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("Find", flags, Nothing, Reflection.CallingConventions.Any, New Type() {GetType(Integer)}, Nothing)
        Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
        Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {id}, Nothing), OrmBase)
    End Function

    ''' <summary>
    ''' Поиск объекта по Id
    ''' </summary>
    Public Function Find(Of T As {OrmBase, New})(ByVal id As Integer) As T
        Invariant()

        Return LoadType(Of T)(id, _findload, True)
    End Function

    Public Function CreateObject(Of T As {OrmBase, New})(ByVal id As Integer) As T
        Invariant()

        Dim o As T = LoadType(Of T)(id, False, False)
        'o.ObjectState = ObjectState.Created
        Return o
    End Function

    Public Function CreateDBObject(Of T As {OrmBase, New})(ByVal id As Integer, _
        ByVal dic As IDictionary(Of Integer, T), ByVal addOnCreate As Boolean) As OrmBase
        Dim o As OrmBase = LoadTypeInternal(Of T)(id, False, False, dic, addOnCreate)
        Dim type As Type = GetType(T)
        'Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance
        'Dim mi_real As Reflection.MethodInfo = CType(_realLoadTypeDic(type), Reflection.MethodInfo)
        'If (mi_real Is Nothing) Then
        '    SyncLock _realLoadTypeDic.SyncRoot
        '        mi_real = CType(_realLoadTypeDic(type), Reflection.MethodInfo)
        '        If (mi_real Is Nothing) Then
        '            Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("LoadTypeInternal", flags, Nothing, Reflection.CallingConventions.Any, _
        '                New Type() {GetType(Integer), GetType(Boolean), GetType(Boolean), GetType(IDictionary)}, Nothing)
        '            mi_real = mi.MakeGenericMethod(New Type() {type})
        '            _realLoadTypeDic(type) = mi_real
        '        End If
        '    End SyncLock
        'End If
        'o = CType(mi_real.Invoke(Me, flags, Nothing, New Object() {id, False, False, dic}, Nothing), OrmBase)

        'Assert(o IsNot Nothing, "Object must be created: " & id & ". Type - " & type.ToString)
        'Using o.GetSyncRoot()
        If o.ObjectState = ObjectState.Created AndAlso Not IsNewObject(type, id) Then
            o.ObjectState = ObjectState.NotLoaded
            'AddObject(o)
        End If
        'End Using
        Return o
    End Function

    Public Function CreateDBObject(ByVal id As Integer, ByVal type As Type) As OrmBase
#If DEBUG Then
        If Not GetType(OrmBase).IsAssignableFrom(type) Then
            Throw New ArgumentException(String.Format("The type {0} must be derived from Ormbase", type))
        End If
#End If

        Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Public Or Reflection.BindingFlags.Instance
        Dim mi_real As Reflection.MethodInfo = CType(_realCreateDbObjectDic(type), Reflection.MethodInfo)
        If (mi_real Is Nothing) Then
            SyncLock _realCreateDbObjectDic.SyncRoot
                mi_real = CType(_realCreateDbObjectDic(type), Reflection.MethodInfo)
                If (mi_real Is Nothing) Then
                    Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("CreateDBObject", flags, Nothing, Reflection.CallingConventions.Any, New Type() {GetType(Integer)}, Nothing)
                    mi_real = mi.MakeGenericMethod(New Type() {type})
                    _realCreateDbObjectDic(type) = mi_real
                End If
            End SyncLock
        End If
        Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {id}, Nothing), OrmBase)
    End Function

    Public Function CreateDBObject(Of T As {OrmBase, New})(ByVal id As Integer) As T
        Dim o As T = CreateObject(Of T)(id)
        Assert(o IsNot Nothing, "Object must be created: " & id & ". Type - " & GetType(T).ToString)
        Using o.GetSyncRoot()
            If o.ObjectState = ObjectState.Created AndAlso Not IsNewObject(GetType(T), id) Then
                o.ObjectState = ObjectState.NotLoaded
                'AddObject(o)
            End If
        End Using
        Return o
    End Function

    '        Public Function Find(Of T As {OrmBase, New}, T1)(ByVal value As T1, ByVal fieldName As String, ByVal sort As String, ByVal sortType As SortType, ByVal withLoad As Boolean) As ICollection(Of T)
    '#If DEBUG Then
    '            Dim pi As Reflection.PropertyInfo = _schema.GetProperty(GetType(T), fieldName)
    '            If pi Is Nothing Then
    '                Throw New ArgumentException("Invalid field", fieldName)
    '            End If

    '            If GetType(OrmBase).IsAssignableFrom(GetType(T1)) Then
    '                Throw New ArgumentException("Use FindOrm", fieldName)
    '            End If
    '#End If

    '            Dim f As New OrmFilter(GetType(T), fieldName, New TypeWrap(Of Object)(value), FilterOperation.Equal)

    '            Return Find(Of T)(f, sort, withLoad)
    '        End Function

    '        Public Function FindOrm(Of T As {OrmBase, New})(ByVal value As OrmBase, ByVal fieldName As String, ByVal sort As Sort, ByVal withLoad As Boolean) As ICollection(Of T)
    '#If DEBUG Then
    '            Dim pi As Reflection.PropertyInfo = _schema.GetProperty(GetType(T), fieldName)
    '            If pi Is Nothing Then
    '                Throw New ArgumentException("Invalid field", fieldName)
    '            End If
    '#End If

    '            If value Is Nothing Then
    '                Throw New ArgumentNullException("value")
    '            End If

    '            'Dim f As New OrmFilter(GetType(T), fieldName, value, FilterOperation.Equal)

    '            Return Find(Of T)(New Criteria(GetType(T)).Field(fieldName).Eq(value), sort, withLoad)
    '        End Function

    'Protected Function Find(ByVal t As Type, ByVal filter As IOrmFilter, _
    '    ByVal sort As String, ByVal sortType As SortType, ByVal withLoad As Boolean) As IList

    '    Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Public Or Reflection.BindingFlags.Instance
    '    Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("Find", flags, Nothing, Reflection.CallingConventions.Any, _
    '        New Type() {GetType(IOrmFilter), GetType(String), GetType(SortType), GetType(Boolean)}, Nothing)
    '    Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
    '    Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {filter, sort, sortType, withLoad}, Nothing), IList)

    'End Function

    Protected Function FindWithJoinsGetKey(Of T As {OrmBase, New})(ByVal aspect As QueryAspect, _
        ByVal joins As OrmJoin(), ByVal criteria As IGetFilter) As String
        Dim key As String = String.Empty

        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            key &= criteria.Filter(GetType(T)).ToStaticString
        End If

        If joins IsNot Nothing Then
            For Each join As OrmJoin In joins
                If Not OrmJoin.IsEmpty(join) Then
                    key &= join.ToString
                End If
            Next
        End If

        If aspect IsNot Nothing Then
            key &= aspect.GetStaticKey
        End If

        key &= _schema.GetEntityKey(GetFilterInfo, GetType(T))

        Return key & GetStaticKey()
    End Function

    Protected Function FindWithJoinGetId(Of T As {OrmBase, New})(ByVal aspect As QueryAspect, ByVal joins As OrmJoin(), ByVal criteria As IGetFilter) As String
        Dim id As String = String.Empty

        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            id &= criteria.Filter(GetType(T)).ToString
        End If

        If joins IsNot Nothing Then
            For Each join As OrmJoin In joins
                If Not OrmJoin.IsEmpty(join) Then
                    id &= join.ToString
                End If
            Next
        End If

        If aspect IsNot Nothing Then
            id &= aspect.GetDynamicKey
        End If

        Return id & GetType(T).ToString
    End Function

    Private Function GetResultset(Of T As {OrmBase, New})(ByVal withLoad As Boolean, ByVal dic As IDictionary, _
        ByVal id As String, ByVal sync As String, ByVal del As ICustDelegate(Of T), ByRef succeeded As Boolean) As ICollection(Of T)
        Dim v As ICacheValidator = TryCast(del, ICacheValidator)
        Dim ce As CachedItem = GetFromCache(Of T)(dic, sync, id, withLoad, del)
        Dim s As IListObjectConverter.ExtractListResult
        Dim r As ICollection(Of T) = ce.GetObjectList(Of T)(Me, withLoad, del.Created, s)
        succeeded = True

        If s = IListObjectConverter.ExtractListResult.NeedLoad Then
            withLoad = True
l1:
            del.Renew = True
            ce = GetFromCache(Of T)(dic, sync, id, withLoad, del)
            r = ce.GetObjectList(Of T)(Me, withLoad, del.Created, s)
            Assert(s = IListObjectConverter.ExtractListResult.Successed, "Withload should always successed")
        End If

        If s = IListObjectConverter.ExtractListResult.CantApplyFilter Then
            succeeded = False
            Return r
        End If

        If _externalFilter IsNot Nothing Then
            If Not del.Created Then
                Dim psort As Sort = del.Sort

                If ce.SortEquals(psort) OrElse psort Is Nothing Then
                    If v IsNot Nothing AndAlso Not v.Validate(ce) Then
                        del.Renew = True
                        GoTo l1
                    End If
                    If psort IsNot Nothing AndAlso psort.IsExternal AndAlso ce.SortExpires Then
                        Dim objs As ICollection(Of T) = r
                        ce = del.GetCacheItem(_schema.ExternalSort(Of T)(psort, objs))
                        dic(id) = ce
                    End If
                Else
                    If Not del.SmartSort OrElse psort.Previous IsNot Nothing Then
                        del.Renew = True
                        GoTo l1
                    Else
                        'Dim loaded As Integer = 0
                        Dim objs As ICollection(Of T) = r
                        If objs IsNot Nothing AndAlso objs.Count > 0 Then
                            Dim srt As IOrmSorting = Nothing
                            If psort.IsExternal Then
                                ce = del.GetCacheItem(_schema.ExternalSort(Of T)(psort, objs))
                                dic(id) = ce
                            ElseIf CanSortOnClient(GetType(T), CType(objs, System.Collections.ICollection), psort, srt) Then
                                Using SyncHelper.AcquireDynamicLock(sync)
                                    Dim sc As IComparer(Of T) = Nothing
                                    If srt IsNot Nothing Then
                                        sc = srt.CreateSortComparer(Of T)(psort)
                                    Else
                                        sc = New OrmComparer(Of T)(psort)
                                    End If
                                    If sc IsNot Nothing Then
                                        Dim os As New Generic.List(Of T)(objs)
                                        os.Sort(sc)
                                        ce = del.GetCacheItem(os)
                                        dic(id) = ce
                                    Else
                                        del.Renew = True
                                        GoTo l1
                                    End If
                                End Using
                            Else
                                del.Renew = True
                                GoTo l1
                            End If
                        Else
                            'dic.Remove(id)
                            del.Renew = True
                            GoTo l1
                        End If
                    End If
                End If
            End If
        End If
        Return r
    End Function

    Public Function FindWithJoins(Of T As {OrmBase, New})(ByVal aspect As QueryAspect, _
        ByVal joins() As OrmJoin, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As ICollection(Of T)

        Dim key As String = FindWithJoinsGetKey(Of T)(aspect, joins, criteria)

        Dim dic As IDictionary = GetDic(_cache, key)

        Dim id As String = FindWithJoinGetId(Of T)(aspect, joins, criteria)

        Dim sync As String = id & GetStaticKey()

        'CreateDepends(filter, key, id)

        Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(aspect, joins, GetFilter(criteria, GetType(T)), sort, key, id)
        Dim s As Boolean = True
        Dim r As ICollection(Of T) = GetResultset(Of T)(withLoad, dic, id, sync, del, s)
        If Not s Then
            Assert(_externalFilter IsNot Nothing, "GetResultset should fail only when external filter specified")
            Using ac As New ApplyCriteria(Me, Nothing)
                Dim c As Conditions.Condition.ConditionConstructorBase = ObjectSchema.CreateConditionCtor
                c.AddFilter(del.Filter).AddFilter(ac.oldfilter)
                r = FindWithJoins(Of T)(aspect, joins, ObjectSchema.CreateCriteriaLink(c), sort, withLoad)
            End Using
        End If
        Return r
    End Function

    'Public Function FindDistinct(Of T As {OrmBase, New})(ByVal joins() As OrmJoin, ByVal criteria As CriteriaLink, _
    '    ByVal sort As Sort, ByVal withLoad As Boolean) As ICollection(Of T)

    '    Dim key As String = "distinct" & _schema.GetEntityKey(GetType(T))

    '    If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
    '        key &= criteria.Filter.GetStaticString
    '    End If

    '    If joins IsNot Nothing Then
    '        For Each join As OrmJoin In joins
    '            If Not join.IsEmpty Then
    '                key &= join.GetStaticString
    '            End If
    '        Next
    '    End If

    '    key &= GetStaticKey()

    '    Dim dic As IDictionary = GetDic(_cache, key)

    '    Dim id As String = GetType(T).ToString
    '    If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
    '        id &= CObj(criteria.Filter).ToString
    '    End If

    '    If joins IsNot Nothing Then
    '        For Each join As OrmJoin In joins
    '            If Not join.IsEmpty Then
    '                id &= join.ToString
    '            End If
    '        Next
    '    End If
    '    Dim sync As String = id & GetStaticKey()

    '    'CreateDepends(filter, key, id)

    '    Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(joins, GetFilter(criteria), sort, key, id)
    '    Dim ce As CachedItem = GetFromCache(Of T)(dic, sync, id, withLoad, del)
    '    Return ce.GetObjectList(Of T)(Me, withLoad, del.Created)
    'End Function

    Public Function FindDistinct(Of T As {OrmBase, New})(ByVal relation As M2MRelation, _
        ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As ICollection(Of T)

        Dim key As String = "distinct" & _schema.GetEntityKey(GetFilterInfo, GetType(T))

        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            key &= criteria.Filter(GetType(T)).ToStaticString
        End If

        If relation IsNot Nothing Then
            key &= relation.Table.RawName & relation.Column
        End If

        key &= GetStaticKey()

        Dim dic As IDictionary = GetDic(_cache, key)

        Dim id As String = GetType(T).ToString
        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            id &= criteria.Filter(GetType(T)).ToString
        End If

        If relation IsNot Nothing Then
            id &= relation.Table.RawName & relation.Column
        End If

        Dim sync As String = id & GetStaticKey()

        'CreateDepends(filter, key, id)

        Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(relation, GetFilter(criteria, GetType(T)), sort, key, id)
        'Dim ce As CachedItem = GetFromCache(Of T)(dic, sync, id, withLoad, del)
        Dim s As Boolean = True
        Dim r As ICollection(Of T) = GetResultset(Of T)(withLoad, dic, id, sync, del, s)
        If Not s Then
            Assert(_externalFilter IsNot Nothing, "GetResultset should fail only when external filter specified")
            Using ac As New ApplyCriteria(Me, Nothing)
                Dim c As Conditions.Condition.ConditionConstructorBase = ObjectSchema.CreateConditionCtor
                c.AddFilter(del.Filter).AddFilter(ac.oldfilter)
                r = FindDistinct(Of T)(relation, ObjectSchema.CreateCriteriaLink(c), sort, withLoad)
            End Using
        End If
        Return r
    End Function

    Public Function FindJoin(Of T As {OrmBase, New})(ByVal type2join As Type, ByVal joinField As String, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As ICollection(Of T)

        Return FindJoin(Of T)(type2join, joinField, FilterOperation.Equal, JoinType.Join, criteria, sort, withLoad)
    End Function

    Public Function FindJoin(Of T As {OrmBase, New})(ByVal type2join As Type, ByVal joinField As String, _
        ByVal joinOperation As FilterOperation, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As ICollection(Of T)

        Return FindJoin(Of T)(type2join, joinField, joinOperation, JoinType.Join, criteria, sort, withLoad)
    End Function

    Public Function FindJoin(Of T As {OrmBase, New})(ByVal type2join As Type, ByVal joinField As String, _
        ByVal joinOperation As FilterOperation, ByVal joinType As JoinType, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As ICollection(Of T)

        Return FindWithJoins(Of T)(Nothing, New OrmJoin() {MakeJoin(type2join, GetType(T), joinField, joinOperation, joinType)}, _
            criteria, sort, withLoad)
    End Function

    Public Function FindJoin(Of T As {OrmBase, New})(ByVal top As Integer, ByVal type2join As Type, ByVal joinField As String, _
        ByVal joinOperation As FilterOperation, ByVal joinType As JoinType, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As ICollection(Of T)

        Return FindWithJoins(Of T)(ObjectSchema.CreateTopAspect(top), New OrmJoin() {MakeJoin(type2join, GetType(T), joinField, joinOperation, joinType)}, _
            criteria, sort, withLoad)
    End Function

    Protected Function FindGetKey(Of T As {OrmBase, New})(ByVal filter As IFilter) As String
        Return filter.ToStaticString & GetStaticKey() & _schema.GetEntityKey(GetFilterInfo, GetType(T))
    End Function

    Public Function Find(Of T As {OrmBase, New})(ByVal criteria As IGetFilter) As ICollection(Of T)
        Return Find(Of T)(criteria, Nothing, False)
    End Function

    Public Function Find(Of T As {OrmBase, New})(ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As ICollection(Of T)

        If criteria Is Nothing Then
            Throw New ArgumentNullException("filter")
        End If

        Dim filter As IFilter = criteria.Filter(GetType(T))

        Dim joins() As OrmJoin = Nothing
        If HasJoins(GetType(T), filter, sort, joins) Then
            Dim c As Conditions.Condition.ConditionConstructorBase = ObjectSchema.CreateConditionCtor
            c.AddFilter(filter)
            Return FindWithJoins(Of T)(Nothing, joins, ObjectSchema.CreateCriteriaLink(c), sort, withLoad)
        Else
            Dim key As String = FindGetKey(Of T)(filter)

            Dim dic As IDictionary = GetDic(_cache, key)

            Dim id As String = filter.ToString
            Dim sync As String = id & GetStaticKey()

            'CreateDepends(filter, key, id)

            Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(filter, sort, key, id)
            'Dim ce As CachedItem = GetFromCache(Of T)(dic, sync, id, withLoad, del)
            Dim s As Boolean = True
            Dim r As ICollection(Of T) = GetResultset(Of T)(withLoad, dic, id, sync, del, s)
            If Not s Then
                Assert(_externalFilter IsNot Nothing, "GetResultset should fail only when external filter specified")
                Using ac As New ApplyCriteria(Me, Nothing)
                    Dim c As Conditions.Condition.ConditionConstructorBase = ObjectSchema.CreateConditionCtor
                    c.AddFilter(del.Filter).AddFilter(ac.oldfilter)
                    r = Find(Of T)(ObjectSchema.CreateCriteriaLink(c), sort, withLoad)
                End Using
            End If
            Return r
        End If
    End Function

    Public Function Find(Of T As {OrmBase, New})(ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal cols() As String) As ICollection(Of T)

        If criteria Is Nothing Then
            Throw New ArgumentNullException("criteria")
        End If

        Dim filter As IFilter = criteria.Filter(GetType(T))

        Dim key As String = FindGetKey(Of T)(filter) '_schema.GetEntityKey(GetType(T)) & criteria.Filter.GetStaticString & GetStaticKey()

        Dim dic As IDictionary = GetDic(_cache, key)

        Dim id As String = filter.ToString
        Dim sync As String = id & GetStaticKey()

        'CreateDepends(filter, key, id)

        Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(filter, sort, key, id, cols)
        Dim s As Boolean = True
        Dim r As ICollection(Of T) = GetResultset(Of T)(True, dic, id, sync, del, s)
        If Not s Then
            Assert(_externalFilter IsNot Nothing, "GetResultset should fail only when external filter specified")
            Using ac As New ApplyCriteria(Me, Nothing)
                Dim c As Conditions.Condition.ConditionConstructorBase = ObjectSchema.CreateConditionCtor
                c.AddFilter(del.Filter).AddFilter(ac.oldfilter)
                r = Find(Of T)(ObjectSchema.CreateCriteriaLink(c), sort, cols)
            End Using
        End If
        Return r
    End Function

    Public Function FindTop(Of T As {OrmBase, New})(ByVal top As Integer, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As ICollection(Of T)

        '    Dim key As String = String.Empty

        '    If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
        '        key = _schema.GetEntityKey(GetType(T)) & criteria.Filter.GetStaticString & GetStaticKey() & "Top"
        '    Else
        '        key = _schema.GetEntityKey(GetType(T)) & GetStaticKey() & "Top"
        '    End If

        '    If sort IsNot Nothing Then
        '        key &= sort.ToString
        '    End If

        '    Dim dic As IDictionary = GetDic(_cache, key)

        '    Dim f As String = String.Empty
        '    If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
        '        f = CObj(criteria.Filter).ToString
        '    End If

        '    Dim id As String = f & " - top - " & top
        '    Dim sync As String = id & GetStaticKey()

        '    'CreateDepends(filter, key, id)

        '    Dim del As ICustDelegate(Of T) = GetCustDelegate4Top(Of T)(top, GetFilter(criteria), sort, key, id)
        '    Return GetFromCache(Of T)(dic, sync, id, withLoad, del).GetObjectList(Of T)(Me, withLoad, del.Created)
        Dim filter As IFilter = Nothing
        If criteria IsNot Nothing Then
            filter = criteria.Filter(GetType(T))
        End If
        Dim joins() As OrmJoin = Nothing
        HasJoins(GetType(T), filter, sort, joins)
        Return FindWithJoins(Of T)(ObjectSchema.CreateTopAspect(top, sort), joins, filter, sort, withLoad)
    End Function

    '<Obsolete("Use OrmBase Find method")> _
    'Public Function FindMany2Many(Of T2 As {OrmBase, New})(ByVal obj As OrmBase, ByVal filter As IOrmFilter, _
    '    ByVal sort As String, ByVal sortType As SortType, ByVal withLoad As Boolean) As ICollection(Of T2)

    '    If obj Is Nothing Then
    '        Throw New ArgumentNullException("obj")
    '    End If

    '    Dim tt1 As Type = obj.GetType
    '    Dim tt2 As Type = GetType(T2)

    '    Dim key As String = tt1.Name & Const_JoinStaticString & tt2.Name & GetStaticKey()
    '    If filter IsNot Nothing Then
    '        key &= filter.GetStaticString
    '    End If

    '    Dim dic As IDictionary = GetDic(_cache, key)

    '    Dim id As String = obj.Identifier.ToString
    '    If filter IsNot Nothing Then
    '        id &= CObj(filter).ToString
    '    End If

    '    Dim sync As String = id & Const_KeyStaticString & key

    '    'CreateM2MDepends(filter, key, id)

    '    Dim del As ICustDelegate(Of T2) = GetCustDelegate(Of T2)(obj, filter, sort, sortType, id, sync, key, Nothing)
    '    Return FindAdvanced(Of T2)(dic, sync, id, withLoad, del).GetObjectList(Of T2)(Me, withLoad, del.Created)
    'End Function

    '<Obsolete("Use OrmBase Find method")> _
    'Public Function FindMany2ManySelf(Of T As {OrmBase, New})(ByVal obj As T, ByVal filter As IOrmFilter, _
    '    ByVal sort As String, ByVal sortType As SortType, ByVal withLoad As Boolean) As ICollection(Of T)

    '    Dim tt1 As Type = GetType(T)

    '    Dim key As String = tt1.Name & Const_JoinStaticString & tt1.Name & GetStaticKey() & DbSchema.SubRelationTag
    '    If filter IsNot Nothing Then
    '        key &= filter.GetStaticString
    '    End If

    '    Dim dic As IDictionary = GetDic(_cache, key)

    '    Dim id As String = obj.Identifier.ToString
    '    If filter IsNot Nothing Then
    '        id &= CObj(filter).ToString
    '    End If

    '    Dim sync As String = id & Const_KeyStaticString & key

    '    'CreateM2MDepends(filter, key, id)

    '    Dim del As ICustDelegate(Of T) = GetCustDelegateTag(Of T)(obj, filter, sort, sortType, id, sync, key)
    '    Return FindAdvanced(Of T)(dic, sync, id, withLoad, del).GetObjectList(Of T)(Me, withLoad, del.Created)
    'End Function

#End Region

#Region " Cache "

    Protected Function GetFromCache(Of T As {OrmBase, New})(ByVal dic As IDictionary, ByVal sync As String, ByVal id As Object, _
        ByVal withLoad As Boolean, ByVal del As ICustDelegate(Of T)) As CachedItem

        Invariant()

        Dim v As ICacheValidator = TryCast(del, ICacheValidator)

        Dim renew As Boolean = v IsNot Nothing AndAlso Not v.Validate()

        'Dim sort As String = del.Sort
        'Dim sort_type As SortType = del.SortType
        'Dim f As IOrmFilter = del.Filter

        Dim ce As CachedItem = Nothing

        If renew OrElse del.Renew OrElse _dont_cache_lists Then
            ce = del.GetCacheItem(withLoad)
            If Not _dont_cache_lists Then
                dic(id) = ce
            End If
            del.Created = True
        Else
            ce = CType(dic(id), CachedItem)
            If ce Is Nothing Then
l1:
                Using SyncHelper.AcquireDynamicLock(sync)
                    ce = CType(dic(id), CachedItem)
                    Dim emp As Boolean = ce Is Nothing
                    If emp OrElse del.Renew OrElse _dont_cache_lists Then
                        ce = del.GetCacheItem(withLoad)
                        If Not _dont_cache_lists OrElse Not emp Then
                            dic(id) = ce
                        End If
                        del.Created = True
                    End If
                End Using
            End If
        End If

        If del.Renew Then
            _er = New ExecutionResult(ce.GetCount(Me), ce.ExecutionTime, ce.FetchTime, Not del.Created, _loadedInLastFetch)
            Return ce
        End If

        If del.Created Then
            del.CreateDepends()
        Else
            If ce.Expires Then
                ce.Expire()
                del.Renew = True
                GoTo l1
            End If

            If _externalFilter Is Nothing Then
                Dim psort As Sort = del.Sort

                If ce.SortEquals(psort) OrElse psort Is Nothing Then
                    If v IsNot Nothing AndAlso Not v.Validate(ce) Then
                        del.Renew = True
                        GoTo l1
                    End If
                    If psort IsNot Nothing AndAlso psort.IsExternal AndAlso ce.SortExpires Then
                        Dim objs As ICollection(Of T) = ce.GetObjectList(Of T)(Me)
                        ce = del.GetCacheItem(_schema.ExternalSort(Of T)(psort, objs))
                        dic(id) = ce
                    End If
                Else
                    'Dim loaded As Integer = 0
                    Dim objs As ICollection(Of T) = ce.GetObjectList(Of T)(Me)
                    If objs IsNot Nothing AndAlso objs.Count > 0 Then
                        Dim srt As IOrmSorting = Nothing
                        If psort.IsExternal Then
                            ce = del.GetCacheItem(_schema.ExternalSort(Of T)(psort, objs))
                            dic(id) = ce
                        ElseIf CanSortOnClient(GetType(T), CType(objs, System.Collections.ICollection), psort, srt) Then
                            Using SyncHelper.AcquireDynamicLock(sync)
                                Dim sc As IComparer(Of T) = Nothing
                                If srt IsNot Nothing Then
                                    sc = srt.CreateSortComparer(Of T)(psort)
                                Else
                                    sc = New OrmComparer(Of T)(psort)
                                End If
                                If sc IsNot Nothing Then
                                    Dim os As New Generic.List(Of T)(objs)
                                    os.Sort(sc)
                                    ce = del.GetCacheItem(os)
                                    dic(id) = ce
                                Else
                                    del.Renew = True
                                    GoTo l1
                                End If
                            End Using
                        Else
                            del.Renew = True
                            GoTo l1
                        End If
                    Else
                        'dic.Remove(id)
                        del.Renew = True
                        GoTo l1
                    End If
                End If
            End If
        End If

        Dim l As Nullable(Of Integer) = Nothing
        If del.Created Then
            l = _loadedInLastFetch
        End If
        _er = New ExecutionResult(ce.GetCount(Me), ce.ExecutionTime, ce.FetchTime, Not del.Created, l)
        Return ce
    End Function

    Public Function CanSortOnClient(ByVal t As Type, ByVal col As ICollection, ByVal sort As Sort, ByRef sorting As IOrmSorting) As Boolean
        'If sort.Previous IsNot Nothing Then
        '    Return False
        'End If

        If sort.IsCustom Then
            Return False
        End If

        Dim schema As IOrmObjectSchemaBase = _schema.GetObjectSchema(t)
        sorting = TryCast(schema, IOrmSorting)
        'If sorting Is Nothing Then
        '    Return False
        'End If
        Dim loaded As Integer = 0
        For Each o As OrmBase In col
            If o.IsLoaded Then loaded += 1
            If col.Count - loaded > 10 Then
                Return False
            End If
        Next
        Return True
    End Function

    Protected Sub InvalidateCache(ByVal obj As OrmBase, ByVal upd As ICollection) '(Of EntityFilter)
        Dim t As Type = obj.GetType
        Dim l As New List(Of String)
        For Each f As EntityFilter In upd
            '    Assert(f.Type Is t, "")

            '    Cache.AddUpdatedFields(obj, f.FieldName)
            l.Add(f.Template.FieldName)
        Next
        Cache.AddUpdatedFields(obj, l)
    End Sub

    'Protected Function GetDataTable(ByVal id As String, ByVal key As String, ByVal sync As String, ByVal t As Type, _
    '    ByVal obj As OrmBase, ByVal filter As IOrmFilter, ByVal rev As Boolean, _
    '    ByVal appendJoins As Boolean, ByVal renew As Boolean) As System.Data.DataTable

    '    Dim dic As IDictionary = GetDic(_cache, key)

    '    Dim dt As System.Data.DataTable = CType(dic(id), System.Data.DataTable)

    '    If dt Is Nothing OrElse _dont_cache_lists OrElse renew Then
    '        Using SyncHelper.AcquireDynamicLock(sync)
    '            dt = CType(dic(id), System.Data.DataTable)
    '            If dt Is Nothing OrElse _dont_cache_lists OrElse renew Then
    '                If rev Then
    '                    dt = GetDataTableInternal(t, obj, filter, appendJoins, DbSchema.SubRelationTag)
    '                Else
    '                    dt = GetDataTableInternal(t, obj, filter, appendJoins)
    '                End If
    '                If Not _dont_cache_lists Then
    '                    dic(id) = dt
    '                End If
    '                Cache.AddRelationValue(obj.GetType, t)
    '            End If
    '        End Using
    '    End If

    '    Return dt
    'End Function

    'Protected Sub RemoveFromCache(ByVal dic As IDictionary, ByVal id As Object)
    '    Invariant()

    '    dic.Remove(id)
    'End Sub

    'Protected Function HasInCache(ByVal dic As IDictionary, ByVal id As Object) As Boolean
    '    Invariant()

    '    Dim ce As CachedItem = CType(dic(id), CachedItem)

    '    Return ce IsNot Nothing
    'End Function

    'Protected Function GetCacheItem(ByVal dic As IDictionary, ByVal id As Object) As CachedItem
    '    Invariant()

    '    Return CType(dic(id), CachedItem)
    'End Function

    'Protected Function GetCacheItem2(ByVal filter_key As String, ByVal id As Object) As CachedItem
    '    Invariant()

    '    Dim dic As IDictionary = GetDic(_cache, filter_key)

    '    Return GetCacheItem(dic, id)
    'End Function

    'Protected Function GetCacheItem1(ByVal filter_key As String) As CachedItem
    '    Invariant()

    '    Return GetCacheItem(_cache.Filters, filter_key)
    'End Function

    'Protected Function HasInCache2(ByVal filter_key As String, ByVal id As Object) As Boolean
    '    Invariant()

    '    Dim dic As IDictionary = GetDic(_cache, filter_key)

    '    Return HasInCache(dic, id)
    'End Function

    'Protected Function HasInCache1(ByVal filter_key As String) As Boolean
    '    Invariant()

    '    Return HasInCache(_cache.Filters, filter_key)
    'End Function

    'Protected Sub RemoveFromCache1(ByVal filter_key As String)
    '    Invariant()

    '    RemoveFromCache(_cache.Filters, filter_key)
    'End Sub

    Public Function IsInCache(ByVal obj As OrmBase) As Boolean
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        Return IsInCache(obj.Identifier, obj.GetType)
    End Function

    Public Function IsInCache(ByVal id As Integer, ByVal t As Type) As Boolean
        Dim dic As IDictionary = GetDictionary(t)

        If dic Is Nothing Then
            ''todo: throw an exception when all collections will be implemented
            'Return
            Throw New OrmManagerException("Collection for " & t.Name & " not exists")
        End If

        Return dic.Contains(id)
    End Function

#End Region

#Region " Object support "

    Protected Friend Function LoadType(ByVal id As Integer, ByVal t As Type, ByVal load As Boolean, ByVal checkOnCreate As Boolean) As OrmBase
        Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic
        Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("LoadType", flags, Nothing, Reflection.CallingConventions.Any, New Type() {GetType(Integer), GetType(Boolean), GetType(Boolean)}, Nothing)
        Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
        Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {id, load, checkOnCreate}, Nothing), OrmBase)
    End Function

    Protected Function LoadTypeInternal(Of T As {OrmBase, New})(ByVal id As Integer, _
        ByVal load As Boolean, ByVal checkOnCreate As Boolean, ByVal dic As IDictionary(Of Integer, T), ByVal addOnCreate As Boolean) As T

        Dim type As Type = GetType(T)

#If DEBUG Then
        If dic Is Nothing Then
            Dim name As String = GetType(T).Name
            Throw New OrmManagerException("Collection for " & name & " not exists")
        End If
#End If
        Dim created As Boolean = False ', checked As Boolean = False
        Dim a As T = Nothing
        If Not dic.TryGetValue(id, a) AndAlso _newMgr IsNot Nothing Then
            a = CType(_newMgr.GetNew(type, id), T)
            If a IsNot Nothing Then Return a
        End If
        Dim sync_key As String = "LoadType" & id & type.ToString
        If a Is Nothing Then
            Using SyncHelper.AcquireDynamicLock(sync_key)
                If Not dic.TryGetValue(id, a) Then
                    If OrmSchemaBase.GetUnions(type) IsNot Nothing Then
                        Throw New NotSupportedException
                    Else
                        a = New T
                        a.Init(id, _cache, _schema)
                        a._mgrStr = IdentityString
                    End If

                    If load Then
                        a.Load()
                        If Not a.IsLoaded Then
                            a = Nothing
                        End If
                    End If
                    If a IsNot Nothing AndAlso checkOnCreate Then
                        'checked = True
                        If Not a.IsLoaded Then
                            a.Load()
                            If a.ObjectState = ObjectState.NotFoundInDB Then
                                a = Nothing
                            End If
                        End If
                    End If
                    created = True
                End If
            End Using
        End If

        If a IsNot Nothing Then
            If created AndAlso addOnCreate Then
                AddObjectInternal(a, CType(dic, System.Collections.IDictionary))
            End If
            If Not created AndAlso load AndAlso Not a.IsLoaded Then
                a.Load()
            End If
            a.Invariant()
        End If

        Return a
    End Function

    Protected Function GetObjectFromCache(Of T As {OrmBase, New})(ByVal obj As T, ByVal dic As IDictionary(Of Integer, T), _
        ByVal load As Boolean, ByVal checkOnCreate As Boolean, ByVal addOnCreate As Boolean) As T

        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        '    Return GetObjectFromCache(obj.Identifier, dic)
        'End Function

        'Protected Function GetObjectFromCache(Of T As {OrmBase, New})(ByVal id As Integer, _
        '    ByVal dic As IDictionary(Of Integer, T)) As T

        Dim type As Type = GetType(T)

#If DEBUG Then
        If dic Is Nothing Then
            Dim name As String = GetType(T).Name
            Throw New OrmManagerException("Collection for " & name & " not exists")
        End If
#End If
        Dim created As Boolean = False ', checked As Boolean = False
        Dim a As T = Nothing
        Dim id As Integer = obj.Identifier
        If Not dic.TryGetValue(id, a) AndAlso _newMgr IsNot Nothing Then
            a = CType(_newMgr.GetNew(type, id), T)
            If a IsNot Nothing Then Return a
        End If
        Dim sync_key As String = "LoadType" & id & type.ToString
        If a Is Nothing Then
            Using SyncHelper.AcquireDynamicLock(sync_key)
                If Not dic.TryGetValue(id, a) Then
                    If OrmSchemaBase.GetUnions(type) IsNot Nothing Then
                        Throw New NotSupportedException
                    Else
                        a = obj
                        a.Init(_cache, _schema)
                    End If

                    If load Then
                        a.Load()
                        If Not a.IsLoaded Then
                            a = Nothing
                        End If
                    End If
                    If a IsNot Nothing AndAlso checkOnCreate Then
                        'checked = True
                        If Not a.IsLoaded Then
                            a.Load()
                            If a.ObjectState = ObjectState.NotFoundInDB Then
                                a = Nothing
                            End If
                        End If
                    End If
                    created = True
                End If
            End Using
        End If

        If a IsNot Nothing Then
            If created AndAlso addOnCreate Then
                AddObjectInternal(a, CType(dic, System.Collections.IDictionary))
            End If
            If Not created AndAlso load AndAlso Not a.IsLoaded Then
                a.Load()
            End If
        End If

        Return a
    End Function

    Protected Friend Function LoadType(Of T As {OrmBase, New})(ByVal id As Integer, _
        ByVal load As Boolean, ByVal checkOnCreate As Boolean) As T

        Dim dic As Generic.IDictionary(Of Integer, T) = GetDictionary(Of T)()

#If DEBUG Then
        If dic Is Nothing Then
            Dim name As String = GetType(T).Name
            Throw New OrmManagerException("Collection for " & name & " not exists")
        End If
#End If

        Return LoadTypeInternal(Of T)(id, load, checkOnCreate, dic, True)
    End Function

    Protected Sub AddObject(ByVal obj As OrmBase)
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        If obj.Identifier < 0 Then
            Throw New ArgumentNullException(String.Format("Cannot add object {0} to cache ", obj.ObjName))
        End If

        'If obj.ObjectState = ObjectState.Created Then
        '    Throw New ArgumentNullException(String.Format("Cannot add object {0} to cache ", obj.ObjName))
        'End If

        'Debug.Assert(obj.IsLoaded)
        Dim dic As IDictionary = GetDictionary(obj.GetType)

        If dic Is Nothing Then
            ''todo: throw an exception when all collections will be implemented
            'Return
            Dim name As String = obj.GetType.Name
            Throw New OrmManagerException("Collection for " & name & " not exists")
        End If

        AddObjectInternal(obj, dic)
    End Sub

    Protected Sub AddObjectInternal(ByVal obj As OrmBase, ByVal dic As IDictionary)
        Dim trace As Boolean = False
        Dim id As Integer = obj.Identifier
        SyncLock dic.SyncRoot
            If Not dic.Contains(id) Then
                dic.Add(id, obj)
            Else
                trace = True
            End If
        End SyncLock

        If trace AndAlso _mcSwitch.TraceVerbose Then
            Dim name As String = obj.GetType.Name
            WriteLine("Attempt to add existing object " & name & " (" & obj.Identifier & ") to cashe")
        End If
    End Sub

    Public Sub RemoveObjectFromCache(ByVal obj As OrmBase)

        If obj Is Nothing Then
            Throw New ArgumentNullException("obj parameter cannot be nothing")
        End If

        'Debug.Assert(Not obj.IsLoaded)
        Dim t As System.Type = obj.GetType

        Dim name As String = t.Name
        Dim dic As IDictionary = GetDictionary(t)
        If dic Is Nothing Then
            ''todo: throw an exception when all collections will be implemented
            'Return
            Throw New OrmManagerException("Collection for " & name & " not exists")
        End If

        dic.Remove(obj.Identifier)

        _cache.RemoveDepends(obj)

        'Dim l As List(Of Type) = Nothing
        For Each o As Pair(Of OrmManagerBase.M2MCache, Pair(Of String, String)) In Cache.GetM2MEntries(obj, Nothing)
            Dim mdic As IDictionary = GetDic(Cache, o.Second.First)
            mdic.Remove(o.Second.Second)
        Next
    End Sub

#End Region

#Region " helpers "
    Friend Shared Function GetSync(ByVal key As String, ByVal id As String) As String
        Return id & Const_KeyStaticString & key
    End Function

    Public Function GetDictionary(ByVal t As Type) As IDictionary
        Return _cache.GetOrmDictionary(GetFilterInfo, t, _schema)
    End Function

    Public Function GetDictionary(Of T)() As Generic.IDictionary(Of Integer, T)
        Return _cache.GetOrmDictionary(Of T)(GetFilterInfo, _schema)
    End Function

    <Conditional("DEBUG")> _
    Protected Overridable Sub Invariant()
        Debug.Assert(Not _disposed)
        If _disposed Then
            Throw New ObjectDisposedException("MediaContent")
        End If

        Debug.Assert(_schema IsNot Nothing)
        If _schema Is Nothing Then
            Throw New ArgumentNullException("Schema cannot be nothing")
        End If

        Debug.Assert(_cache IsNot Nothing)
        If _cache Is Nothing Then
            Throw New ArgumentNullException("OrmCacheBase cannot be nothing")
        End If
    End Sub

#End Region

#Region " shared helpers "

    Private Shared Sub InsertObject(Of T As {OrmBase, New})(ByVal mgr As OrmManagerBase, ByVal check_loaded As Boolean, ByVal l As Generic.List(Of Integer), ByVal o As OrmBase)
        If o IsNot Nothing Then
            If (Not o.IsLoaded OrElse Not check_loaded) AndAlso o.ObjectState <> ObjectState.NotFoundInDB Then
                If Not (o.ObjectState = ObjectState.Created AndAlso mgr.IsNewObject(GetType(T), o.Identifier)) Then
                    Dim idx As Integer = l.BinarySearch(o.Identifier)
                    If idx < 0 Then
                        l.Insert(Not idx, o.Identifier)
                    End If
                End If
            End If
        End If
    End Sub

    Private Shared Sub InsertObject(Of T As {OrmBase, New})(ByVal mgr As OrmManagerBase, _
        ByVal check_loaded As Boolean, ByVal l As Generic.List(Of Integer), ByVal o As OrmBase, _
        ByVal columns As List(Of ColumnAttribute))

        Throw New NotImplementedException
    End Sub

    Protected Shared Function FormPKValues(Of T As {OrmBase, New})(ByVal mgr As OrmManagerBase, ByVal objs As ICollection(Of T), ByVal start As Integer, ByVal length As Integer, _
        Optional ByVal check_loaded As Boolean = True) As List(Of Integer)

        Dim l As New Generic.List(Of Integer)
        Dim col As IList(Of T) = TryCast(objs, IList(Of T))
        If col IsNot Nothing Then
            For i As Integer = start To start + length - 1
                Dim o As OrmBase = col(i)
                InsertObject(Of T)(mgr, check_loaded, l, o)
            Next
        Else
            Dim i As Integer = 0
            For Each o As OrmBase In objs
                If i >= start + length Then
                    Exit For
                End If
                If i >= start Then
                    InsertObject(Of T)(mgr, check_loaded, l, o)
                    'If o IsNot Nothing Then
                    '    If (Not o.IsLoaded OrElse Not check_loaded) AndAlso o.ObjectState <> ObjectState.NotFoundInDB Then
                    '        If Not (o.ObjectState = ObjectState.Created AndAlso mgr.IsNewObject(GetType(T), o.Identifier)) Then
                    '            Dim idx As Integer = l.BinarySearch(o.Identifier)
                    '            If idx < 0 Then
                    '                l.Insert(Not idx, o.Identifier)
                    '            End If
                    '        End If
                    '    End If
                    'End If
                End If
                i += 1
            Next
        End If
        Return l
    End Function

    Protected Shared Function FormPKValues(Of T As {OrmBase, New})(ByVal mgr As OrmManagerBase, _
        ByVal objs As ICollection(Of T), ByVal start As Integer, ByVal length As Integer, _
        ByVal check_loaded As Boolean, ByVal columns As Generic.List(Of ColumnAttribute)) As List(Of Integer)

        Dim l As New Generic.List(Of Integer)
        Dim col As IList(Of T) = TryCast(objs, IList(Of T))
        If col IsNot Nothing Then
            For i As Integer = start To start + length - 1
                Dim o As OrmBase = col(i)
                InsertObject(Of T)(mgr, check_loaded, l, o, columns)
            Next
        Else
            Dim i As Integer = 0
            For Each o As OrmBase In objs
                If i >= start + length Then
                    Exit For
                End If
                If i >= start Then
                    InsertObject(Of T)(mgr, check_loaded, l, o, columns)
                    'If o IsNot Nothing Then
                    '    If (Not o.IsLoaded OrElse Not check_loaded) AndAlso o.ObjectState <> ObjectState.NotFoundInDB Then
                    '        If Not (o.ObjectState = ObjectState.Created AndAlso mgr.IsNewObject(GetType(T), o.Identifier)) Then
                    '            Dim idx As Integer = l.BinarySearch(o.Identifier)
                    '            If idx < 0 Then
                    '                l.Insert(Not idx, o.Identifier)
                    '            End If
                    '        End If
                    '    End If
                    'End If
                End If
                i += 1
            Next
        End If
        Return l
    End Function

    Public Shared Sub WriteWarning(ByVal message As String)
        If _mcSwitch.TraceWarning Then
            WriteLine(message)
        End If
    End Sub

    Public Shared Sub WriteError(ByVal message As String)
        If _mcSwitch.TraceError Then
            WriteLine(message)
        End If
    End Sub

    Protected Shared Sub WriteLine(ByVal message As String)
        Trace.WriteLine(message)
        Trace.WriteLine(Now & Environment.StackTrace)
    End Sub

    Protected Shared Sub Assert(ByVal condition As Boolean, ByVal message As String)
        Debug.Assert(condition, message)
        Trace.Assert(condition, message)
        If Not condition Then Throw New OrmManagerException(message)
    End Sub

#End Region

#Region " Many2Many "
    Public Function GetM2MList(Of T As {OrmBase, New})(ByVal obj As OrmBase, ByVal direct As Boolean) As EditableList
        Return FindM2MReturnKeys(Of T)(obj, direct).First.Entry
    End Function

    Protected Friend Function GetM2MKey(ByVal tt1 As Type, ByVal tt2 As Type, ByVal direct As Boolean) As String
        Return tt1.Name & Const_JoinStaticString & direct & " - new version - " & tt2.Name & GetStaticKey()
    End Function

    Protected Friend Function FindMany2Many2(Of T As {OrmBase, New})(ByVal obj As OrmBase, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal direct As Boolean, ByVal withLoad As Boolean) As ICollection(Of T)
        '    Dim p As Pair(Of M2MCache, Boolean) = FindM2M(Of T)(obj, direct, criteria, sort, withLoad)
        '    'Return p.First.GetObjectList(Of T)(Me, withLoad, p.Second.Created)
        '    return GetResultset(of T)(withload,dic,
        Dim tt1 As Type = obj.GetType
        Dim tt2 As Type = GetType(T)

        Dim key As String = GetM2MKey(tt1, tt2, direct)
        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            key &= criteria.Filter(tt2).ToStaticString
        End If

        Dim dic As IDictionary = GetDic(_cache, key)

        Dim id As String = obj.Identifier.ToString
        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            id &= criteria.Filter(tt2).ToString
        End If

        Dim sync As String = GetSync(key, id)

        'CreateM2MDepends(filter, key, id)

        Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(obj, GetFilter(criteria, tt2), sort, id, key, direct)
        Dim s As Boolean = True
        Dim r As ICollection(Of T) = GetResultset(Of T)(withLoad, dic, id, sync, del, s)
        If Not s Then
            Assert(_externalFilter IsNot Nothing, "GetResultset should fail only when external filter specified")
            Using ac As New ApplyCriteria(Me, Nothing)
                Dim c As Conditions.Condition.ConditionConstructorBase = ObjectSchema.CreateConditionCtor
                c.AddFilter(del.Filter).AddFilter(ac.oldfilter)
                r = FindMany2Many2(Of T)(obj, ObjectSchema.CreateCriteriaLink(c), sort, direct, withLoad)
            End Using
        End If
        Return r
    End Function

    Protected Function FindM2M(Of T As {OrmBase, New})(ByVal obj As OrmBase, ByVal direct As Boolean, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As Pair(Of M2MCache, Boolean)
        Dim tt1 As Type = obj.GetType
        Dim tt2 As Type = GetType(T)

        Dim key As String = GetM2MKey(tt1, tt2, direct)
        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            key &= criteria.Filter(tt2).ToStaticString
        End If

        Dim dic As IDictionary = GetDic(_cache, key)

        Dim id As String = obj.Identifier.ToString
        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            id &= criteria.Filter(tt2).ToString
        End If

        Dim sync As String = GetSync(key, id)

        'CreateM2MDepends(filter, key, id)

        Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(obj, GetFilter(criteria, tt2), sort, id, key, direct)
        Dim m As M2MCache = CType(GetFromCache(Of T)(dic, sync, id, withLoad, del), M2MCache)
        Dim p As New Pair(Of M2MCache, Boolean)(m, del.Created)
        Return p
    End Function

    Protected Function FindM2MReturnKeysNonGeneric(ByVal mainobj As OrmBase, ByVal t As Type, ByVal direct As Boolean) As Pair(Of M2MCache, Pair(Of String))
        Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic
        'Dim pm As New Reflection.ParameterModifier(6)
        'pm(5) = True
        Dim types As Type() = New Type() {GetType(OrmBase), GetType(Boolean)}
        Dim o() As Object = New Object() {mainobj, direct}
        'Dim m As M2MCache = CType(GetType(OrmManagerBase).InvokeMember("FindM2M", Reflection.BindingFlags.InvokeMethod Or Reflection.BindingFlags.NonPublic, _
        '    Nothing, Me, o, New Reflection.ParameterModifier() {pm}, Nothing, Nothing), M2MCache)
        Dim mi As Reflection.MethodInfo = GetType(OrmManagerBase).GetMethod("FindM2MReturnKeys", flags, Nothing, Reflection.CallingConventions.Any, types, Nothing)
        Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
        Dim p As Pair(Of M2MCache, Pair(Of String)) = CType(mi_real.Invoke(Me, flags, Nothing, o, Nothing), Pair(Of M2MCache, Pair(Of String)))
        Return p
    End Function

    Protected Function FindM2MReturnKeys(Of T As {OrmBase, New})(ByVal obj As OrmBase, ByVal direct As Boolean) As Pair(Of M2MCache, Pair(Of String))
        Dim tt1 As Type = obj.GetType
        Dim tt2 As Type = GetType(T)

        Dim key As String = GetM2MKey(tt1, tt2, direct)

        Dim dic As IDictionary = GetDic(_cache, key)

        Dim id As String = obj.Identifier.ToString

        Dim sync As String = GetSync(key, id)

        'CreateM2MDepends(filter, key, id)

        Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(obj, Nothing, Nothing, id, key, direct)
        Dim m As M2MCache = CType(GetFromCache(Of T)(dic, sync, id, False, del), M2MCache)
        Dim p As New Pair(Of M2MCache, Pair(Of String))(m, New Pair(Of String)(key, id))
        Return p
    End Function

    Protected Friend Sub M2MCancel(ByVal mainobj As OrmBase, ByVal t As Type)
        For Each o As Pair(Of OrmManagerBase.M2MCache, Pair(Of String, String)) In Cache.GetM2MEntries(mainobj, Nothing)
            If o.First.Entry.SubType Is t Then
                o.First.Entry.Reject(True)
            End If
        Next
    End Sub

    Protected Friend Sub M2MDelete(ByVal mainobj As OrmBase, ByVal subobj As OrmBase, ByVal direct As Boolean)
        If mainobj Is Nothing Then
            Throw New ArgumentNullException("mainobj")
        End If

        If subobj Is Nothing Then
            Throw New ArgumentNullException("subobj")
        End If

        M2MDeleteInternal(mainobj, subobj, direct)

        If mainobj.GetType Is subobj.GetType Then
            M2MDeleteInternal(subobj, mainobj, Not direct)
        Else
            M2MDeleteInternal(subobj, mainobj, direct)
        End If
    End Sub

    Protected Friend Sub M2MDelete(ByVal mainobj As OrmBase, ByVal t As Type, ByVal direct As Boolean)
        Dim m As M2MCache = FindM2MNonGeneric(mainobj, t, direct).First
        For Each id As Integer In m.Entry.Current
            'm.Entry.Delete(id)
            M2MDelete(mainobj, CreateDBObject(id, t), direct)
        Next
    End Sub

    Protected Function M2MSave(ByVal mainobj As OrmBase, ByVal t As Type, ByVal direct As Boolean) As OrmBase.AcceptState2
        Invariant()

        If mainobj Is Nothing Then
            Throw New ArgumentNullException("mainobj parameter cannot be nothing")
        End If

        If t Is Nothing Then
            Throw New ArgumentNullException("t parameter cannot be nothing")
        End If

        'Dim tt1 As Type = mainobj.GetType
        'Dim tt2 As Type = t

        For Each o As Pair(Of OrmManagerBase.M2MCache, Pair(Of String, String)) In Cache.GetM2MEntries(mainobj, Nothing)
            Dim m2me As M2MCache = o.First
            If m2me Is Nothing Then
                Throw New OrmManagerException(String.Format("M2MCache entry is nothing for key:[{0}] and id:[{1}]. Quering type {2} for {3}; direct={4}", o.Second.First, o.Second.Second, t, mainobj.ObjName, direct))
            End If
            If m2me.Entry.SubType Is t AndAlso m2me.Filter Is Nothing AndAlso m2me.Entry.HasChanges AndAlso m2me.Entry.Direct = direct Then
                Using SyncHelper.AcquireDynamicLock(GetSync(o.Second.First, o.Second.Second))
                    Dim sv As EditableList = m2me.Entry.PrepareSave(Me)
                    If sv IsNot Nothing Then
                        M2MSave(mainobj, t, direct, sv)
                        m2me.Entry.Saved = True
                    End If
                    'Return New OrmBase.AcceptState2(m2me, o.Second.First, o.Second.Second)
                    Dim acs As OrmBase.AcceptState2 = mainobj.GetAccept(m2me)
                    If acs Is Nothing Then
                        Throw New InvalidOperationException("Accept state must exist")
                    End If
                    Return acs
                End Using
            End If
        Next
        Return Nothing

    End Function

    Protected Sub M2MUpdate(ByVal obj As OrmBase, ByVal oldId As Integer)
        For Each o As Pair(Of M2MCache, Pair(Of String, String)) In Cache.GetM2MEntries(obj, obj.GetOldName(oldId))
            Dim key As String = o.Second.First
            Dim id As String = o.Second.Second
            Dim m As M2MCache = o.First
            Dim dic As IDictionary = GetDic(Cache, key)
            dic.Remove(id)
            m.Entry.MainId = obj.Identifier

            id = obj.Identifier.ToString
            If m.Filter IsNot Nothing Then
                id &= CObj(m.Filter).ToString
            End If
            dic.Add(id, m)
        Next

        Cache.UpdateM2MEntries(obj, oldId, obj.GetOldName(oldId))
        Dim tt1 As Type = obj.GetType

        For Each r As M2MRelation In _schema.GetM2MRelations(obj.GetType)

            Dim key As String = GetM2MKey(tt1, r.Type, Not r.non_direct)
            Dim dic As IDictionary = GetDic(_cache, key)
            Dim id As String = obj.Identifier.ToString
            'Dim sync As String = GetSync(key, id)

            If dic.Contains(id) Then
                Dim m As M2MCache = CType(dic(id), M2MCache)

                For Each oid As Integer In m.Entry.Current
                    Dim o As OrmBase = CreateDBObject(oid, r.Type)
                    M2MSubUpdate(o, obj.Identifier, oldId, obj.GetType)
                Next
            End If
        Next
    End Sub

    Protected Sub M2MSubUpdate(ByVal obj As OrmBase, ByVal id As Integer, ByVal oldId As Integer, ByVal t As Type)
        For Each o As Pair(Of M2MCache, Pair(Of String, String)) In Cache.GetM2MEntries(obj, Nothing)
            Dim m As M2MCache = o.First
            If m.Entry.SubType Is t Then
                m.Entry.Update(id, oldId)
            End If
        Next
    End Sub

    'Protected Sub ObjRelationUpdate(ByVal obj As OrmBase, ByVal oldid As Integer, ByVal t As Type)
    '    Invariant()

    '    If obj Is Nothing Then
    '        Throw New ArgumentNullException("obj parameter cannot be nothing")
    '    End If

    '    If t Is Nothing Then
    '        Throw New ArgumentNullException("t parameter cannot be nothing")
    '    End If

    '    Dim tt1 As Type = obj.GetType
    '    Dim tt2 As Type = t

    '    Dim key As String = tt1.Name & Const_JoinStaticString & tt2.Name & GetStaticKey()
    '    Dim key2 As String = tt2.Name & Const_JoinStaticString & tt1.Name & GetStaticKey()
    '    Dim new_id As String = obj.Identifier.ToString
    '    Dim old_id As String = oldid.ToString
    '    Dim sync As String = old_id & Const_KeyStaticString & key & GetTablePostfix
    '    Dim dic As IDictionary = GetDic(_cache, key & GetTablePostfix)

    '    If dic.Contains(old_id.ToString) Then
    '        Dim dt As System.Data.DataTable = GetDataTable(old_id, key & GetTablePostfix, sync, tt2, obj, Nothing, False, True, False)
    '        dic.Remove(old_id)
    '        dic.Add(new_id, dt)
    '        Using SyncHelper.AcquireDynamicLock(sync)
    '            Dim literal As String = tt1.Name & "ID"
    '            Dim literal2 As String = tt2.Name & "ID"
    '            If tt1 Is tt2 Then
    '                literal2 = literal & "Rev"
    '            End If
    '            For Each dr As System.Data.DataRow In dt.Select(literal & " = " & old_id)
    '                dr(literal) = obj.Identifier
    '                Dim l_id As Integer = CInt(dr(literal2))
    '                SubObjRelationUpdate(l_id, key2, literal2, literal, _
    '                        obj.Identifier, oldid, tt1, tt2)
    '            Next
    '        End Using
    '    End If
    'End Sub

    Protected Friend Sub M2MAdd(ByVal mainobj As OrmBase, ByVal subobj As OrmBase, ByVal direct As Boolean)
        If mainobj Is Nothing Then
            Throw New ArgumentNullException("mainobj")
        End If

        If subobj Is Nothing Then
            Throw New ArgumentNullException("subobj")
        End If

        M2MAddInternal(mainobj, subobj, direct)

        If mainobj.GetType Is subobj.GetType Then
            M2MAddInternal(subobj, mainobj, Not direct)
        Else
            M2MAddInternal(subobj, mainobj, direct)
        End If
    End Sub

    'Public Sub Obj2ObjRelationAdd(ByVal mainobj As OrmBase, ByVal subobj As OrmBase)
    '    Obj2ObjRelationAddInternal(mainobj, subobj)
    '    Obj2ObjRelationAddInternal(subobj, mainobj)
    'End Sub

    'Protected Friend Function FindM2MNonGeneric(ByVal mainobj As OrmBase, ByVal tt2 As Type, ByVal direct As Boolean) As M2MCache
    '    Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic
    '    'Dim pm As New Reflection.ParameterModifier(6)
    '    'pm(5) = True
    '    Dim types As Type() = New Type() {GetType(OrmBase), GetType(Boolean), GetType(CriteriaLink), GetType(Sort), GetType(Boolean)}
    '    Dim o() As Object = New Object() {mainobj, direct, Nothing, Nothing, False}
    '    'Dim m As M2MCache = CType(GetType(OrmManagerBase).InvokeMember("FindM2M", Reflection.BindingFlags.InvokeMethod Or Reflection.BindingFlags.NonPublic, _
    '    '    Nothing, Me, o, New Reflection.ParameterModifier() {pm}, Nothing, Nothing), M2MCache)
    '    Dim mi As Reflection.MethodInfo = GetType(OrmManagerBase).GetMethod("FindM2M", flags, Nothing, Reflection.CallingConventions.Any, types, Nothing)
    '    Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {tt2})
    '    Dim p As Pair(Of M2MCache, Boolean) = CType(mi_real.Invoke(Me, flags, Nothing, o, Nothing), Pair(Of M2MCache, Boolean))
    '    Return p.First
    'End Function

    Protected Friend Function FindM2MNonGeneric(ByVal mainobj As OrmBase, ByVal tt2 As Type, ByVal direct As Boolean) As Pair(Of M2MCache, Boolean)
        Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic
        'Dim pm As New Reflection.ParameterModifier(6)
        'pm(5) = True
        Dim types As Type() = New Type() {GetType(OrmBase), GetType(Boolean), GetType(IGetFilter), GetType(Sort), GetType(Boolean)}
        Dim o() As Object = New Object() {mainobj, direct, Nothing, Nothing, False}
        'Dim m As M2MCache = CType(GetType(OrmManagerBase).InvokeMember("FindM2M", Reflection.BindingFlags.InvokeMethod Or Reflection.BindingFlags.NonPublic, _
        '    Nothing, Me, o, New Reflection.ParameterModifier() {pm}, Nothing, Nothing), M2MCache)
        Dim mi As Reflection.MethodInfo = GetType(OrmManagerBase).GetMethod("FindM2M", flags, Nothing, Reflection.CallingConventions.Any, types, Nothing)
        Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {tt2})
        Dim p As Pair(Of M2MCache, Boolean) = CType(mi_real.Invoke(Me, flags, Nothing, o, Nothing), Pair(Of M2MCache, Boolean))
        Return p
    End Function

    Protected Friend Function GetM2MNonGeneric(ByVal obj As OrmBase, ByVal tt2 As Type, ByVal direct As Boolean) As M2MCache
        Dim tt1 As Type = obj.GetType

        Dim id As String = obj.Identifier.ToString

        Return GetM2MNonGeneric(id, tt1, tt2, direct)
    End Function

    Protected Friend Function GetM2MNonGeneric(ByVal id As String, ByVal tt1 As Type, ByVal tt2 As Type, ByVal direct As Boolean) As M2MCache
        Dim key As String = GetM2MKey(tt1, tt2, direct)

        Dim dic As IDictionary = GetDic(_cache, key)

        Return CType(dic(id), M2MCache)
    End Function

    Protected Sub M2MAddInternal(ByVal mainobj As OrmBase, ByVal subobj As OrmBase, ByVal direct As Boolean)
        If mainobj Is Nothing Then
            Throw New ArgumentNullException("mainobj")
        End If

        If subobj Is Nothing Then
            Throw New ArgumentNullException("subobj")
        End If

        Dim tt1 As Type = mainobj.GetType
        Dim tt2 As Type = subobj.GetType

        If _schema.IsMany2ManyReadonly(tt1, tt2) Then
            Throw New InvalidOperationException("Relation is readonly")
        End If

        Dim p As Pair(Of M2MCache, Pair(Of String)) = FindM2MReturnKeysNonGeneric(mainobj, tt2, direct)
        Dim m As M2MCache = p.First

#If DEBUG Then
        Dim cnt As Integer = m.Entry.Added.Count
#End If
        m.Entry.Add(subobj.Identifier)
#If DEBUG Then
        Debug.Assert(m.Entry.Added.Count = cnt + 1)
#End If
        mainobj.AddAccept(New OrmBase.AcceptState2(m, p.Second.First, p.Second.Second))
    End Sub

    Protected Sub M2MDeleteInternal(ByVal mainobj As OrmBase, ByVal subobj As OrmBase, ByVal direct As Boolean)
        If mainobj Is Nothing Then
            Throw New ArgumentNullException("mainobj")
        End If

        If subobj Is Nothing Then
            Throw New ArgumentNullException("subobj")
        End If

        Dim tt1 As Type = mainobj.GetType
        Dim tt2 As Type = subobj.GetType

        If _schema.IsMany2ManyReadonly(tt1, tt2) Then
            Throw New InvalidOperationException("Relation is readonly")
        End If

        Dim p As Pair(Of M2MCache, Pair(Of String)) = FindM2MReturnKeysNonGeneric(mainobj, tt2, direct)
        Dim m As M2MCache = p.First

        m.Entry.Delete(subobj.Identifier)

        mainobj.AddAccept(New OrmBase.AcceptState2(m, p.Second.First, p.Second.Second))
    End Sub

#End Region

    Public Function ApplyFilter(Of T As OrmBase)(ByVal col As ICollection(Of T), ByVal filter As IFilter, ByRef evaluated As Boolean) As ICollection(Of T)
        evaluated = True
        Dim f As IEntityFilter = TryCast(filter, IEntityFilter)
        If f Is Nothing Then
            Return col
        Else
            Dim l As New List(Of T)
            Dim oschema As IOrmObjectSchemaBase = Nothing
            Dim i As Integer = 0
            For Each o As T In col
                If oschema Is Nothing Then
                    oschema = _schema.GetObjectSchema(o.GetType)
                End If
                Dim er As IEvaluableValue.EvalResult = f.Eval(_schema, o, oschema)
                Select Case er
                    Case IEvaluableValue.EvalResult.Found
                        If i >= _start Then
                            l.Add(o)
                        End If
                    Case IEvaluableValue.EvalResult.Unknown
                        evaluated = False
                        Exit For
                End Select
                If i >= (_start + _length) Then
                    Exit For
                End If
                i += 1
            Next
            Return l
        End If
    End Function

    Public Shared Function ApplySort(Of T As {OrmBase})(ByVal c As ICollection(Of T), ByVal s As Sort, ByVal getObj As OrmComparer(Of T).GetObjectDelegate) As ICollection(Of T)
        Dim q As New Stack(Of Sort)
        If s IsNot Nothing AndAlso Not s.IsExternal Then
            Dim ns As Sort = s
            Do
                If ns.IsExternal Then
                    Throw New DBSchemaException("External sort must be alone")
                End If
                If ns.IsCustom Then
                    Throw New DBSchemaException("Custom sort is not supported")
                End If
                q.Push(ns)
                ns = ns.Previous
            Loop While ns IsNot Nothing

            Dim l As New List(Of T)(c)
            l.Sort(New OrmComparer(Of T)(q, getObj))
            c = l
        End If
        Return c
    End Function

    Public Shared Function ApplySort(Of T As {OrmBase})(ByVal c As ICollection(Of T), ByVal s As Sort) As ICollection(Of T)
        Return ApplySort(c, s, Nothing)
    End Function

    Public Shared Function ApplySortT(ByVal c As ICollection, ByVal s As Sort) As ICollection
        Dim q As New Stack(Of Sort)
        If s IsNot Nothing AndAlso Not s.IsExternal Then
            Dim ns As Sort = s
            Do
                If ns.IsExternal Then
                    Throw New DBSchemaException("External sort must be alone")
                End If
                If ns.IsCustom Then
                    Throw New DBSchemaException("Custom sort is not supported")
                End If
                q.Push(ns)
                ns = ns.Previous
            Loop While ns IsNot Nothing

            Dim l As New ArrayList(c)
            If l.Count > 0 Then
                l.Sort(New OrmComparer(Of OrmBase)(l(0).GetType, q))
                c = l
            End If
        End If
        Return c
    End Function

    Public Shared Function GetK(ByVal cnt As Integer) As Double
        Return 1
    End Function

    Public Shared Function IsGoodTime4Load(ByVal fetchTime As TimeSpan, ByVal execTime As TimeSpan, ByVal totalCount As Integer, ByVal loadedCount As Integer) As Boolean
        Dim tt As TimeSpan = TimeSpan.FromMilliseconds((fetchTime + execTime).TotalMilliseconds * GetK(totalCount))
        'Dim p As Pair(Of Integer, TimeSpan) = mc.Cache.GetLoadTime(GetType(T))
        'Dim tt As TimeSpan = TimeSpan.FromMilliseconds(fetchTime.TotalMilliseconds * 40) + execTime
        Dim slt As Double = 0.0005 '(fetchTime.TotalMilliseconds / totalCount)
        Dim ttl As TimeSpan = TimeSpan.FromSeconds(slt * (totalCount - loadedCount))
        Return tt > ttl
    End Function

    Public Function GetLoadedCount(Of T As OrmBase)(ByVal col As ICollection(Of T)) As Integer
        Dim r As Integer = 0
        For Each o As OrmBase In col
            If o.IsLoaded Then
                r += 1
            End If
        Next
        Return r
    End Function

    Public Function GetLoadedCount(Of T As OrmBase)(ByVal ids As IList(Of Integer)) As Integer
        Dim r As Integer = 0
        Dim dic As IDictionary(Of Integer, T) = GetDictionary(Of T)()
        For Each id As Integer In ids
            If dic.ContainsKey(id) Then
                r += 1
            End If
        Next
        Return r
    End Function

#Region " Search "

    Public Function Search(Of T As {OrmBase, New})(ByVal [string] As String) As ICollection(Of T)
        Invariant()

        If [string] IsNot Nothing AndAlso [string].Length > 0 Then
            'Dim ss() As String = Split4FullTextSearch([string], GetSearchSection)
            Return Search(Of T)(GetType(T), [string], Nothing, Nothing)
        End If
        Return New List(Of T)()
    End Function

    Public Function Search(Of T As {OrmBase, New})(ByVal type2search As Type, ByVal [string] As String, ByVal contextKey As Object) As ICollection(Of T)
        Return Search(Of T)(type2search, [string], Nothing, contextKey)
    End Function

    Public Function Search(Of T As {OrmBase, New})(ByVal type2search As Type, ByVal [string] As String, _
        ByVal sort As Sort, ByVal contextKey As Object) As ICollection(Of T)
        Dim selectType As Type = GetType(T)
        'If selectType IsNot type2search Then
        '    Dim field As String = _schema.GetJoinFieldNameByType(selectType, type2search, Nothing)

        '    If String.IsNullOrEmpty(field) Then
        '        Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2search))
        '    End If

        If [string] IsNot Nothing AndAlso [string].Length > 0 Then
            'Dim ss() As String = Split4FullTextSearch()
            'Dim join As OrmJoin = MakeJoin(type2search, selectType, field, FilterOperation.Equal, JoinType.Join, True)
            Return Search(Of T)(type2search, contextKey, sort, Nothing, New FtsDef([string], GetSearchSection))
            'End If
            Return New List(Of T)()
        Else
            Return Search(Of T)([string], sort, contextKey)
        End If
    End Function

    Public Function Search(Of T As {OrmBase, New})(ByVal [string] As String, ByVal sort As Sort, ByVal contextKey As Object) As ICollection(Of T)
        Return Search(Of T)([string], sort, contextKey, Nothing)
    End Function

    Class FtsDef
        Implements IFtsStringFormater

        Private Class FProxy

            Private _t As Type

            Public Sub New(ByVal t As Type)
                _t = t
            End Sub

            Public Function GetValue(ByVal tokens() As String, ByVal sectionName As String, _
                ByVal f As IOrmFullTextSupport, ByVal contextkey As Object) As String
                Return Configuration.SearchSection.GetValueForFreeText(_t, tokens, sectionName)
            End Function
        End Class

        Private _toks() As String
        Private _del As ValueForSearchDelegate
        'Private _sectionName As String

        Public Sub New(ByVal s As String, ByVal sectionName As String)
            _toks = Split4FullTextSearch(s, sectionName)
            '  _sectionName = sectionName
        End Sub

        Public Sub New(ByVal s As String, ByVal sectionName As String, ByVal del As ValueForSearchDelegate)
            MyClass.New(s, sectionName)
            _del = del
        End Sub

        Public Function GetFtsString(ByVal section As String, ByVal contextKey As Object, _
            ByVal f As IOrmFullTextSupport, ByVal type2search As Type, ByVal ftsString As String) As String Implements IFtsStringFormater.GetFtsString
            If _del Is Nothing Then
                If ftsString = "freetexttable" Then
                    Return New FProxy(type2search).GetValue(_toks, section, f, contextKey)
                ElseIf ftsString = "containstable" Then
                    Return Configuration.SearchSection.GetValueForContains(_toks, section, f, contextKey)
                Else
                    Throw New NotSupportedException
                End If
            Else
                Return _del(_toks, section, f, contextKey)
            End If
        End Function

        Public Function GetTokens() As String() Implements IFtsStringFormater.GetTokens
            Return _toks
        End Function
    End Class

    Public Function Search(Of T As {OrmBase, New})(ByVal [string] As String, ByVal sort As Sort, _
        ByVal contextKey As Object, ByVal filter As IFilter) As ICollection(Of T)
        Invariant()

        If [string] IsNot Nothing AndAlso [string].Length > 0 Then
            Return Search(Of T)(GetType(T), contextKey, sort, filter, New FtsDef([string], GetSearchSection))
        End If
        Return New List(Of T)()
    End Function

    Public Function Search(Of T As {OrmBase, New})(ByVal [string] As String, ByVal sort As Sort, _
        ByVal contextKey As Object, ByVal filter As IFilter, ByVal ftsText As String, _
        ByVal limit As Integer, ByVal del As ValueForSearchDelegate) As ICollection(Of T)
        Invariant()

        If [string] IsNot Nothing AndAlso [string].Length > 0 Then
            Return SearchEx(Of T)(GetType(T), contextKey, sort, filter, ftsText, limit, New FtsDef([string], GetSearchSection, del))
        End If
        Return New List(Of T)()
    End Function

    Public Function Search(Of T As {OrmBase, New})(ByVal [string] As String, ByVal sort As Sort, _
       ByVal contextKey As Object, ByVal filter As IFilter, ByVal ftsText As String, _
       ByVal limit As Integer, ByVal frmt As IFtsStringFormater) As ICollection(Of T)
        Invariant()

        If [string] IsNot Nothing AndAlso [string].Length > 0 Then
            Return SearchEx(Of T)(GetType(T), contextKey, sort, filter, ftsText, limit, frmt)
        End If
        Return New List(Of T)()
    End Function

    'Public Function Search(Of T As {OrmBase, New})(ByVal [string] As String, _
    '    ByVal del As DbSchema.ValueForSearchDelegate) As ICollection(Of T)
    '    Invariant()

    '    If [string] IsNot Nothing AndAlso [string].Length > 0 Then
    '        Dim ss() As String = Split4FullTextSearch([string], GetSearchSection)
    '        Return SearchEx(Of T)(GetType(T), ss, Nothing, Nothing, Nothing, "containstable", Integer.MinValue, del)
    '    End If
    '    Return New List(Of T)()
    'End Function

    Protected Shared Function Split4FullTextSearch(ByVal str As String, ByVal sectionName As String) As String()
        If str Is Nothing Then
            Throw New ArgumentNullException("str parameter cannot be nothing")
        End If

        If str.StartsWith("'"c) AndAlso str.EndsWith("'"c) Then
            str = """" & str.Remove(0, 1)
            str = str.Remove(str.Length - 1) & """"
        End If
        If str.StartsWith("«"c) AndAlso str.EndsWith("»"c) Then
            str = """" & str.Remove(0, 1)
            str = str.Remove(str.Length - 1) & """"
        End If
        str = str.Replace("&", "").Replace("|", "").Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "").Replace("/", "").Replace("\", "").Replace(vbTab, "")
        Dim ss() As String = Split4FullTextSearchInternal(str)
        Dim firstcomma As Integer = -1
        Dim r As New Generic.List(Of String)
        Dim sc As Configuration.SearchSection = Configuration.SearchSection.GetSection(sectionName)
        For i As Integer = 0 To ss.Length - 1
            Dim s As String = ss(i)
            s = s.TrimEnd("!"c)
            If s.StartsWith(""""c) OrElse s.StartsWith("'"c) Then
                firstcomma = i
            ElseIf s.EndsWith(""""c) OrElse s.EndsWith("'"c) Then
                If firstcomma <> -1 Then
                    Dim sb As New StringBuilder
                    For j As Integer = firstcomma To i
                        If j <> firstcomma Then sb.Append(" "c)
                        sb.Append(ss(j).Replace("""", String.Empty))
                    Next
                    If firstcomma = 0 Then r.Clear()
                    'If sc Is Nothing OrElse sb.Length >= sc.minTokenLength Then
                    r.Add(sb.ToString)
                    'End If
                    firstcomma = -1
                Else
                    Dim _s As String = s.Replace("""", String.Empty)
                    'If sc Is Nothing OrElse _s.Length >= sc.minTokenLength Then
                    If sc IsNot Nothing AndAlso sc.ReplaceSection IsNot Nothing Then
                        Dim sss() As String = Nothing
                        For Each re As Configuration.SearchReplaceElement In sc.ReplaceSection
                            If re.From.Equals(_s, StringComparison.InvariantCultureIgnoreCase) Then
                                sss = Split4FullTextSearchInternal(re.To)
                                Exit For
                            End If
                        Next
                        If sss Is Nothing Then
                            r.Add(_s)
                        Else
                            For Each ssss As String In sss
                                r.Add(ssss)
                            Next
                        End If
                    Else
                        r.Add(_s)
                    End If
                    'End If
                End If
            Else
                Dim _s As String = s.Replace("""", String.Empty)
                'If sc Is Nothing OrElse _s.Length >= sc.minTokenLength Then
                If sc IsNot Nothing AndAlso sc.ReplaceSection IsNot Nothing Then
                    Dim sss() As String = Nothing
                    For Each re As Configuration.SearchReplaceElement In sc.ReplaceSection
                        If re.From.Equals(_s, StringComparison.InvariantCultureIgnoreCase) Then
                            sss = Split4FullTextSearchInternal(re.To)
                            Exit For
                        End If
                    Next
                    If sss Is Nothing Then
                        r.Add(_s)
                    Else
                        For Each ssss As String In sss
                            r.Add(ssss)
                        Next
                    End If
                Else
                    r.Add(_s)
                End If
                'End If
            End If
        Next
        If firstcomma <> -1 Then
            If _mcSwitch.TraceInfo Then
                WriteLine("Unclosed comma <" & str & ">")
            End If
            For j As Integer = firstcomma To ss.Length - 1
                Dim s As String = ss(j).Replace("""", String.Empty)
                'If Not String.IsNullOrEmpty(s) AndAlso (sc Is Nothing OrElse s.Length >= sc.minTokenLength) Then
                If sc IsNot Nothing AndAlso sc.ReplaceSection IsNot Nothing Then
                    Dim sss() As String = Nothing
                    For Each re As Configuration.SearchReplaceElement In sc.ReplaceSection
                        If re.From.Equals(s, StringComparison.InvariantCultureIgnoreCase) Then
                            sss = Split4FullTextSearchInternal(re.To)
                            Exit For
                        End If
                    Next
                    If sss Is Nothing Then
                        r.Add(s)
                    Else
                        For Each ssss As String In sss
                            r.Add(ssss)
                        Next
                    End If
                Else
                    r.Add(s)
                End If
                'End If
            Next
        End If
        Return r.ToArray
    End Function

    Protected Shared Function Split4FullTextSearchInternal(ByVal str As String) As String()
        If str Is Nothing Then
            Throw New ArgumentNullException("str parameter cannot be nothing")
        End If

        Dim ss() As String = str.Split(","c)
        If ss.Length = 1 Then
            ss = str.Split(" "c)
            If ss.Length = 1 Then
                ss = str.Split("_"c)
            Else
                Dim arr As New Generic.List(Of String)
                For Each s As String In ss
                    If s <> "" Then arr.AddRange(Split4FullTextSearchInternal(s))
                Next
                ss = arr.ToArray
            End If
        Else
            Dim arr As New Generic.List(Of String)
            For Each s As String In ss
                If s <> "" Then arr.AddRange(Split4FullTextSearchInternal(s))
            Next
            ss = arr.ToArray
        End If
        Return ss
    End Function

#End Region

    ''' <summary>
    ''' Load parent objects from collection of childs
    ''' </summary>
    ''' <typeparam name="T">Type of child collection</typeparam>
    ''' <param name="objs">Child collection</param>
    ''' <param name="fields">Array of properties in child type, used to get parent object</param>
    ''' <param name="start">Point in child collection from where start to load</param>
    ''' <param name="length">Length of loaded window</param>
    ''' <returns>Collection of child objects</returns>
    ''' <remarks></remarks>
    Public Function LoadObjects(Of T As {OrmBase, New})(ByVal objs As ICollection(Of T), ByVal fields() As String, _
        ByVal start As Integer, ByVal length As Integer) As ICollection(Of T)

        Dim col As ICollection(Of T) = LoadObjectsInternal(objs, start, length, True)

        If fields Is Nothing OrElse fields.Length = 0 Then
            Return col
        End If

        Dim prop_objs(fields.Length - 1) As IList

        Dim lt As Type = GetType(List(Of ))
        Dim oschema As IOrmObjectSchemaBase = _schema.GetObjectSchema(GetType(T))

        For Each o As T In col
            For i As Integer = 0 To fields.Length - 1
                'Dim obj As OrmBase = CType(ObjectSchema.GetFieldValue(o, fields(i)), OrmBase)
                Dim obj As OrmBase = CType(o.GetValue(fields(i), oschema), OrmBase)
                If obj IsNot Nothing Then
                    If prop_objs(i) Is Nothing Then
                        prop_objs(i) = CType(Activator.CreateInstance(lt.MakeGenericType(obj.GetType)), IList)
                    End If
                    prop_objs(i).Add(obj)
                End If
            Next
        Next

        If _LoadObjectsMI Is Nothing Then
            Dim mis() As Reflection.MemberInfo = Me.GetType.GetMember("LoadObjects")

            For Each mri_ As Reflection.MemberInfo In mis
                Dim mi_ As Reflection.MethodInfo = TryCast(mri_, Reflection.MethodInfo)
                If mi_ IsNot Nothing AndAlso mi_.GetParameters.Length = 1 Then
                    _LoadObjectsMI = mi_
                    Exit For
                End If
            Next

            If _LoadObjectsMI Is Nothing Then
                Throw New OrmManagerException("Cannot find method LoadObjects")
            End If
        End If

        For Each po As IList In prop_objs
            If po IsNot Nothing AndAlso po.Count > 0 Then
                Dim tt As Type = po(0).GetType
                _LoadObjectsMI.MakeGenericMethod(New Type() {tt}).Invoke(Me, Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public, Nothing, _
                    New Object() {po}, Nothing)
            End If
        Next

        Return col
    End Function

    Public Function LoadObjects(Of T As {OrmBase, New})(ByVal objs As ICollection(Of T), ByVal start As Integer, ByVal length As Integer) As ICollection(Of T)
        Return LoadObjectsInternal(objs, start, length, True)
    End Function

    Public Function LoadObjects(Of T As {OrmBase, New})(ByVal objs As ICollection(Of T)) As ICollection(Of T)
        Return LoadObjectsInternal(objs, 0, objs.Count, True)
    End Function

    'Protected Sub SetIsLoaded(ByVal obj As OrmBase, Optional ByVal loaded As Boolean = True)
    '    obj.IsLoaded = loaded
    'End Sub

    Public Overridable Function ConvertIds2Objects(ByVal t As Type, ByVal ids As ICollection(Of Integer), ByVal check As Boolean) As ICollection
        Dim self_t As Type = Me.GetType
        Dim mis() As Reflection.MemberInfo = self_t.GetMember("ConvertIds2Objects", Reflection.MemberTypes.Method, _
            Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public)
        For Each mmi As Reflection.MemberInfo In mis
            If TypeOf (mmi) Is Reflection.MethodInfo Then
                Dim mi As Reflection.MethodInfo = CType(mmi, Reflection.MethodInfo)
                If mi.IsGenericMethod Then
                    mi = mi.MakeGenericMethod(New Type() {t})
                    Return CType(mi.Invoke(Me, New Object() {ids, check}), System.Collections.ICollection)
                End If
            End If
        Next
        Throw New InvalidOperationException(String.Format("Method {0} not found", "ConvertIds2Objects"))
    End Function

    Public Overridable Function ConvertIds2Objects(Of T As {OrmBase, New})(ByVal ids As ICollection(Of Integer), ByVal check As Boolean) As ICollection(Of T)
        Dim arr As New Generic.List(Of T)

        Dim type As Type = GetType(T)
        For Each id As Integer In ids
            Dim obj As T = Nothing
            If Not check Then
                obj = CreateDBObject(Of T)(id)
            Else
                obj = LoadType(Of T)(id, False, check)
            End If

            If obj IsNot Nothing Then
                arr.Add(obj)
            ElseIf _newMgr IsNot Nothing Then
                obj = CType(_newMgr.GetNew(type, id), T)
                If obj IsNot Nothing Then arr.Add(obj)
            End If
        Next
        Return arr
    End Function

    Public Overridable Function ConvertIds2Objects(Of T As {OrmBase, New})(ByVal ids As IList(Of Integer), _
        ByVal start As Integer, ByVal length As Integer, ByVal check As Boolean) As ICollection(Of T)

        Dim arr As New Generic.List(Of T)
        If start < ids.Count Then
            Dim type As Type = GetType(T)
            length = Math.Min(length + start, ids.Count)
            For i As Integer = start To length - 1
                Dim id As Integer = ids(i)
                Dim obj As T = Nothing
                If Not check Then
                    obj = CreateDBObject(Of T)(id)
                Else
                    obj = LoadType(Of T)(id, False, check)
                End If

                If obj IsNot Nothing Then
                    arr.Add(obj)
                ElseIf _newMgr IsNot Nothing Then
                    obj = CType(_newMgr.GetNew(type, id), T)
                    If obj IsNot Nothing Then arr.Add(obj)
                End If
            Next
        End If
        'Try
        Return arr
        'Catch ex As InvalidCastException
        'Throw New OrmManagerException("Error converting type " & type.ToString, ex)
        'End Try
    End Function

    Public Function LoadObjectsIds(Of T As {OrmBase, New})(ByVal ids As ICollection(Of Integer)) As ICollection(Of T)
        Return LoadObjects(Of T)(ConvertIds2Objects(Of T)(ids, False))
    End Function

    Public Function LoadObjectsIds(Of T As {OrmBase, New})(ByVal ids As IList(Of Integer), ByVal start As Integer, ByVal length As Integer) As ICollection(Of T)
        Return LoadObjects(Of T)(ConvertIds2Objects(Of T)(ids, start, length, False))
    End Function

    Protected Friend Shared Function _GetDic(ByVal cache As OrmCacheBase, ByVal key As String) As IDictionary
        Dim dic As IDictionary = CType(cache.Filters(key), IDictionary)
        If dic Is Nothing Then
            Using SyncHelper.AcquireDynamicLock(key)
                dic = CType(cache.Filters(key), IDictionary)
                If dic Is Nothing Then
                    dic = cache.CreateResultsetsDictionary() 'Hashtable.Synchronized(New Hashtable)
                    cache.Filters.Add(key, dic)
                End If
            End Using
        End If
        Return dic
    End Function

    Protected Friend Function GetDic(ByVal cache As OrmCacheBase, ByVal key As String) As IDictionary
        Dim dic As IDictionary = CType(cache.Filters(key), IDictionary)
        If dic Is Nothing Then
            Using SyncHelper.AcquireDynamicLock(key)
                dic = CType(cache.Filters(key), IDictionary)
                If dic Is Nothing Then
                    dic = cache.CreateResultsetsDictionary(_list) 'Hashtable.Synchronized(New Hashtable)
                    cache.Filters.Add(key, dic)
                End If
            End Using
        End If
        Return dic
    End Function

    Protected Shared Function GetDics(ByVal cache As OrmCacheBase, ByVal key As String, ByVal postfix As String) As IEnumerable(Of IDictionary)
        Dim dics As New List(Of IDictionary)
        For Each de As DictionaryEntry In cache.Filters
            Dim k As String = CStr(de.Key)
            If k <> key AndAlso k.StartsWith(key) AndAlso (String.IsNullOrEmpty(postfix) OrElse k.EndsWith(postfix)) Then
                dics.Add(CType(de.Value, System.Collections.IDictionary))
            End If
        Next
        Return dics
    End Function

    Private Shared Function GetFilter(ByVal criteria As IGetFilter, ByVal t As Type) As IFilter
        If criteria IsNot Nothing Then
            Return criteria.Filter(t)
        End If
        Return Nothing
    End Function

    Protected Friend Overridable Function GetFilterInfo() As Object
        Return Nothing
    End Function

#Region " Abstract members "

    Protected MustOverride Function GetSearchSection() As String

    'Protected MustOverride Function SearchEx(Of T As {OrmBase, New})(ByVal type2search As Type, ByVal tokens() As String, _
    '    ByVal contextKey As Object, ByVal sort As Sort, ByVal filter As IFilter, ByVal ftsText As String, _
    '    ByVal limit As Integer, ByVal del As DbSchema.ValueForSearchDelegate) As ICollection(Of T)

    Protected MustOverride Function SearchEx(Of T As {OrmBase, New})(ByVal type2search As Type, _
        ByVal contextKey As Object, ByVal sort As Sort, ByVal filter As IFilter, ByVal ftsText As String, _
        ByVal limit As Integer, ByVal frmt As IFtsStringFormater) As ICollection(Of T)

    Protected MustOverride Function Search(Of T As {OrmBase, New})( _
        ByVal type2search As Type, ByVal contextKey As Object, ByVal sort As Sort, ByVal filter As IFilter, _
        ByVal frmt As IFtsStringFormater) As ICollection(Of T)

    Protected Friend MustOverride Function GetStaticKey() As String

    Protected MustOverride Function GetCustDelegate(Of T As {OrmBase, New})(ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal key As String, ByVal id As String) As ICustDelegate(Of T)

    Protected MustOverride Function GetCustDelegate(Of T As {OrmBase, New})(ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal key As String, ByVal id As String, ByVal cols() As String) As ICustDelegate(Of T)

    Protected MustOverride Function GetCustDelegate(Of T As {OrmBase, New})(ByVal aspect As QueryAspect, ByVal join() As OrmJoin, ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal key As String, ByVal id As String) As ICustDelegate(Of T)

    Protected MustOverride Function GetCustDelegate(Of T As {OrmBase, New})(ByVal relation As M2MRelation, ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal key As String, ByVal id As String) As ICustDelegate(Of T)

    'Protected MustOverride Function GetCustDelegate4Top(Of T As {OrmBase, New})(ByVal top As Integer, ByVal filter As IOrmFilter, _
    '    ByVal sort As Sort, ByVal key As String, ByVal id As String) As ICustDelegate(Of T)

    Protected MustOverride Function GetCustDelegate(Of T2 As {OrmBase, New})( _
        ByVal obj As OrmBase, ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal id As String, ByVal key As String, ByVal direct As Boolean) As ICustDelegate(Of T2)

    'Protected MustOverride Function GetCustDelegateTag(Of T As {OrmBase, New})( _
    '    ByVal obj As T, ByVal filter As IOrmFilter, ByVal sort As String, ByVal sortType As SortType, ByVal id As String, ByVal sync As String, ByVal key As String) As ICustDelegate(Of T)

    'Protected MustOverride Function GetDataTableInternal(ByVal t As Type, ByVal obj As OrmBase, ByVal filter As IOrmFilter, ByVal appendJoins As Boolean, Optional ByVal tag As String = Nothing) As System.Data.DataTable

    Protected MustOverride Function GetObjects(Of T As {OrmBase, New})(ByVal ids As Generic.IList(Of Integer), ByVal f As IFilter, ByVal objs As IList(Of T), _
       ByVal withLoad As Boolean, ByVal fieldName As String, ByVal idsSorted As Boolean) As Generic.IList(Of T)

    Protected MustOverride Function GetObjects(Of T As {OrmBase, New})(ByVal type As Type, ByVal ids As Generic.IList(Of Integer), ByVal f As IFilter, _
       ByVal relation As M2MRelation, ByVal idsSorted As Boolean, ByVal withLoad As Boolean) As IDictionary(Of Integer, EditableList)

    Protected Friend MustOverride Sub LoadObject(ByVal obj As OrmBase)

    Protected Friend MustOverride Function LoadObjectsInternal(Of T As {OrmBase, New})(ByVal objs As ICollection(Of T), ByVal start As Integer, ByVal length As Integer, ByVal remove_not_found As Boolean, ByVal columns As Generic.List(Of ColumnAttribute)) As ICollection(Of T)
    Protected Friend MustOverride Function LoadObjectsInternal(Of T As {OrmBase, New})(ByVal objs As ICollection(Of T), ByVal start As Integer, ByVal length As Integer, ByVal remove_not_found As Boolean) As ICollection(Of T)

    'Protected MustOverride Overloads Sub FindObjects(ByVal t As Type, ByVal WithLoad As Boolean, ByVal arr As System.Collections.ArrayList, ByVal sort As String, ByVal sort_type As SortType)

    Protected Friend MustOverride Sub SaveObject(ByVal obj As OrmBase)

    Public MustOverride Function Add(ByVal obj As OrmBase) As OrmBase

    Protected Friend MustOverride Sub DeleteObject(ByVal obj As OrmBase)

    Public MustOverride Function SaveAll(ByVal obj As OrmBase, ByVal AcceptChanges As Boolean) As Boolean

    Protected MustOverride Sub M2MSave(ByVal obj As OrmBase, ByVal t As Type, ByVal direct As Boolean, ByVal el As EditableList)

    'Protected MustOverride Function FindObmsByOwnerInternal(ByVal id As Integer, ByVal original_type As Type, ByVal type As Type, ByVal sort As String, ByVal sort_type As SortType) As OrmBase()

    'Protected MustOverride Function FindTopInternal(ByVal original_type As Type, ByVal type As Type, ByVal top As Integer, ByVal sort As String, ByVal sort_type As SortType) As OrmBase()

    'Public MustOverride Function CheckObjects(ByVal objs() As OrmBase) As OrmBase()

    'Protected MustOverride Function FindObjsDirect(ByVal obj() As OrmBase, ByVal filter_key As String, _
    '    ByVal withLoad As Boolean, ByVal filter As OrmFilter, _
    '    ByVal t As Type, ByVal JoinColumn As String, _
    '    ByVal columns As Generic.List(Of ColumnAttribute)) As OrmBase()

    'Protected MustOverride Sub Obj2ObjRelationSave2(ByVal obj As OrmBase, ByVal dt As System.Data.DataTable, ByVal sync As String, ByVal t As System.Type)

    Protected MustOverride Function MakeJoin(ByVal type2join As Type, ByVal selectType As Type, ByVal field As String, _
        ByVal oper As FilterOperation, ByVal joinType As JoinType, Optional ByVal switchTable As Boolean = False) As OrmJoin

    Protected MustOverride ReadOnly Property Exec() As TimeSpan
    Protected MustOverride ReadOnly Property Fecth() As TimeSpan

#End Region

    Protected MustOverride Function BuildDictionary(Of T As {New, OrmBase})(ByVal level As Integer, ByVal filter As IFilter, ByVal join As OrmJoin) As DicIndex(Of T)

    Protected Friend Sub RegisterInCashe(ByVal obj As OrmBase)
        If Not IsInCache(obj) Then
            AddObject(obj)
            If obj.OriginalCopy IsNot Nothing Then
                Me.Cache.RegisterExistingModification(obj, obj.Identifier)
            End If
        End If
    End Sub

    Public Function BuildObjDictionary(Of T As {New, OrmBase})(ByVal level As Integer, ByVal criteria As IGetFilter, ByVal join As OrmJoin) As DicIndex(Of T)

        Dim key As String = String.Empty

        Dim tt As System.Type = GetType(T)
        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            key = criteria.Filter(tt).ToStaticString & _schema.GetEntityKey(GetFilterInfo, tt) & GetStaticKey() & "Dics"
        Else
            key = _schema.GetEntityKey(GetFilterInfo, tt) & GetStaticKey() & "Dics"
        End If

        Dim dic As IDictionary = GetDic(_cache, key)

        Dim f As String = String.Empty
        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            f = criteria.Filter(tt).ToString
        End If

        Dim id As String = f & " - dics - "
        Dim sync As String = id & GetStaticKey()

        Dim roots As DicIndex(Of T) = CType(dic(id), DicIndex(Of T))
        Invariant()

        If roots Is Nothing Then
            Using SyncHelper.AcquireDynamicLock(sync)
                roots = CType(dic(id), DicIndex(Of T))
                If roots Is Nothing Then
                    roots = BuildDictionary(Of T)(level, GetFilter(criteria, tt), join)
                    dic.Add(id, roots)
                End If
            End Using
        End If

        Return roots
    End Function

    Protected Shared Sub BuildDic(Of T As {New, OrmBase})(ByVal name As String, ByVal cnt As Integer, _
        ByVal level As Integer, ByVal root As DicIndex(Of T), ByRef last As DicIndex(Of T), _
        ByRef first As Boolean)
        'If name.Length = 0 Then name = "<без имени>"

        Dim current As DicIndex(Of T) = Nothing

        Dim p As DicIndex(Of T) = Nothing
        If Not first Then
            Dim i As Integer = FirstDiffCharIndex(name, last.Name)
            Debug.Assert(i <= level)

            If last.Name = "" Then
                p = root
            Else
                p = GetParent(last, last.Name.Length - i)
            End If
        Else
            p = last
            first = False
        End If
l1:
        If p Is root And name <> "" Then
            If name(0) = "'" Then
                Dim s As New DicIndex(Of T)("'", Nothing, 0)
                p = CType(root.Dictionary(s), DicIndex(Of T))
                If p IsNot Nothing Then GoTo l1
            End If
            Dim _prev As DicIndex(Of T) = root
            For k As Integer = 1 To name.Length
                Dim c As Integer = 0
                If k = name.Length Then c = cnt
                Dim s As New DicIndex(Of T)(name.Substring(0, k), _prev, c)
                If _prev Is root Then
                    If root.Dictionary.Contains(s.Name) Then
                        s = CType(root.Dictionary(s.Name), DicIndex(Of T))
                    Else
                        'If s.Name = "N" Then
                        '    For Each kd As MediaIndex(Of T) In arr.Keys
                        '        Debug.WriteLine(kd.Name & " " & kd.Name.GetHashCode)
                        '    Next
                        '    Debug.WriteLine("--------")
                        'End If
                        root.Add2Dictionary(s)
                        'root.Dictionary.Add(s.Name, s)
                        root.AddChild(s)
                    End If
                Else
                    root.Add2Dictionary(s)
                    _prev.AddChild(s)
                End If
                _prev = s
            Next
            current = _prev
        Else
            current = New DicIndex(Of T)(name, p, cnt)
            p.AddChild(current)
            root.Add2Dictionary(current)
        End If

        'End If

        last = current
    End Sub

    Protected Shared Function GetParent(Of T As {New, OrmBase})(ByVal mi As DicIndex(Of T), ByVal level As Integer) As DicIndex(Of T)
        Dim p As DicIndex(Of T) = mi
        For i As Integer = 0 To level
            If p Is Nothing Then Return p
            p = p.Parent
        Next
        Return p
    End Function

    <CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062")> _
    Public Shared Function FirstDiffCharIndex(ByVal n1 As String, ByVal n2 As String) As Integer
        Dim l As String
        If n1 Is Nothing Then
            If n2 IsNot Nothing Then
                Return 1
            End If
        Else
            If n2 Is Nothing Then
                Return 1
            End If
        End If
        If n1.Length > n2.Length Then
            l = n2
        Else
            l = n1
        End If

        For i As Integer = 0 To l.Length - 1
            If n1(i) <> n2(i) Then Return i + 1
        Next

        Return l.Length + 1
    End Function

    Protected Function HasJoins(ByVal selectType As Type, ByRef filter As IFilter, ByVal s As Sort, ByRef joins() As OrmJoin) As Boolean
        Dim l As New List(Of OrmJoin)
        Dim oschema As IOrmObjectSchemaBase = _schema.GetObjectSchema(selectType)
        Dim types As New List(Of Type)
        If filter IsNot Nothing Then
            For Each fl As IFilter In filter.GetAllFilters
                Dim f As IEntityFilter = TryCast(fl, IEntityFilter)
                If f IsNot Nothing Then
                    Dim type2join As System.Type = CType(f.Template, OrmFilterTemplate).Type
                    If type2join IsNot selectType AndAlso Not types.Contains(type2join) Then
                        Dim field As String = _schema.GetJoinFieldNameByType(selectType, type2join, oschema)

                        If String.IsNullOrEmpty(field) Then
                            Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2join))
                        End If

                        types.Add(type2join)

                        l.Add(MakeJoin(type2join, selectType, field, FilterOperation.Equal, JoinType.Join))

                        Dim ts As IOrmObjectSchema = CType(_schema.GetObjectSchema(type2join), IOrmObjectSchema)
                        Dim pk_table As OrmTable = ts.GetTables(0)
                        For i As Integer = 1 To ts.GetTables.Length - 1
                            Dim join As OrmJoin = ts.GetJoins(pk_table, ts.GetTables(i))

                            If Not OrmJoin.IsEmpty(join) Then
                                l.Add(join)
                            End If
                        Next

                        Dim newfl As IFilter = ts.GetFilter(GetFilterInfo)
                        If newfl IsNot Nothing Then
                            Dim con As Conditions.Condition.ConditionConstructorBase = ObjectSchema.CreateConditionCtor
                            con.AddFilter(filter)
                            con.AddFilter(newfl)
                            filter = con.Condition
                        End If
                    End If
                End If
            Next
        End If

        If s IsNot Nothing Then
            Dim ns As Sort = s
            Do
                Dim sortType As System.Type = ns.Type
                If sortType IsNot selectType AndAlso sortType IsNot Nothing AndAlso Not types.Contains(sortType) Then
                    Dim field As String = _schema.GetJoinFieldNameByType(selectType, sortType, oschema)

                    If Not String.IsNullOrEmpty(field) Then
                        types.Add(sortType)
                        l.Add(MakeJoin(sortType, selectType, field, FilterOperation.Equal, JoinType.Join))
                        Continue Do
                    End If

                    'Dim sschema As IOrmObjectSchemaBase = _schema.GetObjectSchema(sortType)
                    'field = _schema.GetJoinFieldNameByType(sortType, selectType, sschema)
                    If String.IsNullOrEmpty(field) Then
                        Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, sortType))
                    End If

                    'types.Add(sortType)
                    'l.Add(MakeJoin(selectType, sortType, field, FilterOperation.Equal, JoinType.Join, True))
                End If
                ns = ns.Previous
            Loop While ns IsNot Nothing
        End If
        joins = l.ToArray
        Return joins.Length > 0
    End Function

End Class

'End Namespace
