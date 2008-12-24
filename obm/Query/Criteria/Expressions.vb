Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Core
Imports Worm.Criteria.Values
Imports Worm.Entities
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
        Implements IQueryElement

        Private _o As ExpOperation
        Private _v As IFilterValue
        Private _alias As String

        Public Sub New(ByVal operation As ExpOperation, ByVal value As IFilterValue)
            _o = operation
            _v = value
        End Sub

        Public Sub New(ByVal value As IFilterValue)
            _v = value
        End Sub

        Public Sub New(ByVal [alias] As String, ByVal value As IFilterValue)
            _v = value
            _alias = [alias]
        End Sub

        Public Sub New(ByVal operation As ExpOperation)
            _o = operation
        End Sub

        Public Property [Alias]() As String
            Get
                Return _alias
            End Get
            Set(ByVal value As String)
                _alias = value
            End Set
        End Property

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

        Public Overridable Function ToStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            Return FormatOper() & "$" & _v.GetStaticString(mpe)
        End Function

        Public Overrides Function ToString() As String
            Throw New NotSupportedException
        End Function

        Public Overridable Function _ToString() As String Implements IQueryElement._ToString
            Return FormatOper() & "$" & _v._ToString
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, UnaryExp))
        End Function

        Public Overloads Function Equals(ByVal obj As UnaryExp) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Return _o = obj._o AndAlso Object.Equals(_v, obj._v)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return ToString.GetHashCode()
        End Function

        Public Overridable Function MakeStmt(ByVal s As ObjectMappingEngine, ByVal stmt As StmtGenerator, _
            ByVal pmgr As Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal columns As List(Of String), _
            ByVal filterInfo As Object, ByVal inSelect As Boolean) As String
            Return FormatOper() & FormatParam(s, stmt, pmgr, almgr, columns, filterInfo, inSelect)
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

        Protected Overridable Function FormatParam(ByVal s As ObjectMappingEngine, _
            ByVal stmt As StmtGenerator, ByVal pmgr As Meta.ICreateParam, ByVal almgr As IPrepareTable, _
            ByVal columns As List(Of String), ByVal filterInfo As Object, ByVal inSelect As Boolean) As String
            Return _v.GetParam(s, stmt, pmgr, almgr, Nothing, columns, filterInfo, inSelect)
            'Dim p As IParamFilterValue = TryCast(_v, IParamFilterValue)
            'If p IsNot Nothing Then
            '    Return p.GetParam(s, pmgr, Nothing)
            'Else
            '    Dim e As EntityPropValue = TryCast(_v, EntityPropValue)
            '    If e IsNot Nothing Then
            '        Return e.GetParam(s, almgr)
            '    Else
            '        Dim rv As RefValue = TryCast(_v, RefValue)
            '        If rv IsNot Nothing Then
            '            Return rv.GetParam(columns)
            '        Else
            '            Dim cf As CustomValue = TryCast(_v, CustomValue)
            '            If cf IsNot Nothing Then
            '                Return cf.GetParam(s, almgr)
            '            Else
            '                Dim db As Database.Criteria.Values.IDatabaseFilterValue = TryCast(_v, Database.Criteria.Values.IDatabaseFilterValue)
            '                If db IsNot Nothing Then
            '                    Return db.GetParam(CType(s, Database.SQLGenerator), filterInfo, pmgr, CType(almgr, Database.AliasMgr))
            '                Else
            '                    Throw New NotSupportedException
            '                End If
            '            End If
            '        End If
            '    End If
            'End If
        End Function

        Public Shared Function CreateFilter(ByVal schema As ObjectMappingEngine, ByVal lf As UnaryExp, ByVal rf As UnaryExp, ByVal fo As FilterOperation) As IFilter
            Dim leftValue As IFilterValue = lf._v
            Dim rightValue As IFilterValue = rf._v
            If lf.GetType Is GetType(UnaryExp) AndAlso leftValue.GetType IsNot GetType(RefValue) Then
                If rf.GetType Is GetType(UnaryExp) AndAlso rightValue.GetType IsNot GetType(RefValue) Then
                    If leftValue.GetType Is GetType(FieldValue) Then
                        If GetType(IParamFilterValue).IsAssignableFrom(rightValue.GetType) Then
                            Dim lv As FieldValue = CType(leftValue, FieldValue)
                            If lv.Expression.Table IsNot Nothing Then
                                Return New ColumnPredicate(lv.Expression.Table, lv.Expression.Column).Op(fo, CType(rightValue, IParamFilterValue)).Filter
                            Else
                                Return New PropertyPredicate(lv.Expression.ObjectProperty).Op(fo, CType(rightValue, IParamFilterValue)).Filter
                            End If
                        Else
                            GoTo l1
                        End If
                    ElseIf leftValue.GetType Is GetType(CustomValue) Then
                        Dim lv As CustomValue = CType(leftValue, CustomValue)
                        If GetType(IParamFilterValue).IsAssignableFrom(rightValue.GetType) Then
                            Return New CustomFilter(lv.Format, CType(rightValue, IParamFilterValue), fo, lv.Values)
                        Else
                            GoTo l1
                        End If
                    ElseIf GetType(IParamFilterValue).IsAssignableFrom(leftValue.GetType) Then
                        If rightValue.GetType Is GetType(FieldValue) Then
                            Dim rv As FieldValue = CType(rightValue, FieldValue)
                            If rv.Expression.Table IsNot Nothing Then
                                Return New ColumnPredicate(rv.Expression.Table, rv.Expression.Column).Op(Invert(fo), CType(leftValue, IParamFilterValue)).Filter
                            Else
                                Return New PropertyPredicate(rv.Expression.ObjectProperty).Op(Invert(fo), CType(leftValue, IParamFilterValue)).Filter
                            End If
                        ElseIf rightValue.GetType Is GetType(CustomValue) Then
                            Dim rv As CustomValue = CType(rightValue, CustomValue)
                            Return New CustomFilter(rv.Format, CType(leftValue, IParamFilterValue), Invert(fo), rv.Values)
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
                Return New ExpressionFilter(lf, rf, fo)
                '               Throw New NotImplementedException
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

        Public Overrides Function MakeStmt(ByVal s As ObjectMappingEngine, ByVal stmt As StmtGenerator, _
            ByVal pmgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal columns As List(Of String), _
            ByVal filterInfo As Object, ByVal inSelect As Boolean) As String
            Return "(" & _left.MakeStmt(s, stmt, pmgr, almgr, columns, filterInfo, inSelect) & FormatOper() & _right.MakeStmt(s, stmt, pmgr, almgr, columns, filterInfo, inSelect) & ")"
        End Function

        Public Overrides Function ToStaticString(ByVal mpe As ObjectMappingEngine) As String
            Return _left.ToStaticString(mpe) & "$" & Operation.ToString & "$" & _right.ToStaticString(mpe)
        End Function

        Public Overrides Function _ToString() As String
            Return _left._ToString & "$" & Operation.ToString & "$" & _right._ToString
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, BinaryExp))
        End Function

        Public Overloads Function Equals(ByVal obj As BinaryExp) As Boolean
            If obj Is Nothing Then
                Return False
            End If

            Return Operation = obj.Operation AndAlso _left.Equals(obj._left) AndAlso _right.Equals(obj._right)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return ToString.GetHashCode
        End Function
    End Class
End Class
