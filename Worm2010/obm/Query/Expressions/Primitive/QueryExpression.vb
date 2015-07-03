Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query
Imports Worm.Cache

Namespace Expressions2

    <Serializable()> _
        Public Class QueryExpression
        Implements IExpression ', IDependentTypes

        Private _q As Query.QueryCmd

        Protected Sub New()

        End Sub
        Public Sub New(ByVal q As Query.QueryCmd)
            _q = q
        End Sub

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Return New IExpression() {Me}
        End Function

        Public Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef,
                                      ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable,
                                      ByVal contextInfo As IDictionary, ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            Dim sb As New StringBuilder
            sb.Append("(")
            sb.Append(stmt.MakeQueryStatement(schema, fromClause, contextInfo, _q, paramMgr, almgr))
            sb.Append(")")
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
            Return Equals(TryCast(f, QueryExpression))
        End Function

        Public Overloads Function Equals(ByVal f As QueryExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If
            Return _q.Equals(f._q)
        End Function

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            Return _q._ToString
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            Return _q.GetStaticString(mpe)
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            _q.Prepare(executor, schema, contextInfo, stmt, isAnonym)
        End Sub

        'Public Function GetAddDelete() As System.Collections.Generic.IEnumerable(Of System.Type) Implements Cache.IDependentTypes.GetAddDelete
        '    Return CType(_q, IDependentTypes).GetAddDelete
        'End Function

        'Public Function GetUpdate() As System.Collections.Generic.IEnumerable(Of System.Type) Implements Cache.IDependentTypes.GetUpdate
        '    Return CType(_q, IDependentTypes).GetUpdate
        'End Function

        Protected Overridable Function _Clone() As Object Implements ICloneable.Clone
            Return Clone()
        End Function

        Public Function Clone() As QueryExpression
            Dim n As New QueryExpression
            CopyTo(n)
            Return n
        End Function

        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, QueryExpression))
        End Function

        Public Function CopyTo(target As QueryExpression) As Boolean
            If target Is Nothing Then
                Return False
            End If

            If _q IsNot Nothing Then
                target._q = _q.Clone
            End If

            Return True
        End Function
    End Class


End Namespace