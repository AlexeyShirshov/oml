Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Criteria
Imports Worm.Criteria.Values
Imports dc = Worm.Criteria.Core
Imports Worm.Query.Sorting
Imports Worm.Cache
Imports Worm.Criteria.Core
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Joins
Imports Worm.Query
Imports Worm.Expressions2

Namespace Database

    Public Class SQL2000Generator
        Inherits DbGenerator
        Implements ITopStatement, ILastError

        'Public Const MainRelationTag As String = "main"
        'Public Const SubRelationTag As String = "sub"

        'Public Sub New(ByVal version As String)
        '    MyBase.New(version)
        'End Sub

        'Public Sub New(ByVal version As String, ByVal resolveEntity As ResolveEntity)
        '    MyBase.New(version, resolveEntity)
        'End Sub

        'Public Sub New(ByVal version As String, ByVal resolveName As ResolveEntityName)
        '    MyBase.New(version, resolveName)
        'End Sub

        'Public Sub New(ByVal version As String, ByVal resolveEntity As ResolveEntity, ByVal resolveName As ResolveEntityName)
        '    MyBase.New(version, resolveEntity, resolveName)
        'End Sub

        'Public Overloads Function GetObjectSchema(ByVal t As Type) As IOrmObjectSchema
        '    Return CType(MyBase.GetObjectSchema(t), IOrmObjectSchema)
        'End Function

        Public Overridable Property UseSchema As Boolean = True
#Region " engine properties "
        Public Overridable ReadOnly Property NullIsZero() As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overridable ReadOnly Property SupportFullTextSearch() As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides Function ParamName(ByVal name As String, ByVal i As Integer) As String
            Return "@" & name & i
        End Function

        'Public Overridable ReadOnly Property TablePrefix() As String
        '    Get
        '        Return "dbo."
        '    End Get
        'End Property

        Public Overrides ReadOnly Property Name() As String
            Get
                Return "SQL Server 2000"
            End Get
        End Property

        Public Overridable ReadOnly Property LastError() As String Implements ILastError.LastError
            Get
                Return "@@error"
            End Get
        End Property

        Public Overrides Function DeclareVariable(ByVal name As String, ByVal type As String) As String
            If String.IsNullOrEmpty(name) Then
                Throw New ArgumentNullException("name")
            End If

            If Not name.StartsWith("@") Then
                name = "@" & name
            End If

            Return "declare " & name & " " & type
        End Function

        Public Overrides Function TopStatement(ByVal top As Integer) As String
            Return "top " & top & " "
        End Function

        Public Overridable Function TopStatementPercent(ByVal top As Integer, ByVal percent As Boolean, ByVal ties As Boolean) As String Implements ITopStatement.TopStatementPercent
            Dim sb As New StringBuilder
            sb.Append("top ").Append(top).Append(" ")
            If percent Then
                sb.Append("percent ")
            End If
            If ties Then
                sb.Append(" with ties ")
            End If
            Return sb.ToString
        End Function

        Public Overridable ReadOnly Property IsNull() As String
            Get
                Return "isnull"
            End Get
        End Property

        Public Overrides ReadOnly Property GetDate() As String
            Get
                Return "getdate()"
            End Get
        End Property

        Public Overridable ReadOnly Property GetUTCDate() As String
            Get
                Return "getutcdate()"
            End Get
        End Property

        Public Overrides ReadOnly Property Selector() As String
            Get
                Return "."
            End Get
        End Property

        Public Overrides Function SupportMultiline() As Boolean
            Return True
        End Function
#End Region

#Region " data factory "
        Public Overrides Function CreateDBCommand(ByVal timeout As Integer) As System.Data.Common.DbCommand
            Dim cmd As New SqlCommand
            cmd.CommandTimeout = timeout
            Return cmd
        End Function

        Public Overrides Function CreateDBCommand() As System.Data.Common.DbCommand
            Dim cmd As New SqlCommand
            'cmd.CommandTimeout = XMedia.Framework.Configuration.ApplicationConfiguration.CommandTimeout \ 1000
            Return cmd
        End Function

        Public Overrides Function CreateDataAdapter() As System.Data.Common.DbDataAdapter
            Return New SqlDataAdapter
        End Function

        Public Overrides Function CreateDBParameter() As System.Data.Common.DbParameter
            Return New SqlParameter
        End Function

        Public Overrides Function CreateDBParameter(ByVal name As String, ByVal value As Object) As System.Data.Common.DbParameter
            Return New SqlParameter(name, value)
        End Function

        Public Overrides Function CreateCommandBuilder(ByVal da As System.Data.Common.DbDataAdapter) As System.Data.Common.DbCommandBuilder
            Return New SqlCommandBuilder(CType(da, SqlDataAdapter))
        End Function

        Public Overrides Function CreateConnection(ByVal connectionString As String, info As InfoMessageDelagate) As System.Data.Common.DbConnection
            Dim conn As New SqlConnection(connectionString)
            If info IsNot Nothing Then
                AddHandler conn.InfoMessage, Sub(s, e)
                                                 info(e)
                                             End Sub
            End If
            Return conn
        End Function

#End Region

#Region " Statements "

        Public Shared Function NeedJoin(ByVal schema As IEntitySchema) As Boolean
            Dim r As Boolean = False
            Dim j As IJoinBehavior = TryCast(schema, IJoinBehavior)
            If j IsNot Nothing Then
                r = j.AlwaysJoinMainTable
            End If
            Return r
        End Function

        Protected Delegate Function ConvertDelegate(Of T, T2)(ByVal source As T) As T2

        Protected Function ConvertAll(Of T, T2)(ByVal arr As IList(Of T), ByVal func As ConvertDelegate(Of T, T2)) As IList(Of T2)
            Dim l As New List(Of T2)
            For Each k As T In arr
                l.Add(func(k))
            Next
            Return l
        End Function

#If Not ExcludeFindMethods Then

        Public Overridable Function SelectWithJoin(ByVal mpe As ObjectMappingEngine, ByVal original_type As Type, ByVal tables() As SourceFragment, _
            ByVal almgr As IPrepareTable, ByVal params As ICreateParam, ByVal joins() As Worm.Criteria.Joins.QueryJoin, _
            ByVal wideLoad As Boolean, ByVal aspects() As QueryAspect, ByVal additionalColumns As String, _
            ByVal arr As Generic.IList(Of EntityPropertyAttribute), ByVal schema As IEntitySchema, ByVal filterInfo As Object) As String

            Dim selectcmd As New StringBuilder
            'Dim maintable As String = tables(0)
            selectcmd.Append("select ")
            If aspects IsNot Nothing Then
                For Each asp As QueryAspect In aspects
                    If asp.AscpectType = QueryAspect.AspectType.Columns Then
                        selectcmd.Append(asp.MakeStmt(Me))
                    End If
                Next
            End If

            If original_type IsNot Nothing Then
                If wideLoad Then
                    Dim columns As String = mpe.GetSelectColumnList(original_type, mpe, arr, schema, Nothing)
                    If Not String.IsNullOrEmpty(columns) Then
                        selectcmd.Append(columns)
                        If Not String.IsNullOrEmpty(additionalColumns) Then
                            selectcmd.Append(",").Append(additionalColumns)
                        End If
                    Else

                    End If
                Else
                    mpe.GetPKList(original_type, mpe, schema, selectcmd, Nothing)
                End If
            Else
                selectcmd.Append("*")
            End If

            selectcmd.Append(" from ")
            Dim unions() As String = Nothing
            If original_type IsNot Nothing Then
                unions = ObjectMappingEngine.GetUnions(original_type)
            End If
            'Dim pmgr As ParamMgr = params 'New ParamMgr()

            If unions Is Nothing Then
                AppendFrom(mpe, almgr, filterInfo, tables, selectcmd, params, _
                           TryCast(schema, IMultiTableObjectSchema), original_type)
                If joins IsNot Nothing Then
                    For i As Integer = 0 To joins.Length - 1
                        Dim join As QueryJoin = CType(joins(i), QueryJoin)

                        If Not QueryJoin.IsEmpty(join) Then
                            'almgr.AddTable(join.Table, CType(Nothing, ParamMgr))
                            join.MakeSQLStmt(mpe, Nothing, Me, Nothing, filterInfo, almgr, params, Nothing, selectcmd)
                        End If
                    Next
                End If
            Else
                Throw New NotImplementedException
            End If

            Return selectcmd.ToString
        End Function

        Public Overridable Function SelectWithJoin(ByVal mpe As ObjectMappingEngine, ByVal original_type As Type, _
            ByVal almgr As IPrepareTable, ByVal params As ICreateParam, ByVal joins() As Worm.Criteria.Joins.QueryJoin, _
            ByVal wideLoad As Boolean, ByVal aspects() As QueryAspect, ByVal additionalColumns As String, _
            ByVal filterInfo As Object, ByVal arr As Generic.IList(Of EntityPropertyAttribute)) As String

            If original_type Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "original_type")
            End If

            If almgr Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "almgr")
            End If

            Dim schema As IEntitySchema = mpe.GetEntitySchema(original_type)

            Return SelectWithJoin(mpe, original_type, mpe.GetTables(schema), almgr, params, joins, wideLoad, aspects, _
                additionalColumns, arr, schema, filterInfo)
        End Function

        Public Overridable Function SelectDistinct(ByVal mpe As ObjectMappingEngine, ByVal original_type As Type, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal relation As M2MRelationDesc, _
            ByVal wideLoad As Boolean, ByVal appendSecondTable As Boolean, ByVal filterInfo As Object, _
            ByVal arr As Generic.IList(Of EntityPropertyAttribute)) As String

            If original_type Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "original_type")
            End If

            If almgr Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "almgr")
            End If

            Dim schema As IEntitySchema = mpe.GetEntitySchema(original_type)
            Dim selectcmd As New StringBuilder
            Dim tables() As SourceFragment = mpe.GetTables(schema)
            'Dim maintable As String = tables(0)
            selectcmd.Append("select distinct ")
            If wideLoad Then
                Dim columns As String = mpe.GetSelectColumnList(original_type, mpe, arr, schema, Nothing)
                selectcmd.Append(columns)
            Else
                mpe.GetPKList(original_type, mpe, schema, selectcmd, Nothing)
            End If
            selectcmd.Append(" from ")
            Dim unions() As String = ObjectMappingEngine.GetUnions(original_type)
            Dim pmgr As ParamMgr = params 'New ParamMgr()

            If unions Is Nothing Then
                AppendFrom(mpe, almgr, filterInfo, tables, selectcmd, pmgr, _
                           TryCast(schema, IMultiTableObjectSchema), original_type)

                Dim r2 As M2MRelationDesc = mpe.GetM2MRelation(relation.Entity.GetRealType(mpe), original_type, True)
                Dim tbl As SourceFragment = relation.Table
                If tbl Is Nothing Then
                    If relation.ConnectedType IsNot Nothing Then
                        tbl = mpe.GetTables(relation.ConnectedType)(0)
                    Else
                        Throw New InvalidOperationException(String.Format("Relation from type {0} to {1} has not table", original_type, relation.Entity.GetRealType(mpe)))
                    End If
                End If

                Dim f As New JoinFilter(tbl, r2.Column, original_type, mpe.GetPrimaryKeys(original_type, schema)(0).PropertyAlias, FilterOperation.Equal)
                Dim join As New QueryJoin(tbl, Joins.JoinType.Join, f)

                almgr.AddTable(tbl, CType(Nothing, EntityUnion))
                join.MakeSQLStmt(mpe, Nothing, Me, Nothing, filterInfo, almgr, params, Nothing, selectcmd)

                If appendSecondTable Then
                    Dim schema2 As IEntitySchema = mpe.GetEntitySchema(relation.Entity.GetRealType(mpe))
                    AppendNativeTypeJoins(mpe, relation.Entity.GetRealType(mpe), almgr, mpe.GetTables(relation.Entity.GetRealType(mpe)), selectcmd, params, tbl, relation.Column, True, filterInfo, schema2)
                End If
            Else
                Throw New NotImplementedException
            End If

            Return selectcmd.ToString
        End Function

        Public Function [Select](ByVal mpe As ObjectMappingEngine, ByVal original_type As Type, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal queryAspect() As QueryAspect, _
            ByVal arr As Generic.IList(Of EntityPropertyAttribute), _
             ByVal additionalColumns As String, ByVal filterInfo As Object) As String
            Return SelectWithJoin(mpe, original_type, almgr, params, Nothing, True, queryAspect, additionalColumns, filterInfo, arr)
        End Function

        Public Function SelectID(ByVal mpe As ObjectMappingEngine, ByVal original_type As Type, ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal filterInfo As Object) As String
            Return SelectWithJoin(mpe, original_type, almgr, params, Nothing, False, Nothing, Nothing, filterInfo, Nothing)
        End Function

        ''' <summary>
        ''' Добавление джоинов для типа, когда основная таблица может не линковаться
        ''' </summary>
        ''' <param name="selectedType"></param>
        ''' <param name="almgr"></param>
        ''' <param name="tables"></param>
        ''' <param name="selectcmd"></param>
        ''' <param name="pname"></param>
        ''' <param name="table"></param>
        ''' <param name="id">имя внешнего ключевого поля</param>
        ''' <param name="appendMainTable"></param>
        ''' <remarks></remarks>
        Protected Friend Sub AppendNativeTypeJoins(ByVal mpe As ObjectMappingEngine, ByVal selectedType As Type, ByVal almgr As AliasMgr, _
            ByVal tables As SourceFragment(), ByVal selectcmd As StringBuilder, ByVal pname As ParamMgr, _
            ByVal table As SourceFragment, ByVal id As String, ByVal appendMainTable As Boolean, _
            ByVal filterInfo As Object, ByVal schema As IEntitySchema) ', Optional ByVal dic As IDictionary(Of SourceFragment, SourceFragment) = Nothing)

            Dim pkTable As SourceFragment = mpe.GetPKTable(selectedType, schema)
            Dim sch As IMultiTableObjectSchema = TryCast(schema, IMultiTableObjectSchema)

            If Not appendMainTable Then
                If sch IsNot Nothing Then
                    For j As Integer = 0 To tables.Length - 1
                        If tables(j) Is pkTable Then Continue For

                        Dim join As QueryJoin = CType(mpe.GetJoins(sch, pkTable, tables(j), filterInfo), QueryJoin)

                        If Not QueryJoin.IsEmpty(join) Then
                            almgr.AddTable(tables(j), Nothing, pname)

                            join.InjectJoinFilter(mpe, selectedType, mpe.GetPrimaryKeys(selectedType, schema)(0).PropertyAlias, table, id)
                            'OrmFilter.ChangeValueToLiteral(join, selectedType, "ID", table, id)
                            'For Each fl As OrmFilter In join.Condition.GetAllFilters
                            '    If Not fl.IsLiteralValue Then
                            '        Dim f2 As IOrmFilter = Nothing
                            '        If fl.FieldName = "ID" Then
                            '            f2 = New OrmFilter(table, id, fl.GetTable(Me), fl.SecondFieldPair.Second, FilterOperation.Equal)
                            '        ElseIf fl.SecondFieldPair.Second = "ID" Then
                            '            f2 = New OrmFilter(table, id, fl.GetTable(Me), fl.FieldName, FilterOperation.Equal)
                            '            'Else
                            '            'Throw New DBSchemaException("invalid join")
                            '        End If

                            '        If f2 IsNot Nothing Then
                            '            join.ReplaceFilter(fl, f2)
                            '            Exit For
                            '        End If
                            '    End If
                            'Next

                            join.MakeSQLStmt(mpe, Nothing, Me, Nothing, filterInfo, almgr, pname, Nothing, selectcmd)
                        End If
                    Next
                End If
            Else
                Dim tbl As SourceFragment = pkTable
                tbl = tbl.OnTableAdd(pname)
                Dim adal As Boolean
                If tbl Is Nothing Then
                    tbl = pkTable
                Else 'If dic IsNot Nothing Then
                    'dic.Add(pk_table, tbl)
                    adal = True
                End If
                Dim j As New QueryJoin(tbl, Joins.JoinType.Join, _
                    New JoinFilter(table, id, selectedType, mpe.GetPrimaryKeys(selectedType, schema)(0).PropertyAlias, FilterOperation.Equal))
                Dim al As String = almgr.AddTable(tbl, Nothing, pname)
                If adal Then
                    almgr.AddTable(pkTable, al)
                End If
                j.MakeSQLStmt(mpe, Nothing, Me, Nothing, filterInfo, almgr, pname, Nothing, selectcmd)
                If sch IsNot Nothing Then
                    For i As Integer = 0 To tables.Length - 1
                        If tables(i) Is pkTable Then Continue For

                        Dim join As QueryJoin = CType(mpe.GetJoins(sch, pkTable, tables(i), filterInfo), QueryJoin)

                        If Not QueryJoin.IsEmpty(join) Then
                            almgr.AddTable(tables(i), Nothing, pname)
                            join.MakeSQLStmt(mpe, Nothing, Me, Nothing, filterInfo, almgr, pname, Nothing, selectcmd)
                        End If
                    Next
                End If
            End If
        End Sub

        ''' <summary>
        ''' Построение таблиц для many to many связи
        ''' </summary>
        ''' <param name="selectedType"></param>
        ''' <param name="almgr"></param>
        ''' <param name="tables"></param>
        ''' <param name="selectcmd"></param>
        ''' <param name="pname"></param>
        ''' <param name="table"></param>
        ''' <param name="id"></param>
        ''' <param name="appendMainTable"></param>
        ''' <remarks></remarks>
        Protected Sub AppendFromM2M(ByVal mpe As ObjectMappingEngine, ByVal selectedType As Type, ByVal almgr As AliasMgr, ByVal tables As SourceFragment(), _
            ByVal selectcmd As StringBuilder, ByVal pname As ParamMgr, ByVal table As SourceFragment, ByVal id As String, _
            ByVal appendMainTable As Boolean, ByVal filterInfo As Object, ByVal schema As IEntitySchema)

            If table Is Nothing Then
                Throw New ArgumentNullException("table parameter cannot be nothing")
            End If

            'Dim schema As IOrmObjectSchema = GetObjectSchema(original_type)

            selectcmd.Append(GetTableName(table)).Append(" ").Append(almgr.GetAlias(table, Nothing))

            'Dim f As IOrmFilter = schema.GetFilter(filter_info)
            'Dim dic As New Generic.Dictionary(Of SourceFragment, SourceFragment)
            AppendNativeTypeJoins(mpe, selectedType, almgr, tables, selectcmd, pname, table, id, appendMainTable, filterInfo, schema)

            For Each tbl As SourceFragment In tables
                'Dim newt As SourceFragment = Nothing
                'If dic.TryGetValue(tbl, newt) Then
                '    If almgr.Aliases.ContainsKey(newt) Then
                '        Dim [alias] As String = almgr.Aliases(newt)
                '        selectcmd = selectcmd.Replace(tbl.TableName & ".", [alias] & ".")
                '    End If
                'Else
                If almgr.ContainsKey(tbl, Nothing) Then
                    'Dim [alias] As String = almgr.Aliases(tbl)
                    'selectcmd = selectcmd.Replace(tbl.TableName & ".", [alias] & ".")
                    'almgr.Replace(mpe, Me, tbl, Nothing, selectcmd)
                End If
                'End If
            Next
        End Sub
#End If

        'Public Sub AppendOrder(ByVal mpe As ObjectMappingEngine, ByVal sort As OrderByClause, ByVal almgr As IPrepareTable, _
        '    ByVal sb As StringBuilder, ByVal appendOrder As Boolean, _
        '    ByVal selList As ObjectModel.ReadOnlyCollection(Of SelectExpressionOld))
        '    sb.Append(BinaryExpressionBase.CreateFromEnumerable(sort).MakeStatement(mpe, Nothing, Me, Nothing, almgr, Nothing, MakeStatementMode.None, Nothing))
        'End Sub

#If Not ExcludeFindMethods Then

        Public Function SelectM2M(ByVal mpe As ObjectMappingEngine, ByVal selectedType As Type, ByVal filteredType As Type, ByVal aspects() As QueryAspect, _
            ByVal appendMainTable As Boolean, ByVal appJoins As Boolean, ByVal filterInfo As Object, _
            ByVal pmgr As ParamMgr, ByVal almgr As AliasMgr, ByVal withLoad As Boolean, ByVal key As String) As String

            Dim schema As IEntitySchema = mpe.GetEntitySchema(selectedType)
            'Dim schema2 As IOrmObjectSchema = GetObjectSchema(filteredType)

            'column - select
            Dim selected_r As M2MRelationDesc = Nothing
            'column - filter
            Dim filtered_r As M2MRelationDesc = Nothing

            filtered_r = mpe.GetM2MRelation(selectedType, filteredType, key)
            selected_r = mpe.GetRevM2MRelation(selectedType, filteredType, key)

            If selected_r Is Nothing Then
                Throw New ObjectMappingException(String.Format("Type {0} has no relation to {1}", selectedType.Name, filteredType.Name))
            End If

            If filtered_r Is Nothing Then
                Throw New ObjectMappingException(String.Format("Type {0} has no relation to {1}", filteredType.Name, selectedType.Name))
            End If

            Dim table As SourceFragment = selected_r.Table

            If table Is Nothing Then
                Throw New ArgumentException("Invalid relation", filteredType.ToString)
            End If

            Dim [alias] As String = almgr.AddTable(table, Nothing, pmgr)

            Dim sb As New StringBuilder
            Dim id_clm As String = selected_r.Column
            sb.Append("select ")
            If aspects IsNot Nothing Then
                For Each asp As QueryAspect In aspects
                    If asp.AscpectType = QueryAspect.AspectType.Columns Then
                        sb.Append(asp.MakeStmt(Me))
                    End If
                Next
            End If
            sb.Append([alias]).Append(Selector)
            sb.Append(filtered_r.Column).Append(" ").Append(filteredType.Name).Append(mpe.GetPrimaryKeys(filteredType)(0).PropertyAlias)
            If filteredType Is selectedType Then
                sb.Append("Rev")
            End If
            sb.Append(",").Append([alias]).Append(Selector).Append(id_clm)
            sb.Append(" ").Append(selectedType.Name).Append(mpe.GetPrimaryKeys(selectedType, schema)(0).PropertyAlias)
            If withLoad Then
                Dim s As String = mpe.GetSelectColumnListWithoutPK(selectedType, mpe, Nothing, schema, Nothing)
                If Not String.IsNullOrEmpty(s) Then
                    sb.Append(",").Append(s)
                    appendMainTable = True
                End If
            End If
            sb.Append(" from ")

            Dim ms As IMultiTableObjectSchema = TryCast(schema, IMultiTableObjectSchema)
            If appJoins AndAlso ms IsNot Nothing Then
                AppendFromM2M(mpe, selectedType, almgr, ms.GetTables, sb, pmgr, table, id_clm, appendMainTable, filterInfo, schema)
                'For Each tbl As SourceFragment In schema.GetTables
                '    If almgr.Aliases.ContainsKey(tbl) Then
                '        [alias] = almgr.Aliases(tbl)
                '        sb = sb.Replace(tbl.TableName & ".", [alias] & ".")
                '    End If
                'Next
            Else
                'Dim tbl As SourceFragment = schema.GetTables(0)
                AppendFromM2M(mpe, selectedType, almgr, New SourceFragment() {schema.Table}, sb, pmgr, table, id_clm, appendMainTable, filterInfo, schema)
                'If almgr.Aliases.ContainsKey(tbl) Then
                '    sb = sb.Replace(tbl.TableName & ".", almgr.Aliases(tbl) & ".")
                'End If
            End If

            Return sb.ToString
        End Function

        Public Function SelectM2M(ByVal mpe As ObjectMappingEngine, ByVal almgr As AliasMgr, ByVal obj As IKeyEntity, ByVal type As Type, ByVal filter As Worm.Criteria.Core.IFilter, _
            ByVal filter_info As Object, ByVal appJoins As Boolean, ByVal withLoad As Boolean, ByVal appendMain As Boolean, _
            ByRef params As IList(Of System.Data.Common.DbParameter), ByVal direct As String) As String
            Return SelectM2M(mpe, almgr, obj, type, filter, filter_info, appJoins, withLoad, appendMain, params, direct, New QueryAspect() {})
        End Function

        Public Function SelectM2M(ByVal mpe As ObjectMappingEngine, ByVal almgr As AliasMgr, ByVal obj As IKeyEntity, ByVal type As Type, ByVal filter As Worm.Criteria.Core.IFilter, _
            ByVal filter_info As Object, ByVal appJoins As Boolean, ByVal withLoad As Boolean, ByVal appendMain As Boolean, _
            ByRef params As IList(Of System.Data.Common.DbParameter), ByVal key As String, ByVal aspects() As QueryAspect) As String

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim pmgr As New ParamMgr(Me, "p")

            Dim sb As New StringBuilder

            Dim t As System.Type = obj.GetType

            'Dim schema As IOrmObjectSchema = GetObjectSchema(t)
            Dim schema2 As IEntitySchema = mpe.GetEntitySchema(type)
            Dim cs As IContextObjectSchema = TryCast(schema2, IContextObjectSchema)

            Dim appendMainTable As Boolean = filter IsNot Nothing OrElse _
                (cs IsNot Nothing AndAlso cs.GetContextFilter(filter_info) IsNot Nothing) OrElse _
                appendMain OrElse SQLGenerator.NeedJoin(schema2)
            sb.Append(SelectM2M(mpe, type, t, aspects, appendMainTable, appJoins, filter_info, pmgr, almgr, withLoad, key))

            Dim selected_r As M2MRelationDesc = Nothing
            Dim filtered_r As M2MRelationDesc = Nothing

            filtered_r = mpe.GetM2MRelation(type, t, key)
            selected_r = mpe.GetRevM2MRelation(type, t, key)

            If selected_r Is Nothing Then
                Throw New ObjectMappingException(String.Format("Type {0} has no relation to {1}", t.Name, type.Name))
            End If

            If filtered_r Is Nothing Then
                Throw New ObjectMappingException(String.Format("Type {0} has no relation to {1}", type.Name, t.Name))
            End If

            Dim id_clm As String = filtered_r.Column

            Dim table As SourceFragment = selected_r.Table

            Dim f As New dc.TableFilter(table, id_clm, New EntityValue(obj), FilterOperation.Equal)
            Dim con As New Condition.ConditionConstructor
            con.AddFilter(f)
            con.AddFilter(filter)
            AppendWhere(mpe, type, con.Condition, almgr, sb, filter_info, pmgr)

            params = pmgr.Params

            Return sb.ToString
        End Function

        Public Function MakeSearchStatement(ByVal mpe As ObjectMappingEngine, ByVal searchType As Type, ByVal selectType As Type, _
            ByVal fts As IFtsStringFormatter, ByVal fields As ICollection(Of Pair(Of String, Type)), _
            ByVal sectionName As String, ByVal joins As ICollection(Of Worm.Criteria.Joins.QueryJoin), ByVal sort_type As SortExpression.SortType, _
            ByVal params As ParamMgr, ByVal filter_info As Object, ByVal queryFields As String(), _
            ByVal top As Integer, ByVal table As String, _
            ByVal sort As SortExpression(), ByVal appendBySort As Boolean, ByVal filter As IFilter, ByVal contextKey As Object, _
            ByVal selSchema As IEntitySchema, ByVal searchSchema As IEntitySchema) As String

            'If searchType IsNot selectType AndAlso join.IsEmpty Then
            '    Throw New ArgumentException("Join is empty while type to load differs from type to search")
            'End If

            'If Not join.IsEmpty AndAlso searchType Is selectType Then
            '    Throw New ArgumentException("Join is not empty while type to load the same as type to search")
            'End If

            'Dim selSchema As IOrmObjectSchema = GetObjectSchema(selectType)
            'Dim searchSchema As IOrmObjectSchema = GetObjectSchema(searchType)
            Dim fs As IFullTextSupport = TryCast(searchSchema, IFullTextSupport)

            'Dim value As String = del(tokens, sectionName, fs, contextKey)
            Dim value As String = fts.GetFtsString(sectionName, contextKey, fs, searchType, table)
            If String.IsNullOrEmpty(value) Then
                Return Nothing
            End If

            Dim almgr As AliasMgr = AliasMgr.Create
            Dim ct As New SourceFragment(table)
            Dim [alias] As String = almgr.AddTable(ct, CType(Nothing, EntityUnion))
            Dim pname As String = params.CreateParam(value)
            'cols = New Generic.List(Of EntityPropertyAttribute)
            Dim sb As New StringBuilder, columns As New StringBuilder
            'Dim tbl As SourceFragment = GetTables(t)(0)
            sb.Append("select ")
            Dim appendMain As Boolean = appendBySort
            Dim selTable As SourceFragment = mpe.GetTables(selectType)(0)
            Dim searchTable As SourceFragment = mpe.GetTables(searchType)(0)
            Dim ins_idx As Integer = sb.Length
            If fields IsNot Nothing AndAlso fields.Count > 0 Then
                'If searchType IsNot selectType Then
                '    For Each field As Pair(Of String, Type) In fields
                '        If field.Second Is selectType Then
                '            Dim m As MapField2Column = selSchema.GetFieldColumnMap(field.First)
                '            If appendMain Then
                '                columns.Append(m._tableName).Append(".")
                '                columns.Append(m._columnName).Append(",")
                '            Else
                '                columns.Append([alias]).Append(".[key] ").Append(m._columnName).Append(",")
                '            End If
                '        End If
                '    Next
                For Each field As Pair(Of String, Type) In fields
                    Dim m As MapField2Column = Nothing
                    If field.Second Is searchType Then
                        m = searchSchema.GetFieldColumnMap(field.First)
                        appendMain = True
                    ElseIf field.Second Is selectType Then
                        m = selSchema.GetFieldColumnMap(field.First)
                    Else
                        Throw New InvalidOperationException("Type " & field.Second.ToString & " is not select type or search type")
                    End If
                    columns.Append(m.Table.UniqueName(Nothing)).Append(mpe.Delimiter)
                    columns.Append(m.ColumnExpression).Append(",")
                Next
                '    For Each field As Pair(Of String, Type) In fields
                '        If field.Second Is searchType Then
                '            Dim m As MapField2Column = searchSchema.GetFieldColumnMap(field.First)
                '            columns.Append(m._tableName).Append(".")
                '            columns.Append(m._columnName).Append(",")
                '        End If
                '    Next
                'Else
                '    For Each field As Pair(Of String, Type) In fields
                '        appendMain = True
                '        Dim m As MapField2Column = searchSchema.GetFieldColumnMap(field.First)
                '        columns.Append(GetTableName(m._tableName)).Append(".")
                '        columns.Append(m._columnName).Append(",")
                '    Next
                'End If
                'For Each field As Pair(Of String, Type) In fields
                '    If field.Second Is searchType Then
                '        appendMain = True
                '        Dim m As MapField2Column = searchSchema.GetFieldColumnMap(field.First)
                '        columns.Append(m._tableName).Append(".")
                '        columns.Append(m._columnName).Append(",")
                '    ElseIf field.Second Is selectType Then
                '        Dim m As MapField2Column = selSchema.GetFieldColumnMap(field.First)
                '        columns.Append(m._tableName).Append(".")
                '        columns.Append(m._columnName).Append(",")
                '    End If
                'Next
                If columns.Length > 0 Then
                    columns.Length -= 1
                End If
            Else
                'Dim m As MapField2Column = searchSchema.GetFieldColumnMap(OrmBaseT.PKName)
                sb.Append("[key] ").Append(mpe.GetPrimaryKeys(searchType, searchSchema)(0).PropertyAlias)
            End If
            sb.Append(" from ").Append(table).Append("(")
            Dim tf As ITableFunction = TryCast(searchTable, ITableFunction)
            If tf Is Nothing Then
                sb.Append(GetTableName(searchTable))
            Else
                sb.Append(tf.GetRealTable("*"))
                appendMain = True
            End If
            sb.Append(",")
            If queryFields Is Nothing OrElse queryFields.Length = 0 Then
                sb.Append("*")
            Else
                sb.Append("(")
                For Each f As String In queryFields
                    Dim m As MapField2Column = searchSchema.GetFieldColumnMap(f)
                    sb.Append(m.ColumnExpression).Append(",")
                Next
                sb.Length -= 1
                sb.Append(")")
            End If
            sb.Append(",")
            sb.Append(pname)
            If top <> Integer.MinValue Then
                sb.Append(",").Append(top)
            End If
            sb.Append(") ").Append([alias])
            Dim cs As IContextObjectSchema = TryCast(selSchema, IContextObjectSchema)
            If Not appendMain AndAlso cs IsNot Nothing Then
                appendMain = cs.GetContextFilter(filter_info) IsNot Nothing
            End If
            AppendNativeTypeJoins(mpe, searchType, almgr, mpe.GetTables(searchType), sb, params, ct, FTSKey, appendMain, filter_info, searchSchema)
            'If fields.Count > 0 Then
            If appendMain Then
                'Dim mainAlias As String = almgr.Aliases(searchTable)
                'columns = columns.Replace(searchTable.TableName & ".", mainAlias & ".")
                almgr.Replace(mpe, Me, searchTable, Nothing, columns)
            End If
            'If searchType IsNot selectType Then
            '    almgr.AddTable(selTable)
            '    Dim joinAlias As String = almgr.Aliases(selTable)
            '    columns = columns.Replace(selTable.TableName & ".", joinAlias & ".")
            'End If
            'End If

            For Each join As QueryJoin In joins
                If Not QueryJoin.IsEmpty(join) Then
                    'Dim tm As OrmFilterTemplate = CType(join.InjectJoinFilter(searchType, "ID", ct, "[key]"), OrmFilterTemplate)
                    'If tm Is Nothing Then
                    '    Throw New DBSchemaException("Invalid join")
                    'End If
                    join.InjectJoinFilter(mpe, searchType, mpe.GetPrimaryKeys(searchType, searchSchema)(0).PropertyAlias, ct, FTSKey)
                    'Dim al As String = almgr.AddTable(join.Table)
                    'columns = columns.Replace(join.Table.TableName & ".", al & ".")
                    'Dim tbl As SourceFragment = join.Table
                    'If tbl Is Nothing Then
                    '    'If join.Type IsNot Nothing Then
                    '    '    tbl = GetTables(join.Type)(0)
                    '    'Else
                    '    '    tbl = GetTables(GetTypeByEntityName(join.EntityName))(0)
                    '    'End If
                    '    tbl = mpe.GetTables(join.ObjectSource.GetRealType(mpe))(0)
                    'End If
                    'almgr.AddTable(tbl, CType(Nothing, EntityUnion))
                    almgr.Replace(mpe, Me, join.MakeSQLStmt(mpe, Nothing, Me, Nothing, filter_info, almgr, params, Nothing, sb), Nothing, columns)
                    'Else
                    '    sb = sb.Replace("{XXXXXX}", selSchema.GetFieldColumnMap("ID")._columnName)
                End If
            Next
            If columns.Length > 0 Then
                sb.Insert(ins_idx, columns.ToString)
            End If
            'sb = sb.Replace("{XXXXXX}", almgr.Aliases(selTable) & "." & selSchema.GetFieldColumnMap("ID")._columnName)
            AppendWhere(mpe, selectType, filter, almgr, sb, filter_info, params)
            'sb.Append(" order by rank ").Append(sort_type.ToString)
            If sort IsNot Nothing Then
                'sb.Append(",")
                AppendOrder(mpe, sort, almgr, sb, True, Nothing)
            Else
                sb.Append(" order by rank ").Append(sort_type.ToString)
            End If
            Return sb.ToString
        End Function

#End If

        Protected Function CreateFullJoinsClone(ByVal joins() As Worm.Criteria.Joins.QueryJoin) As Worm.Criteria.Joins.QueryJoin()
            If joins IsNot Nothing Then
                Dim l As New List(Of Worm.Criteria.Joins.QueryJoin)
                For Each j As Worm.Criteria.Joins.QueryJoin In joins
                    l.Add(j.Clone)
                Next
                Return l.ToArray
            Else
                Return Nothing
            End If
        End Function

        'Public Function GetDictionarySelect(ByVal mpe As ObjectMappingEngine, ByVal type As Type, ByVal level As Integer, _
        '    ByVal params As ParamMgr, ByVal filter As IFilter, ByVal joins() As Worm.Criteria.Joins.QueryJoin, ByVal filter_info As Object) As String

        '    Dim odic As ISupportAlphabet = TryCast(mpe.GetEntitySchema(type), ISupportAlphabet)
        '    If odic Is Nothing OrElse String.IsNullOrEmpty(odic.GetFirstDicField) Then
        '        Throw New InvalidOperationException(String.Format("Type {0} is not supports dictionary", type.Name))
        '    End If

        '    Dim sb As New StringBuilder
        '    Dim almgr As AliasMgr = AliasMgr.Create

        '    If String.IsNullOrEmpty(odic.GetSecondDicField) Then
        '        sb.Append(GetDicStmt(mpe, type, CType(odic, IEntitySchema), odic.GetFirstDicField, level, almgr, params, filter, joins, filter_info, True))
        '    Else
        '        Dim joins2() As Worm.Criteria.Joins.QueryJoin = CreateFullJoinsClone(joins)
        '        sb.Append("select name,sum(cnt) from (")
        '        sb.Append(GetDicStmt(mpe, type, CType(odic, IEntitySchema), odic.GetFirstDicField, level, almgr, params, filter, joins, filter_info, False))
        '        sb.Append(" union ")
        '        almgr = AliasMgr.Create
        '        sb.Append(GetDicStmt(mpe, type, CType(odic, IEntitySchema), odic.GetSecondDicField, level, almgr, params, filter, joins2, filter_info, False))
        '        sb.Append(") sd group by name order by name")
        '    End If

        '    Return sb.ToString
        'End Function

        'Public Function GetDictionarySelect(ByVal mpe As ObjectMappingEngine, ByVal type As Type, ByVal level As Integer, _
        '    ByVal params As ParamMgr, ByVal filter As IFilter, ByVal joins() As Worm.Criteria.Joins.QueryJoin, ByVal filter_info As Object, _
        '    ByVal firstField As String, ByVal secField As String) As String

        '    If String.IsNullOrEmpty(firstField) Then
        '        Throw New ArgumentNullException("firstField")
        '    End If

        '    Dim s As IEntitySchema = mpe.GetEntitySchema(type)

        '    Dim sb As New StringBuilder
        '    Dim almgr As AliasMgr = AliasMgr.Create

        '    If String.IsNullOrEmpty(secField) Then
        '        sb.Append(GetDicStmt(mpe, type, s, firstField, level, almgr, params, filter, joins, filter_info, True))
        '    Else
        '        Dim joins2() As Worm.Criteria.Joins.QueryJoin = CreateFullJoinsClone(joins)
        '        sb.Append("select name,sum(cnt) from (")
        '        sb.Append(GetDicStmt(mpe, type, s, firstField, level, almgr, params, filter, joins, filter_info, False))
        '        sb.Append(" union ")
        '        almgr = AliasMgr.Create
        '        sb.Append(GetDicStmt(mpe, type, s, secField, level, almgr, params, filter, joins, filter_info, False))
        '        sb.Append(") sd group by name order by name")
        '    End If

        '    Return sb.ToString
        'End Function

        'Protected Function GetDicStmt(ByVal mpe As ObjectMappingEngine, ByVal t As Type, ByVal schema As IEntitySchema, ByVal field As String, ByVal level As Integer, _
        '    ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal filter As IFilter, ByVal joins() As Worm.Criteria.Joins.QueryJoin, _
        '    ByVal filter_info As Object, ByVal appendOrder As Boolean) As String
        '    Dim sb As New StringBuilder
        '    Dim tbl As SourceFragment = mpe.GetTables(schema)(0)
        '    Dim al As String = "ZZZZZXXXXXXXX"
        '    'If Not almgr.Aliases.ContainsKey(tbl) Then
        '    '    al = almgr.AddTable(tbl)
        '    'Else
        '    '    al = almgr.Aliases(tbl)
        '    'End If

        '    Dim n As String = mpe.GetColumnNameByPropertyAlias(schema, field, False, Nothing)
        '    sb.Append("select left(")
        '    sb.Append(al).Append(Selector).Append(n)
        '    sb.Append(",").Append(level).Append(") name,count(*) cnt from ")
        '    AppendFrom(mpe, almgr, filter_info, mpe.GetTables(t), sb, params, _
        '               TryCast(schema, IMultiTableObjectSchema), t)
        '    If joins IsNot Nothing Then
        '        For i As Integer = 0 To joins.Length - 1
        '            Dim join As QueryJoin = CType(joins(i), QueryJoin)

        '            If Not QueryJoin.IsEmpty(join) Then
        '                'almgr.AddTable(join.Table, Nothing, params)
        '                join.MakeSQLStmt(mpe, Nothing, Me, Nothing, filter_info, almgr, params, Nothing, sb)
        '            End If
        '        Next
        '    End If

        '    AppendWhere(mpe, t, filter, almgr, sb, filter_info, params)

        '    sb.Append(" group by left(")
        '    sb.Append(al).Append(Selector).Append(n)
        '    sb.Append(",").Append(level).Append(")")

        '    If appendOrder Then
        '        sb.Append(" order by left(")
        '        sb.Append(al).Append(Selector).Append(n)
        '        sb.Append(",").Append(level).Append(")")
        '    End If

        '    sb.Replace(al, almgr.GetAlias(tbl, Nothing))

        '    Return sb.ToString
        'End Function



#End Region

        'Public Overrides Function CreateCriteria(ByVal os As ObjectSource) As Worm.Criteria.ICtor
        '    Return New Criteria.Ctor(Me, os)
        'End Function

        'Public Overloads Overrides Function CreateCriteria(ByVal os As ObjectSource, ByVal propertyAlias As String) As Worm.Criteria.CriteriaField
        '    Return Criteria.Ctor.Field(Me, os, propertyAlias)
        'End Function

        'Public Overloads Overrides Function CreateCriteria(ByVal table As Entities.Meta.SourceFragment) As Worm.Criteria.ICtor
        '    Return New Criteria.Ctor(Me, table)
        'End Function

        'Public Overloads Overrides Function CreateCriteria(ByVal table As Entities.Meta.SourceFragment, ByVal columnName As String) As Worm.Criteria.CriteriaColumn
        '    Return Criteria.Ctor.Column(Me, table, columnName)
        'End Function

        'Public Overrides Function CreateConditionCtor() As Condition.ConditionConstructor
        '    Return New Condition.ConditionConstructor
        'End Function

        'Public Overrides Function CreateCriteriaLink(ByVal con As Condition.ConditionConstructor) As Worm.Criteria.CriteriaLink
        '    Return New Criteria.CriteriaLink(Me, CType(con, Condition.ConditionConstructor))
        'End Function

#If Not ExcludeFindMethods Then
        Public Overrides Function CreateTopAspect(ByVal top As Integer) As Entities.Query.TopAspect
            Return New TopAspect(top)
        End Function

        Public Overrides Function CreateTopAspect(ByVal top As Integer, ByVal sort As SortExpression) As Entities.Query.TopAspect
            Return New TopAspect(top, sort)
        End Function

#End If

        Public Overrides Function GetTableName(ByVal t As Entities.Meta.SourceFragment, ByVal contextInfo As IDictionary) As String
            Dim db = String.Empty
            If contextInfo IsNot Nothing Then
                Dim cd = TryCast(contextInfo(CustomTableDelegateProperty), GetCustomTableNameDelegate)
                If cd IsNot Nothing Then
                    Return cd(Me, t, contextInfo)
                End If

                Dim qdb = contextInfo("query database")
                If qdb IsNot Nothing AndAlso Not String.IsNullOrEmpty(qdb.ToString) Then
                    db = qdb.ToString & Selector
                End If

                Dim u = contextInfo("query schema")
                If u IsNot Nothing AndAlso Not String.IsNullOrEmpty(u.ToString) Then
                    Return db & u.ToString & Selector & t.Name
                End If

            End If
            If Not String.IsNullOrEmpty(t.Schema) AndAlso UseSchema Then
                Return db & t.Schema & Selector & t.Name
            Else
                Return db & t.Name
            End If
        End Function

        'Public Overloads Overrides Function CreateCriteriaLink(ByVal con As Criteria.Conditions.Condition.ConditionConstructorBase) As Criteria.CriteriaLink

        'End Function


        'Public Overrides Function CreateCustom(ByVal format As String, ByVal value As Worm.Criteria.Values.IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation, ByVal ParamArray values() As Pair(Of Object, String)) As Worm.Criteria.Core.CustomFilter
        '    Return New CustomFilter(format, value, oper, values)
        'End Function

        'Public Overrides Function CreateSelectExpressionFormater() As Entities.ISelectExpressionFormater
        '    Return New SelectExpressionFormater(Me)
        'End Function



        Public Overrides Sub FormStmt(ByVal dbschema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, _
                                   ByVal contextInfo As IDictionary, ByVal paramMgr As ICreateParam, ByVal almgr As IPrepareTable, _
                                   ByVal sb As StringBuilder, ByVal type As Type, ByVal sourceFragment As SourceFragment, _
                                   ByVal joins() As Joins.QueryJoin, ByVal propertyAlias As String, ByVal filter As IFilter)
            If type Is Nothing Then
                sb.Append(SelectWithJoin(dbschema, Nothing, New SourceFragment() {sourceFragment}, _
                    almgr, paramMgr, joins, _
                    False, Nothing, Nothing, Nothing, Nothing, contextInfo))
            Else
                Dim arr As Generic.IList(Of EntityExpression) = Nothing
                If Not String.IsNullOrEmpty(propertyAlias) Then
                    arr = New Generic.List(Of EntityExpression)
                    arr.Add(New EntityExpression(propertyAlias, type))
                End If
                sb.Append(SelectWithJoin(dbschema, type, almgr, paramMgr, joins, _
                    arr IsNot Nothing, Nothing, Nothing, contextInfo, arr))
            End If

            AppendWhere(dbschema, type, filter, almgr, sb, contextInfo, paramMgr)
        End Sub


        Public Overrides ReadOnly Property FTSKey() As String
            Get
                Return "[key]"
            End Get
        End Property

        Public Overrides ReadOnly Property PlanHint() As String
            Get
                Return " option(use plan N'{0}')"
            End Get
        End Property

        Public Overrides Function FormatGroupBy(ByVal t As Expressions2.GroupExpression.SummaryValues, ByVal fields As String, ByVal custom As String) As String
            Select Case t
                Case Expressions2.GroupExpression.SummaryValues.None
                    Return "group by " & fields
                Case Expressions2.GroupExpression.SummaryValues.Cube
                    Return "group by " & fields & " with cube"
                Case Expressions2.GroupExpression.SummaryValues.Rollup
                    Return "group by " & fields & " with rollup"
                Case Else
                    Throw New NotSupportedException(t.ToString)
            End Select
        End Function

        Public Overrides Function FormatOrderBy(ByVal t As Expressions2.SortExpression.SortType, ByVal fields As String, ByVal collation As String) As String
            Dim sb As New StringBuilder

            sb.Append(fields)
            If Not String.IsNullOrEmpty(collation) Then
                sb.Append(" collate ").Append(collation)
            End If

            If t = Expressions2.SortExpression.SortType.Desc Then
                sb.Append(" desc")
            End If

            Return sb.ToString
        End Function

        Public Overrides Function FormatAggregate(ByVal t As Expressions2.AggregateExpression.AggregateFunction, ByVal fields As String, ByVal custom As String, ByVal distinct As Boolean) As String
            If distinct Then
                fields = " distinct " & fields
            End If
            Select Case t
                Case Expressions2.AggregateExpression.AggregateFunction.Max
                    Return "max(" & fields & ")"
                Case Expressions2.AggregateExpression.AggregateFunction.Min
                    Return "min(" & fields & ")"
                Case Expressions2.AggregateExpression.AggregateFunction.Average
                    Return "avg(" & fields & ")"
                Case Expressions2.AggregateExpression.AggregateFunction.Count
                    Return "count(" & fields & ")"
                Case Expressions2.AggregateExpression.AggregateFunction.BigCount
                    Return "count_big(" & fields & ")"
                Case Expressions2.AggregateExpression.AggregateFunction.Sum
                    Return "sum(" & fields & ")"
                Case Expressions2.AggregateExpression.AggregateFunction.StandardDeviation
                    Return "stdev(" & fields & ")"
                Case Expressions2.AggregateExpression.AggregateFunction.StandardDeviationOfPopulation
                    Return "stdevp(" & fields & ")"
                Case Expressions2.AggregateExpression.AggregateFunction.Variance
                    Return "var(" & fields & ")"
                Case Expressions2.AggregateExpression.AggregateFunction.VarianceOfPopulation
                    Return "varp(" & fields & ")"
                Case Else
                    Throw New NotImplementedException(t.ToString)
            End Select
        End Function

        Public Overrides Function GetLockCommand(pmgr As ICreateParam, name As String, Optional lockTimeout As Integer? = Nothing, Optional lockType As LockTypeEnum = LockTypeEnum.Exclusive) As Data.Common.DbCommand
            Throw New NotImplementedException
        End Function

        Public Overrides Function ReleaseLockCommand(pmgr As ICreateParam, name As String) As Data.Common.DbCommand
            Throw New NotImplementedException
        End Function

        Public Overrides Function TestLockError(v As Object) As Boolean
            Throw New NotImplementedException
        End Function

        Public Overrides Function RollbackSavepoint(name As String) As String
            Return String.Format("rollback tran {0}", name)
        End Function

        Public Overrides Function Savepoint(name As String) As String
            Return String.Format("save tran {0}", name)
        End Function
        Public Overrides ReadOnly Property IsSavepointsSupported As Boolean
            Get
                Return True
            End Get
        End Property
    End Class

End Namespace

