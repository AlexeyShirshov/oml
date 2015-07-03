Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Cache

Namespace Expressions2

    <Serializable()> _
    Public MustInherit Class UnaryExpressionBase
        Implements IUnaryExpression ', IDependentTypes

        Private _v As IExpression

        Protected Sub New()
        End Sub
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

        Public Overridable Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            Return _v.GetStaticString(mpe)
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            _v.Prepare(executor, schema, contextInfo, stmt, isAnonym)
        End Sub

        Public Function ReplaceExpression(ByVal replacement As IExpression, ByVal replacer As IExpression) As IComplexExpression Implements IComplexExpression.ReplaceExpression
            If Equals(replacement) Then
                Return CType(replacer, IComplexExpression)
            End If

            Dim e As IComplexExpression = TryCast(_v, IComplexExpression)
            If e IsNot Nothing Then
                Dim v As IComplexExpression = e.ReplaceExpression(replacement, replacer)
                If v IsNot _v Then
                    Dim clone = CType(_Clone(), UnaryExpressionBase)
                    clone._v = v
                    Return clone
                End If
            ElseIf _v.Equals(replacement) Then
                Dim clone = CType(_Clone(), UnaryExpressionBase)
                clone._v = replacer
                Return clone
            End If
            Return Me
        End Function

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Dim l As New List(Of IExpression)
            l.Add(Me)
            l.AddRange(_v.GetExpressions)
            Return l.ToArray
        End Function

        Public Overridable Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef,
                                                  ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam,
                                                  ByVal almgr As IPrepareTable, ByVal contextInfo As IDictionary,
                                                  ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            Return _v.MakeStatement(schema, fromClause, stmt, paramMgr, almgr, contextInfo, stmtMode, executor)
        End Function

        Public Property Operand() As IExpression Implements IUnaryExpression.Operand
            Get
                Return _v
            End Get
            Set(ByVal value As IExpression)
                _v = value
            End Set
        End Property

        Protected MustOverride Function _Clone() As Object Implements System.ICloneable.Clone

        Protected Overridable Function _CopyTo(target As Query.ICopyable) As Boolean Implements Query.ICopyable.CopyTo
            Return CopyTo(TryCast(target, UnaryExpressionBase))
        End Function

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

        Private Function CopyTo(target As UnaryExpressionBase) As Boolean
            If target Is Nothing Then
                Return False
            End If

            If _v IsNot Nothing Then
                target._v = CType(_v.Clone, IExpression)
            End If
            Return True
        End Function

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

        Public Overrides Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String
            Return OperationType2String(_oper) & MyBase.GetStaticString(mpe)
        End Function

        Protected Overrides Function _Clone() As Object
            Return Clone()
        End Function

        Public Overloads Function Clone() As UnaryExpression
            Dim o As IExpression = Nothing
            If Operand IsNot Nothing Then
                o = CType(Operand.Clone, IExpression)
            End If
            Return New UnaryExpression(_oper, o)
        End Function

        Protected Overrides Function _CopyTo(target As Query.ICopyable) As Boolean
            Return CopyTo(TryCast(target, UnaryExpression))
        End Function

        Public Overloads Function CopyTo(target As UnaryExpression) As Boolean
            If MyBase._CopyTo(target) Then

                If target IsNot Nothing Then

                    target._oper = _oper
                    Return True
                End If
            End If

            Return False
        End Function

        Public Overrides Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef,
                                                ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam,
                                                ByVal almgr As IPrepareTable, ByVal contextInfo As IDictionary, ByVal stmtMode As MakeStatementMode,
                                                ByVal executor As Query.IExecutionContext) As String
            Return stmt.UnaryOperator2String(_oper) & MyBase.MakeStatement(schema, fromClause, stmt, paramMgr, almgr, contextInfo, stmtMode, executor)
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