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
Imports Worm.Expressions

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
                Throw New NotImplementedException
            End Using
        End Function

        Protected Sub GetManager(ByVal o As IEntity, ByVal args As ManagerRequiredArgs)
            args.Manager = _ctx.CreateReadonlyManager
        End Sub

        Protected Sub ObjectCreated(ByVal o As ICachedEntity, ByVal mgr As OrmManager)
            AddHandler o.ManagerRequired, AddressOf GetManager
        End Sub

        Public Function Execute(Of TResult)(ByVal expression As System.Linq.Expressions.Expression) As TResult Implements System.Linq.IQueryProvider.Execute
            Dim mgr = _ctx.CreateReadonlyManager
            mgr.RaiseObjectCreation = True
            Try
                AddHandler mgr.ObjectCreated, AddressOf ObjectCreated
                Dim ev As New QueryVisitor(_ctx.Schema)
                ev.Visit(expression)
                'Dim q As New Worm.Query.QueryCmd(Of TResult)(ev.Query)
                Dim q As Worm.Query.QueryCmd = ev.Query
                Dim rt As Type = GetType(TResult)
                If GetType(IEnumerator).IsAssignableFrom(rt) Then
                    Dim t As Type = rt.GetGenericArguments(0)
                    If GetType(OrmBase).IsAssignableFrom(t) Then
                        q.SelectedType = t
                        Dim e As IEnumerator = q.ToList(mgr).GetEnumerator
                        Return CType(e, TResult)
                    Else
                        Dim lt As Type = GetType(List(Of ))
                        Dim glt As Type = lt.MakeGenericType(New Type() {t})
                        Dim l As IList = CType(Activator.CreateInstance(glt), System.Collections.IList)
                        q.SelectedType = ev.T
                        Dim e As IEnumerator = q.ToList(mgr).GetEnumerator
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
                    If rt.IsValueType OrElse rt Is GetType(String) Then
                        l = q.ToSimpleListDyn(Of TResult)(mgr)
                    Else
                        If GetType(OrmBase).IsAssignableFrom(rt) Then
                            l = CType(q.ToList(mgr), IList(Of TResult))
                        Else
                            l = New List(Of TResult)
                            Dim e As IEnumerator = q.ToList(mgr).GetEnumerator
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
            Finally
                If mgr IsNot Nothing Then
                    mgr.Dispose()
                End If
            End Try
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

    'Public Class FilterValueVisitor
    '    Inherits MyExpressionVisitor

    '    Private _v As IParamFilterValue
    '    Private _p As Orm.OrmProperty
    '    Private _q As QueryVisitor
    '    Private _cust As CustomExp

    '    Sub New(ByVal schema As QueryGenerator, ByVal q As QueryVisitor)
    '        MyBase.new(schema)
    '        _q = q
    '    End Sub

    '    Public ReadOnly Property Prop() As Orm.OrmProperty
    '        Get
    '            Return _p
    '        End Get
    '    End Property

    '    Public ReadOnly Property Value() As IParamFilterValue
    '        Get
    '            Return _v
    '        End Get
    '    End Property

    '    Public ReadOnly Property Custom() As CustomExp
    '        Get
    '            Return _cust
    '        End Get
    '    End Property

    '    Protected Overrides Function VisitConstant(ByVal c As System.Linq.Expressions.ConstantExpression) As System.Linq.Expressions.Expression
    '        If c.Type.IsPrimitive OrElse GetType(String) Is c.Type Then
    '            _v = New ScalarValue(c.Value)
    '            'Else
    '            '    _v = New ScalarValue(Eval(c))
    '        End If
    '        Return Nothing
    '    End Function

    '    Protected Overrides Function VisitMemberAccess(ByVal m As System.Linq.Expressions.MemberExpression) As System.Linq.Expressions.Expression
    '        If m.Expression Is Nothing Then
    '            If m.Type Is GetType(Date) Then
    '                Select Case m.Member.Name
    '                    Case "Now"
    '                        _v = New LiteralValue(CType(_schema, Database.SQLGenerator).GetDate)
    '                    Case Else
    '                        Throw New NotImplementedException(String.Format( _
    '                            "Method {0} of type {1} is not implemented", m.Member.Name, m.Type.FullName))
    '                End Select
    '            Else
    '                Throw New NotImplementedException("Type " & m.Type.FullName & " is not implemented")
    '            End If
    '        Else
    '            Dim b As New FilterValueVisitor(_schema, _q)
    '            b.Visit(m.Expression)
    '            If b._p Is Nothing Then
    '                Dim i = 10
    '            Else
    '                If b._p.Type Is Nothing Then
    '                    _p = _q.GetProperty(m.Member.Name)
    '                Else
    '                    If Not String.IsNullOrEmpty(b._p.Field) Then
    '                        If m.Expression.Type.FullName.StartsWith("System.Nullable") Then
    '                            Select Case m.Member.Name
    '                                Case "Value"
    '                                    _p = b._p
    '                                Case Else
    '                                    Throw New NotImplementedException(String.Format( _
    '                                        "Method {0} of type {1} is not implemented", m.Member.Name, m.Expression.Type.FullName))
    '                            End Select
    '                        ElseIf m.Expression.Type Is GetType(Date) Then
    '                            Select Case m.Member.Name
    '                                Case "Year"
    '                                    _cust = New CustomExp(CType(_schema, Database.SQLGenerator).GetYear, _
    '                                        New Pair(Of Object, String)() {New Pair(Of Object, String)(b._p.Type, b._p.Field)})
    '                                Case Else
    '                                    Throw New NotImplementedException(String.Format( _
    '                                        "Method {0} of type {1} is not implemented", m.Member.Name, m.Expression.Type.FullName))
    '                            End Select
    '                        Else
    '                            Throw New NotImplementedException("Type " & m.Type.FullName & " is not implemented")
    '                        End If
    '                    Else
    '                        _p = New OrmProperty(b._p.Type, GetField(b._p.Type, m.Member.Name))
    '                    End If
    '                End If
    '            End If
    '        End If
    '        Return Nothing
    '    End Function

    '    Protected Overrides Function VisitParameter(ByVal p As System.Linq.Expressions.ParameterExpression) As System.Linq.Expressions.Expression
    '        If GetType(OrmBase).IsAssignableFrom(p.Type) Then
    '            _p = New OrmProperty(p.Type, Nothing)
    '        Else
    '            _p = New OrmProperty(CType(Nothing, Type), Nothing)
    '        End If
    '        'If GetType(OrmBase).IsAssignableFrom(m.Expression.Type) Then
    '        '    Dim field As String = GetField(p.Type, m.Member.Name)
    '        '    _p = New Orm.OrmProperty(p.Type, field)
    '        '    Return Nothing
    '        'Else
    '        '    _p = _q.GetProperty(m.Member.Name)
    '        'End If
    '        Return Nothing
    '    End Function
    'End Class

    Public Class FilterVisitorBase
        Inherits MyExpressionVisitor

        Sub New(ByVal schema As ObjectMappingEngine)
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

    Public Class SimpleExpVis
        Inherits MyExpressionVisitor

        'Private _t As Type
        'Public ReadOnly Property Type() As Type
        '    Get
        '        Return _t
        '    End Get
        'End Property

        'Private _f As String
        'Public ReadOnly Property Field() As String
        '    Get
        '        Return _f
        '    End Get
        'End Property

        'Private _prop As String
        'Public ReadOnly Property PropName() As String
        '    Get
        '        Return _prop
        '    End Get
        'End Property

        Private _exp As UnaryExp
        Public ReadOnly Property Exp() As UnaryExp
            Get
                Return _exp
            End Get
        End Property

        Private _q As QueryVisitor
        Private _mem As String
        Private _cannotEval As Boolean

        Public Sub New(ByVal schema As ObjectMappingEngine, ByVal q As QueryVisitor)
            MyBase.new(schema)
            _q = q
        End Sub

        Protected Sub New(ByVal schema As ObjectMappingEngine, ByVal q As QueryVisitor, ByVal mem As String)
            MyBase.new(schema)
            _q = q
            _mem = mem
        End Sub

        Protected Overrides Function VisitBinary(ByVal b As System.Linq.Expressions.BinaryExpression) As System.Linq.Expressions.Expression
            Select Case b.NodeType
                Case ExpressionType.Add, ExpressionType.AddChecked
                    Dim l As New SimpleExpVis(_schema, _q)
                    l.Visit(b.Left)
                    Dim r As New SimpleExpVis(_schema, _q)
                    r.Visit(b.Right)
                    _exp = New BinaryExp(ExpOperation.Add, l.Exp, r.Exp)
                Case Else
                    Throw New NotImplementedException
            End Select
            Return Nothing
        End Function

        Protected Overrides Function VisitMemberAccess(ByVal m As System.Linq.Expressions.MemberExpression) As System.Linq.Expressions.Expression
            If m.Expression Is Nothing Then
                If m.Type Is GetType(Date) Then
                    Select Case m.Member.Name
                        Case "Now"
                            _exp = New UnaryExp(New LiteralValue(CType(_schema, Database.SQLGenerator).GetDate))
                        Case Else
                            Throw New NotImplementedException(String.Format( _
                                "Method {0} of type {1} is not implemented", m.Member.Name, m.Type.FullName))
                    End Select
                Else
                    Throw New NotImplementedException("Type " & m.Type.FullName & " is not implemented")
                End If
            Else
                Dim b As New SimpleExpVis(_schema, _q, m.Member.Name)
                b.Visit(m.Expression)
                If Not b._cannotEval Then
                    Dim v As Object = Eval(m)
                    Dim o As IOrmBase = TryCast(v, IOrmBase)
                    If o IsNot Nothing Then
                        _exp = New UnaryExp(New EntityValue(o))
                    Else
                        _exp = New UnaryExp(New ScalarValue(v))
                    End If
                Else
                    If Not String.IsNullOrEmpty(b._mem) Then
                        If m.Expression.Type.FullName.StartsWith("System.Nullable") Then
                            Select Case m.Member.Name
                                Case "Value"
                                    _exp = b._exp
                                Case Else
                                    Throw New NotImplementedException(String.Format( _
                                        "Method {0} of type {1} is not implemented", m.Member.Name, m.Expression.Type.FullName))
                            End Select
                        ElseIf m.Expression.Type Is GetType(Date) Then
                            Select Case m.Member.Name
                                Case "Year"
                                    Dim p As Pair(Of Object, String) = Nothing
                                    Dim ev As EntityPropValue = TryCast(b._exp.Value, EntityPropValue)
                                    Dim pr As SelectExpression = Nothing
                                    If ev Is Nothing Then
                                        pr = _q.GetProperty(CType(b._exp.Value, ComputedValue).Alias)
                                    Else
                                        pr = ev.OrmProp
                                    End If
                                    If pr.Table Is Nothing Then
                                        p = New Pair(Of Object, String)(pr.Type, pr.Field)
                                    Else
                                        p = New Pair(Of Object, String)(pr.Table, pr.Column)
                                    End If
                                    _exp = New UnaryExp(New CustomValue(CType(_schema, Database.SQLGenerator).GetYear, _
                                        New Pair(Of Object, String)() {p}))
                                Case Else
                                    Throw New NotImplementedException(String.Format( _
                                        "Method {0} of type {1} is not implemented", m.Member.Name, m.Expression.Type.FullName))
                            End Select
                        Else
                            Throw New NotImplementedException("Type " & m.Type.FullName & " is not implemented")
                        End If
                    Else
                        _exp = b._exp
                    End If
                    'If b._p.Type Is Nothing Then
                    '    _p = _q.GetProperty(m.Member.Name)
                    'Else
                    '    If Not String.IsNullOrEmpty(b._p.Field) Then
                    '        If m.Expression.Type.FullName.StartsWith("System.Nullable") Then
                    '            Select Case m.Member.Name
                    '                Case "Value"
                    '                    _p = b._p
                    '                Case Else
                    '                    Throw New NotImplementedException(String.Format( _
                    '                        "Method {0} of type {1} is not implemented", m.Member.Name, m.Expression.Type.FullName))
                    '            End Select
                    '        ElseIf m.Expression.Type Is GetType(Date) Then
                    '            Select Case m.Member.Name
                    '                Case "Year"
                    '                    _cust = New CustomExp(CType(_schema, Database.SQLGenerator).GetYear, _
                    '                        New Pair(Of Object, String)() {New Pair(Of Object, String)(b._p.Type, b._p.Field)})
                    '                Case Else
                    '                    Throw New NotImplementedException(String.Format( _
                    '                        "Method {0} of type {1} is not implemented", m.Member.Name, m.Expression.Type.FullName))
                    '            End Select
                    '        Else
                    '            Throw New NotImplementedException("Type " & m.Type.FullName & " is not implemented")
                    '        End If
                    '    Else
                    '        _p = New OrmProperty(b._p.Type, GetField(b._p.Type, m.Member.Name))
                    '    End If
                    'End If
                End If
            End If
            Return Nothing
        End Function

        Protected Overrides Function VisitParameter(ByVal p As System.Linq.Expressions.ParameterExpression) As System.Linq.Expressions.Expression
            _cannotEval = True
            If GetType(OrmBase).IsAssignableFrom(p.Type) Then
                _exp = New UnaryExp(_mem, New EntityPropValue(p.Type, GetField(p.Type, _mem)))
                _mem = Nothing
                '_t = p.Type
                '_prop = m.Member.Name
            Else
                Dim pr As SelectExpression = _q.GetProperty(p.Name)
                If pr IsNot Nothing Then
                    _exp = New UnaryExp(New EntityPropValue(pr))
                    _mem = Nothing
                ElseIf p.Type.Name.Contains("$") Then
                    _exp = New UnaryExp(New ComputedValue(_mem))
                    _mem = Nothing
                Else
                    _exp = _q.Sel(0)
                End If
                '_prop = pr.Field
                '_t = pr.Type
            End If
            Return Nothing
        End Function

        Protected Overrides Function VisitConstant(ByVal c As System.Linq.Expressions.ConstantExpression) As System.Linq.Expressions.Expression
            If _mem Is Nothing Then
                _exp = New UnaryExp(New ScalarValue(Eval(c)))
            End If
            'If c.Type.IsPrimitive OrElse GetType(String) Is c.Type Then
            '    _exp = New UnaryExp(New ScalarValue(c.Value))
            '    'Else
            '    '    _v = New ScalarValue(Eval(c))
            'End If
            Return Nothing
        End Function
    End Class

    Public Class SortVisitor
        Inherits FilterVisitorBase

        Private _sort As New List(Of UnaryExp)
        Public ReadOnly Property Sort() As SortOrder
            Get
                Dim so As SortOrder = GetSO(_sort(0))
                For i As Integer = 1 To _sort.Count - 1
                    so = so.NextSort(GetSO(_sort(i)))
                Next
                Return so
            End Get
            'Set(ByVal value As Sort)
            '    _sort = value
            'End Set
        End Property

        Protected Function GetSO(ByVal exp As UnaryExp) As SortOrder
            Dim e As EntityPropValue = TryCast(exp.Value, EntityPropValue)
            If e IsNot Nothing Then
                Return Orm.Sorting.Field(e.OrmProp.Type, e.OrmProp.Field)
            Else
                Dim ce As ComputedValue = TryCast(exp.Value, ComputedValue)
                If ce IsNot Nothing Then
                    Dim p As SelectExpression = _q.GetProperty(ce.Alias)
                    Return Orm.Sorting.Field(p.Type, p.Field)
                Else
                    Throw New NotSupportedException
                End If
            End If
        End Function

        Private _q As QueryVisitor

        'Public Function Order(ByVal desc As Boolean) As SortVisitor
        '    _sort.Order(desc)
        '    Return Me
        'End Function

        Sub New(ByVal schema As ObjectMappingEngine, ByVal q As QueryVisitor)
            MyBase.new(schema)
            _q = q
        End Sub

        Protected Overrides Function VisitMemberAccess(ByVal m As System.Linq.Expressions.MemberExpression) As System.Linq.Expressions.Expression
            If GetType(OrmBase).IsAssignableFrom(m.Expression.Type) Then
                Dim p As ParameterExpression = CType(m.Expression, ParameterExpression)
                Dim field As String = GetField(p.Type, m.Member.Name)
                _sort.Add(New UnaryExp(m.Member.Name, New EntityPropValue(p.Type, field)))
                'If _sort Is Nothing Then
                '    _sort = Worm.Orm.Sorting.Field(field)
                'Else
                '    _sort.NextField(field)
                'End If
            Else
                'Dim pr As OrmProperty = _q.GetProperty(m.Member.Name)
                'Dim s As String = pr.Field
                'If _sort Is Nothing Then
                '    _sort = Worm.Orm.Sorting.Field(s)
                'Else
                '    _sort.NextField(s)
                'End If
                _sort.Add(New UnaryExp(m.Member.Name, New ComputedValue(m.Member.Name)))
            End If
            Return Nothing
        End Function

        Protected Overrides Function VisitParameter(ByVal p As System.Linq.Expressions.ParameterExpression) As System.Linq.Expressions.Expression
            'Dim pr As OrmProperty = _q.GetProperty(p.Name)
            'Dim s As String = pr.Field
            'If _sort Is Nothing Then
            '    _sort = Worm.Orm.Sorting.Field(s)
            'Else
            '    _sort.NextField(s)
            'End If
            _sort.Add(New UnaryExp(p.Name, New ComputedValue(p.Name)))
            Return Nothing
        End Function

    End Class

    Public Class FilterVisitor
        Inherits FilterVisitorBase

        Private _c As New Condition.ConditionConstructor
        Private _q As QueryVisitor

        Sub New(ByVal schema As ObjectMappingEngine, ByVal q As QueryVisitor)
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
            Dim lf As New SimpleExpVis(_schema, _q)
            Dim rf As New SimpleExpVis(_schema, _q)
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
            Filter = UnaryExp.CreateFilter(_schema, _q.Translate(lf.Exp), _q.Translate(rf.Exp), fo)
            'If lf.Prop IsNot Nothing Then
            '    Filter = New EntityFilter(lf.Prop.Type, lf.Prop.Field, rf.Value, fo)
            'ElseIf rf.Prop IsNot Nothing Then
            '    Filter = New EntityFilter(rf.Prop.Type, rf.Prop.Field, lf.Value, Invert(fo))
            'Else
            '    If lf.Custom IsNot Nothing Then
            '        Filter = New CustomFilter(lf.Custom.Format, rf.Value, fo, lf.Custom.Values)
            '    ElseIf rf.Custom IsNot Nothing Then
            '        Filter = New CustomFilter(rf.Custom.Format, lf.Value, Invert(fo), rf.Custom.Values)
            '    End If
            'End If
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

        Private _q As Query.QueryCmd
        Private _so As SortOrder
        Private _new As NewExpression
        'Private _mem As MemberExpression
        Private _t As Type
        Private _ct As Constr
        Private _skip As Boolean
        Private _sel As List(Of UnaryExp)
        Private _sub As Boolean

        Public ReadOnly Property Sel() As List(Of UnaryExp)
            Get
                Return _sel
            End Get
        End Property

        Public Function IsSubQueryRequiredBySelect() As Boolean
            Return _sub
            'If _sel IsNot Nothing Then
            '    For Each e As UnaryExp In _sel
            '        If e.GetType Is GetType(BinaryExp) Then
            '            Return True
            '        End If
            '    Next
            'End If
            'Return False
        End Function

        Public Function IsSubQueryRequired() As Boolean
            Return IsSubQueryRequiredBySelect() OrElse _q.propTop IsNot Nothing OrElse _q.RowNumberFilter IsNot Nothing
        End Function

        Public Function IsLoadRequired() As Boolean
            If _sel IsNot Nothing Then
                For Each e As UnaryExp In _sel
                    If IsOrmExp(e) Then
                        Return True
                    End If
                Next
            End If
            Return False
        End Function

        Public Function IsOrmExp(ByVal e As UnaryExp) As Boolean
            Dim b As BinaryExp = TryCast(e, BinaryExp)
            If b Is Nothing Then
                Return TryCast(e.Value, EntityPropValue) IsNot Nothing
            Else
                If IsOrmExp(b.Left) Then
                    Return True
                Else
                    Return IsOrmExp(b.Right)
                End If
            End If
        End Function

        Sub New(ByVal schema As ObjectMappingEngine)
            MyBase.new(schema)
            _q = New Query.QueryCmd(CType(Nothing, Type))
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

        Public ReadOnly Property Query() As Query.QueryCmd
            Get
                If _so IsNot Nothing Then
                    _q.propSort = _so
                    _so = Nothing
                End If
                Return _q
            End Get
        End Property

        Protected Overrides Function VisitWithLoad(ByVal w As WithLoadExpression) As System.Linq.Expressions.Expression
            _q.propWithLoad = True
            Return MyBase.VisitWithLoad(w)
        End Function

        'Protected Friend Function _GetProperties() As Orm.OrmProperty()
        '    'If _new Is Nothing Then
        '    '    Throw New InvalidOperationException
        '    'End If

        '    'Dim l As New List(Of OrmProperty)
        '    'For i As Integer = 0 To _new.Arguments.Count - 1
        '    '    l.Add(GetMember(i))
        '    'Next

        '    'Return l.ToArray
        '    If _sel Is Nothing Then
        '        Throw New InvalidOperationException
        '    End If

        '    Dim l As New List(Of OrmProperty)
        '    For Each u As UnaryExp In _sel
        '        'Fim()
        '    Next

        '    Return l.ToArray
        'End Function

        Protected Friend Function Translate(ByVal exp As UnaryExp) As UnaryExp

            If _sel IsNot Nothing Then
                Dim c As ComputedValue = TryCast(exp.Value, ComputedValue)
                If c IsNot Nothing Then
                    For Each e As UnaryExp In _sel
                        If e.Alias = c.Alias Then
                            Dim ev As EntityPropValue = TryCast(e.Value, EntityPropValue)
                            If ev IsNot Nothing Then
                                Return e
                            End If
                        End If
                    Next
                Else
                    Dim be As BinaryExp = TryCast(exp, BinaryExp)
                    If be IsNot Nothing Then
                        Dim l As UnaryExp = Translate(be.Left)
                        If l.Equals(be.Left) Then
                            Dim r As UnaryExp = Translate(be.Right)
                            If Not r.Equals(be.Right) Then
                                exp = New BinaryExp(be.Operation, l, r)
                                exp.Alias = be.Alias
                            End If
                        Else
                            Dim r As UnaryExp = Translate(be.Right)
                            exp = New BinaryExp(be.Operation, l, r)
                            exp.Alias = be.Alias
                        End If
                    End If
                End If
            End If

            Return exp
        End Function

        Protected Friend Function GetProperty(ByVal name As String) As Orm.SelectExpression
            If _sel Is Nothing Then
                Throw New InvalidOperationException
            End If

            For Each e As UnaryExp In _sel
                Dim ev As EntityPropValue = CType(e.Value, EntityPropValue)
                If ev IsNot Nothing AndAlso e.Alias = name Then
                    Return ev.OrmProp
                End If
            Next

            Return Nothing
        End Function

        'Protected Function GetMember(ByVal i As Integer) As OrmProperty
        '    Dim m As MemberExpression = CType(_new.Arguments(i), MemberExpression)
        '    Dim t As Type = m.Member.DeclaringType
        '    If t.FullName.StartsWith("Worm.Orm.OrmBaseT") Then
        '        t = t.GetGenericArguments(0)
        '    End If
        '    Return New Orm.OrmProperty(t, GetField(t, m.Member.Name))
        'End Function

        'Protected Friend Function GetIndex(ByVal tt As Type, ByVal name As String) As Integer
        '    If _new Is Nothing Then
        '        Throw New InvalidOperationException
        '    End If

        '    For i As Integer = 0 To _new.Arguments.Count - 1
        '        If _new.Constructor.GetParameters(i).Name = name Then
        '            Dim m As MemberExpression = CType(_new.Arguments(i), MemberExpression)
        '            Dim t As Type = m.Member.DeclaringType
        '            If t.FullName.StartsWith("Worm.Orm.OrmBaseT") Then
        '                t = t.GetGenericArguments(0)
        '            End If
        '            If tt Is t Then
        '                Return i
        '            End If
        '        End If
        '    Next

        '    Return -1
        'End Function

        'Protected Friend Function GetProperty() As Orm.OrmProperty
        '    If _mem Is Nothing Then
        '        Throw New InvalidOperationException
        '    End If
        '    Dim t As Type = _mem.Member.DeclaringType
        '    Return New Orm.OrmProperty(t, GetField(t, _mem.Member.Name))
        'End Function

        Protected Friend Function GetIndex(ByVal exp As UnaryExp) As Integer
            If _sel IsNot Nothing Then
                For Each e As UnaryExp In _sel
                    Dim b As BinaryExp = TryCast(e, BinaryExp)
                    If b Is Nothing Then

                    Else
                    End If
                Next
            End If
            Return 0
        End Function

        Protected Function GetSelectList() As ReadOnlyCollection(Of SelectExpression)
            Dim l As New List(Of SelectExpression)
            If _sel IsNot Nothing Then
                For Each e As UnaryExp In _sel
                    l.Add(CType(e.Value, EntityPropValue).OrmProp)
                Next
                Return New ReadOnlyCollection(Of SelectExpression)(l)
            Else
                Return Nothing
            End If
        End Function

        Protected Overrides Function VisitNew(ByVal nex As System.Linq.Expressions.NewExpression) As System.Linq.Expressions.NewExpression
            If _sel Is Nothing Then
                _sel = New List(Of UnaryExp)
            End If
            _new = nex
            For i As Integer = 0 To _new.Arguments.Count - 1
                Dim s As New SimpleExpVis(_schema, Me)
                s.Visit(_new.Arguments(i))
                Dim e As UnaryExp = s.Exp
                _sel.Add(e)
                e.Alias = _new.Constructor.GetParameters(i).Name
                _sub = _sub OrElse e.GetType IsNot GetType(UnaryExp)
                'Dim name As String = _new.Constructor.GetParameters(i).Name
                'Dim m As MemberExpression = CType(_new.Arguments(i), MemberExpression)
                'Dim tt As Type = m.Expression.Type
                'If tt.FullName.StartsWith("Worm.Orm.OrmBaseT") Then
                '    tt = tt.GetGenericArguments(0)
                'End If
                '_sel.Add(New UnaryExp(m.Member.Name, New EntityPropValue(tt, GetField(tt, m.Member.Name))))
            Next

            Return Nothing
        End Function

        Protected Overrides Function VisitMemberAccess(ByVal m As System.Linq.Expressions.MemberExpression) As System.Linq.Expressions.Expression
            If _sel Is Nothing Then
                _sel = New List(Of UnaryExp)
            End If
            Dim tt As Type = m.Expression.Type
            If tt.FullName.StartsWith("Worm.Orm.OrmBaseT") Then
                tt = tt.GetGenericArguments(0)
            End If
            _sel.Add(New UnaryExp(m.Member.Name, New EntityPropValue(tt, GetField(tt, m.Member.Name))))
            Return Nothing
        End Function

        Protected Overrides Function VisitConstant(ByVal c As System.Linq.Expressions.ConstantExpression) As System.Linq.Expressions.Expression
            If c.Type.IsGenericType Then
                _t = c.Type.GetGenericArguments(0)
            Else
                If _skip Then
                    _q.RowNumberFilter = New TableFilter(QueryCmd.RowNumerColumn, New ScalarValue(CInt(Eval(c))), Worm.Criteria.FilterOperation.GreaterThan)
                Else
                    If _q.RowNumberFilter IsNot Nothing Then
                        Dim f As Integer = CInt(CType(Query.RowNumberFilter.Value, ScalarValue).Value)
                        _q.RowNumberFilter = New TableFilter(QueryCmd.RowNumerColumn, New BetweenValue(f + 1, f + CInt(Eval(c))), Worm.Criteria.FilterOperation.Between)
                    Else
                        _q.propTop = New Top(CInt(Eval(c)))
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
                    If _q.Filter IsNot Nothing Then
                        Dim cnd As New Worm.Database.Criteria.Conditions.Condition.ConditionConstructor
                        cnd.AddFilter(_q.Filter.Filter)
                        cnd.AddFilter(v.Filter, Criteria.Conditions.ConditionOperator.And)
                        _q.Filter = cnd.Condition
                    Else
                        _q.Filter = v.Filter
                    End If
                Case "OrderBy"
                    Me.Visit(m.Arguments(0))
                    Dim sv As New SortVisitor(_schema, Me)
                    sv.Visit(m.Arguments(1))
                    _so = sv.Sort
                Case "OrderByDescending"
                    Me.Visit(m.Arguments(0))
                    Dim sv As New SortVisitor(_schema, Me)
                    sv.Visit(m.Arguments(1))
                    _so = sv.Sort
                    _so.Order(False)
                Case "ThenBy"
                    Me.Visit(m.Arguments(0))
                    Dim sv As New SortVisitor(_schema, Me)
                    sv.Visit(m.Arguments(1))
                    _so = sv.Sort.NextSort(_so)
                Case "ThenByDescending"
                    Me.Visit(m.Arguments(0))
                    Dim sv As New SortVisitor(_schema, Me)
                    sv.Visit(m.Arguments(1))
                    _so = sv.Sort.NextSort(_so)
                    _so.Order(False)
                Case "Select"
                    Me.Visit(m.Arguments(0))
                    Me.Visit(m.Arguments(1))
                    _q.propWithLoad = IsLoadRequired()
                    _q.SelectList = GetSelectList()
                Case "Distinct"
                    Me.Visit(m.Arguments(0))
                    _q.propDistinct = True
                Case "Count"
                    Visit2ParamOverride(m)
                    _q.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() {New Aggregate(AggregateFunction.Count)})
                Case "LongCount"
                    Visit2ParamOverride(m)
                    _q.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() {New Aggregate(AggregateFunction.BigCount)})
                Case "First"
                    Visit2ParamOverride(m)
                    _ct = Linq.Constr.First
                    _q.propTop = New Worm.Query.Top(1)
                Case "FirstOrDefault"
                    Visit2ParamOverride(m)
                    _ct = Linq.Constr.FirstOrDef
                    _q.propTop = New Worm.Query.Top(1)
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
                    VisitAgg(m, AggregateFunction.Sum)
                Case "Min"
                    VisitAgg(m, AggregateFunction.Min)
                Case "Max"
                    VisitAgg(m, AggregateFunction.Max)
                Case "Average"
                    VisitAgg(m, AggregateFunction.Average)
                Case "Contains", "DefaultIfEmpty", "Concat", "ElementAt", "ElementAtOrDefault", "SequenceEqual", "Union", "TakeWhile", "SkipWhile", "Reverse", "All", "Any", "Aggregate"
                    Throw New NotSupportedException
                Case Else
                    Throw New NotImplementedException
            End Select
            Return Nothing
        End Function

        Protected Sub VisitAgg(ByVal m As MethodCallExpression, ByVal af As AggregateFunction)
            Me.Visit(m.Arguments(0))
            _q.propWithLoad = False
            If m.Arguments.Count > 1 Then
                Dim ag As New SimpleExpVis(_schema, Me)
                ag.Visit(m.Arguments(1))
                If IsSubQueryRequired() Then
                    Dim aq As New Query.QueryCmd(CType(Nothing, Type))
                    'Dim al As String = Nothing
                    'Dim num As Integer
                    Dim a As New Aggregate(af, GetIndex(ag.Exp))
                    aq.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() {a})
                    _q.OuterQuery = aq
                    Dim ev As EntityPropValue = TryCast(ag.Exp.Value, EntityPropValue)
                    Dim pr As SelectExpression = Nothing
                    If ev IsNot Nothing Then
                        pr = ev.OrmProp
                    Else
                        Dim cv As ComputedValue = TryCast(ag.Exp.Value, ComputedValue)
                        If cv IsNot Nothing Then
                            pr = GetProperty(cv.Alias)
                        Else
                            pr = GetProperty(ag.Exp.Alias)
                        End If
                    End If
                    _q.SelectList = New ReadOnlyCollection(Of SelectExpression)(New SelectExpression() {pr})
                    'If _mem Is Nothing AndAlso _new Is Nothing Then
                    '    'Visit(m.Arguments(1))
                    '    _q.WithLoad = False
                    '    _q.SelectList = New ReadOnlyCollection(Of OrmProperty)(New OrmProperty() {CType(ag.Exp.Value, EntityPropValue).OrmProp})
                    '    a = New Aggregate(af, 0)
                    'ElseIf _mem IsNot Nothing Then
                    '    _q.WithLoad = False
                    '    _q.SelectList = New ReadOnlyCollection(Of OrmProperty)(New OrmProperty() {GetProperty()})
                    '    a = New Aggregate(af, 0)
                    'ElseIf _new IsNot Nothing Then
                    '    _q.WithLoad = False
                    '    Dim e As EntityPropValue = CType(ag.Exp.Value, EntityPropValue)
                    '    a = New Aggregate(af, GetIndex(e.OrmProp.Type, e.OrmProp.Field))
                    '    _q.SelectList = New ReadOnlyCollection(Of OrmProperty)(GetProperties())
                    'End If
                    'aq.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() {a})
                    '_q.OuterQuery = aq
                Else
                    _q.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() {New Aggregate(af, Translate(ag.Exp))})
                    _q.SelectList = Nothing
                    '_q.WithLoad = False
                    'If _mem IsNot Nothing Then
                    '    _q.SelectList = New ReadOnlyCollection(Of OrmProperty)(New OrmProperty() {GetProperty()})
                    'Else
                    '    _q.SelectList = New ReadOnlyCollection(Of OrmProperty)(GetProperties())
                    'End If
                End If
            Else
                If IsSubQueryRequired() Then
                    '_q.WithLoad = False
                    'If _mem IsNot Nothing Then
                    '    _q.SelectList = New ReadOnlyCollection(Of OrmProperty)(New OrmProperty() {GetProperty()})
                    'Else
                    '    _q.SelectList = New ReadOnlyCollection(Of OrmProperty)(GetProperties())
                    'End If

                    Dim aq As New Query.QueryCmd(CType(Nothing, Type))
                    aq.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() {New Aggregate(af, 0)})
                    _q.OuterQuery = aq
                    _q.Select(New SelectExpression() {CType(_sel(0).Value, EntityPropValue).OrmProp})
                Else
                    'Dim tt As Type = _mem.Member.DeclaringType
                    'If tt.FullName.StartsWith("Worm.Orm.OrmBaseT") Then
                    '    tt = tt.GetGenericArguments(0)
                    'End If
                    _q.SelectList = Nothing
                    _q.Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(New AggregateBase() {New Aggregate(af, Translate(_sel(0)))})
                    '_q.SelectList = New ReadOnlyCollection(Of OrmProperty)(New OrmProperty() {GetProperty()})
                    '_q.WithLoad = False
                End If
            End If
        End Sub

        Public Function GetNew(ByVal o As OrmBase) As Object
            If _new Is Nothing Then
                If _sel IsNot Nothing Then
                    Return o.GetValue(CType(_sel(0).Value, EntityPropValue).OrmProp.Field)
                    'Return Eval(Expression.MakeMemberAccess(Expression.Constant(o), ))
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

        Protected Shared Function GetArgs(ByVal args As ReadOnlyCollection(Of Expression), ByVal o As OrmBase) As Expression()
            Dim l As New List(Of ConstantExpression)
            For Each Exp In args
                Dim mem As MemberExpression = TryCast(Exp, MemberExpression)
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

        Protected Shared Function GetArgsO(ByVal args As ReadOnlyCollection(Of Expression), ByVal o As OrmBase) As Object()
            Dim l As New List(Of Object)
            For Each Exp In args
                Dim mem As MemberExpression = TryCast(Exp, MemberExpression)
                Dim e As Expression = Exp
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