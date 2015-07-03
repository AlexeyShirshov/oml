Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.Query
Imports Worm.Expressions2
Imports System.Linq

Namespace Criteria.Values

    '<Serializable()> _
    'Public Class RefValue
    '    Implements IFilterValue

    '    Private _num As Integer

    '    Public Sub New(ByVal num As Integer)
    '        _num = num
    '    End Sub

    '    Public Function _ToString() As String Implements IFilterValue._ToString
    '        Return _num.ToString
    '    End Function

    '    Public Function GetParam(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
    '                      ByVal almgr As IPrepareTable, ByVal prepare As PrepareValueDelegate, _
    '                      ByVal filterInfo As Object, ByVal inSelect As Boolean) As String Implements IFilterValue.GetParam
    '        Return aliases(_num)
    '    End Function

    '    Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
    '        Return "refval"
    '    End Function
    'End Class

    '<Serializable()> _
    'Public Class SelectExpressionValue
    '    Implements IFilterValue

    '    Private _p As SelectExpressionOld

    '    Public Sub New(ByVal propertyAlias As String)
    '        _p = New SelectExpressionOld(CType(Nothing, EntityUnion), propertyAlias)
    '    End Sub

    '    Public Sub New(ByVal p As SelectExpressionOld)
    '        _p = p
    '    End Sub

    '    Public Sub New(ByVal op As ObjectProperty)
    '        _p = New SelectExpressionOld(op)
    '    End Sub

    '    Public Sub New(ByVal t As Type, ByVal propertyAlias As String)
    '        _p = New SelectExpressionOld(t, propertyAlias)
    '    End Sub

    '    Public Sub New(ByVal entityName As String, ByVal propertyAlias As String)
    '        _p = New SelectExpressionOld(entityName, propertyAlias)
    '    End Sub

    '    Public Sub New(ByVal os As EntityUnion, ByVal propertyAlias As String)
    '        _p = New SelectExpressionOld(os, propertyAlias)
    '    End Sub

    '    Public Sub New(ByVal table As SourceFragment, ByVal column As String)
    '        _p = New SelectExpressionOld(table, column)
    '    End Sub

    '    Public ReadOnly Property Expression() As SelectExpressionOld
    '        Get
    '            Return _p
    '        End Get
    '    End Property

    '    Public Function _ToString() As String Implements IFilterValue._ToString
    '        Return _p.GetDynamicString
    '    End Function

    '    'Public Property AddAlias() As Boolean
    '    '    Get
    '    '        Return _p.AddAlias
    '    '    End Get
    '    '    Set(ByVal value As Boolean)
    '    '        _p.AddAlias = value
    '    '    End Set
    '    'End Property

    '    Public Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
    '                      ByVal almgr As IPrepareTable, ByVal prepare As PrepareValueDelegate, _
    '                      ByVal filterInfo As Object, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String Implements IFilterValue.GetParam
    '        'Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = Nothing

    '        'If almgr IsNot Nothing Then
    '        '    tableAliases = almgr.Aliases
    '        'End If

    '        If schema Is Nothing Then
    '            Throw New ArgumentNullException("schema")
    '        End If

    '        'Dim d As String = schema.Delimiter
    '        'If Not inSelect Then
    '        '    d = stmt.Selector
    '        'End If

    '        'Dim f As ISelectExpressionFormater = stmt.CreateSelectExpressionFormater
    '        Dim sb As New StringBuilder
    '        'f.Format(_p, sb, executor, Nothing, schema, almgr, paramMgr, filterInfo, Nothing, fromClause, inSelect)
    '        Return sb.ToString

    '        'If _p.IsCustom Then
    '        '    Dim f As ISelectExpressionFormater = stmt.CreateSelectExpressionFormater
    '        '    Dim sb As New StringBuilder
    '        '    f.Format(_p, sb, Nothing, schema, almgr, paramMgr, filterInfo, Nothing, Nothing, Nothing, inSelect)
    '        '    Return sb.ToString
    '        'ElseIf _p.Table Is Nothing Then

    '        '    Dim oschema As IEntitySchema = schema.GetEntitySchema(_p.ObjectSource.GetRealType(schema))

    '        '    Dim map As MapField2Column = Nothing
    '        '    Try
    '        '        map = oschema.GetFieldColumnMap()(_p.PropertyAlias)
    '        '    Catch ex As KeyNotFoundException
    '        '        Throw New ObjectMappingException(String.Format("There is not column for property {0} ", _p.ObjectSource.ToStaticString(schema, filterInfo) & schema.Delimiter & _p.PropertyAlias, ex))
    '        '    End Try

    '        '    Dim [alias] As String = String.Empty

    '        '    If almgr IsNot Nothing Then
    '        '        'Debug.Assert(tableAliases.ContainsKey(map.Table), "There is not alias for table " & map._tableName.RawName)
    '        '        If almgr.ContainsKey(map._tableName, _p.ObjectSource) Then
    '        '            [alias] = almgr.GetAlias(map._tableName, _p.ObjectSource) & d
    '        '        Else
    '        '            [alias] = map._tableName.UniqueName(_p.ObjectSource) & d
    '        '        End If
    '        '        'Try
    '        '        '    [alias] = tableAliases(map._tableName) & schema.Selector
    '        '        'Catch ex As KeyNotFoundException
    '        '        '    Throw New QueryGeneratorException("There is not alias for table " & map._tableName.RawName, ex)
    '        '        'End Try
    '        '    End If

    '        '    Return [alias] & map._columnName
    '        'Else
    '        '    Dim [alias] As String = String.Empty

    '        '    If almgr IsNot Nothing Then
    '        '        'Debug.Assert(tableAliases.ContainsKey(map._tableName), "There is not alias for table " & map._tableName.RawName)
    '        '        Try
    '        '            [alias] = almgr.GetAlias(_p.Table, Nothing) & d
    '        '        Catch ex As KeyNotFoundException
    '        '            Throw New ObjectMappingException("There is not alias for table " & _p.Table.RawName, ex)
    '        '        End Try
    '        '    End If

    '        '    Return [alias] & _p.Column
    '        'End If
    '    End Function

    '    Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements IQueryElement.GetStaticString
    '        Return _p.GetStaticString(mpe, contextFilter)
    '    End Function

    '    Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
    '        _p.Prepare(executor, schema, filterInfo, stmt, isAnonym)
    '    End Sub

    '    Public ReadOnly Property ShouldUse() As Boolean Implements IFilterValue.ShouldUse
    '        Get
    '            Return True
    '        End Get
    '    End Property
    'End Class


    '    <Serializable()> _
    '    Public Class SubQuery
    '        Implements Worm.Criteria.Values.IFilterValue, Worm.Criteria.Values.INonTemplateValue,  _
    '        Cache.IQueryDependentTypes

    '        Private _t As Type
    '        Private _tbl As SourceFragment
    '        Private _f As IFilter
    '        Private _field As String
    '        Private _joins() As Worm.Criteria.Joins.QueryJoin

    '        Public Sub New(ByVal t As Type, ByVal f As IFilter)
    '            _t = t
    '            _f = f
    '        End Sub

    '        Public Sub New(ByVal table As SourceFragment, ByVal f As IFilter)
    '            _tbl = table
    '            _f = f
    '        End Sub

    '        Public Sub New(ByVal t As Type, ByVal f As IEntityFilter, ByVal field As String)
    '            '_tbl = CType(OrmManager.CurrentManager.ObjectSchema.GetObjectSchema(t), IOrmObjectSchema).GetTables(0)
    '            _t = t
    '            _f = f
    '            _field = field
    '        End Sub

    '#Region " Properties "

    '        Public Property Joins() As Worm.Criteria.Joins.QueryJoin()
    '            Get
    '                Return _joins
    '            End Get
    '            Set(ByVal value As Worm.Criteria.Joins.QueryJoin())
    '                _joins = value
    '            End Set
    '        End Property

    '        Public ReadOnly Property Filter() As IFilter
    '            Get
    '                Return _f
    '            End Get
    '        End Property

    '        Public ReadOnly Property Table() As SourceFragment
    '            Get
    '                Return _tbl
    '            End Get
    '        End Property

    '        Public ReadOnly Property Type() As Type
    '            Get
    '                Return _t
    '            End Get
    '        End Property

    '#End Region

    '        Public Overridable Function _ToString() As String Implements Worm.Criteria.Values.IFilterValue._ToString
    '            Dim r As String = Nothing
    '            If _t IsNot Nothing Then
    '                r = _t.ToString()
    '            Else
    '                r = _tbl.RawName
    '            End If
    '            If _joins IsNot Nothing Then
    '                r &= "$"
    '                For Each join As Worm.Criteria.Joins.QueryJoin In _joins
    '                    If Not Worm.Criteria.Joins.QueryJoin.IsEmpty(join) Then
    '                        r &= join._ToString
    '                    End If
    '                Next
    '            End If
    '            If _f IsNot Nothing Then
    '                r &= "$" & _f._ToString
    '            End If
    '            If Not String.IsNullOrEmpty(_field) Then
    '                r &= "$" & _field
    '            End If
    '            Return r
    '        End Function

    '        Public Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
    '                      ByVal almgr As IPrepareTable, ByVal prepare As Worm.Criteria.Values.PrepareValueDelegate, _
    '                      ByVal filterInfo As Object, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String Implements Worm.Criteria.Values.IFilterValue.GetParam
    '            Dim sb As New StringBuilder
    '            'Dim dbschema As DbSchema = CType(schema, DbSchema)
    '            sb.Append("(")

    '            'If _stmtGen Is Nothing Then
    '            '    _stmtGen = TryCast(schema, SQLGenerator)
    '            'End If

    '            stmt.FormStmt(schema, fromClause, filterInfo, paramMgr, almgr, sb, _t, _tbl, _joins, _field, _f)

    '            sb.Append(")")

    '            Return sb.ToString
    '        End Function

    '        Public Overridable Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements Worm.Criteria.Values.INonTemplateValue.GetStaticString
    '            Dim r As String = Nothing
    '            If _t IsNot Nothing Then
    '                r = _t.ToString()
    '            Else
    '                r = _tbl.RawName
    '            End If
    '            If _joins IsNot Nothing Then
    '                r &= "$"
    '                For Each join As Worm.Criteria.Joins.QueryJoin In _joins
    '                    If Not Worm.Criteria.Joins.QueryJoin.IsEmpty(join) Then
    '                        r &= join.GetStaticString(mpe, contextFilter)
    '                    End If
    '                Next
    '            End If
    '            If _f IsNot Nothing Then
    '                r &= "$" & _f.GetStaticString(mpe, contextFilter)
    '            End If
    '            If Not String.IsNullOrEmpty(_field) Then
    '                r &= "$" & _field
    '            End If
    '            Return r
    '        End Function

    '        Public Function [Get](ByVal mpe As ObjectMappingEngine) As Cache.IDependentTypes Implements Cache.IQueryDependentTypes.Get
    '            'If _t Is Nothing Then
    '            '    Return New Cache.EmptyDependentTypes
    '            'End If

    '            Dim dp As New Cache.DependentTypes
    '            If _joins IsNot Nothing Then
    '                For Each j As Worm.Criteria.Joins.QueryJoin In _joins
    '                    Dim t As Type = j.ObjectSource.GetRealType(mpe)
    '                    'If t Is Nothing AndAlso Not String.IsNullOrEmpty(j.EntityName) Then
    '                    '    t = mpe.GetTypeByEntityName(j.EntityName)
    '                    'End If
    '                    'If t Is Nothing Then
    '                    '    Return New Cache.EmptyDependentTypes
    '                    'End If
    '                    dp.AddBoth(t)
    '                Next
    '            End If

    '            If _f IsNot Nothing AndAlso TryCast(_f, IEntityFilter) Is Nothing Then
    '                For Each f As IFilter In _f.Filter.GetAllFilters
    '                    Dim fdp As Cache.IDependentTypes = Cache.QueryDependentTypes(mpe, f)
    '                    If Cache.IsCalculated(fdp) Then
    '                        dp.Merge(fdp)
    '                        'Else
    '                        '    Return fdp
    '                    End If
    '                Next
    '            End If

    '            Return dp.Get
    '        End Function

    '        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
    '            'do nothing
    '        End Sub

    '        Public ReadOnly Property ShouldUse() As Boolean Implements IFilterValue.ShouldUse
    '            Get
    '                Return True
    '            End Get
    '        End Property
    '    End Class

    <Serializable()> _
    Public Class SubQueryCmd
        Implements Worm.Criteria.Values.IFilterValue, Worm.Criteria.Values.INonTemplateValue,  _
        Cache.IQueryDependentTypes

        Private _q As Query.QueryCmd

        'Public Sub New(ByVal q As Query.QueryCmd)
        '    MyClass.New(q)
        'End Sub

        Public Sub New(ByVal q As Query.QueryCmd)
            _q = q
        End Sub

        Public Function _ToString() As String Implements Worm.Criteria.Values.IFilterValue._ToString
            Return _q._ToString()
        End Function

        Public Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, ByVal paramMgr As ICreateParam, _
                          ByVal almgr As IPrepareTable, ByVal prepare As Worm.Criteria.Values.PrepareValueDelegate, _
                          ByVal contextInfo As IDictionary, ByVal inSelect As Boolean, ByVal executor As IExecutionContext) As String Implements Worm.Criteria.Values.IFilterValue.GetParam
            Dim sb As New StringBuilder
            'Dim dbschema As DbSchema = CType(schema, DbSchema)
            sb.Append("(")

            'Dim c As New Query.QueryCmd.svct(_q)
            'Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
            'If _q.SelectedType Is Nothing Then
            '    If String.IsNullOrEmpty(_q.SelectedEntityName) Then
            '        _q.SelectedType = _q.CreateType
            '    Else
            '        _q.SelectedType = schema.GetTypeByEntityName(_q.SelectedEntityName)
            '    End If
            'End If

            'If GetType(Entities.AnonymousEntity).IsAssignableFrom(_q.SelectedType) Then
            '    _q.SelectedType = Nothing
            'End If

            'If _q.CreateType Is Nothing AndAlso _q.SelectedType IsNot Nothing Then
            '    _q.Into(_q.SelectedType)
            'End If

            'QueryCmd.Prepare(_q, Nothing, schema, filterInfo, stmt)
            sb.Append(stmt.MakeQueryStatement(schema, fromClause, contextInfo, _q, paramMgr, almgr))
            'End Using

            sb.Append(")")

            Return sb.ToString
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements Worm.Criteria.Values.INonTemplateValue.GetStaticString
            Return _q.ToStaticString(mpe)
        End Function

        Public Function [Get](ByVal mpe As ObjectMappingEngine) As Cache.IDependentTypes Implements Cache.IQueryDependentTypes.Get
            Dim qp As Cache.IDependentTypes = CType(_q, Cache.IQueryDependentTypes).Get(mpe)
            If Cache.IsEmpty(qp) Then
                Dim dt As New Cache.DependentTypes
                Dim types As ICollection(Of Type) = Nothing
                If _q.GetSelectedTypes(mpe, types) Then
                    dt.AddBoth(types)
                End If
                qp = dt
            End If
            Return qp
        End Function

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare
            _q.AutoFields = False
            _q.Prepare(executor, schema, contextInfo, stmt, isAnonym)
        End Sub

        Public ReadOnly Property ShouldUse() As Boolean Implements IFilterValue.ShouldUse
            Get
                Return True
            End Get
        End Property

        Protected Overridable Function _Clone() As Object Implements ICloneable.Clone
            Return Clone
        End Function

        Public Function Clone() As SubQueryCmd
            Return New SubQueryCmd(_q.Clone)
        End Function
        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, SubQueryCmd))
        End Function

        Public Function CopyTo(target As SubQueryCmd) As Boolean
            If target Is Nothing Then
                Return False
            End If

            If _q IsNot Nothing Then
                target._q = _q.Clone
            End If
            Return True
        End Function
    End Class
End Namespace

'Namespace Database
'    Namespace Criteria.Values
'        'Public Interface IDatabaseFilterValue
'        '    Inherits Worm.Criteria.Values.IFilterValue
'        '    Function GetParam(ByVal schema As SQLGenerator, ByVal filterInfo As Object, ByVal paramMgr As ICreateParam, ByVal almgr As IPrepareTable) As String
'        'End Interface


'    End Namespace
'End Namespace
