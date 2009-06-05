Namespace Expressions2
    Public Class EntityExpression
        Implements IEntityPropertyExpression

        Public Function Clone() As Object Implements System.ICloneable.Clone

        End Function

        Public Property Entity() As Query.EntityUnion Implements IEntityPropertyExpression.Entity
            Get

            End Get
            Set(ByVal value As Query.EntityUnion)

            End Set
        End Property

        Public Property PropertyAlias() As String Implements IEntityPropertyExpression.PropertyAlias
            Get

            End Get
            Set(ByVal value As String)

            End Set
        End Property

        Public Function SetEntity(ByVal eu As Query.EntityUnion) As IEntityPropertyExpression Implements IEntityPropertyExpression.SetEntity

        End Function

        Public Function GetExpressions() As System.Collections.Generic.ICollection(Of IExpression) Implements IExpression.GetExpressions

        End Function

        Public Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal inSelect As Boolean, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement

        End Function

        Public ReadOnly Property ShouldUse() As Boolean Implements IExpression.ShouldUse
            Get

            End Get
        End Property

        Public ReadOnly Property Expression() As IExpression Implements IGetExpression.Expression
            Get

            End Get
        End Property

        Public Function Equals1(ByVal f As IQueryElement) As Boolean Implements IQueryElement.Equals

        End Function

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString

        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString

        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextFilter As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare

        End Sub
    End Class
End Namespace