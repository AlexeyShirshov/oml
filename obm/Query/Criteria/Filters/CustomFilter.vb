Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Values
Imports Worm.Entities
Imports Worm.Expressions
Imports Worm.Query
Imports Worm.Expressions2
Imports Worm.Criteria.Joins
Imports System.Linq

Namespace Criteria.Core

    <Serializable()> _
    Public Class CustomFilter
        Inherits Worm.Criteria.Core.TemplatedFilterBase

        'Public Class TemplateCls
        '    Inherits TemplateBase

        '    Private _format As String
        '    Private _values() As FieldReference
        '    Private _sstr As String

        '    Public Sub New(ByVal oper As FilterOperation, ByVal format As String, ByVal values() As FieldReference)
        '        MyBase.New(oper)
        '        _format = format
        '        _values = values
        '    End Sub

        '    Public Overrides Function _ToString() As String
        '        Return _format & OperationString
        '    End Function

        '    Public Overrides Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String
        '        If String.IsNullOrEmpty(_sstr) Then
        '            If _values IsNot Nothing Then
        '                Dim values As New List(Of String)
        '                For Each p As FieldReference In _values
        '                    'If p.First Is Nothing Then
        '                    '    values.Add(p.Second)
        '                    'Else
        '                    '    values.Add(p.First.ToString & "^" & p.Second)
        '                    'End If
        '                    values.Add(p.ToString)
        '                Next
        '                _sstr = String.Format(_format, values.ToArray) & OperationString
        '            Else
        '                _sstr = _format & OperationString
        '            End If
        '        End If
        '        Return _sstr
        '    End Function

        '    Protected ReadOnly Property OperationString() As String
        '        Get
        '            Return TemplateBase.OperToStringInternal(Operation)
        '        End Get
        '    End Property

        '    Public ReadOnly Property Format() As String
        '        Get
        '            Return _format
        '        End Get
        '    End Property

        '    Public ReadOnly Property Values() As FieldReference()
        '        Get
        '            Return _values
        '        End Get
        '    End Property

        '    Public Function MakeStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal almgr As IPrepareTable) As String
        '        Dim s As String = _format
        '        If _values IsNot Nothing Then
        '            s = String.Format(s, ObjectMappingEngine.ExtractValues(schema, stmt, almgr, _values).ToArray)
        '        End If
        '        Return s
        '    End Function
        'End Class

        'Private _t As Type
        'Private _tbl As SourceFragment
        'Private _field As String
        'Private _oper As Worm.Criteria.FilterOperation
        'Public ReadOnly Property Operation() As Worm.Criteria.FilterOperation
        '    Get
        '        Return _oper
        '    End Get
        'End Property

        Private _str As String

        Public Sub New(ByVal format As String, ByVal value As IFilterValue, _
                       ByVal oper As Worm.Criteria.FilterOperation, ByVal exp() As IExpression)
            MyBase.New(value, New CustomValue(oper, format, exp))
            '_t = table
            '_field = field
            '_format = format
            '_oper = oper
            '_values = values
        End Sub

        Public Sub New(ByVal format As String, ByVal oper As Worm.Criteria.FilterOperation, ByVal value As IFilterValue)
            MyBase.New(value, New CustomValue(oper, format, Nothing))
            '_format = format
            '_oper = oper
        End Sub

        Protected Sub New(ByVal t As CustomValue, ByVal value As IFilterValue)
            MyBase.New(value, t)
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
                _str = Value._ToString & Template._ToString
            End If
            Return _str
        End Function

        Public Overrides Function GetAllFilters() As IFilter()
            Return New CustomFilter() {Me}
        End Function

        Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef,
                                                ByVal stmt As StmtGenerator, ByVal executor As Query.IExecutionContext, _
            ByVal contextInfo As IDictionary, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As String
            'Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = almgr.Aliases

            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            'Dim pf As IParamFilterValue = TryCast(Value, IParamFilterValue)

            If Value.ShouldUse Then
                'Dim s As String = CType(Template, TemplateCls).Format
                'If CType(Template, TemplateCls).Values IsNot Nothing Then
                '    s = String.Format(s, ObjectMappingEngine.ExtractValues(schema, stmt, almgr, CType(Template, TemplateCls).Values).ToArray)
                'End If

                Return CType(Template, CustomValue).GetParam(schema, fromClause, stmt, pname, almgr, Nothing, contextInfo, False, executor) & stmt.Oper2String(Template.Operation) & GetParam(schema, fromClause, stmt, pname, False, almgr, contextInfo, executor)
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
            Return Value.GetStaticString(mpe) & Template.GetStaticString(mpe)
        End Function

        'Protected Sub CopyTo(ByVal obj As CustomFilter)
        '    With obj
        '        ._format = _format
        '        ._oper = _oper
        '        ._sstr = _sstr
        '        ._str = _str
        '        ._values = _values
        '    End With
        'End Sub

        'Public Overloads Function MakeSQLStmt(ByVal schema As DbSchema, ByVal almgr As AliasMgr, ByVal pname As Orm.Meta.ICreateParam) As String Implements IFilter.MakeSQLStmt
        '    Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = almgr.Aliases

        '    If schema Is Nothing Then
        '        Throw New ArgumentNullException("schema")
        '    End If

        '    Dim values As List(Of String) = Worm.Sorting.Sort.ExtractValues(schema, tableAliases, _values)

        '    Return String.Format(_format, values.ToArray) & TemplateBase.Oper2String(_oper) & GetParam(schema, pname)
        'End Function

        Protected Overrides Function _Clone() As Object
            Return Clone()
        End Function

        Public Overloads Function Clone() As CustomFilter
            Dim vc As IFilterValue = Nothing
            If Value IsNot Nothing Then
                vc = CType(Value.Clone, IFilterValue)
            End If
            Return New CustomFilter(CType(Template, CustomValue), vc)
        End Function

        Public Overrides Function MakeSingleQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, _
            ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam, ByVal executor As Query.IExecutionContext) As Pair(Of String)
            Throw New NotImplementedException
        End Function
    End Class
End Namespace