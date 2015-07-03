Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.Query
Imports Worm.Expressions2
Imports System.Linq

Namespace Criteria.Values
    <Serializable()> _
    Public Class BetweenValue
        Inherits ScalarValue

        Protected Sub New()
            MyBase.New()
        End Sub
        Public Sub New(ByVal left As Object, ByVal right As Object)
            MyBase.New(New Pair(Of IFilterValue)(New ScalarValue(left), New ScalarValue(right)))

            If left Is Nothing Then
                Throw New ArgumentNullException("left")
            End If

            If right Is Nothing Then
                Throw New ArgumentNullException("right")
            End If
        End Sub

        Public Sub New(ByVal left As IFilterValue, ByVal right As IFilterValue)
            MyBase.New(New Pair(Of IFilterValue)(left, right))

            If left Is Nothing Then
                Throw New ArgumentNullException("left")
            End If

            If right Is Nothing Then
                Throw New ArgumentNullException("right")
            End If
        End Sub

        Public Overrides Function Eval(ByVal v As Object, ByVal mpe As ObjectMappingEngine, ByVal template As OrmFilterTemplate) As IEvaluableValue.EvalResult
            Dim r As IEvaluableValue.EvalResult = IEvaluableValue.EvalResult.Unknown

            Dim val As Object = GetValue(v, template, r)
            If r <> IEvaluableValue.EvalResult.Unknown Then
                Return r
            Else
                r = IEvaluableValue.EvalResult.NotFound
            End If

            If template.Operation = FilterOperation.Between Then
                Dim int_v As Pair(Of IFilterValue) = Value

                Dim fe As IEvaluableValue = TryCast(int_v.First, IEvaluableValue)
                Dim se As IEvaluableValue = TryCast(int_v.Second, IEvaluableValue)

                If fe IsNot Nothing AndAlso se IsNot Nothing Then
                    Dim i As Integer = CType(v, IComparable).CompareTo(fe.Value)
                    If i >= 0 Then
                        i = CType(v, IComparable).CompareTo(se.Value)
                        If i <= 0 Then
                            r = IEvaluableValue.EvalResult.Found
                        End If
                    End If
                End If
            Else
                Throw New InvalidOperationException(String.Format("Invalid operation {0} for BetweenValue", template.OperToString))
            End If

            Return r
        End Function

        Public Overrides Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, ByVal prepare As PrepareValueDelegate, _
                          ByVal contextInfo As IDictionary, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String

            If paramMgr Is Nothing Then
                Throw New ArgumentNullException("paramMgr")
            End If

            Dim left As IFilterValue = Value.First, right As IFilterValue = Value.Second
            'If prepare IsNot Nothing Then
            '    left = prepare(schema, left)
            '    right = prepare(schema, right)
            'End If

            '_l = paramMgr.AddParam(_l, left)
            '_r = paramMgr.AddParam(_r, right)

            'Return _l & " and " & _r

            Return left.GetParam(schema, fromClause, stmt, paramMgr, almgr, prepare, contextInfo, inSelect, executor) & _
                " and " & _
                right.GetParam(schema, fromClause, stmt, paramMgr, almgr, prepare, contextInfo, inSelect, executor)
        End Function

        Public Overrides Function _ToString() As String
            Return Value.First._ToString & "__$__" & Value.Second._ToString
        End Function

        Public Overrides Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String
            Return Value.First.GetStaticString(mpe) & "between" & Value.Second.GetStaticString(mpe)
        End Function

        Public Overrides Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean)
            Value.First.Prepare(executor, schema, contextInfo, stmt, isAnonym)
            Value.Second.Prepare(executor, schema, contextInfo, stmt, isAnonym)
        End Sub

        Public Shadows ReadOnly Property Value() As Pair(Of IFilterValue)
            Get
                Return CType(MyBase.Value, Pair(Of IFilterValue))
            End Get
        End Property

        Protected Overrides Function _Clone() As Object
            Return Clone()
        End Function

        Public Overloads Function Clone() As BetweenValue
            Dim n As New BetweenValue()
            _CopyTo(n)
            Return n
        End Function

        Protected Overrides Function _CopyTo(target As ICopyable) As Boolean
            Return CopyTo(TryCast(target, BetweenValue))
        End Function

        Public Overloads Function CopyTo(target As BetweenValue) As Boolean
            If target Is Nothing Then
                Return False
            End If

            If Value IsNot Nothing Then
                Dim lc As IFilterValue = Nothing
                Dim rc As IFilterValue = Nothing

                If Value.First IsNot Nothing Then
                    lc = CType(Value.First.Clone, IFilterValue)
                End If

                If Value.Second IsNot Nothing Then
                    rc = CType(Value.Second.Clone, IFilterValue)
                End If

                Dim p As New Pair(Of IFilterValue)(lc, rc)

                target.SetValue(p)

            End If

            Return True
        End Function
    End Class
End Namespace