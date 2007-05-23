Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports CoreFramework.Structures
Imports CoreFramework.Threading
Imports System.Collections.Generic

Namespace Orm
    Public Interface IOrmFilter
        Enum EvalResult
            Found
            NotFound
            Unknown
        End Enum

        Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal tableAliases As IDictionary(Of OrmTable, String), ByVal pname As ICreateParam) As String
        Function GetAllFilters() As ICollection(Of OrmFilter)
        Function ReplaceFilter(ByVal replacement As IOrmFilter, ByVal replacer As IOrmFilter) As IOrmFilter
        'ReadOnly Property IsEmpty() As Boolean
        Function GetStaticString() As String
        Function Eval(ByVal schema As OrmSchemaBase, ByVal obj As OrmBase) As EvalResult
    End Interface

    Public Class OrmFilter
        Implements IOrmFilter

        Private _t As Type
        Private _fieldname As String
        Private _tbl As OrmTable
        Private _value As Object
        Private _oper As FilterOperation
        Private _pname As String
        Private _param_value As Boolean
        Private _ormt As Type

        Public Sub New(ByVal t As Type, ByVal FieldName As String, ByVal value As TypeWrap(Of Object), ByVal operation As FilterOperation)
            Debug.Assert(Not GetType(OrmBase).IsAssignableFrom(value.GetType))

            _t = t
            _fieldname = FieldName
            If value IsNot Nothing Then
                _value = value.Value
            End If
            _oper = operation
            _param_value = False
        End Sub

        Public Sub New(ByVal t As Type, ByVal FieldName As String, ByVal literal As String, ByVal operation As FilterOperation)
            _t = t
            _fieldname = FieldName
            _value = literal
            _oper = operation
            _param_value = True
        End Sub

        Public Sub New(ByVal t As Type, ByVal FieldName As String, ByVal entity As OrmBase, ByVal operation As FilterOperation)
            _t = t
            _fieldname = FieldName
            If entity IsNot Nothing Then
                _value = entity.Identifier
                _ormt = entity.GetType
            Else
                _ormt = GetType(OrmBase)
            End If
            _oper = operation
            _param_value = False
        End Sub

        Public Sub New(ByVal t As Type, ByVal FieldName As String, ByVal t2 As Type, ByVal FieldName2 As String, ByVal operation As FilterOperation)
            _t = t
            _fieldname = FieldName
            _value = New Pair(Of Type, String)(t2, FieldName2)
            _oper = operation
            _param_value = False
        End Sub

        Public Sub New(ByVal table As OrmTable, ByVal column As String, ByVal value As TypeWrap(Of Object), ByVal operation As FilterOperation)
            _t = Nothing
            _tbl = table
            _fieldname = column
            If value IsNot Nothing Then
                _value = value.Value
            End If
            _oper = operation
            _param_value = False
        End Sub

        Public Sub New(ByVal table As OrmTable, ByVal column As String, ByVal literal As String, ByVal operation As FilterOperation)
            _t = Nothing
            _tbl = table
            _fieldname = column
            _value = literal
            _oper = operation
            _param_value = True
        End Sub

        Public Sub New(ByVal table As OrmTable, ByVal column As String, ByVal entity As OrmBase, ByVal operation As FilterOperation)
            _t = Nothing
            _tbl = table
            _fieldname = column
            If entity IsNot Nothing Then
                _value = entity.Identifier
            Else
                _ormt = GetType(OrmBase)
            End If
            _oper = operation
            _param_value = False
            _ormt = entity.GetType
        End Sub

        Public Sub New(ByVal table As OrmTable, ByVal column As String, ByVal t2 As Type, ByVal FieldName2 As String, ByVal operation As FilterOperation)
            _tbl = table
            _fieldname = column
            _value = New Pair(Of Type, String)(t2, FieldName2)
            _oper = operation
            _param_value = False
        End Sub

        Public Sub New(ByVal table As OrmTable, ByVal column As String, ByVal table2 As OrmTable, ByVal column2 As String, ByVal operation As FilterOperation)
            _tbl = table
            _fieldname = column
            _value = New Pair(Of OrmTable, String)(table2, column2)
            _oper = operation
            _param_value = False
        End Sub

        Public ReadOnly Property IsParamOrm() As Boolean
            Get
                Return _ormt IsNot Nothing
            End Get
        End Property

        Public ReadOnly Property IsValueType() As Boolean
            Get
                If _value Is Nothing Then Return False
                Return GetType(Pair(Of Type, String)) Is Value.GetType
            End Get
        End Property

        Public ReadOnly Property IsValueTable() As Boolean
            Get
                If _value Is Nothing Then Return False
                Return GetType(Pair(Of OrmTable, String)) Is Value.GetType
            End Get
        End Property

        Public ReadOnly Property IsValueComplex() As Boolean
            Get
                Return IsValueTable OrElse IsValueType
            End Get
        End Property

        Public ReadOnly Property IsValueLiteral() As Boolean
            Get
                Return _param_value
            End Get
        End Property

        Public ReadOnly Property Type() As Type
            Get
                Return _t
            End Get
        End Property

        Public ReadOnly Property FieldName() As String
            Get
                Return _fieldname
            End Get
        End Property

        Public ReadOnly Property Operation() As FilterOperation
            Get
                Return _oper
            End Get
        End Property

        Public ReadOnly Property Value() As Object
            Get
                Return _value
            End Get
        End Property

        Public ReadOnly Property ValueOfType() As Pair(Of Type, String)
            Get
                Return CType(Value, Pair(Of Global.System.Type, String))
            End Get
        End Property

        Public ReadOnly Property ValueOfTable() As Pair(Of OrmTable, String)
            Get
                Return CType(Value, Pair(Of OrmTable, String))
            End Get
        End Property

        Public ReadOnly Property ValueOrm(ByVal mgr As OrmManagerBase) As OrmBase
            Get
                If Not IsParamOrm Then
                    Throw New InvalidOperationException("Value is not Orm")
                End If
                Return mgr.CreateDBObject(CInt(_value), _ormt)
            End Get
        End Property

        Public Function GetTable(ByVal schema As OrmSchemaBase) As OrmTable
            Dim s As OrmTable = _tbl
            If _t IsNot Nothing Then
                s = schema.GetObjectSchema(_t).GetFieldColumnMap()(_fieldname)._tableName
            End If
            Return s
        End Function

        Public ReadOnly Property Table() As OrmTable
            Get
                Return _tbl
            End Get
        End Property

        Public Overrides Function ToString() As String
            Dim s As String = Nothing
            If _tbl IsNot Nothing Then
                s = _tbl.TableName
            ElseIf _t IsNot Nothing Then
                s = _t.Name
            Else
                Throw New InvalidOperationException("Table name must be specified")
            End If
            Dim v As String = _value.ToString
            'If Me.IsParamOrm Then
            '    v = CType(_value, OrmBase).Identifier.ToString
            'End If
            Return s & _fieldname & v & operToString
        End Function

        Protected ReadOnly Property operToString() As String
            Get
                Select Case _oper
                    Case FilterOperation.Equal
                        Return "Equal"
                    Case FilterOperation.GreaterEqualThan
                        Return "GreaterEqualThan"
                    Case FilterOperation.GreaterThan
                        Return "GreaterThan"
                    Case FilterOperation.In
                        Return "In"
                    Case FilterOperation.LessEqualThan
                        Return "LessEqualThan"
                    Case FilterOperation.NotEqual
                        Return "NotEqual"
                    Case FilterOperation.NotIn
                        Return "NotIn"
                    Case FilterOperation.Like
                        Return "Like"
                    Case FilterOperation.LessThan
                        Return "LessThan"
                    Case Else
                        Throw New DBSchemaException("Operation " & _oper & " not supported")
                End Select
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return ToString.GetHashCode()
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Dim o As OrmFilter = TryCast(obj, OrmFilter)
            Return Equals(o)
        End Function

        Public Overloads Function Equals(ByVal obj As OrmFilter) As Boolean
            If obj IsNot Nothing Then
                Return ToString.Equals(obj.ToString)
            Else
                Return False
            End If
        End Function

        Public Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal tableAliases As IDictionary(Of OrmTable, String), ByVal pname As ICreateParam) As String Implements IOrmFilter.MakeSQLStmt

            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            Dim map As MapField2Column = Nothing
            If _t IsNot Nothing Then
                map = schema.GetObjectSchema(_t).GetFieldColumnMap()(_fieldname)
            Else
                map = New MapField2Column(String.Empty, _fieldname, _tbl)
            End If

            If IsValueComplex Then
                If tableAliases Is Nothing Then
                    Throw New ArgumentNullException("tableAliases")
                End If

                Dim map2 As MapField2Column = Nothing
                If IsValueType Then
                    Dim v As Pair(Of Type, String) = CType(Value, Pair(Of Global.System.Type, String))
                    map2 = schema.GetObjectSchema(v.First).GetFieldColumnMap()(v.Second)
                Else
                    Dim v As Pair(Of OrmTable, String) = CType(Value, Pair(Of OrmTable, String))
                    map2 = New MapField2Column(String.Empty, v.Second, v.First)
                End If

                Dim [alias] As String = String.Empty

                If tableAliases IsNot Nothing Then
                    [alias] = tableAliases(map._tableName) & "."
                End If

                Dim alias2 As String = String.Empty
                If map2._tableName IsNot Nothing AndAlso tableAliases IsNot Nothing AndAlso tableAliases.ContainsKey(map2._tableName) Then
                    alias2 = tableAliases(map2._tableName) & "."
                End If

                Return [alias] & map._columnName & Oper2String() & alias2 & map2._columnName
            Else
                If IsValueLiteral Then
                    Dim [alias] As String = String.Empty

                    If tableAliases IsNot Nothing Then
                        [alias] = tableAliases(map._tableName) & "."
                    End If

                    Return [alias] & map._columnName & Oper2String() & CStr(_value)
                Else
                    If pname Is Nothing Then
                        Throw New ArgumentNullException("pname")
                    End If

                    Dim [alias] As String = String.Empty

                    If tableAliases IsNot Nothing Then
                        [alias] = tableAliases(map._tableName) & "."
                    End If

                    Dim o As Object = Value
                    If Not IsParamOrm Then
                        o = ChangeValue(schema, Value)
                    End If

                    If String.IsNullOrEmpty(_pname) OrElse Not pname.NamedParams Then
                        _pname = pname.CreateParam(o)
                    Else
                        _pname = pname.AddParam(_pname, o)
                    End If

                    Return [alias] & map._columnName & Oper2String() & _pname
                End If
            End If
        End Function

        Protected Function ChangeValue(ByVal schema As OrmSchemaBase, ByVal v As Object) As Object
            If _t IsNot Nothing Then
                Return schema.ChangeValueType(_t, schema.GetColumnByFieldName(_t, _fieldname), v)
            End If
            Return v
        End Function

        Public Function MakeSignleStmt(ByVal schema As DbSchema, ByVal pname As ICreateParam) As Pair(Of String)

            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If pname Is Nothing Then
                Throw New ArgumentNullException("pname")
            End If

            Dim prname As String = String.Empty
            If IsValueLiteral Then
                prname = CStr(_value)
            Else
                Dim o As Object = Value
                If Not IsParamOrm Then
                    o = ChangeValue(schema, Value)
                End If

                If String.IsNullOrEmpty(_pname) OrElse Not pname.NamedParams Then
                    prname = pname.CreateParam(o)
                Else
                    prname = _pname
                End If
            End If

            If _t IsNot Nothing Then
                Dim map As MapField2Column = schema.GetObjectSchema(_t).GetFieldColumnMap()(_fieldname)
                If _value Is DBNull.Value Then
                    If schema.GetFieldTypeByName(_t, _fieldname) Is GetType(Byte()) Then
                        pname.GetParameter(prname).DbType = System.Data.DbType.Binary
                    ElseIf schema.GetFieldTypeByName(_t, _fieldname) Is GetType(Decimal) Then
                        pname.GetParameter(prname).DbType = System.Data.DbType.Decimal
                    End If
                End If
                Return New Pair(Of String)(map._columnName, prname)
            Else
                Return New Pair(Of String)(_fieldname, prname)
            End If
        End Function

        Private Function Oper2String() As String
            Select Case _oper
                Case FilterOperation.Equal
                    Return " = "
                Case FilterOperation.GreaterEqualThan
                    Return " >= "
                Case FilterOperation.GreaterThan
                    Return " > "
                Case FilterOperation.In
                    Return " in "
                Case FilterOperation.NotEqual
                    Return " <> "
                Case FilterOperation.NotIn
                    Return " not in "
                Case FilterOperation.LessEqualThan
                    Return " <= "
                Case FilterOperation.Like
                    Return " like "
                Case FilterOperation.LessThan
                    Return " < "
                Case Else
                    Throw New DBSchemaException("invalid opration " & _oper.ToString)
            End Select
        End Function

        Public Function GetAllFilters() As System.Collections.Generic.ICollection(Of OrmFilter) Implements IOrmFilter.GetAllFilters
            Dim l As New List(Of OrmFilter)
            l.Add(Me)
            Return l
        End Function

        Public Function ReplaceFilter(ByVal replacement As IOrmFilter, ByVal replacer As IOrmFilter) As IOrmFilter Implements IOrmFilter.ReplaceFilter
            If Not Equals(replacement) Then
                Return Nothing 'Throw New ArgumentException("invalid filter", "replacement")
            End If
            Return replacer
        End Function

        Public Function GetStaticString() As String Implements IOrmFilter.GetStaticString
            Dim tbl As String = String.Empty
            If _t IsNot Nothing Then
                tbl = _t.Name
            ElseIf _tbl IsNot Nothing Then
                tbl = _tbl.TableName
            Else
                Throw New InvalidOperationException("Table name must be specified")
            End If
            Return tbl & _fieldname & Oper2String()
        End Function

        Public Function Eval(ByVal schema As OrmSchemaBase, ByVal o As OrmBase) As IOrmFilter.EvalResult Implements IOrmFilter.Eval
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If o Is Nothing Then
                Throw New ArgumentNullException("o")
            End If

            Dim r As IOrmFilter.EvalResult = IOrmFilter.EvalResult.NotFound

            If _t IsNot Nothing AndAlso Not (IsValueLiteral Or IsValueComplex) Then
                Dim t As Type = o.GetType
                If _t Is t Then
                    Dim v As Object = schema.GetFieldValue(o, _fieldname)
                    If v IsNot Nothing Then
                        Dim tt As Type = v.GetType
                        Dim orm As OrmBase = TryCast(v, OrmBase)
                        Dim b As Boolean = orm IsNot Nothing
                        If b Then
                            If Not IsParamOrm Then
                                Return IOrmFilter.EvalResult.NotFound
                            End If
                            If tt IsNot _ormt Then
                                Return IOrmFilter.EvalResult.NotFound
                            End If
                        End If

                        Select Case _oper
                            Case FilterOperation.Equal
                                If Equals(v, _value) Then
                                    r = IOrmFilter.EvalResult.Found
                                End If
                            Case FilterOperation.GreaterEqualThan
                                Dim i As Integer = CType(v, IComparable).CompareTo(_value)
                                If i >= 0 Then
                                    r = IOrmFilter.EvalResult.Found
                                End If
                            Case FilterOperation.GreaterThan
                                Dim i As Integer = CType(v, IComparable).CompareTo(_value)
                                If i > 0 Then
                                    r = IOrmFilter.EvalResult.Found
                                End If
                            Case FilterOperation.LessEqualThan
                                Dim i As Integer = CType(v, IComparable).CompareTo(_value)
                                If i <= 0 Then
                                    r = IOrmFilter.EvalResult.Found
                                End If
                            Case FilterOperation.LessThan
                                Dim i As Integer = CType(v, IComparable).CompareTo(_value)
                                If i < 0 Then
                                    r = IOrmFilter.EvalResult.Found
                                End If
                            Case FilterOperation.NotEqual
                                If Not Equals(v, _value) Then
                                    r = IOrmFilter.EvalResult.Found
                                End If
                            Case Else
                                r = IOrmFilter.EvalResult.Unknown
                        End Select
                    Else
                        If _value Is Nothing Then
                            r = IOrmFilter.EvalResult.Found
                        End If
                    End If
                End If
            End If
            Return r
        End Function

        Public Shared Sub ChangeValueToLiteral(ByRef join As OrmJoin, ByVal schema As OrmSchemaBase, ByVal t As Type, ByVal field As String, ByVal table As OrmTable, ByVal literal As String)
            Dim f As IOrmFilter = Nothing
            For Each fl As OrmFilter In join.Condition.GetAllFilters()
                If fl.IsValueComplex AndAlso fl.Type Is t AndAlso fl.FieldName = field Then
                    If fl.IsValueTable Then
                        f = New OrmFilter(table, literal, fl.ValueOfTable.First, fl.ValueOfTable.Second, fl.Operation)
                    Else
                        f = New OrmFilter(table, literal, fl.ValueOfType.First, fl.ValueOfType.Second, fl.Operation)
                    End If
                End If
                If f Is Nothing Then
                    If fl.IsValueType AndAlso fl.ValueOfType.First Is t AndAlso fl.ValueOfType.Second = field Then
                        If fl.Table Is Nothing Then
                            f = New OrmFilter(table, literal, fl.Type, fl.FieldName, fl.Operation)
                        Else
                            f = New OrmFilter(table, literal, fl.Table, fl.FieldName, fl.Operation)
                        End If
                    End If
                End If

                If f IsNot Nothing Then
                    join.ReplaceFilter(fl, f)
                End If
            Next
        End Sub

        Public Shared Function ChangeValueToLiteral(ByVal source As IOrmFilter, ByVal schema As OrmSchemaBase, ByVal t As Type, ByVal field As String, ByVal literal As String) As IOrmFilter
            Dim f As IOrmFilter = Nothing
            For Each fl As OrmFilter In source.GetAllFilters()
                If fl.IsValueComplex AndAlso fl.Type Is t AndAlso fl.FieldName = field Then
                    If fl.IsValueTable Then
                        f = New OrmFilter(fl.ValueOfTable.First, fl.ValueOfTable.Second, literal, fl.Operation)
                    Else
                        f = New OrmFilter(fl.ValueOfType.First, fl.ValueOfType.Second, literal, fl.Operation)
                    End If
                End If
                If f Is Nothing Then
                    If fl.IsValueType AndAlso fl.ValueOfType.First Is t AndAlso fl.ValueOfType.Second = field Then
                        If fl.Table Is Nothing Then
                            f = New OrmFilter(fl.Type, fl.FieldName, literal, fl.Operation)
                        Else
                            f = New OrmFilter(fl.Table, fl.FieldName, literal, fl.Operation)
                        End If
                    End If
                End If

                If f IsNot Nothing Then
                    Return source.ReplaceFilter(fl, f)
                End If
            Next
            Return Nothing
        End Function

        Public Shared Function ChangeValueToParam(ByVal source As IOrmFilter, ByVal schema As OrmSchemaBase, ByVal t As Type, ByVal field As String, ByVal value As Object) As IOrmFilter
            Dim f As IOrmFilter = Nothing
            For Each fl As OrmFilter In source.GetAllFilters()
                If fl.IsValueComplex AndAlso fl.Type Is t AndAlso fl.FieldName = field Then
                    If fl.IsValueTable Then
                        f = New OrmFilter(fl.ValueOfTable.First, fl.ValueOfTable.Second, New TypeWrap(Of Object)(value), fl.Operation)
                    Else
                        f = New OrmFilter(fl.ValueOfType.First, fl.ValueOfType.Second, New TypeWrap(Of Object)(value), fl.Operation)
                    End If
                End If
                If f Is Nothing Then
                    If fl.IsValueType AndAlso fl.ValueOfType.First Is t AndAlso fl.ValueOfType.Second = field Then
                        If fl.Table Is Nothing Then
                            f = New OrmFilter(fl.Type, fl.FieldName, New TypeWrap(Of Object)(value), fl.Operation)
                        Else
                            f = New OrmFilter(fl.Table, fl.FieldName, New TypeWrap(Of Object)(value), fl.Operation)
                        End If
                    End If
                End If

                If f IsNot Nothing Then
                    Return source.ReplaceFilter(fl, f)
                End If
            Next
            Return Nothing
        End Function
    End Class ' OrmFilter

    Public Class OrmCondition
        Implements IOrmFilter

        Private _left As IOrmFilter
        Private _right As IOrmFilter
        Private _oper As ConditionOperator

        Public Sub New(ByVal left As IOrmFilter, ByVal right As IOrmFilter, ByVal [operator] As ConditionOperator)
            _left = left
            _right = right
            _oper = [operator]
        End Sub

        Public Overrides Function ToString() As String

            Dim r As String = String.Empty
            If _right IsNot Nothing Then
                r = CObj(_right).ToString()
            End If

            Return CObj(_left).ToString() & Condition2String() & r
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return ToString.GetHashCode
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            If obj.GetType IsNot GetType(OrmCondition) Then Return False
            Return Equals(CType(obj, OrmCondition))
        End Function

        Public Overloads Function Equals(ByVal con As OrmCondition) As Boolean
            If con Is Nothing Then
                Return False
            End If

            Dim r As Boolean = True

            If _right IsNot Nothing Then
                r = CObj(_right).Equals(con._right)
            End If

            Return CObj(_left).Equals(con._left) AndAlso r AndAlso _oper = con._oper
        End Function

        Public Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal tableAliases As IDictionary(Of OrmTable, String), ByVal pname As ICreateParam) As String Implements IOrmFilter.MakeSQLStmt

            If _right Is Nothing Then
                Return _left.MakeSQLStmt(schema, tableAliases, pname)
            End If

            Return "(" & _left.MakeSQLStmt(schema, tableAliases, pname) & Condition2String() & _right.MakeSQLStmt(schema, tableAliases, pname) & ")"
        End Function

        Private Function Condition2String() As String
            If _oper = ConditionOperator.And Then
                Return " and "
            Else
                Return " or "
            End If
        End Function

        'Public ReadOnly Property Left() As IOrmFilter
        '    Get
        '        Return _left
        '    End Get
        'End Property

        'Public ReadOnly Property Right() As IOrmFilter
        '    Get
        '        Return _right
        '    End Get
        'End Property

        Public Function GetAllFilters() As System.Collections.Generic.ICollection(Of OrmFilter) Implements IOrmFilter.GetAllFilters

            Dim l As New List(Of OrmFilter)
            l.AddRange(_left.GetAllFilters)

            If _right IsNot Nothing Then
                l.AddRange(_right.GetAllFilters)
            End If

            Return l.ToArray
        End Function

        Private Function _ReplaceFilter(ByVal replacement As IOrmFilter, ByVal replacer As IOrmFilter) As IOrmFilter Implements IOrmFilter.ReplaceFilter
            Return ReplaceFilter(replacement, replacer)
        End Function

        Public Function ReplaceFilter(ByVal replacement As IOrmFilter, ByVal replacer As IOrmFilter) As OrmCondition

            If replacement.GetStaticString.Equals(_left.GetStaticString) Then
                Return New OrmCondition(replacer, _right, _oper)
            ElseIf replacement.GetStaticString.Equals(_right.GetStaticString) Then
                Return New OrmCondition(_left, replacer, _oper)
            Else
                Dim r As IOrmFilter = _left.ReplaceFilter(replacement, replacer)

                If r IsNot Nothing Then
                    Return New OrmCondition(r, _right, _oper)
                Else
                    r = _right.ReplaceFilter(replacement, replacer)

                    If r IsNot Nothing Then
                        Return New OrmCondition(_left, r, _oper)
                    End If
                End If
            End If

            Return Nothing
        End Function

        Public Class OrmConditionConstructor
            Private _cond As OrmCondition

            Public Function AddFilter(ByVal f As IOrmFilter, ByVal [operator] As ConditionOperator) As OrmConditionConstructor
                If _cond Is Nothing AndAlso f IsNot Nothing Then
                    _cond = New OrmCondition(f, Nothing, [operator])
                ElseIf _cond IsNot Nothing AndAlso _cond._right Is Nothing AndAlso f IsNot Nothing Then
                    _cond._right = f
                    _cond._oper = [operator]
                ElseIf f IsNot Nothing Then
                    _cond = New OrmCondition(_cond, f, [operator])
                End If

                Return Me
            End Function

            Public Function AddFilter(ByVal f As IOrmFilter) As OrmConditionConstructor
                If _cond Is Nothing AndAlso f IsNot Nothing Then
                    _cond = New OrmCondition(f, Nothing, ConditionOperator.And)
                ElseIf _cond IsNot Nothing AndAlso _cond._right Is Nothing AndAlso f IsNot Nothing Then
                    _cond._right = f
                ElseIf f IsNot Nothing Then
                    _cond = New OrmCondition(_cond, f, ConditionOperator.And)
                End If

                Return Me
            End Function

            Public ReadOnly Property Condition() As IOrmFilter
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

        Public Function GetStaticString() As String Implements IOrmFilter.GetStaticString
            Dim r As String = String.Empty
            If _right IsNot Nothing Then
                r = _right.GetStaticString
            End If

            Return _left.GetStaticString & Condition2String() & r
        End Function

        Public Function Eval(ByVal schema As OrmSchemaBase, ByVal obj As OrmBase) As IOrmFilter.EvalResult Implements IOrmFilter.Eval
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim b As IOrmFilter.EvalResult = _left.Eval(schema, obj)
            If _right IsNot Nothing Then
                If _oper = ConditionOperator.And Then
                    If b = IOrmFilter.EvalResult.Found Then
                        b = _right.Eval(schema, obj)
                    End If
                ElseIf _oper = ConditionOperator.Or Then
                    If b <> IOrmFilter.EvalResult.Unknown Then
                        Dim r As IOrmFilter.EvalResult = _right.Eval(schema, obj)
                        If r <> IOrmFilter.EvalResult.Unknown Then
                            If b <> IOrmFilter.EvalResult.Found Then
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

    End Class ' OrmCondition

    Public Structure OrmJoin
        Private _table As OrmTable
        Private _joinType As JoinType
        Private _condition As IOrmFilter

        Public Sub New(ByVal Table As OrmTable, ByVal JoinType As JoinType, ByVal condition As IOrmFilter)
            _table = Table
            _joinType = JoinType
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

        Public ReadOnly Property Condition() As IOrmFilter
            Get
                Return _condition
            End Get
        End Property

        Public Sub ReplaceFilter(ByVal replacement As IOrmFilter, ByVal replacer As IOrmFilter)
            _condition = _condition.ReplaceFilter(replacement, replacer)
        End Sub

        Public Function GetStaticString() As String
            Return _table.TableName & JoinTypeString() & _condition.GetStaticString
        End Function

        Public Overrides Function ToString() As String
            Return _table.TableName & JoinTypeString() & CObj(_condition).ToString
        End Function

        Public ReadOnly Property Table() As OrmTable
            Get
                Return _table
            End Get
        End Property
    End Structure

End Namespace