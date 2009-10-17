Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.Query
Imports Worm.Expressions2

Namespace Criteria.Values

    '<Serializable()> _
    'Public Class RefValue
    '    Implements IFilterValue

    '    Private _num As Integer

    '    Public Sub New(ByVal num As Integer)
    '        _num = num
    '    End Sub

    '    Public Function _ToString() As String Implements IFilterValue._ToString
    '        Return _num.ToString
    '    End Function

    '    Public Function GetParam(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
    '                      ByVal almgr As IPrepareTable, ByVal prepare As PrepareValueDelegate, _
    '                      ByVal filterInfo As Object, ByVal inSelect As Boolean) As String Implements IFilterValue.GetParam
    '        Return aliases(_num)
    '    End Function

    '    Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
    '        Return "refval"
    '    End Function
    'End Class

    <Serializable()> _
    Public Class CustomValue
        Inherits TemplateBase
        Implements IFilterValue

        Private _f As String
        Public ReadOnly Property Format() As String
            Get
                Return _f
            End Get
        End Property

        Private _exp As IExpression
        Public ReadOnly Property Values() As IExpression
            Get
                Return _exp
            End Get
        End Property

        Private _filter As Boolean

        Public Sub New(ByVal format As String)
            _f = format
            _filter = True
        End Sub

        Public Sub New(ByVal format As String, ByVal values As IExpression)
            _f = format
            _exp = values
            _filter = True
        End Sub

        'Public Sub New(ByVal format As String, ByVal ParamArray values() As SelectExpressionOld)
        '    _f = format
        '    _v = Array.ConvertAll(values, Function(se As SelectExpressionOld) New SelectExpressionValue(se))
        '    _filter = True
        'End Sub

        Public Sub New(ByVal oper As FilterOperation, ByVal format As String, ByVal values As IExpression)
            MyBase.New(oper)
            _f = format
            _exp = values
        End Sub

        Public Overrides Function _ToString() As String Implements IQueryElement._ToString
            If _exp IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In _exp.GetExpressions
                    l.Add(v.GetDynamicString)
                Next
                If _filter Then
                    Return String.Format(_f, l.ToArray)
                Else
                    Return String.Format(_f, l.ToArray) & OperationString
                End If
            Else
                If _filter Then
                    Return _f & OperationString
                Else
                    Return _f
                End If
            End If
        End Function

        Protected ReadOnly Property OperationString() As String
            Get
                Return TemplateBase.OperToStringInternal(Operation)
            End Get
        End Property

        Public Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, ByVal prepare As PrepareValueDelegate, _
                          ByVal filterInfo As Object, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String Implements IFilterValue.GetParam
            'Dim values As List(Of String) = ObjectMappingEngine.ExtractValues(schema, stmt, almgr, _v)

            'Return String.Format(_f, values.ToArray)
            If _exp IsNot Nothing Then
                'Dim l As New List(Of String)
                'For Each v As IFilterValue In _v
                '    'If TypeOf v Is SelectExpressionValue Then
                '    '    CType(v, SelectExpressionValue).AddAlias = False
                '    'End If
                '    Dim s As String = v.GetParam(schema, fromClause, stmt, paramMgr, almgr, prepare, filterInfo, inSelect, executor)
                '    l.Add(s)
                'Next
                'Return String.Format(_f, l.ToArray)
                Return String.Format(_f, _exp.MakeStatement(schema, fromClause, stmt, paramMgr, almgr, filterInfo, MakeStatementMode.None, executor))
            Else
                Return _f
            End If
        End Function

        Public Overrides Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            If _exp IsNot Nothing Then
                Dim l As New List(Of String)
                For Each v As IExpression In _exp.GetExpressions
                    l.Add(v.GetStaticString(mpe, contextFilter))
                Next
                If _filter Then
                    Return String.Format(_f, l.ToArray)
                Else
                    Return String.Format(_f, l.ToArray) & OperationString
                End If
            Else
                If _filter Then
                    Return _f
                Else
                    Return _f & OperationString
                End If
            End If
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            If _exp IsNot Nothing Then
                _exp.Prepare(executor, schema, filterInfo, stmt, isAnonym)
            End If
        End Sub

        Public ReadOnly Property ShouldUse() As Boolean Implements IFilterValue.ShouldUse
            Get
                Return True
            End Get
        End Property

        Public Overrides Function Equals(ByVal f As Object) As Boolean
            Return Equals(TryCast(f, CustomValue))
        End Function

        Public Overloads Function Equals(ByVal f As CustomValue) As Boolean
            If f IsNot Nothing Then
                Return _ToString.Equals(f._ToString)
            Else
                Return False
            End If
        End Function

    End Class

    <Serializable()> _
    Public Class ComputedValue
        Implements IFilterValue

        Private _alias As String

        Public Sub New(ByVal [alias] As String)
            _alias = [alias]
        End Sub

        Public ReadOnly Property [Alias]() As String
            Get
                Return _alias
            End Get
        End Property

        Public Function _ToString() As String Implements IFilterValue._ToString
            Return _alias
        End Function

        Public Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As Entities.Meta.ICreateParam, ByVal almgr As IPrepareTable, ByVal prepare As PrepareValueDelegate, ByVal filterInfo As Object, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String Implements IFilterValue.GetParam
            Return [Alias]
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            Return "compval"
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            'do nothing
        End Sub

        Public ReadOnly Property ShouldUse() As Boolean Implements IFilterValue.ShouldUse
            Get
                Return True
            End Get
        End Property
    End Class

    '<Serializable()> _
    'Public Class SelectExpressionValue
    '    Implements IFilterValue

    '    Private _p As SelectExpressionOld

    '    Public Sub New(ByVal propertyAlias As String)
    '        _p = New SelectExpressionOld(CType(Nothing, EntityUnion), propertyAlias)
    '    End Sub

    '    Public Sub New(ByVal p As SelectExpressionOld)
    '        _p = p
    '    End Sub

    '    Public Sub New(ByVal op As ObjectProperty)
    '        _p = New SelectExpressionOld(op)
    '    End Sub

    '    Public Sub New(ByVal t As Type, ByVal propertyAlias As String)
    '        _p = New SelectExpressionOld(t, propertyAlias)
    '    End Sub

    '    Public Sub New(ByVal entityName As String, ByVal propertyAlias As String)
    '        _p = New SelectExpressionOld(entityName, propertyAlias)
    '    End Sub

    '    Public Sub New(ByVal os As EntityUnion, ByVal propertyAlias As String)
    '        _p = New SelectExpressionOld(os, propertyAlias)
    '    End Sub

    '    Public Sub New(ByVal table As SourceFragment, ByVal column As String)
    '        _p = New SelectExpressionOld(table, column)
    '    End Sub

    '    Public ReadOnly Property Expression() As SelectExpressionOld
    '        Get
    '            Return _p
    '        End Get
    '    End Property

    '    Public Function _ToString() As String Implements IFilterValue._ToString
    '        Return _p.GetDynamicString
    '    End Function

    '    'Public Property AddAlias() As Boolean
    '    '    Get
    '    '        Return _p.AddAlias
    '    '    End Get
    '    '    Set(ByVal value As Boolean)
    '    '        _p.AddAlias = value
    '    '    End Set
    '    'End Property

    '    Public Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
    '                      ByVal almgr As IPrepareTable, ByVal prepare As PrepareValueDelegate, _
    '                      ByVal filterInfo As Object, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String Implements IFilterValue.GetParam
    '        'Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = Nothing

    '        'If almgr IsNot Nothing Then
    '        '    tableAliases = almgr.Aliases
    '        'End If

    '        If schema Is Nothing Then
    '            Throw New ArgumentNullException("schema")
    '        End If

    '        'Dim d As String = schema.Delimiter
    '        'If Not inSelect Then
    '        '    d = stmt.Selector
    '        'End If

    '        'Dim f As ISelectExpressionFormater = stmt.CreateSelectExpressionFormater
    '        Dim sb As New StringBuilder
    '        'f.Format(_p, sb, executor, Nothing, schema, almgr, paramMgr, filterInfo, Nothing, fromClause, inSelect)
    '        Return sb.ToString

    '        'If _p.IsCustom Then
    '        '    Dim f As ISelectExpressionFormater = stmt.CreateSelectExpressionFormater
    '        '    Dim sb As New StringBuilder
    '        '    f.Format(_p, sb, Nothing, schema, almgr, paramMgr, filterInfo, Nothing, Nothing, Nothing, inSelect)
    '        '    Return sb.ToString
    '        'ElseIf _p.Table Is Nothing Then

    '        '    Dim oschema As IEntitySchema = schema.GetEntitySchema(_p.ObjectSource.GetRealType(schema))

    '        '    Dim map As MapField2Column = Nothing
    '        '    Try
    '        '        map = oschema.GetFieldColumnMap()(_p.PropertyAlias)
    '        '    Catch ex As KeyNotFoundException
    '        '        Throw New ObjectMappingException(String.Format("There is not column for property {0} ", _p.ObjectSource.ToStaticString(schema, filterInfo) & schema.Delimiter & _p.PropertyAlias, ex))
    '        '    End Try

    '        '    Dim [alias] As String = String.Empty

    '        '    If almgr IsNot Nothing Then
    '        '        'Debug.Assert(tableAliases.ContainsKey(map.Table), "There is not alias for table " & map._tableName.RawName)
    '        '        If almgr.ContainsKey(map._tableName, _p.ObjectSource) Then
    '        '            [alias] = almgr.GetAlias(map._tableName, _p.ObjectSource) & d
    '        '        Else
    '        '            [alias] = map._tableName.UniqueName(_p.ObjectSource) & d
    '        '        End If
    '        '        'Try
    '        '        '    [alias] = tableAliases(map._tableName) & schema.Selector
    '        '        'Catch ex As KeyNotFoundException
    '        '        '    Throw New QueryGeneratorException("There is not alias for table " & map._tableName.RawName, ex)
    '        '        'End Try
    '        '    End If

    '        '    Return [alias] & map._columnName
    '        'Else
    '        '    Dim [alias] As String = String.Empty

    '        '    If almgr IsNot Nothing Then
    '        '        'Debug.Assert(tableAliases.ContainsKey(map._tableName), "There is not alias for table " & map._tableName.RawName)
    '        '        Try
    '        '            [alias] = almgr.GetAlias(_p.Table, Nothing) & d
    '        '        Catch ex As KeyNotFoundException
    '        '            Throw New ObjectMappingException("There is not alias for table " & _p.Table.RawName, ex)
    '        '        End Try
    '        '    End If

    '        '    Return [alias] & _p.Column
    '        'End If
    '    End Function

    '    Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
    '        Return _p.GetStaticString(mpe, contextFilter)
    '    End Function

    '    Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
    '        _p.Prepare(executor, schema, filterInfo, stmt, isAnonym)
    '    End Sub

    '    Public ReadOnly Property ShouldUse() As Boolean Implements IFilterValue.ShouldUse
    '        Get
    '            Return True
    '        End Get
    '    End Property
    'End Class

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
                          ByVal filterInfo As Object, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String Implements IEvaluableValue.GetParam

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
                            If GetType(IKeyEntity).IsAssignableFrom(vt) Then
                                If Equals(CType(evaluatedValue, IKeyEntity).Identifier, filterValue) Then
                                    r = IEvaluableValue.EvalResult.Found
                                End If
                            ElseIf GetType(ICachedEntity).IsAssignableFrom(vt) Then
                                Dim pks() As PKDesc = CType(evaluatedValue, ICachedEntity).GetPKValues
                                If pks.Length <> 1 Then
                                    Throw New ObjectMappingException(String.Format("Type {0} has complex primary key", vt))
                                End If
                                If Equals(pks(0).Value, filterValue) Then
                                    r = IEvaluableValue.EvalResult.Found
                                End If
                            ElseIf ObjectMappingEngine.IsEntityType(vt, mpe) Then
                                Dim pks As IList(Of EntityPropertyAttribute) = mpe.GetPrimaryKeys(vt)
                                If pks.Count <> 1 Then
                                    Throw New ObjectMappingException(String.Format("Type {0} has complex primary key", vt))
                                End If
                                If Equals(mpe.GetPropertyValue(evaluatedValue, pks(0).PropertyAlias), filterValue) Then
                                    r = IEvaluableValue.EvalResult.Found
                                End If
                            ElseIf GetType(IKeyEntity).IsAssignableFrom(valt) Then
                                If Equals(CType(filterValue, IKeyEntity).Identifier, evaluatedValue) Then
                                    r = IEvaluableValue.EvalResult.Found
                                End If
                            ElseIf GetType(ICachedEntity).IsAssignableFrom(valt) Then
                                Dim pks() As PKDesc = CType(filterValue, ICachedEntity).GetPKValues
                                If pks.Length <> 1 Then
                                    Throw New ObjectMappingException(String.Format("Type {0} has complex primary key", vt))
                                End If
                                If Equals(pks(0).Value, evaluatedValue) Then
                                    r = IEvaluableValue.EvalResult.Found
                                End If
                            ElseIf ObjectMappingEngine.IsEntityType(valt, mpe) Then
                                Dim pks As IList(Of EntityPropertyAttribute) = mpe.GetPrimaryKeys(valt)
                                If pks.Count <> 1 Then
                                    Throw New ObjectMappingException(String.Format("Type {0} has complex primary key", vt))
                                End If
                                If Equals(mpe.GetPropertyValue(filterValue, pks(0).PropertyAlias), evaluatedValue) Then
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
                Throw New InvalidOperationException(String.Format("Cannot eval field {4}.{0} of type {1} through value {2} of type {3}. Operation {5}. Stack {6}", _
                    template.PropertyAlias, filterValue.GetType, evaluatedValue, evaluatedValue.GetType, _
                    If(template.ObjectSource.AnyType Is Nothing, template.ObjectSource.AnyEntityName, template.ObjectSource.AnyType.ToString), template.Operation, ex.StackTrace, ex))
            End Try
            Return r
        End Function

        Public Overridable ReadOnly Property ShouldUse() As Boolean Implements IFilterValue.ShouldUse
            Get
                Return True
            End Get
        End Property

        Public Overridable Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
            Return "scalarval"
        End Function

        Public Overridable Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            'do nothing
        End Sub

    End Class

    <Serializable()> _
    Public Class LiteralValue
        Implements IFilterValue

        Private _pname As String

        Public Sub New(ByVal literal As String)
            _pname = literal
        End Sub

        Public Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, ByVal prepare As PrepareValueDelegate, _
                          ByVal filterInfo As Object, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String Implements IFilterValue.GetParam
            Return _pname
        End Function

        Public Function _ToString() As String Implements IFilterValue._ToString
            Return _pname
        End Function

        Public Overridable ReadOnly Property ShouldUse() As Boolean Implements IFilterValue.ShouldUse
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
    End Class

    <Serializable()> _
    Public Class DBNullValue
        Inherits LiteralValue
        Implements IEvaluableValue

        Public Sub New()
            MyBase.New("null")
        End Sub

        Public Function Eval(ByVal v As Object, ByVal mpe As ObjectMappingEngine, ByVal template As Core.OrmFilterTemplate) As IEvaluableValue.EvalResult Implements IEvaluableValue.Eval
            If template.Operation = FilterOperation.Is Then
                If v Is Nothing Then
                    Return IEvaluableValue.EvalResult.Found
                Else
                    Return IEvaluableValue.EvalResult.NotFound
                End If
            ElseIf template.Operation = FilterOperation.IsNot Then
                If v IsNot Nothing Then
                    Return IEvaluableValue.EvalResult.Found
                Else
                    Return IEvaluableValue.EvalResult.NotFound
                End If
            Else
                Throw New NotSupportedException(String.Format("Operation {0} is not supported for IsNull statement", template.OperToString))
            End If
        End Function

        Public ReadOnly Property Value() As Object Implements IEvaluableValue.Value
            Get
                Return Nothing
            End Get
        End Property
    End Class

    <Serializable()> _
    Public Class EntityValue
        Inherits ScalarValue

        Private _t As Type

        Public Sub New(ByVal o As IKeyEntity)
            MyBase.New()
            If o IsNot Nothing Then
                _t = o.GetType
                SetValue(o.Identifier)
            Else
                _t = GetType(IKeyEntity)
            End If
        End Sub

        Public Sub New(ByVal o As IKeyEntity, ByVal caseSensitive As Boolean)
            MyClass.New(o)
            Me.CaseSensitive = caseSensitive
        End Sub

        Public Function GetOrmValue(ByVal mgr As OrmManager) As IKeyEntity
            Return mgr.GetKeyEntityFromCacheOrCreate(Value, _t)
        End Function

        Public ReadOnly Property OrmType() As Type
            Get
                Return _t
            End Get
        End Property

        Protected Overrides Function GetValue(ByVal v As Object, ByVal template As OrmFilterTemplate, ByRef r As IEvaluableValue.EvalResult) As Object
            r = IEvaluableValue.EvalResult.Unknown
            Dim orm As IKeyEntity = TryCast(v, IKeyEntity)
            If orm IsNot Nothing Then
                Dim ov As EntityValue = TryCast(Me, EntityValue)
                If ov Is Nothing Then
                    Throw New InvalidOperationException(String.Format("Field {0} is Entity but param is not", template.PropertyAlias))
                End If
                Dim tt As Type = v.GetType
                If Not tt.IsAssignableFrom(ov.OrmType) Then
                    If Value Is Nothing Then
                        r = IEvaluableValue.EvalResult.NotFound
                        Return Nothing
                    Else
                        Throw New InvalidOperationException(String.Format("Field {0} is type of {1} but param is type of {2}", template.PropertyAlias, tt.ToString, ov.OrmType.ToString))
                    End If
                End If
                Return ov.GetOrmValue(OrmManager.CurrentManager)
            End If
            Return Value
        End Function

        'Public Overrides ReadOnly Property Value() As Object
        '    Get
        '        Return GetOrmValue(OrmManager.CurrentManager)
        '    End Get
        'End Property
    End Class

    <Serializable()> _
    Public Class InValue
        Inherits ScalarValue

        Private _l As New List(Of String)
        Private _str As String

        Public Sub New(ByVal value As IEnumerable)
            MyBase.New(value)
        End Sub

        Public Sub New(ByVal value As IEnumerable, ByVal caseSensitive As Boolean)
            MyBase.New(value, caseSensitive)
        End Sub

        Public Overrides Function _ToString() As String
            If String.IsNullOrEmpty(_str) Then
                Dim l As New List(Of String)
                For Each o As Object In Value
                    l.Add(o.ToString)
                Next
                l.Sort()
                Dim sb As New StringBuilder
                For Each s As String In l
                    sb.Append(s).Append("$")
                Next
                _str = sb.ToString
            End If
            Return _str
        End Function

        Public Overrides Function Eval(ByVal v As Object, ByVal mpe As ObjectMappingEngine, ByVal template As OrmFilterTemplate) As IEvaluableValue.EvalResult
            Dim r As IEvaluableValue.EvalResult = IEvaluableValue.EvalResult.NotFound

            'Dim val As Object = GetValue(v, template, r)
            'If r <> IEvaluableValue.EvalResult.Unknown Then
            '    Return r
            'Else
            '    r = IEvaluableValue.EvalResult.NotFound
            'End If

            Select Case template.Operation
                Case FilterOperation.In
                    For Each o As Object In Value
                        If Object.Equals(o, v) Then
                            r = IEvaluableValue.EvalResult.Found
                            Exit For
                        End If
                    Next
                Case FilterOperation.NotIn
                    For Each o As Object In Value
                        If Object.Equals(o, v) Then
                            r = IEvaluableValue.EvalResult.NotFound
                            Exit For
                        End If
                    Next
                Case Else
                    Throw New InvalidOperationException(String.Format("Invalid operation {0} for InValue", template.OperToString))
            End Select

            Return r
        End Function

        Public Shadows ReadOnly Property Value() As IEnumerable
            Get
                Return CType(MyBase.Value, IEnumerable)
            End Get
        End Property

        Public Overrides Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, ByVal prepare As PrepareValueDelegate, _
                          ByVal filterInfo As Object, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String

            Dim sb As New StringBuilder
            Dim idx As Integer
            For Each o As Object In Value
                Dim v As Object = o
                If prepare IsNot Nothing Then
                    v = prepare(schema, o)
                End If

                If paramMgr Is Nothing Then
                    Throw New ArgumentNullException("paramMgr")
                End If

                Dim pname As String = Nothing
                Dim add As Boolean
                If _l.Count < idx Then
                    pname = _l(idx)
                Else
                    add = True
                End If

                pname = paramMgr.AddParam(pname, v)
                If add Then
                    _l.Add(pname)
                End If

                sb.Append(pname).Append(",")
                idx += 1
            Next
            sb.Length -= 1
            sb.Insert(0, "(").Append(")")
            Return sb.ToString
        End Function

        Public Overrides ReadOnly Property ShouldUse() As Boolean
            Get
                If Value IsNot Nothing Then
                    For Each s As Object In Value
                        Return True
                    Next
                End If
                Return False
            End Get
        End Property
    End Class

    <Serializable()> _
    Public Class BetweenValue
        Inherits ScalarValue

        Public Sub New(ByVal left As Object, ByVal right As Object)
            MyBase.New(New Pair(Of IFilterValue)(New ScalarValue(left), New ScalarValue(right)))

            If left Is Nothing Then
                Throw New ArgumentNullException("left")
            End If

            If right Is Nothing Then
                Throw New ArgumentNullException("right")
            End If
        End Sub

        Public Sub New(ByVal left As IFilterValue, ByVal right As IFilterValue)
            MyBase.New(New Pair(Of IFilterValue)(left, right))

            If left Is Nothing Then
                Throw New ArgumentNullException("left")
            End If

            If right Is Nothing Then
                Throw New ArgumentNullException("right")
            End If
        End Sub

        Public Overrides Function Eval(ByVal v As Object, ByVal mpe As ObjectMappingEngine, ByVal template As OrmFilterTemplate) As IEvaluableValue.EvalResult
            Dim r As IEvaluableValue.EvalResult = IEvaluableValue.EvalResult.Unknown

            Dim val As Object = GetValue(v, template, r)
            If r <> IEvaluableValue.EvalResult.Unknown Then
                Return r
            Else
                r = IEvaluableValue.EvalResult.NotFound
            End If

            If template.Operation = FilterOperation.Between Then
                Dim int_v As Pair(Of IFilterValue) = Value

                Dim fe As IEvaluableValue = TryCast(int_v.First, IEvaluableValue)
                Dim se As IEvaluableValue = TryCast(int_v.Second, IEvaluableValue)

                If fe IsNot Nothing AndAlso se IsNot Nothing Then
                    Dim i As Integer = CType(v, IComparable).CompareTo(fe.Value)
                    If i >= 0 Then
                        i = CType(v, IComparable).CompareTo(se.Value)
                        If i <= 0 Then
                            r = IEvaluableValue.EvalResult.Found
                        End If
                    End If
                End If
            Else
                Throw New InvalidOperationException(String.Format("Invalid operation {0} for BetweenValue", template.OperToString))
            End If

            Return r
        End Function

        Public Overrides Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, ByVal prepare As PrepareValueDelegate, _
                          ByVal filterInfo As Object, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String

            If paramMgr Is Nothing Then
                Throw New ArgumentNullException("paramMgr")
            End If

            Dim left As IFilterValue = Value.First, right As IFilterValue = Value.Second
            'If prepare IsNot Nothing Then
            '    left = prepare(schema, left)
            '    right = prepare(schema, right)
            'End If

            '_l = paramMgr.AddParam(_l, left)
            '_r = paramMgr.AddParam(_r, right)

            'Return _l & " and " & _r

            Return left.GetParam(schema, fromClause, stmt, paramMgr, almgr, prepare, filterInfo, inSelect, executor) & _
                " and " & _
                right.GetParam(schema, fromClause, stmt, paramMgr, almgr, prepare, filterInfo, inSelect, executor)
        End Function

        Public Overrides Function _ToString() As String
            Return Value.First._ToString & "__$__" & Value.Second._ToString
        End Function

        Public Overrides Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String
            Return Value.First.GetStaticString(mpe, contextFilter) & "between" & Value.Second.GetStaticString(mpe, contextFilter)
        End Function

        Public Overrides Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean)
            Value.First.Prepare(executor, schema, filterInfo, stmt, isAnonym)
            Value.Second.Prepare(executor, schema, filterInfo, stmt, isAnonym)
        End Sub

        Public Shadows ReadOnly Property Value() As Pair(Of IFilterValue)
            Get
                Return CType(MyBase.Value, Pair(Of IFilterValue))
            End Get
        End Property
    End Class

    '    <Serializable()> _
    '    Public Class SubQuery
    '        Implements Worm.Criteria.Values.IFilterValue, Worm.Criteria.Values.INonTemplateValue,  _
    '        Cache.IQueryDependentTypes

    '        Private _t As Type
    '        Private _tbl As SourceFragment
    '        Private _f As IFilter
    '        Private _field As String
    '        Private _joins() As Worm.Criteria.Joins.QueryJoin

    '        Public Sub New(ByVal t As Type, ByVal f As IFilter)
    '            _t = t
    '            _f = f
    '        End Sub

    '        Public Sub New(ByVal table As SourceFragment, ByVal f As IFilter)
    '            _tbl = table
    '            _f = f
    '        End Sub

    '        Public Sub New(ByVal t As Type, ByVal f As IEntityFilter, ByVal field As String)
    '            '_tbl = CType(OrmManager.CurrentManager.ObjectSchema.GetObjectSchema(t), IOrmObjectSchema).GetTables(0)
    '            _t = t
    '            _f = f
    '            _field = field
    '        End Sub

    '#Region " Properties "

    '        Public Property Joins() As Worm.Criteria.Joins.QueryJoin()
    '            Get
    '                Return _joins
    '            End Get
    '            Set(ByVal value As Worm.Criteria.Joins.QueryJoin())
    '                _joins = value
    '            End Set
    '        End Property

    '        Public ReadOnly Property Filter() As IFilter
    '            Get
    '                Return _f
    '            End Get
    '        End Property

    '        Public ReadOnly Property Table() As SourceFragment
    '            Get
    '                Return _tbl
    '            End Get
    '        End Property

    '        Public ReadOnly Property Type() As Type
    '            Get
    '                Return _t
    '            End Get
    '        End Property

    '#End Region

    '        Public Overridable Function _ToString() As String Implements Worm.Criteria.Values.IFilterValue._ToString
    '            Dim r As String = Nothing
    '            If _t IsNot Nothing Then
    '                r = _t.ToString()
    '            Else
    '                r = _tbl.RawName
    '            End If
    '            If _joins IsNot Nothing Then
    '                r &= "$"
    '                For Each join As Worm.Criteria.Joins.QueryJoin In _joins
    '                    If Not Worm.Criteria.Joins.QueryJoin.IsEmpty(join) Then
    '                        r &= join._ToString
    '                    End If
    '                Next
    '            End If
    '            If _f IsNot Nothing Then
    '                r &= "$" & _f._ToString
    '            End If
    '            If Not String.IsNullOrEmpty(_field) Then
    '                r &= "$" & _field
    '            End If
    '            Return r
    '        End Function

    '        Public Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
    '                      ByVal almgr As IPrepareTable, ByVal prepare As Worm.Criteria.Values.PrepareValueDelegate, _
    '                      ByVal filterInfo As Object, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String Implements Worm.Criteria.Values.IFilterValue.GetParam
    '            Dim sb As New StringBuilder
    '            'Dim dbschema As DbSchema = CType(schema, DbSchema)
    '            sb.Append("(")

    '            'If _stmtGen Is Nothing Then
    '            '    _stmtGen = TryCast(schema, SQLGenerator)
    '            'End If

    '            stmt.FormStmt(schema, fromClause, filterInfo, paramMgr, almgr, sb, _t, _tbl, _joins, _field, _f)

    '            sb.Append(")")

    '            Return sb.ToString
    '        End Function

    '        Public Overridable Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements Worm.Criteria.Values.INonTemplateValue.GetStaticString
    '            Dim r As String = Nothing
    '            If _t IsNot Nothing Then
    '                r = _t.ToString()
    '            Else
    '                r = _tbl.RawName
    '            End If
    '            If _joins IsNot Nothing Then
    '                r &= "$"
    '                For Each join As Worm.Criteria.Joins.QueryJoin In _joins
    '                    If Not Worm.Criteria.Joins.QueryJoin.IsEmpty(join) Then
    '                        r &= join.GetStaticString(mpe, contextFilter)
    '                    End If
    '                Next
    '            End If
    '            If _f IsNot Nothing Then
    '                r &= "$" & _f.GetStaticString(mpe, contextFilter)
    '            End If
    '            If Not String.IsNullOrEmpty(_field) Then
    '                r &= "$" & _field
    '            End If
    '            Return r
    '        End Function

    '        Public Function [Get](ByVal mpe As ObjectMappingEngine) As Cache.IDependentTypes Implements Cache.IQueryDependentTypes.Get
    '            'If _t Is Nothing Then
    '            '    Return New Cache.EmptyDependentTypes
    '            'End If

    '            Dim dp As New Cache.DependentTypes
    '            If _joins IsNot Nothing Then
    '                For Each j As Worm.Criteria.Joins.QueryJoin In _joins
    '                    Dim t As Type = j.ObjectSource.GetRealType(mpe)
    '                    'If t Is Nothing AndAlso Not String.IsNullOrEmpty(j.EntityName) Then
    '                    '    t = mpe.GetTypeByEntityName(j.EntityName)
    '                    'End If
    '                    'If t Is Nothing Then
    '                    '    Return New Cache.EmptyDependentTypes
    '                    'End If
    '                    dp.AddBoth(t)
    '                Next
    '            End If

    '            If _f IsNot Nothing AndAlso TryCast(_f, IEntityFilter) Is Nothing Then
    '                For Each f As IFilter In _f.Filter.GetAllFilters
    '                    Dim fdp As Cache.IDependentTypes = Cache.QueryDependentTypes(mpe, f)
    '                    If Cache.IsCalculated(fdp) Then
    '                        dp.Merge(fdp)
    '                        'Else
    '                        '    Return fdp
    '                    End If
    '                Next
    '            End If

    '            Return dp.Get
    '        End Function

    '        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
    '            'do nothing
    '        End Sub

    '        Public ReadOnly Property ShouldUse() As Boolean Implements IFilterValue.ShouldUse
    '            Get
    '                Return True
    '            End Get
    '        End Property
    '    End Class

    <Serializable()> _
    Public Class SubQueryCmd
        Implements Worm.Criteria.Values.IFilterValue, Worm.Criteria.Values.INonTemplateValue,  _
        Cache.IQueryDependentTypes

        Private _q As Query.QueryCmd

        'Public Sub New(ByVal q As Query.QueryCmd)
        '    MyClass.New(q)
        'End Sub

        Public Sub New(ByVal q As Query.QueryCmd)
            _q = q
        End Sub

        Public Function _ToString() As String Implements Worm.Criteria.Values.IFilterValue._ToString
            Return _q._ToString()
        End Function

        Public Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, ByVal prepare As Worm.Criteria.Values.PrepareValueDelegate, _
                          ByVal filterInfo As Object, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String Implements Worm.Criteria.Values.IFilterValue.GetParam
            Dim sb As New StringBuilder
            'Dim dbschema As DbSchema = CType(schema, DbSchema)
            sb.Append("(")

            'Dim c As New Query.QueryCmd.svct(_q)
            'Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
            'If _q.SelectedType Is Nothing Then
            '    If String.IsNullOrEmpty(_q.SelectedEntityName) Then
            '        _q.SelectedType = _q.CreateType
            '    Else
            '        _q.SelectedType = schema.GetTypeByEntityName(_q.SelectedEntityName)
            '    End If
            'End If

            'If GetType(Entities.AnonymousEntity).IsAssignableFrom(_q.SelectedType) Then
            '    _q.SelectedType = Nothing
            'End If

            'If _q.CreateType Is Nothing AndAlso _q.SelectedType IsNot Nothing Then
            '    _q.Into(_q.SelectedType)
            'End If

            'QueryCmd.Prepare(_q, Nothing, schema, filterInfo, stmt)
            sb.Append(stmt.MakeQueryStatement(schema, fromClause, filterInfo, _q, paramMgr, almgr))
            'End Using

            sb.Append(")")

            Return sb.ToString
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements Worm.Criteria.Values.INonTemplateValue.GetStaticString
            Return _q.ToStaticString(mpe, contextFilter)
        End Function

        Public Function [Get](ByVal mpe As ObjectMappingEngine) As Cache.IDependentTypes Implements Cache.IQueryDependentTypes.Get
            Dim qp As Cache.IDependentTypes = CType(_q, Cache.IQueryDependentTypes).Get(mpe)
            If Cache.IsEmpty(qp) Then
                Dim dt As New Cache.DependentTypes
                Dim types As ICollection(Of Type) = Nothing
                If _q.GetSelectedTypes(mpe, types) Then
                    dt.AddBoth(types)
                End If
                qp = dt
            End If
            Return qp
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            _q.AutoFields = False
            _q.Prepare(executor, schema, filterInfo, stmt, isAnonym)
        End Sub

        Public ReadOnly Property ShouldUse() As Boolean Implements IFilterValue.ShouldUse
            Get
                Return True
            End Get
        End Property
    End Class
End Namespace

'Namespace Database
'    Namespace Criteria.Values
'        'Public Interface IDatabaseFilterValue
'        '    Inherits Worm.Criteria.Values.IFilterValue
'        '    Function GetParam(ByVal schema As SQLGenerator, ByVal filterInfo As Object, ByVal paramMgr As ICreateParam, ByVal almgr As IPrepareTable) As String
'        'End Interface


'    End Namespace
'End Namespace
