Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Query

Namespace Expressions2

    <Serializable()> _
    Public MustInherit Class UnaryExpressionBase
        Implements IUnaryExpression

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

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            Return "litval"
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            'do nothing
        End Sub

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Return New IExpression() {Me}
        End Function

        Public Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
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
    End Class

    <Serializable()> _
    Public Class DBNullExpression
        Inherits LiteralExpression
        Implements IParameterExpression

        Public Sub New()
            MyBase.New("null")
        End Sub

        'Public Function Test(ByVal oper As BinaryOperationType, ByVal v As Object, ByVal mpe As ObjectMappingEngine) As IParameterExpression.EvalResult Implements IParameterExpression.Test
        '    If oper = BinaryOperationType.Is Then
        '        If v Is Nothing Then
        '            Return IParameterExpression.EvalResult.Found
        '        Else
        '            Return IParameterExpression.EvalResult.NotFound
        '        End If
        '    ElseIf oper = BinaryOperationType.IsNot Then
        '        If v IsNot Nothing Then
        '            Return IParameterExpression.EvalResult.Found
        '        Else
        '            Return IParameterExpression.EvalResult.NotFound
        '        End If
        '    Else
        '        Throw New NotSupportedException(String.Format("Operation {0} is not supported for IsNull statement", oper))
        '    End If
        'End Function

        Public ReadOnly Property Value() As Object Implements IParameterExpression.Value
            Get
                Return Nothing
            End Get
        End Property

        Public Event ModifyValue(ByVal sender As IParameterExpression, ByVal args As IParameterExpression.ModifyValueArgs) Implements IParameterExpression.ModifyValue
    End Class

    <Serializable()> _
    Public Class QueryExpression
        Implements IExpression

        Private _q As Query.QueryCmd

        Public Sub New(ByVal q As Query.QueryCmd)
            _q = q
        End Sub

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Return New IExpression() {Me}
        End Function

        Public Function MakeStatement(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            Dim sb As New StringBuilder
            sb.Append("(")
            sb.Append(stmt.MakeQueryStatement(schema, fromClause, contextFilter, _q, paramMgr, almgr))
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

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            Return _q.GetStaticString(mpe, contextFilter)
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextFilter As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            _q.Prepare(executor, schema, contextFilter, stmt, isAnonym)
        End Sub
    End Class

    <Serializable()> _
    Public Class CustomExpression
        Implements IExpression

        Public Sub New(ByVal format As String)
            _f = format
        End Sub

        Public Sub New(ByVal format As String, ByVal ParamArray v() As IExpression)
            _f = format
            _v = v
        End Sub

        Public Sub New(ByVal format As String, ByVal ParamArray v() As IGetExpression)
            _f = format
            _v = Array.ConvertAll(v, Function(p) p.Expression)
        End Sub

        Private _f As String
        Public ReadOnly Property Format() As String
            Get
                Return _f
            End Get
        End Property

        Private _v() As IExpression
        Public ReadOnly Property Values() As IExpression()
            Get
                Return _v
            End Get
        End Property

        Public Function GetExpressions() As IExpression() Implements IExpression.GetExpressions
            Dim l As New List(Of IExpression)
            l.Add(Me)
            If _v IsNot Nothing Then
                For Each e As IExpression In Values
                    l.AddRange(e.GetExpressions)
                Next
            End If
            Return l.ToArray
        End Function

        Public Function MakeStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef, _
            ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal contextFilter As Object, ByVal stmtMode As MakeStatementMode, ByVal executor As Query.IExecutionContext) As String Implements IExpression.MakeStatement
            If _v IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In _v
                    l.Add(v.MakeStatement(mpe, fromClause, stmt, paramMgr, almgr, contextFilter, stmtMode And Not MakeStatementMode.AddColumnAlias, executor))
                Next
                Return String.Format(_f, l.ToArray)
            Else
                Return _f
            End If
        End Function

        Public ReadOnly Property ShouldUse() As Boolean Implements IExpression.ShouldUse
            Get
                Return Not String.IsNullOrEmpty(_f)
            End Get
        End Property

        Public ReadOnly Property Expression() As IExpression Implements IGetExpression.Expression
            Get
                Return Me
            End Get
        End Property

        Public Overloads Function Equals(ByVal f As IQueryElement) As Boolean Implements IQueryElement.Equals
            Return Equals(TryCast(f, CustomExpression))
        End Function

        Public Overloads Function Equals(ByVal f As CustomExpression) As Boolean
            If f Is Nothing Then
                Return False
            End If

            Return _f = f._f AndAlso GetDynamicString() = f.GetDynamicString
        End Function

        Public Function GetDynamicString() As String Implements IQueryElement.GetDynamicString
            If _v IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In _v
                    l.Add(v.GetDynamicString())
                Next
                Return String.Format(_f, l.ToArray)
            Else
                Return _f
            End If
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            If _v IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In _v
                    l.Add(v.GetStaticString(mpe, contextFilter))
                Next
                Return String.Format(_f, l.ToArray)
            Else
                Return _f
            End If
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            If _v IsNot Nothing Then
                For Each e As IExpression In Values
                    e.Prepare(executor, mpe, contextFilter, stmt, isAnonym)
                Next
            End If
        End Sub
    End Class

    Module Helper
        Public Const EmptyHash As String = "fd_empty_hash_aldf"

        Public Function CloneExpression(ByVal exp As IExpression) As IExpression
            Dim c As ICloneable = TryCast(exp, ICloneable)
            If c IsNot Nothing Then
                Return CType(c.Clone, IExpression)
            End If
            Return exp
        End Function

        Public Function GetSelectedEntities(ByVal exp As IExpression) As ICollection(Of SelectUnion)
            Dim l As New List(Of SelectUnion)
            For Each e As Expressions2.IExpression In exp.GetExpressions
                Dim ee As Expressions2.IEntityPropertyExpression = TryCast(e, Expressions2.IEntityPropertyExpression)
                If ee IsNot Nothing Then
                    l.Add(New SelectUnion(ee.ObjectProperty.Entity))
                Else
                    Dim te As Expressions2.TableExpression = TryCast(e, Expressions2.TableExpression)
                    If te IsNot Nothing Then
                        l.Add(New SelectUnion(te.SourceFragment))
                    End If
                End If
            Next

            Return l
        End Function

        Public Function GetEntityExpressions(ByVal exp As IExpression) As ICollection(Of IEntityPropertyExpression)
            Dim l As New List(Of IEntityPropertyExpression)
            For Each e As Expressions2.IExpression In exp.GetExpressions
                Dim ee As Expressions2.IEntityPropertyExpression = TryCast(e, Expressions2.IEntityPropertyExpression)
                If ee IsNot Nothing Then
                    l.Add(ee)
                End If
            Next

            Return l
        End Function

        Public Function OperationType2String(ByVal oper As UnaryOperationType) As String
            Select Case oper
                Case UnaryOperationType.Negate
                    Return "neg"
                Case UnaryOperationType.Not
                    Return "not"
                Case Else
                    Throw New NotSupportedException(oper.ToString)
            End Select
        End Function

        Public Function OperationType2String(ByVal oper As BinaryOperationType) As String
            Select Case oper
                Case BinaryOperationType.Equal
                    Return "Equal"
                Case BinaryOperationType.GreaterEqualThan
                    Return "GreaterEqualThan"
                Case BinaryOperationType.GreaterThan
                    Return "GreaterThan"
                Case BinaryOperationType.In
                    Return "In"
                Case BinaryOperationType.LessEqualThan
                    Return "LessEqualThan"
                Case BinaryOperationType.NotEqual
                    Return "NotEqual"
                Case BinaryOperationType.NotIn
                    Return "NotIn"
                Case BinaryOperationType.Like
                    Return "Like"
                Case BinaryOperationType.LessThan
                    Return "LessThan"
                Case BinaryOperationType.Is
                    Return "Is"
                Case BinaryOperationType.IsNot
                    Return "IsNot"
                Case BinaryOperationType.Exists
                    Return "Exists"
                Case BinaryOperationType.NotExists
                    Return "NotExists"
                Case BinaryOperationType.Between
                    Return "Between"
                Case BinaryOperationType.And
                    Return "And"
                Case BinaryOperationType.Or
                    Return "Or"
                Case BinaryOperationType.ExclusiveOr
                    Return "EOr"
                Case BinaryOperationType.BitAnd
                    Return "BAnd"
                Case BinaryOperationType.BitOr
                    Return "BOr"
                Case BinaryOperationType.Add
                    Return "Add"
                Case BinaryOperationType.Subtract
                    Return "Subtract"
                Case BinaryOperationType.Divide
                    Return "Divide"
                Case BinaryOperationType.Multiply
                    Return "Mul"
                Case BinaryOperationType.Modulo
                    Return "Mod"
                Case BinaryOperationType.LeftShift
                    Return "LShift"
                Case BinaryOperationType.RightShift
                    Return "RShift"
                Case Else
                    Throw New ObjectMappingException("Operation " & oper & " not supported")
            End Select
        End Function

        Public Function Test(ByVal filterValue As Object, ByVal evaluatedValue As Object, _
                             ByVal oper As BinaryOperationType, ByVal [case] As Boolean, _
                             ByVal mpe As ObjectMappingEngine) As IParameterExpression.EvalResult

            Dim r As IParameterExpression.EvalResult = IParameterExpression.EvalResult.NotFound

            If filterValue IsNot Nothing AndAlso evaluatedValue IsNot Nothing Then
                Dim vt As Type = evaluatedValue.GetType()
                Dim valt As Type = filterValue.GetType
                If Not vt.IsAssignableFrom(valt) AndAlso ( _
                    (vt.IsPrimitive AndAlso valt.IsPrimitive) OrElse _
                    (vt.IsValueType AndAlso valt.IsValueType)) Then
                    filterValue = Convert.ChangeType(filterValue, evaluatedValue.GetType)
                ElseIf vt.IsArray <> valt.IsArray Then
                    Return IParameterExpression.EvalResult.Unknown
                End If
            End If

            Select Case oper
                Case BinaryOperationType.Equal
                    If Equals(evaluatedValue, filterValue) Then
                        r = IParameterExpression.EvalResult.Found
                    ElseIf evaluatedValue IsNot Nothing Then
                        Dim vt As Type = evaluatedValue.GetType()
                        If GetType(IKeyEntity).IsAssignableFrom(vt) Then
                            If Equals(CType(evaluatedValue, IKeyEntity).Identifier, filterValue) Then
                                r = IParameterExpression.EvalResult.Found
                            End If
                        ElseIf GetType(ICachedEntity).IsAssignableFrom(vt) Then
                            Dim pks() As PKDesc = CType(evaluatedValue, ICachedEntity).GetPKValues
                            If pks.Length <> 1 Then
                                Throw New ObjectMappingException(String.Format("Type {0} has complex primary key", vt))
                            End If
                            If Equals(pks(0).Value, filterValue) Then
                                r = IParameterExpression.EvalResult.Found
                            End If
                        ElseIf ObjectMappingEngine.IsEntityType(vt, mpe) Then
                            Dim pks As IList(Of EntityPropertyAttribute) = mpe.GetPrimaryKeys(vt)
                            If pks.Count <> 1 Then
                                Throw New ObjectMappingException(String.Format("Type {0} has complex primary key", vt))
                            End If
                            If Equals(mpe.GetPropertyValue(evaluatedValue, pks(0).PropertyAlias, Nothing), filterValue) Then
                                r = IParameterExpression.EvalResult.Found
                            End If
                        End If
                    End If
                Case BinaryOperationType.GreaterEqualThan
                    Dim c As IComparable = CType(evaluatedValue, IComparable)
                    If c Is Nothing Then
                        If filterValue Is Nothing Then
                            r = IParameterExpression.EvalResult.Unknown
                        Else
                            r = IParameterExpression.EvalResult.NotFound
                        End If
                    Else
                        Dim i As Integer = c.CompareTo(filterValue)
                        If i >= 0 Then
                            r = IParameterExpression.EvalResult.Found
                        End If
                    End If
                Case BinaryOperationType.GreaterThan
                    Dim c As IComparable = CType(evaluatedValue, IComparable)
                    If c Is Nothing Then
                        If filterValue Is Nothing Then
                            r = IParameterExpression.EvalResult.Unknown
                        Else
                            r = IParameterExpression.EvalResult.NotFound
                        End If
                    Else
                        Dim i As Integer = c.CompareTo(filterValue)
                        If i > 0 Then
                            r = IParameterExpression.EvalResult.Found
                        End If
                    End If
                Case BinaryOperationType.LessEqualThan
                    Dim c As IComparable = CType(filterValue, IComparable)
                    If c Is Nothing Then
                        If evaluatedValue Is Nothing Then
                            r = IParameterExpression.EvalResult.Unknown
                        Else
                            r = IParameterExpression.EvalResult.NotFound
                        End If
                    Else
                        Dim i As Integer = c.CompareTo(evaluatedValue)
                        If i >= 0 Then
                            r = IParameterExpression.EvalResult.Found
                        End If
                    End If
                Case BinaryOperationType.LessThan
                    Dim c As IComparable = CType(filterValue, IComparable)
                    If c Is Nothing Then
                        If evaluatedValue Is Nothing Then
                            r = IParameterExpression.EvalResult.Unknown
                        Else
                            r = IParameterExpression.EvalResult.NotFound
                        End If
                    Else
                        Dim i As Integer = c.CompareTo(evaluatedValue)
                        If i > 0 Then
                            r = IParameterExpression.EvalResult.Found
                        End If
                    End If
                Case BinaryOperationType.NotEqual
                    If Not Equals(evaluatedValue, filterValue) Then
                        r = IParameterExpression.EvalResult.Found
                    End If
                Case BinaryOperationType.Like
                    If filterValue Is Nothing OrElse evaluatedValue Is Nothing Then
                        r = IParameterExpression.EvalResult.Unknown
                    Else
                        Dim par As String = CStr(filterValue)
                        Dim str As String = CStr(evaluatedValue)
                        r = IParameterExpression.EvalResult.NotFound
                        Dim sc As StringComparison = StringComparison.InvariantCulture
                        If Not [case] Then
                            sc = StringComparison.InvariantCultureIgnoreCase
                        End If
                        If par.StartsWith("%") Then
                            If par.EndsWith("%") Then
                                If str.IndexOf(par.Trim("%"c), sc) >= 0 Then
                                    r = IParameterExpression.EvalResult.Found
                                End If
                            Else
                                If str.EndsWith(par.TrimStart("%"c), sc) Then
                                    r = IParameterExpression.EvalResult.Found
                                End If
                            End If
                        ElseIf par.EndsWith("%") Then
                            If str.StartsWith(par.TrimEnd("%"c), sc) Then
                                r = IParameterExpression.EvalResult.Found
                            End If
                        ElseIf par.Equals(str, sc) Then
                            r = IParameterExpression.EvalResult.Found
                        End If
                    End If
                Case BinaryOperationType.In
                    For Each o As Object In CType(evaluatedValue, IEnumerable)
                        If Object.Equals(o, filterValue) Then
                            r = IParameterExpression.EvalResult.Found
                            Exit For
                        End If
                    Next
                Case BinaryOperationType.NotIn
                    For Each o As Object In CType(evaluatedValue, IEnumerable)
                        If Object.Equals(o, filterValue) Then
                            r = IParameterExpression.EvalResult.NotFound
                            Exit For
                        End If
                    Next
                Case BinaryOperationType.Is
                    If evaluatedValue Is Nothing Then
                        Return IParameterExpression.EvalResult.Found
                    Else
                        Return IParameterExpression.EvalResult.NotFound
                    End If
                Case BinaryOperationType.IsNot
                    If evaluatedValue IsNot Nothing Then
                        Return IParameterExpression.EvalResult.Found
                    Else
                        Return IParameterExpression.EvalResult.NotFound
                    End If
                Case Else
                    r = IParameterExpression.EvalResult.Unknown
            End Select

            Return r
        End Function

        Public Function Eval(ByVal filterValue As Object, ByVal evaluatedValue As Object, _
                             ByVal oper As BinaryOperationType, _
                             ByVal mpe As ObjectMappingEngine, ByRef v As Object) As Boolean

            Select Case oper
                Case BinaryOperationType.ExclusiveOr
                    If IsNumeric(filterValue) Then
                        v = Convert.ChangeType(CLng(filterValue) Xor CLng(evaluatedValue), filterValue.GetType)
                        Return True
                    End If
                Case BinaryOperationType.BitAnd
                    If IsNumeric(filterValue) Then
                        v = Convert.ChangeType(CLng(filterValue) And CLng(evaluatedValue), filterValue.GetType)
                        Return True
                    End If
                Case BinaryOperationType.BitOr
                    If IsNumeric(filterValue) Then
                        v = Convert.ChangeType(CLng(filterValue) Or CLng(evaluatedValue), filterValue.GetType)
                        Return True
                    End If
                Case BinaryOperationType.Add
                    If IsNumeric(filterValue) Then
                        v = Convert.ChangeType(CDec(filterValue) + CDec(evaluatedValue), filterValue.GetType)
                        Return True
                    ElseIf TypeOf filterValue Is String Then
                        v = CStr(filterValue) & CStr(evaluatedValue)
                    End If
                Case BinaryOperationType.Subtract
                    If IsNumeric(filterValue) Then
                        v = Convert.ChangeType(CDec(filterValue) - CDec(evaluatedValue), filterValue.GetType)
                        Return True
                    End If
                Case BinaryOperationType.Divide
                    If IsNumeric(filterValue) Then
                        v = Convert.ChangeType(CDec(filterValue) / CDec(evaluatedValue), filterValue.GetType)
                        Return True
                    End If
                Case BinaryOperationType.Multiply
                    If IsNumeric(filterValue) Then
                        v = Convert.ChangeType(CDec(filterValue) * CDec(evaluatedValue), filterValue.GetType)
                        Return True
                    End If
                Case BinaryOperationType.Modulo
                    If IsNumeric(filterValue) Then
                        v = Convert.ChangeType(CDec(filterValue) Mod CDec(evaluatedValue), filterValue.GetType)
                        Return True
                    End If
                Case BinaryOperationType.LeftShift
                    If IsNumeric(filterValue) Then
                        v = Convert.ChangeType(CLng(filterValue) << CInt(evaluatedValue), filterValue.GetType)
                        Return True
                    End If
                Case BinaryOperationType.RightShift
                    If IsNumeric(filterValue) Then
                        v = Convert.ChangeType(CLng(filterValue) >> CInt(evaluatedValue), filterValue.GetType)
                        Return True
                    End If
                Case Else
                    Throw New NotSupportedException(oper.ToString)
            End Select

            Return False
        End Function

        Public Function GetValue(ByVal mpe As ObjectMappingEngine, _
            ByVal obj As _IEntity, ByVal oschema As IEntitySchema, ByVal exp As IExpression, ByRef v As Object) As Boolean

            Dim eexp As IEntityPropertyExpression = TryCast(exp, IEntityPropertyExpression)

            If eexp IsNot Nothing Then
                Dim t As Type = obj.GetType
                Dim rt As Type = eexp.ObjectProperty.Entity.GetRealType(mpe)
                If rt Is t Then
                    v = mpe.GetPropertyValue(obj, eexp.ObjectProperty.PropertyAlias, oschema)
                    Return True
                Else
                    Throw New NotSupportedException(String.Format("Different types in expression ({0}) and object ({1})", rt, t))
                End If
            Else
                Dim pexp As IParameterExpression = TryCast(exp, IParameterExpression)
                If pexp IsNot Nothing Then
                    v = pexp.Value
                    Return True
                Else
                    Dim cexp As IEvaluable = TryCast(exp, IEvaluable)
                    If cexp IsNot Nothing Then
                        Return cexp.Eval(mpe, obj, oschema, v)
                    Else
                        Throw New NotSupportedException(String.Format("Expression {0} is not evaluable", exp.GetStaticString(mpe, Nothing)))
                    End If
                End If
            End If
        End Function

        Public Function CanEval(ByVal exp As IExpression, ByVal mpe As ObjectMappingEngine) As Boolean
            Dim eexp As IEntityPropertyExpression = TryCast(exp, IEntityPropertyExpression)
            If eexp IsNot Nothing Then
                Return True
            Else
                Dim pexp As IParameterExpression = TryCast(exp, IParameterExpression)
                If pexp IsNot Nothing Then
                    Return True
                Else
                    Dim l As IEvaluable = TryCast(exp, IEvaluable)
                    Return l IsNot Nothing AndAlso l.CanEval(mpe)
                End If
            End If
        End Function
    End Module
End Namespace