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

        Public Shared Function AutoTypeField(ByVal fieldName As String) As CriteriaField
            If String.IsNullOrEmpty(fieldName) Then
                Throw New ArgumentNullException("fieldName")
            End If

            Return New CriteriaField(Nothing, fieldName)
        End Function

        'Public Shared Function Exists(ByVal t As Type, ByVal fieldName As String) As CriteriaLink
        '    Return New CriteriaLink().AndExists(t, fieldName)
        'End Function
    End Class

    Public Class CriteriaField
        Private _t As Type
        Private _f As String
        Private _con As Orm.Condition.ConditionConstructor
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
            ByVal con As Orm.Condition.ConditionConstructor, ByVal ct As ConditionOperator)
            _t = t
            _f = fieldName
            _con = con
            _ct = ct
        End Sub

        Protected Function GetLink(ByVal fl As Orm.IFilter) As CriteriaLink
            If _con Is Nothing Then
                _con = New Orm.Condition.ConditionConstructor
            End If
            _con.AddFilter(fl, _ct)
            Return New CriteriaLink(_t, _con)
        End Function

        Public Function Eq(ByVal value As Object) As CriteriaLink
            If value Is Nothing OrElse value Is DBNull.Value Then
                Return IsNull()
            Else
                Return GetLink(New EntityFilter(_t, _f, New ScalarValue(value), FilterOperation.Equal))
            End If
        End Function

        Public Function NotEq(ByVal value As Object) As CriteriaLink
            If value Is Nothing OrElse value Is DBNull.Value Then
                Return IsNotNull()
            Else
                Return GetLink(New EntityFilter(_t, _f, New ScalarValue(value), FilterOperation.NotEqual))
            End If
        End Function

        Public Function Eq(ByVal value As OrmBase) As CriteriaLink
            If value Is Nothing Then
                Return IsNull()
            Else
                Return GetLink(New EntityFilter(_t, _f, New EntityValue(value), FilterOperation.Equal))
            End If
        End Function

        Public Function NotEq(ByVal value As OrmBase) As CriteriaLink
            If value Is Nothing Then
                Return IsNotNull()
            Else
                Return GetLink(New EntityFilter(_t, _f, New EntityValue(value), FilterOperation.NotEqual))
            End If
        End Function

        Public Function GreaterThanEq(ByVal value As Object) As CriteriaLink
            Return GetLink(New EntityFilter(_t, _f, New ScalarValue(value), FilterOperation.GreaterEqualThan))
        End Function

        Public Function LessThanEq(ByVal value As Object) As CriteriaLink
            Return GetLink(New EntityFilter(_t, _f, New ScalarValue(value), FilterOperation.LessEqualThan))
        End Function

        Public Function GreaterThan(ByVal value As Object) As CriteriaLink
            Return GetLink(New EntityFilter(_t, _f, New ScalarValue(value), FilterOperation.GreaterThan))
        End Function

        Public Function LessThan(ByVal value As Object) As CriteriaLink
            Return GetLink(New EntityFilter(_t, _f, New ScalarValue(value), FilterOperation.LessThan))
        End Function

        Public Function [Like](ByVal value As String) As CriteriaLink
            If String.IsNullOrEmpty(value) Then
                Throw New ArgumentNullException("value")
            End If
            Return GetLink(New EntityFilter(_t, _f, New ScalarValue(value), FilterOperation.Like))
        End Function

        Public Function IsNull() As CriteriaLink
            Return GetLink(New EntityFilter(_t, _f, New DBNullValue(), FilterOperation.Is))
        End Function

        Public Function IsNotNull() As CriteriaLink
            Return GetLink(New EntityFilter(_t, _f, New DBNullValue(), FilterOperation.IsNot))
        End Function

        Public Function [In](ByVal arr As ICollection) As CriteriaLink
            Return GetLink(New EntityFilter(_t, _f, New InValue(arr), FilterOperation.In))
        End Function

        Public Function NotIn(ByVal arr As ICollection) As CriteriaLink
            Return GetLink(New EntityFilter(_t, _f, New InValue(arr), FilterOperation.NotIn))
        End Function

        Public Function [In](ByVal t As Type) As CriteriaLink
            Return GetLink(New EntityFilter(_t, _f, New SubQuery(t, Nothing), FilterOperation.In))
        End Function

        Public Function NotIn(ByVal t As Type) As CriteriaLink
            Return GetLink(New EntityFilter(_t, _f, New SubQuery(t, Nothing), FilterOperation.NotIn))
        End Function

        Public Function [In](ByVal t As Type, ByVal fieldName As String) As CriteriaLink
            Return GetLink(New EntityFilter(_t, _f, New SubQuery(t, Nothing, fieldName), FilterOperation.In))
        End Function

        Public Function NotIn(ByVal t As Type, ByVal fieldName As String) As CriteriaLink
            Return GetLink(New EntityFilter(_t, _f, New SubQuery(t, Nothing, fieldName), FilterOperation.NotIn))
        End Function

        Public Function Exists(ByVal t As Type, ByVal joinField As String) As CriteriaLink
            Dim j As New JoinFilter(_t, _f, t, joinField, FilterOperation.Equal)
            Return GetLink(New NonTemplateFilter(New SubQuery(t, j), FilterOperation.Exists))
        End Function

        Public Function NotExists(ByVal t As Type, ByVal joinField As String) As CriteriaLink
            Dim j As New JoinFilter(_t, _f, t, joinField, FilterOperation.Equal)
            Return GetLink(New NonTemplateFilter(New SubQuery(t, j), FilterOperation.NotExists))
        End Function

        Public Function Exists(ByVal t As Type) As CriteriaLink
            Return Exists(t, "ID")
        End Function

        Public Function NotExists(ByVal t As Type) As CriteriaLink
            Return NotExists(t, "ID")
        End Function

        Public Function Exists(ByVal t As Type, ByVal f As IFilter) As CriteriaLink
            Return GetLink(New NonTemplateFilter(New SubQuery(t, f), FilterOperation.Exists))
        End Function

        Public Function NotExists(ByVal t As Type, ByVal f As IFilter) As CriteriaLink
            Return GetLink(New NonTemplateFilter(New SubQuery(t, f), FilterOperation.NotExists))
        End Function

        Public Function Op(ByVal oper As FilterOperation, ByVal value As IFilterValue) As CriteriaLink
            Return GetLink(New EntityFilter(_t, _f, value, oper))
        End Function
    End Class

    Public Class CriteriaNonField
        Private _con As Orm.Condition.ConditionConstructor
        Private _ct As ConditionOperator

        Protected Friend Sub New(ByVal con As Orm.Condition.ConditionConstructor, ByVal ct As ConditionOperator)
            _con = con
            _ct = ct
        End Sub

        Protected Function GetLink(ByVal fl As Orm.IFilter) As CriteriaLink
            If _con Is Nothing Then
                _con = New Orm.Condition.ConditionConstructor
            End If
            _con.AddFilter(fl, _ct)
            Return New CriteriaLink(_con)
        End Function

        Public Function Exists(ByVal t As Type, ByVal joinFilter As IFilter) As CriteriaLink
            Return GetLink(New NonTemplateFilter(New SubQuery(t, joinFilter), FilterOperation.Exists))
        End Function

        Public Function NotExists(ByVal t As Type, ByVal joinFilter As IFilter) As CriteriaLink
            Return GetLink(New NonTemplateFilter(New SubQuery(t, joinFilter), FilterOperation.NotExists))
        End Function

    End Class

    Public Interface IGetFilter
        ReadOnly Property Filter() As IFilter
        ReadOnly Property Filter(ByVal t As Type) As IFilter
    End Interface

    Public Class CriteriaLink
        Implements IGetFilter

        Private _con As Orm.Condition.ConditionConstructor
        Private _t As Type

        Protected Friend Sub New(ByVal con As Condition.ConditionConstructor)
            _con = con
        End Sub

        Public Sub New()

        End Sub

        Public Sub New(ByVal t As Type)
            _t = t
        End Sub

        Protected Friend Sub New(ByVal t As Type, ByVal con As Orm.Condition.ConditionConstructor)
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

        Public Function AndExists(ByVal t As Type, ByVal joinFilter As IFilter) As CriteriaLink
            Return New CriteriaNonField(_con, ConditionOperator.And).Exists(t, joinFilter)
        End Function

        Public Function AndNotExists(ByVal t As Type, ByVal joinFilter As IFilter) As CriteriaLink
            Return New CriteriaNonField(_con, ConditionOperator.And).NotExists(t, joinFilter)
        End Function

        Public Function OrExists(ByVal t As Type, ByVal joinFilter As IFilter) As CriteriaLink
            Return New CriteriaNonField(_con, ConditionOperator.Or).Exists(t, joinFilter)
        End Function

        Public Function OrNotExists(ByVal t As Type, ByVal joinFilter As IFilter) As CriteriaLink
            Return New CriteriaNonField(_con, ConditionOperator.Or).NotExists(t, joinFilter)
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