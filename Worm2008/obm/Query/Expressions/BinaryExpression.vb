Imports System.Collections.Generic

Namespace Expressions2
    <Serializable()> _
    Public MustInherit Class BinaryExpressionBase
        Implements IComplexExpression

        Private _v As Pair(Of IExpression)

        Public Sub New(ByVal left As Object, ByVal right As Object)
            If left Is Nothing Then
                Throw New ArgumentNullException("left")
            End If

            If right Is Nothing Then
                Throw New ArgumentNullException("right")
            End If

            _v = New Pair(Of IExpression)(New ParameterExpression(left), New ParameterExpression(right))
        End Sub

        Public Sub New(ByVal left As IExpression, ByVal right As IExpression)
            If left Is Nothing Then
                Throw New ArgumentNullException("left")
            End If

            If right Is Nothing Then
                Throw New ArgumentNullException("right")
            End If

            _v = New Pair(Of IExpression)(left, right)
        End Sub

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            Return Value.First.GetDynamicString & BinaryType & Value.Second.GetDynamicString
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            Return Value.First.GetStaticString(mpe, contextFilter) & BinaryType & Value.Second.GetStaticString(mpe, contextFilter)
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            Value.First.Prepare(executor, schema, filterInfo, stmt, isAnonym)
            Value.Second.Prepare(executor, schema, filterInfo, stmt, isAnonym)
        End Sub

        Public ReadOnly Property Value() As Pair(Of IExpression)
            Get
                Return _v
            End Get
        End Property

        Protected MustOverride ReadOnly Property BinaryType() As String
        Public MustOverride Function Clone() As Object Implements System.ICloneable.Clone
        Public MustOverride Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal inSelect As Boolean, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
        Public MustOverride Function MakeHash(ByVal schema As ObjectMappingEngine, ByVal oschema As Entities.Meta.IEntitySchema, ByVal obj As Entities.ICachedEntity) As String Implements IHashable.MakeHash
        Public MustOverride Function Test(ByVal schema As ObjectMappingEngine, ByVal obj As Entities._IEntity, ByVal oschema As Entities.Meta.IEntitySchema) As IParameterExpression.EvalResult Implements IHashable.Test
        Public MustOverride Function RemoveExpression(ByVal f As IComplexExpression) As IComplexExpression Implements IComplexExpression.RemoveExpression

        Public Function ReplaceExpression(ByVal replacement As IComplexExpression, ByVal replacer As IComplexExpression) As IComplexExpression Implements IComplexExpression.ReplaceExpression

            If replacement.Equals(Me) Then
                Return replacer
            End If

            If GetType(IComplexExpression).IsAssignableFrom(Left.GetType) OrElse GetType(IComplexExpression).IsAssignableFrom(Right.GetType) Then
                Dim b As BinaryExpressionBase = CType(Clone(), BinaryExpressionBase)
                If GetType(IComplexExpression).IsAssignableFrom(Left.GetType) Then
                    b.Left = CType(Left, IComplexExpression).ReplaceExpression(replacement, replacer)
                End If
                If GetType(IComplexExpression).IsAssignableFrom(Right.GetType) Then
                    b.Right = CType(Right, IComplexExpression).ReplaceExpression(replacement, replacer)
                End If
                If b.Left IsNot Left OrElse b.Right IsNot Right Then
                    Return b
                End If
            End If

            Return Me
        End Function

        Public Function GetExpressions() As System.Collections.Generic.ICollection(Of IExpression) Implements IExpression.GetExpressions
            Dim l As New List(Of IExpression)
            l.Add(Me)
            l.AddRange(Value.First.GetExpressions)
            l.AddRange(Value.Second.GetExpressions)
            Return l
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
            Return Equals(TryCast(f, BinaryExpressionBase))
        End Function

        Public Overloads Function Equals(ByVal f As BinaryExpressionBase) As Boolean
            If f Is Nothing Then
                Return False
            End If
            Return BinaryType = f.BinaryType AndAlso Value.First.Equals(f.Value.First) AndAlso Value.Second.Equals(Value.Second)
        End Function

        Public Property Left() As IExpression
            Get
                Return _v.First
            End Get
            Protected Set(ByVal value As IExpression)
                _v = New Pair(Of IExpression)(value, Right)
            End Set
        End Property

        Public Property Right() As IExpression
            Get
                Return _v.Second
            End Get
            Protected Set(ByVal value As IExpression)
                _v = New Pair(Of IExpression)(Left, value)
            End Set
        End Property
    End Class

    Public Class BinaryExpresssion
        Inherits BinaryExpressionBase

        Private _oper As BinaryOperationType

        Public Sub New(ByVal left As String, ByVal oper As BinaryOperationType, ByVal right As String)
            MyClass.New(New LiteralExpression(left), oper, New LiteralExpression(right))
        End Sub

        Public Sub New(ByVal left As Object, ByVal oper As BinaryOperationType, ByVal right As Object)
            MyBase.New(left, right)
            _oper = oper
        End Sub

        Public Sub New(ByVal left As IExpression, ByVal oper As BinaryOperationType, ByVal right As IExpression)
            MyBase.New(left, right)
            _oper = oper
        End Sub

        Protected Overrides ReadOnly Property BinaryType() As String
            Get
                Return OperationType2String(_oper)
            End Get
        End Property

        Public Overrides Function Clone() As Object
            Return New BinaryExpresssion(Left, _oper, Right)
        End Function

        Public Overrides Function MakeHash(ByVal schema As ObjectMappingEngine, ByVal oschema As Entities.Meta.IEntitySchema, ByVal obj As Entities.ICachedEntity) As String

        End Function

        Public Overrides Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal inSelect As Boolean, ByVal executor As Query.IExecutionContext) As String
            If Right Is Nothing Then
                Return Left.MakeStatement(schema, fromClause, stmt, paramMgr, almgr, contextFilter, inSelect, executor)
            End If
            Return "(" & Left.MakeStatement(schema, fromClause, stmt, paramMgr, almgr, contextFilter, inSelect, executor) & _
                stmt.BinaryOperator2String(_oper) & _
                Right.MakeStatement(schema, fromClause, stmt, paramMgr, almgr, contextFilter, inSelect, executor) & ")"
        End Function

        Public Overrides Function RemoveExpression(ByVal f As IComplexExpression) As IComplexExpression

        End Function

        Public Overrides Function Test(ByVal schema As ObjectMappingEngine, ByVal obj As Entities._IEntity, ByVal oschema As Entities.Meta.IEntitySchema) As IParameterExpression.EvalResult

        End Function
    End Class

    Public Class BetweenExpresssion
        Inherits BinaryExpressionBase
        Implements IParameterExpression

        Public Sub New(ByVal left As Object, ByVal right As Object)
            MyBase.New(left, right)
        End Sub

        Public Sub New(ByVal left As IExpression, ByVal right As IExpression)
            MyBase.New(left, right)
        End Sub

        Public Overrides Function Test(ByVal schema As ObjectMappingEngine, ByVal obj As Entities._IEntity, ByVal oschema As Entities.Meta.IEntitySchema) As IParameterExpression.EvalResult
            Throw New NotImplementedException
        End Function

        Public Overrides Function MakeHash(ByVal schema As ObjectMappingEngine, ByVal oschema As Entities.Meta.IEntitySchema, ByVal obj As Entities.ICachedEntity) As String
            Throw New NotImplementedException
        End Function

        Public Overrides Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal inSelect As Boolean, ByVal executor As Query.IExecutionContext) As String
            Return Left.MakeStatement(schema, fromClause, stmt, paramMgr, almgr, contextFilter, inSelect, executor) & _
                " and " & _
                Right.MakeStatement(schema, fromClause, stmt, paramMgr, almgr, contextFilter, inSelect, executor)
        End Function

        Protected Overrides ReadOnly Property BinaryType() As String
            Get
                Return "between"
            End Get
        End Property

        Public Overrides Function Clone() As Object
            Return New BetweenExpresssion(CloneExpression(Left), CloneExpression(Right))
        End Function

        Public Event ModifyValue(ByVal sender As IParameterExpression, ByVal args As IParameterExpression.ModifyValueArgs) Implements IParameterExpression.ModifyValue

        Public Overloads Function Test(ByVal oper As BinaryOperationType, ByVal v As Object, ByVal mpe As ObjectMappingEngine) As IParameterExpression.EvalResult Implements IParameterExpression.Test
            Dim r As IParameterExpression.EvalResult = IParameterExpression.EvalResult.NotFound

            If oper = BinaryOperationType.Between Then
                Dim fe As IParameterExpression = TryCast(Left, IParameterExpression)
                Dim se As IParameterExpression = TryCast(Right, IParameterExpression)

                If fe IsNot Nothing AndAlso se IsNot Nothing Then
                    Dim i As Integer = CType(v, IComparable).CompareTo(fe.Value)
                    If i >= 0 Then
                        i = CType(v, IComparable).CompareTo(se.Value)
                        If i <= 0 Then
                            r = IParameterExpression.EvalResult.Found
                        End If
                    End If
                End If
            Else
                Throw New InvalidOperationException(String.Format("Invalid operation {0} for BetweenValue", oper))
            End If
            Return r
        End Function

        Protected ReadOnly Property _Value() As Object Implements IParameterExpression.Value
            Get
                Return Value
            End Get
        End Property

        Public Overrides Function RemoveExpression(ByVal f As IComplexExpression) As IComplexExpression
            If f.Equals(Me) Then
                Return Nothing
            End If

            Dim _left As IExpression = Nothing, _right As IExpression = Nothing

            If Left IsNot Nothing Then
                If Left.Equals(f) Then
                    _left = New BinaryExpresssion("1", BinaryOperationType.Equal, "1")
                ElseIf (GetType(IComplexExpression).IsAssignableFrom(Left.GetType)) Then
                    _left = CType(Left, IComplexExpression).RemoveExpression(f)
                End If
            End If

            If Right IsNot Nothing Then
                If Right.Equals(f) Then
                    _right = New BinaryExpresssion("1", BinaryOperationType.Equal, "1")
                ElseIf (GetType(IComplexExpression).IsAssignableFrom(Right.GetType)) Then
                    _right = CType(Right, IComplexExpression).RemoveExpression(f)
                End If
            End If

            If Left IsNot _left OrElse Right IsNot _right Then
                Return New BetweenExpresssion(_left, _right)
            Else
                Return Me
            End If
        End Function
    End Class
End Namespace