Namespace Orm

    Public Class Criteria
        Private _t As Type

        Public Sub New(ByVal t As Type)
            If t Is Nothing Then
                Throw New ArgumentNullException("t")
            End If

            _t = t
        End Sub

        Public Function Field(ByVal fieldName As String) As CriteriaField
            If String.IsNullOrEmpty(fieldName) Then
                Throw New ArgumentNullException("fieldName")
            End If

            Return New CriteriaField(_t, fieldName)
        End Function

        Public Shared Function Field(ByVal t As Type, ByVal fieldName As String) As CriteriaField
            If t Is Nothing Then
                Throw New ArgumentNullException("t")
            End If

            If String.IsNullOrEmpty(fieldName) Then
                Throw New ArgumentNullException("fieldName")
            End If

            Return New CriteriaField(t, fieldName)
        End Function

    End Class

    Public Class CriteriaField
        Private _t As Type
        Private _f As String
        Private _con As OrmCondition.OrmConditionConstructor
        Private _ct As ConditionOperator

        Protected Friend Sub New(ByVal t As Type, ByVal fieldName As String)
            'If t Is Nothing Then
            '    Throw New ArgumentNullException("t")
            'End If

            'If String.IsNullOrEmpty(fieldName) Then
            '    Throw New ArgumentNullException("fieldName")
            'End If

            _t = t
            _f = fieldName
        End Sub

        Protected Friend Sub New(ByVal t As Type, ByVal fieldName As String, _
            ByVal con As OrmCondition.OrmConditionConstructor, ByVal ct As ConditionOperator)
            _t = t
            _f = fieldName
            _con = con
            _ct = ct
        End Sub

        Protected Function GetLink(ByVal fl As IOrmFilter) As CriteriaLink
            If _con Is Nothing Then
                _con = New OrmCondition.OrmConditionConstructor
            End If
            _con.AddFilter(fl, _ct)
            Return New CriteriaLink(_t, _con)
        End Function

        Public Function Eq(ByVal value As Object) As CriteriaLink
            Return GetLink(New OrmFilter(_t, _f, New TypeWrap(Of Object)(value), FilterOperation.Equal))
        End Function

        Public Function NotEq(ByVal value As Object) As CriteriaLink
            Return GetLink(New OrmFilter(_t, _f, New TypeWrap(Of Object)(value), FilterOperation.NotEqual))
        End Function

        Public Function Eq(ByVal value As OrmBase) As CriteriaLink
            Return GetLink(New OrmFilter(_t, _f, value, FilterOperation.Equal))
        End Function

        Public Function NotEq(ByVal value As OrmBase) As CriteriaLink
            Return GetLink(New OrmFilter(_t, _f, value, FilterOperation.NotEqual))
        End Function

        Public Function GreaterThanEq(ByVal value As Object) As CriteriaLink
            Return GetLink(New OrmFilter(_t, _f, New TypeWrap(Of Object)(value), FilterOperation.GreaterEqualThan))
        End Function

        Public Function LessThanEq(ByVal value As Object) As CriteriaLink
            Return GetLink(New OrmFilter(_t, _f, New TypeWrap(Of Object)(value), FilterOperation.LessEqualThan))
        End Function

        Public Function GreaterThan(ByVal value As Object) As CriteriaLink
            Return GetLink(New OrmFilter(_t, _f, New TypeWrap(Of Object)(value), FilterOperation.GreaterThan))
        End Function

        Public Function LessThan(ByVal value As Object) As CriteriaLink
            Return GetLink(New OrmFilter(_t, _f, New TypeWrap(Of Object)(value), FilterOperation.LessThan))
        End Function

        Public Function [Like](ByVal value As String) As CriteriaLink
            Return GetLink(New OrmFilter(_t, _f, New TypeWrap(Of Object)(value), FilterOperation.Like))
        End Function

    End Class

    Public Class CriteriaLink
        Private _con As OrmCondition.OrmConditionConstructor
        Private _t As Type

        'Protected Friend Sub New(ByVal con As OrmCondition.OrmConditionConstructor)
        '    _con = con
        'End Sub

        Protected Friend Sub New(ByVal t As Type, ByVal con As OrmCondition.OrmConditionConstructor)
            _con = con
            _t = t
        End Sub

        Public Function [And](ByVal t As Type, ByVal fieldName As String) As CriteriaField
            If String.IsNullOrEmpty(fieldName) Then
                Throw New ArgumentNullException("fieldName")
            End If

            If t Is Nothing Then
                Throw New ArgumentNullException("t")
            End If

            Return New CriteriaField(t, fieldName, _con, ConditionOperator.And)
        End Function

        Public Function [Or](ByVal t As Type, ByVal fieldName As String) As CriteriaField
            If String.IsNullOrEmpty(fieldName) Then
                Throw New ArgumentNullException("fieldName")
            End If

            If t Is Nothing Then
                Throw New ArgumentNullException("t")
            End If

            Return New CriteriaField(t, fieldName, _con, ConditionOperator.Or)
        End Function

        Public Function [And](ByVal fieldName As String) As CriteriaField
            If String.IsNullOrEmpty(fieldName) Then
                Throw New ArgumentNullException("fieldName")
            End If

            Return New CriteriaField(_t, fieldName, _con, ConditionOperator.And)
        End Function

        Public Function [Or](ByVal fieldName As String) As CriteriaField
            If String.IsNullOrEmpty(fieldName) Then
                Throw New ArgumentNullException("fieldName")
            End If

            Return New CriteriaField(_t, fieldName, _con, ConditionOperator.Or)
        End Function

        Public Function [And](ByVal link As CriteriaLink) As CriteriaLink
            If link IsNot Nothing Then
                _con.AddFilter(link._con.Condition, ConditionOperator.And)
            End If

            Return Me
        End Function

        Public Function [Or](ByVal link As CriteriaLink) As CriteriaLink
            If link IsNot Nothing Then
                _con.AddFilter(link._con.Condition, ConditionOperator.Or)
            End If

            Return Me
        End Function

        Public ReadOnly Property Filter() As IOrmFilter
            Get
                Return _con.Condition
            End Get
        End Property
    End Class

    Public Module Sorting

        Public Function Field(ByVal fieldName As String) As SortOrder
            Return New SortOrder(fieldName)
        End Function

        Public Function External(ByVal fieldName As String) As SortOrder
            Return New SortOrder(fieldName, True)
        End Function
    End Module

    Public Class SortOrder
        Private _f As String
        Private _ext As Boolean

        Protected Friend Sub New(ByVal f As String)
            _f = f
        End Sub

        Protected Friend Sub New(ByVal f As String, ByVal ext As Boolean)
            _f = f
            _ext = ext
        End Sub

        Public ReadOnly Property Asc() As Sort
            Get
                Return New Sort(_f, SortType.Asc, _ext)
            End Get
        End Property

        Public ReadOnly Property Desc() As Sort
            Get
                Return New Sort(_f, SortType.Desc, _ext)
            End Get
        End Property

        Public Function Order(ByVal orderParam As Boolean) As Sort
            If orderParam Then
                Return New Sort(_f, SortType.Asc, _ext)
            Else
                Return New Sort(_f, SortType.Desc, _ext)
            End If
        End Function

        Public Function Order(ByVal orderParam As String) As Sort
            Return New Sort(_f, (CType([Enum].Parse(GetType(SortType), orderParam, True), SortType)), _ext)
        End Function
    End Class

    Public Class Sort
        Private _f As String
        Private _order As SortType
        Private _ext As Boolean

        Public Sub New(ByVal fieldName As String, ByVal order As SortType, ByVal external As Boolean)
            _f = fieldName
            _order = order
            _ext = external
        End Sub

        Public Property FieldName() As String
            Get
                Return _f
            End Get
            Set(ByVal value As String)
                _f = value
            End Set
        End Property

        Public Property Order() As SortType
            Get
                Return _order
            End Get
            Set(ByVal value As SortType)
                _order = value
            End Set
        End Property

        Public Property IsExternal() As Boolean
            Get
                Return _ext
            End Get
            Set(ByVal value As Boolean)
                _ext = value
            End Set
        End Property

        Public Overrides Function ToString() As String
            Return _f & _order.ToString & _ext.ToString
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, Sort))
        End Function

        Public Overloads Function Equals(ByVal s As Sort) As Boolean
            If s Is Nothing Then
                Return False
            Else
                Return _f = s._f AndAlso _order = s._order AndAlso _ext = s._ext
            End If
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return ToString.GetHashCode
        End Function
    End Class
End Namespace