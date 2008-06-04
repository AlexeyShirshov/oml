Imports System.Linq.Expressions
Imports System.Collections.ObjectModel
Imports Worm.Database.Criteria.Core
Imports Worm.Database.Criteria.Conditions
Imports Worm.Criteria.Values
Imports Worm.Orm
Imports System.Reflection
Imports Worm.Orm.Meta
Imports Worm.Sorting
Imports Worm.Query

Namespace Linq
    Enum Constr
        None
        First
        FirstOrDef
        [Single]
        SingleOrDef
        Last
        LastOrDef
    End Enum

    <Serializable()> _
    Public Class LinqException
        Inherits System.Exception

        Public Sub New()
        End Sub

        Public Sub New(ByVal message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(ByVal message As String, ByVal inner As Exception)
            MyBase.New(message, inner)
        End Sub

        Private Sub New( _
            ByVal info As System.Runtime.Serialization.SerializationInfo, _
            ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
        End Sub
    End Class

    Public Class WormLinqProvider
        Implements IQueryProvider

        Private _ctx As WormContext

        Public Sub New(ByVal ctx As WormContext)
            _ctx = ctx
        End Sub

        Public Function CreateQuery(ByVal expression As System.Linq.Expressions.Expression) As System.Linq.IQueryable Implements System.Linq.IQueryProvider.CreateQuery
            Return _ctx.CreateQueryWrapper(expression.Type, Me, expression)
        End Function

        Public Function CreateQuery(Of TElement)(ByVal expression As System.Linq.Expressions.Expression) As System.Linq.IQueryable(Of TElement) Implements System.Linq.IQueryProvider.CreateQuery
            Return _ctx.CreateQueryWrapper(Of TElement)(Me, expression)
        End Function

        Public Function Execute(ByVal expression As System.Linq.Expressions.Expression) As Object Implements System.Linq.IQueryProvider.Execute
            Using _ctx.CreateReadonlyManager

            End Using
        End Function

        Public Function Execute(Of TResult)(ByVal expression As System.Linq.Expressions.Expression) As TResult Implements System.Linq.IQueryProvider.Execute
            Using mgr = _ctx.CreateReadonlyManager
                Dim ev As New QueryVisitor(_ctx.Schema)
                ev.Visit(expression)
                'Dim q As New Worm.Query.QueryCmd(Of TResult)(ev.Query)
                Dim q As Worm.Query.QueryCmdBase = ev.Query
                Dim rt As Type = GetType(TResult)
                If GetType(IEnumerator).IsAssignableFrom(rt) Then
                    Dim t As Type = rt.GetGenericArguments(0)
                    If GetType(OrmBase).IsAssignableFrom(t) Then
                        q.SelectedType = t
                        Dim e As IEnumerator = q.ExecTypeless(mgr)
                        Return CType(e, TResult)
                    Else
                        Dim lt As Type = GetType(List(Of ))
                        Dim glt As Type = lt.MakeGenericType(New Type() {t})
                        Dim l As IList = CType(Activator.CreateInstance(glt), System.Collections.IList)
                        q.SelectedType = ev.T
                        Dim e As IEnumerator = q.ExecTypeless(mgr)
                        Do While e.MoveNext
                            Dim o As OrmBase = CType(e.Current, OrmBase)
                            l.Add(ev.GetNew(o))
                        Loop
                        Return CType(l.GetEnumerator, TResult)
                    End If
                Else
                    q.SelectedType = ev.T
                    Dim l As IList(Of TResult) = Nothing
                    
                    'Else
                    If rt.IsValueType Then
                        l = q.ExecSimple(Of TResult)(mgr)
                    Else
                        If GetType(OrmBase).IsAssignableFrom(rt) Then
                            l = CType(q.ExecTypelessToList(mgr), IList(Of TResult))
                        Else
                            l = New List(Of TResult)
                            Dim e As IEnumerator = q.ExecTypeless(mgr)
                            Do While e.MoveNext
                                Dim o As OrmBase = CType(e.Current, OrmBase)
                                l.Add(CType(ev.GetNew(o), TResult))
                            Loop
                        End If
                    End If

                    Select Case ev.Constr
                        Case Constr.First
                            Return l(0)
                        Case Constr.FirstOrDef
                            If l.Count = 0 Then
                                Return Nothing
                            Else
                                Return l(0)
                            End If
                        Case Constr.Single
                            If l.Count <> 1 Then
                                Throw New LinqException
                            Else
                                Return l(0)
                            End If
                        Case Constr.SingleOrDef
                            If l.Count > 1 Then
                                Throw New LinqException
                            ElseIf l.Count = 0 Then
                                Return Nothing
                            Else
                                Return l(0)
                            End If
                        Case Constr.None
                            If l.Count > 0 Then
                                Return l(0)
                            Else
                                Throw New InvalidOperationException
                            End If
                        Case Constr.Last
                            If l.Count = 0 Then
                                Throw New LinqException
                            Else
                                Return l(l.Count - 1)
                            End If
                        Case Constr.LastOrDef
                            If l.Count = 0 Then
                                Return Nothing
                            Else
                                Return l(l.Count - 1)
                            End If
                        Case Else
                            Throw New NotImplementedException
                    End Select
                End If
            End Using
        End Function
    End Class

    '    Class Enumm(Of T)
    '        Implements IEnumerator(Of T)

    '        Public ReadOnly Property Current() As T Implements System.Collections.Generic.IEnumerator(Of T).Current
    '            Get

    '            End Get
    '        End Property

    '        Public ReadOnly Property Current1() As Object Implements System.Collections.IEnumerator.Current
    '            Get

    '            End Get
    '        End Property

    '        Public Function MoveNext() As Boolean Implements System.Collections.IEnumerator.MoveNext

    '        End Function

    '        Public Sub Reset() Implements System.Collections.IEnumerator.Reset

    '        End Sub

    '        Private disposedValue As Boolean = False        ' To detect redundant calls

    '        ' IDisposable
    '        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
    '            If Not Me.disposedValue Then
    '                If disposing Then
    '                    ' TODO: free other state (managed objects).
    '                End If

    '                ' TODO: free your own state (unmanaged objects).
    '                ' TODO: set large fields to null.
    '            End If
    '            Me.disposedValue = True
    '        End Sub

    '#Region " IDisposable Support "
    '        ' This code added by Visual Basic to correctly implement the disposable pattern.
    '        Public Sub Dispose() Implements IDisposable.Dispose
    '            ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
    '            Dispose(True)
    '            GC.SuppressFinalize(Me)
    '        End Sub
    '#End Region

    '    End Class

    Public Class FilterValueVisitor
        Inherits MyExpressionVisitor

        Private _v As IParamFilterValue
        Private _p As Orm.OrmProperty
        Private _q As QueryVisitor

        Sub New(ByVal schema As QueryGenerator, ByVal q As QueryVisitor)
            MyBase.new(schema)
            _q = q
        End Sub

        Public ReadOnly Property Prop() As Orm.OrmProperty
            Get
                Return _p
            End Get
        End Property

        Public ReadOnly Property Value() As IParamFilterValue
            Get
                Return _v
            End Get
        End Property

        Protected Overrides Function VisitConstant(ByVal c As System.Linq.Expressions.ConstantExpression) As System.Linq.Expressions.Expression
            If c.Type.IsPrimitive OrElse GetType(String) Is c.Type Then
                _v = New ScalarValue(c.Value)
                'Else
                '    _v = New ScalarValue(Eval(c))
            End If
            Return Nothing
        End Function

        Protected Overrides Function VisitMemberAccess(ByVal m As System.Linq.Expressions.MemberExpression) As System.Linq.Expressions.Expression
            If GetType(OrmBase).IsAssignableFrom(m.Expression.Type) Then
                Dim p As ParameterExpression = CType(m.Expression, ParameterExpression)
                Dim field As String = GetField(p.Type, m.Member.Name)
                _p = New Orm.OrmProperty(p.Type, field)
                Return Nothing
                '                ElseIf TypeOf m.Expression Is ConstantExpression Then
                '    _v = New ScalarValue(Eval(m))
                '    Return Nothing
            ElseIf TypeOf m.Expression Is ParameterExpression Then
                'If GetType(OrmBase).IsAssignableFrom(m.Expression.Type) Then
                _p = _q.GetProperty(m.Member.Name)
                'Else
                '    _p = _q.GetProperty
                'End If
            Else
                Return MyBase.VisitMemberAccess(m)
            End If
            Return Nothing
        End Function
    End Class

    Public Class FilterVisitorBase
        Inherits MyExpressionVisitor

        Sub New(ByVal schema As QueryGenerator)
            MyBase.new(schema)
        End Sub

        Private _f As Criteria.Core.IFilter

        Public Overridable Property Filter() As Criteria.Core.IFilter
            Get
                Return _f
            End Get
            Protected Set(ByVal value As Criteria.Core.IFilter)
                _f = value
            End Set
        End Property

    End Class

    Public Class AgVisitor
        Inherits FilterVisitorBase

        Private _t As Type
        Public ReadOnly Property Type() As Type
            Get
                Return _t
            End Get
        End Property

        Private _f As String
        Public ReadOnly Property Field() As String
            Get
                Return _f
            End Get
        End Property

        Sub New(ByVal schema As QueryGenerator)
            MyBase.new(schema)
        End Sub

        Protected Overrides Function VisitMemberAccess(ByVal m As System.Linq.Expressions.MemberExpression) As System.Linq.Expressions.Expression
            If GetType(OrmBase).IsAssignableFrom(m.Expression.Type) Then
                Dim p As ParameterExpression = CType(m.Expression, ParameterExpression)
                _f = GetField(p.Type, m.Member.Name)
                _t = p.Type
            Else
                Throw New NotImplementedException
            End If
            Return Nothing
        End Function
    End Class

    Public Class SortVisitor
        Inherits FilterVisitorBase

        Private _sort As SortOrder
        Public ReadOnly Property Sort() As SortOrder
            Get
                Return _sort
            End Get
            'Set(ByVal value As Sort)
            '    _sort = value
            'End Set
        End Property

        'Public Function Order(ByVal desc As Boolean) As SortVisitor
        '    _sort.Order(desc)
        '    Return Me
        'End Function

        Sub New(ByVal schema As QueryGenerator)
            MyBase.new(schema)
        End Sub

        Protected Overrides Function VisitMemberAccess(ByVal m As System.Linq.Expressions.MemberExpression) As System.Linq.Expressions.Expression
            If GetType(OrmBase).IsAssignableFrom(m.Expression.Type) Then
                Dim p As ParameterExpression = CType(m.Expression, ParameterExpression)
                Dim field As String = GetField(p.Type, m.Member.Name)
                If _sort Is Nothing Then
                    _sort = Worm.Orm.Sorting.Field(field)
                Else
                    _sort.NextField(field)
                End If
                Return Nothing
            Else
                Throw New NotImplementedException
            End If
        End Function
    End Class

    Public Class FilterVisitor
        Inherits FilterVisitorBase

        Private _c As New Condition.ConditionConstructor
        Private _q As QueryVisitor

        Sub New(ByVal schema As QueryGenerator, ByVal q As QueryVisitor)
            MyBase.new(schema)
            _q = q
        End Sub

        Public Overrides Property Filter() As Criteria.Core.IFilter
            Get
                If MyBase.Filter IsNot Nothing Then
                    Return MyBase.Filter
                Else
                    Return _c.Condition
                End If
            End Get
            Protected Set(ByVal value As Criteria.Core.IFilter)
                MyBase.Filter = value
            End Set
        End Property

        Private Sub ExtractOrAnd(ByVal b As System.Linq.Expressions.BinaryExpression, ByVal oper As Criteria.Conditions.ConditionOperator)
            Dim rf As New FilterVisitor(_schema, _q) : rf.Visit(b.Left)
            _c.AddFilter(rf.Filter)
            Dim lf As New FilterVisitor(_schema, _q) : lf.Visit(b.Right)
            _c.AddFilter(lf.Filter, oper)
        End Sub

        Protected Sub ExtractCondition(ByVal b As System.Linq.Expressions.BinaryExpression, ByVal fo As Criteria.FilterOperation)
            Dim lf As New FilterValueVisitor(_schema, _q)
            Dim rf As New FilterValueVisitor(_schema, _q)
            If TypeOf (b.Left) Is MethodCallExpression Then
                Dim m As MethodCallExpression = CType(b.Left, MethodCallExpression)
                If m.Method.Name = "CompareString" Then
                    lf.Visit(m.Arguments(0))
                    rf.Visit(m.Arguments(1))
                End If
            Else
                lf.Visit(b.Left)
                rf.Visit(b.Right)
            End If
            If lf.Prop IsNot Nothing Then
                Filter = New EntityFilter(lf.Prop.Type, lf.Prop.Field, rf.Value, fo)
            Else
                Filter = New EntityFilter(rf.Prop.Type, rf.Prop.Field, lf.Value, Invert(fo))
            End If
        End Sub

        Protected Overrides Function VisitBinary(ByVal b As System.Linq.Expressions.BinaryExpression) As System.Linq.Expressions.Expression
            Select Case b.NodeType
                Case ExpressionType.And, ExpressionType.AndAlso
                    ExtractOrAnd(b, Criteria.Conditions.ConditionOperator.And)
                Case ExpressionType.Or, ExpressionType.OrElse
                    ExtractOrAnd(b, Criteria.Conditions.ConditionOperator.Or)
                Case ExpressionType.NotEqual
                    ExtractCondition(b, Criteria.FilterOperation.NotEqual)
                Case ExpressionType.Equal
                    ExtractCondition(b, Criteria.FilterOperation.Equal)
                Case ExpressionType.GreaterThan
                    ExtractCondition(b, Criteria.FilterOperation.GreaterThan)
                Case ExpressionType.GreaterThanOrEqual
                    ExtractCondition(b, Criteria.FilterOperation.GreaterEqualThan)
                Case ExpressionType.LessThan
                    ExtractCondition(b, Criteria.FilterOperation.LessThan)
                Case ExpressionType.LessThanOrEqual
                    ExtractCondition(b, Criteria.FilterOperation.LessEqualThan)
                    'Case ExpressionType.Coalesce
                    '    Return Nothing
                Case Else
                    Return MyBase.VisitBinary(b)
            End Select
            Return Nothing
        End Function

        'Protected Overrides Function VisitMemberAccess(ByVal m As System.Linq.Expressions.MemberExpression) As System.Linq.Expressions.Expression
        '    Return MyBase.VisitMemberAccess(m)
        'End Function

        'Protected Overrides Function VisitUnary(ByVal u As System.Linq.Expressions.UnaryExpression) As System.Linq.Expressions.Expression
        '    Return MyBase.VisitUnary(u)
        'End Function
    End Class

    Public Class QueryVisitor
        Inherits MyExpressionVisitor

        Private _q As Query.QueryCmdBase
        Private _so As SortOrder
        Private _new As NewExpression
        Private _mem As MemberExpression
        Private _t As Type
        Private _ct As Constr
        Private _skip As Boolean

        Sub New(ByVal schema As QueryGenerator)
            MyBase.new(schema)
            _q = New Query.QueryCmdBase(Nothing)
        End Sub

        'Sub New(ByVal q As Query.QueryCmdBase)
        '    _q = q
        'End Sub

        Friend ReadOnly Property Constr() As Constr
            Get
                Return _ct
            End Get
        End Property

        Public ReadOnly Property T() As Type
            Get
                Return _t
            End Get
        End Property

        Public ReadOnly Property Query() As Query.QueryCmdBase
            Get
                If _so IsNot Nothing Then
                    _q.Sort = _so
                    _so = Nothing
                End If
                Return _q
            End Get
        End Property

        Protected Overrides Function VisitWithLoad(ByVal w As WithLoadExpression) As System.Linq.Expressions.Expression
            _q.WithLoad = True
            Return MyBase.VisitWithLoad(w)
        End Function

        Protected Friend Function GetProperty(ByVal name As String) As Orm.OrmProperty
            If _new Is Nothing Then
                Return Me.GetProperty
            End If

            For i As Integer = 0 To _new.Arguments.Count
                If _new.Constructor.GetParameters(i).Name = name Then
                    Dim m As MemberExpression = CType(_new.Arguments(i), MemberExpression)
                    Dim t As Type = m.Member.DeclaringType
                    If t.FullName.StartsWith("Worm.Orm.OrmBaseT") Then
                        t = t.GetGenericArguments(0)
                    End If
                    Return New Orm.OrmProperty(t, m.Member.Name)
                End If
            Next

            Return Nothing
        End Function

        Protected Friend Function GetProperty() As Orm.OrmProperty
            If _mem Is Nothing Then
                Throw New InvalidOperationException
            End If

            Return New Orm.OrmProperty(_mem.Member.DeclaringType, _mem.Member.Name)
        End Function

        Protected Overrides Function VisitNew(ByVal nex As System.Linq.Expressions.NewExpression) As System.Linq.Expressions.NewExpression
            _new = nex
            Return Nothing
        End Function

        Protected Overrides Function VisitMemberAccess(ByVal m As System.Linq.Expressions.MemberExpression) As System.Linq.Expressions.Expression
            _mem = m
            Return Nothing
        End Function

        Protected Overrides Function VisitConstant(ByVal c As System.Linq.Expressions.ConstantExpression) As System.Linq.Expressions.Expression
            If c.Type.IsGenericType Then
                _t = c.Type.GetGenericArguments(0)
            Else
                If _skip Then
                    _q.RowNumberFilter = New TableFilter(QueryCmdBase.RowNumerColumn, New ScalarValue(CInt(Eval(c))), Worm.Criteria.FilterOperation.GreaterThan)
                Else
                    If _q.RowNumberFilter IsNot Nothing Then
                        Dim f As Integer = CInt(CType(Query.RowNumberFilter.Value, ScalarValue).Value)
                        _q.RowNumberFilter = New TableFilter(QueryCmdBase.RowNumerColumn, New BetweenValue(f + 1, f + CInt(Eval(c))), Worm.Criteria.FilterOperation.Between)
                    Else
                        _q.Top = New Top(CInt(Eval(c)))
                    End If
                End If
            End If
            Return Nothing
        End Function

        Private Sub Visit2ParamOverride(ByVal m As System.Linq.Expressions.MethodCallExpression)
            Me.Visit(m.Arguments(0))
            If m.Arguments.Count > 1 Then
                Dim v = New FilterVisitor(_schema, Me)
                v.Visit(m.Arguments(1))
                _q.Filter = v.Filter
            End If
        End Sub

        Protected Overrides Function VisitMethodCall(ByVal m As System.Linq.Expressions.MethodCallExpression) As System.Linq.Expressions.Expression
            Select Case m.Method.Name
                Case "Where"
                    Me.Visit(m.Arguments(0))
                    Dim v = New FilterVisitor(_schema, Me)
                    v.Visit(m.Arguments(1))
                    _q.Filter = v.Filter
                Case "OrderBy"
                    Me.Visit(m.Arguments(0))
                    Dim sv As New SortVisitor(_schema)
                    sv.Visit(m.Arguments(1))
                    _so = sv.Sort
                Case "OrderByDescending"
                    Me.Visit(m.Arguments(0))
                    Dim sv As New SortVisitor(_schema)
                    sv.Visit(m.Arguments(1))
                    _so = sv.Sort
                    _so.Order(False)
                Case "ThenBy"
                    Me.Visit(m.Arguments(0))
                    Dim sv As New SortVisitor(_schema)
                    sv.Visit(m.Arguments(1))
                    _so = sv.Sort.NextSort(_so)
                Case "ThenByDescending"
                    Me.Visit(m.Arguments(0))
                    Dim sv As New SortVisitor(_schema)
                    sv.Visit(m.Arguments(1))
                    _so = sv.Sort.NextSort(_so)
                    _so.Order(False)
                Case "Select"
                    Me.Visit(m.Arguments(0))
                    Me.Visit(m.Arguments(1))
                    If _mem IsNot Nothing OrElse _new IsNot Nothing Then
                        _q.WithLoad = True
                    End If
                Case "Distinct"
                    Me.Visit(m.Arguments(0))
                    _q.Distinct = True
                Case "Count"
                    Visit2ParamOverride(m)
                    _q.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() {New Aggregate(AggregateFunction.Count)})
                Case "LongCount"
                    Visit2ParamOverride(m)
                    _q.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() {New Aggregate(AggregateFunction.BigCount)})
                Case "First"
                    Visit2ParamOverride(m)
                    _ct = Linq.Constr.First
                    _q.Top = New Worm.Query.Top(1)
                Case "FirstOrDefault"
                    Visit2ParamOverride(m)
                    _ct = Linq.Constr.FirstOrDef
                    _q.Top = New Worm.Query.Top(1)
                Case "Single"
                    Visit2ParamOverride(m)
                    _ct = Linq.Constr.Single
                Case "SingleOrDefault"
                    Visit2ParamOverride(m)
                    _ct = Linq.Constr.SingleOrDef
                Case "Last"
                    Visit2ParamOverride(m)
                    _ct = Linq.Constr.Last
                Case "LastOrDefault"
                    Visit2ParamOverride(m)
                    _ct = Linq.Constr.LastOrDef
                Case "Take"
                    Me.Visit(m.Arguments(0))
                    Me.Visit(m.Arguments(1))
                Case "Skip"
                    Me.Visit(m.Arguments(0))
                    _skip = True
                    Me.Visit(m.Arguments(1))
                    _skip = False
                Case "Sum"
                    Me.Visit(m.Arguments(0))
                    Dim ag As New AgVisitor(_schema)
                    ag.Visit(m.Arguments(1))
                    _q.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() {New Aggregate(AggregateFunction.Sum, ag.Type, ag.Field)})
                Case "Contains", "DefaultIfEmpty", "Concat", "ElementAt", "ElementAtOrDefault", "SequenceEqual", "Union", "TakeWhile", "SkipWhile", "Reverse", "All", "Any", "Aggregate"
                    Throw New NotSupportedException
                Case Else
                    Throw New NotImplementedException
            End Select
            Return Nothing
        End Function

        Public Function GetNew(ByVal o As OrmBase) As Object
            If _new Is Nothing Then
                If _mem IsNot Nothing Then
                    Return Eval(Expression.MakeMemberAccess(Expression.Constant(o), _mem.Member))
                Else
                    Throw New InvalidOperationException
                End If
            End If

            'Dim nex = Expression.[New](_new.Constructor, GetArgs(_new.Arguments, o), _new.Members)
            'Dim l As LambdaExpression = Expression.Lambda(nex.Type, nex, Nothing)
            'Dim f As [Delegate] = l.Compile()
            'Return f.DynamicInvoke(Nothing)
            'Return Eval(nex)
            Return _new.Constructor.Invoke(GetArgsO(_new.Arguments, o))
        End Function

        Protected Function GetArgs(ByVal args As ReadOnlyCollection(Of Expression), ByVal o As OrmBase) As Expression()
            Dim l As New List(Of ConstantExpression)
            For Each exp In args
                Dim mem As MemberExpression = TryCast(exp, MemberExpression)
                Dim e As Expression = Nothing
                If mem IsNot Nothing Then
                    e = Expression.MakeMemberAccess(Expression.Constant(o), mem.Member)
                Else
                End If
                'Dim v As Object = exp.Type.InvokeMember(Nothing, BindingFlags.CreateInstance, Nothing, Nothing, New Object() {Eval(e)})
                'Dim v As Object = Activator.CreateInstance(exp.Type, New Object() {Eval(e)})
                Dim v As Object = Eval(e)
                'Dim v As Object = Eval(Expression.[New](exp.Type.GetConstructor(New Type() {}), New Expression() {expression.Constant(Eval(e))}))
                l.Add(Expression.Constant(v))
            Next
            Return l.ToArray
        End Function

        Protected Function GetArgsO(ByVal args As ReadOnlyCollection(Of Expression), ByVal o As OrmBase) As Object()
            Dim l As New List(Of Object)
            For Each exp In args
                Dim mem As MemberExpression = TryCast(exp, MemberExpression)
                Dim e As Expression = exp
                If mem IsNot Nothing Then
                    e = Expression.MakeMemberAccess(Expression.Constant(o), mem.Member)
                End If
                'Dim v As Object = exp.Type.InvokeMember(Nothing, BindingFlags.CreateInstance, Nothing, Nothing, New Object() {Eval(e)})
                'Dim v As Object = Activator.CreateInstance(exp.Type, New Object() {Eval(e)})
                Dim v As Object = Eval(e)
                'Dim v As Object = Eval(Expression.[New](exp.Type.GetConstructor(New Type() {}), New Expression() {expression.Constant(Eval(e))}))
                'l.Add(Expression.Constant(v))
                l.Add(v)
            Next
            Return l.ToArray
        End Function
    End Class
End Namespace