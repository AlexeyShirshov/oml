Imports System.Collections.Generic
Imports Worm.Criteria.Core
Imports Worm.Orm.Meta
Imports Worm.Orm
Imports cc = Worm.Criteria.Conditions
Imports Worm.Criteria.Values
Imports Worm.Criteria

Namespace Criteria.Conditions

    Public Enum ConditionOperator
        [And]
        [Or]
    End Enum

    Public MustInherit Class Condition
        Implements ITemplateFilter

        Public MustInherit Class ConditionConstructorBase
            Private _cond As Condition

            Public Function AddFilter(ByVal f As IFilter, ByVal [operator] As ConditionOperator) As ConditionConstructorBase
                Return AddFilter(f, [operator], True)
            End Function

            Protected Function AddFilter(ByVal f As IFilter, ByVal [operator] As ConditionOperator, ByVal useOper As Boolean) As ConditionConstructorBase
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

            Public Function AddFilter(ByVal f As IFilter) As ConditionConstructorBase
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

            Protected MustOverride Function CreateCondition(ByVal left As IFilter, ByVal right As IFilter, ByVal [operator] As ConditionOperator) As Condition
            Protected MustOverride Function CreateEntityCondition(ByVal left As IEntityFilter, ByVal right As IEntityFilter, ByVal [operator] As ConditionOperator) As Condition
        End Class

        Protected MustInherit Class ConditionTemplateBase
            Inherits TemplateBase

            Protected _con As Condition

            Public Sub New(ByVal con As Condition)
                _con = con
            End Sub

            Public Overrides Function GetStaticString() As String
                Dim sb As New StringBuilder
                sb.Append(CType(_con._left, ITemplateFilter).Template.GetStaticString)
                sb.Append(_con.Condition2String())
                If _con._right IsNot Nothing Then
                    sb.Append(CType(_con._right, ITemplateFilter).Template.GetStaticString)
                End If
                Return sb.ToString
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

        Public Function GetAllFilters() As System.Collections.Generic.ICollection(Of IFilter) Implements IFilter.GetAllFilters
            Dim res As ICollection(Of IFilter) = _left.GetAllFilters

            If _right IsNot Nothing Then
                Dim l As New List(Of IFilter)
                l.AddRange(res)
                l.AddRange(_right.GetAllFilters)
                res = l
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

        Public MustOverride Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal pname As ICreateParam) As String Implements IFilter.MakeSQLStmt
        Protected MustOverride Function CreateMe(ByVal left As IFilter, ByVal right As IFilter, ByVal [operator] As ConditionOperator) As Condition
        Public MustOverride ReadOnly Property Template() As Core.ITemplate Implements Core.ITemplateFilterBase.Template

        Private Function _ReplaceTemplate(ByVal replacement As ITemplateFilter, ByVal replacer As ITemplateFilter) As ITemplateFilter Implements ITemplateFilter.ReplaceByTemplate
            Return ReplaceCondition(replacement, replacer)
        End Function

        Public Overridable Function ReplaceCondition(ByVal replacement As ITemplateFilter, ByVal replacer As ITemplateFilter) As Condition
            If replacement.Template.Equals(CType(_left, ITemplateFilter).Template) Then
                Return CreateMe(replacer, _right, _oper)
            ElseIf replacement.Template.Equals(CType(_right, ITemplateFilter).Template) Then
                Return CreateMe(_left, replacer, _oper)
            Else
                Dim r As ITemplateFilter = CType(_left, ITemplateFilter).ReplaceByTemplate(replacement, replacer)

                If r IsNot Nothing Then
                    Return CreateMe(r, _right, _oper)
                Else
                    r = CType(_right, ITemplateFilter).ReplaceByTemplate(replacement, replacer)

                    If r IsNot Nothing Then
                        Return CreateMe(_left, r, _oper)
                    End If
                End If
            End If

            Return Nothing
        End Function

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

        Protected Function Condition2String() As String
            If _oper = ConditionOperator.And Then
                Return " and "
            Else
                Return " or "
            End If
        End Function

        Private Function Equals1(ByVal f As IFilter) As Boolean Implements IFilter.Equals
            Return Equals(TryCast(f, Condition))
        End Function

        Public Function ReplaceFilter(ByVal replacement As IFilter, ByVal replacer As IFilter) As IFilter Implements IFilter.ReplaceFilter
            If replacement.Equals(_left) Then
                Return CreateMe(replacer, _right, _oper)
            ElseIf replacement.Equals(_right) Then
                Return CreateMe(_left, replacer, _oper)
            Else
                Dim r As IFilter = _left.ReplaceFilter(replacement, replacer)

                If r IsNot Nothing Then
                    Return CreateMe(r, _right, _oper)
                Else
                    r = _right.ReplaceFilter(replacement, replacer)

                    If r IsNot Nothing Then
                        Return CreateMe(_left, r, _oper)
                    End If
                End If
            End If

            Return Nothing
        End Function

        Public Function MakeSingleStmt(ByVal schema As OrmSchemaBase, ByVal pname As ICreateParam) As Pair(Of String) Implements ITemplateFilter.MakeSingleStmt
            Throw New NotSupportedException
        End Function

        Private Function _ToString() As String Implements IFilter.ToString
            Dim r As String = String.Empty
            If _right IsNot Nothing Then
                r = _right.ToString()
            End If

            Return _left.ToString() & Condition2String() & r
        End Function

        Public Function ToStaticString() As String Implements IFilter.ToStaticString
            Dim r As String = String.Empty
            If _right IsNot Nothing Then
                r = _right.ToStaticString()
            End If

            Return _left.ToStaticString() & Condition2String() & r
        End Function

    End Class

    Public MustInherit Class EntityCondition
        Inherits Condition
        Implements IEntityFilter

        Protected MustInherit Class EntityConditionTemplateBase
            Inherits Condition.ConditionTemplateBase
            Implements IOrmFilterTemplate

            Public Sub New(ByVal con As EntityCondition)
                MyBase.New(con)
            End Sub

            Public Function MakeFilter(ByVal schema As OrmSchemaBase, ByVal oschema As IOrmObjectSchemaBase, ByVal obj As OrmBase) As IEntityFilter Implements IOrmFilterTemplate.MakeFilter
                Dim r As IEntityFilter = Nothing
                If Con._right IsNot Nothing Then
                    r = Con.Right.GetFilterTemplate.MakeFilter(schema, oschema, obj)
                End If
                Dim e As EntityCondition = CreateCon(Con.Left.GetFilterTemplate.MakeFilter(schema, oschema, obj), r, Con._oper)
                Return e
            End Function

            Public Sub SetType(ByVal t As System.Type) Implements IOrmFilterTemplate.SetType
                Con.Left.GetFilterTemplate.SetType(t)
                If Con._right IsNot Nothing Then
                    Con.Right.GetFilterTemplate.SetType(t)
                End If
            End Sub

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

            Public Function MakeHash(ByVal schema As OrmSchemaBase, ByVal oschema As IOrmObjectSchemaBase, ByVal obj As OrmBase) As String Implements IOrmFilterTemplate.MakeHash
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
            End Function

            Protected MustOverride Function CreateCon(ByVal left As IEntityFilter, ByVal right As IEntityFilter, ByVal [operator] As ConditionOperator) As EntityCondition
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

        Protected MustOverride Function CreateMeE(ByVal left As IEntityFilter, ByVal right As IEntityFilter, ByVal [operator] As ConditionOperator) As Condition

        Public Function Eval(ByVal schema As OrmSchemaBase, ByVal obj As OrmBase, ByVal oschema As IOrmObjectSchemaBase) As IEvaluableValue.EvalResult Implements IEntityFilter.Eval
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim b As IEvaluableValue.EvalResult = Left.Eval(schema, obj, oschema)
            If _right IsNot Nothing Then
                If _oper = ConditionOperator.And Then
                    If b = IEvaluableValue.EvalResult.Found Then
                        b = Right.Eval(schema, obj, oschema)
                    End If
                ElseIf _oper = ConditionOperator.Or Then
                    If b <> IEvaluableValue.EvalResult.Unknown Then
                        Dim r As IEvaluableValue.EvalResult = Right.Eval(schema, obj, oschema)
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

        Public Function GetFilterTemplate() As IOrmFilterTemplate Implements IEntityFilter.GetFilterTemplate
            Return CType(Template, IOrmFilterTemplate)
        End Function

        Public Function PrepareValue(ByVal schema As OrmSchemaBase, ByVal v As Object) As Object Implements IEntityFilter.PrepareValue
            Throw New NotSupportedException
        End Function

        Public Overrides Function ReplaceCondition(ByVal replacement As ITemplateFilter, ByVal replacer As ITemplateFilter) As Condition
            If replacement.Equals(_left) Then
                If GetType(IEntityFilter).IsAssignableFrom(CObj(replacer).GetType) Then
                    Return CreateMeE(CType(replacer, IEntityFilter), CType(_right, IEntityFilter), _oper)
                Else
                    Return CreateMe(replacer, _right, _oper)
                End If
            ElseIf replacement.Equals(_right) Then
                If GetType(IEntityFilter).IsAssignableFrom(CObj(replacer).GetType) Then
                    Return CreateMeE(CType(_left, IEntityFilter), CType(replacer, IEntityFilter), _oper)
                Else
                    Return CreateMe(_left, replacer, _oper)
                End If
            Else
                Dim r As IFilter = _left.ReplaceFilter(replacement, replacer)

                If r IsNot Nothing Then
                    Return CreateMeE(CType(r, IEntityFilter), CType(_right, IEntityFilter), _oper)
                Else
                    r = _right.ReplaceFilter(replacement, replacer)

                    If r IsNot Nothing Then
                        Return CreateMeE(CType(_left, IEntityFilter), CType(r, IEntityFilter), _oper)
                    End If
                End If
            End If

            Return Nothing
        End Function

        Public Function MakeHash() As String Implements IEntityFilter.MakeHash
            Dim l As String = Left.MakeHash
            If _right IsNot Nothing Then
                Dim r As String = Right.MakeHash
                If r = EntityFilter.EmptyHash Then
                    If _oper <> ConditionOperator.And Then
                        l = r
                    End If
                Else
                    l = l & Condition2String() & r
                End If
            End If
            Return l
        End Function
    End Class

End Namespace