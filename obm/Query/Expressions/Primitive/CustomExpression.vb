Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query
Imports Worm.Cache

Namespace Expressions2

    <Serializable()> _
    Public Class CustomExpression
        Inherits ExpressionsArray

        Private _f As String

        Public Sub New(ByVal format As String)
            MyBase.New()
            _f = format
        End Sub

        Public Sub New(ByVal format As String, ByVal ParamArray v() As IExpression)
            MyBase.New(v)
            _f = format
        End Sub

        Public Sub New(ByVal format As String, ByVal ParamArray v() As IGetExpression)
            MyBase.New(Array.ConvertAll(v, Function(p) p.Expression))
            _f = format
        End Sub

        Public ReadOnly Property Format() As String
            Get
                Return _f
            End Get
        End Property

        Public ReadOnly Property Values() As IExpression()
            Get
                Return MyBase.GetExpressions
            End Get
        End Property

        Public Overrides Function GetExpressions() As IExpression()
            Dim l As New List(Of IExpression)
            l.Add(Me)
            If Values IsNot Nothing Then
                For Each e As IExpression In Values
                    l.AddRange(e.GetExpressions)
                Next
            End If
            Return l.ToArray
        End Function

        Public Overrides Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, _
            ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextInfo As IDictionary, ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String
            If Values IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In Values
                    l.Add(v.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextInfo, stmtMode And Not MakeStatementMode.AddColumnAlias, executor))
                Next
                Return String.Format(_f, l.ToArray)
            Else
                Return _f
            End If
        End Function

        Public Overrides ReadOnly Property ShouldUse() As Boolean
            Get
                Return Not String.IsNullOrEmpty(_f)
            End Get
        End Property

        Public Overloads Overrides Function Equals(ByVal f As IQueryElement) As Boolean
            Return Equals(TryCast(f, CustomExpression))
        End Function

        Public Overloads Function Equals(ByVal f As CustomExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If

            Return _f = f._f AndAlso GetDynamicString() = f.GetDynamicString
        End Function

        Public Overrides Function GetDynamicString() As String
            If Values IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In Values
                    l.Add(v.GetDynamicString())
                Next
                Return String.Format(_f, l.ToArray)
            Else
                Return _f
            End If
        End Function

        Public Overrides Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String
            If Values IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In Values
                    l.Add(v.GetStaticString(mpe))
                Next
                Return String.Format(_f, l.ToArray)
            Else
                Return _f
            End If
        End Function

        Protected Overrides Function _Clone() As Object
            Return Clone()
        End Function

        Public Overloads Function Clone() As CustomExpression
            Dim n As New CustomExpression(_f)
            CopyTo(n)
            Return n
        End Function

        Protected Overrides Function _CopyTo(target As ICopyable) As Boolean
            Return CopyTo(TryCast(target, CustomExpression))
        End Function

        Public Overloads Function CopyTo(target As CustomExpression) As Boolean
            If MyBase._CopyTo(target) Then

                If target IsNot Nothing Then
                    target._f = _f
                End If
            End If

            Return False
        End Function
    End Class

End Namespace