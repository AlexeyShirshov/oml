Imports System.Collections.Generic
Imports Worm.Orm
Imports Worm.Orm.Meta

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

        Public ReadOnly DateTimeCreated As Date

        Private _filters As IDictionary
        Private _loadTimes As New Dictionary(Of Type, Pair(Of Integer, TimeSpan))
        Private _lock As New Object
        Private _list_converter As IListObjectConverter
        Private _modifiedobjects As IDictionary
        Private _externalObjects As IDictionary

        Private _m2m_dep As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Object)))

        Public Event RegisterEntityCreation(ByVal e As IEntity)
        Public Event RegisterObjectCreation(ByVal t As Type, ByVal id As Integer)
        Public Event RegisterObjectRemoval(ByVal obj As ICachedEntity)

        Public Event RegisterCollectionCreation(ByVal t As Type)
        Public Event RegisterCollectionRemoval(ByVal ce As OrmManager.CachedItem)
        Public Event CacheHasModification As EventHandler
        Public Event CacheHasnotModification As EventHandler

        Sub New()
            _filters = CreateRootDictionary4Queries()
            DateTimeCreated = Now
            _list_converter = CreateListConverter()
            _modifiedobjects = Hashtable.Synchronized(New Hashtable)
            _externalObjects = CreateRootDictionary4ExternalObjects()
        End Sub

        Public MustOverride Function CreateResultsetsDictionary() As IDictionary

        Public MustOverride Function GetOrmDictionary(ByVal filterInfo As Object, ByVal t As Type, ByVal schema As ObjectMappingEngine) As System.Collections.IDictionary

        Public MustOverride Function GetOrmDictionary(Of T)(ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine) As System.Collections.Generic.IDictionary(Of Object, T)

        Public MustOverride Function GetOrmDictionary(ByVal filterInfo As Object, ByVal t As Type, ByVal schema As ObjectMappingEngine, ByVal oschema As IObjectSchemaBase) As System.Collections.IDictionary

        Public MustOverride Function GetOrmDictionary(Of T)(ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine, ByVal oschema As IObjectSchemaBase) As System.Collections.Generic.IDictionary(Of Object, T)

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

        Public Overridable Function CreateResultsetsDictionary(ByVal mark As String) As IDictionary
            If String.IsNullOrEmpty(mark) Then
                Return CreateResultsetsDictionary()
            End If
            Throw New NotImplementedException(String.Format("Mark {0} is not supported", mark))
        End Function

        Public Overridable Sub RegisterCreation(ByVal obj As IEntity)
            RaiseEvent RegisterEntityCreation(obj)
        End Sub

        Public Overridable Sub RegisterCreation(ByVal t As Type, ByVal id As Integer)
            RaiseEvent RegisterObjectCreation(t, id)
#If TraceCreation Then
            _added.add(new Pair(Of date,Pair(Of type,Integer))(Now,New Pair(Of type,Integer)(t,id)))
#End If
        End Sub

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

        Public Overridable Sub RegisterRemoval(ByVal obj As _ICachedEntity)
            Debug.Assert(ShadowCopy(obj) Is Nothing)
            RaiseEvent RegisterObjectRemoval(obj)
            obj.RemoveFromCache(Me)
#If TraceCreation Then
            _removed.add(new Pair(Of date,ormbase)(Now,obj))
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
            RaiseEvent RegisterCollectionCreation(t)
        End Sub

        Public Overridable Sub RegisterRemovalCacheItem(ByVal ce As OrmManager.CachedItem)
            RaiseEvent RegisterCollectionRemoval(ce)
        End Sub

        Public Overridable Sub RemoveEntry(ByVal key As String, ByVal id As String)
            Dim dic As IDictionary = CType(_filters(key), System.Collections.IDictionary)
            If dic IsNot Nothing Then
                Dim ce As OrmManager.CachedItem = TryCast(dic(id), OrmManager.CachedItem)
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

        Public Function ShadowCopy(ByVal t As Type, ByVal id As CacheKey) As ObjectModification
            Using SyncRoot
                Dim name As String = t.Name & ":" & id.ToString
                Return CType(_modifiedobjects(name), ObjectModification)
            End Using
        End Function

        Public Function ShadowCopy(ByVal obj As _ICachedEntity) As ObjectModification
            Using SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                If obj.IsPKLoaded Then
                    Dim name As String = obj.GetType().Name & ":" & obj.Key
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

        Protected Friend Function RegisterModification(ByVal obj As _ICachedEntity, ByVal reason As ObjectModification.ReasonEnum) As ObjectModification
            Using SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                Dim mo As ObjectModification = Nothing
                Dim name As String = obj.GetType().Name & ":" & obj.Key
                'Using SyncHelper.AcquireDynamicLock(name)
                Assert(OrmManager.CurrentManager IsNot Nothing, "You have to create MediaContent object to perform this operation")
                Assert(Not _modifiedobjects.Contains(name), "Key " & name & " already in collection")
                mo = New ObjectModification(obj, OrmManager.CurrentManager.CurrentUser, reason)
                _modifiedobjects.Add(name, mo)
                'End Using
                If _modifiedobjects.Count = 1 Then
                    RaiseEvent CacheHasModification(Me, EventArgs.Empty)
                End If
                Return mo
            End Using
        End Function

        Public Function GetModifiedObjects(Of T As {ICachedEntity})() As ICollection(Of T)
            Dim al As New Generic.List(Of T)
            Dim tt As Type = GetType(T)
            For Each s As String In New ArrayList(_modifiedobjects.Keys)
                If s.IndexOf(tt.Name & ":") >= 0 Then
                    Dim mo As ObjectModification = CType(_modifiedobjects(s), ObjectModification)
                    If mo IsNot Nothing Then
                        al.Add(CType(mo.Obj, T))
                    End If
                End If
            Next
            Return al
        End Function

        Protected Friend Sub RegisterExistingModification(ByVal obj As ICachedEntity, ByVal key As Integer)
            Using SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                Dim name As String = obj.GetType().Name & ":" & key
                _modifiedobjects.Add(name, obj.OriginalCopy)
                If _modifiedobjects.Count = 1 Then
                    RaiseEvent CacheHasModification(Me, EventArgs.Empty)
                End If
            End Using
        End Sub

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
        Protected Friend Sub UnregisterModification(ByVal obj As _ICachedEntity)
            Using SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                If _modifiedobjects.Count > 0 Then
                    Dim name As String = obj.GetType().Name & ":" & obj.Key
                    _modifiedobjects.Remove(name)
                    obj.RaiseCopyRemoved()
#If TraceCreation Then
                    _s.Add(New Pair(Of String, _ICachedEntity)(Environment.StackTrace, obj))
#End If
                    If _modifiedobjects.Count = 0 Then 'AndAlso obj.old_state <> ObjectState.Created Then
                        RaiseEvent CacheHasnotModification(Me, EventArgs.Empty)
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
            Using SyncHelper.AcquireDynamicLock_Debug("q89rbvadfk" & t.ToString,"d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("q89rbvadfk" & t.ToString)
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

        Protected Friend Sub AddM2MObjDependent(ByVal obj As _IOrmBase, ByVal key As String, ByVal id As String)
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

        Protected Friend Function GetM2MEntries(ByVal obj As _IOrmBase, ByVal name As String) As ICollection(Of Pair(Of OrmManager.M2MCache, Pair(Of String, String)))
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
                Dim etrs As New List(Of Pair(Of OrmManager.M2MCache, Pair(Of String, String)))
                Dim l As Dictionary(Of String, Dictionary(Of String, Object)) = Nothing

                If _m2m_dep.TryGetValue(name, l) Then
                    For Each p As KeyValuePair(Of String, Dictionary(Of String, Object)) In l
                        Dim dic As IDictionary = _GetDictionary(p.Key)
                        If dic IsNot Nothing Then
                            For Each id As String In p.Value.Keys
                                Dim ce As OrmManager.M2MCache = TryCast(dic(id), OrmManager.M2MCache)
                                If ce Is Nothing Then
                                    'dic.Remove(id)
                                    RemoveEntry(p.Key, id)
                                Else
                                    etrs.Add(New Pair(Of OrmManager.M2MCache, Pair(Of String, String))(ce, New Pair(Of String, String)(p.Key, id)))
                                End If
                            Next
                        End If
                    Next
                End If
                Return etrs
            End Using
        End Function

        Protected Friend Sub UpdateM2MEntries(ByVal obj As _IOrmBase, ByVal oldId As Object, ByVal name As String)
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

    End Class

    Public Class ReadonlyCache
        Inherits CacheBase
        Implements IExploreCache

        Private _rootObjectsDictionary As IDictionary = Hashtable.Synchronized(New Hashtable)

        Public Overloads Overrides Function CreateResultsetsDictionary() As System.Collections.IDictionary
            Return Hashtable.Synchronized(New Hashtable)
        End Function

        Public Overrides Function GetOrmDictionary(ByVal filterInfo As Object, ByVal t As System.Type, ByVal schema As ObjectMappingEngine) As System.Collections.IDictionary
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
            ByVal schema As ObjectMappingEngine, ByVal oschema As IObjectSchemaBase) As System.Collections.IDictionary
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

        Public Overrides Function GetOrmDictionary(Of T)(ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine) As System.Collections.Generic.IDictionary(Of Object, T)
            Return CType(GetOrmDictionary(filterInfo, GetType(T), schema), IDictionary(Of Object, T))
        End Function

        Public Overrides Function GetOrmDictionary(Of T)(ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine, ByVal oschema As IObjectSchemaBase) As System.Collections.Generic.IDictionary(Of Object, T)
            Return CType(GetOrmDictionary(filterInfo, GetType(T), schema, oschema), IDictionary(Of Object, T))
        End Function

        Protected Overridable Function CreateDictionary4ObjectInstances(ByVal t As Type) As IDictionary
            Dim gt As Type = GetType(Collections.SynchronizedDictionary(Of ))
            gt = gt.MakeGenericType(New Type() {t})
            Return CType(gt.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IDictionary)
        End Function

        Public Function GetAllKeys() As System.Collections.ArrayList Implements IExploreCache.GetAllKeys
            Return New ArrayList(_rootObjectsDictionary.Keys)
        End Function

        Public Function GetDictionary_(ByVal key As Object) As System.Collections.IDictionary Implements IExploreCache.GetDictionary
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
            Dim pol As DictionatyCachePolicy = GetPolicy(t)
            Dim args() As Object = Nothing
            Dim dt As Type = Nothing
            If pol Is Nothing Then
                dt = GetType(Collections.SynchronizedDictionary(Of ))
            Else
                dt = GetType(OrmDictionary(Of ))
                args = GetArgs(t, pol)
            End If
            dt = dt.MakeGenericType(New Type() {t})
            Return CType(dt.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, _
                args), System.Collections.IDictionary)
        End Function

        Protected Function GetArgs(ByVal t As Type, ByVal pol As DictionatyCachePolicy) As Object()
            Return New Object() { _
                Me, pol.AbsoluteExpiration, pol.SlidingExpiration, _
                pol.Priority, pol.Dependency _
            }
        End Function

        Protected MustOverride Function GetPolicy(ByVal t As Type) As DictionatyCachePolicy

        Protected Overrides Function CreateListConverter() As IListObjectConverter
            Return New ListConverter
        End Function
    End Class
End Namespace