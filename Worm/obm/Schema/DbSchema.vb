Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports CoreFramework.Structures
Imports CoreFramework.Threading
Imports System.Collections.Generic

Namespace Orm

    Public Interface IDbSchema
        Function GetSharedTable(ByVal tableName As String) As OrmTable
        Function GetTables(ByVal type As Type) As OrmTable()
        Function GetJoins(ByVal type As Type, ByVal left As OrmTable, ByVal right As OrmTable) As OrmJoin
    End Interface

    Public Class DbSchema
        Inherits OrmSchemaBase
        Implements IDbSchema

        Public Const QueryLength As Integer = 490
        'Public Const MainRelationTag As String = "main"
        'Public Const SubRelationTag As String = "sub"

        Private _sharedTables As Hashtable = Hashtable.Synchronized(New Hashtable)

        Public Sub New(ByVal version As String)
            MyBase.New(version)
        End Sub

        Public Sub New(ByVal version As String, ByVal mapVersion As MapVersion)
            MyBase.New(version, mapVersion)
        End Sub

        Public Function GetSharedTable(ByVal tableName As String) As OrmTable Implements IDbSchema.GetSharedTable
            Dim t As OrmTable = CType(_sharedTables(tableName), OrmTable)
            If t Is Nothing Then
                SyncLock Me
                    t = CType(_sharedTables(tableName), OrmTable)
                    If t Is Nothing Then
                        t = New OrmTable(tableName)
                        _sharedTables.Add(tableName, t)
                    End If
                End SyncLock
            End If
            Return t
        End Function

        Public Function GetTables(ByVal type As Type) As OrmTable() Implements IDbSchema.GetTables
            If type Is Nothing Then
                Throw New ArgumentNullException("type")
            End If

            Dim schema As IOrmObjectSchema = GetObjectSchema(type)

            Return schema.GetTables
        End Function

        Protected Function GetJoins(ByVal type As Type, ByVal left As OrmTable, ByVal right As OrmTable) As OrmJoin Implements IDbSchema.GetJoins
            If type Is Nothing Then
                Throw New ArgumentNullException("type")
            End If

            Dim schema As IOrmObjectSchema = GetObjectSchema(type)

            Return schema.GetJoins(left, right)
        End Function

        Public Overloads Function GetObjectSchema(ByVal t As Type) As IOrmObjectSchema
            Return CType(MyBase.GetObjectSchema(t), IOrmObjectSchema)
        End Function

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

        Public Overridable Function ParamName(ByVal name As String, ByVal i As Integer) As String
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

        Protected Overridable Function DeclareVariable(ByVal name As String, ByVal type As String) As String
            Return "declare " & name & " " & type
        End Function

        Public Overridable Function SupportMultiline() As Boolean
            Return True
        End Function

        Protected Overridable Function TopStatement(ByVal top As Integer) As String
            Return "top " & top & " "
        End Function

        Protected Overridable Function DefaultValues() As String
            Return "default values"
        End Function

        Public Overridable ReadOnly Property IsNull() As String
            Get
                Return "isnull"
            End Get
        End Property

#End Region

#Region " data factory "
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

        Public Function Insert(ByVal obj As OrmBase, _
            ByRef dbparams As ICollection(Of System.Data.Common.DbParameter), _
            ByRef select_columns As Generic.IList(Of ColumnAttribute)) As String

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Using obj.GetSyncRoot()
                Dim ins_cmd As New StringBuilder
                dbparams = Nothing
                If obj.ObjectState = ObjectState.Created Then
                    'Dim named_params As Boolean = ParamName("p", 1) <> ParamName("p", 2)
                    'Dim type As Type = obj.GetType
                    Dim inserted_tables As New Generic.Dictionary(Of OrmTable, IList(Of OrmFilter))
                    'Dim tables_val As New System.Collections.Specialized.ListDictionary
                    'Dim params_val As New System.Collections.Specialized.ListDictionary
                    'Dim _params As New ArrayList
                    Dim sel_columns As New Generic.List(Of ColumnAttribute)
                    Dim prim_key As ColumnAttribute = Nothing
                    'Dim prim_key_value As Object = Nothing

                    Dim real_t As Type = obj.GetType
                    Dim unions() As String = GetUnions(real_t)
                    'Dim uniontype As String = ""
                    If unions IsNot Nothing Then
                        Throw New NotImplementedException
                        'uniontype = GetUnionType(obj)
                    End If

                    For Each de As DictionaryEntry In GetProperties(real_t)
                        Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                        Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                        If c IsNot Nothing Then
                            Dim current As Object = pi.GetValue(obj, Nothing)
                            Dim att As Field2DbRelations = GetAttributes(real_t, c)
                            If (att And Field2DbRelations.ReadOnly) <> Field2DbRelations.ReadOnly OrElse _
                                (att And Field2DbRelations.InsertDefault) = Field2DbRelations.InsertDefault Then
                                Dim tb As OrmTable = GetFieldTable(real_t, c.FieldName)
                                If unions IsNot Nothing Then
                                    Throw New NotImplementedException
                                    'tb = MapUnionType2Table(real_t, uniontype)
                                End If

                                Dim f As OrmFilter = Nothing
                                Dim v As Object = ChangeValueType(real_t, c, current)
                                If (att And Field2DbRelations.InsertDefault) = Field2DbRelations.InsertDefault AndAlso v Is DBNull.Value Then
                                    f = New OrmFilter(real_t, c.FieldName, DefaultValue, FilterOperation.Equal)
                                Else
                                    f = New OrmFilter(real_t, c.FieldName, New TypeWrap(Of Object)(v), FilterOperation.Equal)
                                End If
                                If Not inserted_tables.ContainsKey(tb) Then
                                    inserted_tables.Add(tb, New List(Of OrmFilter))
                                End If
                                inserted_tables(tb).Add(f)

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

                    Dim tbls() As OrmTable = GetTables(real_t)

                    Dim pkt As OrmTable = tbls(0)

                    If Not inserted_tables.ContainsKey(pkt) Then
                        inserted_tables.Add(pkt, Nothing)
                    End If

                    inserted_tables = Sort(Of OrmTable, IList(Of OrmFilter))(inserted_tables, tbls)

                    For j As Integer = 1 To inserted_tables.Count - 1
                        Dim join_table As OrmTable = tbls(j)
                        Dim jn As OrmJoin = GetJoins(real_t, pkt, join_table)
                        If Not jn.IsEmpty Then
                            Dim f As IOrmFilter = OrmFilter.ChangeValueToLiteral(jn.Condition, real_t, prim_key.FieldName, "@id")

                            If f Is Nothing Then
                                Throw New DBSchemaException("Cannot change join")
                            End If

                            For Each fl As OrmFilter In f.GetAllFilters
                                inserted_tables(join_table).Add(fl)
                            Next
                        End If
                    Next
                    dbparams = FormInsert(inserted_tables, ins_cmd, real_t, sel_columns, _
                         unions, Nothing)

                    select_columns = sel_columns
                End If

                Return ins_cmd.ToString
            End Using
        End Function

        Protected Overridable Function FormInsert(ByVal inserted_tables As Dictionary(Of OrmTable, IList(Of OrmFilter)), _
            ByVal ins_cmd As StringBuilder, ByVal type As Type, _
            ByVal sel_columns As Generic.List(Of ColumnAttribute), _
            ByVal unions() As String, ByVal params As ICreateParam) As ICollection(Of System.Data.Common.DbParameter)

            If params Is Nothing Then
                params = New ParamMgr(Me, "p")
            End If

            If inserted_tables.Count > 0 Then
                If sel_columns IsNot Nothing Then
                    ins_cmd.Append(DeclareVariable("@id", "int"))
                    ins_cmd.Append(EndLine)
                    ins_cmd.Append(DeclareVariable("@rcount", "int"))
                    ins_cmd.Append(EndLine)
                    If inserted_tables.Count > 1 Then
                        ins_cmd.Append(DeclareVariable("@err", "int"))
                        ins_cmd.Append(EndLine)
                    End If
                End If
                Dim b As Boolean = False
                Dim os As IOrmObjectSchema = GetObjectSchema(type)
                Dim pk_table As OrmTable = os.GetTables(0)
                For Each item As Generic.KeyValuePair(Of OrmTable, IList(Of OrmFilter)) In inserted_tables
                    If b Then
                        ins_cmd.Append(EndLine)
                        If SupportIf() Then
                            ins_cmd.Append("if @err = 0 ")
                        End If
                    Else
                        b = True
                    End If

                    Dim pk_id As String = String.Empty

                    If item.Value Is Nothing OrElse item.Value.Count = 0 Then
                        ins_cmd.Append("insert into ").Append(item.Key).Append(" ").Append(DefaultValues)
                    Else
                        ins_cmd.Append("insert into ").Append(item.Key).Append(" (")
                        Dim values_sb As New StringBuilder
                        values_sb.Append(") values(")
                        For Each f As OrmFilter In item.Value
                            Dim p As Pair(Of String) = f.MakeSignleStmt(Me, params)
                            If f.FieldName = "ID" Then
                                Dim att As Field2DbRelations = os.GetFieldColumnMap(f.FieldName).GetAttributes(GetColumnByFieldName(type, f.FieldName))
                                If (att And Field2DbRelations.SyncInsert) = 0 AndAlso _
                                    (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                                    pk_id = p.Second
                                End If
                            End If
                            ins_cmd.Append(p.First).Append(",")
                            values_sb.Append(p.Second).Append(",")
                        Next

                        ins_cmd.Length -= 1
                        values_sb.Length -= 1
                        ins_cmd.Append(values_sb.ToString).Append(")")
                    End If

                    If pk_table.Equals(item.Key) AndAlso sel_columns IsNot Nothing Then
                        ins_cmd.Append(EndLine)
                        ins_cmd.Append("select @rcount = ").Append(RowCount)
                        If String.IsNullOrEmpty(pk_id) Then
                            ins_cmd.Append(", @id = ").Append(LastInsertID)
                        Else
                            ins_cmd.Append(", @id = ").Append(pk_id)
                        End If
                        'ins_cmd.Append(EndLine)
                        If inserted_tables.Count > 1 Then
                            ins_cmd.Append(", @err = ").Append(LastError)
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
                        ins_cmd.Append("select ")
                        Dim com As Boolean = False
                        For Each c As ColumnAttribute In sel_columns
                            If com Then
                                ins_cmd.Append(", ")
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
                                ins_cmd.Append(GetColumnNameByFieldName(type, c.FieldName))
                            End If
                        Next

                        ins_cmd.Append(" from ").Append(pk_table).Append(" where ")
                        If unions IsNot Nothing Then
                            Throw New NotImplementedException
                            'ins_cmd.Append(GetColumnNameByFieldName(type, "ID", pk_table)).Append(" = @id")
                        Else
                            ins_cmd.Append(GetColumnNameByFieldName(type, "ID")).Append(" = @id")
                        End If
                    End If
                End If
            End If

            Return params.Params
        End Function

        Protected Structure TableUpdate
            Public _table As OrmTable
            Public _updates As IList(Of OrmFilter)
            Public _where4update As OrmCondition.OrmConditionConstructor

            Public Sub New(ByVal table As OrmTable)
                _table = table
                _updates = New List(Of OrmFilter)
                _where4update = New OrmCondition.OrmConditionConstructor
            End Sub

        End Structure

        Protected Sub GetChangedFields(ByVal obj As OrmBase, ByVal tables As IDictionary(Of OrmTable, TableUpdate), _
            ByVal sel_columns As Generic.List(Of ColumnAttribute), ByVal unions As String())

            Dim rt As Type = obj.GetType

            For Each de As DictionaryEntry In GetProperties(rt)
                'Dim c As ColumnAttribute = CType(Attribute.GetCustomAttribute(pi, GetType(ColumnAttribute), True), ColumnAttribute)
                Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                If c IsNot Nothing Then
                    Dim original As Object = pi.GetValue(obj.GetModifiedObject, Nothing)

                    'If (c.SyncBehavior And Field2DbRelations.PrimaryKey) <> Field2DbRelations.PrimaryKey AndAlso _
                    '    (c.SyncBehavior And Field2DbRelations.RowVersion) <> Field2DbRelations.RowVersion Then
                    Dim att As Field2DbRelations = GetAttributes(rt, c)
                    If (att And Field2DbRelations.ReadOnly) <> Field2DbRelations.ReadOnly Then

                        Dim current As Object = pi.GetValue(obj, Nothing)
                        If (original IsNot Nothing AndAlso Not original.Equals(current)) OrElse _
                            (current IsNot Nothing AndAlso Not current.Equals(original)) OrElse obj.ForseUpdate(c) Then
                            Dim fieldTable As OrmTable = GetFieldTable(rt, c.FieldName)

                            If unions IsNot Nothing Then
                                Throw New NotImplementedException
                                'fieldTable = MapUnionType2Table(rt, uniontype)
                            End If

                            If Not tables.ContainsKey(fieldTable) Then
                                tables.Add(fieldTable, New TableUpdate(fieldTable))
                                'param_vals.Add(fieldTable, New ArrayList)
                            End If

                            Dim updates As IList(Of OrmFilter) = tables(fieldTable)._updates

                            updates.Add(New OrmFilter(rt, c.FieldName, New TypeWrap(Of Object)(current), FilterOperation.Equal))

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
            Next
        End Sub

        Protected Sub GetUpdateConditions(ByVal obj As OrmBase, _
            ByVal updated_tables As IDictionary(Of OrmTable, TableUpdate), ByVal unions() As String)

            Dim rt As Type = obj.GetType

            For Each de As DictionaryEntry In GetProperties(rt)
                'Dim c As ColumnAttribute = CType(Attribute.GetCustomAttribute(pi, GetType(ColumnAttribute), True), ColumnAttribute)
                Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                If c IsNot Nothing Then
                    Dim att As Field2DbRelations = GetAttributes(rt, c)
                    If (att And Field2DbRelations.PK) = Field2DbRelations.PK OrElse _
                        (att And Field2DbRelations.RV) = Field2DbRelations.RV Then

                        Dim original As Object = pi.GetValue(obj, Nothing)
                        'Dim original As Object = pi.GetValue(obj.ModifiedObject, Nothing)
                        'If obj.ModifiedObject.old_state = ObjectState.Created Then
                        'original = pi.GetValue(obj, Nothing)
                        'End If

                        Dim tb As OrmTable = GetFieldTable(rt, c.FieldName)
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


                        For Each de_table As Generic.KeyValuePair(Of OrmTable, TableUpdate) In updated_tables 'In New Generic.List(Of Generic.KeyValuePair(Of String, TableUpdate))(CType(updated_tables, Generic.ICollection(Of Generic.KeyValuePair(Of String, TableUpdate))))
                            'Dim de_table As TableUpdate = updated_tables(tb)
                            If de_table.Key.Equals(tb) Then
                                'updated_tables(de_table.Key) = New TableUpdate(de_table.Value._table, de_table.Value._updates, de_table.Value._where4update.AddFilter(New OrmFilter(rt, c.FieldName, ChangeValueType(rt, c, original), FilterOperation.Equal)))
                                de_table.Value._where4update.AddFilter(New OrmFilter(rt, c.FieldName, New TypeWrap(Of Object)(original), FilterOperation.Equal))
                            Else
                                Dim join As OrmJoin = GetJoins(rt, tb, de_table.Key)
                                If Not join.IsEmpty Then
                                    Dim f As IOrmFilter = OrmFilter.ChangeValueToParam(join.Condition, rt, c.FieldName, original)

                                    If f Is Nothing Then
                                        Throw New DBSchemaException("Cannot replace join")
                                    End If

                                    'updated_tables(de_table.Key) = New TableUpdate(de_table.Value._table, de_table.Value._updates, de_table.Value._where4update.AddFilter(f))
                                    de_table.Value._where4update.AddFilter(f)
                                End If
                            End If
                        Next
                    End If
                End If
            Next
        End Sub

        Public Overridable Function Update(ByVal obj As OrmBase, ByRef dbparams As IEnumerable(Of System.Data.Common.DbParameter), _
            ByRef select_columns As Generic.IList(Of ColumnAttribute), ByRef updated_fields As IList(Of OrmFilter)) As String

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj parameter cannot be nothing")
            End If

            select_columns = Nothing
            updated_fields = Nothing

            Using obj.GetSyncRoot()
                Dim upd_cmd As New StringBuilder
                dbparams = Nothing
                If obj.ObjectState = ObjectState.Modified Then
                    Dim sel_columns As New Generic.List(Of ColumnAttribute)
                    Dim updated_tables As New Dictionary(Of OrmTable, TableUpdate)
                    Dim rt As Type = obj.GetType

                    Dim unions() As String = GetUnions(rt)
                    'Dim uniontype As String = ""
                    If unions IsNot Nothing Then
                        Throw New NotImplementedException
                        'uniontype = GetUnionType(obj)
                    End If

                    'Dim sb_updates As New StringBuilder

                    GetChangedFields(obj, updated_tables, sel_columns, unions)

                    Dim l As New List(Of OrmFilter)
                    For Each tu As TableUpdate In updated_tables.Values
                        l.AddRange(tu._updates)
                    Next
                    updated_fields = l

                    GetUpdateConditions(obj, updated_tables, unions)

                    select_columns = sel_columns

                    If updated_tables.Count > 0 Then
                        Dim sch As IOrmObjectSchema = GetObjectSchema(rt)
                        Dim pk_table As OrmTable = sch.GetTables()(0)
                        Dim amgr As AliasMgr = AliasMgr.Create
                        Dim params As New ParamMgr(Me, "p")

                        For Each item As Generic.KeyValuePair(Of OrmTable, TableUpdate) In updated_tables
                            If upd_cmd.Length > 0 Then
                                upd_cmd.Append(EndLine)
                                If SupportIf() Then
                                    upd_cmd.Append("if ").Append(LastError).Append(" = 0 ")
                                End If
                            End If

                            Dim tbl As OrmTable = item.Key
                            Dim [alias] As String = amgr.AddTable(tbl, sch, params)

                            upd_cmd.Append("update ").Append([alias]).Append(" set ")
                            For Each f As OrmFilter In item.Value._updates
                                upd_cmd.Append(f.MakeSQLStmt(Me, amgr.Aliases, params)).Append(",")
                            Next
                            upd_cmd.Length -= 1
                            upd_cmd.Append(" from ").Append(tbl).Append(" ").Append([alias])
                            upd_cmd.Append(" where ").Append(item.Value._where4update.Condition.MakeSQLStmt(Me, amgr.Aliases, params))
                            If Not item.Key.Equals(pk_table) Then
                                'Dim pcnt As Integer = 0
                                'If Not named_params Then pcnt = XMedia.Framework.Data.DBA.ExtractParamsCount(upd_cmd.ToString)
                                CorrectUpdateWithInsert(tbl, item.Value, upd_cmd, obj, params)
                                'FormInsert(,upd_cmd,
                            End If
                        Next

                        If sel_columns.Count > 0 Then
                            'If unions Is Nothing Then
                            '    Dim rem_list As New ArrayList
                            '    For Each c As ColumnAttribute In sel_columns
                            '        If Not tables.Contains(GetFieldTable(Type, c.FieldName)) Then rem_list.Add(c)
                            '    Next

                            '    For Each c As ColumnAttribute In rem_list
                            '        sel_columns.Remove(c)
                            '    Next
                            'End If

                            sel_columns.Sort()
                            If sel_columns.Count > 0 Then
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
                                        sel_sb.Append(GetColumnNameByFieldName(rt, c.FieldName))
                                    End If
                                Next

                                Dim [alias] As String = amgr.Aliases(pk_table)
                                sel_sb = sel_sb.Replace(pk_table.TableName, [alias])
                                sel_sb.Append(" from ").Append(pk_table).Append(" ").Append([alias]).Append(" where ")
                                'sel_sb.Append(updated_tables(pk_table)._where4update.Condition.MakeSQLStmt(Me, amgr.Aliases, params))
                                sel_sb.Append(New OrmFilter(rt, "ID", obj, FilterOperation.Equal).MakeSQLStmt(Me, amgr.Aliases, params))

                                upd_cmd.Append(sel_sb)
                            End If
                            select_columns = sel_columns
                        End If

                        dbparams = params.Params
                    End If

                End If
                Return upd_cmd.ToString
            End Using
        End Function

        Protected Overridable Sub CorrectUpdateWithInsert(ByVal table As OrmTable, ByVal tableinfo As TableUpdate, _
            ByVal upd_cmd As StringBuilder, ByVal obj As OrmBase, ByVal params As ICreateParam)

            Dim dic As New Dictionary(Of OrmTable, IList(Of OrmFilter))
            Dim l As New List(Of OrmFilter)
            l.AddRange(tableinfo._updates)
            l.AddRange(tableinfo._where4update.Condition.GetAllFilters)
            dic.Add(table, l.ToArray)
            upd_cmd.Append(EndLine).Append("if ").Append(RowCount).Append(" = 0 ")
            FormInsert(dic, upd_cmd, obj.GetType, Nothing, Nothing, params)
        End Sub

        Protected Sub GetDeletedConditions(ByVal deleted_tables As IDictionary(Of OrmTable, IOrmFilter), ByVal type As Type, ByVal obj As OrmBase)
            Dim tables() As OrmTable = GetTables(type)
            Dim pk_table As OrmTable = tables(0)
            For j As Integer = 0 To tables.Length - 1
                Dim table As OrmTable = tables(j)
                deleted_tables.Add(table, Nothing)
                Dim o As New OrmCondition.OrmConditionConstructor
                If table.Equals(pk_table) Then
                    For Each de As DictionaryEntry In GetProperties(type)
                        Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                        Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                        If c IsNot Nothing Then
                            Dim att As Field2DbRelations = GetAttributes(type, c)
                            If (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                                o.AddFilter(New OrmFilter(type, c.FieldName, "@id", FilterOperation.Equal))
                            ElseIf (att And Field2DbRelations.RV) = Field2DbRelations.RV Then
                                Dim v As Object = pi.GetValue(obj, Nothing)
                                o.AddFilter((New OrmFilter(type, c.FieldName, New TypeWrap(Of Object)(v), FilterOperation.Equal)))
                            End If
                        End If
                    Next
                Else
                    Dim join As OrmJoin = GetJoins(type, tables(0), table)
                    If Not join.IsEmpty Then
                        Dim f As IOrmFilter = OrmFilter.ChangeValueToLiteral(join.Condition, type, "ID", "@id")

                        If f Is Nothing Then
                            Throw New DBSchemaException("Cannot replace join")
                        End If

                        o.AddFilter(f)
                    End If
                End If

                deleted_tables(table) = o.Condition
            Next
        End Sub

        Public Overridable Function Delete(ByVal obj As OrmBase, ByRef dbparams As IEnumerable(Of System.Data.Common.DbParameter)) As String
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj parameter cannot be nothing")
            End If

            Using obj.GetSyncRoot()
                Dim del_cmd As New StringBuilder
                dbparams = Nothing

                If obj.ObjectState = ObjectState.Deleted Then
                    Dim type As Type = obj.GetType
                    Dim params As New ParamMgr(Me, "p")
                    Dim deleted_tables As New Generic.Dictionary(Of OrmTable, IOrmFilter)

                    del_cmd.Append(DeclareVariable("@id", "int")).Append(EndLine)
                    del_cmd.Append("set @id = ").Append(params.CreateParam(obj.Identifier)).Append(EndLine)

                    GetDeletedConditions(deleted_tables, type, obj)

                    For Each de As KeyValuePair(Of OrmTable, IOrmFilter) In deleted_tables
                        del_cmd.Append("delete from ").Append(de.Key)
                        del_cmd.Append(" where ").Append(de.Value.MakeSQLStmt(Me, Nothing, params))
                        del_cmd.Append(EndLine)
                    Next
                    del_cmd.Length -= EndLine.Length
                    dbparams = params.Params
                End If

                Return del_cmd.ToString
            End Using
        End Function

        Public Overridable Function Delete(ByVal t As Type, ByVal filter As IOrmFilter, ByVal params As ParamMgr) As String
            If t Is Nothing Then
                Throw New ArgumentNullException("t parameter cannot be nothing")
            End If

            If filter Is Nothing Then
                Throw New ArgumentNullException("filter parameter cannot be nothing")
            End If

            Dim del_cmd As New StringBuilder
            del_cmd.Append("delete from ").Append(GetTables(t)(0))
            del_cmd.Append(" where ").Append(filter.MakeSQLStmt(Me, Nothing, params))

            Return del_cmd.ToString
        End Function

        Public Overridable Function SelectWithJoin(ByVal original_type As Type, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal joins() As OrmJoin, _
            ByVal wideLoad As Boolean, ByVal distinct As Boolean, ByVal additionalColumns As String, _
            Optional ByVal arr As Generic.IList(Of ColumnAttribute) = Nothing) As String

            If original_type Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "original_type")
            End If

            If almgr.IsEmpty Then
                Throw New ArgumentNullException("parameter cannot be nothing", "almgr")
            End If

            Dim selectcmd As New StringBuilder
            Dim tables() As OrmTable = GetTables(original_type)
            'Dim maintable As String = tables(0)
            If distinct Then
                selectcmd.Append("select distinct ")
            Else
                selectcmd.Append("select ")
            End If

            If wideLoad Then
                Dim columns As String = GetSelectColumnList(original_type, arr)
                selectcmd.Append(columns)
                If Not String.IsNullOrEmpty(additionalColumns) Then
                    selectcmd.Append(",").Append(additionalColumns)
                End If
            Else
                GetPKList(original_type, selectcmd)
            End If
            selectcmd.Append(" from ")
            Dim unions() As String = GetUnions(original_type)
            Dim pmgr As ParamMgr = params 'New ParamMgr()

            If unions Is Nothing Then
                AppendFrom(original_type, almgr, tables, selectcmd, pmgr)
                If joins IsNot Nothing Then
                    For i As Integer = 0 To joins.Length - 1
                        Dim join As OrmJoin = joins(i)

                        If Not join.IsEmpty Then
                            almgr.AddTable(join.Table, Nothing, Nothing)
                            selectcmd.Append(join.MakeSQLStmt(Me, almgr.Aliases, params))
                        End If
                    Next
                End If
            Else
                Throw New NotImplementedException
            End If

            Return selectcmd.ToString
        End Function

        Public Overridable Function SelectDistinct(ByVal original_type As Type, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal relation As M2MRelation, _
            ByVal wideLoad As Boolean, ByVal appendSecondTable As Boolean, _
            Optional ByVal arr As Generic.IList(Of ColumnAttribute) = Nothing) As String

            If original_type Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "original_type")
            End If

            If almgr.IsEmpty Then
                Throw New ArgumentNullException("parameter cannot be nothing", "almgr")
            End If

            Dim selectcmd As New StringBuilder
            Dim tables() As OrmTable = GetTables(original_type)
            'Dim maintable As String = tables(0)
            selectcmd.Append("select distinct ")
            If wideLoad Then
                Dim columns As String = GetSelectColumnList(original_type, arr)
                selectcmd.Append(columns)
            Else
                GetPKList(original_type, selectcmd)
            End If
            selectcmd.Append(" from ")
            Dim unions() As String = GetUnions(original_type)
            Dim pmgr As ParamMgr = params 'New ParamMgr()

            If unions Is Nothing Then
                AppendFrom(original_type, almgr, tables, selectcmd, pmgr)

                Dim r2 As M2MRelation = GetM2MRelation(relation.Type, original_type, True)
                Dim tbl As OrmTable = relation.Table
                Dim f As New OrmFilter(tbl, r2.Column, original_type, "ID", FilterOperation.Equal)
                Dim join As New OrmJoin(tbl, JoinType.Join, f)

                almgr.AddTable(tbl)
                selectcmd.Append(join.MakeSQLStmt(Me, almgr.Aliases, params))

                If appendSecondTable Then
                    AppendJoins(relation.Type, almgr, GetTables(relation.Type), selectcmd, params, tbl, relation.Column, True)
                End If
            Else
                Throw New NotImplementedException
            End If

            Return selectcmd.ToString
        End Function

        Public Overridable Function [Select](ByVal original_type As Type, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, _
            Optional ByVal arr As Generic.IList(Of ColumnAttribute) = Nothing, _
            Optional ByVal additionalColumns As String = Nothing) As String

            If original_type Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "original_type")
            End If

            If almgr.IsEmpty Then
                Throw New ArgumentNullException("parameter cannot be nothing", "almgr")
            End If

            Dim columns As String = GetSelectColumnList(original_type, arr)
            'Dim select_artist As String = "select" & original_type.ToString & columns
            'Dim cmd As String = CStr(map(select_artist))

            'If cmd Is Nothing Then
            'Using SyncHelper.AcquireDynamicLock(select_artist)
            'cmd = CStr(map(select_artist))
            'If cmd Is Nothing Then
            Dim selectcmd As New StringBuilder
            Dim tables() As OrmTable = GetTables(original_type)
            'Dim maintable As String = tables(0)
            selectcmd.Append("select ").Append(columns)
            If Not String.IsNullOrEmpty(additionalColumns) Then
                selectcmd.Append(",").Append(additionalColumns)
            End If
            selectcmd.Append(" from ")
            Dim unions() As String = GetUnions(original_type)
            Dim pmgr As ParamMgr = params 'New ParamMgr()

            If unions Is Nothing Then
                selectcmd = AppendFrom(original_type, almgr, tables, selectcmd, pmgr)
            Else
                Throw New NotImplementedException
                'If tables.Length > 1 Then
                '    Throw New DBSchemaException("Unions doesnot supports joins")
                'End If

                'selectcmd.Replace(tables(0) & ".", "t1.")
                'selectcmd.Append("(").Append(vbCrLf)
                'selectcmd.Append("select ").Append(GetSelectColumnList(original_type))
                'selectcmd.Append(" from ").Append(tables(0)).Append(vbCrLf)
                'For Each u As String In unions
                '    selectcmd.Append("union all").Append(vbCrLf)
                '    selectcmd.Append("select ").Append(GetSelectColumnList(original_type, u))
                '    selectcmd.Append(" from ").Append(u).Append(vbCrLf)
                'Next
                'selectcmd.Append(") t1") '.Append(type.Name)
            End If
            Return selectcmd.ToString
            'cmd = selectcmd.ToString
            'map.Add(select_artist, cmd)
            'params = pmgr.Params
            'End If
            'End Using
            'End If

            'Return cmd
        End Function

        Public Overridable Function SelectID(ByVal original_type As Type, ByVal almgr As AliasMgr, ByVal params As ParamMgr) As String
            'Dim select_artist As String = "selectid" & original_type.ToString
            'Dim cmd As String = CStr(map(select_artist))

            'If cmd Is Nothing Then
            'Using SyncHelper.AcquireDynamicLock(select_artist)
            'cmd = CStr(map(select_artist))
            'If cmd Is Nothing Then
            Dim selectcmd As New StringBuilder
            Dim tables() As OrmTable = GetTables(original_type)
            'Dim maintable As OrmTable = tables(0)
            selectcmd.Append("select ")
            GetPKList(original_type, selectcmd)

            'Dim talias As String = almgr.AddTable(maintable)

            'selectcmd = selectcmd.Replace(maintable, talias)
            selectcmd.Append(" from ")
            Dim unions() As String = GetUnions(original_type)
            If unions Is Nothing Then
                'selectcmd.Append(maintable)
                'selectcmd.Append(" ").Append(talias)
                selectcmd = AppendFrom(original_type, almgr, tables, selectcmd, params)
            Else
                Throw New NotImplementedException
                'If tables.Length > 1 Then
                '    Throw New DBSchemaException("Unions doesnot supports joins")
                'End If

                'selectcmd.Replace(tables(0) & ".", "t1.")
                'selectcmd.Append("(").Append(vbCrLf)
                'selectcmd.Append("select ")
                'GetPKList(original_type, selectcmd)
                'selectcmd.Append(" from ").Append(tables(0)).Append(vbCrLf)
                'For Each u As String In unions
                '    selectcmd.Append("union all").Append(vbCrLf)
                '    selectcmd.Append("select ")
                '    GetPKList(original_type, selectcmd, u)
                '    selectcmd.Append(" from ").Append(u).Append(vbCrLf)
                'Next
                'selectcmd.Append(") t1") '.Append(type.Name)
            End If
            Return selectcmd.ToString
            'cmd = selectcmd.ToString
            'map.Add(select_artist, cmd)
            'End If
            'End Using
            'End If
            'Return cmd
        End Function

        Public Overridable Function SelectTop(ByVal top As Integer, ByVal original_type As Type, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, _
            Optional ByVal arr As Generic.ICollection(Of ColumnAttribute) = Nothing) As String

            If original_type Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "original_type")
            End If

            If almgr.IsEmpty Then
                Throw New ArgumentNullException("parameter cannot be nothing", "almgr")
            End If

            Dim columns As String = GetSelectColumnList(original_type, arr)
            Dim selectcmd As New StringBuilder
            Dim tables() As OrmTable = GetTables(original_type)
            selectcmd.Append("select ").Append(TopStatement(top)).Append(columns)
            selectcmd.Append(" from ")
            Dim pmgr As ParamMgr = params 'New ParamMgr()
            selectcmd = AppendFrom(original_type, almgr, tables, selectcmd, pmgr)
            Return selectcmd.ToString
        End Function

        Public Overridable Function SelectIDTop(ByVal top As Integer, ByVal original_type As Type, ByVal almgr As AliasMgr, ByVal params As ParamMgr) As String
            If original_type Is Nothing Then
                Throw New ArgumentNullException("parameter cannot be nothing", "original_type")
            End If

            If almgr.IsEmpty Then
                Throw New ArgumentNullException("parameter cannot be nothing", "almgr")
            End If

            Dim selectcmd As New StringBuilder
            Dim tables() As OrmTable = GetTables(original_type)
            'Dim maintable As OrmTable = tables(0)
            selectcmd.Append("select ").Append(TopStatement(top))
            GetPKList(original_type, selectcmd)

            selectcmd.Append(" from ")
            selectcmd = AppendFrom(original_type, almgr, tables, selectcmd, params)
            Return selectcmd.ToString
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
        Protected Friend Sub AppendJoins(ByVal selectedType As Type, ByVal almgr As AliasMgr, _
            ByVal tables As OrmTable(), ByVal selectcmd As StringBuilder, ByVal pname As ParamMgr, _
            ByVal table As OrmTable, ByVal id As String, ByVal appendMainTable As Boolean)

            Dim pk_table As OrmTable = tables(0)

            If Not appendMainTable Then

                For j As Integer = 1 To tables.Length - 1
                    Dim join As OrmJoin = GetJoins(selectedType, pk_table, tables(j))

                    If Not join.IsEmpty Then
                        almgr.AddTable(tables(j), Nothing, Nothing)

                        OrmFilter.ChangeValueToLiteral(join, selectedType, "ID", table, id)
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

                        selectcmd.Append(join.MakeSQLStmt(Me, almgr.Aliases, pname))
                    End If
                Next
            Else
                Dim tbl As OrmTable = pk_table
                Dim sch As IOrmObjectSchema = GetObjectSchema(selectedType)
                Dim j As New OrmJoin(tbl, JoinType.Join, New OrmFilter(table, id, selectedType, "ID", FilterOperation.Equal))
                almgr.AddTable(tbl, sch, pname)
                selectcmd.Append(j.MakeSQLStmt(Me, almgr.Aliases, pname))
                For i As Integer = 1 To tables.Length - 1
                    Dim join As OrmJoin = sch.GetJoins(pk_table, tables(i))

                    If Not join.IsEmpty Then
                        almgr.AddTable(tables(i), Nothing, Nothing)
                        selectcmd.Append(join.MakeSQLStmt(Me, almgr.Aliases, pname))
                    End If
                Next
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
        Protected Sub AppendFromM2M(ByVal selectedType As Type, ByVal almgr As AliasMgr, ByVal tables As OrmTable(), _
            ByVal selectcmd As StringBuilder, ByVal pname As ParamMgr, ByVal table As OrmTable, ByVal id As String, _
            ByVal appendMainTable As Boolean)

            If table Is Nothing Then
                Throw New ArgumentNullException("table parameter cannot be nothing")
            End If

            'Dim schema As IOrmObjectSchema = GetObjectSchema(original_type)

            selectcmd.Append(table).Append(" ").Append(almgr.Aliases(table))

            'Dim f As IOrmFilter = schema.GetFilter(filter_info)
            AppendJoins(selectedType, almgr, tables, selectcmd, pname, table, id, appendMainTable)
        End Sub

        ''' <summary>
        ''' Построение таблиц, включая джоины
        ''' </summary>
        ''' <param name="original_type"></param>
        ''' <param name="almgr"></param>
        ''' <param name="tables"></param>
        ''' <param name="selectcmd"></param>
        ''' <param name="pname"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function AppendFrom(ByVal original_type As Type, ByVal almgr As AliasMgr, _
            ByVal tables As OrmTable(), ByVal selectcmd As StringBuilder, ByVal pname As ParamMgr) As StringBuilder
            Dim sch As IOrmObjectSchema = GetObjectSchema(original_type)
            For i As Integer = 0 To tables.Length - 1
                Dim tbl As OrmTable = tables(i)
                Dim tbl_real As OrmTable = tbl
                Dim [alias] As String = Nothing
                If Not almgr.Aliases.TryGetValue(tbl, [alias]) Then
                    [alias] = almgr.AddTable(tbl_real, sch, pname)
                Else
                    tbl_real = tbl.OnTableAdd(pname)
                    If tbl_real Is Nothing Then
                        tbl_real = tbl
                    End If
                End If

                selectcmd = selectcmd.Replace(tbl.TableName & ".", [alias] & ".")

                If i = 0 Then
                    selectcmd.Append(tbl_real.TableName).Append(" ").Append([alias])
                End If

                For j As Integer = i + 1 To tables.Length - 1
                    Dim join As OrmJoin = GetJoins(original_type, tbl, tables(j))

                    If Not join.IsEmpty Then
                        If Not almgr.Aliases.ContainsKey(tables(j)) Then
                            almgr.AddTable(tables(j), Nothing, Nothing)
                        End If
                        selectcmd.Append(join.MakeSQLStmt(Me, almgr.Aliases, pname))
                    End If
                Next
            Next

            Return selectcmd
        End Function

        Public Overridable Function AppendWhere(ByVal t As Type, ByVal filter As IOrmFilter, _
            ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal filter_info As Object, ByVal pmgr As ParamMgr) As Boolean

            Dim schema As IOrmObjectSchema = GetObjectSchema(t)
            Dim con As New OrmCondition.OrmConditionConstructor
            con.AddFilter(schema.GetFilter(filter_info)).AddFilter(filter)

            If Not con.IsEmpty Then
                sb.Append(" where ").Append(con.Condition.MakeSQLStmt(Me, almgr.Aliases, pmgr))
                Return True
            End If
            Return False
        End Function

        Public Sub AppendOrder(ByVal t As Type, ByVal sort As Sort, ByVal almgr As AliasMgr, ByVal sb As StringBuilder)
            If sort IsNot Nothing AndAlso Not sort.IsExternal Then
                Dim schema As IOrmObjectSchema = GetObjectSchema(t)
                'Dim s As IOrmSorting = TryCast(schema, IOrmSorting)
                'If s Is Nothing Then

                'End If
                'Dim sort_field As String = schema.MapSort2FieldName(sort)
                'If String.IsNullOrEmpty(sort_field) Then
                '    Throw New ArgumentException("Sort " & sort & " is not supported", "sort")
                'End If

                Dim map As MapField2Column = Nothing
                If schema.GetFieldColumnMap().TryGetValue(sort.FieldName, map) Then
                    sb.Append(" order by ").Append(almgr.Aliases(map._tableName)).Append(".").Append(map._columnName)
                    If sort.Order = Orm.SortType.Desc Then
                        sb.Append(" desc")
                    End If
                Else
                    Throw New ArgumentException(String.Format("Field {0} is not defuned", sort.FieldName))
                End If
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

        Public Function SelectM2M(ByVal selectedType As Type, ByVal filteredType As Type, _
            ByVal appendMainTable As Boolean, ByVal appJoins As Boolean, _
            ByVal pmgr As ParamMgr, ByVal almgr As AliasMgr, ByVal withLoad As Boolean, ByVal direct As Boolean) As String

            Dim schema As IOrmObjectSchema = GetObjectSchema(selectedType)
            'Dim schema2 As IOrmObjectSchema = GetObjectSchema(filteredType)

            'column - select
            Dim selected_r As M2MRelation = Nothing
            'column - filter
            Dim filtered_r As M2MRelation = Nothing

            filtered_r = GetM2MRelation(selectedType, filteredType, direct)
            selected_r = GetRevM2MRelation(selectedType, filteredType, direct)

            If selected_r Is Nothing Then
                Throw New DBSchemaException(String.Format("Type {0} has no relation to {1}", selectedType.Name, filteredType.Name))
            End If

            If filtered_r Is Nothing Then
                Throw New DBSchemaException(String.Format("Type {0} has no relation to {1}", filteredType.Name, selectedType.Name))
            End If

            Dim table As OrmTable = selected_r.Table

            If table Is Nothing Then
                Throw New ArgumentException("Invalid relation", filteredType.ToString)
            End If

            Dim [alias] As String = almgr.AddTable(table, Nothing, Nothing)

            Dim sb As New StringBuilder
            Dim id_clm As String = selected_r.Column
            sb.Append("select ").Append([alias]).Append(".")
            sb.Append(filtered_r.Column).Append(" ").Append(filteredType.Name).Append("ID")
            If filtered_r Is selectedType Then
                sb.Append("Rev")
            End If
            sb.Append(",").Append([alias]).Append(".").Append(id_clm)
            sb.Append(" ").Append(selectedType.Name).Append("ID")
            If withLoad Then
                sb.Append(",").Append(GetSelectColumnList(selectedType))
                appendMainTable = True
            End If
            sb.Append(" from ")

            If appJoins Then
                AppendFromM2M(selectedType, almgr, schema.GetTables, sb, pmgr, table, id_clm, appendMainTable)
                For Each tbl As OrmTable In schema.GetTables
                    If almgr.Aliases.ContainsKey(tbl) Then
                        [alias] = almgr.Aliases(tbl)
                        sb = sb.Replace(tbl.TableName & ".", [alias] & ".")
                    End If
                Next
            Else
                Dim tbl As OrmTable = schema.GetTables(0)
                AppendFromM2M(selectedType, almgr, New OrmTable() {tbl}, sb, pmgr, table, id_clm, appendMainTable)
                If almgr.Aliases.ContainsKey(tbl) Then
                    sb = sb.Replace(tbl.TableName & ".", almgr.Aliases(tbl) & ".")
                End If
            End If

            Return sb.ToString
        End Function

        Public Function SelectM2M(ByVal almgr As AliasMgr, ByVal obj As OrmBase, ByVal type As Type, ByVal filter As IOrmFilter, _
            ByVal filter_info As Object, ByVal appJoins As Boolean, ByVal withLoad As Boolean, ByVal appendMain As Boolean, _
            ByRef params As IList(Of System.Data.Common.DbParameter), ByVal direct As Boolean) As String

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim pmgr As New ParamMgr(Me, "p")

            Dim sb As New StringBuilder

            Dim t As System.Type = obj.GetType

            'Dim schema As IOrmObjectSchema = GetObjectSchema(t)
            Dim schema2 As IOrmObjectSchema = GetObjectSchema(type)

            Dim appendMainTable As Boolean = filter IsNot Nothing OrElse schema2.GetFilter(filter_info) IsNot Nothing OrElse appendMain
            sb.Append(SelectM2M(type, t, appendMainTable, appJoins, pmgr, almgr, withLoad, direct))

            Dim selected_r As M2MRelation = Nothing
            Dim filtered_r As M2MRelation = Nothing

            filtered_r = GetM2MRelation(type, t, direct)
            selected_r = GetRevM2MRelation(type, t, direct)

            If selected_r Is Nothing Then
                Throw New DBSchemaException(String.Format("Type {0} has no relation to {1}", t.Name, type.Name))
            End If

            If filtered_r Is Nothing Then
                Throw New DBSchemaException(String.Format("Type {0} has no relation to {1}", type.Name, t.Name))
            End If

            Dim id_clm As String = filtered_r.Column

            Dim table As OrmTable = selected_r.Table

            Dim f As New OrmFilter(table, id_clm, obj, FilterOperation.Equal)
            Dim con As New OrmCondition.OrmConditionConstructor
            con.AddFilter(f)
            con.AddFilter(filter)
            AppendWhere(type, con.Condition, almgr, sb, filter_info, pmgr)

            params = pmgr.Params

            Return sb.ToString
        End Function

        Public Function MakeSearchContainsStatements(ByVal t As Type, ByVal tokens() As String, ByVal fields() As String, _
            ByVal sectionName As String, ByVal join As OrmJoin, ByVal sort_type As SortType, _
            ByVal params As ParamMgr, ByVal filter_info As Object) As String

            Dim obj_schema As IOrmObjectSchema = GetObjectSchema(t)
            Dim fs As IOrmFullTextSupport = TryCast(obj_schema, IOrmFullTextSupport)

            Dim value As String = Configuration.SearchSection.GetValueForContains(tokens, sectionName, fs)
            If String.IsNullOrEmpty(value) Then
                Return Nothing
            End If

            Dim almgr As AliasMgr = AliasMgr.Create
            Dim ct As New OrmTable("containstable")
            Dim [alias] As String = almgr.AddTable(ct)
            Dim pname As String = params.CreateParam(value)
            'cols = New Generic.List(Of ColumnAttribute)
            Dim sb As New StringBuilder, columns As New StringBuilder
            Dim tbl As OrmTable = GetTables(t)(0)
            sb.Append("select [key] ").Append(obj_schema.GetFieldColumnMap("ID")._columnName)
            Dim appendMain As Boolean = False
            Dim main_table As OrmTable = GetTables(t)(0)
            Dim ins_idx As Integer = sb.Length
            If fields IsNot Nothing AndAlso fields.Length > 0 Then
                appendMain = True
                For Each field As String In fields
                    columns.Append(",").Append(main_table).Append(".")
                    columns.Append(GetColumnNameByFieldNameInternal(t, field, False))
                Next
            End If
            sb.Append(" from containstable(")
            sb.Append(tbl).Append(",*,")
            sb.Append(pname).Append(") ").Append([alias])
            If Not appendMain Then
                appendMain = obj_schema.GetFilter(filter_info) IsNot Nothing
            End If
            AppendJoins(t, almgr, GetTables(t), sb, params, ct, "[key]", appendMain)
            If appendMain Then
                Dim mainAlias As String = almgr.Aliases(main_table)
                sb.Insert(ins_idx, columns.Replace(main_table.TableName, mainAlias).ToString)
            End If

            If Not join.IsEmpty Then
                Dim r As New OrmFilter(t, "ID", New TypeWrap(Of Object)(Nothing), FilterOperation.Equal)
                Dim r2 As IOrmFilter = Nothing
                For Each f As IOrmFilter In join.Condition.GetAllFilters
                    If f.GetStaticString = r.GetStaticString Then
                        r2 = New OrmFilter(ct, "[key]", New TypeWrap(Of Object)(r.Value), FilterOperation.Equal)
                    End If
                Next

                If r2 Is Nothing Then
                    Throw New DBSchemaException("Invalid join")
                Else
                    join.ReplaceFilter(r, r2)
                End If

                sb.Append(join.MakeSQLStmt(Me, almgr.Aliases, params))
            End If
            AppendWhere(t, Nothing, almgr, sb, filter_info, params)
            sb.Append(" order by rank ").Append(sort_type.ToString)
            Return sb.ToString
        End Function

        Public Function MakeSearchFreetextStatements(ByVal t As Type, ByVal tokens() As String, ByVal fields() As String, _
            ByVal sectionName As String, ByVal join As OrmJoin, ByVal sort_type As SortType, _
            ByVal params As ParamMgr, ByVal filter_info As Object) As String

            Dim value As String = Configuration.SearchSection.GetValueForFreeText(t, tokens, sectionName)
            If String.IsNullOrEmpty(value) Then
                Return Nothing
            End If

            Dim almgr As AliasMgr = AliasMgr.Create
            Dim ft As New OrmTable("freetexttable")
            Dim [alias] As String = almgr.AddTable(ft)
            Dim pname As String = params.CreateParam(value)
            'cols = New Generic.List(Of ColumnAttribute)
            Dim sb As New StringBuilder, columns As New StringBuilder
            Dim tbl As OrmTable = GetTables(t)(0)
            Dim obj_schema As IOrmObjectSchema = GetObjectSchema(t)
            sb.Append("select [key] ").Append(obj_schema.GetFieldColumnMap("ID")._columnName)
            Dim appendMain As Boolean = False
            Dim main_table As OrmTable = GetTables(t)(0)
            Dim ins_idx As Integer = sb.Length
            If fields IsNot Nothing AndAlso fields.Length > 0 Then
                appendMain = True
                For Each field As String In fields
                    columns.Append(",").Append(main_table).Append(".")
                    columns.Append(GetColumnNameByFieldNameInternal(t, field, False))
                Next
            End If
            sb.Append(" from freetexttable(")
            sb.Append(tbl).Append(",*,")
            sb.Append(pname).Append(",500) ").Append([alias])
            If Not appendMain Then
                appendMain = obj_schema.GetFilter(filter_info) IsNot Nothing
            End If
            AppendJoins(t, almgr, GetTables(t), sb, params, ft, "[key]", appendMain)
            If appendMain Then
                Dim mainAlias As String = almgr.Aliases(main_table)
                sb.Insert(ins_idx, columns.Replace(main_table.TableName, mainAlias).ToString)
            End If

            If Not join.IsEmpty Then
                Dim r As New OrmFilter(t, "ID", New TypeWrap(Of Object)(Nothing), FilterOperation.Equal)
                Dim r2 As IOrmFilter = Nothing
                For Each f As IOrmFilter In join.Condition.GetAllFilters
                    If f.GetStaticString = r.GetStaticString Then
                        r2 = New OrmFilter(ft, "[key]", New TypeWrap(Of Object)(r.Value), FilterOperation.Equal)
                    End If
                Next

                If r2 Is Nothing Then
                    Throw New DBSchemaException("Invalid join")
                Else
                    join.ReplaceFilter(r, r2)
                End If

                sb.Append(join.MakeSQLStmt(Me, almgr.Aliases, params))
            End If
            AppendWhere(t, Nothing, almgr, sb, filter_info, params)
            sb.Append(" order by rank ").Append(sort_type.ToString)
            Return sb.ToString
        End Function

        Public Function GetDictionarySelect(ByVal type As Type, ByVal level As Integer, _
            ByVal params As ParamMgr, ByVal filter As IOrmFilter, ByVal filter_info As Object) As String

            Dim odic As IOrmDictionary = TryCast(GetObjectSchema(type), IOrmDictionary)
            If odic Is Nothing OrElse String.IsNullOrEmpty(odic.GetFirstDicField) Then
                Throw New InvalidOperationException(String.Format("Type {0} is not supports dictionary", type.Name))
            End If

            Dim sb As New StringBuilder
            Dim almgr As AliasMgr = AliasMgr.Create

            If String.IsNullOrEmpty(odic.GetSecondDicField) Then
                sb.Append(GetDicStmt(type, odic.GetFirstDicField, level, almgr, params, filter, filter_info, True))
            Else
                sb.Append("select name,sum(cnt) from (")
                sb.Append(GetDicStmt(type, odic.GetFirstDicField, level, almgr, params, filter, filter_info, False))
                sb.Append(" union ")
                sb.Append(GetDicStmt(type, odic.GetSecondDicField, level, almgr, params, filter, filter_info, False))
                sb.Append(") sd group by name order by name")
            End If

            Return sb.ToString
        End Function

        Protected Function GetDicStmt(ByVal t As Type, ByVal field As String, ByVal level As Integer, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal filter As IOrmFilter, _
            ByVal filter_info As Object, ByVal appendOrder As Boolean) As String
            Dim sb As New StringBuilder
            Dim tbl As OrmTable = GetTables(t)(0)
            Dim al As String = "ZZZZZXXXXXXXX"
            'If Not almgr.Aliases.ContainsKey(tbl) Then
            '    al = almgr.AddTable(tbl)
            'Else
            '    al = almgr.Aliases(tbl)
            'End If

            Dim n As String = GetColumnNameByFieldNameInternal(t, field, False)
            sb.Append("select left(")
            sb.Append(al).Append(".").Append(n)
            sb.Append(",").Append(level).Append(") name,count(*) cnt from ")
            AppendFrom(t, almgr, GetTables(t), sb, params)

            AppendWhere(t, filter, almgr, sb, filter_info, params)

            sb.Append(" group by left(")
            sb.Append(al).Append(".").Append(n)
            sb.Append(",").Append(level).Append(")")

            If appendOrder Then
                sb.Append(" order by left(")
                sb.Append(al).Append(".").Append(n)
                sb.Append(",").Append(level).Append(")")
            End If

            sb.Replace(al, almgr.Aliases(tbl))

            Return sb.ToString
        End Function

        Public Function SaveM2M(ByVal obj As OrmBase, ByVal relation As M2MRelation, ByVal entry As EditableList, _
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
            Dim tbl As OrmTable = relation.Table
            Dim param_relation As M2MRelation = GetRevM2MRelation(obj.GetType, relation.Type, Not relation.non_direct)

            If param_relation Is Nothing Then
                Throw New ArgumentException("Invalid relation")
            End If

            Dim pk As String = Nothing
            If entry.HasDeleted Then
                sb.Append("delete from ").Append(tbl.TableName)
                sb.Append(" where ").Append(param_relation.Column).Append(" = ")
                pk = pmgr.AddParam(pk, obj.Identifier)
                sb.Append(pk).Append(" and ").Append(relation.Column).Append(" in(")
                For Each id As Integer In entry.Deleted
                    sb.Append(id).Append(",")
                Next
                sb.Length -= 1
                sb.AppendLine(")")
                If entry.HasAdded Then
                    sb.Append("if ").Append(LastError).AppendLine(" = 0 begin")
                End If
            End If

            If entry.HasAdded Then
                For Each id As Integer In entry.Added
                    If entry.HasDeleted Then
                        sb.Append(vbTab)
                    End If
                    sb.Append("if ").Append(LastError).Append(" = 0 ")
                    sb.Append("insert into ").Append(tbl.TableName).Append("(")
                    sb.Append(param_relation.Column).Append(",").Append(relation.Column).Append(") values(")
                    pk = pmgr.AddParam(pk, obj.Identifier)
                    sb.Append(pk).Append(",").Append(id).AppendLine(")")
                Next

                If entry.HasDeleted Then
                    sb.AppendLine("end")
                End If
            End If

            Return sb.ToString
        End Function

#End Region

        Public Function PrepareConcurrencyException(ByVal obj As OrmBase) As OrmManagerException
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim sb As New StringBuilder
            Dim t As Type = obj.GetType
            sb.Append("Concurrency violation error during save object ")
            sb.Append(t.Name).Append(". Key values {")
            Dim cm As Boolean = False
            For Each de As DictionaryEntry In GetProperties(t)
                Dim pi As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                If c IsNot Nothing AndAlso _
                    ((GetAttributes(t, c) And Field2DbRelations.PK) = Field2DbRelations.PK OrElse _
                    (GetAttributes(t, c) And Field2DbRelations.RV) = Field2DbRelations.RV) Then

                    Dim s As String = GetColumnNameByFieldName(t, c.FieldName)
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
    End Class

End Namespace

