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

        Public MustOverride Function GetOrmDictionary(ByVal filterInfo As Object, ByVal t As Type, ByVal schema As ObjectMappingEngine) As System.Collections.IDictionary

        Public MustOverride Function GetOrmDictionary(Of T As _ICachedEntity)(ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine) As System.Collections.Generic.IDictionary(Of Object, T)

        Public MustOverride Function GetOrmDictionary(ByVal filterInfo As Object, ByVal t As Type, ByVal schema As ObjectMappingEngine, ByVal oschema As IEntitySchema) As System.Collections.IDictionary

        Public MustOverride Function GetOrmDictionary(Of T As _ICachedEntity)(ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine, ByVal oschema As IEntitySchema) As System.Collections.Generic.IDictionary(Of Object, T)

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

        Public Overridable Sub RegisterRemoval(ByVal obj As _ICachedEntity, ByVal mgr As OrmManager)
            If mgr IsNot Nothing Then
                Dim sc As ObjectModification = ShadowCopy(obj, mgr, mgr.MappingEngine.GetEntitySchema(obj.GetType))
                If sc IsNot Nothing Then
                    Dim st As String = String.Empty
#If DEBUG Then
                    st = sc.StackTrace
#End If
                    Throw New OrmManagerException(String.Format("Object is going to die {0}. Stack {1}", sc.Obj.ObjName, st))
                End If
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

        Public Function ShadowCopy(ByVal obj As _ICachedEntity, ByVal s As ObjectMappingEngine, ByVal oschema As IEntitySchema) As ObjectModification
            Using SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                If obj.IsPKLoaded Then
                    Dim name As String = GetModificationKey(obj, s, Nothing, oschema)
                    Return CType(_modifiedobjects(name), ObjectModification)
                Else
                    Return Nothing
                End If
            End Using
        End Function

        Public Function ShadowCopy(ByVal obj As _ICachedEntity, ByVal mgr As OrmManager) As ObjectModification
            Return ShadowCopy(obj, mgr, mgr.MappingEngine.GetEntitySchema(obj.GetType))
        End Function

        Public Function ShadowCopy(ByVal obj As _ICachedEntity, ByVal mgr As OrmManager, ByVal oschema As IEntitySchema) As ObjectModification
            Using SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                If obj.IsPKLoaded Then
                    Dim name As String = GetModificationKey(obj, mgr.MappingEngine, mgr.GetContextInfo, oschema)
                    Return CType(_modifiedobjects(name), ObjectModification)
                Else
                    Return Nothing
                End If
            End Using
        End Function

        Protected Shared Sub Assert(ByVal condition As Boolean, ByVal message As String)
            Debug.Assert(condition, message)
            Trace.Assert(condition, message)
            If Not condition Then Throw New OrmCacheException(message)
        End Sub

        Protected Function GetModificationKey(ByVal obj As _ICachedEntity, ByVal mpe As ObjectMappingEngine, _
            ByVal context As Object, ByVal oschema As IEntitySchema) As String
            Return GetModificationKey(obj, obj.Key, mpe, context, oschema)
        End Function

        Protected Function GetModificationKey(ByVal obj As _ICachedEntity, ByVal key As Integer, _
            ByVal mpe As ObjectMappingEngine, ByVal context As Object, ByVal oschema As IEntitySchema) As String
            If mpe IsNot Nothing Then
                Dim t As Type = obj.GetType
                Return mpe.GetEntityTypeKey(context, t, oschema).ToString & ":" & key
            Else
                Return obj.GetType().FullName & ":" & key
            End If
        End Function

        Protected Friend Function RegisterModification(ByVal mgr As OrmManager, ByVal obj As _ICachedEntity, _
            ByVal reason As ObjectModification.ReasonEnum, ByVal oschema As IEntitySchema) As ObjectModification
            Return RegisterModification(mgr, obj, Nothing, reason, oschema)
        End Function

        Protected Friend Function RegisterModification(ByVal mgr As OrmManager, ByVal obj As _ICachedEntity, _
            ByVal pk() As PKDesc, ByVal reason As ObjectModification.ReasonEnum, ByVal oschema As IEntitySchema) As ObjectModification
            Using SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                Dim key As String = GetModificationKey(obj, mgr.MappingEngine, mgr.GetContextInfo, oschema)
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
                If tt Is mo.Obj Then
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
            ByVal mpe As ObjectMappingEngine, ByVal context As Object, ByVal oschema As IEntitySchema)
            Using SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                If _modifiedobjects.Count > 0 Then
                    Dim key As String = GetModificationKey(obj, mpe, context, oschema)
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

        Protected Friend Sub AddM2MObjDependent(ByVal obj As _IKeyEntity, ByVal key As String, ByVal id As String)
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

        Protected Friend Sub UpdateM2MEntries(ByVal obj As _IKeyEntity, ByVal oldId As Object, ByVal name As String)
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

        Public Function GetKeyFromPK(Of T As {New, IKeyEntity})(ByVal id As Object) As Integer
            Dim o As T = KeyEntity.CreateKeyEntity(Of T)(id, Me, Nothing)
            Return o.Key
        End Function

        Public Function GetKeyFromPK(ByVal pk() As PKDesc, ByVal type As Type) As Integer
            Dim o As ICachedEntity = CachedEntity.CreateObject(pk, type, Me, Nothing)
            Return o.Key
        End Function

        Public Function IsInCachePrecise(ByVal obj As ICachedEntity, ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine) As Boolean
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim t As Type = obj.GetType

            Dim dic As IDictionary = GetOrmDictionary(filterInfo, t, schema)

            If dic Is Nothing Then
                ''todo: throw an exception when all collections will be implemented
                'Return
                Throw New OrmManagerException("Collection for " & t.Name & " not exists")
            End If

            Return ReferenceEquals(dic(New CacheKey(obj)), obj)
        End Function

        Public Function IsInCache(ByVal id As Object, ByVal t As Type, ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine) As Boolean
            Dim dic As IDictionary = GetOrmDictionary(filterInfo, t, schema)

            If dic Is Nothing Then
                ''todo: throw an exception when all collections will be implemented
                'Return
                Throw New OrmManagerException("Collection for " & t.Name & " not exists")
            End If

            Return dic.Contains(New CacheKey(KeyEntity.CreateKeyEntity(id, t, Me, schema)))
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

        Public Function NormalizeObject(ByVal obj As _ICachedEntity, _
            ByVal load As Boolean, ByVal checkOnCreate As Boolean, ByVal dic As IDictionary, _
            ByVal addOnCreate As Boolean, ByVal mgr As OrmManager) As _ICachedEntity

            Return NormalizeObject(obj, load, checkOnCreate, dic, addOnCreate, mgr, False, _
                                   mgr.MappingEngine.GetEntitySchema(obj.GetType))
        End Function

        Public Function NormalizeObject(ByVal obj As _ICachedEntity, _
            ByVal load As Boolean, ByVal checkOnCreate As Boolean, ByVal dic As IDictionary, _
            ByVal addOnCreate As Boolean, ByVal mgr As OrmManager, ByVal fromDb As Boolean, _
            ByVal oschema As IEntitySchema) As _ICachedEntity

            Assert(obj.IsPKLoaded, "Primary key is not loaded")

            Dim type As Type = obj.GetType

#If DEBUG Then
            If dic Is Nothing Then
                Dim name As String = type.Name
                Throw New OrmManagerException("Collection for " & name & " not exists")
            End If
#End If
            Dim id As CacheKey = New CacheKey(obj)
            Dim created As Boolean = False ', checked As Boolean = False
            Dim a As _ICachedEntity = CType(dic(id), _ICachedEntity)
            Dim oc As ObjectModification = Nothing
            If a Is Nothing AndAlso Not fromDb AndAlso NewObjectManager IsNot Nothing Then
                a = NewObjectManager.GetNew(type, obj.GetPKValues)
                If a IsNot Nothing Then Return a
                If mgr IsNot Nothing Then
                    oc = ShadowCopy(obj, mgr, oschema)
                    If oc IsNot Nothing Then
                        Dim oldpk() As PKDesc = oc.OlPK
                        If oldpk IsNot Nothing Then
                            a = NewObjectManager.GetNew(type, oldpk)
                            If a IsNot Nothing Then Return a
                        End If
                    End If
                End If
            End If
            If a Is Nothing AndAlso mgr IsNot Nothing Then
                If oc Is Nothing Then oc = ShadowCopy(obj, mgr, oschema)
                If oc IsNot Nothing Then
                    a = CType(oc.Obj, _ICachedEntity)
                    AddObjectInternal(a, New CacheKey(a), dic)
                    Return a
                End If
            End If

            If a Is Nothing Then
                Dim sync_key As String = "LoadType" & id.ToString & type.ToString
                Using SyncHelper.AcquireDynamicLock(sync_key)
                    a = CType(dic(id), _ICachedEntity)
                    If a Is Nothing Then
                        'If ObjectMappingEngine.GetUnions(type) IsNot Nothing Then
                        '    Throw New NotSupportedException
                        'Else
                        a = obj
                        If a.ObjectState = ObjectState.Created AndAlso Not a.IsLoaded Then
                            If GetType(IKeyEntity).IsAssignableFrom(type) Then
                                Dim orm As IKeyEntity = CType(a, IKeyEntity)
                                orm.Init(orm.Identifier, Me, mgr.MappingEngine)
                            Else
                                a.Init(a.GetPKValues, Me, mgr.MappingEngine)
                            End If
                        End If
                        'End If

                        If load Then
                            a.Load(mgr)
                            If Not a.IsLoaded Then
                                a = Nothing
                            End If
                        End If
                        If a IsNot Nothing AndAlso checkOnCreate Then
                            'checked = True
                            If Not a.IsLoaded Then
                                a.Load(mgr)
                                If a.ObjectState = ObjectState.NotFoundInSource Then
                                    a = Nothing
                                End If
                            End If
                        End If
                        created = True
                        If a IsNot Nothing AndAlso addOnCreate Then
                            AddObjectInternal(a, id, dic)
                        End If
                    End If
                End Using
            End If

            If a IsNot Nothing Then
                If Not created AndAlso load AndAlso Not a.IsLoaded Then
                    a.Load(mgr)
                End If
            End If

            Return a
        End Function

        Public Function GetKeyEntityFromCacheOrCreate(ByVal id As Object, ByVal type As Type, _
            ByVal add2CacheOnCreate As Boolean) As IKeyEntity

            Return GetKeyEntityFromCacheOrCreate(id, type, add2CacheOnCreate, Nothing, Nothing)
        End Function

        Public Function GetKeyEntityFromCacheOrCreate(ByVal id As Object, ByVal type As Type, _
            ByVal add2CacheOnCreate As Boolean, ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine) As IKeyEntity

            Dim o As IKeyEntity = KeyEntity.CreateKeyEntity(id, type, Me, schema)
            o.SetObjectState(ObjectState.NotLoaded)

            Dim obj As _ICachedEntity = NormalizeObject(o, False, False, _
                GetOrmDictionary(filterInfo, type, schema), add2CacheOnCreate, Nothing, False, schema.GetEntitySchema(type))

            If ReferenceEquals(o, obj) AndAlso Not add2CacheOnCreate Then
                o.SetObjectState(ObjectState.Created)
            End If

            Return CType(obj, IKeyEntity)
        End Function

        Public Function GetEntityOrOrmFromCacheOrCreate(Of T As {New, _ICachedEntity})( _
            ByVal pk() As PKDesc, ByVal addOnCreate As Boolean, ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine) As T

            Dim o As T = CachedEntity.CreateObject(Of T)(pk, Me, schema)

            o.SetObjectState(ObjectState.NotLoaded)

            Return CType(NormalizeObject(o, False, False, _
                CType(GetOrmDictionary(Of T)(filterInfo, schema), System.Collections.IDictionary), addOnCreate, Nothing), T)
        End Function

        Public Function GetEntityFromCacheOrCreate(Of T As {New, _ICachedEntity})( _
            ByVal pk() As PKDesc, ByVal addOnCreate As Boolean, ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine) As T

            Dim o As T = CachedEntity.CreateEntity(Of T)(pk, Me, schema)

            o.SetObjectState(ObjectState.NotLoaded)

            Return CType(NormalizeObject(o, False, False, _
                CType(GetOrmDictionary(Of T)(filterInfo, schema), System.Collections.IDictionary), addOnCreate, Nothing), T)
        End Function

        Public Function GetEntityFromCacheOrCreate(ByVal pk() As PKDesc, ByVal type As Type, _
            ByVal addOnCreate As Boolean, ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine) As ICachedEntity
            Dim o As _ICachedEntity = CachedEntity.CreateObject(pk, type, Me, schema)

            o.SetObjectState(ObjectState.NotLoaded)

            Return NormalizeObject(o, False, False, GetOrmDictionary(filterInfo, type, schema), addOnCreate, Nothing)
        End Function

        Protected Friend Shared Sub AddObjectInternal(ByVal obj As ICachedEntity, ByVal id As CacheKey, ByVal dic As IDictionary)
            Debug.Assert(obj.ObjectState <> ObjectState.Deleted)
            Dim trace As Boolean = False
            SyncLock dic.SyncRoot
                If Not dic.Contains(id) Then
                    dic.Add(id, obj)
#If TraceCreation Then
                Diagnostics.Debug.WriteLine(String.Format("{2} - dt: {0}, {1}", Now, Environment.StackTrace, obj.GetName))
#End If
                Else
                    trace = True
                End If
            End SyncLock

            If trace AndAlso OrmManager._mcSwitch.TraceVerbose Then
                Dim name As String = obj.GetType.Name
                OrmManager.WriteLine("Attempt to add existing object " & name & " (" & obj.Key & ") to cashe")
            End If
        End Sub

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

        Public Function GetPOCO(ByVal mpe As ObjectMappingEngine, ByVal o As Object) As ICachedEntity
            Return GetPOCO(mpe, mpe.GetEntitySchema(o.GetType, False), o)
        End Function

        Public Function GetPOCO(ByVal mpe As ObjectMappingEngine, ByVal oschema As IEntitySchema, ByVal o As Object) As _ICachedEntity
            Dim pk As New List(Of PKDesc)
            For Each e As EntityPropertyAttribute In mpe.GetPrimaryKeys(o.GetType, oschema)
                Dim pkd As New PKDesc(e.PropertyAlias, mpe.GetPropertyValue(o, e.PropertyAlias, oschema))
                pk.Add(pkd)
            Next
            Dim c As _ICachedEntity = CachedEntity.CreateEntity(pk.ToArray, GetType(AnonymousCachedEntity), Me, mpe)

            c.SetObjectState(ObjectState.NotLoaded)

            Dim ro As _ICachedEntity = NormalizeObject(c, False, False, _
                GetOrmDictionary(Nothing, GetType(AnonymousCachedEntity), mpe, oschema), True, Nothing, _
                False, oschema)

            If ReferenceEquals(ro, c) Then
                CType(c, AnonymousCachedEntity)._myschema = oschema
            End If
            Return ro
        End Function

        Public Function SyncPOCO(ByVal mpe As ObjectMappingEngine, ByVal oschema As IEntitySchema, ByVal o As Object) As ICachedEntity
            Dim c As _ICachedEntity = GetPOCO(mpe, oschema, o)
            SyncPOCO(c, mpe, oschema, o)
            Return c
        End Function

        Public Sub SyncPOCO(ByVal c As _ICachedEntity, ByVal mpe As ObjectMappingEngine, ByVal oschema As IEntitySchema, ByVal o As Object)
            If c IsNot Nothing Then
                For Each de As DictionaryEntry In mpe.GetProperties(o.GetType, oschema)
                    Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                    Dim ea As EntityPropertyAttribute = CType(de.Key, EntityPropertyAttribute)
                    mpe.SetPropertyValue(c, ea.PropertyAlias, mpe.GetPropertyValue(o, ea.PropertyAlias, oschema), oschema)
                Next
            End If
        End Sub
    End Class

    Public Class ReadonlyCache
        Inherits CacheBase
        Implements IExploreEntityCache

        Private _rootObjectsDictionary As IDictionary = Hashtable.Synchronized(New Hashtable)

        Public Overloads Overrides Function CreateResultsetsDictionary() As System.Collections.IDictionary
            Return Hashtable.Synchronized(New Hashtable)
        End Function

        Public Overrides Function GetOrmDictionary(ByVal filterInfo As Object, ByVal t As System.Type, ByVal schema As ObjectMappingEngine) As System.Collections.IDictionary
            If Not GetType(_ICachedEntity).IsAssignableFrom(t) Then
                Return Nothing
            End If

            Dim k As Object = t
            If schema IsNot Nothing Then
                k = schema.GetEntityTypeKey(filterInfo, t)
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

        Public Overrides Function GetOrmDictionary(ByVal filterInfo As Object, ByVal t As System.Type, _
            ByVal schema As ObjectMappingEngine, ByVal oschema As IEntitySchema) As System.Collections.IDictionary

            If Not GetType(_ICachedEntity).IsAssignableFrom(t) Then
                Return Nothing
            End If

            Dim k As Object = t
            If schema IsNot Nothing Then
                k = schema.GetEntityTypeKey(filterInfo, t, oschema)
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

        Public Overrides Function GetOrmDictionary(Of T As _ICachedEntity)(ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine) As System.Collections.Generic.IDictionary(Of Object, T)
            Return CType(GetOrmDictionary(filterInfo, GetType(T), schema), IDictionary(Of Object, T))
        End Function

        Public Overrides Function GetOrmDictionary(Of T As _ICachedEntity)(ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine, ByVal oschema As IEntitySchema) As System.Collections.Generic.IDictionary(Of Object, T)
            Return CType(GetOrmDictionary(filterInfo, GetType(T), schema, oschema), IDictionary(Of Object, T))
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