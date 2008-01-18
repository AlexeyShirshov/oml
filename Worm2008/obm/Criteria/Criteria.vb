Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Core
Imports Worm.Criteria.Values
Imports Worm.Orm
Imports Worm.Criteria.Joins
Imports Worm.Criteria

Namespace Criteria

    Public Enum FilterOperation
        Equal
        NotEqual
        GreaterThan
        GreaterEqualThan
        LessEqualThan
        LessThan
        [In]
        NotIn
        [Like]
        [Is]
        [IsNot]
        Exists
        NotExists
        Between
    End Enum

    Public Interface ICtor
        Function Field(ByVal fieldName As String) As CriteriaField
    End Interface

    Public MustInherit Class CriteriaField
        Private _t As Type
        Private _f As String
        Private _con As Condition.ConditionConstructorBase
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
            ByVal con As Condition.ConditionConstructorBase, ByVal ct As ConditionOperator)
            _t = t
            _f = fieldName
            _con = con
            _ct = ct
        End Sub

        Protected ReadOnly Property Type() As Type
            Get
                Return _t
            End Get
        End Property

        Protected ReadOnly Property Field() As String
            Get
                Return _f
            End Get
        End Property

        Protected Property ConditionCtor() As Condition.ConditionConstructorBase
            Get
                Return _con
            End Get
            Set(ByVal value As Condition.ConditionConstructorBase)
                _con = value
            End Set
        End Property

        Protected ReadOnly Property ConditionOper() As ConditionOperator
            Get
                Return _ct
            End Get
        End Property

        Protected MustOverride Function GetLink(ByVal fl As IFilter) As CriteriaLink
        Protected MustOverride Function CreateFilter(ByVal v As IParamFilterValue, ByVal oper As FilterOperation) As IFilter

        Public Function Eq(ByVal value As Object) As CriteriaLink
            If value Is Nothing OrElse value Is DBNull.Value Then
                Return IsNull()
            Else
                Return GetLink(CreateFilter(New ScalarValue(value), FilterOperation.Equal))
            End If
        End Function

        Public Function NotEq(ByVal value As Object) As CriteriaLink
            If value Is Nothing OrElse value Is DBNull.Value Then
                Return IsNotNull()
            Else
                Return GetLink(CreateFilter(New ScalarValue(value), FilterOperation.NotEqual))
            End If
        End Function

        Public Function Eq(ByVal value As OrmBase) As CriteriaLink
            If value Is Nothing Then
                Return IsNull()
            Else
                Return GetLink(CreateFilter(New EntityValue(value), FilterOperation.Equal))
            End If
        End Function

        Public Function NotEq(ByVal value As OrmBase) As CriteriaLink
            If value Is Nothing Then
                Return IsNotNull()
            Else
                Return GetLink(CreateFilter(New EntityValue(value), FilterOperation.NotEqual))
            End If
        End Function

        Public Function GreaterThanEq(ByVal value As Object) As CriteriaLink
            Return GetLink(CreateFilter(New ScalarValue(value), FilterOperation.GreaterEqualThan))
        End Function

        Public Function LessThanEq(ByVal value As Object) As CriteriaLink
            Return GetLink(CreateFilter(New ScalarValue(value), FilterOperation.LessEqualThan))
        End Function

        Public Function GreaterThan(ByVal value As Object) As CriteriaLink
            Return GetLink(CreateFilter(New ScalarValue(value), FilterOperation.GreaterThan))
        End Function

        Public Function LessThan(ByVal value As Object) As CriteriaLink
            Return GetLink(CreateFilter(New ScalarValue(value), FilterOperation.LessThan))
        End Function

        Public Function [Like](ByVal value As String) As CriteriaLink
            If String.IsNullOrEmpty(value) Then
                Throw New ArgumentNullException("value")
            End If
            Return GetLink(CreateFilter(New ScalarValue(value), FilterOperation.Like))
        End Function

        Public Function IsNull() As CriteriaLink
            Return GetLink(CreateFilter(New DBNullValue(), FilterOperation.Is))
        End Function

        Public Function IsNotNull() As CriteriaLink
            Return GetLink(CreateFilter(New DBNullValue(), FilterOperation.IsNot))
        End Function

        Public Function [In](ByVal arr As ICollection) As CriteriaLink
            Return GetLink(CreateFilter(New InValue(arr), FilterOperation.In))
        End Function

        Public Function NotIn(ByVal arr As ICollection) As CriteriaLink
            Return GetLink(CreateFilter(New InValue(arr), FilterOperation.NotIn))
        End Function

        Public Function Between(ByVal left As Object, ByVal right As Object) As CriteriaLink
            Return GetLink(CreateFilter(New BetweenValue(left, right), FilterOperation.Between))
        End Function

        Public Function Op(ByVal oper As FilterOperation, ByVal value As IParamFilterValue) As CriteriaLink
            Return GetLink(CreateFilter(value, oper))
        End Function

    End Class

    Public Interface IGetFilter
        ReadOnly Property Filter() As IFilter
        ReadOnly Property Filter(ByVal t As Type) As IFilter
    End Interface

    Public MustInherit Class CriteriaLink
        Implements IGetFilter

        Private _con As Condition.ConditionConstructorBase
        Private _t As Type

        Protected Friend Sub New(ByVal con As Condition.ConditionConstructorBase)
            _con = con
        End Sub

        Public Sub New()

        End Sub

        Public Sub New(ByVal t As Type)
            _t = t
        End Sub

        Protected Friend Sub New(ByVal t As Type, ByVal con As Condition.ConditionConstructorBase)
            _con = con
            _t = t
        End Sub

        Protected MustOverride Function CreateField(ByVal t As Type, ByVal fieldName As String, ByVal con As Condition.ConditionConstructorBase, ByVal oper As ConditionOperator) As CriteriaField

        Public Function [And](ByVal t As Type, ByVal fieldName As String) As CriteriaField
            If String.IsNullOrEmpty(fieldName) Then
                Throw New ArgumentNullException("fieldName")
            End If

            If t Is Nothing Then
                Throw New ArgumentNullException("t")
            End If

            Return CreateField(t, fieldName, _con, ConditionOperator.And)
        End Function

        Public Function [Or](ByVal t As Type, ByVal fieldName As String) As CriteriaField
            If String.IsNullOrEmpty(fieldName) Then
                Throw New ArgumentNullException("fieldName")
            End If

            If t Is Nothing Then
                Throw New ArgumentNullException("t")
            End If

            Return CreateField(t, fieldName, _con, ConditionOperator.Or)
        End Function

        Public Function [And](ByVal fieldName As String) As CriteriaField
            If String.IsNullOrEmpty(fieldName) Then
                Throw New ArgumentNullException("fieldName")
            End If

            Return CreateField(_t, fieldName, _con, ConditionOperator.And)
        End Function

        Public Function [Or](ByVal fieldName As String) As CriteriaField
            If String.IsNullOrEmpty(fieldName) Then
                Throw New ArgumentNullException("fieldName")
            End If

            Return CreateField(_t, fieldName, _con, ConditionOperator.Or)
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

        Public ReadOnly Property Filter() As IFilter Implements IGetFilter.Filter
            Get
                If _con IsNot Nothing Then
                    Return _con.Condition
                Else
                    Return Nothing
                End If
            End Get
        End Property

        Public Overridable ReadOnly Property Filter(ByVal t As Type) As IFilter Implements IGetFilter.Filter
            Get
                If _con IsNot Nothing Then
                    Dim f As IFilter = _con.Condition
                    Dim ef As IEntityFilter = TryCast(f, IEntityFilter)
                    If ef IsNot Nothing Then
                        ef.GetFilterTemplate.SetType(t)
                    End If
                    Return f
                Else
                    Return Nothing
                End If
            End Get
        End Property

        Protected ReadOnly Property ConditionCtor() As Condition.ConditionConstructorBase
            Get
                Return _con
            End Get
        End Property
    End Class

    'Friend Class _CriteriaLink
    '    Inherits CriteriaLink

    '    Private _f As IEntityFilter

    '    Public Sub New(ByVal f As IEntityFilter)
    '        _f = f
    '    End Sub

    '    Public Overrides ReadOnly Property Filter(ByVal t As System.Type) As IEntityFilter
    '        Get
    '            Return _f
    '        End Get
    '    End Property
    'End Class

End Namespace
