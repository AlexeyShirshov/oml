Imports Worm.Criteria
Imports Worm.Criteria.Joins
Imports Worm.Cache
Imports Worm.Entities.Meta
Imports Worm.Entities.Query
Imports Worm.Criteria.Values
Imports Worm.Criteria.Core
Imports Worm.Sorting
Imports Worm.Entities
Imports System.Collections.Generic
Imports Worm.Criteria.Conditions
Imports Worm.Misc
Imports Worm.Query

#Const DontUseStringIntern = True
#Const TraceM2M = False
#Const TraceManagerCreation = False

'Namespace Managers

Partial Public MustInherit Class OrmManager
    Implements IDisposable

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
    Friend _externalFilter As IFilter
    Protected Friend _loadedInLastFetch As Integer
    Friend _list As String
    Private _listeners As New List(Of TraceListener)
    Private _stmtHelper As StmtGenerator

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

    Public ReadOnly Property GetLastExecutionResult() As ExecutionResult
        Get
            Return _er
        End Get
    End Property

    Public Event ObjectLoaded(ByVal sender As OrmManager, ByVal o As IEntity)
    Public Event ObjectRestoredFromCache(ByVal sender As OrmManager, ByVal created As Boolean, ByVal o As ICachedEntity)
    Public Event BeginUpdate(ByVal sender As OrmManager, ByVal o As ICachedEntity)
    Public Event BeginDelete(ByVal sender As OrmManager, ByVal o As ICachedEntity)

    'Public Event ObjectRejected(ByVal o As OrmBase)
    Public Event DataAvailable(ByVal mgr As OrmManager, ByVal r As ExecutionResult)

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

        If type2load IsNot relation.Rel.GetRealType(MappingEngine) Then
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

    Public Function GetEntityFromCacheOrDB(ByVal pk() As PKDesc, ByVal type As Type) As ICachedEntity
        Dim o As _ICachedEntity = CreateObject(pk, type)
        o.SetObjectState(ObjectState.NotLoaded)
        Return GetFromCacheOrLoadFromDB(o, GetDictionary(type))
    End Function

    Public Function GetEntityFromCacheOrCreate(ByVal pk() As PKDesc, ByVal type As Type) As ICachedEntity
        Dim o As _ICachedEntity = CreateObject(pk, type)
        o.SetObjectState(ObjectState.NotLoaded)
        Return NormalizeObject(o, GetDictionary(type))
    End Function

    Public Function GetEntityOrOrmFromCacheOrCreate(Of T As {New, _ICachedEntity})(ByVal pk() As PKDesc) As T
        Dim o As T = CreateObject(Of T)(pk)
        o.SetObjectState(ObjectState.NotLoaded)
        Return CType(NormalizeObject(o, CType(GetDictionary(Of T)(), System.Collections.IDictionary)), T)
    End Function

    Public Function GetEntityFromCacheOrCreate(Of T As {New, _ICachedEntity})(ByVal pk() As PKDesc) As T
        Dim o As T = CreateEntity(Of T)(pk)
        o.SetObjectState(ObjectState.NotLoaded)
        Return CType(NormalizeObject(o, CType(GetDictionary(Of T)(), System.Collections.IDictionary)), T)
    End Function

    Public Function GetKeyEntityFromCacheOrCreate(ByVal id As Object, ByVal type As Type) As IKeyEntity
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
        Dim o As IKeyEntity = CreateKeyEntity(id, type)
        o.SetObjectState(ObjectState.NotLoaded)
        Return CType(NormalizeObject(o, GetDictionary(type)), IKeyEntity)
    End Function

    Public Function GetKeyEntityFromCacheOrCreate(ByVal id As Object, ByVal type As Type, ByVal add2CacheOnCreate As Boolean) As IKeyEntity
        Dim o As IKeyEntity = CreateKeyEntity(id, type)
        o.SetObjectState(ObjectState.NotLoaded)
        Dim obj As _ICachedEntity = NormalizeObject(o, CType(GetDictionary(type), System.Collections.IDictionary), add2CacheOnCreate)
        If ReferenceEquals(o, obj) AndAlso Not add2CacheOnCreate Then
            o.SetObjectState(ObjectState.Created)
        End If
        Return CType(obj, IKeyEntity)
    End Function

    Public Function GetKeyEntityFromCacheOrCreate(Of T As {IKeyEntity, New})(ByVal id As Object) As T
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
        Return CType(NormalizeObject(o, CType(GetDictionary(Of T)(), System.Collections.IDictionary)), T)
    End Function

    Public Function GetKeyEntityFromCacheOrCreate(Of T As {IKeyEntity, New})(ByVal id As Object, ByVal add2CacheOnCreate As Boolean) As T
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
        Dim obj As _ICachedEntity = NormalizeObject(o, CType(GetDictionary(Of T)(), System.Collections.IDictionary), add2CacheOnCreate)
        If ReferenceEquals(o, obj) AndAlso Not add2CacheOnCreate Then
            o.SetObjectState(ObjectState.Created)
        End If
        Return CType(obj, T)
    End Function

    Public Function GetKeyEntityFromCacheOrDB(Of T As {IKeyEntity, New})(ByVal id As Object) As T
        Dim o As T = CreateKeyEntity(Of T)(id)
        o.SetObjectState(ObjectState.NotLoaded)
        Return CType(GetFromCacheOrLoadFromDB(o, CType(GetDictionary(Of T)(), System.Collections.IDictionary)), T)
    End Function

    Public Function GetKeyEntityFromCacheOrDB(ByVal id As Object, ByVal type As Type) As IKeyEntity
        Dim o As IKeyEntity = CreateKeyEntity(id, type)
        o.SetObjectState(ObjectState.NotLoaded)
        Return CType(GetFromCacheOrLoadFromDB(o, GetDictionary(type)), IKeyEntity)
    End Function

    Public Function [Get](Of T As {IKeyEntity, New})(ByVal id As Object) As T
        Return GetKeyEntityFromCacheOrDB(Of T)(id)
    End Function

    Public Function [Get](Of T As {_ICachedEntity, New})(ByVal pk() As PKDesc) As T
        Dim o As _ICachedEntity = CreateObject(Of T)(pk)
        o.SetObjectState(ObjectState.NotLoaded)
        Return CType(GetFromCacheOrLoadFromDB(o, CType(GetDictionary(Of T)(), System.Collections.IDictionary)), T)
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
                Dim psort As Sort = del.Sort

                If ce.SortEquals(psort, MappingEngine, GetContextInfo) OrElse psort Is Nothing Then
                    If v IsNot Nothing AndAlso Not v.ValidateItemFromCache(ce) Then
                        del.Renew = True
                        GoTo l1
                    End If
                    If psort IsNot Nothing AndAlso psort.IsExternal AndAlso ce.SortExpires Then
                        'Dim objs As ICollection(Of T) = r
                        ce = del.GetCacheItem(CType(_schema.ExternalSort(Of T)(Me, psort, r.List), ReadOnlyEntityList(Of T)))
                        dic(id) = ce
                    End If
                Else
                    If Not del.SmartSort Then
                        del.Renew = True
                        GoTo l1
                    Else
                        'Dim loaded As Integer = 0
                        Dim objs As ReadOnlyEntityList(Of T) = r
                        If objs IsNot Nothing AndAlso objs.Count > 0 Then
                            Dim srt As IOrmSorting = Nothing
                            If psort.IsExternal Then
                                ce = del.GetCacheItem(CType(_schema.ExternalSort(Of T)(Me, psort, objs.List), ReadOnlyEntityList(Of T)))
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
                                        Dim os As ReadOnlyEntityList(Of T) = CType(CreateReadonlyList(GetType(T), objs), Global.Worm.ReadOnlyEntityList(Of T))
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

    Public Function FindWithJoins(Of T As {IKeyEntity, New})(ByVal aspect As QueryAspect, _
        ByVal joins() As QueryJoin, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
        Return FindWithJoins(Of T)(aspect, joins, criteria, sort, withLoad, Nothing)
    End Function

    Public Function FindWithJoins(Of T As {IKeyEntity, New})(ByVal aspect As QueryAspect, _
        ByVal joins() As QueryJoin, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean, ByVal cols() As String) As ReadOnlyList(Of T)

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
        ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)

        Return FindJoin(Of T)(type2join, joinField, FilterOperation.Equal, JoinType.Join, criteria, sort, withLoad)
    End Function

    Public Function FindJoin(Of T As {IKeyEntity, New})(ByVal type2join As Type, ByVal joinField As String, _
        ByVal joinOperation As FilterOperation, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)

        Return FindJoin(Of T)(type2join, joinField, joinOperation, JoinType.Join, criteria, sort, withLoad)
    End Function

    Public Function FindJoin(Of T As {IKeyEntity, New})(ByVal type2join As Type, ByVal joinField As String, _
        ByVal joinOperation As FilterOperation, ByVal joinType As JoinType, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)

        Return FindWithJoins(Of T)(Nothing, New QueryJoin() {MakeJoin(type2join, GetType(T), joinField, joinOperation, joinType)}, _
            criteria, sort, withLoad)
    End Function

    Public Function FindJoin(Of T As {IKeyEntity, New})(ByVal top As Integer, ByVal type2join As Type, ByVal joinField As String, _
        ByVal joinOperation As FilterOperation, ByVal joinType As JoinType, ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)

        Return FindWithJoins(Of T)(StmtGenerator.CreateTopAspect(top), New QueryJoin() {MakeJoin(type2join, GetType(T), joinField, joinOperation, joinType)}, _
            criteria, sort, withLoad)
    End Function

    Protected Function FindGetKey(ByVal filter As IFilter, ByVal t As Type) As String
        Return filter.GetStaticString(_schema, GetContextInfo) & GetStaticKey() & _schema.GetEntityKey(GetContextInfo, t)
    End Function

    Public Function Find(Of T As {IKeyEntity, New})(ByVal criteria As IGetFilter) As ReadOnlyList(Of T)
        Return Find(Of T)(criteria, Nothing, False)
    End Function

    Public Function FindDistinct(Of T As {IKeyEntity, New})(ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)
        Dim filter As IFilter = Nothing
        If criteria IsNot Nothing Then
            'filter = criteria.Filter(GetType(T))
            filter = criteria.Filter()
        End If
        Dim joins() As QueryJoin = Nothing
        Dim appendMain As Boolean
        HasJoins(_schema, GetType(T), filter, sort, GetContextInfo, joins, appendMain)
        Return FindWithJoins(Of T)(New DistinctAspect(), joins, filter, sort, withLoad)
    End Function

    Public Function Find(Of T As {IKeyEntity, New})(ByVal criteria As IGetFilter, _
        ByVal sort As Sort, ByVal withLoad As Boolean) As ReadOnlyList(Of T)

        If criteria Is Nothing Then
            Throw New ArgumentNullException("filter")
        End If

        'Dim filter As IFilter = criteria.Filter(GetType(T))
        Dim filter As IFilter = criteria.Filter()

        Dim joins() As QueryJoin = Nothing
        Dim appendMain As Boolean
        If HasJoins(_schema, GetType(T), filter, sort, GetContextInfo, joins, appendMain) Then
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
        HasJoins(_schema, GetType(T), filter, sort, GetContextInfo, joins, appendMain)
        Return FindWithJoins(Of T)(StmtGenerator.CreateTopAspect(top, sort), joins, filter, sort, withLoad, cols)
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

    Protected Friend Delegate Function ValDelegate(ByRef ce As CachedItemBase, _
        ByVal del As ICacheItemProvoderBase, ByVal dic As IDictionary, ByVal id As Object, _
        ByVal sync As String, ByVal v As ICacheValidator) As Boolean

    Protected Friend Function GetFromCache2(ByVal dic As IDictionary, ByVal sync As String, ByVal id As Object, _
        ByVal withLoad As Boolean, ByVal del As ICacheItemProvoderBase) As CachedItemBase

        Return GetFromCacheBase(dic, sync, id, New TypeWrap(Of Object)(New Boolean() {withLoad}), del, Nothing)
    End Function

    Protected Friend Function GetFromCache(Of T As _ICachedEntity)(ByVal dic As IDictionary, ByVal sync As String, ByVal id As Object, _
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

    Private Function _ValCE(Of T As _ICachedEntity)(ByRef ce As CachedItemBase, _
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
            Dim psort As Sort = del.Sort

            If ce_.SortEquals(psort, MappingEngine, GetContextInfo) OrElse psort Is Nothing Then
                If psort IsNot Nothing AndAlso psort.IsExternal AndAlso ce_.SortExpires Then
                    Dim objs As ReadOnlyEntityList(Of T) = ce_.GetObjectList(Of T)(Me)
                    Dim ce2 As UpdatableCachedItem = del.GetCacheItem(CType(_schema.ExternalSort(Of T)(Me, psort, objs.List), ReadOnlyEntityList(Of T)))
                    If ce_.CanRenewAfterSort Then
                        dic(id) = ce2
                    End If
                End If
            Else
                'Dim loaded As Integer = 0
                Dim objs As ReadOnlyEntityList(Of T) = ce_.GetObjectList(Of T)(Me)
                If objs IsNot Nothing Then
                    If objs.Count = 0 Then Return True

                    Dim srt As IOrmSorting = Nothing
                    If psort.IsExternal Then
                        Dim ce2 As UpdatableCachedItem = del.GetCacheItem(CType(_schema.ExternalSort(Of T)(Me, psort, objs.List), ReadOnlyEntityList(Of T)))
                        If ce_.CanRenewAfterSort Then
                            dic(id) = ce2
                        End If
                        ce = ce2
                    ElseIf CanSortOnClient(GetType(T), CType(objs, System.Collections.ICollection), psort, srt) Then
                        Using SyncHelper.AcquireDynamicLock(sync)
                            Dim sc As IComparer(Of T) = Nothing
                            If srt IsNot Nothing Then
                                sc = srt.CreateSortComparer(Of T)(psort)
                            Else
                                sc = New OrmComparer(Of T)(psort)
                            End If
                            If sc IsNot Nothing Then
                                Dim os As ReadOnlyEntityList(Of T) = CType(CreateReadonlyList(GetType(T), objs), Global.Worm.ReadOnlyEntityList(Of T))
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

    Public Shared Function CanSortOnClient(ByVal t As Type, ByVal col As ICollection, ByVal sort As Sort, ByRef sorting As IOrmSorting) As Boolean
        'If sort.Previous IsNot Nothing Then
        '    Return False
        'End If

        If sort.IsCustom OrElse sort.Query IsNot Nothing Then
            Return False
        End If

        'Dim schema As IObjectSchemaBase = _schema.GetObjectSchema(t)
        'sorting = TryCast(schema, IOrmSorting)
        'If sorting Is Nothing Then
        '    Return False
        'End If
        Dim loaded As Integer = 0
        For Each o As IEntity In col
            If o.IsLoaded Then loaded += 1
            If col.Count - loaded > 10 Then
                Return False
            End If
        Next
        Return True
    End Function

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

#End Region

#Region " Object support "
    Protected Friend Function GetSyncForSave(ByVal t As Type, ByVal obj As ICachedEntity) As IDisposable
#If DebugLocks Then
            Return SyncHelper.AcquireDynamicLock_Debug("4098jwefpv345mfds-" & New EntityProxy(obj).ToString, "d:\temp\")
#Else
        Return SyncHelper.AcquireDynamicLock("4098jwefpv345mfds-" & New EntityProxy(obj).ToString)
#End If
    End Function

    Protected Friend Function LoadType(ByVal id As Object, ByVal t As Type, ByVal load As Boolean, ByVal checkOnCreate As Boolean) As IKeyEntity
        Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic
        Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("LoadType", flags, Nothing, Reflection.CallingConventions.Any, New Type() {GetType(Object), GetType(Boolean), GetType(Boolean)}, Nothing)
        Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {t})
        Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {id, load, checkOnCreate}, Nothing), IKeyEntity)
    End Function

    Public Function CreateKeyEntity(ByVal id As Object, ByVal t As Type) As IKeyEntity
        Return KeyEntity.CreateKeyEntity(id, t, _cache, _schema)
    End Function

    Public Function CreateKeyEntity(Of T As {IKeyEntity, New})(ByVal id As Object) As T
        Return KeyEntity.CreateOrmBase(Of T)(id, _cache, _schema)
    End Function

    Public Function CreateObject(Of T As {_ICachedEntity, New})(ByVal pk() As PKDesc) As T
        Return CachedEntity.CreateObject(Of T)(pk, _cache, _schema)
    End Function

    Public Function CreateObject(ByVal pk() As PKDesc, ByVal type As Type) As _ICachedEntity
        Return CachedEntity.CreateObject(pk, type, _cache, _schema)
    End Function

    Public Function CreateEntity(Of T As {_ICachedEntity, New})(ByVal pk() As PKDesc) As T
        Return CachedEntity.CreateEntity(Of T)(pk, _cache, _schema)
    End Function

    Public Function CreateEntity(ByVal pk() As PKDesc, ByVal t As Type) As _ICachedEntity
        Return CachedEntity.CreateEntity(pk, t, _cache, _schema)
    End Function

    Protected Friend Function CreateEntity(ByVal t As Type) As IEntity
        Return Entity.CreateEntity(t, _cache, _schema)
    End Function

    Protected Friend Function CreateEntity(Of T As {_IEntity, New})() As T
        Return Entity.CreateEntity(Of T)(_cache, _schema)
    End Function

    Public Function NormalizeObject(ByVal obj As _ICachedEntity, ByVal dic As IDictionary) As _ICachedEntity
        Return NormalizeObject(obj, dic, True)
    End Function

    Public Function NormalizeObject(ByVal obj As _ICachedEntity, ByVal dic As IDictionary, ByVal add2Cache As Boolean) As _ICachedEntity
        Return _cache.NormalizeObject(obj, False, False, dic, add2Cache, Me)
    End Function

    Public Function GetFromCacheOrLoadFromDB(ByVal obj As _ICachedEntity, ByVal dic As IDictionary) As _ICachedEntity
        Return _cache.NormalizeObject(obj, False, True, dic, True, Me)
    End Function

    Public Function GetLoadedObjectFromCacheOrDB(ByVal obj As _ICachedEntity, ByVal dic As IDictionary) As _ICachedEntity
        Return _cache.NormalizeObject(obj, True, True, dic, True, Me)
    End Function

    Protected Function LoadTypeInternal(Of T As {IKeyEntity, New})(ByVal id As Object, _
        ByVal load As Boolean, ByVal checkOnCreate As Boolean, ByVal dic As IDictionary(Of Object, T), ByVal addOnCreate As Boolean) As T

        Dim o As T = CreateKeyEntity(Of T)(id)
        Return CType(_cache.NormalizeObject(o, load, checkOnCreate, CType(dic, System.Collections.IDictionary), addOnCreate, Me), T)
    End Function

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

    Protected Friend Function LoadType(Of T As {IKeyEntity, New})(ByVal id As Object, _
        ByVal load As Boolean, ByVal checkOnCreate As Boolean) As T

        Dim dic As Generic.IDictionary(Of Object, T) = GetDictionary(Of T)()

#If DEBUG Then
        If dic Is Nothing Then
            Dim name As String = GetType(T).Name
            Throw New OrmManagerException("Collection for " & name & " not exists")
        End If
#End If

        Return LoadTypeInternal(Of T)(id, load, checkOnCreate, dic, True)
    End Function

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
        Dim dic As IDictionary = GetDictionary(obj.GetType)

        If dic Is Nothing Then
            ''todo: throw an exception when all collections will be implemented
            'Return
            Dim name As String = obj.GetType.Name
            Throw New OrmManagerException("Collection for " & name & " not exists")
        End If

        CacheBase.AddObjectInternal(obj, dic)
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

        Using obj.GetSyncRoot
            If obj.ObjectState = ObjectState.Modified OrElse obj.ObjectState = ObjectState.Deleted Then
                Return False
            End If

            Return _RemoveObjectFromCache(obj)
        End Using
    End Function

    Protected Friend Function _RemoveObjectFromCache(ByVal obj As _ICachedEntity) As Boolean
        'Debug.Assert(Not obj.IsLoaded)
        Dim t As System.Type = obj.GetType

        Dim name As String = t.Name
        Dim dic As IDictionary = GetDictionary(t)
        If dic Is Nothing Then
            ''todo: throw an exception when all collections will be implemented
            'Return
            Throw New OrmManagerException("Collection for " & name & " not exists")
        End If

        Dim id As CacheKey = New CacheKey(obj)
        Dim sync_key As String = "LoadType" & id.ToString & t.ToString

        Using SyncHelper.AcquireDynamicLock(sync_key)
            If Cache.ShadowCopy(obj, Me) IsNot Nothing Then
                Return False
            End If

            dic.Remove(id)

            Dim c As OrmCache = TryCast(_cache, OrmCache)
            If c IsNot Nothing Then
                c.RemoveDepends(obj)

                Dim orm As _IKeyEntity = TryCast(obj, _IKeyEntity)
                If orm IsNot Nothing Then
                    For Each o As Pair(Of M2MCache, Pair(Of String, String)) In c.GetM2MEntries(orm, Nothing)
                        If Not o.First.Entry.HasChanges Then
                            Dim mdic As IDictionary = GetDic(Cache, o.Second.First)
                            mdic.Remove(o.Second.Second)
                        End If
                    Next
                End If
            End If

            _cache.RegisterRemoval(obj, Me)

            Debug.Assert(Not IsInCachePrecise(obj))
            Debug.Assert(Cache.ShadowCopy(obj, Me) Is Nothing)
        End Using
        Return True
    End Function
#End Region

#Region " helpers "
    Protected Function MakeJoin(ByVal type2join As Type, ByVal selectType As Type, ByVal field As String, _
        ByVal oper As Criteria.FilterOperation, ByVal joinType As JoinType, Optional ByVal switchTable As Boolean = False) As QueryJoin
        Return MakeJoin(_schema, type2join, selectType, field, oper, joinType, switchTable)
    End Function

    Protected Function MakeM2MJoin(ByVal m2m As M2MRelationDesc, ByVal type2join As Type) As QueryJoin()
        Return MakeM2MJoin(_schema, m2m, type2join)
    End Function

    Friend Shared Function GetSync(ByVal key As String, ByVal id As String) As String
        Return id & Const_KeyStaticString & key
    End Function

    Public Function GetDictionary(ByVal t As Type) As IDictionary
        Return _cache.GetOrmDictionary(GetContextInfo, t, _schema)
    End Function

    Public Function GetDictionary(ByVal t As Type, ByVal schema As IEntitySchema) As IDictionary
        Return _cache.GetOrmDictionary(GetContextInfo, t, _schema, schema)
    End Function

    Public Function GetDictionary(Of T As _ICachedEntity)() As Generic.IDictionary(Of Object, T)
        Return _cache.GetOrmDictionary(Of T)(GetContextInfo, _schema)
    End Function

    Public Function GetDictionary(Of T As _ICachedEntity)(ByVal oschema As IEntitySchema) As Generic.IDictionary(Of Object, T)
        If oschema Is Nothing Then
            Return Nothing
        Else
            Return _cache.GetOrmDictionary(Of T)(GetContextInfo, _schema, oschema)
        End If
    End Function

    Public Function IsInCachePrecise(ByVal obj As ICachedEntity) As Boolean
        Return _cache.IsInCachePrecise(obj, GetContextInfo, _schema)
    End Function

    Public Function IsInCache(ByVal id As Object, ByVal t As Type) As Boolean
        Return _cache.IsInCache(id, t, GetContextInfo, _schema)
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
            Throw New ArgumentNullException("OrmCache cannot be nothing")
        End If
    End Sub

#End Region

#Region " shared helpers "

    Private Shared Sub InsertObject(Of T As {IKeyEntity})(ByVal cache As CacheBase, ByVal check_loaded As Boolean, ByVal l As Generic.List(Of Object), ByVal o As IKeyEntity)
        If o IsNot Nothing Then
            If (Not o.IsLoaded OrElse Not check_loaded) AndAlso o.ObjectState <> ObjectState.NotFoundInSource Then
                If Not (o.ObjectState = ObjectState.Created AndAlso cache.IsNewObject(GetType(T), o.GetPKValues)) Then
                    Dim idx As Integer = l.BinarySearch(o.Identifier)
                    If idx < 0 Then
                        l.Insert(Not idx, o.Identifier)
                    End If
                End If
            End If
        End If
    End Sub

    Private Shared Sub InsertObject(Of T As {IKeyEntity})(ByVal mgr As OrmManager, _
        ByVal check_loaded As Boolean, ByVal l As Generic.List(Of Object), ByVal o As IKeyEntity, _
        ByVal columns As List(Of EntityPropertyAttribute))

        Throw New NotImplementedException
    End Sub

    Protected Shared Function FormPKValues(Of T As {IKeyEntity})(ByVal cache As CacheBase, ByVal objs As ReadOnlyList(Of T), ByVal start As Integer, ByVal length As Integer, _
        Optional ByVal check_loaded As Boolean = True) As List(Of Object)

        Dim l As New Generic.List(Of Object)
        Dim col As IList(Of T) = TryCast(objs, IList(Of T))
        If col IsNot Nothing Then
            For i As Integer = start To start + length - 1
                Dim o As IKeyEntity = col(i)
                InsertObject(Of T)(cache, check_loaded, l, o)
            Next
        Else
            Dim i As Integer = 0
            For Each o As IKeyEntity In objs
                If i >= start + length Then
                    Exit For
                End If
                If i >= start Then
                    InsertObject(Of T)(cache, check_loaded, l, o)
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

    Protected Shared Function FormPKValues(Of T As {IKeyEntity, New})(ByVal mgr As OrmManager, _
        ByVal objs As ReadOnlyList(Of T), ByVal start As Integer, ByVal length As Integer, _
        ByVal check_loaded As Boolean, ByVal columns As Generic.List(Of EntityPropertyAttribute)) As List(Of Object)

        Dim l As New Generic.List(Of Object)
        Dim col As IList(Of T) = TryCast(objs, IList(Of T))
        If col IsNot Nothing Then
            For i As Integer = start To start + length - 1
                Dim o As IKeyEntity = col(i)
                InsertObject(Of T)(mgr, check_loaded, l, o, columns)
            Next
        Else
            Dim i As Integer = 0
            For Each o As IKeyEntity In objs
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

    Protected Friend Shared Sub WriteLine(ByVal message As String)
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
    Public Function GetM2MList(Of T As {IKeyEntity, New})(ByVal obj As _IKeyEntity, ByVal direct As String) As CachedM2MRelation
        Return FindM2MReturnKeys(Of T)(obj, direct).First.Entry
    End Function

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

    Protected Friend Function GetM2MKey(ByVal tt1 As Type, ByVal tt2 As Type, ByVal direct As String) As String
        Return _schema.GetEntityKey(GetContextInfo, tt1) & Const_JoinStaticString & direct & " - new version - " & _schema.GetEntityKey(GetContextInfo, tt2) & "$" & GetStaticKey()
    End Function

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

    Protected Sub M2MUpdate(ByVal obj As _IKeyEntity, ByVal oldId As Object)
        If oldId IsNot Nothing Then
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

                Dim key As String = GetM2MKey(tt1, r.Rel.GetRealType(MappingEngine), r.Key)
                Dim dic As IDictionary = GetDic(_cache, key)
                Dim id As String = obj.Identifier.ToString
                'Dim sync As String = GetSync(key, id)

                If dic.Contains(id) Then
                    Dim m As M2MCache = CType(dic(id), M2MCache)

                    For Each oid As Integer In m.Entry.Current
                        Dim o As _IKeyEntity = CType(GetKeyEntityFromCacheOrCreate(oid, r.Rel.GetRealType(MappingEngine), False), _IKeyEntity)
                        M2MSubUpdate(o, obj, oldId, obj.GetType)
                    Next
                End If
            Next

            Dim c As OrmCache = TryCast(_cache, OrmCache)
            If c IsNot Nothing Then
                For Each rl As Relation In obj.GetAllRelation
                    Dim el As M2MRelation = TryCast(rl, M2MRelation)
                    'Dim p As Pair(Of String) = _cache.RemoveM2MQuery(el)
                    If el IsNot Nothing Then c.RemoveM2MQueries(el)

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
        mainobj.AddAccept(New AcceptState2(m, p.Second.First, p.Second.Second))
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

#End Region

    Friend Shared Function CreateReadonlyList(ByVal t As Type) As IListEdit
        Dim rt As Type = Nothing
        If GetType(IKeyEntity).IsAssignableFrom(t) Then
            rt = GetType(ReadOnlyList(Of ))
        ElseIf GetType(ICachedEntity).IsAssignableFrom(t) Then
            rt = GetType(ReadOnlyEntityList(Of ))
        Else
            rt = GetType(ReadOnlyObjectList(Of ))
        End If
        Return CType(Activator.CreateInstance(rt.MakeGenericType(New Type() {t})), IListEdit)
    End Function

    Friend Shared Function CreateReadonlyList(ByVal t As Type, ByVal l As IEnumerable) As IListEdit
        Dim rt As Type = Nothing
        If GetType(IKeyEntity).IsAssignableFrom(t) Then
            rt = GetType(ReadOnlyList(Of ))
        ElseIf GetType(ICachedEntity).IsAssignableFrom(t) Then
            rt = GetType(ReadOnlyEntityList(Of ))
        Else
            rt = GetType(ReadOnlyObjectList(Of ))
        End If
        Return CType(Activator.CreateInstance(rt.MakeGenericType(New Type() {t}), New Object() {l}), IListEdit)
    End Function

    Friend Shared Function CreateReadonlyList(ByVal t As Type, ByVal l As IEnumerable, ByVal et As Type) As IListEdit
        Dim rt As Type = Nothing
        If GetType(IKeyEntity).IsAssignableFrom(t) Then
            rt = GetType(ReadOnlyList(Of ))
        ElseIf GetType(ICachedEntity).IsAssignableFrom(t) Then
            rt = GetType(ReadOnlyEntityList(Of ))
        Else
            rt = GetType(ReadOnlyObjectList(Of ))
        End If
        Return CType(Activator.CreateInstance(rt.MakeGenericType(New Type() {t}), New Object() {et, l}), IListEdit)
    End Function

    Public Function ApplyFilter(Of T As {_IEntity})(ByVal col As ReadOnlyObjectList(Of T), ByVal filter As IFilter) As ReadOnlyObjectList(Of T)
        Dim evaluated As Boolean
        Dim r As ReadOnlyObjectList(Of T) = ApplyFilter(col, filter, evaluated)
        If Not evaluated Then
            Throw New InvalidOperationException("Filter is not applyable")
        End If
        Return r
    End Function

    Public Function ApplyFilter(Of T As {_IEntity})(ByVal col As ReadOnlyObjectList(Of T), ByVal filter As IFilter, ByRef evaluated As Boolean) As ReadOnlyObjectList(Of T)
        evaluated = True
        Dim f As IEntityFilter = TryCast(filter, IEntityFilter)
        If f Is Nothing Then
            Return col
        Else
            Dim l As IListEdit = CreateReadonlyList(GetType(T))
            Dim oschema As IEntitySchema = Nothing
            Dim i As Integer = 0
            For Each o As T In col
                If oschema Is Nothing Then
                    oschema = _schema.GetEntitySchema(o.GetType)
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
            Return CType(l, Global.Worm.ReadOnlyObjectList(Of T))
        End If
    End Function

    Public Shared Function ApplySort(Of T As {_IEntity})(ByVal c As ICollection(Of T), ByVal s As Sort, ByVal getObj As OrmComparer(Of T).GetObjectDelegate) As ICollection(Of T)
        Dim q As New Stack(Of Sort)
        If s IsNot Nothing AndAlso Not s.IsExternal Then
            For Each ns As Sort In New Sort.Iterator(s)
                If ns.IsExternal Then
                    Throw New ObjectMappingException("External sort must be alone")
                End If
                If ns.IsCustom Then
                    Throw New ObjectMappingException("Custom sort is not supported")
                End If
                q.Push(ns)
            Next

            Dim l As New List(Of T)(c)
            l.Sort(New OrmComparer(Of T)(q, getObj))
            c = l
        End If
        Return c
    End Function

    Public Shared Function ApplySort(Of T As {_IEntity})(ByVal c As ICollection(Of T), ByVal s As Sort) As ICollection(Of T)
        Return ApplySort(c, s, Nothing)
    End Function

    Public Shared Function ApplySortT(ByVal c As ICollection, ByVal s As Sort) As ICollection
        Dim q As New Stack(Of Sort)
        If s IsNot Nothing AndAlso Not s.IsExternal Then
            For Each ns As Sort In New Sort.Iterator(s)
                If ns.IsExternal Then
                    Throw New ObjectMappingException("External sort must be alone")
                End If
                If ns.IsCustom Then
                    Throw New ObjectMappingException("Custom sort is not supported")
                End If
                q.Push(ns)
            Next

            Dim l As New ArrayList(c)
            If l.Count > 0 Then
                l.Sort(New OrmComparer(Of _IEntity)(l(0).GetType, q))
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

    Public Function GetLoadedCount(ByVal t As Type, ByVal ids As IList(Of Object)) As Integer
        Dim r As Integer = 0
        'Dim dic As IDictionary = GetDictionary(t)
        For Each id As Integer In ids
            Dim o As IKeyEntity = GetKeyEntityFromCacheOrCreate(id, t, False)
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

    Public Function GetKeyFromPK(Of T As {New, IKeyEntity})(ByVal id As Object) As Integer
        Dim o As T = CreateKeyEntity(Of T)(id)
        Return o.Key
    End Function

    Public Function GetLoadedCount(Of T As {New, IKeyEntity})(ByVal ids As IList(Of Object)) As Integer
        Dim r As Integer = 0
        'Dim dic As IDictionary(Of Object, T) = GetDictionary(Of T)()
        For Each id As Object In ids
            Dim o As IKeyEntity = GetKeyEntityFromCacheOrCreate(Of T)(id, False)
            If o.IsLoaded OrElse o.ObjectState = ObjectState.Created Then
                'If dic.ContainsKey(GetKeyFromPK(Of T)(id)) Then
                r += 1
            End If
        Next
        Return r
    End Function

#Region " Search "

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

    'Public Function Search(Of T As {OrmBase, New})(ByVal [string] As String, _
    '    ByVal del As DbSchema.ValueForSearchDelegate) As ICollection(Of T)
    '    Invariant()

    '    If [string] IsNot Nothing AndAlso [string].Length > 0 Then
    '        Dim ss() As String = Split4FullTextSearch([string], GetSearchSection)
    '        Return SearchEx(Of T)(GetType(T), ss, Nothing, Nothing, Nothing, "containstable", Integer.MinValue, del)
    '    End If
    '    Return New List(Of T)()
    'End Function

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
                Dim obj As IEntity = CType(MappingEngine.GetPropertyValue(o, fields(i), oschema), IEntity)
                If obj IsNot Nothing Then
                    If prop_objs(i) Is Nothing Then
                        'prop_objs(i) = CType(Activator.CreateInstance(lt.MakeGenericType(obj.GetType)), IListEdit)
                        prop_objs(i) = CreateReadonlyList(obj.GetType)
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

    Protected Function LoadObjectsInternal(Of T As {IKeyEntity, New})(ByVal objs As ReadOnlyList(Of T), ByVal start As Integer, ByVal length As Integer, ByVal remove_not_found As Boolean, ByVal columns As Generic.List(Of EntityPropertyAttribute), ByVal withLoad As Boolean) As ReadOnlyList(Of T)
        Return LoadObjectsInternal(Of T, T)(objs, start, length, remove_not_found, columns, withLoad)
    End Function

    Public Function LoadObjects(Of T As {IKeyEntity, New})(ByVal objs As ReadOnlyList(Of T), ByVal start As Integer, ByVal length As Integer, ByVal columns As List(Of EntityPropertyAttribute)) As ReadOnlyList(Of T)
        Return LoadObjectsInternal(objs, start, length, True, columns, _schema.GetSortedFieldList(GetType(T)).Count = columns.Count)
    End Function

    Public Function LoadObjects(Of T As {IKeyEntity, New})(ByVal objs As ReadOnlyList(Of T), ByVal start As Integer, ByVal length As Integer) As ReadOnlyList(Of T)
        Return LoadObjectsInternal(objs, start, length, True)
    End Function

    Public Function LoadObjects(Of T As {IKeyEntity, New})(ByVal objs As ReadOnlyList(Of T)) As ReadOnlyList(Of T)
        Return LoadObjectsInternal(objs, 0, objs.Count, True)
    End Function

    Public Function LoadObjects(Of T As {IKeyEntity, New}, T2 As IKeyEntity)(ByVal objs As ReadOnlyList(Of T2), ByVal start As Integer, ByVal length As Integer) As ReadOnlyList(Of T2)
        Return LoadObjectsInternal(Of T, T2)(objs, start, length, True, _schema.GetSortedFieldList(GetType(T)), True)
    End Function

    Public Function LoadObjects(Of T As {IKeyEntity, New}, T2 As IKeyEntity)(ByVal objs As ReadOnlyList(Of T2)) As ReadOnlyList(Of T2)
        Return LoadObjectsInternal(Of T, T2)(objs, 0, objs.Count, True, _schema.GetSortedFieldList(GetType(T)), True)
    End Function

    Public Function LoadObjects(Of T2 As IKeyEntity)(ByVal realType As Type, ByVal objs As ReadOnlyList(Of T2), ByVal start As Integer, ByVal length As Integer) As ReadOnlyList(Of T2)
        Return LoadObjectsInternal(Of T2)(realType, objs, start, length, True, _schema.GetSortedFieldList(realType), True)
    End Function

    Public Function LoadObjects(Of T2 As IKeyEntity)(ByVal realType As Type, ByVal objs As ReadOnlyList(Of T2)) As ReadOnlyList(Of T2)
        Return LoadObjectsInternal(Of T2)(realType, objs, 0, objs.Count, True, _schema.GetSortedFieldList(realType), True)
    End Function

    Public Overridable Function ConvertIds2Objects(ByVal t As Type, ByVal ids As ICollection(Of Object), ByVal check As Boolean) As ICollection
        Dim self_t As Type = Me.GetType
        Dim mis() As Reflection.MemberInfo = self_t.GetMember("ConvertIds2Objects", Reflection.MemberTypes.Method, _
            Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public)
        For Each mmi As Reflection.MemberInfo In mis
            If TypeOf (mmi) Is Reflection.MethodInfo Then
                Dim mi As Reflection.MethodInfo = CType(mmi, Reflection.MethodInfo)
                If mi.IsGenericMethod AndAlso mi.GetParameters.Length = 2 Then
                    mi = mi.MakeGenericMethod(New Type() {t})
                    Return CType(mi.Invoke(Me, New Object() {ids, check}), System.Collections.ICollection)
                End If
            End If
        Next
        Throw New InvalidOperationException(String.Format("Method {0} not found", "ConvertIds2Objects"))
    End Function

    Public Overridable Function ConvertIds2Objects(ByVal t As Type, ByVal ids As ICollection(Of Object), ByVal start As Integer, ByVal length As Integer, ByVal check As Boolean) As ICollection
        Dim self_t As Type = Me.GetType
        Dim mis() As Reflection.MemberInfo = self_t.GetMember("ConvertIds2Objects", Reflection.MemberTypes.Method, _
            Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public)
        For Each mmi As Reflection.MemberInfo In mis
            If TypeOf (mmi) Is Reflection.MethodInfo Then
                Dim mi As Reflection.MethodInfo = CType(mmi, Reflection.MethodInfo)
                If mi.IsGenericMethod AndAlso mi.GetParameters.Length = 4 Then
                    mi = mi.MakeGenericMethod(New Type() {t})
                    Return CType(mi.Invoke(Me, New Object() {ids, start, length, check}), System.Collections.ICollection)
                End If
            End If
        Next
        Throw New InvalidOperationException(String.Format("Method {0} not found", "ConvertIds2Objects"))
    End Function

    Public Overridable Function ConvertIds2Objects(Of T As {IKeyEntity, New})(ByVal ids As ICollection(Of Object), ByVal check As Boolean) As ReadOnlyList(Of T)
        Dim arr As New ReadOnlyList(Of T)

        If Not check Then
            Dim type As Type = GetType(T)

            For Each id As Object In ids
                Dim obj As T = GetKeyEntityFromCacheOrCreate(Of T)(id, True)

                If obj IsNot Nothing Then
                    CType(arr, IListEdit).Add(obj)
                ElseIf _cache.NewObjectManager IsNot Nothing Then
                    obj = CType(_cache.NewObjectManager.GetNew(type, obj.GetPKValues), T)
                    If obj IsNot Nothing Then CType(arr, IListEdit).Add(obj)
                End If
            Next
        Else
            Dim r As ReadOnlyList(Of T) = ConvertIds2Objects(Of T)(ids, False)
            arr = LoadObjects(Of T)(r, 0, r.Count, New List(Of EntityPropertyAttribute)(New EntityPropertyAttribute() { _
                _schema.GetPrimaryKeys(GetType(T))(0) _
                }))
        End If
        Return arr
    End Function

    Public Overridable Function ConvertIds2Objects(Of T As {IKeyEntity, New})(ByVal ids As IList(Of Object), _
        ByVal start As Integer, ByVal length As Integer, ByVal check As Boolean) As ReadOnlyList(Of T)

        Dim arr As New ReadOnlyList(Of T)

        If Not check Then
            If start < ids.Count Then
                Dim type As Type = GetType(T)
                length = Math.Min(length + start, ids.Count)
                For i As Integer = start To length - 1
                    Dim id As Object = ids(i)
                    Dim obj As T = GetKeyEntityFromCacheOrCreate(Of T)(id, True)

                    If obj IsNot Nothing Then
                        CType(arr, IListEdit).Add(obj)
                    ElseIf _cache.NewObjectManager IsNot Nothing Then
                        obj = CType(_cache.NewObjectManager.GetNew(type, obj.GetPKValues), T)
                        If obj IsNot Nothing Then CType(arr, IListEdit).Add(obj)
                    End If
                Next
            End If
        Else
            Dim r As ReadOnlyList(Of T) = ConvertIds2Objects(Of T)(ids, start, length, False)
            arr = LoadObjects(Of T)(r, 0, r.Count, New List(Of EntityPropertyAttribute)(New EntityPropertyAttribute() { _
                _schema.GetPrimaryKeys(GetType(T))(0) _
                }))
        End If
        Return arr
    End Function

    Public Function LoadObjectsIds(Of T As {ICachedEntity})(ByVal tt As Type, ByVal ids As IList(Of Object), ByVal start As Integer, ByVal length As Integer) As ReadOnlyEntityList(Of T)
        Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public
        Dim mi As Reflection.MethodInfo = Me.GetType.GetMethod("LoadObjectsIds", flags, Nothing, Reflection.CallingConventions.Any, New Type() {GetType(IList(Of Object)), GetType(Integer), GetType(Integer)}, Nothing)
        Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {tt})
        Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {ids, start, length}, Nothing), ReadOnlyEntityList(Of T))
    End Function

    Public Function LoadObjectsIds(Of T As {IKeyEntity, New})(ByVal ids As ICollection(Of Object)) As ReadOnlyList(Of T)
        Return LoadObjects(Of T)(ConvertIds2Objects(Of T)(ids, False))
    End Function

    Public Function LoadObjectsIds(Of T As {IKeyEntity, New})(ByVal ids As IList(Of Object), ByVal start As Integer, ByVal length As Integer) As ReadOnlyList(Of T)
        Return LoadObjects(Of T)(ConvertIds2Objects(Of T)(ids, start, length, False))
    End Function

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
        Dim oldObj As ICachedEntity = Nothing
        Try
            If _cache.IsReadonly Then
                Throw New OrmManagerException("Cache is readonly")
            End If

            Dim b As Boolean = True
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
            Dim orm As _IKeyEntity = TryCast(obj, _IKeyEntity)
            'Using obj.GetSyncRoot
            Using GetSyncForSave(t, obj)
                Dim old_id As Object = Nothing
                Dim sa As SaveAction
                Dim state As ObjectState = obj.ObjectState
                If state = ObjectState.Created Then
                    old_id = orm.Identifier
                    sa = SaveAction.Insert
                End If

                If state = ObjectState.Deleted Then
                    sa = SaveAction.Delete
                End If
                Dim old_state As ObjectState = state
                Dim hasNew As Boolean = False
                Dim err As Boolean = True, ttt As Boolean
                Try
                    Dim processedType As New List(Of Type)

                    If sa = SaveAction.Delete Then
                        If orm IsNot Nothing Then
                            Dim toDel As New List(Of M2MRelation)
                            For Each r As M2MRelationDesc In MappingEngine.GetM2MRelations(t)
                                Dim acs As AcceptState2 = Nothing
                                If r.ConnectedType Is Nothing Then
                                    If r.DeleteCascade Then
                                        M2MDelete(orm, r.Rel.GetRealType(MappingEngine), r.Key)
                                        Dim cmd As RelationCmd = CType(orm, IRelations).GetCmd(r)
                                        If cmd IsNot Nothing Then
                                            cmd.RemoveAll(Me)
                                            toDel.Add(CType(cmd.Relation, M2MRelation))
                                        End If
                                    End If
                                    acs = M2MSave(orm, r.Rel.GetRealType(MappingEngine), r.Key)
                                    processedType.Add(r.Rel.GetRealType(MappingEngine))
                                End If
                                If acs IsNot Nothing Then CType(orm, _IKeyEntity).AddAccept(acs)
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
                            If oo IsNot Nothing Then
                                Dim o As New M2MEnum(oo, orm, MappingEngine)
                                CType(_cache, OrmCache).ConnectedEntityEnum(Me, t, AddressOf o.Remove)
                            End If
                        End If
                    End If

                    Dim saved As Boolean = obj.Save(Me)
                    If Not saved Then
                        ttt = True
                        Return True
                    End If

                    obj.RaiseSaved(sa)

                    If sa = SaveAction.Insert Then
                        If orm IsNot Nothing Then
                            Dim oo As IRelation = TryCast(MappingEngine.GetEntitySchema(t), IRelation)
                            If oo IsNot Nothing Then
                                Dim o As New M2MEnum(oo, orm, MappingEngine)
                                CType(_cache, OrmCache).ConnectedEntityEnum(Me, t, AddressOf o.Add)
                            End If

                            M2MUpdate(orm, old_id)

                            For Each r As M2MRelationDesc In MappingEngine.GetM2MRelations(t)
                                Dim tt As Type = r.Rel.GetRealType(MappingEngine)
                                If Not MappingEngine.IsMany2ManyReadonly(t, tt) Then
                                    Dim acs As AcceptState2 = M2MSave(orm, tt, r.Key)
                                    If acs IsNot Nothing Then
                                        hasNew = hasNew OrElse acs.el.HasNew
                                        'obj.AddAccept(acs)
                                    End If
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
                                        Throw New InvalidOperationException
                                    End If
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
                    End If

                    If AcceptChanges Then
                        If hasNew Then
                            Throw New OrmObjectException("Cannot accept changes. Some of relation has new objects")
                        End If
                        oldObj = obj.AcceptChanges(False, KeyEntity.IsGoodState(state))
                    End If

                    err = False
                Finally
                    If err Then
                        If sa = SaveAction.Insert AndAlso Not ttt Then
                            obj.RejectChanges(Me)
                        End If

                        state = old_state
                        'Else
                        '    obj.ObjSaved = True
                    End If
                End Try
                Return hasNew
            End Using
        Finally
            '            If obj.ObjSaved AndAlso AcceptChanges Then
            If AcceptChanges Then
                obj.UpdateCache(Me, oldObj)
            End If
        End Try
    End Function

    Public Function AddObject(ByVal obj As _ICachedEntity) As ICachedEntity
        Invariant()

        If obj Is Nothing Then
            Throw New ArgumentNullException("obj")
        End If

        Using obj.GetSyncRoot()
            If obj.ObjectState = ObjectState.Created OrElse obj.ObjectState = ObjectState.NotFoundInSource Then
                If Not InsertObject(obj) Then
                    Return Nothing
                End If
            ElseIf obj.ObjectState = ObjectState.Clone Then
                Throw New InvalidOperationException("Object with state " & obj.ObjectState.ToString & " cann't be added to cashe")
            End If
        End Using

        Return obj
    End Function

    Protected Friend Sub M2MSave(ByVal obj As IKeyEntity, ByVal el As M2MRelation)
        M2MSave(obj, el.Relation.Rel.GetRealType(MappingEngine), el.Key, el)
    End Sub

#Region " Abstract members "

    Protected MustOverride Function GetSearchSection() As String

    'Protected MustOverride Function SearchEx(Of T As {OrmBase, New})(ByVal type2search As Type, ByVal tokens() As String, _
    '    ByVal contextKey As Object, ByVal sort As Sort, ByVal filter As IFilter, ByVal ftsText As String, _
    '    ByVal limit As Integer, ByVal del As DbSchema.ValueForSearchDelegate) As ICollection(Of T)

    Protected MustOverride Function SearchEx(Of T As {IKeyEntity, New})(ByVal type2search As Type, _
        ByVal contextKey As Object, ByVal sort As Sort, ByVal filter As IFilter, ByVal ftsText As String, _
        ByVal limit As Integer, ByVal frmt As IFtsStringFormatter) As ReadOnlyList(Of T)

    Protected MustOverride Function Search(Of T As {IKeyEntity, New})( _
        ByVal type2search As Type, ByVal contextKey As Object, ByVal sort As Sort, ByVal filter As IFilter, _
        ByVal frmt As IFtsStringFormatter, Optional ByVal joins() As QueryJoin = Nothing) As ReadOnlyList(Of T)

    Protected Friend MustOverride Function GetStaticKey() As String

    Protected MustOverride Function GetCustDelegate(Of T As {IKeyEntity, New})(ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal key As String, ByVal id As String) As ICacheItemProvoder(Of T)

    Protected MustOverride Function GetCustDelegate(Of T As {IKeyEntity, New})(ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal key As String, ByVal id As String, ByVal cols() As String) As ICacheItemProvoder(Of T)

    Protected MustOverride Function GetCustDelegate(Of T As {IKeyEntity, New})(ByVal aspect As QueryAspect, ByVal join() As QueryJoin, ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal key As String, ByVal id As String, Optional ByVal cols As List(Of EntityPropertyAttribute) = Nothing) As ICacheItemProvoder(Of T)

    Protected MustOverride Function GetCustDelegate(Of T As {IKeyEntity, New})(ByVal relation As M2MRelationDesc, ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal key As String, ByVal id As String) As ICacheItemProvoder(Of T)

    'Protected MustOverride Function GetCustDelegate4Top(Of T As {OrmBase, New})(ByVal top As Integer, ByVal filter As IOrmFilter, _
    '    ByVal sort As Sort, ByVal key As String, ByVal id As String) As ICustDelegate(Of T)

    Protected MustOverride Function GetCustDelegate(Of T2 As {IKeyEntity, New})( _
        ByVal obj As _IKeyEntity, ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal queryAscpect() As QueryAspect, ByVal id As String, ByVal key As String, ByVal direct As String) As ICacheItemProvoder(Of T2)

    Protected MustOverride Function GetCustDelegate(Of T2 As {IKeyEntity, New})( _
        ByVal obj As _IKeyEntity, ByVal filter As IFilter, _
        ByVal sort As Sort, ByVal id As String, ByVal key As String, ByVal direct As String) As ICacheItemProvoder(Of T2)

    'Protected MustOverride Function GetCustDelegateTag(Of T As {OrmBase, New})( _
    '    ByVal obj As T, ByVal filter As IOrmFilter, ByVal sort As String, ByVal sortType As SortType, ByVal id As String, ByVal sync As String, ByVal key As String) As ICustDelegate(Of T)

    'Protected MustOverride Function GetDataTableInternal(ByVal t As Type, ByVal obj As OrmBase, ByVal filter As IOrmFilter, ByVal appendJoins As Boolean, Optional ByVal tag As String = Nothing) As System.Data.DataTable

    Protected MustOverride Function GetObjects(Of T As {IKeyEntity, New})(ByVal ids As Generic.IList(Of Object), ByVal f As IFilter, ByVal objs As List(Of T), _
       ByVal withLoad As Boolean, ByVal fieldName As String, ByVal idsSorted As Boolean) As Generic.IList(Of T)

    Protected MustOverride Function GetObjects(Of T As {IKeyEntity, New})(ByVal type As Type, ByVal ids As Generic.IList(Of Object), ByVal f As IFilter, _
       ByVal relation As M2MRelationDesc, ByVal idsSorted As Boolean, ByVal withLoad As Boolean) As IDictionary(Of Object, CachedM2MRelation)

    Protected Friend MustOverride Sub LoadObject(ByVal obj As _ICachedEntity, ByVal propertyAlias As String)
    Public MustOverride Function GetObjectFromStorage(ByVal obj As _ICachedEntity) As ICachedEntity
    Public MustOverride Function LoadObjectsInternal(Of T As {IKeyEntity, New}, T2 As {IKeyEntity})(ByVal objs As ReadOnlyList(Of T2), ByVal start As Integer, ByVal length As Integer, ByVal remove_not_found As Boolean, ByVal columns As Generic.List(Of EntityPropertyAttribute), ByVal withLoad As Boolean) As ReadOnlyList(Of T2)
    Public MustOverride Function LoadObjectsInternal(Of T2 As {IKeyEntity})(ByVal realType As Type, ByVal objs As ReadOnlyList(Of T2), ByVal start As Integer, ByVal length As Integer, ByVal remove_not_found As Boolean, ByVal columns As Generic.List(Of EntityPropertyAttribute), ByVal withLoad As Boolean) As ReadOnlyList(Of T2)

    'Protected MustOverride Overloads Sub FindObjects(ByVal t As Type, ByVal WithLoad As Boolean, ByVal arr As System.Collections.ArrayList, ByVal sort As String, ByVal sort_type As SortType)

    Public MustOverride Function UpdateObject(ByVal obj As _ICachedEntity) As Boolean

    Protected MustOverride Function InsertObject(ByVal obj As _ICachedEntity) As Boolean

    Protected Friend MustOverride Sub DeleteObject(ByVal obj As ICachedEntity)

    Protected MustOverride Sub M2MSave(ByVal obj As IKeyEntity, ByVal t As Type, ByVal key As String, ByVal el As M2MRelation)

    'Protected MustOverride Function FindObmsByOwnerInternal(ByVal id As Integer, ByVal original_type As Type, ByVal type As Type, ByVal sort As String, ByVal sort_type As SortType) As OrmBase()

    'Protected MustOverride Function FindTopInternal(ByVal original_type As Type, ByVal type As Type, ByVal top As Integer, ByVal sort As String, ByVal sort_type As SortType) As OrmBase()

    'Public MustOverride Function CheckObjects(ByVal objs() As OrmBase) As OrmBase()

    'Protected MustOverride Function FindObjsDirect(ByVal obj() As OrmBase, ByVal filter_key As String, _
    '    ByVal withLoad As Boolean, ByVal filter As OrmFilter, _
    '    ByVal t As Type, ByVal JoinColumn As String, _
    '    ByVal columns As Generic.List(Of EntityPropertyAttribute)) As OrmBase()

    'Protected MustOverride Sub Obj2ObjRelationSave2(ByVal obj As OrmBase, ByVal dt As System.Data.DataTable, ByVal sync As String, ByVal t As System.Type)

    Protected Friend MustOverride ReadOnly Property Exec() As TimeSpan
    Protected Friend MustOverride ReadOnly Property Fecth() As TimeSpan

#End Region

    Protected MustOverride Function BuildDictionary(Of T As {New, IKeyEntity})(ByVal level As Integer, ByVal filter As IFilter, ByVal joins() As QueryJoin) As DicIndex(Of T)
    Protected MustOverride Function BuildDictionary(Of T As {New, IKeyEntity})(ByVal level As Integer, ByVal filter As IFilter, ByVal joins() As QueryJoin, ByVal firstField As String, ByVal secondField As String) As DicIndex(Of T)

    Protected Friend Function LoadObjectsInternal(Of T As {IKeyEntity, New})( _
            ByVal objs As ReadOnlyList(Of T), ByVal start As Integer, ByVal length As Integer, _
            ByVal remove_not_found As Boolean) As ReadOnlyList(Of T)
        Dim original_type As Type = GetType(T)
        Dim columns As Generic.List(Of EntityPropertyAttribute) = _schema.GetSortedFieldList(original_type)

        Return LoadObjectsInternal(Of T)(objs, start, length, remove_not_found, columns, True)
    End Function

    Protected Friend Sub RegisterInCashe(ByVal obj As _ICachedEntity)
        If Not IsInCachePrecise(obj) Then
            Add2Cache(obj)
            If obj.OriginalCopy() IsNot Nothing Then
                'Dim c As OrmCache = TryCast(_cache, OrmCache)
                'If c IsNot Nothing Then
                '    c.RegisterExistingModification(obj)
                'End If
                Dim r As ObjectModification.ReasonEnum
                Select Case obj.ObjectState
                    Case ObjectState.Deleted
                        r = ObjectModification.ReasonEnum.Delete
                    Case ObjectState.Modified
                        r = ObjectModification.ReasonEnum.Edit
                End Select
                _cache.RegisterModification(Me, obj, r)
            End If
        End If
    End Sub

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

    Public Shared Sub BuildDic(Of T As {New, DicIndexT(Of T2)}, T2 As {New, _IEntity})(ByVal name As String, ByVal cnt As Integer, _
        ByVal level As Integer, ByVal root As T, ByRef last As T, _
        ByRef first As Boolean, ByVal firstField As String, ByVal secField As String)
        'If name.Length = 0 Then name = "<без имени>"

        Dim current As T = Nothing

        Dim p As T = Nothing
        If Not first Then
            Dim i As Integer = FirstDiffCharIndex(name, last.Name)
            Debug.Assert(i <= level)

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
        If p Is root And name <> "" Then
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
        Else
            current = New T
            DicIndexT(Of T2).Init(current, name, p, cnt, firstField, secField, root.Cmd)
            p.AddChild(current)
            root.Add2Dictionary(current)
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

    Protected Friend Shared Function HasJoins(ByVal schema As ObjectMappingEngine, ByVal selectType As Type, _
        ByRef filter As IFilter, ByVal s As Sort, ByVal filterInfo As Object, ByRef joins() As QueryJoin, _
        ByRef appendMain As Boolean) As Boolean
        Dim l As New List(Of QueryJoin)
        Dim oschema As IEntitySchema = schema.GetEntitySchema(selectType)
        Dim ictx As IContextObjectSchema = TryCast(oschema, IContextObjectSchema)
        If ictx IsNot Nothing AndAlso ictx.GetContextFilter(filterInfo) IsNot Nothing Then
            appendMain = True
        End If
        Dim types As New List(Of Type)
        If filter IsNot Nothing Then
            For Each fl As IFilter In filter.Filter.GetAllFilters
                Dim f As IEntityFilter = TryCast(fl, IEntityFilter)
                If f IsNot Nothing Then
                    Dim ot As OrmFilterTemplate = CType(f.Template, OrmFilterTemplate)
                    Dim type2join As System.Type = ot.ObjectSource.GetRealType(schema)

                    'If type2join Is Nothing AndAlso Not String.IsNullOrEmpty(ot.EntityName) Then
                    '    type2join = schema.GetTypeByEntityName(ot.EntityName)
                    'End If

                    If type2join Is Nothing Then
                        Throw New NullReferenceException("Type for OrmFilterTemplate must be specified")
                    End If

                    If selectType Is type2join Then
                        appendMain = True
                    Else
                        Dim s2 As IEntitySchema = schema.GetEntitySchema(type2join)
                        If oschema.Equals(s2) OrElse oschema.GetType.FullName = s2.GetType.FullName Then
                            appendMain = True
                        Else
                            If Not types.Contains(type2join) Then
                                AppendJoin(schema, selectType, filter, filterInfo, l, oschema, types, type2join, s2)
                            End If
                        End If
                    End If
                End If
            Next
        End If

        If s IsNot Nothing Then
            For Each ns As Sort In New Sort.Iterator(s)
                If ns.ObjectSource IsNot Nothing Then
                    Dim sortType As System.Type = ns.ObjectSource.GetRealType(schema)
                    If sortType IsNot selectType AndAlso sortType IsNot Nothing AndAlso Not types.Contains(sortType) Then
                        Dim field As String = schema.GetJoinFieldNameByType(selectType, sortType, oschema)

                        If Not String.IsNullOrEmpty(field) Then
                            types.Add(sortType)
                            l.Add(MakeJoin(schema, sortType, selectType, field, FilterOperation.Equal, JoinType.Join))
                            Continue For
                        End If

                        'Dim sschema As IOrmObjectSchemaBase = _schema.GetObjectSchema(sortType)
                        'field = _schema.GetJoinFieldNameByType(sortType, selectType, sschema)
                        If String.IsNullOrEmpty(field) Then
                            Dim m2m As M2MRelationDesc = schema.GetM2MRelation(sortType, selectType, True)
                            If m2m IsNot Nothing Then
                                l.AddRange(MakeM2MJoin(schema, m2m, sortType))
                            Else
                                Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, sortType))
                            End If
                        End If

                        'types.Add(sortType)
                        'l.Add(MakeJoin(selectType, sortType, field, FilterOperation.Equal, JoinType.Join, True))
                    ElseIf sortType Is selectType OrElse sortType Is Nothing Then
                        appendMain = True
                    End If
                End If
            Next
        End If
        joins = l.ToArray
        Return joins.Length > 0
    End Function

    Public Shared Sub AppendJoin(ByVal schema As ObjectMappingEngine, ByVal selectType As Type, _
        ByRef filter As IFilter, ByVal filterInfo As Object, ByVal l As List(Of QueryJoin), _
        ByVal selSchema As IEntitySchema, ByVal types As List(Of Type), ByVal type2join As System.Type, _
        ByVal t2jSchema As IEntitySchema)

        Dim field As String = schema.GetJoinFieldNameByType(selectType, type2join, selSchema)

        If String.IsNullOrEmpty(field) Then

            field = schema.GetJoinFieldNameByType(type2join, selectType, t2jSchema)

            If String.IsNullOrEmpty(field) Then
                Dim m2m As M2MRelationDesc = schema.GetM2MRelation(type2join, selectType, True)
                If m2m IsNot Nothing Then
                    l.AddRange(MakeM2MJoin(schema, m2m, type2join))
                Else
                    Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2join))
                End If
            Else
                l.Add(MakeJoin(schema, selectType, type2join, field, FilterOperation.Equal, JoinType.Join, True))
            End If
        Else
            l.Add(MakeJoin(schema, type2join, selectType, field, FilterOperation.Equal, JoinType.Join))
        End If

        If types IsNot Nothing Then
            types.Add(type2join)
        End If

        Dim ts As IMultiTableObjectSchema = TryCast(t2jSchema, IMultiTableObjectSchema)
        If ts IsNot Nothing Then
            Dim pk_table As SourceFragment = t2jSchema.Table
            For i As Integer = 1 To ts.GetTables.Length - 1
                Dim joinableTs As IGetJoinsWithContext = TryCast(ts, IGetJoinsWithContext)
                Dim join As QueryJoin = Nothing
                If joinableTs IsNot Nothing Then
                    join = joinableTs.GetJoins(pk_table, ts.GetTables(i), filterInfo)
                Else
                    join = ts.GetJoins(pk_table, ts.GetTables(i))
                End If

                If Not QueryJoin.IsEmpty(join) Then
                    l.Add(join)
                End If
            Next

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
        End If
    End Sub

    Protected Friend Shared Function MakeM2MJoin(ByVal schema As ObjectMappingEngine, ByVal m2m As M2MRelationDesc, ByVal type2join As Type) As Worm.Criteria.Joins.QueryJoin()
        Dim jf As New JoinFilter(m2m.Table, m2m.Column, m2m.Rel.GetRealType(schema), schema.GetPrimaryKeys(m2m.Rel.GetRealType(schema))(0).PropertyAlias, Worm.Criteria.FilterOperation.Equal)
        Dim mj As New QueryJoin(m2m.Table, Joins.JoinType.Join, jf)
        m2m = schema.GetM2MRelation(m2m.Rel.GetRealType(schema), type2join, True)
        Dim jt As New JoinFilter(m2m.Table, m2m.Column, type2join, schema.GetPrimaryKeys(type2join)(0).PropertyAlias, Worm.Criteria.FilterOperation.Equal)
        Dim tj As New QueryJoin(schema.GetTables(type2join)(0), Joins.JoinType.Join, jt)
        Return New QueryJoin() {mj, tj}
    End Function

    Protected Friend Shared Function MakeJoin(ByVal schema As ObjectMappingEngine, ByVal type2join As Type, ByVal selectType As Type, ByVal field As String, _
           ByVal oper As Worm.Criteria.FilterOperation, ByVal joinType As Joins.JoinType, Optional ByVal switchTable As Boolean = False) As Worm.Criteria.Joins.QueryJoin

        'Dim tbl As SourceFragment = GetTables(type2join)(0)
        'If switchTable Then
        '    tbl = GetTables(selectType)(0)
        'End If

        Dim jf As New JoinFilter(type2join, schema.GetPrimaryKeys(type2join)(0).PropertyAlias, selectType, field, oper)

        Dim t As Type = type2join
        If switchTable Then
            t = selectType
        End If

        Return New QueryJoin(t, joinType, jf)
    End Function
End Class

'End Namespace
