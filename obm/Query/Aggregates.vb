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

    <Serializable()> _
    Public MustInherit Class AggregateBase
        Implements IQueryElement

        Private _f As AggregateFunction
        Private _alias As String
        Private _distinct As Boolean

        'Private _dontAddAlias As Boolean

        'Protected Friend Property AddAlias() As Boolean
        '    Get
        '        Return Not _dontAddAlias
        '    End Get
        '    Set(ByVal value As Boolean)
        '        _dontAddAlias = Not value
        '    End Set
        'End Property

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal [alias] As String)
            _f = agFunc
            _alias = [alias]
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction)
            MyClass.New(agFunc, String.Empty)
        End Sub

        Public MustOverride Function MakeStmt(ByVal schema As ObjectMappingEngine, _
            ByVal stmt As StmtGenerator, ByVal pmgr As Meta.ICreateParam, ByVal almgr As IPrepareTable, _
            ByVal filterInfo As Object, ByVal inSelect As Boolean) As String

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
            'If Not _dontAddAlias Then
            Return _alias
            'End If
            'Return Nothing
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

        Public MustOverride Function _ToString() As String Implements Criteria.Values.IQueryElement._ToString
        Public MustOverride Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements Criteria.Values.IQueryElement.GetStaticString
        Public MustOverride Sub Prepare(ByVal executor As IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements Criteria.Values.IQueryElement.Prepare
    End Class

    <Serializable()> _
    Public Class [Aggregate]
        Inherits AggregateBase

        Private _oper As UnaryExp

        Public Sub New(ByVal agFunc As AggregateFunction)
            MyClass.New(agFunc, New UnaryExp(New LiteralValue("*")))
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal op As ObjectProperty)
            MyClass.New(agFunc, New UnaryExp(New FieldValue(op)))
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal t As Type, ByVal propertyAlias As String)
            MyClass.New(agFunc, New UnaryExp(New FieldValue(t, propertyAlias)))
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal entityName As String, ByVal propertyAlias As String)
            MyClass.New(agFunc, New UnaryExp(New FieldValue(entityName, propertyAlias)))
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal os As EntityUnion, ByVal propertyAlias As String)
            MyClass.New(agFunc, New UnaryExp(New FieldValue(os, propertyAlias)))
        End Sub

        Public Sub New(ByVal agFunc As AggregateFunction, ByVal t As SourceFragment, ByVal column As String)
            MyClass.New(agFunc, New UnaryExp(New FieldValue(t, column)))
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

        Public Overrides Function MakeStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal pmgr As Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal filterInfo As Object, ByVal inSelect As Boolean) As String
            Dim s As String = FormatFunc(AggFunc, String.Empty)
            s = String.Format(s, _oper.MakeStmt(schema, stmt, pmgr, almgr, filterInfo, inSelect))
            If Not String.IsNullOrEmpty([Alias]) AndAlso inSelect Then
                s = s & " " & [Alias]
            End If
            Return s
        End Function

        Public ReadOnly Property Expression() As UnaryExp
            Get
                Return _oper
            End Get
        End Property

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
            Throw New NotSupportedException
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

        Public Overrides Function _ToString() As String
            Dim s As String = FormatFunc(AggFunc, String.Empty)
            s = String.Format(s, _oper._ToString)
            Return s
        End Function

        Public Overrides Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String
            Dim s As String = FormatFunc(AggFunc, String.Empty)
            s = String.Format(s, _oper.ToStaticString(mpe, contextFilter))
            Return s
        End Function

        Public Overrides Sub Prepare(ByVal executor As IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean)
            If _oper IsNot Nothing Then
                _oper.Prepare(executor, schema, filterInfo, stmt, isAnonym)
            End If
        End Sub
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

        Public Overrides Function MakeStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal pmgr As Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal filterInfo As Object, ByVal inSelect As Boolean) As String
            Throw New NotImplementedException
        End Function

        Public Overrides Function _ToString() As String
            Throw New NotImplementedException
        End Function

        Public Overrides Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String
            Throw New NotImplementedException
        End Function

        Public Overrides Sub Prepare(ByVal executor As IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean)
            Throw New NotImplementedException
        End Sub
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