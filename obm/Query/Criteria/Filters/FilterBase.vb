Imports Worm.Criteria.Values
Imports Worm.Entities.Meta
Imports Worm.Query

Namespace Criteria.Core

    <Serializable()> _
    Public MustInherit Class FilterBase
        Implements IFilter, IValuableFilter

        Private _v As IFilterValue
        Protected _eu As Query.EntityUnion

        Public Sub New(ByVal value As IFilterValue)
            If value Is Nothing Then
                Throw New ArgumentNullException("value")
            End If
            _v = value
        End Sub

        Protected MustOverride Function _ToString() As String Implements IFilter._ToString
        Protected MustOverride Function _Clone() As Object Implements ICloneable.Clone
        Public MustOverride Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef,
                                                   ByVal stmt As StmtGenerator, ByVal executor As Query.IExecutionContext,
                                                   ByVal contextInfo As IDictionary, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As String Implements IFilter.MakeQueryStmt
        Public MustOverride Function GetAllFilters() As IFilter() Implements IFilter.GetAllFilters
        Public MustOverride Function ToStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary) As String Implements IFilter.GetStaticString

        Public Function SetUnion(ByVal eu As Query.EntityUnion) As IFilter Implements IFilter.SetUnion
            _eu = eu
            Return Me
        End Function

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
                Return _ToString.Equals(f._ToString)
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

        Protected Overridable Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, _
            ByVal stmt As StmtGenerator, ByVal pmgr As ICreateParam, ByVal inSelect As Boolean, _
            ByVal almgr As IPrepareTable, ByVal contextInfo As IDictionary, ByVal executor As IExecutionContext) As String
            If _v Is Nothing Then
                'Return pmgr.CreateParam(Nothing)
                Throw New InvalidOperationException("Param is null")
            End If
            Return Value.GetParam(schema, fromClause, stmt, pmgr, almgr, Nothing, contextInfo, inSelect, executor)
        End Function

        Private Function Equals1(ByVal f As IFilter) As Boolean Implements IFilter.Equals
            Return Equals(TryCast(f, FilterBase))
        End Function

        Public Function ReplaceFilter(ByVal oldValue As IFilter, ByVal newValue As IFilter) As IFilter Implements IFilter.ReplaceFilter
            Return ReplaceFilter(TryCast(oldValue, FilterBase), TryCast(newValue, FilterBase))
        End Function

        Public Function ReplaceFilter(ByVal oldValue As FilterBase, ByVal newValue As FilterBase) As FilterBase
            If Equals(oldValue) Then
                Return newValue
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

        'Public ReadOnly Property Filter(ByVal t As System.Type) As IFilter Implements IGetFilter.Filter
        '    Get
        '        Return Me
        '    End Get
        'End Property

        'Protected Function _MakeQueryStmt(ByVal schema As QueryGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam) As String Implements IFilter.MakeQueryStmt
        '    Throw New NotSupportedException
        '    'Return MakeQueryStmt(schema, Filter, almgr, pname)
        'End Function

        Public Overridable Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements Values.IQueryElement.Prepare
            If _v IsNot Nothing Then
                _v.Prepare(executor, schema, contextInfo, stmt, isAnonym)
            End If
        End Sub

        Public Function RemoveFilter(ByVal f As IFilter) As IFilter Implements IFilter.RemoveFilter
            If Equals(f) Then
                Return Nothing
            Else
                Return Me
            End If
        End Function
    End Class

    <Serializable()> _
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

        Public MustOverride Function MakeSingleQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator,
                                                         ByVal almgr As IPrepareTable, ByVal pname As ICreateParam,
                                                         ByVal executor As Query.IExecutionContext) As Pair(Of String) Implements ITemplateFilter.MakeSingleQueryStmt

        Public Overrides Function ToStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary) As String
            Return _templ.GetStaticString(mpe, contextInfo)
        End Function

        Public ReadOnly Property Template() As ITemplate Implements ITemplateFilterBase.Template
            Get
                Return _templ
            End Get
        End Property
    End Class

End Namespace