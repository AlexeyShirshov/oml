Imports Worm.Orm.Meta
Imports Worm.Orm

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
        <CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104")> _
        Public ReadOnly Obj As _ICachedEntity
        Public ReadOnly DateTime As Date

        Public ReadOnly Reason As ReasonEnum

#If DEBUG Then
        Protected _stack As String
        Public ReadOnly Property StackTrace() As String
            Get
                Return _stack
            End Get
        End Property
#End If
        Sub New(ByVal obj As _ICachedEntity, ByVal user As Object, ByVal reason As ReasonEnum)
            DateTime = Now
            Me.Obj = obj
            Me.User = user
            Me.Reason = reason
#If DEBUG Then
            _stack = Environment.StackTrace
#End If
        End Sub

    End Class

End Namespace