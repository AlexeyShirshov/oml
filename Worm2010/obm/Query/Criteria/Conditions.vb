Imports System.Collections.Generic
Imports Worm.Criteria.Core
Imports Worm.Entities.Meta
Imports Worm.Entities
Imports cc = Worm.Criteria.Conditions
Imports Worm.Criteria.Values
Imports Worm.Criteria
Imports Worm.Query

Namespace Criteria.Conditions

    Public Enum ConditionOperator
        [And]
        [Or]
    End Enum

    <Serializable()> _
    Public Class Condition
        Implements ITemplateFilter
        Implements IEvaluableFilter

        <Serializable()> _
        Public Class ConditionConstructor
            Implements ICloneable

            Protected _cond As Condition

            Public Sub New()
            End Sub

            Protected Sub New(ByVal cond As Condition)
                _cond = cond
            End Sub

            Public Function AddFilter(ByVal f As IFilter, ByVal [operator] As ConditionOperator) As ConditionConstructor
                Return AddFilter(f, [operator], True)
            End Function

            Protected Function AddFilter(ByVal f As IFilter, ByVal [operator] As ConditionOperator, ByVal useOper As Boolean) As ConditionConstructor
                If _cond Is Nothing AndAlso f IsNot Nothing Then
                    If GetType(IEntityFilter).IsAssignableFrom(CObj(f).GetType) Then
                        _cond = CreateEntityCondition(CType(f, IEntityFilter), Nothing, [operator])
                    Else
                        _cond = CreateCondition(f, Nothing, [operator])
                    End If
                ElseIf _cond IsNot Nothing AndAlso _cond._right Is Nothing AndAlso f IsNot Nothing Then
                    If Not GetType(IEntityFilter).IsAssignableFrom(CObj(f).GetType) AndAlso TypeOf (_cond) Is EntityCondition Then
                        _cond = CreateCondition(_cond, f, [operator])
                    Else
                        _cond._right = f
                        If useOper Then
                            _cond._oper = [operator]
                        End If
                    End If
                ElseIf f IsNot Nothing Then
                    If GetType(IEntityFilter).IsAssignableFrom(CObj(f).GetType) AndAlso TypeOf (_cond) Is EntityCondition Then
                        _cond = CreateEntityCondition(CType(_cond, IEntityFilter), CType(f, IEntityFilter), [operator])
                    Else
                        _cond = CreateCondition(_cond, f, [operator])
                    End If
                End If

                Return Me
            End Function

            Public Function AddFilter(ByVal f As IFilter) As ConditionConstructor
                Return AddFilter(f, ConditionOperator.And, False)
            End Function

            Public ReadOnly Property Condition() As IFilter
                Get
                    If _cond Is Nothing Then
                        Return Nothing
                    ElseIf _cond._right Is Nothing Then
                        Return _cond._left
                    Else
                        Return _cond
                    End If
                End Get
            End Property

            Public ReadOnly Property IsEmpty() As Boolean
                Get
                    Return _cond Is Nothing
                End Get
            End Property

            Protected Function CreateCondition(ByVal left As IFilter, ByVal right As IFilter, ByVal [operator] As ConditionOperator) As Condition
                Return New Condition(left, right, [operator])
            End Function

            Protected Function CreateEntityCondition(ByVal left As IEntityFilter, ByVal right As IEntityFilter, ByVal [operator] As ConditionOperator) As Condition
                Return New EntityCondition(left, right, [operator])
            End Function

            Protected Function _Clone() As Object Implements System.ICloneable.Clone
                Return New ConditionConstructor(CType(_cond.Clone, Worm.Criteria.Conditions.Condition))
            End Function

            Public Function Clone() As ConditionConstructor
                Return CType(_Clone(), ConditionConstructor)
            End Function
        End Class

        <Serializable()> _
        Protected Class ConditionTemplate
            Inherits TemplateBase

            Protected _con As Condition

            Public Sub New(ByVal con As Condition)
                _con = con
            End Sub

            Public Overrides Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String
                Dim sb As New StringBuilder
                sb.Append(CType(_con._left, ITemplateFilter).Template.GetStaticString(mpe))
                sb.Append(_con.Condition2String())
                If _con._right IsNot Nothing Then
                    sb.Append(CType(_con._right, ITemplateFilter).Template.GetStaticString(mpe))
                End If
                Return sb.ToString
            End Function

            Public Overrides Function _ToString() As String
                Return _con._ToString
            End Function
        End Class

        Protected _left As IFilter
        Protected _right As IFilter
        Protected _oper As ConditionOperator

        Public Sub New(ByVal left As IFilter, ByVal right As IFilter, ByVal [operator] As ConditionOperator)
            _left = left
            _right = right
            _oper = [operator]
        End Sub

        Public Function SetUnion(ByVal eu As Query.EntityUnion) As IFilter Implements IFilter.SetUnion
            If _left IsNot Nothing Then
                _left.SetUnion(eu)
            End If
            If _right IsNot Nothing Then
                _right.SetUnion(eu)
            End If
            Return Me
        End Function

        Public Function GetAllFilters() As IFilter() Implements IFilter.GetAllFilters
            Dim res As IFilter() = _left.GetAllFilters

            If _right IsNot Nothing Then
                Dim l As New List(Of IFilter)
                l.AddRange(res)
                l.AddRange(_right.GetAllFilters)
                res = l.ToArray
            End If

            Return res
        End Function

        'Public Function GetStaticString() As String Implements ITemplateFilter.GetStaticString
        '    Dim r As String = String.Empty
        '    If _right IsNot Nothing Then
        '        Dim rt As ITemplateFilter = CType(_right, ITemplateFilter)
        '        r = rt.GetStaticString
        '    End If

        '    Dim lt As ITemplateFilter = CType(_left, ITemplateFilter)
        '    Return lt.GetStaticString & Condition2String() & r
        'End Function

        'Public MustOverride Function MakeQueryStmt(ByVal schema As QueryGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As String Implements IFilter.MakeQueryStmt
        Public Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal fromClause As Query.QueryCmd.FromClauseDef,
                                      ByVal stmt As StmtGenerator, ByVal executor As Query.IExecutionContext, ByVal contextInfo As IDictionary,
                                      ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As String Implements IFilter.MakeQueryStmt
            If _right Is Nothing Then
                Return _left.MakeQueryStmt(schema, fromClause, stmt, executor, contextInfo, almgr, pname)
            End If
            Dim left = _left.MakeQueryStmt(schema, fromClause, stmt, executor, contextInfo, almgr, pname)
            Dim right = _right.MakeQueryStmt(schema, fromClause, stmt, executor, contextInfo, almgr, pname)

            If Not String.IsNullOrEmpty(left) AndAlso Not String.IsNullOrEmpty(right) Then
                Return "(" & left & Condition2String() & right & ")"
            ElseIf Not String.IsNullOrEmpty(left) Then
                Return left
            ElseIf Not String.IsNullOrEmpty(right) Then
                Return right
            Else
                Return String.Empty
            End If
        End Function

        Protected Function CreateMe(ByVal left As IFilter, ByVal right As IFilter, ByVal [operator] As ConditionOperator) As Condition
            If TryCast(left, IEntityFilter) Is Nothing OrElse TryCast(right, IEntityFilter) Is Nothing Then
                Return New Condition(left, right, [operator])
            Else
                Return New EntityCondition(CType(left, IEntityFilter), CType(right, IEntityFilter), [operator])
            End If
        End Function

        Public Overridable ReadOnly Property Template() As Core.ITemplate Implements Core.ITemplateFilterBase.Template
            Get
                Return New ConditionTemplate(Me)
            End Get
        End Property

        'Private Function _ReplaceTemplate(ByVal replacement As ITemplateFilter, ByVal replacer As ITemplateFilter) As ITemplateFilter Implements ITemplateFilter.ReplaceByTemplate
        '    Return ReplaceCondition(replacement, replacer)
        'End Function

        'Public Overridable Function ReplaceCondition(ByVal replacement As ITemplateFilter, ByVal replacer As ITemplateFilter) As Condition
        '    'If replacement.Template.Equals(CType(_left, ITemplateFilter).Template) Then
        '    '    Return CreateMe(replacer, _right, _oper)
        '    'ElseIf replacement.Template.Equals(CType(_right, ITemplateFilter).Template) Then
        '    '    Return CreateMe(_left, replacer, _oper)
        '    'Else
        '    '    Dim r As ITemplateFilter = CType(_left, ITemplateFilter).ReplaceByTemplate(replacement, replacer)

        '    '    If r IsNot Nothing Then
        '    '        Return CreateMe(r, _right, _oper)
        '    '    Else
        '    '        r = CType(_right, ITemplateFilter).ReplaceByTemplate(replacement, replacer)

        '    '        If r IsNot Nothing Then
        '    '            Return CreateMe(_left, r, _oper)
        '    '        End If
        '    '    End If
        '    'End If

        '    Return Nothing
        'End Function

        Public Overrides Function ToString() As String
            Return _ToString()
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _ToString.GetHashCode
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, Condition))
        End Function

        Public Overloads Function Equals(ByVal con As Condition) As Boolean
            If con Is Nothing Then
                Return False
            End If

            Dim r As Boolean = True

            If _right IsNot Nothing Then
                r = _right.Equals(con._right)
            End If

            Return _left.Equals(con._left) AndAlso r AndAlso _oper = con._oper
        End Function

        Protected Overridable Function Condition2String() As String
            If _oper = ConditionOperator.And Then
                Return " and "
            Else
                Return " or "
            End If
        End Function

        Private Function Equals1(ByVal f As IFilter) As Boolean Implements IFilter.Equals
            Return Equals(TryCast(f, Condition))
        End Function

        Public Function ReplaceFilter(ByVal oldValue As IFilter, ByVal newValue As IFilter) As IFilter Implements IFilter.ReplaceFilter
            If oldValue.Equals(_left) Then
                Return CreateMe(newValue, _right, _oper)
            ElseIf oldValue.Equals(_right) Then
                Return CreateMe(_left, newValue, _oper)
            Else
                Dim r As IFilter = _left.ReplaceFilter(oldValue, newValue)

                If r IsNot Nothing Then
                    Return CreateMe(r, _right, _oper)
                Else
                    r = _right.ReplaceFilter(oldValue, newValue)

                    If r IsNot Nothing Then
                        Return CreateMe(_left, r, _oper)
                    End If
                End If
            End If

            Return Nothing
        End Function

        Public Function MakeSingleStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam, ByVal executor As Query.IExecutionContext) As Pair(Of String) Implements ITemplateFilter.MakeSingleQueryStmt
            Throw New NotSupportedException
        End Function

        Private Function _ToString() As String Implements IFilter._ToString
            Dim r As String = String.Empty
            If _right IsNot Nothing Then
                r = _right._ToString()
            End If

            Return _left._ToString() & Condition2String() & r
        End Function

        Public Function ToStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IFilter.GetStaticString
            Dim r As String = String.Empty
            If _right IsNot Nothing Then
                r = _right.GetStaticString(mpe)
            End If

            Return _left.GetStaticString(mpe) & Condition2String() & r
        End Function

        Public ReadOnly Property Filter() As Core.IFilter Implements Core.IGetFilter.Filter
            Get
                Return Me
            End Get
        End Property

        'Public ReadOnly Property Filter(ByVal t As System.Type) As Core.IFilter Implements Core.IGetFilter.Filter
        '    Get
        '        Return Me
        '    End Get
        'End Property

        Protected Function _Clone() As Object Implements System.ICloneable.Clone
            Return CreateMe(_left, _right, _oper)
        End Function

        Public Function Clone() As Core.IFilter Implements Core.IFilter.Clone
            Return CreateMe(_left, _right, _oper)
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements Values.IQueryElement.Prepare
            If _left IsNot Nothing Then
                _left.Prepare(executor, schema, contextInfo, stmt, isAnonym)
            End If
            If _right IsNot Nothing Then
                _right.Prepare(executor, schema, contextInfo, stmt, isAnonym)
            End If
        End Sub

        Public Function RemoveFilter(ByVal f As Core.IFilter) As IFilter Implements Core.IFilter.RemoveFilter
            If _oper = ConditionOperator.And Then
                If _left IsNot Nothing Then
                    If _left.Equals(f) Then
                        _left = New CustomFilter("1", FilterOperation.Equal, New LiteralValue("1"))
                    Else
                        _left = _left.RemoveFilter(f)
                    End If
                End If
                If _right IsNot Nothing Then
                    If _right.Equals(f) Then
                        _right = New CustomFilter("1", FilterOperation.Equal, New LiteralValue("1"))
                    Else
                        _right = _right.RemoveFilter(f)
                    End If
                End If
            Else
                If _left IsNot Nothing Then
                    If _left.Equals(f) Then
                        If _right Is Nothing Then
                            _left = New CustomFilter("1", FilterOperation.Equal, New LiteralValue("1"))
                        Else
                            _left = New CustomFilter("1", FilterOperation.Equal, New LiteralValue("0"))
                        End If
                    Else
                        _left = _left.RemoveFilter(f)
                    End If
                End If
                If _right IsNot Nothing Then
                    If _right.Equals(f) Then
                        _right = New CustomFilter("1", FilterOperation.Equal, New LiteralValue("0"))
                    Else
                        _right = _right.RemoveFilter(f)
                    End If
                End If
            End If
            Return New Condition(_left, _right, _oper)
        End Function

        Public Function Eval(mpe As ObjectMappingEngine, d As Core.GetObj4IEntityFilterDelegate,
                              joins() As Joins.QueryJoin, objEU As EntityUnion) As Values.IEvaluableValue.EvalResult Implements Core.IEvaluableFilter.Eval
            If mpe Is Nothing Then
                Throw New ArgumentNullException("mpe")
            End If

            Dim b As IEvaluableValue.EvalResult = IEvaluableValue.EvalResult.Unknown

            Dim lef As IEvaluableFilter = TryCast(_left, IEvaluableFilter)
            If lef IsNot Nothing Then
                'If d Is Nothing Then
                '    Throw New ArgumentNullException("d")
                'End If
                'Dim p As Pair(Of _IEntity, IEntitySchema) = d(lef)
                'If p IsNot Nothing Then
                b = lef.Eval(mpe, d, joins, objEU)
                'End If
                'Else
                'Dim le As IEvaluableFilter = TryCast(_left, IEvaluableFilter)
                'If le IsNot Nothing Then
                '    b = le.Eval(mpe, d, joins, objEU)
                'End If
            End If

            If _right IsNot Nothing Then
                If _oper = ConditionOperator.And Then
                    If b = IEvaluableValue.EvalResult.Found Then
                        b = IEvaluableValue.EvalResult.Unknown
                        Dim ref As IEvaluableFilter = TryCast(_right, IEvaluableFilter)
                        If ref IsNot Nothing Then
                            'If d Is Nothing Then
                            '    Throw New ArgumentNullException("d")
                            'End If
                            'Dim p As Pair(Of _IEntity, IEntitySchema) = d(ref)
                            'If p IsNot Nothing Then
                            b = ref.Eval(mpe, d, joins, objEU)
                            'End If
                            'Else
                            '    Dim re As IEvaluableFilter = TryCast(_right, IEvaluableFilter)
                            '    If re IsNot Nothing Then
                            '        b = re.Eval(mpe, d, joins, objEU)
                            '    End If
                        End If
                    End If
                ElseIf _oper = ConditionOperator.Or Then
                    If b <> IEvaluableValue.EvalResult.Unknown Then
                        Dim r As IEvaluableValue.EvalResult = IEvaluableValue.EvalResult.Unknown
                        Dim ref As IEntityFilter = TryCast(_right, IEntityFilter)
                        If ref IsNot Nothing Then
                            'If d Is Nothing Then
                            '    Throw New ArgumentNullException("d")
                            'End If
                            Dim p As Pair(Of _IEntity, IEntitySchema) = d(ref)
                            If p IsNot Nothing Then
                                r = ref.EvalObj(mpe, p.First, p.Second, joins, objEU)
                            End If
                        Else
                            Dim re As IEvaluableFilter = TryCast(_right, IEvaluableFilter)
                            If re IsNot Nothing Then
                                r = re.Eval(mpe, d, joins, objEU)
                            End If
                        End If
                        If r <> IEvaluableValue.EvalResult.Unknown Then
                            If b <> IEvaluableValue.EvalResult.Found Then
                                b = r
                            End If
                        Else
                            b = r
                        End If
                    End If
                End If
            End If
            Return b
        End Function
    End Class

    <Serializable()> _
    Public Class EntityCondition
        Inherits Condition
        Implements IEntityFilter

        <Serializable()> _
        Protected Class EntityConditionTemplate
            Inherits Condition.ConditionTemplate
            Implements IOrmFilterTemplate

            Public Sub New(ByVal con As EntityCondition)
                MyBase.New(con)
            End Sub

            'Public Function MakeFilter(ByVal schema As OrmSchemaBase, ByVal oschema As IOrmObjectSchemaBase, ByVal obj As OrmBase) As IEntityFilter Implements IOrmFilterTemplate.MakeFilter
            '    Dim r As IEntityFilter = Nothing
            '    If Con._right IsNot Nothing Then
            '        r = Con.Right.GetFilterTemplate.MakeFilter(schema, oschema, obj)
            '    End If
            '    Dim e As EntityCondition = CreateCon(Con.Left.GetFilterTemplate.MakeFilter(schema, oschema, obj), r, Con._oper)
            '    Return e
            'End Function

            'Public Sub SetType(ByVal [alias] As ObjectAlias) Implements IOrmFilterTemplate.SetType
            '    Con.Left.GetFilterTemplate.SetType([alias])
            '    If Con._right IsNot Nothing Then
            '        Con.Right.GetFilterTemplate.SetType([alias])
            '    End If
            'End Sub

            Protected ReadOnly Property Con() As EntityCondition
                Get
                    Return CType(_con, EntityCondition)
                End Get
            End Property

            'Public Function GetStaticString() As String Implements Core.ITemplate.GetStaticString
            '    Dim s As New StringBuilder
            '    s.Append(_con.Left.Template.GetStaticString)
            '    s.Append(_con.Condition2String())
            '    If _con._right IsNot Nothing Then
            '        s.Append(_con.Right.Template.GetStaticString)
            '    End If
            '    Return s.ToString
            'End Function

            Public Function MakeHash(ByVal schema As ObjectMappingEngine, ByVal oschema As IEntitySchema, ByVal obj As ICachedEntity) As String Implements IOrmFilterTemplate.MakeHash
                If Con._right IsNot Nothing AndAlso Con._oper = ConditionOperator.Or Then
                    Dim l As String = Con.Left.GetFilterTemplate._ToString
                    Dim r As String = Con.Right.GetFilterTemplate._ToString
                    Return l & Con.Condition2String() & r
                Else
                    Dim l As String = Con.Left.GetFilterTemplate.MakeHash(schema, oschema, obj)
                    If Con._right IsNot Nothing Then
                        Dim r As String = Con.Right.GetFilterTemplate.MakeHash(schema, oschema, obj)
                        If r = EntityFilter.EmptyHash Then
                            If Con._oper <> ConditionOperator.And Then
                                l = r
                            End If
                        Else
                            l = l & Con.Condition2String() & r
                        End If
                    End If
                    Return l
                End If
            End Function

            'Protected MustOverride Function CreateCon(ByVal left As IEntityFilter, ByVal right As IEntityFilter, ByVal [operator] As ConditionOperator) As EntityCondition
        End Class

        Public Sub New(ByVal left As IEntityFilter, ByVal right As IEntityFilter, ByVal [operator] As ConditionOperator)
            MyBase.New(left, right, [operator])
        End Sub

        Protected ReadOnly Property Left() As IEntityFilter
            Get
                Return CType(_left, IEntityFilter)
            End Get
        End Property

        Protected ReadOnly Property Right() As IEntityFilter
            Get
                Return CType(_right, IEntityFilter)
            End Get
        End Property

        Public Overrides ReadOnly Property Template() As Worm.Criteria.Core.ITemplate
            Get
                Return New EntityConditionTemplate(Me)
            End Get
        End Property

        Public Overloads Function Eval(ByVal schema As ObjectMappingEngine, ByVal obj As _IEntity, ByVal oschema As IEntitySchema,
                              joins() As Joins.QueryJoin, objEU As EntityUnion) As IEvaluableValue.EvalResult Implements IEntityFilter.EvalObj
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim b As IEvaluableValue.EvalResult = Left.EvalObj(schema, obj, oschema, joins, objEU)
            If _right IsNot Nothing Then
                If _oper = ConditionOperator.And Then
                    If b = IEvaluableValue.EvalResult.Found Then
                        b = Right.EvalObj(schema, obj, oschema, joins, objEU)
                    End If
                ElseIf _oper = ConditionOperator.Or Then
                    If b = IEvaluableValue.EvalResult.NotFound Then
                        b = Right.EvalObj(schema, obj, oschema, joins, objEU)
                    End If
                End If
            End If
            Return b
        End Function

        Public Function GetFilterTemplate() As IOrmFilterTemplate Implements IEntityFilter.GetFilterTemplate
            Return CType(Template, IOrmFilterTemplate)
        End Function

        'Public Function PrepareValue(ByVal schema As ObjectMappingEngine, ByVal v As Object) As Object Implements IEntityFilter.PrepareValue
        '    Throw New NotSupportedException
        'End Function

        'Public Overrides Function ReplaceCondition(ByVal replacement As ITemplateFilter, ByVal replacer As ITemplateFilter) As Condition
        '    If replacement.Equals(_left) Then
        '        If GetType(IEntityFilter).IsAssignableFrom(CObj(replacer).GetType) Then
        '            Return CreateMeE(CType(replacer, IEntityFilter), CType(_right, IEntityFilter), _oper)
        '        Else
        '            Return CreateMe(replacer, _right, _oper)
        '        End If
        '    ElseIf replacement.Equals(_right) Then
        '        If GetType(IEntityFilter).IsAssignableFrom(CObj(replacer).GetType) Then
        '            Return CreateMeE(CType(_left, IEntityFilter), CType(replacer, IEntityFilter), _oper)
        '        Else
        '            Return CreateMe(_left, replacer, _oper)
        '        End If
        '    Else
        '        Dim r As IFilter = _left.ReplaceFilter(replacement, replacer)

        '        If r IsNot Nothing Then
        '            Return CreateMeE(CType(r, IEntityFilter), CType(_right, IEntityFilter), _oper)
        '        Else
        '            r = _right.ReplaceFilter(replacement, replacer)

        '            If r IsNot Nothing Then
        '                Return CreateMeE(CType(_left, IEntityFilter), CType(r, IEntityFilter), _oper)
        '            End If
        '        End If
        '    End If

        '    Return Nothing
        'End Function

        Public Function MakeHash() As String Implements IEntityFilter.MakeHash
            If IsHashable Then
                Dim l As String = Left.MakeHash
                If l <> EntityFilter.EmptyHash AndAlso _right IsNot Nothing Then
                    Dim r As String = Right.MakeHash
                    If r = EntityFilter.EmptyHash Then
                        Return r
                    Else
                        l = l & Condition2String() & r
                    End If
                End If
                Return l
            Else
                Return EntityFilter.EmptyHash
            End If

        End Function

        Public Property PrepareValue() As Boolean Implements Core.IEntityFilter.PrepareValue
            Get
                Dim b As Boolean = Left.PrepareValue
                If Right IsNot Nothing Then
                    b = b AndAlso Right.PrepareValue
                End If
                Return b
            End Get
            Set(ByVal value As Boolean)
                Left.PrepareValue = value
                If Right IsNot Nothing Then
                    Right.PrepareValue = value
                End If
            End Set
        End Property

        Public ReadOnly Property IsHashable As Boolean Implements IEntityFilterBase.IsHashable
            Get
                If Left.IsHashable Then
                    If Right IsNot Nothing Then
                        Return Right.IsHashable
                    End If

                    Return True
                End If

                Return False
            End Get
        End Property
    End Class

End Namespace