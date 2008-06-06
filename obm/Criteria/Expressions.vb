Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Core
Imports Worm.Criteria.Values
Imports Worm.Orm
Imports Worm.Criteria.Joins
Imports Worm.Criteria
Imports System.Collections.Generic

Public Class Expressions
    Public Enum ExpOperation
        None
        Neg
        Add
        [Sub]
        Mul
        Div
        [Mod]
    End Enum

    Public Class UnaryExp
        Private _o As ExpOperation
        Private _v As IFilterValue

        Public Sub New(ByVal operation As ExpOperation, ByVal value As IFilterValue)
            _o = operation
            _v = value
        End Sub

        Public Sub New(ByVal value As IFilterValue)
            _v = value
        End Sub

        Public Sub New(ByVal operation As ExpOperation)
            _o = operation
        End Sub

        Public ReadOnly Property Operation() As ExpOperation
            Get
                Return _o
            End Get
        End Property

        Public ReadOnly Property Value() As IFilterValue
            Get
                Return _v
            End Get
        End Property

        Public Overridable Function MakeStmt(ByVal s As QueryGenerator, ByVal pmgr As Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal columns As List(Of String)) As String
            Return FormatOper() & FormatParam(s, pmgr, almgr, columns)
        End Function

        Protected Function FormatOper() As String
            Select Case _o
                Case ExpOperation.Add
                    Return "+"
                Case ExpOperation.Div
                    Return "/"
                Case ExpOperation.Mod
                    Return " mod "
                Case ExpOperation.Mul
                    Return "*"
                Case ExpOperation.Neg
                    Return "!"
                Case ExpOperation.Sub
                    Return "-"
                Case ExpOperation.None
                    Return String.Empty
                Case Else
                    Throw New NotImplementedException
            End Select
        End Function

        Protected Overridable Function FormatParam(ByVal s As QueryGenerator, ByVal pmgr As Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal columns As list(Of String)) As String
            Dim p As IParamFilterValue = TryCast(_v, IParamFilterValue)
            If p IsNot Nothing Then
                Return p.GetParam(s, pmgr, Nothing)
            Else
                Dim e As EntityPropValue = TryCast(_v, EntityPropValue)
                If e IsNot Nothing Then
                    Return e.GetParam(s, almgr)
                Else
                    Dim rv As RefValue = TryCast(_v, RefValue)
                    If rv IsNot Nothing Then
                        Return rv.GetParam(columns)
                    Else
                        Dim cf As CustomValue = TryCast(_v, CustomValue)
                        If cf IsNot Nothing Then
                            Return cf.GetParam(s, almgr)
                        Else
                            Throw New NotSupportedException
                        End If
                    End If
                End If
            End If
        End Function

        Public Shared Function CreateFilter(ByVal lf As UnaryExp, ByVal rf As UnaryExp, ByVal fo As FilterOperation) As IFilter
            Dim leftValue As IFilterValue = lf._v
            Dim rightValue As IFilterValue = rf._v
            If lf.GetType Is GetType(UnaryExp) AndAlso leftValue.GetType IsNot GetType(RefValue) Then
                If rf.GetType Is GetType(UnaryExp) AndAlso rightValue.GetType IsNot GetType(RefValue) Then
                    If leftValue.GetType Is GetType(EntityPropValue) Then
                        If GetType(IParamFilterValue).IsAssignableFrom(rightValue.GetType) Then

                        Else
                            GoTo l1
                        End If
                    ElseIf leftValue.GetType Is GetType(CustomValue) Then
                        If GetType(IParamFilterValue).IsAssignableFrom(rightValue.GetType) Then

                        Else
                            GoTo l1
                        End If
                    ElseIf GetType(IParamFilterValue).IsAssignableFrom(leftValue.GetType) Then
                        If rightValue.GetType Is GetType(EntityPropValue) Then

                        ElseIf rightValue.GetType Is GetType(CustomValue) Then

                        Else
                            GoTo l1
                        End If
                    Else
                        GoTo l1
                    End If
                Else
                    GoTo l1
                End If
            Else
l1:
            End If
        End Function
    End Class

    Public Class BinaryExp
        Inherits UnaryExp

        Private _left As UnaryExp
        Private _right As UnaryExp

        Public Sub New(ByVal operation As ExpOperation, ByVal left As IFilterValue, ByVal right As IFilterValue)
            MyClass.New(operation, New UnaryExp(left), New UnaryExp(right))
        End Sub

        Public Sub New(ByVal operation As ExpOperation, ByVal left As UnaryExp, ByVal right As UnaryExp)
            MyBase.New(operation)
            _left = left
            _right = right
        End Sub

        Public ReadOnly Property Left() As UnaryExp
            Get
                Return _left
            End Get
        End Property

        Public ReadOnly Property Right() As UnaryExp
            Get
                Return _right
            End Get
        End Property

        Public Overrides Function MakeStmt(ByVal s As QueryGenerator, ByVal pmgr As Orm.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal columns As List(Of String)) As String
            Return "(" & _left.MakeStmt(s, pmgr, almgr, columns) & FormatOper() & _right.MakeStmt(s, pmgr, almgr, columns) & ")"
        End Function
    End Class
End Class
