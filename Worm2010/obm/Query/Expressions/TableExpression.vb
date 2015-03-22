Imports Worm.Entities.Meta
Imports System.Collections.Generic
Imports Worm.Query

Namespace Expressions2
    <Serializable()> _
    Public Class TableExpression
        Implements IContextable

        Private _sf As SourceFragment
        Private _col As String
        Private _eu As EntityUnion

        Public Sub New(ByVal col As String)
            _col = col
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal col As String)
            _sf = t
            _col = col
        End Sub

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Return New IExpression() {Me}
        End Function

        Public Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, _
            ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, _
            ByVal almgr As IPrepareTable, ByVal contextInfo As IDictionary, ByVal stmtMode As MakeStatementMode, _
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

            Return [alias] & _col
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
            Return Equals(TryCast(f, TableExpression))
        End Function

        Public Overloads Function Equals(ByVal f As TableExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If

            Return _sf Is f._sf AndAlso _col = f._col
        End Function

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            Return _sf.RawName & "$" & _col
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

        Public ReadOnly Property SourceField() As String
            Get
                Return _col
            End Get
        End Property

        Public Function SetEntity(ByVal eu As Query.EntityUnion) As IContextable Implements IContextable.SetEntity
            _eu = eu
            Return Me
        End Function
    End Class
End Namespace