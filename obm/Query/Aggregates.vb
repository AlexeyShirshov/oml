Imports Worm.Entities
Imports System.Collections.Generic
Imports Worm.Criteria.Values
Imports Worm.Entities.Meta
Imports Worm.Expressions

Namespace Query

    Public Enum AggregateFunction
        Max
        Min
        Average
        Count
        BigCount
        Sum
        Custom
    End Enum

    Public MustInherit Class AggregateBase
        Private _f As AggregateFunction
        Private _alias As String
        Private _distinct As Boolean

        Private _dontAddAlias As Boolean

        Protected Friend Property AddAlias() As Boolean
            Get
                Return Not _dontAddAlias
            End Get
            Set(ByVal value As Boolean)
                _dontAddAlias = Not value
            End Set
        End Property

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal [alias] As String)
            _f = agFunc
            _alias = [alias]
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction)
            MyClass.New(agFunc, String.Empty)
        End Sub

        Public MustOverride Function MakeStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal columnAliases As List(Of String), ByVal pmgr As Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal filterInfo As Object, ByVal inSelect As Boolean) As String

        Public ReadOnly Property AggFunc() As AggregateFunction
            Get
                Return _f
            End Get
        End Property

        Public Property [Alias]() As String
            Get
                Return _alias
            End Get
            Set(ByVal value As String)
                _alias = value
            End Set
        End Property

        Public Property Distinct() As Boolean
            Get
                Return _distinct
            End Get
            Set(ByVal value As Boolean)
                _distinct = value
            End Set
        End Property

        Public Function GetAlias() As String
            If Not _dontAddAlias Then
                Return _alias
            End If
            Return Nothing
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, AggregateBase))
        End Function

        Public Overloads Function Equals(ByVal obj As AggregateBase) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Return _alias = obj._alias AndAlso _f = obj._f
        End Function

    End Class

    'Public Class [Aggregate]
    '    Inherits AggregateBase

    '    Private _prop As OrmProperty
    '    Private _col As Integer

    '    Public Sub New(ByVal agFunc As AggregateFunction, ByVal columnAlias As String, ByVal t As Type, ByVal field As String)
    '        MyBase.New(agFunc, columnAlias)
    '        _prop = New OrmProperty(t, field)
    '    End Sub

    '    Public Sub New(ByVal agFunc As AggregateFunction, ByVal columnAlias As String, ByVal table As SourceFragment, ByVal column As String)
    '        MyBase.New(agFunc, columnAlias)
    '        _prop = New OrmProperty(table, column)
    '    End Sub

    '    Public Sub New(ByVal agFunc As AggregateFunction, ByVal t As Type, ByVal field As String)
    '        MyBase.New(agFunc)
    '        _prop = New OrmProperty(t, field)
    '    End Sub

    '    Public Sub New(ByVal agFunc As AggregateFunction, ByVal column As String)
    '        MyBase.New(agFunc)
    '        _prop = New OrmProperty(CType(Nothing, Type), Nothing)
    '        _prop.Column = column
    '    End Sub

    '    Public Sub New(ByVal agFunc As AggregateFunction, ByVal columnNum As Integer)
    '        MyBase.New(agFunc)
    '        _col = columnNum
    '    End Sub

    '    Public Sub New(ByVal agFunc As AggregateFunction)
    '        MyBase.New(agFunc)
    '        _prop = New OrmProperty(CType(Nothing, Type), Nothing)
    '    End Sub

    '    Public Sub New(ByVal agFunc As AggregateFunction, ByVal table As SourceFragment, ByVal column As String)
    '        MyBase.New(agFunc)
    '        _prop = New OrmProperty(table, column)
    '    End Sub

    '    Public Overrides Function MakeStmt(ByVal t As Type, ByVal schema As QueryGenerator, ByVal columnAliases As List(Of String), ByVal pmgr As Meta.ICreateParam, ByVal almgr As IPrepareTable) As String
    '        Dim s As String = Nothing
    '        If _prop IsNot Nothing Then
    '            s = GetColumn(t, schema)
    '        Else
    '            s = columnAliases(_col)
    '        End If
    '        Return String.Format(GetFunc, s)
    '    End Function

    '    Protected Function GetFunc() As String
    '        Dim s As String = Nothing
    '        Dim d As String = String.Empty
    '        If Distinct Then
    '            d = "distinct "
    '        End If
    '        s = FormatFunc(AggFunc, d)
    '        If Not String.IsNullOrEmpty([Alias]) AndAlso AddAlias Then
    '            s = s & " " & [Alias]
    '        End If
    '        Return s
    '    End Function

    '    Protected Function GetColumn(ByVal t As Type, ByVal schema As QueryGenerator) As String
    '        If _prop.Table IsNot Nothing Then
    '            Return schema.GetTableName(_prop.Table) & schema.Selector & _prop.Column
    '        ElseIf Not String.IsNullOrEmpty(_prop.Field) Then
    '            Dim tt As Type = _prop.Type
    '            If tt Is Nothing Then tt = t

    '            Return schema.GetColumnNameByFieldNameInternal(tt, _prop.Field, True)
    '        ElseIf Not String.IsNullOrEmpty(_prop.Column) Then
    '            Return _prop.Column
    '        Else
    '            Return "*"
    '        End If
    '    End Function

    '    Public ReadOnly Property Prop() As OrmProperty
    '        Get
    '            Return _prop
    '        End Get
    '    End Property



    'End Class

    Public Class [Aggregate]
        Inherits AggregateBase

        Private _oper As UnaryExp

        Public Sub New(ByVal agFunc As AggregateFunction)
            MyClass.New(agFunc, New UnaryExp(New LiteralValue("*")))
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal num As Integer)
            MyClass.New(agFunc, New UnaryExp(New RefValue(num)))
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal t As Type, ByVal field As String)
            MyClass.New(agFunc, New UnaryExp(New EntityPropValue(t, field)))
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal [alias] As String)
            MyClass.New(agFunc, [alias], New UnaryExp(New LiteralValue("*")))
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal operation As UnaryExp)
            MyClass.New(agFunc, Nothing, operation)
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal [alias] As String, _
                       ByVal operation As UnaryExp)
            MyBase.New(agFunc, [alias])
            _oper = operation
        End Sub

        Public Overrides Function MakeStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal columnAliases As List(Of String), ByVal pmgr As Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal filterInfo As Object, ByVal inSelect As Boolean) As String
            Dim s As String = FormatFunc(AggFunc, String.Empty)
            s = String.Format(s, _oper.MakeStmt(schema, stmt, pmgr, almgr, columnAliases, filterInfo, inSelect))
            If Not String.IsNullOrEmpty([Alias]) AndAlso AddAlias Then
                s = s & " " & [Alias]
            End If
            Return s
        End Function

        Public Shared Function FormatFunc(ByVal AggFunc As AggregateFunction, ByVal d As String) As String
            Dim s As String = Nothing
            Select Case AggFunc
                Case AggregateFunction.Max
                    s = "max(" & d & "{0})"
                Case AggregateFunction.Min
                    s = "min(" & d & "{0})"
                Case AggregateFunction.Average
                    s = "avg(" & d & "{0})"
                Case AggregateFunction.Count
                    s = "count(" & d & "{0})"
                Case AggregateFunction.BigCount
                    s = "count_big(" & d & "{0})"
                Case AggregateFunction.Sum
                    s = "sum(" & d & "{0})"
                Case Else
                    Throw New NotImplementedException(AggFunc.ToString)
            End Select
            Return s
        End Function

        Public Overrides Function ToString() As String
            Dim s As String = FormatFunc(AggFunc, String.Empty)
            s = String.Format(s, _oper.ToString)
            Return s
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, Aggregate))
        End Function

        Public Overloads Function Equals(ByVal obj As Aggregate) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Return MyBase.Equals(obj) AndAlso _oper.Equals(obj._oper)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return ToString.GetHashCode()
        End Function
    End Class

    Public Class CustomFuncAggregate
        Inherits AggregateBase

        Private _params As List(Of ScalarValue)
        Private _funcName As String

        Public Sub New(ByVal [alias] As String, ByVal funcName As String, ByVal params As List(Of ScalarValue))
            MyBase.New(AggregateFunction.Average, [alias])
            _funcName = funcName
            _params = params
        End Sub

        Public Overrides Function MakeStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal columnAliases As List(Of String), ByVal pmgr As Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal filterInfo As Object, ByVal inSelect As Boolean) As String
            Throw New NotImplementedException
        End Function
    End Class

    'Public Class AggCtor
    '    Private _l As List(Of AggregateBase)

    '    Public Shared Function Count() As AggCtor
    '        Dim a As New AggCtor
    '        a.GetAllAggregates.Add(New Aggregate(AggregateFunction.Count))
    '        Return a
    '    End Function

    '    Public Shared Function Count(ByVal [alias] As String) As AggCtor
    '        Dim a As New AggCtor
    '        a.GetAllAggregates.Add(New Aggregate(AggregateFunction.Count, [alias]))
    '        Return a
    '    End Function

    '    Public Function GetAllAggregates() As List(Of AggregateBase)
    '        If _l Is Nothing Then
    '            _l = New List(Of AggregateBase)
    '        End If
    '        Return _l
    '    End Function

    '    Public Shared Widening Operator CType(ByVal a As AggCtor) As AggregateBase()
    '        Return a.GetAllAggregates.ToArray
    '    End Operator
    'End Class
End Namespace