Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Criteria
Imports Worm.Criteria.Values
Imports dc = Worm.Criteria.Core
Imports Worm.Entities.Query
Imports Worm.Sorting
Imports Worm.Cache
Imports Worm.Criteria.Core
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Joins

Namespace Database

    Public Class ParamMgr
        Implements ICreateParam

        Private _params As List(Of System.Data.Common.DbParameter)
        Private _schema As SQLGenerator
        Private _prefix As String
        Private _named_params As Boolean

        Public Sub New(ByVal schema As SQLGenerator, ByVal prefix As String)
            _schema = schema
            _params = New List(Of System.Data.Common.DbParameter)
            _prefix = prefix
            _named_params = schema.ParamName("p", 1) <> schema.ParamName("p", 2)
        End Sub

        Public Function AddParam(ByVal pname As String, ByVal value As Object) As String Implements ICreateParam.AddParam
            If NamedParams Then
                Dim p As System.Data.Common.DbParameter = GetParameter(pname)
                If p Is Nothing Then
                    Return CreateParam(value)
                Else
                    If p.Value Is Nothing OrElse p.Value.Equals(value) Then
                        Return pname
                    Else
                        Return CreateParam(value)
                    End If
                End If
            Else
                Return CreateParam(value)
            End If
        End Function

        Public Function CreateParam(ByVal value As Object) As String Implements ICreateParam.CreateParam
            If _schema Is Nothing Then
                Throw New InvalidOperationException("Object must be created")
            End If

            Dim pname As String = _schema.ParamName(_prefix, _params.Count + 1)
            _params.Add(_schema.CreateDBParameter(pname, value))
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

        Public Overridable ReadOnly Property SupportRowNumber() As Boolean
            Get
                Return False
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

        Protected Overridable Function LastError() As String
            Return "@@error"
        End Function

        Protected Overridable Function DefaultValue() As String
            Return "default"
        End Function

        Protected Overridable Function LastInsertID() As String
            Return "scope_identity()"
        End Function

        Protected Overridable Function RowCount() As String
            Return "@@rowcount"
        End Function

        Protected Overridable Function SupportIf() As Boolean
            Return True
        End Function

        Public Overridable Function EndLine() As String
            Return vbCrLf
        End Function

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
            ByRef select_columns As Generic.List(Of ColumnAttribute)) As String

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Using obj.GetSyncRoot()
                Dim ins_cmd As New StringBuilder
                dbparams = Nothing
                If obj.ObjectState = ObjectState.Created Then
                    'Dim named_params As Boolean = ParamName("p", 1) <> ParamName("p", 2)
                    'Dim type As Type = obj.GetType
                    Dim inserted_tables As New Generic.Dictionary(Of SourceFragment, List(Of ITemplateFilter))
                    'Dim tables_val As New System.Collections.Specialized.ListDictionary
                    'Dim params_val As New System.Collections.Specialized.ListDictionary
                    'Dim _params As New ArrayList
                    Dim sel_columns As New Generic.List(Of ColumnAttribute)
                    Dim prim_key As ColumnAttribute = Nothing
                    'Dim prim_key_value As Object = Nothing
                    Dim real_t As Type = obj.GetType
                    Dim oschema As IEntitySchema = mpe.GetObjectSchema(real_t)
                    Dim unions() As String = ObjectMappingEngine.GetUnions(real_t)
                    'Dim uniontype As String = ""
                    If unions IsNot Nothing Then
                        Throw New NotImplementedException
                        'uniontype = GetUnionType(obj)
                    End If

                    Dim rs As IReadonlyObjectSchema = TryCast(oschema, IReadonlyObjectSchema)
                    Dim es As IEntitySchema = oschema
                    If rs IsNot Nothing Then
                        es = rs.GetEditableSchema
                    End If
                    Dim js As IOrmObjectSchema = TryCast(es, IOrmObjectSchema)
                    Dim tbls() As SourceFragment = mpe.GetTables(es)

                    Dim pkt As SourceFragment = tbls(0)

                    For Each de As DictionaryEntry In mpe.GetProperties(real_t, es)
                        Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                        Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                        If c IsNot Nothing Then
                            Dim current As Object = pi.GetValue(obj, Nothing)
                            Dim att As Field2DbRelations = mpe.GetAttributes(es, c)
                            If (att And Field2DbRelations.ReadOnly) <> Field2DbRelations.ReadOnly OrElse _
                             (att And Field2DbRelations.InsertDefault) = Field2DbRelations.InsertDefault Then
                                Dim tb As SourceFragment = mpe.GetFieldTable(es, c.PropertyAlias)
                                If unions IsNot Nothing Then
                                    Throw New NotImplementedException
                                    'tb = MapUnionType2Table(real_t, uniontype)
                                End If

                                Dim f As EntityFilter = Nothing
                                Dim v As Object = mpe.ChangeValueType(es, c, current)
                                If (att And Field2DbRelations.InsertDefault) = Field2DbRelations.InsertDefault AndAlso v Is DBNull.Value Then
                                    If Not String.IsNullOrEmpty(DefaultValue) Then
                                        f = New dc.EntityFilter(real_t, c.PropertyAlias, New LiteralValue(DefaultValue), FilterOperation.Equal)
                                    Else
                                        Throw New ObjectMappingException("DefaultValue required for operation")
                                    End If
                                ElseIf v Is DBNull.Value AndAlso pkt IsNot tb AndAlso js IsNot Nothing Then
                                    Dim j As QueryJoin = CType(mpe.GetJoins(js, pkt, tb, filterInfo), QueryJoin)
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
                                    f = New dc.EntityFilter(real_t, c.PropertyAlias, New ScalarValue(v), FilterOperation.Equal)
                                End If

                                If f IsNot Nothing Then
                                    If Not inserted_tables.ContainsKey(tb) Then
                                        inserted_tables.Add(tb, New List(Of ITemplateFilter))
                                    End If
                                    inserted_tables(tb).Add(f)
                                End If
                            End If

                            If (att And Field2DbRelations.SyncInsert) = Field2DbRelations.SyncInsert Then
                                sel_columns.Add(c)
                            End If

                            If (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                                prim_key = c
                                'prim_key_value = current
                            End If
                        End If
                    Next

                    If Not inserted_tables.ContainsKey(pkt) Then
                        inserted_tables.Add(pkt, Nothing)
                    End If

                    Dim ins_tables As List(Of Pair(Of SourceFragment, List(Of ITemplateFilter))) = Sort(Of SourceFragment, List(Of ITemplateFilter))(inserted_tables, tbls)

                    For j As Integer = 1 To ins_tables.Count - 1
                        Dim join_table As SourceFragment = ins_tables(j).First
                        Dim jn As QueryJoin = Nothing
                        If js IsNot Nothing Then
                            jn = CType(mpe.GetJoins(js, pkt, join_table, filterInfo), QueryJoin)
                        End If
                        If Not QueryJoin.IsEmpty(jn) Then
                            Dim f As IFilter = JoinFilter.ChangeEntityJoinToLiteral(mpe, jn.Condition, real_t, prim_key.PropertyAlias, "@pk_" & mpe.GetColumnNameByPropertyAlias(oschema, prim_key.PropertyAlias, False, Nothing, Nothing))

                            If f Is Nothing Then
                                Throw New ObjectMappingException("Cannot change join")
                            End If

                            'For Each fl As OrmFilter In f.GetAllFilters
                            '    inserted_tables(join_table).Add(fl)
                            'Next
                            ins_tables(j).Second.AddRange(CType(f.GetAllFilters, IEnumerable(Of ITemplateFilter)))
                        End If
                    Next
                    dbparams = FormInsert(mpe, ins_tables, ins_cmd, real_t, es, sel_columns, unions, Nothing)

                    select_columns = sel_columns
                End If

                Return ins_cmd.ToString
            End Using
        End Function

        Protected Overridable Function FormatDBType(ByVal db As DBType) As String
            If db.Size > 0 Then
                Return db.Type & "(" & db.Size & ")"
            Else
                Return db.Type
            End If
        End Function

        Protected Overridable Overloads Function GetDBType(ByVal mpe As ObjectMappingEngine, ByVal type As Type, ByVal os As IEntitySchema, _
                                                 ByVal c As ColumnAttribute, ByVal propType As Type) As String
            Dim db As DBType = mpe.GetDBType(type, os, c)
            If db.IsEmpty Then
                Return DbTypeConvertor.ToSqlDbType(propType).ToString
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

        Protected Overridable Function FormInsert(ByVal mpe As ObjectMappingEngine, ByVal inserted_tables As List(Of Pair(Of SourceFragment, List(Of ITemplateFilter))), _
            ByVal ins_cmd As StringBuilder, ByVal type As Type, ByVal os As IEntitySchema, _
            ByVal sel_columns As Generic.List(Of ColumnAttribute), _
            ByVal unions() As String, ByVal params As ICreateParam) As ICollection(Of System.Data.Common.DbParameter)

            If params Is Nothing Then
                params = New ParamMgr(Me, "p")
            End If

            Dim fromTable As String = Nothing

            If inserted_tables.Count > 0 Then
                Dim insertedPK As New List(Of Pair(Of String))
                Dim syncInsertPK As New List(Of Pair(Of String, Pair(Of String)))
                If sel_columns IsNot Nothing Then
                    For Each de As DictionaryEntry In mpe.GetProperties(type, os)
                        Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                        'If sel_columns.Contains(c) Then
                        Dim att As Field2DbRelations = mpe.GetAttributes(os, c)
                        If (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                            Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                            Dim clm As String = mpe.GetColumnNameByPropertyAlias(os, c.PropertyAlias, False, Nothing, Nothing)
                            Dim s As String = "@pk_" & clm
                            Dim dt As String = "int"
                            If Not (pi.Name = "Identifier" AndAlso pi.DeclaringType.Name = GetType(OrmBaseT(Of )).Name) Then
                                dt = GetDBType(mpe, type, os, c, pi.PropertyType)
                            End If
                            ins_cmd.Append(DeclareVariable(s, dt))
                            ins_cmd.Append(EndLine)
                            insertedPK.Add(New Pair(Of String)(c.PropertyAlias, s))

                            If (att And Field2DbRelations.SyncInsert) = Field2DbRelations.SyncInsert Then
                                syncInsertPK.Add(New Pair(Of String, Pair(Of String))(c.PropertyAlias, New Pair(Of String)(clm, dt)))
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
                For Each item As Pair(Of SourceFragment, List(Of ITemplateFilter)) In inserted_tables
                    If b Then
                        ins_cmd.Append(EndLine)
                        If SupportIf() Then
                            ins_cmd.Append("if @err = 0 ")
                        End If
                    Else
                        b = True
                    End If

                    Dim notSyncInsertPK As New List(Of Pair(Of String))

                    If item.Second Is Nothing OrElse item.Second.Count = 0 Then
                        ins_cmd.Append("insert into ").Append(GetTableName(item.First)).Append(" ").Append(DefaultValues)
                    Else
                        ins_cmd.Append("insert into ").Append(GetTableName(item.First)).Append(" (")
                        Dim values_sb As New StringBuilder
                        values_sb.Append(" values(")
                        For Each f As ITemplateFilter In item.Second
                            Dim ef As EntityFilter = TryCast(f, EntityFilter)
                            If ef IsNot Nothing Then
                                CType(ef, IEntityFilter).PrepareValue = False
                            End If
                            Dim p As Pair(Of String) = f.MakeSingleQueryStmt(mpe, Me, Nothing, params)
                            If ef IsNot Nothing Then
                                p = ef.MakeSingleQueryStmt(os, Me, mpe, Nothing, params)
                                'If ef.Template.PropertyAlias = OrmBaseT.PKName Then
                                Dim att As Field2DbRelations = mpe.GetAttributes(os, mpe.GetColumnByPropertyAlias(type, ef.Template.PropertyAlias, os))
                                If (att And Field2DbRelations.SyncInsert) = 0 AndAlso _
                                    (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                                    notSyncInsertPK.Add(New Pair(Of String)(ef.Template.PropertyAlias, p.Second))
                                End If
                                'End If
                            End If
                            ins_cmd.Append(p.First).Append(",")
                            values_sb.Append(p.Second).Append(",")
                        Next

                        ins_cmd.Length -= 1
                        values_sb.Length -= 1
                        ins_cmd.Append(") ")
                        If pk_table.Equals(item.First) Then
                            ins_cmd.Append(InsertOutput(fromTable, syncInsertPK, notSyncInsertPK, TryCast(os, IChangeOutputOnInsert)))
                        End If
                        ins_cmd.Append(values_sb.ToString).Append(")")
                    End If

                    If pk_table.Equals(item.First) AndAlso sel_columns IsNot Nothing Then
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
                Next

                If sel_columns IsNot Nothing AndAlso sel_columns.Count > 0 Then
                    'If unions Is Nothing Then
                    '    Dim rem_list As New ArrayList
                    '    For Each c As ColumnAttribute In sel_columns
                    '        If Not tables.Contains(GetFieldTable(type, c.FieldName)) Then rem_list.Add(c)
                    '    Next

                    '    For Each c As ColumnAttribute In rem_list
                    '        sel_columns.Remove(c)
                    '    Next
                    'End If

                    If sel_columns.Count > 0 Then
                        sel_columns.Sort()

                        ins_cmd.Append(EndLine)
                        If SupportIf() Then
                            ins_cmd.Append("if @rcount > 0 ")
                        End If

                        Dim selSb As New StringBuilder
                        selSb.Append("select ")
                        Dim com As Boolean = False
                        For Each c As ColumnAttribute In sel_columns
                            If com Then
                                selSb.Append(", ")
                            Else
                                com = True
                            End If
                            If unions IsNot Nothing Then
                                Throw New NotImplementedException
                                'ins_cmd.Append(GetColumnNameByFieldName(type, c.FieldName, pk_table))
                                'If (c.SyncBehavior And Field2DbRelations.PrimaryKey) = Field2DbRelations.PrimaryKey Then
                                '    ins_cmd.Append("+").Append(GetUnionScope(type, pk_table).First)
                                'End If
                            Else
                                selSb.Append(mpe.GetColumnNameByPropertyAlias(os, c.PropertyAlias, Nothing))
                            End If
                        Next

                        Dim almgr As AliasMgr = AliasMgr.Create
                        almgr.AddTable(pk_table, "t1")
                        selSb.Append(" from ").Append(GetTableName(pk_table)).Append(" t1 where ")
                        almgr.Replace(mpe, Me, pk_table, Nothing, selSb)
                        ins_cmd.Append(selSb.ToString)

                        If unions IsNot Nothing Then
                            Throw New NotImplementedException
                            'ins_cmd.Append(GetColumnNameByFieldName(type, "ID", pk_table)).Append(" = @id")
                        Else
                            Dim cn As New PredicateLink
                            For Each pk As Pair(Of String) In insertedPK
                                cn = CType(cn.[and](pk_table, mpe.GetColumnNameByPropertyAlias(os, pk.First, False, Nothing, Nothing)).eq(New LiteralValue(pk.Second)), PredicateLink)
                            Next
                            'ins_cmd.Append(GetColumnNameByFieldName(os, GetPrimaryKeys(type, os)(0).PropertyAlias)).Append(" = @id")
                            ins_cmd.Append(cn.Filter.MakeQueryStmt(mpe, Me, Nothing, almgr, Nothing, Nothing))
                        End If
                    End If
                End If
            End If

            Return params.Params
        End Function

        Protected Structure TableUpdate
            Public _table As SourceFragment
            Public _updates As IList(Of EntityFilter)
            Public _where4update As Condition.ConditionConstructor

            Public Sub New(ByVal table As SourceFragment)
                _table = table
                _updates = New List(Of EntityFilter)
                _where4update = New Condition.ConditionConstructor
            End Sub

        End Structure

        Protected Sub GetChangedFields(ByVal mpe As ObjectMappingEngine, ByVal obj As ICachedEntity, ByVal oschema As IPropertyMap, ByVal tables As IDictionary(Of SourceFragment, TableUpdate), _
            ByVal sel_columns As Generic.List(Of ColumnAttribute), ByVal unions As String())

            Dim rt As Type = obj.GetType
            Dim col As Collections.IndexedCollection(Of String, MapField2Column) = oschema.GetFieldColumnMap
            Dim originalCopy As ICachedEntity = obj.OriginalCopy
            For Each de As DictionaryEntry In mpe.GetProperties(rt, TryCast(oschema, IEntitySchema))
                'Dim c As ColumnAttribute = CType(Attribute.GetCustomAttribute(pi, GetType(ColumnAttribute), True), ColumnAttribute)
                Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                If c IsNot Nothing Then
                    Dim original As Object = pi.GetValue(originalCopy, Nothing)

                    'If (c.SyncBehavior And Field2DbRelations.PrimaryKey) <> Field2DbRelations.PrimaryKey AndAlso _
                    '    (c.SyncBehavior And Field2DbRelations.RowVersion) <> Field2DbRelations.RowVersion Then
                    Dim map As MapField2Column = Nothing
                    If col.TryGetValue(c.PropertyAlias, map) Then
                        Dim att As Field2DbRelations = map.GetAttributes(c)
                        If (att And Field2DbRelations.ReadOnly) <> Field2DbRelations.ReadOnly Then

                            Dim current As Object = pi.GetValue(obj, Nothing)
                            If (original IsNot Nothing AndAlso Not original.Equals(current)) OrElse _
                             (current IsNot Nothing AndAlso Not current.Equals(original)) OrElse CType(obj, _ICachedEntity).ForseUpdate(c) Then

                                If current IsNot Nothing AndAlso GetType(ICachedEntity).IsAssignableFrom(current.GetType) Then
                                    If CType(current, ICachedEntity).ObjectState = ObjectState.Created Then
                                        Throw New ObjectMappingException(obj.ObjName & "Cannot save object while it has reference to new object " & CType(current, ICachedEntity).ObjName)
                                    End If
                                End If

                                Dim fieldTable As SourceFragment = mpe.GetFieldTable(oschema, c.PropertyAlias)

                                If unions IsNot Nothing Then
                                    Throw New NotImplementedException
                                    'fieldTable = MapUnionType2Table(rt, uniontype)
                                End If

                                If Not tables.ContainsKey(fieldTable) Then
                                    tables.Add(fieldTable, New TableUpdate(fieldTable))
                                    'param_vals.Add(fieldTable, New ArrayList)
                                End If

                                Dim updates As IList(Of EntityFilter) = tables(fieldTable)._updates

                                updates.Add(New dc.EntityFilter(rt, c.PropertyAlias, New ScalarValue(current), FilterOperation.Equal))

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

                        If (att And Field2DbRelations.SyncUpdate) = Field2DbRelations.SyncUpdate Then
                            'If sbselect.Length = 0 Then
                            '    sbselect.Append("if @@rowcount > 0 select ")
                            'Else
                            '    sbselect.Append(", ")
                            'End If

                            'sbselect.Append(GetColumnNameByFieldName(type, c.FieldName))
                            sel_columns.Add(c)
                        End If
                    End If
                End If
            Next
        End Sub

        Protected Sub GetUpdateConditions(ByVal mpe As ObjectMappingEngine, ByVal obj As ICachedEntity, ByVal oschema As IEntitySchema, _
         ByVal updated_tables As IDictionary(Of SourceFragment, TableUpdate), ByVal unions() As String, ByVal filterInfo As Object)

            Dim rt As Type = obj.GetType

            For Each de As DictionaryEntry In mpe.GetProperties(rt, oschema)
                'Dim c As ColumnAttribute = CType(Attribute.GetCustomAttribute(pi, GetType(ColumnAttribute), True), ColumnAttribute)
                Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                If c IsNot Nothing Then
                    Dim att As Field2DbRelations = mpe.GetAttributes(oschema, c)
                    If (att And Field2DbRelations.PK) = Field2DbRelations.PK OrElse _
                     (att And Field2DbRelations.RV) = Field2DbRelations.RV Then

                        Dim original As Object = pi.GetValue(obj, Nothing)
                        'Dim original As Object = pi.GetValue(obj.ModifiedObject, Nothing)
                        'If obj.ModifiedObject.old_state = ObjectState.Created Then
                        'original = pi.GetValue(obj, Nothing)
                        'End If

                        Dim tb As SourceFragment = mpe.GetFieldTable(oschema, c.PropertyAlias)
                        If unions IsNot Nothing Then
                            Throw New NotImplementedException
                            'tb = MapUnionType2Table(rt, uniontype)
                        End If
                        'If pk_table Is Nothing Then pk_table = tb
                        'If Not tables_filter.Contains(tb) Then
                        '    tables_filter.Add(tb, New StringBuilder)
                        'End If
                        'Dim tb_sb As StringBuilder = CType(tables_filter(tb), StringBuilder)
                        'Dim pname As String = "pk"
                        'If (c.SyncBehavior And Field2DbRelations.RowVersion) = Field2DbRelations.RowVersion Then
                        '    pname = "rv"
                        'End If
                        'Dim add_param As Boolean = False


                        For Each de_table As Generic.KeyValuePair(Of SourceFragment, TableUpdate) In updated_tables 'In New Generic.List(Of Generic.KeyValuePair(Of String, TableUpdate))(CType(updated_tables, Generic.ICollection(Of Generic.KeyValuePair(Of String, TableUpdate))))
                            'Dim de_table As TableUpdate = updated_tables(tb)
                            If de_table.Key.Equals(tb) Then
                                'updated_tables(de_table.Key) = New TableUpdate(de_table.Value._table, de_table.Value._updates, de_table.Value._where4update.AddFilter(New OrmFilter(rt, c.FieldName, ChangeValueType(rt, c, original), FilterOperation.Equal)))
                                de_table.Value._where4update.AddFilter(New dc.EntityFilter(rt, c.PropertyAlias, New ScalarValue(original), FilterOperation.Equal))
                            Else
                                Dim joinableSchema As IOrmObjectSchema = TryCast(oschema, IOrmObjectSchema)
                                If joinableSchema IsNot Nothing Then
                                    Dim join As QueryJoin = CType(mpe.GetJoins(joinableSchema, tb, de_table.Key, filterInfo), QueryJoin)
                                    If Not QueryJoin.IsEmpty(join) Then
                                        Dim f As IFilter = JoinFilter.ChangeEntityJoinToParam(mpe, join.Condition, rt, c.PropertyAlias, New TypeWrap(Of Object)(original))

                                        If f Is Nothing Then
                                            Throw New ObjectMappingException("Cannot replace join")
                                        End If

                                        'updated_tables(de_table.Key) = New TableUpdate(de_table.Value._table, de_table.Value._updates, de_table.Value._where4update.AddFilter(f))
                                        de_table.Value._where4update.AddFilter(f)
                                    End If
                                End If
                            End If
                        Next
                    End If
                End If
            Next
        End Sub

        Public Overridable Function Update(ByVal mpe As ObjectMappingEngine, ByVal obj As ICachedEntity, ByVal filterInfo As Object, ByRef dbparams As IEnumerable(Of System.Data.Common.DbParameter), _
            ByRef select_columns As Generic.List(Of ColumnAttribute), ByRef updated_fields As IList(Of EntityFilter)) As String

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj parameter cannot be nothing")
            End If

            select_columns = Nothing
            updated_fields = Nothing

            Using obj.GetSyncRoot()
                Dim upd_cmd As New StringBuilder
                dbparams = Nothing
                If obj.OriginalCopy IsNot Nothing Then
                    'If obj.ObjectState = ObjectState.Modified Then
                    'If obj.OriginalCopy Is Nothing Then
                    '    Throw New ObjectStateException(obj.ObjName & "Object in state modified have to has an original copy")
                    'End If

                    Dim sel_columns As New Generic.List(Of ColumnAttribute)
                    Dim updated_tables As New Dictionary(Of SourceFragment, TableUpdate)
                    Dim rt As Type = obj.GetType

                    Dim unions() As String = ObjectMappingEngine.GetUnions(rt)
                    'Dim uniontype As String = ""
                    If unions IsNot Nothing Then
                        Throw New NotImplementedException
                        'uniontype = GetUnionType(obj)
                    End If

                    'Dim sb_updates As New StringBuilder
                    Dim oschema As IEntitySchema = mpe.GetObjectSchema(rt)
                    Dim esch As IEntitySchema = oschema
                    Dim ro As IReadonlyObjectSchema = TryCast(oschema, IReadonlyObjectSchema)
                    If ro IsNot Nothing Then
                        esch = ro.GetEditableSchema
                    End If

                    GetChangedFields(mpe, obj, esch, updated_tables, sel_columns, unions)

                    Dim l As New List(Of EntityFilter)
                    For Each tu As TableUpdate In updated_tables.Values
                        l.AddRange(tu._updates)
                    Next
                    updated_fields = l

                    GetUpdateConditions(mpe, obj, esch, updated_tables, unions, filterInfo)

                    select_columns = sel_columns

                    If updated_tables.Count > 0 Then
                        'Dim sch As IOrmObjectSchema = GetObjectSchema(rt)
                        Dim pk_table As SourceFragment = esch.Table
                        Dim amgr As AliasMgr = AliasMgr.Create
                        Dim params As New ParamMgr(Me, "p")

                        For Each item As Generic.KeyValuePair(Of SourceFragment, TableUpdate) In updated_tables
                            If upd_cmd.Length > 0 Then
                                upd_cmd.Append(EndLine)
                                If SupportIf() Then
                                    upd_cmd.Append("if ").Append(LastError).Append(" = 0 ")
                                End If
                            End If

                            Dim tbl As SourceFragment = item.Key
                            Dim [alias] As String = amgr.AddTable(tbl, Nothing, params)

                            upd_cmd.Append("update ").Append([alias]).Append(" set ")
                            For Each f As EntityFilter In item.Value._updates
                                upd_cmd.Append(f.MakeQueryStmt(esch, Me, filterInfo, mpe, amgr, params)).Append(",")
                            Next
                            upd_cmd.Length -= 1
                            upd_cmd.Append(" from ").Append(GetTableName(tbl)).Append(" ").Append([alias])
                            upd_cmd.Append(" where ")
                            Dim fl As IFilter = CType(item.Value._where4update.Condition, IFilter)
                            Dim ef As EntityFilter = TryCast(fl, EntityFilter)
                            If ef IsNot Nothing Then
                                upd_cmd.Append(ef.MakeQueryStmt(esch, Me, filterInfo, mpe, amgr, params, Nothing))
                            Else
                                upd_cmd.Append(fl.MakeQueryStmt(mpe, Me, filterInfo, amgr, params, Nothing))
                            End If
                            If Not item.Key.Equals(pk_table) Then
                                'Dim pcnt As Integer = 0
                                'If Not named_params Then pcnt = XMedia.Framework.Data.DBA.ExtractParamsCount(upd_cmd.ToString)
                                CorrectUpdateWithInsert(mpe, oschema, tbl, item.Value, upd_cmd, obj, params)
                                'FormInsert(,upd_cmd,
                            End If
                        Next

                        If CheckColumns(sel_columns, updated_tables, esch) Then
                            upd_cmd.Append(EndLine)
                            If SupportIf() Then
                                upd_cmd.Append("if ").Append(RowCount).Append(" > 0 ")
                            End If
                            Dim sel_sb As New StringBuilder
                            sel_sb.Append("select ")
                            Dim com As Boolean = False
                            For Each c As ColumnAttribute In sel_columns
                                If com Then
                                    sel_sb.Append(", ")
                                Else
                                    com = True
                                End If
                                If unions IsNot Nothing Then
                                    Throw New NotImplementedException
                                    'upd_cmd.Append(GetColumnNameByFieldName(Type, c.FieldName, pk_table))
                                    'If (c.SyncBehavior And Field2DbRelations.PrimaryKey) = Field2DbRelations.PrimaryKey Then
                                    '    upd_cmd.Append("+").Append(GetUnionScope(Type, pk_table).First)
                                    'End If
                                Else
                                    sel_sb.Append(mpe.GetColumnNameByPropertyAlias(esch, c.PropertyAlias, Nothing))
                                End If
                            Next

                            Dim [alias] As String = amgr.GetAlias(pk_table, Nothing)
                            'sel_sb = sel_sb.Replace(pk_table.TableName, [alias])
                            amgr.Replace(mpe, Me, pk_table, Nothing, sel_sb)
                            sel_sb.Append(" from ").Append(GetTableName(pk_table)).Append(" ").Append([alias]).Append(" where ")
                            'sel_sb.Append(updated_tables(pk_table)._where4update.Condition.MakeSQLStmt(Me, amgr.Aliases, params))
                            Dim cn As New Condition.ConditionConstructor
                            For Each p As PKDesc In obj.GetPKValues
                                Dim clm As String = mpe.GetColumnNameByPropertyAlias(esch, p.PropertyAlias, False, Nothing, Nothing)
                                cn.AddFilter(New dc.TableFilter(esch.Table, clm, New ScalarValue(p.Value), FilterOperation.Equal))
                            Next
                            Dim f As IFilter = cn.Condition
                            sel_sb.Append(f.MakeQueryStmt(mpe, Me, filterInfo, amgr, params, Nothing))

                            upd_cmd.Append(sel_sb)
                            select_columns = sel_columns
                        End If

                        dbparams = params.Params
                    End If

                End If
                Return upd_cmd.ToString
            End Using
        End Function

        Protected Function CheckColumns(ByVal sel_columns As Generic.IList(Of ColumnAttribute), _
            ByVal tables As IDictionary(Of SourceFragment, TableUpdate), ByVal esch As IEntitySchema) As Boolean

            If sel_columns.Count > 0 Then
                For Each c As ColumnAttribute In sel_columns
                    If Not tables.ContainsKey(esch.GetFieldColumnMap()(c.PropertyAlias)._tableName) Then
                        Return False
                    End If
                Next

                Return True
            End If

            Return False
        End Function

        Protected Overridable Sub CorrectUpdateWithInsert(ByVal mpe As ObjectMappingEngine, ByVal oschema As IEntitySchema, ByVal table As SourceFragment, ByVal tableinfo As TableUpdate, _
            ByVal upd_cmd As StringBuilder, ByVal obj As ICachedEntity, ByVal params As ICreateParam)

            Dim dic As New List(Of Pair(Of SourceFragment, List(Of ITemplateFilter)))
            Dim l As New List(Of ITemplateFilter)
            For Each f As EntityFilter In tableinfo._updates
                l.Add(f)
            Next

            For Each f As ITemplateFilter In tableinfo._where4update.Condition.GetAllFilters
                l.Add(f)
            Next

            dic.Add(New Pair(Of SourceFragment, List(Of ITemplateFilter))(table, l))
            upd_cmd.Append(EndLine).Append("if ").Append(RowCount).Append(" = 0 ")
            FormInsert(mpe, dic, upd_cmd, obj.GetType, oschema, Nothing, Nothing, params)
        End Sub

        Protected Sub GetDeletedConditions(ByVal mpe As ObjectMappingEngine, ByVal deleted_tables As IDictionary(Of SourceFragment, IFilter), ByVal filterInfo As Object, _
            ByVal type As Type, ByVal obj As ICachedEntity, ByVal oschema As IEntitySchema, ByVal relSchema As IMultiTableObjectSchema)
            'Dim oschema As IOrmObjectSchema = GetObjectSchema(type)
            Dim tables() As SourceFragment = mpe.GetTables(oschema)
            Dim pk_table As SourceFragment = tables(0)
            For j As Integer = 0 To tables.Length - 1
                Dim table As SourceFragment = tables(j)
                Dim o As New Condition.ConditionConstructor
                If table.Equals(pk_table) Then
                    For Each de As DictionaryEntry In mpe.GetProperties(type, oschema)
                        Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                        Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                        If c IsNot Nothing Then
                            Dim att As Field2DbRelations = oschema.GetFieldColumnMap()(c.PropertyAlias).GetAttributes(c) 'GetAttributes(type, c)
                            If (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                                o.AddFilter(New dc.EntityFilter(type, c.PropertyAlias, New LiteralValue("@id_" & c.PropertyAlias), FilterOperation.Equal))
                            ElseIf (att And Field2DbRelations.RV) = Field2DbRelations.RV Then
                                Dim v As Object = pi.GetValue(obj, Nothing)
                                o.AddFilter((New dc.EntityFilter(type, c.PropertyAlias, New ScalarValue(v), FilterOperation.Equal)))
                            End If
                        End If
                    Next
                    deleted_tables(table) = CType(o.Condition, IFilter)
                ElseIf relSchema IsNot Nothing Then
                    Dim join As QueryJoin = CType(mpe.GetJoins(relSchema, tables(0), table, filterInfo), QueryJoin)
                    If Not QueryJoin.IsEmpty(join) Then
                        Dim f As IFilter = join.Condition

                        For Each de As DictionaryEntry In mpe.GetProperties(type, oschema)
                            Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                            Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                            If c IsNot Nothing Then
                                Dim att As Field2DbRelations = oschema.GetFieldColumnMap()(c.PropertyAlias).GetAttributes(c) 'GetAttributes(type, c)
                                If (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                                    f = JoinFilter.ChangeEntityJoinToLiteral(mpe, f, type, c.PropertyAlias, "@id_" & c.PropertyAlias)
                                End If
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

            Using obj.GetSyncRoot()
                Dim del_cmd As New StringBuilder
                dbparams = Nothing

                If obj.ObjectState = ObjectState.Deleted Then
                    Dim type As Type = obj.GetType
                    Dim oschema As IEntitySchema = mpe.GetObjectSchema(type)
                    Dim relSchema As IEntitySchema = oschema
                    Dim ro As IReadonlyObjectSchema = TryCast(oschema, IReadonlyObjectSchema)
                    If ro IsNot Nothing Then
                        relSchema = ro.GetEditableSchema
                    End If

                    Dim params As New ParamMgr(Me, "p")
                    Dim deleted_tables As New Generic.Dictionary(Of SourceFragment, IFilter)

                    For Each p As PKDesc In obj.GetPKValues
                        del_cmd.Append(DeclareVariable("@id_" & p.PropertyAlias, "int")).Append(EndLine)
                        del_cmd.Append("set @id_").Append(p.PropertyAlias).Append(" = ").Append(params.CreateParam(p.Value)).Append(EndLine)
                    Next

                    GetDeletedConditions(mpe, deleted_tables, filterInfo, type, obj, relSchema, TryCast(relSchema, IMultiTableObjectSchema))

                    Dim pkFilter As IFilter = deleted_tables(relSchema.Table)
                    deleted_tables.Remove(relSchema.Table)

                    For Each de As KeyValuePair(Of SourceFragment, IFilter) In deleted_tables
                        del_cmd.Append("delete from ").Append(GetTableName(de.Key))
                        del_cmd.Append(" where ").Append(de.Value.MakeQueryStmt(mpe, Me, filterInfo, Nothing, params, Nothing))
                        del_cmd.Append(EndLine)
                    Next
                    del_cmd.Append("delete from ").Append(GetTableName(relSchema.Table))
                    del_cmd.Append(" where ").Append(pkFilter.MakeQueryStmt(mpe, Me, filterInfo, Nothing, params, Nothing))
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
            del_cmd.Append(" where ").Append(filter.MakeQueryStmt(mpe, Me, Nothing, Nothing, params, Nothing))

            Return del_cmd.ToString
        End Function

        Public Overridable Function SelectWithJoin(ByVal mpe As ObjectMappingEngine, ByVal original_type As Type, ByVal tables() As SourceFragment, _
            ByVal almgr As IPrepareTable, ByVal params As ICreateParam, ByVal joins() As Worm.Criteria.Joins.QueryJoin, _
            ByVal wideLoad As Boolean, ByVal aspects() As QueryAspect, ByVal additionalColumns As String, _
            ByVal arr As Generic.IList(Of ColumnAttribute), ByVal schema As IEntitySchema, ByVal filterInfo As Object) As String

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
                    Dim columns As String = mpe.GetSelectColumnList(original_type, mpe, arr, Nothing, schema, Nothing)
                    selectcmd.Append(columns)
                    If Not String.IsNullOrEmpty(additionalColumns) Then
                        selectcmd.Append(",").Append(additionalColumns)
                    End If
                Else
                    mpe.GetPKList(original_type, mpe, schema, selectcmd, Nothing, Nothing)
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
                AppendFrom(mpe, almgr, filterInfo, tables, selectcmd, params, TryCast(schema, IMultiTableObjectSchema))
                If joins IsNot Nothing Then
                    For i As Integer = 0 To joins.Length - 1
                        Dim join As QueryJoin = CType(joins(i), QueryJoin)

                        If Not QueryJoin.IsEmpty(join) Then
                            'almgr.AddTable(join.Table, CType(Nothing, ParamMgr))
                            selectcmd.Append(join.MakeSQLStmt(mpe, Me, filterInfo, almgr, params))
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
            ByVal filterInfo As Object, ByVal arr As Generic.IList(Of ColumnAttribute)) As String

            If original_type Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "original_type")
            End If

            If almgr Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "almgr")
            End If

            Dim schema As IEntitySchema = mpe.GetObjectSchema(original_type)

            Return SelectWithJoin(mpe, original_type, mpe.GetTables(schema), almgr, params, joins, wideLoad, aspects, _
                additionalColumns, arr, schema, filterInfo)
        End Function

        Public Overridable Function SelectDistinct(ByVal mpe As ObjectMappingEngine, ByVal original_type As Type, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal relation As M2MRelation, _
            ByVal wideLoad As Boolean, ByVal appendSecondTable As Boolean, ByVal filterInfo As Object, _
            ByVal arr As Generic.IList(Of ColumnAttribute)) As String

            If original_type Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "original_type")
            End If

            If almgr Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "almgr")
            End If

            Dim schema As IEntitySchema = mpe.GetObjectSchema(original_type)
            Dim selectcmd As New StringBuilder
            Dim tables() As SourceFragment = mpe.GetTables(schema)
            'Dim maintable As String = tables(0)
            selectcmd.Append("select distinct ")
            If wideLoad Then
                Dim columns As String = mpe.GetSelectColumnList(original_type, mpe, arr, Nothing, schema, Nothing)
                selectcmd.Append(columns)
            Else
                mpe.GetPKList(original_type, mpe, schema, selectcmd, Nothing, Nothing)
            End If
            selectcmd.Append(" from ")
            Dim unions() As String = ObjectMappingEngine.GetUnions(original_type)
            Dim pmgr As ParamMgr = params 'New ParamMgr()

            If unions Is Nothing Then
                AppendFrom(mpe, almgr, filterInfo, tables, selectcmd, pmgr, TryCast(schema, IMultiTableObjectSchema))

                Dim r2 As M2MRelation = mpe.GetM2MRelation(relation.Type, original_type, True)
                Dim tbl As SourceFragment = relation.Table
                If tbl Is Nothing Then
                    If relation.ConnectedType IsNot Nothing Then
                        tbl = mpe.GetTables(relation.ConnectedType)(0)
                    Else
                        Throw New InvalidOperationException(String.Format("Relation from type {0} to {1} has not table", original_type, relation.Type))
                    End If
                End If

                Dim f As New JoinFilter(tbl, r2.Column, original_type, mpe.GetPrimaryKeys(original_type, schema)(0).PropertyAlias, FilterOperation.Equal)
                Dim join As New QueryJoin(tbl, Joins.JoinType.Join, f)

                almgr.AddTable(tbl, CType(Nothing, ObjectSource))
                selectcmd.Append(join.MakeSQLStmt(mpe, Me, filterInfo, almgr, params))

                If appendSecondTable Then
                    Dim schema2 As IEntitySchema = mpe.GetObjectSchema(relation.Type)
                    AppendNativeTypeJoins(mpe, relation.Type, almgr, mpe.GetTables(relation.Type), selectcmd, params, tbl, relation.Column, True, filterInfo, schema2)
                End If
            Else
                Throw New NotImplementedException
            End If

            Return selectcmd.ToString
        End Function

        Public Function [Select](ByVal mpe As ObjectMappingEngine, ByVal original_type As Type, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, _
            ByVal arr As Generic.IList(Of ColumnAttribute), _
             ByVal additionalColumns As String, ByVal filterInfo As Object) As String
            Return SelectWithJoin(mpe, original_type, almgr, params, Nothing, True, Nothing, additionalColumns, filterInfo, arr)
        End Function

        Public Function [Select](ByVal mpe As ObjectMappingEngine, ByVal original_type As Type, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal queryAspect() As QueryAspect, _
            ByVal arr As Generic.IList(Of ColumnAttribute), _
             ByVal additionalColumns As String, ByVal filterInfo As Object) As String
            Return SelectWithJoin(mpe, original_type, almgr, params, Nothing, True, queryAspect, additionalColumns, filterInfo, arr)
        End Function

        Public Function SelectID(ByVal mpe As ObjectMappingEngine, ByVal original_type As Type, ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal filterInfo As Object) As String
            Return SelectWithJoin(mpe, original_type, almgr, params, Nothing, False, Nothing, Nothing, filterInfo, Nothing)
        End Function

        'Public Overridable Function [Select](ByVal original_type As Type, _
        '    ByVal almgr As AliasMgr, ByVal params As ParamMgr, _
        '    Optional ByVal arr As Generic.IList(Of ColumnAttribute) = Nothing, _
        '    Optional ByVal additionalColumns As String = Nothing) As String

        '    If original_type Is Nothing Then
        '        Throw New ArgumentNullException("parameter cannot be nothing", "original_type")
        '    End If

        '    If almgr.IsEmpty Then
        '        Throw New ArgumentNullException("parameter cannot be nothing", "almgr")
        '    End If

        '    Dim columns As String = GetSelectColumnList(original_type, arr)
        '    'Dim select_artist As String = "select" & original_type.ToString & columns
        '    'Dim cmd As String = CStr(map(select_artist))

        '    'If cmd Is Nothing Then
        '    'Using SyncHelper.AcquireDynamicLock(select_artist)
        '    'cmd = CStr(map(select_artist))
        '    'If cmd Is Nothing Then
        '    Dim selectcmd As New StringBuilder
        '    Dim tables() As SourceFragment = GetTables(original_type)
        '    'Dim maintable As String = tables(0)
        '    selectcmd.Append("select ").Append(columns)
        '    If Not String.IsNullOrEmpty(additionalColumns) Then
        '        selectcmd.Append(",").Append(additionalColumns)
        '    End If
        '    selectcmd.Append(" from ")
        '    Dim unions() As String = GetUnions(original_type)
        '    Dim pmgr As ParamMgr = params 'New ParamMgr()

        '    If unions Is Nothing Then
        '        selectcmd = AppendFrom(original_type, almgr, tables, selectcmd, pmgr)
        '    Else
        '        Throw New NotImplementedException
        '        'If tables.Length > 1 Then
        '        '    Throw New DBSchemaException("Unions doesnot supports joins")
        '        'End If

        '        'selectcmd.Replace(tables(0) & ".", "t1.")
        '        'selectcmd.Append("(").Append(vbCrLf)
        '        'selectcmd.Append("select ").Append(GetSelectColumnList(original_type))
        '        'selectcmd.Append(" from ").Append(tables(0)).Append(vbCrLf)
        '        'For Each u As String In unions
        '        '    selectcmd.Append("union all").Append(vbCrLf)
        '        '    selectcmd.Append("select ").Append(GetSelectColumnList(original_type, u))
        '        '    selectcmd.Append(" from ").Append(u).Append(vbCrLf)
        '        'Next
        '        'selectcmd.Append(") t1") '.Append(type.Name)
        '    End If
        '    Return selectcmd.ToString
        '    'cmd = selectcmd.ToString
        '    'map.Add(select_artist, cmd)
        '    'params = pmgr.Params
        '    'End If
        '    'End Using
        '    'End If

        '    'Return cmd
        'End Function

        'Public Overridable Function SelectID(ByVal original_type As Type, ByVal almgr As AliasMgr, ByVal params As ParamMgr) As String
        '    'Dim select_artist As String = "selectid" & original_type.ToString
        '    'Dim cmd As String = CStr(map(select_artist))

        '    'If cmd Is Nothing Then
        '    'Using SyncHelper.AcquireDynamicLock(select_artist)
        '    'cmd = CStr(map(select_artist))
        '    'If cmd Is Nothing Then
        '    Dim schema As IOrmObjectSchema = GetObjectSchema(original_type)
        '    Dim selectcmd As New StringBuilder
        '    Dim tables() As SourceFragment = GetTables(schema)
        '    'Dim maintable As SourceFragment = tables(0)
        '    selectcmd.Append("select ")
        '    GetPKList(schema, selectcmd)

        '    'Dim talias As String = almgr.AddTable(maintable)

        '    'selectcmd = selectcmd.Replace(maintable, talias)
        '    selectcmd.Append(" from ")
        '    Dim unions() As String = GetUnions(original_type)
        '    If unions Is Nothing Then
        '        'selectcmd.Append(maintable)
        '        'selectcmd.Append(" ").Append(talias)
        '        selectcmd = AppendFrom(original_type, almgr, tables, selectcmd, params, schema)
        '    Else
        '        Throw New NotImplementedException
        '        'If tables.Length > 1 Then
        '        '    Throw New DBSchemaException("Unions doesnot supports joins")
        '        'End If

        '        'selectcmd.Replace(tables(0) & ".", "t1.")
        '        'selectcmd.Append("(").Append(vbCrLf)
        '        'selectcmd.Append("select ")
        '        'GetPKList(original_type, selectcmd)
        '        'selectcmd.Append(" from ").Append(tables(0)).Append(vbCrLf)
        '        'For Each u As String In unions
        '        '    selectcmd.Append("union all").Append(vbCrLf)
        '        '    selectcmd.Append("select ")
        '        '    GetPKList(original_type, selectcmd, u)
        '        '    selectcmd.Append(" from ").Append(u).Append(vbCrLf)
        '        'Next
        '        'selectcmd.Append(") t1") '.Append(type.Name)
        '    End If
        '    Return selectcmd.ToString
        '    'cmd = selectcmd.ToString
        '    'map.Add(select_artist, cmd)
        '    'End If
        '    'End Using
        '    'End If
        '    'Return cmd
        'End Function

        'Public Overridable Function SelectTop(ByVal top As Integer, ByVal original_type As Type, _
        '    ByVal almgr As AliasMgr, ByVal params As ParamMgr, _
        '    Optional ByVal arr As Generic.ICollection(Of ColumnAttribute) = Nothing) As String

        '    If original_type Is Nothing Then
        '        Throw New ArgumentNullException("parameter cannot be nothing", "original_type")
        '    End If

        '    If almgr.IsEmpty Then
        '        Throw New ArgumentNullException("parameter cannot be nothing", "almgr")
        '    End If

        '    Dim columns As String = GetSelectColumnList(original_type, arr)
        '    Dim selectcmd As New StringBuilder
        '    Dim tables() As SourceFragment = GetTables(original_type)
        '    selectcmd.Append("select ").Append(TopStatement(top)).Append(columns)
        '    selectcmd.Append(" from ")
        '    Dim pmgr As ParamMgr = params 'New ParamMgr()
        '    selectcmd = AppendFrom(original_type, almgr, tables, selectcmd, pmgr)
        '    Return selectcmd.ToString
        'End Function

        'Public Overridable Function SelectIDTop(ByVal top As Integer, ByVal original_type As Type, ByVal almgr As AliasMgr, ByVal params As ParamMgr) As String
        '    If original_type Is Nothing Then
        '        Throw New ArgumentNullException("parameter cannot be nothing", "original_type")
        '    End If

        '    If almgr.IsEmpty Then
        '        Throw New ArgumentNullException("parameter cannot be nothing", "almgr")
        '    End If

        '    Dim selectcmd As New StringBuilder
        '    Dim tables() As SourceFragment = GetTables(original_type)
        '    'Dim maintable As SourceFragment = tables(0)
        '    selectcmd.Append("select ").Append(TopStatement(top))
        '    GetPKList(original_type, selectcmd)

        '    selectcmd.Append(" from ")
        '    selectcmd = AppendFrom(original_type, almgr, tables, selectcmd, params)
        '    Return selectcmd.ToString
        'End Function

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

            Dim pk_table As SourceFragment = schema.Table
            Dim sch As IMultiTableObjectSchema = TryCast(schema, IMultiTableObjectSchema)

            If Not appendMainTable Then
                If sch IsNot Nothing Then
                    For j As Integer = 1 To tables.Length - 1
                        Dim join As QueryJoin = CType(mpe.GetJoins(sch, pk_table, tables(j), filterInfo), QueryJoin)

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

                            selectcmd.Append(join.MakeSQLStmt(mpe, Me, filterInfo, almgr, pname))
                        End If
                    Next
                End If
            Else
                Dim tbl As SourceFragment = pk_table
                tbl = tbl.OnTableAdd(pname)
                Dim adal As Boolean
                If tbl Is Nothing Then
                    tbl = pk_table
                Else 'If dic IsNot Nothing Then
                    'dic.Add(pk_table, tbl)
                    adal = True
                End If
                Dim j As New QueryJoin(tbl, Joins.JoinType.Join, _
                    New JoinFilter(table, id, selectedType, mpe.GetPrimaryKeys(selectedType, schema)(0).PropertyAlias, FilterOperation.Equal))
                Dim al As String = almgr.AddTable(tbl, Nothing, pname)
                If adal Then
                    almgr.AddTable(pk_table, al)
                End If
                selectcmd.Append(j.MakeSQLStmt(mpe, Me, filterInfo, almgr, pname))
                If sch IsNot Nothing Then
                    For i As Integer = 1 To tables.Length - 1
                        Dim join As QueryJoin = CType(mpe.GetJoins(sch, pk_table, tables(i), filterInfo), QueryJoin)

                        If Not QueryJoin.IsEmpty(join) Then
                            almgr.AddTable(tables(i), Nothing, pname)
                            selectcmd.Append(join.MakeSQLStmt(mpe, Me, filterInfo, almgr, pname))
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
                    almgr.Replace(mpe, Me, tbl, Nothing, selectcmd)
                End If
                'End If
            Next
        End Sub

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
            ByVal sch As IMultiTableObjectSchema) As StringBuilder
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
                almgr.Replace(mpe, Me, tbl, Nothing, selectcmd)

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
                            selectcmd.Append(join.MakeSQLStmt(mpe, Me, filterInfo, almgr, pname))
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
                schema = mpe.GetObjectSchema(t)
            End If

            Return AppendWhere(mpe, schema, filter, almgr, sb, filter_info, pmgr, Nothing)
        End Function

        Public Overridable Function AppendWhere(ByVal mpe As ObjectMappingEngine, ByVal schema As IEntitySchema, ByVal filter As Worm.Criteria.Core.IFilter, _
            ByVal almgr As IPrepareTable, ByVal sb As StringBuilder, ByVal filter_info As Object, ByVal pmgr As ICreateParam, ByVal columns As List(Of String)) As Boolean

            Dim con As New Condition.ConditionConstructor
            con.AddFilter(filter)

            'If t IsNot Nothing Then
            '    Dim schema As IOrmObjectSchema = GetObjectSchema(t)
            '    con.AddFilter(schema.GetFilter(filter_info))
            'End If

            If schema IsNot Nothing Then
                Dim cs As IContextObjectSchema = TryCast(schema, IContextObjectSchema)
                If cs IsNot Nothing Then
                    con.AddFilter(cs.GetContextFilter(filter_info))
                End If
            End If

            If Not con.IsEmpty Then
                'Dim bf As Worm.Criteria.Core.IFilter = TryCast(con.Condition, Worm.Criteria.Core.IFilter)
                Dim f As IFilter = TryCast(con.Condition, IFilter)
                'If f IsNot Nothing Then
                Dim s As String = f.MakeQueryStmt(mpe, Me, filter_info, almgr, pmgr, columns)
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

        Public Sub AppendOrder(ByVal mpe As ObjectMappingEngine, ByVal sort As Sort, ByVal almgr As IPrepareTable, _
            ByVal sb As StringBuilder, ByVal appendOrder As Boolean, ByVal selList As ObjectModel.ReadOnlyCollection(Of SelectExpression), ByVal defaultTable As SourceFragment)
            If sort IsNot Nothing AndAlso Not sort.IsExternal Then 'AndAlso Not sort.IsAny
                If appendOrder Then
                    sb.Append(" order by ")
                End If
                Dim pos As Integer = sb.Length
                For Each ns As Sort In New Sort.Iterator(sort)
                    If ns.IsExternal Then
                        Throw New ObjectMappingException("External sort must be alone")
                    End If

                    'If ns.IsAny Then
                    '    Throw New DBSchemaException("Any sort must be alone")
                    'End If

                    'Dim s As IOrmSorting = TryCast(schema, IOrmSorting)
                    'If s Is Nothing Then

                    'End If
                    'Dim sort_field As String = schema.MapSort2FieldName(sort)
                    'If String.IsNullOrEmpty(sort_field) Then
                    '    Throw New ArgumentException("Sort " & sort & " is not supported", "sort")
                    'End If

                    Dim sb2 As New StringBuilder
                    If ns.IsCustom Then
                        'Dim s As String = ns.CustomSortExpression
                        'For Each map In cm
                        '    Dim pos2 As Integer = s.IndexOf("{" & map._fieldName & "}", StringComparison.InvariantCultureIgnoreCase)
                        '    If pos2 <> -1 Then
                        '        s = s.Replace("{" & map._fieldName & "}", almgr.Aliases(map._tableName) & "." & map._columnName)
                        '    End If
                        'Next
                        If ns.Values IsNot Nothing Then
                            sb2.Append(String.Format(ns.CustomSortExpression, ns.GetCustomExpressionValues(mpe, Me, almgr)))
                        Else
                            sb2.Append(ns.CustomSortExpression)
                        End If
                        If ns.Order = SortType.Desc Then
                            sb2.Append(" desc")
                        End If
                    Else
                        Dim st As Type = Nothing
                        If ns.ObjectSource IsNot Nothing Then
                            st = ns.ObjectSource.GetRealType(mpe)
                        End If

                        If st IsNot Nothing Then
                            Dim schema As IEntitySchema = CType(mpe.GetObjectSchema(st, False), IEntitySchema)

                            If schema Is Nothing Then GoTo l1

                            Dim map As MapField2Column = Nothing
                            Dim cm As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()

                            If cm.TryGetValue(ns.SortBy, map) Then
                                sb2.Append(almgr.GetAlias(map._tableName, ns.ObjectSource)).Append(Selector).Append(map._columnName)
                                If ns.Order = SortType.Desc Then
                                    sb2.Append(" desc")
                                End If
                            Else
                                Throw New ArgumentException(String.Format("Field {0} of type {1} is not defined", ns.SortBy, st))
                            End If
                        Else
l1:
                            Dim clm As String = ns.SortBy
                            Dim tbl As SourceFragment = ns.Table
                            If selList IsNot Nothing Then
                                For Each p As SelectExpression In selList
                                    If p.PropertyAlias = clm AndAlso Not String.IsNullOrEmpty(p.Column) Then
                                        If p.Table Is Nothing AndAlso tbl Is Nothing Then
                                            clm = p.Column
                                            tbl = defaultTable
                                            Exit For
                                        ElseIf tbl Is Nothing AndAlso defaultTable.RawName = p.Table.RawName Then
                                            clm = p.Column
                                            tbl = defaultTable
                                            Exit For
                                        ElseIf tbl IsNot Nothing AndAlso p.Table.RawName = tbl.RawName Then
                                            clm = p.Column
                                            Exit For
                                        End If
                                    End If
                                Next
                            ElseIf tbl Is Nothing Then
                                tbl = defaultTable
                            End If

                            sb2.Append(almgr.GetAlias(tbl, Nothing)).Append(Selector).Append(clm)
                            If ns.Order = SortType.Desc Then
                                sb2.Append(" desc")
                            End If
                        End If

                    End If
                    sb2.Append(",")
                    sb.Insert(pos, sb2.ToString)

                Next
                sb.Length -= 1
            End If
        End Sub

        'Public Overridable Function AppendWhere(ByVal t As Type, ByVal filters() As IOrmFilter, _
        '    ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal filter_info As Object, _
        '    ByVal pmgr As ParamMgr, ByVal queryLength As Integer, ByVal startfilter As Integer) As Integer

        '    Dim schema As IOrmObjectSchema = GetObjectSchema(t)
        '    Dim f As OrmFilter = schema.GetFilter(filter_info)
        '    If f IsNot Nothing Then
        '        sb.Append(" where ").Append(f.MakeSQLStmt(Me, almgr.Aliases, pmgr))
        '    Else
        '        sb.Append(" where ")
        '    End If
        '    Dim i As Integer = startfilter
        '    Do
        '        Dim fl As IOrmFilter = filters(i)
        '        sb.Append("(").Append(fl.MakeSQLStmt(Me, almgr.Aliases, pmgr)).Append(") and ")
        '        i += 1
        '    Loop While sb.Length < queryLength
        '    sb.Length -= 4
        '    Return i
        'End Function

        Public Function SelectM2M(ByVal mpe As ObjectMappingEngine, ByVal selectedType As Type, ByVal filteredType As Type, ByVal aspects() As QueryAspect, _
            ByVal appendMainTable As Boolean, ByVal appJoins As Boolean, ByVal filterInfo As Object, _
            ByVal pmgr As ParamMgr, ByVal almgr As AliasMgr, ByVal withLoad As Boolean, ByVal key As String) As String

            Dim schema As IEntitySchema = mpe.GetObjectSchema(selectedType)
            'Dim schema2 As IOrmObjectSchema = GetObjectSchema(filteredType)

            'column - select
            Dim selected_r As M2MRelation = Nothing
            'column - filter
            Dim filtered_r As M2MRelation = Nothing

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
                sb.Append(",").Append(mpe.GetSelectColumnList(selectedType, mpe, Nothing, Nothing, schema, Nothing))
                appendMainTable = True
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

        Public Shared Function NeedJoin(ByVal schema As IEntitySchema) As Boolean
            Dim r As Boolean = False
            Dim j As IJoinBehavior = TryCast(schema, IJoinBehavior)
            If j IsNot Nothing Then
                r = j.AlwaysJoinMainTable
            End If
            Return r
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
            Dim schema2 As IEntitySchema = mpe.GetObjectSchema(type)
            Dim cs As IContextObjectSchema = TryCast(schema2, IContextObjectSchema)

            Dim appendMainTable As Boolean = filter IsNot Nothing OrElse _
                (cs IsNot Nothing AndAlso cs.GetContextFilter(filter_info) IsNot Nothing) OrElse _
                appendMain OrElse SQLGenerator.NeedJoin(schema2)
            sb.Append(SelectM2M(mpe, type, t, aspects, appendMainTable, appJoins, filter_info, pmgr, almgr, withLoad, key))

            Dim selected_r As M2MRelation = Nothing
            Dim filtered_r As M2MRelation = Nothing

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
            ByVal fts As IFtsStringFormater, ByVal fields As ICollection(Of Pair(Of String, Type)), _
            ByVal sectionName As String, ByVal joins As ICollection(Of Worm.Criteria.Joins.QueryJoin), ByVal sort_type As SortType, _
            ByVal params As ParamMgr, ByVal filter_info As Object, ByVal queryFields As String(), _
            ByVal top As Integer, ByVal table As String, _
            ByVal sort As Sort, ByVal appendBySort As Boolean, ByVal filter As IFilter, ByVal contextKey As Object, _
            ByVal selSchema As IEntitySchema, ByVal searchSchema As IEntitySchema) As String

            'If searchType IsNot selectType AndAlso join.IsEmpty Then
            '    Throw New ArgumentException("Join is empty while type to load differs from type to search")
            'End If

            'If Not join.IsEmpty AndAlso searchType Is selectType Then
            '    Throw New ArgumentException("Join is not empty while type to load the same as type to search")
            'End If

            'Dim selSchema As IOrmObjectSchema = GetObjectSchema(selectType)
            'Dim searchSchema As IOrmObjectSchema = GetObjectSchema(searchType)
            Dim fs As IOrmFullTextSupport = TryCast(searchSchema, IOrmFullTextSupport)

            'Dim value As String = del(tokens, sectionName, fs, contextKey)
            Dim value As String = fts.GetFtsString(sectionName, contextKey, fs, searchType, table)
            If String.IsNullOrEmpty(value) Then
                Return Nothing
            End If

            Dim almgr As AliasMgr = AliasMgr.Create
            Dim ct As New SourceFragment(table)
            Dim [alias] As String = almgr.AddTable(ct, CType(Nothing, ObjectSource))
            Dim pname As String = params.CreateParam(value)
            'cols = New Generic.List(Of ColumnAttribute)
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
                    columns.Append(m._tableName.UniqueName(Nothing)).Append(mpe.Delimiter)
                    columns.Append(m._columnName).Append(",")
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
                sb.Append(tf.GetRealTable)
                appendMain = True
            End If
            sb.Append(",")
            If queryFields Is Nothing OrElse queryFields.Length = 0 Then
                sb.Append("*")
            Else
                sb.Append("(")
                For Each f As String In queryFields
                    Dim m As MapField2Column = searchSchema.GetFieldColumnMap(f)
                    sb.Append(m._columnName).Append(",")
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
            AppendNativeTypeJoins(mpe, searchType, almgr, mpe.GetTables(searchType), sb, params, ct, "[key]", appendMain, filter_info, searchSchema)
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
                    join.InjectJoinFilter(mpe, searchType, mpe.GetPrimaryKeys(searchType, searchSchema)(0).PropertyAlias, ct, "[key]")
                    'Dim al As String = almgr.AddTable(join.Table)
                    'columns = columns.Replace(join.Table.TableName & ".", al & ".")
                    Dim tbl As SourceFragment = join.Table
                    If tbl Is Nothing Then
                        'If join.Type IsNot Nothing Then
                        '    tbl = GetTables(join.Type)(0)
                        'Else
                        '    tbl = GetTables(GetTypeByEntityName(join.EntityName))(0)
                        'End If
                        tbl = mpe.GetTables(join.ObjectSource.GetRealType(mpe))(0)
                    End If
                    almgr.AddTable(tbl, CType(Nothing, ObjectSource))
                    almgr.Replace(mpe, Me, tbl, Nothing, columns)
                    sb.Append(join.MakeSQLStmt(mpe, Me, filter_info, almgr, params))
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
                AppendOrder(mpe, sort, almgr, sb, True, Nothing, Nothing)
            Else
                sb.Append(" order by rank ").Append(sort_type.ToString)
            End If
            Return sb.ToString
        End Function

        'Public Function MakeSearchFreetextStatements(ByVal t As Type, ByVal tokens() As String, ByVal fields() As String, _
        '    ByVal sectionName As String, ByVal join As OrmJoin, ByVal sort_type As SortType, _
        '    ByVal params As ParamMgr, ByVal filter_info As Object, ByVal queryFields As String()) As String

        '    Dim value As String = Configuration.SearchSection.GetValueForFreeText(t, tokens, sectionName)
        '    If String.IsNullOrEmpty(value) Then
        '        Return Nothing
        '    End If

        '    Dim almgr As AliasMgr = AliasMgr.Create
        '    Dim ft As New SourceFragment("freetexttable")
        '    Dim [alias] As String = almgr.AddTable(ft)
        '    Dim pname As String = params.CreateParam(value)
        '    'cols = New Generic.List(Of ColumnAttribute)
        '    Dim sb As New StringBuilder, columns As New StringBuilder
        '    Dim tbl As SourceFragment = GetTables(t)(0)
        '    Dim obj_schema As IOrmObjectSchema = GetObjectSchema(t)
        '    sb.Append("select [key] ").Append(obj_schema.GetFieldColumnMap("ID")._columnName)
        '    Dim appendMain As Boolean = False
        '    Dim main_table As SourceFragment = GetTables(t)(0)
        '    Dim ins_idx As Integer = sb.Length
        '    If fields IsNot Nothing AndAlso fields.Length > 0 Then
        '        appendMain = True
        '        For Each field As String In fields
        '            columns.Append(",").Append(main_table).Append(".")
        '            columns.Append(GetColumnNameByFieldNameInternal(t, field, False))
        '        Next
        '    End If
        '    sb.Append(" from freetexttable(")
        '    sb.Append(tbl).Append(",")
        '    If queryFields Is Nothing OrElse queryFields.Length = 0 Then
        '        sb.Append("*")
        '    Else
        '        sb.Append("(")
        '        For Each f As String In queryFields
        '            sb.Append(f)
        '        Next
        '        sb.Append(")")
        '    End If
        '    sb.Append(",")
        '    sb.Append(pname).Append(",500) ").Append([alias])
        '    If Not appendMain Then
        '        appendMain = obj_schema.GetFilter(filter_info) IsNot Nothing
        '    End If
        '    AppendJoins(t, almgr, GetTables(t), sb, params, ft, "[key]", appendMain)
        '    If appendMain Then
        '        Dim mainAlias As String = almgr.Aliases(main_table)
        '        sb.Insert(ins_idx, columns.Replace(main_table.TableName, mainAlias).ToString)
        '    End If

        '    If Not join.IsEmpty Then
        '        Dim r As New EntityFilter(t, "ID", New SimpleValue(Nothing), FilterOperation.Equal)
        '        Dim r2 As TableFilter = Nothing
        '        For Each f As IFilter In join.Condition.GetAllFilters
        '            Dim filt As ITemplateFilter = TryCast(f, ITemplateFilter)
        '            If filt IsNot Nothing AndAlso filt.Template.Equals(r.Template) Then
        '                r2 = New TableFilter(ft, "[key]", New SimpleValue(r.Value), FilterOperation.Equal)
        '                join.ReplaceFilter(r, r2)
        '                Exit For
        '            End If
        '        Next

        '        If r2 Is Nothing Then
        '            Throw New DBSchemaException("Invalid join")
        '        End If

        '        sb.Append(join.MakeSQLStmt(Me, almgr.Aliases, params))
        '    End If
        '    AppendWhere(t, Nothing, almgr, sb, filter_info, params)
        '    sb.Append(" order by rank ").Append(sort_type.ToString)
        '    Return sb.ToString
        'End Function

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

        Public Function GetDictionarySelect(ByVal mpe As ObjectMappingEngine, ByVal type As Type, ByVal level As Integer, _
            ByVal params As ParamMgr, ByVal filter As IFilter, ByVal joins() As Worm.Criteria.Joins.QueryJoin, ByVal filter_info As Object) As String

            Dim odic As IOrmDictionary = TryCast(mpe.GetObjectSchema(type), IOrmDictionary)
            If odic Is Nothing OrElse String.IsNullOrEmpty(odic.GetFirstDicField) Then
                Throw New InvalidOperationException(String.Format("Type {0} is not supports dictionary", type.Name))
            End If

            Dim sb As New StringBuilder
            Dim almgr As AliasMgr = AliasMgr.Create

            If String.IsNullOrEmpty(odic.GetSecondDicField) Then
                sb.Append(GetDicStmt(mpe, type, CType(odic, IEntitySchema), odic.GetFirstDicField, level, almgr, params, filter, joins, filter_info, True))
            Else
                Dim joins2() As Worm.Criteria.Joins.QueryJoin = CreateFullJoinsClone(joins)
                sb.Append("select name,sum(cnt) from (")
                sb.Append(GetDicStmt(mpe, type, CType(odic, IEntitySchema), odic.GetFirstDicField, level, almgr, params, filter, joins, filter_info, False))
                sb.Append(" union ")
                almgr = AliasMgr.Create
                sb.Append(GetDicStmt(mpe, type, CType(odic, IEntitySchema), odic.GetSecondDicField, level, almgr, params, filter, joins2, filter_info, False))
                sb.Append(") sd group by name order by name")
            End If

            Return sb.ToString
        End Function

        Public Function GetDictionarySelect(ByVal mpe As ObjectMappingEngine, ByVal type As Type, ByVal level As Integer, _
            ByVal params As ParamMgr, ByVal filter As IFilter, ByVal joins() As Worm.Criteria.Joins.QueryJoin, ByVal filter_info As Object, _
            ByVal firstField As String, ByVal secField As String) As String

            If String.IsNullOrEmpty(firstField) Then
                Throw New ArgumentNullException("firstField")
            End If

            Dim s As IEntitySchema = mpe.GetObjectSchema(type)

            Dim sb As New StringBuilder
            Dim almgr As AliasMgr = AliasMgr.Create

            If String.IsNullOrEmpty(secField) Then
                sb.Append(GetDicStmt(mpe, type, s, firstField, level, almgr, params, filter, joins, filter_info, True))
            Else
                Dim joins2() As Worm.Criteria.Joins.QueryJoin = CreateFullJoinsClone(joins)
                sb.Append("select name,sum(cnt) from (")
                sb.Append(GetDicStmt(mpe, type, s, firstField, level, almgr, params, filter, joins, filter_info, False))
                sb.Append(" union ")
                almgr = AliasMgr.Create
                sb.Append(GetDicStmt(mpe, type, s, secField, level, almgr, params, filter, joins, filter_info, False))
                sb.Append(") sd group by name order by name")
            End If

            Return sb.ToString
        End Function

        Protected Function GetDicStmt(ByVal mpe As ObjectMappingEngine, ByVal t As Type, ByVal schema As IEntitySchema, ByVal field As String, ByVal level As Integer, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal filter As IFilter, ByVal joins() As Worm.Criteria.Joins.QueryJoin, _
            ByVal filter_info As Object, ByVal appendOrder As Boolean) As String
            Dim sb As New StringBuilder
            Dim tbl As SourceFragment = mpe.GetTables(schema)(0)
            Dim al As String = "ZZZZZXXXXXXXX"
            'If Not almgr.Aliases.ContainsKey(tbl) Then
            '    al = almgr.AddTable(tbl)
            'Else
            '    al = almgr.Aliases(tbl)
            'End If

            Dim n As String = mpe.GetColumnNameByPropertyAlias(schema, field, False, Nothing, Nothing)
            sb.Append("select left(")
            sb.Append(al).Append(Selector).Append(n)
            sb.Append(",").Append(level).Append(") name,count(*) cnt from ")
            AppendFrom(mpe, almgr, filter_info, mpe.GetTables(t), sb, params, TryCast(schema, IMultiTableObjectSchema))
            If joins IsNot Nothing Then
                For i As Integer = 0 To joins.Length - 1
                    Dim join As QueryJoin = CType(joins(i), QueryJoin)

                    If Not QueryJoin.IsEmpty(join) Then
                        almgr.AddTable(join.Table, Nothing, params)
                        sb.Append(join.MakeSQLStmt(mpe, Me, filter_info, almgr, params))
                    End If
                Next
            End If

            AppendWhere(mpe, t, filter, almgr, sb, filter_info, params)

            sb.Append(" group by left(")
            sb.Append(al).Append(Selector).Append(n)
            sb.Append(",").Append(level).Append(")")

            If appendOrder Then
                sb.Append(" order by left(")
                sb.Append(al).Append(Selector).Append(n)
                sb.Append(",").Append(level).Append(")")
            End If

            sb.Replace(al, almgr.GetAlias(tbl, Nothing))

            Return sb.ToString
        End Function

        Public Function SaveM2M(ByVal mpe As ObjectMappingEngine, ByVal obj As IKeyEntity, ByVal relation As M2MRelation, ByVal entry As EditableListBase, _
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

            Dim sb As New StringBuilder
            Dim tbl As SourceFragment = relation.Table
            Dim param_relation As M2MRelation = mpe.GetRevM2MRelation(obj.GetType, relation.Type, Not relation.non_direct)

            If param_relation Is Nothing Then
                Throw New ArgumentException("Invalid relation")
            End If

            Dim pk As String = Nothing
            If entry.HasDeleted Then
                sb.Append("delete from ").Append(GetTableName(tbl))
                sb.Append(" where ").Append(param_relation.Column).Append(" = ")
                pk = pmgr.AddParam(pk, obj.Identifier)
                sb.Append(pk).Append(" and ").Append(relation.Column).Append(" in(")
                For Each toDel As IKeyEntity In entry.Deleted
                    sb.Append(toDel.Identifier).Append(",")
                Next
                sb.Length -= 1
                sb.AppendLine(")")
                If entry.HasAdded Then
                    sb.Append("if ").Append(LastError).AppendLine(" = 0 begin")
                End If
            End If

            If entry.HasAdded Then
                For Each toAdd As IKeyEntity In entry.Added
                    If entry.HasDeleted Then
                        sb.Append(vbTab)
                    End If
                    sb.Append("if ").Append(LastError).Append(" = 0 ")
                    sb.Append("insert into ").Append(GetTableName(tbl)).Append("(")
                    sb.Append(param_relation.Column).Append(",").Append(relation.Column).Append(") values(")
                    pk = pmgr.AddParam(pk, obj.Identifier)
                    sb.Append(pk).Append(",").Append(toAdd.Identifier).AppendLine(")")
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
            For Each de As DictionaryEntry In mpe.GetProperties(t)
                Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                If c IsNot Nothing AndAlso _
                    ((mpe.GetAttributes(t, c) And Field2DbRelations.PK) = Field2DbRelations.PK OrElse _
                    (mpe.GetAttributes(t, c) And Field2DbRelations.RV) = Field2DbRelations.RV) Then

                    Dim s As String = mpe.GetColumnNameByPropertyAlias(t, mpe, c.PropertyAlias, False, Nothing)
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

        Public Overrides Function CreateTopAspect(ByVal top As Integer) As Entities.Query.TopAspect
            Return New TopAspect(top)
        End Function

        Public Overrides Function CreateTopAspect(ByVal top As Integer, ByVal sort As Sort) As Entities.Query.TopAspect
            Return New TopAspect(top, sort)
        End Function

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

        Public Overrides Function CreateSelectExpressionFormater() As Entities.ISelectExpressionFormater
            Return New SelectExpressionFormater(Me)
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

        Public Overrides Sub FormStmt(ByVal dbschema As ObjectMappingEngine, _
                                   ByVal filterInfo As Object, ByVal paramMgr As ICreateParam, ByVal almgr As IPrepareTable, _
                                   ByVal sb As StringBuilder, ByVal _t As Type, ByVal _tbl As SourceFragment, _
                                   ByVal _joins() As Joins.QueryJoin, ByVal _field As String, ByVal _f As IFilter)
            If _t Is Nothing Then
                sb.Append(SelectWithJoin(dbschema, Nothing, New SourceFragment() {_tbl}, _
                    almgr, paramMgr, _joins, _
                    False, Nothing, Nothing, Nothing, Nothing, filterInfo))
            Else
                Dim arr As Generic.IList(Of ColumnAttribute) = Nothing
                If Not String.IsNullOrEmpty(_field) Then
                    arr = New Generic.List(Of ColumnAttribute)
                    arr.Add(New ColumnAttribute(_field))
                End If
                sb.Append(SelectWithJoin(dbschema, _t, almgr, paramMgr, _joins, _
                    arr IsNot Nothing, Nothing, Nothing, filterInfo, arr))
            End If

            AppendWhere(dbschema, _t, _f, almgr, sb, filterInfo, paramMgr)
        End Sub

        Public Overrides Function MakeQueryStatement(ByVal mpe As ObjectMappingEngine, ByVal filterInfo As Object, _
            ByVal query As Worm.Query.QueryCmd, ByVal params As ICreateParam, _
            ByVal joins As List(Of Worm.Criteria.Joins.QueryJoin), ByVal f As IFilter, ByVal almgr As IPrepareTable, ByVal selList As IEnumerable(Of SelectExpression)) As String

            Return Worm.Query.Database.DbQueryExecutor.MakeQueryStatement(mpe, filterInfo, Me, query, params, joins, f, almgr, Nothing, Nothing, Nothing, 0, selList)
        End Function

        Public Overrides ReadOnly Property SupportParams() As Boolean
            Get
                Return True
            End Get
        End Property
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

