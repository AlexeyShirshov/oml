Imports System.Collections.Generic
Imports System.Linq
Imports System.Net.Mime
Imports System.Web.Mail
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query

Namespace Criteria.Values
    <Serializable()>
    Public Class CachedEntityValue
        Implements IFilterMultiValue
        Private _t As Type
        Private _params As IEnumerable(Of IEvaluableValue)

        Public Sub New(ByVal o As ICachedEntity, Optional ByVal caseSensitive As Boolean = False)
            If o IsNot Nothing Then
                _t = o.GetType
                Dim l As New List(Of IEvaluableValue)
                For Each pk In o.GetPKs()
                    l.Add(New ScalarValue(pk.Value, caseSensitive))
                Next
                _params = l
            Else
                _params = {}
                _t = GetType(ICachedEntity)
            End If
        End Sub
        Protected Sub New(params As IEnumerable(Of IEvaluableValue))
            _params = params
        End Sub
        Friend Sub New(params As IEnumerable(Of IEvaluableValue), t As Type)
            _params = params
            _t = t
        End Sub
        Public ReadOnly Property Params As IEnumerable(Of IEvaluableValue) Implements IFilterMultiValue.Params
            Get
                Return _params
            End Get
        End Property

        Public ReadOnly Property ShouldUse As Boolean Implements IFilterValue.ShouldUse
            Get
                Return True
            End Get
        End Property

        Public Sub Prepare(executor As IExecutor, schema As ObjectMappingEngine, contextInfo As IDictionary, stmt As StmtGenerator, isAnonym As Boolean) Implements IQueryElement.Prepare
            For Each p In _params
                p.Prepare(executor, schema, contextInfo, stmt, isAnonym)
            Next
        End Sub

        Public Function Eval(v As Object, mpe As ObjectMappingEngine, template As OrmFilterTemplate) As IEvaluableValue.EvalResult Implements IEvaluableValue.Eval
            For Each p In _params
                Dim r = p.Eval(v, mpe, template)

                Select Case r
                    Case IEvaluableValue.EvalResult.Unknown
                        Return IEvaluableValue.EvalResult.Unknown
                    Case IEvaluableValue.EvalResult.NotFound
                        Return IEvaluableValue.EvalResult.NotFound
                End Select
            Next

            Return IEvaluableValue.EvalResult.Found
        End Function

        Public Function GetStaticString(mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            Return "cachedentityvalue"
        End Function

        Public Function _ToString() As String Implements IQueryElement._ToString
            Dim sb As New StringBuilder
            For Each p In _params
                sb.Append(p._ToString).Append(",")
            Next
            If sb.Length > 0 Then
                sb.Length -= 1
            End If

            Return sb.ToString
        End Function

        Protected Overridable Function _Clone() As Object Implements ICloneable.Clone
            Return Clone()
        End Function

        Public Function Clone() As CachedEntityValue
            Return New CachedEntityValue(_params)
        End Function

        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, CachedEntityValue))
        End Function

        Public Function CopyTo(target As CachedEntityValue) As Boolean
            If target Is Nothing Then
                Return False
            End If

            target._params = _params
            target._t = _t

            Return True
        End Function
        Public ReadOnly Property OrmType() As Type
            Get
                Return _t
            End Get
        End Property
        Public ReadOnly Property Value As Object Implements IEvaluableValue.Value
            Get
                If _params.Count = 1 Then
                    Return _params.First.Value
                End If

                Throw New InvalidOperationException
            End Get
        End Property
        Public Function GetParam(schema As ObjectMappingEngine, fromClause As QueryCmd.FromClauseDef, stmt As StmtGenerator, paramMgr As ICreateParam, almgr As IPrepareTable, prepare As PrepareValueDelegate, contextInfo As IDictionary, inSelect As Boolean, executor As IExecutionContext) As String Implements IFilterValue.GetParam
            If _params.Count = 1 Then
                Return _params.First.GetParam(schema, fromClause, stmt, paramMgr, almgr, prepare, contextInfo, inSelect, executor)
            End If

            Throw New InvalidOperationException
        End Function

    End Class

End Namespace