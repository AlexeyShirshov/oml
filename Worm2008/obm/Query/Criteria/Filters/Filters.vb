Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Values
Imports Worm.Entities
Imports Worm.Expressions

Namespace Criteria.Core

    Public Class EntityFilter
        Inherits TemplatedFilterBase
        Implements IEntityFilter

        'Private _templ As OrmFilterTemplate
        Private _str As String
        Protected _oschema As IObjectSchemaBase
        Private _prep As Boolean = True

        Public Const EmptyHash As String = "fd_empty_hash_aldf"

        Public Sub New(ByVal value As IFilterValue, ByVal tmp As OrmFilterTemplate)
            MyBase.New(value, tmp)
        End Sub

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal value As IFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
            MyBase.New(value, New OrmFilterTemplate(t, propertyAlias, operation))
        End Sub

        Public Sub New(ByVal entityName As String, ByVal propertyAlias As String, ByVal value As IFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
            MyBase.New(value, New OrmFilterTemplate(entityName, propertyAlias, operation))
        End Sub

        Public Sub New(ByVal [alias] As ObjectAlias, ByVal propertyAlias As String, ByVal value As IFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
            MyBase.New(value, New OrmFilterTemplate([alias], propertyAlias, operation))
        End Sub

        Public Sub New(ByVal os As ObjectSource, ByVal propertyAlias As String, ByVal value As IFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
            MyBase.New(value, New OrmFilterTemplate(os, propertyAlias, operation))
        End Sub

        'Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal value As Values.IDatabaseFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
        '    MyBase.New(value, New OrmFilterTemplate(t, fieldName, operation))
        '    _dbFilter = True
        'End Sub

        Protected Overrides Function _ToString() As String
            If _str Is Nothing Then
                _str = Val._ToString & Template.GetStaticString
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

        Public Function Eval(ByVal schema As ObjectMappingEngine, ByVal obj As _IEntity, ByVal oschema As IObjectSchemaBase) As IEvaluableValue.EvalResult Implements IEntityFilter.Eval
            Dim evval As IEvaluableValue = TryCast(Val(), IEvaluableValue)
            If evval IsNot Nothing Then
                If schema Is Nothing Then
                    Throw New ArgumentNullException("schema")
                End If

                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                Dim t As Type = obj.GetType
                Dim rt As Type = Template.ObjectSource.GetRealType(schema)
                If rt Is t Then
                    Dim r As IEvaluableValue.EvalResult = IEvaluableValue.EvalResult.NotFound
                    Dim v As Object = obj.GetValueOptimized(Nothing, Template.PropertyAlias, oschema) 'schema.GetFieldValue(obj, _fieldname)
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
                    Dim o As IKeyEntity = schema.GetJoinObj(oschema, obj, rt)
                    If o IsNot Nothing Then
                        Return Eval(schema, o, schema.GetObjectSchema(rt))
                    End If
                End If
            End If

            Return IEvaluableValue.EvalResult.Unknown
        End Function

        Protected Overloads Function GetParam(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal filterInfo As Object, ByVal pmgr As ICreateParam, ByVal almgr As IPrepareTable, ByVal inSelect As Boolean) As String
            'If _dbFilter Then
            Return Value.GetParam(schema, stmt, pmgr, almgr, AddressOf PrepareValue, Nothing, filterInfo, inSelect)
            'Else
            'Throw New InvalidOperationException
            'End If
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

        Public Function PrepareValue(ByVal schema As ObjectMappingEngine, ByVal v As Object) As Object 'Implements IEntityFilter.PrepareValue
            Return schema.ChangeValueType(_oschema, New ColumnAttribute(Template.PropertyAlias), v)
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

        'Public MustOverride Overloads Function MakeQueryStmt(ByVal oschema As IObjectSchemaBase, ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String

        Public Overloads Function MakeQueryStmt(ByVal oschema As IObjectSchemaBase, ByVal stmt As StmtGenerator, ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam, ByVal columns As List(Of String)) As String
            If _oschema Is Nothing Then
                _oschema = oschema
            End If

            Dim pv As IParamFilterValue = TryCast(Value, IParamFilterValue)

            If pv Is Nothing OrElse pv.ShouldUse Then
                'Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = Nothing

                'If almgr IsNot Nothing Then
                '    tableAliases = almgr.Aliases
                'End If

                If schema Is Nothing Then
                    Throw New ArgumentNullException("schema")
                End If

                Dim map As MapField2Column = Nothing
                Try
                    map = oschema.GetFieldColumnMap()(Template.PropertyAlias)
                Catch ex As KeyNotFoundException
                    Throw New ObjectMappingException(String.Format("There is not column for property {0} ", Template.ObjectSource.ToStaticString & "." & Template.PropertyAlias, ex))
                End Try

                Dim [alias] As String = String.Empty

                If almgr IsNot Nothing Then
                    'Debug.Assert(tableAliases.ContainsKey(map._tableName), "There is not alias for table " & map._tableName.RawName)
                    Try
                        [alias] = almgr.GetAlias(map._tableName, Template.ObjectSource) & stmt.Selector
                    Catch ex As KeyNotFoundException
                        Throw New ObjectMappingException("There is not alias for table " & map._tableName.RawName, ex)
                    End Try
                End If

                'If _dbFilter Then
                '    Return [alias] & map._columnName & Template.OperToStmt & GetParam(CType(schema, SQLGenerator), filterInfo, pname, CType(almgr, AliasMgr))
                'Else
                '    Return [alias] & map._columnName & Template.OperToStmt & GetParam(schema, pname)
                'End If
                Return [alias] & map._columnName & Template.OperToStmt(stmt) & GetParam(schema, stmt, filterInfo, pname, almgr, False)
            Else
                Return String.Empty
            End If
        End Function

        Public Overloads Function MakeQueryStmt(ByVal oschema As IObjectSchemaBase, ByVal stmt As StmtGenerator, ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam) As String
            Return MakeQueryStmt(oschema, stmt, filterInfo, schema, almgr, pname, Nothing)
        End Function

        Public Overridable Overloads Function MakeSingleQueryStmt(ByVal oschema As IObjectSchemaBase, _
            ByVal stmt As StmtGenerator, ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As Pair(Of String)
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

            Dim prname As String = Value.GetParam(schema, stmt, pname, almgr, pd, Nothing, Nothing, False)

            Dim map As MapField2Column = oschema.GetFieldColumnMap()(Template.PropertyAlias)
            Dim rt As Type = Template.ObjectSource.GetRealType(schema)

            Dim v As IEvaluableValue = TryCast(Val(), IEvaluableValue)
            If v IsNot Nothing AndAlso v.Value Is DBNull.Value Then
                If schema.GetPropertyTypeByName(rt, _oschema, Template.PropertyAlias) Is GetType(Byte()) Then
                    pname.GetParameter(prname).DbType = System.Data.DbType.Binary
                ElseIf schema.GetPropertyTypeByName(rt, _oschema, Template.PropertyAlias) Is GetType(Decimal) Then
                    pname.GetParameter(prname).DbType = System.Data.DbType.Decimal
                End If
            End If

            Return New Pair(Of String)(map._columnName, prname)
        End Function

        Public Overrides Function MakeSingleQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As Pair(Of String)
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If _oschema Is Nothing Then
                If Template.ObjectSource.AnyType IsNot Nothing Then
                    _oschema = schema.GetObjectSchema(Template.ObjectSource.AnyType)
                Else
                    _oschema = schema.GetObjectSchema(schema.GetTypeByEntityName(Template.ObjectSource.AnyEntityName))
                End If
            End If

            Return MakeSingleQueryStmt(_oschema, stmt, schema, almgr, pname)
        End Function

        Public Function MakeHash() As String Implements IEntityFilter.MakeHash
            If Template.Operation = FilterOperation.Equal Then
                Return _ToString()
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

        Protected Overrides Function _Clone() As Object
            Return New EntityFilter(Val, CType(Template, OrmFilterTemplate))
        End Function

        Public Overloads Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If stmt Is Nothing Then
                Throw New ArgumentNullException("stmt")
            End If

            If _oschema Is Nothing Then
                'If Template.Type Is Nothing AndAlso Not String.IsNullOrEmpty(Template.EntityName) Then
                '    _oschema = schema.GetObjectSchema(schema.GetTypeByEntityName(Template.EntityName))
                'Else
                '    _oschema = schema.GetObjectSchema(Template.Type)
                'End If
                _oschema = schema.GetObjectSchema(Template.ObjectSource.GetRealType(schema))
            End If

            Return MakeQueryStmt(_oschema, stmt, filterInfo, schema, almgr, pname, columns)
        End Function
    End Class

    Public Class TableFilter
        Inherits Worm.Criteria.Core.TemplatedFilterBase
        Private Const TempTable As String = "calculated"
        'Implements ITemplateFilter

        'Private _templ As TableFilterTemplate

        Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal value As IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
            MyBase.New(value, New TableFilterTemplate(table, column, operation))
            '_templ = New TableFilterTemplate(table, column, operation)
        End Sub

        Public Sub New(ByVal column As String, ByVal value As IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
            MyBase.New(value, New TableFilterTemplate(New SourceFragment(TempTable), column, operation))
            '_templ = New TableFilterTemplate(table, column, operation)
        End Sub

        Protected Sub New(ByVal v As IFilterValue, ByVal template As TemplateBase)
            MyBase.New(v, template)
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

        Public Overloads Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam) As String
            Return MakeQueryStmt(schema, stmt, filterInfo, almgr, pname, Nothing)
        End Function

        Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String
            Dim pf As IParamFilterValue = TryCast(Value, IParamFilterValue)

            If pf Is Nothing OrElse pf.ShouldUse Then
                If Template.Table.Name = TempTable Then
                    Return Template.Column & Template.OperToStmt(stmt) & GetParam(schema, stmt, pname, False)
                Else
                    'Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = Nothing

                    'If almgr IsNot Nothing Then
                    '    tableAliases = almgr.Aliases
                    'End If

                    Dim map As New MapField2Column(String.Empty, Template.Column, Template.Table)
                    Dim [alias] As String = String.Empty

                    If almgr IsNot Nothing Then
                        Debug.Assert(almgr.ContainsKey(map._tableName, Nothing), "There is not alias for table " & map._tableName.RawName)
                        Try
                            [alias] = almgr.GetAlias(map._tableName, Nothing) & stmt.Selector
                        Catch ex As KeyNotFoundException
                            Throw New ObjectMappingException("There is not alias for table " & map._tableName.RawName, ex)
                        End Try
                    End If

                    Return [alias] & map._columnName & Template.OperToStmt(stmt) & GetParam(schema, stmt, pname, False)
                End If
            Else
                Return String.Empty
            End If
        End Function

        Public Overrides Function GetAllFilters() As System.Collections.Generic.ICollection(Of Worm.Criteria.Core.IFilter)
            Return New TableFilter() {Me}
        End Function

        Public Overrides Function MakeSingleQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As Pair(Of String)
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If pname Is Nothing Then
                Throw New ArgumentNullException("pname")
            End If

            Dim prname As String = Value.GetParam(schema, stmt, pname, Nothing, Nothing, Nothing, Nothing, False)

            Return New Pair(Of String)(Template.Column, prname)
        End Function

        'Public Overloads Function MakeSQLStmt(ByVal schema As DbSchema, ByVal almgr As AliasMgr, ByVal pname As ICreateParam) As String Implements IFilter.MakeSQLStmt
        '    Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = almgr.Aliases
        '    Dim map As New MapField2Column(String.Empty, Template.Column, Template.Table)
        '    Dim [alias] As String = String.Empty

        '    If tableAliases IsNot Nothing Then
        '        [alias] = tableAliases(map._tableName) & "."
        '    End If

        '    Return [alias] & map._columnName & Template.OperToStmt & GetParam(schema, pname)
        'End Function

        Protected Overrides Function _Clone() As Object
            Return New TableFilter(val, Template)
        End Function
    End Class

    Public Class CustomFilter
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
            Return New CustomFilter() {Me}
        End Function

        Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String
            'Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = almgr.Aliases

            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            Dim pf As IParamFilterValue = TryCast(Value, IParamFilterValue)

            If pf Is Nothing OrElse pf.ShouldUse Then
                Dim values As List(Of String) = ObjectMappingEngine.ExtractValues(schema, stmt, almgr, _values)

                Return String.Format(_format, values.ToArray) & stmt.Oper2String(_oper) & GetParam(schema, stmt, pname, False)
            Else
                Return String.Empty
            End If
        End Function

        Public Overrides Function ToStaticString(ByVal mpe As ObjectMappingEngine) As String
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

        Protected ReadOnly Property OperationString() As String
            Get
                Return TemplateBase.OperToStringInternal(_oper)
            End Get
        End Property

        Protected Sub CopyTo(ByVal obj As CustomFilter)
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

        Protected Overrides Function _Clone() As Object
            Dim c As New CustomFilter(Value)
            CopyTo(c)
            Return c
        End Function
    End Class

    Public Class ExpressionFilter
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

        Protected Function _Clone() As Object Implements System.ICloneable.Clone
            Return New ExpressionFilter(Left, Right, Operation)
        End Function

        Public Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String Implements IFilter.MakeQueryStmt
            Return Left.MakeStmt(schema, stmt, pname, almgr, columns, filterInfo, False) & stmt.Oper2String(Operation) & Right.MakeStmt(schema, stmt, pname, almgr, columns, filterInfo, False)
        End Function

        Public Function Clone() As IFilter Implements IFilter.Clone
            Return CType(_Clone(), IFilter)
        End Function

        Public Overloads Function Equals(ByVal f As IFilter) As Boolean Implements IFilter.Equals
            If f Is Nothing Then
                Return False
            Else
                Return _ToString.Equals(f._ToString)
            End If
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return ToString.GetHashCode
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, ExpressionFilter))
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

        Public Function ToStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IFilter.GetStaticString
            Return _left.ToStaticString(mpe) & _fo.ToString & _right.ToStaticString(mpe)
        End Function

        Protected Function _ToString() As String Implements IFilter._ToString
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

        'Public ReadOnly Property Filter(ByVal t As System.Type) As IFilter Implements IGetFilter.Filter
        '    Get
        '        Return Me
        '    End Get
        'End Property
    End Class

    Public Class NonTemplateUnaryFilter
        Inherits Worm.Criteria.Core.FilterBase
        Implements Cache.IQueryDependentTypes

        Private _oper As Worm.Criteria.FilterOperation
        Private _str As String

        Public Sub New(ByVal value As IFilterValue, ByVal oper As Worm.Criteria.FilterOperation)
            MyBase.New(value)
            _oper = oper
        End Sub

        Protected Overrides Function _ToString() As String
            If String.IsNullOrEmpty(_str) Then
                _str = val._ToString & Worm.Criteria.Core.TemplateBase.OperToStringInternal(_oper)
            End If
            Return _str
        End Function

        Public Overrides Function GetAllFilters() As System.Collections.Generic.ICollection(Of Worm.Criteria.Core.IFilter)
            Return New NonTemplateUnaryFilter() {Me}
        End Function

        Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String
            'Return TemplateBase.Oper2String(_oper) & GetParam(schema, pname)
            'Dim id As Values.IDatabaseFilterValue = TryCast(val, Values.IDatabaseFilterValue)
            'If id IsNot Nothing Then
            Return stmt.Oper2String(_oper) & Value.GetParam(schema, stmt, pname, almgr, Nothing, columns, filterInfo, False)
            'Else
            'Return MakeQueryStmt(schema, filterInfo, almgr, pname, columns)
            'End If
        End Function

        Public Overrides Function ToStaticString(ByVal mpe As ObjectMappingEngine) As String
            Dim v As INonTemplateValue = TryCast(val, INonTemplateValue)
            If v Is Nothing Then
                Throw New NotImplementedException("Value is not implement INonTemplateValue")
            End If
            Return v.GetStaticString(mpe) & "$" & Worm.Criteria.Core.TemplateBase.OperToStringInternal(_oper)
        End Function

        Protected Overrides Function _Clone() As Object
            Return New NonTemplateUnaryFilter(Value, _oper)
        End Function

        'Public Overloads Function MakeSQLStmt1(ByVal schema As DbSchema, ByVal almgr As AliasMgr, ByVal pname As Orm.Meta.ICreateParam) As String Implements IFilter.MakeSQLStmt
        '    Dim id As Values.IDatabaseFilterValue = TryCast(val, Values.IDatabaseFilterValue)
        '    If id IsNot Nothing Then
        '        Return TemplateBase.Oper2String(_oper) & id.GetParam(schema, pname, almgr)
        '    Else
        '        Return MakeQueryStmt(schema, almgr, pname)
        '    End If
        'End Function

        Public Function [Get](ByVal mpe As ObjectMappingEngine) As Cache.IDependentTypes Implements Cache.IQueryDependentTypes.Get
            Return Cache.QueryDependentTypes(mpe, Value)
        End Function
    End Class

End Namespace
