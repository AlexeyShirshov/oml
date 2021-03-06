Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.Criteria.Values
Imports Worm.Query.Sorting
Imports Worm.Query
Imports Worm.Expressions2
Imports System.Linq

#Const TraceCreation = False

Namespace Cache

    Public Class OrmCache
        Inherits ReadonlyCache

        Public Delegate Function CreateCacheListDelegate(ByVal mark As String) As IDictionary

#Region " Classes "

        Private Class HashIds
            Inherits Dictionary(Of String, HashSet(Of String))

            Private _default As New HashSet(Of String)

            'Public Function GetIds(ByVal hash As String) As IEnumerable(Of String)
            '    'If hash = EntityFilter.EmptyHash Then
            '    '    Return _default
            '    'Else
            '    '    Dim h As List(Of String) = Nothing
            '    '    If Not Me.TryGetValue(hash, h) Then
            '    '        h = New List(Of String)
            '    '        Me(hash) = h
            '    '    End If
            '    '    Return h
            '    'End If
            '    Return GetIds2(hash)
            'End Function

            Public Function GetIds(ByVal hash As String) As HashSet(Of String)
                If hash = EntityFilter.EmptyHash Then
                    Return _default
                Else
                    Dim h As HashSet(Of String) = Nothing
                    If Not Me.TryGetValue(hash, h) Then
                        h = New HashSet(Of String)
                        Me(hash) = h
                    End If
                    Return h
                End If
            End Function

            Public Overloads Sub Remove(ByVal hash As String, ByVal id As String)
                GetIds(hash).Remove(id)
            End Sub
        End Class

        Private Class TemplateHashs
            Inherits Dictionary(Of String, Tuple(Of HashIds, IOrmFilterTemplate))

            Private Function GetIds(ByVal key As String, ByVal filter As IFilter, ByRef def As Boolean) As HashSet(Of String)
                Dim p As Tuple(Of HashIds, IOrmFilterTemplate) = Nothing
                Dim f As IEntityFilter = TryCast(filter, IEntityFilter)
                def = f Is Nothing
                If Not TryGetValue(key, p) Then
                    If Not def Then
                        p = New Tuple(Of HashIds, IOrmFilterTemplate)(New HashIds, f.GetFilterTemplate)
                    Else
                        p = New Tuple(Of HashIds, IOrmFilterTemplate)(New HashIds, Nothing)
                    End If
                    Me(key) = p
                End If
                If Not def Then
                    Return p.Item1.GetIds(f.MakeHash)
                Else
                    Return p.Item1.GetIds(EntityFilter.EmptyHash)
                End If
            End Function

            Public Overloads Function Add(ByVal key As String, ByVal f As IFilter, ByVal id As String) As Boolean
                Dim def As Boolean
                Dim hs = GetIds(key, f, def)
                If Not hs.Contains(id) Then
                    hs.Add(id)
                End If
                Return Not def
            End Function
        End Class

        Private Class Type2SelectiveFiltersDepends
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

        Private Class CacheEntryRef
            Inherits Dictionary(Of String, Dictionary(Of String, Object))

            Public Overloads Sub Add(ByVal key As String, ByVal id As String)
                Dim c As Dictionary(Of String, Object) = Nothing
                If Not TryGetValue(key, c) Then
                    c = New Dictionary(Of String, Object)
                    Add(key, c)
                End If
                c(id) = Nothing
            End Sub

            Public Overloads Sub Remove(ByVal cache As CacheBase)
                For Each kv As KeyValuePair(Of String, Dictionary(Of String, Object)) In Me
                    Dim key As String = kv.Key
                    For Each kv2 As KeyValuePair(Of String, Object) In kv.Value
                        Dim id As String = kv2.Key
                        cache.RemoveEntry(key, id)
                    Next
                Next
            End Sub
        End Class

        Private Class TypeEntryRef
            Inherits Dictionary(Of Type, CacheEntryRef)

            Public Overloads Sub Add(ByVal t As Type, ByVal key As String, ByVal id As String)
                Dim c As CacheEntryRef = Nothing
                If Not TryGetValue(t, c) Then
                    c = New CacheEntryRef
                    Add(t, c)
                End If
                c.Add(key, id)
            End Sub

            Public Overloads Function Remove(ByVal t As Type, ByVal cache As CacheBase) As Boolean
                Dim c As CacheEntryRef = Nothing
                If TryGetValue(t, c) Then
                    c.Remove(cache)
                    Remove(t)
                End If
                Return c IsNot Nothing
            End Function
        End Class

        Private Class FieldsRef
            Inherits Dictionary(Of EntityField, CacheEntryRef)

            Private _t As New Dictionary(Of Type, List(Of EntityField))

            Public Overloads Sub Add(ByVal ef As EntityField, ByVal key As String, ByVal id As String)
                Dim c As CacheEntryRef = Nothing
                If Not TryGetValue(ef, c) Then
                    c = New CacheEntryRef
                    Add(ef, c)
                End If
                c.Add(key, id)
                Dim l As List(Of EntityField) = Nothing
                If Not _t.TryGetValue(ef.OrmType, l) Then
                    l = New List(Of EntityField)
                    _t.Add(ef.OrmType, l)
                End If
                l.Add(ef)
            End Sub

            Public Overloads Function Remove(ByVal t As Type, ByVal cache As CacheBase) As Boolean
                Dim l As List(Of EntityField) = Nothing
                If _t.TryGetValue(t, l) Then
                    For Each ef As EntityField In l
                        Remove(ef, cache, False)
                    Next
                    _t.Remove(t)
                    Return True
                End If
                Return False
            End Function

            Public Overloads Function Remove(ByVal ef As EntityField, ByVal cache As CacheBase, Optional ByVal remTypes As Boolean = True) As Boolean
                Dim c As CacheEntryRef = Nothing
                If TryGetValue(ef, c) Then
                    Remove(ef)
                    If remTypes Then
                        c.Remove(cache)
                        Dim l As List(Of EntityField) = Nothing
                        If _t.TryGetValue(ef.OrmType, l) Then
                            l.Remove(ef)
                        End If
                    End If
                End If
                Return c IsNot Nothing
            End Function

            Public Overloads Function Remove(ByVal t As Type, ByVal fieldName As String, ByVal cache As CacheBase) As Boolean
                Dim ef As New EntityField(fieldName, t)
                Return Remove(ef, cache)
            End Function
        End Class

        'Private Class CacheEntry
        '    Private _fields As New List(Of EntityField)
        '    Private _updTypes As New List(Of Type)
        'End Class
#End Region

        Private _md As CreateCacheListDelegate

        'Public ReadOnly DateTimeCreated As Date
        Private _deferredValidate As Boolean

        'Protected _filters As IDictionary
        'Private _invalidate_types As New List(Of Type)
        ''' <summary>
        ''' dictionary.key - ���
        ''' dictionary.value - ������ ���������� �����
        ''' </summary>
        ''' <remarks></remarks>
        Private _invalidate As New Dictionary(Of Type, List(Of String))
        'Private _relations As New Dictionary(Of Type, List(Of Type))
        Private _object_depends As New Dictionary(Of EntityProxy, Dictionary(Of String, List(Of String)))
        '''' <summary>
        '''' pair.first - ����
        '''' pair.second - ���
        '''' ������� �� dictionary.key - ���� � ���� � dictionary.value - id � ����
        '''' </summary>
        '''' <remarks></remarks>
        'Private _field_depends As New Dictionary(Of EntityField, Dictionary(Of String, List(Of String)))

        Private _lock As New Object
        Private _beh As CacheListBehavior

        'Private _tp As New Type2SelectiveFiltersDepends
        'Private _qt As New Dictionary(Of Object, Dictionary(Of String, Pair(Of String)))
        Private _ct_depends As New Dictionary(Of Type, Dictionary(Of String, Dictionary(Of String, Object)))

        Private _m2mSimpleQueries As New Dictionary(Of M2MRelation, CacheEntryRef)

        'Private _loadTimes As New Dictionary(Of Type, Pair(Of Integer, TimeSpan))
        'Private _jt As New Dictionary(Of Type, List(Of Type))

        Private _trackDelete As New Dictionary(Of Type, Pair(Of Integer, List(Of Integer)))

        Private _addDeleteTypes As New TypeEntryRef
        Private _updateTypes As New TypeEntryRef
        Private _immediateValidate As New Type2SelectiveFiltersDepends
        Private _filteredFields As New FieldsRef
        Private _sortedFields As New FieldsRef
        Private _groupedFields As New FieldsRef
        Private _addedTypes As New Dictionary(Of Type, Type)
        Private _deletedTypes As New Dictionary(Of Type, Type)
        'Private _m2mQueries As New Dictionary(Of M2MRelation, CacheEntryRef)
#If OLDM2M Then
        Public Delegate Function EnumM2MCache(ByVal mgr As OrmManager, ByVal entity As M2MCache) As Boolean
#End If

        Public Event OnObjectUpdated(ByVal cache As OrmCache, ByVal obj As _ICachedEntity, ByVal fields As ICollection(Of String))
        Public Event OnObjectAdded(ByVal cache As OrmCache, ByVal obj As _ICachedEntity)
        Public Event OnObjectDeleted(ByVal cache As OrmCache, ByVal obj As _ICachedEntity)

        'Public Delegate Sub OnUpdateAfterDeleteEnd(ByVal o As OrmBase, ByVal mgr As OrmManager)
        'Public Delegate Sub OnUpdateAfterAddEnd(ByVal o As OrmBase, ByVal mgr As OrmManager, ByVal contextKey As Object)
        Public Delegate Sub OnUpdated(ByVal o As _ICachedEntity, ByVal mgr As OrmManager, ByVal contextKey As Object)

        'Sub New()
        '    MyBase.new()
        '    '_filters = Hashtable.Synchronized(New Hashtable)
        '    'DateTimeCreated = Now
        'End Sub
        Const ImmediateDynamicLock = "BLK$E&80erfvhbdvdksv"
        Const TrackDeleteLock = "309fjsdfas;d"
        Const AddDeleteTypeLock = "(_H* 234ngf90ganv"
        Const UpdateTypeLock = "%G(qjg'oqgiu13rgfasd"
        Const InvalidateLock = "913bh5g9nh04nvgtr0924ng"
        Const SortGroupFilterLock = "N(4nfasd*)Gf"
        Const AddedDeleted = "qoegnq"
#Region " general routines "

        'Protected Friend ReadOnly Property Filters() As IDictionary
        '    Get
        '        Return _filters
        '    End Get
        'End Property

        'Protected Friend Function RegisterModification(ByVal obj As ICachedEntity, ByVal id As Integer, ByVal reason As ModifiedObject.ReasonEnum) As ModifiedObject
        '    Using SyncRoot
        '        If obj Is Nothing Then
        '            Throw New ArgumentNullException("obj")
        '        End If

        '        Dim mo As ModifiedObject = Nothing
        '        Dim name As String = obj.GetType().Name & ":" & id
        '        'Using SyncHelper.AcquireDynamicLock(name)
        '        Assert(OrmManager.CurrentManager IsNot Nothing, "You have to create MediaContent object to perform this operation")
        '        Assert(Not _modifiedobjects.Contains(name), "Key " & name & " already in collection")
        '        mo = New ModifiedObject(obj, OrmManager.CurrentManager.CurrentUser, reason)
        '        _modifiedobjects.Add(name, mo)
        '        'End Using
        '        If _modifiedobjects.Count = 1 Then
        '            RaiseEvent CacheHasModification(Me, EventArgs.Empty)
        '        End If
        '        Return mo
        '    End Using
        'End Function

        Public Overrides ReadOnly Property IsReadonly() As Boolean
            Get
                Return False
            End Get
        End Property

#End Region

        Public Property ValidateBehavior() As ValidateBehavior
            Get
                If _deferredValidate Then
                    Return Cache.ValidateBehavior.Deferred
                Else
                    Return Cache.ValidateBehavior.Immediate
                End If
            End Get
            Set(ByVal value As ValidateBehavior)
                Select Case value
                    Case Cache.ValidateBehavior.Deferred
                        _deferredValidate = True
                    Case Cache.ValidateBehavior.Immediate
                        _deferredValidate = False
                    Case Else
                        Throw New NotSupportedException("Values " & value.ToString & " is not supported")
                End Select
            End Set
        End Property

        Public Overrides Property CacheListBehavior() As CacheListBehavior
            Get
                Return _beh
            End Get
            Set(ByVal value As CacheListBehavior)
                _beh = value
            End Set
        End Property

        Friend Sub BeginTrackDelete(ByVal t As Type)
            Using SyncHelper.AcquireDynamicLock(TrackDeleteLock)
                Dim p As Pair(Of Integer, List(Of Integer)) = Nothing
                If Not _trackDelete.TryGetValue(t, p) Then
                    _trackDelete(t) = New Pair(Of Integer, List(Of Integer))(0, New List(Of Integer))
                Else
                    _trackDelete(t) = New Pair(Of Integer, List(Of Integer))(p.First + 1, p.Second)
                End If
            End Using
        End Sub

        Friend Sub EndTrackDelete(ByVal t As Type)
            Using SyncHelper.AcquireDynamicLock(TrackDeleteLock)
                Dim p As Pair(Of Integer, List(Of Integer)) = Nothing
                If _trackDelete.TryGetValue(t, p) Then
                    If p.First = 0 Then
                        _trackDelete.Remove(t)
                    Else
                        _trackDelete(t) = New Pair(Of Integer, List(Of Integer))(p.First - 1, p.Second)
                    End If
                End If
            End Using
        End Sub

        Friend Sub RegisterDelete(ByVal obj As ICachedEntity)
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim t As Type = obj.GetType

            Using SyncHelper.AcquireDynamicLock(TrackDeleteLock)
                Dim p As Pair(Of Integer, List(Of Integer)) = Nothing
                If _trackDelete.TryGetValue(t, p) Then 'AndAlso Not p.Second.Contains(obj.Identifier)
                    p.Second.Add(obj.Key)
                    'Else
                    '    Throw New OrmCacheException("I have to call BeginTrackDelete")
                End If
            End Using

        End Sub

        Friend Function IsDeleted(ByVal obj As ICachedEntity) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            'If obj Is Nothing Then
            '    Throw New ArgumentNullException("obj")
            'End If

            Dim t As Type = obj.GetType

            Return IsDeleted(t, obj.Key)
        End Function

        Friend Function IsDeleted(ByVal t As Type, ByVal key As Integer) As Boolean
            Using SyncHelper.AcquireDynamicLock(TrackDeleteLock)
                Dim p As Pair(Of Integer, List(Of Integer)) = Nothing
                If _trackDelete.TryGetValue(t, p) Then
                    Dim idx As Integer = p.Second.IndexOf(key)
                    'If idx >= 0 Then
                    '    l.RemoveAt(idx)
                    'End If
                    Return idx >= 0
                    'Else
                    '    Throw New OrmCacheException("I have to call BeginTrackDelete")
                End If

                Return False
            End Using
        End Function

        '        Protected Friend Sub AddFieldDepend(ByVal p As Pair(Of String, Type), ByVal key As String, ByVal id As String)
        '#If DebugLocks Then
        '            Using SyncHelper.AcquireDynamicLock_Debug("9nhervg-jrgfl;jg94gt","d:\temp\")
        '#Else
        '            Using SyncHelper.AcquireDynamicLock("9nhervg-jrgfl;jg94gt")
        '#End If

        '                Dim d As Dictionary(Of String, List(Of String)) = Nothing
        '                Dim ef As New EntityField(p.First, p.Second)
        '                If Not _field_depends.TryGetValue(ef, d) Then
        '                    d = New Dictionary(Of String, List(Of String))
        '                    _field_depends.Add(ef, d)
        '                End If
        '                Dim l As List(Of String) = Nothing
        '                If Not d.TryGetValue(id, l) Then
        '                    l = New List(Of String)
        '                    d.Add(id, l)
        '                End If
        '                If Not l.Contains(key) Then
        '                    l.Add(key)
        '                End If
        '            End Using
        '        End Sub

        '        Protected Friend Function ResetFieldDepends(ByVal p As Pair(Of String, Type)) As Boolean
        '            Dim rv As Boolean
        '#If DebugLocks Then
        '            Using SyncHelper.AcquireDynamicLock_Debug("9nhervg-jrgfl;jg94gt","d:\temp\")
        '#Else
        '            Using SyncHelper.AcquireDynamicLock("9nhervg-jrgfl;jg94gt")
        '#End If
        '                Dim d As Dictionary(Of String, List(Of String)) = Nothing
        '                Dim ef As New EntityField(p.First, p.Second)
        '                If _field_depends.TryGetValue(ef, d) Then
        '                    For Each ke As KeyValuePair(Of String, List(Of String)) In d
        '                        For Each key As String In ke.Value
        '                            RemoveEntry(key, ke.Key)
        '                            rv = True
        '                            'Dim dic As IDictionary = CType(_filters(key), IDictionary)
        '                            'dic.Remove(ke.Key)
        '                        Next
        '                    Next
        '                End If
        '            End Using
        '            Return rv
        '        End Function

        Public Sub validate_AddDeleteType(ByVal ts As IEnumerable(Of Type), ByVal key As String, ByVal id As String)
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug(AddDeleteTypeLock,"d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock(AddDeleteTypeLock)
#End If
                For Each t As Type In ts
                    _addDeleteTypes.Add(t, key, id)
                Next
            End Using
        End Sub

        Public Sub validate_UpdateType(ByVal ts As IEnumerable(Of Type), ByVal key As String, ByVal id As String)
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug(UpdateTypeLock,"d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock(UpdateTypeLock)
#End If
                For Each t As Type In ts
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug(SortGroupLock,"d:\temp\")
#Else
                    Using SyncHelper.AcquireDynamicLock(SortGroupFilterLock)
#End If
                    _updateTypes.Add(t, key, id)
                    _sortedFields.Remove(t, Me)
                    _groupedFields.Remove(t, Me)
                        _filteredFields.Remove(t, Me)
                    End Using
                Next
            End Using
        End Sub

        Protected Friend Function validate_AddCalculatedType(ByVal ts As IEnumerable(Of Type), _
            ByVal key As String, ByVal id As String, ByVal f As IFilter, _
            ByVal schema As ObjectMappingEngine) As Boolean

            Dim r As Boolean

#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug(ImmidiateDynamicLock,"d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock(ImmediateDynamicLock)
#End If
                For Each t As Type In ts
                    Dim tkey As Object = t
                    Dim oschema As IEntitySchema = schema.GetEntitySchema(t, False)
                    If oschema IsNot Nothing Then
                        Dim c As ICacheBehavior = TryCast(oschema, ICacheBehavior)
                        If c IsNot Nothing Then
                            tkey = c.GetEntityKey()
                        End If
                        Dim l As TemplateHashs = _immediateValidate.GetFilters(tkey)
                        r = l.Add(key, f, id)
                    End If
                Next
                'Dim h As List(Of String) = l.GetIds(key, f)
                'If Not h.Contains(id) Then
                '    h.Add(id)
                'End If
            End Using

            Return r
        End Function

        Protected Friend Sub validate_AddDependentObject(ByVal o As ICachedEntity, ByVal key As String, ByVal id As String)
            'Debug.WriteLine(t.Name & ": add dependent " & id)
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("OP*#hnfva","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("OP*#hnfva")
#End If
                AddDepend(o, key, id)
            End Using
        End Sub

        Protected Friend Sub validate_AddDependentFilterField(ByVal p As Pair(Of String, Type), ByVal key As String, ByVal id As String)
            'Debug.WriteLine(t.Name & ": add dependent " & id)
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug(SortGroupLock,"d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock(SortGroupFilterLock)
#End If
                If Not _updateTypes.ContainsKey(p.Second) Then
                    Dim ef As EntityField = New EntityField(p.First, p.Second)

                    _filteredFields.Add(ef, key, id)
                End If
            End Using
        End Sub

        Protected Friend Sub validate_AddDependentSortField(ByVal p As Pair(Of String, Type), ByVal key As String, ByVal id As String)
            'Debug.WriteLine(t.Name & ": add dependent " & id)
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug(SortGroupLock,"d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock(SortGroupFilterLock)
#End If
                If Not _updateTypes.ContainsKey(p.Second) Then
                    Dim ef As EntityField = New EntityField(p.First, p.Second)

                    _sortedFields.Add(ef, key, id)
                End If
            End Using
        End Sub

        Protected Friend Sub validate_AddDependentGroupField(ByVal p As Pair(Of String, Type), ByVal key As String, ByVal id As String)
            'Debug.WriteLine(t.Name & ": add dependent " & id)
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug(SortGroupLock,"d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock(SortGroupFilterLock)
#End If
                If Not _updateTypes.ContainsKey(p.Second) Then
                    Dim ef As EntityField = New EntityField(p.First, p.Second)

                    _groupedFields.Add(ef, key, id)
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
            If t Is Nothing Then
                Throw New ArgumentNullException("t")
            End If
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug(InvalidateLock,"d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock(InvalidateLock)
#End If

                Return _invalidate.TryGetValue(t, l)
            End Using
        End Function

        Protected Friend Sub AddUpdatedFields(ByVal obj As _ICachedEntity, ByVal fields As ICollection(Of String))
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            If fields IsNot Nothing Then
                Dim t As Type = obj.GetType
#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug(InvalidateLock,"d:\temp\")
#Else
                Using SyncHelper.AcquireDynamicLock(InvalidateLock)
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
            End If

            RaiseEvent OnObjectUpdated(Me, obj, fields)
        End Sub

        Protected Friend Sub RemoveUpdatedFields(ByVal t As Type, ByVal field As String)
            If t Is Nothing Then
                Throw New ArgumentNullException("t")
            End If

#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug(InvalidateLock,"d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock(InvalidateLock)
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

        '        Protected Friend Function RemoveM2MQuery(ByVal el As EditableListBase) As Pair(Of String)
        '            If el Is Nothing Then
        '                Throw New ArgumentNullException("el")
        '            End If

        '#If DebugLocks Then
        '            Using SyncHelper.AcquireDynamicLock_Debug("hadfgadfgasdfopgh","d:\temp\")
        '#Else
        '            Using SyncHelper.AcquireDynamicLock("hadfgadfgasdfopgh")
        '#End If
        '                Dim p As Pair(Of String) = _m2mSimpleQueries(el)
        '                _m2mSimpleQueries.Remove(el)
        '                Return p
        '            End Using
        '        End Function

        Protected Friend Sub RemoveM2MQueries(ByVal el As M2MRelation)
            If el Is Nothing Then
                Throw New ArgumentNullException("el")
            End If

#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("hadfgadfgasdfopgh","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("hadfgadfgasdfopgh")
#End If
                Dim ce As CacheEntryRef = Nothing
                If _m2mSimpleQueries.TryGetValue(el, ce) Then
                    ce.Remove(Me)
                    _m2mSimpleQueries.Remove(el)
                End If
            End Using
        End Sub

        Protected Friend Sub AddM2MSimpleQuery(ByVal el As M2MRelation, ByVal key As String, ByVal id As String)
            If el Is Nothing Then
                Throw New ArgumentNullException("el")
            End If

#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug("hadfgadfgasdfopgh","d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock("hadfgadfgasdfopgh")
#End If
                Dim ce As CacheEntryRef = Nothing
                If Not _m2mSimpleQueries.TryGetValue(el, ce) Then
                    ce = New CacheEntryRef
                    _m2mSimpleQueries.Add(el, ce)
                    '_m2mSimpleQueries.Add(el, New Pair(Of String)(key, id))
                End If
                ce.Add(key, id)
            End Using

        End Sub

#If OLDM2M Then
        Protected Friend Sub ConnectedEntityEnum(ByVal mgr As OrmManager, ByVal ct As Type, ByVal f As EnumM2MCache)
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
                        Dim dic As IDictionary = _GetDictionary(p.Key)
                        If dic IsNot Nothing Then
                            'Dim b As Boolean = False
                            Dim remove As New List(Of String)
                            For Each id As String In p.Value.Keys
                                Dim ce As M2MCache = TryCast(dic(id), M2MCache)
                                If ce IsNot Nothing Then
                                    If Not f(mgr, ce) Then
                                        remove.Add(id)
                                    End If
                                End If
                            Next
                            For Each id As String In remove
                                'dic.Remove(id)
                                RemoveEntry(p.Key, id)
                            Next
                            'If Not b Then
                            '    Throw New OrmManagerException(String.Format("Invalid cache entry {0} for type {1}", p.Key, ct))
                            'End If
                        End If
                    Next
                End If
            End Using
        End Sub
#End If
        Protected Friend Function UpdateCacheDeferred(ByVal mpe As ObjectMappingEngine,
            ByVal ts As IEnumerable(Of Type), ByVal f As IEntityFilter, ByVal s As OrderByClause, ByVal g As GroupExpression) As Boolean

            For Each t As Type In ts
                Dim wasAdded, wasDeleted As Boolean

#If DebugLocks Then
                        Using SyncHelper.AcquireDynamicLock_Debug(AddedDeleted,"d:\temp\")
#Else
                Using SyncHelper.AcquireDynamicLock(AddedDeleted)
#End If
                    wasAdded = _addedTypes.ContainsKey(t)
                    wasDeleted = _deletedTypes.ContainsKey(t)

                    If wasAdded Then
                        _addedTypes.Remove(t)
                    End If

                    If wasDeleted Then
                        _deletedTypes.Remove(t)
                    End If
                End Using

                If wasAdded OrElse wasDeleted Then
#If DebugLocks Then
                        Using SyncHelper.AcquireDynamicLock_Debug(AddDeleteTypeLock,"d:\temp\")
#Else
                    Using SyncHelper.AcquireDynamicLock(AddDeleteTypeLock)
#End If
                        If _addDeleteTypes.Remove(t, Me) Then
                            Return False
                        End If
                    End Using
                End If
            Next

            If _invalidate.Count > 0 Then

#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug(InvalidateLock,"d:\temp\")
#Else
                Using SyncHelper.AcquireDynamicLock(InvalidateLock)
#End If
                    For Each t As Type In _invalidate.Keys
                        If ts.Contains(t) Then
#If DebugLocks Then
                                Using SyncHelper.AcquireDynamicLock_Debug(UpdateTypeLock,"d:\temp\")
#Else
                            Using SyncHelper.AcquireDynamicLock(UpdateTypeLock)
#End If
                                If _updateTypes.Remove(t, Me) Then
                                    Return False
                                End If
                            End Using
                        End If
                    Next

                End Using

                If f IsNot Nothing Then
                    For Each fl As IEntityFilter In f.GetAllFilters
                        Dim tmpl As OrmFilterTemplate = CType(fl.Template, OrmFilterTemplate)
                        Dim fields As List(Of String) = Nothing
                        Dim rt As Type = tmpl.ObjectSource.GetRealType(mpe)
                        If GetUpdatedFields(rt, fields) Then
                            If fields.Contains(tmpl.PropertyAlias) Then
#If DebugLocks Then
                                Using SyncHelper.AcquireDynamicLock_Debug(UpdateTypeLock,"d:\temp\")
#Else
                                Using SyncHelper.AcquireDynamicLock(SortGroupFilterLock)
#End If

                                    Dim b As Boolean = _filteredFields.Remove(rt, tmpl.PropertyAlias, Me)
                                    'b = b Or ResetFieldDepends(New Pair(Of String, Type)(tmpl.PropertyAlias, rt))

                                    If b Then
                                        _sortedFields.Remove(rt, tmpl.PropertyAlias, Me)
                                        RemoveUpdatedFields(rt, tmpl.PropertyAlias)
                                        Return False
                                    End If
                                End Using
                            End If
                        End If
                    Next
                End If

                If s IsNot Nothing Then
                    If Not s.CanEvaluate(mpe) Then
                        Return False
                    End If

                    For Each sort As SortExpression In s
                        For Each ee As IEntityPropertyExpression In GetEntityExpressions(sort)
                            Dim t As Type = ee.ObjectProperty.Entity.GetRealType(mpe)
                            Dim fields As List(Of String) = Nothing
                            If GetUpdatedFields(t, fields) Then
                                Dim prop As String = ee.ObjectProperty.PropertyAlias
                                If fields.Contains(prop) Then
#If DebugLocks Then
                                Using SyncHelper.AcquireDynamicLock_Debug(UpdateTypeLock,"d:\temp\")
#Else
                                    Using SyncHelper.AcquireDynamicLock(SortGroupFilterLock)
#End If

                                        If _sortedFields.Remove(t, prop, Me) Then
                                            _filteredFields.Remove(t, prop, Me)
                                            'ResetFieldDepends(New Pair(Of String, Type)(prop, t))
                                            RemoveUpdatedFields(t, prop)
                                            Return False
                                        End If
                                    End Using
                                End If
                            End If
                        Next
                    Next
                End If
            End If

            Return True
        End Function

        Private Sub UpdateCacheImmediate(ByVal tt As Type, ByVal oschema As IEntitySchema, ByVal schema As ObjectMappingEngine,
            ByVal objs As IEnumerable(Of UpdatedEntity), ByVal mgr As OrmManager, ByVal afterDelegate As OnUpdated,
            ByVal contextKey As Object, ByVal callbacks As IUpdateCacheCallbacks2)

            If objs Is Nothing OrElse Not objs.Any Then
                Return
            End If

            If callbacks IsNot Nothing Then
                callbacks.BeginUpdate()
            End If

            Dim tkey As Object = tt
            'Dim oschema As IEntitySchema = mgr.MappingEngine.GetEntitySchema(tt)
            Dim c As ICacheBehavior = TryCast(oschema, ICacheBehavior)
            If c IsNot Nothing Then
                tkey = c.GetEntityKey()
            End If

            Dim oneLoop As Boolean

#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug(ImmidiateDynamicLock,"d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock(ImmediateDynamicLock)
#End If
                Dim hashs As TemplateHashs = _immediateValidate.GetFilters(tkey)

                For Each p As KeyValuePair(Of String, Tuple(Of HashIds, IOrmFilterTemplate)) In hashs
                    Dim dic As IDictionary = GetQueryDictionary(p.Key)

                    For Each op In objs
                        Dim obj As _ICachedEntity = op.CurrentState
                        If obj Is Nothing Then
                            Throw New ArgumentException("At least one element in objs is nothing")
                        End If

                        If tt IsNot obj.GetType Then
                            Throw New ArgumentException("Collection contains different types")
                        End If

                        If dic IsNot Nothing Then
                            UpdateFilters(p, schema, oschema, obj, op.OldState, dic, Nothing, callbacks, True, mgr)
                        ElseIf oneLoop Then
                            Exit For
                        End If

                        If Not oneLoop Then
                            If obj.UpdateCtx.Added OrElse obj.UpdateCtx.Deleted Then
#If DebugLocks Then
                                Using SyncHelper.AcquireDynamicLock_Debug(AddDeleteTypeLock,"d:\temp\")
#Else
                                Using SyncHelper.AcquireDynamicLock(AddDeleteTypeLock)
#End If
                                    _addDeleteTypes.Remove(tt, Me)
                                End Using
                            ElseIf obj.UpdateCtx.UpdatedFields IsNot Nothing Then
                                Dim removed As Boolean
#If DebugLocks Then
                                Using SyncHelper.AcquireDynamicLock_Debug(UpdateTypeLock,"d:\temp\")
#Else
                                Using SyncHelper.AcquireDynamicLock(UpdateTypeLock)
#End If
                                    removed = _updateTypes.Remove(tt, Me)
                                End Using

                                If Not removed Then
                                    For Each f As EntityFilter In obj.UpdateCtx.UpdatedFields
                                        Dim rt As Type = f.Template.ObjectSource.GetRealType(schema)
#If DebugLocks Then
                                Using SyncHelper.AcquireDynamicLock_Debug(UpdateTypeLock,"d:\temp\")
#Else
                                        Using SyncHelper.AcquireDynamicLock(SortGroupFilterLock)
#End If
                                            _filteredFields.Remove(rt, f.Template.PropertyAlias, Me)
                                            _groupedFields.Remove(rt, f.Template.PropertyAlias, Me)
                                            _sortedFields.Remove(rt, f.Template.PropertyAlias, Me)
                                        End Using
                                    Next
                                End If
                            End If
                            oneLoop = True
                        End If

                    Next
                Next
            End Using

            If Not oneLoop Then
                For Each op In objs
                    Dim obj As _ICachedEntity = op.CurrentState
                    If obj Is Nothing Then
                        Throw New ArgumentException("At least one element in objs is nothing")
                    End If

                    If tt IsNot obj.GetType Then
                        Throw New ArgumentException("Collection contains different types")
                    End If
                    If obj.UpdateCtx.Added OrElse obj.UpdateCtx.Deleted Then
#If DebugLocks Then
                                Using SyncHelper.AcquireDynamicLock_Debug(AddDeleteTypeLock,"d:\temp\")
#Else
                        Using SyncHelper.AcquireDynamicLock(AddDeleteTypeLock)
#End If
                            _addDeleteTypes.Remove(tt, Me)
                        End Using
                    ElseIf obj.UpdateCtx.UpdatedFields IsNot Nothing Then
                        Dim removed As Boolean
#If DebugLocks Then
                                Using SyncHelper.AcquireDynamicLock_Debug(UpdateTypeLock,"d:\temp\")
#Else
                        Using SyncHelper.AcquireDynamicLock(UpdateTypeLock)
#End If
                            removed = _updateTypes.Remove(tt, Me)
                        End Using

                        If Not removed Then
                            For Each f As EntityFilter In obj.UpdateCtx.UpdatedFields
                                Dim rt As Type = f.Template.ObjectSource.GetRealType(schema)
#If DebugLocks Then
                                Using SyncHelper.AcquireDynamicLock_Debug(UpdateTypeLock,"d:\temp\")
#Else
                                Using SyncHelper.AcquireDynamicLock(SortGroupFilterLock)
#End If
                                    _filteredFields.Remove(rt, f.Template.PropertyAlias, Me)
                                    _groupedFields.Remove(rt, f.Template.PropertyAlias, Me)
                                    _sortedFields.Remove(rt, f.Template.PropertyAlias, Me)
                                End Using
                            Next
                        End If
                    End If
                Next
            End If

            If callbacks IsNot Nothing Then
                callbacks.EndUpdate()
            End If

        End Sub
        Public Sub UpdateCacheByType(ByVal tt As Type, ByVal oschema As IEntitySchema, ByVal mpe As ObjectMappingEngine,
            ByVal callbacks As IUpdateCacheCallbacks2)

            If callbacks IsNot Nothing Then
                callbacks.BeginUpdate()
            End If

            Dim tkey As Object = tt
            'Dim oschema As IEntitySchema = mgr.MappingEngine.GetEntitySchema(tt)
            Dim c As ICacheBehavior = TryCast(oschema, ICacheBehavior)
            If c IsNot Nothing Then
                tkey = c.GetEntityKey()
            End If

#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug(ImmidiateDynamicLock,"d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock(ImmediateDynamicLock)
#End If
                Dim hashs As TemplateHashs = _immediateValidate.GetFilters(tkey)

                For Each p As KeyValuePair(Of String, Tuple(Of HashIds, IOrmFilterTemplate)) In hashs
                    Dim dic As IDictionary = GetQueryDictionary(p.Key)

                    If dic IsNot Nothing Then
                        dic.Clear()
                    End If
                Next
            End Using

            Dim removed As Boolean
#If DebugLocks Then
                                Using SyncHelper.AcquireDynamicLock_Debug(AddDeleteTypeLock,"d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock(AddDeleteTypeLock)
#End If
                removed = _addDeleteTypes.Remove(tt, Me)
            End Using

#If DebugLocks Then
                                Using SyncHelper.AcquireDynamicLock_Debug(UpdateTypeLock,"d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock(UpdateTypeLock)
#End If
                removed = _updateTypes.Remove(tt, Me) OrElse removed
            End Using

#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug(SortGroupLock,"d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock(SortGroupFilterLock)
#End If
                _sortedFields.Remove(tt, Me)
                _groupedFields.Remove(tt, Me)
                _filteredFields.Remove(tt, Me)
            End Using

            If callbacks IsNot Nothing Then
                callbacks.EndUpdate()
            End If

        End Sub
        Protected Friend Sub UpdateCache(ByVal mpe As ObjectMappingEngine,
                                         ByVal objs As IEnumerable(Of UpdatedEntity), ByVal mgr As OrmManager, ByVal afterDelegate As OnUpdated,
                                         ByVal contextKey As Object, ByVal callbacks As IUpdateCacheCallbacks,
                                         ByVal forseEval As Boolean, ByVal fromUpdate As Boolean, Optional processedTypes As List(Of Type) = Nothing)

            If objs Is Nothing OrElse Not objs.Any Then
                Return
            End If

            Dim tt As Type = Nothing
            Dim addType As Type = Nothing
            Dim delType As Type = Nothing
            Dim oschema As IEntitySchema = Nothing

            Dim relatedObjs As New List(Of UpdatedEntity)
            Dim entTypes = (From k In GetEntityTypes()
                            Where processedTypes Is Nothing OrElse Not processedTypes.Contains(k)
                        ).ToArray
            For Each p In objs
                Dim obj As _ICachedEntity = p.CurrentState
                If obj Is Nothing Then
                    Throw New ArgumentException("At least one element in objs is nothing")
                End If
                tt = obj.GetType

                If oschema Is Nothing Then
                    oschema = obj.GetEntitySchema(mpe)
                End If

                If processedTypes Is Nothing Then
                    processedTypes = New List(Of Type)
                End If
                If Not processedTypes.Contains(tt) Then
                    processedTypes.Add(tt)
                End If

                For Each rt In GetRelatedTypes(tt, entTypes)
                    Dim dic = GetOrmDictionary(rt, mpe)
                    'Dim pkw = New CacheKey(obj, oschema)
                    Dim ro = TryCast(dic(obj), _ICachedEntity)
                    If ro IsNot Nothing Then
                        If Not relatedObjs.Any(Function(it) it.CurrentState.Equals(ro)) Then
                            relatedObjs.Add(New UpdatedEntity(ro, Nothing))
                            dic.Remove(obj)
                        End If
                    Else
                        Dim osc = mpe.GetEntitySchema(rt)
                        Dim tkey As Object = rt
                        Dim c As ICacheBehavior = TryCast(osc, ICacheBehavior)
                        If c IsNot Nothing Then
                            tkey = c.GetEntityKey()
                        End If
                        Dim hashs As TemplateHashs = _immediateValidate.GetFilters(tkey)
                        If hashs IsNot Nothing Then

                            For Each h In hashs
                                Dim ldic = GetQueryDictionary(h.Key)
                                If ldic IsNot Nothing Then
                                    For Each de As DictionaryEntry In New Hashtable(ldic)

                                        Dim ce As CachedItemBase = TryCast(de.Value, CachedItemBase)
                                        ldic.Remove(de.Key)
                                        If ce IsNot Nothing Then
                                            RegisterRemovalCacheItem(ce)
                                        End If
                                    Next
                                    RemoveIfEmpty(ldic, h.Key)
                                End If
                            Next
                        End If
                    End If
                Next

                If obj.UpdateCtx.Added Then
                    addType = tt
                ElseIf obj.UpdateCtx.Deleted Then
                    delType = tt
                End If

                If ValidateBehavior = Cache.ValidateBehavior.Immediate _
                    OrElse (addType IsNot Nothing AndAlso delType IsNot Nothing) _
                    OrElse fromUpdate Then
                    Exit For
                End If

            Next

#If DebugLocks Then
            Using SyncHelper.AcquireDynamicLock_Debug(AddedDeleted,"d:\temp\")
#Else
            Using SyncHelper.AcquireDynamicLock(AddedDeleted)
#End If
                If addType IsNot Nothing Then
                    _addedTypes(addType) = addType
                End If

                If delType IsNot Nothing Then
                    _deletedTypes(delType) = delType
                End If

            End Using

            If ValidateBehavior = Cache.ValidateBehavior.Immediate Then
                UpdateCacheImmediate(tt, oschema, mpe, objs, mgr, afterDelegate, contextKey, TryCast(callbacks, IUpdateCacheCallbacks2))
            End If

            If Not fromUpdate Then
                'Dim oschema As IEntitySchema = schema.GetEntitySchema(tt)
                Dim c As ICacheBehavior = TryCast(oschema, ICacheBehavior)
                Dim k As Object = tt
                If c IsNot Nothing Then
                    k = c.GetEntityTypeKey()
                End If

                '#If DebugLocks Then
                '            Using SyncHelper.AcquireDynamicLock_Debug("1340f89njqodfgn1","d:\temp\")
                '#Else
                '                Using SyncHelper.AcquireDynamicLock("1340f89njqodfgn1")
                '#End If
                '                    Dim l As Dictionary(Of String, Pair(Of String)) = Nothing
                '                    If _qt.TryGetValue(k, l) Then
                '                        Dim rm As New List(Of String)
                '                        For Each kv As KeyValuePair(Of String, Pair(Of String)) In l
                '                            Dim dic As IDictionary = _GetDictionary(kv.Value.First)
                '                            If dic IsNot Nothing Then
                '                                'dic.Remove(kv.Value.Second)
                '                                RemoveEntry(kv.Value)
                '                                rm.Add(kv.Key)
                '                            End If
                '                        Next
                '                        If rm.Count > 0 Then
                '                            For Each s As String In rm
                '                                l.Remove(s)
                '                            Next
                '                            If l.Count = 0 Then
                '                                _qt.Remove(k)
                '                            End If
                '                            GoTo l1
                '                        End If
                '                    End If
                '                End Using

                '#If DebugLocks Then
                '            Using SyncHelper.AcquireDynamicLock_Debug("j13rvnopqefv9-n24bth","d:\temp\")
                '#Else
                '                Using SyncHelper.AcquireDynamicLock("j13rvnopqefv9-n24bth")
                '#End If
                '                    Dim l As TemplateHashs = _tp.GetFilters(k)
                '                    For Each p As KeyValuePair(Of String, Pair(Of HashIds, IOrmFilterTemplate)) In l
                '                        Dim dic As IDictionary = _GetDictionary(p.Key)
                '                        If dic IsNot Nothing Then
                '                            If callbacks IsNot Nothing Then
                '                                callbacks.BeginUpdate(0)
                '                            End If
                '                            For Each op As Pair(Of _ICachedEntity) In objs
                '                                Dim obj As _ICachedEntity = op.First
                '                                If obj Is Nothing Then
                '                                    Throw New ArgumentException("At least one element in objs is nothing")
                '                                End If

                '                                If tt IsNot obj.GetType Then
                '                                    Throw New ArgumentException("Collection contains different types")
                '                                End If

                '                                If obj.UpdateCtx.Added OrElse obj.UpdateCtx.Deleted Then
                '                                    UpdateFilters(p, schema, oschema, obj, op.Second, dic, callbacks, Nothing, forseEval, mgr)
                '                                End If
                '                            Next
                '                            If callbacks IsNot Nothing Then
                '                                callbacks.EndUpdate()
                '                            End If
                '                            '_filters.Remove(p.Key)
                '                        End If
                '                    Next
                '                End Using
l1:
                ValidateExternal(objs, mgr, callbacks, afterDelegate, contextKey)

                'UpdateJoins(tt, objs, schema, oschema, mgr, contextKey, afterDelegate, callbacks)
            End If

            UpdateCache(mpe, relatedObjs, mgr, afterDelegate, contextKey, callbacks, forseEval, fromUpdate, processedTypes)
        End Sub

        Private Iterator Function GetRelatedTypes(tt As Type, rels As IEnumerable(Of Type)) As IEnumerable(Of Type)
            For Each bt In GetBaseTypes(tt)
                If Not {GetType(Object), GetType(Entity), GetType(CachedEntity), GetType(AnonymousCachedEntity), GetType(AnonymousEntity),
                    GetType(SinglePKEntityBase), GetType(SinglePKEntity), GetType(CachedLazyLoad)}.Contains(bt) AndAlso rels.Contains(bt) Then
                    Yield bt
                End If
            Next
            For Each bt In rels
                If bt.IsSubclassOf(tt) Then
                    Yield bt
                End If
            Next
        End Function
        Private Iterator Function GetBaseTypes(tt As Type) As IEnumerable(Of Type)
            If tt.BaseType IsNot Nothing Then
                Yield tt.BaseType
            End If
        End Function
        Private Sub UpdateFilters(ByVal p As KeyValuePair(Of String, Tuple(Of HashIds, IOrmFilterTemplate)),
                                  ByVal schema As ObjectMappingEngine, ByVal oschema As IEntitySchema,
                                  ByVal obj As _ICachedEntity, ByVal oldObj As _ICachedEntity, ByVal dic As IDictionary,
                                  ByVal callbacks As IUpdateCacheCallbacks, ByVal callbacks2 As IUpdateCacheCallbacks2,
                                  ByVal forseEval As Boolean, ByVal mgr As OrmManager)
            Dim hash As String = EntityFilter.EmptyHash
            If p.Value.Item2 IsNot Nothing Then
                Try
                    hash = p.Value.Item2.MakeHash(schema, oschema, obj)
                Catch ex As ArgumentException When ex.Message.StartsWith("Template type")
                    Return
                Catch ex As InvalidOperationException When ex.Message.StartsWith("Type is not specified in filter")
                    Throw New InvalidOperationException(String.Format("Key {0}", p.Key), ex)
                End Try
            End If
            Dim hid As HashIds = p.Value.Item1
            Dim ids As IEnumerable(Of String) = hid.GetIds(hash)
            Dim rm As New List(Of String)
            For Each id As String In ids
                Dim ce As UpdatableCachedItem = TryCast(dic(id), UpdatableCachedItem)
                Dim f As IEntityFilter = Nothing
                If ce IsNot Nothing Then
                    f = TryCast(ce.Filter, IEntityFilter)
                End If
                If ce IsNot Nothing AndAlso f IsNot Nothing Then
                    If callbacks IsNot Nothing Then
                        callbacks.BeginUpdateList(p.Key, id)
                    End If

                    If callbacks2 IsNot Nothing Then
                        callbacks2.BeginUpdateList(p.Key, id)
                    End If

                    If obj.UpdateCtx.Deleted OrElse obj.UpdateCtx.Added OrElse forseEval Then
                        Dim er As IEvaluableValue.EvalResult = IEvaluableValue.EvalResult.Found
                        If f IsNot Nothing Then
                            er = f.EvalObj(schema, obj, oschema, ce.Joins, ce.QueryEU)
                        End If

                        Select Case CInt(er)
                            Case IEvaluableValue.EvalResult.Unknown
                                RemoveEntry(p.Key, id)
                            Case IEvaluableValue.EvalResult.Found
                                Dim sync As String = id & mgr.GetStaticKey
                                Using SyncHelper.AcquireDynamicLock(sync)
                                    If obj.UpdateCtx.Added Then
                                        If Not ce.Add(mgr, obj) Then
                                            RemoveEntry(p.Key, id)
                                            'dic.Remove(id)
                                        End If
                                    ElseIf obj.UpdateCtx.Deleted Then
                                        ce.Delete(mgr, obj)
                                    ElseIf obj.UpdateCtx.UpdatedFields IsNot Nothing Then
                                        If Not ce.Add(mgr, obj) Then
                                            RemoveEntry(p.Key, id)
                                        End If
                                        'Else
                                        '    Throw New InvalidOperationException
                                    End If
                                End Using

                                If callbacks IsNot Nothing Then
                                    callbacks.ObjectDependsUpdated(obj)
                                End If

                                If callbacks2 IsNot Nothing Then
                                    callbacks2.ObjectDependsUpdated(obj)
                                End If
                        End Select
                    End If
                Else
                    rm.Add(id)
                End If

                If callbacks IsNot Nothing Then
                    callbacks.EndUpdateList(p.Key, id)
                End If

                If callbacks2 IsNot Nothing Then
                    callbacks2.EndUpdateList(p.Key, id)
                End If
            Next

            If obj.UpdateCtx.UpdatedFields IsNot Nothing AndAlso oldObj IsNot Nothing Then
                hash = EntityFilter.EmptyHash
                If p.Value.Item2 IsNot Nothing Then
                    Try
                        hash = p.Value.Item2.MakeHash(schema, oschema, oldObj)
                    Catch ex As ArgumentException When ex.Message.StartsWith("Template type")
                        Return
                    End Try
                End If
                hid = p.Value.Item1
                ids = hid.GetIds(hash)

                For Each id As String In ids
                    Dim ce As UpdatableCachedItem = TryCast(dic(id), UpdatableCachedItem)
                    Dim f As IEntityFilter = Nothing
                    If ce IsNot Nothing Then
                        f = TryCast(ce.Filter, IEntityFilter)
                    End If
                    If ce IsNot Nothing AndAlso f IsNot Nothing Then
                        If callbacks IsNot Nothing Then
                            callbacks.BeginUpdateList(p.Key, id)
                        End If

                        If callbacks2 IsNot Nothing Then
                            callbacks2.BeginUpdateList(p.Key, id)
                        End If

                        Dim r As Boolean = False
                        Dim er As IEvaluableValue.EvalResult = IEvaluableValue.EvalResult.Found
                        If f IsNot Nothing Then
                            er = f.EvalObj(schema, oldObj, oschema, ce.Joins, ce.QueryEU)
                            r = er = IEvaluableValue.EvalResult.Unknown
                        End If

                        If r Then
                            RemoveEntry(p.Key, id)
                        Else
                            ce.Delete(mgr, oldObj)
                        End If

                        If callbacks IsNot Nothing Then
                            callbacks.ObjectDependsUpdated(obj)
                        End If

                        If callbacks2 IsNot Nothing Then
                            callbacks2.ObjectDependsUpdated(obj)
                        End If
                    Else
                        rm.Add(id)
                    End If
                Next
            End If

            For Each id As String In rm
                hid.Remove(hash, id)
                'ids.Remove(id)
                RemoveEntry(p.Key, id)
                'dic.Remove(id)
            Next
        End Sub

        '        Private Sub UpdateJoins(ByVal tt As Type, ByVal objs As IList, ByVal schema As ObjectMappingEngine, _
        '                                ByVal oschema As IEntitySchema, ByVal mgr As OrmManager, ByVal contextKey As Object, _
        '                                ByVal afterDelegate As OnUpdated, ByVal callbacks As IUpdateCacheCallbacks)
        '#If DebugLocks Then
        '            Using SyncHelper.AcquireDynamicLock_Debug("9-134g9ngpadfbgp","d:\temp\")
        '#Else
        '            Using SyncHelper.AcquireDynamicLock("9-134g9ngpadfbgp")
        '#End If
        '                Dim ts As List(Of Type) = Nothing
        '                If _jt.TryGetValue(tt, ts) Then
        '                    For Each t As Type In ts
        '                        For Each p As Pair(Of _ICachedEntity) In objs
        '                            Dim obj As _ICachedEntity = p.First

        '                            If obj Is Nothing Then
        '                                Throw New ArgumentException("At least one element in objs is nothing")
        '                            End If

        '                            If tt IsNot obj.GetType Then
        '                                Throw New ArgumentException("Collection contains different types")
        '                            End If

        '                            Dim o As _IEntity = schema.GetJoinObj(oschema, obj, t)

        '                            If o IsNot Nothing Then
        '                                UpdateCache(schema, New Pair(Of _ICachedEntity)() {New Pair(Of _ICachedEntity)(CType(o, _ICachedEntity), Nothing)}, mgr, afterDelegate, contextKey, callbacks, True, False)
        '                            End If
        '                        Next
        '                    Next
        '                End If
        '            End Using
        '        End Sub

        Public Sub ValidateExternal(ByVal objs As IEnumerable(Of UpdatedEntity),
            ByVal mgr As OrmManager, ByVal callbacks As IUpdateCacheCallbacks,
            ByVal afterDelegate As OnUpdated, ByVal contextKey As Object)
            If callbacks IsNot Nothing Then
                callbacks.BeginUpdateProcs()
            End If
            For Each p In objs
                Dim obj As _ICachedEntity = p.CurrentState

                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                If obj.UpdateCtx.Added Then
                    RaiseEvent OnObjectAdded(Me, obj)
                    'ValidateSPOnInsertDelete(obj)
                ElseIf obj.UpdateCtx.Deleted Then
                    RaiseEvent OnObjectDeleted(Me, obj)
                    'ValidateSPOnInsertDelete(obj)
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

        End Sub

        '        ''' <summary>
        '        ''' ����������� ����������� ���� �� ����� � ����
        '        ''' </summary>
        '        ''' <param name="t"></param>
        '        ''' <param name="key"></param>
        '        ''' <param name="id"></param>
        '        ''' <remarks></remarks>
        '        Protected Friend Sub AddDependType(ByVal filterInfo As Object, ByVal t As Type, ByVal key As String, ByVal id As String, ByVal f As IFilter, ByVal schema As ObjectMappingEngine)
        '            'Debug.WriteLine(t.Name & ": add dependent " & id)
        '#If DebugLocks Then
        '            Using SyncHelper.AcquireDynamicLock_Debug("j13rvnopqefv9-n24bth","d:\temp\")
        '#Else
        '            Using SyncHelper.AcquireDynamicLock("j13rvnopqefv9-n24bth")
        '#End If
        '                Dim l As TemplateHashs = _tp.GetFilters(schema.GetEntityTypeKey(filterInfo, t))
        '                l.Add(f, key, id)
        '                'Dim h As List(Of String) = l.GetIds(key, f)
        '                'If Not h.Contains(id) Then
        '                '    h.Add(id)
        '                'End If
        '            End Using
        '        End Sub

        '        Protected Friend Sub AddFilterlessDependType(ByVal filterInfo As Object, ByVal t As Type, ByVal key As String, ByVal id As String, _
        '            ByVal schema As ObjectMappingEngine)
        '            Dim l As Dictionary(Of String, Pair(Of String)) = Nothing
        '            Dim o As Object = schema.GetEntityTypeKey(filterInfo, t)
        '#If DebugLocks Then
        '            Using SyncHelper.AcquireDynamicLock_Debug("1340f89njqodfgn1","d:\temp\")
        '#Else
        '            Using SyncHelper.AcquireDynamicLock("1340f89njqodfgn1")
        '#End If
        '                If Not _qt.TryGetValue(o, l) Then
        '                    l = New Dictionary(Of String, Pair(Of String))
        '                    _qt.Add(o, l)
        '                End If
        '                Dim k As String = key & "-" & id
        '                If Not l.ContainsKey(k) Then
        '                    l.Add(k, New Pair(Of String)(key, id))
        '                End If
        '            End Using
        '        End Sub

        '        Protected Friend Sub AddJoinDepend(ByVal joinType As Type, ByVal selectType As Type)
        '            If joinType Is Nothing Then
        '                Throw New ArgumentNullException("joinType")
        '            End If

        '            If selectType Is Nothing Then
        '                Throw New ArgumentNullException("selectType")
        '            End If

        '#If DebugLocks Then
        '            Using SyncHelper.AcquireDynamicLock_Debug("9-134g9ngpadfbgp","d:\temp\")
        '#Else
        '            Using SyncHelper.AcquireDynamicLock("9-134g9ngpadfbgp")
        '#End If
        '                Dim l As List(Of Type) = Nothing
        '                If Not _jt.TryGetValue(joinType, l) Then
        '                    l = New List(Of Type)
        '                    _jt(joinType) = l
        '                End If
        '                If Not l.Contains(selectType) Then
        '                    l.Add(selectType)
        '                End If
        '            End Using
        '        End Sub

        ''' <summary>
        ''' ����������� ���������� ������� �� ����� (������ ������������ � �������)
        ''' </summary>
        ''' <param name="obj"></param>
        ''' <param name="key"></param>
        ''' <param name="id"></param>
        ''' <remarks></remarks>
        Protected Friend Sub AddDepend(ByVal obj As ICachedEntity, ByVal key As String, ByVal id As String)
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

        Public Overrides Sub RegisterRemoval(ByVal obj As Entities._ICachedEntity, ByVal mpe As ObjectMappingEngine, ByVal cb As ICacheBehavior)
            MyBase.RegisterRemoval(obj, mpe, cb)
            RemoveDepends(obj)
        End Sub
        ''' <summary>
        ''' ������� ��� ����� � ����, ������� ������� �� ������� �������
        ''' </summary>
        ''' <param name="obj"></param>
        ''' <remarks></remarks>
        Protected Friend Sub RemoveDepends(ByVal obj As ICachedEntity)
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
                            Dim dic As IDictionary = GetQueryDictionary(p.Key)
                            If dic IsNot Nothing Then
                                For Each id As String In p.Value
                                    'dic.Remove(id)
                                    RemoveEntry(p.Key, id)
                                Next
                                '_filters.Remove(p.Key)
                            End If
                        Next
                        _object_depends.Remove(op)
                    End If
                End Using
            End If
        End Sub

        Public Overrides Sub Reset()
            '_filters = Hashtable.Synchronized(New Hashtable)
            MyBase.Reset()
        End Sub

        Public Overrides Function CreateResultsetsDictionary(ByVal mark As String) As System.Collections.IDictionary
            If _md IsNot Nothing AndAlso Not String.IsNullOrEmpty(mark) Then
                Return _md(mark)
            Else
                Return MyBase.CreateResultsetsDictionary(mark)
            End If
        End Function

        'Public Overrides ReadOnly Property OrmDictionaryT(of T)() As System.Collections.Generic.IDictionary(Of Integer, T)

        Public Sub New()
            MyBase.New()
            Reset()
        End Sub

        Public Sub New(ByVal cacheListDelegate As CreateCacheListDelegate)
            MyBase.New()
            Reset()
            _md = cacheListDelegate
        End Sub

    End Class

    Public Class WebCache
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

        Public Overrides Function CreateResultsetsDictionary(ByVal mark As String) As System.Collections.IDictionary
            Return MyBase.CreateResultsetsDictionary(mark)
        End Function

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

        Protected Overridable Function GetPolicy(ByVal t As Type) As WebCacheDictionaryPolicy
            Return WebCacheDictionaryPolicy.CreateDefault
        End Function

        Protected Overrides Function CreateListConverter() As IListObjectConverter
            Return New ListConverter
        End Function

        Public Sub New()
            MyBase.New()
        End Sub
    End Class
End Namespace
