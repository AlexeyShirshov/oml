#Const DontUseStringIntern = True

Imports System.Threading
Imports System.Collections.Generic
Imports CoreFramework.Structures
Imports CoreFramework.Threading

Namespace Orm

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

        Public Class CacheListSwitcher
            Implements IDisposable

            Private disposedValue As Boolean
            Private _mgr As OrmManagerBase
            Private _oldvalue As Boolean

            Public Sub New(ByVal mgr As OrmManagerBase, ByVal cache_lists As Boolean)
                _mgr = mgr
                _oldvalue = mgr._dont_cache_lists
                mgr._dont_cache_lists = Not cache_lists
            End Sub

            Protected Overridable Sub Dispose(ByVal disposing As Boolean)
                If Not Me.disposedValue Then
                    _mgr._dont_cache_lists = _oldvalue
                End If
                Me.disposedValue = True
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
            Protected _f As IOrmFilter

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

            Public Sub New(ByVal sort As Sort, ByVal filter As IOrmFilter, ByVal obj As IEnumerable, ByVal mc As OrmManagerBase)
                _sort = sort
                '_st = sortType
                _obj = mc.ListConverter.ToWeakList(obj)
                _cache = mc.Cache
                If obj IsNot Nothing Then _cache.RegisterCreationCacheItem(Me.GetType)
                _f = filter
            End Sub

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

            Public Overridable Function GetObjectList(Of T As {OrmBase, New})(ByVal mc As OrmManagerBase, ByVal withLoad As Boolean, ByVal created As Boolean) As ICollection(Of T)
                Return mc.ListConverter.FromWeakList(Of T)(_obj, mc, withLoad, created)
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

            Public ReadOnly Property Filter() As IOrmFilter
                Get
                    Return _f
                End Get
            End Property
        End Class

        Public Class M2MCache
            Inherits CachedItem

            Public Sub New(ByVal sort As Sort, ByVal filter As IOrmFilter, _
                ByVal mainId As Integer, ByVal obj As IList(Of Integer), ByVal mc As OrmManagerBase, _
                ByVal mainType As Type, ByVal subType As Type, ByVal direct As Boolean)
                _sort = sort
                '_st = sortType
                _cache = mc.Cache
                If obj IsNot Nothing Then
                    _cache.RegisterCreationCacheItem(Me.GetType)
                    _obj = New EditableList(mainId, obj, mainType, subType, direct, sort)
                End If
                _f = filter
            End Sub

            Public Sub New(ByVal sort As Sort, ByVal filter As IOrmFilter, _
                ByVal el As EditableList, ByVal mc As OrmManagerBase)
                _sort = sort
                '_st = SortType
                _cache = mc.Cache
                If el IsNot Nothing Then
                    _cache.RegisterCreationCacheItem(Me.GetType)
                    _obj = el
                End If
                _f = filter
            End Sub

            'Public ReadOnly Property List() As EditableList
            '    Get
            '        Return CType(_obj, EditableList)
            '    End Get
            'End Property

            Public Overrides Function GetObjectList(Of T As {New, OrmBase})(ByVal mc As OrmManagerBase, ByVal withLoad As Boolean, ByVal created As Boolean) As System.Collections.Generic.ICollection(Of T)
                'Return mc.ListConverter.FromWeakList(Of T)(, mc, withLoad, created)
                If withLoad Then
                    Return mc.LoadObjectsIds(Of T)(Entry.Current)
                Else
                    Return mc.ConvertIds2Objects(Of T)(Entry.Current, False)
                End If
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
        End Class

        Public Interface ICustDelegate(Of T As {OrmBase, New})
            Function GetValues(ByVal withLoad As Boolean) As Generic.ICollection(Of T)
            Property Created() As Boolean
            Property Renew() As Boolean
            ReadOnly Property SmartSort() As Boolean
            ReadOnly Property Sort() As Sort
            'ReadOnly Property SortType() As SortType
            ReadOnly Property Filter() As IOrmFilter
            Sub CreateDepends()
            Function GetCacheItem(ByVal withLoad As Boolean) As CachedItem
            Function GetCacheItem(ByVal col As ICollection(Of T)) As CachedItem
        End Interface

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
                    _renew = value
                End Set
            End Property

            Public Overridable Property Created() As Boolean Implements ICustDelegate(Of T).Created
                Get
                    Return _created
                End Get
                Set(ByVal Value As Boolean)
                    _created = value
                End Set
            End Property

            Public Overridable ReadOnly Property SmartSort() As Boolean Implements ICustDelegate(Of T).SmartSort
                Get
                    Return True
                End Get
            End Property

            Public MustOverride Function GetValues(ByVal withLoad As Boolean) As Generic.ICollection(Of T) Implements ICustDelegate(Of T).GetValues
            Public MustOverride Sub CreateDepends() Implements ICustDelegate(Of T).CreateDepends
            Public MustOverride ReadOnly Property Filter() As IOrmFilter Implements ICustDelegate(Of T).Filter
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
        Private _prev As OrmManagerBase = Nothing
        'Protected hide_deleted As Boolean = True
        'Protected _check_status As Boolean = True
        Protected _schema As OrmSchemaBase
        'Private _findnew As FindNew
        'Private _remnew As RemoveNew
        Protected Friend _disposed As Boolean = False
        'Protected _sites() As Partner
        Protected Shared _mcSwitch As New TraceSwitch("mcSwitch", "Switch for OrmManagerBase", "3") 'info
        Protected Shared _LoadObjectsMI As Reflection.MethodInfo = Nothing
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

        Public Event BeginUpdate(ByVal o As OrmBase)
        Public Event BeginDelete(ByVal o As OrmBase)

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

            _list_converter = New FakeListConverter
            CreateInternal()
        End Sub

        <CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1805")> _
        Protected Sub New(ByVal schema As OrmSchemaBase)
            '_dispose_cash = True
            _cache = New OrmCache
            _schema = schema
            _list_converter = New FakeListConverter
            '_check_on_find = True
            CreateInternal()
        End Sub

        Protected Sub CreateInternal()
            _prev = CurrentManager
#If DEBUG Then
            If _prev IsNot Nothing Then
                If _prev._schema.Version <> _schema.Version Then
                    Throw New OrmManagerException("Cannot create nexted managers with different schema versions")
                End If
            End If
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

        Public Property ListConverter() As IListObjectConverter
            Get
                Return _list_converter
            End Get
            Set(ByVal value As IListObjectConverter)
                _list_converter = value
            End Set
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

        Public Function LoadObjects(Of T As {OrmBase, New})(ByVal fieldName As String, ByVal criteria As CriteriaLink, _
            ByVal col As ICollection, ByVal start As Integer, ByVal length As Integer) As ICollection(Of T)
            Dim tt As Type = GetType(T)

            If col Is Nothing Then
                Throw New ArgumentNullException("col")
            End If

            If String.IsNullOrEmpty(fieldName) Then
                Throw New ArgumentNullException(fieldName)
            End If

            If col.Count = 0 OrElse length = 0 OrElse start + length > col.Count Then
                Return New List(Of T)
            End If


#If DEBUG Then
            Dim ft As Type = _schema.GetFieldTypeByName(tt, fieldName)
            For Each o As OrmBase In col
                If o.GetType IsNot ft Then
                    Throw New ArgumentNullException(String.Format("Cannot load {0} with such collection. There is not relation", tt.Name))
                End If
                Exit For
            Next
#End If
            Dim l As New List(Of T)
            Dim newc As New List(Of OrmBase)

            Using SyncHelper.AcquireDynamicLock("9h13bhpqergfbjadflbq34f89h134g")
                If Not _dont_cache_lists Then
                    Dim i As Integer = start
                    For Each o As OrmBase In col
                        'Dim con As New OrmCondition.OrmConditionConstructor
                        'con.AddFilter(New OrmFilter(tt, fieldName, o, FilterOperation.Equal))
                        'con.AddFilter(criteria.Filter)
                        Dim cl As CriteriaLink = Orm.Criteria.Field(tt, fieldName).Eq(o).And(criteria)
                        Dim f As IOrmFilter = cl.Filter
                        Dim key As String = _schema.GetEntityKey(tt) & f.GetStaticString & GetStaticKey()
                        Dim dic As IDictionary = GetDic(_cache, key)
                        Dim id As String = CObj(f).ToString
                        If dic.Contains(id) Then
                            'Dim fs As List(Of String) = Nothing
                            Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(f, Nothing, key, id)
                            Dim v As ICacheValidator = TryCast(del, ICacheValidator)
                            If v Is Nothing OrElse v.Validate() Then
                                l.AddRange(Find(Of T)(cl, Nothing, True))
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
                For Each o As OrmBase In newc
                    ids.Append(o.Identifier)
                Next
                Dim c As New List(Of T)
                GetObjects(Of T)(ids.Ints, GetFilter(criteria), c, True, fieldName, False)
                'l.AddRange(c)

                Dim lookups As New Dictionary(Of OrmBase, List(Of T))
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

                For Each obj As OrmBase In col
                    Dim k As OrmBase = obj
                    Dim v As List(Of T) = Nothing
                    If lookups.TryGetValue(k, v) Then
                        l.AddRange(v)
                        If Not _dont_cache_lists Then
                            'Dim con As New OrmCondition.OrmConditionConstructor
                            'con.AddFilter(New OrmFilter(tt, fieldName, k, FilterOperation.Equal))
                            'con.AddFilter(filter)
                            'Dim f As IOrmFilter = con.Condition
                            Dim cl As CriteriaLink = Orm.Criteria.Field(tt, fieldName).Eq(k).And(criteria)
                            Dim f As IOrmFilter = cl.Filter
                            Dim key As String = _schema.GetEntityKey(tt) & f.GetStaticString & GetStaticKey()
                            Dim dic As IDictionary = GetDic(_cache, key)
                            Dim id As String = CObj(f).ToString
                            dic(id) = New CachedItem(Nothing, f, v, Me)
                        End If
                    End If
                Next

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
                            key &= criteria.Filter.GetStaticString
                        End If

                        Dim dic As IDictionary = GetDic(_cache, key)

                        Dim id As String = o.Identifier.ToString
                        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                            id &= CObj(criteria.Filter).ToString
                        End If

                        If dic.Contains(id) Then
                            'Dim sync As String = GetSync(key, id)
                            Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(o, GetFilter(criteria), Nothing, id, key, direct)
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
                Dim edic As IDictionary(Of Integer, EditableList) = GetObjects(Of T)(type, ids.Ints, GetFilter(criteria), relation, False, True)
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
                                    key &= criteria.Filter.GetStaticString
                                End If

                                Dim dic As IDictionary = GetDic(_cache, key)

                                Dim id As String = o.Identifier.ToString
                                If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                                    id &= CObj(criteria.Filter).ToString
                                End If

                                'Dim sync As String = GetSync(key, id)
                                el.Accept(Nothing)
                                dic(id) = New M2MCache(Nothing, GetFilter(criteria), el, Me)
                            End If

                            'Cache.AddRelationValue(o.GetType, type2load)
                        End If
                    Next
                End If
            End Using
        End Sub

        Protected Function Find(ByVal id As Integer, ByVal t As Type) As OrmBase
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

        Public Function CreateDBObject(ByVal id As Integer, ByVal type As Type) As OrmBase
#If DEBUG Then
            If Not GetType(OrmBase).IsAssignableFrom(type) Then
                Throw New ArgumentException(String.Format("The type {0} must be derived from Ormbase", type))
            End If
#End If

            Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Public Or Reflection.BindingFlags.Instance
            Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("CreateDBObject", flags, Nothing, Reflection.CallingConventions.Any, New Type() {GetType(Integer)}, Nothing)
            Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {type})
            Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {id}, Nothing), OrmBase)
        End Function

        Public Function CreateDBObject(Of T As {OrmBase, New})(ByVal id As Integer) As T
            Dim o As T = CreateObject(Of T)(id)
            Assert(o IsNot Nothing, "Object must be created: " & id & ". Type - " & GetType(T).ToString)
            Using o.GetSyncRoot()
                If o.ObjectState = ObjectState.Created AndAlso Not IsNewObject(GetType(T), id) Then
                    o.ObjectState = ObjectState.NotLoaded
                    AddObject(o)
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

        Public Function FindDistinct(Of T As {OrmBase, New})(ByVal joins() As OrmJoin, ByVal criteria As CriteriaLink, _
            ByVal sort As Sort, ByVal withLoad As Boolean) As ICollection(Of T)

            Dim key As String = "distinct" & _schema.GetEntityKey(GetType(T))

            If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                key &= criteria.Filter.GetStaticString
            End If

            If joins IsNot Nothing Then
                For Each join As OrmJoin In joins
                    If Not join.IsEmpty Then
                        key &= join.GetStaticString
                    End If
                Next
            End If

            key &= GetStaticKey()

            Dim dic As IDictionary = GetDic(_cache, key)

            Dim id As String = GetType(T).ToString
            If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                id &= CObj(criteria.Filter).ToString
            End If

            If joins IsNot Nothing Then
                For Each join As OrmJoin In joins
                    If Not join.IsEmpty Then
                        id &= join.ToString
                    End If
                Next
            End If
            Dim sync As String = id & GetStaticKey()

            'CreateDepends(filter, key, id)

            Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(joins, GetFilter(criteria), sort, key, id)
            Dim ce As CachedItem = FindAdvanced(Of T)(dic, sync, id, withLoad, del)
            Return ce.GetObjectList(Of T)(Me, withLoad, del.Created)
        End Function

        Public Function FindDistinct(Of T As {OrmBase, New})(ByVal relation As M2MRelation, _
            ByVal criteria As CriteriaLink, _
            ByVal sort As Sort, ByVal withLoad As Boolean) As ICollection(Of T)

            Dim key As String = "distinct" & _schema.GetEntityKey(GetType(T))

            If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                key &= criteria.Filter.GetStaticString
            End If

            If relation IsNot Nothing Then
                key &= relation.Table.TableName & relation.Column
            End If

            key &= GetStaticKey()

            Dim dic As IDictionary = GetDic(_cache, key)

            Dim id As String = GetType(T).ToString
            If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                id &= CObj(criteria.Filter).ToString
            End If

            If relation IsNot Nothing Then
                id &= relation.Table.TableName & relation.Column
            End If

            Dim sync As String = id & GetStaticKey()

            'CreateDepends(filter, key, id)

            Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(relation, GetFilter(criteria), sort, key, id)
            Dim ce As CachedItem = FindAdvanced(Of T)(dic, sync, id, withLoad, del)
            Return ce.GetObjectList(Of T)(Me, withLoad, del.Created)
        End Function

        Public Function Find(Of T As {OrmBase, New})(ByVal criteria As CriteriaLink, _
            ByVal sort As Sort, ByVal withLoad As Boolean) As ICollection(Of T)

            If criteria Is Nothing Then
                Throw New ArgumentNullException("filter")
            End If

            Dim filter As IOrmFilter = criteria.Filter

            Dim key As String = _schema.GetEntityKey(GetType(T)) & filter.GetStaticString & GetStaticKey()

            Dim dic As IDictionary = GetDic(_cache, key)

            Dim id As String = CObj(filter).ToString
            Dim sync As String = id & GetStaticKey()

            'CreateDepends(filter, key, id)

            Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(filter, sort, key, id)
            Dim ce As CachedItem = FindAdvanced(Of T)(dic, sync, id, withLoad, del)
            Return ce.GetObjectList(Of T)(Me, withLoad, del.Created)
        End Function

        Public Function Find(Of T As {OrmBase, New})(ByVal criteria As CriteriaLink, _
            ByVal sort As Sort, ByVal cols() As String) As ICollection(Of T)

            If criteria Is Nothing Then
                Throw New ArgumentNullException("criteria")
            End If

            Dim key As String = _schema.GetEntityKey(GetType(T)) & criteria.Filter.GetStaticString & GetStaticKey()

            Dim dic As IDictionary = GetDic(_cache, key)

            Dim id As String = CObj(criteria.Filter).ToString
            Dim sync As String = id & GetStaticKey()

            'CreateDepends(filter, key, id)

            Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(criteria.Filter, sort, key, id, cols)
            Return FindAdvanced(Of T)(dic, sync, id, True, del).GetObjectList(Of T)(Me, True, del.Created)
        End Function

        Public Function FindTop(Of T As {OrmBase, New})(ByVal top As Integer, ByVal criteria As CriteriaLink, _
            ByVal sort As Sort, ByVal withLoad As Boolean) As ICollection(Of T)

            Dim key As String = String.Empty

            If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                key = _schema.GetEntityKey(GetType(T)) & criteria.Filter.GetStaticString & GetStaticKey() & "Top"
            Else
                key = _schema.GetEntityKey(GetType(T)) & GetStaticKey() & "Top"
            End If

            If sort IsNot Nothing Then
                key &= sort.ToString
            End If

            Dim dic As IDictionary = GetDic(_cache, key)

            Dim f As String = String.Empty
            If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                f = CObj(criteria.Filter).ToString
            End If

            Dim id As String = f & " - top - " & top
            Dim sync As String = id & GetStaticKey()

            'CreateDepends(filter, key, id)

            Dim del As ICustDelegate(Of T) = GetCustDelegate4Top(Of T)(top, GetFilter(criteria), sort, key, id)
            Return FindAdvanced(Of T)(dic, sync, id, withLoad, del).GetObjectList(Of T)(Me, withLoad, del.Created)
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

        Protected Friend Function GetM2MKey(ByVal tt1 As Type, ByVal tt2 As Type, ByVal direct As Boolean) As String
            Return tt1.Name & Const_JoinStaticString & direct & " - new version - " & tt2.Name & GetStaticKey()
        End Function

        Friend Shared Function GetSync(ByVal key As String, ByVal id As String) As String
            Return id & Const_KeyStaticString & key
        End Function

        Protected Friend Function FindMany2Many2(Of T As {OrmBase, New})(ByVal obj As OrmBase, ByVal criteria As CriteriaLink, _
            ByVal sort As Sort, ByVal direct As Boolean, ByVal withLoad As Boolean) As ICollection(Of T)
            Dim p As Pair(Of M2MCache, Boolean) = FindM2M(Of T)(obj, direct, criteria, sort, withLoad)
            Return p.First.GetObjectList(Of T)(Me, withLoad, p.Second)
        End Function

        Protected Function FindM2M(Of T As {OrmBase, New})(ByVal obj As OrmBase, ByVal direct As Boolean, ByVal criteria As CriteriaLink, _
            ByVal sort As Sort, ByVal withLoad As Boolean) As Pair(Of M2MCache, Boolean)
            Dim tt1 As Type = obj.GetType
            Dim tt2 As Type = GetType(T)

            Dim key As String = GetM2MKey(tt1, tt2, direct)
            If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                key &= criteria.Filter.GetStaticString
            End If

            Dim dic As IDictionary = GetDic(_cache, key)

            Dim id As String = obj.Identifier.ToString
            If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                id &= CObj(criteria.Filter).ToString
            End If

            Dim sync As String = GetSync(key, id)

            'CreateM2MDepends(filter, key, id)

            Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(obj, GetFilter(criteria), sort, id, key, direct)
            Dim m As M2MCache = CType(FindAdvanced(Of T)(dic, sync, id, withLoad, del), M2MCache)
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
            Dim p As Pair(Of M2MCache, Pair(Of String)) = CType(mi_real.Invoke(Me, flags, Nothing, o, Nothing), Global.CoreFramework.Structures.Pair(Of Global.Worm.Orm.OrmManagerBase.M2MCache, Global.CoreFramework.Structures.Pair(Of String)))
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
            Dim criteria As New CriteriaLink

            Dim del As ICustDelegate(Of T) = GetCustDelegate(Of T)(obj, GetFilter(criteria), Nothing, id, key, direct)
            Dim m As M2MCache = CType(FindAdvanced(Of T)(dic, sync, id, False, del), M2MCache)
            Dim p As New Pair(Of M2MCache, Pair(Of String))(m, New Pair(Of String)(key, id))
            Return p
        End Function
#End Region

#Region " Cache "

        Protected Function FindAdvanced(Of T As {OrmBase, New})(ByVal dic As IDictionary, ByVal sync As String, ByVal id As Object, _
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
                Return ce
            End If

            If del.Created Then
                del.CreateDepends()
            Else
                Dim psort As Sort = del.Sort

                If ce.SortEquals(psort) OrElse psort Is Nothing Then
                    If v IsNot Nothing AndAlso Not v.Validate(ce) Then
                        del.Renew = True
                        GoTo l1
                    End If
                Else
                    If Not del.SmartSort Then
                        del.Renew = True
                        GoTo l1
                    Else
                        'Dim loaded As Integer = 0
                        Dim objs As ICollection(Of T) = ce.GetObjectList(Of T)(Me, withLoad, False)
                        If objs IsNot Nothing AndAlso objs.Count > 0 Then
                            Dim srt As IOrmSorting = Nothing
                            If psort.IsExternal Then
                                ce = del.GetCacheItem(_schema.ExternalSort(Of T)(psort, objs))
                                dic(id) = ce
                            ElseIf CanSortOnClient(GetType(T), CType(objs, System.Collections.ICollection), srt) Then
                                Using SyncHelper.AcquireDynamicLock(sync)
                                    Dim sc As IComparer(Of T) = srt.CreateSortComparer(Of T)(psort)
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

            Return ce
        End Function

        Public Function CanSortOnClient(ByVal t As Type, ByVal col As ICollection, ByRef sorting As IOrmSorting) As Boolean
            Dim schema As IOrmObjectSchemaBase = _schema.GetObjectSchema(t)
            sorting = TryCast(schema, IOrmSorting)
            If sorting Is Nothing Then
                Return False
            End If
            Dim loaded As Integer = 0
            For Each o As OrmBase In col
                If o.IsLoaded Then loaded += 1
            Next
            Return col.Count - loaded < 10
        End Function

        Protected Sub InvalidateCache(ByVal obj As OrmBase, ByVal upd As IList(Of OrmFilter))
            Dim t As Type = obj.GetType
            Dim l As New List(Of String)
            For Each f As OrmFilter In upd
                '    Assert(f.Type Is t, "")

                '    Cache.AddUpdatedFields(obj, f.FieldName)
                l.Add(f.FieldName)
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

        Protected Friend Function LoadType(Of T As {OrmBase, New})(ByVal id As Integer, _
            ByVal load As Boolean, ByVal checkOnCreate As Boolean) As T

            Dim name As String = GetType(T).Name
            Dim dic As Generic.IDictionary(Of Integer, T) = GetDictionary(Of T)()
            Dim type As Type = GetType(T)

#If DEBUG Then
            If dic Is Nothing Then
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
                If created Then
                    AddObject(a)
                End If
                If Not created AndAlso load AndAlso Not a.IsLoaded Then
                    a.Load()
                End If
            End If

            Return a
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
            Dim name As String = obj.GetType.Name
            Dim dic As IDictionary = GetDictionary(obj.GetType)

            If dic Is Nothing Then
                ''todo: throw an exception when all collections will be implemented
                'Return
                Throw New OrmManagerException("Collection for " & name & " not exists")
            End If
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
                WriteLine("Attempt to add existing object " & obj.GetType.Name & " (" & obj.Identifier & ") to cashe")
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
            For Each o As Pair(Of OrmManagerBase.M2MCache, Pair(Of String, String)) In Cache.GetM2MEtries(obj, Nothing)
                Dim mdic As IDictionary = GetDic(Cache, o.Second.First)
                mdic.Remove(o.Second.Second)
            Next
        End Sub

#End Region

#Region " helpers "

        Public Function GetDictionary(ByVal t As Type) As IDictionary
            Return _cache.GetOrmDictionary(t, _schema)
        End Function

        Public Function GetDictionary(Of T)() As Generic.IDictionary(Of Integer, T)
            Return _cache.GetOrmDictionary(Of T)(_schema)
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
        'Public Function Obj2ObjRelationHasChanges(ByVal obj As OrmBase, ByVal t As Type) As Boolean
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

        '    'Dim dic As IDictionary = GetDic(_cache, key)

        '    Dim id As String = obj.Identifier.ToString

        '    Dim sync As String = id & Const_KeyStaticString & key & GetTablePostfix

        '    Dim dt As System.Data.DataTable = GetDataTable(id, key & GetTablePostfix, sync, tt2, obj, Nothing, False, True, False)

        '    Return dt.GetChanges IsNot Nothing
        'End Function

        'Public Sub Obj2ObjRelationCancel(ByVal obj As OrmBase, ByVal t As Type)
        '    Invariant()

        '    If obj Is Nothing Then
        '        Throw New ArgumentNullException("obj parameter cannot be nothing")
        '    End If

        '    If t Is Nothing Then
        '        Throw New ArgumentNullException("t parameter cannot be nothing")
        '    End If

        '    Dim tt1 As Type = obj.GetType
        '    Dim tt2 As Type = t

        '    If _schema.IsMany2ManyReadonly(tt1, tt2) Then
        '        Throw New InvalidOperationException("Relation is readonly")
        '    End If

        '    Dim key As String = tt1.Name & Const_JoinStaticString & tt2.Name & GetStaticKey()

        '    'Dim dic As IDictionary = GetDic(_cache, key)

        '    Dim id As String = obj.Identifier.ToString

        '    Dim sync As String = id & Const_KeyStaticString & key & GetTablePostfix

        '    Dim dt As System.Data.DataTable = GetDataTable(id, key & GetTablePostfix, sync, tt2, obj, Nothing, False, True, False)

        '    Using SyncHelper.AcquireDynamicLock(sync)
        '        dt.RejectChanges()
        '    End Using
        'End Sub

        'Protected Function Obj2ObjRelationSave2(ByVal obj As OrmBase, ByVal t As Type) As OrmBase.AcceptState
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

        '    'Dim dic As IDictionary = GetDic(_cache, key)

        '    Dim id As String = obj.Identifier.ToString

        '    Dim sync As String = id & Const_KeyStaticString & key & GetTablePostfix

        '    Dim dt As System.Data.DataTable = GetDataTable(id, key & GetTablePostfix, sync, tt2, obj, Nothing, False, True, False)
        '    Obj2ObjRelationSave2(obj, dt, sync, t)
        '    Return New OrmBase.AcceptState(dt, id, key, t)
        '    'ResetAllM2MRelations(id, key)
        'End Function

        'Protected Friend Sub Obj2ObjRelationRemove(ByVal obj As OrmBase, ByVal t As Type)
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

        '    Dim dic As IDictionary = GetDic(_cache, key)
        '    Dim dic2 As IDictionary = GetDic(_cache, key & GetTablePostfix)

        '    Dim id As String = obj.Identifier.ToString

        '    dic.Remove(id)
        '    dic2.Remove(id)

        '    ResetAllM2MRelations(id, key)
        'End Sub

        'Public Sub Obj2ObjRelationDelete(ByVal mainobj As OrmBase, ByVal subobj As OrmBase)

        '    Invariant()

        '    If mainobj Is Nothing Then
        '        Throw New ArgumentNullException("mainobj parameter cannot be nothing")
        '    End If

        '    If subobj Is Nothing Then
        '        Throw New ArgumentNullException("subobj parameter cannot be nothing")
        '    End If

        '    Dim tt1 As Type = mainobj.GetType
        '    Dim tt2 As Type = subobj.GetType

        '    If _schema.IsMany2ManyReadonly(tt1, tt2) Then
        '        Throw New InvalidOperationException("Relation is readonly")
        '    End If

        '    Dim key As String = tt1.Name & Const_JoinStaticString & tt2.Name & GetStaticKey()

        '    'Dim dic As IDictionary = GetDic(_cache, key)

        '    Dim id As String = mainobj.Identifier.ToString

        '    Dim sync As String = id & Const_KeyStaticString & key & GetTablePostfix

        '    Dim dt As System.Data.DataTable = GetDataTable(id, key & GetTablePostfix, sync, tt2, mainobj, Nothing, False, True, False)
        '    Dim mid As String = tt1.Name & "ID"
        '    Dim sid As String = tt2.Name & "ID"
        '    If tt1 Is tt2 Then
        '        sid = mid & "Rev"
        '    End If
        '    Using SyncHelper.AcquireDynamicLock(sync)
        '        Dim rs() As System.Data.DataRow = dt.Select(mid & " = " & mainobj.Identifier & " and " & sid & " = " & subobj.Identifier)
        '        If rs.Length = 1 Then
        '            rs(0).Delete()

        '            'ResetAllM2MRelations(id, key)
        '        ElseIf rs.Length = 0 Then
        '            If _mcSwitch.TraceVerbose Then
        '                WriteLine("There is no relation " & mid & " = " & mainobj.Identifier & " and " & sid & " = " & subobj.Identifier)
        '            End If
        '        ElseIf rs.Length > 1 Then
        '            Throw New OrmManagerException(mid & " = " & mainobj.Identifier & " and " & sid & " = " & subobj.Identifier & " is not uniquie")
        '        End If
        '    End Using
        'End Sub

        Protected Friend Sub M2MCancel(ByVal mainobj As OrmBase, ByVal t As Type)
            For Each o As Pair(Of OrmManagerBase.M2MCache, Pair(Of String, String)) In Cache.GetM2MEtries(mainobj, Nothing)
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

            For Each o As Pair(Of OrmManagerBase.M2MCache, Pair(Of String, String)) In Cache.GetM2MEtries(mainobj, Nothing)
                Dim m2me As M2MCache = o.First
                If m2me.Filter Is Nothing AndAlso m2me.Entry.HasChanges AndAlso m2me.Entry.Direct = direct Then
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
            For Each o As Pair(Of M2MCache, Pair(Of String, String)) In Cache.GetM2MEtries(obj, obj.GetOldName(oldId))
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

            Cache.UpdateM2MEtries(obj, oldId, obj.GetOldName(oldId))
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
            For Each o As Pair(Of M2MCache, Pair(Of String, String)) In Cache.GetM2MEtries(obj, Nothing)
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
            Dim types As Type() = New Type() {GetType(OrmBase), GetType(Boolean), GetType(CriteriaLink), GetType(Sort), GetType(Boolean)}
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

            Dim key As String = GetM2MKey(tt1, tt2, direct)

            Dim dic As IDictionary = GetDic(_cache, key)

            Dim id As String = obj.Identifier.ToString

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

            m.Entry.Add(subobj.Identifier)

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

        Public Function Search(Of T As {OrmBase, New})(ByVal [string] As String) As ICollection(Of T)
            Invariant()

            If [string] IsNot Nothing AndAlso [string].Length > 0 Then
                Dim ss() As String = Split4FullTextSearch([string], GetSearchSection)
                Return Search(Of T)(ss, Nothing, Nothing)
            End If
            Return New List(Of T)()
        End Function

        Public Function Search(Of T As {OrmBase, New})(ByVal [string] As String, ByVal contextKey As Object) As ICollection(Of T)
            Invariant()

            If [string] IsNot Nothing AndAlso [string].Length > 0 Then
                Dim ss() As String = Split4FullTextSearch([string], GetSearchSection)
                Return Search(Of T)(ss, Nothing, contextKey)
            End If
            Return New List(Of T)()
        End Function

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
                            For Each re As Orm.Configuration.SearchReplaceElement In sc.ReplaceSection
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
                        For Each re As Orm.Configuration.SearchReplaceElement In sc.ReplaceSection
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
                        For Each re As Orm.Configuration.SearchReplaceElement In sc.ReplaceSection
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
            'Try
            Return arr
            'Catch ex As InvalidCastException
            'Throw New OrmManagerException("Error converting type " & type.ToString, ex)
            'End Try
        End Function

        Public Function LoadObjectsIds(Of T As {OrmBase, New})(ByVal ids As ICollection(Of Integer)) As ICollection(Of T)
            Return LoadObjects(Of T)(ConvertIds2Objects(Of T)(ids, False))
        End Function

        Protected Friend Shared Function GetDic(ByVal cache As OrmCacheBase, ByVal key As String) As IDictionary
            Dim dic As IDictionary = CType(cache.Filters(key), IDictionary)
            If dic Is Nothing Then
                Using SyncHelper.AcquireDynamicLock(key)
                    dic = CType(cache.Filters(key), IDictionary)
                    If dic Is Nothing Then
                        dic = cache.GetFiltersDic() 'Hashtable.Synchronized(New Hashtable)
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

        Private Shared Function GetFilter(ByVal criteria As CriteriaLink) As IOrmFilter
            If criteria IsNot Nothing Then
                Return criteria.Filter
            End If
            Return Nothing
        End Function

#Region " Abstract members "

        Protected MustOverride Function GetSearchSection() As String

        Protected MustOverride Function Search(Of T As {OrmBase, New})(ByVal tokens() As String, ByVal join As OrmJoin, ByVal contextKey As Object) As ICollection(Of T)

        Protected Friend MustOverride Function GetStaticKey() As String

        Protected MustOverride Function GetCustDelegate(Of T As {OrmBase, New})(ByVal filter As IOrmFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String) As ICustDelegate(Of T)

        Protected MustOverride Function GetCustDelegate(Of T As {OrmBase, New})(ByVal filter As IOrmFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String, ByVal cols() As String) As ICustDelegate(Of T)

        Protected MustOverride Function GetCustDelegate(Of T As {OrmBase, New})(ByVal join() As OrmJoin, ByVal filter As IOrmFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String) As ICustDelegate(Of T)

        Protected MustOverride Function GetCustDelegate(Of T As {OrmBase, New})(ByVal relation As M2MRelation, ByVal filter As IOrmFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String) As ICustDelegate(Of T)

        Protected MustOverride Function GetCustDelegate4Top(Of T As {OrmBase, New})(ByVal top As Integer, ByVal filter As IOrmFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String) As ICustDelegate(Of T)

        Protected MustOverride Function GetCustDelegate(Of T2 As {OrmBase, New})( _
            ByVal obj As OrmBase, ByVal filter As IOrmFilter, _
            ByVal sort As Sort, ByVal id As String, ByVal key As String, ByVal direct As Boolean) As ICustDelegate(Of T2)

        'Protected MustOverride Function GetCustDelegateTag(Of T As {OrmBase, New})( _
        '    ByVal obj As T, ByVal filter As IOrmFilter, ByVal sort As String, ByVal sortType As SortType, ByVal id As String, ByVal sync As String, ByVal key As String) As ICustDelegate(Of T)

        'Protected MustOverride Function GetDataTableInternal(ByVal t As Type, ByVal obj As OrmBase, ByVal filter As IOrmFilter, ByVal appendJoins As Boolean, Optional ByVal tag As String = Nothing) As System.Data.DataTable

        Protected MustOverride Function GetObjects(Of T As {OrmBase, New})(ByVal ids As Generic.IList(Of Integer), ByVal f As IOrmFilter, ByVal objs As IList(Of T), _
           ByVal withLoad As Boolean, ByVal fieldName As String, ByVal idsSorted As Boolean) As Generic.IList(Of T)

        Protected MustOverride Function GetObjects(Of T As {OrmBase, New})(ByVal type As Type, ByVal ids As Generic.IList(Of Integer), ByVal f As IOrmFilter, _
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
#End Region

        Protected MustOverride Function BuildDictionary(Of T As {New, OrmBase})(ByVal level As Integer, ByVal filter As IOrmFilter, ByVal join As OrmJoin) As DicIndex(Of T)

        Protected Friend Sub RegisterInCashe(ByVal obj As OrmBase)
            If Not IsInCache(obj) Then
                AddObject(obj)
                If obj.GetModifiedObject IsNot Nothing Then
                    Me.Cache.RegisterExistingModification(obj, obj.Identifier)
                End If
            End If
        End Sub

        Public Function BuildObjDictionary(Of T As {New, OrmBase})(ByVal level As Integer, ByVal criteria As CriteriaLink, ByVal join As OrmJoin) As DicIndex(Of T)

            Dim key As String = String.Empty

            If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                key = criteria.Filter.GetStaticString & _schema.GetEntityKey(GetType(T)) & GetStaticKey() & "Dics"
            Else
                key = _schema.GetEntityKey(GetType(T)) & GetStaticKey() & "Dics"
            End If

            Dim dic As IDictionary = GetDic(_cache, key)

            Dim f As String = String.Empty
            If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                f = CObj(criteria.Filter).ToString
            End If

            Dim id As String = f & " - dics - "
            Dim sync As String = id & GetStaticKey()

            Dim roots As DicIndex(Of T) = CType(dic(id), DicIndex(Of T))
            Invariant()

            If roots Is Nothing Then
                Using SyncHelper.AcquireDynamicLock(sync)
                    roots = CType(dic(id), DicIndex(Of T))
                    If roots Is Nothing Then
                        roots = BuildDictionary(Of T)(level, GetFilter(criteria), join)
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
    End Class

End Namespace
