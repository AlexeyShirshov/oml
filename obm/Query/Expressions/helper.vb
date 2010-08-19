Imports System.Collections.Generic
Imports Worm.Query
Imports Worm.Entities
Imports Worm.Entities.Meta

Namespace Expressions2
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
                    If Not l.Exists(Function(d) ee.ObjectProperty.Entity.Equals(d.EntityUnion)) Then
                        l.Add(New SelectUnion(ee.ObjectProperty.Entity))
                    End If
                Else
                    Dim te As Expressions2.TableExpression = TryCast(e, Expressions2.TableExpression)
                    If te IsNot Nothing Then
                        If Not l.Exists(Function(d) te.SourceFragment Is d.SourceFragment) Then
                            l.Add(New SelectUnion(te.SourceFragment))
                        End If
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
                            If Equals(mpe.GetPropertyValue(evaluatedValue, mpe.GetSinglePK(vt)), filterValue) Then
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
                    v = ObjectMappingEngine.GetPropertyValue(obj, eexp.ObjectProperty.PropertyAlias, oschema, Nothing)
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