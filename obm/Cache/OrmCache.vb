Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Orm
Imports Worm.Database.Storedprocs
Imports Worm.Orm.Meta
Imports Worm.Criteria.Core
Imports Worm.Criteria.Values
Imports Worm.Orm.Query

Namespace Cache

    <Serializable()> _
    Public Class EntityProxy
        Private _id As Integer
        Private _t As Type

        Public Sub New(ByVal id As Integer, ByVal type As Type)
            _id = id
            _t = type
        End Sub

        Public Sub New(ByVal o As OrmBase)
            _id = o.Identifier
            _t = o.GetType
        End Sub

        Public Function GetEntity() As OrmBase
            Return OrmManagerBase.CurrentManager.Find(_id, _t)
        End Function

        Public ReadOnly Property OrmType() As Type
            Get
                Return _t
            End Get
        End Property

        Public ReadOnly Property ID() As Integer
            Get
                Return _id
            End Get
        End Property

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, EntityProxy))
        End Function

        Public Overloads Function Equals(ByVal obj As EntityProxy) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Return _t Is obj._t AndAlso _id = obj._id
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _t.GetHashCode() Xor _id.GetHashCode
        End Function
    End Class

    <Serializable()> _
    Public Class EntityField
        Private _field As String
        Private _t As Type

        Public Sub New(ByVal field As String, ByVal type As Type)
            _field = field
            _t = type
        End Sub

        Public ReadOnly Property OrmType() As Type
            Get
                Return _t
            End Get
        End Property

        Public ReadOnly Property Field() As String
            Get
                Return _field
            End Get
        End Property

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, EntityField))
        End Function

        Public Overloads Function Equals(ByVal obj As EntityField) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Return _t Is obj._t AndAlso _field = obj._field
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _t.GetHashCode() Xor _field.GetHashCode
        End Function
    End Class

    <Serializable()> _
    Public NotInheritable Class OrmCacheException
        Inherits Exception

        Public Sub New()
            ' Add other code for custom properties here.
        End Sub

        Public Sub New(ByVal message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(ByVal message As String, ByVal inner As Exception)
            MyBase.New(message, inner)
        End Sub

        Private Sub New( _
            ByVal info As System.Runtime.Serialization.SerializationInfo, _
            ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
        End Sub
    End Class

    ''' <summary>
    ''' Индексированая по полю <see cref="MapField2Column._fieldName"/> колекция объектов типа <see cref="MapField2Column"/>
    ''' </summary>
    ''' <remarks>
    ''' Наследник абстрактного класс <see cref="Collections.IndexedCollection(Of string, MapField2Column)"/>, реализующий метод <see cref="Collections.IndexedCollection(Of string, MapField2Column).GetKeyForItem" />
    ''' </remarks>
    Public Class OrmObjectIndex
        Inherits Collections.IndexedCollection(Of String, MapField2Column)

        ''' <summary>
        ''' Возвращает ключ коллекции MapField2Column
        ''' </summary>
        ''' <param name="item">Элемент коллекции</param>
        ''' <returns>Возвращает <see cref="MapField2Column._fieldName"/></returns>
        ''' <remarks>Используется при индексации коллекции</remarks>
        Protected Overrides Function GetKeyForItem(ByVal item As MapField2Column) As String
            Return item._fieldName
        End Function
    End Class

    Public Interface IExploreCache
        Function GetAllKeys() As ArrayList
        Function GetDictionary(ByVal key As Object) As IDictionary
    End Interface

    Public MustInherit Class OrmCacheBase

#Region " Classes "

        Private Class HashIds
            Inherits Dictionary(Of String, List(Of String))

            Private _default As New List(Of String)

            Public Function GetIds(ByVal hash As String) As List(Of String)
                If hash = EntityFilter.EmptyHash Then
                    Return _default
                Else
                    Dim h As List(Of String) = Nothing
                    If Not Me.TryGetValue(hash, h) Then
                        h = New List(Of String)
                        Me(hash) = h
                    End If
                    Return h
                End If
            End Function
        End Class

        Private Class TemplateHashs
            Inherits Dictionary(Of String, Pair(Of HashIds, IOrmFilterTemplate))

            Public Function GetIds(ByVal key As String, ByVal filter As IFilter) As List(Of String)
                Dim p As Pair(Of HashIds, IOrmFilterTemplate) = Nothing
                Dim f As IEntityFilter = TryCast(filter, IEntityFilter)
                If Not TryGetValue(key, p) Then
                    If f IsNot Nothing Then
                        p = New Pair(Of HashIds, IOrmFilterTemplate)(New HashIds, f.GetFilterTemplate)
                    Else
                        p = New Pair(Of HashIds, IOrmFilterTemplate)(New HashIds, Nothing)
                    End If
                    Me(key) = p
                End If
                If f IsNot Nothing Then
                    Return p.First.GetIds(f.MakeHash)
                Else
                    Return p.First.GetIds(EntityFilter.EmptyHash)
                End If
            End Function
        End Class

        Private Class TypeDepends
            Inherits Dictionary(Of Object, TemplateHashs)

            Public Function GetFilters(ByVal type As Object) As TemplateHashs
                Dim r As TemplateHashs = Nothing
                If Not TryGetValue(type, r) Then
                    r = New TemplateHashs
                    Me(type) = r
                End If
                Return r
            End Function
        End Class

#End Region

        Public ReadOnly DateTimeCreated As Date

        Protected _filters As IDictionary
        Private _modifiedobjects As IDictionary
        'Private _invalidate_types As New List(Of Type)
        ''' <summary>
        ''' dictionary.key - тип
        ''' dictionary.value - массив изменяемых полей
        ''' </summary>
        ''' <remarks></remarks>
        Private _invalidate As New Dictionary(Of Type, List(Of String))
        'Private _relations As New Dictionary(Of Type, List(Of Type))
        Private _object_depends As New Dictionary(Of EntityProxy, Dictionary(Of String, List(Of String)))
        Private _procs As New Dictionary(Of String, StoredProcBase)
        Private _procTypes As New Dictionary(Of Type, List(Of StoredProcBase))
        ''' <summary>
        ''' pair.first - поле
        ''' pair.second - тип
        ''' зависит от dictionary.key - ключ в кеше и dictionary.value - id в кеше
        ''' </summary>
        ''' <remarks></remarks>
        Private _field_depends As New Dictionary(Of EntityField, Dictionary(Of String, List(Of String)))

        Private _tp As New TypeDepends

        Private _ct_depends As New Dictionary(Of Type, Dictionary(Of String, Dictionary(Of String, Object)))

        Private _m2m_dep As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Object)))

        Private _loadTimes As New Dictionary(Of Type, Pair(Of Integer, TimeSpan))
        Private _jt As New Dictionary(Of Type, List(Of Type))

        Public Event CacheHasModification As EventHandler

        Public Event CacheHasnotModification As EventHandler

        Public Delegate Function EnumM2MCache(ByVal entity As OrmManagerBase.M2MCache) As Boolean

        Public Event RegisterObjectCreation(ByVal t As Type, ByVal id As Integer)
        Public Event RegisterObjectRemoval(ByVal obj As OrmBase)
        Public Event RegisterCollectionCreation(ByVal t As Type)
        Public Event RegisterCollectionRemoval(ByVal ce As OrmManagerBase.CachedItem)

        Public Interface IUpdateCacheCallbacks
            Sub BeginUpdate(ByVal count As Integer)
            Sub EndUpdate()
            Sub BeginUpdateProcs()
            Sub EndUpdateProcs()
            Sub BeginUpdateList(ByVal key As String, ByVal id As String)
            Sub EndUpdateList(ByVal key As String, ByVal id As String)
            Sub ObjectDependsUpdated(ByVal o As OrmBase)
        End Interface

        'Public Delegate Sub OnUpdateAfterDeleteEnd(ByVal o As OrmBase, ByVal mgr As OrmManagerBase)
        'Public Delegate Sub OnUpdateAfterAddEnd(ByVal o As OrmBase, ByVal mgr As OrmManagerBase, ByVal contextKey As Object)
        Public Delegate Sub OnUpdated(ByVal o As OrmBase, ByVal mgr As OrmManagerBase, ByVal contextKey As Object)

        Sub New()
            _filters = Hashtable.Synchronized(New Hashtable)
            DateTimeCreated = Now
            _modifiedobjects = Hashtable.Synchronized(New Hashtable)
        End Sub

#Region " general routines "

        Protected Friend ReadOnly Property Filters() As IDictionary
            Get
                Return _filters
            End Get
        End Property

        Public Function Modified(ByVal obj As OrmBase) As ModifiedObject
            Using SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                Dim name As String = obj.GetType().Name & ":" & obj.Identifier
                Return CType(_modifiedobjects(name), ModifiedObject)
            End Using
        End Function

        Public ReadOnly Property SyncRoot() As IDisposable
            Get
#If DebugLocks Then
                Return New CSScopeMgr_DebugWithStack(Me, "d:\temp\")
#Else
                Return New CSScopeMgr(Me)
#End If
            End Get
        End Property

        Protected Friend Function RegisterModification(ByVal obj As OrmBase) As ModifiedObject
            Using SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                Dim name As String = obj.GetType().Name & ":" & obj.Identifier
                Assert(OrmManagerBase.CurrentManager IsNot Nothing, "You have to create MediaContent object to perform this operation")
                Dim mo As New ModifiedObject(obj, OrmManagerBase.CurrentManager.CurrentUser)
                _modifiedobjects.Add(name, mo)
                If _modifiedobjects.Count = 1 Then
                    RaiseEvent CacheHasModification(Me, EventArgs.Empty)
                End If
                Return mo
            End Using
        End Function

        Public Function GetModifiedObject(Of T As {OrmBase, New})() As ICollection(Of T)
            Dim al As New Generic.List(Of T)
            Dim tt As Type = GetType(T)
            For Each s As String In New ArrayList(_modifiedobjects.Keys)
                If s.IndexOf(tt.Name & ":") >= 0 Then
                    Dim mo As ModifiedObject = CType(_modifiedobjects(s), ModifiedObject)
                    If mo IsNot Nothing Then
                        al.Add(CType(mo.Obj, T))
                    End If
                End If
            Next
            Return al
        End Function

        Protected Shared Sub Assert(ByVal condition As Boolean, ByVal message As String)
            Debug.Assert(condition, message)
            Trace.Assert(condition, message)
            If Not condition Then Throw New OrmCacheException(message)
        End Sub

        Protected Friend Function RegisterModification(ByVal obj As OrmBase, ByVal id As Integer) As ModifiedObject
            Using SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                Dim name As String = obj.GetType().Name & ":" & id
                Assert(OrmManagerBase.CurrentManager IsNot Nothing, "You have to create MediaContent object to perform this operation")
                Dim mo As New ModifiedObject(obj, OrmManagerBase.CurrentManager.CurrentUser)
                _modifiedobjects.Add(name, mo)
                If _modifiedobjects.Count = 1 Then
                    RaiseEvent CacheHasModification(Me, EventArgs.Empty)
                End If
                Return mo
            End Using
        End Function

        Protected Friend Sub RegisterExistingModification(ByVal obj As OrmBase, ByVal id As Integer)
            Using SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                Dim name As String = obj.GetType().Name & ":" & id
                _modifiedobjects.Add(name, obj.GetModifiedObject)
                If _modifiedobjects.Count = 1 Then
                    RaiseEvent CacheHasModification(Me, EventArgs.Empty)
                End If
            End Using
        End Sub

        Protected Friend Sub UnregisterModification(ByVal obj As OrmBase)
            Using SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                If _modifiedobjects.Count > 0 Then
                    Dim name As String = obj.GetType().Name & ":" & obj.Identifier
                    _modifiedobjects.Remove(name)
                    If _modifiedobjects.Count = 0 Then 'AndAlso obj.old_state <> ObjectState.Created Then
                        RaiseEvent CacheHasnotModification(Me, EventArgs.Empty)
                    End If
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

#End Region

        Public MustOverride Function CreateResultsetsDictionary() As IDictionary

        Public MustOverride Function GetOrmDictionary(ByVal t As Type, ByVal schema As OrmSchemaBase) As System.Collections.IDictionary

        Public MustOverride Function GetOrmDictionary(Of T)(ByVal schema As OrmSchemaBase) As System.Collections.Generic.IDictionary(Of Integer, T)

        Public Overridable Sub RegisterCreation(ByVal t As Type, ByVal id As Integer)
            RaiseEvent RegisterObjectCreation(t, id)
        End Sub

        Public Overridable Sub RegisterRemoval(ByVal obj As OrmBase)
            RaiseEvent RegisterObjectRemoval(obj)
            obj.RemoveFromCache(Me)
            RemoveDepends(obj)
        End Sub

        Public MustOverride Sub Reset()

        Public Overridable Sub RegisterCreationCacheItem(ByVal t As Type)
            RaiseEvent RegisterCollectionCreation(t)
        End Sub

        Public Overridable Sub RegisterRemovalCacheItem(ByVal ce As OrmManagerBase.CachedItem)
            RaiseEvent RegisterCollectionRemoval(ce)
        End Sub

        Public Function GetLoadTime(ByVal t As Type) As Pair(Of Integer, TimeSpan)
            Dim p As Pair(Of Integer, TimeSpan) = Nothing
            _loadTimes.TryGetValue(t, p)
            Return p
        End Function

        Protected Friend Sub LogLoadTime(ByVal obj As OrmBase, ByVal time As TimeSpan)
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

        Protected Friend Sub AddFieldDepend(ByVal p As Pair(Of String, Type), ByVal key As String, ByVal id As String)
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("9nhervg-jrgfl;jg94gt","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("9nhervg-jrgfl;jg94gt")
#End If

                Dim d As Dictionary(Of String, List(Of String)) = Nothing
                Dim ef As New EntityField(p.First, p.Second)
                If Not _field_depends.TryGetValue(ef, d) Then
                    d = New Dictionary(Of String, List(Of String))
                    _field_depends.Add(ef, d)
                End If
                Dim l As List(Of String) = Nothing
                If Not d.TryGetValue(id, l) Then
                    l = New List(Of String)
                    d.Add(id, l)
                End If
                If Not l.Contains(key) Then
                    l.Add(key)
                End If
            End Using
        End Sub

        Protected Friend Sub ResetFieldDepends(ByVal p As Pair(Of String, Type))
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("9nhervg-jrgfl;jg94gt","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("9nhervg-jrgfl;jg94gt")
#End If
                Dim d As Dictionary(Of String, List(Of String)) = Nothing
                Dim ef As New EntityField(p.First, p.Second)
                If _field_depends.TryGetValue(ef, d) Then
                    For Each ke As KeyValuePair(Of String, List(Of String)) In d
                        For Each key As String In ke.Value
                            Dim dic As IDictionary = CType(_filters(key), IDictionary)
                            dic.Remove(ke.Key)
                        Next
                    Next
                End If
            End Using
        End Sub

        'Protected Friend Function GetRelationValue(ByVal t As Type, ByRef l As List(Of Type)) As Boolean
        '    Using SyncHelper.AcquireDynamicLock("9h3fn013gf-qjnmerg135g")
        '        Return _relations.TryGetValue(t, l)
        '    End Using
        'End Function

        'Protected Friend Function HasActiveRelation(ByVal mainType As Type, ByVal subType As Type) As Boolean
        '    Using SyncHelper.AcquireDynamicLock("9h3fn013gf-qjnmerg135g")
        '        Dim l As New List(Of Type)
        '        If _relations.TryGetValue(mainType, l) Then
        '            Return l.Contains(subType)
        '        End If
        '    End Using
        '    Return False
        'End Function

        'Protected Friend Sub AddRelationValue(ByVal t As Type, ByVal subtype As Type)
        '    Using SyncHelper.AcquireDynamicLock("9h3fn013gf-qjnmerg135g")
        '        Dim l As List(Of Type) = Nothing
        '        If Not _relations.TryGetValue(t, l) Then
        '            l = New List(Of Type)
        '            _relations.Add(t, l)
        '        End If
        '        If Not l.Contains(subtype) Then
        '            l.Add(subtype)
        '        End If
        '    End Using
        'End Sub

        'Protected Friend Sub RemoveRelationValue(ByVal t As Type, ByVal subtype As Type)
        '    Using SyncHelper.AcquireDynamicLock("9h3fn013gf-qjnmerg135g")
        '        Dim l As List(Of Type) = Nothing
        '        If _relations.TryGetValue(t, l) Then
        '            l.Remove(subtype)
        '        End If
        '    End Using
        'End Sub

        Protected Friend Function GetUpdatedFields(ByVal t As Type, ByRef l As List(Of String)) As Boolean
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("913bh5g9nh04nvgtr0924ng","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("913bh5g9nh04nvgtr0924ng")
#End If

                Return _invalidate.TryGetValue(t, l)
            End Using
        End Function

        Protected Friend Sub AddUpdatedFields(ByVal obj As OrmBase, ByVal fields As ICollection(Of String))
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim t As Type = obj.GetType
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("913bh5g9nh04nvgtr0924ng","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("913bh5g9nh04nvgtr0924ng")
#End If

                Dim l As List(Of String) = Nothing
                If Not _invalidate.TryGetValue(t, l) Then
                    l = New List(Of String)
                    _invalidate.Add(t, l)
                End If
                For Each field As String In fields
                    If Not l.Contains(field) Then
                        l.Add(field)
                    End If
                Next
            End Using

            ValidateSPOnUpdate(obj, fields)
        End Sub

        Protected Friend Sub RemoveUpdatedFields(ByVal t As Type, ByVal field As String)
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("913bh5g9nh04nvgtr0924ng","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("913bh5g9nh04nvgtr0924ng")
#End If

                Dim l As List(Of String) = Nothing
                If _invalidate.TryGetValue(t, l) Then
                    l.Remove(field)
                End If
            End Using
        End Sub

        Protected Friend Sub AddConnectedDepend(ByVal t As Type, ByVal key As String, ByVal id As String)
            If t Is Nothing Then
                Throw New ArgumentNullException("t")
            End If
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("8907h13fkonhasdgft7","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("8907h13fkonhasdgft7")
#End If
                Dim l As Dictionary(Of String, Dictionary(Of String, Object)) = Nothing

                If _ct_depends.TryGetValue(t, l) Then
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
                    _ct_depends.Add(t, l)
                End If
            End Using
        End Sub

        Protected Friend Sub AddM2MObjDependent(ByVal obj As OrmBase, ByVal key As String, ByVal id As String)
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

        Protected Friend Function GetM2MEntries(ByVal obj As OrmBase, ByVal name As String) As ICollection(Of Pair(Of OrmManagerBase.M2MCache, Pair(Of String, String)))
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
                Dim etrs As New List(Of Pair(Of OrmManagerBase.M2MCache, Pair(Of String, String)))
                Dim l As Dictionary(Of String, Dictionary(Of String, Object)) = Nothing

                If _m2m_dep.TryGetValue(name, l) Then
                    For Each p As KeyValuePair(Of String, Dictionary(Of String, Object)) In l
                        Dim dic As IDictionary = CType(_filters(p.Key), IDictionary)
                        For Each id As String In p.Value.Keys
                            Dim ce As OrmManagerBase.M2MCache = TryCast(dic(id), OrmManagerBase.M2MCache)
                            etrs.Add(New Pair(Of OrmManagerBase.M2MCache, Pair(Of String, String))(ce, New Pair(Of String, String)(p.Key, id)))
                        Next
                    Next
                End If
                Return etrs
            End Using
        End Function

        Protected Friend Sub UpdateM2MEntries(ByVal obj As OrmBase, ByVal oldId As Integer, ByVal name As String)
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

        Protected Friend Sub ConnectedEntityEnum(ByVal ct As Type, ByVal f As EnumM2MCache)
            If ct Is Nothing Then
                Throw New ArgumentNullException("ct")
            End If

#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("8907h13fkonhasdgft7","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("8907h13fkonhasdgft7")
#End If
                Dim l As Dictionary(Of String, Dictionary(Of String, Object)) = Nothing

                If _ct_depends.TryGetValue(ct, l) Then
                    For Each p As KeyValuePair(Of String, Dictionary(Of String, Object)) In l
                        Dim dic As IDictionary = CType(_filters(p.Key), IDictionary)
                        'Dim b As Boolean = False
                        Dim remove As New List(Of String)
                        For Each id As String In p.Value.Keys
                            Dim ce As OrmManagerBase.M2MCache = TryCast(dic(id), OrmManagerBase.M2MCache)
                            If ce IsNot Nothing Then
                                If Not f(ce) Then
                                    remove.Add(id)
                                End If
                            End If
                        Next
                        For Each id As String In remove
                            dic.Remove(id)
                        Next
                        'If Not b Then
                        '    Throw New OrmManagerException(String.Format("Invalid cache entry {0} for type {1}", p.Key, ct))
                        'End If
                    Next
                End If
            End Using
        End Sub

        Protected Friend Sub UpdateCache(ByVal schema As OrmSchemaBase, _
            ByVal objs As IList, ByVal mgr As OrmManagerBase, ByVal afterDelegate As OnUpdated, _
            ByVal contextKey As Object, ByVal callbacks As IUpdateCacheCallbacks, Optional ByVal forseEval As Boolean = False)

            Dim tt As Type = Nothing
            For Each obj As OrmBase In objs
                If obj Is Nothing Then
                    Throw New ArgumentException("At least one element in objs is nothing")
                End If
                tt = obj.GetType
                Exit For
            Next

            Dim oschema As IOrmObjectSchemaBase = schema.GetObjectSchema(tt)
            Dim c As ICacheBehavior = TryCast(oschema, ICacheBehavior)
            Dim k As Object = tt
            If c IsNot Nothing Then
                k = c.GetEntityTypeKey
            End If

#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("j13rvnopqefv9-n24bth","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("j13rvnopqefv9-n24bth")
#End If
                Dim l As TemplateHashs = _tp.GetFilters(k)
                If l.Count > 0 Then
                    For Each p As KeyValuePair(Of String, Pair(Of HashIds, IOrmFilterTemplate)) In l
                        Dim dic As IDictionary = CType(_filters(p.Key), IDictionary)
                        If dic IsNot Nothing Then
                            If callbacks IsNot Nothing Then
                                callbacks.BeginUpdate(0)
                            End If
                            For Each obj As OrmBase In objs
                                If obj Is Nothing Then
                                    Throw New ArgumentException("At least one element in objs is nothing")
                                End If

                                If tt IsNot obj.GetType Then
                                    Throw New ArgumentException("Collection contains different types")
                                End If

                                Dim h As String = EntityFilter.EmptyHash
                                If p.Value.Second IsNot Nothing Then
                                    h = p.Value.Second.MakeHash(schema, oschema, obj)
                                End If
                                Dim ids As List(Of String) = p.Value.First.GetIds(h)
                                Dim rm As New List(Of String)
                                For Each id As String In ids
                                    Dim ce As OrmManagerBase.CachedItem = TryCast(dic(id), OrmManagerBase.CachedItem)
                                    Dim f As IEntityFilter = Nothing
                                    If ce IsNot Nothing Then
                                        f = TryCast(ce.Filter, IEntityFilter)
                                    End If
                                    If ce IsNot Nothing AndAlso f IsNot Nothing Then
                                        If callbacks IsNot Nothing Then
                                            callbacks.BeginUpdateList(p.Key, id)
                                        End If

                                        If obj._needAdd OrElse obj._needDelete OrElse forseEval Then
                                            Dim r As Boolean = False
                                            Dim er As IEvaluableValue.EvalResult = IEvaluableValue.EvalResult.Found
                                            If f IsNot Nothing Then
                                                er = f.Eval(schema, obj, oschema)
                                                r = er = IEvaluableValue.EvalResult.Unknown
                                            End If

                                            If r Then
                                                dic.Remove(id)
                                            ElseIf er = IEvaluableValue.EvalResult.Found Then
                                                Dim sync As String = id & mgr.GetStaticKey
                                                Using SyncHelper.AcquireDynamicLock(sync)
                                                    If obj._needAdd Then
                                                        If Not ce.Add(mgr, obj) Then
                                                            dic.Remove(id)
                                                        End If
                                                    ElseIf obj._needDelete Then
                                                        ce.Delete(mgr, obj)
                                                        'Else
                                                        '    Throw New InvalidOperationException
                                                    End If
                                                End Using
                                                If callbacks IsNot Nothing Then
                                                    callbacks.ObjectDependsUpdated(obj)
                                                End If
                                            End If
                                            'Else
                                            '    Assert(False, "Object must be in appropriate state")
                                        End If
                                    Else
                                        rm.Add(id)
                                    End If

                                    If callbacks IsNot Nothing Then
                                        callbacks.EndUpdateList(p.Key, id)
                                    End If
                                Next

                                For Each id As String In rm
                                    ids.Remove(id)
                                    dic.Remove(id)
                                Next
                            Next
                            If callbacks IsNot Nothing Then
                                callbacks.EndUpdate()
                            End If
                            '_filters.Remove(p.Key)
                        End If
                    Next
                End If
            End Using

            If callbacks IsNot Nothing Then
                callbacks.BeginUpdateProcs()
            End If
            For Each obj As OrmBase In objs
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                If obj._needAdd Then
                    ValidateSPOnInsertDelete(obj)
                ElseIf obj._needDelete Then
                    ValidateSPOnInsertDelete(obj)
                    'Else
                    '    Throw New InvalidOperationException
                End If

                If afterDelegate IsNot Nothing Then
                    afterDelegate(obj, mgr, contextKey)
                End If
            Next
            If callbacks IsNot Nothing Then
                callbacks.EndUpdateProcs()
            End If

#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("9-134g9ngpadfbgp","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("9-134g9ngpadfbgp")
#End If
                Dim ts As List(Of Type) = Nothing
                If _jt.TryGetValue(tt, ts) Then
                    For Each t As Type In ts
                        For Each obj As OrmBase In objs
                            If obj Is Nothing Then
                                Throw New ArgumentException("At least one element in objs is nothing")
                            End If

                            If tt IsNot obj.GetType Then
                                Throw New ArgumentException("Collection contains different types")
                            End If

                            Dim o As OrmBase = schema.GetJoinObj(oschema, obj, t)

                            If o IsNot Nothing Then
                                UpdateCache(schema, New OrmBase() {o}, mgr, afterDelegate, contextKey, callbacks, True)
                            End If
                        Next
                    Next
                End If
            End Using
        End Sub

        ''' <summary>
        ''' Зависимость выбираемого типа от ключа в кеше
        ''' </summary>
        ''' <param name="t"></param>
        ''' <param name="key"></param>
        ''' <param name="id"></param>
        ''' <remarks></remarks>
        Protected Friend Sub AddDependType(ByVal t As Type, ByVal key As String, ByVal id As String, ByVal f As IFilter, ByVal schema As OrmSchemaBase)
            'Debug.WriteLine(t.Name & ": add dependent " & id)
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("j13rvnopqefv9-n24bth","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("j13rvnopqefv9-n24bth")
#End If
                Dim l As TemplateHashs = _tp.GetFilters(schema.GetEntityTypeKey(t))
                Dim h As List(Of String) = l.GetIds(key, f)
                If Not h.Contains(id) Then
                    h.Add(id)
                End If
            End Using
        End Sub

        Protected Friend Sub AddDependType(ByVal t As Type, ByVal key As String, ByVal id As String, _
            ByVal asc() As QueryAspect, ByVal schema As OrmSchemaBase)

        End Sub

        Protected Friend Sub AddJoinDepend(ByVal joinType As Type, ByVal selectType As Type)
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("9-134g9ngpadfbgp","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("9-134g9ngpadfbgp")
#End If
                Dim l As List(Of Type) = Nothing
                If Not _jt.TryGetValue(joinType, l) Then
                    l = New List(Of Type)
                    _jt(joinType) = l
                End If
                If Not l.Contains(selectType) Then
                    l.Add(selectType)
                End If
            End Using
        End Sub

        ''' <summary>
        ''' Зависимость экземпляра объекта от ключа (объект присутствует в фильтре)
        ''' </summary>
        ''' <param name="obj"></param>
        ''' <param name="key"></param>
        ''' <param name="id"></param>
        ''' <remarks></remarks>
        Protected Friend Sub AddDepend(ByVal obj As OrmBase, ByVal key As String, ByVal id As String)
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("1380fbhj145g90h2evgrqervg","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("1380fbhj145g90h2evgrqervg")
#End If
                Dim l As Dictionary(Of String, List(Of String)) = Nothing
                Dim op As New EntityProxy(obj)
                If _object_depends.TryGetValue(op, l) Then
                    Dim ll As List(Of String) = Nothing
                    If l.TryGetValue(key, ll) Then
                        Dim idx As Integer = ll.BinarySearch(id)
                        If idx < 0 Then
                            ll.Insert(Not idx, id)
                        End If
                    Else
                        ll = New List(Of String)
                        ll.Add(id)
                        l.Add(key, ll)
                    End If
                Else
                    l = New Dictionary(Of String, List(Of String))
                    Dim ll As New List(Of String)
                    ll.Add(id)
                    l.Add(key, ll)
                    _object_depends.Add(op, l)
                End If
            End Using
        End Sub

        ''' <summary>
        ''' Удаляет все ключи в кеше, которые зависят от данного объекта
        ''' </summary>
        ''' <param name="obj"></param>
        ''' <remarks></remarks>
        Protected Friend Sub RemoveDepends(ByVal obj As OrmBase)
            Dim op As New EntityProxy(obj)
            If _object_depends.ContainsKey(op) Then
#If DebugLocks Then
                Using SyncHelper.AcquireDynamicLock_Debug("1380fbhj145g90h2evgrqervg","d:\temp\")
#Else
                Using SyncHelper.AcquireDynamicLock("1380fbhj145g90h2evgrqervg")
#End If
                    Dim l As Dictionary(Of String, List(Of String)) = Nothing
                    If _object_depends.TryGetValue(op, l) Then
                        For Each p As KeyValuePair(Of String, List(Of String)) In l
                            Dim dic As IDictionary = CType(_filters(p.Key), IDictionary)
                            If dic IsNot Nothing Then
                                For Each id As String In p.Value
                                    dic.Remove(id)
                                Next
                                _filters.Remove(p.Key)
                            End If
                        Next
                        _object_depends.Remove(op)
                    End If
                End Using
            End If
        End Sub

        Private Sub AddStoredProcType(ByVal sp As StoredProcBase, ByVal t As Type)
            Dim l As List(Of StoredProcBase) = Nothing
            If _procTypes.TryGetValue(t, l) Then
                Dim pos As Integer = l.IndexOf(sp)
                If pos < 0 Then
                    l.Add(sp)
                Else
                    l(pos) = sp
                End If
            Else
                l = New List(Of StoredProcBase)
                l.Add(sp)
                _procTypes(t) = l
            End If
        End Sub

        Protected Friend Sub AddStoredProc(ByVal key As String, ByVal sp As StoredProcBase)
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("olnfv9807b45gnpoweg01j3g","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("olnfv9807b45gnpoweg01j3g")
#End If
                If sp.Cached Then
                    _procs(key) = sp
                    Dim types As ICollection(Of Type) = sp.GetTypesToValidate
                    If types IsNot Nothing AndAlso types.Count > 0 Then
                        For Each t As Type In types
                            AddStoredProcType(sp, t)
                        Next
                    Else
                        AddStoredProcType(sp, GetType(Object))
                    End If
                End If
            End Using
        End Sub

        Private Sub ValidateSPByType(ByVal t As Type, ByVal obj As OrmBase)
            Dim l As List(Of StoredProcBase) = Nothing
            If _procTypes.TryGetValue(t, l) Then
                For Each sp As StoredProcBase In l
                    If Not sp.IsReseted AndAlso sp.ValidateOnInsertDelete(obj) Then
                        sp.ResetCache(Me)
                    End If
                Next
            End If
        End Sub

        Private Sub ValidateUpdateSPByType(ByVal t As Type, ByVal obj As OrmBase, ByVal fields As ICollection(Of String))
            Dim l As List(Of StoredProcBase) = Nothing
            If _procTypes.TryGetValue(t, l) Then
                For Each sp As StoredProcBase In l
                    If Not sp.IsReseted AndAlso sp.ValidateOnUpdate(obj, fields) Then
                        sp.ResetCache(Me)
                    End If
                Next
            End If
        End Sub

        Protected Sub ValidateSPOnInsertDelete(ByVal obj As OrmBase)
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("olnfv9807b45gnpoweg01j3g","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("olnfv9807b45gnpoweg01j3g")
#End If
                ValidateSPByType(obj.GetType, obj)
                ValidateSPByType(GetType(Object), obj)
            End Using
        End Sub

        Protected Sub ValidateSPOnUpdate(ByVal obj As OrmBase, ByVal fields As ICollection(Of String))
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("olnfv9807b45gnpoweg01j3g","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("olnfv9807b45gnpoweg01j3g")
#End If
                ValidateUpdateSPByType(obj.GetType, obj, fields)
                ValidateUpdateSPByType(GetType(Object), obj, fields)
            End Using
        End Sub

        Public Overridable Function CreateResultsetsDictionary(ByVal mark As String) As IDictionary
            If String.IsNullOrEmpty(mark) Then
                Return CreateResultsetsDictionary()
            End If
            Throw New NotImplementedException(String.Format("Mark {0} is not supported", mark))
        End Function
    End Class

    Public Class OrmCache
        Inherits OrmCacheBase
        Implements IExploreCache

        Private _dics As IDictionary = Hashtable.Synchronized(New Hashtable)

        Public Overrides Sub Reset()
            _dics = Hashtable.Synchronized(New Hashtable)
        End Sub

        Public Overrides Function CreateResultsetsDictionary() As System.Collections.IDictionary
            Return Hashtable.Synchronized(New Hashtable)
        End Function

        'Public Overrides ReadOnly Property OrmDictionaryT(of T)() As System.Collections.Generic.IDictionary(Of Integer, T)

        Public Overrides Function GetOrmDictionary(ByVal t As System.Type, ByVal schema As OrmSchemaBase) As System.Collections.IDictionary
            Dim k As Object = t
            If schema IsNot Nothing Then
                k = schema.GetEntityTypeKey(t)
            End If

            Dim dic As IDictionary = CType(_dics(k), IDictionary)
            If dic Is Nothing Then
                Using SyncRoot
                    dic = CType(_dics(k), IDictionary)
                    If dic Is Nothing Then
                        dic = CreateDictionary(t)
                        _dics(k) = dic
                    End If
                End Using
            End If
            Return dic
        End Function

        Public Overrides Function GetOrmDictionary(Of T)(ByVal schema As OrmSchemaBase) As System.Collections.Generic.IDictionary(Of Integer, T)
            Return CType(GetOrmDictionary(GetType(T), schema), IDictionary(Of Integer, T))
        End Function

        Public Sub New()
            Reset()
        End Sub

        Protected Overridable Function CreateDictionary(ByVal t As Type) As IDictionary
            Dim gt As Type = GetType(Collections.HybridDictionary(Of ))
            gt = gt.MakeGenericType(New Type() {t})
            Return CType(gt.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IDictionary)
        End Function

        Public Function GetAllKeys() As System.Collections.ArrayList Implements IExploreCache.GetAllKeys
            Return New ArrayList(_dics.Keys)
        End Function

        Public Function GetDictionary(ByVal key As Object) As System.Collections.IDictionary Implements IExploreCache.GetDictionary
            Return CType(_dics(key), System.Collections.IDictionary)
        End Function
    End Class

    <Serializable()> _
    Public Class ModifiedObject
        Public ReadOnly User As Object
        <CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104")> _
        Public ReadOnly Obj As OrmBase
        Public ReadOnly DateTime As Date

        Sub New(ByVal obj As OrmBase, ByVal user As Object)
            DateTime = Now
            Me.Obj = obj
            Me.User = user
        End Sub

    End Class

    Public MustInherit Class WebCache
        Inherits OrmCache

        'Private _dics As IDictionary = Hashtable.Synchronized(New Hashtable)

        'Public Overrides Function GetOrmDictionary(ByVal t As System.Type, ByVal schema As OrmSchemaBase) As System.Collections.IDictionary

        '    Dim td As IDictionary = CType(_dics(t), System.Collections.IDictionary)
        '    If td Is Nothing Then
        '        using _dics.SyncRoot
        '            td = CType(_dics(t), System.Collections.IDictionary)
        '            If td Is Nothing Then
        '                Dim pol As DictionatyCachePolicy = GetPolicy(t)
        '                Dim dt As Type = GetType(OrmDictionary(Of ))
        '                dt = dt.MakeGenericType(New Type() {t})
        '                Dim args() As Object = GetArgs(t, pol)
        '                td = CType(dt.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, _
        '                    args), System.Collections.IDictionary)
        '            End If
        '        End using
        '    End If

        '    Return td
        'End Function

        Protected Overrides Function CreateDictionary(ByVal t As System.Type) As System.Collections.IDictionary
            Dim pol As DictionatyCachePolicy = GetPolicy(t)
            Dim args() As Object = Nothing
            Dim dt As Type = Nothing
            If pol Is Nothing Then
                dt = GetType(Collections.HybridDictionary(Of ))
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

    End Class
End Namespace
