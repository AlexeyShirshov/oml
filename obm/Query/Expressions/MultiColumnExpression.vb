﻿Imports Worm.Entities.Meta
Imports System.Collections.Generic
Imports Worm.Query
Imports System.Linq

Namespace Expressions2
    <Serializable()>
    Public Class MultiColumnExpression
        Implements IContextable

        Private _sf As SourceFragment
        Private _cols As IEnumerable(Of String)
        Private _eu As EntityUnion

        Protected Sub New()

        End Sub
        Public Sub New(ByVal cols As IEnumerable(Of String))
            _cols = cols
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal cols As IEnumerable(Of String))
            _sf = t
            _cols = cols
        End Sub

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Return New IExpression() {Me}
        End Function

        Public Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef,
                                      ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam,
                                      ByVal almgr As IPrepareTable, ByVal contextInfo As IDictionary, ByVal stmtMode As MakeStatementMode,
                                      ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement

            Dim [alias] As String = String.Empty

            If almgr IsNot Nothing AndAlso (stmtMode And MakeStatementMode.WithoutTables) <> MakeStatementMode.WithoutTables Then
                'Debug.Assert(almgr.ContainsKey(_sf, _eu), "There is not alias for table " & _sf.RawName)
                Try
                    [alias] = almgr.GetAlias(_sf, _eu) & stmt.Selector
                Catch ex As KeyNotFoundException
                    Throw New ObjectMappingException("There is not alias for table " & _sf.RawName, ex)
                End Try
            Else
                [alias] = _sf.UniqueName(_eu) & mpe.Delimiter
            End If

            Dim sb As New StringBuilder
            For Each col In _cols
                sb.Append([alias]).Append(col).Append(", ")
            Next
            sb.Length -= 2
            Return sb.ToString
        End Function

        Public ReadOnly Property ShouldUse() As Boolean Implements IExpression.ShouldUse
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property Expression() As IExpression Implements IGetExpression.Expression
            Get
                Return Me
            End Get
        End Property

        Public Overloads Function Equals(ByVal f As IQueryElement) As Boolean Implements IQueryElement.Equals
            Return Equals(TryCast(f, MultiColumnExpression))
        End Function

        Public Overloads Function Equals(ByVal f As MultiColumnExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If

            Return _sf Is f._sf AndAlso _cols.All(Function(it) f._cols.Contains(it))
        End Function

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            Return _sf.RawName & "$" & String.Join(",", _cols)
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            Return GetDynamicString()
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            'do nothing
        End Sub

        Public ReadOnly Property SourceFragment() As SourceFragment
            Get
                Return _sf
            End Get
        End Property

        Public ReadOnly Property Columns() As IEnumerable(Of String)
            Get
                Return _cols
            End Get
        End Property

        Public Function SetEntity(ByVal eu As Query.EntityUnion) As IContextable Implements IContextable.SetEntity
            _eu = eu
            Return Me
        End Function

        Protected Overridable Function _Clone() As Object Implements ICloneable.Clone
            Return Clone()
        End Function
        Public Function Clone() As MultiColumnExpression
            Dim n As New MultiColumnExpression
            CopyTo(n)
            Return n
        End Function

        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, MultiColumnExpression))
        End Function
        Public Function CopyTo(target As MultiColumnExpression) As Boolean
            If target Is Nothing Then
                Return False
            End If

            target._cols = _cols.ToArray

            If _sf IsNot Nothing Then
                target._sf = _sf.Clone
            End If

            If _eu IsNot Nothing Then
                target._eu = _eu.Clone
            End If

            Return True
        End Function
    End Class
End Namespace