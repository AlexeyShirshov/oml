Imports Worm.Orm.Meta
Imports Worm.Criteria.Values
Imports System.Collections.Generic
Imports Worm.Expressions

'Imports Worm.Criteria.Core

Namespace Database

    Namespace Criteria.Core
        'Public Interface IFilter
        '    Inherits Worm.Criteria.Core.IFilter
        '    Overloads Function MakeSQLStmt(ByVal schema As DbSchema, ByVal almgr As AliasMgr, ByVal pname As ICreateParam) As String
        'End Interface

        'Public Interface ITemplateFilter
        '    Inherits IFilter, Worm.Criteria.Core.ITemplateFilterBase
        '    'Function GetStaticString() As String
        'End Interface

        'Public Interface IEntityFilter
        '    Inherits ITemplateFilter, Worm.Criteria.Core.IEntityFilterBase

        'End Interface

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
                        Throw New ObjectMappingException("invalid opration " & oper.ToString)
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

            Private _tbl As SourceFragment
            Private _col As String

            Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal operation As Worm.Criteria.FilterOperation)
                MyBase.New(operation)
                _tbl = table
                _col = column
            End Sub

            Public Sub New(ByVal table As SourceFragment, ByVal column As String)
                MyBase.New()
                _tbl = table
                _col = column
            End Sub

            Public ReadOnly Property Table() As SourceFragment
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
                    _str = val._ToString & TemplateBase.Oper2String(_oper)
                End If
                Return _str
            End Function

            Public Overrides Function GetAllFilters() As System.Collections.Generic.ICollection(Of Worm.Criteria.Core.IFilter)
                Return New NonTemplateUnaryFilter() {Me}
            End Function

            Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String
                'Return TemplateBase.Oper2String(_oper) & GetParam(schema, pname)
                'Dim id As Values.IDatabaseFilterValue = TryCast(val, Values.IDatabaseFilterValue)
                'If id IsNot Nothing Then
                Return TemplateBase.Oper2String(_oper) & Value.GetParam(schema, pname, almgr, Nothing, columns, filterInfo)
                'Else
                'Return MakeQueryStmt(schema, filterInfo, almgr, pname, columns)
                'End If
            End Function

            Public Overrides Function ToStaticString(ByVal mpe As ObjectMappingEngine) As String
                Dim v As INonTemplateValue = TryCast(val, INonTemplateValue)
                If v Is Nothing Then
                    Throw New NotImplementedException("Value is not implement INonTemplateValue")
                End If
                Return v.GetStaticString(mpe) & "$" & TemplateBase.Oper2String(_oper)
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

            Public Overloads Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam) As String
                Return MakeQueryStmt(schema, filterInfo, almgr, pname, Nothing)
            End Function

            Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String
                Dim pf As IParamFilterValue = TryCast(Value, IParamFilterValue)

                If pf Is Nothing OrElse pf.ShouldUse Then
                    If Template.Table.Name = TempTable Then
                        Return Template.Column & Template.OperToStmt & GetParam(schema, pname)
                    Else
                        Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = Nothing

                        If almgr IsNot Nothing Then
                            tableAliases = almgr.Aliases
                        End If

                        Dim map As New MapField2Column(String.Empty, Template.Column, Template.Table)
                        Dim [alias] As String = String.Empty

                        If tableAliases IsNot Nothing Then
                            Debug.Assert(tableAliases.ContainsKey(map._tableName), "There is not alias for table " & map._tableName.RawName)
                            Try
                                [alias] = tableAliases(map._tableName) & "."
                            Catch ex As KeyNotFoundException
                                Throw New ObjectMappingException("There is not alias for table " & map._tableName.RawName, ex)
                            End Try
                        End If

                        Return [alias] & map._columnName & Template.OperToStmt & GetParam(schema, pname)
                    End If
                Else
                    Return String.Empty
                End If
            End Function

            Public Overrides Function GetAllFilters() As System.Collections.Generic.ICollection(Of Worm.Criteria.Core.IFilter)
                Return New TableFilter() {Me}
            End Function

            Public Overrides Function MakeSingleQueryStmt(ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As Pair(Of String)
                If schema Is Nothing Then
                    Throw New ArgumentNullException("schema")
                End If

                If pname Is Nothing Then
                    Throw New ArgumentNullException("pname")
                End If

                Dim prname As String = Value.GetParam(schema, pname, Nothing, Nothing, Nothing, Nothing)

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

        Public Class OrmFilterTemplate
            Inherits Worm.Criteria.Core.OrmFilterTemplateBase

            Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal oper As Worm.Criteria.FilterOperation)
                MyBase.New(t, fieldName, oper)
            End Sub

            Protected Overrides Function CreateEntityFilter(ByVal t As System.Type, ByVal fieldName As String, ByVal value As Worm.Criteria.Values.IParamFilterValue, ByVal operation As Worm.Criteria.FilterOperation) As Worm.Criteria.Core.EntityFilterBase
                Return New EntityFilter(t, fieldName, value, operation)
            End Function

            Public Overrides ReadOnly Property OperToStmt() As String
                Get
                    Return TemplateBase.Oper2String(Operation)
                End Get
            End Property
        End Class

        Public Class EntityFilter
            Inherits Worm.Criteria.Core.EntityFilterBase
            'Implements IEntityFilter

            'Private _dbFilter As Boolean

            Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal value As IFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
                MyBase.New(value, New OrmFilterTemplate(t, fieldName, operation))
            End Sub

            'Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal value As Values.IDatabaseFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
            '    MyBase.New(value, New OrmFilterTemplate(t, fieldName, operation))
            '    _dbFilter = True
            'End Sub

            Protected Sub New(ByVal v As IFilterValue, ByVal template As OrmFilterTemplate)
                MyBase.New(v, template)
            End Sub

            'Public Overloads Function MakeSQLStmt(ByVal schema As DbSchema, ByVal almgr As AliasMgr, ByVal pname As ICreateParam) As String Implements IFilter.MakeSQLStmt
            '    Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = almgr.Aliases

            '    If schema Is Nothing Then
            '        Throw New ArgumentNullException("schema")
            '    End If

            '    If _oschema Is Nothing Then
            '        _oschema = schema.GetObjectSchema(Template.Type)
            '    End If

            '    Dim map As MapField2Column = _oschema.GetFieldColumnMap()(Template.FieldName)
            '    Dim [alias] As String = String.Empty

            '    If tableAliases IsNot Nothing Then
            '        [alias] = tableAliases(map._tableName) & "."
            '    End If

            '    If _dbFilter Then
            '        Return [alias] & map._columnName & Template.OperToStmt & GetParam(schema, pname, almgr)
            '    Else
            '        Return [alias] & map._columnName & Template.OperToStmt & GetParam(schema, pname)
            '    End If
            'End Function

            Public Overloads Function MakeQueryStmt(ByVal oschema As IObjectSchemaBase, ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam) As String
                Return MakeQueryStmt(oschema, filterInfo, schema, almgr, pname, Nothing)
            End Function

            Public Overloads Overrides Function MakeQueryStmt(ByVal oschema As IObjectSchemaBase, ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String
                If _oschema Is Nothing Then
                    _oschema = oschema
                End If

                Dim pv As IParamFilterValue = TryCast(Value, IParamFilterValue)

                If pv Is Nothing OrElse pv.ShouldUse Then
                    Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = Nothing

                    If almgr IsNot Nothing Then
                        tableAliases = almgr.Aliases
                    End If

                    If schema Is Nothing Then
                        Throw New ArgumentNullException("schema")
                    End If

                    Dim map As MapField2Column = Nothing
                    Try
                        map = oschema.GetFieldColumnMap()(Template.FieldName)
                    Catch ex As KeyNotFoundException
                        Throw New ObjectMappingException(String.Format("There is not column for property {0} ", Template.Type.ToString & "." & Template.FieldName, ex))
                    End Try

                    Dim [alias] As String = String.Empty

                    If tableAliases IsNot Nothing Then
                        'Debug.Assert(tableAliases.ContainsKey(map._tableName), "There is not alias for table " & map._tableName.RawName)
                        Try
                            [alias] = tableAliases(map._tableName) & schema.Selector
                        Catch ex As KeyNotFoundException
                            Throw New ObjectMappingException("There is not alias for table " & map._tableName.RawName, ex)
                        End Try
                    End If

                    'If _dbFilter Then
                    '    Return [alias] & map._columnName & Template.OperToStmt & GetParam(CType(schema, SQLGenerator), filterInfo, pname, CType(almgr, AliasMgr))
                    'Else
                    '    Return [alias] & map._columnName & Template.OperToStmt & GetParam(schema, pname)
                    'End If
                    Return [alias] & map._columnName & Template.OperToStmt & GetParam(schema, filterInfo, pname, almgr)
                Else
                    Return String.Empty
                End If
            End Function

            Public Overloads Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam) As String
                Return MakeQueryStmt(schema, filterInfo, almgr, pname, Nothing)
            End Function

            Public Overloads Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String
                If schema Is Nothing Then
                    Throw New ArgumentNullException("schema")
                End If

                If _oschema Is Nothing Then
                    _oschema = schema.GetObjectSchema(Template.Type)
                End If

                Return MakeQueryStmt(_oschema, filterInfo, schema, almgr, pname, columns)
            End Function

            'Protected Overrides Function GetParam(ByVal schema As ObjectMappingEngine, ByVal pmgr As ICreateParam) As String
            '    If _dbFilter Then
            '        Throw New InvalidOperationException
            '    Else
            '        Return MyBase.GetParam(schema, pmgr)
            '    End If
            'End Function

            Protected Overloads Function GetParam(ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal pmgr As ICreateParam, ByVal almgr As IPrepareTable) As String
                'If _dbFilter Then
                Return Value.GetParam(schema, pmgr, almgr, AddressOf PrepareValue, Nothing, filterInfo)
                'Else
                'Throw New InvalidOperationException
                'End If
            End Function

            Protected Overrides Function _Clone() As Object
                Return New EntityFilter(val, CType(Template, OrmFilterTemplate))
            End Function
        End Class

        Public Class CustomFilter
            Inherits Worm.Criteria.Core.CustomFilterBase

            Public Sub New(ByVal format As String, ByVal value As IFilterValue, ByVal oper As Worm.Criteria.FilterOperation, ByVal ParamArray values() As Pair(Of Object, String))
                MyBase.New(format, value, oper, values)
            End Sub

            Protected Sub New(ByVal value As IFilterValue)
                MyBase.New(value)
            End Sub

            Protected Overrides ReadOnly Property OperationString() As String
                Get
                    Return TemplateBase.Oper2String(Operation)
                End Get
            End Property

            Protected Overrides Function _Clone() As Object
                Dim c As New CustomFilter(Value)
                CopyTo(c)
                Return c
            End Function
        End Class

        Public Class ExpressionFilter
            Inherits Worm.Criteria.Core.ExpFilter

            Public Sub New(ByVal left As UnaryExp, ByVal right As UnaryExp, ByVal fo As Worm.Criteria.FilterOperation)
                MyBase.New(left, right, fo)
            End Sub

            Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String
                Return Left.MakeStmt(schema, pname, almgr, columns, filterInfo) & TemplateBase.Oper2String(Operation) & Right.MakeStmt(schema, pname, almgr, columns, filterInfo)
            End Function

            Protected Overrides Function _Clone() As Object
                Return New ExpressionFilter(Left, Right, Operation)
            End Function
        End Class
    End Namespace

End Namespace