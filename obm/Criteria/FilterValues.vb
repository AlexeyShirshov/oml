Imports System.Collections.Generic
Imports Worm.Orm.Meta
Imports Worm.Criteria.Core
Imports Worm.Orm

Namespace Criteria.Values

    Public Interface INonTemplateValue
        Function GetStaticString() As String
    End Interface

    Public Interface IFilterValue
        Function _ToString() As String
    End Interface

    Public Interface IParamFilterValue
        Inherits IFilterValue
        Function GetParam(ByVal schema As OrmSchemaBase, ByVal paramMgr As ICreateParam, ByVal f As IEntityFilter) As String
    End Interface

    Public Interface IEvaluableValue
        Inherits IParamFilterValue

        Enum EvalResult
            Found
            NotFound
            Unknown
        End Enum

        ReadOnly Property Value() As Object
        Function Eval(ByVal v As Object, ByVal template As OrmFilterTemplate) As EvalResult
    End Interface

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

        Public Overridable Function GetParam(ByVal schema As OrmSchemaBase, ByVal paramMgr As ICreateParam, ByVal f As IEntityFilter) As String Implements IEvaluableValue.GetParam
            Dim v As Object = _v
            If f IsNot Nothing Then
                v = f.PrepareValue(schema, v)
            End If

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
                Return _v.ToString
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

        Public Overridable Function Eval(ByVal v As Object, ByVal template As OrmFilterTemplate) As IEvaluableValue.EvalResult Implements IEvaluableValue.Eval
            Dim r As IEvaluableValue.EvalResult

            Dim val As Object = GetValue(v, template, r)
            If r <> IEvaluableValue.EvalResult.Unknown Then
                Return r
            Else
                r = IEvaluableValue.EvalResult.NotFound
            End If

            Select Case template.Operation
                Case FilterOperation.Equal
                    If Equals(v, val) Then
                        r = IEvaluableValue.EvalResult.Found
                    End If
                Case FilterOperation.GreaterEqualThan
                    Dim i As Integer = CType(v, IComparable).CompareTo(val)
                    If i >= 0 Then
                        r = IEvaluableValue.EvalResult.Found
                    End If
                Case FilterOperation.GreaterThan
                    Dim i As Integer = CType(v, IComparable).CompareTo(val)
                    If i > 0 Then
                        r = IEvaluableValue.EvalResult.Found
                    End If
                Case FilterOperation.LessEqualThan
                    Dim i As Integer = CType(v, IComparable).CompareTo(val)
                    If i <= 0 Then
                        r = IEvaluableValue.EvalResult.Found
                    End If
                Case FilterOperation.LessThan
                    Dim i As Integer = CType(v, IComparable).CompareTo(val)
                    If i < 0 Then
                        r = IEvaluableValue.EvalResult.Found
                    End If
                Case FilterOperation.NotEqual
                    If Not Equals(v, val) Then
                        r = IEvaluableValue.EvalResult.Found
                    End If
                Case FilterOperation.Like
                    Dim par As String = CStr(val)
                    Dim str As String = CStr(v)
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
                    End If
                Case Else
                    r = IEvaluableValue.EvalResult.Unknown
            End Select

            Return r
        End Function
    End Class

    Public Class LiteralValue
        Implements IParamFilterValue

        Private _pname As String

        Public Sub New(ByVal literal As String)
            _pname = literal
        End Sub

        Public Function GetParam(ByVal schema As OrmSchemaBase, ByVal paramMgr As ICreateParam, ByVal f As IEntityFilter) As String Implements IParamFilterValue.GetParam
            Return _pname
        End Function

        Public Function _ToString() As String Implements IFilterValue._ToString
            Return _pname
        End Function
    End Class

    Public Class DBNullValue
        Inherits LiteralValue

        Public Sub New()
            MyBase.New("null")
        End Sub

    End Class

    Public Class EntityValue
        Inherits ScalarValue

        Private _t As Type

        Public Sub New(ByVal o As OrmBase)
            MyBase.New()
            If o IsNot Nothing Then
                _t = o.GetType
                SetValue(o.Identifier)
            Else
                _t = GetType(OrmBase)
            End If
        End Sub

        Public Sub New(ByVal o As OrmBase, ByVal caseSensitive As Boolean)
            MyClass.New(o)
            Me.CaseSensitive = caseSensitive
        End Sub

        Public Function GetOrmValue(ByVal mgr As OrmManagerBase) As OrmBase
            Return mgr.CreateDBObject(CInt(Value), _t)
        End Function

        Public ReadOnly Property OrmType() As Type
            Get
                Return _t
            End Get
        End Property

        Protected Overrides Function GetValue(ByVal v As Object, ByVal template As OrmFilterTemplate, ByRef r As IEvaluableValue.EvalResult) As Object
            r = IEvaluableValue.EvalResult.Unknown
            Dim tt As Type = v.GetType
            Dim orm As OrmBase = TryCast(v, OrmBase)
            If orm IsNot Nothing Then
                Dim ov As EntityValue = TryCast(Me, EntityValue)
                If ov Is Nothing Then
                    Throw New InvalidOperationException(String.Format("Field {0} is Entity but param is not", template.FieldName))
                End If
                If Not tt.IsAssignableFrom(ov.OrmType) Then
                    If Value Is Nothing Then
                        r = IEvaluableValue.EvalResult.NotFound
                        Return Nothing
                    Else
                        Throw New InvalidOperationException(String.Format("Field {0} is type of {1} but param is type of {2}", template.FieldName, tt.ToString, ov.OrmType.ToString))
                    End If
                End If
                Return ov.GetOrmValue(OrmManagerBase.CurrentManager)
            End If
            Return Value
        End Function

        'Public Overrides ReadOnly Property Value() As Object
        '    Get
        '        Return GetOrmValue(OrmManagerBase.CurrentManager)
        '    End Get
        'End Property
    End Class

    Public Class InValue
        Inherits ScalarValue

        Private _l As New List(Of String)
        Private _str As String

        Public Sub New(ByVal value As ICollection)
            MyBase.New(value)
        End Sub

        Public Sub New(ByVal value As ICollection, ByVal caseSensitive As Boolean)
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

        Public Overrides Function Eval(ByVal v As Object, ByVal template As OrmFilterTemplate) As IEvaluableValue.EvalResult
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

        Public Shadows ReadOnly Property Value() As ICollection
            Get
                Return CType(MyBase.Value, Global.System.Collections.ICollection)
            End Get
        End Property

        Public Overrides Function GetParam(ByVal schema As OrmSchemaBase, ByVal paramMgr As ICreateParam, ByVal f As IEntityFilter) As String
            Dim sb As New StringBuilder
            Dim idx As Integer
            For Each o As Object In Value
                Dim v As Object = o
                If f IsNot Nothing Then
                    v = f.PrepareValue(schema, o)
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
    End Class

    Public Class BetweenValue
        Inherits ScalarValue

        Private _l As String
        Private _r As String

        Public Sub New(ByVal left As Object, ByVal right As Object)
            MyBase.New(New Pair(Of Object)(left, right))

            If left Is Nothing Then
                Throw New ArgumentNullException("left")
            End If

            If right Is Nothing Then
                Throw New ArgumentNullException("right")
            End If
        End Sub

        Public Overrides Function Eval(ByVal v As Object, ByVal template As OrmFilterTemplate) As IEvaluableValue.EvalResult
            Dim r As IEvaluableValue.EvalResult

            Dim val As Object = GetValue(v, template, r)
            If r <> IEvaluableValue.EvalResult.Unknown Then
                Return r
            Else
                r = IEvaluableValue.EvalResult.NotFound
            End If

            If template.Operation = FilterOperation.Between Then
                Dim int_v As Pair(Of Object) = Value
                Dim i As Integer = CType(v, IComparable).CompareTo(int_v.First)
                If i >= 0 Then
                    i = CType(v, IComparable).CompareTo(int_v.Second)
                    If i <= 0 Then
                        r = IEvaluableValue.EvalResult.Found
                    End If
                End If
            Else
                Throw New InvalidOperationException(String.Format("Invalid operation {0} for BetweenValue", template.OperToString))
            End If

            Return r
        End Function

        Public Overrides Function GetParam(ByVal schema As OrmSchemaBase, ByVal paramMgr As ICreateParam, _
            ByVal f As IEntityFilter) As String

            If paramMgr Is Nothing Then
                Throw New ArgumentNullException("paramMgr")
            End If

            Dim left As Object = Value.First, right As Object = Value.Second
            If f IsNot Nothing Then
                left = f.PrepareValue(schema, left)
                right = f.PrepareValue(schema, right)
            End If

            _l = paramMgr.AddParam(_l, left)
            _r = paramMgr.AddParam(_r, right)

            Return _l & " and " & _r
        End Function

        Public Overrides Function _ToString() As String
            Return Value.First.ToString & "__$__" & Value.Second.ToString
        End Function

        Public Shadows ReadOnly Property Value() As Pair(Of Object)
            Get
                Return CType(MyBase.Value, Pair(Of Object))
            End Get
        End Property
    End Class
End Namespace

Namespace Database
    Namespace Criteria.Values
        Public Interface IDatabaseFilterValue
            Inherits Worm.Criteria.Values.IFilterValue
            Function GetParam(ByVal schema As DbSchema, ByVal paramMgr As ICreateParam, ByVal almgr As AliasMgr) As String
        End Interface

        Public Class SubQuery
            Implements IDatabaseFilterValue, Worm.Criteria.Values.INonTemplateValue

            Private _t As Type
            Private _tbl As OrmTable
            Private _f As Worm.Database.Criteria.Core.IFilter
            Private _field As String

            Public Sub New(ByVal t As Type, ByVal f As Worm.Database.Criteria.Core.IFilter)
                _t = t
                _f = f
            End Sub

            Public Sub New(ByVal table As OrmTable, ByVal f As Worm.Database.Criteria.Core.IFilter)
                _tbl = table
                _f = f
            End Sub

            Public Sub New(ByVal t As Type, ByVal f As Worm.Database.Criteria.Core.IEntityFilter, ByVal field As String)
                '_tbl = CType(OrmManagerBase.CurrentManager.ObjectSchema.GetObjectSchema(t), IOrmObjectSchema).GetTables(0)
                _t = t
                _f = f
                _field = field
            End Sub

            Public Function _ToString() As String Implements IDatabaseFilterValue._ToString
                Dim r As String = Nothing
                If _t IsNot Nothing Then
                    r = _t.ToString()
                Else
                    r = _tbl.TableName
                End If
                If _f IsNot Nothing Then
                    r &= "$" & _f.ToString
                End If
                If Not String.IsNullOrEmpty(_field) Then
                    r &= "$" & _field
                End If
                Return r
            End Function

            Public Function GetParam(ByVal dbschema As DbSchema, ByVal paramMgr As ICreateParam, ByVal almgr As AliasMgr) As String Implements IDatabaseFilterValue.GetParam
                Dim sb As New StringBuilder
                'Dim dbschema As DbSchema = CType(schema, DbSchema)
                sb.Append("(")
                If _t Is Nothing Then
                    sb.Append(dbschema.SelectWithJoin(Nothing, New OrmTable() {_tbl}, almgr, paramMgr, Nothing, False, Nothing, Nothing, Nothing, Nothing))
                Else
                    Dim arr As Generic.IList(Of ColumnAttribute) = Nothing
                    If Not String.IsNullOrEmpty(_field) Then
                        arr = New Generic.List(Of ColumnAttribute)
                        arr.Add(New ColumnAttribute(_field))
                    End If
                    sb.Append(dbschema.SelectWithJoin(_t, almgr, paramMgr, Nothing, arr IsNot Nothing, Nothing, Nothing, arr))
                End If

                dbschema.AppendWhere(_t, _f, almgr, sb, Nothing, paramMgr)

                sb.Append(")")

                Return sb.ToString
            End Function

            Public Function GetStaticString() As String Implements Worm.Criteria.Values.INonTemplateValue.GetStaticString
                Dim r As String = Nothing
                If _t IsNot Nothing Then
                    r = _t.ToString()
                Else
                    r = _tbl.TableName
                End If
                If _f IsNot Nothing Then
                    r &= "$" & _f.ToStaticString
                End If
                If Not String.IsNullOrEmpty(_field) Then
                    r &= "$" & _field
                End If
                Return r
            End Function
        End Class

    End Namespace
End Namespace