Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.Query
Imports Worm.Expressions2
Imports System.Linq

Namespace Criteria.Values
    <Serializable()> _
    Public Class InValue
        Inherits ScalarValue

        Private _l As New List(Of String)
        Private _str As String

        Protected Sub New()
            MyBase.New()
        End Sub

        Public Sub New(ByVal value As IEnumerable)
            MyBase.New(value)
        End Sub

        Public Sub New(ByVal value As IEnumerable, ByVal caseSensitive As Boolean)
            MyBase.New(value, caseSensitive)
        End Sub

        Public Overrides Function _ToString() As String
            If String.IsNullOrEmpty(_str) Then
                If Value Is Nothing Then
                    Return "nothing"
                End If
                Dim l As New List(Of String)
                For Each o As Object In Value
                    l.Add(o.ToString)
                Next
                If l.Count = 0 Then
                    Return "empty"
                End If
                l.Sort()
                Dim sb As New StringBuilder
                For Each s As String In l
                    sb.Append(s).Append("$")
                Next
                _str = sb.ToString
            End If
            Return _str
        End Function

        Public Overrides Function Eval(ByVal v As Object, ByVal mpe As ObjectMappingEngine, ByVal template As OrmFilterTemplate) As IEvaluableValue.EvalResult
            Dim r As IEvaluableValue.EvalResult = IEvaluableValue.EvalResult.NotFound

            'Dim val As Object = GetValue(v, template, r)
            'If r <> IEvaluableValue.EvalResult.Unknown Then
            '    Return r
            'Else
            '    r = IEvaluableValue.EvalResult.NotFound
            'End If

            If Value Is Nothing Then
                Return IEvaluableValue.EvalResult.Found
            End If

            Select Case template.Operation
                Case FilterOperation.In
                    For Each o As Object In Value
                        If Object.Equals(o, v) Then
                            r = IEvaluableValue.EvalResult.Found
                            Exit For
                        Else
                            Dim spk = TryCast(v, ISinglePKEntity)
                            If spk IsNot Nothing Then
                                If Equals(o, spk.Identifier) Then
                                    r = IEvaluableValue.EvalResult.Found
                                    Exit For
                                End If
                            Else
                                spk = TryCast(o, ISinglePKEntity)

                                If spk IsNot Nothing Then
                                    If Equals(spk.Identifier, v) Then
                                        r = IEvaluableValue.EvalResult.Found
                                        Exit For
                                    End If
                                End If
                            End If
                        End If
                    Next
                Case FilterOperation.NotIn
                    For Each o As Object In Value
                        If Object.Equals(o, v) Then
                            r = IEvaluableValue.EvalResult.NotFound
                            Exit For
                        Else
                            Dim spk = TryCast(v, ISinglePKEntity)
                            If spk IsNot Nothing Then
                                If Equals(o, spk.Identifier) Then
                                    r = IEvaluableValue.EvalResult.NotFound
                                    Exit For
                                End If
                            Else
                                spk = TryCast(o, ISinglePKEntity)

                                If spk IsNot Nothing Then
                                    If Equals(spk.Identifier, v) Then
                                        r = IEvaluableValue.EvalResult.NotFound
                                        Exit For
                                    End If
                                End If
                            End If
                        End If
                    Next
                Case Else
                    Throw New InvalidOperationException(String.Format("Invalid operation {0} for InValue", template.OperToString))
            End Select

            Return r
        End Function

        Public Shadows ReadOnly Property Value() As IEnumerable
            Get
                Return CType(MyBase.Value, IEnumerable)
            End Get
        End Property

        Public Overrides Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, ByVal prepare As PrepareValueDelegate, _
                          ByVal contextInfo As IDictionary, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String

            Dim sb As New StringBuilder
            Dim idx As Integer = 0
            For Each o As Object In Value
                Dim v As Object = o
                If prepare IsNot Nothing Then
                    v = prepare(schema, o)
                End If

                If paramMgr Is Nothing Then
                    Throw New ArgumentNullException("paramMgr")
                End If

                Dim pname As String = Nothing
                Dim add As Boolean
                If _l.Count < idx Then
                    pname = _l(idx)
                Else
                    add = True
                End If

                pname = paramMgr.AddParam(pname, v)
                If add Then
                    _l.Add(pname)
                End If

                sb.Append(pname).Append(",")
                idx += 1
            Next
            If sb.Length = 0 Then
                sb.Append("NULL")
            Else
                sb.Length -= 1
            End If
            sb.Insert(0, "(").Append(")")
            Return sb.ToString
        End Function

        Public Overrides ReadOnly Property ShouldUse() As Boolean
            Get
                If Value IsNot Nothing Then
                    'For Each s As Object In Value
                    Return True
                    'Next
                End If
                Return False
            End Get
        End Property

        Protected Overrides Function _Clone() As Object
            Return Clone()
        End Function

        Public Overloads Function Clone() As InValue
            Dim n As New InValue()
            _CopyTo(n)
            Return n
        End Function

        Protected Overrides Function _CopyTo(target As ICopyable) As Boolean
            Return CopyTo(TryCast(target, InValue))
        End Function

        Public Overloads Function CopyTo(target As InValue) As Boolean
            If MyBase._CopyTo(target) Then
                If target Is Nothing Then
                    Return False
                End If

                If Value IsNot Nothing Then
                    Dim l As New ArrayList()
                    For Each o In Value
                        l.Add(o)
                    Next
                    target.SetValue(l)
                End If

                Return True
            End If

            Return False
        End Function
    End Class
End Namespace