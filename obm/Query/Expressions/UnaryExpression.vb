Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Cache

Namespace Expressions2

    <Serializable()> _
    Public MustInherit Class UnaryExpressionBase
        Implements IUnaryExpression ', IDependentTypes

        Private _v As IExpression

        Public Sub New(ByVal operand As IExpression)
            _v = operand
        End Sub

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

        Public MustOverride Overloads Function Equals(ByVal f As IQueryElement) As Boolean Implements IQueryElement.Equals

        Public Overridable Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            Return _v.GetDynamicString
        End Function

        Public Overridable Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            Return _v.GetStaticString(mpe, contextFilter)
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            _v.Prepare(executor, schema, filterInfo, stmt, isAnonym)
        End Sub

        Public Function ReplaceExpression(ByVal replacement As IExpression, ByVal replacer As IExpression) As IComplexExpression Implements IComplexExpression.ReplaceExpression
            If Equals(replacement) Then
                Return CType(replacer, IComplexExpression)
            End If

            Dim e As IComplexExpression = TryCast(_v, IComplexExpression)
            If e IsNot Nothing Then
                Dim v As IComplexExpression = e.ReplaceExpression(replacement, replacer)
                If v IsNot _v Then
                    Return Clone(v)
                End If
            ElseIf _v.Equals(replacement) Then
                Return Clone(replacer)
            End If
            Return Me
        End Function

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Dim l As New List(Of IExpression)
            l.Add(Me)
            l.AddRange(_v.GetExpressions)
            Return l.ToArray
        End Function

        Public Overridable Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal filterInfo As Object, ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            Return _v.MakeStatement(schema, fromClause, stmt, paramMgr, almgr, filterInfo, stmtMode, executor)
        End Function

        Public Property Operand() As IExpression Implements IUnaryExpression.Operand
            Get
                Return _v
            End Get
            Set(ByVal value As IExpression)
                _v = value
            End Set
        End Property

        Public Function Clone() As Object Implements System.ICloneable.Clone
            Return Clone(CloneExpression(_v))
        End Function

        Protected MustOverride Function Clone(ByVal operand As IExpression) As IUnaryExpression

        'Public Function GetAddDelete() As System.Collections.Generic.IEnumerable(Of System.Type) Implements Cache.IDependentTypes.GetAddDelete
        '    Dim fdp As Cache.IDependentTypes = TryCast(Operand, IDependentTypes)
        '    If fdp IsNot Nothing Then
        '        Return fdp.GetAddDelete
        '    Else
        '        Return New EmptyDependentTypes
        '    End If
        'End Function

        'Public Function GetUpdate() As System.Collections.Generic.IEnumerable(Of System.Type) Implements Cache.IDependentTypes.GetUpdate

        'End Function
    End Class

    <Serializable()> _
    Public Class UnaryExpression
        Inherits UnaryExpressionBase
        Implements IUnaryExpression, IEvaluable

        Private _oper As UnaryOperationType

        Public Sub New(ByVal oper As UnaryOperationType, ByVal operand As IExpression)
            MyBase.New(operand)
            _oper = oper
        End Sub

        Public Overrides Function Equals(ByVal f As IQueryElement) As Boolean
            Return Equals(TryCast(f, UnaryExpression))
        End Function

        Public Overloads Function Equals(ByVal f As UnaryExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If
            Return _oper = f._oper AndAlso Operand.Equals(f.Operand)
        End Function

        Public Overrides Function GetDynamicString() As String
            Return OperationType2String(_oper) & MyBase.GetDynamicString
        End Function

        Public Overrides Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String
            Return OperationType2String(_oper) & MyBase.GetStaticString(mpe, contextFilter)
        End Function

        Protected Overrides Function Clone(ByVal operand As IExpression) As IUnaryExpression
            Return New UnaryExpression(_oper, operand)
        End Function

        Public Overrides Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal filterInfo As Object, ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String
            Return stmt.UnaryOperator2String(_oper) & MyBase.MakeStatement(schema, fromClause, stmt, paramMgr, almgr, filterInfo, stmtMode, executor)
        End Function

        Public Function Eval(ByVal mpe As ObjectMappingEngine, ByVal obj As Entities._IEntity, ByVal oschema As IEntitySchema, ByRef v As Object) As Boolean Implements IEvaluable.Eval
            Dim val As Object = Nothing
            If GetValue(mpe, obj, oschema, Operand, val) Then
                Select Case _oper
                    Case UnaryOperationType.Negate
                        If IsNumeric(val) Then
                            v = Convert.ChangeType(-CDec(val), val.GetType)
                            Return True
                        End If
                    Case UnaryOperationType.Not
                        If TypeOf val Is Boolean Then
                            v = Not CBool(val)
                            Return True
                        ElseIf IsNumeric(val) Then
                            v = Convert.ChangeType(Not CLng(val), val.GetType)
                            Return True
                        End If
                    Case Else
                        Throw New NotSupportedException(OperationType2String(_oper))
                End Select
            End If
            Return False
        End Function

        Public Function CanEval(ByVal mpe As ObjectMappingEngine) As Boolean Implements IEvaluable.CanEval
            'Dim val As Object = Nothing
            'If GetValue(mpe, obj, oschema, _v, val) Then

            'End If

            Return False
        End Function
    End Class
End Namespace