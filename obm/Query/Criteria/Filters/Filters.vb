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
    Public Class EntityFilter
        Inherits TemplatedFilterBase
        Implements IEntityFilter

        'Private _templ As OrmFilterTemplate
        Private _str As String
        'Protected _oschema As IEntitySchema
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

        Public Sub New(ByVal [alias] As QueryAlias, ByVal propertyAlias As String, ByVal value As IFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
            MyBase.New(value, New OrmFilterTemplate([alias], propertyAlias, operation))
        End Sub

        Public Sub New(ByVal os As EntityUnion, ByVal propertyAlias As String, ByVal value As IFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
            MyBase.New(value, New OrmFilterTemplate(os, propertyAlias, operation))
        End Sub

        Public Sub New(ByVal op As ObjectProperty, ByVal value As IFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
            MyBase.New(value, New OrmFilterTemplate(op, operation))
        End Sub

        'Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal value As Values.IDatabaseFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
        '    MyBase.New(value, New OrmFilterTemplate(t, fieldName, operation))
        '    _dbFilter = True
        'End Sub

        Protected Overrides Function _ToString() As String
            If _str Is Nothing Then
                _str = val._ToString & Template._ToString
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

        'Public Function Eval(ByVal schema As ObjectMappingEngine, ByVal obj As _IEntity, ByVal oschema As IEntitySchema) As IEvaluableValue.EvalResult Implements IEntityFilter.EvalObj
        '    Dim evval As IEvaluableValue = TryCast(val(), IEvaluableValue)
        '    If evval IsNot Nothing Then
        '        If schema Is Nothing Then
        '            Throw New ArgumentNullException("schema")
        '        End If

        '        If obj Is Nothing Then
        '            Throw New ArgumentNullException("obj")
        '        End If

        '        Dim t As Type = obj.GetType
        '        If oschema Is Nothing Then
        '            oschema = schema.GetEntitySchema(t)
        '        End If

        '        Dim rt As Type = Template.ObjectSource.GetRealType(schema)
        '        If rt Is t Then
        '            Dim r As IEvaluableValue.EvalResult = IEvaluableValue.EvalResult.NotFound
        '            Dim v As Object = ObjectMappingEngine.GetPropertyValue(obj, Template.PropertyAlias, oschema, Nothing) 'schema.GetFieldValue(obj, _fieldname)
        '            r = evval.Eval(v, schema, Template)
        '            'If v IsNot Nothing Then
        '            '    r = evval.Eval(v, Template)
        '            'Else
        '            '    If evval.Value Is Nothing Then
        '            '        r = IEvaluableValue.EvalResult.Found
        '            '    End If
        '            'End If

        '            Return r
        '        Else
        '            Dim o As _IEntity = schema.GetJoinObj(oschema, obj, rt)
        '            If o IsNot Nothing Then
        '                Return Eval(schema, o, schema.GetEntitySchema(rt))
        '            End If
        '        End If
        '    End If

        '    Return IEvaluableValue.EvalResult.Unknown
        'End Function

        Public Function Eval(ByVal mpe As ObjectMappingEngine, ByVal obj As _IEntity, ByVal oschema As IEntitySchema,
                              joins() As Joins.QueryJoin, objEU As EntityUnion) As IEvaluableValue.EvalResult Implements IEntityFilter.EvalObj
            Dim evval As IEvaluableValue = TryCast(val(), IEvaluableValue)
            If evval IsNot Nothing Then
                If mpe Is Nothing Then
                    Throw New ArgumentNullException("mpe")
                End If

                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                Dim t As Type = obj.GetType
                If oschema Is Nothing Then
                    oschema = mpe.GetEntitySchema(t)
                End If

                Dim rt As Type = Template.ObjectSource.GetRealType(mpe)
                If rt Is t Then
                    Dim r As IEvaluableValue.EvalResult = IEvaluableValue.EvalResult.NotFound
                    Dim v As Object = ObjectMappingEngine.GetPropertyValue(obj, Template.PropertyAlias, oschema, Nothing) 'schema.GetFieldValue(obj, _fieldname)
                    r = evval.Eval(v, mpe, Template)
                    'If v IsNot Nothing Then
                    '    r = evval.Eval(v, Template)
                    'Else
                    '    If evval.Value Is Nothing Then
                    '        r = IEvaluableValue.EvalResult.Found
                    '    End If
                    'End If

                    Return r
                Else
                    Dim o As _IEntity = Nothing
                    If objEU IsNot Nothing Then
                        Dim roots As New Dictionary(Of EntityUnion, _IEntity)
                        roots.Add(objEU, obj)
                        Dim r As Pair(Of _IEntity, IEvaluableValue.EvalResult) = GetRoot(Template.ObjectSource, roots, joins, mpe, obj, objEU, oschema)
                        If r.First Is Nothing Then
                            If r.Second = IEvaluableValue.EvalResult.NotFound Then
                                Return IEvaluableValue.EvalResult.NotFound
                            End If
                        Else
                            o = r.First
                        End If
                    End If
                    '                    Dim lschema As IEntitySchema = mpe.GetEntitySchema(rt)
                    '                    Dim pks As IEnumerable(Of MapField2Column) = ObjectMappingEngine.GetPKs(lschema)
                    '                    Dim o As _IEntity = Nothing
                    '                    If joins IsNot Nothing Then
                    '                        Dim join As QueryJoin = joins.FirstOrDefault(Function(it) it.ObjectSource = Template.ObjectSource)
                    '                        Dim pk As New List(Of PKDesc)
                    '                        For Each ff As IFilter In join.Condition.GetAllFilters
                    '                            Dim jf As JoinFilter = TryCast(ff, JoinFilter)
                    '                            If jf IsNot Nothing Then
                    '                                If jf.Left.Property.Entity IsNot Nothing AndAlso jf.Left.Property.Entity = Template.ObjectSource AndAlso
                    '                                    jf.Right.Property.Entity IsNot Nothing AndAlso jf.Right.Property.Entity = objEU AndAlso
                    '                                    pks.Any(Function(it) it.PropertyAlias = jf.Left.Property.PropertyAlias) Then

                    '                                    Dim id As Object = ObjectMappingEngine.GetPropertyValue(obj, jf.Right.Property.PropertyAlias, oschema)
                    '                                    Dim r As _ICachedEntity = TryCast(id, _ICachedEntity)
                    '                                    If r IsNot Nothing Then
                    '                                        pk.AddRange(OrmManager.GetPKValues(r, lschema))
                    '                                        Exit For
                    '                                    Else
                    '                                        pk.Add(New PKDesc(jf.Left.Property.PropertyAlias, id))
                    '                                    End If

                    '                                ElseIf jf.Right.Property.Entity IsNot Nothing AndAlso jf.Right.Property.Entity = Template.ObjectSource AndAlso
                    '                                    jf.Left.Property.Entity IsNot Nothing AndAlso jf.Left.Property.Entity = objEU AndAlso
                    '                                    pks.Any(Function(it) it.PropertyAlias = jf.Right.Property.PropertyAlias) Then

                    '                                    Dim id As Object = ObjectMappingEngine.GetPropertyValue(obj, jf.Left.Property.PropertyAlias, oschema)
                    '                                    Dim r As _ICachedEntity = TryCast(id, _ICachedEntity)
                    '                                    If r IsNot Nothing Then
                    '                                        pk.AddRange(OrmManager.GetPKValues(r, lschema))
                    '                                        Exit For
                    '                                    Else
                    '                                        pk.Add(New PKDesc(jf.Right.Property.PropertyAlias, id))
                    '                                    End If

                    '                                End If
                    '                            Else
                    '                                Dim ef As IEvaluableFilter = TryCast(ff, IEvaluableFilter)
                    '                                If ef Is Nothing Then
                    '                                    GoTo exitFor
                    '                                End If
                    '                            End If
                    '                        Next

                    '                        If pk.Count > 0 Then
                    '                            Dim jObj As ICachedEntity = OrmManager.CurrentManager.GetEntityFromCacheLoadedOrDB(pk.ToArray, rt)
                    '                            If jObj IsNot Nothing Then
                    '                                Dim newF As IFilter = join.Condition
                    '                                For Each jf As JoinFilter In join.Condition.GetAllFilters.OfType(Of JoinFilter).
                    '                                    Where(Function(it) it.Left.Property.Entity IsNot Nothing AndAlso it.Right.Property.Entity IsNot Nothing)
                    '                                    If jf.Left.Property.Entity = Template.ObjectSource Then
                    '                                        Dim root As _IEntity = GetRoot(jf.Right.Property.Entity, roots, joins, mpe)
                    '                                        If root Is Nothing Then
                    '                                            GoTo exitFor
                    '                                        End If
                    '                                        Dim v As Object = ObjectMappingEngine.GetPropertyValue(root, jf.Right.Property.PropertyAlias,
                    '                                                                                               mpe.GetEntitySchema(jf.Right.Property.Entity.AnyType), Nothing)

                    '                                        Dim ef As IFilter = Nothing
                    '                                        Dim spke As ISinglePKEntity = TryCast(v, ISinglePKEntity)
                    '                                        If spke IsNot Nothing Then
                    '                                            ef = New EntityFilter(jf.Left.Property, New EntityValue(spke), FilterOperation.Equal)
                    '                                        Else
                    '                                            ef = New EntityFilter(jf.Left.Property, New ScalarValue(v), FilterOperation.Equal)
                    '                                        End If

                    '                                        newF = newF.ReplaceFilter(jf, ef)
                    '                                    ElseIf jf.Right.Property.Entity = Template.ObjectSource Then
                    '                                        Dim root As _IEntity = GetRoot(jf.Left.Property.Entity, roots, joins, mpe)
                    '                                        If root Is Nothing Then
                    '                                            GoTo exitFor
                    '                                        End If
                    '                                        Dim v As Object = ObjectMappingEngine.GetPropertyValue(root, jf.Left.Property.PropertyAlias,
                    '                                                                                               mpe.GetEntitySchema(jf.Left.Property.Entity.AnyType), Nothing)
                    '                                        Dim ef As IFilter = Nothing
                    '                                        Dim spke As ISinglePKEntity = TryCast(v, ISinglePKEntity)
                    '                                        If spke IsNot Nothing Then
                    '                                            ef = New EntityFilter(jf.Left.Property, New EntityValue(spke), FilterOperation.Equal)
                    '                                        Else
                    '                                            ef = New EntityFilter(jf.Left.Property, New ScalarValue(v), FilterOperation.Equal)
                    '                                        End If
                    '                                        newF = newF.ReplaceFilter(jf, ef)
                    '                                    End If
                    '                                Next

                    '                                Dim evalNewF As IEvaluableFilter = TryCast(newF, IEvaluableFilter)
                    '                                If evalNewF IsNot Nothing Then
                    '                                    Dim res As IEvaluableValue.EvalResult = evalNewF.Eval(mpe,
                    '                                                  Function(efb As IEntityFilterBase)
                    '                                                      Dim tmpl As OrmFilterTemplate = TryCast(efb.GetFilterTemplate, OrmFilterTemplate)
                    '                                                      Dim oo As _IEntity = Nothing
                    '                                                      If tmpl IsNot Nothing Then
                    '                                                          If tmpl.ObjectSource = Template.ObjectSource Then
                    '                                                              oo = jObj
                    '                                                          Else
                    '                                                              roots.TryGetValue(tmpl.ObjectSource, oo)
                    '                                                          End If
                    '                                                      End If
                    '                                                      Return New Pair(Of _IEntity, IEntitySchema)(oo, mpe.GetEntitySchema(tmpl.ObjectSource.AnyType))
                    '                                                  End Function)
                    '                                    If res = IEvaluableValue.EvalResult.Found Then
                    '                                        roots.Add(Template.ObjectSource, jObj)

                    '                                        Return Eval(mpe, jObj, lschema)
                    '                                    End If

                    '                                    Return res
                    '                                End If
                    '                            ElseIf join.JoinType = JoinType.RightOuterJoin Then
                    '                                Return IEvaluableValue.EvalResult.NotFound
                    '                            End If
                    '                        End If
                    'exitFor:
                    'End If

                    If o Is Nothing Then
                        o = mpe.GetJoinObj(oschema, obj, rt)
                    End If

                    If o IsNot Nothing Then
                        Return Eval(mpe, o, mpe.GetEntitySchema(rt), joins, objEU)
                    End If
                End If
            End If

            Return IEvaluableValue.EvalResult.Unknown
        End Function

        Protected Overloads Function GetParam(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, _
            ByVal contextInfo As IDictionary, ByVal pmgr As ICreateParam, ByVal almgr As IPrepareTable, _
            ByVal inSelect As Boolean, ByVal oschema As IEntitySchema, ByVal executor As IExecutionContext) As String
            'If _dbFilter Then
            Return Value.GetParam(schema, fromClause, stmt, pmgr, almgr, AddressOf New cls(oschema, Template.PropertyAlias).PrepareValue, contextInfo, inSelect, executor)
            'Else
            'Throw New InvalidOperationException
            'End If
        End Function

        Public Overrides Function GetAllFilters() As IFilter()
            Return New EntityFilter() {Me}
        End Function

        Public Function GetFilterTemplate() As IOrmFilterTemplate Implements IEntityFilter.GetFilterTemplate
            'If TryCast(Value, IEvaluableValue) IsNot Nothing Then
            Return Template
            'End If
            'Return Nothing
        End Function

        Class cls
            Private _s As IEntitySchema
            Private _pa As String
            Public Sub New(ByVal s As IEntitySchema, ByVal pa As String)
                _s = s
                _pa = pa
            End Sub
            Public Function PrepareValue(ByVal schema As ObjectMappingEngine, ByVal v As Object) As Object 'Implements IEntityFilter.PrepareValue
                Return schema.ChangeValueType(_s, _pa, v)
            End Function
        End Class
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

        Public Overloads Function MakeQueryStmt(ByVal oschema As IEntitySchema, ByVal fromClause As QueryCmd.FromClauseDef, ByVal stmt As StmtGenerator, _
             ByVal executor As Query.IExecutionContext, ByVal contextInfo As IDictionary, _
             ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, _
             ByVal pname As Entities.Meta.ICreateParam, ByVal t As Type) As String
            'If _oschema Is Nothing Then
            '    _oschema = oschema
            'End If

            'Dim pv As IParamFilterValue = TryCast(Value, IParamFilterValue)

            If Value.ShouldUse Then
                'Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = Nothing

                'If almgr IsNot Nothing Then
                '    tableAliases = almgr.Aliases
                'End If

                If schema Is Nothing Then
                    Throw New ArgumentNullException("schema")
                End If

                Dim map As MapField2Column = Nothing

                Dim [alias] As String = String.Empty

                Dim tbl As SourceFragment = Nothing

                If Template.ObjectSource.IsQuery Then
                    tbl = Template.ObjectSource.ObjectAlias.Tbl
                    Dim q As QueryCmd = Template.ObjectSource.ObjectAlias.Query
                    map = New MapField2Column(Template.PropertyAlias, executor.FindColumn(schema, Template.PropertyAlias), tbl)
                Else
                    Try
                        If executor Is Nothing Then
                            map = oschema.FieldColumnMap(Template.PropertyAlias)
                        Else
                            map = executor.GetFieldColumnMap(oschema, t)(Template.PropertyAlias)
                        End If
                        tbl = map.Table
                    Catch ex As KeyNotFoundException
                        Throw New ObjectMappingException(String.Format("There is no column for property {0} ", Template.ObjectSource.ToStaticString(schema, contextInfo) & "." & Template.PropertyAlias, ex))
                    End Try
                End If

                If almgr IsNot Nothing Then
                    'Debug.Assert(almgr.ContainsKey(tbl, Template.ObjectSource), "There is no alias for table " & tbl.RawName)
                    Try
                        [alias] = almgr.GetAlias(tbl, Template.ObjectSource) & stmt.Selector
                    Catch ex As KeyNotFoundException
                        Throw New ObjectMappingException("There is no alias for table " & tbl.RawName, ex)
                    End Try
                End If

                'If _dbFilter Then
                '    Return [alias] & map._columnName & Template.OperToStmt & GetParam(CType(schema, SQLGenerator), filterInfo, pname, CType(almgr, AliasMgr))
                'Else
                '    Return [alias] & map._columnName & Template.OperToStmt & GetParam(schema, pname)
                'End If
                Return [alias] & map.SourceFieldExpression & Template.OperToStmt(stmt) & GetParam(schema, fromClause, stmt, contextInfo, pname, almgr, False, oschema, executor)
            Else
                Return String.Empty
            End If
        End Function

        'Public Overloads Function MakeQueryStmt(ByVal oschema As IEntitySchema, ByVal stmt As StmtGenerator, ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam) As String
        '    Return MakeQueryStmt(oschema, stmt, filterInfo, schema, almgr, pname, Nothing)
        'End Function

        Public Overridable Overloads Function MakeSingleQueryStmt(ByVal oschema As IEntitySchema, _
            ByVal stmt As StmtGenerator, ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, _
            ByVal pname As ICreateParam, ByVal executor As Query.IExecutionContext) As Pair(Of String)
            'If _oschema Is Nothing Then
            '    _oschema = oschema
            'End If

            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If pname Is Nothing Then
                Throw New ArgumentNullException("pname")
            End If

            Dim pd As Values.PrepareValueDelegate = Nothing
            If _prep Then
                pd = AddressOf New cls(oschema, Template.PropertyAlias).PrepareValue
            End If

            Dim prname As String = Value.GetParam(schema, Nothing, stmt, pname, almgr, pd, Nothing, False, Nothing)

            Dim map As MapField2Column = oschema.FieldColumnMap()(Template.PropertyAlias)
            Dim rt As Type = Template.ObjectSource.GetRealType(schema)

            Dim v As IEvaluableValue = TryCast(val(), IEvaluableValue)
            If v IsNot Nothing AndAlso v.Value Is DBNull.Value Then
                If schema.GetPropertyTypeByName(rt, oschema, Template.PropertyAlias) Is GetType(Byte()) Then
                    pname.GetParameter(prname).DbType = System.Data.DbType.Binary
                ElseIf schema.GetPropertyTypeByName(rt, oschema, Template.PropertyAlias) Is GetType(Decimal) Then
                    pname.GetParameter(prname).DbType = System.Data.DbType.Decimal
                End If
            End If

            Return New Pair(Of String)(map.SourceFieldExpression, prname)
        End Function

        Public Overrides Function MakeSingleQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, _
            ByVal almgr As IPrepareTable, ByVal pname As ICreateParam, ByVal executor As Query.IExecutionContext) As Pair(Of String)
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            Dim oschema As IEntitySchema = Nothing

            If oschema Is Nothing Then
                Dim t As Type = Nothing

                If Template.ObjectSource.AnyType IsNot Nothing Then
                    t = Template.ObjectSource.AnyType
                Else
                    t = schema.GetTypeByEntityName(Template.ObjectSource.AnyEntityName)
                End If

                If executor IsNot Nothing Then
                    oschema = executor.GetEntitySchema(schema, t)
                Else
                    oschema = schema.GetEntitySchema(t)
                End If
            End If

            Return MakeSingleQueryStmt(oschema, stmt, schema, almgr, pname, executor)
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
            Return New EntityFilter(val, CType(Template, OrmFilterTemplate))
        End Function

        Public Overloads Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef,
                                                          ByVal stmt As StmtGenerator, ByVal executor As Query.IExecutionContext,
                                                          ByVal contextInfo As IDictionary, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam) As String
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If stmt Is Nothing Then
                Throw New ArgumentNullException("stmt")
            End If

            Dim t As Type = Template.ObjectSource.GetRealType(schema)
            'Dim oschema As IEntitySchema = schema.GetEntitySchema(t, False)
            'If oschema Is Nothing Then
            '    oschema = executor.GetEntitySchema(t)
            'End If
            Dim oschema As IEntitySchema = Nothing
            If executor Is Nothing Then
                oschema = schema.GetEntitySchema(t)
            Else
                oschema = executor.GetEntitySchema(schema, t)
            End If

            Return MakeQueryStmt(oschema, fromClause, stmt, executor, contextInfo, schema, almgr, pname, t)
        End Function

        Public Function Eval1(mpe As ObjectMappingEngine, d As GetObj4IEntityFilterDelegate,
                              joins() As Joins.QueryJoin, objEU As EntityUnion) As Values.IEvaluableValue.EvalResult Implements IEvaluableFilter.Eval
            If d Is Nothing Then
                Return IEvaluableValue.EvalResult.Unknown
            End If

            Dim p As Pair(Of _IEntity, IEntitySchema) = d(Me)

            If p Is Nothing Then
                Return IEvaluableValue.EvalResult.Unknown
            End If

            Return Eval(mpe, p.First, p.Second, joins, objEU)
        End Function

        Private Shared Function GetRoot(entity As EntityUnion, roots As Dictionary(Of EntityUnion, _IEntity), joins As QueryJoin(),
                                        mpe As ObjectMappingEngine, obj As _IEntity, objEU As EntityUnion, oschema As IEntitySchema) As Pair(Of _IEntity, IEvaluableValue.EvalResult)
            Dim oo As _IEntity = Nothing
            If Not roots.TryGetValue(entity, oo) Then
                Dim rt As Type = entity.GetRealType(mpe)
                Dim lschema As IEntitySchema = mpe.GetEntitySchema(rt)
                Dim pks As IEnumerable(Of MapField2Column) = ObjectMappingEngine.GetPKs(lschema)
                Dim o As _IEntity = Nothing
                If joins IsNot Nothing Then
                    Dim join As QueryJoin = joins.FirstOrDefault(Function(it) it.ObjectSource = entity)
                    If join Is Nothing Then
                        Return New Pair(Of _IEntity, IEvaluableValue.EvalResult)(Nothing, IEvaluableValue.EvalResult.Unknown)
                    End If
                    Dim pk As New List(Of PKDesc)
                    For Each ff As IFilter In join.Condition.GetAllFilters
                        Dim jf As JoinFilter = TryCast(ff, JoinFilter)
                        If jf IsNot Nothing Then
                            If jf.Left.Property.Entity IsNot Nothing AndAlso jf.Left.Property.Entity = entity AndAlso
                                jf.Right.Property.Entity IsNot Nothing AndAlso jf.Right.Property.Entity = objEU AndAlso
                                pks.Any(Function(it) it.PropertyAlias = jf.Left.Property.PropertyAlias) Then

                                Dim id As Object = ObjectMappingEngine.GetPropertyValue(obj, jf.Right.Property.PropertyAlias, oschema)
                                Dim r As _ICachedEntity = TryCast(id, _ICachedEntity)
                                If r IsNot Nothing Then
                                    pk.AddRange(OrmManager.GetPKValues(r, lschema))
                                    Exit For
                                Else
                                    pk.Add(New PKDesc(jf.Left.Property.PropertyAlias, id))
                                End If

                            ElseIf jf.Right.Property.Entity IsNot Nothing AndAlso jf.Right.Property.Entity = entity AndAlso
                                jf.Left.Property.Entity IsNot Nothing AndAlso jf.Left.Property.Entity = objEU AndAlso
                                pks.Any(Function(it) it.PropertyAlias = jf.Right.Property.PropertyAlias) Then

                                Dim id As Object = ObjectMappingEngine.GetPropertyValue(obj, jf.Left.Property.PropertyAlias, oschema)
                                Dim r As _ICachedEntity = TryCast(id, _ICachedEntity)
                                If r IsNot Nothing Then
                                    pk.AddRange(OrmManager.GetPKValues(r, lschema))
                                    Exit For
                                Else
                                    pk.Add(New PKDesc(jf.Right.Property.PropertyAlias, id))
                                End If

                            End If
                        Else
                            Dim ef As IEvaluableFilter = TryCast(ff, IEvaluableFilter)
                            If ef Is Nothing Then
                                Return New Pair(Of _IEntity, IEvaluableValue.EvalResult)(Nothing, IEvaluableValue.EvalResult.Unknown)
                            End If
                        End If
                    Next

                    If pk.Count > 0 Then
                        Dim jObj As ICachedEntity = OrmManager.CurrentManager.GetEntityFromCacheLoadedOrDB(pk.ToArray, rt)
                        If jObj IsNot Nothing Then
                            Dim newF As IFilter = join.Condition
                            For Each jf As JoinFilter In join.Condition.GetAllFilters.OfType(Of JoinFilter).
                                Where(Function(it) it.Left.Property.Entity IsNot Nothing AndAlso it.Right.Property.Entity IsNot Nothing)
                                If jf.Left.Property.Entity = entity Then
                                    Dim rootp As Pair(Of _IEntity, IEvaluableValue.EvalResult) = GetRoot(jf.Right.Property.Entity, roots, joins, mpe, jObj, entity, lschema)
                                    If rootp.First Is Nothing Then
                                        Return rootp
                                    End If
                                    Dim v As Object = ObjectMappingEngine.GetPropertyValue(rootp.First, jf.Right.Property.PropertyAlias,
                                                                                           mpe.GetEntitySchema(jf.Right.Property.Entity), Nothing)

                                    Dim ef As IFilter = Nothing
                                    Dim spke As ISinglePKEntity = TryCast(v, ISinglePKEntity)
                                    If spke IsNot Nothing Then
                                        ef = New EntityFilter(jf.Left.Property, New EntityValue(spke), FilterOperation.Equal)
                                    Else
                                        ef = New EntityFilter(jf.Left.Property, New ScalarValue(v), FilterOperation.Equal)
                                    End If

                                    newF = newF.ReplaceFilter(jf, ef)
                                ElseIf jf.Right.Property.Entity = entity Then
                                    Dim rootp As Pair(Of _IEntity, IEvaluableValue.EvalResult) = GetRoot(jf.Left.Property.Entity, roots, joins, mpe, jObj, entity, lschema)
                                    If rootp.First Is Nothing Then
                                        Return rootp
                                    End If
                                    Dim v As Object = ObjectMappingEngine.GetPropertyValue(rootp.First, jf.Left.Property.PropertyAlias,
                                                                                           mpe.GetEntitySchema(jf.Left.Property.Entity), Nothing)
                                    Dim ef As IFilter = Nothing
                                    Dim spke As ISinglePKEntity = TryCast(v, ISinglePKEntity)
                                    If spke IsNot Nothing Then
                                        ef = New EntityFilter(jf.Left.Property, New EntityValue(spke), FilterOperation.Equal)
                                    Else
                                        ef = New EntityFilter(jf.Left.Property, New ScalarValue(v), FilterOperation.Equal)
                                    End If
                                    newF = newF.ReplaceFilter(jf, ef)
                                End If
                            Next

                            Dim evalNewF As IEvaluableFilter = TryCast(newF, IEvaluableFilter)
                            If evalNewF IsNot Nothing Then
                                Dim res As IEvaluableValue.EvalResult = evalNewF.Eval(mpe,
                                              Function(efb As IEntityFilterBase)
                                                  Dim tmpl As OrmFilterTemplate = TryCast(efb.GetFilterTemplate, OrmFilterTemplate)
                                                  Dim o_ As _IEntity = Nothing
                                                  Dim ss As IEntitySchema = Nothing
                                                  If tmpl IsNot Nothing Then
                                                      If tmpl.ObjectSource = entity Then
                                                          o_ = jObj
                                                          ss = lschema
                                                      Else
                                                          If roots.TryGetValue(tmpl.ObjectSource, o_) Then
                                                              ss = mpe.GetEntitySchema(tmpl.ObjectSource)
                                                          End If
                                                      End If
                                                  End If
                                                  Return New Pair(Of _IEntity, IEntitySchema)(o_, ss)
                                              End Function, joins, objEU)
                                If res = IEvaluableValue.EvalResult.Found Then
                                    roots.Add(entity, jObj)

                                    Return New Pair(Of _IEntity, IEvaluableValue.EvalResult)(jObj, IEvaluableValue.EvalResult.Found)
                                End If

                                Return New Pair(Of _IEntity, IEvaluableValue.EvalResult)(Nothing, res)
                            End If
                        ElseIf join.JoinType = JoinType.RightOuterJoin Then
                            Return New Pair(Of _IEntity, IEvaluableValue.EvalResult)(Nothing, IEvaluableValue.EvalResult.NotFound)
                        End If
                    End If
                End If

                Return New Pair(Of _IEntity, IEvaluableValue.EvalResult)(Nothing, IEvaluableValue.EvalResult.Unknown)
            End If

            Return New Pair(Of _IEntity, IEvaluableValue.EvalResult)(oo, IEvaluableValue.EvalResult.Found)
        End Function

    End Class

    <Serializable()> _
    Public Class TableFilter
        Inherits Worm.Criteria.Core.TemplatedFilterBase
        Private Const TempTable As String = "calculated"
        'Implements ITemplateFilter

        'Private _templ As TableFilterTemplate

        Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal value As IFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
            MyBase.New(value, New TableFilterTemplate(table, column, operation))
            '_templ = New TableFilterTemplate(table, column, operation)
        End Sub

        Public Sub New(ByVal column As String, ByVal value As IFilterValue, ByVal operation As Worm.Criteria.FilterOperation)
            MyBase.New(value, New TableFilterTemplate(New SourceFragment(TempTable), column, operation))
            '_templ = New TableFilterTemplate(table, column, operation)
        End Sub

        Friend Sub New(ByVal v As IFilterValue, ByVal template As TemplateBase)
            MyBase.New(v, template)
        End Sub

        Protected Overrides Function _ToString() As String
            'Return _templ.Table.TableName & _templ.Column & Value._ToString & _templ.OperToString
            Return Value._ToString & Template._ToString()
        End Function

        'Public Overrides Function GetStaticString() As String
        '    Return _templ.Table.TableName() & _templ.Column & _templ.Oper2String
        'End Function

        Public Shadows ReadOnly Property Template() As TableFilterTemplate
            Get
                Return CType(MyBase.Template, TableFilterTemplate)
            End Get
        End Property

        'Public Overloads Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam) As String
        '    Return MakeQueryStmt(schema, stmt, filterInfo, almgr, pname, Nothing)
        'End Function

        Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef,
                                                ByVal stmt As StmtGenerator, ByVal executor As Query.IExecutionContext,
                                                ByVal contextInfo As IDictionary, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As String
            'Dim pf As IParamFilterValue = TryCast(Value, IParamFilterValue)

            If Value.ShouldUse Then
                If Template.Table.Name = TempTable Then
                    Return Template.Column & Template.OperToStmt(stmt) & GetParam(schema, fromClause, stmt, pname, False, almgr, contextInfo, executor)
                Else
                    'Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = Nothing

                    'If almgr IsNot Nothing Then
                    '    tableAliases = almgr.Aliases
                    'End If

                    Dim map As New MapField2Column(String.Empty, Template.Column, Template.Table)
                    Dim [alias] As String = String.Empty

                    If almgr IsNot Nothing Then
                        'Debug.Assert(almgr.ContainsKey(map.Table, _eu), "There is not alias for table " & map.Table.RawName)
                        Try
                            [alias] = almgr.GetAlias(map.Table, _eu) & stmt.Selector
                        Catch ex As KeyNotFoundException
                            Throw New ObjectMappingException("There is not alias for table " & map.Table.RawName, ex)
                        End Try
                    End If

                    Return [alias] & map.SourceFieldExpression & Template.OperToStmt(stmt) & GetParam(schema, fromClause, stmt, pname, False, almgr, contextInfo, executor)
                End If
            Else
                Return String.Empty
            End If
        End Function

        Public Overrides Function GetAllFilters() As IFilter()
            Return New TableFilter() {Me}
        End Function

        Public Overrides Function MakeSingleQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, _
            ByVal almgr As IPrepareTable, ByVal pname As ICreateParam, ByVal executor As Query.IExecutionContext) As Pair(Of String)
            If schema Is Nothing Then
                Throw New ArgumentNullException("schema")
            End If

            If pname Is Nothing Then
                Throw New ArgumentNullException("pname")
            End If

            Dim prname As String = Value.GetParam(schema, Nothing, stmt, pname, Nothing, Nothing, Nothing, False, Nothing)

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

        Public Overrides Function ToStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary) As String
            'Dim o As Object = _t
            'If o Is Nothing Then
            '    o = _tbl.TableName
            'End If
            'Return String.Format(_format, o.ToString, _field) & TemplateBase.Oper2String(_oper)
            Return Value.GetStaticString(mpe, contextInfo) & Template.GetStaticString(mpe, contextInfo)
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
            Dim c As New CustomFilter(CType(Template, CustomValue), Value)
            Return c
        End Function

        Public Overrides Function MakeSingleQueryStmt(ByVal schema As ObjectMappingEngine, ByVal stmt As StmtGenerator, _
            ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam, ByVal executor As Query.IExecutionContext) As Pair(Of String)
            Throw New NotImplementedException
        End Function
    End Class

    <Serializable()> _
    Public Class ExpressionFilter
        Implements IFilter
        Implements IEvaluableFilter

        Private _fo As FilterOperation
        Private _left As UnaryExp
        Private _right As UnaryExp
        Private _eu As EntityUnion

        Public Function SetUnion(ByVal eu As Query.EntityUnion) As IFilter Implements IFilter.SetUnion
            Throw New NotImplementedException
        End Function

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

        Public Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef,
                                      ByVal stmt As StmtGenerator, ByVal executor As IExecutionContext, ByVal contextInfo As IDictionary,
                                      ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam) As String Implements IFilter.MakeQueryStmt
            Return Left.MakeStmt(schema, fromClause, stmt, pname, almgr, contextInfo, False, executor) & stmt.Oper2String(Operation) & Right.MakeStmt(schema, fromClause, stmt, pname, almgr, contextInfo, False, executor)
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

        Public Function GetAllFilters() As IFilter() Implements IFilter.GetAllFilters
            Return New IFilter() {Me}
        End Function

        'Public Function MakeQueryStmt(ByVal schema As QueryGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam) As String Implements IFilter.MakeQueryStmt
        '    'Dim columns As List(Of String)
        '    'Return _left.MakeStmt(schema, pname, almgr, columns)
        '    Throw New NotSupportedException("Use MakeQueryStmt with columns parameter")
        'End Function

        Public Function ReplaceFilter(ByVal oldValue As IFilter, ByVal newValue As IFilter) As IFilter Implements IFilter.ReplaceFilter
            If Equals(oldValue) Then
                Return newValue
            End If
            Return Nothing
        End Function

        Public Function ToStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary) As String Implements IFilter.GetStaticString
            Return _left.ToStaticString(mpe, contextInfo) & _fo.ToString & _right.ToStaticString(mpe, contextInfo)
        End Function

        Protected Function _ToString() As String Implements IFilter._ToString
            Return _left._ToString & _fo.ToString & _right._ToString
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

        Public Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements Values.IQueryElement.Prepare
            If _left IsNot Nothing Then
                _left.Prepare(executor, schema, contextInfo, stmt, isAnonym)
            End If
            If _right IsNot Nothing Then
                _right.Prepare(executor, schema, contextInfo, stmt, isAnonym)
            End If
        End Sub

        Public Function RemoveFilter(ByVal f As IFilter) As IFilter Implements IFilter.RemoveFilter
            If _left IsNot Nothing AndAlso _left.Equals(f) Then
                Return Nothing
                'Throw New InvalidOperationException("Cannot remove self")
            ElseIf _right IsNot Nothing AndAlso _right.Equals(f) Then
                Return Nothing
                'Throw New InvalidOperationException("Cannot remove self")
            End If
            Return Me
        End Function

        Public Function Eval(mpe As ObjectMappingEngine, d As GetObj4IEntityFilterDelegate,
                              joins() As Joins.QueryJoin, objEU As EntityUnion) As Values.IEvaluableValue.EvalResult Implements IEvaluableFilter.Eval
            If _right IsNot Nothing Then
                Dim le As IEvaluableValue = TryCast(_left.Value, IEvaluableValue)
                If le IsNot Nothing Then
                    Return le.Eval(_right.Value, mpe, New OrmFilterTemplate(Nothing, FilterOperation.Equal))
                End If
            End If

            Return IEvaluableValue.EvalResult.Unknown
        End Function
    End Class

    <Serializable()> _
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

        Public Overrides Function GetAllFilters() As IFilter()
            Return New NonTemplateUnaryFilter() {Me}
        End Function

        Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef,
                                                ByVal stmt As StmtGenerator, ByVal executor As IExecutionContext, ByVal contextInfo As IDictionary,
                                                ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As String
            'Return TemplateBase.Oper2String(_oper) & GetParam(schema, pname)
            'Dim id As Values.IDatabaseFilterValue = TryCast(val, Values.IDatabaseFilterValue)
            'If id IsNot Nothing Then
            Return stmt.Oper2String(_oper) & Value.GetParam(schema, fromClause, stmt, pname, almgr, Nothing, contextInfo, False, executor)
            'Else
            'Return MakeQueryStmt(schema, filterInfo, almgr, pname, columns)
            'End If
        End Function

        Public Overrides Function ToStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary) As String
            Dim v As INonTemplateValue = TryCast(val, INonTemplateValue)
            If v Is Nothing Then
                Throw New NotImplementedException("Value is not implement INonTemplateValue")
            End If
            Return v.GetStaticString(mpe, contextInfo) & "$" & Worm.Criteria.Core.TemplateBase.OperToStringInternal(_oper)
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

    Public Class AggFilter
        Inherits FilterBase

        Private _exp As AggregateExpression
        Private _fo As FilterOperation

        Public Sub New(ByVal exp As AggregateExpression, ByVal fo As FilterOperation, ByVal val As IFilterValue)
            MyBase.New(val)
            _exp = exp
            _fo = fo
        End Sub

        Protected Overrides Function _Clone() As Object 'Implements System.ICloneable.Clone
            Return New AggFilter(_exp, _fo, Value)
        End Function

        'Public Overloads Function Clone() As IFilter Implements IFilter.Clone
        '    Return New AggFilter(_agg, _fo)
        'End Function

        'Public Overloads Function Equals(ByVal f As IFilter) As Boolean Implements IFilter.Equals
        '    Dim fl As AggFilter = TryCast(f, AggFilter)
        '    If fl Is Nothing Then
        '        Return False
        '    End If
        '    Return _agg.Equals(fl._agg) AndAlso _fo = fl._fo
        'End Function

        Public Overrides Function GetAllFilters() As IFilter() 'Implements IFilter.GetAllFilters
            Return New IFilter() {Me}
        End Function

        Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef,
                                                ByVal stmt As StmtGenerator, ByVal executor As Query.IExecutionContext, _
            ByVal contextInfo As IDictionary, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam) As String 'Implements IFilter.MakeQueryStmt

            Return _exp.MakeStatement(schema, fromClause, stmt, pname, almgr, contextInfo, MakeStatementMode.None, executor) & stmt.Oper2String(_fo) & Value.GetParam(schema, fromClause, stmt, pname, almgr, Nothing, contextInfo, False, executor)
        End Function

        'Public Function ReplaceFilter(ByVal replacement As IFilter, ByVal replacer As IFilter) As IFilter Implements IFilter.ReplaceFilter
        '    If Equals(replacement) Then
        '        Return replacer
        '    End If
        '    Return Nothing
        'End Function

        'Public ReadOnly Property Filter() As IFilter Implements IGetFilter.Filter
        '    Get
        '        Return Me
        '    End Get
        'End Property

        Protected Overrides Function _ToString() As String 'Implements Values.IQueryElement._ToString
            Return _exp.GetDynamicString & TemplateBase.OperToStringInternal(_fo)
        End Function

        Public Overrides Function ToStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary) As String 'Implements Values.IQueryElement.GetStaticString
            Return _exp.GetStaticString(mpe, contextInfo) & TemplateBase.OperToStringInternal(_fo)
        End Function

        Public Overrides Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) 'Implements Values.IQueryElement.Prepare
            _exp.Prepare(executor, schema, contextInfo, stmt, isAnonym)
            MyBase.Prepare(executor, schema, contextInfo, stmt, isAnonym)
        End Sub
    End Class

    Public Class QueryFilter
        Inherits FilterBase

        Private _cmd As QueryCmd
        Private _fo As FilterOperation

        Public Sub New(ByVal cmd As QueryCmd, ByVal fo As FilterOperation, ByVal val As IFilterValue)
            MyBase.New(val)
            _cmd = cmd
            _fo = fo
        End Sub

        Protected Overrides Function _Clone() As Object 'Implements System.ICloneable.Clone
            Return New QueryFilter(_cmd, _fo, Value)
        End Function

        'Public Overloads Function Clone() As IFilter Implements IFilter.Clone
        '    Return New AggFilter(_agg, _fo)
        'End Function

        'Public Overloads Function Equals(ByVal f As IFilter) As Boolean Implements IFilter.Equals
        '    Dim fl As AggFilter = TryCast(f, AggFilter)
        '    If fl Is Nothing Then
        '        Return False
        '    End If
        '    Return _agg.Equals(fl._agg) AndAlso _fo = fl._fo
        'End Function

        Public Overrides Function GetAllFilters() As IFilter() 'Implements IFilter.GetAllFilters
            Return New IFilter() {Me}
        End Function

        Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef,
                                                ByVal stmt As StmtGenerator, ByVal executor As Query.IExecutionContext, _
            ByVal contextInfo As IDictionary, ByVal almgr As IPrepareTable, ByVal pname As Entities.Meta.ICreateParam) As String 'Implements IFilter.MakeQueryStmt

            Return "(" & stmt.MakeQueryStatement(schema, fromClause, contextInfo, _cmd, pname, almgr) & ")" & stmt.Oper2String(_fo) & Value.GetParam(schema, fromClause, stmt, pname, almgr, Nothing, contextInfo, False, executor)
        End Function

        'Public Function ReplaceFilter(ByVal replacement As IFilter, ByVal replacer As IFilter) As IFilter Implements IFilter.ReplaceFilter
        '    If Equals(replacement) Then
        '        Return replacer
        '    End If
        '    Return Nothing
        'End Function

        'Public ReadOnly Property Filter() As IFilter Implements IGetFilter.Filter
        '    Get
        '        Return Me
        '    End Get
        'End Property

        Protected Overrides Function _ToString() As String 'Implements Values.IQueryElement._ToString
            Return _cmd._ToString & TemplateBase.OperToStringInternal(_fo)
        End Function

        Public Overrides Function ToStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary) As String 'Implements Values.IQueryElement.GetStaticString
            Return _cmd.GetStaticString(mpe, contextInfo) & TemplateBase.OperToStringInternal(_fo)
        End Function

        Public Overrides Sub Prepare(ByVal executor As Query.IExecutor, ByVal schema As ObjectMappingEngine, ByVal contextInfo As IDictionary, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) 'Implements Values.IQueryElement.Prepare
            _cmd.Prepare(executor, schema, contextInfo, stmt, isAnonym)
            MyBase.Prepare(executor, schema, contextInfo, stmt, isAnonym)
        End Sub
    End Class
End Namespace
