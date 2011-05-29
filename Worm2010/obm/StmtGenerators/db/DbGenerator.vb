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

    Public Class ParamMgr
        Implements ICreateParam

        Private _params As List(Of System.Data.Common.DbParameter)
        Private _stmt As SQLGenerator
        Private _prefix As String
        Private _named_params As Boolean

        Public Sub New(ByVal stmtGen As SQLGenerator, ByVal prefix As String)
            _stmt = stmtGen
            _params = New List(Of System.Data.Common.DbParameter)
            _prefix = prefix
            _named_params = stmtGen.ParamName("p", 1) <> stmtGen.ParamName("p", 2)
        End Sub

        ', ByVal mpe As ObjectMappingEngine
        Public Function AddParam(ByVal pname As String, ByVal value As Object) As String Implements ICreateParam.AddParam
            If NamedParams Then
                Dim p As System.Data.Common.DbParameter = GetParameter(pname)
                If p Is Nothing Then
                    Return CreateParam(value)
                Else
                    If p.Value Is Nothing OrElse p.Value.Equals(value) Then
                        Return pname
                        'ElseIf ObjectMappingEngine.IsEntityType(value.GetType, mpe) Then
                        '    Dim rpk As Object = Nothing
                        '    If GetType(IKeyEntity).IsAssignableFrom(value.GetType) Then
                        '        rpk = CType(value, IKeyEntity).Identifier
                        '    ElseIf GetType(ICachedEntity).IsAssignableFrom(value.GetType) Then
                        '        Dim pk() As PKDesc = CType(value, ICachedEntity).GetPKValues
                        '        If pk.Length > 1 Then
                        '            Throw New NotSupportedException
                        '        End If
                        '        rpk = pk(0).Value
                        '    Else
                        '        Dim pk As IList(Of EntityPropertyAttribute) = mpe.GetPrimaryKeys(value.GetType)
                        '    End If
                    Else
                        Return CreateParam(value)
                    End If
                End If
            Else
                Return CreateParam(value)
            End If
        End Function

        Public Function CreateParam(ByVal value As Object) As String Implements ICreateParam.CreateParam
            If _stmt Is Nothing Then
                Throw New InvalidOperationException("Object must be created")
            End If

            Dim pname As String = _stmt.ParamName(_prefix, _params.Count + 1)
            _params.Add(_stmt.CreateDBParameter(pname, value))
            Return pname
        End Function

        Public ReadOnly Property Params() As IList(Of System.Data.Common.DbParameter) Implements ICreateParam.Params
            Get
                Return _params
            End Get
        End Property

        Public Function GetParameter(ByVal name As String) As System.Data.Common.DbParameter Implements ICreateParam.GetParameter
            If Not String.IsNullOrEmpty(name) Then
                For Each p As System.Data.Common.DbParameter In _params
                    If p.ParameterName = name Then
                        Return p
                    End If
                Next
            End If
            Return Nothing
        End Function

        Public ReadOnly Property Prefix() As String
            Get
                Return _prefix
            End Get
            'Set(ByVal value As String)
            '    _prefix = value
            'End Set
        End Property

        'Public ReadOnly Property IsEmpty() As Boolean Implements ICreateParam.IsEmpty
        '    Get
        '        Return _params Is Nothing
        '    End Get
        'End Property

        Public ReadOnly Property NamedParams() As Boolean Implements ICreateParam.NamedParams
            Get
                Return _named_params
            End Get
        End Property

        Public Sub AppendParams(ByVal collection As System.Data.Common.DbParameterCollection)
            For Each p As System.Data.Common.DbParameter In _params
                collection.Add(CType(p, ICloneable).Clone)
            Next
        End Sub

        Public Sub AppendParams(ByVal collection As System.Data.Common.DbParameterCollection, ByVal start As Integer, ByVal count As Integer)
            For i As Integer = start To Math.Min(_params.Count, start + count) - 1
                Dim p As System.Data.Common.DbParameter = _params(i)
                collection.Add(CType(p, ICloneable).Clone)
            Next
        End Sub

        Public Sub Clear(ByVal preserve As Integer)
            If preserve > 0 Then
                _params.RemoveRange(preserve - 1, _params.Count - preserve)
            End If
        End Sub
    End Class

    Public Class SQLGenerator
        Inherits StmtGenerator

        Public Const QueryLength As Integer = 490
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

        Public Class InsertedTable
            Private _tbl As SourceFragment
            Private _f As List(Of ITemplateFilter)
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

        Public Overridable ReadOnly Property Name() As String
            Get
                Return "SQL Server 2000"
            End Get
        End Property

        Public Overridable ReadOnly Property LastError() As String
            Get
                Return "@@error"
            End Get
        End Property

        Public Overridable ReadOnly Property DefaultValue() As String
            Get
                Return "default"
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

        Public Overridable ReadOnly Property SupportIf() As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overridable ReadOnly Property EndLine() As String
            Get
                Return vbCrLf
            End Get
        End Property

        Protected Friend Overridable Function DeclareVariable(ByVal name As String, ByVal type As String) As String
            Return "declare " & name & " " & type
        End Function

        Public Overridable Function SupportMultiline() As Boolean
            Return True
        End Function

        Public Overrides Function TopStatement(ByVal top As Integer) As String
            Return "top " & top & " "
        End Function

        Protected Friend Overridable Overloads Function TopStatement(ByVal top As Integer, ByVal percent As Boolean, ByVal ties As Boolean) As String
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

        Protected Overridable Function DefaultValues() As String
            Return "default values"
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

        Public Overrides ReadOnly Property GetYear() As String
            Get
                Return "year({0})"
            End Get
        End Property

        Public Overrides ReadOnly Property Selector() As String
            Get
                Return "."
            End Get
        End Property
#End Region

#Region " data factory "
        Public Overridable Function CreateDBCommand(ByVal timeout As Integer) As System.Data.Common.DbCommand
            Dim cmd As New SqlCommand
            cmd.CommandTimeout = timeout
            Return cmd
        End Function

        Public Overridable Function CreateDBCommand() As System.Data.Common.DbCommand
            Dim cmd As New SqlCommand
            'cmd.CommandTimeout = XMedia.Framework.Configuration.ApplicationConfiguration.CommandTimeout \ 1000
            Return cmd
        End Function

        Public Overridable Function CreateDataAdapter() As System.Data.Common.DbDataAdapter
            Return New SqlDataAdapter
        End Function

        Public Overridable Function CreateDBParameter() As System.Data.Common.DbParameter
            Return New SqlParameter
        End Function

        Public Overridable Function CreateDBParameter(ByVal name As String, ByVal value As Object) As System.Data.Common.DbParameter
            Return New SqlParameter(name, value)
        End Function

        Public Overridable Function CreateCommandBuilder(ByVal da As System.Data.Common.DbDataAdapter) As System.Data.Common.DbCommandBuilder
            Return New SqlCommandBuilder(CType(da, SqlDataAdapter))
        End Function

        Public Overridable Function CreateConnection(ByVal connectionString As String) As System.Data.Common.DbConnection
            Return New SqlConnection(connectionString)
        End Function

#End Region

#Region " Statements "

        Public Function Insert(ByVal mpe As ObjectMappingEngine, ByVal obj As ICachedEntity, ByVal filterInfo As Object, _
            ByRef dbparams As ICollection(Of System.Data.Common.DbParameter), _
            ByRef selectedProperties As List(Of SelectExpression)) As String

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Using obj.LockEntity()
                Dim ins_cmd As New StringBuilder
                dbparams = Nothing
                If obj.ObjectState = ObjectState.Created Then
                    Dim inserted_tables As New Generic.Dictionary(Of SourceFragment, List(Of ITemplateFilter))
                    Dim selectedProps As New Generic.List(Of EntityExpression)
                    Dim keys As New List(Of MapField2Column)
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
                    Dim tbls() As SourceFragment = mpe.GetTables(es)

                    Dim pkTable As SourceFragment = mpe.GetPKTable(real_t, es)

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
                        If (att And Field2DbRelations.ReadOnly) <> Field2DbRelations.ReadOnly OrElse _
                         (att And Field2DbRelations.InsertDefault) = Field2DbRelations.InsertDefault Then
                            Dim tb As SourceFragment = mpe.GetPropertyTable(es, propertyAlias)

                            Dim f As EntityFilter = Nothing
                            Dim v As Object = mpe.ChangeValueType(es, propertyAlias, current)
                            If (att And Field2DbRelations.InsertDefault) = Field2DbRelations.InsertDefault AndAlso v Is DBNull.Value Then
                                If Not String.IsNullOrEmpty(DefaultValue) Then
                                    f = New dc.EntityFilter(real_t, propertyAlias, New LiteralValue(DefaultValue), FilterOperation.Equal)
                                Else
                                    Throw New ObjectMappingException("DefaultValue required for operation")
                                End If
                            ElseIf v Is DBNull.Value AndAlso pkTable IsNot tb AndAlso js IsNot Nothing Then
                                Dim j As QueryJoin = CType(mpe.GetJoins(js, pkTable, tb, filterInfo), QueryJoin)
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
                            keys.Add(m)
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
                            jn = CType(mpe.GetJoins(js, pkTable, join_table, filterInfo), QueryJoin)
                        End If
                        If Not QueryJoin.IsEmpty(jn) Then
                            Dim f As IFilter = jn.Condition
                            For Each m As MapField2Column In keys
                                f = JoinFilter.ChangeEntityJoinToLiteral(mpe, f, real_t, m.PropertyAlias, "@pk_" & m.SourceFieldExpression)

                                If f Is Nothing Then
                                    Throw New ObjectMappingException("Cannot change join")
                                End If
                            Next
                            If f Is Nothing Then
                                Throw New ObjectMappingException("Cannot change join")
                            End If
                            ins_tables(j).Filters.AddRange(CType(f.GetAllFilters, IEnumerable(Of ITemplateFilter)))
                        End If
                    Next
                    dbparams = FormInsert(mpe, ins_tables, ins_cmd, real_t, es, selectedProps, Nothing)

                    If True Then
                        selectedProperties = selectedProps.ConvertAll(Function(e) New SelectExpression(e))
                    End If
                End If

                Return ins_cmd.ToString
            End Using
        End Function

        Public Shared Function NeedJoin(ByVal schema As IEntitySchema) As Boolean
            Dim r As Boolean = False
            Dim j As IJoinBehavior = TryCast(schema, IJoinBehavior)
            If j IsNot Nothing Then
                r = j.AlwaysJoinMainTable
            End If
            Return r
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

        Protected Overridable Function FormatDBType(ByVal db As DBType) As String
            If db.Size > 0 Then
                Return db.Type & "(" & db.Size & ")"
            Else
                Return db.Type
            End If
        End Function

        Protected Overridable Overloads Function GetDBType(ByVal mpe As ObjectMappingEngine, ByVal type As Type, ByVal os As IEntitySchema, _
            ByVal pa As String, ByVal sf As String) As String
            Dim m As MapField2Column = os.FieldColumnMap(pa)
            Dim db As DBType = Nothing
            If String.IsNullOrEmpty(sf) Then
                db = m.SourceFields(0).DBType
            Else
                db = m.SourceFields.Find(Function(s) s.SourceFieldExpression = sf).DBType
            End If

            If db.IsEmpty Then
                Return DbTypeConvertor.ToSqlDbType(m.PropertyInfo.PropertyType).ToString
            Else
                Return FormatDBType(db)
            End If
        End Function

        Protected Overridable Function InsertOutput(ByVal table As String, ByVal syncInsertPK As IEnumerable(Of Pair(Of String, Pair(Of String))), ByVal notSyncInsertPK As List(Of Pair(Of String)), ByVal co As IChangeOutputOnInsert) As String
            Return String.Empty
        End Function

        Protected Overridable Function DeclareOutput(ByVal sb As StringBuilder, ByVal syncInsertPK As IEnumerable(Of Pair(Of String, Pair(Of String)))) As String
            Return Nothing
        End Function

        Protected Overridable Function FormInsert(ByVal mpe As ObjectMappingEngine, ByVal inserted_tables As List(Of InsertedTable), _
            ByVal ins_cmd As StringBuilder, ByVal type As Type, ByVal os As IEntitySchema, _
            ByVal selectedProperties As List(Of EntityExpression), _
            ByVal params As ICreateParam) As ICollection(Of System.Data.Common.DbParameter)

            If params Is Nothing Then
                params = New ParamMgr(Me, "p")
            End If

            Dim fromTable As String = Nothing

            If inserted_tables.Count > 0 Then
                Dim insertedPK As New List(Of Pair(Of String))
                Dim syncInsertPK As New List(Of Pair(Of String, Pair(Of String)))
                If selectedProperties IsNot Nothing Then
                    Dim col As Collections.IndexedCollection(Of String, MapField2Column) = os.FieldColumnMap
                    'Dim ie As ICollection = mpe.GetProperties(type, os)
                    'If ie.Count = 0 AndAlso GetType(AnonymousCachedEntity).IsAssignableFrom(type) Then
                    '    ie = col
                    'End If

                    For Each m As MapField2Column In col
                        Dim pi As Reflection.PropertyInfo = m.PropertyInfo
                        Dim att As Field2DbRelations = m.Attributes
                        If (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                            Dim clm As String = m.SourceFieldExpression 'mpe.GetColumnNameByPropertyAlias(os, m.PropertyAlias, False, Nothing)
                            Dim s As String = "@pk_" & clm
                            Dim dt As String = "int"
                            Dim propertyAlias As String = m.PropertyAlias
                            If pi IsNot Nothing Then 'AndAlso Not (pi.Name = "Identifier" AndAlso pi.DeclaringType.Name = GetType(OrmBaseT(Of )).Name) Then
                                dt = GetDBType(mpe, type, os, propertyAlias, Nothing)
                            End If
                            ins_cmd.Append(DeclareVariable(s, dt))
                            ins_cmd.Append(EndLine)
                            insertedPK.Add(New Pair(Of String)(propertyAlias, s))

                            If (att And Field2DbRelations.SyncInsert) = Field2DbRelations.SyncInsert Then
                                syncInsertPK.Add(New Pair(Of String, Pair(Of String))(propertyAlias, New Pair(Of String)(clm, dt)))
                            End If
                        End If
                        'End If
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

                    Dim notSyncInsertPK As New List(Of Pair(Of String))

                    If item.Filters Is Nothing OrElse item.Filters.Count = 0 Then
                        ins_cmd.Append("insert into ").Append(GetTableName(item.Table)).Append(" ").Append(DefaultValues)
                    Else
                        ins_cmd.Append("insert into ").Append(GetTableName(item.Table)).Append(" (")
                        Dim values_sb As New StringBuilder
                        values_sb.Append(" values(")
                        For Each f As ITemplateFilter In item.Filters
                            Dim ef As EntityFilter = TryCast(f, EntityFilter)
                            If ef IsNot Nothing Then
                                CType(ef, IEntityFilter).PrepareValue = False
                            End If
                            Dim p As Pair(Of String) = f.MakeSingleQueryStmt(mpe, Me, Nothing, params, item.Executor)
                            If ef IsNot Nothing Then
                                p = ef.MakeSingleQueryStmt(os, Me, mpe, Nothing, params, item.Executor)
                                Dim att As Field2DbRelations = os.FieldColumnMap(ef.Template.PropertyAlias).Attributes
                                If (att And Field2DbRelations.SyncInsert) = 0 AndAlso _
                                    (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                                    If mpe.GetPropertyTable(os, ef.Template.PropertyAlias) Is item.Table Then
                                        notSyncInsertPK.Add(New Pair(Of String)(ef.Template.PropertyAlias, p.Second))
                                    Else
                                        ins_cmd.Length = insStart
                                        GoTo l1
                                    End If
                                End If
                                'End If
                            End If
                            ins_cmd.Append(p.First).Append(",")
                            values_sb.Append(p.Second).Append(",")
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
                        For Each pk As Pair(Of String) In insertedPK
                            Dim propertyAlias As String = pk.First
                            Dim pr As Pair(Of String) = notSyncInsertPK.Find(Function(p As Pair(Of String)) p.First = propertyAlias)
                            If pr IsNot Nothing Then
                                ins_cmd.Append(", ").Append(pk.Second).Append(" = ").Append(pr.Second)
                            Else
                                Dim iv As IPKInsertValues = TryCast(os, IPKInsertValues)
                                If iv IsNot Nothing Then
                                    ins_cmd.Append(", ").Append(pk.Second).Append(" = ").Append(iv.GetValue(propertyAlias))
                                Else
                                    'Dim att As Field2DbRelations = GetAttributes(os, GetColumnByFieldName(type, propertyAlias, os))
                                    ins_cmd.Append(", ").Append(pk.Second).Append(" = ").Append(LastInsertID)
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
                            ins_cmd.Append(", @err = ").Append(LastError)
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
                        almgr.AddTable(pk_table, "t1")
                        selSb.Append(BinaryExpressionBase.CreateFromEnumerable(selectedProperties).MakeStatement(mpe, Nothing, Me, params, almgr, Nothing, MakeStatementMode.Select And MakeStatementMode.AddColumnAlias, New ExecutorCtx(type, os)))
                        selSb.Append(" from ").Append(GetTableName(pk_table)).Append(" t1 where ")
                        'almgr.Replace(mpe, Me, pk_table, Nothing, selSb)
                        ins_cmd.Append(selSb.ToString)

                        Dim cn As New PredicateLink
                        For Each pk As Pair(Of String) In insertedPK
                            Dim clm As String = os.FieldColumnMap(pk.First).SourceFieldExpression 'mpe.GetColumnNameByPropertyAlias(os, pk.First, False, Nothing)
                            cn = CType(cn.[and](pk_table, clm).eq(New LiteralValue(pk.Second)), PredicateLink)
                        Next
                        'ins_cmd.Append(GetColumnNameByFieldName(os, GetPrimaryKeys(type, os)(0).PropertyAlias)).Append(" = @id")
                        ins_cmd.Append(cn.Filter.MakeQueryStmt(mpe, Nothing, Me, Nothing, Nothing, almgr, Nothing))
                    End If
                End If
            End If

            Return params.Params
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

        Protected Function GetChangedFields(ByVal mpe As ObjectMappingEngine, ByVal obj As ICachedEntity, _
            ByVal oschema As IPropertyMap, ByVal tables As IDictionary(Of SourceFragment, TableUpdate), _
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
                    If (original IsNot Nothing AndAlso Not original.Equals(current)) OrElse _
                     (current IsNot Nothing AndAlso Not current.Equals(original)) OrElse CType(obj, _ICachedEntity).ForseUpdate(pa) Then

                        If original IsNot Nothing AndAlso current IsNot Nothing Then
                            Dim originalType As Type = original.GetType
                            Dim currentType As Type = current.GetType

                            If originalType IsNot currentType Then
                                If ObjectMappingEngine.IsEntityType(original.GetType, mpe) Then
                                    'Dim sch As IEntitySchema = mpe.GetEntitySchema(originalType, False)
                                    'If sch Is Nothing Then
                                    '    sch = mpe.GetPOCOEntitySchema(originalType)
                                    'End If
                                    'current = mpe.CreateObj(original.GetType, current, sch)
                                    'GoTo l1
                                    Throw New ApplicationException
                                ElseIf ObjectMappingEngine.IsEntityType(currentType, mpe) Then
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
                                AndAlso ObjectMappingEngine.IsEntityType(originalType, mpe) _
                                AndAlso ObjectMappingEngine.IsEntityType(currentType, mpe) Then
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

        Protected Sub GetUpdateConditions(ByVal mpe As ObjectMappingEngine, ByVal obj As ICachedEntity, ByVal oschema As IEntitySchema, _
            ByVal updated_tables As IDictionary(Of SourceFragment, TableUpdate), ByVal filterInfo As Object)

            Dim rt As Type = obj.GetType

            'Dim ie As ICollection = mpe.GetProperties(rt, TryCast(oschema, IEntitySchema))
            'If ie.Count = 0 AndAlso GetType(AnonymousCachedEntity).IsAssignableFrom(rt) Then
            '    ie = oschema.GetFieldColumnMap
            'End If

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
                If (att And Field2DbRelations.PK) = Field2DbRelations.PK OrElse _
                 (att And Field2DbRelations.RV) = Field2DbRelations.RV Then

                    Dim original As Object = ObjectMappingEngine.GetPropertyValue(obj, pa, TryCast(oschema, IEntitySchema), pi)

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
                                        de_table.Value._where4update.AddFilter( _
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
        End Sub

        Public Overridable Function Update(ByVal mpe As ObjectMappingEngine, ByVal obj As ICachedEntity, _
            ByVal filterInfo As Object, ByVal originalCopy As ICachedEntity, ByRef dbparams As IEnumerable(Of System.Data.Common.DbParameter), _
            ByRef selectedProperties As Generic.List(Of SelectExpression),
            ByRef updated_fields As IList(Of EntityFilter)) As String

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj parameter cannot be nothing")
            End If

            selectedProperties = Nothing
            updated_fields = Nothing

            Using obj.LockEntity()
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

                    GetUpdateConditions(mpe, obj, esch, updated_tables, filterInfo)

                    selectedProperties = syncUpdateProps.ConvertAll(Function(e) New SelectExpression(e))

                    If updated_tables.Count > 0 Then
                        'Dim sch As IOrmObjectSchema = GetObjectSchema(rt)
                        Dim pk_table As SourceFragment = Nothing
                        For Each m As MapField2Column In esch.FieldColumnMap
                            If m.IsPK Then
                                pk_table = m.Table 'mpe.GetPropertyTable(esch, c.PropertyAlias)
                                Exit For
                            End If
                        Next

                        Dim amgr As AliasMgr = AliasMgr.Create
                        Dim params As New ParamMgr(Me, "p")
                        Dim hasSyncUpdate As Boolean = False
                        Dim lastTbl As SourceFragment = Nothing, lastUT As TableUpdate = Nothing
                        For Each item As Generic.KeyValuePair(Of SourceFragment, TableUpdate) In updated_tables
                            Dim tbl As SourceFragment = item.Key
                            If upd_cmd.Length > 0 Then
                                upd_cmd.Append(EndLine)
                                If SupportIf() Then
                                    If HasUpdateColumnsForTable(syncUpdateProps, lastTbl, esch) OrElse Not lastTbl.Equals(pk_table) Then
                                        hasSyncUpdate = hasSyncUpdate OrElse HasUpdateColumnsForTable(syncUpdateProps, lastTbl, esch)
                                        Dim varName As String = "@" & lastTbl.Name.Replace(".", "").Trim("["c, "]"c) & "_rownum"
                                        upd_cmd.Append(DeclareVariable(varName, "int")).Append(EndLine)
                                        upd_cmd.Append("select ").Append(varName).Append(" = ").Append(RowCount)
                                        upd_cmd.Append(", @lastErr = ").Append(LastError).Append(EndLine)

                                        If Not lastTbl.Equals(pk_table) Then
                                            CorrectUpdateWithInsert(mpe, oschema, lastTbl, lastUT, upd_cmd, _
                                                obj, params, exec, varName)
                                        End If
                                    Else
                                        upd_cmd.Append("set @lastErr = ").Append(LastError).Append(EndLine)
                                    End If

                                    upd_cmd.Append("if @lastErr = 0 ")
                                End If
                            Else
                                upd_cmd.Append(DeclareVariable("@lastErr", "int")).Append(EndLine)
                            End If

                            lastTbl = tbl
                            lastUT = item.Value
                            Dim [alias] As String = amgr.AddTable(tbl, Nothing, params)

                            upd_cmd.Append("update ").Append([alias]).Append(" set ")
                            For Each f As EntityFilter In item.Value._updates
                                upd_cmd.Append(f.MakeQueryStmt(esch, Nothing, Me, exec, filterInfo, mpe, amgr, params, rt)).Append(",")
                            Next
                            upd_cmd.Length -= 1
                            upd_cmd.Append(" from ").Append(GetTableName(tbl)).Append(" ").Append([alias])
                            For Each join As QueryJoin In item.Value._joins
                                If Not amgr.ContainsKey(join.Table, Nothing) Then
                                    amgr.AddTable(join.Table, Nothing, params)
                                End If
                                join.MakeSQLStmt(mpe, Nothing, Me, Nothing, filterInfo, amgr, params, Nothing, upd_cmd)
                            Next
                            upd_cmd.Append(" where ")
                            Dim fl As IFilter = CType(item.Value._where4update.Condition, IFilter)
                            Dim ef As EntityFilter = TryCast(fl, EntityFilter)
                            If ef IsNot Nothing Then
                                upd_cmd.Append(ef.MakeQueryStmt(esch, Nothing, Me, exec, filterInfo, mpe, amgr, params, rt))
                            Else
                                upd_cmd.Append(fl.MakeQueryStmt(mpe, Nothing, Me, exec, filterInfo, amgr, params))
                            End If
                        Next

                        If SupportIf() Then
                            Dim varName As String = "@" & lastTbl.Name.Replace(".", "").Trim("["c, "]"c) & "_rownum"
                            Dim insSb As New StringBuilder
                            If Not lastTbl.Equals(pk_table) Then
                                CorrectUpdateWithInsert(mpe, oschema, lastTbl, lastUT, insSb, _
                                     obj, params, exec, varName)
                            End If

                            Dim hasCol As Boolean = HasUpdateColumnsForTable(syncUpdateProps, lastTbl, esch)
                            If hasCol OrElse insSb.Length > 0 Then
                                hasSyncUpdate = hasSyncUpdate OrElse hasCol
                                upd_cmd.Append(EndLine)
                                upd_cmd.Append(DeclareVariable(varName, "int")).Append(EndLine)
                                upd_cmd.Append("select ").Append(varName).Append(" = ").Append(RowCount)
                                If insSb.Length > 0 Then
                                    upd_cmd.Append(", @lastErr = ").Append(LastError)
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

                                    Dim varName As String = "@" & tbl.Name.Replace(".", "").Trim("["c, "]"c) & "_rownum"
                                    upd_cmd.Append(varName).Append(" and ")

                                Next
                                upd_cmd.Length -= 5
                                upd_cmd.Append(" > 0 ")
                            End If
                            Dim sel_sb As New StringBuilder
                            Dim newAlMgr As AliasMgr = AliasMgr.Create
                            sel_sb.Append(SelectWithJoin(mpe, rt, mpe.GetTables(esch), newAlMgr, params, _
                                Nothing, True, Nothing, Nothing, syncUpdateProps, esch, filterInfo))
                            Dim cn As New Condition.ConditionConstructor
                            For Each p As PKDesc In OrmManager.GetPKValues(obj, oschema)
                                Dim clm As String = esch.FieldColumnMap(p.PropertyAlias).SourceFieldExpression 'mpe.GetColumnNameByPropertyAlias(esch, p.PropertyAlias, False, Nothing)
                                cn.AddFilter(New dc.TableFilter(mpe.GetPropertyTable(esch, p.PropertyAlias), clm, New ScalarValue(p.Value), FilterOperation.Equal))
                            Next
                            AppendWhere(mpe, rt, esch, cn.Condition, newAlMgr, sel_sb, filterInfo, params)
                            upd_cmd.Append(sel_sb)
                            selectedProperties = syncUpdateProps.ConvertAll(Function(e) New SelectExpression(e))
                        End If

                        dbparams = params.Params
                    End If

                End If
                Return upd_cmd.ToString
            End Using
        End Function

        Protected Function HasUpdateColumns(ByVal sel_columns As Generic.IList(Of EntityPropertyAttribute), _
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

        Protected Function HasUpdateColumnsForTable(ByVal sel_columns As Generic.IList(Of EntityExpression), _
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

        Protected Overridable Function CorrectUpdateWithInsert(ByVal mpe As ObjectMappingEngine, ByVal oschema As IEntitySchema, ByVal table As SourceFragment, ByVal tableinfo As TableUpdate, _
            ByVal upd_cmd As StringBuilder, ByVal obj As ICachedEntity, ByVal params As ICreateParam, _
            ByVal exec As Query.IExecutionContext, ByVal rowCnt As String) As Boolean

            Dim dic As New List(Of InsertedTable)
            Dim l As New List(Of ITemplateFilter)
            For Each f As EntityFilter In tableinfo._updates
                l.Add(f)
            Next

            For Each f As ITemplateFilter In tableinfo._where4update.Condition.GetAllFilters
                l.Add(f)
            Next

            Dim ins As InsertedTable = New InsertedTable(table, l)
            ins.Executor = exec
            dic.Add(ins)
            Dim oldl As Integer = upd_cmd.Length
            upd_cmd.Append(EndLine).Append("if ").Append(rowCnt).Append(" = 0 and @lastErr = 0 ")
            Dim newl As Integer = upd_cmd.Length
            FormInsert(mpe, dic, upd_cmd, obj.GetType, oschema, Nothing, params)
            If newl = upd_cmd.Length Then
                upd_cmd.Length = oldl
                Return False
            Else
                Return True
            End If
        End Function

        Protected Sub GetDeletedConditions(ByVal mpe As ObjectMappingEngine, ByVal deleted_tables As IDictionary(Of SourceFragment, IFilter), ByVal filterInfo As Object, _
            ByVal type As Type, ByVal obj As ICachedEntity, ByVal oschema As IEntitySchema, ByVal relSchema As IMultiTableObjectSchema)
            'Dim oschema As IOrmObjectSchema = GetObjectSchema(type)
            Dim tables() As SourceFragment = mpe.GetTables(oschema)
            Dim pkTable As SourceFragment = mpe.GetPKTable(type, oschema)
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
                        Dim propertyAlias As String = m.PropertyAlias
                        If (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                            o.AddFilter(New dc.EntityFilter(type, propertyAlias, New LiteralValue("@id_" & propertyAlias), FilterOperation.Equal))
                        ElseIf (att And Field2DbRelations.RV) = Field2DbRelations.RV Then
                            Dim v As Object = ObjectMappingEngine.GetPropertyValue(obj, propertyAlias, oschema, m.PropertyInfo)
                            o.AddFilter((New dc.EntityFilter(type, propertyAlias, New ScalarValue(v), FilterOperation.Equal)))
                        End If
                        'End If
                    Next
                    deleted_tables(table) = CType(o.Condition, IFilter)
                ElseIf relSchema IsNot Nothing Then
                    Dim join As QueryJoin = CType(mpe.GetJoins(relSchema, tables(0), table, filterInfo), QueryJoin)
                    If Not QueryJoin.IsEmpty(join) Then
                        Dim f As IFilter = join.Condition

                        For Each m As MapField2Column In oschema.FieldColumnMap
                            If m.IsPK Then
                                f = JoinFilter.ChangeEntityJoinToLiteral(mpe, f, type, m.PropertyAlias, "@id_" & m.PropertyAlias)
                            End If
                        Next

                        If f Is Nothing Then
                            Throw New ObjectMappingException("Cannot replace join")
                        End If

                        o.AddFilter(f)
                        deleted_tables(table) = CType(o.Condition, IFilter)
                    End If
                End If
            Next
        End Sub

        Public Overridable Function Delete(ByVal mpe As ObjectMappingEngine, ByVal obj As ICachedEntity, ByRef dbparams As IEnumerable(Of System.Data.Common.DbParameter), _
            ByVal filterInfo As Object) As String
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj parameter cannot be nothing")
            End If

            Using obj.LockEntity()
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

                    For Each p As PKDesc In OrmManager.GetPKValues(obj, oschema)
                        Dim dbt As String = "int"
                        'Dim c As EntityPropertyAttribute = mpe.GetColumnByPropertyAlias(type, p.PropertyAlias, oschema)
                        'If c Is Nothing Then
                        '    c = New EntityPropertyAttribute()
                        '    c.PropertyAlias = p.PropertyAlias
                        'End If
                        dbt = GetDBType(mpe, type, oschema, p.PropertyAlias, Nothing)
                        del_cmd.Append(DeclareVariable("@id_" & p.PropertyAlias, dbt))
                        del_cmd.Append(EndLine)
                        del_cmd.Append("set @id_").Append(p.PropertyAlias).Append(" = ")
                        del_cmd.Append(params.CreateParam(mpe.ChangeValueType(oschema, p.PropertyAlias, p.Value))).Append(EndLine)
                    Next

                    Dim exec As New ExecutorCtx(type, relSchema)
                    GetDeletedConditions(mpe, deleted_tables, filterInfo, type, obj, relSchema, TryCast(relSchema, IMultiTableObjectSchema))

                    Dim pkFilter As IFilter = deleted_tables(relSchema.Table)
                    deleted_tables.Remove(relSchema.Table)

                    For Each de As KeyValuePair(Of SourceFragment, IFilter) In deleted_tables
                        del_cmd.Append("delete from ").Append(GetTableName(de.Key))
                        del_cmd.Append(" where ").Append(de.Value.MakeQueryStmt(mpe, Nothing, Me, exec, filterInfo, Nothing, params))
                        del_cmd.Append(EndLine)
                    Next
                    del_cmd.Append("delete from ").Append(GetTableName(relSchema.Table))
                    del_cmd.Append(" where ").Append(pkFilter.MakeQueryStmt(mpe, Nothing, Me, exec, filterInfo, Nothing, params))
                    del_cmd.Append(EndLine)

                    del_cmd.Length -= EndLine.Length
                    dbparams = params.Params
                End If

                Return del_cmd.ToString
            End Using
        End Function

        Public Overridable Function Delete(ByVal mpe As ObjectMappingEngine, ByVal t As Type, ByVal filter As IFilter, ByVal params As ParamMgr) As String
            If t Is Nothing Then
                Throw New ArgumentNullException("t parameter cannot be nothing")
            End If

            If filter Is Nothing Then
                Throw New ArgumentNullException("filter parameter cannot be nothing")
            End If

            Dim del_cmd As New StringBuilder
            del_cmd.Append("delete from ").Append(GetTableName(mpe.GetTables(t)(0)))
            del_cmd.Append(" where ").Append(filter.MakeQueryStmt(mpe, Nothing, Me, Nothing, Nothing, Nothing, params))

            Return del_cmd.ToString
        End Function

        Public Overridable Function SelectWithJoin(ByVal mpe As ObjectMappingEngine, ByVal original_type As Type, _
            ByVal almgr As IPrepareTable, ByVal params As ICreateParam, ByVal joins() As Worm.Criteria.Joins.QueryJoin, _
            ByVal wideLoad As Boolean, ByVal empty As Object, ByVal additionalColumns As String, _
            ByVal filterInfo As Object, ByVal selectedProperties As Generic.IList(Of EntityExpression)) As String

            If original_type Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "original_type")
            End If

            If almgr Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "almgr")
            End If

            Dim schema As IEntitySchema = mpe.GetEntitySchema(original_type)

            Return SelectWithJoin(mpe, original_type, mpe.GetTables(schema), almgr, params, joins, wideLoad, Nothing, additionalColumns, selectedProperties, schema, filterInfo)
        End Function

        Public Overridable Function SelectWithJoin(ByVal mpe As ObjectMappingEngine, ByVal original_type As Type, ByVal tables() As SourceFragment, _
            ByVal almgr As IPrepareTable, ByVal params As ICreateParam, ByVal joins() As Worm.Criteria.Joins.QueryJoin, _
            ByVal wideLoad As Boolean, ByVal empty As Object, ByVal additionalColumns As String, _
            ByVal selectedProperties As Generic.IList(Of EntityExpression), ByVal schema As IEntitySchema, ByVal filterInfo As Object) As String

            Dim selectcmd As New StringBuilder
            'Dim pmgr As ParamMgr = params 'New ParamMgr()

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

            Dim selSb As New StringBuilder
            selSb.Append("select ")

            If original_type IsNot Nothing Then
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
                    selSb.Append(BinaryExpressionBase.CreateFromEnumerable(selectedProperties).MakeStatement(mpe, Nothing, Me, params, almgr, filterInfo, MakeStatementMode.Select And MakeStatementMode.AddColumnAlias, New ExecutorCtx(original_type, schema)))
                Else
                    'mpe.GetPKList(original_type, mpe, schema, selSb, Nothing)
                    Dim l As New List(Of EntityExpression)
                    For Each mp As MapField2Column In schema.FieldColumnMap
                        If mp.IsPK Then
                            l.Add(New EntityExpression(mp.PropertyAlias, original_type))
                        End If
                    Next
                    selSb.Append(BinaryExpressionBase.CreateFromEnumerable(l).MakeStatement(mpe, Nothing, Me, params, almgr, filterInfo, MakeStatementMode.Select And MakeStatementMode.AddColumnAlias, New ExecutorCtx(original_type, schema)))
                End If
            Else
                selSb.Append("*")
            End If

            selSb.Append(" from ")
            Return selectcmd.Insert(0, selSb.ToString).ToString
        End Function

        Protected Delegate Function ConvertDelegate(Of T, T2)(ByVal source As T) As T2

        Protected Function ConvertAll(Of T, T2)(ByVal arr As IList(Of T), ByVal func As ConvertDelegate(Of T, T2)) As IList(Of T2)
            Dim l As New List(Of T2)
            For Each k As T In arr
                l.Add(func(k))
            Next
            Return l
        End Function

        Public Function [Select](ByVal mpe As ObjectMappingEngine, ByVal original_type As Type, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, _
            ByVal arr As Generic.IList(Of EntityExpression), _
             ByVal additionalColumns As String, ByVal filterInfo As Object) As String
            Return SelectWithJoin(mpe, original_type, almgr, params, Nothing, True, Nothing, additionalColumns, filterInfo, arr)
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
        Protected Friend Function AppendFrom(ByVal mpe As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal filterInfo As Object, _
            ByVal tables As SourceFragment(), ByVal selectcmd As StringBuilder, ByVal pname As ICreateParam, _
            ByVal sch As IMultiTableObjectSchema, ByVal t As Type) As StringBuilder
            'Dim sch As IOrmObjectSchema = GetObjectSchema(original_type)
            For i As Integer = 0 To tables.Length - 1
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
                    selectcmd.Append(GetTableName(tbl_real)).Append(" ").Append([alias])
                End If

                If sch IsNot Nothing Then
                    For j As Integer = i + 1 To tables.Length - 1
                        Dim join As QueryJoin = CType(mpe.GetJoins(sch, tbl, tables(j), filterInfo), QueryJoin)

                        If Not QueryJoin.IsEmpty(join) Then
                            If Not almgr.ContainsKey(tables(j), Nothing) Then
                                almgr.AddTable(tables(j), Nothing, pname)
                            End If
                            join.MakeSQLStmt(mpe, Nothing, Me, New ExecutorCtx(t, sch), filterInfo, almgr, pname, Nothing, selectcmd)
                        End If
                    Next
                End If
            Next

            Return selectcmd
        End Function

        Public Overridable Function AppendWhere(ByVal mpe As ObjectMappingEngine, ByVal t As Type, ByVal filter As Worm.Criteria.Core.IFilter, _
            ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal filter_info As Object, ByVal pmgr As ICreateParam) As Boolean
            Dim schema As IEntitySchema = Nothing
            If t IsNot Nothing Then
                schema = mpe.GetEntitySchema(t)
            End If

            Return AppendWhere(mpe, t, schema, filter, almgr, sb, filter_info, pmgr)
        End Function

        Public Overridable Function AppendWhere(ByVal mpe As ObjectMappingEngine, ByVal t As Type, ByVal schema As IEntitySchema, ByVal filter As Worm.Criteria.Core.IFilter, _
            ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal filter_info As Object, ByVal pmgr As ICreateParam, _
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
                    Dim f As IFilter = cs.GetContextFilter(filter_info)
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
                Dim s As String = f.MakeQueryStmt(mpe, Nothing, Me, New ExecutorCtx(t, schema), filter_info, almgr, pmgr)
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

        Public Function SaveM2M(ByVal mpe As ObjectMappingEngine, ByVal obj As ISinglePKEntity, _
            ByVal relation As M2MRelationDesc, ByVal entry As M2MRelation, _
            ByVal pmgr As ParamMgr) As String

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            If relation Is Nothing Then
                Throw New ArgumentNullException("relation")
            End If

            If entry Is Nothing Then
                Throw New ArgumentNullException("entry")
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
            If entry.HasDeleted Then
                sb.Append("delete ").Append(al).Append(" from ").Append(GetTableName(tbl)).Append(" ").Append(al)
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

                If entry.HasAdded Then
                    sb.Append("if ").Append(LastError).AppendLine(" = 0 begin")
                End If
            End If

            If entry.HasAdded Then
                For Each toAdd As ISinglePKEntity In entry.Added
                    If entry.HasDeleted Then
                        sb.Append(vbTab)
                    End If
                    sb.Append("if ").Append(LastError).Append(" = 0 ")
                    sb.Append("insert into ").Append(GetTableName(tbl)).Append("(")
                    sb.Append(param_relation.Column).Append(",").Append(relation.Column)
                    Dim consts As New List(Of String)
                    If relation.Constants IsNot Nothing Then
                        For Each f As ITemplateFilter In relation.Constants
                            sb.Append(",")
                            Dim p As Pair(Of String) = f.MakeSingleQueryStmt(mpe, Me, Nothing, pmgr, Nothing)
                            sb.Append(p.First)
                            consts.Add(p.Second)
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

                If entry.HasDeleted Then
                    sb.AppendLine("end")
                End If
            End If

            Return sb.ToString
        End Function

#End Region

        Public Function PrepareConcurrencyException(ByVal mpe As ObjectMappingEngine, ByVal obj As ICachedEntity) As OrmManagerException
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim sb As New StringBuilder
            Dim t As Type = obj.GetType
            sb.Append("Concurrency violation error during save object ")
            sb.Append(t.Name).Append(". Key values {")
            Dim cm As Boolean = False
            For Each m As MapField2Column In mpe.GetEntitySchema(t).FieldColumnMap
                Dim pi As Reflection.PropertyInfo = m.PropertyInfo
                If m.IsPK OrElse m.IsRowVersion Then

                    Dim s As String = m.SourceFieldExpression 'mpe.GetColumnNameByPropertyAlias(t, mpe, c.PropertyAlias, False, Nothing)
                    If cm Then
                        sb.Append(", ")
                    Else
                        cm = True
                    End If
                    sb.Append(s).Append(" = ")

                    Dim o As Object = pi.GetValue(obj, Nothing)

                    If GetType(Array).IsAssignableFrom(o.GetType) Then
                        sb.Append("{")
                        Dim y As Boolean = False
                        For Each item As Object In CType(o, Array) 'CType(o, Object())
                            If y Then
                                sb.Append(", ")
                            Else
                                y = True
                            End If
                            sb.Append(item)
                        Next
                        sb.Append("}")
                    Else
                        sb.Append(o.ToString)
                    End If
                End If
            Next
            sb.Append("}")

            Return New OrmManagerException(sb.ToString)
        End Function

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

        Public Overrides Function GetTableName(ByVal t As Entities.Meta.SourceFragment) As String
            If Not String.IsNullOrEmpty(t.Schema) Then
                Return t.Schema & Selector & t.Name
            Else
                Return t.Name
            End If
        End Function

        'Public Overloads Overrides Function CreateCriteriaLink(ByVal con As Criteria.Conditions.Condition.ConditionConstructorBase) As Criteria.CriteriaLink

        'End Function

        Public Overrides Function CreateExecutor() As Worm.Query.IExecutor
            Return New Worm.Query.Database.DbQueryExecutor
        End Function

        'Public Overrides Function CreateCustom(ByVal format As String, ByVal value As Worm.Criteria.Values.IParamFilterValue, ByVal oper As Worm.Criteria.FilterOperation, ByVal ParamArray values() As Pair(Of Object, String)) As Worm.Criteria.Core.CustomFilter
        '    Return New CustomFilter(format, value, oper, values)
        'End Function

        'Public Overrides Function CreateSelectExpressionFormater() As Entities.ISelectExpressionFormater
        '    Return New SelectExpressionFormater(Me)
        'End Function

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

        Public Overrides Sub FormStmt(ByVal dbschema As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, _
                                   ByVal filterInfo As Object, ByVal paramMgr As ICreateParam, ByVal almgr As IPrepareTable, _
                                   ByVal sb As StringBuilder, ByVal type As Type, ByVal sourceFragment As SourceFragment, _
                                   ByVal joins() As Joins.QueryJoin, ByVal propertyAlias As String, ByVal filter As IFilter)
            If type Is Nothing Then
                sb.Append(SelectWithJoin(dbschema, Nothing, New SourceFragment() {sourceFragment}, _
                    almgr, paramMgr, joins, _
                    False, Nothing, Nothing, Nothing, Nothing, filterInfo))
            Else
                Dim arr As Generic.IList(Of EntityExpression) = Nothing
                If Not String.IsNullOrEmpty(propertyAlias) Then
                    arr = New Generic.List(Of EntityExpression)
                    arr.Add(New EntityExpression(propertyAlias, type))
                End If
                sb.Append(SelectWithJoin(dbschema, type, almgr, paramMgr, joins, _
                    arr IsNot Nothing, Nothing, Nothing, filterInfo, arr))
            End If

            AppendWhere(dbschema, type, filter, almgr, sb, filterInfo, paramMgr)
        End Sub

        Public Overrides Function MakeQueryStatement(ByVal mpe As ObjectMappingEngine, ByVal fromClause As QueryCmd.FromClauseDef, ByVal filterInfo As Object, _
            ByVal query As Worm.Query.QueryCmd, ByVal params As ICreateParam, _
            ByVal almgr As IPrepareTable) As String

            Return Worm.Query.Database.DbQueryExecutor.MakeQueryStatement(mpe, filterInfo, Me, query, params, almgr)
        End Function

        Public Overrides ReadOnly Property SupportParams() As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property FTSKey() As String
            Get
                Return "[key]"
            End Get
        End Property

        Public Overrides ReadOnly Property Left() As String
            Get
                Return "left({0},{1})"
            End Get
        End Property

        Public Overrides Function Comment(ByVal s As String) As String
            Return "/*" & EndLine & s & EndLine & "*/" & EndLine
        End Function

        Public Overrides ReadOnly Property PlanHint() As String
            Get
                Return " option(use plan N'{0}')"
            End Get
        End Property

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
    End Class

    <Serializable()> _
    Public Class SQLGeneratorException
        Inherits System.Exception

        Public Sub New(ByVal message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(ByVal message As String, ByVal inner As Exception)
            MyBase.New(message, inner)
        End Sub

        Protected Sub New( _
            ByVal info As System.Runtime.Serialization.SerializationInfo, _
            ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
        End Sub
    End Class

End Namespace

