Imports Worm.Query
Imports Worm.Entities.Meta
Imports System.Collections.Generic

Namespace Expressions2
    Public Enum BinaryOperationType
        'comparision
        Equal
        NotEqual
        GreaterThan
        GreaterEqualThan
        LessEqualThan
        LessThan
        [In]
        NotIn
        [Like]
        [Is]
        [IsNot]
        Exists
        NotExists
        Between

        'conditional
        [And]
        [Or]

        'bitwise
        ExclusiveOr
        BitAnd
        BitOr

        'arithmetic
        Add
        Subtract
        Divide
        Multiply
        Modulo

        'shift
        LeftShift
        RightShift
    End Enum

    Public Enum UnaryOperationType
        Negate
        [Not]
    End Enum

    Public Interface IQueryElement
        Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String
        Function GetDynamicString() As String
        Sub Prepare(ByVal executor As IExecutor, _
            ByVal schema As ObjectMappingEngine, ByVal contextFilter As Object, _
            ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean)
        Function Equals(ByVal f As IQueryElement) As Boolean
    End Interface

    Public Interface IGetExpression
        ReadOnly Property Expression() As IExpression
    End Interface

    Public Interface IHashable
        Function Test(ByVal schema As ObjectMappingEngine, ByVal obj As Entities._IEntity, ByVal oschema As IEntitySchema) As IParameterExpression.EvalResult
        Function MakeHash(ByVal schema As ObjectMappingEngine, ByVal oschema As IEntitySchema, ByVal obj As Entities.ICachedEntity) As String
    End Interface

    Public Interface IExpression
        Inherits IQueryElement, IGetExpression
        Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, _
                          ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, _
                          ByVal contextFilter As Object, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String
        ReadOnly Property ShouldUse() As Boolean
        Function GetExpressions() As ICollection(Of IExpression)
    End Interface

    Public Interface IParameterExpression
        Inherits IExpression

        Enum EvalResult
            Found
            NotFound
            Unknown
        End Enum

        ReadOnly Property Value() As Object
        Function Test(ByVal oper As BinaryOperationType, ByVal v As Object, ByVal mpe As ObjectMappingEngine) As EvalResult

        Class ModifyValueArgs
            Private _modified As Boolean
            Public Property Modified() As Boolean
                Get
                    Return _modified
                End Get
                Set(ByVal value As Boolean)
                    _modified = value
                End Set
            End Property

            Private _v As Object
            Public Property NewValue() As Object
                Get
                    Return _v
                End Get
                Set(ByVal value As Object)
                    _v = value
                End Set
            End Property
        End Class

        Delegate Sub ModifyValueDelegate(ByVal sender As IParameterExpression, ByVal args As ModifyValueArgs)
        Event ModifyValue As ModifyValueDelegate
    End Interface

    Public Interface IEntityPropertyExpression
        Inherits IExpression, ICloneable
        Property Entity() As EntityUnion
        Property PropertyAlias() As String
        Function SetEntity(ByVal eu As Query.EntityUnion) As IEntityPropertyExpression
    End Interface

    Public Interface IComplexExpression
        Inherits IExpression, ICloneable, IHashable
        Function ReplaceExpression(ByVal replacement As IComplexExpression, ByVal replacer As IComplexExpression) As IComplexExpression
        Function RemoveExpression(ByVal f As IComplexExpression) As IComplexExpression
    End Interface

End Namespace