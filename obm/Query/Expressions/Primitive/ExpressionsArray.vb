Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query
Imports Worm.Cache

Namespace Expressions2

    Public Class ExpressionsArray
        Implements IExpression

        Private _v() As IExpression

        Public Sub New()

        End Sub

        Public Sub New(ParamArray exps() As IExpression)
            If exps IsNot Nothing Then
                Dim l As New List(Of IExpression)
                For Each e As IExpression In exps
                    If TypeOf e Is ExpressionsArray Then
                        l.AddRange(e.GetExpressions)
                    Else
                        l.Add(e)
                    End If
                Next
                _v = l.ToArray
            End If
        End Sub

        Public Overridable Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Return _v
        End Function

        Public Overridable Function MakeStatement(mpe As ObjectMappingEngine, fromClause As Query.QueryCmd.FromClauseDef,
                                                  stmt As StmtGenerator, paramMgr As Entities.Meta.ICreateParam, almgr As IPrepareTable,
                                                  contextInfo As IDictionary, stmtMode As MakeStatementMode, executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            If _v IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In _v
                    l.Add(v.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextInfo, stmtMode And Not MakeStatementMode.AddColumnAlias, executor))
                Next
                Return String.Join(",", l.ToArray)
            Else
                Return String.Empty
            End If
        End Function

        Public Overridable ReadOnly Property ShouldUse As Boolean Implements IExpression.ShouldUse
            Get
                Return _v IsNot Nothing
            End Get
        End Property

        Public ReadOnly Property Expression As IExpression Implements IGetExpression.Expression
            Get
                Return Me
            End Get
        End Property

        Public Overridable Overloads Function Equals(f As IQueryElement) As Boolean Implements IQueryElement.Equals
            Return Equals(CType(f, ExpressionsArray))
        End Function

        Public Overloads Function Equals(ByVal f As ExpressionsArray) As Boolean
            If _v Is Nothing Then
                Return False
            End If

            Return GetDynamicString() = f.GetDynamicString
        End Function

        Public Overridable Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            If _v IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In _v
                    l.Add(v.GetDynamicString())
                Next
                Return String.Join(",", l.ToArray)
            Else
                Return String.Empty
            End If
        End Function

        Public Overridable Function GetStaticString(mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            If _v IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In _v
                    l.Add(v.GetStaticString(mpe))
                Next
                Return String.Join(",", l.ToArray)
            Else
                Return String.Empty
            End If
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            If _v IsNot Nothing Then
                For Each e As IExpression In _v
                    e.Prepare(executor, mpe, contextInfo, stmt, isAnonym)
                Next
            End If
        End Sub

        Protected Overridable Function _Clone() As Object Implements ICloneable.Clone
            Return Clone()
        End Function
        Public Function Clone() As ExpressionsArray
            Dim n As New ExpressionsArray
            CopyTo(n)
            Return n
        End Function

        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, ExpressionsArray))
        End Function

        Public Function CopyTo(target As ExpressionsArray) As Boolean
            If target Is Nothing Then
                Return False
            End If

            If _v IsNot Nothing Then
                Dim l As New List(Of IExpression)
                For Each e In _v
                    l.Add(CType(e.Clone, IExpression))
                Next

                target._v = l.ToArray
            End If

            Return True
        End Function
    End Class


End Namespace