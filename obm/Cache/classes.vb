Imports Worm.Entities.Meta
Imports Worm.Entities
Imports System.Collections.Generic

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

        Public Function GetEntity() As ICachedEntity
            Return OrmManager.CurrentManager.GetEntityFromCacheOrDB(_id, _t)
        End Function

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
        'Public ReadOnly Obj As _ICachedEntity
        Public ReadOnly DateTime As Date
        Public ReadOnly Reason As ReasonEnum
        Private _obj As EntityProxy
        Private _oldpk() As PKDesc

#If DEBUG Then
        Protected _stack As String
        Public ReadOnly Property StackTrace() As String
            Get
                Return _stack
            End Get
        End Property
#End If
        Sub New(ByVal obj As _ICachedEntity, ByVal user As Object, ByVal reason As ReasonEnum, ByVal pk() As PKDesc)
            'Sub New(ByVal user As Object, ByVal reason As ReasonEnum)
            DateTime = Now
            'Me.Obj = obj
            Me.User = user
            Me.Reason = reason
            _obj = New EntityProxy(obj)
            _oldpk = pk
#If DEBUG Then
            _stack = Environment.StackTrace
#End If
        End Sub

        Public ReadOnly Property Proxy() As EntityProxy
            Get
                Return _obj
            End Get
        End Property

        Public ReadOnly Property OlPK() As PKDesc()
            Get
                Return _oldpk
            End Get
        End Property
    End Class

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
            If Not _d.Contains(t) Then
                _d.Add(t)
            End If
        End Sub

        Public Sub AddUpdated(ByVal t As Type)
            If Not _u.Contains(t) Then
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