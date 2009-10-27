Imports Worm.Entities.Meta
Imports Worm.Entities
Imports System.Collections.Generic
Imports Worm.Query.Sorting
Imports Worm.Expressions2

Namespace Cache

    <Serializable()> _
    Public Class EntityProxy
        Private _id() As PKDesc
        Private _t As Type

        Public Sub New(ByVal id() As PKDesc, ByVal type As Type)
            _id = id
            _t = type
        End Sub

        Public Sub New(ByVal o As ICachedEntity)
            _id = o.GetPKValues
            _t = o.GetType
        End Sub

        'Public Function GetEntity() As ICachedEntity
        '    Return OrmManager.CurrentManager.GetEntityFromCacheOrDB(_id, _t)
        'End Function

        Public ReadOnly Property EntityType() As Type
            Get
                Return _t
            End Get
        End Property

        Public ReadOnly Property PK() As PKDesc()
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
            Return _t Is obj._t AndAlso IdEquals(obj.PK)
        End Function

        Protected Function IdEquals(ByVal ids() As PKDesc) As Boolean
            If _id.Length <> ids.Length Then Return False
            For i As Integer = 0 To _id.Length - 1
                Dim p As PKDesc = _id(i)
                Dim p2 As PKDesc = ids(i)
                If p.PropertyAlias <> p2.PropertyAlias OrElse Not p.Value.Equals(p.Value) Then
                    Return False
                End If
            Next
            Return True
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _t.GetHashCode() Xor GetIdsHashCode()
        End Function

        Protected Function GetIdsHashCode() As Integer
            Return GetIdsString.GetHashCode
        End Function

        Protected Function GetIdsString() As String
            Dim sb As New StringBuilder
            For Each p As PKDesc In _id
                sb.Append(p.PropertyAlias).Append(":").Append(p.Value).Append(",")
            Next
            Return sb.ToString
        End Function

        Public Overrides Function ToString() As String
            Return _t.ToString & "^" & GetIdsString()
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

    <Serializable()> _
    Public Class ObjectModification
        Public Enum ReasonEnum
            Unknown
            Delete
            Edit
            SaveNew
        End Enum

        Public ReadOnly User As Object
        '<CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104")> _
        Private _obj As Object
        Public ReadOnly DateTime As Date
        Public ReadOnly Reason As ReasonEnum
        'Private _obj As EntityProxy
        Private _oldpk() As PKDesc

#If DEBUG Then
        Protected _stack As String
        Public ReadOnly Property StackTrace() As String
            Get
                Return _stack
            End Get
        End Property
#End If
        Sub New(ByVal obj As Object, ByVal user As Object, ByVal reason As ReasonEnum, ByVal pk() As PKDesc)
            'Sub New(ByVal user As Object, ByVal reason As ReasonEnum)
            DateTime = Now
            'Me.Obj = obj
            Me.User = user
            Me.Reason = reason
            '_obj = New EntityProxy(obj)
            _obj = obj
            _oldpk = pk
#If DEBUG Then
            _stack = Environment.StackTrace
#End If
        End Sub

        Public ReadOnly Property Obj() As Object
            Get
                Return _obj
            End Get
        End Property

        'Public ReadOnly Property Proxy() As EntityProxy
        '    Get
        '        Return _obj
        '    End Get
        'End Property

        Public ReadOnly Property OlPK() As PKDesc()
            Get
                Return _oldpk
            End Get
        End Property
    End Class

    <Serializable()> _
    Public Class CacheKey
        Inherits PKWrapper

        Private _key As Integer

        Public Sub New(ByVal o As ICachedEntity)
            MyBase.New(o.GetPKValues)
            _key = o.Key
        End Sub

        Public Overrides Function GetHashCode() As Integer
            Return _key
        End Function

        Public Overrides Function ToString() As String
            Return _key.ToString
        End Function
    End Class

    <Serializable()> _
    Public Class WeakEntityReference
        Private _e As EntityProxy
        Private _ref As WeakReference

        Public Sub New(ByVal o As ICachedEntity)
            _e = New EntityProxy(o)
            _ref = New WeakReference(o)
        End Sub

        Public Sub Reset()
            _ref.Target = Nothing
        End Sub

        Protected Function GetEntityFromCacheOrCreate(ByVal mgr As OrmManager, ByVal cache As CacheBase, ByVal pk() As PKDesc, ByVal type As Type, _
            ByVal addOnCreate As Boolean, ByVal filterInfo As Object, ByVal mpe As ObjectMappingEngine, ByVal dic As IDictionary) As ICachedEntity
            Dim o As _ICachedEntity = CachedEntity.CreateObject(pk, type, cache, mpe)

            o.SetObjectState(ObjectState.NotLoaded)

            Dim co As ICachedEntity = CType(cache.FindObjectInCache(type, o, New CacheKey(o), TryCast(mpe.GetEntitySchema(type), ICacheBehavior), dic, addOnCreate, False), ICachedEntity)

            cache.GetEntityFromCacheOrCreate(pk, type, addOnCreate, dic, mpe)
            mgr.RaiseObjectRestored(ReferenceEquals(co, o), co)

            Return co
        End Function

        Public Function GetObject(Of T As ICachedEntity)(ByVal mgr As OrmManager, ByVal dic As IDictionary) As T
            Return GetObject(Of T)(mgr, mgr.Cache, mgr.GetContextInfo, mgr.MappingEngine, dic)
        End Function

        Public Function GetObject(Of T As ICachedEntity)(ByVal mgr As OrmManager, ByVal cache As CacheBase, _
            ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine, ByVal dic As IDictionary) As T
            Dim o As T = CType(_ref.Target, T)
            If o Is Nothing Then
                o = CType(GetEntityFromCacheOrCreate(mgr, cache, _e.PK, _e.EntityType, True, filterInfo, schema, dic), T) 'mc.FindObject(id, t)
                If o Is Nothing AndAlso cache.NewObjectManager IsNot Nothing Then
                    o = CType(cache.NewObjectManager.GetNew(_e.EntityType, _e.PK), T)
                End If
            End If
            Return o
        End Function

        Public Function GetObject(ByVal mgr As OrmManager, ByVal dic As IDictionary) As ICachedEntity
            Return GetObject(mgr, mgr.Cache, mgr.GetContextInfo, mgr.MappingEngine, dic)
        End Function

        Public Function GetObject(ByVal mgr As OrmManager, ByVal cache As CacheBase, _
            ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine, ByVal dic As IDictionary) As ICachedEntity
            Dim o As ICachedEntity = CType(_ref.Target, ICachedEntity)
            If o Is Nothing Then
                o = GetEntityFromCacheOrCreate(mgr, cache, _e.PK, _e.EntityType, True, filterInfo, schema, dic) 'mc.FindObject(id, t)
                If o Is Nothing AndAlso cache.NewObjectManager IsNot Nothing Then
                    o = cache.NewObjectManager.GetNew(_e.EntityType, _e.PK)
                End If
            End If
            Return o
        End Function

        Protected Friend ReadOnly Property Obj() As ICachedEntity
            Get
                Return CType(_ref.Target, ICachedEntity)
            End Get
        End Property

        Public ReadOnly Property ObjName() As String
            Get
                Return _e.ToString
            End Get
        End Property

        Public ReadOnly Property IsAlive() As Boolean
            Get
                Return _ref.IsAlive
            End Get
        End Property

        Public ReadOnly Property IsLoaded() As Boolean
            Get
                Return _ref.IsAlive AndAlso CType(_ref.Target, ICachedEntity).IsLoaded
            End Get
        End Property

        Public ReadOnly Property IsEqual(ByVal obj As ICachedEntity) As Boolean
            Get
                If obj Is Nothing Then
                    Return False
                End If

                If _ref.IsAlive Then
                    Return _ref.Target.Equals(obj)
                Else
                    Return New EntityProxy(obj).Equals(_e)
                End If
            End Get
        End Property

        Public ReadOnly Property EntityType() As Type
            Get
                Return _e.EntityType
            End Get
        End Property
    End Class

    <Serializable()> _
    Public Class WeakEntityList
        Private _l As Generic.List(Of WeakEntityReference)
        Private _t As Type

        Public Sub New(ByVal l As List(Of WeakEntityReference), ByVal t As Type)
            _l = l
            _t = t
        End Sub

        Public Function CanSort(ByVal mc As OrmManager, ByRef arr As ArrayList, _
                                ByVal sort As OrderByClause) As Boolean
            'If sort.Previous IsNot Nothing Then
            '    Return False
            'End If

            arr = New ArrayList
            Dim dic As IDictionary = Nothing
            For Each le As WeakEntityReference In _l
                If Not le.IsLoaded Then
                    Return False
                Else
                    If dic Is Nothing Then
                        dic = mc.Cache.GetOrmDictionary(le.EntityType, mc.MappingEngine)
                    End If
                    Dim o As ICachedEntity = le.GetObject(mc, dic)
                    arr.Add(o)
                End If
            Next
            Return True
        End Function

        Public Sub Clear(ByVal mgr As OrmManager)
            For i As Integer = 0 To _l.Count - 1
                Dim le As WeakEntityReference = _l(i)
                Dim o As ICachedEntity = le.Obj
                If o IsNot Nothing Then
                    mgr.RemoveObjectFromCache(CType(o, _ICachedEntity))
                End If
                le.Reset()
            Next
        End Sub

        Public Sub Remove(ByVal obj As ICachedEntity)
            For i As Integer = 0 To _l.Count - 1
                Dim le As WeakEntityReference = _l(i)
                If le.IsEqual(obj) Then
                    _l.RemoveAt(i)
                    le.Reset()
                    Exit For
                End If
            Next
        End Sub

        Public ReadOnly Property Count() As Integer
            Get
                Return _l.Count
            End Get
        End Property

        Public ReadOnly Property List() As List(Of WeakEntityReference)
            Get
                Return _l
            End Get
        End Property

        Public ReadOnly Property RealType() As Type
            Get
                Return _t
            End Get
        End Property
    End Class

    <Serializable()> _
    Public Class WeakEntityMatrix
        'Implements IEnumerable(Of WeekEntityList)
        Implements ICollection(Of WeakEntityList), ICollection

        Private _l As List(Of WeakEntityList)

        Public Sub New(ByVal l As List(Of WeakEntityList))
            _l = l
        End Sub

        Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of WeakEntityList) Implements System.Collections.Generic.IEnumerable(Of WeakEntityList).GetEnumerator
            Return _l.GetEnumerator
        End Function

        Protected Function _GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return GetEnumerator()
        End Function

        Default Public ReadOnly Property Item(ByVal i As Integer) As WeakEntityList
            Get
                Return _l(i)
            End Get
        End Property

        Public Sub Add(ByVal item As WeakEntityList) Implements System.Collections.Generic.ICollection(Of WeakEntityList).Add
            Throw New NotImplementedException
        End Sub

        Public Sub Clear() Implements System.Collections.Generic.ICollection(Of WeakEntityList).Clear
            Throw New NotImplementedException
        End Sub

        Public Function Contains(ByVal item As WeakEntityList) As Boolean Implements System.Collections.Generic.ICollection(Of WeakEntityList).Contains
            Throw New NotImplementedException
        End Function

        Public Sub CopyTo(ByVal array() As WeakEntityList, ByVal arrayIndex As Integer) Implements System.Collections.Generic.ICollection(Of WeakEntityList).CopyTo
            Throw New NotImplementedException
        End Sub

        Public ReadOnly Property Count() As Integer Implements System.Collections.Generic.ICollection(Of WeakEntityList).Count
            Get
                Return _l.Count
            End Get
        End Property

        Public ReadOnly Property IsReadOnly() As Boolean Implements System.Collections.Generic.ICollection(Of WeakEntityList).IsReadOnly
            Get
                Return True
            End Get
        End Property

        Public Function Remove(ByVal item As WeakEntityList) As Boolean Implements System.Collections.Generic.ICollection(Of WeakEntityList).Remove
            Throw New NotImplementedException
        End Function

        Protected Sub _CopyTo(ByVal array As System.Array, ByVal index As Integer) Implements System.Collections.ICollection.CopyTo
            Throw New NotImplementedException
        End Sub

        Protected ReadOnly Property _Count() As Integer Implements System.Collections.ICollection.Count
            Get
                Return Count
            End Get
        End Property

        Public ReadOnly Property IsSynchronized() As Boolean Implements System.Collections.ICollection.IsSynchronized
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property SyncRoot() As Object Implements System.Collections.ICollection.SyncRoot
            Get
                Throw New NotImplementedException
            End Get
        End Property
    End Class

    Module qd
        Public Function QueryDependentTypes(ByVal mpe As ObjectMappingEngine, ByVal o As Object) As IDependentTypes
            Dim qd As IQueryDependentTypes = TryCast(o, IQueryDependentTypes)
            If qd IsNot Nothing Then
                Return qd.Get(mpe)
            End If

            Return New EmptyDependentTypes
        End Function

        Public Function IsCalculated(ByVal dp As IDependentTypes) As Boolean
            If dp Is Nothing Then
                Return True
            Else
                Return dp.GetType IsNot GetType(EmptyDependentTypes)
            End If
        End Function

        Public Sub Add2Cache(ByVal cache As OrmCache, ByVal dp As IDependentTypes, ByVal key As String, ByVal id As String)
            If dp IsNot Nothing Then
                cache.validate_AddDeleteType(dp.GetAddDelete, key, id)
                cache.validate_UpdateType(dp.GetUpdate, key, id)
            End If
        End Sub

        Public Function IsEmpty(ByVal dp As IDependentTypes) As Boolean
            If dp Is Nothing Then
                Return True
            Else
                Return Not (dp.GetAddDelete.GetEnumerator.MoveNext OrElse dp.GetUpdate.GetEnumerator.MoveNext)
            End If
        End Function
    End Module

    NotInheritable Class EmptyDependentTypes
        Implements IDependentTypes

        Public Function GetAddDelete() As System.Collections.Generic.IEnumerable(Of System.Type) Implements IDependentTypes.GetAddDelete
            Return Nothing
        End Function

        Public Function GetUpdate() As System.Collections.Generic.IEnumerable(Of System.Type) Implements IDependentTypes.GetUpdate
            Return Nothing
        End Function
    End Class

    Class DependentTypes
        Implements IDependentTypes

        Private _d As New List(Of Type)
        Private _u As New List(Of Type)

        Public Sub AddBoth(ByVal t As Type)
            AddDeleted(t)
            AddUpdated(t)
        End Sub

        Public Sub AddBoth(ByVal ts As IEnumerable(Of Type))
            For Each t As Type In ts
                AddDeleted(t)
                AddUpdated(t)
            Next
        End Sub

        Public Sub AddDeleted(ByVal t As Type)
            If t IsNot Nothing AndAlso Not _d.Contains(t) Then
                _d.Add(t)
            End If
        End Sub

        Public Sub AddUpdated(ByVal t As Type)
            If t IsNot Nothing AndAlso Not _u.Contains(t) Then
                _u.Add(t)
            End If
        End Sub

        Public Sub AddDeleted(ByVal col As IEnumerable(Of Type))
            For Each t As Type In col
                AddDeleted(t)
            Next
        End Sub

        Public Sub AddUpdated(ByVal col As IEnumerable(Of Type))
            For Each t As Type In col
                AddUpdated(t)
            Next
        End Sub

        Public Sub Merge(ByVal dp As IDependentTypes)
            If dp IsNot Nothing Then
                AddDeleted(dp.GetAddDelete)
                AddUpdated(dp.GetUpdate)
            End If
        End Sub

        Public ReadOnly Property IsEmpty() As Boolean
            Get
                Return _d.Count = 0 AndAlso _u.Count = 0
            End Get
        End Property

        Public Function [Get]() As IDependentTypes
            If IsEmpty Then
                Return Nothing
            End If
            Return Me
        End Function

        Public Function GetAddDelete() As System.Collections.Generic.IEnumerable(Of System.Type) Implements IDependentTypes.GetAddDelete
            Return _d
        End Function

        Public Function GetUpdate() As System.Collections.Generic.IEnumerable(Of System.Type) Implements IDependentTypes.GetUpdate
            Return _u
        End Function
    End Class

End Namespace