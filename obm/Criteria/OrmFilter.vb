Imports System.Collections.Generic
Imports Worm.Orm.Meta
Imports Worm.Criteria.Values
Imports Worm.Orm

Namespace Criteria.Core

    Public Interface IFilter
        Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal pname As ICreateParam) As String
        Function GetAllFilters() As ICollection(Of IFilter)
        Function Equals(ByVal f As IFilter) As Boolean
        Function ReplaceFilter(ByVal replacement As IFilter, ByVal replacer As IFilter) As IFilter
        Function ToString() As String
        Function ToStaticString() As String
    End Interface

    Public Interface ITemplateFilterBase
        'Function ReplaceByTemplate(ByVal replacement As ITemplateFilter, ByVal replacer As ITemplateFilter) As ITemplateFilter
        ReadOnly Property Template() As ITemplate
        Function MakeSingleStmt(ByVal schema As OrmSchemaBase, ByVal pname As ICreateParam) As Pair(Of String)
    End Interface

    Public Interface ITemplateFilter
        Inherits IFilter, ITemplateFilterBase
        'Function GetStaticString() As String
    End Interface

    Public Interface IValuableFilter
        ReadOnly Property Value() As IParamFilterValue
    End Interface

    Public Interface IEntityFilterBase
        Function Eval(ByVal schema As OrmSchemaBase, ByVal obj As OrmBase, ByVal oschema As IOrmObjectSchemaBase) As IEvaluableValue.EvalResult
        Function GetFilterTemplate() As IOrmFilterTemplate
        Function PrepareValue(ByVal schema As OrmSchemaBase, ByVal v As Object) As Object
        Function MakeHash() As String
    End Interface

    Public Interface IEntityFilter
        Inherits ITemplateFilter, IEntityFilterBase

    End Interface

    Public Interface ITemplate
        ReadOnly Property Operation() As FilterOperation
        ReadOnly Property OperToString() As String
        ReadOnly Property OperToStmt() As String
        Function GetStaticString() As String
    End Interface

    Public Interface IOrmFilterTemplate
        Inherits ITemplate
        Function MakeHash(ByVal schema As OrmSchemaBase, ByVal oschema As IOrmObjectSchemaBase, ByVal obj As OrmBase) As String
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
        Public MustOverride Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal pname As ICreateParam) As String Implements IFilter.MakeSQLStmt
        Public MustOverride Function GetAllFilters() As System.Collections.Generic.ICollection(Of IFilter) Implements IFilter.GetAllFilters
        Public MustOverride Function ToStaticString() As String Implements IFilter.ToStaticString

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

        Public ReadOnly Property ParamValue() As IParamFilterValue Implements IValuableFilter.Value
            Get
                Return CType(_v, IParamFilterValue)
            End Get
        End Property

        Public ReadOnly Property Value() As IFilterValue
            Get
                Return CType(_v, IFilterValue)
            End Get
        End Property

        'Protected Sub SetValue(ByVal v As IFilterValue)
        '    _v = v
        'End Sub

        Protected Overridable Function GetParam(ByVal schema As OrmSchemaBase, ByVal pmgr As ICreateParam) As String
            If _v Is Nothing Then
                'Return pmgr.CreateParam(Nothing)
                Throw New InvalidOperationException("Param is null")
            End If
            Return ParamValue.GetParam(schema, pmgr, TryCast(Me, IEntityFilter))
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

        Public MustOverride Function MakeSingleStmt(ByVal schema As OrmSchemaBase, ByVal pname As ICreateParam) As Pair(Of String) Implements ITemplateFilter.MakeSingleStmt

        Public Overrides Function ToStaticString() As String
            Return _templ.GetStaticString
        End Function

        Public ReadOnly Property Template() As ITemplate Implements ITemplateFilterBase.Template
            Get
                Return _templ
            End Get
        End Property
    End Class

    Public MustInherit Class EntityFilter
        Inherits TemplatedFilterBase
        Implements IEntityFilter

        'Private _templ As OrmFilterTemplate
        Private _str As String
        Protected _oschema As IOrmObjectSchemaBase

        Public Const EmptyHash As String = "fd_empty_hash_aldf"

        Public Sub New(ByVal tmp As OrmFilterTemplate, ByVal value As IFilterValue)
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

        Public Shadows ReadOnly Property Template() As OrmFilterTemplate
            Get
                Return CType(MyBase.Template, OrmFilterTemplate)
            End Get
        End Property

        Public Function Eval(ByVal schema As OrmSchemaBase, ByVal obj As OrmBase, ByVal oschema As IOrmObjectSchemaBase) As IEvaluableValue.EvalResult Implements IEntityFilter.Eval
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
                    Dim v As Object = obj.GetValue(Template.FieldName, oschema) 'schema.GetFieldValue(obj, _fieldname)
                    If v IsNot Nothing Then
                        r = evval.Eval(v, Template)
                    Else
                        If evval.Value Is Nothing Then
                            r = IEvaluableValue.EvalResult.Found
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

            Return IEvaluableValue.EvalResult.Unknown
        End Function

        Public Overrides Function GetAllFilters() As System.Collections.Generic.ICollection(Of IFilter)
            Return New EntityFilter() {Me}
        End Function

        Public Function GetFilterTemplate() As IOrmFilterTemplate Implements IEntityFilter.GetFilterTemplate
            'If TryCast(Value, IEvaluableValue) IsNot Nothing Then
            Return Template
            'End If
            'Return Nothing
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

        Public Overrides Function MakeSingleStmt(ByVal schema As OrmSchemaBase, ByVal pname As ICreateParam) As Pair(Of String)
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If pname Is Nothing Then
                Throw New ArgumentNullException("pname")
            End If

            If _oschema Is Nothing Then
                _oschema = schema.GetObjectSchema(Template.Type)
            End If

            Dim prname As String = ParamValue.GetParam(schema, pname, Me)

            Dim map As MapField2Column = _oschema.GetFieldColumnMap()(Template.FieldName)

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

        Public Function MakeHash() As String Implements IEntityFilter.MakeHash
            If Template.Operation = FilterOperation.Equal Then
                Return ToString()
            Else
                Return EmptyHash
            End If
        End Function
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
                    Throw New DBSchemaException("Operation " & oper & " not supported")
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

    Public MustInherit Class OrmFilterTemplate
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

        Public Overridable Function MakeFilter(ByVal schema As OrmSchemaBase, ByVal oschema As IOrmObjectSchemaBase, ByVal obj As OrmBase) As IEntityFilter 'Implements IOrmFilterTemplate.MakeFilter
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
            Return _t.ToString & _fieldname & OperToString()
        End Function

        Public Function MakeHash(ByVal schema As OrmSchemaBase, ByVal oschema As IOrmObjectSchemaBase, ByVal obj As OrmBase) As String Implements IOrmFilterTemplate.MakeHash
            If Operation = FilterOperation.Equal Then
                Return MakeFilter(schema, oschema, obj).ToString
            Else
                Return EntityFilter.EmptyHash
            End If
        End Function

        Protected MustOverride Function CreateEntityFilter(ByVal t As Type, ByVal fieldName As String, ByVal value As IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation) As EntityFilter

        'Public MustOverride ReadOnly Property Operation() As FilterOperation Implements IOrmFilterTemplate.Operation
        'Public MustOverride ReadOnly Property OperToString() As String Implements ITemplate.OperToString
        'Public MustOverride ReadOnly Property OperToStmt() As String Implements ITemplate.OperToStmt
    End Class

End Namespace

Namespace Database

    Namespace Criteria.Core
        Public Interface IFilter
            Inherits Worm.Criteria.Core.IFilter
            Overloads Function MakeSQLStmt(ByVal schema As DbSchema, ByVal almgr As AliasMgr, ByVal pname As ICreateParam) As String
        End Interface

        Public Interface ITemplateFilter
            Inherits IFilter, Worm.Criteria.Core.ITemplateFilterBase
            'Function GetStaticString() As String
        End Interface

        Public Interface IEntityFilter
            Inherits ITemplateFilter, Worm.Criteria.Core.IEntityFilterBase

        End Interface

        Public MustInherit Class TemplateBase
            Inherits Worm.Criteria.Core.TemplateBase

            Public Sub New()
            End Sub

            Public Sub New(ByVal operation As Worm.Criteria.FilterOperation)
                MyBase.new(operation)
            End Sub

            Protected Friend Shared Function Oper2String(ByVal oper As Worm.Criteria.FilterOperation) As String
                Select Case oper
                    Case Worm.Criteria.FilterOperation.Equal
                        Return " = "
                    Case Worm.Criteria.FilterOperation.GreaterEqualThan
                        Return " >= "
                    Case Worm.Criteria.FilterOperation.GreaterThan
                        Return " > "
                    Case Worm.Criteria.FilterOperation.In
                        Return " in "
                    Case Worm.Criteria.FilterOperation.NotEqual
                        Return " <> "
                    Case Worm.Criteria.FilterOperation.NotIn
                        Return " not in "
                    Case Worm.Criteria.FilterOperation.LessEqualThan
                        Return " <= "
                    Case Worm.Criteria.FilterOperation.Like
                        Return " like "
                    Case Worm.Criteria.FilterOperation.LessThan
                        Return " < "
                    Case Worm.Criteria.FilterOperation.Is
                        Return " is "
                    Case Worm.Criteria.FilterOperation.IsNot
                        Return " is not "
                    Case Worm.Criteria.FilterOperation.Exists
                        Return " exists "
                    Case Worm.Criteria.FilterOperation.NotExists
                        Return " not exists "
                    Case Worm.Criteria.FilterOperation.Between
                        Return " between "
                    Case Else
                        Throw New DBSchemaException("invalid opration " & oper.ToString)
                End Select
            End Function

            Public Overrides Function ToString() As String
                Return GetStaticString()
            End Function

            Public Overrides ReadOnly Property OperToStmt() As String
                Get
                    Return Oper2String(Operation)
                End Get
            End Property
        End Class

        Public Class TableFilterTemplate
            Inherits TemplateBase

            Private _tbl As OrmTable
            Private _col As String

            Public Sub New(ByVal table As OrmTable, ByVal column As String, ByVal operation As Worm.Criteria.FilterOperation)
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
                Return _tbl.RawName() & _col & OperToStmt()
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

        Public Class NonTemplateFilter
            Inherits Worm.Criteria.Core.FilterBase
            Implements IFilter

            Private _oper As Worm.Criteria.FilterOperation
            Private _str As String

            Public Sub New(ByVal value As Values.IDatabaseFilterValue, ByVal oper As Worm.Criteria.FilterOperation)
                MyBase.New(value)
                _oper = oper
            End Sub

            Protected Overrides Function _ToString() As String
                If String.IsNullOrEmpty(_str) Then
                    _str = val._ToString & TemplateBase.Oper2String(_oper)
                End If
                Return _str
            End Function

            Public Overrides Function GetAllFilters() As System.Collections.Generic.ICollection(Of Worm.Criteria.Core.IFilter)
                Return New NonTemplateFilter() {Me}
            End Function

            Public Overrides Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal pname As ICreateParam) As String
                Return TemplateBase.Oper2String(_oper) & GetParam(schema, pname)
            End Function

            Public Overrides Function ToStaticString() As String
                Dim v As INonTemplateValue = TryCast(val, INonTemplateValue)
                If v Is Nothing Then
                    Throw New NotImplementedException("Value is not implement INonTemplateValue")
                End If
                Return v.GetStaticString & "$" & TemplateBase.Oper2String(_oper)
            End Function

            Public Overloads Function MakeSQLStmt1(ByVal schema As DbSchema, ByVal almgr As AliasMgr, ByVal pname As Orm.Meta.ICreateParam) As String Implements IFilter.MakeSQLStmt
                Dim id As Values.IDatabaseFilterValue = TryCast(val, Values.IDatabaseFilterValue)
                If id IsNot Nothing Then
                    Return TemplateBase.Oper2String(_oper) & id.GetParam(schema, pname, almgr)
                Else
                    Return MakeSQLStmt(schema, pname)
                End If
            End Function
        End Class

        Public Class TableFilter
            Inherits Worm.Criteria.Core.TemplatedFilterBase
            Implements ITemplateFilter

            'Private _templ As TableFilterTemplate

            Public Sub New(ByVal table As OrmTable, ByVal column As String, ByVal value As IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
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

            Public Overrides Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal pname As ICreateParam) As String
                'Dim tableAliases As System.Collections.Generic.IDictionary(Of OrmTable, String) = almgr.Aliases
                'Dim map As New MapField2Column(String.Empty, Template.Column, Template.Table)
                'Dim [alias] As String = String.Empty

                'If tableAliases IsNot Nothing Then
                '    [alias] = tableAliases(map._tableName) & "."
                'End If

                'Return [alias] & map._columnName & Template.Oper2String & GetParam(schema, pname, almgr)
                Throw New NotSupportedException
            End Function

            Public Overrides Function GetAllFilters() As System.Collections.Generic.ICollection(Of Worm.Criteria.Core.IFilter)
                Return New TableFilter() {Me}
            End Function

            Public Overrides Function MakeSingleStmt(ByVal schema As OrmSchemaBase, ByVal pname As ICreateParam) As Pair(Of String)
                If schema Is Nothing Then
                    Throw New ArgumentNullException("schema")
                End If

                If pname Is Nothing Then
                    Throw New ArgumentNullException("pname")
                End If

                Dim prname As String = ParamValue.GetParam(schema, pname, Nothing)

                Return New Pair(Of String)(Template.Column, prname)
            End Function

            Public Overloads Function MakeSQLStmt(ByVal schema As DbSchema, ByVal almgr As AliasMgr, ByVal pname As ICreateParam) As String Implements IFilter.MakeSQLStmt
                Dim tableAliases As System.Collections.Generic.IDictionary(Of OrmTable, String) = almgr.Aliases
                Dim map As New MapField2Column(String.Empty, Template.Column, Template.Table)
                Dim [alias] As String = String.Empty

                If tableAliases IsNot Nothing Then
                    [alias] = tableAliases(map._tableName) & "."
                End If

                Return [alias] & map._columnName & Template.OperToStmt & GetParam(schema, pname)
            End Function
        End Class

        Public Class OrmFilterTemplate
            Inherits Worm.Criteria.Core.OrmFilterTemplate

            Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal oper As Worm.Criteria.FilterOperation)
                MyBase.New(t, fieldName, oper)
            End Sub

            Protected Overrides Function CreateEntityFilter(ByVal t As System.Type, ByVal fieldName As String, ByVal value As Worm.Criteria.Values.IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation) As Worm.Criteria.Core.EntityFilter
                Return New EntityFilter(t, fieldName, value, operation)
            End Function

            Public Overrides ReadOnly Property OperToStmt() As String
                Get
                    Return TemplateBase.Oper2String(Operation)
                End Get
            End Property
        End Class

        Public Class EntityFilter
            Inherits Worm.Criteria.Core.EntityFilter
            Implements IEntityFilter

            Private _dbFilter As Boolean

            Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal value As IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
                MyBase.New(New OrmFilterTemplate(t, fieldName, operation), value)
            End Sub

            Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal value As Values.IDatabaseFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
                MyBase.New(New OrmFilterTemplate(t, fieldName, operation), value)
                _dbFilter = True
            End Sub

            Public Overloads Function MakeSQLStmt(ByVal schema As DbSchema, ByVal almgr As AliasMgr, ByVal pname As ICreateParam) As String Implements IFilter.MakeSQLStmt
                Dim tableAliases As System.Collections.Generic.IDictionary(Of OrmTable, String) = almgr.Aliases

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

                If _dbFilter Then
                    Return [alias] & map._columnName & Template.OperToStmt & GetParam(schema, pname, almgr)
                Else
                    Return [alias] & map._columnName & Template.OperToStmt & GetParam(schema, pname)
                End If
            End Function

            Public Overloads Overrides Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal pname As Orm.Meta.ICreateParam) As String
                Throw New NotSupportedException
            End Function

            Protected Overrides Function GetParam(ByVal schema As OrmSchemaBase, ByVal pmgr As ICreateParam) As String
                If _dbFilter Then
                    Throw New InvalidOperationException
                Else
                    Return MyBase.GetParam(schema, pmgr)
                End If
            End Function

            Protected Overloads Function GetParam(ByVal schema As DbSchema, ByVal pmgr As ICreateParam, ByVal almgr As AliasMgr) As String
                If _dbFilter Then
                    Return CType(val, Values.IDatabaseFilterValue).GetParam(schema, pmgr, almgr)
                Else
                    Throw New InvalidOperationException
                End If
            End Function
        End Class

        Public Class CustomFilter
            Inherits Worm.Criteria.Core.FilterBase
            Implements IFilter

            'Private _t As Type
            'Private _tbl As OrmTable
            'Private _field As String
            Private _format As String
            Private _oper As Worm.Criteria.FilterOperation
            Private _str As String
            Private _sstr As String
            Private _values() As Pair(Of Object, String)

            Public Sub New(ByVal format As String, ByVal value As IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation, ByVal ParamArray values() As Pair(Of Object, String))
                MyBase.New(value)
                '_t = table
                '_field = field
                _format = format
                _oper = oper
                _values = values
            End Sub

            'Public Sub New(ByVal value As IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation, ByVal values() As Object)
            '    MyClass.New("{0}.{1}", value, oper, values)
            'End Sub

            'Public Sub New(ByVal table As OrmTable, ByVal field As String, ByVal format As String, ByVal value As IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation)
            '    MyBase.New(value)
            '    _tbl = table
            '    _field = field
            '    _format = format
            '    _oper = oper
            'End Sub

            'Public Sub New(ByVal table As OrmTable, ByVal field As String, ByVal value As IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation)
            '    MyClass.New(table, field, "{0}.{1}", value, oper)
            'End Sub

            Protected Overrides Function _ToString() As String
                If String.IsNullOrEmpty(_str) Then
                    _str = Value._ToString & TemplateBase.Oper2String(_oper)
                End If
                Return _str
            End Function

            Public Overrides Function GetAllFilters() As System.Collections.Generic.ICollection(Of Worm.Criteria.Core.IFilter)
                Return New CustomFilter() {Me}
            End Function

            Public Overrides Function MakeSQLStmt(ByVal schema As OrmSchemaBase, ByVal pname As ICreateParam) As String
                Throw New NotSupportedException
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
                            values.Add(p.First.ToString & "." & p.Second)
                        End If
                    Next
                    _sstr = String.Format(_format, values.ToArray) & TemplateBase.Oper2String(_oper)
                End If
                Return _sstr
            End Function

            Public Overloads Function MakeSQLStmt(ByVal schema As DbSchema, ByVal almgr As AliasMgr, ByVal pname As Orm.Meta.ICreateParam) As String Implements IFilter.MakeSQLStmt
                Dim tableAliases As System.Collections.Generic.IDictionary(Of OrmTable, String) = almgr.Aliases

                If schema Is Nothing Then
                    Throw New ArgumentNullException("schema")
                End If

                Dim [alias] As String = String.Empty
                Dim values As New List(Of String)
                Dim lastt As Type = Nothing
                For Each p As Pair(Of Object, String) In _values
                    Dim o As Object = p.First
                    If o Is Nothing Then
                        Throw New NullReferenceException
                    End If

                    If TypeOf o Is Type Then
                        Dim t As Type = CType(o, Type)
                        If Not GetType(OrmBase).IsAssignableFrom(t) Then
                            Throw New NotSupportedException(String.Format("Type {0} is not assignable from OrmBase", t))
                        End If
                        lastt = t

                        Dim oschema As IOrmObjectSchema = CType(schema.GetObjectSchema(t), IOrmObjectSchema)
                        Dim tbl As OrmTable = Nothing
                        Dim map As MapField2Column = Nothing
                        Dim fld As String = p.Second
                        If oschema.GetFieldColumnMap.TryGetValue(fld, map) Then
                            fld = map._columnName
                            tbl = map._tableName
                        Else
                            tbl = oschema.GetTables(0)
                        End If

                        If tableAliases IsNot Nothing Then
                            [alias] = tableAliases(tbl)
                        End If
                        If Not String.IsNullOrEmpty([alias]) Then
                            values.Add([alias] & "." & fld)
                        Else
                            values.Add(fld)
                        End If
                    ElseIf TypeOf o Is OrmTable Then
                        Dim tbl As OrmTable = CType(o, OrmTable)
                        If tableAliases IsNot Nothing Then
                            [alias] = tableAliases(tbl)
                        End If
                        If Not String.IsNullOrEmpty([alias]) Then
                            values.Add([alias] & "." & p.Second)
                        Else
                            values.Add(p.Second)
                        End If
                    ElseIf o Is Nothing Then
                        values.Add(p.Second)
                    Else
                        Throw New NotSupportedException(String.Format("Type {0} is not supported", o.GetType))
                    End If
                Next

                Return String.Format(_format, values.ToArray) & TemplateBase.Oper2String(_oper) & GetParam(schema, pname)
            End Function
        End Class

    End Namespace

End Namespace