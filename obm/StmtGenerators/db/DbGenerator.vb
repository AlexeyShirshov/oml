Imports Worm.Entities.Meta
Imports Worm.Query
Imports Worm.Criteria.Core
Imports Worm.Criteria.Conditions
Imports Worm.Expressions2
Imports System.Collections.Generic
Imports Worm.Criteria.Joins
Imports Worm.Entities
Imports dc = Worm.Criteria.Core
Imports Worm.Criteria.Values
Imports Worm.Criteria
Imports System.Runtime.CompilerServices
Imports System.Linq
Imports CoreFramework

Namespace Database

    Public Enum LockTypeEnum
        [Shared]
        Update
        IntentShared
        IntentExclusive
        Exclusive
    End Enum

    Public MustInherit Class DbGenerator
        Inherits StmtGenerator

        Public Overridable ReadOnly Property NamedParams As Boolean
            Get
                Return True
            End Get
        End Property

        Public Delegate Sub InfoMessageDelagate(args As EventArgs)

        Public MustOverride Function TestLockError(v As Object) As Boolean
        Public MustOverride Function GetLockCommand(pmgr As ICreateParam, name As String,
                                                    Optional lockTimeout As Integer? = Nothing,
                                                    Optional lockType As LockTypeEnum = LockTypeEnum.Exclusive) As System.Data.Common.DbCommand
        Public MustOverride Function ReleaseLockCommand(pmgr As ICreateParam, name As String) As System.Data.Common.DbCommand

        Public Overridable Function SupportMultiline() As Boolean
            Return False
        End Function
        Public Overrides Function BinaryOperator2String(ByVal oper As Expressions2.BinaryOperationType) As String
            Select Case oper
                Case Expressions2.BinaryOperationType.Equal
                    Return " = "
                Case Expressions2.BinaryOperationType.GreaterEqualThan
                    Return " >= "
                Case Expressions2.BinaryOperationType.GreaterThan
                    Return " > "
                Case Expressions2.BinaryOperationType.In
                    Return " in "
                Case Expressions2.BinaryOperationType.NotEqual
                    Return " <> "
                Case Expressions2.BinaryOperationType.NotIn
                    Return " not in "
                Case Expressions2.BinaryOperationType.LessEqualThan
                    Return " <= "
                Case Expressions2.BinaryOperationType.Like
                    Return " like "
                Case Expressions2.BinaryOperationType.LessThan
                    Return " < "
                Case Expressions2.BinaryOperationType.Is
                    Return " is "
                Case Expressions2.BinaryOperationType.IsNot
                    Return " is not "
                Case Expressions2.BinaryOperationType.Exists
                    Return " exists "
                Case Expressions2.BinaryOperationType.NotExists
                    Return " not exists "
                Case Expressions2.BinaryOperationType.Between
                    Return " between "
                Case Expressions2.BinaryOperationType.And
                    Return " and "
                Case Expressions2.BinaryOperationType.Or
                    Return " or "
                Case Expressions2.BinaryOperationType.ExclusiveOr
                    Return "^"
                Case Expressions2.BinaryOperationType.BitAnd
                    Return "&"
                Case Expressions2.BinaryOperationType.BitOr
                    Return "|"
                Case Expressions2.BinaryOperationType.Add
                    Return "+"
                Case Expressions2.BinaryOperationType.Subtract
                    Return "-"
                Case Expressions2.BinaryOperationType.Divide
                    Return "/"
                Case Expressions2.BinaryOperationType.Multiply
                    Return "*"
                Case Expressions2.BinaryOperationType.Modulo
                    Return "%"
                Case Else
                    Throw New ObjectMappingException("invalid opration " & oper.ToString)
            End Select
        End Function

        Public Overridable ReadOnly Property EndLine() As String
            Get
                Return vbCrLf
            End Get
        End Property

        Public Overrides Function Comment(ByVal s As String) As String
            Return "/*" & EndLine & s & EndLine & "*/" & EndLine
        End Function

        Public Overrides Function CreateExecutor() As Worm.Query.IExecutor
            Return New Worm.Query.Database.DbQueryExecutor
        End Function

        Public Overridable ReadOnly Property DefaultValue() As String
            Get
                Return "default"
            End Get
        End Property

        Protected Overridable Function DefaultValues() As String
            Return "default values"
        End Function

        Public Overrides ReadOnly Property GetYear() As String
            Get
                Return "year({0})"
            End Get
        End Property

        Public Overrides ReadOnly Property Left() As String
            Get
                Return "left({0},{1})"
            End Get
        End Property

        Public Overridable ReadOnly Property LastInsertID() As String
            Get
                Return "scope_identity()"
            End Get
        End Property

        Public Overridable ReadOnly Property RowCount() As String
            Get
                Return "@@rowcount"
            End Get
        End Property

        Public Overrides Function MakeQueryStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef,
                                                     ByVal contextInfo As IDictionary,
            ByVal query As Worm.Query.QueryCmd, ByVal params As ICreateParam,
            ByVal almgr As IPrepareTable) As String

            Return Worm.Query.Database.DbQueryExecutor.MakeQueryStatement(mpe, contextInfo, Me, query, params, almgr)
        End Function

        Public Overrides Function Oper2String(ByVal oper As Worm.Criteria.FilterOperation) As String
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

        Public Overrides ReadOnly Property SupportParams() As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overridable ReadOnly Property SupportIf() As Boolean
            Get
                Return True
            End Get
        End Property
        Public Overridable ReadOnly Property SupportLimitDelete As Boolean
            Get
                Return False
            End Get
        End Property
        Protected Overridable Function InsertOutput(ByVal table As String, ByVal syncInsertPK As IEnumerable(Of ColType), ByVal notSyncInsertPK As List(Of ITemplateFilterBase.ColParam), ByVal co As IChangeOutputOnInsert) As String
            Return String.Empty
        End Function

        Protected Overridable Function DeclareOutput(ByVal sb As StringBuilder, ByVal syncInsertPK As IEnumerable(Of ColType)) As String
            Return Nothing
        End Function

        Public Overrides Function UnaryOperator2String(ByVal oper As Expressions2.UnaryOperationType) As String
            Select Case oper
                Case Expressions2.UnaryOperationType.Negate
                    Return "-"
                Case Expressions2.UnaryOperationType.Not
                    Return "~"
                Case Else
                    Throw New ObjectMappingException("invalid opration " & oper.ToString)
            End Select
        End Function

        Public Overridable Function AppendWhere(ByVal mpe As ObjectMappingEngine, ByVal t As Type, ByVal filter As Worm.Criteria.Core.IFilter,
            ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal contextInfo As IDictionary, ByVal pmgr As ICreateParam) As Boolean
            Dim schema As IEntitySchema = Nothing
            If t IsNot Nothing Then
                schema = mpe.GetEntitySchema(t)
            End If

            Return AppendWhere(mpe, t, schema, filter, almgr, sb, contextInfo, pmgr)
        End Function

        Public Overridable Function AppendWhere(ByVal mpe As ObjectMappingEngine, ByVal t As Type, ByVal schema As IEntitySchema, ByVal filter As Worm.Criteria.Core.IFilter,
            ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal contextInfo As IDictionary, ByVal pmgr As ICreateParam,
            Optional ByVal os As EntityUnion = Nothing) As Boolean

            Dim con As New Condition.ConditionConstructor
            con.AddFilter(filter)

            'If t IsNot Nothing Then
            '    Dim schema As IOrmObjectSchema = GetObjectSchema(t)
            '    con.AddFilter(schema.GetFilter(filter_info))
            'End If

            If schema IsNot Nothing AndAlso (os Is Nothing OrElse Not os.IsQuery) Then
                Dim cs As IContextObjectSchema = TryCast(schema, IContextObjectSchema)
                If cs IsNot Nothing Then
                    Dim f As IFilter = cs.GetContextFilter(contextInfo)
                    If f IsNot Nothing Then
                        If os IsNot Nothing Then
                            f.SetUnion(os)
                        End If
                        con.AddFilter(f)
                    End If
                End If
            End If

            If Not con.IsEmpty Then
                'Dim bf As Worm.Criteria.Core.IFilter = TryCast(con.Condition, Worm.Criteria.Core.IFilter)
                Dim f As IFilter = TryCast(con.Condition, IFilter)
                'If f IsNot Nothing Then
                Dim s As String = f.MakeQueryStmt(mpe, Nothing, Me, New ExecutorCtx(t, schema), contextInfo, almgr, pmgr)
                If Not String.IsNullOrEmpty(s) Then
                    sb.Append(" where ").Append(s)
                End If
                'Else
                '    sb.Append(" where ").Append(bf.MakeQueryStmt(Me, pmgr))
                'End If
                Return True
            End If
            Return False
        End Function

        Public Function [Select](ByVal mpe As ObjectMappingEngine, ByVal original_type As Type,
            ByVal almgr As AliasMgr, ByVal params As ParamMgr,
            ByVal arr As Generic.IList(Of EntityExpression),
            ByVal additionalColumns As String, ByVal contextInfo As IDictionary) As String
            Return SelectWithJoin(mpe, original_type, almgr, params, Nothing, True, Nothing, additionalColumns, contextInfo, arr)
        End Function

        Public Overridable Function Delete(ByVal mpe As ObjectMappingEngine, ByVal sf As SourceFragment, ByVal filter As IFilter, ByVal params As ParamMgr,
                                           ByVal contextInfo As IDictionary) As String
            If sf Is Nothing Then
                Throw New ArgumentNullException("t parameter cannot be nothing")
            End If

            If filter Is Nothing Then
                Throw New ArgumentNullException("filter parameter cannot be nothing")
            End If

            Dim del_cmd As New StringBuilder
            del_cmd.Append("delete from ").Append(GetTableName(sf, contextInfo))
            del_cmd.Append(" where ").Append(filter.MakeQueryStmt(mpe, Nothing, Me, Nothing, contextInfo, Nothing, params))

            Return del_cmd.ToString
        End Function
        Public Overridable Function Delete(ByVal mpe As ObjectMappingEngine, ByVal sf As SourceFragment, ByVal filter As IFilter, ByVal params As ParamMgr,
                                           ByVal contextInfo As IDictionary, limit As Integer) As String
            Throw New NotSupportedException
        End Function
        Public Overridable Function Delete(ByVal mpe As ObjectMappingEngine, ByVal type As Type, ByVal filter As IFilter,
                                           joins() As QueryJoin,
                                           ByVal params As ParamMgr,
                                           ByVal contextInfo As IDictionary) As String
            Dim del_cmd As New StringBuilder
            Dim almgr = AliasMgr.Create
            Dim tables = mpe.GetTables(type)
            Dim schema = mpe.GetEntitySchema(type)

            AppendFrom(mpe, almgr, contextInfo, tables, del_cmd, params, TryCast(schema, IMultiTableObjectSchema), type)

            Dim tblAlias = almgr.GetAlias(tables(0), Nothing)

            del_cmd.Insert(0, "delete " & tblAlias & " from ")

            If joins IsNot Nothing Then
                For i As Integer = 0 To joins.Length - 1
                    Dim join As QueryJoin = CType(joins(i), QueryJoin)

                    If Not QueryJoin.IsEmpty(join) Then
                        'almgr.AddTable(join.Table, CType(Nothing, ParamMgr))
                        join.MakeSQLStmt(mpe, Nothing, Me, Nothing, contextInfo, almgr, params, Nothing, del_cmd)
                    End If
                Next
            End If

            AppendWhere(mpe, type, schema, filter, almgr, del_cmd, contextInfo, params)

            Return del_cmd.ToString
        End Function
        Public Overridable Function Delete(ByVal mpe As ObjectMappingEngine, ByVal type As Type, ByVal filter As IFilter,
                                           joins() As QueryJoin,
                                           ByVal params As ParamMgr,
                                           ByVal contextInfo As IDictionary, limit As Integer) As String
            Throw New NotSupportedException
        End Function
        Public Overridable Function SelectWithJoin(ByVal mpe As ObjectMappingEngine, ByVal original_type As Type,
            ByVal almgr As IPrepareTable, ByVal params As ICreateParam, ByVal joins() As Worm.Criteria.Joins.QueryJoin,
            ByVal wideLoad As Boolean, ByVal empty As Object, ByVal additionalColumns As String,
            ByVal contextInfo As IDictionary, ByVal selectedProperties As Generic.IList(Of EntityExpression)) As String

            If original_type Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "original_type")
            End If

            If almgr Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "almgr")
            End If

            Dim schema As IEntitySchema = mpe.GetEntitySchema(original_type)

            Return SelectWithJoin(mpe, original_type, schema.GetTables(), almgr, params, joins, wideLoad, Nothing, additionalColumns, selectedProperties, schema, contextInfo)
        End Function

        Public Overridable Function SelectWithJoin(ByVal mpe As ObjectMappingEngine, ByVal original_type As Type, ByVal tables() As SourceFragment,
            ByVal almgr As IPrepareTable, ByVal params As ICreateParam, ByVal joins() As Worm.Criteria.Joins.QueryJoin,
            ByVal wideLoad As Boolean, ByVal empty As Object, ByVal additionalColumns As String,
            ByVal selectedProperties As Generic.IList(Of EntityExpression), ByVal schema As IEntitySchema, ByVal contextInfo As IDictionary) As String

            Dim selectcmd As New StringBuilder
            'Dim pmgr As ParamMgr = params 'New ParamMgr()

            AppendFrom(mpe, almgr, contextInfo, tables, selectcmd, params,
                           TryCast(schema, IMultiTableObjectSchema), original_type)
            If joins IsNot Nothing Then
                For i As Integer = 0 To joins.Length - 1
                    Dim join As QueryJoin = CType(joins(i), QueryJoin)

                    If Not QueryJoin.IsEmpty(join) Then
                        'almgr.AddTable(join.Table, CType(Nothing, ParamMgr))
                        join.MakeSQLStmt(mpe, Nothing, Me, Nothing, contextInfo, almgr, params, Nothing, selectcmd)
                    End If
                Next
            End If

            Dim selSb As New StringBuilder
            selSb.Append("select ")

            If original_type IsNot Nothing Then
                Dim exec As New ExecutorCtx(original_type, schema)
                If wideLoad Then
                    'Dim columns As String = mpe.GetSelectColumnList(original_type, mpe, arr, schema, Nothing)
                    'If Not String.IsNullOrEmpty(columns) Then
                    '    selSb.Append(columns)
                    '    If Not String.IsNullOrEmpty(additionalColumns) Then
                    '        selSb.Append(",").Append(additionalColumns)
                    '    End If
                    'Else

                    'End If
                    If selectedProperties Is Nothing Then
                        selectedProperties = New List(Of EntityExpression)
                        For Each m As MapField2Column In schema.FieldColumnMap
                            selectedProperties.Add(New EntityExpression(m.PropertyAlias, original_type))
                        Next
                    End If
                    selSb.Append(BinaryExpressionBase.CreateFromEnumerable(selectedProperties).MakeStatement(mpe, Nothing, Me, params, almgr, contextInfo, MakeStatementMode.Select And MakeStatementMode.AddColumnAlias, exec))
                Else
                    'mpe.GetPKList(original_type, mpe, schema, selSb, Nothing)
                    'Dim l As New List(Of EntityExpression)
                    'For Each mp As MapField2Column In schema.GetPKs
                    '    l.Add(New EntityExpression(mp.PropertyAlias, original_type))
                    'Next
                    selSb.Append(New EntityExpression(schema.GetPK.PropertyAlias, original_type).MakeStatement(mpe, Nothing, Me, params, almgr, contextInfo, MakeStatementMode.Select And MakeStatementMode.AddColumnAlias, exec))
                End If
            Else
                selSb.Append("*")
            End If

            selSb.Append(" from ")
            Return selectcmd.Insert(0, selSb.ToString).ToString
        End Function

        ''' <summary>
        ''' Построение таблиц, включая джоины
        ''' </summary>
        ''' <param name="almgr"></param>
        ''' <param name="tables"></param>
        ''' <param name="selectcmd"></param>
        ''' <param name="pname"></param>
        ''' <param name="sch"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Friend Function AppendFrom(ByVal mpe As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal contextInfo As IDictionary,
            ByVal tables As IEnumerable(Of SourceFragment), ByVal selectcmd As StringBuilder, ByVal pname As ICreateParam,
            ByVal sch As IMultiTableObjectSchema, ByVal t As Type) As StringBuilder
            'Dim sch As IOrmObjectSchema = GetObjectSchema(original_type)
            For i As Integer = 0 To tables.Count - 1
                Dim tbl As SourceFragment = tables(i)
                Dim tbl_real As SourceFragment = tbl
                Dim [alias] As String = Nothing
                'If Not almgr.Aliases.TryGetValue(tbl, [alias]) Then
                '    [alias] = almgr.AddTable(tbl_real, pname)
                'Else
                '    tbl_real = tbl.OnTableAdd(pname)
                '    If tbl_real Is Nothing Then
                '        tbl_real = tbl
                '    End If
                'End If
                If Not almgr.ContainsKey(tbl_real, Nothing) Then
                    [alias] = almgr.AddTable(tbl_real, Nothing, pname)
                Else
                    tbl_real = tbl.OnTableAdd(pname)
                    If tbl_real Is Nothing Then
                        tbl_real = tbl
                    End If
                End If

                'selectcmd = selectcmd.Replace(tbl.TableName & ".", [alias] & ".")
                'almgr.Replace(mpe, Me, tbl, Nothing, selectcmd)

                If i = 0 Then
                    selectcmd.Append(GetTableName(tbl_real, contextInfo)).Append(" ").Append([alias])
                End If

                If sch IsNot Nothing Then
                    For j As Integer = i + 1 To tables.Count - 1
                        Dim join As QueryJoin = CType(mpe.GetJoins(sch, tbl, tables(j), contextInfo), QueryJoin)

                        If Not QueryJoin.IsEmpty(join) Then
                            If Not almgr.ContainsKey(tables(j), Nothing) Then
                                almgr.AddTable(tables(j), Nothing, pname)
                            End If
                            join.MakeSQLStmt(mpe, Nothing, Me, New ExecutorCtx(t, sch), contextInfo, almgr, pname, Nothing, selectcmd)
                        End If
                    Next
                End If
            Next

            Return selectcmd
        End Function

        Public Function SaveM2M(ByVal mpe As ObjectMappingEngine, ByVal obj As ISinglePKEntity,
            ByVal relation As M2MRelationDesc, ByVal entry As M2MRelation,
            ByVal pmgr As ParamMgr, ByVal contextInfo As IDictionary) As String

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            If relation Is Nothing Then
                Throw New ArgumentNullException("relation")
            End If

            If entry Is Nothing Then
                Throw New ArgumentNullException("entry")
            End If

            If Not SupportMultiline() Then
                Throw New DbGeneratorException("Generator doesn't support multiline statements")
            End If

            Dim almgr As IPrepareTable = AliasMgr.Create
            Dim sb As New StringBuilder
            Dim tbl As SourceFragment = relation.Table
            Dim param_relation As M2MRelationDesc = mpe.GetRevM2MRelation(obj.GetType, relation.Entity.GetRealType(mpe), relation.Key)
            Dim al As String = almgr.AddTable(tbl, Nothing)

            If param_relation Is Nothing Then
                Throw New ArgumentException("Invalid relation")
            End If

            Dim pk As String = Nothing
            Dim le As ILastError = TryCast(Me, ILastError)
            If entry.HasDeleted Then
                sb.Append("delete ").Append(al).Append(" from ").Append(GetTableName(tbl, contextInfo)).Append(" ").Append(al)
                sb.Append(" where ").Append(al).Append(".").Append(param_relation.Column).Append(" = ")
                pk = pmgr.AddParam(pk, obj.Identifier)
                sb.Append(pk).Append(" and ").Append(al).Append(".").Append(relation.Column).Append(" in(")
                For Each toDel As ISinglePKEntity In entry.Deleted
                    sb.Append(toDel.Identifier).Append(",")
                Next
                sb.Length -= 1
                sb.Append(")")
                If relation.Constants IsNot Nothing Then
                    For Each f As IFilter In relation.Constants
                        sb.Append(" and ")
                        sb.Append(f.MakeQueryStmt(mpe, Nothing, Me, Nothing, Nothing, almgr, pmgr))
                    Next
                End If

                sb.Append(EndLine)

                If entry.HasAdded AndAlso le IsNot Nothing Then
                    sb.Append("if ").Append(le.LastError).AppendLine(" = 0 begin")
                End If
            End If

            If entry.HasAdded Then
                For Each toAdd As ISinglePKEntity In entry.Added
                    If entry.HasDeleted Then
                        sb.Append(vbTab)
                    End If
                    If le IsNot Nothing Then
                        sb.Append("if ").Append(le.LastError).Append(" = 0 ")
                    End If
                    sb.Append("insert into ").Append(GetTableName(tbl, contextInfo)).Append("(")
                    sb.Append(param_relation.Column).Append(",").Append(relation.Column)
                    Dim consts As New List(Of String)
                    If relation.Constants IsNot Nothing Then
                        For Each f As ITemplateFilter In relation.Constants
                            sb.Append(",")
                            For Each p In f.MakeSingleQueryStmt(mpe, Me, Nothing, pmgr, Nothing)
                                sb.Append(p.Column)
                                consts.Add(p.Param)
                            Next
                        Next
                    End If
                    sb.Append(") values(")
                    pk = pmgr.AddParam(pk, obj.Identifier)
                    sb.Append(pk).Append(",").Append(toAdd.Identifier)
                    For Each s As String In consts
                        sb.Append(",").Append(s)
                    Next
                    sb.AppendLine(")")
                Next

                If entry.HasDeleted AndAlso le IsNot Nothing Then
                    sb.AppendLine("end")
                End If
            End If

            Return sb.ToString
        End Function

        Public Overridable Function Delete(ByVal mpe As ObjectMappingEngine, ByVal obj As ICachedEntity,
                                           ByRef dbparams As IEnumerable(Of System.Data.Common.DbParameter),
            ByVal contextInfo As IDictionary) As String

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj parameter cannot be nothing")
            End If

            If Not SupportMultiline() Then
                Throw New DbGeneratorException("Generator doesn't support multiline statements")
            End If

            Using obj.AcquareLock()
                Dim del_cmd As New StringBuilder
                dbparams = Nothing

                If obj.ObjectState = ObjectState.Deleted Then
                    Dim type As Type = obj.GetType
                    Dim oschema As IEntitySchema = obj.GetEntitySchema(mpe)
                    Dim relSchema As IEntitySchema = oschema
                    Dim ro As IReadonlyObjectSchema = TryCast(oschema, IReadonlyObjectSchema)
                    If ro IsNot Nothing AndAlso (ro.SupportedOperation And IReadonlyObjectSchema.Operation.Delete) = IReadonlyObjectSchema.Operation.Delete Then
                        relSchema = ro.GetEditableSchema
                        For Each ep As MapField2Column In relSchema.FieldColumnMap
                            Dim m As MapField2Column = Nothing
                            Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap
                            If map.TryGetValue(ep.PropertyAlias, m) Then
                                ep.PropertyInfo = m.PropertyInfo
                                ep.Schema = m.Schema
                                If ep.Attributes = Field2DbRelations.None Then
                                    ep.Attributes = m.Attributes
                                End If
                            End If
                        Next
                    End If

                    Dim params As New ParamMgr(Me, "p")
                    Dim deleted_tables As New Generic.Dictionary(Of SourceFragment, IFilter)
                    Dim pk = oschema.GetPK
                    For Each p In obj.GetPKValues(oschema)
                        Dim dbt As String = "int"
                        'Dim c As EntityPropertyAttribute = mpe.GetColumnByPropertyAlias(type, p.PropertyAlias, oschema)
                        'If c Is Nothing Then
                        '    c = New EntityPropertyAttribute()
                        '    c.PropertyAlias = p.PropertyAlias
                        'End If
                        dbt = GetDBType(mpe, type, oschema, pk.PropertyAlias, p.Column)
                        del_cmd.Append(DeclareVariable("@id_" & p.Column.ClearSourceField, dbt))
                        del_cmd.Append(EndLine)
                        del_cmd.Append("set @id_").Append(p.Column.ClearSourceField).Append(" = ")
                        del_cmd.Append(params.CreateParam(oschema.ChangeValueType(p.Column, p.Value))).Append(EndLine)
                    Next

                    Dim exec As New ExecutorCtx(type, relSchema)
                    GetDeletedConditions(mpe, deleted_tables, contextInfo, type, obj, relSchema, TryCast(relSchema, IMultiTableObjectSchema))

                    Dim pkFilter As IFilter = deleted_tables(relSchema.Table)
                    deleted_tables.Remove(relSchema.Table)

                    For Each de As KeyValuePair(Of SourceFragment, IFilter) In deleted_tables
                        del_cmd.Append("delete from ").Append(GetTableName(de.Key, contextInfo))
                        del_cmd.Append(" where ").Append(de.Value.MakeQueryStmt(mpe, Nothing, Me, exec, contextInfo, Nothing, params))
                        del_cmd.Append(EndLine)
                    Next
                    del_cmd.Append("delete from ").Append(GetTableName(relSchema.Table, contextInfo))
                    del_cmd.Append(" where ").Append(pkFilter.MakeQueryStmt(mpe, Nothing, Me, exec, contextInfo, Nothing, params))
                    del_cmd.Append(EndLine)

                    del_cmd.Length -= EndLine.Length
                    dbparams = params.Params
                End If

                Return del_cmd.ToString
            End Using
        End Function

        Protected Overridable Overloads Function GetDBType(ByVal mpe As ObjectMappingEngine, ByVal type As Type, ByVal os As IEntitySchema,
                                                           ByVal pa As String, ByVal sf As String) As String

            Dim m As MapField2Column = os.FieldColumnMap(pa)
            Dim db As DBType? = Nothing
            If String.IsNullOrEmpty(sf) Then
                db = m.SourceFields.FirstOrDefault?.DBType
            Else
                db = m.SourceFields.FirstOrDefault(Function(s) s.SourceFieldExpression = sf)?.DBType
            End If

            If Not db.HasValue OrElse db.Value.IsEmpty Then
                Return DbTypeConvertor.ToSqlDbType(m.PropertyInfo.PropertyType).ToString
            Else
                Return FormatDBType(db.Value)
            End If
        End Function

        Protected Overridable Function FormatDBType(ByVal db As DBType) As String
            If db.Size > 0 Then
                Return db.Type & "(" & db.Size & ")"
            Else
                Return db.Type
            End If
        End Function

        Protected Sub GetDeletedConditions(ByVal mpe As ObjectMappingEngine, ByVal deleted_tables As IDictionary(Of SourceFragment, IFilter), ByVal filterInfo As Object,
                                           ByVal type As Type, ByVal obj As ICachedEntity, ByVal oschema As IEntitySchema, ByVal relSchema As IMultiTableObjectSchema)
            'Dim oschema As IOrmObjectSchema = GetObjectSchema(type)
            Dim tables() As SourceFragment = oschema.GetTables()
            Dim pkTable As SourceFragment = oschema.GetPKTable(type)
            For j As Integer = 0 To tables.Length - 1
                Dim table As SourceFragment = tables(j)
                Dim o As New Condition.ConditionConstructor
                If table.Equals(pkTable) Then
                    Dim col As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap
                    'Dim ie As ICollection = mpe.GetProperties(type, oschema)
                    'If ie.Count = 0 AndAlso GetType(AnonymousCachedEntity).IsAssignableFrom(type) Then
                    '    ie = col
                    'End If

                    For Each m As MapField2Column In col
                        'Dim c As EntityPropertyAttribute = Nothing
                        'Dim pi As Reflection.PropertyInfo = Nothing
                        'If TypeOf (oo) Is DictionaryEntry Then
                        '    Dim de As DictionaryEntry = CType(oo, DictionaryEntry)
                        '    c = CType(de.Key, EntityPropertyAttribute)
                        '    pi = CType(de.Value, Reflection.PropertyInfo)
                        'Else
                        '    Dim m As MapField2Column = CType(oo, MapField2Column)
                        '    c = New EntityPropertyAttribute(m.ColumnExpression)
                        '    c.PropertyAlias = m.PropertyAlias
                        'End If

                        'If c IsNot Nothing Then
                        Dim att As Field2DbRelations = m.Attributes 'oschema.GetFieldColumnMap()(c.PropertyAlias).GetAttributes(c) 'GetAttributes(type, c)
                        'Dim propertyAlias As String = m.PropertyAlias
                        If (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                            For Each sf In m.SourceFields
                                o.AddFilter(New dc.TableFilter(oschema.Table, sf.SourceFieldExpression, New LiteralValue("@id_" & sf.SourceFieldExpression.ClearSourceField), FilterOperation.Equal))
                            Next
                        ElseIf (att And Field2DbRelations.RV) = Field2DbRelations.RV Then
                            Dim v As Object = ObjectMappingEngine.GetPropertyValue(obj, m.PropertyAlias, oschema, m.PropertyInfo)
                            o.AddFilter((New dc.EntityFilter(type, m.PropertyAlias, New ScalarValue(v), FilterOperation.Equal)))
                        End If
                        'End If
                    Next
                    deleted_tables(table) = CType(o.Condition, IFilter)
                ElseIf relSchema IsNot Nothing Then
                    Dim join As QueryJoin = CType(mpe.GetJoins(relSchema, tables(0), table, filterInfo), QueryJoin)
                    If Not QueryJoin.IsEmpty(join) Then
                        Dim pk = oschema.GetPK
                        Dim f As IFilter = JoinFilter.ChangeEntityJoinToLiteral(mpe, join.Condition, type, pk.PropertyAlias, pk.SourceFields.Select(Function(it) "@id_" & it.SourceFieldExpression.ClearSourceField))

                        If f Is Nothing Then
                            f = JoinFilter.ChangeTableJoinToLiteral(Me, mpe, join.Condition, tables(0), pk.SourceFields, pk.SourceFields.Select(Function(it) "@id_" & it.SourceFieldExpression.ClearSourceField))
                        End If

                        If f Is Nothing Then
                            Throw New ObjectMappingException("Cannot replace join")
                        End If

                        o.AddFilter(f)
                        deleted_tables(table) = CType(o.Condition, IFilter)
                    End If
                End If
            Next
        End Sub

        Public Function Insert(ByVal mpe As ObjectMappingEngine, ByVal obj As ICachedEntity, ByVal contextInfo As IDictionary,
                               ByRef dbparams As ICollection(Of System.Data.Common.DbParameter),
                               ByRef selectedProperties As List(Of SelectExpression), ByRef insertedColumns As Integer) As String

            If obj Is Nothing Then
                Throw New ArgumentNullException(NameOf(obj))
            End If

            Using obj.AcquareLock()
                Dim ins_cmd As New StringBuilder
                dbparams = Nothing
                If obj.ObjectState = ObjectState.Created Then
                    Dim inserted_tables As New Generic.Dictionary(Of SourceFragment, List(Of ITemplateFilter))
                    Dim selectedProps As New Generic.List(Of EntityExpression)
                    Dim pk As MapField2Column = Nothing
                    Dim real_t As Type = obj.GetType
                    Dim oschema As IEntitySchema = obj.GetEntitySchema(mpe)
                    Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap
                    Dim rs As IReadonlyObjectSchema = TryCast(oschema, IReadonlyObjectSchema)
                    Dim es As IEntitySchema = oschema
                    If rs IsNot Nothing AndAlso (rs.SupportedOperation And IReadonlyObjectSchema.Operation.Insert) = IReadonlyObjectSchema.Operation.Insert Then
                        es = rs.GetEditableSchema
                        Dim newMap As Collections.IndexedCollection(Of String, MapField2Column) = es.FieldColumnMap
                        For Each ep As MapField2Column In newMap
                            Dim m As MapField2Column = Nothing
                            If map.TryGetValue(ep.PropertyAlias, m) Then
                                ep.PropertyInfo = m.PropertyInfo
                                ep.Schema = m.Schema
                                If ep.Attributes = Field2DbRelations.None Then
                                    ep.Attributes = m.Attributes
                                End If
                            End If
                        Next
                        map = newMap
                    End If
                    Dim js As IMultiTableObjectSchema = TryCast(es, IMultiTableObjectSchema)
                    Dim tbls() As SourceFragment = es.GetTables()

                    Dim pkTable As SourceFragment = es.GetPKTable(real_t)

                    Dim exec As New ExecutorCtx(real_t, es)
                    'Dim ie As ICollection = mpe.GetProperties(real_t, es)
                    'If ie.Count = 0 AndAlso GetType(AnonymousCachedEntity).IsAssignableFrom(real_t) Then
                    '    ie = col
                    'End If

                    For Each m As MapField2Column In map
                        Dim pi As Reflection.PropertyInfo = m.PropertyInfo
                        Dim propertyAlias As String = m.PropertyAlias
                        Dim current As Object = ObjectMappingEngine.GetPropertyValue(obj, propertyAlias, TryCast(oschema, IEntitySchema), pi)

                        Dim att As Field2DbRelations = m.Attributes
                        If (att And Field2DbRelations.ReadOnly) <> Field2DbRelations.ReadOnly OrElse (att And Field2DbRelations.InsertDefault) = Field2DbRelations.InsertDefault Then
                            Dim tb As SourceFragment = mpe.GetPropertyTable(es, propertyAlias)

                            Dim f As EntityFilter = Nothing
                            Dim v As Object = es.ChangeValueType(propertyAlias, current)
                            If (att And Field2DbRelations.InsertDefault) = Field2DbRelations.InsertDefault AndAlso v Is DBNull.Value Then
                                If Not String.IsNullOrEmpty(DefaultValue) Then
                                    f = New dc.EntityFilter(real_t, propertyAlias, New LiteralValue(DefaultValue), FilterOperation.Equal)
                                Else
                                    Throw New ObjectMappingException("DefaultValue required for operation")
                                End If
                            ElseIf v Is DBNull.Value AndAlso pkTable IsNot tb AndAlso js IsNot Nothing Then
                                Dim j As QueryJoin = CType(mpe.GetJoins(js, pkTable, tb, contextInfo), QueryJoin)
                                If j.JoinType = Joins.JoinType.Join Then
                                    GoTo l1
                                End If
                            Else
l1:
                                If current IsNot Nothing AndAlso GetType(ICachedEntity).IsAssignableFrom(current.GetType) Then
                                    If CType(current, ICachedEntity).ObjectState = ObjectState.Created Then
                                        Throw New ObjectMappingException(obj.ObjName & "Cannot save object while it has reference to new object " & CType(current, _IEntity).ObjName)
                                    End If
                                End If
                                f = New dc.EntityFilter(real_t, propertyAlias, New ScalarValue(v), FilterOperation.Equal)
                                insertedColumns += 1
                            End If

                            If f IsNot Nothing Then
                                If Not inserted_tables.ContainsKey(tb) Then
                                    inserted_tables.Add(tb, New List(Of ITemplateFilter))
                                End If
                                inserted_tables(tb).Add(f)
                            End If
                        End If

                        If (att And Field2DbRelations.SyncInsert) = Field2DbRelations.SyncInsert Then
                            selectedProps.Add(New EntityExpression(propertyAlias, real_t))
                        End If

                        If (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                            pk = m
                            'prim_key_value = current
                        End If
                    Next

                    If Not inserted_tables.ContainsKey(pkTable) Then
                        inserted_tables.Add(pkTable, Nothing)
                    End If

                    Dim ins_tables As List(Of InsertedTable) = Sort(inserted_tables, tbls)

                    For j As Integer = 0 To ins_tables.Count - 1
                        ins_tables(j).Executor = exec

                        If ins_tables(j).Table Is pkTable Then Continue For

                        Dim join_table As SourceFragment = ins_tables(j).Table

                        Dim jn As QueryJoin = Nothing
                        If js IsNot Nothing Then
                            jn = CType(mpe.GetJoins(js, pkTable, join_table, contextInfo), QueryJoin)
                        End If
                        If Not QueryJoin.IsEmpty(jn) Then
                            Dim f = JoinFilter.ChangeEntityJoinToLiteral(mpe, jn.Condition, real_t, pk.PropertyAlias, pk.SourceFields.Select(Function(it) it.GetPKParamName4Insert))

                            If f Is Nothing Then
                                f = JoinFilter.ChangeTableJoinToLiteral(Me, mpe, jn.Condition, pkTable, pk.SourceFields, pk.SourceFields.Select(Function(it) it.GetPKParamName4Insert))
                            End If

                            If f Is Nothing Then
                                Throw New ObjectMappingException("Cannot change join")
                            End If

                            ins_tables(j).Filters.AddRange(CType(f.GetAllFilters, IEnumerable(Of ITemplateFilter)))
                        End If
                    Next
                    dbparams = FormInsert(mpe, ins_tables, ins_cmd, real_t, es, selectedProps, Nothing, contextInfo)

                    If True Then
                        selectedProperties = selectedProps.ConvertAll(Function(e) New SelectExpression(e))
                    End If
                End If

                Return ins_cmd.ToString
            End Using
        End Function

        ''' <summary>
        ''' Сортирует словарь в соответствии с порядком ключей в коллекции
        ''' </summary>
        ''' <param name="dic">Словарь</param>
        ''' <param name="model">Упорядоченная коллекция ключей</param>
        ''' <returns>Список пар ключ/значение из словаря, упорядоченный по коллекции <b>model</b></returns>
        ''' <exception cref="InvalidOperationException">Если ключ из словаря не найден в коллекции <b>model</b></exception>
        Private Shared Function Sort(ByVal dic As IDictionary(Of SourceFragment, List(Of ITemplateFilter)), ByVal model() As SourceFragment) As List(Of InsertedTable)
            Dim l As New List(Of InsertedTable)

            If dic IsNot Nothing Then
                Dim arr(model.Length - 1) As List(Of ITemplateFilter)
                For Each de As KeyValuePair(Of SourceFragment, List(Of ITemplateFilter)) In dic

                    Dim idx As Integer = Array.IndexOf(model, de.Key)

                    If idx < 0 Then
                        Throw New InvalidOperationException("Unknown key " + Convert.ToString(de.Key))
                    End If

                    arr(idx) = de.Value
                Next

                For i As Integer = 0 To dic.Count - 1
                    l.Add(New InsertedTable(model(i), arr(i)))
                Next
            End If

            Return l
        End Function

        Protected Structure TableUpdate
            Public _table As SourceFragment
            Public _updates As IList(Of EntityFilter)
            Public _where4update As Condition.ConditionConstructor
            Public _joins As IList(Of QueryJoin)

            Public Sub New(ByVal table As SourceFragment)
                _table = table
                _updates = New List(Of EntityFilter)
                _where4update = New Condition.ConditionConstructor
                _joins = New List(Of QueryJoin)
            End Sub

        End Structure

        Public Overridable Function Update(ByVal mpe As ObjectMappingEngine, ByVal obj As ICachedEntity,
                                           ByVal contextInfo As IDictionary, ByVal originalCopy As ICachedEntity, ByRef dbparams As IEnumerable(Of System.Data.Common.DbParameter),
                                           ByRef selectedProperties As Generic.List(Of SelectExpression),
                                           ByRef updated_fields As IList(Of EntityFilter)) As String

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj parameter cannot be nothing")
            End If

            selectedProperties = Nothing
            updated_fields = Nothing

            Dim le As ILastError = TryCast(Me, ILastError)

            Using obj.AcquareLock()
                Dim upd_cmd As New StringBuilder
                dbparams = Nothing
                If originalCopy IsNot Nothing Then
                    'If obj.ObjectState = ObjectState.Modified Then
                    'If obj.OriginalCopy Is Nothing Then
                    '    Throw New ObjectStateException(obj.ObjName & "Object in state modified have to has an original copy")
                    'End If

                    Dim syncUpdateProps As New List(Of EntityExpression)
                    Dim updated_tables As New Dictionary(Of SourceFragment, TableUpdate)
                    Dim rt As Type = obj.GetType

                    'Dim sb_updates As New StringBuilder
                    Dim oschema As IEntitySchema = obj.GetEntitySchema(mpe)
                    Dim esch As IEntitySchema = oschema
                    Dim ro As IReadonlyObjectSchema = TryCast(oschema, IReadonlyObjectSchema)
                    If ro IsNot Nothing AndAlso (ro.SupportedOperation And IReadonlyObjectSchema.Operation.Update) = IReadonlyObjectSchema.Operation.Update Then
                        esch = ro.GetEditableSchema
                        Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap
                        For Each ep As MapField2Column In esch.FieldColumnMap
                            Dim m As MapField2Column = Nothing
                            If map.TryGetValue(ep.PropertyAlias, m) Then
                                ep.PropertyInfo = m.PropertyInfo
                                ep.Schema = m.Schema
                                If ep.Attributes = Field2DbRelations.None Then
                                    ep.Attributes = m.Attributes
                                End If
                            End If
                        Next
                    End If

                    Dim exec As Query.IExecutionContext = GetChangedFields(mpe, obj, esch, updated_tables, syncUpdateProps, originalCopy)

                    Dim l As New List(Of EntityFilter)
                    For Each tu As TableUpdate In updated_tables.Values
                        l.AddRange(tu._updates)
                    Next
                    updated_fields = l

                    GetUpdateConditions(mpe, obj, esch, updated_tables, contextInfo)

                    selectedProperties = syncUpdateProps.ConvertAll(Function(e) New SelectExpression(e))

                    Dim lastErrIdx = 0
                    'Dim insTables As New List(Of Tuple(Of SourceFragment, String))

                    If updated_tables.Count > 0 Then
                        'Dim sch As IOrmObjectSchema = GetObjectSchema(rt)
                        Dim pk_table As SourceFragment = esch.GetPKTable(rt)
                        Dim amgr As AliasMgr = AliasMgr.Create
                        Dim params As New ParamMgr(Me, "p")
                        Dim hasSyncUpdate As Boolean = False
                        Dim lastTbl As SourceFragment = Nothing, lastUT As TableUpdate = Nothing
                        For Each item As Generic.KeyValuePair(Of SourceFragment, TableUpdate) In updated_tables
                            Dim tbl As SourceFragment = item.Key
                            If upd_cmd.Length > 0 Then
                                upd_cmd.Append(EndLine)
                                If SupportIf() Then
                                    Dim lastError As String = Nothing
                                    upd_cmd.Append(DeclareVariableInc("@lastErr", "int", lastErrIdx, lastError)).Append(EndLine)

                                    If HasUpdateColumnsForTable(syncUpdateProps, lastTbl, esch) OrElse Not lastTbl.Equals(pk_table) Then
                                        hasSyncUpdate = hasSyncUpdate OrElse HasUpdateColumnsForTable(syncUpdateProps, lastTbl, esch)
                                        Dim varName As String = "@" & lastTbl.Name.Replace(".", "").Trim("["c, "]"c) & "_rownum"
                                        upd_cmd.Append(DeclareVariable(varName, "int")).Append(EndLine)
                                        upd_cmd.Append("select ").Append(varName).Append(" = ").Append(RowCount)
                                        upd_cmd.Append(", ").Append(lastError).Append(" = ").Append(le.LastError).Append(EndLine)

                                        If Not lastTbl.Equals(pk_table) Then
                                            CorrectUpdateWithInsert(mpe, oschema, lastTbl, lastUT, upd_cmd,
                                                obj, params, exec, varName, contextInfo, lastError)

                                            'insTables.Add(New Tuple(Of SourceFragment, String)(lastTbl, lastError))
                                        End If
                                    Else
                                        upd_cmd.Append("set ").Append(lastError).Append(" = ").Append(le.LastError).Append(EndLine)
                                    End If

                                    upd_cmd.Append("if ").Append(lastError).Append(" = 0 ")
                                End If
                                'ElseIf updated_tables.Count > 1 Then
                                '    upd_cmd.Append(DeclareVariable("@lastErr", "int")).Append(EndLine)
                            End If

                            lastTbl = tbl
                            lastUT = item.Value
                            Dim [alias] As String = amgr.AddTable(tbl, Nothing, params)

                            upd_cmd.Append("update ").Append([alias]).Append(" set ")
                            For Each f As EntityFilter In item.Value._updates
                                upd_cmd.Append(f.MakeQueryStmt(esch, Nothing, Me, exec, contextInfo, mpe, amgr, params, rt)).Append(",")
                            Next
                            upd_cmd.Length -= 1
                            upd_cmd.Append(" from ").Append(GetTableName(tbl, contextInfo)).Append(" ").Append([alias])
                            For Each join As QueryJoin In item.Value._joins
                                If Not amgr.ContainsKey(join.Table, Nothing) Then
                                    amgr.AddTable(join.Table, Nothing, params)
                                End If
                                join.MakeSQLStmt(mpe, Nothing, Me, Nothing, contextInfo, amgr, params, Nothing, upd_cmd)
                            Next
                            upd_cmd.Append(" where ")
                            Dim fl As IFilter = CType(item.Value._where4update.Condition, IFilter)
                            Dim ef As EntityFilter = TryCast(fl, EntityFilter)
                            If ef IsNot Nothing Then
                                upd_cmd.Append(ef.MakeQueryStmt(esch, Nothing, Me, exec, contextInfo, mpe, amgr, params, rt))
                            Else
                                upd_cmd.Append(fl.MakeQueryStmt(mpe, Nothing, Me, exec, contextInfo, amgr, params))
                            End If
                        Next

                        If SupportIf() Then
                            Dim varName As String = "@" & lastTbl.Name.ClearSourceField & "_rownum"
                            Dim lastError As String = Nothing
                            Dim lastErrStmt = DeclareVariableInc("@lastErr", "int", lastErrIdx, lastError)
                            Dim insSb As New StringBuilder
                            If Not lastTbl.Equals(pk_table) Then
                                CorrectUpdateWithInsert(mpe, oschema, lastTbl, lastUT, insSb,
                                     obj, params, exec, varName, contextInfo, lastError)
                            End If

                            Dim hasCol As Boolean = HasUpdateColumnsForTable(syncUpdateProps, lastTbl, esch)
                            If hasCol OrElse insSb.Length > 0 Then
                                hasSyncUpdate = hasSyncUpdate OrElse hasCol
                                upd_cmd.Append(EndLine)
                                upd_cmd.Append(DeclareVariable(varName, "int")).Append(EndLine)
                                upd_cmd.Append(lastErrStmt).Append(EndLine)
                                upd_cmd.Append("select ").Append(varName).Append(" = ").Append(RowCount)
                                If insSb.Length > 0 Then
                                    upd_cmd.Append(", ").Append(lastError).Append(" = ").Append(le.LastError)
                                End If
                                upd_cmd.Append(insSb.ToString)
                            End If
                        End If

                        'Dim hasSyncUpdate As Boolean = HasUpdateColumns(syncUpdateProps, updated_tables, esch)

                        If hasSyncUpdate Then
                            upd_cmd.Append(EndLine)
                            If SupportIf() Then
                                upd_cmd.Append("if ")
                                For Each tbl As SourceFragment In syncUpdateProps _
                                    .ConvertAll(Function(e) esch.FieldColumnMap(e.ObjectProperty.PropertyAlias).Table)

                                    Dim varName As String = "@" & tbl.Name.ClearSourceField & "_rownum"
                                    upd_cmd.Append(varName).Append(" and ")

                                Next
                                upd_cmd.Length -= 5
                                upd_cmd.Append(" > 0 ")
                            End If
                            Dim sel_sb As New StringBuilder
                            Dim newAlMgr As AliasMgr = AliasMgr.Create
                            sel_sb.Append(SelectWithJoin(mpe, rt, esch.GetTables(), newAlMgr, params,
                                                         Nothing, True, Nothing, Nothing, syncUpdateProps, esch, contextInfo))

                            Dim cn As New Condition.ConditionConstructor
                            For Each p In obj.GetPKValues(oschema)
                                cn.AddFilter(New dc.TableFilter(esch.Table, p.Column, New ScalarValue(p.Value), FilterOperation.Equal))
                            Next
                            AppendWhere(mpe, rt, esch, cn.Condition, newAlMgr, sel_sb, contextInfo, params)
                            upd_cmd.Append(sel_sb)
                            selectedProperties = syncUpdateProps.ConvertAll(Function(e) New SelectExpression(e))
                        End If

                        dbparams = params.Params
                    End If

                End If
                Return upd_cmd.ToString
            End Using
        End Function

        Protected Overridable Function CorrectUpdateWithInsert(ByVal mpe As ObjectMappingEngine, ByVal oschema As IEntitySchema, ByVal table As SourceFragment,
                                                               ByVal tableinfo As TableUpdate,
                                                               ByVal upd_cmd As StringBuilder, ByVal obj As ICachedEntity, ByVal params As ICreateParam,
                                                               ByVal exec As Query.IExecutionContext, ByVal rowCnt As String, ByVal contextInfo As IDictionary,
                                                               lastError As String) As Boolean

            Dim dic As New List(Of InsertedTable)
            Dim l As New List(Of ITemplateFilter)
            For Each f As EntityFilter In tableinfo._updates
                l.Add(f)
            Next

            For Each f As ITemplateFilter In tableinfo._where4update.Condition.GetAllFilters
                l.Add(f)
            Next

            Dim ins As InsertedTable = New InsertedTable(table, l) With {
                .Executor = exec
            }
            dic.Add(ins)
            Dim oldl As Integer = upd_cmd.Length
            upd_cmd.Append(EndLine).Append("if ").Append(rowCnt).Append(" = 0 and ").Append(lastError).Append(" = 0 ")
            Dim newl As Integer = upd_cmd.Length
            FormInsert(mpe, dic, upd_cmd, obj.GetType, oschema, Nothing, params, contextInfo)
            If newl = upd_cmd.Length Then
                upd_cmd.Length = oldl
                Return False
            Else
                Return True
            End If
        End Function

        Protected Function GetChangedFields(ByVal mpe As ObjectMappingEngine, ByVal obj As ICachedEntity,
                                            ByVal oschema As IPropertyMap, ByVal tables As IDictionary(Of SourceFragment, TableUpdate),
                                            ByVal selectedProperties As List(Of EntityExpression),
                                            ByVal originalCopy As ICachedEntity) As ExecutorCtx

            Dim rt As Type = obj.GetType
            Dim col As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap
            Dim exec As New ExecutorCtx(rt, TryCast(oschema, IEntitySchema))
            'Dim ie As ICollection = mpe.GetProperties(rt, TryCast(oschema, IEntitySchema))
            'If ie.Count = 0 AndAlso GetType(AnonymousCachedEntity).IsAssignableFrom(rt) Then
            '    ie = col
            'End If

            For Each map As MapField2Column In col
                'Dim c As EntityPropertyAttribute = Nothing
                Dim pi As Reflection.PropertyInfo = map.PropertyInfo
                'If TypeOf (o) Is DictionaryEntry Then
                '    Dim de As DictionaryEntry = CType(o, DictionaryEntry)
                '    c = CType(de.Key, EntityPropertyAttribute)
                '    pi = CType(de.Value, Reflection.PropertyInfo)
                'Else
                '    Dim m As MapField2Column = CType(o, MapField2Column)
                '    c = New EntityPropertyAttribute(m.ColumnExpression)
                '    c.PropertyAlias = m.PropertyAlias
                'End If
                Dim pa As String = map.PropertyAlias
                'If c IsNot Nothing Then
                Dim original As Object = ObjectMappingEngine.GetPropertyValue(originalCopy, pa, TryCast(oschema, IEntitySchema), pi)
                Dim att As Field2DbRelations = map.Attributes 'map.GetAttributes(c)
                If (att And Field2DbRelations.ReadOnly) <> Field2DbRelations.ReadOnly Then

                    Dim current As Object = ObjectMappingEngine.GetPropertyValue(obj, pa, TryCast(oschema, IEntitySchema), pi)
l1:
                    If (original IsNot Nothing AndAlso Not original.Equals(current)) OrElse
                     (current IsNot Nothing AndAlso Not current.Equals(original)) OrElse CType(obj, _ICachedEntity).ForseUpdate(pa) Then

                        If original IsNot Nothing AndAlso current IsNot Nothing Then
                            Dim originalType As Type = original.GetType
                            Dim currentType As Type = current.GetType

                            If originalType IsNot currentType Then
                                If ObjectMappingEngine.IsEntityType(original.GetType) Then
                                    'Dim sch As IEntitySchema = mpe.GetEntitySchema(originalType, False)
                                    'If sch Is Nothing Then
                                    '    sch = mpe.GetPOCOEntitySchema(originalType)
                                    'End If
                                    'current = mpe.CreateObj(original.GetType, current, sch)
                                    'GoTo l1
                                    Throw New ApplicationException
                                ElseIf ObjectMappingEngine.IsEntityType(currentType) Then
                                    'Dim sch As IEntitySchema = mpe.GetEntitySchema(currentType, False)
                                    'If sch Is Nothing Then
                                    '    sch = mpe.GetPOCOEntitySchema(currentType)
                                    'End If
                                    'original = mpe.CreateObj(currentType, original, sch)
                                    'GoTo l1
                                    Throw New ApplicationException
                                Else
                                    'Throw New InvalidOperationException(String.Format("Property {0} has different types {1} and {2}", pa, originalType, currentType))
                                    If SizeOf(original) > SizeOf(current) Then
                                        current = Convert.ChangeType(current, currentType)
                                    Else
                                        current = Convert.ChangeType(current, originalType)
                                    End If
                                End If
                            ElseIf Not GetType(IEntity).IsAssignableFrom(originalType) _
                                AndAlso Not GetType(IEntity).IsAssignableFrom(currentType) _
                                AndAlso ObjectMappingEngine.IsEntityType(originalType) _
                                AndAlso ObjectMappingEngine.IsEntityType(currentType) Then
                                Dim sch As IEntitySchema = mpe.GetEntitySchema(currentType, False)
                                If sch Is Nothing Then
                                    sch = mpe.GetPOCOEntitySchema(currentType)
                                End If
                                For Each mp As MapField2Column In sch.FieldColumnMap
                                    Dim p As String = mp.PropertyAlias
                                    If Not Object.Equals(ObjectMappingEngine.GetPropertyValue(current, p, sch), ObjectMappingEngine.GetPropertyValue(original, p, sch)) Then
                                        GoTo l3
                                    End If
                                Next
                                GoTo l2
                            End If

l3:
                        End If

                        If current IsNot Nothing AndAlso GetType(ICachedEntity).IsAssignableFrom(current.GetType) Then
                            If CType(current, ICachedEntity).ObjectState = ObjectState.Created Then
                                Throw New ObjectMappingException(obj.ObjName & "Cannot save object while it has reference to new object " & CType(current, ICachedEntity).ObjName)
                            End If
                        End If

                        Dim fieldTable As SourceFragment = mpe.GetPropertyTable(oschema, pa)

                        If Not tables.ContainsKey(fieldTable) Then
                            tables.Add(fieldTable, New TableUpdate(fieldTable))
                            'param_vals.Add(fieldTable, New ArrayList)
                        End If

                        Dim updates As IList(Of EntityFilter) = tables(fieldTable)._updates

                        updates.Add(New dc.EntityFilter(rt, pa, New ScalarValue(current), FilterOperation.Equal))

                        'Dim tb_sb As StringBuilder = CType(tables(fieldTable), StringBuilder)
                        'Dim _params As ArrayList = CType(param_vals(fieldTable), ArrayList)
                        'If tb_sb.Length <> 0 Then
                        '    tb_sb.Append(", ")
                        'End If
                        'If unions IsNot Nothing Then
                        '    Throw New NotSupportedException
                        '    'tb_sb.Append(GetColumnNameByFieldName(type, c.FieldName, tb))
                        'Else
                        '    tb_sb.Append(GetColumnNameByFieldNameInternal(type, c.FieldName, False))
                        'End If
                        'tb_sb.Append(" = ").Append(ParamName("p", i))
                        '_params.Add(CreateDBParameter(ParamName("p", i), ChangeValueType(type, c, current)))
                        'i += 1
                    End If
                    'Else
                    '    If sbwhere.Length > 0 Then sbwhere.Append(" and ")
                    '    sbwhere.Append(GetColumnNameByFieldName(type, c.FieldName))
                    '    Dim pname As String = "pk"
                    '    If (c.SyncBehavior And Field2DbRelations.RowVersion) = Field2DbRelations.RowVersion Then
                    '        pname = "rv"
                    '    Else
                    '        If sbsel_where.Length = 0 Then
                    '            'sbsel_where.Append(" from ").Append(GetTables(type)(0))
                    '            sbsel_where.Append(" where ")
                    '        Else
                    '            sbsel_where.Append(" and ")
                    '        End If

                    '        sbsel_where.Append(GetColumnNameByFieldName(type, c.FieldName))
                    '        sbsel_where.Append(" = ").Append(ParamName(pname, i))
                    '    End If
                    '    sbwhere.Append(" = ").Append(ParamName(pname, i))
                    '    _params.Add(CreateDBParameter(ParamName(pname, i), original))
                    '    i += 1
                End If
l2:
                If (att And Field2DbRelations.SyncUpdate) = Field2DbRelations.SyncUpdate Then
                    'If sbselect.Length = 0 Then
                    '    sbselect.Append("if @@rowcount > 0 select ")
                    'Else
                    '    sbselect.Append(", ")
                    'End If

                    'sbselect.Append(GetColumnNameByFieldName(type, c.FieldName))
                    selectedProperties.Add(New EntityExpression(pa, rt))
                End If
                'End If
            Next
            Return exec
        End Function

        Protected Sub GetUpdateConditions(ByVal mpe As ObjectMappingEngine, ByVal obj As ICachedEntity, ByVal oschema As IEntitySchema,
                                          ByVal updated_tables As IDictionary(Of SourceFragment, TableUpdate), ByVal filterInfo As Object)

            Dim rt As Type = obj.GetType

            'Dim ie As ICollection = mpe.GetProperties(rt, TryCast(oschema, IEntitySchema))
            'If ie.Count = 0 AndAlso GetType(AnonymousCachedEntity).IsAssignableFrom(rt) Then
            '    ie = oschema.GetFieldColumnMap
            'End If

            Dim pkSet = False

            For Each m As MapField2Column In oschema.FieldColumnMap
                'Dim c As EntityPropertyAttribute = Nothing
                Dim pi As Reflection.PropertyInfo = m.PropertyInfo
                'If TypeOf (o) Is DictionaryEntry Then
                '    Dim de As DictionaryEntry = CType(o, DictionaryEntry)
                '    c = CType(de.Key, EntityPropertyAttribute)
                '    pi = CType(de.Value, Reflection.PropertyInfo)
                'Else
                '    Dim m As MapField2Column = CType(o, MapField2Column)
                '    c = New EntityPropertyAttribute(m.ColumnExpression)
                '    c.PropertyAlias = m.PropertyAlias
                'End If
                Dim pa As String = m.PropertyAlias
                Dim att As Field2DbRelations = m.Attributes
                If (att And Field2DbRelations.PK) = Field2DbRelations.PK OrElse (att And Field2DbRelations.RV) = Field2DbRelations.RV Then

                    Dim original As Object = ObjectMappingEngine.GetPropertyValue(obj, pa, TryCast(oschema, IEntitySchema), pi)

                    If original Is Nothing Then
                        Continue For
                    End If

                    Dim tb As SourceFragment = mpe.GetPropertyTable(oschema, pa)

                    For Each de_table As Generic.KeyValuePair(Of SourceFragment, TableUpdate) In updated_tables 'In New Generic.List(Of Generic.KeyValuePair(Of String, TableUpdate))(CType(updated_tables, Generic.ICollection(Of Generic.KeyValuePair(Of String, TableUpdate))))
                        'Dim de_table As TableUpdate = updated_tables(tb)
                        If de_table.Key.Equals(tb) Then
                            Dim sb As IEntitySchemaBase = TryCast(oschema, IEntitySchemaBase)
                            If sb IsNot Nothing Then
                                Dim nv As Object = Nothing
                                If sb.ChangeValueType(pa, original, nv) Then
                                    original = nv
                                End If
                            End If
                            de_table.Value._where4update.AddFilter(New dc.EntityFilter(rt, pa, New ScalarValue(original), FilterOperation.Equal))
                            pkSet = (att And Field2DbRelations.PK) = Field2DbRelations.PK OrElse pkSet
                        Else
                            Dim joinableSchema As IMultiTableObjectSchema = TryCast(oschema, IMultiTableObjectSchema)
                            If joinableSchema IsNot Nothing Then
                                Dim join As QueryJoin = CType(mpe.GetJoins(joinableSchema, tb, de_table.Key, filterInfo), QueryJoin)
                                If Not QueryJoin.IsEmpty(join) Then
                                    Dim f As IFilter = JoinFilter.ChangeEntityJoinToParam(mpe, join.Condition, rt, pa, New TypeWrap(Of Object)(original))

                                    If f Is Nothing Then
                                        'Throw New ObjectMappingException("Cannot replace join")
                                        join = CType(mpe.GetJoins(joinableSchema, de_table.Key, tb, filterInfo), QueryJoin)
                                        de_table.Value._joins.Add(join)
                                        de_table.Value._where4update.AddFilter(
                                            New dc.EntityFilter(rt, pa, New ScalarValue(original), FilterOperation.Equal))
                                    Else
                                        de_table.Value._where4update.AddFilter(f)
                                    End If
                                End If
                            End If
                        End If
                    Next
                End If
            Next

            If Not pkSet Then
                Dim tbl = updated_tables.FirstOrDefault(Function(it) it.Key Is oschema.Table)
                If tbl.Key IsNot Nothing Then

                    For Each pk In obj.GetPKValues(oschema)
                        tbl.Value._where4update.AddFilter(New dc.TableFilter(tbl.Key, pk.Column, New ScalarValue(pk.Value), FilterOperation.Equal))
                    Next
                End If
            End If
        End Sub

        Protected Function HasUpdateColumns(ByVal sel_columns As Generic.IList(Of EntityPropertyAttribute),
            ByVal tables As IDictionary(Of SourceFragment, TableUpdate), ByVal esch As IEntitySchema) As Boolean

            If sel_columns.Count > 0 Then
                For Each c As EntityPropertyAttribute In sel_columns
                    If Not tables.ContainsKey(esch.FieldColumnMap()(c.PropertyAlias).Table) Then
                        Return False
                    End If
                Next

                Return True
            End If

            Return False
        End Function

        Protected Function HasUpdateColumnsForTable(ByVal sel_columns As Generic.IList(Of EntityExpression),
            ByVal table As SourceFragment, ByVal esch As IEntitySchema) As Boolean

            If sel_columns.Count > 0 Then
                For Each c As EntityExpression In sel_columns
                    If table Is esch.FieldColumnMap()(c.ObjectProperty.PropertyAlias).Table Then
                        Return True
                    End If
                Next
            End If

            Return False
        End Function

        Protected Overridable Function FormInsert(ByVal mpe As ObjectMappingEngine, ByVal inserted_tables As List(Of InsertedTable),
                                                  ByVal ins_cmd As StringBuilder, ByVal type As Type, ByVal os As IEntitySchema,
                                                  ByVal selectedProperties As List(Of EntityExpression),
                                                  ByVal params As ICreateParam, ByVal contextInfo As IDictionary) As ICollection(Of System.Data.Common.DbParameter)

            If params Is Nothing Then
                params = New ParamMgr(Me, "p")
            End If

            Dim le As ILastError = TryCast(Me, ILastError)

            Dim fromTable As String = Nothing

            If inserted_tables.Count > 0 Then
                Dim insertedPK As New List(Of ITemplateFilterBase.ColParam)
                Dim syncInsertPK As New List(Of ColType)
                Dim pk = os.GetPK
                Dim pkAttr = pk.Attributes

                If selectedProperties IsNot Nothing Then
                    Dim col As Collections.IndexedCollection(Of String, MapField2Column) = os.FieldColumnMap
                    'Dim ie As ICollection = mpe.GetProperties(type, os)
                    'If ie.Count = 0 AndAlso GetType(AnonymousCachedEntity).IsAssignableFrom(type) Then
                    '    ie = col
                    'End If

                    For Each clm In pk.SourceFields
                        Dim s As String = clm.GetPKParamName4Insert()
                        Dim dt As String = "int"
                        Dim db = clm.DBType
                        Dim pi As Reflection.PropertyInfo = clm.PropertyInfo
                        If pi IsNot Nothing Then 'AndAlso Not (pi.Name = "Identifier" AndAlso pi.DeclaringType.Name = GetType(OrmBaseT(Of )).Name) Then

                            If db.IsEmpty Then
                                dt = DbTypeConvertor.ToSqlDbType(pi.PropertyType).ToString
                            Else
                                dt = FormatDBType(db)
                            End If

                        End If
                        ins_cmd.Append(DeclareVariable(s, dt))
                        ins_cmd.Append(EndLine)
                        insertedPK.Add(New ITemplateFilterBase.ColParam With {.Column = clm.SourceFieldExpression, .Param = s})

                        If (pkAttr And Field2DbRelations.SyncInsert) = Field2DbRelations.SyncInsert Then
                            syncInsertPK.Add(New ColType With {.Column = clm.SourceFieldExpression, .Type = dt})
                        End If
                    Next

                    ins_cmd.Append(DeclareVariable("@rcount", "int"))
                    ins_cmd.Append(EndLine)
                    If inserted_tables.Count > 1 Then
                        ins_cmd.Append(DeclareVariable("@err", "int"))
                        ins_cmd.Append(EndLine)
                    End If
                    fromTable = DeclareOutput(ins_cmd, syncInsertPK)
                    If Not String.IsNullOrEmpty(fromTable) Then
                        ins_cmd.Append(EndLine)
                    End If
                End If
                Dim b As Boolean = False
                'Dim os As IOrmObjectSchema = GetObjectSchema(type)
                Dim pk_table As SourceFragment = os.Table
                For Each item As InsertedTable In inserted_tables
                    Dim insStart As Integer = ins_cmd.Length

                    If b Then
                        ins_cmd.Append(EndLine)
                        If SupportIf() Then
                            ins_cmd.Append("if @err = 0 ")
                        End If
                    Else
                        b = True
                    End If

                    'explicit pk (passed to insert statement as param)
                    Dim notSyncInsertPK As New List(Of ITemplateFilterBase.ColParam)

                    If item.Filters Is Nothing OrElse item.Filters.Count = 0 Then
                        ins_cmd.Append("insert into ").Append(GetTableName(item.Table, contextInfo)).Append(" ").Append(DefaultValues)
                    Else
                        ins_cmd.Append("insert into ").Append(GetTableName(item.Table, contextInfo)).Append(" (")
                        Dim values_sb As New StringBuilder
                        values_sb.Append(" values(")

                        For Each f As ITemplateFilter In item.Filters
                            Dim ef As EntityFilter = TryCast(f, EntityFilter)
                            If ef IsNot Nothing Then
                                CType(ef, IEntityFilter).PrepareValue = False
                            End If
                            Dim p = f.MakeSingleQueryStmt(mpe, Me, Nothing, params, item.Executor)
                            For Each cp In p
                                If ef IsNot Nothing Then
                                    'Dim p2 = ef.MakeSingleQueryStmt(os, Me, mpe, Nothing, params, item.Executor)
                                    Dim att As Field2DbRelations = os.FieldColumnMap(ef.Template.PropertyAlias).Attributes
                                    If (att And Field2DbRelations.SyncInsert) = 0 AndAlso (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                                        If mpe.GetPropertyTable(os, ef.Template.PropertyAlias) Is item.Table Then
                                            notSyncInsertPK.Add(New ITemplateFilterBase.ColParam With {.Column = cp.Column, .Param = cp.Param})
                                        Else
                                            ins_cmd.Length = insStart
                                            GoTo l1
                                        End If
                                    End If
                                    'End If
                                End If
                                ins_cmd.Append(cp.Column).Append(",")
                                values_sb.Append(cp.Param).Append(",")
                            Next
                        Next

                        ins_cmd.Length -= 1
                        values_sb.Length -= 1
                        ins_cmd.Append(") ")
                        If pk_table.Equals(item.Table) Then
                            ins_cmd.Append(InsertOutput(fromTable, syncInsertPK, notSyncInsertPK, TryCast(os, IChangeOutputOnInsert)))
                        End If
                        ins_cmd.Append(values_sb.ToString).Append(")")
                    End If

                    If pk_table.Equals(item.Table) AndAlso selectedProperties IsNot Nothing Then
                        ins_cmd.Append(EndLine)
                        ins_cmd.Append("select @rcount = ").Append(RowCount)
                        For Each ipk In insertedPK
                            Dim pr = notSyncInsertPK.FirstOrDefault(Function(p) p.Column = ipk.Column)
                            If pr IsNot Nothing Then
                                ins_cmd.Append(", ").Append(ipk.Param).Append(" = ").Append(pr.Param)
                            Else
                                Dim iv As IPKInsertValues = TryCast(os, IPKInsertValues)
                                If iv IsNot Nothing Then
                                    ins_cmd.Append(", ").Append(ipk.Param).Append(" = ").Append(iv.GetValue(ipk.Column))
                                Else
                                    If (pkAttr And Field2DbRelations.Identity) = Field2DbRelations.Identity Then
                                        ins_cmd.Append(", ").Append(ipk.Param).Append(" = ").Append(LastInsertID)
                                    End If
                                End If
                            End If
                            'If String.IsNullOrEmpty(identityPK) Then
                            '    ins_cmd.Append(", @id = ").Append(LastInsertID)
                            'Else
                            '    ins_cmd.Append(", @id = ").Append(identityPK)
                            'End If
                        Next
                        'ins_cmd.Append(EndLine)
                        If inserted_tables.Count > 1 Then
                            ins_cmd.Append(", @err = ").Append(le.LastError)
                        End If

                        If Not String.IsNullOrEmpty(fromTable) Then
                            ins_cmd.Append(" from ").Append(fromTable)
                        End If
                    End If
l1:
                Next

                If selectedProperties IsNot Nothing AndAlso selectedProperties.Count > 0 Then
                    'If unions Is Nothing Then
                    '    Dim rem_list As New ArrayList
                    '    For Each c As EntityPropertyAttribute In sel_columns
                    '        If Not tables.Contains(GetFieldTable(type, c.FieldName)) Then rem_list.Add(c)
                    '    Next

                    '    For Each c As EntityPropertyAttribute In rem_list
                    '        sel_columns.Remove(c)
                    '    Next
                    'End If

                    If selectedProperties.Count > 0 Then
                        'sel_columns.Sort()

                        ins_cmd.Append(EndLine)
                        If SupportIf() Then
                            ins_cmd.Append("if @rcount > 0 ")
                        End If

                        Dim selSb As New StringBuilder
                        selSb.Append("select ")
                        'Dim com As Boolean = False
                        'For Each c As EntityPropertyAttribute In sel_columns
                        '    If com Then
                        '        selSb.Append(", ")
                        '    Else
                        '        com = True
                        '    End If
                        '    If unions IsNot Nothing Then
                        '        Throw New NotImplementedException
                        '        'ins_cmd.Append(GetColumnNameByFieldName(type, c.FieldName, pk_table))
                        '        'If (c.SyncBehavior And Field2DbRelations.PrimaryKey) = Field2DbRelations.PrimaryKey Then
                        '        '    ins_cmd.Append("+").Append(GetUnionScope(type, pk_table).First)
                        '        'End If
                        '    Else
                        '        selSb.Append(mpe.GetColumnNameByPropertyAlias(os, c.PropertyAlias, Nothing))
                        '    End If
                        'Next

                        Dim almgr As AliasMgr = AliasMgr.Create
                        Dim selTbls As New List(Of SourceFragment)
                        For Each sp In selectedProperties
                            Dim sf = os.FieldColumnMap.Cast(Of MapField2Column).First(Function(it) it.PropertyAlias = sp.ObjectProperty.PropertyAlias).Table
                            If Not selTbls.Contains(sf) Then
                                selTbls.Add(sf)
                            End If
                        Next
                        Dim fromsb As New StringBuilder
                        AppendFrom(mpe, almgr, contextInfo, selTbls, fromsb, params, TryCast(os, IMultiTableObjectSchema), type)
                        Dim executor As New ExecutorCtx(type, os)
                        selSb.Append(BinaryExpressionBase.CreateFromEnumerable(selectedProperties).MakeStatement(mpe, Nothing, Me, params, almgr, contextInfo, MakeStatementMode.Select And MakeStatementMode.AddColumnAlias, executor))
                        'selSb.Append(" from ").Append(GetTableName(pk_table, contextInfo)).Append(" t1 where ")
                        selSb.Append(" from ").Append(fromsb.ToString)
                        'almgr.Replace(mpe, Me, pk_table, Nothing, selSb)
                        ins_cmd.Append(selSb.ToString).Append(" where ")

                        Dim cn As New PredicateLink
                        For Each ipk In insertedPK
                            cn = CType(cn.[and](pk_table, ipk.Column).eq(New LiteralValue(ipk.Param)), PredicateLink)
                        Next
                        'ins_cmd.Append(GetColumnNameByFieldName(os, GetPrimaryKeys(type, os)(0).PropertyAlias)).Append(" = @id")
                        ins_cmd.Append(cn.Filter.MakeQueryStmt(mpe, Nothing, Me, executor, contextInfo, almgr, Nothing))
                    End If
                End If
            End If

            Return params.Params
        End Function

        Public MustOverride ReadOnly Property Name() As String
        Public MustOverride Function DeclareVariable(ByVal name As String, ByVal type As String) As String
        Public Function DeclareVariableInc(ByVal name As String, ByVal type As String, ByRef idx As Integer, ByRef varName As String) As String
            varName = "{0}{1}".Format2(name, idx)
            Dim r = DeclareVariable(varName, type)
            idx += 1
            Return r
        End Function

#Region " data factory "
        Public MustOverride Function CreateDBCommand(ByVal timeout As Integer) As System.Data.Common.DbCommand

        Public MustOverride Function CreateDBCommand() As System.Data.Common.DbCommand

        Public MustOverride Function CreateDataAdapter() As System.Data.Common.DbDataAdapter

        Public MustOverride Function CreateDBParameter() As System.Data.Common.DbParameter

        Public MustOverride Function CreateDBParameter(ByVal name As String, ByVal value As Object) As System.Data.Common.DbParameter

        Public MustOverride Function CreateCommandBuilder(ByVal da As System.Data.Common.DbDataAdapter) As System.Data.Common.DbCommandBuilder

        Public MustOverride Function CreateConnection(ByVal connectionString As String, info As InfoMessageDelagate) As System.Data.Common.DbConnection

#End Region

        Public Overridable Property QueryLength As Integer = 490
        Public MustOverride Function RollbackSavepoint(name As String) As String
        Public MustOverride Function Savepoint(name As String) As String
        Public Overridable ReadOnly Property IsSavepointsSupported As Boolean = False
    End Class

    Public Interface ITopStatement
        Function TopStatementPercent(ByVal top As Integer, ByVal percent As Boolean, ByVal ties As Boolean) As String
    End Interface

    Interface ILastError
        ReadOnly Property LastError() As String
    End Interface

    <Serializable()>
    Public Class DbGeneratorException
        Inherits System.Exception

        Public Sub New(ByVal message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(ByVal message As String, ByVal inner As Exception)
            MyBase.New(message, inner)
        End Sub

        Protected Sub New(
            ByVal info As System.Runtime.Serialization.SerializationInfo,
            ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
        End Sub
    End Class

    Public Class InsertedTable
        Private ReadOnly _tbl As SourceFragment
        Private ReadOnly _f As List(Of ITemplateFilter)
        Private _ex As Query.IExecutionContext

        Public Sub New(ByVal tbl As SourceFragment, ByVal f As List(Of ITemplateFilter))
            _tbl = tbl
            _f = f
        End Sub

        Public Property Executor() As Query.IExecutionContext
            Get
                Return _ex
            End Get
            Set(ByVal value As Query.IExecutionContext)
                _ex = value
            End Set
        End Property

        Public ReadOnly Property Filters() As List(Of ITemplateFilter)
            Get
                Return _f
            End Get
        End Property

        Public ReadOnly Property Table() As SourceFragment
            Get
                Return _tbl
            End Get
        End Property

    End Class
    Public Class ColType
        Public Column As String
        Public Type As String
    End Class
    Module Extensions
        <Extension>
        Public Function GetPKParamName4Insert(col As SourceField) As String
            Dim clm As String = col.SourceFieldExpression.ClearSourceField
            Return "@pk_" & clm
        End Function
        <Extension>
        Public Function ClearSourceField(sf As String) As String
            Return sf.Replace(".", "").Trim("]"c, "["c, """"c)
        End Function
    End Module
End Namespace