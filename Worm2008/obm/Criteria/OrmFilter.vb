Imports System.Collections.Generic
Imports Worm.Orm.Meta
Imports Worm.Criteria.Values
Imports Worm.Orm
Imports Worm.Expressions

Namespace Criteria.Core

    Public Interface IGetFilter
        ReadOnly Property Filter() As IFilter
        ReadOnly Property Filter(ByVal t As Type) As IFilter
    End Interface

    Public Interface IFilter
        Inherits IGetFilter, ICloneable
        'Function MakeQueryStmt(ByVal schema As QueryGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As String
        Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam, ByVal columns As List(Of String)) As String
        Function GetAllFilters() As ICollection(Of IFilter)
        Function Equals(ByVal f As IFilter) As Boolean
        Function ReplaceFilter(ByVal replacement As IFilter, ByVal replacer As IFilter) As IFilter
        Function ToString() As String
        Function ToStaticString() As String
        Overloads Function Clone() As IFilter
    End Interface

    Public Interface ITemplateFilterBase
        'Function ReplaceByTemplate(ByVal replacement As ITemplateFilter, ByVal replacer As ITemplateFilter) As ITemplateFilter
        ReadOnly Property Template() As ITemplate
        Function MakeSingleQueryStmt(ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As Pair(Of String)
    End Interface

    Public Interface ITemplateFilter
        Inherits IFilter, ITemplateFilterBase
        'Function GetStaticString() As String
    End Interface

    Public Interface IValuableFilter
        ReadOnly Property Value() As IFilterValue
    End Interface

    Public Interface IEntityFilterBase
        Function Eval(ByVal schema As ObjectMappingEngine, ByVal obj As _IEntity, ByVal oschema As IObjectSchemaBase) As IEvaluableValue.EvalResult
        Function GetFilterTemplate() As IOrmFilterTemplate
        'Function PrepareValue(ByVal schema As ObjectMappingEngine, ByVal v As Object) As Object
        Function MakeHash() As String
    End Interface

    Public Interface IEntityFilter
        Inherits ITemplateFilter, IEntityFilterBase
        Property PrepareValue() As Boolean
    End Interface

    Public Interface ITemplate
        ReadOnly Property Operation() As FilterOperation
        ReadOnly Property OperToString() As String
        ReadOnly Property OperToStmt() As String
        Function GetStaticString() As String
    End Interface

    Public Interface IOrmFilterTemplate
        Inherits ITemplate
        Function MakeHash(ByVal schema As ObjectMappingEngine, ByVal oschema As IObjectSchemaBase, ByVal obj As ICachedEntity) As String
        'Function MakeFilter(ByVal schema As OrmSchemaBase, ByVal oschema As IOrmObjectSchemaBase, ByVal obj As OrmBase) As IEntityFilter
        Sub SetType(ByVal t As Type)
    End Interface

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
        Protected MustOverride Function _Clone() As Object Implements ICloneable.Clone
        Public MustOverride Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String Implements IFilter.MakeQueryStmt
        Public MustOverride Function GetAllFilters() As System.Collections.Generic.ICollection(Of IFilter) Implements IFilter.GetAllFilters
        Public MustOverride Function ToStaticString() As String Implements IFilter.ToStaticString

        Public Function Clone() As IFilter Implements IFilter.Clone
            Return CType(_Clone(), IFilter)
        End Function

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

        'Public ReadOnly Property ParamValue() As IParamFilterValue Implements IValuableFilter.Value
        '    Get
        '        Return CType(_v, IParamFilterValue)
        '    End Get
        'End Property

        Public ReadOnly Property Value() As IFilterValue Implements IValuableFilter.Value
            Get
                Return CType(_v, IFilterValue)
            End Get
        End Property

        'Protected Sub SetValue(ByVal v As IFilterValue)
        '    _v = v
        'End Sub

        Protected Overridable Function GetParam(ByVal schema As ObjectMappingEngine, ByVal pmgr As ICreateParam) As String
            If _v Is Nothing Then
                'Return pmgr.CreateParam(Nothing)
                Throw New InvalidOperationException("Param is null")
            End If
            Return Value.GetParam(schema, pmgr, Nothing, Nothing, Nothing, Nothing)
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

        Protected ReadOnly Property val() As IFilterValue
            Get
                Return _v
            End Get
        End Property

        Public ReadOnly Property Filter() As IFilter Implements IGetFilter.Filter
            Get
                Return Me
            End Get
        End Property

        Public ReadOnly Property Filter(ByVal t As System.Type) As IFilter Implements IGetFilter.Filter
            Get
                Return Me
            End Get
        End Property

        'Protected Function _MakeQueryStmt(ByVal schema As QueryGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam) As String Implements IFilter.MakeQueryStmt
        '    Throw New NotSupportedException
        '    'Return MakeQueryStmt(schema, Filter, almgr, pname)
        'End Function
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

        'Public Function Replacetemplate(ByVal replacement As ITemplateFilter, ByVal replacer As ITemplateFilter) As ITemplateFilter Implements ITemplateFilter.ReplaceByTemplate
        '    If Not _templ.Equals(replacement.Template) Then
        '        Return Nothing
        '    End If
        '    Return replacer
        'End Function

        Public MustOverride Function MakeSingleQueryStmt(ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As Pair(Of String) Implements ITemplateFilter.MakeSingleQueryStmt

        Public Overrides Function ToStaticString() As String
            Return _templ.GetStaticString
        End Function

        Public ReadOnly Property Template() As ITemplate Implements ITemplateFilterBase.Template
            Get
                Return _templ
            End Get
        End Property
    End Class

    Public MustInherit Class EntityFilterBase
        Inherits TemplatedFilterBase
        Implements IEntityFilter


        'Private _templ As OrmFilterTemplate
        Private _str As String
        Protected _oschema As IObjectSchemaBase
        Private _prep As Boolean = True

        Public Const EmptyHash As String = "fd_empty_hash_aldf"

        Public Sub New(ByVal value As IFilterValue, ByVal tmp As OrmFilterTemplateBase)
            MyBase.New(value, tmp)
        End Sub

        Protected Overrides Function _ToString() As String
            If _str Is Nothing Then
                _str = val._ToString & Template.GetStaticString
            End If
            Return _str
        End Function

        'Public Overrides Function GetStaticString() As String
        '    Return _templ.Type.ToString & _templ.FieldName & _templ.Oper2String
        'End Function

        Public Shadows ReadOnly Property Template() As OrmFilterTemplateBase
            Get
                Return CType(MyBase.Template, OrmFilterTemplateBase)
            End Get
        End Property

        Public Function Eval(ByVal schema As ObjectMappingEngine, ByVal obj As _IEntity, ByVal oschema As IObjectSchemaBase) As IEvaluableValue.EvalResult Implements IEntityFilter.Eval
            Dim evval As IEvaluableValue = TryCast(val, IEvaluableValue)
            If evval IsNot Nothing Then
                If schema Is Nothing Then
                    Throw New ArgumentNullException("schema")
                End If

                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                Dim t As Type = obj.GetType
                If Template.Type Is t Then
                    Dim r As IEvaluableValue.EvalResult = IEvaluableValue.EvalResult.NotFound
                    Dim v As Object = obj.GetValue(Nothing, New ColumnAttribute(Template.FieldName), oschema) 'schema.GetFieldValue(obj, _fieldname)
                    r = evval.Eval(v, Template)
                    'If v IsNot Nothing Then
                    '    r = evval.Eval(v, Template)
                    'Else
                    '    If evval.Value Is Nothing Then
                    '        r = IEvaluableValue.EvalResult.Found
                    '    End If
                    'End If

                    Return r
                Else
                    Dim o As IOrmBase = schema.GetJoinObj(oschema, obj, Template.Type)
                    If o IsNot Nothing Then
                        Return Eval(schema, o, schema.GetObjectSchema(Template.Type))
                    End If
                End If
            End If

            Return IEvaluableValue.EvalResult.Unknown
        End Function

        Public Overrides Function GetAllFilters() As System.Collections.Generic.ICollection(Of IFilter)
            Return New EntityFilterBase() {Me}
        End Function

        Public Function GetFilterTemplate() As IOrmFilterTemplate Implements IEntityFilter.GetFilterTemplate
            'If TryCast(Value, IEvaluableValue) IsNot Nothing Then
            Return Template
            'End If
            'Return Nothing
        End Function

        Public Function PrepareValue(ByVal schema As ObjectMappingEngine, ByVal v As Object) As Object 'Implements IEntityFilter.PrepareValue
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

        Public MustOverride Overloads Function MakeQueryStmt(ByVal oschema As IObjectSchemaBase, ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String

        Public Overloads Function MakeQueryStmt(ByVal oschema As IObjectSchemaBase, ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam) As String
            Return MakeQueryStmt(oschema, filterInfo, schema, almgr, pname, Nothing)
        End Function

        Public Overridable Overloads Function MakeSingleQueryStmt(ByVal oschema As IObjectSchemaBase, ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As Pair(Of String)
            If _oschema Is Nothing Then
                _oschema = oschema
            End If

            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If pname Is Nothing Then
                Throw New ArgumentNullException("pname")
            End If

            Dim pd As Values.PrepareValueDelegate = Nothing
            If _prep Then
                pd = AddressOf PrepareValue
            End If

            Dim prname As String = Value.GetParam(schema, pname, almgr, pd, Nothing, Nothing)

            Dim map As MapField2Column = oschema.GetFieldColumnMap()(Template.FieldName)

            Dim v As IEvaluableValue = TryCast(val, IEvaluableValue)
            If v IsNot Nothing AndAlso v.Value Is DBNull.Value Then
                If schema.GetFieldTypeByName(Template.Type, Template.FieldName) Is GetType(Byte()) Then
                    pname.GetParameter(prname).DbType = System.Data.DbType.Binary
                ElseIf schema.GetFieldTypeByName(Template.Type, Template.FieldName) Is GetType(Decimal) Then
                    pname.GetParameter(prname).DbType = System.Data.DbType.Decimal
                End If
            End If

            Return New Pair(Of String)(map._columnName, prname)
        End Function

        Public Overrides Function MakeSingleQueryStmt(ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As Pair(Of String)
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If _oschema Is Nothing Then
                _oschema = schema.GetObjectSchema(Template.Type)
            End If

            Return MakeSingleQueryStmt(_oschema, schema, almgr, pname)
        End Function

        Public Function MakeHash() As String Implements IEntityFilter.MakeHash
            If Template.Operation = FilterOperation.Equal Then
                Return ToString()
            Else
                Return EmptyHash
            End If
        End Function

        Private Property _PrepareValue() As Boolean Implements IEntityFilter.PrepareValue
            Get
                Return _prep
            End Get
            Set(ByVal value As Boolean)
                _prep = value
            End Set
        End Property
    End Class

    Public MustInherit Class TemplateBase
        Implements Worm.Criteria.Core.ITemplate

        Private _oper As FilterOperation

        Public Sub New()

        End Sub

        Public Sub New(ByVal operation As FilterOperation)
            _oper = operation
        End Sub

        Public ReadOnly Property Operation() As FilterOperation Implements ITemplate.Operation
            Get
                Return _oper
            End Get
        End Property

        Public Shared Function OperToStringInternal(ByVal oper As FilterOperation) As String
            Select Case oper
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
                Case FilterOperation.Is
                    Return "Is"
                Case FilterOperation.IsNot
                    Return "IsNot"
                Case FilterOperation.Exists
                    Return "Exists"
                Case FilterOperation.NotExists
                    Return "NotExists"
                Case FilterOperation.Between
                    Return "Between"
                Case Else
                    Throw New ObjectMappingException("Operation " & oper & " not supported")
            End Select
        End Function

        Public ReadOnly Property OperToString() As String Implements ITemplate.OperToString
            Get
                Return OperToStringInternal(_oper)
            End Get
        End Property

        Public MustOverride Function GetStaticString() As String Implements ITemplate.GetStaticString
        Public MustOverride ReadOnly Property OperToStmt() As String Implements ITemplate.OperToStmt
    End Class

    Public MustInherit Class OrmFilterTemplateBase
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

        Public Overridable Function MakeFilter(ByVal schema As ObjectMappingEngine, ByVal oschema As IObjectSchemaBase, ByVal obj As ICachedEntity) As IEntityFilter 'Implements IOrmFilterTemplate.MakeFilter
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            If obj.GetType IsNot _t Then
                Dim o As IOrmBase = schema.GetJoinObj(oschema, obj, _t)
                If o Is Nothing Then
                    Throw New ArgumentException(String.Format("Template type {0} is not match {1}", _t.ToString, obj.GetType))
                End If
                Return MakeFilter(schema, schema.GetObjectSchema(_t), o)
            Else
                Dim v As Object = obj.GetValue(Nothing, New ColumnAttribute(_fieldname), oschema)

                Return CreateEntityFilter(_t, _fieldname, New ScalarValue(v), Operation)
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
            Return Equals(TryCast(obj, OrmFilterTemplateBase))
        End Function

        Public Overloads Function Equals(ByVal obj As OrmFilterTemplateBase) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Return _t Is obj._t AndAlso _fieldname Is obj._fieldname AndAlso Operation = obj.Operation
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return GetStaticString.GetHashCode
        End Function

        Public Overrides Function GetStaticString() As String
            Return _t.ToString & _fieldname & OperToString()
        End Function

        Public Function MakeHash(ByVal schema As ObjectMappingEngine, ByVal oschema As IObjectSchemaBase, ByVal obj As ICachedEntity) As String Implements IOrmFilterTemplate.MakeHash
            If Operation = FilterOperation.Equal Then
                Return MakeFilter(schema, oschema, obj).MakeHash
            Else
                Return EntityFilterBase.EmptyHash
            End If
        End Function

        Protected MustOverride Function CreateEntityFilter(ByVal t As Type, ByVal fieldName As String, ByVal value As IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation) As EntityFilterBase

        'Public MustOverride ReadOnly Property Operation() As FilterOperation Implements IOrmFilterTemplate.Operation
        'Public MustOverride ReadOnly Property OperToString() As String Implements ITemplate.OperToString
        'Public MustOverride ReadOnly Property OperToStmt() As String Implements ITemplate.OperToStmt
    End Class

    Public MustInherit Class CustomFilterBase
        Inherits Worm.Criteria.Core.FilterBase
        'Implements IFilter

        'Private _t As Type
        'Private _tbl As SourceFragment
        'Private _field As String
        Private _format As String
        Private _oper As Worm.Criteria.FilterOperation
        Public ReadOnly Property Operation() As Worm.Criteria.FilterOperation
            Get
                Return _oper
            End Get
        End Property

        Private _str As String
        Private _sstr As String
        Private _values() As Pair(Of Object, String)

        Public Sub New(ByVal format As String, ByVal value As IFilterValue, ByVal oper As Worm.Criteria.FilterOperation, ByVal ParamArray values() As Pair(Of Object, String))
            MyBase.New(value)
            '_t = table
            '_field = field
            _format = format
            _oper = oper
            _values = values
        End Sub

        Protected Sub New(ByVal value As IFilterValue)
            MyBase.New(value)
        End Sub

        'Public Sub New(ByVal value As IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation, ByVal values() As Object)
        '    MyClass.New("{0}.{1}", value, oper, values)
        'End Sub

        'Public Sub New(ByVal table As SourceFragment, ByVal field As String, ByVal format As String, ByVal value As IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation)
        '    MyBase.New(value)
        '    _tbl = table
        '    _field = field
        '    _format = format
        '    _oper = oper
        'End Sub

        'Public Sub New(ByVal table As SourceFragment, ByVal field As String, ByVal value As IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation)
        '    MyClass.New(table, field, "{0}.{1}", value, oper)
        'End Sub

        Protected Overrides Function _ToString() As String
            If String.IsNullOrEmpty(_str) Then
                _str = Value._ToString & OperationString
            End If
            Return _str
        End Function

        Public Overrides Function GetAllFilters() As System.Collections.Generic.ICollection(Of Worm.Criteria.Core.IFilter)
            Return New CustomFilterBase() {Me}
        End Function

        Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String
            Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = almgr.Aliases

            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            Dim pf As IParamFilterValue = TryCast(Value, IParamFilterValue)

            If pf Is Nothing OrElse pf.ShouldUse Then
                Dim values As List(Of String) = ObjectMappingEngine.ExtractValues(schema, tableAliases, _values)

                Return String.Format(_format, values.ToArray) & OperationString & GetParam(schema, pname)
            Else
                Return String.Empty
            End If
        End Function

        Public Overrides Function ToStaticString() As String
            'Dim o As Object = _t
            'If o Is Nothing Then
            '    o = _tbl.TableName
            'End If
            'Return String.Format(_format, o.ToString, _field) & TemplateBase.Oper2String(_oper)
            If String.IsNullOrEmpty(_sstr) Then
                Dim values As New List(Of String)
                For Each p As Pair(Of Object, String) In _values
                    If p.First Is Nothing Then
                        values.Add(p.Second)
                    Else
                        values.Add(p.First.ToString & "^" & p.Second)
                    End If
                Next
                _sstr = String.Format(_format, values.ToArray) & OperationString
            End If
            Return _sstr
        End Function

        Protected MustOverride ReadOnly Property OperationString() As String

        Protected Sub CopyTo(ByVal obj As CustomFilterBase)
            With obj
                ._format = _format
                ._oper = _oper
                ._sstr = _sstr
                ._str = _str
                ._values = _values
            End With
        End Sub

        'Public Overloads Function MakeSQLStmt(ByVal schema As DbSchema, ByVal almgr As AliasMgr, ByVal pname As Orm.Meta.ICreateParam) As String Implements IFilter.MakeSQLStmt
        '    Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = almgr.Aliases

        '    If schema Is Nothing Then
        '        Throw New ArgumentNullException("schema")
        '    End If

        '    Dim values As List(Of String) = Worm.Sorting.Sort.ExtractValues(schema, tableAliases, _values)

        '    Return String.Format(_format, values.ToArray) & TemplateBase.Oper2String(_oper) & GetParam(schema, pname)
        'End Function

    End Class

    Public MustInherit Class ExpFilter
        Implements IFilter

        Private _fo As FilterOperation
        Private _left As UnaryExp
        Private _right As UnaryExp

        Public Sub New(ByVal left As UnaryExp, ByVal right As UnaryExp, ByVal fo As FilterOperation)
            _left = left
            _right = right
            _fo = fo
        End Sub

        Public ReadOnly Property Left() As UnaryExp
            Get
                Return _left
            End Get
        End Property

        Public ReadOnly Property Right() As UnaryExp
            Get
                Return _right
            End Get
        End Property

        Public ReadOnly Property Operation() As FilterOperation
            Get
                Return _fo
            End Get
        End Property

        Protected MustOverride Function _Clone() As Object Implements System.ICloneable.Clone
        Public MustOverride Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String Implements IFilter.MakeQueryStmt

        Public Function Clone() As IFilter Implements IFilter.Clone
            Return CType(_Clone(), IFilter)
        End Function

        Public Overloads Function Equals(ByVal f As IFilter) As Boolean Implements IFilter.Equals
            If f Is Nothing Then
                Return False
            Else
                Return _ToString.Equals(f.ToString)
            End If
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return ToString.GetHashCode
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, ExpFilter))
        End Function

        Public Function GetAllFilters() As System.Collections.Generic.ICollection(Of IFilter) Implements IFilter.GetAllFilters
            Return New IFilter() {Me}
        End Function

        'Public Function MakeQueryStmt(ByVal schema As QueryGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam) As String Implements IFilter.MakeQueryStmt
        '    'Dim columns As List(Of String)
        '    'Return _left.MakeStmt(schema, pname, almgr, columns)
        '    Throw New NotSupportedException("Use MakeQueryStmt with columns parameter")
        'End Function

        Public Function ReplaceFilter(ByVal replacement As IFilter, ByVal replacer As IFilter) As IFilter Implements IFilter.ReplaceFilter
            If Equals(replacement) Then
                Return replacer
            End If
            Return Nothing
        End Function

        Public Function ToStaticString() As String Implements IFilter.ToStaticString
            Return _left.ToStaticString & _fo.ToString & _right.ToStaticString
        End Function

        Protected Function _ToString() As String Implements IFilter.ToString
            Return _left.ToString & _fo.ToString & _right.ToString
        End Function

        Public Overrides Function ToString() As String
            Return _ToString()
        End Function

        Public ReadOnly Property Filter() As IFilter Implements IGetFilter.Filter
            Get
                Return Me
            End Get
        End Property

        Public ReadOnly Property Filter(ByVal t As System.Type) As IFilter Implements IGetFilter.Filter
            Get
                Return Me
            End Get
        End Property
    End Class
End Namespace
