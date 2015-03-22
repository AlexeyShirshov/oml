Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta

Namespace Expressions2
    <Serializable()> _
    Public Class BinaryExpressionBase
        Implements IComplexExpression

        Private _v As Pair(Of IExpression)
        Private _case As Boolean?
        Private _parentheses As Boolean

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

        Public Sub New(ByVal left As IExpression, ByVal right As IExpression, ByVal [case] As Boolean)
            If left Is Nothing Then
                Throw New ArgumentNullException("left")
            End If

            If right Is Nothing Then
                Throw New ArgumentNullException("right")
            End If

            _v = New Pair(Of IExpression)(left, right)
            _case = [case]
        End Sub

        Public Sub New(ByVal left As IExpression)
            If left Is Nothing Then
                Throw New ArgumentNullException("left")
            End If

            _v = New Pair(Of IExpression)(left, Nothing)
        End Sub

        Protected Function GetCase() As String
            If _case.HasValue Then
                Return "$" & _case.Value
            Else
                Return String.Empty
            End If
        End Function

        Public Shared Function CreateFromEnumerable(ByVal e As IEnumerable) As BinaryExpressionBase
            Dim b As BinaryExpressionBase = Nothing
            For Each exp As IExpression In e
                If b Is Nothing Then
                    b = New BinaryExpressionBase(exp)
                Else
                    b = New BinaryExpressionBase(b, exp)
                End If
            Next
            Return b
        End Function

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            Dim s As String = BinaryType & "(" & Left.GetDynamicString
            If Right IsNot Nothing Then
                s &= "," & Right.GetDynamicString
            End If
            s &= ")" & GetCase()
            Return s
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            Dim s As String = BinaryType & GetCase() & "(" & Left.GetStaticString(mpe)
            If Right IsNot Nothing Then
                s &= "," & Right.GetStaticString(mpe)
            End If
            s &= ")"
            Return s
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            Left.Prepare(executor, schema, contextInfo, stmt, isAnonym)
            If Right IsNot Nothing Then
                Right.Prepare(executor, schema, contextInfo, stmt, isAnonym)
            End If
        End Sub

        Public ReadOnly Property Value() As Pair(Of IExpression)
            Get
                Return _v
            End Get
        End Property

        Protected Overridable ReadOnly Property BinaryType() As String
            Get
                Return "Comma"
            End Get
        End Property

        Public Overridable Function Clone() As Object Implements System.ICloneable.Clone
            Return New BinaryExpressionBase(Left, Right)
        End Function

        Public Overridable Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef,
                                                  ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam,
                                                  ByVal almgr As IPrepareTable, ByVal contextInfo As IDictionary,
                                                  ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            Dim sb As New StringBuilder
            If _parentheses Then
                sb.Append("(")
            End If

            sb.Append(Left.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextInfo, stmtMode, executor))

            If Right IsNot Nothing Then
                sb.Append(",").Append(Right.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextInfo, stmtMode, executor))
            End If

            If _parentheses Then
                sb.Append(")")
            End If

            Return sb.ToString
        End Function

        'Public MustOverride Function MakeDynamicString(ByVal schema As ObjectMappingEngine, ByVal oschema As Entities.Meta.IEntitySchema, ByVal obj As Entities.ICachedEntity) As String Implements IHashable.MakeDynamicString
        'Public MustOverride Function Test(ByVal schema As ObjectMappingEngine, ByVal obj As Entities._IEntity, ByVal oschema As Entities.Meta.IEntitySchema) As IParameterExpression.EvalResult Implements IHashable.Test
        'Public MustOverride Function RemoveExpression(ByVal f As IComplexExpression) As IComplexExpression Implements IComplexExpression.RemoveExpression
        'Public MustOverride Function Eval(ByVal mpe As ObjectMappingEngine, _
        '    ByVal obj As Entities._IEntity, ByVal oschema As IEntitySchema, ByRef v As Object) As Boolean Implements IComplexExpression.Eval
        'Public MustOverride Function CanEval(ByVal mpe As ObjectMappingEngine) As Boolean Implements IEvaluable.CanEval

        Public Function ReplaceExpression(ByVal replacement As IExpression, ByVal replacer As IExpression) As IComplexExpression Implements IComplexExpression.ReplaceExpression

            If replacement.Equals(Me) Then
                Return CType(replacer, IComplexExpression)
            End If

            Dim b As BinaryExpressionBase = CType(Clone(), BinaryExpressionBase)
            If GetType(IComplexExpression).IsAssignableFrom(Left.GetType) Then
                b.Left = CType(Left, IComplexExpression).ReplaceExpression(replacement, replacer)
            ElseIf replacement.Equals(Left) Then
                b.Left = replacer
            End If

            If GetType(IComplexExpression).IsAssignableFrom(Right.GetType) Then
                b.Right = CType(Right, IComplexExpression).ReplaceExpression(replacement, replacer)
            ElseIf replacement.Equals(Right) Then
                b.Right = replacer
            End If

            If b.Left IsNot Left OrElse b.Right IsNot Right Then
                Return b
            End If

            Return Me
        End Function

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Dim l As New List(Of IExpression)
            l.Add(Me)
            l.AddRange(Left.GetExpressions)
            If Right IsNot Nothing Then
                l.AddRange(Right.GetExpressions)
            End If
            Return l.ToArray
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
            Return BinaryType = f.BinaryType AndAlso Left.Equals(f.Left) AndAlso Object.Equals(Right, f.Right)
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

        Public Function IsCaseSensitive(ByVal mpe As ObjectMappingEngine) As Boolean
            If _case.HasValue Then
                Return _case.Value
            Else
                Return mpe.CaseSensitive
            End If
        End Function

        Public Property Parentheses() As Boolean
            Get
                Return _parentheses
            End Get
            Set(ByVal value As Boolean)
                _parentheses = value
            End Set
        End Property
    End Class

    Public Class BinaryExpression
        Inherits BinaryExpressionBase
        Implements IEvaluable, IHashable

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

        Public Sub New(ByVal left As IExpression, ByVal oper As BinaryOperationType, ByVal right As IExpression, ByVal caseSensitive As Boolean)
            MyBase.New(left, right)
            _oper = oper
        End Sub

        Protected Overrides ReadOnly Property BinaryType() As String
            Get
                Return OperationType2String(_oper)
            End Get
        End Property

        Public ReadOnly Property ExpressionType() As BinaryOperationType
            Get
                Return _oper
            End Get
        End Property

        Public Overrides Function Clone() As Object
            Return New BinaryExpression(Left, _oper, Right)
        End Function

        Public Function MakeDynamicString(ByVal schema As ObjectMappingEngine, _
            ByVal oschema As Entities.Meta.IEntitySchema, ByVal obj As Entities.ICachedEntity) As String Implements IHashable.MakeDynamicString

            Dim l As String = EmptyHash
            'Dim left_ As IComplexExpression = TryCast(Left, IComplexExpression)

            'If left_ IsNot Nothing Then
            '    l = left_.MakeHash(schema, oschema, obj)
            '    Dim right_ As IComplexExpression = TryCast(Right, IComplexExpression)
            '    If right_ IsNot Nothing Then
            '        Dim r As String = right_.MakeHash(schema, oschema, obj)
            '        If r = EmptyHash Then
            '            If _oper <> BinaryOperationType.And Then
            '                l = r
            '            End If
            '        Else
            '            l = l & BinaryType & r
            '        End If
            '    End If
            'End If

            Dim re As IComplexExpression = Me
            For Each e As IExpression In GetExpressions()
                Dim bexp As BinaryExpression = TryCast(e, BinaryExpression)
                If bexp IsNot Nothing Then
                    If bexp.ExpressionType <> BinaryOperationType.Equal AndAlso bexp.ExpressionType <> BinaryOperationType.And Then
                        Throw New NotSupportedException
                    End If

                    Dim eexp As IEntityPropertyExpression = Nothing
                    Dim v As IExpression = Nothing
                    If GetType(IEntityPropertyExpression).IsAssignableFrom(bexp.Left.GetType) Then
                        eexp = CType(bexp.Left, IEntityPropertyExpression)
                        v = bexp.Right
                    ElseIf GetType(IEntityPropertyExpression).IsAssignableFrom(bexp.Right.GetType) Then
                        eexp = CType(bexp.Right, IEntityPropertyExpression)
                        v = bexp.Left
                    End If

                    If TypeOf v Is IParameterExpression Then
                        re = re.ReplaceExpression(v, New ParameterExpression(ObjectMappingEngine.GetPropertyValue(obj, eexp.ObjectProperty.PropertyAlias, oschema)))
                    End If
                End If
            Next

            If re IsNot Me Then
                l = re.GetDynamicString
            End If
            Return l
        End Function

        Public Overrides Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef,
                                                ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam,
                                                ByVal almgr As IPrepareTable, ByVal contextInfo As IDictionary, ByVal stmtMode As MakeStatementMode,
                                                ByVal executor As Query.IExecutionContext) As String
            If Right Is Nothing Then
                Return Left.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextInfo, stmtMode, executor)
            End If
            'If typeof Left is EntityExpression
            Return "(" & Left.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextInfo, stmtMode, executor) & _
                stmt.BinaryOperator2String(_oper) & _
                Right.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextInfo, stmtMode, executor) & ")"
        End Function

        'Public Overrides Function RemoveExpression(ByVal f As IComplexExpression) As IComplexExpression
        '    If f.Equals(Me) Then
        '        Return Nothing
        '    End If

        '    Dim left_ As IExpression = Nothing, right_ As IExpression = Nothing

        '    If _oper = BinaryOperationType.And Then
        '        If Left IsNot Nothing Then
        '            If Left.Equals(f) Then
        '                left_ = New BinaryExpression("1", BinaryOperationType.Equal, "1")
        '            ElseIf (GetType(IComplexExpression).IsAssignableFrom(Left.GetType)) Then
        '                left_ = CType(Left, IComplexExpression).RemoveExpression(f)
        '            End If
        '        End If

        '        If Right IsNot Nothing Then
        '            If Right.Equals(f) Then
        '                right_ = New BinaryExpression("1", BinaryOperationType.Equal, "1")
        '            ElseIf (GetType(IComplexExpression).IsAssignableFrom(Right.GetType)) Then
        '                right_ = CType(Right, IComplexExpression).RemoveExpression(f)
        '            End If
        '        End If
        '    Else
        '        If Left IsNot Nothing Then
        '            If Left.Equals(f) Then
        '                If Right Is Nothing Then
        '                    left_ = New BinaryExpression("1", BinaryOperationType.Equal, "1")
        '                Else
        '                    left_ = New BinaryExpression("1", BinaryOperationType.Equal, "0")
        '                End If
        '            ElseIf (GetType(IComplexExpression).IsAssignableFrom(Left.GetType)) Then
        '                left_ = CType(Left, IComplexExpression).RemoveExpression(f)
        '            End If
        '        End If

        '        If Right IsNot Nothing Then
        '            If Right.Equals(f) Then
        '                right_ = New BinaryExpression("1", BinaryOperationType.Equal, "0")
        '            ElseIf (GetType(IComplexExpression).IsAssignableFrom(Right.GetType)) Then
        '                right_ = CType(Right, IComplexExpression).RemoveExpression(f)
        '            End If
        '        End If
        '    End If

        '    If Left IsNot left_ OrElse Right IsNot right_ Then
        '        Return New BetweenExpresssion(left_, right_)
        '    Else
        '        Return Me
        '    End If
        'End Function

        Public Function Test(ByVal mpe As ObjectMappingEngine, _
            ByVal oschema As Entities.Meta.IEntitySchema, ByVal obj As Worm.Entities._IEntity) As IParameterExpression.EvalResult Implements IHashable.Test

            If mpe Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim b As IParameterExpression.EvalResult = IParameterExpression.EvalResult.Unknown

            If _oper < BinaryOperationType.Between Then
                Dim l As Object = Nothing, r As Object = Nothing
                If GetValue(mpe, obj, oschema, Left, l) AndAlso GetValue(mpe, obj, oschema, Right, r) Then
                    b = Helper.Test(l, r, _oper, IsCaseSensitive(mpe), mpe)
                End If
            ElseIf _oper = BinaryOperationType.Between Then
                Dim l As Object = Nothing, lv As Object = Nothing, rv As Object = Nothing

                If GetBetweenValues(mpe, obj, oschema, l, lv, rv) Then
                    Dim i As Integer = CType(l, IComparable).CompareTo(lv)
                    If i >= 0 Then
                        i = CType(l, IComparable).CompareTo(rv)
                        If i <= 0 Then
                            b = IParameterExpression.EvalResult.Found
                        End If
                    End If
                End If
            ElseIf _oper <= BinaryOperationType.Or Then
                Dim left_ As IHashable = TryCast(Left, IHashable)

                If left_ IsNot Nothing Then
                    b = left_.Test(mpe, oschema, obj)
                    Dim _right As IHashable = TryCast(Right, IHashable)
                    If _right IsNot Nothing Then
                        If _oper = BinaryOperationType.And Then
                            If b = IParameterExpression.EvalResult.Found Then
                                b = _right.Test(mpe, oschema, obj)
                            End If
                        ElseIf _oper = BinaryOperationType.Or Then
                            If b <> IParameterExpression.EvalResult.Unknown Then
                                Dim r As IParameterExpression.EvalResult = _right.Test(mpe, oschema, obj)
                                If r <> IParameterExpression.EvalResult.Unknown Then
                                    If b <> IParameterExpression.EvalResult.Found Then
                                        b = r
                                    End If
                                Else
                                    b = r
                                End If
                            End If
                        End If
                    End If
                End If
            ElseIf _oper <= BinaryOperationType.RightShift Then
                Throw New NotSupportedException(OperationType2String(_oper))
            Else
                Throw New NotSupportedException(OperationType2String(_oper))
            End If

            'If Not Eval(TryCast(Right, IEntityPropertyExpression), TryCast(Left, IParameterExpression), b) Then
            '    If Not Eval(TryCast(Left, IEntityPropertyExpression), TryCast(Right, IParameterExpression), b) Then


            '    End If
            'End If

            Return b
        End Function

        Public Function Eval(ByVal mpe As ObjectMappingEngine, ByVal obj As Entities._IEntity, ByVal oschema As IEntitySchema, _
            ByRef v As Object) As Boolean Implements IEvaluable.Eval

            If _oper > BinaryOperationType.Or AndAlso _oper <= BinaryOperationType.RightShift Then
                Dim l As Object = Nothing, r As Object = Nothing
                If GetValue(mpe, obj, oschema, Left, l) AndAlso GetValue(mpe, obj, oschema, Right, r) Then
                    Return Helper.Eval(l, r, _oper, mpe, v)
                End If
            Else
                Throw New NotSupportedException(OperationType2String(_oper))
            End If
        End Function

        Protected Function GetBetweenValues(ByVal mpe As ObjectMappingEngine, _
            ByVal obj As _IEntity, ByVal oschema As IEntitySchema, _
            ByRef v As Object, ByRef lv As Object, ByRef rv As Object) As Boolean

            Dim bv As BetweenExpression = TryCast(Left, BetweenExpression)
            Dim exp As IExpression = Nothing
            If bv Is Nothing Then
                bv = TryCast(Right, BetweenExpression)
                exp = Left
            Else
                exp = Right
            End If

            If GetValue(mpe, obj, oschema, exp, v) AndAlso GetValue(mpe, obj, oschema, bv.Left, lv) AndAlso GetValue(mpe, obj, oschema, bv.Right, rv) Then
                Return True
            End If

            Return False
        End Function

        Public Overloads Function CanEval(ByVal mpe As ObjectMappingEngine) As Boolean Implements IEvaluable.CanEval
            'If _oper = BinaryOperationType.Equal Then
            '    Dim e As Boolean? = CanEval(TryCast(Left, IEntityPropertyExpression), TryCast(Right, IParameterExpression))

            '    If Not e.HasValue Then
            '        e = CanEval(TryCast(Right, IEntityPropertyExpression), TryCast(Left, IParameterExpression))
            '    End If

            '    Return e.HasValue AndAlso e.Value
            'ElseIf _oper = BinaryOperationType.And Then
            '    Dim l As IEvaluable = TryCast(Left, IEvaluable)
            '    Dim r As IEvaluable = TryCast(Right, IEvaluable)

            '    Return l IsNot Nothing AndAlso r IsNot Nothing AndAlso l.CanEval(mpe) AndAlso r.CanEval(mpe)
            'End If

            'Return False
            If _oper > BinaryOperationType.Or AndAlso _oper <= BinaryOperationType.RightShift Then
                If Helper.CanEval(Left, mpe) Then
                    If Right IsNot Nothing Then
                        Return Helper.CanEval(Right, mpe)
                    Else
                        Return True
                    End If
                End If
            End If
            Return False
        End Function

        Protected Overloads Function CanEval(ByVal eexp As IEntityPropertyExpression, ByVal pexp As IParameterExpression) As Boolean?
            If eexp IsNot Nothing Then
                If pexp IsNot Nothing Then
                    Return True
                End If
            End If

            Return Nothing
        End Function
    End Class

    Public Class BetweenExpression
        Inherits BinaryExpressionBase
        Implements IParameterExpression

        Public Sub New(ByVal left As Object, ByVal right As Object)
            MyBase.New(left, right)
        End Sub

        Public Sub New(ByVal left As IExpression, ByVal right As IExpression)
            MyBase.New(left, right)
        End Sub

        'Public Overrides Function Test(ByVal schema As ObjectMappingEngine, ByVal obj As Entities._IEntity, ByVal oschema As Entities.Meta.IEntitySchema) As IParameterExpression.EvalResult
        '    Throw New NotImplementedException
        'End Function

        'Public Overrides Function MakeDynamicString(ByVal schema As ObjectMappingEngine, ByVal oschema As Entities.Meta.IEntitySchema, ByVal obj As Entities.ICachedEntity) As String
        '    Throw New NotImplementedException
        'End Function

        Public Overrides Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef,
                                                ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam,
                                                ByVal almgr As IPrepareTable, ByVal contextInfo As IDictionary, ByVal stmtMode As MakeStatementMode,
                                                ByVal executor As Query.IExecutionContext) As String
            Return Left.MakeStatement(schema, fromClause, stmt, paramMgr, almgr, contextInfo, stmtMode, executor) & _
                " and " & _
                Right.MakeStatement(schema, fromClause, stmt, paramMgr, almgr, contextInfo, stmtMode, executor)
        End Function

        Protected Overrides ReadOnly Property BinaryType() As String
            Get
                Return "between"
            End Get
        End Property

        Public Overrides Function Clone() As Object
            Return New BetweenExpression(CloneExpression(Left), CloneExpression(Right))
        End Function

        Public Event ModifyValue(ByVal sender As IParameterExpression, ByVal args As IParameterExpression.ModifyValueArgs) Implements IParameterExpression.ModifyValue

        'Public Overloads Function Test(ByVal oper As BinaryOperationType, ByVal v As Object, ByVal mpe As ObjectMappingEngine) As IParameterExpression.EvalResult Implements IParameterExpression.Test
        '    Dim r As IParameterExpression.EvalResult = IParameterExpression.EvalResult.NotFound

        '    If oper = BinaryOperationType.Between Then
        '        Dim fe As IParameterExpression = TryCast(Left, IParameterExpression)
        '        Dim se As IParameterExpression = TryCast(Right, IParameterExpression)

        '        If fe IsNot Nothing AndAlso se IsNot Nothing Then
        '            Dim i As Integer = CType(v, IComparable).CompareTo(fe.Value)
        '            If i >= 0 Then
        '                i = CType(v, IComparable).CompareTo(se.Value)
        '                If i <= 0 Then
        '                    r = IParameterExpression.EvalResult.Found
        '                End If
        '            End If
        '        End If
        '    Else
        '        Throw New InvalidOperationException(String.Format("Invalid operation {0} for BetweenValue", oper))
        '    End If
        '    Return r
        'End Function

        Protected ReadOnly Property _Value() As Object Implements IParameterExpression.Value
            Get
                Return Value
            End Get
        End Property

        'Public Overrides Function RemoveExpression(ByVal f As IComplexExpression) As IComplexExpression
        '    If f.Equals(Me) Then
        '        Return Nothing
        '    End If

        '    Return Me
        'End Function

    End Class
End Namespace