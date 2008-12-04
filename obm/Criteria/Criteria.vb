Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Core
Imports Worm.Criteria.Values
Imports Worm.Orm
Imports Worm.Criteria.Joins
Imports Worm.Criteria
Imports System.Collections.Generic

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
        Function Field(ByVal propertyAlias As String) As CriteriaField
        Function Column(ByVal columnName As String) As CriteriaColumn
    End Interface

    Public Structure ObjectProperty
        Public ReadOnly ObjectSource As ObjectSource
        Public ReadOnly Field As String

        Public Sub New(ByVal entityName As String, ByVal propertyAlias As String)
            Me.ObjectSource = New ObjectSource(entityName)
            Me.Field = propertyAlias
        End Sub

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String)
            Me.ObjectSource = New ObjectSource(t)
            Me.Field = propertyAlias
        End Sub

        Public Sub New(ByVal [alias] As ObjectAlias, ByVal propertyAlias As String)
            Me.ObjectSource = New ObjectSource([alias])
            Me.Field = propertyAlias
        End Sub
    End Structure

    'Public Interface IFullCtor
    '    Function Field(ByVal t As Type, ByVal fieldName As String) As CriteriaField
    '    Function Field(ByVal entityName As String, ByVal fieldName As String) As CriteriaField
    '    Function ColumnOnInlineTable(ByVal columnName As String) As CriteriaColumn
    '    Function Column(ByVal table As Orm.Meta.SourceFragment, ByVal columnName As String) As CriteriaColumn
    '    Function AutoTypeField(ByVal fieldName As String) As CriteriaField
    '    Function Exists(ByVal t As Type, ByVal gf As IGetFilter) As CriteriaLink
    '    Function NotExists(ByVal t As Type, ByVal gf As IGetFilter) As CriteriaLink
    '    Function Exists(ByVal t As Type, ByVal gf As IGetFilter, ByVal joins() As OrmJoin) As CriteriaLink
    '    Function NotExists(ByVal t As Type, ByVal gf As IGetFilter, ByVal joins() As OrmJoin) As CriteriaLink
    '    Function Exists(ByVal cmd As Query.QueryCmd) As CriteriaLink
    '    Function NotExists(ByVal cmd As Query.QueryCmd) As CriteriaLink
    '    Function Custom(ByVal format As String, ByVal ParamArray values() As Pair(Of Object, String)) As CriteriaBase
    'End Interface

    Public MustInherit Class CriteriaBase
        Private _con As Condition.ConditionConstructorBase
        Private _ct As ConditionOperator

        Public Sub New()
        End Sub

        Public Sub New(ByVal con As Condition.ConditionConstructorBase, ByVal ct As ConditionOperator)
            _con = con
            _ct = ct
        End Sub

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

        Public Function Eq(ByVal value As IParamFilterValue) As CriteriaLink
            If value Is Nothing OrElse value Is DBNull.Value Then
                Return IsNull()
            Else
                Return GetLink(CreateFilter(value, FilterOperation.Equal))
            End If
        End Function

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

        Public Function Eq(ByVal value As IOrmBase) As CriteriaLink
            If value Is Nothing Then
                Return IsNull()
            Else
                Return GetLink(CreateFilter(New EntityValue(value), FilterOperation.Equal))
            End If
        End Function

        Public Function NotEq(ByVal value As IOrmBase) As CriteriaLink
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

    Public MustInherit Class CriteriaField
        Inherits CriteriaBase

        Private _os As ObjectSource
        Private _f As String

        Protected Friend Sub New(ByVal t As Type, ByVal propertyAlias As String)
            'If t Is Nothing Then
            '    Throw New ArgumentNullException("t")
            'End If

            'If String.IsNullOrEmpty(fieldName) Then
            '    Throw New ArgumentNullException("fieldName")
            'End If

            _os = New ObjectSource(t)
            _f = propertyAlias
        End Sub

        Protected Friend Sub New(ByVal t As Type, ByVal propertyAlias As String, _
            ByVal con As Condition.ConditionConstructorBase, ByVal ct As ConditionOperator)
            MyBase.New(con, ct)
            _os = New ObjectSource(t)
            _f = propertyAlias
        End Sub

        Protected Friend Sub New(ByVal os As ObjectSource, ByVal propertyAlias As String)
            _os = os
            _f = propertyAlias
        End Sub

        Protected Friend Sub New(ByVal os As ObjectSource, ByVal propertyAlias As String, _
            ByVal con As Condition.ConditionConstructorBase, ByVal ct As ConditionOperator)
            MyBase.New(con, ct)
            _os = os
            _f = propertyAlias
        End Sub

        Protected Friend Sub New(ByVal en As String, ByVal propertyAlias As String)
            _os = New ObjectSource(en)
            _f = propertyAlias
        End Sub

        Protected Friend Sub New(ByVal en As String, ByVal propertyAlias As String, _
            ByVal con As Condition.ConditionConstructorBase, ByVal ct As ConditionOperator)
            MyBase.New(con, ct)
            _os = New ObjectSource(en)
            _f = propertyAlias
        End Sub

        Protected ReadOnly Property ObjectSource() As ObjectSource
            Get
                Return _os
            End Get
        End Property

        'Protected ReadOnly Property EntityName() As String
        '    Get
        '        Return _en
        '    End Get
        'End Property

        'Protected ReadOnly Property Type() As Type
        '    Get
        '        Return _t
        '    End Get
        'End Property

        Protected ReadOnly Property Field() As String
            Get
                Return _f
            End Get
        End Property
    End Class

    Public MustInherit Class CriteriaColumn
        Inherits CriteriaBase

        Private _tbl As Meta.SourceFragment
        Private _col As String

        Protected Friend Sub New(ByVal table As Meta.SourceFragment, ByVal column As String)
            'If t Is Nothing Then
            '    Throw New ArgumentNullException("t")
            'End If

            'If String.IsNullOrEmpty(fieldName) Then
            '    Throw New ArgumentNullException("fieldName")
            'End If

            _tbl = table
            _col = column
        End Sub

        Protected Friend Sub New(ByVal table As Meta.SourceFragment, ByVal column As String, _
            ByVal con As Condition.ConditionConstructorBase, ByVal ct As ConditionOperator)
            MyBase.New(con, ct)
            _tbl = table
            _col = column
        End Sub

        Protected ReadOnly Property Table() As Meta.SourceFragment
            Get
                Return _tbl
            End Get
        End Property

        Protected ReadOnly Property Column() As String
            Get
                Return _col
            End Get
        End Property
    End Class

    Public MustInherit Class CriteriaLink
        Implements IGetFilter, ICloneable

        Private _con As Condition.ConditionConstructorBase
        Private _tbl As Meta.SourceFragment
        Private _os As ObjectSource

        Protected Friend Sub New(ByVal con As Condition.ConditionConstructorBase)
            _con = con
        End Sub

        Public Sub New()

        End Sub

        Public Sub New(ByVal t As Type)
            _os = New ObjectSource(t)
        End Sub

        Protected Friend Sub New(ByVal t As Type, ByVal con As Condition.ConditionConstructorBase)
            _con = con
            _os = New ObjectSource(t)
        End Sub

        Public Sub New(ByVal table As Meta.SourceFragment)
            _tbl = table
        End Sub

        Protected Friend Sub New(ByVal table As Meta.SourceFragment, ByVal con As Condition.ConditionConstructorBase)
            _con = con
            _tbl = table
        End Sub

        Public Sub New(ByVal entityName As String)
            _os = New ObjectSource(entityName)
        End Sub

        Protected Friend Sub New(ByVal entityName As String, ByVal con As Condition.ConditionConstructorBase)
            _con = con
            _os = New ObjectSource(entityName)
        End Sub

        Protected Friend Sub New(ByVal os As ObjectSource, ByVal con As Condition.ConditionConstructorBase)
            _con = con
            _os = os
        End Sub

        Protected Function CreateField(ByVal entityName As String, ByVal propertyAlias As String, ByVal con As Condition.ConditionConstructorBase, ByVal oper As ConditionOperator) As CriteriaField
            Return CreateField(New ObjectSource(entityName), propertyAlias, con, oper)
        End Function

        Protected Function CreateField(ByVal t As Type, ByVal propertyAlias As String, ByVal con As Condition.ConditionConstructorBase, ByVal oper As ConditionOperator) As CriteriaField
            Return CreateField(New ObjectSource(t), propertyAlias, con, oper)
        End Function

        Protected MustOverride Function _Clone() As Object Implements System.ICloneable.Clone
        'Protected MustOverride Function CreateField(ByVal entityName As String, ByVal fieldName As String, ByVal con As Condition.ConditionConstructorBase, ByVal oper As ConditionOperator) As CriteriaField
        Protected MustOverride Function CreateField(ByVal os As ObjectSource, ByVal propertyAlias As String, ByVal con As Condition.ConditionConstructorBase, ByVal oper As ConditionOperator) As CriteriaField
        Protected MustOverride Function CreateColumn(ByVal table As Meta.SourceFragment, ByVal columnName As String, ByVal con As Condition.ConditionConstructorBase, ByVal oper As ConditionOperator) As CriteriaColumn
        Protected MustOverride Function CreateCtor() As Condition.ConditionConstructorBase

        Public Function [And](ByVal t As Type, ByVal propertyAlias As String) As CriteriaField
            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("fieldName")
            End If

            If t Is Nothing Then
                Throw New ArgumentNullException("t")
            End If

            Return CreateField(t, propertyAlias, _con, ConditionOperator.And)
        End Function

        Public Function [And](ByVal os As ObjectSource, ByVal propertyAlias As String) As CriteriaField
            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("fieldName")
            End If

            If os Is Nothing Then
                Throw New ArgumentNullException("os")
            End If

            Return CreateField(os, propertyAlias, _con, ConditionOperator.And)
        End Function

        Public Function [And](ByVal objField As ObjectProperty) As CriteriaField
            Return [And](objField.ObjectSource, objField.Field)
        End Function

        Public Function [And](ByVal entityName As String, ByVal propertyAlias As String) As CriteriaField
            If String.IsNullOrEmpty(entityName) Then
                Throw New ArgumentNullException("entityName")
            End If

            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("propertyAlias")
            End If

            Return CreateField(entityName, propertyAlias, _con, ConditionOperator.And)
        End Function

        Public Function [Or](ByVal t As Type, ByVal propertyAlias As String) As CriteriaField
            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("propertyAlias")
            End If

            If t Is Nothing Then
                Throw New ArgumentNullException("t")
            End If

            Return CreateField(t, propertyAlias, _con, ConditionOperator.Or)
        End Function

        Public Function [Or](ByVal os As ObjectSource, ByVal propertyAlias As String) As CriteriaField
            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("propertyAlias")
            End If

            If os Is Nothing Then
                Throw New ArgumentNullException("os")
            End If

            Return CreateField(os, propertyAlias, _con, ConditionOperator.Or)
        End Function

        Public Function [Or](ByVal objField As ObjectProperty) As CriteriaField
            Return [Or](objField.ObjectSource, objField.Field)
        End Function

        Public Function [Or](ByVal entityName As String, ByVal propertyAlias As String) As CriteriaField
            If String.IsNullOrEmpty(entityName) Then
                Throw New ArgumentNullException("entityName")
            End If

            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("fieldName")
            End If

            Return CreateField(entityName, propertyAlias, _con, ConditionOperator.Or)
        End Function

        Public Function [And](ByVal table As Meta.SourceFragment, ByVal columnName As String) As CriteriaColumn
            If String.IsNullOrEmpty(columnName) Then
                Throw New ArgumentNullException("columnName")
            End If

            If table Is Nothing Then
                Throw New ArgumentNullException("table")
            End If

            Return CreateColumn(table, columnName, _con, ConditionOperator.And)
        End Function

        Public Function [Or](ByVal table As Meta.SourceFragment, ByVal columnName As String) As CriteriaColumn
            If String.IsNullOrEmpty(columnName) Then
                Throw New ArgumentNullException("columnName")
            End If

            If table Is Nothing Then
                Throw New ArgumentNullException("table")
            End If

            Return CreateColumn(table, columnName, _con, ConditionOperator.Or)
        End Function

        Public Function [And](ByVal propertyAlias As String) As CriteriaBase
            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("propertyAlias")
            End If

            If _tbl IsNot Nothing Then
                Return CreateColumn(_tbl, propertyAlias, _con, ConditionOperator.And)
            Else
                Return CreateField(_os, propertyAlias, _con, ConditionOperator.And)
            End If
        End Function

        Public Function [Or](ByVal propertyAlias As String) As CriteriaBase
            If String.IsNullOrEmpty(propertyAlias) Then
                Throw New ArgumentNullException("propertyAlias")
            End If

            If _tbl IsNot Nothing Then
                Return CreateColumn(_tbl, propertyAlias, _con, ConditionOperator.Or)
            Else
                Return CreateField(_os, propertyAlias, _con, ConditionOperator.Or)
            End If
        End Function

        Public Function [And](ByVal link As CriteriaLink) As CriteriaLink
            If link IsNot Nothing Then
                _con.AddFilter(link._con.Condition, ConditionOperator.And)
            End If

            Return Me
        End Function

        Public Function [And](ByVal f As IGetFilter) As CriteriaLink
            If f IsNot Nothing Then
                If _con Is Nothing Then
                    _con = CreateCtor()
                End If
                _con.AddFilter(f.Filter, ConditionOperator.And)
            End If
            Return Me
        End Function

        Public Function [Or](ByVal f As IGetFilter) As CriteriaLink
            If f IsNot Nothing Then
                If _con Is Nothing Then
                    _con = CreateCtor()
                End If
                _con.AddFilter(f.Filter, ConditionOperator.Or)
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

        'Public Overridable ReadOnly Property Filter(ByVal t As Type) As IFilter Implements IGetFilter.Filter
        '    Get
        '        If _con IsNot Nothing Then
        '            Dim f As IFilter = _con.Condition
        '            Dim ef As IEntityFilter = TryCast(f, IEntityFilter)
        '            If ef IsNot Nothing Then
        '                ef.GetFilterTemplate.SetType(New ObjectAlias(t))
        '            End If
        '            Return f
        '        Else
        '            Return Nothing
        '        End If
        '    End Get
        'End Property

        Protected ReadOnly Property ConditionCtor() As Condition.ConditionConstructorBase
            Get
                Return _con
            End Get
        End Property

        Protected ReadOnly Property ObjectSource() As ObjectSource
            Get
                Return _os
            End Get
        End Property

        'Protected ReadOnly Property Type() As Type
        '    Get
        '        Return _t
        '    End Get
        'End Property

        Protected ReadOnly Property Table() As Meta.SourceFragment
            Get
                Return _tbl
            End Get
        End Property

        Public Function Clone() As CriteriaLink
            Return CType(_Clone(), CriteriaLink)
        End Function
    End Class

    Module FilterHlp
        Public Function Invert(ByVal fo As FilterOperation) As FilterOperation
            Select Case fo
                Case FilterOperation.GreaterEqualThan
                    Return FilterOperation.LessEqualThan
                Case FilterOperation.GreaterThan
                    Return FilterOperation.LessThan
                Case FilterOperation.LessThan
                    Return FilterOperation.GreaterThan
                Case FilterOperation.LessEqualThan
                    Return FilterOperation.GreaterEqualThan
                Case Else
                    Return fo
            End Select
        End Function
    End Module

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
