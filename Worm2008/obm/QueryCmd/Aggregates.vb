Imports Worm.Orm
Imports System.Collections.Generic
Imports Worm.Criteria.Values
Imports Worm.Orm.Meta

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

        Public MustOverride Function MakeStmt(ByVal t As Type, ByVal schema As QueryGenerator) As String

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
    End Class

    Public Class [Aggregate]
        Inherits AggregateBase

        Private _prop As OrmProperty

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal columnAlias As String, ByVal t As Type, ByVal field As String)
            MyBase.New(agFunc, columnAlias)
            _prop = New OrmProperty(t, field)
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal columnAlias As String, ByVal table As SourceFragment, ByVal column As String)
            MyBase.New(agFunc, columnAlias)
            _prop = New OrmProperty(table, column)
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal t As Type, ByVal field As String)
            MyBase.New(agFunc)
            _prop = New OrmProperty(t, field)
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal field As String)
            MyBase.New(agFunc)
            _prop = New OrmProperty(CType(Nothing, Type), Nothing)
            _prop.Field = field
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction)
            MyBase.New(agFunc)
            _prop = New OrmProperty(CType(Nothing, Type), Nothing)
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal table As SourceFragment, ByVal column As String)
            MyBase.New(agFunc)
            _prop = New OrmProperty(table, column)
        End Sub

        Public Overrides Function MakeStmt(ByVal t As Type, ByVal schema As QueryGenerator) As String
            Return String.Format(GetFunc, GetColumn(t, schema))
        End Function

        Protected Function GetFunc() As String
            Dim s As String = Nothing
            Dim d As String = String.Empty
            If Distinct Then
                d = "distinct "
            End If
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
            If Not String.IsNullOrEmpty([Alias]) AndAlso AddAlias Then
                s = s & " " & [Alias]
            End If
            Return s
        End Function

        Protected Function GetColumn(ByVal t As Type, ByVal schema As QueryGenerator) As String
            If _prop.Table IsNot Nothing Then
                Return schema.GetTableName(_prop.Table) & "." & _prop.Column
            ElseIf Not String.IsNullOrEmpty(_prop.Field) Then
                Dim tt As Type = _prop.Type
                If tt Is Nothing Then tt = t
                Return schema.GetColumnNameByFieldNameInternal(tt, _prop.Field)
            Else
                Return "*"
            End If
        End Function
    End Class

    Public Class CustomAggregate
        Inherits AggregateBase

        Private _params As List(Of ScalarValue)
        Private _funcName As String

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal [alias] As String, ByVal funcName As String, ByVal params As List(Of ScalarValue))
            MyBase.New(agFunc, [alias])
            _funcName = funcName
            _params = params
        End Sub

        Public Overrides Function MakeStmt(ByVal t As Type, ByVal schema As QueryGenerator) As String

        End Function
    End Class
End Namespace