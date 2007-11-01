Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports CoreFramework.Structures
Imports CoreFramework.Threading
Imports System.Collections.Generic

Namespace Orm

#Region " Filter value "

    Public Interface IFilterValue
        Function GetParam(ByVal schema As OrmSchemaBase, ByVal paramMgr As ICreateParam, ByVal f As IEntityFilter) As String
        Function _ToString() As String
    End Interface

    Public Interface IEvaluableValue
        Inherits IFilterValue
        ReadOnly Property Value() As Object
    End Interface

    Public Class SimpleValue
        Implements IEvaluableValue

        Private _v As Object
        Private _pname As String
        'Private _f As IEntityFilter

        Protected Sub New()
        End Sub

        'Public Sub New(ByVal value As Object)
        '    _v = value
        'End Sub

        Public Sub New(ByVal value As Object)
            _v = value
        End Sub

        Public Function GetParam(ByVal schema As OrmSchemaBase, ByVal paramMgr As ICreateParam, ByVal f As IEntityFilter) As String Implements IFilterValue.GetParam
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
    End Class

    Public Class LiteralValue
        Implements IFilterValue

        Private _pname As String

        Public Sub New(ByVal literal As String)
            _pname = literal
        End Sub

        Public Function GetParam(ByVal schema As OrmSchemaBase, ByVal paramMgr As ICreateParam, ByVal f As IEntityFilter) As String Implements IFilterValue.GetParam
            Return _pname
        End Function

        Public Function _ToString() As String Implements IFilterValue._ToString
            Return _pname
        End Function
    End Class

    Public Class EntityValue
        Inherits SimpleValue

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

        Public Function GetOrmValue(ByVal mgr As OrmManagerBase) As OrmBase
            Return mgr.CreateDBObject(CInt(Value), _t)
        End Function

        Public ReadOnly Property OrmType() As Type
            Get
                Return _t
            End Get
        End Property

        'Public Overrides ReadOnly Property Value() As Object
        '    Get
        '        Return GetOrmValue(OrmManagerBase.CurrentManager)
        '    End Get
        'End Property
    End Class

#End Region

    Public Interface IOrmFilterTemplate
        Function MakeHash(ByVal schema As OrmSchemaBase, ByVal oschema As IOrmObjectSchemaBase, ByVal obj As OrmBase) As String
        Function MakeFilter(ByVal schema As OrmSchemaBase, ByVal oschema As IOrmObjectSchemaBase, ByVal obj As OrmBase) As IEntityFilter
        Sub SetType(ByVal t As Type)
    End Interface

    Public Interface IFilter
        Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal tableAliases As IDictionary(Of OrmTable, String), ByVal pname As ICreateParam) As String
        Function GetAllFilters() As ICollection(Of IFilter)
        Function Equals(ByVal f As IFilter) As Boolean
        Function ReplaceFilter(ByVal replacement As IFilter, ByVal replacer As IFilter) As IFilter
        Function ToString() As String
    End Interface

    Public Interface ITemplateFilter
        Inherits IFilter
        'Function GetStaticString() As String
        Function ReplaceByTemplate(ByVal replacement As ITemplateFilter, ByVal replacer As ITemplateFilter) As ITemplateFilter
        ReadOnly Property Template() As TemplateBase
        Function MakeSingleStmt(ByVal schema As DbSchema, ByVal pname As ICreateParam) As Pair(Of String)
    End Interface

    Public Interface IValuableFilter
        ReadOnly Property Value() As IFilterValue
    End Interface

    Public Interface IEntityFilter
        Inherits ITemplateFilter

        Enum EvalResult
            Found
            NotFound
            Unknown
        End Enum

        Function Eval(ByVal schema As OrmSchemaBase, ByVal obj As OrmBase, ByVal oschema As IOrmObjectSchemaBase) As EvalResult
        Function GetFilterTemplate() As IOrmFilterTemplate
        Function PrepareValue(ByVal schema As OrmSchemaBase, ByVal v As Object) As Object
        Function MakeHash() As String
    End Interface

    Public MustInherit Class TemplateBase

        Private _oper As FilterOperation

        Public Sub New()

        End Sub

        Public Sub New(ByVal operation As FilterOperation)
            _oper = operation
        End Sub

        Public ReadOnly Property Operation() As FilterOperation
            Get
                Return _oper
            End Get
        End Property

        Protected Friend ReadOnly Property OperToString() As String
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

        Protected Friend Function Oper2String() As String
            Return Oper2String(_oper)
        End Function

        Protected Friend Shared Function Oper2String(ByVal oper As FilterOperation) As String
            Select Case oper
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
                    Throw New DBSchemaException("invalid opration " & oper.ToString)
            End Select
        End Function

        Public MustOverride Function GetStaticString() As String

        Public Overrides Function ToString() As String
            Return GetStaticString()
        End Function

    End Class

    Public Class TableFilterTemplate
        Inherits TemplateBase

        Private _tbl As OrmTable
        Private _col As String

        Public Sub New(ByVal table As OrmTable, ByVal column As String, ByVal operation As FilterOperation)
            MyBase.New(operation)
            _tbl = table
            _col = column
        End Sub

        Public Sub New(ByVal table As OrmTable, ByVal column As String)
            MyBase.New()
            _tbl = table
            _col = column
        End Sub

        Public ReadOnly Property Table() As OrmTable
            Get
                Return _tbl
            End Get
        End Property

        Public ReadOnly Property Column() As String
            Get
                Return _col
            End Get
        End Property

        Public Overrides Function GetStaticString() As String
            Return _tbl.TableName() & _col & Oper2String()
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, TableFilterTemplate))
        End Function

        Public Overloads Function Equals(ByVal obj As TableFilterTemplate) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Return _tbl Is obj._tbl AndAlso _col Is obj._col AndAlso Operation = obj.Operation
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return GetStaticString.GetHashCode
        End Function
    End Class

    Public MustInherit Class FilterBase
        Implements IFilter, IValuableFilter

        Private _v As IFilterValue

        Public Sub New(ByVal value As IFilterValue)
            If value Is Nothing Then
                Throw New ArgumentNullException("value")
            End If
            _v = value
        End Sub

        Protected MustOverride Function _ToString() As String Implements IFilter.ToString
        Public MustOverride Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal tableAliases As System.Collections.Generic.IDictionary(Of OrmTable, String), ByVal pname As ICreateParam) As String Implements IFilter.MakeSQLStmt
        Public MustOverride Function GetAllFilters() As System.Collections.Generic.ICollection(Of IFilter) Implements IFilter.GetAllFilters

        Public Overrides Function ToString() As String
            Return _ToString()
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _ToString.GetHashCode()
        End Function

        Public Overrides Function Equals(ByVal f As Object) As Boolean
            Return Equals(TryCast(f, FilterBase))
        End Function

        Public Overloads Function Equals(ByVal f As FilterBase) As Boolean
            If f IsNot Nothing Then
                Return _ToString.Equals(f.ToString)
            Else
                Return False
            End If
        End Function

        Public ReadOnly Property Value() As IFilterValue Implements IValuableFilter.Value
            Get
                Return _v
            End Get
        End Property

        'Protected Sub SetValue(ByVal v As IFilterValue)
        '    _v = v
        'End Sub

        Protected Function GetParam(ByVal schema As OrmSchemaBase, ByVal pmgr As ICreateParam) As String
            If _v Is Nothing Then
                'Return pmgr.CreateParam(Nothing)
                Throw New InvalidOperationException("Param is null")
            End If
            Return _v.GetParam(schema, pmgr, TryCast(Me, IEntityFilter))
        End Function

        Private Function Equals1(ByVal f As IFilter) As Boolean Implements IFilter.Equals
            Return Equals(TryCast(f, FilterBase))
        End Function

        Public Function ReplaceFilter(ByVal replacement As IFilter, ByVal replacer As IFilter) As IFilter Implements IFilter.ReplaceFilter
            Return ReplaceFilter(TryCast(replacement, FilterBase), TryCast(replacer, FilterBase))
        End Function

        Public Function ReplaceFilter(ByVal replacement As FilterBase, ByVal replacer As FilterBase) As FilterBase
            If Equals(replacement) Then
                Return replacer
            End If
            Return Nothing
        End Function
    End Class

    Public MustInherit Class TemplatedFilterBase
        Inherits FilterBase
        Implements ITemplateFilter

        Private _templ As TemplateBase

        Public Sub New(ByVal v As IFilterValue, ByVal template As TemplateBase)
            MyBase.New(v)
            _templ = template
        End Sub

        'Public Function GetStaticString() As String Implements ITemplateFilter.GetStaticString
        '    Return _templ.GetStaticString
        'End Function

        Public Function Replacetemplate(ByVal replacement As ITemplateFilter, ByVal replacer As ITemplateFilter) As ITemplateFilter Implements ITemplateFilter.ReplaceByTemplate
            If Not _templ.Equals(replacement.Template) Then
                Return Nothing
            End If
            Return replacer
        End Function

        Public ReadOnly Property Template() As TemplateBase Implements ITemplateFilter.Template
            Get
                Return _templ
            End Get
        End Property

        Public MustOverride Function MakeSingleStmt(ByVal schema As DbSchema, ByVal pname As ICreateParam) As Pair(Of String) Implements ITemplateFilter.MakeSingleStmt
    End Class

    Public Class TableFilter
        Inherits TemplatedFilterBase

        'Private _templ As TableFilterTemplate

        Public Sub New(ByVal table As OrmTable, ByVal column As String, ByVal value As IFilterValue, ByVal operation As FilterOperation)
            MyBase.New(value, New TableFilterTemplate(table, column, operation))
            '_templ = New TableFilterTemplate(table, column, operation)
        End Sub

        Protected Overrides Function _ToString() As String
            'Return _templ.Table.TableName & _templ.Column & Value._ToString & _templ.OperToString
            Return Value._ToString & Template.GetStaticString()
        End Function

        'Public Overrides Function GetStaticString() As String
        '    Return _templ.Table.TableName() & _templ.Column & _templ.Oper2String
        'End Function

        Public Shadows ReadOnly Property Template() As TableFilterTemplate
            Get
                Return CType(MyBase.Template, TableFilterTemplate)
            End Get
        End Property

        Public Overrides Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal tableAliases As System.Collections.Generic.IDictionary(Of OrmTable, String), ByVal pname As ICreateParam) As String
            Dim map As New MapField2Column(String.Empty, Template.Column, Template.Table)
            Dim [alias] As String = String.Empty

            If tableAliases IsNot Nothing Then
                [alias] = tableAliases(map._tableName) & "."
            End If

            Return [alias] & map._columnName & Template.Oper2String & GetParam(schema, pname)
        End Function

        Public Overrides Function GetAllFilters() As System.Collections.Generic.ICollection(Of IFilter)
            Return New TableFilter() {Me}
        End Function

        Public Overrides Function MakeSingleStmt(ByVal schema As DbSchema, ByVal pname As ICreateParam) As Pair(Of String)
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If pname Is Nothing Then
                Throw New ArgumentNullException("pname")
            End If

            Dim prname As String = Value.GetParam(schema, pname, Nothing)

            Return New Pair(Of String)(Template.Column, prname)
        End Function
    End Class

    Public Class OrmFilterTemplate
        Inherits TemplateBase
        Implements IOrmFilterTemplate

        Private _t As Type
        Private _fieldname As String

        'Private _appl As Boolean

        Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal oper As FilterOperation) ', ByVal appl As Boolean)
            MyBase.New(oper)
            _t = t
            _fieldname = fieldName
            '_appl = appl
        End Sub

        Public Function MakeFilter(ByVal schema As OrmSchemaBase, ByVal oschema As IOrmObjectSchemaBase, ByVal obj As OrmBase) As IEntityFilter Implements IOrmFilterTemplate.MakeFilter
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            If obj.GetType IsNot _t Then
                Dim o As OrmBase = schema.GetJoinObj(oschema, obj, _t)
                If o Is Nothing Then
                    Throw New ArgumentException(String.Format("Template type {0} is not match {1}", _t.ToString, obj.GetType))
                End If
                Return MakeFilter(schema, schema.GetObjectSchema(_t), o)
            Else
                Dim v As Object = schema.GetFieldValue(obj, _fieldname, oschema)

                Return New EntityFilter(_t, _fieldname, New SimpleValue(v), Operation)
            End If
        End Function

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

        Public Sub SetType(ByVal t As System.Type) Implements IOrmFilterTemplate.SetType
            If _t Is Nothing Then
                _t = t
            End If
        End Sub

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, OrmFilterTemplate))
        End Function

        Public Overloads Function Equals(ByVal obj As OrmFilterTemplate) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Return _t Is obj._t AndAlso _fieldname Is obj._fieldname AndAlso Operation = obj.Operation
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return GetStaticString.GetHashCode
        End Function

        Public Overrides Function GetStaticString() As String
            Return _t.ToString & _fieldname & Oper2String()
        End Function

        Public Function MakeHash(ByVal schema As OrmSchemaBase, ByVal oschema As IOrmObjectSchemaBase, ByVal obj As OrmBase) As String Implements IOrmFilterTemplate.MakeHash
            If Operation = FilterOperation.Equal Then
                Return MakeFilter(schema, oschema, obj).ToString
            Else
                Return EntityFilter.EmptyHash
            End If
        End Function
    End Class

    Public Class EntityFilter
        Inherits TemplatedFilterBase
        Implements IEntityFilter

        'Private _templ As OrmFilterTemplate
        Private _str As String
        Private _oschema As IOrmObjectSchemaBase

        Public Const EmptyHash As String = "fd_empty_hash_aldf"

        Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal value As IFilterValue, ByVal operation As FilterOperation)
            MyBase.New(value, New OrmFilterTemplate(t, fieldName, operation))
        End Sub

        Protected Overrides Function _ToString() As String
            If _str Is Nothing Then
                _str = Value._ToString & Template.GetStaticString
            End If
            Return _str
        End Function

        'Public Overrides Function GetStaticString() As String
        '    Return _templ.Type.ToString & _templ.FieldName & _templ.Oper2String
        'End Function

        Public Shadows ReadOnly Property Template() As OrmFilterTemplate
            Get
                Return CType(MyBase.Template, OrmFilterTemplate)
            End Get
        End Property

        Public Overrides Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal tableAliases As System.Collections.Generic.IDictionary(Of OrmTable, String), ByVal pname As ICreateParam) As String
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If _oschema Is Nothing Then
                _oschema = schema.GetObjectSchema(Template.Type)
            End If

            Dim map As MapField2Column = _oschema.GetFieldColumnMap()(Template.FieldName)
            Dim [alias] As String = String.Empty

            If tableAliases IsNot Nothing Then
                [alias] = tableAliases(map._tableName) & "."
            End If

            Return [alias] & map._columnName & Template.Oper2String & GetParam(schema, pname)
        End Function

        Public Function Eval(ByVal schema As OrmSchemaBase, ByVal obj As OrmBase, ByVal oschema As IOrmObjectSchemaBase) As IEntityFilter.EvalResult Implements IEntityFilter.Eval
            Dim evval As IEvaluableValue = TryCast(Value, IEvaluableValue)
            If evval IsNot Nothing Then
                If schema Is Nothing Then
                    Throw New ArgumentNullException("schema")
                End If

                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                Dim t As Type = obj.GetType
                If Template.Type Is t Then
                    Dim r As IEntityFilter.EvalResult = IEntityFilter.EvalResult.NotFound
                    Dim v As Object = obj.GetValue(Template.FieldName, oschema) 'schema.GetFieldValue(obj, _fieldname)
                    If v IsNot Nothing Then
                        Dim tt As Type = v.GetType
                        Dim val As Object = evval.Value
                        Dim orm As OrmBase = TryCast(v, OrmBase)
                        If orm IsNot Nothing Then
                            Dim ov As EntityValue = TryCast(evval, EntityValue)
                            If ov Is Nothing Then
                                Throw New InvalidOperationException(String.Format("Field {0} is Entity but param is not", Template.FieldName))
                            End If
                            If tt IsNot ov.OrmType Then
                                If val Is Nothing Then
                                    Return IEntityFilter.EvalResult.NotFound
                                Else
                                    Throw New InvalidOperationException(String.Format("Field {0} is type of {1} but param is type of {2}", Template.FieldName, tt.ToString, ov.OrmType.ToString))
                                End If
                            End If
                            val = ov.GetOrmValue(OrmManagerBase.CurrentManager)
                        End If

                        Select Case Template.Operation
                            Case FilterOperation.Equal
                                If Equals(v, val) Then
                                    r = IEntityFilter.EvalResult.Found
                                End If
                            Case FilterOperation.GreaterEqualThan
                                Dim i As Integer = CType(v, IComparable).CompareTo(val)
                                If i >= 0 Then
                                    r = IEntityFilter.EvalResult.Found
                                End If
                            Case FilterOperation.GreaterThan
                                Dim i As Integer = CType(v, IComparable).CompareTo(val)
                                If i > 0 Then
                                    r = IEntityFilter.EvalResult.Found
                                End If
                            Case FilterOperation.LessEqualThan
                                Dim i As Integer = CType(v, IComparable).CompareTo(val)
                                If i <= 0 Then
                                    r = IEntityFilter.EvalResult.Found
                                End If
                            Case FilterOperation.LessThan
                                Dim i As Integer = CType(v, IComparable).CompareTo(val)
                                If i < 0 Then
                                    r = IEntityFilter.EvalResult.Found
                                End If
                            Case FilterOperation.NotEqual
                                If Not Equals(v, val) Then
                                    r = IEntityFilter.EvalResult.Found
                                End If
                            Case FilterOperation.Like
                                Dim par As String = CStr(val)
                                Dim str As String = CStr(v)
                                r = IEntityFilter.EvalResult.NotFound
                                If par.StartsWith("%") Then
                                    If par.EndsWith("%") Then
                                        If str.IndexOf(par.Trim("%"c), StringComparison.InvariantCultureIgnoreCase) >= 0 Then
                                            r = IEntityFilter.EvalResult.Found
                                        End If
                                    Else
                                        If str.EndsWith(par.TrimStart("%"c)) Then
                                            r = IEntityFilter.EvalResult.Found
                                        End If
                                    End If
                                ElseIf par.EndsWith("%") Then
                                    If str.StartsWith(par.TrimEnd("%"c)) Then
                                        r = IEntityFilter.EvalResult.Found
                                    End If
                                End If
                            Case Else
                                r = IEntityFilter.EvalResult.Unknown
                        End Select
                    Else
                        If evval.Value Is Nothing Then
                            r = IEntityFilter.EvalResult.Found
                        End If
                    End If

                    Return r
                Else
                    Dim o As OrmBase = schema.GetJoinObj(oschema, obj, Template.Type)
                    If o IsNot Nothing Then
                        Return Eval(schema, o, schema.GetObjectSchema(Template.Type))
                    End If
                End If
            End If

            Return IEntityFilter.EvalResult.Unknown
        End Function

        Public Overrides Function GetAllFilters() As System.Collections.Generic.ICollection(Of IFilter)
            Return New EntityFilter() {Me}
        End Function

        Public Function GetFilterTemplate() As IOrmFilterTemplate Implements IEntityFilter.GetFilterTemplate
            If TryCast(Value, IEvaluableValue) IsNot Nothing Then
                Return Template
            End If
            Return Nothing
        End Function

        Public Function PrepareValue(ByVal schema As OrmSchemaBase, ByVal v As Object) As Object Implements IEntityFilter.PrepareValue
            Return schema.ChangeValueType(_oschema, New ColumnAttribute(Template.FieldName), v)
        End Function

        'Public Overrides Function Equals(ByVal obj As Object) As Boolean
        '    Return Equals(TryCast(obj, EntityFilter))
        'End Function

        'Public Overloads Function Equals(ByVal obj As EntityFilter) As Boolean
        '    If obj Is Nothing Then
        '        Return False
        '    End If
        '    Return _str = obj._str
        'End Function

        'Public Overrides Function GetHashCode() As Integer
        '    Return _str.GetHashCode
        'End Function

        Public Overrides Function MakeSingleStmt(ByVal schema As DbSchema, ByVal pname As ICreateParam) As Pair(Of String)
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If pname Is Nothing Then
                Throw New ArgumentNullException("pname")
            End If

            If _oschema Is Nothing Then
                _oschema = schema.GetObjectSchema(Template.Type)
            End If

            Dim prname As String = Value.GetParam(schema, pname, Me)

            Dim map As MapField2Column = _oschema.GetFieldColumnMap()(Template.FieldName)

            Dim v As IEvaluableValue = TryCast(Value, IEvaluableValue)
            If v IsNot Nothing AndAlso v.Value Is DBNull.Value Then
                If schema.GetFieldTypeByName(Template.Type, Template.FieldName) Is GetType(Byte()) Then
                    pname.GetParameter(prname).DbType = System.Data.DbType.Binary
                ElseIf schema.GetFieldTypeByName(Template.Type, Template.FieldName) Is GetType(Decimal) Then
                    pname.GetParameter(prname).DbType = System.Data.DbType.Decimal
                End If
            End If

            Return New Pair(Of String)(map._columnName, prname)
        End Function

        Public Function MakeHash() As String Implements IEntityFilter.MakeHash
            If Template.Operation = FilterOperation.Equal Then
                Return ToString()
            Else
                Return EmptyHash
            End If
        End Function
    End Class

End Namespace