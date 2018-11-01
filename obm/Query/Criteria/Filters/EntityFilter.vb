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
                              joins As IEnumerable(Of Joins.QueryJoin), objEU As EntityUnion) As IEvaluableValue.EvalResult Implements IEntityFilter.EvalObj
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
l1:
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
                ElseIf (t.IsSubclassOf(rt) OrElse rt.IsSubclassOf(t)) AndAlso oschema.FieldColumnMap.TryGetValue(Template.PropertyAlias, Nothing) Then
                    Dim r As IEvaluableValue.EvalResult = IEvaluableValue.EvalResult.NotFound
                    Dim v As Object = ObjectMappingEngine.GetPropertyValue(obj, Template.PropertyAlias, oschema, Nothing) 'schema.GetFieldValue(obj, _fieldname)
                    r = evval.Eval(v, mpe, Template)
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

                    If o Is Nothing Then
                        o = obj.GetJoinObj(rt, oschema)
                    End If

                    If o IsNot Nothing Then
                        Return Eval(mpe, o, mpe.GetEntitySchema(rt), joins, Template.ObjectSource)
                    ElseIf rt.IsAssignableFrom(t) Then
                        GoTo l1
                    Else
                        Dim objr = TryCast(obj, IRelations)
                        If objr IsNot Nothing Then
                            Dim rel = objr.GetRelation(rt)
                            If rel IsNot Nothing Then
                                Dim cmd = objr.GetCmd(rel.Relation)
                                cmd.IncludeModifiedObjects = True
                                For Each o In cmd.ToList
                                    Dim r = Eval(mpe, o, mpe.GetEntitySchema(rt), joins, Template.ObjectSource)
                                    Select Case r
                                        Case IEvaluableValue.EvalResult.Found
                                            Return IEvaluableValue.EvalResult.Found
                                        Case IEvaluableValue.EvalResult.Unknown
                                            Return IEvaluableValue.EvalResult.Unknown
                                    End Select
                                Next
                                Return IEvaluableValue.EvalResult.NotFound
                                'Dim ev As Boolean = False

                                'OrmManager.ApplyFilter(rt, cmd.ToList, Me, joins, mpe, Template.ObjectSource, 0, Integer.MaxValue, ev)
                            End If
                        End If
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
                Return _s.ChangeValueType(_pa, v)
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
                    map = New MapField2Column(Template.PropertyAlias, CType(q, Query.IExecutionContext).FindColumn(schema, Template.PropertyAlias), tbl)
                Else
                    Try
                        If executor Is Nothing Then
                            map = oschema.FieldColumnMap(Template.PropertyAlias)
                        Else
                            map = executor.GetFieldColumnMap(oschema, t)(Template.PropertyAlias)
                        End If
                        tbl = map.Table
                    Catch ex As KeyNotFoundException
                        Throw New ObjectMappingException(String.Format("There is no column for property {0} ", Template.ObjectSource.ToStaticString(schema) & "." & Template.PropertyAlias, ex))
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
                If oschema.GetPropertyTypeByName(rt, Template.PropertyAlias) Is GetType(Byte()) Then
                    pname.GetParameter(prname).DbType = System.Data.DbType.Binary
                ElseIf oschema.GetPropertyTypeByName(rt, Template.PropertyAlias) Is GetType(Decimal) Then
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
            If IsHashable Then
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
            Return Clone()
        End Function

        Public Overloads Function Clone() As EntityFilter
            Dim vc As IFilterValue = Nothing
            If val IsNot Nothing Then
                vc = CType(val.Clone, IFilterValue)
            End If
            Return New EntityFilter(vc, CType(Template, OrmFilterTemplate))
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
                              joins As IEnumerable(Of Joins.QueryJoin), objEU As EntityUnion) As Values.IEvaluableValue.EvalResult Implements IEvaluableFilter.Eval
            If d Is Nothing Then
                Return IEvaluableValue.EvalResult.Unknown
            End If

            Dim p As Pair(Of _IEntity, IEntitySchema) = d(Me)

            If p Is Nothing Then
                Return IEvaluableValue.EvalResult.Unknown
            End If

            Return Eval(mpe, p.First, p.Second, joins, objEU)
        End Function

        Private Shared Function GetRoot(entity As EntityUnion, roots As Dictionary(Of EntityUnion, _IEntity), joins As IEnumerable(Of QueryJoin),
                                        mpe As ObjectMappingEngine, obj As _IEntity, objEU As EntityUnion, oschema As IEntitySchema) As Pair(Of _IEntity, IEvaluableValue.EvalResult)
            Dim oo As _IEntity = Nothing
            If Not roots.TryGetValue(entity, oo) Then
                Dim rt As Type = entity.GetRealType(mpe)
                Dim lschema As IEntitySchema = mpe.GetEntitySchema(rt)
                Dim pks As IEnumerable(Of MapField2Column) = lschema.GetPKs()
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
                                    pk.AddRange(r.GetPKValues(lschema))
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
                                    pk.AddRange(r.GetPKValues(lschema))
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

        Public ReadOnly Property IsHashable As Boolean Implements IEntityFilterBase.IsHashable
            Get
                If Value IsNot Nothing Then

                    If Template.Operation = FilterOperation.Equal Then
                        Return GetType(ScalarValue).IsAssignableFrom(Value.GetType)
                    End If

                    'If Template.Operation >= FilterOperation.Equal AndAlso Template.Operation <= FilterOperation.LessThan Then
                    '    Return GetType(ScalarValue).IsAssignableFrom(Value.GetType)
                    'End If


                    'If Template.Operation = FilterOperation.In OrElse Template.Operation = FilterOperation.NotIn Then
                    '    Return GetType(InValue).IsAssignableFrom(Value.GetType)
                    'End If

                    'If Template.Operation = FilterOperation.Between Then
                    '    Return GetType(BetweenValue).IsAssignableFrom(Value.GetType)
                    'End If
                End If


                Return False

            End Get
        End Property
    End Class
End Namespace