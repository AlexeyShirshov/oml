Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports CoreFramework.Structures
Imports CoreFramework.Threading
Imports System.Collections.Generic

Namespace Orm
    Public Class JoinFilter
        Implements IFilter

        Friend _e1 As Pair(Of Type, String)
        Friend _t1 As Pair(Of OrmTable, String)

        Friend _e2 As Pair(Of Type, String)
        Friend _t2 As Pair(Of OrmTable, String)

        Friend _oper As FilterOperation

        Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal t2 As Type, ByVal fieldName2 As String, ByVal operation As FilterOperation)
            _e1 = New Pair(Of Type, String)(t, fieldName)
            _e2 = New Pair(Of Type, String)(t2, fieldName2)
            _oper = operation
        End Sub

        Public Sub New(ByVal table As OrmTable, ByVal column As String, ByVal t2 As Type, ByVal FieldName2 As String, ByVal operation As FilterOperation)
            _t1 = New Pair(Of OrmTable, String)(table, column)
            _e2 = New Pair(Of Type, String)(t2, FieldName2)
            _oper = operation
        End Sub

        Public Sub New(ByVal table As OrmTable, ByVal column As String, ByVal table2 As OrmTable, ByVal column2 As String, ByVal operation As FilterOperation)
            _t1 = New Pair(Of OrmTable, String)(table, column)
            _t2 = New Pair(Of OrmTable, String)(table2, column2)
            _oper = operation
        End Sub

        'Public Function GetStaticString() As String Implements IFilter.GetStaticString
        '    Throw New NotSupportedException
        'End Function

        Public Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal tableAliases As System.Collections.Generic.IDictionary(Of OrmTable, String), ByVal pname As ICreateParam) As String Implements IFilter.MakeSQLStmt
            Dim map As MapField2Column = Nothing
            If _e1 IsNot Nothing Then
                map = schema.GetObjectSchema(_e1.First).GetFieldColumnMap(_e1.Second)
            Else
                map = New MapField2Column(Nothing, _t1.Second, _t1.First)
            End If

            Dim map2 As MapField2Column = Nothing
            If _e2 IsNot Nothing Then
                map2 = schema.GetObjectSchema(_e2.First).GetFieldColumnMap(_e2.Second)
            Else
                map2 = New MapField2Column(Nothing, _t2.Second, _t2.First)
            End If

            Dim [alias] As String = String.Empty

            If tableAliases IsNot Nothing Then
                [alias] = tableAliases(map._tableName) & "."
            End If

            Dim alias2 As String = String.Empty
            If map2._tableName IsNot Nothing AndAlso tableAliases IsNot Nothing AndAlso tableAliases.ContainsKey(map2._tableName) Then
                alias2 = tableAliases(map2._tableName) & "."
            End If

            Return [alias] & map._columnName & TemplateBase.Oper2String(_oper) & alias2 & map2._columnName
        End Function

        Public Function ReplaceFilter(ByVal replacement As IFilter, ByVal replacer As IFilter) As IFilter Implements IFilter.ReplaceFilter
            If Not Equals(replacement) Then
                Return Nothing
            End If
            Return replacer
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, JoinFilter))
        End Function

        Public Overloads Function Equals(ByVal obj As JoinFilter) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            'Return ( _
            '    (Equals(_e1, obj._e1) AndAlso Equals(_e2, obj._e2)) OrElse (Equals(_e1, obj._e2) AndAlso Equals(_e2, obj._e1)) _
            '    ) OrElse ( _
            '    (Equals(_t1, obj._t1) AndAlso Equals(_t2, obj._t2)) OrElse (Equals(_t1, obj._t2) AndAlso Equals(_t2, obj._t1)) _
            '    )
            Dim v1 As Object = _e1
            Dim ve1 As Object = obj._e1
            If _e1 Is Nothing Then
                v1 = _t1
                ve1 = obj._t1
            End If

            Dim v2 As Object = _e2
            Dim ve2 As Object = obj._e2
            If v2 Is Nothing Then
                v2 = _t2
                ve2 = obj._e2
            End If

            Dim b As Boolean = (Equals(v1, ve1) AndAlso Equals(v2, ve2)) _
                OrElse (Equals(v1, ve2) AndAlso Equals(v2, ve1))

            Return b
        End Function

        Protected Shared Function ChangeEntityJoinToValue(ByVal source As IFilter, ByVal t As Type, ByVal field As String, ByVal value As IFilterValue) As IFilter
            For Each _fl As IFilter In source.GetAllFilters()
                Dim fl As JoinFilter = TryCast(_fl, JoinFilter)
                If fl IsNot Nothing Then
                    Dim f As IFilter = Nothing
                    If fl._e1 IsNot Nothing AndAlso fl._e1.First Is t AndAlso fl._e1.Second = field Then
                        If fl._e2 IsNot Nothing Then
                            f = New EntityFilter(fl._e2.First, fl._e2.Second, value, fl._oper)
                        Else
                            f = New TableFilter(fl._t2.First, fl._t2.Second, value, fl._oper)
                        End If
                    ElseIf fl._e2 IsNot Nothing AndAlso fl._e2.First Is t AndAlso fl._e2.Second = field Then
                        If fl._e1 IsNot Nothing Then
                            f = New EntityFilter(fl._e1.First, fl._e1.Second, value, fl._oper)
                        Else
                            f = New TableFilter(fl._t1.First, fl._t1.Second, value, fl._oper)
                        End If
                    End If

                    If f IsNot Nothing Then
                        Return source.ReplaceFilter(fl, f)
                    End If
                End If
            Next
            Return Nothing
        End Function

        Public Shared Function ChangeEntityJoinToLiteral(ByVal source As IFilter, ByVal t As Type, ByVal field As String, ByVal literal As String) As IFilter
            Return ChangeEntityJoinToValue(source, t, field, New LiteralValue(literal))
        End Function

        Public Shared Function ChangeEntityJoinToParam(ByVal source As IFilter, ByVal t As Type, ByVal field As String, ByVal value As TypeWrap(Of Object)) As IFilter
            Return ChangeEntityJoinToValue(source, t, field, New ScalarValue(value.Value))
        End Function

        Public Function GetAllFilters() As System.Collections.Generic.ICollection(Of IFilter) Implements IFilter.GetAllFilters
            Return New JoinFilter() {Me}
        End Function

        Private Function Equals1(ByVal f As IFilter) As Boolean Implements IFilter.Equals
            Equals(TryCast(f, JoinFilter))
        End Function

        Private Function _ToString() As String Implements IFilter.ToString
            Dim sb As New StringBuilder
            If _e1 IsNot Nothing Then
                sb.Append(_e1.First.ToString).Append(_e1.Second).Append(" - ")
            Else
                sb.Append(_t1.First.TableName).Append(_t1.Second).Append(" - ")
            End If
            If _e2 IsNot Nothing Then
                sb.Append(_e2.First.ToString).Append(_e2.Second).Append(" - ")
            Else
                sb.Append(_t2.First.TableName).Append(_t2.Second).Append(" - ")
            End If
            Return sb.ToString
        End Function

        Public Overrides Function ToString() As String
            Return _ToString()
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _ToString.GetHashCode
        End Function
    End Class

    Public Class Condition
        Implements ITemplateFilter

        Public Class ConditionConstructor
            Private _cond As Condition

            Public Function AddFilter(ByVal f As IFilter, ByVal [operator] As ConditionOperator) As ConditionConstructor
                Return AddFilter(f, [operator], True)
            End Function

            Protected Function AddFilter(ByVal f As IFilter, ByVal [operator] As ConditionOperator, ByVal useOper As Boolean) As ConditionConstructor
                If _cond Is Nothing AndAlso f IsNot Nothing Then
                    If GetType(IEntityFilter).IsAssignableFrom(CObj(f).GetType) Then
                        _cond = New EntityCondition(CType(f, IEntityFilter), Nothing, [operator])
                    Else
                        _cond = New Condition(f, Nothing, [operator])
                    End If
                ElseIf _cond IsNot Nothing AndAlso _cond._right Is Nothing AndAlso f IsNot Nothing Then
                    If Not GetType(IEntityFilter).IsAssignableFrom(CObj(f).GetType) AndAlso TypeOf (_cond) Is EntityCondition Then
                        _cond = New Condition(_cond, f, [operator])
                    Else
                        _cond._right = f
                        If useOper Then
                            _cond._oper = [operator]
                        End If
                    End If
                ElseIf f IsNot Nothing Then
                    If GetType(IEntityFilter).IsAssignableFrom(CObj(f).GetType) AndAlso TypeOf (_cond) Is EntityCondition Then
                        _cond = New EntityCondition(CType(_cond, IEntityFilter), CType(f, IEntityFilter), [operator])
                    Else
                        _cond = New Condition(_cond, f, [operator])
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
        End Class

        Private Class ConditionTemplate
            Inherits TemplateBase

            Private _con As Condition

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

        Public Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal tableAliases As IDictionary(Of OrmTable, String), ByVal pname As ICreateParam) As String Implements IFilter.MakeSQLStmt
            If _right Is Nothing Then
                Return _left.MakeSQLStmt(schema, tableAliases, pname)
            End If

            Return "(" & _left.MakeSQLStmt(schema, tableAliases, pname) & Condition2String() & _right.MakeSQLStmt(schema, tableAliases, pname) & ")"
        End Function

        Private Function _ReplaceTemplate(ByVal replacement As ITemplateFilter, ByVal replacer As ITemplateFilter) As ITemplateFilter Implements ITemplateFilter.ReplaceByTemplate
            Return ReplaceCondition(replacement, replacer)
        End Function

        Public Overridable Function ReplaceCondition(ByVal replacement As ITemplateFilter, ByVal replacer As ITemplateFilter) As Condition
            If replacement.Template.Equals(CType(_left, ITemplateFilter).Template) Then
                Return New Condition(replacer, _right, _oper)
            ElseIf replacement.Template.Equals(CType(_right, ITemplateFilter).Template) Then
                Return New Condition(_left, replacer, _oper)
            Else
                Dim r As ITemplateFilter = CType(_left, ITemplateFilter).ReplaceByTemplate(replacement, replacer)

                If r IsNot Nothing Then
                    Return New Condition(r, _right, _oper)
                Else
                    r = CType(_right, ITemplateFilter).ReplaceByTemplate(replacement, replacer)

                    If r IsNot Nothing Then
                        Return New Condition(_left, r, _oper)
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

        Public Overridable ReadOnly Property Template() As TemplateBase Implements ITemplateFilter.Template
            Get
                Return New ConditionTemplate(Me)
            End Get
        End Property

        Public Function ReplaceFilter(ByVal replacement As IFilter, ByVal replacer As IFilter) As IFilter Implements IFilter.ReplaceFilter
            If replacement.Equals(_left) Then
                Return New Condition(replacer, _right, _oper)
            ElseIf replacement.Equals(_right) Then
                Return New Condition(_left, replacer, _oper)
            Else
                Dim r As IFilter = _left.ReplaceFilter(replacement, replacer)

                If r IsNot Nothing Then
                    Return New Condition(r, _right, _oper)
                Else
                    r = _right.ReplaceFilter(replacement, replacer)

                    If r IsNot Nothing Then
                        Return New Condition(_left, r, _oper)
                    End If
                End If
            End If

            Return Nothing
        End Function

        Public Function MakeSingleStmt(ByVal schema As DbSchema, ByVal pname As ICreateParam) As Pair(Of String) Implements ITemplateFilter.MakeSingleStmt
            Throw New NotSupportedException
        End Function

        Private Function _ToString() As String Implements IFilter.ToString
            Dim r As String = String.Empty
            If _right IsNot Nothing Then
                r = _right.ToString()
            End If

            Return _left.ToString() & Condition2String() & r
        End Function
    End Class

    Friend Class EntityCondition
        Inherits Condition
        Implements IEntityFilter

        Private Class ConditionTemplate
            Inherits TemplateBase
            Implements IOrmFilterTemplate

            Private _con As EntityCondition

            Public Sub New(ByVal con As EntityCondition)
                _con = con
            End Sub

            Public Function MakeFilter(ByVal schema As OrmSchemaBase, ByVal oschema As IOrmObjectSchemaBase, ByVal obj As OrmBase) As IEntityFilter Implements IOrmFilterTemplate.MakeFilter
                Dim r As IEntityFilter = Nothing
                If _con._right IsNot Nothing Then
                    r = _con.Right.GetFilterTemplate.MakeFilter(schema, oschema, obj)
                End If
                Dim e As New EntityCondition(_con.Left.GetFilterTemplate.MakeFilter(schema, oschema, obj), r, _con._oper)
                Return e
            End Function

            Public Sub SetType(ByVal t As System.Type) Implements IOrmFilterTemplate.SetType
                _con.Left.GetFilterTemplate.SetType(t)
                If _con._right IsNot Nothing Then
                    _con.Right.GetFilterTemplate.SetType(t)
                End If
            End Sub

            Public Overrides Function GetStaticString() As String
                Dim s As New StringBuilder
                s.Append(_con.Left.Template.GetStaticString)
                s.Append(_con.Condition2String())
                If _con._right IsNot Nothing Then
                    s.Append(_con.Right.Template.GetStaticString)
                End If
                Return s.ToString
            End Function

            Public Function MakeHash(ByVal schema As OrmSchemaBase, ByVal oschema As IOrmObjectSchemaBase, ByVal obj As OrmBase) As String Implements IOrmFilterTemplate.MakeHash
                Dim l As String = _con.Left.GetFilterTemplate.MakeHash(schema, oschema, obj)
                If _con._right IsNot Nothing Then
                    Dim r As String = _con.Right.GetFilterTemplate.MakeHash(schema, oschema, obj)
                    If r = EntityFilter.EmptyHash Then
                        If _con._oper <> ConditionOperator.And Then
                            l = r
                        End If
                    Else
                        l = l & _con.Condition2String() & r
                    End If
                End If
                Return l
            End Function
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

        Public Function Eval(ByVal schema As OrmSchemaBase, ByVal obj As OrmBase, ByVal oschema As IOrmObjectSchemaBase) As IEntityFilter.EvalResult Implements IEntityFilter.Eval
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim b As IEntityFilter.EvalResult = Left.Eval(schema, obj, oschema)
            If _right IsNot Nothing Then
                If _oper = ConditionOperator.And Then
                    If b = IEntityFilter.EvalResult.Found Then
                        b = Right.Eval(schema, obj, oschema)
                    End If
                ElseIf _oper = ConditionOperator.Or Then
                    If b <> IEntityFilter.EvalResult.Unknown Then
                        Dim r As IEntityFilter.EvalResult = Right.Eval(schema, obj, oschema)
                        If r <> IEntityFilter.EvalResult.Unknown Then
                            If b <> IEntityFilter.EvalResult.Found Then
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
                    Return New EntityCondition(CType(replacer, IEntityFilter), CType(_right, IEntityFilter), _oper)
                Else
                    Return New Condition(replacer, _right, _oper)
                End If
            ElseIf replacement.Equals(_right) Then
                If GetType(IEntityFilter).IsAssignableFrom(CObj(replacer).GetType) Then
                    Return New EntityCondition(CType(_left, IEntityFilter), CType(replacer, IEntityFilter), _oper)
                Else
                    Return New Condition(_left, replacer, _oper)
                End If
            Else
                Dim r As IFilter = _left.ReplaceFilter(replacement, replacer)

                If r IsNot Nothing Then
                    Return New EntityCondition(CType(r, IEntityFilter), CType(_right, IEntityFilter), _oper)
                Else
                    r = _right.ReplaceFilter(replacement, replacer)

                    If r IsNot Nothing Then
                        Return New EntityCondition(CType(_left, IEntityFilter), CType(r, IEntityFilter), _oper)
                    End If
                End If
            End If

            Return Nothing
        End Function

        Public Overrides ReadOnly Property Template() As TemplateBase
            Get
                Return New ConditionTemplate(Me)
            End Get
        End Property

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

    Public Structure OrmJoin
        Private _table As OrmTable
        Private _joinType As JoinType
        Private _condition As IFilter

        Public Sub New(ByVal Table As OrmTable, ByVal joinType As JoinType, ByVal condition As IFilter)
            _table = Table
            _joinType = joinType
            _condition = condition
        End Sub

        Public Function MakeSQLStmt(ByVal schema As DbSchema, ByVal tableAliases As IDictionary(Of OrmTable, String), ByVal pname As ICreateParam) As String
            If IsEmpty Then
                Throw New InvalidOperationException("Object must be created")
            End If

            If _condition Is Nothing Then
                Throw New InvalidOperationException("Join condition must be specified")
            End If

            If tableAliases Is Nothing Then
                Throw New ArgumentNullException("tableAliases")
            End If

            'Dim table As String = _table
            'Dim sch as IOrmObjectSchema = schema.GetObjectSchema(
            Return JoinTypeString() & _table.TableName & " " & tableAliases(_table) & " on " & _condition.MakeSQLStmt(schema, tableAliases, pname)
        End Function

        Public ReadOnly Property IsEmpty() As Boolean
            Get
                Return _table Is Nothing
            End Get
        End Property

        Private Function JoinTypeString() As String
            Select Case _joinType
                Case JoinType.Join
                    Return " join "
                Case JoinType.LeftOuterJoin
                    Return " left join "
                Case JoinType.RightOuterJoin
                    Return " right join "
                Case JoinType.FullJoin
                    Return " full join "
                Case JoinType.CrossJoin
                    Return " cross join "
                Case Else
                    Throw New DBSchemaException("invalid join type " & _joinType.ToString)
            End Select
        End Function

        Public ReadOnly Property Condition() As IFilter
            Get
                Return _condition
            End Get
        End Property

        Public Sub ReplaceFilter(ByVal replacement As IFilter, ByVal replacer As IFilter)
            _condition = _condition.ReplaceFilter(replacement, replacer)
        End Sub

        'Public Function GetStaticString() As String
        '    Return _table.TableName & JoinTypeString() & _condition.GetStaticString
        'End Function

        Public Overrides Function ToString() As String
            Return _table.TableName & JoinTypeString() & _condition.ToString
        End Function

        Public Property Table() As OrmTable
            Get
                Return _table
            End Get
            Friend Set(ByVal value As OrmTable)
                _table = value
            End Set
        End Property

        Public Function InjectJoinFilter(ByVal t As Type, ByVal field As String, ByVal table As OrmTable, ByVal column As String) As TemplateBase
            For Each _fl As IFilter In _condition.GetAllFilters()
                Dim f As JoinFilter = Nothing
                Dim fl As JoinFilter = TryCast(_fl, JoinFilter)
                Dim tm As TemplateBase = Nothing
                If fl._e1 IsNot Nothing AndAlso fl._e1.First Is t AndAlso fl._e1.Second = field Then
                    If fl._e2 IsNot Nothing Then
                        f = New JoinFilter(table, column, fl._e2.First, fl._e2.Second, fl._oper)
                        tm = New OrmFilterTemplate(fl._e2.First, fl._e2.Second, fl._oper)
                    Else
                        f = New JoinFilter(table, column, fl._t2.First, fl._t2.Second, fl._oper)
                        tm = New TableFilterTemplate(fl._t2.First, fl._t2.Second, fl._oper)
                    End If
                End If
                If f Is Nothing Then
                    If fl._e2 IsNot Nothing AndAlso fl._e2.First Is t AndAlso fl._e2.Second = field Then
                        If fl._e1 IsNot Nothing Then
                            f = New JoinFilter(table, column, fl._e1.First, fl._e1.Second, fl._oper)
                            tm = New OrmFilterTemplate(fl._e1.First, fl._e1.Second, fl._oper)
                        Else
                            f = New JoinFilter(table, column, fl._t1.First, fl._t1.Second, fl._oper)
                            tm = New TableFilterTemplate(fl._t1.First, fl._t1.Second, fl._oper)
                        End If
                    End If
                End If

                If f IsNot Nothing Then
                    ReplaceFilter(fl, f)
                    Return tm
                End If
            Next
            Return Nothing
        End Function

    End Structure
End Namespace
