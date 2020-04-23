Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.Query
Imports Worm.Expressions2
Imports System.Linq

Namespace Criteria.Values
    <Serializable()> _
    Public Class ScalarValue
        Implements IEvaluableValue

        Private _v As Object
        Private _pname As String
        Private _case As Boolean
        'Private _f As IEntityFilter

        Protected Sub New()
        End Sub

        'Public Sub New(ByVal value As Object)
        '    _v = value
        'End Sub

        Public Sub New(ByVal value As Object)
            _v = value
        End Sub

        Public Sub New(ByVal value As Object, ByVal caseSensitive As Boolean)
            _v = value
            _case = caseSensitive
        End Sub

        Public Overridable Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, ByVal prepare As PrepareValueDelegate, _
                          ByVal contextInfo As IDictionary, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String Implements IEvaluableValue.GetParam

            Dim v As Object = _v
            If prepare IsNot Nothing Then
                v = prepare(schema, v)
            End If

            If stmt.SupportParams Then
                If paramMgr Is Nothing Then
                    Throw New ArgumentNullException("paramMgr")
                End If
                'Dim p As String = _pname
                'If String.IsNullOrEmpty(p) Then
                '    p = paramMgr.CreateParam(v)
                '    If paramMgr.NamedParams Then
                '        _pname = p
                '    End If
                'Else
                '    p = paramMgr.AddParam(_pname, v)
                '    _pname = p
                'End If
                'Return p
                _pname = paramMgr.AddParam(_pname, v)
                Return _pname
            Else
                Return "'" & v.ToString & "'"
            End If

        End Function

        'Protected Property Value() As Object
        '    Get
        '        Return _v
        '    End Get
        '    Set(ByVal value As Object)
        '        _v = value
        '    End Set
        'End Property

        Public Property CaseSensitive() As Boolean
            Get
                Return _case
            End Get
            Protected Set(ByVal value As Boolean)
                _case = value
            End Set
        End Property

        Public Overridable Function _ToString() As String Implements IFilterValue._ToString
            If _v IsNot Nothing Then
                If TypeOf _v Is Decimal Then
                    Return CType(_v, Decimal).ToString("G29")
                Else
                    Return _v.ToString
                End If
            End If
            Return String.Empty
        End Function

        Public Overridable ReadOnly Property Value() As Object Implements IEvaluableValue.Value
            Get
                Return _v
            End Get
        End Property

        Protected Sub SetValue(ByVal v As Object)
            _v = v
        End Sub

        Protected Overridable Function GetValue(ByVal v As Object, ByVal template As OrmFilterTemplate, ByRef r As IEvaluableValue.EvalResult) As Object
            r = IEvaluableValue.EvalResult.Unknown
            Return Value
        End Function

        Public Overridable Function Eval(ByVal evaluatedValue As Object, ByVal mpe As ObjectMappingEngine, ByVal template As OrmFilterTemplate) As IEvaluableValue.EvalResult Implements IEvaluableValue.Eval
            Dim r As IEvaluableValue.EvalResult

            Dim fv As IEvaluableValue = TryCast(evaluatedValue, IEvaluableValue)
            If fv IsNot Nothing Then
                evaluatedValue = fv.Value
            End If

            Dim filterValue As Object = GetValue(evaluatedValue, template, r)
            If r <> IEvaluableValue.EvalResult.Unknown Then
                Return r
            Else
                r = IEvaluableValue.EvalResult.NotFound
            End If
            Try
                If filterValue IsNot Nothing AndAlso evaluatedValue IsNot Nothing Then
                    Dim vt As Type = evaluatedValue.GetType()
                    Dim valt As Type = filterValue.GetType
                    If Not vt.IsAssignableFrom(valt) AndAlso ( _
                        (vt.IsPrimitive AndAlso valt.IsPrimitive) OrElse _
                        (vt.IsValueType AndAlso valt.IsValueType)) Then
                        filterValue = Convert.ChangeType(filterValue, evaluatedValue.GetType)
                    ElseIf vt.IsArray <> valt.IsArray Then
                        Return IEvaluableValue.EvalResult.Unknown
                    End If
                End If

                Select Case template.Operation
                    Case FilterOperation.Equal
                        If Equals(evaluatedValue, filterValue) Then
                            r = IEvaluableValue.EvalResult.Found
                        ElseIf evaluatedValue IsNot Nothing Then
                            Dim vt As Type = evaluatedValue.GetType()
                            Dim valt As Type = filterValue.GetType
                            If GetType(ISinglePKEntity).IsAssignableFrom(vt) Then
                                If Equals(CType(evaluatedValue, ISinglePKEntity).Identifier, filterValue) Then
                                    r = IEvaluableValue.EvalResult.Found
                                End If
                            ElseIf GetType(ICachedEntity).IsAssignableFrom(vt) Then
                                Dim pks As IEnumerable(Of PKDesc) = CType(evaluatedValue, ICachedEntity).GetPKValues(Nothing)
                                If pks.count <> 1 Then
                                    Throw New ObjectMappingException(String.Format("Type {0} has complex primary key", vt))
                                End If
                                If Equals(pks(0).Value, filterValue) Then
                                    r = IEvaluableValue.EvalResult.Found
                                End If
                            ElseIf ObjectMappingEngine.IsEntityType(vt) Then
                                If Equals(mpe.GetPropertyValue(evaluatedValue, mpe.GetPrimaryKey(vt)), filterValue) Then
                                    r = IEvaluableValue.EvalResult.Found
                                End If
                            ElseIf GetType(ISinglePKEntity).IsAssignableFrom(valt) Then
                                If Equals(CType(filterValue, ISinglePKEntity).Identifier, evaluatedValue) Then
                                    r = IEvaluableValue.EvalResult.Found
                                End If
                            ElseIf GetType(ICachedEntity).IsAssignableFrom(valt) Then
                                Dim pks As IEnumerable(Of PKDesc) = CType(filterValue, ICachedEntity).GetPKValues(Nothing)
                                If pks.Count <> 1 Then
                                    Throw New ObjectMappingException(String.Format("Type {0} has complex primary key", vt))
                                End If
                                If Equals(pks(0).Value, evaluatedValue) Then
                                    r = IEvaluableValue.EvalResult.Found
                                End If
                            ElseIf ObjectMappingEngine.IsEntityType(valt) Then
                                If Equals(mpe.GetPropertyValue(filterValue, mpe.GetPrimaryKey(valt)), evaluatedValue) Then
                                    r = IEvaluableValue.EvalResult.Found
                                End If
                            End If
                        End If
                    Case FilterOperation.GreaterEqualThan
                        Dim c As IComparable = CType(evaluatedValue, IComparable)
                        If c Is Nothing Then
                            If filterValue Is Nothing Then
                                r = IEvaluableValue.EvalResult.Unknown
                            Else
                                r = IEvaluableValue.EvalResult.NotFound
                            End If
                        Else
                            Dim i As Integer = c.CompareTo(filterValue)
                            If i >= 0 Then
                                r = IEvaluableValue.EvalResult.Found
                            End If
                        End If
                    Case FilterOperation.GreaterThan
                        Dim c As IComparable = CType(evaluatedValue, IComparable)
                        If c Is Nothing Then
                            If filterValue Is Nothing Then
                                r = IEvaluableValue.EvalResult.Unknown
                            Else
                                r = IEvaluableValue.EvalResult.NotFound
                            End If
                        Else
                            Dim i As Integer = c.CompareTo(filterValue)
                            If i > 0 Then
                                r = IEvaluableValue.EvalResult.Found
                            End If
                        End If
                    Case FilterOperation.LessEqualThan
                        Dim c As IComparable = CType(filterValue, IComparable)
                        If c Is Nothing Then
                            If evaluatedValue Is Nothing Then
                                r = IEvaluableValue.EvalResult.Unknown
                            Else
                                r = IEvaluableValue.EvalResult.NotFound
                            End If
                        Else
                            Dim i As Integer = c.CompareTo(evaluatedValue)
                            If i >= 0 Then
                                r = IEvaluableValue.EvalResult.Found
                            End If
                        End If
                    Case FilterOperation.LessThan
                        Dim c As IComparable = CType(filterValue, IComparable)
                        If c Is Nothing Then
                            If evaluatedValue Is Nothing Then
                                r = IEvaluableValue.EvalResult.Unknown
                            Else
                                r = IEvaluableValue.EvalResult.NotFound
                            End If
                        Else
                            Dim i As Integer = c.CompareTo(evaluatedValue)
                            If i > 0 Then
                                r = IEvaluableValue.EvalResult.Found
                            End If
                        End If
                    Case FilterOperation.NotEqual
                        If Not Equals(evaluatedValue, filterValue) Then
                            r = IEvaluableValue.EvalResult.Found
                        End If
                    Case FilterOperation.Like
                        If filterValue Is Nothing OrElse evaluatedValue Is Nothing Then
                            r = IEvaluableValue.EvalResult.Unknown
                        Else
                            Dim par As String = CStr(filterValue)
                            Dim str As String = CStr(evaluatedValue)
                            r = IEvaluableValue.EvalResult.NotFound
                            Dim [case] As StringComparison = StringComparison.InvariantCulture
                            If Not _case Then
                                [case] = StringComparison.InvariantCultureIgnoreCase
                            End If
                            If par.StartsWith("%") Then
                                If par.EndsWith("%") Then
                                    If str.IndexOf(par.Trim("%"c), [case]) >= 0 Then
                                        r = IEvaluableValue.EvalResult.Found
                                    End If
                                Else
                                    If str.EndsWith(par.TrimStart("%"c), [case]) Then
                                        r = IEvaluableValue.EvalResult.Found
                                    End If
                                End If
                            ElseIf par.EndsWith("%") Then
                                If str.StartsWith(par.TrimEnd("%"c), [case]) Then
                                    r = IEvaluableValue.EvalResult.Found
                                End If
                            ElseIf par.Equals(str, [case]) Then
                                r = IEvaluableValue.EvalResult.Found
                            End If
                        End If
                    Case Else
                        r = IEvaluableValue.EvalResult.Unknown
                End Select
            Catch ex As InvalidCastException
                If template IsNot Nothing Then
                    'CoreFramework.Debugging.Stack.PreserveStackTrace(ex)
                    Throw New InvalidOperationException(String.Format("Cannot eval field {4}.{0} of type {1} through value {2} of type {3}. Operation {5}. Stack {6}", _
                        template.PropertyAlias, filterValue.GetType, evaluatedValue, evaluatedValue.GetType, _
                        If(template.ObjectSource.AnyType Is Nothing, template.ObjectSource.AnyEntityName, template.ObjectSource.AnyType.ToString), template.Operation, ex.StackTrace), ex)
                End If
            End Try
            Return r
        End Function

        Public Overridable ReadOnly Property ShouldUse() As Boolean Implements IFilterValue.ShouldUse
            Get
                Return True
            End Get
        End Property

        Public Overridable Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            Return "scalarval"
        End Function

        Public Overridable Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            'do nothing
        End Sub

        Protected Overridable Function _Clone() As Object Implements ICloneable.Clone
            Return Clone
        End Function

        Public Function Clone() As ScalarValue
            Return New ScalarValue(Value, _case)
        End Function

        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, ScalarValue))
        End Function

        Public Function CopyTo(target As ScalarValue) As Boolean
            If target Is Nothing Then
                Return False
            End If

            target._case = _case
            target._v = Value

            Return True
        End Function

    End Class
End Namespace