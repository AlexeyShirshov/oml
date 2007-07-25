Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports CoreFramework.Structures
Imports CoreFramework.Threading

Namespace Orm

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

    Public Class OrmObjectIndex
        Inherits Collections.IndexedCollection(Of String, MapField2Column)

        Protected Overrides Function GetKeyForItem(ByVal item As MapField2Column) As String
            Return item._fieldName
        End Function
    End Class

    Public MustInherit Class OrmCacheBase

        Public ReadOnly DateTimeCreated As Date
        Private _filters As IDictionary
        Private _modifiedobjects As IDictionary
        'Private _invalidate_types As New List(Of Type)
        ''' <summary>
        ''' dictionary.key - тип
        ''' dictionary.value - массив изменяемых полей
        ''' </summary>
        ''' <remarks></remarks>
        Private _invalidate As New Dictionary(Of Type, List(Of String))
        'Private _relations As New Dictionary(Of Type, List(Of Type))
        Private _depends As New Dictionary(Of OrmBase, Dictionary(Of String, List(Of String)))
        Private _procs As New List(Of StoredProcBase)
        ''' <summary>
        ''' pair.first - поле
        ''' pair.second - тип
        ''' зависит от dictionary.key - ключ в кеше и dictionary.value - id в кеше
        ''' </summary>
        ''' <remarks></remarks>
        Private _field_depends As New Dictionary(Of Pair(Of String, Type), Dictionary(Of String, List(Of String)))

        ''' <summary>
        ''' type - тип
        ''' зависит от dictionary.key - ключ в кеше и dictionary.value - id в кеше
        ''' </summary>
        ''' <remarks></remarks>
        Private _type_depends As New Dictionary(Of Type, Dictionary(Of String, Dictionary(Of String, Object)))

        Private _ct_depends As New Dictionary(Of Type, Dictionary(Of String, Dictionary(Of String, Object)))

        Private _m2m_dep As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, Object)))

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

        Protected Friend Function Modified(ByVal obj As OrmBase) As ModifiedObject
            SyncLock SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                Dim name As String = obj.GetType().Name & ":" & obj.Identifier
                Return CType(_modifiedobjects(name), ModifiedObject)
            End SyncLock
        End Function

        Public ReadOnly Property SyncRoot() As Object
            Get
                Return Me
            End Get
        End Property

        Protected Friend Function RegisterModification(ByVal obj As OrmBase) As ModifiedObject
            SyncLock SyncRoot
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
            End SyncLock
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
            SyncLock SyncRoot
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
            End SyncLock
        End Function

        Protected Friend Sub RegisterExistingModification(ByVal obj As OrmBase, ByVal id As Integer)
            SyncLock SyncRoot
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                Dim name As String = obj.GetType().Name & ":" & id
                _modifiedobjects.Add(name, obj.GetModifiedObject)
                If _modifiedobjects.Count = 1 Then
                    RaiseEvent CacheHasModification(Me, EventArgs.Empty)
                End If
            End SyncLock
        End Sub

        Protected Friend Sub UnregisterModification(ByVal obj As OrmBase)
            SyncLock SyncRoot
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
            End SyncLock
        End Sub

        Public ReadOnly Property IsModified() As Boolean
            Get
                SyncLock SyncRoot
                    Return _modifiedobjects.Count <> 0
                End SyncLock
            End Get
        End Property

#End Region

        Public MustOverride Function GetFiltersDic() As IDictionary

        Public MustOverride Function GetOrmDictionary(ByVal t As Type, Optional ByVal schema As OrmSchemaBase = Nothing) As System.Collections.IDictionary

        Public MustOverride Function GetOrmDictionary(Of T)(Optional ByVal schema As OrmSchemaBase = Nothing) As System.Collections.Generic.IDictionary(Of Integer, T)

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

        Protected Friend Sub AddFieldDepend(ByVal p As Pair(Of String, Type), ByVal key As String, ByVal id As String)
            Using SyncHelper.AcquireDynamicLock("9nhervg-jrgfl;jg94gt")
                Dim d As Dictionary(Of String, List(Of String)) = Nothing
                If Not _field_depends.TryGetValue(p, d) Then
                    d = New Dictionary(Of String, List(Of String))
                    _field_depends.Add(p, d)
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
            Using SyncHelper.AcquireDynamicLock("9nhervg-jrgfl;jg94gt")
                Dim d As Dictionary(Of String, List(Of String)) = Nothing
                If _field_depends.TryGetValue(p, d) Then
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
            Using SyncHelper.AcquireDynamicLock("913bh5g9nh04nvgtr0924ng")
                Return _invalidate.TryGetValue(t, l)
            End Using
        End Function

        Protected Friend Sub AddUpdatedFields(ByVal obj As OrmBase, ByVal fields As ICollection(Of String))
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim t As Type = obj.GetType
            Using SyncHelper.AcquireDynamicLock("913bh5g9nh04nvgtr0924ng")
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
            Using SyncHelper.AcquireDynamicLock("913bh5g9nh04nvgtr0924ng")
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

            Using SyncHelper.AcquireDynamicLock("8907h13fkonhasdgft7")
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

            Using SyncHelper.AcquireDynamicLock("bhiasdbvgklbg135t")
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

        Protected Friend Function GetM2MEtries(ByVal obj As OrmBase, ByVal name As String) As ICollection(Of Pair(Of OrmManagerBase.M2MCache, Pair(Of String, String)))
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            If String.IsNullOrEmpty(name) Then
                name = obj.GetName
            End If

            Using SyncHelper.AcquireDynamicLock("bhiasdbvgklbg135t")
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

        Protected Friend Sub UpdateM2MEtries(ByVal obj As OrmBase, ByVal oldId As Integer, ByVal name As String)
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            If String.IsNullOrEmpty(name) Then
                Throw New ArgumentNullException("name")
            End If

            Using SyncHelper.AcquireDynamicLock("bhiasdbvgklbg135t")
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

            Using SyncHelper.AcquireDynamicLock("8907h13fkonhasdgft7")
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

        'Protected Friend Sub UpdateCacheOnAdd(ByVal schema As OrmSchemaBase, _
        '    ByVal objs As IList, ByVal mgr As OrmManagerBase, ByVal afterDelegate As OnUpdateAfterAddEnd, ByVal contextKey As Object)

        '    Using SyncHelper.AcquireDynamicLock("j13rvnopqefv9-n24bth")
        '        Dim l As Dictionary(Of String, Dictionary(Of String, Object)) = Nothing
        '        Dim tt As Type = Nothing
        '        For Each obj As OrmBase In objs
        '            If obj Is Nothing Then
        '                Throw New ArgumentNullException("obj")
        '            End If
        '            tt = obj.GetType
        '        Next

        '        Dim oschema As IOrmObjectSchemaBase = schema.GetObjectSchema(tt)
        '        If _type_depends.TryGetValue(tt, l) Then
        '            For Each p As KeyValuePair(Of String, Dictionary(Of String, Object)) In l
        '                Dim dic As IDictionary = CType(_filters(p.Key), IDictionary)
        '                Dim m2m As Boolean = p.Key.Contains(OrmManagerBase.Const_JoinStaticString)
        '                If dic IsNot Nothing Then
        '                    For Each id As String In p.Value.Keys
        '                        Dim ce As OrmManagerBase.CachedItem = TryCast(dic(id), OrmManagerBase.CachedItem)
        '                        Dim f As IOrmFilter = Nothing
        '                        If ce IsNot Nothing Then
        '                            For Each obj As OrmBase In objs
        '                                If obj Is Nothing Then
        '                                    Throw New ArgumentNullException("obj")
        '                                End If

        '                                If tt IsNot obj.GetType Then
        '                                    Throw New ArgumentException("Collection contains different types")
        '                                End If

        '                                f = ce.Filter
        '                                Dim r As Boolean = False
        '                                Dim er As IOrmFilter.EvalResult = IOrmFilter.EvalResult.Found
        '                                If m2m Then
        '                                    'Dim oo As IRelation = TryCast(schema.GetObjectSchema(t), IRelation)
        '                                    'If oo IsNot Nothing Then
        '                                    '    Dim p1 As Pair(Of String, Type) = oo.GetFirstType
        '                                    '    Dim p2 As Pair(Of String, Type) = oo.GetSecondType
        '                                    '    Dim o1 As OrmBase = CType(schema.GetFieldValue(obj, p1.First), OrmBase)
        '                                    '    Dim o2 As OrmBase = CType(schema.GetFieldValue(obj, p2.First), OrmBase)
        '                                    '    Dim el As EditableList = CType(ce, OrmManagerBase.M2MCache).Entry
        '                                    '    If el.MainId <> o1.Identifier Then
        '                                    '        Throw New OrmManagerException("Invalid cache entry")
        '                                    '    End If
        '                                    '    el.Add(o2.Identifier)
        '                                    'Else
        '                                    '    r = True
        '                                    'End If
        '                                    Continue For
        '                                ElseIf f IsNot Nothing Then
        '                                    er = f.Eval(schema, obj, oschema)
        '                                    r = er = IOrmFilter.EvalResult.Unknown
        '                                End If

        '                                If r Then
        '                                    dic.Remove(id)
        '                                ElseIf er = IOrmFilter.EvalResult.Found Then
        '                                    Dim sync As String = id & mgr.GetStaticKey
        '                                    Using SyncHelper.AcquireDynamicLock(sync)
        '                                        If Not ce.Add(mgr, obj) Then
        '                                            dic.Remove(id)
        '                                        End If
        '                                    End Using
        '                                End If
        '                            Next
        '                        Else
        '                            dic.Remove(id)
        '                        End If
        '                        'Debug.WriteLine(t.Name & ": remove dependent " & id)
        '                    Next
        '                    '_filters.Remove(p.Key)
        '                End If
        '            Next
        '        End If
        '    End Using
        '    For Each obj As OrmBase In objs
        '        If obj Is Nothing Then
        '            Throw New ArgumentNullException("obj")
        '        End If
        '        ValidateSPOnInsertDelete(obj)

        '        If afterDelegate IsNot Nothing Then
        '            afterDelegate(obj, mgr, contextKey)
        '        End If
        '    Next
        'End Sub

        'Protected Friend Sub UpdateCacheOnDelete(ByVal schema As OrmSchemaBase, ByVal objs As IList, _
        '    ByVal mgr As OrmManagerBase, ByVal afterDelegate As OnUpdateAfterDeleteEnd)

        '    Using SyncHelper.AcquireDynamicLock("j13rvnopqefv9-n24bth")
        '        Dim l As Dictionary(Of String, Dictionary(Of String, Object)) = Nothing
        '        Dim t As Type = Nothing
        '        For Each obj As OrmBase In objs
        '            If obj Is Nothing Then
        '                Throw New ArgumentNullException("obj")
        '            End If
        '            t = obj.GetType
        '        Next
        '        Dim oschema As IOrmObjectSchemaBase = schema.GetObjectSchema(t)
        '        'Debug.WriteLine(t.Name & ": register add or delete")
        '        If _type_depends.TryGetValue(t, l) Then
        '            For Each p As KeyValuePair(Of String, Dictionary(Of String, Object)) In l
        '                Dim dic As IDictionary = CType(_filters(p.Key), IDictionary)
        '                Dim m2m As Boolean = p.Key.Contains(OrmManagerBase.Const_JoinStaticString)
        '                If dic IsNot Nothing Then
        '                    For Each id As String In p.Value.Keys
        '                        Dim ce As OrmManagerBase.CachedItem = TryCast(dic(id), OrmManagerBase.CachedItem)
        '                        Dim f As IOrmFilter = Nothing
        '                        If ce IsNot Nothing Then
        '                            For Each obj As OrmBase In objs
        '                                If obj Is Nothing Then
        '                                    Throw New ArgumentNullException("obj")
        '                                End If

        '                                If t IsNot obj.GetType Then
        '                                    Throw New ArgumentException("Collection contains different types")
        '                                End If

        '                                f = ce.Filter
        '                                Dim r As Boolean = False
        '                                If m2m Then
        '                                    r = True
        '                                ElseIf f IsNot Nothing Then
        '                                    r = f.Eval(schema, obj, oschema) = IOrmFilter.EvalResult.Unknown
        '                                End If

        '                                If r Then
        '                                    dic.Remove(id)
        '                                Else
        '                                    Dim sync As String = id & mgr.GetStaticKey
        '                                    Using SyncHelper.AcquireDynamicLock(sync)
        '                                        ce.Delete(mgr, obj)
        '                                    End Using
        '                                End If
        '                            Next
        '                        Else
        '                            dic.Remove(id)
        '                        End If
        '                    Next
        '                    '_filters.Remove(p.Key)
        '                End If
        '            Next
        '        End If
        '    End Using

        '    For Each obj As OrmBase In objs
        '        If obj Is Nothing Then
        '            Throw New ArgumentNullException("obj")
        '        End If
        '        ValidateSPOnInsertDelete(obj)

        '        If afterDelegate IsNot Nothing Then
        '            afterDelegate(obj, mgr)
        '        End If
        '    Next
        'End Sub

        Protected Friend Sub UpdateCache(ByVal schema As OrmSchemaBase, _
            ByVal objs As IList, ByVal mgr As OrmManagerBase, ByVal afterDelegate As OnUpdated, _
            ByVal contextKey As Object, ByVal callbacks As IUpdateCacheCallbacks)

            Using SyncHelper.AcquireDynamicLock("j13rvnopqefv9-n24bth")
                Dim l As Dictionary(Of String, Dictionary(Of String, Object)) = Nothing
                Dim tt As Type = Nothing
                For Each obj As OrmBase In objs
                    If obj Is Nothing Then
                        Throw New ArgumentNullException("obj")
                    End If
                    tt = obj.GetType
                    Exit For
                Next

                Dim oschema As IOrmObjectSchemaBase = schema.GetObjectSchema(tt)
                If _type_depends.TryGetValue(tt, l) Then
                    For Each p As KeyValuePair(Of String, Dictionary(Of String, Object)) In l
                        Dim dic As IDictionary = CType(_filters(p.Key), IDictionary)
                        Dim m2m As Boolean = p.Key.Contains(OrmManagerBase.Const_JoinStaticString)
                        If dic IsNot Nothing Then
                            If callbacks IsNot Nothing Then
                                callbacks.BeginUpdate(p.Value.Count * objs.Count)
                            End If
                            For Each id As String In p.Value.Keys
                                Dim ce As OrmManagerBase.CachedItem = TryCast(dic(id), OrmManagerBase.CachedItem)
                                Dim f As IOrmFilter = Nothing
                                If ce IsNot Nothing Then
                                    If callbacks IsNot Nothing Then
                                        callbacks.BeginUpdateList(p.Key, id)
                                    End If

                                    For Each obj As OrmBase In objs
                                        If obj Is Nothing Then
                                            Throw New ArgumentNullException("obj")
                                        End If

                                        If tt IsNot obj.GetType Then
                                            Throw New ArgumentException("Collection contains different types")
                                        End If

                                        If obj._needAdd OrElse obj._needDelete Then
                                            f = ce.Filter
                                            Dim r As Boolean = False
                                            Dim er As IOrmFilter.EvalResult = IOrmFilter.EvalResult.Found
                                            If m2m Then
                                                'Dim oo As IRelation = TryCast(schema.GetObjectSchema(t), IRelation)
                                                'If oo IsNot Nothing Then
                                                '    Dim p1 As Pair(Of String, Type) = oo.GetFirstType
                                                '    Dim p2 As Pair(Of String, Type) = oo.GetSecondType
                                                '    Dim o1 As OrmBase = CType(schema.GetFieldValue(obj, p1.First), OrmBase)
                                                '    Dim o2 As OrmBase = CType(schema.GetFieldValue(obj, p2.First), OrmBase)
                                                '    Dim el As EditableList = CType(ce, OrmManagerBase.M2MCache).Entry
                                                '    If el.MainId <> o1.Identifier Then
                                                '        Throw New OrmManagerException("Invalid cache entry")
                                                '    End If
                                                '    el.Add(o2.Identifier)
                                                'Else
                                                '    r = True
                                                'End If
                                                Continue For
                                            ElseIf f IsNot Nothing Then
                                                er = f.Eval(schema, obj, oschema)
                                                r = er = IOrmFilter.EvalResult.Unknown
                                            End If

                                            If r Then
                                                dic.Remove(id)
                                            ElseIf er = IOrmFilter.EvalResult.Found Then
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
                                        End If
                                    Next

                                    If callbacks IsNot Nothing Then
                                        callbacks.EndUpdateList(p.Key, id)
                                    End If
                                Else
                                    dic.Remove(id)
                                End If
                                'Debug.WriteLine(t.Name & ": remove dependent " & id)
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
        End Sub

        'Protected Friend Sub DeleteAddOrDelete(ByVal t As Type)
        '    Using SyncHelper.AcquireDynamicLock("j13rvnopqefv9-n24bth")
        '        _invalidate_types.Remove(t)
        '    End Using
        'End Sub

        Private Shared Function Normalize(ByVal t As Type, ByVal f As IOrmFilter) As IOrmFilter
            If f IsNot Nothing Then
                For Each fl As OrmFilter In f.GetAllFilters
                    If fl.Type Is t Then
                        Return f
                    End If
                Next
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Зависимость выбираемого типа от ключа в кеше
        ''' </summary>
        ''' <param name="t"></param>
        ''' <param name="key"></param>
        ''' <param name="id"></param>
        ''' <remarks></remarks>
        Protected Friend Sub AddDependType(ByVal t As Type, ByVal key As String, ByVal id As String)
            'Debug.WriteLine(t.Name & ": add dependent " & id)
            Using SyncHelper.AcquireDynamicLock("j13rvnopqefv9-n24bth")
                Dim l As Dictionary(Of String, Dictionary(Of String, Object)) = Nothing
                If _type_depends.TryGetValue(t, l) Then
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
                    _type_depends.Add(t, l)
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
            Using SyncHelper.AcquireDynamicLock("1380fbhj145g90h2evgrqervg")
                Dim l As Dictionary(Of String, List(Of String)) = Nothing
                If _depends.TryGetValue(obj, l) Then
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
                    _depends.Add(obj, l)
                End If
            End Using
        End Sub

        ''' <summary>
        ''' Удаляет все ключи в кеше, которые зависят от данного объекта
        ''' </summary>
        ''' <param name="obj"></param>
        ''' <remarks></remarks>
        Protected Friend Sub RemoveDepends(ByVal obj As OrmBase)
            If _depends.ContainsKey(obj) Then
                Using SyncHelper.AcquireDynamicLock("1380fbhj145g90h2evgrqervg")
                    Dim l As Dictionary(Of String, List(Of String)) = Nothing
                    If _depends.TryGetValue(obj, l) Then
                        For Each p As KeyValuePair(Of String, List(Of String)) In l
                            Dim dic As IDictionary = CType(_filters(p.Key), IDictionary)
                            If dic IsNot Nothing Then
                                For Each id As String In p.Value
                                    dic.Remove(id)
                                Next
                                _filters.Remove(p.Key)
                            End If
                        Next
                        _depends.Remove(obj)
                    End If
                End Using
            End If
        End Sub

        Protected Friend Sub AddStoredProc(ByVal sp As StoredProcBase)
            Using SyncHelper.AcquireDynamicLock("olnfv9807b45gnpoweg01j3g")
                If sp.cached AndAlso Not _procs.Contains(sp) Then
                    _procs.Add(sp)
                End If
            End Using
        End Sub

        Protected Sub ValidateSPOnInsertDelete(ByVal obj As OrmBase)
            Using SyncHelper.AcquireDynamicLock("olnfv9807b45gnpoweg01j3g")
                For Each sp As StoredProcBase In _procs
                    If Not sp.IsReseted AndAlso sp.ValidateOnInsertDelete(obj) Then
                        sp.ResetCache(Me)
                    End If
                Next
            End Using
        End Sub

        Protected Sub ValidateSPOnUpdate(ByVal obj As OrmBase, ByVal fields As ICollection(Of String))
            Using SyncHelper.AcquireDynamicLock("olnfv9807b45gnpoweg01j3g")
                For Each sp As StoredProcBase In _procs
                    If Not sp.IsReseted AndAlso sp.ValidateOnUpdate(obj, fields) Then
                        sp.ResetCache(Me)
                    End If
                Next
            End Using
        End Sub
    End Class

    Public Class OrmCache
        Inherits OrmCacheBase

        Private _dics As IDictionary = Hashtable.Synchronized(New Hashtable)

        Public Overrides Sub Reset()
            _dics = Hashtable.Synchronized(New Hashtable)
        End Sub

        Public Overrides Function GetFiltersDic() As System.Collections.IDictionary
            Return Hashtable.Synchronized(New Hashtable)
        End Function

        'Public Overrides ReadOnly Property OrmDictionaryT(of T)() As System.Collections.Generic.IDictionary(Of Integer, T)

        Public Overrides Function GetOrmDictionary(ByVal t As System.Type, Optional ByVal schema As OrmSchemaBase = Nothing) As System.Collections.IDictionary
            Dim k As Object = t
            If schema IsNot Nothing Then
                k = schema.GetEntityTypeKey(t)
            End If

            Dim dic As IDictionary = CType(_dics(k), IDictionary)
            If dic Is Nothing Then
                SyncLock SyncRoot
                    dic = CType(_dics(k), IDictionary)
                    If dic Is Nothing Then
                        Dim gt As Type = GetType(Collections.HybridDictionary(Of ))
                        gt = gt.MakeGenericType(New Type() {t})
                        dic = CType(gt.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), IDictionary)
                        _dics(k) = dic
                    End If
                End SyncLock
            End If
            Return dic
        End Function

        Public Overrides Function GetOrmDictionary(Of T)(Optional ByVal schema As OrmSchemaBase = Nothing) As System.Collections.Generic.IDictionary(Of Integer, T)
            Return CType(GetOrmDictionary(GetType(T)), Global.System.Collections.Generic.IDictionary(Of Integer, T))
        End Function

        Public Sub New()
            Reset()
        End Sub
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

End Namespace
