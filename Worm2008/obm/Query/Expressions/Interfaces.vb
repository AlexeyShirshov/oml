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

        ''' <summary>
        ''' Arithmetic additional
        ''' </summary>
        ''' <remarks></remarks>
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

    <Flags()> _
    Public Enum MakeStatementMode
        None = 0
        [Select] = 1
        AddColumnAlias = 2
        WithoutTables = 4
        Replace = 8

        SelectWithoutTables = [Select] Or WithoutTables Or AddColumnAlias
    End Enum

    Public Interface IQueryElement
        Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String
        Function GetDynamicString() As String
        Sub Prepare(ByVal executor As IExecutor, _
            ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object, _
            ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean)
        Function Equals(ByVal f As IQueryElement) As Boolean
    End Interface

    Public Interface IGetExpression
        ReadOnly Property Expression() As IExpression
    End Interface

    Public Interface IHashable
        Function Test(ByVal mpe As ObjectMappingEngine, ByVal oschema As IEntitySchema, ByVal obj As Entities._IEntity) As IParameterExpression.EvalResult
        Function MakeDynamicString(ByVal mpe As ObjectMappingEngine, ByVal oschema As IEntitySchema, ByVal obj As Entities.ICachedEntity) As String
    End Interface

    Public Interface IExpression
        Inherits IQueryElement, IGetExpression
        Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, _
                          ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, _
                          ByVal contextFilter As Object, ByVal stmtMode As MakeStatementMode, ByVal executor As IExecutionContext) As String
        ReadOnly Property ShouldUse() As Boolean
        Function GetExpressions() As IExpression()
    End Interface

    Public Interface IParameterExpression
        Inherits IExpression

        Enum EvalResult
            Found
            NotFound
            Unknown
        End Enum

        ReadOnly Property Value() As Object
        'Function Test(ByVal oper As BinaryOperationType, ByVal v As Object, ByVal mpe As ObjectMappingEngine) As EvalResult

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

        Event ModifyValue(ByVal sender As IParameterExpression, ByVal args As ModifyValueArgs)
    End Interface

    Public Interface IContextable
        Inherits IExpression
        Function SetEntity(ByVal eu As Query.EntityUnion) As IContextable
    End Interface

    Public Interface IEntityPropertyExpression
        Inherits IContextable, ICloneable

        Class FormatBehaviourArgs

            Public Delegate Function MakeStatementDelegate(ByVal mpe As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, _
                          ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, ByVal contextFilter As Object, _
                          ByVal stmtMode As MakeStatementMode, ByVal executor As IExecutionContext, _
                          ByVal field As SourceField) As String

            Class CustomStatementClass
                Public FromLeft As Boolean
                Public MakeStatement As MakeStatementDelegate
            End Class

            Private _notNeedAlias As Boolean
            Public Property NeedAlias() As Boolean
                Get
                    Return Not _notNeedAlias
                End Get
                Set(ByVal value As Boolean)
                    _notNeedAlias = Not value
                End Set
            End Property

            Private _сustom As CustomStatementClass
            Public Property CustomStatement() As CustomStatementClass
                Get
                    Return _сustom
                End Get
                Set(ByVal value As CustomStatementClass)
                    _сustom = value
                End Set
            End Property

        End Class

        Event FormatBehaviour(ByVal sender As IEntityPropertyExpression, ByVal args As FormatBehaviourArgs)

        Property ObjectProperty() As ObjectProperty
    End Interface

    Public Interface IComplexExpression
        Inherits IExpression, ICloneable
        Function ReplaceExpression(ByVal replacement As IExpression, ByVal replacer As IExpression) As IComplexExpression
        'Function RemoveExpression(ByVal f As IComplexExpression) As IComplexExpression
    End Interface

    Public Interface IEvaluable
        Function Eval(ByVal mpe As ObjectMappingEngine, _
            ByVal obj As Entities._IEntity, ByVal oschema As IEntitySchema, ByRef v As Object) As Boolean
        Function CanEval(ByVal mpe As ObjectMappingEngine) As Boolean
    End Interface

    Public Interface IUnaryExpression
        Inherits IComplexExpression

        Property Operand() As IExpression
    End Interface
End Namespace
