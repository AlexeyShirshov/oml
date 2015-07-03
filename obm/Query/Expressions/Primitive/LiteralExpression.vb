Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query
Imports Worm.Cache

Namespace Expressions2

    <Serializable()> _
    Public Class LiteralExpression
        Implements IExpression

        Private _literalValue As String

        Public Sub New(ByVal literal As String)
            _literalValue = literal
        End Sub

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            Return _literalValue
        End Function

        Public Overridable ReadOnly Property ShouldUse() As Boolean Implements IExpression.ShouldUse
            Get
                Return True
            End Get
        End Property

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            Return "litval"
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            'do nothing
        End Sub

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Return New IExpression() {Me}
        End Function

        Public Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef,
                                      ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable,
                                      ByVal contextInfo As IDictionary, ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            Return _literalValue
        End Function

        Public ReadOnly Property Expression() As IExpression Implements IGetExpression.Expression
            Get
                Return Me
            End Get
        End Property

        Public Overloads Function Equals(ByVal f As IQueryElement) As Boolean Implements IQueryElement.Equals
            Return Equals(TryCast(f, LiteralExpression))
        End Function

        Public Overloads Function Equals(ByVal f As LiteralExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If
            Return _literalValue.Equals(f._literalValue)
        End Function

        Shared Function Create(s As Object) As LiteralExpression
            If s Is Nothing Then
                Return Nothing
            End If

            If TypeOf s Is String Then
                Return New LiteralExpression("'" & s.ToString & "'")
            ElseIf TypeOf s Is DateTime Then
                Return New LiteralExpression(String.Format("'{0:yyyy-MM-dd HH:ss}'"))
            ElseIf TypeOf s Is Byte() Then
                Throw New NotImplementedException("database specific")
            End If

            Return New LiteralExpression(s.ToString)
        End Function

        Protected Overridable Function _Clone() As Object Implements ICloneable.Clone
            Return New LiteralExpression(_literalValue)
        End Function

        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, LiteralExpression))
        End Function

        Public Function CopyTo(target As LiteralExpression) As Boolean
            If target Is Nothing Then
                Return False
            End If

            target._literalValue = _literalValue

            Return True
        End Function
    End Class

End Namespace