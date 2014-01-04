Imports Worm.Criteria
Imports Worm.Criteria.Joins
Imports Worm.Cache
Imports Worm.Entities.Meta
Imports Worm.Criteria.Values
Imports Worm.Criteria.Core
Imports Worm.Query.Sorting
Imports Worm.Entities
Imports System.Collections.Generic
Imports Worm.Criteria.Conditions
Imports Worm.Misc
Imports Worm.Query
Imports Worm.Expressions2
Imports cc = Worm.Criteria.Core
Imports System.Linq

#Const DontUseStringIntern = True
#Const TraceM2M = False
#Const TraceManagerCreation = False

'Namespace Managers

Partial Public MustInherit Class OrmManager
    Implements IDisposable

    Private Shared _mpe As ObjectMappingEngine

    Protected Friend Const Const_KeyStaticString As String = " - key -"
    Protected Friend Const Const_JoinStaticString As String = " - join - "
    Protected Friend Const GetTablePostfix As String = " - GetTable"
    Protected Const WithLoadConst As Boolean = False
    Protected Const myConstLocalStorageString As String = "afoivnaodfvodfviogb3159fhbdofvad"
    'Public Const CustomSort As String = "q890h5f130nmv90h1nv9b1v-9134fb"

    Protected _cache As CacheBase
    'Private _dispose_cash As Boolean
    Friend _prev As OrmManager = Nothing
    'Protected hide_deleted As Boolean = True
    'Protected _check_status As Boolean = True
    Friend _schema As ObjectMappingEngine
    'Private _findnew As FindNew
    'Private _remnew As RemoveNew
    Protected Friend _disposed As Boolean = False
    'Protected _sites() As Partner
    Protected Friend Shared _mcSwitch As New TraceSwitch("mcSwitch", "Switch for OrmManager", "3") 'info
    Protected Shared _LoadObjectsMI As Reflection.MethodInfo = Nothing
    Private Shared _realCreateDbObjectDic As Hashtable = Hashtable.Synchronized(New Hashtable())
    Private Shared _realLoadTypeDic As Hashtable = Hashtable.Synchronized(New Hashtable())
    Private Shared _tsExec As New TraceSource("Worm.Diagnostics.Execution", SourceLevels.Information)

    Protected _findload As Boolean = False
    '#If DEBUG Then
    '        Protected _next As OrmManager
    '#End If
    'Public Delegate Function FindNew(ByVal type As Type, ByVal id As Integer) As OrmBase
    'Public Delegate Sub RemoveNew(ByVal type As Type, ByVal id As Integer)

    'Protected _cs As String
    'Protected _prevs As String

    <ThreadStatic()> _
    Private Shared _cur As OrmManager
    'Public Delegate Function GetLocalStorageDelegate(ByVal str As String) As Object
    'Protected _get_cur As GetLocalStorageDelegate

    'Public Function RegisterGetLocalStorage(ByVal getLocalStorage As GetLocalStorageDelegate) As GetLocalStorageDelegate
    '    Dim i As GetLocalStorageDelegate = _get_cur
    '    _get_cur = getLocalStorage
    '    Return i
    'End Function
    'Private _list_converter As IListObjectConverter
    Protected Friend _dont_cache_lists As Boolean
    Friend _expiresPattern As Date
    Protected Friend _start As Integer
    Protected Friend _length As Integer = Integer.MaxValue
    Protected Friend _op As Boolean
    Protected Friend _rev As Boolean
    Protected _er As ExecutionResult
    Friend _externalFilter As IApplyFilter
    Protected Friend _loadedInLastFetch As Integer
    Friend _list As String
    Private _listeners As New List(Of TraceListener)
    Private _stmtHelper As StmtGenerator
    Private _crMan As ICreateManager

#If TraceManagerCreation Then
    Private _callstack As String
#End If

    Protected Friend Function GetRev() As Boolean
        Return _rev
    End Function

    Protected Friend Function GetStart() As Integer
        If _externalFilter IsNot Nothing Then
            Return 0
        End If
        Return _start
    End Function

    Protected Friend Function GetLength() As Integer
        If _externalFilter IsNot Nothing Then
            Return Integer.MaxValue
        End If
        Return _length
    End Function

    Protected Friend ReadOnly Property IsPagingOptimized() As Boolean
        Get
            Return _op
        End Get
    End Property

    Public Property StmtGenerator() As StmtGenerator
        Get
            Return _stmtHelper
        End Get
        Set(ByVal value As StmtGenerator)
            _stmtHelper = value
        End Set
    End Property

    Public ReadOnly Property LastExecutionResult() As ExecutionResult
        Get
            Return _er
        End Get
    End Property

    Public Event ObjectCreated(ByVal sender As OrmManager, ByVal o As IEntity)
    Public Event ObjectLoaded(ByVal sender As OrmManager, ByVal o As IEntity)
    Public Event ObjectRestoredFromCache(ByVal sender As OrmManager, ByVal created As Boolean, ByVal o As ICachedEntity)
    Public Event BeginUpdate(ByVal sender As OrmManager, ByVal o As ICachedEntity)
    Public Event BeginDelete(ByVal sender As OrmManager, ByVal o As ICachedEntity)

    Public Event DataAvailable(ByVal mgr As OrmManager, ByVal r As ExecutionResult)

    Public Event ManagerGoingDown(ByVal mgr As OrmManager)

    Public Shared ReadOnly Property ExecSource() As TraceSource
        Get
            Return _tsExec
        End Get
    End Property

    <CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1805")> _
    Protected Sub New(ByVal cache As CacheBase, ByVal schema As ObjectMappingEngine)

        If cache Is Nothing Then
            Throw New ArgumentNullException("cache")
        End If

        If schema Is Nothing Then
            Throw New ArgumentNullException("schema")
        End If

        _cache = cache
        _schema = schema
        '_check_on_find = True

        '_list_converter = CreateListConverter()
        CreateInternal()
    End Sub

    <CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1805")> _
    Protected Sub New(ByVal schema As ObjectMappingEngine)
        '_dispose_cash = True
        _cache = New OrmCache
        _schema = schema
        '_check_on_find = True
        CreateInternal()
    End Sub

    Protected Friend Overridable ReadOnly Property IdentityString() As String
        Get
            Return Me.GetType.ToString
        End Get
    End Property

    Protected Function IsIdentical(ByVal mgr As OrmManager) As Boolean
        Return IdentityString = mgr.IdentityString
    End Function

    Protected Sub CreateInternal()
        _prev = CurrentManager
        Dim p As OrmManager = _prev
#If DEBUG Then
        Do While p IsNot Nothing
            If p._schema.Version <> _schema.Version AndAlso IsIdentical(p) Then
                Throw New OrmManagerException("Cannot create nested managers with different schema versions")
            End If
            p = p._prev
        Loop
#End If
#If TraceManagerCreation Then
        _callstack = Environment.StackTrace
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

    Protected Friend Overridable Sub SetSchema(ByVal schema As ObjectMappingEngine)
        _schema = schema
    End Sub

    Public ReadOnly Property MappingEngine() As ObjectMappingEngine
        Get
            Return _schema
        End Get
        'Protected Friend Set(ByVal value As ObjectMappingEngine)
        '    _schema = value
        'End Set
    End Property

    'Public Property RaiseObjectCreation() As Boolean
    '    Get
    '        Return _raiseCreated
    '    End Get
    '    Set(ByVal value As Boolean)
    '        _raiseCreated = value
    '    End Set
    'End Property

    Public Sub ResetLocalStorage()
#If TraceManagerCreation Then
        If _prev IsNot Nothing AndAlso Not String.IsNullOrEmpty(_callstack) Then
            Assert(Not _prev._disposed, "Previous MediaContent cannot be disposed. CallStack: " & _callstack)
            'Assert(Not prev.disposed, "Previous MediaContent cannot be disposed. CallStack: " & _s & "PrevCallStack: " & _prevs)
            'Assert(Not prev.disposed, "Previous MediaContent cannot be disposed.")
        End If
#End If
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
            Return Cache.ListConverter
        End Get
        'Set(ByVal value As IListObjectConverter)
        '    _list_converter = value
        'End Set
    End Property

    Public Shared Property CurrentManager() As OrmManager
        Get
            'If System.Web.HttpContext.Current IsNot Nothing Then
            '    Return CType(System.Web.HttpContext.Current.Items(myConstLocalStorageString), OrmManager)
            'Else
            '    Return _cur
            'End If
            'Return CType(Thread.GetData(LocalStorage), OrmManager)
            Return _cur
        End Get
        Protected Set(ByVal value As OrmManager)
            'If System.Web.HttpContext.Current IsNot Nothing Then
            '    System.Web.HttpContext.Current.Items(myConstLocalStorageString) = value
            'Else
            '    _cur = value
            'End If
            _cur = value
        End Set
    End Property

    Public Sub AddListener(ByVal l As TraceListener)
        _listeners.Add(l)
    End Sub

    Public Sub RemoveListener(ByVal l As TraceListener)
        _listeners.Remove(l)
    End Sub

    Protected Sub WriteLineInfo(ByVal str As String)
        For Each l As TraceListener In _listeners
            l.WriteLine(str)
            If Trace.AutoFlush Then l.Flush()
        Next
    End Sub

    Public Overridable ReadOnly Property CurrentUser() As Object
        Get
            Return Nothing
        End Get
    End Property

    Public ReadOnly Property Cache() As CacheBase
        Get
            Invariant()
            Return _cache
        End Get
    End Property

    Protected Sub RaiseObjectCreated(ByVal obj As IEntity)
        RaiseEvent ObjectCreated(Me, obj)
    End Sub

    Protected Sub RaiseObjectLoaded(ByVal obj As IEntity)
        RaiseEvent ObjectLoaded(Me, obj)
    End Sub

    Protected Friend Sub RaiseBeginUpdate(ByVal o As ICachedEntity)
        RaiseEvent BeginUpdate(Me, o)
    End Sub

    Protected Friend Sub RaiseBeginDelete(ByVal o As ICachedEntity)
        RaiseEvent BeginDelete(Me, o)
    End Sub

    Protected Friend Sub RaiseObjectRestored(ByVal created As Boolean, ByVal o As ICachedEntity)
        RaiseEvent ObjectRestoredFromCache(Me, created, o)
    End Sub

    Friend Function ContainsBeginDelete(ByVal h As BeginDeleteEventHandler) As Boolean
        If BeginDeleteEvent Is Nothing Then
            Return False
        Else
            Return Array.IndexOf(BeginDeleteEvent.GetInvocationList, h) >= 0
        End If
    End Function

    Friend Function ContainsBeginUpdate(ByVal h As BeginUpdateEventHandler) As Boolean
        If BeginUpdateEvent Is Nothing Then
            Return False
        Else
            Return Array.IndexOf(BeginUpdateEvent.GetInvocationList, h) >= 0
        End If
    End Function

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

    ' IDisposable
    Protected Overridable Overloads Sub Dispose(ByVal disposing As Boolean)
        SyncLock Me.GetType
            If Not Me._disposed Then
                ResetLocalStorage()
                RaiseEvent ManagerGoingDown(Me)
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

#If Not ExcludeFindMethods Then
    Public Function LoadObjects(Of T As {IKeyEntity, New})(ByVal propertyAlias As String, ByVal criteria As PredicateLink, _
        ByVal col As ICollection) As ReadOnlyList(Of T)
        Return LoadObjects(Of T)(propertyAlias, criteria, col, 0, col.Count)
    End Function

    ''' <summary>
    ''' Load child collections from parents
    ''' </summary>
    ''' <typeparam name="T">Type to load. This is child type.</typeparam>
    ''' <param name="propertyAlias">Field name of property in child type what references to parent type</param>
    ''' <param name="criteria">Additional criteria</param>
    ''' <param name="ecol">Collection of parent objects.</param>
    ''' <param name="start">Point in parent collection from where start to load</param>
    ''' <param name="length">Length of loaded window</param>
    ''' <returns>Collection of child objects arranged in order of parent in parent collection</returns>
    ''' <remarks></remarks>
    ''' 

    Public Function LoadObjects(Of T As {IKeyEntity, New})(ByVal propertyAlias As String, ByVal criteria As PredicateLink, _
        ByVal ecol As IEnumerable, ByVal start As Integer, ByVal length As Integer) As ReadOnlyList(Of T)
        Dim tt As Type = GetType(T)

        If ecol Is Nothing Then
            Throw New ArgumentNullException("col")
        End If

        If String.IsNullOrEmpty(propertyAlias) Then
            Throw New ArgumentNullException(propertyAlias)
        End If

        Dim cc As ICollection = TryCast(ecol, ICollection)
        If cc IsNot Nothing Then
            If cc.Count = 0 OrElse length = 0 OrElse start + length > cc.Count Then
                Return New ReadOnlyList(Of T)(New List(Of T))
            End If
            'Else
            '    col = New ArrayList(ecol)
        End If

        Dim oschema As IEntitySchema = _schema.GetEntitySchema(tt)
#If DEBUG Then
        Dim ft As Type = _schema.GetPropertyTypeByName(tt, oschema, propertyAlias)
        For Each o As IKeyEntity In ecol
            If Not ft.IsAssignableFrom(o.GetType) Then
                Throw New ArgumentNullException(String.Format("Cannot load {0} with such collection. There is not relation", tt.Name))
            End If
            Exit For
        Next
#End If
        Dim lookups As New Dictionary(Of IKeyEntity, ReadOnlyList(Of T))
        Dim newc As New List(Of IKeyEntity)
        Dim hasInCache As New Dictionary(Of IKeyEntity, Object)

        Using SyncHelper.AcquireDynamicLock("9h13bhpqergfbjadflbq34f89h134g")
            If Not _dont_cache_lists Then
                Dim i As Integer
                For Each o As IKeyEntity In ecol
                    If i < start Then
                        i += 1
                        Continue For
                    Else
                        i += 1
                    End If
                    'Dim con As New OrmCondition.OrmConditionConstructor
                    'con.AddFilter(New OrmFilter(tt, fieldName, o, FilterOperation.Equal))
                    'con.AddFilter(criteria.Filter)
                    Dim cl As PredicateLink = New PropertyPredicate(New EntityUnion(tt), propertyAlias).eq(o).[and](criteria)
                    Dim f As IFilter = cl.Filter
                    Dim key As String = FindGetKey(f, tt) '_schema.GetEntityKey(tt) & f.GetStaticString & GetStaticKey()
                    Dim dic As IDictionary = GetDic(_cache, key)
                    Dim id As String = f._ToString
                    Dim ce As UpdatableCachedItem = CType(dic(id), UpdatableCachedItem)
                    If ce IsNot Nothing Then
                        'Dim fs As List(Of String) = Nothing
                        Dim del As ICacheItemProvoder(Of T) = GetCustDelegate(Of T)(f, Nothing, key, id)
                        Dim v As ICacheValidator = TryCast(del, ICacheValidator)
                        If v Is Nothing OrElse v.ValidateBeforCacheProbe() OrElse v.ValidateItemFromCache(ce) Then
                            'l.AddRange(Find(Of T)(cl, Nothing, True))
                            lookups.Add(o, New ReadOnlyList(Of T)(Find(Of T)(cl, Nothing, True)))
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

            Dim ids As New List(Of Object)
            For Each o As IKeyEntity In newc
                ids.Add(o.Identifier)
            Next
            Dim c As New List(Of T)

            'If ids.Ints.Count > 0 Then
            GetObjects(Of T)(ids, GetFilter(criteria, tt), c, True, propertyAlias, False)

            For Each o As T In c
                'Dim v As OrmBase = CType(_schema.GetFieldValue(o, fieldName), OrmBase)
                Dim v As IKeyEntity = CType(MappingEngine.GetPropertyValue(o, propertyAlias, oschema), IKeyEntity)
                Dim ll As ReadOnlyList(Of T) = Nothing
                If Not lookups.TryGetValue(v, ll) Then
                    ll = New ReadOnlyList(Of T)
                    lookups.Add(v, ll)
                End If
                CType(ll, IListEdit).Add(o)
            Next

            Dim l As New ReadOnlyList(Of T)
            Dim j As Integer
            For Each obj As IKeyEntity In ecol
                If j < start Then
                    j += 1
                    Continue For
                Else
                    j += 1
                End If
                Dim v As ReadOnlyList(Of T) = Nothing
                If lookups.TryGetValue(obj, v) Then
                    For Each oo As IEntity In v
                        CType(l, IListEdit).Add(oo)
                    Next
                Else
                    v = New ReadOnlyList(Of T)
                End If
                If Not _dont_cache_lists AndAlso Not hasInCache.ContainsKey(obj) Then
                    'Dim con As New OrmCondition.OrmConditionConstructor
                    'con.AddFilter(New OrmFilter(tt, fieldName, k, FilterOperation.Equal))
                    'con.AddFilter(filter)
                    'Dim f As IOrmFilter = con.Condition
                    Dim cl As PredicateLink = New PropertyPredicate(New EntityUnion(tt), propertyAlias).eq(obj).[and](criteria)
                    Dim f As IFilter = cl.Filter
                    Dim key As String = FindGetKey(f, tt) '_schema.GetEntityKey(tt) & f.GetStaticString & GetStaticKey()
                    Dim dic As IDictionary = GetDic(_cache, key)
                    Dim id As String = f._ToString
                    Dim ce As New UpdatableCachedItem(v, Me)
                    dic(id) = ce
                    ce.Filter = f
                End If
                If j - start = length Then
                    Exit For
                End If
            Next
            'End If

            Return l
        End Using
    End Function
#End If

#If OLDM2M Then
    Public Sub LoadObjects(Of T As {IKeyEntity, New})(ByVal relation As M2MRelationDesc, ByVal criteria As PredicateLink, _
        ByVal col As ICollection, ByVal target As ICollection(Of T))
        LoadObjects(Of T)(relation, criteria, col, target, 0, col.Count)
    End Sub

    Public Sub LoadObjects(Of T As {IKeyEntity, New})(ByVal relation As M2MRelationDesc, ByVal criteria As PredicateLink, _
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

        If type2load IsNot relation.Entity.GetRealType(MappingEngine) Then
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
        Dim newc As New List(Of IKeyEntity)
        Dim direct As String = relation.Key
        Using SyncHelper.AcquireDynamicLock("13498nfb134g8l;adnfvioh")
            If Not _dont_cache_lists Then
                Dim i As Integer = start
                For Each o As _IKeyEntity In col
                    Dim tt1 As Type = o.GetType
                    Dim key As String = GetM2MKey(tt1, type2load, direct)
                    If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                        'key &= criteria.Filter(type2load).GetStaticString(_schema)
                        key &= criteria.Filter().GetStaticString(_schema, GetContextInfo)
                    End If

                    Dim dic As IDictionary = GetDic(_cache, key)

                    Dim id As String = o.Identifier.ToString
                    If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                        'id &= criteria.Filter(type2load)._ToString
                        id &= criteria.Filter()._ToString
                    End If

                    Dim ce As UpdatableCachedItem = CType(dic(id), UpdatableCachedItem)
                    If ce IsNot Nothing Then
                        'Dim sync As String = GetSync(key, id)
                        Dim del As ICacheItemProvoder(Of T) = GetCustDelegate(Of T)(o, GetFilter(criteria, type2load), Nothing, id, key, direct)
                        Dim v As ICacheValidator = TryCast(del, ICacheValidator)
                        If v Is Nothing OrElse v.ValidateBeforCacheProbe() OrElse v.ValidateItemFromCache(ce) Then
                            Dim e As M2MCache = CType(dic(id), M2MCache)
                            If Not v.ValidateItemFromCache(e) Then
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

            Dim ids As New List(Of Object)
            Dim type As Type = Nothing
            For Each o As IKeyEntity In newc
                ids.Add(o.Identifier)
                If type Is Nothing Then
                    type = o.GetType
                End If
            Next
            Dim edic As IDictionary(Of Object, CachedM2MRelation) = GetObjects(Of T)(type, ids, GetFilter(criteria, type2load), relation, False, True)
            'l.AddRange(c)

            If (target IsNot Nothing OrElse Not _dont_cache_lists) AndAlso edic IsNot Nothing Then
                For Each o As IKeyEntity In col
                    Dim el As CachedM2MRelation = Nothing
                    If edic.TryGetValue(o.Identifier, el) Then
                        For Each id As Object In el.Current
                            If target IsNot Nothing Then
                                target.Add(GetKeyEntityFromCacheOrCreate(Of T)(id))
                            End If
                        Next
                        'Cache.AddRelationValue(o.GetType, type2load)
                    Else
                        el = New CachedM2MRelation(o.Identifier, New List(Of Object), type, type2load, Nothing)
                    End If

                    If Not _dont_cache_lists Then
                        Dim tt1 As Type = o.GetType
                        Dim key As String = GetM2MKey(tt1, type2load, direct)
                        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                            'key &= criteria.Filter(type2load).GetStaticString(_schema)
                            key &= criteria.Filter().GetStaticString(_schema, GetContextInfo)
                        End If

                        Dim dic As IDictionary = GetDic(_cache, key)

                        Dim id As String = o.Identifier.ToString
                        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
                            'id &= criteria.Filter(type2load)._ToString
                            id &= criteria.Filter()._ToString
                        End If

                        'Dim sync As String = GetSync(key, id)
                        el.Accept(Me)
                        dic(id) = New M2MCache(Nothing, GetFilter(criteria, type2load), el, Me)
                    End If
                Next
            End If
        End Using
    End Sub
#End If

#If Not ExcludeFindMethods Then
    Protected Friend Function Find(ByVal id As Object, ByVal t As Type) As IKeyEntity
        Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public
        Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("Find", flags, Nothing, Reflection.CallingConventions.Any, New Type() {GetType(Object)}, Nothing)
        Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
        Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {id}, Nothing), IKeyEntity)
    End Function

    ''' <summary>
    ''' Поиск объекта по Id
    ''' </summary>
    Public Function Find(Of T As {IKeyEntity, New})(ByVal id As Object) As T
        Invariant()

        Return LoadType(Of T)(id, _findload, True)
    End Function
#End If


    'Public Function CreateObject(Of T As {IOrmBase, New})(ByVal id As Object) As T
    '    Invariant()

    '    Dim o As T = LoadType(Of T)(id, False, False)
    '    'o.ObjectState = ObjectState.Created
    '    Return o
    'End Function

    'Public Function CreateDBObject(Of T As {IOrmBase, New})(ByVal id As Object, _
    '    ByVal dic As IDictionary(Of Object, T), ByVal addOnCreate As Boolean) As IOrmBase
    '    Dim o As IOrmBase = LoadTypeInternal(Of T)(id, False, False, dic, addOnCreate)
    '    Dim type As Type = GetType(T)
    '    'Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance
    '    'Dim mi_real As Reflection.MethodInfo = CType(_realLoadTypeDic(type), Reflection.MethodInfo)
    '    'If (mi_real Is Nothing) Then
    '    '    SyncLock _realLoadTypeDic.SyncRoot
    '    '        mi_real = CType(_realLoadTypeDic(type), Reflection.MethodInfo)
    '    '        If (mi_real Is Nothing) Then
    '    '            Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("LoadTypeInternal", flags, Nothing, Reflection.CallingConventions.Any, _
    '    '                New Type() {GetType(Integer), GetType(Boolean), GetType(Boolean), GetType(IDictionary)}, Nothing)
    '    '            mi_real = mi.MakeGenericMethod(New Type() {type})
    '    '            _realLoadTypeDic(type) = mi_real
    '    '        End If
    '    '    End SyncLock
    '    'End If
    '    'o = CType(mi_real.Invoke(Me, flags, Nothing, New Object() {id, False, False, dic}, Nothing), OrmBase)

    '    'Assert(o IsNot Nothing, "Object must be created: " & id & ". Type - " & type.ToString)
    '    Using o.GetSyncRoot()
    '        If o.ObjectState = ObjectState.Created AndAlso Not IsNewObject(type, id) Then
    '            Debug.Assert(Not o.IsLoaded)
    '            Throw New ApplicationException
    '            'CType(o, Entity).SetObjectState(ObjectState.NotLoaded)
    '            'AddObject(o)
    '        End If
    '    End Using
    '    Return o
    'End Function

    Public Function GetEntityFromCacheOrDB(ByVal pk As IEnumerable(Of PKDesc), ByVal type As Type) As ICachedEntity
        Dim o As _ICachedEntity = CType(CreateObject(pk, type), _ICachedEntity)
        o.SetObjectState(ObjectState.NotLoaded)
        Return GetFromCacheAsIsOrLoadFromDB(o, GetDictionary(type))
    End Function

    Public Function GetEntityFromCacheOrCreate(ByVal pk As IEnumerable(Of PKDesc), ByVal type As Type) As ICachedEntity
        Dim o As _ICachedEntity = CType(CreateObject(pk, type), _ICachedEntity)
        o.SetObjectState(ObjectState.NotLoaded)
        Return GetOrAdd2Cache(o, GetDictionary(type))
    End Function

    Public Function GetEntityOrOrmFromCacheOrCreate(Of T As {New, _ICachedEntity})(ByVal pk As IEnumerable(Of PKDesc)) As T
        Dim o As T = CreateObject(Of T)(pk)
        o.SetObjectState(ObjectState.NotLoaded)
        Return CType(GetOrAdd2Cache(o, CType(GetDictionary(Of T)(), System.Collections.IDictionary)), T)
    End Function

    Public Function GetEntityFromCacheOrCreate(Of T As {New, _ICachedEntity})(ByVal pk As IEnumerable(Of PKDesc)) As T
        Dim o As T = CreateEntity(Of T)(pk)
        o.SetObjectState(ObjectState.NotLoaded)
        Return CType(GetOrAdd2Cache(o, CType(GetDictionary(Of T)(), System.Collections.IDictionary)), T)
    End Function

    Public Function GetEntityFromCacheLoadedOrDB(pk() As PKDesc, type As Type) As ICachedEntity
        Dim o As _ICachedEntity = CType(CreateObject(pk, type), _ICachedEntity)
        o.SetObjectState(ObjectState.NotLoaded)
        Return GetFromCacheLoadedOrLoadFromDB(o, GetDictionary(type))
    End Function

    Public Function GetKeyEntityFromCacheOrCreate(ByVal id As Object, ByVal type As Type) As ISinglePKEntity
        '#If DEBUG Then
        '        If Not GetType(IOrmBase).IsAssignableFrom(type) Then
        '            Throw New ArgumentException(String.Format("The type {0} must be derived from iOrmBase", type))
        '        End If
        '#End If

        '        Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Public Or Reflection.BindingFlags.Instance
        '        Dim mi_real As Reflection.MethodInfo = CType(_realCreateDbObjectDic(type), Reflection.MethodInfo)
        '        If (mi_real Is Nothing) Then
        '            SyncLock _realCreateDbObjectDic.SyncRoot
        '                mi_real = CType(_realCreateDbObjectDic(type), Reflection.MethodInfo)
        '                If (mi_real Is Nothing) Then
        '                    Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("GetOrCreateOrmBase", flags, Nothing, Reflection.CallingConventions.Any, New Type() {GetType(Object)}, Nothing)
        '                    mi_real = mi.MakeGenericMethod(New Type() {type})
        '                    _realCreateDbObjectDic(type) = mi_real
        '                End If
        '            End SyncLock
        '        End If
        '        Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {id}, Nothing), IOrmBase)
        Dim o As ISinglePKEntity = CreateKeyEntity(id, type)
        o.SetObjectState(ObjectState.NotLoaded)
        Return CType(GetOrAdd2Cache(o, GetDictionary(type)), ISinglePKEntity)
    End Function

    Public Function GetKeyEntityFromCacheOrCreate(ByVal id As Object, ByVal type As Type, ByVal add2CacheOnCreate As Boolean) As ISinglePKEntity
        Dim o As ISinglePKEntity = CreateKeyEntity(id, type)
        o.SetObjectState(ObjectState.NotLoaded)
        Dim obj As _ICachedEntity = NormalizeObject(o, CType(GetDictionary(type), System.Collections.IDictionary), _
            add2CacheOnCreate, False, MappingEngine.GetEntitySchema(type))
        If ReferenceEquals(o, obj) AndAlso Not add2CacheOnCreate Then
            o.SetObjectState(ObjectState.Created)
        End If
        Return CType(obj, ISinglePKEntity)
    End Function

    Public Function GetKeyEntityFromCacheOrCreate(Of T As {ISinglePKEntity, New})(ByVal id As Object) As T
        'Dim o As T = CreateObject(Of T)(id)
        'Assert(o IsNot Nothing, "Object must be created: " & id.ToString & ". Type - " & GetType(T).ToString)
        'Using o.GetSyncRoot()
        '    If o.ObjectState = ObjectState.Created AndAlso Not IsNewObject(GetType(T), id) Then
        '        Debug.Assert(Not o.IsLoaded)
        '        Throw New ApplicationException
        '        'o.ObjectState = ObjectState.NotLoaded
        '        'AddObject(o)
        '    End If
        'End Using
        'Return o
        Dim o As T = CreateKeyEntity(Of T)(id)
        o.SetObjectState(ObjectState.NotLoaded)
        Return CType(GetOrAdd2Cache(o, CType(GetDictionary(Of T)(), System.Collections.IDictionary)), T)
    End Function

    Public Function GetKeyEntityFromCacheOrCreate(Of T As {ISinglePKEntity, New})(ByVal id As Object, ByVal add2CacheOnCreate As Boolean) As T
        'Dim o As T = CreateObject(Of T)(id)
        'Assert(o IsNot Nothing, "Object must be created: " & id.ToString & ". Type - " & GetType(T).ToString)
        'Using o.GetSyncRoot()
        '    If o.ObjectState = ObjectState.Created AndAlso Not IsNewObject(GetType(T), id) Then
        '        Debug.Assert(Not o.IsLoaded)
        '        Throw New ApplicationException
        '        'o.ObjectState = ObjectState.NotLoaded
        '        'AddObject(o)
        '    End If
        'End Using
        'Return o
        Dim o As T = CreateKeyEntity(Of T)(id)
        o.SetObjectState(ObjectState.NotLoaded)
        Dim obj As _ICachedEntity = NormalizeObject(o, CType(GetDictionary(Of T)(), IDictionary), _
            add2CacheOnCreate, False, MappingEngine.GetEntitySchema(GetType(T)))
        If ReferenceEquals(o, obj) AndAlso Not add2CacheOnCreate Then
            o.SetObjectState(ObjectState.Created)
        End If
        Return CType(obj, T)
    End Function

    Public Function GetKeyEntityFromCacheOrDB(Of T As {ISinglePKEntity, New})(ByVal id As Object) As T
        Dim o As T = CreateKeyEntity(Of T)(id)
        o.SetObjectState(ObjectState.NotLoaded)
        Return CType(GetFromCacheAsIsOrLoadFromDB(o, CType(GetDictionary(Of T)(), IDictionary)), T)
    End Function

    Public Function GetKeyEntityFromCacheLoadedOrDB(Of T As {ISinglePKEntity, New})(ByVal id As Object) As T
        Dim o As T = CreateKeyEntity(Of T)(id)
        o.SetObjectState(ObjectState.NotLoaded)
        Return CType(GetFromCacheLoadedOrLoadFromDB(o, CType(GetDictionary(Of T)(), IDictionary)), T)
    End Function

    Public Function GetKeyEntityFromCacheOrDB(ByVal id As Object, ByVal type As Type) As ISinglePKEntity
        Dim o As ISinglePKEntity = CreateKeyEntity(id, type)
        o.SetObjectState(ObjectState.NotLoaded)
        Return CType(GetFromCacheAsIsOrLoadFromDB(o, GetDictionary(type)), ISinglePKEntity)
    End Function

    Public Function GetKeyEntityFromCacheLoadedOrDB(ByVal id As Object, ByVal type As Type) As ISinglePKEntity
        Dim o As ISinglePKEntity = CreateKeyEntity(id, type)
        o.SetObjectState(ObjectState.NotLoaded)
        Return CType(GetFromCacheLoadedOrLoadFromDB(o, GetDictionary(type)), ISinglePKEntity)
    End Function

    Public Function [Get](Of T As {ISinglePKEntity, New})(ByVal id As Object) As T
        Return GetKeyEntityFromCacheOrDB(Of T)(id)
    End Function

    Public Function [Get](Of T As {_ICachedEntity, New})(ByVal pk() As PKDesc) As T
        Dim o As _ICachedEntity = CreateObject(Of T)(pk)
        o.SetObjectState(ObjectState.NotLoaded)
        Return CType(GetFromCacheAsIsOrLoadFromDB(o, CType(GetDictionary(Of T)(), IDictionary)), T)
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
#If Not ExcludeFindMethods Then

    Protected Function FindWithJoinsGetKey(Of T As {IKeyEntity, New})(ByVal aspect As QueryAspect, _
        ByVal joins As QueryJoin(), ByVal criteria As IGetFilter) As String
        Dim key As String = String.Empty

        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            'key &= criteria.Filter(GetType(T)).GetStaticString(_schema)
            key &= criteria.Filter().GetStaticString(_schema, GetContextInfo)
        End If

        If joins IsNot Nothing Then
            For Each join As QueryJoin In joins
                If Not QueryJoin.IsEmpty(join) Then
                    key &= join._ToString
                End If
            Next
        End If

        If aspect IsNot Nothing Then
            key &= aspect.GetStaticKey
        End If

        key &= _schema.GetEntityKey(GetContextInfo, GetType(T))

        Return key & GetStaticKey()
    End Function

    Protected Function FindWithJoinGetId(Of T As {IKeyEntity, New})(ByVal aspect As QueryAspect, ByVal joins As QueryJoin(), ByVal criteria As IGetFilter) As String
        Dim id As String = String.Empty

        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            'id &= criteria.Filter(GetType(T))._ToString
            id &= criteria.Filter()._ToString
        End If

        If joins IsNot Nothing Then
            For Each join As QueryJoin In joins
                If Not QueryJoin.IsEmpty(join) Then
                    id &= join._ToString
                End If
            Next
        End If

        If aspect IsNot Nothing Then
            id &= aspect.GetDynamicKey
        End If

        Return id & GetType(T).ToString
    End Function
#End If


    Protected Friend Sub RaiseOnDataAvailable()
        RaiseEvent DataAvailable(Me, _er)
    End Sub

    Protected Friend Sub RaiseOnDataAvailable(ByVal count As Integer, ByVal execTime As TimeSpan, ByVal fetchTime As TimeSpan, _
            ByVal hit As Boolean)
        RaiseEvent DataAvailable(Me, New ExecutionResult(count, execTime, fetchTime, hit, Nothing))
    End Sub

    Private Function GetResultset(Of T As {_ICachedEntity, New})(ByVal withLoad As Boolean, ByVal dic As IDictionary, _
        ByVal id As String, ByVal sync As String, ByVal del As ICacheItemProvoder(Of T), ByRef succeeded As Boolean) As ReadOnlyEntityList(Of T)
        Dim v As ICacheValidator = TryCast(del, ICacheValidator)
        Dim ce As UpdatableCachedItem = CType(GetFromCache(Of T)(dic, sync, id, withLoad, del), UpdatableCachedItem)
        RaiseOnDataAvailable()
        Dim s As IListObjectConverter.ExtractListResult
        Dim r As ReadOnlyEntityList(Of T) = ce.GetObjectList(Of T)(Me, withLoad, del.Created, GetStart, GetLength, s)
        succeeded = True

        If s = IListObjectConverter.ExtractListResult.NeedLoad Then
            withLoad = True
l1:
            del.Renew = True
            ce = CType(GetFromCache(Of T)(dic, sync, id, withLoad, del), UpdatableCachedItem)
            r = ce.GetObjectList(Of T)(Me, withLoad, del.Created, GetStart, GetLength, s)
            Assert(s = IListObjectConverter.ExtractListResult.Successed, "Withload should always successed")
        End If

        If s = IListObjectConverter.ExtractListResult.CantApplyFilter Then
            succeeded = False
            Return r
        End If

        If _externalFilter IsNot Nothing Then
            If Not del.Created Then
                Dim psort As OrderByClause = del.Sort

                If ce.SortEquals(psort, MappingEngine, GetContextInfo) OrElse psort Is Nothing Then
                    If v IsNot Nothing AndAlso Not v.ValidateItemFromCache(ce) Then
                        del.Renew = True
                        GoTo l1
                    End If
                Else
                    If Not del.SmartSort Then
                        del.Renew = True
                        GoTo l1
                    Else
                        'Dim loaded As Integer = 0
                        Dim objs As ReadOnlyEntityList(Of T) = r
                        If objs IsNot Nothing AndAlso objs.Count > 0 Then
                            If CanSortOnClient(GetType(T), CType(objs, System.Collections.ICollection), psort) Then
                                Using SyncHelper.AcquireDynamicLock(sync)
                                    Dim sc As IComparer(Of T) = New EntityComparer(Of T)(psort)
                                    If sc IsNot Nothing Then
                                        Dim os As ReadOnlyEntityList(Of T) = CType(_CreateReadOnlyList(GetType(T), objs), ReadOnlyEntityList(Of T))
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

    Protected Function FindGetKey(ByVal filter As IFilter, ByVal t As Type) As String
        Return filter.GetStaticString(_schema, GetContextInfo) & GetStaticKey() & _schema.GetEntityKey(t)
    End Function

#If Not ExcludeFindMethods Then


    Public Function FindWithJoins(Of T As {IKeyEntity, New})(ByVal aspect As QueryAspect, _
        ByVal joins() As QueryJoin, ByVal criteria As IGetFilter, _
        ByVal sort As Sortexpression, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
        Return FindWithJoins(Of T)(aspect, joins, criteria, sort, withLoad, Nothing)
    End Function

    Public Function FindWithJoins(Of T As {IKeyEntity, New})(ByVal aspect As QueryAspect, _
        ByVal joins() As QueryJoin, ByVal criteria As IGetFilter, _
        ByVal sort As Sortexpression, ByVal withLoad As Boolean, ByVal cols() As String) As ReadOnlyList(Of T)

        Dim key As String = FindWithJoinsGetKey(Of T)(aspect, joins, criteria)

        Dim dic As IDictionary = GetDic(_cache, key)

        Dim id As String = FindWithJoinGetId(Of T)(aspect, joins, criteria)

        Dim sync As String = id & GetStaticKey()

        Dim l As List(Of EntityPropertyAttribute) = Nothing
        If cols IsNot Nothing Then
            Dim has_id As Boolean = False
            l = New List(Of EntityPropertyAttribute)
            For Each c As String In cols
                Dim col As EntityPropertyAttribute = _schema.GetColumnByPropertyAlias(GetType(T), c)
                If col Is Nothing Then
                    Throw New ArgumentException("Invalid column name " & c)
                End If
                If (_schema.GetAttributes(GetType(T), col) And Field2DbRelations.PK) = Field2DbRelations.PK Then
                    has_id = True
                End If
                l.Add(col)
            Next
            If Not has_id Then
                'l.Add(SQLGenerator.GetColumnByFieldName(GetType(T), OrmBaseT.PKName))
                l.Add(MappingEngine.GetPrimaryKeys(GetType(T))(0))
            End If
        End If

        Dim del As ICacheItemProvoder(Of T) = GetCustDelegate(Of T)(aspect, joins, GetFilter(criteria, GetType(T)), sort, key, id, l)
        Dim s As Boolean = True
        Dim r As ReadOnlyEntityList(Of T) = GetResultset(Of T)(withLoad, dic, id, sync, del, s)
        If Not s Then
            Assert(_externalFilter IsNot Nothing, "GetResultset should fail only when external filter specified")
            Using ac As New ApplyCriteria(Me, Nothing)
                Dim c As Condition.ConditionConstructor = New Condition.ConditionConstructor
                c.AddFilter(del.Filter).AddFilter(ac.oldfilter)
                r = FindWithJoins(Of T)(aspect, joins, New PredicateLink(c), sort, withLoad, cols)
            End Using
        End If
        Return CType(r, Global.Worm.ReadOnlyList(Of T))
    End Function

    'Public Function FindDistinct(Of T As {OrmBase, New})(ByVal joins() As QueryJoin, ByVal criteria As CriteriaLink, _
    '    ByVal sort As Sort, ByVal withLoad As Boolean) As ICollection(Of T)

    '    Dim key As String = "distinct" & _schema.GetEntityKey(GetType(T))

    '    If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
    '        key &= criteria.Filter.GetStaticString
    '    End If

    '    If joins IsNot Nothing Then
    '        For Each join As QueryJoin In joins
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
    '        For Each join As QueryJoin In joins
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

    Public Function FindJoin(Of T As {IKeyEntity, New})(ByVal type2join As Type, ByVal joinField As String, ByVal criteria As IGetFilter, _
        ByVal sort As Sortexpression, ByVal withLoad As Boolean) As ReadOnlyList(Of T)

        Return FindJoin(Of T)(type2join, joinField, FilterOperation.Equal, JoinType.Join, criteria, sort, withLoad)
    End Function

    Public Function FindJoin(Of T As {IKeyEntity, New})(ByVal type2join As Type, ByVal joinField As String, _
        ByVal joinOperation As FilterOperation, ByVal criteria As IGetFilter, _
        ByVal sort As Sortexpression, ByVal withLoad As Boolean) As ReadOnlyList(Of T)

        Return FindJoin(Of T)(type2join, joinField, joinOperation, JoinType.Join, criteria, sort, withLoad)
    End Function

    Public Function FindJoin(Of T As {IKeyEntity, New})(ByVal type2join As Type, ByVal joinField As String, _
        ByVal joinOperation As FilterOperation, ByVal joinType As JoinType, ByVal criteria As IGetFilter, _
        ByVal sort As Sortexpression, ByVal withLoad As Boolean) As ReadOnlyList(Of T)

        Return FindWithJoins(Of T)(Nothing, New QueryJoin() {MakeJoin(type2join, GetType(T), joinField, joinOperation, joinType)}, _
            criteria, sort, withLoad)
    End Function

    Public Function FindJoin(Of T As {IKeyEntity, New})(ByVal top As Integer, ByVal type2join As Type, ByVal joinField As String, _
        ByVal joinOperation As FilterOperation, ByVal joinType As JoinType, ByVal criteria As IGetFilter, _
        ByVal sort As Sortexpression, ByVal withLoad As Boolean) As ReadOnlyList(Of T)

        Return FindWithJoins(Of T)(StmtGenerator.CreateTopAspect(top), New QueryJoin() {MakeJoin(type2join, GetType(T), joinField, joinOperation, joinType)}, _
            criteria, sort, withLoad)
    End Function

    Public Function Find(Of T As {IKeyEntity, New})(ByVal criteria As IGetFilter) As ReadOnlyList(Of T)
        Return Find(Of T)(criteria, Nothing, False)
    End Function

    Public Function FindDistinct(Of T As {IKeyEntity, New})(ByVal criteria As IGetFilter, _
        ByVal sort As Sortexpression, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
        Dim filter As IFilter = Nothing
        If criteria IsNot Nothing Then
            'filter = criteria.Filter(GetType(T))
            filter = criteria.Filter()
        End If
        Dim joins() As QueryJoin = Nothing
        Dim appendMain As Boolean
        HasJoins(_schema, GetType(T), filter, sort, GetContextInfo, joins, appendMain, New EntityUnion(GetType(T)))
        Return FindWithJoins(Of T)(New DistinctAspect(), joins, filter, sort, withLoad)
    End Function

    Public Function Find(Of T As {IKeyEntity, New})(ByVal criteria As IGetFilter, _
        ByVal sort As Sortexpression, ByVal withLoad As Boolean) As ReadOnlyList(Of T)

        If criteria Is Nothing Then
            Throw New ArgumentNullException("filter")
        End If

        'Dim filter As IFilter = criteria.Filter(GetType(T))
        Dim filter As IFilter = criteria.Filter()

        Dim joins() As QueryJoin = Nothing
        Dim appendMain As Boolean
        If HasJoins(_schema, GetType(T), filter, sort, GetContextInfo, joins, appendMain, New EntityUnion(GetType(T))) Then
            Dim c As Condition.ConditionConstructor = New Condition.ConditionConstructor
            c.AddFilter(filter)
            Return FindWithJoins(Of T)(Nothing, joins, New PredicateLink(c), sort, withLoad)
        Else
            Dim key As String = FindGetKey(filter, GetType(T))

            Dim dic As IDictionary = GetDic(_cache, key)

            Dim id As String = filter._ToString
            Dim sync As String = id & GetStaticKey()

            'CreateDepends(filter, key, id)

            Dim del As ICacheItemProvoder(Of T) = GetCustDelegate(Of T)(filter, sort, key, id)
            'Dim ce As CachedItem = GetFromCache(Of T)(dic, sync, id, withLoad, del)
            Dim s As Boolean = True
            Dim r As ReadOnlyList(Of T) = CType(GetResultset(Of T)(withLoad, dic, id, sync, del, s), Global.Worm.ReadOnlyList(Of T))
            If Not s Then
                Assert(_externalFilter IsNot Nothing, "GetResultset should fail only when external filter specified")
                Using ac As New ApplyCriteria(Me, Nothing)
                    Dim c As Condition.ConditionConstructor = New Condition.ConditionConstructor
                    c.AddFilter(del.Filter).AddFilter(ac.oldfilter)
                    r = Find(Of T)(New PredicateLink(c), sort, withLoad)
                End Using
            End If
            Return r
        End If
    End Function

    Public Function Find(Of T As {IKeyEntity, New})(ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal cols() As String) As ReadOnlyList(Of T)

        If criteria Is Nothing Then
            Throw New ArgumentNullException("criteria")
        End If

        'Dim filter As IFilter = criteria.Filter(GetType(T))
        Dim filter As IFilter = criteria.Filter()

        Dim key As String = FindGetKey(filter, GetType(T)) '_schema.GetEntityKey(GetType(T)) & criteria.Filter.GetStaticString & GetStaticKey()

        Dim dic As IDictionary = GetDic(_cache, key)

        Dim id As String = filter._ToString
        Dim sync As String = id & GetStaticKey()

        'CreateDepends(filter, key, id)

        Dim del As ICacheItemProvoder(Of T) = GetCustDelegate(Of T)(filter, sort, key, id, cols)
        Dim s As Boolean = True
        Dim r As ReadOnlyList(Of T) = CType(GetResultset(Of T)(True, dic, id, sync, del, s), Global.Worm.ReadOnlyList(Of T))
        If Not s Then
            Assert(_externalFilter IsNot Nothing, "GetResultset should fail only when external filter specified")
            Using ac As New ApplyCriteria(Me, Nothing)
                Dim c As Condition.ConditionConstructor = New Condition.ConditionConstructor
                c.AddFilter(del.Filter).AddFilter(ac.oldfilter)
                r = Find(Of T)(New PredicateLink(c), sort, cols)
            End Using
        End If
        Return r
    End Function

    Public Function FindTop(Of T As {IKeyEntity, New})(ByVal top As Integer, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
        Return FindTop(Of T)(top, criteria, sort, withLoad, Nothing)
    End Function

    Public Function FindTop(Of T As {IKeyEntity, New})(ByVal top As Integer, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal cols() As String) As ReadOnlyList(Of T)
        Return FindTop(Of T)(top, criteria, sort, True, cols)
    End Function

    Protected Function FindTop(Of T As {IKeyEntity, New})(ByVal top As Integer, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean, ByVal cols() As String) As ReadOnlyList(Of T)

        Dim filter As IFilter = Nothing
        If criteria IsNot Nothing Then
            'filter = criteria.Filter(GetType(T))
            filter = criteria.Filter()
        End If
        Dim joins() As QueryJoin = Nothing
        Dim appendMain As Boolean
        HasJoins(_schema, GetType(T), filter, sort, GetContextInfo, joins, appendMain, New EntityUnion(GetType(T)))
        Return FindWithJoins(Of T)(StmtGenerator.CreateTopAspect(top, sort), joins, filter, sort, withLoad, cols)
    End Function
#End If
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

    Protected Friend Delegate Function ValDelegate(ByRef ce As CachedItemBase, _
        ByVal del As ICacheItemProvoderBase, ByVal dic As IDictionary, ByVal id As Object, _
        ByVal sync As String, ByVal v As ICacheValidator) As Boolean

    Protected Friend Function GetFromCache2(ByVal dic As IDictionary, ByVal sync As String, ByVal id As Object, _
        ByVal withLoad As Boolean, ByVal del As ICacheItemProvoderBase) As CachedItemBase

        Return GetFromCacheBase(dic, sync, id, New TypeWrap(Of Object)(New Boolean() {withLoad}), del, Nothing)
    End Function

    Protected Friend Function GetFromCache(Of T As ICachedEntity)(ByVal dic As IDictionary, ByVal sync As String, ByVal id As Object, _
        ByVal withLoad As Boolean, ByVal del As ICacheItemProvoderBase) As CachedItemBase

        Return GetFromCacheBase(dic, sync, id, New TypeWrap(Of Object)(New Boolean() {withLoad}), del, AddressOf _ValCE(Of T))
    End Function

    Protected Friend Function GetFromCacheBase(ByVal dic As IDictionary, ByVal sync As String, ByVal id As Object, _
        ByVal ctx As TypeWrap(Of Object), ByVal del As ICacheItemProvoderBase, _
        ByVal vdel As ValDelegate) As CachedItemBase

        Invariant()

        Dim v As ICacheValidator = Nothing

        If Not _dont_cache_lists Then
            v = TryCast(del, ICacheValidator)
        End If

        Dim renew As Boolean = v IsNot Nothing AndAlso Not v.ValidateBeforCacheProbe()

        'Dim sort As String = del.Sort
        'Dim sort_type As SortType = del.SortType
        'Dim f As IOrmFilter = del.Filter

        Dim ce As CachedItemBase = Nothing
        del.Created = False

        If renew OrElse del.Renew OrElse _dont_cache_lists Then
            ce = del.GetCacheItem(ctx)
            If Not _dont_cache_lists Then
                dic(id) = ce
            End If
            del.Created = True
        Else
            ce = CType(dic(id), CachedItemBase)
            If ce Is Nothing Then
l1:
                Using SyncHelper.AcquireDynamicLock(sync)
                    ce = CType(dic(id), CachedItemBase)
                    Dim emp As Boolean = ce Is Nothing
                    If emp OrElse del.Renew OrElse _dont_cache_lists Then
                        ce = del.GetCacheItem(ctx)
                        If Not _dont_cache_lists OrElse Not emp Then
                            dic(id) = ce
                        End If
                        del.Created = True
                    End If
                End Using
            End If
        End If

        If del.Renew Then
            If Not _dont_cache_lists Then del.CreateDepends(ce)
            _er = New ExecutionResult(ce.GetCount(Cache), ce.ExecutionTime, ce.FetchTime, Not del.Created, _loadedInLastFetch)
            Return ce
        End If

        If del.Created Then
            If Not _dont_cache_lists Then del.CreateDepends(ce)
        ElseIf vdel IsNot Nothing Then
            If Not vdel(ce, del, dic, id, sync, v) Then
                GoTo l1
            End If
        End If

        Dim l As Nullable(Of Integer) = Nothing
        If del.Created Then
            l = _loadedInLastFetch
        End If
        _er = New ExecutionResult(ce.GetCount(Cache), ce.ExecutionTime, ce.FetchTime, Not del.Created, l)
        Return ce
    End Function

    Private Function _ValCE(Of T As ICachedEntity)(ByRef ce As CachedItemBase, _
        ByVal del_ As ICacheItemProvoderBase, ByVal dic As IDictionary, ByVal id As Object, _
        ByVal sync As String, ByVal v As ICacheValidator) As Boolean

        Dim del As ICacheItemProvoder(Of T) = CType(del_, Global.Worm.OrmManager.ICacheItemProvoder(Of T))

        If ce._expires = Date.MinValue Then
            ce._expires = _expiresPattern
        End If

        Dim ce_ As UpdatableCachedItem = CType(ce, UpdatableCachedItem)

        If ce_.Expires Then
            ce_.Expire()
            del.Renew = True
            Return False
        End If

        If v IsNot Nothing AndAlso Not v.ValidateItemFromCache(ce_) Then
            del.Renew = True
            Return False
        End If

        If _externalFilter Is Nothing Then
            Dim psort As OrderByClause = del.Sort

            If Not (ce_.SortEquals(psort, MappingEngine, GetContextInfo) OrElse psort Is Nothing) Then
                Dim objs As ReadOnlyEntityList(Of T) = ce_.GetObjectList(Of T)(Me)
                If objs IsNot Nothing Then
                    If objs.Count = 0 Then Return True
                    If CanSortOnClient(GetType(T), CType(objs, System.Collections.ICollection), psort) Then
                        Using SyncHelper.AcquireDynamicLock(sync)
                            Dim sc As IComparer(Of T) = New EntityComparer(Of T)(psort)
                            If sc IsNot Nothing Then
                                Dim os As ReadOnlyEntityList(Of T) = CType(_CreateReadOnlyList(GetType(T), objs), ReadOnlyEntityList(Of T))
                                os.Sort(sc)
                                Dim ce2 As UpdatableCachedItem = del.GetCacheItem(os)
                                If ce_.CanRenewAfterSort Then
                                    dic(id) = ce2
                                End If
                                ce = ce2
                            Else
                                del.Renew = True
                                Return False
                            End If
                        End Using
                    Else
                        del.Renew = True
                        Return False
                    End If
                Else
                    'dic.Remove(id)
                    del.Renew = True
                    Return False
                End If
            End If
        End If

        Return True
    End Function

    Public Shared Function CanSortOnClient(ByVal t As Type, ByVal col As ICollection, ByVal orderBy As OrderByClause) As Boolean

        For Each sort As SortExpression In orderBy
            If Not TypeOf sort.Operand Is EntityExpression Then
                Return False
            End If
        Next

        Dim loaded As Integer = 0
        For Each o As IEntity In col
            If o.IsLoaded Then loaded += 1
            If col.Count - loaded > 10 Then
                Return False
            End If
        Next
        Return True
    End Function

#End Region

#Region " Object support "
    Protected Friend Function GetSyncForSave(ByVal t As Type, ByVal obj As ICachedEntity) As IDisposable
#If DebugLocks Then
            Return SyncHelper.AcquireDynamicLock_Debug("4098jwefpv345mfds-" & New EntityProxy(obj).ToString, "d:\temp\")
#Else
        Return SyncHelper.AcquireDynamicLock("4098jwefpv345mfds-" & New EntityProxy(obj).ToString)
#End If
    End Function

    'Protected Friend Function LoadType(ByVal id As Object, ByVal t As Type, ByVal load As Boolean, ByVal checkOnCreate As Boolean) As IKeyEntity
    '    Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic
    '    Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("LoadType", flags, Nothing, Reflection.CallingConventions.Any, New Type() {GetType(Object), GetType(Boolean), GetType(Boolean)}, Nothing)
    '    Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
    '    Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {id, load, checkOnCreate}, Nothing), IKeyEntity)
    'End Function

    Public Function CreateKeyEntity(ByVal id As Object, ByVal t As Type) As ISinglePKEntity
        Return SinglePKEntity.CreateKeyEntity(id, t, _cache, _schema)
    End Function

    Public Function CreateKeyEntity(Of T As {ISinglePKEntity, New})(ByVal id As Object) As T
        Return SinglePKEntity.CreateKeyEntity(Of T)(id, _cache, _schema)
    End Function

    Public Function CreateObject(Of T As {_ICachedEntity, New})(ByVal pk As IEnumerable(Of PKDesc)) As T
        Return CachedEntity.CreateObject(Of T)(pk, _cache, _schema)
    End Function

    Public Function CreateObject(ByVal pk As IEnumerable(Of PKDesc), ByVal type As Type) As Object
        Return CachedEntity.CreateObject(pk, type, _cache, _schema)
    End Function

    Public Function CreateEntity(Of T As {_ICachedEntity, New})(ByVal pk As IEnumerable(Of PKDesc)) As T
        Return CachedEntity.CreateEntity(Of T)(pk, _cache, _schema)
    End Function

    Public Function CreateEntity(ByVal pk As IEnumerable(Of PKDesc), ByVal t As Type) As _ICachedEntity
        Return CachedEntity.CreateEntity(pk, t, _cache, _schema)
    End Function

    Protected Friend Function CreateEntity(ByVal t As Type) As IEntity
        Return Entity.CreateEntity(t, _cache, _schema)
    End Function

    Protected Friend Function CreateEntity(Of T As {_IEntity, New})() As T
        Return Entity.CreateEntity(Of T)(_cache, _schema)
    End Function

    'Public Function NormalizeObject(ByVal obj As _ICachedEntity, ByVal dic As IDictionary, _
    '    ByVal fromDb As Boolean, ByVal oschema As IEntitySchema) As _ICachedEntity
    '    Return NormalizeObject(obj, dic, True, fromDb, oschema)
    'End Function

    Public Function GetOrAdd2Cache(ByVal obj As _ICachedEntity, ByVal dic As IDictionary) As _ICachedEntity
        Return NormalizeObject(obj, dic, True, False, MappingEngine.GetEntitySchema(obj.GetType))
    End Function

    Public Function NormalizeObject(ByVal obj As _ICachedEntity, ByVal entityDictionary As IDictionary, _
        ByVal add2Cache As Boolean, ByVal fromDb As Boolean, ByVal oschema As IEntitySchema) As _ICachedEntity
        Dim cb As ICacheBehavior = TryCast(oschema, ICacheBehavior)
        Return CType(_cache.FindObjectInCache(obj.GetType, obj, New CacheKey(obj), cb, entityDictionary, add2Cache, fromDb), _ICachedEntity)
    End Function

    Public Function GetFromCacheAsIsOrLoadFromDB(ByVal obj As _ICachedEntity, ByVal dic As IDictionary) As _ICachedEntity
        Dim cb As ICacheBehavior = TryCast(obj.GetEntitySchema(MappingEngine), ICacheBehavior)
        Dim ck As CacheKey = New CacheKey(obj)
        Dim t As Type = obj.GetType
        Dim e As _ICachedEntity = CType(_cache.FindObjectInCache(t, obj, ck, cb, dic, False, False), _ICachedEntity)
        If e Is Nothing Then
            If _cache.CheckNonExistent(t, ck) Then
                Return Nothing
            End If

            Me.Load(obj)
            e = CType(_cache.FindObjectInCache(t, obj, ck, cb, dic, False, False), _ICachedEntity)
            If e IsNot Nothing Then
                Assert(e.ObjectState <> ObjectState.NotFoundInSource, "Object {0} cannot be in NotFoundInSource state", e.ObjName)
            Else
                _cache.AddNonExistentObject(t, ck)
            End If
        End If
        Return e
    End Function

    Public Function GetFromCacheLoadedOrLoadFromDB(ByVal obj As _ICachedEntity, ByVal dic As IDictionary) As _ICachedEntity
        Dim cb As ICacheBehavior = TryCast(obj.GetEntitySchema(MappingEngine), ICacheBehavior)
        Dim ck As CacheKey = New CacheKey(obj)
        Dim e As _ICachedEntity = CType(_cache.FindObjectInCache(obj.GetType, obj, ck, cb, dic, False, False), _ICachedEntity)
        If e Is Nothing OrElse (e.ObjectState = ObjectState.NotLoaded) Then
            If e Is Nothing Then
                Me.Load(obj)
            Else
                Me.Load(e)
            End If
            e = CType(_cache.FindObjectInCache(obj.GetType, obj, ck, cb, dic, False, False), _ICachedEntity)
        ElseIf Not e.IsLoaded Then
            e = Nothing
        End If
        Return e
    End Function

    'Public Function GetLoadedObjectFromCacheOrDB(ByVal obj As _ICachedEntity, ByVal dic As IDictionary) As _ICachedEntity
    '    Return _cache.NormalizeObject(obj, True, True, dic, True, Me)
    'End Function

    'Protected Function LoadTypeInternal(Of T As {IKeyEntity, New})(ByVal id As Object, _
    '    ByVal load As Boolean, ByVal checkOnCreate As Boolean, ByVal dic As IDictionary(Of Object, T), ByVal addOnCreate As Boolean) As T

    '    Dim o As T = CreateKeyEntity(Of T)(id)
    '    Return CType(_cache.NormalizeObject(o, load, checkOnCreate, CType(dic, System.Collections.IDictionary), addOnCreate, Me), T)
    'End Function

    '    Protected Function GetObjectFromCache(Of T As {IOrmBase, New})(ByVal obj As T, ByVal dic As IDictionary(Of Object, T), _
    '        ByVal load As Boolean, ByVal checkOnCreate As Boolean, ByVal addOnCreate As Boolean) As T

    '        If obj Is Nothing Then
    '            Throw New ArgumentNullException("obj")
    '        End If

    '        '    Return GetObjectFromCache(obj.Identifier, dic)
    '        'End Function

    '        'Protected Function GetObjectFromCache(Of T As {OrmBase, New})(ByVal id As Integer, _
    '        '    ByVal dic As IDictionary(Of Integer, T)) As T

    '        Dim type As Type = GetType(T)

    '#If DEBUG Then
    '        If dic Is Nothing Then
    '            Dim name As String = GetType(T).Name
    '            Throw New OrmManagerException("Collection for " & name & " not exists")
    '        End If
    '#End If
    '        Dim created As Boolean = False ', checked As Boolean = False
    '        Dim a As T = Nothing
    '        Dim id As Object = obj.Identifier
    '        If Not dic.TryGetValue(id, a) AndAlso _newMgr IsNot Nothing Then
    '            a = CType(_newMgr.GetNew(type, obj.GetPKValues), T)
    '            If a IsNot Nothing Then Return a
    '        End If
    '        Dim sync_key As String = "LoadType" & id.ToString & type.ToString
    '        If a Is Nothing Then
    '            Using SyncHelper.AcquireDynamicLock(sync_key)
    '                If Not dic.TryGetValue(id, a) Then
    '                    If ObjectMappingEngine.GetUnions(type) IsNot Nothing Then
    '                        Throw New NotSupportedException
    '                    Else
    '                        a = obj
    '                        'a.Init(_cache, _schema)
    '                    End If

    '                    If load Then
    '                        a.Load()
    '                        If Not a.IsLoaded Then
    '                            a = Nothing
    '                        End If
    '                    End If
    '                    If a IsNot Nothing AndAlso checkOnCreate Then
    '                        'checked = True
    '                        If Not a.IsLoaded Then
    '                            a.Load()
    '                            If a.ObjectState = ObjectState.NotFoundInSource Then
    '                                a = Nothing
    '                            End If
    '                        End If
    '                    End If
    '                    created = True
    '                End If
    '            End Using
    '        End If

    '        If a IsNot Nothing Then
    '            If created AndAlso addOnCreate Then
    '                AddObjectInternal(a, CType(dic, System.Collections.IDictionary))
    '            End If
    '            If Not created AndAlso load AndAlso Not a.IsLoaded Then
    '                a.Load()
    '            End If
    '        End If

    '        Return a
    '    End Function

    '    Protected Friend Function LoadType(Of T As {IKeyEntity, New})(ByVal id As Object, _
    '        ByVal load As Boolean, ByVal checkOnCreate As Boolean) As T

    '        Dim dic As Generic.IDictionary(Of Object, T) = GetDictionary(Of T)()

    '#If DEBUG Then
    '        If dic Is Nothing Then
    '            Dim name As String = GetType(T).Name
    '            Throw New OrmManagerException("Collection for " & name & " not exists")
    '        End If
    '#End If

    '        Return LoadTypeInternal(Of T)(id, load, checkOnCreate, dic, True)
    '    End Function

    Protected Sub Add2Cache(ByVal obj As ICachedEntity)
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        'If obj.Identifier < 0 Then
        '    Throw New ArgumentNullException(String.Format("Cannot add object {0} to cache ", obj.ObjName))
        'End If

        'If obj.ObjectState = ObjectState.Created Then
        '    Throw New ArgumentNullException(String.Format("Cannot add object {0} to cache ", obj.ObjName))
        'End If

        'Debug.Assert(obj.IsLoaded)
        Dim t As Type = obj.GetType

        Dim dic As IDictionary = GetDictionary(t)

        If dic Is Nothing Then
            ''todo: throw an exception when all collections will be implemented
            'Return
            Dim name As String = t.Name
            Throw New OrmManagerException("Collection for " & name & " not exists")
        End If

        Dim id As CacheKey = New CacheKey(obj)
        Dim sync_key As String = "LoadType" & id.ToString & t.ToString

        Using SyncHelper.AcquireDynamicLock(sync_key)
            CacheBase.AddObjectInternal(obj, id, dic)
            _cache.RemoveNonExistent(t, id)
        End Using
    End Sub

    Protected Friend Function EnsureInCache(ByVal obj As ICachedEntity) As ICachedEntity
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        'If obj.Identifier < 0 Then
        '    Throw New ArgumentNullException(String.Format("Cannot add object {0} to cache ", obj.ObjName))
        'End If

        Dim dic As IDictionary = GetDictionary(obj.GetType)

        If dic Is Nothing Then
            ''todo: throw an exception when all collections will be implemented
            'Return
            Dim name As String = obj.GetType.Name
            Throw New OrmManagerException("Collection for " & name & " not exists")
        End If

        Return EnsureInCache(obj, dic)
    End Function

    Protected Friend Function EnsureInCache(ByVal obj As ICachedEntity, ByVal dic As IDictionary) As ICachedEntity
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        Dim id As CacheKey = New CacheKey(obj)
        SyncLock dic.SyncRoot
            Dim o As ICachedEntity = CType(dic(id), ICachedEntity)
            If o Is Nothing Then
                dic.Add(id, obj)
                o = obj
            End If
            Return o
        End SyncLock
    End Function

    Public Function RemoveObjectFromCache(ByVal obj As _ICachedEntity) As Boolean
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj parameter cannot be nothing")
        End If

        If obj.ObjectState = ObjectState.Modified OrElse obj.ObjectState = ObjectState.Deleted Then
            Return False
        End If

        Return _RemoveObjectFromCache(obj, False)
    End Function

    Protected Friend Function _RemoveObjectFromCache(ByVal obj As _ICachedEntity, forseDelete As Boolean) As Boolean
        'Debug.Assert(Not obj.IsLoaded)
        Dim t As System.Type = obj.GetType

        Dim name As String = t.Name
        Dim cb As ICacheBehavior = TryCast(obj.GetEntitySchema(MappingEngine), ICacheBehavior)
        Dim dic As IDictionary = GetDictionary(t, cb)
        If dic Is Nothing Then
            ''todo: throw an exception when all collections will be implemented
            'Return
            Throw New OrmManagerException("Collection for " & name & " not exists")
        End If

        Dim id As CacheKey = New CacheKey(obj)
        Dim sync_key As String = "LoadType" & id.ToString & t.ToString

        Using SyncHelper.AcquireDynamicLock(sync_key)
            Using obj.LockEntity
                If obj.ObjectState = ObjectState.Modified OrElse (obj.ObjectState = ObjectState.Deleted AndAlso Not forseDelete) Then
                    Return False
                End If

                dic.Remove(id)

                Dim c As OrmCache = TryCast(_cache, OrmCache)
                If c IsNot Nothing Then
                    c.RemoveDepends(obj)
#If OLDM2M Then
                    Dim orm As _IKeyEntity = TryCast(obj, _IKeyEntity)
                    If orm IsNot Nothing Then
                        For Each o As Pair(Of M2MCache, Pair(Of String, String)) In c.GetM2MEntries(orm, Nothing)
                            If Not o.First.Entry.HasChanges Then
                                Dim mdic As IDictionary = GetDic(Cache, o.Second.First)
                                mdic.Remove(o.Second.Second)
                            End If
                        Next
                    End If
#End If
                End If

                _cache.RegisterRemoval(obj, MappingEngine, cb)
            End Using

            Assert(Not IsInCachePrecise(obj), "Object {0} must not be in cache", obj.ObjName)
            Assert(obj.ObjectState <> ObjectState.Modified, "Object {0} cannot be in Modified state", obj.ObjName)
        End Using
        Return True
    End Function
#End Region

#Region " helpers "
    Protected Function MakeJoin(ByVal joinOS As EntityUnion, ByVal selectOS As EntityUnion, _
        ByVal type2join As Type, ByVal field As String, _
        ByVal oper As Criteria.FilterOperation, ByVal joinType As JoinType, _
        Optional ByVal switchTable As Boolean = False) As QueryJoin
        Return MakeJoin(_schema, joinOS, type2join, selectOS, field, oper, joinType, switchTable)
    End Function

    Protected Function MakeJoin( _
        ByVal type2join As Type, ByVal selectType As Type, ByVal field As String, _
        ByVal oper As Criteria.FilterOperation, ByVal joinType As JoinType, _
        Optional ByVal switchTable As Boolean = False) As QueryJoin
        Return MakeJoin(_schema, New EntityUnion(type2join), type2join, New EntityUnion(selectType), field, oper, joinType, switchTable)
    End Function

    Protected Function MakeM2MJoin(ByVal m2m As M2MRelationDesc, ByVal type2join As Type) As QueryJoin()
        Return MakeM2MJoin(_schema, m2m, type2join)
    End Function

    Friend Shared Function GetSync(ByVal key As String, ByVal id As String) As String
        Return id & Const_KeyStaticString & key
    End Function

    Public Function GetDictionary(ByVal t As Type) As IDictionary
        Return _cache.GetOrmDictionary(t, _schema)
    End Function

    Public Function GetDictionary(ByVal t As Type, ByVal cb As ICacheBehavior) As IDictionary
        Return _cache.GetOrmDictionary(t, cb)
    End Function

    Public Function GetDictionary(Of T As _ICachedEntity)() As Generic.IDictionary(Of Object, T)
        Return _cache.GetOrmDictionary(Of T)(_schema)
    End Function

    Public Function GetDictionary(Of T As _ICachedEntity)(ByVal cb As ICacheBehavior) As Generic.IDictionary(Of Object, T)
        Return _cache.GetOrmDictionary(Of T)(cb)
    End Function

    Public Function IsInCachePrecise(ByVal obj As ICachedEntity) As Boolean
        Return _cache.IsInCachePrecise(obj, _schema)
    End Function

    Public Function IsInCache(ByVal id As Object, ByVal t As Type) As Boolean
        Return _cache.IsInCache(id, t, _schema)
    End Function

    <Conditional("DEBUG")> _
    Protected Overridable Sub Invariant()
        'Debug.Assert(Not _disposed)
        If _disposed Then
            Throw New ObjectDisposedException("OrmManager")
        End If

        'Debug.Assert(_schema IsNot Nothing)
        If _schema Is Nothing Then
            Throw New ArgumentNullException("Schema cannot be nothing")
        End If

        'Debug.Assert(_cache IsNot Nothing)
        If _cache Is Nothing Then
            Throw New ArgumentNullException("OrmCache cannot be nothing")
        End If
    End Sub

#End Region

#Region " shared helpers "

    Private Shared Sub InsertObject(Of T As ICachedEntity)(ByVal cache As CacheBase, ByVal check_loaded As Boolean, _
        ByVal entityDictionary As Dictionary(Of ICachedEntity, Object), ByVal o As ICachedEntity)
        If o IsNot Nothing Then
            If (Not o.IsLoaded OrElse Not check_loaded) AndAlso o.ObjectState <> ObjectState.NotFoundInSource Then
                If (cache Is Nothing OrElse Not (o.ObjectState = ObjectState.Created AndAlso cache.IsNewObject(GetType(T), GetPKValues(o, Nothing)))) _
                    AndAlso Not entityDictionary.ContainsKey(o) Then
                    entityDictionary.Add(o, Nothing)
                End If
            End If
        End If
    End Sub

    Private Shared Sub InsertObject(Of T As ICachedEntity)(ByVal cache As CacheBase, _
        ByVal check_loaded As Boolean, ByVal entityDictionary As Dictionary(Of ICachedEntity, Object), ByVal o As ICachedEntity, _
        ByVal properties As List(Of EntityExpression), ByVal mpe As ObjectMappingEngine,
        ByVal map As Collections.IndexedCollection(Of String, MapField2Column))

        If o IsNot Nothing Then
            If (Not o.IsLoaded OrElse Not check_loaded) AndAlso o.ObjectState <> ObjectState.NotFoundInSource Then
                If cache Is Nothing OrElse Not (o.ObjectState = ObjectState.Created AndAlso cache.IsNewObject(GetType(T), GetPKValues(o, Nothing))) Then
                    For Each p As EntityExpression In properties
                        If Not IsPropertyLoaded(o, p.ObjectProperty.PropertyAlias, map, mpe) AndAlso Not entityDictionary.ContainsKey(o) Then
                            entityDictionary.Add(o, Nothing)
                            Exit For
                        End If
                    Next
                End If
            End If
        End If
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <typeparam name="T"></typeparam>
    ''' <param name="cache">Pass null if there is no <see cref="INewObjectsStore" /> or you dont' want use it</param>
    ''' <param name="objs"></param>
    ''' <param name="start"></param>
    ''' <param name="length"></param>
    ''' <param name="check_loaded"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function FormPKValues(Of T As ICachedEntity)(ByVal cache As CacheBase, ByVal objs As ReadOnlyEntityList(Of T), _
        ByVal start As Integer, ByVal length As Integer, _
        Optional ByVal check_loaded As Boolean = True) As ICollection(Of ICachedEntity)

        Dim entityDictionary As New Dictionary(Of ICachedEntity, Object)
        If length > objs.Count - start Then
            length = objs.Count - start
        End If

        For i As Integer = start To start + length - 1
            Dim o As ICachedEntity = objs(i)
            InsertObject(Of T)(cache, check_loaded, entityDictionary, o)
        Next

        Return entityDictionary.Keys
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <typeparam name="T"></typeparam>
    ''' <param name="cache">Pass null if there is no <see cref="INewObjectsStore" /> or you dont' want use it</param>
    ''' <param name="objs"></param>
    ''' <param name="start"></param>
    ''' <param name="length"></param>
    ''' <param name="check_loaded"></param>
    ''' <param name="properties"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function FormPKValues(Of T As ICachedEntity)(ByVal cache As CacheBase, _
        ByVal objs As ReadOnlyEntityList(Of T), ByVal start As Integer, ByVal length As Integer, _
        ByVal check_loaded As Boolean, ByVal properties As List(Of EntityExpression), ByVal mpe As ObjectMappingEngine) As ICollection(Of ICachedEntity)

        Dim entityDictionary As New Dictionary(Of ICachedEntity, Object)
        Dim map As Collections.IndexedCollection(Of String, MapField2Column) = Nothing
        If mpe IsNot Nothing Then
            map = mpe.GetEntitySchema(objs.RealType).FieldColumnMap
        End If
        For i As Integer = start To start + length - 1
            Dim o As ICachedEntity = objs(i)
            Dim ll As IPropertyLazyLoad = TryCast(o, IPropertyLazyLoad)
            If ll IsNot Nothing Then
                InsertObject(Of T)(cache, check_loaded, entityDictionary, o, properties, mpe, map)
            Else
                InsertObject(Of T)(cache, check_loaded, entityDictionary, o)
            End If
        Next
        Return entityDictionary.Keys
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

    Protected Friend Shared Sub WriteLine(ByVal message As String)
        Trace.WriteLine(message)
        Trace.WriteLine(Now & Environment.StackTrace)
    End Sub

    Protected Shared Sub Assert(ByVal condition As Boolean, ByVal message As String, ParamArray params As Object())
        'Debug.Assert(condition, String.Format(message, params))
        'Trace.Assert(condition, String.Format(message, params))
        If Not condition Then Throw New OrmManagerException(String.Format(message, params))
    End Sub

#End Region

#Region " Many2Many "
#If OLDM2M Then
    Public Function GetM2MList(Of T As {IKeyEntity, New})(ByVal obj As _IKeyEntity, ByVal direct As String) As CachedM2MRelation
        Return FindM2MReturnKeys(Of T)(obj, direct).First.Entry
    End Function
#End If

#If Not ExcludeFindMethods Then
    Public Function FindDistinct(Of T As {IKeyEntity, New})(ByVal relation As M2MRelationDesc, _
        ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)

        Dim key As String = "distinct" & _schema.GetEntityKey(GetContextInfo, GetType(T))

        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            'key &= criteria.Filter(GetType(T)).GetStaticString(_schema)
            key &= criteria.Filter().GetStaticString(_schema, GetContextInfo)
        End If

        If relation IsNot Nothing Then
            If relation.ConnectedType IsNot Nothing Then
                key &= relation.ConnectedType.ToString & relation.Column
            Else
                key &= relation.Table.RawName & relation.Column
            End If
        End If

        key &= GetStaticKey()

        Dim dic As IDictionary = GetDic(_cache, key)

        Dim id As String = GetType(T).ToString
        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            'id &= criteria.Filter(GetType(T))._ToString
            id &= criteria.Filter()._ToString
        End If

        'If relation IsNot Nothing Then
        '    id &= relation.Table.RawName & relation.Column
        'End If

        Dim sync As String = id & GetStaticKey()

        'CreateDepends(filter, key, id)

        Dim del As ICacheItemProvoder(Of T) = GetCustDelegate(Of T)(relation, GetFilter(criteria, GetType(T)), sort, key, id)
        'Dim ce As CachedItem = GetFromCache(Of T)(dic, sync, id, withLoad, del)
        Dim s As Boolean = True
        Dim r As ReadOnlyList(Of T) = CType(GetResultset(Of T)(withLoad, dic, id, sync, del, s), Global.Worm.ReadOnlyList(Of T))
        If Not s Then
            Assert(_externalFilter IsNot Nothing, "GetResultset should fail only when external filter specified")
            Using ac As New ApplyCriteria(Me, Nothing)
                Dim c As Condition.ConditionConstructor = New Condition.ConditionConstructor
                c.AddFilter(del.Filter).AddFilter(ac.oldfilter)
                r = FindDistinct(Of T)(relation, New PredicateLink(c), sort, withLoad)
            End Using
        End If
        Return r
    End Function
#End If

    Protected Friend Function GetM2MKey(ByVal tt1 As Type, ByVal tt2 As Type, ByVal direct As String) As String
        If String.IsNullOrEmpty(direct) Then
            direct = M2MRelationDesc.DirKey
        End If
        Return _schema.GetEntityKey(tt1) & Const_JoinStaticString & direct & " - new version - " & _schema.GetEntityKey(tt2) & "$" & GetStaticKey()
    End Function

#If Not ExcludeFindMethods Then

    Protected Friend Function FindMany2Many2(Of T As {IKeyEntity, New})(ByVal obj As _IKeyEntity, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal direct As String, ByVal withLoad As Boolean, Optional ByVal top As Integer = -1) As ReadOnlyList(Of T)
        '    Dim p As Pair(Of M2MCache, Boolean) = FindM2M(Of T)(obj, direct, criteria, sort, withLoad)
        '    'Return p.First.GetObjectList(Of T)(Me, withLoad, p.Second.Created)
        '    return GetResultset(of T)(withload,dic,
        Dim tt1 As Type = obj.GetType
        Dim tt2 As Type = GetType(T)

        Dim key As String = GetM2MKey(tt1, tt2, direct)
        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            'key &= criteria.Filter(tt2).GetStaticString(_schema)
            key &= criteria.Filter().GetStaticString(_schema, GetContextInfo)
        End If

        Dim dic As IDictionary = GetDic(_cache, key)

        Dim id As String = obj.Identifier.ToString
        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            'id &= criteria.Filter(tt2)._ToString
            id &= criteria.Filter()._ToString
        End If

        'CreateM2MDepends(filter, key, id)

        Dim del As ICacheItemProvoder(Of T) = Nothing
        If top > 0 Then
            id &= "top" & top
            del = GetCustDelegate(Of T)(obj, GetFilter(criteria, tt2), sort, New QueryAspect() {StmtGenerator.CreateTopAspect(top)}, id, key, direct)
        Else
            del = GetCustDelegate(Of T)(obj, GetFilter(criteria, tt2), sort, id, key, direct)
        End If
        Dim s As Boolean = True
        Dim sync As String = GetSync(key, id)
        Dim r As ReadOnlyList(Of T) = CType(GetResultset(Of T)(withLoad, dic, id, sync, del, s), Global.Worm.ReadOnlyList(Of T))
        If Not s Then
            Assert(_externalFilter IsNot Nothing, "GetResultset should fail only when external filter specified")
            Using ac As New ApplyCriteria(Me, Nothing)
                Dim c As Condition.ConditionConstructor = New Condition.ConditionConstructor
                c.AddFilter(del.Filter).AddFilter(ac.oldfilter)
                r = FindMany2Many2(Of T)(obj, New PredicateLink(c), sort, direct, withLoad)
            End Using
        End If
        Return r
    End Function

    Protected Function FindM2M(Of T As {IKeyEntity, New})(ByVal obj As _IKeyEntity, ByVal direct As String, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As Pair(Of M2MCache, Boolean)
        Dim tt1 As Type = obj.GetType
        Dim tt2 As Type = GetType(T)

        Dim key As String = GetM2MKey(tt1, tt2, direct)
        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            'key &= criteria.Filter(tt2).GetStaticString(_schema)
            key &= criteria.Filter().GetStaticString(_schema, GetContextInfo)
        End If

        Dim dic As IDictionary = GetDic(_cache, key)

        Dim id As String = obj.Identifier.ToString
        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            'id &= criteria.Filter(tt2)._ToString
            id &= criteria.Filter()._ToString
        End If

        Dim sync As String = GetSync(key, id)

        'CreateM2MDepends(filter, key, id)

        Dim del As ICacheItemProvoder(Of T) = GetCustDelegate(Of T)(obj, GetFilter(criteria, tt2), sort, id, key, direct)
        Dim m As M2MCache = CType(GetFromCache(Of T)(dic, sync, id, withLoad, del), M2MCache)
        Dim p As New Pair(Of M2MCache, Boolean)(m, del.Created)
        Return p
    End Function
#End If
#If OLDM2M Then
    Protected Function FindM2MReturnKeysNonGeneric(ByVal mainobj As IKeyEntity, ByVal t As Type, ByVal direct As String) As Pair(Of M2MCache, Pair(Of String))
        Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic
        'Dim pm As New Reflection.ParameterModifier(6)
        'pm(5) = True
        Dim types As Type() = New Type() {GetType(_IKeyEntity), GetType(String)}
        Dim o() As Object = New Object() {mainobj, direct}
        'Dim m As M2MCache = CType(GetType(OrmManager).InvokeMember("FindM2M", Reflection.BindingFlags.InvokeMethod Or Reflection.BindingFlags.NonPublic, _
        '    Nothing, Me, o, New Reflection.ParameterModifier() {pm}, Nothing, Nothing), M2MCache)
        Dim mi As Reflection.MethodInfo = GetType(OrmManager).GetMethod("FindM2MReturnKeys", flags, Nothing, Reflection.CallingConventions.Any, types, Nothing)
        Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
        Dim p As Pair(Of M2MCache, Pair(Of String)) = CType(mi_real.Invoke(Me, flags, Nothing, o, Nothing), Pair(Of M2MCache, Pair(Of String)))
        Return p
    End Function

    Protected Function FindM2MReturnKeys(Of T As {IKeyEntity, New})(ByVal obj As _IKeyEntity, ByVal direct As String) As Pair(Of M2MCache, Pair(Of String))
        Dim tt1 As Type = obj.GetType
        Dim tt2 As Type = GetType(T)

        Dim key As String = GetM2MKey(tt1, tt2, direct)

        Dim dic As IDictionary = GetDic(_cache, key)

        Dim id As String = obj.Identifier.ToString

        Dim sync As String = GetSync(key, id)

        'CreateM2MDepends(filter, key, id)

        Dim del As ICacheItemProvoder(Of T) = GetCustDelegate(Of T)(obj, Nothing, Nothing, id, key, direct)
        Dim m As M2MCache = CType(GetFromCache(Of T)(dic, sync, id, False, del), M2MCache)
        Dim p As New Pair(Of M2MCache, Pair(Of String))(m, New Pair(Of String)(key, id))
        Return p
    End Function

    Protected Friend Sub M2MCancel(ByVal mainobj As _IKeyEntity, ByVal t As Type)
        For Each o As Pair(Of M2MCache, Pair(Of String, String)) In Cache.GetM2MEntries(mainobj, Nothing)
            If o.First.Entry.SubType Is t Then
                o.First.Entry.Reject(Me, True)
            End If
        Next
    End Sub
#End If
#If OLDM2M Then
    Protected Friend Sub M2MDelete(ByVal mainobj As _IKeyEntity, ByVal subobj As _IKeyEntity, ByVal direct As String)
        If mainobj Is Nothing Then
            Throw New ArgumentNullException("mainobj")
        End If

        If subobj Is Nothing Then
            Throw New ArgumentNullException("subobj")
        End If

        M2MDeleteInternal(mainobj, subobj, direct)

        If mainobj.GetType Is subobj.GetType Then
            M2MDeleteInternal(subobj, mainobj, M2MRelationDesc.GetRevKey(direct))
        Else
            M2MDeleteInternal(subobj, mainobj, direct)
        End If
    End Sub
#End If

#If OLDM2M Then
    Protected Friend Sub M2MDelete(ByVal mainobj As _IKeyEntity, ByVal t As Type, ByVal direct As String)
        Dim m As M2MCache = FindM2MNonGeneric(mainobj, t, direct).First
        For Each id As Object In m.Entry.Current
            'm.Entry.Delete(id)
            M2MDelete(mainobj, CType(GetKeyEntityFromCacheOrCreate(id, t), _IKeyEntity), direct)
        Next
    End Sub


    Protected Function M2MSave(ByVal mainobj As _IKeyEntity, ByVal t As Type, ByVal direct As String) As AcceptState2
        Invariant()

        If mainobj Is Nothing Then
            Throw New ArgumentNullException("mainobj parameter cannot be nothing")
        End If

        If t Is Nothing Then
            Throw New ArgumentNullException("t parameter cannot be nothing")
        End If

        'Dim tt1 As Type = mainobj.GetType
        'Dim tt2 As Type = t

        For Each o As Pair(Of M2MCache, Pair(Of String, String)) In Cache.GetM2MEntries(mainobj, Nothing)
            Dim m2me As M2MCache = o.First
            If m2me Is Nothing Then
                Throw New OrmManagerException(String.Format("M2MCache entry is nothing for key:[{0}] and id:[{1}]. Quering type {2} for {3}; direct={4}", o.Second.First, o.Second.Second, t, mainobj.ObjName, direct))
            End If
            If m2me.Entry.SubType Is t AndAlso m2me.Filter Is Nothing AndAlso m2me.Entry.HasChanges AndAlso m2me.Entry.Key = direct Then
                Using SyncHelper.AcquireDynamicLock(GetSync(o.Second.First, o.Second.Second))
                    Dim sv As M2MRelation = m2me.Entry.PrepareSave(Me)
                    If sv IsNot Nothing Then
                        M2MSave(mainobj, t, direct, sv)
                        m2me.Entry.Saved = True
                        m2me.Entry._savedIds.AddRange(sv.Added)
                    End If
                    'Return New OrmBase.AcceptState2(m2me, o.Second.First, o.Second.Second)
                    Dim acs As AcceptState2 = mainobj.GetAccept(m2me)
                    If acs Is Nothing Then
                        Throw New InvalidOperationException("Accept state must exist")
                    End If
                    Return acs
                End Using
            End If
        Next
        Return Nothing

    End Function
#End If

    Protected Sub M2MUpdate(ByVal obj As _ISinglePKEntity, ByVal oldId As Object)
        If oldId IsNot Nothing Then

#If OLDM2M Then
            For Each o As Pair(Of M2MCache, Pair(Of String, String)) In Cache.GetM2MEntries(obj, obj.GetOldName(oldId))
                Dim key As String = o.Second.First
                Dim id As String = o.Second.Second
                Dim m As M2MCache = o.First
                Dim dic As IDictionary = GetDic(Cache, key)
                dic.Remove(id)
                m.Entry.MainId = obj.Identifier

                id = obj.Identifier.ToString
                If m.Filter IsNot Nothing Then
                    id &= m.Filter._ToString
                End If
                dic.Add(id, m)
            Next

            Cache.UpdateM2MEntries(obj, oldId, obj.GetOldName(oldId))
            Dim tt1 As Type = obj.GetType

            For Each r As M2MRelationDesc In _schema.GetM2MRelations(obj.GetType)

                Dim key As String = GetM2MKey(tt1, r.Entity.GetRealType(MappingEngine), r.Key)
                Dim dic As IDictionary = GetDic(_cache, key)
                Dim id As String = obj.Identifier.ToString
                'Dim sync As String = GetSync(key, id)

                If dic.Contains(id) Then
                    Dim m As M2MCache = CType(dic(id), M2MCache)

                    For Each oid As Integer In m.Entry.Current
                        Dim o As _IKeyEntity = CType(GetKeyEntityFromCacheOrCreate(oid, r.Entity.GetRealType(MappingEngine), False), _IKeyEntity)
                        M2MSubUpdate(o, obj, oldId, obj.GetType)
                    Next
                End If
            Next
#End If

            Dim c As OrmCache = TryCast(_cache, OrmCache)
            If c IsNot Nothing Then
                For Each rl As Relation In obj.GetAllRelation
                    Dim el As M2MRelation = TryCast(rl, M2MRelation)
                    If el IsNot Nothing Then
                        For Each rm As M2MRelation In el.GetRevert(Me)
                            rm.Update(obj, oldId)
                        Next
                    End If
                    'Dim p As Pair(Of String) = _cache.RemoveM2MQuery(el)
                    'If el IsNot Nothing Then c.RemoveM2MQueries(el)

                    'For Each o As IOrmBase In el.Added
                    '    'Dim o As _IOrmBase = CType(GetOrmBaseFromCacheOrCreate(id, el.SubType), _IOrmBase)
                    '    Dim oel As EditableListBase = o.GetRelation(tt1, el.Key)
                    '    oel.Added.Remove(oldId)
                    '    oel.Added.Add(obj.Identifier)
                    'Next

                    'el.MainId = obj.Identifier

                    '_cache.AddM2MQuery(el, p.First, p.Second)
                    'Dim dic As IDictionary = CType(_cache.Filters(p.First), System.Collections.IDictionary)
                    'If dic IsNot Nothing Then
                    '    dic.Remove(p.Second)
                    'End If
                    '_cache.RemoveEntry(p)
                Next
            End If
        End If
    End Sub

#If OLDM2M Then
    Protected Sub M2MSubUpdate(ByVal obj As _IKeyEntity, ByVal obj_ As IKeyEntity, ByVal oldId As Object, ByVal t As Type)
        For Each o As Pair(Of M2MCache, Pair(Of String, String)) In Cache.GetM2MEntries(obj, Nothing)
            Dim m As M2MCache = o.First
            If m.Entry.SubType Is t AndAlso m.Entry.HasAdded Then
                If m.Filter Is Nothing Then
                    m.Entry.Update(obj_, oldId)
                Else
                    Dim dic As IDictionary = GetDic(Cache, o.Second.First)
                    dic.Remove(o.Second.Second)
                End If
            End If
        Next
    End Sub

    Protected Friend Sub M2MAdd(ByVal mainobj As _IKeyEntity, ByVal subobj As _IKeyEntity, ByVal direct As String)
        If mainobj Is Nothing Then
            Throw New ArgumentNullException("mainobj")
        End If

        If subobj Is Nothing Then
            Throw New ArgumentNullException("subobj")
        End If

        M2MAddInternal(mainobj, subobj, direct)

        If mainobj.GetType Is subobj.GetType Then
            M2MAddInternal(subobj, mainobj, M2MRelationDesc.GetRevKey(direct))
        Else
            M2MAddInternal(subobj, mainobj, direct)
        End If
    End Sub
#End If
#If OLDM2M Then
    Protected Friend Function FindM2MNonGeneric(ByVal mainobj As IKeyEntity, ByVal tt2 As Type, ByVal direct As String) As Pair(Of M2MCache, Boolean)
        Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic
        'Dim pm As New Reflection.ParameterModifier(6)
        'pm(5) = True
        Dim types As Type() = New Type() {GetType(_IKeyEntity), GetType(String), GetType(IGetFilter), GetType(Sort), GetType(Boolean)}
        Dim o() As Object = New Object() {mainobj, direct, Nothing, Nothing, False}
        'Dim m As M2MCache = CType(GetType(OrmManager).InvokeMember("FindM2M", Reflection.BindingFlags.InvokeMethod Or Reflection.BindingFlags.NonPublic, _
        '    Nothing, Me, o, New Reflection.ParameterModifier() {pm}, Nothing, Nothing), M2MCache)
        Dim mi As Reflection.MethodInfo = GetType(OrmManager).GetMethod("FindM2M", flags, Nothing, Reflection.CallingConventions.Any, types, Nothing)
        Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {tt2})
        Dim p As Pair(Of M2MCache, Boolean) = CType(mi_real.Invoke(Me, flags, Nothing, o, Nothing), Pair(Of M2MCache, Boolean))
        Return p
    End Function

    Protected Friend Function GetM2MNonGeneric(ByVal obj As IKeyEntity, ByVal tt2 As Type, ByVal direct As String) As M2MCache
        Dim tt1 As Type = obj.GetType

        Dim id As Object = obj.Identifier.ToString

        Return GetM2MNonGeneric(id, tt1, tt2, direct)
    End Function

    Protected Friend Function GetM2MNonGeneric(ByVal id As Object, ByVal tt1 As Type, ByVal tt2 As Type, ByVal direct As String) As M2MCache
        Dim key As String = GetM2MKey(tt1, tt2, direct)

        Dim dic As IDictionary = GetDic(_cache, key)

        Return CType(dic(id), M2MCache)
    End Function

    Protected Sub M2MAddInternal(ByVal mainobj As _IKeyEntity, ByVal subobj As IKeyEntity, ByVal direct As String)
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
        Dim cnt As Integer = m.Entry.Added.Count + m.Entry.Deleted.Count
        Dim check As Boolean = m.Entry.Original.Contains(subobj.Identifier)
#End If
        m.Entry.Add(subobj)
#If DEBUG Then
        Debug.Assert(Not check OrElse (m.Entry.Added.Count + m.Entry.Deleted.Count) <> cnt)
#End If
#If OLDM2M Then
        mainobj.AddAccept(New AcceptState2(m, p.Second.First, p.Second.Second))
#End If
    End Sub

    Protected Sub M2MDeleteInternal(ByVal mainobj As _IKeyEntity, ByVal subobj As IKeyEntity, ByVal direct As String)
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

        m.Entry.Delete(subobj)

        mainobj.AddAccept(New AcceptState2(m, p.Second.First, p.Second.Second))
    End Sub
#End If

#End Region

    Public Shared Function CreateReadOnlyList(ByVal t As Type) As ILoadableList
        Dim rt As Type = Nothing
        If GetType(ISinglePKEntity).IsAssignableFrom(t) Then
            rt = GetType(ReadOnlyList(Of ))
        ElseIf GetType(ICachedEntity).IsAssignableFrom(t) Then
            rt = GetType(ReadOnlyEntityList(Of ))
        Else
            rt = GetType(ReadOnlyObjectList(Of ))
        End If
        Return CType(Activator.CreateInstance(rt.MakeGenericType(New Type() {t})), ILoadableList)
    End Function

    Public Shared Function CreateReadOnlyList(ByVal listType As Type, ByVal l As IEnumerable) As ILoadableList
        Dim rt As Type = Nothing
        If GetType(ISinglePKEntity).IsAssignableFrom(listType) Then
            rt = GetType(ReadOnlyList(Of ))
        ElseIf GetType(ICachedEntity).IsAssignableFrom(listType) Then
            rt = GetType(ReadOnlyEntityList(Of ))
        Else
            rt = GetType(ReadOnlyObjectList(Of ))
        End If
        Return CType(Activator.CreateInstance(rt.MakeGenericType(New Type() {listType}), New Object() {l}), ILoadableList)
    End Function

    Public Shared Function CreateReadOnlyList(ByVal listType As Type, ByVal realType As Type, ByVal l As IEnumerable) As ILoadableList
        Dim rt As Type = Nothing
        If GetType(ISinglePKEntity).IsAssignableFrom(listType) Then
            rt = GetType(ReadOnlyList(Of ))
        ElseIf GetType(ICachedEntity).IsAssignableFrom(listType) Then
            rt = GetType(ReadOnlyEntityList(Of ))
        Else
            rt = GetType(ReadOnlyObjectList(Of ))
        End If
        Return CType(Activator.CreateInstance(rt.MakeGenericType(New Type() {listType}), New Object() {realType, l}), ILoadableList)
    End Function

    Public Shared Function CreateReadOnlyList(ByVal listType As Type, ByVal realType As Type) As ILoadableList
        Dim rt As Type = Nothing
        If GetType(ISinglePKEntity).IsAssignableFrom(listType) Then
            rt = GetType(ReadOnlyList(Of ))
        ElseIf GetType(ICachedEntity).IsAssignableFrom(listType) Then
            rt = GetType(ReadOnlyEntityList(Of ))
        Else
            rt = GetType(ReadOnlyObjectList(Of ))
        End If
        Return CType(Activator.CreateInstance(rt.MakeGenericType(New Type() {listType}), New Object() {realType}), ILoadableList)
    End Function

    Friend Shared Function _CreateReadOnlyList(ByVal t As Type) As IListEdit
        Dim rt As Type = Nothing
        If GetType(ISinglePKEntity).IsAssignableFrom(t) Then
            rt = GetType(ReadOnlyList(Of ))
        ElseIf GetType(ICachedEntity).IsAssignableFrom(t) Then
            rt = GetType(ReadOnlyEntityList(Of ))
        Else
            rt = GetType(ReadOnlyObjectList(Of ))
        End If
        Return CType(Activator.CreateInstance(rt.MakeGenericType(New Type() {t})), IListEdit)
    End Function

    Friend Shared Function _CreateReadOnlyList(ByVal t As Type, ByVal l As IEnumerable) As IListEdit
        Dim rt As Type = Nothing
        If GetType(ISinglePKEntity).IsAssignableFrom(t) Then
            rt = GetType(ReadOnlyList(Of ))
        ElseIf GetType(ICachedEntity).IsAssignableFrom(t) Then
            rt = GetType(ReadOnlyEntityList(Of ))
        Else
            rt = GetType(ReadOnlyObjectList(Of ))
        End If

        Return CType(Activator.CreateInstance(rt.MakeGenericType(New Type() {t}), New Object() {_GetUCFCList(t, l)}), IListEdit)
    End Function

    Private Shared Function _GetUCFCList(ByVal t As Type, ByVal l As IEnumerable) As IEnumerable
        Dim l1 As IEnumerable = l

        If Not GetType(IEnumerable(Of )).MakeGenericType(t).IsAssignableFrom(l.GetType()) Then
            Dim l2 As IList = CType(Activator.CreateInstance(GetType(List(Of )).MakeGenericType(New Type() {t})), IList)
            For Each i As Object In l
                l2.Add(i)
            Next
            l1 = l2
        End If

        Return l1
    End Function

    Friend Shared Function _CreateReadOnlyList(ByVal listType As Type, ByVal realType As Type, ByVal l As IEnumerable) As IListEdit
        Dim rt As Type = Nothing
        If GetType(ISinglePKEntity).IsAssignableFrom(listType) Then
            rt = GetType(ReadOnlyList(Of ))
        ElseIf GetType(ICachedEntity).IsAssignableFrom(listType) Then
            rt = GetType(ReadOnlyEntityList(Of ))
        Else
            rt = GetType(ReadOnlyObjectList(Of ))
        End If
        Return CType(Activator.CreateInstance(rt.MakeGenericType(New Type() {listType}), New Object() {realType, _GetUCFCList(listType, l)}), IListEdit)
    End Function

    Friend Shared Function _CreateReadOnlyList(ByVal listType As Type, ByVal realType As Type) As IListEdit
        Dim rt As Type = Nothing
        If GetType(ISinglePKEntity).IsAssignableFrom(listType) Then
            rt = GetType(ReadOnlyList(Of ))
        ElseIf GetType(ICachedEntity).IsAssignableFrom(listType) Then
            rt = GetType(ReadOnlyEntityList(Of ))
        Else
            rt = GetType(ReadOnlyObjectList(Of ))
        End If
        Return CType(Activator.CreateInstance(rt.MakeGenericType(New Type() {listType}), New Object() {realType}), IListEdit)
    End Function

    'Public Function ApplyFilter(Of T As {_IEntity})(ByVal col As ReadOnlyObjectList(Of T), ByVal filter As IGetFilter) As ReadOnlyObjectList(Of T)
    '    Dim evaluated As Boolean
    '    Dim r As ReadOnlyObjectList(Of T) = ApplyFilter(col, filter, evaluated)
    '    If Not evaluated Then
    '        Throw New InvalidOperationException("Filter is not applyable")
    '    End If
    '    Return r
    'End Function

    Public Function ApplyFilter(Of T As {_IEntity})(ByVal col As ReadOnlyObjectList(Of T), ByVal filter As IGetFilter,
                              joins() As Joins.QueryJoin, objEU As EntityUnion) As ReadOnlyObjectList(Of T)
        Dim evaluated As Boolean
        Dim r As ReadOnlyObjectList(Of T) = ApplyFilter(col, filter, joins, objEU, evaluated)
        If Not evaluated Then
            Throw New InvalidOperationException("Filter is not applyable")
        End If
        Return r
    End Function

    Public Function ApplyFilter(Of T As {_IEntity})(ByVal col As ReadOnlyObjectList(Of T), ByVal filter As IGetFilter, ByRef evaluated As Boolean) As ReadOnlyObjectList(Of T)
        Return ApplyFilter(Of T)(col, filter, Nothing, Nothing, evaluated)
    End Function

    Public Function ApplyFilter(Of T As {_IEntity})(ByVal col As ReadOnlyObjectList(Of T), ByVal filter As IApplyFilter, ByRef evaluated As Boolean) As ReadOnlyObjectList(Of T)
        If filter Is Nothing Then
            evaluated = True
            Return col
        End If
        Return ApplyFilter(Of T)(col, filter.Filter, filter.Joins, filter.RootObjectUnion, evaluated)
    End Function

    Public Function ApplyFilter(Of T As {_IEntity})(ByVal col As ReadOnlyObjectList(Of T), ByVal filter As IGetFilter,
                              joins() As Joins.QueryJoin, objEU As EntityUnion, ByRef evaluated As Boolean) As ReadOnlyObjectList(Of T)
        If filter Is Nothing Then
            evaluated = True
            Return col
        End If

        Dim f As IEntityFilter = TryCast(If(filter Is Nothing, Nothing, filter.Filter), IEntityFilter)
        If f Is Nothing Then
            Return col
        Else
            evaluated = True
            Dim l As IListEdit = _CreateReadOnlyList(GetType(T))
            Dim oschema As IEntitySchema = Nothing
            Dim i As Integer = 0
            For Each o As T In col
                If oschema Is Nothing Then
                    oschema = _schema.GetEntitySchema(o.GetType)
                End If
                Dim er As IEvaluableValue.EvalResult = f.EvalObj(_schema, o, oschema, joins, objEU)
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
            Return CType(l, Global.Worm.ReadOnlyObjectList(Of T))
        End If
    End Function

    Public Function ApplyFilter(ByVal t As Type, ByVal col As IEnumerable, ByVal filter As IGetFilter) As IReadOnlyList
        Dim evaluated As Boolean
        Dim r As IReadOnlyList = ApplyFilter(t, col, filter, evaluated)
        If Not evaluated Then
            Throw New InvalidOperationException("Filter is not applyable")
        End If
        Return r
    End Function

    Public Function ApplyFilter(ByVal t As Type, ByVal col As IEnumerable, ByVal filter As IGetFilter,
                              joins() As Joins.QueryJoin, objEU As EntityUnion) As IReadOnlyList
        Dim evaluated As Boolean
        Dim r As IReadOnlyList = ApplyFilter(t, col, filter, joins, objEU, evaluated)
        If Not evaluated Then
            Throw New InvalidOperationException("Filter is not applyable")
        End If
        Return r
    End Function

    Public Function ApplyFilter(ByVal t As Type, ByVal col As IEnumerable, ByVal filter As IGetFilter, ByRef evaluated As Boolean) As IReadOnlyList
        Return ApplyFilter(t, col, filter, Nothing, Nothing, evaluated)
    End Function

    Public Function ApplyFilter(ByVal t As Type, ByVal col As IEnumerable, ByVal filter As IGetFilter,
                              joins() As Joins.QueryJoin, objEU As EntityUnion, ByRef evaluated As Boolean) As IReadOnlyList
        If filter Is Nothing Then
            evaluated = True
            If GetType(IReadOnlyList).IsAssignableFrom(col.GetType) Then
                Return CType(col, IReadOnlyList)
            Else
                Return _CreateReadOnlyList(t, col)
            End If
        End If

        Dim f As IEntityFilter = TryCast(If(filter Is Nothing, Nothing, filter.Filter), IEntityFilter)
        If f Is Nothing Then
            If GetType(IReadOnlyList).IsAssignableFrom(col.GetType) Then
                Return CType(col, IReadOnlyList)
            Else
                Return _CreateReadOnlyList(t, col)
            End If
        Else
            evaluated = True
            Dim l As IListEdit = _CreateReadOnlyList(t)
            Dim oschema As IEntitySchema = Nothing
            Dim i As Integer = 0
            For Each o As _IEntity In col
                If oschema Is Nothing Then
                    oschema = _schema.GetEntitySchema(o.GetType)
                End If
                Dim er As IEvaluableValue.EvalResult = f.EvalObj(_schema, o, oschema, joins, objEU)
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

    Public Shared Function ApplySort(Of T As {_IEntity})(ByVal c As ICollection(Of T), _
        ByVal s As OrderByClause, ByVal getObj As entityComparer.GetObjectDelegate) As ICollection(Of T)
        If c.Count > 0 Then
            Dim mpe As ObjectMappingEngine = Nothing
            For Each e As _IEntity In c
                mpe = e.GetMappingEngine
                If mpe IsNot Nothing Then Exit For
            Next

            If s IsNot Nothing Then
                'For Each ns As SortExpression In s
                '    If Not ns.CanEvaluate Then
                '        Throw New ObjectMappingException(String.Format("Sort expression of {0} is not supported", ns.Operand.GetType))
                '    End If
                'Next
                If Not s.CanEvaluate(mpe) Then
                    Throw New ObjectMappingException(String.Format("Sort is not evaluable"))
                End If

                Dim l As New List(Of T)(c)
                l.Sort(New EntityComparer(Of T)(s, getObj))
                c = l
            End If
        End If
        Return c
    End Function

    Public Shared Function ApplySort(Of T As {_IEntity})(ByVal c As ICollection(Of T), ByVal s As OrderByClause) As ICollection(Of T)
        Return ApplySort(c, s, Nothing)
    End Function

    Public Shared Function ApplySortT(ByVal c As ICollection, ByVal s As OrderByClause) As ICollection
        If c.Count > 0 Then
            Dim mpe As ObjectMappingEngine = Nothing
            For Each e As _IEntity In c
                mpe = e.GetMappingEngine
                If mpe IsNot Nothing Then Exit For
            Next

            If s IsNot Nothing Then
                'For Each ns As SortExpression In s
                '    If Not ns.CanEvaluate Then
                '        Throw New ObjectMappingException(String.Format("Sort expression of {0} is not supported", ns.Operand.GetType))
                '    End If
                'Next
                If Not s.CanEvaluate(mpe) Then
                    Throw New ObjectMappingException(String.Format("Sort is not evaluable"))
                End If

                Dim l As New ArrayList(c)
                If l.Count > 0 Then
                    l.Sort(New EntityComparer(s))
                    c = l
                End If
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

    Public Function GetLoadedCount(ByVal t As Type, ByVal ids As IList(Of Object)) As Integer
        Dim r As Integer = 0
        'Dim dic As IDictionary = GetDictionary(t)
        For Each id As Integer In ids
            Dim o As ISinglePKEntity = GetKeyEntityFromCacheOrCreate(id, t, False)
            If o.IsLoaded OrElse o.ObjectState = ObjectState.Created Then
                r += 1
            End If
        Next
        Return r
    End Function

    Public Function GetLoadedCount(Of T As {ICachedEntity})(ByVal col As IEnumerable(Of T)) As Integer
        Dim r As Integer = 0
        'If GetType(IOrmBase).IsAssignableFrom(GetType(T)) Then
        For Each o As ICachedEntity In col
            If o.IsLoaded OrElse o.ObjectState = ObjectState.Created Then
                r += 1
            End If
        Next
        'End If
        Return r
    End Function

    Public Function GetKeyFromPK(Of T As {New, ISinglePKEntity})(ByVal id As Object) As Integer
        Dim o As T = CreateKeyEntity(Of T)(id)
        Return o.Key
    End Function

    Public Function GetLoadedCount(Of T As {New, ISinglePKEntity})(ByVal ids As IList(Of Object)) As Integer
        Dim r As Integer = 0
        'Dim dic As IDictionary(Of Object, T) = GetDictionary(Of T)()
        For Each id As Object In ids
            Dim o As ISinglePKEntity = GetKeyEntityFromCacheOrCreate(Of T)(id, False)
            If o.IsLoaded OrElse o.ObjectState = ObjectState.Created Then
                'If dic.ContainsKey(GetKeyFromPK(Of T)(id)) Then
                r += 1
            End If
        Next
        Return r
    End Function

#Region " Search "

#If Not ExcludeFindMethods Then

    Public Function Search(Of T As {IKeyEntity, New})(ByVal [string] As String) As ReadOnlyList(Of T)
        Invariant()

        If [string] IsNot Nothing AndAlso [string].Length > 0 Then
            'Dim ss() As String = Split4FullTextSearch([string], GetSearchSection)
            Return Search(Of T)(GetType(T), [string], Nothing, Nothing)
        End If
        Return New ReadOnlyList(Of T)()
    End Function

    Public Function Search(Of T As {IKeyEntity, New})(ByVal type2search As Type, ByVal [string] As String, ByVal contextKey As Object) As ReadOnlyList(Of T)
        Return Search(Of T)(type2search, [string], Nothing, contextKey)
    End Function

    Public Function Search(Of T As {IKeyEntity, New})(ByVal type2search As Type, ByVal [string] As String, _
        ByVal sort As Sort, ByVal contextKey As Object) As ReadOnlyList(Of T)
        Dim selectType As Type = GetType(T)
        'If selectType IsNot type2search Then
        '    Dim field As String = _schema.GetJoinFieldNameByType(selectType, type2search, Nothing)

        '    If String.IsNullOrEmpty(field) Then
        '        Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2search))
        '    End If

        If [string] IsNot Nothing AndAlso [string].Length > 0 Then
            'Dim ss() As String = Split4FullTextSearch()
            'Dim join As QueryJoin = MakeJoin(type2search, selectType, field, FilterOperation.Equal, JoinType.Join, True)
            Return Search(Of T)(type2search, contextKey, sort, Nothing, New FtsDefaultFormatter([string], GetSearchSection))
            'End If
            'Return New ReadOnlyList(Of T)()
        Else
            Return Search(Of T)([string], sort, contextKey)
        End If
    End Function

    Public Function Search(Of T As {IKeyEntity, New})(ByVal [string] As String, ByVal sort As Sort, ByVal contextKey As Object) As ReadOnlyList(Of T)
        Return Search(Of T)([string], sort, contextKey, Nothing)
    End Function

    Public Function Search(Of T As {IKeyEntity, New})(ByVal [string] As String, ByVal sort As Sort, _
        ByVal contextKey As Object, ByVal filter As IFilter) As ReadOnlyList(Of T)
        Invariant()

        If [string] IsNot Nothing AndAlso [string].Length > 0 Then
            Return Search(Of T)(GetType(T), contextKey, sort, filter, New FtsDefaultFormatter([string], GetSearchSection))
        End If
        Return New ReadOnlyList(Of T)()
    End Function

    Public Function Search(Of T As {IKeyEntity, New})(ByVal [string] As String, ByVal sort As Sort, _
        ByVal contextKey As Object, ByVal filter As IFilter, ByVal ftsText As String, _
        ByVal limit As Integer, ByVal del As FtsDefaultFormatter.ValueForSearchDelegate) As ReadOnlyList(Of T)
        Invariant()

        If [string] IsNot Nothing AndAlso [string].Length > 0 Then
            Return SearchEx(Of T)(GetType(T), contextKey, sort, filter, ftsText, limit, New FtsDefaultFormatter([string], GetSearchSection, del))
        End If
        Return New ReadOnlyList(Of T)()
    End Function

    Public Function Search(Of T As {IKeyEntity, New})(ByVal [string] As String, ByVal sort As Sort, _
       ByVal contextKey As Object, ByVal filter As IFilter, ByVal ftsText As String, _
       ByVal limit As Integer, ByVal frmt As IFtsStringFormatter) As ReadOnlyList(Of T)
        Invariant()

        If [string] IsNot Nothing AndAlso [string].Length > 0 Then
            Return SearchEx(Of T)(GetType(T), contextKey, sort, filter, ftsText, limit, frmt)
        End If
        Return New ReadOnlyList(Of T)()
    End Function

#End If

    Public Shared Function Split4FullTextSearch(ByVal str As String, ByVal sectionName As String) As String()
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

    Public Shared Function Split4FullTextSearchInternal(ByVal str As String) As String()
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
    Public Function LoadObjects(Of T As {ICachedEntity})(ByVal objs As ReadOnlyEntityList(Of T), ByVal fields() As String, _
        ByVal start As Integer, ByVal length As Integer) As ReadOnlyEntityList(Of T)

        Dim col As ReadOnlyEntityList(Of T) = objs.LoadObjects(start, length)

        If fields Is Nothing OrElse fields.Length = 0 Then
            Return col
        End If

        Dim prop_objs(fields.Length - 1) As IListEdit

        'Dim lt As Type = GetType(ReadOnlyEntityList(Of ))
        Dim oschema As IEntitySchema = _schema.GetEntitySchema(GetType(T))

        For Each o As T In col
            For i As Integer = 0 To fields.Length - 1
                'Dim obj As OrmBase = CType(ObjectSchema.GetFieldValue(o, fields(i)), OrmBase)
                Dim obj As IEntity = CType(ObjectMappingEngine.GetPropertyValue(o, fields(i), oschema, Nothing), IEntity)
                If obj IsNot Nothing Then
                    If prop_objs(i) Is Nothing Then
                        'prop_objs(i) = CType(Activator.CreateInstance(lt.MakeGenericType(obj.GetType)), IListEdit)
                        prop_objs(i) = _CreateReadOnlyList(obj.GetType)
                    End If
                    prop_objs(i).Add(obj)
                End If
            Next
        Next

        'If _LoadObjectsMI Is Nothing Then
        '    Dim mis() As Reflection.MemberInfo = Me.GetType.GetMember("LoadObjects")

        '    For Each mri_ As Reflection.MemberInfo In mis
        '        Dim mi_ As Reflection.MethodInfo = TryCast(mri_, Reflection.MethodInfo)
        '        If mi_ IsNot Nothing AndAlso mi_.GetParameters.Length = 1 Then
        '            _LoadObjectsMI = mi_
        '            Exit For
        '        End If
        '    Next

        '    If _LoadObjectsMI Is Nothing Then
        '        Throw New OrmManagerException("Cannot find method LoadObjects")
        '    End If
        'End If

        For Each po As IList In prop_objs
            If po IsNot Nothing AndAlso po.Count > 0 Then
                'Dim tt As Type = po(0).GetType
                '_LoadObjectsMI.MakeGenericMethod(New Type() {tt}).Invoke(Me, Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public, Nothing, _
                '    New Object() {po}, Nothing)
                CType(po, ILoadableList).LoadObjects()
            End If
        Next

        Return col
    End Function

    'Protected Function LoadObjectsInternal(Of T As {IKeyEntity, New})(ByVal objs As ReadOnlyList(Of T), ByVal start As Integer, ByVal length As Integer, ByVal remove_not_found As Boolean, ByVal columns As Generic.List(Of EntityPropertyAttribute), ByVal withLoad As Boolean) As ReadOnlyList(Of T)
    '    Return LoadObjectsInternal(Of T, T)(objs, start, length, remove_not_found, columns, withLoad)
    'End Function

    'Public Function LoadObjects(Of T As {IKeyEntity, New})(ByVal objs As ReadOnlyList(Of T), ByVal start As Integer, ByVal length As Integer, ByVal columns As List(Of EntityPropertyAttribute)) As ReadOnlyList(Of T)
    '    Return LoadObjectsInternal(objs, start, length, True, columns, _schema.GetSortedFieldList(GetType(T)).Count = columns.Count)
    'End Function

    'Public Function LoadObjects(Of T As {IKeyEntity, New})(ByVal objs As ReadOnlyList(Of T), ByVal start As Integer, ByVal length As Integer) As ReadOnlyList(Of T)
    '    Return LoadObjectsInternal(objs, start, length, True)
    'End Function

    'Public Function LoadObjects(Of T As {IKeyEntity, New})(ByVal objs As ReadOnlyList(Of T)) As ReadOnlyList(Of T)
    '    Return LoadObjectsInternal(objs, 0, objs.Count, True)
    'End Function

    'Public Function LoadObjects(Of T As {IKeyEntity, New}, T2 As IKeyEntity)(ByVal objs As ReadOnlyList(Of T2), ByVal start As Integer, ByVal length As Integer) As ReadOnlyList(Of T2)
    '    Return LoadObjectsInternal(Of T, T2)(objs, start, length, True, _schema.GetSortedFieldList(GetType(T)), True)
    'End Function

    'Public Function LoadObjects(Of T As {IKeyEntity, New}, T2 As IKeyEntity)(ByVal objs As ReadOnlyList(Of T2)) As ReadOnlyList(Of T2)
    '    Return LoadObjectsInternal(Of T, T2)(objs, 0, objs.Count, True, _schema.GetSortedFieldList(GetType(T)), True)
    'End Function

    'Public Function LoadObjects(Of T2 As IKeyEntity)(ByVal realType As Type, ByVal objs As ReadOnlyList(Of T2), ByVal start As Integer, ByVal length As Integer) As ReadOnlyList(Of T2)
    '    Return LoadObjectsInternal(Of T2)(realType, objs, start, length, True, _schema.GetSortedFieldList(realType), True)
    'End Function

    'Public Function LoadObjects(Of T2 As IKeyEntity)(ByVal realType As Type, ByVal objs As ReadOnlyList(Of T2)) As ReadOnlyList(Of T2)
    '    Return LoadObjectsInternal(Of T2)(realType, objs, 0, objs.Count, True, _schema.GetSortedFieldList(realType), True)
    'End Function

    'Public Overridable Function ConvertIds2Objects(ByVal t As Type, ByVal ids As ICollection(Of Object), ByVal check As Boolean) As ICollection
    '    Dim self_t As Type = Me.GetType
    '    Dim mis() As Reflection.MemberInfo = self_t.GetMember("ConvertIds2Objects", Reflection.MemberTypes.Method, _
    '        Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public)
    '    For Each mmi As Reflection.MemberInfo In mis
    '        If TypeOf (mmi) Is Reflection.MethodInfo Then
    '            Dim mi As Reflection.MethodInfo = CType(mmi, Reflection.MethodInfo)
    '            If mi.IsGenericMethod AndAlso mi.GetParameters.Length = 2 Then
    '                mi = mi.MakeGenericMethod(New Type() {t})
    '                Return CType(mi.Invoke(Me, New Object() {ids, check}), System.Collections.ICollection)
    '            End If
    '        End If
    '    Next
    '    Throw New InvalidOperationException(String.Format("Method {0} not found", "ConvertIds2Objects"))
    'End Function

    'Public Overridable Function ConvertIds2Objects(ByVal t As Type, ByVal ids As ICollection(Of Object), ByVal start As Integer, ByVal length As Integer, ByVal check As Boolean) As ICollection
    '    Dim self_t As Type = Me.GetType
    '    Dim mis() As Reflection.MemberInfo = self_t.GetMember("ConvertIds2Objects", Reflection.MemberTypes.Method, _
    '        Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public)
    '    For Each mmi As Reflection.MemberInfo In mis
    '        If TypeOf (mmi) Is Reflection.MethodInfo Then
    '            Dim mi As Reflection.MethodInfo = CType(mmi, Reflection.MethodInfo)
    '            If mi.IsGenericMethod AndAlso mi.GetParameters.Length = 4 Then
    '                mi = mi.MakeGenericMethod(New Type() {t})
    '                Return CType(mi.Invoke(Me, New Object() {ids, start, length, check}), System.Collections.ICollection)
    '            End If
    '        End If
    '    Next
    '    Throw New InvalidOperationException(String.Format("Method {0} not found", "ConvertIds2Objects"))
    'End Function

    'Public Overridable Function ConvertIds2Objects(Of T As {IKeyEntity, New})(ByVal ids As ICollection(Of Object), ByVal check As Boolean) As ReadOnlyList(Of T)
    '    Dim arr As New ReadOnlyList(Of T)

    '    If Not check Then
    '        Dim type As Type = GetType(T)

    '        For Each id As Object In ids
    '            Dim obj As T = GetKeyEntityFromCacheOrCreate(Of T)(id, True)

    '            If obj IsNot Nothing Then
    '                CType(arr, IListEdit).Add(obj)
    '            ElseIf _cache.NewObjectManager IsNot Nothing Then
    '                obj = CType(_cache.NewObjectManager.GetNew(type, obj.GetPKValues), T)
    '                If obj IsNot Nothing Then CType(arr, IListEdit).Add(obj)
    '            End If
    '        Next
    '    Else
    '        Dim r As ReadOnlyList(Of T) = ConvertIds2Objects(Of T)(ids, False)
    '        arr = LoadObjects(Of T)(r, 0, r.Count, New List(Of EntityPropertyAttribute)(New EntityPropertyAttribute() { _
    '            _schema.GetPrimaryKeys(GetType(T))(0) _
    '            }))
    '    End If
    '    Return arr
    'End Function

    'Public Overridable Function ConvertIds2Objects(Of T As {IKeyEntity, New})(ByVal ids As IList(Of Object), _
    '    ByVal start As Integer, ByVal length As Integer, ByVal check As Boolean) As ReadOnlyList(Of T)

    '    Dim arr As New ReadOnlyList(Of T)

    '    If Not check Then
    '        If start < ids.Count Then
    '            Dim type As Type = GetType(T)
    '            length = Math.Min(length + start, ids.Count)
    '            For i As Integer = start To length - 1
    '                Dim id As Object = ids(i)
    '                Dim obj As T = GetKeyEntityFromCacheOrCreate(Of T)(id, True)

    '                If obj IsNot Nothing Then
    '                    CType(arr, IListEdit).Add(obj)
    '                ElseIf _cache.NewObjectManager IsNot Nothing Then
    '                    obj = CType(_cache.NewObjectManager.GetNew(type, obj.GetPKValues), T)
    '                    If obj IsNot Nothing Then CType(arr, IListEdit).Add(obj)
    '                End If
    '            Next
    '        End If
    '    Else
    '        Dim r As ReadOnlyList(Of T) = ConvertIds2Objects(Of T)(ids, start, length, False)
    '        arr = LoadObjects(Of T)(r, 0, r.Count, New List(Of EntityPropertyAttribute)(New EntityPropertyAttribute() { _
    '            _schema.GetPrimaryKeys(GetType(T))(0) _
    '            }))
    '    End If
    '    Return arr
    'End Function

    'Public Function LoadObjectsIds(Of T As {ICachedEntity})(ByVal tt As Type, ByVal ids As IList(Of Object), ByVal start As Integer, ByVal length As Integer) As ReadOnlyEntityList(Of T)
    '    Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public
    '    Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("LoadObjectsIds", flags, Nothing, Reflection.CallingConventions.Any, New Type() {GetType(IList(Of Object)), GetType(Integer), GetType(Integer)}, Nothing)
    '    Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {tt})
    '    Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {ids, start, length}, Nothing), ReadOnlyEntityList(Of T))
    'End Function

    'Public Function LoadObjectsIds(Of T As {IKeyEntity, New})(ByVal ids As ICollection(Of Object)) As ReadOnlyList(Of T)
    '    Return LoadObjects(Of T)(ConvertIds2Objects(Of T)(ids, False))
    'End Function

    'Public Function LoadObjectsIds(Of T As {IKeyEntity, New})(ByVal ids As IList(Of Object), ByVal start As Integer, ByVal length As Integer) As ReadOnlyList(Of T)
    '    Return LoadObjects(Of T)(ConvertIds2Objects(Of T)(ids, start, length, False))
    'End Function

    Protected Friend Shared Function _GetDic(ByVal cache As CacheBase, ByVal key As String) As IDictionary
        'Dim dic As IDictionary = CType(cache.Filters(key), IDictionary)
        'If dic Is Nothing Then
        '    Using SyncHelper.AcquireDynamicLock(key)
        '        dic = CType(cache.Filters(key), IDictionary)
        '        If dic Is Nothing Then
        '            dic = cache.CreateResultsetsDictionary() 'Hashtable.Synchronized(New Hashtable)
        '            cache.Filters.Add(key, dic)
        '        End If
        '    End Using
        'End If
        'Return dic
        Return cache.GetDictionary(key)
    End Function

    Protected Friend Function GetDic(ByVal cache As CacheBase, ByVal key As String) As IDictionary
        Dim b As Boolean
        Return GetDic(cache, key, b)
    End Function

    Protected Friend Function GetDic(ByVal cache As CacheBase, ByVal key As String, ByRef created As Boolean) As IDictionary
        'Dim dic As IDictionary = CType(cache.Filters(key), IDictionary)
        'created = False
        'If dic Is Nothing Then
        '    Using SyncHelper.AcquireDynamicLock(key)
        '        dic = CType(cache.Filters(key), IDictionary)
        '        If dic Is Nothing Then
        '            dic = cache.CreateResultsetsDictionary(_list) 'Hashtable.Synchronized(New Hashtable)
        '            cache.Filters.Add(key, dic)
        '            created = True
        '        End If
        '    End Using
        'End If
        'Return dic
        Return cache.GetDictionary(key, _list, created)
    End Function

    'Protected Shared Function GetDics(ByVal cache As OrmCache, ByVal key As String, ByVal postfix As String) As IEnumerable(Of IDictionary)
    '    Dim dics As New List(Of IDictionary)
    '    For Each de As DictionaryEntry In cache.Filters
    '        Dim k As String = CStr(de.Key)
    '        If k <> key AndAlso k.StartsWith(key) AndAlso (String.IsNullOrEmpty(postfix) OrElse k.EndsWith(postfix)) Then
    '            dics.Add(CType(de.Value, System.Collections.IDictionary))
    '        End If
    '    Next
    '    Return dics
    'End Function

    Private Shared Function GetFilter(ByVal criteria As IGetFilter, ByVal t As Type) As IFilter
        If criteria IsNot Nothing Then
            Return criteria.Filter()
        End If
        Return Nothing
    End Function

    Protected Friend Overridable Function GetContextInfo() As Object
        Return Nothing
    End Function

    Public Function SaveChanges(ByVal obj As _ICachedEntity, ByVal AcceptChanges As Boolean) As Boolean
        Using _cache.SyncSave

            Dim oldObj As ICachedEntity = Nothing
            Dim hasErrors As Boolean = True
            Try
                If _cache.IsReadonly Then
                    Throw New OrmManagerException("Cache is readonly")
                End If

                Dim v As _ICachedEntityEx = TryCast(obj, _ICachedEntityEx)
                If v IsNot Nothing Then
                    Select Case obj.ObjectState
                        Case ObjectState.Created, ObjectState.NotFoundInSource
                            v.ValidateNewObject(Me)
                        Case ObjectState.Modified
                            v.ValidateUpdate(Me)
                        Case ObjectState.Deleted
                            v.ValidateDelete(Me)
                    End Select
                End If

                Dim t As Type = obj.GetType
                Dim orm As _ISinglePKEntity = TryCast(obj, _ISinglePKEntity)
                'Using obj.GetSyncRoot
                Using GetSyncForSave(t, obj)
                    Dim old_id As Object = Nothing
                    Dim sa As SaveAction
                    Dim state As ObjectState = obj.ObjectState
                    If state = ObjectState.Created Then
                        If orm IsNot Nothing Then
                            old_id = orm.Identifier
                        End If
                        sa = SaveAction.Insert
                    End If

                    If state = ObjectState.Deleted Then
                        sa = SaveAction.Delete
                    End If

                    'Dim old_state As ObjectState = state
                    Dim hasNew As Boolean = False
                    Dim err As Boolean = True, ttt As Boolean
                    Dim uc As IUndoChanges = TryCast(obj, IUndoChanges)
                    Try
                        Dim processedType As New List(Of Type)

                        If sa = SaveAction.Delete Then
                            If orm IsNot Nothing Then
                                Dim toDel As New List(Of M2MRelation)
                                For Each r As M2MRelationDesc In MappingEngine.GetM2MRelations(t)
#If OLDM2M Then
                                Dim acs As AcceptState2 = Nothing
#End If

                                    If r.ConnectedType Is Nothing Then
                                        Dim cmd As RelationCmd = CType(orm, IRelations).GetCmd(r)
                                        If r.DeleteCascade Then
#If OLDM2M Then
                                        M2MDelete(orm, r.Entity.GetRealType(MappingEngine), r.Key)
#End If

                                            If cmd IsNot Nothing Then
                                                cmd.RemoveAll(Me)
                                                toDel.Add(CType(cmd.Relation, M2MRelation))
                                            End If
                                        ElseIf cmd IsNot Nothing AndAlso cmd.Relation.HasDeleted Then
                                            toDel.Add(CType(cmd.Relation, M2MRelation))
                                        End If
#If OLDM2M Then
                                    acs = M2MSave(orm, r.Entity.GetRealType(MappingEngine), r.Key)
#End If

                                        processedType.Add(r.Entity.GetRealType(MappingEngine))
                                    End If

#If OLDM2M Then
                                If acs IsNot Nothing Then CType(orm, _IKeyEntity).AddAccept(acs)
#End If
                                Next

                                For Each elb As M2MRelation In toDel
                                    Dim el As M2MRelation = elb.PrepareSave(Me)
                                    If el IsNot Nothing Then
                                        M2MSave(orm, el)
                                        'elb.Saved = True
                                        elb._savedIds.AddRange(el.Added)
                                        hasNew = hasNew OrElse elb.HasNew
                                    End If
                                Next

                                Dim oo As IRelation = TryCast(MappingEngine.GetEntitySchema(t), IRelation)
#If OLDM2M Then
                            If oo IsNot Nothing Then
                                Dim o As New M2MEnum(oo, orm, MappingEngine)
                                CType(_cache, OrmCache).ConnectedEntityEnum(Me, t, AddressOf o.Remove)
                            End If
#End If
                            End If
                        End If

                        Dim saved As Boolean = obj.Save(Me)
                        If Not saved Then
                            ttt = True
                            hasErrors = False
                            Return True
                        End If

                        obj.RaiseSaved(sa)

                        If sa = SaveAction.Insert Then
                            If orm IsNot Nothing Then
                                Dim oo As IRelation = TryCast(MappingEngine.GetEntitySchema(t), IRelation)
#If OLDM2M Then
                            If oo IsNot Nothing Then
                                Dim o As New M2MEnum(oo, orm, MappingEngine)
                                CType(_cache, OrmCache).ConnectedEntityEnum(Me, t, AddressOf o.Add)
                            End If
#End If

                                M2MUpdate(orm, old_id)

                                For Each r As M2MRelationDesc In MappingEngine.GetM2MRelations(t)
                                    Dim tt As Type = r.Entity.GetRealType(MappingEngine)
                                    If Not MappingEngine.IsMany2ManyReadonly(t, tt) Then
#If OLDM2M Then

                                    Dim acs As AcceptState2 = M2MSave(orm, tt, r.Key)
                                    If acs IsNot Nothing Then
                                        hasNew = hasNew OrElse acs.el.HasNew
                                        'obj.AddAccept(acs)
                                    End If
#End If
                                    End If
                                Next

                                For Each rl As Relation In orm.GetAllRelation
                                    Dim elb As M2MRelation = TryCast(rl, M2MRelation)
                                    If elb IsNot Nothing Then
                                        Dim el As M2MRelation = elb.PrepareSave(Me)
                                        If el IsNot Nothing Then
                                            M2MSave(orm, el)
                                            'elb.Saved = True
                                            elb._savedIds.AddRange(el.Added)
                                            hasNew = hasNew OrElse elb.HasNew
                                        End If
                                    End If
                                Next
                            End If
                        ElseIf sa = SaveAction.Update Then
                            If orm IsNot Nothing Then
#If OLDM2M Then
                            If orm.GetM2M IsNot Nothing Then
                                For Each acp As AcceptState2 In orm.GetM2M
                                    'Dim el As EditableList = acp.el.PrepareNewSave(Me)
                                    Dim el As M2MRelation = acp.el.PrepareSave(Me)
                                    If el IsNot Nothing Then
                                        M2MSave(orm, acp.el.SubType, acp.el.Key, el)
                                        acp.CacheItem.Entry.Saved = True
                                        acp.CacheItem.Entry._savedIds.AddRange(el.Added)
                                    End If
                                    hasNew = hasNew OrElse acp.el.HasNew
                                    processedType.Add(acp.el.SubType)
                                Next
                            End If

                            For Each o As Pair(Of M2MCache, Pair(Of String, String)) In Cache.GetM2MEntries(orm, Nothing)
                                Dim m2me As M2MCache = o.First
                                If m2me.Filter IsNot Nothing Then
                                    Dim dic As IDictionary = GetDic(_cache, o.Second.First)
                                    dic.Remove(o.Second.Second)
                                Else
                                    If m2me.Entry.HasChanges AndAlso Not m2me.Entry.Saved AndAlso Not processedType.Contains(m2me.Entry.SubType) Then
                                        Throw New InvalidOperationException(String.Format("M2M with {0} key {1} has changes. Key: {2}. Id: {3}", m2me.Entry.SubType, m2me.Entry.Key, o.Second.First, o.Second.Second))
                                    End If
                                End If
                            Next
#End If

                                For Each rl As Relation In orm.GetAllRelation
                                    Dim elb As M2MRelation = TryCast(rl, M2MRelation)
                                    If elb IsNot Nothing Then
                                        Dim el As M2MRelation = elb.PrepareSave(Me)
                                        If el IsNot Nothing Then
                                            M2MSave(orm, el)
                                            'elb.Saved = True
                                            elb._savedIds.AddRange(el.Added)
                                        End If
                                        hasNew = hasNew OrElse elb.HasNew
                                    End If
                                Next
                            End If
                        End If

                        If AcceptChanges Then
                            If hasNew Then
                                Throw New OrmObjectException("Cannot accept changes. Some of relation has new objects")
                            End If
                            oldObj = Me.AcceptChanges(obj, False, SinglePKEntity.IsGoodState(state))
                        End If

                        err = False
                        hasErrors = False
                    Finally
                        If err Then
                            If AcceptChanges AndAlso Not ttt Then
                                Me.RejectChanges(uc)
                            End If

                            'state = old_state
                            'Else
                            '    obj.ObjSaved = True
                        End If
                    End Try
                    Return hasNew
                End Using
            Finally
                '            If obj.ObjSaved AndAlso AcceptChanges Then
                If AcceptChanges AndAlso Not hasErrors Then
                    obj.UpdateCache(Me, oldObj)
                End If
            End Try
        End Using
    End Function

    Public Function AddObject(ByVal obj As _ICachedEntity) As ICachedEntity
        Invariant()

        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        Using obj.LockEntity()
            If obj.ObjectState = ObjectState.Created OrElse obj.ObjectState = ObjectState.NotFoundInSource Then
                If Not InsertObject(obj) Then
                    Return Nothing
                End If
                'ElseIf obj.ObjectState = ObjectState.Clone Then
                '    Throw New InvalidOperationException("Object with state " & obj.ObjectState.ToString & " cann't be added to cashe")
            End If
        End Using

        Return obj
    End Function

    Protected Friend Sub M2MSave(ByVal obj As ISinglePKEntity, ByVal el As M2MRelation)
        M2MSave(obj, el.Relation.Entity.GetRealType(MappingEngine), el.Key, el)
    End Sub

#Region " Abstract members "

    Protected MustOverride Function GetSearchSection() As String

#If Not ExcludeFindMethods Then
    Protected MustOverride Function SearchEx(Of T As {IKeyEntity, New})(ByVal type2search As Type, _
        ByVal contextKey As Object, ByVal sort As Sort, ByVal filter As IFilter, ByVal ftsText As String, _
        ByVal limit As Integer, ByVal frmt As IFtsStringFormatter) As ReadOnlyList(Of T)

    Protected MustOverride Function Search(Of T As {IKeyEntity, New})( _
        ByVal type2search As Type, ByVal contextKey As Object, ByVal sort As Sort, ByVal filter As IFilter, _
        ByVal frmt As IFtsStringFormatter, Optional ByVal joins() As QueryJoin = Nothing) As ReadOnlyList(Of T)

    Protected MustOverride Function GetCustDelegate(Of T As {IKeyEntity, New})(ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal key As String, ByVal id As String) As ICacheItemProvoder(Of T)

    Protected MustOverride Function GetCustDelegate(Of T As {IKeyEntity, New})(ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal key As String, ByVal id As String, ByVal cols() As String) As ICacheItemProvoder(Of T)

    Protected MustOverride Function GetCustDelegate(Of T As {IKeyEntity, New})(ByVal aspect As QueryAspect, ByVal join() As QueryJoin, ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal key As String, ByVal id As String, Optional ByVal cols As List(Of EntityPropertyAttribute) = Nothing) As ICacheItemProvoder(Of T)

    Protected MustOverride Function GetCustDelegate(Of T As {IKeyEntity, New})(ByVal relation As M2MRelationDesc, ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal key As String, ByVal id As String) As ICacheItemProvoder(Of T)

    Protected MustOverride Function GetCustDelegate(Of T2 As {IKeyEntity, New})( _
        ByVal obj As _IKeyEntity, ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal queryAscpect() As QueryAspect, ByVal id As String, ByVal key As String, ByVal direct As String) As ICacheItemProvoder(Of T2)

    Protected MustOverride Function GetCustDelegate(Of T2 As {IKeyEntity, New})( _
        ByVal obj As _IKeyEntity, ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal id As String, ByVal key As String, ByVal direct As String) As ICacheItemProvoder(Of T2)

    Protected MustOverride Function GetObjects(Of T As {IKeyEntity, New})(ByVal ids As Generic.IList(Of Object), ByVal f As IFilter, ByVal objs As List(Of T), _
       ByVal withLoad As Boolean, ByVal fieldName As String, ByVal idsSorted As Boolean) As Generic.IList(Of T)

#End If

#If OLDM2M Then
    Protected MustOverride Function GetObjects(Of T As {IKeyEntity, New})(ByVal type As Type, ByVal ids As Generic.IList(Of Object), ByVal f As IFilter, _
       ByVal relation As M2MRelationDesc, ByVal idsSorted As Boolean, ByVal withLoad As Boolean) As IDictionary(Of Object, CachedM2MRelation)
#End If

    Protected Friend MustOverride Function GetStaticKey() As String

    Protected Friend MustOverride Sub LoadObject(ByVal obj As _IEntity, ByVal propertyAlias As String)
    Public MustOverride Function GetEntityCloneFromStorage(ByVal obj As _ICachedEntity) As ICachedEntity
    'Public MustOverride Function LoadObjectsInternal(Of T As {IKeyEntity, New}, T2 As {IKeyEntity})(ByVal objs As ReadOnlyList(Of T2), ByVal start As Integer, ByVal length As Integer, ByVal remove_not_found As Boolean, ByVal columns As Generic.List(Of SelectExpression), ByVal withLoad As Boolean) As ReadOnlyList(Of T2)
    'Public MustOverride Function LoadObjectsInternal(Of T2 As {IKeyEntity})(ByVal realType As Type, ByVal objs As ReadOnlyList(Of T2), ByVal start As Integer, ByVal length As Integer, ByVal remove_not_found As Boolean, ByVal columns As Generic.List(Of SelectExpression), ByVal withLoad As Boolean) As ReadOnlyList(Of T2)

    'Protected MustOverride Overloads Sub FindObjects(ByVal t As Type, ByVal WithLoad As Boolean, ByVal arr As System.Collections.ArrayList, ByVal sort As String, ByVal sort_type As SortType)

    Public MustOverride Function UpdateObject(ByVal obj As _ICachedEntity) As Boolean

    Protected MustOverride Function InsertObject(ByVal obj As _ICachedEntity) As Boolean

    Protected Friend MustOverride Sub DeleteObject(ByVal obj As ICachedEntity)

    Protected MustOverride Sub M2MSave(ByVal obj As ISinglePKEntity, ByVal t As Type, ByVal key As String, ByVal el As M2MRelation)

    Protected Friend MustOverride ReadOnly Property Exec() As TimeSpan
    Protected Friend MustOverride ReadOnly Property Fecth() As TimeSpan

#If Not ExcludeFindMethods Then
    Protected MustOverride Function BuildDictionary(Of T As {New, IKeyEntity})(ByVal level As Integer, ByVal filter As IFilter, ByVal joins() As QueryJoin) As DicIndex(Of T)
    Protected MustOverride Function BuildDictionary(Of T As {New, IKeyEntity})(ByVal level As Integer, ByVal filter As IFilter, ByVal joins() As QueryJoin, ByVal firstField As String, ByVal secondField As String) As DicIndex(Of T)
#End If

#End Region

    'Protected Friend Function LoadObjectsInternal(Of T As {IKeyEntity, New})( _
    '        ByVal objs As ReadOnlyList(Of T), ByVal start As Integer, ByVal length As Integer, _
    '        ByVal remove_not_found As Boolean) As ReadOnlyList(Of T)
    '    Dim original_type As Type = GetType(T)
    '    Dim columns As Generic.List(Of EntityPropertyAttribute) = _schema.GetSortedFieldList(original_type)

    '    Return LoadObjectsInternal(Of T)(objs, start, length, remove_not_found, columns, True)
    'End Function

    Protected Friend Sub RegisterInCashe(ByVal obj As _ICachedEntity)
        If Not IsInCachePrecise(obj) Then
            Add2Cache(obj)
            'If obj.OriginalCopy() IsNot Nothing Then
            '    'Dim c As OrmCache = TryCast(_cache, OrmCache)
            '    'If c IsNot Nothing Then
            '    '    c.RegisterExistingModification(obj)
            '    'End If
            '    Dim r As ObjectModification.ReasonEnum
            '    Select Case obj.ObjectState
            '        Case ObjectState.Deleted
            '            r = ObjectModification.ReasonEnum.Delete
            '        Case ObjectState.Modified
            '            r = ObjectModification.ReasonEnum.Edit
            '    End Select
            '    _cache.RegisterModification(Me, obj, r, TryCast(MappingEngine.GetEntitySchema(obj.GetType), ICacheBehavior))
            'End If
        End If
    End Sub

#If Not ExcludeFindMethods Then
    Public Function BuildObjDictionary(Of T As {New, IKeyEntity})(ByVal level As Integer, ByVal criteria As IGetFilter, ByVal join() As QueryJoin) As DicIndex(Of T)
        Return BuildObjDic(Of T)(level, criteria, join, AddressOf (New clsDic(Of T)).f)
    End Function

    Public Function BuildObjDictionary(Of T As {New, IKeyEntity})(ByVal level As Integer, _
        ByVal criteria As IGetFilter, ByVal join() As QueryJoin, ByVal field As String) As DicIndex(Of T)
        Return BuildObjDic(Of T)(level, criteria, join, AddressOf (New clsDic(Of T)(field)).f)
    End Function

    Public Function BuildObjDictionary(Of T As {New, IKeyEntity})(ByVal level As Integer, _
        ByVal criteria As IGetFilter, ByVal join() As QueryJoin, ByVal firstField As String, ByVal secondField As String) As DicIndex(Of T)
        Return BuildObjDic(Of T)(level, criteria, join, AddressOf (New clsDic(Of T)(firstField, secondField)).f)
    End Function

    Protected Delegate Function GetRootsDelegate(Of T As {New, IKeyEntity})(ByVal mgr As OrmManager, ByVal level As Integer, ByVal filter As IFilter, ByVal join() As QueryJoin) As DicIndex(Of T)

    Private Class clsDic(Of T As {New, IKeyEntity})
        Private _f As String
        Private _s As String

        Public Sub New()

        End Sub

        Public Sub New(ByVal f As String)
            MyClass.New(f, Nothing)
        End Sub

        Public Sub New(ByVal f As String, ByVal s As String)
            _f = f
            _s = s
        End Sub
        Public Function f(ByVal mgr As OrmManager, ByVal level As Integer, ByVal filter As IFilter, ByVal join() As QueryJoin) As DicIndex(Of T)
            If String.IsNullOrEmpty(_f) Then
                Return mgr.BuildDictionary(Of T)(level, filter, join)
            Else
                Return mgr.BuildDictionary(Of T)(level, filter, join, _f, _s)
            End If
        End Function
    End Class

    Protected Function BuildObjDic(Of T As {New, IKeyEntity})(ByVal level As Integer, ByVal criteria As IGetFilter, _
        ByVal joins() As QueryJoin, ByVal getRoots As GetRootsDelegate(Of T)) As DicIndex(Of T)
        Dim tt As System.Type = GetType(T)

        Dim key As String = String.Empty

        Dim f As String = String.Empty
        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            'f = criteria.Filter(tt)._ToString
            f = criteria.Filter()._ToString
        End If

        If criteria IsNot Nothing AndAlso criteria.Filter IsNot Nothing Then
            'key = criteria.Filter(tt).GetStaticString(_schema) & _schema.GetEntityKey(GetFilterInfo, tt) & GetStaticKey() & "Dics"
            key = criteria.Filter().GetStaticString(_schema, GetContextInfo) & _schema.GetEntityKey(GetContextInfo, tt) & GetStaticKey() & "Dics"
        Else
            key = _schema.GetEntityKey(GetContextInfo, tt) & GetStaticKey() & "Dics"
        End If

        If joins IsNot Nothing Then
            For Each join As QueryJoin In joins
                If Not QueryJoin.IsEmpty(join) Then
                    key &= join._ToString
                    f &= join._ToString
                End If
            Next
        End If

        Dim dic As IDictionary = GetDic(_cache, key)

        Dim id As String = f & " - dics - "
        Dim sync As String = id & GetStaticKey() & "--level" & level

        Dim roots As DicIndex(Of T) = CType(dic(id), DicIndex(Of T))
        Invariant()

        If roots Is Nothing Then
            Using SyncHelper.AcquireDynamicLock(sync)
                roots = CType(dic(id), DicIndex(Of T))
                If roots Is Nothing Then
                    roots = getRoots(Me, level, GetFilter(criteria, tt), joins)
                    dic.Add(id, roots)
                End If
            End Using
        End If

        Return roots
    End Function

#End If

    Public Shared Sub BuildDic(Of T As {New, DicIndexT(Of T2)}, T2 As {New, _IEntity})(ByVal name As String, ByVal cnt As Integer, _
        ByVal level As Integer, ByVal root As T, ByRef last As T, _
        ByRef first As Boolean, ByVal firstField As String, ByVal secField As String)
        'If name.Length = 0 Then name = "<без имени>"

        Dim current As T = Nothing

        Dim p As T = Nothing
        If Not first Then
            Dim i As Integer = FirstDiffCharIndex(name, last.Name)
            Assert(i <= level, "Index {0} must be less than level {1}", i, level)

            If last.Name = "" Then
                p = root
            Else
                p = CType(GetParent(last, last.Name.Length - i), T)
            End If
        Else
            p = last
            first = False
        End If
l1:
        If p Is root AndAlso name <> "" Then
            If name(0) = "'" Then
                Dim s As New T
                DicIndexT(Of T2).Init(s, "'", Nothing, 0, firstField, secField, root.Cmd)
                p = CType(root.Dictionary(s), T)
                If p IsNot Nothing Then GoTo l1
            End If
            Dim _prev As T = root
            For k As Integer = 1 To name.Length
                Dim c As Integer = 0
                If k = name.Length Then c = cnt
                Dim s As New T
                DicIndexT(Of T2).Init(s, name.Substring(0, k), _prev, c, firstField, secField, root.Cmd)
                If _prev Is root Then
                    If root.Dictionary.Contains(s.Name) Then
                        s = CType(root.Dictionary(s.Name), T)
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
        ElseIf Not root.Dictionary.Contains(name) Then
            current = New T
            DicIndexT(Of T2).Init(current, name, p, cnt, firstField, secField, root.Cmd)
            p.AddChild(current)
            root.Add2Dictionary(current)
        Else
            current = root
            first = True
        End If

        'End If

        last = current
    End Sub

    Protected Shared Function GetParent(Of T As {New, _IEntity})(ByVal mi As DicIndexT(Of T), ByVal level As Integer) As DicIndexT(Of T)
        Dim p As DicIndexT(Of T) = mi
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

    Public Shared Sub AppendJoin(ByVal schema As ObjectMappingEngine, ByVal selectType As Type, _
        ByRef filter As IFilter, ByVal filterInfo As Object, ByVal l As List(Of QueryJoin), _
        ByVal selSchema As IEntitySchema, ByVal types As List(Of Type), ByVal type2join As System.Type, _
        ByVal t2jSchema As IEntitySchema, ByVal joinOS As EntityUnion, ByVal selectOS As EntityUnion)

        Dim field As String = schema.GetJoinFieldNameByType(type2join, selSchema)

        If String.IsNullOrEmpty(field) Then

            field = schema.GetJoinFieldNameByType(selectType, t2jSchema)

            If String.IsNullOrEmpty(field) Then
                Dim m2m As M2MRelationDesc = schema.GetM2MRelation(type2join, selectType, True)
                If m2m IsNot Nothing Then
                    l.AddRange(MakeM2MJoin(schema, m2m, type2join))
                Else
                    Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2join))
                End If
            Else
                l.Add(MakeJoin(schema, selectOS, selectType, joinOS, field, FilterOperation.Equal, JoinType.Join, True))
            End If
        Else
            l.Add(MakeJoin(schema, joinOS, type2join, selectOS, field, FilterOperation.Equal, JoinType.Join))
        End If

        If types IsNot Nothing Then
            types.Add(type2join)
        End If

        'Dim ts As IMultiTableObjectSchema = TryCast(t2jSchema, IMultiTableObjectSchema)
        'If ts IsNot Nothing Then
        '    Dim pk_table As SourceFragment = t2jSchema.Table
        '    For i As Integer = 1 To ts.GetTables.Length - 1
        '        Dim joinableTs As IGetJoinsWithContext = TryCast(ts, IGetJoinsWithContext)
        '        Dim join As QueryJoin = Nothing
        '        If joinableTs IsNot Nothing Then
        '            join = joinableTs.GetJoins(pk_table, ts.GetTables(i), filterInfo)
        '        Else
        '            join = ts.GetJoins(pk_table, ts.GetTables(i))
        '        End If

        '        If Not QueryJoin.IsEmpty(join) Then
        '            l.Add(join)
        '        End If
        '    Next


        'End If

        Dim cfs As IContextObjectSchema = TryCast(t2jSchema, IContextObjectSchema)
        If cfs IsNot Nothing Then
            Dim newfl As IFilter = cfs.GetContextFilter(filterInfo)
            If newfl IsNot Nothing Then
                Dim con As Condition.ConditionConstructor = New Condition.ConditionConstructor
                con.AddFilter(filter)
                con.AddFilter(newfl)
                filter = con.Condition
            End If
        End If
    End Sub

    Protected Friend Shared Function MakeM2MJoin(ByVal schema As ObjectMappingEngine, ByVal m2m As M2MRelationDesc, ByVal type2join As Type) As Worm.Criteria.Joins.QueryJoin()
        Dim jf As New JoinFilter(m2m.Table, m2m.Column, m2m.Entity.GetRealType(schema), schema.GetSinglePK(m2m.Entity.GetRealType(schema)), Worm.Criteria.FilterOperation.Equal)
        Dim mj As New QueryJoin(m2m.Table, Joins.JoinType.Join, jf)
        m2m = schema.GetM2MRelation(m2m.Entity.GetRealType(schema), type2join, True)
        Dim jt As New JoinFilter(m2m.Table, m2m.Column, type2join, schema.GetSinglePK(type2join), Worm.Criteria.FilterOperation.Equal)
        Dim tj As New QueryJoin(schema.GetTables(type2join)(0), Joins.JoinType.Join, jt)
        Return New QueryJoin() {mj, tj}
    End Function

    Protected Friend Shared Function MakeJoin(ByVal schema As ObjectMappingEngine, _
        ByVal joinOS As EntityUnion, ByVal type2join As Type, _
        ByVal selectType As EntityUnion, ByVal field As String, _
        ByVal oper As Worm.Criteria.FilterOperation, ByVal joinType As Joins.JoinType, _
        Optional ByVal switchTable As Boolean = False) As Worm.Criteria.Joins.QueryJoin

        'Dim tbl As SourceFragment = GetTables(type2join)(0)
        'If switchTable Then
        '    tbl = GetTables(selectType)(0)
        'End If

        Dim jf As New JoinFilter(joinOS, schema.GetSinglePK(type2join), selectType, field, oper)

        Dim t As EntityUnion = joinOS
        If switchTable Then
            t = selectType
        End If

        Return New QueryJoin(t, joinType, jf)
    End Function

    Public Sub ParseValueFromStorage(ByVal isNull As Boolean, ByVal att As Field2DbRelations, _
            ByVal obj As Object, ByVal m As MapField2Column, ByVal propertyAlias As String, _
            ByVal oschema As IEntitySchema, ByVal map As Collections.IndexedCollection(Of String, MapField2Column), _
            ByVal sv As PKDesc(), ByVal ll As IPropertyLazyLoad, ByVal fac As List(Of Pair(Of String, PKDesc())))
        Dim pi As Reflection.PropertyInfo = m.PropertyInfo
        Dim value As Object = sv(0).Value
        If pi Is Nothing Then
            If isNull Then
                ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, Nothing, oschema)
            Else
                ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, value, oschema)
            End If
            If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True, map, MappingEngine)
        Else
            Dim propType As Type = pi.PropertyType
            'If check_pk AndAlso (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
            '    Dim v As Object = pi.GetValue(obj, Nothing)
            '    If Not value.GetType Is propType AndAlso propType IsNot GetType(Object) Then
            '        If propType.IsEnum Then
            '            value = [Enum].ToObject(propType, value)
            '        Else
            '            value = Convert.ChangeType(value, propType)
            '        End If
            '    End If
            '    If Not v.Equals(value) Then
            '        Throw New OrmManagerException("PK values is not equals (" & dr.GetName(i) & "): value from db: " & value.ToString & "; value from object: " & v.ToString)
            '    End If
            'Else
            If Not isNull AndAlso (att And Field2DbRelations.PK) <> Field2DbRelations.PK Then
                If (att And Field2DbRelations.Factory) = Field2DbRelations.Factory Then
                    fac.Add(New Pair(Of String, PKDesc())(propertyAlias, sv))
                    'If ce IsNot Nothing Then ce.SetLoaded(propertyAlias, True, True, map, MappingEngine)
                    '    'obj.CreateObject(c.FieldName, value)
                    '    obj.SetValue(pi, c, )
                    '    obj.SetLoaded(c, True, True)
                    '    'If GetType(OrmBase) Is pi.PropertyType Then
                    '    '    obj.CreateObject(CInt(value))
                    '    '    obj.SetLoaded(c, True)
                    '    'Else
                    '    '    Dim type_created As Type = pi.PropertyType
                    '    '    Dim o As OrmBase = CreateDBObject(CInt(value), type_created)
                    '    '    obj.SetValue(pi, c, o)
                    '    '    obj.SetLoaded(c, True)
                    '    'End If
                Else
                    'If GetType(IKeyEntity).IsAssignableFrom(propType) Then
                    '    Dim type_created As Type = propType
                    '    Dim en As String = MappingEngine.GetEntityNameByType(type_created)
                    '    If Not String.IsNullOrEmpty(en) Then
                    '        Dim cr As Type = MappingEngine.GetTypeByEntityName(en)
                    '        If cr IsNot Nothing AndAlso type_created.IsAssignableFrom(cr) Then
                    '            type_created = cr
                    '        End If
                    '        If type_created Is Nothing Then
                    '            Throw New OrmManagerException("Cannot find type for entity " & en)
                    '        End If
                    '    End If
                    '    Dim o As IKeyEntity = GetKeyEntityFromCacheOrCreate(value, type_created)
                    '    ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, pi, o, oschema)
                    '    If o IsNot Nothing Then
                    '        If obj.CreateManager IsNot Nothing Then o.SetCreateManager(obj.CreateManager)
                    '        RaiseObjectLoaded(o)
                    '    End If
                    '    If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                    'Else
                    ObjectMappingEngine.AssignValue2Property(propType, MappingEngine, Cache, sv, obj, map, propertyAlias, ll, m, oschema, AddressOf RaiseObjectLoaded, _crMan)
                    'End If
                End If
            ElseIf isNull Then
                ObjectMappingEngine.SetPropertyValue(obj, propertyAlias, Nothing, oschema, pi)
                If ll IsNot Nothing Then OrmManager.SetLoaded(ll, propertyAlias, True, map, MappingEngine)
            End If
        End If
    End Sub

    Protected Function LoadEntityFromStorage(ByVal ce As _ICachedEntity, ByVal obj As _IEntity, ByVal load As Boolean, ByVal modificationSync As Boolean, _
        ByVal ec As OrmCache, ByVal loader As LoadObjectFromStorageDelegate, ByVal dic As IDictionary, ByVal selectList As IList(Of SelectExpression), _
        ByVal baseIdx As Integer) As _IEntity

        Dim loadLock As IDisposable = Nothing
        If ce IsNot Nothing Then
            Dim k As String = String.Empty
            If ce.IsPKLoaded Then
                k = ce.UniqueString
            End If
            Dim sync_key As String = "LoadType" & k & obj.GetType.ToString
            loadLock = SyncHelper.AcquireDynamicLock(sync_key)
        End If
        Try
            Dim oschema As IEntitySchema = obj.GetEntitySchema(MappingEngine)
            'Dim props As IDictionary = MappingEngine.GetProperties(obj.GetType, oschema)
            'Dim cols As Generic.List(Of EntityPropertyAttribute) = MappingEngine.GetSortedFieldList(obj.GetType, oschema)
            Dim cm As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap
            Dim lock As IDisposable = Nothing
            Try
                If obj.ObjectState <> ObjectState.Deleted AndAlso (Not load OrElse ec Is Nothing OrElse Not ec.IsDeleted(ce)) Then
                    If Not modificationSync Then
                        Dim ro As _IEntity = CType(loader(obj, selectList, dic, modificationSync, lock, oschema, cm, 0, baseIdx), _IEntity)
                        AfterLoadingProcess(obj, ro)
                        obj = ro
                        ce = TryCast(obj, _ICachedEntity)
                    Else
                        loader(obj, selectList, dic, modificationSync, lock, oschema, cm, 0, baseIdx)
                        obj.CorrectStateAfterLoading(False)
                    End If
                End If

                If Not modificationSync AndAlso ce IsNot Nothing Then
                    If obj.ObjectState <> ObjectState.NotFoundInSource AndAlso obj.ObjectState <> ObjectState.None Then
                        If load Then AcceptChanges(ce, True, True)
                    End If
                End If
            Finally
                If lock IsNot Nothing Then
                    lock.Dispose()
                End If
            End Try
        Finally
            If loadLock IsNot Nothing Then loadLock.Dispose()
        End Try

        Return obj
    End Function

    Protected Function LoadEntityAndParents(ByVal selDic As Dictionary(Of EntityUnion, LoadTypeDescriptor), ByVal selOS As EntityUnion, _
        ByVal ec As OrmCache, ByVal loader As LoadObjectFromStorageDelegate, _
        ByVal idx As Integer, ByVal o As _IEntity, ByVal eudic As Dictionary(Of String, EntityUnion)) As Integer

        Dim tp As LoadTypeDescriptor = selDic(selOS)

        Dim objType As Type = o.GetType
        If tp.Load AndAlso ec IsNot Nothing Then
            ec.BeginTrackDelete(objType)
        End If
        Try
            LoadEntityFromStorage(TryCast(o, _ICachedEntity), o, tp.Load, False, ec, loader, GetDictionary(objType), _
                                     tp.Properties2Load.ConvertAll(Function(e) New SelectExpression(e)), idx)
            idx += tp.Properties2Load.Count
            For Each pr As Pair(Of String, Reflection.PropertyInfo) In tp.ParentProperties
                Dim pit As Type = pr.Second.PropertyType
                Dim eu As EntityUnion = Nothing
                Dim propertyAlias As String = pr.First
                If Not eudic.TryGetValue(propertyAlias & "$" & pit.ToString, eu) Then
                    eu = New EntityUnion(New QueryAlias(pit))
                    eudic(propertyAlias & "$" & pit.ToString) = eu
                End If
                Dim ov As Object = ObjectMappingEngine.GetPropertyValue(o, propertyAlias, tp.EntitySchema, pr.Second)
                If GetType(IEntity).IsAssignableFrom(ov.GetType) Then
                    idx = LoadEntityAndParents(selDic, eu, ec, loader, idx, CType(ov, _IEntity), eudic)
                Else
                    Dim an As New AnonymousEntity
                    Dim ntp As LoadTypeDescriptor = selDic(eu)
                    'LoadSingleFromReader(Nothing, an, ntp.Load, True, ec, dr, Nothing, ntp.Cols, True, idx)
                    'Dim dic As New Dictionary(Of EntityPropertyAttribute, Reflection.PropertyInfo)
                    'Dim idic As IDictionary = dic
                    'For Each p As Pair(Of EntityPropertyAttribute, Reflection.PropertyInfo) In ntp.Properties
                    '    dic.Add(p.First, p.Second)
                    'Next
                    'If dic.Count = 0 Then
                    '    idic = MappingEngine.GetProperties(pit, ntp.Schema)
                    'End If
                    loader(an, ntp.Properties2Load.ConvertAll(Function(e) New SelectExpression(e)), Nothing, False, Nothing, _
                        ntp.EntitySchema, ntp.EntitySchema.FieldColumnMap, 0, idx)
                End If
            Next
            Return idx
        Finally
            If tp.Load AndAlso ec IsNot Nothing Then
                ec.EndTrackDelete(objType)
            End If
        End Try
    End Function

    Protected Sub AfterLoadingProcess(ByVal obj As _IEntity, ByVal ro As _IEntity)
        Dim notFromCache As Boolean = Object.ReferenceEquals(ro, obj)
        ro.CorrectStateAfterLoading(notFromCache)
        'If notFromCache Then
        '    If ro.ObjectState = ObjectState.None OrElse ro.ObjectState = ObjectState.NotLoaded Then
        '        Dim co As _ICachedEntity = TryCast(ro, _ICachedEntity)
        '        If co IsNot Nothing Then
        '            _cache.UnregisterModification(co)
        '            If co.IsPKLoaded Then
        '                ro = NormalizeObject(co, dic)
        '            End If
        '            If lock Then
        '                Threading.Monitor.Exit(dic)
        '                lock = False
        '            End If
        '        End If
        '    End If
        'End If
    End Sub

    Private Function GetSelList(ByVal original_type As Type, ByVal oschema As IEntitySchema, _
                             ByVal propertyAlias As String, ByVal selOS As EntityUnion) As LoadTypeDescriptor
        Dim arr As New List(Of String)
        For Each m As MapField2Column In oschema.FieldColumnMap
            arr.Add(m.PropertyAlias)
        Next
        Dim load As Boolean = True
        Dim df As IDefferedLoading = TryCast(oschema, IDefferedLoading)
        If df IsNot Nothing Then
            Dim loadGroups()() As String = df.GetDefferedLoadPropertiesGroups
            If loadGroups IsNot Nothing Then
                load = False
                If Not String.IsNullOrEmpty(propertyAlias) Then
                    For Each loadProperties() As String In loadGroups
                        If Array.Exists(loadProperties, Function(pr As String) pr = propertyAlias) Then
                            'arr = New List(Of EntityPropertyAttribute)(MappingEngine.GetPrimaryKeys(original_type, oschema))
                            arr.Clear()
                            For Each m As MapField2Column In oschema.FieldColumnMap
                                If m.IsPK Then arr.Add(m.PropertyAlias)
                            Next
                            For Each pr As String In loadProperties
                                arr.Add(pr)
                            Next
                        End If
                    Next
                Else
                    For Each loadProperties() As String In loadGroups
                        For Each pr As String In loadProperties
                            Dim pr2 As String = pr
                            Dim idx As Integer = arr.FindIndex(Function(pa) pa = pr2)
                            If idx >= 0 Then
                                arr.RemoveAt(idx)
                            End If
                        Next
                    Next
                End If
            End If
        End If

        Dim cols As List(Of EntityExpression) = arr.ConvertAll(Of EntityExpression)(Function(col As String) _
             New EntityExpression(col, selOS))

        Return New LoadTypeDescriptor(load, cols, oschema)
    End Function

    Private Function FindObjectsToLoad(ByVal t As Type, ByVal oschema As IEntitySchema, _
        ByVal selOS As EntityUnion, ByVal c As Condition.ConditionConstructor, ByVal eudic As Dictionary(Of String, EntityUnion), _
        ByVal joins As List(Of QueryJoin), ByVal selDic As Dictionary(Of EntityUnion, LoadTypeDescriptor), _
        ByVal propertyAlias As String) As LoadTypeDescriptor

        Dim p As LoadTypeDescriptor = GetSelList(t, oschema, propertyAlias, selOS)
        selDic.Add(selOS, p)

        For Each m As MapField2Column In oschema.FieldColumnMap
            Dim pi As Reflection.PropertyInfo = m.PropertyInfo
            Dim mPropertyAlias As String = m.PropertyAlias
            If p.Properties2Load.Exists(Function(se As EntityExpression) se.ObjectProperty.PropertyAlias = mPropertyAlias) Then
                Dim pit As Type = pi.PropertyType
                If ObjectMappingEngine.IsEntityType(pit, MappingEngine) _
                    AndAlso Not GetType(IPropertyLazyLoad).IsAssignableFrom(pit) Then
                    Dim eu As EntityUnion = Nothing
                    If Not eudic.TryGetValue(mPropertyAlias & "$" & pit.ToString, eu) Then
                        eu = New EntityUnion(New QueryAlias(pit))
                        eudic(mPropertyAlias & "$" & pit.ToString) = eu
                    End If
                    If Not joins.Exists(Function(q As QueryJoin) eu.Equals(q.ObjectSource)) Then
                        Dim propSchema As IEntitySchema = Nothing
                        If Not GetType(IEntity).IsAssignableFrom(pit) Then
                            propSchema = MappingEngine.GetPOCOEntitySchema(pit)
                        Else
                            propSchema = MappingEngine.GetEntitySchema(pit, False)
                        End If
                        Dim f As IFilter = Nothing
                        MappingEngine.AppendJoin(selOS, t, oschema, eu, pit, propSchema, f, joins, JoinType.LeftOuterJoin, GetContextInfo, ObjectMappingEngine.JoinFieldType.Direct, mPropertyAlias)
                        c.AddFilter(f)
                        Dim tp As LoadTypeDescriptor = FindObjectsToLoad(pit, propSchema, eu, c, eudic, joins, selDic, Nothing)
                        tp.ChildPropertyAlias = mPropertyAlias
                        'tp.PI = pi
                        p.ParentProperties.Add(New Pair(Of String, Reflection.PropertyInfo)(mPropertyAlias, pi))
                    End If
                End If
            End If
        Next

        Return p
    End Function

    Protected Function PrepareEntity2Load(ByVal obj As _IEntity, ByVal propertyAlias As String,
        ByVal original_type As Type, ByVal eudic As Dictionary(Of String, EntityUnion),
        ByVal js As List(Of QueryJoin), ByVal selDic As Dictionary(Of EntityUnion, LoadTypeDescriptor),
        ByVal selOS As EntityUnion, ByVal oschema As IEntitySchema) As Condition.ConditionConstructor

        Dim c As New Condition.ConditionConstructor '= Database.Criteria.Conditions.Condition.ConditionConstructor
        Dim pks As IEnumerable(Of PKDesc) = Nothing
        If GetType(ICachedEntity).IsAssignableFrom(original_type) Then
            pks = GetPKValues(CType(obj, ICachedEntity), oschema)
        Else
            Dim l As New List(Of PKDesc)
            For Each m As MapField2Column In oschema.FieldColumnMap
                If m.IsPK Then
                    l.Add(New PKDesc(m.PropertyAlias, ObjectMappingEngine.GetPropertyValue(obj, m.PropertyAlias, oschema)))
                End If
            Next
            pks = l.ToArray
        End If
        If pks.Count = 0 Then
            Throw New OrmManagerException(String.Format("Entity {0} has no primary key", original_type))
        End If

        For Each pk As PKDesc In pks
            c.AddFilter(New cc.EntityFilter(selOS, pk.PropertyAlias, New ScalarValue(pk.Value), Worm.Criteria.FilterOperation.Equal))
        Next

        FindObjectsToLoad(original_type, oschema, selOS, c, eudic, js, selDic, propertyAlias)
        Return c
    End Function

    Public Sub Load(ByVal e As _IEntity, ByVal oschema As IEntitySchema, Optional ByVal propertyAlias As String = Nothing)
        Dim kp As IKeyProvider = TryCast(e, IKeyProvider)
        If kp IsNot Nothing Then
            'Dim mo As ObjectModification = Cache.ShadowCopy(e.GetType, kp, TryCast(oschema, ICacheBehavior))
            ''If mo Is Nothing Then mo = _mo
            'If mo IsNot Nothing Then
            '    If mo.User IsNot Nothing Then
            '        'Using mc As IGetManager = GetMgr()
            '        If Not mo.User.Equals(CurrentUser) Then
            '            Throw New OrmObjectException(e.ObjName & "Object in readonly state")
            '        End If
            '        'End Using
            '    Else
            '        If e.ObjectState = Entities.ObjectState.Deleted OrElse e.ObjectState = Entities.ObjectState.Modified Then
            '            Throw New OrmObjectException(e.ObjName & "Cannot load object while its state is deleted or modified!")
            '        End If
            '    End If
            'End If
        End If
        If e.ObjectState = Entities.ObjectState.Deleted OrElse e.ObjectState = Entities.ObjectState.Modified Then
            Throw New OrmObjectException(e.ObjName & "Cannot load object while its state is deleted or modified!")
        End If

        Dim olds As ObjectState = e.ObjectState
        Dim ce As _ICachedEntity = TryCast(e, _ICachedEntity)
        If ce IsNot Nothing Then
            Dim robj As ICachedEntity = NormalizeObject(ce, GetDictionary(e.GetType), False, False, oschema)
            If robj IsNot Nothing AndAlso Not ReferenceEquals(robj, e) Then
                Dim ll As IPropertyLazyLoad = TryCast(e, IPropertyLazyLoad)
                If String.IsNullOrEmpty(propertyAlias) Then
                    If ll IsNot Nothing AndAlso ll.IsLoaded Then
l1:
                        OrmManager.CopyBody(robj, e, oschema)
                        Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap
                        For Each m As MapField2Column In map
                            SetLoaded(ll, m.PropertyAlias, True, map, MappingEngine)
                        Next
                        CheckIsAllLoaded(ll, MappingEngine, map.Count, map)
                    Else
                        Load(robj, oschema)
                        GoTo l1
                    End If
                ElseIf ll IsNot Nothing Then
                    Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap
                    If IsPropertyLoaded(robj, propertyAlias, map, MappingEngine) Then
                        Dim m As MapField2Column = Nothing
                        map.TryGetValue(propertyAlias, m)
                        ObjectMappingEngine.SetPropertyValue(e, propertyAlias, ObjectMappingEngine.GetPropertyValue(robj, propertyAlias, oschema, m.PropertyInfo), oschema, m.PropertyInfo)
                        SetLoaded(ll, propertyAlias, True, map, MappingEngine)
                    Else
                        Load(robj, propertyAlias)
                        GoTo l1
                    End If
                Else
                    Load(robj, propertyAlias)
                    GoTo l1
                End If
            Else
                LoadObject(e, propertyAlias)
            End If
        Else
            LoadObject(e, propertyAlias)
        End If

        Dim uc As IUndoChanges = TryCast(e, IUndoChanges)
        If uc IsNot Nothing AndAlso olds = Entities.ObjectState.Created AndAlso e.ObjectState = Entities.ObjectState.Modified Then
            AcceptChanges(uc, True, True)
        ElseIf e.IsLoaded Then
            e.SetObjectState(Entities.ObjectState.None)
        End If
    End Sub

    Public Sub Load(ByVal e As _IEntity, Optional ByVal propertyAlias As String = Nothing)
        Dim oschema As IEntitySchema = e.GetEntitySchema(MappingEngine)
        Load(e, oschema, propertyAlias)
    End Sub

#Region " Undo helpers "
    Public Shared Function GetChangeDescription(ByVal sb As StringBuilder, ByVal e As IEntity) As String
        Dim mpe As ObjectMappingEngine = e.GetMappingEngine
        Dim oschema As IEntitySchema = e.GetEntitySchema(mpe)

        If e.ObjectState = Entities.ObjectState.Modified Then
            Dim uc As IUndoChanges = TryCast(e, IUndoChanges)
            If uc IsNot Nothing Then
                For Each pa As String In GetChanges(e, uc.OriginalCopy, oschema)
                    sb.Append(vbTab).Append(pa).Append(vbCrLf)
                Next
                Return sb.ToString
            End If
        End If

        Return GetChangeDescription(sb, e, oschema, mpe)
    End Function

    Public Shared Function GetChangeDescription(ByVal sb As StringBuilder, ByVal e As Object, ByVal oschema As IEntitySchema, ByVal mpe As ObjectMappingEngine) As String

        Dim o As Object = mpe.CloneIdentity(e, oschema)

        For Each pa As String In GetChanges(e, o, oschema)
            sb.Append(vbTab).Append(pa).Append(vbCrLf)
        Next

        Return sb.ToString

    End Function

    Friend Shared Function GetChanges(ByVal currentVersion As Object, ByVal originalVersion As Object, ByVal oschema As IEntitySchema) As String()
        Dim l As New List(Of String)
        'If Not Object.Equals(obj.SpecificMappingEngine, e.SpecificMappingEngine) Then
        '    obj.SpecificMappingEngine = e.SpecificMappingEngine()
        'End If
        For Each m As MapField2Column In oschema.FieldColumnMap
            Dim original As Object = ObjectMappingEngine.GetPropertyValue(originalVersion, m.PropertyAlias, oschema, m.PropertyInfo)
            If Not m.IsReadOnly Then
                Dim current As Object = ObjectMappingEngine.GetPropertyValue(currentVersion, m.PropertyAlias, oschema, m.PropertyInfo)
                If (original IsNot Nothing AndAlso Not original.Equals(current)) OrElse _
                    (current IsNot Nothing AndAlso Not current.Equals(original)) Then
                    l.Add(m.PropertyAlias)
                End If
            End If
        Next
        Return l.ToArray
    End Function

    Friend Shared Sub Accept_AfterUpdateCacheDelete(ByVal obj As _ICachedEntity, ByVal mc As OrmManager)
        mc._RemoveObjectFromCache(obj, True)
        Dim c As OrmCache = TryCast(mc.Cache, OrmCache)
        If c IsNot Nothing Then
            c.RegisterDelete(obj)
        End If
        'obj._needDelete = False
    End Sub

    Friend Shared Sub Accept_AfterUpdateCacheAdd(ByVal obj As _ICachedEntity, ByVal cache As CacheBase, _
        ByVal contextKey As Object)
        'obj._needAdd = False
        Dim nm As INewObjectsStore = cache.NewObjectManager
        If nm IsNot Nothing Then
            Dim mo As _ICachedEntity = TryCast(contextKey, _ICachedEntity)
            If mo Is Nothing Then
                Dim dic As Generic.Dictionary(Of _ICachedEntity, _ICachedEntity) = TryCast(contextKey, Generic.Dictionary(Of _ICachedEntity, _ICachedEntity))
                If dic IsNot Nothing Then
                    dic.TryGetValue(obj, mo)
                End If
            End If
            If mo IsNot Nothing Then
                nm.RemoveNew(mo)
            End If
        End If
    End Sub

    Protected Friend Shared Sub ClearCacheFlags(ByVal obj As _ICachedEntity, ByVal mc As OrmManager, _
        ByVal contextKey As Object)
        obj.UpdateCtx.Added = False
        obj.UpdateCtx.Deleted = False
    End Sub

    Public Sub AcceptChanges(ByVal e As _ICachedEntity)
        AcceptChanges(e, True, Entity.IsGoodState(e.ObjectState))
    End Sub

    Public Function AcceptChanges(ByVal e As _ICachedEntity, ByVal updateCache As Boolean, ByVal setState As Boolean) As ICachedEntity
        Dim mo As _ICachedEntity = Nothing
        Using e.LockEntity
            If e.ObjectState = Entities.ObjectState.Created Then 'OrElse e.ObjectState = Entities.ObjectState.Clone OrElse _state = Orm.ObjectState.NotLoaded Then
                Throw New OrmObjectException(e.ObjName & "accepting changes allowed in state Modified, deleted or none")
            End If

            Dim mc As OrmManager = Me
            '_valProcs = HasM2MChanges(mc)

            e.AcceptRelationalChanges(updateCache, mc)

            If e.ObjectState <> Entities.ObjectState.None Then
                mo = RemoveVersionData(e, mc.Cache, mc.MappingEngine, setState)
                Dim c As OrmCache = TryCast(mc.Cache, OrmCache)
                If e.UpdateCtx.Deleted Then
                    '_valProcs = False
                    If updateCache AndAlso c IsNot Nothing Then
                        c.UpdateCache(mc.MappingEngine, New Pair(Of _ICachedEntity)() {New Pair(Of _ICachedEntity)(e, mo)}, mc, AddressOf ClearCacheFlags, Nothing, Nothing, False, False)
                        'mc.Cache.UpdateCacheOnDelete(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing)
                    End If
                    Accept_AfterUpdateCacheDelete(e, mc)
                    e.RaiseDeleted(EventArgs.Empty)
                ElseIf e.UpdateCtx.Added Then
                    '_valProcs = False
                    Dim dic As IDictionary = mc.GetDictionary(e.GetType, TryCast(e.GetEntitySchema(mc.MappingEngine), ICacheBehavior))
                    Dim kw As CacheKey = New CacheKey(e)
                    'Dim o As CachedEntity = CType(dic(kw), CachedEntity)
                    'If (o Is Nothing) OrElse (Not o.IsLoaded AndAlso IsLoaded) Then
                    '    dic(kw) = Me
                    'End If
                    CacheBase.AddObjectInternal(e, kw, dic)
                    c.RemoveNonExistent(e.GetType, kw)
                    If updateCache AndAlso c IsNot Nothing Then
                        'mc.Cache.UpdateCacheOnAdd(mc.ObjectSchema, New OrmBase() {Me}, mc, Nothing, Nothing)
                        c.UpdateCache(mc.MappingEngine, New Pair(Of _ICachedEntity)() {New Pair(Of _ICachedEntity)(e, mo)}, mc, AddressOf ClearCacheFlags, Nothing, Nothing, False, False)
                    End If
                    Accept_AfterUpdateCacheAdd(e, mc.Cache, mo)
                    e.RaiseAdded(EventArgs.Empty)
                Else
                    If updateCache Then
                        If c IsNot Nothing Then
                            c.UpdateCache(mc.MappingEngine, New Pair(Of _ICachedEntity)() {New Pair(Of _ICachedEntity)(e, mo)}, mc, AddressOf ClearCacheFlags, Nothing, Nothing, False, True)
                        End If
                        e.UpdateCacheAfterUpdate(c)
                    End If
                    e.RaiseUpdated(EventArgs.Empty)
                End If
                'ElseIf _valProcs AndAlso updateCache Then
                '    mc.Cache.ValidateSPOnUpdate(Me, Nothing)
            End If

            e.RaiseChangesAccepted(EventArgs.Empty)
        End Using

        Return mo
    End Function

    Public Sub RejectChanges(ByVal e As _ICachedEntity)
        RejectChanges(e, MappingEngine, Cache)
    End Sub

    Public Shared Sub RejectChanges(ByVal e As _ICachedEntity, mpe As ObjectMappingEngine, cache As CacheBase)
        Using e.LockEntity
            e.RejectRelationChanges(Nothing)

            If e.ObjectState = ObjectState.Modified OrElse e.ObjectState = Entities.ObjectState.Deleted OrElse e.ObjectState = Entities.ObjectState.Created Then
                Dim uc As IUndoChanges = TryCast(e, IUndoChanges)

                Dim oc As ICachedEntity = Nothing
                If uc IsNot Nothing Then
                    oc = uc.OriginalCopy()
                End If

                Dim nm As INewObjectsStore = cache.NewObjectManager
                If nm IsNot Nothing Then
                    nm.RemoveNew(e)
                End If

                If e.ObjectState <> Entities.ObjectState.Deleted Then
                    If oc Is Nothing Then
                        If e.ObjectState <> Entities.ObjectState.Created Then
                            Throw New OrmObjectException(e.ObjName & ": When object is in modified state it has to have an original copy")
                        End If
                        Return
                    End If
                End If

                Dim oschema As IEntitySchema = e.GetEntitySchema(mpe)
                'Dim mo As ObjectModification = Cache.ShadowCopy(e.GetType, e, TryCast(oschema, ICacheBehavior))
                'If mo IsNot Nothing Then
                '    If mo.User IsNot Nothing AndAlso Not mo.User.Equals(CurrentUser) Then
                '        Throw New OrmObjectException(e.ObjName & " object in readonly state")
                '    End If

                '    If e.ObjectState = Entities.ObjectState.Deleted AndAlso mo.Reason <> ObjectModification.ReasonEnum.Delete Then
                '        'Debug.Assert(False)
                '        'Throw New OrmObjectException
                '        Return
                '    End If

                '    If e.ObjectState = Entities.ObjectState.Modified AndAlso (mo.Reason = ObjectModification.ReasonEnum.Delete) Then
                '        Debug.Assert(False)
                '        Throw New OrmObjectException
                '    End If
                'End If

                'Debug.WriteLine(Environment.StackTrace)
                '_needAdd = False
                '_needDelete = False
                e.UpdateCtx = New UpdateCtx

                Dim olds As ObjectState = Entities.ObjectState.None
                If oc IsNot Nothing Then
                    olds = oc.ObjectState
                End If

                Dim oldkey As Integer?
                If e.IsPKLoaded Then
                    oldkey = e.Key
                End If

                Dim newid As IEnumerable(Of PKDesc) = Nothing
                If oc IsNot Nothing Then
                    newid = GetPKValues(oc, oschema)
                End If

                If olds <> Entities.ObjectState.Created Then
                    If oc IsNot Nothing Then
                        OrmManager.CopyBody(oc, e, oschema)
                    End If
                    OrmManager.RemoveVersionData(e, cache, mpe, False)
                End If

                If newid IsNot Nothing Then
                    SetPK(e, newid, oschema, mpe)
                End If

#If TraceSetState Then
                    If mo isnot Nothing then
                        SetObjectState(olds, mo.Reason, mo.StackTrace, mo.DateTime)
                    end if
#Else
                e.SetObjectStateClear(olds)
#End If
                If e.ObjectState = Entities.ObjectState.Created Then
                    If oldkey.HasValue Then
                        If cache Is Nothing Then
                            Throw New InvalidOperationException("Cache required")
                        End If

                        Dim dic As IDictionary = cache.GetOrmDictionary(e.GetType, mpe)

                        If dic Is Nothing Then
                            Dim name As String = e.GetType.Name
                            Throw New OrmObjectException("Collection for " & name & " not exists")
                        End If

                        dic.Remove(oldkey)
                    End If
                    ' End Using

                    'Cache.UnregisterModification(e, MappingEngine, TryCast(oschema, ICacheBehavior))

                    If uc IsNot Nothing Then
                        uc.OriginalCopy = Nothing
                    End If

                    Dim ll As IPropertyLazyLoad = TryCast(e, IPropertyLazyLoad)
                    If ll IsNot Nothing Then
                        ll.IsLoaded = False
                    End If
                    '_loaded_members = New BitArray(_loaded_members.Count)
                End If

                'ElseIf state = Obm.ObjectState.Deleted Then
                '    CheckCash()
                '    using SyncHelper(false)
                '        state = ObjectState.None
                '        OrmCache.UnregisterModification(Me)
                '    End SyncLock
                'Else
                '    Throw New OrmObjectException(ObjName & "Rejecting changes in the state " & _state.ToString & " is not allowed")
            End If
            'Invariant(mgr)
        End Using
    End Sub

    Friend Shared Function RemoveVersionData(ByVal e As _ICachedEntity, ByVal cache As CacheBase, _
        ByVal mpe As ObjectMappingEngine, ByVal setState As Boolean) As _ICachedEntity

        Dim mo As _ICachedEntity = Nothing

        'unreg = unreg OrElse _state <> Orm.ObjectState.Created
        If setState Then
            e.SetObjectStateClear(Entities.ObjectState.None)
            'Debug.Assert(IsLoaded, ObjName & "Cannot set state None while object is not loaded")
            If Not e.IsLoaded Then
                Throw New OrmObjectException(e.ObjName & "Cannot set state None while object is not loaded")
            End If
        End If

        Dim uc As IUndoChanges = TryCast(e, IUndoChanges)
        If uc IsNot Nothing Then
            mo = CType(uc.OriginalCopy, _ICachedEntity)
            'cache.UnregisterModification(e, mpe, TryCast(e.GetEntitySchema(mpe), ICacheBehavior))
            uc.OriginalCopy = Nothing
        End If

        Return mo
    End Function

    Private Sub CreateClone4Edit(ByVal e As IUndoChanges, ByVal oschema As IEntitySchema)
        If e.OriginalCopy Is Nothing Then
            Dim clone As IEntity = MappingEngine.CloneFullEntity(e, oschema)
            e.SetObjectState(Entities.ObjectState.Modified)
            e.OriginalCopy = CType(clone, CachedEntity)
            'Using mc As IGetManager = GetMgr()

            'OrmCache.RegisterModification(modified).Reason = ModifiedObject.ReasonEnum.Edit
            'If Not e.IsLoading Then
            RaiseBeginUpdate(e)
            Cache.RaiseBeginUpdate(e)
            'End If
        End If
        'Cache.RegisterModification(Me, e, ObjectModification.ReasonEnum.Edit, TryCast(e.GetEntitySchema(MappingEngine), ICacheBehavior))
        'End Using
    End Sub

    Protected Shared Sub StartUpdate(ByVal e As IUndoChanges)
        If Not e.IsLoading Then 'AndAlso ObjectState <> Orm.ObjectState.Deleted Then
            'If e.ObjectState = Entities.ObjectState.Clone Then
            '    Throw New OrmObjectException(e.ObjName & ": Altering clone is not allowed")
            'End If

            If e.ObjectState = Entities.ObjectState.Deleted Then
                Throw New OrmObjectException(e.ObjName & ": Altering deleted object is not allowed")
            End If

            Using mc As IGetManager = e.GetMgr
                If mc IsNot Nothing Then
                    mc.Manager.PrepareUpdate(e)
                End If
            End Using
        End If
    End Sub

    Protected Sub PrepareUpdate(ByVal e As IUndoChanges)
        Dim oschema As IEntitySchema = Nothing

        If Not e.IsLoaded Then
            If e.ObjectState = Entities.ObjectState.None Then
                Throw New InvalidOperationException(String.Format("Object {0} is not loaded while the state is None", e.ObjName))
            End If

            If e.ObjectState = Entities.ObjectState.NotLoaded Then
                oschema = e.GetEntitySchema(MappingEngine)
                Load(e, oschema, Nothing)
                If e.ObjectState = Entities.ObjectState.NotFoundInSource Then
                    Throw New OrmObjectException(e.ObjName & "Object is not editable 'cause it is not found in source")
                End If
            Else
                Return
            End If
        End If

        If oschema Is Nothing Then
            oschema = e.GetEntitySchema(MappingEngine)
        End If

        'Dim mo As ObjectModification = Cache.ShadowCopy(e.GetType, e, TryCast(oschema, ICacheBehavior))
        'If mo IsNot Nothing Then
        '    'Using mc As IGetManager = GetMgr()
        '    If mo.User IsNot Nothing AndAlso Not mo.User.Equals(CurrentUser) Then
        '        Throw New OrmObjectException(e.ObjName & "Object has already altered by another user")
        '    End If
        '    'End Using
        '    If e.ObjectState = Entities.ObjectState.Deleted Then e.SetObjectState(ObjectState.Modified)
        'Else
        'Debug.Assert(ObjectState = Orm.ObjectState.None) ' OrElse state = Obm.ObjectState.Created)
        'CreateModified(_id)
        CreateClone4Edit(e, oschema)
        'If modified.old_state = Obm.ObjectState.Created Then
        '    _mo = mo
        'End If
        'End If

    End Sub

    Public Shared Function RegisterChange(ByVal o As IUndoChanges, ByVal propertyAlias As String) As IDisposable
        'Using mc As IGetManager = o.GetMgr
        '    If mc Is Nothing Then
        '        Return New BlankSyncHelper(Nothing)
        '    End If
        Return ReadWritePrepare(o, False, propertyAlias)
        'End Using
    End Function

    Friend Sub CreateCopyForSaveNewEntry(ByVal e As _ICachedEntity, ByVal oschema As IEntitySchema, ByVal pk As IEnumerable(Of PKDesc))
        Dim uc As IUndoChanges = TryCast(e, IUndoChanges)
        If uc IsNot Nothing Then
            Assert(uc.OriginalCopy Is Nothing, "OriginalCopy for {0} must not exists", e.ObjName)
            Dim clone As IEntity = MappingEngine.CloneFullEntity(e, oschema)
            uc.OriginalCopy = CType(clone, ICachedEntity)
            'Using mc As IGetManager = GetMgr()
            'Dim c As CacheBase = Cache
            'c.RegisterModification(Me, e, pk, ObjectModification.ReasonEnum.Unknown, TryCast(oschema, ICacheBehavior))
            'End Using
            If pk IsNot Nothing Then SetPK(clone, pk, oschema, MappingEngine)
        End If
        e.SetObjectState(Entities.ObjectState.Modified)
    End Sub

#End Region

#Region " ILazyLoad helpers "
    Friend Shared Function DumpState(ByVal e As _IEntity) As String
        Dim mpe As ObjectMappingEngine = e.GetMappingEngine()
        Dim oschema As IEntitySchema = e.GetEntitySchema(mpe)
        Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap
        Dim sb As New StringBuilder
        Dim olr As Boolean = False
        Dim ll As IPropertyLazyLoad = TryCast(e, IPropertyLazyLoad)
        If ll IsNot Nothing Then
            olr = ll.LazyLoadDisabled
            ll.LazyLoadDisabled = True
        End If
        Try
            For Each m As MapField2Column In map
                sb.Append(m.PropertyAlias).Append("=")
                sb.Append(ObjectMappingEngine.GetPropertyValue(e, m.PropertyAlias, oschema, m.PropertyInfo)).Append(";")
            Next
        Finally
            If ll IsNot Nothing Then
                ll.LazyLoadDisabled = olr
            End If
        End Try
        Return sb.ToString
    End Function

    Private Shared Sub PrepareRead(ByVal e As _ICachedEntity, ByVal propertyAlias As String, ByRef d As System.IDisposable)
        Dim ll As IPropertyLazyLoad = TryCast(e, IPropertyLazyLoad)
        If ll Is Nothing OrElse Not ll.LazyLoadDisabled Then

            If Not e.IsLoaded AndAlso (e.ObjectState = Entities.ObjectState.NotLoaded OrElse e.ObjectState = Entities.ObjectState.None) Then
                If String.IsNullOrEmpty(propertyAlias) Then
                    Using mc As IGetManager = e.GetMgr
                        mc.Manager.Load(e, propertyAlias)
                    End Using
                Else
                    If ll IsNot Nothing Then
                        Using mc As IGetManager = e.GetMgr
                            If mc IsNot Nothing Then
                                Dim mpe As ObjectMappingEngine = mc.Manager.MappingEngine
                                Dim oschema As IEntitySchema = e.GetEntitySchema(mpe)
                                If Not IsPropertyLoaded(e, propertyAlias, oschema.FieldColumnMap, mpe) Then
                                    mc.Manager.Load(e, oschema, propertyAlias)
                                End If
                            End If

                        End Using
                    End If
                End If
            End If

            d = e.LockEntity
        End If
    End Sub

    Private Shared Function ReadPrepareWithCheck(ByVal e As _ICachedEntity, ByVal propertyAlias As String, ByVal checkEntity As Boolean) As IDisposable
        If checkEntity Then
            Using mc As IGetManager = e.GetMgr()
                If mc IsNot Nothing Then
                    Dim mpe As ObjectMappingEngine = mc.Manager.MappingEngine
                    Dim oschema As IEntitySchema = mpe.GetEntitySchema(e.GetType)
                    Dim o As ICachedEntity = TryCast(ObjectMappingEngine.GetPropertyValue(e, propertyAlias, oschema), ICachedEntity)
                    If o IsNot Nothing AndAlso o.ObjectState <> Entities.ObjectState.Created AndAlso Not mc.Manager.IsInCachePrecise(o) Then
                        Dim ov As IOptimizedValues = TryCast(e, IOptimizedValues)
                        If ov IsNot Nothing Then
                            Dim eo As ICachedEntity = mc.Manager.GetEntityFromCacheOrCreate(GetPKValues(o, Nothing), o.GetType)
                            If eo.CreateManager Is Nothing Then eo.SetCreateManager(e.CreateManager)
                            ov.SetValueOptimized(propertyAlias, oschema, eo)
                        Else
                            Throw New OrmObjectException("Check read requires IOptimizedValues")
                        End If
                    End If
                End If
            End Using
        End If
        Return ReadWritePrepare(e, True, propertyAlias)
    End Function

    Private Shared Function ReadWritePrepare(ByVal e As _ICachedEntity, ByVal reader As Boolean, ByVal propertyAlias As String) As IDisposable
        Dim err As Boolean = True
        Dim d As IDisposable = New BlankSyncHelper(Nothing)
        Try
            If reader Then
                PrepareRead(e, propertyAlias, d)
            Else
                d = e.LockEntity
                Dim uc As IUndoChanges = CType(e, IUndoChanges)
                StartUpdate(uc)
                If Not uc.DontRaisePropertyChange AndAlso Not e.IsLoading Then
                    d = New Entity.ChangedEventHelper(e, propertyAlias, d)
                End If
            End If
            err = False
        Finally
            If err Then
                If d IsNot Nothing Then d.Dispose()
            End If
        End Try

        Return d
    End Function

    Public Shared Function RegisterRead(ByVal e As IPropertyLazyLoad, ByVal propertyAlias As String) As IDisposable
        Return ReadWritePrepare(CType(e, _ICachedEntity), True, propertyAlias)
    End Function

    Public Shared Function RegisterRead(ByVal e As IPropertyLazyLoad, ByVal propertyAlias As String, ByVal checkEntity As Boolean) As IDisposable
        Return ReadPrepareWithCheck(CType(e, _ICachedEntity), propertyAlias, checkEntity)
    End Function

    Public Shared Function IsPropertyLoaded(ByVal e As _IEntity, ByVal propertyAlias As String,
        ByVal map As Collections.IndexedCollection(Of String, MapField2Column), ByVal mpe As ObjectMappingEngine) As Boolean
        Dim ll As IPropertyLazyLoad = TryCast(e, IPropertyLazyLoad)
        If ll IsNot Nothing Then
            If e Is Nothing Then
                Throw New ArgumentNullException("e")
            End If

            If map Is Nothing Then
                Throw New ArgumentNullException("map")
            End If

            'Dim map As Collections.IndexedCollection(Of String, MapField2Column) = e.GetEntitySchema(mpe).FieldColumnMap
            Dim idx As Integer = map.IndexOf(propertyAlias)
            If idx < 0 Then
                Throw New OrmObjectException(String.Format("Property {0} not found in type {1}. Ensure it is not suppressed", propertyAlias, e.GetType))
            End If
            Return PropertyLoadState(ll, idx, map, mpe)
        End If
        Return True
    End Function

    Private Shared Sub InitLoadState(ByVal e As IPropertyLazyLoad, ByVal map As Collections.IndexedCollection(Of String, MapField2Column), ByVal mpe As ObjectMappingEngine)
        If e.PropertyLoadState Is Nothing Then
            'OrElse _sver <> If(mpe Is Nothing, "w-x", mpe.Version)
            e.PropertyLoadState = New BitArray(map.Count)
            '_sver = If(mpe Is Nothing, "w-x", mpe.Version)
            Dim ce As _ICachedEntity = TryCast(e, _ICachedEntity)
            If ce IsNot Nothing AndAlso ce.IsPKLoaded Then
                For i As Integer = 0 To map.Count - 1
                    If Not e.PropertyLoadState(i) Then
                        If map(i).IsPK Then
                            e.PropertyLoadState(i) = True
                        End If
                    End If
                Next
            End If
        End If
    End Sub

    Protected Shared Property PropertyLoadState(ByVal ll As IPropertyLazyLoad, ByVal idx As Integer, _
        ByVal map As Collections.IndexedCollection(Of String, MapField2Column), ByVal mpe As ObjectMappingEngine) As Boolean
        Get
            InitLoadState(ll, map, mpe)
            Return ll.PropertyLoadState(idx)
        End Get
        Set(ByVal value As Boolean)
            InitLoadState(ll, map, mpe)
            ll.PropertyLoadState(idx) = value
        End Set
    End Property

    Friend Shared Function SetLoaded(ByVal ll As IPropertyLazyLoad, ByVal propertyAlias As String, ByVal loaded As Boolean, _
            ByVal map As Collections.IndexedCollection(Of String, MapField2Column), ByVal mpe As ObjectMappingEngine) As Boolean

        Dim idx As Integer = map.IndexOf(propertyAlias)
        If idx < 0 Then
            'Throw New OrmObjectException(String.Format("There is no property in type {0} with alias {1}", Me.GetType, propertyAlias))
            Return False
        End If
        Dim old As Boolean = PropertyLoadState(ll, idx, map, mpe)
        PropertyLoadState(ll, idx, map, mpe) = loaded
        Return old
    End Function

    Friend Shared Function CheckIsAllLoaded(ByVal ll As IPropertyLazyLoad, ByVal mpe As ObjectMappingEngine, _
            ByVal loadedColumns As Integer, ByVal map As Collections.IndexedCollection(Of String, MapField2Column)) As Boolean
        'Using SyncHelper(False)
        Dim allloaded As Boolean = True
        If Not ll.IsLoaded OrElse ll.PropertyLoadState.Count <= loadedColumns Then
            For i As Integer = 0 To map.Count - 1
                If Not PropertyLoadState(ll, i, map, mpe) Then
                    'Dim at As Field2DbRelations = schema.GetAttributes(Me.GetType, arr(i))
                    'If (at And Field2DbRelations.PK) <> Field2DbRelations.PK Then
                    allloaded = False
                    Exit For
                    'End If
                End If
            Next
            ll.IsLoaded = allloaded
        End If
        Return allloaded
        'End Using
    End Function

    Protected Shared Sub SetLoaded(ByVal ce As _ICachedEntity, ByVal mpe As ObjectMappingEngine, ByVal value As Boolean)
        Dim ll As IPropertyLazyLoad = TryCast(ce, IPropertyLazyLoad)
        If ll IsNot Nothing Then
            Using ce.LockEntity
                Dim oschema As IEntitySchema = ce.GetEntitySchema(mpe)

                Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap

                If value AndAlso Not ll.IsLoaded Then
                    For i As Integer = 0 To map.Count - 1
                        PropertyLoadState(ll, i, map, mpe) = True
                    Next
                ElseIf Not value AndAlso ll.IsLoaded Then
                    For i As Integer = 0 To map.Count - 1
                        PropertyLoadState(ll, i, map, mpe) = False
                    Next
                End If
                ll.IsLoaded = value
                'Assert(ll.IsLoaded = value,"Object {0}")
            End Using
        End If
    End Sub

#End Region

    Public Shared Sub SetPK(ByVal e As Object, ByVal pk As IEnumerable(Of PKDesc), ByVal oschema As IEntitySchema, ByVal mpe As ObjectMappingEngine)
        Dim op As IOptimizePK = TryCast(e, IOptimizePK)
        If op IsNot Nothing Then
            op.SetPK(pk)
        Else
            Using New LoadingWrapper(e)
                Dim ll As IPropertyLazyLoad = TryCast(e, IPropertyLazyLoad)
                Dim map As Collections.IndexedCollection(Of String, MapField2Column) = Nothing
                If ll IsNot Nothing Then
                    If oschema Is Nothing Then
                        oschema = mpe.GetEntitySchema(e.GetType)
                    End If

                    If oschema Is Nothing Then
                        Throw New ArgumentNullException("oschema")
                    End If

                    map = oschema.FieldColumnMap
                End If
                For Each p As PKDesc In pk
                    ObjectMappingEngine.SetPropertyValue(e, p.PropertyAlias, p.Value, oschema)
                    If ll IsNot Nothing Then
                        OrmManager.SetLoaded(ll, p.PropertyAlias, True, map, mpe)
                    End If
                Next
            End Using
        End If
    End Sub

    Public Shared Sub SetPK(ByVal o As Object, ByVal pk As IEnumerable(Of PKDesc), ByVal mpe As ObjectMappingEngine)
        Dim op As IOptimizePK = TryCast(o, IOptimizePK)
        If op IsNot Nothing Then
            op.SetPK(pk)
        Else
            Dim oschema As IEntitySchema = Nothing
            Dim e As IEntity = TryCast(o, IEntity)
            If e IsNot Nothing Then
                oschema = e.GetEntitySchema(mpe)
            Else
                oschema = mpe.GetEntitySchema(o.GetType)
            End If
            SetPK(o, pk, oschema, mpe)
        End If
    End Sub

    Public Shared Function GetPKValues(ByVal e As Object, ByVal oschema As IEntitySchema) As IEnumerable(Of PKDesc)
        Dim op As IOptimizePK = TryCast(e, IOptimizePK)
        If op IsNot Nothing Then
            Return op.GetPKValues()
        Else
            Return ObjectMappingEngine.GetPKs(e, oschema)
        End If
    End Function

    Public Shared Function GetPKValues(ByVal e As IEntity, ByVal oschema As IEntitySchema) As IEnumerable(Of PKDesc)
        Dim op As IOptimizePK = TryCast(e, IOptimizePK)
        If op IsNot Nothing Then
            Return op.GetPKValues()
        Else
            If oschema Is Nothing Then
                Dim mpe As ObjectMappingEngine = e.GetMappingEngine
                oschema = e.GetEntitySchema(mpe)
            End If
            Return GetPKValues(CType(e, Object), oschema)
        End If
    End Function

    Public Shared Function HasBodyChanges(ByVal e As IEntity) As Boolean
        Return e.ObjectState = Entities.ObjectState.Modified OrElse e.ObjectState = Entities.ObjectState.Deleted OrElse e.ObjectState = Entities.ObjectState.Created
    End Function

    Public Shared Function HasChanges(ByVal e As IEntity) As Boolean
        If e.ObjectState = Entities.ObjectState.Modified OrElse e.ObjectState = Entities.ObjectState.Deleted OrElse e.ObjectState = Entities.ObjectState.Created Then
            Return True
        Else
            Dim r As IRelations = TryCast(e, IRelations)
            If r IsNot Nothing Then
                Return r.HasChanges
            End If
        End If
        Return False
    End Function

    Public Shared Sub CopyBody(ByVal [from] As Object, ByVal [to] As Object, ByVal oschema As IEntitySchema)
        Dim e As _IEntity = TryCast([to], _IEntity)
        If e IsNot Nothing Then
            Using e.LockEntity
                e.BeginLoading()
                If oschema Is Nothing Then
                    oschema = e.GetEntitySchema(e.GetMappingEngine)
                End If
                CopyProperties([from], [to], oschema)
                e.EndLoading()
            End Using
        Else
            CopyProperties([from], [to], oschema)
        End If
    End Sub

    Public Shared Sub CopyProperties(ByVal [from] As Object, ByVal [to] As Object, _
        ByVal oschema As IEntitySchema)

        Dim cp As ICopyProperties = TryCast([from], ICopyProperties)
        If cp IsNot Nothing Then
            cp.CopyTo([to])
        Else
            If oschema Is Nothing Then
                Throw New ArgumentNullException("oschema")
            End If

            Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap

            For Each m As MapField2Column In map
                ObjectMappingEngine.SetPropertyValue([to], m.PropertyAlias, ObjectMappingEngine.GetPropertyValue([from], m.PropertyAlias, oschema, m.PropertyInfo), oschema, m.PropertyInfo)
            Next
        End If
    End Sub

    Public Shared Function PrepareConcurrencyException(ByVal mpe As ObjectMappingEngine, ByVal obj As ICachedEntity) As OrmManagerException
        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        Dim sb As New StringBuilder
        Dim t As Type = obj.GetType
        sb.Append("Concurrency violation error during save object ")
        sb.Append(t.Name).Append(". Key values {")
        Dim cm As Boolean = False
        For Each m As MapField2Column In mpe.GetEntitySchema(t).FieldColumnMap
            Dim pi As Reflection.PropertyInfo = m.PropertyInfo
            If m.IsPK OrElse m.IsRowVersion Then

                Dim s As String = m.SourceFieldExpression 'mpe.GetColumnNameByPropertyAlias(t, mpe, c.PropertyAlias, False, Nothing)
                If cm Then
                    sb.Append(", ")
                Else
                    cm = True
                End If
                sb.Append(s).Append(" = ")

                Dim o As Object = pi.GetValue(obj, Nothing)

                If GetType(Array).IsAssignableFrom(o.GetType) Then
                    sb.Append("{")
                    Dim y As Boolean = False
                    For Each item As Object In CType(o, Array) 'CType(o, Object())
                        If y Then
                            sb.Append(", ")
                        Else
                            y = True
                        End If
                        sb.Append(item)
                    Next
                    sb.Append("}")
                Else
                    sb.Append(o.ToString)
                End If
            End If
        Next
        sb.Append("}")

        Return New OrmManagerException(sb.ToString)
    End Function

    Public Shared ReadOnly Property DefaultMappingEngine() As ObjectMappingEngine
        Get
            If _mpe Is Nothing Then
                _mpe = New ObjectMappingEngine("1")
            End If
            Return _mpe
        End Get
    End Property

End Class

'End Namespace
