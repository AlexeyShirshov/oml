Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta

Namespace Cache
    Public Enum CacheListBehavior
        CacheAll
        CacheOrThrowException
        CacheWhatCan
    End Enum

    Public Enum ValidateBehavior
        Immediate
        Deferred
    End Enum

    Public MustInherit Class CacheBase
        Implements IExploreQueryCache

        Public ReadOnly DateTimeCreated As Date

        Private _filters As IDictionary
        Private _loadTimes As New Dictionary(Of Type, Pair(Of Integer, TimeSpan))
        Private _lock As New Object
        Private _lock2 As New Object
        Private _list_converter As IListObjectConverter
        Private _modifiedobjects As IDictionary
        Private _externalObjects As IDictionary
        Private _newMgr As INewObjectsStore = New NewObjectStore
        Private _m2m_dep As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Object)))

        Public Event RegisterEntityCreation(ByVal sender As CacheBase, ByVal e As IEntity)
        'Public Event RegisterObjectCreation(ByVal sender As CacheBase, ByVal t As Type, ByVal id As Integer)
        Public Event RegisterObjectRemoval(ByVal sender As CacheBase, ByVal obj As ICachedEntity)

        Public Event RegisterCollectionCreation(ByVal sender As CacheBase, ByVal t As Type)
        Public Event RegisterCollectionRemoval(ByVal sender As CacheBase, ByVal ce As CachedItemBase)
        Public Event CacheHasModification(ByVal sender As CacheBase)
        Public Event CacheHasnotModification(ByVal sender As CacheBase)

        Public Event BeginUpdate(ByVal sender As CacheBase, ByVal o As ICachedEntity)
        Public Event BeginDelete(ByVal sender As CacheBase, ByVal o As ICachedEntity)

        Sub New()
            _filters = CreateRootDictionary4Queries()
            DateTimeCreated = Now
            _list_converter = CreateListConverter()
            _modifiedobjects = Hashtable.Synchronized(New Hashtable)
            _externalObjects = CreateRootDictionary4ExternalObjects()
        End Sub

        Public MustOverride Function CreateResultsetsDictionary() As IDictionary

        Public MustOverride Function GetOrmDictionary(ByVal t As Type, ByVal mpe As ObjectMappingEngine) As System.Collections.IDictionary

        Public MustOverride Function GetOrmDictionary(Of T As _ICachedEntity)(ByVal mpe As ObjectMappingEngine) As System.Collections.Generic.IDictionary(Of Object, T)

        Public MustOverride Function GetOrmDictionary(ByVal t As Type, ByVal cb As ICacheBehavior) As System.Collections.IDictionary

        Public MustOverride Function GetOrmDictionary(Of T As _ICachedEntity)(ByVal cb As ICacheBehavior) As System.Collections.Generic.IDictionary(Of Object, T)

        Public Overridable Sub Reset()
            _filters = CreateRootDictionary4Queries()
            _externalObjects = CreateRootDictionary4ExternalObjects()
        End Sub

        Public Overridable Function CreateRootDictionary4Queries() As IDictionary
            Return Hashtable.Synchronized(New Hashtable)
        End Function

        Public Overridable Function CreateRootDictionary4ExternalObjects() As IDictionary
            Return Hashtable.Synchronized(New Hashtable)
        End Function

        Public Overridable Function CreateResultsets4SimpleValuesDictionary() As IDictionary
            Return CreateResultsetsDictionary()
        End Function

        Public Overridable Function CreateResultsets4AnonymValuesDictionary() As IDictionary
            Return CreateResultsetsDictionary()
        End Function

        Public Overridable Function CreateResultsetsDictionary(ByVal mark As String) As IDictionary
            If String.IsNullOrEmpty(mark) Then
                Return CreateResultsetsDictionary()
            End If
            Throw New NotImplementedException(String.Format("Mark {0} is not supported", mark))
        End Function

        Public Overridable Sub RegisterCreation(ByVal obj As IEntity)
            RaiseEvent RegisterEntityCreation(Me, obj)
        End Sub

        '        Public Overridable Sub RegisterCreation(ByVal t As Type, ByVal id As Integer)
        '            RaiseEvent RegisterObjectCreation(Me, t, id)
        '#If TraceCreation Then
        '            _added.add(new Pair(Of date,Pair(Of type,Integer))(Now,New Pair(Of type,Integer)(t,id)))
        '#End If
        '        End Sub

#If TraceCreation Then
        Private _added As ArrayList = ArrayList.Synchronized(New ArrayList)
        Private _removed As ArrayList = ArrayList.Synchronized(New ArrayList)

        Private Function IndexOfRemoved(ByVal obj As _ICachedEntity) As Integer
            For i As Integer = 0 To _removed.Count - 1
                Dim r As Pair(Of Date, _ICachedEntity) = CType(_removed(i), Pair(Of Date, _ICachedEntity))
                If r.Second = obj Then
                    Return i
                End If
            Next
        End Function

        Private Function IndexOfAdded(ByVal obj As _ICachedEntity) As Integer
            For i As Integer = 0 To _added.Count - 1
                Dim r As Pair(Of Date, Pair(Of Type, Integer)) = CType(_added(i), Pair(Of Date, Pair(Of Global.System.Type, Integer)))
                If r.Second.First.Equals(obj.GetType) AndAlso r.Second.Second = obj.Identifier Then
                    Return i
                End If
            Next
        End Function
#End If

        Public Overridable Sub RegisterRemoval(ByVal obj As _ICachedEntity, ByVal mpe As ObjectMappingEngine, ByVal cb As ICacheBehavior)
            If cb Is Nothing Then
                cb = TryCast(obj.GetEntitySchema(mpe), ICacheBehavior)
            End If
            Dim sc As ObjectModification = ShadowCopy(obj.GetType, obj, cb)
            If sc IsNot Nothing Then
                Dim st As String = String.Empty
#If DEBUG Then
                st = sc.StackTrace
#End If
                Throw New OrmManagerException(String.Format("Object is going to die {0}. Stack {1}", sc.Obj, st))
            End If

            RaiseEvent RegisterObjectRemoval(Me, obj)
            'obj.RemoveOriginalCopy(Me)
#If TraceCreation Then
            _removed.add(new Pair(Of date,_ICachedEntity)(Now,obj))
#End If
        End Sub

        Public Overridable Property CacheListBehavior() As CacheListBehavior
            Get
                Return Cache.CacheListBehavior.CacheAll
            End Get
            Set(ByVal value As CacheListBehavior)
                'do nothing
            End Set
        End Property

        Public Overridable ReadOnly Property IsReadonly() As Boolean
            Get
                Return True
            End Get
        End Property

        Protected Overridable Function CreateListConverter() As IListObjectConverter
            Return New FakeListConverter
        End Function

        Public ReadOnly Property ListConverter() As IListObjectConverter
            Get
                Return _list_converter
            End Get
        End Property

        Public Overridable Sub RegisterCreationCacheItem(ByVal t As Type)
            RaiseEvent RegisterCollectionCreation(Me, t)
        End Sub

        Public Overridable Sub RegisterRemovalCacheItem(ByVal ce As CachedItemBase)
            RaiseEvent RegisterCollectionRemoval(Me, ce)
        End Sub

        Public Overridable Sub RemoveEntry(ByVal key As String, ByVal id As String)
            Dim dic As IDictionary = CType(_filters(key), System.Collections.IDictionary)
            If dic IsNot Nothing Then
                Dim ce As CachedItemBase = TryCast(dic(id), CachedItemBase)
                dic.Remove(id)
                If ce IsNot Nothing Then
                    RegisterRemovalCacheItem(ce)
                End If
                If dic.Count = 0 Then
                    Using SyncHelper.AcquireDynamicLock(key)
                        If dic.Count = 0 Then
                            _filters.Remove(key)
                        End If
                    End Using
                End If
            End If
        End Sub

        Public Sub RemoveEntry(ByVal p As Pair(Of String))
            RemoveEntry(p.First, p.Second)
        End Sub

        Protected Function _GetDictionary(ByVal key As String) As IDictionary
            Return CType(_filters(key), IDictionary)
        End Function

        Public Function GetAnonymDictionary(ByVal key As String) As IDictionary
            Dim dic As IDictionary = CType(_filters(key), IDictionary)
            If dic Is Nothing Then
                Using SyncHelper.AcquireDynamicLock(key)
                    dic = CType(_filters(key), IDictionary)
                    If dic Is Nothing Then
                        dic = CreateResultsets4AnonymValuesDictionary()
                        _filters.Add(key, dic)
                    End If
                End Using
            End If
            Return dic
        End Function

        Public Function GetSimpleDictionary(ByVal key As String) As IDictionary
            Dim dic As IDictionary = CType(_filters(key), IDictionary)
            If dic Is Nothing Then
                Using SyncHelper.AcquireDynamicLock(key)
                    dic = CType(_filters(key), IDictionary)
                    If dic Is Nothing Then
                        dic = CreateResultsets4SimpleValuesDictionary()
                        _filters.Add(key, dic)
                    End If
                End Using
            End If
            Return dic
        End Function

        Public Function GetDictionary(ByVal key As String) As IDictionary
            Dim dic As IDictionary = CType(_filters(key), IDictionary)
            If dic Is Nothing Then
                Using SyncHelper.AcquireDynamicLock(key)
                    dic = CType(_filters(key), IDictionary)
                    If dic Is Nothing Then
                        dic = CreateResultsetsDictionary()
                        _filters.Add(key, dic)
                    End If
                End Using
            End If
            Return dic
        End Function

        Public Function GetDictionary(ByVal key As String, ByVal mark As String, ByRef created As Boolean) As IDictionary
            Dim dic As IDictionary = CType(_filters(key), IDictionary)
            created = False
            If dic Is Nothing Then
                Using SyncHelper.AcquireDynamicLock(key)
                    dic = CType(_filters(key), IDictionary)
                    If dic Is Nothing Then
                        dic = CreateResultsetsDictionary(mark)
                        _filters.Add(key, dic)
                        created = True
                    End If
                End Using
            End If
            Return dic
        End Function

        Public ReadOnly Property SyncRoot() As IDisposable
            Get
#If DebugLocks Then
                Return New CSScopeMgr_Debug(_lock, "d:\temp\")
#Else
                Return New CSScopeMgr(_lock)
#End If
            End Get
        End Property

        Friend ReadOnly Property SyncRoot2() As IDisposable
            Get
#If DebugLocks Then
                Return New CSScopeMgr_Debug(_lock2, "d:\temp\")
#Else
                Return New CSScopeMgr(_lock2)
#End If
            End Get
        End Property

        Public Function ShadowCopy(ByVal objectType As Type, ByVal kp As IKeyProvider, ByVal cb As ICacheBehavior) As ObjectModification
            Using SyncRoot
                If kp Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                Dim name As String = GetModificationKey(objectType, kp, cb)
                Return CType(_modifiedobjects(name), ObjectModification)
            End Using
        End Function

        Public Function ShadowCopy(ByVal objectType As Type, ByVal obj As IKeyProvider) As ObjectModification
            Return ShadowCopy(objectType, obj, Nothing)
        End Function

        Protected Shared Sub Assert(ByVal condition As Boolean, ByVal message As String)
            Debug.Assert(condition, message)
            Trace.Assert(condition, message)
            If Not condition Then Throw New OrmCacheException(message)
        End Sub

        'Protected Function GetModificationKey(ByVal obj As _ICachedEntity, ByVal mpe As ObjectMappingEngine, _
        '    ByVal context As Object, ByVal oschema As IEntitySchema) As String
        '    Return GetModificationKey(obj, mpe, context, oschema)
        'End Function

        Protected Function GetModificationKey(ByVal objectType As Type, ByVal kp As IKeyProvider, ByVal cb As ICacheBehavior) As String
            Return ObjectMappingEngine.GetEntityTypeKey(objectType, cb).ToString & ":" & kp.UniqueString
        End Function

        Protected Friend Function RegisterModification(ByVal mgr As OrmManager, ByVal obj As _ICachedEntity, _
            ByVal reason As ObjectModification.ReasonEnum, ByVal cb As ICacheBehavior) As ObjectModification
            Return RegisterModification(mgr, obj, Nothing, reason, cb)
        End Function

        Protected Friend Function RegisterModification(ByVal mgr As OrmManager, ByVal obj As _ICachedEntity, _
            ByVal pk() As PKDesc, ByVal reason As ObjectModification.ReasonEnum, ByVal cb As ICacheBehavior) As ObjectModification
            Using SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                Dim key As String = GetModificationKey(obj.GetType, obj, cb)
                'Using SyncHelper.AcquireDynamicLock(name)
                'Assert(mgr IsNot Nothing, "You have to create MediaContent object to perform this operation")
                Assert(Not _modifiedobjects.Contains(key), "Key " & key & " already in collection")
                Dim mo As New ObjectModification(obj, mgr.CurrentUser, reason, pk)
                _modifiedobjects.Add(key, mo)
                'End Using
                If _modifiedobjects.Count = 1 Then
                    RaiseEvent CacheHasModification(Me)
                End If

                Select Case reason
                    Case ObjectModification.ReasonEnum.Delete
                        RaiseBeginDelete(obj)
                    Case ObjectModification.ReasonEnum.Edit
                        If Not obj.IsLoading Then
                            RaiseBeginUpdate(obj)
                        End If
                End Select

                Return mo
            End Using
        End Function

        Public Function GetModifiedObjects(Of T As {New, _ICachedEntity})(ByVal mgr As OrmManager) As IList(Of T)
            Dim al As New Generic.List(Of T)
            Dim tt As Type = GetType(T)
            'For Each s As String In New ArrayList(_modifiedobjects.Keys)
            '    If s.IndexOf(tt.Name & ":") >= 0 Then
            '        Dim mo As ObjectModification = CType(_modifiedobjects(s), ObjectModification)
            '        If mo IsNot Nothing Then
            '            al.Add(CType(mo.Obj, T))
            '        End If
            '    End If
            'Next
            For Each mo As ObjectModification In New ArrayList(_modifiedobjects.Values)
                'If tt Is mo.Proxy.EntityType Then
                '    al.Add(mgr.GetEntityOrOrmFromCacheOrCreate(Of T)(mo.Proxy.PK))
                'End If
                If tt Is mo.Obj.GetType Then
                    al.Add(CType(mo.Obj, T))
                End If
            Next
            Return al
        End Function

        Protected Friend Sub RaiseBeginUpdate(ByVal o As ICachedEntity)
            RaiseEvent BeginUpdate(Me, o)
        End Sub

        Protected Friend Sub RaiseBeginDelete(ByVal o As ICachedEntity)
            RaiseEvent BeginDelete(Me, o)
        End Sub

        'Protected Friend Sub RegisterExistingModification(ByVal obj As _ICachedEntity, ByVal key As Integer)
        '    Using SyncRoot
        '        If obj Is Nothing Then
        '            Throw New ArgumentNullException("obj")
        '        End If

        '        Dim moKey As String = GetModificationKey(obj, key)
        '        _modifiedobjects.Add(moKey, ShadowCopy(obj).Obj)
        '        If _modifiedobjects.Count = 1 Then
        '            RaiseEvent CacheHasModification(Me, EventArgs.Empty)
        '        End If
        '    End Using
        'End Sub

#If TraceCreation Then
        Private _s As New List(Of Pair(Of String, _ICachedEntity))
        Public Function IndexOfUnreg(ByVal o As _ICachedEntity) As Integer
            For i As Integer = 0 To _s.Count - 1
                If _s(i).Second = o Then
                    Return i
                End If
            Next
        End Function
#End If
        Protected Friend Sub UnregisterModification(ByVal obj As _ICachedEntity, _
            ByVal mpe As ObjectMappingEngine, ByVal cb As ICacheBehavior)
            Using SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                If _modifiedobjects.Count > 0 Then
                    Dim key As String = GetModificationKey(obj.GetType, obj, cb)
                    _modifiedobjects.Remove(key)
                    obj.RaiseCopyRemoved()
#If TraceCreation Then
                    _s.Add(New Pair(Of String, _ICachedEntity)(Environment.StackTrace, obj))
#End If
                    If _modifiedobjects.Count = 0 Then 'AndAlso obj.old_state <> ObjectState.Created Then
                        RaiseEvent CacheHasnotModification(Me)
                    End If
                    'If obj.ObjectState = ObjectState.Modified Then
                    '    Throw New OrmCacheException("Unregistered object must not be in modified state")
                    'End If
                End If
            End Using
        End Sub

        Public ReadOnly Property IsModified() As Boolean
            Get
                Using SyncRoot
                    Return _modifiedobjects.Count <> 0
                End Using
            End Get
        End Property

        Public Function GetLoadTime(ByVal t As Type) As Pair(Of Integer, TimeSpan)
            Dim p As Pair(Of Integer, TimeSpan) = Nothing
            _loadTimes.TryGetValue(t, p)
            Return p
        End Function

        Protected Friend Sub LogLoadTime(ByVal obj As IEntity, ByVal time As TimeSpan)
            Dim t As Type = obj.GetType
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("q89rbvadfkqerog" ,"d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("q89rbvadfkqerog")
#End If

                Dim p As Pair(Of Integer, TimeSpan) = Nothing
                If _loadTimes.TryGetValue(t, p) Then
                    _loadTimes(t) = New Pair(Of Integer, TimeSpan)(p.First + 1, p.Second.Add(time))
                Else
                    _loadTimes(t) = New Pair(Of Integer, TimeSpan)(1, time)
                End If
            End Using
        End Sub

        Public Sub AddExternalObject(ByVal objectStoreName As String, ByVal obj As Object)
            _externalObjects(objectStoreName) = obj
        End Sub

        Public Delegate Function GetObjectDelegate(Of T)() As T

        Public Function GetExternalObject(Of T)(ByVal objectStoreName As String, Optional ByVal getObj As GetObjectDelegate(Of T) = Nothing) As T
            Dim o As T = Nothing
            SyncLock _externalObjects.SyncRoot
                o = CType(_externalObjects(objectStoreName), T)
                If o Is Nothing AndAlso getObj IsNot Nothing Then
                    o = getObj()
                    _externalObjects(objectStoreName) = o
                End If
            End SyncLock
            Return o
        End Function

        Public Sub RemoveExternalObject(ByVal objectStoreName As String)
            _externalObjects.Remove(objectStoreName)
        End Sub

        Protected Friend Sub AddM2MObjDependent(ByVal obj As _ISinglePKEntity, ByVal key As String, ByVal id As String)
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("bhiasdbvgklbg135t","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("bhiasdbvgklbg135t")
#End If
                Dim l As Dictionary(Of String, Dictionary(Of String, Object)) = Nothing

                If _m2m_dep.TryGetValue(obj.GetName, l) Then
                    Dim ll As Dictionary(Of String, Object) = Nothing
                    If l.TryGetValue(key, ll) Then
                        If Not ll.ContainsKey(id) Then
                            ll.Add(id, Nothing)
                        End If
                    Else
                        ll = New Dictionary(Of String, Object)
                        ll.Add(id, Nothing)
                        l.Add(key, ll)
                    End If
                Else
                    l = New Dictionary(Of String, Dictionary(Of String, Object))
                    Dim ll As New Dictionary(Of String, Object)
                    ll.Add(id, Nothing)
                    l.Add(key, ll)
                    _m2m_dep.Add(obj.GetName, l)
                End If
            End Using
        End Sub


#If OLDM2M Then
        Protected Friend Function GetM2MEntries(ByVal obj As _IKeyEntity, ByVal name As String) As ICollection(Of Pair(Of M2MCache, Pair(Of String, String)))
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            If String.IsNullOrEmpty(name) Then
                name = obj.GetName
            End If

#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("bhiasdbvgklbg135t", "d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("bhiasdbvgklbg135t")
#End If
                Dim etrs As New List(Of Pair(Of M2MCache, Pair(Of String, String)))
                Dim l As Dictionary(Of String, Dictionary(Of String, Object)) = Nothing

                If _m2m_dep.TryGetValue(name, l) Then
                    For Each p As KeyValuePair(Of String, Dictionary(Of String, Object)) In l
                        Dim dic As IDictionary = _GetDictionary(p.Key)
                        If dic IsNot Nothing Then
                            For Each id As String In p.Value.Keys
                                Dim ce As M2MCache = TryCast(dic(id), M2MCache)
                                If ce Is Nothing Then
                                    'dic.Remove(id)
                                    RemoveEntry(p.Key, id)
                                Else
                                    etrs.Add(New Pair(Of M2MCache, Pair(Of String, String))(ce, New Pair(Of String, String)(p.Key, id)))
                                End If
                            Next
                        End If
                    Next
                End If
                Return etrs
            End Using
        End Function
#End If
        Protected Friend Sub UpdateM2MEntries(ByVal obj As _ISinglePKEntity, ByVal oldId As Object, ByVal name As String)
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            If String.IsNullOrEmpty(name) Then
                Throw New ArgumentNullException("name")
            End If

#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("bhiasdbvgklbg135t", "d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("bhiasdbvgklbg135t")
#End If
                Dim l As Dictionary(Of String, Dictionary(Of String, Object)) = Nothing
                If _m2m_dep.TryGetValue(name, l) Then
                    _m2m_dep.Remove(name)
                    _m2m_dep.Add(obj.GetName, l)

                    For Each kv As KeyValuePair(Of String, Dictionary(Of String, Object)) In l
                        Dim d As Dictionary(Of String, Object) = kv.Value
                        d.Remove(oldId.ToString)
                        d.Add(obj.Identifier.ToString, Nothing)
                    Next
                End If
            End Using
        End Sub

        Public Function CustomObject(Of T As {New, Class})(ByVal o As T) As ICachedEntity
            Throw New NotImplementedException
        End Function

        Public Function GetKeyFromPK(Of T As {New, ISinglePKEntity})(ByVal id As Object) As Integer
            Dim o As T = SinglePKEntity.CreateKeyEntity(Of T)(id, Me, Nothing)
            Return o.Key
        End Function

        Public Function GetKeyFromPK(ByVal pk() As PKDesc, ByVal type As Type) As Integer
            Dim o As IKeyProvider = CType(CachedEntity.CreateObject(pk, type, Me, Nothing), IKeyProvider)
            Return o.Key
        End Function

        Public Function IsInCachePrecise(ByVal obj As ICachedEntity, ByVal mpe As ObjectMappingEngine) As Boolean
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim t As Type = obj.GetType

            Dim dic As IDictionary = GetOrmDictionary(t, TryCast(obj.GetEntitySchema(mpe), ICacheBehavior))

            If dic Is Nothing Then
                ''todo: throw an exception when all collections will be implemented
                'Return
                Throw New OrmManagerException("Collection for " & t.Name & " not exists")
            End If

            Return ReferenceEquals(dic(New CacheKey(obj)), obj)
        End Function

        Public Function IsInCache(ByVal id As Object, ByVal t As Type, ByVal mpe As ObjectMappingEngine) As Boolean
            Dim dic As IDictionary = GetOrmDictionary(t, mpe)

            If dic Is Nothing Then
                ''todo: throw an exception when all collections will be implemented
                'Return
                Throw New OrmManagerException("Collection for " & t.Name & " not exists")
            End If

            Return dic.Contains(New CacheKey(SinglePKEntity.CreateKeyEntity(id, t, Me, mpe)))
        End Function

        Public Property NewObjectManager() As INewObjectsStore
            Get
                Return _newMgr
            End Get
            Set(ByVal value As INewObjectsStore)
                _newMgr = value
            End Set
        End Property

        Public Function IsNewObject(ByVal t As Type, ByVal id() As PKDesc) As Boolean
            Return NewObjectManager IsNot Nothing AndAlso NewObjectManager.GetNew(t, id) IsNot Nothing
        End Function

        'Public Function FindObjectInCache(ByVal obj As _ICachedEntity, _
        '    ByVal load As Boolean, ByVal checkOnCreate As Boolean, ByVal dic As IDictionary, _
        '    ByVal addOnCreate As Boolean, ByVal mgr As OrmManager) As _ICachedEntity

        '    Return FindObjectInCache(Nothing, obj, load, checkOnCreate, dic, addOnCreate, mgr, False, mgr.MappingEngine.GetEntitySchema(obj.GetType))
        'End Function

        Public Function FindObjectInCache(ByVal type As Type, ByVal obj As Object, _
            ByVal id As PKWrapper, ByVal cb As ICacheBehavior, _
            ByVal entityDictionary As IDictionary, ByVal addIfNotFound As Boolean, _
            ByVal fromDb As Boolean) As Object

            Dim a As Object = entityDictionary(id)
            Dim oc As ObjectModification = Nothing
            If a Is Nothing AndAlso Not fromDb AndAlso NewObjectManager IsNot Nothing Then
                a = NewObjectManager.GetNew(type, id.GetPKs)
                If a IsNot Nothing Then Return a
                oc = ShadowCopy(type, id, cb)
                If oc IsNot Nothing Then
                    Dim oldpk() As PKDesc = oc.OlPK
                    If oldpk IsNot Nothing Then
                        a = NewObjectManager.GetNew(type, oldpk)
                        If a IsNot Nothing Then Return a
                    End If
                End If
            End If

            If a Is Nothing Then
                If oc Is Nothing Then oc = ShadowCopy(type, id, cb)
                If oc IsNot Nothing Then
                    Return AddObjectInternal(oc.Obj, id, entityDictionary)
                End If
            End If

            If a Is Nothing AndAlso addIfNotFound Then
                a = AddObjectInternal(obj, id, entityDictionary)
            End If

            Return a
        End Function

        'Public Function GetKeyEntityFromCacheOrCreate(ByVal id As Object, ByVal type As Type, _
        '    ByVal add2CacheOnCreate As Boolean) As IKeyEntity

        '    Return GetKeyEntityFromCacheOrCreate(id, type, add2CacheOnCreate, Nothing, Nothing)
        'End Function

        Public Function GetKeyEntityFromCacheOrCreate(ByVal id As Object, ByVal type As Type, _
            ByVal add2CacheOnCreate As Boolean, ByVal mpe As ObjectMappingEngine) As ISinglePKEntity

            Dim o As ISinglePKEntity = SinglePKEntity.CreateKeyEntity(id, type, Me, mpe)
            o.SetObjectState(ObjectState.NotLoaded)

            Dim cb As ICacheBehavior = TryCast(mpe.GetEntitySchema(type), ICacheBehavior)
            Dim obj As _ICachedEntity = CType(FindObjectInCache(type, o, New CacheKey(o), cb, GetOrmDictionary(type, cb), add2CacheOnCreate, False), _ICachedEntity)

            If ReferenceEquals(o, obj) AndAlso Not add2CacheOnCreate Then
                o.SetObjectState(ObjectState.Created)
            End If

            Return CType(obj, ISinglePKEntity)
        End Function

        Public Function GetEntityOrOrmFromCacheOrCreate(Of T As {New, _ICachedEntity})( _
            ByVal pk() As PKDesc, ByVal addOnCreate As Boolean, ByVal mpe As ObjectMappingEngine) As T

            Dim o As T = CachedEntity.CreateObject(Of T)(pk, Me, mpe)

            o.SetObjectState(ObjectState.NotLoaded)

            Dim cb As ICacheBehavior = TryCast(mpe.GetEntitySchema(GetType(T)), ICacheBehavior)
            Return CType(FindObjectInCache(GetType(T), o, New CacheKey(o), cb, _
                CType(GetOrmDictionary(Of T)(cb), System.Collections.IDictionary), addOnCreate, False), T)
        End Function

        Public Function GetEntityFromCacheOrCreate(Of T As {New, _ICachedEntity})( _
            ByVal pk() As PKDesc, ByVal addOnCreate As Boolean, ByVal mpe As ObjectMappingEngine) As T

            Dim o As T = CachedEntity.CreateEntity(Of T)(pk, Me, mpe)

            o.SetObjectState(ObjectState.NotLoaded)

            Dim cb As ICacheBehavior = TryCast(mpe.GetEntitySchema(GetType(T)), ICacheBehavior)
            Return CType(FindObjectInCache(GetType(T), o, New CacheKey(o), cb, _
                CType(GetOrmDictionary(Of T)(cb), System.Collections.IDictionary), addOnCreate, False), T)
        End Function

        Public Function GetEntityFromCacheOrCreate(ByVal pk() As PKDesc, ByVal type As Type, _
            ByVal addOnCreate As Boolean, ByVal mpe As ObjectMappingEngine) As Object
            Dim o As Object = CachedEntity.CreateObject(pk, type, Me, mpe)
            Dim pkw As PKWrapper = Nothing
            Dim ce As _ICachedEntity = TryCast(o, _ICachedEntity)
            Dim e As _IEntity = TryCast(o, _ICachedEntity)
            If e IsNot Nothing Then
                e.SetObjectState(ObjectState.NotLoaded)
            End If
            If ce IsNot Nothing Then
                pkw = New CacheKey(ce)
            Else
                pkw = New PKWrapper(pk)
            End If

            Dim cb As ICacheBehavior = TryCast(mpe.GetEntitySchema(type), ICacheBehavior)
            Return CType(FindObjectInCache(type, o, pkw, cb, GetOrmDictionary(type, cb), addOnCreate, False), ICachedEntity)
        End Function

        Public Function GetEntityFromCacheOrCreate(ByVal pk() As PKDesc, ByVal type As Type, _
            ByVal addOnCreate As Boolean, ByVal dic As IDictionary, ByVal mpe As ObjectMappingEngine) As Object
            Dim o As Object = CachedEntity.CreateObject(pk, type, Me, mpe)
            Dim pkw As PKWrapper = Nothing
            Dim ce As _ICachedEntity = TryCast(o, _ICachedEntity)
            Dim e As _IEntity = TryCast(o, _ICachedEntity)
            If e IsNot Nothing Then
                e.SetObjectState(ObjectState.NotLoaded)
            End If
            If ce IsNot Nothing Then
                pkw = New CacheKey(ce)
            Else
                pkw = New PKWrapper(pk)
            End If

            Dim cb As ICacheBehavior = TryCast(mpe.GetEntitySchema(type), ICacheBehavior)
            Return CType(FindObjectInCache(type, o, pkw, cb, dic, addOnCreate, False), ICachedEntity)
        End Function

        Protected Friend Shared Function AddObjectInternal(ByVal obj As Object, ByVal id As PKWrapper, ByVal dic As IDictionary) As Object
#If DEBUG Then
            Dim e As IEntity = TryCast(obj, IEntity)
            If e IsNot Nothing Then Debug.Assert(e.ObjectState <> ObjectState.Deleted)
#End If
            'Dim trace As Boolean = False
            SyncLock dic.SyncRoot
                If Not dic.Contains(id) Then
                    dic.Add(id, obj)
                    Return obj
#If TraceCreation Then
                Diagnostics.Debug.WriteLine(String.Format("{2} - dt: {0}, {1}", Now, Environment.StackTrace, obj.GetName))
#End If
                Else
                    'Trace = True
                    Return dic(id)
                End If
            End SyncLock

            'If trace AndAlso OrmManager._mcSwitch.TraceVerbose Then
            '    OrmManager.WriteLine(String.Format("Attempt to add existing object of type {0} ({1}) to cashe", obj.GetType, id.ToString))
            'End If
        End Function

        Public Function GetAllKeys() As List(Of String) Implements IExploreQueryCache.GetAllKeys
            Dim l As New List(Of String)
            For Each s As String In _filters.Keys
                l.Add(s)
            Next
            Return l
        End Function

        Public Function GetQueryDictionary(ByVal key As String) As System.Collections.IDictionary Implements IExploreQueryCache.GetDictionary
            Return CType(_filters(key), System.Collections.IDictionary)
        End Function

#Region " POCO "
        Public Function GetPOCO(ByVal mpe As ObjectMappingEngine, ByVal o As Object) As _ICachedEntity
            Return GetPOCO(mpe, mpe.GetEntitySchema(o.GetType, False), o, Nothing)
        End Function

        Public Function GetPOCO(ByVal mpe As ObjectMappingEngine, ByVal oschema As IEntitySchema, ByVal o As Object) As _ICachedEntity
            Return GetPOCO(mpe, oschema, o, Nothing)
        End Function

        Public Function GetPOCO(ByVal mpe As ObjectMappingEngine, ByVal oschema As IEntitySchema, _
            ByVal o As Object, ByVal mgr As OrmManager) As _ICachedEntity
            Dim type As Type = o.GetType
            'Dim pk As New List(Of PKDesc)
            'For Each e As EntityPropertyAttribute In mpe.GetPrimaryKeys(type, oschema)
            '    Dim pkd As New PKDesc(e.PropertyAlias, ObjectMappingEngine.GetPropertyValue(o, e.PropertyAlias, oschema))
            '    pk.Add(pkd)
            'Next
            Dim pks() As PKDesc = mpe.GetPKs(o, oschema)
            Dim c As _ICachedEntity = CachedEntity.CreateEntity(pks, GetType(AnonymousCachedEntity), Me, mpe)
            Dim cc As IKeyProvider = TryCast(o, IKeyProvider)
            If cc IsNot Nothing Then
                CType(c, AnonymousCachedEntity).SetKey(cc.Key)
            End If
            c.SetObjectState(ObjectState.NotLoaded)
            Dim cb As ICacheBehavior = TryCast(oschema, ICacheBehavior)

            Dim ro As Object = FindObjectInCache(type, c, New CacheKey(c), cb, GetOrmDictionary(GetType(AnonymousCachedEntity), cb), True, False)

            If ReferenceEquals(ro, c) Then
                CType(c, AnonymousCachedEntity)._myschema = oschema
                If mgr IsNot Nothing Then
                    c.Load(mgr)
                    If c.ObjectState = ObjectState.NotFoundInSource Then
                        c.SetObjectState(ObjectState.Created)
                    End If
                End If
            End If
            Return CType(ro, _ICachedEntity)
        End Function

        Public Function SyncPOCO(ByVal mpe As ObjectMappingEngine, ByVal oschema As IEntitySchema, _
            ByVal o As Object, ByVal mgr As OrmManager) As ICachedEntity
            Dim c As _ICachedEntity = GetPOCO(mpe, oschema, o, mgr)
            SyncPOCO(c, mpe, oschema, o)
            Return c
        End Function

        Public Sub SyncPOCO(ByVal c As _ICachedEntity, ByVal mpe As ObjectMappingEngine, ByVal oschema As IEntitySchema, ByVal o As Object)
            If c IsNot Nothing Then
                If c.ObjectState = ObjectState.Created Then c.BeginLoading()
                For Each m As MapField2Column In oschema.FieldColumnMap
                    ObjectMappingEngine.SetPropertyValue(c, m.PropertyAlias, _
                        ObjectMappingEngine.GetPropertyValue(o, m.PropertyAlias, oschema), oschema)
                Next
                If c.ObjectState = ObjectState.Created Then c.EndLoading()
            End If
        End Sub

#End Region

    End Class

    Public Class ReadonlyCache
        Inherits CacheBase
        Implements IExploreEntityCache

        Private _rootObjectsDictionary As IDictionary = Hashtable.Synchronized(New Hashtable)

        Public Overloads Overrides Function CreateResultsetsDictionary() As System.Collections.IDictionary
            Return Hashtable.Synchronized(New Hashtable)
        End Function

        Public Overrides Function GetOrmDictionary(ByVal t As System.Type, ByVal mpe As ObjectMappingEngine) As System.Collections.IDictionary
            If Not GetType(_ICachedEntity).IsAssignableFrom(t) Then
                Return Nothing
            End If

            Dim k As Object = t
            If mpe IsNot Nothing Then
                k = mpe.GetEntityTypeKey(t)
            End If

            Dim dic As IDictionary = CType(_rootObjectsDictionary(k), IDictionary)
            If dic Is Nothing Then
                Using SyncRoot
                    dic = CType(_rootObjectsDictionary(k), IDictionary)
                    If dic Is Nothing Then
                        dic = CreateDictionary4ObjectInstances(t)
                        _rootObjectsDictionary(k) = dic
                    End If
                End Using
            End If
            Return dic
        End Function

        Public Overrides Function GetOrmDictionary(ByVal t As System.Type, _
            ByVal cb As ICacheBehavior) As System.Collections.IDictionary

            If Not GetType(_ICachedEntity).IsAssignableFrom(t) Then
                Return Nothing
            End If

            Dim k As Object = ObjectMappingEngine.GetEntityTypeKey(t, cb)

            Dim dic As IDictionary = CType(_rootObjectsDictionary(k), IDictionary)
            If dic Is Nothing Then
                Using SyncRoot
                    dic = CType(_rootObjectsDictionary(k), IDictionary)
                    If dic Is Nothing Then
                        dic = CreateDictionary4ObjectInstances(t)
                        _rootObjectsDictionary(k) = dic
                    End If
                End Using
            End If
            Return dic
        End Function

        Public Overrides Function GetOrmDictionary(Of T As _ICachedEntity)(ByVal mpe As ObjectMappingEngine) As System.Collections.Generic.IDictionary(Of Object, T)
            Return CType(GetOrmDictionary(GetType(T), mpe), IDictionary(Of Object, T))
        End Function

        Public Overrides Function GetOrmDictionary(Of T As _ICachedEntity)(ByVal cb As ICacheBehavior) As System.Collections.Generic.IDictionary(Of Object, T)
            Return CType(GetOrmDictionary(GetType(T), cb), IDictionary(Of Object, T))
        End Function

        Protected Overridable Function CreateDictionary4ObjectInstances(ByVal t As Type) As IDictionary
            Dim gt As Type = GetType(Collections.SynchronizedDictionary(Of ))
            gt = gt.MakeGenericType(New Type() {t})
            Return CType(gt.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IDictionary)
        End Function

        Public Function GetAllEntitiesKeys() As System.Collections.ArrayList Implements IExploreEntityCache.GetAllKeys
            Return New ArrayList(_rootObjectsDictionary.Keys)
        End Function

        Public Function GetEntitiesDictionary_(ByVal key As Object) As System.Collections.IDictionary Implements IExploreEntityCache.GetDictionary
            Return CType(_rootObjectsDictionary(key), System.Collections.IDictionary)
        End Function

        Public Overrides Sub Reset()
            _rootObjectsDictionary = Hashtable.Synchronized(New Hashtable)
            MyBase.Reset()
        End Sub
    End Class

    Public MustInherit Class ReadonlyWebCache
        Inherits ReadonlyCache

        Protected Overrides Function CreateDictionary4ObjectInstances(ByVal t As System.Type) As System.Collections.IDictionary
            Dim pol As WebCacheDictionaryPolicy = GetPolicy(t)
            Dim args() As Object = Nothing
            Dim dt As Type = Nothing
            If pol Is Nothing Then
                dt = GetType(Collections.SynchronizedDictionary(Of ))
            Else
                dt = GetType(WebCacheEntityDictionary(Of ))
                args = GetArgs(t, pol)
            End If
            dt = dt.MakeGenericType(New Type() {t})
            Return CType(dt.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, _
                args), System.Collections.IDictionary)
        End Function

        Protected Function GetArgs(ByVal t As Type, ByVal pol As WebCacheDictionaryPolicy) As Object()
            Return New Object() { _
                Me, pol.AbsoluteExpiration, pol.SlidingExpiration, _
                pol.Priority, pol.Dependency _
            }
        End Function

        Protected MustOverride Function GetPolicy(ByVal t As Type) As WebCacheDictionaryPolicy

        Protected Overrides Function CreateListConverter() As IListObjectConverter
            Return New ListConverter
        End Function

        'Public Overrides Function CreateRootDictionary4Queries() As System.Collections.IDictionary
        '    Return New WebCacheDictionary(Of IDictionary)
        'End Function

        Public Overrides Function CreateResultsets4SimpleValuesDictionary() As System.Collections.IDictionary
            Return New WebCacheDictionary(Of CachedItemBase)
        End Function

        Public Overrides Function CreateResultsets4AnonymValuesDictionary() As System.Collections.IDictionary
            Return New WebCacheDictionary(Of CachedItemBase)
        End Function
    End Class
End Namespace